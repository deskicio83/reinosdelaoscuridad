using System;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Auth;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;
using ReinoOscuridad.Systems;

namespace ReinoOscuridad.Firebase
{
    /// Gestiona el ciclo de autenticación con Firebase Auth.
    /// Soporta Google, Apple, Facebook y modo invitado (anónimo).
    /// Firebase.FirebaseApp debe estar inicializado antes de llamar cualquier método
    /// de login — la inicialización ocurre en BootScene (S08).
    [DefaultExecutionOrder(-20)]
    public class AuthSystem : MonoBehaviour, ISystem
    {
        // ── Singleton ──────────────────────────────────────────────────────────

        public static AuthSystem Instance { get; private set; }

        // ── Estado público ────────────────────────────────────────────────────

        public bool   IsLoggedIn  { get; private set; }
        public bool   IsGuest     { get; private set; }
        public string CurrentUID  { get; private set; } = string.Empty;

        // ── Referencia interna ────────────────────────────────────────────────

        private FirebaseAuth        _auth;
        private PlayerDataSystem    _pds;

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

        // ── ISystem ───────────────────────────────────────────────────────────

        public void Initialize()
        {
            _pds = GameManager.Instance.GetSystem<PlayerDataSystem>();

            // Firebase puede no estar inicializado aún (BootScene lo hace en S08).
            // El try/catch evita que un crash aquí bloquee el resto del arranque.
            try
            {
                _auth = FirebaseAuth.DefaultInstance;

                var user = _auth.CurrentUser;
                if (user != null)
                    ApplyAuthState(user);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AuthSystem] Firebase no disponible en Initialize(): {e.Message}. Se inicializará en BootScene.");
            }
        }

        public void OnSessionStart() { }

        public void OnSessionEnd() { }

        // ── API pública — Login ───────────────────────────────────────────────

        /// Login anónimo (invitado). Crea una cuenta temporal en Firebase.
        public async Task LoginAsGuest()
        {
            try
            {
                EnsureAuth();
                var result = await _auth.SignInAnonymouslyAsync();
                ApplyAuthState(result.User);
                Debug.Log($"[AuthSystem] Login invitado OK — uid: {CurrentUID}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthSystem] LoginAsGuest falló: {e.Message}");
                throw;
            }
        }

        /// Login con Google. Requiere Google Sign-In SDK externo (pendiente de integrar).
        /// <param name="idToken">ID token obtenido del SDK de Google Sign-In.</param>
        /// <param name="accessToken">Access token (puede ser null para Sign-In with Google).</param>
        public async Task LoginWithGoogle(string idToken, string accessToken = null)
        {
            try
            {
                EnsureAuth();
                var credential = GoogleAuthProvider.GetCredential(idToken, accessToken);
                var user = await _auth.SignInWithCredentialAsync(credential);
                ApplyAuthState(user);
                Debug.Log($"[AuthSystem] Login Google OK — uid: {CurrentUID}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthSystem] LoginWithGoogle falló: {e.Message}");
                throw;
            }
        }

        /// Login con Apple. Requiere Sign in with Apple plugin (pendiente de integrar).
        /// <param name="idToken">ID token de la respuesta de Apple.</param>
        /// <param name="rawNonce">Nonce crudo usado al generar el request.</param>
        public async Task LoginWithApple(string idToken, string rawNonce)
        {
            try
            {
                EnsureAuth();
                var credential = OAuthProvider.GetCredential("apple.com", idToken, rawNonce, null);
                var user = await _auth.SignInWithCredentialAsync(credential);
                ApplyAuthState(user);
                Debug.Log($"[AuthSystem] Login Apple OK — uid: {CurrentUID}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthSystem] LoginWithApple falló: {e.Message}");
                throw;
            }
        }

        /// Login con Facebook. Requiere Facebook SDK (pendiente de integrar).
        /// <param name="accessToken">Access token obtenido del Facebook SDK.</param>
        public async Task LoginWithFacebook(string accessToken)
        {
            try
            {
                EnsureAuth();
                var credential = FacebookAuthProvider.GetCredential(accessToken);
                var user = await _auth.SignInWithCredentialAsync(credential);
                ApplyAuthState(user);
                Debug.Log($"[AuthSystem] Login Facebook OK — uid: {CurrentUID}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AuthSystem] LoginWithFacebook falló: {e.Message}");
                throw;
            }
        }

        // ── API pública — Logout ──────────────────────────────────────────────

        public void Logout()
        {
            try
            {
                EnsureAuth();
                _auth.SignOut();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AuthSystem] SignOut con error: {e.Message}");
            }
            finally
            {
                ClearAuthState();
                Debug.Log("[AuthSystem] Sesión cerrada.");
            }
        }

        // ── Helpers privados ───────────────────────────────────────────────────

        private void ApplyAuthState(FirebaseUser user)
        {
            IsLoggedIn = true;
            IsGuest    = user.IsAnonymous;
            CurrentUID = user.UserId;

            _pds?.SetUID(CurrentUID);

            EventBus.Publish(new AuthStateChangedData
            {
                uid     = CurrentUID,
                isGuest = IsGuest
            });
        }

        private void ClearAuthState()
        {
            IsLoggedIn = false;
            IsGuest    = false;
            CurrentUID = string.Empty;

            _pds?.SetUID(string.Empty);

            EventBus.Publish(new AuthStateChangedData
            {
                uid     = string.Empty,
                isGuest = false
            });
        }

        /// Modo desarrollador: fija un uid sin pasar por Firebase Auth.
        /// Solo para UNITY_EDITOR / DEVELOPMENT_BUILD.
        public void SetDevUID(string uid)
        {
            IsLoggedIn = true;
            IsGuest    = true;
            CurrentUID = uid;
            _pds?.SetUID(uid);
            EventBus.Publish(new AuthStateChangedData { uid = uid, isGuest = true });
            Debug.Log($"[AuthSystem] Dev mode — uid fijo: {uid}");
        }

        private void EnsureAuth()
        {
            if (_auth == null)
                _auth = FirebaseAuth.DefaultInstance;
        }
    }
}
