using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using System;
using UnityEngine.EventSystems;

public class LegionCardUI : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
{
    [Header("Referencias UI")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private GameObject lockOverlay;
    [SerializeField] private Transform starsRoot;
    [SerializeField] private GameObject starPrefab;
    [SerializeField] private Button btnSelect;
    [SerializeField] private TMP_Text txtName;

    [Header("Layout")]
    [Tooltip("Borde visible alrededor del retrato (porcentaje del ancho de la tarjeta).")]
    [SerializeField, Range(0.00f, 0.15f)] private float portraitMarginPercent = 0.05f; // 5%
    [Tooltip("Altura reservada (en px) para nombre + estrellas.")]
    [SerializeField] private float reservedBottomPx = 92f;

    [Header("Owned badge")]
    [SerializeField] private Image ownedBadge;           // icono poseÃ­do (opcional: se crea si no existe)
    [SerializeField] private Sprite ownedBadgeSprite;    // sprite opcional
    [Tooltip("TamaÃ±o del badge en % del ancho de la tarjeta.")]
    [SerializeField, Range(0.10f, 0.40f)] private float ownedBadgeSizePercent = 0.22f; // 22% del ancho
    [Tooltip("Desplazamiento del badge desde la esquina superior-izquierda (px).")]
    [SerializeField] private Vector2 ownedBadgeOffset = new Vector2(0, 0);
    public event System.Action<string> OnSelected;
    private HeroCatalogEntry _def;
    private HeroProgress _prog;
    private bool _obtained;

    private string currentHeroId;
    private System.Action onClick;
    private AspectRatioFitter _portraitAR;
    private Image _lockOverlayImg;
    private RectTransform _lockOverlayRT;
    private bool _needsSync;
    [SerializeField] private Button _button;
    private string _heroId;
    
    // Guarda el heroId y asegura que cualquier click en la tarjeta dispare la selecciÃ³n.
    private System.Action _onSelect;
    private string _heroIdCached;
    // ------------------------------------------------------------------------

    private void Awake()
    {
        if (!portraitImage) Debug.LogError("[LegionCardUI] Falta portraitImage.");
        if (!starsRoot) Debug.LogError("[LegionCardUI] Falta starsRoot.");
        if (!starPrefab) Debug.LogError("[LegionCardUI] Falta starPrefab.");
        if (!btnSelect) Debug.LogError("[LegionCardUI] Falta btnSelect.");

        // Retrato: NO dejar que el AR cambie tamaÃ±o automÃ¡ticamente
        if (portraitImage)
        {
            _portraitAR = portraitImage.GetComponent<AspectRatioFitter>();
            if (!_portraitAR) _portraitAR = portraitImage.gameObject.AddComponent<AspectRatioFitter>();
            _portraitAR.aspectMode = AspectRatioFitter.AspectMode.None; // <--- CLAVE
            portraitImage.preserveAspect = true;
            portraitImage.type = Image.Type.Simple;
            portraitImage.raycastTarget = false;
        }

        // Overlay de candado
        if (lockOverlay)
        {
            _lockOverlayImg = lockOverlay.GetComponent<Image>();
            _lockOverlayRT = lockOverlay.GetComponent<RectTransform>();
            if (_lockOverlayImg) _lockOverlayImg.raycastTarget = false;
            lockOverlay.SetActive(false);
        }

        // Badge "poseÃ­do"
        if (!ownedBadge)
        {
            var go = new GameObject("OwnedBadge", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            ownedBadge = go.GetComponent<Image>();
        }

        var brt = ownedBadge.rectTransform;
        brt.anchorMin = brt.anchorMax = new Vector2(0f, 1f); // esquina sup-izq
        brt.pivot = new Vector2(0f, 1f);
        ownedBadge.raycastTarget = false;
        ownedBadge.color = Color.white;
        if (ownedBadgeSprite) ownedBadge.sprite = ownedBadgeSprite;

        // Borde/outline sutil de la tarjeta
        var outline = GetComponent<Outline>() ?? gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 1f, 1f, 0.20f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    private void OnRectTransformDimensionsChange()
    {
        FitPortraitSquare();     // calcula el retrato con borde
        LayoutBottomBand();      // ajusta nombre y estrellas a 50/50
        UpdateOwnedBadgeSize();  // badge 60x60
        _needsSync = true;       // para sincronizar el overlay en LateUpdate
    }


    private void LateUpdate()
    {
        if (_needsSync)
        {
            SyncOverlayToPortrait();
            _needsSync = false;
        }
        // Si el overlay estÃ¡ activo, asegÃºralo (por si hay re-cÃ¡lculos de layout).
        if (lockOverlay && lockOverlay.activeSelf) SyncOverlayToPortrait();
    }

    // ------------------------------------------------------------------------

    public void Setup(HeroCatalogEntry def, HeroProgress prog, bool obtained, System.Action onClick)
    {
        _def = def;
        _prog = prog;          // <-- ahora existe
        _obtained = obtained;  // <-- ahora existe
        _onSelect = onClick;   // <-- para que OnPointerClick funcione tambiÃ©n

        // Nombre (2 lÃ­neas, centrado, sin elipsis)
        if (txtName != null)
        {
            txtName.enableWordWrapping = true;
            txtName.overflowMode = TMPro.TextOverflowModes.Overflow;
            txtName.alignment = TMPro.TextAlignmentOptions.Center;
            txtName.maxVisibleLines = 2;
            txtName.text = string.IsNullOrEmpty(def.displayName) ? def.heroId : def.displayName;
        }

        // DirecciÃ³n del FULL (awaken si lo tienes y el hÃ©roe estÃ¡ poseÃ­do)
        var addr = _obtained && !string.IsNullOrEmpty(def.fullAddressableAwaken)
            ? def.fullAddressableAwaken
            : def.fullAddressable;
        if (string.IsNullOrEmpty(addr))
        {
            Debug.LogWarning($"[LegionCard] '{def.heroId}' no tiene fullAddressable asignado.");
        }
        LoadPortrait(addr);

        // Estrellas (usa baseStars del catÃ¡logo si no quieres mezclar con progreso)
        BuildStars(Mathf.Max(1, def.baseStars));

        // Badge â€œposeÃ­doâ€
        if (ownedBadge != null)
            ownedBadge.gameObject.SetActive(_obtained);

        // Click (tarjeta y, si existe, botÃ³n dedicado)
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            if (_onSelect != null) btn.onClick.AddListener(() => _onSelect());
        }
        if (btnSelect != null)
        {
            btnSelect.onClick.RemoveAllListeners();
            if (_onSelect != null) btnSelect.onClick.AddListener(() => _onSelect());
        }

        // Layout (borde + margen 5% y badge 60x60)
        ApplyLayout();
    }
    private void ApplyLayout()
    {
        // â€œBordeâ€ = el retrato ocupa 90% del ancho/alto del card (5% de margen por lado)
        var templateRect = transform as RectTransform;
        var portraitRect = portraitImage != null ? portraitImage.rectTransform : null;
        if (templateRect != null && portraitRect != null)
        {
            float cardW = templateRect.rect.width;
            float cardH = templateRect.rect.height;
            float targetW = cardW * 0.90f;
            float targetH = cardH * 0.90f;

            portraitRect.anchorMin = new Vector2(0.5f, 1f);
            portraitRect.anchorMax = new Vector2(0.5f, 1f);
            portraitRect.pivot = new Vector2(0.5f, 1f);
            portraitRect.sizeDelta = new Vector2(targetW, targetH);
            portraitRect.anchoredPosition = new Vector2(0f, -8f); // un poco hacia abajo para dejar sitio a nombre/estrellas
        }

        // Badge â€œposeÃ­doâ€ 60x60
        if (ownedBadge != null)
        {
            var rt = ownedBadge.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(60f, 60f);
            rt.anchoredPosition = new Vector2(8f, -8f);
        }

        // Nombre (2 lÃ­neas, centrado) y estrellas centradas
        if (txtName != null)
        {
            txtName.enableWordWrapping = true;
            txtName.overflowMode = TMPro.TextOverflowModes.Overflow;
            txtName.alignment = TMPro.TextAlignmentOptions.Center;
            txtName.maxVisibleLines = 2;
        }
        if (starsRoot != null)
        {
            var layout = starsRoot.GetComponent<HorizontalLayoutGroup>();
            if (!layout) layout = starsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 2f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }
    }

    // Llamado por el Button y por el click de cualquier parte de la tarjeta
    private void InvokeSelect()
    {
        // Si bloqueas por "lock overlay" o similar, respÃ©talo aquÃ­ si quieres.
        _onSelect?.Invoke();
    }

    // IPointerClickHandler: por si el Button no recibe el evento
    public void OnPointerClick(PointerEventData eventData)
    {
        _onSelect?.Invoke();
    }

    // Borde = retrato con margen porcentual y cuadrado
    private void FitPortraitSquare()
    {
        if (!portraitImage) return;

        var cardRT = (RectTransform)transform;
        float w = Mathf.Max(1f, cardRT.rect.width);
        float h = Mathf.Max(1f, cardRT.rect.height);

        float margin = Mathf.Max(0f, portraitMarginPercent) * w;  // 5% de cada lado
        float topMargin = margin;

        // lado mÃ¡ximo respetando mÃ¡rgenes laterales y zona inferior reservada
        float maxSideByWidth = w - (margin * 2f);
        float maxSideByHeight = h - reservedBottomPx - topMargin;
        float side = Mathf.Clamp(Mathf.Min(maxSideByWidth, maxSideByHeight), 32f, 8192f);

        var rt = portraitImage.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); // centrado arriba
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -topMargin);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, side);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, side);
    }
    private void LayoutBottomBand()
    {
        var cardRT = transform as RectTransform;
        if (!cardRT) return;

        float half = Mathf.Max(0f, reservedBottomPx) * 0.5f;

        // --- Nombre (mitad superior)
        if (txtName)
        {
            var nrt = txtName.rectTransform;
            nrt.anchorMin = new Vector2(0f, 0f);
            nrt.anchorMax = new Vector2(1f, 0f);
            nrt.pivot = new Vector2(0.5f, 0f);
            nrt.sizeDelta = new Vector2(0f, half);
            nrt.anchoredPosition = new Vector2(0f, half); // colocamos la mitad superior

            // EstÃ©tica/ajustes del texto
            txtName.alignment = TextAlignmentOptions.Center;
            txtName.enableWordWrapping = true;
            txtName.maxVisibleLines = 2;
            txtName.overflowMode = TextOverflowModes.Overflow;
            txtName.enableAutoSizing = false; // mantenemos el mismo cuerpo
        }

        // --- Estrellas (mitad inferior)
        if (starsRoot)
        {
            var srt = starsRoot as RectTransform;
            if (srt)
            {
                srt.anchorMin = new Vector2(0f, 0f);
                srt.anchorMax = new Vector2(1f, 0f);
                srt.pivot = new Vector2(0.5f, 0f);
                srt.sizeDelta = new Vector2(0f, half);
                srt.anchoredPosition = new Vector2(0f, 0f); // mitad inferior
            }

            EnsureStarsLayoutGroup(); // centra automÃ¡ticamente las estrellas
        }
    }
    private void EnsureStarsLayoutGroup()
    {
        if (!starsRoot) return;

        var srt = starsRoot.GetComponent<RectTransform>();
        var hlg = starsRoot.GetComponent<HorizontalLayoutGroup>();
        if (!hlg) hlg = starsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();

        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 6f;

        // que su tamaÃ±o vertical lo determine nuestra banda inferior
        var csf = starsRoot.GetComponent<ContentSizeFitter>();
        if (!csf) csf = starsRoot.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }




    private void UpdateOwnedBadgeSize()
    {
        if (!ownedBadge) return;
        var cardRT = transform as RectTransform;
        if (!cardRT) return;

        const float SIZE = 60f; // <<< tamaÃ±o fijo solicitado

        var rt = ownedBadge.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); // esquina sup-izq
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(SIZE, SIZE);

        // pequeÃ±o padding relativo para despegar del borde/outline
        float pad = Mathf.Round(cardRT.rect.width * 0.02f);
        rt.anchoredPosition = new Vector2(pad, -pad);

        EnsureBadgeOnTop();
    }


    private void EnsureBadgeOnTop()
    {
        if (ownedBadge) ownedBadge.transform.SetAsLastSibling(); // por encima del retrato
    }

    private void SyncOverlayToPortrait()
    {
        if (_lockOverlayRT == null || portraitImage == null) return;
        var src = portraitImage.rectTransform;

        _lockOverlayRT.anchorMin = src.anchorMin;
        _lockOverlayRT.anchorMax = src.anchorMax;
        _lockOverlayRT.pivot = src.pivot;
        _lockOverlayRT.anchoredPosition = src.anchoredPosition;
        _lockOverlayRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, src.rect.width);
        _lockOverlayRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   src.rect.height);

        if (_lockOverlayImg != null)
        {
            _lockOverlayImg.color = new Color(0f, 0f, 0f, 0.55f);
            _lockOverlayImg.preserveAspect = true;
            _lockOverlayImg.raycastTarget = false;
        }
    }

    private void LoadPortrait(string addressablePath)
    {
        if (portraitImage == null) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    if (string.IsNullOrEmpty(addressablePath))
    {
        portraitImage.sprite = null;
        portraitImage.color = Color.gray;
        return;
    }
#endif

        var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(addressablePath);
        handle.Completed += op =>
        {
            if (op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                portraitImage.sprite = op.Result;
                portraitImage.SetNativeSize();
                // Tras cargar, re-aplico layout para respetar el â€œbordeâ€ visual
                ApplyLayout();
            }
            else
            {
                Debug.LogWarning($"[LegionCard] FallÃ³ carga sprite: {addressablePath}");
            }
        };
    }


    private void BuildStars(int baseStars)
    {
        if (starsRoot == null || starPrefab == null) return;

        for (int i = starsRoot.childCount - 1; i >= 0; i--)
            Destroy(starsRoot.GetChild(i).gameObject);

        for (int i = 0; i < baseStars; i++)
        {
            var star = Instantiate(starPrefab, starsRoot);
            star.gameObject.SetActive(true);
        }
    }
}
