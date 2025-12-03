unit Emetra.Database.Async;

interface

uses
  Emetra.Logging.Interfaces,
  Data.Win.AdoDB, System.Classes;

type
  TSqlCommandThread = class( TThread )
  strict private
    fConnection: TADOConnection;
    fCommand: TADOCommand;
    fLog: ILog;
  private
    procedure PrepareCommandParameters( const AParams: array of Variant );
  public
    { Initialization }
    constructor Create( const AConnectionString: string; const ASQL: string; const AParams: array of Variant; const ALog: ILog ); reintroduce; overload;
    constructor Create( const AConnectionString: string; const ASQL: string; const ALog: ILog ); overload;
    destructor Destroy; override;
    { Other methods }
    procedure Execute; override;
  end;

implementation

uses
  System.SysUtils;

{ TSqlCommandThread }

constructor TSqlCommandThread.Create( const AConnectionString: string; const ASQL: string; const ALog: ILog );
begin
  Create( AConnectionString, ASQL, [], ALog );
end;

constructor TSqlCommandThread.Create( const AConnectionString: string; const ASQL: string; const AParams: array of Variant; const ALog: ILog );
begin
  inherited Create;
  FreeOnTerminate := true;
  fLog := ALog;
  { Create new connection, as the TADOConnection is not threadsafe }
  fConnection := TADOConnection.Create( nil );
  fConnection.ConnectionString := AConnectionString;
  fConnection.LoginPrompt := false;
  { Create command owned by connection }
  fCommand := TADOCommand.Create( fConnection );
  fCommand.Connection := fConnection;
  { Do ParamCheck only if any parameters are given }
  fCommand.ParamCheck := ( high( AParams ) <> -1 );
  fCommand.CommandText := ASQL;
  if FCommand.ParamCheck then
    PrepareCommandParameters( AParams );
end;

destructor TSqlCommandThread.Destroy;
begin
  fConnection.Destroy;
  inherited;
end;

procedure TSqlCommandThread.Execute;
begin
  fConnection.Connected := true;
  fCommand.Execute;
  fLog.LogSqlCommand( '*' + FCommand.CommandText );
end;

procedure TSqlCommandThread.PrepareCommandParameters( const AParams: array of Variant );
var
  n: integer;
begin
  n := 0;
  while n < fCommand.Parameters.Count do
  begin
    fCommand.Parameters[n].Value := AParams[n];
    inc( n );
  end;
end;

end.
