using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Domain.Populations;

namespace QuickStat.Domain.Patients;

/// <summary>
/// Loads patients through <see cref="ISqlExecutor"/>: from a population, from the default case list,
/// by search, and - the part this repository exists for - by recovering national identity numbers.
/// </summary>
/// <remarks>Delphi: <c>TPatientList</c> (<c>CRF.Patient.List.pas</c>).</remarks>
internal sealed class PatientRepository : IPatientRepository
{
    /// <summary>Label used where a statement stands in for a population in an error message.</summary>
    private const string CaseListLabel = "dbo.GetCaseList";

    private readonly ISqlExecutor _sql;
    private readonly SqlOptions _options;
    private readonly ILogger<PatientRepository> _log;

    /// <summary>Initialises a new instance.</summary>
    /// <param name="sql">The executor.</param>
    /// <param name="options">Supplies the person-id table type and the fallback batch size.</param>
    /// <param name="log">Where diagnostics go.</param>
    public PatientRepository(ISqlExecutor sql, SqlOptions options, ILogger<PatientRepository> log)
    {
        ArgumentNullException.ThrowIfNull(sql);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(log);

        _sql = sql;
        _options = options;
        _log = log;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Patient>> LoadPopulationAsync(
        Population population,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(population);
        ArgumentNullException.ThrowIfNull(parameters);

        SqlRequest request = PatientSql.LoadPopulation(population.QueryText, parameters);
        SqlResultSet rows = await _sql.QueryAsync(request, cancellationToken).ConfigureAwait(false);
        return MapCohort(rows, population.ProcId, population.Title);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Patient>> GetCaseListAsync(int studyId, CancellationToken cancellationToken = default)
    {
        SqlResultSet rows = await _sql.QueryAsync(PatientSql.CaseList(studyId), cancellationToken).ConfigureAwait(false);

        // CRF.Patient.List.pas:407 sends the default case list through the very same Query method, so
        // it gets the same schema rule.
        return MapCohort(rows, 0, CaseListLabel);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, string>> GetNationalIdsAsync(
        IReadOnlyCollection<int> personIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(personIds);

        IReadOnlyList<SqlRequest> requests = PatientSql.NationalIdRequests(personIds, _options);
        if (requests.Count == 0)
        {
            // Upstream produced "WHERE PersonId IN (  )" here, which SQL Server rejects
            // (02-populations-patients.md §5.3, bug B1). No ids, no round trip.
            return ReadOnlyDictionary<int, string>.Empty;
        }

        if (requests.Count > 1)
        {
            _log.LogDebug(
                "The person-id table type is unavailable; recovering national ids in {BatchCount} batches of at most {BatchSize}.",
                requests.Count,
                _options.MaxIdsPerBatch);
        }

        Dictionary<int, string> nationalIds = [];
        foreach (SqlRequest request in requests)
        {
            SqlResultSet rows = await _sql.QueryAsync(request, cancellationToken).ConfigureAwait(false);
            int personId = rows.GetOrdinal(PatientSql.ColPersonId);
            int nationalId = rows.GetOrdinal(PatientSql.ColNationalId);

            foreach (SqlRow row in rows)
            {
                string value = row.GetString(nationalId);
                if (value.Length > 0)
                {
                    nationalIds[row.GetInt32(personId, PatientSql.MissingIntegerValue)] = value;
                }
            }
        }

        return nationalIds;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Patient>> SearchAsync(
        int studyId,
        string searchText,
        CancellationToken cancellationToken = default)
    {
        SqlRequest? request = PatientSearch.Build(studyId, searchText);
        if (request is null)
        {
            return [];
        }

        SqlResultSet rows = await _sql.QueryAsync(request, cancellationToken).ConfigureAwait(false);
        return MapPersons(rows);
    }

    /// <summary>
    /// Maps a population-shaped result set: <c>FullName</c> required and parsed, <c>InfoText</c>
    /// overriding <c>StatusText</c>.
    /// </summary>
    private static IReadOnlyList<Patient> MapCohort(SqlResultSet rows, int procId, string? populationTitle)
    {
        PopulationResultSchema.Validate(rows.Columns.Select(column => column.Name), procId, populationTitle);

        int oPersonId = rows.GetOrdinal(PatientSql.ColPersonId);
        int oFullName = rows.GetOrdinal(PatientSql.ColFullName);
        int oInfoText = rows.IndexOf(PatientSql.ColInfoText);
        OptionalColumns optional = OptionalColumns.From(rows);

        List<Patient> patients = [];
        HashSet<int> seen = [];

        foreach (SqlRow row in rows)
        {
            int personId = row.GetInt32(oPersonId, PatientSql.MissingIntegerValue);

            // EPR.QA.Matrix.pas:409-430 de-duplicates by PersonId, first row wins. Doing it here keeps
            // it in one place; ordering stays the server's, because MatrixSortOrder owns that.
            if (!seen.Add(personId))
            {
                continue;
            }

            PersonName name = PersonName.Parse(row.GetString(oFullName));

            // CRF.Patient.List.pas:317-318: when InfoText is present it replaces StatusText outright.
            string statusText = oInfoText >= 0
                ? row.GetString(oInfoText)
                : optional.ReadStatusText(row);

            patients.Add(optional.Build(row, personId, name.FirstName, name.LastName, statusText));
        }

        return patients;
    }

    /// <summary>
    /// Maps a <c>dbo.Person</c>-shaped result set. There is no <c>FullName</c> column on that table,
    /// and <c>TPatientList.Search</c> (<c>CRF.Patient.List.pas:244-274</c>) never assigns one, so the
    /// split name comes straight from <c>FstName</c> and <c>LstName</c>.
    /// </summary>
    private static IReadOnlyList<Patient> MapPersons(SqlResultSet rows)
    {
        int oPersonId = rows.IndexOf(PatientSql.ColPersonId);
        int oFirstName = rows.IndexOf(PatientSql.ColFirstName);
        int oLastName = rows.IndexOf(PatientSql.ColLastName);
        OptionalColumns optional = OptionalColumns.From(rows);

        List<Patient> patients = [];
        HashSet<int> seen = [];

        foreach (SqlRow row in rows)
        {
            int personId = oPersonId < 0
                ? PatientSql.MissingIntegerValue
                : row.GetInt32(oPersonId, PatientSql.MissingIntegerValue);

            if (!seen.Add(personId))
            {
                continue;
            }

            string firstName = oFirstName < 0 ? "" : row.GetString(oFirstName);
            string lastName = oLastName < 0 ? "" : row.GetString(oLastName);

            patients.Add(optional.Build(row, personId, firstName, lastName, optional.ReadStatusText(row)));
        }

        return patients;
    }

    /// <summary>
    /// Ordinals of the columns both mapping paths read tolerantly, resolved once per result set.
    /// </summary>
    /// <remarks>
    /// Every one of these is a <c>FindField</c> read in the Delphi
    /// (<c>Emetra.Classes.Subject.Stored.pas:186-262</c>), so a missing column is a default rather
    /// than a failure. <c>ReadInteger</c>'s default is <c>-1</c> for a missing <em>and</em> for a null
    /// column, which is why <see cref="PatientSql.MissingIntegerValue"/> is negative.
    /// </remarks>
    private readonly record struct OptionalColumns(
        int Dob,
        int NationalId,
        int GenderId,
        int GroupId,
        int GroupName,
        int StatusId,
        int StatusText,
        int TestCase)
    {
        public static OptionalColumns From(SqlResultSet rows) => new(
            rows.IndexOf(PatientSql.ColDob),
            rows.IndexOf(PatientSql.ColNationalId),
            rows.IndexOf(PatientSql.ColGenderId),
            rows.IndexOf(PatientSql.ColGroupId),
            rows.IndexOf(PatientSql.ColGroupName),
            rows.IndexOf(PatientSql.ColStatusId),
            rows.IndexOf(PatientSql.ColStatusText),
            rows.IndexOf(PatientSql.ColTestCase));

        public string ReadStatusText(SqlRow row) => StatusText < 0 ? "" : row.GetString(StatusText);

        public Patient Build(SqlRow row, int personId, string firstName, string lastName, string statusText)
        {
            int genderId = GenderId < 0
                ? PatientSql.MissingIntegerValue
                : row.GetInt32(GenderId, PatientSql.MissingIntegerValue);

            return new Patient
            {
                PersonId = personId,
                FirstName = firstName,
                LastName = lastName,

                // ReadDateTime defaults a missing column to Delphi's zero date. A nullable is the
                // honest representation of "the population did not return a date of birth", and it is
                // what Patient.DateOfBirth promises.
                DateOfBirth = Dob < 0 || row.IsNull(Dob) ? null : row.GetDateTime(Dob),

                // Never write an empty value over an existing one: the recovery query filters out
                // patients without a national id, so absence means "unknown", not "none".
                NationalId = ReadNationalId(row),

                GenderId = genderId,
                Sex = SexMapping.FromGenderId(genderId),
                GroupId = GroupId < 0 ? PatientSql.MissingIntegerValue : row.GetInt32(GroupId, PatientSql.MissingIntegerValue),
                GroupName = GroupName < 0 ? "" : row.GetString(GroupName),
                StatusId = StatusId < 0 ? PatientSql.MissingIntegerValue : row.GetInt32(StatusId, PatientSql.MissingIntegerValue),
                StatusText = statusText,
                IsTestCase = TestCase >= 0 && row.GetBoolean(TestCase),
            };
        }

        private string? ReadNationalId(SqlRow row)
        {
            if (NationalId < 0)
            {
                return null;
            }

            string value = row.GetString(NationalId);
            return value.Length == 0 ? null : value;
        }
    }
}
