namespace QuickStat.Export;

/// <summary>Which set of CSV conventions to write.</summary>
public enum CsvDialect
{
    /// <summary>
    /// Byte-for-byte what the Delphi produces. The default, and what existing consumers parse.
    /// </summary>
    /// <remarks>
    /// See <see cref="DatasetExportOptions"/> for the full specification. Every element of it is
    /// load-bearing for someone's downstream script, which is why it is reproduced rather than
    /// improved.
    /// </remarks>
    Legacy = 0,

    /// <summary>
    /// Conventional CSV: UTF-8 with a byte-order mark, quote only when necessary, no trailing
    /// separator, invariant decimal point, ISO dates throughout.
    /// </summary>
    /// <remarks>
    /// Offered because <see cref="Legacy"/> is genuinely awkward - a comma decimal separator inside
    /// a semicolon-separated file is only unambiguous by accident - but never the default, and
    /// never selected without the user asking.
    /// </remarks>
    Rfc4180 = 1,
}
