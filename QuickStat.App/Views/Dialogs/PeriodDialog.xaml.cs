using System.Windows;

namespace QuickStat.Views.Dialogs;

/// <summary>The <c>Angi periode</c> modal.</summary>
/// <remarks>
/// <b>OWNER: step 3.6.</b> Step 3.1 wrote the layout skeleton; nothing shows this window yet, because
/// <see cref="QuickStat.Services.WpfPeriodPrompt"/> is still a stub that always cancels. See that
/// class for the full list of what remains, and <c>05-ui-spec.md</c> §D.5 for the metrics.
/// </remarks>
public partial class PeriodDialog : Window
{
    /// <summary>Initialises the dialog.</summary>
    public PeriodDialog() => InitializeComponent();

    private void OnAccept(object sender, RoutedEventArgs e) => DialogResult = true;
}
