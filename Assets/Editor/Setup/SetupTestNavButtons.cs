#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ReinoOscuridad.Utils;

/// Editor Script — añade botones de navegación temporal a SampleScene para pruebas.
/// Menú: Tools → Reino Oscuridad → DEV: Setup Test Nav Buttons
///
/// Crea un Canvas de debug con botones "→ CampaignScene", "→ CombatScene", etc.
/// SOLO para uso en desarrollo. NO añadir a build de producción.
public static class SetupTestNavButtons
{
    [MenuItem("Tools/Reino Oscuridad/DEV: Setup Test Nav Buttons")]
    public static void Run()
    {
        // Abrir SampleScene si no está activa
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != "SampleScene")
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        }

        // Eliminar canvas de debug anterior si existe
        var existing = GameObject.Find("DEV_NavButtons");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
            Debug.Log("[SetupTestNav] Canvas anterior eliminado.");
        }

        // EventSystem — solo si no existe ya
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<InputSystemUIInputModule>();
        }

        // Canvas de debug
        var canvasGO = new GameObject("DEV_NavButtons");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // encima de todo

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Título "DEV"
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
        titleTxt.text      = "DEV — Navegación rápida";
        titleTxt.fontSize  = 14f;
        titleTxt.color     = new Color(1f, 0.8f, 0.2f, 0.9f);
        titleTxt.alignment = TextAlignmentOptions.MidlineLeft;
        SetAnchors(titleGO, new Vector2(0.01f, 0.92f), new Vector2(0.35f, 0.99f));

        // Botones de escena
        string[] scenes =
        {
            "CampaignScene",
            "CombatScene",
            "MainMenuScene",
            "HeroScene",
            "GachaScene",
        };

        float btnH  = 0.07f;
        float gap   = 0.005f;
        float top   = 0.91f;

        for (int i = 0; i < scenes.Length; i++)
        {
            string sceneName = scenes[i];
            float  yMax      = top - i * (btnH + gap);
            float  yMin      = yMax - btnH;

            var btn = MakeButton(canvasGO.transform, $"Btn_{sceneName}",
                $"→ {sceneName}",
                new Vector2(0.01f, yMin), new Vector2(0.22f, yMax),
                new Color(0.10f, 0.08f, 0.20f, 0.92f));

            // El listener se añade via NavButtonHandler en runtime
            var handler = btn.AddComponent<NavButtonHandler>();
            handler.targetScene = sceneName;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[SetupTestNav] Botones de navegación añadidos a SampleScene. ¡SOLO PARA DEV!");
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
        tmp.fontSize  = 13f;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        var lRT       = lblGO.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero;
        lRT.anchorMax = Vector2.one;
        lRT.offsetMin = new Vector2(8, 0);
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
}
#endif
