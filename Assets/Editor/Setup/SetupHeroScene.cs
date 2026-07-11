#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ReinoOscuridad.UI.HeroScene;
using static EditorUIBuilder;

/// Editor Script — configura HeroScene.unity.
/// Menu: Tools -> Reino Oscuridad -> 9. Setup HeroScene
///
/// LAYOUT (1280x720):
///   HeaderBar    (x 0.00-1.00, y 0.90-1.00): BtnVolver + Titulo + Capacidad + Filtros
///   ZonaRoster   (x 0.02-0.32, y 0.05-0.88): ScrollRect vertical con PooledGridView (3 columnas)
///   DetailWrapper(x 0.34-0.98, y 0.05-0.88), oculto hasta seleccionar un heroe:
///     ZonaMedio  (local 0.00-0.42): retrato rectangular + nivel/estrellas + elemento + favorito/bloquear
///     ZonaNav    (local 0.44-1.00): 5 tabs (Info/Habilidades/Equipo/Maestrias/Miscelaneo) + contenido
public static class SetupHeroScene
{
    private const string SCENE_PATH = "Assets/Scenes/HeroScene.unity";

    [MenuItem("Tools/Reino Oscuridad/9. Setup HeroScene")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (File.Exists(SCENE_PATH))
            EditorSceneManager.OpenScene(SCENE_PATH);
        else
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(s, SCENE_PATH);
        }

        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            Object.DestroyImmediate(go);

        Build();
        AddToBuildSettings(SCENE_PATH);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[SetupHeroScene] HeroScene configurada correctamente.");
    }

    private static void Build()
    {
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Hex("#14101E");
        cam.transform.position = new Vector3(0f, 0f, -10f);

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        var canvasGO = new GameObject("HeroCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var canvasTr = canvasGO.transform;

        var fondo = Child(canvasTr, "Fondo");
        Anch(fondo, 0f, 0f, 1f, 1f);
        Img(fondo, Hex("#14101E"));

        // ── HeaderBar ────────────────────────────────────────────────────────

        var header = Child(canvasTr, "HeaderBar");
        Anch(header, 0f, 0.90f, 1f, 1f);
        Img(header, Hex("#0D0D1A"), 0.95f);

        var btnVolverGO = Child(header.transform, "BtnVolver");
        Anch(btnVolverGO, 0.005f, 0.15f, 0.085f, 0.85f);
        var btnVolverImg = Img(btnVolverGO, Hex("#1A1020"));
        var btnVolver = btnVolverGO.AddComponent<Button>();
        btnVolver.targetGraphic = btnVolverImg;
        Txt(Child(btnVolverGO.transform, "Label"), "< Volver", 11f, Hex("#A855F7"), bold: true);

        var tituloGO = Child(header.transform, "Titulo");
        Anch(tituloGO, 0.36f, 0.10f, 0.62f, 0.90f);
        Txt(tituloGO, "ESBIRRATORIO", 12f, Color.white, TextAlignmentOptions.Left, bold: true);

        // Capacidad + Filtros comparten el mismo ancho x que ZonaRoster (0.02-0.32),
        // justo debajo de esta banda, para que visualmente "pertenezcan" al grid.
        var capacidadGO = Child(header.transform, "CapacidadTexto");
        Anch(capacidadGO, 0.095f, 0.10f, 0.27f, 0.90f);
        var capacidadTxt = Txt(capacidadGO, "Esbirros: 0/20", 10f, Hex("#AAAAAA"), TextAlignmentOptions.Left);

        var btnFiltrosGO = Child(header.transform, "BtnFiltros");
        Anch(btnFiltrosGO, 0.281f, 0.15f, 0.32f, 0.85f);
        var btnFiltrosImg = Img(btnFiltrosGO, Hex("#1A1020"));
        var btnFiltros = btnFiltrosGO.AddComponent<Button>();
        btnFiltros.targetGraphic = btnFiltrosImg;

        var iconFiltrosGO = Child(btnFiltrosGO.transform, "Icon");
        Anch(iconFiltrosGO, 0.15f, 0.15f, 0.85f, 0.85f);
        var iconFiltros = Img(iconFiltrosGO, Color.white);
        iconFiltros.preserveAspect = true;

        // ── ZonaRoster ───────────────────────────────────────────────────────

        var zonaRoster = Child(canvasTr, "ZonaRoster");
        Anch(zonaRoster, 0.02f, 0.05f, 0.32f, 0.88f);
        Img(zonaRoster, Hex("#0D0D1A"), 0.6f);

        var scrollGO = Child(zonaRoster.transform, "ScrollRoster");
        Anch(scrollGO, 0.01f, 0.01f, 0.99f, 0.99f);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        var viewportGO = Child(scrollGO.transform, "Viewport");
        Anch(viewportGO, 0f, 0f, 1f, 1f);
        Img(viewportGO, Color.white, 0.01f);
        viewportGO.AddComponent<RectMask2D>();
        var viewportRT = viewportGO.GetComponent<RectTransform>();

        var contentGO = Child(viewportGO.transform, "ContentRoster");
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0f, 800f);
        contentRT.anchoredPosition = Vector2.zero;

        scrollRect.viewport = viewportRT;
        scrollRect.content = contentRT;

        var cardTemplate = BuildCardTemplate(canvasTr);

        // ── PanelFiltros (overlay sobre ZonaRoster, lista vertical única) ─────

        var panelFiltros = Child(canvasTr, "PanelFiltros");
        Anch(panelFiltros, 0.02f, 0.15f, 0.32f, 0.87f);
        Img(panelFiltros, Hex("#1A1020"), 0.97f);
        var panelFiltrosCanvasGroup = panelFiltros.AddComponent<CanvasGroup>();
        panelFiltros.SetActive(false);

        // Lista con scroll — evita que se desborde del panel al añadir más filas
        // de filtro en el futuro (estrellas, facción, etc.).
        var filtrosContentRT = BuildScrollContent(panelFiltros.transform, out _);
        var filtrosListTr = filtrosContentRT.transform;
        var filtrosVLG = filtrosContentRT.GetComponent<VerticalLayoutGroup>();
        filtrosVLG.padding = new RectOffset(4, 4, 4, 4);

        var btnOrdenarGO = Child(filtrosListTr, "BtnOrdenar");
        btnOrdenarGO.AddComponent<LayoutElement>().preferredHeight = 24f;
        var btnOrdenarImg = Img(btnOrdenarGO, Hex("#0D0D1A"));
        var btnOrdenar = btnOrdenarGO.AddComponent<Button>();
        btnOrdenar.targetGraphic = btnOrdenarImg;
        var btnOrdenarLabel = Txt(Child(btnOrdenarGO.transform, "Label"), "Orden: nivel", 9f, Color.white,
            TextAlignmentOptions.Left, bold: true);
        Anch(btnOrdenarLabel.rectTransform.gameObject, 0.06f, 0f, 0.98f, 1f);

        var btnOrdenDireccionGO = Child(filtrosListTr, "BtnOrdenDireccion");
        btnOrdenDireccionGO.AddComponent<LayoutElement>().preferredHeight = 24f;
        var btnOrdenDireccionImg = Img(btnOrdenDireccionGO, Hex("#0D0D1A"));
        var btnOrdenDireccion = btnOrdenDireccionGO.AddComponent<Button>();
        btnOrdenDireccion.targetGraphic = btnOrdenDireccionImg;
        var btnOrdenDireccionLabel = Txt(Child(btnOrdenDireccionGO.transform, "Label"), "Direccion: Descendente", 9f,
            Color.white, TextAlignmentOptions.Left, bold: true);
        Anch(btnOrdenDireccionLabel.rectTransform.gameObject, 0.06f, 0f, 0.98f, 1f);

        var btnSoloFavGO = Child(filtrosListTr, "BtnSoloFavoritos");
        btnSoloFavGO.AddComponent<LayoutElement>().preferredHeight = 24f;
        var btnSoloFavImg = Img(btnSoloFavGO, Hex("#0D0D1A"));
        var btnSoloFav = btnSoloFavGO.AddComponent<Button>();
        btnSoloFav.targetGraphic = btnSoloFavImg;
        var btnSoloFavLabel = Txt(Child(btnSoloFavGO.transform, "Label"), "Favoritos: NO", 9f, Color.white,
            TextAlignmentOptions.Left, bold: true);
        Anch(btnSoloFavLabel.rectTransform.gameObject, 0.06f, 0f, 0.98f, 1f);

        var elementoHeaderGO = Child(filtrosListTr, "ElementoHeader");
        elementoHeaderGO.AddComponent<LayoutElement>().preferredHeight = 18f;
        Txt(elementoHeaderGO, "Elemento", 9f, Hex("#888888"), TextAlignmentOptions.Left, bold: true);

        // Los botones de elemento se instancian dentro de la misma lista con scroll
        // (HeroSceneController.BuildFiltroElementoButtons los añade como filas más).
        var filtroElementoTemplate = BuildFiltroElementoTemplate(canvasTr);

        var estrellasHeaderGO = Child(filtrosListTr, "EstrellasHeader");
        estrellasHeaderGO.AddComponent<LayoutElement>().preferredHeight = 18f;
        Txt(estrellasHeaderGO, "Estrellas", 9f, Hex("#888888"), TextAlignmentOptions.Left, bold: true);

        // Los botones de estrellas se instancian dentro de la misma lista con scroll
        // (HeroSceneController.BuildFiltroEstrellasButtons los añade como filas más).
        var filtroEstrellasTemplate = BuildFiltroElementoTemplate(canvasTr);
        filtroEstrellasTemplate.name = "FiltroEstrellasTemplate";

        // ── Popup comprar huecos / confirmacion generica (overlay centrado) ──

        var popupComprar = Child(canvasTr, "PopupComprarHuecos");
        Anch(popupComprar, 0.30f, 0.35f, 0.70f, 0.65f);
        Img(popupComprar, Hex("#1A1020"), 0.98f);
        popupComprar.SetActive(false);

        var popupTextoGO = Child(popupComprar.transform, "Texto");
        Anch(popupTextoGO, 0.05f, 0.40f, 0.95f, 0.92f);
        var popupTexto = Txt(popupTextoGO, "Comprar 10 huecos", 13f, Color.white);

        var btnConfirmarGO = Child(popupComprar.transform, "BtnConfirmar");
        Anch(btnConfirmarGO, 0.05f, 0.08f, 0.48f, 0.32f);
        Img(btnConfirmarGO, Hex("#166534"));
        var btnConfirmar = btnConfirmarGO.AddComponent<Button>();
        Txt(Child(btnConfirmarGO.transform, "Label"), "Confirmar", 11f, Color.white, bold: true);

        var btnCancelarGO = Child(popupComprar.transform, "BtnCancelar");
        Anch(btnCancelarGO, 0.52f, 0.08f, 0.95f, 0.32f);
        Img(btnCancelarGO, Hex("#0D0D1A"));
        var btnCancelar = btnCancelarGO.AddComponent<Button>();
        Txt(Child(btnCancelarGO.transform, "Label"), "Cancelar", 11f, Color.white, bold: true);

        // ── DetailWrapper (ZonaMedio + ZonaNav) ───────────────────────────────

        var detailWrapper = Child(canvasTr, "DetailWrapper");
        Anch(detailWrapper, 0.34f, 0.05f, 0.98f, 0.88f);
        detailWrapper.SetActive(false);

        var (zonaMedioResult, zonaNavResult) = BuildZonasDetalle(detailWrapper.transform);

        popupComprar.transform.SetAsLastSibling();

        // ── Wiring ───────────────────────────────────────────────────────────

        var controller = canvasGO.AddComponent<HeroSceneController>();
        var so = new SerializedObject(controller);
        so.FindProperty("_scrollRect").objectReferenceValue = scrollRect;
        so.FindProperty("_content").objectReferenceValue = contentRT;
        so.FindProperty("_viewport").objectReferenceValue = viewportRT;
        so.FindProperty("_cardTemplate").objectReferenceValue = cardTemplate;

        so.FindProperty("_capacidadTexto").objectReferenceValue = capacidadTxt;

        so.FindProperty("_btnFiltros").objectReferenceValue = btnFiltros;
        so.FindProperty("_iconFiltros").objectReferenceValue = iconFiltros;
        so.FindProperty("_panelFiltros").objectReferenceValue = panelFiltros;
        so.FindProperty("_panelFiltrosCanvasGroup").objectReferenceValue = panelFiltrosCanvasGroup;
        so.FindProperty("_filtrosElementoContainer").objectReferenceValue = filtrosContentRT;
        so.FindProperty("_filtroElementoBtnTemplate").objectReferenceValue = filtroElementoTemplate;
        so.FindProperty("_filtroEstrellasBtnTemplate").objectReferenceValue = filtroEstrellasTemplate;
        so.FindProperty("_btnSoloFavoritos").objectReferenceValue = btnSoloFav;
        so.FindProperty("_btnSoloFavoritosLabel").objectReferenceValue = btnSoloFavLabel;
        so.FindProperty("_btnOrdenar").objectReferenceValue = btnOrdenar;
        so.FindProperty("_btnOrdenarLabel").objectReferenceValue = btnOrdenarLabel;
        so.FindProperty("_btnOrdenDireccion").objectReferenceValue = btnOrdenDireccion;
        so.FindProperty("_btnOrdenDireccionLabel").objectReferenceValue = btnOrdenDireccionLabel;

        so.FindProperty("_popupComprarHuecos").objectReferenceValue = popupComprar;
        so.FindProperty("_popupComprarTexto").objectReferenceValue = popupTexto;
        so.FindProperty("_btnConfirmarCompra").objectReferenceValue = btnConfirmar;
        so.FindProperty("_btnCancelarCompra").objectReferenceValue = btnCancelar;

        so.FindProperty("_detailPanel").objectReferenceValue = detailWrapper;
        so.FindProperty("_detailPortrait").objectReferenceValue = zonaMedioResult.Portrait;
        so.FindProperty("_detailNombre").objectReferenceValue = zonaMedioResult.Nombre;
        so.FindProperty("_detailNivel").objectReferenceValue = zonaMedioResult.Nivel;
        so.FindProperty("_estrellasContainer").objectReferenceValue = zonaMedioResult.EstrellasContainer;
        so.FindProperty("_iconElemento").objectReferenceValue = zonaMedioResult.IconElemento;
        so.FindProperty("_detailClaseElemento").objectReferenceValue = zonaMedioResult.ClaseElemento;
        so.FindProperty("_btnFavorito").objectReferenceValue = zonaMedioResult.BtnFavorito;
        so.FindProperty("_btnFavoritoLabel").objectReferenceValue = zonaMedioResult.BtnFavoritoLabel;
        so.FindProperty("_btnBloquear").objectReferenceValue = zonaMedioResult.BtnBloquear;
        so.FindProperty("_btnBloquearLabel").objectReferenceValue = zonaMedioResult.BtnBloquearLabel;

        so.FindProperty("_btnTabInfo").objectReferenceValue = zonaNavResult.BtnTabInfo;
        so.FindProperty("_btnTabHabilidades").objectReferenceValue = zonaNavResult.BtnTabHabilidades;
        so.FindProperty("_btnTabEquipo").objectReferenceValue = zonaNavResult.BtnTabEquipo;
        so.FindProperty("_btnTabMaestrias").objectReferenceValue = zonaNavResult.BtnTabMaestrias;
        so.FindProperty("_btnTabMiscelaneo").objectReferenceValue = zonaNavResult.BtnTabMiscelaneo;
        so.FindProperty("_iconTabInfo").objectReferenceValue = zonaNavResult.IconTabInfo;
        so.FindProperty("_iconTabHabilidades").objectReferenceValue = zonaNavResult.IconTabHabilidades;
        so.FindProperty("_iconTabEquipo").objectReferenceValue = zonaNavResult.IconTabEquipo;
        so.FindProperty("_panelInfo").objectReferenceValue = zonaNavResult.PanelInfo;
        so.FindProperty("_panelHabilidades").objectReferenceValue = zonaNavResult.PanelHabilidades;
        so.FindProperty("_panelEquipo").objectReferenceValue = zonaNavResult.PanelEquipo;
        so.FindProperty("_panelMaestrias").objectReferenceValue = zonaNavResult.PanelMaestrias;
        so.FindProperty("_panelMiscelaneo").objectReferenceValue = zonaNavResult.PanelMiscelaneo;
        so.FindProperty("_habilidadesContent").objectReferenceValue = zonaNavResult.HabilidadesContent;
        so.FindProperty("_equipoSlotsContent").objectReferenceValue = zonaNavResult.EquipoSlotsContent;
        so.FindProperty("_btnEliminar").objectReferenceValue = zonaNavResult.BtnEliminar;
        so.FindProperty("_detailStatsLeft").objectReferenceValue = zonaNavResult.DetailStatsLeft;
        so.FindProperty("_detailStatsRight").objectReferenceValue = zonaNavResult.DetailStatsRight;
        so.FindProperty("_detailLore").objectReferenceValue = zonaNavResult.DetailLore;

        so.FindProperty("_btnVolver").objectReferenceValue = btnVolver;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private struct ZonaMedioResult
    {
        public Image Portrait;
        public TMP_Text Nombre;
        public TMP_Text Nivel;
        public RectTransform EstrellasContainer;
        public Image IconElemento;
        public TMP_Text ClaseElemento;
        public Button BtnFavorito;
        public TMP_Text BtnFavoritoLabel;
        public Button BtnBloquear;
        public TMP_Text BtnBloquearLabel;
    }

    private struct ZonaNavResult
    {
        public Button BtnTabInfo, BtnTabHabilidades, BtnTabEquipo, BtnTabMaestrias, BtnTabMiscelaneo;
        public Image IconTabInfo, IconTabHabilidades, IconTabEquipo;
        public GameObject PanelInfo, PanelHabilidades, PanelEquipo, PanelMaestrias, PanelMiscelaneo;
        public RectTransform HabilidadesContent;
        public RectTransform EquipoSlotsContent;
        public Button BtnEliminar;
        public TMP_Text DetailStatsLeft;
        public TMP_Text DetailStatsRight;
        public TMP_Text DetailLore;
    }

    private static (ZonaMedioResult, ZonaNavResult) BuildZonasDetalle(Transform wrapperTr)
    {
        // ── ZonaMedio (local 0.00-0.42 del wrapper) ───────────────────────────

        var zonaMedio = Child(wrapperTr, "ZonaMedio");
        Anch(zonaMedio, 0.00f, 0.00f, 0.40f, 1.00f);
        Img(zonaMedio, Hex("#0D0D1A"), 0.6f);

        var portraitGO = Child(zonaMedio.transform, "Portrait");
        Anch(portraitGO, 0.10f, 0.32f, 0.90f, 0.97f);
        var portraitImg = Img(portraitGO, Color.white);

        var nombreGO = Child(zonaMedio.transform, "Nombre");
        Anch(nombreGO, 0.05f, 0.24f, 0.95f, 0.31f);
        var nombreTxt = Txt(nombreGO, "—", 13f, Color.white, bold: true);

        var nivelGO = Child(zonaMedio.transform, "Nivel");
        Anch(nivelGO, 0.05f, 0.17f, 0.45f, 0.23f);
        var nivelTxt = Txt(nivelGO, "—", 11f, Hex("#AAAAAA"), TextAlignmentOptions.Left);

        var estrellasContainerGO = Child(zonaMedio.transform, "Estrellas");
        Anch(estrellasContainerGO, 0.48f, 0.18f, 0.95f, 0.23f);
        var estrellasHLG = estrellasContainerGO.AddComponent<HorizontalLayoutGroup>();
        estrellasHLG.childControlWidth = true;
        estrellasHLG.childControlHeight = true;
        estrellasHLG.spacing = 2f;
        estrellasHLG.childAlignment = TextAnchor.MiddleLeft;

        var iconElementoGO = Child(zonaMedio.transform, "IconElemento");
        Anch(iconElementoGO, 0.05f, 0.10f, 0.16f, 0.16f);
        var iconElemento = Img(iconElementoGO, Color.white);
        iconElementoGO.SetActive(false);

        var claseGO = Child(zonaMedio.transform, "ClaseElemento");
        Anch(claseGO, 0.20f, 0.10f, 0.95f, 0.16f);
        var claseTxt = Txt(claseGO, "—", 10f, Hex("#AAAAAA"), TextAlignmentOptions.Left);

        var btnFavGO = Child(zonaMedio.transform, "BtnFavorito");
        Anch(btnFavGO, 0.05f, 0.01f, 0.48f, 0.08f);
        var btnFavImg = Img(btnFavGO, Hex("#1A1020"));
        var btnFav = btnFavGO.AddComponent<Button>();
        btnFav.targetGraphic = btnFavImg;
        var btnFavLabel = Txt(Child(btnFavGO.transform, "Label"), "Favorito", 9f, Hex("#A855F7"), bold: true);

        var btnBloquearGO = Child(zonaMedio.transform, "BtnBloquear");
        Anch(btnBloquearGO, 0.52f, 0.01f, 0.95f, 0.08f);
        var btnBloquearImg = Img(btnBloquearGO, Hex("#1A1020"));
        var btnBloquear = btnBloquearGO.AddComponent<Button>();
        btnBloquear.targetGraphic = btnBloquearImg;
        var btnBloquearLabel = Txt(Child(btnBloquearGO.transform, "Label"), "Bloquear", 9f, Hex("#A855F7"), bold: true);

        var zonaMedioResult = new ZonaMedioResult
        {
            Portrait = portraitImg,
            Nombre = nombreTxt,
            Nivel = nivelTxt,
            EstrellasContainer = estrellasContainerGO.GetComponent<RectTransform>(),
            IconElemento = iconElemento,
            ClaseElemento = claseTxt,
            BtnFavorito = btnFav,
            BtnFavoritoLabel = btnFavLabel,
            BtnBloquear = btnBloquear,
            BtnBloquearLabel = btnBloquearLabel,
        };

        // ── ZonaNav (local 0.44-1.00 del wrapper) ─────────────────────────────

        var zonaNav = Child(wrapperTr, "ZonaNav");
        Anch(zonaNav, 0.44f, 0.00f, 1.00f, 1.00f);
        Img(zonaNav, Hex("#0D0D1A"), 0.6f);

        var tabBar = Child(zonaNav.transform, "TabBar");
        Anch(tabBar, 0.02f, 0.90f, 0.98f, 0.99f);

        var (btnTabInfo, iconTabInfo) = BuildTabButton(tabBar.transform, "BtnInfo", 0.00f, 0.19f, "Info");
        var (btnTabHab, iconTabHab) = BuildTabButton(tabBar.transform, "BtnHabilidades", 0.20f, 0.39f, "Habil.");
        var (btnTabEq, iconTabEq) = BuildTabButton(tabBar.transform, "BtnEquipo", 0.40f, 0.59f, "Equipo");
        var (btnTabMae, _) = BuildTabButton(tabBar.transform, "BtnMaestrias", 0.60f, 0.79f, "Maestr.");
        var (btnTabMisc, _) = BuildTabButton(tabBar.transform, "BtnMiscelaneo", 0.80f, 0.99f, "Misc.");

        // Panel Info
        var panelInfo = Child(zonaNav.transform, "PanelInfo");
        Anch(panelInfo, 0.05f, 0.03f, 0.98f, 0.88f);

        var statsGridGO = Child(panelInfo.transform, "StatsGrid");
        Anch(statsGridGO, 0f, 0.60f, 1f, 1f);

        var statsLeftGO = Child(statsGridGO.transform, "StatsLeft");
        Anch(statsLeftGO, 0f, 0f, 0.48f, 1f);
        var statsLeftTxt = Txt(statsLeftGO, "—", 12f, Color.white, TextAlignmentOptions.TopLeft);

        var statsRightGO = Child(statsGridGO.transform, "StatsRight");
        Anch(statsRightGO, 0.52f, 0f, 1f, 1f);
        var statsRightTxt = Txt(statsRightGO, "—", 12f, Color.white, TextAlignmentOptions.TopLeft);

        var loreHeaderGO = Child(panelInfo.transform, "LoreHeader");
        Anch(loreHeaderGO, 0f, 0.53f, 1f, 0.58f);
        Txt(loreHeaderGO, "Historia", 10f, Hex("#888888"), TextAlignmentOptions.Left, bold: true);

        var loreGO = Child(panelInfo.transform, "Lore");
        Anch(loreGO, 0f, 0f, 1f, 0.52f);
        var loreTxt = Txt(loreGO, "—", 10f, Hex("#CCCCCC"), TextAlignmentOptions.TopLeft);
        loreTxt.textWrappingMode = TextWrappingModes.Normal;
        loreTxt.overflowMode = TextOverflowModes.Ellipsis;

        // Panel Habilidades
        var panelHab = Child(zonaNav.transform, "PanelHabilidades");
        Anch(panelHab, 0.05f, 0.03f, 0.98f, 0.88f);
        panelHab.SetActive(false);
        var habContentRT = BuildScrollContent(panelHab.transform, out var habScroll);

        // Panel Equipo
        var panelEq = Child(zonaNav.transform, "PanelEquipo");
        Anch(panelEq, 0.05f, 0.03f, 0.98f, 0.88f);
        panelEq.SetActive(false);
        var equipoContentRT = BuildScrollContent(panelEq.transform, out var eqScroll);

        // Panel Maestrias (stub)
        var panelMae = Child(zonaNav.transform, "PanelMaestrias");
        Anch(panelMae, 0.05f, 0.03f, 0.98f, 0.88f);
        panelMae.SetActive(false);
        Txt(panelMae, "Maestrías — próximamente", 12f, Hex("#AAAAAA"), TextAlignmentOptions.TopLeft);

        // Panel Miscelaneo
        var panelMisc = Child(zonaNav.transform, "PanelMiscelaneo");
        Anch(panelMisc, 0.05f, 0.03f, 0.98f, 0.88f);
        panelMisc.SetActive(false);

        var btnEliminarGO = Child(panelMisc.transform, "BtnEliminar");
        Anch(btnEliminarGO, 0.0f, 0.85f, 0.6f, 0.97f);
        var btnEliminarImg = Img(btnEliminarGO, Hex("#3F1A1A"));
        var btnEliminar = btnEliminarGO.AddComponent<Button>();
        btnEliminar.targetGraphic = btnEliminarImg;
        Txt(Child(btnEliminarGO.transform, "Label"), "Eliminar esbirro", 10f, Hex("#EF4444"), bold: true);

        var zonaNavResult = new ZonaNavResult
        {
            BtnTabInfo = btnTabInfo,
            BtnTabHabilidades = btnTabHab,
            BtnTabEquipo = btnTabEq,
            BtnTabMaestrias = btnTabMae,
            BtnTabMiscelaneo = btnTabMisc,
            IconTabInfo = iconTabInfo,
            IconTabHabilidades = iconTabHab,
            IconTabEquipo = iconTabEq,
            PanelInfo = panelInfo,
            PanelHabilidades = panelHab,
            PanelEquipo = panelEq,
            PanelMaestrias = panelMae,
            PanelMiscelaneo = panelMisc,
            HabilidadesContent = habContentRT,
            EquipoSlotsContent = equipoContentRT,
            BtnEliminar = btnEliminar,
            DetailStatsLeft = statsLeftTxt,
            DetailStatsRight = statsRightTxt,
            DetailLore = loreTxt,
        };

        return (zonaMedioResult, zonaNavResult);
    }

    private static (Button, Image) BuildTabButton(Transform parent, string name, float x0, float x1, string label)
    {
        var go = Child(parent, name);
        Anch(go, x0, 0f, x1, 1f);
        var img = Img(go, Hex("#1A1020"));
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var iconGO = Child(go.transform, "Icon");
        Anch(iconGO, 0.06f, 0.15f, 0.34f, 0.85f);
        var icon = Img(iconGO, Color.white);

        var lbl = Txt(Child(go.transform, "Label"), label, 11f, Color.white, TextAlignmentOptions.Left, bold: true);
        Anch(lbl.rectTransform.gameObject, 0.36f, 0.1f, 0.98f, 0.9f);

        return (btn, icon);
    }

    private static RectTransform BuildScrollContent(Transform parent, out ScrollRect scroll)
    {
        var scrollGO = Child(parent, "Scroll");
        Anch(scrollGO, 0f, 0f, 1f, 1f);
        scroll = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        var viewportGO = Child(scrollGO.transform, "Viewport");
        Anch(viewportGO, 0f, 0f, 1f, 1f);
        Img(viewportGO, Color.white, 0.01f);
        viewportGO.AddComponent<RectMask2D>();

        var contentGO = Child(viewportGO.transform, "Content");
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4f;
        contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewportGO.GetComponent<RectTransform>();
        scroll.content = contentRT;

        return contentRT;
    }

    private static GameObject BuildFiltroElementoTemplate(Transform canvasTr)
    {
        var go = Child(canvasTr, "FiltroElementoTemplate");
        var layout = go.AddComponent<LayoutElement>();
        layout.preferredHeight = 24f;
        layout.flexibleWidth = 1f;
        var btnImg = Img(go, Hex("#0D0D1A"));
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = btnImg;

        var labelGO = Child(go.transform, "Label");
        Anch(labelGO, 0.06f, 0f, 0.98f, 1f);
        Txt(labelGO, "elemento", 9f, Color.white, TextAlignmentOptions.Left, bold: true);

        go.SetActive(false);
        return go;
    }

    private static GameObject BuildCardTemplate(Transform canvasTr)
    {
        var cardGO = Child(canvasTr, "CardTemplate");
        var rt = cardGO.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(140f, 170f);
        var cardImg = Img(cardGO, Hex("#1A1020"));
        var btn = cardGO.AddComponent<Button>();
        btn.targetGraphic = cardImg;
        var colors = btn.colors;
        colors.pressedColor = Hex("#A855F7");
        colors.highlightedColor = Hex("#2A1A3A");
        btn.colors = colors;

        var portraitGO = Child(cardGO.transform, "Portrait");
        Anch(portraitGO, 0.08f, 0.30f, 0.92f, 0.95f);
        var portraitImg = Img(portraitGO, Color.white);

        var nombreGO = Child(cardGO.transform, "Nombre");
        Anch(nombreGO, 0.05f, 0.15f, 0.95f, 0.28f);
        var nombreTxt = Txt(nombreGO, "Heroe", 10f, Color.white, bold: true);

        var nivelGO = Child(cardGO.transform, "Nivel");
        Anch(nivelGO, 0.05f, 0.02f, 0.95f, 0.14f);
        var nivelTxt = Txt(nivelGO, "Nv. 1", 9f, Hex("#AAAAAA"));

        var favGO = Child(cardGO.transform, "FavoritoIcon");
        Anch(favGO, 0.75f, 0.85f, 0.98f, 0.98f);
        var favImg = Img(favGO, Color.white);
        favImg.preserveAspect = true;
        favGO.SetActive(false);

        var lockGO = Child(cardGO.transform, "LockIcon");
        Anch(lockGO, 0.02f, 0.85f, 0.25f, 0.98f);
        var lockImg = Img(lockGO, Color.white);
        lockImg.preserveAspect = true;
        lockGO.SetActive(false);

        var estrellaBadgeGO = Child(cardGO.transform, "EstrellaBadge");
        Anch(estrellaBadgeGO, 0.08f, 0.235f, 0.55f, 0.295f);
        Img(estrellaBadgeGO, Color.black, 0.45f);

        var estrellaIconGO = Child(estrellaBadgeGO.transform, "Icon");
        Anch(estrellaIconGO, 0.06f, 0.15f, 0.36f, 0.85f);
        var estrellaIcon = Img(estrellaIconGO, Color.white);
        estrellaIcon.preserveAspect = true;

        var estrellaTxtGO = Child(estrellaBadgeGO.transform, "Texto");
        Anch(estrellaTxtGO, 0.4f, 0f, 0.98f, 1f);
        var estrellaTxt = Txt(estrellaTxtGO, "x0", 8f, Color.white, TextAlignmentOptions.Left, bold: true);

        var borderGO = Child(cardGO.transform, "BorderIcon");
        Anch(borderGO, 0f, 0f, 1f, 1f);
        var borderImg = Img(borderGO, Color.white);
        borderImg.raycastTarget = false;

        var view = cardGO.AddComponent<HeroCardView>();
        var so = new SerializedObject(view);
        so.FindProperty("_portrait").objectReferenceValue = portraitImg;
        so.FindProperty("_nombre").objectReferenceValue = nombreTxt;
        so.FindProperty("_nivel").objectReferenceValue = nivelTxt;
        so.FindProperty("_favoritoIcon").objectReferenceValue = favGO;
        so.FindProperty("_lockIcon").objectReferenceValue = lockGO;
        so.FindProperty("_estrellaIcon").objectReferenceValue = estrellaIcon;
        so.FindProperty("_estrellaTexto").objectReferenceValue = estrellaTxt;
        so.FindProperty("_borderIcon").objectReferenceValue = borderImg;
        so.FindProperty("_button").objectReferenceValue = btn;
        so.ApplyModifiedPropertiesWithoutUndo();

        cardGO.SetActive(false);
        return cardGO;
    }

    private static void AddToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
            if (s.path == scenePath) return;

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log($"[SetupHeroScene] {scenePath} añadida a Build Settings.");
    }
}
#endif
