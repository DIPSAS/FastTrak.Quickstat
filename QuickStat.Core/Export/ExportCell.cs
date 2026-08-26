using QuickStat.Domain.DataPoints;
using QuickStat.Domain.Matrix;

namespace QuickStat.Export;

/// <summary>One data cell as an export sees it.</summary>
/// <remarks>
/// <para>
/// Flattened out of <see cref="DataPoint"/> so that a writer can be tested against a hand-built
/// dataset. <see cref="HasValue"/> is the distinction the Delphi could only express as "the grid
/// holds a <c>TPersonGridColumn</c> here instead of a <c>TDataPoint</c>".
/// </para>
/// <para>
/// The appearance members carry <see cref="MatrixCell"/>'s decision for the same cell, so the
/// workbook can be shaded exactly as the screen is. They are populated only when
/// <see cref="ExportDataset.FromMatrix"/> is asked for them, because CSV never reads them and
/// computing them means running the display rule for every valued cell in the matrix.
/// </para>
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
    /// The export writes the caption in <b>full</b>; the six-character truncation in
    /// <see cref="DataPointRule.DefaultCaptionLength"/> is the grid's, applied by
    /// <see cref="PersonMatrix.GetCell"/> and not by <c>TPersonGridData.GetCellText</c>.
    /// </remarks>
    public string? Caption { get; init; }

    /// <summary>
    /// Cell background from <see cref="MatrixCell.Background"/>, or <see langword="null"/>.
    /// </summary>
    public Rgb? Background { get; init; }

    /// <summary>
    /// Cell foreground from <see cref="MatrixCell.Foreground"/>, or <see langword="null"/>.
    /// </summary>
    public Rgb? Foreground { get; init; }

    /// <summary>Whether the grid draws this cell left-aligned. From <see cref="MatrixCell.AlignLeft"/>.</summary>
    public bool AlignLeft { get; init; }
}
