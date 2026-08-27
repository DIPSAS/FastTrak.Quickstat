using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using QuickStat.Services;
using QuickStat.ViewModels;

namespace QuickStat;

/// <summary>The shell window: banner, splitter, the three selection tabs and the dataset pane.</summary>
/// <remarks>
/// <para>
/// <c>05-ui-spec.md</c> §A.1-§A.3 for the layout and §G.1 for the geometry persistence. Step 3.1
/// owns this.
/// </para>
/// <para>
/// The code-behind is confined to the four things that are genuinely the window's: applying and
/// capturing geometry, keeping the splitter column and
/// <see cref="MainViewModel.SplitterPosition"/> in step, shutting the shell off while the busy
/// overlay is up, and disposing the view-model graph. All of it is thin, and the decisions behind
/// the first two live in <see cref="IWindowStateService"/>.
/// </para>
/// </remarks>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IWindowStateService _windowState;
    private readonly BusyOverlayViewModel _busyOverlay;
    private readonly ILogger<MainWindow> _logger;
    private IInputElement? _focusBeforeBusy;

    /// <summary>Initialises the window.</summary>
    /// <param name="viewModel">The shell view-model.</param>
    /// <param name="windowState">Persisted geometry, splitter position and last database.</param>
    /// <param name="busyOverlay">The overlay's view-model; the overlay is not on the main graph.</param>
    /// <param name="logger">Log.</param>
    public MainWindow(
        MainViewModel viewModel,
        IWindowStateService windowState,
        BusyOverlayViewModel busyOverlay,
        ILogger<MainWindow> logger)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(windowState);
        ArgumentNullException.ThrowIfNull(busyOverlay);
        ArgumentNullException.ThrowIfNull(logger);

        _viewModel = viewModel;
        _windowState = windowState;
        _busyOverlay = busyOverlay;
        _logger = logger;

        InitializeComponent();

        DataContext = viewModel;
        BusyOverlay.DataContext = busyOverlay;

        // The overlay's own Visibility is bound to IShellProgress.IsBusy, so its visibility - not the
        // flag - is what the lockout hangs off: it is the one signal that cannot be raised before the
        // thing the keyboard has to be moved onto exists.
        BusyOverlay.IsVisibleChanged += OnBusyOverlayVisibilityChanged;

        LeftColumn.Width = new GridLength(viewModel.SplitterPosition);

        _logger.LogInformation("Main window created.");
    }

    /// <inheritdoc />
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // After the HWND exists but before the first frame: setting Left/Top any earlier is
        // overwritten by WindowStartupLocation, and any later produces a visible jump.
        ApplyStoredPlacement();
    }

    /// <inheritdoc />
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        if (e.Cancel)
        {
            return;
        }

        // Delphi FormClose -> SaveFormState.  RestoreBounds rather than Left/Top/Width/Height: for a
        // maximised or minimised window the latter describe the maximised frame, and writing those
        // as the normal bounds is how a window "grows" a little on every run.
        _windowState.Save(new WindowPlacement(
            WindowState,
            WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds));

        _viewModel.PersistSplitterPosition(LeftColumn.ActualWidth);

        _windowState.Flush();
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e)
    {
        BusyOverlay.IsVisibleChanged -= OnBusyOverlayVisibilityChanged;

        // The view-models subscribe to the shell services, which are singletons and outlive the
        // window; without this the whole graph stays reachable after the window is gone.
        _busyOverlay.Dispose();
        _viewModel.Dispose();

        base.OnClosed(e);
    }

    /// <summary>Keeps the keyboard out of the shell for exactly as long as the overlay is up.</summary>
    /// <param name="sender">The overlay.</param>
    /// <param name="e">Unused; <see cref="UIElement.IsVisible"/> is read directly.</param>
    /// <remarks>
    /// <para>
    /// <c>05-ui-spec.md</c> §G.3 is a wait cursor and nothing else, because the Delphi ran the query
    /// on the user-interface thread: while it worked, no message was pumped, so there was no keyboard
    /// to shut out. The port does the work elsewhere and the window stays alive, so the overlay's
    /// dimmed pane - which stops the mouse only, by hit-testing - left <c>Tab</c> walking straight
    /// into the check list underneath it. PORT-PLAN.md §8.10 (f).
    /// </para>
    /// <para>
    /// <b><c>IsEnabled = false</c> is the mechanism, and on its own it is not enough.</b> Measured
    /// rather than assumed, in <c>Ui/Shell/MainWindowBusyLockoutTests.cs</c>: disabling the content
    /// host does stop the keyboard <em>arriving</em> - <see cref="UIElement.Focus"/> on anything
    /// inside returns <see langword="false"/> and tab traversal skips the whole subtree - but it does
    /// <em>not</em> evict focus that is already there. The focused element stays focused while
    /// disabled, and a disabled element handles no input, so the keyboard would be parked on a dead
    /// control with the Cancel button unreachable. Hence the explicit move, and the restore on the
    /// way out. (<c>KeyboardNavigation.TabNavigation="None"</c> was considered and is strictly
    /// weaker: it turns <c>Tab</c> away and leaves access keys, <c>Ctrl+Tab</c> and typing into
    /// whatever already has focus alone.)
    /// </para>
    /// <para>
    /// The order is load-bearing in both directions: disable before taking focus, and re-enable
    /// before giving it back, or the restore lands on a control that cannot accept it.
    /// </para>
    /// </remarks>
    private void OnBusyOverlayVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (BusyOverlay.IsVisible)
        {
            _focusBeforeBusy = Keyboard.FocusedElement;

            ShellContent.IsEnabled = false;

            // The overlay is Focusable for this one reason. It parks the keyboard somewhere harmless
            // and, when an operation has offered one, puts Cancel one Tab away.
            _ = BusyOverlay.Focus();

            return;
        }

        ShellContent.IsEnabled = true;

        // Back where the user left it - §G.4's argument about the check list, applied to focus rather
        // than to the scroll offset. A stale element simply refuses, which is the same as doing
        // nothing.
        _ = _focusBeforeBusy?.Focus();

        _focusBeforeBusy = null;
    }

    private void ApplyStoredPlacement()
    {
        if (_windowState.Restore(new Size(Width, Height)) is not { } placement)
        {
            // Nothing stored: keep WindowStartupLocation="CenterScreen".  The Delphi would put the
            // window at 0,0 here, because it reads Left and Top with a default of zero.
            return;
        }

        if (placement.Bounds is { } bounds)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;

            Left = bounds.Left;
            Top = bounds.Top;
            Width = bounds.Width;
            Height = bounds.Height;
        }

        // Minimized is restored faithfully - the Delphi stores and restores all three states - but
        // starting minimised looks like a failure to launch, so it is promoted to Normal.  The
        // stored value is left alone; only what happens on this run changes.
        WindowState = placement.State == WindowState.Minimized ? WindowState.Normal : placement.State;
    }

    private void OnSplitterDragCompleted(object sender, DragCompletedEventArgs e) =>
        _viewModel.PersistSplitterPosition(LeftColumn.ActualWidth);
}
