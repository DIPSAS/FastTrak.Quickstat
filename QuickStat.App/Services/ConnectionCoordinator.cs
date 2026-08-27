using System.Globalization;
using Microsoft.Extensions.Logging;
using QuickStat.Collectors;
using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Domain.Matrix;
using QuickStat.Logging;

namespace QuickStat.Services;

/// <summary>The one implementation of <see cref="IConnectionCoordinator"/>.</summary>
public sealed class ConnectionCoordinator : IConnectionCoordinator
{
    /// <summary>Status line the moment the user picks a different project. Delphi <c>TXT_PROJECT_SELECTED</c>.</summary>
    public const string ProjectSelectedText = "New project selected";

    /// <summary>Status line while connecting. Delphi <c>TXT_CONNECTING</c>, one <c>%s</c>.</summary>
    public const string ConnectingFormat = "Connecting to {0} ...";

    /// <summary>
    /// Status line while the collector list is being built. Delphi <c>TXT_LOADING_COLLECTORS</c>
    /// (<c>QuickStat.Collectors.pas:86</c>), set by <c>TQuickStatCollectors.PrepareStudy</c>
    /// (<c>:125</c>) and listed in <c>05-ui-spec.md</c> §G.6.
    /// </summary>
    /// <remarks>
    /// It used to live on <c>CollectionsTabViewModel</c>, because that is where the build was
    /// started from. It follows the build here (PORT-PLAN.md §8.10 (g)): the tab no longer runs the
    /// query, so it can no longer honestly announce it.
    /// </remarks>
    public const string LoadingCollectorsText = "Loading collectors";

    private readonly ISessionService _session;
    private readonly ICaptionLoader _captions;
    private readonly ICollectorRegistry _registry;
    private readonly IShellProgress _progress;
    private readonly ILogger<ConnectionCoordinator> _logger;

    /// <summary>
    /// The connect currently in flight, so a second one - or a disconnect - can call it off.
    /// </summary>
    /// <remarks>
    /// The same shape as <c>PopulationPickerViewModel.BeginCatalogueLoad</c>, and here for the same
    /// reason: the work behind it is several round trips and the trigger can arrive again before it
    /// finishes. See <see cref="ConnectAsync"/>.
    /// </remarks>
    private CancellationTokenSource? _inFlight;

    /// <summary>Creates the coordinator.</summary>
    /// <param name="session">Login pipeline.</param>
    /// <param name="captions">Fills the caption dictionary once a session exists.</param>
    /// <param name="registry">Builds the data-element list for the study just connected to.</param>
    /// <param name="progress">Status line, percentage and busy flag.</param>
    /// <param name="logger">Log.</param>
    public ConnectionCoordinator(
        ISessionService session,
        ICaptionLoader captions,
        ICollectorRegistry registry,
        IShellProgress progress,
        ILogger<ConnectionCoordinator> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(captions);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(logger);

        _session = session;
        _captions = captions;
        _registry = registry;
        _progress = progress;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SessionContext> ConnectAsync(
        QuickStatConnection connection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        CancellationTokenSource started = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Whoever was connecting is no longer wanted: their answer would land in a registry and a
        // check list that now belong to somebody else.  Cancelled, not disposed - the losing call
        // owns its own source and disposes it in its own finally.
        CancelInFlight(Interlocked.Exchange(ref _inFlight, started));

        using IDisposable operation = _progress.BeginOperation(ProjectSelectedText);

        try
        {
            _progress.SetInfo(string.Format(CultureInfo.CurrentCulture, ConnectingFormat, connection.Name));

            SessionContext session = await _session.ConnectAsync(connection, _progress, started.Token)
                .ConfigureAwait(true);

            // Captions are cosmetic and ICaptionLoader never throws for a caption failure, so this
            // needs no guard - but it does propagate cancellation, which the caller handles below.
            int captionCount = await _captions.LoadAsync(started.Token).ConfigureAwait(true);

            _progress.SetInfo(LoadingCollectorsText);

            IReadOnlyList<ICollector> collectors =
                await _registry.BuildAsync(session, started.Token).ConfigureAwait(true);

            // Which server, which database, which study - see StartupLog. The first question about
            // any report from the field is "pointed at what?", and this is the line that answers it.
            // It reads SessionContext's own fields rather than parsing the connection string, so
            // there is no credential here to leak.
            _logger.LogSession(connection.Name, session);

            _logger.LogInformation(
                "Connected to {Connection} (study {Study}); {CaptionCount} database captions and " +
                "{CollectorCount} data elements loaded.",
                connection.Name,
                session.StudyName,
                captionCount,
                collectors.Count);

            _progress.Done();

            return session;
        }
        catch (OperationCanceledException)
        {
            // Only if we are still the connect the shell is waiting for.  A superseded one must not
            // write over the status line of the connect that replaced it.
            if (IsCurrent(started))
            {
                _progress.SetInfo(ShellProgress.IdleText);
            }

            throw;
        }
        catch (Exception exception)
        {
            // The status line is the Delphi's only failure surface here.  Whether to raise a dialog
            // as well is the caller's decision, because only the caller knows whether the user asked
            // for this connect or it happened as part of something larger.
            if (IsCurrent(started))
            {
                _progress.Fail(exception.Message);
            }

            _logger.LogError(exception, "Could not connect to {Connection}.", connection.Name);

            throw;
        }
        finally
        {
            _ = Interlocked.CompareExchange(ref _inFlight, null, started);

            started.Dispose();
        }
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        // A connect that is still running would otherwise finish into a session that is being torn
        // down, and install its collector list over the cleared one.
        CancelInFlight(Interlocked.Exchange(ref _inFlight, null));

        return _session.DisconnectAsync(cancellationToken);
    }

    /// <summary>Cancels a superseded connect, tolerating the one it has already finished.</summary>
    /// <param name="source">The source taken off <see cref="_inFlight"/>, or <see langword="null"/>.</param>
    /// <remarks>
    /// Its owner disposes it in <see cref="ConnectAsync"/>'s <c>finally</c>, and that can happen
    /// between the exchange above and the call here. A disposed source means the connect finished
    /// under its own steam, which is exactly the case where there is nothing to cancel.
    /// </remarks>
    private static void CancelInFlight(CancellationTokenSource? source)
    {
        try
        {
            source?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private bool IsCurrent(CancellationTokenSource started) =>
        ReferenceEquals(Volatile.Read(ref _inFlight), started);
}
