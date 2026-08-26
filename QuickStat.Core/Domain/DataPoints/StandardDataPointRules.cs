using System.Globalization;

namespace QuickStat.Domain.DataPoints;

/// <summary>
/// The colouring and display rules QuickStat registers, one per Delphi <c>TDataPoint</c> subclass.
/// </summary>
/// <remarks>
/// <para>
/// <c>RegisterCustomDatapoints</c> (<c>QuickStat.Collectors.pas:154-176</c>) makes <b>sixteen</b>
/// registrations against <b>fourteen</b> distinct classes: <c>SYSBP</c> shares a class with
/// <c>SBP_UNSPEC</c> and <c>DIABP</c> with <c>DBP_UNSPEC</c>. Fourteen classes therefore means
/// fourteen threshold ladders, which is the count PORT-PLAN.md §7.1 records.
/// </para>
/// <para>
/// This is the colouring users actually see. It is <em>not</em> the percentile machinery, which is
/// removed: <c>ProvideColor</c> gates on <c>InheritsFrom(TColoredDatapoint)</c> and nothing
/// registered descends from it, verified at the pinned library tip where <c>TColoredDataPoint</c>
/// (<c>EPR.QA.DataPoint.pas:42</c>) has no descendant anywhere.
/// </para>
/// <para>
/// The ladders are written out as explicit <c>if</c> chains rather than data tables so they can be
/// read side by side with the Pascal. Note that they mix <c>&gt;</c> and <c>&gt;=</c>, that three of
/// them have no no-data branch, and that one band of one ladder is a colour no other ladder uses.
/// </para>
/// </remarks>
public static class StandardDataPointRules
{
    /// <summary>Total cholesterol. Delphi <c>TCholDatapoint</c>.</summary>
    public const string TotalCholesterolVarName = "NPU01566";

    /// <summary>LDL cholesterol. Delphi <c>TLdlDatapoint</c>.</summary>
    public const string LdlVarName = "NPU01568";

    /// <summary>HbA1c in per cent. Delphi <c>THbA1cPercentDatapoint</c>.</summary>
    public const string HbA1cPercentVarName = "NPU03835";

    /// <summary>HbA1c in mmol/mol. Delphi <c>THbA1cMmolDatapoint</c>.</summary>
    public const string HbA1cMmolVarName = "NPU27300";

    /// <summary>Digitoxin. Delphi <c>TDigitoxinDatapoint</c>.</summary>
    public const string DigitoxinVarName = "NPU04786";

    /// <summary>Sodium. Delphi <c>TSodiumDatapoint</c>.</summary>
    public const string SodiumVarName = "NPU03429";

    /// <summary>Potassium. Delphi <c>TPotassiumDatapoint</c>.</summary>
    public const string PotassiumVarName = "NPU03230";

    /// <summary>Haemoglobin. Delphi <c>THemoGlobinDatapoint</c>.</summary>
    public const string HaemoglobinVarName = "NOR05172";

    /// <summary>Systolic blood pressure, unspecified position.</summary>
    public const string SystolicUnspecifiedVarName = "SBP_UNSPEC";

    /// <summary>Diastolic blood pressure, unspecified position.</summary>
    public const string DiastolicUnspecifiedVarName = "DBP_UNSPEC";

    /// <summary>Systolic blood pressure. Same rule as <see cref="SystolicUnspecifiedVarName"/>.</summary>
    public const string SystolicVarName = "SYSBP";

    /// <summary>Diastolic blood pressure. Same rule as <see cref="DiastolicUnspecifiedVarName"/>.</summary>
    public const string DiastolicVarName = "DIABP";

    /// <summary>Body mass index. Delphi <c>TBMIDatapoint</c>.</summary>
    public const string BodyMassIndexVarName = "BMI";

    /// <summary>Pulse quality. Delphi <c>TPulseQualityDatapoint</c>.</summary>
    public const string PulseQualityVarName = "PULSE_QUALITY";

    /// <summary>
    /// Installed database version. Delphi <c>VAR_DB_VERSION</c>
    /// (<c>EPR.QA.DataPoint.Dogfood.pas:24</c>).
    /// </summary>
    /// <remarks>
    /// Differs from <see cref="ServerVersionVarName"/> by case alone, and the lookup is
    /// case-sensitive, so the two resolve to different rules.
    /// </remarks>
    public const string DatabaseVersionVarName = "DB_VERSION";

    /// <summary>
    /// SQL Server major version. Delphi <c>VAR_SERVER_VERSION</c>
    /// (<c>EPR.QA.DataPoint.Dogfood.pas:25</c>).
    /// </summary>
    public const string ServerVersionVarName = "DbVersion";

    /// <summary>
    /// Total cholesterol: <c>&gt;8</c> grave, <c>&gt;7</c> high, <c>&gt;6</c> moderate, <c>&gt;5</c>
    /// mild, <c>&gt;4.5</c> low, <c>&gt;0</c> no risk, otherwise no data.
    /// </summary>
    public static DataPointRule TotalCholesterol { get; } = new() { BrushColor = TotalCholesterolBrush };

    /// <summary>
    /// LDL cholesterol: <c>&gt;5</c> grave, <c>&gt;4</c> high, <c>&gt;3</c> moderate, <c>&gt;2</c>
    /// mild, <c>&gt;1.8</c> low, <c>&gt;0</c> no risk, otherwise no data.
    /// </summary>
    public static DataPointRule Ldl { get; } = new() { BrushColor = LdlBrush };

    /// <summary>
    /// HbA1c in per cent: <c>&gt;10</c> grave, <c>&gt;9</c> high, <c>&gt;8</c> moderate,
    /// <c>&gt;7</c> mild, <c>&gt;6.5</c> low, <c>&gt;0</c> no risk, otherwise no data.
    /// </summary>
    public static DataPointRule HbA1cPercent { get; } = new() { BrushColor = HbA1cPercentBrush };

    /// <summary>
    /// HbA1c in mmol/mol: <c>&gt;86</c> grave, <c>&gt;75</c> high, <c>&gt;65</c> moderate,
    /// <c>&gt;58</c> mild, <c>&gt;53</c> low, <c>&gt;0</c> <see cref="RiskPalette.AliceBlue"/>,
    /// otherwise no data.
    /// </summary>
    /// <remarks>
    /// The lowest positive band is the one place in the whole palette where a ladder does not use
    /// <see cref="RiskPalette.NoRisk"/> for a normal value.
    /// </remarks>
    public static DataPointRule HbA1cMmol { get; } = new() { BrushColor = HbA1cMmolBrush };

    /// <summary>
    /// Digitoxin, two-sided: outside <c>[5,20]</c> grave, outside <c>[6,17]</c> pale purple, outside
    /// <c>[7,16]</c> high, outside <c>[8,15]</c> moderate, outside <c>[9,14]</c> mild, <c>&gt;0</c>
    /// no risk, otherwise no data.
    /// </summary>
    public static DataPointRule Digitoxin { get; } = new() { BrushColor = DigitoxinBrush };

    /// <summary>
    /// Sodium, two-sided: outside <c>[132,150]</c> grave, outside <c>[134,148]</c> high, outside
    /// <c>[136,146]</c> moderate, outside <c>[137,145]</c> mild, otherwise white.
    /// </summary>
    /// <remarks>
    /// <b>No no-data branch.</b> A value of zero is below every low bound and therefore renders as
    /// grave risk. Reproduced deliberately: PORT-PLAN.md §7.2 does not list it among the bugs being
    /// fixed, and §7 says nothing else may diverge.
    /// <c>Docs/Port/04-matrix-export.md</c> R-9 proposes adding the guard, which would be a
    /// one-line change here.
    /// </remarks>
    public static DataPointRule Sodium { get; } = new() { BrushColor = SodiumBrush };

    /// <summary>
    /// Potassium, two-sided: outside <c>[3,5.5]</c> grave, outside <c>[3.2,5.3]</c> high, outside
    /// <c>[3.3,5.2]</c> moderate, outside <c>[3.4,5.1]</c> mild, otherwise white.
    /// </summary>
    /// <remarks>No no-data branch; see <see cref="Sodium"/>.</remarks>
    public static DataPointRule Potassium { get; } = new() { BrushColor = PotassiumBrush };

    /// <summary>
    /// Haemoglobin, two-sided: outside <c>[9,20]</c> grave, outside <c>[10,19]</c> high, outside
    /// <c>[11,18.5]</c> moderate, outside <c>[12,18]</c> mild, otherwise white.
    /// </summary>
    /// <remarks>No no-data branch; see <see cref="Sodium"/>.</remarks>
    public static DataPointRule Haemoglobin { get; } = new() { BrushColor = HaemoglobinBrush };

    /// <summary>
    /// Systolic blood pressure: <c>&gt;180</c> grave, <c>&gt;160</c> high, <c>&gt;150</c> moderate,
    /// <c>&gt;140</c> <em>or</em> <c>&lt;100</c> mild, <c>&gt;0</c> no risk, otherwise no data.
    /// </summary>
    public static DataPointRule SystolicBloodPressure { get; } = new() { BrushColor = SystolicBrush };

    /// <summary>
    /// Diastolic blood pressure: <c>&gt;100</c> grave, <c>&gt;95</c> high, <c>&gt;90</c> moderate,
    /// <c>&gt;85</c> mild, <c>&gt;0</c> no risk, otherwise no data.
    /// </summary>
    public static DataPointRule DiastolicBloodPressure { get; } = new() { BrushColor = DiastolicBrush };

    /// <summary>
    /// Body mass index, shown to one decimal: <c>&lt;=0</c> no data, outside <c>[15,40]</c> grave,
    /// outside <c>[16,35]</c> high, outside <c>[17,30]</c> moderate, outside <c>[18.5,27]</c> mild,
    /// otherwise no risk.
    /// </summary>
    /// <remarks>
    /// The only ladder that tests for no data <em>first</em>, which is why zero renders grey here and
    /// salmon in the three lab ladders that omit the branch.
    /// </remarks>
    public static DataPointRule BodyMassIndex { get; } = new()
    {
        BrushColor = BodyMassIndexBrush,
        FormatValue = static value => value.ToString("F1", CultureInfo.CurrentCulture),
    };

    /// <summary>
    /// Pulse quality, an enumeration: 1 regular, 2 atrial fibrillation, 3 extrasystoles.
    /// </summary>
    /// <remarks>
    /// The value is rounded before it is matched, exactly as the Delphi's <c>Round</c> does, and .NET
    /// agrees with Delphi on half-to-even. Anything else shows <c>?</c> on
    /// <see cref="RiskPalette.NoData"/>.
    /// </remarks>
    public static DataPointRule PulseQuality { get; } = new()
    {
        BrushColor = PulseQualityBrush,
        FormatValue = PulseQualityText,
        SetsCaptionFromText = true,
    };

    /// <summary>
    /// Installed database version: <c>&gt;=19016</c> low, <c>&gt;=19000</c> mild, <c>&gt;=18000</c>
    /// moderate, <c>&gt;0</c> grave, otherwise no data.
    /// </summary>
    /// <remarks>The only ladder built from <c>&gt;=</c> rather than <c>&gt;</c>.</remarks>
    public static DataPointRule DatabaseVersion { get; } = new() { BrushColor = DatabaseVersionBrush };

    /// <summary>
    /// SQL Server major version, shown as a product year: <c>&gt;=7</c> low, <c>&gt;4</c> mild,
    /// <c>&gt;0</c> grave, otherwise no data.
    /// </summary>
    public static DataPointRule ServerVersion { get; } = new()
    {
        BrushColor = ServerVersionBrush,
        FormatValue = ServerVersionText,
    };

    /// <summary>
    /// Drug presence: the caption truncated to eight characters, or <c>Ja</c>/<c>Nei</c>. No
    /// colouring.
    /// </summary>
    /// <remarks>
    /// <b>Declared but not registered.</b> The Delphi registers this class 22 times inside
    /// <c>AddCollectorsDrug</c> under keys such as <c>ATC_A10%</c>, but the collector emits
    /// <c>ATC_A10.F</c> - the raw ATC pattern is converted and a treatment-type suffix appended - so
    /// no key can ever match and <c>Ja</c>/<c>Nei</c> never renders
    /// (<c>Docs/Port/04-matrix-export.md</c> R-6). The rule is provided here so the collector
    /// registry can bind it to the variable names it actually produces; inventing those names is not
    /// this step's to do.
    /// </remarks>
    public static DataPointRule Drug { get; } = new()
    {
        FormatValue = static value => value > 0 ? "Ja" : "Nei",
        CaptionTakesPrecedence = true,
        CaptionLength = 8,
    };

    /// <summary>
    /// The sixteen registrations, in the order <c>RegisterCustomDatapoints</c> makes them.
    /// </summary>
    /// <remarks>Keys are compared ordinally, so <c>DB_VERSION</c> and <c>DbVersion</c> are distinct.</remarks>
    public static IReadOnlyDictionary<string, DataPointRule> Registrations { get; } =
        new Dictionary<string, DataPointRule>(StringComparer.Ordinal)
        {
            [TotalCholesterolVarName] = TotalCholesterol,
            [LdlVarName] = Ldl,
            [HbA1cPercentVarName] = HbA1cPercent,
            [HbA1cMmolVarName] = HbA1cMmol,
            [DigitoxinVarName] = Digitoxin,
            [SodiumVarName] = Sodium,
            [PotassiumVarName] = Potassium,
            [HaemoglobinVarName] = Haemoglobin,
            [SystolicUnspecifiedVarName] = SystolicBloodPressure,
            [DiastolicUnspecifiedVarName] = DiastolicBloodPressure,
            [SystolicVarName] = SystolicBloodPressure,
            [DiastolicVarName] = DiastolicBloodPressure,
            [BodyMassIndexVarName] = BodyMassIndex,
            [PulseQualityVarName] = PulseQuality,
            [DatabaseVersionVarName] = DatabaseVersion,
            [ServerVersionVarName] = ServerVersion,
        };

    private static Rgb? TotalCholesterolBrush(double value) =>
        value > 8 ? RiskPalette.GraveRisk
        : value > 7 ? RiskPalette.HighRisk
        : value > 6 ? RiskPalette.ModerateRisk
        : value > 5 ? RiskPalette.MildRisk
        : value > 4.5 ? RiskPalette.LowRisk
        : value > 0 ? RiskPalette.NoRisk
        : RiskPalette.NoData;

    private static Rgb? LdlBrush(double value) =>
        value > 5 ? RiskPalette.GraveRisk
        : value > 4 ? RiskPalette.HighRisk
        : value > 3 ? RiskPalette.ModerateRisk
        : value > 2 ? RiskPalette.MildRisk
        : value > 1.8 ? RiskPalette.LowRisk
        : value > 0 ? RiskPalette.NoRisk
        : RiskPalette.NoData;

    private static Rgb? HbA1cPercentBrush(double value) =>
        value > 10 ? RiskPalette.GraveRisk
        : value > 9 ? RiskPalette.HighRisk
        : value > 8 ? RiskPalette.ModerateRisk
        : value > 7 ? RiskPalette.MildRisk
        : value > 6.5 ? RiskPalette.LowRisk
        : value > 0 ? RiskPalette.NoRisk
        : RiskPalette.NoData;

    private static Rgb? HbA1cMmolBrush(double value) =>
        value > 86 ? RiskPalette.GraveRisk
        : value > 75 ? RiskPalette.HighRisk
        : value > 65 ? RiskPalette.ModerateRisk
        : value > 58 ? RiskPalette.MildRisk
        : value > 53 ? RiskPalette.LowRisk
        : value > 0 ? RiskPalette.AliceBlue
        : RiskPalette.NoData;

    private static Rgb? DigitoxinBrush(double value) =>
        (value < 5) || (value > 20) ? RiskPalette.GraveRisk
        : (value < 6) || (value > 17) ? RiskPalette.DataPalePurple
        : (value < 7) || (value > 16) ? RiskPalette.HighRisk
        : (value < 8) || (value > 15) ? RiskPalette.ModerateRisk
        : (value < 9) || (value > 14) ? RiskPalette.MildRisk
        : value > 0 ? RiskPalette.NoRisk
        : RiskPalette.NoData;

    private static Rgb? SodiumBrush(double value) =>
        (value < 132) || (value > 150) ? RiskPalette.GraveRisk
        : (value < 134) || (value > 148) ? RiskPalette.HighRisk
        : (value < 136) || (value > 146) ? RiskPalette.ModerateRisk
        : (value < 137) || (value > 145) ? RiskPalette.MildRisk
        : RiskPalette.NoRisk;

    private static Rgb? PotassiumBrush(double value) =>
        (value < 3) || (value > 5.5) ? RiskPalette.GraveRisk
        : (value < 3.2) || (value > 5.3) ? RiskPalette.HighRisk
        : (value < 3.3) || (value > 5.2) ? RiskPalette.ModerateRisk
        : (value < 3.4) || (value > 5.1) ? RiskPalette.MildRisk
        : RiskPalette.NoRisk;

    private static Rgb? HaemoglobinBrush(double value) =>
        (value < 9) || (value > 20) ? RiskPalette.GraveRisk
        : (value < 10) || (value > 19) ? RiskPalette.HighRisk
        : (value < 11) || (value > 18.5) ? RiskPalette.ModerateRisk
        : (value < 12) || (value > 18.0) ? RiskPalette.MildRisk
        : RiskPalette.NoRisk;

    private static Rgb? SystolicBrush(double value) =>
        value > 180 ? RiskPalette.GraveRisk
        : value > 160 ? RiskPalette.HighRisk
        : value > 150 ? RiskPalette.ModerateRisk
        : (value > 140) || (value < 100) ? RiskPalette.MildRisk
        : value > 0 ? RiskPalette.NoRisk
        : RiskPalette.NoData;

    private static Rgb? DiastolicBrush(double value) =>
        value > 100 ? RiskPalette.GraveRisk
        : value > 95 ? RiskPalette.HighRisk
        : value > 90 ? RiskPalette.ModerateRisk
        : value > 85 ? RiskPalette.MildRisk
        : value > 0 ? RiskPalette.NoRisk
        : RiskPalette.NoData;

    private static Rgb? BodyMassIndexBrush(double value) =>
        value <= 0 ? RiskPalette.NoData
        : (value > 40) || (value < 15) ? RiskPalette.GraveRisk
        : (value > 35) || (value < 16) ? RiskPalette.HighRisk
        : (value > 30) || (value < 17) ? RiskPalette.ModerateRisk
        : (value > 27) || (value < 18.5) ? RiskPalette.MildRisk
        : RiskPalette.NoRisk;

    private static Rgb? PulseQualityBrush(double value) => (int)Math.Round(value) switch
    {
        1 => RiskPalette.NoRisk,
        2 or 3 => RiskPalette.MildRisk,
        _ => RiskPalette.NoData,
    };

    private static string PulseQualityText(double value) => (int)Math.Round(value) switch
    {
        1 => "Rgm",
        2 => "AF",
        3 => "ES",
        _ => "?",
    };

    private static Rgb? DatabaseVersionBrush(double value) =>
        value >= 19016 ? RiskPalette.LowRisk
        : value >= 19000 ? RiskPalette.MildRisk
        : value >= 18000 ? RiskPalette.ModerateRisk
        : value > 0 ? RiskPalette.GraveRisk
        : RiskPalette.NoData;

    private static Rgb? ServerVersionBrush(double value) =>
        value >= 7 ? RiskPalette.LowRisk
        : value > 4 ? RiskPalette.MildRisk
        : value > 0 ? RiskPalette.GraveRisk
        : RiskPalette.NoData;

    private static string ServerVersionText(double value)
    {
        // Delphi compares the double against each literal exactly; a version is always integral.
        if (value == 7)
        {
            return "2016";
        }

        if (value == 6)
        {
            return "2014";
        }

        if (value == 5)
        {
            return "2012";
        }

        if (value == 4)
        {
            return "2008R2";
        }

        return value > 0 ? "Gammel" : "?";
    }
}
