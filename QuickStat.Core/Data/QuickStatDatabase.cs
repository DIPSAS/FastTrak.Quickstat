using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;
using QuickStat.Configuration;

namespace QuickStat.Data;

/// <summary>
/// The <see cref="ISqlExecutor"/> implementation: one connection, one gate, one retry policy.
/// </summary>
/// <remarks>
/// <para>
/// Every operation takes a <see cref="SemaphoreSlim"/> before touching the connection. The Delphi
/// achieved the same thing by accident - it had exactly one <c>TADOQuery</c> and ran everything on
/// the UI thread - and every consumer was written against that guarantee. Here concurrency
/// serialises instead of throwing <c>InvalidOperationException</c> from the middle of a reader.
/// </para>
/// <para>
/// Internal because <see cref="ISqlExecutor"/> is the contract. The concrete type is resolved only
/// by <see cref="SessionService"/>, which needs connect and disconnect as well.
/// </para>
/// </remarks>
internal sealed class QuickStatDatabase : ISqlExecutor, IDisposable, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ISqlSession _session;
    private readonly ISqlTextRewriter _rewriter;
    private readonly SqlOptions _options;
    private readonly SqlRetryPolicy _retry;
    private readonly ILogger<QuickStatDatabase> _logger;

    /// <summary>Set by either dispose path; see <see cref="Dispose"/> for why both can run.</summary>
    private bool _disposed;

    public QuickStatDatabase(
        ISqlSession session,
        ISqlTextRewriter rewriter,
        SqlOptions options,
        ILogger<QuickStatDatabase> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(rewriter);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _session = session;
        _rewriter = rewriter;
        _options = options;
        _retry = new SqlRetryPolicy(options);
        _logger = logger;
    }

    /// <summary>Whether a session is open.</summary>
    public bool IsConnected => _session.IsOpen;

    /// <summary>Opens the connection. Does not run the login pipeline; that is
    /// <see cref="SessionService"/>'s job, which the Delphi conflated
    /// (<c>Emetra.Database.Simple.pas:391-406</c>).</summary>
    /// <param name="connectionString">The translated ADO.NET connection string.</param>
    /// <param name="cancellationToken">Cancels the open.</param>
    /// <returns>A task that completes when the connection is usable.</returns>
    public async Task ConnectAsync(ResolvedConnectionString connectionString, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionString);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            _logger.LogInformation("Connecting: {ConnectionString}", connectionString.Redacted);
            await _session.OpenAsync(connectionString.Value, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <summary>Closes the connection. Safe when already closed.</summary>
    /// <param name="cancellationToken">Bounds the wait for the gate.</param>
    /// <returns>A task that completes when the connection is closed.</returns>
    /// <remarks>
    /// <b>"Safe when already closed" includes "already disposed", and that is not pedantry.</b>
    /// Disposal is the only ordering in the process this type does not control: the container decides
    /// when it happens relative to <see cref="SessionService"/>, and a shutdown that arrives in the
    /// wrong order would otherwise wait on a disposed <see cref="SemaphoreSlim"/> and report a
    /// failure that is really a lifetime accident. Phase 5 hit exactly that
    /// (PORT-PLAN.md §8.11); the registration in <c>DataServiceCollectionExtensions</c> now orders it
    /// correctly and this makes the remaining out-of-order call quiet rather than alarming, because a
    /// log line that cries wolf on every clean exit is how a real failure gets missed.
    /// </remarks>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await _session.CloseAsync().ConfigureAwait(false);
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    /// <inheritdoc />
    public Task<SqlResultSet> QueryAsync(SqlRequest request, CancellationToken cancellationToken = default) =>
        RunAsync(request, "QUERY", (command, token) => _session.QueryAsync(command, token), cancellationToken);

    /// <inheritdoc />
    public Task<int> ExecuteAsync(SqlRequest request, CancellationToken cancellationToken = default) =>
        RunAsync(request, "COMMAND", (command, token) => _session.ExecuteAsync(command, token), cancellationToken);

    /// <inheritdoc />
    public async Task<T?> ScalarAsync<T>(SqlRequest request, CancellationToken cancellationToken = default)
    {
        object? value = await RunAsync(
            request,
            "SCALAR",
            (command, token) => _session.ScalarAsync(command, token),
            cancellationToken).ConfigureAwait(false);

        return ConvertScalar<T>(value);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await _session.DisposeAsync().ConfigureAwait(false);
        _gate.Dispose();
    }

    /// <summary>Closes the connection synchronously.</summary>
    /// <remarks>
    /// <para>
    /// Present because <c>ServiceProvider.Dispose()</c> refuses to dispose a singleton that
    /// implements only <see cref="IAsyncDisposable"/>, and the composition root stops the host
    /// synchronously in <c>App.OnExit</c>.
    /// </para>
    /// <para>
    /// <b>Idempotent, because this instance is genuinely disposed twice.</b> It is registered as
    /// <see cref="ISqlExecutor"/> and aliased as the concrete type, and <c>ServiceProvider</c>
    /// captures a disposable once per descriptor that yields it - so two disposal slots hold the same
    /// object. Without the flag the second pass disposes an already-disposed
    /// <see cref="SemaphoreSlim"/> and <see cref="ISqlSession"/>; harmless today, but it is the kind
    /// of thing that becomes a double-close the moment the session type grows a real one.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _session.Dispose();
        _gate.Dispose();
    }

    /// <summary>Converts a scalar result to the requested CLR type.</summary>
    /// <typeparam name="T">Expected type.</typeparam>
    /// <param name="value">The raw value.</param>
    /// <returns>The converted value, or <see langword="default"/> for <c>NULL</c>.</returns>
    internal static T? ConvertScalar<T>(object? value)
    {
        if (value is null or DBNull)
        {
            return default;
        }

        if (value is T typed)
        {
            return typed;
        }

        Type target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (target.IsEnum)
        {
            return (T)Enum.ToObject(target, Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }

        return (T)Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
    }

    private async Task<TResult> RunAsync<TResult>(
        SqlRequest request,
        string kind,
        Func<BoundSqlCommand, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            // Delphi CheckConnected (Emetra.Database.Simple.pas:340-345), before anything else.
            if (!_session.IsOpen)
            {
                throw new DatabaseNotConnectedException(
                    $"'{request.Label ?? Summarise(request.CommandText)}' was issued before a project was selected.");
            }

            BoundSqlCommand command = SqlRequestBinder.Bind(request, _rewriter, _options);

            for (int attempt = 1; ; attempt++)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                try
                {
                    TResult result = await operation(command, cancellationToken).ConfigureAwait(false);
                    stopwatch.Stop();
                    LogStatement(kind, command, stopwatch);
                    return result;
                }
                catch (OperationCanceledException)
                {
                    // A cancelled command sends an attention signal; the connection is usually fine
                    // afterwards but not always, and leaving a broken one behind the gate would fail
                    // every later call (Docs/Port/01-data-access.md §3.8).
                    await RecoverAfterCancellationAsync().ConfigureAwait(false);
                    throw;
                }
                catch (QuickStatDataException exception) when (_retry.ShouldRetry(exception, request.IsIdempotent, attempt))
                {
                    stopwatch.Stop();

                    TimeSpan delay = _retry.DelayFor(attempt);

                    _logger.LogWarning(
                        exception,
                        "Transient failure {Number} on attempt {Attempt}/{MaxAttempts} of {Label}; retrying in {Delay} ms.",
                        exception.Number,
                        attempt,
                        _retry.MaxAttempts,
                        request.Label ?? Summarise(command.CommandText),
                        (int)delay.TotalMilliseconds);

                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    await EnsureUsableAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _ = _gate.Release();
        }
    }

    private async Task EnsureUsableAsync(CancellationToken cancellationToken)
    {
        if (await _session.IsUsableAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        _logger.LogWarning("The connection did not survive the failure; re-opening before the next attempt.");
        await _session.ReopenAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RecoverAfterCancellationAsync()
    {
        try
        {
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));

            if (!await _session.IsUsableAsync(timeout.Token).ConfigureAwait(false))
            {
                await _session.ReopenAsync(timeout.Token).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            // Never let recovery replace the cancellation the caller asked for.
            _logger.LogWarning(exception, "The connection could not be verified after a cancelled command.");
        }
    }

    private void LogStatement(string kind, BoundSqlCommand command, Stopwatch stopwatch)
    {
        if (!_options.LogSql || !_logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }

        // Delphi LogSqlQuery / LogSqlCommand plus ' ... ( %.1f ms )'
        // (Emetra.Database.Simple.pas:564, :581, :600-601).
        _logger.LogDebug(
            "{Kind} {Elapsed:0.0} ms: {Sql}",
            kind,
            stopwatch.Elapsed.TotalMilliseconds,
            command.CommandText);

        if (_options.LogParameterValues && command.Parameters.Count > 0)
        {
            // Off by default: a population parameter can carry a national identity number and the
            // log file is not access-controlled.
            _logger.LogTrace(
                "{Kind} parameters: {Parameters}",
                kind,
                string.Join(", ", command.Parameters.Select(p => $"@{p.Name}={p.Value ?? "NULL"}")));
        }
    }

    private static string Summarise(string commandText)
    {
        string collapsed = commandText.ReplaceLineEndings(" ").Trim();

        return collapsed.Length <= 60 ? collapsed : string.Concat(collapsed.AsSpan(0, 57), "...");
    }
}
