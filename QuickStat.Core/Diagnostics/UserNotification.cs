namespace QuickStat.Diagnostics;

/// <summary>
/// One thing to put in front of the user, already redacted for display and ready to render.
/// </summary>
/// <param name="Message">
/// The text to show. Already passed through <see cref="PiiRedactor.ForDisplay"/>, so handlebars are
/// gone and literal <c>\n</c> escapes have become real line breaks. A presenter renders it as-is.
/// </param>
/// <param name="Title">
/// The caption, or <see langword="null"/> for the application name.
/// </param>
/// <param name="Severity">
/// Which icon and emphasis to use.
/// </param>
/// <param name="IsQuestion">
/// <see langword="true"/> for a confirmation - yes/no buttons - and <see langword="false"/> for a
/// statement with a single dismiss button.
/// </param>
/// <remarks>
/// This exists so a presenter has one parameter rather than four, and so adding a field later does
/// not break every implementation. It carries no behaviour.
/// </remarks>
public readonly record struct UserNotification(
    string Message,
    string? Title,
    NotificationSeverity Severity,
    bool IsQuestion);
