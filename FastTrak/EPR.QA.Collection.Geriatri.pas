unit EPR.QA.Collection.Geriatri;

interface

uses
  EPR.QA.PointFactory,
  EPR.QA.Collection,
  EPR.QA.Matrix.Interfaces,
  {VMR}
  VMR.Lab.Interfaces,
  {General}
  Emetra.Logging.Interfaces,
  Emetra.Database.Interfaces;

type
  TQAGeriatrics = class( TQACollection )
  public
    constructor Create( const AFactory: TDataPointFactory; const ASQL: ISQL; const ALog: ILog );
  end;

  TQANutrition = class( TQAGeriatrics )
  public
    procedure AfterConstruction; override;
  end;

  TQAHeartFailure = class( TQAGeriatrics )
  public
    procedure AfterConstruction; override;
  end;

  TQAFallRisk = class( TQAGeriatrics )
  public
    procedure AfterConstruction; override;
  end;

  TQAWarfarin = class( TQAGeriatrics )
    procedure AfterConstruction; override;
  end;

  TQADiabetes = class( TQAGeriatrics )
    procedure AfterConstruction; override;
  end;

  TQARepeatableForms = class( TQAGeriatrics )
    procedure AfterConstruction; override;
  end;

implementation

uses
  EPR.QA.Collector.Names,
  EPR.QA.Collector.Factory;

resourcestring

  { Collector titles }
  StrCaptionNutrition = 'Ernæring';
  StrCaptionHeartFailure = 'Hjertesvikt';
  StrCaptionGeriatrics = 'Geriatri';
  StrCaptionWarfarin = 'Marevan';
  StrCaptionDiabetes = 'Diabetes';

const
  STUDY_GBD = 'GBD';

  { Populations }
  POPULATION_ALL           = 1;
  POPULATION_WARFARIN      = 102;
  POPULATION_DIABETES      = 108;
  POPULATION_HEART_FAILURE = 164;

constructor TQAGeriatrics.Create( const AFactory: TDataPointFactory; const ASQL: ISQL; const ALog: ILog );
begin
  inherited Create( AFactory, STUDY_GBD, POPULATION_ALL, ASQL, ALog );
  Caption := StrCaptionGeriatrics;
end;

procedure TQANutrition.AfterConstruction;
begin
  inherited;
  Caption := StrCaptionNutrition;
  ImageIndex := qatBreakfastEgg;
  { General collectors }
  AddCollector( QS_PATIENT_AGE );
  { Various key meaures }
  AddCollector( QS_GBD_MEASURES );
  { Variable age weight }
  AddCollector( QS_WEIGHT_DAYS );
  AddCollector( QS_GBD_DEMENTIA );
  AddCollector( QS_GBD_NUTRITION );
  { Nutrition related labdata }
  AddCollector( QST_LAB_NUTRITION );
end;

procedure TQAHeartFailure.AfterConstruction;
begin
  inherited;
  Caption := StrCaptionHeartFailure;
  ImageIndex := qatHeartOrgan;
  PopulationId := POPULATION_HEART_FAILURE;
  { General collectors }
  AddCollector( QS_PATIENT_AGE );
  { Heart failure related labdata }
  AddCollector( QST_LAB_HEART_FAILURE );
  { Heart failure related items }
  AddCollector( QS_GBD_BP );
  AddCollector( QS_GBD_HEART_FAILURE );
  { Heart failure related drugs }
  AddCollector( QS_DRUG_C03 );
  AddCollector( QS_DRUG_C07 );
  AddCollector( QS_DRUG_C08D );
  AddCollector( QS_DRUG_C09 );
  AddCollector( QS_DRUG_M01A );
end;

procedure TQAFallRisk.AfterConstruction;
begin
  Caption := StrTitleVarsetFallRisk;
  ImageIndex := qatFemurBone;
  PopulationId := POPULATION_ALL;
  { Add collectors }
  AddCollector( QS_PATIENT_AGE );
  AddCollector( QS_GBD_FALLS );
  AddCollector( QS_GBD_BP );
end;

{ TQAWarfarin }

procedure TQAWarfarin.AfterConstruction;
begin
  inherited;
  Caption := StrCaptionWarfarin;
  ImageIndex := qatTablet;
  PopulationId := POPULATION_WARFARIN;
  { Rekkefølgen avgjør visning }
  AddCollector( QS_PATIENT_AGE );
  AddCollector( QS_GBD_INR );
  AddCollector( QST_LAB_INR );
  AddCollector( QS_FORMAGE_GBD_MAREVAN );
end;

procedure TQADiabetes.AfterConstruction;
begin
  inherited;
  Caption := StrCaptionDiabetes;
  ImageIndex := qatSyringe;
  PopulationId := POPULATION_DIABETES;
  { Create collectors for this dataset }
  AddCollector( QS_PATIENT_AGE );
  AddCollector( QS_GBD_DIABETES_BASE );
  { Labdata }
  AddCollector( QST_LAB_DIABETES );
  { Smoking etc }
  AddCollector( QS_GBD_SMOKING );
  AddCollector( QS_GBD_MEASURES );
  AddCollector( QS_GBD_BP );
  { Drug data }
  AddCollector( QS_DRUG_C10 );
end;

{ TQARepeatableForms }

procedure TQARepeatableForms.AfterConstruction;
begin
  inherited;
  Caption := 'Skjemarutine';
  ImageIndex := qatFormAge;
  PopulationId := POPULATION_ALL;
  { Rekkefølgen her avgjør rekkefølgen i grid view }
  AddCollector( QS_PATIENT_AGE );
  AddCollector( QS_FORMAGE_GBD_BARTHEL );
  AddCollector( QS_FORMAGE_GBD_KDV );
  AddCollector( QS_FORMAGE_GBD_FLACKERKIELY );
  AddCollector( QS_FORMAGE_GBD_BESLUTNINGER );
  AddCollector( QS_FORMAGE_GBD_MATKORT );
  AddCollector( QS_ITEMAGE_MNA_PART1 );
  AddCollector( QS_FORMAGE_GBD_NEWS2 );
  AddCollector( QS_FORMAGE_GBD_HULTEN );
  AddCollector( QS_FORMAGE_GBD_LMG );
  AddCollector( QS_FORMAGE_GBD_QUALID );
  AddCollector( QS_FORMAGE_GBD_STRATIFY );
end;

end.
