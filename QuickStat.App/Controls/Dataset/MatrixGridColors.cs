using System.Windows.Media;

namespace QuickStat.Controls.Dataset;

/// <summary>The eight colours the cell-painting rules choose between.</summary>
/// <remarks>
/// Passed to <see cref="MatrixGridCellPainter"/> as a value so the whole priority table is a pure
/// function of (cell, caret position, palette) and can be asserted without constructing a control.
/// <see cref="MatrixGrid"/> builds one of these from its own dependency properties each frame.
/// </remarks>
public readonly record struct MatrixGridColors
{
    /// <summary>
    /// A cell with a datapoint whose rule offers no colour. Delphi step 4: <c>clWhite</c>.
    /// </summary>
    public required Color Default { get; init; }

    /// <summary>
    /// A grid position with no object behind it at all. Delphi <c>clWebSnow</c> <c>#FFFAFA</c>.
    /// </summary>
    /// <remarks>
    /// Unreachable in the port; see <see cref="MatrixGrid.MissingObjectBackground"/> for why it is
    /// still wired up rather than deleted.
    /// </remarks>
    public required Color MissingObject { get; init; }

    /// <summary>The header row and the frozen identity columns. Delphi <c>FixedColor</c> <c>#F4FBFB</c>.</summary>
    public required Color Fixed { get; init; }

    /// <summary>The current cell, which overrides every other background. <c>#FFFBD4</c>.</summary>
    public required Color CurrentCell { get; init; }

    /// <summary>The rest of the current row when the cell has no colour of its own. <c>#F3F9FE</c>.</summary>
    public required Color CurrentRow { get; init; }

    /// <summary>
    /// The 50 % blend partner for a <em>coloured</em> cell in the current row. <c>#E7F2FC</c>.
    /// </summary>
    /// <remarks>
    /// <see cref="MatrixGridPalette.Blend"/> of <see cref="Default"/> with this at 50 % equals
    /// <see cref="CurrentRow"/>, so the two agree on the common case by construction.
    /// </remarks>
    public required Color CurrentRowTint { get; init; }

    /// <summary>Text colour everywhere except the <c>PID</c> column.</summary>
    public required Color Text { get; init; }

    /// <summary>
    /// Text colour of the whole <c>PID</c> column, header included. Delphi <c>FixedFontColor</c>,
    /// assigned <c>clMenuBackgroundDarkBrush</c> <c>#035F66</c> at <c>MainQuickStat.pas:376</c>.
    /// </summary>
    public required Color FixedText { get; init; }
}
