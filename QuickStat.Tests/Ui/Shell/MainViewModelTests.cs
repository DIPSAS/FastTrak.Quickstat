using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Diagnostics;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;
using QuickStat.Services;
using QuickStat.Tests.Ui.Services;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Ui.Shell;

/// <summary>The shell view-model: chrome, the Progress block, and the dynamic Collections tab.</summary>
public class MainViewModelTests
{
    private sealed class Harness : IDisposable
    {
        internal Harness()
        {
            Matrix = ShellWorkspaceTests.NewMatrix();
            Workspace = new ShellWorkspace(Matrix);
            Progress = new ShellProgress(new InlineUiDispatcher());
            Settings = new InMemorySettingsStore();
            WindowState = new WindowStateService(
                Settings,
                new FakeMonitorLayout(new System.Windows.Rect(0, 0, 1920, 1040)),
                NullLogger<WindowStateService>.Instance);

            IdentificationPolicy identification = new();
            HeadlessNotificationPresenter presenter = new();

            Dataset = new DatasetViewModel(
                Workspace,
                identification,
                new FakeDatasetExporter(),
                new FakeTempFileTracker(),
                new FakeFileDialogService(),
                new FakeProcessLauncher(),
                new UserNotifier(presenter, NullLogger<UserNotifier>.Instance),
                Progress,
                NullLogger<DatasetViewModel>.Instance);

            ViewModel = new MainViewModel(
                Progress,
                Workspace,
                WindowState,
                new FakeApplicationInfo(),
                QuickStat.Tests.Ui.Populations.PopulationTestDoubles.NewTabViewModel(),
                new CollectionsTabViewModel(Workspace, identification),
                new PackagesTabViewModel(),
                Dataset);
        }

        internal PersonMatrix Matrix { get; }

        internal ShellWorkspace Workspace { get; }

        internal ShellProgress Progress { get; }

        internal InMemorySettingsStore Settings { get; }

        internal WindowStateService WindowState { get; }

        internal DatasetViewModel Dataset { get; }

        internal MainViewModel ViewModel { get; }

        internal void LoadPopulation(int patients = 1)
        {
            Matrix.PreparePopulation(
                [.. Enumerable.Range(1, patients).Select(ShellWorkspaceTests.NewPatient)]);

            Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation());
        }

        public void Dispose() => ViewModel.Dispose();
    }

    [Fact]
    public void TheChromeIsWhatTheDfmSays()
    {
        using Harness harness = new();

        Assert.Equal("FastTrak QuickStat", harness.ViewModel.WindowTitle);
        Assert.Equal("QuickStat", harness.ViewModel.ProductName);
        Assert.Equal("22.12.21.547", harness.ViewModel.VersionText);
    }

    [Fact]
    public void ItOpensOnThePopulationTab()
    {
        using Harness harness = new();

        Assert.Equal(MainViewModel.PopulationTabIndex, harness.ViewModel.SelectedSelectionTab);
    }

    [Fact]
    public void TheCollectionsTabIsHiddenUntilAPopulationIsLoaded()
    {
        // §B.0, and easy to miss: hidden AND disabled at FormCreate.
        using Harness harness = new();

        Assert.False(harness.ViewModel.HasPopulation);

        harness.LoadPopulation();

        Assert.True(harness.ViewModel.HasPopulation);
    }

    [Fact]
    public void AnEmptyPopulationLeavesTheCollectionsTabHidden()
    {
        using Harness harness = new();

        harness.Matrix.PreparePopulation([]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation());

        Assert.False(harness.ViewModel.HasPopulation);
    }

    [Fact]
    public void APreparedPopulationSwitchesToTheCollectionsTab()
    {
        // AfterPopulationSelect: pgSelections.ActivePage := tbsDataElements.
        using Harness harness = new();

        harness.LoadPopulation();
        harness.Workspace.RequestCollectionsTab();

        Assert.Equal(MainViewModel.CollectionsTabIndex, harness.ViewModel.SelectedSelectionTab);
    }

    [Fact]
    public void AnEmptyPopulationDoesNotSwitchToAHiddenTab()
    {
        // LoadPopulationIntoGrid decides visibility before AfterPopulationSelect switches, so an
        // empty cohort must leave the user where they are rather than on a collapsed tab.
        using Harness harness = new();

        harness.Matrix.PreparePopulation([]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation());
        harness.Workspace.RequestCollectionsTab();

        Assert.Equal(MainViewModel.PopulationTabIndex, harness.ViewModel.SelectedSelectionTab);
    }

    [Fact]
    public void LosingThePopulationMovesTheUserOffTheCollectionsTab()
    {
        // A collapsed TabItem that is still selected leaves the TabControl blank - a WPF trap with
        // no Delphi equivalent, because TRzPageControl picks a new active page itself.
        using Harness harness = new();

        harness.LoadPopulation();
        harness.Workspace.RequestCollectionsTab();
        Assert.Equal(MainViewModel.CollectionsTabIndex, harness.ViewModel.SelectedSelectionTab);

        harness.Workspace.SetPopulation(null);

        Assert.Equal(MainViewModel.PopulationTabIndex, harness.ViewModel.SelectedSelectionTab);
    }

    [Fact]
    public void ThePackagesTabIsAlwaysAvailable()
    {
        using Harness harness = new();

        harness.ViewModel.SelectedSelectionTab = MainViewModel.PackagesTabIndex;

        Assert.Equal(MainViewModel.PackagesTabIndex, harness.ViewModel.SelectedSelectionTab);
    }

    [Fact]
    public void TheProgressBlockMirrorsTheService()
    {
        using Harness harness = new();

        Assert.Equal("Progress", harness.ViewModel.ProgressHeader);
        Assert.Equal("Program is idle", harness.ViewModel.ProgressInfo);

        harness.Progress.Done();

        Assert.Equal("Task completed", harness.ViewModel.ProgressInfo);
        Assert.Equal(100, harness.ViewModel.ProgressPercent);
    }

    [Fact]
    public void TheProgressBlockRaisesTheShellNames()
    {
        // The service's property names differ from the view-model's, so the mapping is where this
        // can silently stop updating.
        using Harness harness = new();
        List<string?> raised = [];

        harness.ViewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        harness.Progress.Fail("boom");

        Assert.Contains(nameof(MainViewModel.ProgressInfo), raised);
        Assert.Contains(nameof(MainViewModel.ProgressIsError), raised);
        Assert.True(harness.ViewModel.ProgressIsError);
    }

    [Fact]
    public void BusyIsMirroredToo()
    {
        using Harness harness = new();
        List<string?> raised = [];

        harness.ViewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        using (harness.Progress.BeginOperation("Connecting ..."))
        {
            Assert.True(harness.ViewModel.IsBusy);
        }

        Assert.False(harness.ViewModel.IsBusy);
        Assert.Contains(nameof(MainViewModel.IsBusy), raised);
    }

    [Fact]
    public void TheSplitterStartsAtTheDfmPosition()
    {
        // §I.1: the .dfm says 293, the screenshots show about 336 and the brief says about 330.  The
        // spec specifies 293 with a minimum of 260, and step 3.1 takes that.
        using Harness harness = new();

        Assert.Equal(293, harness.ViewModel.SplitterPosition);
        Assert.Equal(260, MainViewModel.MinimumSplitterPosition);
    }

    [Fact]
    public void TheSplitterPositionIsPersistedAndRestored()
    {
        // An addition; §G.1 recommends it and the Delphi never saves splMain.
        using Harness first = new();

        first.ViewModel.PersistSplitterPosition(336);

        Assert.Equal(336, first.WindowState.GetSplitterPosition(MainViewModel.DefaultSplitterPosition));
    }

    [Fact]
    public void ASplitterPositionBelowTheMinimumIsIgnored()
    {
        using Harness harness = new();

        harness.ViewModel.PersistSplitterPosition(10);

        Assert.Equal(293, harness.ViewModel.SplitterPosition);
    }

    [Fact]
    public void DisposingUnsubscribesFromTheSharedServices()
    {
        // The progress service and the workspace are singletons that outlive the window, so a
        // view-model that stays subscribed keeps the whole graph reachable.  The properties are
        // read-through, so what disposal stops is the notification, not the value.
        Harness harness = new();
        List<string?> raised = [];

        harness.ViewModel.Dispose();
        harness.ViewModel.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        harness.Progress.Fail("after disposal");
        harness.LoadPopulation();

        Assert.Empty(raised);
    }
}
