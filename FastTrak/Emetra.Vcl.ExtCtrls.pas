unit Emetra.Vcl.ExtCtrls;

interface

uses
  System.Classes, System.Types, System.SysUtils, System.Generics.Collections,
  Vcl.Controls, Vcl.ExtCtrls, Vcl.ImgList, Vcl.Imaging.pngimage, System.UITypes,
  {Winapi}
  Winapi.Windows, Winapi.Messages,
  Vcl.Graphics, Vcl.Buttons,
  Emetra.Vcl.Types,
  Emetra.Vcl.Controls,
  Emetra.Vcl.StdCtrls,
  Emetra.Vcl.Buttons,
  Emetra.Vcl.Consts;

const
  intRevealExpanderHeight = 23;
  intToggleOffset         = 15;
  intToggleToCaption      = 8;

resourcestring
  ALERT_HEADER_INFO = 'Informasjon';
  ALERT_HEADER_WARNING = 'Advarsel';
  ALERT_HEADER_ERROR = 'Feil';

type
  { TdcSplitterControl }

  TdcSplitterControl = class( TdcControl )
  strict private
    fDefaultSize: integer;
    fFadeTimer: TTimer;
    fMinSize: integer;
    fResizing: boolean;
    fResizingPos: integer;
    fShowSplitter: boolean;
    fSplitterAlpha: integer;
    fSplitterHeight: integer;
    fSplitterHot: boolean;
    procedure Set_ShowSplitter( const Value: boolean );
    procedure Set_SplitterAlpha( const Value: integer );
    procedure Set_SplitterHeight( const Value: integer );
    procedure Set_SplitterHot( const Value: boolean );
  private
    procedure Set_DefaultSize( const Value: integer );
  protected
    fSplitterGripGlyphs: array [0 .. 1] of TPngImage;
    { Protected Methods }
    function Get_ShowSplitter: boolean; virtual;
    function Get_SplitterRect: TRect; virtual; abstract;
    procedure CreateGripGlyphs; virtual; abstract;
    procedure DoAppDeactivated; virtual;
    procedure DoFadeTimer( Sender: TObject );
    procedure DoHotChanged( const AHot: boolean ); virtual; abstract;
    procedure PaintSplitter;
    procedure DoSplitterChanged; virtual;
    { Overriden Methods }
    procedure MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: integer ); override;
    procedure MouseMove( Shift: TShiftState; X, Y: integer ); override;
    procedure MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: integer ); override;
    { Delphi Messages }
    procedure CMMouseLeave( var Message: TMessage ); message CM_MOUSELEAVE;
    { Winapi Messages }
    procedure WMActivateApp( var Message: TWMActivateApp ); message WM_ACTIVATEAPP;
    { Protected Properties }
    property Resizing: boolean read fResizing write fResizing;
    property SplitterAlpha: integer read fSplitterAlpha write Set_SplitterAlpha;
    property SplitterHot: boolean read fSplitterHot write Set_SplitterHot;
    property SplitterRect: TRect read Get_SplitterRect;
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
  published
    property DefaultSize: integer read fDefaultSize write Set_DefaultSize default 0;
    property MinSize: integer read fMinSize write fMinSize default 280;
    property ShowSplitter: boolean read Get_ShowSplitter write Set_ShowSplitter default false;
    property SplitterHeight: integer read fSplitterHeight write Set_SplitterHeight default 5;
  end;

  { TdcResizeablePanel }

  TdcResizablePanel = class( TdcControl )
  strict private
    fResizing: boolean;
    fResizingY: integer;
    fShowSplitter: boolean;
    fSplitterAlpha: integer;
    fSplitterFadeTimer: TTimer;
    fSplitterGripGlyphs: array [0 .. 1] of TPngImage;
    fSplitterHeight: integer;
    fSplitterHot: boolean;
    { Property Accessors }
    function Get_ShowSplitter: boolean;
    function Get_SplitterRect: TRect;
    procedure Set_ShowSplitter( const Value: boolean );
    procedure Set_SplitterAlpha( const Value: integer );
    procedure Set_SplitterHeight( const Value: integer );
    procedure Set_SplitterHot( const Value: boolean );
  protected
    procedure DoSplitterFadeTimer( Sender: TObject );
    { Overrided Methods }
    procedure CreateParams( var Params: TCreateParams ); override;
    procedure AdjustClientRect( var Rect: TRect ); override;
    procedure MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: integer ); override;
    procedure MouseMove( Shift: TShiftState; X, Y: integer ); override;
    procedure MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: integer ); override;
    procedure Paint; override;
    { Delphi Messages }
    procedure CMMouseLeave( var Message: TMessage ); message CM_MOUSELEAVE;
    { Winapi Messages }
    procedure WMActivateApp( var Message: TWMActivateApp ); message WM_ACTIVATEAPP;
    { Protected Methods }
    procedure LoadGlyphs; virtual;
    procedure PaintSplitter( ACanvas: TCanvas ); virtual;
    { Protected Properties }
    property SplitterAlpha: integer read fSplitterAlpha write Set_SplitterAlpha;
    property SplitterHot: boolean read fSplitterHot write Set_SplitterHot;
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
    property SplitterRect: TRect read Get_SplitterRect;
  published
    property ShowSplitter: boolean read Get_ShowSplitter write Set_ShowSplitter default false;
    property SplitterHeight: integer read fSplitterHeight write Set_SplitterHeight default 5;
  end;

  { TdcHeaderPanel }

  TdcHighlightStyle = ( hsNone, hsSolid, hsThinBar );

  TdcHeaderPanel = class( TdcResizablePanel )
  private
    fActive: boolean;
    fAdaptiveHeaderColors: boolean;
    fButtonStyle: TdcButtonColorStyle;
    fCaptionState: TdcButtonState;
    fCheckBoxState: TdcButtonState;
    fGlyph: TPicture;
    fHeaderColor: TColor;
    fHeaderFontStyle: TFontStyles;
    fHeaderPadding: TPadding;
    fHeaderRelativeFontSize: integer;
    fHighlightActiveColor: TColor;
    fHighlightInactiveColor: TColor;
    fHighlightStyle: TdcHighlightStyle;
    fImageIndex: TImageIndex;
    fImages: TCustomImageList;
    fOldWidth: integer;
    fOnCaptionClick: TNotifyEvent;
    fOnCheckedChanged: TNotifyEvent;
    fOnSideCaptionClick: TNotifyEvent;
    fShowFocus: boolean;
    fShowHeader: boolean;
    fSideCaption: string;
    fSideCaptionState: TdcButtonState;
    function Get_ActualHeaderColor: TColor;
    function Get_BarRect: TRect;
    function Get_CaptionRect: TRect;
    function Get_CheckBoxRect: TRect;
    function Get_Checked: boolean;
    function Get_HeaderContentRect: TRect;
    function Get_HeaderHeight: integer;
    function Get_HeaderRect: TRect;
    function Get_IconSize: TSize;
    function Get_SideCaptionRect: TRect;
    procedure Set_Active( const Value: boolean );
    procedure Set_AdaptiveHeaderColors( const Value: boolean );
    procedure Set_ButtonStyle( const Value: TdcButtonColorStyle );
    procedure Set_CaptionState( const Value: TdcButtonState );
    procedure Set_CheckBoxState( const Value: TdcButtonState );
    procedure Set_Checked( const Value: boolean );
    procedure Set_Glyph( const Value: TPicture );
    procedure Set_HeaderColor( const Value: TColor );
    procedure Set_HeaderFontStyle( const Value: TFontStyles );
    procedure Set_HeaderPadding( const Value: TPadding );
    procedure Set_HeaderRelativeFontSize( const Value: integer );
    procedure Set_HighlightActiveColor( const Value: TColor );
    procedure Set_HighlightInactiveColor( const Value: TColor );
    procedure Set_HighlightStyle( const Value: TdcHighlightStyle );
    procedure Set_ImageIndex( const Value: TImageIndex );
    procedure Set_Images( const Value: TCustomImageList );
    procedure Set_ShowFocus( const Value: boolean );
    procedure Set_ShowHeader( const Value: boolean );
    procedure Set_SideCaption( const Value: string );
    procedure Set_SideCaptionState( const Value: TdcButtonState );
  protected
    procedure AdjustClientRect( var Rect: TRect ); override;
    { Event Handlers }
    procedure DoHeaderPaddingChange( Sender: TObject );
    procedure DoImagesChange( Sender: TObject );
    { Mouse Methods }
    procedure MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: integer ); override;
    procedure MouseMove( Shift: TShiftState; X, Y: integer ); override;
    procedure MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: integer ); override;
    { Painting Methods }
    procedure Paint; override;
    procedure PaintButton( const AButtonRect: TRect; const AButtonState: TdcButtonState );
    procedure PaintHeader; virtual;
    { Delphi Messages }
    procedure CMFocusChanged( var Message: TCMFocusChanged ); message CM_FOCUSCHANGED;
    procedure CMMouseLeave( var Message: TMessage ); message CM_MOUSELEAVE;
    procedure CMTextChanged( var Message: TMessage ); message CM_TEXTCHANGED;
    { Winapi Messages }
    procedure WMSize( var Message: TWMSize ); message WM_SIZE;
    { Protected Properties }
    function GetButtonPadding: integer;
    function HasIcon: boolean;
    property Active: boolean read fActive write Set_Active;
    property BarRect: TRect read Get_BarRect;
    property CaptionRect: TRect read Get_CaptionRect;
    property CaptionState: TdcButtonState read fCaptionState write Set_CaptionState;
    property CheckBoxRect: TRect read Get_CheckBoxRect;
    property CheckBoxState: TdcButtonState read fCheckBoxState write Set_CheckBoxState;
    property HeaderHeight: integer read Get_HeaderHeight;
    property IconSize: TSize read Get_IconSize;
    property SideCaptionRect: TRect read Get_SideCaptionRect;
    property SideCaptionState: TdcButtonState read fSideCaptionState write Set_SideCaptionState;
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
    { Public Methods }
    procedure ToggleChecked;
    { Properties }
    property ActualHeaderColor: TColor read Get_ActualHeaderColor;
    property HeaderRect: TRect read Get_HeaderRect;
    property HeaderContentRect: TRect read Get_HeaderContentRect;
  published
    property AdaptHeaderColors: boolean read fAdaptiveHeaderColors write Set_AdaptiveHeaderColors default true;
    property ButtonStyle: TdcButtonColorStyle read fButtonStyle write Set_ButtonStyle default bcDefault;
    property Caption;
    property Checked: boolean read Get_Checked write Set_Checked default false;
    property Glyph: TPicture read fGlyph write Set_Glyph;
    property HeaderColor: TColor read fHeaderColor write Set_HeaderColor default clNone;
    property HeaderFontStyle: TFontStyles read fHeaderFontStyle write Set_HeaderFontStyle default [fsBold];
    property HeaderPadding: TPadding read fHeaderPadding write Set_HeaderPadding;
    property HeaderRelativeFontSize: integer read fHeaderRelativeFontSize write Set_HeaderRelativeFontSize default 2;
    property HighlightActiveColor: TColor read fHighlightActiveColor write Set_HighlightActiveColor default clSelectedBkDark;
    property HighlightInactiveColor: TColor read fHighlightInactiveColor write Set_HighlightInactiveColor default clTitleLine;
    property HighlightStyle: TdcHighlightStyle read fHighlightStyle write Set_HighlightStyle default hsSolid;
    property ImageIndex: TImageIndex read fImageIndex write Set_ImageIndex default -1;
    property Images: TCustomImageList read fImages write Set_Images;
    property ShowFocus: boolean read fShowFocus write Set_ShowFocus default true;
    property ShowHeader: boolean read fShowHeader write Set_ShowHeader default true;
    property SideCaption: string read fSideCaption write Set_SideCaption;

    property OnCaptionClick: TNotifyEvent read fOnCaptionClick write fOnCaptionClick;
    property OnCheckedChanged: TNotifyEvent read fOnCheckedChanged write fOnCheckedChanged;
    property OnSideCaptionClick: TNotifyEvent read fOnSideCaptionClick write fOnSideCaptionClick;
  end;

  { TdcSplitter }

  TdcSplitter = class( TSplitter )
  private
    fAlpha: integer;
    fFadeTimer: TTimer;
    fGripGlyphs: array [0 .. 3] of TPngImage;
    fHot: boolean;
    procedure Set_Alpha( const Value: integer );
    procedure Set_Hot( const Value: boolean );
  protected
    procedure Paint; override;
    procedure CMMouseEnter( var Message: TMessage ); message CM_MOUSEENTER;
    procedure CMMouseLeave( var Message: TMessage ); message CM_MOUSELEAVE;
    procedure DoFadeTimer( Sender: TObject );
    procedure LoadGlyphs;
    { Winapi Messages }
    procedure WMEraseBkGnd( var Message: TWMEraseBkGnd ); message WM_ERASEBKGND;
    property Alpha: integer read fAlpha write Set_Alpha;
    property Hot: boolean read fHot write Set_Hot;
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
    property ResizeStyle default rsUpdate;
    property Width default 5;
  end;

  { TdcSlidein }

  TSlideDirection = ( sdSlideToLeft, sdSlideToRight );

  TdcSlideinHeader = class;

  TdcSlidein = class( TdcSplitterControl )
  private
    fAnimated: boolean;
    fAnimationTimer: TTimer;
    fContentPadding: TPadding;
    fExpanded: boolean;
    fFullWidth: integer;
    fHeader: TdcSlideinHeader;
    fHeaderState: TdcButtonState;
    fImages: TCustomImageList;
    fOrientation: integer;
    fPanel: TWinControl;
    fSlideDirection: TSlideDirection;
    fSmooth: boolean;
    fUpdating: boolean;
    function Get_HeaderWidth: integer;
    function Get_PanelRect: TRect;
    procedure Set_Expanded( const Value: boolean );
    procedure Set_Images( const Value: TCustomImageList );
    procedure Set_Orientation( const Value: integer );
    procedure Set_Padding( const Value: TPadding );
    procedure Set_SlideDirection( const Value: TSlideDirection );
    procedure Set_FullWidth( const Value: integer );
  protected
    procedure BeginUpdate;
    procedure EndUpdate;
    { Event Handlers }
    procedure DoAnimationTimer( Sender: TObject );
    procedure DoHeaderClick( Sender: TObject );
    procedure DoImagesChange( Sender: TObject );
    { Methods }
    procedure Paint; override;
    procedure RealignPanel( const Value: TWinControl );
    { Virtual Methods }
    procedure CreateGripGlyphs; override;
    procedure DoHotChanged( const AHot: boolean ); override;
    procedure DoSizeChanged( const ADeltaX, ADeltaY: integer ); override;
    procedure DoSplitterChanged; override;
    function Get_ShowSplitter: boolean; override;
    function Get_SplitterRect: TRect; override;
    { Standard Methods }
    procedure AlignControls( AControl: TControl; var Rect: TRect ); override;
    procedure CreateParams( var Params: TCreateParams ); override;
    procedure CreateWnd; override;
    procedure Notification( AComponent: TComponent; Operation: TOperation ); override;
    { DFM Methods }
    procedure DefineProperties( Filer: TFiler ); override;
    procedure ReadFullWidth( Reader: TReader );
    procedure WriteFullWidth( Writer: TWriter );
    { Delphi Messages }
    procedure CMControlChange( var Msg: TCMControlChange ); message CM_CONTROLCHANGE;
    procedure CMTextChanged( var Message: TMessage ); message CM_TEXTCHANGED;
    { Winapi Messages }
    procedure WMSize( var Message: TWMSize ); message WM_SIZE;
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
    property FullWidth: integer read fFullWidth write Set_FullWidth;
    property HeaderWidth: integer read Get_HeaderWidth;
    property Panel: TWinControl read fPanel;
    property PanelRect: TRect read Get_PanelRect;
  published
    property Animated: boolean read fAnimated write fAnimated default true;
    property Caption;
    property Color default clSlideinBk;
    property ContentPadding: TPadding read fContentPadding write Set_Padding;
    property Expanded: boolean read fExpanded write Set_Expanded default true;
    property Images: TCustomImageList read fImages write Set_Images;
    property Orientation: integer read fOrientation write Set_Orientation default 0;
    property Padding;
    property SlideDirection: TSlideDirection read fSlideDirection write Set_SlideDirection default sdSlideToLeft;
    property Smooth: boolean read fSmooth write fSmooth default true;
  end;

  { TdcSlideinHeader }

  TdcSlideinHeader = class( TCustomControl )
  private
    fExpanded: boolean;
    fFadeTimer: TTimer;
    fGlyphs: array [boolean] of TPngImage;
    fHeaderAlpha: integer;
    fOnToggle: TNotifyEvent;
    fPushed: boolean;
    fState: TdcButtonState;
    function Get_ToggleRect: TRect;
    procedure Set_Expanded( const Value: boolean );
    procedure Set_State( const Value: TdcButtonState );
    procedure Set_HeaderAlpha( const Value: integer );
  protected
    { Event Handlers }
    procedure DoFadeTimer( Sender: TObject );
    { Mouse }
    procedure MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: integer ); override;
    procedure MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: integer ); override;
    procedure Paint; override;
    procedure LoadGlyphs;
    { Delphi Messages }
    procedure CMMouseEnter( var Message: TMessage ); message CM_MOUSEENTER;
    procedure CMMouseLeave( var Message: TMessage ); message CM_MOUSELEAVE;
    procedure CMTextChanged( var Message: TMessage ); message CM_TEXTCHANGED;
    { Properties }
    property HeaderAlpha: integer read fHeaderAlpha write Set_HeaderAlpha;
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
    property Caption;
    property Expanded: boolean read fExpanded write Set_Expanded;
    property State: TdcButtonState read fState write Set_State;
    property ToggleRect: TRect read Get_ToggleRect;
    property OnToggle: TNotifyEvent read fOnToggle write fOnToggle;
  end;

  { TdcToolbar }

  TdcToolButton = class;

  TdcToolbar = class( TdcControl )
  private
    fButtonHeight: integer;
    fButtons: TList<TdcToolButton>;
    fButtonWidth: integer;
    fImages: TCustomImageList;
    fShowCaptions: boolean;
    function Get_Button( Index: integer ): TdcToolButton;
    function Get_ButtonCount: integer;
    procedure Set_ButtonHeight( const Value: integer );
    procedure Set_ButtonWidth( const Value: integer );
    procedure Set_Images( const Value: TCustomImageList );
    procedure Set_ShowCaptions( const Value: boolean );
  protected
    procedure AlignControls( AControl: TControl; var Rect: TRect ); override;
    procedure Paint; override;
    procedure UpdateButtons;
    { Delphi Messages }
    procedure CMControlChange( var Message: TCMControlChange ); message CM_CONTROLCHANGE;
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
    { Public Methods }
    function GetButtonRect( AButton: TdcToolButton ): TRect;
    function IndexOf( AButton: TdcToolButton ): integer;
    procedure InsertButton( Control: TControl );
    procedure RemoveButton( Button: TControl );
    procedure ReorderButton( Button: TdcToolButton; ToIndex: integer );
    property ButtonCount: integer read Get_ButtonCount;
    property Buttons[index: integer]: TdcToolButton read Get_Button;
  published
    property Align default alTop;
    property ButtonHeight: integer read fButtonHeight write Set_ButtonHeight default 22;
    property ButtonWidth: integer read fButtonWidth write Set_ButtonWidth default 23;
    property Color default clStatusBarBk;
    property Images: TCustomImageList read fImages write Set_Images;
    property ParentColor default false;
    property ShowCaptions: boolean read fShowCaptions write Set_ShowCaptions default false;
  end;

  { TdcToolButton }

  TdcToolButton = class( TdcCustomSpeedButton )
  private
    fToolbar: TdcToolbar;
    function Get_Index: integer;
  protected
    procedure Paint; override;
    procedure ValidateContainer( AComponent: TComponent ); override;
  public
    constructor Create( AOwner: TComponent ); override;
    procedure SetBounds( ALeft, ATop, AWidth, AHeight: integer ); override;
  published
    property Caption;
    property Cursor default crHandPoint;
    property Height default 22;
    property index: integer read Get_Index;
    property Width default 23;
  end;

  { TdcInlineAlertPresenter }

  TdcInlineAlertPresenter = class( TdcPanel )
  private
    fHeaderSpacing: integer;
    fHeaderText: string;
    fLines: TStrings;
    fShowHeader: boolean;
    fShowIcon: boolean;
    procedure Set_HeaderSpacing( const Value: integer );
    procedure Set_HeaderText( const Value: string );
    procedure Set_Lines( const Value: TStrings );
    procedure Set_ShowHeader( const Value: boolean );
    procedure Set_ShowIcon( const Value: boolean );
  protected
    { Event Handlers }
    procedure DoLinesChange( Sender: TObject );
    procedure Paint; override;
    { Winapi Messages }
  public
    constructor Create( AOwner: TComponent ); override;
  published
    property HeaderSpacing: integer read fHeaderSpacing write Set_HeaderSpacing default 4;
    property HeaderText: string read fHeaderText write Set_HeaderText;
    property Lines: TStrings read fLines write Set_Lines;
    property ShowHeader: boolean read fShowHeader write Set_ShowHeader default true;
    property ShowIcon: boolean read fShowIcon write Set_ShowIcon default true;
  end;

  { TdcRevealExpander }

  TdcRevealExpander = class( TCustomControl )
  private
    fExpanded: boolean;
    fFullHeight: integer;
    fHeaderHeight: integer;
    fPressed: boolean;
    procedure Set_Expanded( const Value: boolean );
    procedure Set_HeaderHeight( const Value: integer );
  protected
    procedure AlignControl( AControl: TControl );
    procedure AdjustClientRect( var Rect: TRect ); override;
    procedure CreateParams( var Params: TCreateParams ); override;
    { Keyboard & Mouse }
    procedure MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: integer ); override;
    procedure MouseMove( Shift: TShiftState; X, Y: integer ); override;
    procedure MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: integer ); override;
    { Painting }
    procedure Paint; override;
    procedure PaintHeader( const AHeaderRect: TRect );
    { DFM Methods }
    procedure DefineProperties( Filer: TFiler ); override;
    procedure ReadFullHeight( Reader: TReader );
    procedure WriteFullHeight( Writer: TWriter );
    { Delphi Messages }
    procedure CMControlChange( var Message: TCMControlChange ); message CM_CONTROLCHANGE;
    { Winapi Messages }
    procedure WMSize( var Message: TWMSize ); message WM_SIZE;
  public
    constructor Create( AOwner: TComponent ); override;
    function GetHeaderRect: TRect;
    function GetToggleButtonSize: TSize;
    function GetToggleButtonRect: TRect;
    procedure InvalidateHeader;
    procedure ToggleExpanded;
  published
    property Align;
    property Anchors;
    property AutoSize;
    property Caption;
    property Color;
    property Constraints;
    property Expanded: boolean read fExpanded write Set_Expanded default true;
    property Font;
    property HeaderHeight: integer read fHeaderHeight write Set_HeaderHeight default intRevealExpanderHeight;
    property Padding;
    property ParentColor;
    property ParentFont;
    property ShowHint;
    property TabOrder;
    property TabStop;
    property Visible;
  end;

  { TdcImageList }

  TdcImageList = class( TCustomImageList )
  public
    procedure Draw( Canvas: TCanvas; X, Y, Index: integer; ADrawingStyle: TDrawingStyle; AImageType: TImageType; Enabled: boolean = true ); overload;
  end;

implementation

uses
  System.Math,
  Vcl.Dialogs, Vcl.Forms,
  Emetra.Vcl.Helpers,
  Emetra.Vcl.Graphics;

{ TdcSplitterControl }

procedure TdcSplitterControl.CMMouseLeave( var Message: TMessage );
begin
  inherited;
  SplitterHot := false;
end;

constructor TdcSplitterControl.Create( AOwner: TComponent );
begin
  inherited;
  fFadeTimer := TTimer.Create( Self );
  fFadeTimer.Enabled := false;
  fFadeTimer.Interval := intSplitterFadeInterval;
  fFadeTimer.OnTimer := DoFadeTimer;
  fMinSize := 280;
  fSplitterAlpha := 0;
  fSplitterHeight := 5;
  fSplitterHot := false;
  CreateGripGlyphs;
end;

destructor TdcSplitterControl.Destroy;
begin
  fSplitterGripGlyphs[0].Free;
  fSplitterGripGlyphs[1].Free;
  inherited;
end;

procedure TdcSplitterControl.DoAppDeactivated;
begin

end;

procedure TdcSplitterControl.DoFadeTimer( Sender: TObject );
begin
  if fSplitterHot then
    SplitterAlpha := SplitterAlpha + intSplitterFadeStep
  else
    SplitterAlpha := SplitterAlpha - intSplitterFadeStep;
end;

procedure TdcSplitterControl.DoSplitterChanged;
begin
  Invalidate;
end;

function TdcSplitterControl.Get_ShowSplitter: boolean;
begin
  Result := fShowSplitter;
end;

procedure TdcSplitterControl.MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: integer );
begin
  inherited;
  if ShowSplitter and SplitterRect.Contains( Point( X, Y ) ) then
  begin
    if ( ssDouble in Shift ) and ( DefaultSize <> 0 ) then
    begin
      Width := DefaultSize;
      exit;
    end;
    fResizingPos := X;
    fResizing := true;
    if CanFocus and CanFocusParentForm then
      SetFocus;
  end;
end;

procedure TdcSplitterControl.MouseMove( Shift: TShiftState; X, Y: integer );
var
  NewWidth: integer;

  function CanResize( var ANewValue: integer ): boolean;
  begin
    Result := true;
    if MinSize > 0 then
    begin
      if ANewValue < fMinSize then
      begin
        ANewValue := fMinSize;
        Result := false;
      end;
    end;
  end;

begin
  inherited;
  if fResizing then
  begin
    if X = fResizingPos then
      exit;

    //
    // Vcl.Forms.GetParentForm( Self, true ).Caption := IntToStr( X );

    case Align of
      alLeft:
        begin
          NewWidth := Width - ( fResizingPos - X );
          if CanResize( NewWidth ) then
          begin
            fResizingPos := X;
          end;
          Width := NewWidth;
        end;
      alRight:
        begin
          NewWidth := Width + ( fResizingPos - X );
          CanResize( NewWidth );
          Width := NewWidth;
        end;
    end;
  end
  else
  begin
    if ShowSplitter and SplitterRect.Contains( Point( X, Y ) ) then
      SplitterHot := true
    else
      SplitterHot := false;
  end;
end;

procedure TdcSplitterControl.MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: integer );
begin
  inherited;
  fResizing := false;
end;

procedure TdcSplitterControl.PaintSplitter;
var
  GlyphIndex: integer;
  BufferRect, GlyphRect: TRect;
  Buffer: TBitmap;
begin
  Buffer := TBitmap.Create;
  try
    Buffer.Width := SplitterRect.Width;
    Buffer.Height := SplitterRect.Height;
    BufferRect := Bounds( 0, 0, Buffer.Width, Buffer.Height );

    with Buffer.Canvas do
    begin
      GlyphIndex := 0;
      if fSplitterAlpha > 64 then
        Inc( GlyphIndex );

      Brush.Color := BlendColor( clSplitterBkHot, Color, fSplitterAlpha );
      FillRect( BufferRect );

      GlyphRect := TRect.Create( 0, 0, fSplitterGripGlyphs[GlyphIndex].Width, fSplitterGripGlyphs[GlyphIndex].Height );
      GlyphRect := CenteredRect( BufferRect, GlyphRect );
      Draw( GlyphRect.left, GlyphRect.Top, fSplitterGripGlyphs[GlyphIndex] );
    end;
    Canvas.Draw( SplitterRect.left, SplitterRect.Top, Buffer );
  finally
    Buffer.Free;
  end;
end;

procedure TdcSplitterControl.Set_DefaultSize( const Value: integer );
begin
  fDefaultSize := Value;
  if fDefaultSize < fMinSize then
    fDefaultSize := fMinSize;

end;

procedure TdcSplitterControl.Set_ShowSplitter( const Value: boolean );
begin
  if Value <> fShowSplitter then
  begin
    fShowSplitter := Value;
    Realign;
    DoSplitterChanged;
  end;
end;

procedure TdcSplitterControl.Set_SplitterAlpha( const Value: integer );
begin
  if Value <> fSplitterAlpha then
  begin
    fSplitterAlpha := Value;
    if fSplitterAlpha > 255 then
    begin
      fSplitterAlpha := 255;
      fFadeTimer.Enabled := false;
    end
    else if fSplitterAlpha < 0 then
    begin
      fSplitterAlpha := 0;
      fFadeTimer.Enabled := false;
    end;
    InvalidateRect( SplitterRect );
  end;
end;

procedure TdcSplitterControl.Set_SplitterHeight( const Value: integer );
begin
  if Value <> fSplitterHeight then
  begin
    fSplitterHeight := Value;
    DoSplitterChanged;
  end;
end;

procedure TdcSplitterControl.Set_SplitterHot( const Value: boolean );
begin
  if Value <> fSplitterHot then
  begin
    fSplitterHot := Value;
    fFadeTimer.Enabled := true;
    DoHotChanged( fSplitterHot ); { Set Cursor etc. }
    InvalidateRect( SplitterRect );
  end;
end;

procedure TdcSplitterControl.WMActivateApp( var Message: TWMActivateApp );
begin
  inherited;
  if not message.Active then
  begin
    DoAppDeactivated;
    fResizing := false;
    SplitterHot := false;
  end;
end;

{ TdcResizablePanel }

procedure TdcResizablePanel.AdjustClientRect( var Rect: TRect );
begin
  inherited;
  if ShowSplitter then
    Inc( Rect.Top, SplitterHeight );
end;

procedure TdcResizablePanel.CMMouseLeave( var Message: TMessage );
begin
  inherited;
  SplitterHot := false;
end;

constructor TdcResizablePanel.Create( AOwner: TComponent );
begin
  inherited;
  ControlStyle := ControlStyle + [csAcceptsControls, csCaptureMouse];
  fSplitterAlpha := 0;
  fSplitterFadeTimer := TTimer.Create( Self );
  fSplitterFadeTimer.Enabled := false;
  fSplitterFadeTimer.Interval := intSplitterFadeInterval;
  fSplitterFadeTimer.OnTimer := DoSplitterFadeTimer;
  fSplitterHeight := 5;
  fSplitterHot := false;
  fResizing := false;
  LoadGlyphs;
end;

procedure TdcResizablePanel.CreateParams( var Params: TCreateParams );
begin
  inherited;
  with Params do
  begin
    with WindowClass do
      Style := Style and not CS_VREDRAW and not CS_HREDRAW;
  end;
end;

destructor TdcResizablePanel.Destroy;
begin
  fSplitterGripGlyphs[0].Free;
  fSplitterGripGlyphs[1].Free;
  inherited;
end;

procedure TdcResizablePanel.DoSplitterFadeTimer( Sender: TObject );
begin
  if fSplitterHot then
    SplitterAlpha := SplitterAlpha + intSplitterFadeStep
  else
    SplitterAlpha := SplitterAlpha - intSplitterFadeStep;
end;

function TdcResizablePanel.Get_ShowSplitter: boolean;
begin
  Result := fShowSplitter and ( Align = alBottom );
end;

function TdcResizablePanel.Get_SplitterRect: TRect;
begin
  if ShowSplitter then
    Result := TRect.Create( 0, 0, ClientWidth, SplitterHeight )
  else
    Result := TRect.Empty;
end;

procedure TdcResizablePanel.LoadGlyphs;
begin
  fSplitterGripGlyphs[0] := TPngImage.Create;
  fSplitterGripGlyphs[0].LoadFromRHResourceName( HInstance, 'SPLITTERGRIPDARK' );
  fSplitterGripGlyphs[1] := TPngImage.Create;
  fSplitterGripGlyphs[1].LoadFromRHResourceName( HInstance, 'SPLITTERGRIPLIGHT' );
end;

procedure TdcResizablePanel.MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: integer );
begin
  inherited;
  if ShowSplitter and SplitterRect.Contains( Point( X, Y ) ) then
  begin
    fResizingY := Y;
    fResizing := true;
    if CanFocus and CanFocusParentForm then
      SetFocus;
  end;
end;

procedure TdcResizablePanel.MouseMove( Shift: TShiftState; X, Y: integer );
begin
  inherited;
  if fResizing then
  begin
    if Y = fResizingY then
      exit;

    Height := Height + ( fResizingY - Y );
  end
  else
  begin
    if ShowSplitter and SplitterRect.Contains( Point( X, Y ) ) then
      SplitterHot := true
    else
      SplitterHot := false;
  end;
end;

procedure TdcResizablePanel.MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: integer );
begin
  inherited;
  fResizing := false;
end;

procedure TdcResizablePanel.Paint;
begin
  inherited;
  if ShowSplitter then
    PaintSplitter( Canvas );
end;

procedure TdcResizablePanel.PaintSplitter( ACanvas: TCanvas );
var
  GlyphIndex: integer;
  GlyphRect: TRect;
  Buffer: TBitmap;
begin
  Buffer := TBitmap.Create;
  try
    Buffer.Width := SplitterRect.Width;
    Buffer.Height := SplitterRect.Height;

    with Buffer.Canvas do
    begin
      case Align of
        alLeft, alRight: GlyphIndex := 2;
      else GlyphIndex := 0;
      end;
      if fSplitterAlpha > 64 then
        Inc( GlyphIndex );

      Brush.Color := BlendColor( clSplitterBkHot, Color, fSplitterAlpha );
      FillRect( SplitterRect );

      GlyphRect := TRect.Create( 0, 0, fSplitterGripGlyphs[GlyphIndex].Width, fSplitterGripGlyphs[GlyphIndex].Height );
      GlyphRect := CenteredRect( SplitterRect, GlyphRect );
      Draw( GlyphRect.left, GlyphRect.Top, fSplitterGripGlyphs[GlyphIndex] );
    end;
    ACanvas.Draw( SplitterRect.left, SplitterRect.Top, Buffer );
  finally
    Buffer.Free;
  end;
end;

procedure TdcResizablePanel.Set_ShowSplitter( const Value: boolean );
begin
  if Value <> fShowSplitter then
  begin
    fShowSplitter := Value;
    Invalidate;
    Realign;
  end;
end;

procedure TdcResizablePanel.Set_SplitterAlpha( const Value: integer );
begin
  if Value <> fSplitterAlpha then
  begin
    fSplitterAlpha := Value;
    if fSplitterAlpha > 255 then
    begin
      fSplitterAlpha := 255;
      fSplitterFadeTimer.Enabled := false;
    end
    else if fSplitterAlpha < 0 then
    begin
      fSplitterAlpha := 0;
      fSplitterFadeTimer.Enabled := false;
    end;
    InvalidateRect( SplitterRect );
  end;
end;

procedure TdcResizablePanel.Set_SplitterHeight( const Value: integer );
begin
  if Value <> fSplitterHeight then
  begin
    fSplitterHeight := Value;
    Invalidate;
    Realign;
  end;
end;

procedure TdcResizablePanel.Set_SplitterHot( const Value: boolean );
begin
  if Value <> fSplitterHot then
  begin
    fSplitterHot := Value;
    if fSplitterHot then
      Cursor := crSizeNS
    else
      Cursor := crDefault;
    fSplitterFadeTimer.Enabled := true;
    InvalidateRect( SplitterRect );
  end;
end;

procedure TdcResizablePanel.WMActivateApp( var Message: TWMActivateApp );
begin
  inherited;
  if not Message.Active then
  begin
    fResizing := false;
    SplitterHot := false;
  end;
end;

{ TdcHeaderPanel }

procedure TdcHeaderPanel.AdjustClientRect( var Rect: TRect );
begin
  inherited;
  if ShowHeader then
    Inc( Rect.Top, HeaderHeight );
end;

procedure TdcHeaderPanel.CMFocusChanged( var Message: TCMFocusChanged );
begin
  inherited;
  if ShowFocus then
    Active := message.Sender.Focused and ( IsRelated( message.Sender, Self ) or ( message.Sender = Self ) );
end;

procedure TdcHeaderPanel.CMMouseLeave( var Message: TMessage );
begin
  inherited;
  CaptionState := CaptionState - [btHot];
  SideCaptionState := SideCaptionState - [btHot];
end;

procedure TdcHeaderPanel.CMTextChanged( var Message: TMessage );
begin
  if HandleAllocated then
    InvalidateRect( HeaderRect );
end;

constructor TdcHeaderPanel.Create( AOwner: TComponent );
begin
  inherited;
  ControlStyle := ControlStyle + [csAcceptsControls, csCaptureMouse, csOpaque];
  fActive := false;
  fAdaptiveHeaderColors := true;
  fButtonStyle := bcDefault;
  fCaptionState := [];
  fCheckBoxState := [];
  fGlyph := TPicture.Create;
  fGlyph.OnChange := DoImagesChange;
  fHighlightActiveColor := clStatusBarBk;
  fHeaderColor := clNone;
  fHeaderFontStyle := [fsBold];
  fHighlightInactiveColor := clTitleLine;
  fHeaderPadding := TPadding.Create( nil );
  fHeaderPadding.SetBounds( SpacingDefault, 0, SpacingDefault, 0 );
  fHeaderPadding.OnChange := DoHeaderPaddingChange;
  fHeaderRelativeFontSize := 2;
  fHighlightStyle := hsSolid;
  fImageIndex := -1;
  fImages := nil;
  fOldWidth := Width;
  fShowFocus := true;
  fShowHeader := true;
  fSideCaption := '';
  fSideCaptionState := [];
end;

destructor TdcHeaderPanel.Destroy;
begin
  FreeAndNil( fGlyph );
  FreeAndNil( fHeaderPadding );
  inherited;
end;

procedure TdcHeaderPanel.DoHeaderPaddingChange( Sender: TObject );
begin
  Invalidate;
  Realign;
end;

procedure TdcHeaderPanel.DoImagesChange( Sender: TObject );
begin
  Invalidate;
end;

function TdcHeaderPanel.GetButtonPadding: integer;
begin
  Result := SpacingDouble;
end;

function TdcHeaderPanel.Get_ActualHeaderColor: TColor;
begin
  if HighlightStyle = hsSolid then
    Result := IfThen( Active, HighlightActiveColor, HighlightInactiveColor )
  else
    Result := IfThen( HeaderColor <> clNone, HeaderColor, Color );
end;

function TdcHeaderPanel.Get_BarRect: TRect;
begin
  Result := HeaderRect;
  Result.Height := 2;
  Result.Offset( 0, 2 );
  Result.Inflate( -2, 0 );
end;

function TdcHeaderPanel.Get_CaptionRect: TRect;
var
  IconLocation: TPoint;
begin
  Result := HeaderContentRect;
  if Assigned( fOnCheckedChanged ) then
    Inc( Result.left, intCheckBoxSize + SpacingDouble );

  { Icon }
  if HasIcon then
  begin
    IconLocation := Point( Result.left, Result.Top + Result.Height div 2 - IconSize.cy div 2 + 1 );
    if Assigned( Images ) and Images.TryDraw( Canvas, IconLocation.X, IconLocation.Y, ImageIndex ) then
      Inc( Result.left, Images.Width + SpacingDouble )
    else
    begin
      Inc( Result.left, Glyph.Width + SpacingDouble );
    end;
  end;

  Canvas.Font.Assign( Font );
  Canvas.Font.Size := Canvas.Font.Size + fHeaderRelativeFontSize;
  Canvas.Font.Style := fHeaderFontStyle;

  with Canvas.GetTextSize( Caption ) do
  begin
    Result.Height := cy;
    Result.Width := cx;
    Result.Offset( 0, HeaderContentRect.Height div 2 - Result.Height div 2 );
    if Assigned( fOnCaptionClick ) then
      Result.Inflate( GetButtonPadding, GetButtonPadding );
  end;
end;

function TdcHeaderPanel.Get_CheckBoxRect: TRect;
begin
  Result := HeaderContentRect;
  Result.Offset( 0, Result.Height div 2 - intCheckBoxSize div 2 );
  Result.Size := TSize.Create( intCheckBoxSize, intCheckBoxSize );
end;

function TdcHeaderPanel.Get_Checked: boolean;
begin
  Result := btChecked in fCheckBoxState;
end;

function TdcHeaderPanel.Get_HeaderContentRect: TRect;
begin
  Result := HeaderRect;
  Result.Inflate( HeaderPadding, true );
end;

function TdcHeaderPanel.Get_HeaderHeight: integer;
begin
  Result := GetFontHeight( Font ) + HeaderPadding.Height + SpacingDefault * 2;
  if HighlightStyle = hsThinBar then
    Inc( Result, 3 );
end;

function TdcHeaderPanel.Get_HeaderRect: TRect;
begin
  Result := Bounds( 0, SplitterRect.Bottom, ClientWidth, HeaderHeight );
end;

function TdcHeaderPanel.Get_IconSize: TSize;
begin
  if Assigned( Images ) and InRange( ImageIndex, 0, Pred( Images.Count ) ) then
    Result := TSize.Create( Images.Width, Images.Height )
  else if Assigned( Glyph.Graphic ) and not Glyph.Graphic.Empty then
    Result := TSize.Create( Glyph.Width, Glyph.Height )
  else
    Result := TSize.Create( 0, 0 );
end;

function TdcHeaderPanel.Get_SideCaptionRect: TRect;
begin
  Canvas.Font.Assign( Font );

  Result := HeaderRect;
  Result.Inflate( HeaderPadding, true );

  with Canvas.GetTextSize( SideCaption ) do
  begin
    Result.Height := cy;
    Result.left := HeaderRect.Right - HeaderPadding.Right - cx;
    Result.Offset( 0, HeaderHeight div 2 - Result.Height div 2 );
    Result.Width := cx;
    Result.Inflate( GetButtonPadding, GetButtonPadding );
  end;
end;

function TdcHeaderPanel.HasIcon: boolean;
begin
  Result := ( Assigned( Images ) and InRange( ImageIndex, 0, Pred( Images.Count ) ) ) or ( Assigned( Glyph.Graphic ) and not Glyph.Graphic.Empty );
end;

procedure TdcHeaderPanel.MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: integer );
begin
  inherited;
  if Assigned( fOnCheckedChanged ) and ( Button = mbLeft ) and CheckBoxRect.Contains( Point( X, Y ) ) then
    CheckBoxState := CheckBoxState + [btPushed];

  if Assigned( fOnCaptionClick ) and ( Button = mbLeft ) and CaptionRect.Contains( Point( X, Y ) ) then
    CaptionState := CaptionState + [btPushed];

  if Assigned( fOnSideCaptionClick ) and ( Button = mbLeft ) and SideCaptionRect.Contains( Point( X, Y ) ) then
    SideCaptionState := SideCaptionState + [btPushed];
end;

procedure TdcHeaderPanel.MouseMove( Shift: TShiftState; X, Y: integer );
begin
  inherited;
  { CheckBox }
  if Assigned( fOnCheckedChanged ) and CheckBoxRect.Contains( Point( X, Y ) ) then
    CheckBoxState := CheckBoxState + [btHot]
  else
    CheckBoxState := CheckBoxState - [btHot];

  { Caption }
  if Assigned( fOnCaptionClick ) and CaptionRect.Contains( Point( X, Y ) ) then
    CaptionState := CaptionState + [btHot]
  else
    CaptionState := CaptionState - [btHot];

  { SideCaption }
  if Assigned( fOnSideCaptionClick ) and SideCaptionRect.Contains( Point( X, Y ) ) then
    SideCaptionState := SideCaptionState + [btHot]
  else
    SideCaptionState := SideCaptionState - [btHot];
end;

procedure TdcHeaderPanel.MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: integer );
begin
  inherited;
  if btPushed in CheckBoxState then
  begin
    CheckBoxState := CheckBoxState - [btPushed];
    if CheckBoxRect.Contains( Point( X, Y ) ) then
    begin
      ToggleChecked;
      if Assigned( fOnCheckedChanged ) then
        fOnCheckedChanged( Self );
    end;
  end;

  if btPushed in CaptionState then
  begin
    CaptionState := CaptionState - [btPushed];
    if CaptionRect.Contains( Point( X, Y ) ) then
      if Assigned( fOnCaptionClick ) then
        fOnCaptionClick( Self );
  end;

  if btPushed in SideCaptionState then
  begin
    SideCaptionState := SideCaptionState - [btPushed];
    if SideCaptionRect.Contains( Point( X, Y ) ) then
      if Assigned( fOnSideCaptionClick ) then
        fOnSideCaptionClick( Self );
  end;
end;

procedure TdcHeaderPanel.Paint;
begin
  inherited;
  if ShowHeader then
    PaintHeader;
end;

procedure TdcHeaderPanel.PaintButton( const AButtonRect: TRect; const AButtonState: TdcButtonState );
begin
  case fButtonStyle of
    bcDefault: Canvas.PaintFlatButtonBackground( AButtonRect, AButtonState );
    bcLight: PaintLightButton( Canvas, ActualHeaderColor, AButtonRect, AButtonState, false );
    bcDark: PaintDarkButton( Canvas, ActualHeaderColor, AButtonRect, AButtonState, false );
  end;
end;

procedure TdcHeaderPanel.PaintHeader;
var
  contentRect, BarRect, CaptionTextRect: TRect;
  IconLocation: TPoint;
  lightColor: boolean;
  buttonTextColor: TColor;
begin
  lightColor := false;

  { Solid Color or bar }
  with Canvas do
  begin
    Font.Assign( Self.Font );
    case HighlightStyle of
      hsNone:
        begin
          Brush.Color := IfThen( HeaderColor <> clNone, HeaderColor, Color );
          FillRect( HeaderRect );
          lightColor := IsColorLight( Brush.Color );
        end;
      hsSolid:
        begin
          if Active then
            Brush.Color := HighlightActiveColor
          else
            Brush.Color := HighlightInactiveColor;
          FillRect( HeaderRect );
          lightColor := IsColorLight( Brush.Color );
        end;
      hsThinBar:
        begin
          Brush.Color := IfThen( HeaderColor <> clNone, HeaderColor, Color );

          FillRect( HeaderRect );
          lightColor := IsColorLight( Brush.Color );

          BarRect := HeaderRect;
          BarRect.Height := 2;
          BarRect.Offset( 0, 2 );
          BarRect.Inflate( -2, 0 );
          if Active then
            Canvas.Brush.Color := fHighlightActiveColor
          else
            Canvas.Brush.Color := fHighlightInactiveColor;
          if Canvas.Brush.Color <> clNone then
            Canvas.FillRect( BarRect );
        end;
    end;
  end;

  contentRect := HeaderRect;
  contentRect.Inflate( HeaderPadding, true );

  { CheckBox }
  if Assigned( OnCheckedChanged ) then
  begin
    TdcCheckBox.DrawCheckBox( Canvas, ccCheckBox, CheckBoxRect, fCheckBoxState );
    contentRect.Offset( intCheckBoxSize + SpacingDouble, 0 );
  end;

  { Icon }
  if HasIcon then
  begin
    IconLocation := Point( contentRect.left, contentRect.Top + contentRect.Height div 2 - IconSize.cy div 2 + 1 );
    if Assigned( Images ) and Images.TryDraw( Canvas, IconLocation.X, IconLocation.Y, ImageIndex ) then
      Inc( contentRect.left, Images.Width + SpacingDouble )
    else
    begin
      Canvas.Draw( IconLocation.X, IconLocation.Y, Glyph.Graphic );
      Inc( contentRect.left, Glyph.Width + SpacingDouble );
    end;
  end;

  { SideCaption }
  Canvas.Font.Assign( Font );
  buttonTextColor := Canvas.Font.Color;
  if AdaptHeaderColors and not lightColor then
    buttonTextColor := clWhite;

  if Assigned( fOnSideCaptionClick ) and ( btHot in fSideCaptionState ) then
    PaintButton( SideCaptionRect, fSideCaptionState );

  CaptionTextRect := SideCaptionRect;
  CaptionTextRect.Inflate( -SpacingDouble, -SpacingDouble );

  Canvas.Font.Color := buttonTextColor;
  Canvas.RenderText( CaptionTextRect, SideCaption, [tfRight, tfSingleLine, tfVerticalCenter, tfEndEllipsis] );

  { Caption }
  CaptionTextRect := CaptionRect;
  if Assigned( fOnCaptionClick ) then
  begin
    if btHot in fCaptionState then
      PaintButton( CaptionRect, fCaptionState );
    CaptionTextRect.Inflate( -GetButtonPadding, -GetButtonPadding );
  end;

{$IFDEF DEBUG_INDICATORS}
  Canvas.Brush.Color := clRed;
  Canvas.FillRect( HeaderContentRect );
  Canvas.Brush.Color := clPurple;
  Canvas.FillRect( CaptionRect );
  Canvas.Brush.Color := clLime;
  Canvas.FillRect( CaptionTextRect );
{$ENDIF}
  Canvas.Font.Assign( Font );
  Canvas.Font.Size := Canvas.Font.Size + fHeaderRelativeFontSize;
  Canvas.Font.Style := fHeaderFontStyle;
  Canvas.Font.Color := buttonTextColor;
  Canvas.RenderText( CaptionTextRect, Caption, [tfLeft, tfSingleLine, tfVerticalCenter, tfEndEllipsis] );
end;

procedure TdcHeaderPanel.Set_Active( const Value: boolean );
begin
  if Value <> fActive then
  begin
    fActive := Value;
    case HighlightStyle of
      hsSolid: InvalidateRect( HeaderRect );
      hsThinBar: InvalidateRect( BarRect );
    end;
    if fActive then
      CheckBoxState := CheckBoxState + [btFocused]
    else
      CheckBoxState := CheckBoxState - [btFocused];
  end;
end;

procedure TdcHeaderPanel.Set_AdaptiveHeaderColors( const Value: boolean );
begin
  if Value <> fAdaptiveHeaderColors then
  begin
    fAdaptiveHeaderColors := Value;
    InvalidateRect( HeaderRect );
  end;
end;

procedure TdcHeaderPanel.Set_ButtonStyle( const Value: TdcButtonColorStyle );
begin
  if Value <> fButtonStyle then
  begin
    fButtonStyle := Value;
    InvalidateRect( HeaderRect );
  end;
end;

procedure TdcHeaderPanel.Set_CaptionState( const Value: TdcButtonState );
begin
  if Value <> fCaptionState then
  begin
    fCaptionState := Value;
    if btHot in fCaptionState then
      Cursor := crHandPoint
    else
      Cursor := crDefault;
    InvalidateRect( CaptionRect );
  end;
end;

procedure TdcHeaderPanel.Set_CheckBoxState( const Value: TdcButtonState );
begin
  if Value <> fCheckBoxState then
  begin
    fCheckBoxState := Value;
    if btHot in fCheckBoxState then
      Cursor := crHandPoint
    else
      Cursor := crDefault;
    InvalidateRect( CheckBoxRect );
  end;
end;

procedure TdcHeaderPanel.Set_Checked( const Value: boolean );
begin
  if Value <> Get_Checked then
  begin
    if btChecked in CheckBoxState then
      CheckBoxState := CheckBoxState - [btChecked]
    else
      CheckBoxState := CheckBoxState + [btChecked];
    InvalidateRect( CheckBoxRect );
  end;
end;

procedure TdcHeaderPanel.Set_Glyph( const Value: TPicture );
begin
  Assert( Assigned( Value ) );
  fGlyph.Assign( Value );
  fGlyph.OnChange := DoImagesChange;
end;

procedure TdcHeaderPanel.Set_HighlightActiveColor( const Value: TColor );
begin
  if Value <> fHighlightActiveColor then
  begin
    fHighlightActiveColor := Value;
    InvalidateRect( HeaderRect );
  end;
end;

procedure TdcHeaderPanel.Set_HeaderColor( const Value: TColor );
begin
  if Value <> fHeaderColor then
  begin
    fHeaderColor := Value;
    InvalidateRect( HeaderRect );
  end;
end;

procedure TdcHeaderPanel.Set_HeaderFontStyle( const Value: TFontStyles );
begin
  if Value <> fHeaderFontStyle then
  begin
    fHeaderFontStyle := Value;
    InvalidateRect( HeaderRect );
  end;
end;

procedure TdcHeaderPanel.Set_HighlightInactiveColor( const Value: TColor );
begin
  if Value <> fHighlightInactiveColor then
  begin
    fHighlightInactiveColor := Value;
    InvalidateRect( HeaderRect );
  end;
end;

procedure TdcHeaderPanel.Set_HeaderPadding( const Value: TPadding );
begin
  fHeaderPadding.Assign( Value );
end;

procedure TdcHeaderPanel.Set_HeaderRelativeFontSize( const Value: integer );
begin
  if Value <> fHeaderRelativeFontSize then
  begin
    fHeaderRelativeFontSize := Value;
    InvalidateRect( HeaderRect );
  end;
end;

procedure TdcHeaderPanel.Set_HighlightStyle( const Value: TdcHighlightStyle );
begin
  if Value <> fHighlightStyle then
  begin
    fHighlightStyle := Value;
    InvalidateRect( HeaderRect );
  end;
end;

procedure TdcHeaderPanel.Set_ImageIndex( const Value: TImageIndex );
begin
  if Value <> fImageIndex then
  begin
    fImageIndex := Value;
    Invalidate;
  end;
end;

procedure TdcHeaderPanel.Set_Images( const Value: TCustomImageList );
begin
  if Value <> fImages then
  begin
    if Assigned( fImages ) then
    begin
      fImages.RemoveFreeNotification( Self );
      fImages.OnChange := nil;;
    end;
    fImages := Value;
    if Assigned( fImages ) then
    begin
      fImages.FreeNotification( Self );
      fImages.OnChange := DoImagesChange;
    end;
  end;
end;

procedure TdcHeaderPanel.Set_ShowFocus( const Value: boolean );
begin
  if Value <> fShowFocus then
  begin
    fShowFocus := Value;
    Invalidate;
  end;
end;

procedure TdcHeaderPanel.Set_ShowHeader( const Value: boolean );
begin
  if Value <> fShowHeader then
  begin
    fShowHeader := Value;
    Invalidate;
    Realign;
  end;
end;

procedure TdcHeaderPanel.Set_SideCaption( const Value: string );
begin
  if Value <> fSideCaption then
  begin
    fSideCaption := Value;
    Invalidate;
  end;
end;

procedure TdcHeaderPanel.Set_SideCaptionState( const Value: TdcButtonState );
begin
  if Value <> fSideCaptionState then
  begin
    fSideCaptionState := Value;
    if btHot in fSideCaptionState then
      Cursor := crHandPoint
    else
      Cursor := crDefault;
    InvalidateRect( SideCaptionRect );
  end;
end;

procedure TdcHeaderPanel.ToggleChecked;
begin
  Checked := not Checked;
end;

procedure TdcHeaderPanel.WMSize( var Message: TWMSize );
begin
  inherited;
  { Avoid header flickering }
  if message.Width <> fOldWidth then
  begin
    fOldWidth := message.Width;
    InvalidateRect( HeaderRect );
  end;
end;

{ TdcSplitter }

procedure TdcSplitter.CMMouseEnter( var Message: TMessage );
begin
  inherited;
  Hot := true;
end;

procedure TdcSplitter.CMMouseLeave( var Message: TMessage );
begin
  inherited;
  Hot := false;
end;

constructor TdcSplitter.Create( AOwner: TComponent );
begin
  inherited;
  ControlStyle := ControlStyle + [csOpaque];
  fAlpha := 0;
  fFadeTimer := TTimer.Create( Self );
  fFadeTimer.Interval := intSplitterFadeInterval;
  fFadeTimer.OnTimer := DoFadeTimer;
  fHot := false;
  LoadGlyphs;
  ResizeStyle := rsUpdate;
  Width := 5;
end;

destructor TdcSplitter.Destroy;
begin
  fGripGlyphs[0].Free;
  fGripGlyphs[1].Free;
  fGripGlyphs[2].Free;
  fGripGlyphs[3].Free;
  inherited;
end;

procedure TdcSplitter.DoFadeTimer( Sender: TObject );
begin
  if fHot then
    Alpha := Alpha + intSplitterFadeStep
  else
    Alpha := Alpha - intSplitterFadeStep;
end;

procedure TdcSplitter.LoadGlyphs;
begin
  fGripGlyphs[0] := TPngImage.Create;
  fGripGlyphs[0].LoadFromRHResourceName( HInstance, 'SPLITTERGRIPDARK' );
  fGripGlyphs[1] := TPngImage.Create;
  fGripGlyphs[1].LoadFromRHResourceName( HInstance, 'SPLITTERGRIPLIGHT' );
  fGripGlyphs[2] := TPngImage.Create;
  fGripGlyphs[2].LoadFromRHResourceName( HInstance, 'SPLITTERGRIPDARKVERT' );
  fGripGlyphs[3] := TPngImage.Create;
  fGripGlyphs[3].LoadFromRHResourceName( HInstance, 'SPLITTERGRIPLIGHTVERT' );
end;

procedure TdcSplitter.Paint;
var
  GlyphIndex: integer;
  GlyphRect: TRect;
  Buffer: TBitmap;
begin
  Buffer := TBitmap.Create;
  try
    Buffer.Width := Width;
    Buffer.Height := Height;

    with Buffer.Canvas do
    begin
      case Align of
        alLeft, alRight: GlyphIndex := 2;
      else GlyphIndex := 0;
      end;
      if fAlpha > 64 then
        Inc( GlyphIndex );

      Brush.Color := BlendColor( clSplitterBkHot, Color, fAlpha );
      FillRect( ClientRect );

      GlyphRect := TRect.Create( 0, 0, fGripGlyphs[GlyphIndex].Width, fGripGlyphs[GlyphIndex].Height );
      GlyphRect := CenteredRect( ClientRect, GlyphRect );
      Draw( GlyphRect.left, GlyphRect.Top, fGripGlyphs[GlyphIndex] );
    end;
    Canvas.Draw( 0, 0, Buffer );
  finally
    Buffer.Free;
  end;
end;

procedure TdcSplitter.Set_Alpha( const Value: integer );
begin
  if Value <> fAlpha then
  begin
    fAlpha := Value;
    if fAlpha > 255 then
    begin
      fAlpha := 255;
      fFadeTimer.Enabled := false;
    end
    else if fAlpha < 0 then
    begin
      fAlpha := 0;
      fFadeTimer.Enabled := false;
    end;
    Invalidate;
  end;
end;

procedure TdcSplitter.Set_Hot( const Value: boolean );
begin
  if Value <> fHot then
  begin
    fHot := Value;
    fFadeTimer.Enabled := true;
    Invalidate;
  end;
end;

procedure TdcSplitter.WMEraseBkGnd( var Message: TWMEraseBkGnd );
begin
  message.Result := 1;
end;

{ TdcSlidein }

procedure TdcSlidein.AlignControls( AControl: TControl; var Rect: TRect );
begin
  if Assigned( fPanel ) and ( AControl = fPanel ) then
  begin
    Rect := PanelRect;
  end
  else
    inherited;
end;

procedure TdcSlidein.BeginUpdate;
begin
  fUpdating := true;
  if Assigned( fPanel ) then
  begin
    if Expanded then
      fPanel.Show
    else if not Smooth then
      fPanel.Hide;
  end;
  fAnimationTimer.Enabled := true;
end;

procedure TdcSlidein.CMControlChange( var Msg: TCMControlChange );
begin
  inherited;
  if ( csDestroying in ComponentState ) or ( csLoading in ComponentState ) then
    exit;

  if Msg.Inserting and ( Msg.Control.Parent = Self ) then
  begin
    DisableAlign;
    try
      if ( Msg.Control <> fHeader ) and not Assigned( fPanel ) then
      begin
        fPanel := TWinControl( Msg.Control );
        RealignPanel( fPanel );
      end;
      Realign;
    finally
      EnableAlign;
    end;
  end
  else
    fPanel := nil;
end;

procedure TdcSlidein.CMTextChanged( var Message: TMessage );
begin
  inherited;
  fHeader.Caption := Caption;
end;

constructor TdcSlidein.Create( AOwner: TComponent );
begin
  inherited;
  ControlStyle := ControlStyle + [csAcceptsControls, csSetCaption, csOpaque];
  fAnimated := true;
  fAnimationTimer := TTimer.Create( Self );
  fAnimationTimer.Enabled := false;
  fAnimationTimer.OnTimer := DoAnimationTimer;
  fAnimationTimer.Interval := 1;
  fContentPadding := TPadding.Create( nil );
  fContentPadding.SetBounds( 4, 4, 4, 4 );
  fExpanded := true;
  fFullWidth := 260;
  fHeader := TdcSlideinHeader.Create( Self );
  fHeader.Align := alLeft;
  fHeader.OnToggle := DoHeaderClick;
  fHeader.Parent := Self;
  fHeader.SetSubComponent( true );
  fHeader.Width := 30;
  fHeaderState := [];
  fImages := nil;
  fOrientation := 900;
  fPanel := nil;
  fSlideDirection := sdSlideToLeft;
  fSmooth := true;
  fUpdating := false;
  Color := clSlideinBk;
  Width := fFullWidth;
  Height := 280;
end;

procedure TdcSlidein.CreateGripGlyphs;
begin
  inherited;
  fSplitterGripGlyphs[0] := TPngImage.Create;
  fSplitterGripGlyphs[0].LoadFromRHResourceName( HInstance, 'SPLITTERGRIPDARKVERT' );
  fSplitterGripGlyphs[1] := TPngImage.Create;
  fSplitterGripGlyphs[1].LoadFromRHResourceName( HInstance, 'SPLITTERGRIPLIGHTVERT' );
end;

procedure TdcSlidein.CreateParams( var Params: TCreateParams );
begin
  inherited;
  with Params do
  begin
    with WindowClass do
      Style := Style and not CS_HREDRAW and not CS_VREDRAW;
  end;
end;

procedure TdcSlidein.CreateWnd;
var
  i: integer;
begin
  inherited;
  for i := 0 to Pred( ControlCount ) do
  begin
    if Controls[i] <> fHeader then
    begin
      fPanel := TWinControl( Controls[i] );
      RealignPanel( fPanel );
      exit;
    end;
  end;
end;

procedure TdcSlidein.DefineProperties( Filer: TFiler );
begin
  inherited;
  Filer.DefineProperty( 'FullWidth', ReadFullWidth, WriteFullWidth, true );
end;

destructor TdcSlidein.Destroy;
begin
  fContentPadding.Free;
  inherited;
end;

procedure TdcSlidein.DoAnimationTimer( Sender: TObject );
const
  Step = 32;
begin
  if Expanded then
  begin
    if ClientWidth + Step > fFullWidth then
    begin
      EndUpdate;
      ClientWidth := fFullWidth;
    end
    else
      ClientWidth := ClientWidth + Step;
  end
  else
  begin
    if ClientWidth - Step < HeaderWidth then
    begin
      EndUpdate;
      ClientWidth := HeaderWidth;
    end
    else
      ClientWidth := ClientWidth - Step;
  end;
end;

procedure TdcSlidein.DoHeaderClick( Sender: TObject );
begin
  Expanded := not Expanded;
end;

procedure TdcSlidein.DoHotChanged( const AHot: boolean );
begin
  if AHot then
    Cursor := crSizeWE
  else
    Cursor := crDefault;
end;

procedure TdcSlidein.DoImagesChange( Sender: TObject );
begin
  Invalidate;
end;

procedure TdcSlidein.DoSizeChanged( const ADeltaX, ADeltaY: integer );
begin
  InvalidateRect( SplitterRect );
  if Resizing then
  begin
    if ADeltaX > 0 then
    begin
      InvalidatePadding( PanelRect, ContentPadding );
      InvalidateRect( PanelRect );
    end;
  end;
end;

procedure TdcSlidein.DoSplitterChanged;
begin
  inherited;
  RealignPanel( fPanel );
end;

procedure TdcSlidein.EndUpdate;
begin
  fUpdating := false;
  fAnimationTimer.Enabled := false;

  if not fSmooth then
    RealignPanel( fPanel );
  if Assigned( fPanel ) then
    if Expanded then
      fPanel.Show
    else
      fPanel.Hide;
end;

function TdcSlidein.Get_HeaderWidth: integer;
begin
  Result := 30;
end;

function TdcSlidein.Get_PanelRect: TRect;
begin
  Result := ClientRect;
  case SlideDirection of
    sdSlideToLeft: Inc( Result.left, fHeader.Width );
    sdSlideToRight: Inc( Result.Right, fHeader.Width );
  end;
end;

function TdcSlidein.Get_ShowSplitter: boolean;
begin
  Result := inherited Get_ShowSplitter and fExpanded and ( ( Align = alLeft ) or ( Align = alRight ) );
end;

function TdcSlidein.Get_SplitterRect: TRect;
begin
  case Align of
    alLeft: Result := TRect.Create( Point( ClientWidth - SplitterHeight, 0 ), Point( ClientWidth, ClientHeight ) );
  else Result := TRect.Create( Point( 0, 0 ), Point( SplitterHeight, ClientHeight ) );
  end;
end;

procedure TdcSlidein.Notification( AComponent: TComponent; Operation: TOperation );
begin
  inherited;
  if ( AComponent = fImages ) and ( Operation = opRemove ) then
    fImages := nil;
end;

procedure TdcSlidein.Paint;
var
  InnerRect: TRect;
begin
  inherited;
  InnerRect := ClientRect;

  case SlideDirection of
    sdSlideToLeft: InnerRect.left := fHeader.Width;
    sdSlideToRight: Dec( InnerRect.Right, fHeader.Width );
  end;

  if ShowSplitter then
  begin
    case SlideDirection of
      sdSlideToLeft: InnerRect.Width := InnerRect.Width - SplitterHeight;
      sdSlideToRight: InnerRect.Width := InnerRect.Width - SplitterHeight;
    end;
  end;

  with Canvas do
  begin
    Brush.Color := Color;
{$IFDEF DEBUG_INDICATORS}
    Brush.Color := clYellow;
{$ENDIF}
    if Assigned( fPanel ) then
      FillPadding( Canvas, Brush.Color, InnerRect, ContentPadding )
    else
      FillRect( InnerRect );
  end;
  if ShowSplitter then
    PaintSplitter;
end;

procedure TdcSlidein.ReadFullWidth( Reader: TReader );
begin
  fFullWidth := Reader.ReadInteger;
  if not fExpanded then
    Width := HeaderWidth
  else
    Width := fFullWidth;
end;

procedure TdcSlidein.RealignPanel( const Value: TWinControl );
var
  PanelBounds: TRect;
  PanelLeft: integer;
begin
  if Assigned( fPanel ) and HandleAllocated then
  begin
    PanelLeft := 0;
    case SlideDirection of
      sdSlideToLeft: PanelLeft := ClientWidth - fFullWidth + fHeader.Width;
      sdSlideToRight:
        if ShowSplitter then
          Inc( PanelLeft, SplitterHeight );
    end;

    PanelBounds := Bounds( PanelLeft, 0, fFullWidth - fHeader.Width, ClientHeight );
    PanelBounds.Inflate( fContentPadding, true );

    { Splitter Visible }
    if ShowSplitter then
      PanelBounds.Width := PanelBounds.Width - SplitterHeight;

    fPanel.BoundsRect := PanelBounds;
    fPanel.SendToBack;
  end;
end;

procedure TdcSlidein.Set_Padding( const Value: TPadding );
begin
  if Value <> fContentPadding then
  begin
    fContentPadding.Assign( Value );
    RealignPanel( fPanel );
  end;
end;

procedure TdcSlidein.Set_Expanded( const Value: boolean );
begin
  if Value <> fExpanded then
  begin
    Resizing := false;
    fExpanded := Value;
    fHeader.Expanded := fExpanded;

    if fAnimated and not( csDesigning in ComponentState ) then
    begin
      BeginUpdate;
      exit;
    end;

    if fExpanded then
    begin
      ClientWidth := fFullWidth;
    end
    else
    begin
      fFullWidth := ClientWidth;
      ClientWidth := HeaderWidth;
    end;
  end;
end;

procedure TdcSlidein.Set_FullWidth( const Value: integer );
begin
  fFullWidth := Value;
  if Expanded then
    Width := fFullWidth;
end;

procedure TdcSlidein.Set_Images( const Value: TCustomImageList );
begin
  if Value <> fImages then
  begin
    if Assigned( fImages ) then
    begin
      fImages.RemoveFreeNotification( Self );
      fImages.OnChange := nil;;
    end;
    fImages := Value;
    if Assigned( fImages ) then
    begin
      fImages.FreeNotification( Self );
      fImages.OnChange := DoImagesChange;
    end;
  end;
end;

procedure TdcSlidein.Set_Orientation( const Value: integer );
begin
  if Value <> fOrientation then
  begin
    fOrientation := Value;
    Invalidate;
  end;
end;

procedure TdcSlidein.Set_SlideDirection( const Value: TSlideDirection );
begin
  if Value <> fSlideDirection then
  begin
    fSlideDirection := Value;
    case fSlideDirection of
      sdSlideToLeft: fHeader.Align := alLeft;
      sdSlideToRight: fHeader.Align := alRight;
    end;
  end;
end;

procedure TdcSlidein.WMSize( var Message: TWMSize );
begin
  inherited;
  if Assigned( fHeader ) then
    fHeader.Width := HeaderWidth;

  if not fUpdating and Expanded then
  begin
    fFullWidth := Width;
  end;

  if Smooth or not fUpdating then
    RealignPanel( fPanel );

  if fUpdating then
    exit;

  if not Expanded then
  begin

  end
  else
  begin
    fFullWidth := Width;
  end;
end;

procedure TdcSlidein.WriteFullWidth( Writer: TWriter );
begin
  Writer.WriteInteger( fFullWidth );
end;

{ TdcSlideinHeader }

procedure TdcSlideinHeader.CMMouseEnter( var Message: TMessage );
begin
  State := State + [btHot];
end;

procedure TdcSlideinHeader.CMMouseLeave( var Message: TMessage );
begin
  State := State - [btHot];
end;

procedure TdcSlideinHeader.CMTextChanged( var Message: TMessage );
begin
  Invalidate;
end;

constructor TdcSlideinHeader.Create( AOwner: TComponent );
begin
  inherited;
  ControlStyle := ControlStyle + [csOpaque];
  fPushed := false;
  fExpanded := true;
  fFadeTimer := TTimer.Create( Self );
  fFadeTimer.Enabled := false;
  fFadeTimer.Interval := 1;
  fFadeTimer.OnTimer := DoFadeTimer;
  fHeaderAlpha := 0;
  fState := [];
  if csDesigning in ComponentState then
    Color := clSlideinBkHot;
  Cursor := crHandPoint;
  LoadGlyphs;
end;

destructor TdcSlideinHeader.Destroy;
begin
  fGlyphs[false].Free;
  fGlyphs[true].Free;
  inherited;
end;

procedure TdcSlideinHeader.DoFadeTimer( Sender: TObject );
begin
  if btHot in fState then
    HeaderAlpha := HeaderAlpha + 12
  else
    HeaderAlpha := HeaderAlpha - 12;
end;

function TdcSlideinHeader.Get_ToggleRect: TRect;
var
  GlyphIndex: boolean;
  Slidein: TdcSlidein;
begin
  Slidein := TdcSlidein( Parent );
  if Slidein.SlideDirection = sdSlideToRight then
    GlyphIndex := not fExpanded
  else
    GlyphIndex := fExpanded;
  Result := ClientRect;
  Result.Offset( 0, intToggleOffset );
  Result.Size := TSize.Create( fGlyphs[GlyphIndex].Width, fGlyphs[GlyphIndex].Height );
  Result.Offset( Width div 2 - Result.Width div 2, 0 );
end;

procedure TdcSlideinHeader.LoadGlyphs;
begin
  fGlyphs[false] := TPngImage.Create;
  fGlyphs[false].LoadFromRHResourceName( HInstance, 'CHEVRONRIGHT' );
  fGlyphs[true] := TPngImage.Create;
  fGlyphs[true].LoadFromRHResourceName( HInstance, 'CHEVRONLEFT' );
end;

procedure TdcSlideinHeader.MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: integer );
begin
  inherited;
  if Button = mbLeft then
    fPushed := true;
end;

procedure TdcSlideinHeader.MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: integer );
begin
  inherited;
  if ( Button = mbLeft ) and fPushed then
    if Assigned( fOnToggle ) then
      fOnToggle( Self );
  fPushed := false;
end;

procedure TdcSlideinHeader.Paint;
var
  HeaderRect, ToggleRect: TRect;
  HeaderText: string;
  headerSize: TSize;
  Buffer: TBitmap;
begin
  Buffer := TBitmap.Create;
  try
    Buffer.Width := Width;
    Buffer.Height := Height;

    with Buffer.Canvas do
    begin
      Font.Assign( Self.Font );
      headerSize := Buffer.Canvas.GetTextSize( Caption );

      HeaderRect := ClientRect;

      Brush.Color := BlendColor( clSlideinBkHot, Color, fHeaderAlpha );
      FillRect( ClientRect );

      { Toggle }
      ToggleRect := Self.ToggleRect;

      Draw( ToggleRect.left, ToggleRect.Top, fGlyphs[fExpanded] );

      Font.Orientation := 900;
      HeaderText := Caption;
      Brush.Style := bsClear;
      HeaderRect.Offset( headerSize.cy div 2, headerSize.cx + ToggleRect.Bottom + intToggleToCaption );
      TextRect( HeaderRect, HeaderText, [tfSingleLine, tfTop, tfNoClip] );
      Brush.Style := bsSolid;
      Font.Orientation := 0;

    end;
    Canvas.Draw( 0, 0, Buffer );
  finally
    Buffer.Free;
  end;
end;

procedure TdcSlideinHeader.Set_Expanded( const Value: boolean );
begin
  if Value <> fExpanded then
  begin
    fExpanded := Value;
    InvalidateRect( Handle, ToggleRect, false );
  end;
end;

procedure TdcSlideinHeader.Set_HeaderAlpha( const Value: integer );
begin
  if Value <> fHeaderAlpha then
  begin
    fHeaderAlpha := Value;
    if fHeaderAlpha > 255 then
    begin
      fHeaderAlpha := 255;
      fFadeTimer.Enabled := false;
    end
    else if fHeaderAlpha < 0 then
    begin
      fHeaderAlpha := 0;
      fFadeTimer.Enabled := false;
    end;
    Invalidate;
  end;
end;

procedure TdcSlideinHeader.Set_State( const Value: TdcButtonState );
var
  HotChanged: boolean;
begin
  if Value <> fState then
  begin
    HotChanged := btHot in fState;
    fState := Value;
    if btHot in fState then
      Cursor := crHandPoint
    else
      Cursor := crDefault;
    if HotChanged <> ( btHot in fState ) then
      fFadeTimer.Enabled := true;
  end;
end;

{ TdcToobar }
{$HINTS OFF}

procedure TdcToolbar.AlignControls( AControl: TControl; var Rect: TRect );
begin
  inherited;
  if AControl is TdcToolButton then
    Rect := GetButtonRect( TdcToolButton( AControl ) );
end;

procedure TdcToolbar.CMControlChange( var Message: TCMControlChange );
begin
  inherited;
  with message do
    if Inserting then
      InsertButton( Control )
    else
      RemoveButton( Control );
end;

constructor TdcToolbar.Create( AOwner: TComponent );
begin
  inherited;
  ControlStyle := [csAcceptsControls, csCaptureMouse, csClickEvents, csDoubleClicks, csMenuEvents, csSetCaption, csGestures];
  fButtons := TList<TdcToolButton>.Create;
  fButtonHeight := 22;
  fButtonWidth := 23;
  fImages := nil;
  fShowCaptions := false;
  Align := alTop;
  Color := clStatusBarBk;
end;

destructor TdcToolbar.Destroy;
begin
  fButtons.Free;
  inherited;
end;

function TdcToolbar.GetButtonRect( AButton: TdcToolButton ): TRect;
var
  i, X: integer;
  Btn: TdcToolButton;
begin
  X := Padding.left;

  for Btn in fButtons do
  begin
    if Btn = AButton then
    begin
      Result := TRect.Create( X, Padding.Top, X + Btn.Width, Padding.Top + Btn.Height );
      exit;
    end;
    Inc( X, Btn.Width );
  end;
end;

function TdcToolbar.Get_Button( Index: integer ): TdcToolButton;
begin
  Result := TdcToolButton( fButtons[index] );
end;

function TdcToolbar.Get_ButtonCount: integer;
begin
  Result := fButtons.Count;
end;

function TdcToolbar.IndexOf( AButton: TdcToolButton ): integer;
begin
  Result := fButtons.IndexOf( AButton as TdcToolButton );
end;

procedure TdcToolbar.InsertButton( Control: TControl );
var
  Index, ToIndex: integer;
  ToolButton: TdcToolButton;
begin
  if Control is TdcToolButton then
  begin
    ToolButton := TdcToolButton( Control );
    ToolButton.ParentColor := true;
    ToolButton.Flat := true;
    ToolButton.fToolbar := Self;
    ToolButton.ShowCaption := ShowCaptions;
    ToolButton.Caption := ToolButton.Index.ToString( );
    ToolButton.Style := bcLight;
    ToolButton.Layout := blGlyphTop;
    ToolButton.Height := ButtonHeight;
    ToolButton.Width := ButtonWidth;
    ToolButton.Images := Images;
  end
  else
    exit;

  if not( csLoading in Control.ComponentState ) then
  begin
    // index := fButtons.IndexOf( Control );

    // if FromIndex >= 0 then
    // ToIndex := ReorderButton( FromIndex, Control.Left, Control.Top )
  end
  else
  begin
    // ToIndex := ButtonIndex( FromIndex, Control.Left, Control.Top );
    ToIndex := ButtonCount;
    fButtons.Insert( ToIndex, ToolButton );
    // ToolButton.BoundsRect := GetButtonRect( ToolButton );

    ReorderButton( ToolButton, ToIndex );
  end;
  // end
  // else
  // begin
  // ToIndex := FButtons.Add(Control);
  // UpdateButton(ToIndex);
  // end;
  // if Wrapable then
  // RepositionButtons(0)
  // else
  // RepositionButtons(ToIndex);
  // RecreateButtons;

end;

procedure TdcToolbar.Paint;
begin
  inherited;

end;

procedure TdcToolbar.RemoveButton( Button: TControl );
begin

end;

procedure TdcToolbar.ReorderButton( Button: TdcToolButton; ToIndex: integer );
var
  i, X: integer;
  Btn: TdcToolButton;
begin
  X := Padding.left;

  for i := 0 to ToIndex - 1 do
  begin
    Btn := Buttons[i];
    Inc( X, Btn.Width );
  end;
  Button.left := X;
end;

procedure TdcToolbar.Set_Images( const Value: TCustomImageList );
begin
  if Value <> fImages then
  begin
    fImages := Value;
    UpdateButtons;
  end;
end;

procedure TdcToolbar.Set_ShowCaptions( const Value: boolean );
begin
  if Value <> fShowCaptions then
  begin
    fShowCaptions := Value;
    UpdateButtons;
  end;
end;

procedure TdcToolbar.UpdateButtons;
var
  i: integer;
  Button: TdcToolButton;
begin
  for Button in fButtons do
  begin
    Button.Images := Images;
    Button.ShowCaption := ShowCaptions;
  end;
end;

procedure TdcToolbar.Set_ButtonHeight( const Value: integer );
begin
  if Value <> fButtonHeight then
  begin
    fButtonHeight := Value;
    UpdateButtons;
  end;
end;

procedure TdcToolbar.Set_ButtonWidth( const Value: integer );
begin
  if Value <> fButtonWidth then
  begin
    fButtonWidth := Value;
    UpdateButtons;
  end;
end;

{ TdcToolButton }

constructor TdcToolButton.Create( AOwner: TComponent );
begin
  inherited;
  fToolbar := nil;
  Cursor := crHandPoint;
  Height := 22;
  Width := 23;
end;

function TdcToolButton.Get_Index: integer;
begin
  if Assigned( fToolbar ) then
    Result := fToolbar.IndexOf( Self )
  else
    Result := -1;
end;

procedure TdcToolButton.Paint;
begin
  inherited;

end;

procedure TdcToolButton.SetBounds( ALeft, ATop, AWidth, AHeight: integer );
begin
  if ( ( ALeft <> left ) or ( ATop <> Top ) or ( AWidth <> Width ) or ( AHeight <> Height ) ) and not( csLoading in ComponentState ) and ( fToolbar <> nil ) then
  begin
    fToolbar.ReorderButton( Self, index );
  end
  else
    inherited SetBounds( ALeft, ATop, AWidth, AHeight );
end;

procedure TdcToolButton.ValidateContainer( AComponent: TComponent );
begin
  inherited;
  if ( csLoading in ComponentState ) and ( AComponent is TdcToolbar ) then
  begin
    with AComponent as TdcToolbar do
    begin
      SetBounds( left, Top, ButtonWidth, ButtonHeight );
    end;
  end;
end;
{$HINTS ON}
{ TdcInlineAlertPresenter }

constructor TdcInlineAlertPresenter.Create( AOwner: TComponent );
begin
  inherited;
  fHeaderSpacing := 4;
  fHeaderText := '';
  fLines := TStringList.Create;
  TStringList( fLines ).OnChange := DoLinesChange;
  fShowHeader := true;
  fShowIcon := true;
end;

procedure TdcInlineAlertPresenter.DoLinesChange( Sender: TObject );
begin
  Invalidate;
end;

procedure TdcInlineAlertPresenter.Paint;
var
  // Y: integer;
  HeaderRect, contentRect: TRect;
  HeaderText: string;
begin
  inherited;
  contentRect := ClientRect;
  contentRect.Inflate( -Padding.left, -Padding.Top, -Padding.Right, -Padding.Bottom );
  with Canvas do
  begin
    Font.Assign( Self.Font );
    if ShowIcon then
    begin
      contentRect.left := 50;
    end;
    HeaderRect := contentRect;
    case PanelKind of
      pkInfo: HeaderText := ALERT_HEADER_INFO;
      pkWarning: HeaderText := ALERT_HEADER_WARNING;
      pkError: HeaderText := ALERT_HEADER_ERROR;
    else HeaderText := fHeaderText;
    end;
    HeaderRect.Height := GetTextSize( HeaderText ).cy;
    Font.Size := Font.Size + 1;
    Font.Style := [fsBold];
    RenderText( HeaderRect, HeaderText, [tfLeft, tfTop, tfSingleLine] );
    // Inc( Y, HeaderRect.Height + HeaderSpacing );
  end;
end;

procedure TdcInlineAlertPresenter.Set_HeaderSpacing( const Value: integer );
begin
  if Value <> fHeaderSpacing then
  begin
    fHeaderSpacing := Value;
    Invalidate;
  end;
end;

procedure TdcInlineAlertPresenter.Set_HeaderText( const Value: string );
begin
  if Value <> fHeaderText then
  begin
    fHeaderText := Value;
    Invalidate;
  end;
end;

procedure TdcInlineAlertPresenter.Set_Lines( const Value: TStrings );
begin
  fLines.Assign( Value );
end;

procedure TdcInlineAlertPresenter.Set_ShowHeader( const Value: boolean );
begin
  if Value <> fShowHeader then
  begin
    fShowHeader := Value;
    Invalidate;
  end;
end;

procedure TdcInlineAlertPresenter.Set_ShowIcon( const Value: boolean );
begin
  if Value <> fShowIcon then
  begin
    fShowIcon := Value;
    Invalidate;
  end;
end;

{ TdcRevealExpander }

procedure TdcRevealExpander.AdjustClientRect( var Rect: TRect );
begin
  inherited;
  Inc( Rect.Top, intRevealExpanderHeight );
end;

procedure TdcRevealExpander.AlignControl( AControl: TControl );
begin

end;

procedure TdcRevealExpander.CMControlChange( var Message: TCMControlChange );
begin
  inherited;
  with message do
    if Inserting then
      AlignControl( Control );
end;

constructor TdcRevealExpander.Create( AOwner: TComponent );
begin
  inherited;
  ControlStyle := ControlStyle + [csAcceptsControls, csSetCaption];
  fExpanded := true;
  fHeaderHeight := intRevealExpanderHeight;
  fPressed := false;
  Height := 128;
  Width := 128;
end;

procedure TdcRevealExpander.CreateParams( var Params: TCreateParams );
begin
  inherited CreateParams( Params );
  with Params do
  begin
    with WindowClass do
      Style := Style or CS_HREDRAW or CS_VREDRAW;
  end;
end;

procedure TdcRevealExpander.DefineProperties( Filer: TFiler );
begin
  inherited;
  Filer.DefineProperty( 'FullHeight', ReadFullHeight, WriteFullHeight, true );
end;

function TdcRevealExpander.GetHeaderRect: TRect;
begin
  Result := ClientRect;
  Result.Height := intRevealExpanderHeight;
end;

function TdcRevealExpander.GetToggleButtonRect: TRect;
begin
  Result := TRect.Create( TPoint.Zero, GetToggleButtonSize.cx, GetToggleButtonSize.cy );
  Result.Offset( 5, GetHeaderRect.Height div 2 - GetToggleButtonSize.cy div 2 );
end;

function TdcRevealExpander.GetToggleButtonSize: TSize;
begin
  if Expanded then
    Result := TSize.Create( 6, 6 )
  else
    Result := TSize.Create( 5, 9 );
end;

procedure TdcRevealExpander.InvalidateHeader;
begin
  InvalidateRect( Handle, GetHeaderRect, false );
end;

procedure TdcRevealExpander.MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: integer );
begin
  inherited;
  fPressed := true;
end;

procedure TdcRevealExpander.MouseMove( Shift: TShiftState; X, Y: integer );
begin
  inherited;

end;

procedure TdcRevealExpander.MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: integer );
begin
  inherited;
  if fPressed then
    ToggleExpanded;
  fPressed := false;
end;

procedure TdcRevealExpander.Paint;
begin
  inherited;
  Canvas.Font.Assign( Font );
  PaintHeader( GetHeaderRect );

  if Expanded then
  begin

  end;
end;

procedure TdcRevealExpander.PaintHeader( const AHeaderRect: TRect );
var
  CaptionRect: TRect;
begin
  with Canvas do
  begin
    Brush.Color := $00E6E3DB;
    FillRect( AHeaderRect );
    CaptionRect := AHeaderRect;
    CaptionRect.left := 24;
    Canvas.Font.Style := [fsBold];
    Canvas.RenderText( CaptionRect, Caption, [tfLeft, tfSingleLine, tfVerticalCenter, tfEndEllipsis] );
  end;
end;

procedure TdcRevealExpander.ReadFullHeight( Reader: TReader );
begin
  fFullHeight := Reader.ReadInteger;
  if not fExpanded then
  begin
    Height := fHeaderHeight;
  end
  else
    Height := fFullHeight;
end;

procedure TdcRevealExpander.Set_Expanded( const Value: boolean );
begin
  if Value <> fExpanded then
  begin
    fExpanded := Value;

    InvalidateHeader;

    if fExpanded then
    begin
      ClientHeight := fFullHeight;
    end
    else
    begin
      fFullHeight := ClientHeight;
      ClientHeight := fHeaderHeight;
    end;
  end;
end;

procedure TdcRevealExpander.Set_HeaderHeight( const Value: integer );
begin
  if Value <> fHeaderHeight then
  begin
    fHeaderHeight := Value;
    Invalidate;
    Realign;
  end;
end;

procedure TdcRevealExpander.ToggleExpanded;
begin
  Expanded := not Expanded;
end;

procedure TdcRevealExpander.WMSize( var Message: TWMSize );
var
  OldHeaderHeight: integer;
begin
  inherited;
  if not Expanded then
  begin
    OldHeaderHeight := HeaderHeight;
    HeaderHeight := ClientHeight;
    Inc( fFullHeight, HeaderHeight - OldHeaderHeight );
  end
  else
  begin
    fFullHeight := ClientHeight;
  end;
end;

procedure TdcRevealExpander.WriteFullHeight( Writer: TWriter );
begin
  Writer.WriteInteger( fFullHeight );
end;

{ TdcImageList }

procedure TdcImageList.Draw( Canvas: TCanvas; X, Y, Index: integer; ADrawingStyle: TDrawingStyle; AImageType: TImageType; Enabled: boolean );
begin
  inherited;
end;

end.
