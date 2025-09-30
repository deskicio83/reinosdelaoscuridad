/*
============================================================
GameBootstrap.cs — Inicialización de juego
------------------------------------------------------------
PROPÓSITO
- Asegurar managers esenciales y carga de datos antes de escenas.

USO
- `DontDestroyOnLoad`; instanciado si falta por SceneUICommon.EnsureEssentials().

MÉTODOS (COMPLETA AQUÍ)
- Awake(): inicializa managers (Music, Data).
- Start(): carga catálogo, player data, salta a primera escena.
============================================================
*/

using UnityEngine;
public class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Ensure<MusicManager>();
        Ensure<AnalyticsManager>();
        // ...
    }

    private void Ensure<T>() where T : MonoBehaviour
    {
        if (FindFirstObjectByType<T>() == null)
        {
            GameObject go = new GameObject(typeof(T).Name);
            go.AddComponent<T>();
            DontDestroyOnLoad(go);
            Debug.Log($"🛠 Instanciado automáticamente: {typeof(T).Name}");
        }
    }
}
