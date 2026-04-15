using UnityEngine;
using UnityEngine.UI;
using ReinoOscuridad.Core;

namespace ReinoOscuridad.Utils
{
    /// Componente de navegación de desarrollo — adjuntado por SetupTestNavButtons.
    /// Llama a UIManager.Instance.NavigateTo(targetScene) al hacer click.
    /// SOLO para uso en desarrollo/testing.
    [RequireComponent(typeof(Button))]
    public class NavButtonHandler : MonoBehaviour
    {
        public string targetScene;

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(async () =>
            {
                if (UIManager.Instance != null)
                    await UIManager.Instance.NavigateTo(targetScene);
                else
                    Debug.LogWarning($"[NavButtonHandler] UIManager no disponible — no se puede navegar a {targetScene}.");
            });
        }
    }
}
