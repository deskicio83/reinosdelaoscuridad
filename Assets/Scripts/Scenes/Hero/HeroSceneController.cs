/*
============================================================
HeroSceneController.cs — Orquestador de la escena de héroes
------------------------------------------------------------
PROPÓSITO
- Gestionar orden de inicialización (LoadingOverlay), enlazar Grid,
  3D, Stats, Skills, Gear, Tabs; aplicar vista compacta/extendida.

REFERENCIAS
- HeroGridController, Hero3DPanelController, HeroStatsPanelController,
  HeroDetailTabMenuController, LoadingOverlayController.

MÉTODOS (COMPLETA AQUÍ)
- Start/Awake(): secuencia de inicialización segura.
- ApplyCompactView(bool large, bool save): alterna layout (llamado en logs).
- InitToggleAfterLayout(): coroutine para esperar 1 frame y aplicar toggles.
============================================================
*/


using System.Collections;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.UI;
using DG.Tweening.Core;          // recomendado en AOT
using DG.Tweening.Plugins.Options;
using DG.Tweening;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class HeroSceneController : MonoBehaviour
{
    [Header("Loading")]
    [SerializeField] private LoadingOverlayController loadingOverlay;

    [Header("Referencias")]
    public HeroGridController heroGridController;
    public GameObject uiBlockerPanel;

    [SerializeField] private TMP_Text heroCountSmall;
    [SerializeField] private TMP_Text heroCountLarge;

    public Button addSpaceHeroBtn;

    [Header("UI")]
    public TMP_Dropdown filterDropdown;

    [Header("Ruta de archivos de datos")]
    public string playerDataJsonPath = "Data/player_data.json";

    private PlayerData playerData;
    public AddSpacePanelUI addSpacePanelUI;

    [Header("Inventario por paneles")]
    [SerializeField] private GameObject panelInventorySmall;
    [SerializeField] private GameObject panelInventoryLarge;

    [SerializeField] private RectTransform smallContent;
    [SerializeField] private RectTransform largeContent;
    [SerializeField] private GridCellResizer resizerSmall;
    [SerializeField] private GridCellResizer resizerLarge;

    [Header("Vista 3D")]
    [SerializeField] private GameObject panelHero3D;
    [SerializeField] private Hero3DPanelController hero3DPanelController;

    [SerializeField] private Toggle btnToggleViewToggle;
    [SerializeField] private RectTransform toggleHandle;
    [SerializeField] private Image toggleBackground;
    [SerializeField] private Color toggleOnColor;
    [SerializeField] private Color toggleOffColor;
    private float handleMoveDistance;

    [Header("Panel Despertar")]
    [SerializeField] private HeroDespertarPanelController panelDespertar;

    [Header("Columnas")]
    [SerializeField] private int smallColumns = 3;
    [SerializeField] private int largeColumns = 8;

    private HeroProgress _prewarmedForHero;

    private bool _smallBuilt = false;
    private bool _largeBuilt = false;
    private HeroProgress _currentHero;

    private void Awake()
    {
        if (btnToggleViewToggle != null)
        {
            btnToggleViewToggle.onValueChanged.RemoveAllListeners();
            btnToggleViewToggle.onValueChanged.AddListener(OnViewToggleChanged);
        }
    }

    // 3) Reemplaza tu Start() por ESTE (misma firma):
    private void Start()
    {
        StartCoroutine(BootWithLoading());
        DOTween.Init(recycleAllByDefault: false, useSafeMode: true);

        if (GameDataManager.Instance == null)
        {
            Debug.LogError("[HeroScene] GameDataManager.Instance es null.");
            return;
        }

        playerData = GameDataManager.Instance.PlayerData;
        if (playerData == null)
        {
            Debug.LogError("[HeroScene] playerData es null.");
            return;
        }
        UpdateHeroCountText();

        // Enganche del evento del grid
        if (heroGridController != null)
        {
            heroGridController.onCardSelected = OnHeroSelected;
            Debug.Log("[HeroScene] Escuchando onCardSelected del grid.");
        }
        else
        {
            Debug.LogWarning("[HeroScene] heroGridController NO asignado en el Inspector.");
        }

        // Espera a que el layout mida correctamente y luego inicializa el toggle
        StartCoroutine(InitToggleAfterLayout());
    }

    /// Precarga TODOS los paneles del héroe actual (sin mostrar nada).
    /// Precarga TODOS los paneles del héroe actual (sin mostrar nada).
    private void PreWarmPanelsForCurrentHero()
    {
        var hero = (heroGridController != null) ? heroGridController.CurrentHero : null;
        if (hero == null) return;

        // Evita repetir si ya está precalentado para este héroe
        if (_prewarmedForHero != null && _prewarmedForHero.heroId == hero.heroId) return;

        // Solo calentamos lo que EXISTE en este controller
        TryWarm(panelDespertar, "[Despertar]");       // HeroDespertarPanelController (componente)
        TryWarm(panelHero3D, "[Hero3D]");          // GameObject raíz 3D

        // Lo que sí expones vía grid (y sabemos que existe porque lo usas en OnHeroAwakened):
        if (heroGridController != null)
            TryWarm(heroGridController.statsPanelController, "[Stats]"); // componente

        _prewarmedForHero = hero;
    }
    /// Acepta GameObject o cualquier Component; si es válido, llama a WarmPanelNoFlicker(...)
    private void TryWarm(UnityEngine.Object obj, string tag)
    {
        if (obj == null) return;

        GameObject root = null;
        if (obj is GameObject go) root = go;
        else if (obj is Component comp) root = comp.gameObject;

        if (root != null)
            WarmPanelNoFlicker(root, tag);
    }



    /// Activa temporalmente el panel, lo deja invisible y sin raycasts,
    /// para que ejecute su OnEnable/Build y luego restaura su estado.
    private void WarmPanelNoFlicker(GameObject root, string tag)
    {
        if (root == null) return;

        var cg = root.GetComponent<CanvasGroup>();
        bool created = false;
        if (cg == null) { cg = root.AddComponent<CanvasGroup>(); created = true; }

        bool prevActive = root.activeSelf;
        float prevAlpha = cg.alpha;
        bool prevBlocks = cg.blocksRaycasts;
        bool prevInteractable = cg.interactable;

        try
        {
            // Activo pero invisible e inerte → construye sin parpadeos
            root.SetActive(true);
            cg.alpha = 0f;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // Forzar un frame para que sus OnEnable/Build corran
            Canvas.ForceUpdateCanvases();

            // Si algún panel expone ShowForHero/Refresh y quieres forzarlo aquí sin adivinar nombres:
            // SendMessage es seguro si no existe el método (no lanza error con DontRequireReceiver).
            var hero = (heroGridController != null) ? heroGridController.CurrentHero : null;
            if (hero != null) root.SendMessage("PreWarm", hero, SendMessageOptions.DontRequireReceiver);
            if (hero != null) root.SendMessage("ShowForHero", hero, SendMessageOptions.DontRequireReceiver);
            if (hero != null) root.SendMessage("RefreshForHero", hero, SendMessageOptions.DontRequireReceiver);
            root.SendMessage("Refresh", null, SendMessageOptions.DontRequireReceiver);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[HeroScene] Prewarm {tag} falló: {ex.Message}");
        }
        finally
        {
            // Restaurar exactamente como estaba
            cg.alpha = prevAlpha;
            cg.blocksRaycasts = prevBlocks;
            cg.interactable = prevInteractable;
            root.SetActive(prevActive);

            // Dejo el CanvasGroup añadido: no molesta y podremos reutilizarlo.
        }
    }

    protected void OnRectTransformDimensionsChange()
    {
        // Cuando cambia el tamaño del canvas o del control (rotación, resolución, etc.)
        CalculateHandleDistance();
    }
    public void OnHeroSelected(HeroProgress hp)
    {
        Debug.Log($"[HeroScene] OnHeroSelected recibido heroId={hp?.heroId}");
        _currentHero = hp;
        if (IsDespertarTabActive())
            RefreshDespertarPanel();
            // Precarga paneles del héroe seleccionado (nuevo) para navegación instantánea
            PreWarmPanelsForCurrentHero();
    }
    private System.Collections.IEnumerator InitToggleAfterLayout()
    {
        // Un frame para que Canvas/AutoLayout calcule tamaños reales en dispositivo
        yield return null;

        CalculateHandleDistance();

        bool isCompact = GameDataManager.Instance.PlayerData.heroSceneCompactView;

        if (btnToggleViewToggle != null)
            btnToggleViewToggle.SetIsOnWithoutNotify(isCompact);

        // Coloca el handle en su sitio inicial SIN tween
        if (toggleHandle != null)
        {
            float x = isCompact ? handleMoveDistance : -handleMoveDistance;
            var p = toggleHandle.anchoredPosition;
            p.x = x;
            toggleHandle.anchoredPosition = p;
        }

        UpdateToggleVisual(isCompact);

        // Construye el grid (small/large)
        ApplyCompactView(isCompact, save: false);

        // ⬇️ AÑADE ESTA LÍNEA: asegura que el texto queda correcto al terminar de montar la UI
        UpdateHeroCountText();
    }

    public void OnTabDespertarOpened()
    {
        // Coge SIEMPRE el héroe seleccionado actualmente en el grid,
        // por si el evento no nos llegó por cualquier motivo.
        if (heroGridController != null && heroGridController.CurrentHero != null)
        {
            _currentHero = heroGridController.CurrentHero;
            Debug.Log($"[HeroScene] Tab 'Despertar' abierto. CurrentHero desde Grid = {_currentHero.heroId}");
        }
        else
        {
            Debug.LogWarning("[HeroScene] Tab 'Despertar' abierto pero el Grid no tiene héroe seleccionado.");
        }

        RefreshDespertarPanel();
    }

    private bool IsDespertarTabActive()
    {
        // Si tienes tabs reales, compruébalo aquí; de momento siempre refrescamos.
        return true;
    }

    private void RefreshDespertarPanel()
    {
        if (panelDespertar == null)
        {
            Debug.LogWarning("[HeroScene] panelDespertar NO asignado.");
            return;
        }
        if (_currentHero == null)
        {
            Debug.LogWarning("[HeroScene] _currentHero es null, no se puede refrescar PanelDespertar.");
            return;
        }

        var pd = GameDataManager.Instance?.PlayerData;
        var cat = HeroCatalogManager.Instance?.GetHeroById(_currentHero.heroId);
        if (pd == null || cat == null)
        {
            Debug.LogWarning($"[HeroScene] Datos insuficientes para refrescar: PD={(pd != null)}, CAT={(cat != null)}");
            return;
        }

        Debug.Log($"[HeroScene] Refrescando PanelDespertar con heroId={_currentHero.heroId}, awaken={_currentHero.awaken}");
        panelDespertar.Show(pd, _currentHero, cat, onAwakened: () => OnHeroAwakened(_currentHero));
    }

    private void OnViewToggleChanged(bool isCompact)
    {
        ApplyCompactView(isCompact, save: false);  // <- no guardes aquí
        UpdateToggleVisual(isCompact);
        GameDataManager.Instance.PlayerData.SetHeroSceneCompactView(isCompact); // <- este sí guarda
    }

    private void CalculateHandleDistance()
    {
        if (toggleBackground == null || toggleHandle == null) return;

        var bgRt = (RectTransform)toggleBackground.transform;
        float backgroundWidth = bgRt.rect.width;
        float handleWidth = toggleHandle.rect.width;

        // Si aún no hay medida válida, evita ponerlo a 0
        if (backgroundWidth <= 0f || handleWidth <= 0f) return;

        // Un pequeño “padding” visual para que no pegue al borde
        const float padding = 8f;
        handleMoveDistance = Mathf.Max(0f, ((backgroundWidth - handleWidth) / 2f) - padding);
    }

    private IEnumerator WaitForManagersReady()
    {
        // Espera a que existan los singletons
        while (GameDataManager.Instance == null) yield return null;
        while (HeroCatalogManager.Instance == null) yield return null;
        while (MasteryCatalogManager.Instance == null) yield return null;

        // Espera a que el catálogo de maestrías esté cargado
        float timeout = 3f;
        while ((MasteryCatalogManager.Instance.Catalog == null) && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
    }


    private IEnumerator BootWithLoading()
    {
        // Asegura overlay visible (usa tu animación y fondo addressable actual)
        if (loadingOverlay) yield return loadingOverlay.Show();

        // 0) Seguridad: asegúrate de que los managers están disponibles (Awake pudo correr antes)
        LoadingOverlayController.Set(0.02f, "Inicializando gestores…");
        yield return WaitForManagersReady();

        // 1) Forzar lectura desde disco al entrar en la escena (centrado aquí)
        LoadingOverlayController.Set(0.05f, "Leyendo datos del jugador…");
        try
        {
            // Si cambió el build, copia default del APK primero
            GameDataManager.Instance.EnsureFreshDefaultsIfBuildChanged();

            // Carga player_data.json desde persistente (o si no existe, seed desde Resources)
            GameDataManager.Instance.CargarPlayerData();
            Debug.Log("[HeroScene] PlayerData cargado al entrar en la escena.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[HeroScene] Error cargando PlayerData al entrar en escena: " + ex.Message);
        }
        yield return null;

        // 2) Garantiza catálogos listos (Hero + Mastery) antes de construir UI
        LoadingOverlayController.Set(0.12f, "Preparando catálogos…");
        yield return WaitForManagersReady(); // espera a HeroCatalog+MasteryCatalog
        yield return null;

        // 3) Construcción del grid con progreso real (mapea 0.20 → 0.70)
        LoadingOverlayController.Set(0.20f, "Construyendo cuadrícula de héroes…");
        if (heroGridController != null)
        {
            var pd = GameDataManager.Instance.PlayerData;
            var heroes = pd.heroes;
            int spaces = pd.maxHeroSpaces;

            // Callback de progreso local → lo mapeamos a la barra del overlay
            System.Action<float, string> onProgress = (p, msg) =>
            {
                float mapped = Mathf.Lerp(0.20f, 0.70f, Mathf.Clamp01(p));
                string txt = string.IsNullOrEmpty(msg) ? "Construyendo…" : msg;
                LoadingOverlayController.Set(mapped, txt);
            };

            yield return heroGridController.BuildGridAsync(heroes, spaces, onProgress);
            UpdateHeroCountText(); // mantén tu contador en sync
            // Precarga paneles del héroe inicial para evitar esperas al abrir tabs
            PreWarmPanelsForCurrentHero();
        }
        else
        {
            Debug.LogWarning("[HeroScene] heroGridController no asignado; se omite construcción del grid.");
        }

        // 4) Inicialización de tabs, 3D y layout (compact/large) ya con datos cargados
        LoadingOverlayController.Set(0.80f, "Inicializando interfaz…");
        // El Start() ya lanza InitToggleAfterLayout(); aquí solo damos un frame para asegurar layout correcto
        yield return null;

        // 5) (Opcional) Pre-calentado ligero: iconos maestrías/recursos si lo necesitas
        LoadingOverlayController.Set(0.90f, "Últimos retoques…");
        yield return null;

        // 6) Fin
        LoadingOverlayController.Set(1.00f, "Listo para empezar");
        if (loadingOverlay) yield return loadingOverlay.Hide();
    }


    private void UpdateToggleVisual(bool isCompact)
    {
        // Recalcula por si la orientación cambió
        CalculateHandleDistance();

        float x = isCompact ? handleMoveDistance : -handleMoveDistance;

        if (toggleHandle != null)
        {
            // Unscaled time para que funcione siempre en móvil
            toggleHandle
                .DOAnchorPosX(x, 0.3f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);

            // Giro 360 al moverse
            toggleHandle
                .DORotate(new Vector3(0, 0, 360f), 0.3f, RotateMode.FastBeyond360)
                .SetRelative()
                .SetEase(Ease.InOutCubic)
                .SetUpdate(true);
        }

        if (toggleBackground != null)
        {
            toggleBackground
                .DOColor(isCompact ? toggleOnColor : toggleOffColor, 0.25f)
                .SetEase(Ease.Linear)
                .SetUpdate(true);
        }
    }


    public void OnFilterDropdownChanged(int value)
    {
        if (heroGridController != null)
            heroGridController.SortHeroesBy(value);
    }

    public void UpdateHeroCountText()
    {
        if (playerData == null) return;
        string txt = $"{playerData.heroes?.Count ?? 0}/{playerData.maxHeroSpaces}";
        if (heroCountSmall != null) heroCountSmall.text = txt;
        if (heroCountLarge != null) heroCountLarge.text = txt;
    }

    public void OnAddSpaceHeroBtnClicked()
    {
        if (uiBlockerPanel != null)
        {
            uiBlockerPanel.SetActive(true);
            uiBlockerPanel.transform.SetAsLastSibling();
        }
        addSpacePanelUI.gameObject.SetActive(true);
        addSpacePanelUI.transform.SetAsLastSibling();
        addSpacePanelUI.Init(playerData, this);
    }

    // private string LoadJsonFromResources(string relativePath)
    // {
    //     string resourcePath = Path.ChangeExtension(relativePath, null);
    //     TextAsset asset = Resources.Load<TextAsset>(resourcePath);
    //     if (asset == null)
    //     {
    //         Debug.LogError($"[HeroScene] No se encontró el archivo en Resources: {resourcePath}");
    //         return null;
    //     }
    //     return asset.text;
    // }

    public void OpenDespertar(HeroProgress progress)
    {
        if (progress == null) return;
        var catalog = HeroCatalogManager.Instance?.GetHeroById(progress.heroId);
        if (catalog == null) return;

        Debug.Log($"[HeroScene] OpenDespertar heroId={progress.heroId}");
        panelDespertar.Show(
            GameDataManager.Instance.PlayerData,
            progress,
            catalog,
            onAwakened: () => OnHeroAwakened(progress)
        );
    }

    private void OnHeroAwakened(HeroProgress awakened)
    {
        if (awakened == null) return;
        Debug.Log($"[HeroScene] OnHeroAwakened heroId={awakened.heroId}");

        // 1) Refrescar SOLO la card del grid (estrellas moradas y retrato awaken)
        if (heroGridController != null)
            heroGridController.RefreshCardStars(awakened.heroId);

        // 2) Refrescar el panel de stats grande
        var catalog = HeroCatalogManager.Instance?.GetHeroById(awakened.heroId);
        if (heroGridController != null && heroGridController.statsPanelController != null && catalog != null)
            heroGridController.statsPanelController.SetHero(awakened, catalog);

        // 3) (NUEVO) Refrescar el modelo 3D para que cambie al prefab awaken si procede
        if (heroGridController != null)
            heroGridController.Refresh3DForCurrent();
    }




    private void ApplyCompactView(bool large, bool save)
    {
        if (panelInventorySmall != null) panelInventorySmall.SetActive(!large);
        if (panelInventoryLarge != null) panelInventoryLarge.SetActive(large);

        if (panelHero3D != null) panelHero3D.SetActive(!large);
        if (hero3DPanelController != null) hero3DPanelController.SetSuspended(large);

        if (heroGridController == null)
        {
            Debug.LogError("[HeroScene] heroGridController no asignado.");
            return;
        }

        if (!large)
        {
            if (smallContent == null) { Debug.LogError("[HeroScene] smallContent no asignado."); return; }

            heroGridController.RebindTo(
                smallContent,
                smallContent.GetComponent<GridLayoutGroup>(),
                resizerSmall
            );

            heroGridController.SetColumns(smallColumns);

            if (!_smallBuilt)
            {
                heroGridController.SetHeroes(playerData.heroes, playerData.maxHeroSpaces);
                _smallBuilt = true;
            }
        }
        else
        {
            if (largeContent == null) { Debug.LogError("[HeroScene] largeContent no asignado."); return; }

            heroGridController.RebindTo(
                largeContent,
                largeContent.GetComponent<GridLayoutGroup>(),
                resizerLarge
            );

            heroGridController.SetColumns(largeColumns);

            if (!_largeBuilt)
            {
                heroGridController.SetHeroes(playerData.heroes, playerData.maxHeroSpaces);
                _largeBuilt = true;
            }
        }

        if (save) GameDataManager.Instance.PlayerData.SetHeroSceneCompactView(large);
        Debug.Log($"[HeroScene] Vista {(large ? "GRANDE" : "PEQUEÑA")} aplicada.");
    }
    // Llama a este método justo DESPUÉS de eliminar al héroe del PlayerData
    public void OnHeroDeletedRefreshUI()
    {
        var pd = GameDataManager.Instance?.PlayerData;
        if (heroGridController != null && pd != null)
        {
            // Re-pinta el grid con la lista actual
            heroGridController.SetHeroes(pd.heroes, pd.maxHeroSpaces);
        }

        // Si no queda ninguno, limpia el 3D explícitamente
        if ((pd?.heroes?.Count ?? 0) == 0)
            hero3DPanelController?.ClearHero();
    }

}
