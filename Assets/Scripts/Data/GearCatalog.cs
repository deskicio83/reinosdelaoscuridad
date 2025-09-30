/*
============================================================
GearCatalog.cs — Definición de catálogos de equipamiento
------------------------------------------------------------
PROPÓSITO
- Estructuras de datos del catálogo de gear (setId, type, iconos,
  mainStatRanges, bonus 2/4 piezas...).

USO
- Deserializado por HeroCatalogManager; resuelve datos estáticos.

MÉTODOS/ESTRUCTURAS (COMPLETA AQUÍ)
- class GearCatalog: campos estáticos del item (spriteAddressable, setId, type).
- Diccionarios mainStatRanges por rareza/tipo de stat.
============================================================
*/

using System;
using System.Collections.Generic;

[Serializable]
public class GearCatalog
{
    public string gearId;
    public string setId;
    public string type;
    public List<string> allowedMainStats;
    public List<string> allowedSubStats;
    public Dictionary<string, Dictionary<string, List<int>>> mainStatRanges;
    public Dictionary<string, List<int>> subStatRanges;
    public string spriteAddressable;
    public string description;
}
