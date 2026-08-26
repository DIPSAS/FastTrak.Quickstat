namespace QuickStat.Export;

/// <summary>One data column as an export sees it.</summary>
/// <remarks>
/// Only <see cref="VarName"/> reaches a CSV: the header row carries the variable name, never the
/// display title (<c>EPR.QA.Matrix.pas:258-259</c>). <see cref="Title"/> is here because the xlsx
/// writer can afford a second header row and downstream readers of a workbook are humans.
/// </remarks>
public sealed record ExportColumn
{
    /// <summary>Column identity, prefix included. This is the CSV header cell.</summary>
    public required string VarName { get; init; }

    /// <summary>Human-readable heading, falling back to <see cref="VarName"/>.</summary>
    public string Title { get; init; } = "";
}
