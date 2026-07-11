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

    private static HeroData MakeHeroData(string heroId, string elemento, string nombre) =>
        new HeroData { heroId = heroId, element = elemento, displayName_es = nombre };

    [Test]
    public void FilterAndSort_ByElemento_OnlyReturnsMatchingElement()
    {
        var roster = new List<PlayerHeroData>
        {
            new PlayerHeroData { heroId = "fuego1" },
            new PlayerHeroData { heroId = "agua1" },
        };
        var catalogo = new Dictionary<string, HeroData>
        {
            { "fuego1", MakeHeroData("fuego1", "fuego", "Ignis") },
            { "agua1",  MakeHeroData("agua1",  "agua",  "Undine") },
        };

        var filtro = new HeroFilterState { elemento = "fuego" };
        var resultado = HeroSceneController.FilterAndSort(roster, filtro, id => catalogo[id]);

        Assert.AreEqual(1, resultado.Count);
        Assert.AreEqual("fuego1", resultado[0].heroId);
    }

    [Test]
    public void FilterAndSort_SoloFavoritos_ExcludesNonFavorites()
    {
        var roster = new List<PlayerHeroData>
        {
            new PlayerHeroData { heroId = "a", favorite = true },
            new PlayerHeroData { heroId = "b", favorite = false },
        };

        var filtro = new HeroFilterState { soloFavoritos = true };
        var resultado = HeroSceneController.FilterAndSort(roster, filtro, null);

        Assert.AreEqual(1, resultado.Count);
        Assert.AreEqual("a", resultado[0].heroId);
    }

    [Test]
    public void FilterAndSort_OrdenarPorEstrellas()
    {
        var roster = new List<PlayerHeroData>
        {
            new PlayerHeroData { heroId = "bajo", stars = 3 },
            new PlayerHeroData { heroId = "alto", stars = 6 },
        };

        var filtro = new HeroFilterState { ordenarPor = "estrellas" };
        var resultado = HeroSceneController.FilterAndSort(roster, filtro, null);

        Assert.AreEqual("alto", resultado[0].heroId, "Mayor número de estrellas debe ir primero");
    }

    [Test]
    public void FilterAndSort_ByEstrellas_OnlyReturnsMatchingCount()
    {
        var roster = new List<PlayerHeroData>
        {
            new PlayerHeroData { heroId = "a", stars = 3 },
            new PlayerHeroData { heroId = "b", stars = 5 },
        };

        var filtro = new HeroFilterState { estrellas = 5 };
        var resultado = HeroSceneController.FilterAndSort(roster, filtro, null);

        Assert.AreEqual(1, resultado.Count);
        Assert.AreEqual("b", resultado[0].heroId);
    }

    [Test]
    public void FilterAndSort_Ascendente_InvierteOrden()
    {
        var roster = new List<PlayerHeroData>
        {
            new PlayerHeroData { heroId = "bajo", level = 10 },
            new PlayerHeroData { heroId = "alto", level = 50 },
        };

        var filtro = new HeroFilterState { ordenarPor = "nivel", ordenAscendente = true };
        var resultado = HeroSceneController.FilterAndSort(roster, filtro, null);

        Assert.AreEqual("bajo", resultado[0].heroId, "Ascendente: menor nivel debe ir primero");
    }

    [Test]
    public void GetSlotExpansionCost_IncreasesWithEachExpansion()
    {
        var (oro1, caos1) = HeroSceneController.GetSlotExpansionCost(20);
        var (oro2, caos2) = HeroSceneController.GetSlotExpansionCost(30);

        Assert.Greater(oro2, oro1, "El coste en oro debe crecer con cada expansión");
        Assert.Greater(caos2, caos1, "El coste en caosífera debe crecer con cada expansión");
    }

    [Test]
    public void GetSlotExpansionCost_FirstExpansion_MatchesBaseFormula()
    {
        var (oro, caosifera) = HeroSceneController.GetSlotExpansionCost(HeroSceneController.SLOT_BASE);

        Assert.AreEqual(1000, oro);
        Assert.AreEqual(20, caosifera);
    }
}
#endif
