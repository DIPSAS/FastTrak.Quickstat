using QuickStat.Collectors;
using QuickStat.Data;
using QuickStat.Diagnostics;
using QuickStat.Domain.Matrix;

namespace QuickStat.Tests.Collectors;

/// <summary>
/// An <see cref="ISqlExecutor"/> that answers from a queue and records what it was asked.
/// </summary>
/// <remarks>
/// No database is available (PORT-PLAN.md R9). Step 2.2 made <see cref="SqlResultSet"/> publicly
/// constructible precisely so the other steps can fake results without one.
/// </remarks>
internal sealed class RecordingSqlExecutor : ISqlExecutor
{
    private readonly Queue<SqlResultSet> _results = new();

    /// <summary>Every request that reached the executor, in order.</summary>
    public List<SqlRequest> Requests { get; } = [];

    /// <summary>Returned when the queue is empty; defaults to no rows.</summary>
    public SqlResultSet Fallback { get; set; } = SqlResultSet.Empty;

    /// <summary>Thrown from the next call instead of answering, if set.</summary>
    public Exception? ThrowOnQuery { get; set; }

    /// <summary>Queues one result, consumed by the next query.</summary>
    /// <param name="result">The result set.</param>
    /// <returns>This, for chaining.</returns>
    public RecordingSqlExecutor Enqueue(SqlResultSet result)
    {
        _results.Enqueue(result);

        return this;
    }

    public Task<SqlResultSet> QueryAsync(SqlRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Requests.Add(request);

        if (ThrowOnQuery is not null)
        {
            throw ThrowOnQuery;
        }

        return Task.FromResult(_results.Count > 0 ? _results.Dequeue() : Fallback);
    }

    public Task<int> ExecuteAsync(SqlRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<T?> ScalarAsync<T>(SqlRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

/// <summary>
/// A sink that records what it was offered and can be told to reject.
/// </summary>
/// <remarks>
/// Deliberately not <c>PersonMatrix</c>: these tests are about the runner, and a sink that records
/// raw offers shows the difference between "the runner never offered the row" and "the sink refused
/// it", which the matrix would hide behind its own cohort filter.
/// </remarks>
internal sealed class RecordingSink : ICollectorResultSink
{
    private readonly Func<string, CollectorResultRow, bool> _accept;

    public RecordingSink(Func<string, CollectorResultRow, bool>? accept = null) =>
        _accept = accept ?? ((_, _) => true);

    /// <summary>Column name and row of every offer, in order.</summary>
    public List<(string ColumnName, CollectorResultRow Row)> Offers { get; } = [];

    /// <summary>The order this sink wants its variable names in.</summary>
    public ColumnOrder ColumnOrder { get; set; }

    /// <summary>How many times the runner asked for a set.</summary>
    public int VariableNameSetsCreated { get; private set; }

    public bool Add(string columnName, in CollectorResultRow row)
    {
        Offers.Add((columnName, row));

        return _accept(columnName, row);
    }

    public VariableNameSet CreateVariableNameSet()
    {
        VariableNameSetsCreated++;

        return new VariableNameSet(ColumnOrder);
    }
}

/// <summary>A sink that does not override <see cref="ICollectorResultSink.CreateVariableNameSet"/>.</summary>
/// <remarks>
/// Exists to prove the default interface implementation is reachable: a sink written before that
/// member was added still compiles and still gets an insertion-ordered set, which is what ships.
/// </remarks>
internal sealed class MinimalSink : ICollectorResultSink
{
    public bool Add(string columnName, in CollectorResultRow row) => true;
}

/// <summary>Collects the progress reports a run emitted.</summary>
internal sealed class RecordingProgress : IProgress<OperationProgress>
{
    public List<OperationProgress> Reports { get; } = [];

    public void Report(OperationProgress value) => Reports.Add(value);
}
