using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using ReinoOscuridad.UI.Common;

namespace ReinoOscuridad.Editor.Setup
{
    /// Crea Assets/Prefabs/UI/InputBlocker.prefab.
    /// Menu: Tools -> Reino Oscuridad -> 3. Setup InputBlocker Prefab
    public static class SetupInputBlockerPrefab
    {
        private const string PREFAB_PATH = "Assets/Prefabs/UI/InputBlocker.prefab";

        [MenuItem("Tools/Reino Oscuridad/3. Setup InputBlocker Prefab")]
        public static void Setup()
        {
            var root = new GameObject("InputBlocker");

            // Canvas propio — sortingOrder configurable en runtime via Show(sortOrder)
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // valor por defecto; UIManager lo cambia al llamar Show()

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight  = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            // Image transparente fullscreen — raycastTarget=true bloquea todos los eventos
            var img = root.AddComponent<Image>();
            img.color         = Color.clear;
            img.raycastTarget = true;

            var rt       = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Componente de control
            root.AddComponent<InputBlocker>();

            // Guardar prefab
            Directory.CreateDirectory("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out bool saved);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();

            Debug.Log(saved
                ? "[SetupInputBlocker] InputBlocker.prefab creado/actualizado correctamente"
                : "[SetupInputBlocker] ERROR al guardar InputBlocker.prefab");
        }
    }
}
