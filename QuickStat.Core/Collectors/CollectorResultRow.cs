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
    /// <para>
    /// Where free-text form answers and ATC names arrive, and it is not merely cosmetic: a non-empty
    /// caption is written to the file <em>instead of</em> the number, in both the CSV and the xlsx
    /// writer. The grid draws it left-aligned and truncated to six characters; the export writes it
    /// in full.
    /// </para>
    /// <para>
    /// That is the point of <c>8486b3d09</c> (2022-05-06, "#489525: QuickStat skal kunne vise og
    /// eksportere tekstdata fra skjema"), the same commit that gave
    /// <c>SpSnapshotFormDataAll</c> its <c>dp.TextVal AS Caption</c> column, and it is how free-text
    /// form answers reach a file at all. <c>TPersonGridData.GetCellText</c> tests the caption first
    /// (<c>EPR.QA.Matrix.pas:242-246</c> on <c>origin/tarmscreening/develop</c>). The commit is on
    /// <b>both</b> tarmscreening refs, so the behaviour does not depend on how R12 was decided.
    /// </para>
    /// <para>
    /// <c>Docs/Port/04-matrix-export.md</c> §5.2 says the raw number always wins. It was written
    /// against this repository's <c>develop_old</c> copy, which predates the feature, and is the
    /// parity-baseline error PORT-PLAN.md R11 warns about. An earlier revision of this remark
    /// repeated it; do not restore it.
    /// </para>
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
