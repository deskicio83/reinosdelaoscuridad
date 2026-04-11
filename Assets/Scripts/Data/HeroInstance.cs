using System;
using System.Collections.Generic;

namespace ReinoOscuridad.Data
{
    /// Heroe en combate — instancia viva con estado mutable.
    /// Se construye a partir de PlayerHeroData + HeroData del catalogo
    /// antes de entrar a CombatScene.
    [Serializable]
    public class HeroInstance
    {
        /// ID del tipo de heroe (referencia a HeroData del catalogo)
        public string heroId;

        public int nivel;

        // ── Vida ──────────────────────────────────────────────────────────────
        public int hpActual;
        public int hpMax;

        // ── Stats de combate ───────────────────────────────────────────────────
        public int atk;
        public int def;
        public int spd;
        public int agi;
        public int crit;
        public int critDmg;
        public int acc;
        public int res;
        public int luk;

        /// Elemento del heroe (Naturaleza, Luz, Oscuridad, Fuego, Agua)
        public string elemento;

        public bool estaVivo;

        /// IDs de efectos activos (buffs, debuffs, DoTs)
        public List<string> efectosActivos;

        /// IDs de las habilidades equipadas (maximo 4)
        public string[] habilidadesEquipadas;
    }
}
