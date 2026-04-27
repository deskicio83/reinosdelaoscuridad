using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

        // Nivel mínimo por nombre de GO. Claves = nombres exactos de ContentEdificios.
        private static readonly Dictionary<string, int> _nivelRequerido = new()
        {
            { "Edificio_Porton",     1  },  // Portal → Campaña
            { "Edificio_Campana",    1  },  // Campaña 2
            { "Edificio_Cuartel",    1  },  // Héroes / Esbirros
            { "Edificio_Mercado",    1  },  // Tienda
            { "Edificio_Misiones",   1  },  // Misiones
            { "Edificio_Altar",      2  },  // Gacha
            { "Edificio_Biblioteca", 2  },  // Gacha avanzado
            { "Edificio_Arena",      3  },  // Arena
            { "Edificio_Taverna",    5  },  // Clan
            { "Edificio_Forja",      7  },  // Conjuros
            { "Edificio_Mazmorra",   10 },  // Torre + Boss + Mazm
        };

        [Header("Desbloqueos por Nivel")]
        [SerializeField] private GameObject[] _lockOverlays;  // mantenido por compatibilidad Setup

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
            StartCoroutine(CentrarScrollEnMundoActual());
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

        public void RefreshEdificiosLock()
        {
            int nivel     = PlayerProgressionSystem.Instance?.GetPlayerNivel() ?? 1;
            var edificios = GetEdificiosEnContent();

            for (int i = 0; i < edificios.Length; i++)
            {
                string nombre   = edificios[i].name;
                bool   bloqueado = _nivelRequerido.TryGetValue(nombre, out int req) && nivel < req;

                var btn = edificios[i].GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = !bloqueado;
                    // btn.enabled=false bloquea touch en dispositivos donde interactable no basta
                    btn.enabled = !bloqueado;
                }

                var lockOv = edificios[i].transform.Find("LockOverlay")?.gameObject;
                if (lockOv != null)
                {
                    lockOv.SetActive(bloqueado);
                    var images = lockOv.GetComponentsInChildren<Image>(true);
                    foreach (var img in images)
                        img.raycastTarget = true;
                }

                var nivelTxt = edificios[i].transform
                    .Find("LockOverlay/NivelReqText")
                    ?.GetComponent<TMP_Text>();
                if (nivelTxt != null && _nivelRequerido.TryGetValue(nombre, out int reqNivel))
                    nivelTxt.text = "Nivel " + reqNivel + " requerido";
            }
        }

        private GameObject[] GetEdificiosEnContent()
        {
            var content = GameObject.Find("ContentEdificios");
            if (content == null) return System.Array.Empty<GameObject>();
            var list = new List<GameObject>(content.transform.childCount);
            for (int i = 0; i < content.transform.childCount; i++)
                list.Add(content.transform.GetChild(i).gameObject);
            return list.ToArray();
        }

        private IEnumerator CentrarScrollEnMundoActual()
        {
            yield return null;
            yield return null;
            var zonaEdif = GameObject.Find("ZonaEdificios");
            if (zonaEdif == null) yield break;
            var sr = zonaEdif.GetComponent<ScrollRect>();
            if (sr == null) yield break;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(sr.content);
            sr.normalizedPosition = new Vector2(0.5f, 0.5f);
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

        public void GoToHeroes()   => LogProximamente("HeroScene");
        public void GoToGacha()    => LogProximamente("GachaScene");
        public void GoToArena()    => LogProximamente("ArenaScene");
        public void GoToTower()    => LogProximamente("TowerScene");
        public void GoToWorldBoss()=> LogProximamente("WorldBossScene");
        public void GoToClan()     => LogProximamente("ClanScene");
        public void GoToShop()     => LogProximamente("ShopScene");
        public void GoToMissions() => LogProximamente("MissionScene");
        public void GoToDungeon()  => LogProximamente("MazmorraScene");
        public void GoToConjuros() => LogProximamente("ConjuroScene");

        private static void LogProximamente(string sceneName) =>
            Debug.Log($"[MainMenuController] {sceneName} — Próximamente (S23+)");

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
