using QuickStat.Domain.Populations;

namespace QuickStat.Domain.Patients;

/// <summary>Loads patients: from a population, from the default case list, or by search.</summary>
/// <remarks>Delphi: <c>TPatientList</c> (<c>CRF.Patient.List.pas</c>).</remarks>
public interface IPatientRepository
{
    /// <summary>Runs a population's own SQL and maps the result set to patients.</summary>
    /// <param name="population">The population; its <see cref="Population.QueryText"/> is executed verbatim.</param>
    /// <param name="parameters">Values from <see cref="IQueryParameterResolver"/>.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The cohort, de-duplicated by <see cref="Patient.PersonId"/>.</returns>
    /// <exception cref="PopulationSchemaException">The result set has no <c>FullName</c> column.</exception>
    Task<IReadOnlyList<Patient>> LoadPopulationAsync(
        Population population,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default);

    /// <summary><c>EXEC dbo.GetCaseList :StudyId</c> - the default list for a study.</summary>
    /// <param name="studyId">Current study.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The study's patients.</returns>
    Task<IReadOnlyList<Patient>> GetCaseListAsync(int studyId, CancellationToken cancellationToken = default);

    /// <summary>Fetches national identity numbers for a set of patients.</summary>
    /// <param name="personIds">The patients to look up. Any number of them.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>
    /// <c>PersonId</c> to national id, containing only the patients that <em>have</em> one.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the feature behind the <c>Fully identified patients</c> option, and it is commented
    /// out in this repository (<c>MainQuickStat.pas:537-539</c>,
    /// <c>// TODO: Disse feiler, hvor er de??</c>) even though the canonical application calls it.
    /// Restoring it is Phase 4.
    /// </para>
    /// <para>
    /// Bound through a table-valued parameter, <em>not</em> the upstream string-concatenated
    /// <c>IN ( %s )</c> list. The upstream version also produces
    /// <c>WHERE PersonId IN (  )</c> - a syntax error - for an empty list, and latches an
    /// "already fetched" flag even when nothing came back, so it never retries. Neither defect is
    /// carried over (<c>Docs/Port/02-populations-patients.md</c> §5.3).
    /// </para>
    /// </remarks>
    Task<IReadOnlyDictionary<int, string>> GetNationalIdsAsync(
        IReadOnlyCollection<int> personIds,
        CancellationToken cancellationToken = default);

    /// <summary>Free-text patient search, dispatched on the shape of the text.</summary>
    /// <param name="studyId">Current study; the search is limited to patients enrolled in it.</param>
    /// <param name="searchText">National id, person id, date of birth, name, or a combination.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>Matching patients, ordered by last then first name.</returns>
    /// <remarks>
    /// <c>TPatientList.Search</c> has no caller in QuickStat today - the UI does not expose patient
    /// search. Declared because the repository is incomplete without it and step 2.3 is porting the
    /// unit anyway; no view is expected to bind to it.
    /// </remarks>
    Task<IReadOnlyList<Patient>> SearchAsync(int studyId, string searchText, CancellationToken cancellationToken = default);
}
