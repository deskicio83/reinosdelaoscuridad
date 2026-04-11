using UnityEngine;
using UnityEngine.UI;

namespace ReinoOscuridad.UI.Common
{
    /// Panel transparente bloqueante de input.
    /// Se coloca entre la Scene y cualquier overlay/popup para evitar que
    /// los clicks atraviesen al contenido de debajo mientras el popup esta abierto.
    ///
    /// Singleton ligero por Scene (no DontDestroyOnLoad).
    /// UIManager lo activa/desactiva automaticamente al abrir y cerrar overlays.
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(Image))]
    public class InputBlocker : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        public static InputBlocker Instance { get; private set; }

        // ── Referencias internas ───────────────────────────────────────────────

        private Canvas _canvas;
        private Image  _image;

        // ── Ciclo de vida ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _canvas = GetComponent<Canvas>();
            _image  = GetComponent<Image>();

            // Asegurar configuracion correcta en runtime
            _image.color         = Color.clear;
            _image.raycastTarget = true;

            var rt       = _image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── API publica ────────────────────────────────────────────────────────

        /// Activa el bloqueador con el sortingOrder indicado.
        /// Usar siempre un valor menor que el sortingOrder del overlay que lo invoca.
        public void Show(int sortOrder)
        {
            _canvas.sortingOrder = sortOrder;
            gameObject.SetActive(true);
        }

        /// Desactiva el bloqueador.
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
