#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// Script de build Android para CLI / batch mode.
/// Uso: Unity.exe -batchmode -projectPath "..." -executeMethod BuildAndroid.Build -quit
public static class BuildAndroid
{
    private const string OUTPUT_PATH = "Builds/Android/ReinosOscuridad_MVP.apk";

    [MenuItem("Tools/Reino Oscuridad/Build Android MVP")]
    public static void Build()
    {
        // Asegurar directorio de salida
        string dir = Path.GetDirectoryName(OUTPUT_PATH);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string[] scenes =
        {
            "Assets/Scenes/BootScene.unity",
            "Assets/Scenes/MainMenuScene.unity",
            "Assets/Scenes/CombatScene.unity",
            "Assets/Scenes/CampaignScene.unity",
        };

        var options = new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = OUTPUT_PATH,
            target           = BuildTarget.Android,
            options          = BuildOptions.Development,
        };

        BuildReport  report  = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            long   sizeKB = (long)(summary.totalSize / 1024);
            Debug.Log($"[MVP] Build Android generada: {OUTPUT_PATH} ({sizeKB:N0} KB)");
        }
        else
        {
            Debug.LogError($"[MVP] Build FALLIDA — resultado: {summary.result} — errores: {summary.totalErrors}");
        }
    }
}
#endif
