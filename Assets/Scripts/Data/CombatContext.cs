using System;

namespace ReinoOscuridad.Data
{
    /// Datos de entrada a CombatScene.
    /// CombatScene es agnostica — recibe este contexto y devuelve CombatResult.
    [Serializable]
    public class CombatContext
    {
        /// ID del encuentro (del encounter_catalog.json)
        public string encounterID;

        /// Scene a la que volver al terminar el combate
        public string callerScene;

        /// Modo de combate
        /// Valores validos: "campaign" | "arena" | "tower" | "dungeon" | "worldboss" | "clan"
        public string combatMode;

        /// Equipo del jugador (maximo 5 heroes)
        public HeroInstance[] playerTeam;

        /// Equipo enemigo
        public EnemyInstance[] enemyTeam;

        /// Si hay una maldicion activa en el encuentro
        public bool maldicionActiva;

        /// Elemento del boss (si aplica, puede ser null o empty)
        public string elementoBoss;
    }
}
