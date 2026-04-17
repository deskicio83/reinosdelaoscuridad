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

/// Editor Script — configura CombatScene.unity con layout corregido de 6 zonas.
/// Menú: Tools → Reino Oscuridad → 5. Setup CombatScene
///
/// LAYOUT (1280×720 landscape):
///   Col izq (x 0.00–0.06): BarraOrdenTurno (y 0.40–0.95) + ZonaHabilidades (y 0.03–0.37)
///   Centro-arriba  (x 0.07–0.88, y 0.55–0.95): ZonaEnemigos
///   Centro-abajo   (x 0.07–0.65, y 0.03–0.47): ZonaEquipo
///   Derecha-abajo  (x 0.66–0.99, y 0.03–0.43): ZonaConjuros (reducida)
///   Derecha-arriba (x 0.88–1.00, y 0.80–1.00): PanelControles
///   Gap visible    (y 0.47–0.55 ≈ 58 px)
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
        Debug.Log("[SetupCombat] CombatScene configurada — layout correcto 6 zonas v2");
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
        cam.backgroundColor  = Hex("#0A0A14");
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0, 0, -10);

        // ── EventSystem ───────────────────────────────────────────────────

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        // ── Canvas ────────────────────────────────────────────────────────

        var canvasGO = new GameObject("CombatCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Fondo ─────────────────────────────────────────────────────────

        Img(Child(canvasGO.transform, "FondoCombate"), Hex("#0A0A14"));
        Anch(canvasGO.transform.Find("FondoCombate").gameObject, 0, 0, 1, 1);

        // ═══════════════════════════════════════════════════════════════════
        // ZONA 1 — Barra orden de turno (lateral izq, acortada)
        // Anchors: x 0.00–0.06, y 0.40–0.95
        // ═══════════════════════════════════════════════════════════════════

        var barraOrden = Child(canvasGO.transform, "BarraOrdenTurno");
        Anch(barraOrden, 0.00f, 0.40f, 0.06f, 0.95f);
        Img(barraOrden, Hex("#0D0D1A"), 0.90f);

        var ordenLabel = Child(barraOrden.transform, "OrdenLabel");
        Anch(ordenLabel, 0.05f, 0.93f, 0.95f, 0.99f);
        Txt(ordenLabel, "Orden", 7f, Hex("#6B7280"));

        var listaRetratos = Child(barraOrden.transform, "ListaRetratos");
        Anch(listaRetratos, 0.05f, 0.02f, 0.95f, 0.92f);

        // 7 retratos placeholder (4 jugador azul-oscuro + 3 enemigo rojo-oscuro)
        Color[] retratoCols =
        {
            Hex("#1E1535"), Hex("#1E1535"), Hex("#1E1535"), Hex("#1E1535"),
            Hex("#2A0010"), Hex("#2A0010"), Hex("#2A0010")
        };
        for (int i = 0; i < 7; i++)
        {
            float yMax = 1f - i * 0.13f;
            float yMin = yMax - 0.11f;
            var r = Child(listaRetratos.transform, $"Retrato_{i}");
            Anch(r, 0.10f, yMin, 0.90f, yMax);
            Img(r, retratoCols[i]);
        }

        // ═══════════════════════════════════════════════════════════════════
        // ZONA 1b — Habilidades activas (3 círculos bajo barra de turno)
        // Anchors: x 0.00–0.06, y 0.03–0.37
        // El círculo seleccionado llama SelectAbility(i) tras confirmar tooltip
        // ═══════════════════════════════════════════════════════════════════

        var zonaHab = Child(canvasGO.transform, "ZonaHabilidades");
        Anch(zonaHab, 0.00f, 0.03f, 0.06f, 0.37f);
        Img(zonaHab, Hex("#0D0D1A"), 0.85f);

        var habLabel = Child(zonaHab.transform, "HabLabel");
        Anch(habLabel, 0.05f, 0.93f, 0.95f, 0.99f);
        Txt(habLabel, "HAB", 7f, Hex("#A855F7"));

        // Posiciones verticales para 3 círculos con margen
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

        // ═══════════════════════════════════════════════════════════════════
        // ZONA 2 — Área de enemigos (centro superior)
        // Anchors: x 0.07–0.88, y 0.55–0.95
        // Tarjeta: HP arriba → Sprite centro → EfectosBar abajo
        // ═══════════════════════════════════════════════════════════════════

        var zonaEne = Child(canvasGO.transform, "ZonaEnemigos");
        Anch(zonaEne, 0.07f, 0.55f, 0.88f, 0.95f);

        var enemySlotGOs  = new GameObject[3];
        var enemyHPFills  = new Image[3];
        var enemyHPTexts  = new TMP_Text[3];
        var enemySlotBtns = new Button[3];

        for (int i = 0; i < 3; i++)
        {
            float xMin = i * 0.34f;
            float xMax = xMin + 0.32f;

            var slot = Child(zonaEne.transform, $"EnemySlot_{i}");
            Anch(slot, xMin, 0.03f, xMax, 0.97f);
            Img(slot, Hex("#1A0D0D"), 0.85f);
            enemySlotGOs[i] = slot;

            // Button para selección de objetivo cubre toda la carta
            enemySlotBtns[i] = slot.AddComponent<Button>();

            // HP bar — TOP de la carta (y 0.83–0.95)
            var hpBar = Child(slot.transform, "HPBar_Enemy");
            Anch(hpBar, 0.06f, 0.83f, 0.94f, 0.95f);

            var hpBg = Child(hpBar.transform, "Fondo");
            Anch(hpBg, 0, 0, 1, 1);
            Img(hpBg, Hex("#3D1A1A"));

            var hpFill = Child(hpBar.transform, "Relleno");
            Anch(hpFill, 0, 0, 1, 1);
            enemyHPFills[i] = Img(hpFill, Hex("#EF4444"));

            // HP text — bajo la barra (y 0.71–0.82)
            var hpTxtGO = Child(slot.transform, "HPText");
            Anch(hpTxtGO, 0.04f, 0.71f, 0.96f, 0.82f);
            enemyHPTexts[i] = Txt(hpTxtGO, "8000/8000", 7f, Hex("#FCA5A5"));

            // Sprite enemigo — centro (y 0.20–0.70)
            var sprite = Child(slot.transform, "EnemySprite");
            Anch(sprite, 0.10f, 0.20f, 0.90f, 0.70f);
            Img(sprite, Hex("#2D1A1A"));

            // Efectos/buff bar — BOTTOM de la carta (y 0.04–0.17)
            var efBar = Child(slot.transform, "EfectosBar");
            Anch(efBar, 0.06f, 0.04f, 0.94f, 0.17f);
            Img(efBar, Hex("#1A0A0A"), 0.70f);
        }

        // ═══════════════════════════════════════════════════════════════════
        // ZONA 3 — Controles (esquina superior derecha)
        // ═══════════════════════════════════════════════════════════════════

        var panelCtrl = Child(canvasGO.transform, "PanelControles");
        Anch(panelCtrl, 0.88f, 0.80f, 1.00f, 1.00f);
        Img(panelCtrl, Hex("#0D0D1A"), 0.93f);

        // Turno label
        var turnoLabelGO = Child(panelCtrl.transform, "TurnoLabel");
        Anch(turnoLabelGO, 0.05f, 0.82f, 0.95f, 0.98f);
        var turnoLabel = Txt(turnoLabelGO, "Turno 1", 9f, Hex("#9CA3AF"), TextAlignmentOptions.Right);

        // BtnVelocidad — "x1 >" (ASCII, LiberationSans)
        var btnVelGO = Child(panelCtrl.transform, "BtnVelocidad");
        Anch(btnVelGO, 0.04f, 0.54f, 0.96f, 0.80f);
        Img(btnVelGO, Hex("#1A1020"));
        var btnVelocidad = btnVelGO.AddComponent<Button>();
        Txt(Child(btnVelGO.transform, "Label"), "x1 >", 11f, Hex("#A855F7"), bold: true);

        // BtnModo
        var btnModoGO = Child(panelCtrl.transform, "BtnModo");
        Anch(btnModoGO, 0.04f, 0.28f, 0.96f, 0.52f);
        Img(btnModoGO, Hex("#166534"));
        var btnModo = btnModoGO.AddComponent<Button>();
        Txt(Child(btnModoGO.transform, "Label"), "AUTO", 11f, Hex("#4ADE80"), bold: true);

        // BtnPausa — "||" (ASCII)
        var btnPausaGO = Child(panelCtrl.transform, "BtnPausa");
        Anch(btnPausaGO, 0.04f, 0.14f, 0.48f, 0.26f);
        Img(btnPausaGO, Hex("#1A1020"));
        var btnPausa = btnPausaGO.AddComponent<Button>();
        Txt(Child(btnPausaGO.transform, "Label"), "||", 14f, Hex("#9CA3AF"));

        // BtnHuir
        var btnHuirGO = Child(panelCtrl.transform, "BtnHuir");
        Anch(btnHuirGO, 0.04f, 0.02f, 0.96f, 0.12f);
        Img(btnHuirGO, Hex("#2A0000"));
        var btnHuir = btnHuirGO.AddComponent<Button>();
        Txt(Child(btnHuirGO.transform, "Label"), "Huir", 10f, Hex("#F87171"));

        // ═══════════════════════════════════════════════════════════════════
        // ZONA 4 — Equipo jugador (inferior izq, 4 slots)
        // Anchors: x 0.07–0.65, y 0.03–0.47
        // Tarjeta: TurnIndicator arriba → EfectosBar → Portrait → Nombre → HPBar abajo
        // ═══════════════════════════════════════════════════════════════════

        var zonaEquipo = Child(canvasGO.transform, "ZonaEquipo");
        Anch(zonaEquipo, 0.07f, 0.03f, 0.65f, 0.47f);

        var heroCards      = new Button[4];
        var heroNames      = new TMP_Text[4];
        var heroHPFills    = new Image[4];
        var turnIndicators = new Image[4];

        for (int i = 0; i < 4; i++)
        {
            float xMin = i * 0.25f;
            float xMax = xMin + 0.24f;

            var card = Child(zonaEquipo.transform, $"HeroCard_{i}");
            Anch(card, xMin, 0.02f, xMax, 0.98f);
            Img(card, Hex("#1A1A2D"));
            heroCards[i] = card.AddComponent<Button>();

            // TurnIndicator — barra dorada en TOP (y 0.95–1.00), alpha=0 por defecto
            var indGO = Child(card.transform, "TurnIndicator");
            Anch(indGO, 0f, 0.95f, 1f, 1.00f);
            var indImg = Img(indGO, Hex("#FACC15"));
            indImg.color = new Color(indImg.color.r, indImg.color.g, indImg.color.b, 0f);
            turnIndicators[i] = indImg;

            // EfectosBar — justo bajo TurnIndicator, TOP de la carta (y 0.80–0.93)
            var efBar = Child(card.transform, "EfectosBar");
            Anch(efBar, 0.04f, 0.80f, 0.96f, 0.93f);
            Img(efBar, Hex("#0D0D1E"), 0.70f);

            // Portrait — centro (y 0.38–0.78)
            var portrait = Child(card.transform, "Portrait");
            Anch(portrait, 0.06f, 0.38f, 0.94f, 0.78f);
            Img(portrait, Hex("#2D2D4E"));

            // Nombre — bajo portrait (y 0.26–0.37)
            var nameGO = Child(card.transform, "NombreHero");
            Anch(nameGO, 0.02f, 0.26f, 0.98f, 0.37f);
            heroNames[i] = Txt(nameGO, $"Heroe {i + 1}", 8f, Hex("#E9D5FF"));

            // HP bar — BOTTOM de la carta (y 0.12–0.23)
            var hpBar = Child(card.transform, "HPBar");
            Anch(hpBar, 0.06f, 0.12f, 0.94f, 0.23f);

            var hpBg = Child(hpBar.transform, "Fondo");
            Anch(hpBg, 0, 0, 1, 1);
            Img(hpBg, Hex("#1A1A2E"));

            var hpFill = Child(hpBar.transform, "Relleno");
            Anch(hpFill, 0, 0, 1, 1);
            heroHPFills[i] = Img(hpFill, Hex("#22C55E"));
        }

        // ═══════════════════════════════════════════════════════════════════
        // ZONA 5 — Conjuros (inferior derecha, 2×5, reducida)
        // Anchors: x 0.66–0.99, y 0.03–0.43
        // ═══════════════════════════════════════════════════════════════════

        var zonaConj = Child(canvasGO.transform, "ZonaConjuros");
        Anch(zonaConj, 0.66f, 0.03f, 0.99f, 0.43f);
        Img(zonaConj, Hex("#0D0D1A"), 0.78f);

        var conjuroSlots = new Button[10];

        for (int fila = 0; fila < 2; fila++)
        {
            for (int col = 0; col < 5; col++)
            {
                int idx    = fila * 5 + col;
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

        // ═══════════════════════════════════════════════════════════════════
        // TOOLTIP PANEL (compartido: habilidades + conjuros)
        // Sort Order alto para estar sobre todo. SetActive(false) por defecto.
        // ═══════════════════════════════════════════════════════════════════

        var tooltipGO = Child(canvasGO.transform, "TooltipPanel");
        Anch(tooltipGO, 0.15f, 0.28f, 0.85f, 0.72f);
        var tooltipImg = Img(tooltipGO, Hex("#1A0F2E"));
        tooltipImg.color = new Color(tooltipImg.color.r, tooltipImg.color.g, tooltipImg.color.b, 0.97f);
        tooltipGO.SetActive(false);

        // Borde decorativo (outline interior)
        var tooltipBorder = Child(tooltipGO.transform, "Border");
        Anch(tooltipBorder, 0.01f, 0.02f, 0.99f, 0.98f);
        var borderImg = Img(tooltipBorder, Hex("#A855F7"), 0.25f);

        // Título "Descripción"
        var tooltipTituloGO = Child(tooltipGO.transform, "TooltipTitulo");
        Anch(tooltipTituloGO, 0.05f, 0.78f, 0.85f, 0.95f);
        Txt(tooltipTituloGO, "Descripcion", 12f, Hex("#A855F7"), bold: true);

        // Texto descripción
        var tooltipTxtGO = Child(tooltipGO.transform, "TooltipText");
        Anch(tooltipTxtGO, 0.05f, 0.28f, 0.95f, 0.76f);
        var tooltipTxt = Txt(tooltipTxtGO, "Descripcion de la habilidad...", 11f, Hex("#E9D5FF"),
                             align: TextAlignmentOptions.Left);

        // BtnUsar (visible solo si hay accion de confirmacion)
        var btnUsarGO = Child(tooltipGO.transform, "BtnUsar");
        Anch(btnUsarGO, 0.05f, 0.06f, 0.48f, 0.24f);
        Img(btnUsarGO, Hex("#4C1D95"));
        var btnUsarTooltip = btnUsarGO.AddComponent<Button>();
        Txt(Child(btnUsarGO.transform, "Label"), "Usar", 12f, Hex("#E9D5FF"), bold: true);

        // BtnCerrar
        var btnCerrarGO = Child(tooltipGO.transform, "BtnCerrar");
        Anch(btnCerrarGO, 0.52f, 0.06f, 0.95f, 0.24f);
        Img(btnCerrarGO, Hex("#2A0000"));
        var btnCerrarTooltip = btnCerrarGO.AddComponent<Button>();
        Txt(Child(btnCerrarGO.transform, "Label"), "Cerrar", 12f, Hex("#F87171"), bold: true);

        // ═══════════════════════════════════════════════════════════════════
        // ZONA 6 — ResultPanel (overlay, inactivo)
        // ═══════════════════════════════════════════════════════════════════

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
            resultStarImages[si] = Img(star, Hex("#FACC15")); // dorado por defecto
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

        // ── CombatSystem ──────────────────────────────────────────────────

        new GameObject("Systems").AddComponent<CombatSystem>();

        // ── CombatSceneController + wiring ────────────────────────────────

        var ctrlGO     = new GameObject("CombatSceneController");
        var controller = ctrlGO.AddComponent<CombatSceneController>();
        var so         = new SerializedObject(controller);

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
        SetArray   (so, "_heroCards",      4, i => heroCards[i]);
        SetTxtArray(so, "_heroNames",      4, i => heroNames[i]);
        SetArray   (so, "_heroHPFills",    4, i => heroHPFills[i]);
        SetArray   (so, "_turnIndicators", 4, i => turnIndicators[i]);

        // Zona Habilidades (3 círculos)
        SetArray(so, "_abilityCircles", 3, i => abilityCircleBtns[i]);

        // Zona Conjuros
        SetArray(so, "_conjuroSlots", 10, i => conjuroSlots[i]);

        // Tooltip Panel
        so.FindProperty("_tooltipPanel").objectReferenceValue      = tooltipGO;
        so.FindProperty("_tooltipText").objectReferenceValue       = tooltipTxt;
        so.FindProperty("_btnUsarTooltip").objectReferenceValue    = btnUsarTooltip;
        so.FindProperty("_btnCerrarTooltip").objectReferenceValue  = btnCerrarTooltip;

        // Barra de Turno
        so.FindProperty("_listaRetratos").objectReferenceValue    = listaRetratos.transform;

        // Result Panel
        so.FindProperty("_resultPanel").objectReferenceValue      = resultGO;
        so.FindProperty("_resultTitleText").objectReferenceValue  = tituloTxt;
        var resultStarsProp = so.FindProperty("_resultStarImages");
        resultStarsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            resultStarsProp.GetArrayElementAtIndex(i).objectReferenceValue = resultStarImages[i];
        so.FindProperty("_resultXPText").objectReferenceValue     = xpTxt;
        so.FindProperty("_resultDropsText").objectReferenceValue  = dropsTxt;
        so.FindProperty("_btnContinuar").objectReferenceValue     = btnContinuar;
        so.FindProperty("_btnReintentar").objectReferenceValue    = btnReintentar;

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
