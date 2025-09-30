// Assets/Scripts/Scenes/Mastery/UILineConnector.cs
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dibuja una “línea” entre dos RectTransform usando un Image estirado y rotado.
/// - Coloca la línea detrás de los nodos (usa SetAsFirstSibling afuera si quieres).
/// - No depende de DOTween ni de ninguna otra clase del proyecto.
/// - Compatible con tu HeroMasteryController: usa SetLit/SetPreview para brillo.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class UILineConnector : MonoBehaviour
{
    [Header("EndPoints")]
    public RectTransform from;          // padre
    public RectTransform to;            // hijo

    [Header("Style")]
    [Min(1f)] public float thickness = 8f;
    public Color baseColor = new Color(1f, 1f, 1f, 0.5f);

    [Header("Insets")]
    [Tooltip("Si está activo, los insets se calculan como % de la distancia (0..1).")]
    public bool useRelativeInset = true;
    [Range(0f, 0.3f)] public float insetPercent = 0f; // 0 = une exactamente de borde a borde
    public float startInset = 0f;                     // usados si useRelativeInset = false
    public float endInset = 0f;

    RectTransform _rt;
    Image _img;
    bool _dirty = true;

    void Awake()
    {
        _rt = transform as RectTransform;
        _img = GetComponent<Image>();
        if (_img) _img.color = baseColor;
    }

    void OnEnable() { _dirty = true; }
    void OnTransformParentChanged() { _dirty = true; }
    void OnRectTransformDimensionsChange() { _dirty = true; }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!_rt) _rt = transform as RectTransform;
        if (!_img) _img = GetComponent<Image>();
        if (_img) _img.color = baseColor;
        _dirty = true;
    }
#endif

    void LateUpdate()
    {
        if (_dirty) { _dirty = false; UpdateLine(); }
    }

    /// <summary>Fuerza un recálculo inmediato (la usa tu controlador tras relayout).</summary>
    public void ForceUpdateNow()
    {
        _dirty = false;
        UpdateLine();
    }

    /// <summary>Activa/desactiva brillo (alfa 1.0 vs 0.12) manteniendo el color base.</summary>
    public void SetLit(bool strong)
    {
        if (!_img) return;
        float a = strong ? 1f : 0.12f;
        _img.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
    }

    /// <summary>Modo “preview” muy tenue (alfa 0.12).</summary>
    public void SetPreview(bool preview)
    {
        if (!_img) return;
        float a = preview ? 0.12f : 1f;
        _img.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
    }

    void UpdateLine()
    {
        if (from == null || to == null || _rt == null) return;
        var parent = _rt.parent as RectTransform;
        if (!parent) return;

        // Unimos centro-inferior del padre con centro-superior del hijo
        Vector3 worldA = GetWorldPoint(from, 0.5f, 0f); // bottom-center
        Vector3 worldB = GetWorldPoint(to,   0.5f, 1f); // top-center

        Vector2 a = WorldToLocal(parent, worldA);
        Vector2 b = WorldToLocal(parent, worldB);

        Vector2 dir = b - a;
        float len = dir.magnitude;
        if (len < 0.5f)
        {
            _rt.sizeDelta = Vector2.zero;
            return;
        }

        float sInset = useRelativeInset ? insetPercent * len : startInset;
        float eInset = useRelativeInset ? insetPercent * len : endInset;

        Vector2 aa = a + dir.normalized * sInset;
        Vector2 bb = b - dir.normalized * eInset;
        dir = bb - aa;
        len = dir.magnitude;

        // RectTransform como “barra” horizontal pivotando a la izquierda
        _rt.pivot = new Vector2(0f, 0.5f);
        _rt.anchorMin = _rt.anchorMax = new Vector2(0f, 1f); // mismo espacio que tus nodos
        _rt.anchoredPosition = new Vector2(aa.x, aa.y);
        _rt.sizeDelta = new Vector2(len, thickness);
        _rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        // Asegura que el color base se respeta (por si cambias en runtime)
        if (_img) _img.color = new Color(baseColor.r, baseColor.g, baseColor.b, _img.color.a);
    }

    static Vector3 GetWorldPoint(RectTransform rt, float nx, float ny)
    {
        // Esquinas en mundo
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        var min = corners[0];
        var max = corners[2];
        return new Vector3(
            Mathf.Lerp(min.x, max.x, nx),
            Mathf.Lerp(min.y, max.y, ny),
            0f
        );
    }

    static Vector2 WorldToLocal(RectTransform parent, Vector3 world)
    {
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, world, null, out local);
        return local;
    }
}
