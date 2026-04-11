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

## Notas técnicas
- Orden de ejecución (`DefaultExecutionOrder`):
  GameManager: -100 · UIManager: -75 · PlayerDataSystem: -50 · EconomySystem: -25 · AuthSystem: -20 · DataStorageSystem: -15
- Deserialización: **Newtonsoft.Json** en todo el proyecto
- Firebase SDK 13.9.0: `SignInAnonymouslyAsync` → `Task<AuthResult>.User` · `SignInWithCredentialAsync` → `Task<FirebaseUser>`
- `google-services.json` y `GoogleService-Info.plist` en Assets/ — en `.gitignore`, cada desarrollador los coloca en local
- BootScene contiene todos los GameObjects de sistemas (DontDestroyOnLoad) — solo necesitan estar aquí
- **New Input System**: nunca usar `Input.GetKeyDown`. Usar `Keyboard.current`, `Touchscreen.current` o ActionAsset
- **UI**: posicionamiento siempre por anclas relativas (0–1). NUNCA píxeles absolutos. `UIConstants` define márgenes globales. Editor Scripts en `Assets/Editor/Setup/` configuran cada Scene automáticamente.

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

## Siguiente paso
S12 — CampaignScene: Editor Script que configura la scene con mapa de niveles y selección de stage.
