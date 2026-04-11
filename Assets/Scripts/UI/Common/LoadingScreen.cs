using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ReinoOscuridad.UI.Common
{
    /// Pantalla de carga reutilizable. DontDestroyOnLoad. Sort Order 998.
    /// Uso:
    ///   LoadingScreen.Instance.Show("cargando heroes");
    ///   LoadingScreen.Instance.SetProgress(0.5f);
    ///   LoadingScreen.Instance.SetProgress(1f, 0.3f);
    ///   LoadingScreen.Instance.Hide();
    public class LoadingScreen : MonoBehaviour
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        public static LoadingScreen Instance { get; private set; }

        // ── Referencias visuales ───────────────────────────────────────────────

        [Header("Visual")]
        [SerializeField] private Image       _imagenCarrusel;
        [SerializeField] private RectTransform _barraRelleno;
        [SerializeField] private TMP_Text    _textoFrase;

        [Header("Carrusel (opcional — asignar sprites via Addressables en el futuro)")]
        [SerializeField] private Sprite[]    _spritesCarrusel;

        // ── Frases de carga ────────────────────────────────────────────────────

        private readonly string[] _frases =
        {
            "Echando troncos al fuego eterno...",
            "Afilando las puntas de lanza...",
            "Despertando a los esbirros dormidos...",
            "Contando las almas corrompidas...",
            "Calibrando el nivel de maldad...",
            "Preparando las mazmorras...",
            "Invocando entidades menores...",
            "Revisando el grimorio de hechizos..."
        };

        // ── Estado interno ─────────────────────────────────────────────────────

        private int   _lastSpriteIndex  = -1;
        private Coroutine _progressCoroutine;

        // ── Ciclo de vida ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── API publica ────────────────────────────────────────────────────────

        /// Activa la pantalla de carga.
        /// Elige frase aleatoria, reinicia barra a 0 y muestra sprite aleatorio.
        public void Show(string contexto = "")
        {
            gameObject.SetActive(true);

            // Frase aleatoria
            if (_textoFrase != null && _frases.Length > 0)
                _textoFrase.text = _frases[Random.Range(0, _frases.Length)];

            // Barra reiniciada
            SetProgressImmediate(0f);

            // Carrusel
            MostrarSpriteAleatorio();

            if (!string.IsNullOrEmpty(contexto))
                Debug.Log($"[LoadingScreen] Show — contexto: {contexto}");
        }

        /// Desactiva la pantalla con fade out de 200ms.
        public void Hide()
        {
            StartCoroutine(HideFadeCoroutine());
        }

        /// Actualiza la barra de progreso instantaneamente (value 0–1).
        public void SetProgress(float value)
        {
            if (_progressCoroutine != null)
            {
                StopCoroutine(_progressCoroutine);
                _progressCoroutine = null;
            }
            SetProgressImmediate(Mathf.Clamp01(value));
        }

        /// Anima suavemente la barra hasta value en duration segundos.
        public void SetProgress(float value, float duration)
        {
            if (_progressCoroutine != null)
                StopCoroutine(_progressCoroutine);
            _progressCoroutine = StartCoroutine(AnimateProgressCoroutine(value, duration));
        }

        // ── Internos ───────────────────────────────────────────────────────────

        private void SetProgressImmediate(float value)
        {
            if (_barraRelleno == null) return;
            var anchor      = _barraRelleno.anchorMax;
            anchor.x        = Mathf.Clamp01(value);
            _barraRelleno.anchorMax = anchor;
        }

        private IEnumerator AnimateProgressCoroutine(float target, float duration)
        {
            float start   = _barraRelleno != null ? _barraRelleno.anchorMax.x : 0f;
            float elapsed = 0f;
            target        = Mathf.Clamp01(target);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetProgressImmediate(Mathf.Lerp(start, target, elapsed / duration));
                yield return null;
            }

            SetProgressImmediate(target);
            _progressCoroutine = null;
        }

        private IEnumerator HideFadeCoroutine()
        {
            // Fade out 200ms via alpha del Canvas Group (si existe) o directo
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            const float FADE = 0.2f;

            while (elapsed < FADE)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / FADE);
                yield return null;
            }

            canvasGroup.alpha = 1f; // resetear para el proximo Show
            gameObject.SetActive(false);
        }

        private void MostrarSpriteAleatorio()
        {
            if (_imagenCarrusel == null) return;

            if (_spritesCarrusel == null || _spritesCarrusel.Length == 0)
            {
                // Sin sprites → mantener placeholder color definido en el prefab
                _imagenCarrusel.sprite = null;
                return;
            }

            int idx;
            do { idx = Random.Range(0, _spritesCarrusel.Length); }
            while (_spritesCarrusel.Length > 1 && idx == _lastSpriteIndex);

            _lastSpriteIndex       = idx;
            _imagenCarrusel.sprite = _spritesCarrusel[idx];
        }
    }
}
