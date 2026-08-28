using System.Windows.Automation.Peers;
using QuickStat.Controls.Dataset;
using QuickStat.Domain.Matrix;
using Xunit;

namespace QuickStat.Tests.Ui.Controls;

/// <summary>
/// The grid survives its matrix changing shape underneath it, which is what a collect run does.
/// </summary>
/// <remarks>
/// <para>
/// <b>The crash these were written for terminated the process.</b> A collect run mutates the bound
/// <see cref="PersonMatrix"/> in place - it raises no change notification, which is why
/// <see cref="MatrixGrid.Refresh"/> exists at all - and
/// <c>CollectionsTabViewModel.CollectDataAsync</c> opens with <c>matrix.ClearVariables()</c> and then
/// <c>await</c>s once per ticked element. Between that clear and the end of the run, which is minutes
/// with 213 elements ticked, the grid's own <c>MatrixGridLayout</c> still reported the old column
/// count.
/// </para>
/// <para>
/// Painting was never affected: <c>OnRender</c> and <c>MeasureOverride</c> both call
/// <c>SyncCounts</c> first, so every frame re-reads the shape. The accessors below are not on that
/// path. <see cref="MatrixGridAutomationPeer"/> reaches them from <c>UpdateSubtree</c> during
/// <c>ContextLayoutManager.fireAutomationEvents</c>, so with any UI Automation client attached -
/// a screen reader, Narrator, an automation script - every layout pass in that window indexed an
/// emptied list and threw <see cref="ArgumentOutOfRangeException"/> onto the dispatcher.
/// PORT-PLAN.md §8.11 (7).
/// </para>
/// <para>
/// Note what made it invisible for so long: it needs an accessibility client to be listening, and
/// the peer only builds a subtree once one is. The port's own suite exercises the peer directly,
/// which does not reproduce the timing; it took attaching UIA to a running instance and then
/// collecting.
/// </para>
/// </remarks>
public class MatrixGridStaleShapeTests
{
    [Fact]
    public void ReadingCellsAfterTheColumnsAreClearedReturnsNothingRatherThanThrowing()
    {
        StaTestRunner.Run(() =>
        {
            PersonMatrix matrix = MatrixGridTestData.LargeMatrix(rows: 20, columns: 5);
            MatrixGrid grid = MatrixGridHarness.CreateGrid(matrix);

            // Laid out against five columns; every position below was legal a moment ago.
            Assert.NotEqual("", grid.GetDisplayCellText(MatrixGrid.NoIndex, 1));
            Assert.NotEqual("", grid.GetDisplayCellText(0, 1));

            // What CollectDataAsync does before its first await.
            matrix.ClearVariables();

            // No SyncCounts has run, so the layout still believes in six display columns.
            Assert.Equal("", grid.GetDisplayCellText(MatrixGrid.NoIndex, 1));
            Assert.Equal("", grid.GetDisplayCellText(0, 1));
            Assert.Null(grid.GetDisplayCellToolTip(MatrixGrid.NoIndex, 1));
            Assert.Null(grid.GetDisplayCellToolTip(0, 1));

            // The frozen column outlives the data columns, because ClearVariables keeps the cohort.
            Assert.NotEqual("", grid.GetDisplayCellText(0, 0));
        });
    }

    [Fact]
    public void ReadingCellsAfterThePopulationIsClearedReturnsNothingRatherThanThrowing()
    {
        // The other half: ClearPopulation drops the rows and leaves the columns, so the guard has to
        // be per-axis rather than one count.
        StaTestRunner.Run(() =>
        {
            PersonMatrix matrix = MatrixGridTestData.LargeMatrix(rows: 20, columns: 5);
            MatrixGrid grid = MatrixGridHarness.CreateGrid(matrix);

            matrix.ClearPopulation();

            Assert.Equal("", grid.GetDisplayCellText(0, 0));
            Assert.Equal("", grid.GetDisplayCellText(0, 1));
            Assert.Null(grid.GetDisplayCellToolTip(0, 1));

            // A column header is not addressed by row, so it survives losing the rows.
            Assert.NotEqual("", grid.GetDisplayCellText(MatrixGrid.NoIndex, 1));
        });
    }

    [Fact]
    public void TheWholeVisiblePeerSubtreeSurvivesTheColumnsGoingAway()
    {
        // Closest reproduction the suite can hold of what fireAutomationEvents does: build the peers
        // for everything on screen, pull the columns out from under them, then read every one of
        // them the way UpdateSubtree does.  Before the fix this threw on the first column header.
        StaTestRunner.Run(() =>
        {
            PersonMatrix matrix = MatrixGridTestData.LargeMatrix(rows: 20, columns: 5);
            MatrixGrid grid = MatrixGridHarness.CreateGrid(matrix);
            MatrixGridAutomationPeer peer = new(grid);

            List<MatrixGridCellAutomationPeer> cells = [.. peer.ColumnHeaderPeers()];

            for (int column = 0; column < grid.DisplayColumnCount; column++)
            {
                if (peer.CellPeer(0, column) is { } cell)
                {
                    cells.Add(cell);
                }
            }

            Assert.NotEmpty(cells);

            matrix.ClearVariables();

            foreach (MatrixGridCellAutomationPeer cell in cells)
            {
                Assert.NotNull(cell.Value);
            }
        });
    }

    [Fact]
    public void RefreshDropsThePeersCachedCells()
    {
        // Refresh is the "the matrix changed under us" entry point and was the one path that did not
        // invalidate the peer - only the dependency properties did, and a collect run moves none of
        // them.  A screen reader was reading the previous dataset out of recycled peers.
        StaTestRunner.Run(() =>
        {
            PersonMatrix matrix = MatrixGridTestData.LargeMatrix(rows: 20, columns: 5);
            MatrixGrid grid = MatrixGridHarness.CreateGrid(matrix);

            // Refresh finds the peer through UIElementAutomationPeer.FromElement, which only sees
            // the one WPF itself associated with the element.  Constructing a MatrixGridAutomationPeer
            // by hand would leave FromElement null and the test would pass against a no-op.
            MatrixGridAutomationPeer peer = Assert.IsType<MatrixGridAutomationPeer>(
                UIElementAutomationPeer.CreatePeerForElement(grid));

            Assert.Same(peer, UIElementAutomationPeer.FromElement(grid));

            MatrixGridCellAutomationPeer? before = peer.CellPeer(0, 1);

            Assert.NotNull(before);
            Assert.Same(before, peer.CellPeer(0, 1));

            grid.Refresh();

            Assert.NotSame(before, peer.CellPeer(0, 1));
        });
    }
}
