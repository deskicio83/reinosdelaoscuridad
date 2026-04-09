using System;
using System.Collections.Generic;

namespace ReinoOscuridad.Data
{
    /// Raíz del JSON hero_catalog.json
    [Serializable]
    public class HeroCatalog
    {
        public List<HeroData> heroes;
    }

    // ── Definición completa de un héroe ───────────────────────────────────────

    [Serializable]
    public class HeroData
    {
        public string heroId;
        public int baseStars;
        /// "Puro" o "Oscuro"
        public string lado;
        public string reino;
        /// Elemento (Naturaleza, Luz, Oscuridad, Fuego, Agua)
        public string element;
        /// Clase principal (Especialista, Sanador, Tanque, DPS…)
        public string classStandard;
        public string subClass;

        // Textos localizados
        public string displayName_es;
        public string displayName_en;
        public string awakenName_es;
        public string awakenName_en;
        public string description_es;
        public string description_en;

        public int baseLevel;
        public int maxLevel;

        // Rutas de assets (Addressables)
        public string heroBackground;
        public string modelAddressable;
        public string modelAddressableAwaken;
        public string portraitAddressable;
        public string portraitAddressableAwaken;
        public string fullAddressable;
        public string fullAddressableAwaken;

        public HeroBaseStats stats;
        public List<HeroSkillDef> skills;
        public AwakenRequirements awakenRequirements;
        public AwakenBonus awakenBonus;
        public List<AwakenUpgrade> awakenUpgrades;
    }

    // ── Stats base del héroe ──────────────────────────────────────────────────

    [Serializable]
    public class HeroBaseStats
    {
        public int baseHP;   public int maxHP;
        public int baseATK;  public int maxATK;
        public int baseDEF;  public int maxDEF;
        public int baseSPD;  public int maxSPD;
        /// Tasa de crítico base (%)
        public int baseTCRI; public int maxTCRI;
        /// Daño de crítico base (%)
        public int baseDCRI; public int maxDCRI;
        public int baseACC;  public int maxACC;
        public int baseRES;  public int maxRES;
        public int baseLUK;  public int maxLUK;
        public int baseAGI;  public int maxAGI;
    }

    // ── Definición de habilidad ───────────────────────────────────────────────

    [Serializable]
    public class HeroSkillDef
    {
        public string skillId;
        /// "basic", "strong", "ultimate", "passive"
        public string type;
        public string name_es;
        public string name_en;
        public int hits;
        /// Stat que escala el daño (ATK, HP, DEF…)
        public string scaleStat;
        public float multiplier;
        public float flatAdd;
        public int cooldown;
        public List<SkillEffect> effect;
        public List<SkillLevelUp> levelUp;
        public string description_es;
        public string description_en;
    }

    [Serializable]
    public class SkillEffect
    {
        public string type;
        /// "enemy", "team", "all_enemy", "self"…
        public string target;
        public float value;
        /// 0–1. Omitido en algunos efectos garantizados
        public float chance;
        /// -1 = permanente
        public int duration;
    }

    [Serializable]
    public class SkillLevelUp
    {
        public int lvl;
        /// Nombre del campo que cambia (multiplier, chance, cooldown…)
        public string change;
        /// Valor como string ("+0.05x", "+10%", "-1")
        public string value;
    }

    // ── Awaken ────────────────────────────────────────────────────────────────

    [Serializable]
    public class AwakenRequirements
    {
        public bool available;
        public int magic_baja;
        public int magic_media;
        public int magic_alta;
        public int element_baja;
        public int element_media;
        public int element_alta;
    }

    [Serializable]
    public class AwakenBonus
    {
        /// Stat que recibe el bonus (SPD%, HP%…)
        public string stat;
        public float value;
    }

    [Serializable]
    public class AwakenUpgrade
    {
        public List<SkillEffect> effect;
        /// Nombre de la habilidad que se mejora ("basic", "strong", "ultimate")
        public string skill;
    }
}
