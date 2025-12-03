unit EPR.PeriodDictionary;

interface

uses
  Emetra.VclForm.Period,
  Emetra.VclUtil.Style.Interfaces,
  { General }
  Emetra.Dictionary.Interfaces,
  Emetra.Settings.Interfaces,
  Emetra.Classes.Business,
  Emetra.Classes.Transient,
  Emetra.Logging.Interfaces,
  { Standard }
  System.Classes;

type
  TPeriodDictionary = class( TTransient, IPeriodDictionary )
  strict private
    fSettings: IScopedSettingsReadWrite;
    fGuiStyle: IGuiStyle;
    fForm: TfrmPeriod;
  private
    function TryGetPeriod( const AContext, ACaption: string; out APeriodStart, APeriodEnd: TDateTime ): boolean;
  public
    constructor Create( AGuiStyle: IGuiStyle; ASettings: IScopedSettingsReadWrite; ALog: ILog ); reintroduce; overload;
    constructor Create( ASettings: IScopedSettingsReadWrite; ALog: ILog ); overload;
  end;

implementation

uses
  System.SysUtils, Vcl.Forms;

const
  KEY_PERIOD_START = 'PeriodStart';
  KEY_PERIOD_END   = 'PeriodEnd';

{$REGION 'TPeriodDictionary'}

constructor TPeriodDictionary.Create( AGuiStyle: IGuiStyle; ASettings: IScopedSettingsReadWrite; ALog: ILog );
begin
  inherited Create( ALog );
  fSettings := ASettings;
  fGuiStyle := AGuiStyle;
end;

constructor TPeriodDictionary.Create( ASettings: IScopedSettingsReadWrite; ALog: ILog );
begin
  inherited Create( ALog );
  fSettings := ASettings;
end;

function TPeriodDictionary.TryGetPeriod( const AContext, ACaption: string; out APeriodStart, APeriodEnd: TDateTime ): boolean;
var
  obs: IGuiStyleObserver;
begin
  Result := false;
  if fForm = nil then
    fForm := TfrmPeriod.Create( Application );
  if Assigned( fGuiStyle ) and Supports( fForm, IGuiStyleObserver, obs ) then
    obs.UpdateStyle( fGuiStyle );
  if Assigned( fSettings ) then
  begin
    APeriodStart := fSettings.ReadDate( ssUser, KEY_PERIOD_START, AContext, Now - 1 );
    APeriodEnd := fSettings.ReadDate( ssUser, KEY_PERIOD_END, AContext, Now );
  end;
  fForm.lblSubheader.Caption := ACaption;
  fForm.CalendarView1.Date := APeriodStart;
  fForm.CalendarView2.Date := APeriodEnd;
  if fForm.TryGetPeriod( APeriodStart, APeriodEnd ) then
  begin
    if Assigned( fSettings ) then
    begin
      fSettings.WriteDateTime( ssUser, KEY_PERIOD_START, AContext, APeriodStart );
      fSettings.WriteDateTime( ssUser, KEY_PERIOD_END, AContext, APeriodEnd );
    end;
    Result := true;
  end;
end;
{$ENDREGION}

end.
