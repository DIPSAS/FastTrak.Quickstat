using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;
using QuickStat.Services;
using QuickStat.Tests.Ui.Services;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Ui.Collections;

/// <summary>
/// The <c>Collections</c> tab: the list, the enable rule, and the collect run.
/// </summary>
/// <remarks>
/// Delphi <c>AfterLogin</c> (<c>MainQuickStat.pas:471-493</c>),
/// <c>ValidateCollectorSelection</c> (<c>:690-713</c>) and <c>actCollectDataExecute</c>
/// (<c>:633-681</c>). Nothing here touches a database: the registry and the runner are the two
/// seams, and both are faked.
/// </remarks>
public class CollectionsTabViewModelTests
{
    /// <summary>Everything one case needs, wired the way the container wires it.</summary>
    private sealed class Harness : IDisposable
    {
        internal Harness()
        {
            Matrix = ShellWorkspaceTests.NewMatrix();
            Workspace = new ShellWorkspace(Matrix);
            Identification = new IdentificationPolicy();
            Registry = new FakeCollectorRegistry();
            Runner = new RecordingCollectorRunner();
            Session = new FakeSessionService();
            Progress = new ShellProgress(new InlineUiDispatcher());
            Notifier = new RecordingUserNotifier();

            ViewModel = new CollectionsTabViewModel(
                Workspace,
                Identification,
                Registry,
                Runner,
                Session,
                Progress,
                new InlineUiDispatcher(),
                Notifier,
                NullLogger<CollectionsTabViewModel>.Instance);
        }

        internal PersonMatrix Matrix { get; }

        internal ShellWorkspace Workspace { get; }

        internal IdentificationPolicy Identification { get; }

        internal FakeCollectorRegistry Registry { get; }

        internal RecordingCollectorRunner Runner { get; }

        internal FakeSessionService Session { get; }

        internal ShellProgress Progress { get; }

        internal RecordingUserNotifier Notifier { get; }

        internal CollectionsTabViewModel ViewModel { get; }

        /// <summary>Puts a cohort in the matrix, as a population load does.</summary>
        /// <param name="personIds">The people.</param>
        internal void LoadPopulation(params int[] personIds)
        {
            Matrix.PreparePopulation([.. personIds.Select(ShellWorkspaceTests.NewPatient)]);
            Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation());
        }

        /// <summary>Signals a login and waits for the list to be rebuilt.</summary>
        /// <param name="studyId">The study id the session carries.</param>
        /// <returns>A task that completes when <c>DataElements</c> is filled.</returns>
        internal async Task LoginAsync(int studyId = 42)
        {
            Session.Raise(FakeSessionService.NewSession(studyId: studyId));

            await ViewModel.PendingReload;
        }

        /// <summary>Ticks elements by collector name.</summary>
        /// <param name="names">The names to tick.</param>
        internal void Tick(params string[] names)
        {
            foreach (DataElementViewModel element in ViewModel.DataElements
                         .Where(element => names.Contains(element.Name, StringComparer.Ordinal)))
            {
                element.IsChecked = true;
            }
        }

        public void Dispose() => ViewModel.Dispose();
    }

    // ---------------------------------------------------------------- the list (AfterLogin)

    [Fact]
    public void ThereAreNoDataElementsBeforeALogin()
    {
        using Harness harness = new();

        Assert.Empty(harness.ViewModel.DataElements);
        Assert.False(harness.ViewModel.CollectDataCommand.CanExecute(null));
    }

    [Fact]
    public async Task ALoginFillsTheListFromTheRegistry()
    {
        using Harness harness = new();

        harness.Registry.With("C", "Diabetes: Behandling").With("A", "^ Alder");

        await harness.LoginAsync();

        Assert.Equal(["A", "C"], harness.ViewModel.DataElements.Select(element => element.Name));
    }

    [Fact]
    public async Task TheListIsSortedByTitleAndNotByRegistryOrder()
    {
        // PORT-PLAN.md §6. Registry order decides which collectors exist; the sorted list decides
        // where their columns land.
        using Harness harness = new();

        harness.Registry
            .With("REG1", "Nyrefunksjon")
            .With("REG2", "^ Kjønn")
            .With("REG3", "Anemi")
            .With("REG4", "^ Alder");

        await harness.LoginAsync();

        Assert.Equal(
            ["^ Alder", "^ Kjønn", "Anemi", "Nyrefunksjon"],
            harness.ViewModel.DataElements.Select(element => element.Title));
    }

    [Fact]
    public async Task ALoginCopiesTheStudyIdOntoTheMatrix()
    {
        // The other half of Delphi AfterLogin's fGrid.Data.PrepareStudy. Nothing else in the port
        // sets it, and a saved package records it (MainQuickStat.pas:868).
        using Harness harness = new();

        await harness.LoginAsync(studyId: 4711);

        Assert.Equal(4711, harness.Matrix.StudyId);
    }

    [Fact]
    public async Task ASecondLoginReplacesTheListRatherThanAppendingToIt()
    {
        using Harness harness = new();

        harness.Registry.With("A", "Alfa");

        await harness.LoginAsync();

        harness.Registry.Next.Clear();
        harness.Registry.With("B", "Beta");

        await harness.LoginAsync();

        Assert.Equal(["B"], harness.ViewModel.DataElements.Select(element => element.Name));
        Assert.Equal(2, harness.Registry.BuildCount);
    }

    [Fact]
    public async Task DisconnectingEmptiesTheListAndUnticksEverything()
    {
        using Harness harness = new();

        harness.Registry.With("A", "Alfa");

        await harness.LoginAsync();

        harness.Tick("A");

        Assert.Single(harness.Workspace.CheckedCollectorNames);

        harness.Session.Raise(null);

        Assert.Empty(harness.ViewModel.DataElements);
        Assert.Empty(harness.Workspace.CheckedCollectorNames);
        Assert.False(harness.ViewModel.CollectDataCommand.CanExecute(null));
    }

    [Fact]
    public async Task ALoginSaysLoadingCollectorsAndThenTaskCompleted()
    {
        using Harness harness = new();

        List<string> info = [];

        harness.Progress.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ShellProgress.Info))
            {
                info.Add(harness.Progress.Info);
            }
        };

        await harness.LoginAsync();

        Assert.Equal([CollectionsTabViewModel.LoadingCollectorsText, ShellProgress.CompletedText], info);
        Assert.False(harness.Progress.IsBusy);
    }

    [Fact]
    public async Task AFailedRegistryBuildLeavesAnEmptyListAndARedStatusLine()
    {
        using Harness harness = new();

        harness.Registry.With("A", "Alfa");
        harness.Registry.Throws = new InvalidOperationException("Report.GetFormClasses is missing.");

        await harness.LoginAsync();

        Assert.Empty(harness.ViewModel.DataElements);
        Assert.True(harness.Progress.IsError);
        Assert.Equal("Report.GetFormClasses is missing.", harness.Progress.Info);
        Assert.False(harness.Progress.IsBusy);
    }

    [Fact]
    public async Task AFailedRegistryBuildDoesNotFaultTheTaskTheEventHandlerAbandoned()
    {
        // The SessionChanged handler cannot await, so an escaping exception would end up in
        // TaskScheduler.UnobservedTaskException and, through App.Report, in a crash dialog.
        using Harness harness = new();

        harness.Registry.Throws = new InvalidOperationException("boom");

        harness.Session.Raise(FakeSessionService.NewSession());

        await harness.ViewModel.PendingReload;

        Assert.Equal(TaskStatus.RanToCompletion, harness.ViewModel.PendingReload.Status);
    }

    // ---------------------------------------------------------- ValidateCollectorSelection

    [Fact]
    public async Task CollectDataIsEnabledWhileSomethingIsTicked()
    {
        using Harness harness = new();

        harness.Registry.With("A", "Alfa").With("B", "Beta");

        await harness.LoginAsync();

        Assert.False(harness.ViewModel.CollectDataCommand.CanExecute(null));

        harness.ViewModel.DataElements[0].IsChecked = true;

        Assert.True(harness.ViewModel.CollectDataCommand.CanExecute(null));

        harness.ViewModel.DataElements[0].IsChecked = false;

        Assert.False(harness.ViewModel.CollectDataCommand.CanExecute(null));
    }

    [Fact]
    public async Task TickingPublishesTheNamesInCheckListOrder()
    {
        using Harness harness = new();

        harness.Registry.With("KJONN", "^ Kjønn").With("ALDER", "^ Alder").With("ANEMI", "Anemi");

        await harness.LoginAsync();

        harness.Tick("ANEMI");
        harness.Tick("KJONN");

        // Names, not titles - a package stores names - and in list order, not click order.
        Assert.Equal(["KJONN", "ANEMI"], harness.Workspace.CheckedCollectorNames);
    }

    // ------------------------------------------------------------------ ApplyPackagedSelection

    [Fact]
    public async Task ApplyingAPackageUnticksEverythingFirst()
    {
        // Delphi: cbDataCollector.CheckAll( cbUnchecked ) before the loop (MainQuickStat.pas:796).
        using Harness harness = new();

        harness.Registry.With("A", "Alfa").With("B", "Beta");

        await harness.LoginAsync();

        harness.Tick("A");

        IReadOnlyList<string> unknown = harness.ViewModel.ApplyPackagedSelection(["B"]);

        Assert.Empty(unknown);
        Assert.Equal(["B"], harness.Workspace.CheckedCollectorNames);
    }

    [Fact]
    public async Task ApplyingAPackageMatchesOnTitleAsWellAsName()
    {
        // TryFindCollector accepts either (MainQuickStat.pas:716-732), and so does ICollectorRegistry.
        using Harness harness = new();

        harness.Registry.With("A", "Alfa").With("B", "Beta");

        await harness.LoginAsync();

        Assert.Empty(harness.ViewModel.ApplyPackagedSelection(["beta"]));
        Assert.Equal(["B"], harness.Workspace.CheckedCollectorNames);
    }

    [Fact]
    public async Task ApplyingAPackageReturnsTheNamesItCouldNotFind()
    {
        using Harness harness = new();

        harness.Registry.With("A", "Alfa");

        await harness.LoginAsync();

        IReadOnlyList<string> unknown = harness.ViewModel.ApplyPackagedSelection(["A", "GONE", "ALSO_GONE"]);

        Assert.Equal(["GONE", "ALSO_GONE"], unknown);
        Assert.Equal(["A"], harness.Workspace.CheckedCollectorNames);
    }

    [Fact]
    public async Task ApplyingAPackagePublishesTheCheckedNamesOnce()
    {
        using Harness harness = new();

        harness.Registry.With("A", "Alfa").With("B", "Beta").With("C", "Cesium");

        await harness.LoginAsync();

        int pushes = 0;

        harness.Workspace.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IShellWorkspace.CheckedCollectorNames))
            {
                pushes++;
            }
        };

        _ = harness.ViewModel.ApplyPackagedSelection(["A", "C"]);

        Assert.Equal(1, pushes);
        Assert.Equal(["A", "C"], harness.Workspace.CheckedCollectorNames);
    }

    // --------------------------------------------------------------------- the collect run

    [Fact]
    public async Task TheRunWalksTheListFromIndexZeroAndSkipsWhatIsNotTicked()
    {
        using Harness harness = new();

        harness.Registry
            .With("LAST", "Nyrefunksjon")
            .With("SKIP", "Anemi")
            .With("FIRST", "^ Alder");

        await harness.LoginAsync();
        harness.LoadPopulation(8, 13);
        harness.Tick("FIRST", "LAST");

        await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

        Assert.Equal(["FIRST", "LAST"], harness.Runner.Ran);
    }

    [Fact]
    public async Task ColumnOrderIsCheckListOrder()
    {
        // The §6 parity item, end to end: the sorted list is walked from index 0 and the matrix
        // appends columns in the order it is handed them, so the list is the column order.
        using Harness harness = new();

        harness.Registry
            .With("REG_FIRST", "Nyrefunksjon")
            .With("REG_SECOND", "^ Alder");

        harness.Runner
            .Producing("REG_FIRST", "GFR", "KREA")
            .Producing("REG_SECOND", "AGE");

        await harness.LoginAsync();
        harness.LoadPopulation(8);
        harness.Tick("REG_FIRST", "REG_SECOND");

        await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

        Assert.Equal(["AGE", "GFR", "KREA"], harness.Matrix.Columns.Select(column => column.VarName));
    }

    [Fact]
    public async Task TheRunGivesTheRunnerTheCohortAndTheStudyId()
    {
        using Harness harness = new();

        harness.Registry.With("A", "Alfa");

        await harness.LoginAsync(studyId: 4711);
        harness.LoadPopulation(52, 8, 13);
        harness.Tick("A");

        await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

        // PreparePopulation sorts by PersonId, and the cohort is the matrix's row order.
        Assert.Equal([8, 13, 52], harness.Runner.Cohorts[0]);
        Assert.Equal(4711, harness.Runner.StudyIds[0]);
    }

    [Fact]
    public async Task TheRunLocksTheMatrixAndThenAnnouncesTheChange()
    {
        // 07-ui-contracts.md §3.1: Lock, then NotifyDataChanged, in that order.
        using Harness harness = new();

        harness.Registry.With("A", "Alfa");

        await harness.LoginAsync();
        harness.LoadPopulation(8);
        harness.Tick("A");

        bool lockedWhenAnnounced = false;
        int announcements = 0;

        harness.Workspace.DataChanged += (_, _) =>
        {
            announcements++;
            lockedWhenAnnounced = harness.Matrix.IsLocked;
        };

        await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

        Assert.Equal(1, announcements);
        Assert.True(lockedWhenAnnounced);
        Assert.True(harness.Workspace.HasData);
    }

    [Fact]
    public async Task TheRunClearsTheColumnsOfWhateverWasThereBefore()
    {
        // Delphi: fGrid.Data.ClearVariables is the first statement of actCollectDataExecute.
        using Harness harness = new();

        harness.Registry.With("A", "Alfa");
        harness.Runner.Producing("A", "AGE");

        await harness.LoginAsync();
        harness.LoadPopulation(8);

        ShellWorkspaceTests.AddColumn(harness.Matrix, "STALE", 8, 1);

        harness.Tick("A");

        await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

        Assert.Equal(["AGE"], harness.Matrix.Columns.Select(column => column.VarName));
    }

    [Fact]
    public async Task TheStatusLineNamesEachElementAsItIsCollected()
    {
        // Delphi SetInfo( selectedCollector.Title ) inside the loop; §G.6 lists "<collector title>"
        // as one of the status texts.
        using Harness harness = new();

        harness.Registry.With("A", "^ Alder").With("B", "Nyrefunksjon");

        await harness.LoginAsync();
        harness.LoadPopulation(8);
        harness.Tick("A", "B");

        List<string> seen = [];

        harness.Runner.Observe = _ => seen.Add(harness.Progress.Info);

        await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

        Assert.Equal(["^ Alder", "Nyrefunksjon"], seen);
        Assert.Equal(ShellProgress.CompletedText, harness.Progress.Info);
    }

    [Fact]
    public async Task TheRunIsBusyThroughoutAndNotAfterwards()
    {
        // §G.3: the Delphi's Screen.Cursor := crSqlWait for the whole of actCollectDataExecute.
        using Harness harness = new();

        harness.Registry.With("A", "Alfa");

        await harness.LoginAsync();
        harness.LoadPopulation(8);
        harness.Tick("A");

        bool busyDuring = false;

        harness.Runner.Observe = _ => busyDuring = harness.Progress.IsBusy;

        await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

        Assert.True(busyDuring);
        Assert.False(harness.Progress.IsBusy);
    }

    [Fact]
    public async Task ARunInsideAnotherOperationLeavesThatOperationBusy()
    {
        // Step 3.4's package replay runs a collect inside its own scope; BeginOperation counts, which
        // is why the Delphi saves and restores Screen.Cursor rather than assigning crDefault (§G.3).
        using Harness harness = new();

        harness.Registry.With("A", "Alfa");

        await harness.LoginAsync();
        harness.LoadPopulation(8);
        harness.Tick("A");

        using (IDisposable outer = harness.Progress.BeginOperation("Replaying a package ..."))
        {
            await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

            Assert.True(harness.Progress.IsBusy);
        }

        Assert.False(harness.Progress.IsBusy);
    }

    [Fact]
    public async Task TheElementBeingCollectedIsMarkedAndThenUnmarked()
    {
        // §G.4: the visual feedback the Delphi got from ItemIndex := n.
        using Harness harness = new();

        harness.Registry.With("A", "^ Alder").With("B", "Nyrefunksjon");

        await harness.LoginAsync();
        harness.LoadPopulation(8);
        harness.Tick("A", "B");

        List<string> marked = [];

        harness.Runner.Observe = _ => marked.AddRange(
            harness.ViewModel.DataElements.Where(e => e.IsCollecting).Select(e => e.Name));

        await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

        Assert.Equal(["A", "B"], marked);
        Assert.Null(harness.ViewModel.CurrentlyCollecting);
        Assert.DoesNotContain(harness.ViewModel.DataElements, element => element.IsCollecting);
    }

    [Fact]
    public async Task TheRunBracketsItselfWithTheTwoScrollEvents()
    {
        using Harness harness = new();

        harness.Registry.With("A", "Alfa");

        await harness.LoginAsync();
        harness.LoadPopulation(8);
        harness.Tick("A");

        List<string> events = [];

        harness.ViewModel.CollectRunStarting += (_, _) => events.Add("start");
        harness.ViewModel.CollectRunFinished += (_, _) => events.Add("finish");
        harness.Runner.Observe = _ => events.Add("collect");

        await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

        Assert.Equal(["start", "collect", "finish"], events);
    }

    [Fact]
    public async Task AFailingCollectorAbortsTheRunAndReportsIt()
    {
        // The Delphi lets the exception escape actCollectDataExecute, so fGrid.Lock is skipped and
        // the VCL shows its own dialog.  Here the dialog is explicit and the matrix stays unlocked,
        // which is what stops DatasetViewModel exporting a half-collected file.
        using Harness harness = new();

        harness.Registry.With("A", "Alfa").With("B", "Beta");
        harness.Runner.ThrowFor = "B";
        harness.Runner.Failure = new InvalidOperationException("Timeout expired.");

        await harness.LoginAsync();
        harness.LoadPopulation(8);
        harness.Tick("A", "B");

        int announcements = 0;

        harness.Workspace.DataChanged += (_, _) => announcements++;

        await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

        Assert.False(harness.Matrix.IsLocked);
        Assert.True(harness.Progress.IsError);
        Assert.Equal("Timeout expired.", harness.Progress.Info);
        Assert.Contains(harness.Notifier.Messages, message => message.StartsWith("error:", StringComparison.Ordinal));

        // Delphi UpdateGridInfo is in the finally, so the caption refreshes either way.
        Assert.Equal(1, announcements);
        Assert.Null(harness.ViewModel.CurrentlyCollecting);
        Assert.DoesNotContain(harness.ViewModel.DataElements, element => element.IsCollecting);
    }

    [Fact]
    public async Task ASecondRunSimplyReCollects()
    {
        // Clicking Collect data twice is ordinary use - tick a few more elements, run again - and the
        // Delphi allows it: fLocked gates painting and export, never AddData
        // (FastTrak/EPR.QA.Matrix.pas:214, :236, :332).  The port briefly did not, because
        // PersonMatrix.ClearVariables did not lift the lock that Lock() had set at the end of the
        // previous run, so the second click threw out of AddColumns.  This test is the reason
        // ClearVariables now unlocks.
        using Harness harness = new();

        harness.Registry.With("A", "Alfa").With("B", "Beta");

        await harness.LoginAsync();
        harness.LoadPopulation(8);
        harness.Tick("A");

        await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

        Assert.True(harness.Matrix.IsLocked);
        Assert.Single(harness.Runner.Ran);

        harness.Tick("B");

        await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

        // Three collector invocations, not two: the second run collects everything still ticked, so
        // Alfa runs again alongside Beta.  That is the Delphi's loop over the whole check list.
        Assert.Equal(["A", "A", "B"], harness.Runner.Ran);
        Assert.True(harness.Matrix.IsLocked);
        Assert.Empty(harness.Notifier.Messages);
    }

    [Fact]
    public async Task ARunWithNothingTickedLeavesTheStatusLineAlone()
    {
        // Reachable only from step 3.4's replay, where a package may name nothing that still exists.
        using Harness harness = new();

        harness.Registry.With("A", "Alfa");

        await harness.LoginAsync();
        harness.LoadPopulation(8);

        harness.Progress.SetInfo("Something else");

        await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

        Assert.Empty(harness.Runner.Ran);
        Assert.True(harness.Matrix.IsLocked);
        Assert.Equal(ShellProgress.CompletedText, harness.Progress.Info);
    }

    [Fact]
    public async Task ACancelledRunLeavesTheMatrixUnlockedAndTheStatusLineIdle()
    {
        using Harness harness = new();

        harness.Registry.With("A", "Alfa").With("B", "Beta");

        await harness.LoginAsync();
        harness.LoadPopulation(8);
        harness.Tick("A", "B");

        harness.Runner.Observe = _ => harness.ViewModel.CollectDataCommand.Cancel();

        await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

        Assert.Single(harness.Runner.Ran);
        Assert.False(harness.Matrix.IsLocked);
        Assert.Equal(ShellProgress.IdleText, harness.Progress.Info);
        Assert.False(harness.Progress.IsBusy);
    }

    [Fact]
    public async Task ThePackageReplayShapeProducesColumnsInCheckListOrder()
    {
        // Step 3.4's replay, end to end on this side of the seam: it ticks by stored name, then
        // calls ExecuteAsync(null) directly - deliberately ignoring CanExecute, because the Delphi
        // calls actCollectDataExecute rather than the action's enabled state
        // (MainQuickStat.pas:794-806) - all inside its own wait-cursor scope.
        using Harness harness = new();

        harness.Registry
            .With("GBD.WEIGHT", "GBD: Vekt fra siste 2 mnd")
            .With("DEMO.AGE", "^ Alder")
            .With("LAB.ANEMIA", "Labdata: Anemi");

        harness.Runner
            .Producing("GBD.WEIGHT", "WEIGHT")
            .Producing("DEMO.AGE", "AGE");

        await harness.LoginAsync();
        harness.LoadPopulation(8, 13);

        // Stored order is not check-list order, and it must not become column order.
        IReadOnlyList<string> unknown = harness.ViewModel.ApplyPackagedSelection(["GBD.WEIGHT", "DEMO.AGE"]);

        Assert.Empty(unknown);

        using (IDisposable replay = harness.Progress.BeginOperation("Replaying a package ..."))
        {
            await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

            Assert.True(harness.Progress.IsBusy);
        }

        Assert.Equal(["DEMO.AGE", "GBD.WEIGHT"], harness.Runner.Ran);
        Assert.Equal(["AGE", "WEIGHT"], harness.Matrix.Columns.Select(column => column.VarName));
        Assert.True(harness.Matrix.IsLocked);
        Assert.False(harness.Progress.IsBusy);
    }

    [Fact]
    public async Task TheRunNeedsNothingTheUserTouchedOnThisTab()
    {
        // The replay path: the user is on the Packages tab, has selected nothing here, and the
        // population was loaded programmatically.  Nothing the run reads may come from the view.
        using Harness harness = new();

        harness.Registry.With("A", "Alfa");

        await harness.LoginAsync();
        harness.LoadPopulation(8);

        harness.ViewModel.DataElements[0].IsChecked = true;

        Assert.Null(harness.ViewModel.CurrentlyCollecting);

        await harness.ViewModel.CollectDataCommand.ExecuteAsync(null);

        Assert.Single(harness.Runner.Ran);
    }

    // ------------------------------------------------------------------- the two pass-throughs

    [Fact]
    public void TheIdentificationModeIsThePolicysAndNotACopy()
    {
        using Harness harness = new();

        Assert.Equal(PersonIdentification.PersonIdOnly, harness.ViewModel.Identification);

        harness.ViewModel.Identification = PersonIdentification.Full;

        Assert.Equal(PersonIdentification.Full, harness.Identification.Mode);

        harness.Identification.Mode = PersonIdentification.RandomPersonId;

        Assert.Equal(PersonIdentification.RandomPersonId, harness.ViewModel.Identification);
    }

    [Fact]
    public void TheTimestampFlagIsTheWorkspacesAndNotACopy()
    {
        using Harness harness = new();

        Assert.False(harness.ViewModel.ExportTimestamps);

        harness.ViewModel.ExportTimestamps = true;

        Assert.True(harness.Workspace.ExportTimestamps);
    }

    [Fact]
    public async Task DisposingStopsListeningToTheSession()
    {
        Harness harness = new();

        harness.Registry.With("A", "Alfa");

        harness.ViewModel.Dispose();

        harness.Session.Raise(FakeSessionService.NewSession());

        await harness.ViewModel.PendingReload;

        Assert.Empty(harness.ViewModel.DataElements);
        Assert.Equal(0, harness.Registry.BuildCount);
    }
}
