using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using QuickStat.Controls.Dataset;
using QuickStat.Diagnostics;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.DataPoints;
using QuickStat.Domain.Matrix;
using QuickStat.Export;
using QuickStat.Services;

namespace QuickStat.ViewModels;

/// <summary>The <c>Dataset</c> tab: caption bar, the grid, the floating hint, and the save actions.</summary>
/// <remarks>
/// <para>
/// <c>05-ui-spec.md</c> §C.1, §D.1, §D.2, §G.2. Step 3.1 owns this.
/// </para>
/// <para>
/// The grid itself is <see cref="MatrixGrid"/>, which step 3.5 owns; this view-model binds to it and
/// never reaches inside it. Rendering, scrolling, hit-testing and the automation peer are all over
/// there.
/// </para>
/// </remarks>
public sealed partial class DatasetViewModel : ObservableObject, IDisposable
{
    /// <summary>Caption before a population has been loaded. Delphi <c>hdrPopulationName.Caption</c>.</summary>
    public const string DefaultCaption = "Your dataset";

    /// <summary>
    /// Live caption. Delphi <c>rsGridInfo</c>, verbatim including the quotation marks.
    /// </summary>
    /// <remarks>
    /// Arguments in order: <c>ProcId</c>, <c>Title</c>, <b>rows</b>, <b>columns</b>. The
    /// <c>%d x %d</c> is rows by columns, which reads backwards if you assume width first;
    /// screenshot 3 shows <c>17 x 20</c> for 17 patients over 20 fields.
    /// </remarks>
    public const string CaptionFormat = "Population: {0} \"{1}\". Grid size: {2} x {3}";

    /// <summary>Shown instead of an export when there is nothing to write.</summary>
    public const string NothingToExportMessage =
        "There is no dataset to export yet. Load a population, tick some data elements, and click \"Collect data\".";

    /// <summary>Horizontal offset of the hint from the clicked cell's left edge.</summary>
    /// <remarks>Delphi <c>OffsetRect(panRect, 3, 3)</c>.</remarks>
    private const double HintOffsetX = 3;

    /// <summary>Vertical offset of the hint below the clicked cell's bottom edge.</summary>
    /// <remarks>
    /// Delphi: <c>panRect.Top + 3</c> (the offset) <c>+ DefaultRowHeight + 1</c>. Since the cell's
    /// bottom already includes the row height, that is four device-independent units below it.
    /// </remarks>
    private const double HintOffsetY = 4;

    private readonly IShellWorkspace _workspace;
    private readonly IIdentificationPolicy _identification;
    private readonly IDatasetExporter _exporter;
    private readonly ITempFileTracker _tempFiles;
    private readonly IFileDialogService _fileDialogs;
    private readonly IProcessLauncher _launcher;
    private readonly IUserNotifier _notifier;
    private readonly IShellProgress _progress;
    private readonly ILogger<DatasetViewModel> _logger;

    [ObservableProperty]
    private string _captionText = DefaultCaption;

    [ObservableProperty]
    private bool _wideColumns;

    [ObservableProperty]
    private bool _showDataHint = true;

    [ObservableProperty]
    private DataHint? _hint;

    [ObservableProperty]
    private PersonIdentification _identificationMode;

    private bool _hasData;
    private bool _disposed;

    /// <summary>Creates the Dataset tab's view-model.</summary>
    /// <param name="workspace">Cross-tab state: the matrix, the population, the export flags.</param>
    /// <param name="identification">The one shared identification mode.</param>
    /// <param name="exporter">Writes CSV and xlsx.</param>
    /// <param name="tempFiles">Remembers the temporary CSV so it can be deleted on exit.</param>
    /// <param name="fileDialogs">The Save-as dialog.</param>
    /// <param name="launcher">Hands the temporary CSV to Excel.</param>
    /// <param name="notifier">Reports failures to the user.</param>
    /// <param name="progress">Status line.</param>
    /// <param name="logger">Log.</param>
    public DatasetViewModel(
        IShellWorkspace workspace,
        IIdentificationPolicy identification,
        IDatasetExporter exporter,
        ITempFileTracker tempFiles,
        IFileDialogService fileDialogs,
        IProcessLauncher launcher,
        IUserNotifier notifier,
        IShellProgress progress,
        ILogger<DatasetViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(identification);
        ArgumentNullException.ThrowIfNull(exporter);
        ArgumentNullException.ThrowIfNull(tempFiles);
        ArgumentNullException.ThrowIfNull(fileDialogs);
        ArgumentNullException.ThrowIfNull(launcher);
        ArgumentNullException.ThrowIfNull(notifier);
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(logger);

        _workspace = workspace;
        _identification = identification;
        _exporter = exporter;
        _tempFiles = tempFiles;
        _fileDialogs = fileDialogs;
        _launcher = launcher;
        _notifier = notifier;
        _progress = progress;
        _logger = logger;

        _identificationMode = identification.Mode;

        _identification.ModeChanged += OnIdentificationModeChanged;
        _workspace.PopulationChanged += OnPopulationChanged;
        _workspace.DataChanged += OnDataChanged;
        _workspace.PropertyChanged += OnWorkspacePropertyChanged;
    }

    /// <summary>
    /// Raised when the grid must repaint because <see cref="PersonMatrix"/> changed underneath it.
    /// </summary>
    /// <remarks>
    /// The matrix is a plain mutable object with no change notification, so a collect run adds
    /// columns to the very instance the control is bound to and no dependency property moves. The
    /// view answers this by calling <see cref="MatrixGrid.Refresh"/>.
    /// </remarks>
    public event EventHandler? GridRefreshRequested;

    /// <summary>
    /// Raised by <c>SaveDataPackageCommand</c>. <b>Step 3.4 subscribes and does the work.</b>
    /// </summary>
    /// <remarks>
    /// The command's caption, its home in the grid context menu and its enable rule are all Dataset
    /// tab concerns (§D.1, §D.2) and live here. What happens on execute is not: it shows
    /// <c>TfrmSaveSpec</c> (step 3.6), builds a
    /// <see cref="QuickStat.Domain.Packages.PackagedSelection"/> from
    /// <see cref="IShellWorkspace.CheckedCollectorNames"/> and the current population, saves it
    /// through <see cref="QuickStat.Domain.Packages.IPackageRepository"/>, and refreshes the
    /// packages list - all of which belongs to the Packages tab. So this is the seam: 3.4 injects
    /// this view-model and subscribes in its constructor.
    /// </remarks>
    public event EventHandler? SaveDataPackageRequested;

    /// <summary>The dataset the grid renders.</summary>
    public PersonMatrix Matrix => _workspace.Matrix;

    /// <summary>Data-column width: 64, or 120 when <see cref="WideColumns"/> is on.</summary>
    /// <remarks>Delphi <c>cbWideColumnsChecked</c>: <c>DataColWidth := 120</c> or <c>COL_WIDTH = 64</c>.</remarks>
    public double DataColumnWidth =>
        WideColumns ? MatrixGrid.WideDataColumnWidth : MatrixGrid.NarrowDataColumnWidth;

    /// <summary>
    /// Whether a collect run has ever produced data. Gates <c>Open this dataset in Excel</c>.
    /// </summary>
    /// <remarks>
    /// <b>Latching, on purpose.</b> §D.1: <c>actExportData.Enabled := fGrid.Data.HasData</c> is set
    /// inside <c>actCollectDataExecute</c> and never reset to false, so in the Delphi the menu item
    /// stays enabled after a new population is loaded and the columns are gone. Reproducing the
    /// latch keeps the menu behaving as users expect; the execute path guards the empty case rather
    /// than the enable rule, which is why <see cref="NothingToExportMessage"/> exists.
    /// </remarks>
    public bool HasData
    {
        get => _hasData;

        private set
        {
            if (_hasData == value)
            {
                return;
            }

            _hasData = value;

            OnPropertyChanged();
            OpenInExcelCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>Refreshes the caption from the workspace. Delphi <c>UpdateGridInfo</c>.</summary>
    /// <remarks>
    /// Called after a population load and at the end of a collect run. With no population the
    /// caption falls back to <see cref="DefaultCaption"/> rather than showing
    /// <c>Population: 0 ""</c>.
    /// </remarks>
    public void RefreshCaption()
    {
        if (_workspace.Population is not { } population)
        {
            CaptionText = DefaultCaption;

            return;
        }

        CaptionText = string.Format(
            CultureInfo.CurrentCulture,
            CaptionFormat,
            population.ProcId,
            population.Title,
            _workspace.RowCount,
            _workspace.ColumnCount);
    }

    /// <summary>
    /// Replaces the caption with arbitrary text.
    /// </summary>
    /// <param name="caption">The text.</param>
    /// <remarks>
    /// Step 3.4 calls this at the end of a package replay:
    /// <c>hdrPopulationName.Caption := packagedSelection.Title</c>
    /// (<c>MainQuickStat.pas:806</c>) overwrites the computed caption with the package's own title,
    /// and stays until the next population load.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="caption"/> is <see langword="null"/>.</exception>
    public void SetCaption(string caption)
    {
        ArgumentNullException.ThrowIfNull(caption);

        CaptionText = caption;
    }

    /// <summary>Recomputes the floating hint for a clicked cell.</summary>
    /// <param name="rowIndex">Index into <see cref="PersonMatrix.Rows"/>, or <see cref="MatrixGrid.NoIndex"/>.</param>
    /// <param name="columnIndex">Index into <see cref="PersonMatrix.Columns"/>, or <see cref="MatrixGrid.NoIndex"/>.</param>
    /// <param name="cellBounds">
    /// The clicked cell's rectangle from <see cref="MatrixGrid.TryGetCellBounds"/>, or
    /// <see langword="null"/> when the cell is not laid out.
    /// </param>
    /// <remarks>
    /// <para>
    /// Delphi <c>UpdateDataHintPanel</c>. The hint is hidden first, unconditionally, and only
    /// reappears if every condition holds: the check box is on, the row is a real patient, and the
    /// cell has a datapoint. A cell with no value therefore hides the hint rather than showing an
    /// empty one.
    /// </para>
    /// <para>
    /// Any failure turns the status line red and shows the message, which is the Delphi's
    /// <c>lblInfo.Font.Color := clRed</c> branch.
    /// </para>
    /// </remarks>
    public void UpdateHint(int rowIndex, int columnIndex, Rect? cellBounds)
    {
        Hint = null;

        if (!ShowDataHint || cellBounds is not { } bounds)
        {
            return;
        }

        try
        {
            if ((uint)rowIndex >= (uint)_workspace.Matrix.Rows.Count)
            {
                return;
            }

            if (!_workspace.Matrix.TryGetDataPoint(rowIndex, columnIndex, out DataPoint? dataPoint))
            {
                return;
            }

            MatrixRow row = _workspace.Matrix.Rows[rowIndex];

            Hint = new DataHint(
                DescribePerson(row, IdentificationMode),
                dataPoint.Describe(CultureInfo.CurrentCulture),
                new Point(bounds.Left + HintOffsetX, bounds.Bottom + HintOffsetY));
        }
        catch (Exception exception)
        {
            _progress.Fail(exception.Message);

            _logger.LogWarning(exception, "Could not build the data hint.");
        }
    }

    /// <summary>Line one of the hint.</summary>
    /// <param name="row">The clicked row.</param>
    /// <param name="identification">The current mode.</param>
    /// <returns>
    /// <c>PersonId = &lt;n&gt;</c> for anything but
    /// <see cref="PersonIdentification.Full"/>; otherwise the patient's full name.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Delphi: <c>if fGrid.Anonymous then Format('PersonId = %d', [PersonId]) else FullName</c>, and
    /// <c>Anonymous</c> is <c>not rbFullIdentification.Checked</c>. The identifier is the
    /// <em>real</em> person id even in <see cref="PersonIdentification.RandomPersonId"/> mode -
    /// pseudonyms are produced at export time, not on screen.
    /// </para>
    /// <para>
    /// The number is formatted invariantly: <c>%d</c> applies no digit grouping, so a Norwegian
    /// machine must not print <c>1 234</c> where an English one prints <c>1234</c>.
    /// </para>
    /// </remarks>
    internal static string DescribePerson(MatrixRow row, PersonIdentification identification)
    {
        ArgumentNullException.ThrowIfNull(row);

        return identification == PersonIdentification.Full
            ? row.FullName
            : string.Create(CultureInfo.InvariantCulture, $"PersonId = {row.PersonId}");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _identification.ModeChanged -= OnIdentificationModeChanged;
        _workspace.PopulationChanged -= OnPopulationChanged;
        _workspace.DataChanged -= OnDataChanged;
        _workspace.PropertyChanged -= OnWorkspacePropertyChanged;
    }

    /// <summary>
    /// <c>Package dataset specification for reuse</c>. Enabled while something is ticked and a
    /// population is loaded.
    /// </summary>
    /// <remarks>
    /// Delphi <c>actSaveDataPackage</c>, enabled by <c>ValidateCollectorSelection</c> together with
    /// <c>actCollectData</c>, and additionally guarded at execute time by
    /// <c>Guard.CheckNotNull(fGridPopulation)</c> - an assertion that would have crashed the
    /// application. Folding that guard into <c>CanExecute</c> greys the menu item instead, which is
    /// what §D.1's sketch recommends.
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanSaveDataPackage))]
    private void SaveDataPackage() => SaveDataPackageRequested?.Invoke(this, EventArgs.Empty);

    private bool CanSaveDataPackage() =>
        _workspace.CheckedCollectorNames.Count > 0 && _workspace.Population is not null;

    /// <summary>
    /// <c>Open this dataset in Excel</c>: a temporary CSV, tracked for deletion, handed to the shell.
    /// </summary>
    /// <remarks>
    /// Delphi <c>actExportToExcelExecute</c>. Two differences, both from PORT-PLAN.md §7:
    /// the <c>Sleep(50)</c> message-pump loop that blocked QuickStat until Excel exited is gone
    /// (§7.3), and the key file - which the Delphi wrote next to every anonymised export and never
    /// deleted - is off by default and tracked when it is on (§7.2).
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanOpenInExcel))]
    private async Task OpenInExcelAsync(CancellationToken cancellationToken)
    {
        if (!EnsureExportable())
        {
            return;
        }

        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".csv");

        try
        {
            using IDisposable operation = _progress.BeginOperation("Preparing the dataset for Excel ...");

            // Tracked before the write, not after: a partially written file still has to be cleaned
            // up, and the Delphi registers the name up front for the same reason.
            _tempFiles.Track(path);

            DatasetExportResult result = await _exporter
                .ExportAsync(_workspace.Matrix, path, BuildExportOptions(), cancellationToken)
                .ConfigureAwait(true);

            if (result.KeyFilePath is { } keyFile)
            {
                _tempFiles.Track(keyFile);
            }

            _launcher.OpenWithShell(result.FilePath);

            _progress.Done();
        }
        catch (OperationCanceledException)
        {
            _tempFiles.Delete(path);
        }
        catch (Exception exception)
        {
            _tempFiles.Delete(path);

            await ReportExportFailureAsync(exception, "Could not open the dataset in Excel.").ConfigureAwait(true);
        }
    }

    private bool CanOpenInExcel() => HasData;

    /// <summary>
    /// <c>Save this dataset to CSV file</c>. Always enabled, exactly as the Delphi action is.
    /// </summary>
    /// <remarks>
    /// Delphi <c>actSaveDataset</c> has <c>Enabled</c> unset in the <c>.dfm</c> and nothing ever
    /// changes it, so the menu item is live even before anything has been collected. §D.1 records
    /// that as-implemented behaviour and the port keeps it; the empty case is caught at execute
    /// time, where the user gets a sentence instead of a file full of <c>(not ready)</c>.
    /// </remarks>
    [RelayCommand]
    private async Task SaveDatasetToCsvAsync(CancellationToken cancellationToken)
    {
        if (!EnsureExportable())
        {
            return;
        }

        if (_fileDialogs.ShowSaveFileDialog(SaveFileRequest.DatasetCsv) is not { } path)
        {
            return;
        }

        try
        {
            using IDisposable operation = _progress.BeginOperation("Saving the dataset ...");

            DatasetExportResult result = await _exporter
                .ExportAsync(_workspace.Matrix, path, BuildExportOptions(), cancellationToken)
                .ConfigureAwait(true);

            _logger.LogInformation(
                "Saved {RowCount} x {ColumnCount} to {Path}.",
                result.RowCount,
                result.ColumnCount,
                result.FilePath);

            _progress.Done();
        }
        catch (OperationCanceledException)
        {
            _progress.Reset();
        }
        catch (Exception exception)
        {
            await ReportExportFailureAsync(exception, "Could not save the dataset.").ConfigureAwait(true);
        }
    }

    /// <summary>Builds the options both export commands use.</summary>
    /// <returns>The options.</returns>
    /// <remarks>
    /// The mode comes from <see cref="IIdentificationPolicy"/> and the timestamp flag from the
    /// workspace, so what the grid shows and what the file contains cannot disagree - PORT-PLAN.md
    /// §7.2. The column set is derived, not chosen: <c>DatasetExportOptions.Columns</c> has no
    /// setter.
    /// </remarks>
    private DatasetExportOptions BuildExportOptions() => new()
    {
        Identification = _identification.Mode,
        IncludeTimestamps = _workspace.ExportTimestamps,
        Format = ExportFormat.Csv,
        Dialect = CsvDialect.Legacy,
    };

    /// <summary>Refuses to export a matrix that would produce a meaningless file.</summary>
    /// <returns><see langword="true"/> when the export may proceed.</returns>
    /// <remarks>
    /// <b>An improvement, flagged.</b> The Delphi exports whatever is there: an unlocked matrix
    /// writes the literal <c>(not ready)</c> into every cell and an empty population writes a
    /// phantom <c>"nil"</c> row. Since <c>actSaveDataset</c> is always enabled and
    /// <c>actExportData</c> latches on, both are reachable. Saying so is better than producing the
    /// file.
    /// </remarks>
    private bool EnsureExportable()
    {
        if (_workspace.Matrix is { HasData: true, IsLocked: true })
        {
            return true;
        }

        // Fire and forget: the notifier marshals and logs, and the command has nothing to do with
        // the answer.
        _ = _notifier.InformAsync(NothingToExportMessage);

        return false;
    }

    private async Task ReportExportFailureAsync(Exception exception, string headline)
    {
        _logger.LogError(exception, "{Headline}", headline);

        _progress.Fail(exception.Message);

        await _notifier.ErrorAsync(headline + Environment.NewLine + Environment.NewLine + exception.Message)
            .ConfigureAwait(true);
    }

    partial void OnWideColumnsChanged(bool value)
    {
        _ = value;

        OnPropertyChanged(nameof(DataColumnWidth));
    }

    partial void OnShowDataHintChanged(bool value)
    {
        // Delphi: cbShowDataHint.OnClick is UpdateDataHintPanel, whose first statement hides the
        // panel.  Turning the box back on does not restore the previous hint - the user has to
        // click a cell again.
        if (!value)
        {
            Hint = null;
        }
    }

    private void OnIdentificationModeChanged(object? sender, PersonIdentification mode)
    {
        IdentificationMode = mode;

        // The grid hides or shows Født / Fødselsnummer / Navn, so it has to repaint; and the hint's
        // first line changes meaning, so a stale one would now be wrong.
        Hint = null;

        GridRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnPopulationChanged(object? sender, EventArgs e)
    {
        Hint = null;

        RefreshCaption();

        SaveDataPackageCommand.NotifyCanExecuteChanged();

        GridRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnDataChanged(object? sender, EventArgs e)
    {
        // Latching: see HasData.  Never assigned false here.
        if (_workspace.HasData)
        {
            HasData = true;
        }

        RefreshCaption();

        GridRefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IShellWorkspace.CheckedCollectorNames))
        {
            SaveDataPackageCommand.NotifyCanExecuteChanged();
        }
    }
}
