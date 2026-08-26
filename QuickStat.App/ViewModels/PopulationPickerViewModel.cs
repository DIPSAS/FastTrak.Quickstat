using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace QuickStat.ViewModels;

/// <summary>The embedded population picker: filter box, two check boxes, the list, the SQL preview.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.2. This is a compiling stub with no behaviour.</b>
/// </para>
/// <para>
/// <b>What is left to do</b> (<c>05-ui-spec.md</c> §B.1.1, §G.5, §G.6, §H.2):
/// </para>
/// <list type="bullet">
///   <item><description>
///     Load the catalogue through <see cref="QuickStat.Domain.Populations.IPopulationRepository"/>
///     when a session appears, and again whenever <see cref="FrequentlyUsedOnly"/> changes - that
///     box <b>re-queries the server</b> with a different stored procedure; it is not a client-side
///     filter. It starts <b>disabled</b> and only becomes usable once <c>StudyId &gt; 0</c>.
///   </description></item>
///   <item><description>
///     Live filtering on every keystroke through an <c>ICollectionView</c>. The exact rule is
///     settled in PORT-PLAN.md §8.8 (i) and repeated in <c>Docs/Port/07-ui-contracts.md</c>:
///     lowercase <b>both</b> sides with <c>ToLower(CultureInfo.CurrentCulture)</c>, do <b>not</b>
///     trim the filter, and compare with <c>StringComparison.Ordinal</c>. Not
///     <c>CurrentCultureIgnoreCase</c> - that is a collation and folds more than Delphi's
///     <c>Pos</c> does. Populations are <b>not</b> sorted by the client; they arrive in
///     stored-procedure order.
///   </description></item>
///   <item><description>
///     Single click fills <see cref="SqlPreview"/> from
///     <see cref="QuickStat.Domain.Populations.Population.SourceCode"/>. Double click, and
///     <c>Enter</c>, prepare the population - see <see cref="PreparePopulationCommand"/>.
///   </description></item>
///   <item><description>
///     <see cref="IsSqlPreviewVisible"/> is gated on the <c>FUNC_POPULATION_SOURCE</c> access right,
///     whose default is denied. The access-control plumbing is outside the UI spec's scope
///     (§I.9); until it exists, decide and record a default rather than leaving the pane
///     unconditionally visible.
///   </description></item>
///   <item><description>
///     An empty result shows an empty-state message. The VCL hides the whole list instead; §B.1.1
///     records the message as a deliberate improvement.
///   </description></item>
/// </list>
/// <para>
/// <b>The prepare sequence is a contract, not a suggestion</b> - see
/// <see cref="QuickStat.Services.IShellWorkspace"/>: sort, prepare the matrix, then
/// <c>SetPopulation</c>, then <c>RequestCollectionsTab</c>. Getting the order wrong leaves
/// <c>HasPopulation</c> reading the previous cohort's row count.
/// </para>
/// </remarks>
public sealed partial class PopulationPickerViewModel : ObservableObject
{
    /// <summary>Placeholder in the filter box. Overwritten in English at run time by the Delphi.</summary>
    public const string FilterPlaceholder = "Type filter text here";

    /// <summary>Label above the filter box.</summary>
    public const string FilterHeader = "Filter / search text";

    /// <summary>Caption of the left-hand check box.</summary>
    public const string FrequentlyUsedCaption = "Frequently used only";

    /// <summary>Caption of the right-hand check box, which sits to the <em>left</em> of its box.</summary>
    public const string SimplifiedCaption = "Simplified";

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

    /// <summary>The catalogue, in stored-procedure order.</summary>
    public ObservableCollection<PopulationViewModel> Populations { get; } = [];

    /// <summary>Double click, or <c>Enter</c>: load the population into the grid.</summary>
    /// <remarks>Step 3.2 replaces this with the real command; the stub does nothing.</remarks>
    public CommunityToolkit.Mvvm.Input.IRelayCommand PreparePopulationCommand { get; } =
        new CommunityToolkit.Mvvm.Input.RelayCommand(static () => { }, static () => false);
}
