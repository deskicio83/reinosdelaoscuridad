/*
============================================================
SkillProgress.cs — Progreso de habilidades por héroe
------------------------------------------------------------
PROPÓSITO
- Nivel de cada skill del jugador para render de upgrades/deltas.

USO
- Leído por HeroSkillsPanelController para pintar verde/azul.

ESTRUCTURAS (COMPLETA AQUÍ)
- skillId, level.
============================================================
*/

using System;

[System.Serializable]
public class SkillProgress
{
    public string skillId;
    public int level;
}
