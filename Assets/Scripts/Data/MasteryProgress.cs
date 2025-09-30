// ===============================================================
// MasteryProgress.cs
// ---------------------------------------------------------------
// Qué es y para qué sirve:
// Estado in-memory del progreso de maestrías de un héroe.
// Guarda nodos comprados, rutas activas y verifica elegibilidad.
//
// Funciones / responsabilidades clave:
// - HasSelected/Select: consulta y añade nodos comprados.
// - Helpers de gating: validaciones de stars, awaken y padres desbloqueados.
// - Integración con persistencia: sincroniza con PlayerData y refresca UI.
// ===============================================================
using System;
using System.Collections.Generic;
using System.Linq;

// Progreso por HÉROE: qué nodos de maestría tiene seleccionados
[Serializable]
public class MasteryProgress
{
    public string heroId;
    public List<string> selectedNodeIds = new List<string>();

    // NUEVO ► número de reseteos hechos para este héroe
    public int reset = 0;
}

// Materiales del jugador para maestrías
[Serializable]
public class MasteryMaterials
{
    public int basico;
    public int avanzado;
    public int divino;
}

// Helpers para PlayerData + MasteryProgress
public static class PlayerMasteryExtensions
{
    public static MasteryProgress GetOrCreateMasteryProgress(this PlayerData pd, string heroId)
    {
        if (pd.masteries == null) pd.masteries = new List<MasteryProgress>();
        var mp = pd.masteries.FirstOrDefault(m => m.heroId == heroId);
        if (mp == null)
        {
            mp = new MasteryProgress { heroId = heroId };
            pd.masteries.Add(mp);
        }
        return mp;
    }

    public static bool HasSelected(this MasteryProgress mp, string nodeId) =>
        mp != null && mp.selectedNodeIds != null && mp.selectedNodeIds.Contains(nodeId);

    public static int CountPicksIn(this MasteryProgress mp, string branch, int tier, MasteryCatalogManager cat)
    {
        if (mp == null || mp.selectedNodeIds == null || cat == null) return 0;
        int c = 0;
        foreach (var id in mp.selectedNodeIds)
        {
            var n = cat.GetNode(id);
            if (n != null && n.branch == branch && n.tier == tier) c++;
        }
        return c;
    }

    public static int CountPicksAtTier(this MasteryProgress mp, int tier, MasteryCatalogManager cat)
    {
        if (mp == null || mp.selectedNodeIds == null || cat == null) return 0;
        int c = 0;
        foreach (var id in mp.selectedNodeIds)
        {
            var n = cat.GetNode(id);
            if (n != null && n.tier == tier) c++;
        }
        return c;
    }

    public static bool AnyTier6SelectedOtherBranch(this MasteryProgress mp, string branch, MasteryCatalogManager cat)
    {
        if (mp == null || mp.selectedNodeIds == null || cat == null) return false;
        foreach (var id in mp.selectedNodeIds)
        {
            var n = cat.GetNode(id);
            if (n != null && n.tier == 6 && n.branch != branch) return true;
        }
        return false;
    }
}
