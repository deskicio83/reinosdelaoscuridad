using UnityEngine;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;
using ReinoOscuridad.Systems;

namespace ReinoOscuridad.UI.HUD
{
    /// MonoBehaviour principal del HUD global.
    /// Se suscribe a EventBus.OnCurrencyChanged y delega la actualización
    /// visual a las tres CurrencyPill hijas.
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private CurrencyPill _energyPill;
        [SerializeField] private CurrencyPill _goldPill;
        [SerializeField] private CurrencyPill _caosiferaPill;

        // ── Ciclo de vida ──────────────────────────────────────────────────────

        private void OnEnable()
        {
            EventBus.OnCurrencyChanged += HandleCurrencyChanged;
        }

        private void OnDisable()
        {
            EventBus.OnCurrencyChanged -= HandleCurrencyChanged;
        }

        // ── API pública ────────────────────────────────────────────────────────

        /// Muestra u oculta el HUD completo.
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        /// Refresca las tres pills con los valores actuales de EconomySystem.
        /// Llamar al navegar a una Scene nueva para asegurar valores frescos.
        public void RefreshAll()
        {
            var eco = EconomySystem.Instance;
            if (eco == null) return;

            _energyPill?.UpdateValue(eco.CurrentEnergy, eco.MaxEnergy);
            _goldPill?.UpdateValue(eco.CurrentGold);
            _caosiferaPill?.UpdateValue(eco.CurrentCaosifera);
        }

        // ── Handlers privados ──────────────────────────────────────────────────

        private void HandleCurrencyChanged(CurrencyChangedData data)
        {
            var eco = EconomySystem.Instance;
            int maxEnergy = eco != null ? eco.MaxEnergy : -1;

            switch (data.currency)
            {
                case "energia":
                    _energyPill?.UpdateValue(data.newAmount, maxEnergy);
                    break;
                case "oroNegro":
                    _goldPill?.UpdateValue(data.newAmount);
                    break;
                case "caosifera":
                    _caosiferaPill?.UpdateValue(data.newAmount);
                    break;
            }
        }
    }
}
