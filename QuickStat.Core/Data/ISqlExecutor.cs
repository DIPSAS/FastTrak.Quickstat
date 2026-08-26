namespace QuickStat.Data;

/// <summary>
/// The one way anything in QuickStat reaches SQL Server.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>ISQL</c> (<c>Emetra.Database.Interfaces.pas:254-268</c>), of which only
/// <c>FastQuery</c>, <c>ExecuteCommand</c>, <c>Dataset</c> and <c>Connected</c> were ever called.
/// <c>Dataset</c> is gone: <c>FastQuery</c> returned the <em>same</em> <c>TADOQuery</c> instance
/// every time (<c>Emetra.Database.Simple.pas:578</c>), so only one result set could be alive and
/// every caller had to follow an open-drain-close discipline. Because the cursor was
/// <c>clUseClient</c>, ADO already materialised the whole result set anyway, so returning a
/// buffered <see cref="SqlResultSet"/> is behaviour-preserving rather than a regression.
/// </para>
/// <para>
/// Implementations serialise concurrent calls; QuickStat is a single-user desktop application that
/// never needs two result sets at once, and the session-scoped <c>SET</c> options rule out
/// per-operation pooling.
/// </para>
/// </remarks>
public interface ISqlExecutor
{
    /// <summary>Runs a statement and materialises the first result set.</summary>
    /// <param name="request">Statement, arguments and per-call policy.</param>
    /// <param name="cancellationToken">Cancels the command; the connection is verified afterwards.</param>
    /// <returns>The rows, buffered client-side.</returns>
    /// <exception cref="DatabaseNotConnectedException">No session is open.</exception>
    /// <exception cref="SqlParameterBindingException">Arguments do not match the placeholders.</exception>
    /// <exception cref="SqlPrivilegeException">The login lacks a required <c>GRANT</c>.</exception>
    /// <exception cref="SqlUserDefinedException">A stored procedure raised a business error.</exception>
    /// <exception cref="SqlCommandFailedException">Any other server-side failure.</exception>
    Task<SqlResultSet> QueryAsync(SqlRequest request, CancellationToken cancellationToken = default);

    /// <summary>Runs a statement for its side effect.</summary>
    /// <param name="request">Statement, arguments and per-call policy.</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>
    /// Rows affected. The Delphi always returned the literal <c>1</c>
    /// (<c>Emetra.Database.Simple.pas:493</c>); this returns the real count.
    /// </returns>
    Task<int> ExecuteAsync(SqlRequest request, CancellationToken cancellationToken = default);

    /// <summary>Runs a statement and returns the first column of the first row.</summary>
    /// <typeparam name="T">Expected CLR type.</typeparam>
    /// <param name="request">Statement, arguments and per-call policy.</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>The value, or <see langword="default"/> when the result set is empty or the value is null.</returns>
    Task<T?> ScalarAsync<T>(SqlRequest request, CancellationToken cancellationToken = default);
}
