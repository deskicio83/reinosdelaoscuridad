#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ReinoOscuridad.UI.Campaign;

/// Editor Script — genera el prefab RewardPanel.
/// Menú: Tools → Reino Oscuridad → 8. Setup RewardPanel Prefab
///
/// LAYOUT (1280×720, overlay sobre CampaignScene):
///   Canvas (ScreenSpaceOverlay, sortOrder 50)
///   ├── Background (full-screen, semitransparente)
///   └── PanelPrincipal (0.05–0.95 x 0.05–0.95)
///       ├── TxtResultado      (VICTORIA / DERROTA)
///       ├── TxtEstrellas      (★★★ / ★★☆ / ★☆☆)
///       ├── TxtXPJugador      (+N XP  Jugador Nv.X)
///       ├── XPBarContainer    → XPBarBg → XPBarFill ← _barraXPFill
///       ├── ContenedorHeroes  (filas generadas en runtime) ← _contenedorHeroes
///       ├── ContenedorDrops   (items generados en runtime) ← _contenedorDrops
///       └── Botones
///           ├── BtnSiguienteFase ← _btnSiguienteFase
///           └── BtnVolverMapa    ← _btnVolverMapa
public static class SetupRewardPanelPrefab
{
    private const string PREFAB_PATH = "Assets/Prefabs/UI/RewardPanel.prefab";

    [MenuItem("Tools/Reino Oscuridad/8. Setup RewardPanel Prefab")]
    public static void Run()
    {
        EnsureDirectory("Assets/Prefabs/UI");

        var root = Build();

        bool success;
        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out success);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(success
            ? $"[SetupRewardPanelPrefab] Prefab guardado: {PREFAB_PATH}"
            : "[SetupRewardPanelPrefab] Error guardando prefab.");
    }

    // ── Construcción ─────────────────────────────────────────────────────────

    private static GameObject Build()
    {
        // ── Root: Canvas ──────────────────────────────────────────────────────

        var root   = new GameObject("RewardPanel");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight  = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // ── Background ────────────────────────────────────────────────────────

        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(root.transform, false);
        bgGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.90f);
        SetAnchors(bgGO, Vector2.zero, Vector2.one);

        // ── Panel principal ───────────────────────────────────────────────────

        var panelGO = new GameObject("PanelPrincipal");
        panelGO.transform.SetParent(root.transform, false);
        panelGO.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.14f);
        SetAnchors(panelGO, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));

        // Resultado (VICTORIA / DERROTA)
        var txtResultadoGO = new GameObject("TxtResultado");
        txtResultadoGO.transform.SetParent(panelGO.transform, false);
        var txtResultado = txtResultadoGO.AddComponent<TextMeshProUGUI>();
        txtResultado.text      = "VICTORIA";
        txtResultado.fontSize  = 32f;
        txtResultado.color     = new Color(0.4f, 1f, 0.4f);
        txtResultado.alignment = TextAlignmentOptions.Center;
        SetAnchors(txtResultadoGO, new Vector2(0.02f, 0.86f), new Vector2(0.98f, 1f));

        // Estrellas (3 Image sprites — sin caracteres Unicode)
        var estrellasContainer = new GameObject("EstrellasFila");
        estrellasContainer.transform.SetParent(panelGO.transform, false);
        estrellasContainer.AddComponent<Image>().color = Color.clear;
        var estHLG = estrellasContainer.AddComponent<HorizontalLayoutGroup>();
        estHLG.childForceExpandWidth  = false;
        estHLG.childForceExpandHeight = false;
        estHLG.spacing    = 10f;
        estHLG.childAlignment = TextAnchor.MiddleCenter;
        SetAnchors(estrellasContainer, new Vector2(0.20f, 0.78f), new Vector2(0.80f, 0.87f));

        var rewardStarImages = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var star = new GameObject($"Star_{i}");
            star.transform.SetParent(estrellasContainer.transform, false);
            rewardStarImages[i] = star.AddComponent<Image>();
            rewardStarImages[i].color = new Color(0.98f, 0.80f, 0.08f); // dorado por defecto
            var le = star.AddComponent<LayoutElement>();
            le.preferredWidth  = 36f;
            le.preferredHeight = 36f;
        }

        // XP jugador (texto)
        var txtXPGO = new GameObject("TxtXPJugador");
        txtXPGO.transform.SetParent(panelGO.transform, false);
        var txtXP = txtXPGO.AddComponent<TextMeshProUGUI>();
        txtXP.text      = "+0 XP   Jugador Nv.1";
        txtXP.fontSize  = 15f;
        txtXP.color     = new Color(0.8f, 0.8f, 0.4f);
        txtXP.alignment = TextAlignmentOptions.Center;
        SetAnchors(txtXPGO, new Vector2(0.02f, 0.71f), new Vector2(0.98f, 0.78f));

        // Barra XP jugador
        var xpBarContainerGO = new GameObject("XPBarContainer");
        xpBarContainerGO.transform.SetParent(panelGO.transform, false);
        xpBarContainerGO.AddComponent<Image>().color = Color.clear;
        SetAnchors(xpBarContainerGO, new Vector2(0.03f, 0.67f), new Vector2(0.97f, 0.72f));

        var xpBarBgGO = new GameObject("XPBarBg");
        xpBarBgGO.transform.SetParent(xpBarContainerGO.transform, false);
        xpBarBgGO.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.20f);
        SetAnchors(xpBarBgGO, Vector2.zero, Vector2.one);

        var xpBarFillGO = new GameObject("XPBarFill");
        xpBarFillGO.transform.SetParent(xpBarBgGO.transform, false);
        xpBarFillGO.AddComponent<Image>().color = new Color(0.35f, 0.55f, 1f);
        var fillRT      = xpBarFillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0f, 1f); // actualizado en runtime
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        // Contenedor de filas de héroes
        var contenedorHeroesGO = new GameObject("ContenedorHeroes");
        contenedorHeroesGO.transform.SetParent(panelGO.transform, false);
        contenedorHeroesGO.AddComponent<Image>().color = Color.clear;
        var heroesVLG = contenedorHeroesGO.AddComponent<VerticalLayoutGroup>();
        heroesVLG.spacing             = 4f;
        heroesVLG.padding             = new RectOffset(4, 4, 4, 4);
        heroesVLG.childForceExpandWidth  = true;
        heroesVLG.childForceExpandHeight = false;
        contenedorHeroesGO.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;
        SetAnchors(contenedorHeroesGO, new Vector2(0.02f, 0.35f), new Vector2(0.98f, 0.66f));

        // Contenedor de drops
        var contenedorDropsGO = new GameObject("ContenedorDrops");
        contenedorDropsGO.transform.SetParent(panelGO.transform, false);
        contenedorDropsGO.AddComponent<Image>().color = new Color(0.06f, 0.04f, 0.10f);
        var dropsHLG = contenedorDropsGO.AddComponent<HorizontalLayoutGroup>();
        dropsHLG.spacing             = 6f;
        dropsHLG.padding             = new RectOffset(6, 6, 4, 4);
        dropsHLG.childForceExpandWidth  = false;
        dropsHLG.childForceExpandHeight = true;
        dropsHLG.childAlignment         = TextAnchor.MiddleLeft;
        SetAnchors(contenedorDropsGO, new Vector2(0.02f, 0.13f), new Vector2(0.98f, 0.34f));

        // Título de drops
        var lblDropsGO = new GameObject("LblDrops");
        lblDropsGO.transform.SetParent(panelGO.transform, false);
        var lblDrops = lblDropsGO.AddComponent<TextMeshProUGUI>();
        lblDrops.text      = "Drops:";
        lblDrops.fontSize  = 13f;
        lblDrops.color     = new Color(0.7f, 0.7f, 0.7f);
        lblDrops.alignment = TextAlignmentOptions.MidlineLeft;
        SetAnchors(lblDropsGO, new Vector2(0.02f, 0.34f), new Vector2(0.25f, 0.39f));

        // ── Botones ───────────────────────────────────────────────────────────

        var btnSigGO = MakeButton(panelGO.transform, "BtnSiguienteFase", "Siguiente fase",
            new Vector2(0.01f, 0.01f), new Vector2(0.48f, 0.12f),
            new Color(0.12f, 0.35f, 0.12f));

        var btnVolverGO = MakeButton(panelGO.transform, "BtnVolverMapa", "Volver al mapa",
            new Vector2(0.51f, 0.01f), new Vector2(0.99f, 0.12f),
            new Color(0.20f, 0.10f, 0.30f));

        // ── RewardPanel MonoBehaviour ─────────────────────────────────────────

        var comp = root.AddComponent<RewardPanel>();
        var so   = new SerializedObject(comp);

        so.FindProperty("_txtResultado")     .objectReferenceValue = txtResultado;
        var rewardStarsProp = so.FindProperty("_imgEstrellas");
        rewardStarsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            rewardStarsProp.GetArrayElementAtIndex(i).objectReferenceValue = rewardStarImages[i];
        so.FindProperty("_txtXPJugador")     .objectReferenceValue = txtXP;
        so.FindProperty("_barraXPFill")      .objectReferenceValue = fillRT;
        so.FindProperty("_contenedorHeroes") .objectReferenceValue = contenedorHeroesGO.transform;
        so.FindProperty("_contenedorDrops")  .objectReferenceValue = contenedorDropsGO.transform;
        so.FindProperty("_btnSiguienteFase") .objectReferenceValue = btnSigGO.GetComponent<Button>();
        so.FindProperty("_btnVolverMapa")    .objectReferenceValue = btnVolverGO.GetComponent<Button>();
        so.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    // ── Utilidades ────────────────────────────────────────────────────────────

    private static GameObject MakeButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        go.AddComponent<Button>();
        SetAnchors(go, anchorMin, anchorMax);

        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        var tmp   = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 14f;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        var lRT       = lblGO.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.offsetMin = Vector2.zero;
        lRT.offsetMax = Vector2.zero;

        return go;
    }

    private static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void EnsureDirectory(string assetPath)
    {
        string fullPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
        if (!Directory.Exists(fullPath))
            Directory.CreateDirectory(fullPath);
    }
}
#endif
