/*
============================================================
GridCellResizer.cs — Tamaño de celdas en GridLayoutGroup por columnas
------------------------------------------------------------
PROPÓSITO
- Calcular cellSize (ancho x alto) en función del ancho visible del viewport,
  padding y spacing, manteniendo aspect ratio.

USO
- Añadir al GO con GridLayoutGroup y asignar referenceRect (ScrollRect.viewport).

MÉTODOS
- Awake(): resuelve referenceRect automáticamente si no se asigna.
- Start(): primer ResizeCells().
- OnRectTransformDimensionsChange(): vuelve a ajustar ante cambios.
- SetColumns(int cols, bool refresh=true): cambia nº de columnas.
- ResizeNow(): alias público.
- ResizeCells(): cálculo principal (padding + spacing) y aplica constraint/spacing/cellSize.
============================================================
*/

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(GridLayoutGroup))]
public class GridCellResizer : MonoBehaviour
{
    [Header("Fuente de ancho (Viewport)")]
    public RectTransform referenceRect;                  // Asigna: ViewPort del ScrollRect

    [Header("Parámetros")]
    public int columns = 3;                              // columnas actuales
    public Vector2 spacing = new Vector2(10, 10);        // igual que en el GridLayoutGroup
    public float aspectRatio = 1f;                       // 1 = cuadrado

    private GridLayoutGroup grid;

    void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
        if (referenceRect == null)
        {
            // Intenta resolverlo automáticamente
            var sr = GetComponentInParent<ScrollRect>(true);
            if (sr != null) referenceRect = sr.viewport != null ? sr.viewport : sr.GetComponent<RectTransform>();
        }
    }

    void Start() { ResizeCells(); }

    void OnRectTransformDimensionsChange() { ResizeCells(); }

    public void SetColumns(int cols, bool refresh = true)
    {
        columns = Mathf.Max(1, cols);
        if (refresh) ResizeCells();
    }

    public void ResizeNow() => ResizeCells();

    private void ResizeCells()
    {
        if (grid == null || referenceRect == null) return;

        // Ancho visible del viewport
        float width = referenceRect.rect.width;

        // Restar padding y espaciados
        float totalSpacing = spacing.x * (columns - 1);
        float innerWidth = width - grid.padding.left - grid.padding.right - totalSpacing;
        if (innerWidth <= 0f) return;

        float cellWidth = innerWidth / columns;
        float cellHeight = cellWidth * aspectRatio;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.spacing = spacing;
        grid.cellSize = new Vector2(cellWidth, cellHeight);

        // Rebuild inmediato
        var rt = grid.GetComponent<RectTransform>();
        if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }
}
