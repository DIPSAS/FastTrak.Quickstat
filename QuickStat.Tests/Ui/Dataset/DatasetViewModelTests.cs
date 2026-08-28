using System.Globalization;
using System.Windows;
using QuickStat.Diagnostics;
using QuickStat.Domain.Anonymisation;
using QuickStat.Export;
using QuickStat.Tests.Ui.Services;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Ui.Dataset;

/// <summary>
/// The Dataset tab: the caption, the command enable rules of <c>05-ui-spec.md</c> §D.1, the export
/// paths, and the floating hint of §G.2.
/// </summary>
/// <remarks>
/// Every case that renders text runs under a forced <c>en-US</c>. This machine is <c>nb-NO</c>, so a
/// hint assertion written without one would pass here and fail on a build agent - the same rule
/// Phase 2 swept the whole suite for.
/// </remarks>
public class DatasetViewModelTests
{
    /// <summary>Forces a culture for the duration of a case, and puts it back afterwards.</summary>
    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

        internal CultureScope(string name) => CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);

        public void Dispose() => CultureInfo.CurrentCulture = _previous;
    }

    [Fact]
    public void TheCaptionStartsAsYourDataset()
    {
        using DatasetHarness harness = new();

        Assert.Equal("Your dataset", harness.ViewModel.CaptionText);
    }

    [Fact]
    public void TheCaptionIsRowsByColumns()
    {
        // rsGridInfo, verbatim.  Note the order: %d x %d is DataRows x FieldCount, so screenshot 3's
        // "17 x 20" is 17 patients over 20 fields, not the other way round.
        using CultureScope culture = new("en-US");
        using DatasetHarness harness = new();

        harness.LoadAndCollect();

        Assert.Equal(
            "Population: 1 \"Aktive pasienter\". Grid size: 1 x 1",
            harness.ViewModel.CaptionText);
    }

    [Fact]
    public void SetCaptionOverridesIt()
    {
        // The package replay ends with hdrPopulationName.Caption := packagedSelection.Title.
        using DatasetHarness harness = new();

        harness.LoadAndCollect();
        harness.ViewModel.SetCaption("Diabetes basissett 2024");

        Assert.Equal("Diabetes basissett 2024", harness.ViewModel.CaptionText);
    }

    [Fact]
    public void WideColumnsSwitchesBetweenSixtyFourAndOneHundredAndTwenty()
    {
        using DatasetHarness harness = new();

        Assert.Equal(64, harness.ViewModel.DataColumnWidth);

        harness.ViewModel.WideColumns = true;

        Assert.Equal(120, harness.ViewModel.DataColumnWidth);
    }

    [Fact]
    public void OpenInExcelIsDisabledUntilAColuectRunProducesData()
    {
        using DatasetHarness harness = new();

        Assert.False(harness.ViewModel.OpenInExcelCommand.CanExecute(null));

        harness.LoadAndCollect();

        Assert.True(harness.ViewModel.OpenInExcelCommand.CanExecute(null));
    }

    [Fact]
    public void OpenInExcelIsNeverDisabledAgain()
    {
        // §D.1: actExportData.Enabled is set inside actCollectDataExecute and never reset to false.
        // Loading a new population empties the matrix, and in the Delphi the menu item stays live.
        using DatasetHarness harness = new();

        harness.LoadAndCollect();
        Assert.True(harness.ViewModel.OpenInExcelCommand.CanExecute(null));

        harness.Matrix.PreparePopulation([ShellWorkspaceTests.NewPatient(99)]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation(2, "Andre"));
        harness.Workspace.NotifyDataChanged();

        Assert.False(harness.Workspace.HasData);
        Assert.True(harness.ViewModel.OpenInExcelCommand.CanExecute(null));
    }

    [Fact]
    public void SaveDatasetToCsvIsAlwaysEnabled()
    {
        // actSaveDataset has Enabled unset in the .dfm and nothing ever changes it.
        using DatasetHarness harness = new();

        Assert.True(harness.ViewModel.SaveDatasetToCsvCommand.CanExecute(null));
    }

    [Fact]
    public void SaveDataPackageNeedsBothATickedElementAndAPopulation()
    {
        // ValidateCollectorSelection enables it with actCollectData, and the Delphi additionally
        // asserts Guard.CheckNotNull(fGridPopulation) at execute time - which would have crashed.
        using DatasetHarness harness = new();

        Assert.False(harness.ViewModel.SaveDataPackageCommand.CanExecute(null));

        harness.Workspace.SetCheckedCollectorNames(["QS_AGE"]);
        Assert.False(harness.ViewModel.SaveDataPackageCommand.CanExecute(null));

        harness.Matrix.PreparePopulation([ShellWorkspaceTests.NewPatient(1)]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation());

        Assert.True(harness.ViewModel.SaveDataPackageCommand.CanExecute(null));
    }

    [Fact]
    public void UntickingEverythingDisablesSaveDataPackageAgain()
    {
        using DatasetHarness harness = new();

        harness.LoadAndCollect();
        harness.Workspace.SetCheckedCollectorNames(["QS_AGE"]);
        Assert.True(harness.ViewModel.SaveDataPackageCommand.CanExecute(null));

        harness.Workspace.SetCheckedCollectorNames([]);

        Assert.False(harness.ViewModel.SaveDataPackageCommand.CanExecute(null));
    }

    [Fact]
    public void SaveDataPackageRaisesTheSeamStepThreeFourSubscribesTo()
    {
        using DatasetHarness harness = new();
        int requests = 0;

        harness.ViewModel.SaveDataPackageRequested += (_, _) => requests++;

        harness.LoadAndCollect();
        harness.Workspace.SetCheckedCollectorNames(["QS_AGE"]);

        harness.ViewModel.SaveDataPackageCommand.Execute(null);

        Assert.Equal(1, requests);
    }

    [Fact]
    public async Task OpenInExcelWritesATrackedTemporaryCsvAndHandsItToTheShell()
    {
        using DatasetHarness harness = new();

        harness.LoadAndCollect();

        await harness.ViewModel.OpenInExcelCommand.ExecuteAsync(null);

        string written = Assert.Single(harness.Exporter.Paths);

        Assert.EndsWith(".csv", written, StringComparison.Ordinal);
        Assert.Contains(written, harness.TempFiles.TrackedPaths);
        Assert.Equal([written], harness.Launcher.Opened);
    }

    [Fact]
    public async Task OpenInExcelTracksTheKeyFileToo()
    {
        // PORT-PLAN.md §7.2: the Delphi wrote the re-identification key next to every anonymised
        // export and never deleted it, so plaintext keys accumulate in %TEMP%.
        using DatasetHarness harness = new();

        harness.Exporter.KeyFilePath = "C:\\Temp\\dataset.mapping.txt";
        harness.LoadAndCollect();

        await harness.ViewModel.OpenInExcelCommand.ExecuteAsync(null);

        Assert.Contains("C:\\Temp\\dataset.mapping.txt", harness.TempFiles.TrackedPaths);
    }

    [Fact]
    public async Task AFailedExportDeletesThePartialFileAndTellsTheUser()
    {
        using DatasetHarness harness = new();

        harness.Exporter.Throws = new InvalidOperationException("disk full");
        harness.LoadAndCollect();

        await harness.ViewModel.OpenInExcelCommand.ExecuteAsync(null);

        Assert.Single(harness.TempFiles.Deleted);
        Assert.Empty(harness.Launcher.Opened);
        Assert.True(harness.Progress.IsError);
        Assert.Contains(harness.Presenter.Notifications, n => n.Severity == NotificationSeverity.Error);
    }

    [Fact]
    public async Task ExportingWithNothingCollectedExplainsRatherThanWritingAFile()
    {
        // An improvement, flagged: the Delphi writes the literal "(not ready)" into every cell of an
        // unlocked matrix, and a phantom "nil" row for an empty population.  Both are reachable,
        // because Save-to-CSV is always enabled.
        using DatasetHarness harness = new();

        await harness.ViewModel.SaveDatasetToCsvCommand.ExecuteAsync(null);

        Assert.Empty(harness.Exporter.Paths);
        Assert.Equal(0, harness.FileDialogs.ShowCount);
        Assert.Contains(harness.Presenter.Notifications, n => n.Message == DatasetViewModel.NothingToExportMessage);
    }

    [Fact]
    public async Task ExportingAnUnlockedMatrixIsRefusedToo()
    {
        using DatasetHarness harness = new();

        harness.Matrix.PreparePopulation([ShellWorkspaceTests.NewPatient(1)]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation());
        ShellWorkspaceTests.AddColumn(harness.Matrix, "AGE", 1, 64);
        harness.Workspace.NotifyDataChanged();

        Assert.True(harness.Workspace.HasData);
        Assert.False(harness.Matrix.IsLocked);

        await harness.ViewModel.SaveDatasetToCsvCommand.ExecuteAsync(null);

        Assert.Empty(harness.Exporter.Paths);
    }

    [Fact]
    public async Task SaveDatasetToCsvUsesTheDelphiDialogSettings()
    {
        using DatasetHarness harness = new();

        harness.FileDialogs.Answer = "C:\\Temp\\out.csv";
        harness.LoadAndCollect();

        await harness.ViewModel.SaveDatasetToCsvCommand.ExecuteAsync(null);

        Assert.NotNull(harness.FileDialogs.LastRequest);
        Assert.Equal("QuickStat.csv", harness.FileDialogs.LastRequest.FileName);
        Assert.Equal("csv", harness.FileDialogs.LastRequest.DefaultExtension);
        Assert.Equal("Comma separated values|*.csv", harness.FileDialogs.LastRequest.Filter);
        Assert.True(harness.FileDialogs.LastRequest.OverwritePrompt);
        Assert.Equal(["C:\\Temp\\out.csv"], harness.Exporter.Paths);
    }

    [Fact]
    public async Task CancellingTheSaveDialogWritesNothing()
    {
        using DatasetHarness harness = new();

        harness.FileDialogs.Answer = null;
        harness.LoadAndCollect();

        await harness.ViewModel.SaveDatasetToCsvCommand.ExecuteAsync(null);

        Assert.Empty(harness.Exporter.Paths);
    }

    [Fact]
    public async Task ExportOptionsComeFromTheSharedIdentificationPolicyAndTheWorkspaceFlag()
    {
        // PORT-PLAN.md §7.2: display anonymity and export anonymity are one value, not two paths
        // that happen to agree.
        using DatasetHarness harness = new();

        harness.Identification.Mode = PersonIdentification.RandomPersonId;
        harness.Workspace.ExportTimestamps = true;
        harness.FileDialogs.Answer = "C:\\Temp\\out.csv";
        harness.LoadAndCollect();

        await harness.ViewModel.SaveDatasetToCsvCommand.ExecuteAsync(null);

        Assert.NotNull(harness.Exporter.LastOptions);
        Assert.Equal(PersonIdentification.RandomPersonId, harness.Exporter.LastOptions.Identification);
        Assert.True(harness.Exporter.LastOptions.IncludeTimestamps);
        Assert.Equal(ExportFormat.Csv, harness.Exporter.LastOptions.Format);
        Assert.Equal(CsvDialect.Legacy, harness.Exporter.LastOptions.Dialect);
    }

    [Fact]
    public void ChangingTheIdentificationModeReachesTheGrid()
    {
        using DatasetHarness harness = new();
        int refreshes = 0;

        harness.ViewModel.GridRefreshRequested += (_, _) => refreshes++;

        harness.Identification.Mode = PersonIdentification.Full;

        Assert.Equal(PersonIdentification.Full, harness.ViewModel.IdentificationMode);
        Assert.Equal(1, refreshes);
    }

    [Fact]
    public void TheHintShowsThePersonIdWhenAnonymous()
    {
        using CultureScope culture = new("en-US");
        using DatasetHarness harness = new();

        harness.LoadAndCollect(personId: 52, varName: "B-Hemo", value: 10);

        harness.ViewModel.UpdateHint(0, 0, new Rect(100, 200, 64, 17));

        Assert.NotNull(harness.ViewModel.Hint);
        Assert.Equal("PersonId = 52", harness.ViewModel.Hint.Line1);
        Assert.StartsWith("B-Hemo = 10", harness.ViewModel.Hint.Line2, StringComparison.Ordinal);
    }

    [Fact]
    public void TheHintShowsTheNameWhenFullyIdentified()
    {
        using CultureScope culture = new("en-US");
        using DatasetHarness harness = new();

        harness.LoadAndCollect();
        harness.Identification.Mode = PersonIdentification.Full;

        harness.ViewModel.UpdateHint(0, 0, new Rect(100, 200, 64, 17));

        Assert.NotNull(harness.ViewModel.Hint);
        Assert.Equal("Hansen, Ola", harness.ViewModel.Hint.Line1);
    }

    [Fact]
    public void TheHintShowsTheRealPersonIdEvenWithPseudonyms()
    {
        // fGrid.Anonymous is "not rbFullIdentification.Checked", and thisPatient.PersonId is the real
        // one; pseudonyms are produced at export time, not on screen.
        using CultureScope culture = new("en-US");
        using DatasetHarness harness = new();

        harness.LoadAndCollect(personId: 52);
        harness.Identification.Mode = PersonIdentification.RandomPersonId;

        harness.ViewModel.UpdateHint(0, 0, new Rect(0, 0, 64, 17));

        Assert.NotNull(harness.ViewModel.Hint);
        Assert.Equal("PersonId = 52", harness.ViewModel.Hint.Line1);
    }

    [Fact]
    public void TheHintIsAnchoredJustBelowTheClickedCell()
    {
        // Delphi: OffsetRect(panRect, 3, 3) then Top := panRect.Top + DefaultRowHeight + 1.  The
        // cell's bottom already includes the row height, so that is four units below it.
        using DatasetHarness harness = new();

        harness.LoadAndCollect();

        harness.ViewModel.UpdateHint(0, 0, new Rect(100, 200, 64, 17));

        Assert.NotNull(harness.ViewModel.Hint);
        Assert.Equal(new Point(103, 221), harness.ViewModel.Hint.Anchor);
    }

    [Fact]
    public void TheHintIsHiddenWhenTheCellHasNoDataPoint()
    {
        using DatasetHarness harness = new();

        harness.Matrix.PreparePopulation(
            [ShellWorkspaceTests.NewPatient(8), ShellWorkspaceTests.NewPatient(13)]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation());
        ShellWorkspaceTests.AddColumn(harness.Matrix, "AGE", 8, 64);
        harness.Matrix.Lock();
        harness.Workspace.NotifyDataChanged();

        // Row 1 is person 13, who has no AGE.
        harness.ViewModel.UpdateHint(1, 0, new Rect(0, 17, 64, 17));

        Assert.Null(harness.ViewModel.Hint);
    }

    [Fact]
    public void TheHintIsHiddenWhenShowDataHintIsOff()
    {
        using DatasetHarness harness = new();

        harness.LoadAndCollect();
        harness.ViewModel.ShowDataHint = false;

        harness.ViewModel.UpdateHint(0, 0, new Rect(0, 0, 64, 17));

        Assert.Null(harness.ViewModel.Hint);
    }

    [Fact]
    public void TurningTheHintCheckBoxOffHidesWhatIsAlreadyThere()
    {
        // cbShowDataHint.OnClick is UpdateDataHintPanel, whose very first statement hides the panel.
        using DatasetHarness harness = new();

        harness.LoadAndCollect();
        harness.ViewModel.UpdateHint(0, 0, new Rect(0, 0, 64, 17));
        Assert.NotNull(harness.ViewModel.Hint);

        harness.ViewModel.ShowDataHint = false;

        Assert.Null(harness.ViewModel.Hint);
    }

    [Fact]
    public void TogglingTheCheckBoxAsksForTheHintToBeRebuiltEitherWay()
    {
        // The other half of the same Delphi procedure: after hiding the panel it reads fGrid.Col and
        // fGrid.Row and builds the hint for the CURRENT cell.  The view-model cannot see the caret,
        // so all it can do is ask - and it has to ask on the way up as well as on the way down,
        // because "hidden" is also an answer that has to be recomputed rather than assumed.
        using DatasetHarness harness = new();

        harness.LoadAndCollect();

        int asked = 0;

        harness.ViewModel.HintRefreshRequested += (_, _) => asked++;

        harness.ViewModel.ShowDataHint = false;
        Assert.Equal(1, asked);

        harness.ViewModel.ShowDataHint = true;
        Assert.Equal(2, asked);
    }

    [Fact]
    public void TheHintIsHiddenBeforeTheViewIsAskedForANewOne()
    {
        // Ordering, not decoration.  A view that never answers - a headless view-model, or the
        // window between construction and DataContext - has to be left with the hint off; if the
        // clearing came after the event the two would fight over the same property.
        using DatasetHarness harness = new();

        harness.LoadAndCollect();
        harness.ViewModel.UpdateHint(0, 0, new Rect(0, 0, 64, 17));

        DataHint? whenAsked = harness.ViewModel.Hint;

        harness.ViewModel.HintRefreshRequested += (_, _) => whenAsked = harness.ViewModel.Hint;

        harness.ViewModel.ShowDataHint = false;

        Assert.Null(whenAsked);
    }

    [Fact]
    public void TheHintIsHiddenWhenTheCellIsScrolledOutOfView()
    {
        // MatrixGrid.TryGetCellBounds returns false rather than an off-screen rectangle, so the
        // caller hides the hint instead of parking it outside the window.
        using DatasetHarness harness = new();

        harness.LoadAndCollect();

        harness.ViewModel.UpdateHint(0, 0, null);

        Assert.Null(harness.ViewModel.Hint);
    }

    [Fact]
    public void TheHintIsHiddenForTheHeaderRowAndTheFixedColumns()
    {
        using DatasetHarness harness = new();

        harness.LoadAndCollect();

        harness.ViewModel.UpdateHint(-1, 0, new Rect(0, 0, 64, 17));
        Assert.Null(harness.ViewModel.Hint);

        harness.ViewModel.UpdateHint(0, -1, new Rect(0, 0, 44, 17));
        Assert.Null(harness.ViewModel.Hint);
    }

    [Fact]
    public void TheHintUsesTheCurrentCultureForTheValueButNotForThePersonId()
    {
        // DataPoint.Describe follows the Delphi's %g and locale short date; the person id goes
        // through %d, which applies no digit grouping in any locale.
        using CultureScope culture = new("nb-NO");
        using DatasetHarness harness = new();

        harness.LoadAndCollect(personId: 1234, varName: "BMI", value: 24.5);

        harness.ViewModel.UpdateHint(0, 0, new Rect(0, 0, 64, 17));

        Assert.NotNull(harness.ViewModel.Hint);
        Assert.Equal("PersonId = 1234", harness.ViewModel.Hint.Line1);
        Assert.StartsWith("BMI = 24,5", harness.ViewModel.Hint.Line2, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadingAPopulationClearsAStaleHint()
    {
        using DatasetHarness harness = new();

        harness.LoadAndCollect();
        harness.ViewModel.UpdateHint(0, 0, new Rect(0, 0, 64, 17));
        Assert.NotNull(harness.ViewModel.Hint);

        harness.Matrix.PreparePopulation([ShellWorkspaceTests.NewPatient(99)]);
        harness.Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation(2, "Andre"));

        Assert.Null(harness.ViewModel.Hint);
    }

    [Fact]
    public void DisposingUnsubscribesFromTheSharedServices()
    {
        // The workspace and the policy are singletons that outlive the window; without this the
        // whole view-model graph stays reachable.
        DatasetHarness harness = new();

        harness.ViewModel.Dispose();

        harness.Identification.Mode = PersonIdentification.Full;

        Assert.Equal(PersonIdentification.PersonIdOnly, harness.ViewModel.IdentificationMode);

        harness.TempFiles.Dispose();
    }
}
