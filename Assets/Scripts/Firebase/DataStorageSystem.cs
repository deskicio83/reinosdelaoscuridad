// REGLA: máximo 1 read al arrancar, 3-5 writes por sesión típica.
// NUNCA llamar LoadPlayerDataFromFirestore fuera de BootScene.
// NUNCA llamar SavePlayerDataToFirestore en Update() o coroutines periódicas.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;
using ReinoOscuridad.Systems;

namespace ReinoOscuridad.Firebase
{
    /// Único punto de acceso a Firestore en el proyecto.
    /// — 1 read por sesión (BootScene llama LoadPlayerDataFromFirestore)
    /// — Writes solo en checkpoints definidos (combat, gacha, cierre de sesión)
    /// Ningún otro sistema toca Firestore directamente.
    [DefaultExecutionOrder(-15)]
    public class DataStorageSystem : MonoBehaviour, ISystem
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        public static DataStorageSystem Instance { get; private set; }

        // ── Constantes ─────────────────────────────────────────────────────────

        private const string PLAYERS_COLLECTION = "players";

        // ── Referencias internas ──────────────────────────────────────────────

        private FirebaseFirestore _db;
        private PlayerDataSystem  _pds;

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

            try
            {
                _db = FirebaseFirestore.DefaultInstance;
                Debug.Log("[DataStorageSystem] Firestore inicializado.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DataStorageSystem] Firestore no disponible en Initialize(): {e.Message}. Se inicializará en BootScene.");
            }
        }

        public void OnSessionStart() { }

        public async void OnSessionEnd()
        {
            if (_pds != null && _pds.HasPendingChanges)
            {
                Debug.Log("[DataStorageSystem] OnSessionEnd — guardando cambios pendientes.");
                await SavePlayerDataToFirestore();
            }
        }

        // ── CARGA INICIAL (1 read por sesión) ─────────────────────────────────

        /// Lee el documento del jugador desde Firestore y actualiza PlayerDataSystem.
        /// Llamar SOLO desde BootScene, una vez por sesión.
        /// Si el documento no existe o hay error de red, mantiene datos locales (modo offline).
        public async Task LoadPlayerDataFromFirestore(string uid)
        {
            if (!ValidateFirestore("LoadPlayerDataFromFirestore")) return;
            if (string.IsNullOrEmpty(uid))
            {
                Debug.LogWarning("[DataStorageSystem] LoadPlayerDataFromFirestore: uid vacío, abortando.");
                return;
            }

            try
            {
                Debug.Log($"[DataStorageSystem] Cargando datos de Firestore — uid: {uid}");
                var docRef  = _db.Collection(PLAYERS_COLLECTION).Document(uid);
                var snapshot = await docRef.GetSnapshotAsync();

                if (!snapshot.Exists)
                {
                    Debug.Log("[DataStorageSystem] Documento no encontrado — jugador nuevo. Manteniendo datos locales.");
                    return;
                }

                // Convertir el snapshot a JSON y deserializar con Newtonsoft
                // para respetar Dictionary<string,T> y tipos anulables.
                string json      = JsonConvert.SerializeObject(snapshot.ToDictionary());
                var playerData   = JsonConvert.DeserializeObject<PlayerData>(json);

                if (playerData == null)
                {
                    Debug.LogWarning("[DataStorageSystem] Error al deserializar datos de Firestore. Manteniendo datos locales.");
                    return;
                }

                // UpdatePlayerData marca dirty internamente — lo limpiamos
                // porque acabamos de cargar desde la fuente de verdad.
                _pds.UpdatePlayerData(playerData);
                _pds.ClearDirty();

                Debug.Log($"[DataStorageSystem] Datos cargados desde Firestore — jugador: {playerData.playerName}, nivel: {playerData.playerLevel}");
            }
            catch (Exception e)
            {
                // Error de red u otro error — modo offline, datos locales intactos.
                Debug.LogWarning($"[DataStorageSystem] LoadPlayerDataFromFirestore falló (modo offline): {e.Message}");
            }
        }

        // ── GUARDADO EN CHECKPOINTS ────────────────────────────────────────────

        /// Persiste el PlayerData actual en Firestore.
        /// Solo ejecuta si HasPendingChanges es true.
        /// Llamar solo en checkpoints: combate completado, gacha x10, cierre de sesión.
        public async Task SavePlayerDataToFirestore()
        {
            if (_pds == null || !_pds.HasPendingChanges)
            {
                Debug.Log("[DataStorageSystem] SavePlayerDataToFirestore: sin cambios pendientes, skip.");
                return;
            }

            if (!ValidateFirestore("SavePlayerDataToFirestore")) return;

            var playerData = _pds.GetPlayerData();
            string uid = playerData?.uid ?? string.Empty;

            if (string.IsNullOrEmpty(uid))
            {
                Debug.LogWarning("[DataStorageSystem] SavePlayerDataToFirestore: uid vacío, abortando.");
                return;
            }

            try
            {
                Debug.Log($"[DataStorageSystem] Guardando en Firestore — uid: {uid}");

                // Serializar con Newtonsoft → convertir a tipos nativos C# para Firestore.
                // SetAsync no entiende JArray/JObject de Newtonsoft — hay que convertirlos
                // a List<object>/Dictionary<string,object> o Firestore lanza "Nested arrays".
                string json    = JsonConvert.SerializeObject(playerData);
                var rawDict    = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                var dict       = ToFirestoreDict(rawDict);

                var docRef = _db.Collection(PLAYERS_COLLECTION).Document(uid);
                await docRef.SetAsync(dict);

                _pds.ClearDirty();
                Debug.Log("[DataStorageSystem] Datos guardados en Firestore correctamente.");
            }
            catch (Exception e)
            {
                // NO reintentar automáticamente — el siguiente checkpoint intentará de nuevo.
                Debug.LogError($"[DataStorageSystem] SavePlayerDataToFirestore falló: {e.Message}");
            }
        }

        // ── Conversión Newtonsoft → tipos nativos para Firestore ──────────────

        /// Convierte recursivamente JObject/JArray a Dictionary/List nativos de C#
        /// para que el SDK de Firestore pueda serializar correctamente la jerarquía.
        private static Dictionary<string, object> ToFirestoreDict(Dictionary<string, object> src)
        {
            if (src == null) return null;
            var result = new Dictionary<string, object>(src.Count);
            foreach (var kvp in src)
                result[kvp.Key] = ToFirestoreValue(kvp.Value);
            return result;
        }

        private static object ToFirestoreValue(object value)
        {
            switch (value)
            {
                case JObject jObj:
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in jObj.Properties())
                        dict[prop.Name] = ToFirestoreValue(prop.Value);
                    return dict;
                }
                case JArray jArr:
                {
                    var list = new List<object>(jArr.Count);
                    foreach (var item in jArr)
                        list.Add(ToFirestoreValue(item));
                    return list;
                }
                case JValue jVal:
                    return jVal.Value;
                default:
                    return value;
            }
        }

        // ── Helper privado ────────────────────────────────────────────────────

        private bool ValidateFirestore(string caller)
        {
            if (_db != null) return true;

            try
            {
                _db = FirebaseFirestore.DefaultInstance;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DataStorageSystem] {caller}: Firestore no disponible — {e.Message}");
                return false;
            }
        }
    }
}
