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
using ReinoOscuridad.UI.Campaign;

/// Editor Script — configura CampaignScene.unity con diseño de 3 paneles.
/// Menu: Tools → Reino Oscuridad → 6. Setup CampaignScene
///
/// LAYOUT (1280×720 landscape):
///   ScrollMundos  — full screen, estado inicial (7 mundos scrolleables horizontal)
///   PanelFases    — fixed 1280×720, slide-in desde derecha al seleccionar mundo
///                   Header · TabsDificultad · ScrollFases · ScrollEsbirros · ElementalChart · BtnReclamar
///   PanelBatalla  — full screen overlay, aparece al clicar una fase
///                   Titulo · Energia · 4 slots equipo · BtnEntrar · BtnCancelar
///   PopupBloqueado— overlay central, aparece al clicar fase/mundo bloqueado
public static class SetupCampaignScene
{
    private const string SCENE_PATH = "Assets/Scenes/CampaignScene.unity";

    [MenuItem("Tools/Reino Oscuridad/6. Setup CampaignScene")]
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

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[SetupCampaign] CampaignScene configurada — 3 paneles v2.");
    }

    // ── Construcción ───────────────────────────────────────────────────────

    private static void Build()
    {
        // ── Main Camera ───────────────────────────────────────────────────

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam   = camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = Hex("#0A0A14");
        cam.transform.position = new Vector3(0f, 0f, -10f);

        // ── EventSystem ───────────────────────────────────────────────────

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        // ── Canvas ────────────────────────────────────────────────────────

        var canvasGO = new GameObject("CampaignCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ════════════════════════════════════════════════════════════════
        // PANEL 1 — ScrollMundos (full screen, activo por defecto)
        // ════════════════════════════════════════════════════════════════

        var scrollMundosGO = Child(canvasGO.transform, "ScrollMundos");
        Anch(scrollMundosGO, 0f, 0f, 1f, 1f);
        Img(scrollMundosGO, Hex("#0A0A14"));
        var scrollMundosRT = scrollMundosGO.GetComponent<RectTransform>();

        // TopBar (y 0.88–1.00)
        var topBarGO = Child(scrollMundosGO.transform, "TopBar");
        Anch(topBarGO, 0f, 0.88f, 1f, 1.00f);
        Img(topBarGO, Hex("#0D0D1A"), 0.92f);

        var btnVolverMainGO = Child(topBarGO.transform, "BtnVolverMain");
        Anch(btnVolverMainGO, 0.00f, 0.08f, 0.13f, 0.92f);
        Img(btnVolverMainGO, Hex("#1A1020"));
        var btnVolverMain = btnVolverMainGO.AddComponent<Button>();
        Txt(Child(btnVolverMainGO.transform, "Label"), "< Menu", 11f, Hex("#A855F7"), bold: true);

        var tituloGO = Child(topBarGO.transform, "TxtTitulo");
        Anch(tituloGO, 0.14f, 0.05f, 0.90f, 0.95f);
        Txt(tituloGO, "CAMPANA", 22f, Hex("#E9D5FF"), bold: true);

        // ScrollView horizontal mundos (y 0.14–0.86)
        Transform contenedorMundosBtns;
        MakeHScrollView(scrollMundosGO.transform, "ScrollViewMundos",
            0.01f, 0.14f, 0.99f, 0.86f, out contenedorMundosBtns, spacing: 12f, padH: 20);

        // IndicadorDots HLG (y 0.07–0.13)
        var dotsGO = Child(scrollMundosGO.transform, "IndicadorDots");
        Anch(dotsGO, 0.20f, 0.07f, 0.80f, 0.13f);
        Img(dotsGO, Color.clear, 0f);
        var dotsHLG = dotsGO.AddComponent<HorizontalLayoutGroup>();
        dotsHLG.spacing            = 8f;
        dotsHLG.childForceExpandWidth  = false;
        dotsHLG.childForceExpandHeight = false;
        dotsHLG.childAlignment         = TextAnchor.MiddleCenter;

        // ════════════════════════════════════════════════════════════════
        // PANEL 2 — PanelFases (1280×720 fijo, empieza off-screen derecha)
        // Se usa anchoredPosition para el slide — NO anchors stretch.
        // ════════════════════════════════════════════════════════════════

        var panelFasesGO = Child(canvasGO.transform, "PanelFases");
        var panelFasesRT = panelFasesGO.GetComponent<RectTransform>();
        panelFasesRT.anchorMin        = Vector2.zero;
        panelFasesRT.anchorMax        = Vector2.zero;
        panelFasesRT.pivot            = Vector2.zero;
        panelFasesRT.sizeDelta        = new Vector2(1280f, 720f);
        panelFasesRT.anchoredPosition = new Vector2(1280f, 0f); // off-screen right
        Img(panelFasesGO, Hex("#0A0A14"));
        panelFasesGO.SetActive(false);

        // Header PanelFases (y 0.88–1.00)
        var headerFasesGO = Child(panelFasesGO.transform, "HeaderFases");
        Anch(headerFasesGO, 0f, 0.88f, 1f, 1.00f);
        Img(headerFasesGO, Hex("#0D0D1A"), 0.92f);

        var btnVolverFasesGO = Child(headerFasesGO.transform, "BtnVolverFases");
        Anch(btnVolverFasesGO, 0.00f, 0.08f, 0.15f, 0.92f);
        Img(btnVolverFasesGO, Hex("#1A1020"));
        var btnVolverFases = btnVolverFasesGO.AddComponent<Button>();
        Txt(Child(btnVolverFasesGO.transform, "Label"), "< Mundos", 11f, Hex("#A855F7"), bold: true);

        var txtMundoNombreGO = Child(headerFasesGO.transform, "TxtMundoNombre");
        Anch(txtMundoNombreGO, 0.16f, 0.05f, 0.92f, 0.95f);
        var txtMundoNombre = Txt(txtMundoNombreGO, "Mundo 1", 20f, Hex("#E9D5FF"), bold: true);

        // TabsDificultad (y 0.80–0.88) — relleno en runtime por BuildDifTabs()
        var contenedorDifGO = Child(panelFasesGO.transform, "ContenedorDificultad");
        Anch(contenedorDifGO, 0f, 0.80f, 1f, 0.88f);
        Img(contenedorDifGO, Hex("#0D0D1E"), 0.80f);
        var difHLG = contenedorDifGO.AddComponent<HorizontalLayoutGroup>();
        difHLG.spacing            = 10f;
        difHLG.padding            = new RectOffset(12, 12, 4, 4);
        difHLG.childForceExpandWidth  = false;
        difHLG.childForceExpandHeight = true;
        difHLG.childAlignment         = TextAnchor.MiddleCenter;

        // ScrollFases horizontal (y 0.50–0.80) — nodos rellenos en runtime
        Transform contenedorFases;
        MakeHScrollView(panelFasesGO.transform, "ScrollFases",
            0f, 0.50f, 1f, 0.80f, out contenedorFases, spacing: 12f, padH: 16);

        // ScrollEsbirros horizontal (y 0.28–0.50) — enemigos rellenos en runtime
        Transform contenedorEsbirros;
        MakeHScrollView(panelFasesGO.transform, "ScrollEsbirros",
            0f, 0.28f, 1f, 0.50f, out contenedorEsbirros, spacing: 8f, padH: 10);

        // Zona Elemental (y 0.12–0.28)
        var zonaElemGO = Child(panelFasesGO.transform, "ZonaElemental");
        Anch(zonaElemGO, 0f, 0.12f, 1f, 0.28f);
        Img(zonaElemGO, Hex("#0D0D1A"), 0.85f);

        var lblElemGO = Child(zonaElemGO.transform, "LblElemental");
        Anch(lblElemGO, 0.02f, 0.60f, 0.30f, 0.98f);
        Txt(lblElemGO, "Tabla Elemental", 9f, Hex("#9CA3AF"));

        var imgElemGO = Child(zonaElemGO.transform, "ImgElementalChart");
        Anch(imgElemGO, 0.31f, 0.05f, 0.98f, 0.95f);
        var imgElementalChart = Img(imgElemGO, Hex("#1A1A2A"), 0.90f);

        // BtnReclamarRecompensa (y 0.02–0.12)
        var btnReclamarGO = Child(panelFasesGO.transform, "BtnReclamarRecompensa");
        Anch(btnReclamarGO, 0.30f, 0.02f, 0.70f, 0.12f);
        Img(btnReclamarGO, Hex("#4C1D95"));
        var btnReclamar = btnReclamarGO.AddComponent<Button>();
        btnReclamar.interactable = false; // habilitado en runtime cuando mundo completo
        Txt(Child(btnReclamarGO.transform, "Label"), "Reclamar Recompensa", 12f, Hex("#E9D5FF"), bold: true);

        // ════════════════════════════════════════════════════════════════
        // PANEL 3 — PanelBatalla (full screen overlay, oculto)
        // ════════════════════════════════════════════════════════════════

        var panelBatallaGO = Child(canvasGO.transform, "PanelBatalla");
        Anch(panelBatallaGO, 0f, 0f, 1f, 1f);
        Img(panelBatallaGO, Hex("#080810"), 0.97f);
        panelBatallaGO.SetActive(false);
        var panelBatallaRT = panelBatallaGO.GetComponent<RectTransform>();

        var txtBatallaTituloGO = Child(panelBatallaGO.transform, "TxtBatallaTitulo");
        Anch(txtBatallaTituloGO, 0.05f, 0.84f, 0.95f, 0.98f);
        var txtBatallaTitulo = Txt(txtBatallaTituloGO, "Mundo 1 - Fase 1 - NORMAL", 20f, Hex("#E9D5FF"), bold: true);

        var txtBatallaEnergiaGO = Child(panelBatallaGO.transform, "TxtBatallaEnergia");
        Anch(txtBatallaEnergiaGO, 0.05f, 0.76f, 0.95f, 0.84f);
        var txtBatallaEnergia = Txt(txtBatallaEnergiaGO, "Coste: 6 energia", 15f, Hex("#60A5FA"));

        var lblEquipoGO = Child(panelBatallaGO.transform, "LblEquipo");
        Anch(lblEquipoGO, 0.05f, 0.70f, 0.95f, 0.77f);
        Txt(lblEquipoGO, "Equipo seleccionado (max 4):", 13f, Hex("#9CA3AF"), align: TextAlignmentOptions.Left);

        // ContenedorEquipoSelec HLG (y 0.56–0.70)
        var contenedorEquipoGO = Child(panelBatallaGO.transform, "ContenedorEquipoSelec");
        Anch(contenedorEquipoGO, 0.05f, 0.56f, 0.95f, 0.70f);
        Img(contenedorEquipoGO, Color.clear, 0f);
        var equipoHLG = contenedorEquipoGO.AddComponent<HorizontalLayoutGroup>();
        equipoHLG.spacing            = 10f;
        equipoHLG.childForceExpandWidth  = false;
        equipoHLG.childForceExpandHeight = true;
        equipoHLG.childAlignment         = TextAnchor.MiddleCenter;

        // Info texto equipo
        var txtEquipoInfoGO = Child(panelBatallaGO.transform, "TxtEquipoInfo");
        Anch(txtEquipoInfoGO, 0.05f, 0.40f, 0.95f, 0.55f);
        Txt(txtEquipoInfoGO, "Selecciona heroes desde HeroScene o usa el equipo automatico.",
            11f, Hex("#6B7280"), align: TextAlignmentOptions.Center);

        // BtnCancelarBatalla (y 0.08–0.22, izquierda)
        var btnCancelarGO = Child(panelBatallaGO.transform, "BtnCancelarBatalla");
        Anch(btnCancelarGO, 0.08f, 0.08f, 0.44f, 0.22f);
        Img(btnCancelarGO, Hex("#2A0000"));
        var btnCancelarBatalla = btnCancelarGO.AddComponent<Button>();
        Txt(Child(btnCancelarGO.transform, "Label"), "Cancelar", 15f, Hex("#F87171"), bold: true);

        // BtnEntrar (y 0.08–0.22, derecha)
        var btnEntrarGO = Child(panelBatallaGO.transform, "BtnEntrar");
        Anch(btnEntrarGO, 0.56f, 0.08f, 0.92f, 0.22f);
        Img(btnEntrarGO, Hex("#4C1D95"));
        var btnEntrar = btnEntrarGO.AddComponent<Button>();
        Txt(Child(btnEntrarGO.transform, "Label"), "ENTRAR", 16f, Hex("#E9D5FF"), bold: true);

        // ════════════════════════════════════════════════════════════════
        // POPUP — PopupBloqueado (overlay central, oculto)
        // ════════════════════════════════════════════════════════════════

        var popupGO = Child(canvasGO.transform, "PopupBloqueado");
        Anch(popupGO, 0.22f, 0.35f, 0.78f, 0.65f);
        Img(popupGO, Hex("#0F0F20"), 0.97f);
        popupGO.SetActive(false);

        var popupBordeGO = Child(popupGO.transform, "Borde");
        Anch(popupBordeGO, 0f, 0f, 1f, 1f);
        Img(popupBordeGO, Hex("#A855F7"), 0.22f);

        var txtBloqGO = Child(popupGO.transform, "TxtBloqueadoInfo");
        Anch(txtBloqGO, 0.08f, 0.38f, 0.92f, 0.92f);
        var txtBloqueadoInfo = Txt(txtBloqGO, "Contenido bloqueado.", 13f, Hex("#E9D5FF"),
                                   align: TextAlignmentOptions.Center);

        var btnCerrarPopupGO = Child(popupGO.transform, "BtnCerrarPopup");
        Anch(btnCerrarPopupGO, 0.30f, 0.06f, 0.70f, 0.32f);
        Img(btnCerrarPopupGO, Hex("#1A1020"));
        var btnCerrarPopup = btnCerrarPopupGO.AddComponent<Button>();
        Txt(Child(btnCerrarPopupGO.transform, "Label"), "Cerrar", 13f, Hex("#A855F7"), bold: true);

        // ── CampaignSceneController + wiring ─────────────────────────────

        var ctrlGO     = new GameObject("CampaignSceneController");
        var controller = ctrlGO.AddComponent<CampaignSceneController>();
        var so         = new SerializedObject(controller);

        // ScrollMundos
        so.FindProperty("_scrollMundos")        .objectReferenceValue = scrollMundosRT;
        so.FindProperty("_contenedorMundosBtns").objectReferenceValue = contenedorMundosBtns;
        so.FindProperty("_indicadorDots")       .objectReferenceValue = dotsGO.transform;

        // PanelFases
        so.FindProperty("_panelFases")            .objectReferenceValue = panelFasesRT;
        so.FindProperty("_txtMundoNombre")         .objectReferenceValue = txtMundoNombre;
        so.FindProperty("_btnVolverFases")         .objectReferenceValue = btnVolverFases;
        so.FindProperty("_contenedorDificultad")   .objectReferenceValue = contenedorDifGO.transform;
        so.FindProperty("_contenedorFases")        .objectReferenceValue = contenedorFases;
        so.FindProperty("_contenedorEsbirros")     .objectReferenceValue = contenedorEsbirros;
        so.FindProperty("_imgElementalChart")      .objectReferenceValue = imgElementalChart;
        so.FindProperty("_btnReclamarRecompensa")  .objectReferenceValue = btnReclamar;

        // PanelBatalla
        so.FindProperty("_panelBatalla")            .objectReferenceValue = panelBatallaRT;
        so.FindProperty("_txtBatallaTitulo")         .objectReferenceValue = txtBatallaTitulo;
        so.FindProperty("_txtBatallaEnergia")        .objectReferenceValue = txtBatallaEnergia;
        so.FindProperty("_contenedorEquipoSelec")    .objectReferenceValue = contenedorEquipoGO.transform;
        so.FindProperty("_btnEntrar")                .objectReferenceValue = btnEntrar;
        so.FindProperty("_btnCancelarBatalla")       .objectReferenceValue = btnCancelarBatalla;

        // PopupBloqueado
        so.FindProperty("_popupBloqueado")  .objectReferenceValue = popupGO;
        so.FindProperty("_txtBloqueadoInfo").objectReferenceValue = txtBloqueadoInfo;
        so.FindProperty("_btnCerrarPopup")  .objectReferenceValue = btnCerrarPopup;

        // Navegación
        so.FindProperty("_btnVolverMain").objectReferenceValue = btnVolverMain;

        // Prefabs de overlays (opcionales — deben existir antes)
        var battlePrepPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/BattlePrepPanel.prefab");
        var rewardPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/RewardPanel.prefab");
        if (battlePrepPrefab != null) so.FindProperty("_battlePrepPrefab").objectReferenceValue = battlePrepPrefab;
        if (rewardPrefab     != null) so.FindProperty("_rewardPrefab")    .objectReferenceValue = rewardPrefab;

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// Crea un ScrollRect horizontal. Devuelve el content Transform via out.
    private static void MakeHScrollView(Transform parent, string name,
        float xMin, float yMin, float xMax, float yMax,
        out Transform contentOut, float spacing = 10f, int padH = 10)
    {
        var scrollGO = Child(parent, name);
        Anch(scrollGO, xMin, yMin, xMax, yMax);
        // Image necesaria para que ScrollRect reciba input de arrastre
        Img(scrollGO, Hex("#000000"), 0f);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();

        var vpGO = Child(scrollGO.transform, "Viewport");
        Anch(vpGO, 0f, 0f, 1f, 1f);
        Img(vpGO, Color.clear, 0f);
        vpGO.AddComponent<RectMask2D>();

        var contentGO = Child(vpGO.transform, "Content");
        Img(contentGO, Color.clear, 0f);
        var contentRT = contentGO.GetComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = new Vector2(0f, 1f);
        contentRT.pivot     = new Vector2(0f, 0.5f);
        contentRT.sizeDelta = Vector2.zero;

        var hlg = contentGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing            = spacing;
        hlg.padding            = new RectOffset(padH, padH, 6, 6);
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        contentGO.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content    = contentRT;
        scrollRect.viewport   = vpGO.GetComponent<RectTransform>();
        scrollRect.horizontal = true;
        scrollRect.vertical   = false;
        scrollRect.scrollSensitivity = 20f;

        contentOut = contentGO.transform;
    }

    private static GameObject Child(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void Anch(GameObject go, float x0, float y0, float x1, float y1)
    {
        var rt       = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Image Img(GameObject go, Color col, float alpha = 1f)
    {
        var img = go.AddComponent<Image>();
        img.color = new Color(col.r, col.g, col.b, alpha);
        return img;
    }

    private static TMP_Text Txt(GameObject go, string text, float size, Color col,
                                 TextAlignmentOptions align = TextAlignmentOptions.Center,
                                 bool bold = false)
    {
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = size;
        t.color     = col;
        t.alignment = align;
        if (bold) t.fontStyle = FontStyles.Bold;
        return t;
    }

    private static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out var c);
        return c;
    }
}
#endif
