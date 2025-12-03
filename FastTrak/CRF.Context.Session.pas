unit CRF.Context.Session;
{$M+}
{
  Summary:
  Keeps track of file locations and URLs for forms, visits and logos.
  There is usually only one global CRFSession in an application, so
  there is a global variable here that can be accessed and shared from
  any CRF unit.
}

interface

uses
  {Project}
  CRF.Study.Interfaces,
  CRF.Context.Session.Interfaces,
  {General}
  Emetra.Logging.Interfaces,
  Emetra.Database.Interfaces,
  Emetra.Classes.Business,
  {Standard}
  WinApi.Windows, Data.Db,
  System.SysUtils, System.Win.Registry, System.Classes, System.Inifiles, System.Types, System.Contnrs;

type
{$REGION 'Interfaces'}
  TCRFStudyContext = class( TBusiness, IStudyId, IStudyContext, IStudyLoginContext, IStudySession, ILoginObserver )
  strict private
    fStudyName: string;
    fStudyObservers: TObjectList;
    fSessId: integer;
    fStudyId: integer;
    fAppVersion: string;
    fUpdates: integer;
    fInserts: integer;
  private
    fSQL: ISQL;
    procedure NotifyStudyObservers;
    procedure LoadStudyProperties;
    { Property accessors }
    function Get_SessId: integer;
    function Get_StudyId: integer;
    function Get_StudyName: string;
  protected
    { Other members }
    function Connected: boolean;
    function FriendlyName: string;
    procedure SetContext( const AUser, APassword, AContext: string ); overload;
    procedure SetStudyNameInDatabase( const AStudyName: string );
    procedure Set_StudyId( const AValue: integer ); // Must be visible to TCRFStudyContextFileSystem for interface reasons
  public
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    procedure AfterLogin( Sender: IDatabaseConnection );
    procedure AddStudyObserver( AStudyObserver: IStudyObserver );
    procedure CloseSession;
    procedure IncrementUpdates;
    procedure IncrementInserts;
    procedure RemoveAllStudyObservers;
    procedure SetStudyName( const AStudyName: string ); overload;
  published
    property AppVersion: string read fAppVersion write fAppVersion;
    property Id: integer read Get_SessId;
    property SessId: integer read Get_SessId;
    property StudyId: integer read Get_StudyId write Set_StudyId;
    property StudyName: string read Get_StudyName;
  end;

  TCRFStudyContextFileSystem = class( TCRFStudyContext, IStudyContext, IStudyFileSystemContext, IStudySelector, IStudyFolder )
  private
    fWorkDir: string;
    function FileRoot: string;
    function WebRoot: string;
  protected
    { Property accessors }
    function Get_Path: string;
    function Get_Root: string;
    function Get_UDL: string;
    procedure Set_Root( const ARootDir: string );
    { Other members }
    procedure SetStudyNameOnDisk( const s: string );
  public
    { Initialization }
    constructor Create( const ARoot: string; const ALog: ILog ); reintroduce; overload;
    constructor Create( const ALog: ILog ); overload;
    { Other members }
    function PatientDir( const AShared: boolean = false; const AOnWeb: boolean = false ): string;
    function PopulationDir( const AShared: boolean = false; const AOnWeb: boolean = false ): string;
    function ProtocolDir( const AOnWeb: boolean = false ): string;
    procedure SelectStudy( Sender: TObject );
    procedure Refresh;
  published
    property Path: string read Get_Path;
    property Root: string read Get_Root write Set_Root;
    property UDL: string read Get_UDL;
  end;

const
  { Useful constants }
  XML_TAG_CLOSE = '/>';
  XML_TAG_START = '<%s ';
  ATTR_STR      = '%s="%s" ';
  ATTR_INT      = '%s="%d" ';
  ATTR_GEN      = '%s="%g" ';

implementation

uses

  CRF.SQL,
  {General}
  Emetra.Database.Dialog.Interfaces,
  Emetra.Win.User,
  Emetra.Utils.ExeParams;

const
  EXT_UDL        = '.UDL';
  DIR_POPULATION = 'Population\';
  DIR_PATIENT    = 'Patient\';

resourcestring
  URL_ROOT = 'https://fasttrak.dips.no/protocols/';
  EXC_FAILED_TO_RETRIEVE_FIELD = 'Feltet "%s" ble ikke funnet i datasettet fra plukklisten..';
  EXC_MISSING_PICKLIST = 'Plukklisten er udefinert, så denne funksjonen er utilgjengelig.';
  SUBTITLE_SHOWS_ALL_PROTOCOLS = 'Listen viser alle fagområder i systemet.';
  TITLE_SELECT_PROTOCOL = 'Velg fagjournal';
  WARN_FAILED_TO_SELECT_PROTOCOL = 'Kunne ikke velge ny protokoll: %s';

{$REGION 'TCRFDataSession'}

procedure TCRFStudyContext.AddStudyObserver( AStudyObserver: IStudyObserver );
const
  PROC_NAME = 'AddObserver';
var
  objObserver: TObject;
begin
  Log.EnterMethod( Self, Format( '%s( %d ): %s', [PROC_NAME, fStudyObservers.Count, AStudyObserver.GetNamePath] ) );
  try
    objObserver := TObject( AStudyObserver );
    if fStudyObservers.IndexOf( objObserver ) = -1 then
      fStudyObservers.Add( objObserver )
    else
      Log.Event( '%s.%s: Duplicate registration attempt for %s.', [ClassName, PROC_NAME, AStudyObserver.GetNamePath], ltWarning );
  finally
    Log.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TCRFStudyContext.RemoveAllStudyObservers;
begin
  fStudyObservers.Clear;
end;

procedure TCRFStudyContext.AfterLogin( Sender: IDatabaseConnection );
const
  PROC_NAME = 'AfterLogin';
begin
  EnterMethod( PROC_NAME );
  try
    if not Supports( Sender, ISQL, fSQL ) then
      raise Exception.Create( 'Sender must support ISQL ' );
    if fStudyName <> '' then
      LoadStudyProperties;
  finally
    LeaveMethod( PROC_NAME );
  end;
end;

procedure TCRFStudyContext.BeforeDestruction;
begin
  fStudyObservers.Free;
  inherited;
end;

procedure TCRFStudyContext.LoadStudyProperties;
const
  PROC_NAME = 'LoadStudyProperties';
begin
  EnterMethod( Format( '%s: StudyName="%s"', [PROC_NAME, fStudyName] ) );
  try
    fSessId := 0;
    fStudyId := 0;
    fUpdates := 0;
    fInserts := 0;
    if ( fStudyName <> EmptyStr ) and Connected then
    begin
      if not Assigned( fSQL ) then
        Log.Event( 'SQL is unassigned, no settings loaded.' )
      else
      begin
        with fSQL.FastQuery( QRY_STUDY_ID, [fStudyName] ) do
          try
            if not EOF then
              fStudyId := Fields[0].AsInteger;
          finally
            Close;
          end;
        if fStudyId > 0 then
          with fSQL.FastQuery( QRY_ADD_SESSION, [StudyId, GetWindowsComputerName, GetWindowsUserName, Now, fAppVersion] ) do
            try
              if not EOF then
                fSessId := Fields[0].AsInteger;
            finally
              Close;
            end;
      end;
      NotifyStudyObservers;
    end;
  finally
    LeaveMethod( PROC_NAME );
  end;
end;

procedure TCRFStudyContext.AfterConstruction;
begin
  inherited;
  fStudyObservers := TObjectList.Create( false );
  fAppVersion := EmptyStr;
end;

function TCRFStudyContext.FriendlyName: string;
begin
  Result := 'Brukerøkt';
end;

function TCRFStudyContext.Connected: boolean;
begin

  Result := Assigned( fSQL ) and ( fSQL.Connected );
end;

function TCRFStudyContext.Get_SessId: integer;
begin
  Result := fSessId;
end;

procedure TCRFStudyContext.CloseSession;
begin
  if fSessId > 0 then
  begin
    if Assigned( fSQL ) and ( fSQL.Connected ) then
      fSQL.ExecuteCommand( CMD_CLOSE_SESSION, [fSessId, fUpdates, fInserts] );
    fSessId := 0;
    fUpdates := 0;
    fInserts := 0;
  end;
end;

procedure TCRFStudyContext.IncrementUpdates;
begin
  inc( fUpdates );
end;

procedure TCRFStudyContext.IncrementInserts;
begin
  inc( fInserts );
end;

procedure TCRFStudyContext.SetStudyNameInDatabase( const AStudyName: string );
const
  PROC_NAME = 'SetStudyNameInDatabase';
begin
  EnterMethod( Format( '%s("%s")', [PROC_NAME, AStudyName] ) );
  try
    if fStudyName <> AStudyName then
      CloseSession;
    fStudyName := AStudyName;
    LoadStudyProperties;
  finally
    LeaveMethod( PROC_NAME );
  end;
end;

function TCRFStudyContext.Get_StudyName: string;
begin
  Result := fStudyName;
end;

procedure TCRFStudyContext.NotifyStudyObservers;
const
  PROC_NAME = 'NotifyStudyObservers';
var
  n: integer;
  thisObserver: IStudyObserver;
begin
  EnterMethod( PROC_NAME );
  try
    n := 0;
    while n < fStudyObservers.Count do
    begin
      if Supports( fStudyObservers[n], IStudyObserver, thisObserver ) then
        thisObserver.AfterStudyChange( Self );
      inc( n );
    end;
  finally
    LeaveMethod( PROC_NAME );
  end;
end;

procedure TCRFStudyContext.Set_StudyId( const AValue: integer );
begin
  if AValue = fStudyId then
    exit;
  fStudyId := AValue;
end;

function TCRFStudyContext.Get_StudyId: integer;
begin
  Result := fStudyId;
end;

procedure TCRFStudyContext.SetStudyName( const AStudyName: string );
const
  PROC_NAME = 'SetStudyName';
begin
  Log.EnterMethod( Self, Format( '%s(%s)', [PROC_NAME, AStudyName] ) );
  try
    SetContext( EmptyStr, EmptyStr, AStudyName );
  finally
    Log.LeaveMethod( Self, Format( '%s(%s)', [PROC_NAME, AStudyName] ) );
  end;
end;

procedure TCRFStudyContext.SetContext( const AUser, APassword, AContext: string );
begin
  if ( AContext <> '' ) and ( AContext <> fStudyName ) then
  begin
    { Clear first }
    if ( fStudyName <> EmptyStr ) then
    begin
      fStudyName := EmptyStr;
      fStudyId := 0;
      NotifyStudyObservers;
    end;
    { Now change }
    SetStudyNameInDatabase( AContext );
  end;
end;

{$ENDREGION}

constructor TCRFStudyContextFileSystem.Create( const ALog: ILog );
begin
  Create( EmptyStr, ALog );
end;

constructor TCRFStudyContextFileSystem.Create( const ARoot: string; const ALog: ILog );
begin
  inherited Create( ALog );
  Set_Root( ARoot );
end;

procedure TCRFStudyContextFileSystem.SelectStudy( Sender: TObject );
var
  newStudyId: integer;
  newStudyName: string;
begin
  Assert( Assigned( GlobalPickList ), EXC_MISSING_PICKLIST );
  try
    newStudyId := GlobalPickList.SelectInteger( QRY_MY_STUDIES, [StudyId], TITLE_SELECT_PROTOCOL, SUBTITLE_SHOWS_ALL_PROTOCOLS );
    if ( newStudyId > 0 ) then
      if GlobalPickList.TryGetFieldValue( FLD_STUDY_NAME, newStudyName ) then
        SetStudyNameOnDisk( newStudyName )
      else
        raise EDatabaseError.CreateFmt( EXC_FAILED_TO_RETRIEVE_FIELD, [FLD_STUDY_NAME] );
  except
    on E: Exception do
      GlobalLog.Event( WARN_FAILED_TO_SELECT_PROTOCOL, [E.Message], ltWarning );
  end;
end;

procedure TCRFStudyContextFileSystem.Refresh;
begin
  SetStudyNameOnDisk( Self.StudyName );
end;

function TCRFStudyContextFileSystem.ProtocolDir( const AOnWeb: boolean = false ): string;
begin
  if AOnWeb then
    Result := WebRoot + StudyName + '/'
  else
    Result := FileRoot + StudyName + '\';
end;

function TCRFStudyContextFileSystem.Get_UDL: string;
begin
  Result := IncludeTrailingPathDelimiter( Root + StudyName ) + StudyName + EXT_UDL;
end;

function TCRFStudyContextFileSystem.FileRoot: string;
begin
  Result := fWorkDir;
end;

procedure TCRFStudyContextFileSystem.SetStudyNameOnDisk( const s: string );
const
  PROC_NAME = 'SetStudyNameOnDisk';
begin
  EnterMethod( PROC_NAME );
  try
    SetStudyNameInDatabase( s );
    try
      ForceDirectories( Self.Path );
    except
      on E: Exception do
        Log.SilentError( '%s.%s: %s', [ClassName, PROC_NAME, E.Message] );
    end;
  finally
    LeaveMethod( PROC_NAME );
  end;
end;

procedure TCRFStudyContextFileSystem.Set_Root( const ARootDir: string );
const
  LOG_ERROR = '%s.Set_Root: %s';
begin
  fWorkDir := ARootDir;
  if fWorkDir = EmptyStr then
    fWorkDir := ExeParams.Values['DIR'];
  if fWorkDir = EmptyStr then
    fWorkDir := ExtractFilePath( ParamStr( 0 ) );
  fWorkDir := IncludeTrailingPathDelimiter( fWorkDir );
  try
    ForceDirectories( fWorkDir );
  except
    on E: Exception do
      Log.SilentError( LOG_ERROR, [ClassName, E.Message] )
  end;
end;

function TCRFStudyContextFileSystem.PatientDir( const AShared: boolean = false; const AOnWeb: boolean = false ): string;
begin
  if AOnWeb then
    Result := WebRoot
  else
    Result := FileRoot;
  if not AShared then
    Result := Result + StudyName + '\';
  Result := Result + DIR_PATIENT;
  if AOnWeb then
    Result := StringReplace( Result, '\', '/', [rfReplaceAll] );
end;

function TCRFStudyContextFileSystem.PopulationDir( const AShared: boolean = false; const AOnWeb: boolean = false ): string;
begin
  if AOnWeb then
    Result := WebRoot
  else
    Result := FileRoot;
  if not AShared then
    Result := Result + StudyName + '\';
  Result := Result + DIR_POPULATION;
  if AOnWeb then
    Result := StringReplace( Result, '\', '/', [rfReplaceAll] );
end;

function TCRFStudyContextFileSystem.Get_Path: string;
begin
  Result := ProtocolDir( false );
end;

function TCRFStudyContextFileSystem.Get_Root: string;
begin
  Result := fWorkDir;
end;

function TCRFStudyContextFileSystem.WebRoot: string;
begin
  Result := URL_ROOT;
end;

end.
