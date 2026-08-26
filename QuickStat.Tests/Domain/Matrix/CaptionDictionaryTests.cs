using QuickStat.Domain.Matrix;
using Xunit;

namespace QuickStat.Tests.Domain.Matrix;

/// <summary>
/// Column headings. The precedence rule is asymmetric on purpose: hardcoded captions overwrite,
/// database captions do not.
/// </summary>
public class CaptionDictionaryTests
{
    [Fact]
    public void AnUnknownVariableIsTitledByItsOwnName()
    {
        // EPR.QA.CaptionDictionary.pas:176-184.  This fallback is why the grid shows raw names such
        // as NDV_INS… beside friendly lab names.
        CaptionDictionary captions = new();

        Assert.Equal("NDV_INSULIN", captions.GetVarTitle("NDV_INSULIN"));
        Assert.Equal("", captions.GetVarDescription("NDV_INSULIN"));
    }

    [Fact]
    public void AddCaptionOverwrites()
    {
        CaptionDictionary captions = new();

        captions.AddCaption(new CaptionRecord { VarName = "X", Title = "First" });
        captions.AddCaption(new CaptionRecord { VarName = "X", Title = "Second" });

        Assert.Equal("Second", captions.GetVarTitle("X"));
        Assert.Equal(1, captions.Count);
    }

    [Fact]
    public void TheDatabaseLoadPathIsFirstWins()
    {
        // The hardcoded captions are installed before the query runs, so they must survive it.
        CaptionDictionary captions = CaptionDictionary.WithQuickStatDefaults();

        int added = captions.AddRange(
        [
            new CaptionRecord { VarName = "DRUG.METFORMIN", Title = "From the database" },
            new CaptionRecord { VarName = "NPU01566", Title = "P-Kolesterol" },
        ]);

        Assert.Equal(1, added);
        Assert.Equal("Metform", captions.GetVarTitle("DRUG.METFORMIN"));
        Assert.Equal("P-Kolesterol", captions.GetVarTitle("NPU01566"));
    }

    [Fact]
    public void LookupIsOrdinal()
    {
        CaptionDictionary captions = new();

        captions.AddCaption(new CaptionRecord { VarName = "DbVersion", Title = "Server" });

        Assert.Equal("Server", captions.GetVarTitle("DbVersion"));
        Assert.Equal("DBVERSION", captions.GetVarTitle("DBVERSION"));
    }

    [Fact]
    public void TheTwelveHardcodedCaptionsAreCarriedAcross()
    {
        // MainQuickStat.pas:453-469, verbatim from the canonical source.
        CaptionDictionary captions = CaptionDictionary.WithQuickStatDefaults();

        Assert.Equal(12, CaptionDictionary.QuickStatDefaults.Count);
        Assert.Equal(12, captions.Count);

        Assert.Equal("DDI-R", captions.GetVarTitle("DRUID.RED"));
        Assert.Equal("Drug-Drug interactions, red level", captions.GetVarDescription("DRUID.RED"));
        Assert.Equal("DDI-Y", captions.GetVarTitle("DRUID.YELLOW"));
        Assert.Equal("DDI-O", captions.GetVarTitle("DRUID.ORANGE"));
        Assert.Equal("DDI-G", captions.GetVarTitle("DRUID.GREEN"));
        Assert.Equal("Regular", captions.GetVarTitle("DRUG.F"));
        Assert.Equal("AsNeeded", captions.GetVarTitle("DRUG.B"));
        Assert.Equal("Weekly", captions.GetVarTitle("DRUG.U"));
        Assert.Equal("Unspec", captions.GetVarTitle("DRUG.X"));
        Assert.Equal("Cure", captions.GetVarTitle("DRUG.K"));
        Assert.Equal("NoAtc", captions.GetVarTitle("DRUG.NOATC"));
        Assert.Equal("Resist", captions.GetVarTitle("DRUG.RESISTANCE_DRIVING"));
        Assert.Equal("Resistance-driving antibiotics", captions.GetVarDescription("DRUG.RESISTANCE_DRIVING"));
        Assert.Equal("Metform", captions.GetVarTitle("DRUG.METFORMIN"));
        Assert.Equal("", captions.GetVarDescription("DRUG.F"));
    }

    [Fact]
    public void ACaptionMustCarryBothANameAndATitle()
    {
        CaptionDictionary captions = new();

        Assert.Throws<ArgumentException>(() => captions.AddCaption(new CaptionRecord { VarName = "", Title = "X" }));
        Assert.Throws<ArgumentException>(() => captions.AddCaption(new CaptionRecord { VarName = "X", Title = "" }));
    }

    [Fact]
    public void ClearEmptiesTheDictionary()
    {
        CaptionDictionary captions = CaptionDictionary.WithQuickStatDefaults();

        captions.Clear();

        Assert.Equal(0, captions.Count);
        Assert.Equal("DRUG.F", captions.GetVarTitle("DRUG.F"));
    }
}
