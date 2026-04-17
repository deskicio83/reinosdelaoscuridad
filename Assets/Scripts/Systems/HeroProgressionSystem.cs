using System.Collections.Generic;
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
    ///
    /// Curva de XP: xpRequired(N) = RoundToInt(baseExp * growth^(N-1))
    /// Cap de nivel por estrellas: 3★→30 · 4★→40 · 5★→50 · 6★→60
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

        /// Datos de la curva de nivel (formula + caps por estrellas)
        private LevelCurveData _curve;

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

            int heroCount  = _heroes?.Count ?? 0;
            int curveValid = _curve != null ? 1 : 0;
            Debug.Log($"[HeroProgression] Inicializado — {heroCount} héroes · curva cargada: {curveValid == 1} · maxLevel: {_curve?.maxLevel}");
        }

        public void OnSessionStart() { }
        public void OnSessionEnd()   { }

        // ── API pública — Stats ────────────────────────────────────────────────

        /// Construye una HeroInstance con los stats finales para el nivel indicado.
        /// Fórmula: stat(N) = Lerp(statBase, statMax, t)
        ///   donde t = (nivel - 1) / (maxLevel - 1), usando el maxLevel absoluto (60).
        /// Gear y maestrías se aplican encima en GearSystem (S16).
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

            int absMax = _curve?.maxLevel ?? data.maxLevel;
            nivel = Mathf.Clamp(nivel, data.baseLevel, absMax);

            float t = absMax > data.baseLevel
                ? Mathf.Clamp01((nivel - data.baseLevel) / (float)(absMax - data.baseLevel))
                : 0f;

            var s  = data.stats;
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
        /// El nivel máximo depende de las estrellas actuales (3★→30, 4★→40…).
        /// Retorna false si ya está al cap de estrellas o sin XP suficiente.
        public bool TryLevelUp(string heroId)
        {
            var hero = FindHero(heroId);
            if (hero == null) return false;

            int cap      = GetStarsCap(hero.stars);
            int xpNeeded = GetXPRequired(hero.level);

            if (hero.level >= cap)
            {
                Debug.Log($"[HeroProgression] {heroId} en nivel cap para {hero.stars}★ ({cap}).");
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

            while (TryLevelUp(heroId)) { }
        }

        // ── API pública — Awaken ───────────────────────────────────────────────

        /// Intenta awaken (evolución de estrellas) del héroe.
        /// Requiere: estar en el nivel cap para las estrellas actuales
        ///   + N copias del mismo héroe (N = estrellas actuales).
        /// Consume copias del roster. Máximo 6★.
        public bool TryAwaken(string heroId)
        {
            if (!_heroes.TryGetValue(heroId, out _))
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
                Debug.LogError($"[HeroProgression] TryAwaken: '{heroId}' no en el roster.");
                return false;
            }

            if (mainHero.stars >= MAX_STARS)
            {
                Debug.Log($"[HeroProgression] {heroId} ya en {MAX_STARS}★.");
                return false;
            }

            int cap = GetStarsCap(mainHero.stars);
            if (mainHero.level < cap)
            {
                Debug.Log($"[HeroProgression] {heroId} necesita nivel {cap} para awaken (actual: {mainHero.level}).");
                return false;
            }

            int copiesNeeded    = mainHero.stars; // 3★→4★: 3, 4★→5★: 4, etc.
            int copiesAvailable = pd.heroes.Count(h => h.heroId == heroId) - 1;

            if (copiesAvailable < copiesNeeded)
            {
                Debug.Log($"[HeroProgression] {heroId} awaken {mainHero.stars}★→{mainHero.stars + 1}★ requiere {copiesNeeded} copias. Disponibles: {copiesAvailable}.");
                return false;
            }

            // Consume copias (de atrás hacia delante, sin tocar mainHero)
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
            Debug.Log($"[HeroProgression] {heroId} awakened → {mainHero.stars}★ (cap ahora: {GetStarsCap(mainHero.stars)})");
            return true;
        }

        // ── API pública — Consultas ────────────────────────────────────────────

        /// Nivel actual del héroe en el roster. Devuelve 0 si no existe.
        public int GetNivel(string heroId)
            => FindHero(heroId)?.level ?? 0;

        /// Estrellas actuales. Devuelve 0 si no existe.
        public int GetEstrellas(string heroId)
            => FindHero(heroId)?.stars ?? 0;

        /// Progreso XP hacia el siguiente nivel (0–1). 1.0 si en nivel cap actual.
        public float GetXPProgress(string heroId)
        {
            var hero = FindHero(heroId);
            if (hero == null) return 0f;

            int cap = GetStarsCap(hero.stars);
            if (hero.level >= cap) return 1f;

            int xpNeeded = GetXPRequired(hero.level);
            if (xpNeeded <= 0) return 0f;

            return Mathf.Clamp01(hero.exp / (float)xpNeeded);
        }

        // ── Helpers de curva ───────────────────────────────────────────────────

        /// XP requerida para subir DEL nivel N AL nivel N+1.
        /// Fórmula: RoundToInt(baseExp * growth^(level-1))
        private int GetXPRequired(int level)
        {
            if (_curve == null) return 9999;
            return Mathf.RoundToInt(_curve.baseExp * Mathf.Pow(_curve.growth, level - 1));
        }

        /// Nivel máximo que puede alcanzar un héroe según sus estrellas actuales.
        private int GetStarsCap(int stars)
        {
            if (_curve?.maxLevelPerStars == null) return _curve?.maxLevel ?? 60;
            string key = stars.ToString();
            return _curve.maxLevelPerStars.TryGetValue(key, out int cap)
                ? cap
                : (_curve.maxLevel);
        }

        // ── Carga de catálogos ─────────────────────────────────────────────────

        private void LoadCatalogs()
        {
            LoadHeroCatalog();
            LoadLevelCurve();
        }

        private void LoadHeroCatalog()
        {
            var ta = Resources.Load<TextAsset>("Data/hero_catalog");
            if (ta == null)
            {
                Debug.LogError("[HeroProgression] hero_catalog no encontrado en Resources/Data/");
                _heroes = new Dictionary<string, HeroData>();
                return;
            }

            var catalog = JsonConvert.DeserializeObject<HeroCatalog>(ta.text);
            _heroes = new Dictionary<string, HeroData>();

            if (catalog?.heroes == null)
            {
                Debug.LogError("[HeroProgression] hero_catalog.json vacío o malformado.");
                return;
            }

            foreach (var h in catalog.heroes)
                if (!string.IsNullOrEmpty(h.heroId))
                    _heroes[h.heroId] = h;
        }

        private void LoadLevelCurve()
        {
            var ta = Resources.Load<TextAsset>("Data/hero_level_curve");
            if (ta == null)
            {
                Debug.LogError("[HeroProgression] hero_level_curve no encontrado en Resources/Data/");
                _curve = null;
                return;
            }

            _curve = JsonConvert.DeserializeObject<LevelCurveData>(ta.text);
            if (_curve == null)
                Debug.LogError("[HeroProgression] hero_level_curve.json vacío o malformado.");
        }

        // ── Helpers privados ───────────────────────────────────────────────────

        private PlayerHeroData FindHero(string heroId)
        {
            var pd = _pds?.GetPlayerData();
            if (pd?.heroes == null)
            {
                Debug.LogError($"[HeroProgression] PlayerData no disponible para '{heroId}'.");
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
