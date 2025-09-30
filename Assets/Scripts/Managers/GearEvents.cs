/*
============================================================
GearEvents.cs — Bus de eventos para equipamiento
------------------------------------------------------------
PROPÓSITO
- Señalizar cambios de gear para refrescar UI desacoplada.

USO
- GearEvents.RaiseHeroGearChanged(heroId);
- Suscribirse a OnHeroGearChanged.

EVENTOS
- public static event Action<string> OnHeroGearChanged;

MÉTODOS
- RaiseHeroGearChanged(string heroId): invoca listeners.
============================================================
*/

using System;
using UnityEngine;

public static class GearEvents
{
    /// <summary>
    /// Dispara cuando se equipa/desequipa/cambia gear de un héroe.
    /// Param: heroId afectado.
    /// </summary>
    public static event Action<string> OnHeroGearChanged;
    // --- HERO DELETE EVENT ---
    public static event System.Action<string> OnHeroDeleted;

    public static void RaiseHeroGearChanged(string heroId)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            Debug.LogWarning("[GearEvents] heroId vacío al notificar cambio de gear");
            return;
        }
        Debug.Log($"[GearEvents] Notificando cambio de gear para heroId={heroId}");
        OnHeroGearChanged?.Invoke(heroId);
    }
    public static void RaiseHeroDeleted(string heroId)
    {
        try
        {
            OnHeroDeleted?.Invoke(heroId);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[GearEvents] Error invoking OnHeroDeleted: {e}");
        }
    }
    

}
