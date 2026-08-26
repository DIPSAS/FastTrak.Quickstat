namespace QuickStat.Domain.Matrix;

/// <summary>
/// The order in which a collector's variables become matrix columns.
/// </summary>
/// <remarks>
/// <para>
/// This is a policy flag rather than a hardcoded choice because the two candidate behaviours both
/// exist in the Delphi lineage and they produce different files.
/// </para>
/// <para>
/// <b>The default is <see cref="FirstSeen"/>.</b> At the pinned library tip
/// (<c>origin/tarmscreening/develop</c>) <c>TDataCollector.VarNames</c> returns <c>FVarOrder</c>,
/// the insertion-ordered projection, and that is what every shipped QuickStat binary does - checked
/// across all nine candidate baselines, 9 of 9 (PORT-PLAN.md §8.5). For form data, insertion order
/// is on-form item order, because the query carries <c>ORDER BY mfi.OrderNumber</c>.
/// </para>
/// <para>
/// This repository's reduced <c>develop_old</c> copies return the sorted <c>FVarList</c> instead, so
/// <c>Docs/Port/03-collectors.md</c> §F.2 originally recommended <see cref="Alphabetical"/>. Its
/// own correction block overrides that. Defaulting to alphabetical would silently reorder the
/// columns of every exported file, and the mistake would not surface until Phase 5.
/// </para>
/// </remarks>
public enum ColumnOrder
{
    /// <summary>
    /// The order the rows first arrived - Delphi <c>FVarOrder</c>. The default.
    /// </summary>
    FirstSeen = 0,

    /// <summary>
    /// Ordinal alphabetical order - Delphi <c>FVarList</c>, which is <c>Sorted := true</c>.
    /// </summary>
    Alphabetical = 1,
}
