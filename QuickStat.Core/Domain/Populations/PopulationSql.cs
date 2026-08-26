using QuickStat.Data;

namespace QuickStat.Domain.Populations;

/// <summary>
/// Every statement, parameter name and result column the population catalogue uses, in one place.
/// </summary>
/// <remarks>
/// <para>
/// The statements are the Delphi constants verbatim, still in <c>:Name</c> form, because
/// <see cref="QuickStat.Data.SqlRequest.CommandText"/> is rewritten to <c>@Name</c> by the executor
/// immediately before binding. Keeping them byte-identical to the Delphi is what makes the Phase 5
/// golden files meaningful.
/// </para>
/// <para>
/// Sources: <c>CRF.Population.Interfaces.pas:38-51</c> (catalogue statements and column names) and
/// <c>CRF.SQL.pas:174</c> (the audit command).
/// </para>
/// </remarks>
internal static class PopulationSql
{
    /// <summary><c>QRY_POPULAR_POPULATIONS</c> - the "Frequently used only" catalogue.</summary>
    public const string PopularPopulations = "EXEC Populations.GetPopularPopulations :StudyId, :DbVer";

    /// <summary><c>QRY_STUDY_POPULATIONS_WITH_VERSION</c> - the catalogue on a database at or above 18200.</summary>
    public const string StudyPopulationsWithVersion = "EXEC Populations.GetStudyPopulations :StudyId, :DbVer";

    /// <summary><c>QRY_STUDY_POPULATIONS_NO_VERSION</c> - the catalogue on an older database.</summary>
    public const string StudyPopulationsNoVersion = "EXEC Populations.GetStudyPopulations :StudyId";

    /// <summary><c>CMD_LOG_POPULATION_CHANGE</c> - the fire-and-forget selection audit.</summary>
    public const string AddPopulationLog = "EXEC dbo.AddPopulationLog :StudyId, :ProcId, :ProcDesc, :ElapsedMs";

    /// <summary>Placeholder for the current study.</summary>
    public const string ParamStudyId = "StudyId";

    /// <summary>Placeholder for the FastTrak schema version.</summary>
    public const string ParamDbVer = "DbVer";

    /// <summary>Placeholder for the population's primary key.</summary>
    public const string ParamProcId = "ProcId";

    /// <summary>
    /// Placeholder the audit command reserves for a description - the Delphi passes the population's
    /// <em>title</em> into it (<c>EPR.VclFrame.Populations.pas:219</c>).
    /// </summary>
    public const string ParamProcDesc = "ProcDesc";

    /// <summary>Placeholder for how long preparing the population took.</summary>
    public const string ParamElapsedMs = "ElapsedMs";

    /// <summary><c>FLD_PROC_ID</c>. Required.</summary>
    public const string ColProcId = "ProcId";

    /// <summary><c>FLD_PROC_TITLE</c>. Required.</summary>
    public const string ColProcTitle = "ProcTitle";

    /// <summary><c>FLD_SQL_TEXT</c>. Required - it is the statement that produces the cohort.</summary>
    public const string ColSqlText = "SqlText";

    /// <summary><c>FLD_PROC_GROUP</c>. Optional.</summary>
    public const string ColProcGroup = "ProcGroup";

    /// <summary><c>FLD_HELP_TEXT</c>. Optional.</summary>
    public const string ColHelpText = "HelpText";

    /// <summary><c>FLD_INFO_CAPTION</c>. Optional.</summary>
    public const string ColInfoCaption = "InfoCaption";

    /// <summary><c>FLD_SOURCE_CODE</c>. Optional.</summary>
    public const string ColSourceCode = "ProcSourceCode";

    /// <summary>Log label for the catalogue query.</summary>
    public const string CatalogueLabel = "Populations";

    /// <summary>Log label for the audit command.</summary>
    public const string SelectionAuditLabel = "AddPopulationLog";

    /// <summary>Builds the catalogue request, choosing between the three procedure variants.</summary>
    /// <param name="studyId">Current study.</param>
    /// <param name="dbVersion">FastTrak schema version.</param>
    /// <param name="frequentlyUsedOnly">Whether the "Frequently used only" box is ticked.</param>
    /// <returns>The request.</returns>
    /// <remarks>
    /// <c>EPR.Population.List.pas:102-107</c>. The order of the tests matters: "frequently used" wins
    /// over the version check, so a popular-populations load always passes <c>:DbVer</c> even on an
    /// old database.
    /// </remarks>
    public static SqlRequest Catalogue(int studyId, int dbVersion, bool frequentlyUsedOnly)
    {
        // Only the single-argument variant omits :DbVer, and it is reached only when the box is
        // unticked on a database below the threshold.
        bool passesVersion = frequentlyUsedOnly || dbVersion >= DatabaseInfo.PopulationsWithVersionDbVersion;

        string commandText = frequentlyUsedOnly
            ? PopularPopulations
            : passesVersion
                ? StudyPopulationsWithVersion
                : StudyPopulationsNoVersion;

        Dictionary<string, object?> values = new(StringComparer.OrdinalIgnoreCase)
        {
            [ParamStudyId] = studyId,
        };

        if (passesVersion)
        {
            values[ParamDbVer] = dbVersion;
        }

        return new SqlRequest
        {
            CommandText = commandText,
            NamedValues = values,
            IsIdempotent = true,
            Label = CatalogueLabel,
        };
    }

    /// <summary>Builds the selection-audit command.</summary>
    /// <param name="studyId">Current study.</param>
    /// <param name="procId">The population that was prepared.</param>
    /// <param name="procTitle">The population's title, which goes into the <c>:ProcDesc</c> slot.</param>
    /// <param name="elapsedMilliseconds">How long preparing it took.</param>
    /// <returns>The request. Never idempotent: an audit row must not be written twice.</returns>
    public static SqlRequest SelectionAudit(int studyId, int procId, string procTitle, long elapsedMilliseconds) =>
        new()
        {
            CommandText = AddPopulationLog,
            NamedValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                [ParamStudyId] = studyId,
                [ParamProcId] = procId,
                [ParamProcDesc] = procTitle,
                [ParamElapsedMs] = elapsedMilliseconds,
            },
            IsIdempotent = false,
            Label = SelectionAuditLabel,
        };
}
