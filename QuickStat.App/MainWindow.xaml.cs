using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
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
/// The code-behind is confined to the three things that are genuinely the window's: applying and
/// capturing geometry, keeping the splitter column and
/// <see cref="MainViewModel.SplitterPosition"/> in step, and disposing the view-model graph. All of
/// it is thin, and the decisions behind it live in <see cref="IWindowStateService"/>.
/// </para>
/// </remarks>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IWindowStateService _windowState;
    private readonly BusyOverlayViewModel _busyOverlay;
    private readonly ILogger<MainWindow> _logger;

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
        // The view-models subscribe to the shell services, which are singletons and outlive the
        // window; without this the whole graph stays reachable after the window is gone.
        _busyOverlay.Dispose();
        _viewModel.Dispose();

        base.OnClosed(e);
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
