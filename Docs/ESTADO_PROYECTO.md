# ESTADO DEL PROYECTO — Reino de la Oscuridad
_Actualizar al final de cada sesión de Claude Code_

## Setup completado
- [x] Unity 6000.4.0f1 · URP 2D · 1280×720 landscape
- [x] Packages instalados (Addressables, Firebase, RevenueCat, TMP, Localization)
- [x] Estructura de carpetas Assets/ creada
- [x] CLAUDE.md en raíz
- [x] JSONs copiados a Assets/Data/

## Sistemas implementados
- [x] S02 — Modelos C# tipados para JSONs principales
  - `GlobalVariables.cs` — variables_globales.json completo
  - `HeroCatalog.cs` — hero_catalog.json (HeroCatalog + HeroData + skills + awaken)
  - `PlayerData.cs` — player_data.json completo (héroe, gear, artefactos, awaken inventory)
- [x] S03 — Núcleo de arquitectura
  - `ISystem.cs` — contrato Initialize / OnSessionStart / OnSessionEnd
  - `EventBus.cs` — bus de eventos estático con 7 eventos + Publish() + ClearAll()
  - `EventData.cs` — structs de datos para cada evento
  - `GameManager.cs` — singleton DontDestroyOnLoad, RegisterSystem<T>, GetSystem<T>, TryFireDailyReset
  - Validado en Unity: 3/3 checks PASS

## Scenes implementadas
_(vacío)_

## Notas técnicas
- Modelos en namespace `ReinoOscuridad.Data`, solo `[Serializable]`, sin MonoBehaviours
- `PlayerBonusStats` existe aunque sea null en el JSON de muestra
- `RangoPM.max` es `int?` para admitir null en el último rango de Presencia Maldita
- `GameManager.NotifySessionStart(lastLoginTimestamp)` lo llama PlayerDataSystem tras cargar Firestore
- `TryFireDailyReset` compara contra medianoche UTC — no accede a Firestore
- Siguiente paso: BootScene — CatalogLoader + PlayerDataSystem (carga Firestore, llama NotifySessionStart)
