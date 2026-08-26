using QuickStat.Collectors.Sql;

namespace QuickStat.Collectors.Registry;

/// <content>
/// The 24 GBD var-set and custom collectors of the <c>GBD|LANGTID|KORTTID</c> block
/// (<c>QuickStat.Collectors.pas:425-451</c>).
/// </content>
public static partial class CollectorCatalog
{
    private static IReadOnlyList<ICollector> CreateGbdCollectors() =>
    [
        Make.VarSetAge(CollectorNames.GbdWeightDays, CollectorTitles.GbdWeightDays, ItemSets.Weight),
        Make.Custom(CollectorNames.GbdTvangsvedtak, CollectorTitles.GbdTvangsvedtak, string.Empty, QaSql.GbdTvangsvedtak),
        Make.FormCountSingle(
            CollectorNames.GbdAdmissions12M,
            CollectorTitles.GbdAdmissions12M,
            CollectorNames.Forms12MPrefix,
            FormNames.GbdInnleggelse,
            months: 12),

        // Note the variable prefix: the collector is named FORMS3M.LEGEALLE but its columns come out
        // as FORMS.GBDLEGE. That mismatch is upstream (EPR.QA.Collector.Factory.pas:129).
        Make.Custom(
            CollectorNames.GbdDoctorNotes3M,
            CollectorTitles.GbdDoctorNotes3M,
            CollectorNames.FormsPrefix,
            QaSql.RecentFormGroupLege3M(),
            CollectorKind.FormCount),

        Make.VarSetNumeric(CollectorNames.GbdScores, CollectorTitles.GbdScores, ItemSets.GbdScores),
        Make.VarSetNumeric(CollectorNames.GbdBloodPressure, CollectorTitles.GbdBloodPressure, ItemSets.GbdBloodPressure),
        Make.VarSetText(CollectorNames.GbdPrimaryContact, CollectorTitles.GbdPrimaryContact, ItemSets.GbdPrimaryContact),

        Make.Custom(
            CollectorNames.GbdWeight2M,
            CollectorTitles.GbdWeight2M,
            CollectorNames.Last2MVariablePrefix,
            QaSql.RecentQuantityPresent(itemId: 3224, monthsAgo: 2)),

        // PORT-PLAN.md §7.2, collision 2: upstream registers this under QS_GBD_WEIGHT_2M, so the
        // weight collector shadows it. Fixed here - the name is GBD.SBP_2M, which is what the
        // QS_GBD_SBP_2M constant always said.
        Make.Custom(
            CollectorNames.GbdSystolic2M,
            CollectorTitles.GbdSystolic2M,
            CollectorNames.Last2MVariablePrefix,
            QaSql.RecentQuantityPresent(itemId: 3556, monthsAgo: 2)),

        Make.Custom(
            CollectorNames.GbdFlacker12M,
            CollectorTitles.GbdFlacker12M,
            CollectorNames.Last12MVariablePrefix,
            QaSql.RecentQuantityPresent(itemId: 1128, monthsAgo: 12)),
        Make.Custom(
            CollectorNames.GbdFlackerDeath,
            CollectorTitles.GbdFlackerDeath,
            CollectorNames.FlackerKielyVariablePrefix,
            QaSql.FlackerKielyDeath()),
        Make.Custom(
            CollectorNames.GbdHulten3M,
            CollectorTitles.GbdHulten3M,
            CollectorNames.Last3MVariablePrefix,
            QaSql.RecentQuantityPresent(itemId: 4234, monthsAgo: 3)),
        Make.Custom(
            CollectorNames.GbdQualid6M,
            CollectorTitles.GbdQualid6M,
            CollectorNames.Last6MVariablePrefix,
            QaSql.RecentQuantityPresent(itemId: 5827, monthsAgo: 6)),
        Make.Custom(
            CollectorNames.GbdKdv6M,
            CollectorTitles.GbdKdv6M,
            CollectorNames.Last6MVariablePrefix,
            QaSql.RecentQuantityPresent(itemId: 1685, monthsAgo: 6)),
        Make.Custom(
            CollectorNames.GbdBarthel6M,
            CollectorTitles.GbdBarthel6M,
            CollectorNames.Last6MVariablePrefix,
            QaSql.RecentQuantityPresent(itemId: 4342, monthsAgo: 6)),
        Make.Custom(
            CollectorNames.GbdStratify6M,
            CollectorTitles.GbdStratify6M,
            CollectorNames.Last6MVariablePrefix,
            QaSql.RecentQuantityPresent(itemId: 9257, monthsAgo: 6)),
        Make.Custom(
            CollectorNames.GbdMna6M,
            CollectorTitles.GbdMna6M,
            CollectorNames.Last6MVariablePrefix,
            QaSql.RecentQuantityPresent(itemId: 4771, monthsAgo: 6)),

        Make.Custom(
            CollectorNames.GbdAntiHypertensivesLowBp,
            CollectorTitles.GbdAntiHypertensivesLowBp,
            CollectorNames.OverTreatVariablePrefix,
            QaSql.DrugHypertensionWithLowBp(120)),
        Make.Custom(
            CollectorNames.GbdLowBp,
            CollectorTitles.GbdLowBp,
            CollectorNames.LastBelowVariablePrefix,
            QaSql.SnapshotQuantityIfBelowThreshold(itemId: 3556, value: 120.0)),
        Make.Custom(
            CollectorNames.GbdAceLowGfr,
            CollectorTitles.GbdAceLowGfr,
            CollectorNames.LastBelowVariablePrefix,
            QaSql.DrugAndRenalFunction(AtcPatterns.C09, 35)),
        Make.Custom(
            CollectorNames.GbdMetforminLowGfr,
            CollectorTitles.GbdMetforminLowGfr,
            CollectorNames.LastBelowVariablePrefix,
            QaSql.DrugAndRenalFunction("A10BA%", 50)),

        Make.FormCompleteness(
            CollectorNames.GbdLmg6M,
            CollectorTitles.GbdLmg6M,
            CollectorNames.Forms6MPrefix,
            FormNames.Lmg,
            months: 6),
        Make.FormCompleteness(
            CollectorNames.GbdBeslutninger6M,
            CollectorTitles.GbdBeslutninger6M,
            CollectorNames.Forms6MPrefix,
            FormNames.Beslutninger,
            months: 6),

        // The one lab-set collector inside the GBD block. Its group name already contains a colon,
        // so CollectorTitle.ForLabSet leaves it verbatim instead of wrapping it as "Labdata: …".
        Make.LabSet(CollectorNames.LabGeriatric, CollectorTitles.LabSetGeriatric, LabClassSets.Geriatric),
    ];
}
