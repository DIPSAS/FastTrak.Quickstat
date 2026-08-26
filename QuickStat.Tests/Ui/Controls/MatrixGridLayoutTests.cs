using System.Windows;
using QuickStat.Controls.Dataset;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;
using Xunit;

namespace QuickStat.Tests.Ui.Controls;

/// <summary>
/// The grid's layout arithmetic: column geometry, hit-testing, and which cells a viewport covers.
/// </summary>
/// <remarks>
/// None of this constructs a WPF object, so it runs straight on the MTA test thread with no
/// <see cref="StaTestRunner"/> and no dispatcher. That separation is deliberate: the interesting
/// logic in an owner-drawn grid is arithmetic, and arithmetic tested through a rendered bitmap is
/// tested badly.
/// </remarks>
public class MatrixGridLayoutTests
{
    private static MatrixGridLayout Layout(
        int rows = 10,
        int columns = 5,
        PersonIdentification identification = PersonIdentification.PersonIdOnly) => new()
        {
            RowCount = rows,
            DataColumnCount = columns,
            VisibleFixedOrdinals = FixedColumns.VisibleOrdinals(IdentificationColumns.For(identification)),
        };

    [Fact]
    public void AnonymousModeFreezesOnlyThePersonIdColumn()
    {
        MatrixGridLayout layout = Layout();

        Assert.Equal(1, layout.FixedColumnCount);
        Assert.Equal(44, layout.FrozenWidth);
        Assert.Equal(FixedColumns.PersonId, layout.FixedOrdinalAt(0));
    }

    [Fact]
    public void FullIdentificationFreezesAllFourAtTheDocumentedWidths()
    {
        MatrixGridLayout layout = Layout(identification: PersonIdentification.Full);

        Assert.Equal(4, layout.FixedColumnCount);
        Assert.Equal([44, 64, 84, 128], Enumerable.Range(0, 4).Select(layout.ColumnWidth));

        // 44 + 64 + 84 + 128.
        Assert.Equal(320, layout.FrozenWidth);
    }

    [Fact]
    public void HidingIdentityColumnsShiftsTheDataColumnsLeft()
    {
        MatrixGridLayout full = Layout(identification: PersonIdentification.Full);
        MatrixGridLayout anonymous = Layout();

        Assert.Equal(4, full.DisplayIndexOfData(0));
        Assert.Equal(1, anonymous.DisplayIndexOfData(0));

        // The model index is unmoved either way; only the display index shifts.
        Assert.Equal(0, full.DataColumnAt(4));
        Assert.Equal(0, anonymous.DataColumnAt(1));
    }

    [Fact]
    public void AHiddenIdentityColumnHasNoDisplayIndexAtAll()
    {
        MatrixGridLayout layout = Layout();

        // The VCL hid a column by setting its width to -1, which left it draggable back into view
        // (PORT-PLAN.md §7.2). Here it simply is not laid out.
        Assert.Equal(MatrixGrid.NoIndex, layout.DisplayIndexOfFixed(FixedColumns.Name));
        Assert.Equal(MatrixGrid.NoIndex, layout.DisplayIndexOfFixed(FixedColumns.NationalId));
        Assert.Equal(0, layout.DisplayIndexOfFixed(FixedColumns.PersonId));
    }

    [Fact]
    public void DataColumnsAreSixtyFourWideAndOneHundredAndTwentyWhenWide()
    {
        MatrixGridLayout layout = Layout(columns: 3);

        Assert.Equal(MatrixGrid.NarrowDataColumnWidth, layout.ColumnWidth(1));
        Assert.Equal(3 * 64, layout.DataWidth);

        layout.DataColumnWidth = MatrixGrid.WideDataColumnWidth;

        Assert.Equal(120, layout.ColumnWidth(1));
        Assert.Equal(3 * 120, layout.DataWidth);
    }

    [Fact]
    public void TotalHeightIsTheHeaderPlusTheRows()
    {
        MatrixGridLayout layout = Layout(rows: 17);

        Assert.Equal(18 + (17 * 17), layout.TotalHeight);
        Assert.Equal(17 * 17, layout.DataHeight);
    }

    [Fact]
    public void OnlyTheRowsInsideTheViewportAreVisible()
    {
        MatrixGridLayout layout = Layout(rows: 1500);

        // A 340-unit band at row 100 covers exactly twenty 17-unit rows.
        Assert.Equal(new MatrixGridRange(100, 20), layout.VisibleRows(1700, 340));
    }

    [Fact]
    public void APartiallyVisibleRowAtEitherEdgeIsIncluded()
    {
        MatrixGridLayout layout = Layout(rows: 1500);

        MatrixGridRange rows = layout.VisibleRows(1705, 340);

        Assert.Equal(100, rows.First);
        Assert.Equal(21, rows.Count);
    }

    [Fact]
    public void VisibleRowsIsClampedToTheEndOfTheGrid()
    {
        MatrixGridLayout layout = Layout(rows: 5);

        Assert.Equal(new MatrixGridRange(0, 5), layout.VisibleRows(0, 1000));
        Assert.True(layout.VisibleRows(10_000, 340).IsEmpty);
    }

    [Fact]
    public void OnlyTheColumnsInsideTheViewportAreVisible()
    {
        MatrixGridLayout layout = Layout(columns: 1000);

        // 640 units of band at offset 6400 is columns 100 to 109.
        Assert.Equal(new MatrixGridRange(100, 10), layout.VisibleDataColumns(6400, 640));
    }

    [Fact]
    public void VisibleColumnsIsEmptyWhenScrolledPastTheLastColumn()
    {
        MatrixGridLayout layout = Layout(columns: 10);

        Assert.True(layout.VisibleDataColumns(10_000, 640).IsEmpty);
    }

    [Fact]
    public void CellBoundsFollowTheScrollOffsetForDataColumnsButNotFrozenOnes()
    {
        MatrixGridLayout layout = Layout(rows: 100, columns: 100);

        Assert.True(layout.TryGetCellBounds(10, 1, 128, 34, out Rect data));
        Assert.Equal(new Rect(44 - 128, (18 + 170) - 34, 64, 17), data);

        Assert.True(layout.TryGetCellBounds(10, 0, 128, 34, out Rect frozen));
        Assert.Equal(new Rect(0, (18 + 170) - 34, 44, 17), frozen);
    }

    [Fact]
    public void HeaderCellBoundsIgnoreTheVerticalOffset()
    {
        MatrixGridLayout layout = Layout();

        Assert.True(layout.TryGetCellBounds(MatrixGrid.NoIndex, 0, 0, 500, out Rect header));
        Assert.Equal(new Rect(0, 0, 44, 18), header);
    }

    [Fact]
    public void CellBoundsRefusesAPositionThatAddressesNoCell()
    {
        MatrixGridLayout layout = Layout(rows: 3, columns: 2);

        Assert.False(layout.TryGetCellBounds(3, 0, 0, 0, out _));
        Assert.False(layout.TryGetCellBounds(0, 3, 0, 0, out _));
    }

    [Fact]
    public void HitTestFindsTheHeaderTheFrozenColumnAndTheDataCell()
    {
        MatrixGridLayout layout = Layout(rows: 10, columns: 10);

        MatrixGridHit header = layout.HitTest(new Point(60, 5), 0, 0);
        MatrixGridHit frozen = layout.HitTest(new Point(10, 30), 0, 0);
        MatrixGridHit data = layout.HitTest(new Point(60, 30), 0, 0);

        Assert.Equal(MatrixGridCellKind.ColumnHeader, header.Kind);
        Assert.Equal(MatrixGrid.NoIndex, header.RowIndex);
        Assert.Equal(0, header.ColumnIndex);

        Assert.Equal(MatrixGridCellKind.Fixed, frozen.Kind);
        Assert.Equal(0, frozen.RowIndex);
        Assert.Equal(MatrixGrid.NoIndex, frozen.ColumnIndex);
        Assert.Equal(FixedColumns.PersonId, frozen.FixedOrdinal);

        Assert.Equal(MatrixGridCellKind.Data, data.Kind);
        Assert.Equal(0, data.RowIndex);
        Assert.Equal(0, data.ColumnIndex);
    }

    [Fact]
    public void HitTestAccountsForBothScrollOffsets()
    {
        MatrixGridLayout layout = Layout(rows: 100, columns: 100);

        // Horizontally scrolled by ten columns, vertically by ten rows.
        MatrixGridHit hit = layout.HitTest(new Point(60, 30), 640, 170);

        Assert.Equal(MatrixGridCellKind.Data, hit.Kind);
        Assert.Equal(10, hit.ColumnIndex);
        Assert.Equal(10, hit.RowIndex);
    }

    [Fact]
    public void TheFrozenBlockWinsOverWhateverHasScrolledUnderIt()
    {
        MatrixGridLayout layout = Layout(rows: 10, columns: 100);

        // x = 20 is inside the 44-unit PID column, and a data column is scrolled under it.
        MatrixGridHit hit = layout.HitTest(new Point(20, 30), 640, 0);

        Assert.Equal(MatrixGridCellKind.Fixed, hit.Kind);
    }

    [Fact]
    public void HitTestMissesOutsideTheGrid()
    {
        MatrixGridLayout layout = Layout(rows: 2, columns: 2);

        Assert.False(layout.HitTest(new Point(-1, 10), 0, 0).IsHit);
        Assert.False(layout.HitTest(new Point(10, -1), 0, 0).IsHit);
        Assert.False(layout.HitTest(new Point(10, 5000), 0, 0).IsHit);
        Assert.False(layout.HitTest(new Point(5000, 30), 0, 0).IsHit);
    }

    [Fact]
    public void ResizingOneColumnMovesEveryColumnAfterIt()
    {
        MatrixGridLayout layout = Layout(columns: 3);

        layout.SetColumnWidth(1, 100);

        Assert.Equal(100, layout.ColumnWidth(1));
        Assert.Equal(44 + 100, layout.ColumnOffset(2));
        Assert.Equal(100 + 64 + 64, layout.DataWidth);
    }

    [Fact]
    public void AColumnCannotBeDraggedNarrowerThanTheMinimum()
    {
        MatrixGridLayout layout = Layout(columns: 2);

        layout.SetColumnWidth(1, -50);

        Assert.Equal(MatrixGridLayout.MinimumColumnWidth, layout.ColumnWidth(1));
    }

    [Fact]
    public void ChangingTheDataColumnWidthDiscardsEveryHandSetWidth()
    {
        MatrixGridLayout layout = Layout(columns: 2, identification: PersonIdentification.Full);

        layout.SetColumnWidth(4, 200);
        layout.SetColumnWidth(0, 200);

        layout.DataColumnWidth = MatrixGrid.WideDataColumnWidth;

        // Delphi Set_DataColWidth assigns DefaultColWidth and then re-applies the four fixed widths,
        // so both kinds of override go (EPR.QA.GUI.Grid.pas:338-347).
        Assert.Equal(120, layout.ColumnWidth(4));
        Assert.Equal(44, layout.ColumnWidth(0));
    }

    [Fact]
    public void TheResizeGripStraddlesAColumnEdge()
    {
        MatrixGridLayout layout = Layout(columns: 3);

        // The PID column ends at 44.
        Assert.Equal(0, layout.ColumnResizeTargetAt(new Point(43, 5), 0));
        Assert.Equal(0, layout.ColumnResizeTargetAt(new Point(46, 5), 0));
        Assert.Equal(MatrixGrid.NoIndex, layout.ColumnResizeTargetAt(new Point(70, 5), 0));

        // And the first data column ends at 44 + 64.
        Assert.Equal(1, layout.ColumnResizeTargetAt(new Point(108, 5), 0));
    }

    [Fact]
    public void AnEdgeScrolledBehindTheFrozenBlockIsNotGrabbable()
    {
        MatrixGridLayout layout = Layout(columns: 100);

        // At offset 600 the right edge of data column 8 falls at x = 20, under the 44-wide frozen
        // block. A user cannot see it, so a user cannot drag it.
        Assert.Equal(MatrixGrid.NoIndex, layout.ColumnResizeTargetAt(new Point(20, 5), 600));
    }

    [Fact]
    public void TheFrozenEdgeWinsTheGripOverAColumnScrolledUnderIt()
    {
        MatrixGridLayout layout = Layout(columns: 100);

        // At offset 640 the right edge of data column 10 lands exactly on the frozen block's own
        // edge. Dragging there resizes PID, which is what is drawn at that pixel.
        Assert.Equal(0, layout.ColumnResizeTargetAt(new Point(44, 5), 640));
    }

    [Fact]
    public void ScrollArrowsStepOneWholeColumn()
    {
        MatrixGridLayout layout = Layout(columns: 10);

        Assert.Equal(64, layout.NextColumnBoundary(0));
        Assert.Equal(128, layout.NextColumnBoundary(64));
        Assert.Equal(128, layout.NextColumnBoundary(100));

        Assert.Equal(0, layout.PreviousColumnBoundary(64));
        Assert.Equal(64, layout.PreviousColumnBoundary(100));
        Assert.Equal(0, layout.PreviousColumnBoundary(0));
    }

    [Fact]
    public void ScrollStepsFollowAHandSetWidthRatherThanTheDefault()
    {
        MatrixGridLayout layout = Layout(columns: 3);

        layout.SetColumnWidth(1, 200);

        Assert.Equal(200, layout.NextColumnBoundary(0));
        Assert.Equal(0, layout.PreviousColumnBoundary(200));
    }

    [Fact]
    public void ScrollingIntoViewMovesAsLittleAsPossible()
    {
        MatrixGridLayout layout = Layout(rows: 100, columns: 100);

        // Already visible: no movement.
        Assert.Equal(0, layout.OffsetToShowColumn(2, 0, 640));

        // Off the right edge: scroll just far enough that its right edge lands on the viewport edge.
        Assert.Equal((11 * 64) - 640, layout.OffsetToShowColumn(10, 0, 640));

        // Off the left edge: scroll to its left edge exactly.
        Assert.Equal(2 * 64, layout.OffsetToShowColumn(2, 500, 640));

        Assert.Equal(0, layout.OffsetToShowRow(3, 0, 340));
        Assert.Equal((51 * 17) - 340, layout.OffsetToShowRow(50, 0, 340));
        Assert.Equal(3 * 17, layout.OffsetToShowRow(3, 500, 340));
    }

    [Fact]
    public void AColumnWiderThanTheViewportIsShownFromItsLeftEdge()
    {
        MatrixGridLayout layout = Layout(columns: 3);

        layout.SetColumnWidth(2, 900);

        // Its right edge cannot be reached without losing its start, so its start wins.
        Assert.Equal(64, layout.OffsetToShowColumn(1, 0, 300));
    }

    [Fact]
    public void KindAtNamesTheFourCellKinds()
    {
        MatrixGridLayout layout = Layout(rows: 3, columns: 3, identification: PersonIdentification.Full);

        Assert.Equal(MatrixGridCellKind.FixedHeader, layout.KindAt(MatrixGrid.NoIndex, 0));
        Assert.Equal(MatrixGridCellKind.ColumnHeader, layout.KindAt(MatrixGrid.NoIndex, 4));
        Assert.Equal(MatrixGridCellKind.Fixed, layout.KindAt(0, 3));
        Assert.Equal(MatrixGridCellKind.Data, layout.KindAt(0, 4));
        Assert.Equal(MatrixGridCellKind.None, layout.KindAt(0, 7));
        Assert.Equal(MatrixGridCellKind.None, layout.KindAt(3, 4));
    }

    [Fact]
    public void AGridWithNoColumnsHasNoCellsAtAll()
    {
        MatrixGridLayout layout = Layout(rows: 3, columns: 0);

        layout.VisibleFixedOrdinals = [];

        Assert.Equal(0, layout.DisplayColumnCount);
        Assert.Equal(0, layout.TotalWidth);
        Assert.False(layout.HitTest(new Point(10, 30), 0, 0).IsHit);
        Assert.True(layout.VisibleDataColumns(0, 640).IsEmpty);
    }
}
