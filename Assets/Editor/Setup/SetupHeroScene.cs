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
///   HeaderBar    (x 0.00-1.00, y 0.90-1.00): BtnVolver + Titulo
///   ZonaRoster   (x 0.02-0.62, y 0.05-0.88): ScrollRect vertical con PooledGridView
///   ZonaDetalle  (x 0.64-0.98, y 0.05-0.88): retrato + tabs Info/Habilidades/Equipo
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
        Anch(btnVolverGO, 0.01f, 0.15f, 0.10f, 0.85f);
        Img(btnVolverGO, Hex("#1A1020"));
        var btnVolver = btnVolverGO.AddComponent<Button>();
        Txt(Child(btnVolverGO.transform, "Label"), "< Volver", 12f, Hex("#A855F7"), bold: true);

        var tituloGO = Child(header.transform, "Titulo");
        Anch(tituloGO, 0.30f, 0.10f, 0.70f, 0.90f);
        Txt(tituloGO, "ESBIRRATORIO", 18f, Color.white, bold: true);

        // ── ZonaRoster ───────────────────────────────────────────────────────

        var zonaRoster = Child(canvasTr, "ZonaRoster");
        Anch(zonaRoster, 0.02f, 0.05f, 0.62f, 0.88f);
        Img(zonaRoster, Hex("#0D0D1A"), 0.6f);

        var scrollGO = Child(zonaRoster.transform, "ScrollRoster");
        Anch(scrollGO, 0.01f, 0.01f, 0.99f, 0.99f);
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        var viewportGO = Child(scrollGO.transform, "Viewport");
        Anch(viewportGO, 0f, 0f, 1f, 1f);
        Img(viewportGO, Color.white, 0.01f);
        var mask = viewportGO.AddComponent<RectMask2D>();
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

        // Template de carta (oculto, PooledGridView instancia clones)
        var cardTemplate = BuildCardTemplate(canvasTr);

        // ── ZonaDetalle ──────────────────────────────────────────────────────

        var zonaDetalle = Child(canvasTr, "ZonaDetalle");
        Anch(zonaDetalle, 0.64f, 0.05f, 0.98f, 0.88f);
        Img(zonaDetalle, Hex("#0D0D1A"), 0.6f);
        zonaDetalle.SetActive(false);

        var portraitGO = Child(zonaDetalle.transform, "Portrait");
        Anch(portraitGO, 0.05f, 0.68f, 0.35f, 0.97f);
        var portraitImg = Img(portraitGO, Color.white);

        var nombreGO = Child(zonaDetalle.transform, "Nombre");
        Anch(nombreGO, 0.38f, 0.86f, 0.98f, 0.97f);
        var nombreTxt = Txt(nombreGO, "—", 16f, Color.white, TextAlignmentOptions.Left, bold: true);

        var nivelGO = Child(zonaDetalle.transform, "NivelEstrellas");
        Anch(nivelGO, 0.38f, 0.78f, 0.98f, 0.86f);
        var nivelTxt = Txt(nivelGO, "—", 12f, Hex("#AAAAAA"), TextAlignmentOptions.Left);

        var claseGO = Child(zonaDetalle.transform, "ClaseElemento");
        Anch(claseGO, 0.38f, 0.70f, 0.98f, 0.78f);
        var claseTxt = Txt(claseGO, "—", 12f, Hex("#AAAAAA"), TextAlignmentOptions.Left);

        var btnFavGO = Child(zonaDetalle.transform, "BtnFavorito");
        Anch(btnFavGO, 0.05f, 0.60f, 0.35f, 0.67f);
        Img(btnFavGO, Hex("#1A1020"));
        var btnFav = btnFavGO.AddComponent<Button>();
        var btnFavLabel = Txt(Child(btnFavGO.transform, "Label"), "☆ Marcar favorito", 11f, Hex("#A855F7"), bold: true);

        // Tabs
        var tabBar = Child(zonaDetalle.transform, "TabBar");
        Anch(tabBar, 0.05f, 0.60f, 0.98f, 0.67f);

        var btnTabInfoGO = Child(tabBar.transform, "BtnInfo");
        Anch(btnTabInfoGO, 0.00f, 0f, 0.32f, 1f);
        Img(btnTabInfoGO, Hex("#1A1020"));
        var btnTabInfo = btnTabInfoGO.AddComponent<Button>();
        Txt(Child(btnTabInfoGO.transform, "Label"), "Info", 11f, Color.white, bold: true);

        var btnTabHabGO = Child(tabBar.transform, "BtnHabilidades");
        Anch(btnTabHabGO, 0.34f, 0f, 0.66f, 1f);
        Img(btnTabHabGO, Hex("#1A1020"));
        var btnTabHab = btnTabHabGO.AddComponent<Button>();
        Txt(Child(btnTabHabGO.transform, "Label"), "Habilidades", 10f, Color.white, bold: true);

        var btnTabEqGO = Child(tabBar.transform, "BtnEquipo");
        Anch(btnTabEqGO, 0.68f, 0f, 1.00f, 1f);
        Img(btnTabEqGO, Hex("#1A1020"));
        var btnTabEq = btnTabEqGO.AddComponent<Button>();
        Txt(Child(btnTabEqGO.transform, "Label"), "Equipo", 11f, Color.white, bold: true);

        // Panel Info
        var panelInfo = Child(zonaDetalle.transform, "PanelInfo");
        Anch(panelInfo, 0.05f, 0.03f, 0.98f, 0.58f);
        var detailStatsTxt = Txt(panelInfo, "—", 13f, Color.white, TextAlignmentOptions.TopLeft);

        // Panel Habilidades
        var panelHab = Child(zonaDetalle.transform, "PanelHabilidades");
        Anch(panelHab, 0.05f, 0.03f, 0.98f, 0.58f);
        panelHab.SetActive(false);

        var habScrollGO = Child(panelHab.transform, "ScrollHabilidades");
        Anch(habScrollGO, 0f, 0f, 1f, 1f);
        var habScroll = habScrollGO.AddComponent<ScrollRect>();
        habScroll.horizontal = false;
        var habViewportGO = Child(habScrollGO.transform, "Viewport");
        Anch(habViewportGO, 0f, 0f, 1f, 1f);
        Img(habViewportGO, Color.white, 0.01f);
        habViewportGO.AddComponent<RectMask2D>();
        var habContentGO = Child(habViewportGO.transform, "Content");
        var habContentRT = habContentGO.GetComponent<RectTransform>();
        habContentRT.anchorMin = new Vector2(0f, 1f);
        habContentRT.anchorMax = new Vector2(1f, 1f);
        habContentRT.pivot = new Vector2(0.5f, 1f);
        var habVLG = habContentGO.AddComponent<VerticalLayoutGroup>();
        habVLG.childControlWidth = true;
        habVLG.childControlHeight = false;
        habVLG.childForceExpandHeight = false;
        habContentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        habScroll.viewport = habViewportGO.GetComponent<RectTransform>();
        habScroll.content = habContentRT;

        // Panel Equipo
        var panelEq = Child(zonaDetalle.transform, "PanelEquipo");
        Anch(panelEq, 0.05f, 0.03f, 0.98f, 0.58f);
        panelEq.SetActive(false);
        var equipoTxt = Txt(panelEq, "—", 13f, Color.white, TextAlignmentOptions.TopLeft);

        // ── Wiring ───────────────────────────────────────────────────────────

        var controller = canvasGO.AddComponent<HeroSceneController>();
        var so = new SerializedObject(controller);
        so.FindProperty("_scrollRect").objectReferenceValue = scrollRect;
        so.FindProperty("_content").objectReferenceValue = contentRT;
        so.FindProperty("_viewport").objectReferenceValue = viewportRT;
        so.FindProperty("_cardTemplate").objectReferenceValue = cardTemplate;

        so.FindProperty("_detailPanel").objectReferenceValue = zonaDetalle;
        so.FindProperty("_detailPortrait").objectReferenceValue = portraitImg;
        so.FindProperty("_detailNombre").objectReferenceValue = nombreTxt;
        so.FindProperty("_detailNivelEstrellas").objectReferenceValue = nivelTxt;
        so.FindProperty("_detailClaseElemento").objectReferenceValue = claseTxt;
        so.FindProperty("_detailStats").objectReferenceValue = detailStatsTxt;
        so.FindProperty("_btnFavorito").objectReferenceValue = btnFav;
        so.FindProperty("_btnFavoritoLabel").objectReferenceValue = btnFavLabel;

        so.FindProperty("_btnTabInfo").objectReferenceValue = btnTabInfo;
        so.FindProperty("_btnTabHabilidades").objectReferenceValue = btnTabHab;
        so.FindProperty("_btnTabEquipo").objectReferenceValue = btnTabEq;
        so.FindProperty("_panelInfo").objectReferenceValue = panelInfo;
        so.FindProperty("_panelHabilidades").objectReferenceValue = panelHab;
        so.FindProperty("_panelEquipo").objectReferenceValue = panelEq;
        so.FindProperty("_habilidadesContent").objectReferenceValue = habContentRT;
        so.FindProperty("_equipoContent").objectReferenceValue = equipoTxt;

        so.FindProperty("_btnVolver").objectReferenceValue = btnVolver;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject BuildCardTemplate(Transform canvasTr)
    {
        var cardGO = Child(canvasTr, "CardTemplate");
        var rt = cardGO.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(140f, 170f);
        Img(cardGO, Hex("#1A1020"));
        var btn = cardGO.AddComponent<Button>();

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
        Img(favGO, Hex("#F59E0B"));
        favGO.SetActive(false);

        var view = cardGO.AddComponent<HeroCardView>();
        var so = new SerializedObject(view);
        so.FindProperty("_portrait").objectReferenceValue = portraitImg;
        so.FindProperty("_nombre").objectReferenceValue = nombreTxt;
        so.FindProperty("_nivel").objectReferenceValue = nivelTxt;
        so.FindProperty("_favoritoIcon").objectReferenceValue = favGO;
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
