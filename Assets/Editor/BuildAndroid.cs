#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

/// Script de build Android para CLI / batch mode.
/// Uso: Unity.exe -batchmode -projectPath "..." -executeMethod BuildAndroid.Build -quit
/// Release: -executeMethod BuildAndroid.BuildRelease
public static class BuildAndroid
{
    private const string OUTPUT_PATH         = "Builds/Android/ReinosOscuridad_MVP.apk";
    private const string OUTPUT_PATH_RELEASE = "Builds/Android/ReinosOscuridad_v010.apk";

    private static readonly string[] SCENES =
    {
        "Assets/Scenes/BootScene.unity",
        "Assets/Scenes/MainMenuScene.unity",
        "Assets/Scenes/CombatScene.unity",
        "Assets/Scenes/CampaignScene.unity",
    };

    [MenuItem("Tools/Reino Oscuridad/Build Android MVP")]
    public static void Build()
    {
        string dir = Path.GetDirectoryName(OUTPUT_PATH);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var options = new BuildPlayerOptions
        {
            scenes           = SCENES,
            locationPathName = OUTPUT_PATH,
            target           = BuildTarget.Android,
            options          = BuildOptions.Development,
        };

        RunBuild(options, "MVP");
    }

    [MenuItem("Tools/Reino Oscuridad/Build Android Release")]
    public static void BuildRelease()
    {
        string dir = Path.GetDirectoryName(OUTPUT_PATH_RELEASE);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        // Player Settings — release configuration
        PlayerSettings.bundleVersion                         = "0.1.0";
        PlayerSettings.Android.bundleVersionCode             = 1;
        PlayerSettings.Android.targetSdkVersion              = (AndroidSdkVersions)34;
        PlayerSettings.Android.minSdkVersion                 = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures           = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Minimal);
        PlayerSettings.stripEngineCode                       = true;
        PlayerSettings.defaultInterfaceOrientation           = UIOrientation.LandscapeLeft;

        // Graphics APIs: OpenGLES3 + OpenGLES2 fallback
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
            new[] { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLES2 });

        var options = new BuildPlayerOptions
        {
            scenes           = SCENES,
            locationPathName = OUTPUT_PATH_RELEASE,
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
        };

        RunBuild(options, "Release v0.1.0");
    }

    private static void RunBuild(BuildPlayerOptions options, string label)
    {
        BuildReport  report  = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            long sizeMB = (long)(summary.totalSize / (1024 * 1024));
            Debug.Log($"[{label}] Build Android generada: {options.locationPathName} ({sizeMB} MB)");
        }
        else
        {
            Debug.LogError($"[{label}] Build FALLIDA — resultado: {summary.result} — errores: {summary.totalErrors}");
        }
    }
}
#endif
