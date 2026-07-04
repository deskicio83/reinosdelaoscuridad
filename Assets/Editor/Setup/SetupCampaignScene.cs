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
using static EditorUIBuilder;

/// Editor Script — configura CampaignScene.unity con ESTADO 1: Scroll horizontal de 7 mundos.
/// Menu: Tools → Reino Oscuridad → 6. Setup CampaignScene
///
/// LAYOUT (1280×720 landscape):
///   Fondo             — pantalla completa
///   BtnVolver         — esquina inferior izquierda
///   TituloCampaign    — banda superior
///   ScrollMundos      — zona central, scroll horizontal clamped, 3 mundos visibles
///     ContentMundos   — HLG + ContentSizeFitter, padding 280 L/R para centrado
///       BtnMundo[0-6] — LayoutElement 220×260 cada uno
///   DotsIndicador     — HLG de 7 puntos bajo el scroll
///   PopupBloqueado    — overlay central oculto por defecto
public static class SetupCampaignScene
{
    private const string SCENE_PATH = "Assets/Scenes/CampaignScene.unity";

    private static readonly string[] MUNDO_NOMBRES =
    {
        "Cripta de los Quejosos",
        "Circo Agonizante",
        "Pantano del Arrepentimiento",
        "Fabrica de Pesadillas",
        "Salon de los Fracasados",
        "Cementerio de Modas",
        "Trono del Caos Eterno",
    };

    private static readonly Color[] MUNDO_TINTS =
    {
        new Color(0.8f, 0.3f, 0.3f), // 0 Cripta
        new Color(0.3f, 0.8f, 0.5f), // 1 Circo
        new Color(0.3f, 0.5f, 0.8f), // 2 Pantano
        new Color(0.8f, 0.6f, 0.2f), // 3 Fabrica
        new Color(0.6f, 0.3f, 0.8f), // 4 Salon
        new Color(0.8f, 0.4f, 0.6f), // 5 Cementerio
        new Color(0.9f, 0.2f, 0.2f), // 6 Trono
    };

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

        // Limpiar la escena por completo
        foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            UnityEngine.Object.DestroyImmediate(go);

        Build();

        // Añadir a Build Settings si no está
        AddToBuildSettings(SCENE_PATH);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[SetupCampaign] CampaignScene — ESTADO 1+2+3 configurada correctamente.");
    }

    // ── Construcción ───────────────────────────────────────────────────────

    private static void Build()
    {
        // ── Main Camera ───────────────────────────────────────────────────

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = Hex("#14101E");
        cam.transform.position = new Vector3(0f, 0f, -10f);

        // ── EventSystem ───────────────────────────────────────────────────

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        // ── Canvas ────────────────────────────────────────────────────────

        var canvasGO = new GameObject("CampaignCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var canvasTr = canvasGO.transform;

        // ── Fondo ─────────────────────────────────────────────────────────

        var fondoGO = Child(canvasTr, "Fondo");
        Anch(fondoGO, 0f, 0f, 1f, 1f);
        Img(fondoGO, Hex("#0A0A14"));

        // ── BtnVolver (esquina inf-izquierda) ─────────────────────────────

        var btnVolverGO = Child(canvasTr, "BtnVolver");
        Anch(btnVolverGO, 0.01f, 0.01f, 0.22f, 0.08f);
        Img(btnVolverGO, Hex("#1A1020"));
        var btnVolver = btnVolverGO.AddComponent<Button>();
        Txt(Child(btnVolverGO.transform, "Label"), "< Menu", 13f, Hex("#A855F7"), bold: true);

        // ── TituloCampaign (banda superior) ───────────────────────────────

        var tituloGO = Child(canvasTr, "TituloCampaign");
        Anch(tituloGO, 0.25f, 0.91f, 0.75f, 0.99f);
        Txt(tituloGO, "CAMPANA", 22f, Hex("#E9D5FF"), bold: true);

        // ── ScrollMundos ──────────────────────────────────────────────────
        // Viewport ocupa 0.00,0.20 → 1.00,0.90
        // El scroll es Clamped horizontal, mostrando ~3 mundos a la vez.
        // ContentSizeFitter con HLG y padding L/R=280 permite centrar mundos.

        var scrollGO = Child(canvasTr, "ScrollMundos");
        Anch(scrollGO, 0.00f, 0.20f, 1.00f, 0.90f);
        Img(scrollGO, Color.clear, 0f); // imagen transparente necesaria para capturar drags
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal           = true;
        scrollRect.vertical             = false;
        scrollRect.movementType         = ScrollRect.MovementType.Clamped;
        scrollRect.decelerationRate     = 0.15f;
        scrollRect.scrollSensitivity    = 50f;
        scrollRect.inertia              = true;

        var vpGO = Child(scrollGO.transform, "Viewport");
        Anch(vpGO, 0f, 0f, 1f, 1f);
        Img(vpGO, Color.clear, 0f);
        vpGO.AddComponent<RectMask2D>();

        var contentGO = Child(vpGO.transform, "ContentMundos");
        Img(contentGO, Color.clear, 0f);
        var contentRT = contentGO.GetComponent<RectTransform>();
        // anchor izquierda, estiramiento vertical
        contentRT.anchorMin = new Vector2(0f, 0f);
        contentRT.anchorMax = new Vector2(0f, 1f);
        contentRT.pivot     = new Vector2(0f, 0.5f);
        contentRT.sizeDelta = Vector2.zero;

        var hlg = contentGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 24f;
        hlg.padding                = new RectOffset(280, 280, 0, 0);
        hlg.childControlWidth      = true;   // respetar LayoutElement.preferredWidth
        hlg.childControlHeight     = true;   // respetar LayoutElement.preferredHeight
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        contentGO.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content  = contentRT;
        scrollRect.viewport = vpGO.GetComponent<RectTransform>();

        // ── 7 BtnMundo ────────────────────────────────────────────────────

        var btnsMundo        = new Button[7];
        var lockedOverlays   = new GameObject[7];
        var mundoSelBordes   = new Image[7];
        var mundoNombres     = new TMP_Text[7];
        var progresoTexts    = new TMP_Text[7];

        for (int i = 0; i < 7; i++)
        {
            var btnGO = Child(contentGO.transform, $"BtnMundo{i}");
            Img(btnGO, Hex("#1A1428"));
            var le = btnGO.AddComponent<LayoutElement>();
            le.preferredWidth  = 220f;
            le.preferredHeight = 260f;
            btnsMundo[i] = btnGO.AddComponent<Button>();

            // MundoPortrait (imagen de fondo del mundo, tintada)
            var portraitGO = Child(btnGO.transform, "MundoPortrait");
            Anch(portraitGO, 0.05f, 0.38f, 0.95f, 0.92f);
            var portraitImg = Img(portraitGO, MUNDO_TINTS[i], 0.75f);

            // Número del mundo (arriba izquierda del portrait)
            var numGO = Child(btnGO.transform, "MundoNumero");
            Anch(numGO, 0.05f, 0.82f, 0.40f, 0.96f);
            Txt(numGO, $"M{i + 1}", 14f, Hex("#FFFFFF"), bold: true);

            // Nombre del mundo (zona inferior)
            var nombreGO = Child(btnGO.transform, "MundoNombre");
            Anch(nombreGO, 0.03f, 0.20f, 0.97f, 0.38f);
            mundoNombres[i] = Txt(nombreGO, MUNDO_NOMBRES[i], 10f, Hex("#E9D5FF"),
                                   align: TextAlignmentOptions.Center);
            mundoNombres[i].textWrappingMode = TextWrappingModes.Normal;

            // Progreso (zona muy inferior)
            var progresoGO = Child(btnGO.transform, "ProgresoText");
            Anch(progresoGO, 0.05f, 0.06f, 0.95f, 0.21f);
            progresoTexts[i] = Txt(progresoGO, "0/7 fases", 9f, Hex("#9CA3AF"),
                                    align: TextAlignmentOptions.Center);

            // Borde seleccionado (dorado, alpha 0 por defecto)
            var bordeGO = Child(btnGO.transform, "MundoSeleccionadoBorde");
            Anch(bordeGO, 0f, 0f, 1f, 1f);
            mundoSelBordes[i] = Img(bordeGO, Hex("#F59E0B"), 0f);

            // LockedOverlay (visible solo si bloqueado; SetActive false = desbloqueado)
            var lockedGO = Child(btnGO.transform, "LockedOverlay");
            Anch(lockedGO, 0f, 0f, 1f, 1f);
            Img(lockedGO, Hex("#000000"), 0.75f);

            var lockIconGO = Child(lockedGO.transform, "LockIcon");
            Anch(lockIconGO, 0.35f, 0.52f, 0.65f, 0.80f);
            Img(lockIconGO, Hex("#A855F7"), 0.85f);

            var lockTxtGO = Child(lockedGO.transform, "LockText");
            Anch(lockTxtGO, 0.05f, 0.20f, 0.95f, 0.50f);
            Txt(lockTxtGO, "BLOQUEADO", 10f, Hex("#E9D5FF"),
                align: TextAlignmentOptions.Center, bold: true);

            lockedOverlays[i] = lockedGO;
            lockedGO.SetActive(false); // El controller lo activa si corresponde
        }

        // ── DotsIndicador (HLG de 7 puntos) ──────────────────────────────

        var dotsGO = Child(canvasTr, "DotsIndicador");
        Anch(dotsGO, 0.30f, 0.11f, 0.70f, 0.19f);
        Img(dotsGO, Color.clear, 0f);
        var dotsHLG = dotsGO.AddComponent<HorizontalLayoutGroup>();
        dotsHLG.spacing               = 8f;
        dotsHLG.childForceExpandWidth  = false;
        dotsHLG.childForceExpandHeight = false;
        dotsHLG.childAlignment         = TextAnchor.MiddleCenter;

        var dots = new Image[7];
        for (int i = 0; i < 7; i++)
        {
            var dotGO = Child(dotsGO.transform, $"Dot{i}");
            Img(dotGO, Hex("#A855F7"), 0.35f);
            var le = dotGO.AddComponent<LayoutElement>();
            le.preferredWidth  = 10f;
            le.preferredHeight = 10f;
            dots[i] = dotGO.GetComponent<Image>();
        }

        // ── PopupBloqueado ────────────────────────────────────────────────

        var popupGO = Child(canvasTr, "PopupBloqueado");
        Anch(popupGO, 0.25f, 0.35f, 0.75f, 0.65f);
        Img(popupGO, Hex("#0F0F20"), 0.97f);
        popupGO.SetActive(false);

        var popupBordeGO = Child(popupGO.transform, "Borde");
        Anch(popupBordeGO, 0f, 0f, 1f, 1f);
        Img(popupBordeGO, Hex("#A855F7"), 0.22f);

        var popupTxtGO = Child(popupGO.transform, "TxtInfo");
        Anch(popupTxtGO, 0.08f, 0.38f, 0.92f, 0.92f);
        Txt(popupTxtGO, "Completa el mundo anterior para desbloquear.", 12f,
            Hex("#E9D5FF"), align: TextAlignmentOptions.Center);

        var btnCerrarGO = Child(popupGO.transform, "BtnCerrar");
        Anch(btnCerrarGO, 0.30f, 0.06f, 0.70f, 0.32f);
        Img(btnCerrarGO, Hex("#1A1020"));
        btnCerrarGO.AddComponent<Button>();
        Txt(Child(btnCerrarGO.transform, "Label"), "Cerrar", 12f, Hex("#A855F7"), bold: true);

        // ════════════════════════════════════════════════════════════════
        // PANEL FASES — slide-in desde la derecha
        // anchors (0.52,0.09)→(1.00,0.90) = posición "abierto".
        // El controller lo desplaza fuera de pantalla vía anchoredPosition.x
        // ════════════════════════════════════════════════════════════════

        var panelFasesGO = Child(canvasTr, "PanelFases");
        Anch(panelFasesGO, 0.52f, 0.09f, 1.00f, 0.90f);
        Img(panelFasesGO, Hex("#0F0F1A"));
        var panelFasesRT = panelFasesGO.GetComponent<RectTransform>();

        // ── Header del PanelFases ─────────────────────────────────────

        var headerFasesGO = Child(panelFasesGO.transform, "HeaderFases");
        Anch(headerFasesGO, 0f, 0.92f, 1f, 1.00f);
        Img(headerFasesGO, Hex("#14101E"));

        var tituloMundoGO = Child(headerFasesGO.transform, "TituloMundoFases");
        Anch(tituloMundoGO, 0.05f, 0.15f, 0.78f, 0.85f);
        var tituloMundoFases = Txt(tituloMundoGO, "Mundo 1", 13f, Hex("#A855F7"), bold: true,
                                    align: TextAlignmentOptions.Left);

        var btnCerrarFasesGO = Child(headerFasesGO.transform, "BtnCerrarFases");
        Anch(btnCerrarFasesGO, 0.82f, 0.10f, 0.98f, 0.90f);
        Img(btnCerrarFasesGO, Hex("#1A1A2E"));
        var btnCerrarFases = btnCerrarFasesGO.AddComponent<Button>();
        Txt(Child(btnCerrarFasesGO.transform, "Label"), "X", 12f, Hex("#9CA3AF"), bold: true);

        // ── ScrollFases vertical ──────────────────────────────────────

        var scrollFasesGO = Child(panelFasesGO.transform, "ScrollFases");
        Anch(scrollFasesGO, 0f, 0f, 1f, 0.92f);
        Img(scrollFasesGO, Color.clear, 0f);
        var scrollFasesRect = scrollFasesGO.AddComponent<ScrollRect>();
        scrollFasesRect.horizontal        = false;
        scrollFasesRect.vertical          = true;
        scrollFasesRect.inertia           = true;
        scrollFasesRect.decelerationRate  = 0.15f;

        var vpFasesGO = Child(scrollFasesGO.transform, "ViewportFases");
        Anch(vpFasesGO, 0f, 0f, 1f, 1f);
        Img(vpFasesGO, Color.clear, 0f);
        vpFasesGO.AddComponent<RectMask2D>();

        var contentFasesGO = Child(vpFasesGO.transform, "ContentFases");
        Img(contentFasesGO, Color.clear, 0f);
        var contentFasesRT = contentFasesGO.GetComponent<RectTransform>();
        // Pivot arriba — el scroll empieza desde arriba
        contentFasesRT.anchorMin = new Vector2(0f, 1f);
        contentFasesRT.anchorMax = new Vector2(1f, 1f);
        contentFasesRT.pivot     = new Vector2(0.5f, 1f);
        contentFasesRT.sizeDelta = Vector2.zero;

        var vlg = contentFasesGO.AddComponent<VerticalLayoutGroup>();
        vlg.spacing               = 8f;
        vlg.padding               = new RectOffset(8, 8, 8, 8);
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment         = TextAnchor.UpperCenter;
        contentFasesGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollFasesRect.content  = contentFasesRT;
        scrollFasesRect.viewport = vpFasesGO.GetComponent<RectTransform>();

        // ── 7 Nodos de fase ───────────────────────────────────────────

        var btnsFase      = new Button[7];
        var nodoImages    = new Image[7];
        var dropTexts     = new TMP_Text[7];
        var estrellasTexts = new TMP_Text[7];
        var lockIconsFase = new GameObject[7];

        // Precargar sprite world_locked para lock icons
        var lockSprite = Resources.Load<Sprite>("Placeholders/world_locked");

        for (int i = 0; i < 7; i++)
        {
            var nodoGO = Child(contentFasesGO.transform, $"NodoFase_{i}");
            var nodoLE = nodoGO.AddComponent<LayoutElement>();
            nodoLE.preferredHeight = 72f;
            Color nodoColor = i == 0 ? Hex("#1E1535") : Hex("#0D0D0D");
            nodoImages[i] = Img(nodoGO, nodoColor);
            btnsFase[i] = nodoGO.AddComponent<Button>();
            if (i > 0) btnsFase[i].interactable = false;

            // NumFase
            var numGO = Child(nodoGO.transform, "NumFase");
            Anch(numGO, 0.04f, 0.55f, 0.42f, 0.95f);
            string labelFase = i < 6 ? $"Fase {i + 1}" : "BOSS";
            Txt(numGO, labelFase, 13f, Hex("#E9D5FF"), bold: true, align: TextAlignmentOptions.Left);

            // DropGarantizado
            var dropGO = Child(nodoGO.transform, "DropGarantizado");
            Anch(dropGO, 0.04f, 0.10f, 0.58f, 0.52f);
            dropTexts[i] = Txt(dropGO, "Drop: -", 9f, Hex("#9CA3AF"), align: TextAlignmentOptions.Left);

            // StaminaCost
            var costGO = Child(nodoGO.transform, "StaminaCost");
            Anch(costGO, 0.77f, 0.55f, 0.96f, 0.95f);
            Txt(costGO, "6", 11f, Hex("#FACC15"), bold: true, align: TextAlignmentOptions.Right);

            // EstrellasFase
            var estrellaGO = Child(nodoGO.transform, "EstrellasFase");
            Anch(estrellaGO, 0.62f, 0.10f, 0.96f, 0.50f);
            estrellasTexts[i] = Txt(estrellaGO, "- - -", 10f, Hex("#9CA3AF"));

            // LockIconFase
            var lockGO = Child(nodoGO.transform, "LockIconFase");
            Anch(lockGO, 0.44f, 0.20f, 0.60f, 0.80f);
            var lockImg = Img(lockGO, Hex("#374151"));
            if (lockSprite != null) lockImg.sprite = lockSprite;
            lockIconsFase[i] = lockGO;
            lockGO.SetActive(i > 0); // solo activo si bloqueado
        }

        // ════════════════════════════════════════════════════════════════
        // PANEL BATALLA — tapa toda la pantalla al hacer click en una fase
        // SetActive(false) por defecto. Se activa desde el controller.
        // Debe ser el último hijo del canvas para aparecer encima de todo.
        // ════════════════════════════════════════════════════════════════

        var panelBatallaGO = Child(canvasTr, "PanelBatalla");
        Anch(panelBatallaGO, 0f, 0f, 1f, 1f);
        Img(panelBatallaGO, Hex("#0F0F1A"));
        panelBatallaGO.SetActive(false);

        // ── Header Batalla ────────────────────────────────────────────

        var headerBatallaGO = Child(panelBatallaGO.transform, "HeaderBatalla");
        Anch(headerBatallaGO, 0f, 0.90f, 1f, 1.00f);
        Img(headerBatallaGO, Hex("#14101E"));

        var mundoFaseGO = Child(headerBatallaGO.transform, "MundoFaseText");
        Anch(mundoFaseGO, 0.02f, 0.15f, 0.55f, 0.85f);
        var mundoFaseText = Txt(mundoFaseGO, "Mundo 1 - Fase 1", 13f, Hex("#A855F7"),
                                bold: true, align: TextAlignmentOptions.Left);

        var dificultadGO = Child(headerBatallaGO.transform, "DificultadText");
        Anch(dificultadGO, 0.56f, 0.15f, 0.76f, 0.85f);
        var dificultadText = Txt(dificultadGO, "Normal", 11f, Hex("#9CA3AF"));

        var btnVolverBatallaGO = Child(headerBatallaGO.transform, "BtnVolverBatalla");
        Anch(btnVolverBatallaGO, 0.78f, 0.10f, 0.98f, 0.90f);
        Img(btnVolverBatallaGO, Hex("#1A1A2E"));
        var btnVolverBatalla = btnVolverBatallaGO.AddComponent<Button>();
        Txt(Child(btnVolverBatallaGO.transform, "Label"), "Volver", 11f, Hex("#9CA3AF"), bold: true);

        // ── Zona Central ──────────────────────────────────────────────

        var zonaCentralGO = Child(panelBatallaGO.transform, "ZonaCentral");
        Anch(zonaCentralGO, 0f, 0.15f, 1f, 0.90f);

        // ── Panel Equipo (izquierda 40%) ──────────────────────────────

        var panelEquipoGO = Child(zonaCentralGO.transform, "PanelEquipo");
        Anch(panelEquipoGO, 0.00f, 0f, 0.40f, 1f);
        Img(panelEquipoGO, Hex("#0D0D1A"));

        var tituloEquipoGO = Child(panelEquipoGO.transform, "TituloEquipo");
        Anch(tituloEquipoGO, 0.05f, 0.93f, 0.95f, 0.99f);
        var tituloEquipo = Txt(tituloEquipoGO, "Mi Equipo (0/4)", 10f, Hex("#9CA3AF"));

        // Grid 2x2 de slots
        var heroSlots     = new Button[4];
        var slotPortraits = new Image[4];
        var slotLabels    = new TMP_Text[4];

        // Cruz: slot 0 = derecha (primer slot), 1 = arriba, 2 = izquierda, 3 = abajo
        float[][] slotAnchors =
        {
            new[] { 0.52f, 0.47f, 0.98f, 0.68f },  // 0 = Derecha (primer slot)
            new[] { 0.20f, 0.70f, 0.80f, 0.91f },  // 1 = Arriba
            new[] { 0.02f, 0.47f, 0.48f, 0.68f },  // 2 = Izquierda
            new[] { 0.20f, 0.26f, 0.80f, 0.47f },  // 3 = Abajo
        };

        for (int i = 0; i < 4; i++)
        {
            var slotGO = Child(panelEquipoGO.transform, $"HeroSlot_{i}");
            Anch(slotGO, slotAnchors[i][0], slotAnchors[i][1], slotAnchors[i][2], slotAnchors[i][3]);
            Img(slotGO, Hex("#1A1A2E"));
            heroSlots[i] = slotGO.AddComponent<Button>();

            var portraitGO = Child(slotGO.transform, "SlotPortrait");
            Anch(portraitGO, 0.05f, 0.30f, 0.95f, 0.95f);
            slotPortraits[i] = Img(portraitGO, Hex("#2D2D4E"));

            var labelGO = Child(slotGO.transform, "SlotLabel");
            Anch(labelGO, 0.02f, 0.02f, 0.98f, 0.28f);
            slotLabels[i] = Txt(labelGO, "+", 8f, Hex("#6B7280"));
        }

        // Título conjuros
        var tituloConjurosGO = Child(panelEquipoGO.transform, "TituloConjuros");
        Anch(tituloConjurosGO, 0.05f, 0.18f, 0.95f, 0.24f);
        Txt(tituloConjurosGO, "Conjuros equipados", 9f, Hex("#9CA3AF"));

        // ScrollConjuros (horizontal)
        var scrollConjurosGO = Child(panelEquipoGO.transform, "ScrollConjuros");
        Anch(scrollConjurosGO, 0.02f, 0.06f, 0.98f, 0.18f);
        Img(scrollConjurosGO, Color.clear, 0f);
        var scrollConjuros = scrollConjurosGO.AddComponent<ScrollRect>();
        scrollConjuros.horizontal = true; scrollConjuros.vertical = false;

        var vpConjurosGO = Child(scrollConjurosGO.transform, "ViewportConjuros");
        Anch(vpConjurosGO, 0f, 0f, 1f, 1f);
        Img(vpConjurosGO, Color.clear, 0f);
        vpConjurosGO.AddComponent<RectMask2D>();

        var contentConjurosGO = Child(vpConjurosGO.transform, "ContentConjuros");
        Img(contentConjurosGO, Color.clear, 0f);
        var contentConjurosRT = contentConjurosGO.GetComponent<RectTransform>();
        contentConjurosRT.anchorMin = new Vector2(0f, 0f);
        contentConjurosRT.anchorMax = new Vector2(0f, 1f);
        contentConjurosRT.pivot     = new Vector2(0f, 0.5f);
        contentConjurosRT.sizeDelta = Vector2.zero;
        var hlgConj = contentConjurosGO.AddComponent<HorizontalLayoutGroup>();
        hlgConj.spacing               = 4f;
        hlgConj.childControlWidth      = true;
        hlgConj.childControlHeight     = true;
        hlgConj.childForceExpandWidth  = false;
        hlgConj.childForceExpandHeight = false;
        contentConjurosGO.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollConjuros.content  = contentConjurosRT;
        scrollConjuros.viewport = vpConjurosGO.GetComponent<RectTransform>();

        for (int i = 0; i < 3; i++)
        {
            var conjGO = Child(contentConjurosGO.transform, $"ConjuroSlot_{i}");
            var conjLE = conjGO.AddComponent<LayoutElement>();
            conjLE.preferredWidth  = 40f;
            conjLE.preferredHeight = 40f;
            Img(conjGO, Hex("#1A1020"));
            conjGO.AddComponent<Button>();
            Txt(Child(conjGO.transform, "Label"), "?", 16f, Hex("#6B7280"));
        }

        // ── Zona Elemental (centro 20%) ───────────────────────────────

        var zonaElementalGO = Child(zonaCentralGO.transform, "ZonaElemental");
        Anch(zonaElementalGO, 0.40f, 0.05f, 0.60f, 0.95f);

        var elementalLabelGO = Child(zonaElementalGO.transform, "ElementalLabel");
        Anch(elementalLabelGO, 0.05f, 0.88f, 0.95f, 0.97f);
        Txt(elementalLabelGO, "Ventajas elementales", 8f, Hex("#6B7280"));

        var elementalChartGO = Child(zonaElementalGO.transform, "ElementalChart");
        Anch(elementalChartGO, 0.05f, 0.15f, 0.95f, 0.88f);
        var elementalImg = Img(elementalChartGO, Hex("#1A1A2E"));
        {
            var chartTex = Resources.Load<Texture2D>("Placeholders/elemental_chart");
            if (chartTex != null)
                elementalImg.sprite = Sprite.Create(chartTex,
                    new Rect(0, 0, chartTex.width, chartTex.height), new Vector2(0.5f, 0.5f));
        }

        // ── Panel Enemigos (derecha 40%) ──────────────────────────────

        var panelEnemigosGO = Child(zonaCentralGO.transform, "PanelEnemigos");
        Anch(panelEnemigosGO, 0.60f, 0f, 1.00f, 1f);
        Img(panelEnemigosGO, Hex("#0D0D1A"));

        var tituloEnemigosGO = Child(panelEnemigosGO.transform, "TituloEnemigos");
        Anch(tituloEnemigosGO, 0.05f, 0.93f, 0.95f, 0.99f);
        Txt(tituloEnemigosGO, "Enemigos", 10f, Hex("#9CA3AF"));

        var listaEnemigosGO = Child(panelEnemigosGO.transform, "ListaEnemigos");
        Anch(listaEnemigosGO, 0.05f, 0.38f, 0.95f, 0.91f);
        var vlgEnemigos = listaEnemigosGO.AddComponent<VerticalLayoutGroup>();
        vlgEnemigos.spacing               = 6f;
        vlgEnemigos.childControlWidth      = true;
        vlgEnemigos.childControlHeight     = true;
        vlgEnemigos.childForceExpandWidth  = true;
        vlgEnemigos.childForceExpandHeight = false;

        var enemyPreviews = new GameObject[3];
        var enemyNombres  = new TMP_Text[3];
        var enemyNiveles  = new TMP_Text[3];

        for (int i = 0; i < 3; i++)
        {
            var epGO = Child(listaEnemigosGO.transform, $"EnemyPreview_{i}");
            var epLE = epGO.AddComponent<LayoutElement>();
            epLE.preferredHeight = 54f;
            Img(epGO, Hex("#1A0A0A"));
            epGO.SetActive(false);

            var eSpriteGO = Child(epGO.transform, "EnemySprite");
            Anch(eSpriteGO, 0.02f, 0.10f, 0.28f, 0.90f);
            Img(eSpriteGO, Hex("#2A1010"));

            var eNombreGO = Child(epGO.transform, "EnemyNombre");
            Anch(eNombreGO, 0.30f, 0.55f, 0.98f, 0.92f);
            enemyNombres[i] = Txt(eNombreGO, "Enemigo", 9f, Hex("#EF4444"),
                                   align: TextAlignmentOptions.Left);

            var eNivelGO = Child(epGO.transform, "EnemyNivel");
            Anch(eNivelGO, 0.30f, 0.08f, 0.98f, 0.52f);
            enemyNiveles[i] = Txt(eNivelGO, "Nv. ?", 8f, Hex("#9CA3AF"),
                                   align: TextAlignmentOptions.Left);

            enemyPreviews[i] = epGO;
        }

        // BtnBatallar
        var btnBatallarGO = Child(panelEnemigosGO.transform, "BtnBatallar");
        Anch(btnBatallarGO, 0.08f, 0.20f, 0.92f, 0.34f);
        Img(btnBatallarGO, Hex("#4C1D95"));
        var btnBatallar = btnBatallarGO.AddComponent<Button>();
        btnBatallar.interactable = false;
        Txt(Child(btnBatallarGO.transform, "Label"), "BATALLAR", 14f, Hex("#E9D5FF"), bold: true);

        // CosteEnergia
        var costeEnergiaGO = Child(panelEnemigosGO.transform, "CosteEnergia");
        Anch(costeEnergiaGO, 0.08f, 0.12f, 0.92f, 0.20f);

        var costeIconGO = Child(costeEnergiaGO.transform, "CosteIcon");
        Anch(costeIconGO, 0.00f, 0f, 0.18f, 1f);
        Img(costeIconGO, Hex("#FACC15"));

        var costeTextGO = Child(costeEnergiaGO.transform, "CosteText");
        Anch(costeTextGO, 0.20f, 0f, 1.00f, 1f);
        Txt(costeTextGO, "6 Energia por intento", 10f, Hex("#FACC15"), align: TextAlignmentOptions.Left);

        // SinEnergiaText
        var sinEnergiaGO = Child(panelEnemigosGO.transform, "SinEnergiaText");
        Anch(sinEnergiaGO, 0.05f, 0.06f, 0.95f, 0.12f);
        var sinEnergiaText = Txt(sinEnergiaGO, "Sin energia suficiente", 9f, Hex("#EF4444"));
        sinEnergiaGO.SetActive(false);

        // ── Franja Esbirros (15% inferior) ───────────────────────────

        var franjaGO = Child(panelBatallaGO.transform, "FranjaEsbirros");
        Anch(franjaGO, 0f, 0f, 1f, 0.15f);
        Img(franjaGO, Hex("#0A0A14"));

        var tituloEsbirrosGO = Child(franjaGO.transform, "TituloEsbirros");
        Anch(tituloEsbirrosGO, 0.01f, 0.72f, 0.18f, 0.97f);
        Txt(tituloEsbirrosGO, "Esbirros:", 9f, Hex("#9CA3AF"), align: TextAlignmentOptions.Left);

        var scrollEsbirrosGO = Child(franjaGO.transform, "ScrollEsbirros");
        Anch(scrollEsbirrosGO, 0.01f, 0.04f, 0.98f, 0.70f);
        Img(scrollEsbirrosGO, Color.clear, 0f);
        var scrollEsbirros = scrollEsbirrosGO.AddComponent<ScrollRect>();
        scrollEsbirros.horizontal        = true;
        scrollEsbirros.vertical          = false;
        scrollEsbirros.inertia           = true;
        scrollEsbirros.decelerationRate  = 0.15f;

        var vpEsbirrosGO = Child(scrollEsbirrosGO.transform, "ViewportEsbirros");
        Anch(vpEsbirrosGO, 0f, 0f, 1f, 1f);
        Img(vpEsbirrosGO, Color.clear, 0f);
        vpEsbirrosGO.AddComponent<RectMask2D>();

        var contentEsbirrosGO = Child(vpEsbirrosGO.transform, "ContentEsbirros");
        Img(contentEsbirrosGO, Color.clear, 0f);
        var contentEsbirrosRT = contentEsbirrosGO.GetComponent<RectTransform>();
        contentEsbirrosRT.anchorMin = new Vector2(0f, 0f);
        contentEsbirrosRT.anchorMax = new Vector2(0f, 1f);
        contentEsbirrosRT.pivot     = new Vector2(0f, 0.5f);
        contentEsbirrosRT.sizeDelta = Vector2.zero;
        var hlgEsbirros = contentEsbirrosGO.AddComponent<HorizontalLayoutGroup>();
        hlgEsbirros.spacing               = 6f;
        hlgEsbirros.padding               = new RectOffset(4, 4, 0, 0);
        hlgEsbirros.childControlWidth      = true;
        hlgEsbirros.childControlHeight     = true;
        hlgEsbirros.childForceExpandWidth  = false;
        hlgEsbirros.childForceExpandHeight = false;
        contentEsbirrosGO.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollEsbirros.content  = contentEsbirrosRT;
        scrollEsbirros.viewport = vpEsbirrosGO.GetComponent<RectTransform>();

        // ── CampaignSceneController + wiring ─────────────────────────

        var ctrlGO     = new GameObject("CampaignSceneController");
        var controller = ctrlGO.AddComponent<CampaignSceneController>();
        var so         = new SerializedObject(controller);

        // ESTADO 1 — ScrollMundos
        so.FindProperty("_scrollMundos") .objectReferenceValue = scrollRect;
        so.FindProperty("_contentMundos").objectReferenceValue = contentRT;
        so.FindProperty("_btnVolverMain").objectReferenceValue = btnVolver;
        so.FindProperty("_popupBloqueado").objectReferenceValue = popupGO;
        SetArray(so, "_btnsMundo",      btnsMundo);
        SetArray(so, "_lockedOverlays", lockedOverlays);
        SetArray(so, "_mundoSelBordes", mundoSelBordes);
        SetArray(so, "_mundoNombres",   mundoNombres);
        SetArray(so, "_progresoTexts",  progresoTexts);
        SetArray(so, "_dots",           dots);

        // ESTADO 2 — PanelFases
        so.FindProperty("_panelFasesRT")     .objectReferenceValue = panelFasesRT;
        so.FindProperty("_tituloMundoFases") .objectReferenceValue = tituloMundoFases;
        so.FindProperty("_btnCerrarFases")   .objectReferenceValue = btnCerrarFases;
        SetArray(so, "_btnsFase",       btnsFase);
        SetArray(so, "_nodoFaseImages", nodoImages);
        SetArray(so, "_dropTexts",      dropTexts);
        SetArray(so, "_estrellasTexts", estrellasTexts);
        SetArray(so, "_lockIconsFase",  lockIconsFase);

        // ESTADO 3 — PanelBatalla
        so.FindProperty("_panelBatalla")    .objectReferenceValue = panelBatallaGO;
        so.FindProperty("_mundoFaseText")   .objectReferenceValue = mundoFaseText;
        so.FindProperty("_dificultadText")  .objectReferenceValue = dificultadText;
        so.FindProperty("_btnVolverBatalla").objectReferenceValue = btnVolverBatalla;
        so.FindProperty("_tituloEquipo")    .objectReferenceValue = tituloEquipo;
        so.FindProperty("_btnBatallar")     .objectReferenceValue = btnBatallar;
        so.FindProperty("_sinEnergiaText")  .objectReferenceValue = sinEnergiaText;
        so.FindProperty("_contentEsbirros") .objectReferenceValue = contentEsbirrosRT;
        SetArray(so, "_heroSlots",      heroSlots);
        SetArray(so, "_slotPortraits",  slotPortraits);
        SetArray(so, "_slotLabels",     slotLabels);
        SetArray(so, "_enemyPreviews",  enemyPreviews);
        SetArray(so, "_enemyNombres",   enemyNombres);
        SetArray(so, "_enemyNiveles",   enemyNiveles);

        so.ApplyModifiedPropertiesWithoutUndo();

        // RewardPanel prefab
        var rewardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/RewardPanel.prefab");
        if (rewardPrefab != null)
        {
            so.FindProperty("_rewardPanelPrefab").objectReferenceValue = rewardPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        else
            Debug.LogWarning("[SetupCampaign] Assets/Prefabs/UI/RewardPanel.prefab no encontrado — cablear manualmente.");

        Debug.Log("[SetupCampaign] CampaignSceneController cableado — ESTADO 1+2+3.");
    }

    // ── Build Settings ─────────────────────────────────────────────────────

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
        Debug.Log($"[SetupCampaign] {scenePath} añadida a Build Settings.");
    }

    // ── Array wiring helper ────────────────────────────────────────────────

    /// Wire un array SerializedProperty con cualquier array de UnityEngine.Object.
    private static void SetArray<T>(SerializedObject so, string propName, T[] items)
        where T : UnityEngine.Object
    {
        var prop = so.FindProperty(propName);
        if (prop == null) { Debug.LogWarning($"[SetupCampaign] Prop '{propName}' no encontrada."); return; }
        prop.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

    /// Sobrecarga para GameObject[] (que no hereda de Component).
    private static void SetArray(SerializedObject so, string propName, GameObject[] items)
    {
        var prop = so.FindProperty(propName);
        if (prop == null) { Debug.LogWarning($"[SetupCampaign] Prop '{propName}' no encontrada."); return; }
        prop.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }

}
#endif
