/*
============================================================
PlayerResourcesManager.cs — Recursos del jugador (oro, gemas...)
------------------------------------------------------------
PROPÓSITO
- Operaciones atómicas sobre contadores y señales de cambios.

USO
- Increment/Decrement + eventos → HUD.

MÉTODOS (COMPLETA AQUÍ)
- AddGold(int), SpendGems(int).
- event Action OnResourcesChanged.
============================================================
*/

using UnityEngine;
using System;
using System.IO;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PlayerResourcesManager : MonoBehaviour
{
    public static PlayerResourcesManager Instance { get; private set; }

    private const string SaveFilePath = "player_data.json";
    private static PlayerData _data;
    public static PlayerData Data => _data; // <--- ESTA PROPIEDAD ES CLAVE

    public static event Action OnDataLoaded;
    public static event Action OnDataChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("🟥 PlayerResourcesManager: ¡Duplicado! Me destruyo");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("🟩 PlayerResourcesManager: Inicializado y marcado como DontDestroyOnLoad");

        LoadData();
        RefreshEnergyFromLastLogin();
    }

    private static void LoadData()
    {
        Debug.Log("📂 Intentando cargar datos de jugador desde Addressables: Assets/Addressables/Data/player_data.json");

        Addressables.LoadAssetAsync<TextAsset>("Assets/Addressables/Data/player_data.json").Completed += (AsyncOperationHandle<TextAsset> op) =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                string json = op.Result.text;
                _data = JsonUtility.FromJson<PlayerData>(json);
                Debug.Log("📥 Datos cargados correctamente desde Addressables.");
                Debug.Log($"🧠 Datos cargados: {(_data != null && _data.heroes != null ? _data.heroes.Count : 0)} héroes.");

                OnDataLoaded?.Invoke();
                OnDataChanged?.Invoke();
            }
            else
            {
                Debug.LogWarning("❌ No se pudo cargar el JSON de jugador desde Addressables.");
                _data = new PlayerData();
                OnDataLoaded?.Invoke();
                OnDataChanged?.Invoke();
            }
        };
    }

    public static void RefreshEnergyFromLastLogin()
    {
        if (_data == null) return;

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long secondsPassed = now - _data.lastLoginTimestamp;

        int energiaRecuperada = (int)(secondsPassed / 300); // 1 energía cada 5 minutos

        if (energiaRecuperada > 0 && _data.energia < _data.energiaMax)
        {
            int antes = _data.energia;
            _data.energia = Mathf.Min(_data.energia + energiaRecuperada, _data.energiaMax);
            Debug.Log($"⚡ Energía regenerada offline: {antes} ➜ {_data.energia}");
        }

        _data.lastLoginTimestamp = now;
        Save();
    }

    public static string GetEnergiaAsString() => $"{Data.energia}/{Data.energiaMax}";
    public static string GetMonedasAsString() => Data.oroNegro.ToString();
    public static string GetCaosiferasAsString() => Data.caosifera.ToString();

    public static void Save()
    {
        if (_data == null)
        {
            Debug.LogWarning("[PlayerResourcesManager] No hay datos para guardar.");
            return;
        }

        string json = JsonUtility.ToJson(_data, true);
        string path = Path.Combine(Application.persistentDataPath, SaveFilePath);

        try
        {
            File.WriteAllText(path, json);
            Debug.Log($"💾 Datos guardados en: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError("❌ Error al guardar datos: " + e.Message);
        }
    }

    // --- MÉTODO PARA IMPORTAR JSON DE TEST ---
    public static void ImportTestJson(string testJsonPath)
    {
        string destination = Path.Combine(Application.persistentDataPath, "player_data.json");
        if (File.Exists(testJsonPath))
        {
            File.Copy(testJsonPath, destination, true);
            Debug.Log($"✅ Copiado JSON de test a datos del jugador: {destination}");
        }
        else
        {
            Debug.LogWarning($"❌ No se encontró el archivo de test en: {testJsonPath}");
        }
    }
}
