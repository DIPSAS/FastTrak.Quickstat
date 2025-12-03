unit EPR.QA.Definitions;

interface

uses
  VMR.Lab.Interfaces;

type
  TItemSet = array of integer;

  TCollectorSpecification = class( TObject )
  strict private
    { Required fields }
    fName: string;
    fTitle: string;
    fVarPrefix: string;
  public
    constructor Create( const AName: string; ATitle: string; const AVarPrefix: string ); reintroduce;
    property name: string read fName;
    property Title: string read fTitle;
    property VarPrefix: string read fVarPrefix;
  end;

  TItemSetSpecification = class( TCollectorSpecification )
  strict private
    fVarSet: TItemSet;
  public
    constructor Create( const AName, ATitle: string; const AVarset: TItemSet ); reintroduce;
  end;

  TItemAgeSpecification = class( TCollectorSpecification )
  strict private
    fItemId: integer;
    fItemAge: integer;
  public
    constructor Create( const AName, ATitle: string; const AItemId, AItemAge: integer ); reintroduce;
    property ItemId: integer read fItemId;
    property ItemAge: integer read fItemAge;
  end;

  TDrugCollectorSpecification = class( TCollectorSpecification )
  strict private
    fAtcPattern: string;
    { Defaults to empty }
    fCustomWhere: string;
    fCustomSuffix: string;
    fUseNameChecksum: boolean;
    fLongTermOnly: boolean;
  public
    constructor CreateBasic( const ATitle, AMatchPattern: string );
    { Properties }
    property AtcPattern: string read fAtcPattern;
    property CustomWhere: string read fCustomWhere;
    property CustomSuffix: string read fCustomSuffix;
    property LongTermOnly: boolean read fLongTermOnly;
    property UseNameChecksum: boolean read fUseNameChecksum;
  end;

const
  SET_DM_LABDATA = [ltAcRatio, ltCholesterol, ltLdlCholesterol, ltHdlCholesterol, ltTriglycerides, ltInsuline, ltPlasmaGlucose, ltHbA1c, ltCPeptide];

  SET_NDV_COMPLICATIONS: array [0 .. 20] of integer = ( 3351, 3352, 4235, 3218, 3397, 3398, 3414, 3415, 3417, 4054, 4055, 4062, 4087, 4205, 4521, 4527, 4845, 7517, 7519, 7520, 7521 );
  SET_NDV_INSULIN: array [0 .. 7] of integer        = ( 3322, 4056, 3209, 3906, 3206, 3905, 3933, 3908 );
  SET_NDV_HYPOGLYCEMIA: array [0 .. 3] of integer   = ( 3220, 3351, 4234, 3352 );
  SET_NDV_EXERCISE: array [0 .. 2] of integer       = ( 3340, 3197, 4638 );
  SET_NDV_SOCIAL: array [0 .. 1] of integer         = ( 3982, 4002 );
  SET_NDV_BP: array [0 .. 1] of integer             = ( 3230, 3231 );
  SET_NDV_DIAGNOSE: array [0 .. 2] of integer       = ( 3196, 3389, 3486 );
  SET_NDV_TREATMENT: array [0 .. 1] of integer      = ( 3322, 4056 );
  SET_NDV_CONSENT: array [0 .. 0] of integer        = ( 3389 );

  { GBD Varsets }
  SET_GBD_BP: array [0 .. 1] of integer              = ( 3555, 3556 );
  SET_GBD_HEART_FAILURE: array [0 .. 6] of integer   = ( 186, 187, 195, 200, 205, 210, 221 );
  SET_GBD_FALLS: array [0 .. 8] of integer           = ( 9248, 9249, 9250, 9254, 9252, 9253, 9255, 9256, 9257 );
  SET_GBD_INR: array [0 .. 3] of integer             = ( 3737, 3738, 3739, 3741 );
  SET_GBD_SCORES: array [0 .. 8] of integer          = ( 1128, 1685, 4234, 4342, 4771, 4787, 4791, 5827, 9257 );
  SET_GBD_NUTRITION: array [0 .. 3] of integer       = ( 4353, 4354, 4529, 4771 );
  SET_GBD_DEMENTIA: array [0 .. 1] of integer        = ( 4429, 1685 );
  SET_MNA_PART1: array [0 .. 0] of integer           = ( 4771 );
  SET_GBD_PRIMARY_CONTACT: array [0 .. 0] of integer = ( 8420 );

  { Barnediabetes Varsets }
  SET_INSULINPUMPE: array [0 .. 1] of integer      = ( 5166, 5162 );
  SET_BDR_DIAGNOSE: array [0 .. 1] of integer      = ( 3196, 3843 );
  SET_BDR_DIAGNOSE_YEAR: array [0 .. 0] of integer = ( 3486 );
  SET_BDR_COMORBID: array [0 .. 6] of integer      = ( 3410, 6312, 6313, 3364, 3355, 3356, 3357 );

  { Shared varsets }
  SET_BP_ALL: array [0 .. 9] of integer            = ( 185, 207, 573, 600, 3145, 3146, 3230, 3231, 3555, 3556 );
  SET_WEIGHT: array [0 .. 0] of integer            = ( 3224 );
  SET_HEIGHT_WEIGHT_BMI: array [0 .. 2] of integer = ( 3224, 3225, 3310 );
  SET_SMOKING: array [0 .. 0] of integer           = ( 3227 );

  { ROAS Varsets }
  SET_GWAS_BG: array [0 .. 14] of integer          = ( 2143, 6089, 6299, 6090, 6312, 6321, 6313, 6314, 6317, 3411, 6318, 8594, 3410, 6320, 6050 );
  SET_GWAS_AUTOANTIBODY: array [0 .. 6] of integer = ( 5947, 5948, 5949, 6044, 6049, 6051, 6058 );
  SET_GWAS_APS1: array [0 .. 7] of integer         = ( 6076, 6077, 6078, 6079, 6080, 6073, 6045, 6074 );

  SET_POI_ORD: array [0 .. 19] of integer = ( 2143, 6299, 6090, 6314, 6321, 6663, 6312, 6313, 6318, 6806, 3410, 7977, 3411, 6320, 6322, 7978, 6317, 6316, 8543, 6050 );
  SET_POI_QN: array [0 .. 12] of integer  = ( 6089, 3486, 6332, 6323, 6324, 6334, 6328, 6330, 6331, 6333, 6327, 6326, 8544 );

type
  TLabClassSet = array of integer;

const
  LABCLASSES_ANEMIA: TLabClassSet          = [22, 29, 30, 31, 62, 63, 64, 77, 78, 79, 80, 81, 82, 193];
  LABCLASSES_CRP: TLabClassSet             = [26];
  LABCLASSES_DIABETES: TLabClassSet        = [3, 4, 5, 6, 7, 34, 35, 36, 53, 54, 50, 49, 90, 91, 995, 1058, 1075];
  LABCLASSES_DIABETES_LIPIDS: TLabClassSet = [34, 35, 36];
  LABCLASSES_DIABETES_NDV: TLabClassSet    = [5, 6, 34, 35, 36, 49, 50, 995, 1058, 1075];
  LABCLASSES_DIABETES_BDR: TLabClassSet    = [35, 497];
  LABCLASSES_GLUCOSE: TLabClassSet         = [41, 42, 43, 44, 46, 47, 48, 58, 59, 60, 1058];
  LABCLASSES_DIGITALIS: TLabClassSet       = [91, 124, 140, 171];
  LABCLASSES_GERIATRIC: TLabClassSet       = [22, 50, 51, 52, 53, 91, 140, 575, 995, 1075];
  LABCLASSES_HEART_FAILURE: TLabClassSet   = [6, 22, 49, 50, 51, 52, 53, 90, 91, 124, 140, 171, 575, 995, 1075];
  LABCLASSES_HYPERPARA: TLabClassSet       = [94, 95, 332, 576, 770];
  LABCLASSES_INR: TLabClassSet             = [18, 20];
  LABCLASSES_KIDNEY: TLabClassSet          = [3, 4, 5, 6, 7, 53, 54, 50, 49, 90, 91, 995, 1075];
  LABCLASSES_LIPIDS: TLabClassSet          = [34, 35, 36, 37, 38, 39, 40];
  LABCLASSES_LIVER: TLabClassSet           = [123, 124, 125, 126, 127, 128, 129, 139];
  LABCLASSES_NUTRITION: TLabClassSet       = [22, 55, 83, 772, 1058];
  LABCLASSES_URINE: TLabClassSet           = [3, 4, 5, 6, 7];
  LABCLASSES_THYROID: TLabClassSet         = [83, 84, 85, 86, 87, 88, 89];

implementation

uses
  EPR.QA.Collector.Names,
  System.RegularExpressions, System.SysUtils;

{ TDrugCollectorSpecification }

function ConvertAtcPatternToVariableName( const AMatchPatternAtc: string ): string;
begin
  { Generate name based on ATC, remove invalid variable characters }
  Result := TRegEx.Replace( AMatchPatternAtc, '\[', 'x' );
  Result := TRegEx.Replace( Result, '[%\]]', EmptyStr );
end;

constructor TDrugCollectorSpecification.CreateBasic( const ATitle, AMatchPattern: string );
begin
  inherited Create( PREFIX_DRUG_COLLECTOR + ConvertAtcPatternToVariableName( AMatchPattern ), ATitle, VAR_PREFIX_DRUG );
  fAtcPattern := AMatchPattern;
end;

{ TItemSetSpecification }

constructor TItemSetSpecification.Create( const AName, ATitle: string; const AVarset: TItemSet );
begin
  inherited Create( AName, ATitle, EmptyStr );
  fVarSet := AVarset;
end;

{ TCollectorSpecification }

constructor TCollectorSpecification.Create( const AName: string; ATitle: string; const AVarPrefix: string );
begin
  inherited Create;
  fName := AName;
  fTitle := ATitle;
  fVarPrefix := AVarPrefix;
end;

{ TItemAgeSpecification }

constructor TItemAgeSpecification.Create( const AName, ATitle: string; const AItemId, AItemAge: integer );
begin
  inherited Create( AName, ATitle, VAR_PREFIX_ITEMAGE );
  fItemId := AItemId;
  fItemAge := AItemAge;
end;

end.
