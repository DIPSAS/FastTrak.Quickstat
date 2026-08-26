namespace QuickStat.Collectors.Registry;

/// <summary>
/// The <c>LABCLASSES_*</c> lab-class arrays from <c>EPR.QA.Definitions.pas</c>, plus the one set
/// that upstream expresses as a Delphi <c>set of TLabTest</c>.
/// </summary>
/// <remarks>
/// Order is observable in the generated <c>IN ( … )</c> list, exactly as for
/// <see cref="ItemSets"/>. Only the sets a registered collector uses are here;
/// <c>LABCLASSES_KIDNEY</c>, <c>LABCLASSES_NUTRITION</c>, <c>LABCLASSES_URINE</c>,
/// <c>LABCLASSES_DIABETES_LIPIDS</c>, <c>LABCLASSES_DIABETES_NDV</c> and
/// <c>LABCLASSES_DIABETES_BDR</c> are unreachable from QuickStat's registry.
/// </remarks>
public static class LabClassSets
{
    /// <summary>
    /// <c>LABSET_KIDNEY</c>, resolved to lab-class ids.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The only collector built with <c>TLabSetCollector.CreateOldSchool</c>, which converts a
    /// Delphi <c>set of TLabTest</c> to ordinals. A Delphi <c>for..in</c> over a set yields
    /// ordinals in <b>ascending</b> order, so the emitted list is sorted even though the set
    /// literal is not.
    /// </para>
    /// <para>
    /// <c>LABSET_KIDNEY = [ltUrate, ltUrea, ltEstGFR, ltCreatinine, ltNatrium, ltKalium] +
    /// LABSET_URINE</c> where <c>LABSET_URINE = [ltDUAlbumin, ltUAlbumin, ltUMicroAlbumin,
    /// ltACRatio, ltDUProtein]</c> (<c>VMR.Lab.Interfaces.pas:95,103</c>). Against the
    /// <c>TLabTest</c> declaration those are 53, 54, 50, 49, 90, 91 and 7, 4, 5, 6, 3.
    /// </para>
    /// <para>
    /// <b>This corrects <c>Docs/Port/03-collectors.md</c> §B.7</b>, whose ordinal table is one too
    /// high for the five kidney members and therefore derives
    /// <c>(3, 4, 5, 6, 7, 50, 51, 54, 55, 90, 91)</c>. Two independent checks say otherwise:
    /// <c>LABCLASSES_KIDNEY</c> contains 49, 50, 53 and 54 for the same four analytes, and
    /// <c>QuickStat.Collectors.pas:206-209</c> registers 51 and 52 as "eGFR Cockgroft-Gault" and
    /// "eGFR MDRD", which are <c>ltCockcroftGault</c> and <c>ltMDRD</c> - the two members
    /// immediately after <c>ltEstGFR</c>.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<int> Kidney { get; } = [3, 4, 5, 6, 7, 49, 50, 53, 54, 90, 91];

    /// <summary><c>LABCLASSES_ANEMIA</c> - 14 ids.</summary>
    public static IReadOnlyList<int> Anemia { get; } = [22, 29, 30, 31, 62, 63, 64, 77, 78, 79, 80, 81, 82, 193];

    /// <summary><c>LABCLASSES_LIPIDS</c>.</summary>
    public static IReadOnlyList<int> Lipids { get; } = [34, 35, 36, 37, 38, 39, 40];

    /// <summary><c>LABCLASSES_DIGITALIS</c>.</summary>
    public static IReadOnlyList<int> Digitalis { get; } = [91, 124, 140, 171];

    /// <summary><c>LABCLASSES_LIVER</c>.</summary>
    public static IReadOnlyList<int> Liver { get; } = [123, 124, 125, 126, 127, 128, 129, 139];

    /// <summary><c>LABCLASSES_THYROID</c>.</summary>
    public static IReadOnlyList<int> Thyroid { get; } = [83, 84, 85, 86, 87, 88, 89];

    /// <summary><c>LABCLASSES_GLUCOSE</c>.</summary>
    public static IReadOnlyList<int> Glucose { get; } = [41, 42, 43, 44, 46, 47, 48, 58, 59, 60, 1058];

    /// <summary><c>LABCLASSES_INR</c>.</summary>
    public static IReadOnlyList<int> Inr { get; } = [18, 20];

    /// <summary><c>LABCLASSES_HYPERPARA</c>.</summary>
    public static IReadOnlyList<int> HyperPara { get; } = [94, 95, 332, 576, 770];

    /// <summary><c>LABCLASSES_HEART_FAILURE</c> - 15 ids.</summary>
    public static IReadOnlyList<int> HeartFailure { get; } =
        [6, 22, 49, 50, 51, 52, 53, 90, 91, 124, 140, 171, 575, 995, 1075];

    /// <summary><c>LABCLASSES_INTERLEUKINS</c> - <c>[1094..1104]</c>, 11 consecutive ids.</summary>
    /// <remarks>
    /// No gaps and no extras (<c>EPR.QA.Definitions.pas:122</c>). Restored by Phase 4 from commit
    /// <c>fefc8a809</c>, the only one of the four features that exists on just one of the two
    /// tarmscreening refs (PORT-PLAN.md R12, <c>Docs/Port/03-collectors.md</c> §E.4).
    /// </remarks>
    public static IReadOnlyList<int> Interleukins { get; } =
        [1094, 1095, 1096, 1097, 1098, 1099, 1100, 1101, 1102, 1103, 1104];

    /// <summary><c>LABCLASSES_CRP</c>.</summary>
    public static IReadOnlyList<int> Crp { get; } = [26];

    /// <summary><c>LABCLASSES_GERIATRIC</c>.</summary>
    public static IReadOnlyList<int> Geriatric { get; } = [22, 50, 51, 52, 53, 91, 140, 575, 995, 1075];

    /// <summary><c>LABCLASSES_DIABETES</c> - 17 ids.</summary>
    public static IReadOnlyList<int> Diabetes { get; } =
        [3, 4, 5, 6, 7, 34, 35, 36, 53, 54, 50, 49, 90, 91, 995, 1058, 1075];
}
