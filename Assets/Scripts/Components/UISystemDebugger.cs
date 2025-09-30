/*
============================================================
UISystemDebugger.cs — Inspector/overlay de depuración UI
------------------------------------------------------------
PROPÓSITO
- Mostrar info en vivo: raycasts, orden de capas, bloqueadores, etc.

USO
- Añadir en escenas para depurar problemas de clics/orden.

MÉTODOS (COMPLETA AQUÍ)
- ToggleOverlay(): on/off.
- DumpHierarchy(): vuelca jerarquía con estados activos.
============================================================
*/

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class UISystemDebugger : MonoBehaviour
{
    void Update()
    {
        // Usamos el nuevo sistema de Input
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            if (results.Count == 0)
            {
                Debug.Log("⚠️ [UI DEBUG] No UI element hit");
                return;
            }

            Debug.Log($"🔎 [UI DEBUG] Raycast hits ({results.Count}):");

            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                Debug.Log($"[{i}] {GetFullPath(r.gameObject)} " +
                          $"(Layer:{r.gameObject.layer}, SortingOrder:{r.sortingOrder}, Depth:{r.depth})");
            }

            Debug.Log($"👉 [UI DEBUG] Bloqueador real: {GetFullPath(results[0].gameObject)}");
        }
    }

    // Devuelve el path completo en la jerarquía (Canvas/Panel/Objeto)
    private string GetFullPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;

        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }
}
