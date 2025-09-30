/*
============================================================
HeroDetailTabMenuController.cs — Pestañas (Info, Habilidades, Despertar, Equipo)
------------------------------------------------------------
- Orquesta el cambio de paneles y coordina refrescos.
- Se suscribe a GearEvents para refrescar Equipo al equipar/desequipar.
============================================================
*/

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HeroDetailTabMenuController : MonoBehaviour
{
    [Header("Botones de las tabs")]
    public Button btnInfo;
    public Button btnHabilidad;
    public Button btnDespertar;
    public Button btnEquipo;
    public Button btnMaestria;
    public Button btnMisc;

    [Header("Paneles principales")]
    public GameObject panelInfo;
    public GameObject panelHabilidades;
    public GameObject panelDespertar;
    public GameObject panelEquipo;     // <- ESTE es el panel de equipo correcto
    public GameObject panelMaestria;
    public GameObject panelMisc;

    public HeroGridController heroGridController;

    private Dictionary<Button, GameObject> tabMapping;

    private HeroProgress currentHero;
    private HeroCatalogEntry currentCatalog;

    void Awake()
    {
        tabMapping = new Dictionary<Button, GameObject>
        {
            { btnInfo,       panelInfo },
            { btnHabilidad,  panelHabilidades },
            { btnDespertar,  panelDespertar },
            { btnEquipo,     panelEquipo },
            { btnMaestria,   panelMaestria },
            { btnMisc,       panelMisc }
        };

        // Enlaza clicks
        btnInfo     ?.onClick.AddListener(() => ShowPanel(panelInfo));
        btnHabilidad?.onClick.AddListener(() => ShowPanel(panelHabilidades));
        btnDespertar?.onClick.AddListener(() => ShowPanel(panelDespertar));
        btnEquipo   ?.onClick.AddListener(() => ShowPanel(panelEquipo));
        btnMaestria ?.onClick.AddListener(() => ShowPanel(panelMaestria));
        btnMisc     ?.onClick.AddListener(() => ShowPanel(panelMisc));
    }

    void Start()
    {
        ShowPanel(panelInfo); // por defecto

        // Pre-refresco de Equipo si ya tenemos héroe
        var equipoUI = panelEquipo != null ? panelEquipo.GetComponent<HeroEquipmentPanelUI>() : null;
        if (equipoUI != null && currentHero != null)
            equipoUI.SetEquipmentForHero(currentHero, null);
    }

    private void OnEnable()
    {
        GearEvents.OnHeroGearChanged += HandleGearChanged;
    }

    private void OnDisable()
    {
        GearEvents.OnHeroGearChanged -= HandleGearChanged;
    }

    // Refresco tras equipar/desequipar (lo dispara PanelEquiparController / AutoEquipConfirmPanelUI)
    private void HandleGearChanged(string heroId)
    {
        if (currentHero == null || currentHero.heroId != heroId) return;

        if (panelEquipo != null && panelEquipo.activeInHierarchy)
        {
            var equipoUI = panelEquipo.GetComponent<HeroEquipmentPanelUI>();
            if (equipoUI != null)
                equipoUI.SetEquipmentForHero(currentHero, null);
        }
    }

    public void ShowPanel(GameObject panelToShow)
    {
        // Oculta cualquier sub-UI de equipar que esté abierta
        var panelEquipar = FindFirstObjectByType<PanelEquiparController>();
        if (panelEquipar != null && panelEquipar.gameObject.activeSelf)
            panelEquipar.Hide();

        // Oculta todo
        if (panelInfo)        panelInfo.SetActive(false);
        if (panelHabilidades) panelHabilidades.SetActive(false);
        if (panelDespertar)   panelDespertar.SetActive(false);
        if (panelEquipo)      panelEquipo.SetActive(false);
        if (panelMaestria)    panelMaestria.SetActive(false);
        if (panelMisc)        panelMisc.SetActive(false);

        // Muestra y refresca el solicitado
        panelToShow?.SetActive(true);
        RefreshPanelData(panelToShow);
    }

    private void RefreshPanelData(GameObject panel)
    {
        if (panel == panelEquipo)
        {
            var equipoUI = panelEquipo.GetComponent<HeroEquipmentPanelUI>();
            if (equipoUI != null && currentHero != null)
                equipoUI.SetEquipmentForHero(currentHero, null);                
        }
        else if (panel == panelInfo)
        {
            var statsController = panelInfo.GetComponentInChildren<HeroStatsPanelController>(true);
            if (statsController != null && currentHero != null && currentCatalog != null)
                statsController.SetHero(currentHero, currentCatalog);

            var miscController = panelMisc != null ? panelMisc.GetComponentInChildren<HeroMiscPanelController>(true) : null;
            if (miscController != null && currentHero != null && currentCatalog != null)
                miscController.SetHero(currentHero, currentCatalog);

            if (heroGridController != null)
                heroGridController.ScrollToSelected();
        }
        else if (panel == panelHabilidades)
        {
            var skillsController = panelHabilidades.GetComponent<HeroSkillsPanelController>();
            if (skillsController != null && currentHero != null && currentCatalog != null)
                skillsController.SetSkills(currentCatalog.skills, currentHero.skills);
        }
        else if (panel == panelDespertar)
        {
            var despertarCtrl = panelDespertar.GetComponent<HeroDespertarPanelController>();
            if (despertarCtrl != null && currentHero != null && currentCatalog != null)
                despertarCtrl.SetHero(currentHero, currentCatalog);
        }
        else if (panel == panelMaestria)
        {
            var masteryUI = panelMaestria.GetComponent<HeroMasteryController>();
            if (masteryUI != null && currentHero != null)
                masteryUI.ShowForHero(currentHero);
        }
    }

    public void ShowInfoTab() => ShowPanel(panelInfo);

    // Llamado desde el grid de héroes
    public void SetHero(HeroProgress progress, HeroCatalogEntry catalog)
    {
        currentHero   = progress;
        currentCatalog= catalog;
        ShowPanel(panelInfo);
    }

    // Útil si algún panel necesita refrescar con el mismo héroe
    public void RefreshCurrentHeroPanels()
    {
        if (currentHero == null || currentCatalog == null) return;

        if (panelInfo != null && panelInfo.activeSelf)
        {
            var statsController = panelInfo.GetComponentInChildren<HeroStatsPanelController>(true);
            if (statsController != null)
                statsController.SetHero(currentHero, currentCatalog);
        }

        if (panelEquipo != null && panelEquipo.activeSelf)
        {
            var equipoUI = panelEquipo.GetComponent<HeroEquipmentPanelUI>();
            if (equipoUI != null)
                equipoUI.SetEquipmentForHero(currentHero, null);
        }
    }
}
