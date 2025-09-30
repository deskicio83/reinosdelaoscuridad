/*
============================================================
HeroDespertarPanelController.cs — UI de despertar
------------------------------------------------------------
PROPÓSITO
- Mostrar costes, cambios de stats, nuevas pasivas; confirmar acción.

MÉTODOS (COMPLETA AQUÍ)
- Show(HeroProgress, HeroCatalogEntry)/Hide().
- OnConfirmAwaken(): aplica cambios y refresca UI.
============================================================
*/


using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Reflection;
using System.Linq;
using DG.Tweening;
using System.IO; // para leer el JSON recién guardado en el bloque de verificación
using System.Collections;
// Paquetes Coffee
using Coffee.UIExtensions;   // UIParticle
using Coffee.UIEffects;      // UIShiny



public class HeroDespertarPanelController : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject noDespiertoPanel;  // contiene Slots + BtnDespertar
    [SerializeField] private GameObject despiertoPanel;    // “Despertar Completado”

    [Header("Textos (Top/Mid)")]
    [SerializeField] private TMP_Text skillUpTxt;          // futuro: subir stat / mejorar skill / líder...
    [SerializeField] private TMP_Text basicStatsUpTxt;     // “Aumenta las estadísticas básicas”
    [SerializeField] private TMP_Text nameAwakenTxt;       // “Se convierte en [AwakenName]”

    [Header("Slots (4) en NoDespierto")]
    [SerializeField] private SlotUI[] slots = new SlotUI[4];

    [Header("Botón")]
    [SerializeField] private Button btnDespertar;          // dentro de NoDespierto

    [Header("Sprites de esencias (opcional)")]
    // CAOS
    [SerializeField] private Sprite caos_extracto, caos_infusion, caos_destilado;
    // ELEMENTOS (luz, oscuridad, fuego, naturaleza, agua)
    [SerializeField] private Sprite element_luz_extracto,        element_luz_infusion,        element_luz_destilado;
    [SerializeField] private Sprite element_oscuridad_extracto,  element_oscuridad_infusion,  element_oscuridad_destilado;
    [SerializeField] private Sprite element_fuego_extracto,      element_fuego_infusion,      element_fuego_destilado;
    [SerializeField] private Sprite element_naturaleza_extracto, element_naturaleza_infusion, element_naturaleza_destilado;
    [SerializeField] private Sprite element_agua_extracto,       element_agua_infusion,       element_agua_destilado;
    [Header("HeroAwkPanel (Comparativa retratos)")]
    [SerializeField] private Image heroBeforeImg; // <- asigna en el inspector
    [SerializeField] private Image heroAfterImg;  // <- asigna en el inspector

    [Header("Diálogo de confirmación")]
    [SerializeField] private ConfirmAwakenDialog confirmDialog;  // oculto por defecto; se abre al pulsar Despertar
    [Header("FX Despertar (opcional)")]
    [SerializeField] private RectTransform panelRoot;           // Raíz para shake (asigna el root del panel)
    [SerializeField] private AudioSource sfxAwaken;             // SFX (opcional)
    [SerializeField] private ParticleSystem vfxBurst;           // Partículas (opcional)

    [Header("FX Coffee UI (opcionales)")]
    // No referenciamos tipos del paquete en tiempo de compilación.
    // Si existen, los obtendremos por reflexión.
    [SerializeField] private Component shinyOnName;           // (UIShiny si está disponible)
    [SerializeField] private Component shinyOnAfterPortrait;  // (UIShiny si está disponible)
    [SerializeField] private Component uiBurst;               // (UIParticle si está disponible)
    
    // --- Addressables (rutas exactas que nos diste) ---
    private const string ADDR_RAYS = "Assets/Addressables/Art/HeroScene/FX/RayBurst_Rays.png";
    private const string ADDR_HALO     = "Assets/Addressables/Art/HeroScene/FX/RayBurst_HaloSoft.png";
    private const string ADDR_RING     = "Assets/Addressables/Art/HeroScene/FX/RayBurst_Ring.png";
    private const string ADDR_SPARKLE  = "Assets/Addressables/Art/HeroScene/FX/Sparkle_Star.png";
    private const string ADDR_WHITE    = "Assets/Addressables/Art/HeroScene/FX/White_1x1.png";

    // Sprites cacheados
    private Sprite spRays, spHalo, spRing, spSparkle, spWhite;

    // Jerarquía creada por código (si no la tienes en escena)
    private CanvasGroup fxOverlay;     // GO: FX_Overlay
    private Image       fxWhiteFlash;  // GO: FX_WhiteFlash (hijo de FX_Overlay)
    private Image       fxRays;        // GO: FX_Rays      (hijo de FX_Overlay)
    private Image       fxHalo;        // GO: FX_Halo      (hijo de FX_Overlay)
    private Image       fxRing;        // GO: FX_Ring      (hijo de FX_Overlay)



    [System.Serializable]
    public class SlotUI
    {
        public Image icon;
        public TMP_Text txt;
        [HideInInspector] public string key;   // "caos_<tier>" o "element_<elem>_<tier>"
        [HideInInspector] public int needed;
    }

    // Estado
    private PlayerData player;
    private HeroProgress progress;
    private HeroCatalogEntry catalog;
    private System.Action _onAwakened; // callback hacia la escena para refrescar todo
    // ===============================
    //  Descripción de mejoras Awaken
    // ===============================
    private static readonly Dictionary<string, string> _effectGlossaryES = new Dictionary<string, string>
    {
        // Mapa de ejemplo/fallback; si en el futuro lo cargas del JSON, sustituye aquí.
        // Añade entradas según tu glosario:
        { "atk_up",       "Aumento de ATQ" },
        { "atk_down",     "Reducción de ATQ" },
        { "def_up",       "Aumento de DEF" },
        { "def_down",     "Reducción de DEF" },
        { "spd_up",       "Aumento de VEL" },
        { "spd_down",     "Reducción de VEL" },
        { "res_up",       "Aumento de RES" },
        { "res_down",     "Reducción de RES" },
        { "acc_up",       "Aumento de ACC" },
        { "acc_down",     "Reducción de ACC" },
        { "crit_up",      "Aumento de CRI" },
        { "crit_down",    "Reducción de CRI" },
    };

    private string TranslateEffectEs(string code)
    {
        if (string.IsNullOrEmpty(code)) return "";
        if (_effectGlossaryES.TryGetValue(code, out var es)) return es;
        // Fallback: humaniza mínimamente el identificador
        return code.Replace('_', ' ');
    }

    private static string CleanStatLabel(string stat)
    {
        if (string.IsNullOrEmpty(stat)) return "";
        // Normaliza algunos alias comunes a un formato amigable
        var s = stat.Trim();
        s = s.Replace("ATK", "ATQ"); // si usas ATQ en español
        s = s.ToUpperInvariant();
        return s;
    }
    private IEnumerator EnsureFxReady()
    {
        // 1) Construye jerarquía si no existe.
        if (fxOverlay == null) BuildFxOverlayHierarchy();

        // 2) Carga sprites si faltan.
        if (spWhite == null || spRays == null || spHalo == null || spRing == null || spSparkle == null)
            yield return StartCoroutine(LoadFxSpritesAddressables());

        // 3) Asigna sprites.
        AssignFxSpritesIfNeeded();
    }
    private IEnumerator VerifySaveWrittenToDisk(string heroIdForLog = null)
    {
        var gdm = GameDataManager.Instance;
        if (gdm == null) yield break;

        // NOMBRES DISTINTOS para evitar CS0136
        string verifyPathLocal = gdm.GetPlayerDataPath();

        // Espera un frame para dar tiempo al FS.
        yield return null;

        bool fileExists = File.Exists(verifyPathLocal);
        string json = fileExists ? File.ReadAllText(verifyPathLocal) : "(no existe)";

        bool awakenTrueInJson = json.Contains("\"awaken\": true");
        Debug.Log($"[Awaken][Verify] path='{verifyPathLocal}' exists={fileExists} awakenTrueInJson={awakenTrueInJson} hero='{heroIdForLog}'");
    }



    private void BuildFxOverlayHierarchy()
    {
        // Busca si ya existe en la escena (por si lo tienes en el prefab)
        var existing = transform.Find("FX_Overlay");
        if (existing != null)
        {
            fxOverlay    = existing.GetComponent<CanvasGroup>();
            if (fxOverlay == null) fxOverlay = existing.gameObject.AddComponent<CanvasGroup>();
            fxWhiteFlash = existing.Find("FX_WhiteFlash")?.GetComponent<Image>();
            fxRays       = existing.Find("FX_Rays")?.GetComponent<Image>();
            fxHalo       = existing.Find("FX_Halo")?.GetComponent<Image>();
            fxRing       = existing.Find("FX_Ring")?.GetComponent<Image>();
            return;
        }

        // Crear GO raíz
        var root = new GameObject("FX_Overlay", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(this.transform, false);
        var rt = (RectTransform)root.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        fxOverlay = root.GetComponent<CanvasGroup>();
        fxOverlay.alpha = 0f; fxOverlay.interactable = false; fxOverlay.blocksRaycasts = false;

        // Crea helper para hijo "Image fullscreen"
        Image CreateChild(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(root.transform, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = Color.white;
            img.type = Image.Type.Simple;
            return img;
        }

        fxWhiteFlash = CreateChild("FX_WhiteFlash");
        fxRays       = CreateChild("FX_Rays");
        fxHalo       = CreateChild("FX_Halo");
        fxRing       = CreateChild("FX_Ring");

        // Por defecto, oculto todo
        fxWhiteFlash.enabled = fxRays.enabled = fxHalo.enabled = fxRing.enabled = false;
    }

    private IEnumerator LoadFxSpritesAddressables()
    {
        // helper local
        IEnumerator LoadOne(string addr, System.Action<Sprite> setter)
        {
            var handle = Addressables.LoadAssetAsync<Sprite>(addr);
            yield return handle;
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                setter?.Invoke(handle.Result);
            }
            else
            {
                Debug.LogWarning($"[AwakenFX] No se pudo cargar Addressable: {addr}");
            }
        }

        yield return StartCoroutine(LoadOne(ADDR_WHITE,   s => spWhite   = s));
        yield return StartCoroutine(LoadOne(ADDR_RAYS,    s => spRays    = s));
        yield return StartCoroutine(LoadOne(ADDR_HALO,    s => spHalo    = s));
        yield return StartCoroutine(LoadOne(ADDR_RING,    s => spRing    = s));
        yield return StartCoroutine(LoadOne(ADDR_SPARKLE, s => spSparkle = s));
    }

    private void AssignFxSpritesIfNeeded()
    {
        if (fxWhiteFlash != null && spWhite != null) { fxWhiteFlash.sprite = spWhite; fxWhiteFlash.enabled = true; fxWhiteFlash.type = Image.Type.Simple; }
        if (fxRays       != null && spRays  != null) { fxRays.sprite       = spRays;  fxRays.enabled       = true; fxRays.type       = Image.Type.Sliced; }
        if (fxHalo       != null && spHalo  != null) { fxHalo.sprite       = spHalo;  fxHalo.enabled       = true; fxHalo.type       = Image.Type.Sliced; }
        if (fxRing       != null && spRing  != null) { fxRing.sprite       = spRing;  fxRing.enabled       = true; fxRing.type       = Image.Type.Sliced; }

        // Estados base
        if (fxWhiteFlash) { var c = fxWhiteFlash.color; c.a = 0f; fxWhiteFlash.color = c; fxWhiteFlash.rectTransform.localScale = Vector3.one; }
        if (fxRays)       { var c = fxRays.color;       c.a = 0f; fxRays.color       = c; fxRays.rectTransform.localScale = Vector3.one; fxRays.rectTransform.localRotation = Quaternion.identity; }
        if (fxHalo)       { var c = fxHalo.color;       c.a = 0f; fxHalo.color       = c; fxHalo.rectTransform.localScale = Vector3.one * 1.05f; }
        if (fxRing)       { var c = fxRing.color;       c.a = 0f; fxRing.color       = c; fxRing.rectTransform.localScale = Vector3.one * 0.9f; fxRing.rectTransform.localRotation = Quaternion.identity; }
    }


    private System.Collections.IEnumerator PlayAwakenSequence(System.Action onComplete)
    {
        // Asegura jerarquía y sprites
        yield return StartCoroutine(EnsureFxReady());
        AssignFxSpritesIfNeeded();
        if (panelRoot == null) panelRoot = this.transform as RectTransform;

        // Al frente
        fxOverlay.transform.SetAsLastSibling();
        fxOverlay.gameObject.SetActive(true);
        fxOverlay.alpha = 0f;

        // Estados iniciales
        var cFlash = fxWhiteFlash.color; cFlash.a = 0f; fxWhiteFlash.color = cFlash;
        var cRays  = fxRays.color;       cRays.a  = 0f; fxRays.color       = cRays;
        var cHalo  = fxHalo.color;       cHalo.a  = 0f; fxHalo.color       = cHalo;
        var cRing  = fxRing.color;       cRing.a  = 0f; fxRing.color       = cRing;

        fxWhiteFlash.rectTransform.localScale = Vector3.one * 1.0f;
        fxRays.rectTransform.localScale       = Vector3.one * 1.1f;
        fxHalo.rectTransform.localScale       = Vector3.one * 1.05f;
        fxRing.rectTransform.localScale       = Vector3.one * 0.9f;

        // 1) Fade-in overlay
        fxOverlay.DOFade(0.85f, 0.20f).SetEase(Ease.OutSine).SetUpdate(true);
        yield return new WaitForSecondsRealtime(0.20f);

        // 2) Flash principal potente
        fxWhiteFlash.DOFade(0.95f, 0.18f).SetEase(Ease.OutQuad).SetUpdate(true);
        fxWhiteFlash.rectTransform.DOScale(1.18f, 0.22f).SetEase(Ease.OutQuad).SetUpdate(true);

        // Halo y Ring aparecen
        fxHalo.DOFade(0.55f, 0.30f).SetEase(Ease.OutSine).SetUpdate(true);
        fxRing.DOFade(0.75f, 0.30f).SetEase(Ease.OutSine).SetUpdate(true);
        fxRing.rectTransform.DORotate(new Vector3(0, 0, 90f), 0.60f, RotateMode.FastBeyond360).SetEase(Ease.InOutSine).SetUpdate(true);

        yield return new WaitForSecondsRealtime(0.24f);

        // 3) Retira flash, entra haz radial suave + sparkles
        fxWhiteFlash.DOFade(0f, 0.22f).SetEase(Ease.InSine).SetUpdate(true);
        if (fxRays != null)
        {
            fxRays.DOFade(0.45f, 0.30f).SetEase(Ease.OutSine).SetUpdate(true);
            fxRays.rectTransform.DOScale(1.45f, 0.40f).SetEase(Ease.OutSine).SetUpdate(true);
            fxRays.rectTransform.DORotate(new Vector3(0, 0, -60f), 0.80f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetUpdate(true);
        }

        // Chispas (más “mágico”)
        yield return StartCoroutine(SpawnSparklesBurst(18, 0.65f, 0.38f));

        // 4) Shiny sweeps si UIShiny existe
        AnimateShinySweep(shinyOnName,          1.10f, 0.00f);
        AnimateShinySweep(shinyOnAfterPortrait, 1.25f, 0.08f);

        // 5) Mantén un poco la estela
        yield return new WaitForSecondsRealtime(0.35f);

        // 6) Desvanecer todo
        var seq = DOTween.Sequence().SetUpdate(true);
        seq.Join(fxOverlay.DOFade(0f, 0.22f).SetEase(Ease.InSine));
        if (fxRays) seq.Join(fxRays.DOFade(0f, 0.22f).SetEase(Ease.InSine));
        if (fxHalo) seq.Join(fxHalo.DOFade(0f, 0.22f).SetEase(Ease.InSine));
        if (fxRing) seq.Join(fxRing.DOFade(0f, 0.22f).SetEase(Ease.InSine));
        yield return seq.WaitForCompletion();

        fxOverlay.gameObject.SetActive(false);
        onComplete?.Invoke();
    }

    private void EnsureFxSetup()
    {
        if (panelRoot == null) panelRoot = this.transform as RectTransform;

        // Overlay raíz (oscurecer fondo)
        if (fxOverlay == null)
        {
            var go = new GameObject("FX_Overlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(panelRoot, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            fxOverlay = go.GetComponent<CanvasGroup>();
            fxOverlay.alpha = 0f;
            fxOverlay.interactable = false;
            fxOverlay.blocksRaycasts = false;

            var bg = go.GetComponent<Image>();
            bg.raycastTarget = false;
            bg.color = new Color(0f, 0f, 0f, 0.65f);
        }

        // Colores por elemento
        var baseColor = GetElementColor(catalog != null ? catalog.element : null);

        Image CreateOrGet(string name)
        {
            var t = fxOverlay.transform.Find(name);
            Image img;
            if (t == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(fxOverlay.transform, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                img = go.GetComponent<Image>();
                img.raycastTarget = false;
            }
            else img = t.GetComponent<Image>();
            return img;
        }

        fxWhiteFlash = fxWhiteFlash ? fxWhiteFlash : CreateOrGet("FX_WhiteFlash");
        fxRays       = fxRays       ? fxRays       : CreateOrGet("FX_Rays");
        fxHalo       = fxHalo       ? fxHalo       : CreateOrGet("FX_Halo");
        fxRing       = fxRing       ? fxRing       : CreateOrGet("FX_Ring");

        // Colores iniciales
        if (fxWhiteFlash) fxWhiteFlash.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        if (fxRays)       fxRays.color       = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        if (fxHalo)       fxHalo.color       = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        if (fxRing)       fxRing.color       = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

        // Traer delante
        fxOverlay.transform.SetAsLastSibling();
    }

    private IEnumerator SpawnSparklesBurst(int count, float life, float radius01 = 0.35f)
    {
        if (spSparkle == null || fxOverlay == null) yield break;
        var root = fxOverlay.transform as RectTransform;
        var rng = new System.Random(); // <- System.Random, no Unity

        var spawned = new List<Image>(count);
        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("FX_Sparkle", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(root, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(32, 32);

            float ang = (float)(rng.NextDouble() * Mathf.PI * 2f);
            float rad = radius01 * Mathf.Lerp(0.2f, 1f, (float)rng.NextDouble()) * Mathf.Min(root.rect.width, root.rect.height) * 0.5f;
            Vector2 pos = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
            rt.anchoredPosition = pos;

            var img = go.GetComponent<Image>();
            img.sprite = spSparkle;
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, 0f);
            spawned.Add(img);

            // Tweens
            img.DOFade(0.95f, 0.18f).SetEase(Ease.OutQuad).SetUpdate(true).OnComplete(() =>
            {
                img.DOFade(0f, life - 0.18f).SetEase(Ease.InSine).SetUpdate(true);
            });

            rt.localScale = Vector3.one * 0.6f;
            rt.DOScale(1.15f, life).SetEase(Ease.OutSine).SetUpdate(true);
            rt.DORotate(new Vector3(0, 0, UnityEngine.Random.Range(-90f, 90f)), life, RotateMode.LocalAxisAdd) // <- UnityEngine.Random
            .SetEase(Ease.Linear)
            .SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(life);
        foreach (var s in spawned) if (s) Destroy(s.gameObject);
    }



    private Color GetElementColor(string elementRaw)
    {
        string e = string.IsNullOrEmpty(elementRaw) ? "luz" : elementRaw.Trim().ToLowerInvariant();
        // Colores suaves/“mágicos” para el flash
        switch (e)
        {
            case "fuego": return new Color(1.00f, 0.40f, 0.25f); // naranja/rojo
            case "agua": return new Color(0.25f, 0.65f, 1.00f); // azul
            case "naturaleza": return new Color(0.35f, 0.95f, 0.50f); // verde
            case "oscuridad": return new Color(0.75f, 0.35f, 1.00f); // violeta
            case "luz":
            default: return new Color(1.00f, 0.92f, 0.45f); // dorado
        }
    }
    // === Helpers de reflexión para efectos opcionales (UIShiny / UIParticle) ===
    private static System.Type FindType(string fullName)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(fullName);
            if (t != null) return t;
        }
        return null;
    }

    private static void SetProp(Component c, string prop, object value)
    {
        if (c == null) return;
        var p = c.GetType().GetProperty(prop, BindingFlags.Instance | BindingFlags.Public);
        if (p != null && p.CanWrite) p.SetValue(c, value, null);
    }

    private static float GetPropFloat(Component c, string prop, float fallback)
    {
        if (c == null) return fallback;
        var p = c.GetType().GetProperty(prop, BindingFlags.Instance | BindingFlags.Public);
        if (p != null && p.CanRead)
        {
            var v = p.GetValue(c, null);
            if (v is float f) return f;
        }
        return fallback;
    }

    private static Component EnsureComponent(GameObject go, string fullTypeName)
    {
        var t = FindType(fullTypeName);
        if (t == null || go == null) return null;
        var comp = go.GetComponent(t);
        return comp != null ? comp : go.AddComponent(t);
    }

    private static void AnimateShinySweep(Component shiny, float duration, float delay)
    {
        if (shiny == null) return;
        // UIShiny tiene propiedad float 'location' y Color 'effectColor', etc.
        float start = -0.3f;
        SetProp(shiny, "location", start);
        DG.Tweening.DOTween
            .To(() => GetPropFloat(shiny, "location", start),
                v => SetProp(shiny, "location", v),
                1.3f, duration)
            .SetDelay(delay)
            .SetUpdate(true)
            .SetEase(DG.Tweening.Ease.InOutSine);
    }


    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    private static string FormatPercent(float v)
    {
        // Si el valor es 0..1 tratamos como fracción (0.15 => 15%)
        if (v > 0f && v <= 1f) return $"{Mathf.RoundToInt(v * 100f)}%";
        return $"{v:0.#}%";
    }

    // Rellena basicStatsUpTxt y skillUpTxt con awakenBonus + awakenUpgrades (incluido effect[])
    // Rellena basicStatsUpTxt y skillUpTxt con awakenBonus + awakenUpgrades (incluido effect[])
    // Rellena basicStatsUpTxt y skillUpTxt con awakenBonus + awakenUpgrades (incluye effect[] + glosario)
    private void BuildAwakenTexts(HeroCatalogEntry entry)
    {
        if (entry == null) return;

        // --- BASIC STATS ---
        string basic = "Aumenta las estadísticas básicas.";
        if (entry.awakenBonus != null && !string.IsNullOrEmpty(entry.awakenBonus.stat))
        {
            var stat = CleanStatLabel(entry.awakenBonus.stat);
            var val = FormatPercent(entry.awakenBonus.value);
            basic += $"\nAdemás se mejorará <b>{stat}</b> en <b>{val}</b>.";
        }
        if (basicStatsUpTxt != null) basicStatsUpTxt.text = basic;

        // --- SKILL UPGRADES ---
        var sb = new System.Text.StringBuilder();
        var ups = entry.awakenUpgrades;

        if (ups != null && ups.Count > 0)
        {
            foreach (var u in ups)
            {
                if (u.effect != null && u.effect.Count > 0)
                {
                    string typeNorm = string.IsNullOrEmpty(u.skill) ? null : u.skill.ToLowerInvariant();
                    string skillName = !string.IsNullOrEmpty(typeNorm)
                        ? (entry.GetSkillNameByType(typeNorm) ?? typeNorm)
                        : null;

                    foreach (var e in u.effect)
                    {
                        string msg;

                        // (1) chance: aumenta la probabilidad de la habilidad
                        if (!string.IsNullOrEmpty(e.type) && e.type.Equals("chance", StringComparison.OrdinalIgnoreCase))
                        {
                            string inc = (e.value > 0f) ? FormatPercent(e.value) : null; // 0.1 -> "10%"
                            string prefix = (skillName != null) ? $"Habilidad <b>{skillName}</b>: " : string.Empty;
                            msg = $"{prefix}Probabilidad <b>+{inc}</b>.";
                            sb.AppendLine(msg);
                            continue;
                        }

                        // (2) extender duración de debuffs concretos (appliesTo[])
                        if (!string.IsNullOrEmpty(e.type) && e.type.Equals("extend_debuff_duration", StringComparison.OrdinalIgnoreCase))
                        {
                            int turns = (e.value > 0f) ? Math.Max(1, (int)Math.Round(e.value)) : Math.Max(1, e.duration);
                            string targetEs = TargetToEs(e.target);
                            string listDebuffs = BuildGlossaryList(e.appliesTo);
                            string prefix = (skillName != null) ? $"Habilidad <b>{skillName}</b>: " : string.Empty;
                            string tailTarget = string.IsNullOrEmpty(targetEs) ? "" : $" a {targetEs}";
                            msg = $"{prefix}Extiende en <b>+{turns} turno/s</b> la duración de {listDebuffs}{tailTarget}.";
                            // Gramática: "a el" -> "al"
                            msg = msg.Replace(" a el ", " al ");
                            sb.AppendLine(msg);
                            continue;
                        }

                        // (3) valor genérico
                        if (!string.IsNullOrEmpty(e.type) && e.type.Equals("value", StringComparison.OrdinalIgnoreCase))
                        {
                            string inc = (e.value > 0f) ? FormatPercent(e.value) : null;
                            string prefix = (skillName != null) ? $"Habilidad <b>{skillName}</b>: " : string.Empty;
                            msg = $"{prefix}Mejora cuantitativa <b>+{inc}</b>.";
                            sb.AppendLine(msg);
                            continue;
                        }

                        // (4) efecto normal: usa glosarioEstadosES (mantiene _ en la clave)
                        string effectDisp = GlossaryNameOrKey(e.type, true);
                        string targetEsN = TargetToEs(e.target);
                        string durTxt = (e.duration > 0) ? $"{e.duration} turno/s" : null;
                        string chTxt = (e.chance > 0f) ? FormatPercent(e.chance) : null;

                        var parts = new System.Collections.Generic.List<string>();
                        if (!string.IsNullOrEmpty(effectDisp)) parts.Add($"<b>{effectDisp}</b>");
                        if (!string.IsNullOrEmpty(durTxt)) parts.Add(durTxt);
                        if (!string.IsNullOrEmpty(chTxt)) parts.Add($"({chTxt})");
                        if (!string.IsNullOrEmpty(targetEsN)) parts.Add($"a {targetEsN}");

                        string core = string.Join(" ", parts.Where(p => !string.IsNullOrEmpty(p)));

                        string tierTxt = TierUpgradeToEs(e.fromTier, e.tier);
                        string prefixN = (skillName != null) ? $"Habilidad <b>{skillName}</b>: " : string.Empty;

                        msg = string.IsNullOrEmpty(tierTxt)
                            ? $"{prefixN}Aplica {core}."
                            : $"{prefixN}Aplica {core}. {tierTxt}";

                        msg = msg.Replace(" a el ", " al "); // ajuste gramatical
                        sb.AppendLine(msg);
                    }

                    continue;
                }

                // stat/value global
                if (!string.IsNullOrEmpty(u.stat) && u.value != 0 && string.IsNullOrEmpty(u.skill) && string.IsNullOrEmpty(u.skillRef))
                {
                    var stat = CleanStatLabel(u.stat);
                    var val = FormatPercent(u.value);
                    sb.AppendLine($"Aumento de <b>{stat}</b> <b>{val}</b>.");
                    continue;
                }

                // por tipo con texto libre
                if (!string.IsNullOrEmpty(u.skill) && !string.IsNullOrEmpty(u.improvement))
                {
                    string type = u.skill.ToLowerInvariant();
                    string skName = entry.GetSkillNameByType(type) ?? type;
                    sb.AppendLine($"Habilidad <b>{skName}</b>: {u.improvement}");
                    continue;
                }

                // legacy: skillRef + effectCode + from/to (traducido vía glosario también)
                if (!string.IsNullOrEmpty(u.skillRef) && !string.IsNullOrEmpty(u.effectCode))
                {
                    string skn = entry.GetSkillNameById(u.skillRef) ?? u.skillRef;
                    string eff = GlossaryNameOrKey(u.effectCode, true);
                    string fromTxt = FormatPercent(u.from);
                    string toTxt = FormatPercent(u.to);
                    var line = $"Mejora de Habilidad: <b>{skn}</b>: efecto <b>{eff}</b> de <b>{fromTxt}</b> a <b>{toTxt}</b>.";
                    sb.AppendLine(line);
                }
            }
        }

        if (skillUpTxt != null)
            skillUpTxt.text = sb.Length > 0 ? sb.ToString().TrimEnd('\n', '\r') : "—";
    }




    private static string TierUpgradeToEs(string fromTier, string toTier)
    {
        if (string.IsNullOrEmpty(toTier) && string.IsNullOrEmpty(fromTier)) return null;

        string Nice(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            s = s.Trim().ToLowerInvariant();
            if (s == "bajo") return "bajo";
            if (s == "medio" || s == "media") return "medio";
            if (s == "alto" || s == "alta") return "alto";
            return s;
        }

        string to = Nice(toTier);
        string frm = Nice(fromTier);

        if (!string.IsNullOrEmpty(frm) && !string.IsNullOrEmpty(to))
            return $"Mejora de tier: <b>{frm}</b> → <b>{to}</b>.";
        if (!string.IsNullOrEmpty(to))
            return $"El efecto asciende a tier <b>{to}</b>.";
        return null;
    }


    // === API coherente con Stats/Skills (el Tab llama a esto) ===
    public void SetHero(HeroProgress heroProgress, HeroCatalogEntry catalogEntry)
    {
        var pd = GameDataManager.Instance != null ? GameDataManager.Instance.PlayerData : null;
        if (pd == null)
        {
            Debug.LogError("[Despertar] PlayerData no disponible (GameDataManager.Instance.PlayerData es null).");
            return;
        }
        Show(pd, heroProgress, catalogEntry, null);
    }
    private void LoadAwakenComparePortraits()
    {
        if (catalog == null)
        {
            if (heroBeforeImg) heroBeforeImg.sprite = null;
            if (heroAfterImg) heroAfterImg.sprite = null;
            return;
        }

        // BEFORE = portraitAddressable
        if (heroBeforeImg)
        {
            if (!string.IsNullOrEmpty(catalog.portraitAddressable))
            {
                Addressables.LoadAssetAsync<Sprite>(catalog.portraitAddressable).Completed += h =>
                {
                    if (h.Status == AsyncOperationStatus.Succeeded) heroBeforeImg.sprite = h.Result;
                    else heroBeforeImg.sprite = null;
                };
            }
            else heroBeforeImg.sprite = null;
        }

        // AFTER = portraitAddressableAwaken (o fallback al normal si no hay)
        if (heroAfterImg)
        {
            string key = !string.IsNullOrEmpty(catalog.portraitAddressableAwaken)
                ? catalog.portraitAddressableAwaken
                : catalog.portraitAddressable;

            if (!string.IsNullOrEmpty(key))
            {
                Addressables.LoadAssetAsync<Sprite>(key).Completed += h =>
                {
                    if (h.Status == AsyncOperationStatus.Succeeded) heroAfterImg.sprite = h.Result;
                    else heroAfterImg.sprite = null;
                };
            }
            else heroAfterImg.sprite = null;
        }
    }


    // ---------- API ----------
    public void Show(PlayerData playerData, HeroProgress heroProgress, HeroCatalogEntry catalogEntry, System.Action onAwakened = null)
    {
        player = playerData;
        progress = heroProgress;
        catalog = catalogEntry;
        EnsureFxSetup();
        _onAwakened = onAwakened;

        // Placeholders inmediatos (evitar parpadeos)
        if (skillUpTxt) skillUpTxt.text = "—";
        if (basicStatsUpTxt) basicStatsUpTxt.text = "Aumenta las estadísticas básicas";

        string targetName = (!string.IsNullOrEmpty(catalog?.awakenName)) ? catalog.awakenName : (catalog?.displayName ?? "?");
        if (nameAwakenTxt) nameAwakenTxt.text = $"Se convierte en <color=#B066FF><b>{targetName}</b></color>";

        if (btnDespertar)
        {
            btnDespertar.onClick.RemoveAllListeners();
            btnDespertar.onClick.AddListener(OnClickDespertar);
        }

        var req = catalog?.awakenRequirements;
        Debug.Log($"[Despertar] heroId={progress?.heroId} awaken={progress?.awaken} " +
                  $"avail={(req != null && req.available)} elem={catalog?.element}");

        // Comparativa de retratos (antes/después)
        LoadAwakenComparePortraits();

        // >>> Descripciones de Bonus/Upgrades del awaken <<<
        BuildAwakenTexts(catalog);

        // Refresco de slots/botón/paneles
        RefreshUI();
    }



    // ---------- Internos ----------
    private void RefreshUI()
    {
        bool alreadyAwaken = progress != null && progress.awaken;

        if (noDespiertoPanel) noDespiertoPanel.SetActive(!alreadyAwaken);
        if (despiertoPanel)   despiertoPanel.SetActive(alreadyAwaken);

        if (alreadyAwaken) return; // no hay que pintar slots

        var req = catalog.awakenRequirements;
        string elem = NormalizeElement((catalog.element ?? "luz").ToLowerInvariant());

        var list = new List<(string key, int need)>(4);
        // ELEMENTO → extracto/infusion/destilado
        if (req.element_baja  > 0) list.Add( (PlayerData.AwakenKey("element", elem, "extracto"),  req.element_baja) );
        if (req.element_media > 0) list.Add( (PlayerData.AwakenKey("element", elem, "infusion"),  req.element_media) );
        if (req.element_alta  > 0) list.Add( (PlayerData.AwakenKey("element", elem, "destilado"), req.element_alta) );
        // CAOS (antes magic)
        if (req.magic_baja  > 0) list.Add( (PlayerData.AwakenKey("caos",  null, "extracto"),  req.magic_baja) );
        if (req.magic_media > 0) list.Add( (PlayerData.AwakenKey("caos",  null, "infusion"),  req.magic_media) );
        if (req.magic_alta  > 0) list.Add( (PlayerData.AwakenKey("caos",  null, "destilado"), req.magic_alta) );

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < list.Count)
            {
                var (key, need) = list[i];
                slots[i].key = key;
                slots[i].needed = need;

                if (slots[i].icon)
                {
                    slots[i].icon.sprite = GetSpriteForKey(key);
                    slots[i].icon.enabled = (slots[i].icon.sprite != null);
                }

                // ✅ LECTURA del inventario con tu wrapper (que ahora sí persiste)
                int owned = player.awakenInventory?.Get(key) ?? 0;
                bool enough = owned >= need;
                if (slots[i].txt)
                    slots[i].txt.text = enough ? $"<color=#5FE05F>{owned}/{need}</color>" : $"<color=#FF4B4B>{owned}/{need}</color>";

                slots[i].icon.transform.parent.gameObject.SetActive(true);
            }
            else if (slots[i]?.icon)
            {
                slots[i].icon.transform.parent.gameObject.SetActive(false);
                slots[i].key = null;
                slots[i].needed = 0;
            }
        }

        bool canAwaken = req.available && !alreadyAwaken;
        foreach (var s in slots)
        {
            if (string.IsNullOrEmpty(s.key)) continue;
            int owned = player.awakenInventory?.Get(s.key) ?? 0;
            if (owned < s.needed) { canAwaken = false; break; }
        }
        if (btnDespertar) btnDespertar.interactable = canAwaken;
    }

    private void OnClickDespertar()
    {
        string title = "Despertar";
        string body  = BuildConfirmBody();

        if (confirmDialog != null)
        {
            // Lanzamos la coroutine desde el callback del diálogo
            confirmDialog.Open(title, body, () => StartCoroutine(DoAwaken()));
        }
        else
        {
            StartCoroutine(DoAwaken());
        }
    }

    private string BuildConfirmBody()
    {
        var req = catalog.awakenRequirements;
        string elem = NormalizeElement((catalog.element ?? "luz").ToLowerInvariant());

        string e = $"Elemento ({elem}): "
                 + Part(req.element_baja,  "extracto")
                 + Part(req.element_media, "infusion")
                 + Part(req.element_alta,  "destilado");

        string c = "Caos: "
                 + Part(req.magic_baja,  "extracto")
                 + Part(req.magic_media, "infusion")
                 + Part(req.magic_alta,  "destilado");

        return $"¿Confirmas el Despertar?\n\n{e}\n{c}";
    }

    private string Part(int amount, string tier) => amount > 0 ? $"{tier} ×{amount}   " : "";

    private IEnumerator DoAwaken()
    {
        // 1) Validar recursos
        foreach (var s in slots)
        {
            if (string.IsNullOrEmpty(s.key)) continue;
            int owned = player.awakenInventory?.Get(s.key) ?? 0;
            if (owned < s.needed)
            {
                Debug.LogWarning("[Despertar] Recursos insuficientes, no se despierta.");
                RefreshUI();
                yield break;
            }
        }

        // 2) Gastar recursos
        foreach (var s in slots)
        {
            if (string.IsNullOrEmpty(s.key) || s.needed <= 0) continue;
            player.awakenInventory.TrySpend(s.key, s.needed);
        }

        // 3) Marcar awaken y reflejar en PlayerData.heroes
        if (progress != null) progress.awaken = true;
        if (player?.heroes != null)
        {
            var h = player.heroes.FirstOrDefault(x => x.heroId == progress.heroId);
            if (h != null)
            {
                h.awaken = true;
                if (progress.Maestries != null)
                {
                    if (h.Maestries == null) h.Maestries = new List<string>();
                    h.Maestries.Clear();
                    h.Maestries.AddRange(progress.Maestries);
                }
            }
        }

        // 4) Guardar a disco (sin yields dentro del try/catch)
        try
        {
            player.NormalizeAfterLoad();
            player.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Despertar] Error al guardar player_data.json: {e}");
        }

        // 5) Verificar guardado en disco (fuera del try/catch)
        yield return StartCoroutine(VerifySaveWrittenToDisk(progress?.heroId));
        try
        {
            var path = GameDataManager.Instance.GetPlayerDataPath();
            var justSaved = File.ReadAllText(path);
            bool ok = justSaved.Contains($"\"heroId\": \"{progress.heroId}\"") && justSaved.Contains("\"awaken\": true");
            Debug.Log($"[Despertar][POST-DISK] {progress.heroId} awaken_en_disco={ok}");
            Debug.Log("[Despertar] Guardado correcto: awaken=true.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Despertar] No se pudo verificar el guardado en disco: {e.Message}");
        }

        // 6) FX + refrescos
        yield return StartCoroutine(PlayAwakenSequence(null));

        RefreshUI();

        var grid = UnityEngine.Object.FindObjectOfType<HeroGridController>(false);
        if (grid != null)
            grid.RefreshCardStars(progress.heroId);

        _onAwakened?.Invoke();
    }

    private static string GlossaryLink(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        var g = HeroCatalogManager.Instance?.GlossaryES;
        if (g != null && g.TryGetValue(key, out var info))
        {
            string display = string.IsNullOrEmpty(info.display) ? key : info.display;
            return $"<link=\"glossary:{key}\"><u><i>{display}</i></u></link>";
        }
        // Fallback
        return $"<link=\"glossary:{key}\"><u><i>{key}</i></u></link>";
    }

    // Normaliza claves del glosario: minúsculas + espacios/guiones -> "_"
    private static string NormalizeGlossaryKey(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        var s = raw.Trim().ToLowerInvariant();
        s = s.Replace(' ', '_').Replace('-', '_');
        while (s.Contains("__")) s = s.Replace("__", "_");
        return s;
    }


    // Devuelve el nombre ES del glosario o, si no existe, una versión amigable
    private static string GlossaryNameOrKey(string rawKey, bool asLink)
    {
        var key = NormalizeGlossaryKey(rawKey);
        if (string.IsNullOrEmpty(key)) return rawKey ?? "";

        var g = HeroCatalogManager.Instance?.GlossaryES;
        if (g != null && g.TryGetValue(key, out var entry))
        {
            // entry.display y entry.desc (tupla con nombres)
            var display = string.IsNullOrEmpty(entry.display) ? key : entry.display;
            if (asLink)
                return $"<link=\"glossary:{key}\"><u><i>{display}</i></u></link>";
            return display;
        }

        // Fallback legible: "turn_bar_up" -> "Turn bar up"
        var friendly = System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(key.Replace('_', ' '));
        return asLink ? $"<link=\"glossary:{key}\"><u><i>{friendly}</i></u></link>" : friendly;
    }


    // Lista con enlaces al glosario (para appliesTo[])
    private static string BuildGlossaryList(System.Collections.Generic.IEnumerable<string> keys)
    {
        if (keys == null) return "";
        var items = keys
            .Where(k => !string.IsNullOrEmpty(k))
            .Select(k => GlossaryNameOrKey(k, true))
            .ToList();

        if (items.Count == 0) return "";
        if (items.Count == 1) return items[0];
        if (items.Count == 2) return $"{items[0]} y {items[1]}";
        var allButLast = string.Join(", ", items.Take(items.Count - 1));
        return $"{allButLast} y {items[items.Count - 1]}";
    }


    private static string JoinNatural(System.Collections.Generic.IList<string> items)
    {
        if (items == null || items.Count == 0) return "";
        if (items.Count == 1) return items[0];
        if (items.Count == 2) return $"{items[0]} y {items[1]}";
        // 3 o más: "a, b y c"
        var allButLast = string.Join(", ", items.Take(items.Count - 1));
        return $"{allButLast} y {items[items.Count - 1]}";
    }
    private static string TargetToEs(string code)
    {
        if (string.IsNullOrEmpty(code)) return "el objetivo";
        switch (code.ToLowerInvariant())
        {
            case "enemy": return "un enemigo";
            case "all_enemy": return "todos los enemigos";
            case "ally": return "un aliado";
            case "all_ally": return "todos los aliados";
            case "self": return "ti mismo";
            default: return code.Replace('_', ' ');
        }
    }


    // --- helpers ---
    private static string NormalizeElement(string e)
    {
        switch (e)
        {
            case "blanco": return "luz";
            case "negro": return "oscuridad";
            case "rojo": return "fuego";
            case "verde": return "naturaleza";
            case "azul": return "agua";
            default: return e;
        }
    }

    private Sprite GetSpriteForKey(string key)
    {
        // CAOS
        if (key == "caos_extracto")  return caos_extracto;
        if (key == "caos_infusion")  return caos_infusion;
        if (key == "caos_destilado") return caos_destilado;

        if (!key.StartsWith("element_")) return null;

        var p = key.Split('_'); // "element", "<elem>", "<tier>"
        if (p.Length != 3) return null;

        string elem = p[1];
        string tier = p[2];

        switch (elem)
        {
            case "luz":
                if (tier=="extracto") return element_luz_extracto;
                if (tier=="infusion") return element_luz_infusion;
                if (tier=="destilado")return element_luz_destilado;
                break;
            case "oscuridad":
                if (tier=="extracto") return element_oscuridad_extracto;
                if (tier=="infusion") return element_oscuridad_infusion;
                if (tier=="destilado")return element_oscuridad_destilado;
                break;
            case "fuego":
                if (tier=="extracto") return element_fuego_extracto;
                if (tier=="infusion") return element_fuego_infusion;
                if (tier=="destilado")return element_fuego_destilado;
                break;
            case "naturaleza":
                if (tier=="extracto") return element_naturaleza_extracto;
                if (tier=="infusion") return element_naturaleza_infusion;
                if (tier=="destilado")return element_naturaleza_destilado;
                break;
            case "agua":
                if (tier=="extracto") return element_agua_extracto;
                if (tier=="infusion") return element_agua_infusion;
                if (tier=="destilado")return element_agua_destilado;
                break;
        }
        return null;
    }
}
