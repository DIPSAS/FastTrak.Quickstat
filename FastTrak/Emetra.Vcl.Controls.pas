unit Emetra.Vcl.Controls;

{$DEFINE DEBUG_INDICATORS}
{$R 'dcControls.res'}
{$R 'dcGlyphs.res'}

interface

uses
  {Standard}
  System.Classes, System.Types, Winapi.Messages, Winapi.Windows,
  Vcl.Graphics, Vcl.ExtCtrls,
  {Controls}
  Vcl.Controls, Vcl.StdCtrls, Vcl.Forms,
  System.UITypes,
  {Emetra.Vcl}
  Emetra.Vcl.Consts,
  Emetra.Vcl.UITypes,
  Emetra.Vcl.Intf;

type
  TdcIndexNotifyEvent = procedure( Sender: TObject; Index: integer ) of object;

  { TdcControl }

  TdcControl = class( TCustomControl )
  strict private
    { Private Fields }
    fAlpha: Byte;
    fBorderColor: TColor;
    fBorderSize: Integer;
    fHasSize: boolean;
    fHintLocation: TPoint;
    fHintPauseTimer: TTimer;
    fHintText: WideString;
    fHintWindow: THintWindow;
    fOldSize: TSize;
    fOnPaint: TNotifyEvent;
    fTagString: string;
    { Property Acessors }
    procedure Set_Alpha( const Value: Byte );
    procedure Set_BorderColor( const Value: TColor );
    procedure Set_BorderSize( const Value: Integer );
  protected
    procedure ActivateHint( Location: TPoint; Text: WideString );
    function CanFocusParentForm: boolean;
    procedure DeactivateHint;
    { Invalidation Methods }
    procedure EraseBkGnd( const Source: TRect; AColor: TColor = clNone );
    procedure ValidateRect( const Source: TRect );
    procedure InvalidatePadding( const Source: TRect; const Padding: TPadding );
    procedure InvalidateRect( const Source: TRect );
    procedure InvalidateNC;
    procedure Paint; override;
    { Event Handlers }
    procedure DoHintPauseTimer( Sender: TObject );
    procedure DoPaint; dynamic;
    procedure DoSizeChanged( const ADeltaX, ADeltaY: Integer ); virtual;
    { Overrided Methods }
    procedure CreateParams( var Params: TCreateParams ); override;
    procedure PaintWindow( DC: HDC ); override;
    { Delphi Messages }
    procedure CMMouseLeave( var Message: TMessage ); message CM_MOUSELEAVE;
    { Winapi Messages }
    procedure WMNCCalcSize( var Message: TWMNCCalcSize ); message WM_NCCALCSIZE;
    procedure WMNCPaint( var Message: TMessage ); message WM_NCPAINT;
    procedure WMSize( var Message: TWMSize ); message WM_SIZE;
    procedure WMWindowPosChanged( var Message: TWMWindowPosChanged ); message WM_WINDOWPOSCHANGED;
    procedure WMWindowPosChanging( var Message: TWMWindowPosChanging ); message WM_WINDOWPOSCHANGING;
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
    { Methods }
    procedure FocusFirstChild( const AParent: TWinControl );
  published
    property Action;
    property Align;
    property Alpha: Byte read fAlpha write Set_Alpha default 255;
    property Anchors;
    property BorderColor: TColor read fBorderColor write Set_BorderColor default clVeryDarkBorderColor;
    property BorderSize: Integer read fBorderSize write Set_BorderSize default 0;
    property Color;
    property Constraints;
    property DoubleBuffered;
    property DragCursor;
    property DragKind;
    property DragMode;
    property Enabled;
    property Font;
    property Hint;
    property Margins;
    property Padding;
    property ParentColor;
    property ParentDoubleBuffered;
    property ParentFont;
    property ParentShowHint;
    property PopupMenu;
    property ShowHint;
    property TabOrder;
    property TabStop;
    property Tag;
    property TagString: string read fTagString write fTagString;
    property Touch;
    property Visible;

    property OnClick;
    property OnContextPopup;
    property OnDblClick;
    property OnDragDrop;
    property OnDragOver;
    property OnEndDock;
    property OnEndDrag;
    property OnEnter;
    property OnExit;
    property OnGesture;
    property OnKeyDown;
    property OnKeyPress;
    property OnKeyUp;
    property OnMouseDown;
    property OnMouseEnter;
    property OnMouseLeave;
    property OnMouseMove;
    property OnMouseUp;
    property OnPaint: TNotifyEvent read fOnPaint write fOnPaint;
    property OnStartDock;
    property OnStartDrag;
  end;

  { INxScrollingControl }

  TNxScrollBar = class;

  INxScrollBar = interface
    ['{FFA366CC-2DEB-4454-B19F-5E24B559491A}']
    { Property Accessors }
    function GetPosition: Integer;
    function GetShowing: boolean;
    procedure SetPosition( const Value: Integer );
    { Methods }
    procedure Clear( Update: boolean = False );
    procedure First;
    procedure Hide;
    function IsFirst: boolean;
    function IsLast: boolean;
    procedure Last;
    procedure Lock;
    procedure MoveBy( Distance: Integer );
    procedure Next;
    procedure PageDown;
    procedure PageUp;
    procedure Prior;
    procedure Scroll( ScrollType: TNxScrollType );
    procedure SetValues( Max, PageSize: Integer );
    procedure Show;
    procedure Unlock( Update: boolean = True );
    procedure Update;
    { Properties }
    property Position: Integer read GetPosition write SetPosition;
  end;

  { TdcScrollControl }

  TdcScrollControl = class( TdcControl )
  private
    fHorzScrollBar: TNxScrollBar;
    fOnContentScroll: TNotifyEvent;
    fOnHorizontalScroll: TNotifyEvent;
    fOnVerticalScroll: TNotifyEvent;
    fScrollBars: TNxScrollBars;
    fVertScrollBar: TNxScrollBar;
    procedure SetScrollBars( const Value: TNxScrollBars );
  protected
    function GetHandle: HWND;
    function GetHorzScrollBar: TNxScrollBar;
    function GetScrollBars: TScrollStyle; virtual;
    function GetScrollType( ScrollCode: Integer ): TNxScrollType;
    function GetVertScrollBar: TNxScrollBar;
    function IsDestroying: boolean;
    function IsReading: boolean;
    { Event Handlers }
    procedure DoHorizontalScroll; dynamic;
    procedure DoVerticalScroll; dynamic;
    { VCL Core }
    procedure CreateParams( var Params: TCreateParams ); override;
    procedure CreateWnd; override;
    { Mouse Methods }
    function DoMouseWheelDown( Shift: TShiftState; MousePos: TPoint ): boolean; override;
    function DoMouseWheelUp( Shift: TShiftState; MousePos: TPoint ): boolean; override;
    { Methods }
    procedure ScrollContentBy( DeltaX, DeltaY: Integer ); virtual;
    procedure ScrollRect( DeltaX, DeltaY: Integer; Rect, ClipRect: TRect ); virtual;
    procedure SelectNextControl;
    procedure SelectPrevControl;
    { Win32 Messages }
    procedure WMHScroll( var Message: TWMHScroll ); message WM_HSCROLL;
    procedure WMVScroll( var Message: TWMVScroll ); message WM_VSCROLL;
    { Properties }
    property ScrollBars: TNxScrollBars read fScrollBars write SetScrollBars;
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
    { Properties }
    property HorzScrollBar: TNxScrollBar read GetHorzScrollBar;
    property VertScrollBar: TNxScrollBar read GetVertScrollBar;
  published
    property OnContentScroll: TNotifyEvent read fOnContentScroll write fOnContentScroll;
    property OnHorizontalScroll: TNotifyEvent read fOnHorizontalScroll write fOnHorizontalScroll;
    property OnVerticalScroll: TNotifyEvent read fOnVerticalScroll write fOnVerticalScroll;
  end;

  { TdcPersistentScrollBar }

  TdcPersistentScrollBar = class( TInterfacedPersistent )
  private
    FLargeChange: Integer;
    FMax: Integer;
    FOnChange: TNotifyEvent;
    FPageSize: Integer;
    FPosition: Integer;
    FSmallChange: Integer;
    FVisible: boolean;
    function GetLargeChange: Integer;
    function GetMax: Integer;
    function GetPageSize: Integer;
    function GetPosition: Integer;
    function GetSmallChange: Integer;
    function GetVisible: boolean;
    procedure SetLargeChange( const Value: Integer );
    procedure SetMax( const Value: Integer );
    procedure SetPageSize( const Value: Integer );
    procedure SetPosition( const Value: Integer );
    procedure SetSmallChange( const Value: Integer );
    procedure SetVisible( const Value: boolean );
  protected
    procedure DoChange; dynamic;
  public
    constructor Create; virtual;
    procedure PageUp;
    procedure PageDown;
    procedure Prior;
    procedure Next;
  published
    property LargeChange: Integer read GetLargeChange write SetLargeChange;
    property Max: Integer read GetMax write SetMax;
    property Position: Integer read GetPosition write SetPosition;
    property PageSize: Integer read GetPageSize write SetPageSize;
    property SmallChange: Integer read GetSmallChange write SetSmallChange;
    property Visible: boolean read GetVisible write SetVisible;
    property OnChange: TNotifyEvent read FOnChange write FOnChange;
  end;

  TNxCustomScrollBar = class( TInterfacedPersistent, INxScrollBar )
  private
    fAutoHide: boolean;
    fControl: TdcScrollControl;
    fManualScroll: boolean;
    fEnabled: boolean;
    fKind: TScrollBarKind;
    FLargeChange: Integer;
    fLocked: boolean;
    FMax: Integer;
    fMin: Integer;
    fOldPosition: Integer;
    FPageSize: Integer;
    FPosition: Integer;
    FSmallChange: Integer;
    fSnapshotPosition: Integer;
    fScrollKind: TScrollBarKind;
    fUpdating: boolean;
    FVisible: boolean;
    { Property Accessors }
    function GetInfoFlag: Cardinal;
    function GetScrollInfo: TScrollInfo;
    function GetShowing: boolean;
    function GetSnapshotPosition: Integer;
    function GetThumbPosition: Integer;
    procedure SetAutoHide( const Value: boolean );
    procedure SetEnabled( const Value: boolean );
    procedure SetKind( const Value: TScrollBarKind );
    procedure SetManualScroll( const Value: boolean );
    procedure SetMax( const Value: Integer );
    procedure SetMin( const Value: Integer );
    procedure SetPageSize( const Value: Integer );
    procedure SetPosition( const Value: Integer );
  protected
    procedure CheckValues;
    function GetControlScrollBars: TScrollStyle;
    function GetFlag: Integer; virtual;
    procedure SetVisible( const Value: boolean ); virtual;
    function ShouldBeVisible: boolean;
    { Virtual Methods }
    function GetPosition: Integer; virtual;
    procedure UpdateScrollBar; virtual;
    { Properties }
    property Flag: Integer read GetFlag;
    property InfoFlag: Cardinal read GetInfoFlag;
    property ScrollInfo: TScrollInfo read GetScrollInfo;
  public
    constructor Create( AControl: TdcScrollControl; AKind: TScrollBarKind ); virtual;
    destructor Destroy; override;
    function IsFirst: boolean;
    function IsLast: boolean;
    procedure Assign( Source: TPersistent ); override;
    procedure Clear( Update: boolean = False );
    procedure EraseSnapshot;
    function IsUpdating: boolean;
    procedure First;
    procedure Hide;
    procedure Last;
    procedure Lock;
    procedure MoveBy( Distance: Integer );
    procedure Next;
    procedure Prior;
    procedure PageDown; virtual;
    procedure PageUp; virtual;
    procedure SetValues( AMax, APageSize: Integer ); virtual;
    procedure Scroll( ScrollType: TNxScrollType ); virtual;
    procedure Show;
    procedure Snapshot;
    procedure Unlock( Update: boolean = True );
    procedure Update; virtual;
    { Properties }
    property AutoHide: boolean read fAutoHide write SetAutoHide;
    property Enabled: boolean read fEnabled write SetEnabled;
    property Kind: TScrollBarKind read fKind write SetKind;
    property LargeChange: Integer read FLargeChange write FLargeChange;
    property ManualScroll: boolean read fManualScroll write SetManualScroll;
    property Max: Integer read FMax write SetMax;
    property Min: Integer read fMin write SetMin;
    property OldPosition: Integer read fOldPosition;
    property PageSize: Integer read FPageSize write SetPageSize;
    property Position: Integer read GetPosition write SetPosition;
    property ScrollKind: TScrollBarKind read fScrollKind;
    property Showing: boolean read GetShowing;
    property SmallChange: Integer read FSmallChange write FSmallChange;
    property SnapshotPosition: Integer read GetSnapshotPosition;
    property ThumbPosition: Integer read GetThumbPosition;
    property Visible: boolean read FVisible write SetVisible;
  end;

  TNxScrollBar = class( TNxCustomScrollBar );

  { TNxTextFitHintWindow }

  TNxTextFitHintWindow = class( THintWindow )
  protected
    procedure CreateParams( var Params: TCreateParams ); override;
  public
    constructor Create( AOwner: TComponent ); override;
  end;

  { TControlHelper }

  TControlHelper = class helper for TControl
    procedure SetAutoSize;
  end;

{$IFNDEF DROP_SHADOW}

const
{$EXTERNALSYM CS_DROPSHADOW}
  CS_DROPSHADOW = $20000;

function CheckWin32Version( AMajor: Integer; AMinor: Integer = 0 ): boolean;
{$ENDIF}
function IsRelated( AChild, AParent: TControl ): boolean;
function MeasureFontHeight( Font: TFont ): Integer;

procedure RemoveKeyMessage( Handle: HWND );
procedure RemovePaintMessage( Handle: HWND );

function GetTaskBarAlign: TAlign;
function GetTaskBarSize: TSize;

implementation

uses
  {Standard}
  Vcl.Dialogs, SysUtils, ShellApi, Math;

function IsRelated( AChild, AParent: TControl ): boolean;
var
  Current: TControl;
begin
  Current := AChild;
  repeat
    Current := Current.Parent;
    if Current = AParent then
      Exit( True );
  until Current = nil;
  Result := False;
end;

/// <summary>
/// Measure intrinsic height of the font.
/// </summary>
function MeasureFontHeight( Font: TFont ): Integer;
var
  DC: HDC;
  SaveFont: HFont;
  SysMetrics, Metrics: TTextMetric;
begin
  DC := GetDC( 0 );
  try
    GetTextMetrics( DC, SysMetrics );
    SaveFont := SelectObject( DC, Font.Handle );
    GetTextMetrics( DC, Metrics );
    SelectObject( DC, SaveFont );
  finally
    ReleaseDC( 0, DC );
  end;
  Result := Metrics.tmHeight;
end;

function GetTaskBarBounds: TRect;
begin
  GetWindowRect( FindWindow( 'Shell_TrayWnd', '' ), Result );
end;

function GetTaskBarSize: TSize;
var
  TaskBarBounds: TRect;
begin
  TaskBarBounds := GetTaskBarBounds;
  with ( TaskBarBounds ) do
  begin
    Result.cx := Right - Abs( Left );
    Result.cy := Bottom - Abs( Top );
  end;
end;

function GetTaskBarAlign: TAlign;
var
  TaskBarBounds: TRect;
begin
  Result := alNone;

  if ( FindWindow( 'Shell_TrayWnd', '' ) > 0 ) then
  begin
    TaskBarBounds := GetTaskBarBounds;

    with ( TaskBarBounds ) do
      // At Left or at top of screen ?
      if ( Left <= 0 ) and ( Top <= 0 ) then
      begin
        if ( Bottom >= 480 ) then
          Result := alLeft
        else
          Result := alTop;
      end
      else
      begin
        if ( Left <= 0 ) then
          Result := alBottom
        else
          Result := alRight;
      end;
  end;
end;

{$IFNDEF DROP_SHADOW}

function CheckWin32Version( AMajor: Integer; AMinor: Integer = 0 ): boolean;
begin
  Result := ( Win32MajorVersion > AMajor ) or ( ( Win32MajorVersion = AMajor ) and ( Win32MinorVersion >= AMinor ) );
end;
{$ENDIF}

procedure RemoveKeyMessage( Handle: HWND );
var
  Msg: TMsg;
begin
  Msg.Message := 0;
  if PeekMessage( Msg, Handle, WM_KEYFIRST, WM_KEYLAST, PM_REMOVE ) and ( Msg.Message = WM_QUIT ) then
    PostQuitMessage( Msg.wParam );
end;

procedure RemovePaintMessage( Handle: HWND );
var
  Msg: TMsg;
begin
  Msg.Message := 0;
  PeekMessage( Msg, Handle, WM_PAINT, WM_PAINT, PM_REMOVE )
end;

{ TNxControl6 }

procedure TdcControl.ActivateHint( Location: TPoint; Text: WideString );
begin
  if Text = '' then
    Exit;

  fHintLocation := Location;
  fHintText := Text;

  { Should pause? }
  if not Assigned( fHintWindow ) then
  begin
    fHintPauseTimer := TTimer.Create( Self );
    fHintPauseTimer.Interval := Application.HintPause;
    fHintPauseTimer.OnTimer := DoHintPauseTimer;
  end
  else
    DoHintPauseTimer( Self ); { Call now }
end;

function TdcControl.CanFocusParentForm: boolean;
var
  parentForm: TWinControl;
begin
  parentForm := GetParentForm( Self );
  Result := Assigned( parentForm ) and parentForm.Showing;
end;

procedure TdcControl.CMMouseLeave( var Message: TMessage );
begin
  inherited;
  DeactivateHint;
end;

constructor TdcControl.Create( AOwner: TComponent );
begin
  inherited;
  fAlpha := 255;
  fBorderColor := clVeryDarkBorderColor;
  fBorderSize := 0;
  fHasSize := False;
  fTagString := EmptyStr;
end;

procedure TdcControl.CreateParams( var Params: TCreateParams );
begin
  inherited;
  with Params do
  begin
    if Alpha < 255 then
      ExStyle := ExStyle or WS_EX_TRANSPARENT;
  end;
end;

procedure TdcControl.DeactivateHint;
begin
  { Stop & Destroy }
  FreeAndNil( fHintPauseTimer );

  if Assigned( fHintWindow ) then
  begin
    fHintWindow.ReleaseHandle;
    { Destroy }
    if Assigned( fHintWindow ) then
      FreeAndNil( fHintWindow );
  end;
end;

destructor TdcControl.Destroy;
begin
  { Destroy Obj. }
  DeactivateHint;
  inherited;
end;

procedure TdcControl.DoHintPauseTimer( Sender: TObject );
var
  HintRect: TRect;
begin
  { Release previous? }
  DeactivateHint;

  { Create Hint Window }
  fHintWindow := HintWindowClass.Create( nil );
  fHintWindow.Color := clInfoBk;

  { Set Position & Activate }

  { Calculate Rect }
  HintRect := fHintWindow.CalcHintRect( Screen.Width, fHintText, nil );

  { Cordinates must be "Screen" }
  HintRect.TopLeft := ClientToScreen( fHintLocation );

  HintRect.Bottom := HintRect.Top + HintRect.Bottom;
  HintRect.Right := HintRect.Left + HintRect.Right;

  { Show Hint }
  fHintWindow.ActivateHint( HintRect, fHintText );
end;

procedure TdcControl.DoPaint;
begin
  if Assigned( fOnPaint ) then
    fOnPaint( Self );
end;

procedure TdcControl.DoSizeChanged( const ADeltaX, ADeltaY: Integer );
begin

end;

procedure TdcControl.EraseBkGnd( const Source: TRect; AColor: TColor );
begin
  with Canvas do
  begin
    if AColor <> clNone then
      Brush.Color := AColor
    else
      Brush.Color := Color;
    FillRect( Source );
  end;
end;

procedure TdcControl.FocusFirstChild( const AParent: TWinControl );
var
  i: Integer;
begin
  for i := 0 to AParent.ControlCount - 1 do
  begin
    if AParent.Controls[i] is TWinControl then
      with AParent.Controls[i] as TWinControl do
      begin
        if CanFocus then
        begin
          SetFocus;
          Exit;
        end;
      end;
  end;
end;

procedure TdcControl.InvalidateNC;
var
  R: TRect;
begin
  R := ClientRect;
  RedrawWindow( Handle, @R, 0, RDW_FRAME or RDW_INVALIDATE );
end;

procedure TdcControl.InvalidatePadding( const Source: TRect; const Padding: TPadding );
begin
  with Padding do
  begin
    InvalidateRect( Rect( Source.Left + Left, Source.Top, Source.Right - Right, Source.Top + Top ) );
    InvalidateRect( Rect( Source.Right - Right, Source.Top, Source.Right, Source.Bottom ) );
    InvalidateRect( Rect( Source.Left + Left, Source.Bottom - Bottom, Source.Right - Right, Source.Bottom ) );
    InvalidateRect( Rect( Source.Left, Source.Top, Source.Left + Left, Source.Bottom ) );
  end;
end;

procedure TdcControl.InvalidateRect( const Source: TRect );
begin
  Winapi.Windows.InvalidateRect( Handle, Source, False );
end;

procedure TdcControl.Paint;
begin
  inherited;
  if Alpha < 255 then
    EraseBkGnd( ClientRect );
end;

procedure TdcControl.PaintWindow( DC: HDC );
begin
  inherited;
  DoPaint;
end;

procedure TdcControl.Set_BorderColor( const Value: TColor );
begin
  if Value <> fBorderColor then
  begin
    fBorderColor := Value;
    InvalidateNC;
  end;
end;

procedure TdcControl.Set_BorderSize( const Value: Integer );
begin
  if Value <> fBorderSize then
  begin
    fBorderSize := Value;
    Perform( CM_BORDERCHANGED, 0, 0 );
  end;
end;

procedure TdcControl.ValidateRect( const Source: TRect );
begin
  Winapi.Windows.ValidateRect( Handle, Source );
end;

procedure TdcControl.Set_Alpha( const Value: Byte );
var
  Update: boolean;
begin
  if Value <> fAlpha then
  begin
    Update := fAlpha = 255;
    fAlpha := Value;
    if Update then
      RecreateWnd;
    Invalidate;
  end;
end;

procedure TdcControl.WMNCCalcSize( var Message: TWMNCCalcSize );
var
  Params: PNCCalcSizeParams;
begin
  inherited;
  Params := message.CalcSize_Params;
  with Params^ do
  begin
    InflateRect( rgrc[0], -Integer( fBorderSize ), -Integer( fBorderSize ) );
  end;
end;

procedure TdcControl.WMNCPaint( var Message: TMessage );
var
  Device: HDC;
  Pen: HPEN;
  i: Integer;
  R: TRect;
  P: array [0 .. 2] of TPoint;
  HightlightColor, ShadowColor: TColor;
begin
  { Required for ScrollBars }
  inherited;

  { Can draw? }
  if BorderSize > 0 then
  begin
    HightlightColor := fBorderColor;
    ShadowColor := fBorderColor;

    Device := GetWindowDC( Handle );
    try
      GetWindowRect( Handle, R );
      OffsetRect( R, -R.Left, -R.Top );

      Pen := CreatePen( PS_SOLID, 1, ColorToRGB( ShadowColor ) );
      try
        SelectObject( Device, Pen );

        P[0] := Point( R.Left, R.Bottom - 1 );
        P[1] := R.TopLeft;
        P[2] := Point( R.Right, R.Top );

        Polyline( Device, P, 3 );

        for i := 2 to BorderSize do
        begin
          Inc( P[0].X );
          Dec( P[0].Y );
          Inc( P[1].X );
          Inc( P[1].Y );
          Dec( P[2].X );
          Inc( P[2].Y );

          Polyline( Device, P, 3 );
        end;

      finally
        DeleteObject( Pen );
      end;

      Pen := CreatePen( PS_SOLID, 1, ColorToRGB( HightlightColor ) );
      try
        SelectObject( Device, Pen );

        P[0] := Point( R.Left + 1, R.Bottom - 1 );
        P[1] := Point( R.Right - 1, R.Bottom - 1 );
        P[2] := Point( R.Right - 1, R.Top );

        Polyline( Device, P, 3 );

        for i := 2 to BorderSize do
        begin
          Inc( P[0].X );
          Dec( P[0].Y );
          Dec( P[1].X );
          Dec( P[1].Y );
          Dec( P[2].X );
          Inc( P[2].Y );

          Polyline( Device, P, 3 );
        end;

      finally
        DeleteObject( Pen );
      end;

    finally
      ReleaseDC( Handle, Device );
    end;

  end;

end;

procedure TdcControl.WMSize( var Message: TWMSize );
begin
  inherited;
  if fHasSize then
  begin
    DoSizeChanged( Width - fOldSize.cx, Height - fOldSize.cy );
  end;
  fHasSize := True;
  fOldSize := TSize.Create( Width, Height );
end;

procedure TdcControl.WMWindowPosChanged( var Message: TWMWindowPosChanged );
begin
  inherited;
  if Alpha < 255 then
    Invalidate;
end;

procedure TdcControl.WMWindowPosChanging( var Message: TWMWindowPosChanging );
begin
  inherited;
  if Alpha < 255 then
    Invalidate;
end;

{ TdcPersistentScrollBar }

constructor TdcPersistentScrollBar.Create;
begin
  FLargeChange := 50;
  FMax := 100;
  FPosition := 0;
  FSmallChange := 1;
  FVisible := True;
end;

procedure TdcPersistentScrollBar.DoChange;
begin
  if Assigned( FOnChange ) then
    FOnChange( Self );
end;

function TdcPersistentScrollBar.GetLargeChange: Integer;
begin
  Result := FLargeChange;
end;

function TdcPersistentScrollBar.GetMax: Integer;
begin
  Result := FMax;
end;

function TdcPersistentScrollBar.GetPageSize: Integer;
begin
  Result := FPageSize;
end;

function TdcPersistentScrollBar.GetPosition: Integer;
begin
  Result := FPosition;
end;

function TdcPersistentScrollBar.GetSmallChange: Integer;
begin
  Result := FSmallChange;
end;

function TdcPersistentScrollBar.GetVisible: boolean;
begin
  Result := FVisible;
end;

procedure TdcPersistentScrollBar.Next;
begin
  Position := Position + SmallChange;
end;

procedure TdcPersistentScrollBar.PageDown;
begin
  Position := Position + LargeChange;
end;

procedure TdcPersistentScrollBar.PageUp;
begin
  Position := Position - LargeChange;
end;

procedure TdcPersistentScrollBar.Prior;
begin
  Position := Position - SmallChange;
end;

procedure TdcPersistentScrollBar.SetLargeChange( const Value: Integer );
begin
  FLargeChange := Value;
end;

procedure TdcPersistentScrollBar.SetMax( const Value: Integer );
begin
  FMax := Value;
end;

procedure TdcPersistentScrollBar.SetPageSize( const Value: Integer );
begin
  FPageSize := Value;
end;

procedure TdcPersistentScrollBar.SetPosition( const Value: Integer );
var
  fOldPosition: Integer;
begin
  fOldPosition := FPosition;
  FPosition := Value;
  if FPosition < 0 then
    FPosition := 0;
  if FPosition + FPageSize > FMax then
    FPosition := ( FMax - FPageSize ) + 1;
  if FPosition <> fOldPosition then
    DoChange;
end;

procedure TdcPersistentScrollBar.SetSmallChange( const Value: Integer );
begin
  FSmallChange := Value;
end;

procedure TdcPersistentScrollBar.SetVisible( const Value: boolean );
begin
  FVisible := Value;
end;

{ TdcScrollControl }

constructor TdcScrollControl.Create( AOwner: TComponent );
begin
  inherited;
  fHorzScrollBar := TNxScrollBar.Create( Self, sbHorizontal );
  fScrollBars := [sbHorizontal, sbVertical];
  fVertScrollBar := TNxScrollBar.Create( Self, sbVertical );

  { Note: BorderWidth cause bug
    in ScrollBar re-paint, so
    it need to be 0! }
  // BorderWidth := 0;
end;

procedure TdcScrollControl.CreateParams( var Params: TCreateParams );
begin
  inherited;
  with Params do
    Style := Style or WS_HSCROLL or WS_VSCROLL or WS_CLIPCHILDREN;
end;

procedure TdcScrollControl.CreateWnd;
begin
  inherited;

  // SetorderStyle( FBorderStyle );

  { This is the place where scroll-bars
    need to be initialized }
  SetScrollBars( fScrollBars );
end;

destructor TdcScrollControl.Destroy;
begin
  { Destroy Obj. }
  fHorzScrollBar.Free;
  fVertScrollBar.Free;

  inherited Destroy;
end;

procedure TdcScrollControl.DoHorizontalScroll;
begin
  if Assigned( fOnHorizontalScroll ) then
    fOnHorizontalScroll( Self );
end;

function TdcScrollControl.DoMouseWheelDown( Shift: TShiftState; MousePos: TPoint ): boolean;
begin
  Result := True;
end;

function TdcScrollControl.DoMouseWheelUp( Shift: TShiftState; MousePos: TPoint ): boolean;
begin
  Result := True;
end;

procedure TdcScrollControl.DoVerticalScroll;
begin
  if Assigned( fOnVerticalScroll ) then
    fOnVerticalScroll( Self );
end;

function TdcScrollControl.GetHandle: HWND;
begin
  Result := inherited Handle;
end;

function TdcScrollControl.GetHorzScrollBar: TNxScrollBar;
begin
  Result := fHorzScrollBar;
end;

function TdcScrollControl.GetScrollBars: TScrollStyle;
begin
  Result := ssNone;
end;

function TdcScrollControl.GetScrollType( ScrollCode: Integer ): TNxScrollType;
begin
  case ScrollCode of
    SB_LINEDOWN: Result := stSmallIncrement;
    SB_LINEUP: Result := stSmallDecrement;
    SB_PAGEDOWN: Result := stLargeIncrement;
    SB_PAGEUP: Result := stLargeDecrement;
    SB_THUMBPOSITION: Result := stThumbPosition;
    SB_THUMBTRACK: Result := stThumbTrack;
    SB_TOP: Result := stFirst;
    SB_BOTTOM: Result := stLast;
  else Result := stEndScroll;
  end;
end;

function TdcScrollControl.GetVertScrollBar: TNxScrollBar;
begin
  Result := fVertScrollBar;
end;

function TdcScrollControl.IsDestroying: boolean;
begin
  Result := csDestroying in ComponentState;
end;

function TdcScrollControl.IsReading: boolean;
begin
  Result := csReading in ComponentState;
end;

procedure TdcScrollControl.ScrollContentBy( DeltaX, DeltaY: Integer );
begin
  if Assigned( fOnContentScroll ) then
    fOnContentScroll( Self );
end;

procedure TdcScrollControl.ScrollRect( DeltaX, DeltaY: Integer; Rect, ClipRect: TRect );
begin

end;

procedure TdcScrollControl.SelectNextControl;
begin
  if ( Parent is TWinControl ) then
    PostMessage( ( Parent as TWinControl ).Handle, WM_KEYDOWN, VK_TAB, 0 );
end;

procedure TdcScrollControl.SelectPrevControl;
begin
  if ( Parent is TWinControl ) then
  begin
    PostMessage( ( Parent as TWinControl ).Handle, WM_KEYDOWN, VK_SHIFT, 0 );
    PostMessage( ( Parent as TWinControl ).Handle, WM_KEYDOWN, VK_TAB, 0 );
  end;
end;

procedure TdcScrollControl.SetScrollBars( const Value: TNxScrollBars );
begin
  fScrollBars := Value;
  if HandleAllocated then
  begin
    fHorzScrollBar.Visible := sbHorizontal in fScrollBars;
    fVertScrollBar.Visible := sbVertical in fScrollBars;
    Invalidate;
  end;
end;

procedure TdcScrollControl.WMHScroll( var Message: TWMHScroll );
var
  ScrollType: TNxScrollType;
begin
  ScrollType := GetScrollType( message.ScrollCode );

  HorzScrollBar.Scroll( ScrollType );

  { Event }
  if ScrollType <> stEndScroll then
    DoHorizontalScroll;
end;

procedure TdcScrollControl.WMVScroll( var Message: TWMVScroll );
var
  ScrollType: TNxScrollType;
begin
  inherited;
  { Note: After first occur, Msg occur once
    more to send SB_ENDSCROLL ScrollCode }

  ScrollType := GetScrollType( message.ScrollCode );

  VertScrollBar.Scroll( ScrollType );

  { Event }
  if ScrollType <> stEndScroll then
    DoVerticalScroll;
end;

{ TNxCustomScrollBar }

procedure TNxCustomScrollBar.Assign( Source: TPersistent );
begin
  inherited;

end;

procedure TNxCustomScrollBar.CheckValues;
begin
  { Note: Max is zero-based }
  // FMax := Math.Max(0, FMax);

  { Note: Max set to 0 will hide Bar. Hide
    Bar if PageSize > Max }
  if ( FPageSize > FMax ) or ( FPageSize = 0 ) or ( FMax = 0 ) then
  begin
    FPosition := 0;
  end;
end;

procedure TNxCustomScrollBar.Clear( Update: boolean );
begin
  FMax := 0;
  FPageSize := 0;
  FPosition := 0;

  { Upon request }
  if Update then
    Self.Update;
end;

constructor TNxCustomScrollBar.Create( AControl: TdcScrollControl; AKind: TScrollBarKind );
begin
  fAutoHide := True;
  fControl := AControl;
  fEnabled := True;
  fKind := AKind;
  FLargeChange := 10;
  fLocked := False;
  FMax := 0;
  fMin := 0;
  fOldPosition := 0;
  FPosition := 0;
  FSmallChange := 1;
  fSnapshotPosition := -1;
  fUpdating := False;
  FVisible := True;
end;

destructor TNxCustomScrollBar.Destroy;
begin

  inherited;
end;

procedure TNxCustomScrollBar.EraseSnapshot;
begin
  fSnapshotPosition := -1;
end;

procedure TNxCustomScrollBar.First;
begin
  if Max > 0 then
    case Kind of
      sbHorizontal: SendMessage( fControl.Handle, WM_HSCROLL, SB_TOP, 0 );
      sbVertical: SendMessage( fControl.Handle, WM_VSCROLL, SB_TOP, 0 );
    end;
end;

function TNxCustomScrollBar.GetControlScrollBars: TScrollStyle;
var
  Flags: DWORD;
begin
  Flags := GetWindowLong( fControl.Handle, GWL_STYLE ) and ( WS_VSCROLL or WS_HSCROLL );
  case Flags of
    0: Result := ssNone;
    WS_VSCROLL: Result := ssVertical;
    WS_HSCROLL: Result := ssHorizontal;
  else Result := ssBoth;
  end;
end;

function TNxCustomScrollBar.GetFlag: Integer;
begin
  case Kind of
    sbHorizontal: Result := SB_HORZ;
  else Result := SB_VERT;
  end;
end;

function TNxCustomScrollBar.GetInfoFlag: Cardinal;
begin
  case fScrollKind of
    sbHorizontal: Result := OBJID_HSCROLL;
  else Result := OBJID_VSCROLL;
  end;
end;

function TNxCustomScrollBar.GetPosition: Integer;
begin
  if Showing and not fLocked then
  begin
    Result := ScrollInfo.nPos;
  end
  else
    Result := FPosition;
end;

function TNxCustomScrollBar.GetScrollInfo: TScrollInfo;
begin
  Result.cbSize := SizeOf( TScrollInfo );
  Result.fMask := SIF_ALL;

  { Win32 Call }
  Winapi.Windows.GetScrollInfo( fControl.Handle, Flag, Result );
end;

function TNxCustomScrollBar.GetShowing: boolean;
begin
  Result := GetControlScrollBars = ssBoth;
  case Kind of
    sbHorizontal: Result := Result or ( GetControlScrollBars = ssHorizontal );
    sbVertical: Result := Result or ( GetControlScrollBars = ssVertical );
  end;
end;

function TNxCustomScrollBar.GetSnapshotPosition: Integer;
begin
  Result := fSnapshotPosition;
  if Result = -1 then
    Result := Position;
end;

function TNxCustomScrollBar.GetThumbPosition: Integer;
var
  Info: TScrollInfo;
begin
  Info.cbSize := SizeOf( TScrollInfo );
  Info.fMask := SIF_TRACKPOS;

  { Win32 Call }
  Winapi.Windows.GetScrollInfo( fControl.Handle, Flag, Info );
  Result := Info.nTrackPos;
end;

procedure TNxCustomScrollBar.Hide;
begin
  Winapi.Windows.ShowScrollBar( fControl.Handle, Flag, False );
end;

function TNxCustomScrollBar.IsFirst: boolean;
begin
  Result := ScrollInfo.nPos = 0;
end;

function TNxCustomScrollBar.IsLast: boolean;
begin
  Result := ScrollInfo.nPos > ScrollInfo.nMax - Integer( ScrollInfo.nPage );
end;

function TNxCustomScrollBar.IsUpdating: boolean;
begin
  Result := fUpdating;
end;

procedure TNxCustomScrollBar.Last;
begin
  if Max > 0 then
    case Kind of
      sbHorizontal: SendMessage( fControl.Handle, WM_HSCROLL, SB_BOTTOM, 0 );
      sbVertical: SendMessage( fControl.Handle, WM_VSCROLL, SB_BOTTOM, 0 );
    end;
end;

procedure TNxCustomScrollBar.Lock;
begin
  fLocked := True;
end;

procedure TNxCustomScrollBar.MoveBy( Distance: Integer );
begin
  Position := Position + Distance;
end;

procedure TNxCustomScrollBar.Next;
begin
  if Max > 0 then
    case Kind of
      sbHorizontal: SendMessage( fControl.Handle, WM_HSCROLL, SB_LINERIGHT, 0 );
      sbVertical: SendMessage( fControl.Handle, WM_VSCROLL, SB_LINEDOWN, 0 );
    end;
end;

procedure TNxCustomScrollBar.PageDown;
begin
  if Max > 0 then
    case Kind of
      sbHorizontal: SendMessage( fControl.Handle, WM_HSCROLL, SB_PAGERIGHT, 0 );
      sbVertical: SendMessage( fControl.Handle, WM_VSCROLL, SB_PAGEDOWN, 0 );
    end;
end;

procedure TNxCustomScrollBar.PageUp;
begin
  if Max > 0 then
    case Kind of
      sbHorizontal: SendMessage( fControl.Handle, WM_HSCROLL, SB_PAGELEFT, 0 );
      sbVertical: SendMessage( fControl.Handle, WM_VSCROLL, SB_PAGEUP, 0 );
    end;
end;

procedure TNxCustomScrollBar.Prior;
begin
  if Max > 0 then
    case Kind of
      sbHorizontal: SendMessage( fControl.Handle, WM_HSCROLL, SB_LINELEFT, 0 );
      sbVertical: SendMessage( fControl.Handle, WM_VSCROLL, SB_LINEUP, 0 );
    end;
end;

procedure TNxCustomScrollBar.Scroll( ScrollType: TNxScrollType );
var
  ScrollPos: Integer;
begin
  case ScrollType of
    stFirst: ScrollPos := 0;
    stLast: ScrollPos := Max;
    stSmallDecrement: ScrollPos := Position - SmallChange;
    stSmallIncrement: ScrollPos := Position + SmallChange;
    stLargeDecrement: ScrollPos := Position - LargeChange;
    stLargeIncrement: ScrollPos := Position + LargeChange;
    stThumbPosition: ScrollPos := ThumbPosition;
    stThumbTrack: ScrollPos := ThumbPosition;
  else Exit;
  end;

  Position := ScrollPos;
end;

procedure TNxCustomScrollBar.SetAutoHide( const Value: boolean );
begin
  if Value <> fAutoHide then
  begin
    fAutoHide := Value;
    Update;
  end;
end;

procedure TNxCustomScrollBar.SetEnabled( const Value: boolean );
begin

end;

procedure TNxCustomScrollBar.SetKind( const Value: TScrollBarKind );
begin

end;

procedure TNxCustomScrollBar.SetManualScroll( const Value: boolean );
begin

end;

procedure TNxCustomScrollBar.SetMax( const Value: Integer );
begin
  FMax := Value;

  Update; { Set ScrollBar by Win32 }
end;

procedure TNxCustomScrollBar.SetMin( const Value: Integer );
begin
  fMin := Value;

  Update; { Set ScrollBar by Win32 }
end;

procedure TNxCustomScrollBar.SetPageSize( const Value: Integer );
begin
  FPageSize := Value;

  { Note: Max set to 0 will hide Bar. Hide
    Bar if PageSize > Max }
  if FPageSize > FMax then
    Clear;

  Update; { Set ScrollBar by Win32 }
end;

procedure TNxCustomScrollBar.SetPosition( const Value: Integer );
var
  FPrevPosition: Integer;
begin
  FPrevPosition := 0;

  { Remember Pos. before Lock }
  if not fLocked then
    FPrevPosition := Position;

  fOldPosition := FPosition;

  { Note: FPosition is set even if Lock }
  FPosition := Value;

  { Check bounds }
  if FPosition + PageSize > Max then
    FPosition := ( Max - PageSize ) + 1;
  if FPosition < 0 then
    FPosition := 0;

  { Set ScrollBar by
    Win32 (SetScrollInfo) }
  Update;

  if not fLocked then
    case Kind of
      sbHorizontal: fControl.ScrollContentBy( Position - FPrevPosition, 0 );
      sbVertical: fControl.ScrollContentBy( 0, Position - FPrevPosition );
    end;
end;

procedure TNxCustomScrollBar.SetValues( AMax, APageSize: Integer );
begin
  FMax := AMax;
  FPageSize := APageSize;

  CheckValues;

  { Set ScrollBar by Win32 }
  Update;
end;

procedure TNxCustomScrollBar.SetVisible( const Value: boolean );
begin
  FVisible := Value;
  if Showing <> Visible then
  begin
    ShowScrollBar( fControl.Handle, Flag, FVisible );
  end;
end;

function TNxCustomScrollBar.ShouldBeVisible: boolean;
begin
  Result := ( FMax > FPageSize ) or not AutoHide;
end;

procedure TNxCustomScrollBar.Show;
begin
  Winapi.Windows.ShowScrollBar( fControl.Handle, Flag, True );
end;

procedure TNxCustomScrollBar.Snapshot;
begin
  fSnapshotPosition := Position;
end;

procedure TNxCustomScrollBar.Unlock( Update: boolean );
begin
  if fLocked then
  begin
    fLocked := False;

    if Update then
      Self.Update; { Set ScrollBar by Win32 }

    { ToDo: Scroll Content }
  end;
end;

procedure TNxCustomScrollBar.Update;
const
  EnableBar: array [boolean] of DWORD = ( ESB_DISABLE_BOTH, ESB_ENABLE_BOTH );
var
  ScrollInfo: TScrollInfo;
  Flags, nMax, nPage: Integer;
  NeedSet, Unchanged: boolean;
begin
  if fUpdating or fLocked then
    Exit;

  { Required }
  if Assigned( fControl ) and fControl.HandleAllocated and not fControl.IsDestroying then
  begin

    try
      fUpdating := True;

      Flags := SIF_ALL;

      if not AutoHide then
        Flags := Flags or SIF_DISABLENOSCROLL;

      nMax := FMax;
      nPage := FPageSize;

      { Hide or disable Bar }

      { Count is 0 }
      if nMax = -1 then
        nMax := 0;

      case Kind of
        sbHorizontal:
          if nPage > nMax then
          begin
            nMax := 0;
          end;
      end;

      ScrollInfo.nMin := fMin;
      ScrollInfo.nPage := nPage;
      ScrollInfo.nMax := nMax;
      ScrollInfo.nPos := FPosition;
      ScrollInfo.nTrackPos := FPosition;
      ScrollInfo.fMask := Flags;
      ScrollInfo.cbSize := SizeOf( ScrollInfo );

      NeedSet := True;

      if not AutoHide then
      begin
        Unchanged := ( nMax = 0 ) and ( GetScrollInfo.nMax = 0 );

        NeedSet := not Unchanged;
      end;

      { NOTICE: SetScrollInfo need to be called! }

      { Set ScrollBar Prop. in Win }
      if Visible then
      begin

        { Buttons bug. }
        if NeedSet then
        begin
          SetScrollInfo( fControl.Handle, Flag, ScrollInfo, True );
        end;

        if FPageSize < FMax then
        begin
          { 6/20/07:  Disable scrollbar when control is not enabled. If scrollbar is already
            disabled, it will not be enabled. }
          EnableScrollBar( fControl.Handle, Flag, EnableBar[fControl.Enabled and fEnabled and ( FMax > 0 )] );
        end;
      end;

    finally
      fUpdating := False;
    end;

  end;

end;

procedure TNxCustomScrollBar.UpdateScrollBar;
begin

end;

{ TNxTextFitHintWindow }

constructor TNxTextFitHintWindow.Create( AOwner: TComponent );
begin
  inherited;
  Color := clWindow;
end;

procedure TNxTextFitHintWindow.CreateParams( var Params: TCreateParams );
begin
  inherited;
  with Params do
  begin
    WindowClass.Style := WindowClass.Style and not CS_DROPSHADOW;
  end;
end;

{ TControlHelper }

procedure TControlHelper.SetAutoSize;
begin
  Self.AutoSize := False;
  Self.AutoSize := True;
end;

end.
