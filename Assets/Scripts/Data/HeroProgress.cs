/*
============================================================
HeroProgress.cs — Progreso runtime de héroes
------------------------------------------------------------
PROPÓSITO
- Datos del jugador por héroe (level, stars, awaken, equipment).

USO
- Persistido en PlayerData; mutado por GearEquipService.

MÉTODOS/ESTRUCTURAS (COMPLETA AQUÍ)
- class HeroProgress: heroId, level, stars, awaken, List<GearInstance> equipment.
============================================================
*/


using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[System.Serializable]
public class HeroProgress
{
    public string heroId; // Coincide con hero_catalog
    public int exp;
    public int level;
    public int stars;
    public int ascension;
    public List<SkillProgress> skills; // Nivel de cada skill del héroe
    public Dictionary<string, float> bonusStats; // Stats bonus temporales/efectos extra (si no se usa, dejar vacío)
    public List<GearInstance> equipment; // Gear equipado (slots casco, botas, etc)
    //public string portraitAddressable; // Path del retrato custom (opcional)
    public bool locked;
    public bool favorite;
    public string customName; // Puede ser null si el jugador no la ha cambiado
    public bool awaken; // Si el héroe está despertado
    public string lado; // Luz u Oscuridad
    // Guardaremos aquí las maestrías compradas de este héroe.
    // Clave EXACTA en JSON: "Maestries"
    [JsonProperty("Maestries")]
    public List<string> Maestries;
}
