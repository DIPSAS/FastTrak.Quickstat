{$ALIGN ON, $BOOLEVAL OFF, $LONGSTRINGS ON, $IOCHECKS ON, $WRITEABLECONST OFF}
{$OVERFLOWCHECKS OFF, $RANGECHECKS OFF, $TYPEDADDRESS ON, $MINENUMSIZE 1}
unit Emetra.Win.ShellFolders;

interface

uses
  Windows, SysUtils;

const
  CSIDL_PERSONAL             = $0005;      // My Documents
  CSIDL_STARTUP              = $0007;      // Startup
  CSIDL_COMMON_STARTUP       = $0018;      // Common startup
  CSIDL_APPDATA              = $001A;      // Application Data, new for NT4

  CSIDL_LOCAL_APPDATA        = $001C;      // non roaming, user\Local Settings\Application Data
  CSIDL_INTERNET_CACHE       = $0020;
  CSIDL_COOKIES              = $0021;
  CSIDL_HISTORY              = $0022;
  CSIDL_DESKTOP              = 16;
  CSIDL_COMMON_DESKTOP       = 25;
  CSIDL_COMMON_APPDATA       = $0023;      // All Users\Application Data
  CSIDL_WINDOWS              = $0024;      // GetWindowsDirectory()
  CSIDL_SYSTEM               = $0025;      // GetSystemDirectory()
  CSIDL_PROGRAM_FILES        = $0026;      // C:\Program Files
  CSIDL_MYPICTURES           = $0027;      // My Pictures, new for Win2K
  CSIDL_PROGRAM_FILES_COMMON = $002b;      // C:\Program Files\Common
  CSIDL_COMMON_DOCUMENTS     = $002e;      // All Users\Documents

  CSIDL_COMMON_ADMINTOOLS    = $002f;      // All Users\Start Menu\Programs\Administrative Tools
  CSIDL_ADMINTOOLS           = $0030;      // <user name>\Start Menu\Programs\Administrative Tools

  CSIDL_FLAG_CREATE          = $8000;      // new for Win2K, or this in to force creation of folder

  SHGFP_TYPE_CURRENT  = 0;   // current value for user, verify it exists
  SHGFP_TYPE_DEFAULT  = 1;   // default value, may not exist


function GetShellFolder( ACSIDL: integer ): string;
function ShellGetFolderPath(ACSIDL:Integer; var APath:String):Boolean;
function ShellGetQuickLaunchPath( var APath: string ): boolean;
function SHGetFolderPath(hwndOwner:HWnd; nFolder:Integer; hToken:THandle;
                         dwFlags:DWord; pszPath:LPSTR):HRESULT; stdcall;
function SHGetFolderPathA(hwndOwner:HWnd; nFolder:Integer; hToken:THandle;
                          dwFlags:DWord; pszPath:LPSTR):HRESULT; stdcall;
function SHGetFolderPathW(hwndOwner:HWnd; nFolder:Integer; hToken:THandle;
                          dwFlags:DWord; pszPath:LPWSTR):HRESULT; stdcall;
implementation

function SHGetFolderPath; external 'SHFolder.dll' name 'SHGetFolderPathA';
function SHGetFolderPathA; external 'SHFolder.dll' name 'SHGetFolderPathA';
function SHGetFolderPathW; external 'SHFolder.dll' name 'SHGetFolderPathW';

function GetShellFolder( ACSIDL: integer ): string;
begin
  if not ShellGetFolderPath( ACSIDL, Result ) then
    Result := ''
  else
    Result := IncludeTrailingPathDelimiter( Result );
end;

function ShellGetFolderPath(ACSIDL:Integer; var APath:String):Boolean;
var
  shellPath: array[0..MAX_PATH*2] of AnsiChar;
begin
  Result := SHGetFolderPath(0,ACSIDL,0,SHGFP_TYPE_CURRENT,shellPath)=S_OK;
  APath := string( shellPath );
end;

function ShellGetQuickLaunchPath( var APath: string ): boolean;
begin
  Result := ShellGetFolderPath( CSIDL_APPDATA, APath );
  if Result then
    APath := APath + '\Microsoft\Internet Explorer\Quick Launch'
  else
    Result := false;
end;

end.

