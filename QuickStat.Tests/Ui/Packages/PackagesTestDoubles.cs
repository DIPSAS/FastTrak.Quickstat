using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Diagnostics;
using QuickStat.Domain.Packages;
using QuickStat.Domain.Patients;
using QuickStat.Domain.Populations;

namespace QuickStat.Tests.Ui.Packages;

/// <summary>Builds the session objects the Packages tab needs, with nothing else filled in.</summary>
internal static class FakeSession
{
    /// <summary>A session that only carries a study id, which is all this tab reads.</summary>
    /// <param name="studyId">The study.</param>
    /// <returns>A minimal but valid session.</returns>
    internal static SessionContext ForStudy(int studyId = 124) => new()
    {
        StudyName = "Tarmscreening",
        StudyId = studyId,
        SessionId = 1,
        User = new StudyUser { UserId = 7, UserName = "chs" },
        Database = new DatabaseInfo(),
        ServerName = "sql01",
        DatabaseName = "FastTrak",
    };
}

/// <summary>An <see cref="ISessionService"/> whose current session a test sets and announces.</summary>
/// <remarks>
/// Distinct from <c>QuickStat.Tests.Domain.Populations.StubSessionService</c>, which cannot raise
/// <see cref="SessionChanged"/> at all - and that event is what makes the packages list load after
/// login, so it has to be raisable here.
/// </remarks>
internal sealed class FakeSessionService : ISessionService
{
    public SessionContext? Current { get; private set; }

    public bool IsConnected => Current is not null;

    public event EventHandler<SessionContext?>? SessionChanged;

    /// <summary>Sets <see cref="Current"/> and raises <see cref="SessionChanged"/>, like a login.</summary>
    /// <param name="session">The new session, or <see langword="null"/> for a disconnect.</param>
    public void Announce(SessionContext? session)
    {
        Current = session;

        SessionChanged?.Invoke(this, session);
    }

    /// <summary>Sets <see cref="Current"/> without telling anybody.</summary>
    /// <param name="session">The session to install.</param>
    public void SetSilently(SessionContext? session) => Current = session;

    public Task<SessionContext> ConnectAsync(
        QuickStatConnection connection,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>An <see cref="IPackageRepository"/> backed by a list instead of <c>Report.QuickStat</c>.</summary>
internal sealed class FakePackageRepository : IPackageRepository
{
    private int _nextRowId = 100;

    /// <summary>What <see cref="GetPackagesAsync"/> returns, in the order it returns them.</summary>
    public List<PackagedSelection> Stored { get; } = [];

    /// <summary>The study ids <see cref="GetPackagesAsync"/> was called with.</summary>
    public List<int> LoadedStudyIds { get; } = [];

    /// <summary>Everything handed to <see cref="SaveAsync"/>, before the row id was assigned.</summary>
    public List<PackagedSelection> Saved { get; } = [];

    /// <summary>Every row id handed to <see cref="DeleteAsync"/>.</summary>
    public List<int> Deleted { get; } = [];

    /// <summary>When set, every call fails with it.</summary>
    public Exception? Throws { get; set; }

    public Task<IReadOnlyList<PackagedSelection>> GetPackagesAsync(
        int studyId,
        CancellationToken cancellationToken = default)
    {
        LoadedStudyIds.Add(studyId);

        return Throws is not null
            ? Task.FromException<IReadOnlyList<PackagedSelection>>(Throws)
            : Task.FromResult<IReadOnlyList<PackagedSelection>>([.. Stored]);
    }

    public Task<PackagedSelection> SaveAsync(
        PackagedSelection package,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);

        Saved.Add(package);

        if (Throws is not null)
        {
            return Task.FromException<PackagedSelection>(Throws);
        }

        PackagedSelection withRowId = package with { RowId = _nextRowId++ };

        Stored.Add(withRowId);

        return Task.FromResult(withRowId);
    }

    public Task DeleteAsync(int rowId, CancellationToken cancellationToken = default)
    {
        Deleted.Add(rowId);

        if (Throws is not null)
        {
            return Task.FromException(Throws);
        }

        Stored.RemoveAll(package => package.RowId == rowId);

        return Task.CompletedTask;
    }
}

/// <summary>An <see cref="IPatientRepository"/> that hands back a cohort a test chose.</summary>
internal sealed class FakePatientRepository : IPatientRepository
{
    /// <summary>The cohort every population load returns.</summary>
    public List<Patient> Cohort { get; } = [];

    /// <summary>The populations <see cref="LoadPopulationAsync"/> was asked for, in order.</summary>
    public List<Population> Loaded { get; } = [];

    /// <summary>The parameter values of the most recent load.</summary>
    public IReadOnlyDictionary<string, object?>? LastParameters { get; private set; }

    /// <summary>When set, <see cref="LoadPopulationAsync"/> fails with it.</summary>
    public Exception? Throws { get; set; }

    public Task<IReadOnlyList<Patient>> LoadPopulationAsync(
        Population population,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(population);

        Loaded.Add(population);
        LastParameters = parameters;

        return Throws is not null
            ? Task.FromException<IReadOnlyList<Patient>>(Throws)
            : Task.FromResult<IReadOnlyList<Patient>>([.. Cohort]);
    }

    public Task<IReadOnlyList<Patient>> GetCaseListAsync(int studyId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyDictionary<int, string>> GetNationalIdsAsync(
        IReadOnlyCollection<int> personIds,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<Patient>> SearchAsync(
        int studyId,
        string searchText,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

/// <summary>
/// A presenter that runs a probe at the moment a dialog would go up.
/// </summary>
/// <remarks>
/// <see cref="HeadlessNotificationPresenter"/> records what was shown but not what the rest of the
/// application looked like while it was showing, and "is the busy overlay behind this message box"
/// is exactly that kind of question.
/// </remarks>
/// <param name="probe">Called before the notification is recorded.</param>
/// <param name="answer">What every confirmation answers.</param>
internal sealed class ProbingNotificationPresenter(Action<UserNotification> probe, bool answer = false)
    : IUserNotificationPresenter
{
    private readonly List<UserNotification> _notifications = [];

    /// <summary>Everything this presenter was asked to show, in order.</summary>
    public IReadOnlyList<UserNotification> Notifications => _notifications;

    public Task PresentAsync(UserNotification notification)
    {
        probe(notification);
        _notifications.Add(notification);

        return Task.CompletedTask;
    }

    public Task<bool> AskAsync(UserNotification notification)
    {
        probe(notification);
        _notifications.Add(notification);

        return Task.FromResult(answer);
    }
}

/// <summary>An <see cref="IQueryParameterResolver"/> that answers with whatever the test set.</summary>
internal sealed class FakeParameterResolver : IQueryParameterResolver
{
    /// <summary>The answer. Succeeds with no values unless a test says otherwise.</summary>
    public ParameterResolution Answer { get; set; } = new() { Succeeded = true };

    /// <summary>The statements it was asked about.</summary>
    public List<string> Resolved { get; } = [];

    public Task<ParameterResolution> ResolveAsync(string sqlText, CancellationToken cancellationToken = default)
    {
        Resolved.Add(sqlText);

        return Task.FromResult(Answer);
    }
}
