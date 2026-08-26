using QuickStat.Diagnostics;

namespace QuickStat.Data;

/// <summary>
/// Mutable state threaded through the <see cref="ILoginStep"/> pipeline; frozen into a
/// <see cref="SessionContext"/> when the last step succeeds.
/// </summary>
/// <remarks>
/// Mutable on purpose - it is a builder. The immutable result is what the rest of the application
/// sees, so no consumer can observe a half-built session.
/// </remarks>
public sealed class LoginContext
{
    /// <summary>Study short name from <see cref="QuickStat.Configuration.QuickStatConnection.StudyName"/>.</summary>
    public required string StudyName { get; init; }

    /// <summary>The executor on the freshly opened connection.</summary>
    public required ISqlExecutor Sql { get; init; }

    /// <summary>Per-step progress, or <see langword="null"/> when nobody is watching.</summary>
    public IProgress<OperationProgress>? Progress { get; init; }

    /// <summary>
    /// <c>dbo.Study.StudyId</c>. Resolved once.
    /// </summary>
    /// <remarks>
    /// The Delphi resolved it three times per connect - from <c>dbo.GetStudyAndUser</c>, then from
    /// <c>dbo.Study</c>, then again for the grid (<c>CRF.Context.ActiveUser.pas:236</c>,
    /// <c>CRF.Context.Session.pas:194</c>, <c>EPR.QA.Matrix.pas:434</c>).
    /// </remarks>
    public int StudyId { get; set; }

    /// <summary><c>dbo.AddSession</c> row id.</summary>
    public int SessionId { get; set; }

    /// <summary>Filled by the active-user step.</summary>
    public StudyUser? User { get; set; }

    /// <summary>Filled by the database-info step; may report <c>DbVersion = -1</c> after a swallowed failure.</summary>
    public DatabaseInfo? Database { get; set; }

    /// <summary><c>@@SERVERNAME</c>.</summary>
    public string? ServerName { get; set; }

    /// <summary><c>DB_NAME()</c>.</summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Set when the logged-in user has no profession or no work site registered in FastTrak.
    /// </summary>
    /// <remarks>
    /// In the Delphi this state dereferenced the never-assigned <c>GlobalPickList</c> and produced
    /// an access violation in Release (<c>CRF.Context.ActiveUser.pas:209-218</c>). QuickStat does
    /// not actually need <c>ProfId</c> or <c>CenterId</c> for anything, so the port continues and
    /// reports the condition instead.
    /// </remarks>
    public bool HasIncompleteUserProfile { get; set; }
}
