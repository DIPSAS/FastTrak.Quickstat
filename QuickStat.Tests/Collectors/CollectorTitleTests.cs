using QuickStat.Collectors;
using QuickStat.Collectors.Registry;
using Xunit;

namespace QuickStat.Tests.Collectors;

/// <summary>
/// The three title-suffix rules the Delphi collector classes applied in their constructors, and
/// the registrations that depend on them.
/// </summary>
public class CollectorTitleTests
{
    [Fact]
    public void SuffixConstantsCarryTheirLeadingSpace()
    {
        Assert.Equal(" (siste)", CollectorTitle.LastSuffix);
        Assert.Equal(" (høyeste)", CollectorTitle.MaxSuffix);
        Assert.Equal("Labdata: {0} (siste)", CollectorTitle.LabSetTemplate);
    }

    [Fact]
    public void LastAndMaxSuffixesAreAppendedVerbatim()
    {
        Assert.Equal("GBD: Viktigste scores (siste)", CollectorTitle.WithLastSuffix("GBD: Viktigste scores"));
        Assert.Equal("GWAS Autoantistoffer (høyeste)", CollectorTitle.WithMaxSuffix("GWAS Autoantistoffer"));
    }

    [Theory]
    // No colon: the group name is wrapped.
    [InlineData("Anemi", "Labdata: Anemi (siste)")]
    [InlineData("Nyrefunksjon", "Labdata: Nyrefunksjon (siste)")]
    [InlineData("Interleukiner", "Labdata: Interleukiner (siste)")]
    [InlineData("Hjertesviktrelaterte labdata", "Labdata: Hjertesviktrelaterte labdata (siste)")]
    // A colon anywhere opts the group name out of the wrapper, which is the whole point of the
    // rule: it is what lets QST_LAB_GERIATRIC show as a GBD row rather than a Labdata one.
    [InlineData("GBD: Sentrale labdata (siste)", "GBD: Sentrale labdata (siste)")]
    [InlineData("NDV: Labdata", "NDV: Labdata")]
    [InlineData("BDJ: HbA1c (siste)", "BDJ: HbA1c (siste)")]
    public void LabSetWrapperAppliesOnlyWhenTheGroupNameHasNoColon(string groupName, string expected) =>
        Assert.Equal(expected, CollectorTitle.ForLabSet(groupName));

    [Fact]
    public void RegisteredVarSetTitlesCarryTheLastSuffixExactlyOnce()
    {
        AssertTitle(CollectorNames.Size, "Antropometri: Høyde og vekt (siste)");
        AssertTitle(CollectorNames.GbdScores, "GBD: Viktigste scores (siste)");
        AssertTitle(CollectorNames.NdvDiagnose, "NDV: Basisdata (siste)");
        AssertTitle(CollectorNames.RoasPoiQuantity, "POI Diagnoseår (siste)");
        AssertTitle(CollectorNames.DogfoodDatabaseVersion, "Dogfood: Databaseversjoner (siste)");

        // TVarSetAgeCollector uses the same suffix.
        AssertTitle(CollectorNames.GbdWeightDays, "GBD: Tid siden siste veiing (siste)");
    }

    [Fact]
    public void RegisteredMaxTitlesCarryTheHoyesteSuffix()
    {
        AssertTitle(CollectorNames.RoasGwasAutoAntibody, "GWAS Autoantistoffer (høyeste)");
        AssertTitle(CollectorNames.RoasGwasAps1, "GWAS APS-I spesfikk (høyeste)");
    }

    [Fact]
    public void RegisteredLabSetTitlesFollowTheColonRule()
    {
        AssertTitle(CollectorNames.LabKidney, "Labdata: Nyrefunksjon (siste)");
        AssertTitle(CollectorNames.LabInr, "Labdata: INR fra labarket (siste)");
        AssertTitle(CollectorNames.LabHeartFailure, "Labdata: Hjertesviktrelaterte labdata (siste)");
        AssertTitle(CollectorNames.LabDiabetes, "Labdata: Diabetes (siste)");

        // Already has a colon, so it is left alone rather than becoming "Labdata: GBD: … (siste)".
        AssertTitle(CollectorNames.LabGeriatric, "GBD: Sentrale labdata (siste)");
    }

    [Fact]
    public void CollectorsThatAppendNothingKeepTheirTitleVerbatim()
    {
        // TCustomDataCollector and the trust-level collectors append nothing at all, including the
        // three titles that already end in "(siste)" or carry their own trailing space.
        AssertTitle(CollectorNames.GbdLowBp, "GBD: Blodtrykk < 120 (siste)");
        AssertTitle(CollectorNames.LabHighTrust, "Labdata: Alle med høy konfidens");
        AssertTitle(CollectorNames.FormFrequency, "Skjema: Antall totalt per type");
    }

    [Fact]
    public void TheAntibioticTitlesReadAsOneFamily()
    {
        // Docs/Port/03-collectors.md §E.1 and §E.2. TCustomDataCollector appends nothing, so these
        // are the registered resourcestrings verbatim - including the missing "s" in
        // "Resistendrivende", which is upstream (PORT-PLAN.md §8.4).
        AssertTitle(CollectorNames.DrugAntibioticResistance, "Antibiotika: Resistendrivende");
        AssertTitle(CollectorNames.DrugAntibioticIntermediate, "Antibiotika: Intermediære");
        AssertTitle(CollectorNames.DrugAntibioticRecommended, "Antibiotika: Anbefalte");
        AssertTitle(CollectorNames.DrugJ01Xx05, "Antibiotika: Metenamin / Hiprex");
    }

    [Fact]
    public void UpstreamTypographicalQuirksArePreserved()
    {
        // Every one of these is what the shipped build displays. Parity beats tidiness
        // (PORT-PLAN.md §6).
        AssertTitle(CollectorNames.GbdMetforminLowGfr, "GBD: Metformin og GFR < 50 ");
        AssertTitle("DX.E1x014", "Diagnoser: E1[014] - Diabetes Mellitus ");
        AssertTitle("DX.Ex23", "Diagnoser: E[23] - Andre endokrine lidelser )");
        AssertTitle("DX.Fx123456789", "Diagnoser: F[123456789]  - Psykisk lidelser");
        AssertTitle("DX.I6x01234", "Diagnoser: I6[01234 - Hjerneslag");
        AssertTitle("DRUG.B01AF", "Medisiner: BO1AF - DOAK");
        AssertTitle(CollectorNames.PatientMonthOfBirth, "^ Fødselmåned");
    }

    [Fact]
    public void RenalTitlesSayGfrNotEgfr()
    {
        // PORT-PLAN.md §8.5: "eGFR" is mainline-only wording that never shipped. 9 of 9 candidate
        // baselines say "GFR".
        AssertTitle(CollectorNames.GbdAceLowGfr, "GBD: ACE/A2 og GFR < 35");
        AssertTitle(CollectorNames.GbdMetforminLowGfr, "GBD: Metformin og GFR < 50 ");
    }

    private static void AssertTitle(string collectorName, string expectedTitle)
    {
        IReadOnlyList<ICollector> all = CollectorCatalog.All;

        Assert.Equal(expectedTitle, CollectorTestContext.ByName(all, collectorName).Descriptor.Title);
    }
}
