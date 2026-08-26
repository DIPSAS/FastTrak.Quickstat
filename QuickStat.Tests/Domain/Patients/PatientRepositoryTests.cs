using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Domain.Patients;
using QuickStat.Domain.Populations;
using QuickStat.Tests.Domain.Populations;
using Xunit;

namespace QuickStat.Tests.Domain.Patients;

/// <summary>
/// What the patient repository actually sends to the server.
/// </summary>
public class PatientRepositoryTests
{
    private readonly RecordingSqlExecutor _executor = new();

    private static Population SamplePopulation(string queryText = "EXEC dbo.GetCaseListHbA1c :StudyId") => new()
    {
        ProcId = 261,
        Title = "HbA1c > 53 (7%)",
        QueryText = queryText,
    };

    private PatientRepository CreateRepository(SqlOptions? options = null) =>
        new(_executor, options ?? new SqlOptions(), NullLogger<PatientRepository>.Instance);

    [Fact]
    public async Task APopulationsOwnStatementIsRunVerbatim()
    {
        // CRF.Patient.List.pas:283 - the SqlText column is the statement, unmodified.
        const string Sql = "SELECT v.PersonId, v.FullName FROM dbo.ViewActiveCaseListStub v WHERE v.StudyId = :StudyId";

        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().LoadPopulationAsync(
                SamplePopulation(Sql),
                new Dictionary<string, object?> { ["StudyId"] = 3 }));

        Assert.Equal(Sql, request.CommandText);
        Assert.True(request.IsIdempotent);
    }

    [Fact]
    public async Task PopulationParametersAreBoundByNameAndCaseInsensitively()
    {
        // The Delphi bound positionally, which cannot express a repeated placeholder at all.
        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().LoadPopulationAsync(
                SamplePopulation("EXEC dbo.X :StudyId, :StartDate, :StopDate"),
                new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["StudyId"] = 3,
                    ["StartDate"] = new DateTime(2024, 1, 1),
                    ["StopDate"] = new DateTime(2024, 2, 1),
                }));

        Assert.NotNull(request.NamedValues);
        Assert.Empty(request.Values);
        Assert.Equal(3, request.NamedValues!.Count);
        Assert.True(request.NamedValues.TryGetValue("STUDYID", out object? studyId));
        Assert.Equal(3, studyId);
    }

    [Fact]
    public async Task ThePeriodReachesTheServerWithoutBeingShifted()
    {
        // PORT-PLAN.md R8 end to end: what the prompt returned is what gets bound.
        DateTime start = new(2024, 3, 1, 0, 0, 0, DateTimeKind.Unspecified);
        DateTime stop = new(2024, 4, 1, 0, 0, 0, DateTimeKind.Unspecified);

        StubPeriodPrompt prompt = new() { Answer = new HalfOpenPeriod(start, stop) };
        QueryParameterResolver resolver = new(
            new StubSqlTextRewriter(),
            new StubSessionService(),
            prompt,
            NullLogger<QueryParameterResolver>.Instance);

        Population population = SamplePopulation("EXEC dbo.GetCaseListPeriod :StartDate, :StopDate");
        ParameterResolution resolution = await resolver.ResolveAsync(population.QueryText);

        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().LoadPopulationAsync(population, resolution.Values));

        Assert.Equal(start, request.NamedValues!["StartDate"]);
        Assert.Equal(stop, request.NamedValues["StopDate"]);
    }

    [Fact]
    public async Task TheDefaultCaseListIsTheDelphiStatement()
    {
        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().GetCaseListAsync(3));

        Assert.Equal("EXEC dbo.GetCaseList :StudyId", request.CommandText);
        Assert.Equal(3, request.NamedValues!["StudyId"]);
    }

    [Fact]
    public async Task NoPatientsMeansNoNationalIdRoundTrip()
    {
        IReadOnlyDictionary<int, string> map = await CreateRepository().GetNationalIdsAsync([]);

        Assert.Empty(map);
        Assert.Empty(_executor.Requests);
    }

    [Fact]
    public async Task NationalIdRecoveryUsesTheTableValuedParameter()
    {
        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().GetNationalIdsAsync([4711, 88]));

        SqlTableParameter table = Assert.Single(request.TableParameters);
        Assert.Equal([4711, 88], table.Values);
        Assert.DoesNotContain("IN (", request.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ACohortAboveTheParameterLimitStillTakesOneRoundTrip()
    {
        int[] ids = [.. Enumerable.Range(1, 3000)];

        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().GetNationalIdsAsync(ids));

        Assert.Single(_executor.Requests);
        Assert.Equal(3000, Assert.Single(request.TableParameters).Values.Count);
    }

    [Fact]
    public async Task WithoutTheTableTypeTheFirstBatchIsStillParameterised()
    {
        SqlOptions fallback = new() { PersonIdListTypeName = null, MaxIdsPerBatch = 2 };

        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository(fallback).GetNationalIdsAsync([1, 2, 3, 4, 5]));

        Assert.Empty(request.TableParameters);
        Assert.Equal(2, request.NamedValues!.Count);
        Assert.Contains(":p0, :p1", request.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnEmptySearchNeverReachesTheServer()
    {
        IReadOnlyList<Patient> patients = await CreateRepository().SearchAsync(3, "   ");

        Assert.Empty(patients);
        Assert.Empty(_executor.Requests);
    }

    [Fact]
    public async Task ASearchDispatchesToTheMatchingStatement()
    {
        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().SearchAsync(3, "01019012345"));

        Assert.Equal(PatientSql.PersonByNationalId, request.CommandText);
    }

    [Fact]
    public async Task NullArgumentsAreRejected()
    {
        PatientRepository repository = CreateRepository();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.LoadPopulationAsync(null!, new Dictionary<string, object?>()));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => repository.LoadPopulationAsync(SamplePopulation(), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.GetNationalIdsAsync(null!));
    }
}
