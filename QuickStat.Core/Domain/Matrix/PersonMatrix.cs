using QuickStat.Collectors;
using QuickStat.Domain.DataPoints;
using QuickStat.Domain.Patients;

namespace QuickStat.Domain.Matrix;

/// <summary>
/// The result dataset: people down, variables across. Owns its data.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TPersonGridData</c> (<c>EPR.QA.Matrix.pas:35</c>), which was <em>not</em> a standalone
/// model - it delegated all cell storage to the grid control through
/// <c>IPersonGridComponent</c> and read cells back out of a sparse array keyed by the string
/// <c>"col:row"</c>. Inverting that is the single largest structural change in the port
/// (<c>Docs/Port/04-matrix-export.md</c> R-11): the matrix owns rows and columns, and the view is a
/// projection.
/// </para>
/// <para>
/// Implementing <see cref="ICollectorResultSink"/> is what makes the matrix the destination of a
/// collector run without either side knowing about the other's internals.
/// </para>
/// </remarks>
public sealed class PersonMatrix : ICollectorResultSink
{
    /// <summary>The rows, sorted and materialised.</summary>
    public IReadOnlyList<MatrixRow> Rows => throw new NotImplementedException();

    /// <summary>The data columns, excluding the four fixed identity columns.</summary>
    public IReadOnlyList<MatrixColumn> Columns => throw new NotImplementedException();

    /// <summary>Whether the matrix has been frozen by <see cref="Lock"/>.</summary>
    public bool IsLocked => throw new NotImplementedException();

    /// <summary>
    /// Whether there is anything to export.
    /// </summary>
    /// <remarks>
    /// Delphi <c>HasData</c> counts <em>columns</em>, not rows - a population with people but no
    /// collected variables is "no data". Export must additionally require
    /// <see cref="IsLocked"/>: exporting an unlocked matrix writes the literal <c>(not ready)</c>
    /// into every single cell, and an empty population writes a phantom <c>"nil"</c> row.
    /// </remarks>
    public bool HasData => throw new NotImplementedException();

    /// <summary>Study the data belongs to.</summary>
    public int StudyId { get; set; }

    /// <summary>
    /// Row order. QuickStat always sets <see cref="MatrixSortOrder.PersonId"/> before loading.
    /// </summary>
    /// <remarks>
    /// Changing this after <see cref="Lock"/> is an error - the Delphi raised
    /// <c>'Can not change sort order after locking'</c> (<c>EPR.QA.Matrix.pas:512-520</c>), which is
    /// why the assignment happens before the population is prepared.
    /// </remarks>
    public MatrixSortOrder SortBy { get; set; }

    /// <summary>
    /// Column-order policy handed to each <see cref="VariableNameSet"/>. Defaults to
    /// <see cref="ColumnOrder.FirstSeen"/>, which is value zero.
    /// </summary>
    public ColumnOrder ColumnOrder { get; set; }

    /// <summary>Replaces the rows with a fresh cohort.</summary>
    /// <param name="patients">The loaded population.</param>
    /// <remarks>
    /// De-duplicates by <see cref="Patient.PersonId"/>, keeping the first occurrence, then sorts by
    /// <see cref="SortBy"/>. Both behaviours are inherited: the Delphi funnelled rows through a
    /// dictionary keyed on person id, so duplicates were silently dropped and any <c>ORDER BY</c>
    /// in the population procedure was discarded.
    /// </remarks>
    public void PreparePopulation(IEnumerable<Patient> patients) => throw new NotImplementedException();

    /// <summary>Drops the rows and unlocks.</summary>
    public void ClearPopulation() => throw new NotImplementedException();

    /// <summary>Drops the columns and every datapoint, keeping the rows.</summary>
    /// <remarks>Called at the start of every collect run.</remarks>
    public void ClearVariables() => throw new NotImplementedException();

    /// <summary>Drops everything.</summary>
    public void Clear() => throw new NotImplementedException();

    /// <summary>Appends the columns one collector produced, in that collector's order.</summary>
    /// <param name="variableNames">
    /// From <see cref="CollectorRunSummary.VariableNames"/>. The order here becomes the column order
    /// of the grid and of every exported file.
    /// </param>
    /// <remarks>
    /// De-duplicates across collectors, which the Delphi did not: two collectors emitting the same
    /// variable produced two identical columns, because <c>ContainsVariable</c> existed and was
    /// never called (<c>EPR.QA.Matrix.Column.pas:83</c>).
    /// </remarks>
    public void AddColumns(VariableNameSet variableNames) => throw new NotImplementedException();

    /// <summary>Freezes the matrix so it can be rendered and exported.</summary>
    public void Lock() => throw new NotImplementedException();

    /// <summary>Reads one cell's datapoint.</summary>
    /// <param name="rowIndex">Index into <see cref="Rows"/>.</param>
    /// <param name="columnIndex">Index into <see cref="Columns"/>.</param>
    /// <param name="dataPoint">The datapoint.</param>
    /// <returns><see langword="true"/> when the cell has a value.</returns>
    public bool TryGetDataPoint(int rowIndex, int columnIndex, out DataPoint? dataPoint) =>
        throw new NotImplementedException();

    /// <summary>Computes everything needed to render one cell.</summary>
    /// <param name="rowIndex">Index into <see cref="Rows"/>.</param>
    /// <param name="columnIndex">Index into <see cref="Columns"/>.</param>
    /// <returns>The cell.</returns>
    public MatrixCell GetCell(int rowIndex, int columnIndex) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool Add(string columnName, in CollectorResultRow row) => throw new NotImplementedException();
}
