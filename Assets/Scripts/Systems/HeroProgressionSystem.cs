using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;

namespace ReinoOscuridad.Systems
{
    /// Gestiona la progresión de héroes: subida de nivel, awaken y cálculo de stats.
    /// NUNCA escribe a Firestore — todo opera en memoria vía PlayerDataSystem.
    /// Los stats de gear y maestrías se aplican encima en GearSystem (S16).
    [DefaultExecutionOrder(-8)]
    public class HeroProgressionSystem : MonoBehaviour, ISystem
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        public static HeroProgressionSystem Instance { get; private set; }

        // ── Constantes ─────────────────────────────────────────────────────────

        private const int MAX_STARS = 6;

        // ── Catálogos en memoria ───────────────────────────────────────────────

        /// heroId → HeroData
        private Dictionary<string, HeroData> _heroes;

        /// level → xpRequired (XP para subir DE ese nivel AL siguiente)
        private Dictionary<int, int> _xpCurve;

        // ── Dependencias ───────────────────────────────────────────────────────

        private PlayerDataSystem _pds;

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
            if (_pds == null)
                Debug.LogError("[HeroProgression] PlayerDataSystem no encontrado.");

            LoadCatalogs();
            Debug.Log($"[HeroProgression] Inicializado — {_heroes?.Count ?? 0} héroes · {_xpCurve?.Count ?? 0} niveles en curva.");
        }

        public void OnSessionStart() { }
        public void OnSessionEnd()   { }

        // ── API pública — Stats ────────────────────────────────────────────────

        /// Construye una HeroInstance con stats finales para el nivel indicado.
        /// Fórmula: stat(N) = Lerp(statBase, statMax, t) donde t = (N-1)/(maxLevel-1).
        /// Gear y maestrías se aplican encima en GearSystem.
        /// Devuelve null con log de error si heroId no existe en el catálogo.
        public HeroInstance BuildHeroInstance(string heroId, int nivel)
        {
            if (_heroes == null)
            {
                Debug.LogError("[HeroProgression] Catálogo no cargado.");
                return null;
            }

            if (!_heroes.TryGetValue(heroId, out var data))
            {
                Debug.LogError($"[HeroProgression] heroId '{heroId}' no encontrado en catálogo.");
                return null;
            }

            nivel = Mathf.Clamp(nivel, data.baseLevel, data.maxLevel);

            float t = data.maxLevel > data.baseLevel
                ? Mathf.Clamp01((nivel - data.baseLevel) / (float)(data.maxLevel - data.baseLevel))
                : 0f;

            var s = data.stats;
            int hp = LerpStat(s.baseHP, s.maxHP, t);

            return new HeroInstance
            {
                heroId               = heroId,
                nivel                = nivel,
                hpMax                = hp,
                hpActual             = hp,
                atk                  = LerpStat(s.baseATK,  s.maxATK,  t),
                def                  = LerpStat(s.baseDEF,  s.maxDEF,  t),
                spd                  = LerpStat(s.baseSPD,  s.maxSPD,  t),
                agi                  = LerpStat(s.baseAGI,  s.maxAGI,  t),
                crit                 = LerpStat(s.baseTCRI, s.maxTCRI, t),
                critDmg              = LerpStat(s.baseDCRI, s.maxDCRI, t),
                acc                  = LerpStat(s.baseACC,  s.maxACC,  t),
                res                  = LerpStat(s.baseRES,  s.maxRES,  t),
                luk                  = LerpStat(s.baseLUK,  s.maxLUK,  t),
                elemento             = data.element,
                estaVivo             = true,
                efectosActivos       = new List<string>(),
                habilidadesEquipadas = new string[4]
            };
        }

        // ── API pública — Subida de nivel ──────────────────────────────────────

        /// Intenta subir un nivel al héroe si tiene XP suficiente.
        /// Retorna true si subió, false si no tiene XP o ya está al máximo.
        public bool TryLevelUp(string heroId)
        {
            var hero = FindHero(heroId);
            if (hero == null) return false;

            if (!_heroes.TryGetValue(heroId, out var data)) return false;

            if (hero.level >= data.maxLevel)
            {
                Debug.Log($"[HeroProgression] {heroId} ya está al nivel máximo ({data.maxLevel}).");
                return false;
            }

            if (!_xpCurve.TryGetValue(hero.level, out int xpNeeded) || xpNeeded <= 0)
            {
                Debug.LogWarning($"[HeroProgression] No se encontró XP requerida para nivel {hero.level}.");
                return false;
            }

            if (hero.exp < xpNeeded) return false;

            hero.exp   -= xpNeeded;
            hero.level += 1;
            _pds.MarkDirty();

            EventBus.Publish(new HeroLevelUpData { heroId = heroId, nuevoNivel = hero.level });
            Debug.Log($"[HeroProgression] {heroId} → nivel {hero.level} (XP restante: {hero.exp})");
            return true;
        }

        // ── API pública — XP ───────────────────────────────────────────────────

        /// Añade XP al héroe y dispara TryLevelUp automáticamente si supera el umbral.
        /// Puede ganar varios niveles de una vez.
        public void AddXP(string heroId, int cantidad)
        {
            if (cantidad <= 0) return;

            var hero = FindHero(heroId);
            if (hero == null) return;

            hero.exp += cantidad;
            _pds.MarkDirty();

            // Auto-levelup en bucle (puede ganar varios niveles)
            while (TryLevelUp(heroId)) { }
        }

        // ── API pública — Awaken ───────────────────────────────────────────────

        /// Intenta awaken (evolución de estrellas) del héroe.
        /// Requiere: nivel máximo + N copias del mismo héroe (N = estrellas actuales).
        /// Consume las copias del inventario. Máximo 6★.
        public bool TryAwaken(string heroId)
        {
            if (!_heroes.TryGetValue(heroId, out var data))
            {
                Debug.LogError($"[HeroProgression] TryAwaken: '{heroId}' no en catálogo.");
                return false;
            }

            var pd = _pds.GetPlayerData();
            if (pd?.heroes == null)
            {
                Debug.LogError("[HeroProgression] TryAwaken: PlayerData no disponible.");
                return false;
            }

            var mainHero = pd.heroes.Find(h => h.heroId == heroId);
            if (mainHero == null)
            {
                Debug.LogError($"[HeroProgression] TryAwaken: '{heroId}' no está en el roster.");
                return false;
            }

            if (mainHero.stars >= MAX_STARS)
            {
                Debug.Log($"[HeroProgression] {heroId} ya está en {MAX_STARS}★.");
                return false;
            }

            if (mainHero.level < data.maxLevel)
            {
                Debug.Log($"[HeroProgression] {heroId} necesita nivel máximo ({data.maxLevel}) para awaken. Nivel actual: {mainHero.level}.");
                return false;
            }

            int copiesNeeded    = mainHero.stars; // 1★→2★: 1, 2★→3★: 2, etc.
            int copiesAvailable = pd.heroes.Count(h => h.heroId == heroId) - 1;

            if (copiesAvailable < copiesNeeded)
            {
                Debug.Log($"[HeroProgression] {heroId} awaken {mainHero.stars}★→{mainHero.stars + 1}★ requiere {copiesNeeded} copias. Disponibles: {copiesAvailable}.");
                return false;
            }

            // Consume las copias (de atrás hacia delante, sin tocar mainHero)
            int consumed = 0;
            for (int i = pd.heroes.Count - 1; i >= 0 && consumed < copiesNeeded; i--)
            {
                if (pd.heroes[i].heroId == heroId && pd.heroes[i] != mainHero)
                {
                    pd.heroes.RemoveAt(i);
                    consumed++;
                }
            }

            mainHero.stars += 1;
            mainHero.awaken = true;
            _pds.MarkDirty();

            EventBus.Publish(new HeroAwakenedData { heroId = heroId, nuevasEstrellas = mainHero.stars });
            Debug.Log($"[HeroProgression] {heroId} awakened → {mainHero.stars}★");
            return true;
        }

        // ── API pública — Consultas ────────────────────────────────────────────

        /// Nivel actual del héroe en el roster del jugador. Devuelve 0 si no existe.
        public int GetNivel(string heroId)
            => FindHero(heroId)?.level ?? 0;

        /// Estrellas actuales. Devuelve 0 si el héroe no está en el roster.
        public int GetEstrellas(string heroId)
            => FindHero(heroId)?.stars ?? 0;

        /// Progreso de XP hacia el siguiente nivel (0–1). 1.0 en nivel máximo.
        public float GetXPProgress(string heroId)
        {
            var hero = FindHero(heroId);
            if (hero == null) return 0f;

            if (!_heroes.TryGetValue(heroId, out var data)) return 0f;
            if (hero.level >= data.maxLevel) return 1f;

            if (!_xpCurve.TryGetValue(hero.level, out int xpNeeded) || xpNeeded <= 0)
                return 0f;

            return Mathf.Clamp01(hero.exp / (float)xpNeeded);
        }

        // ── Carga de catálogos ─────────────────────────────────────────────────

        private void LoadCatalogs()
        {
            LoadHeroCatalog();
            LoadLevelCurve();
        }

        private void LoadHeroCatalog()
        {
            string path = Path.Combine(Application.dataPath, "Data", "hero_catalog.json");
            if (!File.Exists(path))
            {
                Debug.LogError($"[HeroProgression] hero_catalog.json no encontrado en: {path}");
                _heroes = new Dictionary<string, HeroData>();
                return;
            }

            var catalog = JsonConvert.DeserializeObject<HeroCatalog>(File.ReadAllText(path));
            _heroes = new Dictionary<string, HeroData>();

            if (catalog?.heroes == null)
            {
                Debug.LogError("[HeroProgression] hero_catalog.json vacío o malformado.");
                return;
            }

            foreach (var h in catalog.heroes)
            {
                if (!string.IsNullOrEmpty(h.heroId))
                    _heroes[h.heroId] = h;
            }
        }

        private void LoadLevelCurve()
        {
            string path = Path.Combine(Application.dataPath, "Data", "hero_level_curve.json");
            if (!File.Exists(path))
            {
                Debug.LogError($"[HeroProgression] hero_level_curve.json no encontrado en: {path}");
                _xpCurve = new Dictionary<int, int>();
                return;
            }

            var curveData = JsonConvert.DeserializeObject<LevelCurveData>(File.ReadAllText(path));
            _xpCurve = new Dictionary<int, int>();

            if (curveData?.curve == null)
            {
                Debug.LogError("[HeroProgression] hero_level_curve.json vacío o malformado.");
                return;
            }

            foreach (var entry in curveData.curve)
                _xpCurve[entry.level] = entry.xpRequired;
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private PlayerHeroData FindHero(string heroId)
        {
            var pd = _pds?.GetPlayerData();
            if (pd?.heroes == null)
            {
                Debug.LogError($"[HeroProgression] PlayerData no disponible para heroId '{heroId}'.");
                return null;
            }

            var hero = pd.heroes.Find(h => h.heroId == heroId);
            if (hero == null)
                Debug.LogError($"[HeroProgression] '{heroId}' no encontrado en el roster del jugador.");

            return hero;
        }

        /// Interpolación lineal entre dos stats enteros, redondeada a int.
        private static int LerpStat(int from, int to, float t)
            => Mathf.RoundToInt(from + (to - from) * t);
    }
}
