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
  - Validado en Unity: 4/4 checks PASS
- [x] S04b — Migración a Newtonsoft.Json
  - `JsonConvert.DeserializeObject` reemplaza `JsonUtility.FromJson` en PlayerDataSystem
  - Validado: `awakenInventory.items` = 18 entradas correctas
  - Deuda técnica de Dictionary<string,T> resuelta
- [x] S05 — EconomySystem
  - `EconomySystem.cs` — energía, oro negro y caosifera en memoria
  - ConsumeEnergy/AddEnergy · ConsumeGold/AddGold · ConsumeCaosifera/AddCaosifera
  - Regeneración offline: 4 min/unidad (240 s), calculada en Initialize()
  - Publica EventBus.OnCurrencyChanged en cada operación
  - Validado en Unity: 6/6 checks PASS
    - Offline 20 min → +5 unidades exactas ✓
    - JSON de muestra: 258 días offline → cap correcto a 120/120 ✓

## Scenes implementadas
_(vacío)_

## Notas técnicas
- Modelos en namespace `ReinoOscuridad.Data`, solo `[Serializable]`, sin MonoBehaviours
- `RangoPM.max` es `int?` — Newtonsoft lo maneja nativamente
- Orden de ejecución garantizado por `DefaultExecutionOrder`:
  - GameManager: -100 · PlayerDataSystem: -50 · EconomySystem: -25
- `GameManager.NotifySessionStart(lastLoginTimestamp)` lo llama PlayerDataSystem tras cargar Firestore
- `TryFireDailyReset` compara contra medianoche UTC — no accede a Firestore
- Deserialización: **Newtonsoft.Json** en todo el proyecto
- Siguiente paso: BootScene — CatalogLoader + flujo de carga inicial + pantalla de loading
