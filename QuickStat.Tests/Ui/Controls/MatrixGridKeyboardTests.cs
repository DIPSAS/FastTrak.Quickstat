using System.Windows;
using System.Windows.Input;
using QuickStat.Controls.Dataset;
using Xunit;

namespace QuickStat.Tests.Ui.Controls;

/// <summary>Keyboard navigation, which is also how the grid is usable without a mouse.</summary>
/// <remarks>
/// Driven through <c>MoveCaret</c> rather than a synthesised <see cref="KeyEventArgs"/>: those need
/// a live <see cref="PresentationSource"/>, and <see cref="Keyboard.Modifiers"/> would read the
/// machine's real keyboard mid-test.
/// </remarks>
public class MatrixGridKeyboardTests
{
    [Fact]
    public void TheFirstArrowLandsOnTheFirstCell()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid();

            Assert.Equal(MatrixGrid.NoIndex, grid.CurrentRowIndex);
            Assert.True(grid.MoveCaret(Key.Down, control: false));

            Assert.Equal(0, grid.CurrentRowIndex);
            Assert.Equal(0, grid.CurrentColumnIndex);
        });
    }

    [Fact]
    public void ArrowsWalkOneCellAtATime()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid();

            grid.SetCurrentCell(5, 5);

            grid.MoveCaret(Key.Down, control: false);
            grid.MoveCaret(Key.Right, control: false);

            Assert.Equal(6, grid.CurrentRowIndex);
            Assert.Equal(6, grid.CurrentColumnIndex);

            grid.MoveCaret(Key.Up, control: false);
            grid.MoveCaret(Key.Left, control: false);

            Assert.Equal(5, grid.CurrentRowIndex);
            Assert.Equal(5, grid.CurrentColumnIndex);
        });
    }

    [Fact]
    public void TheCaretStopsAtTheEdgesRatherThanWrapping()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid();

            grid.SetCurrentCell(0, 0);
            grid.MoveCaret(Key.Up, control: false);
            grid.MoveCaret(Key.Left, control: false);

            Assert.Equal(0, grid.CurrentRowIndex);
            Assert.Equal(0, grid.CurrentColumnIndex);

            grid.SetCurrentCell(49, 19);
            grid.MoveCaret(Key.Down, control: false);
            grid.MoveCaret(Key.Right, control: false);

            Assert.Equal(49, grid.CurrentRowIndex);
            Assert.Equal(19, grid.CurrentColumnIndex);
        });
    }

    [Fact]
    public void PageKeysMoveAViewportOfRows()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid();

            grid.SetCurrentCell(0, 0);
            grid.MoveCaret(Key.PageDown, control: false);

            // A 120-unit control has 102 units of data band, which is six whole 17-unit rows.
            Assert.Equal(6, grid.CurrentRowIndex);

            grid.MoveCaret(Key.PageUp, control: false);

            Assert.Equal(0, grid.CurrentRowIndex);
        });
    }

    [Fact]
    public void HomeAndEndMoveAlongTheRowAndWithControlToTheCorners()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid();

            grid.SetCurrentCell(20, 10);

            grid.MoveCaret(Key.End, control: false);

            Assert.Equal(20, grid.CurrentRowIndex);
            Assert.Equal(19, grid.CurrentColumnIndex);

            grid.MoveCaret(Key.Home, control: false);

            Assert.Equal(20, grid.CurrentRowIndex);
            Assert.Equal(0, grid.CurrentColumnIndex);

            grid.MoveCaret(Key.End, control: true);

            Assert.Equal(49, grid.CurrentRowIndex);
            Assert.Equal(19, grid.CurrentColumnIndex);

            grid.MoveCaret(Key.Home, control: true);

            Assert.Equal(0, grid.CurrentRowIndex);
            Assert.Equal(0, grid.CurrentColumnIndex);
        });
    }

    [Fact]
    public void NavigationKeepsTheCaretOnScreen()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid();

            grid.MoveCaret(Key.End, control: true);

            Assert.True(grid.TryGetCellBounds(49, 19, out Rect bounds));
            Assert.InRange(bounds.X, grid.FrozenWidth, 300);
            Assert.InRange(bounds.Y, 18, 120);
        });
    }

    [Fact]
    public void AKeyThatIsNotNavigationIsLeftAlone()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid();

            grid.SetCurrentCell(3, 3);

            Assert.False(grid.MoveCaret(Key.A, control: false));
            Assert.False(grid.MoveCaret(Key.Escape, control: false));

            Assert.Equal(3, grid.CurrentRowIndex);
            Assert.Equal(3, grid.CurrentColumnIndex);
        });
    }

    [Fact]
    public void AnEmptyGridSwallowsNothing()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(null, 300, 120);

            Assert.False(grid.MoveCaret(Key.Down, control: false));
            Assert.Equal(MatrixGrid.NoIndex, grid.CurrentRowIndex);
        });
    }

    [Fact]
    public void AGridWithRowsButNoColumnsStillMovesDownTheIdentityColumn()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.RowsWithoutColumns(), 300, 120);

            Assert.True(grid.MoveCaret(Key.Down, control: false));

            Assert.Equal(0, grid.CurrentRowIndex);
            Assert.Equal(MatrixGrid.NoIndex, grid.CurrentColumnIndex);
        });
    }

    [Fact]
    public void TheGridIsFocusableSoItCanBeReachedByTab()
    {
        StaTestRunner.Run(() => Assert.True(MatrixGridHarness.CreateGrid(null).Focusable));
    }

    private static MatrixGrid Grid() =>
        MatrixGridHarness.CreateGrid(MatrixGridTestData.LargeMatrix(rows: 50, columns: 20), 300, 120);
}
