unit QuickStat.Collectors;

interface

uses
  QuickStat.Percentile,
  CRF.Study.Interfaces,
  {EPR.QA}
  EPR.QA.Definitions,
  EPR.QA.Collector.LabData,
  EPR.QA.Collector.Base,
  EPR.QA.Collector.Standard,
  EPR.QA.Collector.VarSet,
  EPR.QA.Collector.Drug,
  EPR.QA.Collector.Diagnose,
  EPR.QA.Collector.Factory,
  {Datapoints}
  EPR.QA.DataPoint,
  EPR.QA.DataPoint.VitalSigns,
  EPR.QA.DataPoint.Pharmacology,
  EPR.QA.DataPoint.Biochemistry,
  EPR.QA.DataPoint.HeartFailure,
  EPR.QA.DataPoint.Dogfood,
  EPR.QA.PointFactory,
  {VMR}
  VMR.Lab.Interfaces,
  {Logging}
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces,
  Emetra.Progress.Interfaces,
  {Standard}
  Generics.Collections, System.Classes;

type
  TQuickStatCollectors = class( TObject )
  strict private
    fLabColorDictionary: TColorDictionary;
    fLog: ILog;
    fSQL: ISQL;
    fStatus: IStatus;
    fStudyId: IStudyId;
    fDataCollectors: TDataCollectorList;
    fDatapointFactory: TDataPointFactory;
    fCollectorFactory: TCollectorFactory;
  private
    { Other members }
    procedure AddCollector( const ACollector: TDataCollector );
    procedure AddCollectorsBasic;
    procedure AddCollectorsHardCoded;
    procedure AddCollectorsDiagnose;
    procedure AddCollectorsStudySpecific;
    procedure AddCollectorsLabData;
    procedure AddCollectorsDrug;
    procedure RegisterLabPercentileColoring( const ALabClassId: integer; const AStrategy: TColorStrategy ); overload;
    procedure RegisterLabPercentileColoring( const ALabTest: TLabTest; const AStrategy: TColorStrategy ); overload;
    procedure SetColor( AColoredDatapoint: TColoredDatapoint );
    procedure RegisterLabColors;
    procedure RegisterCustomDatapoints;
  public
    { Initialization }
    constructor Create( ASQL: ISQL; AStatus: IStatus; ALog: ILog ); reintroduce;
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { Other members }
    procedure ProvideColor( AObject: TObject );
    procedure PrepareStudy( AStudyId: IStudyId );
    { Properties }
    property Collectors: TDataCollectorList read fDataCollectors;
  end;

implementation

uses
  EPR.QA.SQL,
  EPR.QA.Collector.Names,
  EPR.QA.Collector.Demographics,
  EPR.QA.Collection.Geriatri,
  System.RegularExpressions,
  {Standard}
  System.SysUtils;

resourcestring
  { Labset collectors }
  StrTitleLabsetDiabetes = 'Labdata: Viktigste ved diabetes';

  TXT_LOADING_COLLECTORS = 'Loading collectors';

{$REGION 'Initialization'}

constructor TQuickStatCollectors.Create( ASQL: ISQL; AStatus: IStatus; ALog: ILog );
begin
  inherited Create;
  fSQL := ASQL;
  fLog := ALog;
  fStatus := AStatus;
end;

procedure TQuickStatCollectors.AfterConstruction;
begin
  inherited;
  { Create lists and dictionaries }
  fLabColorDictionary := TColorDictionary.Create( fSQL, fLog );
  fDatapointFactory := TDataPointFactory.Create( TDatapoint );
  fDataCollectors := TDataCollectorList.Create;
  fCollectorFactory := TCollectorFactory.Create( fDatapointFactory, fSQL, fLog );
  { Initialize datapoint factories and color dictionaries }
  RegisterLabColors;
  RegisterCustomDatapoints;
  { Initialize data collectors }
end;

procedure TQuickStatCollectors.BeforeDestruction;
begin
  FreeAndNil( fCollectorFactory );
  FreeAndNil( fDataCollectors );
  FreeAndNil( fDatapointFactory );
  FreeAndNil( fLabColorDictionary );
  inherited;
end;

{$ENDREGION}

procedure TQuickStatCollectors.PrepareStudy( AStudyId: IStudyId );
begin
  fStatus.Info := TXT_LOADING_COLLECTORS;
  try
    fStudyId := AStudyId;
    fDataCollectors.Clear;
    AddCollector( TFormInstanceCollector.Create( QS_FORM_FREQUENCY, StrTitleFormFrequencies, fDatapointFactory, fSQL, GlobalLog ) );
    fLabColorDictionary.AfterLogin( fSQL );
    AddCollectorsBasic;
    AddCollectorsLabData;
    AddCollectorsStudySpecific;
    AddCollectorsHardCoded;
  finally
    fStatus.Done;
  end;
end;

procedure TQuickStatCollectors.SetColor( AColoredDatapoint: TColoredDatapoint );
var
  thisColoring: TColoring;
begin
  if fLabColorDictionary.TryGetValue( AColoredDatapoint.VarName, thisColoring ) then
    AColoredDatapoint.SetColor( thisColoring.GetColor( AColoredDatapoint.Value ) );
end;

procedure TQuickStatCollectors.ProvideColor( AObject: TObject );
begin
  if AObject.InheritsFrom( TColoredDatapoint ) then
    SetColor( AObject as TColoredDatapoint );
end;

procedure TQuickStatCollectors.RegisterCustomDatapoints;
begin
  with fDatapointFactory do
  begin
    RegisterDataPointClass( 'NPU01566', TCholDatapoint );
    RegisterDataPointClass( 'NPU01568', TLdlDatapoint );
    RegisterDataPointClass( 'NPU03835', THbA1cPercentDatapoint );
    RegisterDataPointClass( 'NPU27300', THbA1cMmolDatapoint );
    RegisterDataPointClass( 'NPU04786', TDigitoxinDatapoint );
    RegisterDataPointClass( 'NPU03429', TSodiumDatapoint );
    RegisterDataPointClass( 'NPU03230', TPotassiumDatapoint );
    RegisterDataPointClass( 'NOR05172', THemoglobinDatapoint );

    RegisterDataPointClass( 'SBP_UNSPEC', TSBPDatapoint );
    RegisterDataPointClass( 'DBP_UNSPEC', TDBPDatapoint );
    RegisterDataPointClass( 'SYSBP', TSBPDatapoint );
    RegisterDataPointClass( 'DIABP', TDBPDatapoint );
    RegisterDataPointClass( 'BMI', TBMIDatapoint );
    RegisterDataPointClass( 'PULSE_QUALITY', TPulseQualityDatapoint );
    RegisterDataPointClass( VAR_DB_VERSION, TDbVersionDatapoint );
    RegisterDataPointClass( VAR_SERVER_VERSION, TDbServerVersionDatapoint );
  end;
end;

procedure TQuickStatCollectors.RegisterLabPercentileColoring( const ALabTest: TLabTest; const AStrategy: TColorStrategy );
begin
  RegisterLabPercentileColoring( ord( ALabTest ), AStrategy );
end;

procedure TQuickStatCollectors.RegisterLabPercentileColoring( const ALabClassId: integer; const AStrategy: TColorStrategy );
begin
  fLabColorDictionary.Add( ALabClassId, AStrategy );
end;

procedure TQuickStatCollectors.RegisterLabColors;
begin
  { Configure coloring for labdata }
  RegisterLabPercentileColoring( ltALAT, csHighIsBadOnly );
  RegisterLabPercentileColoring( ltAlcalicPhosphatase, csHighIsBadOnly );
  RegisterLabPercentileColoring( ltASAT, csHighIsBadOnly );
  RegisterLabPercentileColoring( ltEVF, csHighAndLowIsBad );
  RegisterLabPercentileColoring( ltTPC, csHighAndLowIsBad );
  RegisterLabPercentileColoring( ltHGB, csHighAndLowIsBad );
  RegisterLabPercentileColoring( ltMCH, csHighAndLowIsBad );
  RegisterLabPercentileColoring( ltMCHC, csHighAndLowIsBad );
  RegisterLabPercentileColoring( ltMCV, csHighAndLowIsBad );
  RegisterLabPercentileColoring( ltCK, csHighIsBadOnly );
  RegisterLabPercentileColoring( ltCreatinine, csHighIsBadOnly );
  RegisterLabPercentileColoring( ltCRP, csHighIsBadOnly );
  RegisterLabPercentileColoring( ltINR, csHighIsBadOnly );
  RegisterLabPercentileColoring( ltESR, csHighIsBadOnly );
  RegisterLabPercentileColoring( ltEstGfr, csLowIsBadOnly );
  RegisterLabPercentileColoring( 51, csLowIsBadOnly ); { eGFR Cockgroft-Gault }
  RegisterLabPercentileColoring( 52, csLowIsBadOnly ); { eGFR MDRD }
  RegisterLabPercentileColoring( 995, csLowIsBadOnly ); { eGFR Cystatin C }
  RegisterLabPercentileColoring( 1075, csLowIsBadOnly ); { eGFR CKD-EPI }
  RegisterLabPercentileColoring( ltGammaGT, csHighIsBadOnly );
  RegisterLabPercentileColoring( ltProBNP_pMol, csHighIsBadLowIsGood );
  RegisterLabPercentileColoring( ltAlbumine, csHighAndLowIsBad );
  RegisterLabPercentileColoring( ltCalcium, csHighAndLowIsBad );
  RegisterLabPercentileColoring( ltChloride, csHighAndLowIsBad );
  RegisterLabPercentileColoring( ltIron, csHighAndLowIsBad );
  RegisterLabPercentileColoring( ltFerritine, csHighAndLowIsBad );
  RegisterLabPercentileColoring( ltPFolate, csLowIsBadOnly );
  RegisterLabPercentileColoring( ltFT4, csHighAndLowIsBad );
  RegisterLabPercentileColoring( ltPlasmaGlucose, csHighIsBadOnly );
  RegisterLabPercentileColoring( ltBloodGlucose, csHighIsBadOnly );
  RegisterLabPercentileColoring( ltKalium, csHighAndLowIsBad );
  RegisterLabPercentileColoring( ltNatrium, csHighAndLowIsBad );
  RegisterLabPercentileColoring( ltTSH, csHighAndLowIsBad );
  RegisterLabPercentileColoring( ltUrea, csHighIsBadLowIsGood );
  RegisterLabPercentileColoring( ltUrate, csHighIsBadLowIsGood );
end;

{$REGION 'Add collectors to factory'}

procedure TQuickStatCollectors.AddCollector( const ACollector: TDataCollector );
begin
  fDataCollectors.Add( ACollector );
end;

procedure TQuickStatCollectors.AddCollectorsBasic;
begin
  { Demographics }
  AddCollector( fCollectorFactory.CreateCollector( QS_PATIENT_AGE ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_PATIENT_SEX ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_PATIENT_YOB ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_PATIENT_YOD ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_PATIENT_MOB ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_PATIENT_ZIP ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_STUDY_STATUS ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_STUDY_CENTER ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_STUDY_GROUP ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_STUDY_GROUP_DEATH ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_STUDY_CENTER_DEATH ) );
  { Counting forms }
  AddCollector( fCollectorFactory.CreateCollector( QS_FORM_COUNT24M ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_FORM_COUNT12M ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_FORM_COUNT6M ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_FORM_COUNT3M ) );
end;

procedure TQuickStatCollectors.AddCollectorsDiagnose;
begin
  { Counting diagnoses }
  AddCollector( fCollectorFactory.CreateCollector( QS_DIAGNOSE_ALL1 ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DIAGNOSE_ALL2 ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DIAGNOSE_ALL3 ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DIAGNOSE_ALL4 ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DIAGNOSE_ALL5 ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DIAGNOSE_MISSING_E11 ) );
  { ICD-10 patterns }
  AddCollector( TDiagnoseCollector.Create( StrTitleDiagnoseCancer, 'C%', fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDiagnoseCollector.Create( StrTitleDiagnoseThyroid, 'E0%', fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDiagnoseCollector.Create( StrTitleDiagnoseDiabetes, 'E1[014]%', fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDiagnoseCollector.Create( StrTitleDiagnoseEndocrine, 'E[23]%', fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDiagnoseCollector.Create( StrTitleDiagnoseMetabolic, 'E[789]%', fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDiagnoseCollector.Create( StrTitleDiagnosePsychiatry, 'F[123456789]%', fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDiagnoseCollector.Create( StrTitleDiagnoseHypertension, 'I1[012345]%', fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDiagnoseCollector.Create( StrTitleDiagnoseIschemia, 'I2[012345]%', fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDiagnoseCollector.Create( StrTitleDiagnoseAtrialFibrillation, 'I48%', fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDiagnoseCollector.Create( StrTitleDiagnoseStroke, 'I6[01234]%', fDatapointFactory, fSQL, GlobalLog ) );
  { Custom collectors }
  AddCollector( TDementiaCollector.Create( StrTitleDiagnoseDementia, fDatapointFactory, fSQL, GlobalLog ) );
end;

procedure TQuickStatCollectors.AddCollectorsLabData;
const
  PROC_NAME = 'AddCollectorsLabData';
begin
  GlobalLog.EnterMethod( Self, PROC_NAME );
  try
    { Standard LabSet collectors }
    AddCollector( TLabSetCollector.CreateOldSchool( QST_LAB_KIDNEY, StrTitleLabsetKidney, LABSET_KIDNEY, fDatapointFactory, fSQL, GlobalLog ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_ANEMIA ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_LIPIDS ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_DIGITALIS ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_LIVER ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_THYROID ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_GLUCOSE ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_INR ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_HYPERPARA ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_HEART_FAILURE ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_INTERLEUKINS ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_CRP ) );
    { All labdata }
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_HIGH ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_MEDIUM ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_LOW ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_COUNT_3M ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_COUNT_6M ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_COUNT_12M ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_COUNT_24M ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_COUNT_60M ) );
  finally
    GlobalLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TQuickStatCollectors.AddCollectorsDrug;
begin
  with fDatapointFactory do
  begin
    { Simple drug selections that can be matches with a "ATC LIKE" statment in SQL }
    RegisterDataPointClass( VAR_PREFIX_DRUG + ATC_A10, TDrugDatapoint ); // Antidiabetic
    RegisterDataPointClass( VAR_PREFIX_DRUG + ATC_A10BA02, TDrugDatapoint ); // Metformin except combinations
    RegisterDataPointClass( VAR_PREFIX_DRUG + ATC_A11EA, TDrugDatapoint ); // Vitamin B-complex
    RegisterDataPointClass( VAR_PREFIX_DRUG + ATC_B01AA03, TDrugDatapoint ); // Warfarin
    RegisterDataPointClass( VAR_PREFIX_DRUG + ATC_B01AF, TDrugDatapoint ); // DOAK
    RegisterDataPointClass( VAR_PREFIX_DRUG + ATC_B03BA, TDrugDatapoint ); // B12
    RegisterDataPointClass( VAR_PREFIX_DRUG + ATC_B03BA01, TDrugDatapoint ); // Cyanokobalamin
    RegisterDataPointClass( VAR_PREFIX_DRUG + ATC_B03BA03, TDrugDatapoint ); // Hydroksykobalamin
    RegisterDataPointClass( VAR_PREFIX_DRUG + ATC_C0x23789, TDrugDatapoint ); // Blood pressure medications
    RegisterDataPointClass( VAR_PREFIX_DRUG + ATC_C09, TDrugDatapoint ); // Renin/angiotensin
    RegisterDataPointClass( VAR_PREFIX_DRUG + ATC_M01A, TDrugDatapoint ); // NSAID
    RegisterDataPointClass( VAR_PREFIX_DRUG + ATC_N04BA, TDrugDatapoint ); // Anti-Parkinson drugs
    RegisterDataPointClass( VAR_PREFIX_DRUG + ATC_N05B, TDrugDatapoint ); // Anxiolytics
    RegisterDataPointClass( VAR_PREFIX_DRUG + ATC_N05C, TDrugDatapoint ); // Hypnotics and sedatives
    RegisterDataPointClass( VAR_PREFIX_DRUG + ATC_N06D, TDrugDatapoint ); // Antidementia drugs
    { Faste medisiner }
    RegisterDataPointClass( VAR_PREFIX_DRUG_FAST + ATC_N02A, TDrugDatapoint ); // Opioider
    RegisterDataPointClass( VAR_PREFIX_DRUG_FAST + ATC_N05A, TDrugDatapoint ); // Nevroleptika
    RegisterDataPointClass( VAR_PREFIX_DRUG_FAST + ATC_N05B, TDrugDatapoint ); // Anxiolytika
    RegisterDataPointClass( VAR_PREFIX_DRUG_FAST + ATC_N05C, TDrugDatapoint ); // Hypnotics and sedatives
    RegisterDataPointClass( VAR_PREFIX_DRUG_FAST + ATC_N06D, TDrugDatapoint ); // Antidementia drugs
    { Custom lists not expressible with simple patters }
    RegisterDataPointClass( VAR_PREFIX_DRUG + VAR_ANTICHOLIN_N05, TDrugDatapoint ); // Antikolinerge antipsykotika
    RegisterDataPointClass( VAR_PREFIX_DRUG + VAR_ANTICHOLIN_AB, TDrugDatapoint ); // Antikolinerge midler
  end;
  { Simple drug collectors }
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_A10, ATC_A10, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_A10BA02, ATC_A10BA02, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_A11EA, ATC_A11EA, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_B01AA03, ATC_B01AA03, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_B01AF, ATC_B01AF, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_B03BA, ATC_B03BA, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_B03BA01, ATC_B03BA01, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_B03BA03, ATC_B03BA03, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_C01A, ATC_C01A, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_C02, ATC_C02, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_C03, ATC_C03, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_C07, ATC_C07, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_C08, ATC_C08, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_C08D, ATC_C08D, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_C09, ATC_C09, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_C0x23789, ATC_C0x23789, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_M01A, ATC_M01A, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateChecksum( TXT_DRUG_N04BA, ATC_N04BA, fDatapointFactory, fSQL, GlobalLog ) );
  { Collectors fast }
  AddCollector( TDrugCollector.CreateForTreatType( TXT_DRUG_N02A, ATC_N02A, ttAnyTreatType, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateForTreatType( TXT_DRUG_N02B, ATC_N02B, ttAnyTreatType, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateForTreatType( TXT_DRUG_N05A, ATC_N05A, ttAnyTreatType, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateForTreatType( TXT_DRUG_N05B, ATC_N05B, ttAnyTreatType, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateForTreatType( TXT_DRUG_N05C, ATC_N05C, ttAnyTreatType, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateForTreatType( TXT_DRUG_N06A, ATC_N06A, ttAnyTreatType, fDatapointFactory, fSQL, GlobalLog ) );
  AddCollector( TDrugCollector.CreateForTreatType( TXT_DRUG_N06D, ATC_N06D, ttAnyTreatType, fDatapointFactory, fSQL, GlobalLog ) );
  { Drug interaction collector }
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUID_COUNT ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUID_SPECIFIC ) );
  { Drug collectors of various sorts }
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_COUNT_GROUP ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_COUNT_NOATC ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_COUNT ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_METFORMIN ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_ANTICHOLIN_N05 ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_ANTICHOLIN_AB ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_ANTIBIOTIC_RESISTANCE ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_ANTIBIOTIC_INTERMEDIATE ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_ANTIBIOTIC_RECOMMENDED ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_J01XX05 ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_NorGeP ) );
end;

procedure TQuickStatCollectors.AddCollectorsStudySpecific;
var
  formTitle: string;
  formName: string;
  formClasses: TDictionary<string, string>;
begin
  formClasses := TDictionary<string, string>.Create;
  with fSQL.FastQuery( QRY_FORM_CLASSES, [fStudyId.StudyId] ) do
    try
      while not EOF do
      begin
        formName := FieldByName( FLD_FORM_NAME ).AsString;
        formTitle := FieldByName( FLD_FORM_TITLE ).AsString;
        if TRegEx.IsMatch( formName, 'FORM\d+' ) then
          fLog.Event( 'Skipping anonymous forms' )
        else if not formClasses.ContainsKey( formName ) then
        begin
          AddCollector( TFormAgeCollector.Create( formName, Format( StrTitleFormageTemplate, [formTitle, formName] ), formName, fDatapointFactory, fSQL, GlobalLog ) );
          // AddCollector( TFormDataCollector.Create( formName, Format( StrTitleFormdataTemplate, [formTitle, formName] ), formName, fDatapointFactory, fSQL, GlobalLog ) );
          AddCollector( TFormDataCollector.Create( formName, Format( StrTitleFormdataTemplate, [formTitle, formName] ), formName, fDatapointFactory, fSQL, GlobalLog ) );
          formClasses.Add( formName, formTitle );
        end;
        Next;
      end;
    finally
      Close;
      formClasses.Free;
    end;
end;

procedure TQuickStatCollectors.AddCollectorsHardCoded;
const
  PROC_NAME = 'AddCollectorsHardcoded';
begin
  GlobalLog.EnterMethod( Self, PROC_NAME );
  try
    AddCollector( TVarSetCollector.CreateForNumeric( 'SIZE', StrTitleAntropometrics, SET_HEIGHT_WEIGHT_BMI, fDatapointFactory, fSQL, GlobalLog ) );
    { GBD Protocol VarSet collectors }
    if TRegEx.IsMatch( fStudyId.StudyName, 'GBD|LANGTID' ) then
    begin
      AddCollector( fCollectorFactory.CreateCollector( QS_WEIGHT_DAYS ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_TVANGSVEDTAK ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_INNLEGGELSE_12M ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_FORM_LEGE3M ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_SCORES ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_BP ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_PRIMARY_CONTACT ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_WEIGHT_2M ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_SBP_2M ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_FLACKER_12M ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_FLACKER_DEATH ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_HULTEN_3M ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_QUALID_6M ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_KDV_6M ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_BARTHEL_6M ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_STRATIFY_6M ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_MNA_6M ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_ANTIHYPERTENSIVES_LOW_BP ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_LOW_BP ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_C09_GFR ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_METFORMIN_GFR ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_LMG_6M ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_GBD_BESLUTNINGER_6M ) );
      { Geriatric LabSet collectors }
      AddCollector( fCollectorFactory.CreateCollector( QST_LAB_GERIATRIC ) );
      AddCollectorsDiagnose;
      AddCollectorsDrug;
    end;
    if TRegEx.IsMatch( fStudyId.StudyName, 'NDV|ENDO|LANGTID|GBD' ) then
    begin
      { NDV Protocol VarSet collectors }
      AddCollector( fCollectorFactory.CreateCollector( QS_NDV_DIAGNOSE ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_NDV_TREATMENT ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_NDV_COMPLICATIONS ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_NDV_INSULIN ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_NDV_HYPOGLYCEMIA ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_NDV_EXERCISE ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_NDV_SOCIAL ) );
      { Diabetes LabSet collectors }
      AddCollector( fCollectorFactory.CreateCollector( QST_LAB_DIABETES ) );
    end;
    if TRegEx.IsMatch( fStudyId.StudyName, 'GWAS' ) then
    begin
      { ROAS LabSetCollectors }
      AddCollector( fCollectorFactory.CreateCollector( QS_ROAS_GWAS_BG ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_ROAS_GWAS_AB ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_ROAS_GWAS_AB_APS1 ) );
    end;
    if TRegEx.IsMatch( fStudyId.StudyName, 'ROAS' ) then
    begin
      { ROAS POI Collectors }
      AddCollector( fCollectorFactory.CreateCollector( QS_ROAS_POI_ORD ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_ROAS_POI_QN ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_ROAS_BASE ) );
    end;
    if TRegEx.IsMatch( fStudyId.StudyName, 'DOGFOOD', [roIgnoreCase] ) then
    begin
      { DOGFOOD Collectors }
      AddCollector( fCollectorFactory.CreateCollector( QS_DOGFOOD_DATABASE_VERSION ) );
    end;
  finally
    GlobalLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

{$ENDREGION}

end.
