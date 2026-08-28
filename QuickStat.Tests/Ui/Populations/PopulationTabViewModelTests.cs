using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Configuration;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Ui.Populations;

/// <summary>
/// The <c>Population</c> tab: the project combo box and what choosing an entry sets off.
/// <c>05-ui-spec.md</c> §B.1, §G.5.
/// </summary>
public class PopulationTabViewModelTests
{
    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

        internal CultureScope(string name) => CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);

        public void Dispose() => CultureInfo.CurrentCulture = _previous;
    }

    private static PopulationHarness WithProjects(params string[] names)
    {
        PopulationHarness harness = new();

        harness.Connections.Connections = [.. names.Select(PopulationTestDoubles.NewConnection)];

        return harness;
    }

    private static PopulationTabViewModel NewTab(PopulationHarness harness) => new(
        harness.Picker,
        harness.Connections,
        harness.Coordinator,
        harness.WindowState,
        harness.Notifier,
        NullLogger<PopulationTabViewModel>.Instance);

    private static Task Settle(PopulationTabViewModel tab) => tab.ConnectCommand.ExecutionTask ?? Task.CompletedTask;

    // ---------------------------------------------------------------------------------------
    // The combo box
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void TheChromeIsWhatTheDfmSays()
    {
        Assert.Equal("Select database", PopulationTabViewModel.DatabaseHeader);
        Assert.Equal("Select population", PopulationTabViewModel.PopulationHeader);
    }

    [Fact]
    public void TheProjectListIsReadFromTheDeployedConfigFile()
    {
        using PopulationHarness harness = WithProjects("Testdatabase (NDV)");

        PopulationTabViewModel tab = NewTab(harness);

        Assert.Equal([harness.Connections.DefaultConfigFilePath], harness.Connections.RequestedPaths[^1..]);
        Assert.Equal(["Testdatabase (NDV)"], tab.Projects.Select(project => project.Name));
    }

    [Fact]
    public void TheProjectListIsSortedByNameIgnoringCase()
    {
        // cbProject.Sorted := true, and TStringList.Sorted compares with AnsiCompareText - which is
        // case-insensitive, so ordinal would put every lower-case name after every upper-case one.
        using PopulationHarness harness = WithProjects("gamma", "Beta", "alpha");

        PopulationTabViewModel tab = NewTab(harness);

        Assert.Equal(["alpha", "Beta", "gamma"], tab.Projects.Select(project => project.Name));
    }

    [Fact]
    public void TheProjectOrderIsTheSameUnderAnyCulture()
    {
        // The test above runs under the machine's nn-NO; this one pins the other Norwegian locale,
        // which is the one the application actually meets in the field.
        using CultureScope culture = new("nb-NO");
        using PopulationHarness harness = WithProjects("gamma", "Beta", "alpha");

        PopulationTabViewModel tab = NewTab(harness);

        Assert.Equal(["alpha", "Beta", "gamma"], tab.Projects.Select(project => project.Name));
    }

    [Fact]
    public void NothingIsPreselected()
    {
        using PopulationHarness harness = WithProjects("Testdatabase (NDV)", "Produksjon");

        PopulationTabViewModel tab = NewTab(harness);

        // §B.1: "No item is preselected in cbProject" - the user must pick one, which connects.
        Assert.Null(tab.SelectedProject);
        Assert.Empty(harness.Coordinator.Connected);
    }

    [Fact]
    public void ARememberedDatabaseIsNeitherPreselectedNorConnected()
    {
        // Decision (g) of 07-ui-contracts.md §5, taken by step 3.2. Recorded on
        // PopulationTabViewModel.LastDatabase together with what it would cost to reverse.
        using PopulationHarness harness = WithProjects("Testdatabase (NDV)");

        harness.WindowState.SetLastDatabase("Testdatabase (NDV)");

        PopulationTabViewModel tab = NewTab(harness);

        Assert.Equal("Testdatabase (NDV)", tab.LastDatabase);
        Assert.Null(tab.SelectedProject);
        Assert.Empty(harness.Coordinator.Connected);
    }

    [Fact]
    public void AMissingConfigFileLeavesAnEmptyListRatherThanFailing()
    {
        // MainQuickStat.pas:392-398 logs and carries on; Docs/Port/06-contracts.md keeps the log and
        // drops the modal dialog, and IConnectionCatalog.Load returns empty for a missing file.
        using PopulationHarness harness = new();

        PopulationTabViewModel tab = NewTab(harness);

        Assert.Empty(tab.Projects);
    }

    [Fact]
    public void AnUnreadableConfigFileDoesNotTakeTheShellDown()
    {
        // This runs while the container builds MainViewModel, so throwing here would stop the window
        // ever appearing - strictly worse than the Delphi, which shows a dialog and carries on.
        using PopulationHarness harness = new();

        harness.Connections.Throws = new QuickStatConfigurationException("Broken XML.")
        {
            FilePath = @"C:\x.config.xml",
        };

        PopulationTabViewModel tab = NewTab(harness);

        Assert.Empty(tab.Projects);
    }

    [Fact]
    public void EveryCollaboratorIsRequired()
    {
        using PopulationHarness harness = new();

        Assert.Throws<ArgumentNullException>(() => new PopulationTabViewModel(
            null!,
            harness.Connections,
            harness.Coordinator,
            harness.WindowState,
            harness.Notifier,
            NullLogger<PopulationTabViewModel>.Instance));

        Assert.Throws<ArgumentNullException>(() => new PopulationTabViewModel(
            harness.Picker,
            null!,
            harness.Coordinator,
            harness.WindowState,
            harness.Notifier,
            NullLogger<PopulationTabViewModel>.Instance));
    }

    // ---------------------------------------------------------------------------------------
    // Choosing a project - SelectConnection
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task ChoosingAProjectConnectsThroughTheCoordinator()
    {
        using PopulationHarness harness = WithProjects("Testdatabase (NDV)");

        PopulationTabViewModel tab = NewTab(harness);

        tab.SelectedProject = tab.Projects[0];

        await Settle(tab);

        // The coordinator is the whole of SelectConnection, ICaptionLoader included; the tab must
        // not reach for ISessionService itself (07-ui-contracts.md §3.3).
        Assert.Equal(["Testdatabase (NDV)"], harness.Coordinator.Connected.Select(entry => entry.Name));
    }

    [Fact]
    public async Task ConnectingFillsThePopulationList()
    {
        using PopulationHarness harness = WithProjects("Testdatabase (NDV)");

        harness.Catalogue.Catalogue = [PopulationTestDoubles.NewPopulation(1, "Aktive pasientar")];

        PopulationTabViewModel tab = NewTab(harness);

        tab.SelectedProject = tab.Projects[0];

        await Settle(tab);
        await harness.WaitForCatalogueAsync();

        // AfterStudyChange -> ReadPopulationList; the picker listens to the session, not to the tab.
        Assert.Single(tab.Picker.Populations);
        Assert.True(tab.Picker.CanFilterFrequentlyUsed);
    }

    [Fact]
    public async Task ASuccessfulConnectRemembersTheDatabase()
    {
        using PopulationHarness harness = WithProjects("Testdatabase (NDV)");

        PopulationTabViewModel tab = NewTab(harness);

        tab.SelectedProject = tab.Projects[0];

        await Settle(tab);

        // Addition, decision (f) of 07-ui-contracts.md §5.
        Assert.Equal("Testdatabase (NDV)", harness.WindowState.GetLastDatabase());
    }

    [Fact]
    public async Task AFailedConnectIsShownAndNotRemembered()
    {
        using PopulationHarness harness = WithProjects("Testdatabase (NDV)");

        harness.Coordinator.Throws = new InvalidOperationException("Login failed for user 'x'.");

        PopulationTabViewModel tab = NewTab(harness);

        tab.SelectedProject = tab.Projects[0];

        await Settle(tab);

        Assert.Equal(["Login failed for user 'x'."], harness.Notifier.Errors);
        Assert.Null(harness.WindowState.GetLastDatabase());
    }

    [Fact]
    public async Task ClearingTheSelectionDisconnects()
    {
        // cbProject with ItemIndex = -1: fConnection := nil, after the disconnect
        // (MainQuickStat.pas:501-508). Unreachable from the drop-down, which offers no empty entry.
        using PopulationHarness harness = WithProjects("Testdatabase (NDV)");

        PopulationTabViewModel tab = NewTab(harness);

        tab.SelectedProject = tab.Projects[0];

        await Settle(tab);

        tab.SelectedProject = null;

        await Settle(tab);

        Assert.Equal(1, harness.Coordinator.DisconnectCount);
        Assert.Empty(tab.Picker.Populations);
    }

    [Fact]
    public async Task SwitchingProjectConnectsAgain()
    {
        using PopulationHarness harness = WithProjects("alpha", "Beta");

        PopulationTabViewModel tab = NewTab(harness);

        tab.SelectedProject = tab.Projects[0];

        await Settle(tab);

        tab.SelectedProject = tab.Projects[1];

        await Settle(tab);

        Assert.Equal(["alpha", "Beta"], harness.Coordinator.Connected.Select(entry => entry.Name));
        Assert.Equal("Beta", harness.WindowState.GetLastDatabase());
    }

    [Fact]
    public void ThePickerIsTheOneTheContainerSupplied()
    {
        using PopulationHarness harness = new();

        PopulationTabViewModel tab = NewTab(harness);

        Assert.Same(harness.Picker, tab.Picker);
    }

    [Fact]
    public void TheProjectComparerIsCaseInsensitiveAndNotOrdinal()
    {
        // AnsiCompareText, not AnsiCompareStr: ordinal would sort every lower-case name after every
        // upper-case one, which the Delphi combo box does not do.
        Assert.True(PopulationTabViewModel.ProjectOrder.Equals("Testdatabase", "TESTDATABASE"));
        Assert.True(PopulationTabViewModel.ProjectOrder.Compare("alpha", "Beta") < 0);
        Assert.True(StringComparer.Ordinal.Compare("alpha", "Beta") > 0);
    }
}
