using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;
using ReinoOscuridad.Systems;

namespace ReinoOscuridad.UI.Campaign
{
    /// Controlador de CampaignScene.
    /// 3 estados visuales: ScrollMundos → PanelFases (slide derecha) → PanelBatalla (overlay).
    /// NO es singleton — se instancia una vez por carga de CampaignScene.
    public class CampaignSceneController : MonoBehaviour
    {
        // ── Constantes ─────────────────────────────────────────────────────────

        private const int    MUNDOS          = 7;
        private const int    FASES_POR_MUNDO = 7; // f1-f6 + boss
        private const string DIF_NORMAL      = "normal";
        private const string DIF_DIFICIL     = "dificil";
        private const string DIF_HEROICA     = "heroica";
        private static readonly string[] DIFICULTADES = { DIF_NORMAL, DIF_DIFICIL, DIF_HEROICA };

        // ── UI refs — ScrollMundos ─────────────────────────────────────────────

        [SerializeField] private RectTransform _scrollMundos;       // root del estado ScrollMundos
        [SerializeField] private Transform     _contenedorMundosBtns; // content del scroll horizontal
        [SerializeField] private Transform     _indicadorDots;      // HLG de puntos indicadores

        // ── UI refs — PanelFases ───────────────────────────────────────────────

        [SerializeField] private RectTransform _panelFases;           // panel que slide desde derecha
        [SerializeField] private TMP_Text      _txtMundoNombre;       // header: "Mundo N"
        [SerializeField] private Button        _btnVolverFases;       // volver a ScrollMundos
        [SerializeField] private Transform     _contenedorDificultad; // HLG tabs dificultad (runtime)
        [SerializeField] private Transform     _contenedorFases;      // content scroll fases (runtime)
        [SerializeField] private Transform     _contenedorEsbirros;   // content scroll esbirros (runtime)
        [SerializeField] private Image         _imgElementalChart;    // placeholder tabla elemental
        [SerializeField] private Button        _btnReclamarRecompensa;

        // ── UI refs — PanelBatalla ─────────────────────────────────────────────

        [SerializeField] private RectTransform _panelBatalla;
        [SerializeField] private TMP_Text      _txtBatallaTitulo;
        [SerializeField] private TMP_Text      _txtBatallaEnergia;
        [SerializeField] private Transform     _contenedorEquipoSelec; // 4 slots héroe seleccionado
        [SerializeField] private Button        _btnEntrar;
        [SerializeField] private Button        _btnCancelarBatalla;

        // ── UI refs — PopupBloqueado ───────────────────────────────────────────

        [SerializeField] private GameObject _popupBloqueado;
        [SerializeField] private TMP_Text   _txtBloqueadoInfo;
        [SerializeField] private Button     _btnCerrarPopup;

        // ── UI refs — navegación ───────────────────────────────────────────────

        [SerializeField] private Button     _btnVolverMain;
        [SerializeField] private GameObject _battlePrepPrefab;  // reservado, no usado en este flujo
        [SerializeField] private GameObject _rewardPrefab;

        // ── Estado de selección ────────────────────────────────────────────────

        private int          _mundoSeleccionado      = 0;
        private string       _dificultadSeleccionada = DIF_NORMAL;
        private int          _faseSeleccionada       = 0;
        private string       _pendingEncounterKey;
        private List<string> _selectedHeroIds        = new List<string>();

        // ── Catálogos en memoria ───────────────────────────────────────────────

        private Dictionary<string, EncounterEntry>    _encounterById;
        private Dictionary<string, EnemyCatalogEntry> _enemyById;

        // ── Modelos de catálogo (privados) ─────────────────────────────────────

        private class EncounterCatalogRoot { public List<EncounterEntry> encounters; }
        private class EncounterEntry
        {
            public string       encounterId;
            public string       world;
            public string       phase;
            public string       difficulty;
            public List<string> enemies;
            public string       environmentId;
            public int          energyCost;
        }

        private class EnemyCatalogRoot { public List<EnemyCatalogEntry> enemies; }
        private class EnemyCatalogEntry
        {
            public string                  enemyId;
            public string                  name_es;
            public string                  element;
            public int                     spd;
            public Dictionary<string, int> levels;
        }

        // ── Ciclo de vida ──────────────────────────────────────────────────────

        private void Awake()
        {
            LoadCatalogs();
            if (PlayerDataSystem.Instance == null)
                Debug.LogWarning("[CampaignController] PlayerDataSystem no disponible — modo preview (datos offline).");
        }

        private void Start()
        {
            EnsureCampanaData();
            BindButtons();
            BuildMundoButtons();
            BuildDifTabs();

            // Estados iniciales
            if (_panelFases != null)
            {
                _panelFases.anchoredPosition = new Vector2(1280f, 0f);
                _panelFases.gameObject.SetActive(false);
            }
            if (_panelBatalla   != null) _panelBatalla.gameObject.SetActive(false);
            if (_popupBloqueado != null) _popupBloqueado.SetActive(false);

            CheckCombatReturn();

            Debug.Log("[CampaignController] CampaignScene lista.");
        }

        private void BindButtons()
        {
            if (_btnVolverMain      != null) _btnVolverMain.onClick.AddListener(() => UIManager.Instance?.NavigateBack());
            if (_btnVolverFases     != null) _btnVolverFases.onClick.AddListener(ClosePanelFases);
            if (_btnCancelarBatalla != null) _btnCancelarBatalla.onClick.AddListener(ClosePanelBatalla);
            if (_btnEntrar          != null) _btnEntrar.onClick.AddListener(TryEnterBatalla);
            if (_btnCerrarPopup     != null) _btnCerrarPopup.onClick.AddListener(() => _popupBloqueado?.SetActive(false));
            if (_btnReclamarRecompensa != null) _btnReclamarRecompensa.onClick.AddListener(() => ReclamarRecompensaMundo(_mundoSeleccionado));
        }

        // ── API pública — desbloqueos ──────────────────────────────────────────

        /// true si mundo 0 o el boss del mundo anterior fue completado en Normal.
        public bool IsMundoDesbloqueado(int mundo)
        {
            if (mundo == 0) return true;
            var completadas = GetCampanaData()?.fasesCompletadas;
            return completadas != null &&
                   completadas.Contains(BuildEncounterKey(mundo - 1, FASES_POR_MUNDO - 1, DIF_NORMAL));
        }

        /// true si la fase puede jugarse.
        public bool IsFaseDesbloqueada(int mundo, int fase, string dif)
        {
            if (mundo == 0 && fase == 0 && dif == DIF_NORMAL) return true;

            var completadas = GetCampanaData()?.fasesCompletadas;
            if (completadas == null) return false;

            switch (dif)
            {
                case DIF_DIFICIL:
                    for (int f = 0; f < FASES_POR_MUNDO; f++)
                        if (!completadas.Contains(BuildEncounterKey(mundo, f, DIF_NORMAL))) return false;
                    if (fase == 0) return true;
                    return completadas.Contains(BuildEncounterKey(mundo, fase - 1, DIF_DIFICIL));

                case DIF_HEROICA:
                    for (int f = 0; f < FASES_POR_MUNDO; f++)
                        if (!completadas.Contains(BuildEncounterKey(mundo, f, DIF_DIFICIL))) return false;
                    if (fase == 0) return true;
                    return completadas.Contains(BuildEncounterKey(mundo, fase - 1, DIF_HEROICA));

                default: // normal
                    if (fase == 0)
                        return mundo == 0 ||
                               completadas.Contains(BuildEncounterKey(mundo - 1, FASES_POR_MUNDO - 1, DIF_NORMAL));
                    return completadas.Contains(BuildEncounterKey(mundo, fase - 1, DIF_NORMAL));
            }
        }

        // ── API pública — clave de encuentro ───────────────────────────────────

        /// Genera la clave de encuentro compatible con encounter_catalog.json.
        /// fase 0-5 = "f1"-"f6"; fase 6 = "boss".
        public string BuildEncounterKey(int mundo, int fase, string dif)
        {
            string worldStr = $"mundo_{mundo + 1}";
            string phaseStr = fase < FASES_POR_MUNDO - 1 ? $"f{fase + 1}" : "boss";
            return $"campaign_{worldStr}_{phaseStr}_{dif}";
        }

        // ── API pública — equipos ──────────────────────────────────────────────

        public HeroInstance[] BuildPlayerTeam()
        {
            var pds = PlayerDataSystem.Instance;
            var gs  = GearSystem.Instance;
            if (pds == null || gs == null) return Array.Empty<HeroInstance>();

            var heroes = pds.GetPlayerData()?.heroes;
            if (heroes == null || heroes.Count == 0) return Array.Empty<HeroInstance>();

            var result = new List<HeroInstance>();
            int limit  = Mathf.Min(heroes.Count, 4);
            for (int i = 0; i < limit; i++)
            {
                var hi = gs.BuildCombatInstance(heroes[i].heroId);
                if (hi != null) result.Add(hi);
            }
            return result.ToArray();
        }

        public EnemyInstance[] BuildEnemyTeam(string encounterKey)
        {
            if (_encounterById != null && _encounterById.TryGetValue(encounterKey, out var enc)
                && enc.enemies != null && enc.enemies.Count > 0)
            {
                var result = new List<EnemyInstance>();
                foreach (var eid in enc.enemies)
                {
                    EnemyCatalogEntry edata = null;
                    _enemyById?.TryGetValue(eid, out edata);
                    result.Add(BuildEnemyInstance(eid, edata, enc.difficulty));
                }
                return result.ToArray();
            }
            Debug.LogWarning($"[CampaignController] Encounter '{encounterKey}' no encontrado — placeholder.");
            return new[] { PlaceholderEnemy() };
        }

        // ── API pública — progreso ─────────────────────────────────────────────

        public void MarcarFaseCompletada(string encounterKey)
        {
            var campana = EnsureCampanaData();
            if (campana == null) return;
            if (!campana.fasesCompletadas.Contains(encounterKey))
                campana.fasesCompletadas.Add(encounterKey);
            PlayerDataSystem.Instance?.MarkDirty();
        }

        // ── Selección de mundo ─────────────────────────────────────────────────

        public void SelectMundo(int mundo)
        {
            if (!IsMundoDesbloqueado(mundo))
            {
                ShowPopupBloqueado($"Completa el Mundo {mundo} en dificultad Normal para desbloquear.");
                return;
            }

            _mundoSeleccionado      = Mathf.Clamp(mundo, 0, MUNDOS - 1);
            _dificultadSeleccionada = DIF_NORMAL;

            if (_txtMundoNombre != null)
                _txtMundoNombre.text = $"Mundo {_mundoSeleccionado + 1}";

            BuildDifTabs();
            RefreshFaseNodes();
            PoblarScrollEsbirros(_mundoSeleccionado, _dificultadSeleccionada);
            RefreshBtnReclamar();

            // Ocultar ScrollMundos mientras dure la animación y después
            if (_scrollMundos != null) _scrollMundos.gameObject.SetActive(false);
            StartCoroutine(SlidePanelFromRight(_panelFases, slideIn: true));
        }

        public void SelectDificultad(string dif)
        {
            if (Array.IndexOf(DIFICULTADES, dif) < 0) return;
            _dificultadSeleccionada = dif;
            RefreshFaseNodes();
            PoblarScrollEsbirros(_mundoSeleccionado, dif);
        }

        // ── Selección de fase ──────────────────────────────────────────────────

        public void SelectFase(int fase)
        {
            _faseSeleccionada = fase;
            string key    = BuildEncounterKey(_mundoSeleccionado, fase, _dificultadSeleccionada);
            bool   esBoss = fase == FASES_POR_MUNDO - 1;

            if (!IsFaseDesbloqueada(_mundoSeleccionado, fase, _dificultadSeleccionada))
            {
                ShowPopupBloqueado("Completa la fase anterior para desbloquear esta.");
                return;
            }

            _pendingEncounterKey = key;
            var campana = EnsureCampanaData();
            if (campana != null) campana.ultimoEncuentroIntentado = key;

            string titulo = esBoss
                ? $"JEFE   M{_mundoSeleccionado + 1}   {_dificultadSeleccionada.ToUpper()}"
                : $"M{_mundoSeleccionado + 1}   F{fase + 1}   {_dificultadSeleccionada.ToUpper()}";
            int cost = GetEnergyCost(key);

            if (_txtBatallaTitulo  != null) _txtBatallaTitulo.text  = titulo;
            if (_txtBatallaEnergia != null) _txtBatallaEnergia.text = $"Coste: {cost} energia";

            _selectedHeroIds.Clear();
            RefreshEquipoSeleccionado();

            if (_panelBatalla != null) _panelBatalla.gameObject.SetActive(true);
        }

        // ── Equipo seleccionado ────────────────────────────────────────────────

        public void ToggleHeroInTeam(string heroId)
        {
            if (_selectedHeroIds.Contains(heroId))
                _selectedHeroIds.Remove(heroId);
            else if (_selectedHeroIds.Count < 4)
                _selectedHeroIds.Add(heroId);
            RefreshEquipoSeleccionado();
        }

        private void RefreshEquipoSeleccionado()
        {
            if (_contenedorEquipoSelec == null) return;
            for (int ci = _contenedorEquipoSelec.childCount - 1; ci >= 0; ci--)
            {
                var child = _contenedorEquipoSelec.GetChild(ci);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
            for (int s = 0; s < 4; s++)
            {
                bool ocupado = s < _selectedHeroIds.Count;

                var slot = new GameObject($"SlotEquipo_{s}");
                slot.transform.SetParent(_contenedorEquipoSelec, false);
                var le = slot.AddComponent<LayoutElement>();
                le.preferredWidth  = 80f;
                le.preferredHeight = 80f;
                var img = slot.AddComponent<Image>();
                img.color = ocupado ? new Color(0.15f, 0.40f, 0.18f) : new Color(0.12f, 0.12f, 0.18f);

                var lblGO = new GameObject("Lbl");
                lblGO.transform.SetParent(slot.transform, false);
                var txt = lblGO.AddComponent<TextMeshProUGUI>();
                txt.text      = ocupado ? _selectedHeroIds[s].Split('_')[0] : "---";
                txt.fontSize  = 10f;
                txt.color     = Color.white;
                txt.alignment = TextAlignmentOptions.Center;
                var lrt = lblGO.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
            }
        }

        // ── Entrar a batalla ───────────────────────────────────────────────────

        public void TryEnterBatalla()
        {
            int cost = GetEnergyCost(_pendingEncounterKey);
            var eco  = EconomySystem.Instance;
            if (eco != null && !eco.ConsumeEnergy(cost))
            {
                if (_txtBatallaEnergia != null) _txtBatallaEnergia.text = "Energia insuficiente";
                Debug.Log("[CampaignController] Energia insuficiente.");
                return;
            }

            HeroInstance[] team = _selectedHeroIds.Count > 0
                ? RebuildTeam(_selectedHeroIds.ToArray())
                : BuildPlayerTeam();

            if (_panelBatalla != null) _panelBatalla.gameObject.SetActive(false);

            CombatSceneData.PendingContext = new CombatContext
            {
                encounterID     = _pendingEncounterKey,
                callerScene     = "CampaignScene",
                combatMode      = "campaign",
                playerTeam      = team,
                enemyTeam       = BuildEnemyTeam(_pendingEncounterKey),
                maldicionActiva = false,
                elementoBoss    = null
            };

            UIManager.Instance?.NavigateTo("CombatScene");
        }

        // ── Scroll de esbirros ─────────────────────────────────────────────────

        public void PoblarScrollEsbirros(int mundo, string dif)
        {
            if (_contenedorEsbirros == null) return;
            for (int ci = _contenedorEsbirros.childCount - 1; ci >= 0; ci--)
            {
                var child = _contenedorEsbirros.GetChild(ci);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            // Recolectar IDs de enemigos sin duplicados de todas las fases del mundo/dif
            var ids = new List<string>();
            for (int f = 0; f < FASES_POR_MUNDO; f++)
            {
                string key = BuildEncounterKey(mundo, f, dif);
                if (_encounterById != null && _encounterById.TryGetValue(key, out var enc) && enc.enemies != null)
                    foreach (var eid in enc.enemies)
                        if (!ids.Contains(eid)) ids.Add(eid);
            }
            if (ids.Count == 0) ids.Add("esbirro_oscuro");

            foreach (var eid in ids)
            {
                _enemyById?.TryGetValue(eid, out var edata);
                string nombre   = edata?.name_es ?? eid;
                string elemento = edata?.element ?? "Oscuridad";

                var card = new GameObject($"EsbirroCard_{eid}");
                card.transform.SetParent(_contenedorEsbirros, false);
                var le = card.AddComponent<LayoutElement>();
                le.preferredWidth  = 68f;
                le.preferredHeight = 78f;
                card.AddComponent<Image>().color = new Color(0.12f, 0.09f, 0.16f);

                var iconGO = new GameObject("Icono");
                iconGO.transform.SetParent(card.transform, false);
                iconGO.AddComponent<Image>().color = ElementoColor(elemento);
                var iconRT = iconGO.GetComponent<RectTransform>();
                iconRT.anchorMin = new Vector2(0.10f, 0.55f);
                iconRT.anchorMax = new Vector2(0.90f, 0.92f);
                iconRT.offsetMin = Vector2.zero;
                iconRT.offsetMax = Vector2.zero;

                var nomGO = new GameObject("Nombre");
                nomGO.transform.SetParent(card.transform, false);
                var nomTxt = nomGO.AddComponent<TextMeshProUGUI>();
                nomTxt.text      = nombre.Length > 8 ? nombre.Substring(0, 8) : nombre;
                nomTxt.fontSize  = 8f;
                nomTxt.color     = Color.white;
                nomTxt.alignment = TextAlignmentOptions.Center;
                var nomRT = nomGO.GetComponent<RectTransform>();
                nomRT.anchorMin = new Vector2(0f, 0.05f);
                nomRT.anchorMax = new Vector2(1f, 0.52f);
                nomRT.offsetMin = Vector2.zero;
                nomRT.offsetMax = Vector2.zero;
            }
        }

        // ── Recompensa de mundo ────────────────────────────────────────────────

        public void ReclamarRecompensaMundo(int mundo)
        {
            var completadas = GetCampanaData()?.fasesCompletadas;
            if (completadas == null) return;

            for (int f = 0; f < FASES_POR_MUNDO; f++)
                if (!completadas.Contains(BuildEncounterKey(mundo, f, DIF_NORMAL)))
                {
                    Debug.Log($"[CampaignController] Mundo {mundo + 1} incompleto — no se puede reclamar.");
                    return;
                }

            Debug.Log($"[CampaignController] Recompensa Mundo {mundo + 1} reclamada. TODO(S23): conceder ítems reales.");
            RefreshBtnReclamar();
        }

        // ── Retorno de combate ─────────────────────────────────────────────────

        private void CheckCombatReturn()
        {
            var result  = CombatSceneData.LastResult;
            var campana = GetCampanaData();
            if (result == null) return;

            if (result.victoria && campana != null
                && !string.IsNullOrEmpty(campana.ultimoEncuentroIntentado))
            {
                MarcarFaseCompletada(campana.ultimoEncuentroIntentado);
                campana.ultimoEncuentroIntentado = null;
            }
            CombatSceneData.SetResult(null);

            // Refrescar UI con el nuevo estado de progreso
            BuildMundoButtons();

            var lastTeamIds = PlayerDataSystem.Instance?.GetPlayerData()?.lastTeam;
            var team = RebuildTeam(lastTeamIds);
            if (_rewardPrefab != null)
                RewardPanel.Show(_rewardPrefab, result, team, () =>
                    Debug.Log("[CampaignController] Recompensa vista."));
        }

        // ── Helpers UI internos ────────────────────────────────────────────────

        private void ClosePanelFases()
        {
            StartCoroutine(SlidePanelFromRight(_panelFases, slideIn: false));
            // Restaurar ScrollMundos al terminar la animación — se hace dentro del coroutine
        }

        private void ClosePanelBatalla()
        {
            if (_panelBatalla != null) _panelBatalla.gameObject.SetActive(false);
        }

        private void ShowPopupBloqueado(string info)
        {
            if (_popupBloqueado == null) return;
            if (_txtBloqueadoInfo != null) _txtBloqueadoInfo.text = info;
            _popupBloqueado.SetActive(true);
        }

        private void RefreshBtnReclamar()
        {
            if (_btnReclamarRecompensa == null) return;
            bool ok = true;
            var completadas = GetCampanaData()?.fasesCompletadas;
            if (completadas == null) ok = false;
            else
                for (int f = 0; f < FASES_POR_MUNDO; f++)
                    if (!completadas.Contains(BuildEncounterKey(_mundoSeleccionado, f, DIF_NORMAL)))
                    {
                        ok = false;
                        break;
                    }
            _btnReclamarRecompensa.interactable = ok;
        }

        private IEnumerator SlidePanelFromRight(RectTransform panel, bool slideIn, float duration = 0.22f)
        {
            if (panel == null) yield break;
            float from = slideIn ? 1280f : 0f;
            float to   = slideIn ? 0f   : 1280f;
            panel.gameObject.SetActive(true);

            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(t + Time.deltaTime / duration, 1f);
                float e = t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t; // ease-in-out quad
                panel.anchoredPosition = new Vector2(Mathf.Lerp(from, to, e), 0f);
                yield return null;
            }

            if (!slideIn)
            {
                panel.gameObject.SetActive(false);
                if (_scrollMundos != null) _scrollMundos.gameObject.SetActive(true);
            }
        }

        // ── Construcción de botones de mundo ───────────────────────────────────

        private void BuildMundoButtons()
        {
            if (_contenedorMundosBtns == null) return;
            for (int ci = _contenedorMundosBtns.childCount - 1; ci >= 0; ci--)
            {
                var child = _contenedorMundosBtns.GetChild(ci);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            var campana = GetCampanaData();
            for (int m = 0; m < MUNDOS; m++)
            {
                int  captured     = m;
                bool desb         = IsMundoDesbloqueado(m);

                int fasesComp = 0;
                if (campana?.fasesCompletadas != null)
                    for (int f = 0; f < FASES_POR_MUNDO; f++)
                        if (campana.fasesCompletadas.Contains(BuildEncounterKey(m, f, DIF_NORMAL)))
                            fasesComp++;

                var go = new GameObject($"BtnMundo{m + 1}");
                go.transform.SetParent(_contenedorMundosBtns, false);
                var le = go.AddComponent<LayoutElement>();
                le.preferredWidth  = 148f;
                le.preferredHeight = 170f;

                var img = go.AddComponent<Image>();
                img.color = !desb
                    ? new Color(0.08f, 0.08f, 0.10f)
                    : fasesComp >= FASES_POR_MUNDO
                        ? new Color(0.08f, 0.28f, 0.10f)
                        : new Color(0.14f, 0.08f, 0.22f);

                var btn = go.AddComponent<Button>();
                btn.interactable = desb;

                // Nombre M1–M7
                var lblGO = new GameObject("Label");
                lblGO.transform.SetParent(go.transform, false);
                var lbl = lblGO.AddComponent<TextMeshProUGUI>();
                lbl.text      = desb ? $"Mundo {m + 1}" : $"M{m + 1}\n[LOCK]";
                lbl.fontSize  = 16f;
                lbl.color     = desb ? Color.white : new Color(0.40f, 0.40f, 0.40f);
                lbl.alignment = TextAlignmentOptions.Center;
                var lrt = lblGO.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 0.45f);
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(4f, 4f);
                lrt.offsetMax = new Vector2(-4f, -4f);

                // Progreso N/7
                var progGO = new GameObject("Progreso");
                progGO.transform.SetParent(go.transform, false);
                var prog = progGO.AddComponent<TextMeshProUGUI>();
                prog.text      = desb ? $"{fasesComp}/{FASES_POR_MUNDO}" : "---";
                prog.fontSize  = 12f;
                prog.color     = new Color(0.65f, 0.65f, 0.65f);
                prog.alignment = TextAlignmentOptions.Center;
                var prt = progGO.GetComponent<RectTransform>();
                prt.anchorMin = Vector2.zero;
                prt.anchorMax = new Vector2(1f, 0.45f);
                prt.offsetMin = new Vector2(4f, 4f);
                prt.offsetMax = new Vector2(-4f, -4f);

                btn.onClick.AddListener(() => SelectMundo(captured));
            }

            BuildIndicadorDots();
        }

        private void BuildIndicadorDots()
        {
            if (_indicadorDots == null) return;
            for (int ci = _indicadorDots.childCount - 1; ci >= 0; ci--)
            {
                var child = _indicadorDots.GetChild(ci);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
            for (int m = 0; m < MUNDOS; m++)
            {
                var dot = new GameObject($"Dot_{m}");
                dot.transform.SetParent(_indicadorDots, false);
                var le = dot.AddComponent<LayoutElement>();
                le.preferredWidth  = 10f;
                le.preferredHeight = 10f;
                dot.AddComponent<Image>().color = IsMundoDesbloqueado(m)
                    ? new Color(0.55f, 0.28f, 0.85f)
                    : new Color(0.22f, 0.22f, 0.25f);
            }
        }

        private void BuildDifTabs()
        {
            if (_contenedorDificultad == null) return;
            for (int ci = _contenedorDificultad.childCount - 1; ci >= 0; ci--)
            {
                var child = _contenedorDificultad.GetChild(ci);
                child.SetParent(null);
                Destroy(child.gameObject);
            }
            foreach (string dif in DIFICULTADES)
            {
                string capturedDif = dif;
                var go = new GameObject($"BtnDif_{dif}");
                go.transform.SetParent(_contenedorDificultad, false);
                var le = go.AddComponent<LayoutElement>();
                le.preferredWidth  = 110f;
                le.preferredHeight = 36f;
                go.AddComponent<Image>().color = new Color(0.20f, 0.13f, 0.28f);
                var btn = go.AddComponent<Button>();

                var lblGO = new GameObject("Label");
                lblGO.transform.SetParent(go.transform, false);
                var tmp = lblGO.AddComponent<TextMeshProUGUI>();
                tmp.text      = char.ToUpper(dif[0]) + dif.Substring(1);
                tmp.fontSize  = 13f;
                tmp.color     = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
                var lrt = lblGO.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;

                btn.onClick.AddListener(() => SelectDificultad(capturedDif));
            }
        }

        private void RefreshFaseNodes()
        {
            if (_contenedorFases == null) return;
            for (int ci = _contenedorFases.childCount - 1; ci >= 0; ci--)
            {
                var child = _contenedorFases.GetChild(ci);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            var campana = GetCampanaData();
            for (int f = 0; f < FASES_POR_MUNDO; f++)
            {
                int    capturedFase = f;
                bool   esBoss       = f == FASES_POR_MUNDO - 1;
                bool   desb         = IsFaseDesbloqueada(_mundoSeleccionado, f, _dificultadSeleccionada);
                string key          = BuildEncounterKey(_mundoSeleccionado, f, _dificultadSeleccionada);
                bool   completada   = campana?.fasesCompletadas?.Contains(key) ?? false;
                string label        = esBoss ? $"Boss M{_mundoSeleccionado + 1}" : $"F{f + 1}";

                Color nodeColor;
                if (!desb)          nodeColor = new Color(0.18f, 0.18f, 0.20f);
                else if (completada) nodeColor = new Color(0.10f, 0.38f, 0.12f);
                else if (esBoss)    nodeColor = new Color(0.68f, 0.44f, 0.04f);
                else                nodeColor = new Color(0.18f, 0.11f, 0.30f);

                var go = new GameObject($"FaseNode_{f}");
                go.transform.SetParent(_contenedorFases, false);
                var le = go.AddComponent<LayoutElement>();
                le.preferredWidth  = 108f;
                le.preferredHeight = 78f;
                go.AddComponent<Image>().color = nodeColor;
                var btn = go.AddComponent<Button>();
                btn.interactable = desb;

                var lblGO = new GameObject("Label");
                lblGO.transform.SetParent(go.transform, false);
                var tmp = lblGO.AddComponent<TextMeshProUGUI>();
                tmp.text      = label;
                tmp.fontSize  = 13f;
                tmp.color     = desb ? Color.white : new Color(0.38f, 0.38f, 0.38f);
                tmp.alignment = TextAlignmentOptions.Center;
                var lrt = lblGO.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 0.45f);
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(4f, 2f);
                lrt.offsetMax = new Vector2(-4f, -2f);

                var costGO = new GameObject("Cost");
                costGO.transform.SetParent(go.transform, false);
                var costTxt = costGO.AddComponent<TextMeshProUGUI>();
                costTxt.text      = desb ? $"{GetEnergyCost(key)} nrg" : "---";
                costTxt.fontSize  = 10f;
                costTxt.color     = new Color(0.45f, 0.70f, 1f);
                costTxt.alignment = TextAlignmentOptions.Center;
                var crt = costGO.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(0f, 0f);
                crt.anchorMax = new Vector2(1f, 0.45f);
                crt.offsetMin = new Vector2(4f, 2f);
                crt.offsetMax = new Vector2(-4f, -2f);

                btn.onClick.AddListener(() => SelectFase(capturedFase));
            }
        }

        // ── Helpers internos ───────────────────────────────────────────────────

        private CampaignProgressData GetCampanaData()
            => PlayerDataSystem.Instance?.GetPlayerData()?.campana;

        private CampaignProgressData EnsureCampanaData()
        {
            var pd = PlayerDataSystem.Instance?.GetPlayerData();
            if (pd == null) return null;

            pd.campana ??= new CampaignProgressData
            {
                mundoActual              = 0,
                faseActual               = 0,
                dificultadActual         = DIF_NORMAL,
                fasesCompletadas         = new List<string>(),
                ultimoEncuentroIntentado = null
            };
            pd.campana.fasesCompletadas ??= new List<string>();
            return pd.campana;
        }

        private int GetEnergyCost(string encounterKey)
        {
            if (_encounterById != null && _encounterById.TryGetValue(encounterKey, out var enc))
                return enc.energyCost;
            return 6;
        }

        private HeroInstance[] RebuildTeam(string[] heroIds)
        {
            if (heroIds == null || heroIds.Length == 0) return Array.Empty<HeroInstance>();
            var gs = GearSystem.Instance;
            if (gs == null) return Array.Empty<HeroInstance>();
            var result = new List<HeroInstance>();
            foreach (var id in heroIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                var hi = gs.BuildCombatInstance(id);
                if (hi != null) result.Add(hi);
            }
            return result.ToArray();
        }

        private EnemyInstance BuildEnemyInstance(string enemyId, EnemyCatalogEntry data, string dif)
        {
            int level = 1;
            if (data?.levels != null && data.levels.TryGetValue(dif, out int l)) level = l;
            int baseHp  = 1000 + level * 50;
            int baseAtk = 100  + level * 5;
            int baseDef = 80   + level * 4;
            return new EnemyInstance
            {
                enemyId        = enemyId,
                nombre         = data?.name_es ?? enemyId,
                nivel          = level,
                hpActual       = baseHp,
                hpMax          = baseHp,
                atk            = baseAtk,
                def            = baseDef,
                spd            = data?.spd ?? 100,
                agi            = 100,
                elemento       = data?.element ?? "Oscuridad",
                estaVivo       = true,
                efectosActivos = new List<string>()
            };
        }

        private static EnemyInstance PlaceholderEnemy()
            => new EnemyInstance
            {
                enemyId        = "placeholder_enemy",
                nombre         = "Esbirro Oscuro",
                nivel          = 1,
                hpActual       = 1000,
                hpMax          = 1000,
                atk            = 100,
                def            = 80,
                spd            = 100,
                agi            = 100,
                elemento       = "Oscuridad",
                estaVivo       = true,
                efectosActivos = new List<string>()
            };

        private static Color ElementoColor(string elem)
        {
            switch (elem?.ToLower())
            {
                case "fuego":      return new Color(1.00f, 0.20f, 0.00f);
                case "agua":       return new Color(0.10f, 0.40f, 1.00f);
                case "tierra":     return new Color(0.50f, 0.30f, 0.10f);
                case "naturaleza": return new Color(0.10f, 0.60f, 0.10f);
                case "luz":        return new Color(1.00f, 1.00f, 0.20f);
                case "rayo":       return new Color(0.20f, 0.80f, 1.00f);
                case "hielo":      return new Color(0.60f, 0.85f, 1.00f);
                default:           return new Color(0.28f, 0.00f, 0.42f); // oscuridad
            }
        }

        // ── Carga de catálogos ─────────────────────────────────────────────────

        private void LoadCatalogs()
        {
            _encounterById = new Dictionary<string, EncounterEntry>();
            _enemyById     = new Dictionary<string, EnemyCatalogEntry>();
            LoadEncounterCatalog();
            LoadEnemyCatalog();
        }

        private void LoadEncounterCatalog()
        {
            var ta = Resources.Load<TextAsset>("Data/encounter_catalog");
            if (ta == null)
            {
                Debug.LogError("[CampaignController] encounter_catalog no encontrado en Resources/Data/");
                return;
            }
            var settings = new JsonSerializerSettings
                { Error = (_, args) => { args.ErrorContext.Handled = true; } };
            var root = JsonConvert.DeserializeObject<EncounterCatalogRoot>(ta.text, settings);
            if (root?.encounters == null) return;
            foreach (var e in root.encounters)
                if (e.encounterId != null) _encounterById[e.encounterId] = e;
            Debug.Log($"[CampaignController] {_encounterById.Count} encuentros cargados.");
        }

        private void LoadEnemyCatalog()
        {
            var ta = Resources.Load<TextAsset>("Data/enemy_catalog");
            if (ta == null)
            {
                Debug.LogError("[CampaignController] enemy_catalog no encontrado en Resources/Data/");
                return;
            }
            var root = JsonConvert.DeserializeObject<EnemyCatalogRoot>(ta.text);
            if (root?.enemies == null) return;
            foreach (var e in root.enemies)
                _enemyById[e.enemyId] = e;
            Debug.Log($"[CampaignController] {_enemyById.Count} enemigos cargados.");
        }
    }
}
