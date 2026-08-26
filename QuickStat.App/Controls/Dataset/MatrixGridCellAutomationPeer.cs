using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace QuickStat.Controls.Dataset;

/// <summary>One cell of the dataset grid, as assistive technology sees it.</summary>
/// <remarks>
/// <para>
/// Derives straight from <see cref="AutomationPeer"/> because there is no element to wrap - the grid
/// draws itself and has no children. That is the same shape <c>DataGridCellItemAutomationPeer</c>
/// takes, and it is why every one of the base class's abstract members has to be answered here.
/// </para>
/// <para>
/// The three patterns matter for different readers:
/// <see cref="IGridItemProvider"/> gives the cell's coordinates, <see cref="ITableItemProvider"/>
/// links it to its column header so "8" is announced as "PID 8", and
/// <see cref="IValueProvider"/> exposes the text for readers that ask for a value rather than a
/// name.
/// </para>
/// </remarks>
public class MatrixGridCellAutomationPeer : AutomationPeer, IGridItemProvider, ITableItemProvider, IValueProvider
{
    private readonly MatrixGridAutomationPeer _grid;

    /// <summary>Creates the peer.</summary>
    /// <param name="grid">The owning grid's peer.</param>
    /// <param name="rowIndex">Data row, or <see cref="MatrixGrid.NoIndex"/> for the header row.</param>
    /// <param name="displayColumnIndex">Column position on screen, frozen columns first.</param>
    /// <exception cref="ArgumentNullException"><paramref name="grid"/> is <see langword="null"/>.</exception>
    public MatrixGridCellAutomationPeer(MatrixGridAutomationPeer grid, int rowIndex, int displayColumnIndex)
    {
        ArgumentNullException.ThrowIfNull(grid);

        _grid = grid;
        RowIndex = rowIndex;
        DisplayColumnIndex = displayColumnIndex;
    }

    /// <summary>Data row, or <see cref="MatrixGrid.NoIndex"/> for the header row.</summary>
    public int RowIndex { get; }

    /// <summary>Column position on screen, frozen columns first.</summary>
    public int DisplayColumnIndex { get; }

    /// <summary>Which of the four cell kinds this is.</summary>
    public MatrixGridCellKind Kind => Owner.GetDisplayCellKind(RowIndex, DisplayColumnIndex);

    /// <inheritdoc />
    /// <remarks>The header row is row zero to UIA, so data rows start at one.</remarks>
    public int Row => RowIndex == MatrixGrid.NoIndex ? 0 : RowIndex;

    /// <inheritdoc />
    public int Column => DisplayColumnIndex;

    /// <inheritdoc />
    /// <remarks>Always one: the grid has no merged cells.</remarks>
    public int RowSpan => 1;

    /// <inheritdoc />
    /// <remarks>Always one: the grid has no merged cells.</remarks>
    public int ColumnSpan => 1;

    /// <inheritdoc />
    public IRawElementProviderSimple? ContainingGrid => ProviderFromPeer(_grid);

    /// <inheritdoc />
    /// <remarks>Read-only: the grid is a report, and the Delphi had <c>goEditing</c> off.</remarks>
    public bool IsReadOnly => true;

    /// <inheritdoc />
    public string Value => Owner.GetDisplayCellText(RowIndex, DisplayColumnIndex);

    private MatrixGrid Owner => (MatrixGrid)_grid.Owner;

    /// <inheritdoc />
    /// <remarks>None: the <c>PID</c> column is a frozen data column, not a row header.</remarks>
    public IRawElementProviderSimple[] GetRowHeaderItems() => [];

    /// <inheritdoc />
    /// <remarks>
    /// The column's own header cell, which is what turns a bare number into "AGE 97" for a screen
    /// reader. A header cell has no header of its own.
    /// </remarks>
    public IRawElementProviderSimple[] GetColumnHeaderItems()
    {
        if (RowIndex == MatrixGrid.NoIndex)
        {
            return [];
        }

        MatrixGridCellAutomationPeer? header = _grid.CellPeer(MatrixGrid.NoIndex, DisplayColumnIndex);

        return header is not null && ProviderFromPeer(header) is { } provider ? [provider] : [];
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Always: the grid is read-only.</exception>
    public void SetValue(string value) =>
        throw new InvalidOperationException("The dataset grid is read-only.");

    /// <inheritdoc />
    public override object? GetPattern(PatternInterface patternInterface) => patternInterface switch
    {
        PatternInterface.GridItem or PatternInterface.TableItem or PatternInterface.Value => this,
        _ => null,
    };

    /// <inheritdoc />
    protected override string GetAcceleratorKeyCore() => "";

    /// <inheritdoc />
    protected override string GetAccessKeyCore() => "";

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore() =>
        RowIndex == MatrixGrid.NoIndex ? AutomationControlType.HeaderItem : AutomationControlType.DataItem;

    /// <inheritdoc />
    protected override string GetAutomationIdCore() =>
        string.Create(CultureInfo.InvariantCulture, $"R{RowIndex}C{DisplayColumnIndex}");

    /// <inheritdoc />
    protected override Rect GetBoundingRectangleCore()
    {
        MatrixGrid owner = Owner;

        if (!owner.TryGetDisplayCellBounds(RowIndex, DisplayColumnIndex, out Rect bounds))
        {
            return Rect.Empty;
        }

        // No presentation source means the control is not in a window - under test, for instance -
        // and PointToScreen would throw rather than return something useless.
        if (PresentationSource.FromVisual(owner) is null)
        {
            return Rect.Empty;
        }

        Point topLeft = owner.PointToScreen(bounds.TopLeft);
        Point bottomRight = owner.PointToScreen(bounds.BottomRight);

        return new Rect(topLeft, bottomRight);
    }

    /// <inheritdoc />
    protected override List<AutomationPeer> GetChildrenCore() => [];

    /// <inheritdoc />
    protected override string GetClassNameCore() => "MatrixGridCell";

    /// <inheritdoc />
    protected override Point GetClickablePointCore()
    {
        Rect bounds = GetBoundingRectangleCore();

        return bounds.IsEmpty
            ? new Point(double.NaN, double.NaN)
            : new Point(bounds.Left + (bounds.Width / 2), bounds.Top + (bounds.Height / 2));
    }

    /// <inheritdoc />
    /// <remarks>
    /// The cell's tooltip: a variable's description on a header, and the datapoint's full hint -
    /// value, timestamp, row id, update count - on a data cell. That is the one place a screen
    /// reader can get at information the visible six characters do not carry.
    /// </remarks>
    protected override string GetHelpTextCore() =>
        Owner.GetDisplayCellToolTip(RowIndex, DisplayColumnIndex) ?? "";

    /// <inheritdoc />
    protected override string GetItemStatusCore() => "";

    /// <inheritdoc />
    protected override string GetItemTypeCore() => "";

    /// <inheritdoc />
    protected override AutomationPeer? GetLabeledByCore() => null;

    /// <inheritdoc />
    /// <remarks>
    /// The cell's own text. An empty cell keeps an empty name rather than gaining a placeholder: the
    /// column header comes from <see cref="GetColumnHeaderItems"/>, which is the UIA-correct place
    /// for it, and inventing "empty" here would make every blank cell read as a word.
    /// </remarks>
    protected override string GetNameCore() => Owner.GetDisplayCellText(RowIndex, DisplayColumnIndex);

    /// <inheritdoc />
    protected override AutomationOrientation GetOrientationCore() => AutomationOrientation.None;

    /// <inheritdoc />
    protected override bool HasKeyboardFocusCore()
    {
        MatrixGrid owner = Owner;

        return owner.IsKeyboardFocused
            && RowIndex != MatrixGrid.NoIndex
            && RowIndex == owner.CurrentRowIndex
            && DisplayColumnIndex == owner.DisplayIndexOfDataColumn(owner.CurrentColumnIndex);
    }

    /// <inheritdoc />
    protected override bool IsContentElementCore() => true;

    /// <inheritdoc />
    protected override bool IsControlElementCore() => true;

    /// <inheritdoc />
    protected override bool IsEnabledCore() => Owner.IsEnabled;

    /// <inheritdoc />
    protected override bool IsKeyboardFocusableCore() =>
        Kind == MatrixGridCellKind.Data && Owner.Focusable;

    /// <inheritdoc />
    protected override bool IsOffscreenCore() =>
        !Owner.TryGetDisplayCellBounds(RowIndex, DisplayColumnIndex, out _);

    /// <inheritdoc />
    protected override bool IsPasswordCore() => false;

    /// <inheritdoc />
    protected override bool IsRequiredForFormCore() => false;

    /// <inheritdoc />
    /// <remarks>
    /// Moves the caret onto the cell and scrolls it into view, which is what focusing a cell means
    /// in a control with no per-cell visual.
    /// </remarks>
    protected override void SetFocusCore()
    {
        MatrixGrid owner = Owner;

        _ = owner.Focus();

        if (Kind == MatrixGridCellKind.Data)
        {
            owner.SetCurrentCell(RowIndex, DisplayColumnIndex - owner.FrozenColumnCount);
        }
        else if (RowIndex != MatrixGrid.NoIndex)
        {
            owner.SetCurrentCell(RowIndex, owner.CurrentColumnIndex);
        }
    }
}
