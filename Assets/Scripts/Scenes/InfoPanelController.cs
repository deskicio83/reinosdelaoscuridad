/*
============================================================
InfoPanelController.cs — Panel informativo de héroe
------------------------------------------------------------
PROPÓSITO
- Mostrar nombre, rareza, elemento, descripción corta, etc.

MÉTODOS (COMPLETA AQUÍ)
- SetHero(HeroProgress, HeroCatalogEntry): actualiza textos/iconos.
- Show()/Hide().
============================================================
*/

using UnityEngine;

public class InfoPanelController : MonoBehaviour
{
    public GameObject infoPanel;
    public GameObject uiBlockerPanel; // <-- ASÍGNALO DESDE EL INSPECTOR

    public void ShowPanel()
    {
        if (uiBlockerPanel != null)
            uiBlockerPanel.SetActive(true); // Activa bloqueador detrás

        if (infoPanel != null)
            infoPanel.SetActive(true);
    }

    public void HidePanel()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);

        if (uiBlockerPanel != null)
            uiBlockerPanel.SetActive(false); // Oculta bloqueador también
    }
}