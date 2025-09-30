/*
============================================================
GearUtils.cs — Utilidades de equipamiento
------------------------------------------------------------
PROPÓSITO
- Normalización de claves de stats; extracción de slotType desde gearId.

MÉTODOS
- GetSlotTypeFromGearId(string gearId): "dolor_comico_casco_001" → "casco".
- NormalizeStatKey(string s): "CRI%", "crit_rate" → "tcri"/"tcri%".
- IsPercentStatKey(string key): si la clave representa porcentaje.
- IsSameStat(string a, string b): compara ignorando “%” y alias.
============================================================
*/

using System;
using System.Collections.Generic;

public static class GearUtils
{
    private static readonly Dictionary<string, string> StatAliases = new()
    {
        // Normaliza claves inconsistentes de player_data/substats
        { "cri", "tcri" },    // "cri%" -> "tcri%"
        { "crit", "tcri" },   // por si aparece en el futuro
        { "crit_rate", "tcri" },
        { "critdmg", "dcri" },
        { "crit_dmg", "dcri" },
    };

    /// Devuelve "casco", "espada", "escudo", "guantes", "pechera" o "botas" a partir del gearId.
    public static string GetSlotTypeFromGearId(string gearId)
    {
        if (string.IsNullOrEmpty(gearId)) return "";
        var parts = gearId.Split('_');
        return parts.Length >= 3 ? parts[2].ToLowerInvariant() : "";
    }

    /// Normaliza una clave de stat ("ATK%", "cri%", "Hp") → "atk", "tcri", "hp" y conserva "%" si existiese.
    public static string NormalizeStatKey(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Trim().ToLowerInvariant();

        bool isPercent = s.EndsWith("%");
        string core = isPercent ? s[..^1] : s;

        core = core.Replace(" ", "").Replace("\t", "");
        if (StatAliases.TryGetValue(core, out var mapped))
            core = mapped;

        return isPercent ? core + "%" : core;
    }

    /// ¿Es un stat porcentual?
    public static bool IsPercentStatKey(string key)
    {
        key = NormalizeStatKey(key);
        return key.EndsWith("%");
    }

    /// ¿Coinciden dos claves de stat (ignorando “%” y alias)?
    public static bool IsSameStat(string a, string b)
    {
        string na = NormalizeStatKey(a).Replace("%", "");
        string nb = NormalizeStatKey(b).Replace("%", "");
        return na == nb;
    }
}
