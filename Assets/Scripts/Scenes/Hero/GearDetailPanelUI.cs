/*
============================================================
GearDetailPanelUI.cs — Panel de detalle de pieza equipada
------------------------------------------------------------
PROPÓSITO
- Mostrar stats, set, y permitir quitar la pieza del héroe.

MÉTODOS (COMPLETA AQUÍ)
- Show(gearInstance, gearCatalog, equipment, heroId, onRemoveCb).
- Hide().
============================================================
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class GearDetailPanelUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text typeText;
    public TMP_Text rarityText;
    public TMP_Text levelText;
    public TMP_Text mainStatText;
    public TMP_Text subStatsText;
    public TMP_Text setBonusText;

    // IMPORTANTES: usa estos nombres para que coincidan con tu escena (BtnRemove / BtnClose)
    public Button btnRemove;
    public Button btnClose;

    [Header("Animación (opcional)")]
    public CanvasGroup canvasGroup; 
    [SerializeField] private float fadeDuration = 0.12f;

    // Estado
    private GearInstance currentGear;
    private GearCatalog currentCatalog;
    private string currentHeroId;
    private Action<GearInstance, string> onRemove; // callback externo (HeroEquipmentPanelUI)
    private Action onClose;

    // ========= API =========
    public void Show(
        GearInstance gear,
        GearCatalog catalog,
        List<GearInstance> equippedGear,
        string heroId = null,
        Action<GearInstance, string> onRemoveCallback = null,
        Action onCloseCallback = null)
    {
        currentGear = gear;
        currentCatalog = catalog;
        currentHeroId = heroId;
        onRemove = onRemoveCallback;
        onClose = onCloseCallback;

        // Activa panel + animación
        gameObject.SetActive(true);
        UIAnimator.Appear(gameObject);

        // --- Relleno UI ---
        if (iconImage != null && catalog != null && !string.IsNullOrEmpty(catalog.spriteAddressable))
        {
            Addressables.LoadAssetAsync<Sprite>(catalog.spriteAddressable).Completed += (handle) =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded)
                    iconImage.sprite = handle.Result;
            };
        }
        if (nameText != null) nameText.text = GetDisplayName(gear, catalog);
        if (typeText != null) typeText.text = $"{catalog?.type ?? "?"}";
        if (rarityText != null) rarityText.text = GetRarityDisplay(gear.rarity);
        if (levelText != null) levelText.text = $"+{gear.upgradeLevel}";
        if (mainStatText != null) mainStatText.text = $"{gear.mainStatType} +{GetMainStatValue(gear, catalog)}";
        if (subStatsText != null) subStatsText.text = GetSubstatsString(gear);
        if (setBonusText != null) setBonusText.text = GetSetBonus(catalog, equippedGear);

        // --- Botones ---
        if (btnRemove != null)
        {
            btnRemove.onClick.RemoveAllListeners();
            btnRemove.onClick.AddListener(OnClickRemove);
            btnRemove.interactable = (currentGear != null && !string.IsNullOrEmpty(currentHeroId));
        }
        if (btnClose != null)
        {
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(() =>
            {
                onClose?.Invoke();
                Hide();
            });
            btnClose.interactable = true;
        }

        // Al frente
        transform.SetAsLastSibling();
    }

    public void Hide()
    {
        if (!gameObject.activeInHierarchy)
        {
            UIModalBlocker.Instance?.Release(this);
            return;
        }

        UIAnimator.Disappear(gameObject);
        UIModalBlocker.Instance?.Release(this);
    }

    // ========= Botón Remove =========
    public void OnClickRemove()
    {
        if (currentGear == null || string.IsNullOrEmpty(currentHeroId))
        {
            Debug.LogWarning("[GearDetailPanelUI] Remove: datos incompletos.");
            return;
        }

        // Si hay callback externo (desde HeroEquipmentPanelUI), úsalo
        if (onRemove != null)
        {
            onRemove.Invoke(currentGear, currentHeroId);
            Hide();
            return;
        }

        // Fallback directo aquí
        var player = GameDataManager.Instance.PlayerData;
        var hero = player.heroes.Find(h => h.heroId == currentHeroId);
        if (hero == null)
        {
            Debug.LogWarning("[GearDetailPanelUI] Remove: héroe no encontrado.");
            return;
        }

        bool removed = hero.equipment != null &&
                       hero.equipment.RemoveAll(g => g.instanceId == currentGear.instanceId) > 0;
        if (!removed)
        {
            Debug.LogWarning("[GearDetailPanelUI] Remove: la pieza no estaba equipada.");
            return;
        }

        if (player.gearInventory == null) player.gearInventory = new List<GearInstance>();
        player.gearInventory.Add(currentGear);

        player.Save();

        // Evento global
        GearEvents.RaiseHeroGearChanged(currentHeroId);

        // -------- Refrescos locales equivalentes a GearUIRefreshManager --------
        // 1) Inventario
        var inv = FindFirstObjectByType<PanelEquiparController>(FindObjectsInactive.Include);
        if (inv != null) inv.PopulateInventory();

        // 2) Panel de equipo del héroe
        var equipPanel = FindFirstObjectByType<HeroEquipmentPanelUI>(FindObjectsInactive.Include);
        if (equipPanel != null) equipPanel.SetEquipment(hero.equipment);

        // 3) Grid de héroes (reconstruye y refresca)
        var grid = FindFirstObjectByType<HeroGridController>(FindObjectsInactive.Include);
        if (grid != null)
        {
            var pd = GameDataManager.Instance.PlayerData;
            grid.SetHeroes(pd.heroes, pd.maxHeroSpaces);
            grid.RefreshGrid(0);
        }

        // 4) Stats preview
        var statsPrev = FindFirstObjectByType<StatsPreviewPanelController>(FindObjectsInactive.Include);
        if (statsPrev != null) statsPrev.ShowStats(currentHeroId);
        // ----------------------------------------------------------------------

        Hide();
    }

    // ========= Helpers UI =========
    private string GetDisplayName(GearInstance gear, GearCatalog catalog) => GetSetName(catalog);

    private string GetSetName(GearCatalog catalog)
    {
        if (catalog == null || string.IsNullOrEmpty(catalog.setId)) return "";
        var setEntry = HeroCatalogManager.Instance.GetSetById(catalog.setId);
        return setEntry == null ? catalog.setId : setEntry.setName;
    }

    private string GetRarityDisplay(string rarity)
    {
        switch (rarity)
        {
            case "mugroso": return "<color=#888888>Mugroso</color>";
            case "extranito": return "<color=#00FFFF>Extrañito</color>";
            case "absurdamente_escaso": return "<color=#A020F0>Absurdamente Escaso</color>";
            case "divinamente_ridiculo": return "<color=#FFA500>Divinamente Ridículo</color>";
            case "memeticamente_unico": return "<color=#FF00FF>Meméticamente Único</color>";
            default: return rarity;
        }
    }

    private string GetSubstatsString(GearInstance gear)
    {
        if (gear?.substats == null || gear.substats.Count == 0) return "Sin substats";
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var stat in gear.substats)
            sb.AppendLine($"{stat.type}: +{Mathf.RoundToInt(stat.value)}");
        return sb.ToString().TrimEnd();
    }

    private float GetMainStatValue(GearInstance gear, GearCatalog catalog)
    {
        if (catalog != null && catalog.mainStatRanges != null &&
            catalog.mainStatRanges.TryGetValue(gear.rarity, out var dict) &&
            dict.TryGetValue(gear.mainStatType.Replace("%", ""), out var range))
            return range.Count > 1 ? range[1] : range[0];
        return 0f;
    }

    private string GetSetBonus(GearCatalog catalog, List<GearInstance> equippedGear)
    {
        if (catalog == null || string.IsNullOrEmpty(catalog.setId) || equippedGear == null) return "";
        string setId = catalog.setId;
        int count = 0;
        foreach (var g in equippedGear)
        {
            if (g == null) continue;
            var gc = HeroCatalogManager.Instance.GetGearById(g.gearId);
            if (gc != null && gc.setId == setId) count++;
        }
        var setEntry = HeroCatalogManager.Instance.GetSetById(setId);
        if (setEntry == null) return "";
        if (count >= 6) return $"<color=#FFD700>2 piezas:</color> {setEntry.bonus2}\n<color=#FFD700>4 piezas:</color> {setEntry.bonus4}";
        if (count >= 4) return $"<color=#FFD700>4 piezas:</color> {setEntry.bonus4}";
        if (count >= 2) return $"<color=#FFD700>2 piezas:</color> {setEntry.bonus2}";
        return "";
    }

    // ========= Animaciones (no usadas, mantenidas por si reactivas CanvasGroup) =========
    private IEnumerator FadeInCoroutine()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOutAndDisable()
    {
        float t = fadeDuration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}
