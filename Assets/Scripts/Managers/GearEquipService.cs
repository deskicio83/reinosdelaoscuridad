/*
============================================================
GearEquipService.cs — Servicio de equipar/desequipar centralizado
------------------------------------------------------------
PROPÓSITO
- Aplicar cambios de equipamiento de forma robusta y coherente.
- Mover piezas previas al inventario, guardar PlayerData, avisar UI.

USO
- UI llama a: EquipSingle(...) o ApplyProposal(...).
- Lanza GearEvents.RaiseHeroGearChanged(heroId).

MÉTODOS
- EquipSingle(string heroId, GearInstance gear, string slotType=null, bool save=true, bool raiseEvents=true)
  Equipa 1 pieza; resuelve slotType con GearUtils si no se pasa; mueve la previa al inventario.
- ApplyProposal(string heroId, Dictionary<string,GearInstance> proposal, bool save=true, bool raiseEvents=true)
  Equipa en bloque una propuesta (auto-equip).
- UnequipSlot(string heroId, string slotType, bool save=true, bool raiseEvents=true)
  Desequipa el slot indicado y devuelve la pieza al inventario.
============================================================
*/


using System;
using System.Collections.Generic;
using UnityEngine;

public static class GearEquipService
{
    /// <summary>
    /// Equipa una pieza en el héroe indicado. Mueve la anterior (si la hay) al inventario.
    /// slotType: si es null, se deriva de gear.gearId con GearUtils.GetSlotTypeFromGearId.
    /// Guarda PlayerData y lanza GearEvents.RaiseHeroGearChanged(heroId).
    /// </summary>
    public static void EquipSingle(string heroId, GearInstance gear, string slotType = null, bool save = true, bool raiseEvents = true)
    {
        if (string.IsNullOrEmpty(heroId) || gear == null)
        {
            Debug.LogWarning("[GearEquipService] EquipSingle: heroId o gear es null.");
            return;
        }

        var player = GameDataManager.Instance?.PlayerData;
        if (player == null)
        {
            Debug.LogError("[GearEquipService] PlayerData es null.");
            return;
        }

        var hero = player.heroes.Find(h => h.heroId == heroId);
        if (hero == null)
        {
            Debug.LogError($"[GearEquipService] Héroe no encontrado: {heroId}");
            return;
        }

        if (hero.equipment == null) hero.equipment = new List<GearInstance>();
        if (player.gearInventory == null) player.gearInventory = new List<GearInstance>();

        // Resolver slot
        slotType ??= GearUtils.GetSlotTypeFromGearId(gear.gearId);
        if (string.IsNullOrEmpty(slotType))
        {
            Debug.LogWarning($"[GearEquipService] No se pudo resolver slotType para gearId={gear.gearId}");
            return;
        }

        // Quitar previo del mismo slot si existe
        var prev = hero.equipment.Find(g => GearUtils.GetSlotTypeFromGearId(g?.gearId) == slotType);
        if (prev != null)
        {
            hero.equipment.Remove(prev);
            player.gearInventory.Add(prev);
        }

        // Quitar del inventario la nueva pieza y equiparla
        player.gearInventory.RemoveAll(x => x.instanceId == gear.instanceId);
        hero.equipment.Add(gear);

        if (save) player.Save();
        if (raiseEvents) GearEvents.RaiseHeroGearChanged(heroId);
    }

    /// <summary>
    /// Aplica en bloque una propuesta: slotType -> GearInstance.
    /// Guarda PlayerData y lanza GearEvents al final (una vez).
    /// </summary>
    public static void ApplyProposal(string heroId, Dictionary<string, GearInstance> proposal, bool save = true, bool raiseEvents = true)
    {
        if (string.IsNullOrEmpty(heroId) || proposal == null || proposal.Count == 0) return;

        var player = GameDataManager.Instance?.PlayerData;
        if (player == null)
        {
            Debug.LogError("[GearEquipService] PlayerData es null.");
            return;
        }

        var hero = player.heroes.Find(h => h.heroId == heroId);
        if (hero == null)
        {
            Debug.LogError($"[GearEquipService] Héroe no encontrado: {heroId}");
            return;
        }

        if (hero.equipment == null) hero.equipment = new List<GearInstance>();
        if (player.gearInventory == null) player.gearInventory = new List<GearInstance>();

        foreach (var kv in proposal)
        {
            string slotType = kv.Key;
            var newGear = kv.Value;
            if (newGear == null) continue;

            // Quita previo del mismo slot
            var prev = hero.equipment.Find(g => GearUtils.GetSlotTypeFromGearId(g?.gearId) == slotType);
            if (prev != null)
            {
                hero.equipment.Remove(prev);
                player.gearInventory.Add(prev);
            }

            // Saca del inventario la nueva y equipa
            player.gearInventory.RemoveAll(x => x.instanceId == newGear.instanceId);
            hero.equipment.Add(newGear);
        }

        if (save) player.Save();
        if (raiseEvents) GearEvents.RaiseHeroGearChanged(heroId);
    }

    /// <summary>
    /// Desequipa el slot indicado y devuelve la pieza al inventario.
    /// </summary>
    public static void UnequipSlot(string heroId, string slotType, bool save = true, bool raiseEvents = true)
    {
        if (string.IsNullOrEmpty(heroId) || string.IsNullOrEmpty(slotType)) return;

        var player = GameDataManager.Instance?.PlayerData;
        if (player == null) return;

        var hero = player.heroes.Find(h => h.heroId == heroId);
        if (hero == null) return;

        if (hero.equipment == null) return;
        if (player.gearInventory == null) player.gearInventory = new List<GearInstance>();

        var prev = hero.equipment.Find(g => GearUtils.GetSlotTypeFromGearId(g?.gearId) == slotType);
        if (prev != null)
        {
            hero.equipment.Remove(prev);
            player.gearInventory.Add(prev);

            if (save) player.Save();
            if (raiseEvents) GearEvents.RaiseHeroGearChanged(heroId);
        }
    }
}
