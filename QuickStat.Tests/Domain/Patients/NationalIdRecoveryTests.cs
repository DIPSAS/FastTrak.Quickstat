using Microsoft.Extensions.Logging;
using QuickStat.Diagnostics;
using QuickStat.Domain.Patients;
using QuickStat.Domain.Populations;
using Xunit;

namespace QuickStat.Tests.Domain.Patients;

/// <summary>
/// The step this repository lost: <c>if not fPersonList.IncludesNationalId then
/// fPersonList.AddNationalIds;</c> (<c>MainQuickStat.pas:536-540</c>, commented out here - see
/// PORT-PLAN.md §2.1 and §5 Phase 4).
/// </summary>
/// <remarks>
/// The two view-models that load a population share
/// <see cref="NationalIdRecovery.EnsureNationalIdsAsync"/>, so its guards, its "absent means
/// unknown" rule and its failure behaviour are pinned once, here, rather than twice through a
/// window.
/// </remarks>
public class NationalIdRecoveryTests
{
    private const string OlasNationalId = "12032212345";
    private const string KarisNationalId = "01029912345";

    private static Patient NewPatient(int personId, string? nationalId = null) => new()
    {
        PersonId = personId,
        LastName = "Hansen",
        FirstName = "Ola",
        NationalId = nationalId,
    };

    [Fact]
    public async Task ACohortWithoutNationalIdsIsQueriedOnceForAllOfThem()
    {
        FakeRepository repository = new()
        {
            NationalIds = { [8] = OlasNationalId, [13] = KarisNationalId },
        };

        List<Patient> cohort = [NewPatient(8), NewPatient(13)];

        int recovered = await NationalIdRecovery.EnsureNationalIdsAsync(
            repository, cohort, new RecordingLogger());

        Assert.Equal(2, recovered);
        Assert.Equal([8, 13], Assert.Single(repository.Requests));
        Assert.Equal(OlasNationalId, cohort[0].NationalId);
        Assert.Equal(KarisNationalId, cohort[1].NationalId);
    }

    [Fact]
    public async Task ACohortThatAlreadyCarriesThemIsNotQueriedAtAll()
    {
        // TPatientList.IncludesNationalId: some population procedures do return the column, and the
        // Delphi skips AddNationalIds entirely when they do.
        FakeRepository repository = new();

        List<Patient> cohort = [NewPatient(8, OlasNationalId), NewPatient(13, KarisNationalId)];

        int recovered = await NationalIdRecovery.EnsureNationalIdsAsync(
            repository, cohort, new RecordingLogger());

        Assert.Equal(0, recovered);
        Assert.Empty(repository.Requests);
        Assert.Equal(OlasNationalId, cohort[0].NationalId);
    }

    [Fact]
    public async Task APartlyFilledCohortIsRequeriedInFull()
    {
        // All-or-nothing, as upstream's guard is: a population either selects the column or it does
        // not, so one filled row does not mean the rest are filled.
        FakeRepository repository = new()
        {
            NationalIds = { [8] = OlasNationalId, [13] = KarisNationalId },
        };

        List<Patient> cohort = [NewPatient(8, OlasNationalId), NewPatient(13)];

        int recovered = await NationalIdRecovery.EnsureNationalIdsAsync(
            repository, cohort, new RecordingLogger());

        // One of the two gained an id; the other already had it and is not counted twice.
        Assert.Equal(1, recovered);
        Assert.Equal([8, 13], Assert.Single(repository.Requests));
        Assert.Equal(KarisNationalId, cohort[1].NationalId);
    }

    [Fact]
    public async Task AnEmptyCohortMakesNoRoundTrip()
    {
        FakeRepository repository = new();

        int recovered = await NationalIdRecovery.EnsureNationalIdsAsync(
            repository, [], new RecordingLogger());

        Assert.Equal(0, recovered);
        Assert.Empty(repository.Requests);
    }

    [Fact]
    public async Task APatientTheQueryDidNotReturnKeepsANullNationalId()
    {
        // The statement filters NationalId IS NOT NULL, so a person with none on file is simply
        // absent from the result.  Absent must mean null, not an exception and not an empty string.
        FakeRepository repository = new()
        {
            NationalIds = { [8] = OlasNationalId },
        };

        List<Patient> cohort = [NewPatient(8), NewPatient(13)];

        int recovered = await NationalIdRecovery.EnsureNationalIdsAsync(
            repository, cohort, new RecordingLogger());

        Assert.Equal(1, recovered);
        Assert.Equal(OlasNationalId, cohort[0].NationalId);
        Assert.Null(cohort[1].NationalId);
    }

    [Fact]
    public async Task AnIdTheQueryDoesNotReturnIsNotBlankedOut()
    {
        FakeRepository repository = new()
        {
            NationalIds = { [13] = KarisNationalId },
        };

        List<Patient> cohort = [NewPatient(8, OlasNationalId), NewPatient(13)];

        await NationalIdRecovery.EnsureNationalIdsAsync(repository, cohort, new RecordingLogger());

        Assert.Equal(OlasNationalId, cohort[0].NationalId);
    }

    [Fact]
    public async Task AFailedRecoveryIsLoggedAndDegradedRatherThanThrown()
    {
        // The user asked for a cohort; the national id is one column of it.  The fetch is also
        // unconditional, so in an anonymous mode a fatal failure here would destroy a load whose
        // result never needed the ids at all.
        FakeRepository repository = new() { Throws = new InvalidOperationException("Invalid object name 'dbo.Person'.") };
        RecordingLogger logger = new();

        List<Patient> cohort = [NewPatient(8), NewPatient(13)];

        int recovered = await NationalIdRecovery.EnsureNationalIdsAsync(repository, cohort, logger);

        Assert.Equal(0, recovered);
        Assert.Null(cohort[0].NationalId);
        Assert.Null(cohort[1].NationalId);

        (LogLevel level, string message, Exception? exception) = Assert.Single(logger.Entries);

        Assert.Equal(LogLevel.Warning, level);
        Assert.Contains("2 patients", message, StringComparison.Ordinal);
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public async Task CancellationIsNotSwallowed()
    {
        // A cancelled token means the whole load is being abandoned, and the caller owns that -
        // unlike a query failure, which only costs this one column.
        FakeRepository repository = new();

        using CancellationTokenSource cancellation = new();

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => NationalIdRecovery.EnsureNationalIdsAsync(
                repository, [NewPatient(8)], new RecordingLogger(), cancellation.Token));
    }

    [Fact]
    public async Task TheLogSaysHowManyWereRecoveredAndNeverWhatTheyAre()
    {
        // PiiRedactor exists because national ids leak into files; a log line that carried one would
        // be redacted by the sink, but the honest fix is not to write it.
        FakeRepository repository = new()
        {
            NationalIds = { [8] = OlasNationalId, [13] = KarisNationalId },
        };

        RecordingLogger logger = new();

        await NationalIdRecovery.EnsureNationalIdsAsync(
            repository, [NewPatient(8), NewPatient(13)], logger);

        (LogLevel level, string message, Exception? exception) = Assert.Single(logger.Entries);

        Assert.Equal(LogLevel.Information, level);
        Assert.Null(exception);
        Assert.Contains("2", message, StringComparison.Ordinal);
        Assert.DoesNotContain(OlasNationalId, message, StringComparison.Ordinal);
        Assert.DoesNotContain(KarisNationalId, message, StringComparison.Ordinal);
        Assert.False(PiiRedactor.ContainsPersonalIdentifier(message));
    }

    [Fact]
    public void IncludesNationalIdIsFalseForAnEmptyCohort()
    {
        // TPatientList.IncludesNationalId returns false for an empty list, and EnsureNationalIdsAsync
        // has its own Count == 0 guard so that the two together still make no round trip.
        Assert.False(NationalIdRecovery.IncludesNationalId([]));
        Assert.True(NationalIdRecovery.IncludesNationalId([NewPatient(8, OlasNationalId)]));
        Assert.False(NationalIdRecovery.IncludesNationalId([NewPatient(8, "")]));
        Assert.False(NationalIdRecovery.IncludesNationalId([NewPatient(8)]));
    }

    [Fact]
    public async Task EveryArgumentIsChecked()
    {
        FakeRepository repository = new();

        Assert.Throws<ArgumentNullException>(() => NationalIdRecovery.IncludesNationalId(null!));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => NationalIdRecovery.EnsureNationalIdsAsync(null!, [], new RecordingLogger()));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => NationalIdRecovery.EnsureNationalIdsAsync(repository, null!, new RecordingLogger()));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => NationalIdRecovery.EnsureNationalIdsAsync(repository, [], null!));
    }

    /// <summary>A repository whose recovery query answers from a dictionary and records its calls.</summary>
    private sealed class FakeRepository : IPatientRepository
    {
        public Dictionary<int, string> NationalIds { get; } = [];

        public List<int[]> Requests { get; } = [];

        public Exception? Throws { get; init; }

        public Task<IReadOnlyList<Patient>> LoadPopulationAsync(
            Population population,
            IReadOnlyDictionary<string, object?> parameters,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<Patient>> GetCaseListAsync(
            int studyId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<int, string>> GetNationalIdsAsync(
            IReadOnlyCollection<int> personIds,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(personIds);

            Requests.Add([.. personIds]);

            cancellationToken.ThrowIfCancellationRequested();

            if (Throws is not null)
            {
                throw Throws;
            }

            Dictionary<int, string> answer = [];

            foreach (int personId in personIds)
            {
                if (NationalIds.TryGetValue(personId, out string? nationalId))
                {
                    answer[personId] = nationalId;
                }
            }

            return Task.FromResult<IReadOnlyDictionary<int, string>>(answer);
        }

        public Task<IReadOnlyList<Patient>> SearchAsync(
            int studyId,
            string searchText,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    /// <summary>Captures level, rendered message and exception, so the log itself can be asserted.</summary>
    private sealed class RecordingLogger : ILogger
    {
        private readonly List<(LogLevel Level, string Message, Exception? Exception)> _entries = [];

        public IReadOnlyList<(LogLevel Level, string Message, Exception? Exception)> Entries => _entries;

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            _entries.Add((logLevel, formatter(state, exception), exception));
        }
    }
}
