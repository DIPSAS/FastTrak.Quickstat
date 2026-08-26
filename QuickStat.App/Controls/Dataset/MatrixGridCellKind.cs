namespace QuickStat.Controls.Dataset;

/// <summary>What kind of cell a grid position addresses.</summary>
/// <remarks>
/// The Delphi decided this inline from <c>ARow &lt; FixedRows</c> and <c>ACol &lt; FixedCols</c>
/// inside <c>HandleCellDraw</c> (<c>EPR.QA.GUI.Grid.Study.pas:189-220</c>). Naming the four cases
/// is what lets the painting priority table be written as a pure function and unit-tested.
/// </remarks>
public enum MatrixGridCellKind
{
    /// <summary>No cell - the point is outside the grid, or the grid has no data.</summary>
    None = 0,

    /// <summary>The header row over one of the frozen identity columns (<c>PID</c>, <c>Født</c>, …).</summary>
    FixedHeader,

    /// <summary>The header row over a data column; the text is the variable's title.</summary>
    ColumnHeader,

    /// <summary>A frozen identity cell in a data row.</summary>
    Fixed,

    /// <summary>A data cell: one person's value for one variable.</summary>
    Data,
}
