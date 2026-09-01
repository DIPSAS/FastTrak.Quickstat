using System.Windows;
using QuickStat.Diagnostics;
using QuickStat.ViewModels;

namespace QuickStat.Views.Dialogs;

/// <summary>The themed replacement for <c>MessageBox</c>.</summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.6.</b> Shown by <see cref="QuickStat.Services.WpfNotificationPresenter"/>, which
/// is the <see cref="IUserNotificationPresenter"/> the shell installs over the headless default.
/// </para>
/// <para>
/// <see cref="Window.ShowDialog"/> returns <see langword="true"/> for <c>Yes</c> and nothing else.
/// <c>No</c> is the <c>IsCancel</c> button, so it answers <see langword="false"/>; the close box
/// leaves <see cref="Window.DialogResult"/> null, which is also not a yes. There is exactly one
/// statement in this file that can produce a yes, and it is a click handler on the <c>Yes</c>
/// button.
/// </para>
/// </remarks>
public partial class NotificationDialog : Window
{
    /// <summary>Initialises the dialog for one notification.</summary>
    /// <param name="notification">What to show.</param>
    public NotificationDialog(UserNotification notification)
        : this() => DataContext = new NotificationViewModel(notification);

    /// <summary>
    /// Loads the XAML. Private: a notification dialog with nothing to notify has no use, and there
    /// is no path that produces one by accident.
    /// </summary>
    private NotificationDialog() => InitializeComponent();

    /// <inheritdoc />
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        DialogOwner.CentreOnOwner(this);
    }

    private void OnYes(object sender, RoutedEventArgs e) => DialogResult = true;
}
