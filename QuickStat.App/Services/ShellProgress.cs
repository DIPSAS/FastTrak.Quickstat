using System.ComponentModel;
using QuickStat.Diagnostics;

namespace QuickStat.Services;

/// <summary>The one implementation of <see cref="IShellProgress"/>.</summary>
/// <remarks>
/// Every mutation is funnelled through the <see cref="IUiDispatcher"/>, because
/// <see cref="Report"/> is called from whatever thread a Core service happens to be on and WPF
/// bindings must be updated from the user-interface thread. The dispatcher runs inline when it is
/// already there, so the common case costs nothing.
/// </remarks>
public sealed class ShellProgress : IShellProgress
{
    /// <summary>The heading, which nothing in QuickStat ever changes. Delphi <c>lblProgress.Caption</c>.</summary>
    public const string DefaultHeader = "Progress";

    /// <summary>The status line before anything has happened. Delphi <c>lblInfo.Caption</c>.</summary>
    public const string IdleText = "Program is idle";

    /// <summary>The status line after a connect or a collect run. Delphi <c>TXT_TASK_COMPLETED</c>.</summary>
    public const string CompletedText = "Task completed";

    private readonly IUiDispatcher _dispatcher;
    private readonly List<CancellationTokenSource> _cancellations = [];
    private string _header = DefaultHeader;
    private string _info = IdleText;
    private double _percent;
    private bool _isError;
    private int _busyDepth;

    /// <summary>Creates the progress service.</summary>
    /// <param name="dispatcher">Marshals every change onto the user-interface thread.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dispatcher"/> is <see langword="null"/>.</exception>
    public ShellProgress(IUiDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        _dispatcher = dispatcher;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public string Header => _header;

    /// <inheritdoc />
    public string Info => _info;

    /// <inheritdoc />
    public double Percent => _percent;

    /// <inheritdoc />
    public bool IsError => _isError;

    /// <inheritdoc />
    public bool IsBusy => _busyDepth > 0;

    /// <inheritdoc />
    public bool IsCancellable => _cancellations.Count > 0;

    /// <inheritdoc />
    public void Report(OperationProgress value) => _dispatcher.Invoke(() =>
    {
        // value.Header is deliberately ignored.  Core's login pipeline reports "Connecting" and
        // CollectorRunner reports "Collecting data", and letting either win left the banner reading
        // "Collecting data" for the rest of the session - the label never went back.
        //
        // The Delphi's header is a static caption.  TfrmQuickStat implements IProgress.SetHeader
        // (MainQuickStat.pas:433, assigning lblProgress.Caption) and *nothing in the application
        // ever calls it*, which is what 05-ui-spec.md §G.6 records. The operation's name belongs on
        // the status line underneath, which is exactly where Info puts it.
        //
        // Found by step 3.3 during Phase 3 wave 2, and verified against the Delphi source.
        Set(ref _info, value.Info ?? "", nameof(Info));
        Set(ref _isError, false, nameof(IsError));

        if (value.Percent is { } percent)
        {
            Set(ref _percent, Math.Clamp(percent, 0, 100), nameof(Percent));
        }
    });

    /// <inheritdoc />
    public void SetInfo(string info)
    {
        ArgumentNullException.ThrowIfNull(info);

        _dispatcher.Invoke(() =>
        {
            Set(ref _info, info, nameof(Info));
            Set(ref _isError, false, nameof(IsError));
        });
    }

    /// <inheritdoc />
    public void Fail(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _dispatcher.Invoke(() =>
        {
            Set(ref _info, message, nameof(Info));
            Set(ref _isError, true, nameof(IsError));
        });
    }

    /// <inheritdoc />
    public void Done() => _dispatcher.Invoke(() =>
    {
        Set(ref _percent, 100d, nameof(Percent));
        Set(ref _info, CompletedText, nameof(Info));
        Set(ref _isError, false, nameof(IsError));
    });

    /// <inheritdoc />
    public void Reset() => _dispatcher.Invoke(() =>
    {
        Set(ref _percent, 0d, nameof(Percent));
        Set(ref _info, IdleText, nameof(Info));
        Set(ref _isError, false, nameof(IsError));
    });

    /// <inheritdoc />
    public void RequestCancellation() => _dispatcher.Invoke(() =>
    {
        // A snapshot, because Cancel runs its registered callbacks synchronously and one of them may
        // well be the operation unwinding - which withdraws its own offer from this very list.
        foreach (CancellationTokenSource cancellation in _cancellations.ToArray())
        {
            try
            {
                cancellation.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Disposed by its owner between the offer and the click: that operation is over, and
                // a button press must not take the application down.
            }
        }
    });

    /// <inheritdoc />
    public IDisposable BeginOperation(string info) => Begin(info, null);

    /// <inheritdoc />
    public IDisposable BeginOperation(string info, CancellationTokenSource cancellation)
    {
        ArgumentNullException.ThrowIfNull(cancellation);

        return Begin(info, cancellation);
    }

    private IDisposable Begin(string info, CancellationTokenSource? cancellation)
    {
        ArgumentNullException.ThrowIfNull(info);

        _dispatcher.Invoke(() =>
        {
            _busyDepth++;

            if (_busyDepth == 1)
            {
                Raise(nameof(IsBusy));
            }

            if (cancellation is not null)
            {
                _cancellations.Add(cancellation);

                if (_cancellations.Count == 1)
                {
                    Raise(nameof(IsCancellable));
                }
            }

            Set(ref _info, info, nameof(Info));
            Set(ref _isError, false, nameof(IsError));
        });

        return new Operation(this, cancellation);
    }

    private void EndOperation(CancellationTokenSource? cancellation) => _dispatcher.Invoke(() =>
    {
        // The offer goes first, so the overlay drops "Cancelling…" before it drops the overlay; the
        // other order shows the resting state for one frame on the way out.
        if (cancellation is not null && _cancellations.Remove(cancellation) && _cancellations.Count == 0)
        {
            Raise(nameof(IsCancellable));
        }

        if (_busyDepth == 0)
        {
            return;
        }

        _busyDepth--;

        if (_busyDepth == 0)
        {
            Raise(nameof(IsBusy));
        }
    });

    private void Set<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;

        Raise(propertyName);
    }

    private void Raise(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary>The token <c>BeginOperation</c> hands out. Disposing twice is harmless.</summary>
    private sealed class Operation : IDisposable
    {
        private readonly CancellationTokenSource? _cancellation;
        private ShellProgress? _owner;

        internal Operation(ShellProgress owner, CancellationTokenSource? cancellation)
        {
            _owner = owner;
            _cancellation = cancellation;
        }

        public void Dispose()
        {
            ShellProgress? owner = Interlocked.Exchange(ref _owner, null);

            owner?.EndOperation(_cancellation);
        }
    }
}
