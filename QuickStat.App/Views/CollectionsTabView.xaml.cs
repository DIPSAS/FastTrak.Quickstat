using System.Windows.Controls;

namespace QuickStat.Views;

/// <summary>The <c>Collections</c> tab.</summary>
/// <remarks>
/// <b>OWNER: step 3.3.</b> Step 3.1 wrote the layout skeleton and wired the radio group and the
/// timestamp check box to their shared homes. Left to do: fill the list from the collector registry
/// sorted ordinally by title, run the collect, show which element is being collected without moving
/// the selection, and restore the scroll offset when the run finishes.
/// </remarks>
public partial class CollectionsTabView : UserControl
{
    /// <summary>Initialises the tab.</summary>
    public CollectionsTabView() => InitializeComponent();
}
