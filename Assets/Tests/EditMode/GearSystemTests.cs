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
        // Fórmula documentada en LECCIONES_TECNICAS.md — GearSystem.RollSubstat es
        // privado, así que esto valida la forma de la distribución, no la llamada real.
        float min = 3f, max = 8f;
        int   N   = 10000;
        float suma = 0f;
        for (int i = 0; i < N; i++)
        {
            float u    = Random.value;
            float roll = max - (max - min) * Mathf.Sqrt(1 - u);
            suma += roll;
        }
        float media    = suma / N;
        float midpoint = (min + max) / 2f;

        Assert.Less(media, midpoint,
            "La media debe estar por debajo del punto medio (sesgado al mínimo)");
        Assert.Greater(media, min,
            "La media debe ser mayor que el mínimo");
    }
}
#endif
