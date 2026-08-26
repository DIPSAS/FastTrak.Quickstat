namespace QuickStat.Controls.Dataset;

/// <summary>Identifies the cell the user activated.</summary>
/// <param name="rowIndex">
/// Index into <see cref="QuickStat.Domain.Matrix.PersonMatrix.Rows"/>, or
/// <see cref="MatrixGrid.NoIndex"/> when the header row was clicked.
/// </param>
/// <param name="columnIndex">
/// Index into <see cref="QuickStat.Domain.Matrix.PersonMatrix.Columns"/>, or
/// <see cref="MatrixGrid.NoIndex"/> when one of the fixed identity columns was clicked.
/// </param>
/// <remarks>
/// Part of the 3.1 ↔ 3.5 contract; step 3.5 owns this file. A class rather than a record struct
/// because it is an <see cref="EventArgs"/> and WPF event handlers take it by reference.
/// </remarks>
public sealed class MatrixGridCellEventArgs(int rowIndex, int columnIndex) : EventArgs
{
    /// <summary>Index into <see cref="QuickStat.Domain.Matrix.PersonMatrix.Rows"/>.</summary>
    public int RowIndex { get; } = rowIndex;

    /// <summary>Index into <see cref="QuickStat.Domain.Matrix.PersonMatrix.Columns"/>.</summary>
    public int ColumnIndex { get; } = columnIndex;

    /// <summary>Whether both indices point at a real data cell rather than a header or fixed column.</summary>
    public bool IsDataCell => RowIndex != MatrixGrid.NoIndex && ColumnIndex != MatrixGrid.NoIndex;
}
