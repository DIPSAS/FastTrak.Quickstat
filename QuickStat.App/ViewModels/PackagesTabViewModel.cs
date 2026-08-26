using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using QuickStat.Data;
using QuickStat.Diagnostics;
using QuickStat.Domain.Matrix;
using QuickStat.Domain.Packages;
using QuickStat.Domain.Patients;
using QuickStat.Domain.Populations;
using QuickStat.Services;

namespace QuickStat.ViewModels;

/// <summary>The <c>Packages</c> tab: filter, delete button, and the packaged-datasets list.</summary>
/// <remarks>
/// <para>
/// <c>05-ui-spec.md</c> §B.3, §D.1, §D.3, §D.4. Step 3.4 owns this. Delphi:
/// <c>LoadPackagedSelections</c>, <c>PreparePackagedSelection</c>, <c>actSaveDataPackageExecute</c>
/// and <c>actDeletePackageExecute</c> (<c>MainQuickStat.pas:770-901</c>).
/// </para>
/// <para>
/// <b>The replay is the whole point of this tab.</b> A double click finds the package's population,
/// loads it into the matrix, unticks every data element, ticks the ones the package stored
/// <em>by name</em>, runs the collect, and finally overwrites the dataset caption with the package
/// title. Every one of those steps is somebody else's code; this view-model only sequences them.
/// </para>
/// </remarks>
public sealed partial class PackagesTabViewModel : ObservableObject, IDisposable
{
    /// <summary>Teal header. <b>Not</b> <c>Packages</c> - that is only the tab caption.</summary>
    public const string PackagesHeader = "Packaged datasets";

    /// <summary>The toolbar button and the context-menu item. Delphi <c>actDeletePackage</c>.</summary>
    public const string DeletePackageCaption = "Delete this package";

    /// <summary>
    /// The delete confirmation, one argument: the package title. Delphi
    /// <c>CONFIRM_DELETE_PACKAGE</c>.
    /// </summary>
    /// <remarks>
    /// The Delphi resource string contains the two-character sequence <c>\n</c>, which a
    /// single-quoted Pascal literal does not process, so users see a backslash and an <c>n</c>. §I.8
    /// and decision (d) in <c>Docs/Port/07-ui-contracts.md</c>: real line breaks in the port.
    /// </remarks>
    public const string ConfirmDeleteFormat = "Do you really want to delete this package:\n\"{0}\"?";

    /// <summary>
    /// Warning when the package names a population this study does not have. One argument: the
    /// <c>ProcId</c>. Delphi <c>MSG_UNKNOWN_POPULATION</c>.
    /// </summary>
    public const string UnknownPopulationFormat =
        "The selection is based on an unknown population (ProcId={0}).\n"
        + "The data collection can not be performed at this time.\n"
        + "Perhaps the population is from a different protocol?";

    /// <summary>
    /// Warning when a stored collector name matches nothing in the registry. One argument: the
    /// name. Delphi <c>MSG_UNKNOWN_COLLECTOR</c>.
    /// </summary>
    public const string UnknownCollectorFormat =
        "The selection contains an unknown data element.\n"
        + "Element name was \"{0}\".\n"
        + "The data collection will be incomplete.\n"
        + "Perhaps the selection was created in a later version?";

    /// <summary>Shown when a replay is attempted before a project has been chosen.</summary>
    /// <remarks>
    /// <b>An addition.</b> The Delphi cannot reach this state - its population frame and its package
    /// list are both empty until <c>AfterLogin</c> - but here the list is loaded from the server and
    /// a disconnect between load and double click is reachable. Saying so beats a silent no-op.
    /// </remarks>
    public const string NotConnectedMessage =
        "There is no open connection, so this package can not be replayed.\n"
        + "Choose a project on the Population tab first.";

    private readonly IShellWorkspace _workspace;
    private readonly IShellProgress _progress;
    private readonly IUiDispatcher _dispatcher;
    private readonly ISessionService _session;
    private readonly IPackageRepository _repository;
    private readonly IPatientRepository _patients;
    private readonly IPopulationRepository _populationRepository;
    private readonly IQueryParameterResolver _parameters;
    private readonly IUserNotifier _notifier;
    private readonly PopulationPickerViewModel _populations;
    private readonly CollectionsTabViewModel _collections;
    private readonly DatasetViewModel _dataset;
    private readonly ILogger<PackagesTabViewModel> _logger;

    [ObservableProperty]
    private string _filterText = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeletePackageCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenPackageCommand))]
    private PackageViewModel? _selectedPackage;

    private bool _disposed;

    /// <summary>Creates the tab's view-model and subscribes to the Dataset tab's save request.</summary>
    /// <param name="workspace">Cross-tab state: the matrix, the population, the ticked names.</param>
    /// <param name="progress">Status line and the busy flag.</param>
    /// <param name="dispatcher">Marshals the post-login reload onto the user-interface thread.</param>
    /// <param name="session">Tells us the study id, and when a project has been connected.</param>
    /// <param name="repository">Reads, writes and deletes rows in <c>Report.QuickStat</c>.</param>
    /// <param name="patients">Runs the population's own query during a replay.</param>
    /// <param name="populationRepository">Writes the <c>dbo.AddPopulationLog</c> audit row.</param>
    /// <param name="parameters">Resolves that query's placeholders, prompting for a period.</param>
    /// <param name="notifier">The §D.4 warnings and the delete confirmation.</param>
    /// <param name="populations">Where the replay finds the population to load. Step 3.2's.</param>
    /// <param name="collections">Where the replay ticks elements and starts the collect. Step 3.3's.</param>
    /// <param name="dataset">Raises the save request, and owns the caption a replay overwrites.</param>
    /// <param name="logger">Log.</param>
    public PackagesTabViewModel(
        IShellWorkspace workspace,
        IShellProgress progress,
        IUiDispatcher dispatcher,
        ISessionService session,
        IPackageRepository repository,
        IPatientRepository patients,
        IPopulationRepository populationRepository,
        IQueryParameterResolver parameters,
        IUserNotifier notifier,
        PopulationPickerViewModel populations,
        CollectionsTabViewModel collections,
        DatasetViewModel dataset,
        ILogger<PackagesTabViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(patients);
        ArgumentNullException.ThrowIfNull(populationRepository);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(notifier);
        ArgumentNullException.ThrowIfNull(populations);
        ArgumentNullException.ThrowIfNull(collections);
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(logger);

        _workspace = workspace;
        _progress = progress;
        _dispatcher = dispatcher;
        _session = session;
        _repository = repository;
        _patients = patients;
        _populationRepository = populationRepository;
        _parameters = parameters;
        _notifier = notifier;
        _populations = populations;
        _collections = collections;
        _dataset = dataset;
        _logger = logger;

        PackagesView = CollectionViewSource.GetDefaultView(Packages);
        PackagesView.Filter = MatchesFilter;

        _session.SessionChanged += OnSessionChanged;
        _dataset.SaveDataPackageRequested += OnSaveDataPackageRequested;
    }

    /// <summary>
    /// Raised when <c>Package dataset specification for reuse</c> needs the <c>Save specification</c>
    /// modal on screen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The other half of decision (l). The Dataset tab owns the command and hands the request here
    /// through <see cref="DatasetViewModel.SaveDataPackageRequested"/>; this view-model owns
    /// everything that happens next except the one step that needs a <c>Window</c>. Showing
    /// <c>SaveSpecDialog</c> is <c>PackagesTabView</c>'s job, exactly as
    /// <c>Views/Dialogs/SaveSpecDialog.xaml.cs</c> says: the caller sets the
    /// <c>DataContext</c> and reads it back when <c>ShowDialog</c> returns true.
    /// </para>
    /// <para>
    /// It is an event with a mutable argument rather than a service so that no container
    /// registration is needed and the whole save path stays unit-testable: a test subscribes, fills
    /// in <see cref="SaveSpecRequest.Title"/> and sets
    /// <see cref="SaveSpecRequest.Accepted"/>. <b>With no subscriber at all the request is treated
    /// as cancelled</b>, which is the safe answer for a headless host.
    /// </para>
    /// </remarks>
    public event EventHandler<SaveSpecRequest>? SaveSpecRequested;

    /// <summary>The saved specifications for this study, in server order.</summary>
    /// <remarks>
    /// Delphi <c>fPackagedQuickStatGrids</c>, filled by <c>LoadPackagedSelections</c> from
    /// <c>Report.QuickStat</c>. The client does not sort: §G.5 sorts the collector list and the
    /// project drop-down, and says nothing about this one.
    /// </remarks>
    public ObservableCollection<PackageViewModel> Packages { get; } = [];

    /// <summary>What the list actually shows: <see cref="Packages"/> behind the filter.</summary>
    public ICollectionView PackagesView { get; }

    /// <summary>Deletes the selected package after confirming.</summary>
    /// <remarks>
    /// <para>
    /// <c>CanExecute</c> is <c>SelectedPackage is not null</c>. <b>An improvement</b>, recorded in
    /// §D.1: the Delphi action is always enabled and warns
    /// <c>You need to select a package for this operation.</c> at execute time. That warning is
    /// therefore unreachable in the port and is not carried over.
    /// </para>
    /// <para>
    /// The confirmation goes through <see cref="IUserNotifier.ConfirmAsync"/>, whose Delphi
    /// ancestor <em>failed open</em> below the dialog threshold and would have answered yes on the
    /// user's behalf.
    /// </para>
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanActOnSelectedPackage))]
    private async Task DeletePackageAsync(CancellationToken cancellationToken)
    {
        if (SelectedPackage is not { } package)
        {
            return;
        }

        string question = string.Format(CultureInfo.CurrentCulture, ConfirmDeleteFormat, package.Title);

        if (!await _notifier.ConfirmAsync(question).ConfigureAwait(true))
        {
            return;
        }

        try
        {
            using IDisposable operation = _progress.BeginOperation(DeletePackageCaption);

            await _repository.DeleteAsync(package.RowId, cancellationToken).ConfigureAwait(true);

            // The Delphi calls lbPackagedGrids.Items.Delete(ItemIndex), which removes the *rendered*
            // row and leaves the object in fPackagedQuickStatGrids - so the next time the filter
            // repaints the list, the deleted package comes back (MainQuickStat.pas:897).  Removing
            // it from the model is the fix.
            Packages.Remove(package);
            SelectedPackage = null;

            _logger.LogInformation("Deleted package {RowId} \"{Title}\".", package.RowId, package.Title);
        }
        catch (OperationCanceledException)
        {
            _progress.Reset();
        }
        catch (Exception exception)
        {
            await ReportFailureAsync(exception, "Could not delete the package.").ConfigureAwait(true);
        }
    }

    /// <summary>Replays the selected package. Double click. Delphi <c>PreparePackagedSelection</c>.</summary>
    /// <remarks>
    /// <para>
    /// The sequence is <c>MainQuickStat.pas:780-814</c> step for step: find the population, load it
    /// into the matrix, untick everything, tick each stored collector by name, run the collect, set
    /// the caption. A collector the registry does not know produces a warning and the run continues
    /// - the Delphi says so in as many words: <c>The data collection will be incomplete.</c>
    /// </para>
    /// <para>
    /// <b>It switches to the Collections tab, like the population double-click.</b> An earlier
    /// revision of <c>Docs/Port/07-ui-contracts.md</c> §3.1 said the replay stayed here, and this
    /// step was instructed accordingly; steps 3.2 and 3.4 both traced the opposite and it was
    /// verified against the source. <c>PreparePackagedSelection</c> calls
    /// <c>TrySelect(procId, ALoadIt := true, …)</c> (<c>MainQuickStat.pas:789</c>) → <c>TrySelect</c>
    /// calls <c>PopulationRequested</c> (<c>EPR.VclFrame.Populations.pas:195</c>) → that notifies
    /// every observer's <c>AfterPopulationSelect</c> (<c>:217-218</c>) → and <c>TfrmQuickStat</c>,
    /// which registered itself at <c>MainQuickStat.pas:288</c>, ends that handler with
    /// <c>pgSelections.ActivePage := tbsDataElements</c> (<c>:541</c>). The tab therefore changes
    /// before the collect starts, which is also when the user can see which elements the package
    /// ticked.
    /// </para>
    /// <para>
    /// The Delphi then loads the cohort a <em>second</em> time, because control returns to
    /// <c>PreparePackagedSelection</c> which calls <c>LoadPopulationIntoGrid</c> itself. That is
    /// wasted work with no visible effect, and is not reproduced.
    /// </para>
    /// <para>
    /// The whole replay sits in one <see cref="IShellProgress.BeginOperation"/> scope and the collect
    /// opens another inside it. That is why the scope counts, and why the Delphi saves and restores
    /// <c>Screen.Cursor</c> instead of assigning <c>crDefault</c> (§G.3).
    /// </para>
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanActOnSelectedPackage))]
    private async Task OpenPackageAsync(CancellationToken cancellationToken)
    {
        if (SelectedPackage is not { } package)
        {
            return;
        }

        // Both refusals happen before the busy scope opens, and so do their message boxes.  The
        // Delphi is in the same state: TrySelect returns false before PopulationRequested is
        // reached, so nothing has set crSqlWait when MSG_UNKNOWN_POPULATION goes up.
        if (_session.Current is null)
        {
            await _notifier.WarnAsync(NotConnectedMessage).ConfigureAwait(true);

            return;
        }

        if (FindPopulation(package.Selection.PopulationId) is not { } population)
        {
            _logger.LogWarning(
                "Package {RowId} names population {ProcId}, which this study does not have.",
                package.RowId,
                package.Selection.PopulationId);

            await _notifier.WarnAsync(string.Format(
                CultureInfo.InvariantCulture,
                UnknownPopulationFormat,
                package.Selection.PopulationId)).ConfigureAwait(true);

            return;
        }

        using IDisposable operation = _progress.BeginOperation(package.Title);

        try
        {
            if (!await LoadPopulationAsync(population, cancellationToken).ConfigureAwait(true))
            {
                return;
            }

            // Where AfterPopulationSelect does it: after the cohort is in the matrix and before the
            // collect, so the ticks that ApplyCollectorSelectionAsync is about to make are on screen
            // while the run works down them.
            _workspace.RequestCollectionsTab();

            await ApplyCollectorSelectionAsync(package.Selection).ConfigureAwait(true);

            await _collections.CollectDataCommand.ExecuteAsync(null).ConfigureAwait(true);

            // Last, and after the collect: actCollectDataExecute ends in UpdateGridInfo, which writes
            // "Population: 1 "...". Grid size: 17 x 20" into the caption bar, and the Delphi then
            // overwrites it with the package title (MainQuickStat.pas:807).
            _dataset.SetCaption(package.Title);
        }
        catch (OperationCanceledException)
        {
            _progress.Reset();
        }
        catch (Exception exception)
        {
            await ReportFailureAsync(exception, "Could not replay the package.").ConfigureAwait(true);
        }
    }

    private bool CanActOnSelectedPackage() => SelectedPackage is not null;

    /// <summary>Reloads the list from <c>Report.QuickStat</c>. Delphi <c>LoadPackagedSelections</c>.</summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>A task that completes when the list has been replaced.</returns>
    /// <remarks>
    /// Called after login and after a save. Without a session the list is emptied, which is the
    /// state the Delphi is in before <c>AfterLogin</c> runs.
    /// </remarks>
    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        if (_session.Current is not { } current)
        {
            Replace([]);

            return;
        }

        try
        {
            IReadOnlyList<PackagedSelection> stored = await _repository
                .GetPackagesAsync(current.StudyId, cancellationToken)
                .ConfigureAwait(true);

            Replace(stored);

            _logger.LogInformation(
                "Loaded {PackageCount} packaged selections for study {StudyId}.",
                stored.Count,
                current.StudyId);
        }
        catch (OperationCanceledException)
        {
            // Shutting down or reconnecting; the next login reloads.
        }
        catch (Exception exception)
        {
            await ReportFailureAsync(exception, "Could not load the packaged datasets.").ConfigureAwait(true);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _session.SessionChanged -= OnSessionChanged;
        _dataset.SaveDataPackageRequested -= OnSaveDataPackageRequested;
    }

    /// <summary>
    /// The filter predicate. <b>This is not the population filter</b> - see
    /// <c>Docs/Port/07-ui-contracts.md</c> §5 and PORT-PLAN.md §8.8 (i).
    /// </summary>
    /// <param name="item">The candidate row.</param>
    /// <returns>Whether it stays visible.</returns>
    /// <remarks>
    /// <para>
    /// Delphi <c>TSpotLightContext.InternalRefreshList</c>
    /// (<c>Emetra.VclUtil.Spotlight.pas:143-164</c>), verbatim:
    /// <c>lookFor := AnsiUppercase(Trim(FEditFilter.Text))</c> and then
    /// <c>Pos(lookFor, AnsiUppercase(itemText)) &gt; 0</c> - a plain substring search, and the
    /// filter text <em>is</em> trimmed here where the population list's is not. That is a different
    /// class from the population frame's <c>TObjectListView.RefreshView</c>, which is why the two
    /// filters differ at all.
    /// </para>
    /// <para>
    /// <c>itemText</c> is <c>AsListBox(false)</c>, and <c>false</c> is not a choice: the package
    /// list's spotlight context is constructed with no <c>Simplified</c> check box
    /// (<c>MainQuickStat.pas:295</c>), so the comment is always part of what is matched.
    /// </para>
    /// <para>
    /// Both sides are folded with the <b>current culture</b>, because <c>AnsiUppercase</c> is
    /// locale-sensitive too; the comparison itself is
    /// <see cref="StringComparison.Ordinal"/>, never <c>CurrentCultureIgnoreCase</c>, which is a
    /// collation and folds more than <c>Pos</c> does.
    /// </para>
    /// </remarks>
    private bool MatchesFilter(object item)
    {
        if (item is not PackageViewModel package)
        {
            return false;
        }

        string needle = FilterText.Trim().ToUpper(CultureInfo.CurrentCulture);

        return needle.Length == 0
            || package.SearchText.ToUpper(CultureInfo.CurrentCulture).Contains(needle, StringComparison.Ordinal);
    }

    partial void OnFilterTextChanged(string value)
    {
        _ = value;

        PackagesView.Refresh();
    }

    private void Replace(IReadOnlyList<PackagedSelection> stored)
    {
        Packages.Clear();

        foreach (PackagedSelection selection in stored)
        {
            Packages.Add(new PackageViewModel(selection));
        }

        SelectedPackage = null;

        PackagesView.Refresh();
    }

    /// <summary>
    /// Finds the population a package replays. Delphi
    /// <c>fPopulations.TryGetPopulation(AProcId, ...)</c>.
    /// </summary>
    /// <param name="procId">The stored <c>ProcId</c>.</param>
    /// <returns>The catalogue row, or <see langword="null"/>.</returns>
    /// <remarks>
    /// <b>A deliberate fix.</b> The Delphi looks the population up in the full catalogue and then
    /// calls <c>TSelectionListView.TrySelectObject</c>, which searches the <em>rendered</em> list
    /// (<c>Emetra.VclComp.ListView.pas:245-261</c>) - so a filter left in the population box makes
    /// <c>TrySelect</c> return false and the replay report the population as unknown. Matching
    /// against the whole catalogue removes a failure that had nothing to do with the package.
    /// </remarks>
    private Population? FindPopulation(int procId)
    {
        foreach (PopulationViewModel candidate in _populations.Populations)
        {
            if (candidate.ProcId == procId)
            {
                return candidate.Population;
            }
        }

        return null;
    }

    /// <summary>Loads a population into the matrix. Delphi <c>LoadPopulationIntoGrid</c>.</summary>
    /// <param name="population">The population to run.</param>
    /// <param name="cancellationToken">Cancels the query and the period prompt.</param>
    /// <returns>Whether the matrix now holds that population.</returns>
    /// <remarks>
    /// <para>
    /// The order is a contract, not a style: <c>PersonMatrix</c> raises no notifications, so
    /// <see cref="IShellWorkspace.SetPopulation"/> has to come last or
    /// <see cref="IShellWorkspace.HasPopulation"/> reads the previous cohort's row count.
    /// </para>
    /// <para>
    /// <b><c>ClearPopulation</c> comes first, and that is not decoration.</b>
    /// <see cref="PersonMatrix.SortBy"/> throws once the matrix is locked, and a collect run locks
    /// it - so replaying a second package would fail on the sort assignment. The Delphi opens
    /// <c>LoadPopulationIntoGrid</c> with <c>fGrid.Data.ClearPopulation</c> for exactly this reason:
    /// it clears <c>fLocked</c> (<c>EPR.QA.Matrix.pas:211-215</c>).
    /// </para>
    /// <para>
    /// A cancelled period prompt is not an error and raises nothing, which is what
    /// <see cref="ParameterResolution.CancelledByUser"/> exists to say.
    /// </para>
    /// <para>
    /// The <c>dbo.AddPopulationLog</c> audit row is written here as well. The Delphi writes it from
    /// <c>PopulationRequested</c> (<c>EPR.VclFrame.Populations.pas:219</c>), which the replay reaches
    /// through <c>TrySelect</c> - so a replay does count towards the popularity ranking that the
    /// <c>Frequently used only</c> box reads, and skipping it here would quietly change which
    /// populations that box offers.
    /// </para>
    /// </remarks>
    private async Task<bool> LoadPopulationAsync(Population population, CancellationToken cancellationToken)
    {
        long startedAt = Stopwatch.GetTimestamp();

        ParameterResolution resolution = await _parameters
            .ResolveAsync(population.QueryText, cancellationToken)
            .ConfigureAwait(true);

        if (!resolution.Succeeded)
        {
            if (resolution.CancelledByUser)
            {
                _progress.Reset();
            }
            else
            {
                _progress.Fail(resolution.FailureReason ?? "");

                _logger.LogWarning(
                    "Could not resolve the parameters of population {ProcId}: {Reason}",
                    population.ProcId,
                    resolution.FailureReason);
            }

            return false;
        }

        IReadOnlyList<Patient> patients = await _patients
            .LoadPopulationAsync(population, resolution.Values, cancellationToken)
            .ConfigureAwait(true);

        PersonMatrix matrix = _workspace.Matrix;

        matrix.ClearPopulation();
        matrix.SortBy = MatrixSortOrder.PersonId;
        matrix.PreparePopulation(patients);

        _workspace.SetPopulation(population);

        LogPopulationSelected(population, Stopwatch.GetElapsedTime(startedAt));

        return true;
    }

    /// <summary>Writes the popularity audit row, and never lets it matter.</summary>
    /// <param name="population">The population that was prepared.</param>
    /// <param name="elapsed">How long preparing it took.</param>
    /// <remarks>
    /// Fire and forget, as <see cref="IPopulationRepository.LogPopulationSelectedAsync"/> requires:
    /// the Delphi logs and swallows the failure (<c>EPR.VclFrame.Populations.pas:224-226</c>) and it
    /// must never block or surface.
    /// </remarks>
    private void LogPopulationSelected(Population population, TimeSpan elapsed)
    {
        if (_session.Current is not { } current)
        {
            return;
        }

        _ = _populationRepository
            .LogPopulationSelectedAsync(current.StudyId, population.ProcId, population.Title, (long)elapsed.TotalMilliseconds)
            .ContinueWith(
                task => _logger.LogWarning(task.Exception, "Could not write the population audit row."),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
    }

    /// <summary>
    /// Unticks everything, then ticks each stored collector by name. Delphi
    /// <c>cbDataCollector.CheckAll(cbUnchecked)</c> plus <c>TryFindCollector</c>.
    /// </summary>
    /// <param name="selection">The package being replayed.</param>
    /// <returns>A task that completes once every unknown name has been reported.</returns>
    /// <remarks>
    /// <c>TryFindCollector</c> accepts the collector's <b>name or its title</b>, case-insensitively
    /// (<c>SameText</c>), and takes the first list entry that matches either
    /// (<c>MainQuickStat.pas:716-732</c>). That leniency is how the registry's name/title collisions
    /// can corrupt a replay, but it is also what lets packages written before a rename still open,
    /// so it is reproduced. <see cref="StringComparison.OrdinalIgnoreCase"/> stands in for
    /// <c>SameText</c>: collector names are ASCII identifiers, and an ordinal fold cannot change
    /// under a locale change.
    /// </remarks>
    private async Task ApplyCollectorSelectionAsync(PackagedSelection selection)
    {
        foreach (DataElementViewModel element in _collections.DataElements)
        {
            element.IsChecked = false;
        }

        foreach (string name in selection.CollectorNames)
        {
            if (TryFindCollector(name) is { } element)
            {
                element.IsChecked = true;

                continue;
            }

            _logger.LogWarning("Package {RowId} names data element \"{Name}\", which does not exist.", selection.RowId, name);

            await _notifier
                .WarnAsync(string.Format(CultureInfo.CurrentCulture, UnknownCollectorFormat, name))
                .ConfigureAwait(true);
        }
    }

    private DataElementViewModel? TryFindCollector(string name)
    {
        foreach (DataElementViewModel element in _collections.DataElements)
        {
            if (string.Equals(name, element.Name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, element.Title, StringComparison.OrdinalIgnoreCase))
            {
                return element;
            }
        }

        return null;
    }

    /// <summary>
    /// The save half of decision (l): show the modal, build the package, store it, reload.
    /// </summary>
    /// <returns>A task that completes when the list has been refreshed.</returns>
    /// <remarks>
    /// Delphi <c>actSaveDataPackageExecute</c>. Two things it does that are gone: the
    /// <c>Guard.CheckNotNull(fGridPopulation)</c> assertion, which is now
    /// <c>DatasetViewModel.CanSaveDataPackage</c> and greys the menu item instead of crashing; and
    /// re-walking the check list for the ticked names, which the workspace already projects. The
    /// stored order is not this list's order - <c>Report.QuickStat.DataElements</c> is written
    /// sorted and de-duplicated by <c>QuickStat.Core</c>, as the Delphi's <c>TStringList</c> was.
    /// </remarks>
    private async Task SaveDataPackageAsync()
    {
        if (_workspace.Population is not { } population)
        {
            _logger.LogWarning("Package dataset specification was requested with no population loaded.");

            return;
        }

        if (_session.Current is not { } current)
        {
            _logger.LogWarning("Package dataset specification was requested with no open session.");

            return;
        }

        IReadOnlyList<string> collectorNames = _workspace.CheckedCollectorNames;

        try
        {
            SaveSpecRequest request = new();

            // Outside the busy scope on purpose: this blocks on a modal window, and marking the shell
            // busy for its duration would put the wait cursor and the overlay behind the dialog the
            // user is typing into.
            SaveSpecRequested?.Invoke(this, request);

            if (!request.Accepted)
            {
                return;
            }

            using IDisposable operation = _progress.BeginOperation(SaveSpecViewModel.SaveSpecificationHeader);

            PackagedSelection saved = await _repository.SaveAsync(new PackagedSelection
            {
                StudyId = current.StudyId,
                PopulationId = population.ProcId,
                Title = request.Title,
                Comment = request.Comment,
                CollectorNames = collectorNames,
            }).ConfigureAwait(true);

            _logger.LogInformation(
                "Saved package {RowId} \"{Title}\" with {ElementCount} data elements.",
                saved.RowId,
                saved.Title,
                saved.CollectorNames.Count);

            await ReloadAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            await ReportFailureAsync(exception, "Could not save the packaged dataset.").ConfigureAwait(true);
        }
    }

    private void OnSaveDataPackageRequested(object? sender, EventArgs e) =>
        _ = SaveDataPackageAsync();

    /// <summary>Reloads the list whenever a project is connected or disconnected.</summary>
    /// <remarks>
    /// Delphi <c>AfterLogin</c>, which calls <c>LoadPackagedSelections</c> after
    /// <c>fGrid.Data.PrepareStudy</c> has established the study id
    /// (<c>MainQuickStat.pas:470-488</c>). <see cref="ISessionService.SessionChanged"/> can arrive on
    /// any thread and <see cref="Packages"/> is bound, so it goes through the dispatcher.
    /// </remarks>
    private void OnSessionChanged(object? sender, SessionContext? session)
    {
        _ = session;

        _dispatcher.Post(() => _ = ReloadAsync());
    }

    private async Task ReportFailureAsync(Exception exception, string headline)
    {
        _logger.LogError(exception, "{Headline}", headline);

        _progress.Fail(exception.Message);

        await _notifier.ErrorAsync(headline + Environment.NewLine + Environment.NewLine + exception.Message)
            .ConfigureAwait(true);
    }
}

/// <summary>
/// The argument of <see cref="PackagesTabViewModel.SaveSpecRequested"/>: what the
/// <c>Save specification</c> modal is asked, and what it answered.
/// </summary>
/// <remarks>
/// A mutable request object rather than a service, so that showing a <c>Window</c> stays in a view
/// and the save path can still be driven end to end from a unit test. Left untouched it means
/// <em>cancel</em>, which is what a host with no window must answer.
/// </remarks>
public sealed class SaveSpecRequest
{
    /// <summary>
    /// The only header the dialog is ever given. Delphi <c>TXT_SAVE_SPEC</c>.
    /// </summary>
    /// <remarks>
    /// The second one, <c>Save selection</c>, belongs to <c>actSavePatientSelection</c>, which is
    /// bound to no menu item, button or toolbar and is not ported - decision (e).
    /// </remarks>
    public string Header => SaveSpecViewModel.SaveSpecificationHeader;

    /// <summary>Whether the user pressed <c>OK</c>. <see langword="false"/> means do nothing at all.</summary>
    public bool Accepted { get; set; }

    /// <summary>What the user typed into <c>Unique name</c>.</summary>
    public string Title { get; set; } = "";

    /// <summary>What the user typed into <c>Comments</c>.</summary>
    public string Comment { get; set; } = "";
}
