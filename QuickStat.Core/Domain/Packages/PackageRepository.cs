using Microsoft.Extensions.Logging;
using QuickStat.Data;

namespace QuickStat.Domain.Packages;

/// <summary>
/// Reads, writes and deletes packaged selections in <c>Report.QuickStat</c>.
/// </summary>
/// <remarks>
/// Delphi: <c>TPackagedSelection</c> (<c>QuickStat.Selection.pas</c>), which carried its own SQL, plus
/// <c>TfrmQuickStat.LoadPackagedSelections</c> (<c>MainQuickStat.pas:816-841</c>). Packages are
/// server-side state, not a settings file, which is why they are a repository.
/// </remarks>
internal sealed class PackageRepository : IPackageRepository
{
    private readonly ISqlExecutor _sql;
    private readonly ILogger<PackageRepository> _log;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="sql">The executor.</param>
    /// <param name="log">Where diagnostics go.</param>
    public PackageRepository(ISqlExecutor sql, ILogger<PackageRepository> log)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentNullException.ThrowIfNull(log);

        _sql = sql;
        _log = log;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PackagedSelection>> GetPackagesAsync(
        int studyId,
        CancellationToken cancellationToken = default)
    {
        SqlResultSet rows = await _sql.QueryAsync(PackageSql.List(studyId), cancellationToken).ConfigureAwait(false);

        // QuickStat.Selection.pas:99-110 reads all six columns with FieldByName. They come from a
        // fixed FastTrak table rather than from author-supplied SQL, so the strict read stays strict.
        int oStudyId = rows.GetOrdinal(PackageSql.ColStudyId);
        int oRowId = rows.GetOrdinal(PackageSql.ColRowId);
        int oProcId = rows.GetOrdinal(PackageSql.ColProcId);
        int oTitle = rows.GetOrdinal(PackageSql.ColTitle);
        int oComment = rows.GetOrdinal(PackageSql.ColComment);
        int oDataElements = rows.GetOrdinal(PackageSql.ColDataElements);

        List<PackagedSelection> packages = [];
        foreach (SqlRow row in rows)
        {
            packages.Add(new PackagedSelection
            {
                StudyId = row.GetInt32(oStudyId),
                RowId = row.GetInt32(oRowId),
                PopulationId = row.GetInt32(oProcId),
                Title = row.GetString(oTitle),
                Comment = row.GetString(oComment),
                CollectorNames = CollectorNameList.Parse(row.GetString(oDataElements)),
            });
        }

        return packages;
    }

    /// <inheritdoc />
    public async Task<PackagedSelection> SaveAsync(
        PackagedSelection package,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);

        // Report.AddQuickStat is a query, not a command: TPackagedSelection.Save reads the new RowId
        // out of its result set (QuickStat.Selection.pas:112-120).
        SqlResultSet rows = await _sql.QueryAsync(PackageSql.Save(package), cancellationToken).ConfigureAwait(false);

        int ordinal = rows.IndexOf(PackageSql.ColRowId);
        if (ordinal < 0 || rows.Count == 0)
        {
            // The Delphi silently stored zero here, because reading a field on an empty dataset yields
            // null. Keeping the incoming row id loses nothing and says what happened.
            _log.LogWarning(
                "Report.AddQuickStat returned no {RowIdColumn}, so the saved package \"{Title}\" has no server row id.",
                PackageSql.ColRowId,
                package.Title);
            return package;
        }

        return package with { RowId = rows[0].GetInt32(ordinal) };
    }

    /// <inheritdoc />
    public Task DeleteAsync(int rowId, CancellationToken cancellationToken = default) =>
        _sql.ExecuteAsync(PackageSql.Delete(rowId), cancellationToken);
}
