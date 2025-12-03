unit CRF.User.StudyUser;

interface

uses
  {Project}
  CRF.User.Interfaces,
  CRF.Person,
  Emetra.Person.SQL,
  Emetra.Profession.Interfaces,
  {Standard}
  Data.Db,
  System.Classes, System.SysUtils;

type
  TStudyUser = class( TCustomStudyPerson, ICRFUser, ICRFUserEducation, IProfession )
  private
    fHPRNo: integer;
    fUserId: integer;
    fUserName: string;
    fSignature: string;
    fProfName: string;
    fProfId: integer;
    fProfType: string;
    fCaseList: integer;
  protected
    { Accessors }
    function Get_CaseList: integer;
    function Get_HPRNo: integer;
    function Get_ProfessionName: string;
    function Get_ProfId: integer;
    function Get_ProfType: string;
    function Get_Signature: string;
    function Get_UserId: integer;
    function Get_UserName: string;
    procedure Set_HPRNo( const Value: integer );
    procedure Set_UserId( const Value: integer );
    procedure Set_UserName( const AValue: string );
    procedure Set_GroupId( const AValue: integer );
    { Other members }
    function GetData: TDataset; override;
    function GetDataById: TDataset;
    function ShowAndSelectCenter: integer;
    procedure SetAddress( const AStreetAddress, APostcode, ACity: string );
  public
    function AddUser( const AUsername, APassword: string ): boolean;
    function HealthCareProfessionalAtCollegeLevel: boolean;
    procedure Clear; override;
    procedure Load( ADataset: TDataset ); override;
    procedure RevokeAccess;
    procedure RecreateUser;
    procedure SelectCenter; dynamic;
    procedure SelectProfession; dynamic;
    procedure MapToPerson( const APersonId: integer );
    procedure SelectStudyUser( const AStudyId, AUserId: integer );
  published
    property CaseList: integer read Get_CaseList;
    property HPRNo: integer read Get_HPRNo write Set_HPRNo;
    property OID9060: string read fProfType;
    property GroupId: integer read Get_GroupId write Set_GroupId;
    property Profession: string read Get_ProfessionName;
    property ProfessionId: integer read Get_ProfId;
    property ProfessionName: string read Get_ProfessionName;
    property ProfessionType: string read Get_ProfType;
    property Signature: string read Get_Signature;
    property UserId: integer read Get_UserId write Set_UserId;
    property UserName: string read Get_UserName write Set_UserName;
    { Deprecated properties }
    property ProfId: integer read Get_ProfId;
    property ProfName: string read Get_ProfessionName;
  end;

resourcestring
  TXT_MY_CENTER = 'Mine arbeidssteder';
  TXT_SELECT_ONE_CENTER = 'Velg ett av arbeidsstedene og klikk OK.';
  TXT_USER_CENTER = 'Brukerens arbeidssted';
  TXT_PROFESSION = 'Velg profesjon';
  TXT_SELECT_ONE = 'Marker ett valg og klikk OK.';
  WARN_SET_CENTER = 'Informasjon om arbeidssted kunne ikke oppdateres:\n%s';
  WARN_SET_PROFESS = 'Informasjon om profesjon kunne ikke oppdateres:\n%s';
  WARN_NO_CENTERS = 'Ingen arbeidssteder er definert!';

implementation

uses
  CRF.SQL,
  CRF.SQL.Fields,
  {General}
  Emetra.Logging.Interfaces,
  Emetra.Classes.Subject.Stored,
  {Standard}
  System.RegularExpressions;

resourcestring
  { Recreate user }
  ASK_RECREATE_USER =
  { } 'Gjenoppretting skal utføres hvis brukere\n' +
  { } 'får melding om at brukerkonto mangler.\n' +
  { } 'Roller må tildeles på nytt etter gjenoppretting.\n' +
  { } 'Vil du gjenopprette brukeren nå?';
  MSG_RECREATE_SUCCESSFUL =
  { } 'Gjenoppretting av %s var vellykket.\n' +
  { } 'Etter gjenoppretting må du angi yrket til brukeren på nytt.\n' +
  { } 'I tillegg må gi brukeren tilbake eventuelle spesialroller!';
  EXC_RECREATE_NOT_ALLOWED =
  { } 'Denne brukeren kan ikke gjenopprettes.\n' +
  { } 'USER_ID()=%d er ikke en vanlig brukerkonto.';
  ERR_USERID_MISMATCH =
  { } 'Brukeren %s fikk ny USER_ID() ved gjenoppretting.\n' +
  { } 'Ikke opprett nye brukere nå, det kan lage mer problemer.\n' +
  { } 'Kontakt DIPS Kundestøtte snarest for bistand !';

  { Other }
  ERR_USER_CREATE = 'Brukeren kunne ikke opprettes:\n%s';
  MSG_USER_CREATED = 'Brukeren %s er opprettet og gitt tilgang til databasen.';
  WARN_MAP_USER = 'Brukeren kunne ikke kobles til denne personen:\n%s';
  WARN_NO_PROFESSIONS =
  { } 'Det finnes ingen tilgjengelige profesjoner i denne databasen.\n' +
  { } 'Du må kontakt systemansvarlig eller leverandøren for bistand.';

  { College professions }
  StrCollegeProfessions = '^(LE|SP|ET|FT|BI|KE|VP)$';

  { TStudyUser }

procedure TStudyUser.SelectProfession;
var
  NewProfessionId: integer;
begin
  NewProfessionId := PickList.SelectInteger( QRY_ACTIVE_PROFESSIONS, [], TXT_PROFESSION, TXT_SELECT_ONE, WARN_NO_PROFESSIONS );
  if ( NewProfessionId > 0 ) and ( NewProfessionId <> fProfId ) then
    try
      SQL.ExecuteCommand( CMD_UPD_PROFESSION, [fUserId, NewProfessionId] );
      Populate;
    except
      on E: Exception do
        CheckPermissionProblem( E, WARN_SET_PROFESS );
    end;
end;

procedure TStudyUser.SelectStudyUser(const AStudyId, AUserId: integer);
begin
  Load( SQL.FastQuery( QRY_ANY_STUDY_USER, [AStudyId, AUserId] ) );
end;

procedure TStudyUser.SelectCenter;
var
  NewCenterId: integer;
begin
  NewCenterId := ShowAndSelectCenter;
  if ( NewCenterId > 0 ) and ( NewCenterId <> CenterId ) then
    try
      SQL.ExecuteCommand( CMD_UPD_USER_CENTER, [fUserId, NewCenterId] ); { Update for other user [dbo] }
      Populate;
    except
      on E: Exception do
        CheckPermissionProblem( E, WARN_SET_CENTER );
    end;
end;

function TStudyUser.ShowAndSelectCenter: integer;
begin
  Result := PickList.SelectInteger( QRY_ALL_CENTERS, [], TXT_USER_CENTER, TXT_SELECT_ONE_CENTER, WARN_NO_CENTERS );
end;

function TStudyUser.GetData: TDataset;
begin
  Result := SQL.FastQuery( QRY_USER_DATA, [fUserName, StudyId] );
end;

function TStudyUser.GetDataById: TDataset;
begin
  Result := SQL.FastQuery( QRY_USER_DATA, [fUserId, StudyId] );
end;

function TStudyUser.Get_CaseList: integer;
begin
  Result := fCaseList;
end;

function TStudyUser.Get_HPRNo: integer;
begin
  Result := fHPRNo;
end;

function TStudyUser.Get_ProfessionName: string;
begin
  Result := fProfName;
end;

function TStudyUser.Get_ProfId: integer;
begin
  Result := fProfId;
end;

function TStudyUser.Get_ProfType: string;
begin
  Result := fProfType;
end;

function TStudyUser.Get_Signature: string;
begin
  Result := fSignature;
end;

function TStudyUser.Get_UserId: integer;
begin
  Result := fUserId;
end;

function TStudyUser.Get_UserName: string;
begin
  Result := fUserName;
end;

function TStudyUser.HealthCareProfessionalAtCollegeLevel: boolean;
begin
  Result := TRegEx.IsMatch( fProfType, StrCollegeProfessions );
end;

procedure TStudyUser.MapToPerson( const APersonId: integer );
begin
  try
    SQL.ExecuteCommand( CMD_UPD_USER_PERSON, [fUserId, APersonId] );
    Populate;
  except
    on E: Exception do
      CheckPermissionProblem( E, WARN_MAP_USER );
  end;
end;

procedure TStudyUser.Load( ADataset: TDataset );
begin
  BeginUpdate;
  try
    inherited Load( ADataset );
    fUserId := ReadInteger( FLD_USER_ID );
    fUserName := ReadString( FLD_USER_NAME );
    fHPRNo := ReadInteger( FLD_HPR_NO );
    fSignature := ReadString( FLD_SIGNATURE );
    fProfId := ReadInteger( FLD_PROF_ID );
    fProfName := ReadString( FLD_PROF_NAME );
    fProfType := ReadString( FLD_PROF_TYPE );
    fCaseList := ReadInteger( FLD_CASELIST );
  finally
    EndUpdate;
  end;
end;

procedure TStudyUser.RecreateUser;
var
  oldUserId: integer;
  newUserId: integer;
begin
  if Log.LogYesNo( ASK_RECREATE_USER, ltWarning ) then
    try
      with SQL.FastQuery( QRY_USER_ID, [fUserName] ) do
        try
          oldUserId := Fields[0].AsInteger;
        finally
          Close;
        end;
      if oldUserId < 2 then
        raise Exception.CreateFmt( EXC_RECREATE_NOT_ALLOWED, [oldUserId] )
      else
      begin
        SQL.ExecuteCommand( CMD_REVOKE_DBACCESS, [fUserName] );
        SQL.ExecuteCommand( CMD_GRANT_DBACCESS, [fUserName] );
        SQL.ExecuteCommand( Format( CMD_GRANT_FASTTRAK_ROLE, [fUserName] ) );
        with SQL.FastQuery( QRY_USER_ID, [fUserName] ) do
          try
            newUserId := Fields[0].AsInteger;
          finally
            Close;
          end;
      end;
      if oldUserId <> newUserId then
        Log.Event( ERR_USERID_MISMATCH, [fUserName], ltError )
      else
        Log.Event( MSG_RECREATE_SUCCESSFUL, [fUserName], ltMessage );
    except
      on E: Exception do
        Log.Event( E.Message, ltError );
    end;
end;

procedure TStudyUser.RevokeAccess;
begin
  SQL.ExecuteCommand( CMD_DELETE_USER, [fUserName] );
end;

procedure TStudyUser.SetAddress( const AStreetAddress, APostcode, ACity: string );
begin
  FPerson.SetAddress( AStreetAddress, APostcode, ACity );
end;

procedure TStudyUser.Set_GroupId( const AValue: integer );
begin
  SQL.ExecuteCommand( CMD_UPD_USER_GROUP, [fStudyId, fUserId, AValue] );
  Populate;
end;

procedure TStudyUser.Set_HPRNo( const Value: integer );
begin
  if Value = fHPRNo then
    exit;
  BeginUpdate;
  try
    SQL.ExecuteCommand( CMD_UPDATE_HPR, [PersonId, Value] );
    fHPRNo := Value;
  finally
    EndUpdate;
  end;
end;

procedure TStudyUser.Set_UserId( const Value: integer );
begin
  if Value = fUserId then
    exit;
  BeginUpdate;
  try
    Clear;
    fUserId := Value;
    if fUserId <> 0 then
    begin
      Load( GetDataById );
      CloseDataset;
    end;
  finally
    EndUpdate;
  end;
end;

procedure TStudyUser.Set_UserName( const AValue: string );
begin
  if AValue = fUserName then
    exit;
  BeginUpdate;
  try
    Clear;
    fUserName := AValue;
    if fUserName <> EmptyStr then
    begin
      Load( GetData );
      CloseDataset;
    end;
  finally
    EndUpdate;
  end;
end;

procedure TStudyUser.Clear;
begin
  fUserId := 0;
  fUserName := EmptyStr;
  fProfName := EmptyStr;
  fSignature := EmptyStr;
  fHPRNo := 0;
  inherited Clear;
end;

function TStudyUser.AddUser( const AUsername, APassword: string ): boolean;
begin
  Result := false;
  try
    SQL.ExecuteCommand( CMD_ADD_SQL_USER, [AUsername, APassword] );
    Result := true;
    Log.Event( MSG_USER_CREATED, [AUsername], ltMessage );
  except
    on E: Exception do
      CheckPermissionProblem( E, ERR_USER_CREATE );
  end;
end;

end.
