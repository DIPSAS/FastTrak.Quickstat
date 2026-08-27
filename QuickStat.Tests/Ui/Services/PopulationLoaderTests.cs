using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Domain.Matrix;
using QuickStat.Domain.Patients;
using QuickStat.Domain.Populations;
using QuickStat.Services;
using QuickStat.Tests.Ui.Populations;
using Xunit;

namespace QuickStat.Tests.Ui.Services;

/// <summary>
/// The one population-load sequence, PORT-PLAN.md §8.10 (b).
/// </summary>
/// <remarks>
/// <para>
/// The Populations tab and the package replay used to carry a copy each. Both were tested, both
/// agreed, and both were then changed in step with each other by hand - which is the shape §8.10 (b)
/// calls out, because two of this port's defects grew in it. The sequence now lives in
/// <see cref="PopulationLoader"/>, and these are the assertions about the sequence itself rather
/// than about either caller: <c>PopulationPickerViewModelTests</c> and
/// <c>PackagesTabViewModelTests</c> still drive the real loader end to end, and still assert what
/// each tab does around it.
/// </para>
/// <para>
/// Every collaborator is a fake from step 3.2's file rather than a fourth copy of the same fakes -
/// the point of the exercise being not to write things twice.
/// </para>
/// </remarks>
public class PopulationLoaderTests
{
    private const string OlasNationalId = "12032212345";
    private const string KarisNationalId = "01029912345";

    /// <summary>The loader over a real matrix and workspace, with the queries faked.</summary>
    private sealed class Harness
    {
        internal Harness()
        {
            Matrix = ShellWorkspaceTests.NewMatrix();
            Workspace = new ShellWorkspace(Matrix);
            Loader = new PopulationLoader(Parameters, Patients, Workspace);
        }

        internal PersonMatrix Matrix { get; }

        internal ShellWorkspace Workspace { get; }

        internal FakePatientRepository Patients { get; } = new();

        internal FakeParameterResolver Parameters { get; } = new();

        internal PopulationLoader Loader { get; }

        internal Task<PopulationLoadResult> LoadAsync(Population? population = null) =>
            Loader.LoadAsync(population ?? ShellWorkspaceTests.NewPopulation(257), NullLogger.Instance);
    }

    /// <summary>A cohort member whose population procedure <em>did</em> return the national id.</summary>
    private static Patient WithNationalId(int personId, string nationalId)
    {
        Patient patient = ShellWorkspaceTests.NewPatient(personId);

        patient.NationalId = nationalId;

        return patient;
    }

    // ---------------------------------------------------------------------------------------
    //  The sequence, in order.  Delphi AfterPopulationSelect (MainQuickStat.pas:521-550) and
    //  LoadPopulationIntoGrid (:554-575), which is the pair the Delphi shared between its own two
    //  entry points.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task LoadingRunsTheCohortQueryAndFillsTheMatrix()
    {
        Harness harness = new();
        Population population = ShellWorkspaceTests.NewPopulation(257, "Aktive diabetikere");

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1), ShellWorkspaceTests.NewPatient(2)];

        PopulationLoadResult result = await harness.LoadAsync(population);

        Assert.True(result.Loaded);
        Assert.Null(result.Unresolved);
        Assert.Equal(2, result.RowCount);
        Assert.True(result.ElapsedMilliseconds >= 0);

        Assert.Equal([population.QueryText], harness.Parameters.Resolved);
        Assert.Equal([population], harness.Patients.Loaded);
        Assert.Same(population, harness.Workspace.Population);
        Assert.Equal(2, harness.Workspace.RowCount);
    }

    [Fact]
    public async Task TheNationalIdsAreRecoveredBeforePreparePopulationCopiesThem()
    {
        // The ordering this method exists to hold: PreparePopulation copies Patient.NationalId onto
        // the row it builds (PersonMatrix.cs:151) and never reads the patient again, so a recovery
        // that ran afterwards would leave every Fødselsnummer blank while looking like it worked.
        // MainQuickStat.pas:536-540 puts AddNationalIds in exactly this slot.
        Harness harness = new();

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1), ShellWorkspaceTests.NewPatient(2)];
        harness.Patients.NationalIds[1] = OlasNationalId;
        harness.Patients.NationalIds[2] = KarisNationalId;

        await harness.LoadAsync();

        Assert.Equal([1, 2], Assert.Single(harness.Patients.NationalIdRequests));
        Assert.Equal([OlasNationalId, KarisNationalId], harness.Matrix.Rows.Select(row => row.NationalId));
    }

    [Fact]
    public async Task NothingIsAskedForWhenThePopulationAlreadyReturnedTheNationalIds()
    {
        // TPatientList.IncludesNationalId: some population procedures do select the column.
        Harness harness = new();

        harness.Patients.Cohort = [WithNationalId(1, OlasNationalId)];

        await harness.LoadAsync();

        Assert.Empty(harness.Patients.NationalIdRequests);
        Assert.Equal(OlasNationalId, Assert.Single(harness.Matrix.Rows).NationalId);
    }

    [Fact]
    public async Task AFailedRecoveryCostsTheColumnAndNotTheLoad()
    {
        // Both callers rely on this: the fetch is unconditional, so in an anonymous identification
        // mode a fatal failure here would destroy a load whose result never needed the ids at all.
        Harness harness = new();

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1)];
        harness.Patients.NationalIdThrows = new InvalidOperationException("Invalid object name 'dbo.Person'.");

        PopulationLoadResult result = await harness.LoadAsync();

        Assert.True(result.Loaded);
        Assert.Null(Assert.Single(harness.Matrix.Rows).NationalId);
    }

    [Fact]
    public async Task TheWorkspaceIsToldLastSoItsRowCountIsAlreadyRight()
    {
        // PersonMatrix raises no notifications, so IShellWorkspace cannot observe it and reads
        // Rows.Count at the moment it is told.  Told any earlier, HasPopulation would answer for the
        // previous cohort.
        Harness harness = new();

        harness.Patients.Cohort = [.. Enumerable.Range(1, 3).Select(ShellWorkspaceTests.NewPatient)];

        bool rowsWereReady = false;

        harness.Workspace.PopulationChanged += (_, _) =>
            rowsWereReady = harness.Workspace is { HasPopulation: true, RowCount: 3 };

        await harness.LoadAsync();

        Assert.True(rowsWereReady);
    }

    [Fact]
    public async Task TheSortOrderIsPersonIdWhateverItWasBefore()
    {
        // PersonId is the enum's zero, so prove the assignment happens by starting somewhere else.
        Harness harness = new();

        harness.Matrix.SortBy = MatrixSortOrder.ReverseName;

        harness.Patients.Cohort =
        [
            ShellWorkspaceTests.NewPatient(9),
            ShellWorkspaceTests.NewPatient(3),
            ShellWorkspaceTests.NewPatient(5),
        ];

        await harness.LoadAsync();

        Assert.Equal(MatrixSortOrder.PersonId, harness.Matrix.SortBy);
        Assert.Equal([3, 5, 9], harness.Matrix.Rows.Select(row => row.PersonId));
    }

    [Fact]
    public async Task ASecondLoadAfterACollectRunDoesNotThrow()
    {
        // The regression the leading Clear exists for: PersonMatrix.SortBy throws once the matrix is
        // locked and a collect run locks it, so without it the sequence works once and throws the
        // second time.  The Delphi survives it because LoadPopulationIntoGrid opens with
        // ClearPopulation, which clears fLocked (EPR.QA.Matrix.pas:211-215).
        Harness harness = new();

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1)];

        await harness.LoadAsync();

        harness.Matrix.Lock();

        PopulationLoadResult result = await harness.LoadAsync(ShellWorkspaceTests.NewPopulation(258));

        Assert.True(result.Loaded);
        Assert.False(harness.Matrix.IsLocked);
        Assert.Equal(258, harness.Workspace.Population?.ProcId);
    }

    [Fact]
    public async Task LoadingDropsTheColumnsOfThePreviousCollectRun()
    {
        // Clear, not ClearPopulation - the wider of the two, matching fGrid.Clear
        // (MainQuickStat.pas:532).  The package replay used to spell it ClearPopulation and this
        // assertion holds either way, which is the point: PreparePopulation opens with a full Clear
        // of its own, and the only statement between the two sorts an already-empty row list, so
        // nothing can observe which of them ran - PersonMatrix raises no notifications and neither
        // spelling can throw once both have unlocked.  That is why the two could be collapsed
        // without changing behaviour; what is pinned here is the outcome they share.
        Harness harness = new();

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1)];

        await harness.LoadAsync();

        ShellWorkspaceTests.AddColumn(harness.Matrix, "HBA1C", 1, 53);

        Assert.True(harness.Matrix.HasData);

        await harness.LoadAsync();

        Assert.False(harness.Matrix.HasData);
    }

    // ---------------------------------------------------------------------------------------
    //  The one outcome the loader hands back instead of acting on.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task AnUnresolvedParameterSetComesBackWholeAndUnreported()
    {
        // Handed back rather than reported, because the two callers word it differently: the
        // Populations tab shows a message box, the package replay only writes the status line.
        Harness harness = new();

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1)];
        harness.Parameters.Answer = new ParameterResolution
        {
            Succeeded = false,
            FailureReason = "Unknown placeholder ':Klinikk'.",
        };

        PopulationLoadResult result = await harness.LoadAsync();

        Assert.False(result.Loaded);
        Assert.Same(harness.Parameters.Answer, result.Unresolved);
    }

    [Fact]
    public async Task AnUnresolvedParameterSetLeavesThePreviousCohortAlone()
    {
        // PORT-PLAN.md §7.2: the Delphi cleared the grid at MainQuickStat.pas:532 and only then
        // asked, so a cancelled period dialog left the previous cohort on screen under the new
        // population's title.  Placeholders first is what fixes that, and this is the assertion.
        Harness harness = new();
        Population first = ShellWorkspaceTests.NewPopulation(257);

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1), ShellWorkspaceTests.NewPatient(2)];

        await harness.LoadAsync(first);

        harness.Parameters.Answer = new ParameterResolution { Succeeded = false, CancelledByUser = true };

        PopulationLoadResult result = await harness.LoadAsync(ShellWorkspaceTests.NewPopulation(258));

        Assert.False(result.Loaded);
        Assert.True(result.Unresolved?.CancelledByUser);

        Assert.Same(first, harness.Workspace.Population);
        Assert.Equal(2, harness.Workspace.RowCount);
        Assert.Single(harness.Patients.Loaded);
    }

    // ---------------------------------------------------------------------------------------
    //  Guards.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void EveryCollaboratorIsRequired()
    {
        FakeParameterResolver parameters = new();
        FakePatientRepository patients = new();
        ShellWorkspace workspace = new(ShellWorkspaceTests.NewMatrix());

        Assert.Throws<ArgumentNullException>(() => new PopulationLoader(null!, patients, workspace));
        Assert.Throws<ArgumentNullException>(() => new PopulationLoader(parameters, null!, workspace));
        Assert.Throws<ArgumentNullException>(() => new PopulationLoader(parameters, patients, null!));
    }

    [Fact]
    public async Task ThePopulationAndTheLoggerAreRequired()
    {
        // The logger is a parameter and not a field on purpose: NationalIdRecovery takes the
        // caller's log so that its lines carry the caller's category, and a logger of the loader's
        // own would make one line of a load say PopulationLoader and the four around it say
        // PopulationPickerViewModel.
        Harness harness = new();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => harness.Loader.LoadAsync(null!, NullLogger.Instance));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => harness.Loader.LoadAsync(ShellWorkspaceTests.NewPopulation(), null!));
    }
}
