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
/// bookkeeping around a collect run. It lives in <see cref="ScrollKeeper"/> rather than in this
/// class so that it can be tested: instantiating this view needs the theme, and a
/// <c>StaticResource</c> is resolved while the XAML is being parsed, so the only place it could be
/// found from is <see cref="Application.Current"/> - which is <see langword="null"/> under test and
/// must stay that way.
/// </para>
/// </remarks>
public partial class CollectionsTabView : UserControl
{
    private readonly ScrollKeeper _scroll;

    /// <summary>Initialises the tab.</summary>
    public CollectionsTabView()
    {
        InitializeComponent();

        _scroll = new ScrollKeeper(ElementsList);

        DataContextChanged += OnDataContextChanged;

        Unloaded += (_, _) =>
        {
            DataContextChanged -= OnDataContextChanged;

            _scroll.Attach(null);
        };
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) =>
        _scroll.Attach(e.NewValue as CollectionsTabViewModel);

    /// <summary>
    /// §G.4: keeps the check list where the user left it across a collect run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Delphi saves <c>TopIndex</c>, drags the selection down the list as it collects so the run
    /// is visible, and puts <c>TopIndex</c> back afterwards (<c>MainQuickStat.pas:651-676</c>). The
    /// three steps here are the same, except that the middle one scrolls without selecting: §G.4
    /// says to keep the "which element is being collected" feedback and drop the <c>ItemIndex</c>
    /// move, so a user's own selection survives a run - as it does in
    /// <c>Docs/Screenshots/QuickStat bilde 2.png</c>, still sitting on <c>^ Kjønn</c>.
    /// </para>
    /// <para>
    /// Where the list is scrolled to is the view's business and no view-model's, which is why this
    /// listens to two events and a property rather than binding anything.
    /// </para>
    /// </remarks>
    internal sealed class ScrollKeeper
    {
        private readonly ListBox _list;
        private ScrollViewer? _scrollHost;
        private CollectionsTabViewModel? _viewModel;
        private double _savedVerticalOffset;

        /// <summary>Creates the keeper for one check list.</summary>
        /// <param name="list">The data-element list.</param>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is <see langword="null"/>.</exception>
        internal ScrollKeeper(ListBox list)
        {
            ArgumentNullException.ThrowIfNull(list);

            _list = list;
        }

        /// <summary>The list's scroll viewer, once its template has been applied.</summary>
        internal ScrollViewer? ScrollHost => _scrollHost ??= FindScrollViewer(_list);

        /// <summary>Listens to a view-model, or to none.</summary>
        /// <param name="viewModel">The tab's view-model, or <see langword="null"/> to detach.</param>
        internal void Attach(CollectionsTabViewModel? viewModel)
        {
            if (_viewModel is not null)
            {
                _viewModel.CollectRunStarting -= OnCollectRunStarting;
                _viewModel.CollectRunFinished -= OnCollectRunFinished;
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            _viewModel = viewModel;

            if (_viewModel is null)
            {
                return;
            }

            _viewModel.CollectRunStarting += OnCollectRunStarting;
            _viewModel.CollectRunFinished += OnCollectRunFinished;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnCollectRunStarting(object? sender, EventArgs e) =>
            _savedVerticalOffset = ScrollHost?.VerticalOffset ?? 0;

        private void OnCollectRunFinished(object? sender, EventArgs e) =>
            ScrollHost?.ScrollToVerticalOffset(_savedVerticalOffset);

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is not nameof(CollectionsTabViewModel.CurrentlyCollecting))
            {
                return;
            }

            if (_viewModel?.CurrentlyCollecting is { } element)
            {
                // ScrollIntoView does not select, which is the whole point: the Delphi's
                // ItemIndex := n both scrolled and selected, and §G.4 keeps only the scrolling.
                _list.ScrollIntoView(element);
            }
        }
    }

    /// <summary>Finds the first <see cref="ScrollViewer"/> at or below an element.</summary>
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
}
