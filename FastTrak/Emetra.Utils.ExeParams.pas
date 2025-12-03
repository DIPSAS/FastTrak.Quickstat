unit Emetra.Utils.ExeParams;

interface

uses
  Classes;

type
  TExeParams = class( TStringList )
  public
    constructor Create; reintroduce;
    function AutoRun: boolean;
    function AutoClose: boolean;
    function GetOverride( const AKey: string; var AValue: string ): boolean;
  end;

var
  ExeParams: TExeParams;

procedure SetStartupDir;

implementation

uses
  SysUtils;

procedure SetStartupDir;
var
  strStartDir: string;
begin
  strStartDir := ExeParams.Values['DIR'];
  if (strStartDir <> EmptyStr) and DirectoryExists(strStartDir) then
    SetCurrentDir(strStartDir)
  else
    SetCurrentDir( ExtractFilePath( ExeParams[0] ) );
end;

{ TExeParams }

constructor TExeParams.Create;
var
  n: Integer;
begin
  inherited;
  CaseSensitive := false;
  for n := 0 to ParamCount do
    Add(ParamStr(n));
end;

function TExeParams.GetOverride(const AKey: string; var AValue: string): boolean;
begin
  Result := IndexOfName( AKey ) <> -1;
  if Result then
    AValue := Values[AKey];
end;

function TExeParams.AutoClose: boolean;
begin
  Result := IndexOf( 'AutoClose' ) <> -1;
end;

function TExeParams.AutoRun: boolean;
begin
  Result := IndexOf( 'AutoRun' ) <> -1;
end;

initialization

ExeParams := TExeParams.Create;

finalization

ExeParams.Free;

end.
