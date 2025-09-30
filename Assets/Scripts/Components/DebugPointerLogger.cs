/*
============================================================
DebugPointerLogger.cs — Logger de eventos de puntero
------------------------------------------------------------
PROPÓSITO
- Trazar eventos de entrada (pointer down/up/enter/exit) sobre un GO UI.

USO
- Adjuntar a elementos problemáticos para comprobar bloqueo de raycasts.

MÉTODOS (COMPLETA AQUÍ)
- IPointerDown/Up/Enter/ExitHandler: Logs con nombre del GO y orden.
============================================================
*/

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DebugPointerLogger : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[UI DEBUG] Click detectado en {gameObject.name}");

        // Recolectar todos los hits bajo el ratón
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        Debug.Log($"[UI DEBUG] Total hits: {results.Count}");

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            Debug.Log($"[{i}] {r.gameObject.name} (Layer:{r.gameObject.layer}, SortingOrder:{r.sortingOrder}, Depth:{r.depth})");
        }

        if (results.Count > 0)
            Debug.Log($"👉 BLOQUEADOR real: {results[0].gameObject.name}");
    }
}
