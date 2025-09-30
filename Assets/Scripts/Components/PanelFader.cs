/*
============================================================
PanelFader.cs — Fade in/out para paneles UI
------------------------------------------------------------
PROPÓSITO
- Entradas/salidas suaves con CanvasGroup para paneles.

USO
- Añadir a panel; invocar Show()/Hide() o animaciones puntuales.

DEPENDENCIAS
- CanvasGroup requerido (lo añade si falta).

MÉTODOS (COMPLETA AQUÍ)
- Show(float dur)/Hide(float dur): Interpola alpha y activa/desactiva.
- SetVisible(bool): Estado directo sin animación.
============================================================
*/

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class PanelFader : MonoBehaviour
{
    public float fadeDuration = 0.2f;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        // Asegúrate de que el panel esté oculto al inicio si no está activo
        if (!gameObject.activeSelf)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void FadeIn()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeCanvasGroup(1f, true));
    }

    public IEnumerator FadeOut()
    {
        StopAllCoroutines();
        yield return StartCoroutine(FadeCanvasGroup(0f, false));
        gameObject.SetActive(false);
    }

    private IEnumerator FadeCanvasGroup(float targetAlpha, bool setActiveAfter)
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        float startAlpha = canvasGroup.alpha;
        float time = 0f;
        while (time < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            time += Time.unscaledDeltaTime;
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = setActiveAfter && targetAlpha == 1f;
        canvasGroup.blocksRaycasts = setActiveAfter && targetAlpha == 1f;
    }

    // DEBUG: Para asegurarte que siempre está opaco al activar
    void OnEnable()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup.alpha < 1f) canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
}
