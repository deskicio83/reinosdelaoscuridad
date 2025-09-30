#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CreateLegionDetailPanel
{
    [MenuItem("Tools/Legión/Crear \"LegionDetailPanel\" (pantalla completa)")]
    public static void CreatePanel()
    {
        // 1) Canvas + EventSystem
        var canvas = Object.FindObjectOfType<Canvas>();
        if (!canvas)
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }
        EnsureEventSystem();

        var canvasRT = canvas.transform as RectTransform;

        // Si ya existe, seleccionarlo y salir
        var existing = canvas.transform.Find("LegionDetailPanel");
        if (existing)
        {
            Selection.activeTransform = existing;
            EditorGUIUtility.PingObject(existing);
            Debug.Log("Ya existe un LegionDetailPanel en la escena. Lo he seleccionado.");
            return;
        }

        // 2) Root del panel
        var root = new GameObject("LegionDetailPanel", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvasRT, false);
        var rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.pivot = new Vector2(0.5f, 0.5f);
        rootRT.anchoredPosition = Vector2.zero;
        rootRT.sizeDelta = Vector2.zero;

        // Fondo semitransparente y que bloquee clics
        var bg = root.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        bg.raycastTarget = true;

        // 3) Cabecera
        var header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(rootRT, false);
        var headerRT = header.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0f, 1f);
        headerRT.anchorMax = new Vector2(1f, 1f);
        headerRT.pivot = new Vector2(0.5f, 1f);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta = new Vector2(0f, 96f);
        header.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);

        // Título (TMP)
        var titleGO = new GameObject("TxtTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(headerRT, false);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.offsetMin  = new Vector2(24f, 16f);
        titleRT.offsetMax  = new Vector2(-160f, -16f);
        var titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
        titleTMP.text = "Detalle de Héroe";
        titleTMP.fontSize = 48;
        titleTMP.enableAutoSizing = true;
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        titleTMP.color = Color.white;

        // Botón Volver (oculta el panel)
        var btnGO = new GameObject("BtnVolver", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGO.transform.SetParent(headerRT, false);
        var btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(1f, 0.5f);
        btnRT.anchorMax = new Vector2(1f, 0.5f);
        btnRT.pivot     = new Vector2(1f, 0.5f);
        btnRT.sizeDelta = new Vector2(160f, 56f);
        btnRT.anchoredPosition = new Vector2(-16f, 0f);
        btnGO.GetComponent<Image>().color = new Color(1f,1f,1f,0.08f);
        var btn = btnGO.GetComponent<Button>();

        // Texto del botón
        var btnTextGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextGO.transform.SetParent(btnRT, false);
        var btnTextRT = btnTextGO.GetComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.offsetMin = Vector2.zero;
        btnTextRT.offsetMax = Vector2.zero;
        var btnTMP = btnTextGO.GetComponent<TextMeshProUGUI>();
        btnTMP.text = "Volver";
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.enableAutoSizing = true;
        btnTMP.fontSize = 30;

        // 4) Área de contenido (placeholder)
        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(rootRT, false);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 0f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.offsetMin = new Vector2(16f, 16f);
        contentRT.offsetMax = new Vector2(-16f, -112f);

        // 5) Componente funcional
        var detail = root.AddComponent<LegionDetailPanelUI>();

        // Asignar campo serializado 'rootPanel' del detalle
        var so = new SerializedObject(detail);
        so.FindProperty("rootPanel").objectReferenceValue = root;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Conectar botón a LegionDetailPanelUI.Hide()
        UnityEventTools.AddPersistentListener(btn.onClick, detail.Hide);

        // 6) Poner arriba del todo y ocultarlo
        root.transform.SetAsLastSibling();
        root.SetActive(false);

        // 7) (Opcional) asignarlo al LegionSceneController si existe
        var sceneCtrl = Object.FindObjectOfType<LegionSceneController>();
        if (sceneCtrl)
        {
            var soCtrl = new SerializedObject(sceneCtrl);
            var prop = soCtrl.FindProperty("legionDetailPanel");
            if (prop != null)
            {
                prop.objectReferenceValue = detail;
                soCtrl.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // Seleccionar en jerarquía
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);
        Debug.Log("✅ LegionDetailPanel creado (pantalla completa) y listo para usar.");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() != null) return;
        var es = new GameObject("EventSystem",
            typeof(UnityEngine.EventSystems.EventSystem),
            typeof(UnityEngine.EventSystems.StandaloneInputModule));
        Object.DontDestroyOnLoad(es); // por si cambias de escena en editor
    }
}
#endif
