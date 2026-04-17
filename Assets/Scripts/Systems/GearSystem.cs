using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;

namespace ReinoOscuridad.Systems
{
    /// Gestiona el equipamiento de gear en héroes, la mejora con rolls de substats
    /// y el cálculo de stats finales combinados héroe + gear.
    /// NUNCA escribe a Firestore — todo opera en memoria vía PlayerDataSystem.
    ///
    /// Rolls de substats: +3 · +6 · +9 · +12 (distribución triangular sesgada al mínimo)
    /// Stats combinados: HeroProgressionSystem.BuildHeroInstance + gear stats encima.
    [DefaultExecutionOrder(-6)]
    public class GearSystem : MonoBehaviour, ISystem
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        public static GearSystem Instance { get; private set; }

        // ── Tablas internas ────────────────────────────────────────────────────

        /// instanceId → GearInstance (todas las piezas del jugador en memoria)
        private Dictionary<string, GearInstance> _gearById = new();

        /// gearId → GearCatalogItem (catálogo cargado en Initialize)
        private Dictionary<string, GearCatalogItem> _catalogById = new();

        // Slots: índice 0-5 → nombre de slot en GearInstance.slot
        private static readonly string[] SLOT_NAMES =
            { "weapon", "helmet", "armor", "boots", "ring", "necklace" };

        // Mapeo del tipo de catálogo (español) → slot name (inglés en GearInstance)
        private static readonly Dictionary<string, string> CATALOG_TYPE_TO_SLOT = new()
        {
            { "espada",  "weapon"   },
            { "casco",   "helmet"   },
            { "pechera", "armor"    },
            { "botas",   "boots"    },
            { "guantes", "ring"     },
            { "escudo",  "necklace" }
        };

        // Rangos por defecto si el substat no está en el catálogo
        private static readonly Dictionary<string, (int min, int max)> SUBSTAT_DEFAULTS = new()
        {
            { "atk%",   (3, 8)  },
            { "def%",   (3, 8)  },
            { "hp%",    (3, 8)  },
            { "spd",    (2, 5)  },
            { "tcri%",  (3, 8)  },
            { "dcri%",  (4, 10) },
            { "acc",    (3, 8)  },
            { "res",    (3, 8)  },
            { "agi",    (2, 6)  },
            { "luk%",   (2, 6)  }
        };

        // ── Dependencias ───────────────────────────────────────────────────────

        private PlayerDataSystem _pds;
        private EconomySystem    _eco;

        // ── Ciclo de vida Unity ────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameManager.Instance.RegisterSystem(this);
        }

        // ── ISystem ───────────────────────────────────────────────────────────

        public void Initialize()
        {
            _pds = GameManager.Instance.GetSystem<PlayerDataSystem>();
            _eco = GameManager.Instance.GetSystem<EconomySystem>();

            if (_pds == null) Debug.LogError("[GearSystem] PlayerDataSystem no encontrado.");
            if (_eco == null) Debug.LogWarning("[GearSystem] EconomySystem no encontrado — costes de mejora no podrán verificarse.");

            LoadGearCatalog();
            BuildRegistryFromPlayerData();

            Debug.Log($"[GearSystem] Inicializado — {_catalogById.Count} piezas en catálogo · {_gearById.Count} instancias cargadas.");
        }

        public void OnSessionStart() { }
        public void OnSessionEnd()   { }

        // ── API pública — Inventario ───────────────────────────────────────────

        /// Registra una pieza de gear nueva en el sistema y la añade al inventario del jugador.
        /// Usar al obtener gear (gacha, drops, crafting).
        /// Si instanceId está vacío se genera un GUID nuevo.
        public void AddGearToInventory(GearInstance gear)
        {
            if (gear == null) return;

            if (string.IsNullOrEmpty(gear.instanceId))
                gear.instanceId = Guid.NewGuid().ToString();

            gear.equipadoEn = null;
            gear.substats ??= new List<SubstatEntry>();

            _gearById[gear.instanceId] = gear;

            var pd = _pds?.GetPlayerData();
            if (pd != null)
            {
                pd.gearInventory ??= new List<PlayerGearInstance>();
                pd.gearInventory.Add(ConvertToPlayerGear(gear));
                _pds.MarkDirty();
            }
        }

        /// Devuelve la instancia de gear por su instanceId, o null si no existe.
        public GearInstance GetGear(string instanceId)
            => _gearById.TryGetValue(instanceId, out var g) ? g : null;

        // ── API pública — Equipar / Desequipar ────────────────────────────────

        /// Equipa la pieza indicada en el héroe.
        /// slot: índice 0–5 (0=weapon, 1=helmet, 2=armor, 3=boots, 4=ring, 5=necklace).
        /// Si el héroe ya tiene gear en ese slot lo mueve al inventario.
        /// Devuelve false si la pieza no existe o el slot no coincide con el tipo de gear.
        public bool EquipGear(string instanceId, string heroId, int slot)
        {
            if (!_gearById.TryGetValue(instanceId, out var gear))
            {
                Debug.LogError($"[GearSystem] EquipGear: instanceId '{instanceId}' no encontrado.");
                return false;
            }

            if (slot < 0 || slot >= SLOT_NAMES.Length)
            {
                Debug.LogError($"[GearSystem] EquipGear: índice de slot inválido ({slot}).");
                return false;
            }

            string expectedSlot = SLOT_NAMES[slot];
            if (!string.IsNullOrEmpty(gear.slot) && gear.slot != expectedSlot)
            {
                Debug.Log($"[GearSystem] EquipGear: slot mismatch — gear es '{gear.slot}', slot {slot} espera '{expectedSlot}'.");
                return false;
            }

            var pd = _pds?.GetPlayerData();
            if (pd?.heroes == null)
            {
                Debug.LogError("[GearSystem] EquipGear: PlayerData no disponible.");
                return false;
            }

            var hero = pd.heroes.Find(h => h.heroId == heroId);
            if (hero == null)
            {
                Debug.LogError($"[GearSystem] EquipGear: héroe '{heroId}' no encontrado en el roster.");
                return false;
            }

            hero.equipment ??= new List<PlayerGearInstance>();

            // Si ya hay gear en este slot, moverlo al inventario
            var occupying = FindEquippedGearInSlot(heroId, gear.slot);
            if (occupying != null)
            {
                occupying.equipadoEn = null;
                hero.equipment.RemoveAll(pg => pg.instanceId == occupying.instanceId);
                pd.gearInventory ??= new List<PlayerGearInstance>();
                pd.gearInventory.Add(ConvertToPlayerGear(occupying));
            }

            // Retirar del inventario si estaba ahí
            pd.gearInventory?.RemoveAll(pg => pg.instanceId == instanceId);

            // Equipar
            gear.equipadoEn = heroId;
            hero.equipment.Add(ConvertToPlayerGear(gear));
            _pds.MarkDirty();

            EventBus.Publish(new GearChangedData
            {
                instanceId = instanceId,
                gearId     = gear.gearId,
                changeType = "equipped",
                heroId     = heroId
            });
            Debug.Log($"[GearSystem] {instanceId} equipado en '{heroId}' (slot {slot}).");
            return true;
        }

        /// Desequipa la pieza y la mueve al inventario.
        public void UnequipGear(string instanceId)
        {
            if (!_gearById.TryGetValue(instanceId, out var gear))
            {
                Debug.LogError($"[GearSystem] UnequipGear: instanceId '{instanceId}' no encontrado.");
                return;
            }

            if (gear.equipadoEn == null)
            {
                Debug.Log($"[GearSystem] UnequipGear: '{instanceId}' ya está en el inventario.");
                return;
            }

            string prevHeroId = gear.equipadoEn;

            var pd = _pds?.GetPlayerData();
            if (pd != null)
            {
                var hero = pd.heroes?.Find(h => h.heroId == prevHeroId);
                hero?.equipment?.RemoveAll(pg => pg.instanceId == instanceId);

                gear.equipadoEn = null;
                pd.gearInventory ??= new List<PlayerGearInstance>();
                pd.gearInventory.Add(ConvertToPlayerGear(gear));
                _pds.MarkDirty();
            }

            EventBus.Publish(new GearChangedData
            {
                instanceId = instanceId,
                gearId     = gear.gearId,
                changeType = "unequipped",
                heroId     = prevHeroId
            });
            Debug.Log($"[GearSystem] {instanceId} desequipado de '{prevHeroId}'.");
        }

        // ── API pública — Mejora ───────────────────────────────────────────────

        /// Intenta mejorar la pieza en +1.
        /// Coste: (nivelActual + 1) * 500 Oro Negro.
        /// Rolls de substats en los niveles +3, +6, +9, +12.
        /// Devuelve false si ya está al nivel máximo o no hay oro suficiente.
        public bool TryUpgradeGear(string instanceId)
        {
            if (!_gearById.TryGetValue(instanceId, out var gear))
            {
                Debug.LogError($"[GearSystem] TryUpgradeGear: '{instanceId}' no encontrado.");
                return false;
            }

            if (gear.nivel >= 15)
            {
                Debug.Log($"[GearSystem] TryUpgradeGear: '{instanceId}' ya en nivel máximo (15).");
                return false;
            }

            int cost = (gear.nivel + 1) * 500;

            // Consumir Oro Negro via EconomySystem si está disponible
            if (_eco != null)
            {
                if (!_eco.ConsumeGold(cost))
                {
                    Debug.Log($"[GearSystem] TryUpgradeGear: oro insuficiente (necesita {cost}).");
                    return false;
                }
            }
            else
            {
                // Fallback directo a PlayerData si EconomySystem no está listo
                var pd = _pds?.GetPlayerData();
                if (pd == null || pd.oroNegro < cost)
                {
                    Debug.Log($"[GearSystem] TryUpgradeGear: oro insuficiente (necesita {cost}).");
                    return false;
                }
                pd.oroNegro -= cost;
            }

            gear.nivel += 1;

            // Rolls de substats en +3, +6, +9, +12
            if (gear.nivel == 3 || gear.nivel == 6 || gear.nivel == 9 || gear.nivel == 12)
                RollSubstat(gear);

            // Actualizar mainStatValue con el nuevo nivel
            gear.mainStatValue = CalculateMainStatValue(gear);

            SyncGearInPlayerData(gear);
            _pds.MarkDirty();

            EventBus.Publish(new GearChangedData
            {
                instanceId = instanceId,
                gearId     = gear.gearId,
                changeType = "upgraded",
                heroId     = gear.equipadoEn
            });
            Debug.Log($"[GearSystem] '{instanceId}' mejorado a +{gear.nivel} (coste: {cost} Oro Negro).");
            return true;
        }

        // ── API pública — Stats combinados ─────────────────────────────────────

        /// Construye una HeroInstance con los stats finales héroe + gear.
        /// Base: HeroProgressionSystem.BuildHeroInstance al nivel actual del héroe.
        /// Encima: stats del gear equipado (mainStat + substats revelados).
        /// Stats porcentuales se aplican sobre la base heroica (no acumulativa).
        /// Devuelve null si el héroe no existe en el roster o en el catálogo.
        public HeroInstance BuildCombatInstance(string heroId)
        {
            var hps = HeroProgressionSystem.Instance;
            if (hps == null)
            {
                Debug.LogError("[GearSystem] BuildCombatInstance: HeroProgressionSystem no disponible.");
                return null;
            }

            var pd = _pds?.GetPlayerData();
            var heroData = pd?.heroes?.Find(h => h.heroId == heroId);
            if (heroData == null)
            {
                Debug.LogError($"[GearSystem] BuildCombatInstance: '{heroId}' no está en el roster.");
                return null;
            }

            var instance = hps.BuildHeroInstance(heroId, heroData.level);
            if (instance == null) return null;

            // Capturar stats base antes de aplicar gear (para porcentuales)
            int baseAtk    = instance.atk;
            int baseDef    = instance.def;
            int baseHpMax  = instance.hpMax;

            var equippedGear = _gearById.Values
                .Where(g => g.equipadoEn == heroId)
                .ToList();

            foreach (var gear in equippedGear)
                ApplyGearStats(instance, gear, baseAtk, baseDef, baseHpMax);

            return instance;
        }

        // ── Rolls de substats ──────────────────────────────────────────────────

        /// Distribuye triangular sesgada al mínimo:
        ///   u = Random.value; roll = max - (max - min) * Sqrt(1 - u)
        /// La media de una distribución así cae en: max - (max-min)/3 ≈ min + 2/3*(max-min)/2
        /// Con Sqrt(1-u), la moda está en el mínimo y la media ~1/3 del rango desde el min.
        private void RollSubstat(GearInstance gear)
        {
            gear.substats ??= new List<SubstatEntry>();

            var unrevealedIdxs = new List<int>();
            for (int i = 0; i < gear.substats.Count; i++)
                if (!gear.substats[i].revelado) unrevealedIdxs.Add(i);

            if (unrevealedIdxs.Count > 0)
            {
                // Revelar uno aleatorio
                int pick = unrevealedIdxs[UnityEngine.Random.Range(0, unrevealedIdxs.Count)];
                var entry = gear.substats[pick];
                var (min, max) = GetSubstatRange(gear, entry.statName);
                entry.valor    = TriangularRoll(min, max);
                entry.revelado = true;
                gear.substats[pick] = entry;
            }
            else if (gear.substats.Count > 0)
            {
                // Todos revelados — subir uno aleatorio
                int pick = UnityEngine.Random.Range(0, gear.substats.Count);
                var entry = gear.substats[pick];
                var (min, max) = GetSubstatRange(gear, entry.statName);
                entry.valor += TriangularRoll(min, max);
                gear.substats[pick] = entry;
            }
        }

        // ── Helpers de distribución ────────────────────────────────────────────

        /// Retorna un valor con distribución triangular sesgada hacia el mínimo.
        /// Fórmula: max - (max - min) * Sqrt(1 - u), donde u = Random.value
        public static int TriangularRoll(int min, int max)
        {
            float u    = UnityEngine.Random.value;
            float roll = max - (max - min) * Mathf.Sqrt(1f - u);
            return Mathf.Clamp(Mathf.RoundToInt(roll), min, max);
        }

        private (int min, int max) GetSubstatRange(GearInstance gear, string statName)
        {
            string key = statName.ToLower();

            // Intentar obtener del catálogo primero
            if (_catalogById.TryGetValue(gear.gearId, out var item) && item.subStatRanges != null)
            {
                if (item.subStatRanges.TryGetValue(key, out var range) && range != null && range.Length >= 2)
                    return (range[0], range[1]);
            }

            // Fallback a defaults
            if (SUBSTAT_DEFAULTS.TryGetValue(key, out var def))
                return def;

            return (2, 8); // default genérico
        }

        // ── Cálculo de mainStatValue ───────────────────────────────────────────

        private int CalculateMainStatValue(GearInstance gear)
        {
            if (!_catalogById.TryGetValue(gear.gearId, out var item)
                || item.mainStatRanges == null)
                return gear.mainStatValue;

            if (!item.mainStatRanges.TryGetValue(gear.mainStat.ToLower(), out var rarityMap)
                || rarityMap == null)
                return gear.mainStatValue;

            if (!rarityMap.TryGetValue(gear.rareza, out var range)
                || range == null || range.Length < 2)
                return gear.mainStatValue;

            float t = gear.nivel / 15f;
            return Mathf.RoundToInt(Mathf.Lerp(range[0], range[1], t));
        }

        // ── Aplicación de stats ────────────────────────────────────────────────

        private static void ApplyGearStats(HeroInstance h, GearInstance gear,
                                           int baseAtk, int baseDef, int baseHp)
        {
            // Stat principal
            ApplySingleStat(h, gear.mainStat, gear.mainStatValue, baseAtk, baseDef, baseHp);

            // Substats revelados
            if (gear.substats == null) return;
            foreach (var sub in gear.substats)
                if (sub.revelado)
                    ApplySingleStat(h, sub.statName, sub.valor, baseAtk, baseDef, baseHp);
        }

        private static void ApplySingleStat(HeroInstance h, string statName, int valor,
                                            int baseAtk, int baseDef, int baseHp)
        {
            if (string.IsNullOrEmpty(statName) || valor <= 0) return;

            switch (statName.ToLower())
            {
                // ── Stats absolutos ──────────────────────────────────────────
                case "atk":    h.atk    += valor; break;
                case "def":    h.def    += valor; break;
                case "spd":    h.spd    += valor; break;
                case "agi":    h.agi    += valor; break;
                case "luk":    h.luk    += valor; break;
                case "acc":    h.acc    += valor; break;
                case "res":    h.res    += valor; break;
                case "tcri":
                case "crit":   h.crit   += valor; break;
                case "dcri":
                case "critdmg":h.critDmg += valor; break;
                case "hp":
                    h.hpMax    += valor;
                    h.hpActual += valor;
                    break;

                // ── Stats porcentuales (aplican sobre base, no sobre total acumulado) ──
                case "atk%":
                    h.atk += Mathf.RoundToInt(baseAtk * valor / 100f);
                    break;
                case "def%":
                    h.def += Mathf.RoundToInt(baseDef * valor / 100f);
                    break;
                case "hp%":
                    int hpBonus = Mathf.RoundToInt(baseHp * valor / 100f);
                    h.hpMax    += hpBonus;
                    h.hpActual += hpBonus;
                    break;

                // ── Stats % directos (CRIT, CRITDMG, ACC, RES son porcentajes que suman) ──
                case "tcri%":  h.crit    += valor; break;
                case "dcri%":  h.critDmg += valor; break;
                case "acc%":   h.acc     += valor; break;
                case "res%":   h.res     += valor; break;
                case "luk%":   h.luk     += valor; break;
                case "agi%":   h.agi     += valor; break;
            }
        }

        // ── Carga de catálogo ──────────────────────────────────────────────────

        private void LoadGearCatalog()
        {
            var ta = Resources.Load<TextAsset>("Data/gear_catalog");
            if (ta == null)
            {
                Debug.LogError("[GearSystem] gear_catalog no encontrado en Resources/Data/");
                return;
            }

            var root = JsonConvert.DeserializeObject<GearCatalogRoot>(ta.text);
            if (root?.gear == null)
            {
                Debug.LogError("[GearSystem] gear_catalog.json vacío o malformado.");
                return;
            }

            _catalogById = new Dictionary<string, GearCatalogItem>();
            foreach (var item in root.gear)
                if (!string.IsNullOrEmpty(item.gearId))
                    _catalogById[item.gearId] = item;
        }

        // ── Construcción del registro desde PlayerData ─────────────────────────

        private void BuildRegistryFromPlayerData()
        {
            _gearById = new Dictionary<string, GearInstance>();

            var pd = _pds?.GetPlayerData();
            if (pd == null) return;

            // Gear en inventario general
            if (pd.gearInventory != null)
                foreach (var pg in pd.gearInventory)
                    RegisterFromPlayerGear(pg, equipadoEn: null);

            // Gear equipado en héroes
            if (pd.heroes != null)
                foreach (var hero in pd.heroes)
                    if (hero.equipment != null)
                        foreach (var pg in hero.equipment)
                            RegisterFromPlayerGear(pg, equipadoEn: hero.heroId);
        }

        private void RegisterFromPlayerGear(PlayerGearInstance pg, string equipadoEn)
        {
            if (pg == null || string.IsNullOrEmpty(pg.instanceId)) return;
            if (_gearById.ContainsKey(pg.instanceId)) return; // ya registrado

            string slotName = string.Empty;
            if (!string.IsNullOrEmpty(pg.gearId) && _catalogById.TryGetValue(pg.gearId, out var item))
                CATALOG_TYPE_TO_SLOT.TryGetValue(item.type ?? "", out slotName);

            var gear = new GearInstance
            {
                instanceId    = pg.instanceId,
                gearId        = pg.gearId,
                slot          = slotName,
                rareza        = pg.rarity,
                nivel         = pg.upgradeLevel,
                mainStat      = pg.mainStatType,
                mainStatValue = 0,
                substats      = new List<SubstatEntry>(),
                equipadoEn    = equipadoEn
            };

            if (pg.substats != null)
                foreach (var s in pg.substats)
                    gear.substats.Add(new SubstatEntry
                    {
                        statName = s.type,
                        valor    = (int)s.value,
                        revelado = true
                    });

            _gearById[gear.instanceId] = gear;
        }

        // ── Helpers de sincronización con PlayerData ───────────────────────────

        private void SyncGearInPlayerData(GearInstance gear)
        {
            var pd = _pds?.GetPlayerData();
            if (pd == null) return;

            var pg = ConvertToPlayerGear(gear);

            // Buscar y actualizar en inventario
            if (pd.gearInventory != null)
            {
                int idx = pd.gearInventory.FindIndex(g => g.instanceId == gear.instanceId);
                if (idx >= 0) { pd.gearInventory[idx] = pg; return; }
            }

            // Buscar y actualizar en equipo de héroes
            if (pd.heroes != null)
                foreach (var hero in pd.heroes)
                {
                    if (hero.equipment == null) continue;
                    int idx = hero.equipment.FindIndex(g => g.instanceId == gear.instanceId);
                    if (idx >= 0) { hero.equipment[idx] = pg; return; }
                }
        }

        private GearInstance FindEquippedGearInSlot(string heroId, string slotName)
            => _gearById.Values.FirstOrDefault(g =>
                g.equipadoEn == heroId &&
                g.slot       == slotName);

        // ── Conversión GearInstance ↔ PlayerGearInstance ───────────────────────

        private static PlayerGearInstance ConvertToPlayerGear(GearInstance gear)
        {
            var pg = new PlayerGearInstance
            {
                instanceId   = gear.instanceId,
                gearId       = gear.gearId,
                rarity       = gear.rareza,
                upgradeLevel = gear.nivel,
                mainStatType = gear.mainStat,
                substats     = new List<GearSubstat>()
            };

            if (gear.substats != null)
                foreach (var s in gear.substats)
                    if (s.revelado)
                        pg.substats.Add(new GearSubstat { type = s.statName, value = s.valor });

            return pg;
        }
    }
}
