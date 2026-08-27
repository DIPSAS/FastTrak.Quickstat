using System.Windows.Media;
using QuickStat.Controls.Dataset;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;
using Xunit;

namespace QuickStat.Tests.Ui.Controls;

/// <summary>
/// What the grid actually puts on screen, read back pixel by pixel.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MatrixGridCellPainterTests"/> proves the painting <i>decisions</i>; these prove the
/// decisions reach the bitmap, which is the only way to catch a renderer that draws the bands in the
/// wrong order, clips a frozen column away, or paints the current-row tint over the current cell.
/// </para>
/// <para>
/// Probe coordinates are derived from the documented geometry: header 18 tall, rows 17 tall, the
/// <c>PID</c> column 44 wide and data columns 64. In the anonymous default that puts <c>AGE</c> at
/// x 44-107, the haemoglobin column at 108-171 and <c>SEX</c> at 172-235; data row 0 spans y 18-34,
/// row 1 35-51 and row 2 52-68.
/// </para>
/// </remarks>
public class MatrixGridRenderTests
{
    private static readonly Color White = Color.FromRgb(0xFF, 0xFF, 0xFF);
    private static readonly Color FixedFill = Color.FromRgb(0xF4, 0xFB, 0xFB);
    private static readonly Color EmptyCell = Color.FromRgb(0xF5, 0xF5, 0xF5);
    private static readonly Color Moderate = Color.FromRgb(0xFF, 0xED, 0xBF);
    private static readonly Color CurrentCell = Color.FromRgb(0xC8, 0xD9, 0xE9);
    private static readonly Color CurrentRow = Color.FromRgb(0xF3, 0xF9, 0xFD);
    private static readonly Color GridLine = Color.FromRgb(0xE2, 0xE6, 0xE6);
    private static readonly Color FixedLine = Color.FromRgb(0xD0, 0xD6, 0xD6);

    [Fact]
    public void TheHeaderRowAndTheFrozenColumnTakeTheFixedFill()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridHarness harness = MatrixGridHarness.RenderMatrix(MatrixGridTestData.SmallMatrix());

            // Probes stay clear of the glyphs: "PID" is right-aligned against x = 41 and the data
            // headers are left-aligned from x + 3, so the far side of each cell is bare fill.
            Assert.Equal(FixedFill, harness.PixelAt(6, 6));
            Assert.Equal(FixedFill, harness.PixelAt(100, 6));
            Assert.Equal(FixedFill, harness.PixelAt(10, 26));
        });
    }

    [Fact]
    public void AValueIsWhiteAGapIsGreyAndARiskLadderColoursItsOwnCell()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridHarness harness = MatrixGridHarness.RenderMatrix(MatrixGridTestData.SmallMatrix());

            // Row 0 has AGE, so it is an ordinary white cell.
            Assert.Equal(White, harness.PixelAt(48, 26));

            // Row 0's haemoglobin is 10, which the ladder puts in the moderate band. This is the one
            // coloured cell in Docs/Screenshots/QuickStat bilde 3.png.
            Assert.Equal(Moderate, harness.PixelAt(112, 26));

            // Row 2 has no values at all, so every data cell is the known-empty grey.
            Assert.Equal(EmptyCell, harness.PixelAt(48, 60));
            Assert.Equal(EmptyCell, harness.PixelAt(176, 60));
        });
    }

    [Fact]
    public void TheCurrentCellOverridesTheColourUnderIt()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridHarness harness = MatrixGridHarness.RenderMatrix(
                MatrixGridTestData.SmallMatrix(),
                configure: grid =>
                {
                    grid.CurrentRowIndex = 0;
                    grid.CurrentColumnIndex = 1;
                });

            // Column 1 is the amber haemoglobin cell; the caret paints straight over it.
            Assert.Equal(CurrentCell, harness.PixelAt(112, 26));
        });
    }

    [Fact]
    public void TheCurrentRowTintsWithoutErasingARiskColour()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridHarness harness = MatrixGridHarness.RenderMatrix(
                MatrixGridTestData.SmallMatrix(),
                configure: grid =>
                {
                    grid.CurrentRowIndex = 0;
                    grid.CurrentColumnIndex = 0;
                });

            // The caret is on AGE.
            Assert.Equal(CurrentCell, harness.PixelAt(48, 26));

            // The amber cell in the same row keeps its colour, blended 50 % toward the tint.
            Assert.Equal(Color.FromRgb(243, 239, 221), harness.PixelAt(112, 26));

            // A plain cell in the same row takes the flat tint...
            Assert.Equal(CurrentRow, harness.PixelAt(176, 26));

            // ...and so does the PID cell, which beats the fixed fill.
            Assert.Equal(CurrentRow, harness.PixelAt(10, 26));

            // A different row is untouched.
            Assert.Equal(FixedFill, harness.PixelAt(10, 43));
        });
    }

    [Fact]
    public void GridLinesSeparateDataColumnsAndTheFrozenBlockGetsADarkerOne()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridHarness harness = MatrixGridHarness.RenderMatrix(MatrixGridTestData.SmallMatrix());

            // Right edge of the AGE column, and the bottom of data row 0.
            Assert.Equal(GridLine, harness.PixelAt(107, 26));
            Assert.Equal(GridLine, harness.PixelAt(48, 34));

            // The frozen block's right edge and the header row's underline are darker.
            Assert.Equal(FixedLine, harness.PixelAt(43, 26));
            Assert.Equal(FixedLine, harness.PixelAt(60, 17));
        });
    }

    [Fact]
    public void ThereIsNoVerticalLineInsideTheFrozenBlock()
    {
        StaTestRunner.Run(() =>
        {
            // Fully identified: PID 0-43, Født 44-107, Fødselsnummer 108-191, Navn 192-319.
            MatrixGridHarness harness = MatrixGridHarness.RenderMatrix(
                MatrixGridTestData.SmallMatrix(),
                width: 600,
                identification: PersonIdentification.Full);

            // Delphi removes goFixedVertLine, so the boundaries inside the block are plain fill.
            Assert.Equal(FixedFill, harness.PixelAt(43, 26));
            Assert.Equal(FixedFill, harness.PixelAt(107, 26));
            Assert.Equal(FixedFill, harness.PixelAt(191, 26));

            // Only the block's own trailing edge is a line.
            Assert.Equal(FixedLine, harness.PixelAt(319, 26));
        });
    }

    [Fact]
    public void NothingIsPaintedPastTheLastRowOrColumn()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridHarness harness = MatrixGridHarness.RenderMatrix(MatrixGridTestData.SmallMatrix());

            // Three rows end at y = 69, three data columns at x = 236.
            Assert.Equal(White, harness.PixelAt(300, 26));
            Assert.Equal(White, harness.PixelAt(48, 120));
            Assert.Equal(White, harness.PixelAt(300, 6));
        });
    }

    [Fact]
    public void ScrollingMovesTheDataBandAndLeavesTheFrozenBlockAlone()
    {
        StaTestRunner.Run(() =>
        {
            // Narrow enough that the three columns do not fit: a grid wider than its own content
            // clamps every offset to zero, which would make this test pass for the wrong reason.
            MatrixGridHarness harness = MatrixGridHarness.RenderMatrix(
                MatrixGridTestData.SmallMatrix(),
                width: 150,
                configure: grid => grid.SetHorizontalOffset(64));

            // AGE has scrolled away, so the amber haemoglobin cell now sits where AGE was.
            Assert.Equal(Moderate, harness.PixelAt(48, 26));

            // The frozen PID column has not moved.
            Assert.Equal(FixedFill, harness.PixelAt(10, 26));
            Assert.Equal(FixedLine, harness.PixelAt(43, 26));
        });
    }

    [Fact]
    public void VerticalScrollingPinsTheHeaderRow()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridHarness harness = MatrixGridHarness.RenderMatrix(
                MatrixGridTestData.LargeMatrix(rows: 200, columns: 5),
                configure: grid => grid.SetVerticalOffset(17 * 50));

            // The header is still at the top, whatever the offset.
            Assert.Equal(FixedFill, harness.PixelAt(60, 6));
            Assert.Equal(FixedLine, harness.PixelAt(60, 17));
        });
    }

    [Fact]
    public void AGridWithRowsButNoColumnsShowsOnlyTheIdentityBlock()
    {
        StaTestRunner.Run(() =>
        {
            // The Delphi always had one phantom data column here, painted clWebSnow #FFFAFA, because
            // ColCount was FixedCols + max(n, 1). The port has no such column, so the area right of
            // PID is plain background - the one place the two builds visibly differ.
            MatrixGridHarness harness = MatrixGridHarness.RenderMatrix(MatrixGridTestData.RowsWithoutColumns());

            Assert.Equal(FixedFill, harness.PixelAt(10, 26));
            Assert.Equal(White, harness.PixelAt(60, 26));
            Assert.Equal(White, harness.PixelAt(200, 26));
        });
    }

    [Fact]
    public void TheMissingObjectColourNeverReachesAPixel()
    {
        StaTestRunner.Run(() =>
        {
            // #FFFAFA is one shade off white and would be invisible if it did leak, so it is set to
            // magenta for this test. The Delphi painted it wherever its sparse cell array had no
            // object; the port has no such array, and the one place it is still computed - a header
            // cell, which has no MatrixCell behind it, exactly as the Delphi's fixed headers had no
            // TObject - is immediately overpainted by the fixed fill, as it was there.
            Color magenta = Color.FromRgb(0xFF, 0x00, 0xFF);

            MatrixGridHarness harness = MatrixGridHarness.RenderMatrix(
                MatrixGridTestData.SmallMatrix(),
                configure: grid =>
                {
                    grid.MissingObjectBackground = new SolidColorBrush(magenta);
                    grid.CurrentRowIndex = 0;
                    grid.CurrentColumnIndex = 0;
                });

            for (int y = 0; y < harness.Height; y++)
            {
                for (int x = 0; x < harness.Width; x++)
                {
                    Assert.NotEqual(magenta, harness.PixelAt(x, y));
                }
            }
        });
    }

    [Fact]
    public void ANullMatrixRendersAnEmptyBackgroundRatherThanThrowing()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridHarness harness = MatrixGridHarness.RenderMatrix(null);

            Assert.Equal(White, harness.PixelAt(10, 10));
            Assert.Equal(White, harness.PixelAt(200, 100));
        });
    }

    [Fact]
    public void WideColumnsWidensTheDataColumnsAndNotTheIdentityOnes()
    {
        StaTestRunner.Run(() =>
        {
            MatrixGridHarness harness = MatrixGridHarness.RenderMatrix(
                MatrixGridTestData.SmallMatrix(),
                width: 600,
                configure: grid => grid.DataColumnWidth = MatrixGrid.WideDataColumnWidth);

            // AGE now runs 44-163, so the amber cell starts at 164 rather than 108.
            Assert.Equal(White, harness.PixelAt(112, 26));
            Assert.Equal(Moderate, harness.PixelAt(168, 26));
            Assert.Equal(GridLine, harness.PixelAt(163, 26));

            // PID is still 44 wide.
            Assert.Equal(FixedLine, harness.PixelAt(43, 26));
        });
    }

    [Fact]
    public void TheGridDrawsItsCellTextInTheCultureItIsGiven()
    {
        StaTestRunner.Run(() =>
        {
            PersonMatrix matrix = MatrixGridTestData.SmallMatrix();
            MatrixGrid grid = MatrixGridHarness.CreateGrid(matrix, identification: PersonIdentification.Full);

            grid.CellCulture = MatrixGridTestData.Culture;

            Assert.Equal("3/12/1922", grid.GetDisplayCellText(0, 1));

            grid.CellCulture = MatrixGridTestData.NorwegianCulture;

            Assert.Equal("12.03.1922", grid.GetDisplayCellText(0, 1));
        });
    }
}
