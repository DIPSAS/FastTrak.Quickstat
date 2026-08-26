using System.Globalization;

namespace QuickStat.Export;

/// <summary>
/// Delphi's <c>Format('%g', [Value])</c>, which is what every numeric cell in the legacy CSV is.
/// </summary>
/// <remarks>
/// <para>
/// <b>The single entry point for numeric formatting in step 2.6.</b> Step 2.5 has its own
/// <c>NumericFormat.G</c> for the grid; the two were written in separate worktrees and neither could
/// see the other. Keeping this one type deliberately small and internal is what lets the duplicate
/// be reconciled at merge instead of being hunted through the writers.
/// </para>
/// <para>
/// This is <b>not</b> .NET's <c>ToString("G")</c>. The rules below were read out of the Delphi RTL
/// (<c>System.SysUtils.pas</c>: the <c>'G'</c> case of <c>Format</c> at line 13405 calls
/// <c>FloatToText(..., ffGeneral, Precision, 3, ...)</c>, and <c>ffGeneral</c> is implemented at
/// line 15390) and then confirmed by compiling and running a probe with the installed
/// <c>dcc32</c> 35.0 for Win32 - the same compiler family that built the shipping exe.
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Precision is 15 significant digits.</b> <c>Format</c> substitutes 15 when no precision is
///     given. So <c>1/3</c> is <c>0,333333333333333</c> and <c>0.1 + 0.2</c> is <c>0,3</c>.
///   </description></item>
///   <item><description>
///     <b>Fixed or scientific</b> is decided <em>after</em> rounding, on the count of digits before
///     the decimal point (<c>FloatRec.Exponent</c>): scientific when that count is above 15 or below
///     -3. In terms of the scientific exponent <c>x = d.ddd x 10^e</c> that is <c>e &gt;= 15</c> or
///     <c>e &lt;= -5</c> - which is exactly .NET's rule for <c>G15</c>, so
///     <see cref="double.ToString(string?, IFormatProvider?)"/> can be reused for the hard part.
///     <c>1e15</c> is <c>1E015</c> but <c>999999999999999</c> is written out in full; <c>1e-5</c> is
///     <c>1E-005</c> but <c>0.0001</c> is <c>0,0001</c>.
///   </description></item>
///   <item><description>
///     <b>The exponent is zero-padded to three digits and never carries a plus sign</b>, because
///     <c>Format</c> passes <c>ADigits = 3</c> and <c>ffGeneral</c> suppresses <c>'+'</c>. So
///     <c>1E015</c>, <c>1E-005</c>, <c>1E100</c>. .NET writes <c>1E+15</c>, <c>1E-05</c> and
///     <c>1E+100</c>, which is the only part that has to be rewritten.
///   </description></item>
///   <item><description>
///     <b>Only the decimal separator is localised.</b> The minus sign is the RTL's hardcoded
///     <c>'-'</c>, not the culture's <c>NegativeSign</c> - which matters because several ICU
///     cultures use U+2212 MINUS SIGN, a character Windows-1252 cannot even encode. Formatting is
///     therefore done invariantly and only the separator substituted.
///   </description></item>
///   <item><description>
///     <b>Trailing zeros are dropped and negative zero prints as <c>0</c>.</b> Specials are
///     <c>INF</c>, <c>-INF</c> and <c>NAN</c>, uppercase and unlocalised.
///   </description></item>
/// </list>
/// </remarks>
internal static class DelphiNumberFormat
{
    /// <summary>
    /// Significant digits. <c>Format</c> substitutes this when <c>%g</c> carries no precision
    /// (<c>System.SysUtils.pas:13394-13397</c>).
    /// </summary>
    public const int GeneralPrecision = 15;

    /// <summary>
    /// Minimum exponent digits, from the <c>ADigits = 3</c> that <c>Format</c> passes to
    /// <c>FloatToText</c> (<c>System.SysUtils.pas:13405</c>, used at <c>:15302-15305</c>).
    /// </summary>
    public const int MinimumExponentDigits = 3;

    /// <summary>Delphi's rendering of positive infinity.</summary>
    public const string PositiveInfinity = "INF";

    /// <summary>Delphi's rendering of negative infinity.</summary>
    public const string NegativeInfinity = "-INF";

    /// <summary>Delphi's rendering of not-a-number.</summary>
    public const string NotANumber = "NAN";

    private const string NetFormatSpecifier = "G15";

    /// <summary>Formats one value exactly as Delphi's <c>%g</c> would.</summary>
    /// <param name="value">The value.</param>
    /// <param name="culture">
    /// Culture supplying the decimal separator. Nothing else about it is consulted.
    /// </param>
    /// <returns>The formatted number.</returns>
    public static string Format(double value, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        if (double.IsNaN(value))
        {
            return NotANumber;
        }

        if (double.IsPositiveInfinity(value))
        {
            return PositiveInfinity;
        }

        if (double.IsNegativeInfinity(value))
        {
            return NegativeInfinity;
        }

        if (value == 0)
        {
            // Covers negative zero, which Delphi writes as "0" while .NET writes "-0".
            return "0";
        }

        string invariant = value.ToString(NetFormatSpecifier, CultureInfo.InvariantCulture);
        int exponentAt = invariant.IndexOf('E', StringComparison.Ordinal);
        string separator = culture.NumberFormat.NumberDecimalSeparator;

        if (exponentAt < 0)
        {
            return invariant.Replace(".", separator, StringComparison.Ordinal);
        }

        string mantissa = invariant[..exponentAt].Replace(".", separator, StringComparison.Ordinal);

        return string.Concat(mantissa, "E", FormatExponent(invariant.AsSpan(exponentAt + 1)));
    }

    /// <summary>
    /// Rewrites .NET's <c>+17</c> / <c>-05</c> exponent as Delphi's <c>017</c> / <c>-005</c>.
    /// </summary>
    private static string FormatExponent(ReadOnlySpan<char> exponent)
    {
        bool negative = exponent[0] == '-';

        if (exponent[0] is '+' or '-')
        {
            exponent = exponent[1..];
        }

        ReadOnlySpan<char> digits = exponent.TrimStart('0');

        if (digits.IsEmpty)
        {
            digits = "0";
        }

        int padding = Math.Max(MinimumExponentDigits - digits.Length, 0);

        return string.Concat(
            negative ? "-" : string.Empty,
            new string('0', padding),
            digits);
    }
}
