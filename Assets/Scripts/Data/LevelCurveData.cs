using System;
using System.Collections.Generic;

namespace ReinoOscuridad.Data
{
    /// Raíz del JSON hero_level_curve.json
    [Serializable]
    public class LevelCurveData
    {
        public List<LevelCurveEntry> curve;
    }

    /// Una entrada en la curva de niveles.
    /// xpRequired = XP que el héroe necesita acumular estando en <level>
    /// para subir al nivel siguiente.
    /// El héroe al nivel máximo tiene xpRequired = 0.
    [Serializable]
    public class LevelCurveEntry
    {
        public int level;
        public int xpRequired;
    }
}
