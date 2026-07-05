using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using ReinoOscuridad.Firebase;
using ReinoOscuridad.UI.HeroScene;

[TestFixture]
public class HeroFlowTests
{
    [UnitySetUp]
    public IEnumerator Setup()
    {
        DataStorageSystem.IsDevMode = true;
        SceneManager.LoadScene("HeroScene");
        yield return new WaitForSeconds(2f);
    }

    [UnityTest]
    public IEnumerator HeroScene_Loads()
    {
        yield return new WaitForSeconds(1f);
        Assert.AreEqual("HeroScene", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator Roster_PopulatesAtLeastOneCard()
    {
        yield return new WaitForSeconds(1f);
        var cards = Object.FindObjectsByType<HeroCardView>(FindObjectsInactive.Exclude);
        Assert.Greater(cards.Length, 0, "Debe haber al menos 1 carta de héroe visible en el roster");
    }

    [UnityTest]
    public IEnumerator ClickCard_OpensDetailPanelWithTabs()
    {
        yield return new WaitForSeconds(1f);

        var card = Object.FindObjectsByType<HeroCardView>(FindObjectsInactive.Exclude).FirstOrDefault();
        Assert.IsNotNull(card, "Debe existir al menos una carta para poder abrir el detalle");

        card.CardButton.onClick.Invoke();
        yield return null;

        var controller = Object.FindAnyObjectByType<HeroSceneController>();
        Assert.IsNotNull(controller, "HeroSceneController debe existir tras el click");
    }
}
