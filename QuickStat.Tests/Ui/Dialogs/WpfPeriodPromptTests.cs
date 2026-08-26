using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Configuration.Settings;
using QuickStat.Domain.Populations;
using QuickStat.Services;
using QuickStat.Tests.Ui.Services;
using QuickStat.ViewModels;
using Xunit;

namespace QuickStat.Tests.Ui.Dialogs;

/// <summary>
/// The <see cref="IPeriodPrompt"/> contract as step 3.6 implements it: what accept, cancel and a
/// remembered range each produce, and what reaches the settings file.
/// </summary>
/// <remarks>
/// <para>
/// This is the seam a period-gated population runs through, so the three outcomes are pinned rather
/// than described. The modal itself is substituted; <c>PeriodDialogTests</c> drives the real window.
/// </para>
/// <para>
/// <b>There is no "the user was never asked" outcome.</b> The dialog is shown every time a query
/// declares both <c>:StartDate</c> and <c>:StopDate</c>; a remembered range only pre-fills it. That
/// is the Delphi's behaviour too - <c>Emetra.Database.ParameterDictionary.pas:98-106</c> calls
/// <c>TryGetPeriod</c> unconditionally and <c>EPR.PeriodDictionary.pas:71</c> always reaches
/// <c>ShowModal</c>.
/// </para>
/// </remarks>
public class WpfPeriodPromptTests
{
    private const string Context = "EXEC dbo.GetCaseList :StartDate, :StopDate";
    private const string Caption = "Denne spørringen krever at du angir et tidsintervall.";

    private static readonly DateTime March4 = new(2019, 3, 4, 0, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime March18 = new(2019, 3, 18, 0, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public async Task AcceptingReturnsTheChosenHalfOpenPeriod()
    {
        InMemorySettingsStore settings = new();
        WpfPeriodPrompt prompt = Prompt(settings, model =>
        {
            model.Start = March4;
            model.Stop = March18;

            return true;
        });

        HalfOpenPeriod? period = await prompt.TryGetPeriodAsync(Context, Caption);

        Assert.Equal(new HalfOpenPeriod(March4, March18), period);
    }

    [Fact]
    public async Task CancellingReturnsNull()
    {
        InMemorySettingsStore settings = new();
        WpfPeriodPrompt prompt = Prompt(settings, _ => false);

        Assert.Null(await prompt.TryGetPeriodAsync(Context, Caption));
    }

    [Fact]
    public async Task CancellingRemembersNothing()
    {
        // EPR.PeriodDictionary.pas:71-79 writes inside the success branch only, so a cancelled
        // prompt must leave whatever was remembered before exactly as it was.
        InMemorySettingsStore settings = new();

        _ = await Prompt(settings, model =>
        {
            model.Start = March4;
            model.Stop = March18;

            return true;
        }).TryGetPeriodAsync(Context, Caption);

        _ = await Prompt(settings, model =>
        {
            model.Start = March4.AddYears(-5);
            model.Stop = March18.AddYears(-5);

            return false;
        }).TryGetPeriodAsync(Context, Caption);

        DateTime seen = default;

        _ = await Prompt(settings, model =>
        {
            seen = model.Start;

            return false;
        }).TryGetPeriodAsync(Context, Caption);

        Assert.Equal(March4, seen);
    }

    [Fact]
    public async Task AnAcceptedRangeIsOfferedBackTheNextTime()
    {
        // The Delphi feature that never worked: the key was the whole statement, so nothing ever
        // round-tripped and every prompt opened on yesterday-and-today (PORT-PLAN.md §7.2).
        InMemorySettingsStore settings = new();

        _ = await Prompt(settings, model =>
        {
            model.Start = March4;
            model.Stop = March18;

            return true;
        }).TryGetPeriodAsync(Context, Caption);

        (DateTime start, DateTime stop) = (default, default);

        _ = await Prompt(settings, model =>
        {
            (start, stop) = (model.Start, model.Stop);

            return false;
        }).TryGetPeriodAsync(Context, Caption);

        Assert.Equal(March4, start);
        Assert.Equal(March18, stop);
    }

    [Fact]
    public async Task NothingRememberedOpensOnYesterdayAndToday()
    {
        InMemorySettingsStore settings = new();
        (DateTime start, DateTime stop) = (default, default);

        _ = await Prompt(settings, model =>
        {
            (start, stop) = (model.Start, model.Stop);

            return false;
        }).TryGetPeriodAsync(Context, Caption);

        Assert.Equal(DateTime.Today.AddDays(-1), start);
        Assert.Equal(DateTime.Today, stop);
        Assert.Equal(WpfPeriodPrompt.DefaultPeriod, (start, stop));
    }

    [Fact]
    public async Task TwoQueriesRememberSeparately()
    {
        InMemorySettingsStore settings = new();

        _ = await Prompt(settings, model =>
        {
            model.Start = March4;
            model.Stop = March18;

            return true;
        }).TryGetPeriodAsync(Context, Caption);

        DateTime other = default;

        _ = await Prompt(settings, model =>
        {
            other = model.Start;

            return false;
        }).TryGetPeriodAsync("EXEC dbo.SomethingElse :StartDate, :StopDate", Caption);

        Assert.Equal(DateTime.Today.AddDays(-1), other);
    }

    [Fact]
    public async Task NeitherTheStatementNorAnythingReadableReachesTheSettingsFile()
    {
        // The privacy half of PORT-PLAN.md §7.2: population SQL can name tables, columns and study
        // identifiers, and the settings file is not access-controlled.
        RecordingSettingsStore settings = new();

        _ = await Prompt(settings, model =>
        {
            model.Start = March4;
            model.Stop = March18;

            return true;
        }).TryGetPeriodAsync(Context, Caption);

        List<string> keys = [.. settings.Keys];

        Assert.Equal(2, keys.Count);
        Assert.All(keys, key => Assert.DoesNotContain("StartDate", key, StringComparison.OrdinalIgnoreCase));
        Assert.All(keys, key => Assert.DoesNotContain("dbo", key, StringComparison.OrdinalIgnoreCase));
        Assert.All(settings.Sections, section => Assert.Equal(PeriodSettingsKey.SettingsSection, section));

        // The suffixes are Core's, and the stem is the same for both halves of the pair.
        Assert.Contains(keys, key => key.EndsWith(PeriodSettingsKey.StartKeySuffix, StringComparison.Ordinal));
        Assert.Contains(keys, key => key.EndsWith(PeriodSettingsKey.StopKeySuffix, StringComparison.Ordinal));
        Assert.Single(keys.Select(key => key[..key.LastIndexOf('.')]).Distinct(StringComparer.Ordinal));
    }

    [Fact]
    public async Task AnAlreadyCancelledTokenNeverShowsTheDialog()
    {
        InMemorySettingsStore settings = new();
        int shown = 0;

        WpfPeriodPrompt prompt = Prompt(settings, _ =>
        {
            shown++;

            return true;
        });

        using CancellationTokenSource cancellation = new();

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => prompt.TryGetPeriodAsync(Context, Caption, cancellation.Token));

        Assert.Equal(0, shown);
    }

    [Fact]
    public async Task AnEmptyCaptionFallsBackToTheRealSubHeader()
    {
        InMemorySettingsStore settings = new();
        string seen = "";

        _ = await Prompt(settings, model =>
        {
            seen = model.SubHeaderText;

            return false;
        }).TryGetPeriodAsync(Context, "   ");

        Assert.Equal(PeriodViewModel.SubHeader, seen);
    }

    [Fact]
    public async Task TheCaptionReachesTheSubHeader()
    {
        InMemorySettingsStore settings = new();
        string seen = "";

        _ = await Prompt(settings, model =>
        {
            seen = model.SubHeaderText;

            return false;
        }).TryGetPeriodAsync(Context, Caption);

        Assert.Equal(Caption, seen);
    }

    [Fact]
    public async Task AnInvalidRangeIsTreatedAsACancelEvenIfTheDialogSaysOtherwise()
    {
        // Emetra.VclForm.Period.pas:52 re-checks after ShowModal.  The port keeps the check, because
        // QueryParameterResolver reports an invalid period as a broken prompt rather than as a user
        // decision - and would then fail the load with an error instead of a quiet abort.
        InMemorySettingsStore settings = new();

        WpfPeriodPrompt prompt = Prompt(settings, model =>
        {
            model.Start = March18;
            model.Stop = March4;

            return true;
        });

        Assert.Null(await prompt.TryGetPeriodAsync(Context, Caption));
        Assert.False(settings.Contains(PeriodSettingsKey.SettingsSection, PeriodSettingsKey.For(PeriodSettingsKey.For(Context)) + PeriodSettingsKey.StartKeySuffix));
    }

    [Fact]
    public async Task ShowingTheDialogGoesThroughTheDispatcher()
    {
        CountingDispatcher dispatcher = new();
        WpfPeriodPrompt prompt = new(dispatcher, new InMemorySettingsStore(), NullLogger<WpfPeriodPrompt>.Instance, _ => false);

        _ = await prompt.TryGetPeriodAsync(Context, Caption);

        Assert.Equal(1, dispatcher.InvokeAsyncCount);
    }

    private static WpfPeriodPrompt Prompt(ISettingsStore settings, Func<PeriodViewModel, bool> show) =>
        new(new InlineUiDispatcher(), settings, NullLogger<WpfPeriodPrompt>.Instance, show);

    /// <summary>Records the section and key of every write, so the shape of the key is assertable.</summary>
    private sealed class RecordingSettingsStore : ISettingsStore
    {
        private readonly InMemorySettingsStore _inner = new();

        internal List<string> Sections { get; } = [];

        internal List<string> Keys { get; } = [];

        public bool Contains(string section, string key) => _inner.Contains(section, key);

        public string GetString(string section, string key, string defaultValue = "") =>
            _inner.GetString(section, key, defaultValue);

        public int GetInt32(string section, string key, int defaultValue = 0) =>
            _inner.GetInt32(section, key, defaultValue);

        public bool GetBoolean(string section, string key, bool defaultValue = false) =>
            _inner.GetBoolean(section, key, defaultValue);

        public double GetDouble(string section, string key, double defaultValue = 0) =>
            _inner.GetDouble(section, key, defaultValue);

        public DateTime GetDateTime(string section, string key, DateTime defaultValue) =>
            _inner.GetDateTime(section, key, defaultValue);

        public void SetString(string section, string key, string value) => Record(section, key, () => _inner.SetString(section, key, value));

        public void SetInt32(string section, string key, int value) => Record(section, key, () => _inner.SetInt32(section, key, value));

        public void SetBoolean(string section, string key, bool value) => Record(section, key, () => _inner.SetBoolean(section, key, value));

        public void SetDouble(string section, string key, double value) => Record(section, key, () => _inner.SetDouble(section, key, value));

        public void SetDateTime(string section, string key, DateTime value) => Record(section, key, () => _inner.SetDateTime(section, key, value));

        public void Remove(string section, string key) => _inner.Remove(section, key);

        public void Flush() => _inner.Flush();

        private void Record(string section, string key, Action write)
        {
            Sections.Add(section);
            Keys.Add(key);

            write();
        }
    }

    /// <summary>Runs inline like <c>InlineUiDispatcher</c>, and counts.</summary>
    private sealed class CountingDispatcher : IUiDispatcher
    {
        internal int InvokeAsyncCount { get; private set; }

        public bool IsOnUiThread => true;

        public void Invoke(Action action) => action();

        public void Post(Action action) => action();

        public Task InvokeAsync(Action action)
        {
            InvokeAsyncCount++;

            action();

            return Task.CompletedTask;
        }
    }
}
