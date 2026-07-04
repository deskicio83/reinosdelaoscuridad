using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
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

}
