using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickStat.Services;

namespace QuickStat.ViewModels;

/// <summary>The overlay shown while a long-running operation is in flight.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.6. This is a compiling stub</b> - it reports the state correctly and the view
/// that renders it is a placeholder.
/// </para>
/// <para>
/// Replaces the Delphi's <c>Screen.Cursor := crSqlWait</c> (<c>05-ui-spec.md</c> §G.3). Because the
/// port does its database work off the user-interface thread, a cursor alone no longer says enough:
/// the window stays responsive, so something has to explain why the buttons are dead.
/// </para>
/// <para>
/// <b>What is left to do:</b> the visual - a dimming layer, the current status line, and a
/// cancel affordance for the operations that take a
/// <see cref="System.Threading.CancellationToken"/>. Keep it non-interactive otherwise; the point is
/// to stop a second collect run starting on top of the first.
/// </para>
/// </remarks>
public sealed partial class BusyOverlayViewModel : ObservableObject, IDisposable
{
    private readonly IShellProgress _progress;
    private bool _disposed;

    /// <summary>Creates the overlay's view-model.</summary>
    /// <param name="progress">The busy flag and the status line.</param>
    /// <exception cref="ArgumentNullException"><paramref name="progress"/> is <see langword="null"/>.</exception>
    public BusyOverlayViewModel(IShellProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        _progress = progress;
        _progress.PropertyChanged += OnProgressChanged;
    }

    /// <summary>Whether the overlay is up.</summary>
    public bool IsBusy => _progress.IsBusy;

    /// <summary>What the operation is doing, mirroring the banner's status line.</summary>
    public string Message => _progress.Info;

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _progress.PropertyChanged -= OnProgressChanged;
    }

    private void OnProgressChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IShellProgress.IsBusy))
        {
            OnPropertyChanged(nameof(IsBusy));
        }
        else if (e.PropertyName is nameof(IShellProgress.Info))
        {
            OnPropertyChanged(nameof(Message));
        }
    }
}
