using QuickStat.Diagnostics;
using QuickStat.ViewModels;
using QuickStat.Views.Dialogs;

namespace QuickStat.Services;

/// <summary>
/// Shows <see cref="UserNotification"/>s in a themed window. Replaces
/// <see cref="HeadlessNotificationPresenter"/> through <c>services.Replace(…)</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.6.</b> Step 3.1 wrote the <c>MessageBox</c> version so the application would not
/// swallow every warning while wave 2 was in flight; this replaces the rendering with
/// <see cref="NotificationDialog"/> and keeps every behavioural rule that version carried:
/// </para>
/// <list type="bullet">
///   <item><description>
///     It marshals to the user-interface thread. Callers of <see cref="IUserNotifier"/> are
///     explicitly told not to, and <see cref="UserNotifier"/> does not either.
///   </description></item>
///   <item><description>
///     <see cref="AskAsync"/> returns <see langword="true"/> <b>only</b> for an actual yes. Closing
///     the window, escape, an exception - all false. There is no path to true by omission.
///   </description></item>
///   <item><description>
///     It does not reimplement <see cref="IUserNotifier"/>. Severity mapping to a log level, PII
///     redaction and the never-fail-open rule live in <c>QuickStat.Core</c>, and a test asserts
///     <see cref="UserNotifier"/> is the only non-abstract implementation in the assembly.
///   </description></item>
///   <item><description>
///     The message arrives already redacted and with real line breaks; it is rendered verbatim.
///   </description></item>
/// </list>
/// <para>
/// The dialog's own header records what the button sets and icons reproduce, and the one place they
/// deliberately differ from the Delphi.
/// </para>
/// </remarks>
public sealed class WpfNotificationPresenter : IUserNotificationPresenter
{
    /// <summary>Caption used when a notification carries none.</summary>
    /// <remarks>Kept as the presenter's own constant so callers need not know about the view-model.</remarks>
    public const string DefaultTitle = NotificationViewModel.DefaultTitle;

    private readonly IUiDispatcher _dispatcher;
    private readonly Func<UserNotification, bool?> _show;

    /// <summary>Creates the presenter.</summary>
    /// <param name="dispatcher">Marshals to the user-interface thread.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dispatcher"/> is <see langword="null"/>.</exception>
    public WpfNotificationPresenter(IUiDispatcher dispatcher)
        : this(dispatcher, ShowDialog)
    {
    }

    /// <summary>Creates the presenter over a substitute for the modal. For tests only.</summary>
    /// <param name="dispatcher">Marshals to the user-interface thread.</param>
    /// <param name="show">Stands in for <see cref="ShowDialog"/>.</param>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    /// <remarks>
    /// Internal, so the container never sees it and cannot pick it: <c>QuickStat.App</c> already
    /// grants <c>InternalsVisibleTo</c> to the test assembly. Without a seam the never-fail-open
    /// rule below could only be checked by driving a modal window, and a test that drives a modal
    /// and gets it wrong hangs rather than fails.
    /// </remarks>
    internal WpfNotificationPresenter(IUiDispatcher dispatcher, Func<UserNotification, bool?> show)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(show);

        _dispatcher = dispatcher;
        _show = show;
    }

    /// <inheritdoc />
    public Task PresentAsync(UserNotification notification) => _dispatcher.InvokeAsync(() => _show(notification));

    /// <inheritdoc />
    public async Task<bool> AskAsync(UserNotification notification)
    {
        // Definitely assigned false first, and reassigned in exactly one place: the dialog's answer.
        // Anything that is not literally true - No, escape, the close box, a null result - stays no.
        bool answer = false;

        await _dispatcher.InvokeAsync(() => answer = _show(notification) == true).ConfigureAwait(true);

        return answer;
    }

    /// <summary>Shows the themed dialog and reports what the user pressed.</summary>
    /// <param name="notification">What to show.</param>
    /// <returns>
    /// <see langword="true"/> for <c>Yes</c>, <see langword="false"/> for <c>No</c> or <c>OK</c>, and
    /// <see langword="null"/> when the window was closed without either.
    /// </returns>
    private static bool? ShowDialog(UserNotification notification)
    {
        NotificationDialog dialog = new(notification);

        DialogOwner.Attach(dialog);

        return dialog.ShowDialog();
    }
}
