using System;
using System.Collections.Generic;

namespace ReinoOscuridad.Data
{
    /// Raíz del JSON gear_catalog.json
    [Serializable]
    public class GearCatalogRoot
    {
        public List<GearCatalogSet>  sets;
        public List<GearCatalogItem> gear;
    }

    /// Set de gear (bonus por 2 y 4 piezas del mismo set)
    [Serializable]
    public class GearCatalogSet
    {
        public string setId;
        public string setName_es;
        public string setName_en;
    }

    /// Definición de una pieza de gear en el catálogo
    [Serializable]
    public class GearCatalogItem
    {
        public string gearId;
        public string setId;

        /// Tipo de slot en español del catálogo (espada, casco, pechera, botas, guantes, escudo)
        public string type;

        public List<string> allowedMainStats;
        public List<string> allowedSubStats;

        /// Rangos de stat principal según rareza:
        ///   mainStatRanges["hp"]["mugroso"] = [100, 200]
        public Dictionary<string, Dictionary<string, int[]>> mainStatRanges;

        /// Rangos min/max por tipo de substat:
        ///   subStatRanges["atk"] = [2, 10]
        public Dictionary<string, int[]> subStatRanges;

        public string spriteAddressable;
        public string displayName_es;
        public string displayName_en;
    }
}
