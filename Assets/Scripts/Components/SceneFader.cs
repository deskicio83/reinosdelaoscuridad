/*
============================================================
SceneFader.cs — Fundidos globales de escena
------------------------------------------------------------
PROPÓSITO
- Controlar un overlay fullscreen (CanvasGroup/Image) para fundidos.

USO
- Instanciar único en escena; usar FadeIn/Out en transiciones.

MÉTODOS (COMPLETA AQUÍ)
- FadeIn(duration)/FadeOut(duration, onDone).
============================================================
*/

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }

    [Header("Duraciones por defecto (segundos)")]
    [SerializeField] private float defaultFadeOutDuration = 0.35f;
    [SerializeField] private float defaultFadeInDuration  = 0.35f;

    private Canvas overlayCanvas;
    private Image  fadeImage;
    private CanvasGroup fadeGroup;

    private bool busy;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildOverlayIfNeeded();
    }

    private void BuildOverlayIfNeeded()
    {
        if (overlayCanvas != null && fadeImage != null && fadeGroup != null) return;

        // Canvas overlay
        var canvasGO = new GameObject("SceneFaderCanvas", typeof(Canvas), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasGO);

        overlayCanvas = canvasGO.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = short.MaxValue; // por encima de todo

        // Imagen negra a pantalla completa
        var imgGO = new GameObject("Fade");
        imgGO.transform.SetParent(canvasGO.transform, false);
        fadeImage = imgGO.AddComponent<Image>();
        fadeImage.color = Color.black;

        // Stretch full screen
        var rt = fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // CanvasGroup para controlar alpha
        fadeGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();
        fadeGroup.interactable = false;
        fadeGroup.blocksRaycasts = false;

        // Arrancamos en negro (alpha=1). Tu SplashScreen llama a FadeOut al empezar.
        fadeGroup.alpha = 1f;
    }

    // Negro -> Transparente
    public void FadeOut(Action onComplete = null, float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeOutDuration;
        BuildOverlayIfNeeded();

        DOTween.Kill(fadeGroup);
        fadeGroup.DOFade(0f, duration).SetUpdate(true).OnComplete(() => onComplete?.Invoke());
    }

    // Transparente -> Negro
    public void FadeIn(Action onComplete = null, float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeInDuration;
        BuildOverlayIfNeeded();

        DOTween.Kill(fadeGroup);
        fadeGroup.DOFade(1f, duration).SetUpdate(true).OnComplete(() => onComplete?.Invoke());
    }

    // FadeIn -> LoadScene -> FadeOut
    public void FadeToScene(string sceneName, float fadeInDur = -1f, float fadeOutDur = -1f)
    {
        if (busy) return; // evita dobles disparos
        StartCoroutine(FadeToScene_Coroutine(sceneName, fadeInDur < 0 ? defaultFadeInDuration : fadeInDur,
                                                       fadeOutDur < 0 ? defaultFadeOutDuration : fadeOutDur));
    }

    private IEnumerator FadeToScene_Coroutine(string sceneName, float fadeInDur, float fadeOutDur)
    {
        busy = true;
        BuildOverlayIfNeeded();

        // A negro
        yield return fadeGroup.DOFade(1f, fadeInDur).SetUpdate(true).WaitForCompletion();

        // Carga
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!op.isDone) yield return null;

        // A transparente
        yield return fadeGroup.DOFade(0f, fadeOutDur).SetUpdate(true).WaitForCompletion();

        busy = false;
    }
}
