namespace QuickStat.Services;

/// <summary>
/// Marshals work onto the user-interface thread, so nothing outside a view has to mention
/// <c>Dispatcher</c>.
/// </summary>
/// <remarks>
/// <para>
/// Two reasons this exists rather than a direct <c>Application.Current.Dispatcher</c> call. First,
/// <c>Application.Current</c> may be <see langword="null"/> under test - most of the suite composes
/// the container without one (PORT-PLAN.md §5 Phase 3, §8.10 (a)) - so a view-model that reaches for
/// it is a view-model that cannot be unit-tested. Second, background
/// work in <c>QuickStat.Core</c> - a collect run reporting progress per patient, a session change -
/// completes on the thread pool, and every one of those callbacks would otherwise need its own
/// marshalling code.
/// </para>
/// <para>
/// Wave-2 steps: inject this, do not touch <c>Dispatcher</c>. In a unit test, use a fake that runs
/// the callback inline.
/// </para>
/// </remarks>
public interface IUiDispatcher
{
    /// <summary>Whether the calling thread is already the user-interface thread.</summary>
    /// <remarks>
    /// <see langword="true"/> when there is no dispatcher at all, which is the headless case: the
    /// caller is then already on the only thread there is.
    /// </remarks>
    bool IsOnUiThread { get; }

    /// <summary>Runs an action on the user-interface thread and waits for it.</summary>
    /// <param name="action">The work.</param>
    /// <remarks>Executes inline when already on that thread, so it is safe to call unconditionally.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    void Invoke(Action action);

    /// <summary>Queues an action onto the user-interface thread without waiting.</summary>
    /// <param name="action">The work.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    void Post(Action action);

    /// <summary>Queues an action onto the user-interface thread and awaits its completion.</summary>
    /// <param name="action">The work.</param>
    /// <returns>A task that completes when the action has run.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    Task InvokeAsync(Action action);
}
