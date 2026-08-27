using System.Windows.Media;
using QuickStat.Controls.Dataset;
using QuickStat.Domain.DataPoints;
using Xunit;

namespace QuickStat.Tests.Ui.Controls;

/// <summary>The colour conversion and brush cache at the Core-to-WPF boundary.</summary>
public class MatrixGridPaletteTests
{
    [Fact]
    public void ADomainColourCrossesToWpfChannelForChannel()
    {
        Color color = MatrixGridPalette.ToColor(RiskPalette.GraveRisk);

        Assert.Equal(Color.FromRgb(0xFF, 0x80, 0x80), color);
        Assert.Equal(255, color.A);
    }

    [Theory]
    [InlineData(0xFF, 0xFF, 0xFF, 0xF3, 0xF9, 0xFD)]   // an ordinary cell
    [InlineData(0xF5, 0xF5, 0xF5, 0xEE, 0xF3, 0xF9)]   // a known variable with no value
    public void TheCurrentRowTintRoundsHalfToEvenLikeTheDelphi(byte r, byte g, byte b, byte er, byte eg, byte eb)
    {
        // Both rows were read off the running 22.12.21.547 build: 213 data elements collected over
        // population 282, one cell clicked, and the grid's own pixels counted.  #F3F9FD covered the
        // white cells of the current row and #EEF3F9 the empty-variable cells - PORT-PLAN.md §8.14.
        //
        // Only half-to-even produces both, and it is the rule Delphi's Round follows because Round
        // is the FPU's (Emetra.VclUtil.ColorCalculator.pas:229-238 divides in floating point and
        // rounds).  An earlier revision truncated toward zero and this test asserted #F3F9FE, one
        // step out in blue.  Every channel here lands exactly on .5, because the tint is applied at
        // 50 %, so the tie-break rule is the whole answer rather than a rounding detail:
        //
        //   white       G 255 + round(-6.5) = 255 - 6 = 249    B 255 + round(-1.5) = 255 - 2 = 253
        //   whitesmoke  G 245 + round(-1.5) = 245 - 2 = 243    B 245 + round( 3.5) = 245 + 4 = 249
        Color blended = MatrixGridPalette.Blend(Color.FromRgb(r, g, b), Color.FromRgb(0xE7, 0xF2, 0xFC), 50);

        Assert.Equal(Color.FromRgb(er, eg, eb), blended);
    }

    [Theory]
    [InlineData(0, 0xFF, 0xFF, 0xFF)]
    [InlineData(100, 0xE7, 0xF2, 0xFC)]
    public void TheBlendEndpointsAreTheTwoInputs(int percent, byte r, byte g, byte b)
    {
        Color blended = MatrixGridPalette.Blend(Colors.White, Color.FromRgb(0xE7, 0xF2, 0xFC), percent);

        Assert.Equal(Color.FromRgb(r, g, b), blended);
    }

    [Fact]
    public void TheBlendIsDirectional()
    {
        // round(255 * 0.25) = round(63.75) = 64 either way; the two are not each other's complement
        // because both start from their own endpoint.
        Color forward = MatrixGridPalette.Blend(Colors.Black, Colors.White, 25);
        Color backward = MatrixGridPalette.Blend(Colors.White, Colors.Black, 25);

        Assert.Equal(Color.FromRgb(64, 64, 64), forward);
        Assert.Equal(Color.FromRgb(191, 191, 191), backward);
    }

    [Fact]
    public void TheSameColourAlwaysYieldsTheSameFrozenBrush()
    {
        MatrixGridPalette palette = new();

        SolidColorBrush first = palette.Brush(Colors.White);
        SolidColorBrush second = palette.Brush(Color.FromRgb(255, 255, 255));

        Assert.Same(first, second);
        Assert.True(first.IsFrozen);
        Assert.Equal(1, palette.Count);
    }

    [Fact]
    public void ThousandsOfLookupsAcrossAHandfulOfColoursStayAtAHandfulOfBrushes()
    {
        // The point of the cache: a 1500 x 1000 matrix draws millions of cells out of nine risk
        // colours, white and a grey. Allocating a brush per cell per frame would not survive it.
        MatrixGridPalette palette = new();
        Rgb[] ladder =
        [
            RiskPalette.NoRisk,
            RiskPalette.LowRisk,
            RiskPalette.MildRisk,
            RiskPalette.ModerateRisk,
            RiskPalette.HighRisk,
            RiskPalette.GraveRisk,
            RiskPalette.NoData,
            RiskPalette.EmptyCell,
        ];

        for (int i = 0; i < 10_000; i++)
        {
            _ = palette.Brush(MatrixGridPalette.ToColor(ladder[i % ladder.Length]));
        }

        Assert.Equal(ladder.Length, palette.Count);
    }
}
