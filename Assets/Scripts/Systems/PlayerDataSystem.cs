using System.IO;
using UnityEngine;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;

namespace ReinoOscuridad.Systems
{
    /// Singleton DontDestroyOnLoad. Fuente de verdad del jugador en memoria.
    /// Ningún sistema lee o escribe Firestore directamente — todo pasa por aquí.
    /// Carga desde JSON local en Initialize(). Firestore se integra en S07.
    public class PlayerDataSystem : MonoBehaviour, ISystem
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        public static PlayerDataSystem Instance { get; private set; }

        // ── Estado interno ─────────────────────────────────────────────────────

        private PlayerData _playerData;
        private bool _hasPendingChanges;

        /// true si hay cambios en memoria que aún no se han persistido.
        public bool HasPendingChanges => _hasPendingChanges;

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

            // GameManager corre con DefaultExecutionOrder(-100), así que
            // Instance ya existe aquí con order por defecto (0).
            GameManager.Instance.RegisterSystem(this);
        }

        // ── ISystem ───────────────────────────────────────────────────────────

        public void Initialize()
        {
            LoadFromLocalJson();
        }

        public void OnSessionStart()
        {
            // PlayerDataSystem no necesita acción adicional en SessionStart.
            // Los sistemas que dependen de los datos del jugador se suscriben
            // al EventBus y se actualizan cuando PublishSessionReady() se llame.
        }

        public void OnSessionEnd()
        {
            if (_hasPendingChanges)
                Debug.LogWarning("[PlayerDataSystem] OnSessionEnd — hay cambios pendientes sin guardar. DataStorageSystem debería haberlos persistido.");
            else
                Debug.Log("[PlayerDataSystem] OnSessionEnd — sin cambios pendientes.");
        }

        // ── API pública ───────────────────────────────────────────────────────

        /// Devuelve los datos del jugador en memoria. Nunca null tras Initialize().
        public PlayerData GetPlayerData() => _playerData;

        /// Reemplaza los datos en memoria. NO escribe a Firestore.
        /// Llama a MarkDirty() automáticamente.
        public void UpdatePlayerData(PlayerData updated)
        {
            _playerData = updated;
            MarkDirty();
        }

        /// Señala que hay cambios en memoria pendientes de guardar.
        /// El guardado real lo ejecuta DataStorageSystem en el checkpoint adecuado.
        public void MarkDirty()
        {
            _hasPendingChanges = true;
        }

        /// Confirma que los datos han sido persistidos. Llamado por DataStorageSystem
        /// tras completar una escritura exitosa a Firestore.
        public void ClearDirty()
        {
            _hasPendingChanges = false;
        }

        // ── Carga local (temporal hasta S07 — será reemplazado por Firestore) ─

        private void LoadFromLocalJson()
        {
            // Application.dataPath apunta a Assets/ en el Editor.
            // En builds mobile, Firestore reemplazará este bloque (S07).
            string path = Path.Combine(Application.dataPath, "Data", "player_data.json");

            if (!File.Exists(path))
            {
                Debug.LogError($"[PlayerDataSystem] No se encontró player_data.json en: {path}");
                _playerData = CreateEmptyPlayerData();
                return;
            }

            string json = File.ReadAllText(path);
            _playerData = JsonUtility.FromJson<PlayerData>(json);

            if (_playerData == null)
            {
                Debug.LogError("[PlayerDataSystem] Error al deserializar player_data.json. Usando datos vacíos.");
                _playerData = CreateEmptyPlayerData();
                return;
            }

            Debug.Log($"[PlayerDataSystem] Datos cargados — jugador: {_playerData.playerName}, nivel: {_playerData.playerLevel}");
            _hasPendingChanges = false;
        }

        private static PlayerData CreateEmptyPlayerData()
        {
            return new PlayerData
            {
                playerName    = "Nuevo Jugador",
                playerLevel   = 1,
                energia       = 60,
                energiaMax    = 60,
                oroNegro      = 0,
                caosifera     = 0,
                resources     = new PlayerResources(),
                heroes        = new System.Collections.Generic.List<PlayerHeroData>(),
                gearInventory = new System.Collections.Generic.List<PlayerGearInstance>(),
                equipment     = new System.Collections.Generic.List<PlayerGearInstance>(),
                artifactInventory = new System.Collections.Generic.List<PlayerArtifactInstance>(),
                awakenInventory   = new AwakenInventory
                {
                    items = new System.Collections.Generic.Dictionary<string, int>()
                },
                maxHeroSpaces = 20
            };
        }
    }
}
