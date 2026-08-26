using System.Text.RegularExpressions;
using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Diagnostics;
using QuickStat.Domain.Populations;
using Xunit;

namespace QuickStat.Tests.Domain.Populations;

/// <summary>
/// Raised by <see cref="RecordingSqlExecutor.QueryAsync"/> once the request has been recorded.
/// </summary>
/// <remarks>
/// <see cref="SqlResultSet"/> belongs to step 2.2 and every one of its members still throws
/// <see cref="NotImplementedException"/>, so a test cannot hand a populated result set back. Nor is
/// there a database (PORT-PLAN.md §9, R9). What step 2.3 can be held to without either is the
/// request it builds - the statement, the bound values and the table-valued parameter are pure
/// values - so the fake records the request and stops there, deterministically and independently of
/// how far along step 2.2 happens to be.
/// </remarks>
internal sealed class ResultSetUnavailableException : Exception
{
    public ResultSetUnavailableException()
        : base("The fake executor recorded the request; SqlResultSet cannot be produced in a unit test.")
    {
    }
}

/// <summary>An <see cref="ISqlExecutor"/> that records every request instead of running it.</summary>
internal sealed class RecordingSqlExecutor : ISqlExecutor
{
    public List<SqlRequest> Requests { get; } = [];

    public SqlRequest Last =>
        Requests.Count > 0 ? Requests[^1] : throw new InvalidOperationException("No request was recorded.");

    /// <summary>Rows affected returned by <see cref="ExecuteAsync"/>.</summary>
    public int RowsAffected { get; set; } = 1;

    /// <summary>When set, <see cref="ExecuteAsync"/> fails with it.</summary>
    public Exception? ExecuteFailure { get; set; }

    public Task<SqlResultSet> QueryAsync(SqlRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        throw new ResultSetUnavailableException();
    }

    public Task<int> ExecuteAsync(SqlRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return ExecuteFailure is null
            ? Task.FromResult(RowsAffected)
            : Task.FromException<int>(ExecuteFailure);
    }

    public Task<T?> ScalarAsync<T>(SqlRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult<T?>(default);
    }

    /// <summary>Runs an operation that queries, and returns the request it built.</summary>
    public static async Task<SqlRequest> CaptureAsync(RecordingSqlExecutor executor, Func<Task> operation)
    {
        await Assert.ThrowsAsync<ResultSetUnavailableException>(operation);
        return executor.Last;
    }
}

/// <summary>
/// A stand-in for step 2.2's <see cref="ISqlTextRewriter"/>, good enough for the statements these
/// tests use.
/// </summary>
/// <remarks>
/// Deliberately naive: the real scanner has to skip literals, bracketed and quoted identifiers,
/// comments and <c>::</c>, and writing a second one in production code is explicitly out of scope for
/// step 2.3. This exists only so the resolver can be exercised before step 2.2 lands.
/// </remarks>
internal sealed partial class StubSqlTextRewriter : ISqlTextRewriter
{
    public RewrittenSql Rewrite(string commandText)
    {
        List<string> names = [];
        bool repeated = false;

        foreach (Match match in PlaceholderPattern().Matches(commandText))
        {
            string name = match.Groups["name"].Value;
            if (names.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                repeated = true;
            }
            else
            {
                names.Add(name);
            }
        }

        string rewritten = PlaceholderPattern().Replace(commandText, "@${name}");
        return new RewrittenSql(rewritten, names, repeated);
    }

    [GeneratedRegex(@"(?<![:\w]):(?<name>[A-Za-z_][A-Za-z0-9_]*)")]
    private static partial Regex PlaceholderPattern();
}

/// <summary>An <see cref="ISessionService"/> whose current session the test sets directly.</summary>
internal sealed class StubSessionService : ISessionService
{
    public SessionContext? Current { get; set; }

    public bool IsConnected => Current is not null;

    // Explicit accessors: an auto-implemented event that nothing raises is CS0067, and warnings are
    // errors here.
    public event EventHandler<SessionContext?>? SessionChanged
    {
        add { }
        remove { }
    }

    public Task<SessionContext> ConnectAsync(
        QuickStatConnection connection,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

/// <summary>An <see cref="IPeriodPrompt"/> that answers with whatever the test configured.</summary>
internal sealed class StubPeriodPrompt : IPeriodPrompt
{
    /// <summary>What the prompt returns. <see langword="null"/> means the user cancelled.</summary>
    public HalfOpenPeriod? Answer { get; set; }

    public int CallCount { get; private set; }

    public string? LastContext { get; private set; }

    public string? LastCaption { get; private set; }

    public Task<HalfOpenPeriod?> TryGetPeriodAsync(
        string context,
        string caption,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastContext = context;
        LastCaption = caption;
        return Task.FromResult(Answer);
    }
}
