using System.Windows;
using Microsoft.Extensions.Logging;
using QuickStat.Configuration.Settings;
using QuickStat.Domain.Populations;
using QuickStat.ViewModels;
using QuickStat.Views.Dialogs;

namespace QuickStat.Services;

/// <summary>
/// The WPF <see cref="IPeriodPrompt"/>: the <c>Angi periode</c> dialog.
/// </summary>
/// <remarks>
/// <para>
/// <b>OWNER: step 3.6.</b> Delphi: <c>TPeriodDictionary.TryGetPeriod</c>
/// (<c>EPR.PeriodDictionary.pas:54-80</c>) driving the modal <c>TfrmPeriod</c>. The sequence is the
/// Delphi's, in the same order: read the remembered pair, pre-fill the calendars, show the modal,
/// and write the pair back <em>only</em> on OK.
/// </para>
/// <para>
/// <b>The dialog is shown every time.</b> A remembered period pre-fills the calendars and nothing
/// more; there is no path that skips the prompt, in the Delphi or here
/// (<c>Emetra.Database.ParameterDictionary.pas:98-106</c> calls
/// <c>fPeriodDictionary.TryGetPeriod</c> unconditionally whenever a query declares both
/// <c>:StartDate</c> and <c>:StopDate</c>, and <c>TPeriodDictionary</c> always reaches
/// <c>ShowModal</c>).
/// </para>
/// <para>
/// <b>The key is a hash, never the statement.</b> PORT-PLAN.md §7.2: the Delphi passed the whole
/// <c>EXEC … :StartDate, :StopDate</c> text where the settings API expected a key, with the section
/// and key arguments swapped as well (<c>EPR.PeriodDictionary.pas:65-66, 75-76</c>), so nothing
/// readable was ever written and every prompt opened on yesterday-and-today.
/// Core's <c>QueryParameterResolver</c> already hashes the SQL before it calls this, so the
/// <c>context</c> argument arrives as a key rather than as a statement;
/// <see cref="PeriodSettingsKey.For"/> is applied again here anyway, so that "the query text never
/// reaches the settings file" is a property of this class and not of whoever calls it. Hashing a
/// hash is stable and costs nothing.
/// </para>
/// <para>
/// <b>Fixing this means the feature starts working for the first time.</b> Users who have never
/// seen a remembered range will now see one.
/// </para>
/// </remarks>
public sealed class WpfPeriodPrompt : IPeriodPrompt
{
    private readonly IUiDispatcher _dispatcher;
    private readonly ISettingsStore _settings;
    private readonly ILogger<WpfPeriodPrompt> _logger;

    /// <summary>Creates the prompt.</summary>
    /// <param name="dispatcher">Marshals to the user-interface thread; a modal needs one.</param>
    /// <param name="settings">Where the last range for this query is remembered.</param>
    /// <param name="logger">Log.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public WpfPeriodPrompt(IUiDispatcher dispatcher, ISettingsStore settings, ILogger<WpfPeriodPrompt> logger)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);

        _dispatcher = dispatcher;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>The range a query gets when nothing has been remembered for it.</summary>
    /// <remarks>
    /// <c>Now - 1</c> and <c>Now</c> (<c>EPR.PeriodDictionary.pas:65-66</c>), taken to midnight:
    /// the calendars have no time-of-day and a period whose bounds carry one is a half-open range
    /// nobody asked for.
    /// </remarks>
    public static (DateTime Start, DateTime Stop) DefaultPeriod => (DateTime.Today.AddDays(-1), DateTime.Today);

    /// <inheritdoc />
    /// <remarks>
    /// Returns the chosen range on OK; <see langword="null"/> on Cancel, on <c>Escape</c>, on
    /// closing the window, and on a cancelled <paramref name="cancellationToken"/>. Nothing is
    /// written to the settings file except on OK, so a cancelled prompt leaves the remembered range
    /// exactly as it was.
    /// </remarks>
    public async Task<HalfOpenPeriod?> TryGetPeriodAsync(
        string context,
        string caption,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        cancellationToken.ThrowIfCancellationRequested();

        string key = PeriodSettingsKey.For(context);
        (DateTime start, DateTime stop) = Remembered(key);

        HalfOpenPeriod? chosen = null;

        await _dispatcher.InvokeAsync(() => chosen = Show(start, stop, caption)).ConfigureAwait(true);

        if (chosen is not { } period)
        {
            _logger.LogInformation("The period dialog was cancelled.");

            return null;
        }

        Remember(key, period);

        _logger.LogInformation(
            "Period chosen: {Start:yyyy-MM-dd} up to but not including {Stop:yyyy-MM-dd}.",
            period.Start,
            period.Stop);

        return period;
    }

    private static HalfOpenPeriod? Show(DateTime start, DateTime stop, string caption)
    {
        PeriodViewModel model = new()
        {
            Start = start,
            Stop = stop,

            // TPeriodDictionary sets lblSubheader.Caption from its argument; an empty one would
            // leave the Delphi's design-time placeholder, so fall back to the real sub-header.
            SubHeaderText = string.IsNullOrWhiteSpace(caption) ? PeriodViewModel.SubHeader : caption,
        };

        // Application.Current is null under test and Owner does not accept one, so the owner is
        // chosen rather than passed through.  Without it the modal is not centred on the shell and
        // can end up behind it.
        Window? owner = Application.Current?.MainWindow;

        PeriodDialog dialog = new() { DataContext = model };

        if (owner is not null && !ReferenceEquals(owner, dialog))
        {
            dialog.Owner = owner;
        }

        // Emetra.VclForm.Period.pas:52 checks the range again after ShowModal, because VerifyInput
        // is only wired to OnChange and an untouched dialog never runs it.  Here CanAccept is bound
        // and correct from the first frame, so this is belt and braces - and it stays, because the
        // one thing that must not happen is an invalid period reaching the query.
        return dialog.ShowDialog() == true && model.CanAccept ? model.Period : null;
    }

    private (DateTime Start, DateTime Stop) Remembered(string key)
    {
        (DateTime start, DateTime stop) = DefaultPeriod;

        return (
            _settings.GetDateTime(PeriodSettingsKey.SettingsSection, key + PeriodSettingsKey.StartKeySuffix, start),
            _settings.GetDateTime(PeriodSettingsKey.SettingsSection, key + PeriodSettingsKey.StopKeySuffix, stop));
    }

    private void Remember(string key, HalfOpenPeriod period)
    {
        _settings.SetDateTime(PeriodSettingsKey.SettingsSection, key + PeriodSettingsKey.StartKeySuffix, period.Start);
        _settings.SetDateTime(PeriodSettingsKey.SettingsSection, key + PeriodSettingsKey.StopKeySuffix, period.Stop);

        // The store buffers, unlike WritePrivateProfileString.  Flushing here rather than at
        // shutdown means a crash mid-collect does not lose the range the user just chose, and the
        // contract says Flush never throws.
        _settings.Flush();
    }
}
