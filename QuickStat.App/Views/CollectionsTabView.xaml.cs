using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QuickStat.ViewModels;

namespace QuickStat.Views;

/// <summary>The <c>Collections</c> tab.</summary>
/// <remarks>
/// <para>
/// <c>05-ui-spec.md</c> §B.2. Step 3.3 owns this; step 3.1 wrote the layout skeleton and wired the
/// radio group and the timestamp check box to their shared homes.
/// </para>
/// <para>
/// The code-behind does one thing, and it is the thing that cannot be done in XAML: §G.4's scroll
/// bookkeeping around a collect run. The Delphi saves <c>TopIndex</c>, drags the selection down the
/// list as it collects so the user can see progress, and puts <c>TopIndex</c> back afterwards
/// (<c>MainQuickStat.pas:651-676</c>). Here the same three steps are: remember
/// <see cref="ScrollViewer.VerticalOffset"/>, bring each collecting element into view <b>without</b>
/// touching <c>SelectedItem</c>, and scroll back when the run ends. Which element is being
/// collected, and when a run starts and stops, are the view-model's; where the list is scrolled to
/// is the view's, and no view-model should know.
/// </para>
/// </remarks>
public partial class CollectionsTabView : UserControl
{
    private CollectionsTabViewModel? _viewModel;
    private ScrollViewer? _scrollHost;
    private double _savedVerticalOffset;

    /// <summary>Initialises the tab.</summary>
    public CollectionsTabView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;

        Unloaded += (_, _) =>
        {
            DataContextChanged -= OnDataContextChanged;

            Detach();
        };
    }

    /// <summary>Finds the first <see cref="ScrollViewer"/> below an element.</summary>
    /// <param name="root">Where to start; the search includes it.</param>
    /// <returns>The scroll viewer, or <see langword="null"/> when the template is not applied yet.</returns>
    /// <remarks>
    /// A walk rather than <c>Template.FindName</c>: the default <see cref="ListBox"/> template's
    /// scroll viewer carries no <c>x:Name</c>, so there is nothing to look up, and a template that
    /// did name it would tie this view to one theme.
    /// </remarks>
    internal static ScrollViewer? FindScrollViewer(DependencyObject? root)
    {
        if (root is null)
        {
            return null;
        }

        if (root is ScrollViewer found)
        {
            return found;
        }

        int count = VisualTreeHelper.GetChildrenCount(root);

        for (int index = 0; index < count; index++)
        {
            if (FindScrollViewer(VisualTreeHelper.GetChild(root, index)) is { } scrollViewer)
            {
                return scrollViewer;
            }
        }

        return null;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        Detach();

        _viewModel = e.NewValue as CollectionsTabViewModel;

        if (_viewModel is null)
        {
            return;
        }

        _viewModel.CollectRunStarting += OnCollectRunStarting;
        _viewModel.CollectRunFinished += OnCollectRunFinished;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void Detach()
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.CollectRunStarting -= OnCollectRunStarting;
        _viewModel.CollectRunFinished -= OnCollectRunFinished;
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel = null;
    }

    private void OnCollectRunStarting(object? sender, EventArgs e) =>
        _savedVerticalOffset = ScrollHost()?.VerticalOffset ?? 0;

    private void OnCollectRunFinished(object? sender, EventArgs e) =>
        ScrollHost()?.ScrollToVerticalOffset(_savedVerticalOffset);

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not nameof(CollectionsTabViewModel.CurrentlyCollecting))
        {
            return;
        }

        if (_viewModel?.CurrentlyCollecting is { } element)
        {
            // ScrollIntoView does not select, which is the whole point: the Delphi's ItemIndex := n
            // both scrolled and selected, and §G.4 keeps only the scrolling.
            ElementsList.ScrollIntoView(element);
        }
    }

    private ScrollViewer? ScrollHost() => _scrollHost ??= FindScrollViewer(ElementsList);
}
