#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// Editor Script — genera PNG placeholder para uso durante el desarrollo.
/// Los placeholders se cargan vía Resources.Load (excepción temporal a la regla de Addressables).
/// Menú: Tools → Reino Oscuridad → 0. Generate Placeholder Assets
public static class GeneratePlaceholderAssets
{
    private const string OUTPUT_SUBPATH = "Resources/Placeholders";

    [MenuItem("Tools/Reino Oscuridad/0. Generate Placeholder Assets")]
    public static void Run()
    {
        string fullDir = Path.Combine(Application.dataPath, OUTPUT_SUBPATH);
        Directory.CreateDirectory(fullDir);

        // Retratos de héroe por elemento
        foreach (var (name, color) in ELEMENTOS)
            GeneratePng(fullDir, $"hero_{name}", 128, 128, color);

        // Sprites de enemigo por tipo
        foreach (var (name, color) in TIPOS_ENEMIGO)
            GeneratePng(fullDir, $"enemy_{name}", 128, 128, color);

        // Estados de nodo del mapa
        foreach (var (name, color) in ESTADOS_NODO)
            GeneratePng(fullDir, $"node_{name}", 128, 128, color);

        // Fondos de scene
        foreach (var (name, color) in FONDOS)
            GeneratePng(fullDir, $"bg_{name}", 256, 128, color);

        // Genérico fallback
        GeneratePng(fullDir, "placeholder", 128, 128, new Color(0.5f, 0.5f, 0.5f));

        AssetDatabase.Refresh();
        Debug.Log("[GeneratePlaceholders] Placeholders generados en Assets/Resources/Placeholders/");
    }

    // ── Catálogos de colores ─────────────────────────────────────────────────

    private static readonly (string, Color)[] ELEMENTOS =
    {
        ( "fuego",      new Color(1.00f, 0.20f, 0.00f) ),
        ( "agua",       new Color(0.10f, 0.40f, 1.00f) ),
        ( "tierra",     new Color(0.50f, 0.30f, 0.10f) ),
        ( "naturaleza", new Color(0.10f, 0.60f, 0.10f) ),
        ( "luz",        new Color(1.00f, 1.00f, 0.20f) ),
        ( "oscuridad",  new Color(0.15f, 0.00f, 0.25f) ),
        ( "rayo",       new Color(0.20f, 0.80f, 1.00f) ),
        ( "hielo",      new Color(0.60f, 0.85f, 1.00f) ),
    };

    private static readonly (string, Color)[] TIPOS_ENEMIGO =
    {
        ( "humanoide", new Color(0.70f, 0.60f, 0.50f) ),
        ( "bestia",    new Color(0.60f, 0.40f, 0.20f) ),
        ( "no_muerto", new Color(0.40f, 0.40f, 0.45f) ),
        ( "demonio",   new Color(0.50f, 0.00f, 0.00f) ),
        ( "dragon",    new Color(0.20f, 0.50f, 0.20f) ),
        ( "elemental", new Color(0.00f, 0.60f, 0.60f) ),
        ( "generico",  new Color(0.45f, 0.30f, 0.30f) ),
    };

    private static readonly (string, Color)[] ESTADOS_NODO =
    {
        ( "bloqueado",  new Color(0.25f, 0.25f, 0.25f) ),
        ( "disponible", new Color(0.85f, 0.85f, 0.85f) ),
        ( "jefe",       new Color(1.00f, 0.85f, 0.00f) ),
        ( "completado", new Color(0.20f, 0.70f, 0.25f) ),
    };

    private static readonly (string, Color)[] FONDOS =
    {
        ( "campaign", new Color(0.06f, 0.04f, 0.10f) ),
        ( "combat",   new Color(0.10f, 0.04f, 0.04f) ),
        ( "mainmenu", new Color(0.04f, 0.04f, 0.08f) ),
        ( "boot",     new Color(0.02f, 0.02f, 0.04f) ),
    };

    // ── Generación ───────────────────────────────────────────────────────────

    private static void GeneratePng(string dir, string name, int w, int h, Color color)
    {
        var tex    = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color[w * h];

        for (int i = 0; i < pixels.Length; i++)
        {
            int x = i % w;
            int y = i / w;
            bool border = x < 4 || x >= w - 4 || y < 4 || y >= h - 4;
            pixels[i] = border ? new Color(0.8f, 0.8f, 0.8f, 1f) : color;
        }

        tex.SetPixels(pixels);
        tex.Apply();

        File.WriteAllBytes(Path.Combine(dir, $"{name}.png"), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }
}
#endif
