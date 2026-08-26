using QuickStat.Configuration;

namespace QuickStat.Data;

/// <summary>
/// Decides whether a failure is worth retrying and how long to wait first.
/// </summary>
/// <remarks>
/// <para>
/// Narrowed from the Delphi on purpose (<c>Docs/Port/01-data-access.md</c> §5.1, PORT-PLAN.md §7.2).
/// There, <c>ExecuteCommand</c> and <c>OpenDataset</c> both looped up to ten times with a flat 500 ms
/// pause (<c>Emetra.Database.Simple.pas:505-529</c>, <c>:544-562</c>, <c>:185-186</c>) and retried
/// <em>everything</em> - so a transient failure part-way through <c>Report.AddSelectionMember</c> or
/// <c>dbo.AddSession</c> duplicated rows. Here only
/// <see cref="SqlRequest.IsIdempotent"/> requests are eligible.
/// </para>
/// <para>
/// The Delphi's trigger was SQLSTATE <c>08S01</c> (communication link failure). SqlClient reports
/// error numbers rather than SQLSTATE, so the equivalent set is enumerated below.
/// </para>
/// </remarks>
internal sealed class SqlRetryPolicy
{
    /// <summary>
    /// Errors that mean "the network or the server hiccuped", not "your statement is wrong".
    /// </summary>
    /// <remarks>
    /// <c>-2</c> is the client-side command timeout; 20/64/233/10053/10054/10060/10061 are the
    /// transport failures that SQLSTATE <c>08S01</c> covered; 1205 is the deadlock victim; 4060,
    /// 4221 and the 40xxx/499xx range are the Azure SQL throttling and failover codes, harmless to
    /// carry against an on-premise server.
    /// </remarks>
    private static readonly HashSet<int> TransientNumbers =
    [
        -2, 20, 64, 233, 1205, 4060, 4221, 10053, 10054, 10060, 10061,
        40197, 40501, 40613, 49918, 49919, 49920,
    ];

    private static readonly TimeSpan MaximumDelay = TimeSpan.FromSeconds(30);

    private readonly SqlOptions _options;

    public SqlRetryPolicy(SqlOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <summary>Attempts including the first. Delphi: 10.</summary>
    public int MaxAttempts => Math.Max(1, _options.MaxRetryAttempts);

    /// <summary>Whether an error number identifies a transient failure.</summary>
    /// <param name="number">SQL Server error number, or <see langword="null"/> when unknown.</param>
    /// <returns><see langword="true"/> when retrying could help.</returns>
    public static bool IsTransient(int? number) => number is int value && TransientNumbers.Contains(value);

    /// <summary>Whether a failure warrants another attempt.</summary>
    /// <param name="exception">The classified failure.</param>
    /// <param name="isIdempotent">Whether re-running the statement is safe.</param>
    /// <param name="attempt">One-based number of the attempt that just failed.</param>
    /// <returns><see langword="true"/> when the caller should wait and try again.</returns>
    public bool ShouldRetry(QuickStatDataException exception, bool isIdempotent, int attempt)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return isIdempotent && attempt < MaxAttempts && IsTransient(exception.Number);
    }

    /// <summary>Backoff before the next attempt: base delay doubled per attempt, plus jitter.</summary>
    /// <param name="attempt">One-based number of the attempt that just failed.</param>
    /// <returns>How long to wait.</returns>
    public TimeSpan DelayFor(int attempt)
    {
        int exponent = Math.Clamp(attempt - 1, 0, 16);
        double milliseconds = _options.RetryBaseDelay.TotalMilliseconds * Math.Pow(2, exponent);

        milliseconds = Math.Min(milliseconds, MaximumDelay.TotalMilliseconds);

        // Jitter keeps a burst of retries from re-colliding after a shared outage. Capped at a
        // quarter of the interval so DelayFor stays predictable enough to assert on.
        double jitter = milliseconds * 0.25 * Random.Shared.NextDouble();

        return TimeSpan.FromMilliseconds(milliseconds + jitter);
    }
}
