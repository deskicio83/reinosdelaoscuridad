# PLAN DE DESARROLLO — Reino de la Oscuridad
_Creado 2026-07-02 tras auditoría completa de código, del proyecto original (deskicio83/main) y del GDD._
_Reestructurado 2026-07-02 en sprints cerrados con gate de dependencia._
_Leer junto con `ESTADO_PROYECTO.md` (estado sesión a sesión) y `LECCIONES_TECNICAS.md` (patrones técnicos)._

## Cómo se lee este documento

Cada sprint es **cerrado**: tiene un objetivo único, un alcance explícito (qué entra y qué NO
entra), y criterios de cierre (Definition of Done) verificables. **No se abre el siguiente sprint
hasta que el actual cumple su DoD.** Si durante un sprint aparece algo que invalida el orden o el
alcance de los sprints siguientes (un hallazgo "insalvable"), se documenta en la sección
**Replanificaciones** al final y se ajusta el roadmap antes de continuar — no se improvisa scope
nuevo dentro de un sprint ya cerrado en objetivo.

---

## Sistemas transversales

Contrato que **toda** Scene/sprint de contenido debe cumplir. No son features de una Scene — son
infraestructura compartida. Cuando un sprint de Scene diga "integra con transversales", significa
usar estos sistemas tal cual, no reinventarlos.

### Ya implementados (usar, no reconstruir)
| Sistema | Qué resuelve | Dónde vive |
|---|---|---|
| `GameManager` + `EventBus` + `ISystem` | Registro/ciclo de vida de sistemas, comunicación desacoplada | `Assets/Scripts/Core/` |
| `UIManager` | Navegación entre Scenes (fade, historial), stack de overlays, back button Android | `Assets/Scripts/Core/UIManager.cs` |
| `PlayerDataSystem` | Única fuente de verdad del jugador en memoria durante la sesión | `Assets/Scripts/Systems/PlayerDataSystem.cs` |
| `DataStorageSystem` | Único punto de acceso a Firestore — 1 read/sesión, writes en checkpoints | `Assets/Scripts/Firebase/DataStorageSystem.cs` |
| `AuthSystem` | Sesión (Google/Apple/Guest/Dev) | `Assets/Scripts/Firebase/AuthSystem.cs` |
| `EconomySystem` | Energía/oro/caosifera en memoria | `Assets/Scripts/Systems/EconomySystem.cs` |
| HUD global (`HUDController`, `CurrencyPill`) | Monedas/energía visibles en toda Scene con HUD | `Assets/Scripts/UI/HUD/` |
| `LoadingScreen` / `InputBlocker` | Bloqueo de input y overlay de carga durante transiciones | `Assets/Scripts/UI/Common/` |
| Editor Setup Scripts (`Assets/Editor/Setup/*.cs`) | Construcción de Scenes por código con anchors 0–1 sobre 1280×720 | ver convención abajo |

### Convención de generación de UI (usar en cualquier tarea de GUI)
Toda Scene se construye por código en un `SetupXScene.cs` con helpers `Child()`, `Anch(xMin,yMin,xMax,yMax)`
(anchors normalizados 0–1), `Img()`, `Txt()`, `Hex()`. Canvas siempre `CanvasScaler.ScaleWithScreenSize`,
referencia 1280×720, match 0.5. Documentar las zonas de la Scene en un comentario de cabecera (ver
`SetupCombatScene.cs` como referencia). **Pendiente (ver Sprint 0.5 abajo)**: estos helpers están
duplicados literalmente en 8 de 12 archivos — extraer a `EditorUIBuilder` compartido antes de que
se dupliquen una vez más en los próximos Setup Scripts.

### Existen pero necesitan endurecerse (parte de Sprint 0)
- **Feedback de error al jugador**: hoy varias acciones fallan en silencio (ej. energía
  insuficiente). No hay un componente reusable de "toast/mensaje" — construirlo la primera vez que
  un sprint de contenido lo necesite de verdad (probablemente Sprint 1, HeroScene) y de ahí
  reusarlo en todos los siguientes.
- **Versionado de PlayerData** (`UIConstants.DATA_VERSION`) — existe y migra `dev_playerdata`, pero
  no hay aún un plan de migración de esquema para datos reales de Firestore cuando el modelo
  `PlayerData.cs` cambie de forma incompatible. Definir el patrón en el primer sprint que necesite
  cambiar el esquema de forma no aditiva.

### Faltan por completo — construir como su propio sprint corto ANTES de que el primer contenido los necesite
- **`PooledScrollList` / grid reciclable genérico** — el hallazgo más repetido en la auditoría del
  proyecto original: todas sus listas (roster de héroes, inventario de gear) destruían e
  instanciaban TODO el contenido en cada refresco, sin pooling. HeroScene (Sprint 1) es la primera
  Scene que necesita esto — construir el componente genérico ahí mismo, reusarlo después en Gacha
  (historial), Clan (miembros), Torre (ranking), etc.
- **`EditorUIBuilder` compartido** — ver arriba.
- **Sistema de Correo (Mail)** — entrega asíncrona de recompensas pendientes (cofres de WorldBoss,
  regalos de Shop). No existe. Necesario antes de Sprint WorldBoss.
- **Analytics/tracking hooks transversales** — MissionScene (logros/misiones) necesita que TODOS
  los sistemas (combate, gacha, arena, clan, gear) emitan eventos de EventBus rastreables. Se
  construye sprint a sprint (cada sistema nuevo publica sus eventos desde el día 1) para no tener
  que retro-instrumentar todo antes de MissionScene.

---

## SPRINT 0 — Estabilización y hallazgos críticos de combate

**Objetivo**: cerrar los bugs críticos encontrados en la auditoría de código y dejar el combate
real conectado a la lógica de efectos de estado que ya existe y está testeada.

**Estado: EJECUTADO 2026-07-02.** Detalle de cada punto:

- ✅ **`lastLoginTimestamp` nunca se actualizaba** (energía offline duplicable indefinidamente).
  Fix en `BootSceneController.ProceedAfterLoginAsync()`: se resella el timestamp a "ahora" después
  de que `EconomySystem.Initialize()` calculó la ganancia offline y `GameManager.NotifySessionStart()`
  comprobó el daily reset (ambos necesitan el valor ANTERIOR — el orden importa).
- ✅ **Doble `Initialize()` de EconomySystem** — investigado a fondo, NO es un bug real: la primera
  llamada automática (vía cadena de `Awake()`→`RegisterSystem`) corre antes de que Firestore cargue,
  con `PlayerData` vacío por defecto, y su resultado queda descartado por completo cuando
  `LoadPlayerDataFromFirestore()` reemplaza `_playerData` entero. Se documenta aquí para que no se
  vuelva a marcar como crítico en una futura auditoría sin este contexto.
- ✅ **`CombatSceneData.PrepareRepeat()` reutilizaba `playerTeam` con HP/efectos del intento
  anterior** al pulsar "Repetir". Fix: reconstruye `playerTeam` igual que ya hacía con `enemyTeam`
  (HP al máximo, `estaVivo=true`, `efectosActivos` vacío).
- ✅ **Singletons DDOL no autorizados** — los 6 que ya existían (`DataStorageSystem`, `AuthSystem`,
  `GearSystem`, `HeroProgressionSystem`, `PlayerProgressionSystem`, `LoadingScreen`) más
  `CombatSystem` se formalizaron en `CLAUDE.md` con la razón de cada uno, en vez de refactorizarlos
  a mitad de una sesión de estabilización.
- ✅ **`CombatSystem.ProcessCombat()` era código muerto — los efectos de estado nunca se ejecutaban
  en producción.** Este fue el hallazgo más grande y con más alcance real de lo que parecía en la
  primera auditoría — ver el bloque siguiente.

### Hallazgo ampliado durante la ejecución: el catálogo tiene ~45 tipos de efecto, no ~6

Al wirear el fix, `hero_catalog.json` resultó tener **45 valores distintos** de `SkillEffect.type`
(`shield`, `def_down`, `atk_down`, `spd_up/down`, `turn_bar_up/down`, `decrease_cooldowns`,
`heal_team`, `strengthen`, `counterattack`, `taunt`, `revive`, `mark`, `blind`,
`guaranteed_crit`, `unkillable`, `reflect_damage`, `block_buffs`, `cleanse`, etc.), mientras que
`CombatSystem` (`TipoEfecto` enum + `TickEffects`/`TryApplyEffect`/`RemoveEffect`) solo tiene lógica
completa para **5**: Bleed, Burn, Poison (DoT), Stun (skip turno), Regen (heal %). Esto coincide con
el "Sprint 3 — 46 efectos de estado" que el GDD ya documenta como sistema propio y grande — no es
una tarea de Sprint 0.

**Decisión de alcance tomada (replanificación menor, documentada aquí en vez de bloquear Sprint 0)**:
Sprint 0 conecta ÚNICAMENTE los 5 efectos que ya tenían lógica completa y testeada — son los que
de verdad estaban "muertos" por desconexión, no por estar sin implementar. Cambios reales:
- `CombatSystem.cs`: nuevos wrappers públicos `TickEffects(HeroInstance)` / `TickEffects(EnemyInstance)`,
  `TryApplyEffect(TipoEfecto, List<string>, float chance01)` (probabilidad explícita del catálogo,
  en vez de la fórmula ACC/RES), `HasStun(...)` estáticos.
- `CombatSceneController.cs`: `BuildHeroSkillList()` ya no descarta el campo `effect` del catálogo
  al construir la lista de habilidades del héroe (bug de pérdida de datos independiente, corregido
  de paso). Nuevo `TickInicioDeTurno()` llamado al inicio de `PlayerTurnRoutine`/`EnemyTurnRoutine`
  (tick de DoT/Regen + chequeo de Stun, con el mismo manejo visual de "murió por el tick" que
  `ExecuteAction`). Nuevo `AplicarEfectosDeHabilidad()` llamado tras aplicar daño en `ExecuteAction`,
  que mapea `SkillEffect.type` → `TipoEfecto` (solo los 5 soportados; el resto se ignora sin romper
  nada) y respeta `target` ("enemy"/"all_enemy"/"self"). Extraído `TryFinalizeIfCombatOver()` como
  helper reusable (antes era código inline duplicable).
- El resto de los ~40 tipos de efecto (shield, todos los buffs/debuffs de stat, control de turno,
  utilidad) queda **fuera de alcance** — no rompen nada al ignorarse (el switch de mapeo devuelve
  null y se salta), pero tampoco producen su efecto real en combate todavía. Ver **Sprint 3** abajo.

**DoD real cumplido**: Bleed/Burn/Poison/Stun/Regen ahora se aplican y tickean en el combate
jugable real (antes solo en tests EditMode). `PrepareRepeat` no arrastra estado del intento
anterior. Energía offline no es duplicable. Singletons documentados.

**Pendiente de verificación manual** (no se puede confirmar sin abrir Unity — ver checklist al
final del documento): compilar sin errores, correr los 32 EditMode + 18 PlayMode existentes, y un
smoke test manual de un combate donde un héroe use una habilidad con Bleed/Stun/Regen para
confirmar visualmente que el efecto se aplica y tickea.

---

## SPRINT 0.5 — Infraestructura compartida antes del primer contenido nuevo — CERRADO 2026-07-04

**Depende de**: Sprint 0 cerrado.
**Objetivo**: construir las dos piezas de infraestructura transversal que Sprint 1 (HeroScene) ya
necesita, para no reinventarlas a mitad de esa Scene ni repetir el patrón sin pooling del proyecto
original.

- ✅ **`PooledGridView`** (`Assets/Scripts/UI/Common/PooledGridView.cs`): grid con recycling real de
  celdas — solo mantiene instanciadas las filas visibles + buffer, en vez de todo el catálogo. La
  lógica de qué filas están visibles (`VisibleRowRange`) es un método estático puro, testeado en
  EditMode con un catálogo simulado de 250 items (`PooledGridViewTests.cs`, 4 tests). API: `Init(...)`
  para poblar por primera vez, `SetItemCount(...)` para refrescar tras filtrar/ordenar sin perder el
  pool ya instanciado.
- ✅ **`EditorUIBuilder`**: los 12 Setup Scripts migrados por completo (no solo los 2 iniciales de
  Sprint 0). De paso se unificó la convención de `Hex()` (algunos archivos omitían el `#`) y se
  detectó que `SetAnchors` tenía comportamiento inconsistente entre archivos (3 de 4 hacían `return`
  silencioso si faltaba el `RectTransform` en vez de crearlo) — consolidado a un único comportamiento.

**DoD cumplido**: EditMode 27/27 · PlayMode 15/15 verificado por CLI tras el cambio (incluye un fix
de un test flaky pre-existente, `CritMultiplier_IncreasesWithCritDmg`, encontrado durante esta
verificación — no relacionado con Sprint 0.5, pero corregido de paso con el mismo patrón de
reintentos ya usado en el resto del archivo).

**Nota para Sprint 1**: `PooledGridView` aún no se ha probado dentro de una Scene real con
`GridLayoutGroup`/`ContentSizeFitter` reales — la próxima vez que se use (HeroScene) confirmar que
el toggle de vista compacta/expandida (idea rescatada del proyecto original) encaja bien con este
componente o si necesita alguna adaptación puntual.

---

## SPRINT 1 — HeroScene núcleo (roster + detalle)

**Depende de**: Sprint 0.5 cerrado.
**Objetivo**: Scene de gestión de colección jugable — grid pooled, panel de detalle con tabs,
favoritos. Integra con GearSystem y HeroProgressionSystem ya existentes.

**Alcance**: grid de roster (usa `PooledScrollList`), tabs Info/Habilidades/Equipo (patrón de
`ShowPanel()` un panel visible a la vez, tomado del proyecto original pero sin lógica de negocio en
el controller), favoritos, primer componente reusable de feedback/toast.
**Fuera de alcance**: Maestrías, Ascensión, Substats (sprints siguientes).

**DoD**: navegar MainMenu→HeroScene, ver el roster completo del jugador sin lag perceptible con
roster grande simulado, abrir detalle de un héroe, cambiar de tab, marcar favorito y que persista
tras salir/reentrar a la Scene.

---

## SPRINT 2 — Maestrías + Ascensión

**Depende de**: Sprint 1 cerrado.
**Alcance**: árbol de 66 nodos / 3 ramas dentro de HeroScene. Portar la técnica `UILineConnector`
del proyecto original (Image UI estirada+rotada entre nodos, sin LineRenderer) para las líneas del
árbol. Ascensión por estrellas.
**DoD**: comprar un nodo de maestría con materiales reales, ver el nodo reflejado visualmente
conectado a su padre, ascender un héroe y ver el cambio de stats aplicado en combate.

---

## SPRINT 3 — Sistema completo de efectos de estado (los ~40 que quedaron fuera de Sprint 0)

**Depende de**: Sprint 0 cerrado (puede correr en paralelo con Sprints 1–2 si hay ancho de banda,
pero bloquea cualquier sprint de combate con jefes especiales — Torre/Mazmorra/WorldBoss).
**Objetivo**: cubrir el resto del catálogo de `SkillEffect.type` — shields reales (con semántica de
`value` decidida explícitamente, no asumida), buffs/debuffs de stat (atk/def/spd/agi/luk/tcri/dcri
up/down), control de turno (`turn_bar_up/down`, `decrease_cooldowns`), utilidad (`revive`,
`cleanse`, `taunt`, `counterattack`, `mark`, `blind`, `unkillable`, `reflect_damage`, `block_buffs`, etc.).
**Nota de diseño obligatoria antes de escribir código**: decidir la semántica de `SkillEffect.value`
por tipo de efecto (¿% de stat, flat, % de HP?) consultando el GDD — no inferirla del nombre del
campo.
**DoD**: todos los `type` presentes en `hero_catalog.json` tienen un mapeo consciente (implementado
o explícitamente descartado con razón documentada), ningún combate real depende ya de efectos
"fantasma" que se seleccionan en UI pero no hacen nada.

---

## SPRINT 4 — Substats de Gear

**Depende de**: Sprint 1 cerrado (no depende de Sprint 3).
**Alcance**: rolls +3/+6/+9/+12, distribución triangular (ya hay precedente en GearSystem), 11
substats. Extiende GearSystem existente.
**DoD**: piezas de gear nuevas dropean con substats visibles, se pueden ver en el panel de equipo de
HeroScene, afectan el cálculo de daño real en combate.

---

## SPRINT 5 — ShopScene + RevenueCat IAP real

**Depende de**: ninguno de los anteriores estrictamente, pero conviene después de Sprint 1 para
tener contenido que mostrar en la tienda.
**DoD**: compra real de prueba (sandbox RevenueCat) entrega el producto correcto y actualiza
PlayerData vía checkpoint.

---

## SPRINT 6 — GachaScene (Pozonegro™)

**Depende de**: Sprint 5 cerrado (necesita Caosifera comprable) — aunque el pull con Scrolls (F2P)
podría desbloquearse antes si se decide adelantar.
**Bloqueante de diseño — replanificación obligatoria antes de abrir este sprint**: existen DOS
versiones del diseño de gacha sin reconciliar (`Sprint2_Gacha_IAP_Jefes.docx`: una moneda, pity
75/100 · `GDD_COMPLETO` sección 12: dos fragmentos, pity dual/triple). **Decidir cuál es la versión
final es la primera tarea de este sprint, antes de cualquier código.**
**DoD**: pull x1/x10 con animación, pity funcionando según la versión de diseño elegida, RNG
resuelto en Cloud Function (nunca en cliente).

---

## SPRINT 7 — Pase Oscuro

**Depende de**: Sprint 5 y una versión mínima de tracking de misiones (puede ser un contador
simple, no hace falta MissionScene completa todavía).
**DoD**: progreso de pase visible, reclamar recompensa de un nivel actualiza PlayerData.

---

## SPRINT 8 — TowerScene

**Depende de**: Sprint 0 cerrado (usa CombatSystem), Sprint 3 recomendado si los bosses de Torre
usan efectos fuera de los 5 básicos (probable, confirmar contra el catálogo real de esos encuentros
antes de abrir el sprint).
**DoD**: 100 escalones navegables, rotación de dificultad, ranking mensual básico.

---

## SPRINT 9 — MazmorraScene

**Fuente de verdad de diseño**: `Sprint9_Mazmorras.docx` (10 mazmorras × 10 niveles, rotación
semanal). El detalle de "5 Guardianes G1-G12" del GDD principal es una versión anterior — **no
implementar esa parte**, ya fue descartada por el propio proyecto (Sprint9 es el doc más reciente).
**Depende de**: Sprint 0 (CombatSystem), nivel 7 de PlayerProgressionSystem (Maestrías) para la zona
de Altar de Maestrías.
**DoD**: 3 zonas navegables, rotación elemental semanal, 3 intentos/día compartidos.

---

## SPRINT 10 — ConjuroScene

**Alcance**: sistema nuevo por completo — 10 conjuros equipables, 4 afinidades, mejora, combinación.
Requiere nuevo catálogo `conjuro_catalog.json` (no existe aún).
**DoD**: equipar/mejorar/combinar un conjuro, ver su efecto reflejado en combate.

---

## SPRINT 11 — ArenaScene

**Depende de**: Sprint 0 (CombatSystem en modo "equipo controlado por IA", que hoy no existe —
construirlo aquí).
**DoD**: ataque contra defensa pregrabada resuelto vía Cloud Function transaccional, liga y
matchmaking ±15% trofeos funcionando.

---

## SPRINT 12 — Altar de la Corrupción

**Depende de**: Sprint 11 cerrado (Arena nivel 10, Almas Corruptas).
**DoD**: 7 líneas de stat globales × 10 niveles aplicando bonus permanente verificable en combate.

---

## SPRINT 13 — WorldBossScene + Correo (Mail)

**Depende de**: Sprint 11 cerrado (Cloud Functions transaccionales ya probadas con Arena).
**Alcance incluye** el sistema de Correo transversal (ver sección de sistemas transversales) — se
construye aquí porque es la primera Scene que lo necesita de verdad.
**DoD**: ataque a boss con HP compartida server-side sin race conditions bajo prueba de
concurrencia básica, recompensas entregadas vía Correo.

---

## SPRINT 14 — ClanScene MVP (recortado deliberadamente)

**Decisión de alcance ya tomada, no reabrir sin motivo fuerte**: SOLO Boss de Clan cooperativo (sin
tiempo real) + donaciones. Nada de chat en tiempo real, raids de 3 fases, ni búsqueda/matching de
clanes en esta versión — el propio GDD ya marca el equivalente en tiempo real como Post-MVP.
**DoD**: crear/unirse a clan, donar, contribuir daño a un boss de clan compartido, ranking de
contribución visible.

**Evolutivo (no antes de que el resto del juego esté estable)**: chat, raids, tienda de clan,
búsqueda avanzada.

---

## SPRINT 15 — MissionScene MVP (diarias + semanales)

**Depende de**: cuantos más sprints de contenido estén cerrados, más eventos de EventBus habrá para
enganchar — por eso va casi al final. Logros (75)/medallas (32)/tracking completo se posponen a un
sprint evolutivo posterior, no entran aquí.
**DoD**: misión diaria se completa automáticamente al disparar el evento correspondiente, reclamo
de recompensa actualiza PlayerData.

---

## SPRINT 16 — Presencia Maldita

**Depende de**: Sprint 4 (Substats de Gear — el peso del gear en la fórmula de PM depende de esto).
**DoD**: score recalculado server-side tras cambios de inventario, visible en el perfil del jugador.

---

## SPRINT 17 — TutorialScene

**Depende de**: Campaña, Combate, HeroScene, GachaScene, ConjuroScene ya jugables (es overlay sobre
Scenes reales, no tiene sentido antes).
**DoD**: onboarding de 12 pasos completable sin salirse del flujo real de las Scenes.

---

## SPRINT 18 — Pre-producción (bloqueante para cualquier lanzamiento)

**Alcance**: Firestore security rules · Google Sign-In SDK nativo (la lógica C# ya existe, falta el
plugin que obtiene el idToken) · Account Linking cross-platform (7 casos de flujo, sesión propia y
aislada por ser lógica de auth delicada) · QA de balance completo (7 jefes de campaña con IA de
fases) · build store-ready.
**DoD**: build firmada subida a testing interno de Play Store / TestFlight, sin bloqueantes de
seguridad abiertos.

---

## Evolutivo / fuera de todos los sprints por ahora

- **Base del Mal (idle builder)**: 6 edificios idle, autocontenido, sin dependencias. Hueco de
  calendario oportunista, no tiene sprint fijo asignado.
- Chat de Clan, raids, búsqueda de clanes — post-MVP explícito.
- Logros/medallas/pase completo de MissionScene — evolutivo tras Sprint 15.

---

## Replanificaciones

Registro de decisiones de alcance que se tomaron a mitad de un sprint porque el sprint tal como
estaba planteado resultó "insalvable" en su forma original. Cada entrada: qué se descubrió, qué se
decidió, por qué.

- **2026-07-02 — Sprint 0**: el fix de "CombatSystem effects no se aplican" se descubrió con mucho
  más alcance del esperado (45 tipos de efecto en el catálogo, no ~6). Decisión: cerrar Sprint 0 con
  solo los 5 efectos que ya tenían lógica completa (Bleed/Burn/Poison/Stun/Regen), mover el resto a
  un Sprint 3 dedicado. Razón: mantener Sprint 0 con un DoD alcanzable en una sesión en vez de que
  se convierta en un sprint de alcance indefinido a mitad de la estabilización.

---

## Verificación de Sprint 0 — COMPLETADA 2026-07-04

Automatizada vía Unity CLI batch mode (ver comando de referencia en `ESTADO_PROYECTO.md`),
resultados en `Builds/CI/*.xml`:

1. ✅ Compila sin errores rojos.
2. ✅ EditMode 23/23 · PlayMode 15/15 (conteo menor a los "32/18" históricos porque esta sesión
   también eliminó tests tautológicos que no probaban código real — ver sección de limpieza en
   `ESTADO_PROYECTO.md`).
3. ⏳ Pendiente (requiere ojo humano, no automatizable): smoke test visual de Bleed/Stun en combate
   real — los tests automatizados ya verifican la lógica (`CombatSystemTests.TickEffects_*`), pero
   confirmar que se VE bien en pantalla (posición de textos, timing de animación) es una tarea de
   GUI que le corresponde al usuario.
4. ✅ Cubierto por test automatizado (`CombatSceneData_PrepareRepeat_RevivesDeadPlayerHero`).

**Sprint 0 queda formalmente CERRADO.** Sprint 0.5 puede abrirse.
