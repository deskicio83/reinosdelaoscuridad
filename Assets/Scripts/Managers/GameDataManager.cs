/*
============================================================
GameDataManager.cs — Acceso centralizado a datos del jugador
------------------------------------------------------------
PROPÓSITO
- Singleton que expone PlayerData y utilidades de guardado.

USO
- GameDataManager.Instance.PlayerData para mutaciones.

MÉTODOS (COMPLETA AQUÍ)
- LoadPlayerData()/SavePlayerData().
- ResetProgress() (si aplica).
============================================================
*/


using UnityEngine;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;


public class GameDataManager : MonoBehaviour
{
    public static GameDataManager Instance { get; private set; }

    private PlayerData playerData;
    public PlayerData PlayerData => playerData;

    [Header("Ruta archivo player data (relative to persistentDataPath)")]
    public string playerDataJsonPath = "Data/player_data.json";

    // PlayerPrefs key para detectar builds distintos
    private const string PD_BUILD_GUID_KEY = "PD_BUILD_GUID";
    // Ruta en Resources del default embebido en el APK
    private const string DEFAULT_PD_RES_PRIMARY = "Data/player_data_default"; // Assets/Resources/Data/player_data_default.json
    private const string DEFAULT_PD_RES_FALLBACK = "Data/player_data";        // Assets/Resources/Data/player_data.json (tu flujo actual)

    private string PlayerFullPath => Path.Combine(Application.persistentDataPath, playerDataJsonPath);
    private string PlayerDir      => Path.GetDirectoryName(PlayerFullPath);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 1) Si el build ha cambiado, copiamos el default del APK sobre el save persistente
        EnsureFreshDefaultsIfBuildChanged();

        // 2) Cargamos el PlayerData (leerá del persistente si existe)
        CargarPlayerData();
    }
    // Ruta absoluta al JSON de PlayerData (pública para verificaciones y utilidades)
    public string GetPlayerDataPath()
    {
        return System.IO.Path.Combine(Application.persistentDataPath, playerDataJsonPath);
    }


    /// <summary>
    /// Si el GUID del build ha cambiado (o no hay save), fuerza copiar el default del APK.
    /// </summary>
    public void EnsureFreshDefaultsIfBuildChanged()
    {
        // GUID único por build; Unity lo genera en cada build aunque no cambies el "Version"
        string currentGuid = Application.buildGUID;
        string lastGuid = PlayerPrefs.GetString(PD_BUILD_GUID_KEY, "");

        // Si no hay archivo aún, o es otro build → sobreescribe con el default embebido
        if (!File.Exists(PlayerFullPath) || lastGuid != currentGuid)
        {
            Debug.Log($"[GameDataManager] Build GUID cambió '{lastGuid}' → '{currentGuid}'. Copiando default del APK.");
            OverwriteSaveWithBundledDefault();
            PlayerPrefs.SetString(PD_BUILD_GUID_KEY, currentGuid);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Sobrescribe el save persistente con el default embebido en Resources.
    /// </summary>
    public void OverwriteSaveWithBundledDefault()
    {
        // Prioridad al nuevo player_data_default.json; si no existe, usa player_data.json (tu fallback actual).
        TextAsset jsonAsset = Resources.Load<TextAsset>(DEFAULT_PD_RES_PRIMARY);
        if (jsonAsset == null)
            jsonAsset = Resources.Load<TextAsset>(DEFAULT_PD_RES_FALLBACK);

        if (jsonAsset == null)
        {
            Debug.LogWarning($"[GameDataManager] No se encontró {DEFAULT_PD_RES_PRIMARY}.json ni {DEFAULT_PD_RES_FALLBACK}.json en Resources. No se sobreescribe el save.");
            return;
        }

        try
        {
            if (!Directory.Exists(PlayerDir))
                Directory.CreateDirectory(PlayerDir);

            File.WriteAllText(PlayerFullPath, jsonAsset.text, Encoding.UTF8);
            Debug.Log($"[GameDataManager] Default del APK escrito en: {PlayerFullPath}");

            // Opcional: precargar en memoria la instancia PlayerData con el default
            var fresh = JsonConvert.DeserializeObject<PlayerData>(jsonAsset.text) ?? new PlayerData();
            SanitizeAfterLoad(fresh);
            playerData = fresh;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameDataManager] Error escribiendo default del APK: {ex}");
        }
    }

    // === REEMPLAZAR COMPLETO ===
    public void CargarPlayerData()
    {
        try
        {
            // Usa las utilidades ya presentes en este fichero
            string path = PlayerFullPath;

            if (!File.Exists(path))
            {
                // Si no existe, deja lo que ya haya en memoria (posible default de Resources)
                if (playerData == null)
                    playerData = new PlayerData();

                SanitizeAfterLoad(playerData);
                playerData.NormalizeAfterLoad();

                // Guarda un archivo inicial para futuras cargas
                SavePlayerData(playerData);
                Debug.Log($"[GameDataManager] player_data.json no existía. Creado en: {path}");
                return;
            }

            string json = File.ReadAllText(path, Encoding.UTF8);
            var loaded = JsonConvert.DeserializeObject<PlayerData>(json) ?? new PlayerData();

            // Normalización mínima sin pisar datos del JSON
            SanitizeAfterLoad(loaded);
            loaded.NormalizeAfterLoad();

            // Asignar al campo (la propiedad PlayerData es de solo lectura)
            playerData = loaded;

            int invCount = (playerData?.awakenInventory?.items != null) ? playerData.awakenInventory.items.Count : 0;
            Debug.Log($"[GameDataManager] PlayerData cargado desde disco: {path}. awakenInventory={invCount}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameDataManager] Error al cargar PlayerData: {ex}");
            // Fallback seguro en memoria
            if (playerData == null) playerData = new PlayerData();
            SanitizeAfterLoad(playerData);
            playerData.NormalizeAfterLoad();
        }
    }

    private void SanitizeAfterLoad(PlayerData pd)
    {
        if (pd == null) return;
        if (pd.awakenInventory == null) pd.awakenInventory = new AwakenInventory();
        if (pd.awakenInventory.items == null) pd.awakenInventory.items = new Dictionary<string, int>();
        if (pd.heroes == null) pd.heroes = new List<HeroProgress>();
    }

    // GameDataManager.cs  ->  sustituye TODO el contenido del método SavePlayerData por esto
    public void SavePlayerData(PlayerData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[GameDataManager] SavePlayerData: data nulo.");
            return;
        }

        try
        {
            var dir = System.IO.Path.GetDirectoryName(GetPlayerDataPath());
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);

            // Escritura atómica + flush al disco
            var finalPath = GetPlayerDataPath();
            var tmp = finalPath + ".tmp";
            using (var fs = new System.IO.FileStream(tmp, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 4096, System.IO.FileOptions.WriteThrough))
            using (var sw = new System.IO.StreamWriter(fs, System.Text.Encoding.UTF8))
            {
                sw.Write(json);
                sw.Flush();
                fs.Flush(true);
            }
            System.IO.File.Copy(tmp, finalPath, overwrite: true);
            System.IO.File.Delete(tmp);

            // Log de verificación (requiere System.Linq)
            int awakenTrue = data.heroes?.Count(h => h.awaken) ?? 0;
            Debug.Log($"[GameDataManager] Guardado OK → {finalPath} | heroes awaken=true: {awakenTrue}");
        }
        catch (System.Exception ex) // <= OJO: System.Exception
        {
            Debug.LogError($"[GameDataManager] Error guardando PlayerData: {ex}");
        }
    }

    // Opcional: utilidad para borrar guardados (útil en dev)
    public void DeleteAllSaves()
    {
        try
        {
            string dir = Path.GetDirectoryName(PlayerFullPath);
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);

            PlayerPrefs.DeleteKey(PD_BUILD_GUID_KEY);
            PlayerPrefs.Save();

            Debug.Log("[GameDataManager] Guardados eliminados y versión de build reseteada.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameDataManager] Error al borrar guardados: {ex}");
        }
    }
}
