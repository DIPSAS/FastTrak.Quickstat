namespace QuickStat.Controls.Dataset;

/// <summary>The cell a point lands on.</summary>
/// <param name="Kind">Which of the four cell kinds was hit.</param>
/// <param name="RowIndex">
/// Index into <see cref="QuickStat.Domain.Matrix.PersonMatrix.Rows"/>, or
/// <see cref="MatrixGrid.NoIndex"/> for the header row.
/// </param>
/// <param name="ColumnIndex">
/// Index into <see cref="QuickStat.Domain.Matrix.PersonMatrix.Columns"/>, or
/// <see cref="MatrixGrid.NoIndex"/> for a frozen identity column.
/// </param>
/// <param name="FixedOrdinal">
/// One of the <see cref="QuickStat.Domain.Matrix.FixedColumns"/> ordinals, or
/// <see cref="MatrixGrid.NoIndex"/> for a data column.
/// </param>
/// <param name="DisplayColumnIndex">
/// The column's position on screen, counting the visible frozen columns first. Unlike
/// <paramref name="ColumnIndex"/> this shifts when the identification mode changes, so it is a
/// layout coordinate and never a model one.
/// </param>
/// <remarks>
/// <paramref name="RowIndex"/> and <paramref name="ColumnIndex"/> are deliberately the same
/// coordinates <see cref="MatrixGridCellEventArgs"/> carries, so a hit can be raised as an event
/// without translation.
/// </remarks>
public readonly record struct MatrixGridHit(
    MatrixGridCellKind Kind,
    int RowIndex,
    int ColumnIndex,
    int FixedOrdinal,
    int DisplayColumnIndex)
{
    /// <summary>A point that landed on no cell.</summary>
    public static MatrixGridHit Miss { get; } = new(
        MatrixGridCellKind.None,
        MatrixGrid.NoIndex,
        MatrixGrid.NoIndex,
        MatrixGrid.NoIndex,
        MatrixGrid.NoIndex);

    /// <summary>Whether a cell was hit at all.</summary>
    public bool IsHit => Kind != MatrixGridCellKind.None;

    /// <summary>Whether the hit is on the header row.</summary>
    public bool IsHeader => Kind is MatrixGridCellKind.FixedHeader or MatrixGridCellKind.ColumnHeader;
}
