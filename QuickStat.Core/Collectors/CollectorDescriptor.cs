namespace QuickStat.Collectors;

/// <summary>
/// Everything about a data element except how to build its SQL: the metadata the list, the saved
/// packages and the golden-file tests all work from.
/// </summary>
/// <remarks>
/// Pure data, deliberately. A descriptor can be written to a golden file and diffed, which is how a
/// 131-entry registry transcribed by hand gets verified without a database (PORT-PLAN.md R3).
/// </remarks>
public sealed record CollectorDescriptor
{
    /// <summary>
    /// Stable identity, e.g. <c>LAB.ANEMIA</c>. Persisted in
    /// <see cref="QuickStat.Domain.Packages.PackagedSelection.CollectorNames"/>.
    /// </summary>
    /// <remarks>
    /// Because it is persisted, a name is not free to change. Six collectors in the Delphi registry
    /// are constructed with the <em>wrong</em> name constant - <c>QST_LAB_LOW</c> registered as
    /// <c>LAB.TRUST2</c>, <c>QS_GBD_SBP_2M</c> as <c>GBD.WEIGHT_2M</c>, and so on
    /// (<c>Docs/Port/03-collectors.md</c> §A.12). Those are fixed in the port and the golden files
    /// pin the corrected names.
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>
    /// The exact Norwegian string shown in the data-element list, suffixes already applied.
    /// </summary>
    /// <remarks>
    /// Parity that must not drift (PORT-PLAN.md §6): titles are how users recognise rows, and
    /// <c>TryFindCollector</c> matches on the title as well as the name, so they must stay unique.
    /// Build them through <see cref="CollectorTitle"/> rather than by writing the suffix into the
    /// literal - registering <c>'Autommunitet (siste)'</c> when the class also appends
    /// <c>' (siste)'</c> is a mistake that has already been made once upstream and corrected.
    /// </remarks>
    public required string Title { get; init; }

    /// <summary>Family, for grouping and golden-file naming.</summary>
    public required CollectorKind Kind { get; init; }

    /// <summary>
    /// Prepended to every <c>VarName</c> the query returns, forming the matrix column name.
    /// </summary>
    /// <remarks>
    /// See <see cref="CollectorResultRow.ColumnName"/>. Often empty; often ends in a dot, e.g.
    /// <c>ATC_</c>, <c>DX.</c>, <c>LAST6M.</c>.
    /// </remarks>
    public string VarPrefix { get; init; } = "";

    /// <summary>How the person-id list reaches the server.</summary>
    public required PidBinding PidBinding { get; init; }

    /// <summary>
    /// Maximum people per statement. Ignored when <see cref="PidBinding"/> is
    /// <see cref="PidBinding.None"/>.
    /// </summary>
    /// <remarks>
    /// The Delphi's values are 1, 100, 200 and <c>maxint</c>, and they are accidents of history.
    /// They are transcribed faithfully so the first port is comparable against a Delphi trace; with
    /// a table-valued parameter in place they become policy rather than per-collector magic.
    /// </remarks>
    public int BatchSize { get; init; } = 100;

    /// <summary>Which studies this is registered for. <see cref="StudyGate.Always"/> means all.</summary>
    public StudyGate Gate { get; init; } = StudyGate.Always;

    /// <summary>Whether the connected database can support this collector at all.</summary>
    public CollectorAvailability Availability { get; init; } = CollectorAvailability.Always;
}
