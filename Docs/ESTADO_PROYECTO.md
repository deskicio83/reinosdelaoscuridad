# ESTADO DEL PROYECTO — Reino de la Oscuridad
_Última actualización: S24_fix2 — 2026-04-19_

---

## Setup completado
- [x] Unity 6000.4.0f1 · URP 2D · 1280×720 landscape
- [x] Packages: Addressables · Input System (New) · TextMeshPro · Localization (ES/EN) · Firebase SDK 13.9.0 · RevenueCat · Newtonsoft.Json
- [x] Estructura de carpetas Assets/ creada
- [x] CLAUDE.md en raíz
- [x] JSONs de catálogo en `Assets/Resources/Data/` (migrado desde `Assets/Data/` en S18b_MVP para compatibilidad Android)
- [x] google-services.json en Assets/ (en .gitignore — cada dev lo coloca en local)

---

## Sistemas implementados

| Sprint | Sistema | Descripción |
|---|---|---|
| S02 | Modelos C# | `GlobalVariables`, `HeroCatalog`, `PlayerData` tipados para los JSONs |
| S03 | Núcleo arquitectura | `ISystem`, `EventBus`, `EventData`, `GameManager` |
| S04 | PlayerDataSystem | `GetPlayerData`, `UpdatePlayerData`, `MarkDirty`, `SetUID/GetUID` |
| S04b | Newtonsoft.Json | Migración a Newtonsoft — `Dictionary<string,T>` funciona |
| S05 | EconomySystem | Energía 4 min/unidad · oro · caosifera · regen offline |
| S06 | AuthSystem | Google · Apple · Facebook · Guest · `OnAuthStateChanged` |
| S07 | DataStorageSystem | 1 read/sesión · writes en checkpoints · modo offline · fix `ToFirestoreValue()` para JArray/JObject |
| S08 | BootSceneController | Firebase init → Auth → Firestore → Energía offline → navegación |
| S09 | UIManager | Singleton DDOL · NavigateTo(fade 200ms) · ShowOverlay/HideOverlay · back button Android |
| S10 | HUD global | `CurrencyPill` · `HUDController` (suscripción EventBus) · `HUDIcons` |
| S10b | Editor Scripts HUD | `UIConstants` · `SetupHUDPrefab` → HUD.prefab |
| S10c | MainMenuScene | Layout Bastión Maldito · edificios scroll · 6 iconos flotantes · Triloguzano abanico |
| S11 | MainMenuController | Hub de navegación · Awake guard · instancia HUD runtime |
| S12 | InputBlocker + LoadingScreen | Panel bloqueante SO49 · LoadingScreen SO998 DDOL fade 200ms · Modelos datos combate |
| S13 | CombatSystem | Combate por turnos puro (sin UI) · 5 pasos: Dodge→Crit→Base→Elemental→Efecto · Bleed no removible |
| S14 | CombatScene UI | Controlador combate · Modo Auto/Manual · HP bars dinámicas · ResultPanel |
| S14a | CombatScene layout | 6 zonas corregidas · ZonaHabilidades fija · Tooltip compartido · ASCII en labels |
| S15 | HeroProgressionSystem | Niveles 1–60 · `BuildHeroInstance` · `TryAwaken` · curva XP |
| S16 | GearSystem | 6 slots · rolls triangular +3/+6/+9/+12 · `BuildCombatInstance` |
| S17 | PlayerProgressionSystem | Desbloqueos por nivel · Pase Oscuro · `AddPlayerXP` · `UpdateEnergyMax` |
| S18 | CampaignScene | 7 mundos · 3 dificultades · nodos fase · desbloqueos secuenciales · navegación CombatScene |
| S18b | BattlePrepPanel | Panel unificado RAID-style · selección equipo 4 héroes · scroll colección · RewardPanel |
| S19_fix | SetupBootScene + validación MVP | `SetupBootScene.cs` Editor Script · `BootSceneController` con `_btnApple` + aliases públicos · `S19fix_Test.cs` 6 checks automáticos |
| S19_fix2 | Correcciones flujo MVP | `tutorialCompleted: true` en player_data.json · fallback TutorialScene→MainMenuScene en BootSceneController · DataStorageSystem catch ya era LogWarning (sin cambio) |
| S20_fix | Correcciones pre-MVP | BootScene siempre muestra LoginPanel en Editor + BtnDevMode · 5 héroes añadidos a player_data.json · LayoutElement en nodos CampaignScene · stars correctas en ResultPanel · RefreshBarraTurno dinámica |
| S20_fix2 | Correcciones layout | SetParent(null)+Destroy en RefreshFaseNodes/BuildHeroCards/BuildEnemyCards · VLG en BarraTurno ListaRetratos · RefreshBarraTurno usa LayoutElement · stars Image sprites (sin Unicode) |
| S21_fix | Sistema ATB + dinámica de combate | `ATBUnit.cs` (tick/IsReady/ResetATB/highlight) · `FloatingDamageText.cs` (spawn+animate 80px 1s) · `CombatSceneController.cs` reescrito — enum CombatPhase · ATBLoop coroutine · PlayerTurnRoutine (manual+auto) · EnemyTurnRoutine (menor HP) · ExecuteAction (animación escala 150ms · daño · floating text · dim muerte) · `SetupCombatScene.cs` — elimina BarraOrdenTurno · ATBBar+CardHighlight+ATBUnit en cada carta/slot · layout nuevo 5 zonas |
| S22_fix | Bugs CombatScene + desbloqueos MainMenu | **BLOQUE 1**: ATBPercent en barras · FIX highlight lingering (_activeUnit=null) · FIX remove Usar/Cancelar (direct OnAbilitySelected) · FIX long-press AbilityTooltip (AbilityTooltip.cs + EventTrigger + BuildAbilityTooltip en SetupCombatScene) · **BLOQUE 3**: MainMenuController RefreshEdificiosLock() + _lockOverlays[11] + NIVEL_REQUERIDO_EDIFICIO · SetupMainMenuScene LockOverlay en 11 edificios |
| S22a | CampaignScene ESTADO 1 — Scroll de Mundos | Reescritura completa CampaignSceneController.cs + SetupCampaignScene.cs. Solo ESTADO 1: ScrollRect horizontal clamped, 7 BtnMundo (LayoutElement 220×260), ContentSizeFitter+HLG padding L/R=280 para centrado, LockedOverlay por mundo, DotsIndicador HLG 7 puntos, PopupBloqueado, CentrarScrollInicial coroutine, IsMundoDesbloqueado basado en _fasesCompletadas[N-1][6]. Stubs OnFaseClick/OnConfirmarEquipo/TryEnterBatalla con Debug.Log("TODO S22b/c"). |
| S22b | CampaignScene ESTADO 2 — PanelFases slide-in | PanelFases (anchors 0.52,0.09→1.00,0.90) · ScrollFases VLG vertical · 7 NodoFase Button (LayoutElement h=72) con NumFase/DropGarantizado/StaminaCost/EstrellasFase/LockIconFase · AnimarPanelFases coroutine SmoothStep 0.2s via anchoredPosition · OpenPanelFases/ClosePanelFases · RefreshNodosFase (color verde/morado/gris + estrellas + interactable + lock) · GetDropGarantizado desde encounter_catalog (drop_gear_slot/drop_tipo) · InicializarPanelFases coroutine para calcular _panelFasesAncho en runtime. |
| S23_fix | Fundido Scenes + CombatScene fixes + MainMenu desbloqueos | **BLOQUE 1**: UIManager.NavigateTo/NavigateBack reescritos — orden correcto: FadeOut → CloseAllOverlaysSilent → LoadSceneAsync(allowSceneActivation=false) → activar → 2 frames → FadeIn. CanvasGroup en FadeCanvas (alpha) en vez de Image.color. _fadeDuration=0.3f SerializeField + FadeDuration propiedad pública. **BLOQUE 2**: ATBPercent + AbilityTooltip + highlight cleanup + OnAbilitySelected directo ya implementados en S22_fix — verificados en Setup. **BLOQUE 3**: MainMenuController suscribe EventBus.OnPlayerLevelUp → RefreshEdificiosLock. SetupMainMenuScene LockOverlay mejorado: LockDim + LockIcon (world_locked sprite) + NivelReqText "Nivel N requerido". |
| S24_fix | Tap vs Long Press habilidades combate | `TapOrHoldHandler.cs` nuevo MonoBehaviour — IPointerDown/Up + coroutine WaitForSeconds(0.2s): si suelta antes → OnTap; si llega al umbral → OnHoldStart; al soltar → OnHoldEnd. Reemplaza onClick+EventTrigger en círculos de habilidad. `OnAbilityTap()` con toggle (tap en habilidad ya seleccionada la desmarca). `ShowAbilityTooltip()` solo muestra info sin modificar selección ni fase. |
| S23_fix2 | Bugs post-S23_fix | **1**: ATBUnit reescrito — abandona Image.Type.Filled (sin sprite no renderiza fill) y usa anchorMax del RectTransform: anchorMax.x = atbValue/100f. **2**: TryEnterBatalla guarda ultimoEncuentroIntentado + cierra paneles al instante SetActive(false). **3**: CheckCombatReturn() en Start(). **4**: MainMenuController LogProximamente() guard. **5**: FloatingDamageText go.SetActive(true) tras SetParent. **6**: Canvas buscado solo en escena activa (fix "735" en DDOL canvas). **7**: OnContinuarPressed usa NavigateBack(). **8**: FinalizarCombate usa MostrarResultadoConDelay coroutine (1.5s) para sincronizar panel de resultado con fin de FloatingDamageText. |
| S22c | CampaignScene ESTADO 3 — PanelBatalla | PanelBatalla pantalla completa (SetActive) · HeaderBatalla con MundoFaseText/DificultadText/BtnVolver · ZonaCentral: PanelEquipo (grid 2x2 slots + ScrollConjuros 3 slots placeholder) · ZonaElemental (elemental_chart placeholder) · PanelEnemigos (ListaEnemigos VLG 3 previews dinámicos + BtnBatallar + CosteEnergia + SinEnergiaText) · FranjaEsbirros ScrollRect horizontal con ContentEsbirros HLG + ContentSizeFitter (poblado dinámico por hero) · ToggleHeroEnEquipo/RefreshSlotsEquipo/RefreshMiniCards · TryEnterBatalla async — construye CombatContext y navega a CombatScene · LoadHeroCatalog para nombres y elementos · GetHeroElemento/Nombre/Nivel helpers. |
| S24_fix2 | Bugs MVP: Firebase dev uid + cooldowns + Huir | **BUG 1**: `AuthSystem.SetDevUID(uid)` — fija uid sin Firebase Auth (IsLoggedIn=true, IsGuest=true, publica AuthStateChangedData). `BootSceneController.DevModeLogin()` llama SetDevUID("dev_mode_player_001") antes de ProceedAfterLoginAsync(). **BUG 2**: Sistema de cooldowns en CombatSceneController — `_cooldowns` dict<heroId, int[3]>. Inicializado en BuildATBUnits. `DecrementarCooldowns` al inicio de PlayerTurnRoutine. `AplicarCooldown` tras ExecuteAction. `UpdateAbilityCirclesForHero` muestra "CD:N" y deshabilita botones en CD. Valores: hab0=0, hab1=2, hab2=3 turnos. **BUG 3 verificado**: daño enemigo→héroe ya correcto (linea 431-436) + RefreshHeroZone usa anchorMax. **BUG 4**: `FinalizarCombate` acepta parámetro delay (default 1.5f). Botón Huir usa delay=0.5f + pasa `dañoTotal=_dañoAcumulado`. |

---

## Scenes implementadas

| Scene | Estado | Notas |
|---|---|---|
| `BootScene.unity` | ✓ completa | Firebase init · Auth · Firestore · LoadingScreen |
| `MainMenuScene.unity` | ✓ completa | 9 edificios scroll · HUD · navegación validada |
| `CombatScene.unity` | ✓ completa | Flujo turno completo · Modo Auto/Manual · ResultPanel |
| `CampaignScene.unity` | ✓ completa | 7 mundos · 3 dificultades · BattlePrepPanel · RewardPanel |
| `HeroScene` | ✗ pendiente | No implementada |
| `GachaScene` | ✗ pendiente | No implementada |
| `ArenaScene` | ✗ pendiente | No implementada |
| `TowerScene` | ✗ pendiente | No implementada |
| Resto de Scenes (8) | ✗ pendiente | No implementadas |

**Build Settings orden correcto:**
- 0: `BootScene` · 1: `MainMenuScene` · 2: `CombatScene` · 3: `CampaignScene`

---

## Assets

**Prefabs:**
- `Assets/Prefabs/UI/HUD.prefab`
- `Assets/Prefabs/UI/InputBlocker.prefab`
- `Assets/Prefabs/UI/LoadingScreen.prefab`
- `Assets/Prefabs/UI/BattlePrepPanel.prefab`
- `Assets/Prefabs/UI/RewardPanel.prefab`

**Placeholders PNG (21 archivos):** `Assets/Resources/Placeholders/`
- Héroes por elemento: fuego · agua · tierra · naturaleza · luz · oscuridad · rayo · hielo
- Enemigos por tipo: humanoide · bestia · no_muerto · demonio · dragon · elemental · generico
- Nodos de mapa: bloqueado · disponible · jefe · completado
- Fondos: boot · campaign · combat · mainmenu

**JSONs catálogos:** `Assets/Resources/Data/` (cargados vía `Resources.Load<TextAsset>()`)
- `hero_catalog.json` · `hero_level_curve.json` · `gear_catalog.json`
- `player_level_curve.json` · `encounter_catalog.json` · `enemy_catalog.json`

**JSONs estáticos no cargados en runtime:** `Assets/Data/`
- `drop_table.json` · `environment_catalog.json` · `spells_catalog.json`
- `player_data.json` (fallback solo Editor — Firebase lo reemplaza en device)

---

## Build Android

**Estado:** Configurada, NO ejecutada (Unity estaba abierto al intentar CLI build)

**Player Settings Android configurados (S18b_MVP):**
- Bundle ID: `com.DesdeMiPC.ReinosdelaOscuridad` ✓
- Min API: 25 (Android 7.1) ✓
- Target/Compile API: 34 (Android 14) ✓ ← corregido 0→33→34 (androidx.credentials requiere ≥34)
- Scripting Backend: IL2CPP ✓
- Target Architectures: ARMv7 + ARM64 (valor 3) ✓ ← corregido de 2→3
- Orientation: Landscape Left forzado ✓ ← corregido de LandscapeRight→LandscapeLeft
- Internet Access: Required ✓ ← corregido de 0→1

**✅ BUILD GENERADA:** `Builds/Android/ReinosOscuridad_MVP.apk` — **105 MB** — IL2CPP · ARMv7+ARM64
- Fecha: 2026-04-17
- Warnings: RevenueCat deprecaciones internas (no bloquean)
- Script: `Assets/Editor/BuildAndroid.cs` → `Tools → Reino Oscuridad → Build Android MVP`

**Para regenerar (CLI, con Unity cerrado):**
```
"C:/Program Files/Unity/Hub/Editor/6000.4.0f1/Editor/Unity.exe" -batchmode
  -projectPath "C:/Users/franh/Documents/GitHub/reinosdelaoscuridad"
  -executeMethod BuildAndroid.Build -logFile Builds/Android/build_log.txt -quit
```

---

## Deuda técnica activa

| Prioridad | Issue | Detalle |
|---|---|---|
| 🔴 Alta | Firestore security rules | `allow read, write: if request.auth != null && request.auth.uid == userId;` — configurar antes de producción |
| 🔴 Alta | Google Sign-In SDK | `LoginWithGoogle()` lanza `NotImplementedException` |
| 🟡 Media | TutorialScene | No existe — añadir a Build Settings cuando se cree |
| 🟡 Media | Energía — feedback visual | Si energía < coste, `OnTeamSelected()` retorna en silencio sin mensaje al jugador |
| 🟡 Media | MainMenuDebug.cs | Eliminar o desactivar antes de producción |
| 🟡 Media | RewardPanel no conectado | `CheckCombatReturn()` llama a `RewardPanel.Show()` pero el flujo completo requiere validación en Play Mode |
| 🔴 Alta | ACCIÓN MANUAL PENDIENTE — Firestore doc corrupto | Firebase Console → Firestore → colección "players" → documento `jgdMjlq3sdRmFKCRcXz8b7WPDz43` → ELIMINAR. Campo `presenciaMalditaLastCalc` tiene formato objeto anidado donde se espera string. Se recreará limpio en el próximo save. |
| 🟢 Baja | player_data.json en build | Solo se usa como fallback en Editor; en Android Firebase lo reemplaza. Si Firebase falla, el jugador ve datos vacíos |

---

## Notas técnicas críticas

- **Input System**: SIEMPRE `InputSystemUIInputModule`. NUNCA `StandaloneInputModule`. Verificado en las 4 Scenes del MVP.
- **JSON loading**: `Resources.Load<TextAsset>("Data/<nombre_sin_extension>")` para catálogos estáticos. `Application.dataPath + "/Data/"` con `File.ReadAllText` SOLO funciona en Editor.
- **Firestore / escrituras**: 1 read en BootScene · writes solo en checkpoints (fin combate, gacha x10, cierre app). NUNCA en Update() ni coroutines periódicas.
- **UI anchors**: siempre relativas (0–1). NUNCA píxeles absolutos. `UIConstants` define márgenes globales.
- **Orden ejecución DefaultExecutionOrder**: GameManager -100 · UIManager -75 · PlayerDataSystem -50 · EconomySystem -25 · AuthSystem -20 · DataStorageSystem -15 · CombatSystem -10 · HeroProgressionSystem -8 · GearSystem -6 · PlayerProgressionSystem -5
- **CombatScene agnostica**: recibe `CombatContext` vía `CombatSceneData.PendingContext`, devuelve `CombatResult` vía `CombatSceneData.LastResult`.
- **EventBus**: única vía de comunicación entre sistemas. No llamadas directas entre sistemas.
- **Bleed**: NO puede removerse por Cleanse (hardcoded en CombatSystem).
- **GearSystem rolls**: distribución triangular sesgada al mínimo: `roll = max - (max-min)*Sqrt(1-u)`.

---

## Próximas sesiones

| Sprint | Objetivo | Prioridad |
|---|---|---|
| S23 | HeroScene — roster, filtros, upgrade, awaken | Alta |
| S23 | HeroScene — roster, filtros, upgrade, awaken | Alta |
| S24 | GachaScene — pull x1/x10, animación, historial | Alta |
| S25 | ArenaScene — PvP asincrónico | Media |
| S26 | RewardPanel — conectar a CheckCombatReturn | Media |
| S27 | ShopScene — paquetes IAP RevenueCat | Media |
| S28 | Google Sign-In SDK integración | Alta (bloquea producción) |
| Prod | Firestore security rules | Antes de cualquier deploy |
