using QuickStat.Data;

namespace QuickStat.Tests.Data.Fakes;

/// <summary>
/// An <see cref="ISqlSession"/> that records what it was asked to do and answers from a script.
/// </summary>
/// <remarks>
/// <para>
/// This is why <see cref="ISqlSession"/> exists. <c>SqlConnection</c> cannot be faked and
/// <c>SqlException</c> cannot even be constructed, so without a seam at this level nothing above it
/// - serialisation, retry, the not-connected guard, binding, logging - could be tested at all, and
/// PORT-PLAN.md §9 R9 says no database is available.
/// </para>
/// <para>
/// A transient failure is reproduced by throwing an already-classified
/// <see cref="SqlCommandFailedException"/> with a transient <c>Number</c>, which is exactly what the
/// real session does after translating a <c>SqlException</c>.
/// </para>
/// </remarks>
internal sealed class FakeSqlSession : ISqlSession
{
    private readonly Queue<Func<BoundSqlCommand, object>> _script = new();

    public bool IsOpen { get; private set; }

    /// <summary>Every statement, in the order it was executed, including the session options.</summary>
    public List<string> Statements { get; } = [];

    /// <summary>Every command, in order.</summary>
    public List<BoundSqlCommand> Commands { get; } = [];

    public int OpenCount { get; private set; }

    public int ReopenCount { get; private set; }

    public int CloseCount { get; private set; }

    public int UsabilityProbeCount { get; private set; }

    /// <summary>What <see cref="IsUsableAsync"/> answers.</summary>
    public bool IsUsable { get; set; } = true;

    /// <summary>Queues one scripted answer.</summary>
    public FakeSqlSession Returns(object result)
    {
        _script.Enqueue(_ => result);
        return this;
    }

    /// <summary>Queues one scripted failure.</summary>
    public FakeSqlSession Throws(Exception exception)
    {
        _script.Enqueue(_ => throw exception);
        return this;
    }

    /// <summary>Queues one scripted answer computed from the command.</summary>
    public FakeSqlSession Answers(Func<BoundSqlCommand, object> answer)
    {
        _script.Enqueue(answer);
        return this;
    }

    public Task OpenAsync(string connectionString, CancellationToken cancellationToken)
    {
        OpenCount++;
        IsOpen = true;
        Statements.Add(SqlClientSession.SessionOptionsBatch);
        return Task.CompletedTask;
    }

    public Task ReopenAsync(CancellationToken cancellationToken)
    {
        ReopenCount++;
        IsOpen = true;
        Statements.Add(SqlClientSession.SessionOptionsBatch);
        return Task.CompletedTask;
    }

    public Task<bool> IsUsableAsync(CancellationToken cancellationToken)
    {
        UsabilityProbeCount++;
        return Task.FromResult(IsUsable && IsOpen);
    }

    public Task CloseAsync()
    {
        CloseCount++;
        IsOpen = false;
        return Task.CompletedTask;
    }

    public Task<SqlResultSet> QueryAsync(BoundSqlCommand command, CancellationToken cancellationToken) =>
        Task.FromResult((SqlResultSet)Next(command, SqlResultSet.Empty));

    public Task<int> ExecuteAsync(BoundSqlCommand command, CancellationToken cancellationToken) =>
        Task.FromResult((int)Next(command, 0));

    public Task<object?> ScalarAsync(BoundSqlCommand command, CancellationToken cancellationToken) =>
        Task.FromResult<object?>(Next(command, null!));

    public ValueTask DisposeAsync()
    {
        IsOpen = false;
        return ValueTask.CompletedTask;
    }

    public void Dispose() => IsOpen = false;

    private object Next(BoundSqlCommand command, object fallback)
    {
        Statements.Add(command.CommandText);
        Commands.Add(command);

        return _script.Count > 0 ? _script.Dequeue()(command) : fallback;
    }
}
