using System.Windows.Input;
using QuickStat.Controls.Dataset;
using Xunit;

namespace QuickStat.Tests.Ui.Controls;

/// <summary>
/// What the mouse wheel does over the dataset grid: it moves the caret, it does not scroll.
/// </summary>
/// <remarks>
/// <para>
/// <c>TCustomGrid.DoMouseWheelDown</c> (<c>Vcl.Grids.pas</c>) has two branches, and only the second
/// one scrolls:
/// </para>
/// <code>
/// if (Row = -1) or (Col = -1) then
///   begin if TopRow &lt; RowCount - 1 then TopRow := TopRow + 1 end
/// else if Row &lt; RowCount - 1 then
///   Row := Row + 1;
/// </code>
/// <para>
/// A grid that has a current cell - which, in the VCL, is every grid with rows in it - therefore
/// moves its <em>selection</em> one row a notch and scrolls only as much as keeping the caret
/// visible requires. <c>TControl.DoMouseWheel</c> accumulates the delta and calls that once per
/// <c>WHEEL_DELTA</c>, applying no <c>WheelScrollLines</c>: one notch is one row, not the three a
/// wheel usually means.
/// </para>
/// <para>
/// <b>Why this file exists.</b> The first fix for PORT-PLAN.md §8.11 (6) assumed the wheel scrolls,
/// hosted the grid in a <c>ScrollViewer</c> and stopped there. That was right about the scrollbars,
/// which were genuinely missing, and wrong about the gesture: on the 31-patient cohort the parity
/// pass uses, every row fits on screen, so there is nothing to scroll and the wheel still appeared
/// dead. The reference build was moving the highlight the whole time.
/// </para>
/// </remarks>
public class MatrixGridWheelTests
{
    private const int Width = 300;

    /// <summary>Tall enough for the header and six data rows: 18 + (6 x 17).</summary>
    private const int Height = 120;

    [Fact]
    public void TheWheelMovesTheCaretOneRowANotch()
    {
        (int first, int second, int back) = StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid(rows: 100);

            // The first notch only establishes the caret, exactly as the first arrow key does -
            // MoveCaret's rowStep is zero until there is something to step from.
            Wheel(grid, notches: -1);
            int a = grid.CurrentRowIndex;

            Wheel(grid, notches: -1);
            int b = grid.CurrentRowIndex;

            Wheel(grid, notches: 1);
            int c = grid.CurrentRowIndex;

            return (a, b, c);
        });

        Assert.Equal(0, first);
        Assert.Equal(1, second);
        Assert.Equal(0, back);
    }

    [Fact]
    public void TheWheelStopsAtBothEnds()
    {
        // Row := Row + 1 is guarded by `if Row < RowCount - 1`, and its opposite by
        // `if Row > FixedRows`.  The VCL returns True either way, so the gesture is consumed at the
        // ends rather than falling through to whatever encloses the grid.
        (int top, int bottom, bool consumed) = StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid(rows: 20);

            // Up at the first row.
            grid.SetCurrentCell(0, 0);
            bool handled = Wheel(grid, notches: 5);
            int atTheTop = grid.CurrentRowIndex;

            // Down at the last.
            grid.SetCurrentCell(19, 0);
            Wheel(grid, notches: -5);

            return (atTheTop, grid.CurrentRowIndex, handled);
        });

        Assert.Equal(0, top);
        Assert.Equal(19, bottom);
        Assert.True(consumed, "The grid let the wheel bubble; the host would scroll as well.");
    }

    [Fact]
    public void TwoNotchesInOneMessageMoveTwoRows()
    {
        // A wheel can report more than one notch per message, and WPF passes the delta through
        // rather than splitting it.  TControl.DoMouseWheel loops; the ScrollViewer, for contrast,
        // reads only the sign - which is one more reason not to have left this to the host.
        int row = StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid(rows: 100);

            grid.SetCurrentCell(10, 0);
            Wheel(grid, notches: -2);

            return grid.CurrentRowIndex;
        });

        Assert.Equal(12, row);
    }

    [Fact]
    public void DeltasBelowOneNotchAddUpRatherThanVanish()
    {
        // A precision touchpad sends fractions of a notch.  Truncating each message would mean the
        // caret never moves at all; the accumulator is TControl.DoMouseWheel's, and this is what it
        // is for.
        (int halfway, int arrived) = StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid(rows: 100);

            grid.SetCurrentCell(5, 0);

            Wheel(grid, delta: -Mouse.MouseWheelDeltaForOneLine / 2);
            int a = grid.CurrentRowIndex;

            Wheel(grid, delta: -Mouse.MouseWheelDeltaForOneLine / 2);

            return (a, grid.CurrentRowIndex);
        });

        Assert.Equal(5, halfway);
        Assert.Equal(6, arrived);
    }

    [Fact]
    public void TheViewFollowsTheCaretOffTheBottom()
    {
        // Exactly six rows fit: 120 tall, less an 18 header, over rows of 17.  The caret leaving the
        // viewport is the only thing that scrolls the grid on a wheel - TCustomGrid sets Row and
        // lets the grid catch up - so three notches from the top move nothing at all.
        (double whileVisible, double afterItLeft) = StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid(rows: 100);

            grid.SetCurrentCell(0, 0);

            Wheel(grid, notches: -3);
            double a = grid.VerticalOffset;

            Wheel(grid, notches: -20);

            return (a, grid.VerticalOffset);
        });

        Assert.Equal(0, whileVisible);

        // Row 23 is the caret; showing it whole means its *bottom* edge, 24 rows down, at the foot
        // of the viewport.
        Assert.Equal((24 * 17) - (Height - 18), afterItLeft);
    }

    [Fact]
    public void TheWheelMovesTheHintWithTheCaret()
    {
        // Reported from the parity pass: the hint kept saying PersonId = 260 while the caret was on
        // 261.  A VCL Click is not a mouse click - TCustomGrid.FocusCell raises it, SetRow goes
        // through FocusCell, and MainQuickStat.pas:311 hangs UpdateDataHintPanel off OnClick with
        // the comment "Moving around in grid triggers update hint view".  So the hint follows the
        // wheel there, and 05-ui-spec.md §G.2 was wrong to say otherwise.
        (int raised, int lastRow) = StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid(rows: 100);
            int count = 0;
            int row = MatrixGrid.NoIndex;

            grid.CellActivated += (_, e) =>
            {
                count++;
                row = e.RowIndex;
            };

            grid.SetCurrentCell(4, 0);
            Wheel(grid, notches: -3);

            return (count, row);
        });

        Assert.Equal(3, raised);
        Assert.Equal(7, lastRow);
    }

    [Fact]
    public void AWheelThatChangesNothingLeavesTheHintAlone()
    {
        // The VCL's own guard, at Vcl.Grids.pas:4383 - `if (NewCurrent.X <> Col) or (NewCurrent.Y <>
        // Row)` - so wheeling up at the first row does not re-fire it.
        int activations = StaTestRunner.Run(() =>
        {
            MatrixGrid grid = Grid(rows: 100);
            int raised = 0;

            grid.SetCurrentCell(0, 0);
            grid.CellActivated += (_, _) => raised++;

            Wheel(grid, notches: 3);

            return raised;
        });

        Assert.Equal(0, activations);
    }

    [Fact]
    public void AnEmptyGridLetsTheWheelPass()
    {
        // Nothing to navigate and nothing to scroll, so consuming the gesture would only stop
        // whatever encloses the grid from using it.
        bool handled = StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(null, Width, Height);

            return Wheel(grid, notches: -1);
        });

        Assert.False(handled);
    }

    private static MatrixGrid Grid(int rows) =>
        MatrixGridHarness.CreateGrid(MatrixGridTestData.LargeMatrix(rows, columns: 4), Width, Height);

    private static bool Wheel(MatrixGrid grid, int notches = 0, int? delta = null)
    {
        MouseWheelEventArgs args = new(
            Mouse.PrimaryDevice,
            0,
            delta ?? (notches * Mouse.MouseWheelDeltaForOneLine))
        {
            RoutedEvent = Mouse.MouseWheelEvent,
            Source = grid,
        };

        grid.RaiseEvent(args);

        return args.Handled;
    }
}
