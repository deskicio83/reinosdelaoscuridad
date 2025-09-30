/*
============================================================
HeroEquipmentPanelUI.cs — Panel de slots de equipamiento del héroe
------------------------------------------------------------
PROPÓSITO
- Mostrar 6 slots (casco, espada, escudo, guantes, pechera, botas),
  abrir inventario filtrado por slot, y mostrar detalle del item equipado.

REFERENCIAS (INSPECTOR)
- 6 GearSlotUI, GearDetailPanelUI gearDetailPanel, PanelEquiparController panelEquiparController.

MÉTODOS
- SetEquipment(List<GearInstance> equipment, Action<GearInstance,string> onGearClick=null):
  actualiza cada slot; click en slot abre inventario filtrado; click en pieza muestra detalle.
- HideDetailPanel(): cierra panel detalle si está abierto.
- OnAddGearSlotClicked(string slotType): abre PanelEquiparController con filtro de slot.
- GetHeroIdForEquippedGear(List<GearInstance> equippedGear): intenta resolver heroId por referencia/ids.
- OnRemoveGearFromHero(GearInstance gear, string heroId):
  desequipa la pieza (mueve a inventario), guarda y emite GearEvents.RaiseHeroGearChanged(heroId).

HELPERS PRIVADOS
- BuildSlotMap(): mapa slotType→GearSlotUI.

NOTAS
- Usa GearUtils.GetSlotTypeFromGearId(...) para derivar slot.
============================================================
*/


using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class HeroEquipmentPanelUI : MonoBehaviour
{
    [Header("Referencias a cada GearSlotUI")]
    public GearSlotUI slotCasco;
    public GearSlotUI slotEspada;
    public GearSlotUI slotEscudo;
    public GearSlotUI slotGuantes;
    public GearSlotUI slotPechera;
    public GearSlotUI slotBotas;

    [Header("Panel de detalle de Gear (asignar en inspector)")]
    public GearDetailPanelUI gearDetailPanel;

    [Header("Panel Equipar (nuevo sistema)")]
    public PanelEquiparController panelEquiparController;

    private string currentHeroId = null;
    private Dictionary<string, GearSlotUI> slotMap;

    // Inicializa el mapeo slotType -> GearSlotUI
    private void BuildSlotMap()
    {
        slotMap = new Dictionary<string, GearSlotUI>
        {
            { "casco", slotCasco },
            { "espada", slotEspada },
            { "escudo", slotEscudo },
            { "guantes", slotGuantes },
            { "pechera", slotPechera },
            { "botas", slotBotas }
        };
    }

    /// <summary>
    /// Llama a este método para actualizar los slots al seleccionar un héroe

    // Factorizado: pinta los slots y enlaza callbacks (usa currentHeroId ya fijado)
    private void UpdateSlots(List<GearInstance> equipment, System.Action<GearInstance, string> onGearClick)
    {
        if (slotMap == null)
            BuildSlotMap();

        var occupiedSlots = new HashSet<string>();
        if (equipment != null)
        {
            foreach (var gear in equipment)
            {
                string slotType = GearUtils.GetSlotTypeFromGearId(gear.gearId);
                occupiedSlots.Add(slotType);
            }
        }

        foreach (var kvp in slotMap)
        {
            string slotType = kvp.Key;
            GearSlotUI slot = kvp.Value;
            GearInstance gearToAssign = null;

            if (equipment != null)
                gearToAssign = equipment.Find(g => GearUtils.GetSlotTypeFromGearId(g.gearId) == slotType);

            if (gearToAssign != null)
            {
                slot.Setup(gearToAssign, slotType, (clickedGear) =>
                {
                    // 1) Abrir inventario filtrando por el slot pulsado
                    if (panelEquiparController != null)
                    {
                        panelEquiparController.gameObject.SetActive(true);
                        panelEquiparController.Show(currentHeroId, slotType);
                    }
                    else
                    {
                        Debug.LogWarning("[HeroEquipmentPanelUI] PanelEquiparController no asignado.");
                    }

                    // 2) Mostrar detalle del item equipado
                    if (clickedGear != null && gearDetailPanel != null)
                    {
                        var gearCatalog = HeroCatalogManager.Instance.GetGearById(clickedGear.gearId);
                        string heroId = currentHeroId;
                        gearDetailPanel.Show(clickedGear, gearCatalog, equipment, heroId, OnRemoveGearFromHero);
                    }
                });
                slot.OnAddGearClicked = () => OnAddGearSlotClicked(slotType);
            }
            else
            {
                slot.Setup(null, slotType, null);
                slot.OnAddGearClicked = () => OnAddGearSlotClicked(slotType);
            }
        }
    }


    public void SetEquipment(List<GearInstance> equipment, Action<GearInstance, string> onGearClick = null)
    {
         Debug.Log($"[HeroEquipmentPanelUI] SetEquipment ...: {(equipment == null ? "null" : equipment.Count.ToString())}");

        // Intento de deducción (referencia o contenido) por compatibilidad
        currentHeroId = GetHeroIdForEquippedGear(equipment);

        if (string.IsNullOrEmpty(currentHeroId))
        {
            // Downgrade a warning: en listas vacías o copias no siempre es deducible
            Debug.LogWarning("[HeroEquipmentPanelUI] No se pudo deducir el heroId a partir del equipo. " +
                            "Usa SetEquipmentForHero(hero) para máxima fiabilidad.");
        }

        // Pinta igual con el heroId que tengamos (si null, solo afecta a callbacks que lo requieran)
        UpdateSlots(equipment ?? new List<GearInstance>(), onGearClick);

        if (slotMap == null)
            BuildSlotMap();

        var occupiedSlots = new HashSet<string>();
        if (equipment != null)
        {
            foreach (var gear in equipment)
            {
                string slotType = GearUtils.GetSlotTypeFromGearId(gear.gearId);
                occupiedSlots.Add(slotType);
            }
        }

        foreach (var kvp in slotMap)
        {
            string slotType = kvp.Key;
            GearSlotUI slot = kvp.Value;
            GearInstance gearToAssign = null;
            if (equipment != null)
                gearToAssign = equipment.Find(g => GearUtils.GetSlotTypeFromGearId(g.gearId) == slotType);

            if (gearToAssign != null)
            {
                slot.Setup(gearToAssign, slotType, (clickedGear) =>
                {
                    // 1) Abrir inventario filtrando por el slot pulsado
                    if (panelEquiparController != null)
                    {
                        panelEquiparController.gameObject.SetActive(true);
                        panelEquiparController.Show(currentHeroId, slotType);
                    }
                    else
                    {
                        Debug.LogWarning("[HeroEquipmentPanelUI] PanelEquiparController no asignado.");
                    }

                    // 2) Mostrar detalle del item equipado
                    if (clickedGear != null && gearDetailPanel != null)
                    {
                        var gearCatalog = HeroCatalogManager.Instance.GetGearById(clickedGear.gearId);
                        string heroId = currentHeroId;
                        gearDetailPanel.Show(clickedGear, gearCatalog, equipment, heroId, OnRemoveGearFromHero);
                    }
                });
                slot.OnAddGearClicked = () => OnAddGearSlotClicked(slotType);
            }
            else
            {
                slot.Setup(null, slotType, null);
                slot.OnAddGearClicked = () => OnAddGearSlotClicked(slotType);
            }
        }
    }
    // Nuevo: usar SIEMPRE que conozcas el héroe (evita deducir heroId por la lista)

    public void SetEquipmentForHero(HeroProgress hero, System.Action<GearInstance, string> onGearClick = null)
    {
        if (hero == null)
        {
            Debug.LogError("[HeroEquipmentPanelUI] Héroe nulo en SetEquipmentForHero.");
            return;
        }

        currentHeroId = hero.heroId;
        var equipment = hero.equipment ?? new List<GearInstance>();

        // Reusa el mismo pipeline de pintado
        UpdateSlots(equipment, onGearClick);
    }
    private void OnAddGearSlotClicked(string slotType = null)
    {
        if (panelEquiparController != null)
        {
            panelEquiparController.gameObject.SetActive(true);
            panelEquiparController.Show(currentHeroId, slotType);
        }
        else
        {
            Debug.LogWarning("[HeroEquipmentPanelUI] PanelEquiparController no asignado.");
        }
    }

    public void HideDetailPanel()
    {
        if (gearDetailPanel != null)
            gearDetailPanel.Hide();
    }

    private string GetHeroIdForEquippedGear(List<GearInstance> equippedGear)
    {
        if (equippedGear == null || equippedGear.Count == 0) return null;
        var playerData = GameDataManager.Instance.PlayerData;
        foreach (var hero in playerData.heroes)
        {
            // Compara por instancia
            if (hero.equipment == equippedGear)
                return hero.heroId;

            // Compara por IDs si hay duplicados en memoria
            if (hero.equipment.Count == equippedGear.Count)
            {
                bool allMatch = true;
                for (int i = 0; i < hero.equipment.Count; i++)
                {
                    if (hero.equipment[i].instanceId != equippedGear[i].instanceId)
                    {
                        allMatch = false;
                        break;
                    }
                }
                if (allMatch)
                    return hero.heroId;
            }
        }
        return null;
    }

    // CALLBACK: Desequipar equipo y mover a inventario (REFORMADO)
    private void OnRemoveGearFromHero(GearInstance removed, string heroId)
    {
        var gdm = GameDataManager.Instance;
        var pd = gdm != null ? gdm.PlayerData : null;
        if (pd == null || removed == null || string.IsNullOrEmpty(heroId))
            return;

        // 1) Localiza el héroe POR ID (no uses GetHeroById: no existe en tu PlayerData)
        var hero = pd.heroes != null ? pd.heroes.Find(h => h.heroId == heroId) : null;
        if (hero == null || hero.equipment == null)
            return;

        // 2) Comprueba que realmente está equipado (por referencia o por instanceId)
        int idx = hero.equipment.FindIndex(g =>
            ReferenceEquals(g, removed) || g.instanceId == removed.instanceId);
        if (idx < 0) return;

        var gear = hero.equipment[idx];

        // 3) Quita del héroe y devuelve al inventario (evita duplicados en el inventario)
        hero.equipment.RemoveAt(idx);
        if (pd.gearInventory != null)
        {
            bool yaEsta = pd.gearInventory.Exists(g =>
                ReferenceEquals(g, gear) || g.instanceId == gear.instanceId);
            if (!yaEsta) pd.gearInventory.Add(gear);
        }

        // 4) Guarda con tu firma real (SavePlayerData(PlayerData))
        gdm.SavePlayerData(pd);

        // 5) Repinta inmediatamente este panel con el mismo héroe
        SetEquipmentForHero(hero);

        // 6) Cierra detalle si está abierto
        if (gearDetailPanel) gearDetailPanel.Hide();

        // 7) Fuerza el layout para que el cambio sea visible en el mismo frame
        Canvas.ForceUpdateCanvases();
        var rt = GetComponent<RectTransform>();
        if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        // 8) Notifica por tu bus existente (NO inventamos RaiseInventoryChanged)
        GearEvents.RaiseHeroGearChanged(heroId);
    }

    // // Añadir dentro de GearEvents (si no está ya)
    // public static event System.Action OnInventoryChanged;

    // public static void RaiseInventoryChanged()
    // {
    //     OnInventoryChanged?.Invoke();
    // }
    

    // === Suscripción al evento global de cambios de equipo ===
    private void OnEnable()
    {
        GearEvents.OnHeroGearChanged += HandleHeroGearChanged;
    }

    private void OnDisable()
    {
        GearEvents.OnHeroGearChanged -= HandleHeroGearChanged;
    }

    // Repinta SOLO si el cambio es del héroe que estamos mostrando
    private void HandleHeroGearChanged(string heroId)
    {
        if (string.IsNullOrEmpty(heroId)) return;
        if (!string.IsNullOrEmpty(currentHeroId) && heroId != currentHeroId) return;

        var pd = GameDataManager.Instance.PlayerData;
        var hero = pd?.heroes?.Find(h => h.heroId == heroId);
        if (hero == null) return;

        // Mantén currentHeroId para callbacks del inventario, etc.
        currentHeroId = heroId;

        // Reutiliza el pipeline de pintado ya existente
        SetEquipmentForHero(hero);
    }


}
