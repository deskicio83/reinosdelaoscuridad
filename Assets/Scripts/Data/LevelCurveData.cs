using System;
using System.Collections.Generic;

namespace ReinoOscuridad.Data
{
    /// Datos de la curva de nivel — hero_level_curve.json
    /// XP requerida para subir del nivel N al N+1:
    ///   xpRequired(N) = RoundToInt(baseExp * growth^(N-1))
    /// El cap de nivel depende de las estrellas actuales del héroe (maxLevelPerStars).
    [Serializable]
    public class LevelCurveData
    {
        /// Nivel máximo absoluto (requiere 6★ awakened)
        public int maxLevel;

        /// XP base para el primer nivel (nivel 1 → nivel 2)
        public int baseExp;

        /// Factor de crecimiento geométrico por nivel (e.g. 1.1 = +10% por nivel)
        public float growth;

        /// Cap de nivel según estrellas: "3"→30, "4"→40, "5"→50, "6"→60
        public Dictionary<string, int> maxLevelPerStars;

        /// Nivel máximo cuando el héroe está completamente awakened (6★)
        public int maxLevelAwaken;
    }
}
