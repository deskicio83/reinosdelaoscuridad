/*
============================================================
HeroCatalogManager.cs — Acceso al catálogo (héroes/sets/gear)
------------------------------------------------------------
PROPÓSITO
- Cargar y exponer búsquedas de catálogo (por heroId, setId, gearId).

USO
- GetHeroById, GetSetById, GetGearById, glosarioEstadosES.

MÉTODOS (COMPLETA AQUÍ)
- LoadCatalogs(): Addressables/Resources → memoria.
- GetHeroById(string), GetSetById(string), GetGearById(string).
============================================================
*/



using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Linq;

public class HeroCatalogManager : MonoBehaviour
{
    public static HeroCatalogManager Instance { get; private set; }

    [Header("Configuración")]
    [Tooltip("Nombre del archivo JSON del catálogo de héroes (sin extensión)")]
    public string heroCatalogJsonName = "hero_catalog";
    [Tooltip("Nombre del archivo JSON del catálogo de equipo (sin extensión)")]
    public string gearCatalogJsonName = "gear_catalog";

    // Diccionarios para acceso rápido por ID
    private Dictionary<string, HeroCatalogEntry> heroDict = new Dictionary<string, HeroCatalogEntry>();
    private Dictionary<string, GearCatalog> gearDict = new Dictionary<string, GearCatalog>();
    private Dictionary<string, SetCatalogEntry> setDict = new Dictionary<string, SetCatalogEntry>();

    // Glosario ES: key -> (display, desc)
    public Dictionary<string, (string display, string desc)> GlossaryES { get; private set; }
        = new Dictionary<string, (string display, string desc)>(StringComparer.OrdinalIgnoreCase);

    // Listas completas (públicas si quieres iterarlas en inspector)
    public List<HeroCatalogEntry> heroes = new List<HeroCatalogEntry>();
    public List<GearCatalog> gears = new List<GearCatalog>();
    public List<SetCatalogEntry> sets = new List<SetCatalogEntry>();
        
    private static Dictionary<string,(string nombre,string descripcion)> _glossaryES;
    private static string _rawCatalogJson; // copia del JSON crudo para poder (re)construir el glosario cuando haga falta
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this);

        // Carga catálogos
        LoadHeroCatalogFromJson();     // <- overload sin parámetros (Resources)
        LoadGearCatalogFromJson();
        LoadSetCatalogFromJson();

        // Construye diccionarios
        BuildHeroDictionary();
        BuildGearDictionary();
        BuildSetDictionary();
    }
    // --- PEGAR DENTRO DE HeroCatalogManager (ámbito de clase) ---
    public static void SetRawCatalogJson(string json)
    {
        _rawCatalogJson = json;
        BuildGlossaryFromJson(json);
    }

    public static Dictionary<string,(string nombre,string descripcion)> GetGlossaryDict()
    {
        if (_glossaryES != null) return _glossaryES;
        if (!string.IsNullOrEmpty(_rawCatalogJson)) BuildGlossaryFromJson(_rawCatalogJson);
        return _glossaryES ?? new Dictionary<string,(string,string)>(StringComparer.OrdinalIgnoreCase);
    }

    private static void BuildGlossaryFromJson(string json)
    {
        try
        {
            var wrapper = JsonUtility.FromJson<HeroCatalogListWrapper>(json);
            var list = wrapper?.glosarioEstadosES ?? wrapper?.glossaryEstadosES;

            var res = new Dictionary<string,(string,string)>(StringComparer.OrdinalIgnoreCase);
            if (list != null)
            {
                foreach (var e in list)
                {
                    if (e == null || string.IsNullOrWhiteSpace(e.key)) continue;
                    var k = e.key.Trim();
                    var nombre =
                        !string.IsNullOrWhiteSpace(e.nombre) ? e.nombre.Trim() :
                        !string.IsNullOrWhiteSpace(e.display) ? e.display.Trim() :
                        !string.IsNullOrWhiteSpace(e.name) ? e.name.Trim() :
                        k;
                    var desc =
                        !string.IsNullOrWhiteSpace(e.descripcion) ? e.descripcion :
                        !string.IsNullOrWhiteSpace(e.desc) ? e.desc :
                        !string.IsNullOrWhiteSpace(e.description) ? e.description :
                        "";
                    res[k] = (nombre, desc);
                }
            }

            _glossaryES = res;
            Debug.Log($"[HeroCatalogManager] Glossary loaded: {_glossaryES.Count} entradas");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[HeroCatalogManager] Failed to build glossary: {ex}");
            _glossaryES = new Dictionary<string,(string,string)>(StringComparer.OrdinalIgnoreCase);
        }
    }

    // ============================================================
    //  CARGA DE HÉROES + GLOSARIO
    // ============================================================

    /// <summary>
    /// Overload sin parámetros: carga el JSON desde Resources/Data/{heroCatalogJsonName}.json
    /// y delega en el overload con string json.
    /// </summary>
    public void LoadHeroCatalogFromJson()
    {
        string resourcePath = $"Data/{heroCatalogJsonName}";
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            Debug.LogError($"[HeroCatalogManager] ❌ No se encontró el archivo: Resources/{resourcePath}.json");
            heroes = new List<HeroCatalogEntry>();
            GlossaryES = new Dictionary<string, (string display, string desc)>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        LoadHeroCatalogFromJson(asset.text);

    }

    /// <summary>
    /// Overload con el contenido JSON. Deserializa y construye lista de héroes + glosario ES.
    /// Acepta varias variantes de nombres según versiones anteriores del JSON.
    /// </summary>
    public void LoadHeroCatalogFromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[HeroCatalogManager] JSON vacío para catálogo de héroes.");
            heroes = new List<HeroCatalogEntry>();
            GlossaryES = new Dictionary<string, (string display, string desc)>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        HeroCatalogListWrapper wrapper = null;
        try
        {
            wrapper = JsonUtility.FromJson<HeroCatalogListWrapper>(json);
        }
        catch (Exception ex)
        {
            Debug.LogError("[HeroCatalogManager] Error deserializando catálogo de héroes: " + ex.Message);
        }

        if (wrapper == null)
        {
            heroes = new List<HeroCatalogEntry>();
            GlossaryES = new Dictionary<string, (string display, string desc)>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        // HÉROES (acepta heroes/Heroes)
        if (wrapper.heroes != null) heroes = wrapper.heroes;
        else if (wrapper.Heroes != null) heroes = wrapper.Heroes;
        else heroes = new List<HeroCatalogEntry>();

        // GLOSARIO (acepta glosarioEstadosES/glossaryEstadosES)
        GlossaryES = new Dictionary<string, (string display, string desc)>(StringComparer.OrdinalIgnoreCase);
        var rawList = wrapper.glosarioEstadosES ?? wrapper.glossaryEstadosES;
        if (rawList != null)
        {
            foreach (var e in rawList)
            {
                if (e == null || string.IsNullOrWhiteSpace(e.key)) continue;
                var key = e.key.Trim();

                // display: nombre | display | name | key
                var display =
                    !string.IsNullOrWhiteSpace(e.nombre) ? e.nombre.Trim() :
                    !string.IsNullOrWhiteSpace(e.display) ? e.display.Trim() :
                    !string.IsNullOrWhiteSpace(e.name) ? e.name.Trim() :
                    key;

                // desc: descripcion | desc | description | ""
                var desc =
                    !string.IsNullOrWhiteSpace(e.descripcion) ? e.descripcion :
                    !string.IsNullOrWhiteSpace(e.desc) ? e.desc :
                    !string.IsNullOrWhiteSpace(e.description) ? e.description :
                    "";

                if (!GlossaryES.ContainsKey(key))
                    GlossaryES[key] = (display, desc);
            }
        }

        // Guarda el JSON crudo y construye caché estática para GetGlossaryDict()
        SetRawCatalogJson(json);

        Debug.Log($"[HeroCatalogManager] ✅ Héroes: {heroes.Count} | GlosarioES: {GlossaryES.Count}");
    }
    /// <summary>
    /// Devuelve el diccionario del glosario ES (nunca null).
    /// </summary>
    public Dictionary<string, (string display, string desc)> GetGlossaryES()
    {
        return GlossaryES ?? new Dictionary<string, (string display, string desc)>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>LoadSetCatalogFromJson
    /// Intenta obtener una entrada del glosario (display, desc) por key.
    /// </summary>
    public bool TryGetGlossaryEntry(string key, out (string display, string desc) entry)
    {
        entry = default;
        if (string.IsNullOrEmpty(key) || GlossaryES == null) return false;
        return GlossaryES.TryGetValue(key, out entry);
    }

    // ============================================================
    //  CARGA DE CATÁLOGO DE EQUIPO / SETS 
    // ============================================================

    public void LoadGearCatalogFromJson()
    {
        gears.Clear();
        string resourcePath = $"Data/{gearCatalogJsonName}";
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            Debug.LogError($"[HeroCatalogManager] ❌ No se encontró el archivo: Resources/{resourcePath}.json");
            return;
        }
        GearCatalogListWrapper wrapper = JsonUtility.FromJson<GearCatalogListWrapper>(asset.text);
        if (wrapper == null || wrapper.gear == null)
        {
            Debug.LogError("[HeroCatalogManager] ❌ El catálogo de equipo no tiene la estructura esperada.");
            return;
        }
        gears = wrapper.gear;
        Debug.Log($"[HeroCatalogManager] ✅ Equipo cargado: {gears.Count} piezas.");
    }

    public void LoadSetCatalogFromJson()
    {
        sets.Clear();
        string resourcePath = $"Data/{gearCatalogJsonName}";
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            Debug.LogError($"[HeroCatalogManager] ❌ No se encontró el archivo: Resources/{resourcePath}.json");
            return;
        }
        try
        {
            SetCatalogListWrapper wrapper = JsonUtility.FromJson<SetCatalogListWrapper>(asset.text);
            if (wrapper != null && wrapper.sets != null)
            {
                sets = wrapper.sets;
                Debug.Log($"[HeroCatalogManager] ✅ Sets cargados: {sets.Count}.");
            }
            else
            {
                Debug.LogWarning("[HeroCatalogManager] No se encontraron sets en el JSON de gear.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[HeroCatalogManager] Error parseando sets: {ex.Message}");
        }
    }

    // ============================================================
    //  DICCIONARIOS
    // ============================================================

    private void BuildHeroDictionary()
    {
        heroDict = new Dictionary<string, HeroCatalogEntry>();
        foreach (var entry in heroes)
        {
            if (entry == null || string.IsNullOrEmpty(entry.heroId))
                continue;
            if (!heroDict.ContainsKey(entry.heroId))
                heroDict.Add(entry.heroId, entry);
        }
        Debug.Log($"[HeroCatalogManager] heroDict: {heroDict.Count} héroes.");
    }

    private void BuildGearDictionary()
    {
        gearDict = new Dictionary<string, GearCatalog>();
        foreach (var entry in gears)
        {
            if (entry == null || string.IsNullOrEmpty(entry.gearId))
                continue;
            if (!gearDict.ContainsKey(entry.gearId))
                gearDict.Add(entry.gearId, entry);
        }
        Debug.Log($"[HeroCatalogManager] gearDict: {gearDict.Count} ítems.");
    }

    private void BuildSetDictionary()
    {
        setDict = new Dictionary<string, SetCatalogEntry>();
        foreach (var entry in sets)
        {
            if (entry == null || string.IsNullOrEmpty(entry.setId))
                continue;
            if (!setDict.ContainsKey(entry.setId))
                setDict.Add(entry.setId, entry);
        }
        Debug.Log($"[HeroCatalogManager] setDict: {setDict.Count} sets.");
    }

    // ============================================================
    //  ACCESO PÚBLICO
    // ============================================================

    public HeroCatalogEntry GetHeroById(string heroId)
    {
        if (string.IsNullOrEmpty(heroId)) return null;
        if (heroDict == null || heroDict.Count == 0)
        {
            Debug.LogError("[HeroCatalogManager] heroDict no está inicializado.");
            return null;
        }
        heroDict.TryGetValue(heroId, out var entry);
        return entry;
    }

    public GearCatalog GetGearById(string gearId)
    {
        if (string.IsNullOrEmpty(gearId)) return null;
        if (gearDict == null || gearDict.Count == 0)
        {
            Debug.LogError("[HeroCatalogManager] gearDict no está inicializado.");
            return null;
        }
        gearDict.TryGetValue(gearId, out var entry);
        return entry;
    }

    public SetCatalogEntry GetSetById(string setId)
    {
        if (string.IsNullOrEmpty(setId)) return null;
        if (setDict == null || setDict.Count == 0)
        {
            Debug.LogWarning("[HeroCatalogManager] setDict no está inicializado.");
            return null;
        }
        setDict.TryGetValue(setId, out var entry);
        return entry;
    }

    // ------------------------
    // 🔹 HELPERS PARA EQUIPO
    // ------------------------

    /// Devuelve el setId de una pieza de equipo
    public string GetSetIdForGear(string gearId)
    {
        if (string.IsNullOrEmpty(gearId)) return null;
        if (gearDict.TryGetValue(gearId, out var gear))
            return gear.setId;
        return null;
    }

    /// Devuelve el setName (para cargar el icono)
    public string GetSetName(string setId)
    {
        if (string.IsNullOrEmpty(setId)) return null;
        if (setDict.TryGetValue(setId, out var set))
            return set.setName;
        return null;
    }

    /// Devuelve el objeto completo del set (con bonus)
    public SetCatalogEntry GetSetData(string setId)
    {
        if (string.IsNullOrEmpty(setId)) return null;
        if (setDict.TryGetValue(setId, out var set))
            return set;
        return null;
    }

    // ============================================================
    //  WRAPPERS JSON
    // ============================================================
    [Serializable]
    private class GlossaryEntry
    {
        public string key;

        // distintas variantes que pueden aparecer en JSON
        public string nombre;       // título/nombre (ES)
        public string descripcion;  // cuerpo (ES)

        public string display;      // variantes antiguas/alternas
        public string desc;
        public string name;
        public string description;
    }
    // Acepta heroes/Heroes y glosarioEstadosES/glosarioEstadosES
    [Serializable]
    private class HeroCatalogListWrapper
    {
        public List<HeroCatalogEntry> heroes;               // nombre usado en tu JSON
        public List<HeroCatalogEntry> Heroes;               // fallback por si cambia

        public List<GlossaryEntry> glosarioEstadosES;       // nombre EXACTO que usas
        public List<GlossaryEntry> glossaryEstadosES;       // fallback por si cambia
    }

    [Serializable]
    private class GlossaryRaw
    {
        public string key;

        // posibles nombres para título/display
        public string display;     // variante A
        public string nombre;      // variante B
        public string name;        // variante C

        // posibles nombres para la descripción
        public string desc;          // variante A
        public string descripcion;   // variante B
        public string description;   // variante C
    }

    [Serializable]
    public class GearCatalogListWrapper
    {
        public List<GearCatalog> gear;
        public List<SetCatalogEntry> sets;
    }

    [Serializable]
    public class SetCatalogListWrapper
    {
        public List<SetCatalogEntry> sets;
    }

}

[Serializable]
public class SetCatalogEntry
{
    public string setId;
    public string setName;
    public string bonus2;
    public string bonus4;
}