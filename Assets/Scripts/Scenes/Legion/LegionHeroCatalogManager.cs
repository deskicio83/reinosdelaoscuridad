// Assets/Scripts/Scenes/Legion/LegionHeroCatalogManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class LegionHeroCatalogManager : MonoBehaviour
{
    public static LegionHeroCatalogManager Instance { get; private set; }

    [Header("Nombre del JSON en Resources/Data")]
    [SerializeField] private string heroCatalogJsonName = "hero_catalog"; // -> Assets/Resources/Data/hero_catalog.json

    [NonSerialized] public List<HeroCatalogEntry> heroes = new List<HeroCatalogEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromResources();
    }

    public void LoadFromResources()
    {
        var ta = Resources.Load<TextAsset>($"Data/{heroCatalogJsonName}");
        if (ta == null)
        {
            Debug.LogError($"[LegionCatalog] No se encontró Resources/Data/{heroCatalogJsonName}.json");
            heroes = new List<HeroCatalogEntry>();
            return;
        }

        try
        {
            var wrapper = JsonUtility.FromJson<HeroListWrapper>(ta.text);
            heroes = wrapper != null && wrapper.heroes != null ? wrapper.heroes : new List<HeroCatalogEntry>();
            Debug.Log($"[LegionCatalog] Héroes cargados: {heroes.Count}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[LegionCatalog] Error parseando hero_catalog: {ex}");
            heroes = new List<HeroCatalogEntry>();
        }
    }

    [Serializable] private class HeroListWrapper { public List<HeroCatalogEntry> heroes; }
}
