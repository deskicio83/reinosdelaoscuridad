using UnityEngine;
using UnityEngine.UI;
using ReinoOscuridad.Data;

namespace ReinoOscuridad.UI.Combat
{
    /// Componente ATB por carta (héroe o enemigo).
    /// Gestiona el valor ATB propio, la barra visual y el highlight de carta.
    public class ATBUnit : MonoBehaviour
    {
        // ── Datos de la unidad (asignados en runtime por CombatSceneController) ─

        public bool          esJugador;
        public string        unitId;
        public int           spd;
        public bool          estaVivo = true;
        public HeroInstance  heroData;
        public EnemyInstance enemyData;

        // ── Refs de UI (cableadas desde SetupCombatScene) ─────────────────────

        [SerializeField] private Image _atbBarRelleno;
        [SerializeField] private Image _cardHighlight;

        // ── ATB value ──────────────────────────────────────────────────────────

        public float atbValue { get; private set; }

        // ── Colores ────────────────────────────────────────────────────────────

        private static readonly Color COLOR_JUGADOR    = new Color(0.486f, 0.227f, 0.929f, 0.40f); // #7C3AED a0.4
        private static readonly Color COLOR_ENEMIGO    = new Color(0.863f, 0.149f, 0.149f, 0.40f); // #DC2626 a0.4
        private static readonly Color COLOR_TARGETABLE = new Color(0.133f, 0.773f, 0.333f, 0.50f); // #22C55E a0.5

        // ── API pública ────────────────────────────────────────────────────────

        /// Avanza el valor ATB en `ganancia` unidades (clampea en 100).
        public void TickATB(float ganancia)
        {
            if (!estaVivo) return;
            atbValue = Mathf.Min(atbValue + ganancia, 100f);
            if (_atbBarRelleno != null)
                _atbBarRelleno.fillAmount = atbValue / 100f;
        }

        public bool IsReady() => atbValue >= 100f;

        public void ResetATB()
        {
            atbValue = 0f;
            if (_atbBarRelleno != null)
                _atbBarRelleno.fillAmount = 0f;
        }

        /// Activa o desactiva el highlight de "turno activo" (morado/rojo según bando).
        public void SetActive(bool active)
        {
            if (_cardHighlight == null) return;
            var col = esJugador ? COLOR_JUGADOR : COLOR_ENEMIGO;
            _cardHighlight.color = new Color(col.r, col.g, col.b, active ? col.a : 0f);
        }

        /// Activa el highlight verde de "objetivable" para selección de target.
        public void SetHighlightTargetable(bool targetable)
        {
            if (_cardHighlight == null) return;
            var col = COLOR_TARGETABLE;
            _cardHighlight.color = new Color(col.r, col.g, col.b, targetable ? col.a : 0f);
        }

        // ── Accesores para SetupCombatScene ────────────────────────────────────

        public Image AtbBarRelleno => _atbBarRelleno;
        public Image CardHighlight  => _cardHighlight;

        // Wiring desde Editor Script
        public void SetAtbBarRelleno(Image img) => _atbBarRelleno = img;
        public void SetCardHighlight(Image img)  => _cardHighlight  = img;
    }
}
