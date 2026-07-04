#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ReinoOscuridad.Data;
using ReinoOscuridad.Systems;

[TestFixture]
public class CombatSystemTests
{
    private CombatSystem _system;
    private HeroInstance _heroe;
    private EnemyInstance _enemigo;

    [SetUp]
    public void Setup()
    {
        var go = new GameObject("CombatSystem_Test");
        _system = go.AddComponent<CombatSystem>();

        _heroe = new HeroInstance
        {
            heroId   = "h1",
            atk      = 3000,
            def      = 1000,
            spd      = 120,
            agi      = 100,
            crit     = 0,
            critDmg  = 150,
            acc      = 80,
            res      = 20,
            luk      = 50,
            elemento = "fuego",
            hpActual = 10000,
            hpMax    = 10000,
            estaVivo = true,
            efectosActivos = new List<string>()
        };
        _enemigo = new EnemyInstance
        {
            enemyId  = "e1",
            atk      = 1500,
            def      = 600,
            spd      = 80,
            agi      = 50,
            elemento = "naturaleza",
            hpActual = 5000,
            hpMax    = 5000,
            estaVivo = true,
            efectosActivos = new List<string>()
        };
    }

    [TearDown]
    public void TearDown()
    {
        if (_system != null)
            Object.DestroyImmediate(_system.gameObject);
    }

    [Test]
    public void CalculateDamage_ReturnsPositiveDamage()
    {
        var result = _system.CalculateDamage(_heroe, _enemigo, 1f);
        Assert.Greater(result.dañoFinal, 0, "El daño debe ser mayor que 0");
    }

    [Test]
    public void CalculateDamage_MinimumDamageIsOne()
    {
        _heroe.atk   = 1;
        _enemigo.def = 99999;
        // Dodge mín=5%: iterar hasta obtener un golpe no esquivado.
        for (int i = 0; i < 50; i++)
        {
            var result = _system.CalculateDamage(_heroe, _enemigo, 1f);
            if (!result.fueEsquivado)
            {
                Assert.GreaterOrEqual(result.dañoFinal, 1,
                    "El daño mínimo (sin esquive) debe ser 1");
                return;
            }
        }
        Assert.Inconclusive("50 intentos todos esquivados");
    }

    [Test]
    public void ElementalAdvantage_FuegoVsNaturaleza()
    {
        // Testear la tabla elemental directamente — CalculateDamage
        // puede esquivar antes de llegar al paso elemental (dodge 5% mín).
        float mult = _system.GetElementalMultiplier("fuego", "naturaleza");
        Assert.Greater(mult, 1f,
            "Fuego vs Naturaleza debe ser ventaja (mult > 1)");
    }

    [Test]
    public void ElementalDisadvantage_FuegoVsAgua()
    {
        float mult = _system.GetElementalMultiplier("fuego", "agua");
        Assert.Less(mult, 1f,
            "Fuego vs Agua debe ser desventaja (mult < 1)");
    }

    [Test]
    public void ElementalNeutral_FuegoVsRayo()
    {
        float mult = _system.GetElementalMultiplier("fuego", "rayo");
        Assert.AreEqual(1f, mult, 0.001f,
            "Fuego vs Rayo debe ser neutro (mult = 1)");
    }

    [Test]
    public void Bleed_CannotBeRemovedByCleanse()
    {
        // Regla irrompible: Bleed no puede ser removido por Cleanse.
        _heroe.efectosActivos = new List<string> { "Bleed" };
        _system.RemoveEffect(TipoEfecto.Bleed, _heroe.efectosActivos);
        Assert.IsTrue(_heroe.efectosActivos.Contains("Bleed"),
            "Bleed NO debe ser removido por Cleanse");
    }

    [Test]
    public void CritMultiplier_IncreasesWithCritDmg()
    {
        // Con crit garantizado, critDmg alto debe dar más daño.
        // Dodge mín=5%: reintentar hasta obtener un par de golpes no esquivados.
        _heroe.crit = 100;

        for (int i = 0; i < 50; i++)
        {
            _heroe.critDmg = 200;
            var resultHighCrit = _system.CalculateDamage(_heroe, _enemigo, 1f);
            if (resultHighCrit.fueEsquivado) continue;

            _heroe.critDmg = 50;
            var resultLowCrit = _system.CalculateDamage(_heroe, _enemigo, 1f);
            if (resultLowCrit.fueEsquivado) continue;

            Assert.GreaterOrEqual(resultHighCrit.dañoFinal, resultLowCrit.dañoFinal,
                "Mayor critDmg debe producir más daño con crit garantizado");
            return;
        }
        Assert.Inconclusive("50 intentos todos esquivados");
    }

    [Test]
    public void TryApplyEffect_AccResChance_StaysWithinTheoreticalRate()
    {
        // acc=80, res=20 fijo → chance teórica = clamp(0.8-0.2, 0.10, 0.90) = 0.6
        const int intentos = 300;
        int aplicados = 0;
        for (int i = 0; i < intentos; i++)
        {
            var efectos = new List<string>();
            if (_system.TryApplyEffect(TipoEfecto.Bleed, efectos, _heroe.acc, 20))
                aplicados++;
        }
        float ratio = (float)aplicados / intentos;
        Assert.That(ratio, Is.InRange(0.4f, 0.8f),
            $"La tasa de aplicación ({ratio:P0}) debe acercarse a la chance teórica (~60%)");
    }

    // ── Sprint 0 — wiring de efectos al combate real ────────────────────────
    // Estos tests cubren la API pública añadida para que CombatSceneController
    // pueda invocar la lógica de efectos que antes solo se ejecutaba desde
    // CombatSystem.ProcessCombat() (nunca llamado en producción).

    [Test]
    public void TryApplyEffect_ExplicitChance_AppliesWhenGuaranteed()
    {
        bool applied = _system.TryApplyEffect(
            TipoEfecto.Stun, _enemigo.efectosActivos, 1f);
        Assert.IsTrue(applied, "Con chance=1 el efecto debe aplicarse siempre");
        Assert.IsTrue(_enemigo.efectosActivos.Contains("Stun"));
    }

    [Test]
    public void TryApplyEffect_ExplicitChance_NeverAppliesWhenZero()
    {
        bool applied = _system.TryApplyEffect(
            TipoEfecto.Poison, _enemigo.efectosActivos, 0f);
        Assert.IsFalse(applied, "Con chance=0 el efecto nunca debe aplicarse");
        Assert.IsFalse(_enemigo.efectosActivos.Contains("Poison"));
    }

    [Test]
    public void TryApplyEffect_ExplicitChance_RespectsStackLimit()
    {
        // MAX_STACKS = 3 — el cuarto intento con chance=1 no debe añadirse.
        for (int i = 0; i < 4; i++)
            _system.TryApplyEffect(TipoEfecto.Burn, _enemigo.efectosActivos, 1f);

        int stacks = _enemigo.efectosActivos.FindAll(e => e == "Burn").Count;
        Assert.AreEqual(3, stacks, "No debe superar el límite de 3 stacks");
    }

    [Test]
    public void TickEffects_HeroInstance_BleedDealsDamageAndIsNotRemovable()
    {
        _heroe.efectosActivos.Add("Bleed");
        int hpAntes = _heroe.hpActual;

        _system.TickEffects(_heroe);

        Assert.Less(_heroe.hpActual, hpAntes,
            "Bleed debe restar HP en el tick de inicio de turno");
        Assert.IsTrue(_heroe.efectosActivos.Contains("Bleed"),
            "El tick no debe consumir/remover el Bleed");
    }

    [Test]
    public void TickEffects_EnemyInstance_RegenHealsUpToMax()
    {
        _enemigo.hpActual = _enemigo.hpMax - 100;
        _enemigo.efectosActivos.Add("Regen");

        _system.TickEffects(_enemigo);

        Assert.Greater(_enemigo.hpActual, _enemigo.hpMax - 100,
            "Regen debe curar HP en el tick");
        Assert.LessOrEqual(_enemigo.hpActual, _enemigo.hpMax,
            "Regen no debe superar el HP máximo");
    }

    [Test]
    public void HasStun_DetectsStunOnHeroAndEnemy()
    {
        Assert.IsFalse(CombatSystem.HasStun(_heroe), "Sin Stun no debe detectarlo");

        _heroe.efectosActivos.Add("Stun");
        _enemigo.efectosActivos.Add("Stun");

        Assert.IsTrue(CombatSystem.HasStun(_heroe));
        Assert.IsTrue(CombatSystem.HasStun(_enemigo));
    }
}
#endif
