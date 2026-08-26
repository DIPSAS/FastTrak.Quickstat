using Microsoft.Extensions.Logging;
using QuickStat.Data;

namespace QuickStat.Domain.Matrix;

/// <summary>Reads lab captions through <see cref="ISqlExecutor"/>.</summary>
/// <remarks>Delphi: the <c>QueryLabCaptions</c> loop in <c>EPR.QA.CaptionDictionary.pas:103-117</c>.</remarks>
internal sealed class CaptionRepository : ICaptionRepository
{
    private readonly ISqlExecutor _sql;
    private readonly ILogger<CaptionRepository> _log;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="sql">The executor.</param>
    /// <param name="log">Where diagnostics go.</param>
    public CaptionRepository(ISqlExecutor sql, ILogger<CaptionRepository> log)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentNullException.ThrowIfNull(log);

        _sql = sql;
        _log = log;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CaptionRecord>> GetLabCaptionsAsync(CancellationToken cancellationToken = default)
    {
        SqlResultSet rows = await _sql
            .QueryAsync(CaptionSql.LabCaptionRequest(), cancellationToken)
            .ConfigureAwait(false);

        return Map(rows);
    }

    private List<CaptionRecord> Map(SqlResultSet rows)
    {
        // Strict on all three, because the query selects them by those exact aliases: a missing one
        // means the statement is not the statement this method thinks it is.
        int varName = rows.GetOrdinal(CaptionSql.ColVarName);
        int caption = rows.GetOrdinal(CaptionSql.ColCaption);
        int description = rows.GetOrdinal(CaptionSql.ColVarDescription);

        List<CaptionRecord> captions = new(rows.Count);
        int skipped = 0;

        foreach (SqlRow row in rows)
        {
            string name = row.GetString(varName);

            // ISNULL(NLK, Report.LabClassName(LabClassId)) can still be empty when both are, and an
            // empty variable name can never match a column.  The Delphi stored it under the empty
            // key and left it there harmlessly; CaptionDictionary rejects it outright, so drop it
            // here rather than letting one bad reference row throw the whole load away.
            if (name.Length == 0)
            {
                skipped++;

                continue;
            }

            captions.Add(new CaptionRecord
            {
                VarName = name,
                Title = row.GetString(caption),
                Description = row.GetString(description),
            });
        }

        if (skipped > 0)
        {
            _log.LogWarning("Skipped {Skipped} lab caption rows with no variable name.", skipped);
        }

        _log.LogDebug("Read {Count} lab captions.", captions.Count);

        return captions;
    }
}
