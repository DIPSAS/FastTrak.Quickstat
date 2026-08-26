using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Domain.Populations;
using Xunit;

namespace QuickStat.Tests.Domain.Populations;

/// <summary>
/// Covers parameter resolution: when the period is asked for, what a cancel means as opposed to an
/// unknown placeholder, and that the half-open period is bound without being shifted.
/// </summary>
public class QueryParameterResolverTests
{
    private const string PeriodSql = "EXEC dbo.GetCaseListPeriod :StartDate, :StopDate";
    private const string NorwegianCaption = "Denne spørringen krever at du angir et tidsintervall.";

    private readonly StubPeriodPrompt _prompt = new();
    private readonly StubSessionService _sessions = new();

    private QueryParameterResolver CreateResolver() => new(
        new StubSqlTextRewriter(),
        _sessions,
        _prompt,
        NullLogger<QueryParameterResolver>.Instance);

    [Fact]
    public async Task AStatementWithoutPlaceholdersResolvesToNothing()
    {
        ParameterResolution resolution = await CreateResolver().ResolveAsync("EXEC dbo.GetCaseListAll");

        Assert.True(resolution.Succeeded);
        Assert.Empty(resolution.Values);
        Assert.Equal(0, _prompt.CallCount);
        Assert.Null(resolution.FailureReason);
        Assert.False(resolution.CancelledByUser);
    }

    [Fact]
    public async Task BothHalvesOfThePairTriggerThePrompt()
    {
        _prompt.Answer = new HalfOpenPeriod(new DateTime(2024, 1, 1), new DateTime(2024, 2, 1));

        ParameterResolution resolution = await CreateResolver().ResolveAsync(PeriodSql);

        Assert.True(resolution.Succeeded);
        Assert.Equal(1, _prompt.CallCount);
        Assert.Equal(NorwegianCaption, _prompt.LastCaption);
    }

    [Fact]
    public async Task OnePlaceholderOfThePairDoesNotTriggerThePrompt()
    {
        // Emetra.Database.ParameterDictionary.pas:98 requires both. A lone :StartDate is resolved from
        // the session like any other name - and therefore fails, exactly as it does today.
        ParameterResolution resolution =
            await CreateResolver().ResolveAsync("EXEC dbo.GetCaseListSince :StartDate");

        Assert.Equal(0, _prompt.CallCount);
        Assert.False(resolution.Succeeded);
        Assert.False(resolution.CancelledByUser);
        Assert.NotNull(resolution.FailureReason);
        Assert.Contains("StartDate", resolution.FailureReason!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ThePeriodIsBoundHalfOpenAndUnshifted()
    {
        // PORT-PLAN.md R8. Adding or subtracting a day at either end moves every cohort by a day.
        DateTime start = new(2024, 3, 1, 0, 0, 0, DateTimeKind.Unspecified);
        DateTime stop = new(2024, 4, 1, 0, 0, 0, DateTimeKind.Unspecified);
        _prompt.Answer = new HalfOpenPeriod(start, stop);

        ParameterResolution resolution = await CreateResolver().ResolveAsync(PeriodSql);

        Assert.True(resolution.Succeeded);
        Assert.Equal(2, resolution.Values.Count);
        Assert.Equal(start, Assert.IsType<DateTime>(resolution.Values["StartDate"]));
        Assert.Equal(stop, Assert.IsType<DateTime>(resolution.Values["StopDate"]));
    }

    [Fact]
    public async Task ThePeriodPlaceholdersAreMatchedCaseInsensitively()
    {
        // Delphi's TParams.FindParam is case-insensitive, so a population writing :startdate still
        // gets the dialog.
        _prompt.Answer = new HalfOpenPeriod(new DateTime(2024, 1, 1), new DateTime(2024, 1, 2));

        ParameterResolution resolution =
            await CreateResolver().ResolveAsync("EXEC dbo.X :startdate, :STOPDATE");

        Assert.True(resolution.Succeeded);
        Assert.Equal(1, _prompt.CallCount);
        Assert.Equal(2, resolution.Values.Count);
    }

    [Fact]
    public async Task CancellingThePromptIsACancelAndNotAnError()
    {
        // PORT-PLAN.md §7.2. This is what lets the caller abort the load instead of leaving the
        // previous cohort on screen under the new population's title.
        _prompt.Answer = null;

        ParameterResolution resolution = await CreateResolver().ResolveAsync(PeriodSql);

        Assert.False(resolution.Succeeded);
        Assert.True(resolution.CancelledByUser);
        Assert.Null(resolution.FailureReason);
        Assert.Empty(resolution.Values);
    }

    [Fact]
    public async Task AnUnknownPlaceholderIsAnErrorAndNotACancel()
    {
        // The distinction the Delphi's bare boolean could not express.
        _sessions.Current = null;

        ParameterResolution resolution =
            await CreateResolver().ResolveAsync("EXEC dbo.GetCaseListWeird :NoSuchThing");

        Assert.False(resolution.Succeeded);
        Assert.False(resolution.CancelledByUser);
        Assert.NotNull(resolution.FailureReason);
        Assert.Contains("NoSuchThing", resolution.FailureReason!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnInvalidPeriodFromThePromptIsAnErrorAndNotACancel()
    {
        // The dialog itself refuses Start >= Stop (Emetra.VclForm.Period.pas:52) and a cancel is null,
        // so this can only be a broken prompt - which must not be reported as a user decision.
        _prompt.Answer = new HalfOpenPeriod(new DateTime(2024, 5, 1), new DateTime(2024, 5, 1));

        ParameterResolution resolution = await CreateResolver().ResolveAsync(PeriodSql);

        Assert.False(resolution.Succeeded);
        Assert.False(resolution.CancelledByUser);
        Assert.NotNull(resolution.FailureReason);
    }

    [Fact]
    public async Task ThePromptContextIsTheHashOfTheStatementAndNotTheStatement()
    {
        // PORT-PLAN.md §7.2: the Delphi passed the whole SQL where a settings key belonged, so the
        // remembered period never round-tripped.
        _prompt.Answer = new HalfOpenPeriod(new DateTime(2024, 1, 1), new DateTime(2024, 1, 2));

        await CreateResolver().ResolveAsync(PeriodSql);

        Assert.Equal(PeriodSettingsKey.For(PeriodSql), _prompt.LastContext);
        Assert.DoesNotContain("EXEC", _prompt.LastContext!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ACancelAbortsBeforeAnyOtherPlaceholderIsResolved()
    {
        // Order matters: the period prompt runs first and a cancel returns immediately, so the caller
        // never sees a partially populated set of values.
        _prompt.Answer = null;
        _sessions.Current = null;

        ParameterResolution resolution = await CreateResolver().ResolveAsync(
            "EXEC dbo.GetCaseListPeriod :StudyId, :StartDate, :StopDate, :Unknowable");

        Assert.True(resolution.CancelledByUser);
        Assert.Null(resolution.FailureReason);
    }

    [Fact]
    public async Task ANullStatementIsRejected()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateResolver().ResolveAsync(null!));
    }
}
