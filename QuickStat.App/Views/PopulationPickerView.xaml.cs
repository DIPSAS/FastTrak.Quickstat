using System.Windows.Controls;

namespace QuickStat.Views;

/// <summary>The embedded population picker.</summary>
/// <remarks>
/// <b>OWNER: step 3.2.</b> Step 3.1 wrote the layout skeleton so the shell lays out and the two
/// steps never open the same file. Left to do: the row template and its expansion, the live filter
/// (<c>ICollectionView</c>, semantics in <c>Docs/Port/07-ui-contracts.md</c>), the empty-state
/// message, double click and <c>Enter</c> as <c>PreparePopulation</c>, and the
/// <c>FUNC_POPULATION_SOURCE</c> gate on the SQL preview.
/// </remarks>
public partial class PopulationPickerView : UserControl
{
    /// <summary>Initialises the picker.</summary>
    public PopulationPickerView() => InitializeComponent();
}
