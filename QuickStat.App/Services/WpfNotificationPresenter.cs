using System.Windows;
using QuickStat.Diagnostics;

namespace QuickStat.Services;

/// <summary>
/// Shows <see cref="UserNotification"/>s in a window. Replaces
/// <see cref="HeadlessNotificationPresenter"/> through <c>services.Replace(…)</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.6.</b> Step 3.1 wrote this so that the application does not swallow every
/// warning and error while wave 2 is in flight - the headless default answers, logs and shows
/// nothing, which would make the shell look broken. What is here works and is correct; what is
/// missing is the <em>styling</em>.
/// </para>
/// <para>
/// <b>What 3.6 has left to do:</b> replace <see cref="MessageBox"/> with the themed dialog, so the
/// notification chrome matches the rest of the application. Keep every behavioural rule below -
/// they are not cosmetic:
/// </para>
/// <list type="bullet">
///   <item><description>
///     Marshal to the user-interface thread. Callers of <see cref="IUserNotifier"/> are explicitly
///     told not to, and <see cref="UserNotifier"/> does not either.
///   </description></item>
///   <item><description>
///     <see cref="AskAsync"/> returns <see langword="true"/> <b>only</b> for an actual yes. Closing
///     the window, escape, an exception - all false. There must be no path to true by omission.
///   </description></item>
///   <item><description>
///     Do not reimplement <see cref="IUserNotifier"/>. Severity mapping, PII redaction and the
///     never-fail-open rule live in <c>QuickStat.Core</c> and a test asserts
///     <see cref="UserNotifier"/> is the only non-abstract implementation in the assembly.
///   </description></item>
///   <item><description>
///     The message arrives already redacted and with real line breaks; render it verbatim.
///   </description></item>
/// </list>
/// </remarks>
public sealed class WpfNotificationPresenter : IUserNotificationPresenter
{
    /// <summary>Caption used when a notification carries none.</summary>
    public const string DefaultTitle = "QuickStat";

    private readonly IUiDispatcher _dispatcher;

    /// <summary>Creates the presenter.</summary>
    /// <param name="dispatcher">Marshals to the user-interface thread.</param>
    /// <exception cref="ArgumentNullException"><paramref name="dispatcher"/> is <see langword="null"/>.</exception>
    public WpfNotificationPresenter(IUiDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);

        _dispatcher = dispatcher;
    }

    /// <inheritdoc />
    public Task PresentAsync(UserNotification notification) => _dispatcher.InvokeAsync(() =>
        Show(notification, MessageBoxButton.OK));

    /// <inheritdoc />
    public async Task<bool> AskAsync(UserNotification notification)
    {
        bool answer = false;

        await _dispatcher.InvokeAsync(() => answer = Show(notification, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            .ConfigureAwait(true);

        return answer;
    }

    private static MessageBoxResult Show(UserNotification notification, MessageBoxButton buttons)
    {
        MessageBoxImage icon = notification.Severity switch
        {
            NotificationSeverity.Error => MessageBoxImage.Error,
            NotificationSeverity.Warning => MessageBoxImage.Warning,
            _ => MessageBoxImage.Information,
        };

        // Application.Current is null under test and MessageBox.Show(null, …) is not an overload,
        // so the owner has to be chosen rather than passed through.
        Window? owner = Application.Current?.MainWindow;

        return owner is null
            ? MessageBox.Show(notification.Message, notification.Title ?? DefaultTitle, buttons, icon)
            : MessageBox.Show(owner, notification.Message, notification.Title ?? DefaultTitle, buttons, icon);
    }
}
