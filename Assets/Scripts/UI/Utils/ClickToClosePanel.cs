/*
============================================================
ClickToClosePanel.cs — Cierre por clic fuera (etiquetas)
------------------------------------------------------------
PROPÓSITO
- Al hacer clic en el fondo/bloqueador, cerrar paneles con un tag dado.

USO
- Asignar a un GO que hace de blocker; configurar panelTag.

MÉTODOS
- OnPointerClick(PointerEventData):
  Cierra todos los paneles con “panelTag” y luego se oculta a sí mismo.
============================================================
*/

using UnityEngine;
using UnityEngine.EventSystems;

public class ClickToClosePanel : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string panelTag = "UIPanel";

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.pointerEnter == gameObject)
        {
            // Primero cerramos todos los paneles con ese tag
            var panels = GameObject.FindGameObjectsWithTag(panelTag);

            foreach (var panel in panels)
            {
                // No cerrar aún el propio UIBlockerPanel
                if (panel != gameObject)
                    panel.SetActive(false);
            }

            // Y luego cerramos el propio UIBlockerPanel
            gameObject.SetActive(false);
        }
    }
}
