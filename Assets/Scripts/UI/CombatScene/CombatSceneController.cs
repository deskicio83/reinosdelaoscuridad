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
    public enum CombatPhase
    {
        Idle,
        PlayerTurn,
        EnemyTurn,
        ActionResolve,
        CombatEnd
    }

    /// Controlador de CombatScene — sistema ATB.
    /// NO singleton. Se instancia una vez por carga de CombatScene.
    /// Recibe CombatContext desde CombatSceneData (pasarela entre Scenes).
    public class CombatSceneController : MonoBehaviour
    {
        // ── ATB Units (cableados desde SetupCombatScene) ───────────────────

        [Header("ATB Units")]
        [SerializeField] private ATBUnit[] _heroUnits;   // 4 — uno por HeroCard
        [SerializeField] private ATBUnit[] _enemyUnits;  // 3 — uno por EnemySlot

        // ── Panel Controles ────────────────────────────────────────────────

        [Header("Panel Controles")]
        [SerializeField] private TMP_Text _turnoLabel;
        [SerializeField] private Button   _btnModo;
        [SerializeField] private Button   _btnVelocidad;
        [SerializeField] private Button   _btnPausa;
        [SerializeField] private Button   _btnHuir;

        // ── Zona Enemigos (3 slots dinámicos) ──────────────────────────────

        [Header("Zona Enemigos (3 slots)")]
        [SerializeField] private GameObject[] _enemySlots;
        [SerializeField] private Image[]      _enemyHPFills;
        [SerializeField] private TMP_Text[]   _enemyHPTexts;
        [SerializeField] private Button[]     _enemySlotBtns;

        // ── Zona Equipo (4 cartas) ─────────────────────────────────────────

        [Header("Zona Equipo (4 slots)")]
        [SerializeField] private Button[]   _heroCards;
        [SerializeField] private TMP_Text[] _heroNames;
        [SerializeField] private Image[]    _heroHPFills;

        // ── Zona Habilidades ───────────────────────────────────────────────

        [Header("Zona Habilidades (3 circulos)")]
        [SerializeField] private Button[] _abilityCircles;

        // ── Zona Conjuros ──────────────────────────────────────────────────

        [Header("Zona Conjuros (10 slots)")]
        [SerializeField] private Button[] _conjuroSlots;

        // ── Tooltip Panel ──────────────────────────────────────────────────

        [Header("Tooltip Panel")]
        [SerializeField] private GameObject _tooltipPanel;
        [SerializeField] private TMP_Text   _tooltipText;
        [SerializeField] private Button     _btnUsarTooltip;
        [SerializeField] private Button     _btnCerrarTooltip;

        // ── Result Panel ───────────────────────────────────────────────────

        [Header("Result Panel")]
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TMP_Text   _resultTitleText;
        [SerializeField] private Image[]    _resultStarImages;
        [SerializeField] private TMP_Text   _resultXPText;
        [SerializeField] private TMP_Text   _resultDropsText;
        [SerializeField] private Button     _btnContinuar;
        [SerializeField] private Button     _btnReintentar;

        // ── Colores HP ─────────────────────────────────────────────────────

        private static readonly Color HP_HEALTHY  = new Color(0.08f, 0.50f, 0.24f); // #15803D
        private static readonly Color HP_MEDIUM   = new Color(0.85f, 0.47f, 0.03f); // #D97706
        private static readonly Color HP_CRITICAL = new Color(0.86f, 0.15f, 0.15f); // #DC2626

        // ── ATB constantes ─────────────────────────────────────────────────

        private const float TICK_RATE      = 0.05f;
        private const float ATB_SPEED_BASE = 30f;
        private const float ATB_HEADSTART  = 50f;

        // ── Estado interno ─────────────────────────────────────────────────

        private CombatContext   _ctx;
        private CombatSystem    _combatSystem;
        private CombatPhase     _phase = CombatPhase.Idle;
        private ATBUnit         _activeUnit;
        private List<ATBUnit>   _allUnits;
        private float           _tiempoMultiplier = 1f;
        private bool            _autoMode;
        private int             _turnoActual = 1;
        private int             _selectedAbilityIndex = -1;
        private ATBUnit         _pendingTarget;
        private int             _dañoAcumulado;
        private Coroutine       _atbCoroutine;
        private Canvas          _canvas;
        private Action          _tooltipConfirmAction;

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

            _canvas = GetComponentInParent<Canvas>() ?? FindAnyObjectByType<Canvas>();

            BuildATBUnits();
            RefreshEnemyZone();
            RefreshHeroZone();
            DisableAbilityCircles();

            if (_tooltipPanel != null) _tooltipPanel.SetActive(false);
            if (_resultPanel   != null) _resultPanel.SetActive(false);

            BindButtons();

            await System.Threading.Tasks.Task.Delay(400);
            if (LoadingScreen.Instance != null) LoadingScreen.Instance.Hide();

            Debug.Log($"[CombatScene] Combate iniciado ATB — encuentro: {_ctx.encounterID}");

            _atbCoroutine = StartCoroutine(ATBLoop());
        }

        // ── Construcción de unidades ATB ───────────────────────────────────

        private void BuildATBUnits()
        {
            _allUnits = new List<ATBUnit>();

            // Héroes
            if (_heroUnits != null && _ctx.playerTeam != null)
            {
                for (int i = 0; i < _heroUnits.Length; i++)
                {
                    var unit = _heroUnits[i];
                    if (unit == null) continue;

                    if (i < _ctx.playerTeam.Length)
                    {
                        var h = _ctx.playerTeam[i];
                        unit.esJugador = true;
                        unit.unitId    = h.heroId;
                        unit.spd       = Mathf.Max(h.spd, 1);
                        unit.estaVivo  = h.estaVivo;
                        unit.heroData  = h;
                        unit.ResetATB();
                        _allUnits.Add(unit);
                    }
                    else
                    {
                        unit.estaVivo = false;
                        unit.gameObject.SetActive(false);
                    }
                }
            }

            // Enemigos
            if (_enemyUnits != null && _ctx.enemyTeam != null)
            {
                for (int i = 0; i < _enemyUnits.Length; i++)
                {
                    var unit = _enemyUnits[i];
                    if (unit == null) continue;

                    if (i < _ctx.enemyTeam.Length)
                    {
                        var e = _ctx.enemyTeam[i];
                        unit.esJugador = false;
                        unit.unitId    = e.enemyId;
                        unit.spd       = Mathf.Max(e.spd, 1);
                        unit.estaVivo  = e.estaVivo;
                        unit.enemyData = e;
                        unit.ResetATB();
                        _allUnits.Add(unit);
                    }
                    else
                    {
                        unit.estaVivo = false;
                        if (_enemySlots != null && i < _enemySlots.Length && _enemySlots[i] != null)
                            _enemySlots[i].SetActive(false);
                    }
                }
            }

            if (_allUnits.Count == 0) return;

            // La unidad más rápida arranca con ventaja de ATB
            int maxSpd = 0;
            foreach (var u in _allUnits) if (u.spd > maxSpd) maxSpd = u.spd;

            bool headStartAssigned = false;
            foreach (var u in _allUnits)
            {
                if (!headStartAssigned && u.spd == maxSpd)
                {
                    u.TickATB(ATB_HEADSTART);
                    headStartAssigned = true;
                }
            }
        }

        // ── ATB Loop principal ─────────────────────────────────────────────

        private IEnumerator ATBLoop()
        {
            while (_phase != CombatPhase.CombatEnd)
            {
                if (_phase == CombatPhase.Idle && _allUnits != null)
                {
                    // Calcular spd máximo de vivos
                    int maxSpd = 1;
                    foreach (var u in _allUnits)
                        if (u.estaVivo && u.spd > maxSpd) maxSpd = u.spd;

                    // Tick a todas las unidades vivas
                    foreach (var u in _allUnits)
                    {
                        if (!u.estaVivo) continue;
                        float gain = (float)u.spd / maxSpd * ATB_SPEED_BASE * _tiempoMultiplier;
                        u.TickATB(gain);
                    }

                    // Buscar la primera unidad lista (mayor spd gana en empate)
                    ATBUnit readyUnit = null;
                    int highestSpd = -1;
                    foreach (var u in _allUnits)
                    {
                        if (!u.estaVivo || !u.IsReady()) continue;
                        if (u.spd > highestSpd)
                        {
                            highestSpd = u.spd;
                            readyUnit  = u;
                        }
                    }

                    if (readyUnit != null)
                    {
                        _activeUnit = readyUnit;
                        _activeUnit.SetActive(true);

                        if (readyUnit.esJugador)
                            yield return StartCoroutine(PlayerTurnRoutine(readyUnit));
                        else
                            yield return StartCoroutine(EnemyTurnRoutine(readyUnit));
                    }
                }

                yield return new WaitForSeconds(TICK_RATE);
            }
        }

        // ── Turno del jugador ──────────────────────────────────────────────

        private IEnumerator PlayerTurnRoutine(ATBUnit unit)
        {
            _phase = CombatPhase.PlayerTurn;
            _turnoActual++;
            UpdateTurnoLabel();
            _selectedAbilityIndex = -1;
            _pendingTarget        = null;

            if (_autoMode)
            {
                yield return new WaitForSeconds(0.5f / _tiempoMultiplier);

                ATBUnit autoTarget = null;
                foreach (var u in _allUnits)
                    if (!u.esJugador && u.estaVivo) { autoTarget = u; break; }

                if (autoTarget != null)
                    yield return StartCoroutine(ExecuteAction(unit, autoTarget, 0));
                else
                {
                    unit.ResetATB();
                    unit.SetActive(false);
                    _phase = CombatPhase.Idle;
                }
                yield break;
            }

            // Manual — habilitar círculos y esperar input
            int heroIdx = GetHeroIndex(unit);
            UpdateAbilityCirclesForHero(heroIdx);

            yield return new WaitUntil(() => _pendingTarget != null || _phase == CombatPhase.CombatEnd);

            if (_phase == CombatPhase.CombatEnd) yield break;

            var target     = _pendingTarget;
            int abilityIdx = _selectedAbilityIndex >= 0 ? _selectedAbilityIndex : 0;
            _pendingTarget        = null;
            _selectedAbilityIndex = -1;

            yield return StartCoroutine(ExecuteAction(unit, target, abilityIdx));
        }

        // ── Turno del enemigo ──────────────────────────────────────────────

        private IEnumerator EnemyTurnRoutine(ATBUnit unit)
        {
            _phase = CombatPhase.EnemyTurn;
            _turnoActual++;
            UpdateTurnoLabel();

            yield return new WaitForSeconds(0.5f / _tiempoMultiplier);

            // Objetivo: héroe con menor HP
            ATBUnit target   = null;
            int     lowestHP = int.MaxValue;
            foreach (var u in _allUnits)
            {
                if (!u.esJugador || !u.estaVivo) continue;
                int hp = u.heroData?.hpActual ?? 0;
                if (hp < lowestHP) { lowestHP = hp; target = u; }
            }

            if (target != null)
                yield return StartCoroutine(ExecuteAction(unit, target, 0));
            else
            {
                unit.ResetATB();
                unit.SetActive(false);
                _phase = CombatPhase.Idle;
            }
        }

        // ── Ejecución de acción ────────────────────────────────────────────

        private IEnumerator ExecuteAction(ATBUnit attacker, ATBUnit target, int abilityIndex)
        {
            _phase = CombatPhase.ActionResolve;
            DisableAbilityCircles();

            // Animación: escalar carta 1.0 → 1.1 en 150 ms y volver
            var attackerRT = attacker.GetComponent<RectTransform>();
            if (attackerRT != null)
            {
                float t = 0f;
                while (t < 0.15f)
                {
                    t += Time.deltaTime;
                    attackerRT.localScale = Vector3.one * Mathf.Lerp(1f, 1.1f, t / 0.15f);
                    yield return null;
                }
                attackerRT.localScale = Vector3.one;
            }

            yield return new WaitForSeconds(0.2f);

            // Calcular daño
            int  daño    = 0;
            bool crit    = false;
            bool ventaja = false;
            bool esquivado = false;

            if (_combatSystem != null)
            {
                if (attacker.esJugador && attacker.heroData != null && target.enemyData != null)
                {
                    float mult = abilityIndex == 1 ? 1.8f : abilityIndex == 2 ? 1.2f : 1f;
                    var res = _combatSystem.CalculateDamage(attacker.heroData, target.enemyData, mult);
                    daño     = res.dañoFinal;
                    crit     = res.fueCritico;
                    ventaja  = res.fueElementalVentaja;
                    esquivado = res.fueEsquivado;

                    if (!esquivado)
                    {
                        target.enemyData.hpActual = Mathf.Max(0, target.enemyData.hpActual - daño);
                        if (target.enemyData.hpActual == 0)
                        {
                            target.enemyData.estaVivo = false;
                            target.estaVivo           = false;
                        }
                    }
                }
                else if (!attacker.esJugador && attacker.enemyData != null && target.heroData != null)
                {
                    var res = _combatSystem.CalculateDamageEnemyAttack(attacker.enemyData, target.heroData);
                    daño     = res.dañoFinal;
                    crit     = res.fueCritico;
                    esquivado = res.fueEsquivado;

                    if (!esquivado)
                    {
                        target.heroData.hpActual = Mathf.Max(0, target.heroData.hpActual - daño);
                        if (target.heroData.hpActual == 0)
                        {
                            target.heroData.estaVivo = false;
                            target.estaVivo          = false;
                        }
                    }
                }
            }
            else
            {
                // Modo degradado sin CombatSystem
                daño = UnityEngine.Random.Range(100, 500);
                if (target.enemyData != null)
                {
                    target.enemyData.hpActual = Mathf.Max(0, target.enemyData.hpActual - daño);
                    if (target.enemyData.hpActual == 0) { target.enemyData.estaVivo = false; target.estaVivo = false; }
                }
                else if (target.heroData != null)
                {
                    target.heroData.hpActual = Mathf.Max(0, target.heroData.hpActual - daño);
                    if (target.heroData.hpActual == 0) { target.heroData.estaVivo = false; target.estaVivo = false; }
                }
            }

            _dañoAcumulado += daño;

            Debug.Log($"[ATB] {attacker.unitId} hab:{abilityIndex} → {target.unitId}: {daño}" +
                      (esquivado ? " [ESQ]" : "") + (crit ? " [CRIT]" : "") + (ventaja ? " [VENTAJA]" : ""));

            // Texto de daño flotante
            if (_canvas != null && daño > 0 && !esquivado)
            {
                var targetRT = target.GetComponent<RectTransform>();
                Vector2 screenPos = targetRT != null
                    ? RectTransformUtility.WorldToScreenPoint(null, targetRT.position)
                    : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                FloatingDamageText.Spawn(_canvas, screenPos, daño, crit, ventaja);
            }

            // Actualizar barras HP
            RefreshEnemyZone();
            RefreshHeroZone();

            // Dim carta si murió
            if (!target.estaVivo)
            {
                target.SetActive(false);
                var cardImg = target.GetComponent<Image>();
                if (cardImg != null)
                    cardImg.color = new Color(0.15f, 0.15f, 0.15f, 0.50f);
            }

            yield return new WaitForSeconds(0.2f);

            // Verificar fin de combate
            bool anyHeroAlive  = false;
            bool anyEnemyAlive = false;
            foreach (var u in _allUnits)
            {
                if (u.esJugador  && u.estaVivo) anyHeroAlive  = true;
                if (!u.esJugador && u.estaVivo) anyEnemyAlive = true;
            }

            if (!anyHeroAlive || !anyEnemyAlive)
            {
                bool victoria = anyHeroAlive && !anyEnemyAlive;
                FinalizarCombate(new CombatResult
                {
                    victoria      = victoria,
                    danoTotal     = _dañoAcumulado,
                    drops         = Array.Empty<string>(),
                    xpGanada      = victoria ? 200 : 0,
                    trofeosDelta  = 0,
                    gradoObtenido = victoria ? "" : ""
                });
                yield break;
            }

            // Resetear atacante y volver a Idle
            attacker.ResetATB();
            attacker.SetActive(false);
            _phase = CombatPhase.Idle;
        }

        // ── Input del jugador ──────────────────────────────────────────────

        public void OnAbilitySelected(int abilityIndex)
        {
            if (_phase != CombatPhase.PlayerTurn) return;
            _selectedAbilityIndex = abilityIndex;

            // Highlight enemies como objetivo
            if (_allUnits == null) return;
            foreach (var u in _allUnits)
            {
                if (!u.esJugador && u.estaVivo)
                    u.SetHighlightTargetable(true);
            }

            Debug.Log($"[ATB] Habilidad {abilityIndex} seleccionada — elige objetivo.");
        }

        public void OnUnitTapped(ATBUnit tapped)
        {
            if (_phase != CombatPhase.PlayerTurn) return;
            if (_selectedAbilityIndex < 0) return;
            if (tapped == null || !tapped.estaVivo || tapped.esJugador) return;

            // Limpiar highlights
            foreach (var u in _allUnits) u.SetHighlightTargetable(false);
            DisableAbilityCircles();

            _pendingTarget = tapped;
        }

        // ── Tooltip ────────────────────────────────────────────────────────

        public void ShowTooltip(string description, Action onConfirm)
        {
            if (_tooltipPanel == null) { onConfirm?.Invoke(); return; }
            if (_tooltipText != null)    _tooltipText.text = description;
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

        // ── Finalización ───────────────────────────────────────────────────

        private void FinalizarCombate(CombatResult result)
        {
            _phase = CombatPhase.CombatEnd;
            if (_atbCoroutine != null) StopCoroutine(_atbCoroutine);
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

            if (_resultStarImages != null)
            {
                int stars = result.victoria ? CalcularEstrellas(_ctx.playerTeam) : 0;
                for (int i = 0; i < _resultStarImages.Length; i++)
                {
                    if (_resultStarImages[i] == null) continue;
                    _resultStarImages[i].color = i < stars
                        ? new Color(0.98f, 0.80f, 0.08f)
                        : result.victoria
                            ? new Color(0.25f, 0.25f, 0.28f)
                            : new Color(0.55f, 0.10f, 0.10f);
                }
            }

            if (_resultXPText   != null) _resultXPText.text   = $"+{result.xpGanada} XP";
            if (_resultDropsText != null)
                _resultDropsText.text = result.drops != null && result.drops.Length > 0
                    ? "Recompensas:\n* " + string.Join("\n* ", result.drops)
                    : "Recompensas:\nSin drops";
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

        private void UpdateTurnoLabel()
        {
            if (_turnoLabel != null)
            {
                string quien = _phase == CombatPhase.PlayerTurn ? "Jugador"
                             : _phase == CombatPhase.EnemyTurn  ? "Enemigo"
                             : "...";
                _turnoLabel.text = $"T{_turnoActual} {quien}";
            }
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
            if (_activeUnit == null || _activeUnit.heroData == null)
                return $"Habilidad {abilityIndex + 1}";

            var hero = _activeUnit.heroData;
            string habId = hero.habilidadesEquipadas != null && abilityIndex < hero.habilidadesEquipadas.Length
                ? hero.habilidadesEquipadas[abilityIndex]
                : null;

            return string.IsNullOrEmpty(habId)
                ? $"Habilidad {abilityIndex + 1}\n(Sin equipar)"
                : $"{habId}\n(Descripcion pendiente de catalogo)";
        }

        // ── Bind botones ───────────────────────────────────────────────────

        private void BindButtons()
        {
            // Velocidad x1/x2
            _btnVelocidad?.onClick.AddListener(() =>
            {
                _tiempoMultiplier = _tiempoMultiplier < 1.5f ? 2f : 1f;
                var lbl = _btnVelocidad.GetComponentInChildren<TMP_Text>();
                if (lbl != null) lbl.text = _tiempoMultiplier > 1.5f ? "x2 >" : "x1 >";
            });

            // Modo Auto/Manual
            _btnModo?.onClick.AddListener(() =>
            {
                _autoMode = !_autoMode;
                var img = _btnModo.GetComponent<Image>();
                if (img != null) img.color = _autoMode ? new Color(0.09f, 0.18f, 0.09f) : new Color(0.10f, 0.10f, 0.11f);
                var lbl = _btnModo.GetComponentInChildren<TMP_Text>();
                if (lbl != null) lbl.text = _autoMode ? "AUTO" : "MANUAL";
            });

            // Huir
            _btnHuir?.onClick.AddListener(() =>
                FinalizarCombate(new CombatResult
                {
                    victoria = false, drops = Array.Empty<string>(), gradoObtenido = ""
                }));

            _btnContinuar?.onClick.AddListener(OnContinuarPressed);
            _btnReintentar?.onClick.AddListener(OnReintentarPressed);

            _btnUsarTooltip?.onClick.AddListener(OnUsarTooltip);
            _btnCerrarTooltip?.onClick.AddListener(HideTooltip);

            // Círculos de habilidad → tooltip → OnAbilitySelected
            if (_abilityCircles != null)
            {
                for (int i = 0; i < _abilityCircles.Length; i++)
                {
                    int idx = i;
                    _abilityCircles[idx]?.onClick.AddListener(() =>
                    {
                        string desc = GetAbilityDescription(idx);
                        ShowTooltip(desc, () => OnAbilitySelected(idx));
                    });
                }
            }

            // Conjuro slots → tooltip informativo
            if (_conjuroSlots != null)
            {
                for (int i = 0; i < _conjuroSlots.Length; i++)
                {
                    int idx = i;
                    _conjuroSlots[idx]?.onClick.AddListener(() =>
                        ShowTooltip($"Conjuro {idx + 1}\n(Descripcion pendiente de catalogo)", null));
                }
            }

            // Enemy slot buttons → selección de objetivo tras elegir habilidad
            if (_enemySlotBtns != null)
            {
                for (int i = 0; i < _enemySlotBtns.Length; i++)
                {
                    int idx = i;
                    _enemySlotBtns[idx]?.onClick.AddListener(() =>
                    {
                        if (_enemyUnits != null && idx < _enemyUnits.Length && _enemyUnits[idx] != null)
                            OnUnitTapped(_enemyUnits[idx]);
                    });
                }
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private int GetHeroIndex(ATBUnit unit)
        {
            if (_heroUnits == null) return -1;
            for (int i = 0; i < _heroUnits.Length; i++)
                if (_heroUnits[i] == unit) return i;
            return -1;
        }

        private static Color HPColor(float ratio)
        {
            if (ratio > 0.50f) return HP_HEALTHY;
            if (ratio > 0.25f) return HP_MEDIUM;
            return HP_CRITICAL;
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
    }
}
