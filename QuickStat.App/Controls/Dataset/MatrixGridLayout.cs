using System.Windows;
using QuickStat.Domain.Matrix;

namespace QuickStat.Controls.Dataset;

/// <summary>
/// Where every row and column of the dataset grid sits, and which of them a viewport covers.
/// </summary>
/// <remarks>
/// <para>
/// Split out of <see cref="MatrixGrid"/> on purpose. All of this is arithmetic over
/// <see cref="double"/>s and index ranges: it creates no WPF object, needs no dispatcher and no
/// apartment, and is therefore unit-testable directly on the (MTA) test thread. What is left in the
/// control is drawing, which is tested by rendering pixels.
/// </para>
/// <para>
/// <b>Column model.</b> Columns are addressed three ways and the distinction matters:
/// </para>
/// <list type="bullet">
/// <item><description>
/// a <i>fixed ordinal</i> is one of <see cref="FixedColumns.PersonId"/> …
/// <see cref="FixedColumns.Name"/>, i.e. a model concept that survives hiding;
/// </description></item>
/// <item><description>
/// a <i>data column index</i> indexes <see cref="PersonMatrix.Columns"/>;
/// </description></item>
/// <item><description>
/// a <i>display index</i> is the on-screen position, visible frozen columns first, then data
/// columns. Only this one shifts when the identification mode hides three identity columns.
/// </description></item>
/// </list>
/// <para>
/// Delphi: <c>TPersonGrid</c> (<c>EPR.QA.GUI.Grid.pas</c>) had no such separation - it hid a column
/// by setting <c>ColWidths[n] := -1</c> and then read the width <em>back</em> to discover whether it
/// was anonymous (<c>:207-210</c>). That is the trick PORT-PLAN.md §7.2 removes.
/// </para>
/// </remarks>
public sealed class MatrixGridLayout
{
    /// <summary>Height of one data row. Delphi <c>DefaultRowHeight := 17</c>.</summary>
    public const double DefaultRowHeight = 17;

    /// <summary>Height of the single header row. Delphi <c>RowHeights[0] := 18</c>.</summary>
    public const double DefaultHeaderHeight = 18;

    /// <summary>Narrowest a column may be dragged. Below this the header text is unreadable.</summary>
    public const double MinimumColumnWidth = 16;

    /// <summary>How close to a column edge the pointer counts as being on the resize grip.</summary>
    public const double ResizeGripWidth = 4;

    /// <summary>
    /// Slack for comparing a scroll offset against a column boundary, so a click on a scroll arrow
    /// at a boundary steps a whole column instead of moving by a rounding error.
    /// </summary>
    private const double BoundaryEpsilon = 0.5;

    /// <summary>
    /// Default widths of the four identity columns, by
    /// <see cref="FixedColumns"/> ordinal: 44 / 64 / 84 / 128.
    /// </summary>
    /// <remarks>
    /// Delphi <c>TPersonGrid.Create</c> (<c>EPR.QA.GUI.Grid.pas:118-121</c>). They are constants
    /// rather than a computation: <c>UpdateStyle</c> re-derived them from text metrics
    /// (<c>Grid.Study.pas:290-296</c>), but <c>UpdateStyle</c> is only reachable through
    /// <c>IGuiStyleObserver</c>, which QuickStat never registers, so the shipped build uses these
    /// literals.
    /// </remarks>
    public static readonly IReadOnlyList<double> DefaultFixedColumnWidths = [44, 64, 84, 128];

    private static readonly int[] NoOrdinals = [];

    private readonly double[] _fixedWidths =
        [.. DefaultFixedColumnWidths];

    private readonly Dictionary<int, double> _dataWidthOverrides = [];

    private double[] _offsets = [0];
    private IReadOnlyList<int> _visibleFixedOrdinals = NoOrdinals;
    private double _dataColumnWidth = MatrixGrid.NarrowDataColumnWidth;
    private double _rowHeight = DefaultRowHeight;
    private double _headerHeight = DefaultHeaderHeight;
    private int _rowCount;
    private int _dataColumnCount;
    private bool _offsetsAreStale = true;

    /// <summary>Number of data rows.</summary>
    public int RowCount
    {
        get => _rowCount;
        set => _rowCount = Math.Max(0, value);
    }

    /// <summary>Number of data columns, i.e. <c>Matrix.Columns.Count</c>.</summary>
    public int DataColumnCount
    {
        get => _dataColumnCount;

        set
        {
            int clamped = Math.Max(0, value);

            if (_dataColumnCount == clamped)
            {
                return;
            }

            _dataColumnCount = clamped;
            _offsetsAreStale = true;
        }
    }

    /// <summary>
    /// Which identity ordinals are on screen, in order, from
    /// <see cref="FixedColumns.VisibleOrdinals"/>.
    /// </summary>
    /// <remarks>
    /// Assigning this is the <em>only</em> way to change the frozen block, and the caller is
    /// expected to have derived the list through
    /// <see cref="QuickStat.Domain.Anonymisation.IdentificationColumns.For"/>. Deriving it here from
    /// a <see cref="QuickStat.Domain.Anonymisation.PersonIdentification"/> would be the second
    /// interpretation PORT-PLAN.md §7.2 exists to prevent.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The value is <see langword="null"/>.</exception>
    public IReadOnlyList<int> VisibleFixedOrdinals
    {
        get => _visibleFixedOrdinals;

        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _visibleFixedOrdinals = value;
            _offsetsAreStale = true;
        }
    }

    /// <summary>
    /// Width of a data column that has not been resized by hand: 64, or 120 with <i>Wide columns</i>.
    /// </summary>
    /// <remarks>
    /// Assigning this <b>discards every hand-set data-column width</b>, which is faithful:
    /// <c>Set_DataColWidth</c> assigns <c>DefaultColWidth</c>, and in the VCL that resets every
    /// column that has not been individually assigned since - and then re-applies the four fixed
    /// widths explicitly (<c>EPR.QA.GUI.Grid.pas:338-347</c>). Toggling <i>Wide columns</i> is
    /// therefore also how a user undoes a mis-drag.
    /// </remarks>
    public double DataColumnWidth
    {
        get => _dataColumnWidth;

        set
        {
            double clamped = Math.Max(MinimumColumnWidth, value);

            if (_dataColumnWidth.Equals(clamped))
            {
                return;
            }

            _dataColumnWidth = clamped;

            ResetColumnWidths();
        }
    }

    /// <summary>Height of one data row.</summary>
    public double RowHeight
    {
        get => _rowHeight;
        set => _rowHeight = Math.Max(1, value);
    }

    /// <summary>Height of the header row.</summary>
    public double HeaderHeight
    {
        get => _headerHeight;
        set => _headerHeight = Math.Max(0, value);
    }

    /// <summary>How many identity columns are on screen: 4 when fully identified, otherwise 1.</summary>
    public int FixedColumnCount => _visibleFixedOrdinals.Count;

    /// <summary>Total number of columns on screen, frozen block included.</summary>
    public int DisplayColumnCount => FixedColumnCount + _dataColumnCount;

    /// <summary>Combined width of the frozen block; the x where the scrollable band starts.</summary>
    public double FrozenWidth => Offsets[FixedColumnCount];

    /// <summary>Combined width of the data columns, which is the horizontal scroll extent.</summary>
    public double DataWidth => Offsets[DisplayColumnCount] - FrozenWidth;

    /// <summary>Width of the whole grid, frozen block included.</summary>
    public double TotalWidth => Offsets[DisplayColumnCount];

    /// <summary>Height of the whole grid, header row included.</summary>
    public double TotalHeight => _headerHeight + (_rowCount * _rowHeight);

    /// <summary>Height of the data band, which is the vertical scroll extent.</summary>
    public double DataHeight => _rowCount * _rowHeight;

    private double[] Offsets
    {
        get
        {
            if (_offsetsAreStale)
            {
                RebuildOffsets();
            }

            return _offsets;
        }
    }

    /// <summary>Width of one display column.</summary>
    /// <param name="displayIndex">Position on screen, frozen columns first.</param>
    /// <returns>The width in device-independent units.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the grid.</exception>
    public double ColumnWidth(int displayIndex)
    {
        ThrowIfNotAColumn(displayIndex);

        return Offsets[displayIndex + 1] - Offsets[displayIndex];
    }

    /// <summary>Distance from the left edge of the grid to the left edge of a display column.</summary>
    /// <param name="displayIndex">Position on screen, frozen columns first.</param>
    /// <returns>
    /// The unscrolled offset. For a data column, subtract <see cref="FrozenWidth"/> to get the
    /// offset within the scrollable band, which is what the horizontal scroll offset is measured in.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the grid.</exception>
    public double ColumnOffset(int displayIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(displayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(displayIndex, DisplayColumnCount);

        return Offsets[displayIndex];
    }

    /// <summary>Overrides one column's width, as a resize drag does.</summary>
    /// <param name="displayIndex">Position on screen, frozen columns first.</param>
    /// <param name="width">The new width; clamped to <see cref="MinimumColumnWidth"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the grid.</exception>
    /// <remarks>
    /// Delphi allowed this through <c>goColSizing</c>. Note that resizing a <em>hidden</em> identity
    /// column back into view - the loophole PORT-PLAN.md §7.2 records, because the VCL hid a column
    /// by giving it width <c>-1</c> - is structurally impossible here: a hidden ordinal has no
    /// display index at all.
    /// </remarks>
    public void SetColumnWidth(int displayIndex, double width)
    {
        ThrowIfNotAColumn(displayIndex);

        double clamped = Math.Max(MinimumColumnWidth, width);
        int ordinal = FixedOrdinalAt(displayIndex);

        if (ordinal != MatrixGrid.NoIndex)
        {
            _fixedWidths[ordinal] = clamped;
        }
        else
        {
            _dataWidthOverrides[DataColumnAt(displayIndex)] = clamped;
        }

        _offsetsAreStale = true;
    }

    /// <summary>Discards every hand-set width and returns to the defaults.</summary>
    public void ResetColumnWidths()
    {
        for (int ordinal = 0; ordinal < _fixedWidths.Length; ordinal++)
        {
            _fixedWidths[ordinal] = DefaultFixedColumnWidths[ordinal];
        }

        _dataWidthOverrides.Clear();

        _offsetsAreStale = true;
    }

    /// <summary>The identity ordinal a display column shows.</summary>
    /// <param name="displayIndex">Position on screen, frozen columns first.</param>
    /// <returns>The ordinal, or <see cref="MatrixGrid.NoIndex"/> for a data column.</returns>
    public int FixedOrdinalAt(int displayIndex) =>
        displayIndex >= 0 && displayIndex < FixedColumnCount
            ? _visibleFixedOrdinals[displayIndex]
            : MatrixGrid.NoIndex;

    /// <summary>The data column a display column shows.</summary>
    /// <param name="displayIndex">Position on screen, frozen columns first.</param>
    /// <returns>
    /// The index into <see cref="PersonMatrix.Columns"/>, or <see cref="MatrixGrid.NoIndex"/> for a
    /// frozen column.
    /// </returns>
    public int DataColumnAt(int displayIndex) =>
        displayIndex >= FixedColumnCount && displayIndex < DisplayColumnCount
            ? displayIndex - FixedColumnCount
            : MatrixGrid.NoIndex;

    /// <summary>Where a data column sits on screen.</summary>
    /// <param name="dataColumnIndex">Index into <see cref="PersonMatrix.Columns"/>.</param>
    /// <returns>The display index, or <see cref="MatrixGrid.NoIndex"/> when out of range.</returns>
    public int DisplayIndexOfData(int dataColumnIndex) =>
        dataColumnIndex >= 0 && dataColumnIndex < _dataColumnCount
            ? dataColumnIndex + FixedColumnCount
            : MatrixGrid.NoIndex;

    /// <summary>Where an identity column sits on screen.</summary>
    /// <param name="ordinal">A <see cref="FixedColumns"/> ordinal.</param>
    /// <returns>
    /// The display index, or <see cref="MatrixGrid.NoIndex"/> when the identification mode hides it.
    /// </returns>
    public int DisplayIndexOfFixed(int ordinal)
    {
        for (int index = 0; index < _visibleFixedOrdinals.Count; index++)
        {
            if (_visibleFixedOrdinals[index] == ordinal)
            {
                return index;
            }
        }

        return MatrixGrid.NoIndex;
    }

    /// <summary>Which cell kind a display position addresses.</summary>
    /// <param name="rowIndex">Data row, or <see cref="MatrixGrid.NoIndex"/> for the header row.</param>
    /// <param name="displayIndex">Position on screen, frozen columns first.</param>
    /// <returns>The kind, or <see cref="MatrixGridCellKind.None"/> when either index is out of range.</returns>
    public MatrixGridCellKind KindAt(int rowIndex, int displayIndex)
    {
        if (displayIndex < 0 || displayIndex >= DisplayColumnCount)
        {
            return MatrixGridCellKind.None;
        }

        bool isFixedColumn = displayIndex < FixedColumnCount;

        if (rowIndex == MatrixGrid.NoIndex)
        {
            return isFixedColumn ? MatrixGridCellKind.FixedHeader : MatrixGridCellKind.ColumnHeader;
        }

        if (rowIndex < 0 || rowIndex >= _rowCount)
        {
            return MatrixGridCellKind.None;
        }

        return isFixedColumn ? MatrixGridCellKind.Fixed : MatrixGridCellKind.Data;
    }

    /// <summary>The data rows a viewport covers.</summary>
    /// <param name="verticalOffset">Scroll offset into the data band, in device-independent units.</param>
    /// <param name="viewportHeight">Height of the data band on screen, header row excluded.</param>
    /// <returns>The visible run, clamped to the grid.</returns>
    /// <remarks>
    /// This is the whole of vertical virtualisation. A partially visible row at either edge is
    /// included, because it has to be painted.
    /// </remarks>
    public MatrixGridRange VisibleRows(double verticalOffset, double viewportHeight)
    {
        if (_rowCount == 0 || viewportHeight <= 0 || _rowHeight <= 0)
        {
            return MatrixGridRange.Empty;
        }

        double top = Math.Max(0, verticalOffset);
        int first = (int)Math.Floor(top / _rowHeight);

        if (first >= _rowCount)
        {
            return MatrixGridRange.Empty;
        }

        int last = (int)Math.Ceiling((top + viewportHeight) / _rowHeight) - 1;

        last = Math.Min(last, _rowCount - 1);

        return new MatrixGridRange(first, (last - first) + 1);
    }

    /// <summary>The data columns a viewport covers.</summary>
    /// <param name="horizontalOffset">Scroll offset into the data band; the frozen block never scrolls.</param>
    /// <param name="viewportWidth">Width of the scrollable band, frozen block excluded.</param>
    /// <returns>The visible run of <em>data column indices</em>, clamped to the grid.</returns>
    /// <remarks>
    /// This is the whole of horizontal virtualisation, and the reason a <c>DataGrid</c> was rejected:
    /// realistic datasets reach hundreds of columns, and WPF's column virtualisation interacts badly
    /// with <c>FrozenColumnCount</c> (PORT-PLAN.md §5 Phase 3).
    /// </remarks>
    public MatrixGridRange VisibleDataColumns(double horizontalOffset, double viewportWidth)
    {
        if (_dataColumnCount == 0 || viewportWidth <= 0)
        {
            return MatrixGridRange.Empty;
        }

        double[] offsets = Offsets;
        double frozen = offsets[FixedColumnCount];
        double left = frozen + Math.Max(0, horizontalOffset);
        double right = left + viewportWidth;

        int first = FindColumnAt(offsets, left, FixedColumnCount);

        if (first == MatrixGrid.NoIndex)
        {
            return MatrixGridRange.Empty;
        }

        int last = first;

        while (last + 1 < DisplayColumnCount && offsets[last + 1] < right)
        {
            last++;
        }

        return new MatrixGridRange(first - FixedColumnCount, (last - first) + 1);
    }

    /// <summary>Where a cell lands on screen.</summary>
    /// <param name="rowIndex">Data row, or <see cref="MatrixGrid.NoIndex"/> for the header row.</param>
    /// <param name="displayIndex">Position on screen, frozen columns first.</param>
    /// <param name="horizontalOffset">Current horizontal scroll offset.</param>
    /// <param name="verticalOffset">Current vertical scroll offset.</param>
    /// <param name="bounds">The rectangle, in the control's own coordinates.</param>
    /// <returns><see langword="false"/> when the position addresses no cell.</returns>
    /// <remarks>
    /// The rectangle may lie outside the viewport; deciding whether that counts as "visible" needs
    /// the viewport size and belongs to the control, not here.
    /// </remarks>
    public bool TryGetCellBounds(
        int rowIndex,
        int displayIndex,
        double horizontalOffset,
        double verticalOffset,
        out Rect bounds)
    {
        bounds = default;

        MatrixGridCellKind kind = KindAt(rowIndex, displayIndex);

        if (kind == MatrixGridCellKind.None)
        {
            return false;
        }

        double[] offsets = Offsets;
        bool isFrozen = displayIndex < FixedColumnCount;
        double x = isFrozen ? offsets[displayIndex] : offsets[displayIndex] - horizontalOffset;
        double width = offsets[displayIndex + 1] - offsets[displayIndex];

        double y = rowIndex == MatrixGrid.NoIndex
            ? 0
            : (_headerHeight + (rowIndex * _rowHeight)) - verticalOffset;

        double height = rowIndex == MatrixGrid.NoIndex ? _headerHeight : _rowHeight;

        bounds = new Rect(x, y, width, height);

        return true;
    }

    /// <summary>Which cell a point lands on.</summary>
    /// <param name="point">A point in the control's own coordinates.</param>
    /// <param name="horizontalOffset">Current horizontal scroll offset.</param>
    /// <param name="verticalOffset">Current vertical scroll offset.</param>
    /// <returns>The hit, or <see cref="MatrixGridHit.Miss"/>.</returns>
    /// <remarks>
    /// The frozen block wins over the scrolled band wherever they overlap, because the frozen block
    /// is painted last and is therefore what the user sees at that point.
    /// </remarks>
    public MatrixGridHit HitTest(Point point, double horizontalOffset, double verticalOffset)
    {
        if (point.X < 0 || point.Y < 0 || DisplayColumnCount == 0)
        {
            return MatrixGridHit.Miss;
        }

        int displayIndex = DisplayColumnAtX(point.X, horizontalOffset);

        if (displayIndex == MatrixGrid.NoIndex)
        {
            return MatrixGridHit.Miss;
        }

        int rowIndex;

        if (point.Y < _headerHeight)
        {
            rowIndex = MatrixGrid.NoIndex;
        }
        else
        {
            double dataY = (point.Y - _headerHeight) + verticalOffset;

            rowIndex = _rowHeight > 0 ? (int)Math.Floor(dataY / _rowHeight) : MatrixGrid.NoIndex;

            if (rowIndex < 0 || rowIndex >= _rowCount)
            {
                return MatrixGridHit.Miss;
            }
        }

        MatrixGridCellKind kind = KindAt(rowIndex, displayIndex);

        if (kind == MatrixGridCellKind.None)
        {
            return MatrixGridHit.Miss;
        }

        return new MatrixGridHit(
            kind,
            rowIndex,
            DataColumnAt(displayIndex),
            FixedOrdinalAt(displayIndex),
            displayIndex);
    }

    /// <summary>Which display column's right edge a point is grabbing, for a resize drag.</summary>
    /// <param name="point">A point in the control's own coordinates.</param>
    /// <param name="horizontalOffset">Current horizontal scroll offset.</param>
    /// <returns>
    /// The display index whose width the drag would change, or <see cref="MatrixGrid.NoIndex"/> when
    /// the point is not on a grip.
    /// </returns>
    /// <remarks>
    /// Delphi enabled this with <c>goColSizing</c>, which only grips inside the fixed <em>row</em> -
    /// so the caller is expected to have established that <paramref name="point"/> is in the header.
    /// The grip straddles the edge, so grabbing the first pixel of column <i>n</i> resizes column
    /// <i>n-1</i>, which is what every grid does.
    /// </remarks>
    public int ColumnResizeTargetAt(Point point, double horizontalOffset)
    {
        if (point.X < 0 || DisplayColumnCount == 0)
        {
            return MatrixGrid.NoIndex;
        }

        // The frozen block is painted last and wins hit-testing, so its trailing edge wins the grip
        // too. Without this the identity columns would stop being resizable the moment a data
        // column's edge scrolled under that same x.
        int lastFrozen = FixedColumnCount - 1;

        if (IsOnRightEdgeOf(lastFrozen, point.X, horizontalOffset))
        {
            return lastFrozen;
        }

        // Only three edges can be within a grip of any x: the two around the column under the
        // pointer, and the grid's own right edge.  Scanning all of them would cost a linear pass per
        // mouse move, and this control is built for a thousand columns.
        int under = DisplayColumnAtX(point.X, horizontalOffset);
        int last = DisplayColumnCount - 1;

        if (under == MatrixGrid.NoIndex)
        {
            return IsOnRightEdgeOf(last, point.X, horizontalOffset) ? last : MatrixGrid.NoIndex;
        }

        if (IsOnRightEdgeOf(under, point.X, horizontalOffset))
        {
            return under;
        }

        return under > 0 && IsOnRightEdgeOf(under - 1, point.X, horizontalOffset)
            ? under - 1
            : MatrixGrid.NoIndex;
    }

    private bool IsOnRightEdgeOf(int displayIndex, double x, double horizontalOffset)
    {
        if (displayIndex < 0 || displayIndex >= DisplayColumnCount)
        {
            return false;
        }

        double[] offsets = Offsets;
        double frozen = offsets[FixedColumnCount];
        bool isFrozen = displayIndex < FixedColumnCount;
        double edge = isFrozen ? offsets[displayIndex + 1] : offsets[displayIndex + 1] - horizontalOffset;

        // An edge that has scrolled in behind the frozen block is covered by it and must not be
        // grabbable through it. Strictly greater, so an edge sitting exactly on the boundary belongs
        // to the frozen column that is drawn there.
        return (isFrozen || edge > frozen) && Math.Abs(x - edge) <= ResizeGripWidth;
    }

    /// <summary>
    /// The horizontal scroll offset that brings a data column fully into view, moving as little as
    /// possible.
    /// </summary>
    /// <param name="dataColumnIndex">Index into <see cref="PersonMatrix.Columns"/>.</param>
    /// <param name="horizontalOffset">The current offset.</param>
    /// <param name="viewportWidth">Width of the scrollable band, frozen block excluded.</param>
    /// <returns>The offset to scroll to; the current one when the column is already visible.</returns>
    public double OffsetToShowColumn(int dataColumnIndex, double horizontalOffset, double viewportWidth)
    {
        int displayIndex = DisplayIndexOfData(dataColumnIndex);

        if (displayIndex == MatrixGrid.NoIndex || viewportWidth <= 0)
        {
            return horizontalOffset;
        }

        double[] offsets = Offsets;
        double frozen = offsets[FixedColumnCount];
        double left = offsets[displayIndex] - frozen;
        double right = offsets[displayIndex + 1] - frozen;

        if (left < horizontalOffset)
        {
            return left;
        }

        if (right > horizontalOffset + viewportWidth)
        {
            // Never scroll so far that the column's own left edge leaves the viewport: a column
            // wider than the band must be shown from its start, not from its end.
            return Math.Min(left, right - viewportWidth);
        }

        return horizontalOffset;
    }

    /// <summary>The next data-column boundary strictly to the right of an offset.</summary>
    /// <param name="horizontalOffset">The current offset into the scrollable band.</param>
    /// <returns>The offset of the next boundary, or <see cref="DataWidth"/> when there is none.</returns>
    /// <remarks>
    /// A scroll-bar arrow moves the view by one column, not by a fixed pixel count - otherwise a
    /// grid whose columns have been dragged to different widths drifts a little on every click.
    /// </remarks>
    public double NextColumnBoundary(double horizontalOffset)
    {
        double[] offsets = Offsets;
        double frozen = offsets[FixedColumnCount];
        int index = FindColumnAt(offsets, frozen + Math.Max(0, horizontalOffset), FixedColumnCount);

        // The column under the offset always ends to the right of it, because FindColumnAt returns
        // the column whose half-open span contains the point.
        return index == MatrixGrid.NoIndex ? DataWidth : offsets[index + 1] - frozen;
    }

    /// <summary>The previous data-column boundary strictly to the left of an offset.</summary>
    /// <param name="horizontalOffset">The current offset into the scrollable band.</param>
    /// <returns>The offset of the previous boundary, or zero when there is none.</returns>
    public double PreviousColumnBoundary(double horizontalOffset)
    {
        if (horizontalOffset <= 0 || DataColumnCount == 0)
        {
            return 0;
        }

        double[] offsets = Offsets;
        double frozen = offsets[FixedColumnCount];
        int index = FindColumnAt(offsets, frozen + horizontalOffset, FixedColumnCount);

        if (index == MatrixGrid.NoIndex)
        {
            index = DisplayColumnCount - 1;
        }

        double left = offsets[index] - frozen;

        if (left < horizontalOffset - BoundaryEpsilon)
        {
            return left;
        }

        return index > FixedColumnCount ? offsets[index - 1] - frozen : 0;
    }

    /// <summary>The vertical scroll offset that brings a row fully into view.</summary>
    /// <param name="rowIndex">Index into <see cref="PersonMatrix.Rows"/>.</param>
    /// <param name="verticalOffset">The current offset.</param>
    /// <param name="viewportHeight">Height of the data band, header row excluded.</param>
    /// <returns>The offset to scroll to; the current one when the row is already visible.</returns>
    public double OffsetToShowRow(int rowIndex, double verticalOffset, double viewportHeight)
    {
        if (rowIndex < 0 || rowIndex >= _rowCount || viewportHeight <= 0)
        {
            return verticalOffset;
        }

        double top = rowIndex * _rowHeight;
        double bottom = top + _rowHeight;

        if (top < verticalOffset)
        {
            return top;
        }

        if (bottom > verticalOffset + viewportHeight)
        {
            return Math.Min(top, bottom - viewportHeight);
        }

        return verticalOffset;
    }

    private int DisplayColumnAtX(double x, double horizontalOffset)
    {
        double[] offsets = Offsets;
        double frozen = offsets[FixedColumnCount];

        if (x < frozen)
        {
            return FindColumnAt(offsets, x, 0);
        }

        int index = FindColumnAt(offsets, x + horizontalOffset, FixedColumnCount);

        // Guard the case where the scrolled x lands back inside the frozen block: that column is
        // hidden behind the frozen band and cannot be clicked.
        return index >= FixedColumnCount ? index : MatrixGrid.NoIndex;
    }

    private int FindColumnAt(double[] offsets, double x, int firstCandidate)
    {
        if (x < offsets[firstCandidate] || x >= offsets[DisplayColumnCount])
        {
            return MatrixGrid.NoIndex;
        }

        // Binary search over the prefix sums: hundreds of columns must not cost a linear scan per
        // mouse move.
        int low = firstCandidate;
        int high = DisplayColumnCount - 1;

        while (low < high)
        {
            int middle = low + ((high - low + 1) / 2);

            if (offsets[middle] <= x)
            {
                low = middle;
            }
            else
            {
                high = middle - 1;
            }
        }

        return low;
    }

    private void RebuildOffsets()
    {
        int columns = DisplayColumnCount;

        if (_offsets.Length != columns + 1)
        {
            _offsets = new double[columns + 1];
        }

        double running = 0;

        _offsets[0] = 0;

        for (int displayIndex = 0; displayIndex < columns; displayIndex++)
        {
            running += displayIndex < FixedColumnCount
                ? _fixedWidths[_visibleFixedOrdinals[displayIndex]]
                : DataWidthOf(displayIndex - FixedColumnCount);

            _offsets[displayIndex + 1] = running;
        }

        _offsetsAreStale = false;
    }

    private double DataWidthOf(int dataColumnIndex) =>
        _dataWidthOverrides.TryGetValue(dataColumnIndex, out double width) ? width : _dataColumnWidth;

    private void ThrowIfNotAColumn(int displayIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(displayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(displayIndex, DisplayColumnCount);
    }
}
