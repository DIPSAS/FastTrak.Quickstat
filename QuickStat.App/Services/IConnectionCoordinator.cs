using QuickStat.Collectors;
using QuickStat.Configuration;
using QuickStat.Data;

namespace QuickStat.Services;

/// <summary>
/// The whole of <c>SelectConnection</c>: status text, busy state, login, caption load, collector
/// list, done.
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
/// <para>
/// <b>So is <see cref="ICollectorRegistry.BuildAsync"/></b>, and for a stronger reason - PORT-PLAN.md
/// §8.10 (g). It used to hang off <c>ISessionService.SessionChanged</c> and run unawaited, which is
/// not what the Delphi does: the list is filled by <c>TfrmQuickStat.AfterLogin</c>
/// (<c>MainQuickStat.pas:471-493</c>), a login observer that <c>TSimpleDatabase.Connect</c> calls
/// synchronously (<c>Emetra.Database.Simple.pas:391-406</c>), so <c>Connect</c> cannot return -
/// and <c>SelectConnection</c> cannot give the mouse back - until <c>cbDataCollector</c> is
/// populated. Awaiting it here restores that: <b>a session handed back by
/// <see cref="ConnectAsync"/> is one whose data elements exist.</b>
/// </para>
/// </remarks>
public interface IConnectionCoordinator
{
    /// <summary>
    /// Disconnects, connects to <paramref name="connection"/>, loads the captions, and builds the
    /// collector list.
    /// </summary>
    /// <param name="connection">The catalogue entry the user picked.</param>
    /// <param name="cancellationToken">Cancels the connect.</param>
    /// <returns>The established session, with <see cref="ICollectorRegistry.Collectors"/> filled.</returns>
    /// <remarks>
    /// <para>
    /// Reports through <see cref="IShellProgress"/> as it goes: <c>New project selected</c>, then
    /// <c>Connecting to &lt;name&gt; ...</c>, then whatever the login pipeline reports, then
    /// <c>Loading collectors</c>, and finally <c>Task completed</c>. On failure the status line turns
    /// red and the exception propagates - the caller decides whether to notify.
    /// </para>
    /// <para>
    /// <b>A failed collector build fails the connect, and leaves the session open.</b> Both halves
    /// are deliberate.
    /// </para>
    /// <para>
    /// It <em>fails</em> because the alternative - the fire-and-forget this replaced - reported the
    /// failure into a log line and a status line that <c>Task completed</c> then raced to overwrite,
    /// and left the user connected to a project with no data elements and no clue why. It is not the
    /// case that a missing database object gets here: that is
    /// <see cref="CollectorAvailability"/>'s job, and it drops the collector, logs the skip and
    /// carries on, so the degradation the port is designed for happens <em>inside</em> the build.
    /// What escapes is a round trip that failed - <c>EXEC Report.GetFormClasses</c> or the
    /// <c>OBJECT_ID</c> probe - and a database that cannot answer those cannot be collected from at
    /// all. Delphi agrees: a throwing <c>AfterLogin</c> becomes <c>EDatabaseLoginObserverError</c>
    /// (<c>Emetra.Database.Simple.pas:397-404</c>) and takes the whole <c>Connect</c> down.
    /// </para>
    /// <para>
    /// It leaves the session open because that is also what the Delphi does - the observer aborts
    /// <c>Connect</c> but nothing rolls the ADO connection back, so <c>Connected</c> still answers
    /// true (<c>Docs/Port/01-data-access.md</c> §1.6) - and because the session <em>is</em> sound:
    /// the login pipeline finished, the session row is open and the population list works. Closing
    /// it would turn one failed query into a torn-down connection, which is a bigger punishment than
    /// the fault deserves. <see cref="QuickStat.Data.ISessionService.IsConnected"/> is therefore not
    /// a promise that the collector list exists; a successful return from here is.
    /// </para>
    /// <para>
    /// <b>Calling this again while it is running cancels the first call</b>, which then throws
    /// <see cref="OperationCanceledException"/>. Without that, two overlapping connects race to
    /// install their study's list, and the loser can win - the shipped Delphi cannot reach the state
    /// at all, because <c>SelectConnection</c> holds the message loop, but the busy overlay here
    /// blocks only the mouse (PORT-PLAN.md §8.10 (f)) and the project drop-down is still reachable
    /// from the keyboard. <see cref="DisconnectAsync"/> cancels it for the same reason.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException">
    /// The connect was cancelled, or superseded by a later one.
    /// </exception>
    Task<SessionContext> ConnectAsync(QuickStatConnection connection, CancellationToken cancellationToken = default);

    /// <summary>Closes the session. Safe when already disconnected.</summary>
    /// <param name="cancellationToken">Bounds the close.</param>
    /// <returns>A task that completes when the connection is closed.</returns>
    /// <remarks>Cancels a connect that is still in flight; see <see cref="ConnectAsync"/>.</remarks>
    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
