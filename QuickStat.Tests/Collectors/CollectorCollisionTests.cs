using QuickStat.Collectors;
using QuickStat.Collectors.Registry;
using Xunit;

namespace QuickStat.Tests.Collectors;

/// <summary>
/// The six name and title collisions PORT-PLAN.md §7.2 says to fix.
/// </summary>
/// <remarks>
/// Four of the six concern registrations this port makes; two concern collectors and constants that
/// are dropped outright, and are pinned here as absences so that nobody re-introduces them.
/// </remarks>
public class CollectorCollisionTests
{
    [Fact]
    public void Collision1_LowTrustLabIsRegisteredUnderItsOwnName()
    {
        // Upstream: TLabLowTrustCollector.Create( QST_LAB_MEDIUM, StrTitleLabsetLow, … ), so the
        // low-trust collector's name is LAB.TRUST2 and the medium-trust one shadows it.
        IReadOnlyList<ICollector> all = CollectorCatalog.All;

        ICollector low = CollectorTestContext.ByName(all, "LAB.TRUST1");
        ICollector medium = CollectorTestContext.ByName(all, "LAB.TRUST2");

        Assert.Equal("Labdata: Alle med lav konfidens", low.Descriptor.Title);
        Assert.Equal("Labdata: Alle med middels konfidens", medium.Descriptor.Title);

        // The trust level is what distinguishes the two statements; make sure the names did not get
        // swapped along with the fix.
        Assert.Contains("la.TrustLevel = 1", low.BuildSql(CollectorTestContext.SqlContext), System.StringComparison.Ordinal);
        Assert.Contains("la.TrustLevel = 2", medium.BuildSql(CollectorTestContext.SqlContext), System.StringComparison.Ordinal);
    }

    [Fact]
    public void Collision2_SystolicTwoMonthCollectorIsRegisteredUnderItsOwnName()
    {
        // Upstream: TCustomDataCollector.Create( QS_GBD_WEIGHT_2M, StrTitleGbdSbp2m, … ).
        IReadOnlyList<ICollector> all = CollectorCatalog.All;

        ICollector systolic = CollectorTestContext.ByName(all, "GBD.SBP_2M");
        ICollector weight = CollectorTestContext.ByName(all, "GBD.WEIGHT_2M");

        Assert.Equal("GBD: Blodtrykk fra siste 2 mnd", systolic.Descriptor.Title);
        Assert.Equal("GBD: Vekt fra siste 2 mnd", weight.Descriptor.Title);

        // Item 3556 is systolic blood pressure, 3224 is weight.
        Assert.Contains("cdp.ItemId = 3556", systolic.BuildSql(CollectorTestContext.SqlContext), System.StringComparison.Ordinal);
        Assert.Contains("cdp.ItemId = 3224", weight.BuildSql(CollectorTestContext.SqlContext), System.StringComparison.Ordinal);
    }

    [Fact]
    public void Collision3And4_TheFormAgeCollectorsAreNotPortedAtAll()
    {
        // QS_FORMAGE_GBD_MAREVAN is registered upstream under QS_GBD_FORM_MAREVAN, and
        // QS_FORMAGE_GBD_FLACKERKIELY shares StrTitleFormStratify with QS_FORMAGE_GBD_STRATIFY. Both
        // belong to the 39 factory names QuickStat never registers, which PORT-PLAN.md §7.1 drops -
        // so in this port there is nothing to collide.
        List<string> names = CollectorTestContext.Names(CollectorCatalog.All);

        Assert.DoesNotContain(names, name => name.StartsWith("FORMAGE.", System.StringComparison.Ordinal));
        Assert.DoesNotContain("FORM.GBD_MAREVAN", names);

        IReadOnlyList<string> titles = [.. CollectorCatalog.All.Select(collector => collector.Descriptor.Title)];

        Assert.DoesNotContain("Stratify (siste)", titles);
        Assert.DoesNotContain("Marevanskjema (siste)", titles);
    }

    [Fact]
    public void Collision5_TheDeadDiabetesLabNameIsNotTranscribed()
    {
        // QST_LAB_DIABETESw is a dead duplicate of QST_LAB_DIABETES - same value, LAB.DIABETES -
        // and only one collector may carry that name.
        Assert.Single(CollectorCatalog.All, collector => collector.Descriptor.Name == "LAB.DIABETES");
    }

    [Fact]
    public void Collision6_TheUnusedDiabetesLabTitleIsNotTranscribed()
    {
        // StrTitleLabsetDiabetes = 'Labdata: Viktigste ved diabetes' is declared in
        // QuickStat.Collectors.pas and never used. It must not appear anywhere.
        Assert.DoesNotContain(
            CollectorCatalog.All,
            collector => collector.Descriptor.Title.Contains("Viktigste ved diabetes", System.StringComparison.Ordinal));
    }
}
