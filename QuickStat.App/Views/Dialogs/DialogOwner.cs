using System.Windows;

namespace QuickStat.Views.Dialogs;

/// <summary>
/// Gives a modal window the shell as its owner, when there is a shell to give it.
/// </summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.6.</b> Three lines that are wrong in two different ways if written from memory,
/// which is why they are written once:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <see cref="Application.Current"/> may be <see langword="null"/> under test - WPF allows one
///     <see cref="Application"/> per <c>AppDomain</c> and the suite creates at most one - so it must
///     never be dereferenced without a null check (<c>07-ui-contracts.md</c> §6). An owner-less
///     dialog still shows; it is simply not centred on anything.
///   </description></item>
///   <item><description>
///     Assigning <see cref="Window.Owner"/> a window that has never been shown throws
///     <see cref="InvalidOperationException"/>. That is reachable during start-up, where
///     <c>App.OnStartup</c> assigns <c>MainWindow</c> before calling <c>Show</c> on it, and a
///     notification raised in between would take the process down while reporting something else.
///   </description></item>
/// </list>
/// <para>
/// Without an owner, <c>WindowStartupLocation="CenterOwner"</c> falls back to centring on the
/// screen, which is the right behaviour for the only case that produces it.
/// </para>
/// </remarks>
internal static class DialogOwner
{
    /// <summary>Sets <see cref="Window.Owner"/> to the shell window, if that is possible.</summary>
    /// <param name="dialog">The modal about to be shown.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dialog"/> is <see langword="null"/>.</exception>
    internal static void Attach(Window dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        if (Application.Current?.MainWindow is not { IsLoaded: true } owner || ReferenceEquals(owner, dialog))
        {
            return;
        }

        dialog.Owner = owner;
    }

    /// <summary>Puts a dialog over the centre of its owner, once its own size is known.</summary>
    /// <param name="dialog">The modal, from its <c>SourceInitialized</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dialog"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// <c>WindowStartupLocation="CenterOwner"</c> already does this, and gets it right on every open
    /// but the <em>first</em> in a process: these dialogs size to their content, and the placement
    /// is computed before the content has settled, so the first <c>Save specification</c> of a
    /// session came up 27 px left and 90 px above the owner's centre (parity checklist 7.1).
    /// Redoing the arithmetic once the window has been measured is cheap and cannot be wrong twice.
    /// </para>
    /// <para>
    /// A maximised owner is left alone. <see cref="Window.Left"/> and <see cref="Window.Top"/> report
    /// the <em>restore</em> bounds in that state, so correcting from them would move a correctly
    /// placed dialog; WPF, which works from the real window rectangle, is right there already.
    /// </para>
    /// </remarks>
    internal static void CentreOnOwner(Window dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        if (dialog.Owner is not { WindowState: WindowState.Normal } owner)
        {
            return;
        }

        dialog.UpdateLayout();

        if (dialog.ActualWidth <= 0 || dialog.ActualHeight <= 0)
        {
            return;
        }

        dialog.Left = owner.Left + ((owner.ActualWidth - dialog.ActualWidth) / 2);
        dialog.Top = owner.Top + ((owner.ActualHeight - dialog.ActualHeight) / 2);
    }
}
