unit Emetra.Database.Info;

interface

uses
  {General interfaces}
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces,
  {Standard}
  System.Classes, Data.Db;

type
  TServerType = ( stUndefined, stMSSQL, stOracle );

  TDatabaseInfo = class( TInterfacedPersistent, ILoginObserver, IDatabaseInfo )
  strict private
    fDbName: string;
    fDbVersion: integer;
    fEventScale: integer;
    fServerType: TServerType;
    fServerVersion: string;
    fProductVersion: string;
    fProductMajorVersion: integer;
    fCollation: string;
    fServerName: string;
    fSimulatedVersion: integer;
    fWorkstationName: string;
    fSQL: ISQL;
    fLog: ILog;
    procedure VerifyDbVersion;
  protected
    { Property Accessors }
    function Get_Collation: string;
    function Get_DbVersion: integer;
    function Get_DbName: string;
    function Get_EventScale: integer;
    function Get_ServerName: string;
    function Get_ServerVersion: string;
    function Get_ProductVersion: string;
    function Get_ProductYear: integer;
    function Get_ServerType: TServerType;
    { Other members }
    function FriendlyName: string;
    function ProductVersionAtLeast( const AProductVersion: integer ): boolean;
    procedure LogAllFields( const ADataset: TDataset );
  public
    constructor Create( ASQL: ISQL; ALog: ILog );
    procedure AfterLogin( Sender: IDatabaseConnection );
    procedure SimulateVersion( const ASimulatedVersion: integer );
    procedure Refresh;
    function Is2005OrHigher: boolean;
    function Is2008OrHigher: boolean;
    function Is2012OrHigher: boolean;
    function Is2014OrHigher: boolean;
    function Is2016OrHigher: boolean;
  published
    property Collation: string read Get_Collation;
    property DbName: string read Get_DbName;
    property DbVersion: integer read Get_DbVersion;
    property EventScale: integer read Get_EventScale;
    property ProductMajorVersion: integer read fProductMajorVersion;
    property ProductVersion: string read Get_ProductVersion;
    property ProductYear: integer read Get_ProductYear;
    property ServerName: string read Get_ServerName;
    property ServerType: TServerType read Get_ServerType;
    property ServerVersion: string read Get_ServerVersion;
    property WorkstationName: string read fWorkstationName;
  end;

implementation

uses
  System.SysUtils;

resourcestring
  EXC_DATABASE_LEVEL = 'Databasen (versjon %d) må oppgraderes til versjon %d.';

const
  CMD_SET_XACTABORT  = 'SET XACT_ABORT ON';
  CMD_SET_DATEFORMAT = 'SET DATEFORMAT ymd';
  QRY_DATABASE_INFO  = 'EXEC dbo.GetDatabaseInfo';
  QRY_PROPERTIES     = 'SELECT SERVERPROPERTY(''ProductVersion'') AS ProductVersion,SERVERPROPERTY(''Collation'') AS Collation,' + 'SERVERPROPERTY(''ServerName'') AS ServerName,HOST_NAME() AS WorkstationName, DB_NAME() AS DatabaseName';

  { Field names used }
  FLD_DB_NAME        = 'DatabaseName';
  FLD_DB_VERSION     = 'DatabaseVersion';
  FLD_EVENT_SCALE    = 'EventScale';
  FLD_SERVER_TYPE    = 'ServerType';
  FLD_SERVER_VERSION = 'ServerVersion';
  MIN_DB_VERSION     = 510;

constructor TDatabaseInfo.Create( ASQL: ISQL; ALog: ILog );
begin
  inherited Create;
  fSQL := ASQL;
  fLog := ALog;
end;

procedure TDatabaseInfo.Refresh;
begin
  with fSQL.FastQuery( QRY_PROPERTIES ) do
    try
      fProductVersion := Fields[0].AsString;
      fCollation := Fields[1].AsString;
      fServerName := Fields[2].AsString;
      fWorkstationName := Fields[3].AsString;
      fDbName := FieldByName( FLD_DB_NAME ).AsString;
      fProductMajorVersion := StrToIntDef( Copy( fProductVersion, 1, Pos( '.', fProductVersion ) - 1 ), 0 );
      LogAllFields( Fields[0].DataSet );
    finally
      Close;
    end;
  with fSQL.FastQuery( QRY_DATABASE_INFO ) do
    try
      fServerType := TServerType( FieldByName( FLD_SERVER_TYPE ).AsInteger );
      fDbName := FieldByName( FLD_DB_NAME ).AsString;
      fDbVersion := FieldByName( FLD_DB_VERSION ).AsInteger;
      fServerVersion := FieldByName( FLD_SERVER_VERSION ).AsString;
      fEventScale := FieldByName( FLD_EVENT_SCALE ).AsInteger;
      LogAllFields( Fields[0].DataSet );
    finally
      Close;
    end;
end;

procedure TDatabaseInfo.SimulateVersion( const ASimulatedVersion: integer );
begin
  fSimulatedVersion := ASimulatedVersion;
end;

procedure TDatabaseInfo.VerifyDbVersion;
begin
  if ( fDbVersion < MIN_DB_VERSION ) and ( fDbVersion > 0 ) then
    raise Exception.CreateFmt( EXC_DATABASE_LEVEL, [fDbVersion, MIN_DB_VERSION] );
end;

procedure TDatabaseInfo.AfterLogin( Sender: IDatabaseConnection );
const
  PROC_NAME   = 'AfterLogin';
  LOG_VERSION = '%s.%s: ProductYear: %d ProductVersion: %s';
begin
  fLog.EnterMethod( Self, PROC_NAME );
  try
    if Supports( Sender, ISQL, fSQL ) then
    begin
      fSQL.ExecuteCommand( CMD_SET_XACTABORT );
      fSQL.ExecuteCommand( CMD_SET_DATEFORMAT );
      Refresh;
      VerifyDbVersion;
      if fSimulatedVersion <> 0 then
        fDbVersion := fSimulatedVersion;
      fLog.SilentSuccess( LOG_VERSION, [ClassName, PROC_NAME, ProductYear, ProductVersion] );
    end;
  except on E:Exception do
    begin
      fLog.SilentError( '%s.%s: %s (Setting DbVersion to -1)', [ClassName,PROC_NAME,E.Message] );
      fDbVersion := -1;
    end;
  end;
  fLog.LeaveMethod( Self, PROC_NAME );
end;

function TDatabaseInfo.FriendlyName: string;
begin
  Result := 'DatabaseInfo';
end;

function TDatabaseInfo.Get_Collation: string;
begin
  Result := fCollation;
end;

function TDatabaseInfo.Get_DbName: string;
begin
  Result := fDbName;
end;

function TDatabaseInfo.Get_DbVersion: integer;
begin
  Result := fDbVersion;
end;

function TDatabaseInfo.Get_EventScale: integer;
begin
  Result := fEventScale;
end;

function TDatabaseInfo.Get_ServerName: string;
begin
  Result := fServerName;
end;

function TDatabaseInfo.Get_ServerVersion: string;
begin
  Result := fServerVersion;
end;

function TDatabaseInfo.Get_ProductVersion: string;
begin
  Result := fProductVersion;
end;

function TDatabaseInfo.Get_ProductYear: integer;
begin
  case fProductMajorVersion of
    6: Result := 1996;
    7: Result := 1998;
    8: Result := 2000;
    9: Result := 2005;
    10: Result := 2008;
    11: Result := 2012;
    12: Result := 2014;
    13: Result := 2016;
    14: Result := 2017;
    15: Result := 2019;
    16: Result := 2022;
  else Result := 9999;
  end;
end;

function TDatabaseInfo.Get_ServerType: TServerType;
begin
  Result := fServerType;
end;

function TDatabaseInfo.Is2005OrHigher: boolean;
begin
  Result := ProductVersionAtLeast( 9 );
end;

function TDatabaseInfo.Is2008OrHigher: boolean;
begin
  Result := ProductVersionAtLeast( 10 );
end;

function TDatabaseInfo.Is2012OrHigher: boolean;
begin
  Result := ProductVersionAtLeast( 11 );
end;

function TDatabaseInfo.Is2014OrHigher: boolean;
begin
  Result := ProductVersionAtLeast( 12 );
end;

function TDatabaseInfo.Is2016OrHigher: boolean;
begin
  Result := ProductVersionAtLeast( 13 );
end;

function TDatabaseInfo.ProductVersionAtLeast( const AProductVersion: integer ): boolean;
begin
  Result := ( fProductMajorVersion >= AProductVersion );
end;

procedure TDatabaseInfo.LogAllFields( const ADataset: TDataset );
var
  i: integer;
begin
  with ADataset do
    for i := 0 to FieldCount - 1 do
      fLog.Event( '%s = %s', [Fields[i].FieldName, Fields[i].AsString] );
end;

end.
