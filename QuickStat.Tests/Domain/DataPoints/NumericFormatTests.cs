using System.Globalization;
using QuickStat.Domain.DataPoints;
using Xunit;

namespace QuickStat.Tests.Domain.DataPoints;

/// <summary>
/// Delphi's <c>%g</c>. Every numeric cell and every exported number goes through this, so the
/// decimal separator, the trailing-zero removal and the fixed/scientific cut-over are all observable.
/// </summary>
public class NumericFormatTests
{
    private static readonly CultureInfo Norwegian = CultureInfo.GetCultureInfo("nb-NO");

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(-1, "-1")]
    [InlineData(97, "97")]
    [InlineData(1922, "1922")]
    [InlineData(19016, "19016")]
    [InlineData(100000, "100000")]
    public void WholeNumbersHaveNoDecimalSeparator(double value, string expected)
    {
        Assert.Equal(expected, NumericFormat.G(value, CultureInfo.InvariantCulture));
        Assert.Equal(expected, NumericFormat.G(value, Norwegian));
    }

    [Theory]
    [InlineData(3.5, "3.5", "3,5")]
    [InlineData(-3.5, "-3.5", "-3,5")]
    [InlineData(0.5, "0.5", "0,5")]
    [InlineData(18.5, "18.5", "18,5")]
    [InlineData(0.0001, "0.0001", "0,0001")]
    public void TheDecimalSeparatorComesFromTheCulture(double value, string invariant, string norwegian)
    {
        // PORT-PLAN.md §6: the CSV writes the locale separator, which is why InvariantGlobalization
        // must stay off.
        Assert.Equal(invariant, NumericFormat.G(value, CultureInfo.InvariantCulture));
        Assert.Equal(norwegian, NumericFormat.G(value, Norwegian));
    }

    [Theory]
    [InlineData(1.10, "1.1")]
    [InlineData(2.500, "2.5")]
    [InlineData(31.0, "31")]
    public void TrailingZeroesAreRemoved(double value, string expected) =>
        Assert.Equal(expected, NumericFormat.G(value, CultureInfo.InvariantCulture));

    [Theory]
    [InlineData(1e20, "1E20")]
    [InlineData(1e16, "1E16")]
    [InlineData(-1e20, "-1E20")]
    [InlineData(1e-5, "1E-5")]
    [InlineData(1.5e20, "1.5E20")]
    public void ScientificNotationHasNoPlusSignAndNoExponentPadding(double value, string expected)
    {
        // .NET's "G15" would write 1E+20 here.  Delphi writes 1E20, and a CSV consumer would see the
        // difference.
        Assert.Equal(expected, NumericFormat.G(value, CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(999999999999999, "999999999999999")] // fifteen digits still fit...
    [InlineData(1e15, "1E15")] // ...and the sixteenth tips it into scientific notation
    [InlineData(0.001, "0.001")] // the low cut-over is at 1e-4
    [InlineData(0.0001, "0.0001")]
    public void TheFixedRangeIsWideButNotUnbounded(double value, string expected) =>
        Assert.Equal(expected, NumericFormat.G(value, CultureInfo.InvariantCulture));

    [Fact]
    public void FifteenSignificantDigitsAreKept() =>
        Assert.Equal("1.23456789012346", NumericFormat.G(1.234567890123456789, CultureInfo.InvariantCulture));

    [Fact]
    public void TheMinusSignIsAsciiEvenWhereTheCultureDisagrees()
    {
        // Delphi's FloatToText writes a literal '-'.  ICU gives nb-NO U+2212 MINUS SIGN, which does
        // not even encode in the CP1252 the legacy CSV uses.
        string negative = NumericFormat.G(-3.5, Norwegian);

        Assert.Equal("-3,5", negative);
        Assert.Equal('-', negative[0]);
        Assert.DoesNotContain('−', negative);
    }

    [Fact]
    public void NegativeZeroRendersAsZero() =>
        Assert.Equal("0", NumericFormat.G(-0.0, CultureInfo.InvariantCulture));

    [Fact]
    public void NoProviderMeansTheCurrentCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = Norwegian;

            Assert.Equal("3,5", NumericFormat.G(3.5));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void NonFiniteValuesUseTheCultureSymbols()
    {
        Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NaNSymbol, NumericFormat.G(double.NaN, CultureInfo.InvariantCulture));
        Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.PositiveInfinitySymbol, NumericFormat.G(double.PositiveInfinity, CultureInfo.InvariantCulture));
        Assert.Equal(CultureInfo.InvariantCulture.NumberFormat.NegativeInfinitySymbol, NumericFormat.G(double.NegativeInfinity, CultureInfo.InvariantCulture));
    }
}
