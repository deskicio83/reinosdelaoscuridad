using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;

namespace ReinoOscuridad.Systems
{
    /// Gestiona la XP y nivel del jugador, los desbloqueos por nivel
    /// y los puntos del Pase Oscuro.
    /// NUNCA escribe a Firestore — todo opera en memoria vía PlayerDataSystem.
    ///
    /// Curva XP: xpRequired(N) = RoundToInt(baseExp * growth^(N-1))
    /// Mismos parámetros que LevelCurveData — reutiliza el modelo existente.
    [DefaultExecutionOrder(-5)]
    public class PlayerProgressionSystem : MonoBehaviour, ISystem
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        public static PlayerProgressionSystem Instance { get; private set; }

        // ── Constantes ─────────────────────────────────────────────────────────

        /// Puntos de Pase Oscuro necesarios para subir un nivel de Pase.
        private const int PASE_POINTS_PER_NIVEL = 1000;

        // ── Tabla de desbloqueos por nivel ─────────────────────────────────────

        private static readonly Dictionary<int, string[]> DESBLOQUEOS = new()
        {
            { 2,  new[] { "gacha" } },
            { 3,  new[] { "arena" } },
            { 5,  new[] { "clan" } },
            { 7,  new[] { "conjuros" } },
            { 10, new[] { "torre_normal", "torre_dificil" } },
            { 15, new[] { "mazmorra" } },
            { 20, new[] { "world_boss" } },
            { 25, new[] { "pase_oscuro" } },
            { 30, new[] { "altar_corrupcion" } }
        };

        // ── Catálogo en memoria ────────────────────────────────────────────────

        private LevelCurveData _curve;

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

            if (_pds == null)
                Debug.LogError("[PlayerProgression] PlayerDataSystem no encontrado.");

            LoadLevelCurve();
            EnsurePaseOscuroData();
            SubscribeToEvents();

            Debug.Log($"[PlayerProgression] Inicializado — nivel jugador: {GetPlayerNivel()} · curva cargada: {_curve != null} · maxLevel: {_curve?.maxLevel}");
        }

        public void OnSessionStart() { }
        public void OnSessionEnd()   { }

        // ── API pública — XP del jugador ───────────────────────────────────────

        /// Añade XP al jugador. Sube de nivel automáticamente si supera el umbral.
        /// Puede ganar varios niveles de una vez.
        public void AddPlayerXP(int cantidad)
        {
            if (cantidad <= 0) return;

            var pd = _pds?.GetPlayerData();
            if (pd == null) return;

            int maxNivel = _curve?.maxLevel ?? 100;
            if (pd.playerLevel >= maxNivel) return;

            pd.playerXP += cantidad;

            while (pd.playerLevel < maxNivel)
            {
                int xpNeeded = GetXPRequired(pd.playerLevel);
                if (pd.playerXP < xpNeeded) break;

                pd.playerXP   -= xpNeeded;
                int prevLevel  = pd.playerLevel;
                pd.playerLevel += 1;

                var desbloqueos = CheckDesbloqueos(pd.playerLevel);
                UpdateEnergyMax(pd);

                EventBus.Publish(new PlayerLevelUpData
                {
                    previousLevel = prevLevel,
                    newLevel      = pd.playerLevel,
                    nuevoNivel    = pd.playerLevel,
                    desbloqueos   = desbloqueos.ToArray()
                });

                Debug.Log($"[PlayerProgression] Jugador nivel {pd.playerLevel} " +
                          $"(XP restante: {pd.playerXP})" +
                          (desbloqueos.Count > 0 ? $" · Desbloqueos: {string.Join(", ", desbloqueos)}" : ""));
            }

            _pds.MarkDirty();
        }

        // ── API pública — Consultas de XP ──────────────────────────────────────

        /// Nivel actual del jugador.
        public int GetPlayerNivel()
            => _pds?.GetPlayerData()?.playerLevel ?? 1;

        /// XP actual acumulada dentro del nivel corriente.
        public int GetPlayerXPActual()
            => _pds?.GetPlayerData()?.playerXP ?? 0;

        /// XP total requerida para pasar del nivel actual al siguiente.
        public int GetPlayerXPParaSiguiente()
            => GetXPRequired(GetPlayerNivel());

        /// Progreso de XP dentro del nivel actual (0–1). 1.0 si en nivel máximo.
        public float GetPlayerXPProgress()
        {
            var pd = _pds?.GetPlayerData();
            if (pd == null) return 0f;

            int maxNivel = _curve?.maxLevel ?? 100;
            if (pd.playerLevel >= maxNivel) return 1f;

            int xpNeeded = GetXPRequired(pd.playerLevel);
            if (xpNeeded <= 0) return 0f;

            return Mathf.Clamp01(pd.playerXP / (float)xpNeeded);
        }

        // ── API pública — Desbloqueos ──────────────────────────────────────────

        /// true si la feature está desbloqueada para el nivel actual del jugador.
        public bool IsFeatureUnlocked(string featureId)
        {
            int nivel = GetPlayerNivel();
            foreach (var kvp in DESBLOQUEOS)
                if (kvp.Key <= nivel && Array.Exists(kvp.Value, f => f == featureId))
                    return true;
            return false;
        }

        /// Lista de features desbloqueadas exactamente en ese nivel.
        /// Devuelve lista vacía si no hay desbloqueos en ese nivel.
        public List<string> GetFeaturesUnlockedAt(int nivel)
            => DESBLOQUEOS.TryGetValue(nivel, out var features)
               ? new List<string>(features)
               : new List<string>();

        // ── API pública — Pase Oscuro ──────────────────────────────────────────

        /// Añade puntos al Pase Oscuro. Sube de nivel si supera el umbral.
        public void AddPasePoints(int cantidad)
        {
            if (cantidad <= 0) return;

            var pd = _pds?.GetPlayerData();
            if (pd == null) return;

            EnsurePaseOscuroData();

            pd.paseOscuro.puntosActuales += cantidad;

            while (pd.paseOscuro.puntosActuales >= PASE_POINTS_PER_NIVEL)
            {
                pd.paseOscuro.puntosActuales -= PASE_POINTS_PER_NIVEL;
                pd.paseOscuro.nivelActual    += 1;

                EventBus.Publish(new PaseLevelUpData
                {
                    nuevoNivel      = pd.paseOscuro.nivelActual,
                    esCarrilPremium = pd.paseOscuro.tienePasePremium
                });

                Debug.Log($"[PlayerProgression] Pase Oscuro → nivel {pd.paseOscuro.nivelActual}");
            }

            _pds.MarkDirty();
        }

        /// Nivel actual del Pase Oscuro del jugador.
        public int GetPaseNivel()
        {
            var pd = _pds?.GetPlayerData();
            return pd?.paseOscuro?.nivelActual ?? 0;
        }

        /// true si el jugador tiene el carril premium del Pase Oscuro activo.
        public bool TienePasePremium()
        {
            var pd = _pds?.GetPlayerData();
            return pd?.paseOscuro?.tienePasePremium ?? false;
        }

        // ── Desbloqueos internos ───────────────────────────────────────────────

        /// Devuelve las features desbloqueadas al alcanzar ese nivel.
        private List<string> CheckDesbloqueos(int nivel)
            => GetFeaturesUnlockedAt(nivel);

        // ── Energía máxima por nivel ───────────────────────────────────────────

        /// Actualiza energiaMax en PlayerData según el nivel alcanzado.
        /// EconomySystem.MaxEnergy se sincronizará en la próxima sesión.
        /// Tabla: L1-9→60, L10-19→70, L20-29→80, L30+→100
        private static void UpdateEnergyMax(PlayerData pd)
        {
            int newMax = pd.playerLevel switch
            {
                >= 30 => 100,
                >= 20 => 80,
                >= 10 => 70,
                _      => 60
            };

            if (pd.energiaMax != newMax)
            {
                pd.energiaMax = newMax;
                Debug.Log($"[PlayerProgression] energiaMax actualizado a {newMax} (nivel {pd.playerLevel}).");
            }
        }

        // ── Carga de la curva de nivel ─────────────────────────────────────────

        private void LoadLevelCurve()
        {
            string path = Path.Combine(Application.dataPath, "Data", "player_level_curve.json");
            if (!File.Exists(path))
            {
                Debug.LogError($"[PlayerProgression] player_level_curve.json no encontrado: {path}");
                _curve = null;
                return;
            }

            _curve = JsonConvert.DeserializeObject<LevelCurveData>(File.ReadAllText(path));
            if (_curve == null)
                Debug.LogError("[PlayerProgression] player_level_curve.json vacío o malformado.");
        }

        // ── Suscripción a eventos ──────────────────────────────────────────────

        private void SubscribeToEvents()
        {
            EventBus.OnCombatCompleted  += OnCombatCompleted;
            EventBus.OnMissionCompleted += OnMissionCompleted;
        }

        private void OnDestroy()
        {
            EventBus.OnCombatCompleted  -= OnCombatCompleted;
            EventBus.OnMissionCompleted -= OnMissionCompleted;
        }

        private void OnCombatCompleted(CombatCompletedData data)
        {
            if (data.expGained > 0)
                AddPlayerXP(data.expGained);
        }

        private void OnMissionCompleted(MissionCompletedData data)
        {
            if (data.pasePoints > 0)
                AddPasePoints(data.pasePoints);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        /// XP requerida para subir del nivel N al nivel N+1.
        private int GetXPRequired(int level)
        {
            if (_curve == null) return 9999;
            return Mathf.RoundToInt(_curve.baseExp * Mathf.Pow(_curve.growth, level - 1));
        }

        /// Inicializa PaseOscuroData si es null (partidas antiguas o datos vacíos).
        private void EnsurePaseOscuroData()
        {
            var pd = _pds?.GetPlayerData();
            if (pd == null) return;
            pd.paseOscuro ??= new PaseOscuroData();
        }
    }
}
