/*
============================================================
Hero3DPanelController.cs — Render/poses del modelo 3D
------------------------------------------------------------
PROPÓSITO
- Cargar modelo, animación idle, LookAt, etc.

MÉTODOS (COMPLETA AQUÍ)
- SetHero(HeroCatalogEntry/HeroProgress).
- ClearHero().
============================================================
*/


using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;

public class Hero3DPanelController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform heroContainer;
    public Camera heroCamera;
    public RawImage heroBackground;       // 🔹 RawImage del Canvas
    public RectTransform rotationArea;

    [Header("Rotación y Zoom")]
    public float rotationSpeed = 150f;
    public float zoomStep = 0.1f;
    public float minZoom = 1f;
    public float maxZoom = 2f;
    public bool zoomRestrictedToArea = true;

    public TMP_InputField customNameInput;

    private HeroProgress currentHeroProgress; // mantener referencia

    [Header("Escalado del Modelo")]
    public float globalScaleMultiplier = 100f;

    [Header("Background Settings")]
    public Material backgroundMaterial;         // 🔹 Material con el shader
    public float parallaxIntensity = 0.02f;     // 🔹 Parallax al girar héroe
    public float backgroundScrollSpeed = 0.02f; // 🔹 Velocidad scroll lineal

    [Header("Brillo y Saturación del Héroe")]
    [Range(0.5f, 3f)]
    public float heroBrightness = 1.5f;         // 🔹 Multiplicador de brillo
    [Range(0f, 2f)]
    public float heroSaturation = 1.2f;         // 🔹 Multiplicador de saturación

    private float currentParallaxX = 0f;
    private float backgroundScrollX = 0f;

    private GameObject currentHeroModel;
    private float currentZoom = 1.5f;
    //private string lastHeroDescription;

    private Dictionary<string, Texture2D> backgroundCache = new Dictionary<string, Texture2D>();
    // === Suspender/Retomar 3D para ahorrar coste cuando la vista está oculta ===
    [SerializeField] private bool destroyInstanceOnSuspend = false;  // si true, destruye instancias 3D al ocultar
    private bool _suspended = false;
    public bool CanRun => isActiveAndEnabled && gameObject.activeInHierarchy;
    private void Awake()
    {
        if (heroBackground == null)
        {
            heroBackground = FindFirstObjectByType<RawImage>();
            if (heroBackground != null)
                Debug.Log($"[Hero3DPanelController] Fondo detectado en Canvas (RawImage): {heroBackground.gameObject.name}");
        }

        if (heroBackground != null && backgroundMaterial != null)
            heroBackground.material = backgroundMaterial;

    }

    // ============================
    // HEROES
    // ============================

    public void ShowHero(GameObject heroPrefab, HeroCatalogEntry heroEntry, HeroProgress heroProgress)
    {
        Debug.Log($"[Hero3DPanelController] ShowHero llamado con {heroEntry.heroId}, fondo={heroEntry.heroBackground}");

        ClearHero();

        if (heroPrefab == null) return;

        currentHeroModel = Instantiate(heroPrefab, heroContainer);
        SetLayerRecursively(currentHeroModel, LayerMask.NameToLayer("Hero3D"));
        CenterAndScaleHero(currentHeroModel);

        // 🔹 Forzar materiales al shader UnlitHeroColor con brillo y saturación
        ForceUnlitColorShader(currentHeroModel);

        if (!string.IsNullOrEmpty(heroEntry.heroBackground))
        {
            if (backgroundCache.TryGetValue(heroEntry.heroBackground, out Texture2D cachedTex))
            {
                ApplyBackground(cachedTex);
            }
            else
            {
                Debug.Log($"[Hero3DPanelController] Cargando fondo desde Addressables: {heroEntry.heroBackground}");
                Addressables.LoadAssetAsync<Texture2D>(heroEntry.heroBackground).Completed += handle =>
                {
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        Texture2D newTex = handle.Result;
                        backgroundCache[heroEntry.heroBackground] = newTex;
                        ApplyBackground(newTex);
                    }
                    else
                    {
                        Debug.LogError($"[Hero3DPanelController] ❌ ERROR al cargar fondo: {heroEntry.heroBackground}");
                    }
                };
            }
        }
        else
        {
            Debug.LogWarning($"[Hero3DPanelController] El héroe {heroEntry.heroId} no tiene heroBackground definido.");
        }

        currentHeroProgress = heroProgress;

    }

    private void ApplyBackground(Texture2D tex)
    {
        if (heroBackground == null)
        {
            Debug.LogError("[Hero3DPanelController] heroBackground es NULL");
            return;
        }

        // Asegura repeat + UVs reiniciados
        if (tex != null) tex.wrapMode = TextureWrapMode.Repeat;

        // Quita materiales especiales: el looper trabaja con uvRect del RawImage
        heroBackground.material = null;

        // Asigna textura y reinicia el rect UV
        heroBackground.texture = tex;
        heroBackground.uvRect = new Rect(0f, 0f, 1f, 1f);
        heroBackground.enabled = true;

        // Si existe el looper, pásale la textura (fuerza repeat + reset interno)
        var looper = heroBackground.GetComponent<HeroBackgroundLooper>();
        if (looper != null)
            looper.SetTexture(tex);

        Debug.Log($"[Hero3DPanelController] Fondo aplicado correctamente: {tex?.name}");
    }


    public void ClearHero()
    {
        if (currentHeroModel != null)
            Destroy(currentHeroModel);
    }

    // ============================
    // UPDATE
    // ============================
    private void Update()
    {
        if (currentHeroModel == null) return;

        HandleRotation();
        HandleZoom();
        UpdateBackground();

    }

    private void HandleRotation()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed && IsPointerOverRotationArea())
        {
            float deltaX = Mouse.current.delta.ReadValue().x;
            currentHeroModel.transform.Rotate(Vector3.up, -deltaX * rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void HandleZoom()
    {
        if (Mouse.current != null)
        {
            if (!zoomRestrictedToArea || IsPointerOverRotationArea())
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    currentZoom = Mathf.Clamp(currentZoom - scroll * zoomStep, minZoom, maxZoom);
                    UpdateCameraZoom();
                }
            }
        }
    }

    // ============================
    // BACKGROUND
    // ============================
    private void UpdateBackground()
    {
        if (heroBackground == null || backgroundMaterial == null)
            return;

        // === 1. Scroll infinito de izquierda a derecha ===
        backgroundScrollX += backgroundScrollSpeed * Time.deltaTime;
        if (backgroundScrollX > 1f) backgroundScrollX = 0f;

        // === 2. Parallax por rotación del héroe ===
        float parallaxOffset = 0f;
        if (currentHeroModel != null)
        {
            float heroRotationY = currentHeroModel.transform.localEulerAngles.y;
            if (heroRotationY > 180f) heroRotationY -= 360f;

            float normalizedRotation = Mathf.Clamp(heroRotationY / 30f, -1f, 1f);
            currentParallaxX = Mathf.Lerp(currentParallaxX, normalizedRotation * parallaxIntensity, Time.deltaTime * 5f);

            parallaxOffset = currentParallaxX;
        }

        // === 3. Aplicar al material ===
        backgroundMaterial.SetFloat("_OffsetX", backgroundScrollX + parallaxOffset);
        backgroundMaterial.SetFloat("_OffsetY", 0f);
    }

    private bool IsPointerOverRotationArea()
    {
        if (rotationArea == null) return true;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rotationArea,
            Mouse.current.position.ReadValue(),
            null,
            out Vector2 localMousePos
        );

        return rotationArea.rect.Contains(localMousePos);
    }

    private void UpdateCameraZoom()
    {
        heroCamera.transform.localPosition = new Vector3(0, 0, -currentZoom * 5f);
        heroCamera.transform.LookAt(heroContainer);
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private void CenterAndScaleHero(GameObject hero)
    {
        hero.transform.localPosition = Vector3.zero;
        hero.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        Renderer[] renderers = hero.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        float modelHeight = bounds.size.y;
        if (modelHeight <= 0.001f) return;

        float scaleFactor = (1f / modelHeight) * globalScaleMultiplier;
        hero.transform.localScale = Vector3.one * scaleFactor;

        UpdateCameraZoom();

        Debug.Log($"[Hero3DPanelController] Escalado {hero.name}, altura={modelHeight}, factor aplicado={scaleFactor}");
    }

    // ============================
    // FORCE UNLIT HERO COLOR
    // ============================
    private void ForceUnlitColorShader(GameObject hero)
    {
        Renderer[] renderers = hero.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                mat.shader = Shader.Find("Custom/UnlitHeroColor");
                mat.SetFloat("_Brightness", heroBrightness);
                mat.SetFloat("_Saturation", heroSaturation);
            }
        }

        Debug.Log($"[Hero3DPanelController] Materiales del héroe forzados a UnlitHeroColor (Brillo={heroBrightness}, Saturación={heroSaturation}).");
    }

    public System.Collections.IEnumerator LoadFirstHeroAsync(
        HeroProgress hp,
        HeroCatalogEntry cat,
        System.Action<float, string> onProgress = null
    )
    {
        // ⛔ Si el GO está inactivo o el componente no está habilitado, no arranques nada
        if (hp == null || cat == null) yield break;
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            Debug.Log("[Hero3DPanel] Panel inactivo: se omite carga 3D inicial.");
            yield break;
        }

        // 1) Decide orden de intento (awaken → normal, o normal → awaken)
        bool wantsAwaken = hp.awaken && !string.IsNullOrEmpty(cat.modelAddressableAwaken);
        string primaryKey = wantsAwaken ? cat.modelAddressableAwaken : cat.modelAddressable;
        string fallbackKey = wantsAwaken ? cat.modelAddressable : cat.modelAddressableAwaken;

        // 2) Intenta cargar el prefab con fallback (SIN StartCoroutine para evitar error si se desactiva)
        onProgress?.Invoke(0.05f, "Localizando modelo 3D…");
        GameObject loadedPrefab = null;
        var loadRoutine = LoadPrefabWithFallback(primaryKey, fallbackKey, go => loadedPrefab = go);
        while (true)
        {
            // Si durante la carga desactivan el panel, abortamos ordenadamente
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
            {
                Debug.Log("[Hero3DPanel] Panel desactivado durante la carga: abortado.");
                yield break;
            }
            if (!loadRoutine.MoveNext()) break;
            yield return loadRoutine.Current;
        }

        if (loadedPrefab == null)
        {
            Debug.LogError($"[Hero3DPanel] ❌ No se pudo cargar NINGÚN prefab. primary='{primaryKey}' fallback='{fallbackKey}'");
            yield break;
        }

        onProgress?.Invoke(0.35f, "Instanciando modelo 3D…");
        ShowHero(loadedPrefab, cat, hp);
        yield return null;

        // 3) Fondo (heroBackground)
        if (!string.IsNullOrEmpty(cat.heroBackground))
        {
            onProgress?.Invoke(0.60f, "Cargando fondo del héroe…");
            var hTex = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<Texture2D>(cat.heroBackground);
            yield return hTex;

            if (!isActiveAndEnabled || !gameObject.activeInHierarchy) yield break;

            if (hTex.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                ApplyBackground(hTex.Result);
            }
            else
            {
                Debug.LogError($"[Hero3DPanel] ❌ No se pudo cargar fondo: {cat.heroBackground}");
            }
        }

        onProgress?.Invoke(1f, "Ajustando cámara…");
        yield return null;
    }


    private System.Collections.IEnumerator LoadPrefabWithFallback(
        string primaryKey,
        string fallbackKey,
        System.Action<GameObject> onLoaded
    )
    {
        // Intenta PRIMARY, y si no, FALLBACK (si existe)
        string[] candidates = string.IsNullOrEmpty(fallbackKey)
            ? new[] { primaryKey }
            : new[] { primaryKey, fallbackKey };

        for (int i = 0; i < candidates.Length; i++)
        {
            string key = candidates[i];
            if (string.IsNullOrEmpty(key)) continue;

            // Pre-check para evitar InvalidKeyException ruidosas
            var locHandle = UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync(key);
            yield return locHandle;
            bool hasLocation = locHandle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded
                               && locHandle.Result != null
                               && locHandle.Result.Count > 0;

            // (Opcional) liberar el handle de locations
            UnityEngine.AddressableAssets.Addressables.Release(locHandle);

            if (!hasLocation)
            {
                Debug.LogWarning($"[Hero3DPanel] No hay Addressable para key='{key}'. Probando siguiente…");
                continue;
            }

            var hPrefab = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(key);
            yield return hPrefab;

            if (hPrefab.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                onLoaded?.Invoke(hPrefab.Result);
                yield break;
            }
            else
            {
                Debug.LogError($"[Hero3DPanel] ❌ Falló la carga del prefab '{key}'. Probando siguiente…");
            }
        }

        onLoaded?.Invoke(null);
    }





    public void OnCustomNameChanged(string newName)
    {
        string finalName = customNameInput != null ? customNameInput.text : newName;

        Debug.Log($"[Hero3DPanelController] OnCustomNameChanged recibido: '{newName}' | InputField.text: '{finalName}'");

        if (currentHeroProgress == null) return;

        if (string.IsNullOrWhiteSpace(finalName))
            currentHeroProgress.customName = null;
        else
            currentHeroProgress.customName = finalName.Trim().Substring(0, Mathf.Min(15, finalName.Trim().Length));

        GameDataManager.Instance.PlayerData.Save();
        Debug.Log($"[Hero3DPanelController] CustomName guardado: {currentHeroProgress.customName}");
    }

    public void SetSuspended(bool value)
    {
        if (_suspended == value) return;
        _suspended = value;

        if (destroyInstanceOnSuspend && value)
        {
            // Destruir cualquier malla/instancia 3D dentro de este panel
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) Destroy(r.gameObject);
            var anims = GetComponentsInChildren<Animator>(true);
            foreach (var a in anims) Destroy(a.gameObject);
            return;
        }

        // Alternativa: activar/desactivar componentes pesados (más suave, no destruye)
        var skinned = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var s in skinned) s.enabled = !value;

        var meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (var r in meshRenderers) r.enabled = !value;

        var animators = GetComponentsInChildren<Animator>(true);
        foreach (var a in animators) a.enabled = !value;

        var particles = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var p in particles)
        {
            var em = p.emission; em.enabled = !value;
            if (value) p.Clear(true);
        }
    }
    /// <summary>
    /// Lanza LoadFirstHeroAsync usando el 'runner' indicado, sólo si el panel está activo.
    /// Devuelve la Coroutine o null si se omitió por estar inactivo.
    /// </summary>
    public Coroutine LoadFirstHeroIfActive(
        MonoBehaviour runner,
        HeroProgress hp,
        HeroCatalogEntry cat,
        System.Action<float,string> onStep = null)
    {
        if (!CanRun)
        {
            Debug.Log("[Hero3DPanel] Saltado: panel inactivo (no se carga el modelo).");
            return null;
        }
        return runner.StartCoroutine(LoadFirstHeroAsync(hp, cat, onStep));
    }

}
