namespace QuickStat.Collectors.Registry;

/// <summary>
/// Every statically registered collector, in the order <c>TQuickStatCollectors.PrepareStudy</c>
/// registers it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Order is part of the contract.</b> It is the order of the check-list users know, it is the
/// order columns appear in an export, and <see cref="ICollectorRegistry.TryFind"/> takes the first
/// match on a duplicate. The Delphi sequence is: the form-frequency collector, the basic set, the
/// lab set, then <c>2 x N</c> dynamic per-form collectors, then <c>'SIZE'</c>, then each gated
/// block. That is why the always-on collectors are split into two properties rather than one - the
/// dynamic collectors are registered <em>between</em> them.
/// </para>
/// <para>
/// The catalog is <c>partial</c> and each family owns its own file: 126 registrations in one file
/// would not be reviewable. The families are demographics and forms
/// (<c>CollectorCatalog.Basic.cs</c>), lab data (<c>.LabData.cs</c>), GBD var-sets
/// (<c>.Gbd.cs</c>), diagnoses (<c>.Diagnose.cs</c>), drugs (<c>.Drug.cs</c>), NDV / diabetes
/// (<c>.Ndv.cs</c>) and the three small protocol families (<c>.Protocol.cs</c>).
/// </para>
/// <para>
/// Each family is produced by a factory method rather than by a field initializer, so that the
/// composition below cannot depend on the order in which the compiler happens to process the
/// partial files.
/// </para>
/// <para>
/// <b>Five collectors are deliberately absent</b>: <c>QS_DRUG_ANTIBIOTIC_INTERMEDIATE</c>,
/// <c>QS_DRUG_ANTIBIOTIC_RECOMMENDED</c>, <c>QS_DRUG_J01XX05</c>, <c>QS_ROAS_BASE</c> and
/// <c>QST_LAB_INTERLEUKINS</c>. They are commented out in this repository's
/// <c>QuickStat.Collectors.pas</c> and they also need library-side implementations brought across
/// from the pinned ref, which is Phase 4's job (PORT-PLAN.md §5). Restoring them takes the registry
/// from 126 to 131 distinct names and a <c>KORTTID</c> study from 120 to 124; each is one line in
/// the family file it belongs to.
/// </para>
/// </remarks>
public static partial class CollectorCatalog
{
    /// <summary>
    /// The always-on collectors registered <b>before</b> the dynamic per-form ones: 35 of them.
    /// </summary>
    public static IReadOnlyList<ICollector> AlwaysBeforeFormCollectors { get; } = CreateAlwaysBeforeFormCollectors();

    /// <summary>
    /// The always-on collectors registered <b>after</b> the dynamic per-form ones: just
    /// <c>'SIZE'</c>, which sits outside every gate block in
    /// <c>AddCollectorsHardCoded</c>.
    /// </summary>
    public static IReadOnlyList<ICollector> AlwaysAfterFormCollectors { get; } = CreateAlwaysAfterFormCollectors();

    /// <summary>
    /// Gate <b>G</b>: 24 GBD var-sets, then the 17 diagnosis collectors, then the 35 drug
    /// collectors - 76 in all.
    /// </summary>
    /// <remarks>
    /// <c>AddCollectorsDiagnose</c> and <c>AddCollectorsDrug</c> are called from inside the
    /// <c>GBD|LANGTID|KORTTID</c> block and nowhere else, which is why those two families are part
    /// of this gate rather than gates of their own.
    /// </remarks>
    public static IReadOnlyList<ICollector> GbdFamily { get; } =
        [.. CreateGbdCollectors(), .. CreateDiagnoseCollectors(), .. CreateDrugCollectors()];

    /// <summary>Gate <b>N</b>: the NDV / diabetes set, 8 collectors.</summary>
    public static IReadOnlyList<ICollector> NdvFamily { get; } = CreateNdvCollectors();

    /// <summary>Gate <b>W</b>: 3 GWAS collectors.</summary>
    public static IReadOnlyList<ICollector> GwasFamily { get; } = CreateGwasCollectors();

    /// <summary>Gate <b>R</b>: 2 ROAS collectors.</summary>
    public static IReadOnlyList<ICollector> RoasFamily { get; } = CreateRoasCollectors();

    /// <summary>Gate <b>D</b>: the single dogfood collector.</summary>
    public static IReadOnlyList<ICollector> DogfoodFamily { get; } = CreateDogfoodCollectors();

    /// <summary>
    /// The gated families in Delphi registration order, each paired with the gate that admits it.
    /// </summary>
    public static IReadOnlyList<GatedCollectorFamily> GatedFamilies { get; } =
    [
        new(StudyGate.Gbd, GbdFamily),
        new(StudyGate.Ndv, NdvFamily),
        new(StudyGate.Gwas, GwasFamily),
        new(StudyGate.Roas, RoasFamily),
        new(StudyGate.Dogfood, DogfoodFamily),
    ];

    /// <summary>
    /// All 126 static collectors, in registration order, with every gate treated as open.
    /// </summary>
    /// <remarks>
    /// The order is the same as a study matching every gate would produce, except that the
    /// <c>2 x N</c> dynamic per-form collectors are absent - they depend on the study's form
    /// classes and are added by <see cref="CollectorRegistryBuilder"/>.
    /// </remarks>
    public static IReadOnlyList<ICollector> All { get; } =
    [
        .. AlwaysBeforeFormCollectors,
        .. AlwaysAfterFormCollectors,
        .. GbdFamily,
        .. NdvFamily,
        .. GwasFamily,
        .. RoasFamily,
        .. DogfoodFamily,
    ];

    private static IReadOnlyList<ICollector> CreateAlwaysBeforeFormCollectors() =>
        [.. CreateBasicCollectors(), .. CreateLabDataCollectors()];
}
