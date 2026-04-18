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
    /// Controlador de CampaignScene — S22a ESTADO 1: Scroll horizontal de 7 mundos.
    /// ESTADO 2 (PanelFases) y ESTADO 3 (PanelBatalla) se añaden en S22b/S22c.
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

        // ── UI refs — ScrollMundos ─────────────────────────────────────────────

        [SerializeField] private ScrollRect    _scrollMundos;       // ScrollRect del scroll horizontal
        [SerializeField] private RectTransform _contentMundos;      // Content (HLG + ContentSizeFitter)
        [SerializeField] private Button[]      _btnsMundo;          // [7] botones de mundo
        [SerializeField] private GameObject[]  _lockedOverlays;     // [7] overlay de bloqueado por mundo
        [SerializeField] private Image[]       _mundoSelBordes;     // [7] borde dorado de seleccionado
        [SerializeField] private TMP_Text[]    _mundoNombres;       // [7] nombre del mundo
        [SerializeField] private TMP_Text[]    _progresoTexts;      // [7] "X/7 fases"
        [SerializeField] private Image[]       _dots;               // [7] indicadores inferiores
        [SerializeField] private GameObject    _popupBloqueado;     // popup "mundo bloqueado"
        [SerializeField] private Button        _btnVolverMain;

        // ── UI refs stub — se usan en S22b / S22c ─────────────────────────────

        [SerializeField] private GameObject _panelFases;            // TODO S22b
        [SerializeField] private GameObject _panelBatalla;          // TODO S22c

        // ── Estado interno ─────────────────────────────────────────────────────

        private int      _mundoSeleccionado = -1;
        private bool[][] _fasesCompletadas;        // [mundo][fase]
        private bool[]   _mundosRecompensaReclamada;

        // ── Catálogos (mantenidos para futura integración S22b) ────────────────

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

            if (_panelFases  != null) _panelFases.SetActive(false);
            if (_panelBatalla != null) _panelBatalla.SetActive(false);
            if (_popupBloqueado != null) _popupBloqueado.SetActive(false);

            Debug.Log("[CampaignController] ESTADO 1 — scroll de mundos listo.");
        }

        // ── Progreso ───────────────────────────────────────────────────────────

        private void CargarProgreso()
        {
            _fasesCompletadas        = new bool[MUNDOS][];
            _mundosRecompensaReclamada = new bool[MUNDOS];

            for (int m = 0; m < MUNDOS; m++)
                _fasesCompletadas[m] = new bool[FASES_POR_MUNDO];

            var pds = PlayerDataSystem.Instance;
            if (pds == null) return;

            var pd = pds.GetPlayerData();
            if (pd?.campana?.fasesCompletadas == null) return;

            foreach (var key in pd.campana.fasesCompletadas)
            {
                // key format: "campaign_mundo_N_fX_dif"  or  "campaign_mundo_N_boss_dif"
                if (!TryParseEncounterKey(key, out int m, out int f)) continue;
                if (m >= 0 && m < MUNDOS && f >= 0 && f < FASES_POR_MUNDO)
                    _fasesCompletadas[m][f] = true;
            }

            // mundosRecompensaReclamada se añadirá a CampaignProgressData en S22b
        }

        /// Parsea "campaign_mundo_N_fX_dif" o "campaign_mundo_N_boss_dif" → (m=N-1, f).
        private bool TryParseEncounterKey(string key, out int mundo, out int fase)
        {
            mundo = -1; fase = -1;
            // Esperado: campaign_mundo_1_f1_normal / campaign_mundo_1_boss_normal
            var parts = key.Split('_');
            if (parts.Length < 5) return false;
            // parts[2] = mundo, parts[3] = mundo_num, parts[4] = "fX" o "boss", rest = dif
            // Corrección: "campaign" "mundo" "1" "f1" "normal"  → indices 0 1 2 3 4
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

        // ── Desbloqueos ────────────────────────────────────────────────────────

        /// Mundo 0 siempre desbloqueado. Mundo N desbloqueado si el boss del mundo N-1 fue completado.
        public bool IsMundoDesbloqueado(int mundo)
        {
            if (mundo == 0) return true;
            if (_fasesCompletadas == null) return false;
            return _fasesCompletadas[mundo - 1][FASES_POR_MUNDO - 1]; // boss completado
        }

        // ── Refresh UI ─────────────────────────────────────────────────────────

        private void RefreshMundos()
        {
            for (int i = 0; i < MUNDOS; i++)
            {
                bool desbloqueado = IsMundoDesbloqueado(i);

                // Nombre
                if (_mundoNombres != null && i < _mundoNombres.Length && _mundoNombres[i] != null)
                    _mundoNombres[i].text = MUNDO_NOMBRES[i];

                // Progreso
                if (_progresoTexts != null && i < _progresoTexts.Length && _progresoTexts[i] != null)
                {
                    int completadas = 0;
                    if (_fasesCompletadas != null)
                        foreach (var b in _fasesCompletadas[i]) if (b) completadas++;
                    _progresoTexts[i].text = desbloqueado ? $"{completadas}/{FASES_POR_MUNDO} fases" : "Bloqueado";
                }

                // Overlay bloqueado
                if (_lockedOverlays != null && i < _lockedOverlays.Length && _lockedOverlays[i] != null)
                    _lockedOverlays[i].SetActive(!desbloqueado);

                // Borde seleccionado (transparente por defecto)
                if (_mundoSelBordes != null && i < _mundoSelBordes.Length && _mundoSelBordes[i] != null)
                {
                    var c = _mundoSelBordes[i].color;
                    c.a = (i == _mundoSeleccionado) ? 1f : 0f;
                    _mundoSelBordes[i].color = c;
                }

                // Dot indicador
                if (_dots != null && i < _dots.Length && _dots[i] != null)
                {
                    var c = _dots[i].color;
                    c.a = (i == _mundoSeleccionado) ? 1f : 0.35f;
                    _dots[i].color = c;
                }
            }
        }

        // ── Centrar scroll en primer mundo ─────────────────────────────────────

        private IEnumerator CentrarScrollInicial()
        {
            // Esperar un frame para que ContentSizeFitter calcule el tamaño
            yield return null;
            yield return null;

            if (_scrollMundos == null) yield break;

            // Ir al inicio (mundo 0)
            float t = 0f;
            float start = _scrollMundos.horizontalNormalizedPosition;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / 0.3f);
                // ease out quad
                p = 1f - (1f - p) * (1f - p);
                _scrollMundos.horizontalNormalizedPosition = Mathf.Lerp(start, 0f, p);
                yield return null;
            }
            _scrollMundos.horizontalNormalizedPosition = 0f;
        }

        // ── Input mundos ───────────────────────────────────────────────────────

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

            Debug.Log($"[CampaignController] Mundo {mundo + 1} seleccionado — TODO S22b: abrir PanelFases.");
        }

        // ── Stubs S22b / S22c ──────────────────────────────────────────────────

        public void OnFaseClick(int fase)
        {
            Debug.Log($"[CampaignController] TODO S22b: OnFaseClick({fase})");
        }

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
                    if (UIManager.Instance != null)
                        UIManager.Instance.NavigateBack();
                    else
                        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
                });

            if (_popupBloqueado != null)
            {
                // El popup tiene un BtnCerrar hijo
                var btnCerrar = _popupBloqueado.GetComponentInChildren<Button>();
                if (btnCerrar != null)
                    btnCerrar.onClick.AddListener(() => _popupBloqueado.SetActive(false));
            }

            // Wiring de los 7 botones de mundo (capture local por closure)
            if (_btnsMundo != null)
            {
                for (int i = 0; i < _btnsMundo.Length; i++)
                {
                    int idx = i;
                    if (_btnsMundo[i] != null)
                        _btnsMundo[i].onClick.AddListener(() => OnMundoClick(idx));
                }
            }
        }

        // ── Catálogos (para uso futuro en S22b) ───────────────────────────────

        private void LoadEncounterCatalog()
        {
            var ta = Resources.Load<TextAsset>("Data/encounter_catalog");
            if (ta == null) { Debug.LogWarning("[CampaignController] encounter_catalog.json no encontrado."); return; }
            try
            {
                var root = JsonConvert.DeserializeObject<EncounterCatalogRoot>(ta.text);
                _encounterById = new Dictionary<string, EncounterEntry>();
                if (root?.encounters != null)
                    foreach (var e in root.encounters)
                        if (!string.IsNullOrEmpty(e.encounterId))
                            _encounterById[e.encounterId] = e;
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

        // ── IsFaseDesbloqueada (para S22b) ─────────────────────────────────────

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
                    // Requiere dificil completo — simplificado como bool array normal
                    if (fase == 0) return _fasesCompletadas[mundo][FASES_POR_MUNDO - 1];
                    return _fasesCompletadas[mundo][fase - 1];

                default: // normal
                    if (fase == 0)
                        return mundo == 0 || _fasesCompletadas[mundo - 1][FASES_POR_MUNDO - 1];
                    return _fasesCompletadas[mundo][fase - 1];
            }
        }

        // ── BuildEnemyTeam (para S22c) ─────────────────────────────────────────

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
