namespace QuickStat.Export;

/// <summary>One data cell as an export sees it.</summary>
/// <remarks>
/// Flattened out of <c>QuickStat.Domain.DataPoints.DataPoint</c> so that a writer can be tested
/// against a hand-built dataset. <see cref="HasValue"/> is the distinction the Delphi could only
/// express as "the grid holds a <c>TPersonGridColumn</c> here instead of a <c>TDataPoint</c>".
/// </remarks>
public readonly record struct ExportCell
{
    /// <summary>Whether the person has a datapoint in this column at all.</summary>
    /// <remarks>
    /// When <see langword="false"/> the legacy CSV writes an empty quoted field, and its timestamp
    /// sub-field is written empty and <em>unquoted</em>.
    /// </remarks>
    public bool HasValue { get; init; }

    /// <summary>The numeric value.</summary>
    public double Value { get; init; }

    /// <summary>When the underlying observation was made.</summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Free text from the collector - a form answer or an ATC name - or <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// <b>When this is non-empty it is what the export writes, in place of the number.</b> That is
    /// the whole point of upstream commit <c>8486b3d09</c> (2022-05-06, "#489525: QuickStat skal
    /// kunne vise og eksportere tekstdata fra skjema"), which added the branch to
    /// <c>TPersonGridData.GetCellText</c>. Note this is <em>not</em> the same mechanism as the
    /// <c>ICellText.CellText</c> subclass overrides (<c>Ja</c>/<c>Nei</c>, <c>Rgm</c>/<c>AF</c>,
    /// BMI's one decimal place), which really are display-only and really are ignored on export.
    /// </remarks>
    public string? Caption { get; init; }
}
