using QuickStat.Data;

namespace QuickStat.Tests.Data.Fakes;

/// <summary>
/// An <see cref="ISqlExecutor"/> that records every request and answers from a queue.
/// </summary>
/// <remarks>
/// The seam the login steps are tested through, and the same shape every other Phase 2 step can use
/// for its own repositories: <see cref="SqlResultSet.Create"/> builds the answers, so no database is
/// involved anywhere (PORT-PLAN.md §9 R9).
/// </remarks>
internal sealed class RecordingSqlExecutor : ISqlExecutor
{
    private readonly Queue<Func<SqlRequest, object?>> _script = new();

    /// <summary>Every request, in the order it was issued.</summary>
    public List<SqlRequest> Requests { get; } = [];

    /// <summary>The statements only, for terse ordering assertions.</summary>
    public IReadOnlyList<string> Statements => [.. Requests.Select(r => r.CommandText)];

    public RecordingSqlExecutor Returns(object? result)
    {
        _script.Enqueue(_ => result);
        return this;
    }

    public RecordingSqlExecutor Throws(Exception exception)
    {
        _script.Enqueue(_ => throw exception);
        return this;
    }

    public Task<SqlResultSet> QueryAsync(SqlRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Next(request) as SqlResultSet ?? SqlResultSet.Empty);

    public Task<int> ExecuteAsync(SqlRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(Next(request) is int rows ? rows : 0);

    public Task<T?> ScalarAsync<T>(SqlRequest request, CancellationToken cancellationToken = default)
    {
        object? value = Next(request);

        return Task.FromResult(value is T typed ? typed : default);
    }

    private object? Next(SqlRequest request)
    {
        Requests.Add(request);

        return _script.Count > 0 ? _script.Dequeue()(request) : null;
    }
}
