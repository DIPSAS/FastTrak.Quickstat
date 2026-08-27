using Microsoft.Extensions.Logging;
using QuickStat.Configuration;
using QuickStat.Diagnostics;

namespace QuickStat.Data;

/// <summary>
/// Runs the login pipeline and owns the resulting <see cref="SessionContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TCRFSimpleContext.Connect</c> / <c>Disconnect</c>
/// (<c>CRF.Context.Facade.pas:216-243</c>) plus the observer loop inside
/// <c>TSimpleDatabase.Connect</c> (<c>Emetra.Database.Simple.pas:391-406</c>). Selecting a project
/// cost roughly 55 synchronous round trips on the UI thread behind a wait cursor
/// (<c>Docs/Port/01-data-access.md</c> §1.4); the pipeline here is four ordered steps and six round
/// trips, awaitable and cancellable.
/// </para>
/// <para>
/// A failing step leaves the service disconnected. In the Delphi a failing observer aborted the
/// login but left the ADO connection open, so <c>Connected</c> answered <see langword="true"/> after
/// a login that had not finished (<c>Docs/Port/01-data-access.md</c> §1.6).
/// </para>
/// </remarks>
internal sealed class SessionService : ISessionService, IDisposable, IAsyncDisposable
{
    /// <summary>Shutdown must never block on a database that has stopped answering.</summary>
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(5);

    private readonly QuickStatDatabase _database;
    private readonly IConnectionStringTranslator _translator;
    private readonly ILoginStep[] _steps;
    private readonly ILogger<SessionService> _logger;

    /// <summary>Set by either dispose path; see <see cref="Dispose"/> for why both can run.</summary>
    private bool _disposed;

    public SessionService(
        QuickStatDatabase database,
        IConnectionStringTranslator translator,
        IEnumerable<ILoginStep> steps,
        ILogger<SessionService> logger)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(translator);
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(logger);

        _database = database;
        _translator = translator;
        _steps = [.. steps.OrderBy(step => step.Order)];
        _logger = logger;
    }

    /// <inheritdoc />
    public SessionContext? Current { get; private set; }

    /// <inheritdoc />
    public bool IsConnected => Current is not null && _database.IsConnected;

    /// <inheritdoc />
    public event EventHandler<SessionContext?>? SessionChanged;

    /// <summary>The steps in the order they will run. Exposed so the ordering is assertable.</summary>
    internal IReadOnlyList<ILoginStep> Steps => _steps;

    /// <inheritdoc />
    public async Task<SessionContext> ConnectAsync(
        QuickStatConnection connection,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        await DisconnectAsync(cancellationToken).ConfigureAwait(false);

        progress?.Report(new OperationProgress("Connecting", $"Connecting to {connection.Name} ...", 0));

        ResolvedConnectionString resolved = _translator.Translate(connection);

        await _database.ConnectAsync(resolved, cancellationToken).ConfigureAwait(false);

        LoginContext context = new()
        {
            StudyName = connection.StudyName,
            Sql = _database,
            Progress = progress,
        };

        try
        {
            for (int i = 0; i < _steps.Length; i++)
            {
                ILoginStep step = _steps[i];

                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("Login step {Order} {Name}.", step.Order, step.Name);

                progress?.Report(new OperationProgress(
                    "Connecting",
                    step.Name,
                    100d * i / _steps.Length));

                await step.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Login failed for '{Name}'; closing the connection.", connection.Name);

            // Do not leave a half-built session behind a connection that answers.
            await SafeDisconnectAsync().ConfigureAwait(false);
            throw;
        }

        SessionContext session = Freeze(connection, context);

        Current = session;
        progress?.Report(new OperationProgress("Connecting", "Task completed", 100));

        _logger.LogInformation(
            "Connected to {Server}/{Database} as {User} for study {Study} (id {StudyId}, session {SessionId}).",
            session.ServerName,
            session.DatabaseName,
            session.User.UserName,
            session.StudyName,
            session.StudyId,
            session.SessionId);

        SessionChanged?.Invoke(this, session);

        return session;
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        SessionContext? previous = Current;

        if (previous is not null && _database.IsConnected && previous.SessionId > 0)
        {
            try
            {
                // Delphi TCRFStudyContext.CloseSession (CRF.Context.Session.pas:237-247). QuickStat
                // never increments the counters, so both are zero, exactly as before.
                _ = await _database.ExecuteAsync(
                    new SqlRequest
                    {
                        CommandText = DataSql.CloseSession,
                        Values = [previous.SessionId, 0, 0],
                        IsIdempotent = false,
                        Label = "Close session",
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is QuickStatDataException or OperationCanceledException)
            {
                // Disconnecting must never fail; the row is closed by the server eventually.
                _logger.LogWarning(exception, "dbo.CloseSession failed for session {SessionId}.", previous.SessionId);
            }
        }

        await SafeDisconnectAsync().ConfigureAwait(false);

        if (previous is not null)
        {
            SessionChanged?.Invoke(this, null);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            using CancellationTokenSource timeout = new(ShutdownTimeout);
            await DisconnectAsync(timeout.Token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "The session did not close cleanly during shutdown.");
        }

        // The container owns QuickStatDatabase and disposes it itself.
    }

    /// <summary>Closes the session row and the connection synchronously.</summary>
    /// <remarks>
    /// <para>
    /// <c>ServiceProvider.Dispose()</c> throws for a singleton that implements only
    /// <see cref="IAsyncDisposable"/>, and the composition root shuts the host down synchronously in
    /// <c>App.OnExit</c>, so this has to exist or a clean exit becomes an unhandled exception.
    /// </para>
    /// <para>
    /// The work runs on the thread pool and is bounded, because <c>App.OnExit</c> is on the WPF
    /// dispatcher thread and shutdown must never hang on a database that has stopped answering.
    /// </para>
    /// <para>
    /// <b>Idempotent, because this instance is genuinely disposed twice.</b> It is registered as
    /// <see cref="ISessionService"/> and aliased as the concrete type, and <c>ServiceProvider</c>
    /// captures a disposable once per descriptor that yields it. Here the flag earns its keep rather
    /// than merely tidying up: without it the second pass runs <c>dbo.CloseSession</c> a second time
    /// for a session that is already closed - a duplicate write, on shutdown, that nobody would ever
    /// see in a log.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            using CancellationTokenSource timeout = new(ShutdownTimeout);

            Task.Run(() => DisconnectAsync(timeout.Token), timeout.Token)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "The session did not close cleanly during shutdown.");
        }
    }

    private static SessionContext Freeze(QuickStatConnection connection, LoginContext context) => new()
    {
        StudyName = connection.StudyName,
        StudyId = context.StudyId,
        SessionId = context.SessionId,
        User = context.User ?? new StudyUser { UserId = 0, UserName = "" },
        Database = context.Database ?? new DatabaseInfo { DbVersion = -1 },
        ServerName = context.ServerName ?? "",
        DatabaseName = context.DatabaseName ?? "",
        HasIncompleteUserProfile = context.HasIncompleteUserProfile,
    };

    private async Task SafeDisconnectAsync()
    {
        Current = null;

        try
        {
            await _database.DisconnectAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "The connection did not close cleanly.");
        }
    }
}
