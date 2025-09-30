/*
============================================================
NewGearStatsPanelUI.cs — Panel de detalle para pieza de inventario
------------------------------------------------------------
PROPÓSITO
- Mostrar icono, set, rareza, +nivel, main stat, substats y bonus del set,
  y permitir equipar la pieza en el héroe activo.

REFERENCIAS (INSPECTOR)
- Image iconImage; TMP_Text nameText, rarityText, levelText, mainStatText, subStatsText, setBonusText;
  Button closeButton, btnEquipar.

MÉTODOS
- Show(GearInstance gear, GearCatalog catalog, string slotType, string heroId, List<GearInstance> equippedGear=null, Action onCloseCallback=null):
  Carga icono (Addressables), construye textos y engancha acciones de botones.
- Hide(): desactiva el GO.
- OnBtnEquiparClicked():
  ▶ Resuelve slotType (si no se pasó) con GearUtils.GetSlotTypeFromGearId.
  ▶ Llama GearEquipService.EquipSingle(heroId, gearToEquip, slotType, save:true, raiseEvents:true).
  ▶ Se cierra; los refrescos los hará GearUIRefreshManager.
- GetDisplayName/GetSetName/GetRarityDisplay/GetMainStatValue/GetSubstatsString/GetSetBonus:
  utilidades de presentación.

NOTAS
- El valor de mainStat puede venir de mainStatRanges o del GearInstance si lo guardas.
============================================================
*/

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using System;
using System.Linq;
using System.Collections.Generic;

public class NewGearStatsPanelUI : MonoBehaviour
{
    [Header("UI Campos")]
    public Image iconImage;
    public TMP_Text nameText;
    //public TMP_Text typeText;
    public TMP_Text rarityText;
    public TMP_Text levelText;
    public TMP_Text mainStatText;
    public TMP_Text subStatsText;
    public TMP_Text setBonusText;
    public Button closeButton;
    public Button btnEquipar;

    private GearInstance currentGear;
    private GearCatalog currentCatalog;

    private GearInstance gearToEquip;
    private HeroProgress heroProgress;
    private string heroId;
    private string slotTypeToEquip;

    public void Show(
        GearInstance gear,
        GearCatalog catalog, // O GearCatalog según tu estructura real
        string slotType,
        string heroId, // <-- ESTE CAMPO AÑADIDO
        List<GearInstance> equippedGear = null,
        Action onCloseCallback = null)
    {
        Debug.Log($"[NewGearStatsPanelUI] Show: gear={gear?.gearId}, slotType={slotType}, heroId={heroId}");
        gameObject.SetActive(true);
        currentGear = gear;
        currentCatalog = catalog;
        this.gearToEquip = gear;
        this.heroId = heroId;
        this.slotTypeToEquip = slotType;

        // -- Cargar icono --
        if (iconImage != null && catalog != null && !string.IsNullOrEmpty(catalog.spriteAddressable))
        {
            Addressables.LoadAssetAsync<Sprite>(catalog.spriteAddressable).Completed += handle =>
            {
                if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    iconImage.sprite = handle.Result;
            };
        }

        // -- Mostrar datos --
        if (nameText != null)
            nameText.text = GetDisplayName(gear, catalog);

        //if (typeText != null)
        //    typeText.text = $"{catalog?.type ?? "?"}";

        if (rarityText != null)
            rarityText.text = $"{GetRarityDisplay(gear.rarity)}";

        if (levelText != null)
            levelText.text = $"+{gear.upgradeLevel}";

        if (mainStatText != null)
            mainStatText.text = $"{gear.mainStatType} +{GetMainStatValue(gear, catalog)}";

        if (subStatsText != null)
            subStatsText.text = GetSubstatsString(gear);

        if (setBonusText != null)
            setBonusText.text = $"{GetSetBonus(catalog, equippedGear)}";

        if (btnEquipar != null)
        {
            btnEquipar.onClick.RemoveAllListeners();
            btnEquipar.onClick.AddListener(OnBtnEquiparClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                onCloseCallback?.Invoke();
                Hide();
            });
        }

    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private string GetDisplayName(GearInstance gear, GearCatalog catalog)
    {
        return $"{GetSetName(catalog)}";
    }
    private string GetSetName(GearCatalog catalog)
    {
        if (catalog == null)
        {
            Debug.LogWarning("[NewGearStatsPanelUI] Catálogo de gear es null en GetSetName.");
            return "";
        }
        if (string.IsNullOrEmpty(catalog.setId))
        {
            Debug.LogWarning("[NewGearStatsPanelUI] setId es null o vacío en GetSetName.");
            return "";
        }

        Debug.Log($"[NewGearStatsPanelUI] Buscando nombre del set para setId: '{catalog.setId}'");

        var setEntry = HeroCatalogManager.Instance.GetSetById(catalog.setId);

        if (setEntry == null)
        {
            Debug.LogWarning($"[NewGearStatsPanelUI] No se encontró setEntry para setId: '{catalog.setId}'.");
            return catalog.setId;
        }

        Debug.Log($"[NewGearStatsPanelUI] Nombre del set encontrado para setId '{catalog.setId}': {setEntry.setName}");

        return setEntry.setName;
    }

    private string GetRarityDisplay(string rarity)
    {
        switch (rarity)
        {
            case "mugroso": return "<color=#888888>Mugroso</color>"; // gris
            case "extranito": return "<color=#00FFFF>Extrañito</color>"; // cyan
            case "absurdamente_escaso": return "<color=#A020F0>Absurdamente Escaso</color>"; // purple
            case "divinamente_ridiculo": return "<color=#FFA500>Divinamente Ridículo</color>"; // orange
            case "memeticamente_unico": return "<color=#FF00FF>Meméticamente Único</color>"; // magenta
            default: return rarity;
        }
    }

    private float GetMainStatValue(GearInstance gear, GearCatalog catalog)
    {
        // Busca en mainStatRanges el valor para la rareza y el tipo
        if (catalog != null && catalog.mainStatRanges != null &&
            catalog.mainStatRanges.TryGetValue(gear.rarity, out var statDict) &&
            statDict.TryGetValue(gear.mainStatType.Replace("%", ""), out var rangeList))
        {
            // Aquí podrías devolver el valor generado, si lo tienes en el gearInstance;
            // si no, muestra el rango mínimo-máximo para referencia
            return rangeList.Count > 1 ? rangeList[1] : rangeList[0];
        }
        // Alternativamente, si el valor ya está calculado y guardado en GearInstance, úsalo.
        return 0;
    }

    private string GetSubstatsString(GearInstance gear)
    {
        if (gear?.substats != null && gear.substats.Count > 0)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var sub in gear.substats)
            {
                sb.AppendLine($"{sub.type} +{Mathf.RoundToInt(sub.value)}");
            }
            return sb.ToString();
        }
        return "Sin substats";
    }

    private string GetSetBonus(GearCatalog catalog, List<GearInstance> equippedGear)
    {
        if (catalog == null) return "";
        var setData = HeroCatalogManager.Instance.GetSetById(catalog.setId);
        if (setData == null) return "";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        if (!string.IsNullOrEmpty(setData.bonus2))
            sb.AppendLine($"2 piezas: {setData.bonus2}");
        if (!string.IsNullOrEmpty(setData.bonus4))
            sb.AppendLine($"4 piezas: {setData.bonus4}");
        return sb.ToString();
    }

    private void OnBtnEquiparClicked()
    {
        if (string.IsNullOrEmpty(heroId) || gearToEquip == null)
        {
            Debug.LogWarning("[NewGearStatsPanelUI] Faltan datos para equipar.");
            return;
        }

        string slotType = slotTypeToEquip;
        if (string.IsNullOrEmpty(slotType))
            slotType = GearUtils.GetSlotTypeFromGearId(gearToEquip.gearId);

        GearEquipService.EquipSingle(heroId, gearToEquip, slotType, save: true, raiseEvents: true);
        this.Hide();
    }

}
