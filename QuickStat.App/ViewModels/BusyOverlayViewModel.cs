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
/// <see cref="IShellProgress.BeginOperation"/> counts, which is what keeps the overlay up while the
/// package replay runs a collect inside its own scope - the Delphi saves and restores
/// <c>Screen.Cursor</c> for exactly that reason rather than assigning <c>crDefault</c>. Cancelling
/// therefore does <em>not</em> take the overlay down: it signals the token and leaves the operation
/// to unwind its own scope, which is the only thing that can know the work has actually stopped.
/// </para>
/// <para>
/// <b>The cancel affordance is opt-in and currently unused.</b> A Cancel button appears only while
/// something has called <see cref="OfferCancellation"/>, because a button that cannot stop anything
/// is worse than no button. Nothing calls it yet: the natural place is
/// <see cref="IShellProgress.BeginOperation"/>, which takes no
/// <see cref="CancellationTokenSource"/> and belongs to step 3.1. Until that seam exists, an
/// operation that wants to be cancellable opens both scopes itself.
/// </para>
/// </remarks>
public sealed partial class BusyOverlayViewModel : ObservableObject, IDisposable
{
    /// <summary>The cancel button's caption.</summary>
    public const string CancelCaption = "Cancel";

    /// <summary>Shown once cancellation has been requested and before the operation has stopped.</summary>
    public const string CancellingText = "Cancelling…";

    private readonly IShellProgress _progress;
    private readonly List<CancellationTokenSource> _cancellations = [];
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
    /// Determinate rather than a spinner because there is a real percentage to show: a collect run
    /// reports one step per patient (§G.6). <c>QsProgressBar</c> animates <c>IsIndeterminate</c> as
    /// of PORT-PLAN.md §8.10 (d), so this is a choice about the data rather than, as it once was, a
    /// gap in the theme.
    /// </remarks>
    public double Percent => _progress.Percent;

    /// <summary>Whether a Cancel button is shown at all.</summary>
    public bool IsCancelOffered => _cancellations.Count > 0;

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

    /// <summary>
    /// Offers the user a Cancel button for as long as the returned scope lives.
    /// </summary>
    /// <param name="cancellation">The source the button signals. Not disposed here.</param>
    /// <returns>A scope that withdraws the offer. Disposing twice is harmless.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cancellation"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// Nests, like <see cref="IShellProgress.BeginOperation"/>: the package replay's collect can
    /// offer its own token inside the replay's. Cancel signals <em>all</em> of them, because the
    /// user is cancelling the operation they can see and the inner one is part of it.
    /// </para>
    /// <para>
    /// Call it on the user-interface thread; it touches bindable state.
    /// </para>
    /// </remarks>
    public IDisposable OfferCancellation(CancellationTokenSource cancellation)
    {
        ArgumentNullException.ThrowIfNull(cancellation);

        _cancellations.Add(cancellation);

        OnPropertyChanged(nameof(IsCancelOffered));
        OnPropertyChanged(nameof(CanCancel));
        CancelCommand.NotifyCanExecuteChanged();

        return new Offer(this, cancellation);
    }

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
        foreach (CancellationTokenSource cancellation in _cancellations.ToArray())
        {
            // A source disposed by its owner between the offer and the click is not an error; the
            // operation it belonged to is over.
            try
            {
                cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Nothing to cancel.
            }
        }

        IsCancelling = true;
    }

    private void Withdraw(CancellationTokenSource cancellation)
    {
        if (!_cancellations.Remove(cancellation))
        {
            return;
        }

        if (_cancellations.Count == 0)
        {
            // Back to the resting state, so the next operation does not inherit "Cancelling".
            IsCancelling = false;
        }

        OnPropertyChanged(nameof(IsCancelOffered));
        OnPropertyChanged(nameof(CanCancel));
        CancelCommand.NotifyCanExecuteChanged();
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

            default:
                break;
        }
    }

    /// <summary>The scope <see cref="OfferCancellation"/> hands out.</summary>
    private sealed class Offer : IDisposable
    {
        private readonly CancellationTokenSource _cancellation;
        private BusyOverlayViewModel? _owner;

        internal Offer(BusyOverlayViewModel owner, CancellationTokenSource cancellation)
        {
            _owner = owner;
            _cancellation = cancellation;
        }

        public void Dispose()
        {
            BusyOverlayViewModel? owner = Interlocked.Exchange(ref _owner, null);

            owner?.Withdraw(_cancellation);
        }
    }
}
