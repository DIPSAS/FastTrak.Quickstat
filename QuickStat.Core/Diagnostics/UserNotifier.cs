using Microsoft.Extensions.Logging;

namespace QuickStat.Diagnostics;

/// <summary>
/// The one and only <see cref="IUserNotifier"/>. Logs every notification, hands it to an
/// <see cref="IUserNotificationPresenter"/>, and guarantees that a confirmation is never answered
/// by anything except a human.
/// </summary>
/// <remarks>
/// <para>
/// PORT-PLAN.md §7.3 splits the Delphi's <c>ILog</c> in two. This is the "asking" half. The
/// Delphi raised its dialogs from inside <c>TPlainTextLog.Event</c>, <em>while holding the log
/// lock</em> (<c>Emetra.Logging.PlainText.pas:397-423</c>): logging blocked on a user, and a second
/// thread logging anything at all blocked behind the dialog. Here the two are strictly ordered and
/// unrelated - the <see cref="ILogger"/> call completes before the presenter is ever entered, and no
/// lock of any kind is held across it.
/// </para>
/// <para>
/// <strong>Why <see cref="ConfirmAsync"/> cannot fail open.</strong> The Delphi's
/// <c>LogYesNo</c> could return "yes" without asking: <c>PrepareButtonsYesNo</c> set the default
/// button to <c>mbYes</c>, and any severity below <c>ThresholdForDialog</c> skipped the dialog and
/// took the default (<c>Emetra.Logging.Base.pas:135-146</c>, <c>:257-267</c>). Its one reachable
/// QuickStat call site deletes a package (<c>MainQuickStat.pas:894</c>). Four properties make that
/// impossible here, and none of them is a comment:
/// </para>
/// <list type="number">
///   <item>
///     <description>
///     <strong>No threshold exists.</strong> <see cref="NotificationSeverity"/> selects an icon and
///     a log level. It is never compared against anything, so no severity can route around the
///     question. There is nothing to misconfigure because there is no configuration.
///     </description>
///   </item>
///   <item>
///     <description>
///     <strong>No state.</strong> This class has no properties, no settable fields and no options
///     object. There is no "default answer" to set, and nothing about one call can affect the next -
///     unlike the Delphi, where the answer lived in a shared <c>fModalResult</c> field.
///     </description>
///   </item>
///   <item>
///     <description>
///     <strong>One source of a "yes".</strong> The answer variable is definitely assigned in exactly
///     two places: the presenter's return value, and the literal <see langword="false"/> in the
///     catch block. No other statement in the method can produce <see langword="true"/>.
///     </description>
///   </item>
///   <item>
///     <description>
///     <strong>Every failure resolves to "no".</strong> A presenter that throws, returns a faulted
///     task, returns a cancelled task, or returns <see langword="null"/> instead of a task all end
///     in the same catch block. So does a missing presenter, because
///     <c>AddQuickStatDiagnostics</c> always registers one, and its default answer is no.
///     </description>
///   </item>
/// </list>
/// </remarks>
public sealed class UserNotifier : IUserNotifier
{
    private readonly IUserNotificationPresenter _presenter;
    private readonly ILogger _logger;

    /// <summary>Creates a notifier over a presenter.</summary>
    /// <param name="presenter">Where notifications are shown.</param>
    /// <param name="logger">Where notifications are recorded.</param>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null"/>.</exception>
    public UserNotifier(IUserNotificationPresenter presenter, ILogger<UserNotifier> logger)
    {
        ArgumentNullException.ThrowIfNull(presenter);
        ArgumentNullException.ThrowIfNull(logger);

        _presenter = presenter;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task InformAsync(string message, string? title = null)
        => PresentAsync(message, NotificationSeverity.Information, title);

    /// <inheritdoc />
    public Task WarnAsync(string message, string? title = null)
        => PresentAsync(message, NotificationSeverity.Warning, title);

    /// <inheritdoc />
    public Task ErrorAsync(string message, string? title = null)
        => PresentAsync(message, NotificationSeverity.Error, title);

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(
        string message,
        NotificationSeverity severity = NotificationSeverity.Warning,
        string? title = null)
    {
        UserNotification question = Build(message, severity, title, isQuestion: true);

        // The raw message, never question.Message: ForDisplay has already stripped the handlebars
        // and kept their content, so redacting that would find nothing left to redact.
        Record(severity, message, isQuestion: true);

        // Definitely assigned in exactly two places below. One of them is a literal false; the other
        // is the presenter, i.e. the user. There is no third.
        bool answeredYes;

        try
        {
            answeredYes = await _presenter.AskAsync(question).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // Covers a throwing presenter, a faulted task, a cancelled task, and a null task.
            // A confirmation that cannot be put to the user has not been answered, and an
            // unanswered confirmation is no.
            _logger.LogError(
                exception,
                "Confirmation could not be presented, so it is answered no: {Question}",
                PiiRedactor.ForLog(message));

            answeredYes = false;
        }

        _logger.LogInformation(
            "Confirmation answered {Answer}: {Question}",
            answeredYes ? "yes" : "no",
            PiiRedactor.ForLog(message));

        return answeredYes;
    }

    private async Task PresentAsync(string message, NotificationSeverity severity, string? title)
    {
        UserNotification notification = Build(message, severity, title, isQuestion: false);

        // The raw message, never notification.Message - see ConfirmAsync.
        Record(severity, message, isQuestion: false);

        try
        {
            await _presenter.PresentAsync(notification).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            // The message is already in the log by this point, which is the durable half. Failing to
            // draw a dialog must not take down the operation that wanted to report something -
            // least of all when that operation is itself reporting a failure.
            _logger.LogError(
                exception,
                "Notification could not be presented: {Message}",
                PiiRedactor.ForLog(message));
        }
    }

    private static UserNotification Build(
        string message,
        NotificationSeverity severity,
        string? title,
        bool isQuestion)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new UserNotification(PiiRedactor.ForDisplay(message), title, severity, isQuestion);
    }

    /// <summary>
    /// Writes the notification to the log at the level PORT-PLAN.md §7.3 maps it to, redacted,
    /// so the log file still contains everything the Delphi log contained and nothing it should not.
    /// </summary>
    private void Record(NotificationSeverity severity, string rawMessage, bool isQuestion)
    {
        string text = PiiRedactor.ForLog(rawMessage);
        string kind = isQuestion ? "Asking" : "Notifying";

        switch (severity)
        {
            case NotificationSeverity.Error:
                _logger.LogError("{Kind} user: {Message}", kind, text);

                break;

            case NotificationSeverity.Warning:
                _logger.LogWarning("{Kind} user: {Message}", kind, text);

                break;

            case NotificationSeverity.Information:
            default:
                _logger.LogInformation("{Kind} user: {Message}", kind, text);

                break;
        }
    }
}
