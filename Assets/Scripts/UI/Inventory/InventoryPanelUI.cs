/*
============================================================
InventoryPanelUI.cs — Listado de inventario agrupado por sets
------------------------------------------------------------
PROPÓSITO
- Agrupar gearInventory por setId y renderizar secciones (SetGroupUI).
- Mostrar conteo total X/maxInventory.

REFERENCIAS (INSPECTOR)
- Transform setsContainer, GameObject setGroupPrefab, gearItemPrefab,
  GearDetailPanelUI gearDetailPanel, TextMeshProUGUI countGear, GameObject uiBlockerPanel.

MÉTODOS
- Refresh(PlayerData data=null):
  - Guarda PlayerData si se pasa.
  - Pinta countGear.
  - Agrupa por setId → instancia SetGroupUI.Setup(...).
- ShowGearDetail(GearInstance gear):
  - Abre GearDetailPanelUI.Show(...) y activa uiBlockerPanel.

NOTAS
- Usa HeroCatalogManager.GetSetById para nombres/bonus del set.
============================================================
*/

using UnityEngine;
using UnityEngine.UI; // Para Text
using TMPro; // Para TextMeshProUGUI
using System.Collections.Generic;
using System.Linq;

public class InventoryPanelUI : MonoBehaviour
{
    public Transform setsContainer;
    public GameObject setGroupPrefab;
    public GameObject gearItemPrefab;
    public GearDetailPanelUI gearDetailPanel;
    public int maxInventory = 1000;
    public GameObject uiBlockerPanel;

    [Header("Header UI")]
    public TextMeshProUGUI countGear;

    private PlayerData playerData;

    public void Refresh(PlayerData data = null)
    {
        if (data != null)
            playerData = data;

        if (playerData == null || playerData.gearInventory == null)
        {
            Debug.LogWarning("[InventoryPanelUI] PlayerData o gearInventory es null");
            return;
        }

        if (countGear != null)
            countGear.text = $"{playerData.gearInventory.Count} / {maxInventory}";

        foreach (Transform child in setsContainer)
            Destroy(child.gameObject);

        var gearBySet = playerData.gearInventory
            .GroupBy(g => {
                var gearCat = HeroCatalogManager.Instance.GetGearById(g.gearId);
                return gearCat != null ? gearCat.setId : "desconocido";
            })
            .ToList();

        foreach (var group in gearBySet)
        {
            var setData = HeroCatalogManager.Instance.GetSetById(group.Key);
            string setBonus = "";
            if (setData != null) {
                if (!string.IsNullOrEmpty(setData.bonus2))
                    setBonus += $"2 piezas: {setData.bonus2}";
                if (!string.IsNullOrEmpty(setData.bonus4)) {
                    if (setBonus.Length > 0)
                        setBonus += "\n";
                    setBonus += $"4 piezas: {setData.bonus4}";
                }
            }
            var setGO = Instantiate(setGroupPrefab, setsContainer);
            var setUI = setGO.GetComponent<SetGroupUI>();

            setUI.Setup(
                setId: group.Key,
                setName: setData != null ? setData.setName : group.Key,
                setBonus: setBonus,
                count: group.Count(),
                max: maxInventory,
                onGearItemClicked: ShowGearDetail,
                gears: group.ToList(),
                gearItemPrefab: gearItemPrefab
            );
        }
    }

    void ShowGearDetail(GearInstance gear)
    {
        if (gear == null) return;
        var gearCat = HeroCatalogManager.Instance.GetGearById(gear.gearId);
        if (gearDetailPanel != null && gearCat != null)
        {
            gearDetailPanel.Show(gear, gearCat, null);
            if (uiBlockerPanel != null)
                uiBlockerPanel.SetActive(true);
        }
    }
}