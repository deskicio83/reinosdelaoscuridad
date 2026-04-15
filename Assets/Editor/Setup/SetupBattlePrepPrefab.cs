#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ReinoOscuridad.UI.Campaign;

/// Editor Script — genera el prefab BattlePrepPanel (estilo RAID Shadow Legends).
/// Menú: Tools → Reino Oscuridad → 7. Setup BattlePrep Prefab
///
/// LAYOUT (1280×720, overlay full-screen):
///   Canvas (sortOrder 50)
///   ├── Background (full, oscuro 87% opacidad)
///   └── PanelRoot (full)
///       ├── BarraSuperior  (top 10%)  — TxtTitulo · TxtEnergia · BtnCancelar
///       ├── SepH           (línea separadora)
///       ├── ZonaIzquierda  (mid 55%, left 50%) — slots 2×2 + BtnBatallar
///       ├── SepV           (línea separadora vertical)
///       ├── ZonaDerecha    (mid 55%, right 50%) — ContenedorEnemigos (dinámico)
///       └── ZonaHeroes     (bot 35%) — header + scroll horizontal
///           └── ContenedorHeroes (HLG + CSF, dinámico)
public static class SetupBattlePrepPrefab
{
    private const string PREFAB_PATH = "Assets/Prefabs/UI/BattlePrepPanel.prefab";

    [MenuItem("Tools/Reino Oscuridad/7. Setup BattlePrep Prefab")]
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
            ? $"[SetupBattlePrepPrefab] Prefab guardado: {PREFAB_PATH}"
            : "[SetupBattlePrepPrefab] Error guardando prefab.");
    }

    // ── Construcción ─────────────────────────────────────────────────────────

    private static GameObject Build()
    {
        // Paleta
        var cFondo    = new Color(0f,    0f,    0f,    0.88f);
        var cPanelBg  = new Color(0.08f, 0.06f, 0.14f, 1f);
        var cTopBar   = new Color(0.06f, 0.04f, 0.12f, 1f);
        var cIzq      = new Color(0.07f, 0.12f, 0.10f, 1f);   // verde oscuro
        var cDer      = new Color(0.06f, 0.05f, 0.14f, 1f);   // azul-morado oscuro
        var cSlot     = new Color(0.06f, 0.10f, 0.18f, 1f);   // slot vacío
        var cHeroes   = new Color(0.05f, 0.04f, 0.10f, 1f);   // banda inferior
        var cSep      = new Color(0.40f, 0.25f, 0.60f, 0.45f);
        var cOro      = new Color(0.95f, 0.78f, 0.22f, 1f);
        var cSub      = new Color(0.60f, 0.55f, 0.70f, 1f);

        // ── Canvas ────────────────────────────────────────────────────────────

        var root   = new GameObject("BattlePrepPanel");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight  = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        // ── Fondo ─────────────────────────────────────────────────────────────

        MakeImage(root.transform, "Background", cFondo, Vector2.zero, Vector2.one);

        // ── Panel raíz ────────────────────────────────────────────────────────

        var panelRoot = MakeImage(root.transform, "PanelRoot", cPanelBg, Vector2.zero, Vector2.one);

        // ── BARRA SUPERIOR (y 0.90 – 1.00) ────────────────────────────────────

        var topBar = MakeImage(panelRoot.transform, "BarraSuperior",
            cTopBar, new Vector2(0f, 0.90f), Vector2.one);

        // Titulo (izquierda 65%)
        var txtTituloGO = MakeTMP(topBar.transform, "TxtTitulo",
            "Mundo 1 · Fase 1 · Normal",
            18f, cOro, TextAlignmentOptions.MidlineLeft,
            new Vector2(0.01f, 0.05f), new Vector2(0.64f, 0.95f));

        // Energia (centro-derecha)
        var txtEnergiaGO = MakeTMP(topBar.transform, "TxtEnergia",
            "Energía: 6",
            14f, new Color(0.4f, 0.85f, 1f), TextAlignmentOptions.Midline,
            new Vector2(0.65f, 0.05f), new Vector2(0.84f, 0.95f));

        // Botón cancelar X (extremo derecho)
        var btnCancelGO = MakeButton(topBar.transform, "BtnCancelar", "✕  CANCELAR",
            new Vector2(0.85f, 0.05f), new Vector2(0.99f, 0.95f),
            new Color(0.40f, 0.08f, 0.08f));

        // Separador horizontal
        MakeImage(panelRoot.transform, "SepH", cSep,
            new Vector2(0f, 0.898f), new Vector2(1f, 0.902f));

        // ── ZONA IZQUIERDA — equipo (x 0–0.50, y 0.35–0.90) ──────────────────

        var zonaIzq = MakeImage(panelRoot.transform, "ZonaIzquierda",
            cIzq, new Vector2(0f, 0.35f), new Vector2(0.50f, 0.90f));

        // Header
        var hdrIzqGO = MakeImage(zonaIzq.transform, "HeaderIzq",
            new Color(0.05f, 0.18f, 0.12f), new Vector2(0f, 0.90f), Vector2.one);
        MakeTMP(hdrIzqGO.transform, "Txt", "TU EQUIPO  (toca un héroe abajo para añadir)",
            12f, cOro, TextAlignmentOptions.MidlineLeft,
            new Vector2(0.02f, 0f), Vector2.one);

        // 4 slots en cuadrícula 2×2
        //   Slot0 (izq-arriba), Slot1 (der-arriba), Slot2 (izq-abajo), Slot3 (der-abajo)
        var slotMinX = new float[] { 0.03f, 0.52f, 0.03f, 0.52f };
        var slotMaxX = new float[] { 0.50f, 0.97f, 0.50f, 0.97f };
        var slotMinY = new float[] { 0.46f, 0.46f, 0.10f, 0.10f };
        var slotMaxY = new float[] { 0.88f, 0.88f, 0.44f, 0.44f };
        var slots    = new Button[4];

        for (int i = 0; i < 4; i++)
        {
            // Frame (borde ligeramente más claro)
            var frameGO = MakeImage(zonaIzq.transform, $"Slot{i}Frame",
                new Color(0.18f, 0.35f, 0.25f, 0.55f),
                new Vector2(slotMinX[i], slotMinY[i]),
                new Vector2(slotMaxX[i], slotMaxY[i]));

            // Botón interior
            var slotGO  = new GameObject($"Slot{i}");
            slotGO.transform.SetParent(frameGO.transform, false);
            slotGO.AddComponent<Image>().color = cSlot;
            slots[i] = slotGO.AddComponent<Button>();
            SetAnchors(slotGO, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.96f));

            // Número de ranura (banda izquierda)
            var numBgGO = MakeImage(slotGO.transform, "NumBg",
                new Color(0.10f, 0.20f, 0.15f),
                Vector2.zero, new Vector2(0.22f, 1f));
            MakeTMP(numBgGO.transform, "NumTxt", $"{i + 1}",
                20f, cOro, TextAlignmentOptions.Midline,
                Vector2.zero, Vector2.one);

            // Icono de elemento (oculto si vacío)
            var iconGO  = MakeImage(slotGO.transform, "SlotIcon",
                new Color(0.3f, 0.9f, 0.5f),
                new Vector2(0.24f, 0.55f), new Vector2(0.98f, 0.98f));
            iconGO.SetActive(false);

            // Label
            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(slotGO.transform, false);
            var lTxt  = lblGO.AddComponent<TextMeshProUGUI>();
            lTxt.text      = $"<size=9><color=#555555>RANURA {i + 1}</color></size>\n<size=10>— libre —</size>";
            lTxt.fontSize  = 11f;
            lTxt.color     = new Color(0.7f, 0.9f, 0.8f);
            lTxt.alignment = TextAlignmentOptions.MidlineLeft;
            var lRT        = lblGO.GetComponent<RectTransform>();
            lRT.anchorMin  = new Vector2(0.24f, 0f);
            lRT.anchorMax  = Vector2.one;
            lRT.offsetMin  = new Vector2(6, 3);
            lRT.offsetMax  = new Vector2(-4, -3);
        }

        // Botón BATALLAR (dentro de ZonaIzquierda, parte baja)
        var btnBatallarGO = MakeButton(zonaIzq.transform, "BtnBatallar", "⚔  BATALLAR",
            new Vector2(0.05f, 0.01f), new Vector2(0.95f, 0.09f),
            new Color(0.55f, 0.08f, 0.05f));
        // Texto más grande para el botón principal
        var bLabel = btnBatallarGO.GetComponentInChildren<TextMeshProUGUI>();
        if (bLabel != null) { bLabel.fontSize = 16f; bLabel.fontStyle = FontStyles.Bold; }

        // ── Separador vertical entre zonas ────────────────────────────────────

        MakeImage(panelRoot.transform, "SepV", cSep,
            new Vector2(0.498f, 0.35f), new Vector2(0.502f, 0.90f));

        // ── ZONA DERECHA — enemigos (x 0.50–1.00, y 0.35–0.90) ───────────────

        var zonaDer = MakeImage(panelRoot.transform, "ZonaDerecha",
            cDer, new Vector2(0.50f, 0.35f), new Vector2(1f, 0.90f));

        // Header
        var hdrDerGO = MakeImage(zonaDer.transform, "HeaderDer",
            new Color(0.08f, 0.06f, 0.20f), new Vector2(0f, 0.90f), Vector2.one);
        MakeTMP(hdrDerGO.transform, "Txt", "ENEMIGOS",
            13f, new Color(1f, 0.4f, 0.4f), TextAlignmentOptions.MidlineLeft,
            new Vector2(0.02f, 0f), Vector2.one);

        // Contenedor de tarjetas de enemigos (dinámico en runtime)
        var contEnemigosGO = new GameObject("ContenedorEnemigos");
        contEnemigosGO.transform.SetParent(zonaDer.transform, false);
        contEnemigosGO.AddComponent<Image>().color = Color.clear;
        var eHLG = contEnemigosGO.AddComponent<HorizontalLayoutGroup>();
        eHLG.spacing              = 6f;
        eHLG.padding              = new RectOffset(8, 8, 6, 6);
        eHLG.childForceExpandWidth  = false;
        eHLG.childForceExpandHeight = false;
        eHLG.childAlignment         = TextAnchor.MiddleLeft;
        SetAnchors(contEnemigosGO, new Vector2(0f, 0.02f), new Vector2(1f, 0.88f));

        // ── Separador horizontal (entre media y hero roster) ──────────────────

        MakeImage(panelRoot.transform, "SepH2", cSep,
            new Vector2(0f, 0.348f), new Vector2(1f, 0.352f));

        // ── ZONA HÉROES — roster (y 0.00–0.35) ───────────────────────────────

        var zonaHeroes = MakeImage(panelRoot.transform, "ZonaHeroes",
            cHeroes, new Vector2(0f, 0f), new Vector2(1f, 0.35f));

        // Header del roster
        var hdrHeroesGO = MakeImage(zonaHeroes.transform, "HeaderHeroes",
            new Color(0.06f, 0.05f, 0.14f), new Vector2(0f, 0.85f), Vector2.one);
        MakeTMP(hdrHeroesGO.transform, "Txt",
            "ESBIRROS  —  toca para añadir al equipo  /  toca un slot para quitar",
            11f, cSub, TextAlignmentOptions.MidlineLeft,
            new Vector2(0.01f, 0f), Vector2.one);

        // Scroll horizontal
        var scrollGO  = new GameObject("ScrollHeroes");
        scrollGO.transform.SetParent(zonaHeroes.transform, false);
        scrollGO.AddComponent<Image>().color = Color.clear;
        var scrollRect = scrollGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical   = false;
        SetAnchors(scrollGO, new Vector2(0f, 0f), new Vector2(1f, 0.84f));

        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(scrollGO.transform, false);
        viewportGO.AddComponent<Image>().color = Color.clear;
        viewportGO.AddComponent<RectMask2D>();
        SetAnchors(viewportGO, Vector2.zero, Vector2.one);

        var contHeroesGO = new GameObject("ContenedorHeroes");
        contHeroesGO.transform.SetParent(viewportGO.transform, false);
        contHeroesGO.AddComponent<Image>().color = Color.clear;
        var hHLG = contHeroesGO.AddComponent<HorizontalLayoutGroup>();
        hHLG.spacing              = 4f;
        hHLG.padding              = new RectOffset(6, 6, 4, 4);
        hHLG.childForceExpandWidth  = false;
        hHLG.childForceExpandHeight = true;
        hHLG.childAlignment         = TextAnchor.MiddleLeft;
        var csf = contHeroesGO.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Anclar al borde izquierdo para scroll hacia la derecha
        var cRT = contHeroesGO.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0f, 0f);
        cRT.anchorMax = new Vector2(0f, 1f);
        cRT.pivot     = new Vector2(0f, 0.5f);
        cRT.offsetMin = Vector2.zero;
        cRT.offsetMax = Vector2.zero;

        scrollRect.content  = cRT;
        scrollRect.viewport = viewportGO.GetComponent<RectTransform>();

        // ── BattlePrepPanel MonoBehaviour ─────────────────────────────────────

        var comp = root.AddComponent<BattlePrepPanel>();
        var so   = new SerializedObject(comp);

        so.FindProperty("_txtTitulo").objectReferenceValue          = txtTituloGO.GetComponent<TMP_Text>();
        so.FindProperty("_txtEnergia").objectReferenceValue         = txtEnergiaGO.GetComponent<TMP_Text>();
        so.FindProperty("_contenedorEnemigos").objectReferenceValue = contEnemigosGO.transform;
        so.FindProperty("_contenedorHeroes").objectReferenceValue   = contHeroesGO.transform;
        so.FindProperty("_btnBatallar").objectReferenceValue        = btnBatallarGO.GetComponent<Button>();
        so.FindProperty("_btnCancelar").objectReferenceValue        = btnCancelGO.GetComponent<Button>();

        var slotsProp = so.FindProperty("_slotButtons");
        slotsProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
            slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];

        so.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

    // ── Utilidades ────────────────────────────────────────────────────────────

    private static GameObject MakeImage(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        SetAnchors(go, anchorMin, anchorMax);
        return go;
    }

    private static GameObject MakeTMP(Transform parent, string name, string text,
        float fontSize, Color color, TextAlignmentOptions alignment,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = color;
        tmp.alignment = alignment;
        SetAnchors(go, anchorMin, anchorMax);
        return go;
    }

    private static GameObject MakeButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        go.AddComponent<Button>();
        SetAnchors(go, anchorMin, anchorMax);

        var lGO = new GameObject("Label");
        lGO.transform.SetParent(go.transform, false);
        var tmp = lGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 14f;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Midline;
        var lRT = lGO.GetComponent<RectTransform>();
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
