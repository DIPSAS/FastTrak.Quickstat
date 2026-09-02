using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using QuickStat.Collectors;
using QuickStat.Data;
using QuickStat.Diagnostics;
using QuickStat.Domain.Anonymisation;
using QuickStat.Domain.Matrix;
using QuickStat.Services;

namespace QuickStat.ViewModels;

/// <summary>The <c>Collections</c> tab: the data-element list, <c>Collect data</c>, export options.</summary>
/// <remarks>
/// <para>
/// <c>05-ui-spec.md</c> §B.2, §D.1, §G.3-§G.6. Step 3.3 owns this.
/// </para>
/// <para>
/// It is the port of three Delphi routines: <c>AfterLogin</c> (<c>MainQuickStat.pas:471-493</c>)
/// fills the list, <c>ValidateCollectorSelection</c> (<c>:690-713</c>) decides whether
/// <c>Collect data</c> is enabled, and <c>actCollectDataExecute</c> (<c>:633-681</c>) is the run
/// itself.
/// </para>
/// <para>
/// Of <c>AfterLogin</c> it keeps only the two list operations - the <c>Clear</c> and the fill. The
/// <c>fQuickStat.PrepareStudy</c> that supplies them is awaited by
/// <see cref="IConnectionCoordinator.ConnectAsync"/> instead (PORT-PLAN.md §8.10 (g)), so this tab
/// reacts to <see cref="ICollectorRegistry.Rebuilt"/> and never starts a query of its own.
/// </para>
/// <para>
/// <b>The order of <see cref="DataElements"/> is the column order of every export</b> - PORT-PLAN.md
/// §6 - because the run walks the list from index 0 and the matrix appends columns in the order it
/// is given them. See <see cref="DataElementViewModel.TitleOrder"/> for the rule and for why it is
/// not <see cref="StringComparer.Ordinal"/>.
/// </para>
/// </remarks>
public sealed partial class CollectionsTabViewModel : ObservableObject, IDisposable
{
    /// <summary>Teal header above the list.</summary>
    public const string ElementsHeader = "Select data elements";

    /// <summary>Teal header above the radio group.</summary>
    public const string ExportOptionsHeader = "Export options";

    /// <summary>
    /// The wrapped paragraph above the list, verbatim - <b>including the two spaces after
    /// <c>process.</c></b>
    /// </summary>
    public const string InfoParagraph =
        "Select data elements from the list below, and click \"Collect data\" at the bottom to start "
        + "the process.  Depending on what you select, this will take some time!";

    /// <summary>Label above the filter box.</summary>
    /// <remarks>
    /// <para>
    /// <b>ADDITION, on the product owner's request of 2026-09-02: the check list gets the same
    /// filter the population list has.</b> The Delphi has nothing of the kind here -
    /// <c>cbDataCollector</c> is a bare <c>TCheckListBox</c> - and with 530 data elements on the
    /// largest study in the field, finding one means scrolling. PORT-PLAN.md §7.3.
    /// </para>
    /// <para>
    /// The wording is <see cref="PopulationPickerViewModel.FilterHeader"/>'s, verbatim, and so is
    /// <see cref="FilterPlaceholder"/>: the two boxes sit two tabs apart in one window, and a user
    /// who has learnt one should recognise the other. They are not shared constants, because that
    /// one is an English caption <c>TfrmQuickStat.FormCreate</c> writes over a Norwegian
    /// <c>.dfm</c> at run time (<c>MainQuickStat.pas:289-292</c>) and this one is ours; if the
    /// Delphi's ever changes, only its own should follow.
    /// </para>
    /// </remarks>
    public const string FilterHeader = "Filter / search text";

    /// <summary>Placeholder inside the filter box. See <see cref="FilterHeader"/>.</summary>
    public const string FilterPlaceholder = "Type filter text here";

    /// <summary>Shown in the list's place when the filter excludes every data element.</summary>
    /// <remarks>
    /// One state, not the population picker's three. The other two it distinguishes - no database,
    /// and a database with an empty catalogue - are not reachable here in a way worth a sentence: a
    /// study with no data elements at all does not exist, and before a connection the whole tab is
    /// hidden. What <em>is</em> reachable, and looks exactly like a defect without a message, is
    /// typing a filter that matches nothing.
    /// </remarks>
    public const string NoMatchesText = "No data elements match the filter.";

    /// <summary>Caption of the tall button at the bottom. Delphi <c>actCollectData</c>.</summary>
    public const string CollectDataCaption = "Collect data";

    /// <summary>First radio. Delphi <c>rbFullIdentification</c>.</summary>
    public const string FullIdentificationCaption = "Fully identified patients";

    /// <summary>Second radio, checked by default. Delphi <c>rbKeepPids</c>.</summary>
    public const string PersonIdOnlyCaption = "Identified with PID only";

    /// <summary>
    /// Third radio. Delphi <c>rbRandomisePids</c>, whose <c>.dfm</c> caption has a trailing space;
    /// dropped here, as §B.2 instructs.
    /// </summary>
    public const string RandomPersonIdCaption = "Generate new random PIDs";

    /// <summary>The timestamp check box. Delphi <c>cbExportDates</c>.</summary>
    public const string ExportTimestampsCaption = "Export timestamp for every data element";

    private readonly IShellWorkspace _workspace;
    private readonly IIdentificationPolicy _identification;
    private readonly ICollectorRegistry _registry;
    private readonly ICollectorRunner _runner;
    private readonly ISessionService _session;
    private readonly IShellProgress _progress;
    private readonly IUiDispatcher _dispatcher;
    private readonly IUserNotifier _notifier;
    private readonly ILogger<CollectionsTabViewModel> _logger;

    [ObservableProperty]
    private DataElementViewModel? _currentlyCollecting;

    /// <summary>
    /// What the user has typed in the filter box, applied on every keystroke.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It hides rows and nothing else.</b> <see cref="DataElements"/> is untouched, so a ticked
    /// element the filter is hiding is still collected, still in check-list order, and still in the
    /// same export column - see <see cref="VisibleDataElements"/>. That is the only defensible
    /// behaviour for a filter over a <em>check</em> list: the alternative is a keystroke silently
    /// dropping a column from an export, which is the class of failure PORT-PLAN.md §6 exists to
    /// prevent.
    /// </para>
    /// <para>
    /// The fold rule is <see cref="PopulationPickerViewModel"/>'s, deliberately: lowercase both
    /// sides in the current culture, then an <b>ordinal</b> substring test, and <b>no trim</b>. Not
    /// the Packages tab's, which trims and folds upward, because that one is a port of
    /// <c>TSpotLightContext</c> and has a Delphi original to answer to. This one has no original at
    /// all, so it copies the box the owner pointed at.
    /// </para>
    /// </remarks>
    [ObservableProperty]
    private string _filterText = "";

    private string _foldedFilter = "";
    private bool _suspendCheckedNotifications;
    private bool _disposed;

    /// <summary>Creates the tab's view-model.</summary>
    /// <param name="workspace">Cross-tab state; owns the timestamp flag and the ticked names.</param>
    /// <param name="identification">The one shared identification mode.</param>
    /// <param name="registry">
    /// Supplies the data elements for the connected study, and says when they have changed.
    /// </param>
    /// <param name="runner">Runs one collector over the cohort.</param>
    /// <param name="session">Tells this tab when to empty the list, and supplies the study id.</param>
    /// <param name="progress">Status line, percentage and the busy flag.</param>
    /// <param name="dispatcher">Marshals a session change onto the user-interface thread.</param>
    /// <param name="notifier">Reports a failed run to the user.</param>
    /// <param name="logger">Log.</param>
    public CollectionsTabViewModel(
        IShellWorkspace workspace,
        IIdentificationPolicy identification,
        ICollectorRegistry registry,
        ICollectorRunner runner,
        ISessionService session,
        IShellProgress progress,
        IUiDispatcher dispatcher,
        IUserNotifier notifier,
        ILogger<CollectionsTabViewModel> logger)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(identification);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(notifier);
        ArgumentNullException.ThrowIfNull(logger);

        _workspace = workspace;
        _identification = identification;
        _registry = registry;
        _runner = runner;
        _session = session;
        _progress = progress;
        _dispatcher = dispatcher;
        _notifier = notifier;
        _logger = logger;

        _identification.ModeChanged += OnIdentificationModeChanged;
        _session.SessionChanged += OnSessionChanged;
        _registry.Rebuilt += OnRegistryRebuilt;
    }

    /// <summary>
    /// Raised immediately before a run walks the list, so the view can remember where the check list
    /// is scrolled to.
    /// </summary>
    /// <remarks>
    /// §G.4. Delphi <c>actCollectDataExecute</c> saves <c>TopIndex</c> and <c>ItemIndex</c> before
    /// the loop and puts both back after it. The port keeps the scroll half and drops the
    /// selection half - the run marks <see cref="DataElementViewModel.IsCollecting"/> rather than
    /// moving <c>SelectedItem</c>, so there is no selection to restore.
    /// </remarks>
    public event EventHandler? CollectRunStarting;

    /// <summary>Raised after a run finishes, however it finished, so the view can scroll back.</summary>
    public event EventHandler? CollectRunFinished;

    /// <summary>
    /// The tickable data elements, ordered by <see cref="DataElementViewModel.TitleOrder"/>.
    /// </summary>
    /// <remarks>
    /// Filled from <see cref="ICollectorRegistry"/> after every successful login, which is Delphi
    /// <c>AfterLogin</c>. <b>This order is the column order of every export</b> (PORT-PLAN.md §6).
    /// </remarks>
    public ObservableCollection<DataElementViewModel> DataElements { get; } = [];

    /// <summary>The subset of <see cref="DataElements"/> the check list actually shows.</summary>
    /// <remarks>
    /// <para>
    /// <b>A projection, and only ever written by <see cref="ApplyFilter"/>.</b> The same
    /// <see cref="DataElementViewModel"/> instances in the same order, minus the ones the filter
    /// excludes - so a tick is a tick whichever collection you reach it through, and there is no
    /// second copy of any state.
    /// </para>
    /// <para>
    /// <b>Nothing that decides what is collected or exported reads this.</b>
    /// <see cref="CollectDataAsync"/> and <see cref="CanCollectData"/> both enumerate
    /// <see cref="DataElements"/>, which is the whole point of filtering a projection rather than
    /// the list: the run walks the check list from index 0 in
    /// <see cref="DataElementViewModel.TitleOrder"/> order whatever the box says, and
    /// <c>Collect data</c> stays enabled when the one ticked element happens to be hidden.
    /// <c>TheFilterHidesRowsWithoutChangingWhatIsCollected</c> pins it.
    /// </para>
    /// <para>
    /// <b>Why not an <c>ICollectionView</c>, which is how the other two filters in this application
    /// are built</b> (<see cref="PopulationPickerViewModel.PopulationsView"/>,
    /// <c>PackagesTabViewModel.PackagesView</c>)? Because a <c>CollectionView</c> captures
    /// <c>Dispatcher.CurrentDispatcher</c> when it is created and throws
    /// <c>NotSupportedException</c> - <i>"does not support changes to its SourceCollection from a
    /// thread different from the Dispatcher thread"</i> - on any change raised elsewhere. This list
    /// is the one that is filled from off the user-interface thread: <c>ICollectorRegistry.Rebuilt</c>
    /// is raised from <em>inside</em> <c>BuildAsync</c>, which is what PORT-PLAN.md §8.10 (g) bought,
    /// and <see cref="OnRegistryRebuilt"/> marshals with <see cref="IUiDispatcher"/>. That is correct
    /// in the running application and it would make this view-model the only one in the shell that a
    /// test cannot drive without a real <c>Dispatcher</c> - which is precisely the property
    /// <c>InlineUiDispatcher</c> exists to preserve. A plain collection has no affinity at all, so it
    /// costs an <c>ObservableCollection</c> and buys back both.
    /// </para>
    /// </remarks>
    public ObservableCollection<DataElementViewModel> VisibleDataElements { get; } = [];

    /// <summary>Whether the filter is hiding every data element there is.</summary>
    /// <remarks>
    /// Deliberately not "the list is empty": before a connection there is nothing to show and
    /// nothing to explain, and a message there would be noise on a tab that is not reachable yet.
    /// See <see cref="NoMatchesText"/>.
    /// </remarks>
    public bool ShowNoMatches => DataElements.Count > 0 && VisibleDataElements.Count == 0;

    /// <summary>
    /// The radio group, bound through <see cref="QuickStat.Converters.EnumToBooleanConverter"/>.
    /// </summary>
    /// <remarks>
    /// A pass-through to <see cref="IIdentificationPolicy"/>, which is the single shared answer for
    /// both the grid and the exporter. Do not add a backing field: a second copy is exactly the
    /// display-versus-export divergence PORT-PLAN.md §7.2 exists to remove.
    /// </remarks>
    public PersonIdentification Identification
    {
        get => _identification.Mode;

        set
        {
            if (_identification.Mode == value)
            {
                return;
            }

            _identification.Mode = value;

            OnPropertyChanged();
        }
    }

    /// <summary>
    /// The <c>Export timestamp for every data element</c> check box.
    /// </summary>
    /// <remarks>
    /// A pass-through to <see cref="IShellWorkspace.ExportTimestamps"/>. The box lives on this tab
    /// but is read by the Dataset tab's two export commands, so the value belongs to the workspace -
    /// see the note about §H.2 in <see cref="IShellWorkspace"/>.
    /// </remarks>
    public bool ExportTimestamps
    {
        get => _workspace.ExportTimestamps;

        set
        {
            if (_workspace.ExportTimestamps == value)
            {
                return;
            }

            _workspace.ExportTimestamps = value;

            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Unticks everything, then ticks the elements a saved package names. Delphi
    /// <c>PreparePackagedSelection</c> (<c>MainQuickStat.pas:794-803</c>).
    /// </summary>
    /// <param name="collectorNames">
    /// <see cref="QuickStat.Domain.Packages.PackagedSelection.CollectorNames"/>, in stored order.
    /// </param>
    /// <returns>The names that matched no data element, in the order they were given.</returns>
    /// <remarks>
    /// <para>
    /// <b>For step 3.4's replay.</b> Ticking is this tab's business - the workspace's checked names
    /// are a projection of <see cref="DataElements"/>, not a second copy - so the replay asks rather
    /// than reaching in. Matching is <see cref="ICollectorRegistry.TryFind"/>, i.e. name
    /// <em>or</em> title, case-insensitively, exactly as <c>TryFindCollector</c> does.
    /// </para>
    /// <para>
    /// The unknown names are returned rather than reported: §D.4's
    /// <c>The selection contains an unknown data element.</c> warning belongs to the Packages tab,
    /// which knows which package it came from. The Delphi raised it once per missing element from
    /// inside the loop (<c>:803</c>); one warning listing all of them is step 3.4's decision to make.
    /// </para>
    /// <para>
    /// The whole update pushes to <see cref="IShellWorkspace.SetCheckedCollectorNames"/> once, not
    /// once per tick.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="collectorNames"/> is <see langword="null"/>.</exception>
    public IReadOnlyList<string> ApplyPackagedSelection(IEnumerable<string> collectorNames)
    {
        ArgumentNullException.ThrowIfNull(collectorNames);

        List<string> unknown = [];

        _suspendCheckedNotifications = true;

        try
        {
            foreach (DataElementViewModel element in DataElements)
            {
                element.IsChecked = false;
            }

            foreach (string collectorName in collectorNames)
            {
                if (_registry.TryFind(collectorName, out ICollector? collector) &&
                    FindByName(collector.Descriptor.Name) is { } element)
                {
                    element.IsChecked = true;
                }
                else
                {
                    unknown.Add(collectorName);
                }
            }
        }
        finally
        {
            _suspendCheckedNotifications = false;
        }

        PublishCheckedCollectors();

        return unknown;
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
        _session.SessionChanged -= OnSessionChanged;
        _registry.Rebuilt -= OnRegistryRebuilt;
    }

    /// <summary>
    /// The collect run. Delphi <c>actCollectDataExecute</c> (<c>MainQuickStat.pas:633-681</c>).
    /// </summary>
    /// <param name="cancellationToken">
    /// The command's own, from <c>AsyncRelayCommand</c>. Linked with the one the overlay's Cancel
    /// button signals, so either stops the run - between collectors and inside one.
    /// </param>
    /// <returns>A task that completes when the run has finished.</returns>
    /// <remarks>
    /// <para>
    /// Clear the variables, walk the list <b>from index 0</b>, and for each ticked element put its
    /// title on the status line and run it into the matrix. Then lock the matrix and announce the
    /// change, in that order - <c>07-ui-contracts.md</c> §3.1.
    /// </para>
    /// <para>
    /// Four things the Delphi does that are gone, each because they moved rather than disappeared:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <c>AddCaptions</c> - the twelve DRUID/DRUG caption records plus <c>LoadCaptions</c> - now
    ///     happens once per connection in <see cref="ICaptionLoader"/>, which is where step 2.5 put
    ///     it and why <see cref="IConnectionCoordinator"/> calls it.
    ///   </description></item>
    ///   <item><description>
    ///     <c>Screen.Cursor := crSqlWait</c> is
    ///     <see cref="IShellProgress.BeginOperation(string, CancellationTokenSource)"/>, which
    ///     counts, so a package replay that wraps this in its own scope still gets one wait cursor
    ///     (§G.3). This is the one call site that passes a source: PORT-PLAN.md §8.10 (c).
    ///   </description></item>
    ///   <item><description>
    ///     <c>actExportData.Enabled := fGrid.Data.HasData</c> is <c>DatasetViewModel.CanExport</c>,
    ///     re-asked by <see cref="IShellWorkspace.NotifyDataChanged"/>. The Delphi never assigns it
    ///     false again; the port does - see that method for the divergence.
    ///   </description></item>
    ///   <item><description>
    ///     <c>UpdateGridInfo</c> is the same notification, which is why it is raised in the
    ///     <c>finally</c>: the Delphi refreshes the caption whether or not the run succeeded.
    ///   </description></item>
    /// </list>
    /// <para>
    /// Progress is the runner's: one report per batch, <c>title (n/m)</c>. The Delphi's own
    /// per-patient percentage (<c>EPR.QA.Matrix.pas:159-160</c>) was also really per batch, since it
    /// only fired when a batch filled.
    /// </para>
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanCollectData))]
    private async Task CollectDataAsync(CancellationToken cancellationToken)
    {
        PersonMatrix matrix = _workspace.Matrix;

        // A snapshot: the collection must not be enumerated while a collector is running, and the
        // cohort is fixed for the whole run.
        List<DataElementViewModel> ticked = [.. DataElements.Where(static element => element.IsChecked)];
        int[] personIds = [.. matrix.Rows.Select(static row => row.PersonId)];
        int studyId = _session.Current?.StudyId ?? matrix.StudyId;

        CollectRunStarting?.Invoke(this, EventArgs.Empty);

        // The one operation in the application that offers the overlay a Cancel button, because it
        // is the one that can take minutes and the one that honours the token all the way down -
        // between collectors here, between batches in CollectorRunner, and inside the statement
        // itself.  Linked rather than replacing: AsyncRelayCommand hands out a token and keeps the
        // source, so this is how the same run can be stopped from either end.
        using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // BeginOperation always sets the status line, and the Delphi's first is the first collector's
        // title.  With nothing ticked there is no run to describe, so the line is left as it is.
        using IDisposable operation = _progress.BeginOperation(
            ticked.Count > 0 ? ticked[0].Title : _progress.Info,
            cancellation);

        try
        {
            matrix.ClearVariables();

            foreach (DataElementViewModel element in ticked)
            {
                cancellation.Token.ThrowIfCancellationRequested();

                await CollectOneAsync(element, matrix, personIds, studyId, cancellation.Token).ConfigureAwait(true);
            }

            matrix.Lock();

            _progress.Done();
        }
        catch (OperationCanceledException)
        {
            // The matrix stays unlocked, so the columns collected so far show but cannot be
            // exported - DatasetViewModel.EnsureExportable.  There is no Delphi equivalent, because
            // the Delphi run could not be interrupted at all; this is where the overlay's Cancel
            // button lands.
            _logger.LogInformation("The collect run was cancelled.");

            _progress.Reset();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "The collect run failed.");

            _progress.Fail(exception.Message);

            await _notifier.ErrorAsync(
                "The data collection failed." + Environment.NewLine + Environment.NewLine + exception.Message)
                .ConfigureAwait(true);
        }
        finally
        {
            CurrentlyCollecting = null;

            // Delphi: UpdateGridInfo is in the finally, so the caption is refreshed even when a
            // collector threw.  After the happy path this is the second half of the ordering
            // contract - Lock, then notify.
            _workspace.NotifyDataChanged();

            CollectRunFinished?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool CanCollectData() => DataElements.Any(static element => element.IsChecked);

    private async Task CollectOneAsync(
        DataElementViewModel element,
        PersonMatrix matrix,
        IReadOnlyList<int> personIds,
        int studyId,
        CancellationToken cancellationToken)
    {
        if (!_registry.TryFind(element.Name, out ICollector? collector))
        {
            // Delphi: `if Supports( Items.Objects[n], IGridDataCollector, ... )` - a list entry that
            // is not a collector is skipped in silence.  Unreachable unless the registry was rebuilt
            // underneath the list.
            _logger.LogWarning("Data element {CollectorName} is ticked but no longer in the registry.", element.Name);

            return;
        }

        CurrentlyCollecting = element;
        element.IsCollecting = true;

        try
        {
            _progress.SetInfo(element.Title);

            CollectorRunSummary summary = await _runner
                .RunAsync(collector, personIds, studyId, matrix, _progress, cancellationToken)
                .ConfigureAwait(true);

            matrix.AddColumns(summary.VariableNames);

            _logger.LogDebug(
                "{CollectorName}: {ColumnCount} columns, {RowsAccepted} values, {BatchCount} batches.",
                summary.Descriptor.Name,
                summary.VariableNames.Count,
                summary.RowsAccepted,
                summary.BatchCount);
        }
        finally
        {
            element.IsCollecting = false;
        }
    }

    private DataElementViewModel? FindByName(string name) =>
        DataElements.FirstOrDefault(element => string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase));

    private void Replace(IEnumerable<ICollector> collectors)
    {
        Clear();

        foreach (ICollector collector in collectors.OrderBy(
                     static collector => collector.Descriptor.Title,
                     DataElementViewModel.TitleOrder))
        {
            DataElements.Add(new DataElementViewModel(
                collector.Descriptor.Name,
                collector.Descriptor.Title,
                OnElementCheckedChanged));
        }

        // The filter text survives a project switch - the population box does the same - so a new
        // study arrives under whatever is already in the box.
        ApplyFilter();

        CollectDataCommand.NotifyCanExecuteChanged();
    }

    private void Clear()
    {
        CurrentlyCollecting = null;

        DataElements.Clear();

        ApplyFilter();

        // Nothing is ticked any more, so neither Collect data nor the Dataset tab's package command
        // may stay enabled.  Delphi AfterLogin calls ValidateCollectorSelection for the same reason.
        _workspace.SetCheckedCollectorNames([]);

        CollectDataCommand.NotifyCanExecuteChanged();
    }

    private void OnElementCheckedChanged(DataElementViewModel element)
    {
        _ = element;

        if (_suspendCheckedNotifications)
        {
            return;
        }

        PublishCheckedCollectors();
    }

    private void PublishCheckedCollectors()
    {
        // In check-list order, which is the order a saved package stores and the order a replay
        // re-ticks in.
        _workspace.SetCheckedCollectorNames(
            DataElements.Where(static element => element.IsChecked).Select(static element => element.Name));

        CollectDataCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Rebuilds <see cref="VisibleDataElements"/> from <see cref="DataElements"/> and the filter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rule is <see cref="PopulationPickerViewModel"/>'s, for the reason given on
    /// <see cref="FilterText"/>: lowercase both sides in the current culture, then
    /// <see cref="StringComparison.Ordinal"/> rather than <c>CurrentCultureIgnoreCase</c>, which is a
    /// collation and folds more than a substring search should. The filter is not trimmed.
    /// </para>
    /// <para>
    /// <b>Only <see cref="DataElementViewModel.Title"/> is matched</b>, where the population rows
    /// match four fields. It is the one thing a row shows, so it is the one thing a user can be
    /// filtering by. <see cref="DataElementViewModel.Name"/> is the persistence key - <c>DRUG_…</c>,
    /// <c>QA_…</c> - and matching it would leave rows standing for a reason nothing on screen
    /// explains.
    /// </para>
    /// <para>
    /// The <c>^ </c> sort prefix is part of the title and therefore searchable, which is a small
    /// bonus: typing <c>^</c> narrows the list to the eleven demographic elements.
    /// </para>
    /// <para>
    /// Rebuilt whole rather than patched. It runs on a keystroke over at most a few hundred rows,
    /// the alternative is index arithmetic against a sorted list, and getting that wrong would
    /// reorder the check list - which is the export column order (PORT-PLAN.md §6).
    /// </para>
    /// </remarks>
    private void ApplyFilter()
    {
        VisibleDataElements.Clear();

        foreach (DataElementViewModel element in DataElements)
        {
            if (_foldedFilter.Length == 0 ||
                element.Title.ToLower(CultureInfo.CurrentCulture).Contains(_foldedFilter, StringComparison.Ordinal))
            {
                VisibleDataElements.Add(element);
            }
        }

        OnPropertyChanged(nameof(ShowNoMatches));
    }

    partial void OnFilterTextChanged(string value)
    {
        // Not trimmed: the population list's rule, PORT-PLAN.md §8.8 (i).
        _foldedFilter = value.ToLower(CultureInfo.CurrentCulture);

        ApplyFilter();
    }

    private void OnIdentificationModeChanged(object? sender, PersonIdentification mode)
    {
        _ = mode;

        OnPropertyChanged(nameof(Identification));
    }

    /// <summary>
    /// Empties the list, and records the study on the matrix. The first half of Delphi
    /// <c>AfterLogin</c>.
    /// </summary>
    /// <param name="sender">The session service.</param>
    /// <param name="session">The new session, or <see langword="null"/> for a disconnect.</param>
    /// <remarks>
    /// <para>
    /// <b>This handler no longer builds anything</b> - PORT-PLAN.md §8.10 (g). It used to start
    /// <see cref="ICollectorRegistry.BuildAsync"/> and abandon the task, which meant the shell could
    /// call itself connected while two round trips were still outstanding.
    /// <see cref="IConnectionCoordinator.ConnectAsync"/> awaits the build now, and the list arrives
    /// through <see cref="OnRegistryRebuilt"/>; what is left here is the <c>cbDataCollector.Clear</c>
    /// at <c>MainQuickStat.pas:481</c>, which runs on <em>every</em> session change so that a project
    /// switch cannot leave the previous study's elements on screen while the new list is fetched.
    /// </para>
    /// <para>
    /// It also copies the study id onto the matrix, which is the other half of <c>AfterLogin</c>'s
    /// <c>fGrid.Data.PrepareStudy</c> (<c>:479</c>): nothing else in the port sets
    /// <see cref="PersonMatrix.StudyId"/>, and a saved package records it. It belongs to the session
    /// rather than to the registry, which is why it stayed behind.
    /// </para>
    /// </remarks>
    private void OnSessionChanged(object? sender, SessionContext? session)
    {
        if (session is not null)
        {
            _workspace.Matrix.StudyId = session.StudyId;
        }

        // ISessionService.ConnectAsync awaits with ConfigureAwait(false) throughout, so this can
        // arrive on a thread-pool thread and DataElements is bound to a ListBox.
        _dispatcher.Invoke(Clear);
    }

    /// <summary>Shows the list the registry has just built. The second half of <c>AfterLogin</c>.</summary>
    /// <param name="sender">The registry.</param>
    /// <param name="collectors">Its new contents, in registry order.</param>
    /// <remarks>
    /// <para>
    /// Raised from inside <see cref="ICollectorRegistry.BuildAsync"/>, so the marshalling below
    /// finishes before the build's caller resumes: when
    /// <see cref="IConnectionCoordinator.ConnectAsync"/> hands a session back, this list is on
    /// screen. That is the ordering the Delphi got for free by filling <c>cbDataCollector</c> from a
    /// login observer.
    /// </para>
    /// <para>
    /// <b>A build that lands after a disconnect is dropped.</b> Cancelling the connect is the main
    /// defence and covers the reachable cases, but <c>ISessionService.DisconnectAsync</c> can also be
    /// called straight past this coordinator - <c>SessionService.Dispose</c> does exactly that at
    /// shutdown - and a list re-appearing under a session that is gone is worse than an empty one.
    /// </para>
    /// </remarks>
    private void OnRegistryRebuilt(object? sender, IReadOnlyList<ICollector> collectors)
    {
        if (_session.Current is not { } current)
        {
            _logger.LogInformation("A collector list arrived after the session had closed; it was discarded.");

            return;
        }

        _dispatcher.Invoke(() => Replace(collectors));

        _logger.LogInformation(
            "The Collections tab lists {ElementCount} data elements for study {StudyName}.",
            collectors.Count,
            current.StudyName);
    }
}
