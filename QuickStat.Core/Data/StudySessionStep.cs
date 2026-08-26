using System.Reflection;
using Microsoft.Extensions.Logging;
using QuickStat.Diagnostics;

namespace QuickStat.Data;

/// <summary>
/// Step 300: resolve the study id if it is still unknown, then open the session row.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TCRFStudyContext.LoadStudyProperties</c> (<c>CRF.Context.Session.pas:175-212</c>).
/// There, <c>SELECT StudyId FROM dbo.Study</c> ran unconditionally, as a second resolution of a
/// value <c>dbo.GetStudyAndUser</c> had already produced, and a third resolution followed for the
/// grid (<c>EPR.QA.Matrix.pas:434</c>). Here it is a fallback: it runs only when
/// <see cref="ActiveUserStep"/> came back with zero - which is what happens when the user is not
/// enrolled in the study - so the two can still disagree without the login silently picking one.
/// </para>
/// <para>
/// <c>@AppVer</c> now carries a real version. The Delphi always sent the empty string, because
/// nothing ever assigned <c>Session.AppVersion</c> (<c>CRF.Context.Session.pas:218</c>).
/// </para>
/// </remarks>
internal sealed class StudySessionStep : ILoginStep
{
    /// <summary>
    /// <c>dbo.AddSession</c>'s column width is not known from this repository
    /// (<c>Docs/Port/01-data-access.md</c> Part 4, item 13) and it has only ever received the empty
    /// string, so the value is capped rather than risking a truncation error at login.
    /// </summary>
    internal const int MaximumAppVersionLength = 50;

    private readonly ILogger<StudySessionStep> _logger;
    private readonly string _appVersion;

    public StudySessionStep(ILogger<StudySessionStep> logger)
        : this(logger, CurrentAppVersion())
    {
    }

    internal StudySessionStep(ILogger<StudySessionStep> logger, string appVersion)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(appVersion);

        _logger = logger;
        _appVersion = appVersion;
    }

    /// <inheritdoc />
    public string Name => "Study session";

    /// <inheritdoc />
    public int Order => LoginStepOrder.StudySession;

    /// <summary>The informational version of the entry assembly, trimmed to fit.</summary>
    /// <returns>For example <c>1.0.0</c>.</returns>
    internal static string CurrentAppVersion()
    {
        Assembly assembly = Assembly.GetEntryAssembly() ?? typeof(StudySessionStep).Assembly;

        string version =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "";

        // Strip the '+<commit sha>' build metadata that SourceLink appends.
        int plus = version.IndexOf('+', StringComparison.Ordinal);

        if (plus >= 0)
        {
            version = version[..plus];
        }

        return version.Length <= MaximumAppVersionLength ? version : version[..MaximumAppVersionLength];
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(LoginContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Progress?.Report(new OperationProgress("Connecting", "Opening session ...", null));

        if (context.StudyId <= 0)
        {
            _logger.LogInformation(
                "dbo.GetStudyAndUser did not yield a study id for '{StudyName}'; falling back to dbo.Study.",
                context.StudyName);

            SqlResultSet study = await context.Sql.QueryAsync(
                new SqlRequest
                {
                    CommandText = DataSql.StudyId,
                    Values = [context.StudyName],
                    IsIdempotent = true,
                    Label = "Study id",
                },
                cancellationToken).ConfigureAwait(false);

            if (study.Count > 0)
            {
                context.StudyId = study[0].GetInt32(0);
            }
        }

        if (context.StudyId <= 0)
        {
            _logger.LogWarning(
                "Study '{StudyName}' does not exist in this database; no session row was created.",
                context.StudyName);
            return;
        }

        SqlResultSet session = await context.Sql.QueryAsync(
            new SqlRequest
            {
                CommandText = DataSql.AddSession,
                Values =
                [
                    context.StudyId,
                    Environment.MachineName,
                    Environment.UserName,
                    DateTime.Now,
                    _appVersion,
                ],

                // dbo.AddSession inserts a row. Retrying it after a transient failure is exactly the
                // duplication hazard PORT-PLAN.md §7.2 removes.
                IsIdempotent = false,
                Label = "Add session",
            },
            cancellationToken).ConfigureAwait(false);

        if (session.Count > 0)
        {
            context.SessionId = session[0].GetInt32(0);
        }
    }
}
