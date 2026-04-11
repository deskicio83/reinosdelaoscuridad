using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ReinoOscuridad.UI.Common;

namespace ReinoOscuridad.Core
{
    /// Singleton DontDestroyOnLoad. Gestiona navegación entre Scenes y stack de overlays.
    /// Todos los cambios de Scene deben pasar por aquí — nunca llamar SceneManager directamente.
    [DefaultExecutionOrder(-75)]
    public class UIManager : MonoBehaviour, ISystem
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        public static UIManager Instance { get; private set; }

        // ── Constantes ─────────────────────────────────────────────────────────

        private const int   MAX_HISTORY  = 5;
        private const int   MAX_OVERLAYS = 2;
        private const float FADE_SECONDS = 0.2f;

        // ── Estado de navegación ──────────────────────────────────────────────

        public string CurrentScene { get; private set; }

        /// Historial de Scenes: [0] = más antigua, [last] = inmediatamente anterior.
        private readonly List<string> _sceneHistory = new List<string>();
        private bool _isTransitioning;

        // ── Stack de overlays ─────────────────────────────────────────────────

        private readonly List<GameObject> _activeOverlays = new List<GameObject>();

        // Sort order por defecto del InputBlocker — justo por debajo de cualquier overlay tipico
        private const int INPUT_BLOCKER_SORT_ORDER = 49;

        // ── Fade canvas ───────────────────────────────────────────────────────

        private Canvas _fadeCanvas;
        private Image  _fadeImage;
        private bool   _fadeCanvasReady;

        // ── Ciclo de vida Unity ────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameManager.Instance.RegisterSystem(this);
        }

        private void Update()
        {
            // Back físico de Android: cerrar overlay superior o navegar atrás
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_activeOverlays.Count > 0)
                    HideOverlay(_activeOverlays[_activeOverlays.Count - 1]);
                else
                    NavigateBack();
            }
        }

        // ── ISystem ───────────────────────────────────────────────────────────

        public void Initialize()
        {
            CurrentScene = SceneManager.GetActiveScene().name;
            CreateFadeCanvas();
            Debug.Log($"[UIManager] Inicializado — Scene actual: {CurrentScene}");
        }

        public void OnSessionStart() { }
        public void OnSessionEnd()   { }

        // ── NAVEGACIÓN ────────────────────────────────────────────────────────

        /// Carga la Scene por nombre con transición fade (200ms entrada + 200ms salida).
        public async Task NavigateTo(string sceneName)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning($"[UIManager] NavigateTo({sceneName}) ignorado — transición en curso.");
                return;
            }

            _isTransitioning = true;

            // Guardar Scene actual en historial
            if (!string.IsNullOrEmpty(CurrentScene))
            {
                _sceneHistory.Add(CurrentScene);
                if (_sceneHistory.Count > MAX_HISTORY)
                    _sceneHistory.RemoveAt(0);
            }

            await FadeAsync(0f, 1f); // FadeIn: transparente → negro
            SceneManager.LoadScene(sceneName);
            CurrentScene = sceneName;
            await FadeAsync(1f, 0f); // FadeOut: negro → transparente

            _isTransitioning = false;
            Debug.Log($"[UIManager] Navegado a: {sceneName}");
        }

        /// Vuelve a la Scene anterior del historial (si existe).
        public async void NavigateBack()
        {
            if (_sceneHistory.Count == 0)
            {
                Debug.Log("[UIManager] NavigateBack — sin historial.");
                return;
            }

            string previous = _sceneHistory[_sceneHistory.Count - 1];
            _sceneHistory.RemoveAt(_sceneHistory.Count - 1);

            _isTransitioning = true;

            await FadeAsync(0f, 1f);
            SceneManager.LoadScene(previous);
            CurrentScene = previous;
            await FadeAsync(1f, 0f);

            _isTransitioning = false;
            Debug.Log($"[UIManager] Vuelto a: {previous}");
        }

        // ── OVERLAYS ──────────────────────────────────────────────────────────

        /// Instancia un overlay sobre la Scene activa.
        /// Si ya hay 2 overlays activos, cierra el más antiguo automáticamente.
        public GameObject ShowOverlay(GameObject overlayPrefab)
        {
            if (overlayPrefab == null)
            {
                Debug.LogWarning("[UIManager] ShowOverlay: prefab es null.");
                return null;
            }

            if (_activeOverlays.Count >= MAX_OVERLAYS)
            {
                Debug.Log("[UIManager] Máximo de overlays alcanzado — cerrando el más antiguo.");
                HideOverlay(_activeOverlays[0]);
            }

            var instance = Instantiate(overlayPrefab);
            _activeOverlays.Add(instance);

            // Mostrar InputBlocker por debajo del overlay
            if (InputBlocker.Instance != null)
                InputBlocker.Instance.Show(INPUT_BLOCKER_SORT_ORDER);

            Debug.Log($"[UIManager] Overlay abierto: {overlayPrefab.name} ({_activeOverlays.Count} activos)");
            return instance;
        }

        /// Destruye la instancia de overlay y la elimina del stack.
        public void HideOverlay(GameObject overlayInstance)
        {
            if (overlayInstance == null) return;

            _activeOverlays.Remove(overlayInstance);
            Destroy(overlayInstance);

            // Ocultar InputBlocker cuando no quedan overlays
            if (_activeOverlays.Count == 0 && InputBlocker.Instance != null)
                InputBlocker.Instance.Hide();

            Debug.Log($"[UIManager] Overlay cerrado ({_activeOverlays.Count} restantes)");
        }

        /// Número de overlays activos en este momento.
        public int ActiveOverlayCount => _activeOverlays.Count;

        // ── FADE ──────────────────────────────────────────────────────────────

        private async Task FadeAsync(float from, float to)
        {
            if (!_fadeCanvasReady) return;

            float elapsed = 0f;
            SetFadeAlpha(from);
            _fadeCanvas.gameObject.SetActive(true);

            while (elapsed < FADE_SECONDS)
            {
                elapsed += Time.deltaTime;
                SetFadeAlpha(Mathf.Lerp(from, to, elapsed / FADE_SECONDS));
                await Task.Yield();
            }

            SetFadeAlpha(to);

            if (Mathf.Approximately(to, 0f))
                _fadeCanvas.gameObject.SetActive(false);
        }

        private void SetFadeAlpha(float alpha)
        {
            if (_fadeImage != null)
                _fadeImage.color = new Color(0f, 0f, 0f, alpha);
        }

        // ── Creación del canvas de fade ───────────────────────────────────────

        private void CreateFadeCanvas()
        {
            var go = new GameObject("UIManager_FadeCanvas");
            DontDestroyOnLoad(go);

            _fadeCanvas = go.AddComponent<Canvas>();
            _fadeCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _fadeCanvas.sortingOrder = 999;

            go.AddComponent<CanvasScaler>();

            _fadeImage               = go.AddComponent<Image>();
            _fadeImage.color         = new Color(0f, 0f, 0f, 0f);
            _fadeImage.raycastTarget = true; // bloquea input durante la transición

            var rt        = _fadeImage.rectTransform;
            rt.anchorMin  = Vector2.zero;
            rt.anchorMax  = Vector2.one;
            rt.offsetMin  = Vector2.zero;
            rt.offsetMax  = Vector2.zero;

            go.SetActive(false);
            _fadeCanvasReady = true;
            Debug.Log("[UIManager] FadeCanvas creado (sortingOrder=999).");
        }
    }
}
