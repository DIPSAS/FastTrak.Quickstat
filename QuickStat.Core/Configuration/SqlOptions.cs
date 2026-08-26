namespace QuickStat.Configuration;

/// <summary>
/// Process-wide knobs for connecting and executing. One instance, injected everywhere, so that a
/// support scenario can be diagnosed from a single object rather than from scattered literals.
/// </summary>
/// <remarks>
/// Every default here differs from the Delphi on purpose; the reasons are in the individual
/// members. Nothing in this record is read from <c>QuickStat.config.xml</c> - the legacy file has
/// no place for it - so overrides come from step 2.7's settings store or from environment
/// variables.
/// </remarks>
public sealed record SqlOptions
{
    /// <summary>
    /// Command timeout when a <see cref="QuickStat.Data.SqlRequest"/> does not carry its own.
    /// </summary>
    /// <remarks>
    /// The Delphi left ADO at its 30 s default (<c>Emetra.Database.Simple.pas:157</c>), which is a
    /// known source of failures on large protocols: the collector queries routinely scan the whole
    /// database (<c>Docs/Port/03-collectors.md</c> §C.2).
    /// </remarks>
    public TimeSpan DefaultCommandTimeout { get; init; } = TimeSpan.FromSeconds(300);

    /// <summary>Login timeout injected into the connection string when it is absent.</summary>
    public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Emit every statement to the log at <c>Debug</c>. Delphi: <c>Database.LogSql := true</c>
    /// (<c>MainQuickStat.pas:270</c>), which was unconditional.
    /// </summary>
    public bool LogSql { get; init; } = true;

    /// <summary>
    /// Also log parameter <em>values</em>. Off by default: population parameters can carry a
    /// national identity number, and the log file is not access-controlled.
    /// </summary>
    public bool LogParameterValues { get; init; }

    /// <summary>
    /// Attempts for a transient failure, including the first. Delphi: 10
    /// (<c>Emetra.Database.Simple.pas:185</c>).
    /// </summary>
    /// <remarks>
    /// Only <see cref="QuickStat.Data.SqlRequest.IsIdempotent"/> requests are retried. The Delphi
    /// retried commands as well, so a transient failure during <c>Report.AddSelectionMember</c>
    /// could duplicate rows.
    /// </remarks>
    public int MaxRetryAttempts { get; init; } = 3;

    /// <summary>First backoff interval; subsequent attempts double it and add jitter.</summary>
    public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// <c>Application Name</c> injected when absent, so the session is identifiable in
    /// <c>sys.dm_exec_sessions</c>.
    /// </summary>
    public string ApplicationName { get; init; } = "DIPS QuickStat";

    /// <summary>
    /// Encryption keywords injected only when the translated string specifies none.
    /// </summary>
    /// <remarks>
    /// PORT-PLAN.md §8.2 and R1: a verbatim translation of the legacy strings silently turns TLS
    /// on, which fails against on-premise servers with self-signed certificates. This default
    /// preserves today's connectivity and is explicitly <em>not</em> a security improvement; it is
    /// overridable per connection through <see cref="QuickStatConnection.SqlOptions"/>.
    /// </remarks>
    public string DefaultEncryptionOptions { get; init; } = "Encrypt=True;TrustServerCertificate=True";

    /// <summary>
    /// Name of the table type used to pass person-id lists, or <see langword="null"/> to force the
    /// chunked-literal fallback.
    /// </summary>
    /// <remarks>
    /// PORT-PLAN.md §7.3: patient-id lists move from a string-concatenated <c>IN (…)</c> to a
    /// table-valued parameter - one round trip, one cached plan, no exposure to SQL Server's
    /// 2 100-parameter limit. Shared by step 2.3 (national ids) and step 2.4 (every
    /// <c>{IdList}</c> collector), which is why the name lives here and not in either of them.
    /// </remarks>
    public string? PersonIdListTypeName { get; init; } = "Report.PersonIdList";

    /// <summary>Single column of <see cref="PersonIdListTypeName"/>.</summary>
    public string PersonIdListColumnName { get; init; } = "PersonId";

    /// <summary>
    /// Person ids per statement when <see cref="PersonIdListTypeName"/> is unavailable and the
    /// literal fallback is in use.
    /// </summary>
    /// <remarks>
    /// 1 000 keeps a comfortable margin below the 2 100-parameter ceiling and matches the
    /// recommendation in <c>Docs/Port/03-collectors.md</c> §C.4.
    /// </remarks>
    public int MaxIdsPerBatch { get; init; } = 1000;
}
