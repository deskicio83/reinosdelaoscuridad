/*
============================================================
WorldUICameraAligner.cs — Alineador de cámara/UI de mundo
------------------------------------------------------------
PROPÓSITO
- Mantener alineados elementos de UI en World Space con la cámara principal
  (p.ej., fondos o HUDs anclados en “mundo”).

USO
- Añadir como componente a un GameObject padre de elementos UI/World.
- Útil cuando la cámara ortográfica/perspectiva mueve/zoomea.

RESPONSABILIDADES
- Escuchar cambios de tamaño y/o posición de cámara.
- Reposicionar/reescalar contenedores mundiales para mantener el encuadre.

DEPENDENCIAS
- Camera.main; posibles managers de escena si sincroniza en Start.

REFERENCIAS (INSPECTOR)
- Transforms o RectTransforms a alinear (si aplica).

EVENTOS/SEÑALES
- No emite eventos.

MÉTODOS (COMPLETA AQUÍ CON TUS FIRMAS REALES)
- Awake/OnEnable/Start: Inicializa referencias a cámara y nodos.
- LateUpdate/OnRectTransformDimensionsChange: Reaplica alineación tras cambios.
- SetWorldBounds(...): (si existe) Ajusta bounds por tamaño de fondo.
- ApplyAlignment(): Recalcula posición/escala respecto a cámara.

NOTAS
- Si el fondo es SpriteRenderer, puede requerir lectura de bounds tras 1 frame.
============================================================
*/

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class WorldUICameraAligner : MonoBehaviour
{
    [MenuItem("Tools/Alinear Background/Cámara/Canvas (1920x1080)")]
    public static void AlignWorldToUI()
    {
        // Busca los objetos necesarios
        var cam = Camera.main;
        var backgroundGO = GameObject.Find("Background");
        var canvas = GameObject.FindFirstObjectByType<Canvas>();

        if (cam == null || backgroundGO == null || canvas == null)
        {
            Debug.LogError("❌ Asegúrate de tener Camera (Main), Background y Canvas en la escena.");
            return;
        }

        // 1. Ajustar cámara
        cam.orthographic = true;
        float targetWidth = 1920f;
        float targetHeight = 1080f;
        float aspect = targetWidth / targetHeight;
        cam.orthographicSize = targetHeight / 2f;
        cam.transform.position = new Vector3(targetWidth / 2f, targetHeight / 2f, -10);

        // 2. Ajustar Background
        var sr = backgroundGO.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("❌ El Background no tiene SpriteRenderer.");
            return;
        }

        // Cambia Pixels Per Unit para que el fondo ocupe exactamente 1920x1080 unidades
        string path = AssetDatabase.GetAssetPath(sr.sprite);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.spritePixelsPerUnit = 1;
            importer.SaveAndReimport();
        }

        // Asegúrate que esté en (0,0,0) y scale (1,1,1)
        backgroundGO.transform.position = Vector3.zero;
        backgroundGO.transform.localScale = Vector3.one;

        // 3. Canvas Reference Resolution
        var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (scaler != null)
        {
            scaler.referenceResolution = new Vector2(targetWidth, targetHeight);
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        Debug.Log("✅ Cámara, fondo y Canvas alineados perfectamente en 1920x1080 unidades. ¡Los iconos de UI y el fondo coincidirán 1:1!");
    }
}
#endif
