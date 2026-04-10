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
  - GetPlayerData() · UpdatePlayerData() · MarkDirty() · ClearDirty() · HasPendingChanges · SetUID/GetUID
  - Validado en Unity: 4/4 checks PASS
- [x] S04b — Migración a Newtonsoft.Json
  - `JsonConvert.DeserializeObject` reemplaza `JsonUtility.FromJson`
  - Validado: `awakenInventory.items` = 18 entradas correctas
- [x] S05 — EconomySystem
  - `EconomySystem.cs` — energía, oro negro y caosifera en memoria
  - Regeneración offline: 4 min/unidad (240 s)
  - Validado en Unity: 6/6 checks PASS
- [x] S06 — AuthSystem
  - `AuthSystem.cs` — Firebase Auth con Google / Apple / Facebook / Guest
  - `AuthStateChangedData` · `OnAuthStateChanged` en EventBus
  - `uid` en PlayerData · `SetUID/GetUID` en PlayerDataSystem
  - Validado: checks estructurales PASS · Firebase Auth funciona (uid real obtenido)
- [x] S07 — DataStorageSystem
  - `DataStorageSystem.cs` — único punto de acceso a Firestore
  - `LoadPlayerDataFromFirestore(uid)` — 1 read por sesión, modo offline si falla
  - `SavePlayerDataToFirestore()` — solo si HasPendingChanges, skip si no hay cambios
  - `OnSessionEnd()` — guarda automáticamente si hay pendientes
  - Validado: 4/4 checks PASS
    - Firebase Auth real: uid=jgdMjlq3sdRmFKCRcXz8b7WPDz43 ✓
    - Firestore "Missing or insufficient permissions" → capturado como modo offline ✓
    - Skip correcto cuando HasPendingChanges=false ✓

## Scenes implementadas
_(vacío)_

## Notas técnicas
- Modelos en namespace `ReinoOscuridad.Data`, solo `[Serializable]`, sin MonoBehaviours
- `RangoPM.max` es `int?` — Newtonsoft lo maneja nativamente
- Orden de ejecución (`DefaultExecutionOrder`):
  GameManager: -100 · PlayerDataSystem: -50 · EconomySystem: -25 · AuthSystem: -20 · DataStorageSystem: -15
- Deserialización: **Newtonsoft.Json** en todo el proyecto
- Firebase SDK 13.9.0: `SignInAnonymouslyAsync` → `Task<AuthResult>` · `SignInWithCredentialAsync` → `Task<FirebaseUser>`
- `google-services.json` y `GoogleService-Info.plist` en Assets/ — en `.gitignore`,
  cada desarrollador los coloca en local. El SDK los busca también en Assets/StreamingAssets/.
- Firestore security rules: actualmente deniegan acceso a cuentas anónimas.
  Configurar reglas para permitir `request.auth.uid == resource.data.uid` antes de S08.

## ⚠️ PENDIENTE ANTES DE S08 (BootScene)
- Configurar reglas de seguridad de Firestore para permitir lectura/escritura
  al usuario autenticado sobre su propio documento:
  `allow read, write: if request.auth != null && request.auth.uid == userId;`
- Siguiente paso: BootScene — `FirebaseApp.CheckAndFixDependenciesAsync()` + CatalogLoader + pantalla de loading
