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
using ReinoOscuridad.Systems;
using ReinoOscuridad.UI.Combat;

/// Editor Script — configura CombatScene.unity con todos los elementos de UI.
/// Menú: Tools → Reino Oscuridad → 5. Setup CombatScene
public static class SetupCombatScene
{
    private const string SCENE_PATH = "Assets/Scenes/CombatScene.unity";

    [MenuItem("Tools/Reino Oscuridad/5. Setup CombatScene")]
    public static void Run()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Abrir o crear la Scene
        if (File.Exists(SCENE_PATH))
            EditorSceneManager.OpenScene(SCENE_PATH);
        else
        {
            var s = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(s, SCENE_PATH);
        }

        // Limpiar objetos existentes
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            Object.DestroyImmediate(go);

        Build();

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[SetupCombatScene] CombatScene configurada correctamente.");
    }

    // ── Construcción principal ─────────────────────────────────────────────

    private static void Build()
    {
        // ── PASO 1 — Main Camera ──────────────────────────────────────────

        var camGO  = new GameObject("Main Camera");
        camGO.tag  = "MainCamera";
        var cam    = camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor  = Hex("#0A0A14");
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0, 0, -10);

        // ── PASO 2 — EventSystem con InputSystemUIInputModule ─────────────

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        // ── PASO 3 — CombatCanvas ─────────────────────────────────────────

        var canvasGO = new GameObject("CombatCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── PASO 4 — Fondo de combate ─────────────────────────────────────

        var fondo = Child(canvasGO.transform, "FondoCombate");
        Anch(fondo, 0, 0, 1, 1);
        Img(fondo, Hex("#0A0A14"));

        // ── PASO 5 — Zona Enemigos ────────────────────────────────────────

        var zonaEne = Child(canvasGO.transform, "ZonaEnemigos");
        Anch(zonaEne, 0.05f, 0.55f, 0.95f, 0.90f);

        var enemySlotImages = new Image[3];
        var enemyHPFills    = new Image[3];
        var enemyHPTexts    = new TMP_Text[3];
        var enemySlotBtns   = new Button[3];

        float[] eMinX = { 0.05f, 0.36f, 0.67f };
        float[] eMaxX = { 0.33f, 0.64f, 0.95f };

        for (int i = 0; i < 3; i++)
        {
            var slot = Child(zonaEne.transform, $"EnemySlot_{i}");
            Anch(slot, eMinX[i], 0f, eMaxX[i], 1f);
            enemySlotImages[i] = Img(slot, Hex("#2D1A1A"));
            enemySlotBtns[i]   = slot.AddComponent<Button>();

            var label = Child(slot.transform, "Label");
            Anch(label, 0.02f, 0.02f, 0.98f, 0.98f);
            var txt = label.AddComponent<TextMeshProUGUI>();
            txt.text      = $"Enemigo {i + 1}";
            txt.fontSize  = 11f;
            txt.color     = Hex("#FF6B6B");
            txt.alignment = TextAlignmentOptions.Center;
        }

        // ── PASO 6 — Barras HP Enemigos ───────────────────────────────────

        var barrasEne = Child(canvasGO.transform, "BarrasHP_Enemigos");
        Anch(barrasEne, 0.05f, 0.90f, 0.95f, 0.95f);

        for (int i = 0; i < 3; i++)
        {
            var hpBar = Child(barrasEne.transform, $"HPBar_Enemy_{i}");
            Anch(hpBar, eMinX[i], 0f, eMaxX[i], 1f);

            var bg = Child(hpBar.transform, "Fondo");
            Anch(bg, 0f, 0f, 1f, 1f);
            Img(bg, Hex("#3D1A1A"));

            var fill = Child(hpBar.transform, "Relleno");
            Anch(fill, 0f, 0f, 1f, 1f);
            enemyHPFills[i] = Img(fill, Hex("#EF4444"));

            var hpTxtGO = Child(hpBar.transform, "HPText");
            Anch(hpTxtGO, 0f, 0f, 1f, 1f);
            var hpTxt = hpTxtGO.AddComponent<TextMeshProUGUI>();
            hpTxt.text      = "8000/8000";
            hpTxt.fontSize  = 9f;
            hpTxt.color     = Hex("#FCA5A5");
            hpTxt.alignment = TextAlignmentOptions.Center;
            enemyHPTexts[i] = hpTxt;
        }

        // ── PASO 7 — Zona Equipo (5 cartas de héroes) ────────────────────

        var zonaEquipo = Child(canvasGO.transform, "ZonaEquipo");
        Anch(zonaEquipo, 0.00f, 0.02f, 0.55f, 0.45f);

        var heroCards       = new Button[5];
        var heroNames       = new TMP_Text[5];
        var heroHPFills     = new Image[5];
        var turnIndicators  = new GameObject[5];

        for (int i = 0; i < 5; i++)
        {
            float minX = i * 0.20f;
            float maxX = i * 0.20f + 0.19f;

            var card = Child(zonaEquipo.transform, $"HeroCard_{i}");
            Anch(card, minX, 0f, maxX, 1f);
            Img(card, Hex("#1A1A2D"));
            heroCards[i] = card.AddComponent<Button>();

            // Portrait
            var portrait = Child(card.transform, "Portrait");
            Anch(portrait, 0.05f, 0.35f, 0.95f, 0.95f);
            Img(portrait, Hex("#2D2D4E"));

            // Nombre
            var nameGO = Child(card.transform, "NombreHero");
            Anch(nameGO, 0.02f, 0.22f, 0.98f, 0.35f);
            var nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
            nameTxt.text      = $"Héroe {i + 1}";
            nameTxt.fontSize  = 9f;
            nameTxt.color     = Hex("#E9D5FF");
            nameTxt.alignment = TextAlignmentOptions.Center;
            heroNames[i] = nameTxt;

            // HP bar
            var hpBarGO = Child(card.transform, "HPBar");
            Anch(hpBarGO, 0.05f, 0.10f, 0.95f, 0.20f);

            var hpBg = Child(hpBarGO.transform, "Fondo");
            Anch(hpBg, 0f, 0f, 1f, 1f);
            Img(hpBg, Hex("#1A1A2E"));

            var hpFill = Child(hpBarGO.transform, "Relleno");
            Anch(hpFill, 0f, 0f, 1f, 1f);
            heroHPFills[i] = Img(hpFill, Hex("#22C55E"));

            // Turn Indicator
            var indicator = Child(card.transform, "TurnIndicator");
            Anch(indicator, 0f, 0f, 1f, 0.05f);
            var indImg = Img(indicator, Hex("#FACC15"));
            indImg.color = new Color(indImg.color.r, indImg.color.g, indImg.color.b, 0f);
            turnIndicators[i] = indicator;
        }

        // ── PASO 8 — Panel de Habilidades ─────────────────────────────────

        var panelHab = Child(canvasGO.transform, "PanelHabilidades");
        Anch(panelHab, 0.55f, 0.02f, 0.95f, 0.45f);
        Img(panelHab, Hex("#0F0F1A"));

        var abilityButtons = new Button[3];

        // BtnHabilidad_0 — ocupa la mitad superior completa
        var btn0 = Child(panelHab.transform, "BtnHabilidad_0");
        Anch(btn0, 0.02f, 0.52f, 0.98f, 0.98f);
        Img(btn0, Hex("#1A1A2E"));
        abilityButtons[0] = btn0.AddComponent<Button>();
        AddLabel(btn0.transform, "Habilidad 1", 12f, Hex("#A855F7"));

        // BtnHabilidad_1 — mitad inferior izquierda
        var btn1 = Child(panelHab.transform, "BtnHabilidad_1");
        Anch(btn1, 0.02f, 0.02f, 0.48f, 0.48f);
        Img(btn1, Hex("#1A1A2E"));
        abilityButtons[1] = btn1.AddComponent<Button>();
        AddLabel(btn1.transform, "Habilidad 2", 11f, Hex("#A855F7"));

        // BtnHabilidad_2 — mitad inferior derecha
        var btn2 = Child(panelHab.transform, "BtnHabilidad_2");
        Anch(btn2, 0.52f, 0.02f, 0.98f, 0.48f);
        Img(btn2, Hex("#1A1A2E"));
        abilityButtons[2] = btn2.AddComponent<Button>();
        AddLabel(btn2.transform, "Habilidad 3", 11f, Hex("#A855F7"));

        // ── PASO 9 — Controles superiores ────────────────────────────────

        var controles = Child(canvasGO.transform, "ControlesCombate");
        Anch(controles, 0.00f, 0.91f, 1.00f, 1.00f);

        // BtnAuto
        var btnAutoGO = Child(controles.transform, "BtnAuto");
        Anch(btnAutoGO, 0.02f, 0.10f, 0.18f, 0.90f);
        Img(btnAutoGO, Hex("#1A2E1A"));
        var btnAuto = btnAutoGO.AddComponent<Button>();
        AddLabel(btnAutoGO.transform, "AUTO", 11f, Hex("#4ADE80"), bold: true);

        // BtnVelocidad
        var btnVelGO = Child(controles.transform, "BtnVelocidad");
        Anch(btnVelGO, 0.19f, 0.10f, 0.32f, 0.90f);
        Img(btnVelGO, Hex("#1A1A2E"));
        btnVelGO.AddComponent<Button>();
        AddLabel(btnVelGO.transform, "x1", 11f, Hex("#A855F7"), bold: true);

        // TurnoText
        var turnoGO = Child(controles.transform, "TurnoText");
        Anch(turnoGO, 0.35f, 0.10f, 0.65f, 0.90f);
        var turnoTxt = turnoGO.AddComponent<TextMeshProUGUI>();
        turnoTxt.text      = "Turno 1";
        turnoTxt.fontSize  = 12f;
        turnoTxt.color     = Hex("#E9D5FF");
        turnoTxt.alignment = TextAlignmentOptions.Center;

        // BtnHuir
        var btnHuirGO = Child(controles.transform, "BtnHuir");
        Anch(btnHuirGO, 0.82f, 0.10f, 0.98f, 0.90f);
        Img(btnHuirGO, Hex("#2E1A1A"));
        var btnHuir = btnHuirGO.AddComponent<Button>();
        AddLabel(btnHuirGO.transform, "Huir", 11f, Hex("#EF4444"));

        // ── PASO 10 — ResultPanel (inactivo por defecto) ──────────────────

        var resultGO = Child(canvasGO.transform, "ResultPanel");
        Anch(resultGO, 0.10f, 0.12f, 0.90f, 0.88f);
        Img(resultGO, Hex("#0A0A14"), alpha: 0.97f);
        resultGO.SetActive(false);

        // Título (VICTORIA / DERROTA)
        var titleGO = Child(resultGO.transform, "TituloResult");
        Anch(titleGO, 0.05f, 0.82f, 0.95f, 0.97f);
        var titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
        titleTxt.text      = "VICTORIA";
        titleTxt.fontSize  = 24f;
        titleTxt.color     = Hex("#E9D5FF");
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.fontStyle = FontStyles.Bold;

        // Grado
        var gradeGO = Child(resultGO.transform, "GradeText");
        Anch(gradeGO, 0.05f, 0.68f, 0.95f, 0.82f);
        var gradeTxt = gradeGO.AddComponent<TextMeshProUGUI>();
        gradeTxt.text      = "Grado: C";
        gradeTxt.fontSize  = 16f;
        gradeTxt.color     = Hex("#A855F7");
        gradeTxt.alignment = TextAlignmentOptions.Center;

        // XP
        var xpGO = Child(resultGO.transform, "XPText");
        Anch(xpGO, 0.05f, 0.55f, 0.95f, 0.68f);
        var xpTxt = xpGO.AddComponent<TextMeshProUGUI>();
        xpTxt.text      = "XP: +0";
        xpTxt.fontSize  = 14f;
        xpTxt.color     = Hex("#4ADE80");
        xpTxt.alignment = TextAlignmentOptions.Center;

        // Drops
        var dropsGO = Child(resultGO.transform, "DropsText");
        Anch(dropsGO, 0.05f, 0.20f, 0.95f, 0.55f);
        var dropsTxt = dropsGO.AddComponent<TextMeshProUGUI>();
        dropsTxt.text      = "Sin drops";
        dropsTxt.fontSize  = 12f;
        dropsTxt.color     = Hex("#9CA3AF");
        dropsTxt.alignment = TextAlignmentOptions.Center;

        // BtnContinuar
        var btnContGO = Child(resultGO.transform, "BtnContinuar");
        Anch(btnContGO, 0.20f, 0.05f, 0.80f, 0.18f);
        Img(btnContGO, Hex("#1A1A2E"));
        var btnContinuar = btnContGO.AddComponent<Button>();
        AddLabel(btnContGO.transform, "Continuar", 14f, Hex("#E9D5FF"));

        // ── PASO 11 — Systems (CombatSystem para esta Scene) ─────────────

        var systemsGO    = new GameObject("Systems");
        systemsGO.AddComponent<CombatSystem>();

        // ── PASO 12 — CombatSceneController con referencias ───────────────

        var ctrlGO     = new GameObject("CombatSceneController");
        var controller = ctrlGO.AddComponent<CombatSceneController>();

        // Wiring via SerializedObject
        var so = new UnityEditor.SerializedObject(controller);

        // Control Panel
        so.FindProperty("_turnoText").objectReferenceValue = turnoTxt;
        so.FindProperty("_btnAuto").objectReferenceValue   = btnAuto;
        so.FindProperty("_btnHuir").objectReferenceValue   = btnHuir;

        // Enemy arrays
        SetArray(so, "_enemySlots",    3, i => enemySlotImages[i]);
        SetArray(so, "_enemyHPFills",  3, i => enemyHPFills[i]);
        SetArray(so, "_enemyHPTexts",  3, i => (Object)enemyHPTexts[i]);
        SetArray(so, "_enemySlotBtns", 3, i => enemySlotBtns[i]);

        // Hero arrays
        SetArray(so, "_heroCards",      5, i => heroCards[i]);
        SetArray(so, "_heroNames",      5, i => (Object)heroNames[i]);
        SetArray(so, "_heroHPFills",    5, i => heroHPFills[i]);
        SetArray(so, "_turnIndicators", 5, i => turnIndicators[i]);

        // Ability buttons
        SetArray(so, "_abilityButtons", 3, i => abilityButtons[i]);

        // Result panel
        so.FindProperty("_resultPanel").objectReferenceValue      = resultGO;
        so.FindProperty("_resultTitleText").objectReferenceValue  = titleTxt;
        so.FindProperty("_resultGradeText").objectReferenceValue  = gradeTxt;
        so.FindProperty("_resultXPText").objectReferenceValue     = xpTxt;
        so.FindProperty("_resultDropsText").objectReferenceValue  = dropsTxt;
        so.FindProperty("_btnContinuar").objectReferenceValue     = btnContinuar;

        so.ApplyModifiedProperties();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static GameObject Child(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private static void Anch(GameObject go, float minX, float minY, float maxX, float maxY)
    {
        var rt      = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Image Img(GameObject go, Color color, float alpha = 1f)
    {
        var img   = go.AddComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, alpha);
        return img;
    }

    private static void AddLabel(Transform parent, string text, float fontSize, Color color, bool bold = false)
    {
        var go  = new GameObject("Label");
        go.transform.SetParent(parent, false);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text      = text;
        txt.fontSize  = fontSize;
        txt.color     = color;
        txt.alignment = TextAlignmentOptions.Center;
        if (bold) txt.fontStyle = FontStyles.Bold;
    }

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }

    private static void SetArray(UnityEditor.SerializedObject so, string propName, int count,
                                  System.Func<int, Object> getter)
    {
        var prop = so.FindProperty(propName);
        if (prop == null)
        {
            Debug.LogWarning($"[SetupCombatScene] Propiedad '{propName}' no encontrada.");
            return;
        }
        prop.arraySize = count;
        for (int i = 0; i < count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = getter(i);
    }
}
#endif
