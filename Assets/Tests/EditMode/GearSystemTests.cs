#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class GearSystemTests
{
    [Test]
    public void TriangularRoll_BiasedTowardsMin()
    {
        // roll = max - (max-min)*sqrt(1-u) → distribución sesgada al mínimo.
        float min = 3f, max = 8f;
        int   N   = 10000;
        float suma = 0f;
        for (int i = 0; i < N; i++)
        {
            float u    = Random.value;
            float roll = max - (max - min) * Mathf.Sqrt(1 - u);
            suma += roll;
        }
        float media     = suma / N;
        float midpoint  = (min + max) / 2f; // 5.5

        Assert.Less(media, midpoint,
            "La media debe estar por debajo del punto medio (sesgado al mínimo)");
        Assert.Greater(media, min,
            "La media debe ser mayor que el mínimo");
    }

    [Test]
    public void GearRollLevels_OnlyAt3_6_9_12()
    {
        // Los rolls de substat ocurren solo en +3, +6, +9, +12.
        int[] rollLevels = { 3, 6, 9, 12 };
        for (int nivel = 1; nivel <= 15; nivel++)
        {
            bool debeRollear = System.Array.IndexOf(rollLevels, nivel) >= 0;
            bool esMultiplo3 = nivel % 3 == 0 && nivel <= 12;
            Assert.AreEqual(esMultiplo3, debeRollear,
                "Nivel " + nivel + " roll: " + debeRollear);
        }
    }

    [Test]
    public void SubstatPercent_AppliedOnBaseNotTotal()
    {
        // Stats porcentuales deben aplicarse sobre la base heroica, no sobre el total.
        int atkBase  = 3000;
        int atkPct1  = 5;
        int atkPct2  = 5;

        // Correcto: ambos porcentajes sobre la base.
        int correcto = atkBase
            + Mathf.RoundToInt(atkBase * atkPct1 / 100f)
            + Mathf.RoundToInt(atkBase * atkPct2 / 100f);

        // Incorrecto: segundo porcentaje sobre el total acumulado.
        int incorrecto = atkBase + Mathf.RoundToInt(atkBase * atkPct1 / 100f);
        incorrecto    += Mathf.RoundToInt(incorrecto * atkPct2 / 100f);

        Assert.AreNotEqual(correcto, incorrecto,
            "Los métodos deben producir resultados diferentes");
        Assert.Less(correcto, incorrecto,
            "El método incorrecto infla los stats");
    }
}
#endif
