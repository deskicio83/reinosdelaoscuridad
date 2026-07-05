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
    public class HeroSceneController : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private GameObject _cardTemplate;
        [SerializeField] private int _columns = 4;
        [SerializeField] private Vector2 _cellSize = new Vector2(140f, 170f);
        [SerializeField] private Vector2 _cellSpacing = new Vector2(12f, 12f);

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
        private List<PlayerHeroData> _roster;
        private string _selectedHeroId;

        private void Start()
        {
            _pds = GameManager.Instance?.GetSystem<PlayerDataSystem>();
            _hps = GameManager.Instance?.GetSystem<HeroProgressionSystem>();

            if (_btnTabInfo != null) _btnTabInfo.onClick.AddListener(() => ShowTab(0));
            if (_btnTabHabilidades != null) _btnTabHabilidades.onClick.AddListener(() => ShowTab(1));
            if (_btnTabEquipo != null) _btnTabEquipo.onClick.AddListener(() => ShowTab(2));
            if (_btnFavorito != null) _btnFavorito.onClick.AddListener(OnFavoritoToggled);
            if (_btnVolver != null) _btnVolver.onClick.AddListener(() => UIManager.Instance.NavigateBack());

            if (_detailPanel != null) _detailPanel.SetActive(false);

            BuildRoster();
        }

        private void BuildRoster()
        {
            var pd = _pds?.GetPlayerData();
            _roster = SortRoster(pd?.heroes ?? new List<PlayerHeroData>());

            _gridView = _content.GetComponent<PooledGridView>();
            if (_gridView == null) _gridView = _content.gameObject.AddComponent<PooledGridView>();

            _gridView.Init(_cardTemplate, _content, _viewport, _scrollRect, _columns, _cellSize, _cellSpacing,
                BindCard, _roster.Count);
        }

        public static List<PlayerHeroData> SortRoster(List<PlayerHeroData> roster)
        {
            return roster
                .OrderByDescending(h => h.favorite)
                .ThenByDescending(h => h.level)
                .ThenBy(h => h.heroId)
                .ToList();
        }

        private void BindCard(GameObject cellGO, int index)
        {
            var view = cellGO.GetComponent<HeroCardView>();
            if (view == null || index >= _roster.Count) return;

            var playerHero = _roster[index];
            var catalogData = _hps?.GetHeroData(playerHero.heroId);
            string nombre = catalogData?.displayName_es ?? playerHero.heroId;
            var portrait = PlaceholderAssets.GetHeroPortrait(catalogData?.element);

            view.Bind(playerHero.heroId, nombre, playerHero.level, portrait, playerHero.favorite, OnCardClicked);
        }

        private void OnCardClicked(string heroId)
        {
            _selectedHeroId = heroId;
            ShowDetail(heroId);
        }

        private void ShowDetail(string heroId)
        {
            var playerHero = _roster.FirstOrDefault(h => h.heroId == heroId);
            if (playerHero == null) return;

            var catalogData = _hps?.GetHeroData(heroId);
            var instance = GearSystem.Instance?.BuildCombatInstance(heroId);

            if (_detailPanel != null) _detailPanel.SetActive(true);

            if (_detailPortrait != null) _detailPortrait.sprite = PlaceholderAssets.GetHeroPortrait(catalogData?.element);
            if (_detailNombre != null) _detailNombre.text = catalogData?.displayName_es ?? heroId;
            if (_detailNivelEstrellas != null) _detailNivelEstrellas.text = $"Nv. {playerHero.level}  ·  {playerHero.stars}★";
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
                _btnFavoritoLabel.text = favorito ? "★ Favorito" : "☆ Marcar favorito";
        }

        private void OnFavoritoToggled()
        {
            if (string.IsNullOrEmpty(_selectedHeroId)) return;

            var playerHero = _roster.FirstOrDefault(h => h.heroId == _selectedHeroId);
            if (playerHero == null) return;

            playerHero.favorite = !playerHero.favorite;
            _pds?.MarkDirty();

            RefreshFavoritoButton(playerHero.favorite);
            _gridView.SetItemCount(_roster.Count);
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
