using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Collectors;
using QuickStat.Configuration;
using QuickStat.Data;
using QuickStat.Domain.Matrix;
using Xunit;

namespace QuickStat.Tests.Collectors;

/// <summary>
/// <see cref="CollectorRunner"/> - batching, the three <see cref="PidBinding"/> paths, the
/// cohort filter and the column order that reaches the export.
/// </summary>
/// <remarks>
/// Untestable until step 2.5 implemented <see cref="VariableNameSet"/> and step 2.2 made
/// <see cref="SqlResultSet"/> publicly constructible. Still no database (PORT-PLAN.md R9).
/// </remarks>
public class CollectorRunnerTests
{
    /// <summary>The five positional columns every collector query must project, in order.</summary>
    private static readonly string[] FiveColumns = ["PersonId", "VarName", "DpValue", "VarDate", "RowId"];

    private static readonly DateTime Timestamp = new(2024, 5, 17, 8, 30, 0, DateTimeKind.Unspecified);

    // ---- Batching ------------------------------------------------------------------------------

    [Fact]
    public async Task IdListCollectorIsBatchedByItsDescriptorSize()
    {
        // 250 people at 100 per statement is three round trips: 100, 100, 50.
        RecordingSqlExecutor sql = new();
        CollectorRunner runner = Runner(sql);

        CollectorRunSummary summary = await runner.RunAsync(
            IdListCollector(batchSize: 100),
            [.. Enumerable.Range(1, 250)],
            studyId: 42,
            new RecordingSink());

        Assert.Equal(3, summary.BatchCount);
        Assert.Equal(3, sql.Requests.Count);

        // Contiguous, in cohort order, and the parentheses come from the fragment - the placeholder
        // itself is bare, exactly as the Delphi's '(' + pidList.DelimitedText + ')' produces.
        Assert.Contains("IN (1,2,3,", sql.Requests[0].CommandText, StringComparison.Ordinal);
        Assert.EndsWith(",99,100)", sql.Requests[0].CommandText, StringComparison.Ordinal);
        Assert.Contains("IN (101,102,", sql.Requests[1].CommandText, StringComparison.Ordinal);
        Assert.Contains("IN (201,202,", sql.Requests[2].CommandText, StringComparison.Ordinal);
        Assert.EndsWith(",249,250)", sql.Requests[2].CommandText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FormDataCollectorUsesItsLargerBatchSize()
    {
        // PORT-PLAN.md §8.5: SpSnapshotFormDataAll runs 200 at a time, not 100.
        RecordingSqlExecutor sql = new();

        CollectorRunSummary summary = await Runner(sql).RunAsync(
            IdListCollector(batchSize: 200),
            [.. Enumerable.Range(1, 250)],
            studyId: 42,
            new RecordingSink());

        Assert.Equal(2, summary.BatchCount);
    }

    [Fact]
    public async Task TheBinderCeilingCapsAWholeCohortIdListCollector()
    {
        // The four drug-set collectors have maxint batches AND {IdList}. Upstream inlines the whole
        // population; the literal binder chunks at SqlOptions.MaxIdsPerBatch instead, which cannot
        // change a per-person projection.
        RecordingSqlExecutor sql = new();
        CollectorRunner runner = Runner(sql, new SqlOptions { MaxIdsPerBatch = 60 });

        CollectorRunSummary summary = await runner.RunAsync(
            IdListCollector(batchSize: int.MaxValue),
            [.. Enumerable.Range(1, 250)],
            studyId: 42,
            new RecordingSink());

        Assert.Equal(5, summary.BatchCount);
    }

    [Fact]
    public async Task AnEmptyCohortIssuesNoStatementAtAll()
    {
        RecordingSqlExecutor sql = new();

        CollectorRunSummary summary = await Runner(sql).RunAsync(
            IdListCollector(),
            [],
            studyId: 42,
            new RecordingSink());

        Assert.Equal(0, summary.BatchCount);
        Assert.Empty(sql.Requests);
        Assert.Empty(summary.VariableNames);
    }

    // ---- The three PidBinding paths --------------------------------------------------------------

    [Fact]
    public async Task SinglePersonCollectorIssuesOneRoundTripPerPatientAndBindsPositionally()
    {
        RecordingSqlExecutor sql = new();

        CollectorRunSummary summary = await Runner(sql).RunAsync(
            SinglePersonCollector(),
            [7, 8, 9],
            studyId: 42,
            new RecordingSink());

        Assert.Equal(3, summary.BatchCount);
        Assert.Equal(3, sql.Requests.Count);

        // The statement keeps its :PersonId placeholder and the id travels as the only value.
        Assert.All(sql.Requests, request => Assert.Contains(":PersonId", request.CommandText, StringComparison.Ordinal));
        Assert.All(sql.Requests, request => Assert.Empty(request.TableParameters));

        List<object?> boundIds = [.. sql.Requests.Select(request => Assert.Single(request.Values))];

        Assert.Equal<object?>([7, 8, 9], boundIds);
    }

    [Fact]
    public async Task WholeDatabaseCollectorIssuesOneStatementWithNoIdsAndFiltersClientSide()
    {
        // PORT-PLAN.md R10, the behaviour that is preserved rather than fixed: no {IdList} in the
        // statement, one pass over everything, and the cohort filter happens here.
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(
            FiveColumns,
            [1, "AGE", 80d, Timestamp, 11],
            [999, "AGE", 55d, Timestamp, 12],   // not in the cohort
            [2, "AGE", 71d, Timestamp, 13],
            [1000, "AGE", 40d, Timestamp, 14])); // not in the cohort

        RecordingSink sink = new();

        CollectorRunSummary summary = await Runner(sql).RunAsync(
            WholeDatabaseCollector(),
            [1, 2, 3],
            studyId: 42,
            sink);

        Assert.Single(sql.Requests);
        Assert.DoesNotContain("(1,2,3)", sql.Requests[0].CommandText, StringComparison.Ordinal);

        Assert.Equal(2, summary.RowsAccepted);
        Assert.Equal(2, summary.RowsForUnknownPersons);

        // The discarded rows never reach the sink at all.
        Assert.Equal([1, 2], sink.Offers.Select(offer => offer.Row.PersonId));
    }

    [Fact]
    public async Task AWholeDatabaseCollectorIsNotChunkedEvenForALargeCohort()
    {
        RecordingSqlExecutor sql = new();

        CollectorRunSummary summary = await Runner(sql, new SqlOptions { MaxIdsPerBatch = 10 }).RunAsync(
            WholeDatabaseCollector(),
            [.. Enumerable.Range(1, 5000)],
            studyId: 42,
            new RecordingSink());

        Assert.Equal(1, summary.BatchCount);
    }

    [Fact]
    public async Task TheCohortFilterIsPerBatchNotPerRun()
    {
        // TDataCollector filters against FBatch, which holds only the current chunk. A row for a
        // person who is in the cohort but in a *different* chunk is discarded, and that is what the
        // "Unknown patients found" diagnostic counts on an {IdList} collector.
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(FiveColumns, [1, "AGE", 80d, Timestamp, 11], [2, "AGE", 71d, Timestamp, 12]));
        sql.Enqueue(SqlResultSet.Create(FiveColumns, [2, "AGE", 71d, Timestamp, 12]));

        CollectorRunSummary summary = await Runner(sql).RunAsync(
            IdListCollector(batchSize: 1),
            [1, 2],
            studyId: 42,
            new RecordingSink());

        Assert.Equal(2, summary.BatchCount);
        Assert.Equal(2, summary.RowsAccepted);
        Assert.Equal(1, summary.RowsForUnknownPersons);
    }

    // ---- Reading -----------------------------------------------------------------------------------

    [Fact]
    public async Task RowsAreReadByOrdinalAndPrefixed()
    {
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(FiveColumns, [1, "A10.F", 4711d, Timestamp, 90210]));

        RecordingSink sink = new();

        await Runner(sql).RunAsync(IdListCollector(varPrefix: "ATC_"), [1], studyId: 42, sink);

        (string columnName, CollectorResultRow row) = Assert.Single(sink.Offers);

        Assert.Equal("ATC_A10.F", columnName);
        Assert.Equal(1, row.PersonId);
        Assert.Equal("A10.F", row.VarName);
        Assert.Equal(4711d, row.Value);
        Assert.Equal(Timestamp, row.Timestamp);
        Assert.Equal(90210, row.RowId);

        // Absent optional columns leave their defaults.
        Assert.Equal(0, row.ItemId);
        Assert.Null(row.Caption);
    }

    [Fact]
    public async Task ItemIdAndCaptionAreReadByNameAndSurviveExtraColumns()
    {
        // Extra columns after position 4 are tolerated; ItemId and Caption are found by name
        // wherever they sit, which is the whole reason the contract treats them differently.
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(
            ["PersonId", "VarName", "DpValue", "VarDate", "RowId", "OrderBy", "Caption", "ItemId"],
            [1, "WEIGHT", 82.5d, Timestamp, 5, 1, "Fritekst", 3224]));

        RecordingSink sink = new();

        await Runner(sql).RunAsync(IdListCollector(), [1], studyId: 42, sink);

        CollectorResultRow row = Assert.Single(sink.Offers).Row;

        Assert.Equal(3224, row.ItemId);
        Assert.Equal("Fritekst", row.Caption);
    }

    [Fact]
    public async Task NullsReadWithDelphiFieldSemantics()
    {
        // dataset.Fields[2].AsFloat on a NULL yields 0, and AsDateTime yields the 1899 zero date -
        // not an exception and not DateTime.MinValue.
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(FiveColumns, [1, "AGE", null, null, null]));

        RecordingSink sink = new();

        await Runner(sql).RunAsync(IdListCollector(), [1], studyId: 42, sink);

        CollectorResultRow row = Assert.Single(sink.Offers).Row;

        Assert.Equal(0d, row.Value);
        Assert.Equal(SqlRow.ZeroDate, row.Timestamp);
        Assert.Equal(0, row.RowId);
    }

    // ---- Column order: the upstream half of the chain 2.6 already proved ----------------------------

    [Fact]
    public async Task VariableNamesAccumulateInArrivalOrder()
    {
        // ColumnOrder.FirstSeen means "the order the rows arrived", which for form data is on-form
        // item order because the query carries ORDER BY mfi.OrderNumber. 2.6 proved insertion order
        // survives from matrix to file bytes; this is the half that feeds it.
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(
            FiveColumns,
            [1, "ZEBRA", 1d, Timestamp, 1],
            [1, "ALPHA", 2d, Timestamp, 2],
            [2, "ZEBRA", 3d, Timestamp, 3],
            [1, "MIDDLE", 4d, Timestamp, 4]));

        CollectorRunSummary summary = await Runner(sql).RunAsync(
            IdListCollector(),
            [1, 2],
            studyId: 42,
            new RecordingSink());

        Assert.Equal(["ZEBRA", "ALPHA", "MIDDLE"], summary.VariableNames);
    }

    [Fact]
    public async Task VariableNamesAreDeDuplicatedAcrossBatches()
    {
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(FiveColumns, [1, "AGE", 1d, Timestamp, 1], [1, "YOB", 2d, Timestamp, 2]));
        sql.Enqueue(SqlResultSet.Create(FiveColumns, [2, "AGE", 3d, Timestamp, 3], [2, "MOB", 4d, Timestamp, 4]));

        CollectorRunSummary summary = await Runner(sql).RunAsync(
            IdListCollector(batchSize: 1),
            [1, 2],
            studyId: 42,
            new RecordingSink());

        Assert.Equal(["AGE", "YOB", "MOB"], summary.VariableNames);
    }

    [Fact]
    public async Task TheSinkChoosesTheColumnOrderPolicy()
    {
        // Only the sink knows the policy - PersonMatrix.ColumnOrder is settable - so the sink makes
        // the set and the runner fills it. Before this the runner constructed the set itself and
        // PersonMatrix.ColumnOrder was a property nobody read.
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(
            FiveColumns,
            [1, "ZEBRA", 1d, Timestamp, 1],
            [1, "ALPHA", 2d, Timestamp, 2]));

        RecordingSink sink = new() { ColumnOrder = ColumnOrder.Alphabetical };

        CollectorRunSummary summary = await Runner(sql).RunAsync(IdListCollector(), [1], studyId: 42, sink);

        Assert.Equal(1, sink.VariableNameSetsCreated);
        Assert.Equal(ColumnOrder.Alphabetical, summary.VariableNames.Order);
        Assert.Equal(["ALPHA", "ZEBRA"], summary.VariableNames);
    }

    [Fact]
    public async Task ASinkThatDoesNotCareGetsInsertionOrder()
    {
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(FiveColumns, [1, "ZEBRA", 1d, Timestamp, 1], [1, "ALPHA", 2d, Timestamp, 2]));

        CollectorRunSummary summary = await Runner(sql).RunAsync(IdListCollector(), [1], studyId: 42, new MinimalSink());

        Assert.Equal(ColumnOrder.FirstSeen, summary.VariableNames.Order);
        Assert.Equal(["ZEBRA", "ALPHA"], summary.VariableNames);
    }

    [Fact]
    public async Task ARejectedRowStillCreatesItsColumn()
    {
        // EPR.QA.Collector.Base.pas:160-170 records the variable name before it tries to store the
        // datapoint, so a column exists even when every value for it is a rejected duplicate.
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(FiveColumns, [1, "AGE", 1d, Timestamp, 1], [1, "AGE", 2d, Timestamp, 2]));

        RecordingSink sink = new((_, row) => row.RowId == 1);

        CollectorRunSummary summary = await Runner(sql).RunAsync(IdListCollector(), [1], studyId: 42, sink);

        Assert.Equal(["AGE"], summary.VariableNames);
        Assert.Equal(1, summary.RowsAccepted);
        Assert.Equal(0, summary.RowsForUnknownPersons);

        // Both rows were offered; only the first was kept. SpRecentQuantityPresent depends on this -
        // it returns every value in the window and the oldest one wins.
        Assert.Equal(2, sink.Offers.Count);
    }

    // ---- End to end through the real matrix ---------------------------------------------------------

    [Fact]
    public async Task RunningIntoTheRealMatrixProducesColumnsInArrivalOrder()
    {
        // The seam 2.5 implements. Nothing here fakes the sink: PersonMatrix does the cohort lookup,
        // the datapoint creation and the duplicate rejection.
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(
            ["PersonId", "VarName", "DpValue", "VarDate", "RowId", "Caption"],
            [8, "SCORE", 12d, Timestamp, 1, null],
            [8, "NOTAT", 0d, Timestamp, 2, "Fritekst frå skjema"],
            [4711, "SCORE", 99d, Timestamp, 3, null]));

        PersonMatrix matrix = new(new QuickStat.Domain.DataPoints.DataPointFactory());

        matrix.PreparePopulation(
        [
            new QuickStat.Domain.Patients.Patient { PersonId = 8, FirstName = "Ola", LastName = "Hansen" },
        ]);

        CollectorRunSummary summary = await Runner(sql).RunAsync(
            IdListCollector(varPrefix: "GBD."),
            [8],
            studyId: 42,
            matrix);

        matrix.AddColumns(summary.VariableNames);
        matrix.Lock();

        Assert.Equal(["GBD.SCORE", "GBD.NOTAT"], matrix.Columns.Select(column => column.VarName));
        Assert.Equal(1, summary.RowsForUnknownPersons);

        // A caption reaches the cell, which is how free-text form answers get into a file at all.
        Assert.True(matrix.TryGetDataPoint(0, 1, out QuickStat.Domain.DataPoints.DataPoint? note));
        Assert.Equal("Fritekst frå skjema", note!.Caption);
    }

    [Fact]
    public async Task TheMatrixColumnOrderPolicyReachesTheColumns()
    {
        RecordingSqlExecutor sql = new();

        sql.Enqueue(SqlResultSet.Create(FiveColumns, [8, "ZEBRA", 1d, Timestamp, 1], [8, "ALPHA", 2d, Timestamp, 2]));

        PersonMatrix matrix = new(new QuickStat.Domain.DataPoints.DataPointFactory())
        {
            ColumnOrder = ColumnOrder.Alphabetical,
        };

        matrix.PreparePopulation([new QuickStat.Domain.Patients.Patient { PersonId = 8 }]);

        CollectorRunSummary summary = await Runner(sql).RunAsync(IdListCollector(), [8], studyId: 42, matrix);

        matrix.AddColumns(summary.VariableNames);

        Assert.Equal(["ALPHA", "ZEBRA"], matrix.Columns.Select(column => column.VarName));
    }

    // ---- Diagnostics and cancellation ---------------------------------------------------------------

    [Fact]
    public async Task TheSummaryNamesTheCollectorAndCountsTheBatches()
    {
        RecordingSqlExecutor sql = new();
        ICollector collector = IdListCollector();

        CollectorRunSummary summary = await Runner(sql).RunAsync(
            collector,
            [.. Enumerable.Range(1, 150)],
            studyId: 42,
            new RecordingSink());

        Assert.Same(collector.Descriptor, summary.Descriptor);
        Assert.Equal(2, summary.BatchCount);
    }

    [Fact]
    public async Task ProgressIsReportedOncePerBatch()
    {
        RecordingSqlExecutor sql = new();
        RecordingProgress progress = new();

        await Runner(sql).RunAsync(
            IdListCollector(),
            [.. Enumerable.Range(1, 250)],
            studyId: 42,
            new RecordingSink(),
            progress);

        Assert.Equal(3, progress.Reports.Count);
        Assert.All(progress.Reports, report => Assert.Equal("Collecting data", report.Header));
        Assert.Contains("(1/3)", progress.Reports[0].Info, StringComparison.Ordinal);
        Assert.Equal(0d, progress.Reports[0].Percent);
    }

    [Fact]
    public async Task CancellationStopsBetweenBatches()
    {
        RecordingSqlExecutor sql = new();
        using CancellationTokenSource cancellation = new();

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Runner(sql).RunAsync(
            IdListCollector(),
            [.. Enumerable.Range(1, 250)],
            studyId: 42,
            new RecordingSink(),
            progress: null,
            cancellation.Token));

        Assert.Empty(sql.Requests);
    }

    [Fact]
    public async Task TheRequestIsIdempotentAndLabelledWithTheCollectorName()
    {
        // Collector queries are reads, so the retry policy may re-run them; the label is what shows
        // up in the log and the busy indicator.
        RecordingSqlExecutor sql = new();

        await Runner(sql).RunAsync(IdListCollector(), [1], studyId: 42, new RecordingSink());

        SqlRequest request = Assert.Single(sql.Requests);

        Assert.True(request.IsIdempotent);
        Assert.Equal("TEST.COLLECTOR", request.Label);
    }

    [Fact]
    public async Task TheStudyIdReachesTheStatement()
    {
        RecordingSqlExecutor sql = new();

        Collector studyScoped = new(
            Descriptor("TEST.STUDY", PidBinding.None, int.MaxValue, ""),
            context => "SELECT 1 WHERE StudyId = " + context.StudyId.ToString(System.Globalization.CultureInfo.InvariantCulture));

        await Runner(sql).RunAsync(studyScoped, [1], studyId: 4711, new RecordingSink());

        Assert.Equal("SELECT 1 WHERE StudyId = 4711", Assert.Single(sql.Requests).CommandText);
    }

    [Fact]
    public async Task TheTableValuedBinderPutsTheIdsInATableParameter()
    {
        RecordingSqlExecutor sql = new();
        SqlOptions options = new();

        CollectorRunner runner = new(sql, new TableValuedPersonIdListBinder(options), NullLogger<CollectorRunner>.Instance);

        await runner.RunAsync(IdListCollector(), [4, 5, 6], studyId: 42, new RecordingSink());

        SqlRequest request = Assert.Single(sql.Requests);

        Assert.Contains("(SELECT PersonId FROM @pids)", request.CommandText, StringComparison.Ordinal);

        SqlTableParameter table = Assert.Single(request.TableParameters);

        Assert.Equal("pids", table.Name);
        Assert.Equal(options.PersonIdListTypeName, table.TypeName);
        Assert.Equal(options.PersonIdListColumnName, table.ColumnName);
        Assert.Equal([4, 5, 6], table.Values);
    }

    // ---- Helpers -------------------------------------------------------------------------------------

    private static CollectorRunner Runner(ISqlExecutor sql, SqlOptions? options = null) =>
        new(sql, new InlineLiteralPersonIdListBinder(options ?? new SqlOptions()), NullLogger<CollectorRunner>.Instance);

    private static CollectorDescriptor Descriptor(string name, PidBinding binding, int batchSize, string varPrefix) => new()
    {
        Name = name,
        Title = "Test: " + name,
        Kind = CollectorKind.Custom,
        VarPrefix = varPrefix,
        PidBinding = binding,
        BatchSize = batchSize,
    };

    private static Collector IdListCollector(int batchSize = 100, string varPrefix = "") =>
        new(
            Descriptor("TEST.COLLECTOR", PidBinding.IdList, batchSize, varPrefix),
            context => "SELECT * FROM T WHERE PersonId IN " + context.IdListFragment);

    private static Collector WholeDatabaseCollector() =>
        new(Descriptor("TEST.SCAN", PidBinding.None, int.MaxValue, ""), _ => "SELECT * FROM T");

    private static Collector SinglePersonCollector() =>
        new(Descriptor("TEST.ONEBYONE", PidBinding.SinglePerson, 1, "FORM."), _ => "EXEC Report.GetFormInstances :PersonId");
}
