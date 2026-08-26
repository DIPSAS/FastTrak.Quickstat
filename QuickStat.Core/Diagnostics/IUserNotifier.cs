namespace QuickStat.Diagnostics;

/// <summary>
/// Telling the user something and asking them something. The half of the Delphi's <c>ILog</c> that
/// was never logging at all.
/// </summary>
/// <remarks>
/// <para>
/// PORT-PLAN.md §7.3: <c>ILog</c> splits into <c>ILogger&lt;T&gt;</c> and this. In the Delphi every
/// <c>Event</c> at <c>ltMessage</c> or above raised a modal dialog as a side effect of logging -
/// and did so <em>while holding the log lock</em> (<c>Emetra.Logging.PlainText.pas:397-423</c>).
/// About 35 call sites depended on that, including several developer diagnostics that interrupted
/// end users. Each one is now an explicit, reviewable decision at the call site.
/// </para>
/// <para>
/// Implementations also write the message to <c>ILogger</c> at the matching level, so the log file
/// still contains everything the Delphi log contained. They marshal to the UI thread themselves;
/// callers must not.
/// </para>
/// </remarks>
public interface IUserNotifier
{
    /// <summary>Shows an informational message.</summary>
    /// <param name="message">Text to show.</param>
    /// <param name="title">Caption, or <see langword="null"/> for the application name.</param>
    /// <returns>A task that completes when the user dismisses it.</returns>
    Task InformAsync(string message, string? title = null);

    /// <summary>Shows a warning.</summary>
    /// <param name="message">Text to show.</param>
    /// <param name="title">Caption, or <see langword="null"/> for the application name.</param>
    /// <returns>A task that completes when the user dismisses it.</returns>
    Task WarnAsync(string message, string? title = null);

    /// <summary>Shows an error.</summary>
    /// <param name="message">Text to show.</param>
    /// <param name="title">Caption, or <see langword="null"/> for the application name.</param>
    /// <returns>A task that completes when the user dismisses it.</returns>
    Task ErrorAsync(string message, string? title = null);

    /// <summary>Asks a yes/no question and waits for the answer.</summary>
    /// <param name="message">The question.</param>
    /// <param name="severity">Icon and emphasis.</param>
    /// <param name="title">Caption, or <see langword="null"/> for the application name.</param>
    /// <returns><see langword="true"/> only if the user actually answered yes.</returns>
    /// <remarks>
    /// Delphi: <c>ILog.LogYesNo</c>, which <em>failed open</em>. Below the dialog threshold it
    /// returned the default button - yes - without asking anything
    /// (<c>Emetra.Logging.Base.pas:143-146</c>). Its one reachable QuickStat call site is the
    /// delete-package confirmation (<c>MainQuickStat.pas:894</c>), so the fail-open path was one
    /// configuration change away from deleting a package without asking. Implementations of this
    /// method must always ask.
    /// </remarks>
    Task<bool> ConfirmAsync(
        string message,
        NotificationSeverity severity = NotificationSeverity.Warning,
        string? title = null);
}
