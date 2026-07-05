#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public static class ImportHeroArt
{
    private static readonly string[] FOLDERS =
    {
        "Assets/Addressables/Heroes/Avatar",
        "Assets/Addressables/Heroes/AvatarAwaken",
        "Assets/Addressables/Heroes/Full",
        "Assets/Addressables/Heroes/FullAwaken",
        "Assets/Addressables/Heroes/BorderGear",
        "Assets/Addressables/SkillIcon",
        "Assets/Addressables/Art/HeroScene/Elemento",
        "Assets/Addressables/Art/HeroScene/Clase",
        "Assets/Addressables/Art/HeroScene/Faccion",
        "Assets/Addressables/Art/HeroScene/TabMenuBar",
    };

    [MenuItem("Tools/Reino Oscuridad/10. Import Hero Art (Addressables)")]
    public static void Run()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[ImportHeroArt] No hay AddressableAssetSettings en el proyecto.");
            return;
        }

        var group = settings.FindGroup("HeroArt");
        if (group == null)
        {
            group = settings.CreateGroup("HeroArt", false, false, true, null,
                typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        }

        int total = 0;
        int marcados = 0;

        foreach (var folder in FOLDERS)
        {
            if (!Directory.Exists(folder)) continue;

            var files = Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string assetPath = file.Replace("\\", "/");
                total++;

                if (AssetImporter.GetAtPath(assetPath) is TextureImporter importer
                    && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.SaveAndReimport();
                }

                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid)) continue;

                var entry = settings.CreateOrMoveEntry(guid, group, false, false);
                entry.address = assetPath;
                marcados++;
            }
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, null, true);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ImportHeroArt] {marcados}/{total} sprites marcados como Addressable en el grupo 'HeroArt'.");
    }
}
#endif
