using System.Globalization;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.DataPoints;
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
/// <para>
/// <b>How it virtualises.</b> Nothing is instantiated per cell - there is no visual tree below this
/// element at all. Each frame resolves the viewport to one <see cref="MatrixGridRange"/> of rows and
/// one of columns (<see cref="MatrixGridLayout.VisibleRows"/> /
/// <see cref="MatrixGridLayout.VisibleDataColumns"/>) and asks
/// <see cref="PersonMatrix.GetCell"/> only about those. Column widths are held as prefix sums so
/// hit-testing a thousand columns is a binary search, brushes are cached and frozen, and the frozen
/// identity columns are simply painted last rather than living in a second panel.
/// </para>
/// <para>
/// <b>Host requirements.</b> Put it inside a <c>ScrollViewer</c> with
/// <c>CanContentScroll="True"</c>; the control implements <see cref="IScrollInfo"/> and drives both
/// bars itself. Do not give it a <c>Height</c> - it fills what it is given and scrolls the rest.
/// </para>
/// </remarks>
public class MatrixGrid : FrameworkElement, IScrollInfo
{
    /// <summary>The value <see cref="CurrentRowIndex"/> and <see cref="CurrentColumnIndex"/> take when nothing is current.</summary>
    public const int NoIndex = -1;

    /// <summary>Data-column width when <i>Wide columns</i> is off.</summary>
    /// <remarks><c>DataColWidth := 64</c> in <c>MainQuickStat.pas</c>.</remarks>
    public const double NarrowDataColumnWidth = 64;

    /// <summary>Data-column width when <i>Wide columns</i> is on.</summary>
    /// <remarks><c>DataColWidth := 120</c> in <c>MainQuickStat.pas</c>.</remarks>
    public const double WideDataColumnWidth = 120;

    /// <summary>Space between a cell's edge and its text, horizontally. Delphi <c>FGapX = 3</c>.</summary>
    public const double CellPaddingX = 3;

    /// <summary>Space between a cell's edge and its text, vertically. Delphi <c>FGapY = 1</c>.</summary>
    public const double CellPaddingY = 1;

    /// <summary>Thickness of every grid line.</summary>
    public const double GridLineThickness = 1;

    /// <summary>Identifies the <see cref="Matrix"/> dependency property.</summary>
    public static readonly DependencyProperty MatrixProperty = DependencyProperty.Register(
        nameof(Matrix),
        typeof(PersonMatrix),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(
            null,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
            OnMatrixChanged));

    /// <summary>Identifies the <see cref="Identification"/> dependency property.</summary>
    public static readonly DependencyProperty IdentificationProperty = DependencyProperty.Register(
        nameof(Identification),
        typeof(PersonIdentification),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(
            PersonIdentification.PersonIdOnly,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
            OnStructureChanged));

    /// <summary>Identifies the <see cref="DataColumnWidth"/> dependency property.</summary>
    public static readonly DependencyProperty DataColumnWidthProperty = DependencyProperty.Register(
        nameof(DataColumnWidth),
        typeof(double),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(
            NarrowDataColumnWidth,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
            OnStructureChanged));

    /// <summary>Identifies the <see cref="RowHeight"/> dependency property.</summary>
    public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(
        nameof(RowHeight),
        typeof(double),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(
            MatrixGridLayout.DefaultRowHeight,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
            OnStructureChanged));

    /// <summary>Identifies the <see cref="HeaderRowHeight"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderRowHeightProperty = DependencyProperty.Register(
        nameof(HeaderRowHeight),
        typeof(double),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(
            MatrixGridLayout.DefaultHeaderHeight,
            FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
            OnStructureChanged));

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

    /// <summary>Identifies the <see cref="Background"/> dependency property.</summary>
    public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
        nameof(Background),
        typeof(Brush),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(Frozen("#FFFFFF"), FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="GridLineBrush"/> dependency property.</summary>
    public static readonly DependencyProperty GridLineBrushProperty = DependencyProperty.Register(
        nameof(GridLineBrush),
        typeof(Brush),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(Frozen("#E2E6E6"), FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="FixedLineBrush"/> dependency property.</summary>
    public static readonly DependencyProperty FixedLineBrushProperty = DependencyProperty.Register(
        nameof(FixedLineBrush),
        typeof(Brush),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(Frozen("#D0D6D6"), FrameworkPropertyMetadataOptions.AffectsRender));

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

    /// <summary>Identifies the <see cref="CurrentRowTint"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentRowTintProperty = DependencyProperty.Register(
        nameof(CurrentRowTint),
        typeof(Brush),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(Frozen("#E7F2FC"), FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="MissingObjectBackground"/> dependency property.</summary>
    public static readonly DependencyProperty MissingObjectBackgroundProperty = DependencyProperty.Register(
        nameof(MissingObjectBackground),
        typeof(Brush),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(Frozen("#FFFAFA"), FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Default font size for cell text. <c>05-ui-spec.md</c> §F.3: the port's grid is 12.</summary>
    public const double DefaultFontSize = 12;

    /// <summary>Identifies the <see cref="FontFamily"/> dependency property.</summary>
    /// <remarks>
    /// <b>Every <c>AddOwner</c> below passes an explicit default.</b> The obvious-looking
    /// <c>new FrameworkPropertyMetadata(FrameworkPropertyMetadataOptions.Inherits | …)</c> compiles,
    /// because it binds to the <c>(object defaultValue)</c> overload and boxes the flags - and then
    /// throws <c>ArgumentException: Default value type does not match type of property</c> from the
    /// static constructor the first time anything instantiates the control. There is no
    /// flags-only overload.
    /// </remarks>
    public static readonly DependencyProperty FontFamilyProperty =
        TextElement.FontFamilyProperty.AddOwner(
            typeof(MatrixGrid),
            new FrameworkPropertyMetadata(
                SystemFonts.MessageFontFamily,
                FrameworkPropertyMetadataOptions.Inherits
                | FrameworkPropertyMetadataOptions.AffectsMeasure
                | FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="FontSize"/> dependency property.</summary>
    public static readonly DependencyProperty FontSizeProperty =
        TextElement.FontSizeProperty.AddOwner(
            typeof(MatrixGrid),
            new FrameworkPropertyMetadata(
                DefaultFontSize,
                FrameworkPropertyMetadataOptions.Inherits
                | FrameworkPropertyMetadataOptions.AffectsMeasure
                | FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="FontWeight"/> dependency property.</summary>
    public static readonly DependencyProperty FontWeightProperty =
        TextElement.FontWeightProperty.AddOwner(
            typeof(MatrixGrid),
            new FrameworkPropertyMetadata(
                FontWeights.Normal,
                FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="EmphasisFontWeight"/> dependency property.</summary>
    public static readonly DependencyProperty EmphasisFontWeightProperty = DependencyProperty.Register(
        nameof(EmphasisFontWeight),
        typeof(FontWeight),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(FontWeights.SemiBold, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="Foreground"/> dependency property.</summary>
    public static readonly DependencyProperty ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner(
            typeof(MatrixGrid),
            new FrameworkPropertyMetadata(
                Brushes.Black,
                FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="CellCulture"/> dependency property.</summary>
    public static readonly DependencyProperty CellCultureProperty = DependencyProperty.Register(
        nameof(CellCulture),
        typeof(CultureInfo),
        typeof(MatrixGrid),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    private readonly MatrixGridLayout _layout = new();
    private readonly MatrixGridPalette _palette = new();

    private Size _viewport;
    private double _horizontalOffset;
    private double _verticalOffset;
    private MatrixGridHit _hover = MatrixGridHit.Miss;
    private ToolTip? _toolTip;
    private int _resizingColumn = NoIndex;
    private double _resizeOriginX;
    private double _resizeOriginWidth;
    private Typeface _regularTypeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
    private Typeface _emphasisTypeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private double _pixelsPerDip = 1;
    private MatrixGridRenderStatistics _lastRender;

    static MatrixGrid()
    {
        FocusableProperty.OverrideMetadata(typeof(MatrixGrid), new FrameworkPropertyMetadata(true));
    }

    /// <summary>Creates an empty grid.</summary>
    public MatrixGrid()
    {
        SyncFixedColumns();
        SyncMetrics();
    }

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
    /// <para>
    /// Assigning it also discards every hand-dragged column width, which is what the Delphi's
    /// <c>DefaultColWidth</c> assignment did (<c>EPR.QA.GUI.Grid.pas:338-347</c>). Toggling
    /// <i>Wide columns</i> twice is therefore how a user undoes a mis-drag.
    /// </para>
    /// </remarks>
    public double DataColumnWidth
    {
        get => (double)GetValue(DataColumnWidthProperty);
        set => SetValue(DataColumnWidthProperty, value);
    }

    /// <summary>Height of one data row. Delphi <c>DefaultRowHeight := 17</c>.</summary>
    public double RowHeight
    {
        get => (double)GetValue(RowHeightProperty);
        set => SetValue(RowHeightProperty, value);
    }

    /// <summary>Height of the single header row. Delphi <c>RowHeights[0] := 18</c>.</summary>
    /// <remarks>
    /// One header row, not two. The Delphi's <c>TPersonGrid</c> carries dead branches for a second
    /// header row carrying a column subtitle, but <c>FixedRows = 1</c> makes them unreachable and
    /// the subtitle was never assigned anyway.
    /// </remarks>
    public double HeaderRowHeight
    {
        get => (double)GetValue(HeaderRowHeightProperty);
        set => SetValue(HeaderRowHeightProperty, value);
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

    /// <summary>Fill behind the whole control, including the area past the last row and column.</summary>
    /// <remarks>
    /// Also what makes the control hit-testable: a <see cref="FrameworkElement"/> is only hit where
    /// it has drawn something, so this rectangle is painted first, every frame.
    /// </remarks>
    public Brush Background
    {
        get => (Brush)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>Line between data columns and between data rows. Delphi <c>clSilver</c>, modernised.</summary>
    public Brush GridLineBrush
    {
        get => (Brush)GetValue(GridLineBrushProperty);
        set => SetValue(GridLineBrushProperty, value);
    }

    /// <summary>
    /// The darker line under the header row and down the right edge of the frozen block.
    /// </summary>
    /// <remarks>
    /// §C.3: "the fixed (<c>PID</c>) column and the header row are separated by a darker line".
    /// Defaults to the theme's <c>QsBorderBrush</c> <c>#D0D6D6</c>, which §F.4 records as the
    /// modernisation of the Delphi's <c>#A0A0A0</c>. There is deliberately no line <i>inside</i> the
    /// frozen block: the Delphi removed <c>goFixedVertLine</c>.
    /// </remarks>
    public Brush FixedLineBrush
    {
        get => (Brush)GetValue(FixedLineBrushProperty);
        set => SetValue(FixedLineBrushProperty, value);
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

    /// <summary>Fill for an <i>uncoloured</i> cell in the current row.</summary>
    /// <remarks>
    /// A risk-coloured cell is not filled with this - it is blended toward
    /// <see cref="CurrentRowTint"/> instead, so selecting a row cannot hide a red haemoglobin. The
    /// two agree by construction: blending the default white with the default tint at 50 % produces
    /// exactly this brush's default, <c>#F3F9FE</c>.
    /// </remarks>
    public Brush CurrentRowBackground
    {
        get => (Brush)GetValue(CurrentRowBackgroundProperty);
        set => SetValue(CurrentRowBackgroundProperty, value);
    }

    /// <summary>The 50 % blend partner for a coloured cell in the current row.</summary>
    /// <remarks>
    /// Delphi <c>clUnfocusedSelectionColor</c> <c>#E7F2FC</c>, used through
    /// <c>BlendColors(cellColour, CurrentRowColor, 50)</c> (<c>Grid.Study.pas:226</c>). Distinct from
    /// <see cref="CurrentRowBackground"/>, which is the <i>result</i> of that blend over a white
    /// cell; §F.1 records the result and this is the input. Setting only one of the two is a partial
    /// override.
    /// </remarks>
    public Brush CurrentRowTint
    {
        get => (Brush)GetValue(CurrentRowTintProperty);
        set => SetValue(CurrentRowTintProperty, value);
    }

    /// <summary>Fill for a cell with no object behind it at all.</summary>
    /// <remarks>
    /// <para>
    /// Delphi <c>clWebSnow</c> <c>#FFFAFA</c>, distinct from the <c>#F5F5F5</c> that
    /// <see cref="PersonMatrix.GetCell"/> already returns for a known variable with no value.
    /// </para>
    /// <para>
    /// <b>Unreachable in the port, and kept only because it is part of the 3.1 contract.</b> In the
    /// Delphi the two were genuinely different states, because cells were <c>TObject</c> references
    /// in a sparse array and a slot could be empty: the grid always had at least one data row and
    /// one data column (<c>ColCount := FixedCols + max(n,1)</c>, <c>EPR.QA.GUI.Grid.pas:180-181</c>,
    /// <c>:335</c>, <c>:351</c>), so a cleared or not-yet-collected grid painted a phantom column of
    /// snow, and during a collect run the new columns were snow until <c>Lock</c> filled every slot
    /// with either a datapoint or the column object. The port has no sparse array, no phantom
    /// row or column and no unlocked intermediate state - the renderer only ever addresses
    /// <c>0 … Rows.Count-1</c> × <c>0 … Columns.Count-1</c>, and <see cref="PersonMatrix.GetCell"/>
    /// answers every one of those with a real colour. Nothing assigns this brush.
    /// </para>
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

    /// <summary>Weight of ordinary cell text.</summary>
    public FontWeight FontWeight
    {
        get => (FontWeight)GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    /// <summary>Weight of the header row and the current row.</summary>
    /// <remarks>
    /// The Delphi used <c>[fsBold]</c>; §F.3 specifies <c>SemiBold</c> for the port's grid header,
    /// which is what this defaults to.
    /// </remarks>
    public FontWeight EmphasisFontWeight
    {
        get => (FontWeight)GetValue(EmphasisFontWeightProperty);
        set => SetValue(EmphasisFontWeightProperty, value);
    }

    /// <summary>Default text colour for data cells.</summary>
    public Brush Foreground
    {
        get => (Brush)GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>
    /// Culture used to format the date of birth and the person id, and the hint text.
    /// <see langword="null"/> means <see cref="CultureInfo.CurrentCulture"/>.
    /// </summary>
    /// <remarks>
    /// Deliberately <b>not</b> <see cref="FrameworkElement.Language"/>. WPF defaults that to
    /// <c>en-US</c> whatever the machine is set to, which would render 14 August 2019 as
    /// <c>8/14/2019</c> on a Norwegian workstation; the Delphi used <c>DateToStr</c>, i.e. the OS
    /// locale. Tests set this explicitly so an assertion about a formatted date cannot pass here and
    /// fail on an English build agent.
    /// </remarks>
    public CultureInfo? CellCulture
    {
        get => (CultureInfo?)GetValue(CellCultureProperty);
        set => SetValue(CellCultureProperty, value);
    }

    /// <summary>Number of data rows currently laid out.</summary>
    public int RowCount => _layout.RowCount;

    /// <summary>Number of columns on screen, frozen identity columns included.</summary>
    public int DisplayColumnCount => _layout.DisplayColumnCount;

    /// <summary>Number of frozen identity columns on screen: four when fully identified, else one.</summary>
    public int FrozenColumnCount => _layout.FixedColumnCount;

    /// <summary>Combined width of the frozen block.</summary>
    public double FrozenWidth => _layout.FrozenWidth;

    /// <summary>The culture cell text is formatted in.</summary>
    public CultureInfo EffectiveCulture => CellCulture ?? CultureInfo.CurrentCulture;

    /// <inheritdoc />
    public bool CanHorizontallyScroll { get; set; }

    /// <inheritdoc />
    public bool CanVerticallyScroll { get; set; }

    /// <inheritdoc />
    /// <remarks>The data columns only: the frozen block never scrolls, so it is not part of the extent.</remarks>
    public double ExtentWidth => _layout.DataWidth;

    /// <inheritdoc />
    /// <remarks>The data rows only: the header row is pinned, so it is not part of the extent.</remarks>
    public double ExtentHeight => _layout.DataHeight;

    /// <inheritdoc />
    public double ViewportWidth => Math.Max(0, _viewport.Width - _layout.FrozenWidth);

    /// <inheritdoc />
    public double ViewportHeight => Math.Max(0, _viewport.Height - _layout.HeaderHeight);

    /// <inheritdoc />
    public double HorizontalOffset => _horizontalOffset;

    /// <inheritdoc />
    public double VerticalOffset => _verticalOffset;

    /// <inheritdoc />
    public ScrollViewer? ScrollOwner { get; set; }

    internal MatrixGridRenderStatistics LastRenderStatistics => _lastRender;

    internal int BrushCacheSize => _palette.Count;

    /// <summary>Re-reads <see cref="Matrix"/> and repaints.</summary>
    /// <remarks>
    /// Needed because <see cref="PersonMatrix"/> is a plain mutable object: a collect run adds
    /// columns and datapoints to the instance already bound here, and no property changes.
    /// </remarks>
    public virtual void Refresh()
    {
        SyncCounts();
        ClampCurrentCell();
        InvalidateMeasure();
        InvalidateVisual();
        ScrollOwner?.InvalidateScrollInfo();
    }

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
    /// <para>
    /// This exists for the floating data-hint panel, which anchors itself just below the clicked
    /// cell. Scrolled-out cells return <see langword="false"/> rather than an off-screen rectangle,
    /// so the caller hides the hint instead of parking it outside the window.
    /// </para>
    /// <para>
    /// <paramref name="columnIndex"/> of <see cref="NoIndex"/> resolves to the leading frozen
    /// column, i.e. <c>PID</c> - the cell a fixed-column click selected the row from.
    /// <paramref name="rowIndex"/> of <see cref="NoIndex"/> resolves to that column's header.
    /// The rectangle is the cell's full geometry even when it is only partly on screen; the caller
    /// is expected to clamp its own popup.
    /// </para>
    /// </remarks>
    public virtual bool TryGetCellBounds(int rowIndex, int columnIndex, out Rect bounds)
    {
        int displayIndex = columnIndex == NoIndex
            ? (_layout.FixedColumnCount > 0 ? 0 : NoIndex)
            : _layout.DisplayIndexOfData(columnIndex);

        return TryGetDisplayCellBounds(rowIndex, displayIndex, out bounds);
    }

    /// <summary>
    /// Gets the bounds of one cell addressed by its position on screen rather than by model index.
    /// </summary>
    /// <param name="rowIndex">Data row, or <see cref="NoIndex"/> for the header row.</param>
    /// <param name="displayColumnIndex">Column position on screen, frozen columns first.</param>
    /// <param name="bounds">The cell rectangle when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when the cell exists and is at least partly visible.</returns>
    public bool TryGetDisplayCellBounds(int rowIndex, int displayColumnIndex, out Rect bounds)
    {
        bounds = default;

        if (!_layout.TryGetCellBounds(rowIndex, displayColumnIndex, _horizontalOffset, _verticalOffset, out Rect cell))
        {
            return false;
        }

        if (!VisibleBandFor(rowIndex, displayColumnIndex).IntersectsWith(cell))
        {
            return false;
        }

        bounds = cell;

        return true;
    }

    /// <summary>Which kind of cell a display position addresses.</summary>
    /// <param name="rowIndex">Data row, or <see cref="NoIndex"/> for the header row.</param>
    /// <param name="displayColumnIndex">Column position on screen, frozen columns first.</param>
    /// <returns>The kind, or <see cref="MatrixGridCellKind.None"/>.</returns>
    public MatrixGridCellKind GetDisplayCellKind(int rowIndex, int displayColumnIndex) =>
        _layout.KindAt(rowIndex, displayColumnIndex);

    /// <summary>The display position of a data column.</summary>
    /// <param name="columnIndex">Index into <see cref="PersonMatrix.Columns"/>.</param>
    /// <returns>The display index, or <see cref="NoIndex"/>.</returns>
    public int DisplayIndexOfDataColumn(int columnIndex) => _layout.DisplayIndexOfData(columnIndex);

    /// <summary>The text drawn in one cell.</summary>
    /// <param name="rowIndex">Data row, or <see cref="NoIndex"/> for the header row.</param>
    /// <param name="displayColumnIndex">Column position on screen, frozen columns first.</param>
    /// <returns>The text, or an empty string when the position addresses no cell.</returns>
    /// <remarks>
    /// The automation peer reads cell values through here, so a screen reader and the screen agree
    /// by construction rather than by coincidence.
    /// </remarks>
    public string GetDisplayCellText(int rowIndex, int displayColumnIndex)
    {
        MatrixGridCellKind kind = _layout.KindAt(rowIndex, displayColumnIndex);
        PersonMatrix? matrix = Matrix;

        if (kind == MatrixGridCellKind.None || matrix is null)
        {
            return "";
        }

        int ordinal = _layout.FixedOrdinalAt(displayColumnIndex);
        int dataColumn = _layout.DataColumnAt(displayColumnIndex);

        return kind switch
        {
            MatrixGridCellKind.FixedHeader => FixedColumns.Header(ordinal),
            MatrixGridCellKind.ColumnHeader => matrix.Columns[dataColumn].Title,
            MatrixGridCellKind.Fixed => matrix.GetFixedCell(rowIndex, ordinal, EffectiveCulture).Text,
            _ => matrix.GetCell(rowIndex, dataColumn).Text,
        };
    }

    /// <summary>The tooltip for one cell, or <see langword="null"/> when it has none.</summary>
    /// <param name="rowIndex">Data row, or <see cref="NoIndex"/> for the header row.</param>
    /// <param name="displayColumnIndex">Column position on screen, frozen columns first.</param>
    /// <returns>The tooltip text.</returns>
    /// <remarks>
    /// <para>
    /// Delphi <c>CMHintShow</c> (<c>Grid.Study.pas:69-105</c>) resolves to almost nothing as shipped:
    /// a fixed header has no cell object so no hint fires at all; <c>TPersonGridRow</c> does not
    /// implement <c>ICellText</c>, so the identity cells hint with an empty string; and
    /// <c>TDataPoint.CellHint</c> returns <c>''</c> and is never overridden, so <b>data cells never
    /// had a tooltip either</b>. Only a data column's header could produce text, from
    /// <c>Data.Description(varName)</c>.
    /// </para>
    /// <para>
    /// Two deliberate additions, both reported: a data cell hints with
    /// <see cref="DataPoint.Describe(IFormatProvider)"/>, the same text the floating hint panel
    /// shows, because an empty tooltip is worth nothing; and <b>any</b> cell whose text this control
    /// has elided hints with the full text, because the ellipsis is the port's own doing.
    /// </para>
    /// </remarks>
    public string? GetDisplayCellToolTip(int rowIndex, int displayColumnIndex)
    {
        MatrixGridCellKind kind = _layout.KindAt(rowIndex, displayColumnIndex);
        PersonMatrix? matrix = Matrix;

        if (kind == MatrixGridCellKind.None || matrix is null)
        {
            return null;
        }

        string? description = kind == MatrixGridCellKind.ColumnHeader
            ? NullIfEmpty(matrix.Columns[_layout.DataColumnAt(displayColumnIndex)].Description)
            : null;

        if (kind == MatrixGridCellKind.Data
            && matrix.TryGetDataPoint(rowIndex, _layout.DataColumnAt(displayColumnIndex), out DataPoint? dataPoint))
        {
            description = dataPoint.Describe(EffectiveCulture);
        }

        string text = GetDisplayCellText(rowIndex, displayColumnIndex);

        if (!IsElided(text, displayColumnIndex, kind))
        {
            return description;
        }

        return description is null ? text : string.Concat(text, "\n", description);
    }

    /// <summary>Whether a cell's text is too wide for its column and will be drawn with an ellipsis.</summary>
    /// <param name="text">The text.</param>
    /// <param name="displayColumnIndex">Column position on screen, frozen columns first.</param>
    /// <param name="kind">Which cell kind, which decides the font weight.</param>
    /// <returns><see langword="true"/> when the text does not fit.</returns>
    /// <remarks>
    /// <see cref="FormattedText"/> works on an MTA thread, so column-fit logic is testable with no
    /// window and no dispatcher (PORT-PLAN.md §5 Phase 3).
    /// </remarks>
    public bool IsElided(string text, int displayColumnIndex, MatrixGridCellKind kind)
    {
        if (string.IsNullOrEmpty(text) || displayColumnIndex < 0 || displayColumnIndex >= _layout.DisplayColumnCount)
        {
            return false;
        }

        bool emphasis = kind is MatrixGridCellKind.FixedHeader or MatrixGridCellKind.ColumnHeader
            || (CurrentRowIndex != NoIndex && kind is MatrixGridCellKind.Fixed or MatrixGridCellKind.Data);

        return MeasureTextWidth(text, emphasis) > _layout.ColumnWidth(displayColumnIndex) - (2 * CellPaddingX);
    }

    /// <summary>Measures a run of cell text in the grid's own font.</summary>
    /// <param name="text">The text.</param>
    /// <param name="emphasised">Whether to measure in <see cref="EmphasisFontWeight"/>.</param>
    /// <returns>The width in device-independent units.</returns>
    public double MeasureTextWidth(string text, bool emphasised)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        UpdateTypefaces();

        FormattedText formatted = new(
            text,
            EffectiveCulture,
            FlowDirection.LeftToRight,
            emphasised ? _emphasisTypeface : _regularTypeface,
            FontSize,
            Brushes.Black,
            _pixelsPerDip);

        return formatted.WidthIncludingTrailingWhitespace;
    }

    /// <summary>Moves the caret to a cell and scrolls it into view.</summary>
    /// <param name="rowIndex">Index into <see cref="PersonMatrix.Rows"/>, or <see cref="NoIndex"/>.</param>
    /// <param name="columnIndex">Index into <see cref="PersonMatrix.Columns"/>, or <see cref="NoIndex"/>.</param>
    /// <remarks>Does <b>not</b> raise <see cref="CellActivated"/>; only a click does that.</remarks>
    public void SetCurrentCell(int rowIndex, int columnIndex)
    {
        CurrentRowIndex = rowIndex;
        CurrentColumnIndex = columnIndex;

        ScrollIntoView(rowIndex, columnIndex);
    }

    /// <summary>Scrolls a cell into view without moving the caret.</summary>
    /// <param name="rowIndex">Index into <see cref="PersonMatrix.Rows"/>, or <see cref="NoIndex"/>.</param>
    /// <param name="columnIndex">Index into <see cref="PersonMatrix.Columns"/>, or <see cref="NoIndex"/>.</param>
    public void ScrollIntoView(int rowIndex, int columnIndex)
    {
        if (rowIndex != NoIndex)
        {
            SetVerticalOffset(_layout.OffsetToShowRow(rowIndex, _verticalOffset, ViewportHeight));
        }

        if (columnIndex != NoIndex)
        {
            SetHorizontalOffset(_layout.OffsetToShowColumn(columnIndex, _horizontalOffset, ViewportWidth));
        }
    }

    /// <inheritdoc />
    public void LineUp() => SetVerticalOffset(_verticalOffset - _layout.RowHeight);

    /// <inheritdoc />
    public void LineDown() => SetVerticalOffset(_verticalOffset + _layout.RowHeight);

    /// <inheritdoc />
    /// <remarks>Steps to the previous column boundary, as a grid does, not by a fixed pixel count.</remarks>
    public void LineLeft() => SetHorizontalOffset(_layout.PreviousColumnBoundary(_horizontalOffset));

    /// <inheritdoc />
    /// <remarks>Steps to the next column boundary, as a grid does, not by a fixed pixel count.</remarks>
    public void LineRight() => SetHorizontalOffset(_layout.NextColumnBoundary(_horizontalOffset));

    /// <inheritdoc />
    public void PageUp() => SetVerticalOffset(_verticalOffset - ViewportHeight);

    /// <inheritdoc />
    public void PageDown() => SetVerticalOffset(_verticalOffset + ViewportHeight);

    /// <inheritdoc />
    public void PageLeft() => SetHorizontalOffset(_horizontalOffset - ViewportWidth);

    /// <inheritdoc />
    public void PageRight() => SetHorizontalOffset(_horizontalOffset + ViewportWidth);

    /// <inheritdoc />
    public void MouseWheelUp() => SetVerticalOffset(_verticalOffset - WheelStep());

    /// <inheritdoc />
    public void MouseWheelDown() => SetVerticalOffset(_verticalOffset + WheelStep());

    /// <inheritdoc />
    public void MouseWheelLeft() => LineLeft();

    /// <inheritdoc />
    public void MouseWheelRight() => LineRight();

    /// <inheritdoc />
    public void SetHorizontalOffset(double offset)
    {
        double clamped = Clamp(offset, ExtentWidth - ViewportWidth);

        if (_horizontalOffset.Equals(clamped))
        {
            return;
        }

        _horizontalOffset = clamped;

        ScrollOwner?.InvalidateScrollInfo();
        InvalidateVisual();
    }

    /// <inheritdoc />
    public void SetVerticalOffset(double offset)
    {
        double clamped = Clamp(offset, ExtentHeight - ViewportHeight);

        if (_verticalOffset.Equals(clamped))
        {
            return;
        }

        _verticalOffset = clamped;

        ScrollOwner?.InvalidateScrollInfo();
        InvalidateVisual();
    }

    /// <inheritdoc />
    public Rect MakeVisible(Visual visual, Rect rectangle)
    {
        if (rectangle.IsEmpty)
        {
            return rectangle;
        }

        // The rectangle arrives in this element's own coordinates, because there is no child visual
        // for it to be relative to.
        double frozen = _layout.FrozenWidth;
        double header = _layout.HeaderHeight;

        if (rectangle.Left < frozen)
        {
            SetHorizontalOffset(_horizontalOffset - (frozen - rectangle.Left));
        }
        else if (rectangle.Right > _viewport.Width)
        {
            SetHorizontalOffset(_horizontalOffset + (rectangle.Right - _viewport.Width));
        }

        if (rectangle.Top < header)
        {
            SetVerticalOffset(_verticalOffset - (header - rectangle.Top));
        }
        else if (rectangle.Bottom > _viewport.Height)
        {
            SetVerticalOffset(_verticalOffset + (rectangle.Bottom - _viewport.Height));
        }

        return rectangle;
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new MatrixGridAutomationPeer(this);

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        SyncMetrics();
        SyncCounts();

        double width = double.IsInfinity(availableSize.Width) ? _layout.TotalWidth : availableSize.Width;
        double height = double.IsInfinity(availableSize.Height) ? _layout.TotalHeight : availableSize.Height;

        return new Size(width, height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        _viewport = finalSize;

        // Clamping here rather than in the setters is what keeps a shrinking window, a narrowing
        // dataset and the Wide-columns toggle from leaving the view scrolled past its own content.
        SetHorizontalOffset(_horizontalOffset);
        SetVerticalOffset(_verticalOffset);

        ScrollOwner?.InvalidateScrollInfo();

        return finalSize;
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);

        base.OnRender(drawingContext);

        SyncMetrics();
        SyncCounts();
        UpdateTypefaces();

        double width = _viewport.Width;
        double height = _viewport.Height;

        drawingContext.DrawRectangle(Background, null, new Rect(0, 0, Math.Max(0, width), Math.Max(0, height)));

        _lastRender = default;

        PersonMatrix? matrix = Matrix;

        if (matrix is null || width <= 0 || height <= 0 || _layout.DisplayColumnCount == 0)
        {
            return;
        }

        MatrixGridColors colors = BuildColors();
        MatrixGridRange rows = _layout.VisibleRows(_verticalOffset, ViewportHeight);
        MatrixGridRange columns = _layout.VisibleDataColumns(_horizontalOffset, ViewportWidth);

        DrawDataBand(drawingContext, matrix, colors, rows, columns);
        DrawFrozenBand(drawingContext, matrix, colors, rows);
        DrawColumnHeaders(drawingContext, matrix, colors, columns);
        DrawFrozenHeaders(drawingContext, colors);
        DrawSeparators(drawingContext, rows);

        _lastRender = _lastRender with { Rows = rows.Count, Columns = columns.Count };
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        base.OnMouseLeftButtonDown(e);

        Focus();

        e.Handled = PressAt(e.GetPosition(this));
    }

    /// <inheritdoc />
    /// <remarks>
    /// Deliberately does nothing. §G.6 records that the VCL does not move the current cell on
    /// right-click, and that a WPF <c>DataGrid</c> would - but this is not a <c>DataGrid</c>, so the
    /// faithful behaviour is also the free one. The context menu therefore always acts on the cell
    /// the user last left-clicked, which is the cell the floating hint is describing.
    /// </remarks>
    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        base.OnMouseRightButtonDown(e);

        Focus();
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        base.OnMouseMove(e);

        MoveTo(e.GetPosition(this));
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        base.OnMouseLeftButtonUp(e);

        EndColumnResize();
    }

    /// <summary>Handles a primary press at a point in the control's own coordinates.</summary>
    /// <param name="point">Where the press landed.</param>
    /// <returns><see langword="true"/> when the press was consumed.</returns>
    /// <remarks>
    /// Split out of <see cref="OnMouseLeftButtonDown"/> so it can be driven from a test: a
    /// synthesised <see cref="MouseButtonEventArgs"/> reports the <em>real</em> cursor position, so
    /// there is no way to raise a click at a chosen cell without a window otherwise.
    /// </remarks>
    internal bool PressAt(Point point)
    {
        MatrixGridHit hit = _layout.HitTest(point, _horizontalOffset, _verticalOffset);

        if (hit.IsHeader)
        {
            return BeginColumnResize(point);
        }

        if (!hit.IsHit)
        {
            return false;
        }

        // A fixed-column click selects the row and leaves the current column alone, which is
        // TPersonGrid.HandleFixedClick assigning Row and never Col (EPR.QA.GUI.Grid.pas:151-155).
        // The caret therefore lands on the same variable one row down, and the hint follows it.
        CurrentRowIndex = hit.RowIndex;

        if (hit.Kind != MatrixGridCellKind.Fixed)
        {
            CurrentColumnIndex = hit.ColumnIndex;
        }

        ScrollIntoView(hit.RowIndex, hit.ColumnIndex);

        OnCellActivated(new MatrixGridCellEventArgs(hit.RowIndex, hit.ColumnIndex));

        return true;
    }

    /// <summary>Handles pointer movement at a point in the control's own coordinates.</summary>
    /// <param name="point">Where the pointer is.</param>
    internal void MoveTo(Point point)
    {
        if (_resizingColumn != NoIndex)
        {
            _layout.SetColumnWidth(_resizingColumn, _resizeOriginWidth + (point.X - _resizeOriginX));

            InvalidateMeasure();
            InvalidateVisual();
            ScrollOwner?.InvalidateScrollInfo();

            return;
        }

        UpdateResizeCursor(point);
        UpdateHover(_layout.HitTest(point, _horizontalOffset, _verticalOffset));
    }

    /// <summary>The cell the pointer is currently over.</summary>
    internal MatrixGridHit Hover => _hover;

    /// <summary>Whether a column-resize drag is in progress.</summary>
    internal bool IsResizingColumn => _resizingColumn != NoIndex;

    /// <summary>Ends a column-resize drag.</summary>
    internal void ReleasePointer() => EndColumnResize();

    /// <inheritdoc />
    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);

        _resizingColumn = NoIndex;
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        UpdateHover(MatrixGridHit.Miss);

        Cursor = null;
    }

    /// <inheritdoc />
    protected override void OnToolTipOpening(ToolTipEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        base.OnToolTipOpening(e);

        string? text = _hover.IsHit
            ? GetDisplayCellToolTip(_hover.RowIndex, _hover.DisplayColumnIndex)
            : null;

        if (text is null)
        {
            e.Handled = true;

            return;
        }

        EnsureToolTip().Content = text;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        base.OnKeyDown(e);

        if (e.Handled || Matrix is null || _layout.RowCount == 0)
        {
            return;
        }

        int rows = _layout.RowCount;
        int columns = _layout.DataColumnCount;
        int row = Math.Clamp(CurrentRowIndex == NoIndex ? 0 : CurrentRowIndex, 0, rows - 1);
        int column = columns == 0
            ? NoIndex
            : Math.Clamp(CurrentColumnIndex == NoIndex ? 0 : CurrentColumnIndex, 0, columns - 1);

        bool control = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        int page = Math.Max(1, (int)(ViewportHeight / _layout.RowHeight));

        switch (e.Key)
        {
            case Key.Up: row = Math.Max(0, row - 1); break;
            case Key.Down: row = Math.Min(rows - 1, row + 1); break;
            case Key.PageUp: row = Math.Max(0, row - page); break;
            case Key.PageDown: row = Math.Min(rows - 1, row + page); break;
            case Key.Left when column != NoIndex: column = Math.Max(0, column - 1); break;
            case Key.Right when column != NoIndex: column = Math.Min(columns - 1, column + 1); break;

            case Key.Home:
                column = columns == 0 ? NoIndex : 0;
                if (control)
                {
                    row = 0;
                }

                break;

            case Key.End:
                column = columns == 0 ? NoIndex : columns - 1;
                if (control)
                {
                    row = rows - 1;
                }

                break;

            default: return;
        }

        SetCurrentCell(row, column);

        e.Handled = true;
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

    private static void OnMatrixChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        MatrixGrid grid = (MatrixGrid)sender;

        // A different matrix means different variables, so a width dragged onto column 7 of the old
        // dataset must not silently apply to a different variable in the new one.
        grid._layout.ResetColumnWidths();
        grid.SyncCounts();
        grid.ClampCurrentCell();
        grid.SetHorizontalOffset(0);
        grid.SetVerticalOffset(0);
        grid.InvalidatePeer();
    }

    private static void OnStructureChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        MatrixGrid grid = (MatrixGrid)sender;

        grid.SyncFixedColumns();
        grid.SyncMetrics();
        grid.InvalidatePeer();
        grid.ScrollOwner?.InvalidateScrollInfo();
    }

    private static Color ColorOf(Brush brush, Color fallback) =>
        brush is SolidColorBrush solid ? solid.Color : fallback;

    private static double Clamp(double offset, double maximum) =>
        double.IsNaN(offset) ? 0 : Math.Max(0, Math.Min(offset, Math.Max(0, maximum)));

    private static string? NullIfEmpty(string? text) => string.IsNullOrEmpty(text) ? null : text;

    private static double WheelStep() => 3 * MatrixGridLayout.DefaultRowHeight;

    private void SyncFixedColumns() =>
        _layout.VisibleFixedOrdinals = FixedColumns.VisibleOrdinals(IdentificationColumns.For(Identification));

    private void SyncMetrics()
    {
        _layout.DataColumnWidth = DataColumnWidth;
        _layout.RowHeight = RowHeight;
        _layout.HeaderHeight = HeaderRowHeight;
    }

    private void SyncCounts()
    {
        PersonMatrix? matrix = Matrix;

        _layout.RowCount = matrix?.Rows.Count ?? 0;
        _layout.DataColumnCount = matrix?.Columns.Count ?? 0;
    }

    private void ClampCurrentCell()
    {
        if (CurrentRowIndex >= _layout.RowCount)
        {
            CurrentRowIndex = NoIndex;
        }

        if (CurrentColumnIndex >= _layout.DataColumnCount)
        {
            CurrentColumnIndex = NoIndex;
        }
    }

    private void InvalidatePeer() =>
        (UIElementAutomationPeer.FromElement(this) as MatrixGridAutomationPeer)?.InvalidateStructure();

    private void UpdateTypefaces()
    {
        FontFamily family = FontFamily;

        _regularTypeface = new Typeface(family, FontStyles.Normal, FontWeight, FontStretches.Normal);
        _emphasisTypeface = new Typeface(family, FontStyles.Normal, EmphasisFontWeight, FontStretches.Normal);
        _pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
    }

    private MatrixGridColors BuildColors() => new()
    {
        Default = Colors.White,
        MissingObject = ColorOf(MissingObjectBackground, Colors.White),
        Fixed = ColorOf(FixedCellBackground, Colors.White),
        CurrentCell = ColorOf(CurrentCellBackground, Colors.White),
        CurrentRow = ColorOf(CurrentRowBackground, Colors.White),
        CurrentRowTint = ColorOf(CurrentRowTint, Colors.White),
        Text = ColorOf(Foreground, Colors.Black),
        FixedText = ColorOf(FixedCellForeground, Colors.Black),
    };

    private Rect VisibleBandFor(int rowIndex, int displayColumnIndex)
    {
        bool frozenColumn = displayColumnIndex >= 0 && displayColumnIndex < _layout.FixedColumnCount;
        double x = frozenColumn ? 0 : _layout.FrozenWidth;
        double bandWidth = frozenColumn ? _layout.FrozenWidth : ViewportWidth;
        bool header = rowIndex == NoIndex;
        double y = header ? 0 : _layout.HeaderHeight;
        double bandHeight = header ? _layout.HeaderHeight : ViewportHeight;

        return new Rect(x, y, Math.Max(0, bandWidth), Math.Max(0, bandHeight));
    }

    private void DrawDataBand(
        DrawingContext dc,
        PersonMatrix matrix,
        in MatrixGridColors colors,
        MatrixGridRange rows,
        MatrixGridRange columns)
    {
        double bandWidth = ViewportWidth;
        double bandHeight = ViewportHeight;

        if (rows.IsEmpty || columns.IsEmpty || bandWidth <= 0 || bandHeight <= 0)
        {
            return;
        }

        double frozen = _layout.FrozenWidth;
        double header = _layout.HeaderHeight;

        dc.PushClip(new RectangleGeometry(new Rect(frozen, header, bandWidth, bandHeight)));

        int currentRow = CurrentRowIndex;
        int currentColumn = CurrentColumnIndex;
        int cells = 0;

        for (int row = rows.First; row < rows.End; row++)
        {
            bool rowIsCurrent = row == currentRow;
            double y = (header + (row * _layout.RowHeight)) - _verticalOffset;

            for (int column = columns.First; column < columns.End; column++)
            {
                int displayIndex = _layout.DisplayIndexOfData(column);
                double x = _layout.ColumnOffset(displayIndex) - _horizontalOffset;
                Rect bounds = new(x, y, _layout.ColumnWidth(displayIndex), _layout.RowHeight);
                MatrixCell cell = matrix.GetCell(row, column);

                cells++;

                DrawCell(
                    dc,
                    bounds,
                    cell.Text,
                    MatrixGridCellPainter.Resolve(
                        MatrixGridCellKind.Data,
                        cell,
                        NoIndex,
                        rowIsCurrent && column == currentColumn,
                        rowIsCurrent,
                        colors));
            }
        }

        _lastRender = _lastRender with { DataCells = cells };

        DrawDataGridLines(dc, rows, columns);

        dc.Pop();
    }

    private void DrawFrozenBand(
        DrawingContext dc,
        PersonMatrix matrix,
        in MatrixGridColors colors,
        MatrixGridRange rows)
    {
        double frozen = _layout.FrozenWidth;
        double bandHeight = ViewportHeight;

        if (rows.IsEmpty || frozen <= 0 || bandHeight <= 0)
        {
            return;
        }

        double header = _layout.HeaderHeight;

        dc.PushClip(new RectangleGeometry(new Rect(0, header, frozen, bandHeight)));

        int currentRow = CurrentRowIndex;
        int cells = 0;

        for (int row = rows.First; row < rows.End; row++)
        {
            bool rowIsCurrent = row == currentRow;
            double y = (header + (row * _layout.RowHeight)) - _verticalOffset;

            for (int displayIndex = 0; displayIndex < _layout.FixedColumnCount; displayIndex++)
            {
                int ordinal = _layout.FixedOrdinalAt(displayIndex);
                Rect bounds = new(
                    _layout.ColumnOffset(displayIndex),
                    y,
                    _layout.ColumnWidth(displayIndex),
                    _layout.RowHeight);

                MatrixCell cell = matrix.GetFixedCell(row, ordinal, EffectiveCulture);

                cells++;

                DrawCell(
                    dc,
                    bounds,
                    cell.Text,
                    MatrixGridCellPainter.Resolve(MatrixGridCellKind.Fixed, cell, ordinal, false, rowIsCurrent, colors));
            }

            DrawLine(dc, new Rect(0, (y + _layout.RowHeight) - GridLineThickness, frozen, GridLineThickness), GridLineBrush);
        }

        _lastRender = _lastRender with { FixedCells = cells };

        dc.Pop();
    }

    private void DrawColumnHeaders(
        DrawingContext dc,
        PersonMatrix matrix,
        in MatrixGridColors colors,
        MatrixGridRange columns)
    {
        double bandWidth = ViewportWidth;
        double header = _layout.HeaderHeight;

        if (columns.IsEmpty || bandWidth <= 0 || header <= 0)
        {
            return;
        }

        double frozen = _layout.FrozenWidth;

        dc.PushClip(new RectangleGeometry(new Rect(frozen, 0, bandWidth, header)));

        int cells = 0;

        for (int column = columns.First; column < columns.End; column++)
        {
            int displayIndex = _layout.DisplayIndexOfData(column);
            double x = _layout.ColumnOffset(displayIndex) - _horizontalOffset;
            double columnWidth = _layout.ColumnWidth(displayIndex);
            Rect bounds = new(x, 0, columnWidth, header);

            cells++;

            DrawCell(
                dc,
                bounds,
                matrix.Columns[column].Title,
                MatrixGridCellPainter.Resolve(MatrixGridCellKind.ColumnHeader, null, NoIndex, false, false, colors));

            DrawLine(
                dc,
                new Rect((x + columnWidth) - GridLineThickness, 0, GridLineThickness, header),
                GridLineBrush);
        }

        _lastRender = _lastRender with { HeaderCells = _lastRender.HeaderCells + cells };

        dc.Pop();
    }

    private void DrawFrozenHeaders(DrawingContext dc, in MatrixGridColors colors)
    {
        double frozen = _layout.FrozenWidth;
        double header = _layout.HeaderHeight;

        if (frozen <= 0 || header <= 0)
        {
            return;
        }

        dc.PushClip(new RectangleGeometry(new Rect(0, 0, frozen, header)));

        int cells = 0;

        for (int displayIndex = 0; displayIndex < _layout.FixedColumnCount; displayIndex++)
        {
            int ordinal = _layout.FixedOrdinalAt(displayIndex);
            Rect bounds = new(_layout.ColumnOffset(displayIndex), 0, _layout.ColumnWidth(displayIndex), header);

            cells++;

            DrawCell(
                dc,
                bounds,
                FixedColumns.Header(ordinal),
                MatrixGridCellPainter.Resolve(MatrixGridCellKind.FixedHeader, null, ordinal, false, false, colors));
        }

        _lastRender = _lastRender with { HeaderCells = _lastRender.HeaderCells + cells };

        dc.Pop();
    }

    private void DrawDataGridLines(DrawingContext dc, MatrixGridRange rows, MatrixGridRange columns)
    {
        Brush lines = GridLineBrush;
        double frozen = _layout.FrozenWidth;
        double header = _layout.HeaderHeight;
        double right = Math.Min(frozen + ViewportWidth, (frozen + _layout.DataWidth) - _horizontalOffset);

        for (int column = columns.First; column < columns.End; column++)
        {
            int displayIndex = _layout.DisplayIndexOfData(column);
            double edge = (_layout.ColumnOffset(displayIndex) + _layout.ColumnWidth(displayIndex)) - _horizontalOffset;

            DrawLine(
                dc,
                new Rect(
                    edge - GridLineThickness,
                    header,
                    GridLineThickness,
                    Math.Max(0, Math.Min(ViewportHeight, _layout.DataHeight - _verticalOffset))),
                lines);
        }

        for (int row = rows.First; row < rows.End; row++)
        {
            double bottom = (header + ((row + 1) * _layout.RowHeight)) - _verticalOffset;

            DrawLine(dc, new Rect(frozen, bottom - GridLineThickness, Math.Max(0, right - frozen), GridLineThickness), lines);
        }
    }

    private void DrawSeparators(DrawingContext dc, MatrixGridRange rows)
    {
        Brush separator = FixedLineBrush;
        double frozen = _layout.FrozenWidth;
        double header = _layout.HeaderHeight;
        double paintedRight = Math.Min(_viewport.Width, (frozen + _layout.DataWidth) - _horizontalOffset);
        double paintedBottom = Math.Min(_viewport.Height, (header + _layout.DataHeight) - _verticalOffset);

        // The header row's underline. Darker than the cell lines, because it separates the fixed
        // block from the data (§C.3).
        if (header > 0)
        {
            DrawLine(dc, new Rect(0, header - GridLineThickness, Math.Max(0, paintedRight), GridLineThickness), separator);
        }

        // The frozen block's right edge, running the height of the painted rows. There is no line
        // *inside* the block: the Delphi removed goFixedVertLine (EPR.QA.GUI.Grid.pas:122).
        if (frozen > 0 && !rows.IsEmpty)
        {
            DrawLine(
                dc,
                new Rect(frozen - GridLineThickness, 0, GridLineThickness, Math.Max(0, paintedBottom)),
                separator);
        }
    }

    private void DrawCell(DrawingContext dc, Rect bounds, string text, in MatrixGridCellPaint paint)
    {
        dc.DrawRectangle(_palette.Brush(paint.Background), null, bounds);

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        double available = bounds.Width - (2 * CellPaddingX);

        if (available <= 0)
        {
            return;
        }

        FormattedText formatted = new(
            text,
            EffectiveCulture,
            FlowDirection.LeftToRight,
            paint.Bold ? _emphasisTypeface : _regularTypeface,
            FontSize,
            _palette.Brush(paint.Foreground),
            _pixelsPerDip)
        {
            MaxTextWidth = available,
            MaxLineCount = 1,
            Trimming = TextTrimming.CharacterEllipsis,
            TextAlignment = paint.AlignLeft ? TextAlignment.Left : TextAlignment.Right,
        };

        double y = bounds.Y + Math.Max(CellPaddingY, (bounds.Height - formatted.Height) / 2);

        dc.DrawText(formatted, new Point(bounds.X + CellPaddingX, y));
    }

    private void DrawLine(DrawingContext dc, Rect bounds, Brush brush)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        dc.DrawRectangle(brush, null, bounds);
    }

    private bool BeginColumnResize(Point point)
    {
        int target = _layout.ColumnResizeTargetAt(point, _horizontalOffset);

        if (target == NoIndex)
        {
            return false;
        }

        _resizingColumn = target;
        _resizeOriginX = point.X;
        _resizeOriginWidth = _layout.ColumnWidth(target);

        // Returns false with no window - harmless, and the drag still tracks through OnMouseMove.
        _ = CaptureMouse();

        return true;
    }

    private void EndColumnResize()
    {
        if (_resizingColumn == NoIndex)
        {
            return;
        }

        _resizingColumn = NoIndex;

        ReleaseMouseCapture();
    }

    private void UpdateResizeCursor(Point point)
    {
        bool onGrip = point.Y < _layout.HeaderHeight
            && _layout.ColumnResizeTargetAt(point, _horizontalOffset) != NoIndex;

        Cursor = onGrip ? Cursors.SizeWE : null;
    }

    private void UpdateHover(MatrixGridHit hit)
    {
        if (hit == _hover)
        {
            return;
        }

        _hover = hit;

        // WPF will not re-open a tooltip while the pointer stays inside one element, so moving to a
        // new cell has to close the old one by hand. Without this the first cell's text follows the
        // pointer across the whole grid.
        if (_toolTip is not null)
        {
            _toolTip.IsOpen = false;
        }

        if (hit.IsHit)
        {
            _ = EnsureToolTip();
        }
    }

    private ToolTip EnsureToolTip()
    {
        if (_toolTip is not null)
        {
            return _toolTip;
        }

        // Created lazily: a test that only exercises arithmetic must not have to construct a
        // templated Control, and Application.Current is null under test.
        _toolTip = new ToolTip();
        ToolTip = _toolTip;

        return _toolTip;
    }
}
