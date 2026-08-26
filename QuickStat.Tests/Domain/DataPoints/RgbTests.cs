using QuickStat.Domain.DataPoints;
using Xunit;

namespace QuickStat.Tests.Domain.DataPoints;

/// <summary>
/// Delphi <c>TColor</c> literals are <c>$00BBGGRR</c>, reversed relative to HTML. Getting the byte
/// order wrong makes every colour in the application wrong in a way that still looks plausible, so
/// the conversion is pinned against literals whose correct answer is independently known.
/// </summary>
public class RgbTests
{
    [Theory]
    // Asymmetric literals - these are the ones a transcription error shows up on.
    [InlineData(0x008080FF, "#FF8080")] // clGraveRisk, salmon red
    [InlineData(0x00B3EFD1, "#D1EFB3")] // clLowRisk, pale green
    [InlineData(0x00BFEDFF, "#FFEDBF")] // clModerateRisk, pale amber
    [InlineData(0x00BFDBFF, "#FFDBBF")] // clHighRisk, pale orange
    [InlineData(0x00E7B2EE, "#EEB2E7")] // clDataPalePurple
    [InlineData(0x00FFF8F0, "#F0F8FF")] // clWebAliceBlue, whose real value is known independently
    [InlineData(0x00FAFAFF, "#FFFAFA")] // clWebSnow, likewise
    [InlineData(0x00008000, "#008000")] // clGreen
    [InlineData(0x000000FF, "#FF0000")] // clWebRed - the literal reads like blue
    [InlineData(0x00FF0000, "#0000FF")] // clBlue - and this one reads like red
    [InlineData(0x00008CFF, "#FF8C00")] // clWebDarkOrange
    // Symmetric literals - correct under either byte order, so they prove nothing on their own.
    [InlineData(0x00FFFFFF, "#FFFFFF")] // clWhite
    [InlineData(0x00BFFFFF, "#FFFFBF")] // clMildRisk
    [InlineData(0x00DCDCDC, "#DCDCDC")] // clWebGainsboro
    [InlineData(0x00F5F5F5, "#F5F5F5")] // clWebWhiteSmoke
    public void FromDelphiReversesTheByteOrder(int delphi, string expected) =>
        Assert.Equal(expected, Rgb.FromDelphi(delphi).ToHex());

    [Fact]
    public void FromDelphiPutsTheLowByteInRed()
    {
        Rgb colour = Rgb.FromDelphi(0x00332211);

        Assert.Equal(0x11, colour.R);
        Assert.Equal(0x22, colour.G);
        Assert.Equal(0x33, colour.B);
    }

    [Fact]
    public void FromDelphiIgnoresTheSystemColourFlagByte()
    {
        // The top byte carries the VCL's palette/system flag; no palette constant sets it, and a
        // stray one must not bleed into blue.
        Assert.Equal(Rgb.FromDelphi(0x008080FF), Rgb.FromDelphi(unchecked((int)0xFF8080FF)));
    }

    [Fact]
    public void ToHexIsSevenUppercaseCharacters()
    {
        string hex = new Rgb(0x0A, 0xB0, 0xCD).ToHex();

        Assert.Equal("#0AB0CD", hex);
        Assert.Equal(7, hex.Length);
    }

    [Theory]
    [InlineData("#FFFFFF")] // NoRisk
    [InlineData("#D1EFB3")] // LowRisk
    [InlineData("#FFFFBF")] // MildRisk
    [InlineData("#FFEDBF")] // ModerateRisk
    [InlineData("#FFDBBF")] // HighRisk
    [InlineData("#FF8080")] // GraveRisk
    [InlineData("#DCDCDC")] // NoData
    [InlineData("#EEB2E7")] // DataPalePurple
    [InlineData("#F0F8FF")] // AliceBlue
    [InlineData("#F5F5F5")] // EmptyCell
    [InlineData("#FFFAFA")] // MissingObject
    public void EveryPaletteEntryIsDistinct(string hex) =>
        Assert.Single(Palette, entry => entry == hex);

    private static readonly string[] Palette =
    [
        RiskPalette.NoRisk.ToHex(),
        RiskPalette.LowRisk.ToHex(),
        RiskPalette.MildRisk.ToHex(),
        RiskPalette.ModerateRisk.ToHex(),
        RiskPalette.HighRisk.ToHex(),
        RiskPalette.GraveRisk.ToHex(),
        RiskPalette.NoData.ToHex(),
        RiskPalette.DataPalePurple.ToHex(),
        RiskPalette.AliceBlue.ToHex(),
        RiskPalette.EmptyCell.ToHex(),
        RiskPalette.MissingObject.ToHex(),
    ];
}
