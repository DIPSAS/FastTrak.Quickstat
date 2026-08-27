using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using QuickStat.Data;
using QuickStat.Diagnostics;
using QuickStat.Domain.Matrix;
using QuickStat.Domain.Populations;
using QuickStat.Services;

namespace QuickStat.ViewModels;

/// <summary>The embedded population picker: filter box, two check boxes, the list, the SQL preview.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.2.</b> Delphi <c>TfrmPopulations</c> (<c>EPR.VclFrame.Populations.pas</c>) plus
/// the half of <c>TfrmQuickStat.AfterPopulationSelect</c> / <c>LoadPopulationIntoGrid</c>
/// (<c>MainQuickStat.pas:521-575</c>) that the frame drove through its observer list.
/// <c>05-ui-spec.md</c> §B.1.1.
/// </para>
/// <para>
/// <b>The prepare sequence is a contract, and it is <see cref="PopulationLoader"/>'s.</b>
/// <see cref="PersonMatrix"/> raises no notifications, so <see cref="IShellWorkspace"/> cannot
/// observe it and the order is what makes <see cref="IShellWorkspace.HasPopulation"/> read the new
/// cohort rather than the previous one. That order used to be written out twice - here and in
/// step 3.4's package replay - which is PORT-PLAN.md §8.10 (b); it is now written once, in the
/// loader, and this view-model contributes only what is its own: the busy scope, the reporting and
/// the audit row.
/// </para>
/// </remarks>
public sealed partial class PopulationPickerViewModel : ObservableObject, IDisposable
{
    /// <summary>Placeholder in the filter box. Overwritten in English at run time by the Delphi.</summary>
    public const string FilterPlaceholder = "Type filter text here";

    /// <summary>Label above the filter box.</summary>
    public const string FilterHeader = "Filter / search text";

    /// <summary>Caption of the left-hand check box.</summary>
    public const string FrequentlyUsedCaption = "Frequently used only";

    /// <summary>Caption of the right-hand check box, which sits to the <em>left</em> of its box.</summary>
    public const string SimplifiedCaption = "Simplified";

    /// <summary>Empty state before a database has been chosen. This is what a first run shows.</summary>
    /// <remarks>
    /// <b>Addition</b>, flagged. The VCL hides the whole list when it has nothing to show
    /// (<c>Emetra.VclComp.ListView.pas:524</c>, <c>Visible := FLocalList.Count &gt; 0</c>), leaving a
    /// blank rectangle with no explanation; <c>05-ui-spec.md</c> §B.1.1 asks for a message instead
    /// and does not say what it should read. Three of them rather than one, because a single
    /// sentence would be untrue in two of the three states it has to cover.
    /// </remarks>
    public const string NoDatabaseText = "No database is selected. Choose one above.";

    /// <summary>Empty state when a study is connected and its catalogue is empty.</summary>
    /// <remarks>See <see cref="NoDatabaseText"/>; same addition, same reason.</remarks>
    public const string NoPopulationsText = "This database has no populations.";

    /// <summary>Empty state when the catalogue is not empty but the filter excludes everything.</summary>
    /// <remarks>See <see cref="NoDatabaseText"/>; same addition, same reason.</remarks>
    public const string NoMatchesText = "No populations match the filter.";

    private readonly IPopulationRepository _catalogue;
    private readonly PopulationLoader _loader;
    private readonly ISessionService _session;
    private readonly IShellWorkspace _workspace;
    private readonly IShellProgress _progress;
    private readonly IUiDispatcher _dispatcher;
    private readonly IUserNotifier _notifier;
    private readonly ILogger<PopulationPickerViewModel> _logger;

    private CancellationTokenSource? _catalogueLoad;
    private string _foldedFilter = "";
    private bool _disposed;

    [ObservableProperty]
    private string _filterText = "";

    [ObservableProperty]
    private bool _frequentlyUsedOnly;

    [ObservableProperty]
    private bool _simplified;

    [ObservableProperty]
    private bool _canFilterFrequentlyUsed;

    [ObservableProperty]
    private PopulationViewModel? _selectedPopulation;

    [ObservableProperty]
    private string _sqlPreview = "";

    [ObservableProperty]
    private bool _isSqlPreviewVisible;

    /// <summary>Creates the picker's view-model.</summary>
    /// <param name="catalogue">The population catalogue and the selection audit.</param>
    /// <param name="loader">
    /// Runs a population and puts its cohort in the matrix. The whole of the load sequence, shared
    /// with the package replay - PORT-PLAN.md §8.10 (b). It is what used to be
    /// <c>IPatientRepository</c> plus <c>IQueryParameterResolver</c> plus thirty lines of ordering
    /// here, and taking it rather than those two is deliberate: this view-model no longer holds the
    /// pieces from which a second copy of the sequence could be assembled.
    /// </param>
    /// <param name="session">Study and database version; the source of the reload trigger.</param>
    /// <param name="workspace">Cross-tab state: the matrix and the loaded population.</param>
    /// <param name="progress">Status line and busy flag.</param>
    /// <param name="dispatcher">Marshals <c>SessionChanged</c>, which arrives off the UI thread.</param>
    /// <param name="notifier">Reports failures to the user.</param>
    /// <param name="logger">Log.</param>
    public PopulationPickerViewModel(
        IPopulationRepository catalogue,
        PopulationLoader loader,
        ISessionService session,
        IShellWorkspace workspace,
        IShellProgress progress,
        IUiDispatcher dispatcher,
        IUserNotifier notifier,
        ILogger<PopulationPickerViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(catalogue);
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(notifier);
        ArgumentNullException.ThrowIfNull(logger);

        _catalogue = catalogue;
        _loader = loader;
        _session = session;
        _workspace = workspace;
        _progress = progress;
        _dispatcher = dispatcher;
        _notifier = notifier;
        _logger = logger;

        // The ListBox binds to Populations, so WPF uses this very view; filtering it here is what
        // makes the list filter without the collection itself changing (05-ui-spec.md §H.2).
        PopulationsView = CollectionViewSource.GetDefaultView(Populations);
        PopulationsView.Filter = MatchesFilter;

        Populations.CollectionChanged += OnPopulationsChanged;

        _canFilterFrequentlyUsed = session.Current is { StudyId: > 0 };

        _session.SessionChanged += OnSessionChanged;
    }

    /// <summary>The catalogue, in stored-procedure order.</summary>
    /// <remarks>
    /// Populations are <b>not</b> sorted by the client (<c>05-ui-spec.md</c> §G.5); they arrive in
    /// the order <c>Populations.GetStudyPopulations</c> returns them.
    /// </remarks>
    public ObservableCollection<PopulationViewModel> Populations { get; } = [];

    /// <summary>The filtered projection the list actually shows.</summary>
    /// <remarks>
    /// The default view of <see cref="Populations"/>, so a <c>ListBox</c> bound straight to the
    /// collection picks it up. Exposed because the empty state and the tests read it.
    /// </remarks>
    public ICollectionView PopulationsView { get; }

    /// <summary>Whether the list has nothing to show, so the empty state takes its place.</summary>
    public bool IsEmpty => PopulationsView.IsEmpty;

    /// <summary>What the empty state says. See <see cref="NoDatabaseText"/>.</summary>
    public string EmptyStateText => (_session.Current, Populations.Count) switch
    {
        (null, _) => NoDatabaseText,
        (_, 0) => NoPopulationsText,
        _ => NoMatchesText,
    };

    /// <summary>Whether a catalogue load is in flight.</summary>
    internal bool IsLoadingCatalogue => _catalogueLoad is not null;

    /// <summary>
    /// Grants or revokes the <c>FUNC_POPULATION_SOURCE</c> right that gates the SQL preview.
    /// </summary>
    /// <param name="granted">Whether the user may see a population's <c>CREATE PROCEDURE</c> text.</param>
    /// <remarks>
    /// <para>
    /// <b>Decision, recorded.</b> <c>05-ui-spec.md</c> §I.9 leaves the access-control plumbing out of
    /// scope and there is no <c>IAccessControl</c> in the port, so the port takes the
    /// <em>registered</em> default and nothing else:
    /// <c>AManager.AddFunctionPoint(FUNC_POPULATION_SOURCE, asDenied)</c>
    /// (<c>EPR.VclFrame.Populations.pas:170</c>). The pane is therefore hidden, exactly as it is for
    /// a user who holds no rights today. Showing SQL to everyone because the gate has not been built
    /// yet would be the one failure mode a deny-by-default right exists to prevent.
    /// </para>
    /// <para>
    /// This method is the whole of the seam: whoever adds access control calls it, as
    /// <c>AfterAccessControlChanged</c> (<c>:173-177</c>) does - which also clears the pane's text,
    /// reproduced here.
    /// </para>
    /// </remarks>
    public void SetSourceCodeAccess(bool granted)
    {
        SqlPreview = "";
        IsSqlPreviewVisible = granted;
    }

    /// <summary>
    /// Finds a population by <c>ProcId</c>, selects it, and loads its cohort into the grid.
    /// </summary>
    /// <param name="procId">The <c>ProcId</c> stored in a packaged selection.</param>
    /// <param name="cancellationToken">Cancels the load.</param>
    /// <returns><see langword="false"/> when no population in the catalogue has that id.</returns>
    /// <remarks>
    /// <para>
    /// Delphi <c>TfrmPopulations.TrySelect(AProcId, ALoadIt := true, …)</c>
    /// (<c>EPR.VclFrame.Populations.pas:186-200</c>), which exists for one caller: the package replay
    /// (<c>MainQuickStat.pas:789</c>).
    /// </para>
    /// <para>
    /// <b>The replay does not, in the end, come through here</b>, and an earlier revision of this
    /// comment claimed it did.
    /// <see cref="QuickStat.ViewModels.PackagesTabViewModel.OpenPackageCommand"/> has to tell
    /// "this study has no such population" (which warns and stops) apart from "the population is
    /// there but its placeholders would not resolve" (which is quiet), it words its failures around
    /// the <em>package</em> rather than the population, and it writes the audit row fire-and-forget
    /// inside a busy scope it opened itself - so it does its own lookup and its own reporting. What
    /// it must not do, and no longer does, is repeat the load sequence: both paths run
    /// <see cref="PopulationLoader.LoadAsync"/>, which is PORT-PLAN.md §8.10 (b). This overload
    /// remains the answer to "load the population with this <c>ProcId</c>", it selects the row as
    /// <c>TrySelect</c> does, and it is awaitable because a replay has to know the cohort is in the
    /// grid before it starts collecting.
    /// </para>
    /// <para>
    /// <b>Two deliberate differences from the Delphi, both reported.</b>
    /// </para>
    /// <para>
    /// First, the lookup is against the whole catalogue, not the filtered view. The Delphi looks the
    /// population up in the full list and then asks the <em>list view</em> to select it
    /// (<c>Emetra.VclComp.ListView.pas:245-261</c>, which searches <c>FLocalList</c>), so replaying
    /// a package while a filter is typed fails and reports
    /// <c>The selection is based on an unknown population</c> - a message that is simply untrue.
    /// </para>
    /// <para>
    /// Second, it does <b>not</b> switch to the <c>Collections</c> tab. In the Delphi it does, because
    /// <c>TrySelect(..., ALoadIt := true, ...)</c> goes through <c>PopulationRequested</c> and
    /// therefore through <c>AfterPopulationSelect</c>, whose last act is
    /// <c>pgSelections.ActivePage := tbsDataElements</c> (<c>MainQuickStat.pas:541</c>).
    /// <c>Docs/Port/07-ui-contracts.md</c> §3.1 states the opposite and instructs step 3.4 not to
    /// request the tab; this follows the instruction and flags the discrepancy rather than resolving
    /// it unilaterally. Reversal is one call to
    /// <see cref="IShellWorkspace.RequestCollectionsTab"/>.
    /// </para>
    /// </remarks>
    public async Task<bool> TryLoadPopulationAsync(int procId, CancellationToken cancellationToken = default)
    {
        PopulationViewModel? found = null;

        foreach (PopulationViewModel candidate in Populations)
        {
            if (candidate.ProcId == procId)
            {
                found = candidate;

                break;
            }
        }

        if (found is null)
        {
            return false;
        }

        SelectedPopulation = found;

        await LoadAsync(found, activateCollectionsTab: false, cancellationToken).ConfigureAwait(true);

        return true;
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
        Populations.CollectionChanged -= OnPopulationsChanged;

        // Not disposed here: RunCatalogueLoadAsync owns it and will.
        Interlocked.Exchange(ref _catalogueLoad, null)?.Cancel();
    }

    /// <summary>
    /// Reloads the catalogue for the current session. <c>ReadPopulationList</c>.
    /// </summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>A task that completes when the list has been replaced.</returns>
    /// <remarks>
    /// Delphi <c>TfrmPopulations.ReadPopulationList</c>
    /// (<c>EPR.VclFrame.Populations.pas:239-255</c>): clears the current population, queries, and on
    /// failure logs the message at <c>ltException</c> - which in the Delphi is also a modal dialog,
    /// so this notifies.
    /// </remarks>
    internal async Task ReloadCatalogueAsync(CancellationToken cancellationToken = default)
    {
        // fCurrentPopulation := nil, before the query rather than after it.
        SelectedPopulation = null;

        SessionContext? session = _session.Current;

        if (session is null)
        {
            Populations.Clear();

            return;
        }

        try
        {
            IReadOnlyList<Population> loaded = await _catalogue
                .GetPopulationsAsync(
                    session.StudyId,
                    session.Database.DbVersion,
                    FrequentlyUsedOnly,
                    cancellationToken)
                .ConfigureAwait(true);

            Populations.Clear();

            foreach (Population population in loaded)
            {
                Populations.Add(new PopulationViewModel(population));
            }

            ApplyExpansion();

            _logger.LogInformation("Found {Count} populations for study {StudyId}.", loaded.Count, session.StudyId);
        }
        catch (OperationCanceledException)
        {
            // A newer load replaced this one, or the shell is closing. Leave the list alone.
        }
        catch (Exception exception)
        {
            Populations.Clear();

            _progress.Fail(exception.Message);

            _logger.LogError(exception, "Could not read the population catalogue.");

            await _notifier.ErrorAsync(exception.Message).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// Double click, or <c>Enter</c>: run the population and load the cohort into the grid.
    /// </summary>
    /// <param name="cancellationToken">Cancels the load.</param>
    /// <returns>A task that completes when the grid holds the new cohort.</returns>
    /// <remarks>
    /// <para>
    /// Delphi <c>PopulationRequested</c> (<c>EPR.VclFrame.Populations.pas:207-226</c>) driving
    /// <c>TfrmQuickStat.AfterPopulationSelect</c> (<c>MainQuickStat.pas:521-550</c>) and
    /// <c>LoadPopulationIntoGrid</c> (<c>:554-575</c>).
    /// </para>
    /// <para>
    /// <b>Ordering matters, and the order is <see cref="PopulationLoader.LoadAsync"/>'s</b> - clear
    /// (which unlocks, because <see cref="PersonMatrix.SortBy"/> throws on a locked matrix and a
    /// collect run locks it), sort, prepare, and only then
    /// <see cref="IShellWorkspace.SetPopulation"/>, with the national-id recovery between the cohort
    /// query and <see cref="PersonMatrix.PreparePopulation"/> because <c>PreparePopulation</c> copies
    /// the ids onto the rows it builds and never reads the patients again. All of it is reasoned out
    /// there, against <c>MainQuickStat.pas:532</c>, <c>:536-540</c> and <c>:564-566</c>; it is stated
    /// once because it used to be stated twice (PORT-PLAN.md §8.10 (b)).
    /// </para>
    /// <para>
    /// What is left here is what the package replay does <em>differently</em>: this path opens its
    /// own busy scope under the population's title, reports an unresolvable placeholder to the user,
    /// asks for the <c>Collections</c> tab, and awaits the audit row.
    /// </para>
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanPreparePopulation))]
    private async Task PreparePopulationAsync(CancellationToken cancellationToken)
    {
        if (SelectedPopulation is not { } selected)
        {
            // ERR_POPULATION_NOT_SELECTED (MainQuickStat.pas:545). Unreachable through the command,
            // whose CanExecute already requires a selection, but the Delphi guards it too.
            _logger.LogError("A population was requested while nothing was selected.");

            return;
        }

        await LoadAsync(selected, activateCollectionsTab: true, cancellationToken).ConfigureAwait(true);
    }

    private bool CanPreparePopulation() => SelectedPopulation is not null;

    private async Task LoadAsync(
        PopulationViewModel selected,
        bool activateCollectionsTab,
        CancellationToken cancellationToken)
    {
        Population population = selected.Population;

        // The status line is the population's own title, in the same spirit as the collect run's
        // "<collector title>" (05-ui-spec.md §G.6). The Delphi sets no text here at all - it only
        // raises a crSqlWait cursor - but BeginOperation is what carries the busy flag that replaces
        // that cursor (§G.3), and it needs a line.
        using IDisposable operation = _progress.BeginOperation(population.Title);

        try
        {
            // Placeholders, cohort, national ids, matrix, workspace - PopulationLoader.LoadAsync,
            // which the package replay runs too.  The one outcome it does not report is an
            // unresolved parameter set, because the two callers word that differently; everything
            // else it can fail at throws, and lands in the catch blocks below.
            PopulationLoadResult result = await _loader
                .LoadAsync(population, _logger, cancellationToken)
                .ConfigureAwait(true);

            if (result.Unresolved is { } resolution)
            {
                await ReportUnresolvedParametersAsync(population, resolution).ConfigureAwait(true);

                return;
            }

            if (activateCollectionsTab)
            {
                _workspace.RequestCollectionsTab();
            }

            _logger.LogInformation(
                "Loaded population {ProcId} '{Title}': {RowCount} patients in {Elapsed} ms.",
                population.ProcId,
                population.Title,
                result.RowCount,
                result.ElapsedMilliseconds);

            // Unconditional, and outside the observers' failure path. The Delphi wrote it after the
            // observers inside the same try, so a grid failure lost the audit row that feeds the
            // "frequently used" ranking (IPopulationRepository.LogPopulationSelectedAsync).
            //
            // Awaited, with the token, and inside this try - unlike the replay, which fires it and
            // forgets it.  That is a real difference and it is left alone: it means a study id of
            // zero still writes a row here, and that a failing audit write is reported as a failed
            // load even though the cohort is already on screen.
            await _catalogue
                .LogPopulationSelectedAsync(
                    _session.Current?.StudyId ?? 0,
                    population.ProcId,
                    population.Title,
                    result.ElapsedMilliseconds,
                    cancellationToken)
                .ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            _progress.Reset();
        }
        catch (Exception exception)
        {
            // Deliberate change, flagged. The Delphi swallows this into fLog.SilentWarning
            // (EPR.VclFrame.Populations.pas:220-226), so a population that cannot run looks like one
            // that returned nothing. PORT-PLAN.md §7.2 asks for the opposite - "fail loudly".
            _progress.Fail(exception.Message);

            _logger.LogError(
                exception,
                "Could not load population {ProcId} '{Title}'.",
                population.ProcId,
                population.Title);

            await _notifier.ErrorAsync(exception.Message).ConfigureAwait(true);
        }
    }

    private async Task ReportUnresolvedParametersAsync(Population population, ParameterResolution resolution)
    {
        if (resolution.CancelledByUser)
        {
            // Not an error and must show nothing: ParameterResolution exists to tell these apart.
            _logger.LogInformation(
                "The user cancelled the period prompt for population {ProcId}.",
                population.ProcId);

            _progress.Reset();

            return;
        }

        string message = resolution.FailureReason ?? "The population's parameters could not be resolved.";

        _progress.Fail(message);

        _logger.LogError(
            "Could not resolve the parameters of population {ProcId} '{Title}': {Reason}",
            population.ProcId,
            population.Title,
            message);

        await _notifier.ErrorAsync(message).ConfigureAwait(true);
    }

    /// <summary>
    /// The list filter. Lowercase both sides in the current culture, then an <b>ordinal</b>
    /// substring test.
    /// </summary>
    /// <param name="item">A <see cref="PopulationViewModel"/>.</param>
    /// <returns>Whether the row stays visible.</returns>
    /// <remarks>
    /// PORT-PLAN.md §8.8 (i), from <c>Emetra.VclComp.ListView.pas:353-362, 482-518</c>:
    /// <c>FilterCase = fcLower</c> lowercases the filter, <c>AfterUpdate</c> lowercases
    /// <c>AsListBox(false)</c>, and the test is Delphi <c>Pos</c> - not a collation. The filter is
    /// <b>not</b> trimmed and an empty one matches everything. <c>StringComparison.Ordinal</c>, never
    /// <c>CurrentCultureIgnoreCase</c>, which folds more than <c>Pos</c> does.
    /// </remarks>
    private bool MatchesFilter(object item)
    {
        if (_foldedFilter.Length == 0)
        {
            return true;
        }

        return item is PopulationViewModel population
            && population.SearchText
                .ToLower(CultureInfo.CurrentCulture)
                .Contains(_foldedFilter, StringComparison.Ordinal);
    }

    /// <summary>
    /// Applies <c>ExpandRow</c> to every row: all of them, or only the selected one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Emetra.VclComp.ListView.pas:752-755</c>. <b>One deliberate difference:</b> the VCL grid
    /// initialises <c>FSimpleView := true</c> (<c>:283</c>) while <c>cbSimpleView</c> starts
    /// <em>unticked</em> (<c>EPR.VclFrame.Populations.dfm</c>), so a freshly loaded list shows
    /// collapsed rows under an unticked <c>Simplified</c> box until the first keystroke or click
    /// re-synchronises the two. The port follows the check box, which is what §B.1.1 describes.
    /// </para>
    /// </remarks>
    private void ApplyExpansion()
    {
        foreach (PopulationViewModel population in Populations)
        {
            population.IsExpanded = !Simplified || ReferenceEquals(population, SelectedPopulation);
        }
    }

    private void OnSessionChanged(object? sender, SessionContext? session)
    {
        // SessionService raises this from wherever the last login step's continuation ran, which is
        // a thread-pool thread; everything below touches bound collections.
        _dispatcher.Post(() =>
        {
            // cbShowCommon.Enabled := Sender.StudyId > 0 (EPR.VclFrame.Populations.pas:146).
            CanFilterFrequentlyUsed = session is { StudyId: > 0 };

            // EmptyStateText distinguishes "no database" from "no populations", and the collection
            // raises nothing when it was empty before and is empty after.
            OnPropertyChanged(nameof(EmptyStateText));

            BeginCatalogueLoad();
        });
    }

    /// <summary>
    /// Starts a catalogue load, abandoning whichever one was already running.
    /// </summary>
    /// <remarks>
    /// Both triggers - a session change and the <c>Frequently used only</c> box - are events with
    /// nowhere to return a task to, and each is a server round trip. Cancelling the previous one
    /// keeps a fast series of clicks from racing an older answer into the list.
    /// </remarks>
    private void BeginCatalogueLoad()
    {
        CancellationTokenSource started = new();

        Interlocked.Exchange(ref _catalogueLoad, started)?.Cancel();

        _ = RunCatalogueLoadAsync(started);
    }

    private async Task RunCatalogueLoadAsync(CancellationTokenSource owner)
    {
        try
        {
            await ReloadCatalogueAsync(owner.Token).ConfigureAwait(true);
        }
        finally
        {
            // Each source is disposed exactly once, by the run that owns it, and only clears the
            // field when it is still the current one.
            Interlocked.CompareExchange(ref _catalogueLoad, null, owner);

            owner.Dispose();
        }
    }

    private void OnPopulationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyStateText));
    }

    partial void OnFilterTextChanged(string value)
    {
        // Not trimmed: PORT-PLAN.md §8.8 (i). The packages filter is the one that trims.
        _foldedFilter = value.ToLower(CultureInfo.CurrentCulture);

        PopulationsView.Refresh();

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(EmptyStateText));
    }

    partial void OnFrequentlyUsedOnlyChanged(bool value)
    {
        _ = value;

        // Not a client-side filter: cbShowCommon.OnClick is ReadPopulationList, which swaps to
        // Populations.GetPopularPopulations (EPR.VclFrame.Populations.pas:128, 246).
        BeginCatalogueLoad();
    }

    partial void OnSimplifiedChanged(bool value)
    {
        _ = value;

        ApplyExpansion();
    }

    partial void OnSelectedPopulationChanged(PopulationViewModel? value)
    {
        ApplyExpansion();

        // PopulationSelected (EPR.VclFrame.Populations.pas:228-237) fills the pane on a single
        // click, normalising every line break to CRLF, and blanks it when nothing is highlighted.
        // The Delphi fills the memo whether or not the pane is visible, and gates only the pane.
        SqlPreview = value is null ? "" : NormaliseLineBreaks(value.Population.SourceCode);

        PreparePopulationCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Turns every line break into CRLF, exactly as the Delphi's double replace does.</summary>
    /// <param name="text">The stored <c>CREATE PROCEDURE</c> text.</param>
    /// <returns>The same text with CRLF line breaks.</returns>
    /// <remarks>
    /// <c>StringReplace(StringReplace(SourceCode, #13#10, #10, …), #10, #13#10, …)</c>
    /// (<c>EPR.VclFrame.Populations.pas:234</c>). Written out rather than as
    /// <c>ReplaceLineEndings</c>, which would also fold the vertical tab, the form feed and the three
    /// Unicode separators the Delphi leaves alone.
    /// </remarks>
    private static string NormaliseLineBreaks(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\n", "\r\n", StringComparison.Ordinal);
}
