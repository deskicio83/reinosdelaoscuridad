/*
============================================================
ScrollToTopOnEnable.cs — Auto-scroll al inicio
------------------------------------------------------------
PROPÓSITO
- Mover un ScrollRect a la parte superior cuando el panel se activa.

USO
- Añadir al mismo GO del ScrollRect o a su contenedor.

MÉTODOS (COMPLETA AQUÍ)
- OnEnable(): scrollRect.verticalNormalizedPosition = 1f.
============================================================
*/


using UnityEngine;
using UnityEngine.UI;

public class ScrollToTopOnEnable : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;

    void OnEnable()
    {
        // Espera un frame para que el layout calcule alturas
        StartCoroutine(FixNextFrame());
    }

    System.Collections.IEnumerator FixNextFrame()
    {
        yield return null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        scrollRect.verticalNormalizedPosition = 1f; // arriba del todo
    }

    // Llama a esto si reescribes el texto dinámicamente
    public void RefreshNow()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        scrollRect.verticalNormalizedPosition = 1f;
    }
}
