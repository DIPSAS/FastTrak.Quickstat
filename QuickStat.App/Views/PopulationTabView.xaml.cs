using System.Windows.Controls;

namespace QuickStat.Views;

/// <summary>The <c>Population</c> tab.</summary>
/// <remarks>
/// <b>OWNER: step 3.2.</b> <c>05-ui-spec.md</c> §B.1: the <c>Select database</c> header and its combo
/// box, the <c>Select population</c> header, the embedded
/// <see cref="PopulationPickerView"/>, and the tip along the bottom. Choosing a project runs
/// <see cref="QuickStat.Services.IConnectionCoordinator.ConnectAsync"/>, which is the whole of the
/// Delphi's <c>SelectConnection</c> - caption load included.
/// </remarks>
public partial class PopulationTabView : UserControl
{
    /// <summary>Initialises the tab.</summary>
    public PopulationTabView() => InitializeComponent();
}
