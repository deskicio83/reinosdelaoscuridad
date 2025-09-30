/*
============================================================
UIAnimator.cs — Animaciones UI comunes (DOTween)
------------------------------------------------------------
PROPÓSITO
- Aparición/desaparición de paneles y punch de botones con Ease adecuados.

MÉTODOS
- Appear(GameObject root, float duration=0.18f, float overshoot=1.04f):
  Añade CanvasGroup si falta, fade-in + scale con back.
- Disappear(GameObject root, float duration=0.14f):
  Scale a 0.96 + fade-out y desactiva root al terminar.
- Punch(Transform t, float scale=0.95f, float duration=0.08f):
  Pequeño rebote para clicks.

PRIVADO
- GetOrAddCanvasGroup(GameObject): helper.
============================================================
*/

using UnityEngine;
using DG.Tweening;

public static class UIAnimator
{
    // Aparece con fade + scale
    public static void Appear(GameObject root, float duration = 0.18f, float overshoot = 1.04f)
    {
        if (root == null) return;
        var cg = GetOrAddCanvasGroup(root);
        root.SetActive(true);
        cg.alpha = 0f;
        var rt = root.transform as RectTransform;
        if (rt != null)
        {
            rt.localScale = Vector3.one * 0.92f;
            rt.DOKill();
            rt.DOScale(overshoot, duration * 0.6f).SetEase(Ease.OutBack, 1.4f).OnComplete(() =>
            {
                rt.DOScale(1f, duration * 0.4f).SetEase(Ease.OutSine);
            });
        }
        cg.DOKill();
        cg.DOFade(1f, duration).SetEase(Ease.OutSine);
    }

    // Desaparece con fade + scale
    public static void Disappear(GameObject root, float duration = 0.14f)
    {
        if (root == null) return;
        var cg = GetOrAddCanvasGroup(root);
        var rt = root.transform as RectTransform;
        if (rt != null)
        {
            rt.DOKill();
            rt.DOScale(0.96f, duration).SetEase(Ease.InSine);
        }
        cg.DOKill();
        cg.DOFade(0f, duration).SetEase(Ease.InSine).OnComplete(() =>
        {
            root.SetActive(false);
        });
    }

    // Pequeño punch al pulsar botones
    public static void Punch(Transform t, float scale = 0.95f, float duration = 0.08f)
    {
        if (t == null) return;
        t.DOKill();
        var s0 = t.localScale;
        t.localScale = s0;
        t.DOScale(s0 * scale, duration).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            t.DOScale(s0, duration).SetEase(Ease.OutQuad);
        });
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }
}
