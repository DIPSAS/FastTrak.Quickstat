using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace QuickStat.Controls.Dataset;

/// <summary>
/// Makes the owner-drawn grid reachable by a screen reader.
/// </summary>
/// <remarks>
/// <para>
/// This is the accessibility cost of choosing a custom control over <c>DataGrid</c>, and
/// PORT-PLAN.md §5 Phase 3 budgets it by name: "it must not be skipped". Nothing below the grid is a
/// visual, so without a peer the whole dataset is a single blank rectangle to assistive technology.
/// </para>
/// <para>
/// <b>Peers are virtualised too.</b> <see cref="GetChildrenCore"/> returns peers only for the cells
/// currently on screen - a 1500 × 1000 matrix would otherwise mean one and a half million peer
/// objects. Random access to any cell, on or off screen, goes through
/// <see cref="IGridProvider.GetItem"/>, which is exactly the mechanism UIA grid navigation uses and
/// what <c>DataGrid</c> does under row virtualisation. Peers are cached per position so identity is
/// stable between calls, and the cache is dropped when the structure changes.
/// </para>
/// </remarks>
public class MatrixGridAutomationPeer : FrameworkElementAutomationPeer, IGridProvider, ITableProvider
{
    private readonly Dictionary<(int Row, int Column), MatrixGridCellAutomationPeer> _cells = [];

    /// <summary>Creates the peer.</summary>
    /// <param name="owner">The grid.</param>
    /// <exception cref="ArgumentNullException"><paramref name="owner"/> is <see langword="null"/>.</exception>
    public MatrixGridAutomationPeer(MatrixGrid owner)
        : base(owner)
    {
    }

    /// <inheritdoc />
    public int RowCount => Grid.RowCount;

    /// <inheritdoc />
    public int ColumnCount => Grid.DisplayColumnCount;

    /// <inheritdoc />
    /// <remarks>
    /// Row-major, and there are no row headers: the <c>PID</c> column is an ordinary frozen column
    /// that happens to identify the person, not a header. Exposing it as one would make a screen
    /// reader announce the person id twice for every cell in the row.
    /// </remarks>
    public RowOrColumnMajor RowOrColumnMajor => RowOrColumnMajor.RowMajor;

    private MatrixGrid Grid => (MatrixGrid)Owner;

    /// <inheritdoc />
    public IRawElementProviderSimple? GetItem(int row, int column)
    {
        MatrixGridCellAutomationPeer? peer = CellPeer(row, column);

        return peer is null ? null : ProviderFromPeer(peer);
    }

    /// <inheritdoc />
    /// <remarks>None: see <see cref="RowOrColumnMajor"/>.</remarks>
    public IRawElementProviderSimple[] GetRowHeaders() => [];

    /// <inheritdoc />
    /// <remarks>
    /// Returns an empty array until the grid is in a window: <c>ProviderFromPeer</c> needs a window
    /// handle to wrap a peer in. <see cref="ColumnHeaderPeers"/> is the part that chooses <i>which</i>
    /// cells are headers, and it works either way.
    /// </remarks>
    public IRawElementProviderSimple[] GetColumnHeaders()
    {
        IReadOnlyList<MatrixGridCellAutomationPeer> peers = ColumnHeaderPeers();
        List<IRawElementProviderSimple> headers = new(peers.Count);

        foreach (MatrixGridCellAutomationPeer peer in peers)
        {
            if (ProviderFromPeer(peer) is { } provider)
            {
                headers.Add(provider);
            }
        }

        return [.. headers];
    }

    /// <summary>The header cell of every column, left to right.</summary>
    /// <returns>One peer per display column.</returns>
    protected internal IReadOnlyList<MatrixGridCellAutomationPeer> ColumnHeaderPeers()
    {
        int columns = Grid.DisplayColumnCount;
        List<MatrixGridCellAutomationPeer> headers = new(columns);

        for (int column = 0; column < columns; column++)
        {
            if (CellPeer(MatrixGrid.NoIndex, column) is { } peer)
            {
                headers.Add(peer);
            }
        }

        return headers;
    }

    /// <summary>Drops the cached cell peers and tells UIA the structure changed.</summary>
    /// <remarks>
    /// Called when the matrix, the identification mode or the column layout changes. Without it a
    /// screen reader keeps announcing the previous dataset's values from stale peers.
    /// </remarks>
    public void InvalidateStructure()
    {
        _cells.Clear();

        ResetChildrenCache();
    }

    /// <inheritdoc />
    public override object? GetPattern(PatternInterface patternInterface) => patternInterface switch
    {
        PatternInterface.Grid or PatternInterface.Table => this,
        _ => base.GetPattern(patternInterface),
    };

    /// <summary>Finds or creates the peer for one cell.</summary>
    /// <param name="rowIndex">Data row, or <see cref="MatrixGrid.NoIndex"/> for the header row.</param>
    /// <param name="displayColumnIndex">Column position on screen, frozen columns first.</param>
    /// <returns>The peer, or <see langword="null"/> when the position addresses no cell.</returns>
    protected internal MatrixGridCellAutomationPeer? CellPeer(int rowIndex, int displayColumnIndex)
    {
        if (Grid.GetDisplayCellKind(rowIndex, displayColumnIndex) == MatrixGridCellKind.None)
        {
            return null;
        }

        (int, int) key = (rowIndex, displayColumnIndex);

        if (_cells.TryGetValue(key, out MatrixGridCellAutomationPeer? cached))
        {
            return cached;
        }

        MatrixGridCellAutomationPeer peer = new(this, rowIndex, displayColumnIndex);

        _cells.Add(key, peer);

        return peer;
    }

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataGrid;

    /// <inheritdoc />
    protected override string GetClassNameCore() => nameof(MatrixGrid);

    /// <inheritdoc />
    /// <remarks>
    /// Falls back to a name of its own rather than returning empty, because an unnamed grid is
    /// announced as "data grid" and nothing else. Step 3.1 can override it with
    /// <see cref="AutomationProperties.NameProperty"/> on the element.
    /// </remarks>
    protected override string GetNameCore()
    {
        string name = base.GetNameCore();

        return string.IsNullOrEmpty(name) ? "Dataset" : name;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Only the cells on screen. The rest of the grid is reachable through
    /// <see cref="GetItem"/> and <see cref="GetColumnHeaders"/>, which is how UIA expects a
    /// virtualised grid to behave.
    /// </remarks>
    protected override List<AutomationPeer> GetChildrenCore()
    {
        MatrixGrid grid = Grid;
        List<AutomationPeer> children = [];

        for (int column = 0; column < grid.DisplayColumnCount; column++)
        {
            AddIfVisible(children, MatrixGrid.NoIndex, column);
        }

        for (int row = 0; row < grid.RowCount; row++)
        {
            bool anyVisible = false;

            for (int column = 0; column < grid.DisplayColumnCount; column++)
            {
                anyVisible |= AddIfVisible(children, row, column);
            }

            // Rows are laid out top to bottom, so the first row past the bottom of the viewport ends
            // the scan - without this a thousand-row matrix walks every row on every UIA query.
            if (!anyVisible && children.Count > grid.DisplayColumnCount)
            {
                break;
            }
        }

        return children;
    }

    private bool AddIfVisible(List<AutomationPeer> children, int rowIndex, int displayColumnIndex)
    {
        if (!Grid.TryGetDisplayCellBounds(rowIndex, displayColumnIndex, out _))
        {
            return false;
        }

        MatrixGridCellAutomationPeer? peer = CellPeer(rowIndex, displayColumnIndex);

        if (peer is null)
        {
            return false;
        }

        children.Add(peer);

        return true;
    }
}
