namespace QuickStat.Domain.Populations;

/// <summary>Reads the population catalogue and writes the selection audit trail.</summary>
/// <remarks>Delphi: <c>TPopulationList</c> (<c>EPR.Population.List.pas:95-121</c>).</remarks>
public interface IPopulationRepository
{
    /// <summary>Loads the catalogue for one study.</summary>
    /// <param name="studyId">Current study. Zero or below yields an empty list without a round trip.</param>
    /// <param name="dbVersion">
    /// <see cref="QuickStat.Data.DatabaseInfo.DbVersion"/>. Selects the procedure overload:
    /// <c>Populations.GetStudyPopulations @StudyId, @DbVer</c> at or above
    /// <see cref="QuickStat.Data.DatabaseInfo.PopulationsWithVersionDbVersion"/>, and the
    /// single-argument form below it. Note that <c>-1</c> - the swallowed-failure value - therefore
    /// routes to the old procedure; surface that rather than treating "unknown" as "old".
    /// </param>
    /// <param name="frequentlyUsedOnly">
    /// The <c>Frequently used only</c> check box. Swaps to
    /// <c>Populations.GetPopularPopulations</c>; the <em>server</em> decides what is popular, from
    /// the audit rows written by <see cref="LogPopulationSelectedAsync"/>. It is not a client-side
    /// filter, which is why toggling it re-queries.
    /// </param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The catalogue in server order.</returns>
    Task<IReadOnlyList<Population>> GetPopulationsAsync(
        int studyId,
        int dbVersion,
        bool frequentlyUsedOnly,
        CancellationToken cancellationToken = default);

    /// <summary>Records that a population was prepared, feeding the popularity ranking.</summary>
    /// <param name="studyId">Current study.</param>
    /// <param name="procId">The population.</param>
    /// <param name="procTitle">The population's title - the Delphi passes it into the <c>:ProcDesc</c> slot.</param>
    /// <param name="elapsedMilliseconds">How long preparing it took.</param>
    /// <param name="cancellationToken">Cancels the command.</param>
    /// <returns>A task that completes when the audit row is written.</returns>
    /// <remarks>
    /// <c>EXEC dbo.AddPopulationLog</c> (<c>CRF.SQL.pas:174</c>). Fire and forget: failures are
    /// logged and swallowed (<c>EPR.VclFrame.Populations.pas:224-226</c>) and must never surface to
    /// the user or block the load. Unlike the Delphi, which wrote it after the observers and inside
    /// the same <c>try</c> - so a grid failure lost the audit row - write it unconditionally.
    /// </remarks>
    Task LogPopulationSelectedAsync(
        int studyId,
        int procId,
        string procTitle,
        long elapsedMilliseconds,
        CancellationToken cancellationToken = default);
}
