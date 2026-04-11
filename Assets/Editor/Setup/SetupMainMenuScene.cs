using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using ReinoOscuridad.UI.MainMenu;
using ReinoOscuridad.Utils;

namespace ReinoOscuridad.Editor.Setup
{
    /// Configura MainMenuScene.unity completa con anclas relativas.
    /// Menú: Tools → Reino Oscuridad → 2. Setup MainMenuScene
    public static class SetupMainMenuScene
    {
        private const string SCENE_PATH  = "Assets/Scenes/MainMenuScene.unity";
        private const string HUD_PATH    = "Assets/Prefabs/UI/HUD.prefab";

        [MenuItem("Tools/Reino Oscuridad/2. Setup MainMenuScene")]
        public static void Setup()
        {
            // Crear el archivo .unity si no existe
            if (!File.Exists(SCENE_PATH))
            {
                var blank = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Directory.CreateDirectory("Assets/Scenes");
                EditorSceneManager.SaveScene(blank, SCENE_PATH);
            }

            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

            // ── PASO 1: Limpiar objetos por defecto ───────────────────────────
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name != "Main Camera")
                    Object.DestroyImmediate(go);
            }

            // ── PASO 2: Configurar cámara ─────────────────────────────────────
            var camGO = GameObject.Find("Main Camera");
            if (camGO == null)
            {
                camGO     = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }
            var cam = camGO.GetComponent<Camera>();
            if (cam == null) cam = camGO.AddComponent<Camera>();
            cam.orthographic      = true;
            cam.orthographicSize  = 5f;
            cam.backgroundColor   = Hex("0A0A0A");
            cam.clearFlags        = CameraClearFlags.SolidColor;
            camGO.transform.position = new Vector3(0f, 0f, -10f);

            // EventSystem (necesario para interacción UI)
            var evSys = new GameObject("EventSystem");
            evSys.AddComponent<EventSystem>();
            evSys.AddComponent<StandaloneInputModule>();

            // ── PASO 3: MainMenuController ────────────────────────────────────
            var ctrlGO = new GameObject("MainMenuController");
            var ctrl   = ctrlGO.AddComponent<MainMenuController>();

            // ── PASO 4: Canvas principal ──────────────────────────────────────
            var canvasGO = new GameObject("MainMenuCanvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // ── PASO 5: Fondo ─────────────────────────────────────────────────
            var bgGO = Child(canvasGO, "Background");
            bgGO.AddComponent<Image>().color = Hex("0D0D1A");
            Anch(bgGO, 0f, 0f, 1f, UIConstants.CONTENT_START_PERCENT);

            // ── PASO 6: Grid de botones ───────────────────────────────────────
            var gridGO = Child(canvasGO, "GridBotones");
            Anch(gridGO, 0.05f, 0.05f, 0.95f, 0.88f);

            // (nombre, label, método, c0, c1, r0, r1)
            var defs = new (string n, string lbl, UnityAction act, float c0, float c1, float r0, float r1)[]
            {
                // Fila 3
                ("Btn_Campaign",  "⚔ Campaña",    ctrl.GoToCampaign,  0.00f, 0.31f, 0.75f, 1.00f),
                ("Btn_Heroes",    "🧟 Esbirros",   ctrl.GoToHeroes,    0.34f, 0.65f, 0.75f, 1.00f),
                ("Btn_Gacha",     "🌀 Invocar",    ctrl.GoToGacha,     0.68f, 0.99f, 0.75f, 1.00f),
                // Fila 2
                ("Btn_Arena",     "🏆 Arena",      ctrl.GoToArena,     0.00f, 0.31f, 0.50f, 0.73f),
                ("Btn_Tower",     "🗼 Torre",      ctrl.GoToTower,     0.34f, 0.65f, 0.50f, 0.73f),
                ("Btn_WorldBoss", "💀 Boss Global",ctrl.GoToWorldBoss, 0.68f, 0.99f, 0.50f, 0.73f),
                // Fila 1
                ("Btn_Clan",      "☠ Clan",       ctrl.GoToClan,      0.00f, 0.31f, 0.25f, 0.48f),
                ("Btn_Shop",      "🛒 Tienda",     ctrl.GoToShop,      0.34f, 0.65f, 0.25f, 0.48f),
                ("Btn_Dungeon",   "🕳 Mazmorras",  ctrl.GoToDungeon,   0.68f, 0.99f, 0.25f, 0.48f),
                // Fila 0
                ("Btn_Missions",  "📋 Misiones",   ctrl.GoToMissions,  0.17f, 0.48f, 0.00f, 0.23f),
                ("Btn_Conjuros",  "✨ Conjuros",   ctrl.GoToConjuros,  0.51f, 0.82f, 0.00f, 0.23f),
            };

            var buttons = new Button[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                var d     = defs[i];
                var btnGO = Child(gridGO, d.n);
                Anch(btnGO, d.c0, d.r0, d.c1, d.r1);

                var img = btnGO.AddComponent<Image>();
                img.color = Hex("1A1A2E");

                var btn = btnGO.AddComponent<Button>();
                btn.targetGraphic = img;

                var colors             = btn.colors;
                colors.normalColor     = Hex("1A1A2E");
                colors.highlightedColor = Hex("2D2D4E");
                colors.pressedColor    = Hex("3D3D6E");
                btn.colors = colors;

                var lblGO  = Child(btnGO, "Label");
                var lbl    = lblGO.AddComponent<TextMeshProUGUI>();
                lbl.text      = d.lbl;
                lbl.fontSize  = 16;
                lbl.fontStyle = FontStyles.Bold;
                lbl.color     = Color.white;
                lbl.alignment = TextAlignmentOptions.Center;
                Anch(lblGO, 0f, 0f, 1f, 1f);

                UnityEventTools.AddPersistentListener(btn.onClick, d.act);
                buttons[i] = btn;
            }

            // Asignar botones en MainMenuController
            var soCtrl = new SerializedObject(ctrl);
            soCtrl.FindProperty("_btnCampaign").objectReferenceValue  = buttons[0];
            soCtrl.FindProperty("_btnHeroes").objectReferenceValue    = buttons[1];
            soCtrl.FindProperty("_btnGacha").objectReferenceValue     = buttons[2];
            soCtrl.FindProperty("_btnArena").objectReferenceValue     = buttons[3];
            soCtrl.FindProperty("_btnTower").objectReferenceValue     = buttons[4];
            soCtrl.FindProperty("_btnWorldBoss").objectReferenceValue = buttons[5];
            soCtrl.FindProperty("_btnClan").objectReferenceValue      = buttons[6];
            soCtrl.FindProperty("_btnShop").objectReferenceValue      = buttons[7];
            soCtrl.FindProperty("_btnDungeon").objectReferenceValue   = buttons[8];
            soCtrl.FindProperty("_btnMissions").objectReferenceValue  = buttons[9];
            soCtrl.FindProperty("_btnConjuros").objectReferenceValue  = buttons[10];
            soCtrl.ApplyModifiedProperties();

            // ── PASO 7: HUD prefab ────────────────────────────────────────────
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HUD_PATH);
            if (hudPrefab != null)
            {
                var hudInstance = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab);
                hudInstance.name = "HUD";

                var soCtrl2 = new SerializedObject(ctrl);
                soCtrl2.FindProperty("_hudPrefab").objectReferenceValue = hudPrefab;
                soCtrl2.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("[SetupMainMenu] HUD.prefab no encontrado en " + HUD_PATH +
                                 ". Ejecuta primero 'Tools → Reino Oscuridad → 1. Setup HUD Prefab'.");
            }

            // ── Añadir MainMenuScene a Build Settings (índice 1) ───────────────
            AddToBuildSettings(SCENE_PATH);

            // ── PASO 8: Guardar ───────────────────────────────────────────────
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[SetupMainMenu] MainMenuScene configurada correctamente");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static void AddToBuildSettings(string path)
        {
            var existing = EditorBuildSettings.scenes;
            foreach (var s in existing)
                if (s.path == path) return;

            var updated = new EditorBuildSettingsScene[existing.Length + 1];
            existing.CopyTo(updated, 0);
            updated[existing.Length] = new EditorBuildSettingsScene(path, true);
            EditorBuildSettings.scenes = updated;
        }

        static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            if (go.GetComponent<RectTransform>() == null)
                go.AddComponent<RectTransform>();
            return go;
        }

        static void Anch(GameObject go, float xMin, float yMin, float xMax, float yMax)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color c);
            return c;
        }
    }
}
