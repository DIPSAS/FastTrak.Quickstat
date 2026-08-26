using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Collectors;
using QuickStat.Collectors.Registry;
using QuickStat.Data;
using Xunit;

namespace QuickStat.Tests.Collectors;

/// <summary>
/// <see cref="CollectorRegistry"/> - the thin wrapper that fetches what
/// <see cref="CollectorRegistryBuilder"/> needs.
/// </summary>
/// <remarks>
/// The ordering and gating logic is tested against the pure builder in the other files; this file
/// only covers the two round trips, which became testable when step 2.2 made
/// <see cref="SqlResultSet"/> publicly constructible.
/// </remarks>
public class CollectorRegistryTests
{
    private static readonly string[] FormClassColumns = ["FormName", "FormTitle"];

    [Fact]
    public async Task BuildReadsTheFormClassesAndRegistersTwoCollectorsForEach()
    {
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(FormClassColumns, ["BARTHEL", "Barthel ADL"], ["LMG", "Legemiddelgjennomgang"]));

        CollectorRegistry registry = Registry(sql);

        IReadOnlyList<ICollector> collectors = await registry.BuildAsync(Session("TARMSCREENING"));

        // 36 always-on plus 2 x 2 dynamic.
        Assert.Equal(40, collectors.Count);
        Assert.Same(collectors, registry.Collectors);

        SqlRequest request = Assert.Single(sql.Requests);

        Assert.Equal("EXEC Report.GetFormClasses :StudyId", request.CommandText);
        Assert.Equal(42, Assert.Single(request.Values));
        Assert.True(request.IsIdempotent);
        Assert.Equal("Report.GetFormClasses", request.Label);
    }

    [Fact]
    public async Task TheStudyNameFromTheSessionDrivesTheGates()
    {
        CollectorRegistry registry = Registry(new RecordingSqlExecutor());

        Assert.Equal(120, (await registry.BuildAsync(Session("KORTTID"))).Count);
        Assert.Equal(36, (await registry.BuildAsync(Session("TARMSCREENING"))).Count);
    }

    [Fact]
    public async Task BuildReplacesThePreviousSessionsList()
    {
        // PrepareStudy clears the list first; a project switch must not accumulate.
        CollectorRegistry registry = Registry(new RecordingSqlExecutor());

        await registry.BuildAsync(Session("KORTTID"));
        await registry.BuildAsync(Session("NDV"));

        Assert.Equal(44, registry.Collectors.Count);
    }

    [Fact]
    public async Task ColumnsAreFoundByNameNotByPosition()
    {
        // Report.GetFormClasses is a stored procedure; the port must not depend on its column order.
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(["FormTitle", "Extra", "FormName"], ["Barthel ADL", 1, "BARTHEL"]));

        IReadOnlyList<ICollector> collectors = await Registry(sql).BuildAsync(Session("TARMSCREENING"));

        Assert.Equal("Skjema-alder: Barthel ADL (BARTHEL) (siste)", CollectorTestContext.ByName(collectors, "BARTHEL").Descriptor.Title);
    }

    [Fact]
    public async Task AMissingFormNameColumnFailsLoudly()
    {
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(["Something", "Else"], ["a", "b"]));

        await Assert.ThrowsAsync<SqlCommandFailedException>(() => Registry(sql).BuildAsync(Session("TARMSCREENING")));
    }

    [Fact]
    public async Task AnEmptyFormClassResultIsFineAndAddsNothing()
    {
        // A study with no form classes is ordinary, not an error. The registry must not demand the
        // FormName column before it knows there is a row to read it from: the Delphi calls
        // FieldByName inside its `while not EOF` loop, so a result set with no rows never has its
        // metadata inspected at all.
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Empty);

        Assert.Equal(36, (await Registry(sql).BuildAsync(Session("TARMSCREENING"))).Count);
    }

    [Fact]
    public async Task AResultSetWithColumnsButNoRowsIsAlsoFine()
    {
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(FormClassColumns));

        Assert.Equal(36, (await Registry(sql).BuildAsync(Session("TARMSCREENING"))).Count);
    }

    [Fact]
    public async Task NoAvailabilityProbeIsIssuedWhileNoCollectorNeedsOne()
    {
        // Phase 4 adds KB.AntibioticResistance2 and the probe starts running; until then a second
        // round trip on every project switch would be pure cost.
        Assert.Empty(CollectorRegistryBuilder.RequiredDatabaseObjects);

        RecordingSqlExecutor sql = new();

        await Registry(sql).BuildAsync(Session("KORTTID"));

        Assert.Single(sql.Requests);
    }

    [Fact]
    public async Task TryFindMatchesNameAndTitleCaseInsensitively()
    {
        CollectorRegistry registry = Registry(new RecordingSqlExecutor());

        await registry.BuildAsync(Session("KORTTID"));

        Assert.True(registry.TryFind("LAB.ANEMIA", out ICollector? byName));
        Assert.Equal(CollectorNames.LabAnemia, byName!.Descriptor.Name);

        Assert.True(registry.TryFind("lab.anemia", out ICollector? byLowerCaseName));
        Assert.Same(byName, byLowerCaseName);

        // TryFindCollector tests SameText against the title too, which is how a saved package that
        // stored a title rather than a name still re-ticks.
        Assert.True(registry.TryFind("Labdata: Anemi (siste)", out ICollector? byTitle));
        Assert.Same(byName, byTitle);

        Assert.False(registry.TryFind("NO.SUCH.COLLECTOR", out ICollector? missing));
        Assert.Null(missing);
    }

    [Fact]
    public async Task TryFindOnlySeesWhatTheCurrentStudyRegistered()
    {
        CollectorRegistry registry = Registry(new RecordingSqlExecutor());

        await registry.BuildAsync(Session("TARMSCREENING"));

        // A saved package from a GBD study re-ticked against an ungated one finds nothing, which is
        // the "one warning per package" path in the UI rather than a crash.
        Assert.False(registry.TryFind(CollectorNames.GbdScores, out _));
        Assert.True(registry.TryFind(CollectorNames.LabAnemia, out _));
    }

    [Fact]
    public void CollectorsIsEmptyBeforeTheFirstBuild() => Assert.Empty(Registry(new RecordingSqlExecutor()).Collectors);

    [Fact]
    public async Task CancellationPropagates()
    {
        RecordingSqlExecutor sql = new();
        using CancellationTokenSource cancellation = new();

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Registry(sql).BuildAsync(Session("KORTTID"), cancellation.Token));
    }

    [Fact]
    public async Task DynamicCollectorsSitBetweenTheTwoAlwaysOnBlocks()
    {
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(FormClassColumns, ["FORM12", "Anonymous"], ["BARTHEL", "Barthel ADL"]));

        List<string> names = CollectorTestContext.Names(await Registry(sql).BuildAsync(Session("TARMSCREENING")));

        // The anonymous form is skipped, so only one pair arrives.
        Assert.Equal(38, names.Count);
        Assert.Equal(names.IndexOf(CollectorNames.LabCount60M) + 1, names.IndexOf("BARTHEL"));
        Assert.Equal(names.IndexOf("FORM.BARTHEL") + 1, names.IndexOf(CollectorNames.Size));
    }

    private static CollectorRegistry Registry(ISqlExecutor sql) => new(sql, NullLogger<CollectorRegistry>.Instance);

    private static SessionContext Session(string studyName) => new()
    {
        StudyName = studyName,
        StudyId = CollectorTestContext.StudyId,
        SessionId = 1,
        User = new StudyUser { UserId = 1, UserName = "test" },
        Database = new DatabaseInfo(),
        ServerName = "SERVER",
        DatabaseName = "DB",
    };
}
