using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Collectors;
using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Diagnostics;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;
using QuickStat.Services;
using QuickStat.Tests.Ui.Collections;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Ui.Services;

/// <summary>
/// The whole of <c>SelectConnection</c> (<c>MainQuickStat.pas:495-519</c>): the order of the steps,
/// what a failure in each of them means, and what cancelling does.
/// </summary>
/// <remarks>
/// <para>
/// Most of these exist because of PORT-PLAN.md §8.10 (g). The collector build used to hang off
/// <c>ISessionService.SessionChanged</c> and run unawaited, so "connected" meant a session existed
/// and said nothing about whether there was anything to collect; three of the cases below are
/// exactly the states that made reachable.
/// </para>
/// <para>
/// The Delphi has no such window: <c>fQuickStat.PrepareStudy</c> is called from
/// <c>TfrmQuickStat.AfterLogin</c> (<c>MainQuickStat.pas:480</c>), a login observer that
/// <c>TSimpleDatabase.Connect</c> invokes synchronously in its own loop
/// (<c>Emetra.Database.Simple.pas:391-406</c>).
/// </para>
/// </remarks>
public class ConnectionCoordinatorTests
{
    private static readonly QuickStatConnection Project = new()
    {
        Name = "Testdatabase (NDV)",
        StudyName = "NDV",
        ConnectionString = @"FILE NAME=.\FastTrak.UDL",
    };

    // ------------------------------------------------------------------- the order of the steps

    [Fact]
    public async Task ConnectingDoesNotFinishUntilTheCollectorListIsReady()
    {
        Harness harness = new();

        harness.Registry.With("A", "Alfa");
        harness.Registry.Gate = new TaskCompletionSource();

        Task<SessionContext> connect = harness.Coordinator.ConnectAsync(Project);

        // The login has happened and SessionChanged has been raised; the two round trips behind the
        // data-element list have not come back.  This is the window the item was about.
        Assert.False(connect.IsCompleted);

        harness.Registry.Gate.SetResult();

        _ = await connect;

        Assert.Equal(["A"], harness.Registry.Collectors.Select(collector => collector.Descriptor.Name));
    }

    [Fact]
    public async Task TheStepsRunInTheOrderAfterLoginRunsThem()
    {
        Harness harness = new();

        _ = await harness.Coordinator.ConnectAsync(Project);

        // Login, then captions, then the list.  The captions are not in the Delphi's AfterLogin at
        // all - AddCaptions is called from actCollectDataExecute (MainQuickStat.pas:649) and step
        // 2.5 moved it here - so they go first and leave "Loading collectors" as the last thing the
        // status line says before Task completed, which is what PrepareStudy does
        // (QuickStat.Collectors.pas:125).
        Assert.Equal(["login", "captions", "collectors"], harness.Steps);
    }

    [Fact]
    public async Task TheListIsBuiltForTheSessionThatWasJustEstablished()
    {
        Harness harness = new();

        SessionContext session = await harness.Coordinator.ConnectAsync(Project);

        Assert.Same(session, Assert.Single(harness.Registry.Sessions));
    }

    [Fact]
    public async Task TheStatusLineSaysLoadingCollectorsAndThenTaskCompleted()
    {
        Harness harness = new();

        List<string> info = [];

        harness.Progress.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ShellProgress.Info))
            {
                info.Add(harness.Progress.Info);
            }
        };

        _ = await harness.Coordinator.ConnectAsync(Project);

        Assert.Equal(
            [
                ConnectionCoordinator.ProjectSelectedText,
                "Connecting to Testdatabase (NDV) ...",
                ConnectionCoordinator.LoadingCollectorsText,
                ShellProgress.CompletedText,
            ],
            info);

        Assert.False(harness.Progress.IsBusy);
    }

    [Fact]
    public async Task TheCheckListIsFilledBeforeTheConnectHandsTheSessionBack()
    {
        // The point of the whole item, one step further out: not just the registry but the list the
        // user sees and the package replay walks (PackagesTabViewModel.ApplyCollectorSelectionAsync
        // reads DataElements, and a package replayed against an empty one reports every stored
        // element as unknown and collects nothing).  The Collections tab hears about the build
        // through ICollectorRegistry.Rebuilt, which is raised inside BuildAsync, so this is settled
        // by the time the await below returns rather than by whichever handler happened to be quick.
        Harness harness = new();

        harness.Registry.With("ALDER", "^ Alder");
        harness.Registry.Gate = new TaskCompletionSource();

        ShellWorkspace workspace = new(ShellWorkspaceTests.NewMatrix());

        using CollectionsTabViewModel collections = new(
            workspace,
            new IdentificationPolicy(),
            harness.Registry,
            new RecordingCollectorRunner(),
            harness.Session,
            harness.Progress,
            new InlineUiDispatcher(),
            new RecordingUserNotifier(),
            NullLogger<CollectionsTabViewModel>.Instance);

        Task<SessionContext> connect = harness.Coordinator.ConnectAsync(Project);

        // The session exists and the tab has been told; the elements have not arrived. A connect
        // that reported success here is the state the item describes.
        Assert.False(connect.IsCompleted);
        Assert.Empty(collections.DataElements);

        harness.Registry.Gate.SetResult();

        _ = await connect;

        Assert.Equal(["ALDER"], collections.DataElements.Select(element => element.Name));
        Assert.Equal(42, workspace.Matrix.StudyId);
    }

    // ------------------------------------------------------------------------ failure semantics

    [Fact]
    public async Task AFailedCollectorBuildFailsTheConnect()
    {
        // It used to be swallowed into a log line and a red status line that the coordinator's own
        // Task completed then raced to overwrite - the build fails on its first round trip, the
        // caption load is a whole query slower, so "Task completed" usually won.  The user was told
        // the project had opened and given an empty list.
        Harness harness = new();

        harness.Registry.Throws = new InvalidOperationException("Report.GetFormClasses is missing.");

        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Coordinator.ConnectAsync(Project));

        Assert.Equal("Report.GetFormClasses is missing.", failure.Message);
        Assert.True(harness.Progress.IsError);
        Assert.Equal("Report.GetFormClasses is missing.", harness.Progress.Info);
        Assert.False(harness.Progress.IsBusy);
    }

    [Fact]
    public async Task AFailedCollectorBuildLeavesTheSessionOpen()
    {
        // Docs/Port/01-data-access.md §1.6: a throwing login observer aborts Connect, but nothing
        // rolls the ADO connection back, so fCrfContext.Connected still answers true.  The port
        // agrees, deliberately - the login pipeline did finish, the session row is open and the
        // population list works.  It is the connect that failed, not the connection.
        Harness harness = new();

        harness.Registry.Throws = new InvalidOperationException("boom");

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => harness.Coordinator.ConnectAsync(Project));

        Assert.True(harness.Session.IsConnected);
    }

    // ------------------------------------------------------------ cancellation and re-entrancy

    [Fact]
    public async Task CancellingTheConnectCancelsTheCollectorBuild()
    {
        // The abandoned task was started with CancellationToken.None, so nothing could call it off.
        Harness harness = new();
        using CancellationTokenSource cancellation = new();

        harness.Registry.Gate = new TaskCompletionSource();

        Task<SessionContext> connect = harness.Coordinator.ConnectAsync(Project, cancellation.Token);

        await cancellation.CancelAsync();

        harness.Registry.Gate.SetResult();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connect);

        Assert.Empty(harness.Registry.Collectors);
        Assert.Equal(ShellProgress.IdleText, harness.Progress.Info);
    }

    [Fact]
    public async Task ASecondConnectCancelsTheFirstRatherThanRacingIt()
    {
        // Two builds in flight write the same registry and the same check list, and the loser can
        // land last - a list belonging to the project the user just left.  The Delphi cannot reach
        // this: SelectConnection holds the message loop for the whole login.  Here the busy overlay
        // blocks only the mouse (PORT-PLAN.md §8.10 (f)), so the drop-down is still reachable.
        Harness harness = new();

        harness.Registry.With("FIRST", "First");
        harness.Registry.Gate = new TaskCompletionSource();

        Task<SessionContext> first = harness.Coordinator.ConnectAsync(Project);

        harness.Registry.Next.Clear();
        harness.Registry.With("SECOND", "Second");
        harness.Registry.Gate = new TaskCompletionSource();

        Task<SessionContext> second = harness.Coordinator.ConnectAsync(Project with { Name = "Other" });

        harness.Registry.Gate.SetResult();

        _ = await second;
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);

        Assert.Equal(["SECOND"], harness.Registry.Collectors.Select(collector => collector.Descriptor.Name));
    }

    [Fact]
    public async Task DisconnectingCancelsAConnectThatIsStillRunning()
    {
        Harness harness = new();

        harness.Registry.With("A", "Alfa");
        harness.Registry.Gate = new TaskCompletionSource();

        Task<SessionContext> connect = harness.Coordinator.ConnectAsync(Project);

        await harness.Coordinator.DisconnectAsync();

        harness.Registry.Gate.SetResult();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connect);

        Assert.Empty(harness.Registry.Collectors);
    }

    [Fact]
    public async Task DisconnectingWithNothingInFlightStillDisconnects()
    {
        Harness harness = new();

        _ = await harness.Coordinator.ConnectAsync(Project);

        await harness.Coordinator.DisconnectAsync();

        Assert.False(harness.Session.IsConnected);
    }

    /// <summary>The coordinator over fakes for each of its four collaborators.</summary>
    private sealed class Harness
    {
        internal Harness()
        {
            Progress = new ShellProgress(new InlineUiDispatcher());
            Session = new ConnectingSessionService(Steps);
            Captions = new RecordingCaptionLoader(Steps);
            Registry = new RecordingCollectorRegistry(Steps);

            Coordinator = new ConnectionCoordinator(
                Session,
                Captions,
                Registry,
                Progress,
                NullLogger<ConnectionCoordinator>.Instance);
        }

        /// <summary>The names of the steps that ran, in the order they ran.</summary>
        internal List<string> Steps { get; } = [];

        internal ShellProgress Progress { get; }

        internal ConnectingSessionService Session { get; }

        internal RecordingCaptionLoader Captions { get; }

        internal RecordingCollectorRegistry Registry { get; }

        internal ConnectionCoordinator Coordinator { get; }
    }

    /// <summary>An <see cref="ISessionService"/> that logs in without a database.</summary>
    /// <remarks>
    /// The two <c>FakeSessionService</c>s the tab tests use both throw from <c>ConnectAsync</c> -
    /// they only need the event - and the point here is the sequence <em>around</em> the login.
    /// </remarks>
    private sealed class ConnectingSessionService(List<string> steps) : ISessionService
    {
        public event EventHandler<SessionContext?>? SessionChanged;

        public SessionContext? Current { get; private set; }

        public bool IsConnected => Current is not null;

        public Task<SessionContext> ConnectAsync(
            QuickStatConnection connection,
            IProgress<OperationProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(connection);

            cancellationToken.ThrowIfCancellationRequested();

            steps.Add("login");

            Current = new SessionContext
            {
                StudyName = connection.StudyName,
                StudyId = 42,
                SessionId = 1,
                User = new StudyUser { UserId = 1, UserName = "chs" },
                Database = new DatabaseInfo(),
                ServerName = "sql01",
                DatabaseName = "FastTrak",
            };

            SessionChanged?.Invoke(this, Current);

            return Task.FromResult(Current);
        }

        public Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            steps.Add("disconnect");

            if (Current is not null)
            {
                Current = null;

                SessionChanged?.Invoke(this, null);
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>An <see cref="ICaptionLoader"/> that records that it ran.</summary>
    private sealed class RecordingCaptionLoader(List<string> steps) : ICaptionLoader
    {
        public Task<int> LoadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            steps.Add("captions");

            return Task.FromResult(3);
        }
    }

    /// <summary>
    /// <see cref="FakeCollectorRegistry"/> plus the two things this file asks it: when it ran, and
    /// which session it was handed.
    /// </summary>
    private sealed class RecordingCollectorRegistry(List<string> steps) : ICollectorRegistry
    {
        private readonly FakeCollectorRegistry _inner = new();

        public event EventHandler<IReadOnlyList<ICollector>>? Rebuilt
        {
            add => _inner.Rebuilt += value;
            remove => _inner.Rebuilt -= value;
        }

        /// <summary>Every session a build was asked for, in order.</summary>
        public List<SessionContext> Sessions { get; } = [];

        public IReadOnlyList<ICollector> Collectors => _inner.Collectors;

        public List<ICollector> Next => _inner.Next;

        public Exception? Throws
        {
            get => _inner.Throws;
            set => _inner.Throws = value;
        }

        public TaskCompletionSource? Gate
        {
            get => _inner.Gate;
            set => _inner.Gate = value;
        }

        public RecordingCollectorRegistry With(string name, string title)
        {
            _ = _inner.With(name, title);

            return this;
        }

        public Task<IReadOnlyList<ICollector>> BuildAsync(
            SessionContext session,
            CancellationToken cancellationToken = default)
        {
            steps.Add("collectors");
            Sessions.Add(session);

            return _inner.BuildAsync(session, cancellationToken);
        }

        public bool TryFind(string nameOrTitle, [NotNullWhen(true)] out ICollector? collector) =>
            _inner.TryFind(nameOrTitle, out collector);
    }
}
