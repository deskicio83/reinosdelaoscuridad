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
using ReinoOscuridad.Data;

namespace ReinoOscuridad.Editor.Setup
{
    /// Configura MainMenuScene.unity — layout Bastion Maldito 5 zonas.
    /// Menu: Tools -> Reino Oscuridad -> 2. Setup MainMenuScene
    public static class SetupMainMenuScene
    {
        private const string SCENE_PATH = "Assets/Scenes/MainMenuScene.unity";
        private const string HUD_PATH   = "Assets/Prefabs/UI/HUD.prefab";

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
            // ZONA 1 — HUD_PlayerInfo (izquierda superior)
            // =================================================================
            var hudPlayerInfo = Child(canvasGO, "HUD_PlayerInfo");
            hudPlayerInfo.AddComponent<Image>().color = new Color(0.039f, 0.039f, 0.059f, 0.85f);
            Anch(hudPlayerInfo, 0f, UIConstants.CONTENT_START_PERCENT, 0.28f, 1f);

            var avatarGO = Child(hudPlayerInfo, "AvatarButton");
            var avatarImg = avatarGO.AddComponent<Image>();
            avatarImg.color = Hex("444466");
            var avatarBtn = avatarGO.AddComponent<Button>();
            avatarBtn.targetGraphic = avatarImg;
            Anch(avatarGO, 0.03f, 0.10f, 0.22f, 0.90f);

            var nombreGO  = Child(hudPlayerInfo, "NombreText");
            var nombreTMP = nombreGO.AddComponent<TextMeshProUGUI>();
            nombreTMP.text         = "DarkLord42";
            nombreTMP.fontSize     = 16;
            nombreTMP.fontStyle    = FontStyles.Bold;
            nombreTMP.color        = Color.white;
            nombreTMP.overflowMode = TextOverflowModes.Truncate;
            nombreTMP.alignment    = TextAlignmentOptions.Left;
            Anch(nombreGO, 0.24f, 0.52f, 0.97f, 0.95f);

            var nivelGO  = Child(hudPlayerInfo, "NivelBadge");
            var nivelTMP = nivelGO.AddComponent<TextMeshProUGUI>();
            nivelTMP.text      = "Nv. 15";
            nivelTMP.fontSize  = 12;
            nivelTMP.color     = Hex("AAAACC");
            nivelTMP.alignment = TextAlignmentOptions.Left;
            Anch(nivelGO, 0.24f, 0.05f, 0.97f, 0.50f);

            // =================================================================
            // ZONA 2 — HUD_Monedas (centro superior)
            // =================================================================
            var hudMonedas = Child(canvasGO, "HUD_Monedas");
            hudMonedas.AddComponent<Image>().color = new Color(0.039f, 0.039f, 0.059f, 0.85f);
            Anch(hudMonedas, 0.28f, UIConstants.CONTENT_START_PERCENT, 0.72f, 1f);

            var energyPillGO    = MakePill(hudMonedas, "Pill_Energy",    0f,     0.33f, "120/120", CurrencyType.Energy);
            var goldPillGO      = MakePill(hudMonedas, "Pill_Gold",      0.33f,  0.66f, "7.690",   CurrencyType.Gold);
            var caosiferaPillGO = MakePill(hudMonedas, "Pill_Caosifera", 0.66f,  1f,    "385",     CurrencyType.Caosifera);

            // =================================================================
            // ZONA 3 — HUD_Acciones (derecha superior)
            // =================================================================
            var hudAcciones = Child(canvasGO, "HUD_Acciones");
            hudAcciones.AddComponent<Image>().color = new Color(0.039f, 0.039f, 0.059f, 0.85f);
            Anch(hudAcciones, 0.72f, UIConstants.CONTENT_START_PERCENT, 1f, 1f);

            var btnChatGO     = MakeAccionBtn(hudAcciones, "Btn_Chat",     "Chat", 0.02f,  0.26f);
            var btnMailGO     = MakeAccionBtn(hudAcciones, "Btn_Mail",     "Mail", 0.27f,  0.51f);
            var btnSettingsGO = MakeAccionBtn(hudAcciones, "Btn_Settings", "Set.", 0.52f,  0.76f);
            var btnEventGO    = MakeAccionBtn(hudAcciones, "Btn_Event",    "Evt.", 0.77f,  0.99f);

            // Badge en Mail
            var mailBadgeGO = Child(btnMailGO, "MailBadge");
            mailBadgeGO.AddComponent<Image>().color = Hex("E53E3E");
            Anch(mailBadgeGO, 0.58f, 0.55f, 1f, 1f);
            var mailBadgeTextGO  = Child(mailBadgeGO, "BadgeText");
            var mailBadgeTMP     = mailBadgeTextGO.AddComponent<TextMeshProUGUI>();
            mailBadgeTMP.text      = "3";
            mailBadgeTMP.fontSize  = 9;
            mailBadgeTMP.color     = Color.white;
            mailBadgeTMP.alignment = TextAlignmentOptions.Center;
            Anch(mailBadgeTextGO, 0f, 0f, 1f, 1f);

            // Badge en Event
            var evtBadgeGO = Child(btnEventGO, "EventBadge");
            evtBadgeGO.AddComponent<Image>().color = Hex("F6AD55");
            Anch(evtBadgeGO, 0.58f, 0.55f, 1f, 1f);
            var evtBadgeTextGO  = Child(evtBadgeGO, "BadgeText");
            var evtBadgeTMP     = evtBadgeTextGO.AddComponent<TextMeshProUGUI>();
            evtBadgeTMP.text      = "!";
            evtBadgeTMP.fontSize  = 9;
            evtBadgeTMP.color     = Color.white;
            evtBadgeTMP.alignment = TextAlignmentOptions.Center;
            Anch(evtBadgeTextGO, 0f, 0f, 1f, 1f);

            // =================================================================
            // ZONA 4 — ZonaEdificios (ScrollRect horizontal)
            // =================================================================
            var zonaEdifGO = Child(canvasGO, "ZonaEdificios");
            Anch(zonaEdifGO, 0f, 0.14f, 1f, UIConstants.CONTENT_START_PERCENT);

            // Mascara + ScrollRect
            var scrollGO  = Child(zonaEdifGO, "ScrollRect_Edificios");
            Anch(scrollGO, 0.04f, 0.05f, 0.96f, 0.95f);
            var scrollImg  = scrollGO.AddComponent<Image>();
            scrollImg.color = Color.clear;
            var scrollMask = scrollGO.AddComponent<Mask>();
            scrollMask.showMaskGraphic = false;

            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal         = true;
            scroll.vertical           = false;
            scroll.movementType       = ScrollRect.MovementType.Elastic;
            scroll.elasticity         = 0.1f;
            scroll.inertia            = true;
            scroll.decelerationRate   = 0.135f;
            scroll.scrollSensitivity  = 1f;
            scroll.horizontalScrollbar = null;
            scroll.verticalScrollbar   = null;

            // Content
            var contentGO  = Child(scrollGO, "ContentEdificios");
            var contentRT  = contentGO.GetComponent<RectTransform>();
            if (contentRT == null) contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin       = new Vector2(0f, 0f);
            contentRT.anchorMax       = new Vector2(0f, 1f);
            contentRT.pivot           = new Vector2(0f, 0.5f);
            contentRT.sizeDelta       = new Vector2(1800f, 0f);
            contentRT.anchoredPosition = Vector2.zero;
            scroll.content = contentRT;

            // 9 edificios: name, label, xPos, size
            var edificios = new (string name, string label, float x, float w, float h)[]
            {
                ("Edificio_Porton",    "Portal",    30f,   200f, 200f),
                ("Edificio_Campana",   "Campana",   260f,  160f, 160f),
                ("Edificio_Cuartel",   "Cuartel",   450f,  160f, 160f),
                ("Edificio_Forja",     "Forja",     640f,  160f, 160f),
                ("Edificio_Mercado",   "Mercado",   830f,  160f, 160f),
                ("Edificio_Biblioteca","Biblioteca",1020f, 160f, 160f),
                ("Edificio_Altar",     "Altar",     1210f, 160f, 160f),
                ("Edificio_Taverna",   "Taverna",   1400f, 160f, 160f),
                ("Edificio_Mazmorra",  "Mazmorra",  1590f, 160f, 160f),
            };

            var edificioBtns = new Button[edificios.Length];
            for (int i = 0; i < edificios.Length; i++)
            {
                var ed      = edificios[i];
                var edGO    = Child(contentGO, ed.name);
                var edRT    = edGO.GetComponent<RectTransform>();
                if (edRT == null) edRT = edGO.AddComponent<RectTransform>();

                edRT.anchorMin        = new Vector2(0f, 0.5f);
                edRT.anchorMax        = new Vector2(0f, 0.5f);
                edRT.pivot            = new Vector2(0f, 0.5f);
                edRT.sizeDelta        = new Vector2(ed.w, ed.h);
                edRT.anchoredPosition = new Vector2(ed.x, 0f);

                var edImg   = edGO.AddComponent<Image>();
                edImg.color = Hex("1A1A2E");
                var edBtn   = edGO.AddComponent<Button>();
                edBtn.targetGraphic = edImg;

                var edColors               = edBtn.colors;
                edColors.normalColor       = Hex("1A1A2E");
                edColors.highlightedColor  = Hex("2D2D4E");
                edColors.pressedColor      = Hex("3D3D6E");
                edBtn.colors = edColors;

                var edLblGO  = Child(edGO, "Label");
                var edLblRT  = edLblGO.GetComponent<RectTransform>();
                if (edLblRT == null) edLblRT = edLblGO.AddComponent<RectTransform>();
                edLblRT.anchorMin = Vector2.zero;
                edLblRT.anchorMax = Vector2.one;
                edLblRT.offsetMin = Vector2.zero;
                edLblRT.offsetMax = Vector2.zero;

                var edTMP   = edLblGO.AddComponent<TextMeshProUGUI>();
                edTMP.text      = ed.label;
                edTMP.fontSize  = 14;
                edTMP.fontStyle = FontStyles.Bold;
                edTMP.color     = Color.white;
                edTMP.alignment = TextAlignmentOptions.Center;

                edificioBtns[i] = edBtn;
            }

            // Flechas de scroll (fuera del ScrollRect, sobre ZonaEdificios)
            var arrowIzqGO  = Child(zonaEdifGO, "ArrowIzquierda");
            var arrowIzqImg = arrowIzqGO.AddComponent<Image>();
            arrowIzqImg.color = new Color(1f, 1f, 1f, 0.3f);
            var arrowIzqBtn = arrowIzqGO.AddComponent<Button>();
            arrowIzqBtn.targetGraphic = arrowIzqImg;
            Anch(arrowIzqGO, 0f, 0.20f, 0.04f, 0.80f);
            var arrowIzqLbl = Child(arrowIzqGO, "Label");
            Anch(arrowIzqLbl, 0f, 0f, 1f, 1f);
            var arrowIzqTMP = arrowIzqLbl.AddComponent<TextMeshProUGUI>();
            arrowIzqTMP.text      = "<";
            arrowIzqTMP.fontSize  = 20;
            arrowIzqTMP.color     = Color.white;
            arrowIzqTMP.alignment = TextAlignmentOptions.Center;

            var arrowDerGO  = Child(zonaEdifGO, "ArrowDerecha");
            var arrowDerImg = arrowDerGO.AddComponent<Image>();
            arrowDerImg.color = new Color(1f, 1f, 1f, 0.3f);
            var arrowDerBtn = arrowDerGO.AddComponent<Button>();
            arrowDerBtn.targetGraphic = arrowDerImg;
            Anch(arrowDerGO, 0.96f, 0.20f, 1f, 0.80f);
            var arrowDerLbl = Child(arrowDerGO, "Label");
            Anch(arrowDerLbl, 0f, 0f, 1f, 1f);
            var arrowDerTMP = arrowDerLbl.AddComponent<TextMeshProUGUI>();
            arrowDerTMP.text      = ">";
            arrowDerTMP.fontSize  = 20;
            arrowDerTMP.color     = Color.white;
            arrowDerTMP.alignment = TextAlignmentOptions.Center;

            // =================================================================
            // ZONA 5 — ZonaAccesosRapidos (izquierda inferior, fija)
            // =================================================================
            var zonaAccGO = Child(canvasGO, "ZonaAccesosRapidos");
            zonaAccGO.AddComponent<Image>().color = new Color(0.039f, 0.039f, 0.059f, 0.70f);
            Anch(zonaAccGO, 0f, 0f, 0.52f, 0.14f);

            // 6 iconos de acceso rapido en fila
            // (name, label, xMin, xMax)
            var accesos = new (string name, string label, float x0, float x1)[]
            {
                ("Btn_Triloguzano", "Trio",   0.01f, 0.18f),
                ("Btn_Tower",       "Torre",  0.19f, 0.36f),
                ("Btn_WorldBoss",   "Boss",   0.37f, 0.53f),
                ("Btn_Mazmorra",    "Mazm.",  0.54f, 0.70f),
                ("Btn_Event",       "Evento", 0.71f, 0.87f),
                ("Btn_Profile",     "Perfil", 0.88f, 1.00f),
            };

            var accesoBtns = new Button[accesos.Length];
            for (int i = 0; i < accesos.Length; i++)
            {
                var ac     = accesos[i];
                var acGO   = Child(zonaAccGO, ac.name);
                var acImg  = acGO.AddComponent<Image>();
                acImg.color = Hex("22224A");
                var acBtn  = acGO.AddComponent<Button>();
                acBtn.targetGraphic = acImg;
                Anch(acGO, ac.x0, 0.08f, ac.x1, 0.92f);

                var acLblGO  = Child(acGO, "Label");
                Anch(acLblGO, 0f, 0f, 1f, 1f);
                var acTMP   = acLblGO.AddComponent<TextMeshProUGUI>();
                acTMP.text      = ac.label;
                acTMP.fontSize  = 11;
                acTMP.color     = Color.white;
                acTMP.alignment = TextAlignmentOptions.Center;

                accesoBtns[i] = acBtn;
            }

            // SubIconosAbanico (Toggle oculto por defecto — GoToTriloguzano lo activa)
            var abanicoGO = Child(zonaAccGO, "SubIconosAbanico");
            abanicoGO.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.2f, 0.95f);
            Anch(abanicoGO, 0f, 1f, 0.35f, 4f); // sube sobre la zona inferior

            var subDefs = new (string name, string label, float y0, float y1)[]
            {
                ("SubBtn_Torre",    "Torre",    0.64f, 0.97f),
                ("SubBtn_Boss",     "Boss",     0.32f, 0.63f),
                ("SubBtn_Mazmorra", "Mazm.",    0f,    0.31f),
            };
            var subBtns = new Button[subDefs.Length];
            for (int i = 0; i < subDefs.Length; i++)
            {
                var sd     = subDefs[i];
                var sdGO   = Child(abanicoGO, sd.name);
                var sdImg  = sdGO.AddComponent<Image>();
                sdImg.color = Hex("2D2D5E");
                var sdBtn  = sdGO.AddComponent<Button>();
                sdBtn.targetGraphic = sdImg;
                Anch(sdGO, 0.05f, sd.y0, 0.95f, sd.y1);

                var sdLblGO  = Child(sdGO, "Label");
                Anch(sdLblGO, 0f, 0f, 1f, 1f);
                var sdTMP   = sdLblGO.AddComponent<TextMeshProUGUI>();
                sdTMP.text      = sd.label;
                sdTMP.fontSize  = 12;
                sdTMP.color     = Color.white;
                sdTMP.alignment = TextAlignmentOptions.Center;

                subBtns[i] = sdBtn;
            }

            abanicoGO.SetActive(false);

            // =================================================================
            // Asignar listeners via UnityEventTools
            // =================================================================
            // Edificios
            UnityEventTools.AddPersistentListener(edificioBtns[0].onClick, ctrl.GoToCampaign);  // Portal
            UnityEventTools.AddPersistentListener(edificioBtns[1].onClick, ctrl.GoToCampaign);  // Campana -> Campaign
            UnityEventTools.AddPersistentListener(edificioBtns[2].onClick, ctrl.GoToHeroes);    // Cuartel
            UnityEventTools.AddPersistentListener(edificioBtns[3].onClick, ctrl.GoToConjuros);  // Forja
            UnityEventTools.AddPersistentListener(edificioBtns[4].onClick, ctrl.GoToShop);      // Mercado
            UnityEventTools.AddPersistentListener(edificioBtns[5].onClick, ctrl.GoToGacha);     // Biblioteca
            UnityEventTools.AddPersistentListener(edificioBtns[6].onClick, ctrl.GoToGacha);     // Altar
            UnityEventTools.AddPersistentListener(edificioBtns[7].onClick, ctrl.GoToClan);      // Taverna
            UnityEventTools.AddPersistentListener(edificioBtns[8].onClick, ctrl.GoToDungeon);   // Mazmorra

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

            // HUD Acciones
            UnityEventTools.AddPersistentListener(btnChatGO.GetComponent<Button>().onClick,     ctrl.GoToChat);
            UnityEventTools.AddPersistentListener(btnMailGO.GetComponent<Button>().onClick,     ctrl.GoToMail);
            UnityEventTools.AddPersistentListener(btnSettingsGO.GetComponent<Button>().onClick, ctrl.GoToSettings);
            UnityEventTools.AddPersistentListener(btnEventGO.GetComponent<Button>().onClick,    ctrl.GoToEvent);
            UnityEventTools.AddPersistentListener(avatarBtn.onClick,                            ctrl.GoToProfile);

            // =================================================================
            // Asignar SerializeFields en MainMenuController
            // =================================================================
            var soCtrl = new SerializedObject(ctrl);

            soCtrl.FindProperty("_subIconosAbanico").objectReferenceValue = abanicoGO;

            // Edificios
            soCtrl.FindProperty("_btnCampaign").objectReferenceValue  = edificioBtns[0];
            soCtrl.FindProperty("_btnHeroes").objectReferenceValue    = edificioBtns[2];
            soCtrl.FindProperty("_btnGacha").objectReferenceValue     = edificioBtns[6];
            soCtrl.FindProperty("_btnArena").objectReferenceValue     = null;  // sin edificio arena por ahora
            soCtrl.FindProperty("_btnShop").objectReferenceValue      = edificioBtns[4];
            soCtrl.FindProperty("_btnClan").objectReferenceValue      = edificioBtns[7];
            soCtrl.FindProperty("_btnMissions").objectReferenceValue  = null;
            soCtrl.FindProperty("_btnDungeon").objectReferenceValue   = edificioBtns[8];
            soCtrl.FindProperty("_btnConjuros").objectReferenceValue  = edificioBtns[3];

            // Accesos rapidos
            soCtrl.FindProperty("_btnTriloguzano").objectReferenceValue    = accesoBtns[0];
            soCtrl.FindProperty("_btnTower").objectReferenceValue          = accesoBtns[1];
            soCtrl.FindProperty("_btnWorldBoss").objectReferenceValue      = accesoBtns[2];
            soCtrl.FindProperty("_btnMazmorraRapido").objectReferenceValue = accesoBtns[3];
            soCtrl.FindProperty("_btnEvent").objectReferenceValue          = accesoBtns[4];
            soCtrl.FindProperty("_btnProfile").objectReferenceValue        = accesoBtns[5];

            // HUD Acciones
            soCtrl.FindProperty("_btnChat").objectReferenceValue     = btnChatGO.GetComponent<Button>();
            soCtrl.FindProperty("_btnMail").objectReferenceValue     = btnMailGO.GetComponent<Button>();
            soCtrl.FindProperty("_btnSettings").objectReferenceValue = btnSettingsGO.GetComponent<Button>();

            soCtrl.ApplyModifiedProperties();

            // =================================================================
            // HUD prefab
            // =================================================================
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HUD_PATH);
            if (hudPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(hudPrefab);

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
            Debug.Log("[SetupMainMenu] MainMenuScene (5 zonas) configurada correctamente");
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

        static GameObject MakeAccionBtn(GameObject parent, string name,
            string label, float xMin, float xMax)
        {
            var go  = Child(parent, name);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.05f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            Anch(go, xMin, 0.10f, xMax, 0.90f);

            var lblGO  = Child(go, "Label");
            Anch(lblGO, 0f, 0f, 1f, 1f);
            var lbl    = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text      = label;
            lbl.fontSize  = 13;
            lbl.color     = Color.white;
            lbl.alignment = TextAlignmentOptions.Center;

            return go;
        }

        static GameObject MakePill(GameObject parent, string name,
            float xMin, float xMax, string defaultText, CurrencyType type)
        {
            var go   = Child(parent, name);
            Anch(go, xMin, 0f, xMax, 1f);
            var pill = go.AddComponent<ReinoOscuridad.UI.HUD.CurrencyPill>();

            var iconGO = Child(go, "IconPlaceholder");
            iconGO.AddComponent<Image>().color = Hex("FFD700");
            Anch(iconGO, 0.02f, 0.15f, 0.28f, 0.85f);

            var valGO  = Child(go, "ValueText");
            var valTMP = valGO.AddComponent<TextMeshProUGUI>();
            valTMP.text      = defaultText;
            valTMP.fontSize  = 14;
            valTMP.alignment = TextAlignmentOptions.Center;
            valTMP.color     = Color.white;
            Anch(valGO, 0.30f, 0.10f, 0.82f, 0.90f);

            var plusGO  = Child(go, "PlusButton");
            var plusImg = plusGO.AddComponent<Image>();
            plusImg.color = new Color(1f, 1f, 1f, 0.1f);
            var plusBtn = plusGO.AddComponent<Button>();
            plusBtn.targetGraphic = plusImg;
            Anch(plusGO, 0.83f, 0.10f, 0.99f, 0.90f);

            var plusTextGO  = Child(plusGO, "PlusText");
            var plusTMP     = plusTextGO.AddComponent<TextMeshProUGUI>();
            plusTMP.text      = "+";
            plusTMP.fontSize  = 14;
            plusTMP.alignment = TextAlignmentOptions.Center;
            plusTMP.color     = Color.white;
            Anch(plusTextGO, 0f, 0f, 1f, 1f);

            var so = new SerializedObject(pill);
            so.FindProperty("_valueText").objectReferenceValue  = valTMP;
            so.FindProperty("_plusButton").objectReferenceValue = plusBtn;
            so.FindProperty("_currencyType").enumValueIndex     = (int)type;
            so.ApplyModifiedProperties();

            return go;
        }
    }
}
