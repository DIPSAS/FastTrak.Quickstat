namespace QuickStat.Data;

/// <summary>
/// The logged-in user as <c>EXEC dbo.GetStudyAndUser :StudyName</c> reports them.
/// </summary>
/// <remarks>
/// Delphi: <c>TStudyUser.Load</c> (<c>CRF.User.StudyUser.pas:232-248</c>) plus
/// <c>TActiveUser.Load</c> (<c>CRF.Context.ActiveUser.pas:222-240</c>), which between them read the
/// procedure's result set three separate times per connect. Read once here.
/// </remarks>
public sealed record StudyUser
{
    /// <summary><c>dbo.Users.UserId</c>.</summary>
    public required int UserId { get; init; }

    /// <summary>Login name.</summary>
    public required string UserName { get; init; }

    /// <summary>The user's own <c>PersonId</c>, if they are also registered as a person.</summary>
    public int PersonId { get; init; }

    /// <summary>Display name.</summary>
    public string FullName { get; init; } = "";

    /// <summary>Signature initials.</summary>
    public string Signature { get; init; } = "";

    /// <summary>Norwegian health-personnel register number.</summary>
    public string HprNumber { get; init; } = "";

    /// <summary>Profession id; zero when unset.</summary>
    public int ProfessionId { get; init; }

    /// <summary>Profession name. Empty means the profile is incomplete - see
    /// <see cref="SessionContext.HasIncompleteUserProfile"/>.</summary>
    public string ProfessionName { get; init; } = "";

    /// <summary>Profession type code.</summary>
    public string ProfessionType { get; init; } = "";

    /// <summary>Work-site id.</summary>
    public int CenterId { get; init; }

    /// <summary>Work-site name. Empty means the profile is incomplete.</summary>
    public string CenterName { get; init; } = "";

    /// <summary>Group / ward id.</summary>
    public int GroupId { get; init; }

    /// <summary>Group / ward name.</summary>
    public string GroupName { get; init; } = "";

    /// <summary>Member of the database owner role.</summary>
    public bool IsDatabaseOwner { get; init; }

    /// <summary>FastTrak superuser.</summary>
    public bool IsSuperuser { get; init; }

    /// <summary>Restricted to a single group.</summary>
    public bool IsSingleGroupUser { get; init; }

    /// <summary>Whether the user's own group is shown by default.</summary>
    public bool ShowMyGroup { get; init; }

    /// <summary>Access-control rule mask.</summary>
    public int BlockRules { get; init; }

    /// <summary>Number of patient relations.</summary>
    public int RelationCount { get; init; }

    /// <summary>Case-list selector the user last used.</summary>
    public int CaseList { get; init; }
}
