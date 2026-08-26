using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Data;
using QuickStat.Domain.Packages;
using QuickStat.Tests.Domain.Populations;
using Xunit;

namespace QuickStat.Tests.Domain.Packages;

/// <summary>
/// Packaged selections live server-side in <c>Report.QuickStat</c>, so this is a repository and not a
/// setting. These tests pin the three statements it uses.
/// </summary>
public class PackageRepositoryTests
{
    private readonly RecordingSqlExecutor _executor = new();

    private PackageRepository CreateRepository() => new(_executor, NullLogger<PackageRepository>.Instance);

    private static PackagedSelection Sample() => new()
    {
        StudyId = 3,
        PopulationId = 261,
        Title = "Diabetes 2026",
        Comment = "Til årsrapporten",
        CollectorNames = ["QS_LAB_HBA1C", "QS_DEMO_AGE"],
    };

    [Fact]
    public async Task ListingUsesTheJoinedSelect()
    {
        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().GetPackagesAsync(3));

        Assert.Equal(
            "SELECT r.* FROM Report.QuickStat r JOIN dbo.Study s ON s.StudyId=r.StudyId WHERE r.StudyId=:StudyId",
            request.CommandText);
        Assert.Equal(3, request.NamedValues!["StudyId"]);
        Assert.True(request.IsIdempotent);
    }

    [Fact]
    public async Task SavingUsesReportAddQuickStat()
    {
        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().SaveAsync(Sample()));

        Assert.Equal(
            "EXEC Report.AddQuickStat :StudyId,:ProcId,:Title,:DataElements,:Comment",
            request.CommandText);
    }

    [Fact]
    public async Task SavingSendsTheSortedCollectorNames()
    {
        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().SaveAsync(Sample()));

        Assert.Equal(3, request.NamedValues!["StudyId"]);
        Assert.Equal(261, request.NamedValues["ProcId"]);
        Assert.Equal("Diabetes 2026", request.NamedValues["Title"]);
        Assert.Equal("Til årsrapporten", request.NamedValues["Comment"]);
        Assert.Equal("QS_DEMO_AGE;QS_LAB_HBA1C", request.NamedValues["DataElements"]);
    }

    [Fact]
    public async Task SavingIsNeverRetried()
    {
        // A retry would create a second row: Report.AddQuickStat inserts.
        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().SaveAsync(Sample()));

        Assert.False(request.IsIdempotent);
    }

    [Fact]
    public async Task SavingAPackageWithNoCollectorsSendsAnEmptyString()
    {
        SqlRequest request = await RecordingSqlExecutor.CaptureAsync(
            _executor,
            () => CreateRepository().SaveAsync(Sample() with { CollectorNames = [] }));

        Assert.Equal("", request.NamedValues!["DataElements"]);
    }

    [Fact]
    public async Task DeletingUsesTheQuickStatSchemaProcedure()
    {
        // Note the schema: reading and writing are Report, deleting is QuickStat.
        await CreateRepository().DeleteAsync(88);

        SqlRequest request = _executor.Last;
        Assert.Equal("EXEC QuickStat.DeletePackage :RowId", request.CommandText);
        Assert.Equal(88, request.NamedValues!["RowId"]);
        Assert.False(request.IsIdempotent);
    }

    [Fact]
    public async Task DeletingSurfacesAFailureRatherThanSwallowingIt()
    {
        // Unlike the population audit, a failed delete matters: the user asked for it.
        _executor.ExecuteFailure = new InvalidOperationException("boom");

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateRepository().DeleteAsync(88));
    }

    [Fact]
    public async Task SavingRejectsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateRepository().SaveAsync(null!));
    }
}
