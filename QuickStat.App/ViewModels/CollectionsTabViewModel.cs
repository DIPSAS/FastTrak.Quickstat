using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickStat.Domain.Anonymisation;
using QuickStat.Services;

namespace QuickStat.ViewModels;

/// <summary>The <c>Collections</c> tab: the data-element list, <c>Collect data</c>, export options.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.3. This is a compiling stub with no behaviour.</b> Step 3.1 wired the two
/// properties that cross tab boundaries - <see cref="Identification"/> and
/// <see cref="ExportTimestamps"/> - because getting either of them wrong is a privacy or a
/// data-format defect, and both already have a single shared home.
/// </para>
/// <para>
/// <b>What is left to do</b> (<c>05-ui-spec.md</c> §B.2, §D.1, §G.4, §G.5):
/// </para>
/// <list type="bullet">
///   <item><description>
///     Fill <see cref="DataElements"/> from <see cref="QuickStat.Collectors.ICollectorRegistry"/>
///     after login, sorted <b>ordinally by title</b>. That order is the column order of every
///     export - PORT-PLAN.md §6.
///   </description></item>
///   <item><description>
///     <see cref="CollectDataCommand"/>: clear the variables, add the DRUID/DRUG caption records,
///     walk the list from index 0, and for each ticked element set the status line to its title and
///     run it through <see cref="QuickStat.Collectors.ICollectorRunner"/> into the matrix. Then lock
///     the matrix and call
///     <see cref="IShellWorkspace.NotifyDataChanged"/> - which is what re-enables
///     <c>Open this dataset in Excel</c>, refreshes the dataset caption and repaints the grid.
///     Progress is per patient: <c>100 * personIndex / population.Count</c>. Finish with
///     <see cref="IShellProgress.Done"/>.
///   </description></item>
///   <item><description>
///     Show which element is being collected through <see cref="DataElementViewModel.IsCollecting"/>
///     and <see cref="CurrentlyCollecting"/>. §G.4: <b>do not</b> move <c>SelectedItem</c>, and
///     restore the scroll offset afterwards. The Delphi moved <c>ItemIndex</c> and then put it back.
///   </description></item>
///   <item><description>
///     Every <see cref="DataElementViewModel.IsChecked"/> change must call
///     <c>CollectDataCommand.NotifyCanExecuteChanged()</c> and
///     <see cref="IShellWorkspace.SetCheckedCollectorNames"/>.
///   </description></item>
/// </list>
/// </remarks>
public sealed partial class CollectionsTabViewModel : ObservableObject
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

    [ObservableProperty]
    private DataElementViewModel? _currentlyCollecting;

    /// <summary>Creates the tab's view-model.</summary>
    /// <param name="workspace">Cross-tab state; owns the timestamp flag and the ticked names.</param>
    /// <param name="identification">The one shared identification mode.</param>
    public CollectionsTabViewModel(IShellWorkspace workspace, IIdentificationPolicy identification)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(identification);

        _workspace = workspace;
        _identification = identification;

        _identification.ModeChanged += (_, _) => OnPropertyChanged(nameof(Identification));
    }

    /// <summary>The tickable data elements, sorted ordinally by <see cref="DataElementViewModel.Title"/>.</summary>
    public ObservableCollection<DataElementViewModel> DataElements { get; } = [];

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

    /// <summary>Runs the collect. Enabled while at least one element is ticked.</summary>
    /// <remarks>Step 3.3 replaces this with the real command; the stub is permanently disabled.</remarks>
    public IAsyncRelayCommand CollectDataCommand { get; } =
        new AsyncRelayCommand(static () => Task.CompletedTask, static () => false);
}
