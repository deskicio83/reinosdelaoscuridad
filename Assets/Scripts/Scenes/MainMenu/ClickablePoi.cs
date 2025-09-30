using UnityEngine;
using UnityEngine.EventSystems;

/// Adjunta a un edificio/portal (con Collider2D). Al pulsar, carga la escena indicada.
/// Futuro: en vez de cambiar de escena, puede abrir un panel modal (misiones, tienda, etc).
[RequireComponent(typeof(Collider2D))]
public class ClickablePoi : MonoBehaviour
{
    [SerializeField] private string sceneName;

    private void OnMouseUpAsButton()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (string.IsNullOrEmpty(sceneName)) return;

        SceneLoader.LoadScene(sceneName); // SceneLoader estático
    }
}
