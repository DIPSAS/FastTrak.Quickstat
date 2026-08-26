using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Domain.Anonymisation;
using QuickStat.Services;
using QuickStat.Tests.Ui.Services;
using QuickStat.ViewModels;
using QuickStat.Views;
using Xunit;

namespace QuickStat.Tests.Ui.Collections;

/// <summary>
/// The view's one job: <c>05-ui-spec.md</c> §G.4's scroll bookkeeping around a collect run.
/// </summary>
/// <remarks>
/// <para>
/// A user with 131 data elements scrolled to the bottom of the list notices immediately when a run
/// dumps them back at the top, which is why the Delphi saves <c>TopIndex</c> before the loop and
/// restores it afterwards (<c>MainQuickStat.pas:651-676</c>).
/// </para>
/// <para>
/// These cases drive <see cref="CollectionsTabView.ScrollKeeper"/> over a bare
/// <see cref="ListBox"/>, not over <see cref="CollectionsTabView"/> itself.
/// <b>Constructing that view under test is not possible today</b>: a <c>StaticResource</c> is
/// resolved while the XAML is being parsed, so the theme would have to be reachable from
/// <see cref="Application.Current"/> - and there is none under test, deliberately, because WPF
/// allows one per <c>AppDomain</c> and creating one on a short-lived STA thread would leave every
/// later test marshalling to a dead dispatcher. The keeper is a separate type for exactly this
/// reason; what it is attached to is two lines of the view's constructor.
/// </para>
/// <para>
/// They still need <see cref="StaTestRunner"/>: the test thread is MTA and
/// <see cref="UIElement.Measure"/> throws there.
/// </para>
/// </remarks>
public class CollectionsTabViewTests
{
    [Fact]
    public void TheScrollViewerIsFoundInsideTheCheckList()
    {
        StaTestRunner.Run(() =>
        {
            Composed composed = Compose(elements: 60);

            Assert.NotNull(composed.Keeper.ScrollHost);
            Assert.Same(composed.Keeper.ScrollHost, CollectionsTabView.FindScrollViewer(composed.List));
        });
    }

    [Fact]
    public void ThereIsNoScrollViewerBeforeTheTemplateHasBeenApplied()
    {
        StaTestRunner.Run(() => Assert.Null(CollectionsTabView.FindScrollViewer(new ListBox())));
        StaTestRunner.Run(() => Assert.Null(CollectionsTabView.FindScrollViewer(null)));
    }

    [Fact]
    public void TheListIsPutBackWhereTheUserLeftIt()
    {
        StaTestRunner.RunWithDispatcher(async () =>
        {
            Composed composed = Compose(elements: 60);
            ScrollViewer scroll = composed.Keeper.ScrollHost!;

            // A vacuous test otherwise: with nothing to scroll there is nothing to restore.
            Assert.True(scroll.ScrollableHeight > 0);

            double before = ScrollTo(composed, 12);

            Assert.Equal(12, before);

            composed.ViewModel.DataElements[^1].IsChecked = true;

            double duringRun = before;

            composed.Runner.Observe = _ =>
            {
                composed.List.UpdateLayout();

                duringRun = scroll.VerticalOffset;
            };

            await composed.ViewModel.CollectDataCommand.ExecuteAsync(null);

            composed.List.UpdateLayout();

            // The run really does move the list - ScrollIntoView - which is the hazard §G.4 exists
            // for, and the keeper undoes it.
            Assert.NotEqual(before, duringRun);
            Assert.Equal(before, scroll.VerticalOffset);
        });
    }

    [Fact]
    public void AFailedRunStillScrollsBack()
    {
        // The restore is in a finally.  The Delphi's TopIndex := savedTopIndex sits inside the try
        // and is skipped when a collector raises, so this is strictly kinder than the original.
        StaTestRunner.RunWithDispatcher(async () =>
        {
            Composed composed = Compose(elements: 60);
            ScrollViewer scroll = composed.Keeper.ScrollHost!;

            double before = ScrollTo(composed, 12);

            composed.ViewModel.DataElements[^1].IsChecked = true;

            composed.Runner.ThrowFor = composed.ViewModel.DataElements[^1].Name;
            composed.Runner.Observe = _ => composed.List.UpdateLayout();

            await composed.ViewModel.CollectDataCommand.ExecuteAsync(null);

            composed.List.UpdateLayout();

            Assert.Equal(before, scroll.VerticalOffset);
        });
    }

    [Fact]
    public void TheRunDoesNotMoveTheSelection()
    {
        // §G.4 is explicit: keep the highlight, drop the ItemIndex move.  Screenshot 2 shows the
        // user's own selection still sitting on "^ Kjønn" after a run.
        StaTestRunner.RunWithDispatcher(async () =>
        {
            Composed composed = Compose(elements: 20);

            composed.List.SelectedIndex = 3;
            composed.ViewModel.DataElements[^1].IsChecked = true;

            await composed.ViewModel.CollectDataCommand.ExecuteAsync(null);

            Assert.Equal(3, composed.List.SelectedIndex);
            Assert.Null(composed.ViewModel.CurrentlyCollecting);
        });
    }

    [Fact]
    public void DetachingStopsTheKeeperListening()
    {
        StaTestRunner.RunWithDispatcher(async () =>
        {
            Composed composed = Compose(elements: 60);
            ScrollViewer scroll = composed.Keeper.ScrollHost!;

            double before = ScrollTo(composed, 12);

            composed.ViewModel.DataElements[^1].IsChecked = true;
            composed.Keeper.Attach(null);

            await composed.ViewModel.CollectDataCommand.ExecuteAsync(null);

            composed.List.UpdateLayout();

            // A detached keeper neither scrolls the collecting element into view nor scrolls back,
            // so the list has not moved at all - which is what proves both handlers are gone.
            Assert.Equal(before, scroll.VerticalOffset);
        });
    }

    private sealed record Composed(
        ListBox List,
        CollectionsTabView.ScrollKeeper Keeper,
        CollectionsTabViewModel ViewModel,
        RecordingCollectorRunner Runner);

    /// <summary>Scrolls the list and lets the layout settle, before and after.</summary>
    /// <param name="composed">The list under test.</param>
    /// <param name="offset">Where to scroll to, in items - the default <c>ScrollUnit</c>.</param>
    /// <returns>Where it ended up.</returns>
    /// <remarks>
    /// The leading <c>UpdateLayout</c> matters: a pending <c>ScrollIntoView</c> is applied on the
    /// next measure and would otherwise overwrite the offset set here.
    /// </remarks>
    private static double ScrollTo(Composed composed, double offset)
    {
        composed.List.UpdateLayout();
        composed.Keeper.ScrollHost!.ScrollToVerticalOffset(offset);
        composed.List.UpdateLayout();

        return composed.Keeper.ScrollHost!.VerticalOffset;
    }

    private static Composed Compose(int elements)
    {
        FakeCollectorRegistry registry = new();

        for (int index = 0; index < elements; index++)
        {
            // Zero-padded so the check-list order is the order they were added in, whatever the
            // machine's culture does with digits.
            _ = registry.With($"C{index:D3}", $"Element {index:D3}");
        }

        RecordingCollectorRunner runner = new();
        FakeSessionService session = new();
        ShellWorkspace workspace = new(ShellWorkspaceTests.NewMatrix());

        CollectionsTabViewModel viewModel = new(
            workspace,
            new IdentificationPolicy(),
            registry,
            runner,
            session,
            new ShellProgress(new InlineUiDispatcher()),
            new InlineUiDispatcher(),
            new RecordingUserNotifier(),
            NullLogger<CollectionsTabViewModel>.Instance);

        session.Raise(FakeSessionService.NewSession());

        // The fake registry answers synchronously, so the list is already there.
        Assert.Equal(elements, viewModel.DataElements.Count);

        workspace.Matrix.PreparePopulation([ShellWorkspaceTests.NewPatient(8)]);
        workspace.SetPopulation(ShellWorkspaceTests.NewPopulation());

        // The same shape as the check list in CollectionsTabView.xaml, minus the theme: one row per
        // element, and a viewport far shorter than the content.
        ListBox list = new()
        {
            ItemsSource = viewModel.DataElements,
            DisplayMemberPath = nameof(DataElementViewModel.Title),
            Width = 260,
            Height = 160,
        };

        // BeginInit/EndInit, or the list has no Template at all: a code-created FrameworkElement
        // picks up its default theme style in OnInitialized, which nothing fires for an element that
        // is neither parsed from XAML nor added to a tree.  Without this, Measure produces no visual
        // children and the search for the scroll viewer finds nothing.
        list.BeginInit();
        list.EndInit();

        Size available = new(260, 160);

        list.Measure(available);
        list.Arrange(new Rect(new Point(0, 0), available));
        list.UpdateLayout();

        CollectionsTabView.ScrollKeeper keeper = new(list);

        keeper.Attach(viewModel);

        return new Composed(list, keeper, viewModel, runner);
    }
}
