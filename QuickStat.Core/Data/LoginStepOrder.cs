namespace QuickStat.Data;

/// <summary>
/// The <see cref="ILoginStep.Order"/> values, in one place so the sequence can be read at a glance.
/// </summary>
/// <remarks>
/// Gaps of a hundred, so a later phase can insert a stage without renumbering anything. The
/// ordering is the point of the type: the Delphi's five <c>ILoginObserver</c>s fired in whatever
/// order they happened to be registered in, which is how <c>SET DATEFORMAT ymd</c> ended up running
/// after the first user query (<c>Docs/Port/01-data-access.md</c> §1.4).
/// </remarks>
internal static class LoginStepOrder
{
    /// <summary>Session options and server identity.</summary>
    public const int SessionOptions = 0;

    /// <summary>Server properties and <c>dbo.GetDatabaseInfo</c>.</summary>
    public const int DatabaseInfo = 100;

    /// <summary><c>dbo.GetStudyAndUser</c>, which also yields the study id.</summary>
    public const int ActiveUser = 200;

    /// <summary>Study-id fallback and <c>dbo.AddSession</c>.</summary>
    public const int StudySession = 300;
}
