using Microsoft.Extensions.Logging;
using QuickStat.Diagnostics;
using QuickStat.Domain.Populations;

namespace QuickStat.Services;

/// <summary>
/// The WPF <see cref="IPeriodPrompt"/>: the <c>Angi periode</c> dialog.
/// </summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.6. This is a stub and it always cancels.</b> Step 2.3 declared
/// <see cref="IPeriodPrompt"/> and deliberately left it unregistered, because it shows a window;
/// step 3.1 registers this so the container resolves and the application runs. Cancelling is the
/// safe answer - it aborts the population load, which is what a real cancel does and what
/// PORT-PLAN.md §7.2 requires - and the user is told rather than left watching nothing happen.
/// </para>
/// <para>
/// <b>What 3.6 has to build</b>, from <c>05-ui-spec.md</c> §D.5 and
/// <c>Emetra.VclForm.Period.dfm</c>:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <c>Views/Dialogs/PeriodDialog.xaml</c> - 527 x 374, centred on the owner, white top banner,
///     two Monday-first <c>Calendar</c> controls, <c>FirstYear = 1900</c>. Fully Norwegian: header
///     <c>Angi periode</c>, sub-header set at run time from the <c>caption</c> argument, buttons
///     <c>OK</c> and <c>Avbryt</c> at 84 x 36.
///   </description></item>
///   <item><description>
///     <c>OK</c> disabled while <c>start &gt;= stop</c>. The period is half-open - see
///     <see cref="HalfOpenPeriod.IsValid"/> - and the dialog says so:
///     <c>Angis som fra og med første dato (til venstre), og til men ikke inkludert siste dato (til
///     høyre).</c>
///   </description></item>
///   <item><description>
///     Remember the last range <b>per query</b> through <see cref="QuickStat.Configuration.Settings.ISettingsStore"/>, keyed with
///     <see cref="PeriodSettingsKey.For"/> - <em>not</em> the raw SQL. The Delphi swapped the
///     section and key arguments and used the whole statement as the key, so the value could never
///     be read back and every prompt opened on yesterday-and-today. Fixing it means the feature
///     starts working for the first time.
///   </description></item>
///   <item><description>
///     The <c>context</c> argument is what to key on; it is the population's <c>SqlText</c>.
///   </description></item>
/// </list>
/// <para>
/// When you implement it, delete <see cref="IUserNotifier"/> from the constructor - a working dialog
/// has nothing to apologise for.
/// </para>
/// </remarks>
public sealed class WpfPeriodPrompt : IPeriodPrompt
{
    /// <summary>What the stub tells the user. Removed when 3.6 lands the real dialog.</summary>
    public const string NotAvailableMessage =
        "The period dialog has not been built yet, so this query cannot be run.\n\n"
        + "The population needs a date range, and there is no way to supply one in this build.";

    private readonly IUserNotifier _notifier;
    private readonly ILogger<WpfPeriodPrompt> _logger;

    /// <summary>Creates the stub prompt.</summary>
    /// <param name="notifier">Tells the user why the load stopped.</param>
    /// <param name="logger">Log.</param>
    public WpfPeriodPrompt(IUserNotifier notifier, ILogger<WpfPeriodPrompt> logger)
    {
        ArgumentNullException.ThrowIfNull(notifier);
        ArgumentNullException.ThrowIfNull(logger);

        _notifier = notifier;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HalfOpenPeriod?> TryGetPeriodAsync(
        string context,
        string caption,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogWarning(
            "A query asked for a period and the dialog is not implemented (step 3.6); treating it as cancelled.");

        await _notifier.WarnAsync(NotAvailableMessage).ConfigureAwait(true);

        return null;
    }
}
