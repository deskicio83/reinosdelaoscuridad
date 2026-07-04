#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class EditorUIBuilder
{
    public static GameObject Child(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    public static GameObject Child(GameObject parent, string name) => Child(parent.transform, name);

    public static void Anch(GameObject go, float x0, float y0, float x1, float y1)
    {
        var rt       = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(x0, y0);
        rt.anchorMax = new Vector2(x1, y1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static void SetAnchors(GameObject go, Vector2 min, Vector2 max)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static Image Img(GameObject go, Color col, float alpha = 1f)
    {
        var img   = go.AddComponent<Image>();
        img.color = new Color(col.r, col.g, col.b, alpha);
        return img;
    }

    public static TMP_Text Txt(GameObject go, string text, float size, Color col,
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

    public static Color Hex(string h)
    {
        ColorUtility.TryParseHtmlString(h, out var c);
        return c;
    }
}
#endif
