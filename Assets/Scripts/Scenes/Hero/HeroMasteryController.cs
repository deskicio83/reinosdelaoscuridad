// /* =====================================================================================================================
//  * HeroMasteryController
//  * ---------------------------------------------------------------------------------------------------------------------
//  * Qué es
//  *  Controlador principal del sistema de Maestrías del Héroe en la escena de Héroe. Se encarga de:
//  *   • Construir el árbol de maestrías desde el catálogo real.
//  *   • Resolver conexiones padre→hijo, pintar líneas y estados visuales.
//  *   • Gestionar compras/persistencia en PlayerData y contadores del header.
//  *   • Mostrar popup de nodo, reset de maestrías y el Resumen (Page_Resumen).
//  *   • Sincronizar y animar el “Resumen de Maestrías” con el InfoPanel.
//  *
//  * Jerarquía / referencias principales (asignadas por Inspector)
//  *   gridRoot (Scroll Content), scrollRect (Viewport), popupLayer, nodeButtonPrefab, popupPrefab, linePrefab.
//  *   pageResumen, summaryPageRoot, summaryRoot, summaryGridRoot, summaryCellTemplate (Resumen).
//  *   infoPanel (+ viewport + content + template), uiBlockerPanel (bloqueador de taps).
//  *   Textos del header (básico/avanzado/divino/caosífera) y botón Reset.
//  *
//  * Estado y colecciones internas
//  *   _runtime/_byId/_rtToNode : nodos en runtime y sus RectTransforms.
//  *   _selected               : ids compradas (sincronizadas con PlayerData).
//  *   _lines                  : conectores UI entre nodos.
//  *   _summaryImages          : imágenes de celdas del Resumen (Page_Resumen).
//  *   _summaryVisualNudgeX    : micro-ajuste para centrar visualmente la malla del Resumen.
//  *
//  * Reglas núcleo
//  *   • Los requisitos lógicos (estrellas, awaken, límite por tier/ramas) nunca impiden abrir el popup;
//  *     solo controlan el estado visual y si el botón “Comprar” se habilita.
//  *   • El coste real por tier se toma del catálogo (si el nodo no define coste específico).
//  *   • El Resumen se construye una vez y se refresca con iconos/huecos; al abrir InfoPanel
//  *     se anima a la mitad derecha del contenedor y vuelve al centro al cerrar.
//  *
//  * =====================================================================================================================
//  *                     DESCRIPCIÓN DE MÉTODOS (orden aproximado de aparición)
//  * =====================================================================================================================*/

// //
// // --- Entrada / Ciclo de vida ---
// //
// void OnEnable()
//     // Invalida layout y asegura anclajes del grid para que el árbol ocupe exactamente una “page”.
//     // Dispara un reposicionamiento en el siguiente frame.

// protected void OnRectTransformDimensionsChange()
//     // Marca que hay que recalcular layout cuando cambie el tamaño del RectTransform.

// void LateUpdate()
//     // Si _forceRebuild está activo, recoloca todo (árbol + líneas).

// //
// // --- API pública principal ---
// //
// public void ShowForHero(HeroProgress hero)
//     // Punto de entrada: sincroniza PlayerData, construye el árbol, lo posiciona,
//     // recalcula líneas, centra el Resumen y sitúa el scroll arriba.

// public void SetBranchForPager(string branch)
//     // Filtra la vista del árbol: null=todas, "ofensa"/"defensa"/"apoyo"=solo esa rama.
//     // Reposiciona y repinta estados/líneas.

// public void FocusNodeById(string nodeId)
//     // Centra verticalmente el Scroll para que el nodo indicado quede visible.

// public void ShowSummary(bool show, CanvasGroup sr=null, RectTransform grid=null, Button cellTemplate=null)
//     // Muestra/oculta Page_Resumen. Construye si hace falta y refresca iconos.
//     // Oculta/enseña el árbol según ‘show’.

// public void SetResumenRoots(RectTransform page, CanvasGroup rootCg, RectTransform grid, Button template)
//     // Inyección de referencias del Resumen desde el pager/control padre.

// public void AttachTreeToPager(RectTransform pagerContent, RectTransform viewport)
//     // Garantiza que el árbol esté anclado a una “page” (tamaño viewport).

// public void EnsureGridAnchoredToPage(RectTransform viewport)
//     // Ancla el grid a (0,1) y ajusta su sizeDelta al tamaño del viewport.

// public void OnClickResetMasteries()
//     // Abre popup de confirmación de reset; calcula coste/moneda; aplica reset si procede.

// //
// // --- Popup de nodo / compras ---
// //
// private void OnNodeClicked(RuntimeNode rn)
//     // Construye y muestra el popup del nodo: título, descripción, requisitos.
//     // Habilita “Comprar” si las reglas y recursos lo permiten.

// private MasteryNodePopupUI EnsurePopupInViewport()
//     // Crea (o reutiliza) un holder Canvas por encima de todo y instancia el prefab del popup.
//     // Asegura CanvasGroup, GraphicRaycaster, orden y centrado.

// private void CenterPopup()
//     // Encaja el popup dentro del viewport (márgenes y anclajes robustos).

// private bool TrySpendAndCommitPurchase(MasteryNode node)
//     // Valida coste, gasta inventario (PlayerData.awakenInventory), añade compra al héroe,
//     // guarda, refresca cabeceras/árbol/resumen y devuelve true si se completó.

// //
// // --- Reset de maestrías ---
// //
// private void ShowResetPopupForCurrentHero()
//     // Construye un popup con el coste (o gratis) del reset para el héroe actual.
// private void ApplyResetForCurrentHero(bool saveImmediately = true)
//     // Limpia PlayerData y todo el estado de runtime (nodos/líneas/popup),
//     // reconstruye árbol y resumen, actualiza header y guarda si se indica.

// private void GetResetOfferForCurrentHero(out bool esGratis, out string moneda, out int coste)
// private void EnsureGlobalVarsLoaded()
//     // Lee variables_globales.json (Resources) para conocer moneda/coste base.

// //
// // --- Construcción del árbol y líneas ---
// //
// private void BuildAll()
//     // Destruye la instancia previa del árbol, instancia todos los botones de nodo,
//     // resuelve padres, asigna displaySlot, valida integridad, crea líneas y pinta estados.

// private void ResolveParentsFromCatalogOrPattern()
//     // Para cada nodo T>1 obtiene sus padres desde el catálogo o por patrón (fallback),
//     // con ajuste específico T1→T2 para producir conexiones esperadas.

// private void ValidateParentIntegrity()
//     // Verifica que todos los padres pertenezcan a la misma rama y al tier anterior (solo logs).

// private void ComputeDisplaySlots()
//     // Calcula la “columna visual” (displaySlot) por rama/tier en función de la posición de los padres.
//     // T1 respeta el orden natural, T2..T6 usa la media de posiciones de sus padres.

// private void BuildLines()
//     // Instancia y registra todos los UILineConnector entre padre→hijo,
//     // configura grosor, color base por rama y visibilidad inicial.

// private void ApplyPagerBranchVisibility()
//     // Activa/desactiva nodos y líneas según la rama actualmente visible.

// public void ForceLinesRecalc()
//     // Llama a ForceUpdateNow() en cada línea (útil tras relayout).

// //
// // --- Layout del árbol (responsivo) ---
// //
// private void RepositionAll()
//     // Calcula tamaño de celda (fijo u otomático), distribuye 3 columnas (o 1 si hay rama filtrada),
//     // centra cada fila por tier y posiciona cada botón en su displaySlot.

// private static void SizeAndCenter(Button btn, float size)
//     // Normaliza una celda de Resumen/plantilla: targetGraphic, tamaños, raycasts del hijo, etc.

// private System.Collections.IEnumerator RepositionNextFrame()
//     // Espera 1 frame y marca _forceRebuild para recolocar con tamaños ya calculados.

// //
// // --- Estados visuales del árbol ---
// //
// private void RefreshVisualStates()
//     // Recorre todos los nodos y decide su MasteryVisualState (Selected/Available/Locked/Unavailable),
//     // activa/desactiva lock visual, dota roja si se puede pagar, y repinta líneas según owned/open.

// private void RefreshLinesVisual()
//     // Hace brillar las líneas “fuertes” (padre comprado y hijo comprado/abrible) y atenúa el resto.

// private bool IsLockedByStarsOrAwaken(MasteryNode nd)
// private bool CanSelect(MasteryNode nd)
// private int  CountPicks(string branch, int tier)
// private bool HasAnotherBranchPickedTier6(string branch)
// private bool IsThirdBranchLocked(string branch)
// private bool CanAfford(MasteryCost c)
// private MasteryCost ResolveCost(MasteryNode nd)
//     // Conjunto de reglas y utilidades para determinar si un nodo es seleccionable y pagable.

// //
// // --- Header: recursos y reset ---
// //
// private void LoadMasteryResourcesFromPlayerData()
//     // Lee SIEMPRE de PlayerData.awakenInventory los 3 contadores de materiales.

// private void RefreshChaosFromPlayer()
//     // Actualiza los 3 contadores, escribe en el header y traza.

// private void UpdateHeaderCurrencies()
// private void UpdateHeaderCounters()
// private void UpdateResetButtonState()
// private void TryWireResetBtn()
//     // Gestión de textos del header y habilitación del botón Reset.

// //
// // --- Resumen (Page_Resumen) + InfoPanel ---
// //
// public void ApplyResumenTopMargin()
//     // Ajusta anclajes de SummaryPageRoot + coloca el grid con margen superior.

// private void EnsureSummaryBuilt()
//     // Construye la estructura del Resumen (TierRow_1..6, celdas por tier),
//     // normaliza botones, centra, recalcula nudge y refresca iconos.

// private void ReflowSummaryRows()
//     // Recoloca horizontalmente cada fila del Resumen de forma centrada.

// private void ComputeSummaryVisualNudge()
//     // Mide el “centro real” de las dos celdas de T1 y calcula un micro-ajuste (nudge) para centrar
//     // visualmente el conjunto (compensa pixeles/redondeos y sprites).

// private void RefreshSummaryIcons()
//     // Llena las celdas del Resumen por tier: primero compradas (interactivas → abren InfoPanel),
//     // luego huecos/placeholder (no interactivos).

// private void CenterResumenLayoutNow()
//     // Centra el grid del Resumen usando SIEMPRE el ancho de SummaryPageRoot (responsive),
//     // aplica nudge y reflujo de filas.

// public void AnimateResumenToRightAndShowInfo()
//     // Anima: grid del Resumen → mitad derecha; InfoPanel entra en mitad izquierda.
//     // Usa métricas de SummaryPageRoot, no de la pantalla completa.

// public void AnimateResumenBackToCenter()
//     // Anima de vuelta el grid al centro y oculta InfoPanel.

// private struct ResumenMetrics { /* pageW, innerW, halfW, centerW, gap, leftCenterX, rightCenterX */ }
// private ResumenMetrics GetResumenMetrics()
//     // Calcula anchos/centros consistentes para centrar y partir el área en dos mitades con un gap.

// private void DisableSummaryBackgroundRaycasts()
//     // Desactiva raycasts de fondos en Page_Resumen para que los botones de celdas sean clicables.

// private static Button EnsureButton(GameObject go)
// private static Image  FindTemplateImage(Button btn)
// private void NormalizeSummaryCell(Button btn)
//     // Utilidades para asegurarse de que cada celda del Resumen es un Button válido y el hex hijo
//     // no roba raycasts.

// //
// // --- InfoPanel ---
// //
// public  void ShowSummaryInfoPanel()
//     // Asegura layout del panel (ScrollRect/Viewport/Content), lo rellena con las maestrías compradas,
//     // muestra overlay de dismiss y anima el Resumen a la derecha.

// public  void HideSummaryInfoPanel()
//     // Oculta InfoPanel y overlay; devuelve el Resumen al centro.

// private void EnsureInfoPanelLayout()
//     // Configura ScrollRect, Viewport (le añade un Graphic invisible si falta) y Content
//     // con VerticalLayout + ContentSizeFitter para alto preferido.

// private void PopulateInfoPanel()
//     // Reconstruye la lista del InfoPanel (orden rama→tier→slot→id), con icono a la izquierda
//     // y texto autosize (máx 25) a la derecha.

// private void OpenInfoAndScrollTo(string nodeId)
// private System.Collections.IEnumerator ScrollInfoToItemNextFrame(string nodeId)
//     // Abre InfoPanel y, al siguiente frame, desplaza el scroll para centrar el item de esa maestría.

// private void EnsureDismissCatcher()
// private bool IsPointerOverRect(RectTransform rt, Vector2? screenPos = null)
// private void HookBlockerTo(System.Action onClick)
//     // Overlay de dismiss (tap fuera para cerrar) y utilidades de interacción.

// //
// // --- Catálogo/parents por patrón (fallback) ---
// //
// private static readonly int[] TierStart = { 0,1,3,7,11,15,19 };
// private List<string> ParentsByPattern(string branch, int tier, int slot)
//     // Genera ids de posibles padres siguiendo el patrón geométrico si el JSON no define requiresAnyOf.

// //
// // --- Sincronización PlayerData / recursos ---
// //
// private PlayerData PD => GameDataManager.Instance != null ? GameDataManager.Instance.PlayerData : null;
// private void SyncSelectedFromPlayerData()
//     // _selected ← PlayerData (compras del héroe actual).

// private bool HasResourcesFor(MasteryCost c)
//     // Consulta el inventario awaken (extracto/infusión/destilado).

// //
// // --- Utilidades varias ---
// //
// [ContextMenu("DEBUG/Log Popup Wiring")]  private void __DebugLogPopupWiring()
//     // Traza estado de popupLayer/canvas/graphicRaycaster.

// [ContextMenu("Rebuild Context")]         public void RebuildContext()
// public void RebuildContext(HeroProgress hero, bool keepSelection = true)
//     // Reconstrucción manual para debugging/edición.

// private void KillAllNodeTweens()
//     // Mata tweens activos de botones (DOTween) para evitar estados extraños al cambiar de pestaña.

// private void SetTreeVisible(bool visible)
// private void SetCanvas(CanvasGroup cg, float a, bool enable)
// private string HeroNameUI()
// private string GetLockNoteIfAny(string buyingBranch)
// private static string BuildReqsText(int minStars, bool requiresAwaken)
// private string BuildReqs(MasteryNode nd)
// private string ThirdBranch(string a, string b)
// private string Pretty(string br)
// private Color BranchColor(string branch)
// private void Log(string msg)  // condicional por debugLogs
// private void Warn(string msg) // warning simple
// private static int IdNum(string id)
//     // Helpers y formateos varios.
// */



using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;
using DG.Tweening;


public class HeroMasteryController : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private RectTransform gridRoot;     // Content del Scroll
    [SerializeField] private ScrollRect scrollRect;

    [Header("Header counters")]
    [SerializeField] private TMP_Text txtBasico;
    [SerializeField] private TMP_Text txtAvanzado;
    [SerializeField] private TMP_Text txtDivino;
    [SerializeField] private TMP_Text txtCaosfera;
    [SerializeField] private Button resetBtn;

    [Header("Prefabs")]
    [SerializeField] private RectTransform popupLayer;       // Asigna PanelMaestria/PopupLayer
    [SerializeField] private MasteryNodeButton nodeButtonPrefab;
    [SerializeField] private MasteryNodePopupUI popupPrefab;
    [SerializeField] private Image linePrefab; // Debe tener sprite (UI/Sprite). Añade componente UILineConnector si tu sprite no lo tiene.

    [Header("Auto-layout")]
    [SerializeField] private float padding = 8f;
    [SerializeField] private float columnSpacing = 14f;
    [SerializeField] private float horizontalSpacing = 14f;
    [SerializeField] private float minNodeSize = 84f;
    [SerializeField, Range(0.85f, 1f)] private float rowHeightFill = 0.98f;
    [Header("Tamaño de nodo por instancia")]
    [SerializeField] private bool usarTamañoFijo = true;     // activa/desactiva el control directo
    [SerializeField, Min(32f)] private float tamañoNodoFijo = 80f; // pon aquí 80, 90, etc.
    [SerializeField] private Canvas rootCanvas;     // opcional; si no lo asignas lo detecto en runtime
    [SerializeField] private RectTransform dismissCatcher; // se crea en runtime si viene null

    [Header("Lineas")]
    [SerializeField] private float lineThickness = 10f;
    [SerializeField, Range(0f, 0.3f)] private float lineInsetPercent = 0f; // 0 = sin recorte (línea máxima)
    [Header("Resumen - Panel Info")]
    [SerializeField] private RectTransform infoPanel;          // Pager/PopupLayer/InfoPanel (root)
    [SerializeField] private RectTransform infoPanelViewport;  // InfoPanel/ScrollView/Viewport
    [SerializeField] private Transform infoListContent;    // InfoPanel/ScrollView/Viewport/Content
    [SerializeField] private GameObject infoItemTemplate;   // hijo desactivado con: Image (name: "Icon") + TMP_Text (name: "Label")
    [SerializeField] private Sprite fallbackIcon;       // opcional, para cuando no haya sprite
    [SerializeField] private GameObject uiBlockerPanel;        // UIBlockerPanel (captura toques para cerrar)
    // === InfoPanel (lista de maestrías compradas) ===
    [SerializeField] private float infoIconSize = 56f;   // tamaño fijo del icono en cada fila
    [SerializeField] private float infoItemHeight = 92f;   // alto fijo de cada fila (3 líneas)
    [SerializeField] private float infoItemMinHeight = 88f; // ← alto mínimo por fila (icono + margen)
    [SerializeField] private int infoMaxChars = 120;   // recorte suave del texto (nombre + desc)

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private float resumenAnimDuration = 0.35f;
    
    // margen visual entre InfoPanel (izq) y Grid (dcha)
    private const float resumenGap = 18f;

#if USE_ADDRESSABLES
    private const string ICONS_ADDR_DIR = "Assets/Addressables/Art/HeroScene/Maestries/"; // clave EXACTA
#endif
    private const string RESOURCES_DIR = "Art/HeroScene/Maestries/"; // fallback
       


    private PlayerData PD => GameDataManager.Instance != null ? GameDataManager.Instance.PlayerData : null;
    // ======= Catálogo demo (sólo UI) =======
    [Serializable] public class NodeCost { public int basico; public int avanzado; public int divino; }
    [Header("Resumen (Overview)")]
    [SerializeField] private RectTransform pageResumen;        // Pager/Content/Page_Resumen
    [SerializeField] private RectTransform summaryPageRoot;    // Page_Resumen/SummaryPageRoot
    [SerializeField] private CanvasGroup summaryRoot;            // Asignado por MasteryPager
    [SerializeField] private RectTransform summaryGridRoot;      // Asignado por MasteryPager
    [SerializeField] private Button summaryCellTemplate;         // Asignado por MasteryPager (desactivado)
    [SerializeField][Range(0, 150)] private float resumenTopMargin = 0f; // controla “aire” arriba
    

    // --- Resumen (añadir si no están) ---
    [SerializeField] private Sprite summaryEmptySprite;
    [SerializeField] private float summaryNodeSize = 95f; // <-- antes 60f
    [SerializeField] private float summaryPadding = 5f;
    [SerializeField] private float summaryHSpacing = 3f;

    private float _summaryVisualNudgeX = 0f;  // desplazamiento para centrar visualmente el grid

    // NUEVO → controla separación vertical y un pequeño offset superior
    [SerializeField] private float summaryVSpacing = 10f;
    [SerializeField] private float summaryTopOffset = 0f;
    // --- Claves de inventario para piezas de Caos ---
    private const string KEY_CAOS_BASICO = "caos_extracto";
    private const string KEY_CAOS_AVANZADO = "caos_infusion";
    private const string KEY_CAOS_DIVINO = "caos_destilado";

    // Pool interno de celdas creadas para el resumen
    private readonly List<Image> _summaryImages = new(); // sólo guardamos el Image (no texto)
    private bool _summaryBuilt = false;

    // Rama visible para el pager: null = 3 columnas (todas), "ofensa"/"defensa"/"apoyo" = 1 columna a pantalla completa.
    [NonSerialized] private string _pagerBranch = null;
    // ========== VARIABLES GLOBALES (reset) ==========
    [Serializable]
    private class ResetMaestriasCfg
    {
        public string moneda = "caosifera";
        public int coste = 100;
    }

    [Serializable]
    private class GlobalVarsRoot
    {
        public ResetMaestriasCfg reset_maestrias = new ResetMaestriasCfg();
    }

    private GlobalVarsRoot _globalVars;

    // Llama a esto al entrar en la pestaña de Maestrías y siempre tras una compra.
    private void RefreshChaosFromPlayer()
    {
        var pd = GameDataManager.Instance?.PlayerData;
        var inv = pd?.awakenInventory;

        _rBasico = inv?.Get(KEY_CAOS_BASICO) ?? 0;
        _rAvanzado = inv?.Get(KEY_CAOS_AVANZADO) ?? 0;
        _rDivino = inv?.Get(KEY_CAOS_DIVINO) ?? 0;

        UpdateHeaderCounters(); // usa _rBasico/_rAvanzado/_rDivino para pintar los textos

        Debug.Log($"[Mastery] Inventario Caos -> B:{_rBasico}  A:{_rAvanzado}  D:{_rDivino}");
    }
    // añade este método en la clase (por ejemplo, cerca de ReflowSummaryRows)
    private void ComputeSummaryVisualNudge()
    {
        _summaryVisualNudgeX = 0f;
        if (!summaryGridRoot) return;

        // Usamos TierRow_1 como referencia (dos celdas) y calculamos el centro medio real
        var row = summaryGridRoot.Find("TierRow_1") as RectTransform;
        if (!row || row.childCount < 2) return;

        var c0 = row.GetChild(0) as RectTransform;
        var c1 = row.GetChild(1) as RectTransform;
        if (!c0 || !c1) return;

        // Centro local de cada celda (respecto a summaryGridRoot)
        Vector3 w0 = c0.TransformPoint(c0.rect.center);
        Vector3 w1 = c1.TransformPoint(c1.rect.center);
        Vector3 l0 = summaryGridRoot.InverseTransformPoint(w0);
        Vector3 l1 = summaryGridRoot.InverseTransformPoint(w1);

        float midX = (l0.x + l1.x) * 0.5f;  // si está perfecto, debería ser 0
                                            // El nudge es la compensación contraria para que ese “mid” quede exactamente en 0
        _summaryVisualNudgeX = -Mathf.Round(midX);
    }


    private bool HasResourcesFor(MasteryCost c)
    {
        if (c == null) return true;
        var inv = GameDataManager.Instance?.PlayerData?.awakenInventory;
        int b = inv?.Get(KEY_CAOS_BASICO) ?? 0;
        int a = inv?.Get(KEY_CAOS_AVANZADO) ?? 0;
        int d = inv?.Get(KEY_CAOS_DIVINO) ?? 0;
        return b >= c.basico && a >= c.avanzado && d >= c.divino;
    }


    /// Carga perezosa del JSON "Resources/Data/variables_globales.json"
    private void EnsureGlobalVarsLoaded()
    {
        if (_globalVars != null) return;

        try
        {
            var ta = Resources.Load<TextAsset>("Data/variables_globales");
            if (ta != null)
            {
                _globalVars = Newtonsoft.Json.JsonConvert.DeserializeObject<GlobalVarsRoot>(ta.text);
            }
            if (_globalVars == null) _globalVars = new GlobalVarsRoot();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Reset] No se pudo leer variables_globales.json: " + e.Message);
            _globalVars = new GlobalVarsRoot();
        }
    }
    // Devuelve si el reset es gratis y, si no lo es, moneda+coste a pagar.
    // Coste real = baseCoste * resetCount (primera vez resetCount==0 ⇒ gratis).
    private void GetResetOfferForCurrentHero(out bool esGratis, out string moneda, out int coste)
    {
        EnsureGlobalVarsLoaded();

        esGratis = true;
        moneda = _globalVars.reset_maestrias.moneda ?? "caosifera";
        coste = 0;

        if (GameDataManager.Instance == null || GameDataManager.Instance.PlayerData == null || _hero == null)
            return;

        var pd = GameDataManager.Instance.PlayerData;
        int resets = pd.GetMasteryResetCount(_hero.heroId);
        if (resets <= 0)
        {
            esGratis = true;
            coste = 0;
        }
        else
        {
            esGratis = false;
            int baseCost = Math.Max(0, _globalVars.reset_maestrias.coste);
            coste = baseCost * resets; // 2ª vez:*1, 3ª:*2, etc.
        }
    }

    private void ApplyResetForCurrentHero(bool saveImmediately = true)
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.PlayerData == null || _hero == null)
            return;

        var pd = GameDataManager.Instance.PlayerData;

        // 1) Limpia datos persistidos
        pd.ClearHeroMasteries(_hero.heroId);

        // --- NUEVO: limpia completamente el estado de runtime ---
        _selected.Clear();              // cache de compras actual
        _byId.Clear();                  // mapa id → nodo runtime
        _runtime.Clear();               // lista runtime
        _rtToNode.Clear();              // mapa RT → nodo runtime
        _lines.Clear();                 // líneas de conexión

        // Si hubiera un popup reutilizado de antes, lo destruimos para evitar estados raros
        if (_popup) { Destroy(_popup.gameObject); _popup = null; }
        _popupHolder = null;

        // Vuelve a leer (no habrá ninguna compra)
        SyncSelectedFromPlayerData();
        // --------------------------------------------------------

        // 2) Incrementa contador de resets
        pd.IncrementMasteryReset(_hero.heroId);

        // 3) Refresca la UI (reconstrucción completa)
        try
        {
            BuildAll();
            RepositionAll();
            ForceLinesRecalc();
            EnsureSummaryBuilt();
            RefreshSummaryIcons();
            UpdateHeaderCounters();
            UpdateHeaderCurrencies();    // caosífera del header
            UpdateResetButtonState();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Reset] Refresco UI tras reset falló: " + e.Message);
        }

        if (saveImmediately) pd.Save();
    }


    private void EnsureDismissCatcher()
    {
        if (!infoPanel) return;

        // Si ya existe y es hijo del mismo padre, nada que hacer
        if (dismissCatcher && dismissCatcher.parent == infoPanel.parent) return;

        // Crear overlay invisible
        var go = new GameObject("DismissCatcher",
            typeof(RectTransform), typeof(CanvasGroup), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        dismissCatcher = go.GetComponent<RectTransform>();
        dismissCatcher.SetParent(infoPanel.parent, false);
        dismissCatcher.anchorMin = Vector2.zero;
        dismissCatcher.anchorMax = Vector2.one;
        dismissCatcher.pivot = new Vector2(0.5f, 0.5f);
        dismissCatcher.offsetMin = Vector2.zero;
        dismissCatcher.offsetMax = Vector2.zero;

        // Invisible pero con raycast
        var img = go.GetComponent<UnityEngine.UI.Image>();
        img.color = new Color(0, 0, 0, 0);
        img.raycastTarget = true;

        // Clic → cerrar
        var btn = go.GetComponent<UnityEngine.UI.Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(HideSummaryInfoPanel);

        // Debe quedar DETRÁS del InfoPanel pero por delante del resto
        // Lo colocamos justo debajo del InfoPanel en el orden de hermanos.
        var parent = infoPanel.parent as RectTransform;
        int idx = infoPanel.GetSiblingIndex();
        dismissCatcher.SetSiblingIndex(Mathf.Max(0, idx));
        infoPanel.SetSiblingIndex(dismissCatcher.GetSiblingIndex() + 1);

        // Oculto por defecto
        var cg = go.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
        go.SetActive(false);
    }


    // Devuelve true si el puntero está sobre el rect del panel
    private bool IsPointerOverRect(RectTransform rt, Vector2? screenPos = null)
    {
        if (!rt) return false;
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();
        var cam = rootCanvas ? rootCanvas.worldCamera : null;
        var p = screenPos ?? (Vector2)Input.mousePosition;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, p, cam);
    }


    // Carga recursos desde player_data.json a los contadores locales del header
    // === Lee SIEMPRE de awakenInventory ===
    void LoadMasteryResourcesFromPlayerData()
    {
        _rBasico = _rAvanzado = _rDivino = 0;

        var inv = GameDataManager.Instance?.PlayerData?.awakenInventory;
        if (inv == null) return;

        _rBasico = Mathf.Max(0, inv.Get("caos_extracto"));
        _rAvanzado = Mathf.Max(0, inv.Get("caos_infusion"));
        _rDivino = Mathf.Max(0, inv.Get("caos_destilado"));
    }


    // Sincroniza _selected con las compras persistidas
    private void SyncSelectedFromPlayerData()
    {
        _selected.Clear();
        if (PD == null || _hero == null || string.IsNullOrEmpty(_hero.heroId)) return;

        var ids = PD.GetOrCreateHeroMaestries(_hero.heroId);
        foreach (var id in ids)
        {
            var n = MasteryCatalogManager.Instance?.GetNode(id);
            if (n != null) _selected.Add(id);
        }
    }

    // --- mostrar/ocultar resumen -------------------------------------------------
    // Mostrar/Ocultar la página de Resumen (Page_Resumen) y refrescar iconos
    public void ShowSummary(
        bool show,
        CanvasGroup sr = null,
        RectTransform grid = null,
        Button cellTemplate = null)
    {
        if (sr != null) summaryRoot = sr;
        if (grid != null) summaryGridRoot = grid;
        if (cellTemplate != null) summaryCellTemplate = cellTemplate;

        if (show)
        {
            // Construye si hace falta y posiciona el grid
            EnsureSummaryBuilt();
            ApplyResumenTopMargin();
            // MUY IMPORTANTE: desactiva raycasts de fondos que podrían tapar los botones
            DisableSummaryBackgroundRaycasts();

            // Muestra el resumen y oculta el árbol
            SetCanvas(summaryRoot, 1f, true);
            SetTreeVisible(false);

            // Pinta iconos de celdas compradas y huecos
            RefreshSummaryIcons();
        }
        else
        {
            // Oculta el resumen y vuelve a mostrar el árbol
            SetCanvas(summaryRoot, 0f, false);
            SetTreeVisible(true);
        }
    }






    // Rama visible en el pager: null = todas; "ofensa"/"defensa"/"apoyo" = una sola
    public void SetBranchForPager(string branch)
    {
        KillAllNodeTweens();

        _pagerBranch = string.IsNullOrEmpty(branch) ? null : branch;

        // Centraliza la lógica de visibilidad
        ApplyPagerBranchVisibility();

        // Recoloca y actualiza estados/colores
        _forceRebuild = true;
        RepositionAll();
        RefreshVisualStates();   // decide brillo de líneas (compradas/posibles vs. bloqueadas)
        ForceLinesRecalc();

        if (scrollRect) scrollRect.verticalNormalizedPosition = 1f; // arriba del árbol
    }





    /// Centra el scroll vertical sobre un nodo por id (si existe)
    public void FocusNodeById(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || !_byId.TryGetValue(nodeId, out var rn) || scrollRect == null || gridRoot == null)
            return;

        // Calcula posición relativa en Content (anclado arriba)
        float contentH = gridRoot.rect.height;
        float viewH = scrollRect.viewport.rect.height;
        if (contentH <= viewH + 1f) return;

        // y = distancia desde la parte superior del content
        float yTop = Mathf.Abs(rn.rt.anchoredPosition.y); // botones anclados arriba en tu builder
        float target = 1f - Mathf.Clamp01((yTop - viewH * 0.5f) / (contentH - viewH)); // centra el nodo
        scrollRect.verticalNormalizedPosition = target;
    }

    // Devuelve el coste a usar (por nodo o por tier)
    private MasteryCost ResolveCost(MasteryNode nd)
    {
        if (nd == null) return MasteryCost.Zero;
        if (nd.cost != null && !nd.cost.IsZero()) return nd.cost;

        var rules = MasteryCatalogManager.Instance?.Catalog?.rules;
        return rules != null ? rules.GetCostForTier(nd.tier) : MasteryCost.Zero;
    }

    // Intenta gastar, persistir y aplicar UI
    // Llama a este método desde el callback del botón "Comprar" del popup.
    private bool TrySpendAndCommitPurchase(MasteryNode node)
    {
        var pd = GameDataManager.Instance?.PlayerData;
        var inv = pd?.awakenInventory;
        if (pd == null || inv == null || node == null) return false;

        // Catálogo real (para el coste por tier si el nodo no trae coste específico)
        var cat = MasteryCatalogManager.Instance?.Catalog;
        var cost = node.cost ?? cat?.rules?.GetCostForTier(node.tier) ?? MasteryCost.Zero;

        // ¿Alcanza?
        if (!HasResourcesFor(cost))
        {
            int b = inv.Get(KEY_CAOS_BASICO), a = inv.Get(KEY_CAOS_AVANZADO), d = inv.Get(KEY_CAOS_DIVINO);
            Debug.LogWarning($"[HeroMastery] Fondos insuficientes para {node.id}. Requiere B:{cost.basico} A:{cost.avanzado} D:{cost.divino} | tienes B:{b} A:{a} D:{d}");
            return false;
        }

        // Gastar inventario de caos (persistencia automática ya la hace AwakenInventory.TrySpend)
        if (cost.basico > 0) inv.TrySpend(KEY_CAOS_BASICO, cost.basico);
        if (cost.avanzado > 0) inv.TrySpend(KEY_CAOS_AVANZADO, cost.avanzado);
        if (cost.divino > 0) inv.TrySpend(KEY_CAOS_DIVINO, cost.divino);

        // Aplicar selección al héroe actual (runtime + persistencia)
        if (!_selected.Contains(node.id)) _selected.Add(node.id);
        pd.AddMasteryForHero(_hero.heroId, node.id);   // <- método existente en PlayerData :contentReference[oaicite:2]{index=2}

        // Guardar y refrescar cabeceras/árbol/resumen
        pd.Save();                                     // usa GameDataManager.SavePlayerData internamente :contentReference[oaicite:3]{index=3}
        RefreshChaosFromPlayer();                      // re-lee los 3 contadores de caos
        UpdateHeaderCounters();
        UpdateHeaderCurrencies();
        RefreshVisualStates();                         // repinta estados/lock/lines del árbol :contentReference[oaicite:4]{index=4}
        RefreshSummaryIcons();                         // actualiza columna de resumen (si está construida)

        Debug.Log($"[HeroMastery] COMPRADO {node.id}. Guardado OK.");
        return true;
    }

    private class RuntimeNode
    {
        public MasteryNode data;
        public RectTransform rt;
        public MasteryNodeButton btn;
        public readonly List<RuntimeNode> parents = new();

        // NEW: índice usado sólo para posicionar en la fila (1..2 en T1, 1..4 en T2+)
        public int displaySlot;
    }

    private readonly List<RuntimeNode> _runtime = new();
    private readonly Dictionary<string, RuntimeNode> _byId = new();
    private readonly Dictionary<RectTransform, RuntimeNode> _rtToNode = new();
    private readonly Dictionary<string, Sprite> _iconCache = new();
    private readonly HashSet<string> _selected = new(); // ids comprados (demo)

    // Líneas
    private readonly List<UILineConnector> _lines = new();

    // Reglas por tier
    private readonly Dictionary<int, int> _tierMaxPicks = new() { { 1, 1 }, { 2, 2 }, { 3, 2 }, { 4, 2 }, { 5, 2 }, { 6, 1 } };
    private const int MaxBranchesAtTier6 = 1;  // sólo una rama permite picks en T6
    private const int MaxActiveBranches = 2;   // si hay picks en 2 ramas, 3ª bloqueada

    // Héroe actual (demo)
    private HeroProgress _hero;
    private int _rBasico = 999, _rAvanzado = 999, _rDivino = 999;

    // Cache de tamaño para relayout
    private Vector2 _lastSize;
    private bool _forceRebuild;
    // Popup
    private MasteryNodePopupUI _popup;
    private RectTransform _popupHolder;
    private void OnEnable()
    {
        _forceRebuild = true;
        _lastSize = Vector2.negativeInfinity;   // invalida la cache de tamaño
        EnsureGridAnchoredToPage(scrollRect ? scrollRect.viewport : null);
        StartCoroutine(RepositionNextFrame());  // vuelve a posicionar tras 1 frame
    }
    protected void OnRectTransformDimensionsChange()
    {
        _forceRebuild = true;
    }


    // Colores por rama
    private readonly Dictionary<string, Color> _branchColor = new()
    {
        { "ofensa",  new Color(1.00f, 0.55f, 0.10f) },
        { "defensa", new Color(0.10f, 0.85f, 0.40f) },
        { "apoyo",   new Color(0.35f, 0.75f, 1.00f) }
    };


    // Abre InfoPanel y, en el próximo frame, hace scroll hasta el item "Item_<nodeId>"
    private void OpenInfoAndScrollTo(string nodeId)
    {
        ShowSummaryInfoPanel();
        StartCoroutine(ScrollInfoToItemNextFrame(nodeId));
    }
    // Abre el InfoPanel (lista de maestrías compradas) y lo deja operativo.
    public void ShowSummaryInfoPanel()
    {
        // Referencias básicas
        if (!infoPanel)
        {
            Debug.LogWarning("[MasteryInfo] No hay 'infoPanel' asignado en el inspector.");
            return;
        }

        // Asegura layout del Scroll/Viewport/Content y el "graphic" invisible del viewport
        EnsureInfoPanelLayout();

        // Rellena la lista con las maestrías compradas (usa _selected + catálogo real)
        PopulateInfoPanel();

        // CanvasGroup para visibilidad e interacción
        var cg = infoPanel.GetComponent<CanvasGroup>();
        if (!cg) cg = infoPanel.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        cg.interactable = true;

        // Activa el GO y sitúalo encima
        infoPanel.gameObject.SetActive(true);
        infoPanel.SetAsLastSibling();

        // Overlay de dismiss (tap fuera para cerrar)
        EnsureDismissCatcher();
        if (dismissCatcher)
        {
            var dgc = dismissCatcher.GetComponent<CanvasGroup>();
            if (!dgc) dgc = dismissCatcher.gameObject.AddComponent<CanvasGroup>();
            // El overlay es totalmente transparente (alpha=0), pero capta toques.
            dgc.alpha = 0f;
            dgc.blocksRaycasts = true;
            dgc.interactable = true;
            dismissCatcher.gameObject.SetActive(true);
            // Ya conecté su botón en EnsureDismissCatcher → HideSummaryInfoPanel().
        }

        // Si tienes un panel bloqueador propio, lo engancho también a cerrar
        if (uiBlockerPanel) HookBlockerTo(HideSummaryInfoPanel);

        // Lleva el scroll al tope (arriba) en el próximo frame
        var sr = infoPanel.GetComponentInChildren<ScrollRect>(true);
        if (sr) StartCoroutine(SetScrollTopNextFrame(sr));
        AnimateResumenToRightAndShowInfo();
        Debug.Log("[MasteryInfo] ShowSummaryInfoPanel() -> mostrado y listo.");
    }



    // Hace el scroll vertical del InfoPanel hasta centrar el item de esa maestría
    private System.Collections.IEnumerator ScrollInfoToItemNextFrame(string nodeId)
    {
        // espera un frame: ShowSummaryInfoPanel() construye/rellena la lista en ese frame
        yield return null;

        if (!infoPanel || !infoListContent) yield break;
        var sr = infoPanel.GetComponentInChildren<ScrollRect>(true);
        if (!sr || !sr.viewport) yield break;

        // El Content está anclado arriba (pivot 1,1); cada fila se crea con nombre "Item_<id>"
        var tItem = infoListContent.Find($"Item_{nodeId}");
        if (!tItem) yield break;

        var itemRT = tItem as RectTransform;
        var contentRT = (RectTransform)infoListContent;

        // Alturas
        float contentH = contentRT.rect.height;
        float viewH = sr.viewport.rect.height;
        if (contentH <= viewH + 1f) yield break;

        // Con Content anclado arriba, la Y negativa del item indica distancia desde arriba.
        // Convertimos a "posición relativa" para verticalNormalizedPosition.
        float itemTop = -itemRT.anchoredPosition.y;
        // centrado aproximado del item en el viewport
        float target = 1f - Mathf.Clamp01((itemTop - viewH * 0.5f) / Mathf.Max(1f, contentH - viewH));
        sr.verticalNormalizedPosition = target;

        // trazas útiles
        Debug.Log($"[MasteryInfo] Scroll to {nodeId} -> vnPos={target:0.000}");
    }


    // ========= API =========
    public void ShowForHero(HeroProgress hero)
    {
        KillAllNodeTweens();
        _hero = hero ?? new HeroProgress();
        Log($"ShowForHero heroId={_hero.heroId} stars={_hero.stars} awaken={_hero.awaken}");

        // 1) Lee SIEMPRE desde awakenInventory y sincroniza compras antes de construir
        LoadMasteryResourcesFromPlayerData();
        SyncSelectedFromPlayerData();

        UpdateResetButtonState();
        EnsureGridAnchoredToPage(scrollRect ? scrollRect.viewport : null);

        // Pinta header
        UpdateHeaderCurrencies();   // caosífera y 3 contadores
        UpdateHeaderCounters();

        // Construye y posiciona árbol
        BuildAll();
        RepositionAll();
        ForceLinesRecalc();
        CenterResumenLayoutNow(); // ← centra y normaliza el layout en cada entrada/cambio de héroe
        if (scrollRect) scrollRect.verticalNormalizedPosition = 1f;
    }

    // Recoloca horizontalmente todas las filas del Resumen (centradas)
    private void ReflowSummaryRows()
    {
        if (!summaryGridRoot) return;

        float size  = summaryNodeSize;
        float stepX = size + summaryHSpacing;
        float rowW = summaryGridRoot.rect.width;

        for (int tier = 1; tier <= 6; tier++)
        {
            var row = summaryGridRoot.Find($"TierRow_{tier}") as RectTransform;
            if (!row) continue;

            int slots = SummarySlotsForTier(tier);
            float used  = slots * size + (slots - 1) * summaryHSpacing;
            float baseX = Mathf.Round((rowW - used) * 0.5f);

            for (int s = 0; s < row.childCount; s++)
            {
                var rt = row.GetChild(s) as RectTransform;
                if (!rt) continue;

                float x = baseX + s * stepX;
                rt.anchoredPosition = new Vector2(Mathf.Round(x), 0f);

                var btn = rt.GetComponent<Button>();
                if (btn) SizeAndCenter(btn, size);
            }
        }

        // 👉 tras colocar todo, medimos el “centro real” y guardamos el nudge
        ComputeSummaryVisualNudge();
    }

    // ========= BUILD =========
    private void BuildAll()
    {
        if (gridRoot == null || nodeButtonPrefab == null)
        {
            Warn("Faltan referencias: gridRoot o nodeButtonPrefab.");
            return;
        }

        // Limpia grid + colecciones
        foreach (Transform t in gridRoot) Destroy(t.gameObject);
        _runtime.Clear();
        _byId.Clear();
        _rtToNode.Clear();
        _lines.Clear();

        // Catálogo real
        var cat = MasteryCatalogManager.Instance;
        if (cat == null || cat.Catalog == null)
        {
            Debug.LogError("[HeroMastery] Catálogo no cargado. Asegúrate de que MasteryCatalogManager ha leído el JSON.");
            return;
        }

        var ordered = cat.AllNodes()
            .OrderBy(n => n.branch)
            .ThenBy(n => n.tier)
            .ThenBy(n => n.slot);

        foreach (var nd in ordered) // nd: MasteryNode
        {
            var btn = Instantiate(nodeButtonPrefab, gridRoot);
            btn.name = $"{nd.id}_{nd.name}";
            btn.nodeId = nd.id;
            btn.branch = nd.branch;
            btn.tier = nd.tier;
            btn.slot = nd.slot;

            // LOCK OVERLAY: solo visual (no bloquea clicks)
            var lockTr = btn.transform.Find("LockOverlay");
            if (lockTr)
            {
                lockTr.gameObject.SetActive(false);
                var li = lockTr.GetComponent<Image>();
                if (li) li.raycastTarget = false; // importantísimo: no roba el click del botón
            }

            if (_branchColor.TryGetValue(nd.branch, out var bc))
                btn.ApplyBranchStyle(bc);

            var rn = new RuntimeNode
            {
                data = nd,
                rt = btn.GetComponent<RectTransform>(),
                btn = btn
            };

            LoadIconFor(nd.id, s => btn.SetIcon(s));
            btn.button.onClick.RemoveAllListeners();
            btn.button.onClick.AddListener(() => OnNodeClicked(rn));

            _runtime.Add(rn);
            _byId[nd.id] = rn;
            _rtToNode[rn.rt] = rn;
        }

        // Padres, líneas y estados
        ResolveParentsFromCatalogOrPattern();
        ComputeDisplaySlots();
        ValidateParentIntegrity();
        BuildLines();
        RefreshVisualStates();
        RefreshLinesVisual();
    }


    // Calcula displaySlot (orden horizontal) por rama/tier en función de los padres del tier anterior.
    // T1: respeta el orden natural (slot/id). T2..T6: ordena por la media del displaySlot de sus padres.
    private void ComputeDisplaySlots()
    {
        // 1) TIER 1: dos nodos por rama; orden estable por 'slot' y, como fallback, por número de id.
        foreach (var branch in new[] { "ofensa", "defensa", "apoyo" })
        {
            var t1 = _runtime
                .Where(r => r.data.branch == branch && r.data.tier == 1)
                .OrderBy(r => r.data.slot)
                .ThenBy(r => IdNum(r.data.id))
                .ToList();

            for (int i = 0; i < t1.Count; i++)
                t1[i].displaySlot = i + 1; // 1..2
        }

        // 2) TIER 2..6: orden por posición de padres del tier anterior.
        for (int tier = 2; tier <= 6; tier++)
        {
            foreach (var branch in new[] { "ofensa", "defensa", "apoyo" })
            {
                // mapa de posiciones del tier anterior
                var prev = _runtime
                    .Where(r => r.data.branch == branch && r.data.tier == tier - 1)
                    .ToDictionary(r => r.data.id, r => r.displaySlot > 0 ? r.displaySlot : r.data.slot);

                // nodos del tier actual
                var cur = _runtime
                    .Where(r => r.data.branch == branch && r.data.tier == tier)
                    .ToList();

                // clave de orden: media de las posiciones de sus padres (si no hay, usa slot/id)
                float Key(RuntimeNode r)
                {
                    var ps = r.parents
                        .Where(p => p.data.branch == branch && p.data.tier == tier - 1)
                        .Select(p => prev.TryGetValue(p.data.id, out var s) ? s : p.data.slot)
                        .ToList();

                    if (ps.Count == 0) return r.data.slot + 0.01f * IdNum(r.data.id); // fallback robusto
                    return (float)ps.Average() + 0.01f * IdNum(r.data.id); // desempate suave por id
                }

                cur = cur.OrderBy(Key).ToList();

                for (int i = 0; i < cur.Count; i++)
                    cur[i].displaySlot = i + 1; // 1..4
            }
        }
    }


    // helper: número de la id "O3","D14","S22" -> 3,14,22
    private static int IdNum(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length < 2) return int.MaxValue;
        int n; return int.TryParse(id.Substring(1), out n) ? n : int.MaxValue;
    }


    private void ResolveParentsFromCatalogOrPattern()
    {
        // Cache de padres T1 por rama (ordenados por slot: 1..2)
        var t1ParentsMap = _runtime
            .Where(r => r.data.tier == 1)
            .GroupBy(r => r.data.branch)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(r => r.data.slot).Select(r => r.data.id).ToList()
            );

        foreach (var rn in _runtime)
        {
            rn.parents.Clear();
            var nd = rn.data;
            if (nd.tier <= 1) continue;

            // Por defecto: usa JSON si trae requiresAnyOf; si no, patrón fallback
            List<string> srcIds = (nd.requiresAnyOf != null && nd.requiresAnyOf.Count > 0)
                ? nd.requiresAnyOf
                : ParentsByPattern(nd.branch, nd.tier, nd.slot);

            // AJUSTE T1→T2 para TODAS LAS RAMAS (ofensa/defensa/apoyo):
            // Nivel 2 tendrá 3 conexiones totales distribuidas así:
            // - slot 3 -> solo padre T1 slot 1  (p.ej. O3 <- O1)
            // - slot 4 -> ambos padres          (p.ej. O4 <- O1,O2)
            // - slot 1 -> ambos padres          (p.ej. O5 <- O1,O2)
            // - slot 2 -> solo padre T1 slot 2  (p.ej. O6 <- O2)
            if (nd.tier == 2 && t1ParentsMap.TryGetValue(nd.branch, out var t1) && t1.Count >= 2)
            {
                List<string> forced;
                switch (nd.slot)
                {
                    case 3: forced = new List<string> { t1[0] }; break;
                    case 4: forced = new List<string> { t1[0], t1[1] }; break;
                    case 1: forced = new List<string> { t1[0], t1[1] }; break;
                    case 2: forced = new List<string> { t1[1] }; break;
                    default: forced = srcIds; break;
                }

                // Solo si difiere, sustituimos
                var want = new HashSet<string>(forced);
                var have = new HashSet<string>(srcIds ?? new List<string>());
                if (!want.SetEquals(have))
                {
                    srcIds = forced;
                    Debug.Log($"[HeroMastery] Ajuste T1→T2 ({nd.branch}) {nd.id}: parents=[{string.Join(",", srcIds)}]");
                }
            }

            // Aplicar padres
            foreach (var pid in srcIds.Distinct())
            {
                if (_byId.TryGetValue(pid, out var p))
                    rn.parents.Add(p);
                else
                    Debug.LogWarning($"[HeroMastery] Parent '{pid}' no encontrado para '{nd.id}'.");
            }
        }
    }


    /// <summary>
    /// Comprueba que todos los padres sean de la MISMA rama y exactamente del tier anterior.
    /// Solo logea si ve algo raro para ayudarte a cazar errores en el JSON.
    /// </summary>
    private void ValidateParentIntegrity()
    {
        foreach (var rn in _runtime)
        {
            var nd = rn.data;
            foreach (var p in rn.parents)
            {
                if (p.data.branch != nd.branch)
                    Debug.LogError($"[HeroMastery][BAD JSON] {nd.id} ({nd.branch}) tiene parent {p.data.id} de otra rama ({p.data.branch}).");

                if (p.data.tier != nd.tier - 1)
                    Debug.LogError($"[HeroMastery][BAD JSON] {nd.id} (tier {nd.tier}) tiene parent {p.data.id} (tier {p.data.tier}). Debe ser tier {nd.tier - 1}.");
            }
        }
    }


    // Crea todas las líneas (y las registra en _lines)
    private void BuildLines()
    {
        if (linePrefab == null)
        {
            Warn("No hay linePrefab asignado (Image con sprite). No se dibujarán líneas.");
            return;
        }

        // Limpia cualquier resto previo
        foreach (var lc in _lines)
            if (lc) Destroy(lc.gameObject);
        _lines.Clear();

        // Crear líneas parent->child
        foreach (var child in _runtime)
        {
            foreach (var parent in child.parents)
            {
                var img = Instantiate(linePrefab, gridRoot);
                img.name = $"line_{parent.data.id}_{child.data.id}";

                // Detrás de los nodos
                img.transform.SetAsFirstSibling();

                // Componente conector
                var lc = img.GetComponent<UILineConnector>();
                if (lc == null) lc = img.gameObject.AddComponent<UILineConnector>();

                lc.from = parent.rt;
                lc.to = child.rt;

                lc.useRelativeInset = true;
                lc.insetPercent = lineInsetPercent;
                lc.startInset = 0f;      // ignorado si useRelativeInset = true
                lc.endInset = 0f;
                lc.thickness = lineThickness;

                // Color base suave por rama
                if (_branchColor.TryGetValue(child.data.branch, out var bc))
                    lc.baseColor = new Color(bc.r, bc.g, bc.b, 0.35f);

                // 👇 **REGISTRO** imprescindible para poder ocultar/mostrar luego
                _lines.Add(lc);
            }
        }

        // Ajusta visibilidad inicial según la pestaña/rama activa
        ApplyPagerBranchVisibility();

        // Primer cálculo de geometría
        ForceLinesRecalc();
    }

    // Muestra/oculta nodos y líneas según la rama activa del pager
    private void ApplyPagerBranchVisibility()
    {
        bool showAll = string.IsNullOrEmpty(_pagerBranch);

        // Nodos
        foreach (var rn in _runtime)
        {
            bool nodeVisible = showAll || rn.data.branch == _pagerBranch;
            if (rn.btn && rn.btn.gameObject.activeSelf != nodeVisible)
                rn.btn.gameObject.SetActive(nodeVisible);
        }

        // Líneas: activas solo si AMBOS extremos pertenecen a la rama visible
        foreach (var lc in _lines)
        {
            if (!lc) continue;

            _rtToNode.TryGetValue(lc.from, out var p);
            _rtToNode.TryGetValue(lc.to, out var c);

            bool pOk = p != null && (showAll || p.data.branch == _pagerBranch);
            bool cOk = c != null && (showAll || c.data.branch == _pagerBranch);

            bool visible = pOk && cOk;
            if (lc.gameObject.activeSelf != visible)
                lc.gameObject.SetActive(visible);
        }
    }




    // ========= LAYOUT MANUAL (responsivo) =========
    void LateUpdate()
    {
        if (_forceRebuild)
        {
            RepositionAll();
            ForceLinesRecalc();
            _forceRebuild = false;
        }
    }

    private void RepositionAll()
    {
        if (gridRoot == null || _runtime.Count == 0) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(gridRoot);

        var rt = gridRoot.rect;
        if (rt.width < 10f || rt.height < 10f)
        {
            StartCoroutine(RepositionNextFrame());
            return;
        }

        Vector2 cur = new(rt.width, rt.height);
        if (!_forceRebuild && Vector2.Distance(cur, _lastSize) < 0.5f) return;
        _lastSize = cur;
        _forceRebuild = false;

        bool singleBranch = !string.IsNullOrEmpty(_pagerBranch);

        float totalH = rt.height - 2 * padding;
        float rowH = totalH / 6f;

        float colW;
        if (singleBranch)
            colW = rt.width - 2 * padding;                                  // ocupa 100%
        else
            colW = (rt.width - 2 * padding - 2 * columnSpacing) / 3f;       // 3 columnas

        // Tamaño de botón (caso peor: 4 slots por tier)
        const int baseSlotsForSizing = 4;
        float freeWFor4 = colW - (baseSlotsForSizing - 1) * horizontalSpacing;
        float nodeSizeByW = freeWFor4 / baseSlotsForSizing;
        float nodeSizeByH = rowH * rowHeightFill;

        // Límite superior por layout (para no solaparse)
        float maxByLayout = Mathf.Floor(Mathf.Min(nodeSizeByW, nodeSizeByH));

        // === TAMAÑO FINAL ===
        // Si usas tamaño fijo → toma EXACTAMENTE el valor del Inspector (clamp al máximo que cabe).
        // Si no, usa el cálculo automático respetando tu minNodeSize.
        float nodeSize = usarTamañoFijo
            ? Mathf.Clamp(tamañoNodoFijo, 32f, maxByLayout)
            : Mathf.Floor(Mathf.Max(minNodeSize, maxByLayout));
        foreach (var rn in _runtime)
        {
            int colIndex = singleBranch ? 0 : (rn.data.branch == "ofensa" ? 0 : (rn.data.branch == "defensa" ? 1 : 2));
            int slotsInTier = (rn.data.tier == 1) ? 2 : 4;

            var rtBtn = rn.rt;
            rtBtn.localScale = Vector3.one;
            rtBtn.anchorMin = new Vector2(0, 1);
            rtBtn.anchorMax = new Vector2(0, 1);
            rtBtn.pivot = new Vector2(0, 1);
            rtBtn.sizeDelta = new Vector2(nodeSize, nodeSize);

            float baseX = padding + colIndex * (colW + (singleBranch ? 0f : columnSpacing));
            float usedW = slotsInTier * nodeSize + (slotsInTier - 1) * horizontalSpacing;
            float offsetX = Mathf.Max(0f, (colW - usedW) * 0.5f);

            int slotForPos = rn.displaySlot > 0 ? rn.displaySlot : rn.data.slot;

            float x = baseX + offsetX + (slotForPos - 1) * (nodeSize + horizontalSpacing);
            float y = padding + (rn.data.tier - 1) * rowH + (rowH - nodeSize) * 0.5f;
            rtBtn.anchoredPosition = new Vector2(x, -y);
        }

        if (scrollRect && scrollRect.viewport)
            gridRoot.sizeDelta = scrollRect.viewport.rect.size;
    }

    // Ajusta tamaño/centrado del botón y de su Image hijo (hexagonal)
    // ¡IMPORTANTE!: NO desactivar el Image del GO raíz porque es el targetGraphic del Button.
    // Lo dejamos activo y transparente para que reciba el raycast.
    private static void SizeAndCenter(Button btn, float size)
    {
        // Button (sin fondo visible)
        var bg = btn.GetComponent<Image>();
        if (!bg) bg = btn.gameObject.AddComponent<Image>();

        // Debe ser el gráfico objetivo del Button para que reciba el click
#if UNITY_2021_3_OR_NEWER
        if (btn.targetGraphic != bg) btn.targetGraphic = bg;
#endif

        // Lo mantenemos ACTIVO pero invisible y con raycast habilitado
        bg.enabled = true;
        bg.color = new Color(0f, 0f, 0f, 0f);   // transparente
        bg.raycastTarget = true;

        btn.transition = Selectable.Transition.None;

        // Rect del botón
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.localScale = Vector3.one;

        // Imagen hexagonal hija (icono)
        var img = FindTemplateImage(btn);
        if (img)
        {
            var irt = img.rectTransform;
            irt.anchorMin = new Vector2(0.5f, 0.5f);
            irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = new Vector2(size, size);
            img.preserveAspect = true;

            // MUY IMPORTANTE: el hijo NO debe captar raycasts, el click es del GO raíz
            img.raycastTarget = false;
        }

        // Cualquier texto del template se apaga
        var t = btn.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (t) t.gameObject.SetActive(false);
    }

    private System.Collections.IEnumerator RepositionNextFrame()
    {
        yield return null;                      // deja que la UI calcule tamaños
        _forceRebuild = true;                   // pide un relayout en el siguiente LateUpdate
    }

    private void RefreshVisualStates()
    {
        foreach (var rn in _runtime)
        {
            bool owned = _selected.Contains(rn.data.id);
            bool canByRules = !owned && CanSelect(rn.data);
            bool canPay = canByRules && CanAfford(ResolveCost(rn.data));

            // Estado lógico para tus visuals
            MasteryVisualState state =
                owned ? MasteryVisualState.Selected
                    : (canByRules ? MasteryVisualState.Available
                                    : (IsLockedByStarsOrAwaken(rn.data) ? MasteryVisualState.Locked
                                                                        : MasteryVisualState.Unavailable));

            if (rn.btn && rn.btn.gameObject.activeInHierarchy)
            {
                // Visual original del botón
                rn.btn.SetState(state, showDot: canPay);

                // ✅ SIEMPRE clickable para poder abrir el popup, aunque esté bloqueado
                if (rn.btn.button) rn.btn.button.interactable = true;

                // LockOverlay solo como icono (nunca bloquea el click)
                var lockTf = rn.btn.transform.Find("LockOverlay");
                if (lockTf)
                {
                    bool hardLocked =
                        IsLockedByStarsOrAwaken(rn.data) || IsThirdBranchLocked(rn.data.branch);
                    lockTf.gameObject.SetActive(hardLocked);

                    var li = lockTf.GetComponent<Image>();
                    if (li) li.raycastTarget = false;
                }

                // Traza útil por nodo
                Debug.Log($"[HeroMastery] State {rn.data.id} -> {state} | owned={owned} canRules={canByRules} canPay={canPay} interactable={rn.btn.button?.interactable}");
            }
        }

        RefreshLinesVisual();
    }

    // Localiza ResetBtn si no está asignado por Inspector
    private void TryWireResetBtn()
    {
        if (resetBtn) return;

        // Ruta típica: PanelMaestria/TopPanel/ResetBtn
        var t = transform.Find("PanelMaestria/TopPanel/ResetBtn");
        if (t) resetBtn = t.GetComponent<UnityEngine.UI.Button>();
    }


    // Habilita/deshabilita "Reset" según si el héroe tiene alguna maestría comprada
    private void UpdateResetButtonState()
    {
        TryWireResetBtn();
        if (!resetBtn) return;

        // Asegura que _selected refleja PlayerData (por si aún no se sincronizó)
        if (_selected == null || _selected.Count == 0)
            SyncSelectedFromPlayerData();

        bool hasAny = _selected != null && _selected.Count > 0;

        // Interactuable real del botón
        resetBtn.interactable = hasAny;

        // Si el botón NO tiene CanvasGroup, no lo forzamos a añadir (para evitar excepciones al arrancar).
        var cg = resetBtn.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            // Feedback visual opcional
            cg.alpha = hasAny ? 1f : 0.5f;
            cg.blocksRaycasts = true;   // el bloqueo real lo controla resetBtn.interactable
            cg.interactable = true;
        }
        // Si no hay CanvasGroup, lo dejamos tal cual (sin cambiar alpha).
    }




    private void RefreshLinesVisual()
    {
        foreach (var lc in _lines)
        {
            if (!lc) continue;

            _rtToNode.TryGetValue(lc.from, out var p);
            _rtToNode.TryGetValue(lc.to, out var c);

            bool parentOwned = p != null && _selected.Contains(p.data.id);
            bool childOwned = c != null && _selected.Contains(c.data.id);
            bool childOpen = c != null && CanSelect(c.data); // ya incluye estrellas/awaken/limites tier/tercera rama

            bool strong = parentOwned && (childOwned || childOpen);

            lc.SetLit(strong);
            lc.SetPreview(!strong);
        }
    }



    private bool IsLockedByStarsOrAwaken(MasteryNode nd)
    {
        if (_hero == null) return true;
        if (_hero.stars < nd.minStars) return true;
        if (nd.requiresAwaken && !_hero.awaken) return true;
        return false;
    }


    private bool CanSelect(MasteryNode nd)
    {
        if (IsLockedByStarsOrAwaken(nd)) return false;

        int picksInTier = CountPicks(nd.branch, nd.tier);
        if (picksInTier >= _tierMaxPicks[nd.tier]) return false;

        if (nd.tier == 6 && HasAnotherBranchPickedTier6(nd.branch)) return false;
        if (IsThirdBranchLocked(nd.branch)) return false;

        // --- CLAVE: usar los padres RESUELTOS EN RUNTIME (rn.parents) ---
        if (nd.tier > 1)
        {
            // Si ya construimos la gráfica, _byId[nd.id].parents es la verdad.
            System.Collections.Generic.IEnumerable<string> parentIds = null;

            if (_byId.TryGetValue(nd.id, out var rn) && rn.parents != null && rn.parents.Count > 0)
            {
                parentIds = rn.parents.Select(p => p.data.id);
            }
            else
            {
                // Fallback al JSON si por lo que sea no hay padres en runtime
                parentIds = (nd.requiresAnyOf != null && nd.requiresAnyOf.Count > 0)
                    ? nd.requiresAnyOf
                    : System.Linq.Enumerable.Empty<string>();
            }

            bool anyParent = parentIds.Any(pid => _selected.Contains(pid));
            if (!anyParent) return false;
        }

        return true;
    }



    private int CountPicks(string branch, int tier)
    {
        int c = 0;
        foreach (var rn in _runtime)
            if (rn.data.branch == branch && rn.data.tier == tier && _selected.Contains(rn.data.id)) c++;
        return c;
    }

    private bool HasAnotherBranchPickedTier6(string branch)
    {
        foreach (var rn in _runtime)
            if (rn.data.tier == 6 && rn.data.branch != branch && _selected.Contains(rn.data.id))
                return true;
        return false;
    }

    private bool IsThirdBranchLocked(string branch)
    {
        var pickedBranches = new HashSet<string>();
        foreach (var rn in _runtime)
            if (_selected.Contains(rn.data.id)) pickedBranches.Add(rn.data.branch);

        if (pickedBranches.Count < MaxActiveBranches) return false;
        return !pickedBranches.Contains(branch);
    }

    private bool CanAfford(MasteryCost c)
    {
        if (c == null) return true;
        return _rBasico >= c.basico && _rAvanzado >= c.avanzado && _rDivino >= c.divino;
    }
    public void SetResumenRoots(RectTransform pageRt,
                                CanvasGroup summaryRootCg,
                                RectTransform gridRt,
                                Button cellTemplate)
    {
        pageResumen = pageRt;
        summaryRoot = summaryRootCg;
        summaryPageRoot = summaryRootCg ? (RectTransform)summaryRootCg.transform : null;
        summaryGridRoot = gridRt;
        summaryCellTemplate = cellTemplate;
    }
    private string BuildReqs(MasteryNode nd)
    {
        if (nd == null) return string.Empty;

        var lines = new System.Collections.Generic.List<string>();
        if (nd.minStars > 0)
            lines.Add($"{nd.minStars} {(nd.minStars == 1 ? "estrella" : "estrellas")}");
        bool awaken = nd.requiresAwaken || nd.tier >= 6;
        lines.Add(awaken ? "Despertar" : "Sin Despertar");
        return string.Join("\n", lines);
    }

    private string GetLockNoteIfAny(string buyingBranch)
    {
        var picked = new HashSet<string>();
        foreach (var r in _runtime)
            if (_selected.Contains(r.data.id)) picked.Add(r.data.branch);

        if (picked.Count == 1 && !picked.Contains(buyingBranch))
        {
            // comprar en una segunda rama -> la tercera quedará bloqueada
            string firstPicked = null;
            foreach (var b in picked) { firstPicked = b; break; }
            string third = ThirdBranch(firstPicked, buyingBranch);
            return $"Al comprar aquí, la rama <b>{Pretty(third)}</b> quedará bloqueada.";
        }
        return null;
    }

    private string ThirdBranch(string a, string b)
    {
        if (a != "ofensa" && b != "ofensa") return "ofensa";
        if (a != "defensa" && b != "defensa") return "defensa";
        return "apoyo";
    }

    private string Pretty(string br) => br switch
    {
        "ofensa" => "OFENSIVO",
        "defensa" => "DEFENSE",
        _ => "APOYO"
    };

    // -- REQS cuando vienes del catálogo REAL --
    private static string BuildReqsText(int minStars, bool requiresAwaken)
    {
        string linea1 = $"{minStars} estrellas";
        string linea2 = requiresAwaken ? "Awaken" : "Sin Despertar";
        return linea1 + "\n" + linea2;
    }

    private void OnNodeClicked(RuntimeNode rn)
    {
        if (rn == null || rn.btn == null) return;
        var nd = rn.data;
        if (nd == null) return;

        // Texto del popup
        string title = string.IsNullOrEmpty(nd.name) ? nd.id : nd.name;
        string desc = nd.description ?? string.Empty;
        string reqs = BuildReqsText(nd.minStars, nd.requiresAwaken);

        string lockMsg = GetLockNoteIfAny(nd.branch);
        if (!string.IsNullOrEmpty(lockMsg)) reqs += $"\n<i>{lockMsg}</i>";

        // Reglas de compra reales (NO bloquean la apertura del popup)
        var cost = ResolveCost(nd);
        bool alreadyOwned = _selected.Contains(nd.id);
        bool rulesOk = !alreadyOwned && CanSelect(nd);
        bool siblingChosen = (nd.tier == 1) && CountPicks(nd.branch, 1) >= 1 && !_selected.Contains(nd.id);
        bool canBuy = rulesOk && !siblingChosen && CanAfford(cost);

        Debug.Log($"[HeroMastery] CLICK {nd.id} owned={alreadyOwned} rulesOk={rulesOk} siblingChosen={siblingChosen} canBuy={canBuy}");

        // Abrir (o traer al frente) el popup SIEMPRE
        var popup = EnsurePopupInViewport();
        popup.Show(title, desc, reqs, string.Empty, canBuy, () =>
        {
            if (!canBuy) return;                 // no compramos si no procede
            if (!TrySpendAndCommitPurchase(nd))  // gasta + persiste
                return;

            rn.btn.PlayUnlockPulse();            // feedback
        });

        // Mostrar coste (aunque no sea comprable)
        popup.SetCostParts(cost.basico, cost.avanzado, cost.divino);
    }

    // Llama a esto tras relayout o cambio de página para que las líneas encajen.
    public void ForceLinesRecalc()
    {
        if (_lines == null) return;
        foreach (var lc in _lines)
            if (lc) lc.ForceUpdateNow();
    }
    [ContextMenu("DEBUG/Log Popup Wiring")]
    private void __DebugLogPopupWiring()
    {
        Debug.Log($"[MasteryPopup] popupLayer={(popupLayer ? popupLayer.name : "NULL")}, rootCanvas={(rootCanvas ? rootCanvas.name : "NULL")}, prefab={(popupPrefab ? popupPrefab.name : "NULL")}");
        var gr = popupLayer ? popupLayer.GetComponent<GraphicRaycaster>() : null;
        var cv = popupLayer ? popupLayer.GetComponent<Canvas>() : null;
        Debug.Log($"[MasteryPopup] popupLayer Canvas={(cv ? "OK" : "MISSING")} GR={(gr ? "OK" : "MISSING")}");
    }

    // Instancia el prefab en PopupLayer (si existe) o crea una capa propia con Canvas encima de todo
    private MasteryNodePopupUI EnsurePopupInViewport()
    {
        if (_popup != null)
        {
            BringPopupToFront();
            CenterPopup();
            // garantía extra por si algún tween lo puso a 0
            var cg0 = _popup.GetComponent<CanvasGroup>() ?? _popup.gameObject.AddComponent<CanvasGroup>();
            cg0.alpha = 1f; cg0.blocksRaycasts = true; cg0.interactable = true;
            _popup.gameObject.SetActive(true);
            return _popup;
        }

        // 1) Padre preferido: el asignado en el Inspector (PanelMaestria/PopupLayer)
        RectTransform parent = popupLayer;

        // 2) Si no está asignado, intenta localizar un GO llamado "PopupLayer"
        if (!parent)
        {
            var found = GameObject.Find("PopupLayer");
            if (found) parent = found.transform as RectTransform;
        }

        // 3) Si sigue sin existir, créalo bajo el Canvas raíz (todo pantalla)
        if (!parent)
        {
            var go = new GameObject("PopupLayer", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            parent = go.transform as RectTransform;

            var rootCv = rootCanvas ? rootCanvas : GetComponentInParent<Canvas>();
            parent.SetParent(rootCv ? rootCv.transform : transform, false);
        }

        // 4) Estirado a pantalla completa
        parent.anchorMin = Vector2.zero;
        parent.anchorMax = Vector2.one;
        parent.pivot = new Vector2(0.5f, 0.5f);
        parent.offsetMin = Vector2.zero;
        parent.offsetMax = Vector2.zero;

        // 5) Canvas SIEMPRE por encima de todo
        var c = parent.GetComponent<Canvas>();
        if (!c) c = parent.gameObject.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = short.MaxValue - 100;

        if (!parent.TryGetComponent<GraphicRaycaster>(out _))
            parent.gameObject.AddComponent<GraphicRaycaster>();

        parent.SetAsLastSibling();
        _popupHolder = parent;

        // 6) Instanciar el popup y asegurar Canvas, GraphicRaycaster y CanvasGroup
        _popup = Instantiate(popupPrefab, _popupHolder, false);

        // canvas propio del popup, un punto por encima
        var pc = _popup.GetComponent<Canvas>() ?? _popup.gameObject.AddComponent<Canvas>();
        pc.overrideSorting = true;
        pc.sortingOrder = c.sortingOrder + 1;

        if (!_popup.TryGetComponent<GraphicRaycaster>(out _))
            _popup.gameObject.AddComponent<GraphicRaycaster>();

        var cg = _popup.GetComponent<CanvasGroup>();
        if (!cg) cg = _popup.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        cg.interactable = true;

        // por si el prefab viene desactivado
        _popup.gameObject.SetActive(true);

        BringPopupToFront();
        CenterPopup();

        // trazas útiles
        Debug.Log($"[MasteryPopup] Instanciado. holder={_popupHolder?.name} order={pc.sortingOrder} alpha={cg.alpha}");

        return _popup;
    }

    // Asegura que tanto la capa como el popup queden los últimos en su padre
    private void BringPopupToFront()
    {
        if (_popupHolder) _popupHolder.SetAsLastSibling();
        if (_popup) _popup.transform.SetAsLastSibling();
    }


    private void CenterPopup()
    {
        if (_popup == null) return;

        Canvas.ForceUpdateCanvases();

        // Si tu MasteryNodePopupUI tiene FitToContainer, úsalo (mantengo tu tamaño/animaciones)
        _popup.FitToContainer(_popupHolder, heightFraction: 0.5f, margin: 16f);

        // Salvaguarda: anclado centrado por si el prefab viene con offsets raros
        var rt = (RectTransform)_popup.transform;
        rt.anchorMin = new Vector2(0.1f, 0.25f);
        rt.anchorMax = new Vector2(0.9f, 0.85f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // === Pinta contadores del header (3 materiales + caosífera) ===
    private void UpdateHeaderCurrencies()
    {
        var pd = PD;
        if (pd == null)
        {
            if (txtBasico) txtBasico.text = "0";
            if (txtAvanzado) txtAvanzado.text = "0";
            if (txtDivino) txtDivino.text = "0";
            if (txtCaosfera) txtCaosfera.text = "0";
            return;
        }

        // Los tres de maestría salen de _r* (que carga masteryMaterials)
        if (txtBasico) txtBasico.text = _rBasico.ToString();
        if (txtAvanzado) txtAvanzado.text = _rAvanzado.ToString();
        if (txtDivino) txtDivino.text = _rDivino.ToString();

        // La moneda general de reset/costos (según tu PlayerData)
        if (txtCaosfera) txtCaosfera.text = Mathf.Max(0, pd.caosifera).ToString();
    }

    // === Helper: popup de reset con coste caosífera + refresco visual inmediato ===
    private void ShowResetPopupForCurrentHero()
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.PlayerData == null || _hero == null)
            return;

        GetResetOfferForCurrentHero(out bool esGratis, out string moneda, out int coste);

        var popup = EnsurePopupInViewport();
        if (popup != null)
        {
            string title = "Resetear Maestrías";
            string okText = esGratis ? "Reset (Gratis)" : $"Pagar {coste:N0} {moneda} y resetear";
            string mensaje = esGratis
                ? $"Vas a resetear TODAS las maestrías de <b>{HeroNameUI()}</b>.\n\n<b>¡Esta primera vez es GRATIS!</b>"
                : $"Vas a resetear TODAS las maestrías de <b>{HeroNameUI()}</b>.\n\nCoste: <b>{coste:N0}</b> {moneda}.";

            popup.ShowCustom(
                title,
                mensaje,
                okText,
                "Cancelar",
                onOk: () =>
                {
                    var pd = GameDataManager.Instance.PlayerData;

                    if (!esGratis)
                    {
                        if (!pd.TrySpendByCurrency(moneda, coste, out var err))
                        {
                            popup.ShowCustom("Sin recursos", err, "Entendido", null, null);
                            return;
                        }

                        // Guarda y REFRESCA el contador de caosífera del header inmediatamente
                        pd.SaveSelf();
                        UpdateHeaderCurrencies();
                    }

                    ApplyResetForCurrentHero(true);
                },
                onCancel: () => { }
            );
            return;
        }

#if UNITY_EDITOR
    // Fallback Editor por si no hay popup
    if (UnityEditor.EditorUtility.DisplayDialog(
        "Resetear Maestrías",
        esGratis
            ? $"Resetear TODAS las maestrías de {HeroNameUI()}.\n\nEs GRATIS. ¿Continuar?"
            : $"Resetear TODAS las maestrías de {HeroNameUI()}.\n\nCoste: {coste:N0} {moneda}. ¿Continuar?",
        esGratis ? "Reset (Gratis)" : "Pagar y Resetear",
        "Cancelar"))
    {
        var pd = GameDataManager.Instance.PlayerData;
        if (!esGratis)
        {
            if (!pd.TrySpendByCurrency(moneda, coste, out var err))
            {
                UnityEditor.EditorUtility.DisplayDialog("Sin recursos", err, "Entendido");
                return;
            }
            pd.SaveSelf();
            UpdateHeaderCurrencies();
        }
        ApplyResetForCurrentHero(true);
    }
#endif
    }




    private void UpdateHeaderCounters()
    {
        if (txtBasico) txtBasico.text = _rBasico.ToString();
        if (txtAvanzado) txtAvanzado.text = _rAvanzado.ToString();
        if (txtDivino) txtDivino.text = _rDivino.ToString();
    }

    // ========= ICONOS =========

    private void LoadIconFor(string id, Action<Sprite> onReady)
    {
        if (string.IsNullOrEmpty(id)) { onReady?.Invoke(null); return; }
        if (_iconCache.TryGetValue(id, out var cached)) { onReady?.Invoke(cached); return; }

        // Clave EXACTA que usas en el proyecto:
        string key = $"Assets/Addressables/Art/HeroScene/Maestries/{id}.png";
        var handle = Addressables.LoadAssetAsync<Sprite>(key);
        handle.Completed += op =>
        {
            if (op.Status == AsyncOperationStatus.Succeeded && op.Result != null)
            {
                _iconCache[id] = op.Result;
                onReady?.Invoke(op.Result);
            }
            else
            {
                Debug.LogError($"[HeroMastery] ICON NOT FOUND: {key}");
                onReady?.Invoke(null);
            }
        };
    }

    // Aplica margen superior y garantiza anclajes del root y el grid (no cambia el ancho)
    public void ApplyResumenTopMargin()
    {
        if (!summaryPageRoot || !summaryGridRoot) return;

        // SummaryPageRoot ocupa toda la page
        var rtRoot = summaryPageRoot;
        rtRoot.anchorMin = new Vector2(0f, 0f);
        rtRoot.anchorMax = new Vector2(1f, 1f);
        rtRoot.pivot = new Vector2(0.5f, 1f);
        rtRoot.offsetMin = Vector2.zero;
        rtRoot.offsetMax = Vector2.zero;

        // El grid se gestiona centrado (CenterResumenLayoutNow define tamaño y posición)
        summaryGridRoot.anchorMin = new Vector2(0.5f, 1f);
        summaryGridRoot.anchorMax = new Vector2(0.5f, 1f);
        summaryGridRoot.pivot = new Vector2(0.5f, 1f);
        summaryGridRoot.anchoredPosition = new Vector2(0f, -resumenTopMargin);
    }


    // ========= DEMO CATALOGO =========
    private static readonly int[] TierStart = { 0, 1, 3, 7, 11, 15, 19 };

    private List<string> ParentsByPattern(string branch, int tier, int slot)
    {
        var res = new List<string>();
        if (tier <= 1) return res;

        int prevTier = tier - 1;
        List<int> parentSlots = new();
        if (tier == 2)
        {
            parentSlots = (slot <= 2) ? new List<int> { 1 } : new List<int> { 2 };
        }
        else
        {
            switch (slot)
            {
                case 1: parentSlots.AddRange(new[] { 1, 2 }); break;
                case 2: parentSlots.AddRange(new[] { 1, 2, 3 }); break;
                case 3: parentSlots.AddRange(new[] { 2, 3, 4 }); break;
                case 4: parentSlots.AddRange(new[] { 3, 4 }); break;
            }
        }

        foreach (int ps in parentSlots)
        {
            int pIdx = TierStart[prevTier] + (ps - 1);
            string pid = $"{(branch == "ofensa" ? "O" : branch == "defensa" ? "D" : "S")}{pIdx}";
            res.Add(pid);
        }
        return res;
    }

    // ========= LOGS & TOOLS =========
    private void Log(string msg) { if (debugLogs) Debug.Log("[HeroMastery] " + msg); }
    private void Warn(string msg) { Debug.LogWarning("[HeroMastery] " + msg); }

    [ContextMenu("Rebuild Context")]
    public void RebuildContext()
    {
        EnsureGridAnchoredToPage(scrollRect ? scrollRect.viewport : null);
        UpdateHeaderCounters();
        BuildAll();
        RepositionAll();
        ForceLinesRecalc();
        if (scrollRect) scrollRect.verticalNormalizedPosition = 1f;
    }

    public void RebuildContext(HeroProgress hero, bool keepSelection = true)
    {
        if (!keepSelection) _selected.Clear();
        _hero = hero ?? _hero ?? new HeroProgress();
        RebuildContext();
    }
    private void SetCanvas(CanvasGroup cg, float a, bool enable)
    {
        if (cg == null) return;
        cg.alpha = a;
        cg.blocksRaycasts = enable;
        cg.interactable = enable;
    }


    // --- helper: nº de slots que debe tener cada tier en el RESUMEN ---
    private static int SummarySlotsForTier(int tier)
    {
        if (tier == 1) return 2;   // T1 → 2
        if (tier == 6) return 1;   // T6 → 1
        return 4;                  // T2..T5 → 4
    }


    // Construcción del resumen: deja todo preparado pero con el grid centrado
    private void EnsureSummaryBuilt()
    {
        if (summaryRoot == null || summaryGridRoot == null || summaryCellTemplate == null)
            return;

        ApplyResumenTopMargin();

        // Importante: el template siempre oculto
        summaryCellTemplate.gameObject.SetActive(false);
        _summaryImages.Clear();

        // Geometría base
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(summaryGridRoot);

        float size = summaryNodeSize;
        float rowStep = size + summaryVSpacing;
        float top = summaryTopOffset;

        for (int tier = 1; tier <= 6; tier++)
        {
            var row = summaryGridRoot.Find($"TierRow_{tier}") as RectTransform;
            if (!row)
            {
                Debug.LogWarning($"[Resumen] Falta TierRow_{tier} en SummaryPageRoot/GridRoot");
                continue;
            }

            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0f, 1f);
            row.anchoredPosition = new Vector2(0f, -(top + (tier - 1) * rowStep));

            for (int i = row.childCount - 1; i >= 0; i--)
                Destroy(row.GetChild(i).gameObject);

            int slots = SummarySlotsForTier(tier);
            float usedW = slots * size + (slots - 1) * summaryHSpacing;

            // ancho real ya calculado por el grid
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(row);
            float rowW = summaryGridRoot.rect.width > 1f ? summaryGridRoot.rect.width : row.rect.width;

            // >>> CENTRADO SIN NUDGE
            float baseX = Mathf.Round((rowW - usedW) * 0.5f);

            for (int s = 1; s <= slots; s++)
            {
                var go = Instantiate(summaryCellTemplate.gameObject, row, false);
                go.name = $"SummaryCell_T{tier}_S{s}";
                go.SetActive(true);

                var btn = EnsureButton(go);
                NormalizeSummaryCell(btn);
                SizeAndCenter(btn, size);

                var rt = btn.GetComponent<RectTransform>();
                float x = baseX + (s - 1) * (size + summaryHSpacing);
                rt.anchoredPosition = new Vector2(Mathf.Round(x), 0f);

                var img = FindTemplateImage(btn);
                if (img) _summaryImages.Add(img);

                btn.interactable = false;
                btn.onClick.RemoveAllListeners();
            }
        }

        _summaryBuilt = true;
        DisableSummaryBackgroundRaycasts();   // muy importante para que los botones sean clicables
        CenterResumenLayoutNow();
        ReflowSummaryRows();
        RefreshSummaryIcons();
    }
    // Devuelve el Image a usar dentro de una celda de resumen (tu hex). Si no existe, crea uno.
    private static Image FindTemplateImage(Button btn)
    {
        // Prioriza un hijo llamado "Image"
        var child = btn.transform.Find("Image");
        if (child)
        {
            var img = child.GetComponent<Image>();
            if (img) return img;
        }

        // Fallback: cualquier Image hijo
        var any = btn.GetComponentsInChildren<Image>(true);
        foreach (var i in any)
            if (i.gameObject != btn.gameObject) return i;

        // Último recurso: crea uno como hijo (hex genérico)
        var go = new GameObject("Image", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(btn.transform, false);
        return go.GetComponent<Image>();
    }


    // Desactiva raycasts en todos los gráficos del área de Resumen,
    // dejando SOLO los gráficos que sean el targetGraphic de un Selectable (Button, Toggle, etc.)
    private void DisableSummaryBackgroundRaycasts()
    {
        // Alcance: toda la página de Resumen si está asignada; si no, el grid.
        var scope = pageResumen ? (Transform)pageResumen : (Transform)summaryGridRoot;
        if (!scope) return;

        // Todos los Graphics (Image, RawImage, TextMeshProUGUI no tiene raycast por defecto, pero por si acaso)
        var graphics = scope.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        int kept = 0, disabled = 0;

        foreach (var g in graphics)
        {
            // ¿forma parte de una celda? (hay un Button en sus padres)
            var btnParent = g.GetComponentInParent<UnityEngine.UI.Button>(true);

            // ¿es el gráfico objetivo de un Selectable (Button/Toggle/Scrollbar...) en el MISMO GO?
            var sel = g.GetComponent<UnityEngine.UI.Selectable>();
            bool isTargetOfSelectable = sel != null && sel.targetGraphic == g;

            // Conserva raycast SOLO si es el gráfico objetivo del control interactivo
            bool keepRaycast = isTargetOfSelectable;

            // Las imágenes hijas del botón (por ejemplo, el hex) NO deben captar raycast
            if (btnParent && !isTargetOfSelectable)
                keepRaycast = false;

            // Aplica
            g.raycastTarget = keepRaycast;
            if (keepRaycast) kept++; else disabled++;
        }

        Debug.Log($"[Resumen] Raycast sanitizado. keep={kept} disabled={disabled}");
    }

    // Garantiza que cada celda tenga Button->targetGraphic bien enlazado y el hex hijo sin raycast
    private void NormalizeSummaryCell(Button btn)
    {
        if (!btn) return;

        var rootImg = btn.GetComponent<Image>();
        if (!rootImg) rootImg = btn.gameObject.AddComponent<Image>();
#if UNITY_2021_3_OR_NEWER
        btn.targetGraphic = rootImg;
#endif
        rootImg.raycastTarget = true;

        // Hex hijo: sin raycast para no robar el click del botón
        var childHex = btn.transform.Find("Image")?.GetComponent<Image>();
        if (childHex) childHex.raycastTarget = false;
    }


    // --- Refresco de iconos del resumen (rellena por TIER con compradas y huecos) ---
    private void RefreshSummaryIcons()
    {
        if (!_summaryBuilt) return;

        // Asegura que _selected coincide con PlayerData
        SyncSelectedFromPlayerData();

        MasteryNode FindNodeById(string id) => MasteryCatalogManager.Instance?.GetNode(id);

        var boughtByTier = new Dictionary<int, List<MasteryNode>>();
        for (int t = 1; t <= 6; t++) boughtByTier[t] = new List<MasteryNode>();

        foreach (var id in _selected)
        {
            var n = FindNodeById(id);
            if (n != null && boughtByTier.ContainsKey(n.tier))
                boughtByTier[n.tier].Add(n);
        }

        int BranchOrder(string b) => (b == "ofensa") ? 0 : (b == "defensa" ? 1 : 2);
        int IdNumLocal(string nid)
        {
            if (string.IsNullOrEmpty(nid) || nid.Length < 2) return int.MaxValue;
            return int.TryParse(nid.Substring(1), out var v) ? v : int.MaxValue;
        }

        for (int t = 1; t <= 6; t++)
            boughtByTier[t].Sort((a, b) =>
            {
                int c = BranchOrder(a.branch).CompareTo(BranchOrder(b.branch));
                if (c != 0) return c;
                c = a.slot.CompareTo(b.slot);
                if (c != 0) return c;
                return IdNumLocal(a.id).CompareTo(IdNumLocal(b.id));
            });

        int idx = 0;
        for (int tier = 1; tier <= 6; tier++)
        {
            int quota = SummarySlotsForTier(tier);
            var list = boughtByTier[tier];

            // ----- Celdas con maestrías compradas (activan Button + click → Info) -----
            for (int i = 0; i < list.Count && i < quota && idx < _summaryImages.Count; i++, idx++)
            {
                var img = _summaryImages[idx];
                var node = list[i];

                LoadIconFor(node.id, spr =>
                {
                    if (!img) return;
                    img.sprite = spr;
                    img.color = Color.white;
                    img.enabled = true;
                });

                // Asegura que el padre TIENE Button y lo activa sólo para compradas
                var btn = img ? img.GetComponentInParent<Button>() : null;
                if (!btn) btn = EnsureButton(img.transform.parent.gameObject);

                btn.interactable = true;
                btn.onClick.RemoveAllListeners();

                string capturedId = node.id;
                btn.onClick.AddListener(() =>
                {
                    Debug.Log($"[Resumen] Click en {capturedId} → abrir InfoPanel y hacer scroll.");
                    OpenInfoAndScrollTo(capturedId); // abre InfoPanel + centra el item
                });
            }

            // ----- Huecos (placeholder) sin interacción -----
            for (int r = list.Count; r < quota && idx < _summaryImages.Count; r++, idx++)
            {
                var img = _summaryImages[idx];
                img.sprite = summaryEmptySprite;
                img.color = summaryEmptySprite ? new Color(1f, 1f, 1f, 0.35f)
                                            : new Color(1f, 1f, 1f, 0.1f);
                img.enabled = true;

                var btn = img ? img.GetComponentInParent<Button>() : null;
                if (btn)
                {
                    btn.interactable = false;
                    btn.onClick.RemoveAllListeners();
                }
            }
        }

        Debug.Log($"[Resumen] Iconos refrescados. Compradas={_selected.Count}, celdas={_summaryImages.Count}");
    }

    // Asegura que el GO tiene Button + Image correctamente cableado para recibir clicks
    private static Button EnsureButton(GameObject go)
    {
        var btn = go.GetComponent<Button>();
        var img = go.GetComponent<Image>();

        if (img == null) img = go.AddComponent<Image>();
        img.raycastTarget = true;          // el click lo recibe el GO raíz

        if (btn == null) btn = go.AddComponent<Button>();
#if UNITY_2021_3_OR_NEWER
        btn.targetGraphic = img;           // que el propio Image del GO sea el target del botón
#endif
        btn.transition = Selectable.Transition.None;
        return btn;
    }



    public void SetTreeVisible(bool visible)
    {
        if (!gridRoot) return;
        var cg = gridRoot.GetComponent<CanvasGroup>() ?? gridRoot.gameObject.AddComponent<CanvasGroup>();
        SetCanvas(cg, visible ? 1f : 0f, visible);
        // Apágalo para evitar que “se vea” nada aunque tenga alpha 1 por algún efecto
        gridRoot.gameObject.SetActive(visible);
    }

    private void EnsureInfoPanelLayout()
    {
        // Canvas raíz (para RectTransformUtility y cámara)
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();

        var sr = infoPanel ? infoPanel.GetComponentInChildren<ScrollRect>(true) : null;
        if (!sr || !infoPanelViewport || !infoListContent) return;

        // Scroll: SOLO vertical
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.inertia = true;
        sr.scrollSensitivity = 20f;
        sr.elasticity = 0.15f;

        // Enlaza referencias del ScrollRect
        sr.viewport = infoPanelViewport;
        sr.content = (RectTransform)infoListContent;

        // Viewport = todo el panel
        var vp = infoPanelViewport;
        vp.anchorMin = Vector2.zero;
        vp.anchorMax = Vector2.one;
        vp.pivot = new Vector2(0.5f, 1f);
        vp.offsetMin = Vector2.zero;
        vp.offsetMax = Vector2.zero;

        // MUY IMPORTANTE: el Viewport necesita un Graphic para recibir drag.
        // Si no tiene ninguno, le ponemos un Image invisible (no afecta visualmente).
        var graphic = vp.GetComponent<MaskableGraphic>();
        if (!graphic)
        {
            var img = vp.gameObject.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0);   // totalmente transparente
            img.raycastTarget = true;
        }

        // Content: anclado arriba y creciendo por preferencia
        var contentRT = (RectTransform)infoListContent;
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;

        var vlg = contentRT.GetComponent<VerticalLayoutGroup>() ??
                  contentRT.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.childControlHeight = true;   // cada item decide su alto (texto auto-size)
        vlg.childForceExpandHeight = false;
        vlg.spacing = 6;
        vlg.padding = new RectOffset(10, 12, 8, 12);

        var fitter = contentRT.GetComponent<ContentSizeFitter>() ??
                     contentRT.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private Color BranchColor(string branch)
    {
        if (_branchColor != null && _branchColor.TryGetValue(branch, out var c)) return c;
        return Color.white;
    }

    // El Grid del árbol debe ocupar exactamente 1 página (el viewport del pager)
    public void AttachTreeToPager(RectTransform pagerContent, RectTransform viewport)
    {
        if (!gridRoot || !pagerContent) return;
        if (gridRoot.parent != pagerContent)
            gridRoot.SetParent(pagerContent, false);

        EnsureGridAnchoredToPage(viewport);
    }

    // Ancla el Grid a 1 página
    public void EnsureGridAnchoredToPage(RectTransform viewport)
    {
        if (!gridRoot) return;
        var vp = viewport ? viewport : (scrollRect ? scrollRect.viewport : null);

        gridRoot.anchorMin = new Vector2(0f, 1f);
        gridRoot.anchorMax = new Vector2(0f, 1f);
        gridRoot.pivot = new Vector2(0f, 1f);
        gridRoot.anchoredPosition = Vector2.zero;

        if (vp)
            gridRoot.sizeDelta = new Vector2(vp.rect.width, vp.rect.height);
    }

    public void SetTreePageIndex(int pageIndex, float pageWidth)
    {
        if (!gridRoot) return;
        gridRoot.anchoredPosition = new Vector2(pageIndex * Mathf.Max(1f, pageWidth), 0f);
    }

    // Reconstruye la lista con tamaño uniforme: iconos iguales y texto a "3 líneas" de alto fijo
    private void PopulateInfoPanel()
    {
        if (!infoListContent || !infoItemTemplate) return;

        // Limpia clones anteriores dejando el template
        for (int i = infoListContent.childCount - 1; i >= 0; i--)
        {
            var t = infoListContent.GetChild(i);
            if (t && t.gameObject != infoItemTemplate) Destroy(t.gameObject);
        }

        // Datos actualizados desde PlayerData
        SyncSelectedFromPlayerData();

        // Orden: rama → tier → slot → id num
        MasteryNode FindNode(string nid) => MasteryCatalogManager.Instance?.GetNode(nid);
        int BranchOrder(string b) => b == "ofensa" ? 0 : (b == "defensa" ? 1 : 2);
        int IdNumLocal(string nid)
        {
            if (string.IsNullOrEmpty(nid) || nid.Length < 2) return int.MaxValue;
            return int.TryParse(nid.Substring(1), out var v) ? v : int.MaxValue;
        }

        var ordered = _selected
            .Select(id => FindNode(id))
            .Where(n => n != null)
            .OrderBy(n => BranchOrder(n.branch))
            .ThenBy(n => n.tier)
            .ThenBy(n => n.slot)
            .ThenBy(n => IdNumLocal(n.id))
            .ToList();

        const float LeftPadding = 12f;
        const float RightPadding = 12f;

        foreach (var n in ordered)
        {
            // Instancia el template bajo Content (el VLG lo posiciona)
            var go = Instantiate(infoItemTemplate, infoListContent, false);
            go.name = $"Item_{n.id}";
            go.SetActive(true);

            var itemRT = go.GetComponent<RectTransform>();
            itemRT.anchorMin = new Vector2(0f, 1f);
            itemRT.anchorMax = new Vector2(1f, 1f);
            itemRT.pivot = new Vector2(0.5f, 1f);

            // Cada fila ajusta su alto al texto (mínimo por icono)
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            le.minHeight = Mathf.Max(infoItemMinHeight, infoIconSize + 14f);
            le.preferredHeight = -1;
            le.flexibleHeight = 0;

            var itemFitter = go.GetComponent<ContentSizeFitter>() ?? go.AddComponent<ContentSizeFitter>();
            itemFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            itemFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // --- ICONO ---
            var icon = go.transform.Find("Image")?.GetComponent<Image>();
            if (icon)
            {
                icon.raycastTarget = false;
                var irt = icon.rectTransform;
                irt.anchorMin = new Vector2(0f, 0.5f);
                irt.anchorMax = new Vector2(0f, 0.5f);
                irt.pivot = new Vector2(0.5f, 0.5f);
                irt.anchoredPosition = new Vector2(LeftPadding + infoIconSize * 0.5f, 0f);
                irt.sizeDelta = new Vector2(infoIconSize, infoIconSize);
                icon.preserveAspect = true;

                icon.sprite = fallbackIcon;
                LoadIconFor(n.id, s => { if (icon) icon.sprite = s ? s : fallbackIcon; });
            }

            // --- TEXTO (todo visible, con AutoSize máx 25) ---
            var txt = go.transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (txt)
            {
                txt.raycastTarget = false;             // no roba el drag del scroll
                txt.enableAutoSizing = true;
                txt.fontSizeMax = 25f;               // << solicitado
                txt.fontSizeMin = 16f;               // margen inferior razonable
                txt.enableWordWrapping = true;
                txt.overflowMode = TextOverflowModes.Overflow;
                txt.alignment = TextAlignmentOptions.TopLeft;

                // Ocupa todo el ancho menos el bloque del icono
                var trt = txt.rectTransform;
                trt.anchorMin = Vector2.zero; // stretch
                trt.anchorMax = Vector2.one;
                trt.pivot = new Vector2(0f, 1f);

                float left = LeftPadding + infoIconSize + 10f;
                trt.offsetMin = new Vector2(left, 8f);            // left & bottom
                trt.offsetMax = new Vector2(-RightPadding, -8f);  // right & top

                string name = string.IsNullOrEmpty(n.name) ? n.id : n.name;
                string desc = string.IsNullOrEmpty(n.description) ? "" : n.description;

                // Nombre en negrita + descripción completa debajo
                txt.text = $"<b>{name}</b>\n{desc}";
            }

            // Recalcula esta fila para que contribuya a la altura del Content
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemRT);
        }

        Canvas.ForceUpdateCanvases();
    }

    private void KillAllNodeTweens()
    {
        var buttons = GetComponentsInChildren<MasteryNodeButton>(true);
        if (buttons == null || buttons.Length == 0) return;

        foreach (var b in buttons)
        {
            if (!b) continue;
            var cg = b.GetComponent<CanvasGroup>();
            if (cg) DG.Tweening.DOTween.Kill(cg, complete: false);
        }
    }


    // Oculta el panel y el bloqueador
    public void HideSummaryInfoPanel()
    {
        if (infoPanel) infoPanel.gameObject.SetActive(false);

        if (dismissCatcher)
        {
            var cg = dismissCatcher.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
            dismissCatcher.gameObject.SetActive(false);
        }
        AnimateResumenBackToCenter();
    }

    // Conecta el UIBlockerPanel para cerrar el InfoPanel con un tap en cualquier parte
    private void HookBlockerTo(System.Action onClick)
    {
        var btn = uiBlockerPanel.GetComponent<UnityEngine.UI.Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClick?.Invoke());
        }
        // Si en lugar de Button usas EventTrigger, no toco nada: el bloqueador ya capturará el click
    }

    // Nombre que mostraremos para el héroe (seguro aunque no exista heroName)
    private string HeroNameUI()
    {
        return (_hero != null && !string.IsNullOrEmpty(_hero.heroId)) ? _hero.heroId : "Héroe";
    }


    // Botón Reset (lo definimos más tarde)
    // LLAMAR desde el botón "Reset"
    // Botón "Reset" de maestrías
    public void OnClickResetMasteries()
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.PlayerData == null || _hero == null)
            return;

        GetResetOfferForCurrentHero(out bool esGratis, out string moneda, out int coste);

        var popup = EnsurePopupInViewport(); // reutilizamos tu popup
        if (popup != null)
        {
            string title = "Resetear Maestrías";
            string mensaje = esGratis
                ? $"Vas a resetear TODAS las maestrías de <b>{HeroNameUI()}</b>.\n\n<b>¡Esta primera vez es GRATIS!</b>"
                : $"Vas a resetear TODAS las maestrías de <b>{HeroNameUI()}</b>.\n\nCoste: <b>{coste}</b> {moneda}.";

            popup.ShowCustom(
                title,
                mensaje,
                okText: esGratis ? "Reset (Gratis)" : "Pagar y Resetear",
                cancelText: "Cancelar",
                onOk: () =>
                {
                    if (!esGratis)
                    {
                        var pd = GameDataManager.Instance.PlayerData;
                        if (!pd.TrySpendByCurrency(moneda, coste, out var err))
                        {
                            popup.ShowCustom("Sin recursos", err, "Entendido", null, null);
                            return;
                        }
                    }
                    ApplyResetForCurrentHero(true);
                },
                onCancel: () => { }
            );
            return;
        }

#if UNITY_EDITOR
    if (UnityEditor.EditorUtility.DisplayDialog(
        "Resetear Maestrías",
        esGratis
            ? $"Resetear TODAS las maestrías de {HeroNameUI()}.\n\nEs GRATIS. ¿Continuar?"
            : $"Resetear TODAS las maestrías de {HeroNameUI()}.\n\nCoste: {coste} {moneda}. ¿Continuar?",
        esGratis ? "Reset (Gratis)" : "Pagar y Resetear",
        "Cancelar"))
    {
        if (!esGratis)
        {
            var pd = GameDataManager.Instance.PlayerData;
            if (!pd.TrySpendByCurrency(moneda, coste, out var err))
            {
                UnityEditor.EditorUtility.DisplayDialog("Sin recursos", err, "Entendido");
                return;
            }
        }
        ApplyResetForCurrentHero(true);
    }
#endif
    }


    private System.Collections.IEnumerator SetScrollTopNextFrame(ScrollRect sr)
    {
        yield return null; // espera a que el Content calcule su altura
        if (sr)
        {
            sr.StopMovement();
            sr.verticalNormalizedPosition = 1f; // arriba del todo
        }
    }

    // Centra el grid usando SIEMPRE el ancho de SummaryPageRoot

    private void CenterResumenLayoutNow()
    {
        if (!summaryPageRoot || !summaryGridRoot) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(summaryPageRoot);

        var m = GetResumenMetrics();

        summaryGridRoot.anchorMin = new Vector2(0.5f, 1f);
        summaryGridRoot.anchorMax = new Vector2(0.5f, 1f);
        summaryGridRoot.pivot     = new Vector2(0.5f, 1f);

        float w = Mathf.Round(m.centerW);
        summaryGridRoot.sizeDelta        = new Vector2(w, 0f);

        // re-colocamos filas y recalculamos nudge
        ReflowSummaryRows();

        // aplicamos el nudge para que el centro visual quede perfecto
        summaryGridRoot.anchoredPosition = new Vector2(_summaryVisualNudgeX, -resumenTopMargin);
        
    }


    // Anima: grid → mitad derecha de SummaryPageRoot e InfoPanel entra en mitad izquierda
    public void AnimateResumenToRightAndShowInfo()
    {
        if (!summaryGridRoot || !infoPanel || !summaryPageRoot) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(summaryPageRoot);

        var m = GetResumenMetrics();

        summaryGridRoot.DOKill();
        summaryGridRoot.anchorMin = new Vector2(0.5f, 1f);
        summaryGridRoot.anchorMax = new Vector2(0.5f, 1f);
        summaryGridRoot.pivot     = new Vector2(0.5f, 1f);

        float targetGridW = Mathf.Round(m.halfW);
        float targetGridX = Mathf.Round(m.rightCenterX + _summaryVisualNudgeX);   // ← aquí

        var seq = DOTween.Sequence();
        seq.Join(summaryGridRoot.DOSizeDelta(new Vector2(targetGridW, summaryGridRoot.sizeDelta.y), resumenAnimDuration));
        seq.Join(summaryGridRoot.DOAnchorPosX(targetGridX, resumenAnimDuration));
        seq.OnUpdate(ReflowSummaryRows).OnComplete(ReflowSummaryRows);

        // --- INFO: mitad izquierda ---
        var rt = infoPanel;
        rt.gameObject.SetActive(true);
        rt.SetParent(summaryPageRoot, false);
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 0.5f);

        rt.sizeDelta = new Vector2(Mathf.Round(m.halfW), 0f);

        float targetInfoX = Mathf.Round(m.leftCenterX);

        rt.DOKill();
        // entra desde “su” izquierda (no desde fuera de la pantalla)
        rt.anchoredPosition = new Vector2(targetInfoX - m.halfW * 0.9f, 0f);
        rt.DOAnchorPosX(targetInfoX, resumenAnimDuration).SetEase(Ease.OutCubic);

    }

    // Anima: cerrar Info y devolver grid al centro, usando otra vez el ancho de SummaryPageRoot
    public void AnimateResumenBackToCenter()
    {
        if (!summaryGridRoot || !summaryPageRoot) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(summaryPageRoot);

        var m = GetResumenMetrics();

        summaryGridRoot.DOKill();
        var seq = DOTween.Sequence();
        seq.Join(summaryGridRoot.DOSizeDelta(new Vector2(Mathf.Round(m.centerW), summaryGridRoot.sizeDelta.y), resumenAnimDuration));
        // centrado real + nudge
        seq.Join(summaryGridRoot.DOAnchorPosX(_summaryVisualNudgeX, resumenAnimDuration));
        seq.OnUpdate(ReflowSummaryRows)
        .OnComplete(() => { CenterResumenLayoutNow(); });

        if (infoPanel)
        {
            var rt = infoPanel;
            rt.DOKill();
            rt.DOAnchorPosX(-Mathf.Round(m.centerW * 0.5f), resumenAnimDuration)
            .SetEase(Ease.InCubic)
            .OnComplete(() => rt.gameObject.SetActive(false));
        }

    }

    // --- MÉTRICAS CONSISTENTES PARA EL RESUMEN/INFO ---
    private struct ResumenMetrics
    {
        public float pageW, innerW, halfW, centerW, gap, leftCenterX, rightCenterX;
    }

    private ResumenMetrics GetResumenMetrics()
    {
        var m = new ResumenMetrics();
        m.pageW = summaryPageRoot ? summaryPageRoot.rect.width : 0f;
        // zona útil centrada (resta padding a ambos lados)
        m.centerW = Mathf.Max(10f, m.pageW - summaryPadding * 2f);
        m.gap = resumenGap;
        // dos mitades idénticas con un gap en medio
        m.halfW = Mathf.Max(10f, (m.centerW - m.gap) * 0.5f);
        m.innerW = m.halfW * 2f + m.gap;

        // centros absolutos (ancla/pivote 0.5, por lo que 0 = centro de la página)
        float offs = (m.gap * 0.5f + m.halfW * 0.5f); // = gap/2 + half/2
        m.leftCenterX = -offs;
        m.rightCenterX = offs;
        return m;
    }


}
