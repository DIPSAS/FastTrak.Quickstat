using System.Windows.Media;
using QuickStat.Domain.Matrix;

namespace QuickStat.Controls.Dataset;

/// <summary>
/// The cell-painting priority table, ported from <c>TStudyOverviewGrid.HandleCellDraw</c>.
/// </summary>
/// <remarks>
/// <para>
/// A pure function, deliberately. <c>05-ui-spec.md</c> §C.3 lists seven background rules in strict
/// priority order and three text rules; getting the order wrong is invisible in review and obvious
/// to a user, so all of it lives in one place with no WPF object anywhere near it.
/// </para>
/// <para>
/// Steps 3 to 6 of the Delphi's table are already done by
/// <see cref="PersonMatrix.GetCell"/> - the risk-ladder colour, the empty-cell grey, the
/// six-character caption truncation and the caption's left alignment. What is left here is exactly
/// the part that depends on where the caret is rather than on the data.
/// </para>
/// <para>
/// Two behaviours in the Delphi are deliberately <b>not</b> reproduced. <c>OnGetColor</c>
/// (<c>Grid.Study.pas:177-181</c>) was a lazy-memoisation callback into the percentile machinery,
/// which is dead as shipped (PORT-PLAN.md §7.1) - the port resolves colour eagerly from the rule
/// instead. And <c>gdSelected</c> (<c>:225</c>) shared a branch with the current row, but with
/// <c>goRowSelect</c> removed and no editing the selection is never wider than the current cell, so
/// the term adds nothing observable.
/// </para>
/// </remarks>
public static class MatrixGridCellPainter
{
    /// <summary>
    /// How far a coloured cell in the current row moves toward
    /// <see cref="MatrixGridColors.CurrentRowTint"/>. Delphi
    /// <c>BlendColors(brushColor, CurrentRowColor, 50)</c>.
    /// </summary>
    public const int CurrentRowBlendPercent = 50;

    /// <summary>Decides how one cell is drawn.</summary>
    /// <param name="kind">Which of the four cell kinds this position is.</param>
    /// <param name="cell">
    /// The cell's computed appearance, or <see langword="null"/> for the Delphi's "no object behind
    /// the cell" case, which the port cannot actually produce - see
    /// <see cref="MatrixGrid.MissingObjectBackground"/>.
    /// </param>
    /// <param name="fixedOrdinal">
    /// The <see cref="FixedColumns"/> ordinal for a frozen column, or
    /// <see cref="MatrixGrid.NoIndex"/> for a data column.
    /// </param>
    /// <param name="isCurrentCell">Whether this is the caret cell.</param>
    /// <param name="isCurrentRow">Whether this cell's row is the current row.</param>
    /// <param name="colors">The theme colours.</param>
    /// <returns>The resolved appearance.</returns>
    public static MatrixGridCellPaint Resolve(
        MatrixGridCellKind kind,
        MatrixCell? cell,
        int fixedOrdinal,
        bool isCurrentCell,
        bool isCurrentRow,
        in MatrixGridColors colors)
    {
        bool isHeader = kind is MatrixGridCellKind.FixedHeader or MatrixGridCellKind.ColumnHeader;
        bool isFrozen = kind is MatrixGridCellKind.FixedHeader or MatrixGridCellKind.Fixed;

        // The header row is never the current row: the Delphi's OnSelectCell is not raised for fixed
        // cells, so fCurrentRow can only ever hold a data row (EPR.QA.GUI.Grid.pas:157-170).
        bool inCurrentRow = isCurrentRow && !isHeader;

        return new MatrixGridCellPaint
        {
            Background = ResolveBackground(cell, isFrozen || isHeader, isCurrentCell, inCurrentRow, colors),
            Foreground = ResolveForeground(cell, fixedOrdinal, isCurrentCell, colors),
            Bold = isHeader || inCurrentRow,
            AlignLeft = ResolveAlignment(kind, cell, fixedOrdinal),
        };
    }

    private static Color ResolveBackground(
        MatrixCell? cell,
        bool isFixedCell,
        bool isCurrentCell,
        bool isCurrentRow,
        in MatrixGridColors colors)
    {
        // Rules 1-4: no object at all, then the object's own colour, then white.  The empty-cell
        // #F5F5F5 arrives here as the cell's Background, already chosen by PersonMatrix.GetCell.
        Color background = cell is { } value
            ? value.Background is { } rgb ? MatrixGridPalette.ToColor(rgb) : colors.Default
            : colors.MissingObject;

        // Rule 5: the current cell overrides everything above it.
        if (isCurrentCell)
        {
            return colors.CurrentCell;
        }

        // Rule 6: the rest of the current row is tinted, and the tint is a *blend*, not a fill - a
        // risk-coloured cell must keep its colour when the user selects its row.  For the ordinary
        // white cell the two are the same number, which is why the flat brush is used there: it
        // honours a theme that overrides CurrentRow alone, and by construction
        // Blend(Default, CurrentRowTint, 50) == CurrentRow for the shipped values.
        if (isCurrentRow)
        {
            return SameRgb(background, colors.Default)
                ? colors.CurrentRow
                : MatrixGridPalette.Blend(background, colors.CurrentRowTint, CurrentRowBlendPercent);
        }

        // Rule 7: fixed cells, last, so the current row wins over the frozen block - which is why
        // the PID cell of the current row is tinted rather than FixedColor.
        return isFixedCell ? colors.Fixed : background;
    }

    private static Color ResolveForeground(
        MatrixCell? cell,
        int fixedOrdinal,
        bool isCurrentCell,
        in MatrixGridColors colors)
    {
        // Delphi tests ACol = 0, which is COL_PERSON_ID.  It comes first, so the PID column is teal
        // in the header row and in every data row alike.
        if (fixedOrdinal == FixedColumns.PersonId)
        {
            return colors.FixedText;
        }

        // The current cell drops any per-datapoint font colour and uses the default, so the caret
        // stays legible on the pale-yellow fill.
        if (isCurrentCell)
        {
            return colors.Text;
        }

        return cell?.Foreground is { } rgb ? MatrixGridPalette.ToColor(rgb) : colors.Text;
    }

    private static bool ResolveAlignment(MatrixGridCellKind kind, MatrixCell? cell, int fixedOrdinal) => kind switch
    {
        // A data column's header is left-aligned with an ellipsis whatever the column holds
        // (Grid.Study.pas:200-201), because variable names are long and their tails are the
        // distinguishing part.
        MatrixGridCellKind.ColumnHeader => true,

        // A frozen column's header is *not*: it keeps the column's own alignment, so "PID" sits
        // right-aligned over its numbers while "Født" and "Navn" sit left over their text.  The
        // spec's blanket "the header row is left-aligned" is true only of data columns.
        MatrixGridCellKind.FixedHeader or MatrixGridCellKind.Fixed => FixedColumns.IsTextColumn(fixedOrdinal),

        // Right-aligned unless the datapoint carries a caption, which PersonMatrix.GetCell has
        // already decided.
        _ => cell?.AlignLeft ?? false,
    };

    private static bool SameRgb(Color left, Color right) =>
        left.R == right.R && left.G == right.G && left.B == right.B;
}
