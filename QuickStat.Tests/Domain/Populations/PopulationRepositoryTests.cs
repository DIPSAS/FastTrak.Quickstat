using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Data;
using QuickStat.Domain.Populations;
using Xunit;

namespace QuickStat.Tests.Domain.Populations;

/// <summary>
/// Covers the three catalogue procedure variants and the fire-and-forget audit.
/// </summary>
public class PopulationRepositoryTests
{
    private readonly RecordingSqlExecutor _executor = new();

    private PopulationRepository CreateRepository() =>
        new(_executor, NullLogger<PopulationRepository>.Instance);

    [Fact]
    public async Task NoStudyMeansNoRoundTripAndAnEmptyList()
    {
        // EPR.Population.List.pas:102 - "if AStudyId > 0".
        IReadOnlyList<Population> populations =
            await CreateRepository().GetPopulationsAsync(0, 20000, frequentlyUsedOnly: false);

        Assert.Empty(populations);
        Assert.Empty(_executor.Requests);
    }

    [Fact]
    public async Task NegativeStudyMeansNoRoundTripEither()
    {
        IReadOnlyList<Population> populations =
            await CreateRepository().GetPopulationsAsync(-1, 20000, frequentlyUsedOnly: true);

        Assert.Empty(populations);
        Assert.Empty(_executor.Requests);
    }

    [Fact]
    public async Task FrequentlyUsedOnlyUsesThePopularProcedureWithTheVersion()
    {
        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().GetPopulationsAsync(42, 19000, frequentlyUsedOnly: true));

        Assert.Equal("EXEC Populations.GetPopularPopulations :StudyId, :DbVer", request.CommandText);
        AssertNamed(request, ("StudyId", 42), ("DbVer", 19000));
        Assert.True(request.IsIdempotent);
    }

    [Fact]
    public async Task FrequentlyUsedOnlyWinsOverTheVersionTest()
    {
        // The Delphi tests AShowMostCommon first, so the popular procedure is used - with :DbVer -
        // even on a database below 18200.
        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().GetPopulationsAsync(42, 510, frequentlyUsedOnly: true));

        Assert.Equal("EXEC Populations.GetPopularPopulations :StudyId, :DbVer", request.CommandText);
        AssertNamed(request, ("StudyId", 42), ("DbVer", 510));
    }

    [Theory]
    [InlineData(18200)]
    [InlineData(18201)]
    [InlineData(99999)]
    public async Task ModernDatabasesPassTheVersionArgument(int dbVersion)
    {
        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().GetPopulationsAsync(7, dbVersion, frequentlyUsedOnly: false));

        Assert.Equal("EXEC Populations.GetStudyPopulations :StudyId, :DbVer", request.CommandText);
        AssertNamed(request, ("StudyId", 7), ("DbVer", dbVersion));
    }

    [Theory]
    [InlineData(18199)]
    [InlineData(510)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task OlderDatabasesUseTheSingleArgumentProcedure(int dbVersion)
    {
        // -1 is the swallowed-failure value from Emetra.Database.Info.pas:154-158, and it routes here
        // exactly as it does in the Delphi - the repository logs a warning rather than changing route.
        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().GetPopulationsAsync(7, dbVersion, frequentlyUsedOnly: false));

        Assert.Equal("EXEC Populations.GetStudyPopulations :StudyId", request.CommandText);
        AssertNamed(request, ("StudyId", 7));
    }

    [Fact]
    public async Task ThePopulationVersionThresholdIsTheOneTheContractDeclares()
    {
        SqlRequest below = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().GetPopulationsAsync(
                1,
                DatabaseInfo.PopulationsWithVersionDbVersion - 1,
                frequentlyUsedOnly: false));

        SqlRequest atThreshold = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().GetPopulationsAsync(
                1,
                DatabaseInfo.PopulationsWithVersionDbVersion,
                frequentlyUsedOnly: false));

        Assert.DoesNotContain(":DbVer", below.CommandText, StringComparison.Ordinal);
        Assert.Contains(":DbVer", atThreshold.CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheAuditCommandCarriesTheTitleInTheProcDescSlot()
    {
        await CreateRepository().LogPopulationSelectedAsync(3, 261, "HbA1c > 53 (7%)", 1234);

        SqlRequest request = _executor.Last;
        Assert.Equal("EXEC dbo.AddPopulationLog :StudyId, :ProcId, :ProcDesc, :ElapsedMs", request.CommandText);
        AssertNamed(request, ("StudyId", 3), ("ProcId", 261), ("ProcDesc", "HbA1c > 53 (7%)"), ("ElapsedMs", 1234L));

        // A write must never be retried automatically: a duplicated audit row would skew the
        // "frequently used" ranking the server derives from it.
        Assert.False(request.IsIdempotent);
    }

    [Fact]
    public async Task AFailingAuditIsSwallowed()
    {
        // EPR.VclFrame.Populations.pas:224-226 logs a SilentWarning and carries on. Losing an audit
        // row must never abort the load or reach the user.
        _executor.ExecuteFailure = new InvalidOperationException("boom");

        await CreateRepository().LogPopulationSelectedAsync(3, 261, "Whatever", 1);

        Assert.Single(_executor.Requests);
    }

    private static void AssertNamed(SqlRequest request, params (string Name, object? Value)[] expected)
    {
        Assert.NotNull(request.NamedValues);
        Assert.Empty(request.Values);
        Assert.Equal(expected.Length, request.NamedValues!.Count);

        foreach ((string name, object? value) in expected)
        {
            Assert.True(request.NamedValues.TryGetValue(name, out object? actual), $"Missing parameter {name}.");
            Assert.Equal(value, actual);
        }
    }
}
