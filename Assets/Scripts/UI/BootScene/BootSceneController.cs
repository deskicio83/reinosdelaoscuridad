using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using ReinoOscuridad.Core;
using ReinoOscuridad.Firebase;
using ReinoOscuridad.Systems;

namespace ReinoOscuridad.UI.Boot
{
    /// Gestiona el flujo completo de arranque:
    /// Firebase init → Auth → Carga Firestore → Energia offline → Navegacion.
    /// En UNITY_EDITOR/DEVELOPMENT_BUILD siempre muestra LoginPanel (incluye boton MODO DEV).
    /// En release: si hay sesion activa salta LoginPanel; si no, lo muestra.
    public class BootSceneController : MonoBehaviour
    {
        // ── Referencias UI ────────────────────────────────────────────────────

        [Header("Paneles")]
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private GameObject _loginPanel;
        [SerializeField] private GameObject _errorPanel;

        [Header("Textos")]
        [SerializeField] private TextMeshProUGUI _loadingText;
        [SerializeField] private TextMeshProUGUI _errorText;

        [Header("Botones de login")]
        [SerializeField] private Button _btnGoogle;
        [SerializeField] private Button _btnApple;
        [SerializeField] private Button _btnGuest;

        [Header("Boton reintentar")]
        [SerializeField] private Button _btnRetry;

        [Header("Boton modo dev (solo Editor/DevBuild)")]
        [SerializeField] private Button _btnDevMode;

        // ── Ciclo de vida Unity ────────────────────────────────────────────────

        private async void Start()
        {
            await RunBootSequenceAsync();
        }

        // ── Flujo principal ───────────────────────────────────────────────────

        private async Task RunBootSequenceAsync()
        {
            ShowLoading("Iniciando...");

            var status = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (status != DependencyStatus.Available)
            {
                ShowError($"Firebase no disponible: {status}\nComprueba tu conexion e intentalo de nuevo.");
                Debug.LogError($"[BootSceneController] Firebase DependencyStatus: {status}");
                return;
            }

            Debug.Log("[BootSceneController] Firebase OK.");

            if (GameManager.Instance == null)
            {
                ShowError("Error interno: GameManager no encontrado.\nAniade GameManager, PlayerDataSystem, EconomySystem, AuthSystem y DataStorageSystem a BootScene.");
                Debug.LogError("[BootSceneController] GameManager.Instance es null.");
                return;
            }

            if (!ValidateSystems()) return;

            // Ocultar BtnDevMode en builds release
#if !DEVELOPMENT_BUILD && !UNITY_EDITOR
            if (_btnDevMode != null) _btnDevMode.gameObject.SetActive(false);
#endif

            // En Editor/DevBuild: mostrar siempre LoginPanel para poder elegir modo dev
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            ShowLoginPanel();
            return;
#else
            var auth = GameManager.Instance.GetSystem<AuthSystem>();
            if (auth.IsLoggedIn)
            {
                await ProceedAfterLoginAsync();
                return;
            }
            ShowLoginPanel();
#endif
        }

        // ── Post-login: carga datos y navega ──────────────────────────────────

        private async Task ProceedAfterLoginAsync()
        {
            ShowLoading("Cargando datos...");

            var auth = GameManager.Instance.GetSystem<AuthSystem>();
            var dss  = GameManager.Instance.GetSystem<DataStorageSystem>();
            var pds  = GameManager.Instance.GetSystem<PlayerDataSystem>();
            var eco  = GameManager.Instance.GetSystem<EconomySystem>();
            var ui   = GameManager.Instance.GetSystem<UIManager>();

            await dss.LoadPlayerDataFromFirestore(auth?.CurrentUID ?? "");

            eco.Initialize();
            GameManager.Instance.NotifySessionStart(pds.GetPlayerData().lastLoginTimestamp);

            ShowLoading("Entrando...");

            bool tutorialDone = pds.GetPlayerData().tutorialCompleted;

            if (tutorialDone)
            {
                Debug.Log("[BootSceneController] → MainMenuScene");
                await ui.NavigateTo("MainMenuScene");
            }
            else
            {
                // TutorialScene no existe aun (se implementa en S33).
                Debug.LogWarning("[BootSceneController] TutorialScene no disponible — navegando a MainMenuScene como fallback.");
                await ui.NavigateTo("MainMenuScene");
            }
        }

        // ── Botones públicos (cableados por SetupBootScene via UnityEventTools) ─

        /// Login con Google — SDK pendiente (S25).
        public void LoginWithGoogle()
            => ShowError("Google Sign-In SDK pendiente de integrar (S25).\nUsa 'Invitado' o 'Modo Dev'.");

        /// Login con Apple — SDK pendiente (S25, iOS).
        public void LoginWithApple()
            => ShowError("Apple Sign-In pendiente de integrar (iOS, S25).\nUsa 'Invitado' o 'Modo Dev'.");

        /// Login anonimo como invitado → Firebase Auth → ProceedAfterLogin.
        public async void LoginAsGuest()
        {
            ShowLoading("Iniciando sesion...");
            try
            {
                var auth = GameManager.Instance.GetSystem<AuthSystem>();
                await auth.LoginAsGuest();
                await ProceedAfterLoginAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[BootSceneController] LoginAsGuest fallo: {e.Message}");
                ShowError($"Error al iniciar sesion:\n{e.Message}");
            }
        }

        /// Modo desarrollador — salta autenticacion Firebase completamente.
        /// Solo visible en UNITY_EDITOR / DEVELOPMENT_BUILD.
        public async void DevModeLogin()
        {
            const string DEV_UID = "dev_mode_player_001";
            Debug.LogWarning("[BootSceneController] MODO DEV — uid fijo: " + DEV_UID);
            var auth = GameManager.Instance?.GetSystem<AuthSystem>();
            auth?.SetDevUID(DEV_UID);
            await ProceedAfterLoginAsync();
        }

        /// Reintentar arranque completo (usado por BtnRetry en panel de error).
        public void RetryInit() => _ = RunBootSequenceAsync();

        // ── Gestión de UI ─────────────────────────────────────────────────────

        private void ShowLoading(string message = "Cargando...")
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

        // ── Validación de sistemas ────────────────────────────────────────────

        private bool ValidateSystems()
        {
            var auth = GameManager.Instance.GetSystem<AuthSystem>();
            var dss  = GameManager.Instance.GetSystem<DataStorageSystem>();
            var pds  = GameManager.Instance.GetSystem<PlayerDataSystem>();
            var eco  = GameManager.Instance.GetSystem<EconomySystem>();

            if (auth != null && dss != null && pds != null && eco != null) return true;

            ShowError("Error interno: sistemas no encontrados.\nAniade AuthSystem, DataStorageSystem, PlayerDataSystem y EconomySystem a BootScene.");
            Debug.LogError($"[BootSceneController] Sistemas faltantes — auth:{auth != null} dss:{dss != null} pds:{pds != null} eco:{eco != null}");
            return false;
        }
    }
}
