namespace QuickStat.Collectors.Registry;

/// <summary>
/// Every collector name QuickStat registers, transcribed from
/// <c>EPR.QA.Collector.Names.pas</c>.
/// </summary>
/// <remarks>
/// <para>
/// Names are a <b>persistence format</b>: they are what
/// <see cref="QuickStat.Domain.Packages.PackagedSelection.CollectorNames"/> stores in
/// <c>Report.QuickStat.DataElements</c>, so changing one silently breaks every saved specification
/// that mentions it. The Delphi identifier is on every member so the two code bases stay greppable
/// against each other.
/// </para>
/// <para>
/// Only the names QuickStat actually registers are here. The 39 factory names it never registers -
/// BDJ / Barnediabetes, the <c>FORMAGE.*</c> family and several <c>QS_GBD_*</c> var-sets belonging
/// to <c>EPR.QA.Collection.Geriatri</c> and its siblings - are dropped by PORT-PLAN.md §7.1, and so
/// is the dead duplicate <c>QST_LAB_DIABETESw</c> (§7.2 item 5).
/// </para>
/// </remarks>
public static class CollectorNames
{
    // ---- Prefixes (EPR.QA.Collector.Names.pas, {$REGION 'Prefixes'}) --------------------------

    /// <summary><c>PREFIX_PATIENT</c>.</summary>
    public const string PatientPrefix = "PATIENT.";

    /// <summary><c>PREFIX_STUDY</c>.</summary>
    public const string StudyPrefix = "STUDY.";

    /// <summary><c>PREFIX_LAB_COLLECTOR</c>.</summary>
    public const string LabPrefix = "LAB.";

    /// <summary><c>PREFIX_DRUG_COLLECTOR</c>.</summary>
    public const string DrugPrefix = "DRUG.";

    /// <summary><c>GEN_PREFIX_DRUID</c>.</summary>
    public const string DruidPrefix = "DRUID.";

    /// <summary><c>GEN_PREFIX_DRUG_VS_DIAGNOSE</c>.</summary>
    public const string DrugVersusDiagnosePrefix = "RXDX.";

    /// <summary><c>PREFIX_DIAGNOSE_COLLECTOR</c>. Also the var prefix of the pattern collectors.</summary>
    public const string DiagnosePrefix = "DX.";

    /// <summary><c>PREFIX_DIAGNOSE_COUNT</c>. Var prefix only.</summary>
    public const string DiagnoseCountPrefix = "DXC.";

    /// <summary><c>PREFIX_NDV_COLLECTOR</c>.</summary>
    public const string NdvPrefix = "NDV.";

    /// <summary><c>PREFIX_GBD</c>.</summary>
    public const string GbdPrefix = "GBD.";

    /// <summary><c>PREFIX_FORM</c>. Both a name prefix and the form-instance var prefix.</summary>
    public const string FormPrefix = "FORM.";

    /// <summary><c>PREFIX_FORMS_COLLECTOR</c>.</summary>
    public const string FormsPrefix = "FORMS.";

    /// <summary><c>PREFIX_FORMS3M_COLLECTOR</c>.</summary>
    public const string Forms3MPrefix = "FORMS3M.";

    /// <summary><c>PREFIX_FORMS6M_COLLECTOR</c>.</summary>
    public const string Forms6MPrefix = "FORMS6M.";

    /// <summary><c>PREFIX_FORMS12M_COLLECTOR</c>.</summary>
    public const string Forms12MPrefix = "FORMS12M.";

    /// <summary><c>PREFIX_FORMS24M_COLLECTOR</c>.</summary>
    public const string Forms24MPrefix = "FORMS24M.";

    // ---- Variable-name prefixes --------------------------------------------------------------

    /// <summary><c>PREFIX_LAB_VARIABLE</c> - lab columns carry no prefix at all.</summary>
    public const string LabVariablePrefix = "";

    /// <summary><c>VAR_PREFIX_DRUG</c>.</summary>
    public const string DrugVariablePrefix = "ATC_";

    /// <summary><c>VAR_PREFIX_DRUG_COUNT</c>.</summary>
    public const string DrugCountVariablePrefix = "ATCn_";

    /// <summary><c>VAR_PREFIX_DRUG_TREAT</c>.</summary>
    public const string DrugTreatVariablePrefix = "TREATn_";

    /// <summary><c>VAR_PREFIX_DRUG_SPAN</c>.</summary>
    public const string DrugSetVariablePrefix = "DRUG_";

    /// <summary><c>VAR_PREFIX_DRUG_NorGeP</c> - note it does <b>not</b> end in a separator.</summary>
    public const string NorGePVariablePrefix = "NorGeP";

    /// <summary><c>VAR_PREFIX_LAST2M</c>.</summary>
    public const string Last2MVariablePrefix = "LAST2M.";

    /// <summary><c>VAR_PREFIX_LAST3M</c>.</summary>
    public const string Last3MVariablePrefix = "LAST3M.";

    /// <summary><c>VAR_PREFIX_LAST6M</c>.</summary>
    public const string Last6MVariablePrefix = "LAST6M.";

    /// <summary><c>VAR_PREFIX_LAST12M</c>.</summary>
    public const string Last12MVariablePrefix = "LAST12M.";

    /// <summary><c>VAR_PREFIX_OVERTREAT</c>.</summary>
    public const string OverTreatVariablePrefix = "OVERTREAT.";

    /// <summary><c>VAR_PREFIX_LAST_BELOW</c>.</summary>
    public const string LastBelowVariablePrefix = "LASTUNDER.";

    /// <summary><c>PREFIX_FLACKER_KIELY</c>.</summary>
    public const string FlackerKielyVariablePrefix = "FK.";

    /// <summary><c>ITEM_AGE_PREFIX</c> (<c>EPR.QA.Collector.VarSet.pas:41</c>).</summary>
    public const string ItemAgeVariablePrefix = "ITEMAGE.";

    /// <summary><c>ITEM_MAX_PREFIX</c> (<c>EPR.QA.Collector.VarSet.pas:40</c>).</summary>
    public const string ItemMaxVariablePrefix = "ITEMMAX.";

    /// <summary><c>FORM_AGE_PREFIX</c> (<c>EPR.QA.Collector.VarSet.pas:42</c>).</summary>
    public const string FormAgeVariablePrefix = "FORMAGE.";

    // ---- Demographics and study case ---------------------------------------------------------

    /// <summary><c>QS_PATIENT_AGE</c>.</summary>
    public const string PatientAge = PatientPrefix + "AGE";

    /// <summary><c>QS_PATIENT_SEX</c>.</summary>
    public const string PatientSex = PatientPrefix + "SEX";

    /// <summary><c>QS_PATIENT_YOB</c>.</summary>
    public const string PatientYearOfBirth = PatientPrefix + "YOB";

    /// <summary><c>QS_PATIENT_YOD</c>.</summary>
    public const string PatientYearOfDeath = PatientPrefix + "YOD";

    /// <summary><c>QS_PATIENT_MOB</c>.</summary>
    public const string PatientMonthOfBirth = PatientPrefix + "MOB";

    /// <summary><c>QS_PATIENT_ZIP</c>.</summary>
    public const string PatientPostCode = PatientPrefix + "ZIP";

    /// <summary><c>QS_STUDY_STATUS</c>.</summary>
    public const string StudyStatus = StudyPrefix + "STATUS";

    /// <summary><c>QS_STUDY_CENTER</c>.</summary>
    public const string StudyCenter = StudyPrefix + "CENTER";

    /// <summary><c>QS_STUDY_GROUP</c>.</summary>
    public const string StudyGroup = StudyPrefix + "GROUP";

    /// <summary><c>QS_STUDY_GROUP_DEATH</c>.</summary>
    public const string StudyGroupAtDeath = StudyPrefix + "GROUP_DEATH";

    /// <summary><c>QS_STUDY_CENTER_DEATH</c>.</summary>
    public const string StudyCenterAtDeath = StudyPrefix + "CENTER_DEATH";

    // ---- Form counting -----------------------------------------------------------------------

    /// <summary><c>QS_FORM_FREQUENCY</c>.</summary>
    public const string FormFrequency = FormsPrefix + "FREQUENCY";

    /// <summary><c>QS_FORM_COUNT3M</c>.</summary>
    public const string FormCount3M = Forms3MPrefix + "FREQUENCY";

    /// <summary><c>QS_FORM_COUNT6M</c>.</summary>
    public const string FormCount6M = Forms6MPrefix + "FREQUENCY";

    /// <summary><c>QS_FORM_COUNT12M</c>.</summary>
    public const string FormCount12M = Forms12MPrefix + "FREQUENCY";

    /// <summary><c>QS_FORM_COUNT24M</c>.</summary>
    public const string FormCount24M = Forms24MPrefix + "FREQUENCY";

    // ---- Lab ---------------------------------------------------------------------------------

    /// <summary><c>QST_LAB_KIDNEY</c>.</summary>
    public const string LabKidney = LabPrefix + "KIDNEY";

    /// <summary><c>QST_LAB_ANEMIA</c>.</summary>
    public const string LabAnemia = LabPrefix + "ANEMIA";

    /// <summary><c>QST_LAB_LIPIDS</c>.</summary>
    public const string LabLipids = LabPrefix + "LIPIDS";

    /// <summary><c>QST_LAB_DIGITALIS</c>.</summary>
    public const string LabDigitalis = LabPrefix + "DIGITALIS";

    /// <summary><c>QST_LAB_LIVER</c>.</summary>
    public const string LabLiver = LabPrefix + "LIVER";

    /// <summary><c>QST_LAB_THYROID</c>.</summary>
    public const string LabThyroid = LabPrefix + "THYROID";

    /// <summary><c>QST_LAB_GLUCOSE</c>.</summary>
    public const string LabGlucose = LabPrefix + "GLUCOSE";

    /// <summary><c>QST_LAB_INR</c>.</summary>
    public const string LabInr = LabPrefix + "INR";

    /// <summary><c>QST_LAB_HYPERPARA</c>.</summary>
    public const string LabHyperPara = LabPrefix + "HYPERPARA";

    /// <summary><c>QST_LAB_HEART_FAILURE</c>.</summary>
    public const string LabHeartFailure = LabPrefix + "HEART_FAILURE";

    /// <summary><c>QST_LAB_CRP</c>.</summary>
    public const string LabCrp = LabPrefix + "CRP";

    /// <summary><c>QST_LAB_HIGH</c>.</summary>
    public const string LabHighTrust = LabPrefix + "TRUST3";

    /// <summary><c>QST_LAB_MEDIUM</c>.</summary>
    public const string LabMediumTrust = LabPrefix + "TRUST2";

    /// <summary><c>QST_LAB_LOW</c>.</summary>
    /// <remarks>
    /// PORT-PLAN.md §7.2, collision 1. The Delphi factory builds the low-trust collector with
    /// <c>QST_LAB_MEDIUM</c> as its name (<c>EPR.QA.Collector.Factory.pas:209</c>), so today two
    /// collectors are registered as <c>LAB.TRUST2</c> and the second one is unreachable by name.
    /// The port registers it under this constant, which is what <c>QST_LAB_LOW</c> always was.
    /// </remarks>
    public const string LabLowTrust = LabPrefix + "TRUST1";

    /// <summary><c>QST_LAB_COUNT_3M</c>.</summary>
    public const string LabCount3M = LabPrefix + "COUNT_3M";

    /// <summary><c>QST_LAB_COUNT_6M</c>.</summary>
    public const string LabCount6M = LabPrefix + "COUNT_6M";

    /// <summary><c>QST_LAB_COUNT_12M</c>.</summary>
    public const string LabCount12M = LabPrefix + "COUNT_12M";

    /// <summary><c>QST_LAB_COUNT_24M</c>.</summary>
    public const string LabCount24M = LabPrefix + "COUNT_24M";

    /// <summary><c>QST_LAB_COUNT_60M</c>.</summary>
    public const string LabCount60M = LabPrefix + "COUNT_60M";

    /// <summary><c>QST_LAB_GERIATRIC</c>.</summary>
    public const string LabGeriatric = LabPrefix + "GERIATRIC";

    /// <summary><c>QST_LAB_DIABETES</c>.</summary>
    public const string LabDiabetes = LabPrefix + "DIABETES";

    // ---- Anthropometry -----------------------------------------------------------------------

    /// <summary>
    /// The literal <c>'SIZE'</c> - the only registration that does not go through a name constant
    /// (<c>QuickStat.Collectors.pas:423</c>).
    /// </summary>
    public const string Size = "SIZE";

    // ---- GBD ---------------------------------------------------------------------------------

    /// <summary><c>QS_WEIGHT_DAYS</c>.</summary>
    public const string GbdWeightDays = GbdPrefix + "WEIGHT.DAYS";

    /// <summary><c>QS_GBD_TVANGSVEDTAK</c>.</summary>
    public const string GbdTvangsvedtak = GbdPrefix + "AKTIV_TVANG";

    /// <summary><c>QS_GBD_INNLEGGELSE_12M</c>.</summary>
    public const string GbdAdmissions12M = Forms12MPrefix + FormNames.GbdInnleggelse;

    /// <summary><c>QS_GBD_FORM_LEGE3M</c>.</summary>
    public const string GbdDoctorNotes3M = Forms3MPrefix + "LEGEALLE";

    /// <summary><c>QS_GBD_SCORES</c>.</summary>
    public const string GbdScores = GbdPrefix + "SCORES";

    /// <summary><c>QS_GBD_BP</c>.</summary>
    public const string GbdBloodPressure = GbdPrefix + "BP";

    /// <summary><c>QS_GBD_PRIMARY_CONTACT</c>.</summary>
    public const string GbdPrimaryContact = GbdPrefix + "VARAGE.8420";

    /// <summary><c>QS_GBD_WEIGHT_2M</c>.</summary>
    public const string GbdWeight2M = GbdPrefix + "WEIGHT_2M";

    /// <summary><c>QS_GBD_SBP_2M</c>.</summary>
    /// <remarks>
    /// PORT-PLAN.md §7.2, collision 2. The Delphi factory builds this collector with
    /// <c>QS_GBD_WEIGHT_2M</c> as its name (<c>EPR.QA.Collector.Factory.pas:113</c>), duplicating
    /// the weight collector. The port registers it under this constant.
    /// </remarks>
    public const string GbdSystolic2M = GbdPrefix + "SBP_2M";

    /// <summary><c>QS_GBD_FLACKER_12M</c>.</summary>
    public const string GbdFlacker12M = GbdPrefix + "FLACKER_12M";

    /// <summary><c>QS_GBD_FLACKER_DEATH</c>.</summary>
    public const string GbdFlackerDeath = GbdPrefix + "FLACKER_DEATH";

    /// <summary><c>QS_GBD_HULTEN_3M</c>.</summary>
    public const string GbdHulten3M = GbdPrefix + FormNames.Hulten + "_3M";

    /// <summary><c>QS_GBD_QUALID_6M</c>.</summary>
    public const string GbdQualid6M = GbdPrefix + FormNames.Qualid + "_6M";

    /// <summary><c>QS_GBD_KDV_6M</c>.</summary>
    public const string GbdKdv6M = GbdPrefix + FormNames.Kdv + "_6M";

    /// <summary><c>QS_GBD_BARTHEL_6M</c>.</summary>
    public const string GbdBarthel6M = GbdPrefix + FormNames.Barthel + "_6M";

    /// <summary><c>QS_GBD_STRATIFY_6M</c>.</summary>
    public const string GbdStratify6M = GbdPrefix + FormNames.Stratify + "_6M";

    /// <summary><c>QS_GBD_MNA_6M</c>.</summary>
    public const string GbdMna6M = GbdPrefix + FormNames.Mna + "_6M";

    /// <summary><c>QS_GBD_ANTIHYPERTENSIVES_LOW_BP</c>.</summary>
    public const string GbdAntiHypertensivesLowBp = GbdPrefix + "ANTIHT_LOW_BP";

    /// <summary><c>QS_GBD_LOW_BP</c>.</summary>
    public const string GbdLowBp = GbdPrefix + "LOW_BP";

    /// <summary><c>QS_GBD_C09_GFR</c>.</summary>
    public const string GbdAceLowGfr = GbdPrefix + "C09_GFR";

    /// <summary><c>QS_GBD_METFORMIN_GFR</c>.</summary>
    public const string GbdMetforminLowGfr = GbdPrefix + "METFORMIN_GFR";

    /// <summary><c>QS_GBD_LMG_6M</c>.</summary>
    public const string GbdLmg6M = GbdPrefix + FormNames.Lmg + "_6M";

    /// <summary><c>QS_GBD_BESLUTNINGER_6M</c> - which resolves to <c>GBD.GBD_BESLUTNINGER_6M</c>.</summary>
    public const string GbdBeslutninger6M = GbdPrefix + FormNames.Beslutninger + "_6M";

    // ---- Diagnosis ---------------------------------------------------------------------------

    /// <summary><c>QS_DIAGNOSE_ALL1</c>.</summary>
    public const string DiagnoseAll1 = DiagnosePrefix + "ALL1";

    /// <summary><c>QS_DIAGNOSE_ALL2</c>.</summary>
    public const string DiagnoseAll2 = DiagnosePrefix + "ALL2";

    /// <summary><c>QS_DIAGNOSE_ALL3</c>.</summary>
    public const string DiagnoseAll3 = DiagnosePrefix + "ALL3";

    /// <summary><c>QS_DIAGNOSE_ALL4</c>.</summary>
    public const string DiagnoseAll4 = DiagnosePrefix + "ALL4";

    /// <summary><c>QS_DIAGNOSE_ALL5</c>.</summary>
    public const string DiagnoseAll5 = DiagnosePrefix + "ALL5";

    /// <summary><c>QS_DIAGNOSE_MISSING_E11</c>.</summary>
    public const string DiagnoseMissingE11 = DrugVersusDiagnosePrefix + "E1xA10";

    /// <summary><c>TDementiaCollector</c>'s hard-coded name (<c>EPR.QA.Collector.Diagnose.pas:46</c>).</summary>
    public const string DiagnoseDementia = DiagnosePrefix + "DEMENTIA";

    // ---- Drug --------------------------------------------------------------------------------

    /// <summary><c>QS_DRUID_COUNT</c>.</summary>
    public const string DruidCount = DruidPrefix + "COUNT";

    /// <summary><c>QS_DRUID_SPECIFIC</c>.</summary>
    public const string DruidSpecific = DruidPrefix + "SPECIFIED";

    /// <summary><c>QS_DRUG_COUNT_GROUP</c>.</summary>
    public const string DrugCountGroup = DrugPrefix + "GROUPCOUNT";

    /// <summary><c>QS_DRUG_COUNT_NOATC</c>.</summary>
    public const string DrugCountNoAtc = DrugPrefix + "NOATC";

    /// <summary><c>QS_DRUG_COUNT</c>.</summary>
    public const string DrugCount = DrugPrefix + "COUNT";

    /// <summary><c>QS_DRUG_METFORMIN</c>.</summary>
    public const string DrugMetformin = DrugPrefix + "METFORMIN";

    /// <summary><c>QS_DRUG_ANTICHOLIN_N05</c>.</summary>
    public const string DrugAnticholinergicN05 = DrugPrefix + "ANTICHOLIN_N05";

    /// <summary><c>QS_DRUG_ANTICHOLIN_AB</c>.</summary>
    public const string DrugAnticholinergicAb = DrugPrefix + "ANTICHOLIN_AB";

    /// <summary><c>QS_DRUG_ANTIBIOTIC_RESISTANCE</c>.</summary>
    public const string DrugAntibioticResistance = DrugPrefix + "RESISTANCE_DRIVING";

    /// <summary><c>QS_DRUG_NorGeP</c> - note the mixed case, which is part of the stored name.</summary>
    public const string DrugNorGeP = DrugPrefix + "NorGEP";

    // ---- NDV / diabetes ------------------------------------------------------------------------

    /// <summary><c>QS_NDV_DIAGNOSE</c>.</summary>
    public const string NdvDiagnose = NdvPrefix + "DIAGNOSE";

    /// <summary><c>QS_NDV_TREATMENT</c>.</summary>
    public const string NdvTreatment = NdvPrefix + "TREATMENT";

    /// <summary><c>QS_NDV_COMPLICATIONS</c>.</summary>
    public const string NdvComplications = NdvPrefix + "COMPLICATIONS";

    /// <summary><c>QS_NDV_INSULIN</c>.</summary>
    public const string NdvInsulin = NdvPrefix + "INSULIN";

    /// <summary><c>QS_NDV_HYPOGLYCEMIA</c>.</summary>
    public const string NdvHypoglycemia = NdvPrefix + "HYPOGLYCEMIA";

    /// <summary><c>QS_NDV_EXERCISE</c>.</summary>
    public const string NdvExercise = NdvPrefix + "EXERCISE";

    /// <summary><c>QS_NDV_SOCIAL</c>.</summary>
    public const string NdvSocial = NdvPrefix + "SOCIAL";

    // ---- ROAS / GWAS / DOGFOOD -----------------------------------------------------------------

    /// <summary><c>QS_ROAS_GWAS_BG</c>.</summary>
    public const string RoasGwasBackground = "ROAS.GWAS.BG";

    /// <summary><c>QS_ROAS_GWAS_AB</c>.</summary>
    public const string RoasGwasAutoAntibody = "ROAS.GWAS.AB";

    /// <summary><c>QS_ROAS_GWAS_AB_APS1</c>.</summary>
    public const string RoasGwasAps1 = "ROAS.GWAS.AB.APS1";

    /// <summary><c>QS_ROAS_POI_ORD</c>.</summary>
    public const string RoasPoiOrdinal = "ROAS.POI.ORD";

    /// <summary><c>QS_ROAS_POI_QN</c>.</summary>
    public const string RoasPoiQuantity = "ROAS.POI.QN";

    /// <summary><c>QS_DOGFOOD_DATABASE_VERSION</c>.</summary>
    public const string DogfoodDatabaseVersion = "DOGFOOD.DATABASE.VERSION";
}
