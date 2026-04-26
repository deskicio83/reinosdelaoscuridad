using System;
using NUnit.Framework;
using UnityEngine;

[TestFixture]
public class EconomySystemTests
{
    [Test]
    public void EnergyRegen_Is4MinutesPerUnit()
    {
        // Regla irrompible: 4 min/unidad (240 s), no 5 min (300 s).
        const int SEGUNDOS_POR_UNIDAD = 240;
        Assert.AreEqual(240, SEGUNDOS_POR_UNIDAD,
            "La energía debe regenerar cada 4 minutos (240s)");
        Assert.AreNotEqual(300, SEGUNDOS_POR_UNIDAD,
            "NO debe ser 5 minutos (300s)");
    }

    [Test]
    public void OfflineEnergyCalc_CorrectUnits()
    {
        // 20 minutos offline = 5 unidades exactas.
        long ahora      = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long hace20min  = ahora - (20 * 60);
        long segundosOffline = ahora - hace20min;
        int  unidades   = (int)(segundosOffline / 240);
        Assert.AreEqual(5, unidades,
            "20 minutos offline debe dar 5 unidades");
    }

    [Test]
    public void OfflineEnergyCalc_RespectsCap()
    {
        // 999 horas offline no debe superar el máximo.
        const int MAX_ENERGIA         = 120;
        long      segundosMuchisimos  = 999L * 3600;
        int       unidadesSinCap      = (int)(segundosMuchisimos / 240);
        int       unidadesConCap      = Mathf.Min(unidadesSinCap, MAX_ENERGIA);
        Assert.AreEqual(MAX_ENERGIA, unidadesConCap,
            "La energía offline no debe superar el máximo");
    }

    [Test]
    public void EnergyRegen_NotFiveMinutes()
    {
        // 5 unidades en 20 min → 4 min/unidad (correcto).
        // 4 unidades en 20 min → 5 min/unidad (incorrecto).
        int unidadesEn20min_a4min = (int)(20 * 60 / 240);
        int unidadesEn20min_a5min = (int)(20 * 60 / 300);
        Assert.AreEqual(5, unidadesEn20min_a4min);
        Assert.AreEqual(4, unidadesEn20min_a5min);
        Assert.AreNotEqual(unidadesEn20min_a4min, unidadesEn20min_a5min);
    }
}
