unit Emetra.Database.ConnectionString;

interface

uses
  {General}
  Emetra.Utils.Params,
  {Standard}
  System.Classes,
  Generics.Collections;

type
  TSqlProvider = ( sqlOleDb, sqlNativeClient10, sqlNativeClient11 );

  TMSSQLConnString = class( TStringList )
  private
    fSqlProvider: TSqlProvider;
  protected
    { Property accessors }
    function Get_Database: string;
    function Get_IntegratedSecurity: boolean;
    function Get_Password: string;
    function Get_Server: string;
    function Get_Username: string;
    function Get_Value: string;
    procedure Set_Database( const AValue: string );
    procedure Set_IntegratedSecurity( const AValue: boolean );
    procedure Set_Provider( const AValue: TSqlProvider );
    procedure Set_Server( const AValue: string );
    procedure Set_Value( const AValue: string );
  public
    { Initialization }
    constructor Create; overload;
    constructor Create( const AServer, ADatabase: string ); reintroduce; overload;
    { Other methods }
    function HasUsernameAndPassword: boolean;
    procedure LoadFromUdl( const AFileName: string );
    procedure Parse( const AParams: TParamList );
    procedure SetLogin( const AUserName, APassword: string );
    { Properties }
    property Database: string read Get_Database write Set_Database;
    property IntegratedSecurity: boolean read Get_IntegratedSecurity write Set_IntegratedSecurity;
    property Password: string read Get_Password;
    property Provider: TSqlProvider read fSqlProvider write Set_Provider;
    property Server: string read Get_Server write Set_Server;
    property Username: string read Get_Username;
    property Value: string read Get_Value write Set_Value;
  end;

function GetConnectionString( AParams: TParamList ): string;
function GetTrustedConnectionString( const AServer, ADatabase: string; const AUseOleDbProvider: boolean = false ): string;
function GetUntrustedConnectionString( const AServer, ADatabase, AUserName, APassword: string ): string;
function GetFastTrakParentConnection: string;
function GetAzureConnectionString( const APath: string ): string;
function GetDogfoodConnectionString: string;

function TryGetStoredCredentials( const AProjectName: string; out AServerName, ADatabaseName, AUserName, APassword: string ): boolean;

implementation

uses
  Emetra.Win.User,
  System.Win.Registry,
  System.SysUtils;

const
  { Command line parameters }
  PRM_SERVER   = 'Server';
  PRM_DATABASE = 'Database';
  PRM_USERNAME = 'Username';
  PRM_PASSWORD = 'Password';

const
  { Connection string keys }
  KEY_FILE_NAME = 'FILE NAME';
  KEY_PROVIDER  = 'Provider';
  KEY_DATABASE  = 'Initial Catalog';
  KEY_SERVER    = 'Data Source';
  KEY_USER_ID   = 'User ID';
  KEY_PASSWORD  = 'Password';
  KEY_SECURITY  = 'Integrated Security';

const
  { Connection string values }
  VALUE_NATIVE10 = 'SQLNCLI10.1';
  VALUE_NATIVE11 = 'SQLNCLI11.1';
  VALUE_OLEDBX   = 'SQLOLEDB.1';
  VALUE_SSPI     = 'SSPI';

const
  { Values }
  KEYS_SERVER_DB               = KEY_SERVER + '=%s;' + KEY_DATABASE + '=%s;';
  KEYS_LOGIN                   = KEY_USER_ID + '=%s;' + KEY_PASSWORD + '=%s;';
  SQL_CONNECTION_COMMON        = KEYS_SERVER_DB + KEY_SECURITY + '=' + VALUE_SSPI + ';';
  SQL_CONNECTION_SQLOLEDB      = KEY_PROVIDER + '=' + VALUE_OLEDBX + ';' + SQL_CONNECTION_COMMON;
  SQL_CONNECTION_NATIVE_CLIENT = KEY_PROVIDER + '=' + VALUE_NATIVE11 + ';' + SQL_CONNECTION_COMMON;
  { Standard connections }
  SQL_CONNECTION           = SQL_CONNECTION_NATIVE_CLIENT;
  SQL_CONNECTION_UNTRUSTED = KEY_PROVIDER + '=' + VALUE_OLEDBX + ';' + KEYS_SERVER_DB + KEYS_LOGIN;

function FastTrakUdlInParent: string;
begin
  Result := ExtractFilePath( ParamStr( 0 ) ) + '..\FastTrak.UDL';
end;

function GetFastTrakParentConnection: string;
begin
  Result := KEY_FILE_NAME + '=' + FastTrakUdlInParent;
end;

function GetTrustedConnectionString( const AServer, ADatabase: string; const AUseOleDbProvider: boolean = false ): string;
begin
  if AUseOleDbProvider then
    Result := Format( SQL_CONNECTION_SQLOLEDB, [AServer, ADatabase] )
  else
    Result := Format( SQL_CONNECTION, [AServer, ADatabase] );
end;

function GetUntrustedConnectionString( const AServer, ADatabase, AUserName, APassword: string ): string;
begin
  Result := Format( SQL_CONNECTION_UNTRUSTED, [AServer, ADatabase, AUserName, APassword] );
end;

function GetConnectionString( AParams: TParamList ): string;
var
  connStr: TMSSQLConnString;
begin
  if AParams.Exists( PRM_DATABASE ) then
  begin
    connStr := TMSSQLConnString.Create;
    try
      connStr.Parse( AParams );
      Result := connStr.Value;
    finally
      connStr.Free;
    end;
  end
  else
    Result := GetFastTrakParentConnection;
end;

{ TMSSQLConnString }

constructor TMSSQLConnString.Create( const AServer, ADatabase: string );
begin
  inherited Create;
  Delimiter := ';';
  StrictDelimiter := true;
  DelimitedText := SQL_CONNECTION_SQLOLEDB;
  Values[KEY_SERVER] := AServer;
  Values[KEY_DATABASE] := ADatabase;
end;

constructor TMSSQLConnString.Create;
begin
  Create( EmptyStr, EmptyStr );
end;

function TMSSQLConnString.Get_Value: string;
begin
  Result := DelimitedText;
end;

function TMSSQLConnString.HasUsernameAndPassword: boolean;
begin
  Result := ( Username <> EmptyStr ) and ( Password <> EmptyStr );
end;

function TMSSQLConnString.Get_Database: string;
begin
  Result := Values[KEY_DATABASE];
end;

function TMSSQLConnString.Get_Server: string;
begin
  Result := Values[KEY_SERVER];
end;

function TMSSQLConnString.Get_Username: string;
begin
  Result := Values[KEY_USER_ID];
end;

procedure TMSSQLConnString.LoadFromUdl( const AFileName: string );
var
  udlFile: TStringList;
begin
  udlFile := TStringList.Create;
  try
    udlFile.StrictDelimiter := true;
    QuoteChar := CHR( 39 );
    udlFile.LoadFromFile( AFileName );
    if udlFile.Count > 2 then
      DelimitedText := udlFile[2];
  finally
    udlFile.Free;
  end;
end;

procedure TMSSQLConnString.Parse( const AParams: TParamList );
begin
  if AParams.Exists( PRM_SERVER ) then
    Values[KEY_SERVER] := AParams.ReadString( PRM_SERVER )
  else
    Values[KEY_SERVER] := GetWindowsComputerName;
  if AParams.Exists( PRM_DATABASE ) then
    Values[KEY_DATABASE] := AParams.ReadString( PRM_DATABASE );
  if AParams.Exists( PRM_USERNAME ) and AParams.Exists( PRM_PASSWORD ) then
    SetLogin( AParams.ReadString( PRM_USERNAME ), AParams.ReadString( PRM_PASSWORD ) );
end;

procedure TMSSQLConnString.SetLogin( const AUserName, APassword: string );
begin
  Set_IntegratedSecurity( false );
  Values[KEY_USER_ID] := AUserName;
  Values[KEY_PASSWORD] := APassword;
end;

procedure TMSSQLConnString.Set_Database( const AValue: string );
begin
  Values[KEY_DATABASE] := AValue;
end;

procedure TMSSQLConnString.Set_Server( const AValue: string );
begin
  Values[KEY_SERVER] := AValue;
end;

function TMSSQLConnString.Get_IntegratedSecurity: boolean;
begin
  Result := SameText( Values[KEY_SECURITY], VALUE_SSPI );
end;

function TMSSQLConnString.Get_Password: string;
begin
  Result := Values[KEY_PASSWORD];
end;

procedure TMSSQLConnString.Set_IntegratedSecurity( const AValue: boolean );
var keyIndex: integer;
begin
  if AValue then
    Values[KEY_SECURITY] := VALUE_SSPI
  else
  begin
    keyIndex := IndexOfName( KEY_SECURITY );
    if keyIndex <> -1 then
      Delete( keyIndex );
  end;
end;

procedure TMSSQLConnString.Set_Provider( const AValue: TSqlProvider );
begin
  case AValue of
    sqlNativeClient10: Values[KEY_PROVIDER] := VALUE_NATIVE10;
    sqlNativeClient11: Values[KEY_PROVIDER] := VALUE_NATIVE11;
    sqlOleDb: Values[KEY_PROVIDER] := VALUE_OLEDBX;
  end;
end;

procedure TMSSQLConnString.Set_Value( const AValue: string );
var fileName: string;
begin
  DelimitedText := AValue;
  fileName := Values[KEY_FILE_NAME];
  if fileName <> EmptyStr then
    LoadFromUdl( fileName );
end;

function GetAzureConnectionString( const APath: string ): string;
var
  reg: TRegistry;
begin
  reg := TRegistry.Create;
  try
    reg.OpenKeyReadOnly( APath );
    Result := GetUntrustedConnectionString( reg.ReadString( 'Server' ), reg.ReadString( 'Database' ), reg.ReadString( 'UserName' ), reg.ReadString( 'Password' ) );
  finally
    reg.Free;
  end;
end;

function GetDogfoodConnectionString: string;
begin
  Result := GetAzureConnectionString( 'SOFTWARE\Emetra\Dogfood' );
end;

function TryGetStoredCredentials( const AProjectName: string; out AServerName, ADatabaseName, AUserName, APassword: string ): boolean;
var
  reg: TRegistry;
begin
  reg := TRegistry.Create;
  try
    reg.OpenKeyReadOnly( 'Software\Emetra\' + AProjectName );
    AServerName := reg.ReadString( 'Server' );
    ADatabaseName := reg.ReadString( 'Database' );
    AUserName := reg.ReadString( 'UserName' );
    APassword := reg.ReadString( 'Password' );
  finally
    reg.Free;
  end;
  Result := ( AUserName <> EmptyStr ) and ( APassword <> EmptyStr );
end;

end.
