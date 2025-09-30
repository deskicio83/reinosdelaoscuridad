/*
============================================================
HeroCatalog.cs — Catálogo de héroes/skills/sets/glosario
------------------------------------------------------------
PROPÓSITO
- Datos estáticos: heroes[], skills[], glosarioEstadosES, sets.

USO
- Cargado por HeroCatalogManager al inicio; acceso de solo lectura.

MÉTODOS/ESTRUCTURAS (COMPLETA AQUÍ)
- class HeroCatalogEntry (stats base, retratos, elemento).
- class Skill (type, gameDesc, effect[], levelUp[]).
- glosarioEstadosES: key->(nombre, descripcion).
============================================================
*/

using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

[CreateAssetMenu(fileName = "HeroCatalog", menuName = "Game/Hero Catalog")]
public class HeroCatalog : ScriptableObject
{
    public List<HeroCatalogEntry> heroes = new List<HeroCatalogEntry>();
}

[System.Serializable]
public class HeroCatalogEntry
{
    public string heroId;
    public string displayName;
    public string awakenName;
    public string classStandard;
    public string element;
    public string reino;
    public string heroBackground; // <-- nuevo
    public AwakenRequirements awakenRequirements;   // <-- NUEVO
    public AwakenBonus awakenBonus;                 // puede venir o no
    public List<AwakenUpgrade> awakenUpgrades;      // puede venir o no
    public string subClass;
    public string lado;

    [TextArea] public string description;

    public int baseStars;
    public int ascensionStars;
    public int baseLevel;
    public int maxLevel;

    // Prefabs 3D
    public string modelAddressable;          // normal
    public string modelAddressableAwaken;    // awaken

    [Header("Sprites Addressables")]
    public string portraitAddressable;           // Assets/Addressables/Heroes/Avatar/<id>.png
    public string portraitAddressableAwaken;     // Assets/Addressables/Heroes/AvatarAwaken/<id>.png
    public string fullAddressable;               // Assets/Addressables/Heroes/Full/<id>.png   <-- NUEVO
    public string fullAddressableAwaken;         // Assets/Addressables/Heroes/FullAwaken/<id>.png

    public List<HeroSkill> skills = new List<HeroSkill>();
    public HeroStats stats;

    // Devuelve el nombre de la habilidad por tipo ("basic", "strong", "ultimate")
    public string GetSkillNameByType(string type)
    {
        if (skills == null) return null;
        var s = skills.FirstOrDefault(x =>
            string.Equals(x.type, type, StringComparison.OrdinalIgnoreCase));
        return s != null ? s.name : null;
    }

    // Devuelve el nombre de la habilidad por skillId (skillRef)
    public string GetSkillNameById(string skillId)
    {
        if (skills == null) return null;
        var s = skills.FirstOrDefault(x => string.Equals(x.skillId, skillId, StringComparison.OrdinalIgnoreCase));
        return s != null ? s.name : null;
    }
}

[System.Serializable]
public class AwakenBonus
{
    public string stat;   // p.ej. "DEF%"
    public float value;   // p.ej. 12
}

[System.Serializable]
public class AwakenUpgrade
{
    // Variante 1: stat plano (global)
    public string stat;      // p.ej. "DEF%"
    public float value;      // p.ej. 12

    // Variante 2: texto directo por tipo de habilidad
    public string skill;        // "basic" | "strong" | "ultimate"
    public string improvement;  // texto libre

    // Variante 3 (legacy): cambio cuantitativo por skillRef + effectCode + from/to
    public int level;         // p.ej. 1
    public string change;     // p.ej. "effect_value"
    public string skillRef;   // p.ej. "CentellaDeEter_basic"
    public string effectCode; // 🔁 ANTES era 'effect' (string). Renombrado para evitar colisión con la lista 'effect[]'
    public float from;        // p.ej. 0.15
    public float to;          // p.ej. 0.25

    // Variante 4 (NUEVO): lista de efectos estilo skills.gameDesc
    public List<SkillEffectEntry> effect; // NOTA: el JSON usa la misma key "effect"
}

[System.Serializable]
public class SkillCatalogEntry
{
    public string skillName;
    [TextArea] public string description;
}

// ======== HERO SKILLS (modelo alineado con hero_catalog.json nuevo) ========
[System.Serializable]
public class HeroSkill
{
    public string skillId;
    public string type;          // "basic", "strong", "ultimate" (también soporta alias antiguos)
    public string name;
    public string description;

    // Campos de cálculo base (opcionales en el JSON)
    public int hits;
    public string scaleStat;     // "ATK", "DEF", etc.
    public float multiplier;     // 1.0, 0.95, etc.
    public float flatAdd;
    public int cooldown;

    // Efectos declarativos del JSON (opcional)
    public List<SkillEffectEntry> effect;

    // Subida por niveles (nuevo bloque)
    public List<SkillLevelUpEntry> levelUp;

    // *** COMPAT: si algún héroe antiguo aún trae estos, el panel los usará como fallback ***
    public int lvlBase;
    public int lvlMax;
    public List<string> upgradesText; // compat
    public List<SkillUpgrade> upgrades; // compat
}

[System.Serializable]
public class HeroStats
{
    public int baseHP, maxHP;
    public int baseATK, maxATK;
    public int baseDEF, maxDEF;
    public int baseSPD, maxSPD;
    public int baseTCRI, maxTCRI;
    public int baseDCRI, maxDCRI;
    public int baseACC, maxACC;
    public int baseRES, maxRES;
    public int baseLUK, maxLUK;
    public int baseAGI, maxAGI;
}

[System.Serializable]
public class SkillUpgrade
{
    public int level;
    public string type; // "damage", "effect_chance", etc
    public float amount; // Porcentaje o cantidad absoluta
    public string desc;
}

[System.Serializable]
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

[System.Serializable]
public class SkillEffectEntry
{
    public string type;      // "poison", "chance", ...
    public string target;    // "enemy", "all_enemy", "ally", "all_ally", "self"
    public float value;      // porcentaje (0.1 => 10%) o turnos (si aplica)
    public int duration;     // turnos (si aplica)
    public float chance;     // 0..1 (si aplica)

    // NUEVO
    public List<string> appliesTo; // p.ej. ["bleed","burn","poison"]

    // Opcional: rangos/tier (si los usas en otras mejoras)
    public string tier;      // "bajo" | "medio" | "alto"
    public string fromTier;  // "bajo" -> "alto"
}

[System.Serializable]
public class SkillLevelUpEntry
{
    public int lvl;          // 1..N
    public string change;    // "multiplier", "chance", "cooldown", ...
    public string value;     // puede venir como "+0.05x", "+10%", "-1", etc.
}
