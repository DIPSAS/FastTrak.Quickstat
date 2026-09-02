unit EPR.QA.Collector.Names;

interface

{$REGION 'Captions'}

resourcestring
  { Shared }
  { General titles }
  StrTitleDemographicsAge = '^ Alder';
  StrTitleDemographicsSex = '^ Kjønn';
  StrTitleDemographicsYob = '^ Fødselsår';
  StrTitleDemographicsYod = '^ Dødsår';
  StrTitleDemographicsMob = '^ Fødselmåned';
  StrTitleDemographicsPostCode = '^ Postnummer';
  StrTitleStudyCenter = '^ Institusjon / sted';
  StrTitleStudyGroup = '^ Gruppe / avdeling nå';
  StrTitleStudyGroupDeath = '^ Gruppe / avdeling ved død';
  StrTitleStudyCenterDeath = '^ Institusjon / sted ved død';
  StrTitleStudyStatus = '^ Statuskode';
  StrTitleAntropometrics = 'Antropometri: Høyde og vekt';

  { General }
  StrTitleFormCount24m = 'Skjema: Antall siste 24 mnd per type';
  StrTitleFormCount12m = 'Skjema: Antall siste 12 mnd per type';
  StrTitleFormCount6m = 'Skjema: Antall siste 6 mnd per type';
  StrTitleFormCount3m = 'Skjema: Antall siste 3 mnd per type';
  StrTitleFormFrequencies = 'Skjema: Antall totalt per type';

  { NDV }
  StrTitleVarsetNdvAntropometry = 'NDV: Høyde og vekt';
  StrTitleVarsetNdvBasicData = 'NDV: Basisdata';
  StrTitleVarsetNdvBloodPressure = 'NDV: Blodtrykk';
  StrTitleVarsetNdvSmoking = 'NDV: Røyking';
  StrTitleLabsetNdvCore = 'NDV: Labdata';

  { BDR }
  StrTitleLabsetBdrCore = 'BDJ: Labdata';
  StrTitleLabsetBdrHbA1c = 'BDJ: HbA1c (siste)';
  StrTitleLabsetBdrHbA1cQuarters = 'BDJ: HbA1c siste 4 kvartaler';
  StrTitleBdrComorbidity = 'BDJ: Komorbiditet';
  StrTitleBdrDiagnose = 'BDJ: Diagnose';

  { Diabetes collector captions }
  StrTitleVarsetNdvTreatment = 'Diabetes: Behandling';
  StrTitleVarsetNdvCompilcations = 'Diabetes: Komplikasjoner';
  StrTitleVarsetInsulin = 'Diabetes: Insulindosering';
  StrTitleVarsetHypoglycemia = 'Diabetes: Hypoglykemi';
  StrTitleVarsetNdvDiagnose = 'Diabetes: Diagnose';
  StrTitleVarsetNdvExercise = 'Diabetes: Mosjon';
  StrTitleVarsetNdvSocial = 'Diabetes: Sosialt';
  StrTitleVarsetInsulinPump = 'Diabetes: CGM og Pumpetype';

  { GBD }
  StrTitleVarSetAgeWeightDays = 'GBD: Tid siden siste veiing';
  StrTitleLabsetNutrition = 'GBD: Ernæringsrelaterte labdata';
  StrTitleVarsetNutrition = 'GBD: Ernæringsdata';
  StrTitleVarsetDementia = 'GBD: Demens';
  StrTitleVarsetAntropometry = 'GBD: Høyde og vekt';
  StrTitleVarsetFallRisk = 'GBD: Fallrisiko';
  StrTitleVarsetHeartFailure = 'Hjertesvikt';
  StrTitleVarsetWarfarin = 'INR og mål';
  StrTitleGbdTvangsvedtak = 'GBD: Aktivt tvangsvedtak';
  StrTitleGbdInnleggelser12m = 'GBD: Innleggelser siste 12 mnd';
  StrTitleGbdLmg6m = 'GBD: Skjema "Legemiddelgjennomgang" siste 6 mnd (kompletthet)';
  StrTitleGbdBeslutninger6m = 'GBD: Skjema "Beslutninger" siste 6 mnd (kompletthet)';
  StrTitleVarsetGbdScores = 'GBD: Viktigste scores';
  StrTitleVarsetGbdBloodPressure = 'GBD: Blodtrykk fra kurve';
  StrTitleGbdFormLege3m = 'GBD: Legenotater siste 3 mnd';
  StrTitleVarSetPrimaryContactDays = 'GBD: Primærkontakt registrert';
  StrTitleVarSetAgeMNAPart1Days = 'GBD: Tid siden MNA del 1 utfylt';
  StrTitleGbdWeight2m = 'GBD: Vekt fra siste 2 mnd';
  StrTitleGbdSbp2m = 'GBD: Blodtrykk fra siste 2 mnd';
  StrTitleGbdFlacker12m = 'GBD: Flacker-Kiely siste 12 mnd';
  StrTitleGbdHulten3m = 'GBD: Hulten siste 3 mnd';
  StrTitleGbdQualid6m = 'GBD: Qualid siste 6 mnd';
  StrTitleGbdKdv6m = 'GBD: KDV siste 6 mnd';
  StrTitleGbdBarthel6m = 'GBD: Barthel ADL-Indeks siste 6 mnd';
  StrTitleGbdStratify6m = 'GBD: Stratify fallrisiko siste 6 mnd';
  StrTitleGbdMna6m = 'GBD: MNA ernæringsvurdering siste 6 mnd';
  StrTitleGbdAntiHypertensivesLowBp = 'GBD: Blodtrykk < 120 og blodtrykksbehandling';
  StrTitleGbdLowBp = 'GBD: Blodtrykk < 120 (siste)';
  StrTitleGbdAceLowGFR = 'GBD: ACE/A2 og GFR < 35';
  StrTitleGbdMetforminLowGFR = 'GBD: Metformin og GFR < 50 ';
  StrTitleGbdFlackerDeath = 'GBD: Flacker-Kiely og levedager';

  { Diabetes }
  StrTitleVarsetDiabetes = 'Diabetes';
  StrTitleVarsetSmoking = 'Røyking';
  StrTitleVarsetDiabetesBasic = 'Basisdata';

  { Templates for form collectors }
  StrTitleFormAgeTemplate = 'Skjema-alder: %s (%s)';
  StrTitleFormDataTemplate = 'Skjema-data: %s (%s)';

  { Diagose related collector captions }
  StrTitleDiagnoseAll1 = 'Diagnoser: Spesifisert med 1 tegn';
  StrTitleDiagnoseAll2 = 'Diagnoser: Spesifisert med 2 tegn';
  StrTitleDiagnoseAll3 = 'Diagnoser: Spesifisert med 3 tegn';
  StrTitleDiagnoseAll4 = 'Diagnoser: Spesifisert med 4 tegn';
  StrTitleDiagnoseAll5 = 'Diagnoser: Spesifisert med 5 tegn';
  StrTitleDiagnoseMissingE11 = 'Diagnose: Antidiabetika uten diabetesdiagnose';

  StrTitleDiagnoseCancer = 'Diagnoser: C - Kreft';
  StrTitleDiagnoseThyroid = 'Diagnoser: E0 - Tyreoidea-sykdommer';
  StrTitleDiagnoseDiabetes = 'Diagnoser: E1[014] - Diabetes Mellitus ';
  StrTitleDiagnoseEndocrine = 'Diagnoser: E[23] - Andre endokrine lidelser )';
  StrTitleDiagnoseMetabolic = 'Diagnoser: E[789] - Metabolske forstyrrelser';
  StrTitleDiagnoseStroke = 'Diagnoser: I6[01234 - Hjerneslag';
  StrTitleDiagnoseDementia = 'Diagnoser: F0[123]+G03 - Demens + Alzheimer';
  StrTitleDiagnosePsychiatry = 'Diagnoser: F[123456789]  - Psykisk lidelser';
  StrTitleDiagnoseHypertension = 'Diagnoser: I1[012345] - Hypertensjon';
  StrTitleDiagnoseIschemia = 'Diagnoser: I2[012345] - Iskemisk hjertesykdom';
  StrTitleDiagnoseAtrialFibrillation = 'Diagnoser: I48 - Atrieflimmer/flutter';

  { Short fragments that go into TXT_LAB_SET }
  StrTitleLabsetHigh = 'Labdata: Alle med høy konfidens';
  StrTitleLabsetMedium = 'Labdata: Alle med middels konfidens';
  StrTitleLabsetLow = 'Labdata: Alle med lav konfidens';
  StrTitleLabCount3m = 'Labdata: Antall prøver siste 3 mnd';
  StrTitleLabCount6m = 'Labdata: Antall prøver siste 6 mnd';
  StrTitleLabCount12m = 'Labdata: Antall prøver siste 12 mnd';
  StrTitleLabCount24m = 'Labdata: Antall prøver siste 24 mnd (2 år)';
  StrTitleLabCount60m = 'Labdata: Antall prøver siste 60 mnd (5 år)';
  StrTitleLabsetHeartFailure = 'Hjertesviktrelaterte labdata';
  StrTitleLabsetTemplate = 'Labdata: %s (siste)';
  StrTitleLabsetAnemia = 'Anemi';
  StrTitleLabsetDigitalis = 'Digitalis';
  StrTitleLabsetGeriatry = 'GBD: Sentrale labdata (siste)';
  StrTitleLabsetGlucose = 'Glukose';
  StrTitleLabsetHyperparatyreoidism = 'Hyperparatyreoidisme';
  StrTitleLabsetInr = 'INR fra labarket';
  StrTitleLabsetInterleukins = 'Interleukiner';
  StrTitleVarsetINR = 'INR';
  StrTitleLabsetKidney = 'Nyrefunksjon';
  StrTitleLabsetLipids = 'Lipider';
  StrTitleLabsetLiver = 'Leverstatus';
  StrTitleLabsetThyroid = 'Tyreoidea';
  StrTitleLabsetCrp = 'CRP';

  { Form titles }
  StrTitleFormWarfarin = 'Marevanskjema';
  StrTitleFormHulten = 'Hultén';
  StrTitleFormQualid = 'Livskvalitet';
  StrTitleFormKdv = 'Demensvurdering';
  StrTitleFormBarthel = 'Barthel';
  StrTitleFormStratify = 'Stratify';
  StrTitleFormBeslutninger = 'Beslutninger';
  StrTitleFormLmg = 'Legemiddelgjennomgang';
  StrTitleFormNEWS2 = 'NEWS2';
  StrTitleFormMatkort = 'Matkort';

  { Drug with simple ATC group selection }
  TXT_DRUG_A10 = 'Medisiner: A10 - Antidiabetika';
  TXT_DRUG_A10BA02 = 'Medisiner: A10BA02 - Metformin alene';
  TXT_DRUG_A11EA = 'Medisiner: A11EA - Vitamin B-kompleks';
  TXT_DRUG_B01AA03 = 'Medisiner: B01AA03 - Warfarin';
  TXT_DRUG_B01AF = 'Medisiner: BO1AF - DOAK';
  TXT_DRUG_B03BA = 'Medisiner: B03BA - Vitamin B12';
  TXT_DRUG_B03BA01 = 'Medisiner: B03BA01 - Cyanokoblamin';
  TXT_DRUG_B03BA03 = 'Medisiner: B03BA03 - Hydroksykobalamin';
  TXT_DRUG_C0x23789 = 'Medisiner: C0[23789] - Antihypertensiva vidt definert';
  TXT_DRUG_C01A = 'Medisiner: C01A - Hjerteglykosider';
  TXT_DRUG_C02 = 'Medisiner: C02 - Antihypertensiva';
  TXT_DRUG_C03 = 'Medisiner: C03 - Diuretika';
  TXT_DRUG_C07 = 'Medisiner: C07 - Betablokkere';
  TXT_DRUG_C08 = 'Medisiner: C08 - Kalsiumkanalblokkere/CCB';
  TXT_DRUG_C08D = 'Medisiner: C08D - CCB med kardiale effekter';
  TXT_DRUG_C09 = 'Medisiner: C09 - Renin/Angiotensin systemet';
  TXT_DRUG_C10 = 'Medisiner: C10 - Lipidsenkende';
  TXT_DRUG_M01A = 'Medisiner: M01A - NSAID';
  TXT_DRUG_N04BA = 'Medisiner: N04BA - Antiparkinsonmidler';

  { Faste medisiner }
  TXT_DRUG_N02A = 'Medisiner: N02A - Opioider';
  TXT_DRUG_N02B = 'Medisiner: N02B - Analgetika/antipyretika';
  TXT_DRUG_N05A = 'Medisiner: N05A - Antipsykotika';
  TXT_DRUG_N05B = 'Medisiner: N05B - Anxiolytika';
  TXT_DRUG_N05C = 'Medisiner: N05C - Hypnotika/sedativa';
  TXT_DRUG_N06A = 'Medisiner: N06A - Antidepressiva';
  TXT_DRUG_N06D = 'Medisiner: N06D - Antidemensmidler';

  { Custom lists, can not be expressed with simple patterns }
  StrTitleDrugNorGeP = 'Medisin: NorGeP avvik';
  StrTitleDrugMetformin = 'Medisin: Metformin inkl. kombinasjoner';
  StrTitleDrugCountTreatType = 'Medisin: Antall per behandlingstype';
  StrTitleDrugCountNoAtc = 'Medisin: Antall uten ATC-kode';
  StrTitleDrugCountGroup = 'Medisin: Antall på utvalgte ATC-grupper';
  StrTitleDrugAntibioticResistance = 'Antibiotika: Resistendrivende';
  StrTitleDrugAntibioticIntermediate = 'Antibiotika: Intermediære';
  StrTitleDrugAntibioticRecommended = 'Antibiotika: Anbefalte';
  StrTitleDrugAntibioticMetenamine = 'Antibiotika: Metenamin / Hiprex';

  StrTitleDrugStrongAnticholinergics = 'Medisin: Sterke antikolinergika (AB)';
  StrTitleDrugStrongAnticholinergicsN05A = 'Medisin: N05A - Nevroleptika med sterk antikolinerg effekt (AB)';

  { Drug interactions }
  StrTitleDruidCountPerLevel = 'Interaksjoner: Antall per nivå';
  StrTitleDruidSpecific = 'Interaksjoner: Spesifisert i detalj';

{$ENDREGION}
{$REGION 'Prefixes'}

const
  { General prefixes }
  GEN_PREFIX_DRUG_VS_DIAGNOSE = 'RXDX.';
  GEN_PREFIX_DRUID            = 'DRUID.';

  { Variable prefix }
  VAR_PREFIX_DRUG_COUNT  = 'ATCn_';
  VAR_PREFIX_DRUG        = 'ATC_';
  VAR_PREFIX_DRUG_FAST   = 'ATCF_';
  VAR_PREFIX_DRUG_TREAT  = 'TREATn_';
  VAR_PREFIX_DRUG_SPAN   = 'DRUG_';
  VAR_PREFIX_DRUG_NorGeP = 'NorGeP';
  VAR_PREFIX_LAST2M      = 'LAST2M.';
  VAR_PREFIX_LAST3M      = 'LAST3M.';
  VAR_PREFIX_LAST6M      = 'LAST6M.';
  VAR_PREFIX_LAST12M     = 'LAST12M.';
  VAR_PREFIX_ITEMAGE     = 'ITEMAGE.';
  VAR_PREFIX_OVERTREAT   = 'OVERTREAT.';
  VAR_PREFIX_LAST_BELOW  = 'LASTUNDER.';

  { Collector Prefixes }
  PREFIX_FORMAGE_COLLECTOR  = 'FORMAGE.';
  PREFIX_PATIENT            = 'PATIENT.';
  PREFIX_DRUG_COLLECTOR     = 'DRUG.';
  PREFIX_DRUGFAST_COLLECTOR = 'DRUGFAST.';
  PREFIX_DRUGNEED_COLLECTOR = 'DRUGNEED.';
  PREFIX_LAB_COLLECTOR      = 'LAB.';
  PREFIX_LAB_VARIABLE       = '';
  PREFIX_DIAGNOSE_COLLECTOR = 'DX.';
  PREFIX_DIAGNOSE_COUNT     = 'DXC.';
  PREFIX_NDV_COLLECTOR      = 'NDV.';
  PREFIX_GBD                = 'GBD.';
  PREFIX_FORM               = 'FORM.';
  PREFIX_FORMS_COLLECTOR    = 'FORMS.';
  PREFIX_FORMS3M_COLLECTOR  = 'FORMS3M.';
  PREFIX_FORMS6M_COLLECTOR  = 'FORMS6M.';
  PREFIX_FORMS12M_COLLECTOR = 'FORMS12M.';
  PREFIX_FORMS24M_COLLECTOR = 'FORMS24M.';
  PREFIX_STUDY              = 'STUDY.';
  PREFIX_FLACKER_KIELY      = 'FK.';

{$ENDREGION}
{$REGION 'Demographics collectors'}
  { Demographics column names }
  VAR_AGE = 'AGE';
  VAR_YOB = 'YOB';
  VAR_YOD = 'YOD';
  VAR_MOB = 'MOB';
  VAR_SEX = 'SEX';
  VAR_ZIP = 'ZIP';

  { Collector names }
  QS_PATIENT_AGE  = PREFIX_PATIENT + VAR_AGE;
  QS_PATIENT_MOB  = PREFIX_PATIENT + VAR_MOB;
  QS_PATIENT_YOB  = PREFIX_PATIENT + VAR_YOB;
  QS_PATIENT_YOD  = PREFIX_PATIENT + VAR_YOD;
  QS_PATIENT_SEX  = PREFIX_PATIENT + VAR_SEX;
  QS_PATIENT_ZIP  = PREFIX_PATIENT + VAR_ZIP;
  QS_STUDY_STATUS = PREFIX_STUDY + 'STATUS';
  QS_STUDY_GROUP  = PREFIX_STUDY + 'GROUP';
  QS_STUDY_CENTER = PREFIX_STUDY + 'CENTER';

  QS_STUDY_GROUP_DEATH  = PREFIX_STUDY + 'GROUP_DEATH';
  QS_STUDY_CENTER_DEATH = PREFIX_STUDY + 'CENTER_DEATH';

{$ENDREGION}
{$REGION 'Labdata collectors'}
  { Shared LabGroup names }
  LAB_HIGH          = 'TRUST3';
  LAB_MEDIUM        = 'TRUST2';
  LAB_LOW           = 'TRUST1';
  LAB_ANEMIA        = 'ANEMIA';
  LAB_CRP           = 'CRP';
  LAB_NDV_CORE      = 'NDVCORE';
  LAB_DIABETES      = 'DIABETES';
  LAB_DIGITALIS     = 'DIGITALIS';
  LAB_GERIATRIC     = 'GERIATRIC';
  LAB_GLUCOSE       = 'GLUCOSE';
  LAB_HEART_FAILURE = 'HEART_FAILURE';
  LAB_INTERLEUKINS  = 'INTERLEUKINS';
  LAB_HYPERPARA     = 'HYPERPARA';
  LAB_INR           = 'INR';
  LAB_KIDNEY        = 'KIDNEY';
  LAB_LIPIDS        = 'LIPIDS';
  LAB_LIVER         = 'LIVER';
  LAB_NUTRITION     = 'NUTRITION';
  LAB_THYROID       = 'THYROID';

  { Lab collectors }
  QST_LAB_DIABETESw     = PREFIX_LAB_COLLECTOR + LAB_DIABETES;
  QST_LAB_HIGH          = PREFIX_LAB_COLLECTOR + LAB_HIGH;
  QST_LAB_MEDIUM        = PREFIX_LAB_COLLECTOR + LAB_MEDIUM;
  QST_LAB_LOW           = PREFIX_LAB_COLLECTOR + LAB_LOW;
  QST_LAB_COUNT_3M      = PREFIX_LAB_COLLECTOR + 'COUNT_3M';
  QST_LAB_COUNT_6M      = PREFIX_LAB_COLLECTOR + 'COUNT_6M';
  QST_LAB_COUNT_12M     = PREFIX_LAB_COLLECTOR + 'COUNT_12M';
  QST_LAB_COUNT_24M     = PREFIX_LAB_COLLECTOR + 'COUNT_24M';
  QST_LAB_COUNT_60M     = PREFIX_LAB_COLLECTOR + 'COUNT_60M';
  QST_LAB_ANEMIA        = PREFIX_LAB_COLLECTOR + LAB_ANEMIA;
  QST_LAB_CRP           = PREFIX_LAB_COLLECTOR + LAB_CRP;
  QST_LAB_DIABETES      = PREFIX_LAB_COLLECTOR + LAB_DIABETES;
  QST_LAB_DIGITALIS     = PREFIX_LAB_COLLECTOR + LAB_DIGITALIS;
  QST_LAB_GERIATRIC     = PREFIX_LAB_COLLECTOR + LAB_GERIATRIC;
  QST_LAB_GLUCOSE       = PREFIX_LAB_COLLECTOR + LAB_GLUCOSE;
  QST_LAB_HEART_FAILURE = PREFIX_LAB_COLLECTOR + LAB_HEART_FAILURE;
  QST_LAB_INTERLEUKINS  = PREFIX_LAB_COLLECTOR + LAB_INTERLEUKINS;
  QST_LAB_HYPERPARA     = PREFIX_LAB_COLLECTOR + LAB_HYPERPARA;
  QST_LAB_INR           = PREFIX_LAB_COLLECTOR + LAB_INR;
  QST_LAB_KIDNEY        = PREFIX_LAB_COLLECTOR + LAB_KIDNEY;
  QST_LAB_LIPIDS        = PREFIX_LAB_COLLECTOR + LAB_LIPIDS;
  QST_LAB_LIVER         = PREFIX_LAB_COLLECTOR + LAB_LIVER;
  QST_LAB_NUTRITION     = PREFIX_LAB_COLLECTOR + LAB_NUTRITION;
  QST_LAB_THYROID       = PREFIX_LAB_COLLECTOR + LAB_THYROID;
  QST_LAB_NDV_CORE      = PREFIX_LAB_COLLECTOR + LAB_NDV_CORE;

{$ENDREGION}
{$REGION 'Drug Collectors'}
  { Drug collectors }
  QS_DRUG_C02  = PREFIX_DRUG_COLLECTOR + 'C02';
  QS_DRUG_C03  = PREFIX_DRUG_COLLECTOR + 'C03';
  QS_DRUG_C07  = PREFIX_DRUG_COLLECTOR + 'C07';
  QS_DRUG_C08  = PREFIX_DRUG_COLLECTOR + 'C08';
  QS_DRUG_C08D = PREFIX_DRUG_COLLECTOR + 'C08D';
  QS_DRUG_C09  = PREFIX_DRUG_COLLECTOR + 'C09';
  QS_DRUG_C10  = PREFIX_DRUG_COLLECTOR + 'C10';
  QS_DRUG_M01A = PREFIX_DRUG_COLLECTOR + 'M01A';

  QS_DRUG_J01XX05 = PREFIX_DRUG_COLLECTOR + 'J01XX05';

  { Custom drug collector variable names }
  VAR_ANTICHOLIN_AB  = 'ANTICHOLIN_AB';
  VAR_ANTICHOLIN_N05 = 'ANTICHOLIN_N05';
  VAR_METFORMIN      = 'METFORMIN';

  { Custom drug collectors }
  { Collector names }
  { Collector names }
  QS_DRUG_COUNT                 = PREFIX_DRUG_COLLECTOR + 'COUNT';
  QS_DRUG_COUNT_NOATC           = PREFIX_DRUG_COLLECTOR + 'NOATC';
  QS_DRUG_COUNT_GROUP           = PREFIX_DRUG_COLLECTOR + 'GROUPCOUNT';
  QS_DRUG_ANTIBIOTIC_RESISTANCE = PREFIX_DRUG_COLLECTOR + 'RESISTANCE_DRIVING';
  QS_DRUG_ANTIBIOTIC_INTERMEDIATE = PREFIX_DRUG_COLLECTOR + 'INTERMEDIATE';
  QS_DRUG_ANTIBIOTIC_RECOMMENDED= PREFIX_DRUG_COLLECTOR + 'RECOMMENDED';
  QS_DRUG_METFORMIN             = PREFIX_DRUG_COLLECTOR + VAR_METFORMIN;
  QS_DRUG_ANTICHOLIN_AB         = PREFIX_DRUG_COLLECTOR + VAR_ANTICHOLIN_AB;
  QS_DRUG_ANTICHOLIN_N05        = PREFIX_DRUG_COLLECTOR + VAR_ANTICHOLIN_N05;
  QS_DRUG_NorGeP                = PREFIX_DRUG_COLLECTOR + 'NorGEP';

  { DRUID Collectors }
  QS_DRUID_SPECIFIC = GEN_PREFIX_DRUID + 'SPECIFIED';
  QS_DRUID_COUNT    = GEN_PREFIX_DRUID + 'COUNT';

{$ENDREGION}
{$REGION 'GBD Collectors'}
  { GBD Item collectors }
  QS_GBD_SCORES                   = PREFIX_GBD + 'SCORES';
  QS_GBD_MEASURES                 = PREFIX_GBD + 'ANTROPOMETRI';
  QS_GBD_DEMENTIA                 = PREFIX_GBD + 'MMS';
  QS_GBD_NUTRITION                = PREFIX_GBD + 'NUTRITION';
  QS_GBD_FALLS                    = PREFIX_GBD + 'STRATIFY';
  QS_GBD_BP                       = PREFIX_GBD + 'BP';
  QS_GBD_DIABETES_BASE            = PREFIX_GBD + 'DIABETES_BASE';
  QS_GBD_HEART_FAILURE            = PREFIX_GBD + 'HEART_FAILURE';
  QS_GBD_SMOKING                  = PREFIX_GBD + 'SMOKING';
  QS_GBD_INR                      = PREFIX_GBD + 'INR';
  QS_GBD_TVANGSVEDTAK             = PREFIX_GBD + 'AKTIV_TVANG';
  QS_GBD_PRIMARY_CONTACT          = PREFIX_GBD + 'VARAGE.8420';
  QS_GBD_ANTIHYPERTENSIVES_LOW_BP = PREFIX_GBD + 'ANTIHT_LOW_BP';
  QS_GBD_LOW_BP                   = PREFIX_GBD + 'LOW_BP';
  QS_GBD_C09_GFR                  = PREFIX_GBD + 'C09_GFR';
  QS_GBD_METFORMIN_GFR            = PREFIX_GBD + 'METFORMIN_GFR';

  { GBD Form names }
  FORM_NAME_BARTHEL         = 'BARTHEL';
  FORM_NAME_KDV             = 'KDV';
  FORM_NAME_FLACKER_KIELY   = 'FLACKER_KIELY';
  FORM_NAME_HULTEN          = 'HULTEN';
  FORM_NAME_LMG             = 'LMG';
  FORM_NAME_MNA             = 'MNA';
  FORM_NAME_NEWS2           = 'NEWS2';
  FORM_NAME_QUALID          = 'QUALID';
  FORM_NAME_STRATIFY        = 'STRATIFY';
  FORM_NAME_MATKORT         = 'GBD_MATKORTv2';
  FORM_NAME_BESLUTNINGER    = 'GBD_BESLUTNINGER';
  FORM_NAME_MAREVAN         = 'GBD_MAREVAN';
  FORM_NAME_GBD_INNLEGGELSE = 'GBD_INNLEGGELSE';
  FORM_NAME_GBD_BESLUTNIGER = 'GBD_BESLUTNINGER';

  { GBD Custom collectors }
  QS_GBD_FORM_MAREVAN    = PREFIX_FORM + FORM_NAME_MAREVAN;
  QS_GBD_INNLEGGELSE_12M = PREFIX_FORMS12M_COLLECTOR + FORM_NAME_GBD_INNLEGGELSE;
  QS_GBD_WEIGHT_2M       = PREFIX_GBD + 'WEIGHT_2M';
  QS_GBD_SBP_2M          = PREFIX_GBD + 'SBP_2M';
  QS_GBD_FLACKER_12M     = PREFIX_GBD + 'FLACKER_12M';
  QS_GBD_HULTEN_3M       = PREFIX_GBD + FORM_NAME_HULTEN + '_3M';
  QS_GBD_QUALID_6M       = PREFIX_GBD + FORM_NAME_QUALID + '_6M';
  QS_GBD_KDV_6M          = PREFIX_GBD + FORM_NAME_KDV + '_6M';
  QS_GBD_BARTHEL_6M      = PREFIX_GBD + FORM_NAME_BARTHEL + '_6M';
  QS_GBD_STRATIFY_6M     = PREFIX_GBD + FORM_NAME_STRATIFY + '_6M';
  QS_GBD_MNA_6M          = PREFIX_GBD + FORM_NAME_MNA + '_6M';
  QS_GBD_LMG_6M          = PREFIX_GBD + FORM_NAME_LMG + '_6M';
  QS_GBD_BESLUTNINGER_6M = PREFIX_GBD + FORM_NAME_BESLUTNINGER + '_6M';
  QS_GBD_FLACKER_DEATH   = PREFIX_GBD + 'FLACKER_DEATH';

  { GBD ItemAge collectors }
  QS_WEIGHT_DAYS       = PREFIX_GBD + 'WEIGHT.DAYS';
  QS_ITEMAGE_MNA_PART1 = PREFIX_GBD + 'MNA_PART1';

  { GBD Formage collectors }
  QS_FORMAGE_GBD_BARTHEL      = PREFIX_FORMAGE_COLLECTOR + FORM_NAME_BARTHEL;
  QS_FORMAGE_GBD_KDV          = PREFIX_FORMAGE_COLLECTOR + FORM_NAME_KDV;
  QS_FORMAGE_GBD_FLACKERKIELY = PREFIX_FORMAGE_COLLECTOR + FORM_NAME_FLACKER_KIELY;
  QS_FORMAGE_GBD_BESLUTNINGER = PREFIX_FORMAGE_COLLECTOR + FORM_NAME_BESLUTNINGER;
  QS_FORMAGE_GBD_MATKORT      = PREFIX_FORMAGE_COLLECTOR + FORM_NAME_MATKORT;
  QS_FORMAGE_GBD_HULTEN       = PREFIX_FORMAGE_COLLECTOR + FORM_NAME_HULTEN;
  QS_FORMAGE_GBD_LMG          = PREFIX_FORMAGE_COLLECTOR + FORM_NAME_LMG;
  QS_FORMAGE_GBD_NEWS2        = PREFIX_FORMAGE_COLLECTOR + FORM_NAME_NEWS2;
  QS_FORMAGE_GBD_QUALID       = PREFIX_FORMAGE_COLLECTOR + FORM_NAME_QUALID;
  QS_FORMAGE_GBD_STRATIFY     = PREFIX_FORMAGE_COLLECTOR + FORM_NAME_STRATIFY;
  QS_FORMAGE_GBD_MAREVAN      = PREFIX_FORMAGE_COLLECTOR + FORM_NAME_MAREVAN;

  { GBD formset collector }
  QS_GBD_FORM_LEGE3M = PREFIX_FORMS3M_COLLECTOR + 'LEGEALLE';

{$ENDREGION}
{$REGION 'NDV Collectors'}
  { NDV QuickStat Collectors }
  QS_NDV_BP            = PREFIX_NDV_COLLECTOR + 'BP';
  QS_NDV_SMOKING       = PREFIX_NDV_COLLECTOR + 'SMOKING';
  QS_NDV_ANTROPOMETRY  = PREFIX_NDV_COLLECTOR + 'ANTROPOMETRY';
  QS_NDV_DIAGNOSE      = PREFIX_NDV_COLLECTOR + 'DIAGNOSE';
  QS_NDV_TREATMENT     = PREFIX_NDV_COLLECTOR + 'TREATMENT';
  QS_NDV_COMPLICATIONS = PREFIX_NDV_COLLECTOR + 'COMPLICATIONS';
  QS_NDV_INSULIN       = PREFIX_NDV_COLLECTOR + 'INSULIN';
  QS_NDV_HYPOGLYCEMIA  = PREFIX_NDV_COLLECTOR + 'HYPOGLYCEMIA';
  QS_NDV_EXERCISE      = PREFIX_NDV_COLLECTOR + 'EXERCISE';
  QS_NDV_SOCIAL        = PREFIX_NDV_COLLECTOR + 'SOCIAL';

{$ENDREGION}
{$REGION 'Diagnose Collectors'}
  { Diagnose Collectors }
  QS_DIAGNOSE_ALL1        = PREFIX_DIAGNOSE_COLLECTOR + 'ALL1';
  QS_DIAGNOSE_ALL2        = PREFIX_DIAGNOSE_COLLECTOR + 'ALL2';
  QS_DIAGNOSE_ALL3        = PREFIX_DIAGNOSE_COLLECTOR + 'ALL3';
  QS_DIAGNOSE_ALL4        = PREFIX_DIAGNOSE_COLLECTOR + 'ALL4';
  QS_DIAGNOSE_ALL5        = PREFIX_DIAGNOSE_COLLECTOR + 'ALL5';
  QS_DIAGNOSE_MISSING_E11 = GEN_PREFIX_DRUG_VS_DIAGNOSE + 'E1xA10';
{$ENDREGION}
{$REGION 'Other collectors'}
  { Custom form collectors }
  QS_FORM_FREQUENCY = PREFIX_FORMS_COLLECTOR + 'FREQUENCY';
  QS_FORM_COUNT3M   = PREFIX_FORMS3M_COLLECTOR + 'FREQUENCY';
  QS_FORM_COUNT6M   = PREFIX_FORMS6M_COLLECTOR + 'FREQUENCY';
  QS_FORM_COUNT12M  = PREFIX_FORMS12M_COLLECTOR + 'FREQUENCY';
  QS_FORM_COUNT24M  = PREFIX_FORMS24M_COLLECTOR + 'FREQUENCY';
{$ENDREGION}
{$REGION 'ROAS collectors'}
  QS_ROAS_GWAS_BG      = 'ROAS.GWAS.BG';
  QS_ROAS_GWAS_AB      = 'ROAS.GWAS.AB';
  QS_ROAS_GWAS_AB_APS1 = 'ROAS.GWAS.AB.APS1';
  QS_ROAS_POI_ORD      = 'ROAS.POI.ORD';
  QS_ROAS_POI_QN       = 'ROAS.POI.QN';
  QS_ROAS_BASE         = 'ROAS.BASE';
{$ENDREGION}
{$REGION 'DOGFOOD collectors'}
  QS_DOGFOOD_DATABASE_VERSION = 'DOGFOOD.DATABASE.VERSION';
{$ENDREGION}
{$REGION 'Barnediabetes collectors'}
  PREFIX_BDR                 = 'BDR';
  QS_PUMPE_VARSET            = 'BDR.INSPUMP';
  QS_BDR_DIAGNOSE            = 'BDR.DIAGNOSE';
  QS_BDR_DIAGNOSE_YEAR       = 'BDR.DIAGNOSEYEAR';
  QST_LAB_BDR_HBA1C          = 'BDR.HBA1C';
  QST_LAB_BDR_CORE           = 'BDR.LABDDATA';
  QST_BDR_COMORBID           = 'BDR.COMORBID';
  QST_LAB_BDR_HBA1C_QUARTERS = 'BDR.HBA1C_QUARTERS';
{$ENDREGION}

implementation

end.
