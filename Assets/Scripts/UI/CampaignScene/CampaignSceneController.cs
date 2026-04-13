using System;
using System.Collections.Generic;
using System.IO;
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
    /// Gestiona la selección de mundo/dificultad/fase, desbloqueos y lanzamiento de combate.
    /// NO es singleton — se instancia una vez por carga de CampaignScene.
    public class CampaignSceneController : MonoBehaviour
    {
        // ── Constantes ─────────────────────────────────────────────────────────

        private const int    MUNDOS          = 7;
        private const int    FASES_POR_MUNDO = 7; // f1–f6 + boss
        private const string DIF_NORMAL      = "normal";
        private const string DIF_DIFICIL     = "dificil";
        private const string DIF_HEROICA     = "heroica";

        private static readonly string[] DIFICULTADES = { DIF_NORMAL, DIF_DIFICIL, DIF_HEROICA };

        // ── UI refs (asignadas por SetupCampaignScene) ─────────────────────────

        [SerializeField] private Transform    _contenedorMundos;
        [SerializeField] private Transform    _contenedorFases;
        [SerializeField] private Transform    _contenedorDificultad;
        [SerializeField] private Button       _btnVolver;

        // Panel de confirmación de fase
        [SerializeField] private GameObject   _panelConfirmacion;
        [SerializeField] private TMP_Text     _txtFaseNombre;
        [SerializeField] private TMP_Text     _txtEquipo;
        [SerializeField] private TMP_Text     _txtEnergyCost;
        [SerializeField] private Button       _btnConfirmarBatalla;
        [SerializeField] private Button       _btnCancelarConfirmacion;

        // ── Estado de selección ────────────────────────────────────────────────

        private int    _mundoSeleccionado      = 0;
        private string _dificultadSeleccionada = DIF_NORMAL;
        private int    _faseSeleccionada        = 0;  // pendiente de confirmar

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

            // Fallback offline: si los sistemas no están (preview directo en Editor),
            // funciona con datos de campaña vacíos — solo Fase 0 Mundo 1 desbloqueada.
            if (PlayerDataSystem.Instance == null)
                Debug.LogWarning("[CampaignController] PlayerDataSystem no disponible — modo preview (datos offline).");
        }

        private void Start()
        {
            EnsureCampanaData();
            CheckCombatReturn();

            if (_contenedorMundos != null && _contenedorFases != null)
                BuildUI();
            else
                Debug.LogWarning("[CampaignController] Refs de UI nulas — ¿ejecutaste Tools → 6. Setup CampaignScene?");

            if (_btnVolver != null)
                _btnVolver.onClick.AddListener(() => UIManager.Instance?.NavigateBack());

            if (_btnConfirmarBatalla != null)
                _btnConfirmarBatalla.onClick.AddListener(ConfirmarBatalla);

            if (_btnCancelarConfirmacion != null)
                _btnCancelarConfirmacion.onClick.AddListener(CerrarConfirmacion);

            if (_panelConfirmacion != null)
                _panelConfirmacion.SetActive(false);

            Debug.Log($"[CampaignController] CampaignScene lista — mundo:{_mundoSeleccionado} dif:{_dificultadSeleccionada}");
        }

        // ── API pública — desbloqueos ──────────────────────────────────────────

        /// true si la fase puede jugarse.
        /// Índices de base 0: mundo 0 = Mundo 1 del catálogo, fase 6 = Boss.
        public bool IsFaseDesbloqueada(int mundo, int fase, string dif)
        {
            // Primera fase del juego: siempre accesible
            if (mundo == 0 && fase == 0 && dif == DIF_NORMAL) return true;

            var completadas = GetCampanaData()?.fasesCompletadas;
            if (completadas == null) return false;

            switch (dif)
            {
                case DIF_DIFICIL:
                    // Requiere todas las fases normal de este mundo completadas
                    for (int f = 0; f < FASES_POR_MUNDO; f++)
                        if (!completadas.Contains(BuildEncounterKey(mundo, f, DIF_NORMAL))) return false;
                    if (fase == 0) return true;
                    return completadas.Contains(BuildEncounterKey(mundo, fase - 1, DIF_DIFICIL));

                case DIF_HEROICA:
                    // Requiere todas las fases dificil de este mundo completadas
                    for (int f = 0; f < FASES_POR_MUNDO; f++)
                        if (!completadas.Contains(BuildEncounterKey(mundo, f, DIF_DIFICIL))) return false;
                    if (fase == 0) return true;
                    return completadas.Contains(BuildEncounterKey(mundo, fase - 1, DIF_HEROICA));

                default: // normal
                    if (fase == 0)
                    {
                        if (mundo == 0) return true;
                        // Nuevo mundo: requiere boss del mundo anterior en normal
                        return completadas.Contains(BuildEncounterKey(mundo - 1, FASES_POR_MUNDO - 1, DIF_NORMAL));
                    }
                    return completadas.Contains(BuildEncounterKey(mundo, fase - 1, DIF_NORMAL));
            }
        }

        // ── API pública — clave de encuentro ───────────────────────────────────

        /// Genera la clave de encuentro compatible con encounter_catalog.json.
        /// fase 0–5 → "f1"–"f6"; fase 6 → "boss".
        public string BuildEncounterKey(int mundo, int fase, string dif)
        {
            string worldStr = $"mundo_{mundo + 1}";
            string phaseStr = fase < FASES_POR_MUNDO - 1 ? $"f{fase + 1}" : "boss";
            return $"campaign_{worldStr}_{phaseStr}_{dif}";
        }

        // ── API pública — equipos de combate ───────────────────────────────────

        /// Construye el equipo del jugador con stats finales (héroe + gear).
        /// Devuelve los primeros 4 héroes del roster. Array vacío si no hay héroes.
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

        /// Construye el equipo enemigo a partir de la clave de encuentro.
        /// Devuelve un enemigo placeholder si la clave no existe en el catálogo.
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

        /// Añade la clave al historial de fases completadas (sin duplicados).
        public void MarcarFaseCompletada(string encounterKey)
        {
            var campana = EnsureCampanaData();
            if (campana == null) return;
            if (!campana.fasesCompletadas.Contains(encounterKey))
                campana.fasesCompletadas.Add(encounterKey);
            PlayerDataSystem.Instance?.MarkDirty();
        }

        // ── Interacción con UI ─────────────────────────────────────────────────

        /// Selecciona un mundo y refresca los nodos de fases.
        public void SelectMundo(int mundo)
        {
            _mundoSeleccionado = Mathf.Clamp(mundo, 0, MUNDOS - 1);
            RefreshFaseNodes();
        }

        /// Selecciona una dificultad y refresca los nodos.
        public void SelectDificultad(string dif)
        {
            if (Array.IndexOf(DIFICULTADES, dif) < 0) return;
            _dificultadSeleccionada = dif;
            RefreshFaseNodes();
        }

        /// El jugador pulsa un nodo de fase. Muestra el panel de confirmación.
        public void OnFaseClick(int mundo, int fase, string dif)
        {
            if (!IsFaseDesbloqueada(mundo, fase, dif))
            {
                Debug.Log($"[CampaignController] Fase bloqueada — mundo:{mundo} fase:{fase} dif:{dif}");
                return;
            }

            _mundoSeleccionado      = mundo;
            _faseSeleccionada       = fase;
            _dificultadSeleccionada = dif;

            AbrirConfirmacion(mundo, fase, dif);
        }

        // ── Panel de confirmación ──────────────────────────────────────────────

        private void AbrirConfirmacion(int mundo, int fase, string dif)
        {
            if (_panelConfirmacion == null) { ConfirmarBatalla(); return; } // sin panel → combate directo

            string key     = BuildEncounterKey(mundo, fase, dif);
            bool   esBoss  = fase == FASES_POR_MUNDO - 1;
            string nombre  = esBoss ? $"JEFE — Mundo {mundo + 1}" : $"Mundo {mundo + 1} · Fase {fase + 1}";
            int    cost    = GetEnergyCost(key);

            // Nombre de la fase
            if (_txtFaseNombre != null)
                _txtFaseNombre.text = $"{nombre}\n<size=14><color=#aaaaaa>{dif.ToUpper()}</color></size>";

            // Coste de energía
            if (_txtEnergyCost != null)
                _txtEnergyCost.text = $"Energia: {cost} stamina";

            // Equipo que irá a la batalla
            if (_txtEquipo != null)
            {
                var pd = PlayerDataSystem.Instance?.GetPlayerData();
                if (pd?.heroes != null && pd.heroes.Count > 0)
                {
                    var nombres = new System.Text.StringBuilder("Equipo:\n");
                    int limit = Mathf.Min(pd.heroes.Count, 4);
                    for (int i = 0; i < limit; i++)
                        nombres.AppendLine($"  · {pd.heroes[i].heroId} (Nv.{pd.heroes[i].level})");
                    _txtEquipo.text = nombres.ToString();
                }
                else
                {
                    _txtEquipo.text = "Equipo: sin héroes configurados";
                }
            }

            _panelConfirmacion.SetActive(true);
        }

        private void ConfirmarBatalla()
        {
            string key  = BuildEncounterKey(_mundoSeleccionado, _faseSeleccionada, _dificultadSeleccionada);
            int    cost = GetEnergyCost(key);

            var eco = EconomySystem.Instance;
            if (eco != null && !eco.ConsumeEnergy(cost))
            {
                Debug.Log("[CampaignController] Energía insuficiente.");
                CerrarConfirmacion();
                return;
            }

            var campana = EnsureCampanaData();
            if (campana != null) campana.ultimoEncuentroIntentado = key;

            CombatSceneData.PendingContext = new CombatContext
            {
                encounterID     = key,
                callerScene     = "CampaignScene",
                combatMode      = "campaign",
                playerTeam      = BuildPlayerTeam(),
                enemyTeam       = BuildEnemyTeam(key),
                maldicionActiva = false,
                elementoBoss    = null
            };

            CerrarConfirmacion();
            UIManager.Instance?.NavigateTo("CombatScene");
        }

        private void CerrarConfirmacion()
        {
            if (_panelConfirmacion != null)
                _panelConfirmacion.SetActive(false);
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
                mundoActual               = 0,
                faseActual                = 0,
                dificultadActual          = DIF_NORMAL,
                fasesCompletadas          = new List<string>(),
                ultimoEncuentroIntentado  = null
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

        private void CheckCombatReturn()
        {
            var result  = CombatSceneData.LastResult;
            var campana = GetCampanaData();
            if (result == null || campana == null) return;

            if (result.victoria && !string.IsNullOrEmpty(campana.ultimoEncuentroIntentado))
            {
                MarcarFaseCompletada(campana.ultimoEncuentroIntentado);
                campana.ultimoEncuentroIntentado = null;
            }

            CombatSceneData.SetResult(null);
        }

        // ── Carga de catálogos ─────────────────────────────────────────────────

        private void LoadCatalogs()
        {
            _encounterById = new Dictionary<string, EncounterEntry>();
            _enemyById     = new Dictionary<string, EnemyCatalogEntry>();

            string basePath = Path.Combine(Application.dataPath, "Data");
            LoadEncounterCatalog(Path.Combine(basePath, "encounter_catalog.json"));
            LoadEnemyCatalog(Path.Combine(basePath, "enemy_catalog.json"));
        }

        private void LoadEncounterCatalog(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[CampaignController] encounter_catalog.json no encontrado: {path}");
                return;
            }

            // Algunos encuentros tienen "enemies" como string en vez de array.
            // El callback de error salta esos campos y continúa deserializando el resto.
            var settings = new JsonSerializerSettings
            {
                Error = (_, args) => { args.ErrorContext.Handled = true; }
            };

            var root = JsonConvert.DeserializeObject<EncounterCatalogRoot>(File.ReadAllText(path), settings);
            if (root?.encounters == null) return;
            foreach (var e in root.encounters)
                if (e.encounterId != null)
                    _encounterById[e.encounterId] = e;
            Debug.Log($"[CampaignController] {_encounterById.Count} encuentros cargados.");
        }

        private void LoadEnemyCatalog(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[CampaignController] enemy_catalog.json no encontrado: {path}");
                return;
            }
            var root = JsonConvert.DeserializeObject<EnemyCatalogRoot>(File.ReadAllText(path));
            if (root?.enemies == null) return;
            foreach (var e in root.enemies)
                _enemyById[e.enemyId] = e;
            Debug.Log($"[CampaignController] {_enemyById.Count} enemigos cargados.");
        }

        // ── Construcción de UI ─────────────────────────────────────────────────

        private void BuildUI()
        {
            BuildMundoTabs();
            BuildDificultadTabs();
            RefreshFaseNodes();
        }

        private void BuildMundoTabs()
        {
            if (_contenedorMundos == null) return;

            for (int m = 0; m < MUNDOS; m++)
            {
                int capturedIdx = m;

                var go  = new GameObject($"BtnMundo{m + 1}");
                go.transform.SetParent(_contenedorMundos, false);

                var btn = go.AddComponent<Button>();
                go.AddComponent<Image>().color = Color.gray;

                var rt       = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(80f, 40f);

                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(go.transform, false);
                var tmp = labelGO.AddComponent<TextMeshProUGUI>();
                tmp.text      = $"M{m + 1}";
                tmp.fontSize  = 18f;
                tmp.alignment = TextAlignmentOptions.Center;

                var lrt       = labelGO.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;

                btn.onClick.AddListener(() => SelectMundo(capturedIdx));
            }
        }

        private void BuildDificultadTabs()
        {
            if (_contenedorDificultad == null) return;

            foreach (string dif in DIFICULTADES)
            {
                string capturedDif = dif;

                var go  = new GameObject($"BtnDif_{dif}");
                go.transform.SetParent(_contenedorDificultad, false);

                var btn = go.AddComponent<Button>();
                go.AddComponent<Image>().color = Color.gray;

                var rt       = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(110f, 36f);

                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(go.transform, false);
                var tmp = labelGO.AddComponent<TextMeshProUGUI>();
                tmp.text      = dif[0].ToString().ToUpper() + dif.Substring(1);
                tmp.fontSize  = 16f;
                tmp.alignment = TextAlignmentOptions.Center;

                var lrt       = labelGO.GetComponent<RectTransform>();
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

            foreach (Transform child in _contenedorFases)
                Destroy(child.gameObject);

            for (int f = 0; f < FASES_POR_MUNDO; f++)
            {
                int    capturedFase  = f;
                int    capturedMundo = _mundoSeleccionado;
                string capturedDif   = _dificultadSeleccionada;
                bool   esBoss        = f == FASES_POR_MUNDO - 1;
                bool   desbloqueada  = IsFaseDesbloqueada(_mundoSeleccionado, f, _dificultadSeleccionada);
                string label         = esBoss ? $"Boss M{_mundoSeleccionado + 1}" : $"F{f + 1}";

                var go  = new GameObject($"FaseNode_{f}");
                go.transform.SetParent(_contenedorFases, false);

                var btn = go.AddComponent<Button>();
                go.AddComponent<Image>().color = desbloqueada
                    ? (esBoss ? new Color(1f, 0.85f, 0f) : Color.white)
                    : new Color(0.3f, 0.3f, 0.3f);

                var rt       = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(110f, 60f);

                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(go.transform, false);
                var tmp = labelGO.AddComponent<TextMeshProUGUI>();
                tmp.text      = label;
                tmp.fontSize  = 15f;
                tmp.color     = desbloqueada ? Color.white : new Color(0.5f, 0.5f, 0.5f);
                tmp.alignment = TextAlignmentOptions.Center;

                var lrt       = labelGO.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;

                btn.onClick.AddListener(() => OnFaseClick(capturedMundo, capturedFase, capturedDif));
            }
        }
    }
}
