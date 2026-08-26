using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Diagnostics;
using QuickStat.Domain.Matrix;
using QuickStat.Domain.Patients;
using QuickStat.Domain.Populations;
using QuickStat.Services;
using QuickStat.Tests.Ui.Services;
using QuickStat.ViewModels;

namespace QuickStat.Tests.Ui.Populations;

/// <summary>Stand-ins for everything step 3.2's two view-models talk to.</summary>
/// <remarks>
/// <para>
/// No database is available to the suite (PORT-PLAN.md §9 R9), so every collaborator that would
/// reach one is scripted here. Owned by step 3.2; the shell's own doubles live in
/// <c>Ui/Services/ShellTestDoubles.cs</c> and are not duplicated.
/// </para>
/// <para>
/// <b>The folder is <c>Ui/Populations</c>, not <c>Ui/Population</c>, and it has to be.</b> A
/// namespace <c>QuickStat.Tests.Ui.Population</c> shadows the type
/// <see cref="QuickStat.Domain.Populations.Population"/> for every unqualified use anywhere under
/// <c>QuickStat.Tests.Ui</c> - <c>CS0118: 'Population' is a namespace but is used like a type</c> -
/// which breaks <c>Ui/Services/ShellWorkspaceTests.cs</c>. It is the same trap PORT-PLAN.md §5
/// records for <c>Controls/MatrixGrid/</c>.
/// </para>
/// </remarks>
internal static class PopulationTestDoubles
{
    /// <summary>
    /// A fully wired <see cref="PopulationTabViewModel"/> over fakes, for callers that only need one
    /// to exist.
    /// </summary>
    /// <returns>The tab view-model.</returns>
    /// <remarks>
    /// Exists so that <c>Ui/Shell/MainViewModelTests</c> - step 3.1's file, which builds all four
    /// wave-2 view-models by hand - needs one short line rather than fifteen.
    /// </remarks>
    internal static PopulationTabViewModel NewTabViewModel() => new PopulationHarness().Tab;

    /// <summary>
    /// A fully wired <see cref="PopulationPickerViewModel"/> over fakes, for callers that only need
    /// one to exist.
    /// </summary>
    /// <returns>The picker view-model.</returns>
    /// <remarks>
    /// Same purpose as <see cref="NewTabViewModel"/>, for <c>Ui/Packages</c>: step 3.4's tests hand a
    /// picker to <c>PackagesTabViewModel</c> without driving it, and were written against the
    /// parameterless stub this step replaced.
    /// </remarks>
    internal static PopulationPickerViewModel NewPickerViewModel() => new PopulationHarness().Picker;

    /// <summary>A catalogue row.</summary>
    /// <param name="procId">The id.</param>
    /// <param name="title">The bold main text.</param>
    /// <param name="group">The right-aligned category.</param>
    /// <param name="helpText">The wrapped description.</param>
    /// <param name="sourceCode">The <c>CREATE PROCEDURE</c> text.</param>
    /// <returns>The population.</returns>
    internal static Population NewPopulation(
        int procId,
        string title,
        string group = "",
        string helpText = "",
        string sourceCode = "") => new()
        {
            ProcId = procId,
            Title = title,
            QueryText = "EXEC dbo.GetCaseList :StudyId",
            Group = group,
            HelpText = helpText,
            SourceCode = sourceCode,
        };

    /// <summary>A session with a usable study.</summary>
    /// <param name="studyId">The study.</param>
    /// <param name="dbVersion">The schema version that picks the catalogue procedure.</param>
    /// <returns>The session.</returns>
    internal static SessionContext NewSession(int studyId = 7, int dbVersion = 18200) => new()
    {
        StudyName = "NDV",
        StudyId = studyId,
        SessionId = 42,
        User = new StudyUser { UserId = 3, UserName = "tester" },
        Database = new DatabaseInfo { DbVersion = dbVersion },
        ServerName = "SERVER",
        DatabaseName = "FastTrak",
    };

    /// <summary>One <c>&lt;Connection&gt;</c> entry.</summary>
    /// <param name="name">The display name the combo box sorts on.</param>
    /// <returns>The connection.</returns>
    internal static QuickStatConnection NewConnection(string name) => new()
    {
        Name = name,
        StudyName = "NDV",
        ConnectionString = @"FILE NAME=.\FastTrak.UDL",
    };
}

/// <summary>A catalogue that answers from memory and records what it was asked.</summary>
internal sealed class FakePopulationRepository : IPopulationRepository
{
    internal List<Population> Catalogue { get; set; } = [];

    internal List<Population> FrequentlyUsed { get; set; } = [];

    internal List<(int StudyId, int DbVersion, bool FrequentlyUsedOnly)> Requests { get; } = [];

    internal List<(int StudyId, int ProcId, string Title, long Elapsed)> AuditRows { get; } = [];

    internal Exception? Throws { get; set; }

    public Task<IReadOnlyList<Population>> GetPopulationsAsync(
        int studyId,
        int dbVersion,
        bool frequentlyUsedOnly,
        CancellationToken cancellationToken = default)
    {
        Requests.Add((studyId, dbVersion, frequentlyUsedOnly));

        cancellationToken.ThrowIfCancellationRequested();

        if (Throws is not null)
        {
            throw Throws;
        }

        return Task.FromResult<IReadOnlyList<Population>>(frequentlyUsedOnly ? FrequentlyUsed : Catalogue);
    }

    public Task LogPopulationSelectedAsync(
        int studyId,
        int procId,
        string procTitle,
        long elapsedMilliseconds,
        CancellationToken cancellationToken = default)
    {
        AuditRows.Add((studyId, procId, procTitle, elapsedMilliseconds));

        return Task.CompletedTask;
    }
}

/// <summary>A patient repository that hands back whatever the test put in it.</summary>
internal sealed class FakePatientRepository : IPatientRepository
{
    internal List<Patient> Cohort { get; set; } = [];

    internal List<Population> Loaded { get; } = [];

    internal IReadOnlyDictionary<string, object?>? LastParameters { get; private set; }

    internal Exception? Throws { get; set; }

    public Task<IReadOnlyList<Patient>> LoadPopulationAsync(
        Population population,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(population);

        Loaded.Add(population);
        LastParameters = parameters;

        cancellationToken.ThrowIfCancellationRequested();

        if (Throws is not null)
        {
            throw Throws;
        }

        return Task.FromResult<IReadOnlyList<Patient>>(Cohort);
    }

    public Task<IReadOnlyList<Patient>> GetCaseListAsync(int studyId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Patient>>([]);

    public Task<IReadOnlyDictionary<int, string>> GetNationalIdsAsync(
        IReadOnlyCollection<int> personIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<int, string>>(new Dictionary<int, string>());

    public Task<IReadOnlyList<Patient>> SearchAsync(
        int studyId,
        string searchText,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Patient>>([]);
}

/// <summary>A resolver with a scripted answer: success, user cancel, or a named failure.</summary>
internal sealed class FakeParameterResolver : IQueryParameterResolver
{
    internal ParameterResolution Answer { get; set; } = new()
    {
        Succeeded = true,
        Values = new Dictionary<string, object?> { ["StudyId"] = 7 },
    };

    internal List<string> Resolved { get; } = [];

    public Task<ParameterResolution> ResolveAsync(string sqlText, CancellationToken cancellationToken = default)
    {
        Resolved.Add(sqlText);

        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(Answer);
    }
}

/// <summary>A session service whose current session a test sets directly.</summary>
internal sealed class FakeSessionService : ISessionService
{
    public event EventHandler<SessionContext?>? SessionChanged;

    public SessionContext? Current { get; private set; }

    public bool IsConnected => Current is not null;

    internal int DisconnectCount { get; private set; }

    /// <summary>Sets the session and raises <see cref="SessionChanged"/>, as a real login does.</summary>
    /// <param name="session">The new session, or <see langword="null"/> for a disconnect.</param>
    internal void Change(SessionContext? session)
    {
        Current = session;

        SessionChanged?.Invoke(this, session);
    }

    public Task<SessionContext> ConnectAsync(
        QuickStatConnection connection,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        SessionContext session = PopulationTestDoubles.NewSession();

        Change(session);

        return Task.FromResult(session);
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        DisconnectCount++;

        Change(null);

        return Task.CompletedTask;
    }
}

/// <summary>A connection coordinator that records rather than connecting.</summary>
internal sealed class FakeConnectionCoordinator : IConnectionCoordinator
{
    private readonly FakeSessionService? _session;

    internal FakeConnectionCoordinator(FakeSessionService? session = null) => _session = session;

    internal List<QuickStatConnection> Connected { get; } = [];

    internal int DisconnectCount { get; private set; }

    internal Exception? Throws { get; set; }

    public Task<SessionContext> ConnectAsync(
        QuickStatConnection connection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        Connected.Add(connection);

        cancellationToken.ThrowIfCancellationRequested();

        if (Throws is not null)
        {
            throw Throws;
        }

        SessionContext session = PopulationTestDoubles.NewSession();

        _session?.Change(session);

        return Task.FromResult(session);
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        DisconnectCount++;

        _session?.Change(null);

        return Task.CompletedTask;
    }
}

/// <summary>A connection catalogue backed by a list instead of a file.</summary>
internal sealed class FakeConnectionCatalog : IConnectionCatalog
{
    internal List<QuickStatConnection> Connections { get; set; } = [];

    internal Exception? Throws { get; set; }

    internal List<string> RequestedPaths { get; } = [];

    public string DefaultConfigFilePath => @"C:\deployed\QuickStat.config.xml";

    public IReadOnlyList<QuickStatConnection> Load(string configFilePath)
    {
        RequestedPaths.Add(configFilePath);

        if (Throws is not null)
        {
            throw Throws;
        }

        return Connections;
    }
}

/// <summary>An <see cref="IUserNotifier"/> that records instead of showing anything.</summary>
internal sealed class RecordingUserNotifier : IUserNotifier
{
    internal List<string> Informations { get; } = [];

    internal List<string> Warnings { get; } = [];

    internal List<string> Errors { get; } = [];

    internal List<string> Questions { get; } = [];

    internal bool ConfirmationAnswer { get; set; }

    public Task InformAsync(string message, string? title = null)
    {
        Informations.Add(message);

        return Task.CompletedTask;
    }

    public Task WarnAsync(string message, string? title = null)
    {
        Warnings.Add(message);

        return Task.CompletedTask;
    }

    public Task ErrorAsync(string message, string? title = null)
    {
        Errors.Add(message);

        return Task.CompletedTask;
    }

    public Task<bool> ConfirmAsync(
        string message,
        NotificationSeverity severity = NotificationSeverity.Warning,
        string? title = null)
    {
        Questions.Add(message);

        return Task.FromResult(ConfirmationAnswer);
    }
}

/// <summary>Everything step 3.2 owns, wired together over fakes.</summary>
internal sealed class PopulationHarness : IDisposable
{
    internal PopulationHarness()
    {
        Matrix = ShellWorkspaceTests.NewMatrix();
        Workspace = new ShellWorkspace(Matrix);
        Progress = new ShellProgress(new InlineUiDispatcher());
        Settings = new InMemorySettingsStore();
        WindowState = new WindowStateService(
            Settings,
            new FakeMonitorLayout(new System.Windows.Rect(0, 0, 1920, 1040)),
            NullLogger<WindowStateService>.Instance);
        Coordinator = new FakeConnectionCoordinator(Session);

        Picker = new PopulationPickerViewModel(
            Catalogue,
            Patients,
            Parameters,
            Session,
            Workspace,
            Progress,
            new InlineUiDispatcher(),
            Notifier,
            NullLogger<PopulationPickerViewModel>.Instance);

        Tab = new PopulationTabViewModel(
            Picker,
            Connections,
            Coordinator,
            WindowState,
            Notifier,
            NullLogger<PopulationTabViewModel>.Instance);
    }

    internal PersonMatrix Matrix { get; }

    internal ShellWorkspace Workspace { get; }

    internal ShellProgress Progress { get; }

    internal InMemorySettingsStore Settings { get; }

    internal WindowStateService WindowState { get; }

    internal FakePopulationRepository Catalogue { get; } = new();

    internal FakePatientRepository Patients { get; } = new();

    internal FakeParameterResolver Parameters { get; } = new();

    internal FakeSessionService Session { get; } = new();

    internal FakeConnectionCatalog Connections { get; } = new();

    internal FakeConnectionCoordinator Coordinator { get; }

    internal RecordingUserNotifier Notifier { get; } = new();

    internal PopulationPickerViewModel Picker { get; }

    internal PopulationTabViewModel Tab { get; }

    /// <summary>Fills the catalogue and opens a session, exactly as a connect does.</summary>
    /// <param name="populations">The catalogue rows.</param>
    /// <returns>A task that completes when the picker has been refilled.</returns>
    internal Task ConnectAsync(params Population[] populations)
    {
        Catalogue.Catalogue = [.. populations];

        Session.Change(PopulationTestDoubles.NewSession());

        return WaitForCatalogueAsync();
    }

    /// <summary>Lets the fire-and-forget catalogue load finish.</summary>
    /// <returns>A task that completes when no load is in flight.</returns>
    /// <remarks>
    /// The load is started from <c>SessionChanged</c> and from the <c>Frequently used only</c> box,
    /// neither of which can hand a task back to its caller. Every collaborator here completes
    /// synchronously, so one yield is enough; the loop is there so a slower fake cannot make the
    /// suite flaky.
    /// </remarks>
    internal async Task WaitForCatalogueAsync()
    {
        for (int attempt = 0; attempt < 100 && Picker.IsLoadingCatalogue; attempt++)
        {
            await Task.Yield();
        }
    }

    public void Dispose() => Picker.Dispose();
}
