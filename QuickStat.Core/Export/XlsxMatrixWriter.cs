using System.Globalization;
using ClosedXML.Excel;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.DataPoints;
using QuickStat.Domain.Matrix;

namespace QuickStat.Export;

/// <summary>
/// Writes a dataset as a real Excel workbook, with typed cell values.
/// </summary>
/// <remarks>
/// <para>
/// New in the port (PORT-PLAN.md §7.3). <c>Open this dataset in Excel</c> writes a CSV to
/// <c>%TEMP%</c> and hands it to Excel, so every number makes a round trip through locale-formatted
/// text on the way and every national id risks being re-read as a number. <b>Nothing here is
/// formatted into a string first</b>: numbers go in as <see cref="double"/>, dates as
/// <see cref="DateTime"/> with an explicit ISO number format, and national ids as text with an
/// explicit text format so their leading zeros survive. Windows-1252 and the decimal separator
/// simply do not arise, and that is the point of the format.
/// </para>
/// <para>
/// Identification is the same single derivation as the CSV -
/// <see cref="IdentificationColumns.For"/> through <see cref="DatasetExportOptions.Columns"/> - so
/// an omitted column is omitted here too. There is no second privacy path.
/// </para>
/// <para>
/// Cells are shaded from the same <see cref="MatrixCell"/> the screen uses, so the fourteen
/// hardcoded threshold ladders reach the workbook. Two fills are skipped, both deliberately:
/// <see cref="RiskPalette.NoRisk"/>, which is pure white and therefore indistinguishable from
/// Excel's default, and cells with no datapoint, which are not written at all - shading them
/// <see cref="RiskPalette.EmptyCell"/> as the grid does would mean materialising every hole in a
/// sparse matrix, and a realistic worst case is 1 500 columns by 1 000 rows.
/// </para>
/// </remarks>
public static class XlsxMatrixWriter
{
    /// <summary>Excel's hard column ceiling.</summary>
    /// <remarks>
    /// With timestamps enabled every variable costs two columns, so the practical ceiling is about
    /// 8 190 variables (<c>Docs/Port/04-matrix-export.md</c> R-15). The Delphi never hit it because
    /// it only ever wrote CSV.
    /// </remarks>
    public const int MaximumColumns = 16_384;

    /// <summary>Worksheet name.</summary>
    public const string WorksheetName = "QuickStat";

    /// <summary>Number format for every date cell. Excel patterns are lowercase.</summary>
    public const string DateNumberFormat = "yyyy-mm-dd";

    /// <summary>Number format that keeps a national id's leading zeros.</summary>
    public const string TextNumberFormat = "@";

    /// <summary>Writes the dataset to a stream.</summary>
    /// <param name="dataset">The dataset.</param>
    /// <param name="stream">Destination. ClosedXML writes the whole package into it.</param>
    /// <param name="options">Identification and timestamps. Dialect and encoding do not apply.</param>
    /// <param name="anonymiser">
    /// Required when <see cref="IdentificationColumns.UsesPseudonyms"/> is set.
    /// </param>
    /// <param name="cancellationToken">Cancels between rows.</param>
    /// <exception cref="ArgumentException">
    /// <see cref="PersonIdentification.RandomPersonId"/> without an anonymiser.
    /// </exception>
    /// <exception cref="InvalidOperationException">The dataset needs more columns than Excel has.</exception>
    public static void Write(
        ExportDataset dataset,
        Stream stream,
        DatasetExportOptions options,
        IAnonymiser? anonymiser = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(options);

        IdentificationColumns columns = options.Columns;

        if (columns.UsesPseudonyms && anonymiser is null)
        {
            throw new ArgumentException(
                $"{PersonIdentification.RandomPersonId} needs an anonymiser.",
                nameof(anonymiser));
        }

        int columnCount = CsvMatrixWriter.CountColumns(dataset, options);

        if (columnCount > MaximumColumns)
        {
            throw new InvalidOperationException(
                $"This dataset needs {columnCount} columns and Excel allows {MaximumColumns}. " +
                "Turn off the timestamp columns, collect fewer data elements, or export to CSV.");
        }

        using var workbook = new XLWorkbook();
        IXLWorksheet sheet = workbook.AddWorksheet(WorksheetName);

        int identityColumnCount = WriteHeaderRow(sheet, dataset, columns, options.IncludeTimestamps);

        for (int rowIndex = 0; rowIndex < dataset.Rows.Count; rowIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteDataRow(sheet, dataset, dataset.Rows[rowIndex], rowIndex + 2, columns, options, anonymiser);
        }

        sheet.SheetView.Freeze(1, identityColumnCount);
        sheet.Range(1, 1, Math.Max(dataset.Rows.Count + 1, 2), Math.Max(columnCount, 1)).SetAutoFilter();

        workbook.SaveAs(stream);
    }

    private static int WriteHeaderRow(
        IXLWorksheet sheet,
        ExportDataset dataset,
        IdentificationColumns columns,
        bool includeTimestamps)
    {
        int column = 1;

        // The same derivation the CSV writer and the grid use.
        foreach (string header in FixedColumns.HeadersFor(columns))
        {
            sheet.Cell(1, column++).Value = header;
        }

        int identityColumnCount = column - 1;

        foreach (ExportColumn dataColumn in dataset.Columns)
        {
            // The VarName, exactly as in the CSV: it is the identity downstream scripts read.
            // The title goes in the comment so the workbook is still readable by a human.
            IXLCell headerCell = sheet.Cell(1, column++);
            headerCell.Value = dataColumn.VarName;

            if (!string.IsNullOrEmpty(dataColumn.Title) &&
                !string.Equals(dataColumn.Title, dataColumn.VarName, StringComparison.Ordinal))
            {
                headerCell.GetComment().AddText(dataColumn.Title);
            }

            if (includeTimestamps)
            {
                sheet.Cell(1, column++).Value =
                    dataColumn.VarName + DatasetExportOptions.TimestampColumnSuffix;
            }
        }

        sheet.Row(1).Style.Font.Bold = true;

        return identityColumnCount;
    }

    private static void WriteDataRow(
        IXLWorksheet sheet,
        ExportDataset dataset,
        ExportRow row,
        int sheetRow,
        IdentificationColumns columns,
        DatasetExportOptions options,
        IAnonymiser? anonymiser)
    {
        int column = 1;

        foreach (int ordinal in FixedColumns.VisibleOrdinals(columns))
        {
            IXLCell cell = sheet.Cell(sheetRow, column++);

            switch (ordinal)
            {
                case FixedColumns.PersonId:
                    cell.Value = columns.UsesPseudonyms
                        ? anonymiser!.GetPseudonym(row.PersonId)
                        : row.PersonId;
                    break;

                case FixedColumns.DateOfBirth:
                    if (row.DateOfBirth is { } dateOfBirth)
                    {
                        cell.Value = dateOfBirth;
                        cell.Style.DateFormat.Format = DateNumberFormat;
                    }

                    break;

                case FixedColumns.NationalId:
                    // Text, and marked as text: a Norwegian national id starting with a zero must
                    // not be re-read as a number by Excel.
                    cell.Style.NumberFormat.Format = TextNumberFormat;
                    cell.Value = row.NationalId ?? string.Empty;
                    break;

                default:
                    cell.Value = row.FullName;
                    break;
            }
        }

        for (int columnIndex = 0; columnIndex < dataset.Columns.Count; columnIndex++)
        {
            ExportCell source = columnIndex < row.Cells.Count ? row.Cells[columnIndex] : default;
            IXLCell cell = sheet.Cell(sheetRow, column++);

            if (source.HasValue)
            {
                if (string.IsNullOrEmpty(source.Caption))
                {
                    cell.Value = source.Value;
                }
                else
                {
                    // Free text from a form, which the CSV also writes in place of the number - in
                    // full, unlike the grid's six-character truncation.
                    cell.Value = source.Caption;
                }

                ApplyAppearance(cell, source);
            }

            if (!options.IncludeTimestamps)
            {
                continue;
            }

            IXLCell timestampCell = sheet.Cell(sheetRow, column++);

            if (source.HasValue)
            {
                timestampCell.Value = source.Timestamp.Date;
                timestampCell.Style.DateFormat.Format = DateNumberFormat;
            }
        }
    }

    /// <summary>
    /// Paints one data cell the way the grid paints it.
    /// </summary>
    /// <remarks>
    /// A white background is left unset: <see cref="RiskPalette.NoRisk"/> is what every unruled cell
    /// gets, which is nearly all of them, and an explicit white fill is indistinguishable from
    /// Excel's default while costing a style reference per cell.
    /// </remarks>
    private static void ApplyAppearance(IXLCell cell, ExportCell source)
    {
        if (source.Background is { } background && background != RiskPalette.NoRisk)
        {
            cell.Style.Fill.BackgroundColor = ToXLColor(background);
        }

        if (source.Foreground is { } foreground)
        {
            cell.Style.Font.FontColor = ToXLColor(foreground);
        }

        cell.Style.Alignment.Horizontal = source.AlignLeft
            ? XLAlignmentHorizontalValues.Left
            : XLAlignmentHorizontalValues.Right;
    }

    /// <summary>Converts a domain colour, which is what keeps WPF out of <c>QuickStat.Core</c>.</summary>
    /// <param name="colour">The colour.</param>
    /// <returns>The ClosedXML colour.</returns>
    public static XLColor ToXLColor(Rgb colour) => XLColor.FromArgb(colour.R, colour.G, colour.B);

    /// <summary>
    /// Renders one value the way the CSV would, for callers that want the two to agree.
    /// </summary>
    /// <param name="cell">The cell.</param>
    /// <param name="culture">Culture supplying the decimal separator.</param>
    /// <returns>The caption when set, otherwise the <c>%g</c> value, otherwise empty.</returns>
    public static string AsText(ExportCell cell, CultureInfo culture)
    {
        if (!cell.HasValue)
        {
            return string.Empty;
        }

        return string.IsNullOrEmpty(cell.Caption)
            ? DelphiNumberFormat.Format(cell.Value, culture)
            : cell.Caption;
    }
}
