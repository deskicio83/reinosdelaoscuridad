using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ReinoOscuridad.UI.Common;
using static EditorUIBuilder;

namespace ReinoOscuridad.Editor.Setup
{
    /// Crea Assets/Prefabs/UI/LoadingScreen.prefab.
    /// Menu: Tools -> Reino Oscuridad -> 4. Setup LoadingScreen Prefab
    public static class SetupLoadingScreenPrefab
    {
        private const string PREFAB_PATH = "Assets/Prefabs/UI/LoadingScreen.prefab";

        [MenuItem("Tools/Reino Oscuridad/4. Setup LoadingScreen Prefab")]
        public static void Setup()
        {
            // ── Raiz: Canvas propio DontDestroyOnLoad ─────────────────────────
            var root = new GameObject("LoadingScreen");

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 998; // por debajo del FadeCanvas del UIManager (999)

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight  = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            var loadingScreen = root.AddComponent<LoadingScreen>();

            // ── Fondo oscuro fullscreen ────────────────────────────────────────
            var fondo = Child(root, "Fondo");
            var fondoImg = fondo.AddComponent<Image>();
            fondoImg.color = new Color(0.039f, 0.039f, 0.059f, 240f / 255f);
            Anch(fondo, 0f, 0f, 1f, 1f);

            // ── PanelCarrusel — zona central para imagen de carrusel ───────────
            var panelCarrusel = Child(root, "PanelCarrusel");
            Anch(panelCarrusel, 0.15f, 0.25f, 0.85f, 0.80f);

            var imagenCarruselGO = Child(panelCarrusel, "ImagenCarrusel");
            var imagenCarruselImg = imagenCarruselGO.AddComponent<Image>();
            imagenCarruselImg.color = Hex("#1A1A2E"); // placeholder hasta que se asignen sprites
            imagenCarruselImg.preserveAspect = true;
            Anch(imagenCarruselGO, 0f, 0f, 1f, 1f);

            // ── BarraProgreso ─────────────────────────────────────────────────
            var barraProgreso = Child(root, "BarraProgreso");
            Anch(barraProgreso, 0.15f, 0.18f, 0.85f, 0.23f);

            var barraFondoGO = Child(barraProgreso, "BarraFondo");
            barraFondoGO.AddComponent<Image>().color = Hex("#1A1A2E");
            Anch(barraFondoGO, 0f, 0f, 1f, 1f);

            var barraRellenoGO = Child(barraProgreso, "BarraRelleno");
            barraRellenoGO.AddComponent<Image>().color = Hex("#7C3AED");
            // anchorMax.x = 0 al inicio (barra vacia); se anima via SetProgress
            Anch(barraRellenoGO, 0f, 0f, 0f, 1f);
            var barraRellenoRT = barraRellenoGO.GetComponent<RectTransform>();

            // ── TextoFrase ────────────────────────────────────────────────────
            var textoFraseGO = Child(root, "TextoFrase");
            Anch(textoFraseGO, 0.10f, 0.08f, 0.90f, 0.17f);
            var textoFraseTMP = textoFraseGO.AddComponent<TextMeshProUGUI>();
            textoFraseTMP.text      = "Preparando las mazmorras...";
            textoFraseTMP.fontSize  = 14;
            textoFraseTMP.fontStyle = FontStyles.Italic;
            textoFraseTMP.color     = Hex("#A855F7");
            textoFraseTMP.alignment = TextAlignmentOptions.Center;

            // ── Asignar referencias via SerializedObject ──────────────────────
            var so = new SerializedObject(loadingScreen);
            so.FindProperty("_imagenCarrusel").objectReferenceValue = imagenCarruselImg;
            so.FindProperty("_barraRelleno").objectReferenceValue   = barraRellenoRT;
            so.FindProperty("_textoFrase").objectReferenceValue     = textoFraseTMP;
            so.ApplyModifiedProperties();

            // ── Guardar prefab ────────────────────────────────────────────────
            Directory.CreateDirectory("Assets/Prefabs/UI");
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out bool saved);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();

            Debug.Log(saved
                ? "[SetupLoadingScreen] LoadingScreen.prefab creado/actualizado correctamente"
                : "[SetupLoadingScreen] ERROR al guardar LoadingScreen.prefab");
        }

    }
}
