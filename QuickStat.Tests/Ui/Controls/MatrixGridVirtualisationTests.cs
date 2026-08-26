using System.Diagnostics;
using QuickStat.Controls.Dataset;
using QuickStat.Domain.Matrix;
using Xunit;
using Xunit.Abstractions;

namespace QuickStat.Tests.Ui.Controls;

/// <summary>
/// Proof that the grid is virtualised, against the documented worst case of 1500 × 1000.
/// </summary>
/// <remarks>
/// <para>
/// "Virtualised" is only worth claiming if it is measured, so these tests count the calls the
/// renderer makes into <see cref="PersonMatrix"/> rather than inspecting a visual tree - there is no
/// visual tree to inspect, which is the point. A frame must touch the few hundred cells inside the
/// viewport and not the million and a half behind it.
/// </para>
/// <para>
/// The timing assertions are deliberately loose. They exist to catch an accidental full scan, which
/// costs seconds, not to police tens of milliseconds on a shared build agent.
/// </para>
/// </remarks>
public class MatrixGridVirtualisationTests(ITestOutputHelper output)
{
    private const int WorstCaseRows = 1500;
    private const int WorstCaseColumns = 1000;

    [Fact]
    public void AFrameTouchesOnlyTheCellsInsideTheViewport()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(
                MatrixGridTestData.LargeMatrix(WorstCaseRows, WorstCaseColumns),
                width: 1_000,
                height: 600);

            _ = MatrixGridHarness.Render(grid, 1_000, 600);

            MatrixGridRenderStatistics stats = grid.LastRenderStatistics;

            // 582 units of band over 17-unit rows is 35 rows; 956 over 64-unit columns is 15.
            Assert.Equal(35, stats.Rows);
            Assert.Equal(15, stats.Columns);
            Assert.Equal(35 * 15, stats.DataCells);
            Assert.Equal(35, stats.FixedCells);
            Assert.Equal(16, stats.HeaderCells);

            // 1 500 000 data cells exist; the frame asked about 525 of them.
            Assert.True(
                stats.TotalCells < 1_000,
                $"A single frame touched {stats.TotalCells} cells, which is not virtualisation.");
        });
    }

    [Fact]
    public void ScrollingToTheFarCornerStillTouchesTheSameHandfulOfCells()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(
                MatrixGridTestData.LargeMatrix(WorstCaseRows, WorstCaseColumns),
                width: 1_000,
                height: 600);

            grid.SetHorizontalOffset(grid.ExtentWidth);
            grid.SetVerticalOffset(grid.ExtentHeight);

            _ = MatrixGridHarness.Render(grid, 1_000, 600);

            MatrixGridRenderStatistics stats = grid.LastRenderStatistics;

            Assert.InRange(stats.Rows, 34, 36);
            Assert.InRange(stats.Columns, 14, 16);
            Assert.True(stats.TotalCells < 1_000);
        });
    }

    [Fact]
    public void TheBrushCacheStaysTinyAcrossTheWholeWorstCase()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(
                MatrixGridTestData.LargeMatrix(WorstCaseRows, WorstCaseColumns),
                width: 1_000,
                height: 600);

            for (int frame = 0; frame < 20; frame++)
            {
                grid.SetVerticalOffset(frame * 17 * 13);
                grid.SetHorizontalOffset(frame * 64 * 7);

                _ = MatrixGridHarness.Render(grid, 1_000, 600);
            }

            // White, the empty grey, the fixed fill and the two text colours - nothing per cell.
            Assert.InRange(grid.BrushCacheSize, 1, 16);
        });
    }

    [Fact]
    public void TheWorstCaseRendersAFrameQuickly()
    {
        StaTestRunner.Run(
            () =>
            {
                Stopwatch build = Stopwatch.StartNew();
                PersonMatrix matrix = MatrixGridTestData.LargeMatrix(WorstCaseRows, WorstCaseColumns);

                build.Stop();

                MatrixGrid grid = MatrixGridHarness.CreateGrid(matrix, width: 1_000, height: 600);

                // First frame includes typeface resolution and the initial brush cache fill.
                Stopwatch first = Stopwatch.StartNew();

                _ = MatrixGridHarness.Render(grid, 1_000, 600);

                first.Stop();

                Stopwatch steady = Stopwatch.StartNew();
                const int Frames = 20;

                for (int frame = 0; frame < Frames; frame++)
                {
                    grid.SetVerticalOffset(frame * 17);
                    grid.InvalidateVisual();

                    _ = MatrixGridHarness.Render(grid, 1_000, 600);
                }

                steady.Stop();

                double perFrame = steady.Elapsed.TotalMilliseconds / Frames;

                output.WriteLine(
                    $"1500 x 1000: build {build.ElapsedMilliseconds} ms, first frame "
                    + $"{first.Elapsed.TotalMilliseconds:F1} ms, steady state {perFrame:F1} ms/frame "
                    + $"(render to bitmap included).");

                // Generous: this is a guard against an accidental full scan, which takes seconds.
                Assert.True(perFrame < 100, $"A frame took {perFrame:F1} ms, which suggests a full scan.");
            },
            TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void HitTestingAThousandColumnsIsNotALinearScan()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(
                MatrixGridTestData.LargeMatrix(WorstCaseRows, WorstCaseColumns),
                width: 1_000,
                height: 600);

            grid.SetHorizontalOffset(grid.ExtentWidth / 2);

            Stopwatch watch = Stopwatch.StartNew();

            for (int i = 0; i < 100_000; i++)
            {
                grid.MoveTo(new System.Windows.Point(500, 300));
            }

            watch.Stop();

            output.WriteLine($"100 000 hit tests over 1000 columns: {watch.ElapsedMilliseconds} ms.");

            Assert.True(
                watch.ElapsedMilliseconds < 5_000,
                $"100 000 hit tests took {watch.ElapsedMilliseconds} ms.");
        });
    }

    [Fact]
    public void AutomationChildrenAreVirtualisedToo()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(
                MatrixGridTestData.LargeMatrix(WorstCaseRows, WorstCaseColumns),
                width: 1_000,
                height: 600);

            MatrixGridAutomationPeer peer = new(grid);

            List<System.Windows.Automation.Peers.AutomationPeer> children = peer.GetChildren();

            // Fifteen visible data columns plus the frozen one, header and body: a few hundred, not
            // one and a half million.
            Assert.True(children.Count < 1_000, $"The peer produced {children.Count} children.");
            Assert.True(children.Count > 100);

            // The whole grid stays addressable through the grid pattern.
            Assert.Equal(WorstCaseRows, ((System.Windows.Automation.Provider.IGridProvider)peer).RowCount);
            Assert.Equal(WorstCaseColumns + 1, ((System.Windows.Automation.Provider.IGridProvider)peer).ColumnCount);
        });
    }
}
