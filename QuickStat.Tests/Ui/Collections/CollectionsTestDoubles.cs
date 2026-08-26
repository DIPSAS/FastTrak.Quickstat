using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Collectors;
using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Diagnostics;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;
using QuickStat.Services;
using QuickStat.Tests.Ui.Services;
using QuickStat.ViewModels;

namespace QuickStat.Tests.Ui.Collections;

/// <summary>Builds a <see cref="CollectionsTabViewModel"/> that needs no database.</summary>
/// <remarks>
/// <b>Referenced from <c>Ui/Shell/MainViewModelTests.cs</c>, which step 3.1 owns.</b> Filling in a
/// stub's constructor necessarily breaks whoever was constructing the stub, and the shell harness
/// builds all four tab view-models. Routing it through a factory here keeps that file's diff to the
/// single line that names this method, so the three wave-2 steps that have to change it collide as
/// little as possible.
/// </remarks>
internal static class CollectionsTabHarness
{
    /// <summary>Creates the tab with inert collaborators.</summary>
    /// <param name="workspace">The shared workspace under test.</param>
    /// <param name="identification">The shared identification mode.</param>
    /// <param name="progress">The shared progress service.</param>
    /// <returns>A tab view-model with an empty registry and a runner that never runs.</returns>
    internal static CollectionsTabViewModel Headless(
        IShellWorkspace workspace,
        IIdentificationPolicy identification,
        IShellProgress progress) =>
        new(
            workspace,
            identification,
            new FakeCollectorRegistry(),
            new RecordingCollectorRunner(),
            new FakeSessionService(),
            progress,
            new InlineUiDispatcher(),
            new RecordingUserNotifier(),
            NullLogger<CollectionsTabViewModel>.Instance);
}

/// <summary>A collector that answers with a fixed descriptor and a fixed statement.</summary>
/// <remarks>
/// The Collections tab never looks inside a collector - it finds one by name and hands it to the
/// runner - so a descriptor and a string is the whole surface a view-model test needs. Using the
/// real registry would drag in two round trips and 120 catalogue entries for no gain.
/// </remarks>
internal sealed class StubCollector(string name, string title) : ICollector
{
    public CollectorDescriptor Descriptor { get; } = new()
    {
        Name = name,
        Title = title,
        Kind = CollectorKind.Custom,
        PidBinding = PidBinding.IdList,
    };

    public string BuildSql(CollectorSqlContext context) => "SELECT 1";
}

/// <summary>An <see cref="ICollectorRegistry"/> whose list a test sets outright.</summary>
internal sealed class FakeCollectorRegistry : ICollectorRegistry
{
    /// <summary>What <see cref="BuildAsync"/> will install, in registry order.</summary>
    public List<ICollector> Next { get; } = [];

    /// <inheritdoc />
    public IReadOnlyList<ICollector> Collectors { get; private set; } = [];

    /// <summary>How many times a session asked for the list.</summary>
    public int BuildCount { get; private set; }

    /// <summary>Thrown by <see cref="BuildAsync"/> when set.</summary>
    public Exception? Throws { get; set; }

    /// <summary>Awaited by <see cref="BuildAsync"/> before it answers, when set.</summary>
    public TaskCompletionSource? Gate { get; set; }

    /// <summary>Adds one collector to what the next build will install.</summary>
    /// <param name="name">Collector name.</param>
    /// <param name="title">Collector title.</param>
    /// <returns>This, for chaining.</returns>
    public FakeCollectorRegistry With(string name, string title)
    {
        Next.Add(new StubCollector(name, title));

        return this;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ICollector>> BuildAsync(
        SessionContext session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        BuildCount++;

        if (Gate is not null)
        {
            await Gate.Task.ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (Throws is not null)
        {
            throw Throws;
        }

        Collectors = [.. Next];

        return Collectors;
    }

    /// <inheritdoc />
    public bool TryFind(string nameOrTitle, [NotNullWhen(true)] out ICollector? collector)
    {
        foreach (ICollector candidate in Collectors)
        {
            if (string.Equals(candidate.Descriptor.Name, nameOrTitle, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.Descriptor.Title, nameOrTitle, StringComparison.OrdinalIgnoreCase))
            {
                collector = candidate;

                return true;
            }
        }

        collector = null;

        return false;
    }
}

/// <summary>
/// An <see cref="ICollectorRunner"/> that writes the columns a test asked for and records the order
/// it was called in.
/// </summary>
/// <remarks>
/// The order is the point. It is the check list walked from index 0, and therefore the column order
/// of every exported file (PORT-PLAN.md §6).
/// </remarks>
internal sealed class RecordingCollectorRunner : ICollectorRunner
{
    private readonly Dictionary<string, string[]> _columns = new(StringComparer.Ordinal);

    /// <summary>Collector names, in the order they ran.</summary>
    public List<string> Ran { get; } = [];

    /// <summary>The cohort each run was given.</summary>
    public List<IReadOnlyList<int>> Cohorts { get; } = [];

    /// <summary>The study id each run was given.</summary>
    public List<int> StudyIds { get; } = [];

    /// <summary>Called before each run, with the collector name. Somewhere to assert live state.</summary>
    public Action<string>? Observe { get; set; }

    /// <summary>Thrown by the run of this collector, when set.</summary>
    public string? ThrowFor { get; set; }

    /// <summary>What <see cref="ThrowFor"/> throws.</summary>
    public Exception Failure { get; set; } = new InvalidOperationException("The collector failed.");

    /// <summary>Declares the columns one collector produces.</summary>
    /// <param name="collectorName">The collector.</param>
    /// <param name="columnNames">Its columns, in the order the matrix should add them.</param>
    /// <returns>This, for chaining.</returns>
    public RecordingCollectorRunner Producing(string collectorName, params string[] columnNames)
    {
        _columns[collectorName] = columnNames;

        return this;
    }

    /// <inheritdoc />
    public Task<CollectorRunSummary> RunAsync(
        ICollector collector,
        IReadOnlyList<int> personIds,
        int studyId,
        ICollectorResultSink sink,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(collector);
        ArgumentNullException.ThrowIfNull(personIds);
        ArgumentNullException.ThrowIfNull(sink);

        cancellationToken.ThrowIfCancellationRequested();

        string name = collector.Descriptor.Name;

        Ran.Add(name);
        Cohorts.Add(personIds);
        StudyIds.Add(studyId);

        Observe?.Invoke(name);

        if (string.Equals(ThrowFor, name, StringComparison.Ordinal))
        {
            throw Failure;
        }

        progress?.Report(new OperationProgress("Collecting data", collector.Descriptor.Title + " (1/1)", 0));

        VariableNameSet variableNames = sink.CreateVariableNameSet();

        foreach (string columnName in _columns.TryGetValue(name, out string[]? declared) ? declared : [name])
        {
            variableNames.Add(columnName);

            foreach (int personId in personIds)
            {
                _ = sink.Add(columnName, new CollectorResultRow
                {
                    PersonId = personId,
                    VarName = columnName,
                    Value = personId,
                    Timestamp = new DateTime(2019, 8, 14, 0, 0, 0, DateTimeKind.Unspecified),
                    RowId = 1,
                });
            }
        }

        return Task.FromResult(new CollectorRunSummary
        {
            Descriptor = collector.Descriptor,
            VariableNames = variableNames,
            RowsAccepted = variableNames.Count * personIds.Count,
            BatchCount = 1,
        });
    }
}

/// <summary>An <see cref="ISessionService"/> a test drives by hand.</summary>
internal sealed class FakeSessionService : ISessionService
{
    /// <inheritdoc />
    public event EventHandler<SessionContext?>? SessionChanged;

    /// <inheritdoc />
    public SessionContext? Current { get; private set; }

    /// <inheritdoc />
    public bool IsConnected => Current is not null;

    /// <summary>Builds a session that is enough for the registry and the study id.</summary>
    /// <param name="studyName">Study short name.</param>
    /// <param name="studyId">Study id.</param>
    /// <returns>The session.</returns>
    public static SessionContext NewSession(string studyName = "KORTTID", int studyId = 42) => new()
    {
        StudyName = studyName,
        StudyId = studyId,
        SessionId = 1,
        User = new StudyUser { UserId = 1, UserName = "test" },
        Database = new DatabaseInfo(),
        ServerName = "SERVER",
        DatabaseName = "DB",
    };

    /// <summary>Raises <see cref="SessionChanged"/>, as a successful login does.</summary>
    /// <param name="session">The new session, or <see langword="null"/> for a disconnect.</param>
    public void Raise(SessionContext? session)
    {
        Current = session;

        SessionChanged?.Invoke(this, session);
    }

    /// <inheritdoc />
    public Task<SessionContext> ConnectAsync(
        QuickStatConnection connection,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("The Collections tab never connects; it only listens.");

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        Raise(null);

        return Task.CompletedTask;
    }
}

/// <summary>An <see cref="IUserNotifier"/> that records rather than showing anything.</summary>
internal sealed class RecordingUserNotifier : IUserNotifier
{
    /// <summary>Everything shown, prefixed with its severity.</summary>
    public List<string> Messages { get; } = [];

    /// <summary>What <see cref="ConfirmAsync"/> answers.</summary>
    public bool ConfirmAnswer { get; set; }

    public Task InformAsync(string message, string? title = null) => Record("info", message);

    public Task WarnAsync(string message, string? title = null) => Record("warn", message);

    public Task ErrorAsync(string message, string? title = null) => Record("error", message);

    public Task<bool> ConfirmAsync(
        string message,
        NotificationSeverity severity = NotificationSeverity.Warning,
        string? title = null)
    {
        Messages.Add("confirm: " + message);

        return Task.FromResult(ConfirmAnswer);
    }

    private Task Record(string severity, string message)
    {
        Messages.Add(severity + ": " + message);

        return Task.CompletedTask;
    }
}
