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

## Scenes implementadas
_(vacío)_

## Notas técnicas
- Modelos en namespace `ReinoOscuridad.Data`, solo `[Serializable]`, sin MonoBehaviours
- `PlayerBonusStats` existe aunque sea null en el JSON de muestra (puede aparecer en otros registros)
- `RangoPM.max` es `int?` para admitir null en el último rango de Presencia Maldita
- Siguiente paso: `CatalogLoader` en BootScene que deserializa los JSONs de `Assets/Data/` una sola vez
