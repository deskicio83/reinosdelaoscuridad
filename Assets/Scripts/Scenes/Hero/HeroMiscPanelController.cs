/*
============================================================
HeroMiscPanelController.cs — Panel “miscelánea” del héroe
------------------------------------------------------------
PROPÓSITO
- Mostrar info adicional: afinidades, logros, historias, etc.

MÉTODOS (COMPLETA AQUÍ)
- SetHero(HeroProgress, HeroCatalogEntry).
============================================================
*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeroMiscPanelController : MonoBehaviour
{
    [Header("Estado Héroe")]
    [SerializeField] private Image candadoIcon;
    [SerializeField] private Sprite candadoAbiertoSprite;
    [SerializeField] private Sprite candadoCerradoSprite;
    [SerializeField] private Button blockHero;

    [SerializeField] private Image favoriteIcon;
    [SerializeField] private Sprite corazonVacioSprite;
    [SerializeField] private Sprite corazonLlenoSprite;
    [SerializeField] private Button favHero;

    [Header("Descripción")]
    [SerializeField] private Button btnDescHeroe;
    [SerializeField] private GameObject setDescPanel;     // Panel con el TMP_Text de descripción
    [SerializeField] private TMP_Text heroDescTxt;

    [Header("Eliminar Héroe")]
    [SerializeField] private Button deleteButton;         // Botón "Eliminar"
    [SerializeField] private GameObject deletePanel;      // Panel de confirmación
    [SerializeField] private TMP_Text deleteMsgText;      // Texto del panel
    [SerializeField] private Button okDeleteButton;       // OK
    [SerializeField] private Button cancelDeleteButton;   // Cancel

    private HeroProgress _currentHero;
    private HeroCatalogEntry _currentCatalog;

    private void Awake()
    {
        if (blockHero != null) blockHero.onClick.AddListener(ToggleLocked);
        if (favHero   != null) favHero.onClick.AddListener(ToggleFavorite);

        if (btnDescHeroe != null) btnDescHeroe.onClick.AddListener(ToggleDescriptionPanel);

        if (deleteButton      != null) deleteButton.onClick.AddListener(OnDeleteClicked);
        if (okDeleteButton    != null) okDeleteButton.onClick.AddListener(ConfirmDelete);
        if (cancelDeleteButton!= null) cancelDeleteButton.onClick.AddListener(HideDeletePanel);

        SafeHide(setDescPanel);
        SafeHide(deletePanel);
    }

    public void SetHero(HeroProgress hero, HeroCatalogEntry catalog)
    {
        _currentHero   = hero;
        _currentCatalog= catalog;

        if (heroDescTxt != null && _currentCatalog != null)
            heroDescTxt.text = _currentCatalog.description;

        RefreshLockFavIcons();
        SafeHide(setDescPanel);
        SafeHide(deletePanel);
    }

    // ---------- Bloqueo / Favorito ----------
    private void ToggleLocked()
    {
        if (_currentHero == null) return;
        _currentHero.locked = !_currentHero.locked;
        GameDataManager.Instance?.PlayerData?.Save();
        RefreshLockFavIcons();
    }

    private void ToggleFavorite()
    {
        if (_currentHero == null) return;
        _currentHero.favorite = !_currentHero.favorite;
        GameDataManager.Instance?.PlayerData?.Save();
        RefreshLockFavIcons();
    }

    private void RefreshLockFavIcons()
    {
        if (_currentHero == null) return;

        if (candadoIcon != null)
            candadoIcon.sprite = _currentHero.locked ? candadoCerradoSprite : candadoAbiertoSprite;

        if (favoriteIcon != null)
            favoriteIcon.sprite = _currentHero.favorite ? corazonLlenoSprite : corazonVacioSprite;
    }

    // ---------- Descripción ----------
    private void ToggleDescriptionPanel()
    {
        if (setDescPanel == null) return;
        bool show = !setDescPanel.activeSelf;
        if (show) SafeShow(setDescPanel);
        else      SafeHide(setDescPanel);
    }

    private void OnDisable()
    {
        // Si se cambia de pestaña/panel, garantizamos ocultar la descripción y confirmación
        SafeHide(setDescPanel);
        SafeHide(deletePanel);
    }

    // ---------- Eliminar Héroe ----------
    private void OnDeleteClicked()
    {
        if (_currentHero == null || deletePanel == null) return;

        if (_currentHero.locked)
        {
            if (deleteMsgText != null)
                deleteMsgText.text = "El héroe está bloqueado.\nDesbloquéalo para poder eliminarlo.";
            if (okDeleteButton != null) okDeleteButton.gameObject.SetActive(false);
            SafeShow(deletePanel);
            return;
        }

        string name = _currentCatalog != null ? _currentCatalog.displayName : _currentHero.heroId;
        if (deleteMsgText != null)
            deleteMsgText.text = $"Se eliminará «{name}».\n¿Deseas continuar?";
        if (okDeleteButton != null) okDeleteButton.gameObject.SetActive(true);
        SafeShow(deletePanel);
    }

    private void HideDeletePanel()
    {
        SafeHide(deletePanel);
    }

    private void ConfirmDelete()
    {
        if (_currentHero == null) { SafeHide(deletePanel); return; }

        var gdm = GameDataManager.Instance;
        var pd  = gdm != null ? gdm.PlayerData : null;
        if (pd == null) { SafeHide(deletePanel); return; }

        // 1) Devolver equipo al inventario y limpiar al héroe
        if (_currentHero.equipment != null && _currentHero.equipment.Count > 0)
        {
            // PlayerData usa 'gearInventory' (no 'inventory')
            if (pd.gearInventory == null) pd.gearInventory = new System.Collections.Generic.List<GearInstance>();
            foreach (var eq in _currentHero.equipment)
            {
                if (eq != null) pd.gearInventory.Add(eq);
            }
            _currentHero.equipment.Clear();
        }

        // 2) Eliminar héroe del listado
        pd.heroes.RemoveAll(h => h.heroId == _currentHero.heroId);

        // 3) Guardar y notificar
        pd.Save();
        SafeHide(deletePanel);

        // 4) Notificar a todos los paneles/UIS
        GearEvents.RaiseHeroDeleted(_currentHero.heroId);
    }

    // ---------- Utilidades de fade/activación ----------
    private void SafeShow(GameObject go)
    {
        if (go == null) return;
        var f = go.GetComponent<PanelFader>();
        go.SetActive(true);
        if (f != null) f.FadeIn();
    }

    private void SafeHide(GameObject go)
    {
        if (go == null) return;
        var f = go.GetComponent<PanelFader>();
        if (f != null) f.FadeOut();
        else go.SetActive(false);
    }
}
