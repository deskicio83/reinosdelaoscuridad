// ===============================================================
// MasteryNodePopupUI.cs
// ---------------------------------------------------------------
// Qué es y para qué sirve:
// Controla el popup de detalle de un nodo de maestría: título,
// descripción, requisitos, coste, iconos de piezas y los botones
// de Comprar / Volver. Gestiona la interacción (raycasts),
// el anclaje/tamaño relativo al viewport y el vaciado/repintado.
//
// Funciones / responsabilidades clave:
// - Inicialización de referencias UI: cache de TMP, Images, Buttons.
// - Show/Open: rellena datos del nodo y asigna callbacks.
// - Hide/Close: cierra el popup y limpia callbacks.
// - SetCost: pinta hasta 2 piezas de coste con icono y texto.
// - EnsureButtonsInteractable: activa/desactiva interactables y raycasts.
// - EnsureInViewport/CenterPopup: adapta el tamaño al viewport.
// - Clear: limpia textos, oculta piezas y suelta sprites/callbacks.
// ===============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class MasteryNodePopupUI : MonoBehaviour
{
    [Header("Refs (asigna desde el prefab)")]
    [SerializeField] private RectTransform panel;      // Tu “Root” (se auto-detecta si lo dejas vacío)
    [SerializeField] private TMP_Text txtTitle;
    [SerializeField] private TMP_Text txtDesc;
    [SerializeField] private TMP_Text txtReqs;

    [Header("Coste por piezas")]
    [SerializeField] private Image imgPieza1;
    [SerializeField] private TMP_Text txtPieza1;
    [SerializeField] private Image imgPieza2;
    [SerializeField] private TMP_Text txtPieza2;

    [SerializeField] private Button btnBuy;
    [SerializeField] private Button btnClose;

    private Action _onBuy;

    // ===== utilidades internas =====
    private RectTransform PanelRT
    {
        get
        {
            if (panel) return panel;

            // 1) RectTransform en este GO
            var self = GetComponent<RectTransform>();
            if (self) return panel = self;

            // 2) Hijo llamado “Root”
            var root = transform.Find("Root") as RectTransform;
            if (root) return panel = root;

            // 3) Primer RectTransform hijo encontrado
            var all = GetComponentsInChildren<RectTransform>(true);
            foreach (var rt in all) { if (rt != transform) { panel = rt; break; } }
            return panel;
        }
    }

    public RectTransform Root => PanelRT;

    private void Awake()
    {
        if (btnClose) btnClose.onClick.AddListener(Hide);
        if (btnBuy) btnBuy.onClick.AddListener(OnBuy);

        EnsureCanvasAndRaycaster();
        EnsureRaycastSetup();

        var rt = Root;
        if (!rt)
        {
            Debug.LogError("[MasteryPopup] Este prefab necesita un RectTransform visible (por ej. hijo 'Root').");
            return;
        }

        rt.gameObject.SetActive(false);
        Debug.Log("[MasteryPopup] Awake OK. Raycasts configurados.");
    }

    /// <summary>
    /// Asegura Canvas + GraphicRaycaster propios y orden alto.
    /// </summary>
    private void EnsureCanvasAndRaycaster()
    {
        var cvs = gameObject.GetComponent<Canvas>();
        if (!cvs) cvs = gameObject.AddComponent<Canvas>();
        cvs.overrideSorting = true;     // permitirá ordenarlo por encima
        cvs.sortingOrder = 61000;    // mayor que el de PopupLayer

        if (!gameObject.TryGetComponent<GraphicRaycaster>(out _))
            gameObject.AddComponent<GraphicRaycaster>();

        var cg = gameObject.GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }



    /// <summary>Solo los gráficos de los botones pueden capturar raycast.</summary>
    private void EnsureRaycastSetup()
    {
        var rt = Root; if (!rt) return;

        var graphics = rt.GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics)
        {
            bool isButtonGraphic =
                (btnBuy && g.transform.IsChildOf(btnBuy.transform)) ||
                (btnClose && g.transform.IsChildOf(btnClose.transform));

            g.raycastTarget = isButtonGraphic;
        }

        if (btnBuy && btnBuy.targetGraphic) btnBuy.targetGraphic.raycastTarget = true;
        if (btnClose && btnClose.targetGraphic) btnClose.targetGraphic.raycastTarget = true;

        Debug.Log($"[MasteryPopup] EnsureRaycastSetup: Graphics={graphics.Length} (solo botones con raycast).");
    }

    /// <summary>Hace que el panel ocupe el ancho del contenedor y una fracción de su alto.</summary>
    public void FitToContainer(RectTransform container, float heightFraction = 0.5f, float margin = 16f)
    {
        var rt = Root; if (!rt || !container) return;

        rt.SetParent(container, worldPositionStays: false);
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        Canvas.ForceUpdateCanvases();

        float w = Mathf.Max(0f, container.rect.width - margin * 2f);
        float h = Mathf.Max(0f, container.rect.height * heightFraction - margin * 2f);

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
    }

    // ===== interfaz pública =====
    public void Show(string title, string desc, string reqs, string _ignoredCost, bool canBuy, Action onBuy)
    {
        var rt = Root; if (!rt) return;

        if (txtTitle) txtTitle.text = title ?? "";
        if (txtDesc) txtDesc.text = desc ?? "";
        if (txtReqs) txtReqs.text = reqs ?? "";

        _onBuy = onBuy;

        EnsureCanvasAndRaycaster();
        EnsureRaycastSetup();

        if (btnBuy) btnBuy.interactable = canBuy;
        if (btnClose) btnClose.interactable = true;

        rt.gameObject.SetActive(true);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        Debug.Log($"[MasteryPopup] Show -> canBuy={canBuy}");
    }

    /// <summary>
    /// Rellena las piezas (máx. 2). No reordena ni toca anchors/pivots: respeta el prefab.
    /// Addressables esperados:
    ///   Assets/Addressables/Art/Common/piezaBasico.png
    ///   Assets/Addressables/Art/Common/piezaAvanzado.png
    ///   Assets/Addressables/Art/Common/piezaDivino.png
    /// </summary>
    public void SetCostParts(int basico, int avanzado, int divino)
    {
        // limpiar
        SetPieceOff(imgPieza1, txtPieza1);
        SetPieceOff(imgPieza2, txtPieza2);

        // construir hasta 2 entradas
        var items = new System.Collections.Generic.List<(int qty, string key)>();
        if (basico > 0) items.Add((basico, "piezaBasico"));
        if (avanzado > 0) items.Add((avanzado, "piezaAvanzado"));
        if (divino > 0) items.Add((divino, "piezaDivino"));

        if (items.Count >= 1) SetPieceOn(imgPieza1, txtPieza1, items[0].key, items[0].qty);
        if (items.Count >= 2) SetPieceOn(imgPieza2, txtPieza2, items[1].key, items[1].qty);
    }

    // Mantiene el icono dentro de su propio rect como un cuadrado máximo
    private void MakeSquareInPlace(Image img)
    {
        if (!img) return;
        var rt = img.rectTransform;

        // usar exclusivamente el tamaño del slot del prefab
        var r = rt.rect;
        float side = Mathf.Min(Mathf.Abs(r.width), Mathf.Abs(r.height));
        if (side <= 0f) side = 64f; // fallback mínimo

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, side);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, side);

        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        img.raycastTarget = false; // el icono no debe comer clicks
    }

    private void SetPieceOff(Image img, TMP_Text txt)
    {
        if (img)
        {
            img.sprite = null;
            img.enabled = false;
        }
        if (txt) txt.text = string.Empty;
    }

    private void SetPieceOn(Image img, TMP_Text txt, string iconKey, int qty)
    {
        if (txt) txt.text = $"{qty} Piezas";
        if (!img) return;

        LoadCommonIcon(iconKey, sp =>
        {
            img.sprite = sp;
            img.enabled = sp != null;
            if (sp != null) MakeSquareInPlace(img);
        });
    }

    // Carga exacta desde Addressables/Common
    private void LoadCommonIcon(string key, Action<Sprite> cb)
    {
        var h = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Sprite>(
            $"Assets/Addressables/Art/Common/{key}.png"
        );
        h.Completed += op =>
        {
            var ok = op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded;
            cb?.Invoke(ok ? op.Result : null);
        };
    }

    public void Hide()
    {
        var rt = Root; if (!rt) return;

        var cg = GetComponent<CanvasGroup>();
        if (cg) { cg.interactable = false; cg.blocksRaycasts = false; }

        rt.gameObject.SetActive(false);
        _onBuy = null;

        Debug.Log("[MasteryPopup] Hide()");
    }

    private void OnBuy()
    {
        Debug.Log("[MasteryPopup] BtnBuy CLICK");
        var cb = _onBuy;
        Hide();
        cb?.Invoke();
    }
    public void ShowCustom(string title, string body, string okText, string cancelText, System.Action onOk, System.Action onCancel = null)
    {
        var rt = Root; if (!rt) return;

        // Título y cuerpo
        if (txtTitle) txtTitle.text = title ?? string.Empty;
        if (txtDesc) txtDesc.text = body ?? string.Empty;

        // No mostramos requisitos ni costes en el popup de reset
        if (txtReqs) txtReqs.gameObject.SetActive(false);
        SetPieceOff(imgPieza1, txtPieza1);
        SetPieceOff(imgPieza2, txtPieza2);

        // Botón OK
        if (btnBuy)
        {
            btnBuy.gameObject.SetActive(true);
            var t = btnBuy.GetComponentInChildren<TMP_Text>(true);
            if (t) t.text = string.IsNullOrEmpty(okText) ? "OK" : okText;
            btnBuy.onClick.RemoveAllListeners();
            btnBuy.onClick.AddListener(() => { onOk?.Invoke(); Hide(); });
        }

        // Botón Cancelar
        if (btnClose)
        {
            bool showCancel = !string.IsNullOrEmpty(cancelText);
            btnClose.gameObject.SetActive(showCancel);
            var t = btnClose.GetComponentInChildren<TMP_Text>(true);
            if (t) t.text = showCancel ? cancelText : string.Empty;
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(() => { onCancel?.Invoke(); Hide(); });
        }

        EnsureCanvasAndRaycaster();
        EnsureRaycastSetup();

        // Al frente y visible
        transform.SetAsLastSibling();
        rt.gameObject.SetActive(true);
    }



    //public void Hide() => gameObject.SetActive(false);

}
