using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickStat.Services;

namespace QuickStat.ViewModels;

/// <summary>The overlay shown while a long-running operation is in flight.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.6.</b> Replaces the Delphi's <c>Screen.Cursor := crSqlWait</c>
/// (<c>05-ui-spec.md</c> §G.3). Because the port does its database work off the user-interface
/// thread, a cursor alone no longer says enough: the window stays responsive, so something has to
/// explain why the buttons are dead.
/// </para>
/// <para>
/// <b>Everything here is derived from <see cref="IShellProgress"/>; nothing is assigned.</b>
/// <see cref="IShellProgress.BeginOperation(string)"/> counts, which is what keeps the overlay up
/// while the package replay runs a collect inside its own scope - the Delphi saves and restores
/// <c>Screen.Cursor</c> for exactly that reason rather than assigning <c>crDefault</c>. Cancelling
/// therefore does <em>not</em> take the overlay down: it signals the token and leaves the operation
/// to unwind its own scope, which is the only thing that can know the work has actually stopped.
/// </para>
/// <para>
/// <b>The cancel affordance is opt-in, and the offer is not this class's to make.</b> A Cancel
/// button appears only while an operation opened its scope with
/// <see cref="IShellProgress.BeginOperation(string, CancellationTokenSource)"/>, because a button
/// that cannot stop anything is worse than no button. The register of offers lives on the service
/// and not here for the reason the rest of this class exists: the operations are started by the tab
/// view-models, which cannot reach the overlay, and a second register kept here would be a second
/// thing able to disagree with <see cref="IShellProgress.IsCancellable"/>. PORT-PLAN.md §8.10 (c).
/// </para>
/// </remarks>
public sealed partial class BusyOverlayViewModel : ObservableObject, IDisposable
{
    /// <summary>The cancel button's caption.</summary>
    public const string CancelCaption = "Cancel";

    /// <summary>Shown once cancellation has been requested and before the operation has stopped.</summary>
    public const string CancellingText = "Cancelling…";

    private readonly IShellProgress _progress;
    private bool _isCancelling;
    private bool _disposed;

    /// <summary>Creates the overlay's view-model.</summary>
    /// <param name="progress">The busy flag and the status line.</param>
    /// <exception cref="ArgumentNullException"><paramref name="progress"/> is <see langword="null"/>.</exception>
    public BusyOverlayViewModel(IShellProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        _progress = progress;
        _progress.PropertyChanged += OnProgressChanged;

        CancelCommand = new RelayCommand(Cancel, () => CanCancel);
    }

    /// <summary>Whether the overlay is up.</summary>
    public bool IsBusy => _progress.IsBusy;

    /// <summary>What the operation is doing, mirroring the banner's status line.</summary>
    public string Message => _progress.Info;

    /// <summary>Completion, 0 to 100, mirroring the banner's bar.</summary>
    /// <remarks>
    /// Determinate rather than a spinner: a collect run reports a real percentage per patient
    /// (§G.6), and the <c>QsProgressBar</c> template has no indeterminate animation to fall back on.
    /// </remarks>
    public double Percent => _progress.Percent;

    /// <summary>Whether a Cancel button is shown at all.</summary>
    public bool IsCancelOffered => _progress.IsCancellable;

    /// <summary>Whether that button is live. False once it has been pressed.</summary>
    public bool CanCancel => IsCancelOffered && !IsCancelling;

    /// <summary>Whether cancellation has been requested and the operation has not yet stopped.</summary>
    public bool IsCancelling
    {
        get => _isCancelling;
        private set
        {
            if (!SetProperty(ref _isCancelling, value))
            {
                return;
            }

            OnPropertyChanged(nameof(CanCancel));
            CancelCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Requests cancellation of every operation that offered it.</summary>
    public IRelayCommand CancelCommand { get; }

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

    private void Cancel()
    {
        if (!CanCancel)
        {
            return;
        }

        // Signal, then say so. IsBusy is deliberately untouched: the overlay comes down when the
        // operation's own BeginOperation scope is disposed, which is the only moment the work has
        // really stopped.
        _progress.RequestCancellation();

        IsCancelling = true;
    }

    private void OnProgressChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IShellProgress.IsBusy):
                OnPropertyChanged(nameof(IsBusy));

                break;

            case nameof(IShellProgress.Info):
                OnPropertyChanged(nameof(Message));

                break;

            case nameof(IShellProgress.Percent):
                OnPropertyChanged(nameof(Percent));

                break;

            case nameof(IShellProgress.IsCancellable):
                if (!_progress.IsCancellable)
                {
                    // Back to the resting state, so the next operation does not inherit "Cancelling".
                    IsCancelling = false;
                }

                OnPropertyChanged(nameof(IsCancelOffered));
                OnPropertyChanged(nameof(CanCancel));
                CancelCommand.NotifyCanExecuteChanged();

                break;

            default:
                break;
        }
    }
}
