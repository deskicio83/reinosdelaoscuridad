using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;
using ReinoOscuridad.Systems;
using ReinoOscuridad.Utils;
using ReinoOscuridad.UI.Common;

namespace ReinoOscuridad.UI.HeroScene
{
    public class HeroFilterState
    {
        public string elemento;
        public bool soloFavoritos;
        public bool soloBloqueados;
        public string ordenarPor = "nivel";
    }

    public class HeroSceneController : MonoBehaviour
    {
        public const int SLOT_BASE = 20;
        public const int SLOT_INCREMENTO = 10;
        public const int SLOT_MAXIMO = 200;

        public static readonly string[] ELEMENTOS =
            { "fuego", "agua", "tierra", "naturaleza", "luz", "oscuridad", "rayo", "hielo" };

        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private GameObject _cardTemplate;
        [SerializeField] private Vector2 _cellSpacingExpandida = new Vector2(12f, 12f);
        [SerializeField] private Vector2 _cellSpacingCompacta = new Vector2(6f, 6f);

        [SerializeField] private TMP_Text _capacidadTexto;
        [SerializeField] private Button _btnVistaToggle;
        [SerializeField] private TMP_Text _btnVistaToggleLabel;

        [SerializeField] private Button _btnFiltros;
        [SerializeField] private GameObject _panelFiltros;
        [SerializeField] private RectTransform _filtrosElementoContainer;
        [SerializeField] private GameObject _filtroElementoBtnTemplate;
        [SerializeField] private Button _btnSoloFavoritos;
        [SerializeField] private TMP_Text _btnSoloFavoritosLabel;
        [SerializeField] private Button _btnOrdenar;
        [SerializeField] private TMP_Text _btnOrdenarLabel;

        [SerializeField] private GameObject _popupComprarHuecos;
        [SerializeField] private TMP_Text _popupComprarTexto;
        [SerializeField] private Button _btnConfirmarCompra;
        [SerializeField] private Button _btnCancelarCompra;

        [SerializeField] private GameObject _detailPanel;
        [SerializeField] private Image _detailPortrait;
        [SerializeField] private TMP_Text _detailNombre;
        [SerializeField] private TMP_Text _detailNivelEstrellas;
        [SerializeField] private TMP_Text _detailClaseElemento;
        [SerializeField] private TMP_Text _detailStats;
        [SerializeField] private Button _btnFavorito;
        [SerializeField] private TMP_Text _btnFavoritoLabel;

        [SerializeField] private Button _btnTabInfo;
        [SerializeField] private Button _btnTabHabilidades;
        [SerializeField] private Button _btnTabEquipo;
        [SerializeField] private GameObject _panelInfo;
        [SerializeField] private GameObject _panelHabilidades;
        [SerializeField] private GameObject _panelEquipo;
        [SerializeField] private RectTransform _habilidadesContent;
        [SerializeField] private TMP_Text _equipoContent;

        [SerializeField] private Button _btnVolver;

        private PooledGridView _gridView;
        private PlayerDataSystem _pds;
        private HeroProgressionSystem _hps;
        private EconomySystem _eco;
        private List<PlayerHeroData> _rosterCompleto;
        private List<PlayerHeroData> _rosterFiltrado;
        private readonly HeroFilterState _filtro = new HeroFilterState();
        private string _selectedHeroId;
        private bool _vistaCompacta;

        private static readonly string[] ORDEN_CICLO = { "nivel", "estrellas", "nombre" };

        private void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[HeroSceneController] GameManager.Instance es null — esta Scene se abrió " +
                    "directamente sin pasar por BootScene. Arranca Play desde BootScene y navega Menu > Esbirros.");
                return;
            }

            _pds = GameManager.Instance.GetSystem<PlayerDataSystem>();
            _hps = GameManager.Instance.GetSystem<HeroProgressionSystem>();
            _eco = GameManager.Instance.GetSystem<EconomySystem>();

            if (_btnTabInfo != null) _btnTabInfo.onClick.AddListener(() => ShowTab(0));
            if (_btnTabHabilidades != null) _btnTabHabilidades.onClick.AddListener(() => ShowTab(1));
            if (_btnTabEquipo != null) _btnTabEquipo.onClick.AddListener(() => ShowTab(2));
            if (_btnFavorito != null) _btnFavorito.onClick.AddListener(OnFavoritoToggled);
            if (_btnVolver != null) _btnVolver.onClick.AddListener(() => UIManager.Instance.NavigateBack());

            if (_btnVistaToggle != null) _btnVistaToggle.onClick.AddListener(OnVistaToggle);
            if (_btnFiltros != null) _btnFiltros.onClick.AddListener(OnFiltrosToggle);
            if (_btnSoloFavoritos != null) _btnSoloFavoritos.onClick.AddListener(OnSoloFavoritosToggle);
            if (_btnOrdenar != null) _btnOrdenar.onClick.AddListener(OnOrdenarCiclar);
            if (_btnCancelarCompra != null) _btnCancelarCompra.onClick.AddListener(() => _popupComprarHuecos?.SetActive(false));

            if (_detailPanel != null) _detailPanel.SetActive(false);
            if (_panelFiltros != null) _panelFiltros.SetActive(false);
            if (_popupComprarHuecos != null) _popupComprarHuecos.SetActive(false);

            BuildFiltroElementoButtons();

            var pd = _pds?.GetPlayerData();
            _vistaCompacta = pd?.heroSceneCompactView ?? false;
            RefreshVistaToggleLabel();
            RefreshOrdenarLabel();

            BuildRoster();
        }

        // ── Roster / grid ────────────────────────────────────────────────────

        private void BuildRoster()
        {
            var pd = _pds?.GetPlayerData();
            _rosterCompleto = pd?.heroes ?? new List<PlayerHeroData>();
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            _rosterFiltrado = FilterAndSort(_rosterCompleto, _filtro, GetCatalogData);

            int maxHeroSpaces = _pds?.GetPlayerData()?.maxHeroSpaces ?? SLOT_BASE;
            bool mostrarCompra = maxHeroSpaces < SLOT_MAXIMO;
            int itemCount = _rosterFiltrado.Count + (mostrarCompra ? 1 : 0);

            var cellSize = _vistaCompacta ? new Vector2(80f, 100f) : new Vector2(140f, 170f);
            var spacing = _vistaCompacta ? _cellSpacingCompacta : _cellSpacingExpandida;
            int columns = _vistaCompacta ? 6 : 4;

            _gridView = _content.GetComponent<PooledGridView>();
            if (_gridView == null) _gridView = _content.gameObject.AddComponent<PooledGridView>();

            _gridView.Init(_cardTemplate, _content, _viewport, _scrollRect, columns, cellSize, spacing,
                BindCard, itemCount);

            RefreshCapacidadTexto(_rosterCompleto.Count, maxHeroSpaces);
        }

        public static List<PlayerHeroData> SortRoster(List<PlayerHeroData> roster)
        {
            return roster
                .OrderByDescending(h => h.favorite)
                .ThenByDescending(h => h.level)
                .ThenBy(h => h.heroId)
                .ToList();
        }

        public static List<PlayerHeroData> FilterAndSort(List<PlayerHeroData> roster, HeroFilterState filter,
            Func<string, HeroData> catalogLookup)
        {
            IEnumerable<PlayerHeroData> query = roster;

            if (filter.soloFavoritos) query = query.Where(h => h.favorite);
            if (filter.soloBloqueados) query = query.Where(h => h.locked);
            if (!string.IsNullOrEmpty(filter.elemento) && catalogLookup != null)
                query = query.Where(h => string.Equals(catalogLookup(h.heroId)?.element, filter.elemento,
                    StringComparison.OrdinalIgnoreCase));

            switch (filter.ordenarPor)
            {
                case "estrellas":
                    query = query.OrderByDescending(h => h.favorite).ThenByDescending(h => h.stars).ThenBy(h => h.heroId);
                    break;
                case "nombre":
                    query = query.OrderByDescending(h => h.favorite)
                        .ThenBy(h => catalogLookup?.Invoke(h.heroId)?.displayName_es ?? h.heroId);
                    break;
                default:
                    query = query.OrderByDescending(h => h.favorite).ThenByDescending(h => h.level).ThenBy(h => h.heroId);
                    break;
            }

            return query.ToList();
        }

        public static (int oro, int caosifera) GetSlotExpansionCost(int currentMax)
        {
            int n = Mathf.Max(1, ((currentMax - SLOT_BASE) / SLOT_INCREMENTO) + 1);
            return (1000 * n * n, 20 * n);
        }

        private HeroData GetCatalogData(string heroId) => _hps?.GetHeroData(heroId);

        private void BindCard(GameObject cellGO, int index)
        {
            var view = cellGO.GetComponent<HeroCardView>();
            if (view == null) return;

            if (index >= _rosterFiltrado.Count)
            {
                view.BindBuySlot("+", OnBuySlotClicked);
                return;
            }

            var playerHero = _rosterFiltrado[index];
            var catalogData = GetCatalogData(playerHero.heroId);
            string nombre = catalogData?.displayName_es ?? playerHero.heroId;
            var portrait = PlaceholderAssets.GetHeroPortrait(catalogData?.element);

            view.Bind(playerHero.heroId, nombre, playerHero.level, portrait, playerHero.favorite, OnCardClicked);
        }

        private void OnCardClicked(string heroId)
        {
            _selectedHeroId = heroId;
            ShowDetail(heroId);
        }

        // ── Vista compacta / expandida ───────────────────────────────────────

        private void OnVistaToggle()
        {
            _vistaCompacta = !_vistaCompacta;

            var pd = _pds?.GetPlayerData();
            if (pd != null)
            {
                pd.heroSceneCompactView = _vistaCompacta;
                _pds.MarkDirty();
            }

            RefreshVistaToggleLabel();
            ApplyFilters();
        }

        private void RefreshVistaToggleLabel()
        {
            if (_btnVistaToggleLabel != null)
                _btnVistaToggleLabel.text = _vistaCompacta ? "Vista grande" : "Vista compacta";
        }

        // ── Filtros ──────────────────────────────────────────────────────────

        private void BuildFiltroElementoButtons()
        {
            if (_filtrosElementoContainer == null || _filtroElementoBtnTemplate == null) return;

            foreach (var elemento in ELEMENTOS)
            {
                var go = Instantiate(_filtroElementoBtnTemplate, _filtrosElementoContainer);
                go.SetActive(true);
                var label = go.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = elemento;

                var btn = go.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => OnFiltroElementoClicked(elemento));
            }
        }

        private void OnFiltroElementoClicked(string elemento)
        {
            _filtro.elemento = string.Equals(_filtro.elemento, elemento, StringComparison.OrdinalIgnoreCase)
                ? null
                : elemento;
            ApplyFilters();
        }

        private void OnFiltrosToggle()
        {
            if (_panelFiltros != null) _panelFiltros.SetActive(!_panelFiltros.activeSelf);
        }

        private void OnSoloFavoritosToggle()
        {
            _filtro.soloFavoritos = !_filtro.soloFavoritos;
            if (_btnSoloFavoritosLabel != null)
                _btnSoloFavoritosLabel.text = _filtro.soloFavoritos ? "Favoritos: SI" : "Favoritos: NO";
            ApplyFilters();
        }

        private void OnOrdenarCiclar()
        {
            int idx = Array.IndexOf(ORDEN_CICLO, _filtro.ordenarPor);
            _filtro.ordenarPor = ORDEN_CICLO[(idx + 1) % ORDEN_CICLO.Length];
            RefreshOrdenarLabel();
            ApplyFilters();
        }

        private void RefreshOrdenarLabel()
        {
            if (_btnOrdenarLabel != null) _btnOrdenarLabel.text = "Orden: " + _filtro.ordenarPor;
        }

        private void RefreshCapacidadTexto(int actuales, int maximo)
        {
            if (_capacidadTexto != null) _capacidadTexto.text = $"Esbirros: {actuales}/{maximo}";
        }

        // ── Compra de huecos ─────────────────────────────────────────────────

        private void OnBuySlotClicked()
        {
            var pd = _pds?.GetPlayerData();
            if (pd == null) return;

            var (oro, caosifera) = GetSlotExpansionCost(pd.maxHeroSpaces);

            if (_popupComprarTexto != null)
                _popupComprarTexto.text = $"Comprar {SLOT_INCREMENTO} huecos\n{oro} oro negro + {caosifera} caosífera";

            if (_btnConfirmarCompra != null)
            {
                _btnConfirmarCompra.onClick.RemoveAllListeners();
                _btnConfirmarCompra.onClick.AddListener(() => ConfirmarCompraHuecos(oro, caosifera));
            }

            if (_popupComprarHuecos != null) _popupComprarHuecos.SetActive(true);
        }

        private void ConfirmarCompraHuecos(int costoOro, int costoCaosifera)
        {
            var pd = _pds?.GetPlayerData();
            if (pd == null || _eco == null) return;

            if (pd.maxHeroSpaces >= SLOT_MAXIMO) return;

            if (!_eco.ConsumeGold(costoOro)) return;
            if (!_eco.ConsumeCaosifera(costoCaosifera))
            {
                _eco.AddGold(costoOro);
                return;
            }

            pd.maxHeroSpaces = Mathf.Min(SLOT_MAXIMO, pd.maxHeroSpaces + SLOT_INCREMENTO);
            _pds.MarkDirty();

            if (_popupComprarHuecos != null) _popupComprarHuecos.SetActive(false);
            ApplyFilters();
        }

        // ── Detalle ──────────────────────────────────────────────────────────

        private void ShowDetail(string heroId)
        {
            var playerHero = _rosterCompleto.FirstOrDefault(h => h.heroId == heroId);
            if (playerHero == null) return;

            var catalogData = GetCatalogData(heroId);
            var instance = GearSystem.Instance?.BuildCombatInstance(heroId);

            if (_detailPanel != null) _detailPanel.SetActive(true);

            if (_detailPortrait != null) _detailPortrait.sprite = PlaceholderAssets.GetHeroPortrait(catalogData?.element);
            if (_detailNombre != null) _detailNombre.text = catalogData?.displayName_es ?? heroId;
            if (_detailNivelEstrellas != null) _detailNivelEstrellas.text = $"Nv. {playerHero.level}  ·  {playerHero.stars}*";
            if (_detailClaseElemento != null) _detailClaseElemento.text = $"{catalogData?.classStandard}  ·  {catalogData?.element}";

            if (_detailStats != null && instance != null)
                _detailStats.text =
                    $"HP: {instance.hpMax}\nATK: {instance.atk}\nDEF: {instance.def}\nSPD: {instance.spd}\n" +
                    $"CRIT: {instance.crit}%  CRIT DMG: {instance.critDmg}%\nACC: {instance.acc}  RES: {instance.res}";

            RefreshFavoritoButton(playerHero.favorite);
            BuildHabilidades(catalogData);
            BuildEquipo(playerHero);

            ShowTab(0);
        }

        private void RefreshFavoritoButton(bool favorito)
        {
            if (_btnFavoritoLabel != null)
                _btnFavoritoLabel.text = favorito ? "* Favorito" : "Marcar favorito";
        }

        private void OnFavoritoToggled()
        {
            if (string.IsNullOrEmpty(_selectedHeroId)) return;

            var playerHero = _rosterCompleto.FirstOrDefault(h => h.heroId == _selectedHeroId);
            if (playerHero == null) return;

            playerHero.favorite = !playerHero.favorite;
            _pds?.MarkDirty();

            RefreshFavoritoButton(playerHero.favorite);
            ApplyFilters();
        }

        private void BuildHabilidades(HeroData catalogData)
        {
            if (_habilidadesContent == null) return;

            foreach (Transform child in _habilidadesContent)
            {
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            if (catalogData?.skills == null) return;

            foreach (var skill in catalogData.skills)
            {
                if (skill.type == "passive") continue;

                var entryGO = new GameObject("Skill_" + skill.skillId, typeof(RectTransform));
                entryGO.transform.SetParent(_habilidadesContent, false);
                entryGO.AddComponent<LayoutElement>().preferredHeight = 60f;

                var txtGO = new GameObject("Texto", typeof(RectTransform));
                txtGO.transform.SetParent(entryGO.transform, false);
                var rt = txtGO.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var txt = txtGO.AddComponent<TextMeshProUGUI>();
                txt.text = $"{skill.name_es} ({skill.type}) — CD: {skill.cooldown}\n{skill.description_es}";
                txt.fontSize = 12f;
                txt.color = Color.white;
            }
        }

        private void BuildEquipo(PlayerHeroData playerHero)
        {
            if (_equipoContent == null) return;

            var gearSystem = GearSystem.Instance;
            string[] slots = { "weapon", "helmet", "armor", "boots", "ring", "necklace" };
            var lines = new List<string>();

            foreach (var slot in slots)
            {
                GearInstance equipped = null;
                if (gearSystem != null && playerHero.equipment != null)
                {
                    foreach (var g in playerHero.equipment)
                    {
                        var instance = gearSystem.GetGear(g.instanceId);
                        if (instance != null && instance.slot == slot) { equipped = instance; break; }
                    }
                }

                lines.Add(equipped != null
                    ? $"{slot}: {equipped.rareza} · {equipped.mainStat} {equipped.mainStatValue}"
                    : $"{slot}: vacío");
            }

            _equipoContent.text = string.Join("\n", lines);
        }

        private void ShowTab(int index)
        {
            if (_panelInfo != null) _panelInfo.SetActive(index == 0);
            if (_panelHabilidades != null) _panelHabilidades.SetActive(index == 1);
            if (_panelEquipo != null) _panelEquipo.SetActive(index == 2);
        }
    }
}
