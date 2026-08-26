using System.Windows.Controls;

namespace QuickStat.Views;

/// <summary>The <c>Packages</c> tab.</summary>
/// <remarks>
/// <b>OWNER: step 3.4.</b> Step 3.1 wrote the layout skeleton. Left to do: load and refresh the
/// list, the trimmed uppercase filter (semantics in <c>Docs/Port/07-ui-contracts.md</c>), the
/// confirmed delete, the double-click replay, and subscribing to
/// <c>DatasetViewModel.SaveDataPackageRequested</c> so <c>Package dataset specification for reuse</c>
/// has somewhere to land.
/// </remarks>
public partial class PackagesTabView : UserControl
{
    /// <summary>Initialises the tab.</summary>
    public PackagesTabView() => InitializeComponent();
}
