using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using QuickStat.Controls.Dataset;
using QuickStat.Domain.Matrix;
using QuickStat.Tests.Configuration;
using QuickStat.Tests.Ui.Controls;
using QuickStat.Tests.Ui.Dialogs;
using QuickStat.Views;
using Xunit;

namespace QuickStat.Tests.Ui.Dataset;

/// <summary>
/// The Dataset tab gives its grid the host the control requires: two scrollbars and a
/// <c>ScrollOwner</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Written after the bug.</b> <see cref="MatrixGrid"/> implements <c>IScrollInfo</c> in full and
/// its own class documentation says to put it inside a <c>ScrollViewer</c> with
/// <c>CanContentScroll="True"</c> - and <c>DatasetTabView.xaml</c> did not. Nothing calls
/// <c>LineDown</c>, <c>PageDown</c> or <c>SetVerticalOffset</c> except a <c>ScrollViewer</c>: they
/// are interface members, not input handlers. So there were no scrollbars on either axis, a dataset
/// taller or wider than the pane could be reached only with the arrow keys, and every
/// <c>ScrollOwner?.InvalidateScrollInfo()</c> in the control was a null-check that never fired.
/// </para>
/// <para>
/// <c>Ui/Controls/MatrixGridScrollInfoTests.cs</c> covered every one of those methods and passed
/// throughout. It calls them itself. A test that calls an API cannot notice that nothing else does;
/// this file asks the realised view instead.
/// </para>
/// <para>
/// <b>The wheel is not here</b>, and that is the correction rather than an omission: the first fix
/// assumed hosting the grid was also what made the wheel work, and it is not.
/// <c>TCustomGrid.DoMouseWheelDown</c> moves the caret one row a notch and scrolls only to follow
/// it, so <see cref="MatrixGrid.OnMouseWheel"/> handles the gesture and marks it handled before the
/// host sees it - <c>Ui/Controls/MatrixGridWheelTests.cs</c>.
/// <see cref="TheHostDoesNotScrollWhenTheWheelMovesTheCaret"/> is the seam between the two files.
/// </para>
/// </remarks>
[Collection(WpfApplicationCollection.Name)]
public class DatasetGridScrollHostTests
{
    private readonly WpfApplicationFixture _wpf;

    /// <summary>Takes the assembly's one application; the view names theme keys.</summary>
    /// <param name="wpf">Injected by xUnit from <see cref="WpfApplicationCollection"/>.</param>
    public DatasetGridScrollHostTests(WpfApplicationFixture wpf)
    {
        ArgumentNullException.ThrowIfNull(wpf);

        _wpf = wpf;
    }

    [Fact]
    public void AGridWithNoHostHasNoScrollBarsAtAll()
    {
        // The defect itself, kept rather than described: a fully working IScrollInfo, realised, with
        // a dataset far bigger than the window, and not one scrollbar anywhere in the tree.  The
        // control paints its own viewport and never grows, so nothing above it can infer that there
        // is more - which is why this was invisible until somebody tried to reach row 40.
        int bars = _wpf.Run(() =>
        {
            MatrixGrid grid = new() { Matrix = Rows(200, columns: 60) };
            int found = 0;

            RealisedWindow.RunControl(grid, _ => found = Count<ScrollBar>(grid));

            return found;
        });

        Assert.Equal(0, bars);
    }

    [Fact]
    public void TheHostDoesNotScrollWhenTheWheelMovesTheCaret()
    {
        // Both halves of the correction in one case.  The wheel moves the caret - one row, from
        // nothing to row 0 on the first notch - and the ScrollViewer wrapped around the grid does
        // NOT also scroll it three rows, because MatrixGrid.OnMouseWheel marks the event handled.
        // Without that this would read 51, and the grid would jump a screenful for every notch.
        (int row, double offset) = _wpf.Run(() =>
        {
            DatasetTabView view = Tab(Rows(200));
            (int Row, double Offset) after = default;

            RealisedWindow.RunControl(view, _ =>
            {
                WheelOver(view.Grid, notches: -1);

                after = (view.Grid.CurrentRowIndex, view.Grid.VerticalOffset);
            });

            return after;
        });

        Assert.Equal(0, row);
        Assert.Equal(0, offset);
    }

    [Fact]
    public void TheGridFoundItsScrollOwner()
    {
        // Not decoration: ScrollOwner is how the grid tells the bars that the extent changed, so
        // every InvalidateScrollInfo call in the control is dead until this is non-null - a collect
        // run could add three hundred columns and the horizontal bar would not grow.
        bool owned = _wpf.Run(() =>
        {
            DatasetTabView view = Tab(Rows(200));
            bool found = false;

            RealisedWindow.RunControl(view, _ => found = view.Grid.ScrollOwner is not null);

            return found;
        });

        Assert.True(owned, "The grid is not inside a ScrollViewer with CanContentScroll=True.");
    }

    [Fact]
    public void BothBarsAppearWhenTheDatasetOverflows()
    {
        (Visibility vertical, Visibility horizontal) = _wpf.Run(() => Bars(Rows(200, columns: 60)));

        Assert.Equal(Visibility.Visible, vertical);
        Assert.Equal(Visibility.Visible, horizontal);
    }

    [Fact]
    public void NeitherBarAppearsWhenTheDatasetFits()
    {
        // Auto, not Visible: the Delphi grid is ssBoth, and TCustomGrid shows a bar only while
        // there is something behind it.  A pair of permanently greyed-out bars would be a visible
        // difference on the small datasets most of the parity pass uses.
        (Visibility vertical, Visibility horizontal) = _wpf.Run(() => Bars(Rows(3, columns: 2)));

        Assert.Equal(Visibility.Collapsed, vertical);
        Assert.Equal(Visibility.Collapsed, horizontal);
    }

    [Fact]
    public void DraggingTheBarMovesTheGridToo()
    {
        // The wheel and the bar are two different paths into IScrollInfo - MouseWheelDown against
        // SetVerticalOffset - and the bar is the one a long dataset is actually navigated with.
        double offset = _wpf.Run(() =>
        {
            DatasetTabView view = Tab(Rows(200));
            double moved = 0;

            RealisedWindow.RunControl(view, _ =>
            {
                view.Grid.ScrollOwner!.ScrollToVerticalOffset(500);
                view.UpdateLayout();

                moved = view.Grid.VerticalOffset;
            });

            return moved;
        });

        Assert.Equal(500, offset);
    }

    [Fact]
    public void TheHintPanelStillSharesTheGridsOrigin()
    {
        // TryGetCellBounds answers in the grid's own coordinates and DatasetTabView hands those
        // straight to Canvas.Left/Top on a Canvas that is a sibling of the ScrollViewer, not a
        // child of it.  That only lines up while the grid is arranged at the Canvas's origin, which
        // wrapping it in anything could have broken by a scrollbar's width.
        Point origin = _wpf.Run(() =>
        {
            DatasetTabView view = Tab(Rows(200, columns: 60));
            Point corner = default;

            RealisedWindow.RunControl(view, _ =>
            {
                // The Canvas the hint's Canvas.Left/Top are measured against, taken from the hint
                // rather than searched for: a ScrollBar template has visuals of its own, and a test
                // about coordinates should not be able to pick up the wrong element's.
                Visual canvas = (Visual)VisualTreeHelper.GetParent(view.HintPanel);

                corner = view.Grid.TransformToVisual(canvas).Transform(new Point(0, 0));
            });

            return corner;
        });

        Assert.Equal(new Point(0, 0), origin);
    }

    [Fact]
    public void TheMarkupStillHostsTheGridTheWayTheControlRequires()
    {
        // The control's contract is a comment on a class in another folder, and the view that has to
        // honour it is the one file nothing else checks.  Structural, so it fails on the edit rather
        // than on somebody scrolling.
        XNamespace wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace dataset = "clr-namespace:QuickStat.Controls.Dataset";

        XDocument markup = XDocument.Load(
            Path.Combine(RepositoryFiles.Root, "QuickStat.App", "Views", "DatasetTabView.xaml"));

        XElement grid = Assert.Single(markup.Descendants(dataset + "MatrixGrid"));
        XElement scroller = Assert.Single(markup.Descendants(wpf + "ScrollViewer"));

        Assert.Same(scroller, grid.Parent);
        Assert.Equal("True", (string?)scroller.Attribute("CanContentScroll"));

        // Both bars on demand, and the hint left outside so it cannot scroll away from its cell.
        Assert.Equal("Auto", (string?)scroller.Attribute("VerticalScrollBarVisibility"));
        Assert.Equal("Auto", (string?)scroller.Attribute("HorizontalScrollBarVisibility"));
        Assert.Empty(scroller.Descendants(wpf + "Canvas"));
    }

    /// <summary>A dataset tab holding <paramref name="matrix"/>, with no view-model behind it.</summary>
    /// <param name="matrix">What the grid should show.</param>
    /// <returns>The view.</returns>
    /// <remarks>
    /// Assigning <see cref="MatrixGrid.Matrix"/> drops the one-way binding the XAML declares, which
    /// is what is wanted here: these cases are about the host around the grid, and a real
    /// <c>DatasetViewModel</c> would bring a database, an exporter and a clock with it.
    /// <c>ViewInstantiationTests</c> is where the bindings themselves are proved.
    /// </remarks>
    private static DatasetTabView Tab(PersonMatrix matrix)
    {
        DatasetTabView view = new();

        view.Grid.Matrix = matrix;
        view.Grid.CellCulture = MatrixGridTestData.Culture;

        return view;
    }

    private static PersonMatrix Rows(int rows, int columns = 8) =>
        MatrixGridTestData.LargeMatrix(rows, columns);

    private static (Visibility Vertical, Visibility Horizontal) Bars(PersonMatrix matrix)
    {
        DatasetTabView view = Tab(matrix);
        (Visibility Vertical, Visibility Horizontal) computed = default;

        RealisedWindow.RunControl(view, _ =>
        {
            ScrollViewer scroller = view.Grid.ScrollOwner!;

            computed = (
                scroller.ComputedVerticalScrollBarVisibility,
                scroller.ComputedHorizontalScrollBarVisibility);
        });

        return computed;
    }

    /// <summary>Raises real wheel events on the grid and lets them route.</summary>
    /// <param name="grid">Where the pointer is.</param>
    /// <param name="notches">Negative is downwards, as a wheel is.</param>
    /// <remarks>
    /// Not <c>ScrollInfo.MouseWheelDown()</c>, which is the shortcut the defect hid behind. The
    /// event bubbles out of the grid to whatever is above it, so this measures the whole path -
    /// including, in <see cref="TheHostDoesNotScrollWhenTheWheelMovesTheCaret"/>, that it stops
    /// where it should.
    /// </remarks>
    private static void WheelOver(MatrixGrid grid, int notches)
    {
        grid.RaiseEvent(new MouseWheelEventArgs(
            Mouse.PrimaryDevice,
            0,
            notches * Mouse.MouseWheelDeltaForOneLine)
        {
            RoutedEvent = Mouse.MouseWheelEvent,
            Source = grid,
        });

        grid.UpdateLayout();
    }

    private static int Count<T>(DependencyObject root)
        where T : DependencyObject
    {
        int found = root is T ? 1 : 0;

        for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            found += Count<T>(VisualTreeHelper.GetChild(root, index));
        }

        return found;
    }
}
