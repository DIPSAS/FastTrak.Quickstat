namespace QuickStat.Collectors.Registry;

/// <summary>
/// The <c>SET_*</c> item-id arrays from <c>EPR.QA.Definitions.pas</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Order is observable.</b> Each array becomes an <c>IN ( … )</c> list through
/// <see cref="QuickStat.Collectors.Sql.SqlLiteral.List"/>, and that list is what Phase 5's golden
/// files compare byte for byte. Do not sort, de-duplicate or tidy.
/// </para>
/// <para>
/// Only the sets a registered collector uses are here. <c>SET_NDV_BP</c>, <c>SET_SMOKING</c>,
/// <c>SET_GBD_NUTRITION</c>, <c>SET_MNA_PART1</c>, <c>SET_GBD_DEMENTIA</c>,
/// <c>SET_GBD_HEART_FAILURE</c>, <c>SET_GBD_FALLS</c>, <c>SET_GBD_INR</c>, <c>SET_INSULINPUMPE</c>,
/// <c>SET_BDR_*</c>, <c>SET_BP_ALL</c>, <c>SET_NDV_CONSENT</c> and <c>SET_DM_LABDATA</c> feed only
/// collectors QuickStat never registers (PORT-PLAN.md §7.1).
/// </para>
/// </remarks>
public static class ItemSets
{
    /// <summary><c>SET_HEIGHT_WEIGHT_BMI</c> - height, weight, BMI.</summary>
    public static IReadOnlyList<int> HeightWeightBmi { get; } = [3224, 3225, 3310];

    /// <summary><c>SET_WEIGHT</c>.</summary>
    public static IReadOnlyList<int> Weight { get; } = [3224];

    /// <summary><c>SET_GBD_BP</c> - diastolic and systolic from the observation chart.</summary>
    public static IReadOnlyList<int> GbdBloodPressure { get; } = [3555, 3556];

    /// <summary><c>SET_GBD_SCORES</c>.</summary>
    public static IReadOnlyList<int> GbdScores { get; } = [1128, 1685, 4234, 4342, 4771, 4787, 4791, 5827, 9257];

    /// <summary><c>SET_GBD_PRIMARY_CONTACT</c>.</summary>
    public static IReadOnlyList<int> GbdPrimaryContact { get; } = [8420];

    /// <summary><c>SET_NDV_DIAGNOSE</c>.</summary>
    public static IReadOnlyList<int> NdvDiagnose { get; } = [3196, 3389, 3486];

    /// <summary><c>SET_NDV_TREATMENT</c>.</summary>
    public static IReadOnlyList<int> NdvTreatment { get; } = [3322, 4056];

    /// <summary><c>SET_NDV_COMPLICATIONS</c> - 21 ids.</summary>
    public static IReadOnlyList<int> NdvComplications { get; } =
    [
        3351, 3352, 4235, 3218, 3397, 3398, 3414, 3415, 3417, 4054, 4055,
        4062, 4087, 4205, 4521, 4527, 4845, 7517, 7519, 7520, 7521,
    ];

    /// <summary><c>SET_NDV_INSULIN</c> - 8 ids.</summary>
    public static IReadOnlyList<int> NdvInsulin { get; } = [3322, 4056, 3209, 3906, 3206, 3905, 3933, 3908];

    /// <summary><c>SET_NDV_HYPOGLYCEMIA</c>.</summary>
    public static IReadOnlyList<int> NdvHypoglycemia { get; } = [3220, 3351, 4234, 3352];

    /// <summary><c>SET_NDV_EXERCISE</c>.</summary>
    public static IReadOnlyList<int> NdvExercise { get; } = [3340, 3197, 4638];

    /// <summary><c>SET_NDV_SOCIAL</c>.</summary>
    public static IReadOnlyList<int> NdvSocial { get; } = [3982, 4002];

    /// <summary><c>SET_GWAS_BG</c> - 15 ids.</summary>
    public static IReadOnlyList<int> GwasBackground { get; } =
        [2143, 6089, 6299, 6090, 6312, 6321, 6313, 6314, 6317, 3411, 6318, 8594, 3410, 6320, 6050];

    /// <summary><c>SET_GWAS_AUTOANTIBODY</c> - 7 ids.</summary>
    public static IReadOnlyList<int> GwasAutoAntibody { get; } = [5947, 5948, 5949, 6044, 6049, 6051, 6058];

    /// <summary><c>SET_GWAS_APS1</c> - 8 ids.</summary>
    public static IReadOnlyList<int> GwasAps1 { get; } = [6076, 6077, 6078, 6079, 6080, 6073, 6045, 6074];

    /// <summary><c>SET_POI_ORD</c> - 20 ids.</summary>
    public static IReadOnlyList<int> PoiOrdinal { get; } =
    [
        2143, 6299, 6090, 6314, 6321, 6663, 6312, 6313, 6318, 6806,
        3410, 7977, 3411, 6320, 6322, 7978, 6317, 6316, 8543, 6050,
    ];

    /// <summary><c>SET_POI_QN</c> - 13 ids.</summary>
    public static IReadOnlyList<int> PoiQuantity { get; } =
        [6089, 3486, 6332, 6323, 6324, 6334, 6328, 6330, 6331, 6333, 6327, 6326, 8544];

    /// <summary><c>SET_ROAS_BASE</c> - 68 ids (<c>EPR.QA.Definitions.pas:103-105</c>).</summary>
    /// <remarks>
    /// <para>
    /// The largest set in the file, and the one that most needs its order left alone: at 68 items it
    /// can add 68 columns to the matrix, and the generated <c>IN ( … )</c> list is what a golden file
    /// compares. Transcribed in the source order, three source lines flattened into seven rows of
    /// ten; there are no duplicates.
    /// </para>
    /// <para>
    /// Restored by Phase 4 from commit <c>8a9954c13</c>
    /// (<c>Docs/Port/03-collectors.md</c> §E.3).
    /// </para>
    /// </remarks>
    public static IReadOnlyList<int> RoasBase { get; } =
    [
        4255, 6314, 3486, 6312, 6323, 6313, 6324, 6299, 6089, 6090,
        6321, 6332, 3410, 6328, 6317, 6327, 6316, 6326, 8594, 8595,
        6318, 6334, 6329, 3411, 6330, 6320, 6331, 6322, 6333, 8543,
        8544, 6669, 6670, 6671, 6607, 5069, 3982, 6633, 6634, 6635,
        6636, 6637, 6638, 6639, 6640, 6808, 6641, 5170, 9996, 3983,
        7135, 4002, 6682, 3985, 8797, 6605, 2143, 9477, 10643, 3846,
        3981, 6804, 6805, 6802, 6803, 7977, 7979, 6807,
    ];

    /// <summary>
    /// The dogfood collector's inline pair, <c>[3812, 5117]</c>
    /// (<c>EPR.QA.Collector.Factory.pas:323</c>) - not a <c>SET_*</c> constant upstream.
    /// </summary>
    public static IReadOnlyList<int> DogfoodDatabaseVersion { get; } = [3812, 5117];
}
