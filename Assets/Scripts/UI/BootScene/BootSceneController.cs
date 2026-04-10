using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Firebase;
using ReinoOscuridad.Core;
using ReinoOscuridad.Firebase;
using ReinoOscuridad.Systems;

namespace ReinoOscuridad.UI.Boot
{
    /// Gestiona el flujo completo de arranque:
    /// Firebase init → Auth → Carga Firestore → Energía offline → Navegación.
    /// Debe estar en la BootScene como único controlador de flujo.
    public class BootSceneController : MonoBehaviour
    {
        // ── Referencias UI (asignar en Inspector) ─────────────────────────────

        [Header("Paneles")]
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private GameObject _loginPanel;
        [SerializeField] private GameObject _errorPanel;

        [Header("Textos")]
        [SerializeField] private TextMeshProUGUI _loadingText;
        [SerializeField] private TextMeshProUGUI _errorText;

        [Header("Botones de login")]
        [SerializeField] private Button _btnGoogle;
        [SerializeField] private Button _btnGuest;

        [Header("Botón reintentar")]
        [SerializeField] private Button _btnRetry;

        // ── Estado interno ────────────────────────────────────────────────────

        private TaskCompletionSource<LoginChoice> _loginTcs;

        private enum LoginChoice { Google, Guest }

        // ── Ciclo de vida Unity ────────────────────────────────────────────────

        private async void Start()
        {
            await RunBootSequenceAsync();
        }

        // ── Flujo principal ───────────────────────────────────────────────────

        private async Task RunBootSequenceAsync()
        {
            // ── 1. Inicialización Firebase ────────────────────────────────────
            ShowLoading("Iniciando…");

            var status = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (status != DependencyStatus.Available)
            {
                ShowError($"Firebase no disponible: {status}\nComprueba tu conexión e inténtalo de nuevo.");
                Debug.LogError($"[BootSceneController] Firebase DependencyStatus: {status}");
                return;
            }

            Debug.Log("[BootSceneController] Firebase OK.");

            // ── 2. Flujo de login ─────────────────────────────────────────────
            var auth = GameManager.Instance.GetSystem<AuthSystem>();

            if (!auth.IsLoggedIn)
            {
                bool loginOk = await RunLoginFlowAsync(auth);
                if (!loginOk) return; // error ya mostrado en RunLoginFlowAsync
            }

            // ── 3. Carga de datos desde Firestore ─────────────────────────────
            ShowLoading("Cargando datos…");

            var dss = GameManager.Instance.GetSystem<DataStorageSystem>();
            await dss.LoadPlayerDataFromFirestore(auth.CurrentUID);

            // ── 4. Energía offline ────────────────────────────────────────────
            // Re-inicializar EconomySystem con los datos recién cargados de Firestore
            // para que calcule correctamente la energía acumulada offline.
            var economy = GameManager.Instance.GetSystem<EconomySystem>();
            economy.Initialize();

            // Notificar a todos los sistemas que la sesión ha comenzado
            var pds = GameManager.Instance.GetSystem<PlayerDataSystem>();
            GameManager.Instance.NotifySessionStart(pds.GetPlayerData().lastLoginTimestamp);

            // ── 5. Navegación ─────────────────────────────────────────────────
            ShowLoading("Entrando…");

            bool tutorialDone = pds.GetPlayerData().tutorialCompleted;

            // TODO S09: reemplazar SceneManager.LoadScene por UIManager.LoadScene
            if (tutorialDone)
            {
                Debug.Log("[BootSceneController] → MainMenuScene");
                SceneManager.LoadScene("MainMenuScene");
            }
            else
            {
                Debug.Log("[BootSceneController] → TutorialScene (primera vez)");
                SceneManager.LoadScene("TutorialScene");
            }
        }

        // ── Flujo de login con reintentos ─────────────────────────────────────

        private async Task<bool> RunLoginFlowAsync(AuthSystem auth)
        {
            while (true)
            {
                ShowLoginPanel();

                // Esperar elección del jugador
                _loginTcs = new TaskCompletionSource<LoginChoice>();
                LoginChoice choice = await _loginTcs.Task;

                ShowLoading("Iniciando sesión…");

                try
                {
                    switch (choice)
                    {
                        case LoginChoice.Google:
                            // TODO: obtener idToken desde Google Sign-In SDK
                            throw new NotImplementedException("Google Sign-In SDK pendiente de integrar.");
                        case LoginChoice.Guest:
                            await auth.LoginAsGuest();
                            break;
                    }
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[BootSceneController] Login falló: {e.Message}");
                    bool retry = await ShowRetryPromptAsync($"Error al iniciar sesión:\n{e.Message}");
                    if (!retry) return false;
                    // Si retry == true, el bucle vuelve a mostrar los botones
                }
            }
        }

        // ── Callbacks de botones ──────────────────────────────────────────────

        /// Llamar desde el Inspector del botón Google (OnClick).
        public void OnLoginGooglePressed()
        {
            _loginTcs?.TrySetResult(LoginChoice.Google);
        }

        /// Llamar desde el Inspector del botón Invitado (OnClick).
        public void OnLoginGuestPressed()
        {
            _loginTcs?.TrySetResult(LoginChoice.Guest);
        }

        /// Llamar desde el Inspector del botón Reintentar (OnClick).
        public void OnRetryPressed()
        {
            _retryTcs?.TrySetResult(true);
        }

        // ── Gestión de UI ─────────────────────────────────────────────────────

        private void ShowLoading(string message = "Cargando…")
        {
            SetPanels(loading: true, login: false, error: false);
            if (_loadingText != null) _loadingText.text = message;
        }

        private void ShowLoginPanel()
        {
            SetPanels(loading: false, login: true, error: false);
        }

        private void ShowError(string message)
        {
            SetPanels(loading: false, login: false, error: true);
            if (_errorText != null) _errorText.text = message;
        }

        private void SetPanels(bool loading, bool login, bool error)
        {
            if (_loadingPanel != null) _loadingPanel.SetActive(loading);
            if (_loginPanel   != null) _loginPanel.SetActive(login);
            if (_errorPanel   != null) _errorPanel.SetActive(error);
        }

        // ── Prompt de reintento ───────────────────────────────────────────────

        private TaskCompletionSource<bool> _retryTcs;

        private async Task<bool> ShowRetryPromptAsync(string message)
        {
            ShowError(message);
            _retryTcs = new TaskCompletionSource<bool>();
            return await _retryTcs.Task;
        }
    }
}
