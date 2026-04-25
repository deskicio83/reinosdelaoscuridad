using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;
using ReinoOscuridad.Systems;

namespace ReinoOscuridad.UI.Campaign
{
    /// Overlay de resultado post-combate.
    /// Muestra VICTORIA/DERROTA, estrellas, XP jugador, XP de héroes y drops.
    ///
    /// Uso: RewardPanel.Show(prefab, result, team, onSiguienteFase)
    public class RewardPanel : MonoBehaviour
    {
        // ── Refs asignadas por SetupRewardPanelPrefab ──────────────────────────

        [SerializeField] private TMP_Text      _txtResultado;       // "VICTORIA" / "DERROTA"
        [SerializeField] private Image[]       _imgEstrellas;       // 3 sprites: dorado=ganada gris=vacia
        [SerializeField] private TMP_Text      _txtXPJugador;       // "+N XP  Nv.X"
        [SerializeField] private RectTransform _barraXPFill;        // fill de la barra de XP
        [SerializeField] private Transform     _contenedorHeroes;   // filas de héroes
        [SerializeField] private Transform     _contenedorDrops;    // items de drop
        [SerializeField] private Button        _btnRepetir;
        [SerializeField] private Button        _btnVolver;
        [SerializeField] private Button        _btnSiguiente;

        // ── Static factory ────────────────────────────────────────────────────

        /// Muestra el panel de recompensas como overlay vía UIManager.
        public static void Show(GameObject prefab, CombatResult result, HeroInstance[] team)
        {
            if (prefab == null || UIManager.Instance == null)
            {
                Debug.LogWarning("[RewardPanel] prefab null o UIManager no disponible.");
                return;
            }

            var go = UIManager.Instance.ShowOverlay(prefab);
            go?.GetComponent<RewardPanel>()?.Initialize(result, team);
        }

        // ── Inicialización ────────────────────────────────────────────────────

        public void Initialize(CombatResult result, HeroInstance[] team)
        {
            PopulateHeader(result);
            PopulateHeroRows(result, team);
            PopulateDrops(result);

            bool victoria = result?.victoria ?? false;

            // Victoria: 3 botones (Repetir | Volver | Siguiente)
            // Derrota:  2 botones (Repetir | Volver) — se reposicionan para centrar
            if (_btnRepetir != null)
            {
                _btnRepetir.gameObject.SetActive(true);
                _btnRepetir.onClick.AddListener(OnRepetir);
            }
            if (_btnVolver != null)
            {
                _btnVolver.gameObject.SetActive(true);
                _btnVolver.onClick.AddListener(OnVolver);
            }
            if (_btnSiguiente != null)
            {
                _btnSiguiente.gameObject.SetActive(victoria);
                if (victoria)
                    _btnSiguiente.onClick.AddListener(OnSiguiente);
            }

            // Reposicionar para layout derrota (2 botones centrados)
            if (!victoria)
            {
                SetBtnAnchors(_btnRepetir, new Vector2(0.05f, 0.04f), new Vector2(0.48f, 0.18f));
                SetBtnAnchors(_btnVolver,  new Vector2(0.52f, 0.04f), new Vector2(0.95f, 0.18f));
            }
        }

        // ── Header: resultado + estrellas + XP jugador ────────────────────────

        private void PopulateHeader(CombatResult result)
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
                int stars = GetStarCount(result);
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
                int xpGanada  = result?.xpGanada ?? 0;
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
                var rowRT = row.GetComponent<RectTransform>();
                rowRT.sizeDelta = new Vector2(0, 50f);

                // Nombre + nivel
                var nameGO = new GameObject("Name");
                nameGO.transform.SetParent(row.transform, false);
                var nameTxt = nameGO.AddComponent<TextMeshProUGUI>();
                nameTxt.text      = $"<b>{hero.heroId}</b>  Nv.{hero.nivel}";
                nameTxt.fontSize  = 13f;
                nameTxt.color     = Color.white;
                nameTxt.alignment = TextAlignmentOptions.MidlineLeft;
                SetChildAnchors(nameGO, new Vector2(0.01f, 0.55f), new Vector2(0.62f, 1f),
                                new Vector2(6, 2), new Vector2(0, -2));

                // XP obtenida
                var xpGO = new GameObject("XPGanada");
                xpGO.transform.SetParent(row.transform, false);
                var xpTxt = xpGO.AddComponent<TextMeshProUGUI>();
                xpTxt.text      = $"+{xpPorHeroe} XP";
                xpTxt.fontSize  = 12f;
                xpTxt.color     = new Color(0.8f, 0.8f, 0.4f);
                xpTxt.alignment = TextAlignmentOptions.MidlineRight;
                SetChildAnchors(xpGO, new Vector2(0.62f, 0.55f), new Vector2(1f, 1f),
                                new Vector2(0, 2), new Vector2(-6, -2));

                // Fondo de barra XP del héroe
                var barBgGO = new GameObject("XPBarBg");
                barBgGO.transform.SetParent(row.transform, false);
                barBgGO.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);
                SetChildAnchors(barBgGO, new Vector2(0.01f, 0.10f), new Vector2(0.99f, 0.50f),
                                Vector2.zero, Vector2.zero);

                // Relleno de barra XP del héroe
                float xpProg   = hps?.GetXPProgress(hero.heroId) ?? 0f;
                var barFillGO  = new GameObject("XPBarFill");
                barFillGO.transform.SetParent(barBgGO.transform, false);
                barFillGO.AddComponent<Image>().color = new Color(0.35f, 0.55f, 1f);
                var fillRT     = barFillGO.GetComponent<RectTransform>();
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
                var itemRT = item.GetComponent<RectTransform>();
                itemRT.sizeDelta = new Vector2(140f, 40f);

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

        // ── Botones ───────────────────────────────────────────────────────────

        private void OnSiguiente()
        {
            var ctx = CombatSceneData.LastContext;
            if (ctx != null && TryParseEncounterId(ctx.encounterID, out int mundo, out int fase))
            {
                CombatSceneData.NextFaseRequest = true;
                CombatSceneData.NextMundo       = mundo;
                CombatSceneData.NextFase        = fase + 1;
            }
            UIManager.Instance?.HideOverlay(gameObject);
            UIManager.Instance?.NavigateBack();
        }

        private void OnVolver()
        {
            var ctx = CombatSceneData.LastContext;
            if (ctx != null && TryParseEncounterId(ctx.encounterID, out int mundo, out _))
            {
                CombatSceneData.ReturnToPanelFases = true;
                CombatSceneData.NextMundo          = mundo;
            }
            UIManager.Instance?.HideOverlay(gameObject);
            UIManager.Instance?.NavigateBack();
        }

        private void OnRepetir()
        {
            CombatSceneData.PrepareRepeat();
            UIManager.Instance?.HideOverlay(gameObject);
            _ = UIManager.Instance?.NavigateTo("CombatScene");
        }

        private static bool TryParseEncounterId(string id, out int mundo, out int fase)
        {
            mundo = 0; fase = 0;
            if (string.IsNullOrEmpty(id)) return false;
            var parts = id.Split('_');
            if (parts.Length < 4) return false;
            if (!int.TryParse(parts[2], out int mNum)) return false;
            mundo = mNum - 1;
            if (parts[3] == "boss") { fase = 6; return true; }
            if (parts[3].StartsWith("f") && int.TryParse(parts[3].Substring(1), out int fNum))
            { fase = fNum - 1; return true; }
            return false;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static int GetStarCount(CombatResult result)
        {
            if (result == null || !result.victoria) return 0;
            if (result.gradoObtenido == "S" || result.gradoObtenido == "A") return 3;
            if (result.gradoObtenido == "B") return 2;
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
