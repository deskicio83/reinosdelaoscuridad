#if UNITY_EDITOR
using NUnit.Framework;
using ReinoOscuridad.Systems;

[TestFixture]
public class EconomySystemTests
{
    [Test]
    public void EnergyRegen_Is4MinutesPerUnit()
    {
        Assert.AreEqual(240, EconomySystem.ENERGY_REGEN_SECONDS,
            "La energía debe regenerar cada 4 minutos (240s), no 5 (300s)");
    }
}
#endif
