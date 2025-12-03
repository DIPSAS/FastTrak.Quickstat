unit Emetra.Settings.Test;

interface

const
  EXE_ROOT      = 'c:\work\FastTrak\';
  LOOKUP_FOLDER = EXE_ROOT + 'Lookup\';
  CRF_FOLDER    = 'c:\work\Azure.FastTrakDownloads\';

function MSG_ROOT: string;

function WinConnection( const ADatabase: string ): string;
{ Used in development }
function TrustedConnection( const AServer, ADatabase: string ): string;
function ParentFolder: string;
function DevConnection: string;
function DevSessionRoot: string;
function DevMetaConnection: string;
function ProdMetaConnection: string;
function FastTrakConnection: string; // Using FastTrak.UDL in same folder.
function FastTrakParentConnection: string; // Using FastTrak.UDL in parent folder
function FastTrakUdl: string;
function FastTrakUdlInParent: string;
function FastTrakFolder: string;
function DevSqlConnection: string;
function TestServerConnection: string;

implementation

uses
  Emetra.Win.User,
  System.SysUtils,
  StrUtils,
  IOUtils, Classes;

const
  DEV_CONNECTION  = 'Provider=SQLNCLI11.1;Data Source=%s;Initial Catalog=%s;Integrated Security=SSPI;';
  META_CONNECTION = 'Provider=SQLNCLI11.1;Data Source=meta.emetra.no;Initial Catalog=%s;Integrated Security=SSPI;';
  WIN_CONNECTION  = 'Provider=SQLOLEDB.1;Data Source=%s;Initial Catalog=%s;Integrated Security=SSPI;';
  SQL_CONNECTION  = 'Provider=SQLNCLI11.1;User ID=%s;Password=%s;Initial Catalog=%s;Data Source=%s;';

function MSG_ROOT: string;
begin
  Result := FastTrakFolder + 'Messages\KithTestCases\';
end;

function DevSessionRoot: string;
begin
  Result := CRF_FOLDER;
end;

function FastTrakUdl: string;
begin
  Result := ExtractFilePath( ParamStr( 0 ) ) + 'FastTrak.UDL';
end;

function ParentFolder: string;
var
  pathElements: TStringList;
begin
  pathElements := TStringList.Create;
  try
    pathElements.Delimiter := '\';
    pathElements.StrictDelimiter := true;
    pathElements.DelimitedText := ExtractFilePath( ParamStr( 0 ) );
    pathElements.Delete( pathElements.Count - 1 );
    pathElements.Delete( pathElements.Count - 1 );
    Result := pathElements.DelimitedText + '\';
  finally
    pathElements.Free;
  end;
end;

function FastTrakUdlInParent: string;
begin
  Result := ParentFolder + 'FastTrak.UDL';
end;

function FastTrakConnection: string;
begin
  Result := 'FILE NAME=' + FastTrakUdl;
end;

function FastTrakParentConnection: string;
begin
  Result := 'FILE NAME=' + FastTrakUdlInParent;
end;

function FastTrakFolder: string;
begin
  Result := IncludeTrailingPathDelimiter( System.SysUtils.GetEnvironmentVariable( 'FastTrakDir' ) );
  if Result = EmptyStr then
    Result := EXE_ROOT;
end;

function TrustedConnection( const AServer, ADatabase: string ): string;
begin
  Result := Format( DEV_CONNECTION, [AServer, ADatabase] );
end;

function TestServerConnection: string;
begin
  Result := Format( DEV_CONNECTION, ['VD-SQLFAST02', 'EFT00000-TEST'] );
end;

function DevSqlConnection: string;
begin
  Result := Format( SQL_CONNECTION, ['test', 'test', 'GBD', 'localhost'] );
end;

function DevConnection: string;
begin
  Result := TrustedConnection( GetWindowsComputerName, 'GBD' );
end;

function ProdMetaConnection: string;
begin
  Result := Format( META_CONNECTION, ['meta.emetra.no'] );
end;

function DevMetaConnection: string;
begin
  Result := Format( META_CONNECTION, ['test.meta.emetra.no'] );
end;

function WinConnection( const ADatabase: string ): string;
var
  hostName: string;
begin
  if GetWindowsComputerName = 'WIN' then
    hostName := 'WIN'
  else
    hostName := 'win.emetra.no';
  Result := Format( WIN_CONNECTION, [hostName, ADatabase] );
end;

end.
