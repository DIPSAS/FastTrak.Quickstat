using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickStat.Services;

namespace QuickStat.ViewModels;

/// <summary>The shell: window chrome, the Progress block, the tab selection, and the four tabs.</summary>
/// <remarks>
/// <para>
/// <c>05-ui-spec.md</c> §H.2. Step 3.1 owns this.
/// </para>
/// <para>
/// It composes the tab view-models rather than being injected into them, which is why the progress
/// state lives in <see cref="IShellProgress"/> and the cross-tab state in
/// <see cref="IShellWorkspace"/>: a child that took <c>MainViewModel</c> in its constructor would be
/// a cycle the container cannot resolve. Everything a tab needs from the shell it gets from one of
/// those two services.
/// </para>
/// <para>
/// There is no <c>SelectedDatasetTab</c>. The right-hand pane is a plain content host, not a
/// <c>TabControl</c>, because the only other tab was <c>Time series</c> and that is dropped
/// (PORT-PLAN.md §7.1, <c>05-ui-spec.md</c> §C.2).
/// </para>
/// </remarks>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    /// <summary>Index of the <c>Population</c> tab in the left pane.</summary>
    public const int PopulationTabIndex = 0;

    /// <summary>Index of the <c>Collections</c> tab in the left pane.</summary>
    public const int CollectionsTabIndex = 1;

    /// <summary>Index of the <c>Packages</c> tab in the left pane.</summary>
    public const int PackagesTabIndex = 2;

    /// <summary>
    /// The left pane's width before the user drags the splitter.
    /// </summary>
    /// <remarks>
    /// <c>splMain.Position = 293</c> in <c>MainQuickStat.dfm</c>. <c>05-ui-spec.md</c> §I.1 records
    /// that the screenshots show about 336 and the design brief says about 330, and leaves the
    /// choice open; the spec's own recommendation is 293 with a minimum of 260, and the screenshots
    /// are of a 2019 build whose layout differs elsewhere too (§0). Step 3.1 takes 293, and the
    /// position is now persisted (§G.1's recommended addition), so a user who prefers 330 drags once
    /// and keeps it.
    /// </remarks>
    public const double DefaultSplitterPosition = 293;

    /// <summary>Narrowest the left pane may become. <b>Addition</b>; the Delphi sets no minimum.</summary>
    public const double MinimumSplitterPosition = 260;

    private readonly IShellProgress _progress;
    private readonly IShellWorkspace _workspace;
    private readonly IWindowStateService _windowState;
    private readonly IApplicationInfo _applicationInfo;

    [ObservableProperty]
    private int _selectedSelectionTab = PopulationTabIndex;

    [ObservableProperty]
    private double _splitterPosition = DefaultSplitterPosition;

    private bool _disposed;

    /// <summary>Creates the shell view-model.</summary>
    /// <param name="progress">The Progress block and the busy flag.</param>
    /// <param name="workspace">Cross-tab state; the source of <see cref="HasPopulation"/>.</param>
    /// <param name="windowState">Persisted geometry, splitter position and last database.</param>
    /// <param name="applicationInfo">Title, wordmark and version.</param>
    /// <param name="population">The <c>Population</c> tab.</param>
    /// <param name="collections">The <c>Collections</c> tab.</param>
    /// <param name="packages">The <c>Packages</c> tab.</param>
    /// <param name="dataset">The right-hand pane.</param>
    public MainViewModel(
        IShellProgress progress,
        IShellWorkspace workspace,
        IWindowStateService windowState,
        IApplicationInfo applicationInfo,
        PopulationTabViewModel population,
        CollectionsTabViewModel collections,
        PackagesTabViewModel packages,
        DatasetViewModel dataset)
    {
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(windowState);
        ArgumentNullException.ThrowIfNull(applicationInfo);
        ArgumentNullException.ThrowIfNull(population);
        ArgumentNullException.ThrowIfNull(collections);
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentNullException.ThrowIfNull(dataset);

        _progress = progress;
        _workspace = workspace;
        _windowState = windowState;
        _applicationInfo = applicationInfo;

        Population = population;
        Collections = collections;
        Packages = packages;
        Dataset = dataset;

        _splitterPosition = windowState.GetSplitterPosition(DefaultSplitterPosition);

        _progress.PropertyChanged += OnProgressChanged;
        _workspace.PropertyChanged += OnWorkspaceChanged;
        _workspace.CollectionsTabRequested += OnCollectionsTabRequested;
    }

    /// <summary>Window and taskbar title. <c>FastTrak QuickStat</c>.</summary>
    public string WindowTitle => _applicationInfo.Title;

    /// <summary>The wordmark beside the icon. <c>QuickStat</c>.</summary>
    public string ProductName => _applicationInfo.ProductName;

    /// <summary>
    /// The build's file version, shown after the word <c>version</c>.
    /// </summary>
    /// <remarks>
    /// Reads whatever the assembly carries, which <c>&lt;Version&gt;</c> in
    /// <c>Directory.Build.props</c> sets to <c>26.0.0.0</c>; the shipped Delphi build says
    /// <c>22.12.21.547</c>. See <see cref="IApplicationInfo"/>.
    /// </remarks>
    public string VersionText => _applicationInfo.Version;

    /// <summary>The <c>Population</c> tab. Step 3.2.</summary>
    public PopulationTabViewModel Population { get; }

    /// <summary>The <c>Collections</c> tab. Step 3.3.</summary>
    public CollectionsTabViewModel Collections { get; }

    /// <summary>The <c>Packages</c> tab. Step 3.4.</summary>
    public PackagesTabViewModel Packages { get; }

    /// <summary>The right-hand pane. Step 3.1.</summary>
    public DatasetViewModel Dataset { get; }

    /// <summary>The static heading above the status line. Always <c>Progress</c>.</summary>
    public string ProgressHeader => _progress.Header;

    /// <summary>The status line. <c>Program is idle</c> until something happens.</summary>
    public string ProgressInfo => _progress.Info;

    /// <summary>Completion, 0 to 100.</summary>
    public double ProgressPercent => _progress.Percent;

    /// <summary>Whether <see cref="ProgressInfo"/> is a failure and should be shown in red.</summary>
    public bool ProgressIsError => _progress.IsError;

    /// <summary>Whether a long-running operation is in flight: wait cursor and busy overlay.</summary>
    public bool IsBusy => _progress.IsBusy;

    /// <summary>
    /// Whether the <c>Collections</c> tab is shown and enabled.
    /// </summary>
    /// <remarks>
    /// §B.0: hidden <em>and</em> disabled until a population is loaded, then shown and activated.
    /// Hiding without disabling would leave the tab reachable with <c>Ctrl+Tab</c>.
    /// </remarks>
    public bool HasPopulation => _workspace.HasPopulation;

    /// <summary>Stores the splitter position so the next run opens at the same width.</summary>
    /// <remarks>
    /// <b>Addition</b> (§G.1). Called by the shell when the drag finishes and again on close, rather
    /// than on every pixel of movement.
    /// </remarks>
    /// <param name="position">The left pane's width.</param>
    public void PersistSplitterPosition(double position)
    {
        if (position < MinimumSplitterPosition)
        {
            return;
        }

        SplitterPosition = position;

        _windowState.SetSplitterPosition(position);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _progress.PropertyChanged -= OnProgressChanged;
        _workspace.PropertyChanged -= OnWorkspaceChanged;
        _workspace.CollectionsTabRequested -= OnCollectionsTabRequested;

        Dataset.Dispose();
    }

    private void OnProgressChanged(object? sender, PropertyChangedEventArgs e)
    {
        // The shell's names differ from the service's, so this cannot be a straight forward.
        string? mapped = e.PropertyName switch
        {
            nameof(IShellProgress.Header) => nameof(ProgressHeader),
            nameof(IShellProgress.Info) => nameof(ProgressInfo),
            nameof(IShellProgress.Percent) => nameof(ProgressPercent),
            nameof(IShellProgress.IsError) => nameof(ProgressIsError),
            nameof(IShellProgress.IsBusy) => nameof(IsBusy),
            _ => null,
        };

        if (mapped is not null)
        {
            OnPropertyChanged(mapped);
        }
    }

    private void OnWorkspaceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(IShellWorkspace.HasPopulation))
        {
            OnPropertyChanged(nameof(HasPopulation));

            // A collapsed TabItem that is still selected leaves the TabControl blank, so a
            // population that turns out to be empty has to move the user off the tab as well as
            // hide it.
            if (!_workspace.HasPopulation && SelectedSelectionTab == CollectionsTabIndex)
            {
                SelectedSelectionTab = PopulationTabIndex;
            }
        }
    }

    private void OnCollectionsTabRequested(object? sender, EventArgs e)
    {
        // Delphi AfterPopulationSelect: pgSelections.ActivePage := tbsDataElements, but only after
        // LoadPopulationIntoGrid decided the tab was visible at all.
        if (_workspace.HasPopulation)
        {
            SelectedSelectionTab = CollectionsTabIndex;
        }
    }
}
