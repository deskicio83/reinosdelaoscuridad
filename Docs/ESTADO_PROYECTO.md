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
  - Validado en Unity: 3/3 checks PASS
- [x] S04 — PlayerDataSystem
  - `PlayerDataSystem.cs` — singleton DontDestroyOnLoad, fuente de verdad en memoria
  - GetPlayerData() · UpdatePlayerData() · MarkDirty() · ClearDirty() · HasPendingChanges
  - SetUID() · GetUID() — añadidos en S06
  - Validado en Unity: 4/4 checks PASS
- [x] S04b — Migración a Newtonsoft.Json
  - `JsonConvert.DeserializeObject` reemplaza `JsonUtility.FromJson`
  - Validado: `awakenInventory.items` = 18 entradas correctas
- [x] S05 — EconomySystem
  - `EconomySystem.cs` — energía, oro negro y caosifera en memoria
  - Regeneración offline: 4 min/unidad (240 s), calculada en Initialize()
  - Validado en Unity: 6/6 checks PASS
- [x] S06 — AuthSystem
  - `AuthSystem.cs` — Firebase Auth con Google / Apple / Facebook / Guest (anónimo)
  - `AuthStateChangedData` añadido a EventData.cs · `OnAuthStateChanged` añadido a EventBus
  - `uid` añadido a PlayerData · `SetUID/GetUID` añadidos a PlayerDataSystem
  - Validado: checks estructurales 1/2/5 PASS · checks 3/4 NO APLICA (sin google-services.json)
  - Error corregido: SDK 13.9.0 — `SignInAnonymouslyAsync()` → `AuthResult.User`, `SignInWithCredentialAsync()` → `FirebaseUser` directo

## Scenes implementadas
_(vacío)_

## Notas técnicas
- Modelos en namespace `ReinoOscuridad.Data`, solo `[Serializable]`, sin MonoBehaviours
- `RangoPM.max` es `int?` — Newtonsoft lo maneja nativamente
- Orden de ejecución garantizado por `DefaultExecutionOrder`:
  - GameManager: -100 · PlayerDataSystem: -50 · EconomySystem: -25 · AuthSystem: -20
- Deserialización: **Newtonsoft.Json** en todo el proyecto
- Firebase SDK 13.9.0: `SignInAnonymouslyAsync` retorna `Task<AuthResult>`, `SignInWithCredentialAsync` retorna `Task<FirebaseUser>`

## ⚠️ PENDIENTE ANTES DE S08 (BootScene)
- Añadir `google-services.json` (Android) y `GoogleService-Info.plist` (iOS) a `Assets/StreamingAssets/`
  para que Firebase inicialice correctamente. Sin estos archivos, Auth y Firestore no funcionan.
  El SDK los busca en `Assets/StreamingAssets/google-services-desktop.json` en el Editor.

- Siguiente paso: BootScene — `FirebaseApp.CheckAndFixDependenciesAsync()` + CatalogLoader + flujo de carga
