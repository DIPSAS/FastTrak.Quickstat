unit CRF.Context.ActiveUser;

interface

uses
  {Project}
  CRF.User.Interfaces,
  CRF.Context.Session.Interfaces,
  CRF.User.StudyUser,
  CRF.Study.Interfaces,
  {General}
  Emetra.Database.Interfaces,
  Emetra.Interfaces.Observer,
  Emetra.Person.Interfaces,
  {Standard}
  Data.Db, System.SysUtils;

type
  TActiveUser = class( TStudyUser, ICRFActiveUser, IDatabaseUser, IListener, ILoginObserver, IStudyObserver )
    { User data }
  strict private
    fBlockRules: integer;
    fDbOwner: boolean;
    fPassword: string;
    fRelationCount: integer;
    fShowMyGroup: boolean;
    fSingleGroupUser: boolean;
    fSuperuser: boolean;
  private
    { IStudyObserver }
    procedure AfterStudyChange( const Sender: IStudyId );
    function FriendlyName: string;
    { IListener }
    procedure AfterUpdate( Sender: TObject );
    { ILoginObserver }
    procedure AfterLogin( Sender: IDatabaseConnection );
  protected
    { Property accessors }
    function Get_DbOwner: boolean;
    function Get_Password: string;
    function Get_RelationCount: integer;
    function Get_Superuser: boolean;
    function Get_ShowMyGroup: boolean;
    procedure Set_ShowMyGroup( const AValue: boolean );
    { Other members }
    function GetData: TDataset; override;
    function ShowAndSelectGroup: integer;
  public
    { Initialization }
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { Other members }
    function AddMyself( const APerson: IPersonReadOnly ): integer;
    function ChangePassword( const ANewPassword: string ): boolean;
    function SelectGroup: integer; override;
    function SetCenter( const ACenterId: integer ): boolean;
    procedure Clear; override;
    procedure Load( ADataset: TDataset ); override;
    procedure Populate; override;
    procedure SelectCenter; override; { Override from TStudyUser, because different SP is used for MyUser }
    procedure SelectProfession; override; { Override from TStudyUser, because different SPs are used for MyUser }
    procedure SetCredentials( const AUser, APwd: string );
  published
    property BlockRules: integer read fBlockRules;
    property DbOwner: boolean read Get_DbOwner;
    property RelationCount: integer read Get_RelationCount;
    property Superuser: boolean read Get_Superuser;
    property SingleGroup: boolean read fSingleGroupUser;
    property ShowMyGroup: boolean read Get_ShowMyGroup write Set_ShowMyGroup;
  end;

implementation

uses
  {CRF}
  CRF.SQL,
  CRF.SQL.Fields,
  {General}
  Emetra.Logging.Interfaces;

resourcestring
  TXT_GROUP = 'Gruppe';
  ERR_ADD_MYSELF = 'Kunne ikke oppdatere egne personalia:\n%s';
  ERR_CHANGE_PWD = 'Passordet kunne dessverre ikke endres.\n' + 'Melding: %s';
  MSG_PWD_CHANGED = 'Passordet er endret.';

  MSG_SET_PROFESSION =
  { } 'Du har ikke oppgitt yrket ditt tidligere.\n' +
  { } 'Noen av funksjonene i programmet er avhengig\n' +
  { } 'av yrket du oppgir. Det kan endres senere.';
  MSG_SET_CENTER =
  { } 'Du har ikke valgt et arbeidssted tidligere.\n' +
  { } 'Hvilke personer/pasienter du har tilgang til er\n' +
  { } 'avhengig av arbeidssted.  Du må velge dette nå.';

  WARN_SET_GROUP = 'Informasjon om gruppe kunne ikke oppdateres:\n%s';
  ERR_UPD_SHOWGROUP = 'Innstillingen kunne ikke lagres:\n%s';

  WARN_NO_OTHER_PROFESSIONS = 'Du kan ikke bytte til et annet yrke.';

procedure TActiveUser.AfterConstruction;
begin
  FSingleton := true;
  inherited;
end;

procedure TActiveUser.BeforeDestruction;
begin
  DetachAll;
  inherited;
end;

procedure TActiveUser.Clear;
begin
  BeginUpdate;
  try
    inherited Clear;
    fShowMyGroup := false;
    fBlockRules := 0;
  finally
    EndUpdate;
  end;
end;

function TActiveUser.FriendlyName: string;
begin
  Result := 'Aktiv bruker';
end;

procedure TActiveUser.AfterLogin( Sender: IDatabaseConnection );
begin
  Assert( Assigned( FStudyContext ), 'StudyContext must be injected in the constructor' );
  if Sender.Connected and ( FStudyContext.StudyName <> '' ) then
    Populate
  else
    Clear;
end;

procedure TActiveUser.AfterStudyChange( const Sender: IStudyId );
const
  PROC_NAME = 'AfterStudyChange';
begin
  Log.EnterMethod( Self, Format( '%s: %d: %s', [PROC_NAME, Sender.StudyId, Sender.StudyName] ) );
  try
    Assert( Sender.StudyName = FStudyContext.StudyName );
    if SQL.Connected then
    begin
      Populate;
      Log.SilentSuccess( '%s.%s: Welcome %d %s, %s at %s', [ClassName, PROC_NAME, Self.PersonId, Self.FullName, Self.Profession, Self.CenterName] );
    end
    else
    begin
      Log.Event( '%s.%s: Not connected, clearing data', [ClassName, PROC_NAME] );
      Clear;
    end;
  finally
    Log.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TActiveUser.AfterUpdate( Sender: TObject );
begin
  Populate;
end;

function TActiveUser.GetData: TDataset;
begin
  Result := SQL.FastQuery( QRY_MY_STUDYUSER, [FStudyContext.StudyName] )
end;

function TActiveUser.Get_DbOwner: boolean;
begin
  Result := fDbOwner;
end;

function TActiveUser.Get_Password: string;
begin
  Result := fPassword;
end;

function TActiveUser.Get_RelationCount: integer;
begin
  Result := fRelationCount;
end;

function TActiveUser.Get_ShowMyGroup: boolean;
begin
  Result := fShowMyGroup;
end;

function TActiveUser.Get_Superuser: boolean;
begin
  Result := fSuperuser;
end;

function TActiveUser.ShowAndSelectGroup: integer;
begin
  Result := PickList.SelectInteger( QRY_MY_STUDY_GROUPS, [StudyId], TXT_GROUP, TXT_SELECT_ONE, MissingGroupsMessage );
end;

procedure TActiveUser.Populate;
begin
  if FStudyContext.StudyName = EmptyStr then
    Clear
  else
  begin
    Load( GetData );
    FDataset.Close;
    if ProfName = EmptyStr then
    begin
      Log.Event( MSG_SET_PROFESSION, ltMessage );
      SelectProfession;
    end;
    if CenterName = EmptyStr then
    begin
      Log.Event( MSG_SET_CENTER, ltMessage );
      SelectCenter;
    end;
  end;
end;

procedure TActiveUser.Load( ADataset: TDataset );
begin
  BeginUpdate;
  try
    inherited Load( ADataset );
    fShowMyGroup := ReadBool( FLD_SHOW_MY_GROUP );
    fBlockRules := ReadInteger( FLD_BLOCK_RULES );
    fStudyId := ReadInteger( FLD_STUDY_ID );
    { Load roles }
    fSuperuser := ( ReadInteger( FLD_SUPERUSER ) = 1 );
    fDbOwner := ( ReadInteger( FLD_DB_OWNER ) = 1 );
    fSingleGroupUser := ( ReadInteger( FLD_SINGLE_GROUP_USER ) = 1 );
    { Count relations }
    fRelationCount := ReadInteger( ADataset, FLD_RELATION_COUNT, -1 );
    fStudyContext.StudyId := fStudyId;
  finally
    EndUpdate;
  end;
end;

function TActiveUser.ChangePassword( const ANewPassword: string ): boolean;
begin
  Result := false;
  if ( fPassword = ANewPassword ) then
    exit;
  try
    SQL.ExecuteCommand( CMD_CHG_MY_PASSWORD, [fPassword, ANewPassword] );
    fPassword := ANewPassword;
    Result := true;
    Log.Event( MSG_PWD_CHANGED, ltMessage );
  except
    on E: Exception do
      CheckPermissionProblem( E, ERR_CHANGE_PWD );
  end;
end;

function TActiveUser.AddMyself( const APerson: IPersonReadOnly ): integer;
begin
  Result := 0;
  try
    SQL.ExecuteCommand( CMD_ADD_MYSELF, [APerson.Dob, APerson.GenderId, APerson.FirstName, APerson.LastName, APerson.NationalId] );
    Populate;
    Result := PersonId;
  except
    on E: Exception do
      CheckPermissionProblem( E, ERR_ADD_MYSELF );
  end;
end;

procedure TActiveUser.SelectCenter;
var
  NewCenterId: integer;
begin
  NewCenterId := PickList.SelectInteger( QRY_MY_CENTERS, [], TXT_MY_CENTER, TXT_SELECT_ONE_CENTER, WARN_NO_CENTERS );
  if ( NewCenterId > 0 ) and ( NewCenterId <> CenterId ) then
    SetCenter( NewCenterId );
end;

function TActiveUser.SelectGroup: integer;
var
  NewGroupId: integer;
begin
  Result := GroupId;
  NewGroupId := ShowAndSelectGroup;
  if ( GroupId > 0 ) and ( NewGroupId < 0 ) then
    Log.Event( '%s.SelectGroup: Dialog was canceled, but GroupId=%d was already selected and we keep it.', [ClassName, GroupId] )
  else if ( NewGroupId <> GroupId ) or ( ( GroupId < 1 ) and ( NewGroupId < 1 ) ) then
    try
      SQL.ExecuteCommand( CMD_UPD_MY_GROUP, [StudyId, NewGroupId] );
      Populate; { Must reload group name }
      Result := NewGroupId;
    except
      on E: Exception do
        CheckPermissionProblem( E, WARN_SET_GROUP );
    end;
end;

procedure TActiveUser.SelectProfession;
var
  NewProfId: integer;
begin
  NewProfId := PickList.SelectInteger( QRY_MY_PROFESSIONS, [], TXT_PROFESSION, TXT_SELECT_ONE, WARN_NO_OTHER_PROFESSIONS, false );
  if ( NewProfId <> ProfId ) and ( NewProfId > 0 ) then
    try
      if ProfId <= 0 then
        SQL.ExecuteCommand( CMD_ADD_MY_PROFESSION, [NewProfId] )
      else
        SQL.ExecuteCommand( CMD_UPD_MY_PROFESSION, [NewProfId] );
      Populate;
    except
      on E: Exception do
        CheckPermissionProblem( E, WARN_SET_PROFESS );
    end;
end;

function TActiveUser.SetCenter( const ACenterId: integer ): boolean;
begin
  if ACenterId <> CenterId then
    try
      if CenterId > 0 then
        SQL.ExecuteCommand( CMD_UPD_MY_CENTER, [ACenterId] )
      else
        SQL.ExecuteCommand( CMD_ADD_MY_CENTER, [ACenterId] );
      Populate;
    except
      on E: Exception do
        CheckPermissionProblem( E, WARN_SET_CENTER );
    end;
  Result := ACenterId = CenterId;
end;

procedure TActiveUser.SetCredentials( const AUser, APwd: string );
begin
  if ( UserName = AUser ) and ( fPassword = APwd ) then
    exit;
  BeginUpdate;
  try
    UserName := AUser;
    fPassword := APwd;
  finally
    EndUpdate;
  end;
end;

procedure TActiveUser.Set_ShowMyGroup( const AValue: boolean );
begin
  if ( AValue = fShowMyGroup ) then
    exit;
  BeginUpdate;
  try
    SQL.ExecuteCommand( CMD_UPD_SHOW_MY_GROUP, [StudyId, AValue] );
    fShowMyGroup := AValue;
  except
    on E: Exception do
      CheckPermissionProblem( E, ERR_UPD_SHOWGROUP );
  end;
  EndUpdate;
end;

end.
