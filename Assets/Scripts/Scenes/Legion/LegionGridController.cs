using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;

public class LegionGridController : MonoBehaviour
{
    public enum GroupMode { Elemento = 0, Faccion = 1, Clase = 2, BaseStars = 3 }

    [Header("UI")]
    [SerializeField] private RectTransform contentRoot;      // Content del ScrollView
    [SerializeField] private LegionCardUI heroCardTemplate;  // Plantilla DESACTIVADA

    [SerializeField] private int preferColumns = 6;
    [Tooltip("Ajuste rÃ¡pido: +1 muestra una columna extra, -1 hace las tarjetas mÃ¡s grandes")]
    [SerializeField] private int columnBias = 0;

    [Tooltip("SeparaciÃ³n entre tarjetas")]
    [SerializeField] private Vector2 spacing = new Vector2(16, 16);

    [Tooltip("RelaciÃ³n Alto/Ancho de la tarjeta (320x400 -> 1.25)")]
    [SerializeField] private float cardAspect = 1.25f;

    [Tooltip("Ancho mÃ­nimo y mÃ¡ximo por tarjeta (en px). El algoritmo buscarÃ¡ columnas para mantener el ancho dentro de este rango.")]
    [SerializeField] private Vector2 cardWidthRange = new Vector2(280, 380);
    private readonly List<LegionCardUI> activeCards = new();

    private readonly List<GameObject> spawned = new List<GameObject>();
    private List<HeroCatalogEntry> allHeroes;
    private List<HeroProgress> playerHeroes;

    private const float SECTION_HEADER_HEIGHT = 56f;

    public event System.Action<string> OnHeroSelected; // heroId


    void Awake()
    {
        if (contentRoot == null)
            Debug.LogError("[HeroGrid] Falta asignar contentRoot.");
        if (heroCardTemplate == null)
            Debug.LogError("[HeroGrid] Falta asignar heroCardTemplate.");

        heroCardTemplate.gameObject.SetActive(false);
    }

    void OnRectTransformDimensionsChange()
    {
        // Si cambia tamaÃ±o (rotaciÃ³n, notch, simuladorâ€¦), re-calcula TODAS las rejillas.
        RecalculateAllGrids();
    }

    private void ClearGrid()
    {
        foreach (var card in activeCards)
            if (card != null) Destroy(card.gameObject);
        activeCards.Clear();
    }


    // ===== API =====
    public void BuildSectioned(GroupMode mode)
    {
        LoadDatasets();
        EnsureContentIsVerticalList();
        ClearAll();

        var groups = GroupHeroes(mode, allHeroes);

        foreach (var key in OrderKeys(mode, groups.Keys))
        {
            var sectionGO = CreateHeader(key, showIcon: mode == GroupMode.Elemento);
            spawned.Add(sectionGO);

            var gridRT = CreateGrid(sectionGO.transform);
            FillGrid(gridRT, groups[key]);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        var sr = contentRoot.GetComponentInParent<ScrollRect>();
        if (sr) sr.normalizedPosition = new Vector2(0, 1);

        Debug.Log($"[LegionGrid] Construido modo '{mode}' â†’ {groups.Count} secciones.");
    }

    private IEnumerable<string> OrderKeys(GroupMode mode, IEnumerable<string> keys)
    {
        switch (mode)
        {
            case GroupMode.Elemento:
            {
                var order = new List<string> { "Agua", "Fuego", "Naturaleza", "Luz", "Oscuridad" };
                return keys.OrderBy(k =>
                {
                    int idx = order.IndexOf(k);
                    return idx < 0 ? int.MaxValue : idx;
                });
            }
            case GroupMode.BaseStars:
                return keys.OrderBy(k =>
                {
                    string digits = new string(k.Where(char.IsDigit).ToArray());
                    return int.TryParse(digits, out var x) ? x : 0;
                });
            default:
                return keys.OrderBy(k => k);
        }
    }

    public void PopulateAllHeroes()
    {
        ClearGrid();

        var hcm = HeroCatalogManager.Instance;
        if (hcm == null)
        {
            Debug.LogError("[LegionGrid] HeroCatalogManager.Instance es null.");
            allHeroes = new List<HeroCatalogEntry>();
        }
        else
        {
            // API correcta: .heroes
            allHeroes = hcm.heroes ?? new List<HeroCatalogEntry>();
        }

        var pd = GameDataManager.Instance?.PlayerData;
        playerHeroes = pd?.heroes ?? new List<HeroProgress>();

        foreach (var heroDef in allHeroes)
        {
            var prog = playerHeroes.FirstOrDefault(h => h.heroId == heroDef.heroId);
            CreateCard(heroDef, prog);
        }

        Debug.Log($"[LegionGrid] Grid poblado con {allHeroes.Count} héroes.");
    }

    public void ApplyFilter(FilterData filter)
    {
        if (allHeroes == null) { PopulateAllHeroes(); return; }
        ClearGrid();

        var filtered = allHeroes.Where(h =>
        {
            bool ok = true;
            if (!string.IsNullOrEmpty(filter.element))
                ok &= string.Equals(h.element, filter.element, StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(filter.classStandard))
                ok &= string.Equals(h.classStandard, filter.classStandard, StringComparison.OrdinalIgnoreCase);
            if (filter.stars > 0)
                ok &= h.baseStars == filter.stars;
            return ok;
        }).ToList();

        foreach (var heroDef in filtered)
        {
            var prog = playerHeroes.FirstOrDefault(h => h.heroId == heroDef.heroId);
            CreateCard(heroDef, prog);
        }

        Debug.Log($"[LegionGrid] Filtro aplicado → {filtered.Count} héroes visibles.");
    }

    private void CreateCard(HeroCatalogEntry def, HeroProgress prog)
    {
        var card = Instantiate(heroCardTemplate, contentRoot);
        card.gameObject.SetActive(true);

        bool obtained = prog != null && prog.stars > 0;
        card.Setup(def, prog, obtained, () =>
        {
            Debug.Log($"[LegionGrid] Click en {def.heroId}");
            OnHeroSelected?.Invoke(def.heroId);
        });

        activeCards.Add(card);
    }

    // ===== Internos =====
    private void LoadDatasets()
    {
        var hcm = HeroCatalogManager.Instance;
        allHeroes = (hcm != null && hcm.heroes != null) ? hcm.heroes : new List<HeroCatalogEntry>();

        var pd = GameDataManager.Instance?.PlayerData;
        playerHeroes = pd?.heroes ?? new List<HeroProgress>();
    }

    private void EnsureContentIsVerticalList()
    {
        var g = contentRoot.GetComponent<GridLayoutGroup>();
        if (g) g.enabled = false;

        var vlg = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (!vlg) vlg = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.spacing = 8;
        vlg.padding = new RectOffset(0, 0, 0, 0);

        var fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (!fitter) fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.MinSize;

        contentRoot.anchorMin = new Vector2(0, 1);
        contentRoot.anchorMax = new Vector2(1, 1);
        contentRoot.pivot = new Vector2(0, 1);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = new Vector2(0, 0);
    }

    private void ClearAll()
    {
        for (int i = 0; i < contentRoot.childCount; i++)
            Destroy(contentRoot.GetChild(i).gameObject);
        spawned.Clear();
    }

    private Dictionary<string, List<HeroCatalogEntry>> GroupHeroes(GroupMode mode, List<HeroCatalogEntry> defs)
    {
        IEnumerable<IGrouping<string, HeroCatalogEntry>> ordered;

        switch (mode)
        {
            case GroupMode.Elemento:
                ordered = defs.GroupBy(d => d.element);
                break;

            case GroupMode.Faccion:
                ordered = defs.GroupBy(d => string.IsNullOrEmpty(d.reino) ? "?" : d.reino)
                              .OrderBy(g => g.Key);
                break;

            case GroupMode.Clase:
                ordered = defs.GroupBy(d => string.IsNullOrEmpty(d.classStandard) ? "?" : d.classStandard)
                              .OrderBy(g => g.Key);
                break;

            case GroupMode.BaseStars:
            default:
                ordered = defs.GroupBy(d => (d.baseStars > 0 ? d.baseStars : 0).ToString() + " â­")
                              .OrderBy(g =>
                              {
                                  string digits = new string(g.Key.Where(char.IsDigit).ToArray());
                                  int x; return int.TryParse(digits, out x) ? x : 0;
                              });
                break;
        }

        var dict = new Dictionary<string, List<HeroCatalogEntry>>();
        foreach (var g in ordered)
            dict[g.Key] = g.OrderBy(d => d.displayName).ToList();
        return dict;
    }

    private GameObject CreateHeader(string title, bool showIcon)
    {
        var section = new GameObject("Section", typeof(RectTransform));
        var srt = section.GetComponent<RectTransform>();
        srt.SetParent(contentRoot, false);
        srt.anchorMin = new Vector2(0, 1);
        srt.anchorMax = new Vector2(1, 1);
        srt.pivot = new Vector2(0, 1);
        srt.sizeDelta = new Vector2(0, SECTION_HEADER_HEIGHT);

        var header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(section.transform, false);
        var hrt = header.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0, 1);
        hrt.anchorMax = new Vector2(1, 1);
        hrt.pivot = new Vector2(0, 1);
        hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(0, SECTION_HEADER_HEIGHT);
        header.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.9f);

        Image iconImg = null;
        if (showIcon)
        {
            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(header.transform, false);
            var irt = icon.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 0.5f);
            irt.anchorMax = new Vector2(0, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.sizeDelta = new Vector2(36, 36);
            irt.anchoredPosition = new Vector2(26, 0);
            iconImg = icon.GetComponent<Image>();

            string key = $"Assets/Addressables/Art/HeroScene/Elemento/{title}.png";
            Addressables.LoadAssetAsync<Sprite>(key).Completed += op =>
            {
                if (iconImg == null) return;
                if (op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded && op.Result != null)
                { iconImg.sprite = op.Result; iconImg.color = Color.white; }
                else { iconImg.color = new Color(1, 1, 1, 0); Debug.LogWarning($"[LegionGrid] Sin icono: {key}"); }
            };
        }

        var txtGO = new GameObject("TxtHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(header.transform, false);
        var trt = txtGO.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0, 0.5f);
        trt.anchorMax = new Vector2(1, 0.5f);
        trt.pivot = new Vector2(0, 0.5f);
        trt.sizeDelta = new Vector2(0, SECTION_HEADER_HEIGHT);
        trt.anchoredPosition = new Vector2(showIcon ? 64f : 16f, 0);
        var tmp = txtGO.GetComponent<TextMeshProUGUI>();
        tmp.text = title; tmp.fontSize = 30; tmp.alignment = TextAlignmentOptions.Left; tmp.color = Color.white;

        return section;
    }

    private RectTransform CreateGrid(Transform sectionParent)
    {
        var gridHolder = new GameObject("Grid", typeof(RectTransform));
        gridHolder.transform.SetParent(sectionParent, false);
        var rt = gridHolder.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(0, -SECTION_HEADER_HEIGHT);
        rt.sizeDelta = Vector2.zero;

        var grid = gridHolder.AddComponent<GridLayoutGroup>();
        grid.spacing = spacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

        // Ajuste inicial en funciÃ³n del viewport
        RecalculateGrid(grid);

        return rt;
    }

    private void FillGrid(RectTransform grid, List<HeroCatalogEntry> defs)
    {
        var glg = grid.GetComponent<GridLayoutGroup>();
        if (glg == null) glg = grid.gameObject.AddComponent<GridLayoutGroup>();

        // Asegura layout correcto antes de instanciar
        RecalculateGrid(glg);

        foreach (var def in defs)
        {
            var prog = playerHeroes.FirstOrDefault(h => h.heroId == def.heroId);
            bool obtained = prog != null && prog.stars > 0;

            var card = Instantiate(heroCardTemplate, grid);
            card.gameObject.SetActive(true);
            card.Setup(def, prog, obtained, () => OnHeroSelected?.Invoke(def.heroId));
            spawned.Add(card.gameObject);
        }

        int cols = Mathf.Max(1, glg.constraintCount);
        int rows = Mathf.CeilToInt(defs.Count / (float)cols);

        var pad = glg.padding;
        float h = pad.top + pad.bottom
                  + rows * glg.cellSize.y
                  + Mathf.Max(0, rows - 1) * glg.spacing.y;

        var sd = grid.sizeDelta;
        sd.y = h;
        grid.sizeDelta = sd;

        var sectionRT = grid.parent as RectTransform;
        if (sectionRT != null)
        {
            float totalH = SECTION_HEADER_HEIGHT + h;
            sectionRT.sizeDelta = new Vector2(0, totalH);

            var le = sectionRT.GetComponent<LayoutElement>();
            if (!le) le = sectionRT.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = totalH;
            le.minHeight = totalH;
        }

        spawned.Add(grid.gameObject);
    }

    // ====== Responsive ======
    private void RecalculateAllGrids()
    {
        if (contentRoot == null) return;
        var grids = contentRoot.GetComponentsInChildren<GridLayoutGroup>(true);
        foreach (var g in grids) RecalculateGrid(g);
    }

    private void RecalculateGrid(GridLayoutGroup grid)
    {
        if (grid == null) return;

        // Ancho disponible del viewport (Content estÃ¡ estirado a ancho del Scroll).
        var viewportRT = contentRoot;
        float avail = Mathf.Max(1f, viewportRT.rect.width);

        var pad = grid.padding;
        int cols = Mathf.Max(1, preferColumns + columnBias);

        // Encuentra #cols tal que el ancho de tarjeta quede dentro del rango.
        cols = Mathf.Clamp(cols, 1, 12);
        float minW = Mathf.Min(cardWidthRange.x, cardWidthRange.y);
        float maxW = Mathf.Max(cardWidthRange.x, cardWidthRange.y);

        Func<int, float> widthFor = c =>
        {
            float inner = avail - pad.left - pad.right - spacing.x * (c - 1);
            return inner / c;
        };

        // Reduce/Incrementa columnas para mantener ancho en rango.
        float w = widthFor(cols);
        // Si es demasiado pequeÃ±o -> menos columnas (tarjetas mÃ¡s grandes)
        while (cols > 1 && w < minW) { cols--; w = widthFor(cols); }
        // Si es muy grande -> mÃ¡s columnas (cabe una mÃ¡s)
        while (cols < 12 && w > maxW) { cols++; w = widthFor(cols); }

        float cellW = Mathf.Floor(w);
        float cellH = Mathf.Round(cellW * Mathf.Max(0.5f, cardAspect)); // seguridad

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = cols;
        grid.cellSize = new Vector2(cellW, cellH);
        grid.spacing = spacing;
    }
}
