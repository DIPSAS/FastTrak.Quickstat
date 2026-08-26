using System.Globalization;
using Microsoft.Extensions.Logging;
using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Domain.Matrix;

namespace QuickStat.Services;

/// <summary>The one implementation of <see cref="IConnectionCoordinator"/>.</summary>
public sealed class ConnectionCoordinator : IConnectionCoordinator
{
    /// <summary>Status line the moment the user picks a different project. Delphi <c>TXT_PROJECT_SELECTED</c>.</summary>
    public const string ProjectSelectedText = "New project selected";

    /// <summary>Status line while connecting. Delphi <c>TXT_CONNECTING</c>, one <c>%s</c>.</summary>
    public const string ConnectingFormat = "Connecting to {0} ...";

    private readonly ISessionService _session;
    private readonly ICaptionLoader _captions;
    private readonly IShellProgress _progress;
    private readonly ILogger<ConnectionCoordinator> _logger;

    /// <summary>Creates the coordinator.</summary>
    /// <param name="session">Login pipeline.</param>
    /// <param name="captions">Fills the caption dictionary once a session exists.</param>
    /// <param name="progress">Status line, percentage and busy flag.</param>
    /// <param name="logger">Log.</param>
    public ConnectionCoordinator(
        ISessionService session,
        ICaptionLoader captions,
        IShellProgress progress,
        ILogger<ConnectionCoordinator> logger)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(captions);
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentNullException.ThrowIfNull(logger);

        _session = session;
        _captions = captions;
        _progress = progress;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SessionContext> ConnectAsync(
        QuickStatConnection connection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using IDisposable operation = _progress.BeginOperation(ProjectSelectedText);

        try
        {
            _progress.SetInfo(string.Format(CultureInfo.CurrentCulture, ConnectingFormat, connection.Name));

            SessionContext session = await _session.ConnectAsync(connection, _progress, cancellationToken)
                .ConfigureAwait(true);

            // Captions are cosmetic and ICaptionLoader never throws for a caption failure, so this
            // needs no guard - but it does propagate cancellation, which the caller handles below.
            int captionCount = await _captions.LoadAsync(cancellationToken).ConfigureAwait(true);

            _logger.LogInformation(
                "Connected to {Connection} (study {Study}); {CaptionCount} database captions loaded.",
                connection.Name,
                session.StudyName,
                captionCount);

            _progress.Done();

            return session;
        }
        catch (OperationCanceledException)
        {
            _progress.SetInfo(ShellProgress.IdleText);

            throw;
        }
        catch (Exception exception)
        {
            // The status line is the Delphi's only failure surface here.  Whether to raise a dialog
            // as well is the caller's decision, because only the caller knows whether the user asked
            // for this connect or it happened as part of something larger.
            _progress.Fail(exception.Message);

            _logger.LogError(exception, "Could not connect to {Connection}.", connection.Name);

            throw;
        }
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default) =>
        _session.DisconnectAsync(cancellationToken);
}
