using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;

namespace QuickStat.Controls.Dataset;

/// <summary>
/// The dataset grid: a virtualised, owner-drawn person × variable table.
/// </summary>
/// <remarks>
/// <para>
/// <b>This file is the 3.1 ↔ 3.5 contract, and step 3.5 owns it.</b> Everything below is declared so
/// that step 3.1 can data-bind the Dataset tab against a fixed surface while step 3.5 implements the
/// control in parallel. Step 3.5 may <i>add</i> members freely; it must not rename or remove one,
/// because 3.1's XAML binds to these names. Nothing outside
/// <c>QuickStat.App/Controls/Dataset/</c> may be edited by 3.5.
/// </para>
/// <para>
/// Rendering, hit-testing, scrolling (<c>IScrollInfo</c>), keyboard navigation, tooltips and the
/// automation peer are all step 3.5's to write; see <c>Docs/Port/05-ui-spec.md</c> §C.3 for the
/// painting rules and <c>PORT-PLAN.md</c> §5 Phase 3 for why this is a custom
/// <see cref="FrameworkElement"/> rather than a <c>DataGrid</c>.
/// </para>
/// <para>
/// Delphi: <c>TStudyOverviewGrid</c> (<c>EPR.QA.GUI.Grid.Study.pas</c>), a <c>TCustomDrawGrid</c>
/// subclass.
/// </para>
/// </remarks>
public class MatrixGrid : FrameworkElement
{
    /// <summary>The value <see cref="CurrentRowIndex"/> and <see cref="CurrentColumnIndex"/> take when nothing is current.</summary>
    public const int NoIndex = -1;

    /// <summary>Data-column width when <i>Wide columns</i> is off.</summary>
    /// <remarks><c>DataColWidth := 64</c> in <c>MainQuickStat.pas</c>.</remarks>
    public const double NarrowDataColumnWidth = 64;

    /// <summary>Data-column width when <i>Wide columns</i> is on.</summary>
    /// <remarks><c>DataColWidth := 120</c> in <c>MainQuickStat.pas</c>.</remarks>
    public const double WideDataColumnWidth = 120;

    /// <summary>Identifies the <see cref="Matrix"/> dependency property.</summary>
    public static readonly DependencyProperty MatrixProperty = DependencyProperty.Register(
        nameof(Matrix),
        typeof(PersonMatrix),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(
            null,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="Identification"/> dependency property.</summary>
    public static readonly DependencyProperty IdentificationProperty = DependencyProperty.Register(
        nameof(Identification),
        typeof(PersonIdentification),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(
            PersonIdentification.PersonIdOnly,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="DataColumnWidth"/> dependency property.</summary>
    public static readonly DependencyProperty DataColumnWidthProperty = DependencyProperty.Register(
        nameof(DataColumnWidth),
        typeof(double),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(
            NarrowDataColumnWidth,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="CurrentRowIndex"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentRowIndexProperty = DependencyProperty.Register(
        nameof(CurrentRowIndex),
        typeof(int),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(
            NoIndex,
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Identifies the <see cref="CurrentColumnIndex"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentColumnIndexProperty = DependencyProperty.Register(
        nameof(CurrentColumnIndex),
        typeof(int),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(
            NoIndex,
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Identifies the <see cref="GridLineBrush"/> dependency property.</summary>
    public static readonly DependencyProperty GridLineBrushProperty = DependencyProperty.Register(
        nameof(GridLineBrush),
        typeof(Brush),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(Frozen("#E2E6E6"), FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="FixedCellBackground"/> dependency property.</summary>
    public static readonly DependencyProperty FixedCellBackgroundProperty = DependencyProperty.Register(
        nameof(FixedCellBackground),
        typeof(Brush),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(Frozen("#F4FBFB"), FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="FixedCellForeground"/> dependency property.</summary>
    public static readonly DependencyProperty FixedCellForegroundProperty = DependencyProperty.Register(
        nameof(FixedCellForeground),
        typeof(Brush),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(Frozen("#035F66"), FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="CurrentCellBackground"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentCellBackgroundProperty = DependencyProperty.Register(
        nameof(CurrentCellBackground),
        typeof(Brush),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(Frozen("#FFFBD4"), FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="CurrentRowBackground"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentRowBackgroundProperty = DependencyProperty.Register(
        nameof(CurrentRowBackground),
        typeof(Brush),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(Frozen("#F3F9FE"), FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="MissingObjectBackground"/> dependency property.</summary>
    public static readonly DependencyProperty MissingObjectBackgroundProperty = DependencyProperty.Register(
        nameof(MissingObjectBackground),
        typeof(Brush),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(Frozen("#FFFAFA"), FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="FontFamily"/> dependency property.</summary>
    public static readonly DependencyProperty FontFamilyProperty =
        TextElement.FontFamilyProperty.AddOwner(
            typeof(MatrixGrid),
            new FrameworkPropertyMetadata(
                FrameworkPropertyMetadataOptions.Inherits
                | FrameworkPropertyMetadataOptions.AffectsMeasure
                | FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="FontSize"/> dependency property.</summary>
    public static readonly DependencyProperty FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner(
            typeof(MatrixGrid),
            new FrameworkPropertyMetadata(
                FrameworkPropertyMetadataOptions.Inherits
                | FrameworkPropertyMetadataOptions.AffectsMeasure
                | FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="Foreground"/> dependency property.</summary>
    public static readonly DependencyProperty ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner(
            typeof(MatrixGrid),
            new FrameworkPropertyMetadata(FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Raised when the user activates a cell, which in this control means a single click.</summary>
    /// <remarks>
    /// The floating data hint is repositioned on click and on nothing else - not on hover, not on
    /// keyboard movement (<c>05-ui-spec.md</c> §G.2). Keeping that a distinct event, rather than
    /// letting the Dataset tab watch <see cref="CurrentRowIndex"/>, is what preserves the
    /// distinction.
    /// </remarks>
    public event EventHandler<MatrixGridCellEventArgs>? CellActivated;

    /// <summary>The dataset to display. <see langword="null"/> renders an empty grid.</summary>
    /// <remarks>
    /// <see cref="PersonMatrix"/> is mutable and raises no change notification, so a caller that
    /// mutates the same instance must call <see cref="Refresh"/>.
    /// </remarks>
    public PersonMatrix? Matrix
    {
        get => (PersonMatrix?)GetValue(MatrixProperty);
        set => SetValue(MatrixProperty, value);
    }

    /// <summary>Which identity columns are shown.</summary>
    /// <remarks>
    /// Resolve it through <see cref="IdentificationColumns.For"/> rather than switching on the enum:
    /// display and export anonymity share that one derivation on purpose (PORT-PLAN.md §7.2), and a
    /// second interpretation here is exactly the divergence it exists to prevent. Anything other
    /// than <see cref="PersonIdentification.Full"/> hides <c>Født</c>, <c>Fødselsnummer</c> and
    /// <c>Navn</c> outright - no blank column.
    /// </remarks>
    public PersonIdentification Identification
    {
        get => (PersonIdentification)GetValue(IdentificationProperty);
        set => SetValue(IdentificationProperty, value);
    }

    /// <summary>Width of every data column, in device-independent units.</summary>
    /// <remarks>
    /// The <i>Wide columns</i> check box switches between <see cref="NarrowDataColumnWidth"/> and
    /// <see cref="WideDataColumnWidth"/>. The four fixed columns have their own widths (44 / 64 / 84
    /// / 128) and are not affected.
    /// </remarks>
    public double DataColumnWidth
    {
        get => (double)GetValue(DataColumnWidthProperty);
        set => SetValue(DataColumnWidthProperty, value);
    }

    /// <summary>Index of the current row into <see cref="PersonMatrix.Rows"/>, or <see cref="NoIndex"/>.</summary>
    public int CurrentRowIndex
    {
        get => (int)GetValue(CurrentRowIndexProperty);
        set => SetValue(CurrentRowIndexProperty, value);
    }

    /// <summary>
    /// Index of the current column into <see cref="PersonMatrix.Columns"/>, or <see cref="NoIndex"/>
    /// when the current cell is one of the fixed identity columns or there is none.
    /// </summary>
    /// <remarks>
    /// Deliberately <i>not</i> a display-column index. The consumer's next move is always
    /// <c>Matrix.GetCell(row, column)</c> or <c>Matrix.TryGetDataPoint(row, column)</c>, and a
    /// display index would have to be translated back - once per consumer, differently each time.
    /// </remarks>
    public int CurrentColumnIndex
    {
        get => (int)GetValue(CurrentColumnIndexProperty);
        set => SetValue(CurrentColumnIndexProperty, value);
    }

    /// <summary>Line between data columns. Delphi <c>clSilver</c>, modernised.</summary>
    public Brush GridLineBrush
    {
        get => (Brush)GetValue(GridLineBrushProperty);
        set => SetValue(GridLineBrushProperty, value);
    }

    /// <summary>Fill behind the header row and the frozen identity columns.</summary>
    public Brush FixedCellBackground
    {
        get => (Brush)GetValue(FixedCellBackgroundProperty);
        set => SetValue(FixedCellBackgroundProperty, value);
    }

    /// <summary>Text colour in the header row and the <c>PID</c> column.</summary>
    public Brush FixedCellForeground
    {
        get => (Brush)GetValue(FixedCellForegroundProperty);
        set => SetValue(FixedCellForegroundProperty, value);
    }

    /// <summary>Fill for the current cell. Overrides every other cell colour.</summary>
    public Brush CurrentCellBackground
    {
        get => (Brush)GetValue(CurrentCellBackgroundProperty);
        set => SetValue(CurrentCellBackgroundProperty, value);
    }

    /// <summary>Tint blended over the other cells of the current row.</summary>
    public Brush CurrentRowBackground
    {
        get => (Brush)GetValue(CurrentRowBackgroundProperty);
        set => SetValue(CurrentRowBackgroundProperty, value);
    }

    /// <summary>Fill for a cell with no object behind it at all.</summary>
    /// <remarks>
    /// Delphi <c>clWebSnow</c>, distinct from the <c>#F5F5F5</c> that
    /// <see cref="PersonMatrix.GetCell"/> already returns for a known variable with no value. Step
    /// 3.5 should establish whether that first case is still reachable in the ported model and
    /// report the answer rather than inventing one.
    /// </remarks>
    public Brush MissingObjectBackground
    {
        get => (Brush)GetValue(MissingObjectBackgroundProperty);
        set => SetValue(MissingObjectBackgroundProperty, value);
    }

    /// <summary>Typeface for every cell.</summary>
    public FontFamily FontFamily
    {
        get => (FontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    /// <summary>Font size for every cell.</summary>
    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>Default text colour for data cells.</summary>
    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>Re-reads <see cref="Matrix"/> and repaints.</summary>
    /// <remarks>
    /// Needed because <see cref="PersonMatrix"/> is a plain mutable object: a collect run adds
    /// columns and datapoints to the instance already bound here, and no property changes.
    /// </remarks>
    public virtual void Refresh() => InvalidateVisual();

    /// <summary>
    /// Gets the bounds of one cell, in this element's own coordinate space.
    /// </summary>
    /// <param name="rowIndex">Index into <see cref="PersonMatrix.Rows"/>.</param>
    /// <param name="columnIndex">
    /// Index into <see cref="PersonMatrix.Columns"/>, or <see cref="NoIndex"/> for the current
    /// fixed column.
    /// </param>
    /// <param name="bounds">The cell rectangle when the method returns <see langword="true"/>.</param>
    /// <returns>
    /// <see langword="true"/> when the cell exists and is currently laid out; otherwise
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// This exists for the floating data-hint panel, which anchors itself just below the clicked
    /// cell. Scrolled-out cells return <see langword="false"/> rather than an off-screen rectangle,
    /// so the caller hides the hint instead of parking it outside the window.
    /// </remarks>
    public virtual bool TryGetCellBounds(int rowIndex, int columnIndex, out Rect bounds)
    {
        bounds = default;

        return false;
    }

    /// <summary>Raises <see cref="CellActivated"/>.</summary>
    /// <param name="e">The activated cell.</param>
    /// <exception cref="ArgumentNullException"><paramref name="e"/> is <see langword="null"/>.</exception>
    protected virtual void OnCellActivated(MatrixGridCellEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        CellActivated?.Invoke(this, e);
    }

    private static SolidColorBrush Frozen(string hex)
    {
        SolidColorBrush brush = new((Color)ColorConverter.ConvertFromString(hex));

        // Frozen so the default is shared across every instance and can never be mutated by one
        // consumer on behalf of all of them.
        brush.Freeze();

        return brush;
    }
}
