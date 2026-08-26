using System.Globalization;

namespace QuickStat.Data;

/// <summary>
/// One row of a <see cref="SqlResultSet"/>, with accessors that reproduce Delphi's
/// <c>TField</c> null semantics exactly.
/// </summary>
/// <remarks>
/// <para>
/// The defaults are not cosmetic. Code such as
/// <c>fSuperuser := ( ReadInteger( FLD_SUPERUSER ) = 1 )</c>
/// (<c>CRF.Context.ActiveUser.pas:231</c>) and <c>dataset.Fields[2].AsFloat</c>
/// (<c>EPR.QA.Collector.Base.pas:159</c>) depends on null reading as zero rather than throwing, and
/// on a missing timestamp reading as <see cref="ZeroDate"/> rather than
/// <see cref="DateTime.MinValue"/>, because that is what downstream formatting was written against.
/// </para>
/// <para>
/// Conversions between column types follow <c>TField</c> too: <c>AsString</c> on a numeric column
/// renders it, <c>AsInteger</c> on a float column rounds it, <c>AsBoolean</c> on a numeric column is
/// "not zero". They use <see cref="CultureInfo.CurrentCulture"/> because Delphi's <c>FloatToStr</c>
/// and <c>DateToStr</c> did.
/// </para>
/// </remarks>
public readonly record struct SqlRow
{
    private readonly object?[]? _values;

    /// <summary>Wraps an already-materialised row.</summary>
    /// <param name="values">Column values in ordinal order; <c>NULL</c> is <see langword="null"/>.</param>
    internal SqlRow(object?[] values) => _values = values;

    /// <summary>Delphi's <c>TDateTime</c> zero, which <c>AsDateTime</c> yields for a null column.</summary>
    public static DateTime ZeroDate => new(1899, 12, 30);

    /// <summary>Number of columns in the row.</summary>
    public int FieldCount => _values?.Length ?? 0;

    /// <summary>Whether the column is <c>NULL</c>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <returns><see langword="true"/> when the value is <c>NULL</c>.</returns>
    public bool IsNull(int ordinal) => Raw(ordinal) is null;

    /// <summary>The raw value, or <see langword="null"/> for <c>NULL</c>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <returns>The boxed value.</returns>
    public object? GetValue(int ordinal) => Raw(ordinal);

    /// <summary>Reads an integer; <c>NULL</c> yields <paramref name="defaultValue"/>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <param name="defaultValue">Value for <c>NULL</c>. Delphi <c>AsInteger</c> yields 0.</param>
    /// <returns>The value.</returns>
    public int GetInt32(int ordinal, int defaultValue = 0) => Raw(ordinal) switch
    {
        null => defaultValue,
        int value => value,
        double value => (int)Math.Round(value, MidpointRounding.AwayFromZero),
        float value => (int)Math.Round(value, MidpointRounding.AwayFromZero),
        decimal value => (int)Math.Round(value, MidpointRounding.AwayFromZero),
        bool value => value ? 1 : 0,
        object value => Convert.ToInt32(value, CultureInfo.CurrentCulture),
    };

    /// <summary>Reads a 64-bit integer; <c>NULL</c> yields <paramref name="defaultValue"/>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <param name="defaultValue">Value for <c>NULL</c>.</param>
    /// <returns>The value.</returns>
    public long GetInt64(int ordinal, long defaultValue = 0) => Raw(ordinal) switch
    {
        null => defaultValue,
        long value => value,
        int value => value,
        double value => (long)Math.Round(value, MidpointRounding.AwayFromZero),
        decimal value => (long)Math.Round(value, MidpointRounding.AwayFromZero),
        bool value => value ? 1L : 0L,
        object value => Convert.ToInt64(value, CultureInfo.CurrentCulture),
    };

    /// <summary>Reads a string; <c>NULL</c> yields <paramref name="defaultValue"/>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <param name="defaultValue">Value for <c>NULL</c>. Delphi <c>AsString</c> yields the empty string.</param>
    /// <returns>The value.</returns>
    public string GetString(int ordinal, string defaultValue = "") => Raw(ordinal) switch
    {
        null => defaultValue,
        string value => value,
        char[] value => new string(value),
        object value => Convert.ToString(value, CultureInfo.CurrentCulture) ?? defaultValue,
    };

    /// <summary>Reads a double; <c>NULL</c> yields <paramref name="defaultValue"/>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <param name="defaultValue">Value for <c>NULL</c>. Delphi <c>AsFloat</c> yields 0.</param>
    /// <returns>The value.</returns>
    public double GetDouble(int ordinal, double defaultValue = 0) => Raw(ordinal) switch
    {
        null => defaultValue,
        double value => value,
        float value => value,
        decimal value => (double)value,
        int value => value,
        bool value => value ? 1d : 0d,
        object value => Convert.ToDouble(value, CultureInfo.CurrentCulture),
    };

    /// <summary>Reads a decimal; <c>NULL</c> yields <paramref name="defaultValue"/>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <param name="defaultValue">Value for <c>NULL</c>.</param>
    /// <returns>The value.</returns>
    public decimal GetDecimal(int ordinal, decimal defaultValue = 0) => Raw(ordinal) switch
    {
        null => defaultValue,
        decimal value => value,
        int value => value,
        long value => value,
        bool value => value ? 1m : 0m,
        object value => Convert.ToDecimal(value, CultureInfo.CurrentCulture),
    };

    /// <summary>Reads a boolean; <c>NULL</c> yields <paramref name="defaultValue"/>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <param name="defaultValue">Value for <c>NULL</c>. Delphi <c>AsBoolean</c> yields false.</param>
    /// <returns>The value.</returns>
    public bool GetBoolean(int ordinal, bool defaultValue = false) => Raw(ordinal) switch
    {
        null => defaultValue,
        bool value => value,
        int value => value != 0,
        long value => value != 0,
        byte value => value != 0,
        short value => value != 0,
        double value => value != 0,
        decimal value => value != 0,
        string value => ParseBoolean(value, defaultValue),
        object value => Convert.ToBoolean(value, CultureInfo.CurrentCulture),
    };

    /// <summary>Reads a timestamp; <c>NULL</c> yields <paramref name="defaultValue"/>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <param name="defaultValue">Value for <c>NULL</c>, or <see langword="null"/> for <see cref="ZeroDate"/>.</param>
    /// <returns>The value.</returns>
    public DateTime GetDateTime(int ordinal, DateTime? defaultValue = null) => Raw(ordinal) switch
    {
        null => defaultValue ?? ZeroDate,
        DateTime value => value,
        DateTimeOffset value => value.DateTime,
        DateOnly value => value.ToDateTime(TimeOnly.MinValue),
        object value => Convert.ToDateTime(value, CultureInfo.CurrentCulture),
    };

    private static bool ParseBoolean(string text, bool defaultValue)
    {
        if (bool.TryParse(text, out bool parsed))
        {
            return parsed;
        }

        if (text.Length == 0)
        {
            return defaultValue;
        }

        // Delphi's TStringField.GetAsBoolean accepts the first character of the true/false words.
        // 'J' covers the Norwegian 'Ja', which occurs in hand-written lookup tables.
        return text[0] is 'T' or 't' or 'Y' or 'y' or 'J' or 'j' or '1';
    }

    private object? Raw(int ordinal)
    {
        object?[] values = _values ?? [];

        if ((uint)ordinal >= (uint)values.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ordinal), ordinal, $"The row has {values.Length} column(s).");
        }

        return values[ordinal];
    }
}
