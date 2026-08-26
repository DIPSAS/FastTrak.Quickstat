namespace QuickStat.Collectors;

/// <summary>
/// Which family a collector belongs to.
/// </summary>
/// <remarks>
/// Provenance only - it drives grouping, golden-file naming and the split of the registry across
/// files. It is deliberately <em>not</em> polymorphism: the Delphi's thirteen
/// <c>TDataCollector</c> subclasses differ in nothing but
/// <c>(name, title, varPrefix, batchSize, sql)</c>, so the hierarchy does not survive the port
/// (<c>Docs/Port/03-collectors.md</c> §G.1).
/// </remarks>
public enum CollectorKind
{
    /// <summary><c>TCustomDataCollector</c> - a finished SQL string and nothing else.</summary>
    Custom = 0,

    /// <summary><c>TDemographicsCollector</c> and its six subclasses over <c>dbo.Person</c>.</summary>
    Demographics = 1,

    /// <summary><c>TGlobalCollector</c> - study-scoped queries over <c>dbo.StudCase</c>.</summary>
    StudyCase = 2,

    /// <summary><c>TFormInstanceCollector</c> - form counts per type.</summary>
    FormInstance = 3,

    /// <summary><c>TFormDataCollector</c> - one dynamic collector per form class in the study.</summary>
    FormData = 4,

    /// <summary><c>TFormAgeCollector</c> - time since a form was last filled in.</summary>
    FormAge = 5,

    /// <summary>Recent-form counting.</summary>
    FormCount = 6,

    /// <summary>Recent-form completeness.</summary>
    FormCompleteness = 7,

    /// <summary><c>TVarSetCollector</c> - latest value for a fixed set of item ids.</summary>
    VarSet = 8,

    /// <summary><c>TVarSetAgeCollector</c> - time since an item set was last recorded.</summary>
    VarSetAge = 9,

    /// <summary><c>TVarSetMaxCollector</c> - highest value for an item set.</summary>
    VarSetMax = 10,

    /// <summary><c>TLabSetCollector</c> - latest lab values for a set of lab classes.</summary>
    LabSet = 11,

    /// <summary><c>TLab{High,Medium,Low}TrustCollector</c>.</summary>
    LabTrust = 12,

    /// <summary>Lab sample counts over a recent window.</summary>
    LabCount = 13,

    /// <summary><c>TDiagnoseCollector</c> / <c>TDementiaCollector</c>.</summary>
    Diagnose = 14,

    /// <summary>Diagnosis counts by specificity level.</summary>
    DiagnoseCount = 15,

    /// <summary><c>TDrugCollector</c> - one per ATC pattern.</summary>
    Drug = 16,

    /// <summary>Named drug sets, including the antibiotic collectors.</summary>
    DrugSet = 17,

    /// <summary>DRUID drug-drug interaction collectors.</summary>
    DrugInteraction = 18,
}
