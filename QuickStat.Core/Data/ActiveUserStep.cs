using Microsoft.Extensions.Logging;
using QuickStat.Diagnostics;

namespace QuickStat.Data;

/// <summary>
/// Step 200: <c>EXEC dbo.GetStudyAndUser :StudyName</c>, read once.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TActiveUser.Populate</c> (<c>CRF.Context.ActiveUser.pas:201-220</c>) feeding
/// <c>TStudyUser.Load</c> (<c>CRF.User.StudyUser.pas:232-248</c>) and <c>TActiveUser.Load</c>
/// (<c>CRF.Context.ActiveUser.pas:222-240</c>). The same procedure ran three times per project
/// switch: from <c>AfterLogin</c>, again from <c>AfterStudyChange</c>, and again from the
/// <c>Populate</c> that <c>AfterStudyChange</c> called (<c>:134</c>, <c>:148</c>, <c>:210</c>).
/// </para>
/// <para>
/// This is also the first place the study id becomes known
/// (<c>CRF.Context.ActiveUser.pas:236</c>), which is why the fallback lookup in
/// <see cref="StudySessionStep"/> only runs when this one yields zero.
/// </para>
/// <para>
/// <strong>The profession/work-site crash is replaced, not reproduced.</strong> When
/// <c>ProfName</c> or <c>CenterName</c> was empty the Delphi raised a modal dialog and then called
/// <c>SelectProfession</c> / <c>SelectCenter</c>, which dereference <c>GlobalPickList</c> - never
/// assigned anywhere in QuickStat (<c>Emetra.Database.Dialog.Interfaces.pas:60</c>). In Release that
/// is an access violation in the middle of login. QuickStat needs neither <c>ProfId</c> nor
/// <c>CenterId</c> for anything it does, so the condition is recorded on the context and reported.
/// </para>
/// </remarks>
internal sealed class ActiveUserStep : ILoginStep
{
    // CRF.SQL.Fields.pas and Emetra.Person.SQL.pas.
    private const string UserIdField = "UserId";
    private const string UserNameField = "UserName";
    private const string PersonIdField = "PersonId";
    private const string FullNameField = "FullName";
    private const string FirstNameField = "FstName";
    private const string MiddleNameField = "MidName";
    private const string LastNameField = "LstName";
    private const string SignatureField = "Signature";
    private const string HprNumberField = "HPRNo";
    private const string ProfessionIdField = "ProfId";
    private const string ProfessionNameField = "ProfName";
    private const string ProfessionTypeField = "ProfType";
    private const string CenterIdField = "CenterId";
    private const string CenterNameField = "CenterName";
    private const string GroupIdField = "GroupId";
    private const string GroupNameField = "GroupName";
    private const string SuperuserField = "IsSuperuser";
    private const string DatabaseOwnerField = "IsDbOwner";
    private const string SingleGroupUserField = "IsSingleGroupUser";
    private const string ShowMyGroupField = "ShowMyGroup";
    private const string BlockRulesField = "BlockRules";
    private const string RelationCountField = "RelationCount";
    private const string CaseListField = "CaseList";
    private const string StudyIdField = "StudyId";

    private readonly ILogger<ActiveUserStep> _logger;

    public ActiveUserStep(ILogger<ActiveUserStep> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Active user";

    /// <inheritdoc />
    public int Order => LoginStepOrder.ActiveUser;

    /// <inheritdoc />
    public async Task ExecuteAsync(LoginContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Progress?.Report(new OperationProgress("Connecting", "Reading user information ...", null));

        SqlResultSet result = await context.Sql.QueryAsync(
            new SqlRequest
            {
                CommandText = DataSql.StudyAndUser,
                Values = [context.StudyName],
                IsIdempotent = true,
                Label = "Study and user",
            },
            cancellationToken).ConfigureAwait(false);

        if (result.IsEmpty)
        {
            // TStudyUser.Load over an empty dataset read nulls, i.e. zeros and empty strings, and
            // carried on. Reproduce that rather than failing the login, but do not pretend the
            // profile is complete.
            _logger.LogWarning(
                "dbo.GetStudyAndUser returned no row for study '{StudyName}'.", context.StudyName);

            context.User = new StudyUser { UserId = 0, UserName = "" };
            context.HasIncompleteUserProfile = true;
            return;
        }

        StudyUser user = Read(result, result[0]);

        context.User = user;
        context.StudyId = ReadInt32(result, result[0], StudyIdField);

        if (string.IsNullOrEmpty(user.ProfessionName) || string.IsNullOrEmpty(user.CenterName))
        {
            context.HasIncompleteUserProfile = true;

            _logger.LogWarning(
                "User {UserName} has no profession and/or no work site registered in FastTrak. " +
                "QuickStat does not need either, so the login continues.",
                user.UserName);
        }
    }

    private static StudyUser Read(SqlResultSet result, SqlRow row) => new()
    {
        UserId = ReadInt32(result, row, UserIdField),
        UserName = ReadString(result, row, UserNameField),
        PersonId = ReadInt32(result, row, PersonIdField),
        FullName = ReadFullName(result, row),

        // fHPRNo is an integer in the Delphi (CRF.User.StudyUser.pas:239); the contract types it as
        // a string, and TField.AsString renders an integer column, so nothing is lost.
        HprNumber = ReadString(result, row, HprNumberField),
        Signature = ReadString(result, row, SignatureField),

        ProfessionId = ReadInt32(result, row, ProfessionIdField),
        ProfessionName = ReadString(result, row, ProfessionNameField),
        ProfessionType = ReadString(result, row, ProfessionTypeField),

        CenterId = ReadInt32(result, row, CenterIdField),
        CenterName = ReadString(result, row, CenterNameField),
        GroupId = ReadInt32(result, row, GroupIdField),
        GroupName = ReadString(result, row, GroupNameField),

        // The roles are integer columns compared against 1, not bit columns
        // (CRF.Context.ActiveUser.pas:231-233).
        IsSuperuser = ReadInt32(result, row, SuperuserField) == 1,
        IsDatabaseOwner = ReadInt32(result, row, DatabaseOwnerField) == 1,
        IsSingleGroupUser = ReadInt32(result, row, SingleGroupUserField) == 1,

        ShowMyGroup = ReadBoolean(result, row, ShowMyGroupField),
        BlockRules = ReadInt32(result, row, BlockRulesField),

        // Delphi default is -1, not 0 (CRF.Context.ActiveUser.pas:235).
        RelationCount = ReadInt32(result, row, RelationCountField, -1),
        CaseList = ReadInt32(result, row, CaseListField),
    };

    /// <summary>
    /// Uses the <c>FullName</c> column when the procedure projects one, otherwise composes it the
    /// way <c>TPerson.Get_FullName</c> does (<c>Emetra.Person.pas:193-199</c>): first name, middle
    /// initial with a full stop when there is a middle name, last name.
    /// </summary>
    private static string ReadFullName(SqlResultSet result, SqlRow row)
    {
        string full = ReadString(result, row, FullNameField);

        if (full.Length > 0)
        {
            return full;
        }

        string first = ReadString(result, row, FirstNameField);
        string middle = ReadString(result, row, MiddleNameField);
        string last = ReadString(result, row, LastNameField);

        return middle.Length > 0
            ? $"{first} {middle[0]}. {last}"
            : $"{first} {last}".Trim();
    }

    // FindField semantics, not FieldByName: dbo.GetStudyAndUser projects different columns on
    // different schema versions, and the Delphi's ReadInteger/ReadString helpers tolerated absence.
    private static int ReadInt32(SqlResultSet result, SqlRow row, string column, int defaultValue = 0)
    {
        int ordinal = result.IndexOf(column);
        return ordinal < 0 ? defaultValue : row.GetInt32(ordinal, defaultValue);
    }

    private static string ReadString(SqlResultSet result, SqlRow row, string column)
    {
        int ordinal = result.IndexOf(column);
        return ordinal < 0 ? "" : row.GetString(ordinal);
    }

    private static bool ReadBoolean(SqlResultSet result, SqlRow row, string column)
    {
        int ordinal = result.IndexOf(column);
        return ordinal >= 0 && row.GetBoolean(ordinal);
    }
}
