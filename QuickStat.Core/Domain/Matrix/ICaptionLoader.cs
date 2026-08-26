namespace QuickStat.Domain.Matrix;

/// <summary>Fills the caption dictionary for the connected database.</summary>
/// <remarks>
/// <para>
/// This exists so the two-tier precedence rule stays in <c>QuickStat.Core</c>, tested, instead of
/// being reassembled in a view-model: the twelve hardcoded captions must be installed <b>before</b>
/// the database rows, because the database merge is first-wins and that asymmetry is the only
/// reason the hardcoded ones survive (<c>EPR.QA.CaptionDictionary.pas:110-111, 149-154</c>).
/// </para>
/// <para>
/// Call it once after a session is established. The Delphi ran the query at the start of
/// <em>every</em> collect run, through <c>AddCaptions</c> → <c>LoadCaptions(true, false)</c>
/// (<c>MainQuickStat.pas:453-469</c>); <c>dbo.LabClass</c> is a reference table that does not change
/// underneath a session, so the port reads it once per connection instead.
/// </para>
/// </remarks>
public interface ICaptionLoader
{
    /// <summary>
    /// Resets the dictionary to the built-in captions and merges in the database's lab captions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>How many database captions were added.</returns>
    /// <remarks>
    /// <para>
    /// <b>Resetting is deliberate and differs from the Delphi</b>, which kept one
    /// <c>TVarCaptions</c> across every project switch and merged first-wins into it. Captions are
    /// per-database, so on a switch from A to B the original leaves A's captions in place and lets
    /// them beat B's. Reversal cost: drop the reset and merge into whatever is already there.
    /// </para>
    /// <para>
    /// <b>A failure leaves the existing captions alone</b> and reports rather than throwing. The
    /// Delphi's outer handler ran <c>fTitles.Clear</c> (<c>:135-141</c>), so one failed query
    /// against a database without <c>Report.LabClassName</c> discarded the twelve built-in captions
    /// as well and every column in the grid fell back to its raw variable name. Doing nothing is
    /// strictly better than that, and captions are cosmetic — they must never fail a login.
    /// </para>
    /// </remarks>
    Task<int> LoadAsync(CancellationToken cancellationToken = default);
}
