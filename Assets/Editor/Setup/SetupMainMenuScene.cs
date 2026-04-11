using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using ReinoOscuridad.UI.MainMenu;
using ReinoOscuridad.Utils;

namespace ReinoOscuridad.Editor.Setup
{
    /// Configura MainMenuScene.unity — layout Bastion Maldito.
    /// La barra HUD superior la gestiona HUD.prefab (instanciado en runtime por MainMenuController).
    /// Esta scene solo contiene: ZonaEdificios (scroll libre) + ZonaAccesosRapidos (fija).
    /// Menu: Tools -> Reino Oscuridad -> 2. Setup MainMenuScene
    public static class SetupMainMenuScene
    {
        private const string SCENE_PATH = "Assets/Scenes/MainMenuScene.unity";
        private const string HUD_PATH   = "Assets/Prefabs/UI/HUD.prefab";

        // Content del ScrollRect ligeramente mayor que la pantalla para permitir
        // paneo libre en todos los direcciones sin alejarse demasiado.
        private const float CONTENT_W = 1900f;
        private const float CONTENT_H =  640f;

        [MenuItem("Tools/Reino Oscuridad/2. Setup MainMenuScene")]
        public static void Setup()
        {
            // ── Crear Scene si no existe ──────────────────────────────────────
            if (!File.Exists(SCENE_PATH))
            {
                var blank = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Directory.CreateDirectory("Assets/Scenes");
                EditorSceneManager.SaveScene(blank, SCENE_PATH);
            }

            var scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);

            // ── Limpiar objetos por defecto ───────────────────────────────────
            foreach (var go in scene.GetRootGameObjects())
                Object.DestroyImmediate(go);

            // ── Camara ────────────────────────────────────────────────────────
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor  = Hex("0A0A0A");
            cam.clearFlags       = CameraClearFlags.SolidColor;
            camGO.AddComponent<AudioListener>();
            camGO.transform.position = new Vector3(0f, 0f, -10f);

            // ── EventSystem (New Input System) ────────────────────────────────
            var evSys = new GameObject("EventSystem");
            evSys.AddComponent<EventSystem>();
            evSys.AddComponent<InputSystemUIInputModule>();

            // ── MainMenuController ────────────────────────────────────────────
            var ctrlGO = new GameObject("MainMenuController");
            var ctrl   = ctrlGO.AddComponent<MainMenuController>();

            // ── Canvas principal ──────────────────────────────────────────────
            var canvasGO = new GameObject("MainMenuCanvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Fondo global ──────────────────────────────────────────────────
            var bgGO = Child(canvasGO, "Background");
            bgGO.AddComponent<Image>().color = Hex("0D0D1A");
            Anch(bgGO, 0f, 0f, 1f, 1f);

            // =================================================================
            // ZONA EDIFICIOS — ScrollRect libre (todos los lados) con paneo suave
            // Jerarquia correcta: ZonaEdificios(ScrollRect) > Viewport(Mask) > Content
            // El HUD ocupa el 10% superior => el contenido empieza en y=0.90
            // La barra de accesos rapidos ocupa el 14% inferior => y=0.14
            // =================================================================
            var zonaEdifGO = Child(canvasGO, "ZonaEdificios");
            Anch(zonaEdifGO, 0f, 0.14f, 1f, UIConstants.CONTENT_START_PERCENT);

            // ScrollRect en ZonaEdificios
            var scroll = zonaEdifGO.AddComponent<ScrollRect>();
            scroll.horizontal        = true;
            scroll.vertical          = true;
            scroll.movementType      = ScrollRect.MovementType.Elastic;
            scroll.elasticity        = 0.08f;
            scroll.inertia           = true;
            scroll.decelerationRate  = 0.15f;
            scroll.scrollSensitivity = 1f;

            // Viewport — hijo de ZonaEdificios, tiene la Mask
            var viewportGO = Child(zonaEdifGO, "Viewport");
            Anch(viewportGO, 0f, 0f, 1f, 1f);
            var viewImg = viewportGO.AddComponent<Image>();
            viewImg.color = Color.clear;
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = viewportGO.GetComponent<RectTransform>();

            // Content — hijo de Viewport, mas grande que la pantalla para paneo libre
            var contentGO = Child(viewportGO, "ContentEdificios");
            var cRT       = contentGO.GetComponent<RectTransform>();
            cRT.anchorMin        = new Vector2(0.5f, 0.5f);
            cRT.anchorMax        = new Vector2(0.5f, 0.5f);
            cRT.pivot            = new Vector2(0.5f, 0.5f);
            cRT.sizeDelta        = new Vector2(CONTENT_W, CONTENT_H);
            cRT.anchoredPosition = Vector2.zero;
            scroll.content       = cRT;

            // ── 9 edificios dentro del Content ────────────────────────────────
            // Posiciones relativas al centro del Content (pivot 0.5,0.5)
            // x negativo = izquierda, y positivo = arriba
            var edificios = new (string name, string label, float x, float y, float w, float h)[]
            {
                // Fila superior
                ("Edificio_Porton",     "Portal",     -650f,  200f, 200f, 200f),
                ("Edificio_Altar",      "Altar",      -360f,  200f, 160f, 160f),
                ("Edificio_Campana",    "Campana",     -80f,  200f, 160f, 160f),
                ("Edificio_Arena",      "Arena",       240f,  200f, 160f, 160f),
                // Fila central
                ("Edificio_Cuartel",    "Cuartel",    -500f,    0f, 160f, 160f),
                ("Edificio_Forja",      "Forja",      -220f,    0f, 160f, 160f),
                ("Edificio_Biblioteca", "Biblioteca",   80f,    0f, 160f, 160f),
                // Fila inferior
                ("Edificio_Mercado",    "Mercado",    -420f, -200f, 160f, 160f),
                ("Edificio_Taverna",    "Taverna",    -140f, -200f, 160f, 160f),
                ("Edificio_Mazmorra",   "Mazmorra",    180f, -200f, 160f, 160f),
                ("Edificio_Misiones",   "Misiones",    460f, -200f, 160f, 160f),
            };

            var edificioBtns = new Button[edificios.Length];
            for (int i = 0; i < edificios.Length; i++)
            {
                var ed   = edificios[i];
                var edGO = Child(contentGO, ed.name);
                var edRT = edGO.GetComponent<RectTransform>();
                edRT.anchorMin        = new Vector2(0.5f, 0.5f);
                edRT.anchorMax        = new Vector2(0.5f, 0.5f);
                edRT.pivot            = new Vector2(0.5f, 0.5f);
                edRT.sizeDelta        = new Vector2(ed.w, ed.h);
                edRT.anchoredPosition = new Vector2(ed.x, ed.y);

                var edImg = edGO.AddComponent<Image>();
                edImg.color = Hex("4A6FD4");
                var edBtn = edGO.AddComponent<Button>();
                edBtn.targetGraphic = edImg;

                var c               = edBtn.colors;
                c.normalColor       = Hex("4A6FD4");
                c.highlightedColor  = Hex("6A8FF4");
                c.pressedColor      = Hex("3A5FB4");
                edBtn.colors = c;

                var lblGO  = Child(edGO, "Label");
                var lblRT  = lblGO.GetComponent<RectTransform>();
                lblRT.anchorMin = Vector2.zero;
                lblRT.anchorMax = Vector2.one;
                lblRT.offsetMin = Vector2.zero;
                lblRT.offsetMax = Vector2.zero;

                var lbl = lblGO.AddComponent<TextMeshProUGUI>();
                lbl.text      = ed.label;
                lbl.fontSize  = 14;
                lbl.fontStyle = FontStyles.Bold;
                lbl.color     = Color.white;
                lbl.alignment = TextAlignmentOptions.Center;

                edificioBtns[i] = edBtn;
            }

            // =================================================================
            // ZONA ACCESOS RAPIDOS — barra fija inferior izquierda
            // =================================================================
            var zonaAccGO = Child(canvasGO, "ZonaAccesosRapidos");
            zonaAccGO.AddComponent<Image>().color = new Color(0.039f, 0.039f, 0.059f, 0.85f);
            Anch(zonaAccGO, 0f, 0f, 0.52f, 0.14f);

            var accesos = new (string name, string label, float x0, float x1)[]
            {
                ("Btn_Triloguzano", "Trio",   0.01f, 0.17f),
                ("Btn_Tower",       "Torre",  0.18f, 0.34f),
                ("Btn_WorldBoss",   "Boss",   0.35f, 0.51f),
                ("Btn_Mazmorra",    "Mazm.",  0.52f, 0.68f),
                ("Btn_Event",       "Evento", 0.69f, 0.85f),
                ("Btn_Profile",     "Perfil", 0.86f, 1.00f),
            };

            var accesoBtns = new Button[accesos.Length];
            for (int i = 0; i < accesos.Length; i++)
            {
                var ac    = accesos[i];
                var acGO  = Child(zonaAccGO, ac.name);
                var acImg = acGO.AddComponent<Image>();
                acImg.color = Hex("22224A");
                var acBtn = acGO.AddComponent<Button>();
                acBtn.targetGraphic = acImg;
                Anch(acGO, ac.x0, 0.08f, ac.x1, 0.92f);

                var acLbl = Child(acGO, "Label");
                Anch(acLbl, 0f, 0f, 1f, 1f);
                var acTMP = acLbl.AddComponent<TextMeshProUGUI>();
                acTMP.text      = ac.label;
                acTMP.fontSize  = 11;
                acTMP.color     = Color.white;
                acTMP.alignment = TextAlignmentOptions.Center;

                accesoBtns[i] = acBtn;
            }

            // SubIconosAbanico — aparece encima de la barra al pulsar Triloguzano
            var abanicoGO = Child(zonaAccGO, "SubIconosAbanico");
            abanicoGO.AddComponent<Image>().color = new Color(0.10f, 0.10f, 0.20f, 0.95f);
            // Posicion: encima de los primeros 3 iconos, sube hacia arriba
            Anch(abanicoGO, 0f, 1f, 0.52f, 4f);

            var subDefs = new (string name, string label, float y0, float y1)[]
            {
                ("SubBtn_Torre",    "Torre", 0.64f, 0.97f),
                ("SubBtn_Boss",     "Boss",  0.32f, 0.63f),
                ("SubBtn_Mazmorra", "Mazm.", 0.00f, 0.31f),
            };
            var subBtns = new Button[subDefs.Length];
            for (int i = 0; i < subDefs.Length; i++)
            {
                var sd    = subDefs[i];
                var sdGO  = Child(abanicoGO, sd.name);
                var sdImg = sdGO.AddComponent<Image>();
                sdImg.color = Hex("2D2D5E");
                var sdBtn = sdGO.AddComponent<Button>();
                sdBtn.targetGraphic = sdImg;
                Anch(sdGO, 0.05f, sd.y0, 0.95f, sd.y1);

                var sdLbl = Child(sdGO, "Label");
                Anch(sdLbl, 0f, 0f, 1f, 1f);
                var sdTMP = sdLbl.AddComponent<TextMeshProUGUI>();
                sdTMP.text      = sd.label;
                sdTMP.fontSize  = 12;
                sdTMP.color     = Color.white;
                sdTMP.alignment = TextAlignmentOptions.Center;

                subBtns[i] = sdBtn;
            }

            abanicoGO.SetActive(false);

            // =================================================================
            // Listeners persistentes
            // =================================================================
            // Edificios
            UnityEventTools.AddPersistentListener(edificioBtns[0].onClick, ctrl.GoToCampaign);  // Portal
            UnityEventTools.AddPersistentListener(edificioBtns[1].onClick, ctrl.GoToGacha);     // Altar (invocacion)
            UnityEventTools.AddPersistentListener(edificioBtns[2].onClick, ctrl.GoToCampaign);  // Campana
            UnityEventTools.AddPersistentListener(edificioBtns[3].onClick, ctrl.GoToArena);     // Arena
            UnityEventTools.AddPersistentListener(edificioBtns[4].onClick, ctrl.GoToHeroes);    // Cuartel
            UnityEventTools.AddPersistentListener(edificioBtns[5].onClick, ctrl.GoToConjuros);  // Forja
            UnityEventTools.AddPersistentListener(edificioBtns[6].onClick, ctrl.GoToGacha);     // Biblioteca
            UnityEventTools.AddPersistentListener(edificioBtns[7].onClick, ctrl.GoToShop);      // Mercado
            UnityEventTools.AddPersistentListener(edificioBtns[8].onClick, ctrl.GoToClan);      // Taverna
            UnityEventTools.AddPersistentListener(edificioBtns[9].onClick, ctrl.GoToDungeon);   // Mazmorra
            UnityEventTools.AddPersistentListener(edificioBtns[10].onClick, ctrl.GoToMissions); // Misiones

            // Accesos rapidos
            UnityEventTools.AddPersistentListener(accesoBtns[0].onClick, ctrl.GoToTriloguzano);
            UnityEventTools.AddPersistentListener(accesoBtns[1].onClick, ctrl.GoToTower);
            UnityEventTools.AddPersistentListener(accesoBtns[2].onClick, ctrl.GoToWorldBoss);
            UnityEventTools.AddPersistentListener(accesoBtns[3].onClick, ctrl.GoToDungeon);
            UnityEventTools.AddPersistentListener(accesoBtns[4].onClick, ctrl.GoToEvent);
            UnityEventTools.AddPersistentListener(accesoBtns[5].onClick, ctrl.GoToProfile);

            // Sub-abanico
            UnityEventTools.AddPersistentListener(subBtns[0].onClick, ctrl.GoToTower);
            UnityEventTools.AddPersistentListener(subBtns[1].onClick, ctrl.GoToWorldBoss);
            UnityEventTools.AddPersistentListener(subBtns[2].onClick, ctrl.GoToDungeon);

            // =================================================================
            // SerializeFields en MainMenuController
            // =================================================================
            var soCtrl = new SerializedObject(ctrl);

            soCtrl.FindProperty("_subIconosAbanico").objectReferenceValue = abanicoGO;

            // Edificios
            soCtrl.FindProperty("_btnCampaign").objectReferenceValue = edificioBtns[0];  // Portal
            soCtrl.FindProperty("_btnGacha").objectReferenceValue    = edificioBtns[1];  // Altar
            soCtrl.FindProperty("_btnArena").objectReferenceValue    = edificioBtns[3];  // Arena
            soCtrl.FindProperty("_btnHeroes").objectReferenceValue   = edificioBtns[4];  // Cuartel
            soCtrl.FindProperty("_btnConjuros").objectReferenceValue = edificioBtns[5];  // Forja
            soCtrl.FindProperty("_btnShop").objectReferenceValue     = edificioBtns[7];  // Mercado
            soCtrl.FindProperty("_btnClan").objectReferenceValue     = edificioBtns[8];  // Taverna
            soCtrl.FindProperty("_btnDungeon").objectReferenceValue  = edificioBtns[9];  // Mazmorra
            soCtrl.FindProperty("_btnMissions").objectReferenceValue = edificioBtns[10]; // Misiones

            // Accesos rapidos
            soCtrl.FindProperty("_btnTriloguzano").objectReferenceValue    = accesoBtns[0];
            soCtrl.FindProperty("_btnTower").objectReferenceValue          = accesoBtns[1];
            soCtrl.FindProperty("_btnWorldBoss").objectReferenceValue      = accesoBtns[2];
            soCtrl.FindProperty("_btnMazmorraRapido").objectReferenceValue = accesoBtns[3];
            soCtrl.FindProperty("_btnEvent").objectReferenceValue          = accesoBtns[4];
            soCtrl.FindProperty("_btnProfile").objectReferenceValue        = accesoBtns[5];

            soCtrl.ApplyModifiedProperties();

            // ── HUD prefab — solo referencia; lo instancia MainMenuController en runtime ──
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HUD_PATH);
            if (hudPrefab != null)
            {
                var soCtrl2 = new SerializedObject(ctrl);
                soCtrl2.FindProperty("_hudPrefab").objectReferenceValue = hudPrefab;
                soCtrl2.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("[SetupMainMenu] HUD.prefab no encontrado en " + HUD_PATH +
                                 ". Ejecuta primero Tools -> Reino Oscuridad -> 1. Setup HUD Prefab.");
            }

            // ── Build Settings ────────────────────────────────────────────────
            AddToBuildSettings(SCENE_PATH);

            // ── Guardar ───────────────────────────────────────────────────────
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
