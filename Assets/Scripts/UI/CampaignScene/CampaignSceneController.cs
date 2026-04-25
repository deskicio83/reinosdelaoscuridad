using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;
using ReinoOscuridad.Systems;
using ReinoOscuridad.Utils;

namespace ReinoOscuridad.UI.Campaign
{
    /// Controlador de CampaignScene.
    /// ESTADO 1 (S22a): Scroll horizontal de 7 mundos.
    /// ESTADO 2 (S22b): PanelFases slide-in desde derecha.
    /// ESTADO 3 (S22c): PanelBatalla con selección de equipo y entrada a combate.
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

        // ── UI refs — ESTADO 3: PanelBatalla ──────────────────────────────────

        [SerializeField] private GameObject    _panelBatalla;
        [SerializeField] private TMP_Text      _mundoFaseText;
        [SerializeField] private TMP_Text      _dificultadText;
        [SerializeField] private Button        _btnVolverBatalla;
        [SerializeField] private Button[]      _heroSlots;          // [4]
        [SerializeField] private Image[]       _slotPortraits;      // [4]
        [SerializeField] private TMP_Text[]    _slotLabels;         // [4]
        [SerializeField] private TMP_Text      _tituloEquipo;
        [SerializeField] private GameObject[]  _enemyPreviews;      // [3]
        [SerializeField] private TMP_Text[]    _enemyNombres;       // [3]
        [SerializeField] private TMP_Text[]    _enemyNiveles;       // [3]
        [SerializeField] private Button        _btnBatallar;
        [SerializeField] private TMP_Text      _sinEnergiaText;
        [SerializeField] private RectTransform _contentEsbirros;

        // ── RewardPanel ───────────────────────────────────────────────────────

        [SerializeField] private GameObject _rewardPanelPrefab;

        // ── Estado interno ─────────────────────────────────────────────────────

        private int    _mundoSeleccionado   = -1;
        private int    _faseSeleccionada    = -1;
        private string _dificultadActual    = "normal";
        private bool   _panelFasesAbierto   = false;
        private float  _panelFasesAncho     = 0f;

        private bool[][] _fasesCompletadas;
        private bool[]   _mundosRecompensaReclamada;

        private string[]       _equipoSlots   = new string[4];
        private HeroInstance[] _lastTeamBuilt;
        private HeroInstance[] _lastTeamUsado;

        // ── Catálogos ──────────────────────────────────────────────────────────

        private Dictionary<string, EncounterEntry>    _encounterById;
        private Dictionary<string, EnemyCatalogEntry> _enemyById;
        private Dictionary<string, HeroCatalogEntry>  _heroById;

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

        private class HeroCatalogRoot { public List<HeroCatalogEntry> heroes; }
        private class HeroCatalogEntry
        {
            public string heroId;
            public string element;
            public string displayName_es;
        }

        // ── Ciclo de vida ──────────────────────────────────────────────────────

        private void Awake()
        {
            LoadEncounterCatalog();
            LoadEnemyCatalog();
            LoadHeroCatalog();
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

            CheckCombatReturn();

            Debug.Log("[CampaignController] ESTADO 1+2+3 — scroll mundos + panel fases + panel batalla listos.");
        }

        // ── Retorno de CombatScene ─────────────────────────────────────────────

        private void CheckCombatReturn()
        {
            var result = CombatSceneData.LastResult;
            if (result == null) return;

            if (result.victoria) MarcarFaseCompletada();

            // Cargar prefab desde Resources si no está cableado
            var prefab = _rewardPanelPrefab
                ?? Resources.Load<GameObject>("Prefabs/UI/RewardPanel");

            if (prefab != null)
            {
                var panelGO = Instantiate(prefab);
                var rp = panelGO.GetComponent<RewardPanel>();
                rp?.Show(result, _lastTeamUsado,
                         OnRepetirCombate, OnSiguienteFase, OnVolverAlMapa);
            }
            else
            {
                Debug.LogWarning("[CampaignController] RewardPanel prefab no encontrado.");
            }

            CombatSceneData.ClearLastResult();
        }

        private void MarcarFaseCompletada()
        {
            var pds = PlayerDataSystem.Instance;
            if (pds == null) return;
            var pd = pds.GetPlayerData();
            if (pd?.campana == null) return;
            string key = pd.campana.ultimoEncuentroIntentado;
            if (string.IsNullOrEmpty(key)) return;
            if (pd.campana.fasesCompletadas == null)
                pd.campana.fasesCompletadas = new List<string>();
            if (!pd.campana.fasesCompletadas.Contains(key))
            {
                pd.campana.fasesCompletadas.Add(key);
                pd.campana.ultimoEncuentroIntentado = null;
                pds.UpdatePlayerData(pd);
                pds.MarkDirty();
                Debug.Log($"[CampaignController] Fase completada: {key}");
            }
        }

        // ── Callbacks post-combate ─────────────────────────────────────────────

        private void OnRepetirCombate()
        {
            var ctx = CombatSceneData.LastContext;
            if (ctx != null && TryParseEncounterKey(ctx.encounterID, out int mundo, out int fase))
            {
                _mundoSeleccionado = mundo;
                _faseSeleccionada  = fase;
                RefreshMundos();
            }
            AbrirPanelBatalla();
        }

        private void OnSiguienteFase()
        {
            var ctx = CombatSceneData.LastContext;
            if (ctx != null && TryParseEncounterKey(ctx.encounterID, out int mundo, out int fase))
            {
                _mundoSeleccionado = mundo;
                _faseSeleccionada  = Mathf.Clamp(fase + 1, 0, FASES_POR_MUNDO - 1);
                RefreshMundos();
                OpenPanelFases(mundo);
            }
            AbrirPanelBatalla();
        }

        private void OnVolverAlMapa()
        {
            var ctx = CombatSceneData.LastContext;
            if (ctx != null && TryParseEncounterKey(ctx.encounterID, out int mundo, out _))
            {
                _mundoSeleccionado = mundo;
                RefreshMundos();
                OpenPanelFases(mundo);
            }
            ClosePanelBatalla();
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
            yield return null;
            yield return null;

            if (_panelFasesRT == null) yield break;

            _panelFasesAncho = _panelFasesRT.rect.width;
            if (_panelFasesAncho <= 0f) _panelFasesAncho = 614f; // fallback 48% de 1280

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
                return;

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

            if (_panelFasesAncho <= 0f)
            {
                yield return null;
                _panelFasesAncho = _panelFasesRT.rect.width;
                if (_panelFasesAncho <= 0f) _panelFasesAncho = 614f;
            }

            float duracion    = 0.2f;
            float tiempo      = 0f;
            Vector2 posInicio  = _panelFasesRT.anchoredPosition;
            float xDestino    = abrir ? 0f : _panelFasesAncho;
            Vector2 posDestino = new Vector2(xDestino, posInicio.y);

            while (tiempo < duracion)
            {
                tiempo += Time.deltaTime;
                float t       = Mathf.Clamp01(tiempo / duracion);
                float tSmooth = t * t * (3f - 2f * t);
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

                if (_nodoFaseImages != null && i < _nodoFaseImages.Length && _nodoFaseImages[i] != null)
                {
                    if (completada)
                        _nodoFaseImages[i].color = new Color(0.09f, 0.40f, 0.20f);
                    else if (desbloqueada)
                        _nodoFaseImages[i].color = new Color(0.12f, 0.08f, 0.21f);
                    else
                        _nodoFaseImages[i].color = new Color(0.05f, 0.05f, 0.05f);
                }

                if (_btnsFase != null && i < _btnsFase.Length && _btnsFase[i] != null)
                    _btnsFase[i].interactable = desbloqueada;

                if (_lockIconsFase != null && i < _lockIconsFase.Length && _lockIconsFase[i] != null)
                    _lockIconsFase[i].SetActive(!desbloqueada);

                if (_estrellasTexts != null && i < _estrellasTexts.Length && _estrellasTexts[i] != null)
                {
                    if (completada)
                    {
                        _estrellasTexts[i].text  = "* * *";
                        _estrellasTexts[i].color = new Color(0.98f, 0.80f, 0.08f);
                    }
                    else
                    {
                        _estrellasTexts[i].text  = "- - -";
                        _estrellasTexts[i].color = new Color(0.40f, 0.40f, 0.40f);
                    }
                }

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
            AbrirPanelBatalla();
        }

        // ── ESTADO 3: PanelBatalla — abrir / cerrar ────────────────────────────

        public void AbrirPanelBatalla()
        {
            if (_panelBatalla == null) return;

            // Header
            if (_mundoFaseText != null)
                _mundoFaseText.text = "Mundo " + (_mundoSeleccionado + 1) + " - " +
                    (_faseSeleccionada < FASES_POR_MUNDO - 1
                        ? "Fase " + (_faseSeleccionada + 1)
                        : "BOSS");

            if (_dificultadText != null)
            {
                string dif = _dificultadActual;
                _dificultadText.text = dif.Length > 0
                    ? char.ToUpper(dif[0]) + dif.Substring(1)
                    : dif;
            }

            // Resetear equipo (slots posicionales)
            _equipoSlots = new string[4];
            RefreshSlotsEquipo();

            // Cargar lastTeam si existe, respetando posiciones
            var pds = PlayerDataSystem.Instance;
            if (pds != null)
            {
                var pd = pds.GetPlayerData();
                if (pd?.lastTeam != null)
                {
                    int si = 0;
                    foreach (var heroId in pd.lastTeam)
                    {
                        if (!string.IsNullOrEmpty(heroId) && si < 4)
                            _equipoSlots[si++] = heroId;
                    }
                }
            }
            RefreshSlotsEquipo();

            // Cargar enemigos del encuentro
            string key     = BuildEncounterKey(_mundoSeleccionado, _faseSeleccionada, _dificultadActual);
            var    enemies = BuildEnemyTeam(key);
            PoblarEnemyPreviews(enemies);

            // Poblar scroll de esbirros
            PoblarScrollEsbirros();

            // Estado inicial del botón
            ActualizarBtnBatallar();

            _panelBatalla.SetActive(true);
        }

        public void ClosePanelBatalla()
        {
            if (_panelBatalla == null) return;
            _panelBatalla.SetActive(false);

            if (_contentEsbirros != null)
            {
                var children = new List<Transform>();
                foreach (Transform child in _contentEsbirros)
                    children.Add(child);
                foreach (var child in children)
                {
                    child.SetParent(null);
                    Destroy(child.gameObject);
                }
            }
        }

        // ── ESTADO 3: Enemigos ─────────────────────────────────────────────────

        private void PoblarEnemyPreviews(EnemyInstance[] enemies)
        {
            if (_enemyPreviews == null) return;

            for (int i = 0; i < _enemyPreviews.Length; i++)
                if (_enemyPreviews[i] != null) _enemyPreviews[i].SetActive(false);

            int count = Mathf.Min(enemies.Length, _enemyPreviews.Length);
            for (int i = 0; i < count; i++)
            {
                if (_enemyPreviews[i] == null) continue;
                _enemyPreviews[i].SetActive(true);

                if (_enemyNombres != null && i < _enemyNombres.Length && _enemyNombres[i] != null)
                    _enemyNombres[i].text = enemies[i].nombre;

                if (_enemyNiveles != null && i < _enemyNiveles.Length && _enemyNiveles[i] != null)
                    _enemyNiveles[i].text = "Nv. " + enemies[i].nivel;

                var imgs = _enemyPreviews[i].GetComponentsInChildren<Image>();
                if (imgs.Length > 1)
                {
                    var sp = PlaceholderAssets.GetEnemySprite("normal");
                    if (sp != null) imgs[1].sprite = sp;
                }
            }
        }

        // ── ESTADO 3: Scroll de esbirros ──────────────────────────────────────

        private void PoblarScrollEsbirros()
        {
            if (_contentEsbirros == null) return;

            var children = new List<Transform>();
            foreach (Transform child in _contentEsbirros)
                children.Add(child);
            foreach (var child in children)
            {
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            var pds = PlayerDataSystem.Instance;
            if (pds == null) return;

            var pd = pds.GetPlayerData();
            if (pd?.heroes == null) return;

            foreach (var heroEntry in pd.heroes)
                CrearHeroMiniCard(heroEntry);
        }

        private void CrearHeroMiniCard(PlayerHeroData heroData)
        {
            var card = new GameObject("MiniCard_" + heroData.heroId);
            card.transform.SetParent(_contentEsbirros, false);

            var le             = card.AddComponent<LayoutElement>();
            le.preferredWidth  = 64f;
            le.preferredHeight = 64f;

            var img   = card.AddComponent<Image>();
            bool enEquipo = System.Array.IndexOf(_equipoSlots, heroData.heroId) >= 0;
            img.color = enEquipo
                ? new Color(0.30f, 0.13f, 0.49f)
                : new Color(0.10f, 0.10f, 0.18f);

            card.AddComponent<Button>();

            // Portrait
            var portraitGO = new GameObject("Portrait");
            portraitGO.transform.SetParent(card.transform, false);
            var prt   = portraitGO.AddComponent<Image>();
            var prtRT = portraitGO.GetComponent<RectTransform>();
            prtRT.anchorMin = new Vector2(0.05f, 0.22f);
            prtRT.anchorMax = new Vector2(0.95f, 0.95f);
            prtRT.offsetMin = prtRT.offsetMax = Vector2.zero;
            string elem = GetHeroElemento(heroData.heroId);
            var sp      = PlaceholderAssets.GetHeroPortrait(elem);
            if (sp != null) prt.sprite = sp;

            // Nivel (esquina superior izquierda)
            var nivelGO  = new GameObject("Nivel");
            nivelGO.transform.SetParent(card.transform, false);
            var nivelTMP = nivelGO.AddComponent<TextMeshProUGUI>();
            nivelTMP.text      = heroData.level.ToString();
            nivelTMP.fontSize  = 8f;
            nivelTMP.fontStyle = FontStyles.Bold;
            nivelTMP.color     = new Color(0.98f, 0.80f, 0.08f);
            var nivelRT = nivelGO.GetComponent<RectTransform>();
            nivelRT.anchorMin = new Vector2(0.02f, 0.76f);
            nivelRT.anchorMax = new Vector2(0.50f, 0.98f);
            nivelRT.offsetMin = nivelRT.offsetMax = Vector2.zero;

            // Estrellas (parte inferior)
            var starsGO  = new GameObject("Estrellas");
            starsGO.transform.SetParent(card.transform, false);
            var starsTMP = starsGO.AddComponent<TextMeshProUGUI>();
            starsTMP.text      = new string('*', Mathf.Clamp(heroData.stars, 0, 6));
            starsTMP.fontSize  = 7f;
            starsTMP.alignment = TextAlignmentOptions.Center;
            starsTMP.color     = new Color(0.98f, 0.80f, 0.08f);
            var starsRT = starsGO.GetComponent<RectTransform>();
            starsRT.anchorMin = new Vector2(0.02f, 0.02f);
            starsRT.anchorMax = new Vector2(0.98f, 0.20f);
            starsRT.offsetMin = starsRT.offsetMax = Vector2.zero;

            // Click
            string heroId = heroData.heroId;
            card.GetComponent<Button>().onClick.AddListener(() => ToggleHeroEnEquipo(heroId));
        }

        // ── ESTADO 3: Lógica equipo ────────────────────────────────────────────

        public void ToggleHeroEnEquipo(string heroId)
        {
            // Buscar si ya ocupa un slot (posición fija — se vacía ese slot)
            int slotOcupado = -1;
            for (int s = 0; s < 4; s++)
                if (_equipoSlots[s] == heroId) { slotOcupado = s; break; }

            if (slotOcupado >= 0)
            {
                _equipoSlots[slotOcupado] = null;  // vaciar solo ese slot
            }
            else
            {
                // Buscar primer slot libre
                int libre = -1;
                for (int s = 0; s < 4; s++)
                    if (_equipoSlots[s] == null) { libre = s; break; }
                if (libre < 0) return;  // equipo lleno
                _equipoSlots[libre] = heroId;
            }

            RefreshSlotsEquipo();
            RefreshMiniCards();
            ActualizarBtnBatallar();
        }

        private void RefreshSlotsEquipo()
        {
            int count = 0;
            foreach (var s in _equipoSlots) if (s != null) count++;
            if (_tituloEquipo != null)
                _tituloEquipo.text = "Mi Equipo (" + count + "/4)";

            for (int i = 0; i < 4; i++)
            {
                if (_heroSlots == null || i >= _heroSlots.Length || _heroSlots[i] == null) continue;

                if (_equipoSlots[i] != null)
                {
                    string heroId = _equipoSlots[i];
                    string elem   = GetHeroElemento(heroId);
                    var sp        = PlaceholderAssets.GetHeroPortrait(elem);

                    if (_slotPortraits != null && i < _slotPortraits.Length && _slotPortraits[i] != null)
                    {
                        if (sp != null) _slotPortraits[i].sprite = sp;
                        _slotPortraits[i].color = Color.white;
                    }
                    if (_slotLabels != null && i < _slotLabels.Length && _slotLabels[i] != null)
                        _slotLabels[i].text = GetHeroNombre(heroId);

                    _heroSlots[i].image.color = new Color(0.18f, 0.10f, 0.30f);
                }
                else
                {
                    if (_slotPortraits != null && i < _slotPortraits.Length && _slotPortraits[i] != null)
                    {
                        _slotPortraits[i].sprite = null;
                        _slotPortraits[i].color  = new Color(0.18f, 0.18f, 0.28f, 0.5f);
                    }
                    if (_slotLabels != null && i < _slotLabels.Length && _slotLabels[i] != null)
                        _slotLabels[i].text = "+";

                    _heroSlots[i].image.color = new Color(0.10f, 0.10f, 0.18f);
                }
            }
        }

        private void RefreshMiniCards()
        {
            if (_contentEsbirros == null) return;
            foreach (Transform child in _contentEsbirros)
            {
                string heroId = child.name.Replace("MiniCard_", "");
                bool enEquipo = System.Array.IndexOf(_equipoSlots, heroId) >= 0;
                var img = child.GetComponent<Image>();
                if (img != null)
                    img.color = enEquipo
                        ? new Color(0.30f, 0.13f, 0.49f)
                        : new Color(0.10f, 0.10f, 0.18f);
            }
        }

        public void OnHeroSlotClick(int slotIndex)
        {
            if (_equipoSlots == null || slotIndex >= 4 || _equipoSlots[slotIndex] == null) return;
            _equipoSlots[slotIndex] = null;
            RefreshSlotsEquipo();
            RefreshMiniCards();
            ActualizarBtnBatallar();
        }

        private void ActualizarBtnBatallar()
        {
            bool tieneEnergia = EconomySystem.Instance != null
                && EconomySystem.Instance.CurrentEnergy >= 6;
            int  equipoCount   = 0;
            foreach (var s in _equipoSlots) if (s != null) equipoCount++;
            bool tieneEquipo  = equipoCount > 0;

            if (_btnBatallar != null)
                _btnBatallar.interactable = tieneEquipo && tieneEnergia;

            if (_sinEnergiaText != null)
                _sinEnergiaText.gameObject.SetActive(!tieneEnergia);
        }

        public async void TryEnterBatalla()
        {
            // Recoger slots no vacíos en orden
            var equipo = new List<string>();
            foreach (var s in _equipoSlots) if (s != null) equipo.Add(s);
            if (equipo.Count == 0) return;

            if (EconomySystem.Instance == null || EconomySystem.Instance.CurrentEnergy < 6)
            {
                ActualizarBtnBatallar();
                return;
            }

            // Construir equipo
            _lastTeamBuilt = new HeroInstance[equipo.Count];
            for (int i = 0; i < equipo.Count; i++)
            {
                string heroId = equipo[i];
                _lastTeamBuilt[i] = GearSystem.Instance != null
                    ? GearSystem.Instance.BuildCombatInstance(heroId)
                    : HeroProgressionSystem.Instance?.BuildHeroInstance(heroId, GetHeroNivel(heroId))
                      ?? new HeroInstance { heroId = heroId, nivel = 1, hpMax = 1000, hpActual = 1000, estaVivo = true };
            }
            _lastTeamUsado = _lastTeamBuilt;

            // Construir CombatContext
            string key     = BuildEncounterKey(_mundoSeleccionado, _faseSeleccionada, _dificultadActual);
            var    enemies = BuildEnemyTeam(key);

            // Guardar lastTeam + ultimoEncuentroIntentado en PlayerData
            var pds = PlayerDataSystem.Instance;
            if (pds != null)
            {
                var pd = pds.GetPlayerData();
                pd.lastTeam = equipo.ToArray();
                if (pd.campana == null) pd.campana = new CampaignProgressData();
                pd.campana.ultimoEncuentroIntentado = key;
                pds.UpdatePlayerData(pd);
                pds.MarkDirty();
            }

            CombatSceneData.PendingContext = new CombatContext
            {
                encounterID    = key,
                callerScene    = "CampaignScene",
                combatMode     = "campaign",
                playerTeam     = _lastTeamBuilt,
                enemyTeam      = enemies,
                maldicionActiva = false,
            };

            Debug.Log($"[CampaignController] TryEnterBatalla → {key} · equipo: {string.Join(",", equipo)}");

            EconomySystem.Instance.ConsumeEnergy(6);

            // Cerrar paneles al instante sin animación (el FadeOut a negro los cubre)
            if (_panelBatalla != null) _panelBatalla.SetActive(false);
            if (_panelFasesRT != null) _panelFasesRT.gameObject.SetActive(false);
            _panelFasesAbierto = false;

            if (UIManager.Instance != null)
                await UIManager.Instance.NavigateTo("CombatScene");
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("CombatScene");
        }

        // ── ESTADO 3: Helpers de catálogo ─────────────────────────────────────

        private string GetHeroElemento(string heroId)
        {
            if (_heroById != null && _heroById.TryGetValue(heroId, out var h) && !string.IsNullOrEmpty(h.element))
                return h.element.ToLower();
            return "oscuridad";
        }

        private string GetHeroNombre(string heroId)
        {
            if (_heroById != null && _heroById.TryGetValue(heroId, out var h) && !string.IsNullOrEmpty(h.displayName_es))
                return h.displayName_es;
            return heroId;
        }

        private int GetHeroNivel(string heroId)
        {
            var pds = PlayerDataSystem.Instance;
            if (pds == null) return 1;
            var pd = pds.GetPlayerData();
            if (pd?.heroes == null) return 1;
            foreach (var h in pd.heroes)
                if (h.heroId == heroId) return h.level;
            return 1;
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

            // Botones panel batalla
            if (_btnVolverBatalla != null)
                _btnVolverBatalla.onClick.AddListener(ClosePanelBatalla);

            if (_btnBatallar != null)
                _btnBatallar.onClick.AddListener(TryEnterBatalla);

            if (_heroSlots != null)
            {
                for (int i = 0; i < _heroSlots.Length; i++)
                {
                    int idx = i;
                    if (_heroSlots[i] != null)
                        _heroSlots[i].onClick.AddListener(() => OnHeroSlotClick(idx));
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
                    Debug.LogWarning($"[CampaignController] encounter_catalog: {skipped} campo(s) ignorado(s).");
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

        private void LoadHeroCatalog()
        {
            var ta = Resources.Load<TextAsset>("Data/hero_catalog");
            if (ta == null) { Debug.LogWarning("[CampaignController] hero_catalog.json no encontrado."); return; }
            try
            {
                var root = JsonConvert.DeserializeObject<HeroCatalogRoot>(ta.text);
                _heroById = new Dictionary<string, HeroCatalogEntry>();
                if (root?.heroes != null)
                    foreach (var h in root.heroes)
                        if (!string.IsNullOrEmpty(h.heroId))
                            _heroById[h.heroId] = h;
            }
            catch (System.Exception ex) { Debug.LogError($"[CampaignController] Error parsing hero_catalog: {ex.Message}"); }
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

        // ── BuildEnemyTeam ─────────────────────────────────────────────────────

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
