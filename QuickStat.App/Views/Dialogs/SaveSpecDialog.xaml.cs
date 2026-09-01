using System.Windows;

namespace QuickStat.Views.Dialogs;

/// <summary>The <c>Save specification</c> modal.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.6.</b> <c>05-ui-spec.md</c> §E and <c>Emetra.VclForm.EditAndMemo.dfm</c> for
/// the metrics. The caller sets <see cref="FrameworkElement.DataContext"/> to a
/// <see cref="QuickStat.ViewModels.SaveSpecViewModel"/>, calls <see cref="Window.ShowDialog"/>, and
/// reads <c>Title</c> and <c>Comment</c> back off that view-model when it returns
/// <see langword="true"/>:
/// </para>
/// <code>
/// SaveSpecDialog dialog = new() { Owner = owner, DataContext = model };
/// if (dialog.ShowDialog() == true) { … model.Title, model.Comment … }
/// </code>
/// <para>
/// The view-model is registered transient, so each showing gets a fresh one - which is what removes
/// the Delphi's "the form is created once and reused, so the fields keep their contents" behaviour
/// (§E) without needing a <c>Clear</c> call at every call site.
/// </para>
/// <para>
/// The code-behind is one line. <c>Cancel</c> needs none - <c>IsCancel</c> closes the window with
/// <see langword="false"/> - and <c>OK</c> is already disabled while the title is blank, so this
/// cannot accept an unnamed package.
/// </para>
/// </remarks>
public partial class SaveSpecDialog : Window
{
    /// <summary>Initialises the dialog.</summary>
    public SaveSpecDialog() => InitializeComponent();

    /// <inheritdoc />
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        DialogOwner.CentreOnOwner(this);
    }

    private void OnAccept(object sender, RoutedEventArgs e) => DialogResult = true;
}
