using System.Globalization;
using System.Text;

namespace QuickStat.Domain.DataPoints;

/// <summary>
/// Delphi's <c>Format('%g', [Value])</c>, which is how every numeric cell and every exported value
/// is rendered.
/// </summary>
/// <remarks>
/// <para>
/// Not the same as any .NET standard format string. <c>%g</c> is <c>ffGeneral</c> with 15
/// significant digits: trailing zeros are removed, a decimal separator appears only when it is
/// needed, and scientific notation is used only outside a wide fixed range. .NET's <c>"G15"</c>
/// agrees on the common cases but writes <c>1E+20</c> where Delphi writes <c>1E20</c>, so the two
/// are not interchangeable for byte-parity work.
/// </para>
/// <para>
/// The decimal separator is the <em>locale's</em>, exactly as in the Delphi build, which is why
/// <c>InvariantGlobalization</c> must stay off (PORT-PLAN.md §6): on nb-NO a value of 3.5 exports as
/// <c>3,5</c> and downstream scripts depend on it.
/// </para>
/// </remarks>
public static class NumericFormat
{
    /// <summary>Significant digits Delphi's <c>%g</c> keeps.</summary>
    public const int Precision = 15;

    /// <summary>
    /// The minus sign, which is <b>not</b> taken from the culture.
    /// </summary>
    /// <remarks>
    /// Delphi's <c>FloatToText</c> writes a literal ASCII hyphen-minus and never consults
    /// <c>FormatSettings.NegativeSign</c>. .NET on ICU would give U+2212 MINUS SIGN for nb-NO, so
    /// deferring to the culture here would put a three-byte character into every negative value in
    /// a CP1252 export - where it does not even encode.
    /// </remarks>
    private const char NegativeSign = '-';

    /// <summary>Renders a value the way Delphi's <c>%g</c> does.</summary>
    /// <param name="value">The value.</param>
    /// <param name="formatProvider">
    /// Supplies the decimal separator; <see langword="null"/> means
    /// <see cref="CultureInfo.CurrentCulture"/>, matching Delphi's global <c>FormatSettings</c>.
    /// </param>
    /// <returns>The rendered value.</returns>
    public static string G(double value, IFormatProvider? formatProvider = null)
    {
        NumberFormatInfo numbers = NumberFormatInfo.GetInstance(formatProvider ?? CultureInfo.CurrentCulture);

        if (double.IsNaN(value))
        {
            return numbers.NaNSymbol;
        }

        if (double.IsPositiveInfinity(value))
        {
            return numbers.PositiveInfinitySymbol;
        }

        if (double.IsNegativeInfinity(value))
        {
            return numbers.NegativeInfinitySymbol;
        }

        // "E14" is 15 significant digits: one before the point and fourteen after.  Reading the
        // digits back out of it is the only way to get Delphi's "round first, then decide on a
        // layout" ordering; formatting twice would round twice.
        string scientific = Math.Abs(value).ToString("E14", CultureInfo.InvariantCulture);
        int exponentAt = scientific.IndexOf('E', StringComparison.Ordinal);

        string digits = string.Concat(scientific.AsSpan(0, 1), scientific.AsSpan(2, exponentAt - 2));
        digits = digits.TrimEnd('0');

        if (digits.Length == 0)
        {
            return "0";
        }

        // Delphi's Exponent counts the digits before the decimal point: value = 0.<digits> * 10^n.
        int exponent = int.Parse(scientific.AsSpan(exponentAt + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture) + 1;
        bool negative = double.IsNegative(value);

        return (exponent > Precision) || (exponent < -3)
            ? Scientific(digits, exponent, negative, numbers)
            : Fixed(digits, exponent, negative, numbers);
    }

    private static string Fixed(string digits, int exponent, bool negative, NumberFormatInfo numbers)
    {
        StringBuilder text = new(digits.Length + 8);

        if (negative)
        {
            text.Append(NegativeSign);
        }

        if (exponent <= 0)
        {
            text.Append('0').Append(numbers.NumberDecimalSeparator).Append('0', -exponent).Append(digits);
        }
        else if (exponent >= digits.Length)
        {
            text.Append(digits).Append('0', exponent - digits.Length);
        }
        else
        {
            text.Append(digits, 0, exponent).Append(numbers.NumberDecimalSeparator).Append(digits, exponent, digits.Length - exponent);
        }

        return text.ToString();
    }

    private static string Scientific(string digits, int exponent, bool negative, NumberFormatInfo numbers)
    {
        StringBuilder text = new(digits.Length + 10);

        if (negative)
        {
            text.Append(NegativeSign);
        }

        text.Append(digits[0]);

        if (digits.Length > 1)
        {
            text.Append(numbers.NumberDecimalSeparator).Append(digits, 1, digits.Length - 1);
        }

        // Delphi writes no '+' for a positive exponent and pads to no minimum width, so 1e20 is
        // "1E20" and 1e-5 is "1E-5".
        text.Append('E');

        int power = exponent - 1;

        if (power < 0)
        {
            text.Append(NegativeSign);
        }

        return text.Append(Math.Abs(power).ToString(CultureInfo.InvariantCulture)).ToString();
    }
}
