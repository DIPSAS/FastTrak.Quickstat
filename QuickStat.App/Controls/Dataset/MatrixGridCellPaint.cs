using System.Windows.Media;

namespace QuickStat.Controls.Dataset;

/// <summary>How one cell is to be drawn, after every rule has been applied.</summary>
/// <remarks>
/// The Delphi never materialised this: <c>HandleCellDraw</c> assigned straight to
/// <c>Canvas.Brush.Color</c> and <c>Canvas.Font</c> as it went
/// (<c>EPR.QA.GUI.Grid.Study.pas:222-244</c>), so the priority order could only be verified by
/// looking at the screen. Separating the decision from the drawing is what makes it assertable.
/// </remarks>
public readonly record struct MatrixGridCellPaint
{
    /// <summary>The fill.</summary>
    public required Color Background { get; init; }

    /// <summary>The text colour.</summary>
    public required Color Foreground { get; init; }

    /// <summary>Whether the text is emphasised - the header row and the current row are.</summary>
    public required bool Bold { get; init; }

    /// <summary>Whether the text is left-aligned with an ellipsis rather than right-aligned.</summary>
    public required bool AlignLeft { get; init; }
}
