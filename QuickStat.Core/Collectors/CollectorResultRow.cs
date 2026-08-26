namespace QuickStat.Collectors;

/// <summary>
/// One row of a collector's result set, mapped from the fixed five-column positional contract.
/// </summary>
/// <remarks>
/// <para>
/// Every collector query - all 131 of them - must project at least five columns <b>in this exact
/// order</b>, because <c>TDataCollector.RunBatch</c> reads them by ordinal and never by name
/// (<c>EPR.QA.Collector.Base.pas</c>). Extra columns such as <c>OrderBy</c>, <c>rnk</c>,
/// <c>ReverseOrder</c> or <c>OrderNumber</c> are tolerated because they sit after position 4.
/// </para>
/// <para>
/// <see cref="ItemId"/> and <see cref="Caption"/> are the exception: they are looked up <em>by
/// name</em> and are optional, so a collector that does not project them simply leaves them at
/// their defaults.
/// </para>
/// </remarks>
public readonly record struct CollectorResultRow
{
    /// <summary>Ordinal of <see cref="PersonId"/>.</summary>
    public const int PersonIdOrdinal = 0;

    /// <summary>Ordinal of <see cref="VarName"/>.</summary>
    public const int VarNameOrdinal = 1;

    /// <summary>Ordinal of <see cref="Value"/>.</summary>
    public const int ValueOrdinal = 2;

    /// <summary>Ordinal of <see cref="Timestamp"/>.</summary>
    public const int TimestampOrdinal = 3;

    /// <summary>Ordinal of <see cref="RowId"/>.</summary>
    public const int RowIdOrdinal = 4;

    /// <summary>Number of columns every collector query must project.</summary>
    public const int RequiredColumnCount = 5;

    /// <summary>Name of the optional caption column, looked up by name.</summary>
    public const string CaptionColumnName = "Caption";

    /// <summary>Name of the optional item-id column, looked up by name.</summary>
    public const string ItemIdColumnName = "ItemId";

    /// <summary>Ordinal 0. Rows for a person outside the current batch are discarded and counted.</summary>
    public required int PersonId { get; init; }

    /// <summary>Ordinal 1. The unprefixed variable name; see <see cref="ColumnName"/>.</summary>
    public required string VarName { get; init; }

    /// <summary>Ordinal 2, read as a double. Every matrix cell value is a double.</summary>
    public required double Value { get; init; }

    /// <summary>
    /// Ordinal 3. Missing timestamps read as <see cref="QuickStat.Data.SqlRow.ZeroDate"/>, not
    /// <see cref="DateTime.MinValue"/>.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>Ordinal 4. Identity of the source row, used to distinguish duplicates.</summary>
    public required int RowId { get; init; }

    /// <summary>Optional <c>ItemId</c> column; zero when the query does not project it.</summary>
    public int ItemId { get; init; }

    /// <summary>
    /// Optional <c>Caption</c> column; <see langword="null"/> when absent.
    /// </summary>
    /// <remarks>
    /// Where free-text form answers and ATC names arrive. A datapoint with a caption is drawn
    /// left-aligned and displays the caption truncated to six characters instead of the numeric
    /// value - but the CSV export still writes the raw number
    /// (<c>Docs/Port/04-matrix-export.md</c> §5.2).
    /// </remarks>
    public string? Caption { get; init; }

    /// <summary>The matrix column this row belongs in.</summary>
    /// <param name="varPrefix"><see cref="CollectorDescriptor.VarPrefix"/>.</param>
    /// <returns><paramref name="varPrefix"/> concatenated with <see cref="VarName"/>.</returns>
    /// <remarks>
    /// Implemented rather than stubbed because the concatenation <em>is</em> the contract
    /// (<c>EPR.QA.Collector.Base.pas:157</c>): the prefix is not a separator-joined namespace, and
    /// inserting a dot would rename every column in every export. Several prefixes already end in a
    /// dot; several are empty.
    /// </remarks>
    public string ColumnName(string varPrefix) => varPrefix + VarName;
}
