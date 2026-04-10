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
- [x] S04 — PlayerDataSystem
  - `PlayerDataSystem.cs` — singleton DontDestroyOnLoad, fuente de verdad en memoria
  - GetPlayerData() · UpdatePlayerData() · MarkDirty() · ClearDirty() · HasPendingChanges
  - Carga desde Assets/Data/player_data.json en Initialize() (temporal hasta S07)
  - OnSessionEnd() advierte si hay cambios sin persistir
  - Validado en Unity: 4/4 checks PASS (jugador DarkLord42, nivel 15)

## Scenes implementadas
_(vacío)_

## Notas técnicas
- Modelos en namespace `ReinoOscuridad.Data`, solo `[Serializable]`, sin MonoBehaviours
- `PlayerBonusStats` existe aunque sea null en el JSON de muestra
- `RangoPM.max` es `int?` para admitir null en el último rango de Presencia Maldita
- `GameManager` tiene `[DefaultExecutionOrder(-100)]` para garantizar que inicializa antes que cualquier sistema
- `GameManager.NotifySessionStart(lastLoginTimestamp)` lo llama PlayerDataSystem tras cargar Firestore
- `TryFireDailyReset` compara contra medianoche UTC — no accede a Firestore

## ⚠️ DEUDA TÉCNICA — Acción requerida antes de producción

### JsonUtility no deserializa Dictionary<string, T>
**Afecta a:** `PlayerData.awakenInventory.items` y `GlobalVariables` (cualquier campo `Dictionary<>`)

`JsonUtility` de Unity no soporta `Dictionary<string, T>` de forma nativa. En S04 los datos
cargaron correctamente porque los campos básicos (nivel, nombre, héroes como List) sí se
deserializan, pero los diccionarios quedan vacíos/null en runtime.

**Impacto concreto:**
- `awakenInventory.items` → null (materiales de awaken inaccesibles)
- `GlobalVariables.economia_caosifera.fuentes_recurrentes` → null
- `GlobalVariables.presencia_maldita.peso_esbirros.peso_base_por_rareza` → null
- Cualquier otro `Dictionary<string, ?>` en los modelos

**Solución recomendada:** Reemplazar `JsonUtility.FromJson` por **Newtonsoft.Json**
(paquete `com.unity.nuget.newtonsoft-json`, disponible en Unity Package Manager).
Es el estándar de facto en proyectos Unity con datos complejos y soporta Dictionary,
tipos anulables (`int?`) y polimorfismo sin configuración adicional.

**Cuándo hacerlo:** Antes de S07 (integración Firestore), cuando PlayerDataSystem
cargue datos reales del servidor. Si se hace después, los datos de awaken inventory
y economía serán incorrectos en producción.

**Tarea para el director de proyecto:** Aprobar la adición de Newtonsoft.Json al
proyecto antes de la sesión S07. Instalación: Package Manager → Add by name →
`com.unity.nuget.newtonsoft-json`.

---
- Siguiente paso: BootScene — CatalogLoader + flujo de carga inicial + pantalla de loading
