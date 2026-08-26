using System.Globalization;
using System.Text;
using QuickStat.Configuration;
using QuickStat.Data;

namespace QuickStat.Domain.Patients;

/// <summary>
/// Every statement, parameter name and result column the patient loader uses, in one place.
/// </summary>
/// <remarks>
/// Statements are the Delphi constants verbatim - <c>CRF.SQL.pas:170</c>,
/// <c>Emetra.Person.SQL.pas:9-16, 32-35</c> and <c>CRF.Patient.List.pas:103-108</c> - still in
/// <c>:Name</c> form. The single exception is national-id recovery, which replaces the upstream
/// string-concatenated <c>IN ( %s )</c> list; see <see cref="NationalIdRequests"/>.
/// </remarks>
internal static class PatientSql
{
    /// <summary><c>QRY_GET_CASELIST</c> - the default list loaded on a study change.</summary>
    public const string GetCaseList = "EXEC dbo.GetCaseList :StudyId";

    /// <summary><c>SELECT_PERSON</c>. Note the trailing space; it is part of the constant.</summary>
    private const string SelectPerson = "SELECT p.* FROM dbo.Person p ";

    /// <summary><c>TAIL_ORDER_BY</c>. Note the leading space.</summary>
    private const string TailOrderBy = " ORDER BY p.LstName, p.FstName";

    /// <summary><c>JOIN_STUDY</c> - searches are limited to patients enrolled in the study (#498565).</summary>
    private const string JoinStudy =
        " JOIN dbo.StudCase sc ON sc.StudyId=:StudyId AND sc.PersonId = p.PersonId ";

    /// <summary><c>QRY_PERSON_BY_ID</c>.</summary>
    public const string PersonById = SelectPerson + "WHERE p.PersonId = :PersonId" + TailOrderBy;

    /// <summary><c>QRY_PERSON_BY_NATID</c>.</summary>
    public const string PersonByNationalId = SelectPerson + "WHERE p.NationalId = :NationalId" + TailOrderBy;

    /// <summary><c>QRY_STUDY_PERSON_BY_DOB</c>.</summary>
    public const string StudyPersonByDob = SelectPerson + JoinStudy + "WHERE p.DOB = :DOB" + TailOrderBy;

    /// <summary><c>QRY_STUDY_PERSON_BY_DOB_NAME</c>.</summary>
    public const string StudyPersonByDobAndName =
        SelectPerson + JoinStudy + "WHERE p.DOB = :DOB AND p.LstName LIKE :PartialLastName" + TailOrderBy;

    /// <summary><c>QRY_STUDY_PERSON_BY_LAST_NAME</c>.</summary>
    public const string StudyPersonByLastName =
        SelectPerson + JoinStudy + "WHERE p.LstName LIKE :SearchFor" + TailOrderBy;

    /// <summary>Placeholder for the current study.</summary>
    public const string ParamStudyId = "StudyId";

    /// <summary>Placeholder for a single patient.</summary>
    public const string ParamPersonId = "PersonId";

    /// <summary>Placeholder for a national identity number.</summary>
    public const string ParamNationalId = "NationalId";

    /// <summary>Placeholder for a date of birth.</summary>
    public const string ParamDob = "DOB";

    /// <summary>Placeholder for a <c>LIKE</c> pattern on the last name, used with a date of birth.</summary>
    public const string ParamPartialLastName = "PartialLastName";

    /// <summary>Placeholder for a <c>LIKE</c> pattern on the last name.</summary>
    public const string ParamSearchFor = "SearchFor";

    /// <summary>Placeholder carrying the person-id table-valued argument.</summary>
    public const string ParamIdList = "Ids";

    /// <summary>Prefix of the generated placeholders in the chunked fallback: <c>:p0</c>, <c>:p1</c>, …</summary>
    public const string ChunkParameterPrefix = "p";

    /// <summary><c>PersonId</c>. Required in a population result set.</summary>
    public const string ColPersonId = "PersonId";

    /// <summary><c>FullName</c>. Required in a population result set; parsed by <see cref="PersonName.Parse"/>.</summary>
    public const string ColFullName = "FullName";

    /// <summary><c>DOB</c>.</summary>
    public const string ColDob = "DOB";

    /// <summary><c>NationalId</c>. Absent from most population procedures.</summary>
    public const string ColNationalId = "NationalId";

    /// <summary><c>GenderId</c>.</summary>
    public const string ColGenderId = "GenderId";

    /// <summary><c>GroupId</c>.</summary>
    public const string ColGroupId = "GroupId";

    /// <summary><c>GroupName</c>.</summary>
    public const string ColGroupName = "GroupName";

    /// <summary><c>StatusId</c>.</summary>
    public const string ColStatusId = "StatusId";

    /// <summary><c>StatusText</c>.</summary>
    public const string ColStatusText = "StatusText";

    /// <summary><c>InfoText</c> - optional, and it overwrites <c>StatusText</c> when present.</summary>
    public const string ColInfoText = "InfoText";

    /// <summary><c>TestCase</c>.</summary>
    public const string ColTestCase = "TestCase";

    /// <summary><c>FstName</c> - read directly by the person search, which has no <c>FullName</c>.</summary>
    public const string ColFirstName = "FstName";

    /// <summary><c>LstName</c> - read directly by the person search, which has no <c>FullName</c>.</summary>
    public const string ColLastName = "LstName";

    /// <summary>Log label for a population load.</summary>
    public const string PopulationLabel = "LoadPopulation";

    /// <summary>Log label for the default case list.</summary>
    public const string CaseListLabel = "GetCaseList";

    /// <summary>Log label for national-id recovery.</summary>
    public const string NationalIdsLabel = "NationalIds";

    /// <summary>Log label for a patient search.</summary>
    public const string SearchLabel = "SearchPatients";

    /// <summary>
    /// The Delphi default for <c>ReadInteger</c>, which applies to a missing <em>and</em> to a null
    /// column (<c>Emetra.Classes.Subject.Stored.pas:245</c>).
    /// </summary>
    public const int MissingIntegerValue = -1;

    /// <summary>Builds the request that runs a population's own statement.</summary>
    /// <param name="queryText">The population's <c>SqlText</c>, executed verbatim.</param>
    /// <param name="parameters">Values from <see cref="Populations.IQueryParameterResolver"/>.</param>
    /// <returns>The request.</returns>
    /// <remarks>
    /// Bound by name rather than positionally. The Delphi bound positionally
    /// (<c>Emetra.Database.Simple.pas:415-433</c>), which works only because both parsers enumerate
    /// placeholders left to right and breaks outright on a repeated placeholder - something arbitrary
    /// server-authored SQL is free to contain.
    /// </remarks>
    public static SqlRequest LoadPopulation(string queryText, IReadOnlyDictionary<string, object?> parameters)
    {
        Dictionary<string, object?> named = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, object?> pair in parameters)
        {
            named[pair.Key] = pair.Value;
        }

        return new SqlRequest
        {
            CommandText = queryText,
            NamedValues = named,
            IsIdempotent = true,
            Label = PopulationLabel,
        };
    }

    /// <summary>Builds the default case-list request.</summary>
    /// <param name="studyId">Current study.</param>
    /// <returns>The request.</returns>
    public static SqlRequest CaseList(int studyId) => new()
    {
        CommandText = GetCaseList,
        NamedValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [ParamStudyId] = studyId,
        },
        IsIdempotent = true,
        Label = CaseListLabel,
    };

    /// <summary>
    /// Builds the request or requests that recover national identity numbers for a set of patients.
    /// </summary>
    /// <param name="personIds">The patients. Any number of them, duplicates tolerated.</param>
    /// <param name="options">Supplies the table type, its column, and the fallback batch size.</param>
    /// <returns>
    /// One request when a table-valued parameter is available, otherwise one per batch. Empty when
    /// there is nothing to look up.
    /// </returns>
    /// <remarks>
    /// <para>
    /// PORT-PLAN.md §7.3 and R2. Upstream built
    /// <c>… WHERE PersonId IN ( %s ) AND NOT NationalId IS NULL</c> by concatenating the ids into the
    /// statement text, which breaks on SQL Server's 2 100-parameter ceiling the moment it is
    /// parameterised, defeats plan caching, and produces <c>IN (  )</c> - a syntax error - for an
    /// empty list. None of that is carried over: an empty list makes no round trip at all, and the
    /// ids travel as one table-valued argument.
    /// </para>
    /// <para>
    /// The fallback exists only for a database where
    /// <see cref="SqlOptions.PersonIdListTypeName"/> is unset because the table type cannot be
    /// created. It still parameterises every id - the ids are never interpolated into the text.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<SqlRequest> NationalIdRequests(IReadOnlyCollection<int> personIds, SqlOptions options)
    {
        ArgumentNullException.ThrowIfNull(personIds);
        ArgumentNullException.ThrowIfNull(options);

        List<int> distinct = Distinct(personIds);
        if (distinct.Count == 0)
        {
            return [];
        }

        string? typeName = options.PersonIdListTypeName;
        if (!string.IsNullOrWhiteSpace(typeName))
        {
            return [NationalIdsByTable(distinct, typeName, options.PersonIdListColumnName)];
        }

        int batchSize = Math.Max(1, options.MaxIdsPerBatch);
        List<SqlRequest> requests = [];
        for (int offset = 0; offset < distinct.Count; offset += batchSize)
        {
            int count = Math.Min(batchSize, distinct.Count - offset);
            requests.Add(NationalIdsByChunk(distinct.GetRange(offset, count)));
        }

        return requests;
    }

    /// <summary>Builds a person-search request.</summary>
    /// <param name="commandText">One of the <c>QRY_*</c> statements above.</param>
    /// <param name="namedValues">The values for its placeholders.</param>
    /// <returns>The request.</returns>
    public static SqlRequest Search(string commandText, Dictionary<string, object?> namedValues) => new()
    {
        CommandText = commandText,
        NamedValues = namedValues,
        IsIdempotent = true,
        Label = SearchLabel,
    };

    private static SqlRequest NationalIdsByTable(List<int> personIds, string typeName, string columnName)
    {
        string commandText =
            "SELECT p.PersonId, p.NationalId FROM dbo.Person p " +
            $"JOIN :{ParamIdList} i ON i.{QuoteName(columnName)} = p.PersonId " +
            "WHERE p.NationalId IS NOT NULL";

        return new SqlRequest
        {
            CommandText = commandText,
            TableParameters =
            [
                new SqlTableParameter
                {
                    Name = ParamIdList,
                    TypeName = typeName,
                    ColumnName = columnName,
                    Values = personIds,
                },
            ],
            IsIdempotent = true,
            Label = NationalIdsLabel,
        };
    }

    private static SqlRequest NationalIdsByChunk(List<int> personIds)
    {
        StringBuilder text = new("SELECT p.PersonId, p.NationalId FROM dbo.Person p WHERE p.NationalId IS NOT NULL AND p.PersonId IN (");
        Dictionary<string, object?> values = new(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < personIds.Count; i++)
        {
            string name = ChunkParameterPrefix + i.ToString(CultureInfo.InvariantCulture);
            if (i > 0)
            {
                text.Append(", ");
            }

            text.Append(':').Append(name);
            values[name] = personIds[i];
        }

        text.Append(')');

        return new SqlRequest
        {
            CommandText = text.ToString(),
            NamedValues = values,
            IsIdempotent = true,
            Label = NationalIdsLabel,
        };
    }

    private static List<int> Distinct(IReadOnlyCollection<int> personIds)
    {
        HashSet<int> seen = new(personIds.Count);
        List<int> distinct = new(personIds.Count);
        foreach (int id in personIds)
        {
            if (seen.Add(id))
            {
                distinct.Add(id);
            }
        }

        return distinct;
    }

    /// <summary>
    /// Brackets an identifier that comes from configuration rather than from this source file.
    /// </summary>
    private static string QuoteName(string identifier) => "[" + identifier.Replace("]", "]]", StringComparison.Ordinal) + "]";
}
