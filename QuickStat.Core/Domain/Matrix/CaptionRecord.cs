namespace QuickStat.Domain.Matrix;

/// <summary>One caption: a variable name, its heading and its description.</summary>
/// <remarks>
/// Delphi: <c>TCaptionRecord</c> (<c>EPR.QA.CaptionRecord.pas</c>). Its <c>LoadAndNext(TDataset)</c>
/// does not come across - reading a result set is the data layer's job, and the caption source hands
/// these over already built.
/// </remarks>
public sealed record CaptionRecord
{
    /// <summary>The variable the caption applies to. Matched ordinally.</summary>
    public required string VarName { get; init; }

    /// <summary>The column heading.</summary>
    public required string Title { get; init; }

    /// <summary>The tooltip text. Usually empty.</summary>
    public string Description { get; init; } = "";
}
