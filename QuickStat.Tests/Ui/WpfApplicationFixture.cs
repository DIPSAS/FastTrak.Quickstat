using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;
using Xunit;

// =================================================================================================
//  THE WHOLE ASSEMBLY RUNS SEQUENTIALLY, AND THIS FILE IS THE REASON.
//
//  Once WpfApplicationFixture has run, Application.Current is non-null for the rest of the process -
//  WPF offers no way to un-set it - and that turns two pieces of Application state into shared
//  mutable state touched from every apartment the suite creates:
//
//    * Window's constructor registers each new window with Application.Current - on the
//      application's own list when it was built on the application's thread and on a separate
//      "non-app windows" list otherwise - and Close takes it off again.  Neither collection is
//      synchronised; ViewInstantiationTests.TheApplicationRegistersEveryWindowBuiltOnItsOwnThread
//      measures the first half of that rather than taking it on trust.  Two apartments constructing
//      windows at the same moment is therefore an unsynchronised list mutation, and a list whose
//      recorded count and capacity have been made to disagree does not merely lose an entry: it
//      throws out of every subsequent Add, which is to say out of every subsequent Window
//      constructor in the process.  That is the "breaks every later test" shape that kept
//      PORT-PLAN.md §8.10 (a) open, and it is not hypothetical bookkeeping: Ui/Dialogs builds
//      windows on short-lived apartments throughout.
//    * ShutdownMode.  The fixture sets OnExplicitShutdown (below) so that a window closing on some
//      other thread cannot shut the application down, but that setting is applied a few instructions
//      after the Application exists, and only sequential execution makes that gap unobservable.
//
//  Serialising the assembly removes both, for every test that exists today and every test written
//  later, without anyone having to remember an attribute.  The alternative - a non-parallel
//  collection holding only the classes that build windows - was rejected because it is only correct
//  while that list is complete, and nothing enforces completeness.
//
//  MEASURED COST on this machine: 2249 tests in 5 s in parallel, 8 s sequentially.  Three seconds
//  is not a reason to keep a data race.
// =================================================================================================
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace QuickStat.Tests.Ui;

/// <summary>
/// The one <see cref="Application"/> this test assembly is allowed to own, on the one STA thread
/// that owns it, with the shipped theme merged into <see cref="Application.Resources"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>What it is for.</b> <c>{StaticResource}</c> is resolved while XAML is being <em>parsed</em>,
/// by walking the parser's stack of enclosing objects and then falling back to
/// <see cref="Application.Current"/> and the system theme dictionaries. A view that names a theme
/// key and does not merge the theme into its own <c>Resources</c> therefore cannot be constructed at
/// all without an <see cref="Application"/> - <c>InitializeComponent</c> throws
/// <c>XamlParseException: Cannot find resource named 'QsBorderBrush'</c> before the first assertion
/// could run. Step 3.6's dialogs merge the theme themselves and so need none; the views of steps
/// 3.1, 3.2, 3.3 and 3.4 do not, and until this existed their markup was pinned only structurally,
/// as XML, and proved to load only by launching <c>QuickStat.exe</c> by hand. PORT-PLAN.md §8.10 (a).
/// </para>
/// <para>
/// <b>How "exactly one" is enforced, in three layers.</b> WPF permits one <see cref="Application"/>
/// per <c>AppDomain</c> and throws <c>InvalidOperationException: Cannot create more than one
/// System.Windows.Application instance in the same AppDomain</c> on the second attempt - which,
/// arriving from inside an unrelated test, is exactly the confusing failure this class exists to
/// prevent:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// The <see cref="Application"/> is created by a <c>static readonly</c>
/// <see cref="Lazy{T}"/> with <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/>. That is
/// the actual guarantee: it is per-process, it does not depend on xUnit, and it holds even if two
/// collections, two fixtures or a stray <c>new WpfApplicationFixture()</c> ask for it at once.
/// </description>
/// </item>
/// <item>
/// <description>
/// xUnit is told about it through a single <see cref="ICollectionFixture{TFixture}"/>
/// (<see cref="WpfApplicationCollection"/>), so every test class that wants the application shares
/// one fixture instance and asks for it by constructor injection rather than by reaching for a
/// global.
/// </description>
/// </item>
/// <item>
/// <description>
/// The fixture's constructor touches the <see cref="Lazy{T}"/> eagerly, so a failure to start the
/// apartment is reported once, as a collection-level error naming this file, instead of once per
/// test with a stack trace pointing at whichever view happened to run first.
/// </description>
/// </item>
/// </list>
/// <para>
/// <b>Why the real <c>QuickStat.App</c> rather than a bespoke <see cref="Application"/> subclass.</b>
/// The requirement is that the tests see <em>the shipped theme</em>. <c>App.xaml</c> already states
/// which dictionaries that is and in which order - only <c>QuickStat.Styles.xaml</c>, because it
/// merges <c>QuickStat.Brushes.xaml</c> itself and listing both would give every key two instances -
/// so transcribing that composition here would create a second copy able to drift from the first.
/// Calling the generated <c>InitializeComponent</c> uses the one the product uses, and proves
/// <c>App.xaml</c> loads as a side effect. Nothing else about the application class runs:
/// <c>OnStartup</c> and <c>OnExit</c> are raised by <c>Application.Run</c>, which is never called,
/// so the generic host, the log file and the crash handler stay out of the test process.
/// </para>
/// <para>
/// <b>Why the thread outlives the fixture on purpose.</b> There is no way to un-set
/// <see cref="Application.Current"/>, so tearing the apartment down at the end of the collection
/// would not restore the previous state - it would replace "an application with a live dispatcher",
/// which is what production looks like, with "an application whose dispatcher is dead", which
/// nothing looks like, and <see cref="QuickStat.Services.WpfUiDispatcher"/> would start handing out
/// a dispatcher that throws to anything composed afterwards. The thread is instead a
/// <see cref="Thread.IsBackground"/> one, which is what lets the test host exit: a background thread
/// never keeps a process alive, however deep inside <see cref="Dispatcher.Run"/> it is.
/// </para>
/// <para>
/// <b>Use <see cref="StaTestRunner"/>, not this, for anything that does not need the theme.</b> A
/// throwaway apartment per test keeps those tests independent and orderable; this one is shared
/// state by construction.
/// </para>
/// </remarks>
public sealed class WpfApplicationFixture
{
    /// <summary>
    /// The single application host. <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/> is
    /// the load-bearing argument: the factory runs at most once, whatever asks for it and from
    /// however many threads.
    /// </summary>
    private static readonly Lazy<ApplicationHost> Shared =
        new(ApplicationHost.Start, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Starts the apartment, or fails the whole collection trying.</summary>
    public WpfApplicationFixture() => _ = Shared.Value;

    /// <summary>The application, for assertions about the harness itself.</summary>
    /// <remarks>
    /// Reading a property off it is safe from any thread; calling anything on it is not. Use
    /// <see cref="Run(Action, TimeSpan?)"/> for that.
    /// </remarks>
    public Application Application => Shared.Value.Application;

    /// <summary>Runs <paramref name="body"/> on the application's thread and waits for it.</summary>
    /// <param name="body">The code to run. Every WPF object it creates belongs to that thread.</param>
    /// <param name="timeout">Optional override for <see cref="StaTestRunner.DefaultTimeout"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
    /// <exception cref="TimeoutException">The body did not finish within the timeout.</exception>
    public void Run(Action body, TimeSpan? timeout = null)
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

    /// <summary>Runs <paramref name="body"/> on the application's thread and returns its result.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="body">The code to run.</param>
    /// <param name="timeout">Optional override for <see cref="StaTestRunner.DefaultTimeout"/>.</param>
    /// <returns>Whatever <paramref name="body"/> returned.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
    /// <exception cref="TimeoutException">The body did not finish within the timeout.</exception>
    /// <remarks>
    /// A WPF object must not cross back to the calling thread; return a plain value - a size, a
    /// string, a bool - rather than the element that produced it. The same rule as
    /// <see cref="StaTestRunner"/>, for the same reason, and it bites harder here because the
    /// objects survive the test.
    /// </remarks>
    public T Run<T>(Func<T> body, TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(body);

        return Shared.Value.Run(body, timeout ?? StaTestRunner.DefaultTimeout);
    }

    /// <summary>The apartment: one thread, one dispatcher, one application, for the process.</summary>
    private sealed class ApplicationHost
    {
        private readonly Dispatcher _dispatcher;

        private ApplicationHost(Dispatcher dispatcher, Application application)
        {
            _dispatcher = dispatcher;
            Application = application;
        }

        internal Application Application { get; }

        /// <summary>Creates the thread, the application and the pump, and waits for all three.</summary>
        /// <returns>The started host.</returns>
        /// <exception cref="TimeoutException">The apartment did not come up.</exception>
        internal static ApplicationHost Start()
        {
            using ManualResetEventSlim ready = new(false);

            Dispatcher? dispatcher = null;
            Application? application = null;
            ExceptionDispatchInfo? failure = null;

            Thread thread = new(() =>
            {
                try
                {
                    // The product's own Application subclass, so the merge below is the shipped one.
                    // The constructor is what claims Application.Current; InitializeComponent is
                    // what loads App.xaml into Application.Resources.  Neither runs OnStartup.
                    App started = new();

                    started.InitializeComponent();

                    // MUST be set, and set here.  The default is OnLastWindowClose, and a window
                    // closing on ANY thread makes WPF ask whether the application should now exit;
                    // with no window of its own the answer would be yes, and the apartment would be
                    // torn down under the next test that needed it.
                    started.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                    application = started;
                    dispatcher = Dispatcher.CurrentDispatcher;
                }
                catch (Exception exception)
                {
                    // Rethrowing here would take the test host down instead of failing the
                    // collection; the caller rethrows it with its original stack below.
                    failure = ExceptionDispatchInfo.Capture(exception);
                }
                finally
                {
                    ready.Set();
                }

                if (failure is null)
                {
                    // Never returns.  Dispatcher.Run also installs a DispatcherSynchronizationContext
                    // on this thread, so an "await" inside a marshalled body resumes here rather
                    // than on the thread pool.
                    Dispatcher.Run();
                }
            })
            {
                IsBackground = true,
                Name = "QuickStat test application",
            };

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            if (!ready.Wait(StaTestRunner.DefaultTimeout))
            {
                throw new TimeoutException(
                    "The test application's STA thread did not start within "
                    + $"{StaTestRunner.DefaultTimeout.TotalSeconds:F0} s.");
            }

            failure?.Throw();

            return new ApplicationHost(dispatcher!, application!);
        }

        internal T Run<T>(Func<T> body, TimeSpan timeout)
        {
            T result = default!;
            ExceptionDispatchInfo? failure = null;

            DispatcherOperation operation = _dispatcher.InvokeAsync(() =>
            {
                try
                {
                    result = body();
                }
                catch (Exception exception)
                {
                    // Caught rather than left to fault the operation: an exception escaping a
                    // dispatcher callback also raises Dispatcher.UnhandledException, and the default
                    // there is to rethrow on this thread - which would kill the apartment for every
                    // later test rather than failing this one.
                    failure = ExceptionDispatchInfo.Capture(exception);
                }
                finally
                {
                    // Leave no trace.  A Window constructed on the application's own thread makes
                    // itself Application.MainWindow if there is none, and both
                    // QuickStat.Services.WpfFileDialogService and
                    // QuickStat.Views.Dialogs.DialogOwner read that property to pick an owner.  A
                    // window left there by one test would silently become another test's owner.
                    Application.MainWindow = null;
                }
            });

            if (!operation.Task.Wait(timeout))
            {
                _ = operation.Abort();

                throw new TimeoutException(
                    $"The body did not finish on the application thread within {timeout.TotalSeconds:F0} s.");
            }

            failure?.Throw();

            return result;
        }
    }
}

/// <summary>
/// The xUnit collection that owns <see cref="WpfApplicationFixture"/>.
/// </summary>
/// <remarks>
/// One collection definition, one <see cref="ICollectionFixture{TFixture}"/>, so xUnit constructs
/// the fixture once and hands the same instance to every class marked
/// <c>[Collection(WpfApplicationCollection.Name)]</c>. Declaring a second collection over the same
/// fixture type would defeat that, which is what the <see cref="Lazy{T}"/> inside the fixture is
/// there to survive.
/// </remarks>
[CollectionDefinition(Name)]
public sealed class WpfApplicationCollection : ICollectionFixture<WpfApplicationFixture>
{
    /// <summary>The collection name. Use the constant, never the literal.</summary>
    public const string Name = "WPF application";
}
