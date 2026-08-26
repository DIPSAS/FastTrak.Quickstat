using System.Windows;

namespace QuickStat.Views.Dialogs;

/// <summary>The <c>Save specification</c> modal.</summary>
/// <remarks>
/// <b>OWNER: step 3.6.</b> Step 3.1 wrote the layout skeleton and the accept path so the window
/// compiles and closes. Left to do: the exact banner and metrics of <c>05-ui-spec.md</c> §E, and
/// whatever step 3.4 needs to show it - the caller sets
/// <see cref="System.Windows.FrameworkElement.DataContext"/> to a
/// <see cref="QuickStat.ViewModels.SaveSpecViewModel"/> and reads it back after
/// <see cref="Window.ShowDialog"/> returns <see langword="true"/>.
/// </remarks>
public partial class SaveSpecDialog : Window
{
    /// <summary>Initialises the dialog.</summary>
    public SaveSpecDialog() => InitializeComponent();

    private void OnAccept(object sender, RoutedEventArgs e) => DialogResult = true;
}
