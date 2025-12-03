unit Emetra.Settings.IniFile;

interface

uses
  Emetra.Business.BaseClass,
  {General interfaces}
  Emetra.Settings.Interfaces,
  Emetra.Dictionary.Interfaces,
  Emetra.Logging.Interfaces,
  {Standard}
  System.Classes, System.Inifiles, System.SysUtils, Winapi.Windows;

type
  TNotifyStringEvent = procedure( const s: string ) of object;

  /// <summary>
  /// <para>
  /// This is an implementation of settings interfaces that uses
  /// ini-files for storage.
  /// </para>
  /// <para>
  /// ssGlobal settings are stored in <b>Settings\emetra.ini</b> relative
  /// to the root directory passed in the constructor. The global
  /// settings file will contain a <i>root identifier</i> in key <b>
  /// Directory/Identifier</b>. This key will be created automatically if
  /// missing, containing a GUID (given necessary write-permissions).
  /// </para>
  /// <para>
  /// ssUser settings are stored in <b>
  /// %USERPROFILE%\AppData\Roaming\Emetra\Shared\&lt;root
  /// identifier&gt;.ini</b>. The root identifier is read from the global
  /// ini-file, see above. It will normally be a GUID, but any non-empty
  /// string will work. <br />
  /// </para>
  /// <para>
  /// ssMachineUser settings are stored in <b>
  /// %USERPROFILE%\AppData\Roaming\Emetra\Shared\&lt;Computer
  /// Name&gt;.ini</b><br /><br />
  /// </para>
  /// </summary>
  /// <remarks>
  /// Previously, the user settings were stored in <b>
  /// Settings\&lt;username&gt;.ini</b>. However, this required write
  /// permissions to the directory where the global settings reside. This
  /// would enable an unauthorized user to tamper with the global settings.
  /// </remarks>
  TIniSettings = class( TCustomBusinessReferenceCounted, IScopedSettingsRead, IScopedSettingsReadWrite, INumericDictionary )
  strict private
    fRootFolder: string;
    fDirectoryIdentifier: string;
    fCurrScope: TIniFile;
    fInstallIni: TIniFile;
    fLocalPrivateIni: TIniFile;
    fOnUserChange: TNotifyStringEvent;
    fUserIni: TIniFile;
    fWritableInstallIni: boolean;
    fWriteableUserIni: boolean;
  private
    { Property accessors }
    function Get_Scope: TSettingScope;
    function Get_FullAccess: boolean;
    function Get_InstallIni: TIniFile;
    function Get_UserIni: TIniFile;
    procedure Set_Scope( const AValue: TSettingScope );
    { Other members }
    function VerifyWriteAccess( const AIniFile: TIniFile ): boolean;
    function TryGetNumber( const AVarName: string; var AValue: Extended ): boolean;
    procedure WriteRegistryInfo;
    { Generate file names }
    procedure InitializeRootDirectoryIdentifier;
    function GetGlobalSettingsFileName: string;
    function GetRoamingUserSettingsFileName: string;
    function GetRoamingMachineUserSettingsFileName: string;
  protected
    fUserName: string;
    { Check for setting }
    function Exists( const AScope: TSettingScope; const AContext, AKey: string ): boolean;
    { Read settings }
    function ReadBool( const AScope: TSettingScope; const AContext, AKey: string; const ADefault: boolean = false ): boolean;
    function ReadDate( const AScope: TSettingScope; const AContext, AKey: string; const ADefault: TDateTime ): TDateTime;
    function ReadInteger( const AScope: TSettingScope; const AContext, AKey: string; const ADefault: Integer = 0 ): Integer;
    function ReadFloat( const AScope: TSettingScope; const AContext, AKey: string; const ADefault: double = 0 ): double;
    function ReadString( const AScope: TSettingScope; const AContext, AKey: string; const ADefault: string = '' ): string;
    { Write settings }
    procedure WriteBool( const AScope: TSettingScope; const AContext, AKey: string; const AValue: boolean );
    procedure WriteDateTime( const AScope: TSettingScope; const AContext, AKey: string; const AValue: TDateTime );
    procedure WriteFloat( const AScope: TSettingScope; const AContext, AKey: string; const AValue: double );
    procedure WriteInteger( const AScope: TSettingScope; const AContext, AKey: string; const AValue: Integer );
    procedure WriteString( const AScope: TSettingScope; const AContext, AKey, AValue: string );
    { Other members }
    procedure SelectUser( const AUserName: string );
    { Properties }
    property Install: TIniFile read Get_InstallIni;
    property User: TIniFile read Get_UserIni;
    property FullAccess: boolean read Get_FullAccess;
    property SettingsFile: string read GetGlobalSettingsFileName;
    property Username: string read fUserName;
    { Event properties }
    property OnUserChange: TNotifyStringEvent read fOnUserChange write fOnUserChange; { Event triggered when user is set }
  public
    { Initialization }
    constructor Create( const ARootFolder: string; ALog: ILog ); reintroduce; overload;
    constructor Create( ALog: ILog ); overload;
    constructor Create; overload;
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
  end;

implementation

uses
  {General}
  Emetra.Win.User,
  Emetra.Win.ShellFolders,
  {Standard}
  System.Win.Registry;

const
  EXC_CREATE_PRIVATE_SETTINGS_FAILED = '%s: Failed to create private settings file: "%s"';
  EXC_NO_GLOBAL_FILE                 = '%s: There is no global installation settings file.';
  EXC_NO_USER_FILE                   = '%s: There is no user settings file.';

const
  EMETRA_INI    = 'emetra.ini';
  SECTION_TEST  = 'Test';
  SECTION_DIR   = 'Directory';
  KEY_RANDOM    = 'Random';
  KEY_IDENTIFY  = 'Identifier';
  KEY_OPEN      = 'LastOpened';
  KEY_USER_NAME = 'WindowsUserName';
  KEY_APP_DIR   = 'AppDir';
  KEY_ROOT_DIR  = 'RootDir';

const
  APPDATA_SUBFOLDER = 'Emetra\Shared\';

function NormalizeFileName( const AInput: string ): string;
var
  charIdx: Integer;
begin
  Result := EmptyStr;
  charIdx := 1;
  while charIdx < Length( AInput ) do
  begin
    case AInput[charIdx] of
      'A' .. 'Z', '0' .. '9', 'a' .. 'z', '-': Result := Result + AInput[charIdx];
      ':', '{', '}':;
    else Result := Result + '_';
    end;
    inc( charIdx );
  end;
end;

{$REGION 'Initialization'}

constructor TIniSettings.Create;
begin
  Create( EmptyStr, GlobalLog );
end;

constructor TIniSettings.Create( ALog: ILog );
begin
  Create( EmptyStr, ALog );
end;

constructor TIniSettings.Create( const ARootFolder: string; ALog: ILog );
begin
  inherited Create( ALog );

  { Find root folder based on input or executable }

  if ARootFolder = EmptyStr then
    fRootFolder := ExtractFilePath( ParamStr( 0 ) )
  else
    fRootFolder := IncludeTrailingPathDelimiter( ARootFolder );

end;

procedure TIniSettings.AfterConstruction;
begin
inherited;
  { Create folder and open ini-file for Global settings from <root>\Settings }

  try
    ForceDirectories( ExtractFilePath( GetGlobalSettingsFileName ) );
    fInstallIni := TIniFile.Create( GetGlobalSettingsFileName );
    fWritableInstallIni := VerifyWriteAccess( fInstallIni );
    InitializeRootDirectoryIdentifier;
  except
    on E: Exception do
      FreeAndNil( fInstallIni );
  end;

  { Generate folder and open local private file }

  try
    ForceDirectories( ExtractFilePath( GetRoamingMachineUserSettingsFileName ) );
    fLocalPrivateIni := TIniFile.Create( GetRoamingMachineUserSettingsFileName );
    VerifyWriteAccess( fLocalPrivateIni );
  except
    on E: Exception do
    begin
      FreeAndNil( fLocalPrivateIni );
      raise EAssertionFailed.CreateFmt( EXC_CREATE_PRIVATE_SETTINGS_FAILED, [ClassName, GetRoamingMachineUserSettingsFileName] );
    end;
  end;

  SelectUser( GetWindowsUserName );
  WriteRegistryInfo;

end;

procedure TIniSettings.BeforeDestruction;
begin
  fCurrScope := nil;
  if fUserIni <> nil then
    FreeAndNil( fUserIni );
  if fInstallIni <> nil then
    FreeAndNil( fInstallIni );
  if fLocalPrivateIni <> nil then
    FreeAndNil( fLocalPrivateIni );
  inherited;
end;

procedure TIniSettings.InitializeRootDirectoryIdentifier;
var
  rootDirGuid: TGuid;
  rootDirIdentifier: string;
begin
  rootDirIdentifier := fInstallIni.ReadString( SECTION_DIR, KEY_IDENTIFY, EmptyStr );
  if ( rootDirIdentifier = EmptyStr ) then
  begin
    if fWritableInstallIni then
    begin
      { Create an identifier and save it }
      CreateGuid( rootDirGuid );
      rootDirIdentifier := rootDirGuid.ToString;
      fInstallIni.WriteString( SECTION_DIR, KEY_IDENTIFY, rootDirIdentifier );
    end
    else
      { Generate identifier from root path }
      rootDirIdentifier := fRootFolder;
  end;
  fDirectoryIdentifier := NormalizeFileName( rootDirIdentifier );
end;

{$ENDREGION}
{$REGION 'Files and folders'}

function TIniSettings.GetGlobalSettingsFileName: string;
begin
  Result := fRootFolder + 'Settings\' + EMETRA_INI;
end;

function TIniSettings.GetRoamingMachineUserSettingsFileName: string;
var
  appDataFolder: string;
begin
  if ShellGetFolderPath( CSIDL_APPDATA, appDataFolder ) then
    Result := IncludeTrailingPathDelimiter( appDataFolder ) + APPDATA_SUBFOLDER + GetWindowsComputerName + '.ini'
  else
    Result := fRootFolder + 'Settings\' + GetWindowsComputerName + '-' + GetWindowsUserName + '.ini';
end;

function TIniSettings.GetRoamingUserSettingsFileName: string;
var
  appDataFolder: string;
begin
  if not ShellGetFolderPath( CSIDL_APPDATA, appDataFolder ) then
    appDataFolder := fRootFolder + 'Settings';
  Result := IncludeTrailingPathDelimiter( appDataFolder ) + APPDATA_SUBFOLDER + fDirectoryIdentifier + '.ini';
end;

{$ENDREGION}

procedure TIniSettings.WriteRegistryInfo;
var
  appKey: TRegistryIniFile;
  fileName: string;
begin
  appKey := TRegistryIniFile.Create( 'Software\Emetra' );
  try
    fileName := ChangeFileExt( ExtractFileName( ParamStr( 0 ) ), EmptyStr );
    appKey.WriteString( fileName, KEY_APP_DIR, ExtractFilePath( ParamStr( 0 ) ) );
    appKey.WriteDateTime( fileName, KEY_OPEN, Now );
    appKey.WriteString( fileName, KEY_USER_NAME, fUserName );
  finally
    appKey.Free;
  end;
end;

procedure TIniSettings.SelectUser( const AUserName: string );
begin
  if SameText( AUserName, fUserName ) then
    exit
  else
    fUserName := AUserName;
  if fUserIni <> nil then
    FreeAndNil( fUserIni );
  if ( AUserName <> EmptyStr ) then
  begin
    fUserIni := TIniFile.Create( GetRoamingUserSettingsFileName );
    fWriteableUserIni := VerifyWriteAccess( fUserIni );
    if fWriteableUserIni then
  end;
  if Assigned( fOnUserChange ) then
    fOnUserChange( AUserName );
end;

function TIniSettings.VerifyWriteAccess( const AIniFile: TIniFile ): boolean;
begin
  Result := false;
  try
    AIniFile.WriteString( SECTION_DIR, KEY_ROOT_DIR, fRootFolder );
    AIniFile.WriteDateTime( SECTION_TEST, KEY_OPEN, Now );
    AIniFile.WriteString( SECTION_TEST, KEY_USER_NAME, GetWindowsUserName );
    Result := true;
  except
    on Exception do
  end;
end;

{$REGION 'Property accessors'}

function TIniSettings.Get_FullAccess: boolean;
begin
  Result := fWriteableUserIni and fWritableInstallIni;
end;

function TIniSettings.Get_InstallIni: TIniFile;
begin
  if fInstallIni = nil then
    raise EAssertionFailed.CreateFmt( EXC_NO_GLOBAL_FILE, [ClassName] )
  else
    Result := fInstallIni;
end;

function TIniSettings.Get_UserIni: TIniFile;
begin
  if fUserIni = nil then
    raise EAssertionFailed.CreateFmt( EXC_NO_USER_FILE, [ClassName] )
  else
    Result := fUserIni;
end;
{$ENDREGION}
{$REGION 'ISettings interface'}

procedure TIniSettings.Set_Scope( const AValue: TSettingScope );
begin
  if AValue = Get_Scope then
    exit;
  case AValue of
    ssGlobal: fCurrScope := fInstallIni;
    ssUser: fCurrScope := fUserIni;
    ssMachineUser: fCurrScope := fLocalPrivateIni;
  else fCurrScope := nil;
  end;
end;

function TIniSettings.TryGetNumber( const AVarName: string; var AValue: Extended ): boolean;
var
  strScope: string;
  strSection: string;
  strKey: string;
  lstParts: TStringList;
  thisScope: TSettingScope;
begin
  Result := false;
  lstParts := TStringList.Create;
  try
    lstParts.Delimiter := '.';
    lstParts.StrictDelimiter := true;
    lstParts.Text := AVarName;
    if lstParts.Count = 3 then
    begin
      strScope := lstParts[0];
      strSection := lstParts[1];
      strKey := lstParts[2];
      if SameText( strScope, 'Global' ) then
        thisScope := ssGlobal
      else if SameText( strScope, 'User' ) then
        thisScope := ssUser
      else
        thisScope := ssMachineUser;
      AValue := ReadFloat( thisScope, strSection, strScope, AValue );
    end;
  finally
    lstParts.Free;
  end;
end;

function TIniSettings.Get_Scope: TSettingScope;
begin
  if fCurrScope = fUserIni then
    Result := ssUser
  else if fCurrScope = fInstallIni then
    Result := ssGlobal
  else if fCurrScope = fLocalPrivateIni then
    Result := ssMachineUser
  else
    Result := ssUndefined;
end;

function TIniSettings.Exists( const AScope: TSettingScope; const AContext, AKey: string ): boolean;
begin
  Set_Scope( AScope );
  Result := fCurrScope.ValueExists( AContext, AKey );
end;

function TIniSettings.ReadBool( const AScope: TSettingScope; const AContext, AKey: string; const ADefault: boolean = false ): boolean;
begin
  Set_Scope( AScope );
  if Assigned( fCurrScope ) then
    Result := fCurrScope.ReadBool( AContext, AKey, ADefault )
  else
    Result := ADefault;
end;

function TIniSettings.ReadInteger( const AScope: TSettingScope; const AContext, AKey: string; const ADefault: Integer = 0 ): Integer;
begin
  Set_Scope( AScope );
  if Assigned( fCurrScope ) then
    Result := fCurrScope.ReadInteger( AContext, AKey, ADefault )
  else
    Result := ADefault;
end;

function TIniSettings.ReadFloat( const AScope: TSettingScope; const AContext, AKey: string; const ADefault: double = 0 ): double;
begin
  Set_Scope( AScope );
  if Assigned( fCurrScope ) then
    Result := fCurrScope.ReadFloat( AContext, AKey, ADefault )
  else
    Result := ADefault;
end;

function TIniSettings.ReadString( const AScope: TSettingScope; const AContext, AKey: string; const ADefault: string = '' ): string;
begin
  Set_Scope( AScope );
  if Assigned( fCurrScope ) then
    Result := fCurrScope.ReadString( AContext, AKey, ADefault )
  else
    Result := ADefault;
end;

function TIniSettings.ReadDate( const AScope: TSettingScope; const AContext, AKey: string; const ADefault: TDateTime ): TDateTime;
begin
  Set_Scope( AScope );
  if Assigned( fCurrScope ) then
    Result := fCurrScope.ReadDate( AContext, AKey, ADefault )
  else
    Result := ADefault;
end;

procedure TIniSettings.WriteString( const AScope: TSettingScope; const AContext, AKey: string; const AValue: string );
begin
  Set_Scope( AScope );
  if Assigned( fCurrScope ) then
    fCurrScope.WriteString( AContext, AKey, AValue );
end;

procedure TIniSettings.WriteInteger( const AScope: TSettingScope; const AContext, AKey: string; const AValue: Integer );
begin
  Set_Scope( AScope );
  if Assigned( fCurrScope ) then
    fCurrScope.WriteInteger( AContext, AKey, AValue );
end;

procedure TIniSettings.WriteFloat( const AScope: TSettingScope; const AContext, AKey: string; const AValue: double );
begin
  Set_Scope( AScope );
  if Assigned( fCurrScope ) then
    fCurrScope.WriteFloat( AContext, AKey, AValue );
end;

procedure TIniSettings.WriteDateTime( const AScope: TSettingScope; const AContext, AKey: string; const AValue: TDateTime );
begin
  Set_Scope( AScope );
  if Assigned( fCurrScope ) then
    fCurrScope.WriteDateTime( AContext, AKey, AValue );
end;

procedure TIniSettings.WriteBool( const AScope: TSettingScope; const AContext, AKey: string; const AValue: boolean );
begin
  Set_Scope( AScope );
  if Assigned( fCurrScope ) then
    fCurrScope.WriteBool( AContext, AKey, AValue );
end;

{$ENDREGION}

initialization

SetThreadLocale( GetUserDefaultLCID );
GetFormatSettings;

end.
