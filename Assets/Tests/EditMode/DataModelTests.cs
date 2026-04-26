#if UNITY_EDITOR
using NUnit.Framework;
using ReinoOscuridad.Data;

[TestFixture]
public class DataModelTests
{
    [Test]
    public void CombatContext_Instantiation()
    {
        var ctx = new CombatContext
        {
            encounterID  = "test_001",
            callerScene  = "CampaignScene",
            combatMode   = "campaign"
        };
        Assert.AreEqual("test_001",       ctx.encounterID);
        Assert.AreEqual("CampaignScene",  ctx.callerScene);
    }

    [Test]
    public void HeroInstance_DefaultValues()
    {
        var hero = new HeroInstance
        {
            heroId   = "test_hero",
            hpActual = 10000,
            hpMax    = 10000,
            atk      = 3000,
            def      = 1000,
            spd      = 120,
            agi      = 100,
            estaVivo = true
        };
        Assert.IsTrue(hero.estaVivo);
        Assert.AreEqual(10000, hero.hpActual);
        Assert.AreEqual(3000,  hero.atk);
    }

    [Test]
    public void EnemyInstance_DefaultValues()
    {
        var enemy = new EnemyInstance
        {
            enemyId  = "enemy_001",
            nombre   = "Goblin",
            hpActual = 5000,
            hpMax    = 5000,
            atk      = 1500,
            def      = 600,
            spd      = 80,
            agi      = 50,
            estaVivo = true
        };
        Assert.IsTrue(enemy.estaVivo);
        Assert.AreEqual("Goblin", enemy.nombre);
    }

    [Test]
    public void CombatResult_VictoriaFlag()
    {
        var result = new CombatResult
        {
            victoria  = true,
            xpGanada  = 500,
            danoTotal = 10000
        };
        Assert.IsTrue(result.victoria);
        Assert.AreEqual(500, result.xpGanada);
    }

    [Test]
    public void CombatSceneData_PrepareRepeat_RestoresEnemyHP()
    {
        var enemy = new EnemyInstance
        {
            enemyId  = "e1",
            hpActual = 0,
            hpMax    = 5000,
            estaVivo = false,
            atk      = 100,
            def      = 100,
            spd      = 80,
            agi      = 50
        };

        // LastContext tiene setter privado — usamos SetResult para poblarlo.
        CombatSceneData.PendingContext = new CombatContext
        {
            encounterID = "test",
            enemyTeam   = new[] { enemy },
            playerTeam  = new HeroInstance[0]
        };
        CombatSceneData.SetResult(new CombatResult());

        CombatSceneData.PrepareRepeat();

        Assert.IsNotNull(CombatSceneData.PendingContext);
        var restoredEnemy = CombatSceneData.PendingContext.enemyTeam[0];
        Assert.AreEqual(5000, restoredEnemy.hpActual);
        Assert.IsTrue(restoredEnemy.estaVivo);
    }

    [Test]
    public void GearInstance_SubstatEntry_IsStruct()
    {
        var substat = new SubstatEntry
        {
            statName  = "ATK%",
            valor     = 6,
            revelado  = true
        };
        Assert.AreEqual("ATK%", substat.statName);
        Assert.AreEqual(6,      substat.valor);
        Assert.IsTrue(substat.revelado);
    }
}
#endif
