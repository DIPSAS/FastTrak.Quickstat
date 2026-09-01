namespace QuickStat.Domain.Anonymisation;

/// <summary>Assigns pseudonymous person ids, and keeps the map that can undo them.</summary>
/// <remarks>
/// <para>
/// Delphi: <c>TMatrixAnonymizer</c> (<c>EPR.QA.Matrix.Anoymizer.pas</c> - the filename typo is in
/// the repository). Its behaviour is the worst of both worlds and PORT-PLAN.md §7.2 lists it as a
/// bug being fixed:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <c>Randomize</c> is called <b>nowhere</b> in the entire repository, so <c>RandSeed</c> stays
///     at the RTL's initial 0. The pseudonym sequence is therefore identical on every run and on
///     every machine, and two "anonymised" exports of two different cohorts of the same size share
///     the same pseudonym list - joining them re-identifies by position.
///   </description></item>
///   <item><description>
///     A second export in the same process continues the RNG stream, so the <em>same</em> patient
///     gets a <em>different</em> pseudonym. Longitudinal linkage across two exports is impossible.
///   </description></item>
/// </list>
/// <para>
/// The replacement must be stable for the lifetime of one loaded dataset and unlinkable across
/// datasets: a keyed derivation from a per-export salt, with rejection on collision.
/// </para>
/// </remarks>
public interface IAnonymiser
{
    /// <summary>Starts a new pseudonym space and discards the previous map.</summary>
    /// <param name="personCount">
    /// Number of people, which sets the digit count.
    /// </param>
    /// <remarks>
    /// <para>
    /// The Delphi scale factor is the smallest power of ten at or above <c>1 + rowCount</c>, and
    /// pseudonyms fall in <c>[scale, 10 * scale - 1]</c>: 17 people give three-digit ids in
    /// 100-999. Reproduce the width, not the sequence.
    /// </para>
    /// <para>
    /// <b>This is the dataset boundary, and calling it is a privacy decision.</b> Everything mapped
    /// so far becomes unrecoverable and the next export of the same patient gets an unrelated
    /// pseudonym. Call it when a <em>population</em> is loaded, never once per export - exporting
    /// twice from one loaded dataset must give the same pseudonyms, which is the half of the Delphi
    /// behaviour that was broken. An exporter that merely needs a space to exist calls
    /// <see cref="EnsureSpaceFor"/>.
    /// </para>
    /// <para>
    /// <b>There is exactly one caller, and for a while there were none.</b>
    /// <c>QuickStat.App</c>'s <c>PopulationLoader.LoadAsync</c>, which is the single description of
    /// a population load, so both the double-click and the package replay reach it. Nothing else may
    /// call this. Its absence was a live privacy defect rather than a loose end - one map served the
    /// whole session, so a patient in two populations kept one pseudonym across both exports and
    /// joining them re-identified co-membership. PORT-PLAN.md §8.11 (12).
    /// </para>
    /// </remarks>
    void Reset(int personCount);

    /// <summary>
    /// Guarantees that a pseudonym space wide enough for <paramref name="personCount"/> people
    /// exists, without disturbing one that already does.
    /// </summary>
    /// <param name="personCount">Number of people about to be pseudonymised.</param>
    /// <returns>
    /// <see langword="true"/> when a new space had to be created, which a caller should log: it
    /// means whoever loaded the dataset did not call <see cref="Reset"/>.
    /// </returns>
    /// <remarks>
    /// Added over the Delphi's surface because <see cref="Reset"/> alone cannot express "make sure
    /// there is one". The Delphi constructed a fresh <c>TMatrixAnonymizer</c> per <c>SaveToFile</c>,
    /// which is exactly why the same patient got a different pseudonym in two exports of one loaded
    /// dataset.
    /// </remarks>
    bool EnsureSpaceFor(int personCount);

    /// <summary>The pseudonym for a person, stable within the current dataset.</summary>
    /// <param name="personId">The real person id.</param>
    /// <returns>The pseudonym.</returns>
    int GetPseudonym(int personId);

    /// <summary>
    /// Pseudonym to real person id - the content of the re-identification key file.
    /// </summary>
    /// <remarks>
    /// Written as <c>&lt;pseudonym&gt;=&lt;PersonId&gt;</c> lines. In the Delphi this file was
    /// written next to <em>every</em> anonymised export, including the temporary CSV behind
    /// <c>Open this dataset in Excel</c> - and unlike that CSV it was never tracked for deletion, so
    /// plaintext re-identification keys accumulate in the user's <c>%TEMP%</c> indefinitely. Writing
    /// it is opt-in in the port, and whoever writes it must track it for deletion.
    /// </remarks>
    IReadOnlyDictionary<int, int> PseudonymToPersonId { get; }
}
