using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ReinoOscuridad.Data;
using ReinoOscuridad.Systems;

namespace ReinoOscuridad.UI.Campaign
{
    /// Overlay de resultado post-combate.
    /// Se instancia desde CampaignSceneController y recibe callbacks locales.
    /// NO navega — solo comunica la decisión al controller.
    public class RewardPanel : MonoBehaviour
    {
        // ── Refs asignadas por SetupRewardPanelPrefab ──────────────────────────

        [SerializeField] private TMP_Text      _txtResultado;
        [SerializeField] private Image[]       _imgEstrellas;
        [SerializeField] private TMP_Text      _txtXPJugador;
        [SerializeField] private RectTransform _barraXPFill;
        [SerializeField] private Transform     _contenedorHeroes;
        [SerializeField] private Transform     _contenedorDrops;
        [SerializeField] private Button        _btnRepetir;
        [SerializeField] private Button        _btnVolver;
        [SerializeField] private Button        _btnSiguiente;

        // ── Callbacks ──────────────────────────────────────────────────────────

        private Action _onRepetir;
        private Action _onSiguiente;
        private Action _onVolver;

        // ── Inicialización ────────────────────────────────────────────────────

        public void Show(CombatResult result, HeroInstance[] team,
                         Action onRepetir, Action onSiguiente, Action onVolver)
        {
            _onRepetir   = onRepetir;
            _onSiguiente = onSiguiente;
            _onVolver    = onVolver;

            PopulateHeader(result, team);
            PopulateHeroRows(result, team);
            PopulateDrops(result);

            bool victoria = result?.victoria ?? false;

            _btnRepetir?.gameObject.SetActive(true);
            _btnVolver? .gameObject.SetActive(true);
            if (_btnSiguiente != null) _btnSiguiente.gameObject.SetActive(victoria);

            _btnRepetir?  .onClick.RemoveAllListeners();
            _btnVolver?   .onClick.RemoveAllListeners();
            _btnSiguiente?.onClick.RemoveAllListeners();

            _btnRepetir?.onClick.AddListener(() =>
            {
                Destroy(gameObject);
                _onRepetir?.Invoke();
            });
            _btnVolver?.onClick.AddListener(() =>
            {
                Destroy(gameObject);
                _onVolver?.Invoke();
            });
            if (victoria)
                _btnSiguiente?.onClick.AddListener(() =>
                {
                    Destroy(gameObject);
                    _onSiguiente?.Invoke();
                });

            // Reposicionar botones para derrota (2 botones centrados)
            if (!victoria)
            {
                SetBtnAnchors(_btnRepetir, new Vector2(0.05f, 0.04f), new Vector2(0.48f, 0.18f));
                SetBtnAnchors(_btnVolver,  new Vector2(0.52f, 0.04f), new Vector2(0.95f, 0.18f));
            }

            gameObject.SetActive(true);
        }

        // ── Header ────────────────────────────────────────────────────────────

        private void PopulateHeader(CombatResult result, HeroInstance[] team)
        {
            bool victoria = result?.victoria ?? false;

            if (_txtResultado != null)
            {
                _txtResultado.text  = victoria ? "VICTORIA" : "DERROTA";
                _txtResultado.color = victoria
                    ? new Color(0.4f, 1f, 0.4f)
                    : new Color(1f, 0.3f, 0.3f);
            }

            if (_imgEstrellas != null)
            {
                int stars = GetStarCount(result, team);
                for (int i = 0; i < _imgEstrellas.Length; i++)
                {
                    if (_imgEstrellas[i] == null) continue;
                    _imgEstrellas[i].color = i < stars
                        ? new Color(0.98f, 0.80f, 0.08f)
                        : victoria ? new Color(0.25f, 0.25f, 0.28f) : new Color(0.55f, 0.10f, 0.10f);
                }
            }

            var pps = PlayerProgressionSystem.Instance;
            if (_txtXPJugador != null)
            {
                int xpGanada     = result?.xpGanada ?? 0;
                int nivelJugador = pps?.GetPlayerNivel() ?? 0;
                _txtXPJugador.text = $"+{xpGanada} XP   Jugador Nv.{nivelJugador}";
            }

            if (_barraXPFill != null && pps != null)
            {
                float prog    = pps.GetPlayerXPProgress();
                var   anchor  = _barraXPFill.anchorMax;
                anchor.x      = prog;
                _barraXPFill.anchorMax = anchor;
            }
        }

        // ── Filas de héroes ───────────────────────────────────────────────────

        private void PopulateHeroRows(CombatResult result, HeroInstance[] team)
        {
            if (_contenedorHeroes == null || team == null || team.Length == 0) return;

            var hps = HeroProgressionSystem.Instance;
            int xpPorHeroe = team.Length > 0 ? (result?.xpGanada ?? 0) / team.Length : 0;

            foreach (var hero in team)
            {
                var row = new GameObject($"HeroRow_{hero.heroId}");
                row.transform.SetParent(_contenedorHeroes, false);
                row.AddComponent<Image>().color = new Color(0.10f, 0.08f, 0.18f);
                row.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 50f);

                var nameGO  = new GameObject("Name");
                nameGO.transform.SetParent(row.transform, false);
                var nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
                nameTxt.text      = $"<b>{hero.heroId}</b>  Nv.{hero.nivel}";
                nameTxt.fontSize  = 13f;
                nameTxt.color     = Color.white;
                nameTxt.alignment = TextAlignmentOptions.MidlineLeft;
                SetChildAnchors(nameGO, new Vector2(0.01f, 0.55f), new Vector2(0.62f, 1f),
                                new Vector2(6, 2), new Vector2(0, -2));

                var xpGO  = new GameObject("XPGanada");
                xpGO.transform.SetParent(row.transform, false);
                var xpTxt = xpGO.AddComponent<TextMeshProUGUI>();
                xpTxt.text      = $"+{xpPorHeroe} XP";
                xpTxt.fontSize  = 12f;
                xpTxt.color     = new Color(0.8f, 0.8f, 0.4f);
                xpTxt.alignment = TextAlignmentOptions.MidlineRight;
                SetChildAnchors(xpGO, new Vector2(0.62f, 0.55f), new Vector2(1f, 1f),
                                new Vector2(0, 2), new Vector2(-6, -2));

                var barBgGO = new GameObject("XPBarBg");
                barBgGO.transform.SetParent(row.transform, false);
                barBgGO.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);
                SetChildAnchors(barBgGO, new Vector2(0.01f, 0.10f), new Vector2(0.99f, 0.50f),
                                Vector2.zero, Vector2.zero);

                float xpProg  = hps?.GetXPProgress(hero.heroId) ?? 0f;
                var barFillGO = new GameObject("XPBarFill");
                barFillGO.transform.SetParent(barBgGO.transform, false);
                barFillGO.AddComponent<Image>().color = new Color(0.35f, 0.55f, 1f);
                var fillRT    = barFillGO.GetComponent<RectTransform>();
                fillRT.anchorMin = Vector2.zero;
                fillRT.anchorMax = new Vector2(xpProg, 1f);
                fillRT.offsetMin = Vector2.zero;
                fillRT.offsetMax = Vector2.zero;
            }
        }

        // ── Drops ─────────────────────────────────────────────────────────────

        private void PopulateDrops(CombatResult result)
        {
            if (_contenedorDrops == null) return;

            string[] drops = result?.drops;
            if (drops == null || drops.Length == 0)
            {
                var emptyGO = new GameObject("NoDrops");
                emptyGO.transform.SetParent(_contenedorDrops, false);
                var txt = emptyGO.AddComponent<TextMeshProUGUI>();
                txt.text      = "Sin drops";
                txt.fontSize  = 13f;
                txt.color     = new Color(0.55f, 0.55f, 0.55f);
                txt.alignment = TextAlignmentOptions.Center;
                return;
            }

            foreach (var drop in drops)
            {
                var item = new GameObject($"Drop_{drop}");
                item.transform.SetParent(_contenedorDrops, false);
                item.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.18f);
                item.GetComponent<RectTransform>().sizeDelta = new Vector2(140f, 40f);

                var txt = new GameObject("Label");
                txt.transform.SetParent(item.transform, false);
                var tmp = txt.AddComponent<TextMeshProUGUI>();
                tmp.text      = drop;
                tmp.fontSize  = 12f;
                tmp.color     = new Color(0.9f, 0.75f, 0.3f);
                tmp.alignment = TextAlignmentOptions.Center;
                var tRT = txt.GetComponent<RectTransform>();
                tRT.anchorMin = Vector2.zero;
                tRT.anchorMax = Vector2.one;
                tRT.offsetMin = new Vector2(4, 2);
                tRT.offsetMax = new Vector2(-4, -2);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static int GetStarCount(CombatResult result, HeroInstance[] team)
        {
            if (result == null || !result.victoria) return 0;
            if (!string.IsNullOrEmpty(result.gradoObtenido))
            {
                if (result.gradoObtenido == "S" || result.gradoObtenido == "A") return 3;
                if (result.gradoObtenido == "B") return 2;
                return 1;
            }
            if (team != null)
            {
                int bajas = team.Count(h => !h.estaVivo);
                if (bajas == 0) return 3;
                if (bajas <= 2) return 2;
                return 1;
            }
            return 1;
        }

        private static void SetChildAnchors(GameObject go, Vector2 anchorMin, Vector2 anchorMax,
                                             Vector2 offsetMin, Vector2 offsetMax)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        private static void SetBtnAnchors(Button btn, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (btn == null) return;
            var rt = btn.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
