#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using ReinoOscuridad.Data;
using ReinoOscuridad.Firebase;
using ReinoOscuridad.UI.Campaign;

[TestFixture]
public class CampaignFlowTests
{
    [UnitySetUp]
    public IEnumerator Setup()
    {
        DataStorageSystem.IsDevMode = true;
        SceneManager.LoadScene("CampaignScene");
        yield return new WaitForSeconds(2f);
    }

    [UnityTest]
    public IEnumerator CampaignScene_Loads()
    {
        yield return new WaitForSeconds(1f);
        Assert.AreEqual("CampaignScene",
            SceneManager.GetActiveScene().name,
            "CampaignScene debe estar activa");
    }

    [UnityTest]
    public IEnumerator Mundo1_IsUnlocked()
    {
        yield return new WaitForSeconds(1f);
        var controller = Object.FindAnyObjectByType<CampaignSceneController>();
        Assert.IsNotNull(controller, "CampaignSceneController debe existir");
        // Mundo 0 siempre está desbloqueado.
        bool desbloqueado = controller.IsMundoDesbloqueado(0);
        Assert.IsTrue(desbloqueado, "Mundo 1 debe estar desbloqueado");
    }

    [UnityTest]
    public IEnumerator Mundo2_IsLockedInitially()
    {
        yield return new WaitForSeconds(1f);
        var controller = Object.FindAnyObjectByType<CampaignSceneController>();
        Assert.IsNotNull(controller);
        bool desbloqueado = controller.IsMundoDesbloqueado(1);
        Assert.IsFalse(desbloqueado,
            "Mundo 2 debe estar bloqueado inicialmente");
    }

    [UnityTest]
    public IEnumerator CombatContext_NotNullBeforeCombat()
    {
        yield return new WaitForSeconds(1f);
        var ctx = new CombatContext
        {
            encounterID = "campaign_mundo_1_f1_normal",
            callerScene = "CampaignScene",
            combatMode  = "campaign",
            playerTeam  = new HeroInstance[0],
            enemyTeam   = new EnemyInstance[0]
        };
        CombatSceneData.PendingContext = ctx;
        Assert.IsNotNull(CombatSceneData.PendingContext,
            "PendingContext no debe ser null");
    }

    [UnityTest]
    public IEnumerator PrepareRepeat_RestoresEnemyHP()
    {
        yield return null;
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

        // LastContext tiene setter privado — poblar via SetResult.
        CombatSceneData.PendingContext = new CombatContext
        {
            encounterID = "test",
            enemyTeam   = new[] { enemy },
            playerTeam  = new HeroInstance[0]
        };
        CombatSceneData.SetResult(new CombatResult());
        CombatSceneData.PrepareRepeat();

        Assert.IsNotNull(CombatSceneData.PendingContext);
        Assert.AreEqual(5000,
            CombatSceneData.PendingContext.enemyTeam[0].hpActual,
            "HP del enemigo debe estar restaurado");
        Assert.IsTrue(
            CombatSceneData.PendingContext.enemyTeam[0].estaVivo,
            "Enemigo debe estar vivo al repetir");
    }
}
#endif
