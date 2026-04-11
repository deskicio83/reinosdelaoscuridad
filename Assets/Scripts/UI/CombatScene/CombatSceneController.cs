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
        // ── Referencias UI — Control Panel ─────────────────────────────────

        [Header("Control Panel")]
        [SerializeField] private TMP_Text _turnoText;
        [SerializeField] private Button   _btnAuto;
        [SerializeField] private Button   _btnHuir;

        // ── Referencias UI — Zona Enemigos ─────────────────────────────────

        [Header("Zona Enemigos (3 slots)")]
        [SerializeField] private Image[]    _enemySlots;     // Image del slot
        [SerializeField] private Image[]    _enemyHPFills;   // Relleno de barra HP
        [SerializeField] private TMP_Text[] _enemyHPTexts;   // Texto "8000/8000"
        [SerializeField] private Button[]   _enemySlotBtns;  // Para selección de objetivo

        // ── Referencias UI — Zona Héroes ───────────────────────────────────

        [Header("Zona Equipo (5 cards)")]
        [SerializeField] private Button[]     _heroCards;
        [SerializeField] private TMP_Text[]   _heroNames;
        [SerializeField] private Image[]      _heroHPFills;
        [SerializeField] private GameObject[] _turnIndicators; // se activan en turno del héroe

        // ── Referencias UI — Panel Habilidades ─────────────────────────────

        [Header("Panel Habilidades (3 botones)")]
        [SerializeField] private Button[] _abilityButtons;

        // ── Referencias UI — Result Panel ──────────────────────────────────

        [Header("Result Panel")]
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TMP_Text   _resultTitleText;
        [SerializeField] private TMP_Text   _resultGradeText;
        [SerializeField] private TMP_Text   _resultXPText;
        [SerializeField] private TMP_Text   _resultDropsText;
        [SerializeField] private Button     _btnContinuar;

        // ── Colores de HP ───────────────────────────────────────────────────

        private static readonly Color HP_HEALTHY  = new Color(0.08f, 0.50f, 0.24f); // #15803D
        private static readonly Color HP_MEDIUM   = new Color(0.85f, 0.47f, 0.03f); // #D97706
        private static readonly Color HP_CRITICAL = new Color(0.86f, 0.15f, 0.15f); // #DC2626

        // ── Estado interno ─────────────────────────────────────────────────

        private CombatContext _ctx;
        private CombatSystem  _combatSystem;
        private TurnState     _state = TurnState.WaitingForInput;
        private bool          _autoMode;

        // Orden de turno y tracking
        private List<TurnSlot> _turnOrder;
        private int            _currentSlotIndex;
        private int            _turnoActual = 1;

        // Selección de habilidad en modo Manual
        private int _selectedAbilityIndex = -1;

        // ── Inicio ─────────────────────────────────────────────────────────

        private async void Start()
        {
            // Guard: GameManager debe estar activo (viene de BootScene)
            if (GameManager.Instance == null)
            {
                Debug.LogError("[CombatScene] GameManager no encontrado. ¿Se cargó BootScene primero?");
                return;
            }

            // 1. Recuperar contexto desde la pasarela estática
            _ctx = CombatSceneData.PendingContext;
            if (_ctx == null)
            {
                Debug.LogError("[CombatScene] CombatContext es null — volviendo a MainMenuScene.");
                if (UIManager.Instance != null)
                    await UIManager.Instance.NavigateTo("MainMenuScene");
                return;
            }

            // 2. Mostrar loading mientras preparamos
            if (LoadingScreen.Instance != null)
                LoadingScreen.Instance.Show("preparando combate");

            // 3. Obtener CombatSystem (registrado en BootScene o en esta Scene)
            _combatSystem = GameManager.Instance.GetSystem<CombatSystem>();
            if (_combatSystem == null)
                Debug.LogWarning("[CombatScene] CombatSystem no encontrado — modo degradado sin fórmulas.");

            // 4. Calcular orden de turnos por SPD descendente
            BuildTurnOrder();

            // 5. Inicializar UI con datos del contexto
            RefreshEnemyZone();
            RefreshHeroZone();
            UpdateTurnoText();

            // 6. Bindear botones
            BindButtons();

            if (_resultPanel != null)
                _resultPanel.SetActive(false);

            // 7. Ocultar loading y mostrar combate
            await System.Threading.Tasks.Task.Delay(400);
            if (LoadingScreen.Instance != null)
                LoadingScreen.Instance.Hide();

            Debug.Log($"[CombatScene] Combate iniciado — encuentro: {_ctx.encounterID} — modo: {_ctx.combatMode}");

            // 8. Iniciar bucle de combate
            if (_autoMode)
                StartCoroutine(AutoCombatCoroutine());
            else
                StartCoroutine(ManualCombatCoroutine());
        }

        // ── API pública ────────────────────────────────────────────────────

        /// Cambia entre modo Auto y modo Manual.
        public void SetAutoMode(bool auto)
        {
            _autoMode = auto;
            UpdateAutoButtonVisual();
            Debug.Log($"[CombatScene] Modo Auto: {_autoMode}");
        }

        /// Selecciona la habilidad a usar (modo Manual). 0=básico, 1=fuerte, 2=especial.
        public void SelectAbility(int abilityIndex)
        {
            if (_state != TurnState.WaitingForInput) return;
            _selectedAbilityIndex = abilityIndex;
            _state = TurnState.SelectingTarget;
            Debug.Log($"[CombatScene] Habilidad {abilityIndex} seleccionada — elige objetivo.");
        }

        /// Selecciona el enemigo objetivo (modo Manual). Llama después de SelectAbility.
        public void SelectTarget(int targetIndex)
        {
            if (_state != TurnState.SelectingTarget) return;
            if (_ctx?.enemyTeam == null || targetIndex < 0 || targetIndex >= _ctx.enemyTeam.Length) return;
            if (!_ctx.enemyTeam[targetIndex].estaVivo) return;

            var hero   = GetCurrentHero();
            var target = _ctx.enemyTeam[targetIndex];
            ProcessHeroAction(hero, target);
        }

        /// Salta directo al resultado usando ProcessCombat (solo en modo Auto).
        public void SkipToResult()
        {
            if (!_autoMode) return;
            StopAllCoroutines();
            var result = _combatSystem != null
                ? _combatSystem.ProcessCombat(_ctx)
                : new CombatResult { victoria = false, drops = Array.Empty<string>(), gradoObtenido = "C" };
            FinalizarCombate(result);
        }

        // ── Construcción del orden de turno ────────────────────────────────

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
                int spdA = a.IsHero ? _ctx.playerTeam[a.HeroIndex].spd : _ctx.enemyTeam[a.EnemyIndex].spd;
                int spdB = b.IsHero ? _ctx.playerTeam[b.HeroIndex].spd : _ctx.enemyTeam[b.EnemyIndex].spd;
                if (spdB != spdA) return spdB.CompareTo(spdA);
                return a.IsHero ? -1 : 1; // empate: jugador primero
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

            // Procesa todo el combate en un solo paso y espera un beat dramático
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

                // Saltar unidades muertas
                if (!SlotIsAlive(slot))
                {
                    AdvanceTurnSlot();
                    yield return null;
                    continue;
                }

                HighlightActiveTurnIndicator(slot);
                UpdateTurnoText();

                if (slot.IsHero)
                {
                    var hero = _ctx.playerTeam[slot.HeroIndex];

                    // Stun: saltar turno sin input
                    if (hero.efectosActivos != null && hero.efectosActivos.Contains("Stun"))
                    {
                        Debug.Log($"[CombatScene] {hero.heroId} aturdido — turno saltado.");
                        yield return new WaitForSeconds(0.4f);
                    }
                    else
                    {
                        // Esperar input del jugador (SelectAbility → SelectTarget → ProcessHeroAction)
                        _state = TurnState.WaitingForInput;
                        yield return new WaitUntil(() =>
                            _state == TurnState.ProcessingAction ||
                            _state == TurnState.CombatFinished);

                        if (_state == TurnState.CombatFinished) yield break;
                        yield return new WaitForSeconds(0.3f); // pausa post-acción
                    }
                }
                else
                {
                    // Turno del enemigo — ataque automático al primer héroe vivo
                    _state = TurnState.ProcessingAction;
                    var enemy  = _ctx.enemyTeam[slot.EnemyIndex];
                    var target = PickFirstAlive(_ctx.playerTeam);

                    if (target != null && _combatSystem != null)
                    {
                        var dmg = _combatSystem.CalculateDamageEnemyAttack(enemy, target);
                        ApplyDamageToHero(target, dmg.dañoFinal);
                        RefreshHeroZone();

                        Debug.Log($"[CombatScene] {enemy.enemyId} ataca a {target.heroId} por {dmg.dañoFinal}" +
                                  (dmg.fueEsquivado ? " [ESQUIVADO]" : "") +
                                  (dmg.fueCritico   ? " [CRIT]"     : ""));
                    }

                    yield return new WaitForSeconds(0.6f);
                }

                if (!AnyAlive(_ctx.playerTeam) || !AnyAlive(_ctx.enemyTeam)) break;

                AdvanceTurnSlot();
            }

            var resultado = new CombatResult
            {
                victoria      = AnyAlive(_ctx.playerTeam) && !AnyAlive(_ctx.enemyTeam),
                danoTotal     = 0,
                drops         = Array.Empty<string>(),
                xpGanada      = 0,
                trofeosDelta  = 0,
                gradoObtenido = _turnoActual <= 5 ? "S" : _turnoActual <= 10 ? "A" : _turnoActual <= 20 ? "B" : "C"
            };

            FinalizarCombate(resultado);
        }

        // ── Acción del jugador ─────────────────────────────────────────────

        private void ProcessHeroAction(HeroInstance hero, EnemyInstance target)
        {
            if (hero == null || target == null) return;

            float mult = _selectedAbilityIndex == 1 ? 1.8f  // fuerte
                       : _selectedAbilityIndex == 2 ? 1.2f  // especial
                       : 1f;                                 // básico

            if (_combatSystem != null)
            {
                var dmg = _combatSystem.CalculateDamage(hero, target, mult);
                ApplyDamageToEnemy(target, dmg.dañoFinal);
                RefreshEnemyZone();

                Debug.Log($"[CombatScene] {hero.heroId} usa habilidad {_selectedAbilityIndex} " +
                          $"sobre {target.enemyId} por {dmg.dañoFinal}" +
                          (dmg.fueEsquivado         ? " [ESQUIVADO]"  : "") +
                          (dmg.fueCritico            ? " [CRIT]"       : "") +
                          (dmg.fueElementalVentaja   ? " [VENTAJA]"    : "") +
                          (dmg.fueElementalDesventaja? " [DESVENTAJA]" : ""));
            }

            _selectedAbilityIndex = -1;
            _state = TurnState.ProcessingAction; // desbloquea la coroutine manual
        }

        // ── Finalización del combate ────────────────────────────────────────

        private void FinalizarCombate(CombatResult result)
        {
            _state = TurnState.CombatFinished;
            StopAllCoroutines();

            // Guardar resultado en la pasarela
            CombatSceneData.SetResult(result);

            // Publicar al EventBus
            EventBus.Publish(new CombatCompletedData
            {
                encounterId = _ctx?.encounterID ?? "",
                victory     = result.victoria,
                expGained   = result.xpGanada,
                goldGained  = 0
            });

            // InputBlocker mientras el ResultPanel está visible
            if (InputBlocker.Instance != null)
                InputBlocker.Instance.Show(49);

            ShowResultPanel(result);
        }

        private void ShowResultPanel(CombatResult result)
        {
            if (_resultPanel == null)
            {
                Debug.LogWarning("[CombatScene] ResultPanel no asignado — volviendo directamente.");
                _ = (UIManager.Instance?.NavigateTo(_ctx?.callerScene ?? "MainMenuScene"));
                return;
            }

            _resultPanel.SetActive(true);

            if (_resultTitleText != null)
                _resultTitleText.text = result.victoria ? "VICTORIA" : "DERROTA";

            if (_resultGradeText != null)
                _resultGradeText.text = $"Grado: {result.gradoObtenido}";

            if (_resultXPText != null)
                _resultXPText.text = $"XP: +{result.xpGanada}";

            if (_resultDropsText != null)
                _resultDropsText.text = result.drops != null && result.drops.Length > 0
                    ? "Drops: " + string.Join(", ", result.drops)
                    : "Sin drops";

            _state = TurnState.ShowingResult;
        }

        private async void OnContinuarPressed()
        {
            if (InputBlocker.Instance != null)
                InputBlocker.Instance.Hide();

            string destino = _ctx?.callerScene ?? "MainMenuScene";
            if (UIManager.Instance != null)
                await UIManager.Instance.NavigateTo(destino);
        }

        // ── Refresh UI ─────────────────────────────────────────────────────

        private void RefreshEnemyZone()
        {
            if (_ctx?.enemyTeam == null) return;

            for (int i = 0; i < 3; i++)
            {
                bool hasEnemy = i < _ctx.enemyTeam.Length;

                if (_enemySlots != null && i < _enemySlots.Length && _enemySlots[i] != null)
                    _enemySlots[i].gameObject.SetActive(hasEnemy);

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

            for (int i = 0; i < 5; i++)
            {
                bool hasHero = i < _ctx.playerTeam.Length;

                if (_heroCards != null && i < _heroCards.Length && _heroCards[i] != null)
                    _heroCards[i].gameObject.SetActive(hasHero);

                if (!hasHero) continue;
                var h = _ctx.playerTeam[i];

                float ratio = h.hpMax > 0 ? (float)h.hpActual / h.hpMax : 0f;

                if (_heroNames != null && i < _heroNames.Length && _heroNames[i] != null)
                    _heroNames[i].text = string.IsNullOrEmpty(h.heroId) ? $"Héroe {i + 1}" : h.heroId;

                if (_heroHPFills != null && i < _heroHPFills.Length && _heroHPFills[i] != null)
                {
                    var rt = _heroHPFills[i].rectTransform;
                    var mx = rt.anchorMax; mx.x = ratio; rt.anchorMax = mx;
                    _heroHPFills[i].color = HPColor(ratio);
                }
            }
        }

        private void HighlightActiveTurnIndicator(TurnSlot slot)
        {
            if (_turnIndicators == null) return;

            for (int i = 0; i < _turnIndicators.Length; i++)
            {
                if (_turnIndicators[i] == null) continue;
                bool isActive = slot.IsHero && slot.HeroIndex == i;
                var img = _turnIndicators[i].GetComponent<Image>();
                if (img != null)
                    img.color = new Color(img.color.r, img.color.g, img.color.b,
                                          isActive ? 1f : 0f);
            }
        }

        private void UpdateTurnoText()
        {
            if (_turnoText != null)
                _turnoText.text = $"Turno {_turnoActual}";
        }

        private void UpdateAutoButtonVisual()
        {
            if (_btnAuto == null) return;
            var img = _btnAuto.GetComponent<Image>();
            if (img == null) return;
            img.color = _autoMode
                ? new Color(0.09f, 0.18f, 0.09f)  // #166534
                : new Color(0.10f, 0.10f, 0.11f);  // #1A1A1C
        }

        // ── Bind botones ───────────────────────────────────────────────────

        private void BindButtons()
        {
            _btnAuto?.onClick.AddListener(() => SetAutoMode(!_autoMode));

            _btnHuir?.onClick.AddListener(() =>
            {
                var fled = new CombatResult
                {
                    victoria      = false,
                    drops         = Array.Empty<string>(),
                    gradoObtenido = "F"
                };
                FinalizarCombate(fled);
            });

            _btnContinuar?.onClick.AddListener(OnContinuarPressed);

            if (_abilityButtons != null)
                for (int i = 0; i < _abilityButtons.Length; i++)
                {
                    int idx = i;
                    _abilityButtons[idx]?.onClick.AddListener(() => SelectAbility(idx));
                }

            if (_enemySlotBtns != null)
                for (int i = 0; i < _enemySlotBtns.Length; i++)
                {
                    int idx = i;
                    _enemySlotBtns[idx]?.onClick.AddListener(() => SelectTarget(idx));
                }
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static bool AnyAlive(HeroInstance[] team)
        {
            if (team == null) return false;
            foreach (var h in team) if (h.estaVivo) return true;
            return false;
        }

        private static bool AnyAlive(EnemyInstance[] team)
        {
            if (team == null) return false;
            foreach (var e in team) if (e.estaVivo) return true;
            return false;
        }

        private static HeroInstance PickFirstAlive(HeroInstance[] team)
        {
            if (team == null) return null;
            foreach (var h in team) if (h.estaVivo) return h;
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

        private static Color HPColor(float ratio)
        {
            if (ratio > 0.60f) return HP_HEALTHY;
            if (ratio > 0.30f) return HP_MEDIUM;
            return HP_CRITICAL;
        }

        private void AdvanceTurnSlot()
        {
            if (_turnOrder == null || _turnOrder.Count == 0) return;
            _currentSlotIndex = (_currentSlotIndex + 1) % _turnOrder.Count;
            if (_currentSlotIndex == 0) _turnoActual++;
        }

        private bool SlotIsAlive(TurnSlot slot)
        {
            if (slot.IsHero)
                return _ctx.playerTeam != null
                       && slot.HeroIndex < _ctx.playerTeam.Length
                       && _ctx.playerTeam[slot.HeroIndex].estaVivo;

            return _ctx.enemyTeam != null
                   && slot.EnemyIndex < _ctx.enemyTeam.Length
                   && _ctx.enemyTeam[slot.EnemyIndex].estaVivo;
        }

        private HeroInstance GetCurrentHero()
        {
            if (_turnOrder == null || _currentSlotIndex >= _turnOrder.Count) return null;
            var slot = _turnOrder[_currentSlotIndex];
            if (!slot.IsHero) return null;
            return _ctx.playerTeam?[slot.HeroIndex];
        }

        // ── Clase auxiliar ─────────────────────────────────────────────────

        private class TurnSlot
        {
            public bool IsHero;
            public int  HeroIndex;
            public int  EnemyIndex;
        }
    }
}
