using System.Windows;

namespace QuickStat.Tests.Ui.Dialogs;

/// <summary>
/// Shows a window far off the desktop, runs a body against the realised tree, and closes it again.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this is necessary, established by experiment rather than assumed.</b> A binding written in
/// XAML is compiled into BAML as an unattached <c>BindingExpression</c> and is only attached when the
/// element is realised - <c>BindingExpression.Status</c> stays <c>Unattached</c> and the target keeps
/// its default value until then, however many times the tree is measured. Constructing a window and
/// reading a bound property therefore proves nothing: every assertion passes against the default. A
/// binding created in code with <c>SetBinding</c> attaches immediately, which is what makes this look
/// like an inconsistency rather than a rule.
/// </para>
/// <para>
/// So a test that wants to know whether a view's bindings are right has to realise the view. The
/// window is positioned at -10 000, -10 000 and never activated, so nothing appears on the desktop
/// and nothing steals focus; it is closed in a <c>finally</c> so a failing assertion cannot leak an
/// <c>HWND</c>.
/// </para>
/// <para>
/// Use <see cref="QuickStat.Tests.Ui.StaTestRunner"/> around it: this needs an apartment like
/// everything else in WPF. It deliberately creates no <see cref="Application"/>.
/// </para>
/// </remarks>
internal static class RealisedWindow
{
    /// <summary>Far enough off-screen to be invisible on any plausible monitor arrangement.</summary>
    private const double OffScreen = -10000;

    /// <summary>Realises <paramref name="window"/> and runs <paramref name="body"/> against it.</summary>
    /// <typeparam name="TWindow">The window type.</typeparam>
    /// <param name="window">The window to show. Closed before this returns.</param>
    /// <param name="body">What to assert once the tree is live.</param>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    internal static void Run<TWindow>(TWindow window, Action<TWindow> body)
        where TWindow : Window
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(body);

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.ShowInTaskbar = false;
        window.ShowActivated = false;
        window.Left = OffScreen;
        window.Top = OffScreen;

        try
        {
            window.Show();
            window.UpdateLayout();

            body(window);
        }
        finally
        {
            window.Close();
        }
    }

    /// <summary>Realises a stand-alone control by hosting it in a window of its own.</summary>
    /// <typeparam name="TControl">The control type.</typeparam>
    /// <param name="control">The control to realise.</param>
    /// <param name="body">What to assert once the tree is live.</param>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    internal static void RunControl<TControl>(TControl control, Action<TControl> body)
        where TControl : FrameworkElement
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(body);

        // The host carries no DataContext of its own, so the control keeps whatever the caller set.
        Run(new Window { Width = 800, Height = 600, Content = control }, _ => body(control));
    }
}
