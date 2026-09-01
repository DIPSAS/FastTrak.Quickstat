using System.Windows;

namespace QuickStat.Views.Dialogs;

/// <summary>The <c>Angi periode</c> modal.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.6.</b> <c>05-ui-spec.md</c> §D.5 and <c>Emetra.VclForm.Period.dfm</c> for the
/// metrics; <see cref="QuickStat.ViewModels.PeriodViewModel"/> for the strings and the validity
/// rule. The service that shows it is <see cref="QuickStat.Services.WpfPeriodPrompt"/>, which is
/// also what reads and writes the remembered range.
/// </para>
/// <para>
/// The code-behind is one line, because there is one thing the view-model cannot do: close the
/// window. <c>OK</c> is already disabled while the range is invalid
/// (<see cref="QuickStat.ViewModels.PeriodViewModel.CanAccept"/>), so this cannot accept one.
/// </para>
/// </remarks>
public partial class PeriodDialog : Window
{
    /// <summary>Initialises the dialog.</summary>
    public PeriodDialog() => InitializeComponent();

    /// <inheritdoc />
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        DialogOwner.CentreOnOwner(this);
    }

    private void OnAccept(object sender, RoutedEventArgs e) => DialogResult = true;
}
