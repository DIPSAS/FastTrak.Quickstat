using System.Globalization;
using QuickStat.Domain.DataPoints;
using Xunit;

namespace QuickStat.Tests.Domain.DataPoints;

/// <summary>
/// The fourteen threshold ladders, each exercised at the value on both sides of every band edge.
/// </summary>
/// <remarks>
/// <para>
/// Expected colours are written as hex rather than as <see cref="RiskPalette"/> references on
/// purpose: that way a ladder test fails if the ladder is wrong <em>or</em> if the palette byte order
/// is wrong, and the table can be read against
/// <c>Docs/Port/04-matrix-export.md</c> §7.1 without following an indirection.
/// </para>
/// <para>
/// Sources: <c>EPR.QA.DataPoint.{Biochemistry,Pharmacology,VitalSigns,HeartFailure,Dogfood}.pas</c>
/// at <c>origin/tarmscreening/develop</c> (<c>249ac2d16</c>).
/// </para>
/// </remarks>
public class StandardDataPointRulesTests
{
    private const string NoRisk = "#FFFFFF";
    private const string LowRisk = "#D1EFB3";
    private const string MildRisk = "#FFFFBF";
    private const string ModerateRisk = "#FFEDBF";
    private const string HighRisk = "#FFDBBF";
    private const string GraveRisk = "#FF8080";
    private const string NoData = "#DCDCDC";
    private const string PalePurple = "#EEB2E7";
    private const string AliceBlue = "#F0F8FF";

    private static void AssertBrush(DataPointRule rule, double value, string expected)
    {
        Assert.NotNull(rule.BrushColor);

        Rgb? colour = rule.BrushColor(value);

        Assert.NotNull(colour);
        Assert.Equal(expected, colour.Value.ToHex());
    }

    // 1 of 14 - TCholDatapoint, Biochemistry:87-103.
    [Theory]
    [InlineData(8.1, GraveRisk)]
    [InlineData(8, HighRisk)]
    [InlineData(7.1, HighRisk)]
    [InlineData(7, ModerateRisk)]
    [InlineData(6.1, ModerateRisk)]
    [InlineData(6, MildRisk)]
    [InlineData(5.1, MildRisk)]
    [InlineData(5, LowRisk)]
    [InlineData(4.6, LowRisk)]
    [InlineData(4.5, NoRisk)]
    [InlineData(0.1, NoRisk)]
    [InlineData(0, NoData)]
    [InlineData(-1, NoData)]
    public void TotalCholesterol(double value, string expected) =>
        AssertBrush(StandardDataPointRules.TotalCholesterol, value, expected);

    // 2 of 14 - TLdlDatapoint, Biochemistry:67-83.
    [Theory]
    [InlineData(5.1, GraveRisk)]
    [InlineData(5, HighRisk)]
    [InlineData(4.1, HighRisk)]
    [InlineData(4, ModerateRisk)]
    [InlineData(3, MildRisk)]
    [InlineData(2, LowRisk)]
    [InlineData(1.9, LowRisk)]
    [InlineData(1.8, NoRisk)]
    [InlineData(0.1, NoRisk)]
    [InlineData(0, NoData)]
    public void Ldl(double value, string expected) =>
        AssertBrush(StandardDataPointRules.Ldl, value, expected);

    // 3 of 14 - THbA1cPercentDatapoint, Biochemistry:107-123.
    [Theory]
    [InlineData(10.1, GraveRisk)]
    [InlineData(10, HighRisk)]
    [InlineData(9, ModerateRisk)]
    [InlineData(8, MildRisk)]
    [InlineData(7, LowRisk)]
    [InlineData(6.6, LowRisk)]
    [InlineData(6.5, NoRisk)]
    [InlineData(0.1, NoRisk)]
    [InlineData(0, NoData)]
    public void HbA1cPercent(double value, string expected) =>
        AssertBrush(StandardDataPointRules.HbA1cPercent, value, expected);

    // 4 of 14 - THbA1cMmolDatapoint, Biochemistry:125-141.  The only ladder whose lowest positive
    // band is not clNoRisk.
    [Theory]
    [InlineData(86.1, GraveRisk)]
    [InlineData(86, HighRisk)]
    [InlineData(75, ModerateRisk)]
    [InlineData(65, MildRisk)]
    [InlineData(58, LowRisk)]
    [InlineData(53.1, LowRisk)]
    [InlineData(53, AliceBlue)]
    [InlineData(0.1, AliceBlue)]
    [InlineData(0, NoData)]
    public void HbA1cMmol(double value, string expected) =>
        AssertBrush(StandardDataPointRules.HbA1cMmol, value, expected);

    // 5 of 14 - TDigitoxinDatapoint, Pharmacology:42-58.  Two-sided, and the only user of pale
    // purple.
    [Theory]
    [InlineData(20.1, GraveRisk)]
    [InlineData(20, PalePurple)]
    [InlineData(17.1, PalePurple)]
    [InlineData(17, HighRisk)]
    [InlineData(16.1, HighRisk)]
    [InlineData(16, ModerateRisk)]
    [InlineData(15.1, ModerateRisk)]
    [InlineData(15, MildRisk)]
    [InlineData(14.1, MildRisk)]
    [InlineData(14, NoRisk)]
    // Each low bound belongs to the band *above* it, because the ladder tests strict "<": a value
    // of 8 is not < 8, so it falls through to "< 9" and lands on mild rather than moderate.
    [InlineData(9, NoRisk)]
    [InlineData(8.9, MildRisk)]
    [InlineData(8, MildRisk)]
    [InlineData(7.9, ModerateRisk)]
    [InlineData(7, ModerateRisk)]
    [InlineData(6.9, HighRisk)]
    [InlineData(6, HighRisk)]
    [InlineData(5.9, PalePurple)]
    [InlineData(5, PalePurple)]
    [InlineData(4.9, GraveRisk)]
    public void Digitoxin(double value, string expected) =>
        AssertBrush(StandardDataPointRules.Digitoxin, value, expected);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DigitoxinTreatsMissingValuesAsGraveRatherThanNoData(double value)
    {
        // Value < 5 catches zero before the >0 and no-data branches are ever reached, so both of
        // those are unreachable in the Delphi too.  Faithful, and worth pinning so a later "tidy-up"
        // does not quietly change what a clinician sees.
        AssertBrush(StandardDataPointRules.Digitoxin, value, GraveRisk);
    }

    // 6 of 14 - TSodiumDatapoint, Biochemistry:159-171.
    [Theory]
    [InlineData(150.1, GraveRisk)]
    [InlineData(150, HighRisk)]
    [InlineData(148.1, HighRisk)]
    [InlineData(148, ModerateRisk)]
    [InlineData(146.1, ModerateRisk)]
    [InlineData(146, MildRisk)]
    [InlineData(145.1, MildRisk)]
    [InlineData(145, NoRisk)]
    [InlineData(137, NoRisk)]
    [InlineData(136.9, MildRisk)]
    [InlineData(136, MildRisk)]
    [InlineData(135.9, ModerateRisk)]
    [InlineData(134, ModerateRisk)]
    [InlineData(133.9, HighRisk)]
    [InlineData(132, HighRisk)]
    [InlineData(131.9, GraveRisk)]
    public void Sodium(double value, string expected) =>
        AssertBrush(StandardDataPointRules.Sodium, value, expected);

    // 7 of 14 - TPotassiumDatapoint, Biochemistry:175-187.
    [Theory]
    [InlineData(5.51, GraveRisk)]
    [InlineData(5.5, HighRisk)]
    [InlineData(5.31, HighRisk)]
    [InlineData(5.3, ModerateRisk)]
    [InlineData(5.21, ModerateRisk)]
    [InlineData(5.2, MildRisk)]
    [InlineData(5.11, MildRisk)]
    [InlineData(5.1, NoRisk)]
    [InlineData(3.4, NoRisk)]
    [InlineData(3.39, MildRisk)]
    [InlineData(3.3, MildRisk)]
    [InlineData(3.29, ModerateRisk)]
    [InlineData(3.2, ModerateRisk)]
    [InlineData(3.19, HighRisk)]
    [InlineData(3, HighRisk)]
    [InlineData(2.99, GraveRisk)]
    public void Potassium(double value, string expected) =>
        AssertBrush(StandardDataPointRules.Potassium, value, expected);

    // 8 of 14 - THemoGlobinDatapoint, Biochemistry:191-204.
    [Theory]
    [InlineData(20.1, GraveRisk)]
    [InlineData(20, HighRisk)]
    [InlineData(19.1, HighRisk)]
    [InlineData(19, ModerateRisk)]
    [InlineData(18.6, ModerateRisk)]
    [InlineData(18.5, MildRisk)]
    [InlineData(18.1, MildRisk)]
    [InlineData(18, NoRisk)]
    [InlineData(12, NoRisk)]
    [InlineData(11.9, MildRisk)]
    [InlineData(11, MildRisk)]
    [InlineData(10.9, ModerateRisk)]
    [InlineData(10, ModerateRisk)]
    [InlineData(9.9, HighRisk)]
    [InlineData(9, HighRisk)]
    [InlineData(8.9, GraveRisk)]
    public void Haemoglobin(double value, string expected) =>
        AssertBrush(StandardDataPointRules.Haemoglobin, value, expected);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SodiumPotassiumAndHaemoglobinPaintZeroAsGraveRisk(double value)
    {
        // Docs/Port/04-matrix-export.md R-9: these three ladders have no no-data branch, so a
        // missing value reads as the most alarming colour on the palette.  Reproduced deliberately -
        // PORT-PLAN.md §7.2 does not list it among the bugs being fixed, and §7 forbids any other
        // divergence.  Compare with BodyMassIndexGuardsAgainstNoDataFirst below.
        AssertBrush(StandardDataPointRules.Sodium, value, GraveRisk);
        AssertBrush(StandardDataPointRules.Potassium, value, GraveRisk);
        AssertBrush(StandardDataPointRules.Haemoglobin, value, GraveRisk);
    }

    // 9 of 14 - TSBPDatapoint, VitalSigns:63-77.  The mild band is two-sided while the rest is not.
    [Theory]
    [InlineData(181, GraveRisk)]
    [InlineData(180, HighRisk)]
    [InlineData(161, HighRisk)]
    [InlineData(160, ModerateRisk)]
    [InlineData(151, ModerateRisk)]
    [InlineData(150, MildRisk)]
    [InlineData(141, MildRisk)]
    [InlineData(140, NoRisk)]
    [InlineData(100, NoRisk)]
    [InlineData(99, MildRisk)]
    [InlineData(0, MildRisk)]
    public void SystolicBloodPressure(double value, string expected) =>
        AssertBrush(StandardDataPointRules.SystolicBloodPressure, value, expected);

    // 10 of 14 - TDBPDatapoint, VitalSigns:81-95.
    [Theory]
    [InlineData(101, GraveRisk)]
    [InlineData(100, HighRisk)]
    [InlineData(96, HighRisk)]
    [InlineData(95, ModerateRisk)]
    [InlineData(91, ModerateRisk)]
    [InlineData(90, MildRisk)]
    [InlineData(86, MildRisk)]
    [InlineData(85, NoRisk)]
    [InlineData(0.1, NoRisk)]
    [InlineData(0, NoData)]
    public void DiastolicBloodPressure(double value, string expected) =>
        AssertBrush(StandardDataPointRules.DiastolicBloodPressure, value, expected);

    // 11 of 14 - TBMIDatapoint, VitalSigns:45-59.
    [Theory]
    [InlineData(40.1, GraveRisk)]
    [InlineData(40, HighRisk)]
    [InlineData(35.1, HighRisk)]
    [InlineData(35, ModerateRisk)]
    [InlineData(30.1, ModerateRisk)]
    [InlineData(30, MildRisk)]
    [InlineData(27.1, MildRisk)]
    [InlineData(27, NoRisk)]
    [InlineData(18.5, NoRisk)]
    [InlineData(18.4, MildRisk)]
    [InlineData(17, MildRisk)]
    [InlineData(16.9, ModerateRisk)]
    [InlineData(16, ModerateRisk)]
    [InlineData(15.9, HighRisk)]
    [InlineData(15, HighRisk)]
    [InlineData(14.9, GraveRisk)]
    [InlineData(0.1, GraveRisk)]
    public void BodyMassIndex(double value, string expected) =>
        AssertBrush(StandardDataPointRules.BodyMassIndex, value, expected);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BodyMassIndexGuardsAgainstNoDataFirst(double value) =>
        AssertBrush(StandardDataPointRules.BodyMassIndex, value, NoData);

    [Fact]
    public void BodyMassIndexIsShownToOneDecimal()
    {
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nb-NO");

            Assert.NotNull(StandardDataPointRules.BodyMassIndex.FormatValue);
            Assert.Equal("24,5", StandardDataPointRules.BodyMassIndex.FormatValue(24.51));
            Assert.Equal("31,0", StandardDataPointRules.BodyMassIndex.FormatValue(31));

            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            Assert.Equal("31.0", StandardDataPointRules.BodyMassIndex.FormatValue(31));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // 12 of 14 - TPulseQualityDatapoint, HeartFailure:38-62.
    [Theory]
    [InlineData(1, NoRisk)]
    [InlineData(2, MildRisk)]
    [InlineData(3, MildRisk)]
    [InlineData(0, NoData)]
    [InlineData(4, NoData)]
    [InlineData(-1, NoData)]
    public void PulseQuality(double value, string expected) =>
        AssertBrush(StandardDataPointRules.PulseQuality, value, expected);

    [Theory]
    [InlineData(1, "Rgm")]
    [InlineData(2, "AF")]
    [InlineData(3, "ES")]
    [InlineData(0, "?")]
    [InlineData(9, "?")]
    [InlineData(1.4, "Rgm")]
    [InlineData(1.5, "AF")] // Round is half-to-even in both languages.
    [InlineData(2.5, "AF")]
    public void PulseQualityText(double value, string expected)
    {
        Assert.NotNull(StandardDataPointRules.PulseQuality.FormatValue);
        Assert.Equal(expected, StandardDataPointRules.PulseQuality.FormatValue(value));
    }

    [Fact]
    public void PulseQualityIsTheOnlyRuleThatForcesLeftAlignment() =>
        Assert.Equal(
            [StandardDataPointRules.PulseQualityVarName],
            StandardDataPointRules.Registrations
                .Where(entry => entry.Value.SetsCaptionFromText)
                .Select(entry => entry.Key));

    // 13 of 14 - TDbVersionDatapoint, Dogfood:29-41.  The only ladder built from >= rather than >.
    [Theory]
    [InlineData(19016, LowRisk)]
    [InlineData(19015, MildRisk)]
    [InlineData(19000, MildRisk)]
    [InlineData(18999, ModerateRisk)]
    [InlineData(18000, ModerateRisk)]
    [InlineData(17999, GraveRisk)]
    [InlineData(1, GraveRisk)]
    [InlineData(0, NoData)]
    [InlineData(-1, NoData)]
    public void DatabaseVersion(double value, string expected) =>
        AssertBrush(StandardDataPointRules.DatabaseVersion, value, expected);

    // 14 of 14 - TDbServerVersionDatapoint, Dogfood:61-71.
    [Theory]
    [InlineData(8, LowRisk)]
    [InlineData(7, LowRisk)]
    [InlineData(6, MildRisk)]
    [InlineData(5, MildRisk)]
    [InlineData(4.5, MildRisk)]
    [InlineData(4, GraveRisk)]
    [InlineData(1, GraveRisk)]
    [InlineData(0, NoData)]
    [InlineData(-1, NoData)]
    public void ServerVersion(double value, string expected) =>
        AssertBrush(StandardDataPointRules.ServerVersion, value, expected);

    [Theory]
    [InlineData(7, "2016")]
    [InlineData(6, "2014")]
    [InlineData(5, "2012")]
    [InlineData(4, "2008R2")]
    [InlineData(3, "Gammel")]
    [InlineData(0.5, "Gammel")]
    [InlineData(0, "?")]
    [InlineData(-1, "?")]
    public void ServerVersionText(double value, string expected)
    {
        Assert.NotNull(StandardDataPointRules.ServerVersion.FormatValue);
        Assert.Equal(expected, StandardDataPointRules.ServerVersion.FormatValue(value));
    }

    // Registered 22 times in the Delphi under keys that can never match, so it ships unregistered
    // here until the collector registry can supply the names it actually emits.
    [Theory]
    [InlineData(1, "Ja")]
    [InlineData(0.5, "Ja")]
    [InlineData(0, "Nei")]
    [InlineData(-1, "Nei")]
    public void DrugRuleSaysJaOrNei(double value, string expected)
    {
        Assert.NotNull(StandardDataPointRules.Drug.FormatValue);
        Assert.Equal(expected, StandardDataPointRules.Drug.FormatValue(value));
    }

    [Fact]
    public void DrugRuleShowsEightCaptionCharactersAndIsNotRegistered()
    {
        Assert.True(StandardDataPointRules.Drug.CaptionTakesPrecedence);
        Assert.Equal(8, StandardDataPointRules.Drug.CaptionLength);
        Assert.Null(StandardDataPointRules.Drug.BrushColor);
        Assert.DoesNotContain(StandardDataPointRules.Drug, StandardDataPointRules.Registrations.Values);
    }

    [Fact]
    public void ThereAreSixteenRegistrationsOverFourteenLadders()
    {
        // PORT-PLAN.md §7.1 says fourteen analyte classes; QuickStat.Collectors.pas:154-176 makes
        // sixteen registrations.  Both are right: SYSBP shares TSBPDatapoint with SBP_UNSPEC and
        // DIABP shares TDBPDatapoint with DBP_UNSPEC.
        Assert.Equal(16, StandardDataPointRules.Registrations.Count);

        int ladders = StandardDataPointRules.Registrations.Values
            .Where(rule => rule.BrushColor is not null)
            .Distinct()
            .Count();

        Assert.Equal(14, ladders);
    }

    [Theory]
    [InlineData("NPU01566")]
    [InlineData("NPU01568")]
    [InlineData("NPU03835")]
    [InlineData("NPU27300")]
    [InlineData("NPU04786")]
    [InlineData("NPU03429")]
    [InlineData("NPU03230")]
    [InlineData("NOR05172")]
    [InlineData("SBP_UNSPEC")]
    [InlineData("DBP_UNSPEC")]
    [InlineData("SYSBP")]
    [InlineData("DIABP")]
    [InlineData("BMI")]
    [InlineData("PULSE_QUALITY")]
    [InlineData("DB_VERSION")]
    [InlineData("DbVersion")]
    public void EveryRegisteredVariableNameIsPresent(string varName) =>
        Assert.True(StandardDataPointRules.Registrations.ContainsKey(varName));

    [Fact]
    public void TheTwoBloodPressureAliasesShareTheirRules()
    {
        Assert.Same(
            StandardDataPointRules.Registrations["SBP_UNSPEC"],
            StandardDataPointRules.Registrations["SYSBP"]);

        Assert.Same(
            StandardDataPointRules.Registrations["DBP_UNSPEC"],
            StandardDataPointRules.Registrations["DIABP"]);
    }
}
