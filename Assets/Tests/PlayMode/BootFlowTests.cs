#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using ReinoOscuridad.Core;
using ReinoOscuridad.Systems;
using ReinoOscuridad.Firebase;

[TestFixture]
public class BootFlowTests
{
    [UnitySetUp]
    public IEnumerator Setup()
    {
        DataStorageSystem.IsDevMode = true;
        SceneManager.LoadScene("BootScene");
        yield return new WaitForSeconds(2f);
    }

    [UnityTest]
    public IEnumerator GameManager_ExistsAfterBoot()
    {
        yield return new WaitForSeconds(1f);
        Assert.IsNotNull(GameManager.Instance,
            "GameManager debe existir tras el boot");
    }

    [UnityTest]
    public IEnumerator UIManager_ExistsAfterBoot()
    {
        yield return new WaitForSeconds(1f);
        Assert.IsNotNull(UIManager.Instance,
            "UIManager debe existir tras el boot");
    }

    [UnityTest]
    public IEnumerator PlayerDataSystem_LoadedAfterBoot()
    {
        yield return new WaitForSeconds(1f);
        Assert.IsNotNull(PlayerDataSystem.Instance,
            "PlayerDataSystem debe existir");
        var pd = PlayerDataSystem.Instance.GetPlayerData();
        Assert.IsNotNull(pd, "PlayerData no debe ser null");
        Assert.Greater(pd.playerLevel, 0,
            "El nivel del jugador debe ser > 0");
    }

    [UnityTest]
    public IEnumerator EconomySystem_EnergyInitialized()
    {
        yield return new WaitForSeconds(1f);
        Assert.IsNotNull(EconomySystem.Instance);
        Assert.GreaterOrEqual(EconomySystem.Instance.CurrentEnergy, 0,
            "Energía debe ser >= 0");
        Assert.Greater(EconomySystem.Instance.MaxEnergy, 0,
            "Energía máxima debe ser > 0");
        Assert.LessOrEqual(EconomySystem.Instance.CurrentEnergy,
            EconomySystem.Instance.MaxEnergy,
            "Energía actual no debe superar el máximo");
    }

    [UnityTest]
    public IEnumerator FadeCanvas_ExistsAndNotTransitioning()
    {
        yield return new WaitForSeconds(1f);
        Assert.IsNotNull(UIManager.Instance, "UIManager debe existir");
        Assert.IsFalse(UIManager.Instance.IsTransitioning,
            "No debe haber transición activa tras el boot");
    }
}
#endif
