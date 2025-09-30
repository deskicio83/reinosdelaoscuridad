// Assets/Scripts/Scenes/Hero/MasteryPagerController.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class MasteryPagerController : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [Header("Pager")]
    [SerializeField] private ScrollRect scrollRect;
    //[SerializeField] private Scrollbar pageRail;
    [SerializeField] private Button btnResumen;
    [SerializeField] private Button btnOfensa;
    [SerializeField] private Button btnDefensa;
    [SerializeField] private Button btnApoyo;
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private Button infoBtn;   // TopPanel/InfoBtn
    [SerializeField] private Button resetBtn;  // TopPanel/ResetBtn

    [Header("Wires")]
    [SerializeField] private HeroMasteryController masteryCtrl;

    // Resumen (asignados por Inspector)
    [SerializeField] private CanvasGroup   summaryRoot;        // Page_Resumen/SummaryPageRoot (CanvasGroup)
    [SerializeField] private RectTransform summaryGridRoot;    // Page_Resumen/SummaryPageRoot/GridRoot
    [SerializeField] private Button        summaryCellTemplate;// Desactivado

    private RectTransform _content, _viewport;
    private RectTransform _pResumen, _pOfensa, _pDefensa, _pApoyo;

    private int _page = 0;                         // 0..3
    private readonly float[] _stops = { 0f, 1f/3f, 2f/3f, 1f };
    private Vector2 _lastViewportSize = Vector2.negativeInfinity;
    private bool _initialized;

    public int currentPageIndex => _page;
    

    void Awake()
    {
        _content  = scrollRect ? scrollRect.content  : null;
        _viewport = scrollRect ? scrollRect.viewport : null;

        if (btnResumen) btnResumen.onClick.AddListener(() => GoToResumen());
        if (btnOfensa)  btnOfensa .onClick.AddListener(() => GoToOfensa());
        if (btnDefensa) btnDefensa.onClick.AddListener(() => GoToDefensa());
        if (btnApoyo)   btnApoyo  .onClick.AddListener(() => GoToApoyo());

        if (infoBtn)  infoBtn.onClick.AddListener(() => masteryCtrl?.ShowSummaryInfoPanel());
        if (resetBtn) resetBtn.onClick.AddListener(() => masteryCtrl?.OnClickResetMasteries());

        //if (scrollRect && pageRail)
        //    scrollRect.onValueChanged.AddListener(_ => pageRail.value = scrollRect.horizontalNormalizedPosition);

        if (scrollRect)
        {
            scrollRect.horizontal = true;
            scrollRect.vertical   = false;
            scrollRect.inertia    = true;
        }
    }

    void OnEnable()
    {
        EnsurePages();
        StartCoroutine(InitNextFrame());
    }

    System.Collections.IEnumerator InitNextFrame()
    {
        yield return null;
        _initialized = true;
        EnsurePages();

        // vuelve a la última página usada
        switch (_page)
        {
            case 0: GoToResumen(immediate: true); break;
            case 1: GoToOfensa (immediate: true); break;
            case 2: GoToDefensa(immediate: true); break;
            case 3: GoToApoyo  (immediate: true); break;
        }
    }

    void Update()
    {
        if (!_initialized || _viewport == null) return;

        var sz = _viewport.rect.size;
        if ((sz - _lastViewportSize).sqrMagnitude > 0.5f)
        {
            _lastViewportSize = sz;

            float current = scrollRect.horizontalNormalizedPosition;
            EnsurePages();

            // recoloca todo al tamaño nuevo
            SetPage(_page, immediate: true);
            scrollRect.horizontalNormalizedPosition = current;
            //if (pageRail) pageRail.value = current;

            masteryCtrl?.ApplyResumenTopMargin();
            masteryCtrl?.ForceLinesRecalc();
        }
    }
    private void PositionTree()
    {
        if (masteryCtrl != null && _viewport != null)
            masteryCtrl.SetTreePageIndex(_page, Mathf.Max(1f, _viewport.rect.width));
    }

    // ======== construcción de páginas ========
    void EnsurePages()
    {
        if (!_content || !_viewport) return;

        float w = Mathf.Max(1f, _viewport.rect.width);
        float h = Mathf.Max(1f, _viewport.rect.height);

        _pResumen = EnsurePage("Page_Resumen", 0, w, h);
        _pOfensa = EnsurePage("Page_Ofensa", 1, w, h);
        _pDefensa = EnsurePage("Page_Defensa", 2, w, h);
        _pApoyo = EnsurePage("Page_Apoyo", 3, w, h);

        _content.anchorMin = new Vector2(0, 1);
        _content.anchorMax = new Vector2(0, 1);
        _content.pivot = new Vector2(0, 1);
        _content.sizeDelta = new Vector2(w * 4f, h);

        // El árbol debe colgar del Content del pager y ocupar EXACTAMENTE una página
        masteryCtrl?.AttachTreeToPager(_content, _viewport);              // cuelga GridRoot del Content :contentReference[oaicite:1]{index=1}

        // Inyecta/actualiza refs del resumen en la página 0
        if (summaryRoot && summaryRoot.transform.parent != _pResumen)
            summaryRoot.transform.SetParent(_pResumen, false);
        if (summaryGridRoot && summaryGridRoot.transform.parent != summaryRoot.transform)
            summaryGridRoot.transform.SetParent(summaryRoot.transform, false);

        masteryCtrl?.SetResumenRoots(_pResumen, summaryRoot, summaryGridRoot, summaryCellTemplate); // :contentReference[oaicite:2]{index=2}
        masteryCtrl?.ApplyResumenTopMargin();                                                        // mantiene el top correcto :contentReference[oaicite:3]{index=3}

        // ¡Clave! Coloca el árbol en la página actual (0=Resumen, 1/2/3=ramas)
        PositionTree();
    }


    RectTransform EnsurePage(string name, int index, float w, float h)
    {
        Transform t = _content.Find(name);
        RectTransform rt;
        if (!t)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_content, false);
            rt = go.GetComponent<RectTransform>();
        }
        else rt = t as RectTransform;

        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(index * w, 0);
        return rt;
    }

    // ======== navegación ========
    public void GoToResumen(bool immediate = false)
    {
        SetPage(0, immediate);
        masteryCtrl.ShowSummary(true,  summaryRoot, summaryGridRoot, summaryCellTemplate);
        masteryCtrl.HideSummaryInfoPanel();    // por si estaba abierto
        masteryCtrl.SetBranchForPager(null);   // muestra las 3 ramas
        UpdateTopButtonsVisibility();
        UpdateTitle("Resumen Maestrias");
    }

    public void GoToOfensa(bool immediate = false)
    {
        SetPage(1, immediate);
        masteryCtrl.ShowSummary(false, summaryRoot, summaryGridRoot, summaryCellTemplate);
        masteryCtrl.HideSummaryInfoPanel();
        masteryCtrl.SetBranchForPager("ofensa");
        UpdateTopButtonsVisibility();
        UpdateTitle("Rama Ofensiva");
    }

    public void GoToDefensa(bool immediate = false)
    {
        SetPage(2, immediate);
        masteryCtrl.ShowSummary(false, summaryRoot, summaryGridRoot, summaryCellTemplate);
        masteryCtrl.HideSummaryInfoPanel();
        masteryCtrl.SetBranchForPager("defensa");
        UpdateTopButtonsVisibility();
        UpdateTitle("Rama Defensiva");
    }

    public void GoToApoyo(bool immediate = false)
    {
        SetPage(3, immediate);
        masteryCtrl.ShowSummary(false, summaryRoot, summaryGridRoot, summaryCellTemplate);
        masteryCtrl.HideSummaryInfoPanel();
        masteryCtrl.SetBranchForPager("apoyo");
        UpdateTopButtonsVisibility();
        UpdateTitle("Rama de Apoyo");
    }

    void SetPage(int index, bool immediate)
    {
        _page = Mathf.Clamp(index, 0, 3);
        float target = _stops[_page];

        // mueve el árbol para que “viva” dentro de la página seleccionada
        PositionTree();

        DOTween.Kill(scrollRect, complete:false);

        if (immediate)
        {
            scrollRect.horizontalNormalizedPosition = target;
            //if (pageRail) pageRail.value = target;
            masteryCtrl?.ForceLinesRecalc(); // recalcula conectores cuando terminas de saltar
        }
        else
        {
            float start = scrollRect.horizontalNormalizedPosition;
            DOTween.To(() => start,
                    v => { start = v; scrollRect.horizontalNormalizedPosition = v; },
                    target, 0.22f)
                .SetEase(Ease.OutCubic).SetUpdate(true).SetTarget(scrollRect)
                //.OnUpdate(() => { if (pageRail) pageRail.value = start; })
                .OnComplete(() => masteryCtrl?.ForceLinesRecalc());
        }
    }
    void UpdateTitle(string t)
    {
        if (!titleLabel) return;
        titleLabel.DOKill();
        titleLabel.DOFade(0f, 0.08f).SetUpdate(true).OnComplete(() =>
        {
            titleLabel.text = t;
            titleLabel.DOFade(1f, 0.12f).SetUpdate(true);
        });
    }

    private void UpdateTopButtonsVisibility()
    {
        bool onResumen = _page == 0;
        if (infoBtn)  infoBtn.gameObject.SetActive(onResumen);
        if (resetBtn) resetBtn.gameObject.SetActive(onResumen);
    }

    // drag → snap a página cercana
    public void OnBeginDrag(PointerEventData _) { }
    public void OnEndDrag(PointerEventData _)
    {
        float x = scrollRect.horizontalNormalizedPosition;
        int nearest = Mathf.Clamp(Mathf.RoundToInt(x * 3f), 0, 3);
        switch (nearest)
        {
            case 0: GoToResumen(); break;
            case 1: GoToOfensa();  break;
            case 2: GoToDefensa(); break;
            case 3: GoToApoyo();   break;
        }
    }
}
