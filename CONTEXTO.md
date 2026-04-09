# CONTEXTO.md — Reino de la Oscuridad
> Leer este archivo al inicio de cada sesión de Claude Code antes de tocar cualquier archivo.

---

## Identidad del proyecto

| Campo | Valor |
|---|---|
| Nombre | Reino de la Oscuridad |
| Tipo | Gacha RPG móvil — iOS + Android |
| Protagonista | Lord Necrostofeles |
| Tono | Dark fantasy absurdista con humor negro |
| Benchmarks | Summoners War · RAID Shadow Legends · Alliance Sages (Erolabs) |

---

## Stack técnico — NO se negocia

| Componente | Versión / Valor |
|---|---|
| Motor | Unity 6000.4.0f1 |
| Render Pipeline | URP con 2D Renderer |
| Estilo visual | 2.5D — parallax + luces 2D + normal maps |
| Resolución base | 1280×720 landscape 16:9 |
| Orientación | Landscape Left (forzada en iOS y Android) |
| Lenguaje | C# (.NET Standard 2.1) |
| Backend | Firebase Auth + Firestore + Cloud Functions |
| IAP | RevenueCat |
| Control de versiones | GitHub · rama activa: `v2-clean` |
| Repo local | `C:\Users\franh\Documents\GitHub\reinosdelaoscuridad` |
| Company Name | DesdeMiPC |
| Bundle ID | `com.DesdeMiPC.ReinosdelaOscuridad` |

---

## Packages instalados

| Package | Origen | Notas |
|---|---|---|
| Universal RP + 2D Renderer | Template Universal 2D | Preconfigurado |
| TextMeshPro | Unity Registry | Essential Resources importados |
| Addressables | Unity Registry | Grupos inicializados |
| Input System (New) | Unity Registry | Sistema moderno de input táctil |
| Localization | Unity Registry | ES (default) + EN configurados |
| Firebase Auth | SDK manual 13.9.0 | `Assets/Firebase/` |
| Firebase Firestore | SDK manual 13.9.0 | `Assets/Firebase/` |
| Firebase Functions | SDK manual 13.9.0 | `Assets/Firebase/` |
| RevenueCat Purchases | SDK manual | `Assets/RevenueCat/` |

---

## Estructura de carpetas — obligatoria

```
Assets/
├── Addressables/           ← assets de arte via Addressables
│   ├── Heroes/
│   ├── Environments/       ← fondos de combate por encounterID
│   ├── Gear/
│   ├── UI/
│   └── Effects/
├── Data/                   ← JSONs estáticos del catálogo (solo lectura en runtime)
├── Localization/           ← assets de localización ES/EN
├── Prefabs/
│   ├── UI/
│   ├── Combat/
│   └── System/             ← GameManager, UIManager (DontDestroyOnLoad)
├── Scenes/                 ← una .unity por Scene del GDD
├── Scripts/
│   ├── Core/               ← GameManager, UIManager, AudioManager
│   ├── Systems/            ← CombatSystem, EconomySystem, GearSystem...
│   ├── Data/               ← modelos de datos (PlayerData, HeroData...)
│   ├── UI/                 ← una carpeta por Scene
│   │   ├── BootScene/
│   │   ├── MainMenu/
│   │   ├── Combat/
│   │   ├── Campaign/
│   │   ├── Gacha/
│   │   ├── Hero/
│   │   ├── Arena/
│   │   ├── Tower/
│   │   ├── WorldBoss/
│   │   ├── Clan/
│   │   ├── Shop/
│   │   ├── Mission/
│   │   ├── Conjuro/
│   │   ├── Mazmorra/
│   │   ├── Tutorial/
│   │   └── Overlays/
│   ├── Firebase/           ← AuthSystem, DataStorageSystem
│   └── Utils/              ← helpers, extensions, constants
└── Settings/               ← URP assets, Renderer2D, scene templates
```

---

## Naming conventions — obligatorias

| Elemento | Convención | Ejemplo |
|---|---|---|
| Clases | PascalCase | `GameManager`, `CombatSystem` |
| Métodos | PascalCase | `LoadPlayerData()`, `StartCombat()` |
| Variables privadas | _camelCase | `_playerData`, `_currentScene` |
| Variables públicas | camelCase | `heroId`, `upgradeLevel` |
| Constantes | UPPER_SNAKE_CASE | `MAX_ENERGY`, `BASE_CRIT_RATE` |
| Interfaces | IPascalCase | `ISystem`, `ILoadable` |
| ScriptableObjects | PascalCase + SO | `HeroDataSO`, `GearCatalogSO` |
| Prefabs | PascalCase | `HeroCard.prefab` |
| Scenes | PascalCase + Scene | `BootScene`, `CombatScene` |
| Namespace | `ReinoOscuridad.[Carpeta]` | `ReinoOscuridad.Core` |
| Eventos C# | On + PascalCase | `OnCombatEnd`, `OnLevelUp` |

---

## Patrones de arquitectura — no se cambian

### 1. Singletons DontDestroyOnLoad
Solo estos objetos son Singleton y persisten toda la sesión:
- `GameManager` — ciclo de vida global
- `UIManager` — navegación y backstack de overlays
- `AudioManager` — música continua entre Scenes
- `PlayerDataSystem` — fuente de verdad del jugador en memoria
- `EconomySystem` — monedas y energía
- `NotificationManager` — push notifications

Todos los demás sistemas NO son Singleton — se registran en GameManager.

### 2. Datos del jugador en memoria
- `PlayerData` vive en memoria durante toda la sesión
- Se escribe a Firestore SOLO en checkpoints definidos
- **Nunca en `Update()`**
- Máximo 3 reads y 3-5 writes por sesión típica

### 3. Comunicación entre sistemas via eventos
- Los sistemas no se llaman directamente entre sí
- Usan `event Action` / `event Action<T>`
- Ejemplo: `OnCombatEnd?.Invoke(result)` — no `UIManager.ShowResult(result)`

### 4. JSONs del catálogo — carga única
- Se cargan en `BootScene` y permanecen en memoria toda la sesión
- **Nunca `Resources.Load()`** — usar `Assets/Data/` con carga directa en Boot
- Archivos: `hero_catalog.json`, `gear_catalog.json`, `variables_globales.json`, etc.

### 5. CombatScene agnóstica
- Recibe un `CombatContext` (encounterID, equipo, callerScene)
- Devuelve un `CombatResult`
- No sabe si viene de campaña, mazmorra, arena o torre

### 6. Assets visuales via Addressables
- **Nunca `Resources.Load()`** para assets de arte
- Todos los sprites, prefabs de héroes y fondos van via Addressables
- Los JSONs del catálogo NO van en Addressables — van en `Assets/Data/`

---

## Firestore — regla de oro

> Minimizar reads/writes. Trabajar con copia en memoria. Escribir solo en checkpoints.

| Momento | Acción Firestore |
|---|---|
| App abre | 1 read — carga `playerData` completo |
| Durante sesión | 0 reads/writes — todo en memoria |
| Combate completado | 1 write via Cloud Function |
| Sesión cierra | 1 write — estado final |
| Gacha x10 | 1 write via Cloud Function |

Cloud Functions validan todas las operaciones críticas (gacha, IAP, combate, WorldBoss).

---

## Scenes del juego (16 total)

| Scene Unity | Nombre en juego | Estado |
|---|---|---|
| BootScene | — | Documentada |
| TutorialScene | Tutorial | Documentada |
| MainMenuScene | Bastión Maldito | Documentada |
| CombatScene | Campo de Batalla | Documentada |
| CampaignScene | Portón del Dolor™ | Documentada |
| HeroScene | Esbirratorio | Documentada |
| GachaScene | Pozonegro™ | Documentada |
| ArenaScene | Arena de Cachetadas | Documentada |
| TowerScene | Torre de Ascensión | Documentada |
| WorldBossScene | Boss Global | Documentada |
| ClanScene | Guarida Clanosa | Documentada |
| ConjuroScene | Calderón Rúnico | Documentada |
| ShopScene | Tienda Endiablada | Documentada |
| MissionScene | Sala del Mándalo | Documentada |
| MazmorraScene | Mazmorras | Documentada |
| PlayerInfoScene | Overlay sobre MainMenu | Overlay |

Toda la documentación de Scenes está en `/mnt/project/Scene_*_Completa.docx`

---

## JSONs del catálogo

| Archivo | Contenido |
|---|---|
| `hero_catalog.json` | 96 héroes con stats, skills, awaken. Root key: `raw.heroes` |
| `gear_catalog.json` | Sistema de equipamiento con substats (Sprint 8) |
| `variables_globales.json` | Constantes globales del juego |
| `enemy_catalog.json` | Enemigos por mundo |
| `encounter_catalog.json` | Encuentros de campaña |
| `environment_catalog.json` | Fondos temáticos por encounterID |
| `drop_table.json` | Tablas de drops por modo |
| `tower_catalog.json` | Pisos y recompensas de la Torre |
| `spells_catalog.json` | Conjuros del Caos |
| `masteries_catalog_with_meta.json` | Árbol de maestrías por héroe |
| `badge_catalog.json` | Insignias y logros |
| `player_level_curve.json` | Curva de nivel del jugador |
| `hero_level_curve.json` | Curva de nivel de héroes |

---

## Reglas críticas — nunca olvidar

- Energía regenera a **4 minutos por unidad** (no 5)
- **Bleed no puede ser removido por Cleanse**
- **Duplicados van al inventario** — el jugador decide, no se auto-convierten
- Torre Normal y Difícil desbloquean juntas al **nivel 15**; Heroica al completar Difícil
- **EventScene y MailScene son overlays** sobre MainMenuScene, no Scenes independientes
- **`Arquitectura_Unity.docx` está obsoleto** — usa este archivo como referencia
- Subastas/sistemas de alta frecuencia de escritura son **incompatibles con el constraint de Firestore**

---

## Idiomas

| Locale | Estado | Por defecto |
|---|---|---|
| Español (es) | Configurado | ✅ Sí |
| Inglés (en) | Configurado | No |

Las tablas de textos se alimentan progresivamente durante el desarrollo.

---

*Última actualización: Setup inicial v2-clean — Unity 6000.4.0f1 URP 2D*
