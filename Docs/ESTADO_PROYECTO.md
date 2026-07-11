# ESTADO DEL PROYECTO — Reino de la Oscuridad
_Última actualización: Sprint 1 — séptima ronda: Habilidades determinista, estrellas+borde en grid, PanelInfo completo con lore — 2026-07-11_
_Pendiente de verificación visual manual — ver checklist en la sección de Sprint 1 más abajo._
_Roadmap completo por sprints cerrados en `Docs/PLAN_DESARROLLO.md` — leer antes de elegir la próxima sesión._

## Si esta sesión no tiene memoria de lo anterior, leer esto primero

- El repo tiene 2 ramas relevantes en el mismo remoto (`origin` = `github.com/deskicio83/reinosdelaoscuridad`):
  `main` = proyecto original abandonado, `v2-clean` (rama activa) = reescritura desde cero. NO
  clonar nada externo — ya está todo en este mismo repo.
  `Docs/PLAN_DESARROLLO.md` incluye la auditoría de qué rescatar de `main` (sección de Sprint 1/HeroScene).
- **Preferencias explícitas del usuario (no volver a preguntar)**:
  - NO añadir comentarios explicativos ni doc-comments al código (`///`, `//` descriptivos) — código
    limpio sin documentación inline. Esto es una corrección explícita, no inferencia.
  - Automatizar todo lo posible. El usuario solo quiere participar en: (a) diseño/cambios de GUI,
    (b) acciones que requieran 100% un humano (cerrar/abrir Unity Editor, decisiones de producto).
  - Verificación de código (compilar, correr tests) la hago yo vía Unity CLI en modo batch — NUNCA
    pedir al usuario que abra Test Runner manualmente. Bloqueo conocido: Unity Editor no puede tener
    el proyecto abierto mientras corro `-batchmode` (conflicto de lock) — pedir que lo cierre primero.
  - Path de resultados de verificación acordado: `Builds/CI/` (`editmode_results.xml`,
    `editmode_log.txt`, `playmode_results.xml`, `playmode_log.txt`) — ya cubierto por `.gitignore`
    (`[Bb]uilds/`). Yo leo los XML y reporto OK/KO, no pido al usuario que los revise.
  - Sprints son cerrados y secuenciales — no abrir el siguiente sin cerrar el DoD del actual. Si
    algo resulta "insalvable" a mitad de sprint, se documenta en la sección "Replanificaciones" de
    `PLAN_DESARROLLO.md` y se ajusta el alcance ahí, no se improvisa en silencio.
- **Verificación completada 2026-07-04** vía Unity CLI batch mode (`-runTests`, resultados en
  `Builds/CI/*.xml`): **EditMode 23/23 ✅ · PlayMode 15/15 ✅**. Sprint 0 + la limpieza de tests +
  la consolidación de EditorUIBuilder quedan confirmados sólidos.
  - Hallazgo real durante la verificación: `PlayModeTests.asmdef` con `includePlatforms:["Editor"]`
    (puesto en S27_build) excluía el assembly del dominio de Play Mode → los 15-18 tests PlayMode
    llevaban desde S27_build sin ejecutarse nunca, en silencio. Fix: `includePlatforms: []` +
    `defineConstraints: ["UNITY_INCLUDE_TESTS"]` (ver `LECCIONES_TECNICAS.md`). Pendiente de
    confirmar en una futura sesión que el build de Android release sigue sin romperse con este
    cambio (no se pudo probar un build Android completo en esta sesión).
  - Comando de referencia para repetir la verificación (Unity cerrado, sin `-quit` combinado con
    `-runTests`, con `-assemblyNames` explícito para no mezclar EditMode/PlayMode):
    `Unity.exe -batchmode -projectPath <repo> -runTests -testPlatform <EditMode|PlayMode> -assemblyNames <EditModeTests|PlayModeTests> -testResults Builds/CI/<...>.xml -logFile Builds/CI/<...>.txt`

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
| S24_fix3 | Firebase PlayerPrefs + cooldowns catálogo + HP bar + Huir + RewardPanel UX | **BUG 1**: `DataStorageSystem.IsDevMode` (static flag). Load: si devMode, lee PlayerPrefs "dev_playerdata". Save: si devMode, escribe PlayerPrefs + ClearDirty. `DevModeLogin()` activa flag antes de SetDevUID. **BUG 2**: `CargarCatalogoHabilidades()` carga hero_catalog desde Resources. `BuildHeroSkillList()` filtra passives, calcula cooldown real via `CalcularCooldownReal(skill, skillLevel)` leyendo levelUp[].change=="cooldown". `GetAbilityMultiplier()` usa multiplicador real del catálogo en ExecuteAction. `UpdateAbilityCirclesForHero` muestra nombre real (name_es) o "CD:N". **BUG 3**: `UpdateHPBar(ATBUnit)` busca HPBar/Relleno en jerarquía de la carta, fallback por nombre. Se llama en ExecuteAction además de RefreshEnemyZone/RefreshHeroZone. **BUG 4**: `OnHuirPressed()` — marca phase=CombatEnd ANTES de StopAllCoroutines, limpia highlights, delay 0.5f. **FIX 5**: TapOrHoldHandler ya implementado (S24_fix). **FIX 6**: `CombatSceneData` añade NextFaseRequest/NextMundo/NextFase/ReturnToPanelFases. `ShowResultPanel` reconfigura botones: victoria→"Siguiente"/"Repetir", derrota→"Repetir"/"Volver". `CampaignSceneController.HandlePostCombatNav()` coroutine abre PanelBatalla o PanelFases según flags. |
| S24_fix3 | CombatSystem DDOL + FindHPRelleno robusto + botones RewardPanel spec | **PROBLEMA 1**: `CombatSystem.Awake()` añade singleton DDOL (`_instance`/`DontDestroyOnLoad`). `GameManager.GetSystem<T>()` salta referencias destruidas (`s is MonoBehaviour mb && mb == null`). `SetupBootScene.Build()` crea CombatSystem GO si no existe. **PROBLEMA 2**: `FindHPRelleno(ATBUnit)` método separado — paths exactos confirmados: heroes→`HPBar/Relleno`, enemies→`HPBar_Enemy/Relleno`, fallback `GetComponentsInChildren<Image>(true)` con include-inactive. **PROBLEMA 3**: `RewardPanel.cs` renombra `_btnVolverMapa→_btnVolver`, `_btnSiguienteFase→_btnSiguiente`. `Initialize()` reposiciona botones para derrota (2-col: 0.05–0.48 / 0.52–0.95). `SetupRewardPanelPrefab.cs` anchors spec: Repetir 0.03–0.32, Volver 0.35–0.64, Siguiente 0.67–0.97 (y: 0.04–0.18). `MakeButton()` acepta `labelColor?` y `fontSize`. |
| S24_fix2 | Skills catálogo + HP bar enemigo + botón Volver victoria | **CORRECCIÓN 1**: `_heroSkillsByHeroId` dict construido en `CargarCatalogoHabilidades()` (heroId→skills no-pasivas). `BuildHeroSkillList()` añade CASO C: si result vacío tras iterar habilidadesEquipadas, usa fallback por heroId directo del catálogo. Comparación skillId con `StringComparison.OrdinalIgnoreCase`. **CORRECCIÓN 2**: `UpdateHPBar()` — paths distintos por tipo: héroes→"HPBar/Relleno", enemigos→"HPBar_Enemy/Relleno". **CORRECCIÓN 3**: `RewardPanel.cs` — nuevo campo `_btnRepetir` + callback `_onRepetir`. `Show()`/`Initialize()` aceptan `Action onRepetir`. Victoria muestra 3 botones (Repetir+Volver+Siguiente), derrota 2 (Repetir+Volver, oculta Siguiente). `SetupRewardPanelPrefab.cs` — layout 3 columnas: BtnRepetir (0.01→0.32) + BtnVolverMapa (0.34→0.65) + BtnSiguienteFase (0.67→0.99). |
| S24_fix4 | CombatContext persist + Huir transición + navegación directa RewardPanel + CombatTestRunner | **BUG A**: `CombatSceneData` añade `LastContext` (preservado en `SetResult` antes de limpiar) y `PrepareRepeat()` (restaura PendingContext=LastContext). `OnRepetirVictoriaPressed` usa `PrepareRepeat()` en vez de `_ctx` directo. **BUG B/C**: `UIManager` expone `public bool IsTransitioning => _isTransitioning`. `OnHuirPressed` añade guard `if (IsTransitioning) return`. **FIX RewardPanel**: elimina callbacks `_onSiguienteFase`/`_onRepetir`. `OnSiguiente/Volver/Repetir` usan `CombatSceneData.LastContext` + `PrepareRepeat()` + navegación directa UIManager. `Show(prefab, result, team)` sin callbacks. Añade `TryParseEncounterId` helper privado. **CombatTestRunner**: `Assets/Scripts/Utils/CombatTestRunner.cs` — PASS/FAIL para Context, ATB, HPBar. |
| S25_tests | Batería completa de tests NUnit — 6 EditMode + 3 PlayMode. Assembly definitions separados. Correcciones de firmas (TryApplyEffect, RemoveEffect, danoTotal, LastContext setter). Fix dodge 5% en CalculateDamage_MinimumDamageIsOne (loop 50 reintentos). Fix HP tests PlayMode (sondeo LastResult en vez de ATBUnit refs — héroe 1-shotea al enemigo en ~0.8s). Resultado final: EditMode 32/32 · PlayMode 18/18. |
| Sprint 1 (fixes séptima ronda) | Habilidades con layout 100% determinista + estrellas/borde por rareza en tarjeta + PanelInfo completo con lore + animación de filtros | Séptima ronda: el fix de Habilidades de la ronda anterior (HorizontalLayoutGroup+ContentSizeFitter anidados) TAMPOCO resolvió el problema — el usuario seguía viendo el texto desbordar su fila. **Causa raíz real**: esos componentes dependen de que Unity ejecute un pase de layout automático (via `OnEnable`/cola de rebuild) para propagar anchos antes de poder calcular alturas — frágil quirky cuando el panel está inactivo en el momento en que se construye (caso típico: primera vez que se abre un héroe, `PanelHabilidades` sigue inactivo hasta hacer clic en la pestaña). **Fix definitivo**: se eliminaron por completo `HorizontalLayoutGroup`/`ContentSizeFitter` de esta sección — ahora el ancho disponible se lee directamente de `_habilidadesContent.rect.width` (cálculo de anclas puro, válido esté o no activo el panel) y la altura exacta de cada entrada se calcula con `TMP_Text.GetPreferredValues(texto, anchoDisponible, 0)` **antes** de crear la fila, con posicionamiento manual por anclas — cero dependencia de pases de layout automáticos. Esto expuso un bug real de Unity/TMP durante la verificación PlayMode (`NullReferenceException` en `TMPro.MaterialReference..ctor`): un `TextMeshProUGUI` recién creado con `AddComponent` no tiene su `fontAsset` resuelto hasta su propio Awake/render pass, así que llamar `GetPreferredValues()` inmediatamente revienta — solucionado heredando la fuente de un `TMP_Text` ya inicializado en la Scene (`_detailNombre.font`) antes de medir. **Tarjeta de grid**: nuevo icono de estrellas (`Art/Common/Star.png`, migrado desde `main`) + contador "xN" en el borde inferior del retrato; nuevo borde coloreado por tier de estrellas (`HeroBorderColoured.png`, tintado vía código — no existe campo "rareza" para héroes en el catálogo, así que se usa `stars` como proxy, igual que ya hacía el orden) — añadido también como filtro nuevo (`HeroFilterState.estrellas`, botones "N Estrellas" en el panel de filtros). **PanelInfo**: separado en 2 columnas (antes un solo bloque de texto con mucho hueco vacío) — columna izquierda HP/ATK/DEF/SPD/AGI, columna derecha LUK/CRIT/CRIT DMG/ACC/RES (antes CRIT+CRITDMG y ACC+RES compartían línea; AGI/LUK no se mostraban pese a que `HeroInstance` ya los expone). Nueva sección de lore usando `HeroData.description_es` (ya existía en el catálogo, no es lo mismo que la descripción de la habilidad). Nota: el "+XXXX en verde por bonus de equipo" pedido para más adelante queda pendiente explícitamente para cuando el sistema de equipo esté más maduro. **Tabs**: fontSize de las etiquetas Info/Habil./Equipo/Maestr./Misc. subido de 8 a 11. **Panel de filtros**: convertido a lista con scroll (`BuildScrollContent`, ya no se puede desbordar al añadir más filtros) + fade de apertura/cierre vía `CanvasGroup` + coroutine (antes `SetActive` instantáneo, sensación de "a tirones"). **Awaken**: confirmado que ya estaba contemplado desde antes — `HeroCardView.Bind` y `ShowDetail` ya cargan `portraitAddressableAwaken`/`fullAddressableAwaken` según `playerHero.awaken`, sin cambios necesarios. **Verificado: EditMode 37/37 · PlayMode 18/18** (el NullReferenceException de TMP fue detectado y corregido dentro de esta misma sesión, antes de commitear). Pendiente octava verificación visual. |
| Sprint 1 (fixes sexta ronda) | Bug real de child-skip corregido + filtros en lista vertical + orden asc/desc + iconos en tarjeta del grid | Sexta ronda: el fix de Habilidades de la ronda anterior NO resolvió el problema — el usuario seguía viendo entradas de más y mal encuadradas. **Causa raíz real encontrada**: `foreach (Transform child in contenedor) { child.SetParent(null); Destroy(...) }` es un bug conocido de Unity — el enumerador de `Transform` recorre por índice, y al quitar un hijo dentro del propio loop se desincroniza y **salta hijos alternos**, dejando la mitad de los hijos antiguos sin destruir. Por eso, al cambiar de héroe en Habilidades, quedaban habilidades del héroe anterior mezcladas con las del nuevo (ej. "Puño de Peto"/"Quebrantaescudos" de Defensor apareciendo también en la lista de Despertador). Mismo bug estaba en `BuildEstrellas` y `BuildEquipo`. Fix: nuevo helper `ClearChildren()` que recorre por índice en reversa (`for i = childCount-1; i >= 0; i--`), seguro ante el borrado dentro del loop. **Panel de filtros**: pasó de fila horizontal apretada a lista vertical única (`VerticalLayoutGroup`, un `Tools/Setup` folder), con fila de Ordenar (nivel/estrellas/nombre) + nueva fila de dirección Ascendente/Descendente (`HeroFilterState.ordenAscendente`, nuevo test `FilterAndSort_Ascendente_InvierteOrden`) + fila de Solo Favoritos + lista de 8 elementos, todo como filas de ancho completo. **Iconos favorito/bloqueo**: se revirtieron los overlays sobre el retrato central de `ZonaMedio` (el usuario los quería en el grid, no ahí) y en su lugar `HeroCardView` ahora carga los sprites reales `CorazonLleno.png`/`CandadoCerrado.png` sobre los 2 cuadrados ya existentes en cada tarjeta del roster (antes color plano sin icono). **Verificado: EditMode 36/36 (incluye nuevo test) · PlayMode 18/18.** Se detectó el mismo patrón de bug (`foreach`+`Destroy` dentro del loop) en `CampaignSceneController.cs` (3 apariciones) — flaggeado como tarea aparte, no corregido en esta sesión por estar fuera de alcance de HeroScene. Pendiente séptima verificación visual. |
| Sprint 1 (fixes quinta ronda) | Alineación de cabecera + icono de filtros + estado activo + fix Habilidades + iconos de estado | Quinta ronda de feedback visual sobre el rediseño de 3 zonas: **cabecera**: `CapacidadTexto` + `BtnFiltros` reposicionados para que su ancho combinado termine exactamente en `x=0.32`, el mismo borde derecho que `ZonaRoster` (antes se extendían hasta 0.46, sin relación visual con el grid); `Titulo` movido a la derecha (0.36-0.62) para no chocar con el nuevo bloque. **Filtros**: el botón de texto "Filtros" ahora es un icono cuadrado (`HeroFilter.png`, migrado desde `main`). **Estado activo de filtros**: los botones de elemento y "solo favoritos" cambian de color (`#0D0D1A` → `#A855F7`) cuando están seleccionados — antes no había ninguna señal visual y era imposible saber qué filtro estaba aplicado; se corrigió también un bug latente donde esos botones no tenían `targetGraphic` asignado. **Habilidades — causa raíz encontrada**: la aparente "5 habilidades por esbirro" que reportó el usuario NO es un problema de datos — `hero_catalog.json` tiene exactamente 3 skills por héroe (basic/strong/ultimate, sin passives, verificado en las 96 entradas) — era el bug de layout: cada entrada usaba una altura fija de 130px que no alcanzaba para el texto variable (nombre+CD+descripción+líneas de `levelUp`), así que el contenido desbordaba visualmente sobre la siguiente entrada dando la impresión de más elementos de los que hay. Fix: cada entrada ahora usa `HorizontalLayoutGroup` (icono fijo 84×84 + texto flexible) + `ContentSizeFitter` anidado en la fila y en el texto, de forma que la altura se adapta al contenido real de cada habilidad (patrón estándar de "burbuja auto-dimensionada"). **Iconos de favorito/bloqueo sobre el retrato**: nuevos overlays `IconFavoritoOverlay`/`IconBloqueoOverlay` en las 2 esquinas superiores del retrato de `ZonaMedio` (corazón `CorazonLleno.png` arriba-derecha, candado `CandadoCerrado.png` arriba-izquierda — mismo arte migrado de `main`, `Assets/Art/Corazon*.png`/`Candado*.png`), visibles solo cuando el héroe está favorito/bloqueado respectivamente — antes solo cambiaba el texto del botón, sin señal en el propio retrato. `ImportHeroArt.cs` ahora también escanea la raíz de `Assets/Addressables/Art/HeroScene/` (747/747 sprites marcados Addressable tras el fix). **Verificado: EditMode 35/35 · PlayMode 18/18.** Pendiente sexta verificación visual. |
| Sprint 1 (rediseño) | 3 zonas (roster/medio/nav) + Habilidades y Equipo funcionales + 2 tabs nuevos | Tercera ronda de feedback visual, cambios grandes: **layout**: quitado el toggle vista grande/compacta (una sola vista, 3 columnas fijas); `ZonaRoster` reducida a 30% (antes 50%); nuevo `DetailWrapper` con dos sub-zonas — `ZonaMedio` (~27%: retrato AHORA RECTANGULAR — antes cuadrado —, nivel, estrellas, icono de elemento, botones Favorito/Bloquear) y `ZonaNav` (~36%: barra de 5 tabs + contenido). **Habilidades**: cada entrada carga su icono real (`Assets/Addressables/SkillIcon/{heroId}_{tipo}.png`) y muestra los niveles de mejora (`levelUp`) en **negrita** si ya están desbloqueados según el nivel de habilidad del jugador, en gris si no. **Equipo**: dejó de ser texto de solo lectura — cada slot es un botón que busca piezas compatibles en `PlayerData.gearInventory`, muestra la mejor disponible (mayor `mainStatValue`) y permite equiparla vía `GearSystem.EquipGear` tras confirmar en un popup (versión simplificada: auto-selecciona la mejor pieza, no hay selector visual de lista todavía). **2 tabs nuevos**: Maestrías (placeholder "próximamente", el sistema real es Sprint 2) y Miscelaneo (aloja el botón Eliminar, que antes vivía en la zona de detalle). **Verificado: EditMode 35/35 · PlayMode 18/18.** Pendiente cuarta verificación visual. |
| Sprint 1 (arte real) | Migración de arte de héroes desde `main` + fixes de UX visual | Tras dos rondas de prueba visual: **fixes de layout** — grid a 3 columnas (antes 4), tamaño de celda calculado dinámicamente según el ancho real del viewport (`HeroSceneController.ComputeCellSize`, patrón equivalente a `GridCellResizer` del proyecto original), botones Filtros/Vista reposicionados dentro del 50% izquierdo, feedback visual de click en tarjetas (`Button.targetGraphic` no estaba asignado — sin él Unity no anima ningún estado de botón), z-order del popup de compra corregido (`SetAsLastSibling()` — antes ZonaDetalle se pintaba encima). **Nuevo**: bloquear/eliminar héroe (con popup de confirmación genérico reutilizado para ambos casos), estrellas como fila de `Image` en vez de texto. **Arte real**: 96 héroes × 4 variantes (Avatar/AvatarAwaken/Full/FullAwaken, ~737MB) + iconografía (BorderGear, Elemento, Clase, Faccion, TabMenuBar, SkillIcon — 710 sprites en total) copiados desde la rama `main` (heroId coincide 100% entre catálogos, y `hero_catalog.json` ya tenía las rutas `Assets/Addressables/...` pre-configuradas). Nuevo `Assets/Editor/Setup/ImportHeroArt.cs` marca todo como Addressable + `TextureImporter.textureType=Sprite`. `HeroCardView`/`HeroSceneController` cargan ahora vía `Addressables.LoadAssetAsync<Sprite>` con patrón de guard-token (evita que una celda reciclada por `PooledGridView` muestre el sprite de otro héroe si la carga async llega tarde) y liberan el handle al rebind/destroy. Fix de asmdef: `ReinoOscuridad.asmdef` necesitaba `Unity.Addressables`+`Unity.ResourceManager` explícitos (no se auto-incluyen). **Verificado: EditMode 35/35 · PlayMode 18/18.** Pendiente tercera verificación visual con el arte real. **Sin wirear todavía**: SkillIcon (copiado, no conectado a la tab Habilidades), Clase/Faccion icons (copiados, solo Elemento está wireado en detalle), FondoHeroe/Background/Maestries/FX (no copiados, decorativos o para Sprint 2+). |
| Sprint 1 (extensión) | Huecos expandibles + filtros + 2 vistas (feedback tras primera prueba visual) | Tras la primera prueba visual el usuario pidió: ancho de roster limitado a 50% (antes 60%), sistema de huecos expandibles (`maxHeroSpaces` ya existente, tarjeta "+" al final del grid, popup de compra +10 huecos hasta 200 con coste creciente `oro=1000·N² / caosifera=20·N`, usa `EconomySystem.ConsumeGold/ConsumeCaosifera`), panel de filtros (8 elementos + solo-favoritos + orden Nivel/Estrellas/Nombre — método puro `HeroSceneController.FilterAndSort` testeado), toggle de vista grande/compacta (usa `PlayerData.heroSceneCompactView` ya existente, cambia columnas 4↔6 y tamaño de carta). Fix de bug real encontrado: caracteres `★`/`☆` no soportados por `LiberationSans SDF` (ya documentado en LECCIONES_TECNICAS, se me pasó), corregido en código y en el Editor Script. **Verificado: EditMode 35/35 · PlayMode 18/18.** Pendiente de segunda verificación visual con el layout nuevo. |
| Sprint 1 | HeroScene núcleo — roster + detalle + tabs + favoritos | `HeroScene.unity` nueva (`Assets/Editor/Setup/SetupHeroScene.cs`). `Assets/Scripts/UI/HeroScene/HeroSceneController.cs` + `HeroCardView.cs` nuevos — roster con `PooledGridView` (Sprint 0.5), panel de detalle con tabs Info/Habilidades/Equipo (un panel visible a la vez), favoritos que mutan `PlayerHeroData.favorite` directamente + `PlayerDataSystem.MarkDirty()`. `HeroProgressionSystem.GetHeroData(heroId)` nuevo getter público. `MainMenuController.GoToHeroes()` navega de verdad (antes stub "Próximamente"). Equipo tab de solo lectura (equipar/desequipar es trabajo futuro, fuera de alcance de este sprint). **Verificado: EditMode 30/30 · PlayMode 18/18** (incluye `HeroFlowTests` nuevo: carga de Scene, roster con al menos 1 carta, click abre detalle). **Pendiente de verificación visual manual** — ver checklist de pasos en `PLAN_DESARROLLO.md` sección Sprint 1. |
| Sprint 0.5 | Infraestructura compartida — PooledGridView + resto de Setup Scripts consolidados | `Assets/Scripts/UI/Common/PooledGridView.cs` nuevo — grid con recycling de celdas (solo instancia filas visibles + buffer), `VisibleRowRange()` estático puro testeado en EditMode con catálogo simulado de 250 items (`PooledGridViewTests.cs`, 4 tests). `EditorUIBuilder` terminó de consolidar los 12 Setup Scripts (antes solo 2): añadidos overloads `Child(GameObject,...)` y `SetAnchors(Vector2,Vector2)`, unificada la convención de `Hex()` a exigir siempre `#` (3 archivos lo omitían), y corregida la inconsistencia de `SetAnchors` (3 de 4 copias hacían `return` silencioso si faltaba RectTransform en vez de crearlo). De paso se encontró y arregló un test flaky preexistente (`CombatSystemTests.CritMultiplier_IncreasesWithCritDmg`, no consideraba el dodge mínimo del 5%) durante la verificación por CLI. **Verificado: EditMode 27/27 · PlayMode 15/15.** |
| Sprint 0 (limpieza) | Mantenimiento — auditoría de tests + consolidación de Editor Setup Scripts | `Assets/Tests/`: eliminados `HeroProgressionTests.cs` y `PlayerProgressionTests.cs` completos (100% tautológicos — ninguno de sus 9 tests llamaba a código real; uno afirmaba Mazmorra desbloqueada en nivel 15, ya desactualizado desde S28 donde se movió a nivel 10). `EconomySystemTests.cs` reducido de 4 a 1 test real (los otros 3 eran aritmética local sin conexión a `EconomySystem`) — `EconomySystem.ENERGY_REGEN_SECONDS` pasó de `private` a `public const` para poder testearlo de verdad. `GearSystemTests.cs` reducido de 3 a 1 (2 eran aritmética desconectada de `GearSystem`). `CombatSystemTests.TryApplyEffect_BleedApplied` (tautología `applied \|\| !applied`) reemplazado por test real de tasa de aplicación. `CombatFlowTests.Skills_LoadedFromCatalog` (solo `Assert.Pass`) y `CampaignFlowTests.CombatContext_NotNullBeforeCombat`/`PrepareRepeat_RestoresEnemyHP` (duplicado del test ya existente en `DataModelTests.cs`) eliminados. **Editor Setup Scripts**: `Child/Anch/Img/Txt/Hex` (duplicados carácter por carácter en `SetupCombatScene.cs` y `SetupCampaignScene.cs`) consolidados en `Assets/Editor/Setup/EditorUIBuilder.cs` nuevo, ambos migrados vía `using static`. Pendiente sin tocar: `SetupHUDPrefab.cs`/`SetupLoadingScreenPrefab.cs`/`SetupMainMenuScene.cs` tienen variante local de `Child`/`Hex` sin consolidar (firma distinta). **Riesgo documentado en LECCIONES_TECNICAS.md**: los 12 Setup Scripts destruyen y reconstruyen la Scene completa en cada ejecución — puede borrar ajustes manuales de GUI sin aviso. |
| Sprint 0 | Estabilización — auditoría completa + fixes críticos de combate | Auditoría exhaustiva de código (v2-clean), del proyecto original (`main`/deskicio83) y del GDD → roadmap reestructurado en `Docs/PLAN_DESARROLLO.md` (sprints cerrados con DoD + sistemas transversales documentados). Fixes ejecutados: **(1)** `BootSceneController.ProceedAfterLoginAsync()` resella `lastLoginTimestamp` tras el cálculo de energía offline y el daily reset (antes nunca se actualizaba → energía offline duplicable). **(2)** `CombatSceneData.PrepareRepeat()` ahora reconstruye `playerTeam` con HP/efectos reseteados igual que ya hacía con `enemyTeam` (antes "Repetir" arrastraba héroes muertos del intento anterior). **(3)** Singletons DDOL no documentados (`DataStorageSystem`/`AuthSystem`/`GearSystem`/`HeroProgressionSystem`/`PlayerProgressionSystem`/`LoadingScreen`/`CombatSystem`) formalizados en CLAUDE.md con su razón. **(4)** Hallazgo mayor: `CombatSystem.ProcessCombat()` (con todos los efectos de estado) nunca se llamaba desde producción — `CombatSceneController` reimplementaba su propio loop de daño sin efectos. Wireado: `CombatSystem` gana wrappers públicos `TickEffects(HeroInstance/EnemyInstance)`, `TryApplyEffect(TipoEfecto,List,float chance01)`, `HasStun(...)`. `CombatSceneController` gana `TickInicioDeTurno()` (tick DoT/Regen + Stun al inicio de cada turno vía ATB), `AplicarEfectosDeHabilidad()` (mapea `SkillEffect.type`→`TipoEfecto` tras aplicar daño), `TryFinalizeIfCombatOver()` (extraído del código inline de fin de combate). `BuildHeroSkillList()` ya no descarta el campo `effect` del catálogo (bug de pérdida de datos aparte, corregido de paso). **Alcance real cerrado**: solo 5 de ~45 tipos de efecto del catálogo (Bleed/Burn/Poison/Stun/Regen) — el resto (shield, buffs/debuffs de stat, control de turno, utilidad) queda para el Sprint 3 dedicado, documentado en PLAN_DESARROLLO.md. **Pendiente de verificar en próxima sesión con Unity Editor**: compilación, 32 EditMode + 18 PlayMode, smoke test manual de Bleed/Stun en combate real. |
| S28_newplayer | Jugador nuevo + desbloqueos nivel 1 + LockOverlay táctil + scroll centrado | `DataStorageSystem.CreateNewPlayerData()` — jugador nuevo recibe 3 héroes (AlondriaGuardianaDeLaPureza/AngelDespojado/SabioReparador, nivel 5, estrellas 3), 120 energía, 500 oroNegro, 50 caosifera; se guarda inmediatamente en Firestore. `MainMenuController._nivelRequerido` reemplaza array por `Dictionary<string,int>` con claves = nombres reales de GOs (Edificio_Porton/Campana/Cuartel/Mercado/Misiones=1, Altar/Biblioteca=2, Arena=3, Taverna=5, Forja=7, Mazmorra=10). `RefreshEdificiosLock()` reescrito: lookup dinámico via `GetEdificiosEnContent()` + `btn.enabled=false` para táctil. `CentrarScrollEnMundoActual()` coroutine centra el scroll al inicio. `PlayerProgressionSystem.DESBLOQUEOS` añade nivel 1: campana/tienda/misiones/esbirros; mueve mazmorra+world_boss a nivel 10 (antes 15/20). `SetupMainMenuScene`: `MovementType.Clamped` en ScrollRect. |
| S27_build | Sistema data versioning + BuildAndroid release + APK 82 MB | `UIConstants`: `DATA_VERSION=1` + `DATA_VERSION_KEY="reino_data_version"`. `DataStorageSystem.CheckAndMigrateData()` (estático) — limpia `dev_playerdata` en PlayerPrefs si versión cambia; llamado al inicio de `RunBootSequenceAsync()` ANTES de Firebase init. `BuildAndroid.BuildRelease()` — `BuildOptions.None`, IL2CPP, ARMv7+ARM64, `ManagedStrippingLevel.Minimal`, `OpenGLES3+2`, output `Builds/Android/ReinosOscuridad_v010.apk`. Fix: `PlayModeTests.asmdef includePlatforms:[Editor]` para que no rompa compilación player. **✅ APK generada: 82 MB** — `Builds/Android/ReinosOscuridad_v010.apk` (2026-04-26). |
| S26_balance | Balance combate + bug Huir + tercer botón victoria | **BLOQUE 1 (stats enemigos)**: `CampaignSceneController.BuildEnemyTeam()` reemplaza stats hardcoded (hp=500,atk=120,def=60) por escalado dinámico. Nuevos métodos: `ParseEncounterKeyForStats()` extrae mundo/fase/boss/dificultad de la key. `GetNivelEnemigo(mundo, fase, esBoss)` con `_nivelBaseMundo={1,8,16,24,32,40,48}`. `GetDificultadMultiplier()` x1/1.5/2.5. `BuildEnemyWithStats()` aplica fórmula stat(nivel)=base+(max-base)*(nivel-1)/59 × mult. `Assets/Data/player_data.json` — todos los héroes a level=10, stars=3, equipment=[]. **BLOQUE 2 (bug Huir)**: `OnHuirPressed()` — nuevo método `QuitarTodosLosHighlights()`, logs `[Huir] Iniciando huida` y `[Huir] Navegando de vuelta`, orden correcto StopAllCoroutines→null→limpieza→result→navigate. **BLOQUE 3 (tercer botón)**: Scripts ya correctos desde S24_fix3 — `SetupRewardPanelPrefab.cs` crea 3 botones (BtnRepetir/BtnVolver/BtnSiguiente), `RewardPanel.cs` tiene los 3 SerializeFields y Show() los gestiona. PENDIENTE: regenerar prefab (`Tools → 8. Setup RewardPanel Prefab`). |
| S24_fix5 | Flujo post-combate unificado — doble navegación eliminada | **Causa raíz**: CombatScene + RewardPanel navegaban independientemente causando doble carga de escena. **Solución arquitectural**: CombatScene solo llama `NavigateBack()` tras delay. CampaignScene detecta `LastResult` en `Start()` e instancia RewardPanel con callbacks locales. RewardPanel NO navega: solo `Destroy(gameObject)` + invoke callback. **FIX 1 (CombatSceneController)**: Eliminados `_resultPanel` SerializeFields, `ShowResultPanel`, botones inline. Nuevos métodos `FinalizarCombate(CombatResult)` y `OnHuirPressed()` — ambos async void con `Task.Delay` + `NavigateBack()`. **FIX 2 (RewardPanel)**: Reescrito como overlay puro — `Show(result, team, onRepetir, onSiguiente, onVolver)`. No referencia UIManager ni CombatSceneData. **FIX 3 (CampaignSceneController)**: `CheckCombatReturn()` instancia prefab + `rp.Show(...)` con 3 callbacks locales. `_lastTeamUsado` guardado en `TryEnterBatalla()` antes de navegar. **FIX 4 (SetupCampaignScene)**: Cablea `_rewardPanelPrefab` via `AssetDatabase.LoadAssetAtPath`. **FIX 5 (CombatTestRunner)**: `FindObjectsByType<ATBUnit>(FindObjectsInactive.Include)` sin SortMode (Unity 6 correcto). |

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

**✅ BUILD MVP:** `Builds/Android/ReinosOscuridad_MVP.apk` — **105 MB** — IL2CPP · ARMv7+ARM64 (2026-04-17)

**✅ BUILD RELEASE v0.1.0:** `Builds/Android/ReinosOscuridad_v010.apk` — **82 MB** — IL2CPP · ARMv7+ARM64
- Fecha: 2026-04-26
- Player Settings: bundle 0.1.0, versionCode 1, IL2CPP, ARMv7+ARM64, ManagedStripping Minimal, OpenGLES3+2, LandscapeLeft, Min API 24, Target API 34
- Fix necesario: `PlayModeTests.asmdef` debía tener `includePlatforms:[Editor]` (sin eso falla compilación en player)
- Script: `Assets/Editor/BuildAndroid.cs` → `Tools → Reino Oscuridad → Build Android Release`
- CLI (con Unity cerrado):
```
"C:/Program Files/Unity/Hub/Editor/6000.4.0f1/Editor/Unity.exe" -batchmode
  -projectPath "C:/Users/franh/Documents/GitHub/reinosdelaoscuridad"
  -executeMethod BuildAndroid.BuildRelease -logFile Builds/Android/build_release_log.txt -quit
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
| ~~🟡 Media~~ | ~~RewardPanel prefab — regenerar~~ | ✅ Ejecutado S27_build — `Tools → 8. Setup RewardPanel Prefab` completado. |
| ~~🟡 Media~~ | ~~BootScene — paso manual pendiente~~ | ✅ Ejecutado S27_build — `Tools → 9. Setup BootScene` completado. |
| 🔴 Alta | ACCIÓN MANUAL PENDIENTE — Firestore doc corrupto | Firebase Console → Firestore → colección "players" → documento `jgdMjlq3sdRmFKCRcXz8b7WPDz43` → ELIMINAR. Campo `presenciaMalditaLastCalc` tiene formato objeto anidado donde se espera string. Se recreará limpio en el próximo save. |
| 🟢 Baja | player_data.json en build | Solo se usa como fallback en Editor; en Android Firebase lo reemplaza. Si Firebase falla, el jugador ve datos vacíos |

---

## Diseño — Desbloqueos por nivel

```
Campaña, Tienda, Misiones y Héroes/Esbirros: nivel 1 (contenido principal)
Gacha (básico y avanzado): nivel 2
Arena: nivel 3
Clan: nivel 5
Conjuros: nivel 7
Torre + World Boss + Mazmorra: nivel 10
Pase Oscuro: nivel 25
Altar Corrupción: nivel 30
```
> Tutorial: cuando se implemente (S33), cambiar tutorialCompleted=false en CreateNewPlayerData().

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

**Roadmap completo y detallado (sprints cerrados, DoD, dependencias, sistemas transversales) vive
en `Docs/PLAN_DESARROLLO.md` a partir de ahora — esta tabla es solo un resumen rápido, no la
fuente de verdad.**

| Sprint | Objetivo | Prioridad |
|---|---|---|
| S26_balance | ✓ Completado — Ver arriba | — |
| S27_build | ✓ Completado — data versioning + APK release 82 MB | — |
| S28_newplayer | ✓ Completado — jugador nuevo + desbloqueos + LockOverlay táctil | — |
| Sprint 0 | ✓ Ejecutado — ver fila arriba. Pendiente: verificación manual en Unity (compilación + tests + smoke test) | Alta — verificar antes de Sprint 0.5 |
| Sprint 0.5 | `PooledScrollList` genérico + `EditorUIBuilder` compartido (infraestructura para HeroScene) | Alta |
| Sprint 1 | HeroScene — roster pooled, tabs, favoritos | Alta |
| Sprint 2 | Maestrías + Ascensión | Alta |
| Sprint 3 | Sistema completo de efectos de estado (~40 tipos restantes del catálogo) | Alta — bloquea Torre/Mazmorra/WorldBoss si sus jefes los usan |
| Sprint 4+ | Ver `Docs/PLAN_DESARROLLO.md` — Substats de Gear, Shop/IAP, Gacha, Pase Oscuro, Torre, Mazmorra, Conjuros, Arena, Altar, WorldBoss, Clan MVP, Misiones, PM, Tutorial | — |
| Sprint 18 | Pre-producción: Firestore security rules + Google Sign-In SDK + Account Linking + QA + build store-ready | Bloquea cualquier lanzamiento |
