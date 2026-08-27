using System.Runtime.ExceptionServices;
using System.Windows.Threading;

namespace QuickStat.Tests.Ui;

/// <summary>
/// Runs a piece of WPF code on a single-threaded-apartment thread, because the test runner does not
/// provide one.
/// </summary>
/// <remarks>
/// <para>
/// Established by experiment on this machine, not assumed. xUnit v2 on VSTest runs every test on an
/// <b>MTA</b> thread, and on an MTA thread both <c>new Window()</c> and
/// <see cref="System.Windows.UIElement.Measure"/> throw
/// <see cref="InvalidOperationException"/> - <i>"The calling thread must be STA, because many UI
/// components require this."</i> A test that touches either must go through this helper.
/// </para>
/// <para>
/// Three further facts that shape what this class does and does not do:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <b>Many STA threads per process are fine.</b> Each call below creates and joins its own, so
/// tests stay independent and can run in any order.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>No <see cref="System.Windows.Application"/> is needed</b> for measuring and arranging, and
/// none is created here on purpose: WPF allows exactly one <c>Application</c> per <c>AppDomain</c>,
/// so a helper that made one per call would make every later call fail with <i>"Cannot create more
/// than one System.Windows.Application instance in the same AppDomain."</i> The assembly's single
/// one lives in <see cref="WpfApplicationFixture"/> instead, on an apartment of its own, and is
/// needed only by a view that resolves the theme through <c>Application.Current</c>. The
/// consequence for production code is unchanged: <c>Application.Current</c> <em>may</em> be
/// <see langword="null"/> under test - never dereference it without a null check.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b><see cref="System.Windows.Media.FormattedText"/> works on the plain MTA test thread.</b> Text
/// measurement therefore needs no ceremony at all; use this helper only for things that genuinely
/// require an apartment.
/// </description>
/// </item>
/// </list>
/// <para>
/// Shared by every Phase 3 step. It is not owned by any of them: treat it as read-only and report a
/// missing capability rather than editing it, so two parallel steps cannot disagree about it.
/// </para>
/// </remarks>
public static class StaTestRunner
{
    /// <summary>How long a single STA body may take before the run is declared hung.</summary>
    /// <remarks>
    /// Generous, because it exists to turn a deadlock into a readable failure rather than to police
    /// performance. The worker is a background thread, so a hung one cannot keep the process alive.
    /// </remarks>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Runs <paramref name="body"/> on a fresh STA thread and waits for it.</summary>
    /// <param name="body">The code to run. Every WPF object it creates belongs to that thread.</param>
    /// <param name="timeout">Optional override for <see cref="DefaultTimeout"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
    /// <exception cref="TimeoutException">The body did not finish within the timeout.</exception>
    public static void Run(Action body, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(body);

        _ = Run<object?>(
            () =>
            {
                body();

                return null;
            },
            timeout);
    }

    /// <summary>Runs <paramref name="body"/> on a fresh STA thread and returns its result.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="body">The code to run.</param>
    /// <param name="timeout">Optional override for <see cref="DefaultTimeout"/>.</param>
    /// <returns>Whatever <paramref name="body"/> returned.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
    /// <exception cref="TimeoutException">The body did not finish within the timeout.</exception>
    /// <remarks>
    /// A WPF object must not cross back to the calling thread; return a plain value - a measured
    /// size, a rendered string, a bool - rather than the element that produced it.
    /// </remarks>
    public static T Run<T>(Func<T> body, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(body);

        T result = default!;
        ExceptionDispatchInfo? failure = null;

        Thread thread = new(() =>
        {
            try
            {
                result = body();
            }
            catch (Exception exception)
            {
                // Captured rather than rethrown here: rethrowing on the worker would take the
                // process down instead of failing the test.  ExceptionDispatchInfo keeps the
                // original stack trace when it is rethrown on the caller's thread below.
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        })
        {
            IsBackground = true,
            Name = "StaTestRunner",
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!thread.Join(timeout ?? DefaultTimeout))
        {
            throw new TimeoutException(
                $"The STA test body did not finish within {(timeout ?? DefaultTimeout).TotalSeconds:F0} s.");
        }

        failure?.Throw();

        return result;
    }

    /// <summary>
    /// Runs <paramref name="body"/> on a fresh STA thread with a <b>pumped</b> dispatcher.
    /// </summary>
    /// <param name="body">The code to run.</param>
    /// <param name="timeout">Optional override for <see cref="DefaultTimeout"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
    /// <exception cref="TimeoutException">The body did not finish within the timeout.</exception>
    /// <remarks>
    /// Use this, rather than <see cref="Run(Action, TimeSpan?)"/>, whenever the code under test
    /// posts work back to the dispatcher - data binding updates, <c>Dispatcher.BeginInvoke</c>,
    /// <c>CommandManager.InvalidateRequerySuggested</c>. Without a running dispatcher that work is
    /// queued and never executed, and the test sees a stale value with no error to explain it.
    /// </remarks>
    public static void RunWithDispatcher(Action body, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(body);

        RunWithDispatcher(
            () =>
            {
                body();

                return Task.CompletedTask;
            },
            timeout);
    }

    /// <summary>
    /// Runs an asynchronous <paramref name="body"/> on a fresh STA thread with a pumped dispatcher,
    /// so that <c>await</c> continuations come back to that same thread.
    /// </summary>
    /// <param name="body">The code to run.</param>
    /// <param name="timeout">Optional override for <see cref="DefaultTimeout"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
    /// <exception cref="TimeoutException">The body did not finish within the timeout.</exception>
    /// <remarks>
    /// This is the shape an <c>IAsyncRelayCommand</c> test wants: the command is invoked on the UI
    /// thread, its continuations are marshalled back to it, and the dispatcher keeps running until
    /// the returned task completes.
    /// </remarks>
    public static void RunWithDispatcher(Func<Task> body, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(body);

        Run(
            () =>
            {
                Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

                // Without this, an "await" inside the body resumes on the thread pool and any WPF
                // object it then touches throws a cross-thread exception.
                SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));

                ExceptionDispatchInfo? failure = null;

                _ = dispatcher.BeginInvoke(async () =>
                {
                    try
                    {
                        await body().ConfigureAwait(true);
                    }
                    catch (Exception exception)
                    {
                        failure = ExceptionDispatchInfo.Capture(exception);
                    }
                    finally
                    {
                        // Always: an exception must still stop the pump, or Run's timeout becomes
                        // the only thing that ends the test and the real cause is lost.
                        dispatcher.InvokeShutdown();
                    }
                });

                Dispatcher.Run();

                failure?.Throw();
            },
            timeout);
    }
}
