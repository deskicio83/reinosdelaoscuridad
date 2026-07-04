using System;
using System.Collections.Generic;
using UnityEngine;
using ReinoOscuridad.Core;
using ReinoOscuridad.Data;

namespace ReinoOscuridad.Systems
{
    /// Sistema de combate por turnos.
    /// Lógica pura — sin UI. Recibe CombatContext, devuelve CombatResult.
    /// Fórmula de daño en 5 pasos: Dodge → Crit → Base → Elemental → Efecto.
    [DefaultExecutionOrder(-10)]
    public class CombatSystem : MonoBehaviour, ISystem
    {
        // ── Constants ─────────────────────────────────────────────────────────

        private const float K              = 1000f;  // factor de defensa
        private const float DODGE_MIN      = 0.05f;
        private const float DODGE_MAX      = 0.30f;
        private const float EFFECT_MIN     = 0.10f;
        private const float EFFECT_MAX     = 0.90f;
        private const int   MAX_STACKS     = 3;
        private const int   MAX_TURNS      = 50;     // límite anti-loop infinito
        private const float SAME_ELEMENT   = 0.85f;

        // ── Tabla elemental — 12 relaciones explícitas ─────────────────────────
        // (atacante, defensor) → multiplicador
        // 0.7 = desventaja  · 1.5 = ventaja  · 1.2 = ventaja menor
        // Cualquier par no listado → 1.0 (neutro). Mismo elemento → 0.85.

        private static readonly Dictionary<(string, string), float> _elementalTable =
            new Dictionary<(string, string), float>
            {
                // ── Triángulo Fuego / Naturaleza / Agua ──
                { ("fuego",      "naturaleza"), 1.5f },  // fuego quema naturaleza
                { ("naturaleza", "fuego"),      0.7f },  // naturaleza no soporta fuego
                { ("naturaleza", "agua"),        1.5f }, // naturaleza absorbe agua
                { ("agua",       "naturaleza"), 0.7f },  // agua cede ante naturaleza
                { ("agua",       "fuego"),       1.5f }, // agua apaga fuego
                { ("fuego",      "agua"),        0.7f }, // fuego cede ante agua

                // ── Dualidad Luz / Oscuridad ──
                { ("luz",        "oscuridad"),  1.5f },  // luz disipa oscuridad
                { ("oscuridad",  "luz"),         1.5f }, // oscuridad corroe la luz

                // ── Interacciones cruzadas menores ──
                { ("luz",        "naturaleza"), 1.2f },  // luz bendice la naturaleza
                { ("oscuridad",  "naturaleza"), 1.2f },  // oscuridad corrompe naturaleza
                { ("oscuridad",  "agua"),        1.2f }, // oscuridad envenena el agua
                { ("luz",        "fuego"),       1.2f }, // luz potencia el fuego
            };

        // ── Shield HP tracker ─────────────────────────────────────────────────
        // Clave: heroId o enemyId. Valor: HP de escudo restante.

        private readonly Dictionary<string, int> _shieldHp = new Dictionary<string, int>();

        // ── Singleton DDOL ────────────────────────────────────────────────────

        private static CombatSystem _instance;

        // ── ISystem ───────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            if (GameManager.Instance != null)
                GameManager.Instance.RegisterSystem(this);
        }

        public void Initialize()
        {
            _shieldHp.Clear();
        }

        public void OnSessionStart() { }
        public void OnSessionEnd()   { _shieldHp.Clear(); }

        // ── API pública ───────────────────────────────────────────────────────

        /// Ejecuta el combate completo y devuelve el resultado.
        /// Modifica hpActual y efectosActivos de las instancias en ctx.
        public CombatResult ProcessCombat(CombatContext ctx)
        {
            _shieldHp.Clear();

            var result = new CombatResult
            {
                victoria    = false,
                danoTotal   = 0,
                drops       = Array.Empty<string>(),
                xpGanada    = 0,
                trofeosDelta = 0,
                gradoObtenido = "C"
            };

            // Construir lista de unidades con tipo y referencia
            var turnQueue = BuildTurnQueue(ctx);

            int turno = 0;
            while (turno < MAX_TURNS)
            {
                turno++;
                bool playerTeamAlive = AnyAlive(ctx.playerTeam);
                bool enemyTeamAlive  = AnyAlive(ctx.enemyTeam);

                if (!playerTeamAlive || !enemyTeamAlive) break;

                foreach (var unit in turnQueue)
                {
                    if (!unit.IsAlive) continue;

                    // Tick de efectos al inicio del turno de esta unidad
                    TickEffects(unit, ctx);

                    if (!unit.IsAlive) continue;

                    // Stun: saltar turno
                    if (HasEffect(unit, TipoEfecto.Stun.ToString())) continue;

                    // Elegir objetivo
                    if (unit.IsHero)
                    {
                        var target = PickFirstAlive(ctx.enemyTeam);
                        if (target == null) break;

                        var dmgResult = CalculateDamage(unit.Hero, target, 1f);
                        int actualDmg = ApplyShieldAndDamage(target.enemyId, ref target.hpActual, dmgResult.dañoFinal);
                        result.danoTotal += actualDmg;

                        if (target.hpActual <= 0)
                        {
                            target.estaVivo = false;
                            EventBus.Publish(new UnitDefeatedData { unitType = "enemy", unitId = target.enemyId });
                        }
                        else
                        {
                            EventBus.Publish(new UnitDamagedData
                            {
                                unitType  = "enemy",
                                unitId    = target.enemyId,
                                resultado = dmgResult,
                                hpActual  = target.hpActual,
                                hpMax     = target.hpMax
                            });
                        }
                    }
                    else
                    {
                        var target = PickFirstAlive(ctx.playerTeam);
                        if (target == null) break;

                        var dmgResult = CalculateDamageEnemyAttack(unit.Enemy, target);
                        int actualDmg = ApplyShieldAndDamage(target.heroId, ref target.hpActual, dmgResult.dañoFinal);

                        if (target.hpActual <= 0)
                        {
                            target.estaVivo = false;
                            EventBus.Publish(new UnitDefeatedData { unitType = "hero", unitId = target.heroId });
                        }
                        else
                        {
                            EventBus.Publish(new UnitDamagedData
                            {
                                unitType  = "hero",
                                unitId    = target.heroId,
                                resultado = dmgResult,
                                hpActual  = target.hpActual,
                                hpMax     = target.hpMax
                            });
                        }
                    }
                }

                EventBus.Publish(new CombatTurnEndData
                {
                    turnoActual      = turno,
                    esEquipoJugador  = false
                });
            }

            result.victoria = AnyAlive(ctx.playerTeam) && !AnyAlive(ctx.enemyTeam);
            result.gradoObtenido = CalcGrade(turno, ctx);
            return result;
        }

        /// Calcula el daño de un héroe atacando a un enemigo.
        /// skillMultiplier: 1.0 para ataque básico, >1.0 para habilidades.
        public DamageResult CalculateDamage(HeroInstance atacante, EnemyInstance defensor, float skillMultiplier)
        {
            var res = new DamageResult();

            // PASO 1 — DODGE
            float chanceDodge = Mathf.Clamp((defensor.agi - atacante.agi) * 0.001f, DODGE_MIN, DODGE_MAX);
            if (UnityEngine.Random.value < chanceDodge)
            {
                res.fueEsquivado = true;
                res.dañoFinal    = 0;
                return res;
            }

            // PASO 2 — CRIT
            float chanceCrit = Mathf.Clamp01(atacante.crit / 100f + atacante.luk * 0.002f);
            bool  esCritico  = UnityEngine.Random.value < chanceCrit;
            float multCrit   = esCritico ? (1f + atacante.critDmg / 100f) : 1f;
            res.fueCritico   = esCritico;

            // PASO 3 — BASE
            float dañoBase = atacante.atk * skillMultiplier / (1f + defensor.def / K) * multCrit;
            dañoBase = Mathf.Max(1f, dañoBase);

            // PASO 4 — ELEMENTAL
            float multElem = GetElementalMultiplier(atacante.elemento, defensor.elemento);
            if (multElem > 1f)       res.fueElementalVentaja    = true;
            else if (multElem < 1f)  res.fueElementalDesventaja = true;
            dañoBase *= multElem;

            res.dañoFinal = Mathf.Max(1, Mathf.RoundToInt(dañoBase));

            // PASO 5 — EFECTO (intento sobre el enemigo)
            // El llamador puede pasar un efectoId; aquí dejamos efectoAplicado null
            // para que ProcessCombat decida según la habilidad usada.
            // Para ataques básicos no se aplica efecto automático.
            res.efectoAplicado = null;

            return res;
        }

        /// Calcula el daño de un enemigo atacando a un héroe.
        public DamageResult CalculateDamageEnemyAttack(EnemyInstance atacante, HeroInstance defensor)
        {
            var res = new DamageResult();

            // PASO 1 — DODGE (héroe esquiva al enemigo)
            float chanceDodge = Mathf.Clamp((defensor.agi - atacante.agi) * 0.001f, DODGE_MIN, DODGE_MAX);
            if (UnityEngine.Random.value < chanceDodge)
            {
                res.fueEsquivado = true;
                res.dañoFinal    = 0;
                return res;
            }

            // PASO 2 — CRIT (enemigos no tienen stat de crit → 5 % base)
            bool  esCritico = UnityEngine.Random.value < 0.05f;
            float multCrit  = esCritico ? 1.5f : 1f;
            res.fueCritico  = esCritico;

            // PASO 3 — BASE
            float dañoBase = atacante.atk / (1f + defensor.def / K) * multCrit;
            dañoBase = Mathf.Max(1f, dañoBase);

            // PASO 4 — ELEMENTAL
            float multElem = GetElementalMultiplier(atacante.elemento, defensor.elemento);
            if (multElem > 1f)       res.fueElementalVentaja    = true;
            else if (multElem < 1f)  res.fueElementalDesventaja = true;
            dañoBase *= multElem;

            res.dañoFinal = Mathf.Max(1, Mathf.RoundToInt(dañoBase));
            return res;
        }

        /// Intenta aplicar un efecto a los efectosActivos del objetivo.
        /// Respeta el límite de 3 stacks. Devuelve true si se aplicó.
        /// attackerAcc y defenderRes en puntos (ej: 80 = 80 %).
        public bool TryApplyEffect(TipoEfecto efecto, List<string> efectosActivos,
                                   int attackerAcc, int defenderRes)
        {
            float chance = Mathf.Clamp(attackerAcc / 100f - defenderRes / 100f,
                                       EFFECT_MIN, EFFECT_MAX);
            return TryApplyEffect(efecto, efectosActivos, chance);
        }

        /// Intenta aplicar un efecto con una probabilidad explícita (0–1), tal como
        /// viene definida en el campo "chance" de un SkillEffect del catálogo.
        /// Respeta el mismo límite de 3 stacks que la sobrecarga basada en ACC/RES.
        public bool TryApplyEffect(TipoEfecto efecto, List<string> efectosActivos, float chance01)
        {
            if (UnityEngine.Random.value >= Mathf.Clamp01(chance01)) return false;

            string id = efecto.ToString();
            int stacks = CountStacks(efectosActivos, id);
            if (stacks >= MAX_STACKS) return false;

            efectosActivos.Add(id);
            return true;
        }

        /// Aplica el tick de efectos periódicos (DoT/Regen) de inicio de turno a un héroe.
        /// Wrapper público para uso desde controllers de UI (CombatSceneController).
        public void TickEffects(HeroInstance hero) => TickEffects(new TurnUnit(hero), null);

        /// Aplica el tick de efectos periódicos (DoT/Regen) de inicio de turno a un enemigo.
        /// Wrapper público para uso desde controllers de UI (CombatSceneController).
        public void TickEffects(EnemyInstance enemy) => TickEffects(new TurnUnit(enemy), null);

        /// Indica si la unidad tiene el efecto Stun activo (debe saltar su turno).
        public static bool HasStun(HeroInstance hero) =>
            hero?.efectosActivos != null && hero.efectosActivos.Contains(TipoEfecto.Stun.ToString());

        /// Indica si la unidad tiene el efecto Stun activo (debe saltar su turno).
        public static bool HasStun(EnemyInstance enemy) =>
            enemy?.efectosActivos != null && enemy.efectosActivos.Contains(TipoEfecto.Stun.ToString());

        /// Elimina UNA instancia (stack) del efecto de la lista.
        /// Bleed NUNCA puede ser removido → devuelve false.
        public bool RemoveEffect(TipoEfecto efecto, List<string> efectosActivos)
        {
            if (efecto == TipoEfecto.Bleed) return false;

            string id = efecto.ToString();
            return efectosActivos.Remove(id);
        }

        // ── Elemental ─────────────────────────────────────────────────────────

        public float GetElementalMultiplier(string atacante, string defensor)
        {
            if (string.IsNullOrEmpty(atacante) || string.IsNullOrEmpty(defensor))
                return 1f;

            string a = atacante.ToLowerInvariant();
            string d = defensor.ToLowerInvariant();

            if (a == d) return SAME_ELEMENT;

            return _elementalTable.TryGetValue((a, d), out float mult) ? mult : 1f;
        }

        // ── Helpers privados ───────────────────────────────────────────────────

        private List<TurnUnit> BuildTurnQueue(CombatContext ctx)
        {
            var list = new List<TurnUnit>();
            foreach (var h in ctx.playerTeam)
                if (h.estaVivo) list.Add(new TurnUnit(h));
            foreach (var e in ctx.enemyTeam)
                if (e.estaVivo) list.Add(new TurnUnit(e));

            // Ordenar por SPD descendente; empates: héroe antes que enemigo
            list.Sort((a, b) =>
            {
                int spdA = a.IsHero ? a.Hero.spd : a.Enemy.spd;
                int spdB = b.IsHero ? b.Hero.spd : b.Enemy.spd;
                if (spdB != spdA) return spdB.CompareTo(spdA);
                // Empate: jugador primero
                return a.IsHero ? -1 : 1;
            });
            return list;
        }

        private static bool AnyAlive(HeroInstance[] team)
        {
            foreach (var h in team) if (h.estaVivo) return true;
            return false;
        }

        private static bool AnyAlive(EnemyInstance[] team)
        {
            foreach (var e in team) if (e.estaVivo) return true;
            return false;
        }

        private static HeroInstance PickFirstAlive(HeroInstance[] team)
        {
            foreach (var h in team) if (h.estaVivo) return h;
            return null;
        }

        private static EnemyInstance PickFirstAlive(EnemyInstance[] team)
        {
            foreach (var e in team) if (e.estaVivo) return e;
            return null;
        }

        private static int CountStacks(List<string> efectos, string id)
        {
            int c = 0;
            foreach (var e in efectos) if (e == id) c++;
            return c;
        }

        private static bool HasEffect(TurnUnit unit, string efectoId)
        {
            var list = unit.IsHero ? unit.Hero.efectosActivos : unit.Enemy.efectosActivos;
            if (list == null) return false;
            return list.Contains(efectoId);
        }

        /// Aplica shield primero, luego HP. Devuelve daño real aplicado a HP.
        private int ApplyShieldAndDamage(string unitId, ref int hpActual, int daño)
        {
            if (daño <= 0) return 0;

            if (_shieldHp.TryGetValue(unitId, out int shield) && shield > 0)
            {
                int absorbed = Mathf.Min(shield, daño);
                _shieldHp[unitId] = shield - absorbed;
                daño -= absorbed;
            }

            if (daño <= 0) return 0;

            hpActual = Mathf.Max(0, hpActual - daño);
            return daño;
        }

        /// Activa efectos periódicos al inicio del turno de una unidad.
        private void TickEffects(TurnUnit unit, CombatContext ctx)
        {
            var efectos = unit.IsHero
                ? unit.Hero.efectosActivos
                : unit.Enemy.efectosActivos;

            if (efectos == null || efectos.Count == 0) return;

            // DoT — Bleed / Burn / Poison: 1 stack de daño por turno
            TickDot(unit, efectos, TipoEfecto.Bleed.ToString(),  0.08f, ctx);
            TickDot(unit, efectos, TipoEfecto.Burn.ToString(),   0.06f, ctx);
            TickDot(unit, efectos, TipoEfecto.Poison.ToString(), 0.05f, ctx);

            // Regen: recupera HP
            if (efectos.Contains(TipoEfecto.Regen.ToString()))
            {
                int hpMax = unit.IsHero ? unit.Hero.hpMax : unit.Enemy.hpMax;
                int heal  = Mathf.RoundToInt(hpMax * 0.05f);
                if (unit.IsHero)
                    unit.Hero.hpActual = Mathf.Min(unit.Hero.hpMax, unit.Hero.hpActual + heal);
                else
                    unit.Enemy.hpActual = Mathf.Min(unit.Enemy.hpMax, unit.Enemy.hpActual + heal);
            }
        }

        private void TickDot(TurnUnit unit, List<string> efectos, string id, float ratio, CombatContext ctx)
        {
            if (!efectos.Contains(id)) return;

            int hpMax = unit.IsHero ? unit.Hero.hpMax : unit.Enemy.hpMax;
            int dmg   = Mathf.Max(1, Mathf.RoundToInt(hpMax * ratio));

            string unitId;
            if (unit.IsHero)
            {
                unitId = unit.Hero.heroId;
                ApplyShieldAndDamage(unitId, ref unit.Hero.hpActual, dmg);
                if (unit.Hero.hpActual <= 0)
                {
                    unit.Hero.estaVivo = false;
                    EventBus.Publish(new UnitDefeatedData { unitType = "hero", unitId = unitId });
                }
            }
            else
            {
                unitId = unit.Enemy.enemyId;
                ApplyShieldAndDamage(unitId, ref unit.Enemy.hpActual, dmg);
                if (unit.Enemy.hpActual <= 0)
                {
                    unit.Enemy.estaVivo = false;
                    EventBus.Publish(new UnitDefeatedData { unitType = "enemy", unitId = unitId });
                }
            }
        }

        private static string CalcGrade(int turnosUsados, CombatContext ctx)
        {
            if (turnosUsados <= 5)  return "S";
            if (turnosUsados <= 10) return "A";
            if (turnosUsados <= 20) return "B";
            return "C";
        }

        // ── Clase auxiliar interna ─────────────────────────────────────────────

        private class TurnUnit
        {
            public HeroInstance  Hero  { get; }
            public EnemyInstance Enemy { get; }
            public bool IsHero => Hero != null;

            public bool IsAlive => IsHero ? Hero.estaVivo : Enemy.estaVivo;

            public TurnUnit(HeroInstance h)  { Hero  = h; }
            public TurnUnit(EnemyInstance e) { Enemy = e; }
        }
    }
}
