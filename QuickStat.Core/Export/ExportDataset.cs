using QuickStat.Domain.DataPoints;
using QuickStat.Domain.Matrix;

namespace QuickStat.Export;

/// <summary>
/// A locked matrix, flattened into exactly what a writer needs and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// The one seam between step 2.5's <see cref="PersonMatrix"/> and step 2.6's writers.
/// <see cref="FromMatrix"/> is the only code in the export path that touches the matrix, so the CSV
/// and xlsx writers are pure functions of a value that a unit test can build by hand. Byte-level
/// parity assertions and the privacy regression tests that PORT-PLAN.md R6 requires would otherwise
/// depend on a matrix implementation landing first.
/// </para>
/// <para>
/// Colour is deliberately absent. Cell appearance comes from <see cref="PersonMatrix.GetCell"/>,
/// which belongs to 2.5; the xlsx writer therefore does not shade cells yet.
/// </para>
/// </remarks>
public sealed class ExportDataset
{
    /// <summary>The data columns, excluding the four fixed identity columns.</summary>
    public required IReadOnlyList<ExportColumn> Columns { get; init; }

    /// <summary>The people, in matrix order.</summary>
    public required IReadOnlyList<ExportRow> Rows { get; init; }

    /// <summary>Projects a locked matrix.</summary>
    /// <param name="matrix">The dataset. Must be locked.</param>
    /// <returns>The flattened dataset.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="matrix"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// The matrix is not locked. Exporting an unlocked matrix wrote the literal <c>(not ready)</c>
    /// into every cell in the Delphi (<c>EPR.QA.Matrix.pas:236-237</c>); that is a defect, not a
    /// format, so it fails here instead (<c>Docs/Port/04-matrix-export.md</c> R-10).
    /// </exception>
    public static ExportDataset FromMatrix(PersonMatrix matrix)
    {
        ArgumentNullException.ThrowIfNull(matrix);

        if (!matrix.IsLocked)
        {
            throw new InvalidOperationException(
                "The matrix must be locked before it can be exported.");
        }

        IReadOnlyList<MatrixColumn> sourceColumns = matrix.Columns;
        IReadOnlyList<MatrixRow> sourceRows = matrix.Rows;

        var columns = new ExportColumn[sourceColumns.Count];

        for (int columnIndex = 0; columnIndex < sourceColumns.Count; columnIndex++)
        {
            MatrixColumn source = sourceColumns[columnIndex];
            columns[columnIndex] = new ExportColumn { VarName = source.VarName, Title = source.Title };
        }

        var rows = new ExportRow[sourceRows.Count];

        for (int rowIndex = 0; rowIndex < sourceRows.Count; rowIndex++)
        {
            MatrixRow source = sourceRows[rowIndex];
            var cells = new ExportCell[columns.Length];

            for (int columnIndex = 0; columnIndex < columns.Length; columnIndex++)
            {
                if (matrix.TryGetDataPoint(rowIndex, columnIndex, out DataPoint? dataPoint) &&
                    dataPoint is not null)
                {
                    cells[columnIndex] = new ExportCell
                    {
                        HasValue = true,
                        Value = dataPoint.Value,
                        Timestamp = dataPoint.Timestamp,
                        Caption = dataPoint.Caption,
                    };
                }
            }

            rows[rowIndex] = new ExportRow
            {
                PersonId = source.PersonId,
                DateOfBirth = source.DateOfBirth,
                NationalId = source.NationalId,
                FullName = source.FullName,
                Cells = cells,
            };
        }

        return new ExportDataset { Columns = columns, Rows = rows };
    }
}
