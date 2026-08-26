namespace QuickStat.Domain.Matrix;

/// <summary>Reads the variable captions that live in the database rather than in the source.</summary>
/// <remarks>
/// <para>
/// Delphi: the three queries in <c>TVarCaptions.AfterLogin</c>
/// (<c>EPR.QA.CaptionDictionary.pas:78-134</c>). QuickStat enables exactly one of them —
/// <c>LoadCaptions(true, false)</c> at <c>MainQuickStat.pas:468</c> turns lab captions on, custom
/// captions off, and per-item captions were never on — so this interface exposes only the lab
/// query. The other two are deliberately absent rather than stubbed; adding one is a plan change.
/// </para>
/// </remarks>
public interface ICaptionRepository
{
    /// <summary>Reads one caption per row of <c>dbo.LabClass</c>.</summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The captions, in <c>LabClassId</c> order.</returns>
    /// <remarks>
    /// Order is part of the contract: the caption dictionary merges on a first-wins rule, so two
    /// lab classes resolving to the same variable name are settled by which one the server returns
    /// first.
    /// </remarks>
    Task<IReadOnlyList<CaptionRecord>> GetLabCaptionsAsync(CancellationToken cancellationToken = default);
}
