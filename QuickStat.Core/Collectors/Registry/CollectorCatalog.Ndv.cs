namespace QuickStat.Collectors.Registry;

/// <content>
/// The <c>NDV|ENDO|LANGTID|GBD|KORTTID</c> block - seven diabetes var-sets and one lab set
/// (<c>QuickStat.Collectors.pas:455-467</c>).
/// </content>
/// <remarks>
/// <c>LANGTID</c>, <c>KORTTID</c> and <c>GBD</c> appear in this gate <em>and</em> in the GBD gate,
/// so those studies get both families. That overlap is the whole point of commit <c>5502b72</c>
/// and it is why <c>KORTTID</c> must be present in both regex literals.
/// </remarks>
public static partial class CollectorCatalog
{
    private static IReadOnlyList<ICollector> CreateNdvCollectors() =>
    [
        Make.VarSetNumeric(CollectorNames.NdvDiagnose, CollectorTitles.NdvBasicData, ItemSets.NdvDiagnose),
        Make.VarSetNumeric(CollectorNames.NdvTreatment, CollectorTitles.DiabetesTreatment, ItemSets.NdvTreatment),
        Make.VarSetNumeric(CollectorNames.NdvComplications, CollectorTitles.DiabetesComplications, ItemSets.NdvComplications),
        Make.VarSetNumeric(CollectorNames.NdvInsulin, CollectorTitles.DiabetesInsulin, ItemSets.NdvInsulin),
        Make.VarSetNumeric(CollectorNames.NdvHypoglycemia, CollectorTitles.DiabetesHypoglycemia, ItemSets.NdvHypoglycemia),
        Make.VarSetNumeric(CollectorNames.NdvExercise, CollectorTitles.DiabetesExercise, ItemSets.NdvExercise),
        Make.VarSetNumeric(CollectorNames.NdvSocial, CollectorTitles.DiabetesSocial, ItemSets.NdvSocial),

        // Registered by this block only, even though the GBD block registers QST_LAB_GERIATRIC.
        Make.LabSet(CollectorNames.LabDiabetes, CollectorTitles.LabSetDiabetes, LabClassSets.Diabetes),
    ];
}
