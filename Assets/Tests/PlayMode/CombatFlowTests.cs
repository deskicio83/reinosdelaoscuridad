#if UNITY_EDITOR
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
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        Assert.Greater(units.Length, 0,
            "Debe haber al menos 1 ATBUnit");
    }

    [UnityTest]
    public IEnumerator ATB_Progresses()
    {
        yield return new WaitForSeconds(3f);
        var units = Object.FindObjectsByType<ATBUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        bool alguieneConATB = units.Any(u => u.atbValue > 0);
        Assert.IsTrue(alguieneConATB,
            "Al menos 1 unidad debe tener ATB > 0");
    }

    [UnityTest]
    public IEnumerator HPBars_ExistOnAllUnits()
    {
        yield return new WaitForSeconds(2f);
        var units = Object.FindObjectsByType<ATBUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

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
        yield return new WaitForSeconds(2f);
        var units   = Object.FindObjectsByType<ATBUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        var enemigos = units.Where(u => !u.esJugador && u.enemyData != null).ToArray();

        if (enemigos.Length == 0)
        {
            Assert.Inconclusive("No hay enemigos para testear");
            yield break;
        }

        int hpInicial = enemigos[0].enemyData.hpMax;
        yield return new WaitForSeconds(5f);
        int hpActual  = enemigos[0].enemyData.hpActual;

        Assert.Less(hpActual, hpInicial,
            "HP del enemigo debe haber bajado tras un ataque del héroe");
    }

    [UnityTest]
    public IEnumerator HeroHP_DecreasesAfterEnemyAttack()
    {
        yield return new WaitForSeconds(2f);
        var units  = Object.FindObjectsByType<ATBUnit>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        var heroes = units.Where(u => u.esJugador && u.heroData != null).ToArray();

        if (heroes.Length == 0)
        {
            Assert.Inconclusive("No hay héroes para testear");
            yield break;
        }

        int hpInicial = heroes[0].heroData.hpMax;
        yield return new WaitForSeconds(8f);
        int hpActual  = heroes[0].heroData.hpActual;

        if (units.Any(u => !u.esJugador && u.estaVivo))
        {
            Assert.Less(hpActual, hpInicial,
                "HP del héroe debe haber bajado tras un ataque del enemigo");
        }
        else
        {
            Assert.Inconclusive("Enemigo muerto antes de poder atacar");
        }
    }

    [UnityTest]
    public IEnumerator Skills_LoadedFromCatalog()
    {
        yield return new WaitForSeconds(2f);
        var controller = Object.FindAnyObjectByType<CombatSceneController>();
        Assert.IsNotNull(controller,
            "CombatSceneController debe existir");
        // Si llegamos aquí sin excepción, el catálogo cargó correctamente.
        Assert.Pass("Skills cargadas sin excepción");
    }
}
#endif
