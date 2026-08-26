using QuickStat.Collectors.Sql;

namespace QuickStat.Collectors.Registry;

/// <content>
/// <c>AddCollectorsDiagnose</c> - 17 collectors, called from inside the
/// <c>GBD|LANGTID|KORTTID</c> block only (<c>QuickStat.Collectors.pas:256-278</c>).
/// </content>
public static partial class CollectorCatalog
{
    private static IReadOnlyList<ICollector> CreateDiagnoseCollectors() =>
    [
        // Counting diagnoses by how many leading characters of the ICD-10 code are kept.
        Make.DiagnoseCount(CollectorNames.DiagnoseAll1, CollectorTitles.DiagnoseAll1, level: 1),
        Make.DiagnoseCount(CollectorNames.DiagnoseAll2, CollectorTitles.DiagnoseAll2, level: 2),
        Make.DiagnoseCount(CollectorNames.DiagnoseAll3, CollectorTitles.DiagnoseAll3, level: 3),
        Make.DiagnoseCount(CollectorNames.DiagnoseAll4, CollectorTitles.DiagnoseAll4, level: 4),
        Make.DiagnoseCount(CollectorNames.DiagnoseAll5, CollectorTitles.DiagnoseAll5, level: 5),

        // Decision support: on antidiabetics without a diabetes diagnosis.
        Make.Custom(
            CollectorNames.DiagnoseMissingE11,
            CollectorTitles.DiagnoseMissingE11,
            CollectorNames.DrugVersusDiagnosePrefix,
            QaSql.DrugWithoutDiagnose(
                AtcPatterns.AntidiabeticsWithoutDiagnosisVariable,
                AtcPatterns.AntidiabeticsWithoutDiagnosis,
                AtcPatterns.DiabetesDiagnosisCodes)),

        // ICD-10 patterns. Registration order is cancer, thyroid, diabetes, endocrine, metabolic,
        // psychiatry, hypertension, ischemia, atrial fibrillation, stroke - AF before stroke.
        Make.Diagnose(CollectorTitles.DiagnoseCancer, "C%"),
        Make.Diagnose(CollectorTitles.DiagnoseThyroid, "E0%"),
        Make.Diagnose(CollectorTitles.DiagnoseDiabetes, "E1[014]%"),
        Make.Diagnose(CollectorTitles.DiagnoseEndocrine, "E[23]%"),
        Make.Diagnose(CollectorTitles.DiagnoseMetabolic, "E[789]%"),
        Make.Diagnose(CollectorTitles.DiagnosePsychiatry, "F[123456789]%"),
        Make.Diagnose(CollectorTitles.DiagnoseHypertension, "I1[012345]%"),
        Make.Diagnose(CollectorTitles.DiagnoseIschemia, "I2[012345]%"),
        Make.Diagnose(CollectorTitles.DiagnoseAtrialFibrillation, "I48%"),
        Make.Diagnose(CollectorTitles.DiagnoseStroke, "I6[01234]%"),

        Make.Dementia(CollectorTitles.DiagnoseDementia),
    ];
}
