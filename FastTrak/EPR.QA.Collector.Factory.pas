unit EPR.QA.Collector.Factory;

interface

uses
  EPR.QA.PointFactory,
  EPR.QA.Collector.Base,
  Emetra.Classes.Business,
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces,
  System.SysUtils;

type
  TCollectorFactory = class( TBusiness )
  strict private
    fSQL: ISQL;
    fDatapointFactory: TDatapointFactory;
  public
    constructor Create( AFactory: TDatapointFactory; ASQL: ISQL; ALog: ILog ); reintroduce;
    function CreateCollector( const ACollectorName: string ): TDataCollector;
  end;

  EUnknownCollector = class( EArgumentException );

implementation

uses
  EPR.QA.SQL,
  EPR.QA.Definitions,
  EPR.QA.Collector.Standard,
  EPR.QA.Collector.Names,
  EPR.QA.Collector.Drug,
  EPR.QA.Collector.Demographics,
  EPR.QA.Collector.LabData,
  EPR.QA.Collector.VarSet;

{ TCollectorFactor }

constructor TCollectorFactory.Create( AFactory: TDatapointFactory; ASQL: ISQL; ALog: ILog );
begin
  inherited Create( ALog );
  fSQL := ASQL;
  fDatapointFactory := AFactory;
end;

function TCollectorFactory.CreateCollector( const ACollectorName: string ): TDataCollector;
begin
  { General Collectors }
  if ACollectorName = QS_PATIENT_AGE then
    Result := TAgeCollector.Create( QS_PATIENT_AGE, StrTitleDemographicsAge, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_PATIENT_YOB then
    Result := TYOBCollector.Create( QS_PATIENT_YOB, StrTitleDemographicsYob, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_PATIENT_YOD then
    Result := TYODCollector.Create( QS_PATIENT_YOD, StrTitleDemographicsYod, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_PATIENT_MOB then
    Result := TMOBCollector.Create( QS_PATIENT_MOB, StrTitleDemographicsMob, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_PATIENT_SEX then
    Result := TGenderCollector.Create( QS_PATIENT_SEX, StrTitleDemographicsSex, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_PATIENT_ZIP then
    Result := TPostCodeCollector.Create( QS_PATIENT_ZIP, StrTitleDemographicsPostCode, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_STUDY_STATUS then
    Result := TStatusCollector.Create( QS_STUDY_STATUS, StrTitleStudyStatus, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_STUDY_CENTER then
    Result := TCenterCollector.Create( QS_STUDY_CENTER, StrTitleStudyCenter, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_STUDY_GROUP then
    Result := TGroupCollector.Create( QS_STUDY_GROUP, StrTitleStudyGroup, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_STUDY_GROUP_DEATH then
    Result := TGroupAtDeathCollector.Create( QS_STUDY_GROUP_DEATH, StrTitleStudyGroupDeath, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_STUDY_CENTER_DEATH then
    Result := TCenterAtDeathCollector.Create( QS_STUDY_CENTER_DEATH, StrTitleStudyCenterDeath, fDatapointFactory, fSQL, Log )
    { GBD collectors }
  else if ACollectorName = QS_GBD_FORM_MAREVAN then
    Result := TFormDataCollector.Create( QS_GBD_FORM_MAREVAN, StrTitleFormWarfarin, '', fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_MEASURES then
    Result := TVarSetCollector.CreateForNumeric( QS_GBD_MEASURES, StrTitleVarsetAntropometry, SET_HEIGHT_WEIGHT_BMI, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_NUTRITION then
    Result := TVarSetCollector.CreateForNumeric( QS_GBD_NUTRITION, StrTitleVarsetNutrition, SET_GBD_NUTRITION, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_ITEMAGE_MNA_PART1 then
    Result := TVarSetAgeCollector.Create( QS_ITEMAGE_MNA_PART1, StrTitleVarSetAgeMNAPart1Days, SET_MNA_PART1, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_WEIGHT_DAYS then
    Result := TVarSetAgeCollector.Create( QS_WEIGHT_DAYS, StrTitleVarSetAgeWeightDays, SET_WEIGHT, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_PRIMARY_CONTACT then
    Result := TVarSetCollector.CreateForText( QS_GBD_PRIMARY_CONTACT, StrTitleVarSetPrimaryContactDays, SET_GBD_PRIMARY_CONTACT, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_DEMENTIA then
    Result := TVarSetCollector.CreateForNumeric( QS_GBD_DEMENTIA, StrTitleVarsetDementia, SET_GBD_DEMENTIA, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_HEART_FAILURE then
    Result := TVarSetCollector.CreateForNumeric( QS_GBD_HEART_FAILURE, StrTitleVarsetHeartFailure, SET_GBD_HEART_FAILURE, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_FALLS then
    Result := TVarSetCollector.CreateForNumeric( QS_GBD_FALLS, StrTitleVarsetFallRisk, SET_GBD_FALLS, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_INR then
    Result := TVarSetCollector.CreateForNumeric( QS_GBD_INR, StrTitleVarsetWarfarin, SET_GBD_INR, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_DIABETES_BASE then
    Result := TVarSetCollector.CreateForNumeric( QS_GBD_DIABETES_BASE, StrTitleVarsetDiabetesBasic, SET_NDV_DIAGNOSE, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_SMOKING then
    Result := TVarSetCollector.CreateForNumeric( QS_GBD_SMOKING, StrTitleVarsetSmoking, SET_SMOKING, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_BP then
    Result := TVarSetCollector.CreateForNumeric( QS_GBD_BP, StrTitleVarsetGbdBloodPressure, SET_GBD_BP, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_SCORES then
    Result := TVarSetCollector.CreateForNumeric( QS_GBD_SCORES, StrTitleVarsetGbdScores, SET_GBD_SCORES, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_INNLEGGELSE_12M then
    Result := TCustomDataCollector.Create( QS_GBD_INNLEGGELSE_12M, StrTitleGbdInnleggelser12m, PREFIX_FORMS12M_COLLECTOR, SpRecentFormCountSingle( FORM_NAME_GBD_INNLEGGELSE, 12 ), fDatapointFactory,
      fSQL, GlobalLog )
  else if ACollectorName = QS_GBD_FLACKER_DEATH then
    Result := TCustomDataCollector.Create( QS_GBD_FLACKER_DEATH, StrTitleGbdFlackerDeath, PREFIX_FLACKER_KIELY, SpFlackerKileyDeath, fDatapointFactory, fSQL, GlobalLog )
  else if ACollectorName = QS_GBD_LMG_6M then
    Result := TCustomDataCollector.Create( QS_GBD_LMG_6M, StrTitleGbdLmg6m, PREFIX_FORMS6M_COLLECTOR, SpRecentFormCompleteness( FORM_NAME_LMG, 6 ), fDatapointFactory, fSQL, GlobalLog )
  else if ACollectorName = QS_GBD_BESLUTNINGER_6M then
    Result := TCustomDataCollector.Create( QS_GBD_BESLUTNINGER_6M, StrTitleGbdBeslutninger6m, PREFIX_FORMS6M_COLLECTOR, SpRecentFormCompleteness( FORM_NAME_BESLUTNINGER, 6 ), fDatapointFactory, fSQL,
      GlobalLog )
  else if ACollectorName = QS_GBD_WEIGHT_2M then
    Result := TCustomDataCollector.Create( QS_GBD_WEIGHT_2M, StrTitleGbdWeight2m, VAR_PREFIX_LAST2M, SpRecentQuantityPresent( 3224, 2 ), fDatapointFactory, fSQL, GlobalLog )
  else if ACollectorName = QS_GBD_SBP_2M then
    Result := TCustomDataCollector.Create( QS_GBD_WEIGHT_2M, StrTitleGbdSbp2m, VAR_PREFIX_LAST2M, SpRecentQuantityPresent( 3556, 2 ), fDatapointFactory, fSQL, GlobalLog )
  else if ACollectorName = QS_GBD_FLACKER_12M then
    Result := TCustomDataCollector.Create( QS_GBD_FLACKER_12M, StrTitleGbdFlacker12m, VAR_PREFIX_LAST12M, SpRecentQuantityPresent( 1128, 12 ), fDatapointFactory, fSQL, GlobalLog )
  else if ACollectorName = QS_GBD_HULTEN_3M then
    Result := TCustomDataCollector.Create( QS_GBD_HULTEN_3M, StrTitleGbdHulten3m, VAR_PREFIX_LAST3M, SpRecentQuantityPresent( 4234, 3 ), fDatapointFactory, fSQL, GlobalLog )
  else if ACollectorName = QS_GBD_QUALID_6M then
    Result := TCustomDataCollector.Create( QS_GBD_QUALID_6M, StrTitleGbdQualid6m, VAR_PREFIX_LAST6M, SpRecentQuantityPresent( 5827, 6 ), fDatapointFactory, fSQL, GlobalLog )
  else if ACollectorName = QS_GBD_KDV_6M then
    Result := TCustomDataCollector.Create( QS_GBD_KDV_6M, StrTitleGbdKdv6m, VAR_PREFIX_LAST6M, SpRecentQuantityPresent( 1685, 6 ), fDatapointFactory, fSQL, GlobalLog )
  else if ACollectorName = QS_GBD_BARTHEL_6M then
    Result := TCustomDataCollector.Create( QS_GBD_BARTHEL_6M, StrTitleGbdBarthel6m, VAR_PREFIX_LAST6M, SpRecentQuantityPresent( 4342, 6 ), fDatapointFactory, fSQL, GlobalLog )
  else if ACollectorName = QS_GBD_STRATIFY_6M then
    Result := TCustomDataCollector.Create( QS_GBD_STRATIFY_6M, StrTitleGbdStratify6m, VAR_PREFIX_LAST6M, SpRecentQuantityPresent( 9257, 6 ), fDatapointFactory, fSQL, GlobalLog )
  else if ACollectorName = QS_GBD_MNA_6M then
    Result := TCustomDataCollector.Create( QS_GBD_MNA_6M, StrTitleGbdMna6m, VAR_PREFIX_LAST6M, SpRecentQuantityPresent( 4771, 6 ), fDatapointFactory, fSQL, GlobalLog )
  else if ACollectorName = QS_GBD_FORM_LEGE3M then
    Result := TCustomDataCollector.Create( QS_GBD_FORM_LEGE3M, StrTitleGbdFormLege3m, PREFIX_FORMS_COLLECTOR, SpRecentFormGroupLege3m, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_TVANGSVEDTAK then
    Result := TCustomDataCollector.Create( QS_GBD_TVANGSVEDTAK, StrTitleGbdTvangsvedtak, EmptyStr, QRY_TVANGSVEDTAK, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_ANTIHYPERTENSIVES_LOW_BP then
    Result := TCustomDataCollector.Create( QS_GBD_ANTIHYPERTENSIVES_LOW_BP, StrTitleGbdAntiHypertensivesLowBp, VAR_PREFIX_OVERTREAT, SpDrugHypertensionWithLowBp( 120 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_LOW_BP then
    Result := TCustomDataCollector.Create( QS_GBD_LOW_BP, StrTitleGbdLowBp, VAR_PREFIX_LAST_BELOW, SpSnapshotQuantityIfBelowThreshold( 3556, 120.0 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_C09_GFR then
    Result := TCustomDataCollector.Create( QS_GBD_C09_GFR, StrTitleGbdAceLowGFR, VAR_PREFIX_LAST_BELOW, SpDrugAndRenalFunction( 'C09%', 35 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_GBD_METFORMIN_GFR then
    Result := TCustomDataCollector.Create( QS_GBD_METFORMIN_GFR, StrTitleGbdMetforminLowGFR, VAR_PREFIX_LAST_BELOW, SpDrugAndRenalFunction( 'A10BA%', 50 ), fDatapointFactory, fSQL, Log )
    { NDV collectors }
  else if ACollectorName = QS_NDV_BP then
    Result := TVarSetCollector.CreateForNumeric( QS_NDV_BP, StrTitleVarsetNdvBloodPressure, SET_NDV_BP, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_NDV_DIAGNOSE then
    Result := TVarSetCollector.CreateForNumeric( QS_NDV_DIAGNOSE, StrTitleVarsetNdvBasicData, SET_NDV_DIAGNOSE, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_NDV_SMOKING then
    Result := TVarSetCollector.CreateForNumeric( QS_NDV_SMOKING, StrTitleVarsetNdvSmoking, SET_SMOKING, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_NDV_ANTROPOMETRY then
    Result := TVarSetCollector.CreateForNumeric( QS_NDV_ANTROPOMETRY, StrTitleVarsetAntropometry, SET_HEIGHT_WEIGHT_BMI, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_NDV_TREATMENT then
    Result := TVarSetCollector.CreateForNumeric( QS_NDV_TREATMENT, StrTitleVarsetNdvTreatment, SET_NDV_TREATMENT, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_NDV_COMPLICATIONS then
    Result := TVarSetCollector.CreateForNumeric( QS_NDV_COMPLICATIONS, StrTitleVarsetNdvCompilcations, SET_NDV_COMPLICATIONS, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_NDV_INSULIN then
    Result := TVarSetCollector.CreateForNumeric( QS_NDV_INSULIN, StrTitleVarsetInsulin, SET_NDV_INSULIN, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_NDV_HYPOGLYCEMIA then
    Result := TVarSetCollector.CreateForNumeric( QS_NDV_HYPOGLYCEMIA, StrTitleVarsetHypoglycemia, SET_NDV_HYPOGLYCEMIA, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_NDV_EXERCISE then
    Result := TVarSetCollector.CreateForNumeric( QS_NDV_EXERCISE, StrTitleVarsetNdvExercise, SET_NDV_EXERCISE, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_NDV_SOCIAL then
    Result := TVarSetCollector.CreateForNumeric( QS_NDV_SOCIAL, StrTitleVarsetNdvSocial, SET_NDV_SOCIAL, fDatapointFactory, fSQL, Log )
    { Barnediabetes collectors }
  else if ACollectorName = QS_PUMPE_VARSET then
    Result := TVarSetCollector.CreateForEnum( QS_PUMPE_VARSET, StrTitleVarsetInsulinPump, SET_INSULINPUMPE, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_BDR_COMORBID then
    Result := TVarSetCollector.CreateForEnum( QST_BDR_COMORBID, StrTitleBdrComorbidity, SET_BDR_COMORBID, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_BDR_DIAGNOSE then
    Result := TVarSetCollector.CreateForNumeric( QS_BDR_DIAGNOSE, StrTitleBdrDiagnose, SET_BDR_DIAGNOSE, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_BDR_DIAGNOSE_YEAR then
    Result := TVarSetCollector.CreateForNumeric( QS_BDR_DIAGNOSE_YEAR, 'Diagnoseår', SET_BDR_DIAGNOSE_YEAR, fDatapointFactory, fSQL, Log )
    { Drug collectors }
  else if ACollectorName = QS_DRUG_C03 then
    Result := TDrugCollector.CreateBasic( TXT_DRUG_C03, ATC_C03, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_C07 then
    Result := TDrugCollector.CreateBasic( TXT_DRUG_C07, ATC_C07, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_C08D then
    Result := TDrugCollector.CreateBasic( TXT_DRUG_C08D, ATC_C08D, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_C09 then
    Result := TDrugCollector.CreateBasic( TXT_DRUG_C09, ATC_C09, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_C10 then
    Result := TDrugCollector.CreateBasic( TXT_DRUG_C10, ATC_C10, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_M01A then
    Result := TDrugCollector.CreateBasic( TXT_DRUG_M01A, ATC_M01A, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_COUNT_GROUP then
    Result := TCustomDataCollector.Create( QS_DRUG_COUNT_GROUP, StrTitleDrugCountGroup, VAR_PREFIX_DRUG_COUNT, QRY_DRUGCOUNT_BY_ATCGROUP, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_COUNT_NOATC then
    Result := TCustomDataCollector.Create( QS_DRUG_COUNT_NOATC, StrTitleDrugCountNoAtc, VAR_PREFIX_DRUG_COUNT, SpDrugCountNoAtc, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_COUNT then
    Result := TCustomDataCollector.Create( QS_DRUG_COUNT, StrTitleDrugCountTreatType, VAR_PREFIX_DRUG_TREAT, QRY_DRUGCOUNT_BY_TYPE, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_METFORMIN then
    Result := TCustomDataCollector.Create( QS_DRUG_METFORMIN, StrTitleDrugMetformin, VAR_PREFIX_DRUG_SPAN, QRY_DRUGSET_METFORMIN, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_ANTICHOLIN_N05 then
    Result := TCustomDataCollector.Create( QS_DRUG_ANTICHOLIN_N05, StrTitleDrugStrongAnticholinergicsN05A, VAR_PREFIX_DRUG_SPAN, QRY_DRUGSET_ANTICHOLIN_N05A, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_ANTICHOLIN_AB then
    Result := TCustomDataCollector.Create( QS_DRUG_ANTICHOLIN_AB, StrTitleDrugStrongAnticholinergics, VAR_PREFIX_DRUG_SPAN, QRY_DRUGSET_ANTICHOLIN_AB, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_ANTIBIOTIC_RESISTANCE then
    Result := TCustomDataCollector.Create( QS_DRUG_ANTIBIOTIC_RESISTANCE, StrTitleDrugAntibioticResistance, VAR_PREFIX_DRUG_SPAN, SpDrugsetAntibioticResistance, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_ANTIBIOTIC_RECOMMENDED then
    Result := TCustomDataCollector.Create( QS_DRUG_ANTIBIOTIC_RECOMMENDED, StrTitleDrugAntibioticRecommended, VAR_PREFIX_DRUG_SPAN, SpDrugsetAntibioticRecommended, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_ANTIBIOTIC_INTERMEDIATE then
    Result := TCustomDataCollector.Create( QS_DRUG_ANTIBIOTIC_INTERMEDIATE, StrTitleDrugAntibioticIntermediate, VAR_PREFIX_DRUG_SPAN, SpDrugsetAntibioticIntermediate, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_J01XX05  then
    Result := TDrugCollector.CreateBasic( StrTitleDrugAntibioticMetenamine, ATC_J01XX05, fDatapointFactory, fSQL, Log )
    { Labset collectors }
  else if ACollectorName = QST_LAB_HIGH then
    Result := TLabHighTrustCollector.Create( QST_LAB_HIGH, StrTitleLabsetHigh, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_MEDIUM then
    Result := TLabMediumTrustCollector.Create( QST_LAB_MEDIUM, StrTitleLabsetMedium, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_LOW then
    Result := TLabLowTrustCollector.Create( QST_LAB_MEDIUM, StrTitleLabsetLow, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_COUNT_3M then
    Result := TCustomDataCollector.Create( QST_LAB_COUNT_3M, StrTitleLabCount3m, EmptyStr, SpRecentLabdataPresent( 3 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_COUNT_6M then
    Result := TCustomDataCollector.Create( QST_LAB_COUNT_6M, StrTitleLabCount6m, EmptyStr, SpRecentLabdataPresent( 6 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_COUNT_12M then
    Result := TCustomDataCollector.Create( QST_LAB_COUNT_12M, StrTitleLabCount12m, EmptyStr, SpRecentLabdataPresent( 12 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_COUNT_24M then
    Result := TCustomDataCollector.Create( QST_LAB_COUNT_24M, StrTitleLabCount24m, EmptyStr, SpRecentLabdataPresent( 24 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_COUNT_60M then
    Result := TCustomDataCollector.Create( QST_LAB_COUNT_60M, StrTitleLabCount60m, EmptyStr, SpRecentLabdataPresent( 60 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_NUTRITION then
    Result := TLabSetCollector.Create( QST_LAB_NUTRITION, StrTitleLabsetNutrition, LABCLASSES_NUTRITION, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_HEART_FAILURE then
    Result := TLabSetCollector.Create( QST_LAB_HEART_FAILURE, StrTitleLabsetHeartFailure, LABCLASSES_HEART_FAILURE, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_INR then
    Result := TLabSetCollector.Create( QST_LAB_INR, StrTitleLabsetInr, LABCLASSES_INR, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_CRP then
    Result := TLabSetCollector.Create( QST_LAB_CRP, StrTitleLabsetCrp, LABCLASSES_CRP, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_DIABETES then
    Result := TLabSetCollector.Create( QST_LAB_DIABETES, StrTitleVarsetDiabetes, LABCLASSES_DIABETES, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_INTERLEUKINS then
    Result := TLabSetCollector.Create( QST_LAB_INTERLEUKINS, StrTitleLabsetInterleukins, LABCLASSES_INTERLEUKINS, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_NDV_CORE then
    Result := TLabSetCollector.Create( QST_LAB_NDV_CORE, StrTitleLabsetNdvCore, LABCLASSES_DIABETES_NDV, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_BDR_CORE then
    Result := TLabSetCollector.Create( QST_LAB_BDR_CORE, StrTitleLabsetBdrCore, LABCLASSES_DIABETES_BDR, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_BDR_HBA1C then
    Result := TLabSetCollector.Create( QST_LAB_BDR_HBA1C, StrTitleLabsetBdrHbA1c, [1058], fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_BDR_HBA1C_QUARTERS then
    Result := TCustomDataCollector.Create( QST_LAB_BDR_HBA1C_QUARTERS, StrTitleLabsetBdrHbA1cQuarters, EmptyStr, SpSnapshotLabQuarters( 1058 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_ANEMIA then
    Result := TLabSetCollector.Create( QST_LAB_ANEMIA, StrTitleLabsetAnemia, LABCLASSES_ANEMIA, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_LIPIDS then
    Result := TLabSetCollector.Create( QST_LAB_LIPIDS, StrTitleLabsetLipids, LABCLASSES_LIPIDS, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_DIGITALIS then
    Result := TLabSetCollector.Create( QST_LAB_DIGITALIS, StrTitleLabsetDigitalis, LABCLASSES_DIGITALIS, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_LIVER then
    Result := TLabSetCollector.Create( QST_LAB_LIVER, StrTitleLabsetLiver, LABCLASSES_LIVER, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_THYROID then
    Result := TLabSetCollector.Create( QST_LAB_THYROID, StrTitleLabsetThyroid, LABCLASSES_THYROID, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_GLUCOSE then
    Result := TLabSetCollector.Create( QST_LAB_GLUCOSE, StrTitleLabsetGlucose, LABCLASSES_GLUCOSE, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_HYPERPARA then
    Result := TLabSetCollector.Create( QST_LAB_HYPERPARA, StrTitleLabsetHyperparatyreoidism, LABCLASSES_HYPERPARA, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QST_LAB_GERIATRIC then
    Result := TLabSetCollector.Create( QST_LAB_GERIATRIC, StrTitleLabsetGeriatry, LABCLASSES_GERIATRIC, fDatapointFactory, fSQL, Log )
    { Form age collectors }
  else if ACollectorName = QS_FORMAGE_GBD_MAREVAN then
    Result := TFormAgeCollector.Create( QS_GBD_FORM_MAREVAN, StrTitleFormWarfarin, FORM_NAME_MAREVAN, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_FORMAGE_GBD_BARTHEL then
    Result := TFormAgeCollector.Create( QS_FORMAGE_GBD_BARTHEL, StrTitleFormBarthel, FORM_NAME_BARTHEL, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_FORMAGE_GBD_KDV then
    Result := TFormAgeCollector.Create( QS_FORMAGE_GBD_KDV, StrTitleFormKdv, FORM_NAME_KDV, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_FORMAGE_GBD_FLACKERKIELY then
    Result := TFormAgeCollector.Create( QS_FORMAGE_GBD_FLACKERKIELY, StrTitleFormStratify, FORM_NAME_FLACKER_KIELY, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_FORMAGE_GBD_BESLUTNINGER then
    Result := TFormAgeCollector.Create( QS_FORMAGE_GBD_BESLUTNINGER, StrTitleFormBeslutninger, FORM_NAME_BESLUTNINGER, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_FORMAGE_GBD_MATKORT then
    Result := TFormAgeCollector.Create( QS_FORMAGE_GBD_MATKORT, StrTitleFormMatkort, FORM_NAME_MATKORT, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_FORMAGE_GBD_HULTEN then
    Result := TFormAgeCollector.Create( QS_FORMAGE_GBD_HULTEN, StrTitleFormHulten, FORM_NAME_HULTEN, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_FORMAGE_GBD_LMG then
    Result := TFormAgeCollector.Create( QS_FORMAGE_GBD_LMG, StrTitleFormLmg, FORM_NAME_LMG, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_FORMAGE_GBD_NEWS2 then
    Result := TFormAgeCollector.Create( QS_FORMAGE_GBD_NEWS2, StrTitleFormNEWS2, FORM_NAME_NEWS2, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_FORMAGE_GBD_QUALID then
    Result := TFormAgeCollector.Create( QS_FORMAGE_GBD_QUALID, StrTitleFormQualid, FORM_NAME_QUALID, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_FORMAGE_GBD_STRATIFY then
    Result := TFormAgeCollector.Create( QS_FORMAGE_GBD_STRATIFY, StrTitleFormStratify, FORM_NAME_STRATIFY, fDatapointFactory, fSQL, Log )
    { Other custom form collectors }
  else if ACollectorName = QS_FORM_COUNT24M then
    Result := TCustomDataCollector.Create( QS_FORM_COUNT24M, StrTitleFormCount24m, PREFIX_FORMS24M_COLLECTOR, SpRecentFormCountAll( 24 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_FORM_COUNT12M then
    Result := TCustomDataCollector.Create( QS_FORM_COUNT12M, StrTitleFormCount12m, PREFIX_FORMS12M_COLLECTOR, SpRecentFormCountAll( 12 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_FORM_COUNT6M then
    Result := TCustomDataCollector.Create( QS_FORM_COUNT6M, StrTitleFormCount6m, PREFIX_FORMS6M_COLLECTOR, SpRecentFormCountAll( 6 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_FORM_COUNT3M then
    Result := TCustomDataCollector.Create( QS_FORM_COUNT3M, StrTitleFormCount3m, PREFIX_FORMS3M_COLLECTOR, SpRecentFormCountAll( 3 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUID_SPECIFIC then
    Result := TCustomDataCollector.Create( QS_DRUID_SPECIFIC, StrTitleDruidSpecific, '', SpDruidIndividualInteractions( 5 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUID_COUNT then
    Result := TCustomDataCollector.Create( QS_DRUID_COUNT, StrTitleDruidCountPerLevel, GEN_PREFIX_DRUID, SpDruidCountByLevel, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_NorGeP then
    Result := TCustomDataCollector.Create( QS_DRUG_NorGeP, StrTitleDrugNorGEP, VAR_PREFIX_DRUG_NorGeP, QRY_NORGEP, fDatapointFactory, fSQL, Log )
    { Diagnose collectors }
  else if ACollectorName = QS_DIAGNOSE_ALL1 then
    Result := TCustomDataCollector.Create( QS_DIAGNOSE_ALL1, StrTitleDiagnoseAll1, PREFIX_DIAGNOSE_COUNT, SpDiagnoseDetailsByLevel( 1 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DIAGNOSE_ALL2 then
    Result := TCustomDataCollector.Create( QS_DIAGNOSE_ALL2, StrTitleDiagnoseAll2, PREFIX_DIAGNOSE_COUNT, SpDiagnoseDetailsByLevel( 2 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DIAGNOSE_ALL3 then
    Result := TCustomDataCollector.Create( QS_DIAGNOSE_ALL3, StrTitleDiagnoseAll3, PREFIX_DIAGNOSE_COUNT, SpDiagnoseDetailsByLevel( 3 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DIAGNOSE_ALL4 then
    Result := TCustomDataCollector.Create( QS_DIAGNOSE_ALL4, StrTitleDiagnoseAll4, PREFIX_DIAGNOSE_COUNT, SpDiagnoseDetailsByLevel( 4 ), fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DIAGNOSE_ALL5 then
    Result := TCustomDataCollector.Create( QS_DIAGNOSE_ALL5, StrTitleDiagnoseAll5, PREFIX_DIAGNOSE_COUNT, SpDiagnoseDetailsByLevel( 5 ), fDatapointFactory, fSQL, Log )
    { Decision support }
  else if ACollectorName = QS_DIAGNOSE_MISSING_E11 then
    Result := TCustomDataCollector.Create( QS_DIAGNOSE_MISSING_E11, StrTitleDiagnoseMissingE11, GEN_PREFIX_DRUG_VS_DIAGNOSE, SpDrugWithoutDiagnose( 'A10_NOT_E1x01234', 'A10%', 'E1[01234]%' ),
      fDatapointFactory, fSQL, Log )
    { GWAS }
  else if ACollectorName = QS_ROAS_GWAS_BG then
    Result := TVarSetCollector.CreateForNumeric( QS_ROAS_GWAS_BG, 'GWAS Bakgrunn', SET_GWAS_BG, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_ROAS_GWAS_AB then
    Result := TVarSetMaxCollector.Create( QS_ROAS_GWAS_AB, 'GWAS Autoantistoffer', SET_GWAS_AUTOANTIBODY, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_ROAS_GWAS_AB_APS1 then
    Result := TVarSetMaxCollector.Create( QS_ROAS_GWAS_AB_APS1, 'GWAS APS-I spesfikk', SET_GWAS_APS1, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_ROAS_POI_ORD then
    Result := TVarSetCollector.CreateForNumeric( QS_ROAS_POI_ORD, 'POI Diagnoser', SET_POI_ORD, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_ROAS_POI_QN then
    Result := TVarSetCollector.CreateForNumeric( QS_ROAS_POI_QN, 'POI Diagnoseår', SET_POI_QN, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_ROAS_BASE then
    Result := TVarSetCollector.CreateForNumeric( QS_ROAS_BASE, 'Autommunitet', SET_ROAS_BASE, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DOGFOOD_DATABASE_VERSION then
    Result := TVarSetCollector.CreateForNumeric( QS_DOGFOOD_DATABASE_VERSION, 'Dogfood: Databaseversjoner', [3812, 5117], fDatapointFactory, fSQL, Log )
  else
    raise EUnknownCollector.CreateFmt( '%s: Unknown collector %s', [ClassName, ACollectorName] );
end;

end.
