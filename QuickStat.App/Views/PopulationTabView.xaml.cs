using System.Windows.Controls;

namespace QuickStat.Views;

/// <summary>The <c>Population</c> tab.</summary>
/// <remarks>
/// <b>OWNER: step 3.2.</b> Step 3.1 wrote the layout skeleton. Left to do: fill the combo box from
/// <c>IConnectionCatalog</c> sorted by name with nothing preselected, and make the selection call
/// <c>IConnectionCoordinator.ConnectAsync</c> - which is the whole of <c>SelectConnection</c>,
/// caption load included.
/// </remarks>
public partial class PopulationTabView : UserControl
{
    /// <summary>Initialises the tab.</summary>
    public PopulationTabView() => InitializeComponent();
}
