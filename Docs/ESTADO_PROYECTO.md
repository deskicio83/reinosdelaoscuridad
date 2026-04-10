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
- [x] S04 — PlayerDataSystem
  - `PlayerDataSystem.cs` — GetPlayerData · UpdatePlayerData · MarkDirty · ClearDirty · SetUID/GetUID
- [x] S04b — Migración a Newtonsoft.Json (Dictionary<string,T> funciona)
- [x] S05 — EconomySystem (energía 4 min/unidad · oro · caosifera · offline regen)
- [x] S06 — AuthSystem (Google · Apple · Facebook · Guest · OnAuthStateChanged)
- [x] S07 — DataStorageSystem (1 read/sesión · writes en checkpoints · modo offline)
- [x] S08 — BootSceneController
  - `BootSceneController.cs` — flujo completo: Firebase init → Auth → Firestore → Energía offline → Navegación
  - Paneles UI via SerializeField: loading · login · error · retry
  - `tutorialCompleted` añadido a PlayerData — navega a TutorialScene o MainMenuScene
  - TODO S09: reemplazar SceneManager.LoadScene por UIManager.LoadScene
  - Validado: 4/4 checks PASS
    - Firebase DependencyStatus.Available ✓
    - AuthSystem.IsLoggedIn=True (sesión persistente del S07) ✓
    - LoadPlayerDataFromFirestore: "Documento no encontrado" → uid en memoria correcto ✓

## Scenes implementadas
- [ ] BootScene — script listo, pendiente crear .unity y asignar referencias en Inspector

## Notas técnicas
- Orden de ejecución (`DefaultExecutionOrder`):
  GameManager: -100 · PlayerDataSystem: -50 · EconomySystem: -25 · AuthSystem: -20 · DataStorageSystem: -15
- Deserialización: **Newtonsoft.Json** en todo el proyecto
- Firebase SDK 13.9.0: `SignInAnonymouslyAsync` → `Task<AuthResult>.User` · `SignInWithCredentialAsync` → `Task<FirebaseUser>`
- `google-services.json` y `GoogleService-Info.plist` en Assets/ — en `.gitignore`,
  cada desarrollador los coloca en local
- Firestore security rules: configurar antes de producción:
  `allow read, write: if request.auth != null && request.auth.uid == userId;`
- Google Sign-In SDK pendiente de integrar (LoginWithGoogle lanza NotImplementedException)

## Pasos manuales pendientes (BootScene)
1. File > New Scene > Save As > Assets/Scenes/BootScene.unity
2. Crear GameObject "BootController" → asignar BootSceneController.cs
3. Crear GameObjects para paneles UI (LoadingPanel · LoginPanel · ErrorPanel) y asignarlos en Inspector
4. Asignar botones Google, Guest y Retry en Inspector
5. Project Settings > Build Settings → BootScene como Scene 0

## Siguiente paso
S09 — UIManager: gestión de Scenes y stack de overlays · reemplazar SceneManager.LoadScene en BootSceneController
