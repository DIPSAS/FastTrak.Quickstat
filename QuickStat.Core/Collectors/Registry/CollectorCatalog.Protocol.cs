namespace QuickStat.Collectors.Registry;

/// <content>
/// The three small protocol blocks: <c>GWAS</c>, <c>ROAS</c> and <c>DOGFOOD</c>
/// (<c>QuickStat.Collectors.pas:468-486</c>).
/// </content>
public static partial class CollectorCatalog
{
    private static IReadOnlyList<ICollector> CreateGwasCollectors() =>
    [
        Make.VarSetNumeric(CollectorNames.RoasGwasBackground, CollectorTitles.RoasGwasBackground, ItemSets.GwasBackground),
        Make.VarSetMax(CollectorNames.RoasGwasAutoAntibody, CollectorTitles.RoasGwasAutoAntibody, ItemSets.GwasAutoAntibody),
        Make.VarSetMax(CollectorNames.RoasGwasAps1, CollectorTitles.RoasGwasAps1, ItemSets.GwasAps1),
    ];

    private static IReadOnlyList<ICollector> CreateRoasCollectors() =>
    [
        Make.VarSetNumeric(CollectorNames.RoasPoiOrdinal, CollectorTitles.RoasPoiOrdinal, ItemSets.PoiOrdinal),
        Make.VarSetNumeric(CollectorNames.RoasPoiQuantity, CollectorTitles.RoasPoiQuantity, ItemSets.PoiQuantity),

        // Phase 4 slots QS_ROAS_BASE in here - 68 item ids, registered as the bare literal
        // 'Autommunitet' so that TVarSetCollector's own ' (siste)' is not doubled.
    ];

    private static IReadOnlyList<ICollector> CreateDogfoodCollectors() =>
    [
        // The only case-insensitive gate: a study called "dogfood" gets this one.
        Make.VarSetNumeric(
            CollectorNames.DogfoodDatabaseVersion,
            CollectorTitles.DogfoodDatabaseVersion,
            ItemSets.DogfoodDatabaseVersion),
    ];
}
