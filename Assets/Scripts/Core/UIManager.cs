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

        private const int MAX_HISTORY  = 5;
        private const int MAX_OVERLAYS = 2;

        // ── Configuración de fade ─────────────────────────────────────────────

        [SerializeField] private float _fadeDuration = 0.3f;
        public float FadeDuration
        {
            get => _fadeDuration;
            set => _fadeDuration = Mathf.Max(0.05f, value);
        }

        // ── Estado de navegación ──────────────────────────────────────────────

        public string CurrentScene    { get; private set; }
        public bool   IsTransitioning => _isTransitioning;

        private readonly List<string> _sceneHistory = new List<string>();
        private bool _isTransitioning;

        // ── Stack de overlays ─────────────────────────────────────────────────

        private readonly List<GameObject> _activeOverlays = new List<GameObject>();
        private const int INPUT_BLOCKER_SORT_ORDER = 49;

        // ── Fade canvas ───────────────────────────────────────────────────────

        private Canvas       _fadeCanvas;
        private Image        _fadeImage;
        private CanvasGroup  _fadeCanvasGroup;
        private bool         _fadeCanvasReady;

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

        /// Carga la Scene por nombre con transición:
        /// FadeOut → overlays silenciosos → LoadSceneAsync → FadeIn
        public async Task NavigateTo(string sceneName)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning($"[UIManager] NavigateTo({sceneName}) ignorado — transición en curso.");
                return;
            }

            _isTransitioning = true;

            if (!string.IsNullOrEmpty(CurrentScene))
            {
                _sceneHistory.Add(CurrentScene);
                if (_sceneHistory.Count > MAX_HISTORY)
                    _sceneHistory.RemoveAt(0);
            }

            // PASO 1: Fade OUT — pantalla → negro completo
            await FadeOut();

            // PASO 2: Cerrar overlays sin animación (ya en negro)
            CloseAllOverlaysSilent();

            // PASO 3: Cargar Scene en async, sin activar todavía
            var op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
                await Task.Yield();

            // PASO 4: Activar Scene (aún en negro — el jugador no ve nada)
            op.allowSceneActivation = true;

            // Esperar 2 frames para que Awake/Start de la nueva Scene se ejecuten
            await Task.Yield();
            await Task.Yield();

            CurrentScene = sceneName;

            // PASO 5: Fade IN — negro → pantalla
            await FadeIn();

            _isTransitioning = false;
            Debug.Log($"[UIManager] Navegado a: {sceneName}");
        }

        /// Vuelve a la Scene anterior del historial con el mismo orden de fundido.
        public async void NavigateBack()
        {
            if (_sceneHistory.Count == 0)
            {
                Debug.Log("[UIManager] NavigateBack — sin historial.");
                return;
            }

            string previous = _sceneHistory[_sceneHistory.Count - 1];
            _sceneHistory.RemoveAt(_sceneHistory.Count - 1);

            if (_isTransitioning)
            {
                Debug.LogWarning("[UIManager] NavigateBack ignorado — transición en curso.");
                return;
            }

            _isTransitioning = true;

            // PASO 1: Fade OUT
            await FadeOut();

            // PASO 2: Overlays en silencio
            CloseAllOverlaysSilent();

            // PASO 3: Cargar Scene anterior
            var op = SceneManager.LoadSceneAsync(previous);
            op.allowSceneActivation = false;

            while (op.progress < 0.9f)
                await Task.Yield();

            // PASO 4: Activar Scene
            op.allowSceneActivation = true;

            await Task.Yield();
            await Task.Yield();

            CurrentScene = previous;

            // PASO 5: Fade IN
            await FadeIn();

            _isTransitioning = false;
            Debug.Log($"[UIManager] Vuelto a: {previous}");
        }

        // ── OVERLAYS ──────────────────────────────────────────────────────────

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

            if (InputBlocker.Instance != null)
                InputBlocker.Instance.Show(INPUT_BLOCKER_SORT_ORDER);

            Debug.Log($"[UIManager] Overlay abierto: {overlayPrefab.name} ({_activeOverlays.Count} activos)");
            return instance;
        }

        public void HideOverlay(GameObject overlayInstance)
        {
            if (overlayInstance == null) return;

            _activeOverlays.Remove(overlayInstance);
            Destroy(overlayInstance);

            if (_activeOverlays.Count == 0 && InputBlocker.Instance != null)
                InputBlocker.Instance.Hide();

            Debug.Log($"[UIManager] Overlay cerrado ({_activeOverlays.Count} restantes)");
        }

        /// Destruye todos los overlays sin animación (para usar en negro durante transición).
        private void CloseAllOverlaysSilent()
        {
            foreach (var ov in _activeOverlays)
                if (ov != null) Destroy(ov);
            _activeOverlays.Clear();

            if (InputBlocker.Instance != null)
                InputBlocker.Instance.Hide();
        }

        public int ActiveOverlayCount => _activeOverlays.Count;

        // ── FADE ──────────────────────────────────────────────────────────────

        /// Pantalla → negro en _fadeDuration segundos.
        private async Task FadeOut()
        {
            if (!_fadeCanvasReady) return;

            _fadeCanvas.gameObject.SetActive(true);
            _fadeCanvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / _fadeDuration);
                await Task.Yield();
            }
            _fadeCanvasGroup.alpha = 1f;
        }

        /// Negro → pantalla en _fadeDuration segundos.
        private async Task FadeIn()
        {
            if (!_fadeCanvasReady) return;

            _fadeCanvasGroup.alpha = 1f;

            float elapsed = 0f;
            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _fadeCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / _fadeDuration);
                await Task.Yield();
            }
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvas.gameObject.SetActive(false);
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

            // CanvasGroup controla el alpha global — más limpio que Image.color.a
            _fadeCanvasGroup               = go.AddComponent<CanvasGroup>();
            _fadeCanvasGroup.alpha         = 0f;
            _fadeCanvasGroup.blocksRaycasts = true;
            _fadeCanvasGroup.interactable  = false;

            // Image negro sólido; el alpha lo gestiona CanvasGroup
            _fadeImage               = go.AddComponent<Image>();
            _fadeImage.color         = new Color(0f, 0f, 0f, 1f);
            _fadeImage.raycastTarget = true;

            var rt       = _fadeImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            go.SetActive(false);
            _fadeCanvasReady = true;
            Debug.Log("[UIManager] FadeCanvas creado con CanvasGroup (sortingOrder=999, _fadeDuration=" + _fadeDuration + "s).");
        }
    }
}
