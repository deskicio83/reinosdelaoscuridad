/*
============================================================
HeroCardUI.cs — Carta de héroe en el grid (retrato, nivel, elemento, estrellas)
------------------------------------------------------------
PROPÓSITO
- Render de datos básicos de un héroe y feedback visual de selección.

USO
- Instanciado por HeroGridController.PopulateGrid(). El botón llama a callback externo.

REFERENCIAS (INSPECTOR)
- Button button, Image portraitImage, TMP_Text levelText, Transform starsPanel,
  Image elementIcon, GameObject selectionOverlay, prefabs de estrella normal/awaken.

MÉTODOS
- Setup(HeroProgress heroData, Action onClick):
  asigna datos, carga retrato Addressable (awaken/normal), icono de elemento,
  renderiza estrellas (desde progress.stars) y conecta onClick (con animación DOTween opcional).
- SetSelected(bool selected):
  overlay con CanvasGroup + fade, y scale pop del card.
- LoadPortrait(): carga Sprite por Addressables según awaken.
- UpdateProgress(HeroProgress hp): actualiza modelo y re-render de estrellas y retrato.
- ForceRefreshVisual(): fuerza RenderStars().
- RenderStars(): instancia estrellas del prefab correcto.
- GetElementIconKey(string element): mapea elemento → path Addressables.
- Propiedades: string HeroId {get;}, int RuntimeIndex {get;}, SetRuntimeIndex(int).
- OnDisable(): limpia tweens y oculta overlay.

NOTAS
- DOTween requerido; selectionOverlay debe estar asignado en el prefab.
============================================================
*/


using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using DG.Tweening; // DOTween

public class HeroCardUI : MonoBehaviour
{
    [Header("Referencias de UI")]
    public Button button;
    public Image portraitImage;
    public TMP_Text levelText;
    public Transform starsPanel;
    public Image elementIcon;

    [Header("Estrellas (arrastrar prefabs)")]
    [SerializeField] private GameObject starPrefabNormal;  // ⭐ amarilla (NO addressables, referencia directa)
    [SerializeField] private GameObject starPrefabAwaken;  // ⭐ morada   (NO addressables, referencia directa)

    [Header("Animación Click")]
    public bool animateClick = true;
    [Range(0.8f, 1f)] public float clickScale = 0.94f;
    public float clickDuration = 0.1f;
    [Header("Selección")]
    [SerializeField] private Image selectionOverlay;                 // arrástralo en el inspector

    private HeroProgress heroProgress;

    // Acceso de solo lectura para localizar la card por héroe
    public string HeroId => heroProgress != null ? heroProgress.heroId : null;
    // --- Selección por instancia (NO por heroId) ---
    private int _runtimeIndex = -1;
    public int RuntimeIndex => _runtimeIndex;
    public void SetRuntimeIndex(int idx) => _runtimeIndex = idx;

    public void Setup(HeroProgress heroData, Action onClick)
    {
        heroProgress = heroData;
        var catalogEntry = HeroCatalogManager.Instance?.GetHeroById(heroData.heroId);
        SetSelected(false);
        // Nivel
        if (levelText != null)
            levelText.text = $"{heroData.level}";

        // Retrato (normal o awaken según estado del héroe)
        LoadPortrait();

        // Icono elemento
        if (elementIcon != null && catalogEntry != null)
        {
            var elementKey = GetElementIconKey(catalogEntry.element);
            if (!string.IsNullOrEmpty(elementKey))
            {
                Addressables.LoadAssetAsync<Sprite>(elementKey).Completed += (op) =>
                {
                    elementIcon.sprite = (op.Status == AsyncOperationStatus.Succeeded) ? op.Result : null;
                };
            }
            else elementIcon.sprite = null;
        }

        // Estrellas (SIEMPRE desde progress.stars)
        RenderStars();

        // Click → avisa al Grid (no llamamos a SelectHero aquí)
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (!animateClick)
                {
                    onClick?.Invoke();
                    return;
                }

                var rt = transform as RectTransform;
                if (rt == null)
                {
                    onClick?.Invoke();
                    return;
                }

                button.interactable = false;
                rt.DOKill();
                Vector3 s0 = rt.localScale;
                rt.localScale = s0;
                rt.DOScale(s0 * clickScale, clickDuration).SetEase(Ease.OutQuad).OnComplete(() =>
                {
                    rt.DOScale(s0, clickDuration).SetEase(Ease.OutQuad).OnComplete(() =>
                    {
                        button.interactable = true;
                        onClick?.Invoke();
                    });
                });
            });
        }
    }
    public void SetSelected(bool selected)
    {
        // Overlay on/off con pequeño fade
        if (selectionOverlay != null)
        {
            selectionOverlay.raycastTarget = false;
            selectionOverlay.gameObject.SetActive(true);

            var cg = selectionOverlay.GetComponent<CanvasGroup>();
            if (cg == null) cg = selectionOverlay.gameObject.AddComponent<CanvasGroup>();
            cg.DOKill();
            transform.DOKill();

            if (selected)
            {
                cg.alpha = 0f;
                cg.DOFade(0.85f, 0.20f).SetEase(Ease.OutSine).SetUpdate(true);

                var rt = transform as RectTransform;
                if (rt != null)
                {
                    rt.localScale = Vector3.one * 0.98f;
                    rt.DOScale(1f, 0.18f).SetEase(Ease.OutBack).SetUpdate(true);
                }
            }
            else
            {
                cg.DOFade(0f, 0.12f).SetEase(Ease.InSine).SetUpdate(true)
                  .OnComplete(() => selectionOverlay.gameObject.SetActive(false));

                var rt = transform as RectTransform;
                if (rt != null)
                    rt.DOScale(1f, 0.12f).SetEase(Ease.OutSine).SetUpdate(true);
            }
        }
    }


    private void OnDisable()
    {
        transform.DOKill();
        transform.localScale = Vector3.one;

        if (selectionOverlay != null)
        {
            var cg = selectionOverlay.GetComponent<CanvasGroup>();
            if (cg != null) { cg.DOKill(); cg.alpha = 0f; }
            selectionOverlay.gameObject.SetActive(false);
        }
    }

    private void LoadPortrait()
    {
        if (portraitImage == null || heroProgress == null) return;

        var catalogEntry = HeroCatalogManager.Instance?.GetHeroById(heroProgress.heroId);
        if (catalogEntry == null)
        {
            portraitImage.sprite = null;
            return;
        }

        // Si está despierto intenta cargar el retrato awaken; si no hay, usa el normal.
        string key = null;
        if (heroProgress.awaken && !string.IsNullOrEmpty(catalogEntry.portraitAddressableAwaken))
            key = catalogEntry.portraitAddressableAwaken;
        else
            key = catalogEntry.portraitAddressable;

        if (!string.IsNullOrEmpty(key))
        {
            Addressables.LoadAssetAsync<Sprite>(key).Completed += op =>
            {
                portraitImage.sprite = (op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    ? op.Result
                    : null;
            };
        }
        else
        {
            portraitImage.sprite = null;
        }
    }


    // Actualiza el "modelo" y re-renderiza solo las estrellas (sin tocar retrato/elemento)
    public void UpdateProgress(HeroProgress hp)
    {
        heroProgress = hp;
        LoadPortrait();   // <-- ahora también actualiza el retrato si pasa a awaken
        RenderStars();
    }

    public void ForceRefreshVisual() => RenderStars();

    // Render definitivo de estrellas según awaken y progress.stars
    private void RenderStars()
    {
        if (starsPanel == null || heroProgress == null) return;

        // Limpia
        for (int i = starsPanel.childCount - 1; i >= 0; --i)
            Destroy(starsPanel.GetChild(i).gameObject);

        // Prefab según awaken
        var prefab = heroProgress.awaken ? starPrefabAwaken : starPrefabNormal;
        if (prefab == null)
        {
            Debug.LogWarning($"[HeroCardUI] Falta prefab de estrella ({(heroProgress.awaken ? "AWAKEN" : "NORMAL")}) en el inspector.");
            return;
        }

        // SIEMPRE desde progress.stars
        int count = Mathf.Clamp(heroProgress.stars, 0, 10);
        for (int i = 0; i < count; i++)
            Instantiate(prefab, starsPanel);
    }

    private string GetElementIconKey(string element)
    {
        if (string.IsNullOrEmpty(element)) return null;
        switch (element.ToLowerInvariant())
        {
            case "luz": return "Assets/Addressables/Art/HeroScene/Elemento/Luz.png";
            case "oscuridad": return "Assets/Addressables/Art/HeroScene/Elemento/Oscuridad.png";
            case "fuego": return "Assets/Addressables/Art/HeroScene/Elemento/Fuego.png";
            case "naturaleza": return "Assets/Addressables/Art/HeroScene/Elemento/Naturaleza.png";
            case "agua": return "Assets/Addressables/Art/HeroScene/Elemento/Agua.png";
            default: return null;
        }
    }

}
