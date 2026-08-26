using QuickStat.Collectors;
using Xunit;

namespace QuickStat.Tests.Collectors;

/// <summary>
/// The study-gate evaluator. Getting one of these wrong silently halves what a study can collect
/// and nothing fails, which is why they are pinned individually
/// (<c>Docs/Port/03-collectors.md</c> §D.3).
/// </summary>
public class StudyGateTests
{
    [Theory]
    // The three studies that open both the GBD and the NDV gate. KORTTID joining them is commit
    // 5502b72 and is the newest functional change in the Delphi repository.
    [InlineData("GBD", StudyGate.Gbd | StudyGate.Ndv)]
    [InlineData("LANGTID", StudyGate.Gbd | StudyGate.Ndv)]
    [InlineData("KORTTID", StudyGate.Gbd | StudyGate.Ndv)]
    // NDV-only studies.
    [InlineData("NDV", StudyGate.Ndv)]
    [InlineData("ENDO", StudyGate.Ndv)]
    // The three small protocol gates.
    [InlineData("GWAS", StudyGate.Gwas)]
    [InlineData("ROAS", StudyGate.Roas)]
    [InlineData("DOGFOOD", StudyGate.Dogfood)]
    // Anything else opens nothing.
    [InlineData("TARMSCREENING", StudyGate.Always)]
    [InlineData("", StudyGate.Always)]
    public void OpensTheExpectedGates(string studyName, StudyGate expected) =>
        Assert.Equal(expected, StudyGates.For(studyName));

    [Fact]
    public void NullStudyNameOpensNothing() => Assert.Equal(StudyGate.Always, StudyGates.For(null));

    [Theory]
    // Case-sensitive: the first four gates pass no options to TRegEx.IsMatch.
    [InlineData("korttid")]
    [InlineData("Korttid")]
    [InlineData("gbd")]
    [InlineData("langtid")]
    [InlineData("ndv")]
    [InlineData("gwas")]
    [InlineData("roas")]
    public void LowerCaseStudyNamesOpenNothing(string studyName) =>
        Assert.Equal(StudyGate.Always, StudyGates.For(studyName));

    [Theory]
    // DOGFOOD is the one gate with [roIgnoreCase].
    [InlineData("dogfood")]
    [InlineData("DogFood")]
    [InlineData("DOGFOOD")]
    public void DogfoodIsTheOnlyCaseInsensitiveGate(string studyName) =>
        Assert.Equal(StudyGate.Dogfood, StudyGates.For(studyName));

    [Theory]
    // Unanchored substring matches, not equality.
    [InlineData("MYGBDTEST", StudyGate.Gbd | StudyGate.Ndv)]
    [InlineData("PRE-KORTTID-2024", StudyGate.Gbd | StudyGate.Ndv)]
    [InlineData("xxROASxx", StudyGate.Roas)]
    public void GatesMatchAnywhereInTheName(string studyName, StudyGate expected) =>
        Assert.Equal(expected, StudyGates.For(studyName));

    [Fact]
    public void GatesComposeBecauseTheBlocksAreIndependentIfs()
    {
        // The five Delphi blocks are separate ifs, not else ifs.
        Assert.Equal(
            StudyGate.Gbd | StudyGate.Ndv | StudyGate.Gwas,
            StudyGates.For("GBD_NDV_GWAS"));

        Assert.Equal(StudyGate.Gwas | StudyGate.Roas, StudyGates.For("ROAS_GWAS"));
    }

    [Fact]
    public void PatternsAreTheFrozenLiterals()
    {
        // Retyping these is the single easiest thing in the port to get wrong; assert the constants
        // themselves so a "tidy-up" of StudyGatePatterns cannot pass unnoticed.
        Assert.Equal("GBD|LANGTID|KORTTID", StudyGatePatterns.Gbd);
        Assert.Equal("NDV|ENDO|LANGTID|GBD|KORTTID", StudyGatePatterns.Ndv);
        Assert.Equal("GWAS", StudyGatePatterns.Gwas);
        Assert.Equal("ROAS", StudyGatePatterns.Roas);
        Assert.Equal("DOGFOOD", StudyGatePatterns.Dogfood);
    }

    [Fact]
    public void AlwaysIsZeroSoAnUngatedCollectorIsAlwaysOpen()
    {
        CollectorDescriptor ungated = new()
        {
            Name = "X",
            Title = "X",
            Kind = CollectorKind.Custom,
            PidBinding = PidBinding.None,
        };

        Assert.Equal(StudyGate.Always, ungated.Gate);
        Assert.True(StudyGates.IsOpenFor(ungated, StudyGate.Always));
        Assert.True(StudyGates.IsOpenFor(ungated, StudyGate.Gbd | StudyGate.Roas));
    }
}
