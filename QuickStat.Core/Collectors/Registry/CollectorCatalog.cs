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
/// The catalog is <c>partial</c> and each family owns its own file: 131 registrations in one file
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
/// <b>One collector is still absent</b>: <c>QST_LAB_INTERLEUKINS</c>. It is commented out in this
/// repository's <c>QuickStat.Collectors.pas</c> and it also needs a library-side implementation
/// brought across from the pinned ref, which is Phase 4's job (PORT-PLAN.md §5). Restoring it takes
/// the registry from 130 to 131 distinct names, and is one line in <c>CollectorCatalog.LabData.cs</c>.
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
    /// Gate <b>G</b>: 24 GBD var-sets, then the 17 diagnosis collectors, then the 38 drug
    /// collectors - 79 in all.
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

    /// <summary>Gate <b>R</b>: 3 ROAS collectors.</summary>
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
    /// All 130 static collectors, in registration order, with every gate treated as open.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The order is the same as a study matching every gate would produce, except that the
    /// <c>2 x N</c> dynamic per-form collectors are absent - they depend on the study's form
    /// classes and are added by <see cref="CollectorRegistryBuilder"/>.
    /// </para>
    /// <para>
    /// Availability is <em>not</em> applied here either, so
    /// <see cref="CollectorNames.DrugAntibioticIntermediate"/> is present even though a database
    /// without <c>KB.AntibioticResistance2</c> would never register it. This is the catalog, not a
    /// session's registry.
    /// </para>
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
