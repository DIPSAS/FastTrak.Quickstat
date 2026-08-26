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
    /// <para>
    /// A cell with no caption is written as the <b>raw</b> value, formatted <c>%g</c> - never the
    /// display text. The <c>DataPointRule.FormatValue</c> overrides that the grid applies
    /// (<c>Ja</c>/<c>Nei</c>, <c>Rgm</c>/<c>AF</c>, <c>2016</c>, BMI's one decimal place) are
    /// ignored on export. That is correct for analysis and it does surprise users who compare the
    /// two; do not "fix" it silently.
    /// </para>
    /// <para>
    /// A cell whose <c>DataPoint.Caption</c> is set is the exception: the caption is written
    /// <em>instead of</em> the number, and in full. That is not a display override leaking through -
    /// it is a feature, added upstream by <c>8486b3d09</c> (2022-05-06, "#489525: QuickStat skal
    /// kunne vise og eksportere tekstdata fra skjema"), and it is how free-text form answers reach a
    /// file at all. <c>TPersonGridData.GetCellText</c> tests the caption first
    /// (<c>EPR.QA.Matrix.pas:242-246</c> on <c>origin/tarmscreening/develop</c>). Note the grid
    /// truncates the caption to six characters and the export does not.
    /// </para>
    /// <para>
    /// <c>Docs/Port/04-matrix-export.md</c> §5.2 says the value always wins. That was written against
    /// this repository's <c>develop_old</c> copy of the library, which predates the feature, and is
    /// the parity-baseline error PORT-PLAN.md R11 warns about. The commit is present on <b>both</b>
    /// tarmscreening refs, so the behaviour does not depend on how R12 was decided.
    /// </para>
    /// </remarks>
    Task<DatasetExportResult> ExportAsync(
        PersonMatrix matrix,
        string filePath,
        DatasetExportOptions options,
        CancellationToken cancellationToken = default);
}
