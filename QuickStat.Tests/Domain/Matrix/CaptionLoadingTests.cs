using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Data;
using QuickStat.Domain.Matrix;
using QuickStat.Tests.Data.Fakes;
using Xunit;

namespace QuickStat.Tests.Domain.Matrix;

/// <summary>
/// The database half of the caption dictionary: the one query QuickStat runs, and the merge rules
/// around it.
/// </summary>
/// <remarks>
/// Until this existed nothing in production code ever called <c>AddRange</c>, so
/// <c>dbo.LabClass</c> was never read and every lab column fell back to its raw variable name with
/// an empty description — the grid's header tooltips would have been blank for exactly the columns
/// that most need them.
/// </remarks>
public class CaptionLoadingTests
{
    private static SqlResultSet LabRows(params (string VarName, string Caption)[] rows) =>
        SqlResultSet.Create(
            [CaptionSql.ColVarName, CaptionSql.ColCaption, CaptionSql.ColVarDescription],
            [.. rows.Select(r => new object?[] { r.VarName, r.Caption, null })]);

    private static CaptionRepository Repository(RecordingSqlExecutor sql) =>
        new(sql, NullLogger<CaptionRepository>.Instance);

    private static CaptionLoader Loader(ICaptionRepository repository, CaptionDictionary captions) =>
        new(repository, captions, NullLogger<CaptionLoader>.Instance);

    [Fact]
    public void TheQueryIsTheDelphiQueryCharacterForCharacter() =>
        // EPR.QA.SQL.pas:153-157, with the field-name constants substituted.  Pinned as a literal
        // because the ISNULL fallback and the ORDER BY are both load-bearing: the first decides the
        // variable name, the second decides which of two colliding names wins the first-wins merge.
        Assert.Equal(
            "SELECT ISNULL(NLK, Report.LabClassName(LabClassId)) AS VarName, "
            + "FriendlyName AS Caption, "
            + "NULL AS VarDescription "
            + "FROM dbo.LabClass ORDER BY LabClassId",
            CaptionSql.LabCaptions);

    [Fact]
    public void TheQueryIsMarkedIdempotentSoItMayBeRetried()
    {
        SqlRequest request = CaptionSql.LabCaptionRequest();

        Assert.True(request.IsIdempotent);
        Assert.Equal(CaptionSql.LabCaptionsLabel, request.Label);
        Assert.Empty(request.Values);
    }

    [Fact]
    public async Task TheRepositoryReadsEveryRowInOrder()
    {
        RecordingSqlExecutor sql = new RecordingSqlExecutor()
            .Returns(LabRows(("B-Hemo", "Hemoglobin"), ("P-B12", "Vitamin B12")));

        IReadOnlyList<CaptionRecord> captions = await Repository(sql).GetLabCaptionsAsync();

        Assert.Equal(["B-Hemo", "P-B12"], captions.Select(c => c.VarName));
        Assert.Equal(["Hemoglobin", "Vitamin B12"], captions.Select(c => c.Title));
        Assert.Equal(CaptionSql.LabCaptions, Assert.Single(sql.Statements));
    }

    [Fact]
    public async Task ANullDescriptionBecomesAnEmptyString()
    {
        RecordingSqlExecutor sql = new RecordingSqlExecutor().Returns(LabRows(("B-Hemo", "Hemoglobin")));

        CaptionRecord caption = Assert.Single(await Repository(sql).GetLabCaptionsAsync());

        // The query selects NULL AS VarDescription unconditionally, so this is every lab caption,
        // not an edge case.  CaptionRecord.Description is non-nullable.
        Assert.Equal("", caption.Description);
    }

    [Fact]
    public async Task ARowWithNoVariableNameIsDroppedRatherThanThrowing()
    {
        // Both NLK and Report.LabClassName(LabClassId) can be null.  CaptionDictionary rejects an
        // empty key, so one such reference row would otherwise throw away the whole load.
        RecordingSqlExecutor sql = new RecordingSqlExecutor()
            .Returns(LabRows(("", "Orphan"), ("B-Hemo", "Hemoglobin")));

        IReadOnlyList<CaptionRecord> captions = await Repository(sql).GetLabCaptionsAsync();

        Assert.Equal("B-Hemo", Assert.Single(captions).VarName);
    }

    [Fact]
    public async Task TheLoaderMergesDatabaseCaptionsOnTopOfTheBuiltInOnes()
    {
        CaptionDictionary captions = CaptionDictionary.WithQuickStatDefaults();
        RecordingSqlExecutor sql = new RecordingSqlExecutor().Returns(LabRows(("B-Hemo", "Hemoglobin")));

        int added = await Loader(Repository(sql), captions).LoadAsync();

        Assert.Equal(1, added);
        Assert.Equal("Hemoglobin", captions.GetVarTitle("B-Hemo"));
        Assert.Equal("DDI-R", captions.GetVarTitle("DRUID.RED"));
        Assert.Equal(CaptionDictionary.QuickStatDefaults.Count + 1, captions.Count);
    }

    [Fact]
    public async Task ABuiltInCaptionBeatsADatabaseCaptionForTheSameVariable()
    {
        // The whole point of the asymmetry in EPR.QA.CaptionDictionary.pas: AddCaption overwrites,
        // the database merge is first-wins, and the built-ins go in first.
        CaptionDictionary captions = CaptionDictionary.WithQuickStatDefaults();
        RecordingSqlExecutor sql = new RecordingSqlExecutor().Returns(LabRows(("DRUID.RED", "Something else")));

        int added = await Loader(Repository(sql), captions).LoadAsync();

        Assert.Equal(0, added);
        Assert.Equal("DDI-R", captions.GetVarTitle("DRUID.RED"));
    }

    [Fact]
    public async Task TheFirstOfTwoCollidingDatabaseCaptionsWins()
    {
        CaptionDictionary captions = CaptionDictionary.WithQuickStatDefaults();
        RecordingSqlExecutor sql = new RecordingSqlExecutor()
            .Returns(LabRows(("B-Hemo", "First"), ("B-Hemo", "Second")));

        await Loader(Repository(sql), captions).LoadAsync();

        // Which is why the query's ORDER BY LabClassId is part of the contract.
        Assert.Equal("First", captions.GetVarTitle("B-Hemo"));
    }

    [Fact]
    public async Task LoadingAgainDropsThePreviousDatabaseCaptions()
    {
        // Deliberate divergence: the Delphi kept one TVarCaptions across every project switch, so
        // database A's captions stayed and beat database B's on the first-wins merge.
        CaptionDictionary captions = CaptionDictionary.WithQuickStatDefaults();
        RecordingSqlExecutor sql = new RecordingSqlExecutor()
            .Returns(LabRows(("A-Only", "From A")))
            .Returns(LabRows(("B-Only", "From B")));
        ICaptionLoader loader = Loader(Repository(sql), captions);

        await loader.LoadAsync();
        await loader.LoadAsync();

        Assert.Equal("From B", captions.GetVarTitle("B-Only"));

        // A-Only is gone, so GetVarTitle falls back to the variable name itself.
        Assert.Equal("A-Only", captions.GetVarTitle("A-Only"));
        Assert.Equal(CaptionDictionary.QuickStatDefaults.Count + 1, captions.Count);
    }

    [Fact]
    public async Task AFailedQueryKeepsTheCaptionsAlreadyLoaded()
    {
        // The Delphi's outer handler called fTitles.Clear (EPR.QA.CaptionDictionary.pas:135-141), so
        // a database without Report.LabClassName lost the twelve built-in captions as well and every
        // grid column fell back to its raw variable name.  Captions are cosmetic; they must never
        // fail a login, and they must never take the working ones down with them.
        CaptionDictionary captions = CaptionDictionary.WithQuickStatDefaults();
        RecordingSqlExecutor sql = new RecordingSqlExecutor()
            .Throws(new SqlCommandFailedException("Invalid object name 'Report.LabClassName'."));

        int added = await Loader(Repository(sql), captions).LoadAsync();

        Assert.Equal(0, added);
        Assert.Equal(CaptionDictionary.QuickStatDefaults.Count, captions.Count);
        Assert.Equal("DDI-R", captions.GetVarTitle("DRUID.RED"));
    }

    [Fact]
    public async Task CancellationIsNotSwallowedAsACaptionFailure()
    {
        CaptionDictionary captions = CaptionDictionary.WithQuickStatDefaults();
        RecordingSqlExecutor sql = new RecordingSqlExecutor().Throws(new OperationCanceledException());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Loader(Repository(sql), captions).LoadAsync());
    }

    [Fact]
    public void TheMatrixAndTheLoaderShareOneDictionary()
    {
        // If these ever resolve to different instances the loader fills a dictionary nobody reads,
        // and the only symptom is that every column shows its raw variable name.
        ServiceCollection services = new();
        services.AddLogging(builder => builder.ClearProviders());
        services.AddSingleton<ISqlExecutor>(new RecordingSqlExecutor());
        services.AddQuickStatMatrix();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Same(
            provider.GetRequiredService<CaptionDictionary>(),
            provider.GetRequiredService<ITitleDictionary>());
        Assert.NotNull(provider.GetRequiredService<ICaptionLoader>());
        Assert.NotNull(provider.GetRequiredService<ICaptionRepository>());
    }
}
