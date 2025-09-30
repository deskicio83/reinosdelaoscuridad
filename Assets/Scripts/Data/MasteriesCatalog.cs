// ===============================================================
// MasteriesCatalog.cs
// ---------------------------------------------------------------
// Qué es y para qué sirve:
// Modela el catálogo estático de maestrías (JSON). Expone ramas,
// nodos, requisitos y vínculos. Permite búsquedas y listados
// ordenados para construir la UI.
//
// Funciones / responsabilidades clave:
// - GetNode: recupera nodo por id.
// - GetBranch: devuelve rama por clave/nombre.
// - ParentsOf / ChildrenOf: devuelve ids de conexiones.
// - Enumeraciones ordenadas: iterar nodos por tier/slot.
// ===============================================================
using System;
using System.Collections.Generic;

[Serializable]
public class MasteryCost
{
    public int basico;
    public int avanzado;
    public int divino;

    public bool IsZero() => basico == 0 && avanzado == 0 && divino == 0;
    public static MasteryCost Zero => new MasteryCost();
}

[Serializable]
public class MasteryNode
{
    public string id;                  // "O8"
    public string branch;              // "ofensa"|"defensa"|"apoyo"
    public int tier;                   // 1..6
    public int slot;                   // 1..2 (T1) / 1..4 (T2..T6)
    public string name;                // ES
    public string description;         // ES
    public List<string> effects;       // Placeholder (sin uso ahora)
    public List<string> requiresAnyOf; // IDs de padres
    public int minStars;
    public bool requiresAwaken;
    public MasteryCost cost;
}

[Serializable]
public class MasteryRules
{
    // Json trae diccionarios, los exponemos como string->int/cost y
    // damos helpers typed (int tier) para consultarlo seguro.
    public Dictionary<string, int> tierMaxPicks;
    public int maxBranchesAtTier6;
    public Dictionary<string, MasteryCost> costsPerTier;
    public Dictionary<string, int> minStarsPerTier;
    public int requiresAwakenAtTier;

    public int GetTierMaxPicks(int tier) =>
        tierMaxPicks != null && tierMaxPicks.TryGetValue(tier.ToString(), out var v) ? v : 0;

    public MasteryCost GetCostForTier(int tier) =>
        costsPerTier != null && costsPerTier.TryGetValue(tier.ToString(), out var c) ? c : MasteryCost.Zero;

    public int GetMinStarsForTier(int tier) =>
        minStarsPerTier != null && minStarsPerTier.TryGetValue(tier.ToString(), out var v) ? v : 0;
}

[Serializable]
public class MasteriesCatalog
{
    public MasteryRules rules;
    public List<MasteryNode> branches; // 66 nodos
}
