unit CRF.Person.StudyCase;

{$M+}

interface

uses
  {Project}
  CRF.Person.StudyCase.Interfaces,
  CRF.Person,
  {Standard}
  Classes, Db, SysUtils;

type
  TStudyCase = class( TCustomStudyPerson, ICRFStudyCase )
  strict private
    fClinRelId: integer;
    fIsTestCase: boolean;
    fRelId: integer;
    fRelName: string;
    fStatusActive: integer;
    fStatusId: integer;
    fStatusText: string;
  private
    function Get_ID: string;
  protected
    { Property accessors }
    function Get_HPRNo: integer;
    function Get_ClinRelId: integer;
    function Get_IsTestCase: boolean;
    function Get_RelId: integer;
    function Get_RelName: string;
    function Get_StatusId: integer;
    function Get_StatusText: string;
    function Get_VMR: TObject; dynamic;
    procedure Set_StatusId( const AStatusId: integer );
    procedure Set_GroupId( const AGroupId: integer );
    procedure Set_IsTestCase( const AValue: boolean );
    { Other members }
    procedure ClearValues; dynamic;
  public
    procedure Clear; override;
    procedure Load( Dataset: TDataSet ); override;
    procedure Retrieve( const APersonId: integer );
    procedure Populate; override;
    function AddToStudy( const AStudyId: integer; APersonId: integer ): boolean;
    function TryTransfer( const AGroupId, AStatusId: integer ): boolean;
    procedure Adopt( APersonId: integer = 0 ); {
      Adopt a person, for APersonId=0, the current person is adopted }
    function AsListbox( const ASimpleView: boolean = true ): string; override;
    function ValidRelation: boolean;
    { Methods not related to current patient }
    procedure SetTestCase( const ATestCase: boolean; APersonId: integer );
    procedure SetGroup( const AGroupId, APersonId: integer ); {
      Set group for a person (not necessarily the current one }
    procedure SetStatus( const AStatusId, APersonId: integer ); {
      Set status for a person (not necessarily the current one }
    procedure SetGroupRelation( const AStudyId, AGroupId, ARelationId: integer );
    function SelectStatus: integer; {
      Brings up a dialog box with valid status, returns -1 for no selection }
    function GetCaseList: TDataSet;
    procedure ClearRelations( const APersonId: integer );
  published
    { Study related data }
    property FinState: integer read fStatusId;
    property ClinRelId: integer read Get_ClinRelId;
    property Id: string read Get_ID;
    property RelationId: integer read Get_RelId; { alias for RelId }
    property RelationName: string read fRelName; { alias for RelationId }
    property RelId: integer read Get_RelId;
    property RelName: string read Get_RelName;
    property StatusActive: integer read fStatusActive;
    property StatusId: integer read Get_StatusId write Set_StatusId;
    property StatusText: string read Get_StatusText write fStatusText;
  end;

implementation

uses
  CRF.SQL,
  CRF.SQL.Fields,
  {General}
  Emetra.Classes.Subject.Stored,
  Emetra.Logging.Interfaces,
  {Standard}
  Variants;

const
  { Study subjects, status and group }
  TAB = #9;

resourcestring
  StrWarningAdoptFailed = 'Du kan ikke overta ansvar for pasienten:\n%s';
  StrWarningTransferFailed = 'Overføring ble ikke utført:\n%s';
  StrErrorNoStatusDefined = 'Ingen statuskoder er definert for protokollen "%s".\nKontakt leverandøren av systemet for hjelp.';
  { Select status }
  StrStatusHeader = 'Status';
  StrStatusInformation = 'Velg status fra listen';

  { TStudCase }

procedure TStudyCase.Clear;
begin
  BeginUpdate;
  try
    inherited Clear;
    fStatusId := 0;
    fStatusText := '';
    fStatusActive := 0;
    fRelId := 0;
    fRelName := EmptyStr;
  finally
    EndUpdate;
  end;
end;

procedure TStudyCase.ClearRelations( const APersonId: integer );
begin
  SQL.ExecuteCommand( CMD_CLEAR_RELATIONS, [APersonId] );
end;

procedure TStudyCase.ClearValues;
begin
  { Do nothing }
end;

procedure TStudyCase.Retrieve( const APersonId: integer );
begin
  BeginUpdate;
  try
    Clear;
    FPrimaryKey := APersonId;
    FPerson.ChangeId( FPrimaryKey );
    Populate;
  finally
    EndUpdate;
  end;
end;

procedure TStudyCase.Load( Dataset: TDataSet );
begin
  BeginUpdate;
  try
    inherited Load( Dataset );
    { Populate more details for this class }
    with Dataset do
    begin
      fRelId := ReadInteger( Dataset, FLD_RELATION_ID );
      fRelName := ReadString( Dataset, FLD_RELATION_NAME );
      fStatusActive := ReadInteger( Dataset, FLD_STATUS_ACTIVE );
      fStatusId := ReadInteger( Dataset, FLD_STATUS_ID );
      fStatusText := ReadString( Dataset, FLD_STATUS_TEXT );
      fClinRelId := ReadInteger( Dataset, FLD_CLIN_RELATION_ID, 0 );
      fIsTestCase := ReadBool( Dataset, FLD_TEST_CASE, false );
    end;
  finally
    EndUpdate;
  end;
end;

function TStudyCase.ValidRelation: boolean;
begin
  Result := fRelName <> EmptyStr;
end;

procedure TStudyCase.Populate;
begin
  Assert( Assigned( SQL ), Format( 'SQL Must be assigned before %s can be populated', [ClassName] ) );
  BeginUpdate;
  try
    if PersonId = 0 then
      Clear
    else
      with SQL.FastQuery( QRY_LOAD_STUDYCASE, [FStudyContext.StudyId, PersonId] ) do
        try
          Load( SQL.Dataset );
        finally
          Close;
        end;
  finally
    EndUpdate;
  end;
end;

procedure TStudyCase.SetGroupRelation( const AStudyId, AGroupId, ARelationId: integer );
begin
  SQL.ExecuteCommand( CMD_ADD_GROUP_RELATION, [AStudyId, AGroupId, ARelationId] );
end;

procedure TStudyCase.SetStatus( const AStatusId, APersonId: integer );
begin
  SQL.ExecuteCommand( CMD_UPD_STATUS, [StudyId, APersonId, AStatusId] );
  if ( APersonId = PersonId ) and ( AStatusId <> fStatusId ) then
    Populate;
end;

procedure TStudyCase.SetTestCase( const ATestCase: boolean; APersonId: integer );
begin
  SQL.ExecuteCommand( CMD_UPDATE_TESTCASE, [APersonId, ATestCase] );
  if APersonId = PersonId then
    Populate;
end;

procedure TStudyCase.Set_StatusId( const AStatusId: integer );
begin
  BeginUpdate;
  try
    SetStatus( AStatusId, PersonId );
  finally
    EndUpdate;
  end;
end;

procedure TStudyCase.Set_GroupId( const AGroupId: integer );
begin
  BeginUpdate;
  try
    SetGroup( AGroupId, PersonId );
  finally
    EndUpdate;
  end;
end;

procedure TStudyCase.Set_IsTestCase( const AValue: boolean );
begin
  BeginUpdate;
  try
    SetTestCase( AValue, PersonId );
  finally
    EndUpdate;
  end;

end;

function TStudyCase.GetCaseList: TDataSet;
begin
  Result := SQL.FastQuery( QRY_ACTIVE_ANONYMOUS, [FStudyContext.StudyId] );
  FDataset := Result;
end;

function TStudyCase.Get_ID: string;
begin
  Result := IntToStr( PersonId );
end;

function TStudyCase.Get_IsTestCase: boolean;
begin
  Result := fIsTestCase;
end;

function TStudyCase.Get_ClinRelId: integer;
begin
  Result := fClinRelId;
end;

function TStudyCase.Get_HPRNo: integer;
begin
  Result := FPerson.HPRNo;
end;

function TStudyCase.Get_RelId: integer;
begin
  Result := fRelId;
end;

function TStudyCase.Get_RelName: string;
begin
  Result := fRelName;
end;

function TStudyCase.Get_StatusId: integer;
begin
  Result := fStatusId;
end;

function TStudyCase.Get_StatusText: string;
begin
  Result := fStatusText;
end;

function TStudyCase.Get_VMR: TObject;
begin
  Result := nil;
end;

function TStudyCase.AddToStudy( const AStudyId: integer; APersonId: integer ): boolean;
begin
  Result := false;
  if APersonId = 0 then
    APersonId := PersonId;
  if APersonId > 0 then
    try
      SQL.ExecuteCommand( CMD_ADD_STUDY_CASE, [AStudyId, APersonId] );
      Result := true;
    except
      on E: Exception do
        Log.Event( E.Message );
    end;
end;

procedure TStudyCase.Adopt( APersonId: integer = 0 );
begin
  if APersonId = 0 then
    APersonId := PersonId;
  if APersonId = 0 then
    exit;
  try
    SQL.ExecuteCommand( CMD_UPD_CASE_ADOPT, [FStudyContext.StudyId, APersonId] );
  except
    on E: Exception do
      Log.Event( StrWarningAdoptFailed, [E.Message], ltWarning );
  end;
end;

function TStudyCase.AsListbox( const ASimpleView: boolean ): string;
begin
  Result := Format( '%d', [PersonId] ) + TAB;
  if DOB > 0 then
    Result := Result + DateToStr( DOB );
  Result := Trim( Result + TAB + ReverseName + TAB + GroupName + TAB + StatusText );
end;

function TStudyCase.SelectStatus: integer;
begin
  Result := PickList.SelectInteger( QRY_STUDY_STATUS, [FStudyContext.StudyId], StrStatusHeader, StrStatusInformation,
    Format( StrErrorNoStatusDefined, [FStudyContext.StudyName] ) + MSG_SUFFIX_CONTACT_SUPPORT_LOCAL, true, ltError );
end;

procedure TStudyCase.SetGroup( const AGroupId, APersonId: integer );
begin
  SQL.ExecuteCommand( CMD_UPD_GROUP, [StudyId, APersonId, AGroupId] );
  if APersonId = PersonId then
    Populate;
end;

function TStudyCase.TryTransfer( const AGroupId: integer; const AStatusId: integer ): boolean;
begin
  Result := false;
  BeginUpdate;
  try
    try
      SQL.ExecuteCommand( CMD_STUDYCASE_TRANSFER, [StudyId, PersonId, AGroupId, AStatusId] );
      Populate;
      Result := ( GroupId = AGroupId );
    except
      on E: Exception do
        CheckPermissionProblem( E, StrWarningTransferFailed );
    end;
  finally
    EndUpdate;
  end;
end;

end.
