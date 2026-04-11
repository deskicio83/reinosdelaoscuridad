using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;

namespace ReinoOscuridad.UI.HUD
{
    /// Componente reutilizable para mostrar una moneda en el HUD.
    /// Cada pill muestra valor actual (y opcionalmente máximo) y un botón
    /// que lleva a la ShopScene al pulsarlo.
    public class CurrencyPill : MonoBehaviour
    {
        [SerializeField] private TMP_Text    _valueText;
        [SerializeField] private Button      _plusButton;
        [SerializeField] private CurrencyType _currencyType;

        private void Start()
        {
            if (_plusButton != null)
                _plusButton.onClick.AddListener(OnPlusPressed);
        }

        /// Actualiza el texto de la pill.
        /// Si max > 0 muestra "current/max"; si no, solo "current".
        public void UpdateValue(int current, int max = -1)
        {
            if (_valueText == null) return;
            _valueText.text = max > 0 ? $"{current}/{max}" : current.ToString();
        }

        private void OnPlusPressed()
        {
            // Todos los tipos llevan a la tienda por ahora.
            _ = UIManager.Instance?.NavigateTo("ShopScene");
        }
    }
}
