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
/// Step 2.2 adds the backing storage; the surface below is the contract everything else compiles
/// against.
/// </para>
/// </remarks>
public readonly record struct SqlRow
{
    /// <summary>Delphi's <c>TDateTime</c> zero, which <c>AsDateTime</c> yields for a null column.</summary>
    public static DateTime ZeroDate => new(1899, 12, 30);

    /// <summary>Whether the column is <c>NULL</c>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <returns><see langword="true"/> when the value is <c>NULL</c>.</returns>
    public bool IsNull(int ordinal) => throw new NotImplementedException();

    /// <summary>The raw value, or <see langword="null"/> for <c>NULL</c>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <returns>The boxed value.</returns>
    public object? GetValue(int ordinal) => throw new NotImplementedException();

    /// <summary>Reads an integer; <c>NULL</c> yields <paramref name="defaultValue"/>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <param name="defaultValue">Value for <c>NULL</c>. Delphi <c>AsInteger</c> yields 0.</param>
    /// <returns>The value.</returns>
    public int GetInt32(int ordinal, int defaultValue = 0) => throw new NotImplementedException();

    /// <summary>Reads a 64-bit integer; <c>NULL</c> yields <paramref name="defaultValue"/>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <param name="defaultValue">Value for <c>NULL</c>.</param>
    /// <returns>The value.</returns>
    public long GetInt64(int ordinal, long defaultValue = 0) => throw new NotImplementedException();

    /// <summary>Reads a string; <c>NULL</c> yields <paramref name="defaultValue"/>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <param name="defaultValue">Value for <c>NULL</c>. Delphi <c>AsString</c> yields the empty string.</param>
    /// <returns>The value.</returns>
    public string GetString(int ordinal, string defaultValue = "") => throw new NotImplementedException();

    /// <summary>Reads a double; <c>NULL</c> yields <paramref name="defaultValue"/>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <param name="defaultValue">Value for <c>NULL</c>. Delphi <c>AsFloat</c> yields 0.</param>
    /// <returns>The value.</returns>
    public double GetDouble(int ordinal, double defaultValue = 0) => throw new NotImplementedException();

    /// <summary>Reads a decimal; <c>NULL</c> yields <paramref name="defaultValue"/>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <param name="defaultValue">Value for <c>NULL</c>.</param>
    /// <returns>The value.</returns>
    public decimal GetDecimal(int ordinal, decimal defaultValue = 0) => throw new NotImplementedException();

    /// <summary>Reads a boolean; <c>NULL</c> yields <paramref name="defaultValue"/>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <param name="defaultValue">Value for <c>NULL</c>. Delphi <c>AsBoolean</c> yields false.</param>
    /// <returns>The value.</returns>
    public bool GetBoolean(int ordinal, bool defaultValue = false) => throw new NotImplementedException();

    /// <summary>Reads a timestamp; <c>NULL</c> yields <paramref name="defaultValue"/>.</summary>
    /// <param name="ordinal">Zero-based column position.</param>
    /// <param name="defaultValue">Value for <c>NULL</c>, or <see langword="null"/> for <see cref="ZeroDate"/>.</param>
    /// <returns>The value.</returns>
    public DateTime GetDateTime(int ordinal, DateTime? defaultValue = null) => throw new NotImplementedException();
}
