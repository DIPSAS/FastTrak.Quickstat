using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace QuickStat.Data;

/// <summary>
/// The real <see cref="ISqlSession"/>: one long-lived <c>SqlConnection</c>.
/// </summary>
/// <remarks>
/// <para>
/// One connection for the whole session, not one per operation. The application model is explicitly
/// session-scoped - connect, <c>dbo.AddSession</c>, work, <c>dbo.CloseSession</c>, with
/// <c>SessId</c> a real database row - and <c>SET XACT_ABORT ON</c> / <c>SET DATEFORMAT ymd</c> are
/// connection state that <c>sp_reset_connection</c> would discard on every pooled re-open.
/// QuickStat is also a single-user desktop application that never needs two live result sets; the
/// Delphi literally could not produce two, because <c>FastQuery</c> returned one shared
/// <c>TADOQuery</c> (<c>Emetra.Database.Simple.pas:578</c>).
/// </para>
/// <para>
/// Serialisation lives in <see cref="QuickStatDatabase"/>, not here.
/// </para>
/// </remarks>
internal sealed class SqlClientSession : ISqlSession
{
    /// <summary>
    /// Issued on every physical open, before any other statement.
    /// </summary>
    /// <remarks>
    /// PORT-PLAN.md §7.2. The Delphi ran these in login observer #2
    /// (<c>Emetra.Database.Info.pas:146-147</c>) - after <c>SELECT @@SERVERNAME, DB_NAME()</c> in
    /// <c>Connect</c> and after observer #1's <c>EXEC dbo.GetStudyAndUser</c> - so the first
    /// statements of every session parsed dates under whatever the server's default happened to be.
    /// Here nothing can precede them, including after a reconnect.
    /// </remarks>
    public const string SessionOptionsBatch = "SET XACT_ABORT ON;\r\nSET DATEFORMAT ymd;";

    private const string ProbeStatement = "SELECT 1";

    private readonly ILogger<SqlClientSession> _logger;
    private SqlConnection? _connection;
    private string? _connectionString;

    public SqlClientSession(ILogger<SqlClientSession> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public bool IsOpen => _connection is { State: ConnectionState.Open };

    public async Task OpenAsync(string connectionString, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        await CloseAsync().ConfigureAwait(false);

        _connectionString = connectionString;
        await OpenCoreAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ReopenAsync(CancellationToken cancellationToken)
    {
        if (_connectionString is null)
        {
            throw new DatabaseNotConnectedException("There is no connection to re-open.");
        }

        await CloseAsync().ConfigureAwait(false);
        await OpenCoreAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> IsUsableAsync(CancellationToken cancellationToken)
    {
        if (!IsOpen)
        {
            return false;
        }

        try
        {
            using SqlCommand command = _connection!.CreateCommand();
            command.CommandText = ProbeStatement;
            command.CommandTimeout = 5;

            _ = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception exception) when (exception is SqlException or InvalidOperationException)
        {
            _logger.LogWarning(exception, "The connection did not answer the liveness probe.");
            return false;
        }
    }

    public Task CloseAsync()
    {
        SqlConnection? connection = _connection;
        _connection = null;

        if (connection is null)
        {
            return Task.CompletedTask;
        }

        connection.InfoMessage -= OnInfoMessage;

        return CloseCoreAsync(connection);
    }

    public async Task<SqlResultSet> QueryAsync(BoundSqlCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        using SqlCommand sqlCommand = CreateCommand(command);

        try
        {
            using SqlDataReader reader = await sqlCommand
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            return await MaterialiseAsync(reader, cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException exception)
        {
            throw Translate(exception, command.CommandText);
        }
    }

    public async Task<int> ExecuteAsync(BoundSqlCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        using SqlCommand sqlCommand = CreateCommand(command);

        try
        {
            return await sqlCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException exception)
        {
            throw Translate(exception, command.CommandText);
        }
    }

    public async Task<object?> ScalarAsync(BoundSqlCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        using SqlCommand sqlCommand = CreateCommand(command);

        try
        {
            object? value = await sqlCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return value is DBNull ? null : value;
        }
        catch (SqlException exception)
        {
            throw Translate(exception, command.CommandText);
        }
    }

    public async ValueTask DisposeAsync() => await CloseAsync().ConfigureAwait(false);

    public void Dispose()
    {
        SqlConnection? connection = _connection;
        _connection = null;

        if (connection is null)
        {
            return;
        }

        connection.InfoMessage -= OnInfoMessage;

        try
        {
            connection.Close();
        }
        finally
        {
            connection.Dispose();
        }
    }

    /// <summary>Reduces a provider exception to the typed hierarchy.</summary>
    /// <param name="exception">The provider exception.</param>
    /// <param name="commandText">The statement that failed.</param>
    /// <returns>The exception to raise.</returns>
    internal static QuickStatDataException Translate(SqlException exception, string? commandText)
    {
        List<SqlErrorInfo> errors = new(exception.Errors.Count);

        foreach (SqlError error in exception.Errors)
        {
            errors.Add(new SqlErrorInfo(error.Number, error.Class, NullIfEmpty(error.Procedure), error.Message));
        }

        return SqlErrorClassifier.Classify(errors, commandText, exception);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    private static async Task CloseCoreAsync(SqlConnection connection)
    {
        try
        {
            await connection.CloseAsync().ConfigureAwait(false);
        }
        finally
        {
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async Task<SqlResultSet> MaterialiseAsync(SqlDataReader reader, CancellationToken cancellationToken)
    {
        // A batch that starts with SET statements produces a leading result set with no columns.
        while (reader.FieldCount == 0 && await reader.NextResultAsync(cancellationToken).ConfigureAwait(false))
        {
            // Advance to the first statement that actually projected something.
        }

        if (reader.FieldCount == 0)
        {
            return SqlResultSet.Empty;
        }

        SqlColumn[] columns = new SqlColumn[reader.FieldCount];

        for (int i = 0; i < columns.Length; i++)
        {
            columns[i] = new SqlColumn(i, reader.GetName(i), reader.GetFieldType(i) ?? typeof(object));
        }

        List<object?[]> rows = [];

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            object[] buffer = new object[columns.Length];
            _ = reader.GetValues(buffer);

            object?[] values = new object?[columns.Length];

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = buffer[i] is DBNull ? null : buffer[i];
            }

            rows.Add(values);
        }

        return new SqlResultSet(columns, rows);
    }

    private async Task OpenCoreAsync(CancellationToken cancellationToken)
    {
        SqlConnection connection = new(_connectionString);
        connection.InfoMessage += OnInfoMessage;

        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException exception)
        {
            connection.InfoMessage -= OnInfoMessage;
            await connection.DisposeAsync().ConfigureAwait(false);
            throw Translate(exception, commandText: null);
        }
        catch
        {
            connection.InfoMessage -= OnInfoMessage;
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        _connection = connection;

        using SqlCommand command = connection.CreateCommand();
        command.CommandText = SessionOptionsBatch;

        try
        {
            _ = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException exception)
        {
            throw Translate(exception, SessionOptionsBatch);
        }

        _logger.LogDebug("Session options applied: {Options}", SessionOptionsBatch.Replace("\r\n", " ", StringComparison.Ordinal));
    }

    private SqlCommand CreateCommand(BoundSqlCommand command)
    {
        if (_connection is not { State: ConnectionState.Open })
        {
            throw new DatabaseNotConnectedException("The statement was issued while no session was open.");
        }

        SqlCommand sqlCommand = _connection.CreateCommand();

        try
        {
            sqlCommand.CommandText = command.CommandText;
            sqlCommand.CommandType = CommandType.Text;
            sqlCommand.CommandTimeout = (int)Math.Clamp(command.CommandTimeout.TotalSeconds, 0, int.MaxValue);

            foreach (BoundParameter parameter in command.Parameters)
            {
                _ = sqlCommand.Parameters.Add(SqlParameterFactory.Create(parameter.Name, parameter.Value));
            }

            foreach (SqlTableParameter table in command.TableParameters)
            {
                _ = sqlCommand.Parameters.Add(SqlParameterFactory.CreateTableValued(table));
            }

            return sqlCommand;
        }
        catch
        {
            sqlCommand.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Server information messages - <c>PRINT</c>, <c>RAISERROR</c> with severity 10 or less - go to
    /// the log and nowhere else.
    /// </summary>
    /// <remarks>
    /// This is the fix for <c>Emetra.Database.Simple.pas:652-656</c>: there, every entry in the ADO
    /// <c>Errors</c> collection counted, so a stored procedure that printed anything raised
    /// <c>EDatabaseCommandFailed</c> even though the statement had succeeded.
    /// </remarks>
    private void OnInfoMessage(object sender, SqlInfoMessageEventArgs e)
    {
        foreach (SqlError error in e.Errors)
        {
            _logger.LogInformation(
                "SQL Server message {Number} (class {Class}) from {Procedure}: {Message}",
                error.Number,
                error.Class,
                string.IsNullOrEmpty(error.Procedure) ? "(batch)" : error.Procedure,
                error.Message);
        }
    }
}
