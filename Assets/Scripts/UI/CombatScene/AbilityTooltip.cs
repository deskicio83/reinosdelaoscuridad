using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ReinoOscuridad.UI.Combat
{
    /// Tooltip informativo de habilidad que aparece en long-press.
    /// Singleton por Scene — se instancia en CombatScene via SetupCombatScene.
    /// Se muestra con Show() en PointerDown y oculta con Hide() en PointerUp.
    public class AbilityTooltip : MonoBehaviour
    {
        public static AbilityTooltip Instance { get; private set; }

        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text      _txtNombre;
        [SerializeField] private TMP_Text      _txtDescripcion;
        [SerializeField] private TMP_Text      _txtCooldown;

        private Canvas        _canvas;
        private RectTransform _canvasRT;

        private void Awake()
        {
            Instance = this;
            if (_panel != null) _panel.gameObject.SetActive(false);
        }

        private void Start()
        {
            _canvas   = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();
            _canvasRT = _canvas?.GetComponent<RectTransform>();
        }

        // ── API pública ─────────────────────────────────────────────────────────

        /// Muestra el tooltip encima de la posición en pantalla indicada.
        public void Show(Vector2 screenPos, string nombre, string descripcion, int cooldown)
        {
            if (_panel == null) return;

            // Rellenar textos
            if (_txtNombre      != null) _txtNombre.text      = nombre;
            if (_txtDescripcion != null) _txtDescripcion.text = descripcion;
            if (_txtCooldown    != null)
                _txtCooldown.text = cooldown > 0 ? $"Cooldown: {cooldown} turnos" : "Sin cooldown";

            // Posicionar en canvas local
            if (_canvasRT != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRT, screenPos, _canvas.worldCamera, out var localPos);
                // Desplazar hacia la derecha y arriba del icono pulsado
                _panel.anchoredPosition = localPos + new Vector2(80f, 20f);
            }

            _panel.gameObject.SetActive(true);
        }

        /// Oculta el tooltip.
        public void Hide()
        {
            if (_panel != null) _panel.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
