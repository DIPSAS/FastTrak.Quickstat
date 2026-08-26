using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Domain.Matrix;
using QuickStat.Domain.Populations;
using QuickStat.Tests.Ui.Services;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Ui.Populations;

/// <summary>
/// The embedded population picker: catalogue load, the live filter, row expansion, the SQL preview
/// and the prepare sequence. <c>05-ui-spec.md</c> §B.1.1, PORT-PLAN.md §8.8 (i).
/// </summary>
public class PopulationPickerViewModelTests
{
    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

        internal CultureScope(string name) => CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);

        public void Dispose() => CultureInfo.CurrentCulture = _previous;
    }

    private static List<PopulationViewModel> Visible(PopulationPickerViewModel picker) =>
        [.. picker.PopulationsView.Cast<PopulationViewModel>()];

    private static List<int> VisibleIds(PopulationPickerViewModel picker) =>
        [.. Visible(picker).Select(row => row.ProcId)];

    // ---------------------------------------------------------------------------------------
    // Catalogue
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void ItStartsEmptyAndDisabled()
    {
        using PopulationHarness harness = new();

        Assert.Empty(harness.Picker.Populations);
        Assert.False(harness.Picker.CanFilterFrequentlyUsed);
        Assert.False(harness.Picker.FrequentlyUsedOnly);
        Assert.False(harness.Picker.Simplified);
        Assert.False(harness.Picker.IsSqlPreviewVisible);
        Assert.Equal("", harness.Picker.SqlPreview);
        Assert.Null(harness.Picker.SelectedPopulation);
        Assert.False(harness.Picker.PreparePopulationCommand.CanExecute(null));
        Assert.Empty(harness.Catalogue.Requests);
    }

    [Fact]
    public async Task ASessionFillsTheListInServerOrder()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(9, "Siste"),
            PopulationTestDoubles.NewPopulation(1, "Fyrste"),
            PopulationTestDoubles.NewPopulation(5, "Midt i"));

        // 05-ui-spec.md §G.5: populations are NOT sorted by the client.
        Assert.Equal([9, 1, 5], VisibleIds(harness.Picker));
    }

    [Fact]
    public async Task ASessionEnablesFrequentlyUsedOnlyWhenTheStudyIsReal()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync();

        // cbShowCommon.Enabled := Sender.StudyId > 0 (EPR.VclFrame.Populations.pas:146).
        Assert.True(harness.Picker.CanFilterFrequentlyUsed);
    }

    [Fact]
    public async Task AStudyOfZeroLeavesFrequentlyUsedOnlyDisabled()
    {
        using PopulationHarness harness = new();

        harness.Session.Change(PopulationTestDoubles.NewSession(studyId: 0));

        await harness.WaitForCatalogueAsync();

        Assert.False(harness.Picker.CanFilterFrequentlyUsed);
    }

    [Fact]
    public async Task TheCatalogueIsQueriedWithTheStudyAndTheDatabaseVersion()
    {
        using PopulationHarness harness = new();

        harness.Session.Change(PopulationTestDoubles.NewSession(studyId: 12, dbVersion: 510));

        await harness.WaitForCatalogueAsync();

        Assert.Equal([(12, 510, false)], harness.Catalogue.Requests);
    }

    [Fact]
    public async Task DisconnectingEmptiesTheList()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Ein"));

        Assert.Single(harness.Picker.Populations);

        harness.Session.Change(null);

        await harness.WaitForCatalogueAsync();

        Assert.Empty(harness.Picker.Populations);
        Assert.False(harness.Picker.CanFilterFrequentlyUsed);
    }

    [Fact]
    public async Task FrequentlyUsedOnlyRequeriesTheServerRatherThanFilteringLocally()
    {
        using PopulationHarness harness = new();

        harness.Catalogue.FrequentlyUsed = [PopulationTestDoubles.NewPopulation(3, "Populær")];

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(1, "Ein"),
            PopulationTestDoubles.NewPopulation(2, "To"));

        harness.Picker.FrequentlyUsedOnly = true;

        await harness.WaitForCatalogueAsync();

        // Populations.GetPopularPopulations, not a client-side predicate (§B.1.1 item 3, §G.6).
        Assert.Equal(2, harness.Catalogue.Requests.Count);
        Assert.True(harness.Catalogue.Requests[1].FrequentlyUsedOnly);
        Assert.Equal([3], VisibleIds(harness.Picker));
    }

    [Fact]
    public async Task ReloadingClearsTheSelection()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Ein"));

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];

        harness.Picker.FrequentlyUsedOnly = true;

        await harness.WaitForCatalogueAsync();

        // fCurrentPopulation := nil at the top of ReadPopulationList.
        Assert.Null(harness.Picker.SelectedPopulation);
    }

    [Fact]
    public async Task AFailedCatalogueQueryEmptiesTheListAndSaysSo()
    {
        using PopulationHarness harness = new();

        harness.Catalogue.Throws = new InvalidOperationException("The server said no.");

        harness.Session.Change(PopulationTestDoubles.NewSession());

        await harness.WaitForCatalogueAsync();

        Assert.Empty(harness.Picker.Populations);
        Assert.Equal(["The server said no."], harness.Notifier.Errors);
        Assert.True(harness.Progress.IsError);
        Assert.Equal("The server said no.", harness.Progress.Info);
    }

    // ---------------------------------------------------------------------------------------
    // The filter - PORT-PLAN.md §8.8 (i)
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task AnEmptyFilterMatchesEverything()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(1, "Ein"),
            PopulationTestDoubles.NewPopulation(2, "To"));

        harness.Picker.FilterText = "";

        Assert.Equal([1, 2], VisibleIds(harness.Picker));
        Assert.False(harness.Picker.IsEmpty);
    }

    [Fact]
    public async Task TheFilterIsCaseInsensitiveOverTheWholeSearchText()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(11, "Aktive brukarar", group: "Prosess"),
            PopulationTestDoubles.NewPopulation(12, "Avslutta", helpText: "Utmeldte brukarar"),
            PopulationTestDoubles.NewPopulation(13, "Noko heilt anna", group: "Studier"));

        // No dotted or dotless I anywhere in the filter: which of them a fold produces is the
        // subject of TheCaseFoldIsTheCurrentCultureAndNotTheInvariantOne, not of this test.
        harness.Picker.FilterText = "BRUKARAR";

        // Matched against ProcId ⇥ Title ⇥ HelpText ⇥ Group, so both the title and the help text hit.
        Assert.Equal([11, 12], VisibleIds(harness.Picker));
    }

    [Fact]
    public async Task TypingANumberFiltersTheIdBySubstring()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(26, "Ein"),
            PopulationTestDoubles.NewPopulation(126, "To"),
            PopulationTestDoubles.NewPopulation(260, "Tre"),
            PopulationTestDoubles.NewPopulation(7, "Fire"));

        harness.Picker.FilterText = "26";

        Assert.Equal([26, 126, 260], VisibleIds(harness.Picker));
    }

    [Fact]
    public async Task TheGroupIsSearchable()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(1, "Ein", group: "Type 1 u/pumpe"),
            PopulationTestDoubles.NewPopulation(2, "To", group: "Behandling"));

        harness.Picker.FilterText = "pumpe";

        Assert.Equal([1], VisibleIds(harness.Picker));
    }

    [Fact]
    public async Task TheFilterIsNotTrimmed()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Aktive"));

        harness.Picker.FilterText = " aktive";

        // The population list does not trim; the packages list does (PORT-PLAN.md §8.8 (i)). The
        // separator before the title is a tab, so a leading space cannot match.
        Assert.Empty(VisibleIds(harness.Picker));

        harness.Picker.FilterText = "aktive";

        Assert.Equal([1], VisibleIds(harness.Picker));
    }

    [Fact]
    public async Task TheComparisonIsOrdinalAndNotACollation()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Straße"));

        // "straße".Contains("strasse", StringComparison.CurrentCultureIgnoreCase) is TRUE - the
        // collation folds ß to ss - and Delphi's Pos is not a collation. Ordinal keeps them apart.
        harness.Picker.FilterText = "STRASSE";

        Assert.Empty(VisibleIds(harness.Picker));

        harness.Picker.FilterText = "STRASSE"[..4];

        Assert.Equal([1], VisibleIds(harness.Picker));
    }

    [Fact]
    public async Task TheCaseFoldIsTheCurrentCultureAndNotTheInvariantOne()
    {
        // AnsiLowercase is locale-sensitive (PORT-PLAN.md §8.8 (i)); Turkish is where that shows.
        // "I".ToLower("tr-TR") is the dotless "ı", so an upper-case I no longer finds a dotted i.
        using CultureScope culture = new("tr-TR");
        using PopulationHarness harness = new();

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(1, "IKA"),
            PopulationTestDoubles.NewPopulation(2, "ika"));

        harness.Picker.FilterText = "I";

        Assert.Equal([1], VisibleIds(harness.Picker));

        harness.Picker.FilterText = "i";

        Assert.Equal([2], VisibleIds(harness.Picker));
    }

    [Fact]
    public async Task TheSameFilterMatchesBothCasesInEnglish()
    {
        using CultureScope culture = new("en-US");
        using PopulationHarness harness = new();

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(1, "IKA"),
            PopulationTestDoubles.NewPopulation(2, "ika"));

        harness.Picker.FilterText = "I";

        Assert.Equal([1, 2], VisibleIds(harness.Picker));
    }

    [Fact]
    public async Task TheFilterDoesNotChangeTheUnderlyingCollection()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(1, "Ein"),
            PopulationTestDoubles.NewPopulation(2, "To"));

        harness.Picker.FilterText = "ein";

        Assert.Single(Visible(harness.Picker));
        Assert.Equal(2, harness.Picker.Populations.Count);
    }

    // ---------------------------------------------------------------------------------------
    // Empty state
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void WithNoDatabaseTheEmptyStateAsksForOne()
    {
        using PopulationHarness harness = new();

        Assert.True(harness.Picker.IsEmpty);
        Assert.Equal(PopulationPickerViewModel.NoDatabaseText, harness.Picker.EmptyStateText);
    }

    [Fact]
    public async Task WithADatabaseThatHasNoPopulationsTheEmptyStateSaysThatInstead()
    {
        using PopulationHarness harness = new();
        List<string?> changed = [];

        harness.Picker.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        await harness.ConnectAsync();

        Assert.True(harness.Picker.IsEmpty);
        Assert.Equal(PopulationPickerViewModel.NoPopulationsText, harness.Picker.EmptyStateText);

        // The collection was empty before and after, so only the session change can announce this.
        Assert.Contains(nameof(PopulationPickerViewModel.EmptyStateText), changed);
    }

    [Fact]
    public async Task WithAFilterThatExcludesEverythingTheEmptyStateSaysSo()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Ein"));

        harness.Picker.FilterText = "finst ikkje";

        Assert.True(harness.Picker.IsEmpty);
        Assert.Equal(PopulationPickerViewModel.NoMatchesText, harness.Picker.EmptyStateText);
    }

    [Fact]
    public async Task TheEmptyStateGoesAwayWhenTheListFillsUp()
    {
        using PopulationHarness harness = new();
        List<string?> changed = [];

        harness.Picker.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Ein"));

        Assert.False(harness.Picker.IsEmpty);
        Assert.Contains(nameof(PopulationPickerViewModel.IsEmpty), changed);
    }

    // ---------------------------------------------------------------------------------------
    // Expansion - Emetra.VclComp.ListView.pas:752-755
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task WithSimplifiedOffEveryRowIsExpanded()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(1, "Ein", helpText: "Fyrste"),
            PopulationTestDoubles.NewPopulation(2, "To", helpText: "Andre"));

        Assert.All(harness.Picker.Populations, row => Assert.True(row.IsExpanded));
    }

    [Fact]
    public async Task WithSimplifiedOnOnlyTheSelectedRowIsExpanded()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(1, "Ein", helpText: "Fyrste"),
            PopulationTestDoubles.NewPopulation(2, "To", helpText: "Andre"));

        harness.Picker.Simplified = true;

        Assert.All(harness.Picker.Populations, row => Assert.False(row.IsExpanded));

        harness.Picker.SelectedPopulation = harness.Picker.Populations[1];

        Assert.False(harness.Picker.Populations[0].IsExpanded);
        Assert.True(harness.Picker.Populations[1].IsExpanded);

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];

        Assert.True(harness.Picker.Populations[0].IsExpanded);
        Assert.False(harness.Picker.Populations[1].IsExpanded);
    }

    [Fact]
    public async Task TurningSimplifiedOffAgainExpandsEverything()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(1, "Ein"),
            PopulationTestDoubles.NewPopulation(2, "To"));

        harness.Picker.Simplified = true;
        harness.Picker.Simplified = false;

        Assert.All(harness.Picker.Populations, row => Assert.True(row.IsExpanded));
    }

    [Fact]
    public async Task ExpansionDoesNotChangeWhatTheFilterMatches()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(1, "Ein", helpText: "Hemoglobin"),
            PopulationTestDoubles.NewPopulation(2, "To"));

        harness.Picker.FilterText = "hemoglobin";
        harness.Picker.Simplified = true;

        // "The Simplified checkbox does not change what is matched" (§B.1.1).
        Assert.Equal([1], VisibleIds(harness.Picker));
    }

    // ---------------------------------------------------------------------------------------
    // SQL preview - §B.1.1 item 7, §I.9
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task SelectingARowFillsThePreviewEvenWhileItIsHidden()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Ein", sourceCode: "SELECT 1"));

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];

        // PopulationSelected fills memSourceCode regardless; only the pane is gated.
        Assert.Equal("SELECT 1", harness.Picker.SqlPreview);
        Assert.False(harness.Picker.IsSqlPreviewVisible);
    }

    [Fact]
    public async Task ThePreviewNormalisesEveryLineBreakToCrLf()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(1, "Ein", sourceCode: "CREATE PROC\nAS\r\nSELECT 1\n"));

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];

        Assert.Equal("CREATE PROC\r\nAS\r\nSELECT 1\r\n", harness.Picker.SqlPreview);
    }

    [Fact]
    public async Task ClearingTheSelectionBlanksThePreview()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Ein", sourceCode: "SELECT 1"));

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];
        harness.Picker.SelectedPopulation = null;

        Assert.Equal("", harness.Picker.SqlPreview);
    }

    [Fact]
    public async Task GrantingTheSourceCodeRightShowsThePaneAndBlanksIt()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Ein", sourceCode: "SELECT 1"));

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];

        harness.Picker.SetSourceCodeAccess(true);

        // AfterAccessControlChanged clears the memo and then sets the pane's visibility.
        Assert.True(harness.Picker.IsSqlPreviewVisible);
        Assert.Equal("", harness.Picker.SqlPreview);

        harness.Picker.SetSourceCodeAccess(false);

        Assert.False(harness.Picker.IsSqlPreviewVisible);
    }

    // ---------------------------------------------------------------------------------------
    // Prepare - the ordering contract
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task PreparingIsDisabledUntilSomethingIsSelected()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Ein"));

        Assert.False(harness.Picker.PreparePopulationCommand.CanExecute(null));

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];

        Assert.True(harness.Picker.PreparePopulationCommand.CanExecute(null));
    }

    [Fact]
    public async Task PreparingLoadsTheCohortAndPublishesItInTheRightOrder()
    {
        using PopulationHarness harness = new();

        Population population = PopulationTestDoubles.NewPopulation(257, "HbA1c > 53 (7%)");

        harness.Patients.Cohort = [.. Enumerable.Range(1, 3).Select(ShellWorkspaceTests.NewPatient)];

        await harness.ConnectAsync(population);

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];

        // PersonId is the enum's zero, so prove the assignment happens by starting somewhere else.
        harness.Matrix.SortBy = MatrixSortOrder.ReverseName;

        bool rowsWereReadyWhenPopulationChanged = false;
        bool populationWasSetWhenTheTabWasRequested = false;

        harness.Workspace.PopulationChanged += (_, _) =>
            rowsWereReadyWhenPopulationChanged = harness.Workspace is { HasPopulation: true, RowCount: 3 };

        harness.Workspace.CollectionsTabRequested += (_, _) =>
            populationWasSetWhenTheTabWasRequested = ReferenceEquals(harness.Workspace.Population, population);

        await harness.Picker.PreparePopulationCommand.ExecuteAsync(null);

        Assert.Same(population, harness.Workspace.Population);
        Assert.Equal(3, harness.Workspace.RowCount);
        Assert.True(rowsWereReadyWhenPopulationChanged);
        Assert.True(populationWasSetWhenTheTabWasRequested);
        Assert.Equal(MatrixSortOrder.PersonId, harness.Matrix.SortBy);
    }

    [Fact]
    public async Task PreparingASecondPopulationAfterACollectRunDoesNotThrow()
    {
        // PersonMatrix.SortBy throws once the matrix is locked, and a collect run locks it. The
        // Delphi survives this because LoadPopulationIntoGrid calls ClearPopulation first
        // (MainQuickStat.pas:564), which unlocks; the port has to do the same.
        using PopulationHarness harness = new();

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1)];

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(1, "Ein"),
            PopulationTestDoubles.NewPopulation(2, "To"));

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];

        await harness.Picker.PreparePopulationCommand.ExecuteAsync(null);

        harness.Matrix.Lock();

        harness.Picker.SelectedPopulation = harness.Picker.Populations[1];

        await harness.Picker.PreparePopulationCommand.ExecuteAsync(null);

        Assert.Equal(2, harness.Workspace.Population?.ProcId);
        Assert.False(harness.Matrix.IsLocked);
        Assert.Empty(harness.Notifier.Errors);
    }

    [Fact]
    public async Task PreparingClearsTheColumnsOfThePreviousRun()
    {
        using PopulationHarness harness = new();

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1)];

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Ein"));

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];

        await harness.Picker.PreparePopulationCommand.ExecuteAsync(null);

        VariableNameSet names = harness.Matrix.CreateVariableNameSet();

        names.Add("HBA1C");
        harness.Matrix.AddColumns(names);

        Assert.True(harness.Matrix.HasData);

        await harness.Picker.PreparePopulationCommand.ExecuteAsync(null);

        Assert.False(harness.Matrix.HasData);
    }

    [Fact]
    public async Task AnEmptyCohortLeavesHasPopulationFalse()
    {
        using PopulationHarness harness = new();

        harness.Patients.Cohort = [];

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Ein"));

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];

        await harness.Picker.PreparePopulationCommand.ExecuteAsync(null);

        // tbsDataElements.TabVisible := fGrid.Data.DataRows > 0 (MainQuickStat.pas:568).
        Assert.NotNull(harness.Workspace.Population);
        Assert.False(harness.Workspace.HasPopulation);
    }

    [Fact]
    public async Task PreparingWritesTheSelectionAudit()
    {
        using PopulationHarness harness = new();

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1)];

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(257, "HbA1c"));

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];

        await harness.Picker.PreparePopulationCommand.ExecuteAsync(null);

        (int studyId, int procId, string title, long elapsed) = Assert.Single(harness.Catalogue.AuditRows);

        Assert.Equal(7, studyId);
        Assert.Equal(257, procId);
        Assert.Equal("HbA1c", title);
        Assert.True(elapsed >= 0);
    }

    [Fact]
    public async Task PreparingPassesTheResolvedParametersToTheQuery()
    {
        using PopulationHarness harness = new();

        harness.Parameters.Answer = new ParameterResolution
        {
            Succeeded = true,
            Values = new Dictionary<string, object?> { ["StudyId"] = 7, ["StartDate"] = new DateTime(2024, 1, 1) },
        };

        Population population = PopulationTestDoubles.NewPopulation(1, "Ein");

        await harness.ConnectAsync(population);

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];

        await harness.Picker.PreparePopulationCommand.ExecuteAsync(null);

        Assert.Equal([population.QueryText], harness.Parameters.Resolved);
        Assert.Equal([population], harness.Patients.Loaded);
        Assert.NotNull(harness.Patients.LastParameters);
        Assert.Equal(2, harness.Patients.LastParameters.Count);
    }

    [Fact]
    public async Task CancellingThePeriodDialogAbandonsTheLoadWithoutTouchingTheGrid()
    {
        // PORT-PLAN.md §7.2: the Delphi cleared the grid before it asked, so a cancel left the
        // previous cohort on screen under the new population's title.
        using PopulationHarness harness = new();

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1), ShellWorkspaceTests.NewPatient(2)];

        Population first = PopulationTestDoubles.NewPopulation(1, "Ein");

        await harness.ConnectAsync(first, PopulationTestDoubles.NewPopulation(2, "To"));

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];

        await harness.Picker.PreparePopulationCommand.ExecuteAsync(null);

        harness.Parameters.Answer = new ParameterResolution { Succeeded = false, CancelledByUser = true };
        harness.Picker.SelectedPopulation = harness.Picker.Populations[1];

        await harness.Picker.PreparePopulationCommand.ExecuteAsync(null);

        Assert.Same(first, harness.Workspace.Population);
        Assert.Equal(2, harness.Workspace.RowCount);
        Assert.Empty(harness.Notifier.Errors);
        Assert.False(harness.Progress.IsError);
        Assert.Single(harness.Patients.Loaded);
    }

    [Fact]
    public async Task AnUnresolvablePlaceholderIsNamed()
    {
        using PopulationHarness harness = new();

        harness.Parameters.Answer = new ParameterResolution
        {
            Succeeded = false,
            FailureReason = "Unknown placeholder ':Klinikk'.",
        };

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Ein"));

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];

        await harness.Picker.PreparePopulationCommand.ExecuteAsync(null);

        Assert.Equal(["Unknown placeholder ':Klinikk'."], harness.Notifier.Errors);
        Assert.True(harness.Progress.IsError);
        Assert.Empty(harness.Patients.Loaded);
    }

    [Fact]
    public async Task AFailedPopulationQueryIsReportedRatherThanSwallowed()
    {
        // Deliberate change: EPR.VclFrame.Populations.pas:220-226 logs a SilentWarning, so a broken
        // population looks exactly like an empty one. PORT-PLAN.md §7.2 asks it to fail loudly.
        using PopulationHarness harness = new();

        harness.Patients.Throws = new InvalidOperationException("Invalid column name 'FullName'.");

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Ein"));

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];

        await harness.Picker.PreparePopulationCommand.ExecuteAsync(null);

        Assert.Equal(["Invalid column name 'FullName'."], harness.Notifier.Errors);
        Assert.True(harness.Progress.IsError);
        Assert.Null(harness.Workspace.Population);
    }

    [Fact]
    public async Task PreparingRaisesTheBusyFlagAndDropsItAgain()
    {
        using PopulationHarness harness = new();

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1)];

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Aktive pasientar"));

        harness.Picker.SelectedPopulation = harness.Picker.Populations[0];

        bool busyDuringTheLoad = false;

        harness.Workspace.PopulationChanged += (_, _) => busyDuringTheLoad = harness.Progress.IsBusy;

        await harness.Picker.PreparePopulationCommand.ExecuteAsync(null);

        // §G.3: the crSqlWait cursor becomes an IsBusy flag, and it must not latch.
        Assert.True(busyDuringTheLoad);
        Assert.False(harness.Progress.IsBusy);

        // The status line carries the population's own title, as a collect run carries the
        // collector's (§G.6). The Delphi sets nothing here and calls no Done().
        Assert.Equal("Aktive pasientar", harness.Progress.Info);
    }

    // ---------------------------------------------------------------------------------------
    // TryLoadPopulationAsync - the package replay's entry point (step 3.4)
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task LoadingByIdSelectsTheRowAndFillsTheGrid()
    {
        using PopulationHarness harness = new();

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1), ShellWorkspaceTests.NewPatient(2)];

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(1, "Ein"),
            PopulationTestDoubles.NewPopulation(2, "To"));

        Assert.True(await harness.Picker.TryLoadPopulationAsync(2));

        Assert.Equal(2, harness.Picker.SelectedPopulation?.ProcId);
        Assert.Equal(2, harness.Workspace.Population?.ProcId);
        Assert.Equal(2, harness.Workspace.RowCount);
    }

    [Fact]
    public async Task LoadingByIdFindsAPopulationThatTheFilterIsHiding()
    {
        using PopulationHarness harness = new();

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1)];

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(1, "Ein"),
            PopulationTestDoubles.NewPopulation(2, "To"));

        harness.Picker.FilterText = "ein";

        Assert.Equal([1], VisibleIds(harness.Picker));

        // The Delphi asks the list VIEW, so a replay with a filter typed reports the population as
        // unknown. Deliberate change, flagged on TryLoadPopulationAsync.
        Assert.True(await harness.Picker.TryLoadPopulationAsync(2));

        Assert.Equal(2, harness.Workspace.Population?.ProcId);
    }

    [Fact]
    public async Task LoadingByIdDoesNotAskForTheCollectionsTab()
    {
        using PopulationHarness harness = new();

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1)];

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Ein"));

        int requests = 0;

        harness.Workspace.CollectionsTabRequested += (_, _) => requests++;

        Assert.True(await harness.Picker.TryLoadPopulationAsync(1));

        // The replay leaves the user on the Packages tab (07-ui-contracts.md §3.1).
        Assert.Equal(0, requests);
    }

    [Fact]
    public async Task LoadingByIdAnswersFalseForAnUnknownId()
    {
        using PopulationHarness harness = new();

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Ein"));

        Assert.False(await harness.Picker.TryLoadPopulationAsync(999));

        Assert.Null(harness.Picker.SelectedPopulation);
        Assert.Empty(harness.Patients.Loaded);
    }

    [Fact]
    public async Task ReplayingASecondPackageAfterACollectRunDoesNotThrow()
    {
        // The same locked-matrix trap as PreparingASecondPopulationAfterACollectRunDoesNotThrow, on
        // the other entry point: replay a package, collect, replay another.
        using PopulationHarness harness = new();

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1)];

        await harness.ConnectAsync(
            PopulationTestDoubles.NewPopulation(1, "Ein"),
            PopulationTestDoubles.NewPopulation(2, "To"));

        Assert.True(await harness.Picker.TryLoadPopulationAsync(1));

        harness.Matrix.Lock();

        Assert.True(await harness.Picker.TryLoadPopulationAsync(2));

        Assert.Equal(2, harness.Workspace.Population?.ProcId);
        Assert.False(harness.Matrix.IsLocked);
        Assert.Empty(harness.Notifier.Errors);
    }

    [Fact]
    public async Task LoadingByIdIsAwaitableSoAReplayCanCollectAfterwards()
    {
        // The reason this is a Task and not a command: PackagesTabViewModel has to know the cohort
        // is in the grid before it starts the collect run.
        using PopulationHarness harness = new();

        harness.Patients.Cohort = [ShellWorkspaceTests.NewPatient(1)];

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Ein"));

        Task<bool> load = harness.Picker.TryLoadPopulationAsync(1);

        Assert.True(await load);
        Assert.True(harness.Workspace.HasPopulation);
    }

    // ---------------------------------------------------------------------------------------
    // Lifetime
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task DisposingStopsListeningToTheSession()
    {
        PopulationHarness harness = new();

        await harness.ConnectAsync(PopulationTestDoubles.NewPopulation(1, "Ein"));

        harness.Dispose();

        int before = harness.Catalogue.Requests.Count;

        harness.Session.Change(PopulationTestDoubles.NewSession(studyId: 99));

        await harness.WaitForCatalogueAsync();

        Assert.Equal(before, harness.Catalogue.Requests.Count);
    }

    [Fact]
    public void DisposingTwiceIsHarmless()
    {
        PopulationHarness harness = new();

        harness.Dispose();
        harness.Dispose();
    }

    [Fact]
    public void EveryCollaboratorIsRequired()
    {
        using PopulationHarness harness = new();

        Assert.Throws<ArgumentNullException>(() => new PopulationPickerViewModel(
            null!,
            harness.Patients,
            harness.Parameters,
            harness.Session,
            harness.Workspace,
            harness.Progress,
            new InlineUiDispatcher(),
            harness.Notifier,
            NullLogger<PopulationPickerViewModel>.Instance));

        Assert.Throws<ArgumentNullException>(() => new PopulationPickerViewModel(
            harness.Catalogue,
            harness.Patients,
            harness.Parameters,
            harness.Session,
            harness.Workspace,
            harness.Progress,
            new InlineUiDispatcher(),
            harness.Notifier,
            null!));
    }
}
