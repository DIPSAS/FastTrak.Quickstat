namespace QuickStat.Domain.Populations;

/// <summary>
/// Works out the values for a population's <c>:Name</c> placeholders, asking the user only for the
/// period.
/// </summary>
/// <remarks>
/// Delphi: <c>TParameterDictionary.TryApplyParameters</c>
/// (<c>Emetra.Database.ParameterDictionary.pas:79-133</c>).
/// </remarks>
public interface IQueryParameterResolver
{
    /// <summary>Discovers the placeholders in a statement and resolves each of them.</summary>
    /// <param name="sqlText">The population's <c>SqlText</c>.</param>
    /// <param name="cancellationToken">Cancels the resolution, including the period prompt.</param>
    /// <returns>
    /// The values, or a failed <see cref="ParameterResolution"/> distinguishing "the user cancelled"
    /// from "a placeholder is unknown".
    /// </returns>
    /// <remarks>
    /// Order matters. If <em>both</em> <c>StartDate</c> and <c>StopDate</c> appear, the period
    /// prompt runs first and a cancel aborts everything; only then are the remaining placeholders
    /// resolved from <see cref="QuickStat.Data.SessionContext.TryGetParameterValue"/>. One
    /// placeholder alone does not trigger the prompt.
    /// </remarks>
    Task<ParameterResolution> ResolveAsync(string sqlText, CancellationToken cancellationToken = default);

    /// <summary>Placeholder name that, paired with <see cref="StopDateParameterName"/>, triggers the prompt.</summary>
    /// <remarks><c>PRM_START_DATE</c> (<c>Emetra.Database.ParameterDictionary.pas:63</c>).</remarks>
    const string StartDateParameterName = "StartDate";

    /// <summary>Placeholder name that, paired with <see cref="StartDateParameterName"/>, triggers the prompt.</summary>
    /// <remarks><c>PRM_STOP_DATE</c> (<c>Emetra.Database.ParameterDictionary.pas:64</c>).</remarks>
    const string StopDateParameterName = "StopDate";
}
