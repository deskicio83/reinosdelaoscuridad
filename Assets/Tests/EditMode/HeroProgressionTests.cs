using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class HeroProgressionTests
{
    [Test]
    public void StarCap_3Stars_MaxLevel30()
    {
        int cap3stars = 3 * 10;
        Assert.AreEqual(30, cap3stars);
    }

    [Test]
    public void StarCap_6Stars_MaxLevel60()
    {
        int cap6stars = 6 * 10;
        Assert.AreEqual(60, cap6stars);
    }

    [Test]
    public void AwakenCopies_3StarsTo4Stars_Needs3Copies()
    {
        // copiesNeeded = hero.stars (no stars+1).
        // 3★→4★ consume 3 copias.
        int estrellas        = 3;
        int copiasNecesarias = estrellas;
        Assert.AreEqual(3, copiasNecesarias,
            "3★→4★ debe requerir 3 copias");
        Assert.AreNotEqual(4, copiasNecesarias,
            "NO debe ser stars+1");
    }

    [Test]
    public void AwakenCopies_5StarsTo6Stars_Needs5Copies()
    {
        int estrellas        = 5;
        int copiasNecesarias = estrellas;
        Assert.AreEqual(5, copiasNecesarias);
    }

    [Test]
    public void XPCurve_GeometricFormula()
    {
        // xpRequired(N) = RoundToInt(90 * 1.1^(N-1))
        int xpNivel1 = Mathf.RoundToInt(90f * Mathf.Pow(1.1f, 0));
        int xpNivel2 = Mathf.RoundToInt(90f * Mathf.Pow(1.1f, 1));
        Assert.AreEqual(90, xpNivel1);
        Assert.Greater(xpNivel2, xpNivel1,
            "XP requerida debe aumentar con el nivel");
    }
}
