// Assets/Editor/LegionSceneBuilder.cs
// Genera LegionScene.unity con TopBar (contador + dropdown) y Scroll con Content.
// Conecta a tus LegionGridController y LegionSceneController (sin tipos extra).

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class LegionSceneBuilder
{
    [MenuItem("Tools/Reinos/Generar Legion Scene (Colección)")]
    public static void BuildLegionScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // EventSystem
        var evGO = new GameObject("EventSystem", typeof(EventSystem));
        #if ENABLE_INPUT_SYSTEM
        evGO.AddComponent<InputSystemUIInputModule>();
        #else
        evGO.AddComponent<StandaloneInputModule>();
        #endif

        // Canvas
        var canvas = CreateCanvas();
        var canvasRT = canvas.GetComponent<RectTransform>();

        // ===== TopBar =====
        var topBar = CreateRT("TopBar", canvasRT);
        AnchorTop(topBar, height: 90f, left: 16f, right: 16f, top: 16f);
        var topImg = topBar.gameObject.AddComponent<Image>(); topImg.color = new Color(0,0,0,0.25f);

        var txtTitle = CreateTMP("TxtTitle", topBar, "Colección de Héroes", 40, TextAlignmentOptions.Left);
        SetRect(txtTitle.rectTransform, new Vector2(0,0.5f), new Vector2(0,0.5f), new Vector2(12f,0), new Vector2(700,80));

        var txtTotal = CreateTMP("TxtTotalHeroes", topBar, "0/0", 36, TextAlignmentOptions.Right);
        SetRect(txtTotal.rectTransform, new Vector2(1,0.5f), new Vector2(1,0.5f), new Vector2(-250f,0), new Vector2(160,72));

        var ddGO = CreateDropdown("DdOrden", topBar);
        SetRect(ddGO.GetComponent<RectTransform>(), new Vector2(1,0.5f), new Vector2(1,0.5f), new Vector2(-12f,0), new Vector2(220,54));
        var dd = ddGO.GetComponent<Dropdown>();
        dd.options.Clear();
        dd.options.Add(new Dropdown.OptionData("Elemento"));
        dd.options.Add(new Dropdown.OptionData("Facción"));
        dd.options.Add(new Dropdown.OptionData("Clase"));
        dd.options.Add(new Dropdown.OptionData("⭐ BaseStars"));
        dd.value = 0;

        // ===== ScrollView =====
        var root = CreateRT("CollectionRoot", canvasRT);
        AnchorFillBelow(root, top: 16f + 90f + 8f, left: 16f, right: 16f, bottom: 16f);

        var (scrollRect, viewportRT, contentRT) = CreateScroll(root);

        // ===== Template de Card (LegionCardUI) =====
        LegionCardUI cardTemplate = GameObject.Find("LegionCardTemplate")
            ? GameObject.Find("LegionCardTemplate").GetComponent<LegionCardUI>()
            : null;
        if (cardTemplate == null)
            cardTemplate = CreateBasicLegionCardTemplate(canvasRT); // se deja desactivado fuera del Content

        // ===== LegionGridController =====
        var gridGO = new GameObject("LegionGridController", typeof(RectTransform), typeof(LegionGridController));
        gridGO.transform.SetParent(root, false);
        var grid = gridGO.GetComponent<LegionGridController>();
        var soGrid = new SerializedObject(grid);
        soGrid.FindProperty("contentRoot").objectReferenceValue = contentRT;
        soGrid.FindProperty("heroCardTemplate").objectReferenceValue = cardTemplate;
        soGrid.ApplyModifiedProperties();

        // ===== LegionSceneController =====
        var sceneCtrlGO = new GameObject("LegionSceneController", typeof(LegionSceneController));
        var sceneCtrl = sceneCtrlGO.GetComponent<LegionSceneController>();
        var soScene = new SerializedObject(sceneCtrl);
        soScene.FindProperty("txtTotalHeroes").objectReferenceValue = txtTotal;
        soScene.FindProperty("ddOrden").objectReferenceValue = dd;
        soScene.FindProperty("heroGrid").objectReferenceValue = grid;
        soScene.ApplyModifiedProperties();

        // ===== GameDataManager (para Play) =====
        new GameObject("GameDataManager", typeof(GameDataManager));

        // Guardar
        const string savePath = "Assets/Scenes/LegionScene.unity";
        EnsureFolder("Assets/Scenes");
        if (EditorSceneManager.SaveScene(scene, savePath))
        {
            Debug.Log($"✅ LegionScene guardada en: {savePath}");
            AssetDatabase.Refresh();
        }
        else Debug.LogError("❌ Error guardando LegionScene.unity");
    }

    // ---------------- Helpers ----------------
    private static GameObject CreateCanvas()
    {
        var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var cv = go.GetComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = go.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        return go;
    }

    private static RectTransform CreateRT(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static TextMeshProUGUI CreateTMP(string name, RectTransform parent, string text, int size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.alignment = align; tmp.color = Color.white;
        return tmp;
    }

    private static GameObject CreateDropdown(string name, RectTransform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Dropdown));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.18f,0.18f,0.18f,0.95f);

        // Label
        var lbl = new GameObject("Label", typeof(RectTransform), typeof(Text));
        lbl.transform.SetParent(go.transform, false);
        var tl = lbl.GetComponent<Text>(); tl.text="Elemento"; tl.alignment=TextAnchor.MiddleLeft; tl.color=Color.white; tl.fontSize=20;
        var rtL = lbl.GetComponent<RectTransform>(); rtL.anchorMin=Vector2.zero; rtL.anchorMax=Vector2.one; rtL.offsetMin=new Vector2(10,0); rtL.offsetMax=new Vector2(-25,0);

        // Arrow
        var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
        arrow.transform.SetParent(go.transform, false);
        var rtA = arrow.GetComponent<RectTransform>(); rtA.anchorMin=new Vector2(1,0.5f); rtA.anchorMax=new Vector2(1,0.5f); rtA.sizeDelta=new Vector2(16,16); rtA.anchoredPosition=new Vector2(-10,0);

        // Template
        var templ = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        templ.transform.SetParent(go.transform, false);
        var rtT = templ.GetComponent<RectTransform>(); rtT.anchorMin=new Vector2(0,0); rtT.anchorMax=new Vector2(1,0); rtT.pivot=new Vector2(0.5f,1); rtT.anchoredPosition=new Vector2(0,-4); rtT.sizeDelta=new Vector2(0,150);
        templ.SetActive(false);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        viewport.transform.SetParent(templ.transform,false);
        var rtV = viewport.GetComponent<RectTransform>(); rtV.anchorMin=Vector2.zero; rtV.anchorMax=Vector2.one; rtV.offsetMin=Vector2.zero; rtV.offsetMax=Vector2.zero;
        viewport.GetComponent<Mask>().showMaskGraphic=false; viewport.GetComponent<Image>().color=new Color(1,1,1,0.05f);

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform,false);
        var rtC = content.GetComponent<RectTransform>(); rtC.anchorMin=new Vector2(0,1); rtC.anchorMax=new Vector2(1,1); rtC.pivot=new Vector2(0.5f,1); rtC.anchoredPosition=Vector2.zero; rtC.sizeDelta=new Vector2(0,0);

        var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
        item.transform.SetParent(content.transform,false);
        var rtI = item.GetComponent<RectTransform>(); rtI.anchorMin=new Vector2(0,1); rtI.anchorMax=new Vector2(1,1); rtI.sizeDelta=new Vector2(0,30);

        var itemBg = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
        itemBg.transform.SetParent(item.transform,false);
        var rtBg = itemBg.GetComponent<RectTransform>(); rtBg.anchorMin=Vector2.zero; rtBg.anchorMax=Vector2.one; rtBg.offsetMin=Vector2.zero; rtBg.offsetMax=Vector2.zero;

        var itemCheck = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
        itemCheck.transform.SetParent(item.transform,false);
        var rtCk = itemCheck.GetComponent<RectTransform>(); rtCk.anchorMin=new Vector2(0,0.5f); rtCk.anchorMax=new Vector2(0,0.5f); rtCk.sizeDelta=new Vector2(20,20); rtCk.anchoredPosition=new Vector2(10,0);

        var itemLabel = new GameObject("Item Label", typeof(RectTransform), typeof(Text));
        itemLabel.transform.SetParent(item.transform,false);
        var tlI = itemLabel.GetComponent<Text>(); tlI.color=Color.white; tlI.alignment=TextAnchor.MiddleLeft; tlI.fontSize=20; tlI.text="Opción";
        var rtIL = itemLabel.GetComponent<RectTransform>(); rtIL.anchorMin=new Vector2(0,0); rtIL.anchorMax=new Vector2(1,1); rtIL.offsetMin=new Vector2(32,0); rtIL.offsetMax=new Vector2(0,0);

        var dd = go.GetComponent<Dropdown>();
        dd.template = rtT;
        dd.captionText = tl;
        dd.itemText = tlI;

        var sr = templ.GetComponent<ScrollRect>();
        sr.content = rtC;
        sr.viewport = rtV;
        sr.horizontal = false; sr.vertical = true;

        return go;
    }

    private static (ScrollRect, RectTransform, RectTransform) CreateScroll(RectTransform parent)
    {
        var root = new GameObject("CollectionScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        root.transform.SetParent(parent, false);
        var rootRT = root.GetComponent<RectTransform>();
        SetRect(rootRT, new Vector2(0,0), new Vector2(1,1), Vector2.zero, Vector2.zero);
        root.GetComponent<Image>().color = new Color(0,0,0,0.10f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        viewport.transform.SetParent(root.transform, false);
        var viewportRT = viewport.GetComponent<RectTransform>();
        SetRect(viewportRT, new Vector2(0,0), new Vector2(1,1), Vector2.zero, Vector2.zero);
        viewport.GetComponent<Image>().color = new Color(1,1,1,0.05f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0,1);
        contentRT.anchorMax = new Vector2(1,1);
        contentRT.pivot     = new Vector2(0,1);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0,0);

        var sr = root.GetComponent<ScrollRect>();
        sr.viewport = viewportRT;
        sr.content  = contentRT;
        sr.horizontal = false; sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;

        return (sr, viewportRT, contentRT);
    }

    // ====== Card Template básico (si no existe uno tuyo) ======
    private static LegionCardUI CreateBasicLegionCardTemplate(RectTransform parent)
    {
        var template = new GameObject("LegionCardTemplate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LegionCardUI));
        template.transform.SetParent(parent, false);
        var tRT = template.GetComponent<RectTransform>();
        tRT.sizeDelta = new Vector2(240, 320);
        template.GetComponent<Image>().color = new Color(0,0,0,0.3f);
        template.SetActive(false);

        var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portrait.transform.SetParent(template.transform, false);
        SetRect(portrait.GetComponent<RectTransform>(), new Vector2(0.5f,1f), new Vector2(0.5f,1f), new Vector2(0f,-20f), new Vector2(200,200));

        var overlay = new GameObject("LockOverlay", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(template.transform, false);
        SetRect(overlay.GetComponent<RectTransform>(), new Vector2(0.5f,1f), new Vector2(0.5f,1f), new Vector2(0f,-20f), new Vector2(200,200));
        overlay.GetComponent<Image>().color = new Color(0,0,0,0.55f);
        overlay.SetActive(false);

        var starsRoot = new GameObject("StarsRoot", typeof(RectTransform));
        starsRoot.transform.SetParent(template.transform, false);
        var srRT = starsRoot.GetComponent<RectTransform>();
        SetRect(srRT, new Vector2(0.5f,0f), new Vector2(0.5f,0f), new Vector2(0f,18f), new Vector2(200,40));
        var hlg = starsRoot.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter; hlg.spacing = 4;

        var starPrefab = new GameObject("StarTemplate", typeof(RectTransform), typeof(Image));
        starPrefab.transform.SetParent(starsRoot.transform, false);
        SetRect(starPrefab.GetComponent<RectTransform>(), new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(24,24));
        starPrefab.SetActive(false);

        var txtName = CreateTMP("TxtName", tRT, "Hero Name", 22, TextAlignmentOptions.Center);
        SetRect(txtName.rectTransform, new Vector2(0.5f,0f), new Vector2(0.5f,0f), new Vector2(0f,48f), new Vector2(200,40));

        // Asignar campos serializados del LegionCardUI
        var cui = template.GetComponent<LegionCardUI>();
        var so = new SerializedObject(cui);
        so.FindProperty("portraitImage").objectReferenceValue = portrait.GetComponent<Image>();
        so.FindProperty("lockOverlay").objectReferenceValue  = overlay;
        so.FindProperty("starsRoot").objectReferenceValue    = starsRoot.transform;
        so.FindProperty("starPrefab").objectReferenceValue   = starPrefab;
        so.FindProperty("btnSelect").objectReferenceValue    = template.GetComponent<Button>();
        so.FindProperty("txtName").objectReferenceValue      = txtName;
        so.ApplyModifiedProperties();

        return cui;
    }

    // -------- Rect helpers ----------
    private static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    { rt.anchorMin=aMin; rt.anchorMax=aMax; rt.anchoredPosition=pos; rt.sizeDelta=size; }

    private static void AnchorTop(RectTransform rt, float height, float left, float right, float top)
    { rt.anchorMin=new Vector2(0,1); rt.anchorMax=new Vector2(1,1); rt.pivot=new Vector2(0.5f,1); rt.offsetMin=new Vector2(left,-height-top); rt.offsetMax=new Vector2(-right,-top); }

    private static void AnchorFillBelow(RectTransform rt, float top, float left, float right, float bottom)
    { rt.anchorMin=new Vector2(0,0); rt.anchorMax=new Vector2(1,1); rt.pivot=new Vector2(0.5f,0.5f); rt.offsetMin=new Vector2(left,bottom); rt.offsetMax=new Vector2(-right,-top); }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parts = path.Split('/');
            string cur = parts[0];
            for (int i=1;i<parts.Length;i++)
            {
                var next=$"{cur}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }
}
