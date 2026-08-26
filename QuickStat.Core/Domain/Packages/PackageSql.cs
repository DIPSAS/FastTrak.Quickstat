using QuickStat.Data;

namespace QuickStat.Domain.Packages;

/// <summary>
/// Every statement, parameter name and result column the packaged-selection store uses.
/// </summary>
/// <remarks>
/// <c>EPR.QA.SQL.pas:24-30, 44-45</c> and <c>QuickStat.Selection.pas:129</c>, verbatim. Note the
/// three different schemas - <c>Report</c> for reading and writing, <c>QuickStat</c> for deleting -
/// and that <c>CMD_ADD_PACKAGE</c> has no spaces after its commas. Both are preserved.
/// </remarks>
internal static class PackageSql
{
    /// <summary><c>QRY_GET_PACKAGES</c>.</summary>
    public const string GetPackages =
        "SELECT r.* FROM Report.QuickStat r JOIN dbo.Study s ON s.StudyId=r.StudyId WHERE r.StudyId=:StudyId";

    /// <summary><c>CMD_ADD_PACKAGE</c>. Returns a result set carrying the new <c>RowId</c>.</summary>
    public const string AddPackage = "EXEC Report.AddQuickStat :StudyId,:ProcId,:Title,:DataElements,:Comment";

    /// <summary>The delete command, inline in <c>TPackagedSelection.Delete</c>.</summary>
    public const string DeletePackage = "EXEC QuickStat.DeletePackage :RowId";

    /// <summary>Placeholder for the owning study.</summary>
    public const string ParamStudyId = "StudyId";

    /// <summary>Placeholder for the population to replay.</summary>
    public const string ParamProcId = "ProcId";

    /// <summary>Placeholder for the package title.</summary>
    public const string ParamTitle = "Title";

    /// <summary>Placeholder for the semicolon-separated collector names.</summary>
    public const string ParamDataElements = "DataElements";

    /// <summary>Placeholder for the free-text comment.</summary>
    public const string ParamComment = "Comment";

    /// <summary>Placeholder for the row to delete.</summary>
    public const string ParamRowId = "RowId";

    /// <summary><c>FLD_STUDY_ID</c>.</summary>
    public const string ColStudyId = "StudyId";

    /// <summary><c>FLD_ROW_ID</c>.</summary>
    public const string ColRowId = "RowId";

    /// <summary><c>FLD_PROC_ID</c>.</summary>
    public const string ColProcId = "ProcId";

    /// <summary><c>FLD_TITLE</c>.</summary>
    public const string ColTitle = "Title";

    /// <summary><c>FLD_COMMENT</c>.</summary>
    public const string ColComment = "Comment";

    /// <summary><c>FLD_DATA_ELEMENTS</c>.</summary>
    public const string ColDataElements = "DataElements";

    /// <summary>Log label for the package list.</summary>
    public const string GetPackagesLabel = "GetPackages";

    /// <summary>Log label for saving a package.</summary>
    public const string AddPackageLabel = "AddQuickStat";

    /// <summary>Log label for deleting a package.</summary>
    public const string DeletePackageLabel = "DeletePackage";

    /// <summary>Builds the request that lists a study's packages.</summary>
    /// <param name="studyId">Current study.</param>
    /// <returns>The request.</returns>
    public static SqlRequest List(int studyId) => new()
    {
        CommandText = GetPackages,
        NamedValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [ParamStudyId] = studyId,
        },
        IsIdempotent = true,
        Label = GetPackagesLabel,
    };

    /// <summary>Builds the request that saves a package.</summary>
    /// <param name="package">The package to store.</param>
    /// <returns>The request. Never idempotent - a retry would create a second row.</returns>
    public static SqlRequest Save(PackagedSelection package) => new()
    {
        CommandText = AddPackage,
        NamedValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [ParamStudyId] = package.StudyId,
            [ParamProcId] = package.PopulationId,
            [ParamTitle] = package.Title,
            [ParamDataElements] = CollectorNameList.Format(package.CollectorNames),
            [ParamComment] = package.Comment,
        },
        IsIdempotent = false,
        Label = AddPackageLabel,
    };

    /// <summary>Builds the request that deletes a package.</summary>
    /// <param name="rowId">The row to remove.</param>
    /// <returns>The request.</returns>
    public static SqlRequest Delete(int rowId) => new()
    {
        CommandText = DeletePackage,
        NamedValues = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [ParamRowId] = rowId,
        },
        IsIdempotent = false,
        Label = DeletePackageLabel,
    };
}
