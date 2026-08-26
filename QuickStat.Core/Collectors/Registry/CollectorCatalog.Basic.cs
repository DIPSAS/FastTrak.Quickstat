using QuickStat.Collectors.Sql;

namespace QuickStat.Collectors.Registry;

/// <content>
/// Demographics, study-case facts and form counting: <c>QS_FORM_FREQUENCY</c> from
/// <c>PrepareStudy</c>, the fifteen collectors of <c>AddCollectorsBasic</c>, and the one
/// always-on registration inside <c>AddCollectorsHardCoded</c>.
/// </content>
public static partial class CollectorCatalog
{
    private static IReadOnlyList<ICollector> CreateBasicCollectors() =>
    [
        // QuickStat.Collectors.pas:129 - registered by PrepareStudy itself, before everything else.
        Make.FormInstances(CollectorNames.FormFrequency, CollectorTitles.FormFrequencies),

        // AddCollectorsBasic - demographics.
        Make.Demographics(CollectorNames.PatientAge, CollectorTitles.DemographicsAge, "AGE", "DATEDIFF(YYYY,DOB,GETDATE())"),
        Make.Demographics(CollectorNames.PatientSex, CollectorTitles.DemographicsSex, "SEX", "GenderId"),
        Make.Demographics(CollectorNames.PatientYearOfBirth, CollectorTitles.DemographicsYearOfBirth, "YOB", "DATEPART(YYYY,DOB)"),
        Make.Demographics(CollectorNames.PatientYearOfDeath, CollectorTitles.DemographicsYearOfDeath, "YOD", "DATEPART(YYYY,DeceasedDate)"),
        Make.Demographics(CollectorNames.PatientMonthOfBirth, CollectorTitles.DemographicsMonthOfBirth, "MOB", "DATEPART(MM,DOB)"),
        Make.Demographics(CollectorNames.PatientPostCode, CollectorTitles.DemographicsPostCode, "ZIP", "CONVERT(INTEGER,PostalCode)"),

        // AddCollectorsBasic - study case. These five need the study id, so their SQL is a function
        // of it rather than a constant.
        Make.StudyScoped(CollectorNames.StudyStatus, CollectorTitles.StudyStatus, studyId => QaSql.StudCaseFields("StatusId", "FinState", studyId)),
        Make.StudyScoped(CollectorNames.StudyCenter, CollectorTitles.StudyCenter, QaSql.StudyCenter),
        Make.StudyScoped(CollectorNames.StudyGroup, CollectorTitles.StudyGroup, studyId => QaSql.StudCaseFields("GroupId", "GroupId", studyId)),
        Make.StudyScoped(CollectorNames.StudyGroupAtDeath, CollectorTitles.StudyGroupAtDeath, QaSql.StudyGroupDeath),
        Make.StudyScoped(CollectorNames.StudyCenterAtDeath, CollectorTitles.StudyCenterAtDeath, QaSql.StudyCenterDeath),

        // AddCollectorsBasic - counting forms. Registered newest window first, as upstream.
        Make.FormCountAll(CollectorNames.FormCount24M, CollectorTitles.FormCount24M, CollectorNames.Forms24MPrefix, 24),
        Make.FormCountAll(CollectorNames.FormCount12M, CollectorTitles.FormCount12M, CollectorNames.Forms12MPrefix, 12),
        Make.FormCountAll(CollectorNames.FormCount6M, CollectorTitles.FormCount6M, CollectorNames.Forms6MPrefix, 6),
        Make.FormCountAll(CollectorNames.FormCount3M, CollectorTitles.FormCount3M, CollectorNames.Forms3MPrefix, 3),
    ];

    private static IReadOnlyList<ICollector> CreateAlwaysAfterFormCollectors() =>
    [
        // QuickStat.Collectors.pas:423. The only registration whose name is a bare literal, and the
        // only always-on one inside AddCollectorsHardCoded - it sits outside all five gate blocks.
        Make.VarSetNumeric(CollectorNames.Size, CollectorTitles.Anthropometrics, ItemSets.HeightWeightBmi),
    ];
}
