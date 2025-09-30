using System;
using System.Reflection;
using UnityEngine;

// Adaptador para distintas variantes de LoadingOverlayController.
// Llama por reflexión a lo que exista (instancia o estático) y hace fallback seguro.
public static class LoadingOverlayCompat
{
    static readonly MethodInfo miShowRandom;
    static readonly MethodInfo miShow;
    static readonly MethodInfo miSetTip;
    static readonly MethodInfo miFadeToBlackAndHide;

    static readonly MethodInfo miSetInst, miStepInst;
    static readonly MethodInfo miSetStatic, miStepStatic;

    static LoadingOverlayCompat()
    {
        var t = typeof(LoadingOverlayController);

        // Instancia
        miShowRandom        = t.GetMethod("ShowRandomBackground", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        miShow              = t.GetMethod("Show", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(string) }, null)
                              ?? t.GetMethod("Show", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        miSetTip            = t.GetMethod("SetTip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        miFadeToBlackAndHide= t.GetMethod("FadeToBlackAndHide", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        miSetInst           = t.GetMethod("Set",  BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float), typeof(string) }, null)
                              ?? t.GetMethod("Set",  BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float) }, null);
        miStepInst          = t.GetMethod("Step", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float), typeof(string) }, null)
                              ?? t.GetMethod("Step", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float) }, null);

        // Estáticos
        miSetStatic         = t.GetMethod("Set",  BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float), typeof(string) }, null)
                              ?? t.GetMethod("Set",  BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float) }, null);
        miStepStatic        = t.GetMethod("Step", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float), typeof(string) }, null)
                              ?? t.GetMethod("Step", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(float) }, null);
    }

    public static void ShowRandomBackground(LoadingOverlayController o)
    {
        if (o == null) return;
        miShowRandom?.Invoke(o, null);
    }

    public static void Show(LoadingOverlayController o, string tip = null)
    {
        if (o == null) return;
        if (miShow != null)
        {
            var ps = miShow.GetParameters();
            if (ps.Length == 0) miShow.Invoke(o, null);
            else                miShow.Invoke(o, new object[] { tip });
        }
        else
        {
            Debug.Log("[LoadingOverlayCompat] Show() no disponible (se continúa sin mostrar overlay).");
        }
    }

    public static void Set(LoadingOverlayController o, float p, string tip = null)
    {
        if (o == null) return;

        if (miSetInst != null)
        {
            var ps = miSetInst.GetParameters();
            miSetInst.Invoke(o, ps.Length == 1 ? new object[] { p } : new object[] { p, tip });
        }
        else if (miSetStatic != null)
        {
            var ps = miSetStatic.GetParameters();
            miSetStatic.Invoke(null, ps.Length == 1 ? new object[] { p } : new object[] { p, tip });
        }
    }

    public static void Step(LoadingOverlayController o, float delta, string tip = null)
    {
        if (o == null) return;

        if (miStepInst != null)
        {
            var ps = miStepInst.GetParameters();
            miStepInst.Invoke(o, ps.Length == 1 ? new object[] { delta } : new object[] { delta, tip });
        }
        else if (miStepStatic != null)
        {
            var ps = miStepStatic.GetParameters();
            miStepStatic.Invoke(null, ps.Length == 1 ? new object[] { delta } : new object[] { delta, tip });
        }
    }

    public static void SetTip(LoadingOverlayController o, string tip)
    {
        if (o == null) return;
        if (miSetTip != null) miSetTip.Invoke(o, new object[] { tip });
        // Si no existe, simplemente no actualizamos tip (evita compilar contra un método inexistente).
    }

    public static void FadeToBlackAndHide(LoadingOverlayController o)
    {
        if (o == null) return;
        if (miFadeToBlackAndHide != null) { miFadeToBlackAndHide.Invoke(o, null); return; }

        // Fallbacks comunes
        var t = typeof(LoadingOverlayController);
        var miHide = t.GetMethod("Hide", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (miHide != null) { miHide.Invoke(o, null); return; }

        var comp = o as Component;
        if (comp && comp.gameObject) comp.gameObject.SetActive(false);
    }
}
