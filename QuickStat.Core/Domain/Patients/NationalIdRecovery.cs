using Microsoft.Extensions.Logging;

namespace QuickStat.Domain.Patients;

/// <summary>
/// The second query that fills in <see cref="Patient.NationalId"/> for a cohort whose population
/// procedure did not return it.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>if not fPersonList.IncludesNationalId then fPersonList.AddNationalIds;</c>, called
/// from <c>TfrmQuickStat.AfterPopulationSelect</c> right after <c>fPersonList.Load</c> and before
/// <c>LoadPopulationIntoGrid</c>. In <b>this</b> repository those two lines are commented out with
/// <c>// TODO: Disse feiler, hvor er de??</c> (<c>MainQuickStat.pas:536-540</c>) because the
/// extraction paired the canonical application with a library that has neither symbol - see
/// PORT-PLAN.md §2.1 and the last paragraph of §5 Phase 4. That is the whole reason
/// <c>Fødselsnummer</c> is empty here in <i>Fully identified patients</i> mode: most population
/// procedures do not return <c>NationalId</c> (<see cref="PatientSql.ColNationalId"/>), so it has to
/// be fetched in a second query.
/// </para>
/// <para>
/// <b>The fetch is unconditional, guarded only by "the population query did not already return
/// them".</b> It deliberately does <em>not</em> depend on the current
/// <see cref="QuickStat.Domain.Anonymisation.PersonIdentification"/> mode, which
/// <c>02-populations-patients.md</c> §8.5 sketched it as doing, for four reasons:
/// </para>
/// <list type="number">
///   <item><description>
///     The Delphi does it unconditionally, at population-load time, before the grid is filled.
///   </description></item>
///   <item><description>
///     <see cref="QuickStat.Domain.Anonymisation.IIdentificationPolicy"/> raises
///     <c>ModeChanged</c> and the mode is switchable at any time <em>after</em> a population is
///     loaded. A load-time fetch conditioned on <c>Full</c> would leave the <c>Fødselsnummer</c>
///     column silently blank for every user who loads first and switches mode second - data loss
///     dressed up as anonymisation.
///   </description></item>
///   <item><description>
///     <see cref="Patient.FirstName"/>, <see cref="Patient.LastName"/> and
///     <see cref="Patient.DateOfBirth"/> are already loaded regardless of mode, and
///     <see cref="QuickStat.Domain.Anonymisation.IdentificationColumns.For"/> decides what is
///     <em>displayed and exported</em>. Making the national id the one lazily-loaded identity field
///     would be inconsistent and would buy nothing.
///   </description></item>
///   <item><description>
///     PORT-PLAN.md R6 is about what leaves the process, not about which fields sit in memory. Both
///     writers and the grid derive their identity columns from
///     <see cref="QuickStat.Domain.Matrix.FixedColumns.VisibleOrdinals"/>, so neither can emit a
///     national id in an anonymous mode however the field was filled.
///   </description></item>
/// </list>
/// </remarks>
public static class NationalIdRecovery
{
    /// <summary>Whether the population procedure already returned a national id for everybody.</summary>
    /// <param name="cohort">The loaded population.</param>
    /// <returns>
    /// <see langword="true"/> when the cohort is non-empty and every patient carries a non-empty
    /// <see cref="Patient.NationalId"/>.
    /// </returns>
    /// <remarks>
    /// Delphi <c>TPatientList.IncludesNationalId</c>. All-or-nothing, as upstream's is: a population
    /// either selects the column or it does not, so a partially filled cohort is treated as unfilled
    /// and re-queried in full.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="cohort"/> is <see langword="null"/>.</exception>
    public static bool IncludesNationalId(IReadOnlyCollection<Patient> cohort)
    {
        ArgumentNullException.ThrowIfNull(cohort);

        if (cohort.Count == 0)
        {
            return false;
        }

        foreach (Patient patient in cohort)
        {
            if (string.IsNullOrEmpty(patient.NationalId))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Fills in the missing national identity numbers, in place, before the cohort reaches the
    /// matrix.
    /// </summary>
    /// <param name="patients">The repository that runs the recovery query.</param>
    /// <param name="cohort">
    /// The loaded population. Mutated: <see cref="Patient.NationalId"/> is assigned for every
    /// patient the query returned.
    /// </param>
    /// <param name="logger">The caller's log, so the entry carries the caller's category.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>How many patients gained a national id they did not have before.</returns>
    /// <remarks>
    /// <para>
    /// Call this <b>before</b> <see cref="QuickStat.Domain.Matrix.PersonMatrix.PreparePopulation"/>:
    /// that method copies <see cref="Patient.NationalId"/> onto the row it builds
    /// (<c>PersonMatrix.cs:151</c>) and never reads the patient again, so filling the ids afterwards
    /// would leave every <see cref="QuickStat.Domain.Matrix.MatrixRow"/> blank.
    /// </para>
    /// <para>
    /// No round trip is made for an empty cohort or for one that
    /// <see cref="IncludesNationalId"/> already covers.
    /// </para>
    /// <para>
    /// A patient the query did not return keeps a <see langword="null"/> national id. The statement
    /// filters <c>NationalId IS NOT NULL</c> (<see cref="PatientSql.NationalIdRequests"/>), so
    /// absence means "this person has none on file", and blanking a value that the population
    /// procedure did return would be a regression.
    /// </para>
    /// <para>
    /// <b>A failed recovery is logged and degraded, never fatal to the load.</b> The user asked for a
    /// cohort; the national id is one column of it, and the other three identity columns, every
    /// collected variable and the whole export path work without it. Two-thirds of the time the
    /// column is not even on screen - the fetch is unconditional, so in
    /// <c>PersonIdOnly</c> and <c>RandomPersonId</c> a fatal failure here would destroy a load whose
    /// result never needed the ids at all. The visible symptom is the blank column, and the log line
    /// says why. Cancellation is <em>not</em> swallowed: it means the whole load is being abandoned,
    /// and the caller owns that.
    /// </para>
    /// <para>
    /// Nothing here logs a national id, only counts - <see cref="QuickStat.Diagnostics.PiiRedactor"/>
    /// exists because they leak.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    public static async Task<int> EnsureNationalIdsAsync(
        IPatientRepository patients,
        IReadOnlyCollection<Patient> cohort,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patients);
        ArgumentNullException.ThrowIfNull(cohort);
        ArgumentNullException.ThrowIfNull(logger);

        if (cohort.Count == 0 || IncludesNationalId(cohort))
        {
            return 0;
        }

        IReadOnlyDictionary<int, string> recoveredIds;

        try
        {
            int[] personIds = new int[cohort.Count];
            int index = 0;

            foreach (Patient patient in cohort)
            {
                personIds[index++] = patient.PersonId;
            }

            recoveredIds = await patients
                .GetNationalIdsAsync(personIds, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Could not recover national identity numbers for {PatientCount} patients. "
                + "The Fødselsnummer column will be blank; the rest of the cohort is unaffected.",
                cohort.Count);

            return 0;
        }

        int recovered = 0;

        foreach (Patient patient in cohort)
        {
            if (!recoveredIds.TryGetValue(patient.PersonId, out string? nationalId))
            {
                continue;
            }

            if (string.IsNullOrEmpty(patient.NationalId))
            {
                recovered++;
            }

            patient.NationalId = nationalId;
        }

        logger.LogInformation(
            "Recovered {RecoveredCount} national identity numbers for {PatientCount} patients.",
            recovered,
            cohort.Count);

        return recovered;
    }
}
