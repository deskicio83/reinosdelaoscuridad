/*
============================================================
StatsService.cs — Cálculo de stats (base + equipamiento)
------------------------------------------------------------
PROPÓSITO
- Obtener el valor base y total por stat combinando equipo (flat y %).

MÉTODOS
- int GetBaseStat(HeroCatalogEntry catalog, string stat):
  Selecciona baseHP/baseATK/... según la clave.
- int GetTotalStat(HeroCatalogEntry catalog, List<GearInstance> equipment, string stat):
  Suma de flat + % al base; main stat usa upgradeLevel como valor; substats usan su value.
- string[] AllStats:
  Orden estándar de stats para recorrer y pintar.

NOTAS
- Usa GearUtils.IsSameStat/IsPercentStatKey para alinear claves.
============================================================
*/

using System.Collections.Generic;
using UnityEngine;

public static class StatsService
{
    // Orden de stats estándar
    public static readonly string[] AllStats = { "hp", "atk", "def", "spd", "tcri", "dcri", "acc", "res", "luk", "agi" };

    public static int GetBaseStat(HeroCatalogEntry catalog, string stat)
    {
        if (catalog?.stats == null) return 0;
        switch (stat)
        {
            case "hp":   return catalog.stats.baseHP;
            case "atk":  return catalog.stats.baseATK;
            case "def":  return catalog.stats.baseDEF;
            case "spd":  return catalog.stats.baseSPD;
            case "tcri": return catalog.stats.baseTCRI;
            case "dcri": return catalog.stats.baseDCRI;
            case "acc":  return catalog.stats.baseACC;
            case "res":  return catalog.stats.baseRES;
            case "luk":  return catalog.stats.baseLUK;
            case "agi":  return catalog.stats.baseAGI;
            default:     return 0;
        }
    }

    /// Suma flat + % desde equipamiento a un stat base (usa upgradeLevel como valor base para main stat, igual que tus paneles).
    public static int GetTotalStat(HeroCatalogEntry catalog, List<GearInstance> equipment, string stat)
    {
        int baseValue = GetBaseStat(catalog, stat);
        if (equipment == null || equipment.Count == 0) return baseValue;

        int flatBonus = 0;
        int percentBonus = 0;

        foreach (var item in equipment)
        {
            if (item == null) continue;

            // Main stat
            if (!string.IsNullOrEmpty(item.mainStatType) && GearUtils.IsSameStat(item.mainStatType, stat))
            {
                if (GearUtils.IsPercentStatKey(item.mainStatType))
                    percentBonus += item.upgradeLevel;
                else
                    flatBonus += item.upgradeLevel;
            }

            // Substats
            if (item.substats != null)
            {
                foreach (var sub in item.substats)
                {
                    if (sub == null || string.IsNullOrEmpty(sub.type)) continue;
                    if (GearUtils.IsSameStat(sub.type, stat))
                    {
                        int v = Mathf.RoundToInt(sub.value);
                        if (GearUtils.IsPercentStatKey(sub.type)) percentBonus += v;
                        else flatBonus += v;
                    }
                }
            }
        }

        int percentBonusValue = Mathf.RoundToInt(baseValue * (percentBonus / 100f));
        return baseValue + flatBonus + percentBonusValue;
    }
}
