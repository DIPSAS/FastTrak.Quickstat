unit CRF.Person;
{$M+}

interface

uses
  {Project}
  CRF.Person.Interfaces,
  CRF.Context.Session.Interfaces,
  {General}
  Emetra.Logging.Interfaces,
  Emetra.Database.Interfaces,
  Emetra.Person.Interfaces,
  Emetra.Person,
  Emetra.Classes.Subject.Stored,
  {Standard}
  Classes, Db, Variants;

type
  TCRFPerson = class( TStoredListItem, IPersonReadOnly, IPersonId, IPersonIdentity, IPersonVisualId, ICRFPerson )
  private
    fInitials: string;
    function Get_DOBStr: string;
    function Get_BestId: string;
    procedure EditPerson( ADOB: TDateTime; AGenderId: integer; AFirstName, ALastName, ANationalId: string; APersonId: integer = 0 );
  protected
    { Some getters are necessary to conform to IPerson specification }
    fPerson: TPerson;
    function Get_Age: double;
    function Get_YearsOld: integer;
    function Get_DOB: TDate;
    function Get_DOBYMD: string;
    function Get_EmailAddress: string;
    function Get_EmployeeNumber: integer;
    function Get_FirstName: string;
    function Get_FullName: string;
    function Get_GenderId: integer;
    function Get_GSM: string;
    function Get_HPRNo: integer;
    function Get_PhoneNumber: string;
    function Get_LastName: string;
    function Get_NationalId: string;
    function Get_Person: IPersonReadOnly;
    function Get_PersonId: integer;
    function Get_Pronoun: string;
    function Get_PronounObjective: string;
    function Get_ReverseName: string;
    function Get_Sex: TSex;
    function Get_SexStr: string;
    function Get_VisualId: string;
    function Get_YOB: integer;
    function ShortId: string;
    function GetData: TDataset; override;
    procedure Load( ADataset: TDataset ); override;
    procedure Set_DOB( const Value: TDate );
    procedure Set_FirstName( const AValue: string );
    procedure Set_FullName( const AValue: string );
    procedure Set_GenderId( const AValue: integer );
    procedure Set_GSM( const AValue: string );
    procedure Set_LastName( const AValue: string );
    procedure Set_NationalId( const AValue: string );
  public
    { Core data }
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    procedure Clear; override;
    procedure Dispose;
    procedure Identify( const APersonId: integer );
    function AsListbox( const ASimple: boolean ): string; override;
    procedure UpdateNationalId( const ANationalId: string; APersonId: integer = 0 );
    { For display }
    function Valid: boolean;
    { Search functions that return a patient ID, or -1 if nothing was found }
    procedure Update( APerson: IPersonReadOnly; APersonId: integer = 0 );
    { Queries that can be used by other objects }
    property Person: IPersonReadOnly read Get_Person implements IPersonReadOnly;
  published
    property Age: double read Get_Age;
    property BestId: string read Get_BestId;
    property DOB: TDate read Get_DOB write Set_DOB;
    property DOBStr: string read Get_DOBStr;
    property DOBYMD: string read Get_DOBYMD;
    property EmailAddress: string read Get_EmailAddress;
    property EmployeeNumber: integer read Get_EmployeeNumber;
    property FirstName: string read Get_FirstName write Set_FirstName;
    property FstName: string read Get_FirstName;
    property FullName: string read Get_FullName write Set_FullName;
    property GenderId: integer read Get_GenderId write Set_GenderId;
    property GSM: string read Get_GSM write Set_GSM;
    property HPRNo: integer read Get_HPRNo;
    property Initials: string read fInitials;
    property LastName: string read Get_LastName write Set_LastName;
    property LstName: string read Get_LastName;
    property NationalId: string read Get_NationalId write Set_NationalId;
    property PersonId: integer read Get_PersonId;
    property Pronoun: string read Get_Pronoun;
    property PronounObjective: string read Get_PronounObjective;
    property ReverseName: string read Get_ReverseName;
    property Sex: TSex read Get_Sex;
    property SexStr: string read Get_SexStr;
    property TeleCom: string read Get_GSM;
    property VisualId: string read Get_VisualId;
    property YearsOld: integer read Get_YearsOld;
    property YOB: integer read Get_YOB;
  end;

  { TCustomStudyPerson should not be instantiated directly,
    it is a common ancestor for TStudyCase and TStudyUser }

  TCustomStudyPerson = class( TCRFPerson, IStudyCenterContext )
  private
    fCenterAddress: string;
    fCenterCity: string;
    fCenterId: integer;
    fCenterName: string;
    fCenterPhone: string;
    fCenterPostcode: string;
    fGroupId: integer;
    fGroupName: string;
    procedure Set_StudyId( const AValue: integer );
  protected
    fStudyId: integer;
    fStudyContext: IStudyContext;
    function Get_CenterId: integer;
    function Get_CenterName: string;
    function Get_GroupId: integer;
    function Get_GroupName: string;
    function Get_StudyContext: IStudyContext;
    function Get_StudyId: integer;
    { Missing groups }
    function MissingGroupsMessage: string;
  public
    constructor Create( AStudyContext: IStudyContext; ADatabase: ISQL; ALog: ILog ); reintroduce; overload;
    function SelectGroup: integer; dynamic;
    function ValidGroup: boolean;
    procedure Clear; override;
    procedure Load( ADataset: TDataset ); override;
  published
    property CenterAddress: string read fCenterAddress;
    property CenterCity: string read fCenterCity;
    property CenterId: integer read Get_CenterId;
    property CenterName: string read Get_CenterName;
    property CenterPhone: string read fCenterPhone;
    property CenterPostcode: string read fCenterPostcode;
    property GroupId: integer read Get_GroupId;
    property GroupName: string read Get_GroupName;
    property StudyContext: IStudyContext read Get_StudyContext;
    property StudyId: integer read Get_StudyId write Set_StudyId;
  end;

resourcestring
  ERR_EDIT_PERSON_FAILED =
  { } 'Endringer i personalia kunne ikke lagres.\n' +
  { } 'Melding: %s';
  { Select group }
  HDR_GROUP = 'Velg gruppe';
  WARN_NO_GROUPS_IN_PROTOCOL =
  { } 'Ingen grupper er definert i fagjournalen "%s" her (%s).\n' +
  { } 'Superbrukere kan opprette grupper via superbrukermenyen.';

  TXT_SELECT_GROUP = 'Velg lokal gruppe fra listen';

implementation

uses
  CRF.SQL,
  CRF.SQL.Fields,
  Emetra.Person.SQL,
  Emetra.DateUtils,
  {Standard}
  System.DateUtils, System.SysUtils;

resourcestring
  TXT_SHE = 'hun';
  TXT_HE = 'han';
  TXT_HIM = 'ham';
  TXT_HER = 'henne';
  TXT_PERSON_NEUTRAL = 'vedkommende';

const
  CLASS_NAME = 'TPerson';
  TAB        = #9;

  { TPerson }

function TCRFPerson.Get_Age: double;
begin
  Result := YearSpan( Now, fPerson.DOB );
end;

function TCRFPerson.Get_YearsOld: integer;
begin
  Result := Emetra.DateUtils.YearsOld( Now, fPerson.DOB );
end;

function TCRFPerson.Get_BestId: string;
begin
  if fPerson.NationalId <> EmptyStr then
    Result := fPerson.NationalId
  else
    Result := DateToStr( fPerson.DOB );
end;

function TCRFPerson.Get_DOBStr: string;
begin
  Result := DateToStr( fPerson.DOB );
end;

function TCRFPerson.Get_DOBYMD: string;
begin
  Result := FormatDateTime( 'yyyy-mm-dd', fPerson.DOB );
end;

function TCRFPerson.Get_EmailAddress: string;
begin
  Result := fPerson.Email;
end;

function TCRFPerson.Get_EmployeeNumber: integer;
begin
  Result := fPerson.EmployeeNumber;
end;

function TCRFPerson.Get_FullName: string;
begin
  Result := StringReplace( fPerson.FirstName + ' ' + fPerson.MiddleName + ' ' + fPerson.LastName, '  ', ' ', [] );
end;

{$REGION 'Simple Getters'}

function TCRFPerson.Get_DOB: TDate;
begin
  Result := fPerson.DOB;
end;

function TCRFPerson.Get_FirstName: string;
begin
  Result := fPerson.FirstName;
end;

function TCRFPerson.Get_GenderId: integer;
begin
  Result := fPerson.GenderId;
end;

function TCRFPerson.Get_GSM: string;
begin
  Result := fPerson.Phone;
end;

function TCRFPerson.Get_HPRNo: integer;
begin
  Result := fPerson.HPRNo;
end;

function TCRFPerson.Get_LastName: string;
begin
  Result := fPerson.LastName;
end;

function TCRFPerson.Get_NationalId: string;
begin
  Result := fPerson.NationalId;
end;

function TCRFPerson.Get_Person: IPersonReadOnly;
begin
  Result := fPerson;
end;

function TCRFPerson.Get_PersonId: integer;
begin
  Assert( FPrimaryKey = fPerson.PersonId );
  Result := FPrimaryKey;
end;

function TCRFPerson.Get_PhoneNumber: string;
begin
  Result := fPerson.Phone;
end;

function TCRFPerson.Get_ReverseName: string;
begin
  Result := Trim( Format( '%s, %s %s', [fPerson.LastName, fPerson.FirstName, fPerson.MiddleName] ) );
end;

function TCRFPerson.Get_Pronoun;
begin
  case fPerson.Sex of
    sexMale: Result := TXT_HE;
    sexFemale: Result := TXT_SHE;
  else Result := TXT_PERSON_NEUTRAL;
  end;
end;

function TCRFPerson.Get_PronounObjective;
begin
  case fPerson.Sex of
    sexMale: Result := TXT_HIM;
    sexFemale: Result := TXT_HER;
  else Result := TXT_PERSON_NEUTRAL;
  end;
end;

function TCRFPerson.Get_Sex: TSex;
begin
  Result := fPerson.Sex;
end;

function TCRFPerson.Get_SexStr: string;
begin
  Result := fPerson.SexStr;
end;

function TCRFPerson.Get_VisualId: string;
begin
  Result := fPerson.VisualId;
end;

function TCRFPerson.Get_YOB;
begin
  Result := YearOf( fPerson.DOB );
end;

procedure TCRFPerson.Identify( const APersonId: integer );
begin
  BeginUpdate;
  try
    Clear;
    fPerson.PersonId := APersonId;
    FPrimaryKey := APersonId;
  finally
    EndUpdate;
  end;
end;

procedure TCRFPerson.Set_DOB( const Value: TDate );
begin
  BeginUpdate;
  try
    fPerson.DOB := Value;
  finally
    EndUpdate;
  end;
end;

procedure TCRFPerson.Set_FirstName( const AValue: string );
begin
  if AValue = fPerson.FirstName then
    exit;
  BeginUpdate;
  try
    fPerson.FirstName := AValue;
  finally
    EndUpdate;
  end;
end;

procedure TCRFPerson.Set_FullName( const AValue: string );
begin
  if AValue = fPerson.FullName then
    exit;
  BeginUpdate;
  try
    fPerson.FullName := AValue;
  finally
    EndUpdate;
  end;
end;

procedure TCRFPerson.Set_GenderId( const AValue: integer );
begin
  if AValue = fPerson.GenderId then
    exit;
  BeginUpdate;
  try
    fPerson.GenderId := AValue;
  finally
    EndUpdate;
  end;
end;

procedure TCRFPerson.Set_GSM( const AValue: string );
begin
  if AValue = fPerson.Phone then
    exit;
  BeginUpdate;
  try
    SQL.ExecuteCommand( CMD_UPDATE_GSM, [PersonId, AValue] );
    fPerson.Phone := AValue;
  finally
    EndUpdate;
  end;
end;

procedure TCRFPerson.Set_LastName( const AValue: string );
begin
  BeginUpdate;
  try
    fPerson.LastName := AValue;
  finally
    EndUpdate;
  end;
end;

procedure TCRFPerson.Set_NationalId( const AValue: string );
begin
  BeginUpdate;
  try
    fPerson.NationalId := AValue;
  finally
    EndUpdate;
  end;
end;

{$ENDREGION}

function TCRFPerson.AsListbox( const ASimple: boolean ): string;
begin
  Result := DateToStr( DOB ) + TAB + ReverseName + TAB + TAB + NationalId;
end;

procedure TCRFPerson.Clear;
begin
  BeginUpdate;
  try
    inherited Clear;
    fPerson.Clear;
    fInitials := '';
  finally
    EndUpdate;
  end;
end;

procedure TCRFPerson.AfterConstruction;
begin
  inherited;
  fPerson := TPerson.Create( Log );
end;

procedure TCRFPerson.BeforeDestruction;
begin
  fPerson.Free;
  inherited;
end;

procedure TCRFPerson.Dispose;
begin
  Self.Free;
end;

function TCRFPerson.Valid: boolean;
begin
  Result := PersonId > 0;
end;

function TCRFPerson.GetData: TDataset;
begin
  Result := SQL.FastQuery( QRY_PERSON_BY_ID, [PersonId] )
end;

procedure TCRFPerson.Load( ADataset: TDataset );
begin
  BeginUpdate;
  try
    inherited Load( ADataset );
    FPrimaryKey := ReadInteger( ADataset, FLD_PERSON_ID, FPrimaryKey );
    fInitials := ReadString( FLD_INITIALS );
    fPerson.PersonId := FPrimaryKey;
    fPerson.NationalId := ReadString( FLD_NATIONAL_ID );
    fPerson.DOB := ReadDateTime( FLD_DOB );
    fPerson.GenderId := ReadInteger( FLD_GENDER_ID );
    fPerson.FirstName := ReadString( FLD_FIRST );
    fPerson.MiddleName := ReadString( FLD_MIDDLE );
    fPerson.LastName := ReadString( FLD_LAST );
    fPerson.EmployeeNumber := ReadInteger( FLD_EMPLOYEE_NUMBER );
    fPerson.HPRNo := ReadInteger( FLD_HPR_NO );
    fPerson.Email := ReadString( FLD_EMAIL_ADDRESS );
    fPerson.Phone := ReadString( FLD_GSM );
    { "Folkeregister" - address }
    fPerson.SetAddress( ReadString( FLD_STREET_ADDRESS ), ReadString( FLD_POSTAL_CODE ), ReadString( FLD_CITY ) );
  finally
    EndUpdate;
  end;
end;

procedure TCRFPerson.UpdateNationalId( const ANationalId: string; APersonId: integer = 0 );
begin
  BeginUpdate;
  if APersonId = 0 then
    APersonId := PersonId;
  try
    SQL.ExecuteCommand( CMD_UPDATE_NATIONAL_ID, [APersonId, ANationalId] );
    { Update if this is the current person, and all went fine }
    if APersonId = PersonId then
      fPerson.NationalId := ANationalId;
  except
    on E: Exception do
      Log.Event( E.Message, ltException );
  end;
  EndUpdate;
end;

procedure TCRFPerson.Update( APerson: IPersonReadOnly; APersonId: integer = 0 );
begin
  EditPerson( APerson.DOB, APerson.GenderId, APerson.FirstName, APerson.LastName, APerson.NationalId, APersonId );
end;

procedure TCRFPerson.EditPerson( ADOB: TDateTime; AGenderId: integer; AFirstName, ALastName, ANationalId: string; APersonId: integer = 0 );
const
  PROC_NAME = CLASS_NAME + '.EditPerson: ';
begin
  { Update database }
  if APersonId = 0 then
    APersonId := PersonId;
  Log.Event( PROC_NAME + '%d', [APersonId], ltInfo );
  BeginUpdate;
  try
    SQL.ExecuteCommand( CMD_UPDATE_PERSON, [APersonId, ADOB, AGenderId, Trim( AFirstName ), Trim( ALastName ), ANationalId] );
    { Update cached information about current person
      if it is the current one that is edited }
    if APersonId = PersonId then
    begin
      fPerson.LastName := Trim( ALastName );
      fPerson.FirstName := Trim( AFirstName );
      fPerson.DOB := ADOB;
      fPerson.GenderId := AGenderId;
      fPerson.NationalId := ANationalId;
    end;
  except
    on E: Exception do
      Log.Event( ERR_EDIT_PERSON_FAILED, [E.Message], ltError );
  end;
  EndUpdate;
end;

function TCRFPerson.ShortId: string;
begin
  if ( Length( FirstName ) > 0 ) and ( Length( LastName ) > 1 ) then
    Result := FirstName[1] + LastName[1]
  else
    Result := 'NN';
  Result := Result + FormatDateTime( 'ddmmyy', DOB );
end;

{$REGION 'TCustomStudyPerson'}

procedure TCustomStudyPerson.Clear;
begin
  inherited;
  BeginUpdate;
  try
    fCenterAddress := EmptyStr;
    fCenterCity := EmptyStr;
    fCenterId := 0;
    fCenterName := EmptyStr;
    fCenterPhone := EmptyStr;
    fCenterPostcode := EmptyStr;
    fGroupId := 0;
    fGroupName := EmptyStr;
    fStudyId := 0;
  finally
    EndUpdate;
  end;
end;

constructor TCustomStudyPerson.Create( AStudyContext: IStudyContext; ADatabase: ISQL; ALog: ILog );
begin
  inherited Create( ADatabase, ALog );
  fStudyContext := AStudyContext;
end;

function TCustomStudyPerson.Get_CenterId: integer;
begin
  Result := fCenterId;
end;

function TCustomStudyPerson.Get_CenterName: string;
begin
  Result := fCenterName;
end;

function TCustomStudyPerson.Get_GroupId: integer;
begin
  Result := fGroupId;
end;

function TCustomStudyPerson.Get_GroupName: string;
begin
  Result := fGroupName;
end;

function TCustomStudyPerson.Get_StudyContext: IStudyContext;
begin
  Assert( Assigned( fStudyContext ) );
  Result := fStudyContext;
end;

function TCustomStudyPerson.Get_StudyId: integer;
begin
  if Assigned( fStudyContext ) then
    Result := fStudyContext.StudyId
  else
    Result := fStudyId;
end;

procedure TCustomStudyPerson.Load( ADataset: TDataset );
begin
  BeginUpdate;
  try
    inherited Load( ADataset );
    fCenterAddress := ReadString( FLD_CENTER_ADDRESS );
    fCenterCity := ReadString( FLD_CENTER_CITY );
    fCenterId := ReadInteger( FLD_CENTER_ID );
    fCenterName := ReadString( FLD_CENTER_NAME );
    fCenterPhone := ReadString( FLD_CENTER_PHONE );
    fCenterPostcode := ReadString( FLD_CENTER_POSTCODE );
    fGroupId := ReadInteger( FLD_GROUP_ID );
    fGroupName := ReadString( FLD_GROUP_NAME );
    fPerson.Phone := ReadString( FLD_GSM );
    if Assigned( fStudyContext ) then
      fStudyId := fStudyContext.StudyId
    else
      fStudyId := 0;
    { Notify not called, because this should not be instantiated }
  finally
    EndUpdate;
  end;
end;

procedure TCustomStudyPerson.Set_StudyId( const AValue: integer );
var
  oldStudyId: integer;
begin
  if AValue = fStudyId then
    exit;
  Assert( fStudyContext = nil );
  oldStudyId := fStudyId;
  BeginUpdate;
  try
    fStudyId := AValue;
    if ( oldStudyId <> 0 ) and ( AValue <> 0 ) then
      Populate;
  finally
    EndUpdate;
  end;
end;

function TCustomStudyPerson.ValidGroup: boolean;
begin
  Result := fGroupId > 0;
end;

function TCustomStudyPerson.SelectGroup: integer;
begin
  if Assigned( fStudyContext ) then
    Result := PickList.SelectInteger( QRY_MY_STUDY_GROUPS, [fStudyContext.StudyId], HDR_GROUP, TXT_SELECT_GROUP, MissingGroupsMessage, true, ltError )
  else
    Result := -1;
end;

function TCustomStudyPerson.MissingGroupsMessage: string;
begin
  Result := Format( WARN_NO_GROUPS_IN_PROTOCOL, [fStudyContext.StudyName, Self.CenterName] );
end;

{$ENDREGION}

end.
