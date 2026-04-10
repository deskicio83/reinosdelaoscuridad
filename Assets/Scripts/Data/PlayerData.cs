using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ReinoOscuridad.Data
{
    /// Estado completo del jugador — fuente de verdad en memoria durante la sesión.
    /// Se carga desde Firestore en BootScene y se escribe solo en checkpoints.
    [Serializable]
    public class PlayerData
    {
        /// Firebase Auth UID — identifica el documento en Firestore
        public string uid;
        public string playerName;
        public int playerLevel;
        public int energia;
        public int energiaMax;
        public int oroNegro;
        public int caosifera;

        public PlayerResources resources;

        public List<PlayerHeroData> heroes;
        public List<PlayerGearInstance> gearInventory;
        public List<PlayerArtifactInstance> artifactInventory;

        /// ISO 8601
        public string lastLogin;
        /// Unix timestamp
        public long lastLoginTimestamp;

        public int maxHeroSpaces;
        public AwakenInventory awakenInventory;

        /// Gear no equipado que aún no está asignado a un héroe (lista vacía en partidas nuevas)
        public List<PlayerGearInstance> equipment;

        public bool heroSceneCompactView;
        /// true si el jugador ha completado el tutorial inicial
        public bool tutorialCompleted;

        /// Poder de cuenta total (no es moneda, no se gasta)
        public int presenciaMaldita;
        /// Aportación de gear al presenciaMaldita
        public int presenciaMalditaGear;
        /// ISO 8601 — momento del último recálculo oficial
        public string presenciaMalditaLastCalc;
    }

    // ── Recursos del jugador ──────────────────────────────────────────────────

    [Serializable]
    public class PlayerResources
    {
        public int summonScrolls;
        public int ascensionStones;
    }

    // ── Héroe en el roster del jugador ────────────────────────────────────────

    [Serializable]
    public class PlayerHeroData
    {
        public string heroId;
        public int exp;
        public int level;
        public int stars;
        /// Número de ascensiones completadas
        public int ascension;
        public List<PlayerSkillData> skills;
        /// Stats adicionales por ascensión u otros efectos (null si ninguno)
        public PlayerBonusStats bonusStats;
        public List<PlayerGearInstance> equipment;
        public bool locked;
        public bool favorite;
        public string customName;
        public bool awaken;
        /// "Puro" u "Oscuro" — puede estar vacío si no se ha asignado
        public string lado;
    }

    [Serializable]
    public class PlayerSkillData
    {
        public string skillId;
        public int level;
    }

    /// Stats extra aplicados al héroe (ascensión, runa especial, etc.)
    [Serializable]
    public class PlayerBonusStats
    {
        public float hp;
        public float atk;
        public float def;
        public float spd;
        public float tcri;
        public float dcri;
        public float acc;
        public float res;
        public float luk;
        public float agi;
    }

    // ── Instancia de pieza de gear ────────────────────────────────────────────

    [Serializable]
    public class PlayerGearInstance
    {
        /// ID único de esta instancia (e.g. "gear_1001")
        public string instanceId;
        /// ID del tipo de gear del catálogo
        public string gearId;
        /// Rareza (mugroso, extranito, absurdamente_escaso, divinamente_ridiculo, memeticamente_unico)
        public string rarity;
        public int upgradeLevel;
        /// Stat principal (atk%, hp%, spd, def%…)
        public string mainStatType;
        public List<GearSubstat> substats;
    }

    [Serializable]
    public class GearSubstat
    {
        public string type;
        public float value;
    }

    // ── Artefactos ────────────────────────────────────────────────────────────

    [Serializable]
    public class PlayerArtifactInstance
    {
        public string artifactId;
        public int level;
    }

    // ── Inventario de materiales de awaken ────────────────────────────────────

    [Serializable]
    public class AwakenInventory
    {
        /// Clave: ID del material (caos_extracto, element_luz_infusion…) · Valor: cantidad
        public Dictionary<string, int> items;
    }
}
