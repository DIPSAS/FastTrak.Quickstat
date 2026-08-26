using QuickStat.Configuration;
using QuickStat.Diagnostics;

namespace QuickStat.Data;

/// <summary>
/// Owns the connection and the login pipeline: pick a project, connect, work, disconnect.
/// </summary>
/// <remarks>
/// <para>
/// The Delphi did all of this synchronously on the UI thread behind a wait cursor - roughly 55
/// round trips for one combo-box change (<c>Docs/Port/01-data-access.md</c> §1.4). Here it is one
/// awaitable, cancellable operation that reports progress.
/// </para>
/// <para>
/// Connecting when a session is already open closes the old one first, including
/// <c>EXEC dbo.CloseSession</c>, which the Delphi did but without notifying anyone
/// (<c>CRF.Context.Facade.pas:237-243</c>).
/// </para>
/// </remarks>
public interface ISessionService
{
    /// <summary>The current session, or <see langword="null"/> when disconnected.</summary>
    SessionContext? Current { get; }

    /// <summary>Whether a usable session is open.</summary>
    /// <remarks>
    /// Unlike the Delphi, a login step that throws leaves this <see langword="false"/>. There, a
    /// failing observer aborted the login but left the ADO connection open, so <c>Connected</c>
    /// returned true after a partially failed login (<c>Docs/Port/01-data-access.md</c> §1.6).
    /// </remarks>
    bool IsConnected { get; }

    /// <summary>
    /// Raised after <see cref="Current"/> changes, with the new session or <see langword="null"/>.
    /// </summary>
    /// <remarks>Delphi: <c>NotifyStudyObservers</c> (<c>CRF.Context.Session.pas:279-298</c>).</remarks>
    event EventHandler<SessionContext?>? SessionChanged;

    /// <summary>Disconnects any current session, connects, and runs the login pipeline.</summary>
    /// <param name="connection">The catalogue entry the user picked.</param>
    /// <param name="progress">Per-step progress, or <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancels the login.</param>
    /// <returns>The established session.</returns>
    Task<SessionContext> ConnectAsync(
        QuickStatConnection connection,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>Closes the session row and the connection. Safe to call when already disconnected.</summary>
    /// <param name="cancellationToken">Bounds the close; shutdown must never block on it.</param>
    /// <returns>A task that completes when the connection is closed.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
