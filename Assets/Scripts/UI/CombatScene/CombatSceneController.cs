using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;
using ReinoOscuridad.Systems;
using ReinoOscuridad.UI.Common;

namespace ReinoOscuridad.UI.Combat
{
    public enum TurnState
    {
        WaitingForInput,
        SelectingTarget,
        ProcessingAction,
        ShowingResult,
        CombatFinished
    }

    /// Controlador de CombatScene — flujo de turno completo.
    /// NO singleton. Se instancia una vez por carga de CombatScene.
    /// Recibe CombatContext desde CombatSceneData (pasarela entre Scenes).
    public class CombatSceneController : MonoBehaviour
    {
        // ── Panel Controles (esquina superior derecha) ─────────────────────

        [Header("Panel Controles")]
        [SerializeField] private TMP_Text _turnoLabel;
        [SerializeField] private Button   _btnModo;       // toggle Auto/Manual
        [SerializeField] private Button   _btnVelocidad;  // toggle x1/x2
        [SerializeField] private Button   _btnPausa;
        [SerializeField] private Button   _btnHuir;

        // ── Zona Enemigos (3 slots dinámicos) ──────────────────────────────

        [Header("Zona Enemigos (3 slots)")]
        [SerializeField] private GameObject[] _enemySlots;    // GameObjects raíz de cada slot
        [SerializeField] private Image[]      _enemyHPFills;  // Relleno barra HP
        [SerializeField] private TMP_Text[]   _enemyHPTexts;  // "8000/8000"
        [SerializeField] private Button[]     _enemySlotBtns; // para selección de objetivo

        // ── Zona Equipo (4 cartas fijas) ────────────────────────────────────

        [Header("Zona Equipo (4 slots)")]
        [SerializeField] private Button[]   _heroCards;       // 4
        [SerializeField] private TMP_Text[] _heroNames;       // 4
        [SerializeField] private Image[]    _heroHPFills;     // 4
        [SerializeField] private Image[]    _turnIndicators;  // 4 — barra dorada top

        // ── Zona Habilidades (3 círculos bajo barra de turno) ──────────────

        [Header("Zona Habilidades (3 circulos)")]
        [SerializeField] private Button[] _abilityCircles;   // 3 — bajo BarraOrdenTurno

        // ── Zona Conjuros (2×5) ─────────────────────────────────────────────

        [Header("Zona Conjuros (10 slots)")]
        [SerializeField] private Button[] _conjuroSlots; // 10

        // ── Tooltip Panel ───────────────────────────────────────────────────

        [Header("Tooltip Panel")]
        [SerializeField] private GameObject _tooltipPanel;
        [SerializeField] private TMP_Text   _tooltipText;
        [SerializeField] private Button     _btnUsarTooltip;
        [SerializeField] private Button     _btnCerrarTooltip;

        // ── Barra de Turno ──────────────────────────────────────────────────

        [Header("Barra de Turno")]
        [SerializeField] private Transform _listaRetratos;

        // ── Result Panel ────────────────────────────────────────────────────

        [Header("Result Panel")]
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TMP_Text   _resultTitleText;
        [SerializeField] private TMP_Text   _resultGradeText;
        [SerializeField] private TMP_Text   _resultXPText;
        [SerializeField] private TMP_Text   _resultDropsText;
        [SerializeField] private Button     _btnContinuar;
        [SerializeField] private Button     _btnReintentar;

        // ── Colores de HP ───────────────────────────────────────────────────

        private static readonly Color HP_HEALTHY  = new Color(0.08f, 0.50f, 0.24f); // #15803D
        private static readonly Color HP_MEDIUM   = new Color(0.85f, 0.47f, 0.03f); // #D97706
        private static readonly Color HP_CRITICAL = new Color(0.86f, 0.15f, 0.15f); // #DC2626

        // ── Estado interno ─────────────────────────────────────────────────

        private CombatContext _ctx;
        private CombatSystem  _combatSystem;
        private TurnState     _state = TurnState.WaitingForInput;
        private bool          _autoMode;

        private List<TurnSlot> _turnOrder;
        private int            _currentSlotIndex;
        private int            _turnoActual = 1;
        private int            _selectedAbilityIndex = -1;

        private Action _tooltipConfirmAction;

        // ── Inicio ─────────────────────────────────────────────────────────

        private async void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[CombatScene] GameManager no encontrado. ¿Se cargó BootScene primero?");
                return;
            }

            _ctx = CombatSceneData.PendingContext;
            if (_ctx == null)
            {
                Debug.LogError("[CombatScene] CombatContext es null — volviendo a MainMenuScene.");
                if (UIManager.Instance != null)
                    await UIManager.Instance.NavigateTo("MainMenuScene");
                return;
            }

            if (LoadingScreen.Instance != null)
                LoadingScreen.Instance.Show("preparando combate");

            _combatSystem = GameManager.Instance.GetSystem<CombatSystem>();
            if (_combatSystem == null)
                Debug.LogWarning("[CombatScene] CombatSystem no encontrado — modo degradado.");

            BuildTurnOrder();
            RefreshBarraTurno();
            RefreshEnemyZone();
            RefreshHeroZone();
            UpdateTurnoLabel();
            DisableAbilityCircles();

            if (_tooltipPanel != null)
                _tooltipPanel.SetActive(false);

            if (_resultPanel != null)
                _resultPanel.SetActive(false);

            BindButtons();

            await System.Threading.Tasks.Task.Delay(400);
            if (LoadingScreen.Instance != null)
                LoadingScreen.Instance.Hide();

            Debug.Log($"[CombatScene] Combate iniciado — encuentro: {_ctx.encounterID} — modo: {_ctx.combatMode}");

            if (_autoMode)
                StartCoroutine(AutoCombatCoroutine());
            else
                StartCoroutine(ManualCombatCoroutine());
        }

        // ── API pública ────────────────────────────────────────────────────

        public void SetAutoMode(bool auto)
        {
            _autoMode = auto;
            UpdateModoButtonVisual();
            Debug.Log($"[CombatScene] Modo Auto: {_autoMode}");
        }

        public void SelectAbility(int abilityIndex)
        {
            if (_state != TurnState.WaitingForInput) return;
            _selectedAbilityIndex = abilityIndex;
            _state = TurnState.SelectingTarget;
            Debug.Log($"[CombatScene] Habilidad {abilityIndex} seleccionada — elige objetivo.");
        }

        public void SelectTarget(int targetIndex)
        {
            if (_state != TurnState.SelectingTarget) return;
            if (_ctx?.enemyTeam == null || targetIndex < 0 || targetIndex >= _ctx.enemyTeam.Length) return;
            if (!_ctx.enemyTeam[targetIndex].estaVivo) return;

            ProcessHeroAction(GetCurrentHero(), _ctx.enemyTeam[targetIndex]);
        }

        public void SkipToResult()
        {
            if (!_autoMode) return;
            StopAllCoroutines();
            var result = _combatSystem != null
                ? _combatSystem.ProcessCombat(_ctx)
                : new CombatResult { victoria = false, drops = Array.Empty<string>(), gradoObtenido = "C" };
            FinalizarCombate(result);
        }

        // ── Tooltip ────────────────────────────────────────────────────────

        public void ShowTooltip(string description, Action onConfirm)
        {
            if (_tooltipPanel == null)
            {
                onConfirm?.Invoke();
                return;
            }
            if (_tooltipText != null)  _tooltipText.text = description;
            if (_btnUsarTooltip != null) _btnUsarTooltip.gameObject.SetActive(onConfirm != null);
            _tooltipConfirmAction = onConfirm;
            _tooltipPanel.SetActive(true);
        }

        public void HideTooltip()
        {
            if (_tooltipPanel != null) _tooltipPanel.SetActive(false);
            _tooltipConfirmAction = null;
        }

        private void OnUsarTooltip()
        {
            var action = _tooltipConfirmAction;
            HideTooltip();
            action?.Invoke();
        }

        // ── Habilidades ────────────────────────────────────────────────────

        private void UpdateAbilityCirclesForHero(int heroIndex)
        {
            if (_abilityCircles == null) return;
            bool valid = heroIndex >= 0
                         && _ctx?.playerTeam != null
                         && heroIndex < _ctx.playerTeam.Length;

            for (int i = 0; i < _abilityCircles.Length; i++)
            {
                if (_abilityCircles[i] == null) continue;
                _abilityCircles[i].interactable = valid;

                var lbl = _abilityCircles[i].GetComponentInChildren<TMP_Text>(true);
                if (lbl == null) continue;

                if (valid && _ctx.playerTeam[heroIndex].habilidadesEquipadas != null
                          && i < _ctx.playerTeam[heroIndex].habilidadesEquipadas.Length)
                {
                    var habId = _ctx.playerTeam[heroIndex].habilidadesEquipadas[i];
                    lbl.text = string.IsNullOrEmpty(habId)
                        ? $"H{i + 1}"
                        : habId.Substring(0, Mathf.Min(3, habId.Length)).ToUpper();
                }
                else
                {
                    lbl.text = $"H{i + 1}";
                }
            }
        }

        private void DisableAbilityCircles()
        {
            if (_abilityCircles == null) return;
            foreach (var btn in _abilityCircles)
                if (btn != null) btn.interactable = false;
        }

        private string GetAbilityDescription(int abilityIndex)
        {
            if (_turnOrder == null || _currentSlotIndex >= _turnOrder.Count)
                return $"Habilidad {abilityIndex + 1}";

            var slot = _turnOrder[_currentSlotIndex];
            if (!slot.IsHero || _ctx?.playerTeam == null)
                return $"Habilidad {abilityIndex + 1}";

            var hero = _ctx.playerTeam[slot.HeroIndex];
            string habId = hero.habilidadesEquipadas != null && abilityIndex < hero.habilidadesEquipadas.Length
                ? hero.habilidadesEquipadas[abilityIndex]
                : null;

            return string.IsNullOrEmpty(habId)
                ? $"Habilidad {abilityIndex + 1}\n(Sin equipar)"
                : $"{habId}\n(Descripción pendiente de catálogo)";
        }

        // ── Orden de turno ─────────────────────────────────────────────────

        private void BuildTurnOrder()
        {
            _turnOrder = new List<TurnSlot>();

            if (_ctx.playerTeam != null)
                for (int i = 0; i < _ctx.playerTeam.Length; i++)
                    if (_ctx.playerTeam[i].estaVivo)
                        _turnOrder.Add(new TurnSlot { IsHero = true, HeroIndex = i });

            if (_ctx.enemyTeam != null)
                for (int i = 0; i < _ctx.enemyTeam.Length; i++)
                    if (_ctx.enemyTeam[i].estaVivo)
                        _turnOrder.Add(new TurnSlot { IsHero = false, EnemyIndex = i });

            _turnOrder.Sort((a, b) =>
            {
                int sA = a.IsHero ? _ctx.playerTeam[a.HeroIndex].spd : _ctx.enemyTeam[a.EnemyIndex].spd;
                int sB = b.IsHero ? _ctx.playerTeam[b.HeroIndex].spd : _ctx.enemyTeam[b.EnemyIndex].spd;
                if (sB != sA) return sB.CompareTo(sA);
                return a.IsHero ? -1 : 1;
            });

            _currentSlotIndex = 0;
            _turnoActual      = 1;
        }

        // ── Auto Combat ────────────────────────────────────────────────────

        private IEnumerator AutoCombatCoroutine()
        {
            if (_combatSystem == null)
            {
                Debug.LogError("[CombatScene] AutoMode requiere CombatSystem.");
                yield break;
            }

            var result = _combatSystem.ProcessCombat(_ctx);
            RefreshEnemyZone();
            RefreshHeroZone();

            yield return new WaitForSeconds(1.2f);
            FinalizarCombate(result);
        }

        // ── Manual Combat ──────────────────────────────────────────────────

        private IEnumerator ManualCombatCoroutine()
        {
            const int MAX_TURNS = 50;

            while (_turnoActual <= MAX_TURNS
                   && AnyAlive(_ctx.playerTeam)
                   && AnyAlive(_ctx.enemyTeam))
            {
                var slot = _turnOrder[_currentSlotIndex];

                if (!SlotIsAlive(slot))
                {
                    AdvanceTurnSlot();
                    yield return null;
                    continue;
                }

                HighlightActiveTurn(slot);
                UpdateTurnoLabel();

                if (slot.IsHero)
                {
                    var hero = _ctx.playerTeam[slot.HeroIndex];

                    if (hero.efectosActivos != null && hero.efectosActivos.Contains("Stun"))
                    {
                        Debug.Log($"[CombatScene] {hero.heroId} aturdido — turno saltado.");
                        DisableAbilityCircles();
                        yield return new WaitForSeconds(0.4f);
                    }
                    else
                    {
                        UpdateAbilityCirclesForHero(slot.HeroIndex);

                        _state = TurnState.WaitingForInput;
                        yield return new WaitUntil(() =>
                            _state == TurnState.ProcessingAction ||
                            _state == TurnState.CombatFinished);

                        DisableAbilityCircles();
                        if (_state == TurnState.CombatFinished) yield break;
                        yield return new WaitForSeconds(0.3f);
                    }
                }
                else
                {
                    _state = TurnState.ProcessingAction;
                    var enemy  = _ctx.enemyTeam[slot.EnemyIndex];
                    var target = PickFirstAlive(_ctx.playerTeam);

                    if (target != null && _combatSystem != null)
                    {
                        var dmg = _combatSystem.CalculateDamageEnemyAttack(enemy, target);
                        ApplyDamageToHero(target, dmg.dañoFinal);
                        RefreshHeroZone();

                        Debug.Log($"[CombatScene] {enemy.enemyId} → {target.heroId}: {dmg.dañoFinal}" +
                                  (dmg.fueEsquivado ? " [ESQ]" : "") + (dmg.fueCritico ? " [CRIT]" : ""));
                    }

                    yield return new WaitForSeconds(0.6f);
                }

                if (!AnyAlive(_ctx.playerTeam) || !AnyAlive(_ctx.enemyTeam)) break;

                AdvanceTurnSlot();
            }

            FinalizarCombate(new CombatResult
            {
                victoria      = AnyAlive(_ctx.playerTeam) && !AnyAlive(_ctx.enemyTeam),
                danoTotal     = 0,
                drops         = Array.Empty<string>(),
                xpGanada      = 0,
                trofeosDelta  = 0,
                gradoObtenido = _turnoActual <= 5 ? "S" : _turnoActual <= 10 ? "A" : _turnoActual <= 20 ? "B" : "C"
            });
        }

        // ── Acción del jugador ─────────────────────────────────────────────

        private void ProcessHeroAction(HeroInstance hero, EnemyInstance target)
        {
            if (hero == null || target == null) return;

            float mult = _selectedAbilityIndex == 1 ? 1.8f
                       : _selectedAbilityIndex == 2 ? 1.2f
                       : 1f;

            if (_combatSystem != null)
            {
                var dmg = _combatSystem.CalculateDamage(hero, target, mult);
                ApplyDamageToEnemy(target, dmg.dañoFinal);
                RefreshEnemyZone();

                Debug.Log($"[CombatScene] {hero.heroId} hab:{_selectedAbilityIndex} → {target.enemyId}: {dmg.dañoFinal}" +
                          (dmg.fueEsquivado ? " [ESQ]" : "") + (dmg.fueCritico ? " [CRIT]" : "") +
                          (dmg.fueElementalVentaja ? " [VENTAJA]" : "") +
                          (dmg.fueElementalDesventaja ? " [DESV]" : ""));
            }

            _selectedAbilityIndex = -1;
            _state = TurnState.ProcessingAction;
        }

        // ── Finalización ────────────────────────────────────────────────────

        private void FinalizarCombate(CombatResult result)
        {
            _state = TurnState.CombatFinished;
            StopAllCoroutines();
            DisableAbilityCircles();
            HideTooltip();

            CombatSceneData.SetResult(result);

            EventBus.Publish(new CombatCompletedData
            {
                encounterId = _ctx?.encounterID ?? "",
                victory     = result.victoria,
                expGained   = result.xpGanada,
                goldGained  = 0
            });

            if (InputBlocker.Instance != null)
                InputBlocker.Instance.Show(49);

            ShowResultPanel(result);
        }

        private void ShowResultPanel(CombatResult result)
        {
            if (_resultPanel == null)
            {
                Debug.LogWarning("[CombatScene] ResultPanel no asignado.");
                _ = UIManager.Instance?.NavigateTo(_ctx?.callerScene ?? "MainMenuScene");
                return;
            }

            _resultPanel.SetActive(true);

            if (_resultTitleText != null)
                _resultTitleText.text = result.victoria ? "VICTORIA" : "DERROTA";
            if (_resultGradeText != null)
            {
                if (result.victoria)
                {
                    int stars = CalcularEstrellas(_ctx.playerTeam);
                    _resultGradeText.text  = stars == 3 ? "* * *" : stars == 2 ? "* * -" : "* - -";
                    _resultGradeText.color = new Color(0.98f, 0.80f, 0.08f);
                }
                else
                {
                    _resultGradeText.text = "";
                }
            }
            if (_resultXPText != null)
                _resultXPText.text = $"+{result.xpGanada} XP";
            if (_resultDropsText != null)
                _resultDropsText.text = result.drops != null && result.drops.Length > 0
                    ? "Recompensas:\n* " + string.Join("\n* ", result.drops)
                    : "Recompensas:\nSin drops";

            _state = TurnState.ShowingResult;
        }

        private async void OnContinuarPressed()
        {
            if (InputBlocker.Instance != null) InputBlocker.Instance.Hide();
            if (UIManager.Instance != null)
                await UIManager.Instance.NavigateTo(_ctx?.callerScene ?? "MainMenuScene");
        }

        private async void OnReintentarPressed()
        {
            if (InputBlocker.Instance != null) InputBlocker.Instance.Hide();
            if (UIManager.Instance != null)
                await UIManager.Instance.NavigateTo(_ctx?.callerScene ?? "MainMenuScene");
        }

        // ── Refresh UI ─────────────────────────────────────────────────────

        private void RefreshBarraTurno()
        {
            if (_listaRetratos == null || _turnOrder == null) return;

            // Destruir retratos anteriores
            for (int i = _listaRetratos.childCount - 1; i >= 0; i--)
                Destroy(_listaRetratos.GetChild(i).gameObject);

            // Mostrar los próximos N slots del orden de turno (máx 8)
            int count = Mathf.Min(_turnOrder.Count, 8);
            for (int i = 0; i < count; i++)
            {
                int    slotIdx = (_currentSlotIndex + i) % _turnOrder.Count;
                var    slot    = _turnOrder[slotIdx];
                string label;
                Color  color;

                if (slot.IsHero && _ctx.playerTeam != null && slot.HeroIndex < _ctx.playerTeam.Length)
                {
                    var h = _ctx.playerTeam[slot.HeroIndex];
                    label = string.IsNullOrEmpty(h.heroId) ? "?" : h.heroId.Substring(0, Mathf.Min(3, h.heroId.Length)).ToUpper();
                    color = h.estaVivo ? new Color(0.20f, 0.45f, 0.75f) : new Color(0.35f, 0.35f, 0.35f);
                }
                else if (!slot.IsHero && _ctx.enemyTeam != null && slot.EnemyIndex < _ctx.enemyTeam.Length)
                {
                    var e = _ctx.enemyTeam[slot.EnemyIndex];
                    label = string.IsNullOrEmpty(e.enemyId) ? "E" : e.enemyId.Substring(0, Mathf.Min(3, e.enemyId.Length)).ToUpper();
                    color = e.estaVivo ? new Color(0.70f, 0.15f, 0.15f) : new Color(0.35f, 0.35f, 0.35f);
                }
                else continue;

                var go  = new GameObject($"Retrato_{i}");
                go.transform.SetParent(_listaRetratos, false);

                var img = go.AddComponent<UnityEngine.UI.Image>();
                img.color = i == 0 ? new Color(color.r * 1.3f, color.g * 1.3f, color.b * 1.3f) : color;

                var rt       = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(36f, 36f);

                var lblGO = new GameObject("Lbl");
                lblGO.transform.SetParent(go.transform, false);
                var tmp = lblGO.AddComponent<TextMeshProUGUI>();
                tmp.text      = label;
                tmp.fontSize  = 9f;
                tmp.color     = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
                var lrt       = lblGO.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
            }
        }

        private void RefreshEnemyZone()
        {
            if (_ctx?.enemyTeam == null) return;

            for (int i = 0; i < 3; i++)
            {
                bool hasEnemy = i < _ctx.enemyTeam.Length;

                if (_enemySlots != null && i < _enemySlots.Length && _enemySlots[i] != null)
                    _enemySlots[i].SetActive(hasEnemy);

                if (!hasEnemy) continue;
                var e = _ctx.enemyTeam[i];

                float ratio = e.hpMax > 0 ? (float)e.hpActual / e.hpMax : 0f;

                if (_enemyHPFills != null && i < _enemyHPFills.Length && _enemyHPFills[i] != null)
                {
                    var rt = _enemyHPFills[i].rectTransform;
                    var mx = rt.anchorMax; mx.x = ratio; rt.anchorMax = mx;
                    _enemyHPFills[i].color = HPColor(ratio);
                }

                if (_enemyHPTexts != null && i < _enemyHPTexts.Length && _enemyHPTexts[i] != null)
                    _enemyHPTexts[i].text = $"{e.hpActual}/{e.hpMax}";
            }
        }

        private void RefreshHeroZone()
        {
            if (_ctx?.playerTeam == null) return;

            for (int i = 0; i < 4; i++)
            {
                bool hasHero = i < _ctx.playerTeam.Length;

                if (_heroCards != null && i < _heroCards.Length && _heroCards[i] != null)
                    _heroCards[i].gameObject.SetActive(hasHero);

                if (!hasHero) continue;
                var h = _ctx.playerTeam[i];

                float ratio = h.hpMax > 0 ? (float)h.hpActual / h.hpMax : 0f;

                if (_heroNames != null && i < _heroNames.Length && _heroNames[i] != null)
                    _heroNames[i].text = string.IsNullOrEmpty(h.heroId) ? $"Heroe {i + 1}" : h.heroId;

                if (_heroHPFills != null && i < _heroHPFills.Length && _heroHPFills[i] != null)
                {
                    var rt = _heroHPFills[i].rectTransform;
                    var mx = rt.anchorMax; mx.x = ratio; rt.anchorMax = mx;
                    _heroHPFills[i].color = HPColor(ratio);
                }
            }
        }

        private void HighlightActiveTurn(TurnSlot slot)
        {
            if (_turnIndicators == null) return;

            for (int i = 0; i < _turnIndicators.Length; i++)
            {
                if (_turnIndicators[i] == null) continue;
                bool active = slot.IsHero && slot.HeroIndex == i;
                var c = _turnIndicators[i].color;
                _turnIndicators[i].color = new Color(c.r, c.g, c.b, active ? 1f : 0f);
            }
        }

        private void UpdateTurnoLabel()
        {
            if (_turnoLabel != null)
                _turnoLabel.text = $"Turno {_turnoActual}";
        }

        private void UpdateModoButtonVisual()
        {
            if (_btnModo == null) return;
            var img = _btnModo.GetComponent<Image>();
            if (img == null) return;
            img.color = _autoMode
                ? new Color(0.09f, 0.18f, 0.09f)
                : new Color(0.10f, 0.10f, 0.11f);
            var lbl = _btnModo.GetComponentInChildren<TMP_Text>();
            if (lbl != null) lbl.text = _autoMode ? "AUTO" : "MANUAL";
        }

        // ── Bind botones ───────────────────────────────────────────────────

        private void BindButtons()
        {
            _btnModo?.onClick.AddListener(() => SetAutoMode(!_autoMode));

            _btnHuir?.onClick.AddListener(() =>
                FinalizarCombate(new CombatResult
                {
                    victoria = false, drops = Array.Empty<string>(), gradoObtenido = ""
                }));

            _btnContinuar?.onClick.AddListener(OnContinuarPressed);
            _btnReintentar?.onClick.AddListener(OnReintentarPressed);

            _btnUsarTooltip?.onClick.AddListener(OnUsarTooltip);
            _btnCerrarTooltip?.onClick.AddListener(HideTooltip);

            // Círculos de habilidad — tap muestra tooltip, confirmar ejecuta SelectAbility
            if (_abilityCircles != null)
            {
                for (int i = 0; i < _abilityCircles.Length; i++)
                {
                    int idx = i;
                    _abilityCircles[idx]?.onClick.AddListener(() =>
                    {
                        string desc = GetAbilityDescription(idx);
                        ShowTooltip(desc, () => SelectAbility(idx));
                    });
                }
            }

            // Conjuro slots — tap muestra tooltip con descripción
            if (_conjuroSlots != null)
            {
                for (int i = 0; i < _conjuroSlots.Length; i++)
                {
                    int idx = i;
                    _conjuroSlots[idx]?.onClick.AddListener(() =>
                        ShowTooltip($"Conjuro {idx + 1}\n(Descripcion pendiente de catalogo)", null));
                }
            }

            // Enemy slot buttons → target selection
            if (_enemySlotBtns != null)
            {
                for (int i = 0; i < _enemySlotBtns.Length; i++)
                {
                    int idx = i;
                    _enemySlotBtns[idx]?.onClick.AddListener(() => SelectTarget(idx));
                }
            }
        }

        // ── Helpers estáticos ──────────────────────────────────────────────

        private static bool AnyAlive(HeroInstance[] t)
        {
            if (t == null) return false;
            foreach (var h in t) if (h.estaVivo) return true;
            return false;
        }

        private static bool AnyAlive(EnemyInstance[] t)
        {
            if (t == null) return false;
            foreach (var e in t) if (e.estaVivo) return true;
            return false;
        }

        private static HeroInstance PickFirstAlive(HeroInstance[] t)
        {
            if (t == null) return null;
            foreach (var h in t) if (h.estaVivo) return h;
            return null;
        }

        private static void ApplyDamageToHero(HeroInstance h, int dmg)
        {
            if (dmg <= 0) return;
            h.hpActual = Mathf.Max(0, h.hpActual - dmg);
            if (h.hpActual == 0) h.estaVivo = false;
        }

        private static void ApplyDamageToEnemy(EnemyInstance e, int dmg)
        {
            if (dmg <= 0) return;
            e.hpActual = Mathf.Max(0, e.hpActual - dmg);
            if (e.hpActual == 0) e.estaVivo = false;
        }

        private static Color HPColor(float r)
        {
            if (r > 0.60f) return HP_HEALTHY;
            if (r > 0.30f) return HP_MEDIUM;
            return HP_CRITICAL;
        }

        private void AdvanceTurnSlot()
        {
            if (_turnOrder == null || _turnOrder.Count == 0) return;
            _currentSlotIndex = (_currentSlotIndex + 1) % _turnOrder.Count;
            if (_currentSlotIndex == 0) _turnoActual++;
            RefreshBarraTurno();
        }

        private bool SlotIsAlive(TurnSlot slot)
        {
            return slot.IsHero
                ? _ctx.playerTeam != null && slot.HeroIndex < _ctx.playerTeam.Length && _ctx.playerTeam[slot.HeroIndex].estaVivo
                : _ctx.enemyTeam  != null && slot.EnemyIndex < _ctx.enemyTeam.Length  && _ctx.enemyTeam[slot.EnemyIndex].estaVivo;
        }

        private HeroInstance GetCurrentHero()
        {
            if (_turnOrder == null || _currentSlotIndex >= _turnOrder.Count) return null;
            var s = _turnOrder[_currentSlotIndex];
            return s.IsHero ? _ctx.playerTeam?[s.HeroIndex] : null;
        }

        private int CalcularEstrellas(HeroInstance[] team)
        {
            if (team == null) return 0;
            int bajas = 0;
            foreach (var h in team) if (!h.estaVivo) bajas++;
            if (bajas == 0)  return 3;
            if (bajas <= 2)  return 2;
            return 1;
        }

        private class TurnSlot
        {
            public bool IsHero;
            public int  HeroIndex;
            public int  EnemyIndex;
        }
    }
}
