using QuickStat.Configuration;
using QuickStat.Data;

namespace QuickStat.Services;

/// <summary>
/// The whole of <c>SelectConnection</c>: status text, busy state, login, caption load, done.
/// </summary>
/// <remarks>
/// <para>
/// Delphi: <c>TfrmQuickStat.SelectConnection</c> (<c>MainQuickStat.pas:495-519</c>), wired to
/// <c>cbProject.OnChange</c>. The combo box lives on the Population tab, so step 3.2 owns the
/// control - but the sequence it triggers is shell behaviour, and one piece of it is not the
/// Population tab's business at all: <see cref="QuickStat.Domain.Matrix.ICaptionLoader"/> must run
/// once a session exists, or every lab column falls back to its raw variable name with an empty
/// header tooltip (PORT-PLAN.md §8.8, "nobody loaded the captions").
/// </para>
/// <para>
/// So step 3.1 owns the sequence and step 3.2 owns the control:
/// <c>SelectedProject</c>'s setter awaits <see cref="ConnectAsync"/> and does nothing else.
/// </para>
/// <para>
/// The captions are loaded <em>before</em> this returns rather than in the background, so a user who
/// connects and immediately collects still gets titled columns. It is one query over a small
/// reference table.
/// </para>
/// </remarks>
public interface IConnectionCoordinator
{
    /// <summary>Disconnects, connects to <paramref name="connection"/>, and loads the captions.</summary>
    /// <param name="connection">The catalogue entry the user picked.</param>
    /// <param name="cancellationToken">Cancels the connect.</param>
    /// <returns>The established session.</returns>
    /// <remarks>
    /// Reports through <see cref="IShellProgress"/> as it goes: <c>New project selected</c>, then
    /// <c>Connecting to &lt;name&gt; ...</c>, then whatever the login pipeline reports, and finally
    /// <c>Task completed</c>. On failure the status line turns red and the exception propagates -
    /// the caller decides whether to notify.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
    Task<SessionContext> ConnectAsync(QuickStatConnection connection, CancellationToken cancellationToken = default);

    /// <summary>Closes the session. Safe when already disconnected.</summary>
    /// <param name="cancellationToken">Bounds the close.</param>
    /// <returns>A task that completes when the connection is closed.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
