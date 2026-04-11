using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ReinoOscuridad.UI.MainMenu;

namespace ReinoOscuridad.Tests
{
    /// Test de validacion S10c — MainMenuScene layout Bastion Maldito.
    /// Adjuntar a un GameObject vacio en MainMenuScene y ejecutar en Play Mode.
    /// Se autodestruye al terminar. No usar en produccion.
    public class S10c_Test : MonoBehaviour
    {
        private int _pass;
        private int _fail;

        private IEnumerator Start()
        {
            // Esperar un frame para que Canvas y RectTransforms se inicialicen
            yield return null;

            Debug.Log("=== S10c_Test INICIO ===");

            Check1_MainMenuCanvasExiste();
            Check2_ScrollRectBidireccional();
            Check3_ViewportTieneRectMask2D();
            Check4_ContentTiene11Edificios();
            Check5_ContentTieneRaycastTarget();
            Check6_AccesosRapidosTiene6Botones();
            Check7_AbanicoEsHijoDelCanvas();
            Check8_AbanicoInactivoCon3SubBotones();

            Debug.Log($"=== S10c_Test RESULTADO FINAL: {_pass} PASS / {_fail} FAIL ===");
            if (_fail == 0)
                Debug.Log("=== TODOS PASS ✓ ===");

            Destroy(gameObject);
        }

        // ── Check 1 ───────────────────────────────────────────────────────────
        // MainMenuCanvas existe en la Scene y tiene Canvas + CanvasScaler
        void Check1_MainMenuCanvasExiste()
        {
            var canvasGO = GameObject.Find("MainMenuCanvas");
            if (canvasGO == null)          { Fail(1, "MainMenuCanvas no encontrado en la Scene"); return; }
            if (!canvasGO.GetComponent<Canvas>())      { Fail(1, "MainMenuCanvas sin componente Canvas"); return; }
            if (!canvasGO.GetComponent<CanvasScaler>()) { Fail(1, "MainMenuCanvas sin CanvasScaler"); return; }
            Pass(1, "MainMenuCanvas existe con Canvas + CanvasScaler");
        }

        // ── Check 2 ───────────────────────────────────────────────────────────
        // ZonaEdificios tiene ScrollRect con horizontal=true y vertical=true
        void Check2_ScrollRectBidireccional()
        {
            var zonaGO = GameObject.Find("ZonaEdificios");
            if (zonaGO == null) { Fail(2, "ZonaEdificios no encontrado"); return; }

            var sr = zonaGO.GetComponent<ScrollRect>();
            if (sr == null)          { Fail(2, "ZonaEdificios sin ScrollRect"); return; }
            if (!sr.horizontal)      { Fail(2, "ScrollRect.horizontal = false"); return; }
            if (!sr.vertical)        { Fail(2, "ScrollRect.vertical = false"); return; }
            if (sr.content == null)  { Fail(2, "ScrollRect.content no asignado"); return; }
            if (sr.viewport == null) { Fail(2, "ScrollRect.viewport no asignado"); return; }
            Pass(2, $"ScrollRect bidireccional  viewport={sr.viewport.name}  content={sr.content.name}");
        }

        // ── Check 3 ───────────────────────────────────────────────────────────
        // El Viewport tiene RectMask2D (no Mask)
        void Check3_ViewportTieneRectMask2D()
        {
            var viewportGO = GameObject.Find("Viewport");
            if (viewportGO == null) { Fail(3, "Viewport no encontrado"); return; }

            var rm2d = viewportGO.GetComponent<RectMask2D>();
            if (rm2d == null) { Fail(3, "Viewport no tiene RectMask2D"); return; }

            var oldMask = viewportGO.GetComponent<Mask>();
            if (oldMask != null) { Fail(3, "Viewport todavia tiene Mask (deberia ser solo RectMask2D)"); return; }

            Pass(3, "Viewport tiene RectMask2D y no tiene Mask obsoleto");
        }

        // ── Check 4 ───────────────────────────────────────────────────────────
        // ContentEdificios tiene exactamente 11 hijos (edificios)
        void Check4_ContentTiene11Edificios()
        {
            var contentGO = GameObject.Find("ContentEdificios");
            if (contentGO == null) { Fail(4, "ContentEdificios no encontrado"); return; }

            int count = contentGO.transform.childCount;
            if (count != 11)
            {
                Fail(4, $"ContentEdificios tiene {count} hijos, se esperaban 11");
                return;
            }
            Pass(4, "ContentEdificios tiene 11 edificios");
        }

        // ── Check 5 ───────────────────────────────────────────────────────────
        // ContentEdificios tiene Image con raycastTarget=true (drag en espacio vacio)
        void Check5_ContentTieneRaycastTarget()
        {
            var contentGO = GameObject.Find("ContentEdificios");
            if (contentGO == null) { Fail(5, "ContentEdificios no encontrado"); return; }

            var img = contentGO.GetComponent<Image>();
            if (img == null)              { Fail(5, "ContentEdificios sin Image"); return; }
            if (!img.raycastTarget)       { Fail(5, "ContentEdificios Image.raycastTarget = false"); return; }

            Pass(5, "ContentEdificios tiene Image raycastTarget=true (drag en vacio habilitado)");
        }

        // ── Check 6 ───────────────────────────────────────────────────────────
        // ZonaAccesosRapidos tiene exactamente 6 botones hijos directos
        void Check6_AccesosRapidosTiene6Botones()
        {
            var zonaGO = GameObject.Find("ZonaAccesosRapidos");
            if (zonaGO == null) { Fail(6, "ZonaAccesosRapidos no encontrado"); return; }

            int btnCount = 0;
            for (int i = 0; i < zonaGO.transform.childCount; i++)
            {
                var child = zonaGO.transform.GetChild(i);
                if (child.GetComponent<Button>() != null)
                    btnCount++;
            }

            if (btnCount != 6) { Fail(6, $"ZonaAccesosRapidos tiene {btnCount} botones directos, se esperaban 6"); return; }

            // Verificar que Triloguzano es el ultimo (indice 5)
            var lastChild = zonaGO.transform.GetChild(zonaGO.transform.childCount - 1);
            if (lastChild.name != "Btn_Triloguzano")
            {
                Fail(6, $"El ultimo boton no es Btn_Triloguzano, es {lastChild.name}");
                return;
            }
            Pass(6, "ZonaAccesosRapidos tiene 6 botones, Btn_Triloguzano en el extremo derecho");
        }

        // ── Check 7 ───────────────────────────────────────────────────────────
        // SubIconosAbanico es hijo directo de MainMenuCanvas (no de ZonaAccesosRapidos)
        void Check7_AbanicoEsHijoDelCanvas()
        {
            var abanicoGO = GameObject.Find("SubIconosAbanico");
            if (abanicoGO == null)
            {
                // Puede estar inactivo — buscar en todos los GOs incluyendo inactivos
                abanicoGO = FindInactive("SubIconosAbanico");
            }
            if (abanicoGO == null) { Fail(7, "SubIconosAbanico no encontrado (ni activo ni inactivo)"); return; }

            var parent = abanicoGO.transform.parent;
            if (parent == null)            { Fail(7, "SubIconosAbanico no tiene parent"); return; }
            if (parent.name != "MainMenuCanvas")
            {
                Fail(7, $"SubIconosAbanico es hijo de '{parent.name}', se esperaba 'MainMenuCanvas'");
                return;
            }
            Pass(7, "SubIconosAbanico es hijo directo de MainMenuCanvas");
        }

        // ── Check 8 ───────────────────────────────────────────────────────────
        // SubIconosAbanico esta inactivo por defecto y tiene 3 sub-botones
        void Check8_AbanicoInactivoCon3SubBotones()
        {
            var abanicoGO = FindInactive("SubIconosAbanico");
            if (abanicoGO == null) { Fail(8, "SubIconosAbanico no encontrado"); return; }

            if (abanicoGO.activeSelf) { Fail(8, "SubIconosAbanico deberia estar inactivo al inicio"); return; }

            int subBtns = 0;
            for (int i = 0; i < abanicoGO.transform.childCount; i++)
            {
                if (abanicoGO.transform.GetChild(i).GetComponent<Button>() != null)
                    subBtns++;
            }

            if (subBtns != 3) { Fail(8, $"SubIconosAbanico tiene {subBtns} sub-botones, se esperaban 3"); return; }

            Pass(8, "SubIconosAbanico inactivo por defecto con 3 sub-botones (Torre/Boss/Mazm)");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        void Pass(int n, string msg)
        {
            _pass++;
            Debug.Log($"[S10c Check {n}] PASS — {msg}");
        }

        void Fail(int n, string msg)
        {
            _fail++;
            Debug.LogError($"[S10c Check {n}] FAIL — {msg}");
        }

        static GameObject FindInactive(string name)
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.scene.isLoaded && go.name == name)
                    return go;
            }
            return null;
        }
    }
}
