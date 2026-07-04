#if UNITY_EDITOR
using NUnit.Framework;
using ReinoOscuridad.UI.Common;

[TestFixture]
public class PooledGridViewTests
{
    private const int ITEM_COUNT = 250;
    private const int COLUMNS = 3;
    private const float CELL_HEIGHT = 200f;
    private const float SPACING = 10f;
    private const float VIEWPORT_HEIGHT = 720f;

    private static int TotalRows() => (ITEM_COUNT + COLUMNS - 1) / COLUMNS;

    [Test]
    public void VisibleRowRange_LargeCatalog_OnlyReturnsSmallWindow()
    {
        int totalRows = TotalRows();

        var (firstRow, lastRow) = PooledGridView.VisibleRowRange(
            0f, VIEWPORT_HEIGHT, CELL_HEIGHT, SPACING, totalRows, bufferRows: 1);

        int visibleCount = lastRow - firstRow + 1;
        Assert.Less(visibleCount, totalRows,
            "Con 250 items el rango visible debe ser mucho menor que el total — esto es lo que evita instanciar el catálogo completo");
        Assert.GreaterOrEqual(firstRow, 0);
        Assert.LessOrEqual(lastRow, totalRows - 1);
    }

    [Test]
    public void VisibleRowRange_ScrolledToEnd_StaysWithinBounds()
    {
        int totalRows = TotalRows();
        float totalContentHeight = totalRows * CELL_HEIGHT + (totalRows - 1) * SPACING;

        var (firstRow, lastRow) = PooledGridView.VisibleRowRange(
            totalContentHeight, VIEWPORT_HEIGHT, CELL_HEIGHT, SPACING, totalRows, bufferRows: 1);

        Assert.LessOrEqual(lastRow, totalRows - 1,
            "El último índice visible nunca debe exceder el total de filas, ni con el scroll al final");
        Assert.GreaterOrEqual(firstRow, 0);
    }

    [Test]
    public void VisibleRowRange_MidScroll_WindowAdvancesWithScrollPosition()
    {
        int totalRows = TotalRows();

        var (firstAtTop, _) = PooledGridView.VisibleRowRange(
            0f, VIEWPORT_HEIGHT, CELL_HEIGHT, SPACING, totalRows, bufferRows: 1);
        var (firstAtMid, _) = PooledGridView.VisibleRowRange(
            5000f, VIEWPORT_HEIGHT, CELL_HEIGHT, SPACING, totalRows, bufferRows: 1);

        Assert.Greater(firstAtMid, firstAtTop,
            "Al hacer scroll el primer índice visible debe avanzar, no quedarse fijo");
    }

    [Test]
    public void VisibleRowRange_EmptyCatalog_ReturnsEmptyRange()
    {
        var (firstRow, lastRow) = PooledGridView.VisibleRowRange(
            0f, VIEWPORT_HEIGHT, CELL_HEIGHT, SPACING, totalRows: 0, bufferRows: 1);

        Assert.Greater(firstRow, lastRow, "Rango vacío cuando no hay filas");
    }
}
#endif
