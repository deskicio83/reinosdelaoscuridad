using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

[DefaultExecutionOrder(-80)]
public class MasteryCatalogManager : MonoBehaviour
{
    public static MasteryCatalogManager Instance { get; private set; }

    [Header("Carga JSON (Resources)")]
    [Tooltip("Resources path sin .json (p.ej. Data/masteries_catalog_with_meta)")]
    public string resourcesPath = "Data/masteries_catalog_with_meta";

    private MasteriesCatalog _catalog;
    public MasteriesCatalog Catalog => _catalog;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInstall()
    {
        if (Instance == null)
        {
            var go = new GameObject("MasteryCatalogManager");
            go.AddComponent<MasteryCatalogManager>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadCatalog();
    }

    private void LoadCatalog()
    {
        TextAsset ta = Resources.Load<TextAsset>(resourcesPath);
        if (ta == null)
        {
            Debug.LogError($"[MasteryCatalogManager] JSON no encontrado en Resources: '{resourcesPath}.json'. " +
                           $"Colócalo en Assets/Resources/{resourcesPath}.json");
            _catalog = new MasteriesCatalog { rules = new MasteryRules(), branches = new List<MasteryNode>() };
            return;
        }

        try
        {
            _catalog = JsonConvert.DeserializeObject<MasteriesCatalog>(ta.text) ?? new MasteriesCatalog();
            if (_catalog.rules == null) _catalog.rules = new MasteryRules();
            if (_catalog.branches == null) _catalog.branches = new List<MasteryNode>();
            Debug.Log($"[MasteryCatalogManager] Catálogo cargado. Nodos={_catalog.branches.Count}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[MasteryCatalogManager] Error parseando JSON: " + ex);
            _catalog = new MasteriesCatalog { rules = new MasteryRules(), branches = new List<MasteryNode>() };
        }
    }

    public MasteryNode GetNode(string id) =>
        _catalog?.branches?.FirstOrDefault(n => n.id == id);

    public IEnumerable<MasteryNode> GetNodesBy(string branch, int tier) =>
        _catalog?.branches?.Where(n => n.branch == branch && n.tier == tier) ?? Enumerable.Empty<MasteryNode>();

    public IEnumerable<MasteryNode> AllNodes() =>
        _catalog?.branches ?? Enumerable.Empty<MasteryNode>();
}
