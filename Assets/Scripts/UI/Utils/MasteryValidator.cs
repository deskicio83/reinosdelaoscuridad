using System.Linq;
using UnityEngine;

public static class MasteryValidator
{
    public static bool HasAnyParentSelected(MasteryProgress mp, MasteryNode node)
    {
        if (node == null) return false;
        if (node.requiresAnyOf == null || node.requiresAnyOf.Count == 0) return true; // T1
        return node.requiresAnyOf.Any(p => mp.HasSelected(p));
    }

    public static bool CanSelect(
        HeroProgress hero,
        MasteryProgress mp,
        PlayerData pd,
        MasteryNode node,
        MasteriesCatalog cat)
    {
        if (hero == null || mp == null || pd == null || node == null || cat == null || cat.rules == null) return false;

        // Estrellas / Awaken mínimos
        int minStars = node.minStars > 0 ? node.minStars : cat.rules.GetMinStarsForTier(node.tier);
        if (hero.stars < minStars) return false;

        if (node.requiresAwaken || (cat.rules.requiresAwakenAtTier > 0 && node.tier >= cat.rules.requiresAwakenAtTier))
        {
            if (!hero.awaken) return false;
        }

        // Máximo picks del tier (por rama)
        int tierMax = cat.rules.GetTierMaxPicks(node.tier);
        int inTier = mp.CountPicksIn(node.branch, node.tier, MasteryCatalogManager.Instance);
        if (inTier >= tierMax) return false;

        // Camino
        if (node.tier > 1 && !HasAnyParentSelected(mp, node)) return false;

        // Exclusión: Tier 6 en una sola rama
        if (node.tier == 6 && mp.AnyTier6SelectedOtherBranch(node.branch, MasteryCatalogManager.Instance)) return false;

        // Materiales suficientes
        var cost = node.cost ?? cat.rules.GetCostForTier(node.tier);
        var mats = pd?.masteryMaterials ?? new PlayerData.MasteryMaterials();
        
        if (mats.basico < cost.basico) return false;
        if (mats.avanzado < cost.avanzado) return false;
        if (mats.divino < cost.divino) return false;

        return true;
    }

    public static bool TryApplySelect(PlayerData pd, MasteryProgress mp, MasteryNode node, MasteriesCatalog cat)
    {
        if (pd == null || mp == null || node == null)
        {
            UnityEngine.Debug.LogWarning("[Mastery] Parámetros inválidos en TryApplySelect.");
            return false;
        }

        string error;
        bool ok = pd.TrySelectMasteryNode(mp.heroId, node.id, out error, autoSave: true);
        if (!ok)
            UnityEngine.Debug.Log("[Mastery] Selección rechazada: " + error);

        return ok;
    }

}
