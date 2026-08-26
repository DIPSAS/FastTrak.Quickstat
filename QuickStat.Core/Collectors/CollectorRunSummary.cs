using QuickStat.Domain.Matrix;

namespace QuickStat.Collectors;

/// <summary>What one collector produced over one cohort.</summary>
/// <remarks>
/// The important member is <see cref="VariableNames"/>: it carries the <b>column order</b>, and the
/// matrix appends its columns from it. Everything else is diagnostics.
/// </remarks>
public sealed record CollectorRunSummary
{
    /// <summary>The collector that ran.</summary>
    public required CollectorDescriptor Descriptor { get; init; }

    /// <summary>
    /// The distinct column names this collector produced, in the order the matrix should add them.
    /// </summary>
    /// <remarks>
    /// See <see cref="VariableNameSet"/> and <see cref="ColumnOrder"/>. The Delphi accumulated
    /// these into a <c>TStringList</c> that was never cleared between runs, so re-running a
    /// collector against a different population kept variables discovered earlier and produced
    /// columns that were empty for everyone. Reset per run.
    /// </remarks>
    public required VariableNameSet VariableNames { get; init; }

    /// <summary>Rows accepted by the sink.</summary>
    public int RowsAccepted { get; init; }

    /// <summary>
    /// Rows discarded because the person was not in the cohort.
    /// </summary>
    /// <remarks>
    /// Delphi logs this as <c>Unknown patients found, n =%d</c>. It is a real diagnostic and must
    /// survive: for a <see cref="PidBinding.None"/> collector a large number here is normal, and a
    /// large number for an <see cref="PidBinding.IdList"/> collector is a defect.
    /// </remarks>
    public int RowsForUnknownPersons { get; init; }

    /// <summary>Statements issued, i.e. how many batches the cohort was split into.</summary>
    public int BatchCount { get; init; }
}
