using System;

namespace ReinoOscuridad.Data
{
    /// Datos de salida de CombatScene.
    /// Se devuelve a callerScene al finalizar el combate.
    [Serializable]
    public class CombatResult
    {
        /// true si el jugador gano el combate
        public bool victoria;

        /// Dano total infligido durante el combate
        public int danoTotal;

        /// IDs de los items obtenidos como drops
        public string[] drops;

        /// Experiencia ganada por el equipo
        public int xpGanada;

        /// Variacion de trofeos (Arena) — puede ser negativo si se pierde
        public int trofeosDelta;

        /// Grado obtenido en WorldBoss (F / D / C / B / A / S / SS / SSS / SSS+)
        /// Vacio en modos que no usan grado
        public string gradoObtenido;
    }
}
