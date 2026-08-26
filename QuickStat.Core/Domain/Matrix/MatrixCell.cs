using QuickStat.Domain.DataPoints;

namespace QuickStat.Domain.Matrix;

/// <summary>
/// Everything the grid needs to paint one cell, computed without touching a UI framework.
/// </summary>
/// <remarks>
/// The Delphi had no such thing: <c>TPersonGridData</c> pushed cell <em>objects</em> into the grid
/// control's sparse array and the grid's <c>HandleCellDraw</c> queried each object's interfaces
/// while painting (<c>EPR.QA.GUI.Grid.pas</c>). That put the view inside the model and made cell
/// appearance untestable. Producing a value type instead means the whole decision table can be
/// asserted in unit tests, and the custom WPF control becomes a pure projection.
/// </remarks>
public readonly record struct MatrixCell
{
    /// <summary>The text to draw.</summary>
    /// <remarks>
    /// Not necessarily the exported text. Display goes through
    /// <see cref="DataPointRule.FormatValue"/> and the six-character caption truncation; the CSV
    /// always writes the raw <c>%g</c> value.
    /// </remarks>
    public required string Text { get; init; }

    /// <summary>Background, or <see langword="null"/> for the default.</summary>
    public Rgb? Background { get; init; }

    /// <summary>Foreground, or <see langword="null"/> for the default.</summary>
    public Rgb? Foreground { get; init; }

    /// <summary>Whether to draw left-aligned rather than right-aligned.</summary>
    public bool AlignLeft { get; init; }

    /// <summary>Whether the cell holds a datapoint at all.</summary>
    /// <remarks>
    /// Distinguishes an empty cell from a cell whose value happens to render as an empty string,
    /// which the Delphi could not tell apart.
    /// </remarks>
    public bool HasValue { get; init; }
}
