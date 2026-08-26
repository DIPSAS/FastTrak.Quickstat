namespace QuickStat.Collectors;

/// <summary>
/// Whether a collector can be offered at all against the connected database.
/// </summary>
/// <remarks>
/// <para>
/// A concept the Delphi does not have, and the only genuinely new machinery the restored features
/// need (PORT-PLAN.md §5 Phase 4, R7). Exactly one collector uses it:
/// <c>QS_DRUG_ANTIBIOTIC_INTERMEDIATE</c> joins <c>KB.AntibioticResistance2</c> - the only reference
/// to the <c>KB</c> schema anywhere in the subsystem, absent from many customer databases, and
/// possibly present under the author's original misspelling <c>AntibioticRestistance2</c>. The join
/// is an <b>inner</b> join, so a missing table makes the query <em>fail</em> rather than return
/// nothing: the user ticks a box and gets an error, not an empty column.
/// </para>
/// <para>
/// <c>QS_DRUG_ANTIBIOTIC_RECOMMENDED</c>, its neighbour in the registry, is <b>not</b> gated. Its
/// nine ATC codes are written out in the statement and it touches no <c>KB</c> object
/// (<c>EPR.QA.SQL.pas:431-443</c>), so gating it would hide a working collector on every database
/// without the knowledge-base schema.
/// </para>
/// <para>
/// This is declared in Phase 1 rather than retrofitted in Phase 4 because seven Phase 2 agents
/// compile against <see cref="CollectorDescriptor"/>; adding a member to it afterwards is the
/// expensive version of the same change.
/// </para>
/// <para>
/// Declarative first - a list of object names probed in one round trip, which golden-file tests can
/// assert on - with an optional escape hatch for a condition that a name list cannot express.
/// </para>
/// </remarks>
public sealed record CollectorAvailability
{
    /// <summary>No conditions: the collector is always registered.</summary>
    public static CollectorAvailability Always { get; } = new();

    /// <summary>Requires one schema-qualified database object to resolve.</summary>
    /// <param name="objectName">For example <c>KB.AntibioticResistance2</c>.</param>
    /// <returns>The availability.</returns>
    public static CollectorAvailability RequiringDatabaseObject(string objectName) =>
        new() { RequiredDatabaseObjects = [objectName] };

    /// <summary>
    /// Schema-qualified database objects that must resolve, e.g. <c>KB.AntibioticResistance2</c>.
    /// </summary>
    /// <remarks>
    /// Probed with <c>OBJECT_ID(name) IS NOT NULL</c>. When one does not resolve the collector is
    /// skipped and the skip is logged at information level, so support can tell "the table is
    /// missing" from "the column is empty".
    /// </remarks>
    public IReadOnlyList<string> RequiredDatabaseObjects { get; init; } = [];

    /// <summary>
    /// Extra condition evaluated after the object probe, or <see langword="null"/> for none.
    /// </summary>
    /// <remarks>
    /// Both must hold. Keep predicates pure and cheap - they run while the registry is being built,
    /// on every project switch.
    /// </remarks>
    public Func<CollectorAvailabilityContext, bool>? Predicate { get; init; }
}
