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
        Debug.Log("[SetupCampaign] CampaignScene — ESTADO 1 configurada correctamente.");
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

        // ── CampaignSceneController + wiring ─────────────────────────────

        var ctrlGO     = new GameObject("CampaignSceneController");
        var controller = ctrlGO.AddComponent<CampaignSceneController>();
        var so         = new SerializedObject(controller);

        // ScrollRect + Content
        so.FindProperty("_scrollMundos") .objectReferenceValue = scrollRect;
        so.FindProperty("_contentMundos").objectReferenceValue = contentRT;

        // BtnVolver
        so.FindProperty("_btnVolverMain").objectReferenceValue = btnVolver;

        // PopupBloqueado
        so.FindProperty("_popupBloqueado").objectReferenceValue = popupGO;

        // Arrays de 7 elementos
        SetArray(so, "_btnsMundo",      btnsMundo);
        SetArray(so, "_lockedOverlays", lockedOverlays);
        SetArray(so, "_mundoSelBordes", mundoSelBordes);
        SetArray(so, "_mundoNombres",   mundoNombres);
        SetArray(so, "_progresoTexts",  progresoTexts);
        SetArray(so, "_dots",           dots);

        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[SetupCampaign] CampaignSceneController cableado correctamente.");
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

    // ── Helpers ────────────────────────────────────────────────────────────

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
        var img   = go.AddComponent<Image>();
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
