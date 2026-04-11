using UnityEngine;
using UnityEngine.UI;

namespace ReinoOscuridad.UI.MainMenu
{
    /// Componente temporal de diagnostico — adjuntar al MainMenuCanvas en MainMenuScene.
    /// Imprime en Consola las medidas reales de cada RectTransform clave.
    /// Eliminar o desactivar antes de produccion.
    public class MainMenuDebug : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("=== [MainMenuDebug] START ===");
            LogCanvas();
            LogRT("ZonaEdificios",   "ZonaEdificios");
            LogRT("Viewport",        "ZonaEdificios/Viewport");
            LogRT("ContentEdificios","ZonaEdificios/Viewport/ContentEdificios");
            LogScrollRect();
            LogFirstBuilding();
            LogRT("ZonaAccesosRapidos", "ZonaAccesosRapidos");
            Debug.Log("=== [MainMenuDebug] END ===");
        }

        void LogCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null) { Debug.LogWarning("[Debug] Canvas no encontrado en este GO"); return; }
            var rt = canvas.GetComponent<RectTransform>();
            Debug.Log($"[Debug] Canvas size: {rt.rect.width}x{rt.rect.height}  scaleFactor:{canvas.scaleFactor}");
        }

        void LogRT(string label, string path)
        {
            var go = FindByPath(path);
            if (go == null) { Debug.LogWarning($"[Debug] GO no encontrado: {path}"); return; }

            var rt = go.GetComponent<RectTransform>();
            if (rt == null) { Debug.LogWarning($"[Debug] Sin RectTransform: {path}"); return; }

            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float w = Vector3.Distance(corners[0], corners[3]);
            float h = Vector3.Distance(corners[0], corners[1]);

            var img = go.GetComponent<Image>();
            string imgInfo = img != null
                ? $"Image color={img.color}  sprite={(img.sprite != null ? img.sprite.name : "null")}  enabled={img.enabled}"
                : "no Image";

            Debug.Log($"[Debug] {label}: rect={rt.rect}  " +
                      $"anchorMin={rt.anchorMin}  anchorMax={rt.anchorMax}  " +
                      $"anchoredPos={rt.anchoredPosition}  sizeDelta={rt.sizeDelta}  " +
                      $"worldSize={w:F1}x{h:F1}  " +
                      $"worldCorner[0]={corners[0]:F1}  " +
                      $"active={go.activeInHierarchy}  " +
                      imgInfo);
        }

        void LogScrollRect()
        {
            var go = FindByPath("ZonaEdificios");
            if (go == null) return;
            var sr = go.GetComponent<ScrollRect>();
            if (sr == null) { Debug.LogWarning("[Debug] ScrollRect no encontrado en ZonaEdificios"); return; }
            Debug.Log($"[Debug] ScrollRect: " +
                      $"horizontal={sr.horizontal}  vertical={sr.vertical}  " +
                      $"viewport={(sr.viewport != null ? sr.viewport.name : "null")}  " +
                      $"content={(sr.content != null ? sr.content.name : "null")}  " +
                      $"normalizedPos={sr.normalizedPosition}");
        }

        void LogFirstBuilding()
        {
            var contentGO = FindByPath("ZonaEdificios/Viewport/ContentEdificios");
            if (contentGO == null) return;

            int count = contentGO.transform.childCount;
            Debug.Log($"[Debug] ContentEdificios tiene {count} hijos");

            if (count == 0) return;

            var first = contentGO.transform.GetChild(0).gameObject;
            var rt    = first.GetComponent<RectTransform>();
            var img   = first.GetComponent<Image>();
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float w = Vector3.Distance(corners[0], corners[3]);
            float h = Vector3.Distance(corners[0], corners[1]);

            Debug.Log($"[Debug] Primer edificio '{first.name}': " +
                      $"anchoredPos={rt.anchoredPosition}  sizeDelta={rt.sizeDelta}  " +
                      $"worldSize={w:F1}x{h:F1}  " +
                      $"worldCorner[0]={corners[0]:F1}  " +
                      $"active={first.activeInHierarchy}  " +
                      $"Image color={(img != null ? img.color.ToString() : "null")}  " +
                      $"sprite={(img != null && img.sprite != null ? img.sprite.name : "null")}");
        }

        // Busca un GO por path relativo al Canvas raiz (este GO)
        GameObject FindByPath(string path)
        {
            var parts = path.Split('/');
            Transform current = transform;
            foreach (var part in parts)
            {
                bool found = false;
                for (int i = 0; i < current.childCount; i++)
                {
                    if (current.GetChild(i).name == part)
                    {
                        current = current.GetChild(i);
                        found = true;
                        break;
                    }
                }
                if (!found) return null;
            }
            return current.gameObject;
        }
    }
}
