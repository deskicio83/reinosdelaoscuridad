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

/// Editor Script — configura CombatScene.unity con sistema ATB.
/// Menú: Tools → Reino Oscuridad → 5. Setup CombatScene
///
/// LAYOUT (1280×720 landscape):
///   PanelControles  (x 0.00–1.00, y 0.91–1.00): controles full-width
///   ZonaEnemigos    (x 0.07–1.00, y 0.52–0.90): 3 slots enemigos con ATBBar
///   ZonaHabilidades (x 0.00–0.06, y 0.02–0.50): 3 círculos habilidad
///   ZonaEquipo      (x 0.07–0.70, y 0.02–0.50): 4 cartas héroe con ATBBar
///   ZonaConjuros    (x 0.71–1.00, y 0.02–0.50): 2×5 conjuros
public static class SetupCombatScene
{
    private const string SCENE_PATH = "Assets/Scenes/CombatScene.unity";

    [MenuItem("Tools/Reino Oscuridad/5. Setup CombatScene")]
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
        Debug.Log("[SetupCombat] CombatScene configurada — sistema ATB v3");
    }

    // ── Construcción ────────────────────────────────────────────────────────

    private static void Build()
    {
        // ── Main Camera ──────────────────────────────────────────────────────

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam   = camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor  = Hex("#0A0A14");
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0, 0, -10);

        // ── EventSystem ──────────────────────────────────────────────────────

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        // ── Canvas ───────────────────────────────────────────────────────────

        var canvasGO = new GameObject("CombatCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Fondo ────────────────────────────────────────────────────────────

        var fondo = Child(canvasGO.transform, "FondoCombate");
        Anch(fondo, 0, 0, 1, 1);
        Img(fondo, Hex("#0A0A14"));

        // ════════════════════════════════════════════════════════════════════
        // ZONA 1 — Panel Controles (full width, franja superior)
        // Anchors: x 0.00–1.00, y 0.91–1.00
        // ════════════════════════════════════════════════════════════════════

        var panelCtrl = Child(canvasGO.transform, "PanelControles");
        Anch(panelCtrl, 0.00f, 0.91f, 1.00f, 1.00f);
        Img(panelCtrl, Hex("#0D0D1A"), 0.93f);

        // TurnoLabel — izquierda
        var turnoLabelGO = Child(panelCtrl.transform, "TurnoLabel");
        Anch(turnoLabelGO, 0.01f, 0.10f, 0.25f, 0.90f);
        var turnoLabel = Txt(turnoLabelGO, "T1 ...", 9f, Hex("#9CA3AF"), TextAlignmentOptions.Left);

        // BtnVelocidad
        var btnVelGO = Child(panelCtrl.transform, "BtnVelocidad");
        Anch(btnVelGO, 0.26f, 0.10f, 0.44f, 0.90f);
        Img(btnVelGO, Hex("#1A1020"));
        var btnVelocidad = btnVelGO.AddComponent<Button>();
        Txt(Child(btnVelGO.transform, "Label"), "x1 >", 11f, Hex("#A855F7"), bold: true);

        // BtnModo
        var btnModoGO = Child(panelCtrl.transform, "BtnModo");
        Anch(btnModoGO, 0.45f, 0.10f, 0.62f, 0.90f);
        Img(btnModoGO, Hex("#166534"));
        var btnModo = btnModoGO.AddComponent<Button>();
        Txt(Child(btnModoGO.transform, "Label"), "AUTO", 11f, Hex("#4ADE80"), bold: true);

        // BtnPausa
        var btnPausaGO = Child(panelCtrl.transform, "BtnPausa");
        Anch(btnPausaGO, 0.63f, 0.10f, 0.74f, 0.90f);
        Img(btnPausaGO, Hex("#1A1020"));
        var btnPausa = btnPausaGO.AddComponent<Button>();
        Txt(Child(btnPausaGO.transform, "Label"), "||", 14f, Hex("#9CA3AF"));

        // BtnHuir
        var btnHuirGO = Child(panelCtrl.transform, "BtnHuir");
        Anch(btnHuirGO, 0.75f, 0.10f, 0.99f, 0.90f);
        Img(btnHuirGO, Hex("#2A0000"));
        var btnHuir = btnHuirGO.AddComponent<Button>();
        Txt(Child(btnHuirGO.transform, "Label"), "Huir", 10f, Hex("#F87171"));

        // ════════════════════════════════════════════════════════════════════
        // ZONA 2 — Enemigos (3 slots, centro superior)
        // Anchors: x 0.07–1.00, y 0.52–0.90
        // Estructura carta: CardHighlight → HPBar → HPText → Sprite → EfectosBar → ATBBar
        // ════════════════════════════════════════════════════════════════════

        var zonaEne = Child(canvasGO.transform, "ZonaEnemigos");
        Anch(zonaEne, 0.07f, 0.52f, 1.00f, 0.90f);

        var enemySlotGOs  = new GameObject[3];
        var enemyHPFills  = new Image[3];
        var enemyHPTexts  = new TMP_Text[3];
        var enemySlotBtns = new Button[3];
        var enemyATBUnits = new ATBUnit[3];

        for (int i = 0; i < 3; i++)
        {
            float xMin = i * 0.34f;
            float xMax = xMin + 0.32f;

            var slot = Child(zonaEne.transform, $"EnemySlot_{i}");
            Anch(slot, xMin, 0.03f, xMax, 0.97f);
            Img(slot, Hex("#1A0D0D"), 0.85f);
            enemySlotGOs[i]  = slot;
            enemySlotBtns[i] = slot.AddComponent<Button>();

            // CardHighlight — cubre toda la carta (alpha=0 por defecto)
            var highlight = Child(slot.transform, "CardHighlight");
            Anch(highlight, 0f, 0f, 1f, 1f);
            var hlImg = Img(highlight, Hex("#DC2626"), 0f);
            hlImg.raycastTarget = false;

            // HP bar — TOP (y 0.83–0.95)
            var hpBar = Child(slot.transform, "HPBar_Enemy");
            Anch(hpBar, 0.06f, 0.83f, 0.94f, 0.95f);
            var hpBg = Child(hpBar.transform, "Fondo");
            Anch(hpBg, 0, 0, 1, 1);
            Img(hpBg, Hex("#3D1A1A"));
            var hpFill = Child(hpBar.transform, "Relleno");
            Anch(hpFill, 0, 0, 1, 1);
            enemyHPFills[i] = Img(hpFill, Hex("#EF4444"));

            // HP text (y 0.71–0.82)
            var hpTxtGO = Child(slot.transform, "HPText");
            Anch(hpTxtGO, 0.04f, 0.71f, 0.96f, 0.82f);
            enemyHPTexts[i] = Txt(hpTxtGO, "8000/8000", 7f, Hex("#FCA5A5"));

            // Sprite enemigo (y 0.22–0.70)
            var sprite = Child(slot.transform, "EnemySprite");
            Anch(sprite, 0.10f, 0.22f, 0.90f, 0.70f);
            Img(sprite, Hex("#2D1A1A"));

            // EfectosBar — sobre ATBBar (y 0.09–0.20)
            var efBar = Child(slot.transform, "EfectosBar");
            Anch(efBar, 0.06f, 0.09f, 0.94f, 0.20f);
            Img(efBar, Hex("#1A0A0A"), 0.70f);

            // ATBBar — BOTTOM de la carta (y 0.00–0.08)
            var atbBar = Child(slot.transform, "ATBBar");
            Anch(atbBar, 0.05f, 0.00f, 0.95f, 0.08f);
            Img(atbBar, Hex("#1A0A1A"));
            var atbRelleno = Child(atbBar.transform, "ATBRelleno");
            Anch(atbRelleno, 0, 0, 1, 1);
            var atbRellenoImg = Img(atbRelleno, Hex("#A855F7"));
            atbRellenoImg.type       = Image.Type.Filled;
            atbRellenoImg.fillMethod = Image.FillMethod.Horizontal;
            atbRellenoImg.fillAmount = 0f;
            // ATBPercent — texto "0%" sobre la barra
            var atbPercentGO = Child(atbBar.transform, "ATBPercent");
            Anch(atbPercentGO, 0.02f, 0f, 0.98f, 1f);
            var atbPercentTxt = Txt(atbPercentGO, "0%", 7f, Hex("#E9D5FF"), bold: true);
            atbPercentTxt.raycastTarget = false;

            // ATBUnit component
            var unit = slot.AddComponent<ATBUnit>();
            var unitSO = new SerializedObject(unit);
            unitSO.FindProperty("_atbBarRelleno").objectReferenceValue = atbRellenoImg;
            unitSO.FindProperty("_cardHighlight").objectReferenceValue = hlImg;
            unitSO.FindProperty("_atbPercent").objectReferenceValue    = atbPercentTxt;
            unitSO.ApplyModifiedProperties();
            enemyATBUnits[i] = unit;
        }

        // ════════════════════════════════════════════════════════════════════
        // ZONA 3 — Habilidades activas (3 círculos, columna izquierda)
        // Anchors: x 0.00–0.06, y 0.02–0.50
        // ════════════════════════════════════════════════════════════════════

        var zonaHab = Child(canvasGO.transform, "ZonaHabilidades");
        Anch(zonaHab, 0.00f, 0.02f, 0.06f, 0.50f);
        Img(zonaHab, Hex("#0D0D1A"), 0.85f);

        var habLabel = Child(zonaHab.transform, "HabLabel");
        Anch(habLabel, 0.05f, 0.93f, 0.95f, 0.99f);
        Txt(habLabel, "HAB", 7f, Hex("#A855F7"));

        float[] habYMin = { 0.04f, 0.36f, 0.67f };
        float[] habYMax = { 0.32f, 0.63f, 0.91f };
        var abilityCircleBtns = new Button[3];

        for (int i = 0; i < 3; i++)
        {
            var circleGO = Child(zonaHab.transform, $"BtnHab_{i}");
            Anch(circleGO, 0.10f, habYMin[i], 0.90f, habYMax[i]);
            Img(circleGO, Hex("#1A1020"));
            abilityCircleBtns[i] = circleGO.AddComponent<Button>();
            var lbl = Child(circleGO.transform, "Label");
            Anch(lbl, 0, 0, 1, 1);
            Txt(lbl, $"H{i + 1}", 9f, Hex("#A855F7"), bold: true);
        }

        // ════════════════════════════════════════════════════════════════════
        // ZONA 4 — Equipo jugador (4 cartas, inferior centro)
        // Anchors: x 0.07–0.70, y 0.02–0.50
        // Estructura carta: CardHighlight → EfectosBar → Portrait → NombreHero → HPBar → ATBBar
        // ════════════════════════════════════════════════════════════════════

        var zonaEquipo = Child(canvasGO.transform, "ZonaEquipo");
        Anch(zonaEquipo, 0.07f, 0.02f, 0.70f, 0.50f);

        var heroCards   = new Button[4];
        var heroNames   = new TMP_Text[4];
        var heroHPFills = new Image[4];
        var heroATBUnits = new ATBUnit[4];

        for (int i = 0; i < 4; i++)
        {
            float xMin = i * 0.25f;
            float xMax = xMin + 0.24f;

            var card = Child(zonaEquipo.transform, $"HeroCard_{i}");
            Anch(card, xMin, 0.02f, xMax, 0.98f);
            Img(card, Hex("#1A1A2D"));
            heroCards[i] = card.AddComponent<Button>();

            // CardHighlight — cubre toda la carta (alpha=0 por defecto)
            var highlight = Child(card.transform, "CardHighlight");
            Anch(highlight, 0f, 0f, 1f, 1f);
            var hlImg = Img(highlight, Hex("#7C3AED"), 0f);
            hlImg.raycastTarget = false;

            // EfectosBar (y 0.80–0.93)
            var efBar = Child(card.transform, "EfectosBar");
            Anch(efBar, 0.04f, 0.80f, 0.96f, 0.93f);
            Img(efBar, Hex("#0D0D1E"), 0.70f);

            // Portrait (y 0.38–0.78)
            var portrait = Child(card.transform, "Portrait");
            Anch(portrait, 0.06f, 0.38f, 0.94f, 0.78f);
            Img(portrait, Hex("#2D2D4E"));

            // Nombre (y 0.26–0.37)
            var nameGO = Child(card.transform, "NombreHero");
            Anch(nameGO, 0.02f, 0.26f, 0.98f, 0.37f);
            heroNames[i] = Txt(nameGO, $"Heroe {i + 1}", 8f, Hex("#E9D5FF"));

            // HP bar (y 0.08–0.24)
            var hpBar = Child(card.transform, "HPBar");
            Anch(hpBar, 0.06f, 0.08f, 0.94f, 0.24f);
            var hpBg = Child(hpBar.transform, "Fondo");
            Anch(hpBg, 0, 0, 1, 1);
            Img(hpBg, Hex("#1A1A2E"));
            var hpFill = Child(hpBar.transform, "Relleno");
            Anch(hpFill, 0, 0, 1, 1);
            heroHPFills[i] = Img(hpFill, Hex("#22C55E"));

            // ATBBar — BOTTOM de la carta (y 0.00–0.06)
            var atbBar = Child(card.transform, "ATBBar");
            Anch(atbBar, 0.05f, 0.00f, 0.95f, 0.06f);
            Img(atbBar, Hex("#0D0A1A"));
            var atbRelleno = Child(atbBar.transform, "ATBRelleno");
            Anch(atbRelleno, 0, 0, 1, 1);
            var atbRellenoImg = Img(atbRelleno, Hex("#7C3AED"));
            atbRellenoImg.type       = Image.Type.Filled;
            atbRellenoImg.fillMethod = Image.FillMethod.Horizontal;
            atbRellenoImg.fillAmount = 0f;
            // ATBPercent — texto "0%" sobre la barra
            var heroAtbPercentGO = Child(atbBar.transform, "ATBPercent");
            Anch(heroAtbPercentGO, 0.02f, 0f, 0.98f, 1f);
            var heroAtbPercentTxt = Txt(heroAtbPercentGO, "0%", 7f, Hex("#E9D5FF"), bold: true);
            heroAtbPercentTxt.raycastTarget = false;

            // ATBUnit component
            var unit = card.AddComponent<ATBUnit>();
            var unitSO = new SerializedObject(unit);
            unitSO.FindProperty("_atbBarRelleno").objectReferenceValue = atbRellenoImg;
            unitSO.FindProperty("_cardHighlight").objectReferenceValue = hlImg;
            unitSO.FindProperty("_atbPercent").objectReferenceValue    = heroAtbPercentTxt;
            unitSO.ApplyModifiedProperties();
            heroATBUnits[i] = unit;
        }

        // ════════════════════════════════════════════════════════════════════
        // ZONA 5 — Conjuros (inferior derecha, 2×5)
        // Anchors: x 0.71–1.00, y 0.02–0.50
        // ════════════════════════════════════════════════════════════════════

        var zonaConj = Child(canvasGO.transform, "ZonaConjuros");
        Anch(zonaConj, 0.71f, 0.02f, 1.00f, 0.50f);
        Img(zonaConj, Hex("#0D0D1A"), 0.78f);

        var conjuroSlots = new Button[10];

        for (int fila = 0; fila < 2; fila++)
        {
            for (int col = 0; col < 5; col++)
            {
                int   idx  = fila * 5 + col;
                float xMin = col  * 0.19f + 0.02f;
                float xMax = col  * 0.19f + 0.20f;
                float yMin = (1 - fila) * 0.50f + 0.02f;
                float yMax = (1 - fila) * 0.50f + 0.47f;

                var slot = Child(zonaConj.transform, $"ConjuroSlot_{idx}");
                Anch(slot, xMin, yMin, xMax, yMax);
                Img(slot, Hex("#1A1020"));
                conjuroSlots[idx] = slot.AddComponent<Button>();

                var icono = Child(slot.transform, "ConjuroIcono");
                Anch(icono, 0.05f, 0.32f, 0.95f, 0.95f);
                Img(icono, Hex("#2D1A3D"));

                var nombre = Child(slot.transform, "ConjuroNombre");
                Anch(nombre, 0.02f, 0.02f, 0.98f, 0.30f);
                Txt(nombre, "—", 6f, Hex("#9CA3AF"));
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // TOOLTIP PANEL (compartido: habilidades + conjuros)
        // ════════════════════════════════════════════════════════════════════

        var tooltipGO = Child(canvasGO.transform, "TooltipPanel");
        Anch(tooltipGO, 0.15f, 0.28f, 0.85f, 0.72f);
        var tooltipImg = Img(tooltipGO, Hex("#1A0F2E"));
        tooltipImg.color = new Color(tooltipImg.color.r, tooltipImg.color.g, tooltipImg.color.b, 0.97f);
        tooltipGO.SetActive(false);

        var tooltipBorder = Child(tooltipGO.transform, "Border");
        Anch(tooltipBorder, 0.01f, 0.02f, 0.99f, 0.98f);
        Img(tooltipBorder, Hex("#A855F7"), 0.25f);

        var tooltipTituloGO = Child(tooltipGO.transform, "TooltipTitulo");
        Anch(tooltipTituloGO, 0.05f, 0.78f, 0.85f, 0.95f);
        Txt(tooltipTituloGO, "Descripcion", 12f, Hex("#A855F7"), bold: true);

        var tooltipTxtGO = Child(tooltipGO.transform, "TooltipText");
        Anch(tooltipTxtGO, 0.05f, 0.28f, 0.95f, 0.76f);
        var tooltipTxt = Txt(tooltipTxtGO, "Descripcion de la habilidad...", 11f, Hex("#E9D5FF"),
                             align: TextAlignmentOptions.Left);

        var btnUsarGO = Child(tooltipGO.transform, "BtnUsar");
        Anch(btnUsarGO, 0.05f, 0.06f, 0.48f, 0.24f);
        Img(btnUsarGO, Hex("#4C1D95"));
        var btnUsarTooltip = btnUsarGO.AddComponent<Button>();
        Txt(Child(btnUsarGO.transform, "Label"), "Usar", 12f, Hex("#E9D5FF"), bold: true);

        var btnCerrarGO = Child(tooltipGO.transform, "BtnCerrar");
        Anch(btnCerrarGO, 0.52f, 0.06f, 0.95f, 0.24f);
        Img(btnCerrarGO, Hex("#2A0000"));
        var btnCerrarTooltip = btnCerrarGO.AddComponent<Button>();
        Txt(Child(btnCerrarGO.transform, "Label"), "Cerrar", 12f, Hex("#F87171"), bold: true);

        // ════════════════════════════════════════════════════════════════════
        // ZONA 6 — ResultPanel (overlay, inactivo)
        // ════════════════════════════════════════════════════════════════════

        var resultGO = Child(canvasGO.transform, "ResultPanel");
        Anch(resultGO, 0.10f, 0.12f, 0.90f, 0.88f);
        Img(resultGO, Hex("#0F0F1A"));
        resultGO.SetActive(false);

        var tituloGO = Child(resultGO.transform, "TituloResultado");
        Anch(tituloGO, 0.05f, 0.78f, 0.95f, 0.96f);
        var tituloTxt = Txt(tituloGO, "VICTORIA", 28f, Hex("#E9D5FF"), bold: true);

        var estrellasRow = Child(resultGO.transform, "EstrellasFila");
        Anch(estrellasRow, 0.20f, 0.65f, 0.80f, 0.78f);
        var estHLG = estrellasRow.AddComponent<HorizontalLayoutGroup>();
        estHLG.childForceExpandWidth  = false;
        estHLG.childForceExpandHeight = false;
        estHLG.spacing        = 8f;
        estHLG.childAlignment = TextAnchor.MiddleCenter;

        var resultStarImages = new Image[3];
        for (int si = 0; si < 3; si++)
        {
            var star = Child(estrellasRow.transform, $"Star_{si}");
            var le   = star.AddComponent<LayoutElement>();
            le.preferredWidth  = 32f;
            le.preferredHeight = 32f;
            resultStarImages[si] = Img(star, Hex("#FACC15"));
        }

        var xpGO = Child(resultGO.transform, "XPGanada");
        Anch(xpGO, 0.05f, 0.55f, 0.95f, 0.65f);
        var xpTxt = Txt(xpGO, "+500 XP", 14f, Hex("#A855F7"));

        var dropsGO = Child(resultGO.transform, "DropsList");
        Anch(dropsGO, 0.05f, 0.28f, 0.95f, 0.54f);
        var dropsTxt = Txt(dropsGO, "Recompensas:\n* Item 1", 11f, Hex("#E9D5FF"),
                           align: TextAlignmentOptions.Left);

        var btnReinGO = Child(resultGO.transform, "BtnReintentar");
        Anch(btnReinGO, 0.05f, 0.08f, 0.45f, 0.24f);
        Img(btnReinGO, Hex("#1A1A2E"));
        var btnReintentar = btnReinGO.AddComponent<Button>();
        Txt(Child(btnReinGO.transform, "Label"), "Reintentar", 13f, Hex("#9CA3AF"), bold: true);

        var btnContGO = Child(resultGO.transform, "BtnContinuar");
        Anch(btnContGO, 0.55f, 0.08f, 0.95f, 0.24f);
        Img(btnContGO, Hex("#4C1D95"));
        var btnContinuar = btnContGO.AddComponent<Button>();
        Txt(Child(btnContGO.transform, "Label"), "Continuar", 13f, Hex("#E9D5FF"), bold: true);

        // ── AbilityTooltip ────────────────────────────────────────────────

        BuildAbilityTooltip(canvasGO.transform);

        // ── CombatSystem ──────────────────────────────────────────────────

        new GameObject("Systems").AddComponent<CombatSystem>();

        // ── CombatSceneController + wiring ────────────────────────────────

        var ctrlGO     = new GameObject("CombatSceneController");
        var controller = ctrlGO.AddComponent<CombatSceneController>();
        var so         = new SerializedObject(controller);

        // ATB Units — héroes y enemigos
        var heroUnitsProp = so.FindProperty("_heroUnits");
        heroUnitsProp.arraySize = 4;
        for (int i = 0; i < 4; i++)
            heroUnitsProp.GetArrayElementAtIndex(i).objectReferenceValue = heroATBUnits[i];

        var enemyUnitsProp = so.FindProperty("_enemyUnits");
        enemyUnitsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            enemyUnitsProp.GetArrayElementAtIndex(i).objectReferenceValue = enemyATBUnits[i];

        // Panel Controles
        so.FindProperty("_turnoLabel").objectReferenceValue   = turnoLabel;
        so.FindProperty("_btnModo").objectReferenceValue      = btnModo;
        so.FindProperty("_btnVelocidad").objectReferenceValue = btnVelocidad;
        so.FindProperty("_btnPausa").objectReferenceValue     = btnPausa;
        so.FindProperty("_btnHuir").objectReferenceValue      = btnHuir;

        // Zona Enemigos
        SetObjArray(so, "_enemySlots",    3, i => enemySlotGOs[i]);
        SetArray   (so, "_enemyHPFills",  3, i => enemyHPFills[i]);
        SetTxtArray(so, "_enemyHPTexts",  3, i => enemyHPTexts[i]);
        SetArray   (so, "_enemySlotBtns", 3, i => enemySlotBtns[i]);

        // Zona Equipo
        SetArray   (so, "_heroCards",   4, i => heroCards[i]);
        SetTxtArray(so, "_heroNames",   4, i => heroNames[i]);
        SetArray   (so, "_heroHPFills", 4, i => heroHPFills[i]);

        // Zona Habilidades
        SetArray(so, "_abilityCircles", 3, i => abilityCircleBtns[i]);

        // Zona Conjuros
        SetArray(so, "_conjuroSlots", 10, i => conjuroSlots[i]);

        // Tooltip
        so.FindProperty("_tooltipPanel").objectReferenceValue     = tooltipGO;
        so.FindProperty("_tooltipText").objectReferenceValue      = tooltipTxt;
        so.FindProperty("_btnUsarTooltip").objectReferenceValue   = btnUsarTooltip;
        so.FindProperty("_btnCerrarTooltip").objectReferenceValue = btnCerrarTooltip;

        // Result Panel
        so.FindProperty("_resultPanel").objectReferenceValue     = resultGO;
        so.FindProperty("_resultTitleText").objectReferenceValue = tituloTxt;
        var resultStarsProp = so.FindProperty("_resultStarImages");
        resultStarsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            resultStarsProp.GetArrayElementAtIndex(i).objectReferenceValue = resultStarImages[i];
        so.FindProperty("_resultXPText").objectReferenceValue    = xpTxt;
        so.FindProperty("_resultDropsText").objectReferenceValue = dropsTxt;
        so.FindProperty("_btnContinuar").objectReferenceValue    = btnContinuar;
        so.FindProperty("_btnReintentar").objectReferenceValue   = btnReintentar;

        so.ApplyModifiedProperties();
    }

    // ── AbilityTooltip panel ─────────────────────────────────────────────────

    private static void BuildAbilityTooltip(Transform canvasTransform)
    {
        // Holder — child of canvas, carries the AbilityTooltip MonoBehaviour.
        // Size = 0; GetComponentInParent<Canvas>() in Start() finds canvas correctly.
        var holderGO = Child(canvasTransform, "AbilityTooltipHolder");
        var holderRT  = holderGO.GetComponent<RectTransform>();
        holderRT.anchorMin = Vector2.zero;
        holderRT.anchorMax = Vector2.zero;
        holderRT.sizeDelta = Vector2.zero;

        // Panel — separate child of canvas, positioned dynamically by Show().
        // Anchor=center, pivot=bottom-left so anchoredPosition matches local canvas coords.
        var panelGO = Child(canvasTransform, "AbilityTooltipPanel");
        var panelRT  = panelGO.GetComponent<RectTransform>();
        panelRT.anchorMin        = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax        = new Vector2(0.5f, 0.5f);
        panelRT.pivot            = new Vector2(0f,   0f);
        panelRT.sizeDelta        = new Vector2(190f, 115f);
        panelRT.anchoredPosition = Vector2.zero;
        Img(panelGO, Hex("#0D0D1A"), 0.97f);
        panelGO.SetActive(false);

        var border = Child(panelGO.transform, "Border");
        Anch(border, 0f, 0f, 1f, 1f);
        var borderImg = Img(border, Hex("#A855F7"), 0.30f);
        borderImg.raycastTarget = false;

        var nombreGO = Child(panelGO.transform, "NombreHabilidad");
        Anch(nombreGO, 0.05f, 0.75f, 0.95f, 0.95f);
        var txtNombre = Txt(nombreGO, "Habilidad", 13f, Hex("#E9D5FF"), bold: true);

        var descGO = Child(panelGO.transform, "DescripcionHabilidad");
        Anch(descGO, 0.05f, 0.30f, 0.95f, 0.72f);
        var txtDesc = Txt(descGO, "Descripcion", 11f, Hex("#9CA3AF"),
                          align: TextAlignmentOptions.Left);

        var cdGO = Child(panelGO.transform, "CooldownText");
        Anch(cdGO, 0.05f, 0.05f, 0.95f, 0.28f);
        var txtCooldown = Txt(cdGO, "Sin cooldown", 10f, Hex("#FACC15"));

        // Wire AbilityTooltip serialized fields
        var tooltip   = holderGO.AddComponent<AbilityTooltip>();
        var tooltipSO = new SerializedObject(tooltip);
        tooltipSO.FindProperty("_panel").objectReferenceValue           = panelRT;
        tooltipSO.FindProperty("_txtNombre").objectReferenceValue       = txtNombre;
        tooltipSO.FindProperty("_txtDescripcion").objectReferenceValue  = txtDesc;
        tooltipSO.FindProperty("_txtCooldown").objectReferenceValue     = txtCooldown;
        tooltipSO.ApplyModifiedProperties();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

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

    private static void SetArray(SerializedObject so, string prop, int n,
                                  System.Func<int, Component> get)
    {
        var p = so.FindProperty(prop);
        if (p == null) { Debug.LogWarning($"[SetupCombat] prop '{prop}' no encontrada"); return; }
        p.arraySize = n;
        for (int i = 0; i < n; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = get(i);
    }

    private static void SetObjArray(SerializedObject so, string prop, int n,
                                     System.Func<int, GameObject> get)
    {
        var p = so.FindProperty(prop);
        if (p == null) { Debug.LogWarning($"[SetupCombat] prop '{prop}' no encontrada"); return; }
        p.arraySize = n;
        for (int i = 0; i < n; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = get(i);
    }

    private static void SetTxtArray(SerializedObject so, string prop, int n,
                                     System.Func<int, TMP_Text> get)
    {
        var p = so.FindProperty(prop);
        if (p == null) { Debug.LogWarning($"[SetupCombat] prop '{prop}' no encontrada"); return; }
        p.arraySize = n;
        for (int i = 0; i < n; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = get(i);
    }
}
#endif
