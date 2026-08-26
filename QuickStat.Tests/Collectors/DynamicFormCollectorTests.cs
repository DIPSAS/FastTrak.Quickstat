using QuickStat.Collectors;
using QuickStat.Collectors.Registry;
using Xunit;

namespace QuickStat.Tests.Collectors;

/// <summary>
/// <c>AddCollectorsStudySpecific</c> - the <c>2 x N</c> dynamic per-form collectors.
/// </summary>
public class DynamicFormCollectorTests
{
    private static readonly FormClass Barthel = new("BARTHEL", "Barthel ADL-indeks");
    private static readonly FormClass Lmg = new("LMG", "Legemiddelgjennomgang");

    [Fact]
    public void EveryFormClassProducesExactlyTwoCollectors()
    {
        IReadOnlyList<ICollector> collectors = CollectorRegistryBuilder.CreateFormCollectors([Barthel, Lmg]);

        Assert.Equal(4, collectors.Count);
        Assert.Equal(
            new[] { "BARTHEL", "FORM.BARTHEL", "LMG", "FORM.LMG" },
            CollectorTestContext.Names(collectors));
    }

    [Fact]
    public void TheFormAgeCollectorKeepsTheBareFormNameAsItsName()
    {
        // TFormAgeCollector.Create passes ACollectorName straight through; only TFormDataCollector
        // prepends PREFIX_FORM.
        ICollector formAge = CollectorRegistryBuilder.CreateFormCollectors([Barthel])[0];

        Assert.Equal("BARTHEL", formAge.Descriptor.Name);
        Assert.Equal("Skjema-alder: Barthel ADL-indeks (BARTHEL) (siste)", formAge.Descriptor.Title);
        Assert.Equal("FORMAGE.", formAge.Descriptor.VarPrefix);
        Assert.Equal(CollectorKind.FormAge, formAge.Descriptor.Kind);
        Assert.Equal(PidBinding.IdList, formAge.Descriptor.PidBinding);
        Assert.Equal(100, formAge.Descriptor.BatchSize);
    }

    [Fact]
    public void TheFormDataCollectorUsesTheFormNameAsItsVariablePrefix()
    {
        ICollector formData = CollectorRegistryBuilder.CreateFormCollectors([Barthel])[1];

        Assert.Equal("FORM.BARTHEL", formData.Descriptor.Name);

        // No suffix: TFormDataCollector appends nothing.
        Assert.Equal("Skjema-data: Barthel ADL-indeks (BARTHEL)", formData.Descriptor.Title);
        Assert.Equal("BARTHEL.", formData.Descriptor.VarPrefix);
        Assert.Equal(CollectorKind.FormData, formData.Descriptor.Kind);
        Assert.Equal(PidBinding.IdList, formData.Descriptor.PidBinding);

        // PORT-PLAN.md §8.5: SpSnapshotFormDataAll with batch 200, which is what ships.
        Assert.Equal(200, formData.Descriptor.BatchSize);
    }

    [Theory]
    // "Anonymous forms": TRegEx.IsMatch( formName, 'FORM\d+' ), unanchored and case-sensitive.
    [InlineData("FORM1")]
    [InlineData("FORM42")]
    [InlineData("MYFORM12")]
    [InlineData("FORM007X")]
    public void AnonymousFormsAreSkipped(string formName) =>
        Assert.Empty(CollectorRegistryBuilder.CreateFormCollectors([new FormClass(formName, "Anonymous")]));

    [Theory]
    // The pattern needs at least one digit, and it is case-sensitive.
    [InlineData("FORM")]
    [InlineData("FORMULAR")]
    [InlineData("form12")]
    public void NamesThatOnlyLookAnonymousAreKept(string formName) =>
        Assert.Equal(2, CollectorRegistryBuilder.CreateFormCollectors([new FormClass(formName, "Kept")]).Count);

    [Fact]
    public void RepeatedFormNamesAreRegisteredOnce()
    {
        IReadOnlyList<ICollector> collectors = CollectorRegistryBuilder.CreateFormCollectors(
            [Barthel, new FormClass("BARTHEL", "A different title"), Lmg]);

        Assert.Equal(4, collectors.Count);
        Assert.Equal("Skjema-alder: Barthel ADL-indeks (BARTHEL) (siste)", collectors[0].Descriptor.Title);
    }

    [Fact]
    public void DeduplicationIsCaseSensitive()
    {
        // TDictionary<string, string> uses the ordinal, case-sensitive default comparer.
        IReadOnlyList<ICollector> collectors = CollectorRegistryBuilder.CreateFormCollectors(
            [Barthel, new FormClass("barthel", "Lower case")]);

        Assert.Equal(4, collectors.Count);
    }

    [Fact]
    public void TheFormNameIsQuotedIntoBothStatements()
    {
        IReadOnlyList<ICollector> collectors = CollectorRegistryBuilder.CreateFormCollectors([Barthel]);

        Assert.Contains("mf.FormName =  'BARTHEL'", collectors[0].BuildSql(CollectorTestContext.SqlContext), System.StringComparison.Ordinal);
        Assert.Contains("mf.FormName = 'BARTHEL'", collectors[1].BuildSql(CollectorTestContext.SqlContext), System.StringComparison.Ordinal);
    }

    [Fact]
    public void AFormNameWithAnApostropheIsEscaped()
    {
        // Form names come from the database and now round-trip through a UI-visible string, so the
        // escaping must not be assumed away (Docs/Port/03-collectors.md §C.3).
        IReadOnlyList<ICollector> collectors = CollectorRegistryBuilder.CreateFormCollectors(
            [new FormClass("O'BRIEN", "Test")]);

        Assert.Contains("'O''BRIEN'", collectors[0].BuildSql(CollectorTestContext.SqlContext), System.StringComparison.Ordinal);
        Assert.Contains("'O''BRIEN'", collectors[1].BuildSql(CollectorTestContext.SqlContext), System.StringComparison.Ordinal);
    }

    [Fact]
    public void FormCollectorsAreRegisteredBetweenTheLabSetAndSize()
    {
        // The dynamic collectors go in between the two always-on blocks: after AddCollectorsLabData
        // and before AddCollectorsHardCoded's 'SIZE'.
        List<string> names = CollectorTestContext.Names(CollectorTestContext.Build("NDV", Barthel));

        int lastLab = names.IndexOf(CollectorNames.LabCount60M);
        int formAge = names.IndexOf("BARTHEL");
        int formData = names.IndexOf("FORM.BARTHEL");
        int size = names.IndexOf(CollectorNames.Size);

        Assert.True(lastLab < formAge, "Form collectors must follow AddCollectorsLabData.");
        Assert.Equal(formAge + 1, formData);
        Assert.Equal(formData + 1, size);
    }

    [Fact]
    public void FormCollectorsAddTwoPerFormOnTopOfTheStaticCount()
    {
        // Docs/Port/03-collectors.md §D.2: "plus 2 x N dynamic form collectors in every case".
        // The static half depends on the probe outcome; the dynamic half never does.
        Assert.Equal(
            CollectorTestContext.AlwaysCount + 4,
            CollectorTestContext.BuildComplete("TARMSCREENING", Barthel, Lmg).Count);

        Assert.Equal(
            CollectorTestContext.FullyGatedCount + 4,
            CollectorTestContext.BuildComplete("KORTTID", Barthel, Lmg).Count);

        Assert.Equal(
            CollectorTestContext.FullyGatedWithoutOptionalObjects + 4,
            CollectorTestContext.Build("KORTTID", Barthel, Lmg).Count);
    }
}
