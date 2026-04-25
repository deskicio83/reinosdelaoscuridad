using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ReinoOscuridad.UI.Combat
{
    /// Texto de daño flotante — se instancia como hijo del Canvas,
    /// flota 80px hacia arriba en 1 segundo y se desvanece en los últimos 0.3s.
    public class FloatingDamageText : MonoBehaviour
    {
        /// Crea y anima un texto de daño flotante sobre el Canvas.
        /// <param name="canvas">Canvas raíz de CombatScene.</param>
        /// <param name="screenPos">Posición en pantalla donde aparece.</param>
        /// <param name="daño">Valor numérico a mostrar.</param>
        /// <param name="crit">True si fue golpe crítico (28 px, dorado, "!").</param>
        /// <param name="ventaja">True si hubo ventaja elemental (24 px, rojo).</param>
        public static void Spawn(Canvas canvas, Vector2 screenPos, int daño, bool crit, bool ventaja)
        {
            if (canvas == null) return;

            var go = new GameObject("FloatingDamage");
            go.transform.SetParent(canvas.transform, false);
            go.SetActive(true); // re-activar por si el canvas padre está inactivo

            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(140f, 50f);
            rt.anchoredPosition = ScreenToCanvasLocal(canvas, screenPos);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            if (crit)
            {
                tmp.fontSize  = 28f;
                tmp.color     = new Color(0.980f, 0.800f, 0.082f); // #FACC15
                tmp.text      = $"{daño}";
                tmp.fontStyle = FontStyles.Bold;
            }
            else if (ventaja)
            {
                tmp.fontSize  = 24f;
                tmp.color     = new Color(0.937f, 0.267f, 0.267f); // #EF4444
                tmp.text      = $"{daño}";
                tmp.fontStyle = FontStyles.Bold;
            }
            else
            {
                tmp.fontSize  = 20f;
                tmp.color     = Color.white;
                tmp.text      = $"{daño}";
            }

            var comp = go.AddComponent<FloatingDamageText>();
            comp.StartCoroutine(comp.AnimateAndDestroy(rt, tmp));
        }

        // ── Animación ──────────────────────────────────────────────────────────

        private IEnumerator AnimateAndDestroy(RectTransform rt, TextMeshProUGUI tmp)
        {
            const float DURATION      = 1.0f;
            const float FADE_DURATION = 0.3f;
            const float RISE_PX       = 80f;

            Vector2 startPos  = rt.anchoredPosition;
            Vector2 endPos    = startPos + new Vector2(0f, RISE_PX);
            Color   baseColor = tmp.color;
            float   elapsed   = 0f;

            while (elapsed < DURATION)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / DURATION;

                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

                float fadeStart = DURATION - FADE_DURATION;
                float alpha = elapsed > fadeStart
                    ? 1f - (elapsed - fadeStart) / FADE_DURATION
                    : 1f;
                tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Clamp01(alpha));

                yield return null;
            }

            Destroy(gameObject);
        }

        // ── Helper ─────────────────────────────────────────────────────────────

        private static Vector2 ScreenToCanvasLocal(Canvas canvas, Vector2 screenPos)
        {
            var canvasRT = canvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRT, screenPos, canvas.worldCamera, out var localPoint);
            return localPoint;
        }
    }
}
