unit CRF.Context.Facade;

interface

uses
  {Project}
  CRF.Input.Interfaces,
  CRF.Context.Interfaces,
  CRF.Context.Session.Interfaces,
  CRF.User.Interfaces,
  CRF.Study.Interfaces,
  CRF.Person.StudyCase.Interfaces,
  CRF.Person.StudyCase.AccessControl,
  CRF.Person.MoveInterface,
  CRF.Context.Session,
  CRF.Context.ActiveUser,
  CRF.Context.ActiveCase,
  CRF.Context.StudyCase.AccessLog,
  CRF.Input.EventMap,
  {General}
  Emetra.Person.Manager,
  Emetra.Database.Simple,
  Emetra.Database.Info,
  Emetra.ObjectContainer,
  Emetra.Profession.Interfaces,
  {General interfaces}
  Emetra.ObjectContainer.Interfaces,
  Emetra.Database.Interfaces,
  Emetra.Interfaces.Observer,
  {Standard}
  Generics.Collections,
  Classes, Contnrs, SysUtils;

type
  /// <summary>
  ///   This is a facade class, tying together the basic concepts in a CRF
  ///   context: A Database (TSimpleDatabase),version information for that
  ///   database (TDatabaseInfo), a TActiveUser, a study and a patient
  ///   (TActiveCase). Some utility classes are included. They handle access
  ///   control and logging of that access, person management (TPersonManager)
  ///   and study information with file system data
  ///   (TCRFStudyContextFileSystem).
  /// </summary>
  TCRFSimpleContext = class( TObjectContainer, ISQL, IDatabaseInfo, IStudyId, IStudySession, IStudyContext, IStudyLoginContext, IStudySelector,
    IStudyFileSystemContext, ICRFUser, ICRFEventMap, IDatabaseLoginContext, ICRFActiveUser, IDatabaseUser, IActiveStudyCase, IObjectContainer,
    IStudyCaseAccessControl, ICRFContext, IProfession, ICRFSelectCase )
  strict private
    fDb: TSimpleDatabase;
    fDbInfo: TDatabaseInfo;
    fEventMap: TCRFEventMapper;
    fAccessControl: TStudyCaseAccessControl;
    fActiveCase: TActiveCase;
    fActiveCaseAccessLogger: IListener;
    fActiveUser: TActiveUser;
    fPersonManager: TPersonManager;
    fSession: TCRFStudyContextFileSystem;
  private
    { Create objects }
    procedure CreateDBS;
    procedure CreateSessionObject;
    procedure CreateUserObject;
  protected
    { Property accessors }
    function Get_CaseId: integer;
    function Get_CenterId: integer;
    function Get_SessId: integer;
    function Get_StudyCaseTransfer: ICRFStudyCaseTransfer;
    function Get_StudyId: integer;
    function Get_StudyName: string;
    function Get_UserId: integer;
    procedure Set_StudyCaseTransfer( AStudyCaseTransfer: ICRFStudyCaseTransfer );
  public
    { Initialization }
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { Other methods }
    function Connected: boolean;
    function Connect( const AContext: string; AConnString: string = '' ): boolean; dynamic;
    function Select( const APersonId: integer ): boolean;
    function TryGetClass( const AClass: TClass; out AObject: TObject ): boolean;
    procedure Disconnect;
    procedure GetAllImplementors( AInterface: TGuid; AList: TList );
    procedure SetContext( const AContext: string );
    procedure SelectStudy( Sender: TObject );
    procedure Refresh;
    { Object Properties }
    property AccessControl: TStudyCaseAccessControl read fAccessControl implements IStudyCaseAccessControl;
    property ActiveCase: TActiveCase read fActiveCase implements IActiveStudyCase;
    property Database: TSimpleDatabase read fDb implements ISQL, IDatabaseLoginContext;
    property DatabaseInfo: TDatabaseInfo read fDbInfo implements IDatabaseInfo;
    property EventMap: TCRFEventMapper read fEventMap implements ICRFEventMap;
    property PersonManager: TPersonManager read FPersonManager;
    property Session: TCRFStudyContextFileSystem read fSession implements IStudyContext, IStudyId, IStudySession, IStudyFileSystemContext, IStudyLoginContext;
    property User: TActiveUser read fActiveUser implements ICRFActiveUser, ICRFUser, IDatabaseUser, IProfession;
    { Properties that must be set }
    property StudyCaseTransfer: ICRFStudyCaseTransfer read Get_StudyCaseTransfer write Set_StudyCaseTransfer;
  published
    { Primitive Properties }
    property CenterId: integer read Get_CenterId;
    property StudyId: integer read Get_StudyId;
    property StudyName: string read Get_StudyName;
    property UserId: integer read Get_UserId;
    property SessId: integer read Get_SessId;
    property CaseId: integer read Get_CaseId;
  end;

implementation

uses
  CRF.Input.Item.DbLink,
  {General}
  Emetra.Classes.Auditing,
  Emetra.Logging.Interfaces;

{$REGION 'Initialization'}

procedure TCRFSimpleContext.CreateDBS;
begin
  fDb := TSimpleDatabase.Create( Log );
  fDbInfo := TDatabaseInfo.Create( fDb, Log );
  fEventMap := TCRFEventMapper.Create;
  RegisterObject( 'DB', fDb );
  RegisterObject( 'EventMap', fEventMap );
  RegisterObject( 'DbInfo', fDbInfo );
end;

procedure TCRFSimpleContext.CreateSessionObject;
begin
  fSession := TCRFStudyContextFileSystem.Create( EmptyStr, Log );
  RegisterObject( 'Session', fSession );
  RegisterObject( 'Protocol', fSession );
  if not Supports( fSession, IStudyContext ) then
    raise ENotSupportedException.Create( 'TCRFDiskContext does not support IStudyContext' );
  if not Supports( fSession, IStudyLoginContext ) then
    raise ENotSupportedException.Create( 'TCRFDiskContext does not support IStudyLoginContext' );
  if not Supports( fSession, IStudyFileSystemContext ) then
    raise ENotSupportedException.Create( 'TCRFDiskContext does not support IStudyFileSystemContext' );
end;

procedure TCRFSimpleContext.CreateUserObject;
begin
  fActiveUser := TActiveUser.Create( fSession, fDb, Log );
  RegisterObject( 'User', fActiveUser );
  if not Supports( fActiveUser, ICRFActiveUser ) then
    raise ENotSupportedException.Create( 'TActiveUser does not support ICRFActiveUser' );
end;

procedure TCRFSimpleContext.AfterConstruction;
const
  PROC_NAME = 'AfterConstruction';
begin
  Log.EnterMethod( Self, PROC_NAME );
  try
    inherited;
    CreateSessionObject;
    CreateDBS;
    CreateUserObject;
    { There are circular references between fAccessContol and fActiveCase.
      but fAccessControl needs the ActiveCase when access is first requested,
      so we don't pass it in the constructor here. }
    fAccessControl := TStudyCaseAccessControl.Create( fActiveUser, fDb, Log );
    fActiveCase := TActiveCase.Create( fAccessControl, fActiveUser );
    fActiveCaseAccessLogger := TActiveCaseAccessLogger.Create( fActiveUser, fDb, Log );
    fActiveCase.AddActiveCaseObserver( TObject( fActiveCaseAccessLogger ) );
    FPersonManager := TPersonManager.Create( fDb, Log );

    RegisterObject( 'Patient', fActiveCase );
    RegisterObject( 'VMR', fActiveCase.VMR );
    fDb.AddLoginObserver( fActiveUser );
    fDb.AddLoginObserver( fDbInfo ); { Sets date format }
    fDb.AddLoginObserver( fEventMap );
    fDb.AddLoginObserver( fSession );
    fSession.AddStudyObserver( fActiveUser );
    TDbLink.EventMap := fEventMap;
  finally
    Log.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TCRFSimpleContext.BeforeDestruction;
const
  PROC_NAME = 'BeforeDestruction';
begin
  Log.EnterMethod( Self, PROC_NAME );
  try
    FDictionary.Clear;
    try
      TDbLink.EventMap := nil;
      if Assigned( fDb ) and ( fDb.Connected ) then
        fDb.Disconnect;
      fDb.RemoveAllLoginObservers;
      fSession.RemoveAllStudyObservers;
      fActiveCase.PrepareToDestroy; // This is needed to avoid AV because of circular references between fAccessControl and fActiveCase.
      SafeFree( FPersonManager );
      fActiveCaseAccessLogger := nil; // Holds reference to fActiveUser and must be nilled before the user is freed.
      SafeFree( fAccessControl ); // Access control holds reference to fActiveCase }
      SafeFree( fActiveCase ); // fActiveCase holds reference to fAccessControl and fActiveUser
      SafeFree( fActiveUser );
      SafeFree( fDbInfo );
      SafeFree( fEventMap );
      SafeFree( fSession );
      SafeFree( fDb );
    except
      on E: Exception do
        Log.SilentError( '%s.%s', [ClassName, E.Message] )
    end;
    inherited;
  finally
    Log.LeaveMethod( Self, PROC_NAME );
  end;
end;

{$ENDREGION}
{$REGION 'Connect and disconnect'}

function TCRFSimpleContext.Connect( const AContext: string; AConnString: string = '' ): boolean;
begin
  EnterMethod( 'Connect' );
  try
    if fDb.Connected then
      fDb.Disconnect;
    if AConnString <> '' then
      fDb.ConnectionString := AConnString;
    fSession.SetStudyName( AContext );
    fDb.Connect;
    Result := true;
  finally
    LeaveMethod( 'Connect' );
  end;
end;

function TCRFSimpleContext.Connected: boolean;
begin
  Result := Assigned( fDb ) and fDb.Connected;
end;

procedure TCRFSimpleContext.Disconnect;
begin
  if Assigned( fSession ) then
    fSession.CloseSession;
  if Assigned( fDb ) then
    fDb.Disconnect;
end;

{$ENDREGION}
{$REGION 'Simple accessors'}

function TCRFSimpleContext.Get_CaseId: Integer;
begin
  Result := fActiveCase.PersonId;
end;

function TCRFSimpleContext.Get_CenterId: Integer;
begin
  Result := fActiveUser.CenterId;
end;

function TCRFSimpleContext.Get_StudyCaseTransfer: ICRFStudyCaseTransfer;
begin
  Result := fAccessControl.StudyCaseTransfer;
end;

function TCRFSimpleContext.Get_SessId: integer;
begin
  Result := fSession.Id;
end;

function TCRFSimpleContext.Get_StudyId: integer;
begin
  Result := fSession.StudyId;
end;

function TCRFSimpleContext.Get_StudyName: string;
begin
  Result := fSession.StudyName
end;

function TCRFSimpleContext.Get_UserId: integer;
begin
  Result := fActiveUser.UserId;
end;

{$ENDREGION}
{$REGION 'Context switching'}

function TCRFSimpleContext.Select( const APersonId: integer ): boolean;
begin
  Result := fActiveCase.Select( APersonId );
end;

procedure TCRFSimpleContext.SetContext( const AContext: string );
begin
  fSession.SetStudyName( AContext )
end;

procedure TCRFSimpleContext.Set_StudyCaseTransfer( AStudyCaseTransfer: ICRFStudyCaseTransfer );
begin
  fAccessControl.StudyCaseTransfer := AStudyCaseTransfer;
end;

procedure TCRFSimpleContext.SelectStudy( Sender: TObject );
var
  savedPerson: integer;
begin
  savedPerson := fActiveCase.PersonId;
  try
    if fActiveCase.Select( 0 ) then
      fSession.SelectStudy( Sender );
  finally
    fActiveCase.Select( savedPerson );
  end;
end;

procedure TCRFSimpleContext.Refresh;
begin
  fSession.Refresh;
end;

{$ENDREGION}

procedure TCRFSimpleContext.GetAllImplementors( AInterface: TGuid; AList: TList );
var
  thisObject: TObject;
begin
  for thisObject in FDictionary.Values do
    if Supports( thisObject, AInterface ) then
      AList.Add( thisObject );
end;

function TCRFSimpleContext.TryGetClass( const AClass: TClass; out AObject: TObject ): boolean;
var
  thisObject: TObject;
begin
  Result := false;
  for thisObject in FDictionary.Values do
  begin
    Result := Assigned( thisObject ) and ( thisObject.ClassType = AClass );
    if Result then
    begin
      AObject := thisObject;
      break;
    end;
  end;
end;

end.
