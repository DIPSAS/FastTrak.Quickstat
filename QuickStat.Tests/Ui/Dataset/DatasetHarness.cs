using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Diagnostics;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;
using QuickStat.Services;
using QuickStat.Tests.Ui.Services;
using QuickStat.ViewModels;

namespace QuickStat.Tests.Ui.Dataset;

/// <summary>Everything one Dataset-tab case needs, wired the way the container wires it.</summary>
/// <remarks>
/// Shared by <see cref="DatasetViewModelTests"/>, which drives the view-model on its own, and
/// <see cref="DatasetTabHintTests"/>, which puts the real view around it. One wiring, so a case that
/// goes through the view cannot quietly be composed differently from one that does not.
/// </remarks>
internal sealed class DatasetHarness : IDisposable
{
    internal DatasetHarness()
    {
        Matrix = ShellWorkspaceTests.NewMatrix();
        Workspace = new ShellWorkspace(Matrix);
        Identification = new IdentificationPolicy();
        Exporter = new FakeDatasetExporter();
        TempFiles = new FakeTempFileTracker();
        FileDialogs = new FakeFileDialogService();
        Launcher = new FakeProcessLauncher();
        Presenter = new HeadlessNotificationPresenter();
        Progress = new ShellProgress(new InlineUiDispatcher());

        ViewModel = new DatasetViewModel(
            Workspace,
            Identification,
            Exporter,
            TempFiles,
            FileDialogs,
            Launcher,
            new UserNotifier(Presenter, NullLogger<UserNotifier>.Instance),
            Progress,
            NullLogger<DatasetViewModel>.Instance);
    }

    internal PersonMatrix Matrix { get; }

    internal ShellWorkspace Workspace { get; }

    internal IdentificationPolicy Identification { get; }

    internal FakeDatasetExporter Exporter { get; }

    internal FakeTempFileTracker TempFiles { get; }

    internal FakeFileDialogService FileDialogs { get; }

    internal FakeProcessLauncher Launcher { get; }

    internal HeadlessNotificationPresenter Presenter { get; }

    internal ShellProgress Progress { get; }

    internal DatasetViewModel ViewModel { get; }

    /// <summary>Loads one patient and one collected column, and locks - i.e. a finished run.</summary>
    /// <param name="personId">The one patient's id.</param>
    /// <param name="varName">The one column's variable name.</param>
    /// <param name="value">The one value.</param>
    internal void LoadAndCollect(int personId = 52, string varName = "B-Hemo", double value = 10)
    {
        Matrix.PreparePopulation([ShellWorkspaceTests.NewPatient(personId)]);
        Workspace.SetPopulation(ShellWorkspaceTests.NewPopulation());

        ShellWorkspaceTests.AddColumn(Matrix, varName, personId, value);

        Matrix.Lock();
        Workspace.NotifyDataChanged();
    }

    public void Dispose()
    {
        ViewModel.Dispose();
        TempFiles.Dispose();
    }
}
