using QuickStat.Domain.Matrix;

namespace QuickStat.Export;

/// <summary>Writes a result matrix to a file.</summary>
/// <remarks>
/// Delphi: <c>TPersonGridData.SaveToFile</c> (<c>EPR.QA.Matrix.pas:442-495</c>), reached from
/// <c>Open this dataset in Excel</c> and <c>Save this dataset to CSV file</c>. Both go through the
/// same writer, so both are governed by the same options.
/// </remarks>
public interface IDatasetExporter
{
    /// <summary>Exports a matrix.</summary>
    /// <param name="matrix">The dataset. Must be locked and non-empty.</param>
    /// <param name="filePath">Destination. Overwritten if it exists.</param>
    /// <param name="options">Identification, timestamps, format, dialect.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The paths written and the dimensions.</returns>
    /// <remarks>
    /// The cell text written is the <b>raw</b> value, formatted <c>%g</c> - not the display text.
    /// Screen overrides such as <c>Ja</c>/<c>Nei</c>, <c>Rgm</c>/<c>AF</c>, <c>2016</c> and BMI's
    /// one decimal place are ignored on export. That is correct for analysis and it does surprise
    /// users who compare the two; do not "fix" it silently.
    /// </remarks>
    Task<DatasetExportResult> ExportAsync(
        PersonMatrix matrix,
        string filePath,
        DatasetExportOptions options,
        CancellationToken cancellationToken = default);
}
