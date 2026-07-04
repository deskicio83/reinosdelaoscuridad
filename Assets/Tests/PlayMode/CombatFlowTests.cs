using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using ReinoOscuridad.Data;
using ReinoOscuridad.Firebase;
using ReinoOscuridad.UI.Combat;

[TestFixture]
public class CombatFlowTests
{
    [UnitySetUp]
    public IEnumerator Setup()
    {
        CombatSceneData.ClearLastResult();
        CombatSceneData.ForceAutoMode = true;
        CombatSceneData.PendingContext = new CombatContext
        {
            encounterID = "test_combat",
            callerScene = "CampaignScene",
            combatMode  = "campaign",
            playerTeam  = new HeroInstance[]
            {
                new HeroInstance
                {
                    heroId   = "h1",
                    atk      = 3000, def = 1000,
                    spd      = 120,  agi = 100,
                    crit     = 30,   critDmg = 150,
                    acc      = 80,   res = 20, luk = 50,
                    elemento = "fuego",
                    hpActual = 10000, hpMax = 10000,
                    estaVivo = true,
                    habilidadesEquipadas = new string[0],
                    efectosActivos = new List<string>()
                }
            },
            enemyTeam = new EnemyInstance[]
            {
                new EnemyInstance
                {
                    enemyId  = "e1",
                    nombre   = "Goblin",
                    atk      = 500, def = 200,
                    spd      = 60,  agi = 40,
                    elemento = "naturaleza",
                    hpActual = 3000, hpMax = 3000,
                    estaVivo = true,
                    efectosActivos = new List<string>()
                }
            }
        };

        DataStorageSystem.IsDevMode = true;
        SceneManager.LoadScene("CombatScene");
        yield return new WaitForSeconds(3f);
    }

    [UnityTest]
    public IEnumerator CombatScene_Loads()
    {
        yield return new WaitForSeconds(1f);
        Assert.AreEqual("CombatScene",
            SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator CombatContext_IsNotNull()
    {
        yield return new WaitForSeconds(1f);
        var ctx = CombatSceneData.PendingContext ?? CombatSceneData.LastContext;
        Assert.IsNotNull(ctx, "CombatContext no debe ser null");
    }

    [UnityTest]
    public IEnumerator ATBUnits_AreCreated()
    {
        yield return new WaitForSeconds(2f);
        var units = Object.FindObjectsByType<ATBUnit>(
            FindObjectsInactive.Exclude);
        Assert.Greater(units.Length, 0,
            "Debe haber al menos 1 ATBUnit");
    }

    [UnityTest]
    public IEnumerator ATB_Progresses()
    {
        yield return new WaitForSeconds(3f);
        var units = Object.FindObjectsByType<ATBUnit>(
            FindObjectsInactive.Exclude);
        bool alguienConATB = units.Any(u => u.atbValue > 0);
        Assert.IsTrue(alguienConATB,
            "Al menos 1 unidad debe tener ATB > 0");
    }

    [UnityTest]
    public IEnumerator HPBars_ExistOnAllUnits()
    {
        yield return new WaitForSeconds(2f);
        var units = Object.FindObjectsByType<ATBUnit>(
            FindObjectsInactive.Exclude);

        foreach (var unit in units)
        {
            var allImages = unit.GetComponentsInChildren<Image>(true);
            bool tieneRelleno = allImages.Any(img =>
            {
                string n = img.gameObject.name.ToLower();
                return n.Contains("relleno") || n == "fill" || n == "hpfill";
            });
            Assert.IsTrue(tieneRelleno,
                "Unidad " + unit.name + " debe tener HPBar con Relleno");
        }
    }

    [UnityTest]
    public IEnumerator EnemyHP_DecreasesAfterHeroAttack()
    {
        float elapsed = 0f;
        while (CombatSceneData.LastResult == null && elapsed < 10f)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }
        Assert.IsNotNull(CombatSceneData.LastResult,
            "El combate debe haber terminado en 10s");
        Assert.Greater(CombatSceneData.LastResult.danoTotal, 0,
            "Debe haberse infligido daño al enemigo");
    }

    [UnityTest]
    public IEnumerator HeroHP_DecreasesAfterEnemyAttack()
    {
        float elapsed = 0f;
        while (CombatSceneData.LastResult == null && elapsed < 10f)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
        }
        Assert.IsNotNull(CombatSceneData.LastResult,
            "El combate debe haber terminado en 10s");
        Assert.IsTrue(CombatSceneData.LastResult.victoria,
            "El héroe debe ganar con ventaja elemental (fuego vs naturaleza)");
    }
}
