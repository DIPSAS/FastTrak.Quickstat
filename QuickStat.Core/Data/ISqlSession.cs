namespace QuickStat.Data;

/// <summary>
/// The one long-lived connection, behind an interface so that everything above it is testable
/// without a server.
/// </summary>
/// <remarks>
/// <para>
/// <strong>This is the seam.</strong> <c>SqlConnection</c>, <c>SqlCommand</c> and
/// <c>SqlDataReader</c> cannot be faked - <c>SqlException</c> cannot even be constructed - so the
/// only way to unit-test serialisation, retry, timeouts, binding and error classification without a
/// database (PORT-PLAN.md §9 R9) is to draw the line here: everything above this interface is
/// policy and is tested; everything below it is a thin adapter over
/// <c>Microsoft.Data.SqlClient</c> and is exercised by the human parity pass.
/// </para>
/// <para>
/// The interface deliberately takes an already-bound <see cref="BoundSqlCommand"/>: binding is
/// policy, and pushing it below the seam would put the interesting rules out of reach.
/// </para>
/// <para>
/// Implementations raise <see cref="QuickStatDataException"/> and its subclasses, never provider
/// exceptions - which is also what lets a fake reproduce a transient failure by throwing
/// <c>new SqlCommandFailedException("…") { Number = -2 }</c>.
/// </para>
/// <para>
/// <strong>Both</strong> disposal interfaces, deliberately. <c>ServiceProvider.Dispose()</c> throws
/// <c>InvalidOperationException</c> for a singleton that implements only <c>IAsyncDisposable</c>,
/// and the composition root shuts the host down synchronously in
/// <c>App.OnExit</c> - so an async-only chain here would take the process down on every clean exit.
/// </para>
/// </remarks>
internal interface ISqlSession : IDisposable, IAsyncDisposable
{
    /// <summary>Whether the physical connection is open.</summary>
    bool IsOpen { get; }

    /// <summary>Opens the connection and applies the session options.</summary>
    /// <param name="connectionString">An ADO.NET connection string.</param>
    /// <param name="cancellationToken">Cancels the open.</param>
    /// <returns>A task that completes when the session is usable.</returns>
    Task OpenAsync(string connectionString, CancellationToken cancellationToken);

    /// <summary>
    /// Closes and re-opens the connection with the same string, re-applying the session options.
    /// </summary>
    /// <param name="cancellationToken">Cancels the reconnect.</param>
    /// <returns>A task that completes when the session is usable again.</returns>
    /// <remarks>
    /// The half the Delphi was missing: its retry path disconnected
    /// (<c>Emetra.Database.Simple.pas:662</c>) and relied on the provider re-opening implicitly,
    /// which would also have silently dropped <c>SET XACT_ABORT ON</c> and <c>SET DATEFORMAT ymd</c>.
    /// </remarks>
    Task ReopenAsync(CancellationToken cancellationToken);

    /// <summary>Checks that the connection still answers, without disturbing anything.</summary>
    /// <param name="cancellationToken">Cancels the probe.</param>
    /// <returns><see langword="true"/> when the connection is still usable.</returns>
    Task<bool> IsUsableAsync(CancellationToken cancellationToken);

    /// <summary>Closes the connection. Safe when already closed.</summary>
    /// <returns>A task that completes when the connection is closed.</returns>
    Task CloseAsync();

    /// <summary>Runs a statement and materialises the first non-empty result set.</summary>
    /// <param name="command">The bound statement.</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>The rows.</returns>
    Task<SqlResultSet> QueryAsync(BoundSqlCommand command, CancellationToken cancellationToken);

    /// <summary>Runs a statement for its side effect.</summary>
    /// <param name="command">The bound statement.</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>Rows affected.</returns>
    Task<int> ExecuteAsync(BoundSqlCommand command, CancellationToken cancellationToken);

    /// <summary>Runs a statement and returns the first column of the first row.</summary>
    /// <param name="command">The bound statement.</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>The value, or <see langword="null"/>.</returns>
    Task<object?> ScalarAsync(BoundSqlCommand command, CancellationToken cancellationToken);
}
