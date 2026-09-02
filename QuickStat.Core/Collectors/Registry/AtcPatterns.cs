namespace QuickStat.Collectors.Registry;

/// <summary>
/// The <c>ATC_*</c> <c>LIKE</c> patterns from <c>EPR.QA.Collector.Drug.pas</c>.
/// </summary>
/// <remarks>
/// Each pattern determines both the query's <c>LIKE</c> argument and, through
/// <see cref="QuickStat.Collectors.Sql.SqlLiteral.AtcPatternToVariableName"/>, the collector's
/// name and the emitted variable name - so a stray <c>%</c> would rename a column and break a saved
/// package. Patterns without a trailing <c>%</c> are exact matches and are deliberate.
/// </remarks>
public static class AtcPatterns
{
    /// <summary><c>ATC_A10</c> - blood-glucose lowering drugs.</summary>
    public const string A10 = "A10%";

    /// <summary><c>ATC_A10BA02</c> - metformin, excluding combinations. Exact match.</summary>
    public const string A10Ba02 = "A10BA02";

    /// <summary><c>ATC_A11EA</c> - vitamin B complex. Exact match, and unusually has no <c>%</c>.</summary>
    /// <remarks>
    /// Checked on 2026-09-02 and it is deliberate (PORT-PLAN.md §8.11). The rule in the Delphi's
    /// constant block is <c>%</c> iff the code has level-5 children - <c>A10BA</c> has 3,
    /// <c>B01AF</c> 5, <c>B03BA</c> 7, <c>C08DA</c> 4, and all four carry it. <c>A11EA</c> has none,
    /// so <c>LIKE 'A11EA'</c> selects what <c>LIKE 'A11EA%'</c> would. 119 of 120 refs in the
    /// library history define it exactly this way and <c>'A11EA%'</c> has never existed. If FEST
    /// ever assigns an <c>A11EA01</c>, this line is the one to change.
    /// </remarks>
    public const string A11Ea = "A11EA";

    /// <summary><c>ATC_B01AA03</c> - warfarin. Exact match.</summary>
    public const string B01Aa03 = "B01AA03";

    /// <summary><c>ATC_B01AF</c> - direct oral anticoagulants.</summary>
    public const string B01Af = "B01AF%";

    /// <summary><c>ATC_B03BA</c> - vitamin B12.</summary>
    public const string B03Ba = "B03BA%";

    /// <summary><c>ATC_B03BA01</c> - cyanocobalamin. Exact match.</summary>
    public const string B03Ba01 = "B03BA01";

    /// <summary><c>ATC_B03BA03</c> - hydroxocobalamin. Exact match.</summary>
    public const string B03Ba03 = "B03BA03";

    /// <summary><c>ATC_C01A</c> - cardiac glycosides.</summary>
    public const string C01A = "C01A%";

    /// <summary><c>ATC_C02</c> - antihypertensives.</summary>
    public const string C02 = "C02%";

    /// <summary><c>ATC_C03</c> - diuretics.</summary>
    public const string C03 = "C03%";

    /// <summary><c>ATC_C07</c> - beta blockers.</summary>
    public const string C07 = "C07%";

    /// <summary><c>ATC_C08</c> - calcium channel blockers.</summary>
    public const string C08 = "C08%";

    /// <summary><c>ATC_C08D</c> - calcium channel blockers with cardiac effects.</summary>
    public const string C08D = "C08D%";

    /// <summary><c>ATC_C09</c> - renin-angiotensin system.</summary>
    public const string C09 = "C09%";

    /// <summary><c>ATC_C0x23789</c> - antihypertensives, broadly defined.</summary>
    public const string C0X23789 = "C0[23789]%";

    /// <summary>
    /// <c>ATC_J01XX05</c> - methenamine (Hiprex). Exact match, and deliberately so.
    /// </summary>
    /// <remarks>
    /// No <c>%</c>: methenamine has no sub-codes (<c>EPR.QA.Collector.Drug.pas:68</c>). The query
    /// still uses <c>LIKE</c> rather than <c>=</c>, so the collation behaviour is identical to every
    /// other drug pattern (<c>Docs/Port/03-collectors.md</c> §E.2.2).
    /// </remarks>
    public const string J01Xx05 = "J01XX05";

    /// <summary><c>ATC_M01A</c> - NSAIDs.</summary>
    public const string M01A = "M01A%";

    /// <summary><c>ATC_N02A</c> - opioids.</summary>
    public const string N02A = "N02A%";

    /// <summary><c>ATC_N02B</c> - other analgesics and antipyretics.</summary>
    public const string N02B = "N02B%";

    /// <summary><c>ATC_N04BA</c> - antiparkinson drugs.</summary>
    public const string N04Ba = "N04BA%";

    /// <summary><c>ATC_N05A</c> - antipsychotics.</summary>
    public const string N05A = "N05A%";

    /// <summary><c>ATC_N05B</c> - anxiolytics.</summary>
    public const string N05B = "N05B%";

    /// <summary><c>ATC_N05C</c> - hypnotics and sedatives.</summary>
    public const string N05C = "N05C%";

    /// <summary><c>ATC_N06A</c> - antidepressants.</summary>
    public const string N06A = "N06A%";

    /// <summary><c>ATC_N06D</c> - anti-dementia drugs.</summary>
    public const string N06D = "N06D%";

    /// <summary>
    /// The drug pattern of <c>QS_DIAGNOSE_MISSING_E11</c>'s antidiabetic half.
    /// </summary>
    public const string AntidiabeticsWithoutDiagnosis = "A10%";

    /// <summary>The ICD-10 half of <c>QS_DIAGNOSE_MISSING_E11</c>.</summary>
    public const string DiabetesDiagnosisCodes = "E1[01234]%";

    /// <summary>The variable name <c>QS_DIAGNOSE_MISSING_E11</c> emits.</summary>
    public const string AntidiabeticsWithoutDiagnosisVariable = "A10_NOT_E1x01234";
}
