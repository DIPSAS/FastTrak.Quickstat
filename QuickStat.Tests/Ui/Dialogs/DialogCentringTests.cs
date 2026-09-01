using System.Windows;
using QuickStat.ViewModels;
using QuickStat.Views.Dialogs;
using Xunit;

namespace QuickStat.Tests.Ui.Dialogs;

/// <summary>
/// A modal opens over the centre of its owner, including the first one of a session.
/// </summary>
/// <remarks>
/// <para>
/// <c>WindowStartupLocation="CenterOwner"</c> does this by itself on every open <em>but</em> the
/// first in a process: these dialogs size to their content, and the first placement is computed
/// before the content has settled. Driven through the running window, the first
/// <c>Save specification</c> of a session came up 27 px left and 90 px above the owner's centre,
/// and every later one was exact (parity checklist 7.1). Each dialog therefore redoes the
/// arithmetic from <c>OnSourceInitialized</c>.
/// </para>
/// <para>
/// <b>What these tests can and cannot show.</b> The quirk itself needs a fresh process and a real
/// desktop, so it is verified with the live rig, not here. What is pinned here is the arithmetic —
/// including the case that would be made <em>worse</em> by getting it wrong, a maximised owner,
/// whose <see cref="Window.Left"/> reports its restore bounds — and that a dialog shown with an
/// owner ends up centred on it. Read the third case as a contract, not as a reproduction.
/// </para>
/// </remarks>
public class DialogCentringTests
{
    /// <summary>Far enough off the desktop that nothing flashes during a test run.</summary>
    private const double OffScreen = -10000;

    private static Window Owner() => new()
    {
        WindowStartupLocation = WindowStartupLocation.Manual,
        ShowInTaskbar = false,
        ShowActivated = false,
        Left = OffScreen,
        Top = OffScreen,
        Width = 900,
        Height = 600,
    };

    [Fact]
    public void CentringMovesADisplacedDialogBack() => StaTestRunner.Run(() =>
    {
        Window owner = Owner();
        SaveSpecDialog dialog = new() { DataContext = new SaveSpecViewModel(), ShowActivated = false };

        try
        {
            owner.Show();
            owner.UpdateLayout();

            dialog.Owner = owner;
            dialog.Show();
            dialog.UpdateLayout();

            // Displace it the way the first-open placement does, then ask for the correction.
            dialog.Left -= 27;
            dialog.Top -= 90;

            DialogOwner.CentreOnOwner(dialog);

            Assert.Equal(owner.Left + (owner.ActualWidth / 2), dialog.Left + (dialog.ActualWidth / 2), 0);
            Assert.Equal(owner.Top + (owner.ActualHeight / 2), dialog.Top + (dialog.ActualHeight / 2), 0);
        }
        finally
        {
            dialog.Close();
            owner.Close();
        }
    });

    [Fact]
    public void AMaximisedOwnerIsLeftToWpf() => StaTestRunner.Run(() =>
    {
        // Window.Left and Window.Top report the RESTORE bounds while maximised, so centring from
        // them would move a dialog that WPF had placed correctly. The correction must decline.
        Window owner = Owner();
        SaveSpecDialog dialog = new() { DataContext = new SaveSpecViewModel(), ShowActivated = false };

        try
        {
            owner.Show();
            owner.WindowState = WindowState.Maximized;
            owner.UpdateLayout();

            dialog.Owner = owner;
            dialog.Show();
            dialog.UpdateLayout();

            double left = dialog.Left;
            double top = dialog.Top;

            DialogOwner.CentreOnOwner(dialog);

            Assert.Equal(left, dialog.Left);
            Assert.Equal(top, dialog.Top);
        }
        finally
        {
            dialog.Close();
            owner.Close();
        }
    });

    [Fact]
    public void ADialogShownWithAnOwnerIsCentredOnIt() => StaTestRunner.Run(() =>
    {
        Window owner = Owner();
        SaveSpecDialog dialog = new() { DataContext = new SaveSpecViewModel(), ShowActivated = false };

        try
        {
            owner.Show();
            owner.UpdateLayout();

            dialog.Owner = owner;
            dialog.Show();
            dialog.UpdateLayout();

            Assert.Equal(owner.Left + (owner.ActualWidth / 2), dialog.Left + (dialog.ActualWidth / 2), 0);
            Assert.Equal(owner.Top + (owner.ActualHeight / 2), dialog.Top + (dialog.ActualHeight / 2), 0);
        }
        finally
        {
            dialog.Close();
            owner.Close();
        }
    });
}
