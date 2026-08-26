using System.Windows.Controls;

namespace QuickStat.Views;

/// <summary>The white strip across the top: icon, wordmark, version, and the Progress block.</summary>
/// <remarks>
/// Delphi <c>panWhiteTop</c> (<c>05-ui-spec.md</c> §A.2). Its data context is
/// <see cref="QuickStat.ViewModels.MainViewModel"/>, inherited from the window; it has no
/// view-model of its own because everything it shows is window chrome, and §H.2 puts window chrome
/// on the shell view-model. Step 3.1 owns this.
/// </remarks>
public partial class AppBannerView : UserControl
{
    /// <summary>Initialises the banner.</summary>
    public AppBannerView() => InitializeComponent();
}
