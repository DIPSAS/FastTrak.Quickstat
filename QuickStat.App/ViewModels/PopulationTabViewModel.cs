using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickStat.Configuration;

namespace QuickStat.ViewModels;

/// <summary>The <c>Population</c> tab: the database combo box and the embedded picker.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.2. This is a compiling stub with no behaviour.</b>
/// </para>
/// <para>
/// <b>What is left to do</b> (<c>05-ui-spec.md</c> §B.1, §G.3, §G.5):
/// </para>
/// <list type="bullet">
///   <item><description>
///     Fill <see cref="Projects"/> from <see cref="IConnectionCatalog"/> - the deployed
///     <c>QuickStat.config.xml</c> - sorted by display name. <b>Nothing is preselected</b>: picking
///     an item is what triggers the connection.
///   </description></item>
///   <item><description>
///     <see cref="SelectedProject"/>'s setter awaits
///     <see cref="QuickStat.Services.IConnectionCoordinator.ConnectAsync"/> and does nothing else.
///     That one call is the whole of the Delphi's <c>SelectConnection</c>: status text, busy state,
///     login, <b>and the caption load</b>, which must happen or every lab column falls back to its
///     raw variable name. Do not call <c>ISessionService</c> directly.
///   </description></item>
///   <item><description>
///     Optionally restore the last-used database through
///     <see cref="QuickStat.Services.IWindowStateService.GetLastDatabase"/> and store it here. That
///     is an addition step 3.1 provisioned but deliberately did not switch on: auto-connecting on
///     start-up is a behaviour change, not a convenience, so 3.2 should preselect at most and let
///     the user confirm.
///   </description></item>
/// </list>
/// </remarks>
public sealed partial class PopulationTabViewModel : ObservableObject
{
    /// <summary>Teal header above the combo box.</summary>
    public const string DatabaseHeader = "Select database";

    /// <summary>Teal header above the picker.</summary>
    public const string PopulationHeader = "Select population";

    /// <summary>Hint at the bottom of the tab.</summary>
    public const string TipText = "Tip: Double click to prepare population";

    [ObservableProperty]
    private QuickStatConnection? _selectedProject;

    /// <summary>Creates the tab's view-model.</summary>
    /// <param name="picker">The embedded population picker.</param>
    /// <exception cref="ArgumentNullException"><paramref name="picker"/> is <see langword="null"/>.</exception>
    public PopulationTabViewModel(PopulationPickerViewModel picker)
    {
        ArgumentNullException.ThrowIfNull(picker);

        Picker = picker;
    }

    /// <summary>The connections from <c>QuickStat.config.xml</c>, sorted by name.</summary>
    public ObservableCollection<QuickStatConnection> Projects { get; } = [];

    /// <summary>The embedded picker.</summary>
    public PopulationPickerViewModel Picker { get; }
}
