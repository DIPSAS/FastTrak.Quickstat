using System.Windows.Media;
using QuickStat.Controls.Dataset;
using QuickStat.Domain.DataPoints;
using QuickStat.Domain.Matrix;
using Xunit;

namespace QuickStat.Tests.Ui.Controls;

/// <summary>
/// The seven background rules and three text rules of <c>05-ui-spec.md</c> §C.3, in priority order.
/// </summary>
/// <remarks>
/// Every expected colour is written as a hex literal taken from §F.1 rather than computed from the
/// same constants the code uses, so a transcription error in the palette cannot make these pass.
/// </remarks>
public class MatrixGridCellPainterTests
{
    private static readonly MatrixGridColors Colors = new()
    {
        Default = Hex("#FFFFFF"),
        MissingObject = Hex("#FFFAFA"),
        Fixed = Hex("#F4FBFB"),
        CurrentCell = Hex("#C8D9E9"),
        CurrentRow = Hex("#F3F9FD"),
        CurrentRowTint = Hex("#E7F2FC"),
        Text = Hex("#202020"),
        FixedText = Hex("#035F66"),
    };

    [Fact]
    public void ACellWithNoObjectBehindItIsSnow()
    {
        MatrixGridCellPaint paint = Resolve(MatrixGridCellKind.Data, null);

        Assert.Equal(Hex("#FFFAFA"), paint.Background);
    }

    [Fact]
    public void ACellWithARiskColourKeepsIt()
    {
        MatrixGridCellPaint paint = Resolve(MatrixGridCellKind.Data, Cell(RiskPalette.ModerateRisk));

        Assert.Equal(Hex("#FFEDBF"), paint.Background);
    }

    [Fact]
    public void AKnownVariableWithNoValueIsWhiteSmoke()
    {
        MatrixGridCellPaint paint = Resolve(MatrixGridCellKind.Data, Cell(RiskPalette.EmptyCell, hasValue: false));

        Assert.Equal(Hex("#F5F5F5"), paint.Background);
    }

    [Fact]
    public void ACellWhoseRuleOffersNoColourIsWhite()
    {
        MatrixGridCellPaint paint = Resolve(MatrixGridCellKind.Data, Cell(background: null));

        Assert.Equal(Hex("#FFFFFF"), paint.Background);
    }

    [Fact]
    public void TheCurrentCellOverridesEveryOtherBackground()
    {
        foreach (Rgb? background in new Rgb?[] { null, RiskPalette.GraveRisk, RiskPalette.EmptyCell })
        {
            MatrixGridCellPaint paint = Resolve(
                MatrixGridCellKind.Data,
                Cell(background),
                isCurrentCell: true,
                isCurrentRow: true);

            Assert.Equal(Hex("#C8D9E9"), paint.Background);
        }
    }

    [Fact]
    public void AnUncolouredCellInTheCurrentRowTakesTheFlatTint()
    {
        MatrixGridCellPaint paint = Resolve(MatrixGridCellKind.Data, Cell(background: null), isCurrentRow: true);

        Assert.Equal(Hex("#F3F9FD"), paint.Background);
    }

    [Fact]
    public void AColouredCellInTheCurrentRowIsBlendedRatherThanFilled()
    {
        // Delphi BlendColors(#FFEDBF, #E7F2FC, 50): 255+(231-255)/2 = 243, 237+(242-237)/2 = 239,
        // 191+(252-191)/2 = 221.  Selecting a row must not hide a risk colour.
        MatrixGridCellPaint paint = Resolve(
            MatrixGridCellKind.Data,
            Cell(RiskPalette.ModerateRisk),
            isCurrentRow: true);

        Assert.Equal(Color.FromRgb(243, 239, 221), paint.Background);
    }

    [Fact]
    public void TheFlatTintAndTheBlendAgreeOnAWhiteCell()
    {
        // The two knobs are consistent by construction, which is the whole justification for having
        // both: CurrentRow is exactly what blending Default with CurrentRowTint produces.
        Assert.Equal(
            Colors.CurrentRow,
            MatrixGridPalette.Blend(Colors.Default, Colors.CurrentRowTint, MatrixGridCellPainter.CurrentRowBlendPercent));
    }

    [Fact]
    public void AnEmptyCellInTheCurrentRowIsBlendedToo()
    {
        // 245 + round(-7.0) = 238, 245 + round(-1.5) = 243, 245 + round(3.5) = 249, i.e. #EEF3F9 -
        // which is what the running 22.12.21.547 build paints over the empty-variable cells of the
        // current row (PORT-PLAN.md §8.14).  Truncating toward zero, as an earlier revision of
        // MatrixGridPalette.Blend did, gives #EEF4F8 and misses in two channels.
        MatrixGridCellPaint paint = Resolve(
            MatrixGridCellKind.Data,
            Cell(RiskPalette.EmptyCell, hasValue: false),
            isCurrentRow: true);

        Assert.Equal(Color.FromRgb(238, 243, 249), paint.Background);
    }

    [Fact]
    public void FixedCellsAndHeadersTakeTheFixedFill()
    {
        Assert.Equal(Hex("#F4FBFB"), Resolve(MatrixGridCellKind.FixedHeader, null, ordinal: 0).Background);
        Assert.Equal(Hex("#F4FBFB"), Resolve(MatrixGridCellKind.ColumnHeader, null).Background);
        Assert.Equal(Hex("#F4FBFB"), Resolve(MatrixGridCellKind.Fixed, Cell(background: null), ordinal: 0).Background);
    }

    [Fact]
    public void TheCurrentRowBeatsTheFixedFillInTheIdentityColumns()
    {
        // Delphi tests the current row before gdFixed (Grid.Study.pas:223-228), so the PID cell of
        // the selected row is tinted rather than left at FixedColor.
        MatrixGridCellPaint paint = Resolve(
            MatrixGridCellKind.Fixed,
            Cell(background: null),
            ordinal: FixedColumns.PersonId,
            isCurrentRow: true);

        Assert.Equal(Hex("#F3F9FD"), paint.Background);
    }

    [Fact]
    public void TheHeaderRowIsNeverTreatedAsTheCurrentRow()
    {
        // fCurrentRow can only ever hold a data row, because OnSelectCell is not raised for fixed
        // cells. Passing the flag anyway must not tint or embolden the header differently.
        MatrixGridCellPaint paint = Resolve(MatrixGridCellKind.ColumnHeader, null, isCurrentRow: true);

        Assert.Equal(Hex("#F4FBFB"), paint.Background);
    }

    [Fact]
    public void ThePersonIdColumnIsTealInTheHeaderAndInEveryRow()
    {
        Assert.Equal(
            Hex("#035F66"),
            Resolve(MatrixGridCellKind.FixedHeader, null, ordinal: FixedColumns.PersonId).Foreground);

        Assert.Equal(
            Hex("#035F66"),
            Resolve(MatrixGridCellKind.Fixed, Cell(background: null), ordinal: FixedColumns.PersonId).Foreground);
    }

    [Fact]
    public void EveryOtherColumnUsesTheDefaultTextColour()
    {
        Assert.Equal(Hex("#202020"), Resolve(MatrixGridCellKind.Data, Cell(background: null)).Foreground);

        Assert.Equal(
            Hex("#202020"),
            Resolve(MatrixGridCellKind.Fixed, Cell(background: null), ordinal: FixedColumns.Name).Foreground);
    }

    [Fact]
    public void ADatapointFontColourIsUsedButTheCurrentCellDropsIt()
    {
        MatrixCell coloured = new()
        {
            Text = "3.5",
            Foreground = RiskPalette.GraveRisk,
            HasValue = true,
        };

        Assert.Equal(Hex("#FF8080"), Resolve(MatrixGridCellKind.Data, coloured).Foreground);

        Assert.Equal(
            Hex("#202020"),
            Resolve(MatrixGridCellKind.Data, coloured, isCurrentCell: true, isCurrentRow: true).Foreground);
    }

    [Fact]
    public void TheHeaderRowAndTheCurrentRowAreEmphasised()
    {
        Assert.True(Resolve(MatrixGridCellKind.ColumnHeader, null).Bold);
        Assert.True(Resolve(MatrixGridCellKind.FixedHeader, null, ordinal: 0).Bold);
        Assert.True(Resolve(MatrixGridCellKind.Data, Cell(background: null), isCurrentRow: true).Bold);
        Assert.False(Resolve(MatrixGridCellKind.Data, Cell(background: null)).Bold);
    }

    [Fact]
    public void ADataColumnHeaderIsLeftAlignedButAPersonIdHeaderIsNot()
    {
        // The spec's blanket "the header row is left-aligned with ellipsis" is true only of data
        // columns: HandleCellDraw re-applies DT_END_ELLIPSIS at :200-201 solely for ACol >=
        // FixedCols, so PID keeps the right alignment IsTextColumn gave it, over its own numbers.
        Assert.True(Resolve(MatrixGridCellKind.ColumnHeader, null).AlignLeft);
        Assert.False(Resolve(MatrixGridCellKind.FixedHeader, null, ordinal: FixedColumns.PersonId).AlignLeft);
    }

    [Theory]
    [InlineData(FixedColumns.PersonId, false)]
    [InlineData(FixedColumns.DateOfBirth, true)]
    [InlineData(FixedColumns.NationalId, true)]
    [InlineData(FixedColumns.Name, true)]
    public void TheThreeTextIdentityColumnsAreLeftAligned(int ordinal, bool expected)
    {
        Assert.Equal(expected, Resolve(MatrixGridCellKind.FixedHeader, null, ordinal).AlignLeft);
        Assert.Equal(expected, Resolve(MatrixGridCellKind.Fixed, Cell(background: null), ordinal).AlignLeft);
    }

    [Fact]
    public void ADataCellFollowsTheCellsOwnAlignment()
    {
        MatrixCell captioned = new() { Text = "Ja", AlignLeft = true, HasValue = true };

        Assert.True(Resolve(MatrixGridCellKind.Data, captioned).AlignLeft);
        Assert.False(Resolve(MatrixGridCellKind.Data, Cell(background: null)).AlignLeft);
    }

    private static MatrixGridCellPaint Resolve(
        MatrixGridCellKind kind,
        MatrixCell? cell,
        int ordinal = MatrixGrid.NoIndex,
        bool isCurrentCell = false,
        bool isCurrentRow = false) =>
        MatrixGridCellPainter.Resolve(kind, cell, ordinal, isCurrentCell, isCurrentRow, Colors);

    private static MatrixCell Cell(Rgb? background, bool hasValue = true) => new()
    {
        Text = hasValue ? "42" : "",
        Background = background,
        HasValue = hasValue,
    };

    private static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);
}
