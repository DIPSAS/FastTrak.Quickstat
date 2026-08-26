using System.Windows;
using System.Windows.Threading;

namespace QuickStat.Services;

/// <summary>
/// The WPF <see cref="IUiDispatcher"/>: the application's own <see cref="Dispatcher"/> when there is
/// one, and a straight-through call when there is not.
/// </summary>
/// <remarks>
/// <para>
/// The "when there is not" branch is not defensive padding. WPF allows exactly one
/// <see cref="Application"/> per <c>AppDomain</c> and the test suite deliberately creates none, so
/// <see cref="Application.Current"/> is <see langword="null"/> for every test that composes the
/// container. Falling back to inline execution keeps the whole graph resolvable and exercisable
/// headlessly; a null reference here would make the shell untestable.
/// </para>
/// <para>
/// The dispatcher is captured once, at construction, from <see cref="Application.Current"/>. The
/// container builds this on the UI thread during start-up, so that is the right one.
/// </para>
/// </remarks>
public sealed class WpfUiDispatcher : IUiDispatcher
{
    private readonly Dispatcher? _dispatcher;

    /// <summary>Creates a dispatcher bound to <see cref="Application.Current"/>, if any.</summary>
    public WpfUiDispatcher()
        : this(Application.Current?.Dispatcher)
    {
    }

    /// <summary>Creates a dispatcher bound to a specific <see cref="Dispatcher"/>.</summary>
    /// <param name="dispatcher">The target dispatcher, or <see langword="null"/> to run inline.</param>
    public WpfUiDispatcher(Dispatcher? dispatcher) => _dispatcher = dispatcher;

    /// <inheritdoc />
    public bool IsOnUiThread => _dispatcher is null || _dispatcher.CheckAccess();

    /// <inheritdoc />
    public void Invoke(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (IsOnUiThread)
        {
            action();

            return;
        }

        _dispatcher!.Invoke(action);
    }

    /// <inheritdoc />
    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (_dispatcher is null)
        {
            action();

            return;
        }

        _ = _dispatcher.BeginInvoke(action);
    }

    /// <inheritdoc />
    public Task InvokeAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (IsOnUiThread)
        {
            action();

            return Task.CompletedTask;
        }

        return _dispatcher!.InvokeAsync(action).Task;
    }
}
