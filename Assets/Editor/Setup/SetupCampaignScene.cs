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

/// Editor Script — configura CampaignScene.unity con mapa de campaña.
/// Menú: Tools → Reino Oscuridad → 6. Setup CampaignScene
///
/// LAYOUT (1280×720 landscape):
///   TopBar mundial (y 0.88–1.00): 7 botones de mundo
///   TabsDificultad (y 0.78–0.88): 3 botones Normal / Dificil / Heroica
///   ScrollFases    (y 0.05–0.77): 7 nodos de fase (generados en runtime)
///   BtnVolver      (y 0.88–1.00, x 0.00–0.08): volver al menú principal
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
        Debug.Log("[SetupCampaign] CampaignScene configurada.");
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
        cam.backgroundColor  = new Color(0.04f, 0.04f, 0.08f);

        // ── EventSystem ───────────────────────────────────────────────────

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        // ── Canvas principal ──────────────────────────────────────────────

        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler            = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode    = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Fondo ─────────────────────────────────────────────────────────

        var bgGO  = MakePanel(canvasGO.transform, "Background",
            Vector2.zero, Vector2.one, new Color(0.06f, 0.04f, 0.10f));

        // ── Título ────────────────────────────────────────────────────────

        var titleGO = new GameObject("TxtTitulo");
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
        titleTxt.text      = "CAMPAÑA";
        titleTxt.fontSize  = 32f;
        titleTxt.color     = new Color(0.9f, 0.7f, 0.2f);
        titleTxt.alignment = TextAlignmentOptions.Center;
        SetAnchors(titleGO, new Vector2(0.08f, 0.90f), new Vector2(0.92f, 1.00f));

        // ── Botón Volver ──────────────────────────────────────────────────

        var btnVolverGO = MakeButton(canvasGO.transform, "BtnVolver", "← Volver",
            new Vector2(0f, 0.90f), new Vector2(0.08f, 1.00f));

        // ── Contenedor de mundos (TopBar) ─────────────────────────────────

        var mundosGO = MakePanel(canvasGO.transform, "ContenedorMundos",
            new Vector2(0.08f, 0.88f), new Vector2(1.00f, 1.00f),
            new Color(0.10f, 0.08f, 0.16f));

        var mundosHLG = mundosGO.AddComponent<HorizontalLayoutGroup>();
        mundosHLG.spacing            = 6f;
        mundosHLG.padding            = new RectOffset(8, 8, 4, 4);
        mundosHLG.childForceExpandWidth  = true;
        mundosHLG.childForceExpandHeight = true;

        // ── Contenedor de dificultad ──────────────────────────────────────

        var difGO = MakePanel(canvasGO.transform, "ContenedorDificultad",
            new Vector2(0f, 0.78f), new Vector2(1.00f, 0.88f),
            new Color(0.08f, 0.06f, 0.14f));

        var difHLG = difGO.AddComponent<HorizontalLayoutGroup>();
        difHLG.spacing            = 10f;
        difHLG.padding            = new RectOffset(12, 12, 4, 4);
        difHLG.childForceExpandWidth  = false;
        difHLG.childForceExpandHeight = true;
        difHLG.childAlignment         = TextAnchor.MiddleCenter;

        // ── Scroll de fases ───────────────────────────────────────────────
        // IMPORTANTE: añadir Image ANTES de SetAnchors para que exista el RectTransform.

        var scrollGO = new GameObject("ScrollFases");
        scrollGO.transform.SetParent(canvasGO.transform, false);
        scrollGO.AddComponent<Image>().color = new Color(0.05f, 0.04f, 0.09f); // crea RectTransform
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        SetAnchors(scrollGO, new Vector2(0f, 0.05f), new Vector2(1f, 0.78f));

        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        viewportGO.AddComponent<Image>().color = Color.clear; // crea RectTransform, invisible
        viewportGO.AddComponent<RectMask2D>();
        SetAnchors(viewportGO, Vector2.zero, Vector2.one);

        var contenedorFasesGO = new GameObject("ContenedorFases");
        contenedorFasesGO.transform.SetParent(viewportGO.transform, false);
        contenedorFasesGO.AddComponent<Image>().color = Color.clear; // crea RectTransform, invisible
        var fasesHLG = contenedorFasesGO.AddComponent<HorizontalLayoutGroup>();
        fasesHLG.spacing            = 12f;
        fasesHLG.padding            = new RectOffset(20, 20, 10, 10);
        fasesHLG.childForceExpandWidth  = false;
        fasesHLG.childForceExpandHeight = true;
        fasesHLG.childAlignment         = TextAnchor.MiddleLeft;
        contenedorFasesGO.AddComponent<ContentSizeFitter>().horizontalFit =
            ContentSizeFitter.FitMode.PreferredSize;
        SetAnchors(contenedorFasesGO, Vector2.zero, new Vector2(1f, 1f));

        scrollRect.content    = contenedorFasesGO.GetComponent<RectTransform>();
        scrollRect.viewport   = viewportGO.GetComponent<RectTransform>();
        scrollRect.horizontal = true;
        scrollRect.vertical   = false;

        // ── Barra de progreso del mundo (placeholder) ─────────────────────

        MakePanel(canvasGO.transform, "BarraProgreso",
            new Vector2(0f, 0.01f), new Vector2(1f, 0.05f),
            new Color(0.12f, 0.10f, 0.18f));

        // ── Panel de confirmación de fase ────────────────────────────────

        var confirmGO = MakePanel(canvasGO.transform, "PanelConfirmacion",
            new Vector2(0.25f, 0.20f), new Vector2(0.75f, 0.80f),
            new Color(0.08f, 0.06f, 0.14f, 0.97f));

        // Título de la fase
        var txtFaseGO = new GameObject("TxtFaseNombre");
        txtFaseGO.transform.SetParent(confirmGO.transform, false);
        var txtFase = txtFaseGO.AddComponent<TextMeshProUGUI>();
        txtFase.text      = "Fase";
        txtFase.fontSize  = 22f;
        txtFase.color     = new Color(0.9f, 0.7f, 0.2f);
        txtFase.alignment = TextAlignmentOptions.Center;
        SetAnchors(txtFaseGO, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.95f));

        // Info del equipo
        var txtEquipoGO = new GameObject("TxtEquipo");
        txtEquipoGO.transform.SetParent(confirmGO.transform, false);
        var txtEquipo = txtEquipoGO.AddComponent<TextMeshProUGUI>();
        txtEquipo.text     = "Equipo:";
        txtEquipo.fontSize = 16f;
        txtEquipo.color    = Color.white;
        SetAnchors(txtEquipoGO, new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.70f));

        // Coste de energía
        var txtCostGO = new GameObject("TxtEnergyCost");
        txtCostGO.transform.SetParent(confirmGO.transform, false);
        var txtCost = txtCostGO.AddComponent<TextMeshProUGUI>();
        txtCost.text      = "Energia: -";
        txtCost.fontSize  = 16f;
        txtCost.color     = new Color(0.4f, 0.8f, 1f);
        txtCost.alignment = TextAlignmentOptions.Center;
        SetAnchors(txtCostGO, new Vector2(0.05f, 0.26f), new Vector2(0.95f, 0.36f));

        // Botón confirmar
        var btnConfGO = MakeButton(confirmGO.transform, "BtnConfirmarBatalla", ">> BATALLAR",
            new Vector2(0.05f, 0.06f), new Vector2(0.50f, 0.24f));
        btnConfGO.GetComponent<Image>().color = new Color(0.5f, 0.1f, 0.1f);

        // Botón cancelar
        var btnCancelGO = MakeButton(confirmGO.transform, "BtnCancelar", "Cancelar",
            new Vector2(0.52f, 0.06f), new Vector2(0.95f, 0.24f));

        confirmGO.SetActive(false);

        // ── CampaignSceneController ───────────────────────────────────────

        var controllerGO = new GameObject("CampaignSceneController");
        var controller   = controllerGO.AddComponent<CampaignSceneController>();

        // Asignar refs via SerializedObject
        var so = new SerializedObject(controller);
        so.FindProperty("_contenedorMundos")         .objectReferenceValue = mundosGO.transform;
        so.FindProperty("_contenedorFases")          .objectReferenceValue = contenedorFasesGO.transform;
        so.FindProperty("_contenedorDificultad")     .objectReferenceValue = difGO.transform;
        so.FindProperty("_btnVolver")                .objectReferenceValue = btnVolverGO.GetComponent<Button>();
        so.FindProperty("_panelConfirmacion")        .objectReferenceValue = confirmGO;
        so.FindProperty("_txtFaseNombre")            .objectReferenceValue = txtFase;
        so.FindProperty("_txtEquipo")                .objectReferenceValue = txtEquipo;
        so.FindProperty("_txtEnergyCost")            .objectReferenceValue = txtCost;
        so.FindProperty("_btnConfirmarBatalla")      .objectReferenceValue = btnConfGO.GetComponent<Button>();
        so.FindProperty("_btnCancelarConfirmacion")  .objectReferenceValue = btnCancelGO.GetComponent<Button>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── Utilidades ─────────────────────────────────────────────────────────

    private static GameObject MakePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        SetAnchors(go, anchorMin, anchorMax);
        return go;
    }

    private static GameObject MakeButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.2f, 0.1f, 0.3f);
        go.AddComponent<Button>();
        SetAnchors(go, anchorMin, anchorMax);

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(go.transform, false);
        var tmp = labelGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 16f;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        var rt       = labelGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return go;
    }

    private static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
    {
        // RectTransform no puede añadirse directamente — necesita un componente UI primero.
        // Todos los callers deben añadir Image (u otro componente UI) antes de llamar aquí.
        var rt = go.GetComponent<RectTransform>();
        if (rt == null)
        {
            Debug.LogError($"[SetupCampaign] {go.name} no tiene RectTransform. Añadir un componente UI antes de SetAnchors.");
            return;
        }
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
