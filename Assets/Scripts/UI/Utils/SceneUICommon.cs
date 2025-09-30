/*
============================================================
SceneUICommon.cs — Utilidades compartidas para construir UI de escenas
------------------------------------------------------------
PROPÓSITO
- Crear Canvas raíz, cámara ortográfica si falta, y world roots.
- Cargar fondos Addressables al mundo.
- Crear elementos básicos (botones HUD).
- Instalar managers esenciales (GameBootstrap, MusicManager, PlayerResourcesManager).

MÉTODOS
- EnsureSceneUIRoot(): asegura Canvas overlay (ScaleWithScreenSize).
- EnsureWorldRoot(string name="WorldRoot"): crea/recupera nodo para fondo/mundo.
- CalculateInitialZoom(Vector2 worldSize, float visibleWorldHeightPercent): ayuda de zoom ortográfico.
- AddBackgroundToWorld(Transform parent, string addressableKey, Vector2 worldSize):
  Carga Sprite Addressables y ajusta escala a worldSize.
- EnsureWorldSceneStructure(Vector2 worldSize): cámara ortográfica, worldRoot y canvas.
- LoadSpriteFromAddressables(string key, Action<Sprite> cb): wrap de Addressables.
- EnsureEssentials(): instancia GameBootstrap y MusicManager (DontDestroyOnLoad).
- CreateButton(...): botón con texto y fuente Addressable (FreckleFace).
- CreateHUDItemPanel(...): crea fila HUD con icono Addressable y texto.
- LoadFontFromAddressables(...): utilidad para fuentes.
- EnsurePlayerResourcesManager(): crea PRM si falta.

NOTAS
- Si quieres autoinstalar GearUIRefreshManager, ya se auto-instala solo.
============================================================
*/


using UnityEngine;
using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;

public static class SceneUICommon
{
    public static GameObject EnsureSceneUIRoot()
    {
        // ✅ Crear cámara si no existe
        if (Camera.main == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            Camera cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGO.tag = "MainCamera";
            camGO.AddComponent<AudioListener>();
        }

        // ✅ Crear Canvas si no existe
        GameObject canvasGO = GameObject.Find("Canvas");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        return canvasGO;
    }


    public static GameObject EnsureWorldRoot(string worldName = "WorldRoot")
    {
        GameObject worldRoot = GameObject.Find(worldName);
        if (worldRoot == null)
        {
            worldRoot = new GameObject(worldName);
            worldRoot.transform.position = Vector3.zero;
        }
        return worldRoot;
    }

    public static float CalculateInitialZoom(Vector2 worldSize, float visibleWorldHeightPercent)
    {
        return (worldSize.y * visibleWorldHeightPercent) * 0.5f; // Camera.orthographicSize es la MITAD del alto visible
    }
    public static GameObject AddBackgroundToWorld(Transform parent, string addressableKey, Vector2 worldSize)
    {
        GameObject bgGO = new GameObject("Background", typeof(SpriteRenderer));
        bgGO.transform.SetParent(parent, false);
        SpriteRenderer sr = bgGO.GetComponent<SpriteRenderer>();

        Addressables.LoadAssetAsync<Sprite>(addressableKey).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Sprite sprite = handle.Result;
                sr.sprite = sprite;
                sr.sortingOrder = -10;

                // Ajustar escala proporcional al worldSize
                if (sprite.texture != null)
                {
                    float spriteWidth = sprite.texture.width / sprite.pixelsPerUnit;
                    float spriteHeight = sprite.texture.height / sprite.pixelsPerUnit;

                    float scaleX = worldSize.x / spriteWidth;
                    float scaleY = worldSize.y / spriteHeight;

                    bgGO.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                    Debug.Log($"🖼️ Fondo escalado correctamente ({scaleX}, {scaleY})");
                }
                else
                {
                    Debug.LogWarning("⚠️ Sprite cargado pero no tiene textura.");
                }

                Debug.Log($"🖼 Fondo cargado correctamente: {addressableKey}");
            }
            else
            {
                Debug.LogError($"❌ Error cargando fondo: {addressableKey}");
            }
        };

        return bgGO;
    }

    public static (GameObject canvas, GameObject worldRoot) EnsureWorldSceneStructure(Vector2 worldSize)
    {
        // 📷 Cámara ortográfica
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            cam = camGO.AddComponent<Camera>();
            cam.tag = "MainCamera";
            camGO.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographicSize = worldSize.y * 0.3f; // 60% view
        cam.transform.position = new Vector3(worldSize.x / 2f, worldSize.y / 2f, -10);

        // 🌍 WorldRoot (para background y navegación)
        GameObject worldRoot = GameObject.Find("WorldRoot") ?? new GameObject("WorldRoot");
        worldRoot.transform.position = Vector3.zero;

        // 🖼️ Canvas
        GameObject canvas = EnsureSceneUIRoot();

        return (canvas, worldRoot);
    }

    public static void LoadSpriteFromAddressables(string key, Action<Sprite> callback)
    {
        Addressables.LoadAssetAsync<Sprite>(key).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
                callback?.Invoke(handle.Result);
            else
            {
                Debug.LogWarning($"❌ Sprite '{key}' no se pudo cargar desde Addressables.");
                callback?.Invoke(null);
            }
        };
    }

    public static void EnsureEssentials()
    {
        if (UnityEngine.Object.FindFirstObjectByType<GameBootstrap>() == null)
        {
            GameObject go = new GameObject("GameBootstrap");
            go.AddComponent<GameBootstrap>();
            if (Application.isPlaying)
                UnityEngine.Object.DontDestroyOnLoad(go);
            Debug.Log("✅ GameBootstrap instanciado.");
        }

        if (UnityEngine.Object.FindFirstObjectByType<MusicManager>() == null)
        {
            GameObject go = new GameObject("MusicManager");
            go.AddComponent<MusicManager>();
            if (Application.isPlaying)
                UnityEngine.Object.DontDestroyOnLoad(go);
            Debug.Log("🎵 MusicManager instanciado.");
        }
    }


    public static GameObject CreateButton(string name, string text, Vector2 size, Vector2 anchor, Transform parent, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGO = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(Image));
        buttonGO.transform.SetParent(parent, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.sizeDelta = size;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;

        GameObject textGO = new GameObject("Text", typeof(Text));
        textGO.transform.SetParent(buttonGO.transform, false);
        Text txt = textGO.GetComponent<Text>();
        txt.text = text;
        txt.fontSize = 32;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.rectTransform.anchorMin = Vector2.zero;
        txt.rectTransform.anchorMax = Vector2.one;
        txt.rectTransform.offsetMin = Vector2.zero;
        txt.rectTransform.offsetMax = Vector2.zero;

        Addressables.LoadAssetAsync<Font>("Fonts/FreckleFace").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                txt.font = handle.Result;
            }
            else
            {
                Debug.LogWarning("⚠️ No se pudo cargar la fuente desde Addressables.");
            }
        };

        buttonGO.GetComponent<Button>().onClick.AddListener(onClick);

        return buttonGO;
    }

    public static RectTransform CreateHUDItemPanel(GameObject parent, string name, string iconName, string value, float anchorX, float panelWidth, bool shorten, Font font, float iconSize, float spacing)
    {
        GameObject itemPanel = new GameObject($"{name}Panel", typeof(RectTransform));
        itemPanel.transform.SetParent(parent.transform, false);
        RectTransform itemRect = itemPanel.GetComponent<RectTransform>();
        itemRect.anchorMin = itemRect.anchorMax = new Vector2(0, 0.5f);
        itemRect.pivot = new Vector2(0, 0.5f);
        itemRect.sizeDelta = new Vector2(panelWidth, iconSize);

        float panelX = parent.GetComponent<RectTransform>().sizeDelta.x * anchorX - (anchorX >= 0.9f ? panelWidth : 0);
        itemRect.anchoredPosition = new Vector2(panelX, 0);

        // Icono con Addressables
        GameObject iconGO = new GameObject($"{name}Icon", typeof(Image));
        iconGO.transform.SetParent(itemPanel.transform, false);
        Image icon = iconGO.GetComponent<Image>();
        icon.preserveAspect = true;

        RectTransform iconRect = icon.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        iconRect.anchoredPosition = Vector2.zero;

        string iconPath = $"Art/MainMenu/{iconName}";
        Addressables.LoadAssetAsync<Sprite>(iconPath).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                icon.sprite = handle.Result;
            }
            else
            {
                Debug.LogWarning($"❌ No se pudo cargar el icono '{iconPath}' desde Addressables.");
            }
        };

        // Texto
        GameObject textGO = new GameObject($"{name}Text", typeof(Text));
        textGO.transform.SetParent(itemPanel.transform, false);
        Text text = textGO.GetComponent<Text>();
        text.text = shorten ? ShortenNumber(value) : value;
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        if (font != null) text.font = font;

        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = new Vector2(iconSize + spacing, 0);
        textRect.offsetMax = new Vector2(-iconSize - spacing * 2, 0);

        // Botón adicional
        GameObject extraButton = new GameObject($"{name}Button", typeof(Button), typeof(Image));
        extraButton.transform.SetParent(itemPanel.transform, false);
        RectTransform btnRect = extraButton.GetComponent<RectTransform>();
        btnRect.anchorMin = btnRect.anchorMax = new Vector2(1, 0.5f);
        btnRect.pivot = new Vector2(1, 0.5f);
        btnRect.sizeDelta = new Vector2(iconSize, iconSize);
        btnRect.anchoredPosition = Vector2.zero;
        extraButton.GetComponent<Image>().color = new Color(1, 1, 1, 0.2f);

        // Si es Energia, añadir contador oculto
        if (name == "Energia")
        {
            GameObject countdownTextGO = new GameObject("RegenCountdownText", typeof(Text));
            countdownTextGO.transform.SetParent(itemPanel.transform, false);
            Text cdText = countdownTextGO.GetComponent<Text>();
            cdText.text = "";
            cdText.fontSize = 16;
            cdText.color = Color.yellow;
            cdText.alignment = TextAnchor.UpperLeft;
            if (font != null) cdText.font = font;

            RectTransform cdRect = countdownTextGO.GetComponent<RectTransform>();
            cdRect.anchorMin = new Vector2(0, 0);
            cdRect.anchorMax = new Vector2(1, 0);
            cdRect.pivot = new Vector2(0.5f, 1);
            cdRect.anchoredPosition = new Vector2(0, -18);
            cdRect.sizeDelta = new Vector2(100, 20);

            countdownTextGO.SetActive(false);
        }

        return itemRect;
    }
    private static string ShortenNumber(string value)
    {
        // Para esta demo simple, asumimos que `value` es un número (no 20/120)
        if (value.Contains("/")) return value;

        if (int.TryParse(value, out int num))
        {
            if (num >= 1000000)
                return (num / 1000000f).ToString("0.#") + "M";
            if (num >= 1000)
                return (num / 1000f).ToString("0.#") + "K";
        }

        return value;
    }

    public static void LoadFontFromAddressables(string addressableKey, Action<Font> onLoaded, Font fallbackFont = null)
    {
        Addressables.LoadAssetAsync<Font>(addressableKey).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"🔤 Fuente cargada: {addressableKey}");
                onLoaded?.Invoke(handle.Result);
            }
            else
            {
                Debug.LogWarning($"⚠ No se pudo cargar la fuente: {addressableKey}, se usará la fuente por defecto.");
                onLoaded?.Invoke(fallbackFont); // Usa la fuente por defecto si la pasas
            }
        };
    }
    public static void EnsurePlayerResourcesManager()
    {
        if (UnityEngine.Object.FindFirstObjectByType<PlayerResourcesManager>() == null)
        {
            GameObject go = new GameObject("PlayerResourcesManager");
            go.AddComponent<PlayerResourcesManager>();
            if (Application.isPlaying)
                UnityEngine.Object.DontDestroyOnLoad(go);
            Debug.Log("🧠 PlayerResourcesManager instanciado desde SceneUICommon.");
        }
    }

    
}