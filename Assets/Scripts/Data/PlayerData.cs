/*
============================================================
PlayerData.cs — Datos persistentes del jugador
------------------------------------------------------------
- Gestionado por GameDataManager: Load/Save con Newtonsoft.
- Persistencia de maestrías por héroe + materiales.
- Normalización legacy (MasteryProgress) <-> per-hero.
============================================================
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public class PlayerData
{
    // =================== CAMPOS BÁSICOS ===================
    public string playerName;
    public int playerLevel;
    public int energia;
    public int energiaMax;
    public int oroNegro;
    public int caosifera;

    public PlayerResources resources;

    public List<HeroProgress> heroes;
    public List<GearInstance> gearInventory;
    public List<ArtifactInstance> artifactInventory;

    public string lastLogin;
    public long lastLoginTimestamp;
    public int maxHeroSpaces;

    // =================== MAESTRÍAS ===================
    // Materiales de maestría (clase ANIDADA para evitar colisiones de nombre)
    public MasteryMaterials masteryMaterials = new MasteryMaterials();

    // Legacy: progreso por-héroe (se sincroniza con HeroProgress.Maestries)
    public List<MasteryProgress> masteries = new List<MasteryProgress>();

    // =================== DESPERTAR ===================
    public AwakenInventory awakenInventory = new AwakenInventory();

    // Otros
    public List<Equipment> equipment;
    [SerializeField] public bool heroSceneCompactView = false;

    // ========================================================
    //                     MAESTRÍAS (API)
    // ========================================================

    /// Devuelve/crea el progreso legacy por-héroe (compatibilidad)
    // Devuelve el MasteryProgress del héroe (lo crea si no existe)
    public MasteryProgress GetOrCreateMasteryProgress(string heroId)
    {
        if (string.IsNullOrEmpty(heroId)) return null;
        if (masteries == null) masteries = new List<MasteryProgress>();
        var mp = masteries.FirstOrDefault(m => m.heroId == heroId);
        if (mp == null)
        {
            mp = new MasteryProgress { heroId = heroId, selectedNodeIds = new List<string>(), reset = 0 };
            masteries.Add(mp);
        }
        if (mp.selectedNodeIds == null) mp.selectedNodeIds = new List<string>();
        return mp;
    }
    public int GetMasteryResetCount(string heroId)
    {
        var mp = GetOrCreateMasteryProgress(heroId);
        return mp != null ? mp.reset : 0;
    }

    public void IncrementMasteryReset(string heroId)
    {
        var mp = GetOrCreateMasteryProgress(heroId);
        if (mp == null) return;
        mp.reset = Math.Max(0, mp.reset) + 1;
        Save();
    }

    // Elimina TODAS las maestrías del héroe (legacy + lista por héroe)
    public void ClearHeroMasteries(string heroId)
    {
        // per-hero
        var h = heroes?.FirstOrDefault(x => x.heroId == heroId);
        if (h != null && h.Maestries != null) h.Maestries.Clear();

        // legacy
        var mp = masteries?.FirstOrDefault(m => m.heroId == heroId);
        if (mp != null && mp.selectedNodeIds != null) mp.selectedNodeIds.Clear();

        Save();
    }

    // Gasto genérico según la moneda de variables_globales.json
    public bool TrySpendByCurrency(string moneda, int amount, out string error)
    {
        error = null;
        if (amount <= 0) return true;
        moneda = (moneda ?? "").ToLowerInvariant();

        switch (moneda)
        {
            case "caosifera":
                if (caosifera >= amount) { caosifera -= amount; Save(); return true; }
                error = "No tienes Caosífera suficiente.";
                return false;
            case "oro":
                if (oroNegro >= amount) { oroNegro -= amount; Save(); return true; }
                error = "No tienes Oro Negro suficiente.";
                return false;

            default:
                error = "Moneda desconocida: " + moneda;
                return false;
        }
    }

    /// Devuelve lista per-hero de maestrías (crea si falta)
    public List<string> GetOrCreateHeroMaestries(string heroId)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            Debug.LogWarning("[Mastery] heroId vacío en GetOrCreateHeroMaestries.");
            return new List<string>();
        }

        if (heroes == null)
        {
            Debug.LogWarning("[Mastery] No hay héroes en PlayerData.");
            return new List<string>();
        }

        var hero = heroes.FirstOrDefault(h => h.heroId == heroId);
        if (hero == null)
        {
            Debug.LogWarning("[Mastery] Héroe no encontrado: " + heroId);
            return new List<string>();
        }

        if (hero.Maestries == null)
            hero.Maestries = new List<string>();

        return hero.Maestries;
    }

    /// Añade una maestría al héroe y sincroniza legacy
    public void AddMasteryForHero(string heroId, string nodeId)
    {
        if (string.IsNullOrEmpty(heroId) || string.IsNullOrEmpty(nodeId)) return;

        var list = GetOrCreateHeroMaestries(heroId);
        if (!list.Contains(nodeId))
            list.Add(nodeId);

        // Legacy write-through
        var mp = GetOrCreateMasteryProgress(heroId);
        if (!mp.selectedNodeIds.Contains(nodeId))
            mp.selectedNodeIds.Add(nodeId);
    }

    /// Selección centralizada (valida, gasta, persiste)
    public bool TrySelectMasteryNode(string heroId, string nodeId, out string error, bool autoSave = true)
    {
        error = null;

        if (string.IsNullOrEmpty(heroId) || string.IsNullOrEmpty(nodeId))
        {
            error = "Parámetros inválidos.";
            return false;
        }

        var catMgr = MasteryCatalogManager.Instance;
        if (catMgr == null || catMgr.Catalog == null)
        {
            error = "Catálogo de maestrías no cargado.";
            return false;
        }
        var cat = catMgr.Catalog;
        var node = catMgr.GetNode(nodeId);
        if (node == null)
        {
            error = $"Nodo inexistente: {nodeId}";
            return false;
        }

        if (heroes == null) { error = "No hay héroes en PlayerData."; return false; }
        var hero = heroes.FirstOrDefault(h => h.heroId == heroId);
        if (hero == null) { error = $"Héroe no encontrado: {heroId}"; return false; }

        var current = GetOrCreateHeroMaestries(heroId);

        if (current.Contains(node.id))
        {
            error = "Nodo ya seleccionado.";
            return false;
        }

        // Validaciones de negocio (estrellas, awaken, padres, límites, t6)
        if (!ValidateMasterySelectionForIds(current, hero, node, cat, out error))
            return false;

        // Coste (por nodo o por tier)
        var cost = node.cost ?? cat.rules.GetCostForTier(node.tier);
        if (cost == null) { error = "Coste no definido."; return false; }

        // Pago
        if (!TrySpendMasteryMaterials(cost))
        {
            error = "Materiales insuficientes.";
            return false;
        }

        // Aplicar selección
        current.Add(node.id);
        SyncLegacyMasteryProgress_Add(heroId, node.id);

        if (autoSave)
        {
            try { Save(); }
            catch (Exception e) { Debug.LogError("[Mastery] Error al guardar: " + e.Message); }
        }

        Debug.Log($"[Mastery] Seleccionado nodo {node.id} para héroe {heroId}.");
        return true;
    }

    // Sincroniza legacy al añadir
    private void SyncLegacyMasteryProgress_Add(string heroId, string nodeId)
    {
        if (masteries == null) masteries = new List<MasteryProgress>();
        var mp = masteries.FirstOrDefault(m => m.heroId == heroId);
        if (mp == null)
        {
            mp = new MasteryProgress { heroId = heroId, selectedNodeIds = new List<string>() };
            masteries.Add(mp);
        }
        if (mp.selectedNodeIds == null) mp.selectedNodeIds = new List<string>();
        if (!mp.selectedNodeIds.Contains(nodeId))
            mp.selectedNodeIds.Add(nodeId);
    }

    public void NormalizeAfterLoad()
    {
        if (heroes == null || heroes.Count == 0) return;

        // ---- Legacy -> per-hero (SOLO si el héroe NO trae Maestries) ----
        if (masteries != null && masteries.Count > 0)
        {
            foreach (var mp in masteries)
            {
                if (string.IsNullOrEmpty(mp.heroId) || mp.selectedNodeIds == null) continue;
                var h = heroes.FirstOrDefault(x => x.heroId == mp.heroId);
                if (h == null) continue;

                // si ya hay Maestries en el héroe, NO añadimos nada desde legacy
                if (h.Maestries == null || h.Maestries.Count == 0)
                {
                    if (h.Maestries == null) h.Maestries = new List<string>();
                    foreach (var id in mp.selectedNodeIds)
                        if (!string.IsNullOrEmpty(id) && !h.Maestries.Contains(id))
                            h.Maestries.Add(id);
                }
            }
        }

        // ---- per-hero -> legacy (REFLEJO 1:1, sin union) ----
        if (masteries == null) masteries = new List<MasteryProgress>();
        foreach (var h in heroes)
        {
            var mp = masteries.FirstOrDefault(m => m.heroId == h.heroId);
            if (mp == null)
            {
                mp = new MasteryProgress { heroId = h.heroId, selectedNodeIds = new List<string>() };
                masteries.Add(mp);
            }

            if (mp.selectedNodeIds == null) mp.selectedNodeIds = new List<string>();
            mp.selectedNodeIds.Clear();
            if (h.Maestries != null && h.Maestries.Count > 0)
                mp.selectedNodeIds.AddRange(h.Maestries.Where(id => !string.IsNullOrEmpty(id)));
        }
    }


    private bool ValidateMasterySelectionForIds(IReadOnlyList<string> selectedIds, HeroProgress hero, MasteryNode node, MasteriesCatalog cat, out string error)
    {
        error = null;

        // Estrellas mínimas
        int minStars = node.minStars > 0 ? node.minStars : cat.rules.GetMinStarsForTier(node.tier);
        if (hero.stars < minStars)
        {
            error = $"Requiere {minStars} estrellas.";
            return false;
        }

        // Awaken si aplica (por nodo o por regla de tier)
        bool needAwaken = node.requiresAwaken
                          || (cat.rules.requiresAwakenAtTier > 0 && node.tier >= cat.rules.requiresAwakenAtTier);
        if (needAwaken && !hero.awaken)
        {
            error = "Requiere héroe Despertado.";
            return false;
        }

        // Padres: al menos uno
        if (node.requiresAnyOf != null && node.requiresAnyOf.Count > 0)
        {
            bool hasParent = node.requiresAnyOf.Any(reqId => selectedIds.Contains(reqId));
            if (!hasParent)
            {
                error = "No cumple el requisito de nodo previo.";
                return false;
            }
        }

        // Límite por tier
        int maxAtTier = cat.rules.GetTierMaxPicks(node.tier);
        if (maxAtTier > 0)
        {
            int picksAtTier = CountPicksAtTierIds(selectedIds, node.tier);
            if (picksAtTier >= maxAtTier)
            {
                error = $"Ya tienes el máximo ({maxAtTier}) en el Nivel {node.tier}.";
                return false;
            }
        }

        // Exclusión T6 por rama (si tu regla lo exige)
        if (node.tier == 6)
        {
            foreach (var selId in selectedIds)
            {
                var sel = MasteryCatalogManager.Instance.GetNode(selId);
                if (sel != null && sel.tier == 6 && sel.branch != node.branch)
                {
                    error = "En el Nivel 6 no puedes mezclar ramas.";
                    return false;
                }
            }
        }

        return true;
    }

    // Wrapper legacy (por si algo antiguo lo llama)
    private bool ValidateMasterySelection(MasteryProgress mp, HeroProgress hero, MasteryNode node, MasteriesCatalog cat, out string error)
    {
        IReadOnlyList<string> ids =
            (mp != null && mp.selectedNodeIds != null)
                ? (IReadOnlyList<string>)mp.selectedNodeIds
                : Array.Empty<string>();

        return ValidateMasterySelectionForIds(ids, hero, node, cat, out error);
    }

    private int CountPicksAtTierIds(IReadOnlyList<string> ids, int tier)
    {
        int count = 0;
        foreach (var id in ids)
        {
            var n = MasteryCatalogManager.Instance.GetNode(id);
            if (n != null && n.tier == tier) count++;
        }
        return count;
    }

    private int CountPicksAtTier(MasteryProgress mp, int tier)
    {
        IReadOnlyList<string> ids =
            (mp != null && mp.selectedNodeIds != null)
                ? (IReadOnlyList<string>)mp.selectedNodeIds
                : Array.Empty<string>();

        return CountPicksAtTierIds(ids, tier);
    }

    // =================== MATERIALES / MONEDAS ===================

    public bool TrySpendMasteryMaterials(MasteryCost cost)
    {
        if (masteryMaterials == null) masteryMaterials = new MasteryMaterials();
        return masteryMaterials.TrySpend(cost);
    }

    public bool TrySpendOroNegro(int amount)
    {
        if (oroNegro >= amount)
        {
            oroNegro -= amount;
            Save();
            return true;
        }
        return false;
    }

    public bool TrySpendCaosifera(int amount)
    {
        if (caosifera >= amount)
        {
            caosifera -= amount;
            Save();
            return true;
        }
        return false;
    }

    // =================== SAVE / HELPERS ===================

    // Guarda con GameDataManager
    public void Save()
    {
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.SavePlayerData(this);
            Debug.Log("[PlayerData] Guardado forzado desde PlayerData.Save().");
        }
        else
        {
            Debug.LogWarning("[PlayerData] No se pudo guardar: GameDataManager.Instance es null.");
        }
    }

    // Alias explícito si prefieres este nombre en otros sitios
    public void SaveSelf() => Save();

    public void SetHeroSceneCompactView(bool value)
    {
        heroSceneCompactView = value;
        Save();
    }

    public void AddHeroSpaces(int value)
    {
        maxHeroSpaces += value;
        Save();
    }

    public List<GearInstance> GetHeroEquipment(string heroId)
    {
        var hero = heroes?.Find(h => h.heroId == heroId);
        if (hero != null && hero.equipment != null)
            return hero.equipment;
        return new List<GearInstance>();
    }

    // ---------- Claves de inventario de despertar ----------
    public static string AwakenKey(string type, string elementLowerOrNull, string tier)
    {
        type = (type ?? "").ToLowerInvariant();
        tier = NormalizeTier(tier);
        string elem = NormalizeElement(elementLowerOrNull ?? "luz");

        if (type == "magic") type = "caos"; // compat
        if (type == "caos")  return $"caos_{tier}";                 // ej: caos_infusion
        return $"element_{elem}_{tier}";                            // ej: element_fuego_destilado
    }

    private static string NormalizeElement(string e)
    {
        switch ((e ?? "").ToLowerInvariant())
        {
            case "blanco": return "luz";
            case "negro": return "oscuridad";
            case "rojo": return "fuego";
            case "verde": return "naturaleza";
            case "azul": return "agua";
            default: return e;
        }
    }

    private static string NormalizeTier(string t)
    {
        switch ((t ?? "").ToLowerInvariant())
        {
            case "baja":  return "extracto";
            case "media": return "infusion";
            case "alta":  return "destilado";
            default:      return t;
        }
    }

    // =================== CLASES ANIDADAS ===================
    [Serializable]
    public class MasteryMaterials
    {
        public int basico;
        public int avanzado;
        public int divino;

        public bool CanAfford(MasteryCost c)
        {
            if (c == null) return true;
            return basico >= c.basico && avanzado >= c.avanzado && divino >= c.divino;
        }

        public bool TrySpend(MasteryCost c)
        {
            if (c == null) return true;
            if (!CanAfford(c)) return false;
            basico   -= c.basico;
            avanzado -= c.avanzado;
            divino   -= c.divino;
            if (basico < 0) basico = 0;
            if (avanzado < 0) avanzado = 0;
            if (divino < 0) divino = 0;
            return true;
        }
    }
}

// =================== OTRAS CLASES DE DATOS ===================

[Serializable]
public class PlayerResources
{
    public int summonScrolls;
    public int ascensionStones;
}

[Serializable]
public class ArtifactInstance
{
    public string artifactId;
    public int level;
}

[Serializable]
public class Equipment
{
    public string instanceId;
    public string gearId;
    public string rarity;
    public int upgradeLevel;
    public string mainStatType;
    public List<Substat> substats;
}

[Serializable]
public class Substat
{
    public string type;
    public float value;
}

[Serializable]
public class AwakenInventory
{
    // Diccionario con las cantidades (Newtonsoft lo serializa perfecto).
    // Claves válidas:
    //  - "caos_extracto|infusion|destilado"
    //  - "element_<luz|oscuridad|fuego|naturaleza|agua>_<extracto|infusion|destilado>"
    [JsonProperty("items")] public Dictionary<string, int> items = new Dictionary<string, int>();

    public int Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return 0;
        return items != null && items.TryGetValue(key, out var v) ? v : 0;
    }

    public void Add(string key, int amount)
    {
        if (string.IsNullOrEmpty(key) || amount == 0) return;
        if (items == null) items = new Dictionary<string, int>();
        items.TryGetValue(key, out var cur);
        items[key] = Math.Max(0, cur + amount);
    }

    public bool CanSpend(string key, int amount)
    {
        if (amount <= 0) return true;
        return Get(key) >= amount;
    }

    public bool TrySpend(string key, int amount)
    {
        if (string.IsNullOrEmpty(key) || amount <= 0) return true;
        if (!CanSpend(key, amount)) return false;

        items[key] = Get(key) - amount;

        if (GameDataManager.Instance != null && GameDataManager.Instance.PlayerData != null)
            GameDataManager.Instance.SavePlayerData(GameDataManager.Instance.PlayerData);

        return true;
    }
}
