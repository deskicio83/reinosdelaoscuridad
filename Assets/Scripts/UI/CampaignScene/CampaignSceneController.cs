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
    /// ESTADO 1 (S22a): Scroll horizontal de 7 mundos.
    /// ESTADO 2 (S22b): PanelFases slide-in desde derecha.
    /// ESTADO 3 (S22c): PanelBatalla (pendiente).
    public class CampaignSceneController : MonoBehaviour
    {
        // ── Constantes ─────────────────────────────────────────────────────────

        private const int MUNDOS          = 7;
        private const int FASES_POR_MUNDO = 7; // f1-f6 + boss

        private static readonly string[] MUNDO_NOMBRES =
        {
            "Cripta de los Quejosos",
            "Circo Agonizante",
            "Pantano del Arrepentimiento",
            "Fabrica de Pesadillas",
            "Salon de los Fracasados",
            "Cementerio de Modas",
            "Trono del Caos Eterno",
        };

        // ── UI refs — ESTADO 1: ScrollMundos ──────────────────────────────────

        [SerializeField] private ScrollRect    _scrollMundos;
        [SerializeField] private RectTransform _contentMundos;
        [SerializeField] private Button[]      _btnsMundo;          // [7]
        [SerializeField] private GameObject[]  _lockedOverlays;     // [7]
        [SerializeField] private Image[]       _mundoSelBordes;     // [7]
        [SerializeField] private TMP_Text[]    _mundoNombres;       // [7]
        [SerializeField] private TMP_Text[]    _progresoTexts;      // [7]
        [SerializeField] private Image[]       _dots;               // [7]
        [SerializeField] private GameObject    _popupBloqueado;
        [SerializeField] private Button        _btnVolverMain;

        // ── UI refs — ESTADO 2: PanelFases ────────────────────────────────────

        [SerializeField] private RectTransform _panelFasesRT;
        [SerializeField] private TMP_Text      _tituloMundoFases;
        [SerializeField] private Button        _btnCerrarFases;
        [SerializeField] private Button[]      _btnsFase;           // [7]
        [SerializeField] private Image[]       _nodoFaseImages;     // [7]
        [SerializeField] private TMP_Text[]    _dropTexts;          // [7]
        [SerializeField] private TMP_Text[]    _estrellasTexts;     // [7]
        [SerializeField] private GameObject[]  _lockIconsFase;      // [7]

        // ── UI refs stub — ESTADO 3 (S22c) ────────────────────────────────────

        [SerializeField] private GameObject _panelBatalla;

        // ── Estado interno ─────────────────────────────────────────────────────

        private int    _mundoSeleccionado   = -1;
        private int    _faseSeleccionada    = -1;
        private string _dificultadActual    = "normal";
        private bool   _panelFasesAbierto   = false;
        private float  _panelFasesAncho     = 0f;

        private bool[][] _fasesCompletadas;           // [mundo][fase]
        private bool[]   _mundosRecompensaReclamada;

        // ── Catálogos ──────────────────────────────────────────────────────────

        private Dictionary<string, EncounterEntry>    _encounterById;
        private Dictionary<string, EnemyCatalogEntry> _enemyById;

        // ── Modelos privados de catálogo ───────────────────────────────────────

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
            public string       phase_name_es;
            public string       drop_gear_slot;
            public string       drop_tipo;
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
            LoadEncounterCatalog();
            LoadEnemyCatalog();
            if (PlayerDataSystem.Instance == null)
                Debug.LogWarning("[CampaignController] PlayerDataSystem no disponible — modo preview offline.");
        }

        private void Start()
        {
            CargarProgreso();
            BindButtons();
            RefreshMundos();
            StartCoroutine(CentrarScrollInicial());
            StartCoroutine(InicializarPanelFases());

            if (_popupBloqueado != null) _popupBloqueado.SetActive(false);
            if (_panelBatalla   != null) _panelBatalla.SetActive(false);

            Debug.Log("[CampaignController] ESTADO 1+2 — scroll mundos + panel fases listos.");
        }

        // ── ESTADO 1: Progreso ─────────────────────────────────────────────────

        private void CargarProgreso()
        {
            _fasesCompletadas          = new bool[MUNDOS][];
            _mundosRecompensaReclamada = new bool[MUNDOS];

            for (int m = 0; m < MUNDOS; m++)
                _fasesCompletadas[m] = new bool[FASES_POR_MUNDO];

            var pds = PlayerDataSystem.Instance;
            if (pds == null) return;

            var pd = pds.GetPlayerData();
            if (pd?.campana?.fasesCompletadas == null) return;

            foreach (var key in pd.campana.fasesCompletadas)
            {
                if (!TryParseEncounterKey(key, out int m, out int f)) continue;
                if (m >= 0 && m < MUNDOS && f >= 0 && f < FASES_POR_MUNDO)
                    _fasesCompletadas[m][f] = true;
            }
        }

        private bool TryParseEncounterKey(string key, out int mundo, out int fase)
        {
            mundo = -1; fase = -1;
            var parts = key.Split('_');
            if (parts.Length < 5) return false;
            if (!int.TryParse(parts[2], out int mNum)) return false;
            mundo = mNum - 1;
            if (parts[3] == "boss")
                fase = FASES_POR_MUNDO - 1;
            else if (parts[3].StartsWith("f") && int.TryParse(parts[3].Substring(1), out int fNum))
                fase = fNum - 1;
            else
                return false;
            return true;
        }

        // ── ESTADO 1: Desbloqueos ──────────────────────────────────────────────

        public bool IsMundoDesbloqueado(int mundo)
        {
            if (mundo == 0) return true;
            if (_fasesCompletadas == null) return false;
            return _fasesCompletadas[mundo - 1][FASES_POR_MUNDO - 1];
        }

        // ── ESTADO 1: RefreshMundos ────────────────────────────────────────────

        private void RefreshMundos()
        {
            for (int i = 0; i < MUNDOS; i++)
            {
                bool desbloqueado = IsMundoDesbloqueado(i);

                if (_mundoNombres != null && i < _mundoNombres.Length && _mundoNombres[i] != null)
                    _mundoNombres[i].text = MUNDO_NOMBRES[i];

                if (_progresoTexts != null && i < _progresoTexts.Length && _progresoTexts[i] != null)
                {
                    int completadas = 0;
                    if (_fasesCompletadas != null)
                        foreach (var b in _fasesCompletadas[i]) if (b) completadas++;
                    _progresoTexts[i].text = desbloqueado
                        ? $"{completadas}/{FASES_POR_MUNDO} fases"
                        : "Bloqueado";
                }

                if (_lockedOverlays != null && i < _lockedOverlays.Length && _lockedOverlays[i] != null)
                    _lockedOverlays[i].SetActive(!desbloqueado);

                if (_mundoSelBordes != null && i < _mundoSelBordes.Length && _mundoSelBordes[i] != null)
                {
                    var c = _mundoSelBordes[i].color;
                    c.a = (i == _mundoSeleccionado) ? 1f : 0f;
                    _mundoSelBordes[i].color = c;
                }

                if (_dots != null && i < _dots.Length && _dots[i] != null)
                {
                    var c = _dots[i].color;
                    c.a = (i == _mundoSeleccionado) ? 1f : 0.35f;
                    _dots[i].color = c;
                }
            }
        }

        private IEnumerator CentrarScrollInicial()
        {
            yield return null;
            yield return null;

            if (_scrollMundos == null) yield break;

            float t = 0f;
            float start = _scrollMundos.horizontalNormalizedPosition;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / 0.3f);
                p = 1f - (1f - p) * (1f - p);
                _scrollMundos.horizontalNormalizedPosition = Mathf.Lerp(start, 0f, p);
                yield return null;
            }
            _scrollMundos.horizontalNormalizedPosition = 0f;
        }

        // ── ESTADO 1: Input mundos ─────────────────────────────────────────────

        public void OnMundoClick(int mundo)
        {
            if (!IsMundoDesbloqueado(mundo))
            {
                if (_popupBloqueado != null) _popupBloqueado.SetActive(true);
                Debug.Log($"[CampaignController] Mundo {mundo + 1} bloqueado.");
                return;
            }

            _mundoSeleccionado = mundo;
            RefreshMundos();
            OpenPanelFases(mundo);
        }

        // ── ESTADO 2: PanelFases init ──────────────────────────────────────────

        private IEnumerator InicializarPanelFases()
        {
            // Esperar un frame para que el layout calcule rect.width
            yield return null;
            yield return null;

            if (_panelFasesRT == null) yield break;

            _panelFasesAncho = _panelFasesRT.rect.width;
            if (_panelFasesAncho <= 0f) _panelFasesAncho = 614f; // fallback 48% de 1280

            // Posicionar fuera de pantalla a la derecha
            var pos = _panelFasesRT.anchoredPosition;
            _panelFasesRT.anchoredPosition = new Vector2(_panelFasesAncho, pos.y);
        }

        // ── ESTADO 2: Abrir / Cerrar PanelFases ───────────────────────────────

        public void OpenPanelFases(int mundoIndex)
        {
            if (_panelFasesRT == null) return;

            if (_tituloMundoFases != null)
                _tituloMundoFases.text = MUNDO_NOMBRES[mundoIndex];

            RefreshNodosFase(mundoIndex);

            if (_panelFasesAbierto)
            {
                // Ya abierto — solo actualizar contenido sin reanimar
                return;
            }

            StartCoroutine(AnimarPanelFases(abrir: true));
            _panelFasesAbierto = true;
        }

        public void ClosePanelFases()
        {
            if (_panelFasesRT == null) return;
            StartCoroutine(AnimarPanelFases(abrir: false));
            _panelFasesAbierto = false;
        }

        private IEnumerator AnimarPanelFases(bool abrir)
        {
            if (_panelFasesRT == null) yield break;

            // Si el ancho todavía no se ha calculado, esperar
            if (_panelFasesAncho <= 0f)
            {
                yield return null;
                _panelFasesAncho = _panelFasesRT.rect.width;
                if (_panelFasesAncho <= 0f) _panelFasesAncho = 614f;
            }

            float duracion   = 0.2f;
            float tiempo     = 0f;
            Vector2 posInicio = _panelFasesRT.anchoredPosition;
            float xDestino   = abrir ? 0f : _panelFasesAncho;
            Vector2 posDestino = new Vector2(xDestino, posInicio.y);

            while (tiempo < duracion)
            {
                tiempo += Time.deltaTime;
                float t       = Mathf.Clamp01(tiempo / duracion);
                float tSmooth = t * t * (3f - 2f * t); // SmoothStep
                _panelFasesRT.anchoredPosition = Vector2.Lerp(posInicio, posDestino, tSmooth);
                yield return null;
            }
            _panelFasesRT.anchoredPosition = posDestino;
        }

        // ── ESTADO 2: Refresh nodos ────────────────────────────────────────────

        private void RefreshNodosFase(int mundoIndex)
        {
            for (int i = 0; i < FASES_POR_MUNDO; i++)
            {
                bool completada   = _fasesCompletadas != null && _fasesCompletadas[mundoIndex][i];
                bool desbloqueada = IsFaseDesbloqueada(mundoIndex, i, _dificultadActual);

                // Color del nodo
                if (_nodoFaseImages != null && i < _nodoFaseImages.Length && _nodoFaseImages[i] != null)
                {
                    if (completada)
                        _nodoFaseImages[i].color = new Color(0.09f, 0.40f, 0.20f);   // #166534 verde
                    else if (desbloqueada)
                        _nodoFaseImages[i].color = new Color(0.12f, 0.08f, 0.21f);   // #1E1535 morado
                    else
                        _nodoFaseImages[i].color = new Color(0.05f, 0.05f, 0.05f);   // #0D0D0D oscuro
                }

                // Interactable
                if (_btnsFase != null && i < _btnsFase.Length && _btnsFase[i] != null)
                    _btnsFase[i].interactable = desbloqueada;

                // Lock icon
                if (_lockIconsFase != null && i < _lockIconsFase.Length && _lockIconsFase[i] != null)
                    _lockIconsFase[i].SetActive(!desbloqueada);

                // Estrellas
                if (_estrellasTexts != null && i < _estrellasTexts.Length && _estrellasTexts[i] != null)
                {
                    if (completada)
                    {
                        _estrellasTexts[i].text  = "* * *";
                        _estrellasTexts[i].color = new Color(0.98f, 0.80f, 0.08f); // #FACC15
                    }
                    else
                    {
                        _estrellasTexts[i].text  = "- - -";
                        _estrellasTexts[i].color = new Color(0.40f, 0.40f, 0.40f);
                    }
                }

                // Drop garantizado + energía
                if (_dropTexts != null && i < _dropTexts.Length && _dropTexts[i] != null)
                {
                    string encounterKey = BuildEncounterKey(mundoIndex, i, _dificultadActual);
                    string drop         = GetDropGarantizado(encounterKey);
                    _dropTexts[i].text  = "Drop: " + drop;
                }
            }
        }

        // ── ESTADO 2: Helpers catálogo ─────────────────────────────────────────

        public string BuildEncounterKey(int mundo, int fase, string dif)
        {
            string worldStr = $"mundo_{mundo + 1}";
            string phaseStr = fase < FASES_POR_MUNDO - 1 ? $"f{fase + 1}" : "boss";
            return $"campaign_{worldStr}_{phaseStr}_{dif}";
        }

        private string GetDropGarantizado(string encounterKey)
        {
            if (_encounterById == null) return "-";
            if (!_encounterById.TryGetValue(encounterKey, out var enc)) return "-";
            if (!string.IsNullOrEmpty(enc.drop_gear_slot)) return enc.drop_gear_slot;
            if (!string.IsNullOrEmpty(enc.drop_tipo))      return enc.drop_tipo;
            return "Equipo";
        }

        // ── ESTADO 2: Input fases ──────────────────────────────────────────────

        public void OnFaseClick(int faseIndex)
        {
            _faseSeleccionada = faseIndex;
            Debug.Log($"[Campaign] Fase seleccionada: {faseIndex} — PanelBatalla en S22c");
            // TODO S22c: abrir PanelBatalla
        }

        // ── ESTADO 3: Stubs (S22c) ─────────────────────────────────────────────

        public void OnConfirmarEquipo()
        {
            Debug.Log("[CampaignController] TODO S22c: OnConfirmarEquipo");
        }

        public void TryEnterBatalla()
        {
            Debug.Log("[CampaignController] TODO S22c: TryEnterBatalla");
        }

        // ── Bind botones ───────────────────────────────────────────────────────

        private void BindButtons()
        {
            if (_btnVolverMain != null)
                _btnVolverMain.onClick.AddListener(() =>
                {
                    if (UIManager.Instance != null) UIManager.Instance.NavigateBack();
                    else UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
                });

            if (_popupBloqueado != null)
            {
                var btnCerrar = _popupBloqueado.GetComponentInChildren<Button>();
                if (btnCerrar != null)
                    btnCerrar.onClick.AddListener(() => _popupBloqueado.SetActive(false));
            }

            if (_btnCerrarFases != null)
                _btnCerrarFases.onClick.AddListener(ClosePanelFases);

            // Botones mundo
            if (_btnsMundo != null)
            {
                for (int i = 0; i < _btnsMundo.Length; i++)
                {
                    int idx = i;
                    if (_btnsMundo[i] != null)
                        _btnsMundo[i].onClick.AddListener(() => OnMundoClick(idx));
                }
            }

            // Botones fase
            if (_btnsFase != null)
            {
                for (int i = 0; i < _btnsFase.Length; i++)
                {
                    int idx = i;
                    if (_btnsFase[i] != null)
                        _btnsFase[i].onClick.AddListener(() => OnFaseClick(idx));
                }
            }
        }

        // ── Catálogos ──────────────────────────────────────────────────────────

        private void LoadEncounterCatalog()
        {
            var ta = Resources.Load<TextAsset>("Data/encounter_catalog");
            if (ta == null) { Debug.LogWarning("[CampaignController] encounter_catalog.json no encontrado."); return; }
            try
            {
                int skipped = 0;
                var settings = new JsonSerializerSettings
                {
                    Error = (_, args) => { args.ErrorContext.Handled = true; skipped++; }
                };
                var root = JsonConvert.DeserializeObject<EncounterCatalogRoot>(ta.text, settings);
                _encounterById = new Dictionary<string, EncounterEntry>();
                if (root?.encounters != null)
                    foreach (var e in root.encounters)
                        if (e != null && !string.IsNullOrEmpty(e.encounterId))
                            _encounterById[e.encounterId] = e;
                if (skipped > 0)
                    Debug.LogWarning($"[CampaignController] encounter_catalog: {skipped} campo(s) ignorado(s) por formato inesperado.");
            }
            catch (System.Exception ex) { Debug.LogError($"[CampaignController] Error parsing encounter_catalog: {ex.Message}"); }
        }

        private void LoadEnemyCatalog()
        {
            var ta = Resources.Load<TextAsset>("Data/enemy_catalog");
            if (ta == null) { Debug.LogWarning("[CampaignController] enemy_catalog.json no encontrado."); return; }
            try
            {
                var root = JsonConvert.DeserializeObject<EnemyCatalogRoot>(ta.text);
                _enemyById = new Dictionary<string, EnemyCatalogEntry>();
                if (root?.enemies != null)
                    foreach (var e in root.enemies)
                        if (!string.IsNullOrEmpty(e.enemyId))
                            _enemyById[e.enemyId] = e;
            }
            catch (System.Exception ex) { Debug.LogError($"[CampaignController] Error parsing enemy_catalog: {ex.Message}"); }
        }

        // ── IsFaseDesbloqueada ─────────────────────────────────────────────────

        public bool IsFaseDesbloqueada(int mundo, int fase, string dif)
        {
            if (mundo == 0 && fase == 0 && dif == "normal") return true;
            if (_fasesCompletadas == null) return false;

            switch (dif)
            {
                case "dificil":
                    for (int f = 0; f < FASES_POR_MUNDO; f++)
                        if (!_fasesCompletadas[mundo][f]) return false;
                    if (fase == 0) return true;
                    return _fasesCompletadas[mundo][fase - 1];

                case "heroica":
                    if (fase == 0) return _fasesCompletadas[mundo][FASES_POR_MUNDO - 1];
                    return _fasesCompletadas[mundo][fase - 1];

                default: // normal
                    if (fase == 0)
                        return mundo == 0 || _fasesCompletadas[mundo - 1][FASES_POR_MUNDO - 1];
                    return _fasesCompletadas[mundo][fase - 1];
            }
        }

        // ── BuildEnemyTeam (S22c) ──────────────────────────────────────────────

        public EnemyInstance[] BuildEnemyTeam(string encounterKey)
        {
            if (_encounterById != null && _encounterById.TryGetValue(encounterKey, out var enc)
                && enc.enemies != null && enc.enemies.Count > 0)
            {
                var result = new List<EnemyInstance>();
                foreach (var eid in enc.enemies)
                {
                    EnemyCatalogEntry edata = null;
                    if (_enemyById != null) _enemyById.TryGetValue(eid, out edata);
                    result.Add(new EnemyInstance
                    {
                        enemyId  = eid,
                        nombre   = edata?.name_es ?? eid,
                        elemento = edata?.element ?? "none",
                        hpMax    = 500,
                        hpActual = 500,
                        atk      = 120,
                        def      = 60,
                        spd      = edata?.spd ?? 80,
                        estaVivo = true,
                    });
                }
                return result.ToArray();
            }
            Debug.LogWarning($"[CampaignController] Encounter '{encounterKey}' no encontrado — placeholder.");
            return new[] { new EnemyInstance { enemyId = "placeholder", nombre = "Esbirro", elemento = "none",
                                               hpMax = 500, hpActual = 500, atk = 100, def = 50, spd = 80,
                                               estaVivo = true } };
        }
    }
}
