namespace QuickStat.Domain.Populations;

/// <summary>
/// Asks the user for a date range, and remembers the last one per population.
/// </summary>
/// <remarks>
/// Delphi: <c>IPeriodDictionary.TryGetPeriod</c> (<c>EPR.PeriodDictionary.pas:54-80</c>) driving the
/// modal <c>TfrmPeriod</c>. Implemented by the UI layer, because it shows a window; declared here
/// because step 2.3 is what needs an answer.
/// </remarks>
public interface IPeriodPrompt
{
    /// <summary>Shows the period dialog, pre-filled with the last range used for this query.</summary>
    /// <param name="context">
    /// Identifies which population is asking, so the answer can be remembered per population.
    /// </param>
    /// <param name="caption">
    /// Sub-header text, in Norwegian:
    /// <c>Denne spørringen krever at du angir et tidsintervall.</c>
    /// </param>
    /// <param name="cancellationToken">Cancels the prompt.</param>
    /// <returns>The chosen period, or <see langword="null"/> when the user cancelled.</returns>
    /// <remarks>
    /// <para>
    /// The Delphi persisted the answer with the section and key arguments swapped, so the section
    /// was the literal string <c>PeriodStart</c> and the <em>key</em> was the entire SQL text
    /// (<c>EPR.PeriodDictionary.pas:65-66, 75-76</c>). <c>WritePrivateProfileString</c> cannot store
    /// a multi-line key containing <c>=</c>, so for any realistic population the round trip
    /// silently failed and the defaults - yesterday and today - were used every single time. Key on
    /// a hash of the query text instead. Fixing this means the feature starts working for the first
    /// time; users who never saw a remembered range will now see one.
    /// </para>
    /// <para>
    /// Cancelling must abort the population load. In the Delphi the patient list was only cleared
    /// <em>inside</em> the success branch, so a cancelled prompt left the previous population's
    /// patients on screen under the new population's title
    /// (<c>CRF.Patient.List.pas:297-299</c>) - PORT-PLAN.md §7.2.
    /// </para>
    /// </remarks>
    Task<HalfOpenPeriod?> TryGetPeriodAsync(string context, string caption, CancellationToken cancellationToken = default);
}
