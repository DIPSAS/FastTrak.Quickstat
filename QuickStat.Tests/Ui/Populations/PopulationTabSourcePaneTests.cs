using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QuickStat.Tests.Ui.Dialogs;
using QuickStat.ViewModels;
using QuickStat.Views;
using Xunit;

namespace QuickStat.Tests.Ui.Populations;

/// <summary>
/// <c>Show source</c>: the check box under the tip, and the <c>CREATE PROCEDURE</c> pane it opens.
/// </summary>
/// <remarks>
/// <para>
/// The two are three DataContexts apart - the box is on <c>PopulationTabView</c> and binds
/// <c>Picker.ShowSourceCode</c>, the pane is inside <c>PopulationPickerView</c>, whose DataContext
/// <em>is</em> the picker, and binds <c>ShowSourceCode</c>. Both spellings are right and either one
/// alone is silently wrong, which is not something a view-model test can see and not something
/// asserting the markup as XML can see either: a binding that resolves to nothing raises no error,
/// it just never moves the target.
/// </para>
/// <para>
/// So this realises the tab and toggles the box. <c>SqlPreview</c> is set directly rather than by
/// loading a catalogue: the picker's load path is asynchronous and awaiting it inside a dispatcher
/// callback would deadlock, and what is under test here is the wiring, not the query.
/// <c>PopulationPickerViewModelTests</c> owns the rest.
/// </para>
/// </remarks>
[Collection(WpfApplicationCollection.Name)]
public class PopulationTabSourcePaneTests
{
    private readonly WpfApplicationFixture _wpf;

    /// <summary>Takes the assembly's one application; the view names theme keys.</summary>
    /// <param name="wpf">Injected by xUnit from <see cref="WpfApplicationCollection"/>.</param>
    public PopulationTabSourcePaneTests(WpfApplicationFixture wpf)
    {
        ArgumentNullException.ThrowIfNull(wpf);

        _wpf = wpf;
    }

    [Fact]
    public void TickingShowSourceOpensThePaneAndUntickingClosesIt()
    {
        (Visibility Before, Visibility After, Visibility Again, string? Text) pane = _wpf.Run(() =>
        {
            using PopulationHarness harness = new();

            harness.Picker.SqlPreview = "CREATE PROCEDURE dbo.GetCaseListMyRelations";

            PopulationTabView tab = new() { DataContext = harness.Tab };
            (Visibility Before, Visibility After, Visibility Again, string? Text) seen = default;

            RealisedWindow.RunControl(tab, _ =>
            {
                CheckBox box = ShowSourceBox(tab);
                TextBox preview = Preview(tab);

                seen.Before = preview.Visibility;

                box.IsChecked = true;
                tab.UpdateLayout();

                seen.After = preview.Visibility;
                seen.Text = preview.Text;

                box.IsChecked = false;
                tab.UpdateLayout();

                seen.Again = preview.Visibility;
            });

            return seen;
        });

        Assert.Equal(Visibility.Collapsed, pane.Before);
        Assert.Equal(Visibility.Visible, pane.After);
        Assert.Equal("CREATE PROCEDURE dbo.GetCaseListMyRelations", pane.Text);
        Assert.Equal(Visibility.Collapsed, pane.Again);
    }

    [Fact]
    public void TheBoxIsOffWhenTheTabOpens()
    {
        // The pane costs the list its height, and the Delphi shows it only to a user who holds the
        // right - so nobody should meet it without asking.  It is also what makes the toggle worth
        // having: a pane that is always there needs no switch.
        bool ticked = _wpf.Run(() =>
        {
            using PopulationHarness harness = new();

            PopulationTabView tab = new() { DataContext = harness.Tab };
            bool on = true;

            RealisedWindow.RunControl(tab, _ => on = ShowSourceBox(tab).IsChecked ?? true);

            return on;
        });

        Assert.False(ticked);
    }

    /// <summary>The <c>Show source</c> check box, found by its caption.</summary>
    /// <param name="tab">The realised tab.</param>
    /// <returns>The check box.</returns>
    private static CheckBox ShowSourceBox(DependencyObject tab) =>
        Assert.Single(
            Descendants<CheckBox>(tab),
            box => Equals(box.Content, PopulationPickerViewModel.ShowSourceCaption));

    /// <summary>The read-only SQL pane, which is the tab's only <c>TextBox</c> bar the filter.</summary>
    /// <param name="tab">The realised tab.</param>
    /// <returns>The pane.</returns>
    private static TextBox Preview(DependencyObject tab) =>
        Assert.Single(Descendants<TextBox>(tab), box => box.IsReadOnly);

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
