/*
============================================================
GearInstance.cs — Instancia runtime de equipamiento
------------------------------------------------------------
PROPÓSITO
- Datos concretos (instanceId, gearId, rarity, upgradeLevel, mainStatType, substats).

USO
- Viven en PlayerData.gearInventory o hero.equipment.

ESTRUCTURAS (COMPLETA AQUÍ)
- class Substat { type, value }.
============================================================
*/

using System.Collections.Generic;

[System.Serializable]
public class GearInstance
{
    public string instanceId; // ID única de la pieza en inventario
    public string gearId;     // ID en gear_catalog
    public string rarity;     // mugroso, extranito, etc
    public int upgradeLevel;  // Nivel de mejora
    public string mainStatType; // Ej: "hp%", "atk", "spd"
    public List<GearSubstat> substats; // Lista de substats
}

[System.Serializable]
public class GearSubstat
{
    public string type; // "atk%", "cri%", etc
    public float value;
}
