using System.Diagnostics.CodeAnalysis;
using QuickStat.Data;

namespace QuickStat.Collectors;

/// <summary>
/// Builds the ordered list of data elements offered for a session, and finds one by name.
/// </summary>
/// <remarks>
/// Delphi: <c>TQuickStatCollectors.PrepareStudy</c> (<c>QuickStat.Collectors.pas:123-138</c>) and
/// its five <c>AddCollectors*</c> procedures.
/// </remarks>
public interface ICollectorRegistry
{
    /// <summary>The collectors for the current session; empty before <see cref="BuildAsync"/>.</summary>
    IReadOnlyList<ICollector> Collectors { get; }

    /// <summary>
    /// Raised at the end of a successful <see cref="BuildAsync"/>, with the new
    /// <see cref="Collectors"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It exists to carry an ordering guarantee that <c>ISessionService.SessionChanged</c> cannot</b>
    /// (PORT-PLAN.md §8.10 (g)). It is raised <em>inside</em> <see cref="BuildAsync"/>, before that
    /// task completes, so whoever awaits the build knows every handler has already run. The session
    /// event gives no such promise: three view-models hang off it, an event fixes no order between
    /// them, and the one that used to start the build from there could still be working long after
    /// <c>IConnectionCoordinator.ConnectAsync</c> had reported the connection as finished.
    /// </para>
    /// <para>
    /// This is the port's stand-in for the fact that Delphi's list was filled by
    /// <c>TfrmQuickStat.AfterLogin</c> (<c>MainQuickStat.pas:471-493</c>) - a login observer called
    /// synchronously from inside <c>TSimpleDatabase.Connect</c>
    /// (<c>Emetra.Database.Simple.pas:391-406</c>), so <c>Connect</c> could not return until the
    /// check list was populated.
    /// </para>
    /// <para>
    /// Raised on whichever thread ran the build, which in the shell is a thread-pool thread; a
    /// handler that touches bound state must marshal. Nothing is raised for a failed or cancelled
    /// build - <see cref="Collectors"/> is not replaced in that case either.
    /// </para>
    /// </remarks>
    event EventHandler<IReadOnlyList<ICollector>>? Rebuilt;

    /// <summary>Rebuilds the list for a session.</summary>
    /// <param name="session">Supplies the study name for gating and the study id for the SQL.</param>
    /// <param name="cancellationToken">Cancels the build.</param>
    /// <returns>The collectors, in registration order.</returns>
    /// <remarks>
    /// <para>
    /// Order is part of the contract. It is the order of the check-list users know, and
    /// <see cref="TryFind"/> takes the first match, so re-ordering changes behaviour as well as
    /// appearance. The sequence is: the always-on elements in source order, then <c>2 x N</c>
    /// dynamic form collectors from <c>EXEC Report.GetFormClasses :StudyId</c> - skipping names
    /// matching <c>FORM\d+</c> and de-duplicating - then each gated family in Delphi order
    /// (<see cref="StudyGate.Gbd"/> and its diagnosis and drug sub-families,
    /// <see cref="StudyGate.Ndv"/>, <see cref="StudyGate.Gwas"/>, <see cref="StudyGate.Roas"/>,
    /// <see cref="StudyGate.Dogfood"/>).
    /// </para>
    /// <para>
    /// Descriptors whose <see cref="CollectorAvailability"/> is not satisfied are dropped here, so
    /// they never reach the list. The object probe is one round trip for the whole registry.
    /// </para>
    /// <para>
    /// <b>Awaited by <c>IConnectionCoordinator.ConnectAsync</c></b>, which is what makes a returned
    /// session mean "the list is ready" (PORT-PLAN.md §8.10 (g)). It therefore throws where it used
    /// to be swallowed: a failure here fails the connect. A database that is merely <em>missing</em>
    /// an object is not a failure - that is what <see cref="CollectorAvailability"/> is for, and it
    /// drops the collector and logs the skip.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<ICollector>> BuildAsync(SessionContext session, CancellationToken cancellationToken = default);

    /// <summary>Finds a collector by name or by displayed title.</summary>
    /// <param name="nameOrTitle">A <see cref="CollectorDescriptor.Name"/> or a <see cref="CollectorDescriptor.Title"/>.</param>
    /// <param name="collector">The first match.</param>
    /// <returns><see langword="true"/> when found.</returns>
    /// <remarks>
    /// Matches both, case-insensitively, exactly as <c>TryFindCollector</c> does - which is why
    /// titles have to stay unique as well as names. This is how a saved package re-ticks its stored
    /// elements; a name that no longer exists produces one warning per package, not one per element
    /// (<c>MainQuickStat.pas:803</c> pops a modal dialog inside the loop).
    /// </remarks>
    bool TryFind(string nameOrTitle, [NotNullWhen(true)] out ICollector? collector);
}
