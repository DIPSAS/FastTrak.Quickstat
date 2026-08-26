using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using QuickStat.Controls.Dataset;
using QuickStat.Domain.Anonymisation;
using Xunit;

namespace QuickStat.Tests.Ui.Controls;

/// <summary>
/// What a screen reader can reach: cells, their values, and the column headers.
/// </summary>
/// <remarks>
/// PORT-PLAN.md §5 Phase 3 budgets the automation peer by name as the accessibility cost of choosing
/// a custom control over <c>DataGrid</c>. These tests are what stop it from being skipped quietly:
/// they assert that every cell is addressable, that each one knows its coordinates and its column
/// header, and that the datapoint hint - the information the visible six characters do not carry -
/// is exposed as help text.
/// </remarks>
public class MatrixGridAutomationPeerTests
{
    [Fact]
    public void TheGridAnnouncesItselfAsADataGrid()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridAutomationPeer peer = Peer(out _);

            Assert.Equal(AutomationControlType.DataGrid, peer.GetAutomationControlType());
            Assert.Equal("MatrixGrid", peer.GetClassName());
            Assert.Equal("Dataset", peer.GetName());
        });
    }

    [Fact]
    public void TheGridAndTablePatternsAreBothOffered()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridAutomationPeer peer = Peer(out _);

            Assert.Same(peer, peer.GetPattern(PatternInterface.Grid));
            Assert.Same(peer, peer.GetPattern(PatternInterface.Table));
        });
    }

    [Fact]
    public void TheGridReportsItsRealDimensions()
    {
        StaTestRunner.Run(() =>
        {
            IGridProvider provider = Peer(out _);

            // Three people, one frozen PID column and three data columns.
            Assert.Equal(3, provider.RowCount);
            Assert.Equal(4, provider.ColumnCount);
        });
    }

    [Fact]
    public void EveryCellIsAddressableThroughTheGridPattern()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridAutomationPeer peer = Peer(out _);

            // Asserted on the peers rather than on GetItem's return: wrapping a peer in an
            // IRawElementProviderSimple needs a window handle, and there is no window here. What
            // GetItem decides - which cell, and whether it exists at all - is exactly this.
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    Assert.NotNull(peer.CellPeer(row, column));
                }
            }

            Assert.Null(peer.CellPeer(3, 0));
            Assert.Null(peer.CellPeer(0, 4));
            Assert.Null(peer.GetItem(3, 0));
        });
    }

    [Fact]
    public void ThePeerForOneCellIsStableBetweenLookups()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridAutomationPeer peer = Peer(out _);

            Assert.Same(peer.CellPeer(0, 1), peer.CellPeer(0, 1));
        });
    }

    [Fact]
    public void ACellNamesItselfWithItsOwnText()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridAutomationPeer peer = Peer(out _);

            Assert.Equal("8", peer.CellPeer(0, 0)!.GetName());
            Assert.Equal("97", peer.CellPeer(0, 1)!.GetName());
            Assert.Equal("10", peer.CellPeer(0, 2)!.GetName());

            // A cell with no datapoint stays genuinely empty rather than gaining a placeholder word.
            Assert.Equal("", peer.CellPeer(2, 1)!.GetName());
        });
    }

    [Fact]
    public void ACellAlsoExposesItsTextAsAReadOnlyValue()
    {
        StaTestRunner.Run(() =>
        {
            IValueProvider value = Peer(out _).CellPeer(0, 1)!;

            Assert.Equal("97", value.Value);
            Assert.True(value.IsReadOnly);
            Assert.Throws<InvalidOperationException>(() => value.SetValue("1"));
        });
    }

    [Fact]
    public void ACellKnowsWhereItIs()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridAutomationPeer peer = Peer(out _);
            IGridItemProvider item = peer.CellPeer(1, 2)!;

            Assert.Equal(1, item.Row);
            Assert.Equal(2, item.Column);
            Assert.Equal(1, item.RowSpan);
            Assert.Equal(1, item.ColumnSpan);
        });
    }

    [Fact]
    public void ACellPointsAtItsColumnHeaderSoAValueIsAnnouncedWithItsVariable()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridAutomationPeer peer = Peer(out _);
            MatrixGridCellAutomationPeer cell = peer.CellPeer(0, 1)!;

            Assert.Same(peer.CellPeer(MatrixGrid.NoIndex, 1), cell.ColumnHeaderPeer());
            Assert.Empty(((ITableItemProvider)cell).GetRowHeaderItems());

            // A header cell has no header of its own.
            Assert.Null(peer.CellPeer(MatrixGrid.NoIndex, 1)!.ColumnHeaderPeer());
        });
    }

    [Fact]
    public void TheHeaderRowIsExposedThroughTheTablePattern()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridAutomationPeer peer = Peer(out _);
            ITableProvider table = peer;

            Assert.Equal(4, peer.ColumnHeaderPeers().Count);
            Assert.Empty(table.GetRowHeaders());
            Assert.Equal(RowOrColumnMajor.RowMajor, table.RowOrColumnMajor);

            MatrixGridCellAutomationPeer header = peer.CellPeer(MatrixGrid.NoIndex, 0)!;

            Assert.Equal("PID", header.GetName());
            Assert.Equal(AutomationControlType.HeaderItem, header.GetAutomationControlType());
        });
    }

    [Fact]
    public void TheNorwegianHeadersSurviveIntoTheAutomationTree()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(
                MatrixGridTestData.SmallMatrix(),
                width: 700,
                identification: PersonIdentification.Full);

            MatrixGridAutomationPeer peer = new(grid);

            Assert.Equal("Født", peer.CellPeer(MatrixGrid.NoIndex, 1)!.GetName());
            Assert.Equal("Fødselsnummer", peer.CellPeer(MatrixGrid.NoIndex, 2)!.GetName());
            Assert.Equal("Navn", peer.CellPeer(MatrixGrid.NoIndex, 3)!.GetName());
        });
    }

    [Fact]
    public void ACellsHelpTextCarriesTheDatapointHint()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridAutomationPeer peer = Peer(out _);

            // The visible text is six characters at most; the hint is where the timestamp, row id
            // and update count live, and a screen reader has no other way to them.
            string help = peer.CellPeer(0, 2)!.GetHelpText();

            Assert.Contains("NOR05172 = 10", help, StringComparison.Ordinal);
            Assert.Contains("RowId = 8000", help, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ADataCellIsKeyboardFocusableAndAHeaderIsNot()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridAutomationPeer peer = Peer(out _);

            Assert.True(peer.CellPeer(0, 1)!.IsKeyboardFocusable());
            Assert.False(peer.CellPeer(MatrixGrid.NoIndex, 1)!.IsKeyboardFocusable());
            Assert.False(peer.CellPeer(0, 0)!.IsKeyboardFocusable());
        });
    }

    [Fact]
    public void FocusingACellMovesTheCaretToIt()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridAutomationPeer peer = Peer(out MatrixGrid grid);

            peer.CellPeer(2, 3)!.SetFocus();

            Assert.Equal(2, grid.CurrentRowIndex);

            // Display column 3 is data column 2, because one identity column is frozen.
            Assert.Equal(2, grid.CurrentColumnIndex);
        });
    }

    [Fact]
    public void ACellScrolledOutOfViewReportsItselfOffscreen()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(
                MatrixGridTestData.LargeMatrix(rows: 500, columns: 500),
                width: 300,
                height: 120);

            MatrixGridAutomationPeer peer = new(grid);

            Assert.False(peer.CellPeer(0, 1)!.IsOffscreen());
            Assert.True(peer.CellPeer(400, 1)!.IsOffscreen());
        });
    }

    [Fact]
    public void ACellHasNoBoundingRectangleWhenTheGridIsNotInAWindow()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridAutomationPeer peer = Peer(out _);

            // PointToScreen would throw without a presentation source, so the peer answers Empty
            // instead of taking the process down.
            Assert.Equal(System.Windows.Rect.Empty, peer.CellPeer(0, 1)!.GetBoundingRectangle());
        });
    }

    [Fact]
    public void InvalidatingTheStructureDropsTheCachedPeers()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridAutomationPeer peer = Peer(out _);
            MatrixGridCellAutomationPeer before = peer.CellPeer(0, 1)!;

            peer.InvalidateStructure();

            Assert.NotSame(before, peer.CellPeer(0, 1));
        });
    }

    [Fact]
    public void EachCellHasItsOwnStableAutomationId()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridAutomationPeer peer = Peer(out _);

            Assert.Equal("R0C1", peer.CellPeer(0, 1)!.GetAutomationId());
            Assert.Equal("R-1C1", peer.CellPeer(MatrixGrid.NoIndex, 1)!.GetAutomationId());
        });
    }

    [Fact]
    public void TheControlCreatesThisPeerForItself()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());

            Assert.IsType<MatrixGridAutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(grid));
        });
    }

    private static MatrixGridAutomationPeer Peer(out MatrixGrid grid)
    {
        grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());

        return new MatrixGridAutomationPeer(grid);
    }
}
