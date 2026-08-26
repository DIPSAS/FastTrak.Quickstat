using System.Windows.Controls;

namespace QuickStat.Views.Dialogs;

/// <summary>The dimming layer shown while a long-running operation is in flight.</summary>
/// <remarks>
/// <b>OWNER: step 3.6.</b> A placeholder by step 3.1. Replaces the Delphi's
/// <c>Screen.Cursor := crSqlWait</c> (<c>05-ui-spec.md</c> §G.3), which no longer says enough now
/// that the work happens off the user-interface thread and the window stays responsive.
/// </remarks>
public partial class BusyOverlayView : UserControl
{
    /// <summary>Initialises the overlay.</summary>
    public BusyOverlayView() => InitializeComponent();
}
