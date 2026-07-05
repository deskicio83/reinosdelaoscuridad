using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using TMPro;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;
using ReinoOscuridad.Systems;
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

        public const int COLUMNS = 3;
        public const float CARD_ASPECT = 1.2f;
        public const int MAX_ESTRELLAS = 6;

        public static readonly string[] ELEMENTOS =
            { "fuego", "agua", "tierra", "naturaleza", "luz", "oscuridad", "rayo", "hielo" };

        private static readonly string[] GEAR_SLOTS = { "weapon", "helmet", "armor", "boots", "ring", "necklace" };

        private const string ART_HEROSCENE = "Assets/Addressables/Art/HeroScene/";
        private const string ART_SKILLICON = "Assets/Addressables/SkillIcon/";

        private static readonly Dictionary<string, string> ELEMENTO_ICON_PATHS = new Dictionary<string, string>
        {
            { "fuego", ART_HEROSCENE + "Elemento/Fuego.png" },
            { "agua", ART_HEROSCENE + "Elemento/Agua.png" },
            { "naturaleza", ART_HEROSCENE + "Elemento/Naturaleza.png" },
            { "luz", ART_HEROSCENE + "Elemento/Luz.png" },
            { "oscuridad", ART_HEROSCENE + "Elemento/Oscuridad.png" },
        };

        private const string ICON_TAB_INFO = ART_HEROSCENE + "TabMenuBar/IconInfo.png";
        private const string ICON_TAB_HABILIDADES = ART_HEROSCENE + "TabMenuBar/IconHabilidad.png";
        private const string ICON_TAB_EQUIPO = ART_HEROSCENE + "TabMenuBar/IconEquipo.png";

        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private GameObject _cardTemplate;
        [SerializeField] private Vector2 _cellSpacing = new Vector2(10f, 10f);

        [SerializeField] private TMP_Text _capacidadTexto;

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

        // ── Zona media (retrato + datos rápidos) ─────────────────────────────
        [SerializeField] private GameObject _detailPanel;
        [SerializeField] private Image _detailPortrait;
        [SerializeField] private TMP_Text _detailNombre;
        [SerializeField] private TMP_Text _detailNivel;
        [SerializeField] private RectTransform _estrellasContainer;
        [SerializeField] private Image _iconElemento;
        [SerializeField] private TMP_Text _detailClaseElemento;
        [SerializeField] private Button _btnFavorito;
        [SerializeField] private TMP_Text _btnFavoritoLabel;
        [SerializeField] private Button _btnBloquear;
        [SerializeField] private TMP_Text _btnBloquearLabel;

        // ── Zona de navegación (tabs + contenido) ────────────────────────────
        [SerializeField] private Button _btnTabInfo;
        [SerializeField] private Button _btnTabHabilidades;
        [SerializeField] private Button _btnTabEquipo;
        [SerializeField] private Button _btnTabMaestrias;
        [SerializeField] private Button _btnTabMiscelaneo;
        [SerializeField] private Image _iconTabInfo;
        [SerializeField] private Image _iconTabHabilidades;
        [SerializeField] private Image _iconTabEquipo;
        [SerializeField] private GameObject _panelInfo;
        [SerializeField] private GameObject _panelHabilidades;
        [SerializeField] private GameObject _panelEquipo;
        [SerializeField] private GameObject _panelMaestrias;
        [SerializeField] private GameObject _panelMiscelaneo;
        [SerializeField] private RectTransform _habilidadesContent;
        [SerializeField] private RectTransform _equipoSlotsContent;
        [SerializeField] private Button _btnEliminar;
        [SerializeField] private TMP_Text _detailStats;

        [SerializeField] private Button _btnVolver;

        private PooledGridView _gridView;
        private PlayerDataSystem _pds;
        private HeroProgressionSystem _hps;
        private EconomySystem _eco;
        private List<PlayerHeroData> _rosterCompleto;
        private List<PlayerHeroData> _rosterFiltrado;
        private readonly HeroFilterState _filtro = new HeroFilterState();
        private string _selectedHeroId;
        private int _detailLoadToken;

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
            if (_btnTabMaestrias != null) _btnTabMaestrias.onClick.AddListener(() => ShowTab(3));
            if (_btnTabMiscelaneo != null) _btnTabMiscelaneo.onClick.AddListener(() => ShowTab(4));
            if (_btnFavorito != null) _btnFavorito.onClick.AddListener(OnFavoritoToggled);
            if (_btnBloquear != null) _btnBloquear.onClick.AddListener(OnBloquearToggled);
            if (_btnEliminar != null) _btnEliminar.onClick.AddListener(OnEliminarClicked);
            if (_btnVolver != null) _btnVolver.onClick.AddListener(() => UIManager.Instance.NavigateBack());

            if (_btnFiltros != null) _btnFiltros.onClick.AddListener(OnFiltrosToggle);
            if (_btnSoloFavoritos != null) _btnSoloFavoritos.onClick.AddListener(OnSoloFavoritosToggle);
            if (_btnOrdenar != null) _btnOrdenar.onClick.AddListener(OnOrdenarCiclar);
            if (_btnCancelarCompra != null) _btnCancelarCompra.onClick.AddListener(() => _popupComprarHuecos?.SetActive(false));

            if (_detailPanel != null) _detailPanel.SetActive(false);
            if (_panelFiltros != null) _panelFiltros.SetActive(false);
            if (_popupComprarHuecos != null) _popupComprarHuecos.SetActive(false);

            BuildFiltroElementoButtons();
            LoadIconAsync(ICON_TAB_INFO, _iconTabInfo);
            LoadIconAsync(ICON_TAB_HABILIDADES, _iconTabHabilidades);
            LoadIconAsync(ICON_TAB_EQUIPO, _iconTabEquipo);

            RefreshOrdenarLabel();

            var pd = _pds?.GetPlayerData();
            _rosterCompleto = pd?.heroes ?? new List<PlayerHeroData>();

            StartCoroutine(InitializeGridAfterLayout());
        }

        private IEnumerator InitializeGridAfterLayout()
        {
            yield return null;
            yield return null;
            ApplyFilters();
        }

        // ── Roster / grid ────────────────────────────────────────────────────

        private void ApplyFilters()
        {
            _rosterFiltrado = FilterAndSort(_rosterCompleto, _filtro, GetCatalogData);

            int maxHeroSpaces = _pds?.GetPlayerData()?.maxHeroSpaces ?? SLOT_BASE;
            bool mostrarCompra = maxHeroSpaces < SLOT_MAXIMO;
            int itemCount = _rosterFiltrado.Count + (mostrarCompra ? 1 : 0);

            var cellSize = ComputeCellSize(COLUMNS, _cellSpacing);

            _gridView = _content.GetComponent<PooledGridView>();
            if (_gridView == null) _gridView = _content.gameObject.AddComponent<PooledGridView>();

            _gridView.Init(_cardTemplate, _content, _viewport, _scrollRect, COLUMNS, cellSize, _cellSpacing,
                BindCard, itemCount);

            RefreshCapacidadTexto(_rosterCompleto.Count, maxHeroSpaces);
        }

        private Vector2 ComputeCellSize(int columns, Vector2 spacing)
        {
            float viewportWidth = _viewport != null ? _viewport.rect.width : 0f;
            if (viewportWidth <= 0f) viewportWidth = 260f;

            float width = (viewportWidth - spacing.x * (columns - 1)) / columns;
            width = Mathf.Max(40f, width);
            return new Vector2(width, width * CARD_ASPECT);
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
            string portraitAddress = playerHero.awaken
                ? catalogData?.portraitAddressableAwaken
                : catalogData?.portraitAddressable;

            view.Bind(playerHero.heroId, nombre, playerHero.level, portraitAddress, playerHero.favorite,
                playerHero.locked, OnCardClicked);
        }

        private void LoadIconAsync(string address, Image target)
        {
            if (target == null || string.IsNullOrEmpty(address)) return;
            Addressables.LoadAssetAsync<Sprite>(address).Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded && target != null)
                    target.sprite = handle.Result;
            };
        }

        private void OnCardClicked(string heroId)
        {
            _selectedHeroId = heroId;
            ShowDetail(heroId);
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

        // ── Popup de confirmación genérico ───────────────────────────────────

        private void AbrirPopupConfirmacion(string mensaje, Action onConfirm)
        {
            if (_popupComprarTexto != null) _popupComprarTexto.text = mensaje;

            if (_btnConfirmarCompra != null)
            {
                _btnConfirmarCompra.onClick.RemoveAllListeners();
                _btnConfirmarCompra.onClick.AddListener(() =>
                {
                    onConfirm?.Invoke();
                    if (_popupComprarHuecos != null) _popupComprarHuecos.SetActive(false);
                });
            }

            if (_popupComprarHuecos != null) _popupComprarHuecos.SetActive(true);
        }

        // ── Compra de huecos ─────────────────────────────────────────────────

        private void OnBuySlotClicked()
        {
            var pd = _pds?.GetPlayerData();
            if (pd == null) return;

            var (oro, caosifera) = GetSlotExpansionCost(pd.maxHeroSpaces);
            AbrirPopupConfirmacion(
                $"Comprar {SLOT_INCREMENTO} huecos\n{oro} oro negro + {caosifera} caosífera",
                () => ConfirmarCompraHuecos(oro, caosifera));
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

            _detailLoadToken++;
            int myToken = _detailLoadToken;
            if (_detailPortrait != null) _detailPortrait.sprite = null;

            string fullAddress = playerHero.awaken ? catalogData?.fullAddressableAwaken : catalogData?.fullAddressable;
            if (_detailPortrait != null && !string.IsNullOrEmpty(fullAddress))
            {
                Addressables.LoadAssetAsync<Sprite>(fullAddress).Completed += handle =>
                {
                    if (myToken != _detailLoadToken) return;
                    if (handle.Status == AsyncOperationStatus.Succeeded && _detailPortrait != null)
                        _detailPortrait.sprite = handle.Result;
                };
            }

            if (_iconElemento != null)
            {
                _iconElemento.gameObject.SetActive(false);
                string elemento = catalogData?.element?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(elemento) && ELEMENTO_ICON_PATHS.TryGetValue(elemento, out var iconPath))
                {
                    _iconElemento.gameObject.SetActive(true);
                    LoadIconAsync(iconPath, _iconElemento);
                }
            }

            if (_detailNombre != null) _detailNombre.text = catalogData?.displayName_es ?? heroId;
            if (_detailNivel != null) _detailNivel.text = $"Nv. {playerHero.level}";
            BuildEstrellas(playerHero.stars);
            if (_detailClaseElemento != null) _detailClaseElemento.text = $"{catalogData?.classStandard}  ·  {catalogData?.element}";

            if (_detailStats != null && instance != null)
                _detailStats.text =
                    $"HP: {instance.hpMax}\nATK: {instance.atk}\nDEF: {instance.def}\nSPD: {instance.spd}\n" +
                    $"CRIT: {instance.crit}%  CRIT DMG: {instance.critDmg}%\nACC: {instance.acc}  RES: {instance.res}";

            RefreshFavoritoButton(playerHero.favorite);
            RefreshBloquearButton(playerHero.locked);
            BuildHabilidades(playerHero, catalogData);
            BuildEquipo(playerHero);

            ShowTab(0);
        }

        private void BuildEstrellas(int stars)
        {
            if (_estrellasContainer == null) return;

            foreach (Transform child in _estrellasContainer)
            {
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            int total = Mathf.Clamp(stars, 0, MAX_ESTRELLAS);
            for (int i = 0; i < total; i++)
            {
                var go = new GameObject("Estrella" + i, typeof(RectTransform));
                go.transform.SetParent(_estrellasContainer, false);
                go.AddComponent<LayoutElement>().preferredWidth = 12f;
                var img = go.AddComponent<Image>();
                img.color = new Color(0.96f, 0.62f, 0.04f);
            }
        }

        private void RefreshFavoritoButton(bool favorito)
        {
            if (_btnFavoritoLabel != null)
                _btnFavoritoLabel.text = favorito ? "* Favorito" : "Favorito";
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

        private void RefreshBloquearButton(bool bloqueado)
        {
            if (_btnBloquearLabel != null)
                _btnBloquearLabel.text = bloqueado ? "Bloqueado" : "Bloquear";
        }

        private void OnBloquearToggled()
        {
            if (string.IsNullOrEmpty(_selectedHeroId)) return;

            var playerHero = _rosterCompleto.FirstOrDefault(h => h.heroId == _selectedHeroId);
            if (playerHero == null) return;

            playerHero.locked = !playerHero.locked;
            _pds?.MarkDirty();

            RefreshBloquearButton(playerHero.locked);
            ApplyFilters();
        }

        private void OnEliminarClicked()
        {
            if (string.IsNullOrEmpty(_selectedHeroId)) return;

            var playerHero = _rosterCompleto.FirstOrDefault(h => h.heroId == _selectedHeroId);
            if (playerHero == null) return;

            if (playerHero.locked)
            {
                AbrirPopupConfirmacion("Este esbirro está bloqueado.\nDesbloquéalo antes de eliminarlo.", null);
                return;
            }

            var catalogData = GetCatalogData(playerHero.heroId);
            string nombre = catalogData?.displayName_es ?? playerHero.heroId;

            AbrirPopupConfirmacion($"¿Eliminar a {nombre} permanentemente?\nSe perderá su progreso y equipo.",
                () => ConfirmarEliminarHeroe(playerHero));
        }

        private void ConfirmarEliminarHeroe(PlayerHeroData playerHero)
        {
            _rosterCompleto.Remove(playerHero);
            _pds?.MarkDirty();

            _selectedHeroId = null;
            if (_detailPanel != null) _detailPanel.SetActive(false);

            ApplyFilters();
        }

        // ── Habilidades ──────────────────────────────────────────────────────

        private void BuildHabilidades(PlayerHeroData playerHero, HeroData catalogData)
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

                int skillLevel = playerHero.skills?
                    .FirstOrDefault(s => string.Equals(s.skillId, skill.skillId, StringComparison.OrdinalIgnoreCase))
                    ?.level ?? 0;

                var entryGO = new GameObject("Skill_" + skill.skillId, typeof(RectTransform));
                entryGO.transform.SetParent(_habilidadesContent, false);
                entryGO.AddComponent<LayoutElement>().preferredHeight = 130f;

                var iconGO = new GameObject("Icono", typeof(RectTransform));
                iconGO.transform.SetParent(entryGO.transform, false);
                var iconRT = iconGO.GetComponent<RectTransform>();
                iconRT.anchorMin = new Vector2(0f, 0.5f);
                iconRT.anchorMax = new Vector2(0f, 0.5f);
                iconRT.pivot = new Vector2(0f, 0.5f);
                iconRT.sizeDelta = new Vector2(84f, 84f);
                iconRT.anchoredPosition = new Vector2(4f, 0f);
                var iconImg = iconGO.AddComponent<Image>();
                iconImg.color = Color.white;
                LoadIconAsync(ART_SKILLICON + $"{playerHero.heroId}_{skill.type}.png", iconImg);

                var txtGO = new GameObject("Texto", typeof(RectTransform));
                txtGO.transform.SetParent(entryGO.transform, false);
                var rt = txtGO.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(96f, 4f);
                rt.offsetMax = new Vector2(-4f, -4f);

                var txt = txtGO.AddComponent<TextMeshProUGUI>();
                txt.fontSize = 11f;
                txt.color = Color.white;
                txt.textWrappingMode = TextWrappingModes.Normal;
                txt.text = BuildHabilidadRichText(skill, skillLevel);
            }
        }

        private static string BuildHabilidadRichText(HeroSkillDef skill, int skillLevel)
        {
            var sb = new StringBuilder();
            sb.Append($"<b>{skill.name_es}</b>  ({skill.type})  CD:{skill.cooldown}\n");
            sb.Append(skill.description_es).Append('\n');

            if (skill.levelUp != null)
            {
                foreach (var lu in skill.levelUp)
                {
                    bool desbloqueado = skillLevel >= lu.lvl;
                    string linea = $"Nv.{lu.lvl}: {lu.change} {lu.value}";
                    sb.Append(desbloqueado
                        ? $"<b>{linea}</b>\n"
                        : $"<color=#666666>{linea}</color>\n");
                }
            }

            return sb.ToString();
        }

        // ── Equipo ───────────────────────────────────────────────────────────

        private void BuildEquipo(PlayerHeroData playerHero)
        {
            if (_equipoSlotsContent == null) return;

            foreach (Transform child in _equipoSlotsContent)
            {
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            var gearSystem = GearSystem.Instance;

            for (int i = 0; i < GEAR_SLOTS.Length; i++)
            {
                string slot = GEAR_SLOTS[i];
                int slotIndex = i;
                GearInstance equipped = null;

                if (gearSystem != null && playerHero.equipment != null)
                {
                    foreach (var g in playerHero.equipment)
                    {
                        var instance = gearSystem.GetGear(g.instanceId);
                        if (instance != null && instance.slot == slot) { equipped = instance; break; }
                    }
                }

                var rowGO = new GameObject("Slot_" + slot, typeof(RectTransform));
                rowGO.transform.SetParent(_equipoSlotsContent, false);
                rowGO.AddComponent<LayoutElement>().preferredHeight = 46f;
                var rowImg = rowGO.AddComponent<Image>();
                rowImg.color = new Color(1f, 1f, 1f, 0.04f);
                var rowBtn = rowGO.AddComponent<Button>();
                rowBtn.targetGraphic = rowImg;
                rowBtn.onClick.AddListener(() => OnSlotEquipoClicked(playerHero, slot, slotIndex));

                var txtGO = new GameObject("Texto", typeof(RectTransform));
                txtGO.transform.SetParent(rowGO.transform, false);
                var rt = txtGO.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(8f, 0f);
                rt.offsetMax = new Vector2(-8f, 0f);

                var txt = txtGO.AddComponent<TextMeshProUGUI>();
                txt.fontSize = 11f;
                txt.color = Color.white;
                txt.text = equipped != null
                    ? $"{slot}: {equipped.rareza} · {equipped.mainStat} {equipped.mainStatValue}  (toca para cambiar)"
                    : $"{slot}: vacío  (toca para equipar)";
            }
        }

        private void OnSlotEquipoClicked(PlayerHeroData playerHero, string slot, int slotIndex)
        {
            var pd = _pds?.GetPlayerData();
            var gearSystem = GearSystem.Instance;
            if (pd == null || gearSystem == null) return;

            var disponibles = (pd.gearInventory ?? new List<PlayerGearInstance>())
                .Select(g => gearSystem.GetGear(g.instanceId))
                .Where(g => g != null && g.slot == slot)
                .OrderByDescending(g => g.mainStatValue)
                .ToList();

            if (disponibles.Count == 0)
            {
                AbrirPopupConfirmacion($"No tienes piezas disponibles para el slot '{slot}' en el inventario.", null);
                return;
            }

            var mejor = disponibles[0];
            AbrirPopupConfirmacion(
                $"Equipar {mejor.rareza} · {mejor.mainStat} {mejor.mainStatValue}\nen el slot {slot}?\n" +
                $"(se equipa automáticamente la mejor pieza disponible)",
                () =>
                {
                    gearSystem.EquipGear(mejor.instanceId, playerHero.heroId, slotIndex);
                    BuildEquipo(playerHero);
                });
        }

        private void ShowTab(int index)
        {
            if (_panelInfo != null) _panelInfo.SetActive(index == 0);
            if (_panelHabilidades != null) _panelHabilidades.SetActive(index == 1);
            if (_panelEquipo != null) _panelEquipo.SetActive(index == 2);
            if (_panelMaestrias != null) _panelMaestrias.SetActive(index == 3);
            if (_panelMiscelaneo != null) _panelMiscelaneo.SetActive(index == 4);
        }
    }
}
