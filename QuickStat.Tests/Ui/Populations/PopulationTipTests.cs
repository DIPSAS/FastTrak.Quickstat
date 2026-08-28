using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QuickStat.Tests.Ui.Dialogs;
using QuickStat.ViewModels;
using QuickStat.Views;
using Xunit;

namespace QuickStat.Tests.Ui.Populations;

/// <summary>
/// The double-click hint: the line above the list, and the tool tip on every row.
/// </summary>
/// <remarks>
/// <para>
/// <b>A relocation, so the test is about where it is.</b> The Delphi's <c>lblHintPopulation</c> is
/// <c>Align = alBottom</c> on the tab (<c>05-ui-spec.md</c> §B.1 item 5), which puts it below the
/// frame, below the source pane and - once the port added one - below a check box as well, a long
/// way from the list it instructs. Asserting the text alone would pass with the label back at the
/// foot, so the case measures the two rectangles instead.
/// </para>
/// <para>
/// The rows are given directly to <c>PopulationList.ItemsSource</c> rather than through a connected
/// picker: the catalogue load is asynchronous and awaiting it from a dispatcher callback deadlocks
/// (see <see cref="PopulationTabSourcePaneTests"/>). What is under test is the container style, and
/// a container needs only items.
/// </para>
/// </remarks>
[Collection(WpfApplicationCollection.Name)]
public class PopulationTipTests
{
    private readonly WpfApplicationFixture _wpf;

    /// <summary>Takes the assembly's one application; the views name theme keys.</summary>
    /// <param name="wpf">Injected by xUnit from <see cref="WpfApplicationCollection"/>.</param>
    public PopulationTipTests(WpfApplicationFixture wpf)
    {
        ArgumentNullException.ThrowIfNull(wpf);

        _wpf = wpf;
    }

    [Fact]
    public void TheTipSitsDirectlyAboveTheListAndTheSourceBoxStaysAtTheFoot()
    {
        (double Gap, bool BoxIsBelowTheList) laid = _wpf.Run(() =>
        {
            using PopulationHarness harness = new();

            PopulationTabView tab = new() { DataContext = harness.Tab };
            (double Gap, bool BoxIsBelowTheList) seen = default;

            RealisedWindow.RunControl(tab, _ =>
            {
                Rect tip = Bounds(Tip(tab), tab);
                Rect list = Bounds(List(tab), tab);
                Rect box = Bounds(SourceBox(tab), tab);

                seen.Gap = list.Top - tip.Bottom;
                seen.BoxIsBelowTheList = box.Top >= list.Bottom;
            });

            return seen;
        });

        // Non-negative is "above"; the bound is "directly".  The markup asks for a 3-unit bottom
        // margin, and anything that put a control back between the two would blow past this.
        Assert.InRange(laid.Gap, 0, 6);

        Assert.True(laid.BoxIsBelowTheList, "'Show source' should still be at the foot of the tab.");
    }

    [Fact]
    public void EveryRowSaysTheSameThingUnderThePointer()
    {
        List<object?> tips = _wpf.Run(() =>
        {
            PopulationPickerView view = new();

            view.PopulationList.ItemsSource = new[]
            {
                new PopulationViewModel(PopulationTestDoubles.NewPopulation(282, "Diagnoseår mangler")),
                new PopulationViewModel(PopulationTestDoubles.NewPopulation(14, "Alle testpersoner")),
            };

            List<object?> found = [];

            RealisedWindow.RunControl(view, _ =>
            {
                for (int index = 0; index < view.PopulationList.Items.Count; index++)
                {
                    ListBoxItem row =
                        (ListBoxItem)view.PopulationList.ItemContainerGenerator.ContainerFromIndex(index)!;

                    found.Add(row.ToolTip);
                }
            });

            return found;
        });

        Assert.Equal(
            [PopulationPickerViewModel.RowToolTip, PopulationPickerViewModel.RowToolTip],
            tips);
    }

    [Fact]
    public void TheEmptySpaceBelowTheRowsHasNoToolTipOfItsOwn()
    {
        // ToolTipService walks UP from whatever is under the pointer, so a tip on the ListBox would
        // also answer for the blank area under the last row - where "this population" is a lie.
        object? onTheList = _wpf.Run(() =>
        {
            PopulationPickerView view = new();
            object? found = "not read";

            RealisedWindow.RunControl(view, _ => found = view.PopulationList.ToolTip);

            return found;
        });

        Assert.Null(onTheList);
    }

    /// <summary>The hint line, found by its text.</summary>
    /// <param name="tab">The realised tab.</param>
    /// <returns>The text block.</returns>
    private static TextBlock Tip(DependencyObject tab) =>
        Assert.Single(
            Descendants<TextBlock>(tab),
            block => block.Text == PopulationPickerViewModel.TipText);

    private static ListBox List(DependencyObject tab) => Assert.Single(Descendants<ListBox>(tab));

    private static CheckBox SourceBox(DependencyObject tab) =>
        Assert.Single(
            Descendants<CheckBox>(tab),
            box => Equals(box.Content, PopulationPickerViewModel.ShowSourceCaption));

    /// <summary>Where an element ends up, in the coordinates of the tab around it.</summary>
    /// <param name="element">The laid-out element.</param>
    /// <param name="root">An ancestor of it.</param>
    /// <returns>The rectangle, margins excluded.</returns>
    private static Rect Bounds(FrameworkElement element, Visual root) =>
        element.TransformToAncestor(root).TransformBounds(new Rect(element.RenderSize));

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
