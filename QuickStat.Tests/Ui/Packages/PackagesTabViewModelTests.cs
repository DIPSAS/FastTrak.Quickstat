using System.Globalization;
using System.Windows.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Diagnostics;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;
using QuickStat.Domain.Packages;
using QuickStat.Domain.Populations;
using QuickStat.Services;
using QuickStat.Tests.Ui.Services;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Ui.Packages;

/// <summary>
/// The Packages tab: the filter of PORT-PLAN.md §8.8 (i), the confirmed delete of
/// <c>05-ui-spec.md</c> §D.1, the replay of §B.3, and the save half of decision (l).
/// </summary>
/// <remarks>
/// Every case that folds case runs under a forced culture, and the filter cases run under
/// <c>en-US</c> <b>and</b> <c>tr-TR</c>: the filter uppercases with the current culture on purpose,
/// because Delphi's <c>AnsiUppercase</c> is locale-sensitive too, so the behaviour to pin is the
/// rule and not an English outcome.
/// </remarks>
public class PackagesTabViewModelTests
{
    /// <summary>Forces a culture for the duration of a case, and puts it back afterwards.</summary>
    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

        internal CultureScope(string name) => CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);

        public void Dispose() => CultureInfo.CurrentCulture = _previous;
    }

    /// <summary>Everything one case needs, wired the way the container wires it.</summary>
    private sealed class Harness : IDisposable
    {
        internal Harness(bool answerConfirmations = true, IUiDispatcher? uiDispatcher = null)
        {
            Matrix = ShellWorkspaceTests.NewMatrix();
            Workspace = new ShellWorkspace(Matrix);
            Progress = new ShellProgress(new InlineUiDispatcher());
            Session = new FakeSessionService();
            Repository = new FakePackageRepository();
            Patients = new FakePatientRepository();
            Audit = new FakePopulationRepository();
            Parameters = new FakeParameterResolver();
            Presenter = new ProbingNotificationPresenter(_ => OnNotify?.Invoke(), answerConfirmations);
            Populations = QuickStat.Tests.Ui.Populations.PopulationTestDoubles.NewPickerViewModel();

            IdentificationPolicy identification = new();

            Collections = QuickStat.Tests.Ui.Collections.CollectionsTabHarness.Headless(
                Workspace, identification, Progress);

            Dataset = new DatasetViewModel(
                Workspace,
                identification,
                new FakeDatasetExporter(),
                new FakeTempFileTracker(),
                new FakeFileDialogService(),
                new FakeProcessLauncher(),
                new UserNotifier(Presenter, NullLogger<UserNotifier>.Instance),
                Progress,
                NullLogger<DatasetViewModel>.Instance);

            ViewModel = new PackagesTabViewModel(
                Workspace,
                Progress,
                uiDispatcher ?? new InlineUiDispatcher(),
                Session,
                Repository,
                Patients,
                Audit,
                Parameters,
                new UserNotifier(Presenter, NullLogger<UserNotifier>.Instance),
                Populations,
                Collections,
                Dataset,
                NullLogger<PackagesTabViewModel>.Instance);

            // A session without the SessionChanged that announces it, so a case can load the list on
            // its own terms.  Connect() is the one that behaves like a login.
            Session.SetSilently(FakeSession.ForStudy());
        }

        internal PersonMatrix Matrix { get; }

        internal ShellWorkspace Workspace { get; }

        internal ShellProgress Progress { get; }

        internal FakeSessionService Session { get; }

        internal FakePackageRepository Repository { get; }

        internal FakePatientRepository Patients { get; }

        internal FakePopulationRepository Audit { get; }

        internal FakeParameterResolver Parameters { get; }

        internal ProbingNotificationPresenter Presenter { get; }

        /// <summary>Runs the moment a dialog would go up, so a case can look at the shell then.</summary>
        internal Action? OnNotify { get; set; }

        internal PopulationPickerViewModel Populations { get; }

        internal CollectionsTabViewModel Collections { get; }

        internal DatasetViewModel Dataset { get; }

        internal PackagesTabViewModel ViewModel { get; }

        /// <summary>Puts a study on the wire and announces it, which is what a login does.</summary>
        internal void Connect(int studyId = 124) => Session.Announce(FakeSession.ForStudy(studyId));

        /// <summary>Adds a population to the catalogue the replay searches.</summary>
        internal Population AddPopulation(int procId, string title = "Aktive pasienter")
        {
            Population population = ShellWorkspaceTests.NewPopulation(procId, title);

            Populations.Populations.Add(new PopulationViewModel(population));

            return population;
        }

        /// <summary>Adds a tickable data element to the Collections tab's list.</summary>
        internal DataElementViewModel AddElement(string name, string title)
        {
            DataElementViewModel element = new(name, title);

            Collections.DataElements.Add(element);

            return element;
        }

        /// <summary>Loads the list and selects the row with the given title.</summary>
        internal async Task SelectAsync(string title)
        {
            await ViewModel.ReloadAsync();

            ViewModel.SelectedPackage = ViewModel.Packages.Single(package => package.Title == title);
        }

        public void Dispose()
        {
            ViewModel.Dispose();
            Dataset.Dispose();
        }
    }

    /// <summary>
    /// A fully faked Packages tab, for tests that need one but do not test it.
    /// </summary>
    /// <param name="workspace">The shell's workspace, so the tabs share one matrix.</param>
    /// <param name="progress">The shell's progress service.</param>
    /// <param name="dataset">The Dataset tab, whose save request this tab subscribes to.</param>
    /// <returns>A view-model with a fake repository, session, patient source and resolver.</returns>
    /// <remarks>
    /// <c>Ui/Shell/MainViewModelTests.cs</c> composes all four tabs and therefore has to construct
    /// this one. It lives here so the fakes stay in step 3.4's own file.
    /// </remarks>
    internal static PackagesTabViewModel NewViewModel(
        IShellWorkspace workspace,
        IShellProgress progress,
        DatasetViewModel dataset)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        return new PackagesTabViewModel(
            workspace,
            progress,
            new InlineUiDispatcher(),
            new FakeSessionService(),
            new FakePackageRepository(),
            new FakePatientRepository(),
            new FakePopulationRepository(),
            new FakeParameterResolver(),
            new UserNotifier(new HeadlessNotificationPresenter(), NullLogger<UserNotifier>.Instance),
            QuickStat.Tests.Ui.Populations.PopulationTestDoubles.NewPickerViewModel(),
            QuickStat.Tests.Ui.Collections.CollectionsTabHarness.Headless(
                workspace, new IdentificationPolicy(), progress),
            dataset,
            NullLogger<PackagesTabViewModel>.Instance);
    }

    private static PackagedSelection NewPackage(
        int rowId,
        string title,
        string comment = "",
        int populationId = 257,
        params string[] collectorNames) => new()
        {
            RowId = rowId,
            StudyId = 124,
            PopulationId = populationId,
            Title = title,
            Comment = comment,
            CollectorNames = collectorNames,
        };

    private static IReadOnlyList<string> VisibleTitles(PackagesTabViewModel viewModel) =>
        [.. viewModel.PackagesView.Cast<PackageViewModel>().Select(package => package.Title)];

    // ---------------------------------------------------------------------------------------
    //  Loading the list - Delphi LoadPackagedSelections, called from AfterLogin.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void TheListIsEmptyUntilSomethingLoadsIt()
    {
        using Harness harness = new();

        Assert.Empty(harness.ViewModel.Packages);
    }

    [Fact]
    public async Task WithNoSessionTheListIsEmptied()
    {
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(41, "Alfa"));

        await harness.ViewModel.ReloadAsync();

        harness.Session.SetSilently(null);

        await harness.ViewModel.ReloadAsync();

        Assert.Empty(harness.ViewModel.Packages);
    }

    [Fact]
    public void ConnectingLoadsThePackagesForThatStudy()
    {
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(41, "Diabetes basissett 2024"));
        harness.Repository.Stored.Add(NewPackage(42, "Antropometri"));

        harness.Connect(931);

        Assert.Equal([931], harness.Repository.LoadedStudyIds);
        Assert.Equal(["Diabetes basissett 2024", "Antropometri"], harness.ViewModel.Packages.Select(p => p.Title));
    }

    [Fact]
    public void DisconnectingEmptiesTheList()
    {
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(41, "Diabetes basissett 2024"));
        harness.Connect();

        harness.Session.Announce(null);

        Assert.Empty(harness.ViewModel.Packages);
    }

    [Fact]
    public void TheListKeepsServerOrder()
    {
        // §G.5 sorts the collector list and the project drop-down and says nothing about this one;
        // Report.QuickStat's order is what LoadPackagedSelections walks.
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(9, "Zulu"));
        harness.Repository.Stored.Add(NewPackage(3, "Alfa"));
        harness.Repository.Stored.Add(NewPackage(7, "Mike"));

        harness.Connect();

        Assert.Equal(["Zulu", "Alfa", "Mike"], harness.ViewModel.Packages.Select(p => p.Title));
    }

    // ---------------------------------------------------------------------------------------
    //  The filter.  Uppercase both sides with the current culture, trimmed, ordinal.
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    [InlineData("nb-NO")]
    public async Task AnEmptyFilterMatchesEverything(string culture)
    {
        using CultureScope scope = new(culture);
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(41, "Alfa"));
        harness.Repository.Stored.Add(NewPackage(42, "Bravo"));

        await harness.ViewModel.ReloadAsync();

        Assert.Equal(["Alfa", "Bravo"], VisibleTitles(harness.ViewModel));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    public async Task TheFilterIsTrimmed(string culture)
    {
        // The other list filter is not: PORT-PLAN.md §8.8 (i) records the two as deliberately
        // different, and this is the one that trims.
        using CultureScope scope = new(culture);
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(41, "Alfa"));
        harness.Repository.Stored.Add(NewPackage(42, "Bravo"));

        await harness.ViewModel.ReloadAsync();

        harness.ViewModel.FilterText = "   ";

        Assert.Equal(["Alfa", "Bravo"], VisibleTitles(harness.ViewModel));

        harness.ViewModel.FilterText = "  alfa  ";

        Assert.Equal(["Alfa"], VisibleTitles(harness.ViewModel));
    }

    [Theory]
    [InlineData("41")]
    [InlineData("basissett")]
    [InlineData("legemidler")]
    [InlineData("Pop#257")]
    public async Task TheFilterMatchesEveryFieldOfTheRow(string needle)
    {
        // RowId ⇥ Title ⇥ Comment ⇥ Pop#<n> - TPackagedSelection.AsListBox, QuickStat.Selection.pas:147.
        using CultureScope scope = new("en-US");
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(41, "Diabetes basissett 2024", "Med legemidler", 257));
        harness.Repository.Stored.Add(NewPackage(99, "Noe annet", "Uten", 8));

        await harness.ViewModel.ReloadAsync();

        harness.ViewModel.FilterText = needle;

        Assert.Equal(["Diabetes basissett 2024"], VisibleTitles(harness.ViewModel));
    }

    [Fact]
    public async Task TheFilterFoldsCaseWithTheCurrentCultureOnBothSides()
    {
        // Under tr-TR, ToUpper("i") is "İ" and ToUpper("I") is "I".  A lower-case needle therefore
        // still matches, because both sides fold the same way - which is the rule, and the same rule
        // AnsiUppercase gave the Delphi.
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(41, "Diabetes"));

        await harness.ViewModel.ReloadAsync();

        foreach (string culture in (string[])["en-US", "tr-TR", "nb-NO"])
        {
            using CultureScope scope = new(culture);

            harness.ViewModel.FilterText = "";
            harness.ViewModel.FilterText = "di";

            Assert.Equal(["Diabetes"], VisibleTitles(harness.ViewModel));
        }
    }

    [Fact]
    public async Task AnAlreadyUppercaseNeedleFollowsTheCultureToo()
    {
        // The other half of the same rule, and the reason the assertion above is not an English-only
        // outcome: "DI" is already upper case, so tr-TR leaves it as "DI" while the title becomes
        // "DİABETES" - and they no longer match.  Ordinal, locale-sensitive folding: exactly what
        // AnsiUppercase + Pos did.
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(41, "Diabetes"));

        await harness.ViewModel.ReloadAsync();

        using (CultureScope english = new("en-US"))
        {
            harness.ViewModel.FilterText = "DI";

            Assert.Equal(["Diabetes"], VisibleTitles(harness.ViewModel));
        }

        using (CultureScope turkish = new("tr-TR"))
        {
            harness.ViewModel.FilterText = "";
            harness.ViewModel.FilterText = "DI";

            Assert.Empty(VisibleTitles(harness.ViewModel));
        }
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    [InlineData("nb-NO")]
    public async Task TheComparisonIsOrdinalAndNotACollation(string culture)
    {
        // A soft hyphen is ignorable in a linguistic comparison, so CurrentCultureIgnoreCase would
        // match here and Delphi's Pos would not.  This is the assertion that keeps somebody from
        // "simplifying" the filter into a collation.
        using CultureScope scope = new(culture);
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(41, "Diabetes­basissett"));

        await harness.ViewModel.ReloadAsync();

        harness.ViewModel.FilterText = "diabetesbasissett";

        Assert.Empty(VisibleTitles(harness.ViewModel));

        harness.ViewModel.FilterText = "diabetes­basissett";

        Assert.Equal(["Diabetes­basissett"], VisibleTitles(harness.ViewModel));
    }

    [Fact]
    public async Task FilteringDoesNotTouchTheModel()
    {
        using CultureScope scope = new("en-US");
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(41, "Alfa"));
        harness.Repository.Stored.Add(NewPackage(42, "Bravo"));

        await harness.ViewModel.ReloadAsync();

        harness.ViewModel.FilterText = "alfa";

        Assert.Equal(2, harness.ViewModel.Packages.Count);
        Assert.Single(VisibleTitles(harness.ViewModel));
    }

    // ---------------------------------------------------------------------------------------
    //  Delete - §D.1, §D.4.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task DeleteIsDisabledUntilSomethingIsSelected()
    {
        // An improvement over the Delphi, which enables actDeletePackage always and warns
        // "You need to select a package for this operation." at execute time (§D.1).
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(41, "Alfa"));

        Assert.False(harness.ViewModel.DeletePackageCommand.CanExecute(null));

        await harness.SelectAsync("Alfa");

        Assert.True(harness.ViewModel.DeletePackageCommand.CanExecute(null));
    }

    [Fact]
    public async Task DeletingAsksFirstAndDoesNothingOnNo()
    {
        using Harness harness = new(answerConfirmations: false);

        harness.Repository.Stored.Add(NewPackage(41, "Alfa"));

        await harness.SelectAsync("Alfa");
        await harness.ViewModel.DeletePackageCommand.ExecuteAsync(null);

        Assert.Empty(harness.Repository.Deleted);
        Assert.Single(harness.ViewModel.Packages);
    }

    [Fact]
    public async Task TheDeleteQuestionIsTheDelphisWithRealLineBreaks()
    {
        using Harness harness = new(answerConfirmations: false);

        harness.Repository.Stored.Add(NewPackage(41, "Diabetes basissett 2024"));

        await harness.SelectAsync("Diabetes basissett 2024");
        await harness.ViewModel.DeletePackageCommand.ExecuteAsync(null);

        UserNotification question = Assert.Single(harness.Presenter.Notifications);

        Assert.Equal(
            "Do you really want to delete this package:\n\"Diabetes basissett 2024\"?",
            question.Message);
        Assert.DoesNotContain("\\n", question.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeletingRemovesTheRowFromTheModelAndNotJustTheView()
    {
        // The Delphi calls lbPackagedGrids.Items.Delete(ItemIndex) and leaves the object in
        // fPackagedQuickStatGrids (MainQuickStat.pas:897), so the very next filter keystroke repaints
        // the deleted package back into the list.  Fixed.
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(41, "Alfa"));
        harness.Repository.Stored.Add(NewPackage(42, "Bravo"));

        await harness.SelectAsync("Alfa");
        await harness.ViewModel.DeletePackageCommand.ExecuteAsync(null);

        Assert.Equal([41], harness.Repository.Deleted);
        Assert.Equal(["Bravo"], harness.ViewModel.Packages.Select(p => p.Title));

        harness.ViewModel.FilterText = "a";

        Assert.Equal(["Bravo"], VisibleTitles(harness.ViewModel));
    }

    [Fact]
    public async Task DeletingClearsTheSelection()
    {
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(41, "Alfa"));

        await harness.SelectAsync("Alfa");
        await harness.ViewModel.DeletePackageCommand.ExecuteAsync(null);

        Assert.Null(harness.ViewModel.SelectedPackage);
        Assert.False(harness.ViewModel.DeletePackageCommand.CanExecute(null));
    }

    [Fact]
    public async Task AFailedDeleteIsReportedAndLeavesTheRow()
    {
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(41, "Alfa"));

        await harness.SelectAsync("Alfa");

        harness.Repository.Throws = new InvalidOperationException("The server said no.");

        await harness.ViewModel.DeletePackageCommand.ExecuteAsync(null);

        Assert.Single(harness.ViewModel.Packages);
        Assert.True(harness.Progress.IsError);
        Assert.Contains(
            harness.Presenter.Notifications,
            notification => notification.Severity == NotificationSeverity.Error);
    }

    // ---------------------------------------------------------------------------------------
    //  The replay - §B.3, MainQuickStat.pas:772-814.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task OpenIsDisabledUntilSomethingIsSelected()
    {
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(41, "Alfa"));

        Assert.False(harness.ViewModel.OpenPackageCommand.CanExecute(null));

        await harness.SelectAsync("Alfa");

        Assert.True(harness.ViewModel.OpenPackageCommand.CanExecute(null));
    }

    [Fact]
    public async Task ReplayingLoadsThePopulationTicksTheElementsAndSetsTheCaption()
    {
        using Harness harness = new();

        harness.Connect();
        harness.AddPopulation(257, "Aktive diabetikere");
        harness.Patients.Cohort.Add(ShellWorkspaceTests.NewPatient(52));

        DataElementViewModel wanted = harness.AddElement("QS_HBA1C", "Labdata: HbA1c (siste)");
        DataElementViewModel unwanted = harness.AddElement("QS_BMI", "Antropometri");

        unwanted.IsChecked = true;

        harness.Repository.Stored.Add(NewPackage(41, "Diabetes basissett 2024", "", 257, "QS_HBA1C"));

        await harness.SelectAsync("Diabetes basissett 2024");
        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        Assert.Equal(257, Assert.Single(harness.Patients.Loaded).ProcId);
        Assert.Equal(257, harness.Workspace.Population?.ProcId);
        Assert.Equal(1, harness.Workspace.RowCount);
        Assert.True(wanted.IsChecked);
        Assert.False(unwanted.IsChecked);
        Assert.Equal("Diabetes basissett 2024", harness.Dataset.CaptionText);
    }

    [Fact]
    public async Task AReplayCountsTowardsThePopularityRanking()
    {
        // The Delphi writes dbo.AddPopulationLog from PopulationRequested, which the replay reaches
        // through TrySelect (EPR.VclFrame.Populations.pas:219).  Skipping it here would quietly
        // change what the "Frequently used only" box offers, because the server ranks from these
        // rows.
        using Harness harness = new();

        harness.Connect(931);
        harness.AddPopulation(257, "Aktive diabetikere");
        harness.Patients.Cohort.Add(ShellWorkspaceTests.NewPatient(52));
        harness.Repository.Stored.Add(NewPackage(41, "Alfa", "", 257));

        await harness.SelectAsync("Alfa");
        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        Assert.Equal((931, 257, "Aktive diabetikere"), Assert.Single(harness.Audit.AuditRows));
    }

    [Fact]
    public async Task ARefusedReplayWritesNoAuditRow()
    {
        using Harness harness = new();

        harness.Connect();
        harness.AddPopulation(8);
        harness.Repository.Stored.Add(NewPackage(41, "Alfa", "", 257));

        await harness.SelectAsync("Alfa");
        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        Assert.Empty(harness.Audit.AuditRows);
    }

    [Fact]
    public async Task ReplayingSwitchesToTheCollectionsTabExactlyOnce()
    {
        // The reverse of what this test used to assert.  07-ui-contracts.md §3.1 claimed the replay
        // stayed on the Packages tab; the call chain says otherwise and has been checked against the
        // source: PreparePackagedSelection calls TrySelect(procId, ALoadIt := true, ...)
        // (MainQuickStat.pas:789) -> PopulationRequested (EPR.VclFrame.Populations.pas:195) -> every
        // observer's AfterPopulationSelect (:217-218) -> pgSelections.ActivePage := tbsDataElements
        // (MainQuickStat.pas:541).  Once, not twice, even though the Delphi loads the cohort twice.
        using Harness harness = new();

        int requests = 0;

        harness.Workspace.CollectionsTabRequested += (_, _) => requests++;

        harness.Connect();
        harness.AddPopulation(257);
        harness.Patients.Cohort.Add(ShellWorkspaceTests.NewPatient(52));
        harness.Repository.Stored.Add(NewPackage(41, "Alfa", "", 257));

        await harness.SelectAsync("Alfa");
        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        Assert.Equal(1, requests);
    }

    [Fact]
    public async Task AReplayThatCannotLoadThePopulationDoesNotSwitchTabs()
    {
        // The switch happens inside AfterPopulationSelect, which TrySelect only reaches once the
        // population has been found and loaded.  A replay that gives up earlier leaves the user
        // looking at the package list, which is where the problem is.
        using Harness harness = new();

        int requests = 0;

        harness.Workspace.CollectionsTabRequested += (_, _) => requests++;

        harness.Connect();
        harness.Repository.Stored.Add(NewPackage(41, "Alfa", "", 257));

        await harness.SelectAsync("Alfa");
        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        Assert.Equal(0, requests);
        Assert.NotEmpty(harness.Presenter.Notifications);
    }

    [Fact]
    public async Task ReplayingRunsTheCollectionsTabsCollectCommand()
    {
        using Harness harness = new();

        harness.Connect();
        harness.AddPopulation(257);
        harness.Patients.Cohort.Add(ShellWorkspaceTests.NewPatient(52));
        harness.Repository.Stored.Add(NewPackage(41, "Alfa", "", 257));

        await harness.SelectAsync("Alfa");

        // The stub command step 3.1 left behind is permanently disabled; the replay must invoke it
        // anyway, because the Delphi calls actCollectDataExecute directly rather than through the
        // action's enabled state (MainQuickStat.pas:806).
        Assert.False(harness.Collections.CollectDataCommand.CanExecute(null));

        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        Assert.Equal("Alfa", harness.Dataset.CaptionText);
    }

    [Fact]
    public async Task AnUnknownPopulationWarnsAndStops()
    {
        using Harness harness = new();

        harness.Connect();
        harness.AddPopulation(8);
        harness.Repository.Stored.Add(NewPackage(41, "Alfa", "", 257));

        await harness.SelectAsync("Alfa");
        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        UserNotification warning = Assert.Single(harness.Presenter.Notifications);

        Assert.Equal(NotificationSeverity.Warning, warning.Severity);
        Assert.Equal(
            "The selection is based on an unknown population (ProcId=257).\n"
            + "The data collection can not be performed at this time.\n"
            + "Perhaps the population is from a different protocol?",
            warning.Message);
        Assert.Empty(harness.Patients.Loaded);
        Assert.Null(harness.Workspace.Population);
        Assert.False(harness.Progress.IsBusy);
    }

    [Fact]
    public async Task ARefusedReplayNeverMarksTheShellBusy()
    {
        // The warning is a modal box; the busy overlay behind it would be noise.  The Delphi has no
        // wait cursor at this point either - TrySelect gives up before PopulationRequested runs, so
        // nothing has assigned crSqlWait.
        using Harness harness = new();

        List<bool> busyWhenShown = [];

        harness.OnNotify = () => busyWhenShown.Add(harness.Progress.IsBusy);

        harness.Connect();
        harness.Repository.Stored.Add(NewPackage(41, "Alfa", "", 257));

        await harness.SelectAsync("Alfa");
        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        Assert.False(harness.Progress.IsBusy);
        Assert.Equal([false], busyWhenShown);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("nb-NO")]
    public async Task TheUnknownPopulationWarningNeverGroupsTheProcId(string culture)
    {
        // Delphi %d applies no digit grouping.  A Norwegian machine must not print "1 234".
        using CultureScope scope = new(culture);
        using Harness harness = new();

        harness.Connect();
        harness.Repository.Stored.Add(NewPackage(41, "Alfa", "", 1234));

        await harness.SelectAsync("Alfa");
        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        Assert.Contains(
            "ProcId=1234",
            Assert.Single(harness.Presenter.Notifications).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnUnknownCollectorWarnsAndTheRunContinues()
    {
        using Harness harness = new();

        harness.Connect();
        harness.AddPopulation(257);
        harness.Patients.Cohort.Add(ShellWorkspaceTests.NewPatient(52));

        DataElementViewModel known = harness.AddElement("QS_HBA1C", "Labdata: HbA1c (siste)");

        harness.Repository.Stored.Add(NewPackage(41, "Alfa", "", 257, "QS_HBA1C", "QS_GONE"));

        await harness.SelectAsync("Alfa");
        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        UserNotification warning = Assert.Single(harness.Presenter.Notifications);

        Assert.Equal(
            "The selection contains an unknown data element.\n"
            + "Element name was \"QS_GONE\".\n"
            + "The data collection will be incomplete.\n"
            + "Perhaps the selection was created in a later version?",
            warning.Message);
        Assert.True(known.IsChecked);
        Assert.Equal("Alfa", harness.Dataset.CaptionText);
    }

    [Fact]
    public async Task AStoredNameAlsoMatchesACollectorTitle()
    {
        // TryFindCollector accepts Name or Title, case-insensitively (MainQuickStat.pas:725).  That
        // leniency is what lets a package written before a rename still open.
        using Harness harness = new();

        harness.Connect();
        harness.AddPopulation(257);
        harness.Patients.Cohort.Add(ShellWorkspaceTests.NewPatient(52));

        DataElementViewModel element = harness.AddElement("QS_BMI", "Antropometri");

        harness.Repository.Stored.Add(NewPackage(41, "Alfa", "", 257, "antropometri"));

        await harness.SelectAsync("Alfa");
        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        Assert.True(element.IsChecked);
        Assert.Empty(harness.Presenter.Notifications);
    }

    [Fact]
    public async Task ASecondReplayWorksAfterTheMatrixHasBeenLocked()
    {
        // The regression this exists for: PersonMatrix.SortBy throws once the matrix is locked, and a
        // collect run locks it.  The Delphi's LoadPopulationIntoGrid opens with ClearPopulation,
        // which clears fLocked (EPR.QA.Matrix.pas:211-215) - and the three-line ordering contract in
        // 07-ui-contracts.md §3.1 leaves that call out.
        using Harness harness = new();

        harness.Connect();
        harness.AddPopulation(257);
        harness.Patients.Cohort.Add(ShellWorkspaceTests.NewPatient(52));
        harness.Repository.Stored.Add(NewPackage(41, "Alfa", "", 257));

        await harness.SelectAsync("Alfa");
        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        // What step 3.3's collect run ends with.
        harness.Matrix.Lock();
        harness.Workspace.NotifyDataChanged();

        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        Assert.Equal(2, harness.Patients.Loaded.Count);
        Assert.Empty(harness.Presenter.Notifications);

        // Locked, because this harness now drives step 3.3's real collect run rather than the
        // no-op stub this test was written against, and a run ends with Lock() so the dataset is
        // exportable.  The point of the test is unchanged and is the line above plus this one: the
        // second replay got all the way through instead of throwing out of AddColumns.
        Assert.True(harness.Matrix.IsLocked);
    }

    [Fact]
    public async Task ACancelledPeriodPromptAbortsTheReplayQuietly()
    {
        using Harness harness = new();

        harness.Connect();
        harness.AddPopulation(257);
        harness.Repository.Stored.Add(NewPackage(41, "Alfa", "", 257));
        harness.Parameters.Answer = new ParameterResolution { Succeeded = false, CancelledByUser = true };

        await harness.SelectAsync("Alfa");
        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        Assert.Empty(harness.Patients.Loaded);
        Assert.Empty(harness.Presenter.Notifications);
        Assert.False(harness.Progress.IsError);
    }

    [Fact]
    public async Task AnUnresolvablePlaceholderShowsUpOnTheStatusLine()
    {
        using Harness harness = new();

        harness.Connect();
        harness.AddPopulation(257);
        harness.Repository.Stored.Add(NewPackage(41, "Alfa", "", 257));
        harness.Parameters.Answer = new ParameterResolution
        {
            Succeeded = false,
            FailureReason = "Unknown placeholder :Sykehus.",
        };

        await harness.SelectAsync("Alfa");
        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        Assert.True(harness.Progress.IsError);
        Assert.Equal("Unknown placeholder :Sykehus.", harness.Progress.Info);
        Assert.Empty(harness.Patients.Loaded);
    }

    [Fact]
    public async Task TheReplayIsBusyThroughoutAndIdleAfterwards()
    {
        using Harness harness = new();

        harness.Connect();
        harness.AddPopulation(257);
        harness.Patients.Cohort.Add(ShellWorkspaceTests.NewPatient(52));
        harness.Repository.Stored.Add(NewPackage(41, "Alfa", "", 257));

        await harness.SelectAsync("Alfa");

        bool busyDuringLoad = false;

        harness.Workspace.PopulationChanged += (_, _) => busyDuringLoad = harness.Progress.IsBusy;

        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        Assert.True(busyDuringLoad);
        Assert.False(harness.Progress.IsBusy);
    }

    [Fact]
    public async Task ReplayingWithoutASessionSaysSoRatherThanDoingNothing()
    {
        using Harness harness = new();

        harness.Repository.Stored.Add(NewPackage(41, "Alfa", "", 257));

        await harness.SelectAsync("Alfa");

        harness.Session.SetSilently(null);

        await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

        Assert.Equal(
            PackagesTabViewModel.NotConnectedMessage,
            Assert.Single(harness.Presenter.Notifications).Message);
    }

    // ---------------------------------------------------------------------------------------
    //  Saving - decision (l), Delphi actSaveDataPackageExecute.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void TheDatasetTabsSaveRequestArrivesHere()
    {
        using Harness harness = new();

        harness.Connect();
        harness.Workspace.SetCheckedCollectorNames(["QS_HBA1C"]);
        harness.Matrix.PreparePopulation([ShellWorkspaceTests.NewPatient(52)]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation(257));

        int requests = 0;

        harness.ViewModel.SaveSpecRequested += (_, _) => requests++;

        harness.Dataset.SaveDataPackageCommand.Execute(null);

        Assert.Equal(1, requests);
    }

    [Fact]
    public void TheDialogIsOnlyEverGivenTheSaveSpecificationHeader()
    {
        // Decision (e): "Save selection" belongs to actSavePatientSelection, which is not ported.
        using Harness harness = new();

        harness.Connect();
        harness.Workspace.SetCheckedCollectorNames(["QS_HBA1C"]);
        harness.Matrix.PreparePopulation([ShellWorkspaceTests.NewPatient(52)]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation(257));

        string? header = null;

        harness.ViewModel.SaveSpecRequested += (_, request) => header = request.Header;

        harness.Dataset.SaveDataPackageCommand.Execute(null);

        Assert.Equal("Save specification", header);
    }

    [Fact]
    public void CancellingTheDialogSavesNothing()
    {
        using Harness harness = new();

        harness.Connect();
        harness.Workspace.SetCheckedCollectorNames(["QS_HBA1C"]);
        harness.Matrix.PreparePopulation([ShellWorkspaceTests.NewPatient(52)]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation(257));

        harness.ViewModel.SaveSpecRequested += (_, request) => request.Accepted = false;

        harness.Dataset.SaveDataPackageCommand.Execute(null);

        Assert.Empty(harness.Repository.Saved);
    }

    [Fact]
    public void WithNoSubscriberAtAllTheSaveIsTreatedAsCancelled()
    {
        using Harness harness = new();

        harness.Connect();
        harness.Workspace.SetCheckedCollectorNames(["QS_HBA1C"]);
        harness.Matrix.PreparePopulation([ShellWorkspaceTests.NewPatient(52)]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation(257));

        harness.Dataset.SaveDataPackageCommand.Execute(null);

        Assert.Empty(harness.Repository.Saved);
    }

    [Fact]
    public void AcceptingTheDialogStoresTheStudyThePopulationAndTheTickedNames()
    {
        using Harness harness = new();

        harness.Connect(931);
        harness.Workspace.SetCheckedCollectorNames(["QS_HBA1C", "QS_BMI"]);
        harness.Matrix.PreparePopulation([ShellWorkspaceTests.NewPatient(52)]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation(257));

        harness.ViewModel.SaveSpecRequested += (_, request) =>
        {
            request.Accepted = true;
            request.Title = "Diabetes basissett 2024";
            request.Comment = "Med legemidler";
        };

        harness.Dataset.SaveDataPackageCommand.Execute(null);

        PackagedSelection saved = Assert.Single(harness.Repository.Saved);

        Assert.Equal(931, saved.StudyId);
        Assert.Equal(257, saved.PopulationId);
        Assert.Equal("Diabetes basissett 2024", saved.Title);
        Assert.Equal("Med legemidler", saved.Comment);
        Assert.Equal(["QS_HBA1C", "QS_BMI"], saved.CollectorNames);
    }

    [Fact]
    public void SavingRefreshesTheList()
    {
        using Harness harness = new();

        harness.Connect();
        harness.Workspace.SetCheckedCollectorNames(["QS_HBA1C"]);
        harness.Matrix.PreparePopulation([ShellWorkspaceTests.NewPatient(52)]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation(257));

        harness.ViewModel.SaveSpecRequested += (_, request) =>
        {
            request.Accepted = true;
            request.Title = "Nytt sett";
        };

        harness.Dataset.SaveDataPackageCommand.Execute(null);

        PackageViewModel row = Assert.Single(harness.ViewModel.Packages);

        Assert.Equal("Nytt sett", row.Title);
        Assert.NotEqual(0, row.RowId);
    }

    [Fact]
    public void TheShellIsNotMarkedBusyWhileTheModalIsUp()
    {
        // IsBusy drives the wait cursor and the busy overlay, and the dialog is a window the user is
        // typing into.  Marking the shell busy around ShowDialog would put both on top of it.
        using Harness harness = new();

        harness.Connect();
        harness.Workspace.SetCheckedCollectorNames(["QS_HBA1C"]);
        harness.Matrix.PreparePopulation([ShellWorkspaceTests.NewPatient(52)]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation(257));

        bool busyWhileAsking = true;

        harness.ViewModel.SaveSpecRequested += (_, request) =>
        {
            busyWhileAsking = harness.Progress.IsBusy;

            request.Accepted = true;
            request.Title = "Nytt sett";
        };

        harness.Dataset.SaveDataPackageCommand.Execute(null);

        Assert.False(busyWhileAsking);
        Assert.False(harness.Progress.IsBusy);
    }

    [Fact]
    public void AFailingDialogIsReportedRatherThanLostAsAnUnobservedTask()
    {
        // The save runs as fire-and-forget off a synchronous command, so anything that escapes it
        // reaches App.Report as a crash box rather than the status line.
        using Harness harness = new();

        harness.Connect();
        harness.Workspace.SetCheckedCollectorNames(["QS_HBA1C"]);
        harness.Matrix.PreparePopulation([ShellWorkspaceTests.NewPatient(52)]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation(257));

        harness.ViewModel.SaveSpecRequested += (_, _) => throw new InvalidOperationException("No window.");

        harness.Dataset.SaveDataPackageCommand.Execute(null);

        Assert.True(harness.Progress.IsError);
        Assert.Contains(
            harness.Presenter.Notifications,
            notification => notification.Severity == NotificationSeverity.Error);
    }

    [Fact]
    public void AFailedSaveIsReported()
    {
        using Harness harness = new();

        harness.Connect();
        harness.Workspace.SetCheckedCollectorNames(["QS_HBA1C"]);
        harness.Matrix.PreparePopulation([ShellWorkspaceTests.NewPatient(52)]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation(257));
        harness.Repository.Throws = new InvalidOperationException("The server said no.");

        harness.ViewModel.SaveSpecRequested += (_, request) =>
        {
            request.Accepted = true;
            request.Title = "Nytt sett";
        };

        harness.Dataset.SaveDataPackageCommand.Execute(null);

        Assert.True(harness.Progress.IsError);
        Assert.Empty(harness.ViewModel.Packages);
    }

    [Fact]
    public void SavingWithNoPopulationDoesNothing()
    {
        // Delphi Guard.CheckNotNull(fGridPopulation) would have crashed; DatasetViewModel's
        // CanExecute greys the menu item, and this is the belt to that pair of braces.
        using Harness harness = new();

        harness.Connect();

        bool asked = false;

        harness.ViewModel.SaveSpecRequested += (_, _) => asked = true;

        harness.Dataset.SaveDataPackageCommand.Execute(null);

        Assert.False(asked);
        Assert.Empty(harness.Repository.Saved);
    }

    // ---------------------------------------------------------------------------------------
    //  Housekeeping.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void DisposingUnsubscribesFromBothEvents()
    {
        using Harness harness = new();

        harness.Connect();
        harness.Workspace.SetCheckedCollectorNames(["QS_HBA1C"]);
        harness.Matrix.PreparePopulation([ShellWorkspaceTests.NewPatient(52)]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation(257));

        harness.ViewModel.Dispose();

        bool asked = false;

        harness.ViewModel.SaveSpecRequested += (_, _) => asked = true;

        harness.Dataset.SaveDataPackageCommand.Execute(null);
        harness.Repository.Stored.Add(NewPackage(41, "Alfa"));
        harness.Session.Announce(FakeSession.ForStudy());

        Assert.False(asked);
        Assert.Empty(harness.ViewModel.Packages);
    }

    [Fact]
    public void TheHeaderIsPackagedDatasetsAndNotPackages()
    {
        Assert.Equal("Packaged datasets", PackagesTabViewModel.PackagesHeader);
        Assert.Equal("Delete this package", PackagesTabViewModel.DeletePackageCaption);
    }

    // ---------------------------------------------------------------------------------------
    //  Under the real thing: an STA thread with a running dispatcher.
    // ---------------------------------------------------------------------------------------

    [Fact]
    public void TheFilteredViewWorksUnderARunningDispatcher()
    {
        // The cases above run on the plain MTA test thread, where the ListCollectionView happens to
        // behave.  Shipping configuration is an STA thread with a pumped dispatcher, and a collection
        // view is affine to the one that created it - so the whole load-filter-select cycle is worth
        // one pass through the real arrangement.
        StaTestRunner.RunWithDispatcher(async () =>
        {
            using CultureScope scope = new("en-US");
            using Harness harness = new();

            harness.Repository.Stored.Add(NewPackage(41, "Diabetes basissett 2024"));
            harness.Repository.Stored.Add(NewPackage(42, "Antropometri"));

            await harness.ViewModel.ReloadAsync();

            Assert.Equal(2, VisibleTitles(harness.ViewModel).Count);

            harness.ViewModel.FilterText = "antro";

            Assert.Equal(["Antropometri"], VisibleTitles(harness.ViewModel));

            harness.ViewModel.SelectedPackage = harness.ViewModel.Packages[1];

            Assert.True(harness.ViewModel.DeletePackageCommand.CanExecute(null));

            await harness.ViewModel.DeletePackageCommand.ExecuteAsync(null);

            Assert.Equal(["Diabetes basissett 2024"], harness.ViewModel.Packages.Select(p => p.Title));
        });
    }

    [Fact]
    public void ALoginAnnouncedOffTheUiThreadStillFillsTheList()
    {
        // ISessionService.SessionChanged is raised wherever the login pipeline happens to be, and
        // Packages is bound - so the handler has to marshal.  With a real dispatcher in place, a
        // direct call from the thread pool would throw rather than merely race.
        StaTestRunner.RunWithDispatcher(async () =>
        {
            using Harness harness = new(uiDispatcher: new WpfUiDispatcher(Dispatcher.CurrentDispatcher));

            harness.Repository.Stored.Add(NewPackage(41, "Diabetes basissett 2024"));

            await Task.Run(() => harness.Session.Announce(FakeSession.ForStudy(931)));

            // Let the posted reload run: BeginInvoke queues at Normal, this resumes below it.
            await Dispatcher.Yield(DispatcherPriority.Background);

            Assert.Equal([931], harness.Repository.LoadedStudyIds);
            Assert.Equal(["Diabetes basissett 2024"], harness.ViewModel.Packages.Select(p => p.Title));
        });
    }

    [Fact]
    public void TheReplayWorksUnderARunningDispatcher()
    {
        StaTestRunner.RunWithDispatcher(async () =>
        {
            using Harness harness = new();

            harness.Connect();
            harness.AddPopulation(257, "Aktive diabetikere");
            harness.Patients.Cohort.Add(ShellWorkspaceTests.NewPatient(52));

            DataElementViewModel element = harness.AddElement("QS_HBA1C", "Labdata: HbA1c (siste)");

            harness.Repository.Stored.Add(NewPackage(41, "Diabetes basissett 2024", "", 257, "QS_HBA1C"));

            await harness.SelectAsync("Diabetes basissett 2024");
            await harness.ViewModel.OpenPackageCommand.ExecuteAsync(null);

            Assert.True(element.IsChecked);
            Assert.Equal(257, harness.Workspace.Population?.ProcId);
            Assert.Equal("Diabetes basissett 2024", harness.Dataset.CaptionText);
            Assert.False(harness.Progress.IsBusy);
        });
    }
}
