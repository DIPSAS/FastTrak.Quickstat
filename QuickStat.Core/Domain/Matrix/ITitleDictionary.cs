namespace QuickStat.Domain.Matrix;

/// <summary>Turns a variable name into the heading and tooltip the grid shows for its column.</summary>
/// <remarks>
/// <para>
/// Delphi: <c>ITitleDictionary</c> (<c>EPR.QA.Matrix.Interfaces.pas:11</c>), minus
/// <c>GetVarSubtitle</c>. That third member returned the empty string unconditionally
/// (<c>EPR.QA.CaptionDictionary.pas:171-174</c>) and fed a subtitle row that never renders, because
/// the grid has exactly one header row - so it is dropped rather than ported
/// (<c>Docs/Port/04-matrix-export.md</c> R-17).
/// </para>
/// <para>
/// Only the column <em>heading</em> goes through here. The CSV header row carries the raw
/// <see cref="MatrixColumn.VarName"/>, so a caption never changes an exported file.
/// </para>
/// </remarks>
public interface ITitleDictionary
{
    /// <summary>The heading for a variable.</summary>
    /// <param name="varName">Column name, prefix included.</param>
    /// <returns>
    /// The caption, or <paramref name="varName"/> itself when there is none. That fallback is why
    /// the grid shows friendly lab names beside raw names such as <c>NDV_INS…</c>.
    /// </returns>
    string GetVarTitle(string varName);

    /// <summary>The longer description for a variable.</summary>
    /// <param name="varName">Column name, prefix included.</param>
    /// <returns>The description, or an empty string. Usually empty.</returns>
    string GetVarDescription(string varName);
}
