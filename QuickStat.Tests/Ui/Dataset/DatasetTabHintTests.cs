using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QuickStat.Controls.Dataset;
using QuickStat.Tests.Ui.Dialogs;
using QuickStat.ViewModels;
using QuickStat.Views;
using Xunit;

namespace QuickStat.Tests.Ui.Dataset;

/// <summary>
/// <c>Show data hint</c> against the real view: the seam between the check box and the caret.
/// </summary>
/// <remarks>
/// <para>
/// <b>Written after the bug, and it had to be written here.</b> Ticking the box did nothing until
/// the user clicked a different cell. Every view-model case passed throughout, and would have gone
/// on passing: the defect was not in what <see cref="DatasetViewModel.UpdateHint"/> computes but in
/// nobody calling it. <c>MainQuickStat.pas:310</c> assigns <c>UpdateDataHintPanel</c> itself as
/// <c>cbShowDataHint.OnClick</c>, so the check box runs the whole procedure - hide, then rebuild
/// from <c>fGrid.Col</c> and <c>fGrid.Row</c> - and the port had only the hiding half.
/// <c>05-ui-spec.md</c> §G.2 states it in its first bullet; that is the bullet the implementation
/// missed, which is why this file drives the check box rather than the property behind it.
/// </para>
/// <para>
/// These cases need the shipped theme, so they take <see cref="WpfApplicationFixture"/> rather than
/// a throwaway apartment, and they realise the view: the check box's <c>IsChecked</c> binding is
/// unattached until then (see <see cref="RealisedWindow"/>), so clicking it off-tree would move
/// nothing.
/// </para>
/// </remarks>
[Collection(WpfApplicationCollection.Name)]
public class DatasetTabHintTests
{
    private readonly WpfApplicationFixture _wpf;

    /// <summary>Takes the assembly's one application; the view names theme keys.</summary>
    /// <param name="wpf">Injected by xUnit from <see cref="WpfApplicationCollection"/>.</param>
    public DatasetTabHintTests(WpfApplicationFixture wpf)
    {
        ArgumentNullException.ThrowIfNull(wpf);

        _wpf = wpf;
    }

    [Fact]
    public void TickingTheBoxShowsTheHintForTheCellThatIsAlreadySelected()
    {
        (bool AfterClick, bool WhenOff, bool WhenOnAgain, bool SameCell) seen = _wpf.Run(() =>
        {
            using DatasetHarness harness = new();

            harness.LoadAndCollect();

            DatasetTabView view = new() { DataContext = harness.ViewModel };
            (bool AfterClick, bool WhenOff, bool WhenOnAgain, bool SameCell) state = default;

            RealisedWindow.RunControl(view, _ =>
            {
                CheckBox box = ShowDataHintBox(view);

                ClickCell(view.Grid, row: 0, column: 0);

                Point anchor = harness.ViewModel.Hint?.Anchor ?? default;

                state.AfterClick = harness.ViewModel.Hint is not null;

                // The user's gesture, through the two-way binding, not the property behind it.
                box.IsChecked = false;
                state.WhenOff = harness.ViewModel.Hint is null;

                box.IsChecked = true;
                state.WhenOnAgain = harness.ViewModel.Hint is not null;
                state.SameCell = harness.ViewModel.Hint?.Anchor == anchor;
            });

            return state;
        });

        Assert.True(seen.AfterClick, "clicking a cell should show the hint");
        Assert.True(seen.WhenOff, "unticking the box should hide it");
        Assert.True(seen.WhenOnAgain, "ticking it again should bring it back without a second click");
        Assert.True(seen.SameCell, "it should come back on the cell the caret never left");
    }

    [Fact]
    public void TheRestoredHintSaysWhatItSaidBefore()
    {
        // Rebuilt, not remembered: nothing is cached across the toggle, so this also pins that the
        // rebuild reads the same row and column rather than, say, the grid's first data cell.
        (string? Before, string? After) lines = _wpf.Run(() =>
        {
            using DatasetHarness harness = new();

            harness.LoadAndCollect(personId: 52, varName: "B-Hemo", value: 10);

            DatasetTabView view = new() { DataContext = harness.ViewModel };
            (string? Before, string? After) said = default;

            RealisedWindow.RunControl(view, _ =>
            {
                ClickCell(view.Grid, row: 0, column: 0);

                said.Before = harness.ViewModel.Hint?.Line1;

                harness.ViewModel.ShowDataHint = false;
                harness.ViewModel.ShowDataHint = true;

                said.After = harness.ViewModel.Hint?.Line1;
            });

            return said;
        });

        Assert.Equal("PersonId = 52", lines.Before);
        Assert.Equal(lines.Before, lines.After);
    }

    [Fact]
    public void TickingTheBoxWithNoCaretShowsNothing()
    {
        // Deliberate, and the one place the port and the Delphi differ here: a TCustomGrid always
        // has a current cell, so the Delphi would pop a hint on the first data cell of a grid the
        // user had never touched.  MatrixGrid starts at NoIndex and grows a caret on the first click
        // or arrow key, which is a property of the caret rather than of the hint - so the hint
        // simply has nothing to describe, and says nothing.
        bool shown = _wpf.Run(() =>
        {
            using DatasetHarness harness = new();

            harness.LoadAndCollect();

            DatasetTabView view = new() { DataContext = harness.ViewModel };
            bool any = false;

            RealisedWindow.RunControl(view, _ =>
            {
                Assert.Equal(MatrixGrid.NoIndex, view.Grid.CurrentRowIndex);

                harness.ViewModel.ShowDataHint = false;
                harness.ViewModel.ShowDataHint = true;

                any = harness.ViewModel.Hint is not null;
            });

            return any;
        });

        Assert.False(shown);
    }

    [Fact]
    public void ADetachedViewIsNotAskedForAnything()
    {
        // The subscription is one line in OnDataContextChanged and one in Detach, and a missing
        // second line is a leak that only shows up as a resurrected hint on a closed tab.
        bool asked = _wpf.Run(() =>
        {
            using DatasetHarness harness = new();

            harness.LoadAndCollect();

            DatasetTabView view = new() { DataContext = harness.ViewModel };
            bool reached = false;

            RealisedWindow.RunControl(view, _ => ClickCell(view.Grid, row: 0, column: 0));

            // The window has closed, so Unloaded has run.  Anything reaching the view now is
            // reaching a view whose grid is no longer in a tree.
            harness.ViewModel.HintRefreshRequested += (_, _) => reached = true;
            harness.ViewModel.ShowDataHint = false;

            return reached && harness.ViewModel.Hint is not null;
        });

        Assert.False(asked);
    }

    /// <summary>Presses the centre of a cell, the way a user would.</summary>
    /// <param name="grid">The realised grid.</param>
    /// <param name="row">Index into the matrix's rows.</param>
    /// <param name="column">Index into the matrix's columns.</param>
    /// <remarks>
    /// Through <c>PressAt</c> rather than a synthesised <c>MouseButtonEventArgs</c>: WPF resolves a
    /// mouse event's position from the real cursor, so there is no other way to click a chosen cell.
    /// The rectangle comes from the control so the arithmetic cannot drift from the layout.
    /// </remarks>
    private static void ClickCell(MatrixGrid grid, int row, int column)
    {
        Assert.True(grid.TryGetCellBounds(row, column, out Rect cell), "the cell should be laid out");
        Assert.True(grid.PressAt(new Point(cell.Left + (cell.Width / 2), cell.Top + (cell.Height / 2))));
    }

    /// <summary>The <c>Show data hint</c> check box, found by its caption.</summary>
    /// <param name="view">The realised tab.</param>
    /// <returns>The check box.</returns>
    /// <remarks>
    /// It carries no <c>x:Name</c>, and giving it one for a test would be the test changing the
    /// product. The caption is the thing a user identifies it by, so that is what this matches -
    /// and a rename that broke this would be a visible change to the window either way.
    /// </remarks>
    private static CheckBox ShowDataHintBox(DependencyObject view) =>
        Assert.Single(Descendants<CheckBox>(view), box => Equals(box.Content, "Show data hint"));

    private static IEnumerable<T> Descendants<T>(DependencyObject root)
        where T : DependencyObject
    {
        int children = VisualTreeHelper.GetChildrenCount(root);

        for (int index = 0; index < children; index++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, index);

            if (child is T match)
            {
                yield return match;
            }

            foreach (T deeper in Descendants<T>(child))
            {
                yield return deeper;
            }
        }
    }
}
