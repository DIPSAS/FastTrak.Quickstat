using QuickStat.Data;

namespace QuickStat.Domain.Matrix;

/// <summary>The one caption query QuickStat runs.</summary>
/// <remarks>
/// Delphi: <c>QueryLabCaptions</c> (<c>EPR.QA.SQL.pas:153-157</c>). Reproduced verbatim, including
/// the <c>NULL AS VarDescription</c> — lab captions carry a title and never a description, and the
/// column is selected only so one row shape serves all three caption queries in the original.
/// </remarks>
internal static class CaptionSql
{
    /// <summary>The lab-caption query, character for character as the Delphi builds it.</summary>
    public const string LabCaptions =
        "SELECT ISNULL(NLK, Report.LabClassName(LabClassId)) AS VarName, " +
        "FriendlyName AS Caption, " +
        "NULL AS VarDescription " +
        "FROM dbo.LabClass ORDER BY LabClassId";

    /// <summary>Column carrying the variable name.</summary>
    public const string ColVarName = "VarName";

    /// <summary>Column carrying the title.</summary>
    public const string ColCaption = "Caption";

    /// <summary>Column carrying the description.</summary>
    public const string ColVarDescription = "VarDescription";

    /// <summary>Log label for the lab-caption round trip.</summary>
    public const string LabCaptionsLabel = "LabCaptions";

    /// <summary>Builds the lab-caption request.</summary>
    /// <returns>The request.</returns>
    /// <remarks>
    /// Idempotent: it is a pure read of a reference table, so the retry policy may replay it.
    /// </remarks>
    public static SqlRequest LabCaptionRequest() => new()
    {
        CommandText = LabCaptions,
        IsIdempotent = true,
        Label = LabCaptionsLabel,
    };
}
