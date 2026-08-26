using QuickStat.Diagnostics;

namespace QuickStat.ViewModels;

/// <summary>
/// One <see cref="UserNotification"/>, ready for the themed dialog to render.
/// </summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.6.</b> A view-model rather than code-behind so that the glyph choice - the one
/// piece of behaviour in the notification chrome - can be checked without a window, and so
/// <c>NotificationDialog.xaml</c> stays a layout.
/// </para>
/// <para>
/// It is immutable and has no dependencies: the presenter builds one per notification and throws it
/// away with the window, so it is not registered with the container.
/// </para>
/// <para>
/// <b>It adds no behaviour to <see cref="IUserNotifier"/> and must not.</b> Severity mapping to a
/// log level, PII redaction and the never-fail-open rule all live in <c>QuickStat.Core</c>. What is
/// here is the icon, the caption and the button captions - the part that genuinely needs a screen.
/// </para>
/// </remarks>
public sealed class NotificationViewModel
{
    /// <summary>Caption used when a notification carries none.</summary>
    public const string DefaultTitle = "QuickStat";

    /// <summary>The single button on a statement. Delphi <c>mbOK</c>.</summary>
    public const string DismissCaption = "OK";

    /// <summary>Affirmative answer to a confirmation. Delphi <c>mbYes</c>.</summary>
    public const string YesCaption = "Yes";

    /// <summary>Negative answer to a confirmation. Delphi <c>mbNo</c>.</summary>
    public const string NoCaption = "No";

    /// <summary>Segoe MDL2 Assets <c>Info</c>. Delphi <c>mtInformation</c>.</summary>
    /// <remarks>
    /// Written as an escape, exactly as <c>QuickStat.Theme.SegoeIcons</c> is and for the same
    /// reason: these are private-use code points, so a literal renders as a box in every editor and
    /// diff and is silently destroyed by any tool that re-encodes the file.
    /// </remarks>
    public const string InformationGlyph = "\uE946";

    /// <summary>Segoe MDL2 Assets <c>Warning</c>. Delphi <c>mtWarning</c>.</summary>
    public const string WarningGlyph = "\uE7BA";

    /// <summary>Segoe MDL2 Assets <c>ErrorBadge</c>. Delphi <c>mtError</c>.</summary>
    public const string ErrorGlyph = "\uEA39";

    /// <summary>Segoe MDL2 Assets <c>Help</c>. Delphi <c>mtConfirmation</c>.</summary>
    public const string QuestionGlyph = "\uE897";

    /// <summary>Wraps a notification.</summary>
    /// <param name="notification">What to show. Its message is already redacted for display.</param>
    public NotificationViewModel(UserNotification notification)
    {
        // Rendered verbatim: UserNotifier has already run PiiRedactor.ForDisplay over it, which
        // strips the handlebars and turns the literal \n escapes in MainQuickStat.pas's resource
        // strings into real line breaks.  Converting again here would be wrong twice over.
        Message = notification.Message;
        Title = string.IsNullOrEmpty(notification.Title) ? DefaultTitle : notification.Title;
        Severity = notification.Severity;
        IsQuestion = notification.IsQuestion;
    }

    /// <summary>The text, exactly as <see cref="PiiRedactor.ForDisplay"/> left it.</summary>
    public string Message { get; }

    /// <summary>The window caption.</summary>
    public string Title { get; }

    /// <summary>Which icon and emphasis to use.</summary>
    public NotificationSeverity Severity { get; }

    /// <summary>Whether this is a confirmation - two buttons - rather than a statement.</summary>
    public bool IsQuestion { get; }

    /// <summary>
    /// The caption of the button that dismisses the dialog: <c>No</c> for a confirmation,
    /// <c>OK</c> for a statement.
    /// </summary>
    /// <remarks>
    /// One button rather than two, because <c>Escape</c>, <c>Enter</c> and the close box all have to
    /// arrive at the same answer, and that answer is the safe one either way.
    /// </remarks>
    public string DismissText => IsQuestion ? NoCaption : DismissCaption;

    /// <summary>
    /// The Segoe MDL2 Assets code point for this notification.
    /// </summary>
    /// <remarks>
    /// <c>MapDlggType</c> (<c>Emetra.Logging.Base.pas:218-226</c>) maps warning to
    /// <c>mtWarning</c>, error and critical to <c>mtError</c> and everything else to
    /// <c>mtInformation</c> - and then <c>ShowCrossPlatformDialog</c> (<c>:276-277</c>) promotes
    /// <c>mtInformation</c> to <c>mtConfirmation</c> when the button set contains <c>mbNo</c>. So an
    /// informational question gets a question mark, not an <c>i</c>. In QuickStat itself the only
    /// confirmation is raised at warning level - deleting a package,
    /// <c>MainQuickStat.pas:894</c> - so the promotion is unreachable today; it is ported because
    /// the mapping is the contract, not the current call sites.
    /// </remarks>
    public string Glyph => Severity switch
    {
        NotificationSeverity.Error => ErrorGlyph,
        NotificationSeverity.Warning => WarningGlyph,
        _ => IsQuestion ? QuestionGlyph : InformationGlyph,
    };
}
