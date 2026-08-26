using System.Globalization;
using System.Text.RegularExpressions;
using QuickStat.Data;

namespace QuickStat.Domain.Patients;

/// <summary>
/// Decides which statement a free-text patient search runs, from the shape of the text.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TPatientList.TryFindPeople</c> (<c>CRF.Patient.List.pas:350-386</c>) with the patterns
/// from <c>Emetra.Person.Interfaces.pas:177-185</c>. The order of the tests is the contract - a
/// national identity number is checked before a date, and a bare integer before a name - so it is
/// preserved exactly.
/// </para>
/// <para>
/// QuickStat exposes no patient search today, so nothing calls this yet; it is here because the unit
/// is being ported and because <see cref="IPatientRepository.SearchAsync"/> promises it.
/// </para>
/// </remarks>
internal static partial class PatientSearch
{
    /// <summary>
    /// Builds the search request, or <see langword="null"/> when the text matches no rule at all.
    /// </summary>
    /// <param name="studyId">Current study; the enrolled-patients join uses it.</param>
    /// <param name="searchText">Raw text as typed.</param>
    /// <param name="now">Reference instant for the two-digit-year rule. Defaults to now.</param>
    /// <returns>The request to run, or <see langword="null"/> for "search nothing".</returns>
    public static SqlRequest? Build(int studyId, string? searchText, DateTime? now = null)
    {
        string text = (searchText ?? "").Trim();
        if (text.Length == 0)
        {
            return null;
        }

        bool isPersonId = int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int personId);
        Match dateMatch = DatePattern().Match(text);
        Match nameMatch = NamePattern().Match(text);

        DateTime dob = default;
        bool hasDob = dateMatch.Success && TryParseDateOfBirth(dateMatch.Value, out dob, now);

        if (SplitNationalIdPattern().IsMatch(text))
        {
            return PatientSql.Search(
                PatientSql.PersonByNationalId,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    [PatientSql.ParamNationalId] = WhitespacePattern().Replace(text, ""),
                });
        }

        if (DobAndNamePattern().IsMatch(text) && nameMatch.Success && dateMatch.Success)
        {
            return PatientSql.Search(
                PatientSql.StudyPersonByDobAndName,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    [PatientSql.ParamStudyId] = studyId,
                    [PatientSql.ParamDob] = dob,
                    [PatientSql.ParamPartialLastName] = nameMatch.Value + "%",
                });
        }

        if (hasDob)
        {
            // CRF.Patient.List.pas:376 formats the date as 'yyyy-mm-dd' before binding it. That is a
            // workaround for ADO marshalling a Variant date through OLE Automation, where the server
            // then reparsed it under the session's DATEFORMAT. Microsoft.Data.SqlClient sends a typed
            // date, so the string round trip is not only unnecessary, it would reintroduce exactly the
            // ambiguity the workaround was fighting.
            return PatientSql.Search(
                PatientSql.StudyPersonByDob,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    [PatientSql.ParamStudyId] = studyId,
                    [PatientSql.ParamDob] = dob,
                });
        }

        if (isPersonId && personId > 0)
        {
            return PatientSql.Search(
                PatientSql.PersonById,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    [PatientSql.ParamPersonId] = personId,
                });
        }

        if (nameMatch.Success)
        {
            return PatientSql.Search(
                PatientSql.StudyPersonByLastName,
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    [PatientSql.ParamStudyId] = studyId,
                    [PatientSql.ParamSearchFor] = text + "%",
                });
        }

        return null;
    }

    /// <summary>Parses the date prefix <see cref="DatePattern"/> matched.</summary>
    /// <param name="dateText">The matched text: <c>ddmmyy</c> or <c>ddmmyyyy</c>, dots optional.</param>
    /// <param name="value">The parsed date.</param>
    /// <param name="now">Reference instant for the two-digit-year rule. Defaults to now.</param>
    /// <returns><see langword="true"/> when the text is a real date.</returns>
    /// <remarks>
    /// <para>
    /// <c>TPatientList.ParseDateOfBirth</c> delegates to <c>GetDate</c>
    /// (<c>Emetra.Dates.Utils.pas</c>), which is a cascade of <c>StrToDate</c> attempts whose
    /// behaviour depends on the operating system's <c>ShortDateFormat</c> and date separator. Those
    /// branches are not reproduced: they are not deterministic, they are not testable, and none of
    /// them can be reached from this call site, because the only input is a match of
    /// <see cref="DatePattern"/> and that pattern admits exactly the two layouts handled here.
    /// </para>
    /// <para>
    /// What <em>is</em> reproduced is the last line of <c>GetDate</c>: a date more than a year in the
    /// future came from a two-digit year and belongs a century earlier.
    /// </para>
    /// </remarks>
    public static bool TryParseDateOfBirth(string dateText, out DateTime value, DateTime? now = null)
    {
        value = default;

        string digits = (dateText ?? "").Replace(".", "", StringComparison.Ordinal);
        if (digits.Length != 6 && digits.Length != 8)
        {
            return false;
        }

        int day = int.Parse(digits.AsSpan(0, 2), CultureInfo.InvariantCulture);
        int month = int.Parse(digits.AsSpan(2, 2), CultureInfo.InvariantCulture);
        int year = digits.Length == 8
            ? int.Parse(digits.AsSpan(4, 4), CultureInfo.InvariantCulture)
            : 2000 + int.Parse(digits.AsSpan(4, 2), CultureInfo.InvariantCulture);

        if (month is < 1 or > 12 || year is < 1 or > 9999 || day < 1 || day > DateTime.DaysInMonth(year, month))
        {
            return false;
        }

        value = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
        if (value > (now ?? DateTime.Now).AddDays(365))
        {
            value = value.AddYears(-100);
        }

        return true;
    }

    /// <summary><c>RGX_DATE</c>. Unanchored at the end, so it matches a date <em>prefix</em>.</summary>
    [GeneratedRegex(@"^[0123]\d\.?[01]\d\.?(18|19|20)?\d{2}")]
    private static partial Regex DatePattern();

    /// <summary><c>RGX_SPLIT_NATIONAL_ID</c>: eleven digits, optionally split six plus five.</summary>
    [GeneratedRegex(@"^\d{6}\s?\d{5}$")]
    private static partial Regex SplitNationalIdPattern();

    /// <summary><c>RGX_NAME</c>: a run of letters, hyphenated parts allowed.</summary>
    [GeneratedRegex(@"(\p{L}+(\-\p{L}+)*)")]
    private static partial Regex NamePattern();

    /// <summary><c>RGX_DOB_AND_NAME</c>.</summary>
    [GeneratedRegex(@"^[0123]\d\.?[01]\d\.?(18|19|20)?\d{2}\s+(\p{L}+(\-\p{L}+)*)$")]
    private static partial Regex DobAndNamePattern();

    /// <summary>Matches the whitespace stripped out of a split national identity number.</summary>
    [GeneratedRegex(@"\s")]
    private static partial Regex WhitespacePattern();
}
