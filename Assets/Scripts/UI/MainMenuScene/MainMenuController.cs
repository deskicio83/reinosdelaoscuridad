using UnityEngine;
using UnityEngine.UI;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;
using ReinoOscuridad.Systems;

namespace ReinoOscuridad.UI.MainMenu
{
    /// Hub central de navegación. Se coloca en MainMenuScene.
    /// Todos los botones se asignan via SerializeField desde el Inspector.
    public class MainMenuController : MonoBehaviour
    {
        // ── Desbloqueos por nivel ──────────────────────────────────────────────

        /// Nivel mínimo de jugador para acceder a cada edificio (índices 0-10).
        /// Orden: Portal · Altar · Campana · Arena · Cuartel · Forja ·
        ///        Biblioteca · Mercado · Taverna · Mazmorra · Misiones
        private static readonly int[] NIVEL_REQUERIDO_EDIFICIO =
        {
             1, // 0 Portal     (Campaign)
             1, // 1 Altar      (Gacha)
             5, // 2 Campana    (Campaign 2)
            10, // 3 Arena
             1, // 4 Cuartel    (Heroes)
            15, // 5 Forja      (Conjuros)
            20, // 6 Biblioteca (Gacha avanzado)
             5, // 7 Mercado    (Shop)
            25, // 8 Taverna    (Clan)
            30, // 9 Mazmorra
            10, // 10 Misiones
        };

        [Header("Desbloqueos por Nivel")]
        [SerializeField] private GameObject[] _lockOverlays;  // [11] uno por edificio

        // ── Prefab HUD ────────────────────────────────────────────────────────

        [Header("HUD")]
        [SerializeField] private GameObject _hudPrefab;

        // ── Abanico Triloguzano ───────────────────────────────────────────────

        [Header("Abanico")]
        [SerializeField] private GameObject _subIconosAbanico;

        // ── Botones de edificios ──────────────────────────────────────────────

        [Header("Botones Edificios")]
        [SerializeField] private Button _btnCampaign;
        [SerializeField] private Button _btnHeroes;
        [SerializeField] private Button _btnGacha;
        [SerializeField] private Button _btnArena;
        [SerializeField] private Button _btnShop;
        [SerializeField] private Button _btnClan;
        [SerializeField] private Button _btnMissions;
        [SerializeField] private Button _btnDungeon;
        [SerializeField] private Button _btnConjuros;

        // ── Botones acceso rápido ─────────────────────────────────────────────

        [Header("Accesos Rapidos")]
        [SerializeField] private Button _btnTriloguzano;
        [SerializeField] private Button _btnTower;
        [SerializeField] private Button _btnWorldBoss;
        [SerializeField] private Button _btnMazmorraRapido;
        [SerializeField] private Button _btnEvent;
        [SerializeField] private Button _btnProfile;

        // ── Botones HUD acciones ──────────────────────────────────────────────

        [Header("HUD Acciones")]
        [SerializeField] private Button _btnChat;
        [SerializeField] private Button _btnMail;
        [SerializeField] private Button _btnSettings;

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
            RefreshEdificiosLock();
            EventBus.OnPlayerLevelUp += OnPlayerLevelUp;

            if (_subIconosAbanico != null)
                _subIconosAbanico.SetActive(false);
        }

        private void OnDestroy()
        {
            EventBus.OnPlayerLevelUp -= OnPlayerLevelUp;
        }

        private void OnPlayerLevelUp(PlayerLevelUpData data) => RefreshEdificiosLock();

        // ── Desbloqueos ───────────────────────────────────────────────────────

        /// Muestra u oculta el overlay de bloqueo de cada edificio según el nivel del jugador.
        public void RefreshEdificiosLock()
        {
            if (_lockOverlays == null) return;
            var pds = PlayerDataSystem.Instance;
            int playerLevel = pds?.GetPlayerData()?.playerLevel ?? 1;

            for (int i = 0; i < _lockOverlays.Length && i < NIVEL_REQUERIDO_EDIFICIO.Length; i++)
            {
                if (_lockOverlays[i] == null) continue;
                _lockOverlays[i].SetActive(playerLevel < NIVEL_REQUERIDO_EDIFICIO[i]);
            }
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
            if (_btnCampaign      != null) _btnCampaign.onClick.AddListener(GoToCampaign);
            if (_btnHeroes        != null) _btnHeroes.onClick.AddListener(GoToHeroes);
            if (_btnGacha         != null) _btnGacha.onClick.AddListener(GoToGacha);
            if (_btnArena         != null) _btnArena.onClick.AddListener(GoToArena);
            if (_btnShop          != null) _btnShop.onClick.AddListener(GoToShop);
            if (_btnClan          != null) _btnClan.onClick.AddListener(GoToClan);
            if (_btnMissions      != null) _btnMissions.onClick.AddListener(GoToMissions);
            if (_btnDungeon       != null) _btnDungeon.onClick.AddListener(GoToDungeon);
            if (_btnConjuros      != null) _btnConjuros.onClick.AddListener(GoToConjuros);
            if (_btnTriloguzano   != null) _btnTriloguzano.onClick.AddListener(GoToTriloguzano);
            if (_btnTower         != null) _btnTower.onClick.AddListener(GoToTower);
            if (_btnWorldBoss     != null) _btnWorldBoss.onClick.AddListener(GoToWorldBoss);
            if (_btnMazmorraRapido != null) _btnMazmorraRapido.onClick.AddListener(GoToDungeon);
            if (_btnEvent         != null) _btnEvent.onClick.AddListener(GoToEvent);
            if (_btnProfile       != null) _btnProfile.onClick.AddListener(GoToProfile);
            if (_btnChat          != null) _btnChat.onClick.AddListener(GoToChat);
            if (_btnMail          != null) _btnMail.onClick.AddListener(GoToMail);
            if (_btnSettings      != null) _btnSettings.onClick.AddListener(GoToSettings);
        }

        // ── Métodos de navegación — edificios ─────────────────────────────────

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

        // ── Métodos nuevos S10c ───────────────────────────────────────────────

        public void GoToTriloguzano()
        {
            if (_subIconosAbanico != null)
                _subIconosAbanico.SetActive(!_subIconosAbanico.activeSelf);
        }

        public void GoToChat()
        {
            Debug.Log("[MainMenuController] TODO: ChatPanel slide-in S32");
        }

        public void GoToMail()
        {
            Debug.Log("[MainMenuController] TODO: MailPanel overlay S32");
        }

        public void GoToSettings()
        {
            Debug.Log("[MainMenuController] TODO: SettingsPanel overlay S32");
        }

        public void GoToEvent()
        {
            Debug.Log("[MainMenuController] TODO: EventPanel overlay S32");
        }

        public void GoToProfile()
        {
            Debug.Log("[MainMenuController] TODO: PlayerInfoPanel overlay S32");
        }
    }
}
