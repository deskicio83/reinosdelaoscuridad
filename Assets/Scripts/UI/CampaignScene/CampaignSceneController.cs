using System;
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

        // Prefabs de overlays (asignados por SetupCampaignScene si existen)
        [SerializeField] private GameObject   _battlePrepPrefab;
        [SerializeField] private GameObject   _rewardPrefab;

        // ── Estado de selección ────────────────────────────────────────────────

        private int    _mundoSeleccionado      = 0;
        private string _dificultadSeleccionada = DIF_NORMAL;
        private int    _faseSeleccionada        = 0;
        private string _pendingEncounterKey;

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
            CheckCombatReturn();

            if (_contenedorMundos != null && _contenedorFases != null)
                BuildUI();
            else
                Debug.LogWarning("[CampaignController] Refs de UI nulas — ¿ejecutaste Tools → 6. Setup CampaignScene?");

            if (_btnVolver != null)
                _btnVolver.onClick.AddListener(() => UIManager.Instance?.NavigateBack());

            Debug.Log($"[CampaignController] CampaignScene lista — mundo:{_mundoSeleccionado} dif:{_dificultadSeleccionada}");
        }

        // ── API pública — desbloqueos ──────────────────────────────────────────

        /// true si la fase puede jugarse.
        /// Índices de base 0: mundo 0 = Mundo 1 del catálogo, fase 6 = Boss.
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
                    {
                        if (mundo == 0) return true;
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

        public void SelectMundo(int mundo)
        {
            _mundoSeleccionado = Mathf.Clamp(mundo, 0, MUNDOS - 1);
            RefreshFaseNodes();
        }

        public void SelectDificultad(string dif)
        {
            if (Array.IndexOf(DIFICULTADES, dif) < 0) return;
            _dificultadSeleccionada = dif;
            RefreshFaseNodes();
        }

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

            string key    = BuildEncounterKey(mundo, fase, dif);
            bool   esBoss = fase == FASES_POR_MUNDO - 1;
            string titulo = esBoss
                ? $"JEFE  ·  Mundo {mundo + 1}  ·  {dif.ToUpper()}"
                : $"Mundo {mundo + 1}  ·  Fase {fase + 1}  ·  {dif.ToUpper()}";
            int             cost    = GetEnergyCost(key);
            EnemyInstance[] enemies = BuildEnemyTeam(key);

            _pendingEncounterKey = key;
            var campana = EnsureCampanaData();
            if (campana != null) campana.ultimoEncuentroIntentado = key;

            if (_battlePrepPrefab != null && UIManager.Instance != null)
            {
                BattlePrepPanel.Show(_battlePrepPrefab, titulo, cost, enemies, OnTeamSelected);
            }
            else
            {
                if (UIManager.Instance == null)
                    Debug.LogWarning("[CampaignController] UIManager null — juega desde SampleScene para activar los sistemas.");
                OnTeamSelected(BuildPlayerTeam());
            }
        }

        private void OnTeamSelected(HeroInstance[] team)
        {
            int cost = GetEnergyCost(_pendingEncounterKey);
            var eco  = EconomySystem.Instance;
            if (eco != null && !eco.ConsumeEnergy(cost))
            {
                Debug.Log("[CampaignController] Energía insuficiente.");
                return;
            }

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

        // ── GoToNextFase ───────────────────────────────────────────────────────

        /// TODO(S19): calcular la siguiente fase y lanzarla directamente sin volver al mapa.
        /// Actualmente solo cierra el mapa y el jugador selecciona manualmente.
        public void GoToNextFase()
        {
            Debug.Log("[CampaignController] GoToNextFase — TODO: implementar en S19.");
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

            var lastTeamIds = PlayerDataSystem.Instance?.GetPlayerData()?.lastTeam;
            HeroInstance[] team = RebuildTeam(lastTeamIds);

            CombatSceneData.SetResult(null);

            if (_rewardPrefab != null)
                RewardPanel.Show(_rewardPrefab, result, team, GoToNextFase);
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
            {
                Error = (_, args) => { args.ErrorContext.Handled = true; }
            };

            var root = JsonConvert.DeserializeObject<EncounterCatalogRoot>(ta.text, settings);
            if (root?.encounters == null) return;
            foreach (var e in root.encounters)
                if (e.encounterId != null)
                    _encounterById[e.encounterId] = e;
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

            var campana = GetCampanaData();

            for (int m = 0; m < MUNDOS; m++)
            {
                int capturedIdx = m;

                // Calcular progreso normal del mundo (fases completadas de 7)
                int fasesCompletadasMundo = 0;
                if (campana?.fasesCompletadas != null)
                    for (int f = 0; f < FASES_POR_MUNDO; f++)
                        if (campana.fasesCompletadas.Contains(BuildEncounterKey(m, f, DIF_NORMAL)))
                            fasesCompletadasMundo++;

                var go  = new GameObject($"BtnMundo{m + 1}");
                go.transform.SetParent(_contenedorMundos, false);

                var btn = go.AddComponent<Button>();
                go.AddComponent<Image>().color = fasesCompletadasMundo >= FASES_POR_MUNDO
                    ? new Color(0.20f, 0.45f, 0.20f)   // mundo completado
                    : new Color(0.25f, 0.15f, 0.35f);   // disponible/bloqueado

                var rt       = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(80f, 50f);

                // Nombre del mundo
                var lblGO = new GameObject("Label");
                lblGO.transform.SetParent(go.transform, false);
                var tmp = lblGO.AddComponent<TextMeshProUGUI>();
                tmp.text      = $"M{m + 1}";
                tmp.fontSize  = 16f;
                tmp.color     = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
                var lrt       = lblGO.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 0.45f);
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;

                // Progreso "N/7"
                var progGO = new GameObject("Progreso");
                progGO.transform.SetParent(go.transform, false);
                var progTxt = progGO.AddComponent<TextMeshProUGUI>();
                progTxt.text      = $"{fasesCompletadasMundo}/{FASES_POR_MUNDO}";
                progTxt.fontSize  = 11f;
                progTxt.color     = new Color(0.75f, 0.75f, 0.75f);
                progTxt.alignment = TextAlignmentOptions.Center;
                var prt           = progGO.GetComponent<RectTransform>();
                prt.anchorMin     = new Vector2(0f, 0f);
                prt.anchorMax     = new Vector2(1f, 0.45f);
                prt.offsetMin     = Vector2.zero;
                prt.offsetMax     = Vector2.zero;

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
                go.AddComponent<Image>().color = new Color(0.22f, 0.15f, 0.30f);

                var rt       = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(110f, 36f);

                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(go.transform, false);
                var tmp = labelGO.AddComponent<TextMeshProUGUI>();
                tmp.text      = char.ToUpper(dif[0]) + dif.Substring(1);
                tmp.fontSize  = 16f;
                tmp.color     = Color.white;
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

            for (int ci = _contenedorFases.childCount - 1; ci >= 0; ci--)
            {
                var child = _contenedorFases.GetChild(ci);
                child.SetParent(null);   // desacoplar inmediatamente del HLG
                Destroy(child.gameObject);
            }

            var campana = GetCampanaData();

            for (int f = 0; f < FASES_POR_MUNDO; f++)
            {
                int    capturedFase  = f;
                int    capturedMundo = _mundoSeleccionado;
                string capturedDif   = _dificultadSeleccionada;
                bool   esBoss        = f == FASES_POR_MUNDO - 1;
                bool   desbloqueada  = IsFaseDesbloqueada(_mundoSeleccionado, f, _dificultadSeleccionada);
                string key           = BuildEncounterKey(_mundoSeleccionado, f, _dificultadSeleccionada);
                bool   completada    = campana?.fasesCompletadas?.Contains(key) ?? false;
                string label         = esBoss ? $"Boss M{_mundoSeleccionado + 1}" : $"F{f + 1}";

                Color nodeColor;
                if (!desbloqueada)
                    nodeColor = new Color(0.25f, 0.25f, 0.28f);
                else if (completada)
                    nodeColor = new Color(0.15f, 0.45f, 0.15f);
                else if (esBoss)
                    nodeColor = new Color(0.80f, 0.55f, 0.05f);
                else
                    nodeColor = new Color(0.22f, 0.15f, 0.35f);

                var go  = new GameObject($"FaseNode_{f}");
                go.transform.SetParent(_contenedorFases, false);

                var btn = go.AddComponent<Button>();
                go.AddComponent<Image>().color = nodeColor;
                btn.interactable = desbloqueada;

                var rt       = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(110f, 65f);

                var le = go.AddComponent<LayoutElement>();
                le.preferredWidth  = 110f;
                le.preferredHeight = 65f;

                // Etiqueta principal
                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(go.transform, false);
                var tmp = labelGO.AddComponent<TextMeshProUGUI>();
                tmp.text      = label;
                tmp.fontSize  = 15f;
                tmp.color     = desbloqueada ? Color.white : new Color(0.45f, 0.45f, 0.45f);
                tmp.alignment = TextAlignmentOptions.Center;
                var lrt       = labelGO.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0f, 0.45f);
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(4, 2);
                lrt.offsetMax = new Vector2(-4, -2);

                // Coste de energía
                var costGO = new GameObject("Cost");
                costGO.transform.SetParent(go.transform, false);
                var costTxt = costGO.AddComponent<TextMeshProUGUI>();
                costTxt.text      = desbloqueada ? $"{GetEnergyCost(key)} stam" : "---";
                costTxt.fontSize  = 11f;
                costTxt.color     = new Color(0.5f, 0.75f, 1f);
                costTxt.alignment = TextAlignmentOptions.Center;
                var crt           = costGO.GetComponent<RectTransform>();
                crt.anchorMin     = new Vector2(0f, 0f);
                crt.anchorMax     = new Vector2(1f, 0.45f);
                crt.offsetMin     = new Vector2(4, 2);
                crt.offsetMax     = new Vector2(-4, -2);

                btn.onClick.AddListener(() => OnFaseClick(capturedMundo, capturedFase, capturedDif));
            }
        }
    }
}
