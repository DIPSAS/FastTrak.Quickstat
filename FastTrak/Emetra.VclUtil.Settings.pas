unit Emetra.VclUtil.Settings;

interface

uses
  {Standard}
  Emetra.VclUtil.Settings.Interfaces,
  {General}
  Emetra.Logging.Interfaces,
  Emetra.Settings.Interfaces,
  {Emetra.Vcl}
  Emetra.Vcl.ExtCtrls,
  {Standard}
  WinApi.Windows,
  Vcl.Forms, Vcl.Graphics, Vcl.ExtCtrls, Vcl.Controls,
  System.Classes, System.SysUtils, System.TypInfo, System.Math;

type
  TGuiSettings = class( TInterfacedObject, IGuiSettings )
  private
    FMainForm: TForm;
    FSettings: IScopedSettingsReadWrite;
    FLog: ILog;
    function ScreenKey: string;
    function FormKey: string;
    function HasFile: boolean;
  protected
    { All methods are protected. Only usable through interface }
    function TryGetColor( out AColor: TColor ): boolean;
    function TryGetFont( out AFontName: string; out AFontSize: integer ): boolean;
    function TryGetProperyValue( AProperty: string; out AValue: integer; AIniKey: string = '' ): boolean;
    procedure SaveFont( const AFontName: string; const AFontSize: integer );
    procedure SaveColor( const AColor: TColor );
    procedure RestoreControlHeight( AControl: TControl; AIniKey: string = '' );
    procedure RestoreControlWidth( AControl: TControl; AIniKey: string = '' );
    procedure RestorePanelHeight( APanel: TCustomPanel; AIniKey: string = '' );
    procedure RestorePanelWidth( APanel: TCustomPanel; AIniKey: string = '' );
    procedure RestoreSplitter( ASplitter: TComponent; AIniKey: string = '' );
    procedure RestoreSlidein( ASlidein: TdcSlidein; AIniKey: string = '' );
    procedure RestoreFormState;
    procedure SaveControlHeight( AControl: TControl; AIniKey: string = '' );
    procedure SaveControlWidth( AControl: TControl; AIniKey: string = '' );
    procedure SavePanelHeight( APanel: TCustomPanel; AIniKey: string = '' );
    procedure SavePanelWidth( APanel: TCustomPanel; AIniKey: string = '' );
    procedure SaveProperyValue( AProperty: string; AValue: integer; AIniKey: string = '' );
    procedure SaveSplitter( ASplitter: TComponent; AIniKey: string = '' );
    procedure SaveSlidein( ASlidein: TdcSlidein; AIniKey: string = '' );
    procedure SaveFormState;
  public
    constructor Create( AMainForm: TForm; ASettings: IScopedSettingsReadWrite; ALog: ILog );
    class function RectIsVisibleOnMonitors( const ARect: TRect ): boolean;
  end;

implementation

const
  { Componennt property values }
  PROP_LEFT     = 'Left';
  PROP_TOP      = 'Top';
  PROP_WIDTH    = 'Width';
  PROP_SIZE     = 'Position';
  PROP_HEIGHT   = 'Height';
  PROP_STATE    = 'State';
  PROP_EXPANDED = 'Expanded';
  { Inifile keys }
  KEY_WIDTH    = '.' + PROP_WIDTH;
  KEY_HEIGHT   = '.' + PROP_HEIGHT;
  KEY_SIZE     = '.' + PROP_SIZE;
  KEY_EXPANDED = '.' + PROP_EXPANDED;
  { Error messages }
  ERR_SAVE = '%s.Save: %s';
  ERR_LOAD = '%s.Load: %s';

constructor TGuiSettings.Create( AMainForm: TForm; ASettings: IScopedSettingsReadWrite; ALog: ILog );
begin
  inherited Create;
  FMainForm := AMainForm;
  FSettings := ASettings;
  FLog := ALog;
end;

function TGuiSettings.FormKey: string;
begin
{$IFDEF FMX}
  Result := FMainForm.Name;
{$ELSE}
  Result := Format( '%s.%dx%d', [FMainForm.Name, Screen.Width, Screen.Height] );
{$ENDIF}
end;

function TGuiSettings.ScreenKey: string;
begin
{$IFDEF FMX}
  Result := 'Screen';
{$ELSE}
  Result := Format( 'Screen%dx%d', [Screen.Width, Screen.Height] );
{$ENDIF}
end;

function TGuiSettings.HasFile: boolean;
begin
  Result := Assigned( FSettings );
end;

procedure TGuiSettings.RestoreSlidein( ASlidein: TdcSlidein; AIniKey: string );
begin
  if AIniKey = '' then
    AIniKey := ScreenKey;
  if HasFile then
    try
      ASlidein.Expanded := FSettings.ReadBool( ssUser, AIniKey, ASlidein.Name + KEY_EXPANDED, true );
      ASlidein.FullWidth := FSettings.ReadInteger( ssUser, AIniKey, ASlidein.Name + KEY_WIDTH, ASlidein.DefaultSize );
    except
      on E: Exception do
        FLog.Event( ERR_LOAD, [ClassName, E.Message] );
    end;
end;

procedure TGuiSettings.RestoreSplitter( ASplitter: TComponent; AIniKey: string = '' );
var
  absoluteValue: integer;
begin
  if AIniKey = '' then
    AIniKey := ScreenKey;
  if HasFile then
    try
      absoluteValue := GetOrdProp( ASplitter, PROP_SIZE );
      absoluteValue := FSettings.ReadInteger( ssUser, AIniKey, ASplitter.Name + KEY_SIZE, absoluteValue );
      SetOrdProp( ASplitter, PROP_SIZE, absoluteValue );
    except
      on E: Exception do
        FLog.Event( ERR_LOAD, [ClassName, E.Message] );
    end;
end;

procedure TGuiSettings.SaveSlidein( ASlidein: TdcSlidein; AIniKey: string );
begin
  if AIniKey = '' then
    AIniKey := ScreenKey;
  if HasFile then
    try
      FSettings.WriteBool( ssUser, AIniKey, ASlidein.Name + KEY_EXPANDED, ASlidein.Expanded );
      FSettings.WriteInteger( ssUser, AIniKey, ASlidein.Name + KEY_WIDTH, IfThen( ASlidein.Expanded, ASlidein.Width, ASlidein.FullWidth ) );
    except
      on E: Exception do
        FLog.Event( ERR_SAVE, [ClassName, E.Message] );
    end;
end;

procedure TGuiSettings.SaveSplitter( ASplitter: TComponent; AIniKey: string = '' );
var
  absoluteValue: integer;
begin
  if AIniKey = '' then
    AIniKey := ScreenKey;
  if HasFile then
    try
      absoluteValue := GetOrdProp( ASplitter, PROP_SIZE );
      FSettings.WriteInteger( ssUser, AIniKey, ASplitter.Name + KEY_SIZE, absoluteValue );
    except
      on E: Exception do
        FLog.Event( ERR_SAVE, [ClassName, E.Message] );
    end;
end;

procedure TGuiSettings.RestorePanelHeight( APanel: TCustomPanel; AIniKey: string = '' );
begin
  if AIniKey = '' then
    AIniKey := ScreenKey;
  if HasFile then
    APanel.Height := FSettings.ReadInteger( ssUser, AIniKey, APanel.Name + KEY_HEIGHT, APanel.Height );
end;

procedure TGuiSettings.SavePanelHeight( APanel: TCustomPanel; AIniKey: string = '' );
begin
  if AIniKey = '' then
    AIniKey := ScreenKey;
  if HasFile then
    try
      FSettings.WriteInteger( ssUser, AIniKey, APanel.Name + KEY_HEIGHT, APanel.Height );
    except
      on E: Exception do
        FLog.Event( ERR_SAVE, [ClassName, E.Message] );
    end;
end;

procedure TGuiSettings.RestorePanelWidth( APanel: TCustomPanel; AIniKey: string = '' );
begin
  if AIniKey = '' then
    AIniKey := ScreenKey;
  if HasFile then
    APanel.Width := FSettings.ReadInteger( ssUser, AIniKey, APanel.Name + KEY_WIDTH, APanel.Width );
end;

procedure TGuiSettings.SavePanelWidth( APanel: TCustomPanel; AIniKey: string = '' );
begin
  if AIniKey = '' then
    AIniKey := ScreenKey;
  if HasFile then
    try
      FSettings.WriteInteger( ssUser, AIniKey, APanel.Name + KEY_WIDTH, APanel.Width );
    except
      on E: Exception do
        FLog.Event( ERR_SAVE, [ClassName, E.Message] );
    end;
end;

procedure TGuiSettings.SaveProperyValue( AProperty: string; AValue: integer; AIniKey: string );
begin
  if AIniKey = '' then
    AIniKey := ScreenKey;
  if HasFile then
    try
      FSettings.WriteInteger( ssUser, AIniKey, AProperty, AValue );
    except
      on E: Exception do
        FLog.Event( ERR_SAVE, [ClassName, E.Message] );
    end;
end;

class function TGuiSettings.RectIsVisibleOnMonitors( const ARect: TRect ): boolean;
var
  n: integer;
  screenRect: TRect;
begin
  Result := false;
  n := 0;
  while n < Screen.MonitorCount do
  begin
    screenRect := Screen.Monitors[n].WorkareaRect;
    Result := ARect.IntersectsWith( screenRect );
    if Result then
      break;
    inc( n );
  end;
end;

procedure TGuiSettings.RestoreControlHeight( AControl: TControl; AIniKey: string );
begin
  if AIniKey = '' then
    AIniKey := ScreenKey;
  if HasFile then
    AControl.Height := FSettings.ReadInteger( ssUser, AIniKey, AControl.Name + KEY_HEIGHT, AControl.Height );
end;

procedure TGuiSettings.RestoreControlWidth( AControl: TControl; AIniKey: string );
begin
  if AIniKey = '' then
    AIniKey := ScreenKey;
  if HasFile then
    AControl.Width := FSettings.ReadInteger( ssUser, AIniKey, AControl.Name + KEY_WIDTH, AControl.Width );
end;

procedure TGuiSettings.RestoreFormState;
var
  boundsRect: TRect;
begin
  if not HasFile then
    FMainForm.boundsRect := Screen.WorkareaRect
  else
    try
      FMainForm.WindowState := TWindowState( FSettings.ReadInteger( ssUser, FormKey, PROP_STATE, ord( TWindowState.wsNormal ) ) );
      if FMainForm.WindowState <> TWindowState.wsNormal then
        exit;
      boundsRect.Left := FSettings.ReadInteger( ssUser, FormKey, PROP_LEFT, 0 );
      boundsRect.Top := FSettings.ReadInteger( ssUser, FormKey, PROP_TOP, 0 );
{$IFNDEF FMX}
      { TScreen not available in FireMonkey }
      boundsRect.Width := FSettings.ReadInteger( ssUser, FormKey, PROP_WIDTH, FMainForm.Width );
      boundsRect.Height := FSettings.ReadInteger( ssUser, FormKey, PROP_HEIGHT, FMainForm.Height );
{$ENDIF}
      if not RectIsVisibleOnMonitors( boundsRect ) then
        boundsRect := Screen.WorkareaRect;
      FMainForm.boundsRect := boundsRect
    except
      on E: Exception do
        FLog.SilentError( Format( ERR_LOAD, [ClassName, E.Message] ) );
    end;
end;

function TGuiSettings.TryGetColor( out AColor: TColor ): boolean;
begin
  Result := FSettings.Exists( ssUser, ScreenKey, 'Color' );
  if Result then
  begin
    AColor := FSettings.ReadInteger( ssUser, ScreenKey, 'Color', FMainForm.Color );
    Result := AColor <> 0;
  end;
end;

function TGuiSettings.TryGetFont( out AFontName: string; out AFontSize: integer ): boolean;
begin
  AFontName := FSettings.ReadString( ssUser, ScreenKey, 'FontName', '' );
  AFontSize := FSettings.ReadInteger( ssUser, ScreenKey, 'FontSize', 0 );
  Result := ( AFontName <> '' ) and ( AFontSize > 7 );
end;

function TGuiSettings.TryGetProperyValue( AProperty: string; out AValue: integer; AIniKey: string ): boolean;
begin
  if AIniKey = '' then
    AIniKey := ScreenKey;

  Result := FSettings.Exists( ssUser, AIniKey, AProperty );
  if Result then
  begin
    AValue := FSettings.ReadInteger( ssUser, AIniKey, AProperty, 0 );
    Result := AValue <> 0;
  end;
end;

procedure TGuiSettings.SaveColor( const AColor: TColor );
begin
  FSettings.WriteInteger( ssUser, ScreenKey, 'Color', AColor );
end;

procedure TGuiSettings.SaveControlHeight( AControl: TControl; AIniKey: string );
begin
  if AIniKey = '' then
    AIniKey := ScreenKey;
  if HasFile then
    try
      FSettings.WriteInteger( ssUser, AIniKey, AControl.Name + KEY_HEIGHT, AControl.Height );
    except
      on E: Exception do
        FLog.Event( ERR_SAVE, [ClassName, E.Message] );
    end;
end;

procedure TGuiSettings.SaveControlWidth( AControl: TControl; AIniKey: string );
begin
  if AIniKey = '' then
    AIniKey := ScreenKey;
  if HasFile then
    try
      FSettings.WriteInteger( ssUser, AIniKey, AControl.Name + KEY_WIDTH, AControl.Width );
    except
      on E: Exception do
        FLog.Event( ERR_SAVE, [ClassName, E.Message] );
    end;
end;

procedure TGuiSettings.SaveFont( const AFontName: string; const AFontSize: integer );
begin
  FSettings.WriteString( ssUser, ScreenKey, 'FontName', AFontName );
  FSettings.WriteInteger( ssUser, ScreenKey, 'FontSize', AFontSize );
end;

procedure TGuiSettings.SaveFormState;
begin
  if HasFile then
    try
      FSettings.WriteInteger( ssUser, FormKey, PROP_STATE, ord( FMainForm.WindowState ) );
      if FMainForm.WindowState = TWindowState.wsNormal then
      begin
        FSettings.WriteInteger( ssUser, FormKey, PROP_LEFT, FMainForm.Left );
        FSettings.WriteInteger( ssUser, FormKey, PROP_TOP, FMainForm.Top );
        FSettings.WriteInteger( ssUser, FormKey, PROP_WIDTH, FMainForm.Width );
        FSettings.WriteInteger( ssUser, FormKey, PROP_HEIGHT, FMainForm.Height );
      end;
    except
      on E: Exception do
        FLog.SilentWarning( Format( ERR_SAVE, [ClassName, E.Message] ) );
    end;
end;

end.
