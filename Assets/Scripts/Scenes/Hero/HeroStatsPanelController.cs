/*
============================================================
HeroStatsPanelController.cs — Panel de stats principales del héroe
------------------------------------------------------------
PROPÓSITO
- Mostrar stats base + equipos, estrellas grandes, iconos de set.

MÉTODOS (COMPLETA AQUÍ)
- SetHero(HeroProgress, HeroCatalogEntry): actualiza textos e iconos.
- UpdateSetIcons(): carga sprites Addressables (libera handle si aplica).
============================================================
*/


using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System;

public class HeroStatsPanelController : MonoBehaviour
{
    // === UI referencias: Info general ===
    [Header("Panel Info General")]
    public TMP_Text nombreHeroeText;
    public TMP_Text nivelHeroeText;
    [Header("Identidad Héroe (PanelInfo)")]
    [SerializeField] private TMP_Text textClaseHeroe;
    [SerializeField] private Image iconClaseHeroe;
    [SerializeField] private TMP_Text textFaccionHeroe;
    [SerializeField] private Image iconFaccionHeroe;
    [SerializeField] private TMP_Text textElementoHeroe;
    [SerializeField] private Image iconElementoHeroe;
    public Slider expBar;
    public TMP_Text expText;
    public Transform estrellasHeroePanel;
    [SerializeField] private string starIconKey = "Assets/Prefabs/StarIcon_big.prefab";
    [Header("Estrellas GRANDES (arrastrar prefabs)")]
    [SerializeField] private GameObject starBigNormalPrefab;   // ⭐ amarilla (grande)
    [SerializeField] private GameObject starBigAwakenPrefab;   // ⭐ morada  (grande)

    // === UI referencias: Stats ===
    [Header("Panel Stats")]
    public TMP_Text hpText;
    public TMP_Text atkText;
    public TMP_Text defText;
    public TMP_Text spdText;
    public TMP_Text tcriText;
    public TMP_Text dcriText;
    public TMP_Text accText;
    public TMP_Text resText;
    public TMP_Text lukText;
    public TMP_Text agiText;
    [Header("PanelInfo · Bonus/Descripción")]

    [SerializeField] private GameObject setBonusPanel;
    [SerializeField] private TMP_Text setBonusText;
    [SerializeField] private TMP_Text gearBonusTxt;
    [SerializeField] private GameObject uiBlockerPanel;
    
    // ==== PanelInfo: Iconos de set ====
    [Header("PanelInfo · Set Icons")]
    [SerializeField] private Transform setIconsPanel;
    //[SerializeField] private GameObject setIconPrefab;
    [SerializeField] private Sprite defaultSetIcon; // opcional fallback
    //private readonly List<GameObject> activeSetIcons = new List<GameObject>();
    [SerializeField] private Image[] setIconSlots;   // ASIGNA EXACTAMENTE 3 Image en el inspector (hijos de SetIconsPanel)
    [SerializeField] private Sprite emptySetIcon;     // Sprite “sombreado” (placeholder). Si no asignas, uso defaultSetIcon
    [SerializeField][Range(0f, 1f)] private float emptyIconAlpha = 0.35f; // Opacidad para estado vacío/sombreado

    // --- Internos ---
    private HeroProgress currentHero;
    private HeroCatalogEntry currentHeroCatalog;
    private HeroProgress lastProgress;
    private HeroCatalogEntry lastCatalog;
    private int _iconsLoadGuard = 0;  // se incrementa para invalidar callbacks antiguos
    
    // Cache y control anti-doble-instanciación de estrellas
    private GameObject starPrefabCached;
    private int starRequestToken = 0;


    // === EXP por nivel ===
    public static int[] ExpPerLevel = {
        0,100,250,450,700,1000,1350,1750,2200,2700,3250,3850,4500,5200,5950,6750,7600,
        8500,9450,10450,11500,12600,13750,14950,16200,17500,18850,20250,21700,23200,24750,
        26350,28000,29700,31450,33250,35100,37000,38950,40950,43000,45100,47250,49450,51700,
        54000,56350,58750,61200,63700,66250,68850,71500,74200,76950,79750,82600,85500,88450,
        91450,94500
    };

    private void Awake()
    {
        if (uiBlockerPanel != null) uiBlockerPanel.SetActive(false);
        if (setBonusPanel != null) setBonusPanel.SetActive(false);

        // Asegura que el “tap fuera” cierre el panel de bonus
        WireBlocker();
    }

    private void WireBlocker()
    {
        if (uiBlockerPanel == null) return;

        var btn = uiBlockerPanel.GetComponent<Button>();
        if (btn == null) btn = uiBlockerPanel.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            if (setBonusPanel != null) setBonusPanel.SetActive(false);
            uiBlockerPanel.SetActive(false);
        });

        uiBlockerPanel.SetActive(false);
    }

    // =======================
    //  SETS (iconos y bonus)
    // =======================
    // Construye los iconos de set a partir del equipo del héroe y delega la carga de sprites
    // CARGA DE ICONOS DE SET (versión segura: sin doble Release y con guard)
    public void UpdateSetIcons(string[] addressableKeys, Image[] targetImages)
    {
        if (addressableKeys == null || targetImages == null) return;

        // Guard para invalidar callbacks antiguos si cambia el héroe/panel
        int guard = _iconsLoadGuard;

        int n = Mathf.Min(addressableKeys.Length, targetImages.Length);
        for (int i = 0; i < n; i++)
        {
            string key = addressableKeys[i];
            Image img = targetImages[i];

            if (string.IsNullOrEmpty(key) || img == null)
                continue;

            var handle = Addressables.LoadAssetAsync<Sprite>(key);
            handle.Completed += op =>
            {
                try
                {
                    // Si ha cambiado el héroe/panel mientras cargaba, no apliques el resultado
                    if (guard != _iconsLoadGuard) return;

                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        // Evita tocar un Image destruido
                        if (img != null)
                            img.sprite = op.Result;
                    }
                    else
                    {
                        Debug.LogWarning($"[HeroStatsPanel] No se encontró sprite para key: {key}");
                    }
                }
                finally
                {
                    // Liberamos SIEMPRE aquí, y NO volvemos a liberar en ningún otro lado
                    if (op.IsValid()) Addressables.Release(op);
                }
            };
        }
    }


    public void UpdateSetIcons(HeroProgress heroProgress)
    {
        // Invalida callbacks antiguos
        _iconsLoadGuard++;
        int guard = _iconsLoadGuard;

        if (setIconSlots == null || setIconSlots.Length < 3)
        {
            Debug.LogWarning("[HeroStatsPanel] Asigna EXACTAMENTE 3 Image en 'setIconSlots' (hijos de SetIconsPanel).");
            return;
        }

        // 1) Reset: 3 slots sombreados + sin click
        for (int i = 0; i < setIconSlots.Length; i++)
        {
            var img = setIconSlots[i];
            if (!img) continue;

            // El icono nunca debe bloquear el click del botón
            var btnOnSameGO = img.GetComponent<Button>() != null;
            img.raycastTarget = btnOnSameGO;

            img.sprite = emptySetIcon != null ? emptySetIcon : defaultSetIcon;
            img.color = new Color(1f, 1f, 1f, emptyIconAlpha);

            var btn = FindSiblingButton(img);
            if (btn)
            {
                btn.onClick.RemoveAllListeners();
                btn.interactable = false;          // solo se activará si hay icono real
            }
        }

        // Si no hay héroe, dejamos todo sombreado
        if (heroProgress == null)
        {
            if (setBonusText) setBonusText.text = "";
            return;
        }

        // 2) Recuento de piezas por set
        var setCounts = new Dictionary<string, int>();
        var equipment = heroProgress.equipment ?? new List<GearInstance>();
        foreach (var g in equipment)
        {
            var def = HeroCatalogManager.Instance?.GetGearById(g.gearId);
            if (def == null || string.IsNullOrEmpty(def.setId)) continue;

            if (!setCounts.ContainsKey(def.setId)) setCounts[def.setId] = 0;
            setCounts[def.setId]++;
        }

        // 3) Decide qué iconos mostrar (máx. 3) — un icono cada 2 piezas del set
        var toShow = new List<(string key, string setId, int pieces)>();
        foreach (var kv in setCounts)
        {
            var setId = kv.Key;
            var pieces = kv.Value;
            var setDef = HeroCatalogManager.Instance?.GetSetById(setId);
            if (setDef == null) continue;

            int iconCount = Mathf.Min(3 - toShow.Count, pieces / 2);
            for (int i = 0; i < iconCount; i++)
            {
                string key = $"Assets/Addressables/Heroes/Gear/_Icons/{setDef.setName}.png";
                toShow.Add((key, setId, pieces));
                if (toShow.Count >= 3) break;
            }
            if (toShow.Count >= 3) break;
        }

        // 4) Rellena los slots 0..N con iconos reales y habilita el botón
        for (int i = 0; i < toShow.Count && i < setIconSlots.Length; i++)
        {
            var slotImg = setIconSlots[i];
            if (!slotImg) continue;

            // Asegura de nuevo que el icono no bloquee clicks
            slotImg.raycastTarget = false;

            string key = toShow[i].key;
            string setId = toShow[i].setId;
            int pieces = toShow[i].pieces;

            var btn = FindSiblingButton(slotImg);

            var handle = Addressables.LoadAssetAsync<Sprite>(key);
            handle.Completed += op =>
            {
                try
                {
                    if (guard != _iconsLoadGuard) return; // Cambió el héroe/panel en mitad de la carga

                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        if (slotImg != null)
                        {
                            slotImg.sprite = op.Result;
                            slotImg.color = Color.white; // activo

                            if (btn)
                            {
                                btn.onClick.RemoveAllListeners();
                                btn.interactable = true;
                                btn.onClick.AddListener(() => ShowSetBonus(setId, pieces));
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[HeroStatsPanel] No se encontró sprite para key: {key}");
                    }
                }
                finally
                {
                    if (op.IsValid()) Addressables.Release(op);
                }
            };
        }

        // 5) Texto resumen de bonus
        UpdateBonusSummaryText(setCounts);
    }



    // Devuelve el Button hermano dentro del mismo SetSlot (Image y Button son hijos del mismo padre)
    private Button FindSiblingButton(Image img)
    {
        if (img == null) return null;
        var parent = img.transform.parent;
        if (parent == null) return null;

        // Busca cualquier Button dentro del mismo slot (incluye inactivos)
        var btns = parent.GetComponentsInChildren<Button>(true);
        foreach (var b in btns)
        {
            if (b != null && b.gameObject != img.gameObject)
                return b;
        }
        return null;
    }



    private void UpdateBonusSummaryText(Dictionary<string, int> setCounts)
    {
        if (setBonusText == null) return;

        var sb = new StringBuilder();
        foreach (var kv in setCounts)
        {
            if (kv.Value < 2) continue;
            var setDef = HeroCatalogManager.Instance.GetSetById(kv.Key);
            if (setDef == null) continue;

            sb.AppendLine($"<b>{setDef.setName}</b>");
            if (kv.Value >= 2 && !string.IsNullOrEmpty(setDef.bonus2)) sb.AppendLine($"(2) {setDef.bonus2}");
            if (kv.Value >= 4 && !string.IsNullOrEmpty(setDef.bonus4)) sb.AppendLine($"(4) {setDef.bonus4}");
            sb.AppendLine();
        }
        setBonusText.text = sb.ToString().TrimEnd('\n', '\r');
    }

    private void ShowSetBonus(string setId, int pieces)
    {
        var setDef = HeroCatalogManager.Instance.GetSetById(setId);
        if (setDef == null) return;

        string bonusText = "";
        if (pieces >= 6)
        {
            if (!string.IsNullOrEmpty(setDef.bonus2)) bonusText += $"(2) {setDef.bonus2}\n";
            if (!string.IsNullOrEmpty(setDef.bonus4)) bonusText += $"(4) {setDef.bonus4}\n";
        }
        else if (pieces >= 4)
        {
            if (!string.IsNullOrEmpty(setDef.bonus4)) bonusText += $"(4) {setDef.bonus4}\n";
        }
        else if (pieces >= 2)
        {
            if (!string.IsNullOrEmpty(setDef.bonus2)) bonusText += $"(2) {setDef.bonus2}\n";
        }

        if (setBonusText != null) setBonusText.text = bonusText.TrimEnd('\n');

        if (setBonusPanel != null) setBonusPanel.SetActive(true);
        if (uiBlockerPanel != null) uiBlockerPanel.SetActive(true);
    }

    private void ShowSetBonusForSet(string setId, int pieces)
    {
        if (setBonusPanel == null || gearBonusTxt == null) return;

        var setInfo = HeroCatalogManager.Instance.GetSetById(setId);
        string bonus2 = setInfo != null ? setInfo.bonus2 : "+Bonus (2 piezas)";
        string bonus4 = setInfo != null ? setInfo.bonus4 : "+Bonus (4 piezas)";

        // Reglas:
        // - 2 piezas  => (2)
        // - 4 piezas  => (4)
        // - 6 piezas  => (2) y (4)
        var sb = new StringBuilder();
        if (pieces >= 2) sb.AppendLine($"(2) {bonus2}");
        if (pieces >= 4) sb.AppendLine($"(4) {bonus4}");

        gearBonusTxt.text = sb.ToString().TrimEnd();

        // Mostrar panel + activar blocker
        setBonusPanel.SetActive(true);
        if (uiBlockerPanel != null) uiBlockerPanel.SetActive(true);
    }

    public void ShowChildPanels()
    {
        foreach (Transform child in transform)
        {
            if (setBonusPanel != null && child.gameObject == setBonusPanel) continue;
            if (uiBlockerPanel != null && child.gameObject == uiBlockerPanel) continue;

            child.gameObject.SetActive(true);
        }

        // Reafirma estado oculto
        if (setBonusPanel != null) setBonusPanel.SetActive(false);
        if (uiBlockerPanel != null) uiBlockerPanel.SetActive(false);
    }

    // =======================
    //  Identidad y elementos
    // =======================
    private void UpdateIdentityInfo()
    {
        if (currentHeroCatalog == null) return;

        if (textClaseHeroe != null) textClaseHeroe.text = currentHeroCatalog.classStandard;
        if (textFaccionHeroe != null) textFaccionHeroe.text = currentHeroCatalog.reino;
        if (textElementoHeroe != null) textElementoHeroe.text = currentHeroCatalog.element;

        SetIconByAddressable(iconClaseHeroe, GetClassIcon(currentHeroCatalog.classStandard));
        SetIconByAddressable(iconFaccionHeroe, GetFactionIcon(currentHeroCatalog.reino));
        SetIconByAddressable(iconElementoHeroe, GetElementIcon(currentHeroCatalog.element));
    }

    // ===== Helpers de nivel =====
    public static int GetLevelForExp(int exp)
    {
        for (int i = ExpPerLevel.Length - 1; i >= 0; i--)
            if (exp >= ExpPerLevel[i]) return i;
        return 0;
    }

    public static float GetLevelProgress(int exp)
    {
        int level = GetLevelForExp(exp);
        if (level >= ExpPerLevel.Length - 1) return 1f;
        int expCurrentLevel = ExpPerLevel[level];
        int expNextLevel = ExpPerLevel[level + 1];
        int expInLevel = exp - expCurrentLevel;
        int expForLevel = expNextLevel - expCurrentLevel;
        return expForLevel > 0 ? (float)expInLevel / expForLevel : 1f;
    }
    // --- Estrellas ---
    public void SetStars(int starCount)
    {
        // Token para invalidar cargas anteriores si SetHero() se llama varias veces muy rápido
        int myToken = ++starRequestToken;

        // Si ya tenemos el prefab cacheado, reconstruimos sin cargas async
        if (starPrefabCached != null)
        {
            RebuildStars(starCount);
            return;
        }

        string key = string.IsNullOrEmpty(starIconKey)
            ? "Assets/Prefabs/StarIcon_big.prefab"
            : starIconKey;

        var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(key);
        handle.Completed += op =>
        {
            if (op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                // Cacheamos el prefab para futuras llamadas
                starPrefabCached = op.Result;

                // Solo aplica si este callback corresponde al último SetStars solicitado
                if (myToken == starRequestToken)
                    RebuildStars(starCount);
            }
            else
            {
                Debug.LogError("[HeroStatsPanelController] No se pudo cargar el prefab de estrella Addressable con key: " + key);
            }
        };
    }

    private IEnumerator ShowStarsCoroutine(int starCount)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(starIconKey);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject starPrefab = handle.Result;
            for (int i = 0; i < starCount; i++)
            {
                Instantiate(starPrefab, estrellasHeroePanel);
            }
        }
        else
        {
            Debug.LogError("No se pudo cargar el prefab de estrella Addressable con key: " + starIconKey);
        }
        Addressables.Release(handle);
    }

    // =======================
    //  Stats con equipo
    // =======================
    private string GetStatWithEquipmentBonus(HeroProgress hero, string statType, int baseValue)
    {
        var equipment = hero.equipment ?? new List<GearInstance>();
        int flatBonus = 0;
        int percentBonus = 0;

        foreach (var item in equipment)
        {
            if (IsMatchingStat(item.mainStatType, statType))
            {
                if (IsPercentStat(item.mainStatType)) percentBonus += item.upgradeLevel;
                else flatBonus += item.upgradeLevel;
            }

            if (item.substats != null)
            {
                foreach (var sub in item.substats)
                {
                    if (IsMatchingStat(sub.type, statType))
                    {
                        if (IsPercentStat(sub.type)) percentBonus += Mathf.RoundToInt(sub.value);
                        else flatBonus += Mathf.RoundToInt(sub.value);
                    }
                }
            }
        }

        int percentBonusValue = Mathf.RoundToInt(baseValue * percentBonus / 100f);

        var sb = new StringBuilder();
        sb.Append($"{baseValue}");
        if (flatBonus > 0) sb.Append($" + {flatBonus} (plano)");
        if (percentBonus > 0) sb.Append($" + {percentBonusValue} (porc.)");

        return sb.ToString();
    }
    // ¿Qué es un stat porcentual?
    private bool IsPercentStat(string statType) =>
        statType == "atk%" || statType == "hp%" || statType == "def%";

    private bool IsMatchingStat(string a, string b)
    {
        string Clean(string s) => s.ToLowerInvariant().Replace("%", "");
        return Clean(a) == Clean(b);
    }

    // === MÉTODO PRINCIPAL: Actualizar el panel con TODO ===
    // =======================
    //  API PRINCIPAL
    // =======================
    public void SetHero(HeroProgress heroProgress, HeroCatalogEntry heroCatalog)
    {
        currentHero = heroProgress;
        currentHeroCatalog = heroCatalog;

        // Nivel / EXP
        int exp = heroProgress.exp;
        int level = GetLevelForExp(exp);
        float progress = GetLevelProgress(exp);
        int expCurrentLevel = ExpPerLevel[level];
        int expNextLevel = level < ExpPerLevel.Length - 1 ? ExpPerLevel[level + 1] : ExpPerLevel[level];
        int expInLevel = exp - expCurrentLevel;
        int expForLevel = expNextLevel - expCurrentLevel;

        if (expBar != null) expBar.value = progress;
        if (expText != null)
        {
            float percent = expForLevel > 0 ? (float)expInLevel / expForLevel * 100f : 100f;
            expText.text = $"{expInLevel} / {expForLevel}   ({percent:0}%)";
        }

        // Nombre (usa awakenName si está despierto)
        if (nombreHeroeText != null)
        {
            string display = heroCatalog.displayName;
            if (heroProgress.awaken && !string.IsNullOrEmpty(heroCatalog.awakenName))
                display = heroCatalog.awakenName;
            nombreHeroeText.text = display;
        }

        if (nivelHeroeText != null) nivelHeroeText.text = $"{heroProgress.level}";

        // Identidad (clase/facción/elemento)
        UpdateIdentityInfo();
        _iconsLoadGuard++;     // invalida callbacks previos
        // Iconos de set + resumen de bonus
        UpdateSetIcons(heroProgress);

        // Estrellas GRANDES (amarillas o moradas según awaken)
        RenderBigStars(heroProgress.stars, heroProgress.awaken);

        // Stats base + equipo
        if (heroCatalog.stats != null)
        {
            var s = heroCatalog.stats;
            if (hpText != null) hpText.text = GetStatWithEquipmentBonus(heroProgress, "hp", s.baseHP);
            if (atkText != null) atkText.text = GetStatWithEquipmentBonus(heroProgress, "atk", s.baseATK);
            if (defText != null) defText.text = GetStatWithEquipmentBonus(heroProgress, "def", s.baseDEF);
            if (spdText != null) spdText.text = GetStatWithEquipmentBonus(heroProgress, "spd", s.baseSPD);
            if (tcriText != null) tcriText.text = GetStatWithEquipmentBonus(heroProgress, "tcri", s.baseTCRI);
            if (dcriText != null) dcriText.text = GetStatWithEquipmentBonus(heroProgress, "dcri", s.baseDCRI);
            if (accText != null) accText.text = GetStatWithEquipmentBonus(heroProgress, "acc", s.baseACC);
            if (resText != null) resText.text = GetStatWithEquipmentBonus(heroProgress, "res", s.baseRES);
            if (lukText != null) lukText.text = GetStatWithEquipmentBonus(heroProgress, "luk", s.baseLUK);
            if (agiText != null) agiText.text = GetStatWithEquipmentBonus(heroProgress, "agi", s.baseAGI);
        }

        if (uiBlockerPanel != null) uiBlockerPanel.SetActive(false);
        if (setBonusPanel != null) setBonusPanel.SetActive(false);
    }
    private void RenderBigStars(int count, bool isAwaken)
    {
        if (estrellasHeroePanel == null) return;

        for (int i = estrellasHeroePanel.childCount - 1; i >= 0; i--)
            DestroyImmediate(estrellasHeroePanel.GetChild(i).gameObject);

        GameObject prefab = isAwaken ? starBigAwakenPrefab : starBigNormalPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("[HeroStatsPanel] Asigna starBigNormalPrefab y starBigAwakenPrefab en el Inspector.");
            return;
        }

        count = Mathf.Max(0, count);
        for (int i = 0; i < count; i++)
            Instantiate(prefab, estrellasHeroePanel);
    }


    public void HideSetBonus()
    {
        if (setBonusPanel != null) setBonusPanel.SetActive(false);
        if (uiBlockerPanel != null) uiBlockerPanel.SetActive(false);
    }
    private void EnsureBonusBlockerOn()
    {
        if (setBonusPanel != null && setBonusPanel.activeSelf && uiBlockerPanel != null && !uiBlockerPanel.activeSelf)
            uiBlockerPanel.SetActive(true);
    }

    public void ResetStarsPanel()
    {
        if (estrellasHeroePanel == null) return;
        for (int i = estrellasHeroePanel.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(estrellasHeroePanel.GetChild(i).gameObject);
        }
    }

    private void RebuildStars(int starCount)
    {
        if (estrellasHeroePanel == null) return;

        // Limpia siempre antes de instanciar
        for (int i = estrellasHeroePanel.childCount - 1; i >= 0; i--)
            DestroyImmediate(estrellasHeroePanel.GetChild(i).gameObject);

        if (starPrefabCached == null) return;

        for (int i = 0; i < starCount; i++)
            Instantiate(starPrefabCached, estrellasHeroePanel);
    }

    private string GetClassIcon(string classStandard)
    {
        switch (classStandard)
        {
            case "Tanque": return "Assets/Addressables/Art/HeroScene/Clase/Tank Icon.png";
            case "Especialista": return "Assets/Addressables/Art/HeroScene/Clase/Specialist Icon.png";
            case "Sanador": return "Assets/Addressables/Art/HeroScene/Clase/Healer Icon.png";
            case "Atacante": return "Assets/Addressables/Art/HeroScene/Clase/dps Icon.png";
            default: return null;
        }
    }

    private string GetElementIcon(string element)
    {
        switch (element)
        {
            case "Luz": return "Assets/Addressables/Art/HeroScene/Elemento/Luz.png";
            case "Oscuridad": return "Assets/Addressables/Art/HeroScene/Elemento/Oscuridad.png";
            case "Fuego": return "Assets/Addressables/Art/HeroScene/Elemento/Fuego.png";
            case "Naturaleza": return "Assets/Addressables/Art/HeroScene/Elemento/Naturaleza.png";
            case "Agua": return "Assets/Addressables/Art/HeroScene/Elemento/Agua.png";
            default: return null;
        }
    }

    private string GetFactionIcon(string reino)
    {
        if (string.IsNullOrEmpty(reino))
            return "Assets/Addressables/Heroes/Faccion/default_faction.png";
        return $"Assets/Addressables/Art/HeroScene/Faccion/{reino}.png";
    }
    private void SetIconByAddressable(Image target, string key)
    {
        if (target == null || string.IsNullOrEmpty(key)) return;
        Addressables.LoadAssetAsync<Sprite>(key).Completed += h =>
        {
            if (h.Status == AsyncOperationStatus.Succeeded) target.sprite = h.Result;
        };
    }
    // Libera todos los handles actuales (se usa en refrescos y OnDisable/OnDestroy)


    private void OnDisable()
    {
        _iconsLoadGuard++;
    }


    private void OnDestroy()
    {
        _iconsLoadGuard++;
    }

}
