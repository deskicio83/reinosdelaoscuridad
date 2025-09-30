/*
============================================================
UIButtonClickAnimation.cs — Animación de “press” en botones
------------------------------------------------------------
PROPÓSITO
- Reducir escala al pulsar y restaurar al soltar/salir (actualización independiente de TimeScale).

INTERFACES
- IPointerDownHandler, IPointerUpHandler, IPointerExitHandler.

MÉTODOS
- Awake(): guarda escala original.
- OnPointerDown(...): escala a `scaleDown` con Ease.OutQuad.
- OnPointerUp(...): vuelve a escala original.
- OnPointerExit(...): si sale con botón abajo, restaura escala.
- OnDisable(): limpia tween y asegura escala original.
============================================================
*/

using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UIButtonClickAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float scaleDown = 0.9f;
    [SerializeField] private float duration = 0.1f;

    private Vector3 originalScale;
    private Tween currentTween;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale * scaleDown, duration)
                                .SetEase(Ease.OutQuad)
                                .SetUpdate(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale, duration)
                                .SetEase(Ease.OutQuad)
                                .SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Si sale fuera con el botón aún abajo, devuelve la escala
        if (transform.localScale.x < originalScale.x - 0.001f)
        {
            currentTween?.Kill();
            currentTween = transform.DOScale(originalScale, duration)
                                    .SetEase(Ease.OutQuad)
                                    .SetUpdate(true);
        }
    }

    private void OnDisable()
    {
        currentTween?.Kill();
        transform.localScale = originalScale;
    }
}
