/*
============================================================
GearAutoEquipService.cs — Propuestas de auto-equipamiento
------------------------------------------------------------
PROPÓSITO
- Evaluar inventario para sugerir mejor combinación por slot/set.

USO
- Llamado desde PanelEquiparController para generar `proposal`.

MÉTODOS (COMPLETA AQUÍ)
- BuildProposal(heroId, criterio): devuelve Dictionary<slotType, GearInstance>.
- ScoreGearForHero(...): heurísticas de puntuación.
============================================================
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GearAutoEquipService
{
    // ====== CONFIG PESOS (ampliable) ======
    // Claves admitidas: hp, hp%, def, def%, spd, atk, atk%, tcri%, dcri%, res, res%, acc, acc%, luk, luk%
    // Nota: si no existe la clave, peso = 0
    private static readonly Dictionary<string, Dictionary<string, float>> CLASS_WEIGHTS = new Dictionary<string, Dictionary<string, float>>(StringComparer.OrdinalIgnoreCase)
    {
        // Tanque
        ["Tanque"] = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
        {
            ["hp%"]=10f, ["def%"]=9.5f, ["hp"]=6.5f, ["def"]=6.0f, ["spd"]=5.0f,
            ["res%"]=3.0f, ["res"]=2.0f, ["acc%"]=1.0f, ["acc"]=0.5f
        },
        // Atacante (ACTUALIZADO)
        ["Atacante"] = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
        {
            // main priority: atk% > spd > tcri% > dcri% > atk
            ["atk%"]=10f, ["spd"]=9.0f, ["tcri%"]=8.5f, ["dcri%"]=8.0f, ["atk"]=7.0f,
            // secundarios
            ["hp%"]=2f, ["def%"]=2f, ["hp"]=1f, ["def"]=1f, ["acc%"]=1f, ["acc"]=0.5f
        },
        // Sanador
        ["Sanador"] = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
        {
            ["hp%"]=9.5f, ["spd"]=9.0f, ["res%"]=8.0f, ["hp"]=6.0f, ["res"]=5.0f,
            ["def%"]=4.0f, ["def"]=3.0f, ["acc%"]=1.0f, ["acc"]=0.5f
        },
        // Especialista
        ["Especialista"] = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
        {
            ["spd"]=10f, ["luk%"]=9.0f, ["hp%"]=7.5f, ["def%"]=7.0f, ["atk%"]=6.5f, ["tcri%"]=6.0f, ["dcri%"]=5.5f,
            ["hp"]=4.0f, ["def"]=3.5f, ["atk"]=3.0f, ["luk"]=6.0f, ["acc%"]=2.0f, ["acc"]=1.0f, ["res%"]=2.0f, ["res"]=1.0f
        }
    };

    private const float MAINSTAT_MULT = 2.2f;             // Pesa más el mainstat
    private const float PERCENT_BETTER_MULT = 1.15f;      // % > plano
    private const float UPGRADE_LEVEL_BONUS = 0.15f;      // +por nivel
    private const float RARITY_STEP_BONUS = 0.25f;        // +por rareza (escala ligera)
    private const float SET_PIECE_BONUS = 0.8f;           // incentiva piezas del mismo set
    private const int TARGET_SLOTS = 6;

    // Devuelve la propuesta por slotType -> GearInstance (solo las piezas que mejorarían)
    public static Dictionary<string, GearInstance> ComputeBestSet(string heroId)
    {
        var player = GameDataManager.Instance.PlayerData;
        var hero = player.heroes.Find(h => h.heroId == heroId);
        if (hero == null) return new Dictionary<string, GearInstance>();

        var catalog = HeroCatalogManager.Instance.GetHeroById(heroId);
        var className = (catalog?.classStandard ?? "Atacante"); // default segura
        if (!CLASS_WEIGHTS.ContainsKey(className))
            className = "Atacante"; // fallback

        var weights = CLASS_WEIGHTS[className];

        // Inventario candidates por slot
        var inv = player.gearInventory ?? new List<GearInstance>();
        var current = hero.equipment ?? new List<GearInstance>();

        // Mapea por slot
        var currentBySlot = current
            .Where(g => g != null)
            .GroupBy(g => GetSlotType(g.gearId))
            .ToDictionary(g => g.Key, g => g.First());

        var candidatesBySlot = inv
            .Where(g => g != null)
            .GroupBy(g => GetSlotType(g.gearId))
            .ToDictionary(g => g.Key, g => g.ToList());

        // Score por pieza
        Dictionary<string, float> setCountsProjection = new Dictionary<string, float>();
        var proposal = new Dictionary<string, GearInstance>(); // slot -> gear
        var allSlots = new[] { "casco", "espada", "escudo", "guantes", "pechera", "botas" };

        // Primero, estimamos el mejor por slot sin sets
        Dictionary<GearInstance, float> cachedScore = new Dictionary<GearInstance, float>();
        foreach (var slot in allSlots)
        {
            currentBySlot.TryGetValue(slot, out var equipped);
            var best = PickBestForSlot(slot, candidatesBySlot, weights, equipped, out float bestScore, cachedScore);

            // Si el equipado es mejor o no hay mejor candidato, no tocar
            if (equipped != null)
            {
                float equippedScore = ScorePiece(equipped, weights, cachedScore);
                if (best == null || equippedScore >= bestScore)
                    continue;
            }

            if (best != null)
            {
                proposal[slot] = best;
            }
        }

        // Heurística set bonus: pequeño ajuste favoreciendo sets consistentes
        if (proposal.Count > 0)
        {
            var adjusted = TryFavorSets(proposal, currentBySlot, cachedScore, weights);
            proposal = adjusted;
        }

        return proposal;
    }

    private static GearInstance PickBestForSlot(string slot, Dictionary<string, List<GearInstance>> candidatesBySlot,
                                                Dictionary<string, float> weights,
                                                GearInstance equipped,
                                                out float bestScore,
                                                Dictionary<GearInstance, float> cache)
    {
        bestScore = float.MinValue;
        if (!candidatesBySlot.TryGetValue(slot, out var list) || list == null || list.Count == 0)
            return null;

        GearInstance best = null;
        foreach (var g in list)
        {
            float s = ScorePiece(g, weights, cache);
            if (s > bestScore)
            {
                bestScore = s;
                best = g;
            }
        }
        return best;
    }

    private static float ScorePiece(GearInstance g, Dictionary<string, float> weights, Dictionary<GearInstance, float> cache)
    {
        if (g == null) return 0f;
        if (cache.TryGetValue(g, out var cached)) return cached;

        float score = 0f;

        // Main stat
        var mainKey = NormalizeKey(g.mainStatType);
        float mainWeight = GetWeightWithPercentBonus(weights, mainKey) * MAINSTAT_MULT;
        score += mainWeight;

        // Substats
        if (g.substats != null)
        {
            foreach (var s in g.substats)
            {
                var k = NormalizeKey(s.type);
                score += GetWeightWithPercentBonus(weights, k);
            }
        }

        // Upgrade level y rareza
        score += (g.upgradeLevel * UPGRADE_LEVEL_BONUS);
        score += RarityToStep(g.rarity) * RARITY_STEP_BONUS;

        cache[g] = score;
        return score;
    }

    private static Dictionary<string, GearInstance> TryFavorSets(
        Dictionary<string, GearInstance> proposal,
        Dictionary<string, GearInstance> currentBySlot,
        Dictionary<GearInstance, float> cache,
        Dictionary<string, float> weights)
    {
        // Bonus por pieza que coincida de set con otras propuestas
        var list = proposal.ToList();
        var bySet = list.GroupBy(kv => GetSetId(kv.Value?.gearId)).ToDictionary(g => g.Key, g => g.ToList());

        // para cada set propuesto, añade bonus suave a sus piezas
        foreach (var kv in bySet)
        {
            var setId = kv.Key;
            if (string.IsNullOrEmpty(setId)) continue;
            int count = kv.Value.Count;
            float perPieceBonus = count * SET_PIECE_BONUS; // más piezas, más bonus

            foreach (var pair in kv.Value)
            {
                var piece = pair.Value;
                // re-score con plus set
                float baseScore = ScorePiece(piece, weights, cache);
                float adjusted = baseScore + perPieceBonus;
                cache[piece] = adjusted;
            }
        }

        // redecidir por slot con bonus aplicado
        var result = new Dictionary<string, GearInstance>(proposal);
        foreach (var slot in proposal.Keys.ToList())
        {
            var candidate = proposal[slot];
            float candScore = cache[candidate];

            // si equipado supera ahora, deshacer cambio
            if (currentBySlot.TryGetValue(slot, out var equipped) && equipped != null)
            {
                float eqScore = ScorePiece(equipped, weights, cache);
                if (eqScore >= candScore)
                    result.Remove(slot);
            }
        }
        return result;
    }

    private static string GetSlotType(string gearId)
    {
        if (string.IsNullOrEmpty(gearId)) return "";
        var parts = gearId.Split('_');
        if (parts.Length >= 3) return parts[2].ToLowerInvariant();
        return "";
    }

    private static string GetSetId(string gearId)
    {
        if (string.IsNullOrEmpty(gearId)) return null;
        return HeroCatalogManager.Instance.GetSetIdForGear(gearId);
    }

    private static string NormalizeKey(string stat)
    {
        if (string.IsNullOrEmpty(stat)) return "";
        return stat.Trim().ToLowerInvariant();
    }

    private static float GetWeightWithPercentBonus(Dictionary<string, float> weights, string key)
    {
        if (!weights.TryGetValue(key, out float w)) w = 0f;
        if (key.EndsWith("%")) w *= PERCENT_BETTER_MULT;
        return w;
    }

    private static int RarityToStep(string rarity)
    {
        // ordena suavemente las rarezas conocidas
        switch (rarity)
        {
            case "mugroso": return 0;
            case "extranito": return 1;
            case "absurdamente_escaso": return 2;
            case "divinamente_ridiculo": return 3;
            case "memeticamente_unico": return 4;
            default: return 0;
        }
    }
}
