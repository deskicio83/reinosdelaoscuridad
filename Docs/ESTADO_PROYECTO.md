# ESTADO DEL PROYECTO — Reino de la Oscuridad
_Actualizar al final de cada sesión de Claude Code_

## Setup completado
- [x] Unity 6000.4.0f1 · URP 2D · 1280×720 landscape
- [x] Packages instalados (Addressables, Firebase, RevenueCat, TMP, Localization, Newtonsoft.Json)
- [x] Estructura de carpetas Assets/ creada
- [x] CLAUDE.md en raíz
- [x] JSONs copiados a Assets/Data/

## Sistemas implementados
- [x] S02 — Modelos C# tipados para JSONs principales
  - `GlobalVariables.cs` · `HeroCatalog.cs` · `PlayerData.cs`
- [x] S03 — Núcleo de arquitectura
  - `ISystem.cs` · `EventBus.cs` · `EventData.cs` · `GameManager.cs`
- [x] S04 — PlayerDataSystem (GetPlayerData · UpdatePlayerData · Dirty · SetUID/GetUID)
- [x] S04b — Migración a Newtonsoft.Json (Dictionary<string,T> funciona)
- [x] S05 — EconomySystem (energía 4 min/unidad · oro · caosifera · offline regen)
- [x] S06 — AuthSystem (Google · Apple · Facebook · Guest · OnAuthStateChanged)
- [x] S07 — DataStorageSystem (1 read/sesión · writes en checkpoints · modo offline)
- [x] S08 — BootSceneController
  - `BootSceneController.cs` — Firebase init → Auth → Firestore → Energía offline → Navegación
  - Guard defensivo: verifica GameManager e ISystem antes de continuar el flujo
  - Paneles UI via SerializeField: loading · login · error · retry
  - `tutorialCompleted` en PlayerData — navega a TutorialScene o MainMenuScene
  - Flujo validado en BootScene.unity con todos los sistemas ✓
- [x] S09 — UIManager
  - `UIManager.cs` — Singleton DontDestroyOnLoad, implementa ISystem
  - `NavigateTo(string)`: async Task, fade 200ms entrada+salida, historial máx 5 Scenes
  - `NavigateBack()`: extrae última Scene del historial
  - `ShowOverlay(GameObject)`: instancia prefab, máx 2 activos (auto-cierra el más antiguo)
  - `HideOverlay(GameObject)`: Destroy + elimina del stack
  - `ActiveOverlayCount`: propiedad pública
  - FadeCanvas: Canvas sortingOrder=999, Image full-screen negro, DontDestroyOnLoad
  - Back button Android: `Keyboard.current.escapeKey.wasPressedThisFrame` (New Input System)
  - `BootSceneController.cs` actualizado — usa `UIManager.Instance.NavigateTo()` en lugar de SceneManager
  - Validado con S09_Test: 5/5 checks PASS ✓
- [x] S10 — HUD global (CurrencyPill + HUDIcons)
  - `Assets/Scripts/Data/EnumData.cs` — enum `CurrencyType` (Energy · Gold · Caosifera); fichero central de enums
  - `Assets/Scripts/UI/HUD/CurrencyPill.cs` — pill reutilizable: `UpdateValue(current, max)`, plusButton → ShopScene
  - `Assets/Scripts/UI/HUD/HUDController.cs` — suscripción a `EventBus.OnCurrencyChanged`, `SetVisible(bool)`, `RefreshAll()`
  - `Assets/Scripts/UI/HUD/HUDIcons.cs` — botones chat/mail/settings, badge de mail, prefabs de overlay asignados en S32
  - Validado con S10_Test: 5/5 checks PASS ✓

- [x] S10b — Editor Scripts HUD + MainMenuScene
  - `Assets/Scripts/Utils/UIConstants.cs` — constantes globales de layout (HUD_HEIGHT_PERCENT=0.10, CONTENT_START_PERCENT=0.90)
  - `Assets/Editor/Setup/SetupHUDPrefab.cs` — menú Tools → Reino Oscuridad → 1. Setup HUD Prefab
    Genera HUD.prefab con 4 zonas (Background · ZonaJugador · ZonaMonedas · ZonaIconos), anclas relativas, referencias asignadas via SerializedObject
  - Todo posicionamiento via anchorMin/anchorMax — cero píxeles absolutos
- [x] S10c — MainMenuScene layout Bastión Maldito ✓ VALIDADO EN PLAY MODE
  - `Assets/Editor/Setup/SetupMainMenuScene.cs` — reescrito y validado
    - ZonaEdificios (0,0.14→1,0.90): ScrollRect bidireccional (horizontal+vertical)
      · ZonaEdificios(ScrollRect) > Viewport(RectMask2D) > ContentEdificios(1900×640) > 11 edificios
      · Edificios en grid 3 filas con anchoredPosition relativas al centro del Content
      · ContentEdificios: Image transparente raycastTarget=true → drag en espacio vacío funciona
      · RectMask2D en lugar de Mask+Image (Image alpha=0 no escribía al stencil buffer)
    - ZonaAccesosRapidos (0,0→0.52,0.14): 6 iconos fijos, Triloguzano en el extremo derecho
    - SubIconosAbanico: hijo del Canvas (no de ZonaAccesosRapidos), grid 2×2
      · Torre=sup-izq, Boss=sup-der, Mazmorra=inf-der; Triloguzano en la barra = inf-izq
      · Aparece/desaparece al pulsar Btn_Triloguzano (toggle SetActive)
    - HUD.prefab instanciado en runtime por MainMenuController (no en design-time)
  - `Assets/Scripts/UI/MainMenuScene/MainMenuController.cs` — actualizado
    - GoToTriloguzano(): toggle SubIconosAbanico.SetActive
    - GoToChat/Mail/Settings/Event/Profile(): log TODO overlay S32
  - `Assets/Scripts/UI/MainMenuScene/MainMenuDebug.cs` — debug temporal (eliminar antes de producción)
- [x] S11 — MainMenuController
  - Hub de navegación central, Awake guard defensivo, Start instancia HUD + BindButtons
  - Validado con S11_Test: checks PASS ✓

## Scenes implementadas
- [x] BootScene — `Assets/Scenes/BootScene.unity` — sistemas + BootController
- [x] MainMenuScene — `Assets/Scenes/MainMenuScene.unity` — generada con Editor Script, validada en Play Mode ✓

## Prefabs creados
- [x] `Assets/Prefabs/UI/HUD.prefab` — HUDController + HUDIcons + 3× CurrencyPill

## Notas técnicas — CombatScene
- **6 zonas**: BarraOrdenTurno (izq) · ZonaEnemigos (centro sup) · PanelControles (der sup) · ZonaEquipo (inf izq) · ZonaConjuros (inf der) · ResultPanel (overlay)
- Enemigos dinámicos: 1-3 slots, `SetActive(false)` para los vacíos al iniciar
- Equipo siempre 4 slots (WorldBoss 20 fuera de scope actual)
- Habilidades flotan sobre la carta activa (`IconosHabilidad` SetActive per turno)
- Conjuros: 2×5 = 10 slots, esquina inferior derecha
- InputBlocker activo durante ResultPanel (Sort Order 49)
- Flujo: `CombatSceneData.PendingContext` → `NavigateTo("CombatScene")` → `Start()` → `ProcessCombat` → `ResultPanel` → `NavigateTo(callerScene)`

## Notas técnicas
- Orden de ejecución (`DefaultExecutionOrder`):
  GameManager: -100 · UIManager: -75 · PlayerDataSystem: -50 · EconomySystem: -25 · AuthSystem: -20 · DataStorageSystem: -15
- Deserialización: **Newtonsoft.Json** en todo el proyecto
- Firebase SDK 13.9.0: `SignInAnonymouslyAsync` → `Task<AuthResult>.User` · `SignInWithCredentialAsync` → `Task<FirebaseUser>`
- `google-services.json` y `GoogleService-Info.plist` en Assets/ — en `.gitignore`, cada desarrollador los coloca en local
- BootScene contiene todos los GameObjects de sistemas (DontDestroyOnLoad) — solo necesitan estar aquí
- **New Input System**: nunca usar `Input.GetKeyDown`. Usar `Keyboard.current`, `Touchscreen.current` o ActionAsset
- **UI**: posicionamiento siempre por anclas relativas (0–1). NUNCA píxeles absolutos. `UIConstants` define márgenes globales. Editor Scripts en `Assets/Editor/Setup/` configuran cada Scene automáticamente.
- **InputBlocker**: panel bloqueante reutilizable entre Scene y overlay. UIManager lo gestiona automáticamente al abrir/cerrar overlays. Sort Order 49 por defecto (por debajo de cualquier overlay).
- **LoadingScreen**: Sort Order 998, DontDestroyOnLoad, frases y carrusel configurables. Usar Show/SetProgress/Hide en cualquier carga async. Hide() hace fade out 200ms.

## ⚠️ Bugs conocidos / Pendientes
- **Firestore parse error** (no bloqueante): documento de uid `jgdMjlq3sdRmFKCRcXz8b7WPDz43`
  en Firestore tiene `artifactInventory[0].artifactId` = array en lugar de string.
  Solución: borrar el documento desde Firebase Console. El fallback a datos locales funciona correctamente.
- **Firestore security rules**: configurar antes de producción:
  `allow read, write: if request.auth != null && request.auth.uid == userId;`
- **Google Sign-In SDK**: pendiente de integrar (LoginWithGoogle lanza NotImplementedException)
- **TutorialScene**: no existe aún — añadir a Build Settings cuando se cree
## Pendiente antes de producción
- `Assets/Scripts/UI/MainMenuScene/MainMenuDebug.cs` — eliminar o desactivar
- **Firestore security rules**: `allow read, write: if request.auth != null && request.auth.uid == userId;`
- **Google Sign-In SDK**: pendiente de integrar
- **TutorialScene**: no existe aún

- [x] S12 — InputBlocker + LoadingScreen + Modelos datos combate
  - `Assets/Scripts/UI/Common/InputBlocker.cs` — singleton por Scene, Canvas SSO, Image transparente raycastTarget=true, Show(sortOrder)/Hide()
  - `Assets/Scripts/UI/Common/LoadingScreen.cs` — singleton DontDestroyOnLoad, Sort Order 998, Show/Hide(fade 200ms)/SetProgress(float)/SetProgress(float,float), 8 frases, carrusel de sprites
  - `Assets/Editor/Setup/SetupInputBlockerPrefab.cs` — menú 3. Setup InputBlocker Prefab → Assets/Prefabs/UI/InputBlocker.prefab
  - `Assets/Editor/Setup/SetupLoadingScreenPrefab.cs` — menú 4. Setup LoadingScreen Prefab → Assets/Prefabs/UI/LoadingScreen.prefab
  - `Assets/Scripts/Core/UIManager.cs` — integración InputBlocker: Show al abrir overlay, Hide al cerrar el último
  - `Assets/Scripts/Data/CombatContext.cs` — datos entrada CombatScene (encounterID, callerScene, combatMode, playerTeam, enemyTeam, maldicionActiva, elementoBoss)
  - `Assets/Scripts/Data/CombatResult.cs` — datos salida CombatScene (victoria, danoTotal, drops, xpGanada, trofeosDelta, gradoObtenido)
  - `Assets/Scripts/Data/HeroInstance.cs` — héroe en combate (hp, stats, efectosActivos, habilidadesEquipadas)
  - `Assets/Scripts/Data/EnemyInstance.cs` — enemigo en combate (hp, stats, efectosActivos)
  - `Assets/Scripts/Data/DungeonContext.cs` — contexto mazmorra (dungeonId, nivelDungeon, elementoDungeon, callerScene)
  - Validado S12_Test: 10/10 PASS ✓

- [x] S13 — CombatSystem — lógica de combate por turnos completa (pura, sin UI)
  - `Assets/Scripts/Systems/CombatSystem.cs` — `[DefaultExecutionOrder(-10)]`, implementa `ISystem`
    - `ProcessCombat(CombatContext)` → `CombatResult`: bucle de turnos (máx 50), orden SPD desc, empates jugador primero
    - `CalculateDamage(HeroInstance, EnemyInstance, float)` → `DamageResult`: 5 pasos Dodge→Crit→Base→Elemental→Efecto
    - `CalculateDamageEnemyAttack(EnemyInstance, HeroInstance)` → `DamageResult`: enemigos crit base 5 %
    - `TryApplyEffect(TipoEfecto, List<string>, int, int)` → bool: máx 3 stacks, chance = acc/100 - res/100 clamped [0.10, 0.90]
    - `RemoveEffect(TipoEfecto, List<string>)` → bool: Bleed no removible (devuelve false)
    - `GetElementalMultiplier(string, string)` → float: tabla 12 relaciones + mismo elemento 0.85
    - Shield absorbe daño antes de HP; Stun salta turno; DoT (Bleed 8 %, Burn 6 %, Poison 5 % hpMax/turno)
  - `Assets/Scripts/Data/EnumData.cs` — añadido enum `TipoEfecto` (17 valores)
  - `Assets/Scripts/Data/CombatContext.cs` — añadido struct `DamageResult`
  - `Assets/Scripts/Data/EventData.cs` — añadidos structs `CombatTurnEndData` · `UnitDamagedData` · `UnitDefeatedData` · `EffectAppliedData`
  - `Assets/Scripts/Core/EventBus.cs` — 4 nuevos eventos: `OnCombatTurnEnd` · `OnUnitDamaged` · `OnUnitDefeated` · `OnEffectApplied`
  - Validado S13_Test: 8/8 PASS ✓

- [x] S14 — CombatScene — flujo de turno completo con UI de cartas
  - `Assets/Scripts/Data/CombatSceneData.cs` — pasarela estática entre Scenes (`PendingContext` · `LastResult` · `SetResult`)
  - `Assets/Scripts/UI/CombatScene/CombatSceneController.cs` — MonoBehaviour no singleton
    - `Start()`: recibe CombatContext desde CombatSceneData, muestra LoadingScreen, inicializa UI, inicia bucle de combate
    - `TurnState` enum: WaitingForInput · SelectingTarget · ProcessingAction · ShowingResult · CombatFinished
    - **Modo Auto**: `CombatSystem.ProcessCombat()` ejecuta todo, `SkipToResult()` salta directo al ResultPanel
    - **Modo Manual**: coroutine con `WaitUntil` — `SelectAbility(int)` → `SelectTarget(int)` → `ProcessHeroAction()`; enemigos atacan automáticamente
    - Orden de turno por SPD desc, empates: jugador primero; Stun salta turno
    - `FinalizarCombate(CombatResult)`: publica `EventBus.OnCombatCompleted`, activa `InputBlocker`, muestra ResultPanel
    - `OnContinuarPressed()`: oculta InputBlocker, navega a `ctx.callerScene` via `UIManager.NavigateTo()`
    - HP bars actualizan color dinámico: >60% verde · 30-60% naranja · <30% rojo
  - `Assets/Editor/Setup/SetupCombatScene.cs` — menú 5. Setup CombatScene
    - Camera ortográfica · EventSystem con InputSystemUIInputModule
    - ZonaEnemigos: 3 slots EnemySlot_N con Button + HPBar (Fondo + Relleno + HPText)
    - BarrasHP_Enemigos: 3 barras alineadas sobre los enemigos
    - ZonaEquipo: 5 HeroCard_N con Portrait + NombreHero + HPBar + TurnIndicator
    - PanelHabilidades: 3 botones (BtnHabilidad_0 superior + 1 y 2 en cuadrícula inferior)
    - ControlesCombate: BtnAuto · BtnVelocidad · TurnoText · BtnHuir
    - ResultPanel (SetActive false): TituloResult · GradeText · XPText · DropsText · BtnContinuar
    - Systems GameObject con CombatSystem
    - Todas las referencias SerializeField del CombatSceneController cableadas via SerializedObject

- [x] S14a — CombatScene layout corregido + tooltip de habilidades ✓ VALIDADO EN PLAY MODE
  - **Layout 6 zonas corregido** (anclas relativas, sin píxeles absolutos):
    - `BarraOrdenTurno`: acortada a y 0.40–0.95 (antes cubría 0.08–0.92)
    - `ZonaHabilidades` (nueva): y 0.03–0.37 bajo la barra — 3 círculos BtnHab_0/1/2
    - `ZonaEnemigos`: y 0.55–0.95 · tarjeta: HPBar arriba → Sprite centro → EfectosBar abajo
    - `ZonaEquipo`: y 0.03–0.47 · tarjeta: TurnIndicator → EfectosBar → Portrait → Nombre → HPBar
    - Gap visible entre zonas: y 0.47–0.55 (~58 px a 720 p)
    - `ZonaConjuros`: reducida a x 0.66–0.99, y 0.03–0.43
    - `TooltipPanel` (nuevo): centrado, compartido para habilidades y conjuros; botones "Usar" + "Cerrar"
    - ASCII en todos los labels TMP: `"x1 >"` · `"||"` — sin warnings de unicode
  - **CombatSceneController.cs** actualizado:
    - `_iconosHabilidad[]` eliminado → sustituido por `_abilityCircles[3]` en ZonaHabilidades
    - `ShowTooltip(desc, onConfirm)` / `HideTooltip()` / `OnUsarTooltip()` — tooltip compartido
    - Tap círculo hab → tooltip con "Usar" → `SelectAbility(i)` → elige objetivo
    - Tap conjuro → tooltip con descripción (solo "Cerrar", sin acción)
    - `UpdateAbilityCirclesForHero(heroIndex)` / `DisableAbilityCircles()` sustituyen ShowIconosHabilidad
    - Nuevos SerializeField: `_abilityCircles` · `_tooltipPanel` · `_tooltipText` · `_btnUsarTooltip` · `_btnCerrarTooltip`
  - Flujo validado: soloUnEnemigo=true ✓ · soloUnEnemigo=false ✓ (navegación + combate + ResultPanel)

## Scenes implementadas
- [x] BootScene — `Assets/Scenes/BootScene.unity`
- [x] MainMenuScene — `Assets/Scenes/MainMenuScene.unity`
- [x] CombatScene — `Assets/Scenes/CombatScene.unity` — **regenerar con menú Tools → Reino Oscuridad → 5. Setup CombatScene** (layout v2 listo)

## Siguiente paso
S15 — CampaignScene: Editor Script que configura la scene con mapa de niveles y selección de stage.
