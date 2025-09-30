/*
============================================================
SetGroupUI.cs — Sección de inventario por set
------------------------------------------------------------
PROPÓSITO
- Encabezado con icono/nombre/bonus y grid expandible de piezas del set.

REFERENCIAS (INSPECTOR)
- Image iconSet; TMP_Text txtSetName, txtSetCount, txtSetBonus;
  Button btnExpandCollapse, btnAllExpandCollapse;
  RectTransform gearSlotsGrid; GameObject gearItemUIEmptyPrefab; GameObject headerBonus.

MÉTODOS
- Setup(string setId, string setName, string setBonus, int count, int max, Action<GearInstance> onGearItemClicked, List<GearInstance> gears, GameObject gearItemPrefab, string setIconAddressable=null, GameObject gearItemUIEmptyPrefab=null):
  Instancia GearItemUIs, asigna callbacks y rellena huecos vacíos.
- SetResumenView(bool resumen): colapsa/expande grid y headerBonus (LayoutElement.ignoreLayout).
- BtnAllExpandCollapse_Click(): colapsa/expande todas las secciones hermanas.
- FindPanelEvenIfInactive<T>(): busca un panel aunque esté inactivo (útil para NewGearStatsPanelUI).
- (priv.) ToggleResumenView(), LoadAddressableIcon(string address).

NOTAS
- Usa Addressables para icono de set si se pasa path.
============================================================
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;

public class SetGroupUI : MonoBehaviour
{
    [Header("Refs (asigna en inspector)")]
    public Image iconSet;
    public TMP_Text txtSetName;
    public TMP_Text txtSetCount;
    public TMP_Text txtSetBonus;
    public Button btnExpandCollapse;
    public Button btnAllExpandCollapse;
    public RectTransform gearSlotsGrid;
    public GameObject gearItemUIEmptyPrefab;
    public GameObject headerBonus; // <--- CAMBIO: Añade este campo y asígnalo en inspector

    private List<GearInstance> gears;
    private GameObject gearItemPrefab;
    private Action<GearInstance> onGearItemClicked;
    private bool isResumen = false;

    void Awake()
    {
        if (btnExpandCollapse != null)
        {
            btnExpandCollapse.onClick.RemoveAllListeners();
            btnExpandCollapse.onClick.AddListener(() => ToggleResumenView());
        }

        if (btnAllExpandCollapse != null)
        {
            btnAllExpandCollapse.onClick.RemoveAllListeners();
            btnAllExpandCollapse.onClick.AddListener(BtnAllExpandCollapse_Click);
        }
    }

    private void ToggleResumenView()
    {
        isResumen = !isResumen;
        SetResumenView(isResumen);
    }

    public void Setup(
        string setId,
        string setName,
        string setBonus,
        int count,
        int max,
        Action<GearInstance> onGearItemClicked,
        List<GearInstance> gears,
        GameObject gearItemPrefab,
        string setIconAddressable = null,
        GameObject gearItemUIEmptyPrefab = null // nuevo param opcional
    )
    {
        this.gears = gears;
        this.gearItemPrefab = gearItemPrefab;
        this.onGearItemClicked = onGearItemClicked;

        if (txtSetName) txtSetName.text = setName;
        if (txtSetBonus) txtSetBonus.text = setBonus;
        if (txtSetCount) txtSetCount.text = $"{count}";

        // Carga icono addressable si se indica ruta (opcional):
        if (iconSet && !string.IsNullOrEmpty(setIconAddressable))
            LoadAddressableIcon(setIconAddressable);

        // Borra hijos antiguos del grid
        foreach (Transform child in gearSlotsGrid)
            Destroy(child.gameObject);

        // Instancia los gears del set
        foreach (var gear in gears)
        {
            var gearItem = Instantiate(gearItemPrefab, gearSlotsGrid);
            var gearItemUI = gearItem.GetComponent<GearItemUI>();
            if (gearItemUI != null)
            {
                gearItemUI.newGearStatsPanel = FindPanelEvenIfInactive<NewGearStatsPanelUI>();

                gearItemUI.Setup(gear, () =>
                {
                    if (onGearItemClicked != null)
                        onGearItemClicked(gear);
                });
            }
        }

        // Instancia huecos vacíos
        if (gearItemUIEmptyPrefab != null)
        {
            int numSlots = 9; // O el máximo de slots por set que uses (ajusta a tus necesidades)
            int emptyCount = Mathf.Max(0, numSlots - gears.Count);
            for (int i = 0; i < emptyCount; i++)
            {
                Instantiate(gearItemUIEmptyPrefab, gearSlotsGrid);
            }
        }
    }

    public static T FindPanelEvenIfInactive<T>() where T : Component
    {
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave)
                continue;
            if (go.scene.isLoaded)
            {
                T comp = go.GetComponent<T>();
                if (comp != null)
                    return comp;
            }
        }
        return null;
    }

    public void SetResumenView(bool resumen)
    {
        isResumen = resumen;
        if (gearSlotsGrid != null)
        {
            gearSlotsGrid.gameObject.SetActive(!resumen);
            var le = gearSlotsGrid.GetComponent<LayoutElement>();
            if (le) le.ignoreLayout = resumen;
        }
        if (headerBonus != null)
        {
            headerBonus.SetActive(!resumen);
            var le = headerBonus.GetComponent<LayoutElement>();
            if (le) le.ignoreLayout = resumen;
        }
        // Fuerza refresh del layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }
    public void BtnAllExpandCollapse_Click()
    {
        var parent = transform.parent;
        if (parent == null) return;

        var allGroups = parent.GetComponentsInChildren<SetGroupUI>(true);
        // Si todos están colapsados -> expande todos. Si hay alguno expandido -> colapsa todos.
        bool expand = true;
        foreach (var group in allGroups)
        {
            if (!group.isResumen) { expand = false; break; }
        }
        foreach (var group in allGroups)
        {
            group.SetResumenView(!expand);
        }
    }



    private void LoadAddressableIcon(string address)
    {
        Addressables.LoadAssetAsync<Sprite>(address).Completed += (handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (iconSet != null)
                    iconSet.sprite = handle.Result;
            }
        };
    }
}
