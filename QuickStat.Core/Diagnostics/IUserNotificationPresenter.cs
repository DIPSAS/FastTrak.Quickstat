namespace QuickStat.Diagnostics;

/// <summary>
/// The seam between <see cref="IUserNotifier"/> and whatever can actually draw a dialog.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Phase 3 implements this interface, not <see cref="IUserNotifier"/>.</strong> There is
/// exactly one <see cref="IUserNotifier"/> in the product - <see cref="UserNotifier"/> - and it owns
/// three things a WPF class should not have to re-derive: the mapping from severity to log level,
/// PII redaction on the way to the log, and the guarantee that
/// <see cref="IUserNotifier.ConfirmAsync"/> never fails open. A presenter is left with the part that
/// genuinely needs WPF: show a window, marshal to the dispatcher, own the parent window.
/// </para>
/// <para>
/// To install the WPF presenter, replace the headless default rather than adding alongside it:
/// </para>
/// <code>
/// services.AddQuickStatDiagnostics();
/// services.Replace(ServiceDescriptor.Singleton&lt;IUserNotificationPresenter, WpfNotificationPresenter&gt;());
/// </code>
/// <para>
/// A presenter is responsible for marshalling to the UI thread itself. Callers of
/// <see cref="IUserNotifier"/> must not, and <see cref="UserNotifier"/> does not.
/// </para>
/// <para>
/// A presenter may throw and may return a faulted or cancelled task.
/// <see cref="UserNotifier"/> treats all three as "the user did not answer", which is
/// <see langword="false"/>; see <see cref="AskAsync"/>.
/// </para>
/// </remarks>
public interface IUserNotificationPresenter
{
    /// <summary>Shows a statement and waits for the user to dismiss it.</summary>
    /// <param name="notification">What to show. <see cref="UserNotification.IsQuestion"/> is <see langword="false"/>.</param>
    /// <returns>A task that completes when the notification has been dismissed.</returns>
    Task PresentAsync(UserNotification notification);

    /// <summary>Asks a yes/no question and waits for the answer.</summary>
    /// <param name="notification">What to ask. <see cref="UserNotification.IsQuestion"/> is <see langword="true"/>.</param>
    /// <returns>
    /// <see langword="true"/> only when the user actively chose yes. Closing the window, pressing
    /// escape, cancelling, or any other outcome is <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// The return type's default value is the safe answer. That is deliberate: an implementation
    /// that forgets a code path, a test double with no behaviour configured, and a task that never
    /// produces a value all yield <see langword="false"/>. There is no way to arrive at
    /// <see langword="true"/> by omission.
    /// </remarks>
    Task<bool> AskAsync(UserNotification notification);
}
