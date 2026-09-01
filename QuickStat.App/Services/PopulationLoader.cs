using System.Diagnostics;
using Microsoft.Extensions.Logging;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;
using QuickStat.Domain.Patients;
using QuickStat.Domain.Populations;

namespace QuickStat.Services;

/// <summary>
/// The one description of <em>loading a population</em>: resolve the placeholders, run the cohort
/// query, recover the national ids, fill the matrix, draw a fresh pseudonym space, and tell the
/// workspace.
/// </summary>
/// <remarks>
/// <para>
/// Delphi <c>TfrmQuickStat.AfterPopulationSelect</c> (<c>MainQuickStat.pas:521-550</c>) plus
/// <c>LoadPopulationIntoGrid</c> (<c>:554-575</c>), which is the pair the Delphi itself shares
/// between its two entry points - the population double click reaches
/// <c>AfterPopulationSelect</c> through <c>PopulationRequested</c>, and the package replay reaches
/// the very same handler through <c>TrySelect(procId, ALoadIt := true, …)</c> (<c>:789</c>). There
/// was one sequence upstream and there is one here.
/// </para>
/// <para>
/// <b>Why this exists.</b> PORT-PLAN.md §8.10 (b): the port grew two copies of the sequence -
/// <see cref="QuickStat.ViewModels.PopulationPickerViewModel"/> owned one, and step 3.4's replay
/// carried a near-duplicate because the picker's command was synchronous when the replay was
/// written. Both were tested and both agreed, which is precisely the "two halves, each locally
/// correct" shape that had already produced two defects in this port; Phase 4 made it worse by
/// adding the national-id recovery to both. Collapsed here so that a future step cannot change one
/// half.
/// </para>
/// <para>
/// <b>Why it lives in <c>QuickStat.App</c> and not in <c>QuickStat.Core</c>.</b> Nothing below
/// touches WPF, so the type would compile in <c>QuickStat.Core</c> (which targets plain
/// <c>net10.0</c> and must stay that way) - but its last step is
/// <see cref="IShellWorkspace.SetPopulation"/>, and the workspace is a shell concern: it exists to
/// let four tabs see one matrix and it raises <see cref="IShellWorkspace.CollectionsTabRequested"/>
/// at the shell. Pushing the first four steps into the domain and leaving the fifth to each caller
/// would split the ordering contract in half again - the exact failure mode §8.10 (b) is about. The
/// individual steps <em>are</em> domain code and already live in <c>QuickStat.Core</c>
/// (<see cref="IPatientRepository.LoadPopulationAsync"/>,
/// <see cref="NationalIdRecovery.EnsureNationalIdsAsync"/>,
/// <see cref="PersonMatrix.PreparePopulation"/>); what is orchestration, and what this type owns, is
/// the <em>order</em> they run in and who is told afterwards.
/// </para>
/// <para>
/// <b>What is deliberately <em>not</em> here.</b> Everything the two callers genuinely disagree
/// about: which status line the busy scope carries and how far it extends, whether an unresolvable
/// placeholder reaches a message box, whether the <c>dbo.AddPopulationLog</c> row is awaited or
/// fired and forgotten, whether the <c>Collections</c> tab is asked for, and what a failure is
/// called. None of that is unified - the callers keep it, and this method reports the one outcome
/// they both have to branch on through <see cref="PopulationLoadResult"/> and otherwise throws.
/// </para>
/// </remarks>
public sealed class PopulationLoader
{
    private readonly IAnonymiser _anonymiser;
    private readonly IQueryParameterResolver _parameters;
    private readonly IPatientRepository _patients;
    private readonly IShellWorkspace _workspace;

    /// <summary>Creates the loader.</summary>
    /// <param name="parameters">Resolves the population's <c>:Name</c> placeholders, prompting for a period.</param>
    /// <param name="patients">Runs the cohort query, and the national-id recovery query behind it.</param>
    /// <param name="workspace">Owns the one <see cref="PersonMatrix"/> and records what is in it.</param>
    /// <param name="anonymiser">
    /// The shared pseudonym map, reset here because a new cohort is a new dataset. See
    /// <see cref="LoadAsync"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public PopulationLoader(
        IQueryParameterResolver parameters,
        IPatientRepository patients,
        IShellWorkspace workspace,
        IAnonymiser anonymiser)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(patients);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(anonymiser);

        _parameters = parameters;
        _patients = patients;
        _workspace = workspace;
        _anonymiser = anonymiser;
    }

    /// <summary>Runs a population and leaves its cohort in the matrix.</summary>
    /// <param name="population">The population to run. Its <c>QueryText</c> is executed verbatim.</param>
    /// <param name="logger">
    /// The caller's log, so that what the national-id recovery writes carries the caller's category -
    /// see <see cref="NationalIdRecovery.EnsureNationalIdsAsync"/>, which takes it for the same
    /// reason. A logger of this type's own would make one line of a population load say
    /// <c>PopulationLoader</c> while the four around it say <c>PopulationPickerViewModel</c>.
    /// </param>
    /// <param name="cancellationToken">Cancels the period prompt and both queries.</param>
    /// <returns>
    /// <see cref="PopulationLoadResult.Loaded"/> when the matrix holds the cohort and
    /// <see cref="IShellWorkspace.Population"/> names it. The <em>only</em> non-exceptional way this
    /// does not happen is an unresolved placeholder set, which arrives as
    /// <see cref="PopulationLoadResult.Unresolved"/> unreported, because the two callers report it
    /// differently.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="population"/> or <paramref name="logger"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">The token was cancelled.</exception>
    /// <remarks>
    /// <para>
    /// <b>The order is a contract, not a style, and every line of it was paid for.</b>
    /// </para>
    /// <para>
    /// <b>Placeholders first, before anything is cleared.</b> A cancelled period dialog abandons the
    /// whole load and leaves the previous cohort intact under its own title. PORT-PLAN.md §7.2: the
    /// Delphi cleared the grid at <c>MainQuickStat.pas:532</c> and only then asked, so a cancel left
    /// the previous cohort on screen under the <em>new</em> population's title.
    /// </para>
    /// <para>
    /// <b>The national-id recovery sits between the cohort query and
    /// <see cref="PersonMatrix.PreparePopulation"/>.</b> That is where <c>AddNationalIds</c> sits in
    /// the Delphi (<c>MainQuickStat.pas:536-540</c>, commented out in this reduced repository) and it
    /// is where it has to sit here: <c>PreparePopulation</c> copies
    /// <see cref="Patient.NationalId"/> onto the row it builds (<c>PersonMatrix.cs:151</c>) and never
    /// reads the patient again, so filling the ids afterwards leaves every row blank. It is
    /// unconditional and not gated on the identification mode -
    /// <see cref="NationalIdRecovery"/>'s remarks say why, and why a failure degrades this one column
    /// rather than the load.
    /// </para>
    /// <para>
    /// <b><see cref="PersonMatrix.Clear"/> comes first, and that is not decoration.</b>
    /// <see cref="PersonMatrix.SortBy"/> throws once the matrix is locked and a collect run locks it,
    /// so without the clear the sequence works once and throws the second time: load a population,
    /// collect, load another. The Delphi opens <c>LoadPopulationIntoGrid</c> with
    /// <c>fGrid.Data.ClearPopulation</c> for exactly this reason - it clears <c>fLocked</c>
    /// (<c>EPR.QA.Matrix.pas:211-215</c>) - after <c>AfterPopulationSelect</c> has already called
    /// <c>fGrid.Clear</c> (<c>MainQuickStat.pas:532</c>). <c>Clear</c> rather than
    /// <c>ClearPopulation</c> is the wider of the two and matches <c>:532</c>; the package replay
    /// used to spell it <c>ClearPopulation</c>, and the two are <em>indistinguishable</em> here
    /// rather than merely similar, which is why collapsing them changes nothing:
    /// <see cref="PersonMatrix.PreparePopulation"/> opens with a full
    /// <see cref="PersonMatrix.Clear"/> of its own, the only statement in between sorts an
    /// already-empty row list, both spellings unlock so neither can throw, and
    /// <see cref="PersonMatrix"/> raises no notifications - so no observer exists for the one state
    /// they differ in.
    /// </para>
    /// <para>
    /// <b><see cref="IAnonymiser.Reset"/> is here because nowhere else was, and the omission was a
    /// privacy defect rather than an untidiness.</b> <see cref="IAnonymiser"/> is a singleton and
    /// <see cref="QuickStat.Export.DatasetExporter"/> only ever calls
    /// <see cref="IAnonymiser.EnsureSpaceFor"/>,
    /// which by design leaves a space that is already wide enough alone - so with nobody resetting,
    /// the pseudonym map lived for the whole session. A patient in two populations was handed the
    /// <em>same</em> pseudonym in both exports, and joining the two anonymised files revealed who was
    /// in both cohorts: precisely the property <c>MatrixAnonymiser</c>'s remarks and PORT-PLAN.md
    /// §7.2 promise. Two lesser consequences went with it - the space is <c>9 × ScaleFactor</c> wide
    /// and never widens for an accumulated map, so a long session eventually exhausts it, and a
    /// cohort exported after a much larger one inherited the larger one's digit width.
    /// </para>
    /// <para>
    /// It takes <c>Rows.Count</c> and not <c>cohort.Count</c> because <c>Rows.Count</c> is the number
    /// <see cref="QuickStat.Export.ExportDataset.FromMatrix"/> carries to
    /// <see cref="IAnonymiser.EnsureSpaceFor"/> at export time; the two must agree, or the exporter
    /// widens the space and discards the map it was just given.
    /// </para>
    /// <para>
    /// <b><see cref="IShellWorkspace.SetPopulation"/> comes last.</b> <see cref="PersonMatrix"/>
    /// raises no notifications, so the workspace cannot observe it and reads
    /// <c>Rows.Count</c> at the moment it is told; told any earlier,
    /// <see cref="IShellWorkspace.HasPopulation"/> would answer for the previous cohort.
    /// </para>
    /// </remarks>
    public async Task<PopulationLoadResult> LoadAsync(
        Population population,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(population);
        ArgumentNullException.ThrowIfNull(logger);

        long startedAt = Stopwatch.GetTimestamp();

        // Placeholders first, before anything is cleared, so that a cancelled period dialog abandons
        // the load and leaves the previous cohort intact.  PORT-PLAN.md §7.2: the Delphi cleared the
        // grid at MainQuickStat.pas:532 and only then asked, so a cancel left the previous cohort on
        // screen under the new population's title.
        ParameterResolution resolution = await _parameters
            .ResolveAsync(population.QueryText, cancellationToken)
            .ConfigureAwait(true);

        if (!resolution.Succeeded)
        {
            return new PopulationLoadResult { Unresolved = resolution };
        }

        IReadOnlyList<Patient> cohort = await _patients
            .LoadPopulationAsync(population, resolution.Values, cancellationToken)
            .ConfigureAwait(true);

        // The two lines this repository has commented out as "// TODO: Disse feiler, hvor er de??"
        // (MainQuickStat.pas:536-540), restored: unconditional, and here rather than after
        // PreparePopulation, which copies the ids onto the rows it builds (PersonMatrix.cs:151) and
        // never reads the patients again.  Not conditioned on the identification mode -
        // NationalIdRecovery's remarks say why, and why a failure only degrades this one column.
        await NationalIdRecovery
            .EnsureNationalIdsAsync(_patients, cohort, logger, cancellationToken)
            .ConfigureAwait(true);

        PersonMatrix matrix = _workspace.Matrix;

        // Clear unlocks; SortBy throws on a locked matrix and the matrix is locked from the previous
        // collect run.  Delphi: fGrid.Clear (:532), fGrid.Data.ClearPopulation (:564).
        matrix.Clear();
        matrix.SortBy = MatrixSortOrder.PersonId;
        matrix.PreparePopulation(cohort);

        // A new cohort is a new dataset, so it gets a new key and an empty map.  Nothing else in the
        // application calls this: DatasetExporter only calls EnsureSpaceFor, which keeps a space that
        // is already wide enough, so without this line one map served the whole session and the same
        // patient kept one pseudonym across two populations.  Rows.Count, not cohort.Count - see the
        // remarks.
        _anonymiser.Reset(matrix.Rows.Count);

        // Last, because the workspace reads Rows.Count at this moment (:567-569).
        _workspace.SetPopulation(population);

        return new PopulationLoadResult
        {
            RowCount = matrix.Rows.Count,
            ElapsedMilliseconds = (long)Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
        };
    }
}

/// <summary>What <see cref="PopulationLoader.LoadAsync"/> answers.</summary>
/// <remarks>
/// Two outcomes, not three: either the cohort is in the matrix, or the population's placeholders
/// could not be resolved and nothing was touched. Anything else throws, because both callers already
/// have a <c>catch</c> and they word the failure differently.
/// </remarks>
public sealed record PopulationLoadResult
{
    /// <summary>
    /// The resolution that failed, or <see langword="null"/> when the load ran.
    /// </summary>
    /// <remarks>
    /// Handed back whole and <em>unreported</em>, because the distinction inside it drives the
    /// caller: <see cref="ParameterResolution.CancelledByUser"/> is not an error and must raise
    /// nothing, whereas an unresolvable placeholder is a defect in the stored population. The two
    /// callers disagree about how loud that second case is, and this is where that disagreement is
    /// allowed to live.
    /// </remarks>
    public ParameterResolution? Unresolved { get; init; }

    /// <summary>Patients in the matrix afterwards. Zero unless <see cref="Loaded"/>.</summary>
    public int RowCount { get; init; }

    /// <summary>
    /// Whole milliseconds from the period prompt to <see cref="IShellWorkspace.SetPopulation"/>, for
    /// the log line and the <c>dbo.AddPopulationLog</c> row. Zero unless <see cref="Loaded"/>.
    /// </summary>
    /// <remarks>
    /// It starts before the prompt, so a population that asks for a period measures the user's
    /// thinking time as well. Both call sites already did that and the number only ever reaches the
    /// popularity audit, which ranks by <em>count</em>.
    /// </remarks>
    public long ElapsedMilliseconds { get; init; }

    /// <summary>Whether the cohort reached the matrix and the workspace was told.</summary>
    public bool Loaded => Unresolved is null;
}
