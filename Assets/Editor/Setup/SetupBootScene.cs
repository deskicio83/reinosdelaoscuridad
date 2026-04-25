#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ReinoOscuridad.Systems;
using ReinoOscuridad.UI.Boot;

/// Editor Script — reconstruye la UI de BootScene.
/// Preserva los GameObjects de sistemas (GameManager, UIManager, etc.)
/// y reemplaza toda la UI con el layout correcto.
/// Menú: Tools → Reino Oscuridad → 9. Setup BootScene
public static class SetupBootScene
{
    private const string SCENE_PATH = "Assets/Scenes/BootScene.unity";

    // GameObjects de sistema que NO deben borrarse
    private static readonly HashSet<string> SYSTEM_GO_NAMES = new()
    {
        "GameManager", "UIManager", "PlayerDataSystem", "EconomySystem",
        "AuthSystem", "DataStorageSystem", "HeroProgressionSystem",
        "GearSystem", "PlayerProgressionSystem", "CombatSystem",
        "AudioManager", "NotificationManager"
    };

    [MenuItem("Tools/Reino Oscuridad/9. Setup BootScene")]
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

        Build();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[SetupBoot] BootScene configurada correctamente.");
    }

    // ── Construcción ───────────────────────────────────────────────────────

    private static void Build()
    {
        // ── Eliminar UI existente — preservar sistemas ─────────────────────

        var toDestroy = new List<GameObject>();
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (go.transform.parent != null) continue;           // solo raíz
            if (SYSTEM_GO_NAMES.Contains(go.name))   continue;  // preservar sistemas
            toDestroy.Add(go);
        }
        foreach (var go in toDestroy) Object.DestroyImmediate(go);

        // ── Main Camera ────────────────────────────────────────────────────

        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic      = true;
        cam.orthographicSize  = 5f;
        cam.clearFlags        = CameraClearFlags.SolidColor;
        cam.backgroundColor   = new Color(0.039f, 0.039f, 0.078f); // #0A0A14
        cam.transform.position = new Vector3(0f, 0f, -10f);
        camGO.AddComponent<AudioListener>();

        // ── EventSystem ────────────────────────────────────────────────────

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<InputSystemUIInputModule>();

        // ── Canvas ─────────────────────────────────────────────────────────

        var canvasGO = new GameObject("BootCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight   = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Fondo ──────────────────────────────────────────────────────────

        MakePanel(canvasGO.transform, "FondoBoot",
            Vector2.zero, Vector2.one, new Color(0.039f, 0.039f, 0.078f)); // #0A0A14

        // ── Logo ───────────────────────────────────────────────────────────

        var logoGO = new GameObject("LogoPanel");
        logoGO.transform.SetParent(canvasGO.transform, false);
        SetAnchors(logoGO, new Vector2(0.25f, 0.55f), new Vector2(0.75f, 0.90f));

        var logoTxt = MakeTMP(logoGO.transform, "LogoText",
            "REINO DE LA OSCURIDAD",
            32f, FontStyles.Bold, TextAlignmentOptions.Center,
            new Color(0.659f, 0.333f, 0.965f)); // #A855F7
        SetAnchors(logoTxt.gameObject, Vector2.zero, Vector2.one);

        var subTxt = MakeTMP(logoGO.transform, "SubtituloText",
            "El mal nunca duerme. Nosotros, a veces.",
            14f, FontStyles.Normal, TextAlignmentOptions.Center,
            new Color(0.420f, 0.447f, 0.502f)); // #6B7280
        SetAnchors(subTxt.gameObject, new Vector2(0.10f, 0f), new Vector2(0.90f, 0.30f));

        // ── LoadingPanel ───────────────────────────────────────────────────

        var loadingPanel = new GameObject("LoadingPanel");
        loadingPanel.transform.SetParent(canvasGO.transform, false);
        SetAnchors(loadingPanel, new Vector2(0.30f, 0.35f), new Vector2(0.70f, 0.50f));
        loadingPanel.SetActive(true);

        var loadingTxt = MakeTMP(loadingPanel.transform, "LoadingText",
            "Inicializando...",
            14f, FontStyles.Normal, TextAlignmentOptions.Center,
            new Color(0.612f, 0.639f, 0.686f)); // #9CA3AF
        SetAnchors(loadingTxt.gameObject, Vector2.zero, Vector2.one);

        // ── LoginPanel ─────────────────────────────────────────────────────

        var loginPanel = new GameObject("LoginPanel");
        loginPanel.transform.SetParent(canvasGO.transform, false);
        loginPanel.AddComponent<Image>().color = new Color(0.059f, 0.059f, 0.102f); // #0F0F1A
        SetAnchors(loginPanel, new Vector2(0.20f, 0.10f), new Vector2(0.80f, 0.52f));
        loginPanel.SetActive(false);

        var tituloLogin = MakeTMP(loginPanel.transform, "TituloLogin",
            "Elige como entrar al reino",
            14f, FontStyles.Normal, TextAlignmentOptions.Center,
            new Color(0.612f, 0.639f, 0.686f)); // #9CA3AF
        SetAnchors(tituloLogin.gameObject, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f));

        var btnGoogle     = MakeLoginButton(loginPanel.transform, "BtnGoogle",
            "Entrar con Google",
            new Vector2(0.08f, 0.65f), new Vector2(0.92f, 0.82f),
            new Color(0.102f, 0.102f, 0.180f)); // #1A1A2E

        var btnApple      = MakeLoginButton(loginPanel.transform, "BtnApple",
            "Entrar con Apple",
            new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.61f),
            new Color(0.102f, 0.102f, 0.180f));

        var btnGuest      = MakeLoginButton(loginPanel.transform, "BtnGuest",
            "Continuar como Invitado",
            new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.40f),
            new Color(0.298f, 0.114f, 0.584f)); // #4C1D95

        var btnReintentar = MakeLoginButton(loginPanel.transform, "BtnReintentar",
            "Reintentar conexion",
            new Vector2(0.15f, 0.04f), new Vector2(0.85f, 0.18f),
            new Color(0.180f, 0.102f, 0.102f), // #2E1A1A
            new Color(0.937f, 0.267f, 0.267f),  // #EF4444
            12f);
        btnReintentar.gameObject.SetActive(false);

        var btnDevMode = MakeLoginButton(loginPanel.transform, "BtnDevMode",
            "MODO DEV (sin login)",
            new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.19f),
            new Color(0.051f, 0.180f, 0.051f), // #0D2E0D
            new Color(0.290f, 0.871f, 0.502f),  // #4ADE80
            12f);

        // ── ErrorPanel ─────────────────────────────────────────────────────

        var errorPanel = new GameObject("ErrorPanel");
        errorPanel.transform.SetParent(canvasGO.transform, false);
        errorPanel.AddComponent<Image>().color = new Color(0.165f, 0.039f, 0.039f); // #2A0A0A
        SetAnchors(errorPanel, new Vector2(0.15f, 0.30f), new Vector2(0.85f, 0.55f));
        errorPanel.SetActive(false);

        var errorTxt = MakeTMP(errorPanel.transform, "ErrorText",
            "Error de conexion",
            12f, FontStyles.Normal, TextAlignmentOptions.Center,
            new Color(0.937f, 0.267f, 0.267f)); // #EF4444
        SetAnchors(errorTxt.gameObject, new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.80f));

        // ── CombatSystem (DDOL — si no existe ya en la escena) ────────────────

        if (Object.FindAnyObjectByType<CombatSystem>() == null)
        {
            var combatGO = new GameObject("CombatSystem");
            combatGO.AddComponent<CombatSystem>();
        }

        // ── BootController ─────────────────────────────────────────────────

        var controllerGO = new GameObject("BootController");
        var controller   = controllerGO.AddComponent<BootSceneController>();

        // Cablear SerializeFields
        var so = new SerializedObject(controller);
        so.FindProperty("_loadingPanel").objectReferenceValue = loadingPanel;
        so.FindProperty("_loginPanel")  .objectReferenceValue = loginPanel;
        so.FindProperty("_errorPanel")  .objectReferenceValue = errorPanel;
        so.FindProperty("_loadingText") .objectReferenceValue = loadingTxt;
        so.FindProperty("_errorText")   .objectReferenceValue = errorTxt;
        so.FindProperty("_btnGoogle")   .objectReferenceValue = btnGoogle;
        so.FindProperty("_btnApple")    .objectReferenceValue = btnApple;
        so.FindProperty("_btnGuest")    .objectReferenceValue = btnGuest;
        so.FindProperty("_btnRetry")    .objectReferenceValue = btnReintentar;
        so.FindProperty("_btnDevMode")  .objectReferenceValue = btnDevMode;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Cablear onClick via UnityEventTools
        UnityEventTools.AddPersistentListener(btnGoogle.onClick,     controller.LoginWithGoogle);
        UnityEventTools.AddPersistentListener(btnApple.onClick,      controller.LoginWithApple);
        UnityEventTools.AddPersistentListener(btnGuest.onClick,      controller.LoginAsGuest);
        UnityEventTools.AddPersistentListener(btnReintentar.onClick, controller.RetryInit);
        UnityEventTools.AddPersistentListener(btnDevMode.onClick,    controller.DevModeLogin);
    }

    // ── Utilidades ─────────────────────────────────────────────────────────

    private static GameObject MakePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        SetAnchors(go, anchorMin, anchorMax);
        return go;
    }

    private static Button MakeLoginButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax,
        Color bgColor,
        Color? textColor = null,
        float fontSize   = 14f)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgColor;
        var btn = go.AddComponent<Button>();
        SetAnchors(go, anchorMin, anchorMax);

        var lblGO  = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        var tmp  = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = textColor ?? new Color(0.914f, 0.835f, 1f); // #E9D5FF
        tmp.alignment = TextAlignmentOptions.Center;
        var lrt = lblGO.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        return btn;
    }

    private static TextMeshProUGUI MakeTMP(Transform parent, string name, string text,
        float fontSize, FontStyles style, TextAlignmentOptions align, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color     = color;
        return tmp;
    }

    private static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
