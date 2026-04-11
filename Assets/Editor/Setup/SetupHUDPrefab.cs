using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ReinoOscuridad.UI.HUD;
using ReinoOscuridad.Data;

namespace ReinoOscuridad.Editor.Setup
{
    /// Crea o sobreescribe Assets/Prefabs/UI/HUD.prefab con anclas relativas.
    /// Menú: Tools → Reino Oscuridad → 1. Setup HUD Prefab
    public static class SetupHUDPrefab
    {
        private const string PREFAB_PATH = "Assets/Prefabs/UI/HUD.prefab";

        [MenuItem("Tools/Reino Oscuridad/1. Setup HUD Prefab")]
        public static void Setup()
        {
            // ── Raíz: Canvas HUD ──────────────────────────────────────────────
            var root = new GameObject("HUD");

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution  = new Vector2(1280, 720);
            scaler.matchWidthOrHeight   = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            var hudController = root.AddComponent<HUDController>();
            var hudIcons      = root.AddComponent<HUDIcons>();

            // ── Background ────────────────────────────────────────────────────
            var bg = Child(root, "Background");
            bg.AddComponent<Image>().color = new Color(0.039f, 0.039f, 0.039f, 180f / 255f);
            Anch(bg, 0f, 0.90f, 1f, 1f);

            // ── ZonaJugador ───────────────────────────────────────────────────
            var zonaJ = Child(root, "ZonaJugador");
            Anch(zonaJ, 0f, 0.90f, 0.35f, 1f);

            var avatar = Child(zonaJ, "AvatarPlaceholder");
            avatar.AddComponent<Image>().color = Hex("444444");
            Anch(avatar, 0.02f, 0.10f, 0.18f, 0.90f);

            var nombreGO  = Child(zonaJ, "NombreText");
            var nombreTMP = nombreGO.AddComponent<TextMeshProUGUI>();
            nombreTMP.text          = "DarkLord42";
            nombreTMP.fontSize      = 18;
            nombreTMP.fontStyle     = FontStyles.Bold;
            nombreTMP.color         = Color.white;
            nombreTMP.overflowMode  = TextOverflowModes.Truncate;
            nombreTMP.alignment     = TextAlignmentOptions.Left;
            Anch(nombreGO, 0.20f, 0.50f, 0.98f, 0.95f);

            var nivelGO  = Child(zonaJ, "NivelText");
            var nivelTMP = nivelGO.AddComponent<TextMeshProUGUI>();
            nivelTMP.text      = "Nv. 15";
            nivelTMP.fontSize  = 14;
            nivelTMP.color     = Hex("AAAAAA");
            nivelTMP.alignment = TextAlignmentOptions.Left;
            Anch(nivelGO, 0.20f, 0.05f, 0.98f, 0.50f);

            // ── ZonaMonedas ───────────────────────────────────────────────────
            var zonaM = Child(root, "ZonaMonedas");
            Anch(zonaM, 0.35f, 0.90f, 0.80f, 1f);

            var energyPillGO    = MakePill(zonaM, "Pill_Energy",    0f,     0.33f, "120/120", CurrencyType.Energy);
            var goldPillGO      = MakePill(zonaM, "Pill_Gold",      0.33f,  0.66f, "7.690",   CurrencyType.Gold);
            var caosiferaPillGO = MakePill(zonaM, "Pill_Caosifera", 0.66f,  1f,    "385",     CurrencyType.Caosifera);

            // ── ZonaIconos ────────────────────────────────────────────────────
            var zonaI = Child(root, "ZonaIconos");
            Anch(zonaI, 0.80f, 0.90f, 1f, 1f);

            // Texto ASCII — los iconos gráficos se añadirán con Sprites en S32
            var btnChatGO     = MakeIconBtn(zonaI, "Btn_Chat",     "Chat", 0.02f, 0.34f);
            var btnMailGO     = MakeIconBtn(zonaI, "Btn_Mail",     "Mail", 0.35f, 0.67f);
            var btnSettingsGO = MakeIconBtn(zonaI, "Btn_Settings", "Set.", 0.68f, 0.99f);

            // MailBadge
            var badgeGO  = Child(btnMailGO, "MailBadge");
            badgeGO.AddComponent<Image>().color = Hex("E53E3E");
            Anch(badgeGO, 0.60f, 0.55f, 1f, 1f);

            var badgeTextGO  = Child(badgeGO, "BadgeText");
            var badgeTMP     = badgeTextGO.AddComponent<TextMeshProUGUI>();
            badgeTMP.text      = "3";
            badgeTMP.fontSize  = 10;
            badgeTMP.color     = Color.white;
            badgeTMP.alignment = TextAlignmentOptions.Center;
            Anch(badgeTextGO, 0f, 0f, 1f, 1f);

            // ── Asignar referencias via SerializedObject ───────────────────────
            var soCtrl = new SerializedObject(hudController);
            soCtrl.FindProperty("_energyPill").objectReferenceValue    = energyPillGO.GetComponent<CurrencyPill>();
            soCtrl.FindProperty("_goldPill").objectReferenceValue      = goldPillGO.GetComponent<CurrencyPill>();
            soCtrl.FindProperty("_caosiferaPill").objectReferenceValue = caosiferaPillGO.GetComponent<CurrencyPill>();
            soCtrl.ApplyModifiedProperties();

            var soIcons = new SerializedObject(hudIcons);
            soIcons.FindProperty("_chatButton").objectReferenceValue     = btnChatGO.GetComponent<Button>();
            soIcons.FindProperty("_mailButton").objectReferenceValue     = btnMailGO.GetComponent<Button>();
            soIcons.FindProperty("_settingsButton").objectReferenceValue = btnSettingsGO.GetComponent<Button>();
            soIcons.FindProperty("_mailBadge").objectReferenceValue      = badgeGO;
            soIcons.FindProperty("_mailBadgeText").objectReferenceValue  = badgeTMP;
            soIcons.ApplyModifiedProperties();

            // ── Guardar prefab ────────────────────────────────────────────────
            Directory.CreateDirectory("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out bool saved);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();

            Debug.Log(saved
                ? "[SetupHUD] HUD.prefab creado/actualizado correctamente"
                : "[SetupHUD] ERROR al guardar HUD.prefab");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

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

        static GameObject MakePill(GameObject parent, string name,
            float xMin, float xMax, string defaultText, CurrencyType type)
        {
            var go   = Child(parent, name);
            Anch(go, xMin, 0f, xMax, 1f);
            var pill = go.AddComponent<CurrencyPill>();

            // Icono
            var iconGO = Child(go, "IconPlaceholder");
            iconGO.AddComponent<Image>().color = Hex("FFD700");
            Anch(iconGO, 0.02f, 0.15f, 0.28f, 0.85f);

            // ValueText
            var valGO  = Child(go, "ValueText");
            var valTMP = valGO.AddComponent<TextMeshProUGUI>();
            valTMP.text      = defaultText;
            valTMP.fontSize  = 15;
            valTMP.alignment = TextAlignmentOptions.Center;
            valTMP.color     = Color.white;
            Anch(valGO, 0.30f, 0.10f, 0.82f, 0.90f);

            // PlusButton
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

            // Asignar en CurrencyPill
            var so = new SerializedObject(pill);
            so.FindProperty("_valueText").objectReferenceValue  = valTMP;
            so.FindProperty("_plusButton").objectReferenceValue = plusBtn;
            so.FindProperty("_currencyType").enumValueIndex     = (int)type;
            so.ApplyModifiedProperties();

            return go;
        }

        static GameObject MakeIconBtn(GameObject parent, string name,
            string emoji, float xMin, float xMax)
        {
            var go  = Child(parent, name);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.05f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            Anch(go, xMin, 0.10f, xMax, 0.90f);

            var lblGO  = Child(go, "Label");
            var lblTMP = lblGO.AddComponent<TextMeshProUGUI>();
            lblTMP.text      = emoji;
            lblTMP.fontSize  = 20;
            lblTMP.alignment = TextAlignmentOptions.Center;
            lblTMP.color     = Color.white;
            Anch(lblGO, 0f, 0f, 1f, 1f);

            return go;
        }
    }
}
