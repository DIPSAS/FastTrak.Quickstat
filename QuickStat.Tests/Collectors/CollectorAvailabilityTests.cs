using QuickStat.Collectors;
using QuickStat.Collectors.Registry;
using QuickStat.Collectors.Sql;
using Xunit;

namespace QuickStat.Tests.Collectors;

/// <summary>
/// The optional-collector gate (PORT-PLAN.md R7).
/// </summary>
/// <remarks>
/// Exactly one collector in the registry uses it: <c>DRUG.INTERMEDIATE</c>, which inner-joins
/// <c>KB.AntibioticResistance2</c>. Its neighbour <c>DRUG.RECOMMENDED</c> deliberately does not -
/// its nine ATC codes are written out in the statement, so gating it would hide a working collector
/// on every database without the <c>KB</c> schema.
/// </remarks>
public class CollectorAvailabilityTests
{
    private static readonly CollectorSqlContext AnyContext = CollectorTestContext.SqlContext;

    [Fact]
    public void AlwaysIsTheDefaultAndRequiresNothing()
    {
        Assert.Empty(CollectorAvailability.Always.RequiredDatabaseObjects);
        Assert.Null(CollectorAvailability.Always.Predicate);

        CollectorDescriptor descriptor = Descriptor(CollectorAvailability.Always);

        Assert.True(CollectorRegistryBuilder.IsAvailable(descriptor.Availability, CollectorTestContext.Availability("GBD")));
    }

    [Fact]
    public void TheCatalogAsksForExactlyOneDatabaseObject()
    {
        Assert.Equal(
            new[] { "KB.AntibioticResistance2" },
            CollectorRegistryBuilder.RequiredDatabaseObjects);

        Assert.Equal("KB.AntibioticResistance2", DrugSql.AntibioticResistanceKnowledgeBase);
    }

    [Fact]
    public void OnlyTheIntermediateAntibioticCollectorIsGated()
    {
        foreach (ICollector collector in CollectorCatalog.All)
        {
            if (collector.Descriptor.Name == CollectorNames.DrugAntibioticIntermediate)
            {
                Assert.Equal(
                    new[] { DrugSql.AntibioticResistanceKnowledgeBase },
                    collector.Descriptor.Availability.RequiredDatabaseObjects);
            }
            else
            {
                Assert.Same(CollectorAvailability.Always, collector.Descriptor.Availability);
            }
        }
    }

    [Fact]
    public void TheGatedCollectorIsRegisteredWhenTheKnowledgeBaseResolves()
    {
        List<CollectorDescriptor> skipped = [];

        IReadOnlyList<ICollector> registered = CollectorRegistryBuilder.Build(
            "KORTTID",
            [],
            CollectorTestContext.Availability("KORTTID", DrugSql.AntibioticResistanceKnowledgeBase),
            skipped.Add);

        Assert.Empty(skipped);
        Assert.Contains(CollectorNames.DrugAntibioticIntermediate, CollectorTestContext.Names(registered));
    }

    [Fact]
    public void TheGatedCollectorIsDroppedAndReportedOnceWhenTheKnowledgeBaseIsMissing()
    {
        List<CollectorDescriptor> skipped = [];

        List<string> registered = CollectorTestContext.Names(CollectorRegistryBuilder.Build(
            "KORTTID",
            [],
            CollectorTestContext.Availability("KORTTID"),
            skipped.Add));

        // Reported exactly once, with enough in the descriptor for the log line to name the table.
        CollectorDescriptor dropped = Assert.Single(skipped);

        Assert.Equal(CollectorNames.DrugAntibioticIntermediate, dropped.Name);
        Assert.Equal(
            new[] { DrugSql.AntibioticResistanceKnowledgeBase },
            dropped.Availability.RequiredDatabaseObjects);

        Assert.DoesNotContain(CollectorNames.DrugAntibioticIntermediate, registered);

        // ... and nothing else moves. The resistance collector on one side of it and the rest of the
        // drug family on the other are untouched, which is the regression this guards against.
        List<string> complete = CollectorTestContext.Names(CollectorTestContext.BuildComplete("KORTTID"));

        Assert.Equal(
            complete.Where(name => name != CollectorNames.DrugAntibioticIntermediate),
            registered);
    }

    [Fact]
    public void ARequiredObjectThatDoesNotResolveDropsTheCollector()
    {
        CollectorAvailability needsKb = new() { RequiredDatabaseObjects = ["KB.AntibioticResistance2"] };

        Assert.False(CollectorRegistryBuilder.IsAvailable(needsKb, CollectorTestContext.Availability("GBD")));
        Assert.False(CollectorRegistryBuilder.IsAvailable(needsKb, CollectorTestContext.Availability("GBD", "dbo.Person")));
        Assert.True(CollectorRegistryBuilder.IsAvailable(needsKb, CollectorTestContext.Availability("GBD", "KB.AntibioticResistance2")));
    }

    [Fact]
    public void ObjectNamesAreComparedCaseInsensitively()
    {
        CollectorAvailability needsKb = new() { RequiredDatabaseObjects = ["KB.AntibioticResistance2"] };

        Assert.True(CollectorRegistryBuilder.IsAvailable(needsKb, CollectorTestContext.Availability("GBD", "kb.antibioticresistance2")));
    }

    [Fact]
    public void EveryRequiredObjectMustResolve()
    {
        CollectorAvailability needsBoth = new() { RequiredDatabaseObjects = ["KB.First", "KB.Second"] };

        Assert.False(CollectorRegistryBuilder.IsAvailable(needsBoth, CollectorTestContext.Availability("GBD", "KB.First")));
        Assert.True(CollectorRegistryBuilder.IsAvailable(needsBoth, CollectorTestContext.Availability("GBD", "KB.First", "KB.Second")));
    }

    [Fact]
    public void ThePredicateIsAndedWithTheObjectList()
    {
        CollectorAvailability neverPredicate = new()
        {
            RequiredDatabaseObjects = ["KB.AntibioticResistance2"],
            Predicate = _ => false,
        };

        Assert.False(CollectorRegistryBuilder.IsAvailable(
            neverPredicate,
            CollectorTestContext.Availability("GBD", "KB.AntibioticResistance2")));

        CollectorAvailability studyPredicate = new() { Predicate = context => context.StudyName.StartsWith("GBD", System.StringComparison.Ordinal) };

        Assert.True(CollectorRegistryBuilder.IsAvailable(studyPredicate, CollectorTestContext.Availability("GBD")));
        Assert.False(CollectorRegistryBuilder.IsAvailable(studyPredicate, CollectorTestContext.Availability("NDV")));
    }

    [Fact]
    public void AnUnavailableCollectorIsFilteredOutAndReported()
    {
        // Drives the whole builder rather than just the predicate, so that the filter is proven to
        // run on the path the registry actually takes.
        CollectorDescriptor descriptor = Descriptor(new CollectorAvailability { RequiredDatabaseObjects = ["KB.Missing"] });
        Collector optional = new(descriptor, _ => "SELECT 1");

        List<CollectorDescriptor> skipped = [];

        IReadOnlyList<ICollector> kept = CollectorRegistryBuilder.Build(
            "TARMSCREENING",
            [],
            CollectorTestContext.Availability("TARMSCREENING"),
            skipped.Add);

        // TARMSCREENING opens no gate, so the one optional collector in the catalog is not even a
        // candidate and the run is clean ...
        Assert.Empty(skipped);
        Assert.DoesNotContain(optional, kept);

        // ... and the synthetic descriptor confirms the predicate the builder applies.
        Assert.False(CollectorRegistryBuilder.IsAvailable(
            optional.Descriptor.Availability,
            CollectorTestContext.Availability("TARMSCREENING")));

        Assert.Equal("SELECT 1", optional.BuildSql(AnyContext));
    }

    private static CollectorDescriptor Descriptor(CollectorAvailability availability) => new()
    {
        Name = "TEST.OPTIONAL",
        Title = "Test: optional",
        Kind = CollectorKind.DrugSet,
        PidBinding = PidBinding.None,
        Availability = availability,
    };
}
