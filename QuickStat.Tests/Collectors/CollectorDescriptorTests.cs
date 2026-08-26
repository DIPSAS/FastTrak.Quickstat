using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using QuickStat.Collectors;
using QuickStat.Collectors.Registry;
using QuickStat.Configuration;
using QuickStat.Data;
using Xunit;

namespace QuickStat.Tests.Collectors;

/// <summary>
/// Descriptor invariants, batching, the person-id binders and the DI registration.
/// </summary>
public class CollectorDescriptorTests
{
    [Fact]
    public void BatchSizesAreTheFourDelphiValues()
    {
        // 1, 100, 200 and maxint. They are accidents of history, transcribed faithfully so that the
        // first port is comparable against a Delphi trace.
        HashSet<int> allowed = [1, 100, 200, int.MaxValue];
        HashSet<int> sizes = [.. CollectorCatalog.All.Select(collector => collector.Descriptor.BatchSize)];

        Assert.Subset(allowed, sizes);
    }

    [Fact]
    public void EveryDescriptorHasANameAndATitle()
    {
        foreach (ICollector collector in CollectorCatalog.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(collector.Descriptor.Name));
            Assert.False(string.IsNullOrWhiteSpace(collector.Descriptor.Title));
        }
    }

    [Theory]
    // Docs/Port/03-collectors.md §0.5 and §A. A wrong prefix renames every column the collector
    // produces, in the grid and in the export.
    [InlineData("PATIENT.AGE", "")]
    [InlineData("STUDY.STATUS", "")]
    [InlineData("FORMS.FREQUENCY", "FORM.")]
    [InlineData("FORMS24M.FREQUENCY", "FORMS24M.")]
    [InlineData("LAB.ANEMIA", "")]
    [InlineData("LAB.TRUST3", "")]
    [InlineData("LAB.COUNT_3M", "")]
    [InlineData("GBD.WEIGHT.DAYS", "ITEMAGE.")]
    [InlineData("GBD.WEIGHT_2M", "LAST2M.")]
    [InlineData("GBD.SBP_2M", "LAST2M.")]
    [InlineData("GBD.HULTEN_3M", "LAST3M.")]
    [InlineData("GBD.QUALID_6M", "LAST6M.")]
    [InlineData("GBD.FLACKER_12M", "LAST12M.")]
    [InlineData("GBD.FLACKER_DEATH", "FK.")]
    [InlineData("GBD.ANTIHT_LOW_BP", "OVERTREAT.")]
    [InlineData("GBD.LOW_BP", "LASTUNDER.")]
    [InlineData("GBD.C09_GFR", "LASTUNDER.")]
    [InlineData("GBD.LMG_6M", "FORMS6M.")]
    [InlineData("FORMS3M.LEGEALLE", "FORMS.")]
    [InlineData("FORMS12M.GBD_INNLEGGELSE", "FORMS12M.")]
    [InlineData("DX.ALL1", "DXC.")]
    [InlineData("DX.C", "DX.")]
    [InlineData("DX.DEMENTIA", "DX.")]
    [InlineData("RXDX.E1xA10", "RXDX.")]
    [InlineData("DRUG.A10", "ATC_")]
    [InlineData("DRUG.GROUPCOUNT", "ATCn_")]
    [InlineData("DRUG.NOATC", "ATCn_")]
    [InlineData("DRUG.COUNT", "TREATn_")]
    [InlineData("DRUG.METFORMIN", "DRUG_")]
    [InlineData("DRUG.RESISTANCE_DRIVING", "DRUG_")]
    [InlineData("DRUG.NorGEP", "NorGeP")]
    [InlineData("DRUID.COUNT", "DRUID.")]
    [InlineData("DRUID.SPECIFIED", "")]
    [InlineData("NDV.DIAGNOSE", "")]
    [InlineData("ROAS.GWAS.AB", "ITEMMAX.")]
    public void VariablePrefixesMatchTheDelphiConstructors(string collectorName, string expected) =>
        Assert.Equal(expected, CollectorTestContext.ByName(CollectorCatalog.All, collectorName).Descriptor.VarPrefix);

    [Fact]
    public void ColumnNameIsPlainConcatenation()
    {
        // EPR.QA.Collector.Base.pas:160. Inserting a separator would rename every column in every
        // export; several prefixes already end in a dot and several are empty.
        CollectorResultRow row = new()
        {
            PersonId = 1,
            VarName = "A10.F",
            Value = 0,
            Timestamp = DateTime.MinValue,
            RowId = 0,
        };

        Assert.Equal("ATC_A10.F", row.ColumnName("ATC_"));
        Assert.Equal("A10.F", row.ColumnName(""));
        Assert.Equal("LAST6M.A10.F", row.ColumnName("LAST6M."));
    }

    [Theory]
    // A collector with no {IdList} always takes the whole cohort: there is nothing to chunk.
    [InlineData(PidBinding.None, int.MaxValue, 1000, 2500, 2500)]
    [InlineData(PidBinding.None, 100, 1000, 2500, 2500)]
    // Everything else is capped by the smaller of the descriptor's size and the binder's ceiling.
    [InlineData(PidBinding.IdList, 100, 1000, 2500, 100)]
    [InlineData(PidBinding.IdList, 200, 1000, 2500, 200)]
    [InlineData(PidBinding.IdList, int.MaxValue, 1000, 2500, 1000)]
    [InlineData(PidBinding.IdList, int.MaxValue, int.MaxValue, 2500, int.MaxValue)]
    [InlineData(PidBinding.SinglePerson, 1, 1000, 2500, 1)]
    public void ChunkSizeIsTheSmallerOfTheDescriptorAndTheBinder(
        PidBinding binding,
        int batchSize,
        int maxIdsPerBatch,
        int cohortSize,
        int expected)
    {
        CollectorDescriptor descriptor = new()
        {
            Name = "TEST",
            Title = "Test",
            Kind = CollectorKind.Custom,
            PidBinding = binding,
            BatchSize = batchSize,
        };

        Assert.Equal(expected, CollectorRunner.ChunkSizeFor(descriptor, maxIdsPerBatch, cohortSize));
    }

    [Fact]
    public void InlineBinderEmitsTheDelphiInList()
    {
        InlineLiteralPersonIdListBinder binder = new(new SqlOptions());

        PersonIdListBinding binding = binder.Bind([4711, 88, 12903]);

        Assert.Equal("(4711,88,12903)", binding.Fragment);
        Assert.Null(binding.TableParameter);
        Assert.Equal(1000, binder.MaxIdsPerBatch);
    }

    [Fact]
    public void TableValuedBinderEmitsASubqueryAndATableParameter()
    {
        TableValuedPersonIdListBinder binder = new(new SqlOptions());

        PersonIdListBinding binding = binder.Bind([1, 2, 3]);

        Assert.Equal("(SELECT PersonId FROM @pids)", binding.Fragment);
        Assert.NotNull(binding.TableParameter);
        Assert.Equal("pids", binding.TableParameter!.Name);
        Assert.Equal("Report.PersonIdList", binding.TableParameter.TypeName);
        Assert.Equal("PersonId", binding.TableParameter.ColumnName);
        Assert.Equal([1, 2, 3], binding.TableParameter.Values);
        Assert.Equal(int.MaxValue, binder.MaxIdsPerBatch);
    }

    [Fact]
    public void TableValuedBinderRefusesToExistWhenTheTypeNameIsCleared()
    {
        // A null type name is the documented way of forcing the chunked-literal fallback, so
        // constructing the table-valued binder then is a contradiction rather than a silent
        // downgrade.
        SqlOptions noTvp = new() { PersonIdListTypeName = null };

        Assert.Throws<InvalidOperationException>(() => new TableValuedPersonIdListBinder(noTvp));
    }

    [Fact]
    public void RegistrationResolvesTheThreeCollectorServices()
    {
        ServiceCollection services = new();

        services.AddSingleton(new SqlOptions());
        services.AddSingleton<ISqlExecutor, UnusableSqlExecutor>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddQuickStatCollectors();

        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);

        Assert.IsType<InlineLiteralPersonIdListBinder>(provider.GetRequiredService<IPersonIdListBinder>());
        Assert.IsType<CollectorRegistry>(provider.GetRequiredService<ICollectorRegistry>());
        Assert.IsType<CollectorRunner>(provider.GetRequiredService<ICollectorRunner>());

        // Singletons: the registry holds the list for the current session and TryFind must see the
        // same one the user is looking at.
        Assert.Same(provider.GetRequiredService<ICollectorRegistry>(), provider.GetRequiredService<ICollectorRegistry>());
    }

    [Fact]
    public void RegistrationDoesNotOverrideAnExplicitBinderChoice()
    {
        ServiceCollection services = new();

        services.AddSingleton(new SqlOptions());
        services.AddSingleton<ISqlExecutor, UnusableSqlExecutor>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IPersonIdListBinder, TableValuedPersonIdListBinder>();

        services.AddQuickStatCollectors();

        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);

        Assert.IsType<TableValuedPersonIdListBinder>(provider.GetRequiredService<IPersonIdListBinder>());
    }

    /// <summary>An executor that fails loudly: the DI tests resolve services, they never run them.</summary>
    private sealed class UnusableSqlExecutor : ISqlExecutor
    {
        public Task<SqlResultSet> QueryAsync(SqlRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> ExecuteAsync(SqlRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<T?> ScalarAsync<T>(SqlRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
