/*
============================================================
NavigationButtonClickHandler.cs — Encaminador genérico de navegación
------------------------------------------------------------
PROPÓSITO
- Asociar botones a escenas predefinidas (MainMenu, Heroes, Gacha...).

USO
- Asignar al botón y configurar destino en el Inspector.

MÉTODOS (COMPLETA AQUÍ)
- OnClick(): Llama a SceneLoader.Load(escenaDestino).
============================================================
*/


using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.InputSystem; // Input System moderno

public class NavigationButtonClickHandler : MonoBehaviour
{
    public string sceneKey = "HeroScene"; // Pon el nombre de la escena aquí o desde Inspector
    private Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Soporte para Mouse (PC)
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleScreenPoint(Mouse.current.position.ReadValue());
        }

        // Soporte multitouch (mobile)
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame)
                {
                    HandleScreenPoint(touch.position.ReadValue());
                }
            }
        }
    }

    void HandleScreenPoint(Vector2 screenPoint)
    {
        // Convertir a coordenadas del mundo
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, -mainCamera.transform.position.z));
        Vector2 clickPos = new Vector2(worldPoint.x, worldPoint.y);

        Collider2D hit = Physics2D.OverlapPoint(clickPos);
        if (hit != null && hit.gameObject == this.gameObject)
        {
            Debug.Log("Botón del mundo pulsado, navegando a: " + sceneKey);
            Addressables.LoadSceneAsync(sceneKey);
        }
    }
}
