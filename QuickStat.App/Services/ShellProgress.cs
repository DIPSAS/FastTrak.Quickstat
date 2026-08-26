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
    public void Report(OperationProgress value) => _dispatcher.Invoke(() =>
    {
        // Only a non-empty header wins: see IShellProgress.Header.
        if (!string.IsNullOrEmpty(value.Header))
        {
            Set(ref _header, value.Header, nameof(Header));
        }

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
    public IDisposable BeginOperation(string info)
    {
        ArgumentNullException.ThrowIfNull(info);

        _dispatcher.Invoke(() =>
        {
            _busyDepth++;

            if (_busyDepth == 1)
            {
                Raise(nameof(IsBusy));
            }

            Set(ref _info, info, nameof(Info));
            Set(ref _isError, false, nameof(IsError));
        });

        return new Operation(this);
    }

    private void EndOperation() => _dispatcher.Invoke(() =>
    {
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

    /// <summary>The token <see cref="BeginOperation"/> hands out. Disposing twice is harmless.</summary>
    private sealed class Operation : IDisposable
    {
        private ShellProgress? _owner;

        internal Operation(ShellProgress owner) => _owner = owner;

        public void Dispose()
        {
            ShellProgress? owner = Interlocked.Exchange(ref _owner, null);

            owner?.EndOperation();
        }
    }
}
