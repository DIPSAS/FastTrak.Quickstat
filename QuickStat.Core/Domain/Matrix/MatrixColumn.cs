namespace QuickStat.Domain.Matrix;

/// <summary>One column of the result matrix.</summary>
/// <remarks>
/// Delphi: <c>TPersonGridColumn</c> (<c>EPR.QA.Matrix.Column.pas:16</c>). Its <c>Subtitle</c> field
/// is dropped: the constructor never took a value for it, nothing ever assigned one, and the grid
/// rendered it as the text of a cell with no datapoint - which is how "no value" ends up as an
/// empty string.
/// </remarks>
public sealed record MatrixColumn
{
    /// <summary>
    /// Column identity: <see cref="QuickStat.Collectors.CollectorDescriptor.VarPrefix"/> plus the
    /// collector's <c>VarName</c>. Compared ordinally.
    /// </summary>
    /// <remarks>
    /// This is also what the CSV header row contains - the <em>name</em>, not
    /// <see cref="Title"/> (<c>EPR.QA.Matrix.pas:258-259</c>). Downstream scripts read it.
    /// </remarks>
    public required string VarName { get; init; }

    /// <summary>
    /// Human-readable heading for the grid.
    /// </summary>
    /// <remarks>
    /// Resolved from the caption dictionary at column-creation time, falling back to
    /// <see cref="VarName"/> when there is no caption
    /// (<c>EPR.QA.CaptionDictionary.pas:176-184</c>). That fallback is why the grid shows friendly
    /// lab names next to raw names such as <c>NDV_INS…</c>: the only captions QuickStat actually
    /// loads are lab-class friendly names, plus twelve hardcoded ones.
    /// </remarks>
    public required string Title { get; init; }

    /// <summary>Longer description for the tooltip; usually empty.</summary>
    public string Description { get; init; } = "";
}
