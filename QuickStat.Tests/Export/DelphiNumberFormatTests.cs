using System.Globalization;
using QuickStat.Export;
using Xunit;

namespace QuickStat.Tests.Export;

/// <summary>
/// <c>%g</c> parity. Every expectation below is the literal output of a Delphi probe.
/// </summary>
/// <remarks>
/// The probe was a console program compiled with the installed <c>dcc32</c> 35.0 for Win32 - the
/// same compiler family that produced the shipping QuickStat - with
/// <c>FormatSettings.DecimalSeparator := ','</c>, printing <c>Format('%g', [v])</c> for each value.
/// These are not derived from documentation: the Delphi help's claim that fixed point is used "if
/// the value is greater than or equal to 0.00001" is wrong, and the RTL's real threshold, visible
/// here, is 0.0001.
/// </remarks>
public class DelphiNumberFormatTests
{
    [Fact]
    public void NorwegianCultureStillHasACommaDecimalSeparator()
    {
        // If globalization is ever switched to invariant, every number in every legacy export
        // silently changes. Directory.Build.props pins InvariantGlobalization=false; this notices.
        Assert.Equal(",", ExportFixtures.Norwegian.NumberFormat.NumberDecimalSeparator);
    }

    [Theory]
    // Whole numbers: no decimal point, no trailing zeros.
    [InlineData(0d, "0")]
    [InlineData(1d, "1")]
    [InlineData(97d, "97")]
    [InlineData(1922d, "1922")]
    [InlineData(100d, "100")]
    [InlineData(1e5, "100000")]
    // The locale decimal separator, which is the whole reason CsvDialect.Legacy exists.
    [InlineData(3.5, "3,5")]
    [InlineData(-3.5, "-3,5")]
    [InlineData(0.5, "0,5")]
    [InlineData(0.1, "0,1")]
    [InlineData(2.5, "2,5")]
    [InlineData(1.25, "1,25")]
    [InlineData(2.675, "2,675")]
    [InlineData(0.01, "0,01")]
    [InlineData(0.001, "0,001")]
    [InlineData(9.99e-4, "0,000999")]
    // Fifteen significant digits, and rounding at the fifteenth.
    [InlineData(1d / 3d, "0,333333333333333")]
    [InlineData(2d / 3d, "0,666666666666667")]
    [InlineData(0.1 + 0.2, "0,3")]
    [InlineData(1.0000000000000002, "1")]
    [InlineData(123456.789, "123456,789")]
    [InlineData(123456789.123456789, "123456789,123457")]
    // Fixed point right up to fifteen digits before the point, scientific from sixteen.
    [InlineData(1e14, "100000000000000")]
    [InlineData(999999999999999d, "999999999999999")]
    [InlineData(1e15, "1E015")]
    [InlineData(999999999999999.9, "1E015")]
    [InlineData(1e16, "1E016")]
    [InlineData(1234567890123456d, "1,23456789012346E015")]
    [InlineData(123456789012345678d, "1,23456789012346E017")]
    [InlineData(12345678901234567890d, "1,23456789012346E019")]
    [InlineData(1e20, "1E020")]
    [InlineData(-1e20, "-1E020")]
    [InlineData(1e21, "1E021")]
    // Fixed point down to 0.0001, scientific from 0.00001 - not 0.00001 as the docs claim.
    [InlineData(1e-4, "0,0001")]
    [InlineData(0.00009999999999999999, "0,0001")]
    [InlineData(1e-5, "1E-005")]
    [InlineData(-1e-5, "-1E-005")]
    [InlineData(0.000012345, "1,2345E-005")]
    [InlineData(1e-9, "1E-009")]
    [InlineData(1e-10, "1E-010")]
    // The exponent is padded to three digits and never widened past its own length.
    [InlineData(1e-99, "1E-099")]
    [InlineData(1e-100, "1E-100")]
    [InlineData(1e100, "1E100")]
    [InlineData(1.5e-300, "1,5E-300")]
    [InlineData(1.7976931348623157e308, "1,79769313486232E308")]
    [InlineData(5e-324, "4,94065645841247E-324")]
    public void MatchesTheDelphiProbeOnNorwegian(double value, string expected) =>
        Assert.Equal(expected, DelphiNumberFormat.Format(value, ExportFixtures.Norwegian));

    [Theory]
    [InlineData(3.5, "3.5")]
    [InlineData(0.1 + 0.2, "0.3")]
    [InlineData(1e-5, "1E-005")]
    [InlineData(97d, "97")]
    public void UsesTheSuppliedCultureForTheSeparatorOnly(double value, string expected) =>
        Assert.Equal(expected, DelphiNumberFormat.Format(value, CultureInfo.InvariantCulture));

    [Fact]
    public void NegativeZeroLosesItsSignJustAsDelphiDoes()
    {
        // The Delphi probe prints "0" for -0.0; .NET's own G15 prints "-0".
        Assert.Equal("0", DelphiNumberFormat.Format(-0.0, ExportFixtures.Norwegian));
        Assert.Equal("-0", (-0.0).ToString("G15", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void NegativeNumbersUseAnAsciiHyphenWhateverTheCultureSays()
    {
        // Several ICU cultures render NegativeSign as U+2212, which Windows-1252 cannot encode at
        // all. Delphi's FloatToText hardcodes '-' (System.SysUtils.pas, CMinusSign), so this must
        // never go through the culture's sign.
        foreach (string name in new[] { "nb-NO", "sv-SE", "fi-FI", "lt-LT", "en-US" })
        {
            var culture = CultureInfo.GetCultureInfo(name);
            string formatted = DelphiNumberFormat.Format(-12.5, culture);

            Assert.StartsWith("-", formatted, StringComparison.Ordinal);
            Assert.DoesNotContain("−", formatted, StringComparison.Ordinal);
        }
    }

    [Theory]
    // Probed with the FPU exception mask open, since Delphi otherwise raises EZeroDivide before a
    // NaN can be produced. Neither value can reach a matrix from SQL Server, which has no
    // representation for them, but the spellings are the RTL's CSpecial constants either way.
    [InlineData(double.PositiveInfinity, "INF")]
    [InlineData(double.NegativeInfinity, "-INF")]
    [InlineData(double.NaN, "NAN")]
    public void SpecialValuesUseTheDelphiSpellings(double value, string expected) =>
        Assert.Equal(expected, DelphiNumberFormat.Format(value, ExportFixtures.Norwegian));
}
