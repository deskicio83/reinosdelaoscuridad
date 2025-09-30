/*
============================================================
LoadingOverlayController.cs — Overlay de carga genérico
------------------------------------------------------------
PROPÓSITO
- Pantalla de carga con barra (Scrollbar handle) y fondo Addressable.
- Funde a NEGRO al terminar (sin degradado intermedio del fondo).

USO
- Mostrar antes de SceneLoader.Load y ocultar al final.

MÉTODOS (COMPLETA AQUÍ)
- Set(value01, tip): progreso absoluto.
- Step(delta): progreso incremental.
- ShowRandomBackground(labelOrKeys): fondo aleatorio Addressables.
============================================================
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;

[DefaultExecutionOrder(-500)]
public class LoadingOverlayController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;     // CanvasGroup del overlay
    [SerializeField] private Scrollbar progressBar;       // Scrollbar: el Handle "rellena" con size
    [SerializeField] private Image progressFill;          // (Opcional legado) Image con FillMethod
    [SerializeField] private TMP_Text percentTxt;         // Texto de porcentaje
    [SerializeField] private TMP_Text tipTxt;             // Mensajes graciosos
    [SerializeField] private Image blackFade;             // Imagen negra para fundidos (alpha 1 = negro)

    [Header("Background")]
    [SerializeField] private Image bgImage;               // Image para el fondo del loading

    [Tooltip("Si está activo, en vez de usar la lista de 'backgroundKeys' usará una Label de Addressables.")]
    [SerializeField] private bool autoFromLabel = false;

    [Tooltip("Label de Addressables que agrupa los fondos de carga (opcional).")]
    [SerializeField] private string addressablesLabel = "loading_backgrounds";

    [Tooltip("Oscurecer ligeramente el fondo (0=sin oscurecer, 0.5=medio).")]
    [Range(0f, 1f)]
    [SerializeField] private float bgDarken = 0.0f;

    [Header("Timings")]
    [SerializeField] private float fadeInTime = 0.20f;
    [SerializeField] private float tipsEverySeconds = 1.75f;
    [SerializeField] private float blackOutTime = 0.35f; // fundido a NEGRO al salir

    [Header("Tips (editable)")]
    [TextArea(2, 6)]
    [SerializeField]
    private List<string> defaultTips = new()
    {
        "Los esbirros están terminando de pintar…",
        "Añadiendo tornillos a las sillas…",
        "Afilando tridentes con cariño…",
        "Limpiando mocos pegados…",
        "Poniendo azúcar al caldero…",
        "Buscando calcetines desapareados…",
    };

    [Header("Background Keys (en código)")]
    [Tooltip("Claves Addressable para los fondos. Edita/añade las tuyas. Ruta recomendada: Assets/Addressables/Art/Common/Fondo/*.png")]
    [SerializeField]
    private List<string> backgroundKeys = new()
    {
        "Assets/Addressables/Art/Common/Fondo/Fondo_01.png",
        "Assets/Addressables/Art/Common/Fondo/Fondo_02.png",
        "Assets/Addressables/Art/Common/Fondo/Fondo_03.png",
        "Assets/Addressables/Art/Common/Fondo/Fondo_04.png",
        "Assets/Addressables/Art/Common/Fondo/Fondo_05.png",
        "Assets/Addressables/Art/Common/Fondo/Fondo_06.png",
        "Assets/Addressables/Art/Common/Fondo/Fondo_07.png",
    };

    // Añade este helper si no lo tienes ya
    private void SetBgColor()
    {
        if (!bgImage) return;
        float shade = Mathf.Clamp01(1f - bgDarken);
        bgImage.color = new Color(shade, shade, shade, 1f);
    }

    // --- API estática para reportar progreso desde cualquier script ---
    public static LoadingOverlayController Current { get; private set; }

    public static void Step(float delta, string tip = null)
    {
        if (!Current) return;
        Current.AddProgress(delta, tip);
    }

    public static void Set(float value01, string tip = null)
    {
        if (!Current) return;
        Current.SetProgress(value01, tip);
    }

    private float _progress; // 0..1
    private Coroutine _tipRoutine;
    private readonly System.Random _rng = new();

    // Cache label -> keys
    private List<string> _labelKeys = null;
    private string _lastBgKey = null;

    // Handles del fondo (para liberar)
    private AsyncOperationHandle<Sprite>? _bgSpriteHandle = null;
    private AsyncOperationHandle<Texture2D>? _bgTexHandle = null;

    void Awake()
    {
        Current = this;

        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup)
        {
            canvasGroup.alpha = 1f;     // empezamos visibles (negro)
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        // El fondo NO se muestra hasta tener sprite
        if (bgImage)
        {
            bgImage.sprite = null;
            bgImage.enabled = false;
            bgImage.preserveAspect = true;
            SetBgColor();
        }

        // Pantalla negra de inicio
        if (blackFade)
        {
            var c = blackFade.color;
            c.a = 1f;
            blackFade.color = c;
        }

        // Scrollbar como barra de progreso
        if (progressBar)
        {
            progressBar.interactable = false;
            progressBar.numberOfSteps = 0;
            progressBar.direction = Scrollbar.Direction.LeftToRight;
            progressBar.value = 0f;   // anclado a la izquierda
            progressBar.size = 0f;    // tamaño del handle = progreso (0..1)
        }

        if (progressFill) progressFill.fillAmount = 0f;
        if (percentTxt) percentTxt.text = "0%";

        gameObject.SetActive(true);
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
        ReleaseBackground();
    }

    // ------------------------------------------------------------
    // Ciclo de vida visual
    // ------------------------------------------------------------
    public IEnumerator Show()
    {
        // Fondo aleatorio (sin bloquear el fade-in)
        yield return StartCoroutine(EnsureRandomBackgroundLoaded());

        if (_tipRoutine == null) _tipRoutine = StartCoroutine(TipsLooper());

        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.unscaledDeltaTime;
            if (canvasGroup) canvasGroup.alpha = Mathf.Clamp01(t / fadeInTime);
            yield return null;
        }
        if (canvasGroup) canvasGroup.alpha = 1f;
    }

    public IEnumerator Hide()
    {
        // Asegura progreso al 100%
        //SetProgress(1f, "¡Listo!");

        // 1) FUNDIDO A NEGRO (sin enseñar la escena por debajo)
        if (blackFade)
        {
            float t = 0f;
            Color c = blackFade.color;
            float startA = c.a;
            float endA = 1f;

            while (t < blackOutTime)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / blackOutTime);
                c.a = Mathf.Lerp(startA, endA, k);
                blackFade.color = c;
                yield return null;
            }
            c.a = 1f;
            blackFade.color = c;
        }

        // 2) OCULTA de golpe el overlay
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        if (_tipRoutine != null)
        {
            StopCoroutine(_tipRoutine);
            _tipRoutine = null;
        }

        // Desactiva el overlay (ya se verá la escena)
        gameObject.SetActive(false);

        // Libera el fondo (opcional) — si prefieres mantener en memoria, comenta esta línea
        ReleaseBackground();

        // Prepara negro a 0 para próxima vez (no hace flash porque el overlay está inactivo)
        if (blackFade)
        {
            var c = blackFade.color;
            c.a = 0f;
            blackFade.color = c;
        }
    }

    // ------------------------------------------------------------
    // Progreso
    // ------------------------------------------------------------
    public void AddProgress(float delta, string tip = null)
    {
        SetProgress(Mathf.Clamp01(_progress + Mathf.Abs(delta)), tip);
    }

    public void SetProgress(float value01, string tip = null)
    {
        _progress = Mathf.Clamp01(value01);

        if (progressBar)
        {
            progressBar.value = 0f;
            progressBar.size = _progress;
        }

        if (progressFill) progressFill.fillAmount = _progress;
        if (percentTxt) percentTxt.text = $"{Mathf.RoundToInt(_progress * 100f)}%";
        if (!string.IsNullOrEmpty(tip) && tipTxt) tipTxt.text = tip;
    }

    private IEnumerator TipsLooper()
    {
        if (defaultTips == null || defaultTips.Count == 0) yield break;
        while (true)
        {
            if (tipTxt) tipTxt.text = defaultTips[_rng.Next(defaultTips.Count)];
            yield return new WaitForSecondsRealtime(tipsEverySeconds);
        }
    }

    // ------------------------------------------------------------
    // Fondo aleatorio (Addressables)
    // ------------------------------------------------------------
    private IEnumerator EnsureRandomBackgroundLoaded()
    {
        if (!bgImage) yield break;

        // Si quieres recoger claves de una Label (Addressables), las cacheamos una sola vez
        if (autoFromLabel && _labelKeys == null)
        {
            var locHandle = Addressables.LoadResourceLocationsAsync(addressablesLabel);
            yield return locHandle;

            if (locHandle.Status == AsyncOperationStatus.Succeeded && locHandle.Result != null && locHandle.Result.Count > 0)
            {
                _labelKeys = locHandle.Result.Select(loc => loc.PrimaryKey).ToList();
            }
            else
            {
                Debug.LogWarning($"[LoadingOverlay] Label '{addressablesLabel}' no devolvió entradas. Usando 'backgroundKeys' locales.");
                _labelKeys = new List<string>();
            }

            Addressables.Release(locHandle);
        }

        // Conjunto de candidatos: label si hay; si no, lista en código
        var pool = (autoFromLabel && _labelKeys != null && _labelKeys.Count > 0)
            ? _labelKeys
            : backgroundKeys;

        if (pool == null || pool.Count == 0)
        {
            Debug.LogWarning("[LoadingOverlay] No hay fondos configurados. Deja bgImage sin sprite.");
            bgImage.sprite = null;
            yield break;
        }

        // Evita repetir el mismo fondo
        string pick = null;
        if (pool.Count == 1)
        {
            pick = pool[0];
        }
        else
        {
            for (int tries = 0; tries < 8; tries++)
            {
                var k = pool[_rng.Next(pool.Count)];
                if (k != _lastBgKey) { pick = k; break; }
            }
            if (pick == null) pick = pool[_rng.Next(pool.Count)];
        }

        yield return StartCoroutine(LoadBackgroundKey(pick));
        _lastBgKey = pick;
    }

    private IEnumerator LoadBackgroundKey(string key)
    {
        ReleaseBackground(); // limpia anterior

        if (string.IsNullOrEmpty(key))
            yield break;

        // 1) Intenta como SPRITE
        var hSprite = Addressables.LoadAssetAsync<Sprite>(key);
        yield return hSprite;

        if (hSprite.Status == AsyncOperationStatus.Succeeded)
        {
            _bgSpriteHandle = hSprite;
            ApplyBgSprite(hSprite.Result);
            yield break;
        }
        else
        {
            Addressables.Release(hSprite);
        }

        // 2) Intenta como TEXTURE2D
        var hTex = Addressables.LoadAssetAsync<Texture2D>(key);
        yield return hTex;

        if (hTex.Status == AsyncOperationStatus.Succeeded && hTex.Result != null)
        {
            _bgTexHandle = hTex;
            var tex = hTex.Result;

            // Crea un sprite temporal a partir de la textura
            var rect = new Rect(0, 0, tex.width, tex.height);
            var pivot = new Vector2(0.5f, 0.5f);
            var sprite = Sprite.Create(tex, rect, pivot, 100f);
            ApplyBgSprite(sprite, createdFromTexture: true);
        }
        else
        {
            Debug.LogWarning($"[LoadingOverlay] No se pudo cargar fondo: '{key}' como Sprite ni Texture2D.");
            Addressables.Release(hTex);
        }
    }

    private void ApplyBgSprite(Sprite s, bool createdFromTexture = false)
    {
        if (!bgImage) return;
        bgImage.sprite = s;
        bgImage.enabled = true;     // ahora sí mostramos el fondo
        bgImage.preserveAspect = true;
        SetBgColor();
    }

    private void ShowPlainBlack()
    {
        if (bgImage) bgImage.enabled = false; // nos aseguramos de ocultar el Image blanco
        if (blackFade)
        {
            var c = blackFade.color;
            c.a = 1f;
            blackFade.color = c;
        }
    }

    private void ReleaseBackground()
    {
        if (bgImage) bgImage.sprite = null;

        if (_bgSpriteHandle.HasValue)
        {
            if (_bgSpriteHandle.Value.IsValid())
                Addressables.Release(_bgSpriteHandle.Value);
            _bgSpriteHandle = null;
        }
        if (_bgTexHandle.HasValue)
        {
            if (_bgTexHandle.Value.IsValid())
                Addressables.Release(_bgTexHandle.Value);
            _bgTexHandle = null;
        }
    }
       

}
