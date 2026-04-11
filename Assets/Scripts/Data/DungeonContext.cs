using System;

namespace ReinoOscuridad.Data
{
    /// Contexto especifico para mazmorras.
    /// Se usa junto a CombatContext cuando combatMode == "dungeon".
    [Serializable]
    public class DungeonContext
    {
        /// ID de la mazmorra (referencia a los datos de mazmorras)
        public string dungeonId;

        /// Nivel de dificultad de la mazmorra
        public int nivelDungeon;

        /// Elemento predominante de la mazmorra (afecta resistencias y bonus)
        public string elementoDungeon;

        /// Scene a la que volver al salir de la mazmorra
        public string callerScene;
    }
}
