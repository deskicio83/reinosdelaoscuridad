#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using ReinoOscuridad.Data;
using ReinoOscuridad.UI.HeroScene;

[TestFixture]
public class HeroSceneControllerTests
{
    [Test]
    public void SortRoster_FavoritesFirst()
    {
        var roster = new List<PlayerHeroData>
        {
            new PlayerHeroData { heroId = "a", level = 50, favorite = false },
            new PlayerHeroData { heroId = "b", level = 10, favorite = true },
            new PlayerHeroData { heroId = "c", level = 30, favorite = false },
        };

        var sorted = HeroSceneController.SortRoster(roster);

        Assert.AreEqual("b", sorted[0].heroId, "El favorito debe ir primero aunque tenga menor nivel");
    }

    [Test]
    public void SortRoster_ThenByLevelDescending()
    {
        var roster = new List<PlayerHeroData>
        {
            new PlayerHeroData { heroId = "low",  level = 10, favorite = false },
            new PlayerHeroData { heroId = "high", level = 50, favorite = false },
        };

        var sorted = HeroSceneController.SortRoster(roster);

        Assert.AreEqual("high", sorted[0].heroId, "Sin favoritos, el de mayor nivel debe ir primero");
    }

    [Test]
    public void SortRoster_DoesNotMutateOriginalListOrder()
    {
        var roster = new List<PlayerHeroData>
        {
            new PlayerHeroData { heroId = "a", level = 10 },
            new PlayerHeroData { heroId = "b", level = 20 },
        };

        var sorted = HeroSceneController.SortRoster(roster);

        Assert.AreNotSame(roster, sorted, "Debe devolver una lista nueva, no reordenar la original in-place");
        Assert.AreEqual("a", roster[0].heroId, "La lista original no debe reordenarse");
    }
}
#endif
