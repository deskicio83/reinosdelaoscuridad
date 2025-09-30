/*
============================================================
DebugActiveWatcher.cs — Inspector visual del estado activo/inactivo
------------------------------------------------------------
PROPÓSITO
- Mostrar por consola o UI si un GameObject cambia de activo/inactivo.

USO
- Añadir a objetos con activaciones dinámicas para depurar flujos UI.

RESPONSABILIDADES
- Registrar OnEnable/OnDisable y trazar.

MÉTODOS (COMPLETA AQUÍ)
- OnEnable/OnDisable: Logs claros con nombre del GO y ruta en jerarquía.
============================================================
*/

using System;
using UnityEngine;

public class DebugActiveWatcher : MonoBehaviour
{
    private bool lastState = true;

    void Awake()
    {
        lastState = gameObject.activeInHierarchy;
        //Debug.LogWarning($"[DebugActiveWatcher] Awake: Panel activo={lastState}", this);
    }

    private void OnDisable()
    {
        //Debug.LogError("[DebugActiveWatcher] OnDisable: Panel se HA OCULTADO", this);
        Debug.LogError(Environment.StackTrace);
    }
    private void OnEnable()
    {
        //Debug.LogWarning("[DebugActiveWatcher] OnEnable: Panel se ha ACTIVADO", this);
    }

    void Update()
    {
        if (gameObject.activeInHierarchy != lastState)
        {
            //Debug.LogError($"[DebugActiveWatcher] Cambiado en Update: Ahora activo={gameObject.activeInHierarchy}", this);
            lastState = gameObject.activeInHierarchy;
        }
    }
    CanvasGroup cg;
    float lastAlpha = -1;
    void Start()
    {
        cg = GetComponent<CanvasGroup>();
        if (cg != null) lastAlpha = cg.alpha;
    }

    void LateUpdate()
    {
        if (cg != null && Mathf.Abs(cg.alpha - lastAlpha) > 0.01f)
        {
            //Debug.LogWarning($"[DebugActiveWatcher] Alpha cambiado a: {cg.alpha}", this);
            lastAlpha = cg.alpha;
        }
    }
}
