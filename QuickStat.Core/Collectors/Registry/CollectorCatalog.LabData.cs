namespace QuickStat.Collectors.Registry;

/// <content>
/// <c>AddCollectorsLabData</c> - nineteen always-on lab collectors
/// (<c>QuickStat.Collectors.pas:280-311</c>).
/// </content>
public static partial class CollectorCatalog
{
    private static IReadOnlyList<ICollector> CreateLabDataCollectors() =>
    [
        // Standard lab-set collectors. Every group name here is colon-free, so CollectorTitle.ForLabSet
        // wraps each one as "Labdata: <group> (siste)".
        Make.LabSet(CollectorNames.LabKidney, CollectorTitles.LabSetKidney, LabClassSets.Kidney),
        Make.LabSet(CollectorNames.LabAnemia, CollectorTitles.LabSetAnemia, LabClassSets.Anemia),
        Make.LabSet(CollectorNames.LabLipids, CollectorTitles.LabSetLipids, LabClassSets.Lipids),
        Make.LabSet(CollectorNames.LabDigitalis, CollectorTitles.LabSetDigitalis, LabClassSets.Digitalis),
        Make.LabSet(CollectorNames.LabLiver, CollectorTitles.LabSetLiver, LabClassSets.Liver),
        Make.LabSet(CollectorNames.LabThyroid, CollectorTitles.LabSetThyroid, LabClassSets.Thyroid),
        Make.LabSet(CollectorNames.LabGlucose, CollectorTitles.LabSetGlucose, LabClassSets.Glucose),
        Make.LabSet(CollectorNames.LabInr, CollectorTitles.LabSetInr, LabClassSets.Inr),
        Make.LabSet(CollectorNames.LabHyperPara, CollectorTitles.LabSetHyperPara, LabClassSets.HyperPara),
        Make.LabSet(CollectorNames.LabHeartFailure, CollectorTitles.LabSetHeartFailure, LabClassSets.HeartFailure),

        // Phase 4 slots QST_LAB_INTERLEUKINS in here, between heart failure and CRP - that is where
        // the commented-out registration sits (QuickStat.Collectors.pas:297).
        Make.LabSet(CollectorNames.LabCrp, CollectorTitles.LabSetCrp, LabClassSets.Crp),

        // All lab data, by the confidence the lab class carries.
        Make.LabTrust(CollectorNames.LabHighTrust, CollectorTitles.LabHighTrust, trustLevel: 3),
        Make.LabTrust(CollectorNames.LabMediumTrust, CollectorTitles.LabMediumTrust, trustLevel: 2),

        // PORT-PLAN.md §7.2, collision 1: upstream registers this one under QST_LAB_MEDIUM, so the
        // medium-trust collector shadows it and no saved package can ever refer to it. Fixed here.
        Make.LabTrust(CollectorNames.LabLowTrust, CollectorTitles.LabLowTrust, trustLevel: 1),

        // How many lab results in the window at all.
        Make.LabCount(CollectorNames.LabCount3M, CollectorTitles.LabCount3M, months: 3),
        Make.LabCount(CollectorNames.LabCount6M, CollectorTitles.LabCount6M, months: 6),
        Make.LabCount(CollectorNames.LabCount12M, CollectorTitles.LabCount12M, months: 12),
        Make.LabCount(CollectorNames.LabCount24M, CollectorTitles.LabCount24M, months: 24),
        Make.LabCount(CollectorNames.LabCount60M, CollectorTitles.LabCount60M, months: 60),
    ];
}
