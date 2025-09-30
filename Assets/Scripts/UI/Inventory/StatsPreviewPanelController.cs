/*
============================================================
StatsPreviewPanelController.cs — Panel de previsualización/comparativa de stats
------------------------------------------------------------
PROPÓSITO
- Mostrar stats actuales (base + bonus) y la diferencia ante una pieza
  seleccionada o propuesta simulada.

REFERENCIAS (INSPECTOR)
- TMP_Text por cada stat (hp/atk/def/spd/tcri/dcri/acc/res/luk/agi);
  GameObject panelRoot; Button btnClose.

DEPENDENCIAS
- StatsService (cálculo), StatsFormatter (formateo), GameDataManager, HeroCatalogManager.

MÉTODOS
- Awake(): Hook a btnClose → Hide.
- ShowStats(string heroId):
  Carga HeroProgress y HeroCatalogEntry; usa SetStatText por cada stat para “base + bonus”.
- ShowStatsWithGearPreview(string heroId, GearInstance selectedGear):
  Simula equipar selectedGear (sustituyendo por slot con GearUtils) y pinta difs con SetOneDiff.
- ShowStatsAbsoluteWithEquipment(string heroId, List<GearInstance> equipmentOverride):
  Muestra totales con diff vs equipo actual (para propuestas completas).
- Hide(): oculta panelRoot.
- OnEnable(): re-render si teníamos lastProgress/lastCatalog.

HELPERS (refactor centralizado)
- SetStatText(stat, field, catalog, equip, asPercent=false):
  Usa StatsService.GetBaseStat/ GetTotalStat y StatsFormatter.BasePlusBonus.
- SetDiffTexts(catalog, equipCurrent, equipNew):
  Llama SetOneDiff para cada stat.
- SetOneDiff(stat, field, catalog, equipCurrent, equipNew, asPercent):
  Usa StatsFormatter.CurrentWithDiff(cur, diff, asPercent).
- GetStatTextField(string stat): mapea al TMP_Text correcto.

NOTAS
- Para % (tcri/dcri) formatea con sufijo “%”.
============================================================
*/

using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsPreviewPanelController : MonoBehaviour
{
    [Header("Referencias UI")]
    public TMP_Text txtHp;
    public TMP_Text txtAtk;
    public TMP_Text txtDef;
    public TMP_Text txtSpd;
    public TMP_Text txtCritRate;
    public TMP_Text txtCritDmg;
    public TMP_Text txtAcc;
    public TMP_Text txtRes;
    public TMP_Text txtLuk;
    public TMP_Text txtAgi;

    [Header("Panel entero")]
    public GameObject panelRoot;
    public Button btnClose;

    private HeroProgress lastProgress;
    private HeroCatalogEntry lastCatalog;

    private void Awake()
    {
        if (btnClose != null)
            btnClose.onClick.AddListener(Hide);
        Hide();
    }

    public void ShowStats(string heroId)
    {
        Debug.Log("[StatsPreviewPanelController] ShowStats llamado con heroId: " + heroId);

        if (string.IsNullOrEmpty(heroId)) { Hide(); return; }

        var playerData = GameDataManager.Instance?.PlayerData;
        if (playerData == null) { Hide(); return; }

        var heroProgress = playerData.heroes.Find(h => h.heroId == heroId);
        if (heroProgress == null) { Hide(); return; }
        lastProgress = heroProgress;

        var heroCatalog = HeroCatalogManager.Instance?.GetHeroById(heroId);
        if (heroCatalog == null) { Hide(); return; }
        lastCatalog = heroCatalog;

        // Render todos los stats con servicio centralizado
        SetStatText("hp", txtHp, heroCatalog, heroProgress.equipment);
        SetStatText("atk", txtAtk, heroCatalog, heroProgress.equipment);
        SetStatText("def", txtDef, heroCatalog, heroProgress.equipment);
        SetStatText("spd", txtSpd, heroCatalog, heroProgress.equipment);
        SetStatText("tcri", txtCritRate, heroCatalog, heroProgress.equipment, asPercent: true);
        SetStatText("dcri", txtCritDmg, heroCatalog, heroProgress.equipment, asPercent: true);
        SetStatText("acc", txtAcc, heroCatalog, heroProgress.equipment);
        SetStatText("res", txtRes, heroCatalog, heroProgress.equipment);
        SetStatText("luk", txtLuk, heroCatalog, heroProgress.equipment);
        SetStatText("agi", txtAgi, heroCatalog, heroProgress.equipment);

        if (panelRoot != null) panelRoot.SetActive(true);
    }
    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private string GetStatWithEquipmentBonus(HeroProgress hero, string statType, int baseValue)
    {
        var equipment = hero.equipment ?? new List<GearInstance>();

        int flatBonus = 0;
        int percentBonus = 0;

        foreach (var item in equipment)
        {
            // Main stat
            if (IsMatchingStat(item.mainStatType, statType))
            {
                if (IsPercentStat(item.mainStatType))
                    percentBonus += item.upgradeLevel;
                else
                    flatBonus += item.upgradeLevel;
            }

            // Substats
            if (item.substats != null)
            {
                foreach (var sub in item.substats)
                {
                    if (IsMatchingStat(sub.type, statType))
                    {
                        if (IsPercentStat(sub.type))
                            percentBonus += Mathf.RoundToInt(sub.value);
                        else
                            flatBonus += Mathf.RoundToInt(sub.value);
                    }
                }
            }
        }

        int percentBonusValue = Mathf.RoundToInt(baseValue * percentBonus / 100f);
        int totalBonus = flatBonus + percentBonusValue;

        // Para stats de tipo tcri y dcri, añade el % y color al valor base y al bonus
        if (statType == "tcri" || statType == "dcri")
        {
            if (totalBonus > 0)
                return $"{baseValue}%  <color=#FFD700>+{totalBonus}%</color>";
            else
                return $"{baseValue}%";
        }
        else
        {
            if (totalBonus > 0)
                return $"{baseValue}  <color=#FFD700>+{totalBonus}</color>";
            else
                return $"{baseValue}";
        }
    }
    private bool IsPercentStat(string statType)
    {
        return GearUtils.IsPercentStatKey(statType);
    }

    private bool IsMatchingStat(string statType1, string statType2)
    {
        return GearUtils.IsSameStat(statType1, statType2);
    }

    private void OnEnable()
    {
        if (lastProgress != null && lastCatalog != null)
            ShowStats(lastProgress.heroId);
    }

    public void ShowStatsWithGearPreview(string heroId, GearInstance selectedGear)
    {
        if (string.IsNullOrEmpty(heroId) || selectedGear == null)
        {
            Hide();
            return;
        }

        var playerData = GameDataManager.Instance?.PlayerData;
        if (playerData == null)
        {
            Hide();
            return;
        }

        var heroProgress = playerData.heroes.Find(h => h.heroId == heroId);
        if (heroProgress == null)
        {
            Hide();
            return;
        }
        lastProgress = heroProgress;

        var heroCatalog = HeroCatalogManager.Instance?.GetHeroById(heroId);
        if (heroCatalog == null)
        {
            Hide();
            return;
        }
        lastCatalog = heroCatalog;

        // Simulación sustituyendo por slot
        var simulatedEquipment = new List<GearInstance>(heroProgress.equipment ?? new List<GearInstance>());
        string slotTypeToEquip = GearUtils.GetSlotTypeFromGearId(selectedGear.gearId);

        int slotIndex = simulatedEquipment.FindIndex(g =>
            g != null && GearUtils.GetSlotTypeFromGearId(g.gearId) == slotTypeToEquip);

        if (slotIndex >= 0)
            simulatedEquipment[slotIndex] = selectedGear;
        else
            simulatedEquipment.Add(selectedGear);

        // Dif por stat
        var allStats = new[] { "hp", "atk", "def", "spd", "tcri", "dcri", "acc", "res", "luk", "agi" };
        foreach (var stat in allStats)
        {
            var textField = GetStatTextField(stat);
            if (textField == null) continue;

            int baseValue = StatsService.GetBaseStat(heroCatalog, stat);
            int valorActual = StatsService.GetTotalStat(heroCatalog, heroProgress.equipment ?? new List<GearInstance>(), stat);
            int valorPreview = StatsService.GetTotalStat(heroCatalog, simulatedEquipment, stat);
            int diff = valorPreview - valorActual;

            if (stat == "tcri" || stat == "dcri")
            {
                if (diff > 0) textField.text = $"{valorActual}% <color=green>+{diff}%</color>";
                else if (diff < 0) textField.text = $"{valorActual}% <color=red>{diff}%</color>";
                else textField.text = $"{valorActual}%";
            }
            else
            {
                if (diff > 0) textField.text = $"{valorActual} <color=green>+{diff}</color>";
                else if (diff < 0) textField.text = $"{valorActual} <color=red>{diff}</color>";
                else textField.text = $"{valorActual}";
            }
        }

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }



    private TMP_Text GetStatTextField(string stat)
    {
        switch (stat)
        {
            case "hp": return txtHp;
            case "atk": return txtAtk;
            case "def": return txtDef;
            case "spd": return txtSpd;
            case "tcri": return txtCritRate;
            case "dcri": return txtCritDmg;
            case "acc": return txtAcc;
            case "res": return txtRes;
            case "luk": return txtLuk;
            case "agi": return txtAgi; // <-- faltaba
            default: return null;
        }
    }


    private int GetBaseStat(HeroCatalogEntry catalog, string stat)
    {
        if (catalog?.stats == null) return 0;
        switch (stat)
        {
            case "hp": return catalog.stats.baseHP;
            case "atk": return catalog.stats.baseATK;
            case "def": return catalog.stats.baseDEF;
            case "spd": return catalog.stats.baseSPD;
            case "tcri": return catalog.stats.baseTCRI;
            case "dcri": return catalog.stats.baseDCRI;
            case "acc": return catalog.stats.baseACC;
            case "res": return catalog.stats.baseRES;
            case "luk": return catalog.stats.baseLUK;
            default: return 0;
        }
    }


    private int GetStatTotal(List<GearInstance> equipment, string statType, int baseValue)
    {
        int flatBonus = 0;
        int percentBonus = 0;
        foreach (var item in equipment)
        {
            if (item == null) continue;
            // Main stat
            if (IsMatchingStat(item.mainStatType, statType))
            {
                if (IsPercentStat(item.mainStatType))
                    percentBonus += item.upgradeLevel;
                else
                    flatBonus += item.upgradeLevel;
            }

            // Substats
            if (item.substats != null)
            {
                foreach (var sub in item.substats)
                {
                    if (IsMatchingStat(sub.type, statType))
                    {
                        if (IsPercentStat(sub.type))
                            percentBonus += Mathf.RoundToInt(sub.value);
                        else
                            flatBonus += Mathf.RoundToInt(sub.value);
                    }
                }
            }
        }
        int percentBonusValue = Mathf.RoundToInt(baseValue * percentBonus / 100f);
        int totalBonus = flatBonus + percentBonusValue;
        return baseValue + totalBonus;
    }

    private string GetGearSlotType(GearInstance gear)
    {
        return GearUtils.GetSlotTypeFromGearId(gear?.gearId);
    }

    public void ShowStatsAbsoluteWithEquipment(string heroId, List<GearInstance> equipmentOverride)
    {
        if (string.IsNullOrEmpty(heroId) || equipmentOverride == null) { Hide(); return; }

        var playerData = GameDataManager.Instance?.PlayerData;
        if (playerData == null) { Hide(); return; }

        var heroProgress = playerData.heroes.Find(h => h.heroId == heroId);
        var heroCatalog = HeroCatalogManager.Instance?.GetHeroById(heroId);
        if (heroProgress == null || heroCatalog == null) { Hide(); return; }

        lastProgress = heroProgress; lastCatalog = heroCatalog;

        // Absoluto + dif vs equipo actual
        var currentEquip = heroProgress.equipment ?? new List<GearInstance>();
        SetDiffTexts(heroCatalog, currentEquip, equipmentOverride);

        if (panelRoot != null) panelRoot.SetActive(true);
    }

    // === Helpers NUEVOS ===
    private void SetStatText(string stat, TMP_Text field, HeroCatalogEntry catalog, List<GearInstance> equip, bool asPercent = false)
    {
        if (field == null || catalog == null) return;

        int baseValue  = StatsService.GetBaseStat(catalog, stat);
        int totalValue = StatsService.GetTotalStat(catalog, equip ?? new List<GearInstance>(), stat);
        int bonus      = totalValue - baseValue;

        field.text = StatsFormatter.BasePlusBonus(baseValue, bonus, asPercent);
    }

    private void SetDiffTexts(HeroCatalogEntry catalog, List<GearInstance> equipCurrent, List<GearInstance> equipNew)
    {
        SetOneDiff("hp", txtHp, catalog, equipCurrent, equipNew, asPercent: false);
        SetOneDiff("atk", txtAtk, catalog, equipCurrent, equipNew, asPercent: false);
        SetOneDiff("def", txtDef, catalog, equipCurrent, equipNew, asPercent: false);
        SetOneDiff("spd", txtSpd, catalog, equipCurrent, equipNew, asPercent: false);
        SetOneDiff("tcri", txtCritRate, catalog, equipCurrent, equipNew, asPercent: true);
        SetOneDiff("dcri", txtCritDmg, catalog, equipCurrent, equipNew, asPercent: true);
        SetOneDiff("acc", txtAcc, catalog, equipCurrent, equipNew, asPercent: false);
        SetOneDiff("res", txtRes, catalog, equipCurrent, equipNew, asPercent: false);
        SetOneDiff("luk", txtLuk, catalog, equipCurrent, equipNew, asPercent: false);
        SetOneDiff("agi", txtAgi, catalog, equipCurrent, equipNew, asPercent: false);
    }
    private void SetOneDiff(string stat, TMP_Text field, HeroCatalogEntry catalog, List<GearInstance> equipCurrent, List<GearInstance> equipNew, bool asPercent)
    {
        if (field == null || catalog == null) return;

        int cur = StatsService.GetTotalStat(catalog, equipCurrent ?? new List<GearInstance>(), stat);
        int nxt = StatsService.GetTotalStat(catalog, equipNew    ?? new List<GearInstance>(), stat);
        int diff = nxt - cur;

        field.text = StatsFormatter.CurrentWithDiff(cur, diff, asPercent);
    }


}
