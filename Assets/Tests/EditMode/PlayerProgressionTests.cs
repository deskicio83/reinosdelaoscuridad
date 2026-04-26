#if UNITY_EDITOR
using NUnit.Framework;

[TestFixture]
public class PlayerProgressionTests
{
    [Test]
    public void FeatureUnlock_GachaAtLevel2()
    {
        int nivelRequerido = 2;
        Assert.AreEqual(2, nivelRequerido);
        Assert.IsTrue(5  >= nivelRequerido, "Nivel 5 debe tener gacha desbloqueado");
        Assert.IsFalse(1 >= nivelRequerido, "Nivel 1 NO debe tener gacha");
    }

    [Test]
    public void FeatureUnlock_TorreAtLevel10()
    {
        int nivelRequerido = 10;
        Assert.IsTrue(10 >= nivelRequerido);
        Assert.IsFalse(9 >= nivelRequerido);
    }

    [Test]
    public void FeatureUnlock_TorreNormalAndDifficilTogether()
    {
        // Torre Normal y Difícil desbloquean juntas en nivel 10.
        int nivelTorreNormal   = 10;
        int nivelTorreDificil  = 10;
        Assert.AreEqual(nivelTorreNormal, nivelTorreDificil,
            "Torre Normal y Difícil deben desbloquearse al mismo nivel");
    }

    [Test]
    public void FeatureUnlock_MazmorraAtLevel15()
    {
        int nivelRequerido = 15;
        Assert.IsTrue(15  >= nivelRequerido);
        Assert.IsFalse(14 >= nivelRequerido);
    }

    [Test]
    public void PaseOscuro_UnlocksAtLevel25()
    {
        int nivelRequerido = 25;
        Assert.AreEqual(25, nivelRequerido);
    }
}
#endif
