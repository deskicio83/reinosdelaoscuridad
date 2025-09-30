// Assets/Scripts/UI/Mastery/MasteryNodeButton.cs
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public enum MasteryVisualState { Locked, Unavailable, Available, Selected }

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class MasteryNodeButton : MonoBehaviour
{
    [Header("Refs")]
    public Button button;
    public Image icon;          // icono del nodo (hex/oct)
    public Image glowSelected;  // halo dorado al estar comprado
    public Image redDot;        // indicador de disponible
    public Image lockOverlay;   // candado/overlay de bloqueo

    [HideInInspector] public string nodeId;
    [HideInInspector] public string branch;
    [HideInInspector] public int tier;
    [HideInInspector] public int slot;

    [Header("Auto layout del contenido")]
    [SerializeField] private float innerPadding = 0f;
    [SerializeField] private Vector2 redDotSize = new Vector2(22f, 22f);
    [SerializeField] private bool autoRelayoutOnResize = true;

    private CanvasGroup _cg;
    private RectTransform _rt;

    void Awake()
    {
        _rt = transform as RectTransform;

        // Asegura CanvasGroup UNA sola vez (sin duplicarlo)
        if (!TryGetComponent(out _cg)) _cg = gameObject.AddComponent<CanvasGroup>();
        _cg.alpha = 1f;

        if (glowSelected) glowSelected.gameObject.SetActive(false);
        if (redDot)       redDot.gameObject.SetActive(false);
        if (lockOverlay)  lockOverlay.gameObject.SetActive(false);

        ApplyChildLayout(); // estira icono/overlays desde el inicio
    }

    void OnEnable()
    {
        if (autoRelayoutOnResize) ApplyChildLayout();
    }

    void OnRectTransformDimensionsChange()
    {
        if (autoRelayoutOnResize) ApplyChildLayout();
    }

    private void ApplyChildLayout()
    {
        if (icon)
        {
            var rtI = icon.rectTransform;
            StretchToParent(rtI, innerPadding);
            icon.preserveAspect = true;
        }
        if (glowSelected)
        {
            var rtG = glowSelected.rectTransform;
            StretchToParent(rtG, innerPadding);
        }
        if (lockOverlay)
        {
            var rtL = lockOverlay.rectTransform;
            StretchToParent(rtL, innerPadding);
        }
        if (redDot)
        {
            var rtD = redDot.rectTransform;
            rtD.anchorMin = rtD.anchorMax = new Vector2(1f, 1f);
            rtD.pivot = new Vector2(1f, 1f);
            rtD.sizeDelta = redDotSize;
            rtD.anchoredPosition = new Vector2(-Mathf.Max(2f, innerPadding * 0.5f), -Mathf.Max(2f, innerPadding * 0.5f));
        }
    }

    private static void StretchToParent(RectTransform child, float padding)
    {
        if (!child) return;
        child.anchorMin = Vector2.zero;
        child.anchorMax = Vector2.one;
        child.pivot = new Vector2(0.5f, 0.5f);
        child.offsetMin = new Vector2(padding, padding);
        child.offsetMax = new Vector2(-padding, -padding);
        child.localScale = Vector3.one;
        child.localRotation = Quaternion.identity;
    }

    // ===== API visual =====
    public void SetIcon(Sprite s)
    {
        if (icon) icon.sprite = s;
    }

    public void SetState(MasteryVisualState state, bool showDot)
    {
        if (glowSelected) glowSelected.gameObject.SetActive(state == MasteryVisualState.Selected);
        if (lockOverlay)  lockOverlay.gameObject.SetActive(state == MasteryVisualState.Locked);

        if (button) button.interactable = (state == MasteryVisualState.Available);
        if (redDot) redDot.gameObject.SetActive(showDot && state == MasteryVisualState.Available);

        // Asegura CanvasGroup sin duplicar, incluso si el prefab ya lo trae
        if ((_cg == null && !TryGetComponent(out _cg)) || _cg == null)
            _cg = gameObject.AddComponent<CanvasGroup>();

        float targetAlpha = (state == MasteryVisualState.Unavailable || state == MasteryVisualState.Locked) ? 0.55f : 1f;

        // Evita warnings de DOTween: no animes si el objeto no está activo o no hay _cg
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy || _cg == null)
        {
            if (_cg != null) _cg.alpha = targetAlpha;
        }
        else
        {
            _cg.DOKill();                 // mata tweens anteriores sobre este CanvasGroup
            _cg.DOFade(targetAlpha, 0.12f).SetUpdate(true);
        }

        if (state == MasteryVisualState.Selected)
        {
            transform.DOKill();
            transform.localScale = Vector3.one * 0.95f;
            transform.DOScale(1f, 0.18f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    public void ApplyBranchStyle(Color branchColor)
    {
        if (glowSelected != null)
        {
            var c = branchColor; c.a = 0.85f;
            glowSelected.color = c;
        }
    }

    public void PlayUnlockPulse()
    {
        transform.DOKill();
        transform.localScale = Vector3.one * 0.9f;
        transform.DOScale(1f, 0.22f).SetEase(Ease.OutBack).SetUpdate(true);
        if (glowSelected)
        {
            glowSelected.DOKill();
            glowSelected.gameObject.SetActive(true);
            var cg = glowSelected.GetComponent<CanvasGroup>();
            if (cg == null) cg = glowSelected.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.DOFade(1f, 0.2f).SetUpdate(true);
        }
    }
}
