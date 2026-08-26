using Microsoft.Extensions.Logging;
using QuickStat.Data;

namespace QuickStat.Domain.Populations;

/// <summary>
/// Reads the population catalogue through <see cref="ISqlExecutor"/> and writes the selection audit.
/// </summary>
/// <remarks>Delphi: <c>TPopulationList.Load</c> (<c>EPR.Population.List.pas:95-121</c>).</remarks>
internal sealed class PopulationRepository : IPopulationRepository
{
    private readonly ISqlExecutor _sql;
    private readonly ILogger<PopulationRepository> _log;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="sql">The executor.</param>
    /// <param name="log">Where diagnostics go.</param>
    public PopulationRepository(ISqlExecutor sql, ILogger<PopulationRepository> log)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentNullException.ThrowIfNull(log);

        _sql = sql;
        _log = log;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Population>> GetPopulationsAsync(
        int studyId,
        int dbVersion,
        bool frequentlyUsedOnly,
        CancellationToken cancellationToken = default)
    {
        // EPR.Population.List.pas:102 - no study, no round trip, empty list.
        if (studyId <= 0)
        {
            return [];
        }

        // PORT-PLAN.md R15 / 02-populations-patients.md R15: DbVersion is -1 when dbo.GetDatabaseInfo
        // failed, and the Delphi let that fall silently into the "old database" branch. Route it the
        // same way for parity, but say so.
        if (dbVersion < 0)
        {
            _log.LogWarning(
                "Database version is unknown ({DbVersion}); loading the population catalogue with the pre-{MinimumVersion} procedure.",
                dbVersion,
                DatabaseInfo.PopulationsWithVersionDbVersion);
        }

        SqlRequest request = PopulationSql.Catalogue(studyId, dbVersion, frequentlyUsedOnly);
        SqlResultSet rows = await _sql.QueryAsync(request, cancellationToken).ConfigureAwait(false);
        List<Population> populations = Map(rows);

        _log.LogDebug("Found {Count} populations for study {StudyId}.", populations.Count, studyId);
        return populations;
    }

    /// <inheritdoc />
    public async Task LogPopulationSelectedAsync(
        int studyId,
        int procId,
        string procTitle,
        long elapsedMilliseconds,
        CancellationToken cancellationToken = default)
    {
        SqlRequest request = PopulationSql.SelectionAudit(studyId, procId, procTitle ?? "", elapsedMilliseconds);

        try
        {
            await _sql.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // EPR.VclFrame.Populations.pas:224-226 swallows this with SilentWarning. The audit feeds
            // the "frequently used" ranking; losing a row must never reach the user or abort a load.
            _log.LogWarning(ex, "Could not write the population audit row for population {ProcId}.", procId);
        }
    }

    private List<Population> Map(SqlResultSet rows)
    {
        // CRF.Population.pas:81-95 read all seven columns with FieldByName, so one missing column
        // killed the whole load. Population.cs freezes the weaker rule: ProcId, ProcTitle and SqlText
        // are required, everything else defaults and is reported once.
        int procId = rows.GetOrdinal(PopulationSql.ColProcId);
        int title = rows.GetOrdinal(PopulationSql.ColProcTitle);
        int sqlText = rows.GetOrdinal(PopulationSql.ColSqlText);

        int group = rows.IndexOf(PopulationSql.ColProcGroup);
        int helpText = rows.IndexOf(PopulationSql.ColHelpText);
        int infoCaption = rows.IndexOf(PopulationSql.ColInfoCaption);
        int sourceCode = rows.IndexOf(PopulationSql.ColSourceCode);

        WarnAboutMissingOptionalColumns(group, helpText, infoCaption, sourceCode);

        List<Population> populations = [];
        foreach (SqlRow row in rows)
        {
            populations.Add(new Population
            {
                ProcId = row.GetInt32(procId),
                Title = row.GetString(title),
                QueryText = row.GetString(sqlText),
                Group = group < 0 ? "" : row.GetString(group),
                HelpText = helpText < 0 ? "" : row.GetString(helpText),
                InfoCaption = infoCaption < 0 ? "" : row.GetString(infoCaption),
                SourceCode = sourceCode < 0 ? "" : row.GetString(sourceCode),
            });
        }

        return populations;
    }

    private void WarnAboutMissingOptionalColumns(int group, int helpText, int infoCaption, int sourceCode)
    {
        List<string> missing = [];
        if (group < 0)
        {
            missing.Add(PopulationSql.ColProcGroup);
        }

        if (helpText < 0)
        {
            missing.Add(PopulationSql.ColHelpText);
        }

        if (infoCaption < 0)
        {
            missing.Add(PopulationSql.ColInfoCaption);
        }

        if (sourceCode < 0)
        {
            missing.Add(PopulationSql.ColSourceCode);
        }

        if (missing.Count > 0)
        {
            _log.LogWarning(
                "The population catalogue did not return these optional columns: {MissingColumns}. They default to empty.",
                string.Join(", ", missing));
        }
    }
}
