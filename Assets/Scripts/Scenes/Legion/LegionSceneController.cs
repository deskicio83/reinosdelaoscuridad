using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;
using System.IO;

public class LegionSceneController : MonoBehaviour
{
    [Header("TopBar")]
    [SerializeField] private TMP_Text txtTotalHeroes;   // ej: 207/1000

    [SerializeField] private TMP_Dropdown ddOrdenTMP;   // TMP Dropdown principal

    [Header("Grid")]
    [SerializeField] private LegionGridController heroGrid;
    [SerializeField] private LegionDetailPanelUI legionDetailPanel;

    [Header("Loading")]
    [SerializeField] private LoadingOverlayController loadingOverlay;
    [SerializeField] private LegionGridController gridController;
    [SerializeField] private LegionDetailPanelUI detailPanel;

    private void Awake()
    {
        // Autolocaliza si faltan referencias
        if (!gridController) gridController = FindObjectOfType<LegionGridController>(true);
        if (!detailPanel) detailPanel = FindObjectOfType<LegionDetailPanelUI>(true);

        // El panel de detalle empieza oculto
        if (detailPanel) detailPanel.Hide();
    }

    private void Start()
    {
        if (loadingOverlay != null)
            StartCoroutine(LoadLegionFlow());
        else
            StartCoroutine(LateInit_Fallback());

        if (legionDetailPanel != null)
            legionDetailPanel.HideImmediate(); // oculto seguro sin animaciÃ³n

        if (heroGrid != null)
            heroGrid.PopulateAllHeroes();

        UpdateTotalHeroes();
    }

    private void OnDestroy()
    {
        if (heroGrid != null)
            heroGrid.OnHeroSelected -= HandleHeroSelected;
    }



    private IEnumerator LoadLegionFlow()
    {
        EnsureOverlayFullScreen();
        yield return StartCoroutine(loadingOverlay.Show());
        LoadingOverlayController.Set(0f, "Cargando LegiÃ³n...");

        // GameDataManager
        if (GameDataManager.Instance == null)
        {
            var go = new GameObject("GameDataManager");
            go.AddComponent<GameDataManager>();
            LoadingOverlayController.Step(0f, "Inicializando datos de jugador...");
            yield return null;
            LoadingOverlayController.Step(0.08f);
        }

        // Espera breve a PlayerData
        float t = 0f;
        while (GameDataManager.Instance.PlayerData == null && t < 0.5f)
        { t += Time.unscaledDeltaTime; yield return null; }
        LoadingOverlayController.Step(0.12f); // ~20%

        // CatÃ¡logo
        LoadingOverlayController.Step(0f, "Cargando catÃ¡logo de hÃ©roes...");
        var hcm = HeroCatalogManager.Instance;
        if (hcm == null) Debug.LogWarning("[LegionScene] HeroCatalogManager.Instance NULL.");
        LoadingOverlayController.Step(0.20f); // ~40%

        // UI bÃ¡sica
        LoadingOverlayController.Step(0f, "Preparando interfaz...");
        StyleOrderDropdown();
        RefreshTotals();
        LoadingOverlayController.Step(0.20f); // ~60%

        // Primera construcciÃ³n
        LoadingOverlayController.Step(0f, "Generando colecciÃ³n por Elemento...");
        OnOrderChanged(0);
        yield return null;
        LoadingOverlayController.Step(0.25f); // ~85%

        // Fin
        LoadingOverlayController.Step(0f, "Ãšltimos retoques...");
        yield return null;
        LoadingOverlayController.Set(1f);
        yield return StartCoroutine(loadingOverlay.Hide());
    }

    private void EnsureOverlayFullScreen()
    {
        if (loadingOverlay == null) return;

        var rt = loadingOverlay.transform as RectTransform;
        if (rt == null) return;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas != null && rt.parent != canvas.transform)
            rt.SetParent(canvas.transform, worldPositionStays: false);

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.SetAsLastSibling();
    }

    // Fallback sin overlay
    private IEnumerator LateInit_Fallback()
    {
        if (GameDataManager.Instance == null)
        {
            var go = new GameObject("GameDataManager");
            go.AddComponent<GameDataManager>();
            yield return null;
        }

        string dir = Path.Combine(Application.persistentDataPath, "Data");
        string file = Path.Combine(dir, "player_data.json");
        Debug.Log($"[LegionScene] Probe PlayerData â†’ dir='{dir}' exists={Directory.Exists(dir)} | fileExists={File.Exists(file)}");

        RefreshTotals();
        StyleOrderDropdown();
        OnOrderChanged(0);
    }

    private void RefreshTotals()
    {
        var total = HeroCatalogManager.Instance?.heroes?.Count ?? 0;
        var obtained = GameDataManager.Instance?.PlayerData?.heroes?.Count(h => h.stars > 0) ?? 0;
        if (txtTotalHeroes) txtTotalHeroes.text = $"{obtained}/{total}";
    }

    /// <summary>
    /// No modifica estÃ©tica. Solo registra el listener correcto segÃºn el control presente.
    /// </summary>
    private void StyleOrderDropdown()
    {
        // TMP_Dropdown (preferido)
        if (ddOrdenTMP != null)
        {
            ddOrdenTMP.onValueChanged.RemoveListener(OnOrderChanged);
            ddOrdenTMP.onValueChanged.AddListener(OnOrderChanged);
            return;
        }

    }


    // Cambia agrupaciÃ³n segÃºn Ã­ndice elegido (0..3)
    // Cambia la agrupaciÃ³n segÃºn la opciÃ³n seleccionada (0..3)
    private void OnOrderChanged(int idx)
    {
        var mode = (LegionGridController.GroupMode)Mathf.Clamp(idx, 0, 3);

        if (heroGrid == null)
        {
            Debug.LogError("[LegionScene] 'heroGrid' no estÃ¡ asignado en el Inspector.");
            return;
        }

        Debug.Log($"[LegionScene] Agrupar por â†’ {mode}");
        heroGrid.BuildSectioned(mode);
        RefreshTotals();
    }

    private void OnEnable()
    {
        if (gridController != null)
            gridController.OnHeroSelected += OnHeroSelectedFromGrid;
    }
    private void OnHeroSelectedFromGrid(string heroId)
    {
        Debug.Log($"[LegionScene] HÃ©roe seleccionado â†’ {heroId}");
        if (detailPanel == null)
        {
            detailPanel = FindObjectOfType<LegionDetailPanelUI>(true);
            if (detailPanel == null) { Debug.LogError("[LegionScene] No encuentro LegionDetailPanelUI en la escena."); return; }
        }

        // Mostrar inmediatamente y poblar datos
        detailPanel.ShowHero(heroId);
    }
    private void OnDisable()
    {
        if (gridController != null)
            gridController.OnHeroSelected -= OnHeroSelectedFromGrid;
    }

    private void UpdateTotalHeroes()
    {
        if (txtTotalHeroes == null || heroGrid == null) return;
        // si quieres mostrar contador real/total, ajÃºstalo a tus datos
        txtTotalHeroes.text = "0/0";
    }

    private void OnOrderChangedTMP(int idx)
    {
        // AquÃ­ sÃ³lo cambias el orden/agrupaciÃ³n como ya hacÃ­as.
        // Nada visual del dropdown desde cÃ³digo (respetamos tu GUI).
    }

    // ======= CLICK EN TARJETA =======
    private void HandleHeroSelected(string heroId)
    {
        Debug.Log($"[LegionScene] HÃ©roe seleccionado â†’ {heroId}");

        if (legionDetailPanel == null)
        {
            Debug.LogWarning("[LegionScene] legionDetailPanel no asignado en el inspector.");
            return;
        }

        // Mostrar y cargar contenido
        legionDetailPanel.ShowHero(heroId);
        // Traer el panel a primer plano por si el Overlay estÃ¡ por encima
        legionDetailPanel.transform.SetAsLastSibling();
    }

}
