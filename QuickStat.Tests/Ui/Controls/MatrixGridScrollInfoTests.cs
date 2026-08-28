using QuickStat.Controls.Dataset;
using Xunit;

namespace QuickStat.Tests.Ui.Controls;

/// <summary>
/// What the grid reports to a <c>ScrollViewer</c>, and how it moves.
/// </summary>
/// <remarks>
/// The extent deliberately covers only what actually scrolls: the frozen identity block is not part
/// of the horizontal extent and the pinned header row is not part of the vertical one. That is what
/// makes <c>HorizontalOffset</c> the offset into the data band directly, with no frozen-width
/// correction anywhere in the renderer or the hit-tester.
/// </remarks>
public class MatrixGridScrollInfoTests
{
    private const int Width = 300;
    private const int Height = 120;

    // 300 wide minus the 44-unit PID column; 120 tall minus the 18-unit header.
    private const double BandWidth = Width - 44;
    private const double BandHeight = Height - 18;

    [Fact]
    public void TheExtentIsTheScrollableBandAndNotTheWholeGrid()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid(rows: 100, columns: 50);

            Assert.Equal(50 * 64, grid.ExtentWidth);
            Assert.Equal(100 * 17, grid.ExtentHeight);
            Assert.Equal(BandWidth, grid.ViewportWidth);
            Assert.Equal(BandHeight, grid.ViewportHeight);
        });
    }

    [Fact]
    public void OffsetsAreClampedToTheExtent()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid(rows: 100, columns: 50);

            grid.SetHorizontalOffset(1_000_000);
            grid.SetVerticalOffset(1_000_000);

            Assert.Equal(grid.ExtentWidth - grid.ViewportWidth, grid.HorizontalOffset);
            Assert.Equal(grid.ExtentHeight - grid.ViewportHeight, grid.VerticalOffset);

            grid.SetHorizontalOffset(-500);
            grid.SetVerticalOffset(-500);

            Assert.Equal(0, grid.HorizontalOffset);
            Assert.Equal(0, grid.VerticalOffset);
        });
    }

    [Fact]
    public void AGridWiderThanItsContentCannotScrollAtAll()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid(rows: 2, columns: 2);

            grid.SetHorizontalOffset(200);
            grid.SetVerticalOffset(200);

            Assert.Equal(0, grid.HorizontalOffset);
            Assert.Equal(0, grid.VerticalOffset);
        });
    }

    [Fact]
    public void LineStepsAreOneRowAndOneColumn()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid(rows: 100, columns: 50);

            grid.LineDown();
            grid.LineRight();

            Assert.Equal(17, grid.VerticalOffset);
            Assert.Equal(64, grid.HorizontalOffset);

            grid.LineUp();
            grid.LineLeft();

            Assert.Equal(0, grid.VerticalOffset);
            Assert.Equal(0, grid.HorizontalOffset);
        });
    }

    [Fact]
    public void PageStepsAreOneViewport()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid(rows: 100, columns: 50);

            grid.PageDown();

            Assert.Equal(BandHeight, grid.VerticalOffset);

            grid.PageRight();

            Assert.Equal(BandWidth, grid.HorizontalOffset);
        });
    }

    [Fact]
    public void TheIScrollInfoWheelMembersMoveThreeRows()
    {
        // NOT what the wheel does over the dataset - MatrixGrid.OnMouseWheel handles that itself and
        // moves the caret one row, as TCustomGrid does.  These two members are here because
        // IScrollInfo requires them, and this case says only that they are arithmetically right.
        //
        // It used to be called TheWheelMovesThreeRows, and under that name it was read as proof the
        // wheel worked.  It was green throughout the whole of PORT-PLAN.md §8.11 (6), during which
        // the wheel over the Dataset tab did nothing whatsoever.  Ui/Controls/MatrixGridWheelTests.cs
        // and Ui/Dataset/DatasetGridScrollHostTests.cs are where the gesture is answered.
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid(rows: 100, columns: 50);

            grid.MouseWheelDown();

            Assert.Equal(3 * 17, grid.VerticalOffset);

            grid.MouseWheelUp();

            Assert.Equal(0, grid.VerticalOffset);
        });
    }

    [Fact]
    public void ShrinkingTheGridPullsTheOffsetsBackInside()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid(rows: 100, columns: 50);

            grid.SetHorizontalOffset(2_000);
            grid.SetVerticalOffset(1_000);

            // A far bigger viewport leaves nothing to scroll to.
            MatrixGridHarness.LayOut(grid, 5_000, 5_000);

            Assert.Equal(0, grid.HorizontalOffset);
            Assert.Equal(0, grid.VerticalOffset);
        });
    }

    [Fact]
    public void WideColumnsChangesTheExtentAndKeepsTheOffsetValid()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid(rows: 10, columns: 10);

            grid.SetHorizontalOffset(grid.ExtentWidth);

            double narrowOffset = grid.HorizontalOffset;

            grid.DataColumnWidth = MatrixGrid.WideDataColumnWidth;

            MatrixGridHarness.LayOut(grid, Width, Height);

            Assert.Equal(10 * 120, grid.ExtentWidth);
            Assert.True(grid.HorizontalOffset >= narrowOffset);
            Assert.True(grid.HorizontalOffset <= grid.ExtentWidth - grid.ViewportWidth);
        });
    }

    [Fact]
    public void AnEmptyGridReportsAZeroExtent()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(null, Width, Height);

            Assert.Equal(0, grid.ExtentWidth);
            Assert.Equal(0, grid.ExtentHeight);
            Assert.Equal(0, grid.HorizontalOffset);
            Assert.Equal(0, grid.VerticalOffset);
        });
    }

    private static MatrixGrid Grid(int rows, int columns) =>
        MatrixGridHarness.CreateGrid(MatrixGridTestData.LargeMatrix(rows, columns), Width, Height);
}
