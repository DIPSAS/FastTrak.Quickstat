unit Emetra.Database.Simple;

{$DEBUGINFO ON}

interface

uses
  Emetra.Database.ConnectionString,
  Emetra.Database.Async,
  {General interfaces}
  Emetra.Logging.Interfaces,
  Emetra.Database.Interfaces,
  Emetra.Database.Dialog.Interfaces,
  Emetra.Database.NativeErrors,
  {Standard}
  Generics.Collections,
  Data.DB, Data.Win.AdoDB, Winapi.ADOInt, System.Classes, System.Contnrs, System.Types, SysUtils, System.UITypes;

type
  TSimpleDatabase = class( TComponent, ISQL, ISQLWriteOnly, ISQLReadOnly, IDatabaseConnectionString, IDatabaseConnection, IDatabaseName, IObservableDatabase, IDatabaseLoginContext, IDatabaseAddUser, IDatabaseChangePassword )
  strict private
    fAllowAsync: boolean;
    fCachedConnString: string;
    fCommand: TADOCommand;
    fConnection: TADOConnection;
    fConnString: TMSSQLConnString;
    fDatabaseName: string;
    fHandleAdoErrorsAsExceptions: boolean;
    fLog: ILog;
    fLoginDialog: IDatabaseLoginDialog;
    fLoginObservers: TObjectList;
    fLogNativeErrorDetails: boolean;
    fLogSql: boolean;
    fMaxRetries: integer;
    fNativeError: integer;
    fPrivilegeErrors: TPrivilegeErrors;
    fQuery: TADOQuery;
    fRetryDelay: integer;
    fServerName: string;
    fWaitCursor: TCursor;
    fUseCursorStack: boolean;
  private
    function ShouldRetryLastOperation: boolean;
    procedure OpenDataset( var ALogText: string );
    procedure CheckConnected;
    procedure PrepareCommandParameters( var ACommand: TADOCommand; const AParams: array of Variant );
    procedure PrepareQueryParameters( const AParams: array of Variant );
    procedure GetDataAccessVersion;
    procedure SetCursorToWaiting;
    procedure SetCursorBack;
  protected
    { Property accessors }
    function Get_CommandTimeout: integer;
    function Get_Connected: boolean;
    function Get_ConnectionString: string;
    function Get_Dataset: TDataset;
    function Get_DbName: string;
    function Get_HostName: string;
    function Get_NativeError: integer;
    function Get_Password: string;
    function Get_UserName: string;
    procedure Set_CommandTimeout( const AValue: integer );
    procedure Set_ConnectionString( AValue: string );
    { Other members }
    procedure AttachObject( AObserver: TObject );
    procedure DetachObject( AObserver: TObject );
    { Properties }
    property HandleAdoErrorsAsExceptions: boolean read fHandleAdoErrorsAsExceptions write fHandleAdoErrorsAsExceptions;
  public
    { Initialization }
    constructor Create( AOwner: TComponent; AQuery, ACommand: TComponent; const ALog: ILog ); reintroduce; overload;
    constructor Create( AOwner: TComponent; const ALog: ILog ); reintroduce; overload;
    constructor Create( ALog: ILog ); reintroduce; overload;
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { Other members }
    function CanChangePassword: boolean;
    function DatabaseObjectExists( const AQualifiedObjectName: string; const ADbObjectType: TDbObjectType ): boolean;
    function ExecuteAsync( const ASQL: string ): integer; overload;
    function ExecuteAsync( const ASQL: string; const AParams: array of Variant ): integer; overload;
    function ExecuteCommand( const ASQL: string ): integer; overload;
    function ExecuteCommand( const ASQL: string; const AParams: array of Variant ): integer; overload;
    function FastQuery( const ASQL: string ): TDataset; overload;
    function FastQuery( const ASQL: string; const AParams: array of Variant ): TDataset; overload;
    function TryChangePassword( const AOldPassword, ANewPassword: string; out AErrorMessage: string ): boolean;
    procedure AddLoginObserver( AObserver: ILoginObserver );
    procedure AddPerson( const ADOB: TDate; const AFirstName, ALastName: string; const AGenderId: integer; const ANationalId: string );
    procedure AddUser( const AUserName, APassword: string );
    procedure Connect;
    procedure Disconnect;
    procedure RemoveAllLoginObservers;
    procedure RemoveLoginObserver( AObserver: ILoginObserver );
  published
    property CommandTimeout: integer read Get_CommandTimeout write Set_CommandTimeout;
    property Connected: boolean read Get_Connected;
    property Connection: TADOConnection read fConnection;
    property ConnectionParameters: TMSSQLConnString read fConnString;
    property ConnectionString: string read Get_ConnectionString write Set_ConnectionString;
    property DatabaseName: string read Get_DbName;
    property Dataset: TDataset read Get_Dataset;
    property LoginDialog: IDatabaseLoginDialog read fLoginDialog write fLoginDialog;
    property LogSql: boolean read fLogSql write fLogSql;
    property MaxRetries: integer read fMaxRetries write fMaxRetries;
    property NativeError: integer read Get_NativeError;
    property RetryDelay: integer read fRetryDelay write fRetryDelay;
    property ServerName: string read Get_HostName;
    property UseCursorStack: boolean read fUseCursorStack write fUseCursorStack;
    property WaitCursor: TCursor read fWaitCursor write fWaitCursor;
  end;

implementation

uses
  {General classes}
  Emetra.Win.User,
  Emetra.Win.CursorStack,
  {Standard}
  Diagnostics, Registry, Variants, Windows, Vcl.Controls;

resourcestring
  SLoginCancelledByUser = 'Brukeren avbrøt innloggingen.';
  SLoginInterfaceMissing = 'Påloggingsinformasjon mangler!';
  SDatabasePrivilegeError =
  { } 'Du mangler rettigheter til å utføre denne operasjonen:\n%s\n' +
  { } 'Kontakt superbruker/brukerstøtte hvis du mener dette er en feil.';
  SGeneralErrorMessage =
  { } 'Operasjonen medførte %d feil:\n%s\n"' +
  { } 'Kontroller loggen hvis det oppstod flere feil.';
  SChangePasswordViaWindows = 'Passordet ditt er knyttet til din Windows-bruker og kan ikke endres her.';

const
  LOG_ERROR_DETAILS          = 'NativeError = %d, Number = %d, SQLState = "%s" Source = "%s", Description = "%s".';
  LOG_DELAY_AND_RETRY        = 'Disconnecting and sleeping for 500ms, will retry operation.';
  EXC_TOO_LATE_FOR_OBSERVERS = 'Login observers should be added before the first login.';
  EXC_IMPLICIT_CONNECT       = 'Implicit connect not allowed, please call the Connect method explicity.';

const
  QRY_SERVER_AND_DATABASE = 'SELECT @@SERVERNAME, DB_NAME()';
  CMD_ADD_USER            = 'EXEC dbo.AddUser :UserName, :Password';
  CMD_ADD_PERSON          = 'EXEC dbo.AddPerson :DOB, :FstName, NULL, :MidName, :GenderId, :NationalId';
  QRY_OBJECT_ID           = 'SELECT OBJECT_ID(%s,%s)';

{$REGION 'Intialization'}

constructor TSimpleDatabase.Create( AOwner: TComponent; AQuery, ACommand: TComponent; const ALog: ILog );
begin
  inherited Create( AOwner );
{$IFDEF Debug}
  fLogSql := true;
{$ENDIF}
  fLog := ALog;
  fConnection := AOwner as TADOConnection;
  fQuery := AQuery as TADOQuery;
  fCommand := ACommand as TADOCommand;
  fQuery.Connection := fConnection;
  fCommand.Connection := fConnection;
  fConnection.CommandTimeout := 30;
end;

constructor TSimpleDatabase.Create( AOwner: TComponent; const ALog: ILog );
begin
  GuardCheckInterfaceAssigned( 'Log', ALog );
  inherited Create( nil );
  fLog := ALog;
  fConnection := TADOConnection.Create( Self );
  fConnection.LoginPrompt := false;
  fQuery := TADOQuery.Create( Self );
  fQuery.CursorType := ctOpenForwardOnly;
  fQuery.CursorLocation := clUseClient;
  fQuery.LockType := ltReadOnly;
  fCommand := TADOCommand.Create( Self );
  fQuery.Connection := fConnection;
  fCommand.Connection := fConnection;
end;

constructor TSimpleDatabase.Create( ALog: ILog );
begin
  GuardCheckInterfaceAssigned( 'Log', ALog );
  Create( nil, ALog );
end;

procedure TSimpleDatabase.AfterConstruction;
begin
  inherited;
  fMaxRetries := 10;
  fRetryDelay := 500;
  fAllowAsync := true;
  fUseCursorStack := true;
  fWaitCursor := crSqlWait;
  fLogNativeErrorDetails := true;
  fConnString := TMSSQLConnString.Create( GetWindowsComputerName, 'master' );
  fHandleAdoErrorsAsExceptions := true;
  fLoginObservers := TObjectList.Create( false );
  fPrivilegeErrors := TPrivilegeErrors.Create;
  name := 'DB';
  GetDataAccessVersion;
end;

procedure TSimpleDatabase.BeforeDestruction;
begin
  if fLoginObservers.Count > 0 then
    RemoveAllLoginObservers;
  fPrivilegeErrors.Free;
  fLoginObservers.Free;
  fConnString.Free;
  inherited;
end;

procedure TSimpleDatabase.GetDataAccessVersion;
const
  PROC_NAME   = 'GetDataAccessVersion';
  LOG_VERSION = '%s.%s: Version="%s", FullInstallVer="%s"';
  LOG_FAILED  = '%s.%s: MDAC version check failed.';
var
  regKey: TRegistry;
begin
  regKey := TRegistry.Create;
  try
    regKey.RootKey := HKEY_LOCAL_MACHINE;
    if regKey.OpenKeyReadOnly( 'Software\Microsoft\DataAccess\' ) then
      fLog.Event( LOG_VERSION, [ClassName, PROC_NAME, regKey.ReadString( 'Version' ), regKey.ReadString( 'FullInstallVer' )] )
    else
      fLog.SilentWarning( LOG_FAILED, [ClassName, PROC_NAME] );
  finally
    regKey.Free;
  end;
end;

{$ENDREGION}
{$REGION 'Simple accessors'}

function TSimpleDatabase.Get_DbName;
begin
  Result := fDatabaseName;
end;

function TSimpleDatabase.Get_Password: string;
begin
  if fConnString.IntegratedSecurity then
    Result := EmptyStr
  else
    Result := fConnString.Password;
end;

function TSimpleDatabase.Get_HostName;
begin
  Result := fServerName;
end;

function TSimpleDatabase.Get_UserName: string;
begin
  if fConnString.IntegratedSecurity then
    Result := GetWindowsUserName
  else
    Result := fConnString.UserName;
end;

function TSimpleDatabase.Get_CommandTimeout;
begin
  Result := fConnection.CommandTimeout;
end;

procedure TSimpleDatabase.Set_CommandTimeout( const AValue: integer );
begin
  fConnection.CommandTimeout := AValue;
  fCommand.CommandTimeout := AValue;
  fQuery.CommandTimeout := AValue;
end;

function TSimpleDatabase.Get_ConnectionString: string;
begin
  Result := fConnString.Value;
end;

function TSimpleDatabase.Get_Dataset: TDataset;
begin
  Result := fQuery;
end;

procedure TSimpleDatabase.Set_ConnectionString( AValue: string );
begin
  fConnString.Value := AValue;
  fConnection.ConnectionString := AValue;
end;

function TSimpleDatabase.Get_Connected: boolean;
begin
  Result := fConnection.Connected;
end;

function TSimpleDatabase.Get_NativeError: integer;
begin
  Result := fNativeError;
end;

{$ENDREGION}
{$REGION 'Login observer interface'}

procedure TSimpleDatabase.AttachObject( AObserver: TObject );
begin
  fLoginObservers.Add( AObserver );
end;

procedure TSimpleDatabase.DetachObject( AObserver: TObject );
begin
  fLoginObservers.Remove( AObserver );
end;

procedure TSimpleDatabase.AddLoginObserver( AObserver: ILoginObserver );
begin
  Assert( Connected = false, EXC_TOO_LATE_FOR_OBSERVERS );
  AttachObject( TObject( AObserver ) );
end;

procedure TSimpleDatabase.RemoveLoginObserver( AObserver: ILoginObserver );
begin
  DetachObject( TObject( AObserver ) );
end;

procedure TSimpleDatabase.RemoveAllLoginObservers;
begin
  fLog.Event( '%s.RemoveAllLoginObservers', [ClassName] );
  fLoginObservers.Clear;
end;

{$ENDREGION}
{$REGION 'Connect and disconnect'}

procedure TSimpleDatabase.Disconnect;
begin
  fLog.Event( '%s.Disconnect', [ClassName] );
  fConnection.Connected := false;
end;

function TSimpleDatabase.CanChangePassword: boolean;
begin
  Result := not fConnString.IntegratedSecurity;
end;

procedure TSimpleDatabase.CheckConnected;
begin
  fNativeError := 0;
  if not fConnection.Connected then
    raise EDatabaseImplicitConnectError.Create( EXC_IMPLICIT_CONNECT );
end;

procedure TSimpleDatabase.Connect;
const
  PROC_NAME         = 'Connect';
  LOG_FULLY_FORMED  = '%s.%s: Username and password present';
  LOG_CACHED_STRING = '%s.%s: Using cached string';
  LOG_LOGIN_NEEDED  = '%s.%s: Login needed (%s)';
  LOG_NOTIFY        = '%s.%s: Notify class %s of successful login.';
  LOG_NAMES         = '%s.%s: SERVER=%s, DATABASE=%s';
var
  loginUser, loginPassword: string;
  n: integer;
  loginObserver: ILoginObserver;
begin
  if Connected then
    exit;
  fLog.EnterMethod( Self, PROC_NAME );
  try
    if fConnString.IntegratedSecurity then
      fLog.Event( '%s.%s: %s', [ClassName, PROC_NAME, fConnString.DelimitedText] )
    else if fConnString.HasUsernameAndPassword then
      fLog.Event( LOG_FULLY_FORMED, [ClassName, PROC_NAME] )
    else if fConnString.DelimitedText = fCachedConnString then
      fLog.Event( LOG_CACHED_STRING, [ClassName, PROC_NAME] )
    else if Assigned( fLoginDialog ) then
    begin
      fLog.SilentWarning( LOG_LOGIN_NEEDED, [ClassName, PROC_NAME, fConnString.DelimitedText] );
      if fLoginDialog.Login( loginUser, loginPassword ) then
        fConnString.SetLogin( loginUser, loginPassword )
      else
        raise EDatabaseLoginCancelled.Create( SLoginCancelledByUser );
    end
    else
      raise EDatabaseCredentialsMissing.Create( SLoginInterfaceMissing );
    fConnection.ConnectionString := fConnString.DelimitedText;
    fConnection.Connected := true;
    fCachedConnString := fConnString.DelimitedText;
    with FastQuery( QRY_SERVER_AND_DATABASE ) do
      try
        fServerName := Fields[0].AsString;
        fDatabaseName := Fields[1].AsString;
      finally
        { Do not close, overridden methods may need to read info }
      end;
    fLog.SilentSuccess( LOG_NAMES, [ClassName, PROC_NAME, fServerName, fDatabaseName] );
    n := 0;
    while n < fLoginObservers.Count do
    begin
      if Supports( fLoginObservers[n], ILoginObserver, loginObserver ) then
        try
          fLog.Event( LOG_NOTIFY, [ClassName, PROC_NAME, loginObserver.GetNamePath] );
          loginObserver.AfterLogin( Self );
        except
          on E: Exception do
          begin
            fLog.SilentError( '%s.%s: %s', [ClassName, PROC_NAME, E.Message] );
            raise EDatabaseLoginObserverError.Create( E.Message );
          end;
        end;
      inc( n );
    end;
  finally
    fLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

{$ENDREGION}
{$REGION 'Prepare parameters'}

procedure TSimpleDatabase.PrepareQueryParameters( const AParams: array of Variant );
var
  n: integer;
begin
  n := 0;
  while n < fQuery.Parameters.Count do
  begin
    try
      fQuery.Parameters[n].Value := AParams[n];
    except
      on E: Exception do
      begin
        fLog.SilentError( Format( '%s.PrepareQueryParameters[%d]: %s %s', [ClassName, n, VarToStr( AParams[n] ), E.Message] ) );
        raise EDatabaseParameterError( E.Message );
      end;
    end;
    inc( n );
  end;
end;

procedure TSimpleDatabase.PrepareCommandParameters( var ACommand: TADOCommand; const AParams: array of Variant );
var
  n: integer;
begin
  n := 0;
  while n < ACommand.Parameters.Count do
  begin
    try
      ACommand.Parameters[n].Value := AParams[n];
    except
      on E: Exception do
      begin
        fLog.SilentError( Format( '%s.PrepareCommandParameters[%d](%s=%s): %s', [ClassName, n, ACommand.Parameters[n].Name, VarToStr( AParams[n] ), E.Message] ) );
        raise EDatabaseParameterError( E.Message );
      end;
    end;
    inc( n );
  end;
end;

{$ENDREGION}

procedure TSimpleDatabase.AddPerson( const ADOB: TDate; const AFirstName, ALastName: string; const AGenderId: integer; const ANationalId: string );
begin
  ExecuteCommand( CMD_ADD_PERSON, [ADOB, AFirstName, ALastName, AGenderId, ANationalId] );
end;

procedure TSimpleDatabase.AddUser( const AUserName, APassword: string );
begin
  ExecuteCommand( CMD_ADD_USER, [AUserName, APassword] );
end;

function TSimpleDatabase.ExecuteCommand( const ASQL: string ): integer;
begin
  Result := ExecuteCommand( ASQL, [] );
end;

function TSimpleDatabase.ExecuteAsync( const ASQL: string; const AParams: array of Variant ): integer;
begin
  Result := 0;
  if fAllowAsync then
    TSqlCommandThread.Create( fConnString.Value, ASQL, AParams, fLog )
  else
    Result := ExecuteCommand( ASQL, AParams );
end;

function TSimpleDatabase.ExecuteAsync( const ASQL: string ): integer;
begin
  Result := ExecuteAsync( ASQL, [] );
end;

function TSimpleDatabase.ExecuteCommand( const ASQL: string; const AParams: array of Variant ): integer;
var
  stopRetrying: boolean;
  retryCount: integer;
begin
  SetCursorToWaiting;
  try
    Result := 1;
    CheckConnected;
    if fLogSql then
      fLog.LogSqlCommand( ASQL );
    fCommand.Parameters.Clear;
    { Param check only if parameters are given }
    fCommand.ParamCheck := ( high( AParams ) <> -1 );
    fCommand.CommandText := ASQL;
    if fCommand.ParamCheck then
      PrepareCommandParameters( fCommand, AParams );
    stopRetrying := false;
    retryCount := 0;
    repeat
      try
        fCommand.Execute;
        stopRetrying := not ShouldRetryLastOperation;
      except
        on E: EDatabaseUserDefinedError do
          raise;

        on E: EDatabaseCommandFailed do
          raise;

        on E: Exception do
        begin
          if not ShouldRetryLastOperation then
            stopRetrying := true
          else
          begin
            inc( retryCount );
            stopRetrying := retryCount >= fMaxRetries;
          end;
          if stopRetrying then
            raise;
        end;
      end;
    until stopRetrying;
  finally
    SetCursorBack;
  end;
end;

procedure TSimpleDatabase.OpenDataset( var ALogText: string );
var
  retryCount: integer;
  stopRetrying: boolean;
  stopWatch: TStopWatch;
begin
  stopWatch := TStopWatch.StartNew;
  stopRetrying := false;
  retryCount := 0;
  repeat
    try
      fQuery.Active := true;
      stopRetrying := true;
    except
      on E: Exception do
      begin
        if not ShouldRetryLastOperation then
          stopRetrying := true
        else
        begin
          inc( retryCount );
          stopRetrying := retryCount >= fMaxRetries;
        end;
        if stopRetrying then
          raise;
      end;
    end;
  until stopRetrying;
  { Add timing info to the log text }
  ALogText := ALogText + Format( ' ... ( %.1f ms )', [1000 * stopWatch.ElapsedTicks / stopWatch.Frequency] );
end;

function TSimpleDatabase.FastQuery( const ASQL: string ): TDataset;
var
  logText: string;
begin
  logText := ASQL;
  SetCursorToWaiting;
  try
    CheckConnected;
    fQuery.Active := false;
    fQuery.SQL.Text := ASQL;
    OpenDataset( logText );
    Result := fQuery;
  finally
    if fLogSql then
      fLog.LogSqlQuery( logText );
    SetCursorBack;
  end;
end;

function TSimpleDatabase.FastQuery( const ASQL: string; const AParams: array of Variant ): TDataset;
var
  logText: string;
begin
  logText := ASQL;
  SetCursorToWaiting;
  try
    CheckConnected;
    fQuery.Active := false;
    fQuery.SQL.Text := ASQL;
    PrepareQueryParameters( AParams );
    OpenDataset( logText );
    Result := fQuery;
  finally
    if fLogSql then
      fLog.LogSqlQuery( logText );
    SetCursorBack;
  end;
end;

function TSimpleDatabase.ShouldRetryLastOperation: boolean;
const
  // ODBC SQL State code, See https://docs.microsoft.com/en-us/sql/odbc/reference/appendixes/appendix-a-odbc-error-codes
  COMM_LINK_FAILURE = '08S01';
var
  errNo: integer;
  adoError: Winapi.ADOInt.Error;
  errClass: string;
begin
  { Default is to not try operation again }
  Result := false;
  try
    errNo := 0;
    while errNo < fConnection.Errors.Count do
    begin
      adoError := fConnection.Errors[errNo];
      fNativeError := adoError.NativeError;
      if fLogNativeErrorDetails then
      begin
        { The error class is the first two characters of SQL State, and is just a warning message if it is 01 }
        errClass := Copy( adoError.SQLState, 1, 2 );
        if ( errClass = '01' ) and ( adoError.NativeError = 0 ) then
          { These are typically PRINT statements in stored procedures }
          fLog.Event( adoError.Description )
        else if errClass = '01' then
          { Errors that are not considered a failure of the statement }
          fLog.SilentWarning( LOG_ERROR_DETAILS, [adoError.NativeError, adoError.Number, adoError.SQLState, adoError.Source, adoError.Description] )
        else
          fLog.SilentError( LOG_ERROR_DETAILS, [adoError.NativeError, adoError.Number, adoError.SQLState, adoError.Source, adoError.Description] );
      end;

      { Check for permission problem, and raise custom exception }
      if fPrivilegeErrors.Includes( adoError.NativeError ) then
        raise EDatabasePrivilegeError.CreateFmt( SDatabasePrivilegeError, [adoError.Description] );

      { Check for user-defined errors from stored procedures, and raise exception if one is found }
      if adoError.NativeError >= ERR_USER_DEFINED_START then
        raise EDatabaseUserDefinedError.Create( adoError.Description );

      { Check for communication link failure, which means the operation can be retried. }
      if adoError.SQLState = COMM_LINK_FAILURE then
        Result := true;

      inc( errNo );
    end;
    { If we have unhandled errors, then raise exception }
    if ( ( not Result ) and ( errNo > 0 ) ) then
      if errNo = 1 then
        raise EDatabaseCommandFailed.Create( adoError.Description )
      else
        raise EDatabaseCommandFailed.CreateFmt( SGeneralErrorMessage, [errNo, adoError.Description] );

    { Disconnect, clear errors and sleep for 500 ms if operation can be retried. }
    if Result then
    begin
      fLog.SilentWarning( LOG_DELAY_AND_RETRY );
      fConnection.Connected := false;
      Sleep( fRetryDelay );
    end
  finally
    fConnection.Errors.Clear;
  end;
end;

function TSimpleDatabase.TryChangePassword( const AOldPassword, ANewPassword: string; out AErrorMessage: string ): boolean;
begin
  AErrorMessage := EmptyStr;
  Result := false;
  try
    if fConnString.IntegratedSecurity then
      raise EAbort.Create( SChangePasswordViaWindows );
    ExecuteCommand( Format( 'ALTER LOGIN [%s] WITH PASSWORD = %s OLD_PASSWORD = %s', [Get_UserName, QuotedStr( ANewPassword ), QuotedStr( AOldPassword )] ) );
    Result := true;
    AErrorMessage := 'OK';
  except
    on E: Exception do
    begin
      AErrorMessage := E.Message;
      fLog.SilentError( E.Message );
    end;
  end;
end;

function TSimpleDatabase.DatabaseObjectExists( const AQualifiedObjectName: string; const ADbObjectType: TDbObjectType ): boolean;
var
  typeStr: string;
begin
  case ADbObjectType of
    otUserTable: typeStr := 'U';
    otView: typeStr := 'V';
    otScalarFunction: typeStr := 'FN';
    otStoredProcedure: typeStr := 'P';
    otForeignKeyConstraint: typeStr := 'F';
    otTableValuedFunction: typeStr := 'TF';
    otSynonym: typeStr := 'SN';
  else Result := false; exit;
  end;
  with FastQuery( Format( QRY_OBJECT_ID, [QuotedStr( AQualifiedObjectName ), QuotedStr( typeStr )] ) ) do
    try
      Result := not Fields[0].IsNull;
    finally
      Close;
    end;
end;

procedure TSimpleDatabase.SetCursorBack;
begin
  if fUseCursorStack then
    CursorStack.Pop;
end;

procedure TSimpleDatabase.SetCursorToWaiting;
begin
  if fUseCursorStack then
    CursorStack.Push( fWaitCursor );
end;

end.
