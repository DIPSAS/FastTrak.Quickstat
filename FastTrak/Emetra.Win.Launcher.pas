unit Emetra.Win.Launcher;

interface

uses
  Emetra.Interfaces.Launcher,
  Classes;

type
  TMrLauncher = class( TInterfacedObject, ILauncher )
  private
    FProcessMessages: boolean;
    FFileName: string;
    FParameters: string;
    FVisibility: integer;
    FWaitUntilFinished: boolean;
    function Execute: Cardinal; overload;
  public
    constructor Create;
    destructor Destroy; override;
    function ExecuteAndWait( const AFileName, AParams: string; AVisibility: integer = 1 ): longword;
    procedure Execute( const AFileName, AParams: string ); overload;
    property Parameters: string read FParameters;
  end;

implementation

uses
  Windows, Forms, SysUtils;

{ http://blog.delphi-jedi.net/2008/04/11/createprocess-in-full-glory/ }
{ http://www.swissdelphicenter.ch/torry/showcode.php?id=93 }
{ http://msdn.microsoft.com/en-us/library/windows/desktop/ms633548(v=vs.85).aspx }

constructor TMrLauncher.Create;
begin
  FWaitUntilFinished := true;
end;

destructor TMrLauncher.Destroy;
begin
  Assert( FRefCount = 0, Format( '%s: RefCount = %d', [Self.ClassName,FRefCount] ) );
  inherited;
end;

procedure TMrLauncher.Execute(const AFileName, AParams: string);
begin
  FParameters := AParams;
  FFileName := AFileName;
  FVisibility := 1;
  FProcessMessages := true;
  Execute;
end;

function TMrLauncher.ExecuteAndWait( const AFileName, AParams: string; AVisibility: Integer = 1 ): Cardinal;
begin
  FParameters := AParams;
  FFileName := AFileName;
  FVisibility := AVisibility;
  Result := Execute;
end;

function TMrLauncher.Execute: Cardinal;
var
  zAppName: array[0..512] of Char;
  zCurDir: array[0..255] of Char;
  WorkDir: string;
  StartupInfo: TStartupInfo;
  ProcessInfo: TProcessInformation;
begin
  StrPCopy(zAppName, Trim( Format( '"%s" %s', [FFileName,FParameters] ) ) );
  GetDir(0, WorkDir);
  StrPCopy(zCurDir, WorkDir);
  FillChar( StartUpInfo, SizeOf(StartupInfo), #0 );
  StartupInfo.cb          := SizeOf(StartupInfo);
  StartupInfo.dwFlags     := STARTF_USESHOWWINDOW;
  StartupInfo.wShowWindow := FVisibility;
  if not CreateProcess(nil,
    zAppName, // pointer to command line string
    nil, // pointer to process security attributes
    nil, // pointer to thread security attributes
    False, // handle inheritance flag
    CREATE_NEW_CONSOLE or // creation flags
    NORMAL_PRIORITY_CLASS,
    nil, //pointer to new environment block
    zCurDir, // pointer to current directory name
    StartupInfo, // pointer to STARTUPINFO
    ProcessInfo) // pointer to PROCESS_INF
    then Result := WAIT_FAILED
  else
  begin
    if FProcessMessages then
    while WaitForSingleObject(ProcessInfo.hProcess, 0) = WAIT_TIMEOUT do
    begin
      Application.ProcessMessages;
      Sleep(50);
    end;;
    WaitForSingleObject(ProcessInfo.hProcess, INFINITE);
    GetExitCodeProcess(ProcessInfo.hProcess, Result);
    CloseHandle(ProcessInfo.hProcess);
    CloseHandle(ProcessInfo.hThread);
  end;
end;

end.
