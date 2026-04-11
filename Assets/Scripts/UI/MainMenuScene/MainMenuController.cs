using UnityEngine;
using UnityEngine.UI;
using ReinoOscuridad.Core;

namespace ReinoOscuridad.UI.MainMenu
{
    /// Hub central de navegación. Se coloca en MainMenuScene.
    /// Todos los botones se asignan via SerializeField desde el Inspector.
    public class MainMenuController : MonoBehaviour
    {
        // ── Prefab HUD ────────────────────────────────────────────────────────

        [Header("HUD")]
        [SerializeField] private GameObject _hudPrefab;

        // ── Botones de navegación ─────────────────────────────────────────────

        [Header("Botones")]
        [SerializeField] private Button _btnCampaign;
        [SerializeField] private Button _btnHeroes;
        [SerializeField] private Button _btnGacha;
        [SerializeField] private Button _btnArena;
        [SerializeField] private Button _btnTower;
        [SerializeField] private Button _btnWorldBoss;
        [SerializeField] private Button _btnClan;
        [SerializeField] private Button _btnShop;
        [SerializeField] private Button _btnMissions;
        [SerializeField] private Button _btnDungeon;
        [SerializeField] private Button _btnConjuros;

        // ── Ciclo de vida ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (UIManager.Instance == null)
            {
                Debug.LogError("[MainMenuController] UIManager.Instance es null. " +
                               "Asegúrate de que BootScene se ha cargado primero.");
                gameObject.SetActive(false);
                return;
            }
        }

        private void Start()
        {
            InstantiateHUD();
            BindButtons();
        }

        // ── HUD ───────────────────────────────────────────────────────────────

        private void InstantiateHUD()
        {
            if (_hudPrefab == null)
            {
                Debug.LogWarning("[MainMenuController] HUD prefab no asignado — " +
                                 "arrastra Assets/Prefabs/UI/HUD.prefab al Inspector.");
                return;
            }

            Instantiate(_hudPrefab);
            Debug.Log("[MainMenuController] HUD instanciado.");
        }

        // ── Enlace de botones ─────────────────────────────────────────────────

        private void BindButtons()
        {
            if (_btnCampaign  != null) _btnCampaign.onClick.AddListener(GoToCampaign);
            if (_btnHeroes    != null) _btnHeroes.onClick.AddListener(GoToHeroes);
            if (_btnGacha     != null) _btnGacha.onClick.AddListener(GoToGacha);
            if (_btnArena     != null) _btnArena.onClick.AddListener(GoToArena);
            if (_btnTower     != null) _btnTower.onClick.AddListener(GoToTower);
            if (_btnWorldBoss != null) _btnWorldBoss.onClick.AddListener(GoToWorldBoss);
            if (_btnClan      != null) _btnClan.onClick.AddListener(GoToClan);
            if (_btnShop      != null) _btnShop.onClick.AddListener(GoToShop);
            if (_btnMissions  != null) _btnMissions.onClick.AddListener(GoToMissions);
            if (_btnDungeon   != null) _btnDungeon.onClick.AddListener(GoToDungeon);
            if (_btnConjuros  != null) _btnConjuros.onClick.AddListener(GoToConjuros);
        }

        // ── Métodos de navegación ─────────────────────────────────────────────

        public void GoToCampaign()
        {
            Debug.Log("[MainMenuController] Navegando a CampaignScene");
            _ = UIManager.Instance.NavigateTo("CampaignScene");
        }

        public void GoToHeroes()
        {
            Debug.Log("[MainMenuController] Navegando a HeroScene");
            _ = UIManager.Instance.NavigateTo("HeroScene");
        }

        public void GoToGacha()
        {
            Debug.Log("[MainMenuController] Navegando a GachaScene");
            _ = UIManager.Instance.NavigateTo("GachaScene");
        }

        public void GoToArena()
        {
            Debug.Log("[MainMenuController] Navegando a ArenaScene");
            _ = UIManager.Instance.NavigateTo("ArenaScene");
        }

        public void GoToTower()
        {
            Debug.Log("[MainMenuController] Navegando a TowerScene");
            _ = UIManager.Instance.NavigateTo("TowerScene");
        }

        public void GoToWorldBoss()
        {
            Debug.Log("[MainMenuController] Navegando a WorldBossScene");
            _ = UIManager.Instance.NavigateTo("WorldBossScene");
        }

        public void GoToClan()
        {
            Debug.Log("[MainMenuController] Navegando a ClanScene");
            _ = UIManager.Instance.NavigateTo("ClanScene");
        }

        public void GoToShop()
        {
            Debug.Log("[MainMenuController] Navegando a ShopScene");
            _ = UIManager.Instance.NavigateTo("ShopScene");
        }

        public void GoToMissions()
        {
            Debug.Log("[MainMenuController] Navegando a MissionScene");
            _ = UIManager.Instance.NavigateTo("MissionScene");
        }

        public void GoToDungeon()
        {
            Debug.Log("[MainMenuController] Navegando a MazmorraScene");
            _ = UIManager.Instance.NavigateTo("MazmorraScene");
        }

        public void GoToConjuros()
        {
            Debug.Log("[MainMenuController] Navegando a ConjuroScene");
            _ = UIManager.Instance.NavigateTo("ConjuroScene");
        }
    }
}
