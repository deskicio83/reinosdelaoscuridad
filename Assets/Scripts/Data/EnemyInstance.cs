using System;
using System.Collections.Generic;

namespace ReinoOscuridad.Data
{
    /// Enemigo en combate — instancia viva con estado mutable.
    /// Se construye a partir de EnemyData del catalogo (enemy_catalog.json)
    /// antes de entrar a CombatScene.
    [Serializable]
    public class EnemyInstance
    {
        /// ID del tipo de enemigo (referencia a enemy_catalog.json)
        public string enemyId;

        /// Nombre mostrado en combate
        public string nombre;

        public int nivel;

        // ── Vida ──────────────────────────────────────────────────────────────
        public int hpActual;
        public int hpMax;

        // ── Stats de combate ───────────────────────────────────────────────────
        public int atk;
        public int def;
        public int spd;
        public int agi;

        /// Elemento del enemigo (Naturaleza, Luz, Oscuridad, Fuego, Agua)
        public string elemento;

        public bool estaVivo;

        /// IDs de efectos activos (buffs, debuffs, DoTs)
        public List<string> efectosActivos;
    }
}
