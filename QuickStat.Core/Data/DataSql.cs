namespace QuickStat.Data;

/// <summary>
/// The statements the connect-and-log-in sequence issues, transcribed from the Delphi constants.
/// </summary>
/// <remarks>
/// One place, so that a change to a procedure name is one edit and so the golden comparison against
/// the Delphi is a diff rather than a hunt.
/// </remarks>
internal static class DataSql
{
    /// <summary>
    /// <c>SELECT @@SERVERNAME, DB_NAME()</c> - Delphi <c>QRY_SERVER_AND_DATABASE</c>
    /// (<c>Emetra.Database.Simple.pas:138</c>).
    /// </summary>
    /// <remarks>
    /// Column aliases added; the Delphi read <c>Fields[0]</c> and <c>Fields[1]</c> and so does this
    /// port, but a named result set is far easier to read in a trace.
    /// </remarks>
    public const string ServerAndDatabase = "SELECT @@SERVERNAME AS ServerName, DB_NAME() AS DatabaseName";

    /// <summary>Delphi <c>QRY_PROPERTIES</c> (<c>Emetra.Database.Info.pas:82</c>), verbatim.</summary>
    public const string ServerProperties =
        "SELECT SERVERPROPERTY('ProductVersion') AS ProductVersion,SERVERPROPERTY('Collation') AS Collation," +
        "SERVERPROPERTY('ServerName') AS ServerName,HOST_NAME() AS WorkstationName, DB_NAME() AS DatabaseName";

    /// <summary>Delphi <c>QRY_DATABASE_INFO</c> (<c>Emetra.Database.Info.pas:81</c>).</summary>
    /// <remarks>
    /// Issued once. The Delphi issued it twice per login - once from <c>TDatabaseInfo.Refresh</c>
    /// and again from <c>TCRFEventMapper.AfterLogin</c> (<c>CRF.Input.EventMap.pas:47</c>), which
    /// wanted only <c>EventScale</c> from the very same row.
    /// </remarks>
    public const string DatabaseInfo = "EXEC dbo.GetDatabaseInfo";

    /// <summary>Delphi <c>QRY_MY_STUDYUSER</c> (<c>CRF.SQL.pas:69</c>).</summary>
    /// <remarks>
    /// Issued once. The Delphi issued it three times per project switch: from
    /// <c>TActiveUser.AfterLogin</c>, then again from <c>AfterStudyChange</c>, then again from the
    /// second <c>Populate</c> (<c>CRF.Context.ActiveUser.pas:134</c>, <c>:148</c>, <c>:210</c>).
    /// </remarks>
    public const string StudyAndUser = "EXEC dbo.GetStudyAndUser :StudyName";

    /// <summary>Delphi <c>QRY_STUDY_ID</c> (<c>CRF.SQL.pas:21</c>), verbatim.</summary>
    /// <remarks>
    /// Now a fallback rather than a second authority: it runs only when
    /// <c>dbo.GetStudyAndUser</c> did not yield a study id.
    /// </remarks>
    public const string StudyId = "SELECT StudyId FROM dbo.Study WHERE StudName=:StudyName";

    /// <summary>Delphi <c>QRY_ADD_SESSION</c> (<c>CRF.SQL.pas:160</c>), verbatim.</summary>
    public const string AddSession = "EXEC dbo.AddSession :StudyId,:CompName,:CompUser,:CompTime,:AppVer";

    /// <summary>Delphi <c>CMD_CLOSE_SESSION</c> (<c>CRF.SQL.pas:161</c>), verbatim.</summary>
    public const string CloseSession = "EXEC dbo.CloseSession :SessId,:Updates,:Inserts";
}
