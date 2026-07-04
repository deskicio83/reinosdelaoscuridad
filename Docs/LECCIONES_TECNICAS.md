# LECCIONES TÉCNICAS — Reino de la Oscuridad
_Patrones aprendidos durante el desarrollo._
_Leer al inicio de cada sesión. Actualizar cuando_
_se descubra un nuevo patrón o se corrija un error._

---

## Unity 6 — APIs deprecadas

- `FindFirstObjectByType<T>()` → usar
  `FindAnyObjectByType<T>()`
- `FindObjectsByType<T>(inactive, sortMode)` →
  usar `FindObjectsByType<T>(inactive)` sin sortMode
- `enableWordWrapping` en TMP → usar
  `textWrappingMode = TextWrappingModes.Normal`
- `Image.Type.Filled` sin sprite asignado no renderiza
  el fill. Usar anchorMax.x para barras de progreso
  creadas por código sin sprite.

---

## Assembly Definitions (asmdef)

- Los tests NUnit requieren sus propios .asmdef en
  Assets/Tests/EditMode/ y Assets/Tests/PlayMode/
- El código de producción necesita su propio .asmdef
  en Assets/Scripts/ para ser referenciado por los tests
- Los packages de Unity (TextMeshPro, InputSystem)
  NO se incluyen automáticamente en asmdefs custom —
  añadir explícitamente:
    "Unity.TextMeshPro"
    "Unity.InputSystem"
    "Unity.Addressables"
- PlayMode tests NO deben tener guards #if UNITY_EDITOR
- Assembly-CSharp NO es referenciable por nombre
  desde asmdefs custom — crear asmdef propio
- **CORRECCIÓN (2026-07-04, verificado por CLI)**: `"includePlatforms": ["Editor"]` en
  `PlayModeTests.asmdef` (puesto en S27_build para no romper el build Android) también excluye el
  assembly del dominio de Play Mode — con esa línea, `-runTests -testPlatform PlayMode` descubre 0
  tests ("No tests were executed"). Los PlayMode tests estuvieron rotos en silencio desde S27_build
  hasta esta sesión, sin que nadie los volviera a correr de punta a punta.
  **Fix correcto**: `"includePlatforms": []` + `"defineConstraints": ["UNITY_INCLUDE_TESTS"]` — ese
  símbolo solo existe durante ejecuciones de test (Editor/Play Mode), nunca en un build de Player.
- El error "SBP ErrorError" de Addressables en builds
  CLI suele ser consecuencia de errores de compilación
  previos (Script Build Pipeline falla → Addressables
  lo reporta como "SBP ErrorError"). Resolver primero
  los errores de compilación.

---

## Layout y UI

- `child.SetParent(null)` ANTES de `Destroy()` cuando
  el hijo está bajo un LayoutGroup — Destroy() es
  diferido y el LG sigue viendo el hijo hasta fin de frame
- `HorizontalLayoutGroup` + `LayoutElement`:
  siempre setear `childControlWidth=true` y
  `childControlHeight=true` explícitamente
- `LayoutElement.preferredWidth/Height` para tamaños
  bajo LayoutGroup — NUNCA sizeDelta bajo LayoutGroup
- `rect.width/height` devuelve 0 antes del primer frame
  de layout — siempre 2x `yield return null` antes de
  leerlo, con fallback hardcoded
- `RectMask2D` preferible a `Mask+Image` cuando el
  fondo es transparente — Mask requiere que Image
  escriba al stencil buffer (no ocurre con alpha=0)
- En Unity, ScrollRect y Viewport (con Mask/RectMask2D)
  deben ser GameObjects separados en jerarquía
  padre→hijo, nunca el mismo GO
- Image transparente (Color.clear) con
  `raycastTarget=true` hace que zonas vacías de un
  ScrollRect respondan a drags sin renderizar nada
- Overlays que necesitan extenderse más allá del borde
  de su zona deben ser hijos del Canvas, no del panel
- `anchoredPosition.x = rect.width` para slide
  off-screen — funciona con anchors fijos no-stretch
- `SetActive(false)` instantáneo es preferible a
  animaciones antes de una transición de Scene —
  las coroutines compiten con FadeOut de UIManager

---

## Firebase y datos

- `Newtonsoft.Json` + Firestore SDK son incompatibles
  sin conversión — `Dictionary<string,object>` con
  valores `JArray/JObject` no puede pasarse a
  `SetAsync()` directamente. Convertir a tipos nativos
  C# antes de escribir a Firestore
- JSON heterogéneo (campo que puede ser string o array)
  se maneja con:
  `JsonSerializerSettings { Error = (_, args) =>
    args.ErrorContext.Handled = true }`
- Campos `int` en clases deserializadas con Newtonsoft
  NO aceptan `null` del JSON — usar 0 o omitir el campo
- `PlayerPrefs` como caché local en modo dev para
  persistir entre sesiones de Play Mode sin Firestore

---

## Navegación entre Scenes

- `NavigateTo(callerScene)` desde la Scene destino
  ensucia el historial — la Scene que inicia la
  navegación usa NavigateTo, la destino usa
  NavigateBack para volver
- El orden correcto de transición es:
  FadeOut → CloseOverlays → LoadScene → FadeIn
  (nunca al revés — el jugador verá los paneles)
- Al navegar de vuelta, verificar que todos los
  overlays están cerrados antes de cargar la Scene
- `NavigateBack()` en lugar de `NavigateTo(callerScene)`
  evita añadir entradas duplicadas al historial

---

## Coroutines y async

- `Destroy()` diferido rompe LayoutGroups en el mismo
  frame — patrón correcto:
  `child.SetParent(null); Destroy(child.gameObject)`
- `go.SetActive(true)` tras `SetParent()` en jerarquías
  con ancestros inactivos — el GO hereda el estado
  inactivo y `StartCoroutine` falla
- `StopAllCoroutines()` en `OnHuirPressed()` o métodos
  de limpieza debe llamarse DESPUÉS de setear la fase
  a `CombatEnd` para evitar que otras coroutines
  reactiven el loop
- `WaitUntil(() => _pendingTarget != null)` es el
  patrón correcto para pausar una coroutine esperando
  input sin polling activo

---

## Sistemas singleton (DontDestroyOnLoad)

- `CombatSystem` debe ser DDOL igual que GameManager
  y PlayerDataSystem — si está en una Scene concreta
  se destruye al cambiar de Scene
- `FindAnyObjectByType<T>()` NO distingue entre Scenes
  — devuelve objetos de cualquier Scene incluyendo DDOL.
  Para objetos de la Scene activa filtrar con:
  `c.gameObject.scene == gameObject.scene`
- Inicializar con el patrón:
  `if (Instance != null && Instance != this)
    { Destroy(gameObject); return; }
  Instance = this;
  DontDestroyOnLoad(gameObject);`

---

## Combate — patrones específicos

- `Image.Type.Filled` requiere sprite explícito —
  sin sprite, `fillAmount` no produce efecto visual.
  Alternativa robusta: controlar `anchorMax.x`
- `using System` + `using UnityEngine` en conflicto —
  `Object` queda ambiguo. Usar
  `using System.Collections.Generic` en lugar de
  `using System` en scripts Unity
- Los tests de combate en Play Mode necesitan
  `CombatSceneData.ForceAutoMode = true` para que
  el ATBLoop no quede bloqueado esperando input
- `slots posicionales` (string[4] con índice fijo)
  permiten vaciar un hueco sin reordenar el resto.
  `List<string>` con `RemoveAt` reordena — usar array
  para slots con posición visual fija
- Stats porcentuales del gear deben aplicarse sobre
  la base heroica capturada ANTES del loop de gear,
  no sobre el total acumulado

---

## Botones y touch en dispositivos reales

- En dispositivos táctiles Android, `Button.interactable = false` no siempre
  bloquea el touch si hay un overlay encima que no consume completamente el evento.
  La solución robusta es también desactivar el componente Button:
  `btn.interactable = false; btn.enabled = false;`
  Para re-habilitar: `btn.enabled = true; btn.interactable = true;`
- El overlay (LockOverlay) debe tener `raycastTarget = true` en TODOS sus
  Image hijos, incluidos los sub-hijos. Usar
  `GetComponentsInChildren<Image>(true)` con `includeInactive=true`
  para garantizarlo aunque el overlay esté recién activado.
- `GameObject.Find("NombreGO")` solo encuentra GOs activos. Para encontrar
  GOs inactivos usar `FindObjectsByType<T>(FindObjectsInactive.Include)`.
  ContentEdificios y ZonaEdificios están activos, por lo que `Find` funciona.

---

## Fuentes (TMP)

- `LiberationSans SDF` (fuente por defecto de Unity)
  NO soporta caracteres Unicode fuera de ASCII básico
  + Latin-1. No usar: ★ ✕ ⚔ ⚡ 💬 ✉ ⚙ 🧭
- Para indicadores visuales (estrellas, estados) usar
  `Image[]` con colores en lugar de caracteres TMP
- Para botones e iconos usar texto ASCII puro

---

## Editor Setup Scripts (Tools → Reino Oscuridad)

- Los 12 scripts de `Assets/Editor/Setup/*.cs` **destruyen TODOS los GameObjects de la Scene
  activa y la reconstruyen desde cero** cada vez que se ejecutan (`FindObjectsByType<GameObject>
  → DestroyImmediate` seguido de `Build()`). Si se hizo un ajuste manual en el Editor (mover un
  botón, cambiar un color a mano) y se vuelve a correr el Tool correspondiente, ese ajuste se
  pierde sin aviso más allá del diálogo de guardar cambios. No correrlos como rutina — solo cuando
  la intención sea reconstruir la Scene desde el código.
- `Child/Anch/Img/Txt/Hex/SetAnchors` estaban duplicados (con pequeñas variantes de firma y, en el
  caso de `SetAnchors`, con comportamiento inconsistente — 3 de 4 copias hacían `return` silencioso
  si faltaba el `RectTransform` en vez de crearlo) en los 12 Editor Setup Scripts. Consolidados
  todos en `Assets/Editor/Setup/EditorUIBuilder.cs` (`using static EditorUIBuilder;`), con overloads
  `Child(Transform, ...)`/`Child(GameObject, ...)` y `Anch(4 floats)`/`SetAnchors(Vector2, Vector2)`
  para no romper ningún call site existente. Se unificó también la convención de `Hex()` a que
  siempre requiera el prefijo `#` (3 archivos lo omitían).

## Auditoría de catálogos JSON

- Antes de dar por completo un enum que representa datos de un catálogo (ej. `TipoEfecto`), grepear
  los valores reales usados en el JSON (`grep -o '"type": "[a-z_]*"' hero_catalog.json | sort -u`).
  `TipoEfecto` tenía 17 valores pero el catálogo real usa ~45 strings de efecto distintos — el enum
  llevaba desde el diseño inicial sin actualizarse al ritmo del contenido.
- Cuando un controller de UI reimplementa su propio loop en vez de llamar al método "grande" de un
  System (ej. `CombatSceneController` vs `CombatSystem.ProcessCombat()`), verificar con grep que el
  método grande realmente se sigue usando en algún sitio de producción — si solo aparece en tests,
  es candidato a estar completamente desconectado sin que ningún error de compilación lo delate.

## Tests — patrones NUnit en Unity

- Tests de lógica aleatoria con probabilidad deben
  usar loops de N intentos + `Assert.Inconclusive`
  si todos los intentos producen el mismo resultado
  improbable (ej: dodge 5% mínimo)
- `[UnitySetUp]` carga la Scene antes de cada test —
  dar suficiente tiempo con `WaitForSeconds` para que
  los sistemas se inicialicen
- `Assert.Inconclusive` para tests que dependen de
  timing de combate — mejor que FAIL cuando el
  resultado es correcto pero no observable en el
  tiempo del test
- PlayMode tests deben activar `ForceAutoMode` cuando
  prueban flujos de combate para no bloquear en input
