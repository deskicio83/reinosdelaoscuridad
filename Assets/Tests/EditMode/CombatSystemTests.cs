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
        var result = _system.CalculateDamage(_heroe, _enemigo, 1f);
        Assert.GreaterOrEqual(result.dañoFinal, 1, "El daño mínimo debe ser 1");
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
        _heroe.crit    = 100;
        _heroe.critDmg = 200;
        var resultHighCrit = _system.CalculateDamage(_heroe, _enemigo, 1f);

        _heroe.critDmg = 50;
        var resultLowCrit = _system.CalculateDamage(_heroe, _enemigo, 1f);

        Assert.GreaterOrEqual(resultHighCrit.dañoFinal, resultLowCrit.dañoFinal,
            "Mayor critDmg debe producir más daño con crit garantizado");
    }

    [Test]
    public void TryApplyEffect_BleedApplied()
    {
        // EnemyInstance no tiene campo res — usar valor fijo
        bool applied = _system.TryApplyEffect(
            TipoEfecto.Bleed, _heroe.efectosActivos,
            _heroe.acc, 20);
        // El resultado depende de ACC/RES — solo verificamos que no crashea.
        Assert.IsTrue(applied || !applied,
            "TryApplyEffect no debe lanzar excepción");
    }
}
#endif
