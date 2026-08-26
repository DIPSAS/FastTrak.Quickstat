namespace QuickStat.Export;

/// <summary>Which file the exporter writes.</summary>
public enum ExportFormat
{
    /// <summary>
    /// Delimited text. The only format the Delphi has, and the one downstream scripts consume.
    /// </summary>
    Csv = 0,

    /// <summary>
    /// A real Excel workbook, written with ClosedXML.
    /// </summary>
    /// <remarks>
    /// New in the port (PORT-PLAN.md §7.3). <c>Open this dataset in Excel</c> currently writes a CSV
    /// to <c>%TEMP%</c> and hands it to Excel, so the numbers make a locale round trip through text
    /// on the way. A workbook writes them as numbers.
    /// </remarks>
    Xlsx = 1,
}
