using QuickStat.Domain.Packages;
using Xunit;

namespace QuickStat.Tests.Domain.Packages;

/// <summary>
/// <c>Report.QuickStat.DataElements</c> is a persistence format that saved specifications in
/// production databases already depend on, so its shape is pinned here.
/// </summary>
public class CollectorNameListTests
{
    [Fact]
    public void TheSeparatorIsASemicolon()
    {
        Assert.Equal(';', PackagedSelection.CollectorNameSeparator);
    }

    [Fact]
    public void ParsingSplitsOnTheSeparator()
    {
        Assert.Equal(
            ["QS_DEMO_AGE", "QS_DRUG_COUNT", "QS_LAB_HBA1C"],
            CollectorNameList.Parse("QS_DEMO_AGE;QS_LAB_HBA1C;QS_DRUG_COUNT"));
    }

    [Fact]
    public void ParsingSortsBecauseTheStoredOrderCarriesNoMeaning()
    {
        // QuickStat.Selection.pas:74 - Sorted := true. Collection order comes from the registry.
        Assert.Equal(["A", "B", "C"], CollectorNameList.Parse("C;A;B"));
    }

    [Fact]
    public void ParsingDropsDuplicatesCaseInsensitively()
    {
        // Duplicates := dupIgnore, with AnsiCompareText.
        Assert.Equal(["QS_LAB_HBA1C"], CollectorNameList.Parse("QS_LAB_HBA1C;qs_lab_hba1c"));
    }

    [Fact]
    public void ParsingDropsEmptyEntries()
    {
        Assert.Equal(["A", "B"], CollectorNameList.Parse("A;;B;   "));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ParsingNothingYieldsNothing(string? text)
    {
        Assert.Empty(CollectorNameList.Parse(text));
    }

    [Fact]
    public void ASingleNameParsesToOneEntry()
    {
        Assert.Equal(["QS_DEMO_AGE"], CollectorNameList.Parse("QS_DEMO_AGE"));
    }

    [Fact]
    public void FormattingSortsAndDeduplicates()
    {
        Assert.Equal("A;B;C", CollectorNameList.Format(["C", "A", "B", "a"]));
    }

    [Fact]
    public void FormattingNothingYieldsAnEmptyString()
    {
        Assert.Equal("", CollectorNameList.Format([]));
        Assert.Equal("", CollectorNameList.Format(null));
    }

    [Fact]
    public void TheRoundTripIsStable()
    {
        string stored = CollectorNameList.Format(["QS_LAB_HBA1C", "QS_DEMO_AGE"]);

        Assert.Equal("QS_DEMO_AGE;QS_LAB_HBA1C", stored);
        Assert.Equal(CollectorNameList.Parse(stored), CollectorNameList.Format(CollectorNameList.Parse(stored)).Split(';'));
    }
}
