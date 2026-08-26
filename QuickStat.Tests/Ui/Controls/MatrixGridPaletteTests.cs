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

    [Fact]
    public void TheDelphiBlendTruncatesTowardZeroRatherThanRounding()
    {
        // #FFFFFF blended 50 % with #E7F2FC. Rounding gives #F3FAFE; Delphi's integer division gives
        // #F3F9FE, which is the value 05-ui-spec.md §F.1 records as the current-row tint.
        Color blended = MatrixGridPalette.Blend(Colors.White, Color.FromRgb(0xE7, 0xF2, 0xFC), 50);

        Assert.Equal(Color.FromRgb(0xF3, 0xF9, 0xFE), blended);
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
        Color forward = MatrixGridPalette.Blend(Colors.Black, Colors.White, 25);
        Color backward = MatrixGridPalette.Blend(Colors.White, Colors.Black, 25);

        Assert.Equal(Color.FromRgb(63, 63, 63), forward);
        Assert.Equal(Color.FromRgb(192, 192, 192), backward);
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
