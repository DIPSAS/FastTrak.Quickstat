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
}
