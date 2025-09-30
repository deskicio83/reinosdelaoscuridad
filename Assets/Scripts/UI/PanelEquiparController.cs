/*
============================================================
PanelEquiparController.cs — Panel de inventario/auto-equip para un héroe
------------------------------------------------------------
PROPÓSITO
- Mostrar inventario agrupado por sets, filtrar por slot, previsualizar difs,
  y aplicar auto-equip (confirmado por AutoEquipConfirmPanelUI).

DEPENDENCIAS
- InventoryPanelUI, SetGroupUI, StatsPreviewPanelController, GearAutoEquipService,
  GearEquipService (aplicar cambios), GearEvents (refrescos).

MÉTODOS (COMPLETA AQUÍ)
- Show(heroId, slotFilter=null): fija héroe actual y popula inventario.
- PopulateInventory(): rellena sets e items (instancia GearItemUI).
- BuildAutoEquipProposal(): llama a GearAutoEquipService.
- OnConfirmAutoEquip(): abre AutoEquipConfirmPanelUI.Show(heroId, proposal).
============================================================
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PanelEquiparController : MonoBehaviour
{
    [Header("Header")]
    public Image iconInventory;
    public TMP_Text txtInventoryCount;
    public Button btnAuto;
    public Button btnClose;

    [Header("Scroll y Content")]
    public ScrollRect setsScroll;
    public Transform content;

    [Header("Prefabs")]
    public GameObject setGroupUIPrefab;
    public GameObject gearItemUIPrefab;

    [Header("Data")]
    public int maxInventory = 1600;

    [Header("Referencias UI adicionales")]
    public StatsPreviewPanelController statsPreviewPanel;
    public GameObject gearItemUIEmptyPrefab;
    public NewGearStatsPanelUI newGearStatsPanel;

    [Header("Modal blocker (Hijo de PanelEquipar)")]
    public Image modalBlocker; // Asigna el hijo UIBlockerPanel (Image con RaycastTarget ON)

    public string selectedSlotType { get; private set; }

    private Dictionary<string, SetGroupUI> setGroups = new Dictionary<string, SetGroupUI>();
    private AutoEquipConfirmPanelUI confirmPanel;

    public static string currentHeroId { get; private set; }

    void Awake()
    {
        // Cachea el confirmPanel incluso inactivo
        confirmPanel = FindFirstObjectByType<AutoEquipConfirmPanelUI>(FindObjectsInactive.Include);
        if (confirmPanel == null)
            confirmPanel = GetComponentInChildren<AutoEquipConfirmPanelUI>(true);

        if (confirmPanel != null)
        {
            // Suscríbete una sola vez: cuando el modal se cierre, ocultamos el blocker
            confirmPanel.Closed -= OnAnyModalClosed;
            confirmPanel.Closed += OnAnyModalClosed;
        }

        if (modalBlocker != null)
            modalBlocker.gameObject.SetActive(false);

        if (btnAuto != null)
        {
            btnAuto.onClick.RemoveAllListeners();
            btnAuto.onClick.AddListener(OnBtnAuto);
        }

        if (btnClose != null)
        {
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(() =>
            {
                Hide();
                var equipPanel = FindFirstObjectByType<HeroEquipmentPanelUI>(FindObjectsInactive.Include);
                if (equipPanel != null)
                    equipPanel.HideDetailPanel();
            });
        }
    }

    public void Show(string heroId, string slotType = null)
    {
        if (string.IsNullOrEmpty(heroId))
        {
            Debug.LogError("[PanelEquiparController] heroId está vacío o nulo");
            return;
        }
        // ANIMACIÓN
        UIAnimator.Appear(gameObject);


        gameObject.SetActive(true);

        currentHeroId = heroId;
        selectedSlotType = slotType;

        PopulateInventory();

        if (statsPreviewPanel != null)
            statsPreviewPanel.ShowStats(heroId);
        else
            Debug.LogWarning("[PanelEquiparController] statsPreviewPanel no está asignado");

        if (newGearStatsPanel != null)
        {
            newGearStatsPanel.Hide();
        }
    }

    public void PopulateInventory()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        int count = GameDataManager.Instance.PlayerData.gearInventory != null
            ? GameDataManager.Instance.PlayerData.gearInventory.Count : 0;
        if (txtInventoryCount != null)
            txtInventoryCount.text = $"{count}/{maxInventory}";

        Dictionary<string, List<GearInstance>> gearsBySet = new Dictionary<string, List<GearInstance>>();
        foreach (var gear in GameDataManager.Instance.PlayerData.gearInventory)
        {
            var gearCat = HeroCatalogManager.Instance.GetGearById(gear.gearId);
            string setId = gearCat != null ? gearCat.setId : "Unknown";
            if (!gearsBySet.ContainsKey(setId))
                gearsBySet[setId] = new List<GearInstance>();
            gearsBySet[setId].Add(gear);
        }

        foreach (var kvp in gearsBySet)
        {
            string setId = kvp.Key;
            List<GearInstance> gears = kvp.Value;

            GameObject setGO = Instantiate(setGroupUIPrefab, content);
            SetGroupUI setUI = setGO.GetComponent<SetGroupUI>();

            var setData = HeroCatalogManager.Instance.GetSetById(setId);
            string setIcon = null;
            if (setData != null && !string.IsNullOrEmpty(setData.setName))
                setIcon = $"Assets/Addressables/Heroes/Gear/_Icons/{setData.setName}.png";

            string setBonus = "";
            if (setData != null)
            {
                if (!string.IsNullOrEmpty(setData.bonus2))
                    setBonus += $"2 piezas: {setData.bonus2}";
                if (!string.IsNullOrEmpty(setData.bonus4))
                {
                    if (setBonus.Length > 0)
                        setBonus += "\n";
                    setBonus += $"4 piezas: {setData.bonus4}";
                }
            }

            setUI.Setup(
                setId,
                setData != null ? setData.setName : setId,
                setBonus,
                gears.Count,
                maxInventory,
                (gearInstance) => { /* click individual */ },
                gears,
                gearItemUIPrefab,
                setIcon,
                gearItemUIEmptyPrefab
            );

            setGroups[setId] = setUI;
        }
    }

    public void Hide()
    {
        if (modalBlocker != null)
            modalBlocker.gameObject.SetActive(false);

        gameObject.SetActive(false);
        // ANIMACIÓN
        UIAnimator.Disappear(gameObject);
    }

    private void OnBtnAuto()
    {
        if (string.IsNullOrEmpty(currentHeroId))
            return;

        var proposal = GearAutoEquipService.ComputeBestSet(currentHeroId);

        // PREVIEW: simula stats finales en el mismo click
        var simPlayer = GameDataManager.Instance.PlayerData;
        var simHero = simPlayer.heroes.Find(h => h.heroId == currentHeroId);
        var simulated = new List<GearInstance>(simHero.equipment ?? new List<GearInstance>());
        foreach (var kv in proposal)
        {
            var slotType = kv.Key;
            var newGear = kv.Value;
            if (newGear == null) continue;
            int idx = simulated.FindIndex(g => g != null && GearUtils.GetSlotTypeFromGearId(g.gearId) == slotType);
            if (idx >= 0) simulated[idx] = newGear;
            else simulated.Add(newGear);
        }
        if (statsPreviewPanel != null)
            statsPreviewPanel.ShowStatsAbsoluteWithEquipment(currentHeroId, simulated);

        // Abrir modal + activar blocker HIJO de PanelEquipar (si está asignado)
        if (confirmPanel != null)
        {
            OpenModal(confirmPanel.Root);
            confirmPanel.Show(currentHeroId, proposal, () =>
            {
                statsPreviewPanel?.ShowStats(currentHeroId);
                PopulateInventory();
            });
        }
        else
        {
            Debug.LogError("No se encontró AutoEquipConfirmPanelUI");
        }
    }

    private void OpenModal(GameObject modalRoot)
    {
        if (modalRoot == null) return;

        // Activa blocker del PanelEquipar y ordénalo justo detrás del modal
        if (modalBlocker != null)
        {
            modalBlocker.gameObject.SetActive(true);
            modalBlocker.transform.SetAsLastSibling(); // detrás…
        }

        modalRoot.SetActive(true);
        modalRoot.transform.SetAsLastSibling(); // …y el modal por encima
    }

    private void OnAnyModalClosed()
    {
        if (modalBlocker != null)
            modalBlocker.gameObject.SetActive(false);
    }

    // Suscripción segura al abrir/cerrar el panel
    private void OnEnable()
    {
        GearEvents.OnHeroGearChanged += OnHeroGearChanged;
    }

    private void OnDisable()
    {
        GearEvents.OnHeroGearChanged -= OnHeroGearChanged;
    }

    // Llamado cuando cambia el equipo de un héroe
    private void OnHeroGearChanged(string heroId)
    {
        // Si este panel no está visible, no hacemos nada
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy) return;

        // Si estás filtrando por héroe, respeta ese filtro
        if (!string.IsNullOrEmpty(currentHeroId) && currentHeroId != heroId) return;

        // Repinta con los filtros actuales (slot, rareza, etc.)
        PopulateInventory();    // usa el tuyo; si tu firma es PopulateInventory(heroId) llama con currentHeroId
    }
}
