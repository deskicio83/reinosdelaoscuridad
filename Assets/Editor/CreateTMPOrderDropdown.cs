// Assets/Editor/CreateTMPOrderDropdown.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CreateTMPOrderDropdown
{
    [MenuItem("GameObject/Legion/Crear DdOrden (TMP Dropdown)", false, 10)]
    public static void CreateTMPDropdown()
    {
        // ===== Sprites por defecto (builtin) =====
        Sprite uiSprite       = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite bgSprite       = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        Sprite maskSprite     = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd");
        Sprite arrowSprite    = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
        Sprite checkSprite    = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");

        // ===== Fuente TMP =====
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            var all = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (all != null && all.Length > 0) font = all[0];
        }

        // ===== Nodo padre =====
        Transform parent = Selection.activeTransform;
        if (parent == null)
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas != null) parent = canvas.transform;
        }

        // ===== Raíz =====
        var rootGO = new GameObject("DdOrden", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_Dropdown));
        Undo.RegisterCreatedObjectUndo(rootGO, "Crear DdOrden");
        if (parent != null) rootGO.transform.SetParent(parent, false);

        var rootRT  = rootGO.GetComponent<RectTransform>();
        var rootImg = rootGO.GetComponent<Image>();
        var dd      = rootGO.GetComponent<TMP_Dropdown>();

        // Posición/tamaño básicos (ajústalo si quieres desde GUI)
        rootRT.anchorMin = new Vector2(1f, 1f);
        rootRT.anchorMax = new Vector2(1f, 1f);
        rootRT.pivot     = new Vector2(1f, 1f);
        rootRT.sizeDelta = new Vector2(240f, 60f);
        rootRT.anchoredPosition = Vector2.zero;

        rootImg.sprite = uiSprite;
        rootImg.type   = Image.Type.Sliced;

        // ===== Label (caption) =====
        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(rootGO.transform, false);
        var labelRT  = labelGO.GetComponent<RectTransform>();
        var labelTMP = labelGO.GetComponent<TextMeshProUGUI>();
        labelRT.anchorMin = new Vector2(0f, 0f);
        labelRT.anchorMax = new Vector2(1f, 1f);
        labelRT.offsetMin = new Vector2(10f, 6f);
        labelRT.offsetMax = new Vector2(-25f, -7f);
        labelTMP.text = "Elemento";
        if (font) labelTMP.font = font;
        labelTMP.fontSize = 40;
        labelTMP.enableAutoSizing = false;
        labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
        labelTMP.raycastTarget = false;

        // ===== Flecha =====
        var arrowGO = new GameObject("Arrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        arrowGO.transform.SetParent(rootGO.transform, false);
        var arrowRT  = arrowGO.GetComponent<RectTransform>();
        var arrowImg = arrowGO.GetComponent<Image>();
        arrowRT.anchorMin = new Vector2(1f, 0.5f);
        arrowRT.anchorMax = new Vector2(1f, 0.5f);
        arrowRT.sizeDelta = new Vector2(20f, 20f);
        arrowRT.anchoredPosition = new Vector2(-10f, 0f);
        arrowImg.sprite = arrowSprite;
        arrowImg.raycastTarget = false;

        // ===== Template =====
        var templateGO = new GameObject("Template", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        templateGO.transform.SetParent(rootGO.transform, false);
        var templateRT  = templateGO.GetComponent<RectTransform>();
        var templateImg = templateGO.GetComponent<Image>();
        var scrollRect  = templateGO.GetComponent<ScrollRect>();

        // Estructura y tamaño del panel
        templateRT.anchorMin = new Vector2(0f, 1f);
        templateRT.anchorMax = new Vector2(1f, 1f);
        templateRT.pivot     = new Vector2(0.5f, 1f);
        templateRT.offsetMin = new Vector2(0f, -296f); // Alto calculado para 4 opciones (ver doc)
        templateRT.offsetMax = new Vector2(0f, 0f);
        templateImg.sprite   = bgSprite;
        templateImg.type     = Image.Type.Sliced;

        // === Viewport ===
        var vpGO = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        vpGO.transform.SetParent(templateGO.transform, false);
        var vpRT   = vpGO.GetComponent<RectTransform>();
        var vpImg  = vpGO.GetComponent<Image>();
        var vpMask = vpGO.GetComponent<Mask>();
        vpRT.anchorMin = new Vector2(0f, 1f);
        vpRT.anchorMax = new Vector2(1f, 1f);
        vpRT.pivot     = new Vector2(0.5f, 1f);
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;
        vpImg.sprite   = maskSprite;
        vpImg.type     = Image.Type.Sliced;
        vpMask.showMaskGraphic = false;

        // === Content ===
        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(vpGO.transform, false);
        var contentRT   = contentGO.GetComponent<RectTransform>();
        var vlg         = contentGO.GetComponent<VerticalLayoutGroup>();
        var fitter      = contentGO.GetComponent<ContentSizeFitter>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;

        vlg.padding.left = 8; vlg.padding.right = 8; vlg.padding.top = 8; vlg.padding.bottom = 8;
        vlg.spacing = 8f;
        vlg.childControlHeight = true;
        vlg.childForceExpandHeight = false;

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // === Item (plantilla de opción) ===
        var itemGO = new GameObject("Item", typeof(RectTransform), typeof(CanvasRenderer), typeof(Toggle), typeof(Image), typeof(LayoutElement));
        itemGO.transform.SetParent(contentGO.transform, false);
        var itemRT   = itemGO.GetComponent<RectTransform>();
        var itemTgl  = itemGO.GetComponent<Toggle>();
        var itemImg  = itemGO.GetComponent<Image>();
        var itemLE   = itemGO.GetComponent<LayoutElement>();

        itemRT.anchorMin = new Vector2(0f, 0.5f);
        itemRT.anchorMax = new Vector2(1f, 0.5f);
        itemRT.sizeDelta = new Vector2(0f, 64f);
        itemImg.sprite   = uiSprite;
        itemImg.type     = Image.Type.Sliced;
        itemLE.preferredHeight = 64f;
        itemLE.minHeight       = 64f;
        itemLE.flexibleHeight  = 0f;

        // Checkmark
        var ckGO = new GameObject("Item Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        ckGO.transform.SetParent(itemGO.transform, false);
        var ckRT  = ckGO.GetComponent<RectTransform>();
        var ckImg = ckGO.GetComponent<Image>();
        ckRT.anchorMin = new Vector2(0f, 0.5f);
        ckRT.anchorMax = new Vector2(0f, 0.5f);
        ckRT.sizeDelta = new Vector2(20f, 20f);
        ckRT.anchoredPosition = new Vector2(14f, 0f);
        ckImg.sprite = checkSprite;

        // Item Label
        var itemLabelGO = new GameObject("Item Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        itemLabelGO.transform.SetParent(itemGO.transform, false);
        var itemLabelRT  = itemLabelGO.GetComponent<RectTransform>();
        var itemLabelTMP = itemLabelGO.GetComponent<TextMeshProUGUI>();
        itemLabelRT.anchorMin = new Vector2(0f, 0f);
        itemLabelRT.anchorMax = new Vector2(1f, 1f);
        itemLabelRT.offsetMin = new Vector2(40f, 0f); // espacio para el check
        itemLabelRT.offsetMax = new Vector2(-16f, 0f);
        itemLabelTMP.text = "Opción";
        if (font) itemLabelTMP.font = font;
        itemLabelTMP.fontSize = 40;
        itemLabelTMP.enableAutoSizing = false;
        itemLabelTMP.alignment = TextAlignmentOptions.MidlineLeft;

        // Toggle wiring
        itemTgl.targetGraphic = itemImg;
        itemTgl.graphic       = ckImg;

        // ScrollRect wiring (sin scroll)
        scrollRect.content  = contentRT;
        scrollRect.viewport = vpRT;
        scrollRect.horizontal = false;
        scrollRect.vertical   = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Template debe estar inactivo
        templateGO.SetActive(false);

        // ===== Enlazar campos del TMP_Dropdown =====
        dd.template   = templateRT;
        dd.captionText = labelTMP;
        dd.itemText    = itemLabelTMP;
        dd.options.Clear();
        dd.options.Add(new TMP_Dropdown.OptionData("Elemento"));
        dd.options.Add(new TMP_Dropdown.OptionData("Faccion"));
        dd.options.Add(new TMP_Dropdown.OptionData("Clase"));
        dd.options.Add(new TMP_Dropdown.OptionData("Estrellas"));
        dd.value = 0;
        dd.RefreshShownValue();

        // Seleccionar en Hierarchy
        Selection.activeObject = rootGO;

        Debug.Log("[Legion/Editor] DdOrden (TMP Dropdown) creado y configurado. Ajusta estilo final en Inspector si lo deseas.");
    }
}
#endif
