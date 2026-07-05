# Reino de la Oscuridad — CLAUDE.md
> Leer este archivo al inicio de cada sesión antes de tocar cualquier archivo.

Gacha RPG móvil iOS/Android · Unity 6000.4.0f1 · C# · Firebase · RevenueCat  
Landscape 16:9 · Resolución base 1280×720 · Rama activa: `v2-clean`  
Repo local: `C:\Users\franh\Documents\GitHub\reinosdelaoscuridad`

---

## Documentación disponible en Docs/

Lee solo lo que necesites para la tarea actual:

| Documento | Ruta |
|---|---|
| Diseño del juego completo | `Docs/GDD_COMPLETO_ReinoDeLaOscuridad.docx` |
| Arquitectura y navegación | `Docs/Arquitectura_Navegacion.docx` |
| Cada Scene en detalle | `Docs/Scene_[NombreScene]_Completa.docx` |
| Sistema de combate | `Docs/Sprint1_Doc2_Combate.docx` |
| Economía y energía | `Docs/Sprint1_Doc1_Economia.docx` |
| Firebase y datos | `Docs/Sprint1_Doc3_Firestore.docx` |
| Gacha e IAP | `Docs/Sprint2_Gacha_IAP_Jefes.docx` |
| Gear y substats | `Docs/Sprint8_SubstatsGear.docx` |
| Mazmorras | `Docs/Sprint9_Mazmorras.docx` |
| Overlays (32 paneles) | `Docs/Overlays_del_Sistema.docx` |
| Catálogo de JSONs | `Docs/Catalogo_JSONs_Tecnicos.docx` |
| Estado actual del proyecto | `Docs/ESTADO_PROYECTO.md` ← actualizar después de cada sesión |
| Lecciones técnicas acumuladas | `Docs/LECCIONES_TECNICAS.md` ← leer siempre al inicio |

**JSONs del catálogo — fuente de verdad: `Assets/Data/`**  
`hero_catalog.json` · `gear_catalog.json` · `spells_catalog.json`  
`encounter_catalog.json` · `enemy_catalog.json` · `drop_table.json`  
`environment_catalog.json` · `tower_catalog.json` · `variables_globales.json`  
→ A partir de S01 todos los JSONs están en `Assets/Data/`. Cárgalos siempre desde ahí.  
→ Solo leerlos desde `Docs/` si por algún motivo no están aún en `Assets/Data/`.

---

## Stack técnico — NO se negocia

| Componente | Valor |
|---|---|
| Motor | Unity 6000.4.0f1 |
| Render Pipeline | URP con 2D Renderer |
| Estilo visual | 2.5D — parallax + luces 2D dinámicas + normal maps |
| Lenguaje | C# (.NET Standard 2.1) |
| Backend | Firebase Auth + Firestore + Cloud Functions |
| IAP | RevenueCat |
| Bundle ID | `com.DesdeMiPC.ReinosdelaOscuridad` |

**Packages instalados:** Addressables · Input System (New) · TextMeshPro · Localization (ES default + EN) · Firebase SDK 13.9.0 (Auth + Firestore + Functions) · RevenueCat

---

## Reglas que NUNCA se rompen

```
IMPORTANTE: Antes de crear un archivo, busca si ya existe algo similar.
IMPORTANTE: Datos del jugador se escriben a Firestore SOLO en checkpoints.
            Nunca en Update() ni en coroutines periódicas.
IMPORTANTE: Assets visuales van via Addressables. NUNCA Resources.Load().
IMPORTANTE: CombatScene es agnóstica — recibe CombatContext, devuelve CombatResult.
IMPORTANTE: Sistemas se comunican via eventos C#, no llamadas directas entre sí.
IMPORTANTE: JSONs del catálogo se cargan UNA vez en BootScene. No se releen.
IMPORTANTE: Energía regenera a 4 minutos por unidad (no 5).
IMPORTANTE: Bleed NO puede ser removido por Cleanse.
IMPORTANTE: Duplicados de gacha van al inventario — el jugador decide, no se auto-convierten.
IMPORTANTE: Subastas y sistemas de alta frecuencia de escritura son incompatibles con Firestore.
IMPORTANTE: No hacer commit si Unity tiene errores de compilación rojos. Resolver primero.
```

---

## Arquitectura base

**Singletons DontDestroyOnLoad** — solo estos, ninguno más:
- `GameManager` — ciclo de vida global, registra todos los sistemas
- `UIManager` — gestiona Scenes y stack de overlays
- `AudioManager` — música continua entre Scenes
- `PlayerDataSystem` — fuente de verdad del jugador en memoria
- `EconomySystem` — monedas y energía
- `NotificationManager` — push notifications
- `CombatSystem` — lógica de combate pura, debe sobrevivir a la recarga de CombatScene
- `AuthSystem` — sesión de Firebase Auth activa durante toda la app
- `DataStorageSystem` — único punto de acceso a Firestore, debe persistir entre Scenes
- `GearSystem` — inventario de gear en memoria, consumido desde varias Scenes
- `HeroProgressionSystem` — cálculo de niveles/awaken, consumido desde varias Scenes
- `PlayerProgressionSystem` — desbloqueos por nivel, escucha EventBus durante toda la sesión
- `LoadingScreen` — overlay de transición entre Scenes, debe sobrevivir al cambio de Scene

> Nota (Sprint 0, 2026-07-02): estos últimos 7 ya existían como DDOL en el código antes de estar
> documentados aquí — la auditoría de Sprint 0 los encontró como desviación no autorizada. Tras
> revisar cada uno, todos tienen una razón legítima para sobrevivir a la recarga de Scene (estado
> en memoria o listeners de EventBus de larga duración), así que se formalizan aquí en vez de
> refactorizarlos. Cualquier singleton NUEVO que no esté en esta lista debe justificarse o no serlo.

**Sistemas:** implementan `ISystem`, se registran en `GameManager.Initialize()`.  
**Comunicación:** via EventBus central en GameManager — `event Action` / `event Action<T>`.  
**PlayerData:** vive en memoria toda la sesión. Máximo 3 reads y 3-5 writes por sesión típica.

### Firestore — regla de oro

| Momento | Acción |
|---|---|
| App abre (BootScene) | 1 read — carga `playerData` completo |
| Durante la sesión | 0 reads/writes — todo en memoria |
| Combate completado | 1 write via Cloud Function |
| Gacha x10 | 1 write via Cloud Function |
| App cierra | 1 write — estado final |

---

## Estructura de carpetas

```
Assets/
├── Addressables/        ← Heroes/ · Environments/ · Gear/ · UI/ · Effects/
├── Data/                ← JSONs estáticos del catálogo (fuente definitiva)
├── Localization/        ← assets ES/EN
├── Prefabs/             ← UI/ · Combat/ · System/
├── Scenes/              ← una .unity por Scene
├── Scripts/
│   ├── Core/            ← GameManager · UIManager · AudioManager
│   ├── Systems/         ← CombatSystem · EconomySystem · GearSystem...
│   ├── Data/            ← modelos: PlayerData · HeroData...
│   ├── UI/              ← una carpeta por Scene + Overlays/
│   ├── Firebase/        ← AuthSystem · DataStorageSystem
│   └── Utils/           ← helpers · extensions · constants
└── Settings/            ← URP assets · Renderer2D
```

---

## Naming conventions

| Elemento | Convención | Ejemplo |
|---|---|---|
| Clases / Métodos | PascalCase | `GameManager`, `LoadPlayerData()` |
| Variables privadas | _camelCase | `_playerData`, `_currentScene` |
| Variables públicas | camelCase | `heroId`, `upgradeLevel` |
| Constantes | UPPER_SNAKE_CASE | `MAX_ENERGY`, `BASE_CRIT_RATE` |
| Interfaces | IPascalCase | `ISystem`, `ILoadable` |
| ScriptableObjects | PascalCase + SO | `HeroDataSO` |
| Scenes | PascalCase + Scene | `BootScene`, `CombatScene` |
| Namespace | `ReinoOscuridad.[Carpeta]` | `ReinoOscuridad.Core` |
| Eventos C# | On + PascalCase | `OnCombatEnd`, `OnLevelUp` |

---

## Scenes del juego (16 total)

`BootScene` · `TutorialScene` · `MainMenuScene` · `CombatScene` · `CampaignScene`  
`HeroScene` · `GachaScene` · `ArenaScene` · `TowerScene` · `WorldBossScene`  
`ClanScene` · `ConjuroScene` · `ShopScene` · `MissionScene` · `MazmorraScene`  
`PlayerInfoScene` (overlay sobre MainMenu, no Scene independiente)

---

## Trabajo manual en Unity Editor

Algunas tareas no pueden hacerse desde código. Cuando una sesión las requiera,
Claude Code debe listar al final de su respuesta los pasos exactos bajo el título
**"Pasos manuales en Unity Editor"**, con este formato:

```
PASOS MANUALES EN UNITY EDITOR:
1. File > New Scene > Basic 2D → guardar como Assets/Scenes/NombreScene.unity
2. Arrastrar el prefab Assets/Prefabs/System/GameManager.prefab a la jerarquía
3. Seleccionar el GameObject X → Inspector → campo Y → arrastrar el script Z
```

Si la sesión no requiere pasos manuales, escribir: "Sin pasos manuales en Unity Editor."

---

## Flujo de sesiones

Este proyecto se implementa en sesiones únicas y acotadas. Cada sesión:

1. Leer `Docs/ESTADO_PROYECTO.md` para conocer el estado actual.
1b. Leer `Docs/LECCIONES_TECNICAS.md` — contiene patrones aprendidos y errores a no repetir.
    Aplicar antes de escribir cualquier código.
2. Tiene **un solo objetivo** definido antes de empezar
2. Termina con Unity compilando **sin errores rojos**
3. Termina con un **commit** siguiendo el formato establecido
4. Termina actualizando **`Docs/ESTADO_PROYECTO.md`** con lo que se hizo y qué sigue

Si durante la sesión aparece un error de compilación: **resuélvelo antes de continuar**.
No dejes errores pendientes para la siguiente sesión.

---

## Después de cada tarea

1. Confirma que Unity compila sin errores rojos
2. Lista los pasos manuales en Unity Editor si los hay (ver sección anterior)
3. Actualiza `Docs/ESTADO_PROYECTO.md` — marca lo completado y escribe qué sigue
4. Haz commit: `feat/fix/refactor/config: descripción breve`
5. Dime qué archivos creaste o modificaste

> Autorización permanente (2026-07-04): revisar/corregir código y hacer commit + push a `v2-clean`
> en `origin` son parte del flujo normal — no hace falta pedir permiso cada vez para esto.
> Sí sigue requiriendo confirmación explícita: force-push, `git reset --hard`, reescribir historia
> (rebase interactivo, amend de commits ya pusheados), borrar ramas, o cualquier operación sobre la
> rama `main` (es el proyecto original archivado — no tocar sin que se pida explícitamente).
>
> Autorización permanente (2026-07-05): si Unity Editor está abierto y bloquea una verificación
> automatizada por CLI (`-runTests`), matar el proceso directamente en vez de pedir que se cierre
> manualmente. Solo el proceso principal (el que tiene `-projectpath`), no los `AssetImportWorker*`.
