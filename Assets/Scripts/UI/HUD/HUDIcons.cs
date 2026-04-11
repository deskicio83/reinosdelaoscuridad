using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ReinoOscuridad.Core;

namespace ReinoOscuridad.UI.HUD
{
    /// Gestiona los iconos de chat, mail y settings del HUD.
    /// Los prefabs de overlay se asignarán en S32; hasta entonces los botones
    /// loguean un aviso TODO si el prefab no está asignado.
    public class HUDIcons : MonoBehaviour
    {
        [Header("Botones")]
        [SerializeField] private Button _chatButton;
        [SerializeField] private Button _mailButton;
        [SerializeField] private Button _settingsButton;

        [Header("Badge de mail")]
        [SerializeField] private GameObject _mailBadge;
        [SerializeField] private TMP_Text   _mailBadgeText;

        [Header("Prefabs de overlay — asignar en S32")]
        [SerializeField] private GameObject _chatPrefab;
        [SerializeField] private GameObject _mailPrefab;
        [SerializeField] private GameObject _settingsPrefab;

        // ── Ciclo de vida ──────────────────────────────────────────────────────

        private void Start()
        {
            if (_chatButton     != null) _chatButton.onClick.AddListener(OnChatPressed);
            if (_mailButton     != null) _mailButton.onClick.AddListener(OnMailPressed);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OnSettingsPressed);
        }

        // ── API pública ────────────────────────────────────────────────────────

        /// Muestra u oculta el badge rojo de mail con el número de mensajes.
        public void UpdateMailBadge(int count)
        {
            if (_mailBadge == null) return;
            _mailBadge.SetActive(count > 0);
            if (_mailBadgeText != null)
                _mailBadgeText.text = count > 99 ? "99+" : count.ToString();
        }

        // ── Handlers privados ──────────────────────────────────────────────────

        private void OnChatPressed()
        {
            if (_chatPrefab == null)
            {
                Debug.Log("[HUDIcons] TODO: overlay chat pendiente S32");
                return;
            }
            UIManager.Instance?.ShowOverlay(_chatPrefab);
        }

        private void OnMailPressed()
        {
            if (_mailPrefab == null)
            {
                Debug.Log("[HUDIcons] TODO: overlay mail pendiente S32");
                return;
            }
            UIManager.Instance?.ShowOverlay(_mailPrefab);
        }

        private void OnSettingsPressed()
        {
            if (_settingsPrefab == null)
            {
                Debug.Log("[HUDIcons] TODO: overlay settings pendiente S32");
                return;
            }
            UIManager.Instance?.ShowOverlay(_settingsPrefab);
        }
    }
}
