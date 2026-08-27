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
    /// Name of the table type used to pass person-id lists, or <see langword="null"/> - the
    /// default - to use the chunked fallback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PORT-PLAN.md §7.3: patient-id lists move from a string-concatenated <c>IN (…)</c> to a
    /// table-valued parameter - one round trip, one cached plan, no exposure to SQL Server's
    /// 2 100-parameter limit. Shared by step 2.3 (national ids) and step 2.4 (every
    /// <c>{IdList}</c> collector), which is why the name lives here and not in either of them.
    /// </para>
    /// <para>
    /// <b>It defaults to <see langword="null"/> because the type does not exist.</b> This defaulted
    /// to <c>"Report.PersonIdList"</c> until Phase 5 checked, and that name is a proposal - it comes
    /// from <c>Docs/Port/03-collectors.md</c> §C.4 item 2, which asks for a migration that has never
    /// shipped. It appears nowhere in the Delphi, and nowhere in the schema project at
    /// <c>C:\work\FastTrak.Database</c> across 1 422 schema files and 375 upgrade scripts; the only
    /// user-defined table type in the whole product is <c>dbo.QuantityTableType</c>. Against a live
    /// database it is simply absent.
    /// </para>
    /// <para>
    /// The old default was not harmless. <see cref="Domain.Patients.PatientSql.NationalIdRequests"/>
    /// branches on whether this name is <em>set</em>, not on whether the type <em>exists</em>, so it
    /// bound a table-valued parameter of a nonexistent type on every run;
    /// <c>NationalIdRecovery</c> caught the failure and degraded, leaving <c>Fødselsnummer</c>
    /// blank - which is the exact bug Phase 4 restored the feature to fix (§10.5). The collectors
    /// escaped it only because <c>AddCollectors</c> hard-registers
    /// <see cref="Collectors.InlineLiteralPersonIdListBinder"/>.
    /// </para>
    /// <para>
    /// The real answer is §C.4 item 3, which the port never implemented: probe once at login with
    /// <c>SELECT TYPE_ID('…')</c> and set this from the result, exactly as
    /// <c>CollectorAvailability</c> already probes <c>OBJECT_ID</c>. Until that exists,
    /// <see langword="null"/> is the only default that is true of every database in the estate, and
    /// a customer who does ship the migration can still opt in through
    /// <see cref="QuickStatConnection.SqlOptions"/>.
    /// </para>
    /// </remarks>
    public string? PersonIdListTypeName { get; init; }

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
