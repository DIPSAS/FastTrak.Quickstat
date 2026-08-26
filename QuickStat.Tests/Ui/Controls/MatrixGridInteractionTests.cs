using System.Windows;
using System.Windows.Input;
using QuickStat.Controls.Dataset;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;
using Xunit;

namespace QuickStat.Tests.Ui.Controls;

/// <summary>
/// Clicking, keyboard navigation, tooltips and the cell rectangle the floating hint anchors to.
/// </summary>
/// <remarks>
/// Presses go through <c>PressAt</c> rather than a synthesised <c>MouseButtonEventArgs</c>: WPF
/// resolves a mouse event's position from the real cursor, so there is no other way to click a
/// chosen cell without putting a window on screen.
/// </remarks>
public class MatrixGridInteractionTests
{
    [Fact]
    public void ClickingADataCellMovesTheCaretAndRaisesCellActivated()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());
            List<MatrixGridCellEventArgs> activated = [];

            grid.CellActivated += (_, e) => activated.Add(e);

            // Column 1 (haemoglobin) of row 1: x in 108-171, y in 35-51.
            Assert.True(grid.PressAt(new Point(120, 40)));

            Assert.Equal(1, grid.CurrentRowIndex);
            Assert.Equal(1, grid.CurrentColumnIndex);

            MatrixGridCellEventArgs single = Assert.Single(activated);

            Assert.Equal(1, single.RowIndex);
            Assert.Equal(1, single.ColumnIndex);
            Assert.True(single.IsDataCell);
        });
    }

    [Fact]
    public void ClickingAFixedCellSelectsTheRowAndLeavesTheColumnAlone()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());
            List<MatrixGridCellEventArgs> activated = [];

            grid.CellActivated += (_, e) => activated.Add(e);

            grid.PressAt(new Point(120, 22));

            Assert.Equal(1, grid.CurrentColumnIndex);

            // Now the PID cell of row 2: x in 0-43, y in 52-68.
            Assert.True(grid.PressAt(new Point(20, 60)));

            Assert.Equal(2, grid.CurrentRowIndex);

            // HandleFixedClick assigns Row and never Col, so the caret keeps its variable.
            Assert.Equal(1, grid.CurrentColumnIndex);

            // The event still reports NoIndex, because a fixed column is not a model column.
            Assert.Equal(MatrixGrid.NoIndex, activated[^1].ColumnIndex);
            Assert.False(activated[^1].IsDataCell);
        });
    }

    [Fact]
    public void ClickingTheHeaderDoesNotMoveTheCaret()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());
            int raised = 0;

            grid.CellActivated += (_, _) => raised++;
            grid.SetCurrentCell(1, 1);

            // Middle of the AGE header, clear of both resize grips.
            Assert.False(grid.PressAt(new Point(75, 9)));

            Assert.Equal(1, grid.CurrentRowIndex);
            Assert.Equal(1, grid.CurrentColumnIndex);
            Assert.Equal(0, raised);
        });
    }

    [Fact]
    public void ClickingPastTheLastRowChangesNothing()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());
            int raised = 0;

            grid.CellActivated += (_, _) => raised++;

            Assert.False(grid.PressAt(new Point(60, 150)));

            Assert.Equal(MatrixGrid.NoIndex, grid.CurrentRowIndex);
            Assert.Equal(0, raised);
        });
    }

    [Fact]
    public void DraggingAHeaderEdgeResizesThatColumnOnly()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());

            // Grab the right edge of the AGE column, at x = 108, and drag it 40 to the right.
            Assert.True(grid.PressAt(new Point(108, 9)));
            Assert.True(grid.IsResizingColumn);

            grid.MoveTo(new Point(148, 9));
            grid.ReleasePointer();

            Assert.False(grid.IsResizingColumn);

            // AGE is now 104 wide, so the haemoglobin column starts at 148.
            Assert.True(grid.TryGetCellBounds(0, 1, out Rect bounds));
            Assert.Equal(148, bounds.X);
            Assert.Equal(64, bounds.Width);
        });
    }

    [Fact]
    public void TogglingWideColumnsDiscardsAHandDrag()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());

            grid.PressAt(new Point(108, 9));
            grid.MoveTo(new Point(148, 9));
            grid.ReleasePointer();

            grid.DataColumnWidth = MatrixGrid.WideDataColumnWidth;
            grid.DataColumnWidth = MatrixGrid.NarrowDataColumnWidth;

            Assert.True(grid.TryGetCellBounds(0, 0, out Rect bounds));
            Assert.Equal(64, bounds.Width);
        });
    }

    [Fact]
    public void ArrowKeysWalkTheGridWithoutRaisingCellActivated()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());
            int raised = 0;

            grid.CellActivated += (_, _) => raised++;
            grid.SetCurrentCell(1, 1);
            grid.SetCurrentCell(2, 2);

            Assert.Equal(2, grid.CurrentRowIndex);
            Assert.Equal(2, grid.CurrentColumnIndex);

            // The floating hint moves on click and on nothing else (§G.2), so keyboard movement must
            // not raise the event that repositions it.
            Assert.Equal(0, raised);
        });
    }

    [Fact]
    public void SettingTheCurrentCellScrollsItIntoView()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(
                MatrixGridTestData.LargeMatrix(rows: 500, columns: 500),
                width: 300,
                height: 120);

            grid.SetCurrentCell(400, 400);

            Assert.True(grid.TryGetCellBounds(400, 400, out Rect bounds));
            Assert.InRange(bounds.X, grid.FrozenWidth, 300);
            Assert.InRange(bounds.Y, 18, 120);
        });
    }

    [Fact]
    public void CellBoundsAreRefusedForACellScrolledOutOfView()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(
                MatrixGridTestData.LargeMatrix(rows: 500, columns: 500),
                width: 300,
                height: 120);

            grid.SetCurrentCell(400, 400);

            // The hint panel hides itself rather than parking outside the window.
            Assert.False(grid.TryGetCellBounds(0, 0, out _));
        });
    }

    [Fact]
    public void CellBoundsGiveTheFloatingHintSomethingToAnchorTo()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());

            // §G.2: the hint sits at the cell's left edge, one row height below its top, offset by
            // 3,3. All the Dataset tab needs from this control is the cell rectangle.
            Assert.True(grid.TryGetCellBounds(1, 1, out Rect bounds));

            // Data column 1 starts at 44 + 64; data row 1 starts at 18 + 17.
            Assert.Equal(new Rect(108, 35, 64, 17), bounds);
        });
    }

    [Fact]
    public void CellBoundsForTheHeaderRowResolveToTheHeaderCell()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());

            Assert.True(grid.TryGetCellBounds(MatrixGrid.NoIndex, 0, out Rect bounds));

            Assert.Equal(new Rect(44, 0, 64, 18), bounds);
        });
    }

    [Fact]
    public void CellBoundsAreRefusedForACellThatDoesNotExist()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());

            Assert.False(grid.TryGetCellBounds(99, 0, out _));
            Assert.False(grid.TryGetCellBounds(0, 99, out _));
            Assert.False(MatrixGridHarness.CreateGrid(null).TryGetCellBounds(0, 0, out _));
        });
    }

    [Fact]
    public void MakeVisibleScrollsARectangleInsideTheBands()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(
                MatrixGridTestData.LargeMatrix(rows: 200, columns: 200),
                width: 300,
                height: 120);

            grid.SetHorizontalOffset(1_000);
            grid.SetVerticalOffset(1_000);

            // A rectangle above the header and left of the frozen block pulls the view back.
            _ = grid.MakeVisible(grid, new Rect(0, 0, 10, 10));

            Assert.Equal(1_000 - 44, grid.HorizontalOffset);
            Assert.Equal(1_000 - 18, grid.VerticalOffset);
        });
    }

    [Fact]
    public void ThePointerShowsAResizeCursorOnlyOverAHeaderEdge()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());

            grid.MoveTo(new Point(108, 9));

            Assert.Equal(Cursors.SizeWE, grid.Cursor);

            grid.MoveTo(new Point(75, 9));

            Assert.Null(grid.Cursor);

            // The grip is a header-row affordance only: goColSizing does not size from the body.
            grid.MoveTo(new Point(108, 40));

            Assert.Null(grid.Cursor);
        });
    }

    [Fact]
    public void CellBoundsForAFixedColumnResolveToThePersonIdCell()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());

            Assert.True(grid.TryGetCellBounds(1, MatrixGrid.NoIndex, out Rect bounds));

            Assert.Equal(new Rect(0, 35, 44, 17), bounds);
        });
    }

    [Fact]
    public void AHeaderCellHintsWithItsVariableDescription()
    {
        StaTestRunner.Run(() =>
        {
            PersonMatrix matrix = MatrixGridTestData.DescribedColumn(
                "DRUID.RED",
                "DDI-R",
                "Drug-Drug interactions, red level");

            MatrixGrid grid = MatrixGridHarness.CreateGrid(matrix);

            Assert.Equal("Drug-Drug interactions, red level", grid.GetDisplayCellToolTip(MatrixGrid.NoIndex, 1));
        });
    }

    [Fact]
    public void AHeaderWithNoDescriptionAndAFittingTitleHasNoTooltip()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());

            // "AGE" fits inside 64 units, and the lab caption query selects NULL AS VarDescription,
            // so there is nothing to say.
            Assert.Null(grid.GetDisplayCellToolTip(MatrixGrid.NoIndex, 1));
        });
    }

    [Fact]
    public void AnElidedHeaderHintsWithItsFullTitle()
    {
        StaTestRunner.Run(() =>
        {
            // The screenshots are full of these: NDV_INS…, INS_ALL…, NDV_TR…. Column titles fall
            // back to the raw variable name whenever the caption dictionary has nothing, and 64
            // units holds about nine characters.
            const string LongName = "NDV_INSULIN_TREATMENT_LATEST";

            PersonMatrix matrix = MatrixGridTestData.NewMatrix();

            matrix.PreparePopulation([MatrixGridTestData.Person(8)]);
            matrix.Add(LongName, MatrixGridTestData.Row(8, LongName, 3));
            matrix.AddColumns(MatrixGridTestData.Names(LongName));
            matrix.Lock();

            MatrixGrid grid = MatrixGridHarness.CreateGrid(matrix);

            Assert.True(grid.IsElided(LongName, 1, MatrixGridCellKind.ColumnHeader));
            Assert.Equal(LongName, grid.GetDisplayCellToolTip(MatrixGrid.NoIndex, 1));
        });
    }

    [Fact]
    public void ShortHeaderTextIsNotElided()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());

            Assert.False(grid.IsElided("AGE", 1, MatrixGridCellKind.ColumnHeader));
            Assert.False(grid.IsElided("", 1, MatrixGridCellKind.ColumnHeader));
        });
    }

    [Fact]
    public void ADataCellHintsWithTheDatapointDescription()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());

            string? hint = grid.GetDisplayCellToolTip(0, 2);

            Assert.NotNull(hint);
            Assert.StartsWith("NOR05172 = 10\nTimeStamp = ", hint, StringComparison.Ordinal);
            Assert.Contains("\nRowId = 8000\nUpdates = 1", hint, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void ACellWithNoDatapointHasNoTooltip()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());

            Assert.Null(grid.GetDisplayCellToolTip(2, 1));
        });
    }

    [Fact]
    public void AnElidedNameHintsWithTheWholeName()
    {
        StaTestRunner.Run(() =>
        {
            PersonMatrix matrix = MatrixGridTestData.NewMatrix();

            matrix.PreparePopulation(
                [MatrixGridTestData.Person(8, "Kvarsteinsæther-Bjørnstadhaugen", "Anne-Margrethe Kristine")]);

            MatrixGrid grid = MatrixGridHarness.CreateGrid(
                matrix,
                width: 600,
                identification: PersonIdentification.Full);

            // The 128-unit Navn column cannot hold that, and the ellipsis is the port's own doing -
            // the Delphi's identity cells had no hint at all because TPersonGridRow does not
            // implement ICellText.
            Assert.Equal("Kvarsteinsæther-Bjørnstadhaugen, Anne-Margrethe Kristine", grid.GetDisplayCellToolTip(0, 3));
        });
    }

    [Fact]
    public void HoverTracksTheCellUnderThePointer()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix());

            grid.MoveTo(new Point(120, 40));

            Assert.Equal(MatrixGridCellKind.Data, grid.Hover.Kind);
            Assert.Equal(1, grid.Hover.RowIndex);
            Assert.Equal(1, grid.Hover.ColumnIndex);

            grid.MoveTo(new Point(120, 900));

            Assert.False(grid.Hover.IsHit);
        });
    }

    [Fact]
    public void RefreshPicksUpColumnsAddedToTheSameMatrixInstance()
    {
        StaTestRunner.Run(() =>
        {
            PersonMatrix matrix = MatrixGridTestData.NewMatrix();

            matrix.PreparePopulation([MatrixGridTestData.Person(8)]);

            MatrixGrid grid = MatrixGridHarness.CreateGrid(matrix);

            Assert.Equal(1, grid.DisplayColumnCount);

            // A collect run mutates the bound instance and raises nothing, which is why Refresh
            // exists at all.
            matrix.Add("AGE", MatrixGridTestData.Row(8, "AGE", 97));
            matrix.AddColumns(MatrixGridTestData.Names("AGE"));

            grid.Refresh();

            Assert.Equal(2, grid.DisplayColumnCount);
            Assert.Equal("97", grid.GetDisplayCellText(0, 1));
        });
    }

    [Fact]
    public void ReplacingTheMatrixResetsTheCaretAndTheScrollOffsets()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(
                MatrixGridTestData.LargeMatrix(rows: 200, columns: 200),
                width: 300,
                height: 120);

            grid.SetCurrentCell(150, 150);

            Assert.True(grid.HorizontalOffset > 0);
            Assert.True(grid.VerticalOffset > 0);

            grid.Matrix = MatrixGridTestData.SmallMatrix();

            Assert.Equal(MatrixGrid.NoIndex, grid.CurrentRowIndex);
            Assert.Equal(MatrixGrid.NoIndex, grid.CurrentColumnIndex);
            Assert.Equal(0, grid.HorizontalOffset);
            Assert.Equal(0, grid.VerticalOffset);
        });
    }

    [Fact]
    public void ChangingTheIdentificationModeChangesTheFrozenBlock()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGrid grid = MatrixGridHarness.CreateGrid(MatrixGridTestData.SmallMatrix(), width: 600);

            Assert.Equal(1, grid.FrozenColumnCount);
            Assert.Equal(44, grid.FrozenWidth);

            grid.Identification = PersonIdentification.Full;

            Assert.Equal(4, grid.FrozenColumnCount);
            Assert.Equal(320, grid.FrozenWidth);
            Assert.Equal("Fødselsnummer", grid.GetDisplayCellText(MatrixGrid.NoIndex, 2));
            Assert.Equal("12032212345", grid.GetDisplayCellText(0, 2));

            grid.Identification = PersonIdentification.RandomPersonId;

            Assert.Equal(1, grid.FrozenColumnCount);
        });
    }
}
