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
}
