using QuickStat.Collectors;
using QuickStat.Collectors.Registry;
using QuickStat.Collectors.Sql;
using Xunit;

namespace QuickStat.Tests.Collectors;

/// <summary>
/// The registry counts. These are acceptance criteria (PORT-PLAN.md §10.3, §10.4), not
/// nice-to-haves.
/// </summary>
/// <remarks>
/// <para>
/// The target is 131 distinct names and 124 for a <c>KORTTID</c> study, counted against the
/// canonical application. Phase 4 is restoring the five registrations this repository has commented
/// out; the constants in <see cref="CollectorTestContext"/> track how far it has got.
/// </para>
/// <para>
/// A <c>KORTTID</c> count is only meaningful together with a probe outcome, because
/// <c>DRUG.INTERMEDIATE</c> is registered only when <c>KB.AntibioticResistance2</c> resolves. Both
/// outcomes are asserted, and their difference is pinned to the optional collectors themselves
/// rather than to a literal.
/// </para>
/// </remarks>
public class CollectorRegistryCountTests
{
    /// <inheritdoc cref="CollectorTestContext.DistinctNameCount"/>
    private const int DistinctNameCount = CollectorTestContext.DistinctNameCount;

    /// <inheritdoc cref="CollectorTestContext.FullyGatedCount"/>
    private const int FullyGatedCount = CollectorTestContext.FullyGatedCount;

    /// <inheritdoc cref="CollectorTestContext.FullyGatedWithoutOptionalObjects"/>
    private const int FullyGatedWithoutOptionalObjects = CollectorTestContext.FullyGatedWithoutOptionalObjects;

    /// <inheritdoc cref="CollectorTestContext.AlwaysCount"/>
    private const int AlwaysCount = CollectorTestContext.AlwaysCount;

    [Fact]
    public void CatalogHasTheExpectedNumberOfCollectors() => Assert.Equal(DistinctNameCount, CollectorCatalog.All.Count);

    [Fact]
    public void TheOnlyOptionalCollectorIsTheOneThatJoinsTheKnowledgeBase()
    {
        // This is what makes every KORTTID count above derivable rather than guessed: the gap
        // between the two probe outcomes is exactly this list. PORT-PLAN.md R7.
        Assert.Equal(
            new[] { CollectorNames.DrugAntibioticIntermediate },
            CollectorCatalog.All
                .Where(collector => collector.Descriptor.Availability != CollectorAvailability.Always)
                .Select(collector => collector.Descriptor.Name));

        Assert.Equal(
            new[] { DrugSql.AntibioticResistanceKnowledgeBase },
            CollectorRegistryBuilder.RequiredDatabaseObjects);
    }

    [Fact]
    public void TheOptionalCollectorIsTheOnlyDifferenceBetweenTheTwoProbeOutcomes()
    {
        List<string> complete = CollectorTestContext.Names(CollectorTestContext.BuildComplete("KORTTID"));
        List<string> degraded = CollectorTestContext.Names(CollectorTestContext.Build("KORTTID"));

        Assert.Equal(FullyGatedCount, complete.Count);
        Assert.Equal(FullyGatedWithoutOptionalObjects, degraded.Count);
        Assert.Equal(new[] { CollectorNames.DrugAntibioticIntermediate }, complete.Except(degraded));
    }

    [Fact]
    public void CatalogNamesAreDistinct()
    {
        // TryFind takes the first match, so a duplicate name makes a collector unreachable and
        // breaks any saved package that refers to it. Two of the six §7.2 collisions were exactly
        // this.
        List<string> duplicates =
        [
            .. CollectorCatalog.All
                .GroupBy(collector => collector.Descriptor.Name, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key),
        ];

        Assert.Empty(duplicates);
    }

    [Fact]
    public void CatalogTitlesAreDistinct()
    {
        // TryFind matches the title as well as the name, so titles have to be unique too.
        List<string> duplicates =
        [
            .. CollectorCatalog.All
                .GroupBy(collector => collector.Descriptor.Title, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key),
        ];

        Assert.Empty(duplicates);
    }

    [Fact]
    public void FamilySizesMatchTheDelphiBlocks()
    {
        // 36 always-on = 1 form-frequency + 15 basic + 19 lab data + 1 'SIZE'.
        Assert.Equal(35, CollectorCatalog.AlwaysBeforeFormCollectors.Count);
        Assert.Single(CollectorCatalog.AlwaysAfterFormCollectors);

        // 79 = 24 GBD var-sets + 17 diagnoses + 38 drugs.
        Assert.Equal(79, CollectorCatalog.GbdFamily.Count);
        Assert.Equal(8, CollectorCatalog.NdvFamily.Count);
        Assert.Equal(3, CollectorCatalog.GwasFamily.Count);
        Assert.Equal(3, CollectorCatalog.RoasFamily.Count);
        Assert.Single(CollectorCatalog.DogfoodFamily);
    }

    [Theory]
    // Docs/Port/03-collectors.md §D.2 and PORT-PLAN.md §10.4, on a fully provisioned database.
    [InlineData("GBD", FullyGatedCount)]
    [InlineData("LANGTID", FullyGatedCount)]
    [InlineData("KORTTID", FullyGatedCount)]
    [InlineData("NDV", AlwaysCount + 8)]
    [InlineData("ENDO", AlwaysCount + 8)]
    [InlineData("GWAS", AlwaysCount + 3)]
    [InlineData("ROAS", AlwaysCount + 3)]
    [InlineData("ROAS_GWAS", AlwaysCount + 3 + 3)]
    [InlineData("DOGFOOD", AlwaysCount + 1)]
    [InlineData("dogfood", AlwaysCount + 1)]
    [InlineData("TARMSCREENING", AlwaysCount)]
    [InlineData("korttid", AlwaysCount)]
    public void StudyRegistersTheExpectedNumberOfCollectors(string studyName, int expected) =>
        Assert.Equal(expected, CollectorTestContext.BuildComplete(studyName).Count);

    [Theory]
    // The same studies on a database with no KB schema. Only the three gates that admit the drug
    // family move, and each moves by exactly one.
    [InlineData("GBD", FullyGatedWithoutOptionalObjects)]
    [InlineData("LANGTID", FullyGatedWithoutOptionalObjects)]
    [InlineData("KORTTID", FullyGatedWithoutOptionalObjects)]
    [InlineData("NDV", AlwaysCount + 8)]
    [InlineData("ROAS", AlwaysCount + 3)]
    [InlineData("TARMSCREENING", AlwaysCount)]
    public void StudyRegistersOneFewerWhenTheKnowledgeBaseIsMissing(string studyName, int expected) =>
        Assert.Equal(expected, CollectorTestContext.Build(studyName).Count);

    [Fact]
    public void KorttidRegistersExactlyTheSameCollectorsAsGbdAndLangtid()
    {
        // PORT-PLAN.md §10.4, the single easiest thing to lose in transcription: KORTTID must be in
        // BOTH regex literals. Comparing the ordered name lists catches "present in one of them",
        // which a bare count would not - a KORTTID study missing only the NDV block would still
        // differ from GBD by 8, but a study missing only the GBD block by 79, and either mistake is
        // silent at runtime.
        IReadOnlyList<string> gbd = CollectorTestContext.Names(CollectorTestContext.BuildComplete("GBD"));
        IReadOnlyList<string> langtid = CollectorTestContext.Names(CollectorTestContext.BuildComplete("LANGTID"));
        IReadOnlyList<string> korttid = CollectorTestContext.Names(CollectorTestContext.BuildComplete("KORTTID"));

        Assert.Equal(gbd, langtid);
        Assert.Equal(gbd, korttid);
        Assert.Equal(FullyGatedCount, korttid.Count);
    }

    [Fact]
    public void KorttidGetsBothTheGbdAndTheNdvBlock()
    {
        IReadOnlyList<ICollector> korttid = CollectorTestContext.BuildComplete("KORTTID");
        HashSet<string> names = [.. CollectorTestContext.Names(korttid)];

        // One representative of each of the three families the GBD gate admits ...
        Assert.Contains(CollectorNames.GbdScores, names);
        Assert.Contains(CollectorNames.DiagnoseAll1, names);
        Assert.Contains(CollectorNames.DrugAntibioticResistance, names);

        // ... and of the NDV gate.
        Assert.Contains(CollectorNames.NdvDiagnose, names);
        Assert.Contains(CollectorNames.LabDiabetes, names);
    }

    [Fact]
    public void EveryGatedFamilyIsExcludedFromAnUngatedStudy()
    {
        HashSet<string> ungated = [.. CollectorTestContext.Names(CollectorTestContext.Build("TARMSCREENING"))];

        Assert.DoesNotContain(CollectorNames.GbdScores, ungated);
        Assert.DoesNotContain(CollectorNames.DiagnoseAll1, ungated);
        Assert.DoesNotContain("DRUG.A10", ungated);
        Assert.DoesNotContain(CollectorNames.NdvDiagnose, ungated);
        Assert.DoesNotContain(CollectorNames.RoasGwasBackground, ungated);
        Assert.DoesNotContain(CollectorNames.RoasPoiOrdinal, ungated);
        Assert.DoesNotContain(CollectorNames.DogfoodDatabaseVersion, ungated);
    }

    [Fact]
    public void RegistrationOrderFollowsTheDelphiProcedures()
    {
        List<string> names = CollectorTestContext.Names(CollectorTestContext.BuildComplete("GBD_GWAS_ROAS_DOGFOOD"));

        // PrepareStudy registers the form-frequency collector before calling anything else.
        Assert.Equal(CollectorNames.FormFrequency, names[0]);

        // Then AddCollectorsBasic, then AddCollectorsLabData, then - with no form classes here -
        // AddCollectorsHardCoded's ungated 'SIZE'.
        Assert.Equal(CollectorNames.PatientAge, names[1]);
        Assert.Equal(CollectorNames.LabKidney, names[16]);
        Assert.Equal(CollectorNames.Size, names[35]);

        // Then the gated blocks, in source order: GBD (with diagnoses and drugs inside it), NDV,
        // GWAS, ROAS, DOGFOOD.
        AssertOrder(names, CollectorNames.GbdWeightDays, CollectorNames.DiagnoseAll1);
        AssertOrder(names, CollectorNames.DiagnoseDementia, "DRUG.A10");
        AssertOrder(names, CollectorNames.DrugNorGeP, CollectorNames.NdvDiagnose);
        AssertOrder(names, CollectorNames.LabDiabetes, CollectorNames.RoasGwasBackground);
        AssertOrder(names, CollectorNames.RoasGwasAps1, CollectorNames.RoasPoiOrdinal);
        AssertOrder(names, CollectorNames.RoasBase, CollectorNames.DogfoodDatabaseVersion);

        Assert.Equal(CollectorNames.DogfoodDatabaseVersion, names[^1]);
    }

    [Fact]
    public void RoasBaseIsRegisteredLastInItsBlock()
    {
        // QuickStat.Collectors.pas:478-480: after both POI collectors, and the last thing the ROAS
        // block adds.
        List<string> names = CollectorTestContext.Names(CollectorCatalog.RoasFamily);

        Assert.Equal(
            new[] { CollectorNames.RoasPoiOrdinal, CollectorNames.RoasPoiQuantity, CollectorNames.RoasBase },
            names);
    }

    [Fact]
    public void RoasBaseIsBehindTheRoasGateAndSoDoesNotMoveTheKorttidCount()
    {
        // The one restored collector that a KORTTID study never sees, which is why the acceptance
        // target moves by four and not by five (PORT-PLAN.md §10.4).
        Assert.DoesNotContain(
            CollectorNames.RoasBase,
            CollectorTestContext.Names(CollectorTestContext.BuildComplete("KORTTID")));

        Assert.Contains(
            CollectorNames.RoasBase,
            CollectorTestContext.Names(CollectorTestContext.BuildComplete("ROAS")));
    }

    [Fact]
    public void TheAntibioticCollectorsSitWhereTheDelphiPutsThem()
    {
        // Position is part of the contract: it is the column order of every export. The restored
        // registrations go where the commented-out lines sit, immediately after the resistance
        // collector and before NorGeP (QuickStat.Collectors.pas:379-384).
        List<string> names = CollectorTestContext.Names(CollectorTestContext.BuildComplete("KORTTID"));

        int resistance = names.IndexOf(CollectorNames.DrugAntibioticResistance);

        Assert.True(resistance >= 0, "The resistance collector is not registered.");
        Assert.Equal(names.IndexOf(CollectorNames.DrugAnticholinergicAb) + 1, resistance);
        Assert.Equal(resistance + 1, names.IndexOf(CollectorNames.DrugAntibioticIntermediate));
        Assert.Equal(resistance + 2, names.IndexOf(CollectorNames.DrugAntibioticRecommended));
        Assert.Equal(resistance + 3, names.IndexOf(CollectorNames.DrugJ01Xx05));
        Assert.Equal(resistance + 4, names.IndexOf(CollectorNames.DrugNorGeP));
    }

    [Fact]
    public void ThePhase4CollectorsNotYetRestoredAreStillAbsent()
    {
        // Each still needs a library-side implementation brought across from the pinned ref.
        // Recorded as a test so that adding one is a conscious act.
        HashSet<string> names = [.. CollectorTestContext.Names(CollectorCatalog.All)];

        Assert.Contains(CollectorNames.DrugAntibioticIntermediate, names);
        Assert.Contains(CollectorNames.DrugAntibioticRecommended, names);
        Assert.Contains(CollectorNames.DrugJ01Xx05, names);

        Assert.Contains(CollectorNames.RoasBase, names);
        Assert.DoesNotContain("LAB.INTERLEUKINS", names);
    }

    private static void AssertOrder(List<string> names, string first, string second)
    {
        int firstIndex = names.IndexOf(first);
        int secondIndex = names.IndexOf(second);

        Assert.True(firstIndex >= 0, $"{first} is not registered.");
        Assert.True(secondIndex >= 0, $"{second} is not registered.");
        Assert.True(firstIndex < secondIndex, $"{first} must be registered before {second}.");
    }
}
