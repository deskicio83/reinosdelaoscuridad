# Estado del Proyecto — Reino de la Oscuridad

Última actualización: inicio v2-clean

## Implementado
- Estructura de carpetas base de Unity (Assets/Addressables, Data, Prefabs, 
  Resources, Scenes, Scripts, Settings)
- Packages instalados: [el Arquitecto Unity te dirá cuáles cuando termine]

## En progreso
- Configuración de Unity (Chat Arquitecto Unity)

## Pendiente — orden de implementación

### BLOQUE 1 — Base sin la que nada funciona
- [ ] Mover JSONs de Docs/ a Assets/Data/
- [ ] GameManager.cs + EventBus.cs
- [ ] UIManager.cs (navegación entre Scenes)
- [ ] AudioManager.cs
- [ ] 15 Scenes vacías creadas en Assets/Scenes/
- [ ] BootScene funcional (carga JSONs, detecta sesión Firebase)
- [ ] AuthSystem.cs (guest, Google, Apple, Facebook)
- [ ] PlayerDataSystem.cs (datos en memoria)
- [ ] DataStorageSystem.cs (lectura/escritura Firestore)
- [ ] EconomySystem.cs (energía offline, monedas)

### BLOQUE 2 — MVP jugable
- [ ] MainMenuScene
- [ ] CombatScene + CombatSystem (fórmulas del GDD)
- [ ] CampaignScene
- [ ] HeroScene
- [ ] GachaScene
- [ ] ShopScene básico

### BLOQUE 3 — Progresión
- [ ] MissionScene · ArenaScene · TowerScene · MazmorraScene
- [ ] HeroScene completa (gear, maestrías, awaken)

### BLOQUE 4 — Social y monetización
- [ ] ClanScene · WorldBossScene · ConjuroScene · ShopScene completo

### BLOQUE 5 — Arte
- [ ] Integración de assets visuales via Addressables

## Decisiones técnicas tomadas
- UGUI (no UI Toolkit)
- JSONs del catálogo en Assets/Data/ via Resources (excepción controlada)
- Assets visuales via Addressables

## Historial de sesiones
*(se añade aquí después de cada sesión de Claude Code)*