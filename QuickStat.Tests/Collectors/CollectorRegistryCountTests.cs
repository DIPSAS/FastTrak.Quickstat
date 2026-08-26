using QuickStat.Collectors;
using QuickStat.Collectors.Registry;
using Xunit;

namespace QuickStat.Tests.Collectors;

/// <summary>
/// The registry counts. These are acceptance criteria (PORT-PLAN.md §10.3, §10.4), not
/// nice-to-haves.
/// </summary>
/// <remarks>
/// The numbers describe <b>this</b> repository, which is the reduced copy: 126 distinct names and
/// 120 for a <c>KORTTID</c> study. Phase 4 restores the five commented-out registrations and takes
/// them to 131 / 124.
/// </remarks>
public class CollectorRegistryCountTests
{
    /// <summary>Distinct collector names in the catalog. PORT-PLAN.md §10.3.</summary>
    private const int DistinctNameCount = 126;

    /// <summary>Static collectors a fully gated study registers. PORT-PLAN.md §10.4.</summary>
    private const int FullyGatedCount = 120;

    /// <summary>Always-on static collectors.</summary>
    private const int AlwaysCount = 36;

    [Fact]
    public void CatalogHas126Collectors() => Assert.Equal(DistinctNameCount, CollectorCatalog.All.Count);

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

        // 76 = 24 GBD var-sets + 17 diagnoses + 35 drugs.
        Assert.Equal(76, CollectorCatalog.GbdFamily.Count);
        Assert.Equal(8, CollectorCatalog.NdvFamily.Count);
        Assert.Equal(3, CollectorCatalog.GwasFamily.Count);
        Assert.Equal(2, CollectorCatalog.RoasFamily.Count);
        Assert.Single(CollectorCatalog.DogfoodFamily);
    }

    [Theory]
    // Docs/Port/03-collectors.md §D.2, counted against this repository.
    [InlineData("GBD", FullyGatedCount)]
    [InlineData("LANGTID", FullyGatedCount)]
    [InlineData("KORTTID", FullyGatedCount)]
    [InlineData("NDV", AlwaysCount + 8)]
    [InlineData("ENDO", AlwaysCount + 8)]
    [InlineData("GWAS", AlwaysCount + 3)]
    [InlineData("ROAS", AlwaysCount + 2)]
    [InlineData("ROAS_GWAS", AlwaysCount + 3 + 2)]
    [InlineData("DOGFOOD", AlwaysCount + 1)]
    [InlineData("dogfood", AlwaysCount + 1)]
    [InlineData("TARMSCREENING", AlwaysCount)]
    [InlineData("korttid", AlwaysCount)]
    public void StudyRegistersTheExpectedNumberOfCollectors(string studyName, int expected) =>
        Assert.Equal(expected, CollectorTestContext.Build(studyName).Count);

    [Fact]
    public void KorttidRegistersExactlyTheSameCollectorsAsGbdAndLangtid()
    {
        // PORT-PLAN.md §10.4, the single easiest thing to lose in transcription: KORTTID must be in
        // BOTH regex literals. Comparing the ordered name lists catches "present in one of them",
        // which a bare count would not - a KORTTID study missing only the NDV block would still
        // differ from GBD by 8, but a study missing only the GBD block by 76, and either mistake is
        // silent at runtime.
        IReadOnlyList<string> gbd = CollectorTestContext.Names(CollectorTestContext.Build("GBD"));
        IReadOnlyList<string> langtid = CollectorTestContext.Names(CollectorTestContext.Build("LANGTID"));
        IReadOnlyList<string> korttid = CollectorTestContext.Names(CollectorTestContext.Build("KORTTID"));

        Assert.Equal(gbd, langtid);
        Assert.Equal(gbd, korttid);
        Assert.Equal(FullyGatedCount, korttid.Count);
    }

    [Fact]
    public void KorttidGetsBothTheGbdAndTheNdvBlock()
    {
        IReadOnlyList<ICollector> korttid = CollectorTestContext.Build("KORTTID");
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
        List<string> names = CollectorTestContext.Names(CollectorTestContext.Build("GBD_GWAS_ROAS_DOGFOOD"));

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
        AssertOrder(names, CollectorNames.RoasPoiQuantity, CollectorNames.DogfoodDatabaseVersion);

        Assert.Equal(CollectorNames.DogfoodDatabaseVersion, names[^1]);
    }

    [Fact]
    public void TheFiveCollectorsPhase4RestoresAreAbsent()
    {
        // Deliberately not registered here: they also need library-side implementations brought
        // across from the pinned ref, which is Phase 4's job. Recorded as a test so that adding them
        // is a conscious act.
        HashSet<string> names = [.. CollectorTestContext.Names(CollectorCatalog.All)];

        Assert.DoesNotContain("DRUG.INTERMEDIATE", names);
        Assert.DoesNotContain("DRUG.RECOMMENDED", names);
        Assert.DoesNotContain("DRUG.J01XX05", names);
        Assert.DoesNotContain("ROAS.BASE", names);
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
