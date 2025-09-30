/*
============================================================
WorldCanvasFitToSprite.cs — Ajuste de canvas al fondo
------------------------------------------------------------
PROPÓSITO
- Sincronizar tamaño/escala del canvas/elementos con un SpriteRenderer.

MÉTODOS (COMPLETA AQUÍ)
- FitTo(SpriteRenderer): recalcula bounds y escala.
============================================================
*/

using UnityEngine;

[ExecuteAlways]
public class WorldCanvasFitToSprite : MonoBehaviour
{
    public SpriteRenderer targetSprite;   // Asigna Background
    public float pixelsPerUnit = 100f;    // Igual que tu arte
    public float zOffset = 0f;            // 0 en 2D

    void LateUpdate()
    {
        if (targetSprite == null || targetSprite.sprite == null) return;

        var sr = targetSprite;
        var bounds = sr.bounds; // en unidades del mundo

        // Posiciona el canvas centrado en el fondo
        transform.position = new Vector3(bounds.center.x, bounds.center.y, zOffset);
        transform.rotation = Quaternion.identity;

        // Escala/size del canvas para cubrir el fondo
        var rect = GetComponent<RectTransform>();
        if (!rect) return;

        // Cada unidad del mundo equivale a 1 en rect.sizeDelta si el canvas localScale = 1.
        // sizeDelta → en “unidades del mundo” para un World Space Canvas.
        rect.sizeDelta = new Vector2(bounds.size.x, bounds.size.y);
        transform.localScale = Vector3.one; // importante para que 1 unidad = 1 del mundo
    }
}
