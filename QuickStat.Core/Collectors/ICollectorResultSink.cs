using QuickStat.Domain.Matrix;

namespace QuickStat.Collectors;

/// <summary>
/// Receives the rows a collector produced. The seam between running a collector (step 2.4) and
/// storing the result (step 2.5).
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TPersonGridData.AddData</c> drives the batching loop and the collector writes
/// datapoints straight into the grid rows (<c>EPR.QA.Matrix.pas:143</c>,
/// <c>EPR.QA.Collector.Base.pas:160-165</c>). Keeping a streaming seam rather than returning a
/// materialised row list avoids holding every row of a several-thousand-patient run twice.
/// </para>
/// <para>
/// The interface is declared in <c>Collectors</c> because it is the runner's contract; the matrix
/// implements it. See <c>Docs/Port/06-contracts.md</c> for why that placement was chosen.
/// </para>
/// </remarks>
public interface ICollectorResultSink
{
    /// <summary>Offers one row.</summary>
    /// <param name="columnName">
    /// The matrix column, already prefixed - see <see cref="CollectorResultRow.ColumnName"/>.
    /// </param>
    /// <param name="row">The row.</param>
    /// <returns>
    /// <see langword="false"/> when the row was not stored, either because
    /// <see cref="CollectorResultRow.PersonId"/> is not in the current cohort or because the cell
    /// already held a datapoint. The runner counts the rejections; unknown people are expected in
    /// bulk for <see cref="PidBinding.None"/> collectors and are logged as a total, not per row.
    /// </returns>
    bool Add(string columnName, in CollectorResultRow row);

    /// <summary>Creates the set a run accumulates its column names into.</summary>
    /// <returns>An empty set, configured with whatever column-order policy the sink wants.</returns>
    /// <remarks>
    /// <para>
    /// The runner has to build a <see cref="VariableNameSet"/> for
    /// <see cref="CollectorRunSummary.VariableNames"/>, and the set carries a
    /// <see cref="ColumnOrder"/> that decides the column order of the grid and of every exported
    /// file. Only the sink knows which policy is in force - <c>PersonMatrix.ColumnOrder</c> is a
    /// settable property - so the sink makes the set and the runner fills it.
    /// </para>
    /// <para>
    /// It has a default implementation because it is additive: it was added after the sink was
    /// already implemented, and every sink that does not care keeps
    /// <see cref="ColumnOrder.FirstSeen"/>, which is what ships.
    /// <c>PersonMatrix.CreateVariableNameSet</c> already had this exact signature and satisfies it
    /// without changing a line of step 2.5.
    /// </para>
    /// <para>
    /// Before this existed the runner constructed the set itself, so
    /// <c>PersonMatrix.ColumnOrder</c> was a property nobody read: flipping it to
    /// <see cref="ColumnOrder.Alphabetical"/> changed nothing, silently, and the mistake would not
    /// have surfaced until a byte-comparison in Phase 5.
    /// </para>
    /// </remarks>
    VariableNameSet CreateVariableNameSet() => new();
}
