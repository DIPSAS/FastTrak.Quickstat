unit Emetra.Vcl.Buttons;

interface

uses
  System.Classes, System.Types, System.SysUtils,
  {Winapi}
  Winapi.Windows, Winapi.Messages,
  {VCL}
  Vcl.Controls, Vcl.Graphics, Vcl.Buttons, Vcl.ImgList, System.UITypes, Vcl.ComCtrls, Vcl.ExtCtrls, Vcl.Imaging.pngimage,
  {Emetra.Vcl}
  Emetra.Vcl.Controls,
  Emetra.Vcl.Consts,
  Emetra.Vcl.Types,
  Emetra.Vcl.Intf,
  Emetra.Vcl.Glyphs,
  Emetra.Vcl.Tooltip;

const
  DELAY_INTERVAL  = 400;
  REPEAT_INTERVAL = 20;

type
  TdcButtonKind = ( btkCustom, btkPlus, btkCancel, btkClear, btkMinus, btkListRemove, btkOk, btkRefresh, btkSearch );

  { TdcStateButton }

  TdcStateButton = class( TGraphicControl )
  strict private
    fAutoCheck: boolean;
    fChecked: boolean;
    fOnCheckedChange: TNotifyEvent;
    fState: TdcButtonState;
    function GetGlyphState: TdcGlyphState;
    procedure Set_Checked( const Value: boolean );
    procedure Set_State( const Value: TdcButtonState );
  protected
    procedure DoCheckedChange; dynamic;
    { Delphi Messages }
    procedure CMEnabledChanged( var Message: TMessage ); message CM_ENABLEDCHANGED;
    procedure CMMouseEnter( var Message: TMessage ); message CM_MOUSEENTER;
    procedure CMMouseLeave( var Message: TMessage ); message CM_MOUSELEAVE;
    procedure CMTextChanged( var Message: TMessage ); message CM_TEXTCHANGED;
    { Virtual Methods }
    procedure MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: Integer ); override;
    procedure MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: Integer ); override;
    { Winapi Methods }
    procedure WndProc( var Message: TMessage ); override;
    { Winapi Messages }
    procedure WMActivateApp( var Message: TWMActivateApp ); message WM_ACTIVATEAPP;
    { Properties }
    property GlyphState: TdcGlyphState read GetGlyphState;
    property State: TdcButtonState read fState write Set_State;
  public
    constructor Create( AOwner: TComponent ); override;
  published
    { Properties }
    property Action;
    property AutoCheck: boolean read fAutoCheck write fAutoCheck default false;
    property Cursor default crHandPoint;
    property Checked: boolean read fChecked write Set_Checked default false;
    property OnCanResize;
    property OnCheckedChange: TNotifyEvent read fOnCheckedChange write fOnCheckedChange;
    property OnClick;
    property OnConstrainedResize;
    property OnContextPopup;
    property OnDblClick;
    property OnDragDrop;
    property OnDragOver;
    property OnEndDock;
    property OnEndDrag;
    property OnMouseActivate;
    property OnMouseDown;
    property OnMouseEnter;
    property OnMouseLeave;
    property OnMouseMove;
    property OnMouseUp;
    property OnMouseWheel;
    property OnMouseWheelDown;
    property OnMouseWheelUp;
    property OnResize;
    property OnStartDock;
    property OnStartDrag;
  end;

  { TdcGraphicButton }

  TOwnerDrawEvent = procedure( Sender: TObject; Canvas: TCanvas ) of object;

  TdcGraphicButton = class( TGraphicControl )
  private
    fDefault: boolean;
    fOwnerDraw: TOwnerDrawEvent;
    fState: TdcButtonState;
    fTransparentColor: boolean;
    procedure SetState( const Value: TdcButtonState );
    procedure SetDefault( const Value: boolean );
    procedure SetTransparentColor( const Value: boolean );
  protected
    procedure DoOwnerDraw( Canvas: TCanvas ); dynamic;
    { Delphi Messages }
    procedure CMMouseEnter( var Message: TMessage ); message CM_MOUSEENTER;
    procedure CMMouseLeave( var Message: TMessage ); message CM_MOUSELEAVE;
    procedure WMEraseBkgnd( var Message: TWmEraseBkgnd ); message WM_ERASEBKGND;
    { Virtual Methods }
    procedure MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: Integer ); override;
    procedure MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: Integer ); override;
    procedure Paint; override;
    procedure WndProc( var Message: TMessage ); override;
    { Properties }
    property State: TdcButtonState read fState write SetState;
  public
    constructor Create( AOwner: TComponent ); override;
  published
    { Properties }
    property Align;
    property Anchors;
    property default: boolean read fDefault write SetDefault default false;
    property Caption;
    property Color;
    property Constraints;
    property Cursor;
    property Font;
    property ParentColor;
    property TransparentColor: boolean read fTransparentColor write SetTransparentColor default false;
    property OnClick;
    property OnOwnerDraw: TOwnerDrawEvent read fOwnerDraw write fOwnerDraw;
  end;

  { TdcToggleButton }

  TdcToggleButton = class( TdcStateButton )
  private
    fImages: TCustomImageList;
    procedure Set_Images( const Value: TCustomImageList );
  protected
  published
    property Images: TCustomImageList read fImages write Set_Images;
  end;

  { TdcEditButton }

  TdcEditButton = class( TCustomControl, IWinControl, IEditButton )
  private
    { Property Fields }
    FAutoUp: boolean;
    FCaption: string;
    FDown: boolean;
    FEdit: IInplaceEdit;
    FHover: boolean;
    FMouseDown: boolean;
    FOnButtonClick: TNotifyEvent;
    FOnDown: TNotifyEvent;
    FOnUp: TNotifyEvent;
    { Property Accessors }
    function GetAlign: TAlign;
    function GetAutoUp: boolean;
    function GetBoundsRect: TRect;
    function GetCaption: string;
    function GetDown: boolean;
    function GetEdit: IInplaceEdit;
    function GetHandle: HWND;
    function GetHover: boolean;
    function GetLeft: Integer;
    function GetOnButtonClick: TNotifyEvent;
    function GetOnDown: TNotifyEvent;
    function GetOnUp: TNotifyEvent;
    function GetParent: TWinControl;
    function GetVisible: boolean;
    function GetWidth: Integer;
    function Get_State: TdcButtonState;
    procedure SetAlign( const Value: TAlign );
    procedure SetAutoUp( const Value: boolean );
    procedure SetCaption( const Value: string );
    procedure SetDown( const Value: boolean );
    procedure SetEdit( const Value: IInplaceEdit );
    procedure SetHover( const Value: boolean );
    procedure SetLeft( const Value: Integer );
    procedure SetOnButtonClick( const Value: TNotifyEvent );
    procedure SetOnDown( const Value: TNotifyEvent );
    procedure SetOnUp( const Value: TNotifyEvent );
    procedure SetVisible( const Value: boolean );
    procedure SetWidth( const Value: Integer );
  protected
    procedure DoButtonClick; dynamic;
    procedure DoDown; dynamic;
    procedure DoUp; dynamic;
    { Mouse & Keyboard Actions }
    procedure MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: Integer ); override;
    procedure MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: Integer ); override;
    { Painting }
    procedure Paint; override;
    procedure PaintBackground; virtual; abstract;
    { Virtual Methods }
    function CanAutoSize( var NewWidth, NewHeight: Integer ): boolean; override;
    procedure SetParent( AParent: TWinControl ); override;
    { Delphi Messages }
    procedure CMMouseLeave( var Message: TMessage ); message CM_MOUSELEAVE;
    procedure CMMouseEnter( var Message: TMessage ); message CM_MOUSEENTER;
    { Windows Messages }
    procedure WMEraseBkgnd( var Message: TWmEraseBkgnd ); message WM_ERASEBKGND;
  public
    constructor Create( AOwner: TComponent ); override;
    { Properties }
    property AutoUp: boolean read GetAutoUp write SetAutoUp;
    property Down: boolean read GetDown write SetDown;
    property Edit: IInplaceEdit read GetEdit write SetEdit;
    property Handle: HWND read GetHandle;
    property Hover: boolean read GetHover write SetHover;
    property Parent: TWinControl read GetParent write SetParent;
    property State: TdcButtonState read Get_State;
    { Events }
    property OnButtonClick: TNotifyEvent read GetOnButtonClick write SetOnButtonClick;
    property OnDown: TNotifyEvent read GetOnDown write SetOnDown;
    property OnUp: TNotifyEvent read GetOnUp write SetOnUp;
  published
    { Properties }
    property Action;
    property Align;
    property Anchors;
    property AutoSize;
    property Caption: string read GetCaption write SetCaption;
    property Color;
    property Constraints;
    property Cursor default crHandPoint;
    property ParentColor;
    property ParentShowHint;
    property Visible: boolean read GetVisible write SetVisible;
    property Width: Integer read GetWidth write SetWidth;
    { Events }
    property OnClick;
    property OnContextPopup;
    property OnDblClick;
    property OnDragDrop;
    property OnDragOver;
    property OnEndDock;
    property OnEndDrag;
    property OnEnter;
    property OnExit;
    property OnKeyDown;
    property OnKeyPress;
    property OnKeyUp;
    property OnMouseDown;
    property OnMouseMove;
    property OnMouseUp;
    property OnStartDock;
    property OnStartDrag;
  end;

  { TdcDropDownButton }

  TdcDropDownButton = class( TdcEditButton )
  protected
    procedure PaintBackground; override;
  public
    constructor Create( AOwner: TComponent ); override;
  end;

  { TdcExecuteButton }

  TdcButtonGlyphKind = ( gkCustom, gkClose, gkPlus, gkMinus );

  IExecuteButton = interface
    ['{92CF3F17-0593-46AD-9225-324BFA4CDBD1}']
    procedure Set_Glyph( const Value: TGlyphLocator );
    procedure Set_GlyphKind( const Value: TdcButtonGlyphKind );
    procedure Set_ImageIndex( const Value: TImageIndex );
    procedure Set_Images( const Value: TCustomImageList );
    property Glyph: TGlyphLocator write Set_Glyph;
    property GlyphKind: TdcButtonGlyphKind write Set_GlyphKind;
    property ImageIndex: TImageIndex write Set_ImageIndex;
    property Images: TCustomImageList write Set_Images;
  end;

  TdcExecuteButton = class( TdcEditButton, IExecuteButton )
  private
    FDisabledImages: TCustomImageList;
    FGlyph: TGlyphLocator;
    FGlyphKind: TdcButtonGlyphKind;
    FImageIndex: TImageIndex;
    fImages: TCustomImageList;
    procedure Set_DisabledImages( const Value: TCustomImageList );
    procedure Set_Glyph( const Value: TGlyphLocator );
    procedure Set_GlyphKind( const Value: TdcButtonGlyphKind );
    procedure Set_ImageIndex( const Value: TImageIndex );
    procedure Set_Images( const Value: TCustomImageList );
  protected
    procedure DoGlyphChange( Sender: TObject );
    procedure Paint; override;
    procedure PaintBackground; override;
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
    { Static Methods }
    class procedure DrawCloseSign( Canvas: TCanvas; const X, Y: Integer );
    class procedure DrawMinusGlyph( Canvas: TCanvas; const X, Y: Integer );
    class procedure DrawPlusGlyph( Canvas: TCanvas; const X, Y: Integer );
    class procedure PaintInRect( Canvas: TCanvas; Dest: TRect; AButtonState: TdcButtonState; AGlyphKind: TdcButtonGlyphKind; Glyph: TGraphic; Images, DisabledImages: TCustomImageList; ImageIndex: TImageIndex; Enabled: boolean;
      Text: string );
  published
    { Properties }
    property Color;
    property Constraints;
    property DisabledImages: TCustomImageList read FDisabledImages write Set_DisabledImages;
    property DragCursor;
    property DragKind;
    property DragMode;
    property Enabled;
    property Font;
    property Glyph: TGlyphLocator read FGlyph write Set_Glyph;
    property GlyphKind: TdcButtonGlyphKind read FGlyphKind write Set_GlyphKind default gkCustom;
    property ImageIndex: TImageIndex read FImageIndex write Set_ImageIndex default -1;
    property Images: TCustomImageList read fImages write Set_Images;
    property ParentFont;
    property ParentShowHint;
    property ShowHint;
    property Visible;
  end;

  { TdcRepeatButton }

  TdcRepeatButton = class( TdcExecuteButton )
  private
    FDelayTimer: TTimer;
    FRepeatTimer: TTimer;
  protected
    procedure DoDelayTimer( Sender: TObject );
    procedure DoRepeatTimer( Sender: TObject );
    procedure MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: Integer ); override;
    procedure MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: Integer ); override;
    { Winapi Messages }
    procedure WMActivateApp( var Message: TWMActivateApp ); message WM_ACTIVATEAPP;
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
  end;

  { TdcCustomSpeedButton }

  TdcButtonColorStyle = ( bcDefault, bcLight, bcDark );

  TdcCustomSpeedButton = class( TdcStateButton )
  private
    fArrowSpacing: Integer;
    fAutoSizePadding: Integer;
    FDisabledImages: TCustomImageList;
    FDown: boolean;
    fFlat: boolean;
    FGlyph: TPicture;
    fGlyphLocator: TGlyphLocator;
    FImageIndex: TImageIndex;
    fImages: TCustomImageList;
    fLayout: TButtonLayout;
    fShowArrow: boolean;
    fShowCaption: boolean;
    fStyle: TdcButtonColorStyle;
    fTransparent: boolean;
    function Get_ArrowSize: TSize;
    function Get_CaptionSize: TSize;
    function Get_ImageAndCaptionSize: TSize;
    function Get_ImageSize: TSize;
    procedure Set_ArrowSpacing( const Value: Integer );
    procedure Set_DisabledImages( const Value: TCustomImageList );
    procedure Set_Down( const Value: boolean );
    procedure Set_Flat( const Value: boolean );
    procedure Set_Glyph( const Value: TPicture );
    procedure Set_ImageIndex( const Value: TImageIndex );
    procedure Set_Images( const Value: TCustomImageList );
    procedure Set_Layout( const Value: TButtonLayout );
    procedure Set_ShowArrow( const Value: boolean );
    procedure Set_ShowCaption( const Value: boolean );
    procedure Set_Style( const Value: TdcButtonColorStyle );
    procedure Set_Transparent( const Value: boolean );
    procedure Set_GlyphLocator( const Value: TGlyphLocator );
  protected
    procedure AdjustFontColor( const ACanvas: TCanvas ); virtual;
    function CanAutoSize( var NewWidth, NewHeight: Integer ): boolean; override;
    function HasGlyph: boolean;
    function PaintGlyph( Canvas: TCanvas; Location: TPoint ): TSize; virtual;
    procedure DoGlyphChange( Sender: TObject );
    procedure Paint; override;
    procedure PaintBackground( ACanvas: TCanvas ); virtual;
    procedure PaintContent( ACanvas: TCanvas );
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
    { Properties }
    property ArrowSize: TSize read Get_ArrowSize;
    property ArrowSpacing: Integer read fArrowSpacing write Set_ArrowSpacing default DistanceDouple;
    property CaptionSize: TSize read Get_CaptionSize;
    property DisabledImages: TCustomImageList read FDisabledImages write Set_DisabledImages;
    property Flat: boolean read fFlat write Set_Flat default true;
    property ImageAndCaptionSize: TSize read Get_ImageAndCaptionSize;
    property Images: TCustomImageList read fImages write Set_Images;
    property ImageSize: TSize read Get_ImageSize;
    property Layout: TButtonLayout read fLayout write Set_Layout default blGlyphLeft;
    property ShowArrow: boolean read fShowArrow write Set_ShowArrow default false;
    property ShowCaption: boolean read fShowCaption write Set_ShowCaption default true;
    property Style: TdcButtonColorStyle read fStyle write Set_Style default bcDefault;
    property Transparent: boolean read fTransparent write Set_Transparent default true;
  published
    property AutoSizePadding: Integer read fAutoSizePadding write fAutoSizePadding default 8;
    property Caption;
    property Down: boolean read FDown write Set_Down default false;
    property Glyph: TPicture read FGlyph write Set_Glyph;
    property GlyphLocator: TGlyphLocator read fGlyphLocator write Set_GlyphLocator;
    property Height default 30;
    property ImageIndex: TImageIndex read FImageIndex write Set_ImageIndex default -1;
    property ParentShowHint;
    property ShowHint;
    property Width default 30;
  end;

  { TdcSpeedButton }

  TdcSpeedButton = class( TdcCustomSpeedButton )
  published
    property Align;
    property Anchors;
    property AutoSize;
    property Color;
    property Constraints;
    property DisabledImages;
    property DragCursor;
    property DragKind;
    property DragMode;
    property Enabled;
    property Flat;
    property Font;
    property Images;
    property ParentColor default false;
    property ParentFont;
    property ParentShowHint;
    property ShowCaption;
    property ShowHint;
    property Style;
    property Transparent;
    property Visible;
  end;

  { TdcButton }

  TdcButtonStyle = ( bsStandard, bsFlat );

  TdcButton = class( TCustomControl )
  private
    fActive: boolean;
    fArrowSpacing: Integer;
    fCancel: boolean;
    fDefault: boolean;
    fDisabledGlyph: TPicture;
    FDisabledImages: TCustomImageList;
    FDown: boolean;
    fEnableDblClick: boolean;
    FGlyph: TPicture;
    fGlyphSpacing: Integer;
    fGlyphLocator: TGlyphLocator;
    FImageIndex: TImageIndex;
    fImages: TCustomImageList;
    fLayout: TButtonLayout;
    fModalResult: TModalResult;
    fPushedImages: TCustomImageList;
    fShowArrow: boolean;
    fState: TdcButtonState;
    fTransparent: boolean;
    fTransparentColor: TColor;
    { Property Accessories }
    function GetGlyphState: TdcGlyphState;
    function GetIconSize: TSize;
    procedure Set_ArrowSpacing( const Value: Integer );
    procedure Set_Default( const Value: boolean );
    procedure Set_DisabledGlyph( const Value: TPicture );
    procedure Set_DisabledImages( const Value: TCustomImageList );
    procedure Set_Down( const Value: boolean );
    procedure Set_EnableDblClick( const Value: boolean );
    procedure Set_Glyph( const Value: TPicture );
    procedure Set_GlyphLocator( const Value: TGlyphLocator );
    procedure Set_GlyphSpacing( const Value: Integer );
    procedure Set_ImageIndex( const Value: TImageIndex );
    procedure Set_Images( const Value: TCustomImageList );
    procedure Set_Layout( const Value: TButtonLayout );
    procedure Set_PushedImages( const Value: TCustomImageList );
    procedure Set_ShowArrow( const Value: boolean );
    procedure Set_State( const Value: TdcButtonState );
    procedure Set_Transparent( const Value: boolean );
    procedure Set_TransparentColor( const Value: TColor );
  protected
    procedure CreateWnd; override;
    { Methods }
    procedure AdjustFontColor( const ACanvas: TCanvas ); virtual;
    function CanAutoSize( var NewWidth, NewHeight: Integer ): boolean; override;
    procedure DoGlyphChange( Sender: TObject );
    function GetArrowSize: TSize; virtual;
    procedure MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: Integer ); override;
    procedure MouseMove( Shift: TShiftState; X, Y: Integer ); override;
    procedure MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: Integer ); override;
    procedure Notification( AComponent: TComponent; Operation: TOperation ); override;
    procedure Paint; override;
    procedure PaintBackground( const ACanvas: TCanvas; const ButtonRect: TRect; const ButtonState: TdcButtonState ); virtual;
    procedure PaintContent( Canvas: TCanvas );
    procedure PaintIcon( Canvas: TCanvas; Location: TPoint );
    { Delphi Messages }
    procedure CMDialogChar( var Message: TCMDialogChar ); message CM_DIALOGCHAR;
    procedure CMDialogKey( var Message: TCMDialogKey ); message CM_DIALOGKEY;
    procedure CMEnabledChanged( var Message: TMessage ); message CM_ENABLEDCHANGED;
    procedure CMEnter( var Message: TCMEnter ); message CM_ENTER;
    procedure CMExit( var Message: TCMExit ); message CM_EXIT;
    procedure CMFocusChanged( var Message: TCMFocusChanged ); message CM_FOCUSCHANGED;
    procedure CMFontChanged( var Message: TMessage ); message CM_FONTCHANGED;
    procedure CMHintShow( var Message: TCMHintShow ); message CM_HINTSHOW;
    procedure CMMouseEnter( var Message: TMessage ); message CM_MOUSEENTER;
    procedure CMMouseLeave( var Message: TMessage ); message CM_MOUSELEAVE;
    procedure CMTextChanged( var Message: TMessage ); message CM_TEXTCHANGED;
    { Windows Messages }
    procedure WMEraseBkgnd( var Message: TWmEraseBkgnd ); message WM_ERASEBKGND;
    { Properties }
    property State: TdcButtonState read fState write Set_State;
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
    { Methods }
    procedure Assign( Source: TPersistent ); override;
    procedure Click; override;
    function HasIcon: boolean;
    { Properties }
    property ArrowSize: TSize read GetArrowSize;
    property IconSize: TSize read GetIconSize;
    property GlyphState: TdcGlyphState read GetGlyphState;
  published
    property Action;
    property Align;
    property Anchors;
    property ArrowSpacing: Integer read fArrowSpacing write Set_ArrowSpacing default DistanceDouple;
    property AutoSize;
    property Cancel: boolean read fCancel write fCancel default false;
    property Caption;
    property Color;
    property Constraints;
    property Cursor default crHandPoint;
    property default: boolean read fDefault write Set_Default default false;
    property DisabledGlyph: TPicture read fDisabledGlyph write Set_DisabledGlyph;
    property DisabledImages: TCustomImageList read FDisabledImages write Set_DisabledImages;
    property Down: boolean read FDown write Set_Down default false;
    property Enabled;
    property EnableDblClick: boolean read fEnableDblClick write Set_EnableDblClick default true;
    property Font;
    property Glyph: TPicture read FGlyph write Set_Glyph;
    property GlyphLocator: TGlyphLocator read fGlyphLocator write Set_GlyphLocator;
    property GlyphSpacing: Integer read fGlyphSpacing write Set_GlyphSpacing default DistanceDouple;
    property Height default 30;
    property ImageIndex: TImageIndex read FImageIndex write Set_ImageIndex default -1;
    property Images: TCustomImageList read fImages write Set_Images;
    property Layout: TButtonLayout read fLayout write Set_Layout default blGlyphLeft;
    property ModalResult: TModalResult read fModalResult write fModalResult default 0;
    property Padding;
    property ParentColor default false;
    property ParentFont;
    property ParentShowHint;
    property PushedImages: TCustomImageList read fPushedImages write Set_PushedImages;
    property ShowArrow: boolean read fShowArrow write Set_ShowArrow default false;
    property ShowHint;
    property TabOrder;
    property TabStop default true;
    property Transparent: boolean read fTransparent write Set_Transparent default false;
    property TransparentColor: TColor read fTransparentColor write Set_TransparentColor default clNone;
    property Visible;
    property Width default 75;
    property OnClick;
    property OnContextPopup;
    property OnDblClick;
    property OnDragDrop;
    property OnDragOver;
    property OnEndDock;
    property OnEndDrag;
    property OnEnter;
    property OnExit;
    property OnKeyDown;
    property OnKeyPress;
    property OnKeyUp;
    property OnMouseDown;
    property OnMouseMove;
    property OnMouseUp;
    property OnStartDock;
    property OnStartDrag;
  end;

  { TdcFlatButton }

  TdcFlatButton = class( TdcButton )
  private
    fStyle: TdcButtonColorStyle;
    procedure Set_Style( const Value: TdcButtonColorStyle );
  protected
    procedure AdjustFontColor( const ACanvas: TCanvas ); override;
    procedure PaintBackground( const ACanvas: TCanvas; const ButtonRect: TRect; const ButtonState: TdcButtonState ); override;
  public
    constructor Create( AOwner: TComponent ); override;
  published
    property Style: TdcButtonColorStyle read fStyle write Set_Style default bcDefault;
  end;

  { TdcColorPickerButton }

  TdcColorSpeedButton = class( TdcSpeedButton )
  private
    fOnSelectedChanged: TNotifyEvent;
    fSelected: TColor;
    procedure Set_Selected( const Value: TColor );
  public
    constructor Create( AOwner: TComponent ); override;
  published
    property Selected: TColor read fSelected write Set_Selected default clBlue;
    property OnSelectedChanged: TNotifyEvent read fOnSelectedChanged write fOnSelectedChanged;
  end;

  { TdcToolButtonDraw }

  TdcToolButtonDraw = class( TComponent )
  private
    fToolbar: TToolbar;
    fDesignTime: boolean;
    procedure Set_DesignTime( const Value: boolean );
    procedure Set_Toolbar( const Value: TToolbar );
  protected
    procedure DoAdvancedCustomDrawButton( Sender: TToolbar; Button: TToolButton; State: TCustomDrawState; Stage: TCustomDrawStage; var Flags: TTBCustomDrawFlags; var DefaultDraw: boolean );
    procedure DoCustomDrawButton( Sender: TToolbar; Button: TToolButton; State: TCustomDrawState; var DefaultDraw: boolean );
  public
    procedure Add( ToolBar: TToolbar );
    class procedure DrawToolButton( Canvas: TCanvas; Button: TToolButton; Images, DisabledImages: TCustomImageList; State: TCustomDrawState; ShowCaptions, List: boolean );
  published
    property DesignTime: boolean read fDesignTime write Set_DesignTime default false;
    property ToolBar: TToolbar read fToolbar write Set_Toolbar;
  end;

procedure PaintDarkButton( Canvas: TCanvas; Color: TColor; ButtonRect: TRect; ButtonState: TdcButtonState; Flat: boolean );
procedure PaintLightButton( Canvas: TCanvas; Color: TColor; ButtonRect: TRect; ButtonState: TdcButtonState; Flat: boolean );

implementation

uses
  System.Math,
  Vcl.Forms,
  Emetra.Vcl.Graphics,
  Emetra.Vcl.Helpers;

procedure DrawDropDownIndicator( Canvas: TCanvas; Dest: TRect; Size: TSize; Enabled: boolean );
var
  arrowRect: TRect;
begin
  arrowRect := CenteredRect( Dest, Bounds( 0, 0, Size.cx, Size.cy ) );
  with Canvas, arrowRect do
  begin
    if Enabled then
      Brush.Color := clBlack
    else
      Brush.Color := clGlyphDisabled;
    Pen.Color := Brush.Color;
    Polygon( [Point( Left, Top ), Point( Right, Top ), Point( Left + ( Right - Left ) div 2, Bottom )] );
  end;
end;

function LocateResource( Kind: TdcButtonKind; GlyphState: TdcGlyphState ): string;
begin
  Result := '';
  case Kind of
    btkPlus: Result := 'PLUS';
    btkCancel: Result := 'CANCEL';
    btkMinus: Result := 'MINUS';
    btkClear: Result := 'DELETE';
    btkOk: Result := 'OK';
    btkListRemove: Result := 'LISTREMOVE';
    btkRefresh: Result := 'REFRESH';
    btkSearch: Result := 'SEARCH';
  end;
  case GlyphState of
    gtLight: Result := Result + '.LIGHT';
    gtDark: Result := Result + '.DARK';
    gtDisabled: Result := Result + '.DARK.DISABLED';
  end;
end;

procedure PaintButton( Canvas: TCanvas; Color: TColor; ButtonRect: TRect; ButtonState: TdcButtonState );
var
  BrushColor, PenColor: TColor;
begin
  if ( btDefault in ButtonState ) and not( btDisabled in ButtonState ) then
  begin
    if btHot in ButtonState then
    begin
      if btPushed in ButtonState then
        BrushColor := clBtnFaceDefaultPressed
      else
        BrushColor := clBtnFaceDefaultHot;
    end
    else
      BrushColor := clBtnFaceDefault;

    Canvas.Brush.Color := BrushColor;
    Canvas.FillRect( ButtonRect );

    if btSelected in ButtonState then
    begin
      Canvas.Brush.Color := BlendColor( BrushColor, clBlack, 150 );
      Canvas.DrawBorder( ButtonRect, 2 );
      ButtonRect.Inflate( -2, -2 );
      Canvas.Brush.Color := BlendColor( BrushColor, clWhite, 83 );
      Canvas.DrawBorder( ButtonRect, 1 );
    end;
  end
  else
  begin
    if btHot in ButtonState then
    begin
      if btPushed in ButtonState then
      begin
        BrushColor := $008E8E8E;
        PenColor := $00555555;
      end
      else
      begin
        BrushColor := $00DDDDDD;
        PenColor := $009F9F9F;
      end;
    end
    else
    begin
      BrushColor := $00F4F4F4;
      PenColor := $00B9B9B9;
    end;

    if btDisabled in ButtonState then
    begin
      BrushColor := BlendColor( BrushColor, Color, 128 );
      PenColor := BlendColor( PenColor, Color, 128 );
    end;

    Canvas.Brush.Color := BrushColor;
    Canvas.Pen.Color := PenColor;
    Canvas.Rectangle( ButtonRect );

    if ( btSelected in ButtonState ) and not( btDisabled in ButtonState ) then
    begin
      Canvas.Brush.Color := clSelectedBkDark;
      Canvas.DrawBorder( ButtonRect, 2 );
    end;
  end;
end;

procedure PaintDarkButton( Canvas: TCanvas; Color: TColor; ButtonRect: TRect; ButtonState: TdcButtonState; Flat: boolean );
var
  Alpha: Byte;
begin
  Canvas.Brush.Color := Color;
  if Flat then
    Alpha := 255
  else
    Alpha := 184;

  if btDefault in ButtonState then
  begin
    Alpha := 220;
    if btHot in ButtonState then
      Alpha := 200;
  end
  else
  begin
    if btHot in ButtonState then
    begin
      Alpha := 156;
      if btPushed in ButtonState then
        Alpha := 120;
    end;
    if btChecked in ButtonState then
      Alpha := 120;
  end;

  if btDisabled in ButtonState then
    if Flat then
      Alpha := 255
    else
      Alpha := 224;

  if Alpha <> 255 then
  begin
    Canvas.Brush.Color := BlendColor( Canvas.Brush.Color, clBlack, Alpha );
    Canvas.FillRect( ButtonRect );
  end;

  { Focus border }
  if btSelected in ButtonState then
  begin
    // Brush.Color := clSelectedBkDark;
    // DrawBorder( BtnRect, 2 );
  end;
end;

procedure PaintLightButton( Canvas: TCanvas; Color: TColor; ButtonRect: TRect; ButtonState: TdcButtonState; Flat: boolean );
var
  Alpha: Byte;
begin
  Canvas.Brush.Color := Color;
  if Flat then
    Alpha := 255
  else
    Alpha := 184;

  if btDefault in ButtonState then
  begin
    Alpha := 220;
    if btHot in ButtonState then
      Alpha := 200;
  end
  else
  begin
    if btHot in ButtonState then
    begin
      Alpha := 156;
      if btPushed in ButtonState then
        Alpha := 120;
    end;
    if btChecked in ButtonState then
      Alpha := 120;
  end;

  if btDisabled in ButtonState then
    if Flat then
      Alpha := 255
    else
      Alpha := 224;

  if Alpha <> 255 then
  begin
    Canvas.Brush.Color := BlendColor( Canvas.Brush.Color, clBlack, Alpha );
    Canvas.FillRect( ButtonRect );
  end;

  { Focus border }
  if btSelected in ButtonState then
  begin
    // Brush.Color := clSelectedBkDark;
    // DrawBorder( BtnRect, 2 );
  end;
end;

procedure PaintButtonContent( Canvas: TCanvas; Caption: string; Images: TCustomImageList; ImageIndex: TImageIndex; Layout: TButtonLayout );
begin

end;

{ TdcStateButton }

procedure TdcStateButton.CMEnabledChanged( var Message: TMessage );
begin
  inherited;
  if Enabled then
    State := State - [btDisabled]
  else
    State := State + [btDisabled];
end;

procedure TdcStateButton.CMMouseEnter( var Message: TMessage );
begin
  inherited;
  State := State + [btHot];
end;

procedure TdcStateButton.CMMouseLeave( var Message: TMessage );
begin
  inherited;
  State := State - [btHot];
end;

procedure TdcStateButton.CMTextChanged( var Message: TMessage );
begin
  inherited;
  Invalidate;
  if AutoSize then
    RequestAlign;
end;

constructor TdcStateButton.Create( AOwner: TComponent );
begin
  inherited;
  ControlStyle := [csCaptureMouse, csDoubleClicks];
  Cursor := crHandPoint;
end;

procedure TdcStateButton.DoCheckedChange;
begin
  if Assigned( fOnCheckedChange ) then
    fOnCheckedChange( Self );
end;

function TdcStateButton.GetGlyphState: TdcGlyphState;
begin
  Result := gtDark;
  if btHot in State then
  begin
    if btPushed in State then
      Result := gtLight;
  end;
  if btDisabled in State then
    Result := gtDisabled;
end;

procedure TdcStateButton.MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: Integer );
begin
  inherited;
  if Button = mbLeft then
    State := State + [btPushed];
end;

procedure TdcStateButton.MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: Integer );
begin
  inherited;
  if btPushed in State then
  begin
    State := State - [btPushed];
    if AutoCheck then
      Checked := not Checked;
    Click;
  end;
end;

procedure TdcStateButton.Set_Checked( const Value: boolean );
begin
  if Value <> fChecked then
  begin
    fChecked := Value;
    if Checked then
      Include( fState, btChecked )
    else
      Exclude( fState, btChecked );
    DoCheckedChange;
  end;
end;

procedure TdcStateButton.Set_State( const Value: TdcButtonState );
begin
  if Value <> fState then
  begin
    fState := Value;
    Invalidate;
  end;
end;

procedure TdcStateButton.WMActivateApp( var Message: TWMActivateApp );
begin
  inherited;
  if not message.Active then
    State := State - [btHot];
end;

procedure TdcStateButton.WndProc( var Message: TMessage );
begin
  if Owner <> nil then
    inherited;
end;

{ TdcGraphicButton }

procedure TdcGraphicButton.CMMouseEnter( var Message: TMessage );
begin
  inherited;
  State := State + [btHot];
end;

procedure TdcGraphicButton.CMMouseLeave( var Message: TMessage );
begin
  inherited;
  State := State - [btHot];
end;

constructor TdcGraphicButton.Create( AOwner: TComponent );
begin
  inherited;
  fDefault := false;
  fTransparentColor := false;
  ControlStyle := [csCaptureMouse, csDoubleClicks, csOpaque];
end;

procedure TdcGraphicButton.DoOwnerDraw( Canvas: TCanvas );
begin
  if Assigned( fOwnerDraw ) then
    fOwnerDraw( Self, Canvas );
end;

procedure TdcGraphicButton.MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: Integer );
begin
  inherited;
  State := State + [btPushed];
end;

procedure TdcGraphicButton.MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: Integer );
begin
  inherited;
  if btPushed in State then
  begin
    State := State - [btPushed];
    Click;
  end;
end;

procedure TdcGraphicButton.Paint;
var
  FaceColor: TColor;
begin
  inherited;
  if fTransparentColor then
    FaceColor := Color
  else
    FaceColor := ButtonColor[fDefault];
  if btHot in fState then
  begin
    FaceColor := ButtonColorHot[fDefault];
    if btPushed in fState then
      FaceColor := ButtonColorPressed[fDefault];
  end;
  Canvas.Brush.Color := FaceColor;
  Canvas.FillRect( ClientRect );
  Canvas.Font.Color := ButtonColorText[fDefault];
  Canvas.RenderText( ClientRect, Caption, [tfCenter, tfSingleLine, tfVerticalCenter] );
  DoOwnerDraw( Canvas );
end;

procedure TdcGraphicButton.SetDefault( const Value: boolean );
begin
  if Value <> fDefault then
  begin
    fDefault := Value;
    Invalidate;
  end;
end;

procedure TdcGraphicButton.SetState( const Value: TdcButtonState );
begin
  if Value <> fState then
  begin
    fState := Value;
    Invalidate;
  end;
end;

procedure TdcGraphicButton.SetTransparentColor( const Value: boolean );
begin
  if Value <> fTransparentColor then
  begin
    fTransparentColor := Value;
    Invalidate;
  end;
end;

procedure TdcGraphicButton.WMEraseBkgnd( var Message: TWmEraseBkgnd );
begin
  message.Result := 1;
end;

procedure TdcGraphicButton.WndProc( var Message: TMessage );
begin
  if Owner <> nil then
    inherited;
end;

{ TdcToggleButton }

procedure TdcToggleButton.Set_Images( const Value: TCustomImageList );
begin
  fImages := Value;
end;

{ TdcEditButton }

function TdcEditButton.CanAutoSize( var NewWidth, NewHeight: Integer ): boolean;
begin
  Result := inherited CanAutoSize( NewWidth, NewHeight );
  Canvas.Font.Assign( Font );
  NewWidth := Canvas.GetTextSize( Caption ).cx + SpacingDouble * 2;
end;

procedure TdcEditButton.CMMouseEnter( var Message: TMessage );
begin
  if not( csDesigning in ComponentState ) then
  begin
    Hover := true;
  end;
end;

procedure TdcEditButton.CMMouseLeave( var Message: TMessage );
begin
  if not( csDesigning in ComponentState ) then
  begin
    Hover := false;
  end;
end;

constructor TdcEditButton.Create( AOwner: TComponent );
begin
  inherited;
  FAutoUp := true;
  FDown := false;
  FHover := false;
  FMouseDown := false;
  Cursor := crHandPoint;
  Color := clEditControl
end;

procedure TdcEditButton.DoButtonClick;
begin
  if Assigned( OnButtonClick ) then
    FOnButtonClick( Self );
end;

procedure TdcEditButton.DoDown;
begin
  if Assigned( OnDown ) then
    FOnDown( Self );
end;

procedure TdcEditButton.DoUp;
begin
  if Assigned( FOnUp ) then
    FOnUp( Self );
end;

function TdcEditButton.GetAlign: TAlign;
begin
  Result := inherited Align;
end;

function TdcEditButton.GetAutoUp: boolean;
begin
  Result := FAutoUp;
end;

function TdcEditButton.GetBoundsRect: TRect;
begin
  Result := BoundsRect;
end;

function TdcEditButton.GetCaption: string;
begin
  Result := FCaption;
end;

function TdcEditButton.GetDown: boolean;
begin
  Result := FDown;
end;

function TdcEditButton.GetEdit: IInplaceEdit;
begin
  Result := FEdit;
end;

function TdcEditButton.GetHandle: HWND;
begin
  Result := inherited Handle;
end;

function TdcEditButton.GetHover: boolean;
begin
  Result := FHover;
end;

function TdcEditButton.GetLeft: Integer;
begin
  Result := inherited Left;
end;

function TdcEditButton.GetOnButtonClick: TNotifyEvent;
begin
  Result := FOnButtonClick;
end;

function TdcEditButton.GetOnDown: TNotifyEvent;
begin
  Result := FOnDown;
end;

function TdcEditButton.GetOnUp: TNotifyEvent;
begin
  Result := FOnUp;
end;

function TdcEditButton.GetParent: TWinControl;
begin
  Result := inherited Parent;
end;

function TdcEditButton.GetVisible: boolean;
begin
  Result := inherited Visible;
end;

function TdcEditButton.GetWidth: Integer;
begin
  Result := inherited Width;
end;

function TdcEditButton.Get_State: TdcButtonState;
begin
  Result := [];
  if FDown then
    Include( Result, btPushed );
  if FHover then
    Include( Result, btHot );
  if not Enabled then
    Include( Result, btDisabled );
end;

procedure TdcEditButton.MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: Integer );
begin
  inherited;
  Down := not Down;

  { Used in MouseUp }
  FMouseDown := true;

  { Focus Edit too! }
  if Assigned( FEdit ) and FEdit.Showing then
  begin
    FEdit.SetFocus;
  end;
end;

procedure TdcEditButton.MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: Integer );
begin
  inherited;
  if FAutoUp then
    Down := false;

  { Is Click? }
  if FMouseDown then
  begin
    FMouseDown := false;

    { Trigger Event }
    DoButtonClick;
  end;
end;

procedure TdcEditButton.Paint;
begin
  inherited;
  PaintBackground;
end;

procedure TdcEditButton.SetAlign( const Value: TAlign );
begin
  inherited Align := Value;
end;

procedure TdcEditButton.SetAutoUp( const Value: boolean );
begin
  FAutoUp := Value;
end;

procedure TdcEditButton.SetCaption( const Value: string );
begin
  if Value <> FCaption then
  begin
    FCaption := Value;
    if AutoSize then
      Realign;
    Invalidate;
  end;
end;

procedure TdcEditButton.SetDown( const Value: boolean );
begin
  if Value <> FDown then
  begin
    FDown := Value;
    Refresh;
    if not FDown then
      DoUp
    else
      DoDown;
  end;
end;

procedure TdcEditButton.SetEdit( const Value: IInplaceEdit );
begin
  FEdit := Value;
end;

procedure TdcEditButton.SetHover( const Value: boolean );
begin
  if Value <> FHover then
  begin
    FHover := Value;
    Invalidate;
  end;
end;

procedure TdcEditButton.SetLeft( const Value: Integer );
begin
  inherited Left := Value;
end;

procedure TdcEditButton.SetOnButtonClick( const Value: TNotifyEvent );
begin
  FOnButtonClick := Value;
end;

procedure TdcEditButton.SetOnDown( const Value: TNotifyEvent );
begin
  FOnDown := Value;
end;

procedure TdcEditButton.SetOnUp( const Value: TNotifyEvent );
begin
  FOnUp := Value;
end;

procedure TdcEditButton.SetParent( AParent: TWinControl );
begin
  inherited SetParent( AParent );
end;

procedure TdcEditButton.SetVisible( const Value: boolean );
begin
  inherited Visible := Value;
end;

procedure TdcEditButton.SetWidth( const Value: Integer );
begin
  inherited Width := Value;
end;

procedure TdcEditButton.WMEraseBkgnd( var Message: TWmEraseBkgnd );
begin
  inherited;
  message.Result := 1;
end;

{ TNxDropDownButton }

constructor TdcDropDownButton.Create( AOwner: TComponent );
begin
  inherited;
  Height := 19;
  Width := DropDownButtonWidth;
end;

procedure TdcDropDownButton.PaintBackground;
var
  arrowRect: TRect;
  ButtonState: TdcButtonState;
  buttonFace: TColor;

  procedure DrawDropDownArrow( AArrowRect: TRect );
  begin
    with AArrowRect do
    begin
      if ( btDisabled in ButtonState ) then
        Canvas.Pen.Color := clGlyphDisabled
      else
        Canvas.Pen.Color := clGlyph;
      Canvas.Brush.Color := Canvas.Pen.Color;
      Canvas.Polygon( [Point( Left, Top ), Point( Right, Top ), Point( Left + Width div 2, Bottom - 1 )] );
    end;
  end;

begin
  if csDesigning in ComponentState then
    buttonFace := clBtnFaceDropDown
  else
  begin
    ButtonState := [];
    if Hover then
      Include( ButtonState, btHot );
    if Down then
      Include( ButtonState, btPushed );
    if not Parent.Enabled then
      Include( ButtonState, btDisabled );

    buttonFace := Color;
    if Assigned( Edit ) then
      buttonFace := Edit.Color;

    if btHot in ButtonState then
    begin
      buttonFace := clBtnFaceDropDown;
      if btPushed in ButtonState then
        buttonFace := clBtnFaceNormalPressed;
    end;
  end;

  with Canvas do
  begin
    Brush.Color := buttonFace;
    FillRect( ClientRect );
    arrowRect := CenteredRect( ClientRect, Bounds( 0, 0, 8, 5 ) );
    DrawDropDownArrow( arrowRect );
  end;
end;

{ TdcExecuteButton }

constructor TdcExecuteButton.Create( AOwner: TComponent );
begin
  inherited;
  FDisabledImages := nil;
  FGlyph := TGlyphLocator.Create;
  FGlyph.OnChange := DoGlyphChange;
  FGlyphKind := gkCustom;
  FImageIndex := -1;
  fImages := nil;
  ParentColor := true;
  Width := ExecuteButtonWidth;
  Height := 21;
end;

destructor TdcExecuteButton.Destroy;
begin
  FreeAndNil( FGlyph );
  inherited;
end;

procedure TdcExecuteButton.DoGlyphChange( Sender: TObject );
begin
  Invalidate;
end;

class procedure TdcExecuteButton.DrawMinusGlyph( Canvas: TCanvas; const X, Y: Integer );
begin
  with Canvas do
  begin
    FillRect( Bounds( X, Y, 10, 2 ) );
  end;
end;

class procedure TdcExecuteButton.DrawPlusGlyph( Canvas: TCanvas; const X, Y: Integer );
begin
  with Canvas do
  begin
    FillRect( Bounds( X, Y + 4, 10, 2 ) );
    FillRect( Bounds( X + 4, Y, 2, 10 ) );
  end;
end;

class procedure TdcExecuteButton.DrawCloseSign( Canvas: TCanvas; const X, Y: Integer );
begin
  with Canvas do
  begin
    Polygon( [Point( X, Y ), Point( X + 8, Y + 8 ), Point( X + 9, Y + 8 ), Point( X + 1, Y + 0 ), Point( X, Y )] );
    Polygon( [Point( X, Y + 8 ), Point( X + 8, Y ), Point( X + 9, Y ), Point( X + 1, Y + 8 ), Point( X, Y + 8 )] );
  end;
end;

procedure TdcExecuteButton.Paint;
begin
  inherited;
  Canvas.Font.Assign( Font );
  PaintInRect( Canvas, ClientRect, State, GlyphKind, FindGraphic( Glyph.Glyph, gtDark, Glyph.Dimensions ), Images, DisabledImages, ImageIndex, Enabled, Caption );
end;

procedure TdcExecuteButton.PaintBackground;
var
  ButtonState: TdcButtonState;
  buttonFace: TColor;
begin
  if csDesigning in ComponentState then
    buttonFace := clBtnFaceDropDown
  else
  begin
    ButtonState := [];
    if Hover then
      Include( ButtonState, btHot );
    if Down then
      Include( ButtonState, btPushed );
    if not Parent.Enabled then
      Include( ButtonState, btDisabled );

    buttonFace := Color;
    if btHot in ButtonState then
    begin
      buttonFace := clBtnFaceDropDown;
      if btPushed in ButtonState then
        buttonFace := clBtnFaceNormalPressed;
    end;
  end;

  with Canvas do
  begin
    Brush.Color := buttonFace;
    FillRect( ClientRect );
  end;
end;

class procedure TdcExecuteButton.PaintInRect( Canvas: TCanvas; Dest: TRect; AButtonState: TdcButtonState; AGlyphKind: TdcButtonGlyphKind; Glyph: TGraphic; Images, DisabledImages: TCustomImageList; ImageIndex: TImageIndex; Enabled: boolean;
  Text: string );
var
  bitmap: TBitmap;
  textStr: string;
  glyphRect, textRect: TRect;
  imageList: TCustomImageList;
begin
  Canvas.Brush.Color := GetGeometryColor( AButtonState );
  case AGlyphKind of
    gkPlus:
      begin
        glyphRect := TRect.Create( 0, 0, 10, 10 );
        glyphRect := CenteredRect( Dest, glyphRect );
        DrawPlusGlyph( Canvas, glyphRect.Left, glyphRect.Top );
      end;
    gkMinus:
      begin
        glyphRect := TRect.Create( 0, 0, 10, 2 );
        glyphRect := CenteredRect( Dest, glyphRect );
        DrawMinusGlyph( Canvas, glyphRect.Left, glyphRect.Top );
      end;
    gkClose:
      begin
        glyphRect := TRect.Create( 0, 0, 8, 8 );
        glyphRect := CenteredRect( Dest, glyphRect );
        DrawCloseSign( Canvas, glyphRect.Left, glyphRect.Top );
      end
  else
    begin
      if Assigned( Glyph ) and not Glyph.Empty then
      begin
        glyphRect := TRect.Create( 0, 0, Glyph.Width, Glyph.Height );
        glyphRect := CenteredRect( Dest, glyphRect );
        if Glyph is TBitmap then
        begin
          bitmap := Glyph as TBitmap;
          bitmap.Transparent := true;
          bitmap.TransparentColor := bitmap.Canvas.Pixels[0, Pred( bitmap.Height )];
        end;
        Canvas.Draw( glyphRect.Left, glyphRect.Top, Glyph );
      end
      else
      begin
        imageList := Images;
        if not Enabled and Assigned( DisabledImages ) then
          imageList := DisabledImages;
        if Assigned( imageList ) and InRange( ImageIndex, 0, Pred( imageList.Count ) ) then
        begin
          glyphRect := TRect.Create( 0, 0, imageList.Width, imageList.Height );
          glyphRect := CenteredRect( Dest, glyphRect );
          imageList.Draw( Canvas, glyphRect.Left, glyphRect.Top, ImageIndex );
        end;
      end;
    end;
  end; { case }

  if Text <> '' then
  begin
    textRect := Dest;
    textStr := Text;
    Canvas.Brush.Style := bsClear;
    if btPushed in AButtonState then
      Canvas.Font.Color := clWhite;
    Canvas.textRect( textRect, textStr, [tfCenter, tfEndEllipsis, tfSingleLine, tfVerticalCenter] );
    Canvas.Brush.Style := bsSolid;
  end;
end;

procedure TdcExecuteButton.Set_Glyph( const Value: TGlyphLocator );
begin
  FGlyph.Assign( Value );
  if Assigned( FGlyph ) then
    FGlyph.OnChange := DoGlyphChange;
end;

procedure TdcExecuteButton.Set_GlyphKind( const Value: TdcButtonGlyphKind );
begin
  if Value <> FGlyphKind then
  begin
    FGlyphKind := Value;
    Invalidate;
  end;
end;

procedure TdcExecuteButton.Set_ImageIndex( const Value: TImageIndex );
begin
  if Value <> FImageIndex then
  begin
    FImageIndex := Value;
    Invalidate;
  end;
end;

procedure TdcExecuteButton.Set_Images( const Value: TCustomImageList );
begin
  if Value <> fImages then
  begin
    fImages := Value;
    if fImages <> nil then
      fImages.FreeNotification( Self );
    Invalidate;
  end;
end;

procedure TdcExecuteButton.Set_DisabledImages( const Value: TCustomImageList );
begin
  if Value <> FDisabledImages then
  begin
    FDisabledImages := Value;
    if fImages <> nil then
      FDisabledImages.FreeNotification( Self );
    Invalidate;
  end;
end;

{ TdcCustomSpeedButton }

procedure TdcCustomSpeedButton.AdjustFontColor( const ACanvas: TCanvas );
begin
  case Style of
    bcDefault:
      if btPushed in State then
        ACanvas.Font.Color := clWhite;
    bcLight:
      begin
        ACanvas.Font.Color := clWhite;
        if btDisabled in State then
          ACanvas.Font.Color := BlendColor( ACanvas.Font.Color, Color, intTextDisabledAlpha );
      end;
  end;
end;

function TdcCustomSpeedButton.CanAutoSize( var NewWidth, NewHeight: Integer ): boolean;
begin
  Result := inherited CanAutoSize( NewWidth, NewHeight );
  with ImageAndCaptionSize do
  begin
    NewWidth := cx + fAutoSizePadding * 2;
    NewHeight := cy + fAutoSizePadding * 2;
  end;
end;

constructor TdcCustomSpeedButton.Create( AOwner: TComponent );
begin
  inherited;
  fArrowSpacing := SpacingDouble;
  fAutoSizePadding := 8;
  FDown := false;
  FDisabledImages := nil;
  fFlat := true;
  FGlyph := TPicture.Create;
  FGlyph.OnChange := DoGlyphChange;
  fGlyphLocator := TGlyphLocator.Create;
  fGlyphLocator.OnChange := DoGlyphChange;
  FImageIndex := -1;
  fImages := nil;
  fLayout := blGlyphLeft;
  fShowArrow := false;
  fShowCaption := true;
  fStyle := bcDefault;
  fTransparent := true;

  Color := clBtnFaceNormal;
  Cursor := crHandPoint;
  ParentColor := false;
  Width := 30;
  Height := 30;
end;

destructor TdcCustomSpeedButton.Destroy;
begin
  FreeAndNil( FGlyph );
  FreeAndNil( fGlyphLocator );
  inherited;
end;

procedure TdcCustomSpeedButton.DoGlyphChange( Sender: TObject );
begin
  Invalidate;
end;

function TdcCustomSpeedButton.Get_ArrowSize: TSize;
begin
  Result := TSize.Create( 10, 10 );
end;

function TdcCustomSpeedButton.Get_CaptionSize: TSize;
begin
  if ( Caption <> '' ) and ShowCaption then
  begin
    Canvas.Font.Assign( Font );
    Result := Canvas.GetTextSize( Caption );
  end
  else
    Result := TSize.Create( 0, 0 );
end;

function TdcCustomSpeedButton.Get_ImageAndCaptionSize: TSize;
begin
  Result := TSize.Create( ImageSize.cx, Max( ImageSize.cy, CaptionSize.cy ) );
  if Caption <> '' then
    Inc( Result.cx, SpacingDouble + CaptionSize.cx );
end;

function TdcCustomSpeedButton.Get_ImageSize: TSize;
var
  ImgList: TCustomImageList;
begin
  Result := TSize.Create( 0, 0 );
  if GlyphLocator.HasGlyph then
  begin
    Result := GlyphLocator.GraphicSize( GlyphState );
  end
  else if Assigned( Glyph ) and Assigned( Glyph.Graphic ) and not Glyph.Graphic.Empty then
  begin
    Result := TSize.Create( Glyph.Width, Glyph.Height );
  end
  else
  begin
    ImgList := Images;
    if Assigned( DisabledImages ) then
      ImgList := DisabledImages;
    if Assigned( ImgList ) then
      Result := TSize.Create( ImgList.Width, ImgList.Height );
  end;
end;

function TdcCustomSpeedButton.HasGlyph: boolean;
begin
  Result := ( fGlyphLocator.HasGlyph ) or ( Assigned( fImages ) and InRange( FImageIndex, 0, fImages.Count - 1 ) ) or ( Assigned( FGlyph.Graphic ) and not FGlyph.Graphic.Empty );
end;

procedure TdcCustomSpeedButton.Paint;
begin
  inherited;
  if Transparent then
    PerformEraseBackground( Self, Canvas.Handle );
  PaintBackground( Canvas );
  PaintContent( Canvas );
end;

procedure TdcCustomSpeedButton.PaintBackground( ACanvas: TCanvas );
begin
  with ACanvas do
  begin
    case Style of
      bcDefault:
        begin
          if Flat then
            Brush.Color := clNone
          else
            Brush.Color := Color;

          PaintFlatButtonBackground( ClientRect, State );
        end;
      bcLight: PaintLightButton( ACanvas, Color, ClientRect, State, Flat );
    end;

    if Flat and ( csDesigning in ComponentState ) then
    begin
      Brush.Color := BlendColor( Color, clWhite, 192 );
      FrameRect( ClientRect );
    end;
  end;
end;

procedure TdcCustomSpeedButton.PaintContent( ACanvas: TCanvas );
var
  CaptionAlignment: TAlignment;
  CaptionSize: TSize;
  arrowRect, CaptionRect: TRect;
  Spacing, ArrowLeft: Integer;
  CaptionLocation, IconLocation: TPoint;
  TextFormat: TTextFormat;
begin
  with ACanvas do
  begin
    Font.Assign( Self.Font );

    AdjustFontColor( Canvas );

    if ShowCaption then
      CaptionSize := Canvas.GetTextSize( Caption );

    if HasGlyph then
    begin

      Spacing := 0;
      if Caption <> '' then
        Spacing := SpacingDouble;

      IconLocation := ClientRect.CenterSize( ImageSize );

      with IconLocation do
        case Layout of
          blGlyphBottom:
            begin
              Inc( Y, CaptionSize.cy div 2 );
              PaintGlyph( ACanvas, IconLocation );
              Dec( Y, CaptionSize.cy );
            end;

          blGlyphLeft:
            begin
              Dec( X, CaptionSize.cx div 2 );
              PaintGlyph( ACanvas, IconLocation );
              Inc( X, ImageSize.cx + Spacing );
            end;

          blGlyphTop:
            begin
              Dec( Y, CaptionSize.cy div 2 );
              PaintGlyph( ACanvas, IconLocation );
              Inc( Y, ImageSize.cy );
            end;

        else
          begin
            Inc( X, CaptionSize.cx div 2 );

            PaintGlyph( ACanvas, IconLocation );

            Dec( X, Spacing + CaptionSize.cx );
          end;
        end;

      case Layout of
        blGlyphLeft, blGlyphRight:
          begin
            CaptionAlignment := taLeftJustify;
            CaptionRect := Bounds( IconLocation.X, 0, CaptionSize.cx, ClientHeight );

            case Layout of
              blGlyphRight: ArrowLeft := CaptionRect.Right + Spacing + ImageSize.cx;
            else ArrowLeft := CaptionRect.Right;
            end;
          end
      else
        begin
          CaptionAlignment := System.Classes.taCenter;
          CaptionLocation := Point( RectWidth( ClientRect ) div 2 - CaptionSize.cx div 2, IconLocation.Y );

          CaptionRect := Bounds( CaptionLocation.X, CaptionLocation.Y, CaptionSize.cx, CaptionSize.cy );

          ArrowLeft := CaptionRect.Right;
        end;

      end;

      if ShowCaption then
      begin
        TextFormat := [tfSingleLine, tfVerticalCenter, tfEndEllipsis];
        case CaptionAlignment of
          taLeftJustify: TextFormat := TextFormat + [tfLeft];
          taRightJustify: TextFormat := TextFormat + [tfRight];
        else TextFormat := TextFormat + [tfCenter];
        end;
        ACanvas.RenderText( CaptionRect, Caption, TextFormat );
      end;
    end
    else
    begin
      CaptionRect := CenteredRect( ClientRect, Bounds( 0, 0, CaptionSize.cx, ClientHeight ) );
      ArrowLeft := CaptionRect.Right;

      if ShowCaption then
      begin
        TextFormat := [tfCenter, tfSingleLine, tfVerticalCenter, tfEndEllipsis];
        ACanvas.RenderText( CaptionRect, Caption, TextFormat );
      end;
    end;

    if ShowArrow then
    begin
      arrowRect := Bounds( ArrowLeft, 0, ArrowSize.cx, ClientHeight );
      Inc( arrowRect.Left, ArrowSpacing );
      // DrawDropDownIndicator( Canvas, arrowRect, GetArrowSize );
    end;
  end;
end;

function TdcCustomSpeedButton.PaintGlyph( Canvas: TCanvas; Location: TPoint ): TSize;
var
  ImgList: TCustomImageList;
begin
  if GlyphLocator.Glyph <> grNone then
  begin
    Draw( Canvas, GlyphLocator, Location.X, Location.Y, GlyphState );
  end
  else if Assigned( FGlyph ) and Assigned( FGlyph.Graphic ) and not FGlyph.Graphic.Empty then
  begin
    DrawGraphic( Canvas, Location.X, Location.Y, FGlyph.Graphic );
  end
  else
  begin
    ImgList := fImages;
    // if ( default or ( btPushed in fState ) ) and Assigned( fPushedImages ) then
    // ImgList := fPushedImages;
    if not Enabled and Assigned( FDisabledImages ) then
      ImgList := FDisabledImages;
    if Assigned( ImgList ) and InRange( FImageIndex, 0, Pred( ImgList.Count ) ) then
      ImgList.Draw( Canvas, Location.X, Location.Y, FImageIndex );
  end
end;

procedure TdcCustomSpeedButton.Set_ArrowSpacing( const Value: Integer );
begin
  if Value <> fArrowSpacing then
  begin
    fArrowSpacing := Value;
    Invalidate;
  end;
end;

procedure TdcCustomSpeedButton.Set_DisabledImages( const Value: TCustomImageList );
begin
  FDisabledImages := Value;
end;

procedure TdcCustomSpeedButton.Set_Down( const Value: boolean );
begin
  if Value <> FDown then
  begin
    FDown := Value;
    Invalidate;
  end;
end;

procedure TdcCustomSpeedButton.Set_Flat( const Value: boolean );
begin
  if Value <> fFlat then
  begin
    fFlat := Value;
    Invalidate;
  end;
end;

procedure TdcCustomSpeedButton.Set_Glyph( const Value: TPicture );
begin
  FGlyph.Assign( Value );
  if Assigned( FGlyph ) then
    FGlyph.OnChange := DoGlyphChange;
end;

procedure TdcCustomSpeedButton.Set_GlyphLocator( const Value: TGlyphLocator );
begin
  if Value <> fGlyphLocator then
  begin
    fGlyphLocator.Assign( Value );
    Invalidate;
  end;
end;

procedure TdcCustomSpeedButton.Set_ImageIndex( const Value: TImageIndex );
begin
  if Value <> FImageIndex then
  begin
    FImageIndex := Value;
    Invalidate;
  end;
end;

procedure TdcCustomSpeedButton.Set_Images( const Value: TCustomImageList );
begin
  if Value <> fImages then
  begin
    fImages := Value;
    Invalidate;
  end;
end;

procedure TdcCustomSpeedButton.Set_Layout( const Value: TButtonLayout );
begin
  if Value <> fLayout then
  begin
    fLayout := Value;
    Invalidate;
  end;
end;

procedure TdcCustomSpeedButton.Set_ShowArrow( const Value: boolean );
begin
  if Value <> fShowArrow then
  begin
    fShowArrow := Value;
    Invalidate;
  end;
end;

procedure TdcCustomSpeedButton.Set_ShowCaption( const Value: boolean );
begin
  if Value <> fShowCaption then
  begin
    fShowCaption := Value;
    Invalidate;
  end;
end;

procedure TdcCustomSpeedButton.Set_Style( const Value: TdcButtonColorStyle );
begin
  if Value <> fStyle then
  begin
    fStyle := Value;
    Invalidate;
  end;
end;

procedure TdcCustomSpeedButton.Set_Transparent( const Value: boolean );
begin
  if Value <> fTransparent then
  begin
    fTransparent := Value;
    if Value then
      ControlStyle := ControlStyle - [csOpaque]
    else
      ControlStyle := ControlStyle + [csOpaque];
    Invalidate;
  end;
end;

{ TdcButton }

procedure TdcButton.AdjustFontColor( const ACanvas: TCanvas );
begin
  if ( btDefault in State ) or ( ( btPushed in State ) and ( btHot in State ) ) then
    ACanvas.Font.Color := clWhite;

  if ( btDisabled in State ) then
    ACanvas.Font.Color := BlendColor( clGrayText, Color, 128 );
end;

procedure TdcButton.Assign( Source: TPersistent );
begin
  inherited;
  if Source is TdcButton then
  begin
    Cancel := TdcButton( Source ).Cancel;
    default := TdcButton( Source ).Default;
    Down := TdcButton( Source ).Down;
    Glyph.Assign( TdcButton( Source ).Glyph );
    GlyphSpacing := TdcButton( Source ).GlyphSpacing;
    ImageIndex := TdcButton( Source ).ImageIndex;
    Layout := TdcButton( Source ).Layout;
    ModalResult := TdcButton( Source ).ModalResult;
    ShowArrow := TdcButton( Source ).ShowArrow;
    Transparent := TdcButton( Source ).Transparent;
  end;
end;

function TdcButton.CanAutoSize( var NewWidth, NewHeight: Integer ): boolean;
begin
  Result := inherited CanAutoSize( NewWidth, NewHeight );

  Canvas.Font.Assign( Font );

  with Canvas.GetTextSize( Caption ) do
  begin
    NewHeight := cy;
    NewWidth := cx;
  end;

  if HasIcon then
  begin
    NewHeight := Max( IconSize.cy, NewHeight );
    Inc( NewWidth, IconSize.cx + GlyphSpacing );
  end;

  if ShowArrow then
  begin
    Inc( NewWidth, ArrowSpacing );
    Inc( NewWidth, ArrowSize.cx );
  end;

  Inc( NewHeight, Padding.Top + Padding.Bottom );
  Inc( NewWidth, Padding.Left + Padding.Right );
end;

procedure TdcButton.Click;
begin
  if Assigned( GetParentForm( Self ) ) then
    GetParentForm( Self ).ModalResult := ModalResult;
  inherited;
end;

procedure TdcButton.CMDialogChar( var Message: TCMDialogChar );
begin
  with message do
  begin
    if IsAccel( CharCode, Caption ) then
    begin
      Click;
      Result := 1;
    end
    else
      inherited;
  end;
end;

procedure TdcButton.CMDialogKey( var Message: TCMDialogKey );
begin
  inherited;
  with message do
  begin
    if Enabled and ( ( ( CharCode = VK_RETURN ) and fActive ) or ( ( CharCode = VK_ESCAPE ) and fCancel ) ) and ( KeyDataToShiftState( message.KeyData ) = [] ) then
    begin
      Click;
    end
    else
      inherited;
  end;
end;

procedure TdcButton.CMEnabledChanged( var Message: TMessage );
begin
  inherited;
  if Enabled then
    State := State - [btDisabled]
  else
    State := State + [btDisabled];
  Invalidate;
end;

procedure TdcButton.CMEnter( var Message: TCMEnter );
begin
  inherited;
  Invalidate;
end;

procedure TdcButton.CMExit( var Message: TCMExit );
begin
  inherited;
  Invalidate;
end;

procedure TdcButton.CMFocusChanged( var Message: TCMFocusChanged );
begin
  with message do
    if Sender is TdcButton then
      fActive := Sender = Self
    else
      fActive := fDefault;

  if fActive then
    State := State + [btSelected]
  else
    State := State - [btSelected];
  inherited;
end;

procedure TdcButton.CMFontChanged( var Message: TMessage );
begin
  inherited;
  if AutoSize then
    Realign;
end;

procedure TdcButton.CMHintShow( var Message: TCMHintShow );
begin
  inherited;
  message.HintInfo.HintColor := clWhite;
end;

procedure TdcButton.CMMouseEnter( var Message: TMessage );
begin
  inherited;
  State := State + [btHot];
end;

procedure TdcButton.CMMouseLeave( var Message: TMessage );
begin
  inherited;
  State := State - [btHot];
end;

procedure TdcButton.CMTextChanged( var Message: TMessage );
begin
  inherited;
  Invalidate;
  Realign;
end;

constructor TdcButton.Create( AOwner: TComponent );
begin
  inherited Create( AOwner );
  ControlStyle := ControlStyle + [csOpaque];
  fArrowSpacing := SpacingDouble;
  fCancel := false;
  fDefault := false;
  FDown := false;
  fDisabledGlyph := TPicture.Create;
  FDisabledImages := nil;
  FGlyph := TPicture.Create;
  fGlyphLocator := TGlyphLocator.Create;
  fGlyphLocator.OnChange := DoGlyphChange;
  fGlyphSpacing := SpacingDouble;
  FImageIndex := -1;
  fImages := nil;
  fLayout := blGlyphLeft;
  fModalResult := 0;
  fPushedImages := nil;
  fShowArrow := false;
  fState := [];
  fTransparent := false;
  fTransparentColor := clNone;

  Color := clBtnFaceNormal;
  Cursor := crHandPoint;
  ParentColor := false;
  TabStop := true;
  Width := 75;
  Height := 30;
end;

procedure TdcButton.CreateWnd;
begin
  inherited;
  fActive := fDefault;
end;

destructor TdcButton.Destroy;
begin
  FreeAndNil( fDisabledGlyph );
  FreeAndNil( FGlyph );
  FreeAndNil( fGlyphLocator );
  inherited;
end;

procedure TdcButton.DoGlyphChange( Sender: TObject );
begin
  Invalidate;
end;

function TdcButton.GetArrowSize: TSize;
begin
  Result := TSize.Create( 8, 4 );
end;

function TdcButton.GetGlyphState: TdcGlyphState;
begin
  if default then
    Result := gtLight
  else
    Result := gtDark;

  if btHot in State then
  begin
    if btPushed in State then
      Result := gtLight;
  end;
  if btDisabled in State then
    Result := gtDisabled;
end;

function TdcButton.GetIconSize: TSize;
var
  ImgList: TCustomImageList;
begin
  if fGlyphLocator.Glyph <> grNone then
  begin
    Result := GlyphLocator.GraphicSize( GlyphState );
  end
  else if Assigned( Glyph ) and Assigned( Glyph.Graphic ) and not Glyph.Graphic.Empty then
  begin
    Result := TSize.Create( Glyph.Width, Glyph.Height );
  end
  else
  begin
    ImgList := Images;
    if Assigned( DisabledImages ) then
      ImgList := DisabledImages;
    if Assigned( ImgList ) then
      Result := TSize.Create( ImgList.Width, ImgList.Height );
  end;
end;

function TdcButton.HasIcon: boolean;
begin
  Result := ( GlyphLocator.Glyph <> grNone ) or ( Assigned( FGlyph ) and Assigned( FGlyph.Graphic ) and not FGlyph.Graphic.Empty ) or ( Assigned( fImages ) and InRange( FImageIndex, 0, Pred( fImages.Count ) ) );
end;

procedure TdcButton.MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: Integer );
begin
  inherited;
  State := State + [btPushed];
  if not Focused then
    SetFocus;
end;

procedure TdcButton.MouseMove( Shift: TShiftState; X, Y: Integer );
begin
  inherited;
  { Note: CMMouseLeave don't trigger if
    cursor is captured by MouseDown }
  if PtInRect( ClientRect, Point( X, Y ) ) then
    State := State + [btHot]
  else
    State := State - [btHot];
end;

procedure TdcButton.MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: Integer );
begin
  inherited;
  State := State - [btPushed];
end;

procedure TdcButton.Notification( AComponent: TComponent; Operation: TOperation );
begin
  inherited;
  if Operation <> opRemove then
    exit;
  if AComponent = fImages then
    Images := nil;
  if AComponent = FDisabledImages then
    DisabledImages := nil;
  if AComponent = fPushedImages then
    PushedImages := nil;
end;

procedure TdcButton.Paint;
var
  Buffer: TBitmap;
  BtnState: TdcButtonState;
begin
  inherited;
  Buffer := TBitmap.Create;
  try
    Buffer.Width := Width;
    Buffer.Height := Height;

    BtnState := fState;
    if default then
      Include( BtnState, btDefault );
    if Focused then
      Include( BtnState, btSelected );
    if not Enabled then
      Include( BtnState, btDisabled );

    PaintBackground( Buffer.Canvas, ClientRect, BtnState );
    PaintContent( Buffer.Canvas );

    Canvas.Draw( 0, 0, Buffer );
  finally
    FreeAndNil( Buffer );
  end;
end;

procedure TdcButton.PaintBackground( const ACanvas: TCanvas; const ButtonRect: TRect; const ButtonState: TdcButtonState );
begin
  PaintButton( ACanvas, Color, ButtonRect, ButtonState );
end;

procedure TdcButton.PaintContent( Canvas: TCanvas );
var
  CaptionAlignment: TAlignment;
  CaptionSize: TSize;
  CaptionRect, arrowRect: TRect;
  Spacing, ArrowLeft: Integer;
  CaptionLocation, IconLocation: TPoint;
  TextFormat: TTextFormat;
begin
  with Canvas do
  begin
    Font.Assign( Self.Font );

    AdjustFontColor( Canvas );

    CaptionSize := Canvas.GetTextSize( Caption );

    if HasIcon then
    begin

      Spacing := 0;
      if Caption <> '' then
        Spacing := GlyphSpacing;

      IconLocation := ClientRect.CenterSize( IconSize );

      with IconLocation do
        case Layout of
          blGlyphBottom:
            begin
              Inc( Y, CaptionSize.cy div 2 );
              PaintIcon( Canvas, IconLocation );
              Dec( Y, CaptionSize.cy );
            end;

          blGlyphLeft:
            begin
              Dec( X, ( CaptionSize.cx + Spacing ) div 2 );
              PaintIcon( Canvas, IconLocation );
              Inc( X, IconSize.cx + Spacing );
            end;

          blGlyphTop:
            begin
              Dec( Y, CaptionSize.cy div 2 );
              PaintIcon( Canvas, IconLocation );
              Inc( Y, IconSize.cy );
            end;

        else
          begin
            Inc( X, ( CaptionSize.cx + Spacing ) div 2 );

            PaintIcon( Canvas, IconLocation );

            Dec( X, Spacing + CaptionSize.cx );
          end;
        end;

      case Layout of
        blGlyphLeft, blGlyphRight:
          begin
            CaptionAlignment := taLeftJustify;
            CaptionRect := Bounds( IconLocation.X, 0, CaptionSize.cx, ClientHeight );

            case Layout of
              blGlyphRight: ArrowLeft := CaptionRect.Right + Spacing + IconSize.cx;
            else ArrowLeft := CaptionRect.Right;
            end;
          end
      else
        begin
          CaptionAlignment := System.Classes.taCenter;
          CaptionLocation := Point( RectWidth( ClientRect ) div 2 - CaptionSize.cx div 2, IconLocation.Y );

          CaptionRect := Bounds( CaptionLocation.X, CaptionLocation.Y, CaptionSize.cx, CaptionSize.cy );

          ArrowLeft := CaptionRect.Right;
        end;

      end;

      { Draw Text }
      TextFormat := [tfSingleLine, tfVerticalCenter, tfEndEllipsis];
      case CaptionAlignment of
        taLeftJustify: TextFormat := TextFormat + [tfLeft];
        taRightJustify: TextFormat := TextFormat + [tfRight];
      else TextFormat := TextFormat + [tfCenter];
      end;
      Canvas.RenderText( CaptionRect, Caption, TextFormat );
    end
    else
    begin
      CaptionRect := CenteredRect( ClientRect, Bounds( 0, 0, CaptionSize.cx, ClientHeight ) );
      ArrowLeft := CaptionRect.Right;

      TextFormat := [tfCenter, tfSingleLine, tfVerticalCenter, tfEndEllipsis];
      Canvas.RenderText( CaptionRect, Caption, TextFormat );
    end;

    if ShowArrow then
    begin
      arrowRect := Bounds( ArrowLeft, 0, GetArrowSize.cx, ClientHeight );
      Inc( arrowRect.Left, ArrowSpacing );
      DrawDropDownIndicator( Canvas, arrowRect, GetArrowSize, Enabled );
    end;
  end;
end;

procedure TdcButton.PaintIcon( Canvas: TCanvas; Location: TPoint );
var
  ImgList: TCustomImageList;
  Picture: TPicture;
begin
  if GlyphLocator.Glyph <> grNone then
  begin
    Draw( Canvas, GlyphLocator, Location.X, Location.Y, GlyphState );
    exit;
  end;

  Picture := FGlyph;
  if not Enabled and Assigned( fDisabledGlyph ) then
    Picture := fDisabledGlyph;

  if Assigned( Picture ) and Assigned( Picture.Graphic ) and not Picture.Graphic.Empty then
  begin
    DrawGraphic( Canvas, Location.X, Location.Y, Picture.Graphic );
  end
  else
  begin
    ImgList := fImages;
    if ( default or ( btPushed in fState ) ) and Assigned( fPushedImages ) then
      ImgList := fPushedImages;
    if not Enabled and Assigned( FDisabledImages ) then
      ImgList := FDisabledImages;
    if Assigned( ImgList ) and InRange( FImageIndex, 0, Pred( ImgList.Count ) ) then
      ImgList.Draw( Canvas, Location.X, Location.Y, FImageIndex );
  end
end;

procedure TdcButton.Set_ArrowSpacing( const Value: Integer );
begin
  if fArrowSpacing <> Value then
  begin
    fArrowSpacing := Value;
    Invalidate;
  end;
end;

procedure TdcButton.Set_Default( const Value: boolean );
begin
  if fDefault <> Value then
  begin
    fDefault := Value;
    if fDefault then
      Include( fState, btDefault )
    else
      Exclude( fState, btDefault );
    Invalidate;
  end;
end;

procedure TdcButton.Set_DisabledGlyph( const Value: TPicture );
begin
  fDisabledGlyph.Assign( Value );
  if Assigned( fDisabledGlyph ) then
    fDisabledGlyph.OnChange := DoGlyphChange;
  Invalidate;
end;

procedure TdcButton.Set_DisabledImages( const Value: TCustomImageList );
begin
  if Value <> FDisabledImages then
  begin
    FDisabledImages := Value;
    if FDisabledImages <> nil then
      FDisabledImages.FreeNotification( Self );
    Invalidate;
  end;
end;

procedure TdcButton.Set_Down( const Value: boolean );
begin
  if Value <> FDown then
  begin
    FDown := Value;
    Invalidate;
  end;
end;

procedure TdcButton.Set_EnableDblClick( const Value: boolean );
begin
  fEnableDblClick := Value;
end;

procedure TdcButton.Set_Glyph( const Value: TPicture );
begin
  FGlyph.Assign( Value );
  if Assigned( FGlyph ) then
    FGlyph.OnChange := DoGlyphChange;

  Invalidate;
end;

procedure TdcButton.Set_GlyphLocator( const Value: TGlyphLocator );
begin
  if Value <> fGlyphLocator then
  begin
    fGlyphLocator.Assign( Value );
    fGlyphLocator.OnChange := DoGlyphChange;
  end;
end;

procedure TdcButton.Set_GlyphSpacing( const Value: Integer );
begin
  if Value <> fGlyphSpacing then
  begin
    fGlyphSpacing := Value;
    Invalidate;
  end;
end;

procedure TdcButton.Set_ImageIndex( const Value: TImageIndex );
begin
  if Value <> FImageIndex then
  begin
    FImageIndex := Value;
    Invalidate;
  end;
end;

procedure TdcButton.Set_Images( const Value: TCustomImageList );
begin
  if Value <> fImages then
  begin
    fImages := Value;
    if fImages <> nil then
      fImages.FreeNotification( Self );
    Invalidate;
  end;
end;

procedure TdcButton.Set_Layout( const Value: TButtonLayout );
begin
  if Value <> fLayout then
  begin
    fLayout := Value;
    Invalidate;
  end;
end;

procedure TdcButton.Set_ShowArrow( const Value: boolean );
begin
  if Value <> fShowArrow then
  begin
    fShowArrow := Value;
    Invalidate;
  end;
end;

procedure TdcButton.Set_State( const Value: TdcButtonState );
begin
  if Value <> fState then
  begin
    fState := Value;
    Invalidate;
  end;
end;

procedure TdcButton.Set_Transparent( const Value: boolean );
begin
  fTransparent := Value;
end;

procedure TdcButton.Set_TransparentColor( const Value: TColor );
begin
  fTransparentColor := Value;
  if Glyph.Graphic is TBitmap then
    with Glyph.Graphic as TBitmap do
    begin
      TransparentColor := fTransparentColor;
      Transparent := fTransparentColor <> clNone;
    end;
end;

procedure TdcButton.WMEraseBkgnd( var Message: TWmEraseBkgnd );
begin
  inherited;
  message.Result := 1;
end;

procedure TdcButton.Set_PushedImages( const Value: TCustomImageList );
begin
  if Value <> fPushedImages then
  begin
    fPushedImages := Value;
    if fPushedImages <> nil then
      fPushedImages.FreeNotification( Self );
    Invalidate;
  end;
end;

{ TdcFlatButton }

procedure TdcFlatButton.AdjustFontColor( const ACanvas: TCanvas );
begin
  case Style of
    bcDefault:
      begin
        if ( btDefault in State ) or ( ( btPushed in State ) and ( btHot in State ) ) then
          ACanvas.Font.Color := clWhite;

        if ( btDisabled in State ) then
          ACanvas.Font.Color := BlendColor( clGrayText, Color, 128 );
      end;
    bcLight:
      begin
        ACanvas.Font.Color := clWhite;

        if ( btDisabled in State ) then
          ACanvas.Font.Color := BlendColor( ACanvas.Font.Color, Color, 128 );
      end;
  end;

end;

constructor TdcFlatButton.Create( AOwner: TComponent );
begin
  inherited;
  fStyle := bcDefault;
end;

procedure TdcFlatButton.PaintBackground( const ACanvas: TCanvas; const ButtonRect: TRect; const ButtonState: TdcButtonState );
begin
  case Style of
    bcDefault: inherited;
    bcLight: PaintLightButton( ACanvas, Color, ClientRect, State, false );
    bcDark: PaintDarkButton( ACanvas, Color, ClientRect, State, false );
  end;
end;

procedure TdcFlatButton.Set_Style( const Value: TdcButtonColorStyle );
begin
  if Value <> fStyle then
  begin
    fStyle := Value;
    Invalidate;
  end;
end;

{ TdcColorSpeedButton }

constructor TdcColorSpeedButton.Create( AOwner: TComponent );
begin
  inherited;
  fSelected := clBlue;
end;

procedure TdcColorSpeedButton.Set_Selected( const Value: TColor );
begin
  if Value <> fSelected then
  begin
    fSelected := Value;
    if Assigned( fOnSelectedChanged ) then
      fOnSelectedChanged( Self );
  end;
end;

{ TdcToolButtonDraw }

procedure TdcToolButtonDraw.Add( ToolBar: TToolbar );
begin
  ToolBar.DrawingStyle := TTBDrawingStyle.dsNormal;
  ToolBar.EdgeBorders := [];
  ToolBar.Flat := true;
  ToolBar.GradientDrawingOptions := [];
  ToolBar.OnCustomDrawButton := DoCustomDrawButton;
  ToolBar.OnAdvancedCustomDrawButton := DoAdvancedCustomDrawButton;
end;

procedure TdcToolButtonDraw.DoAdvancedCustomDrawButton( Sender: TToolbar; Button: TToolButton; State: TCustomDrawState; Stage: TCustomDrawStage; var Flags: TTBCustomDrawFlags; var DefaultDraw: boolean );
begin
end;

procedure TdcToolButtonDraw.DoCustomDrawButton( Sender: TToolbar; Button: TToolButton; State: TCustomDrawState; var DefaultDraw: boolean );
var
  Buffer: TBitmap;
begin
  DefaultDraw := false;
  Buffer := TBitmap.Create;
  try
    Buffer.SetSize( Button.Width, Button.Height );
    Buffer.Canvas.Brush.Color := Sender.Color;
    DrawToolButton( Buffer.Canvas, Button, Sender.Images, Sender.DisabledImages, State, Sender.ShowCaptions, Sender.List );
    Sender.Canvas.Draw( Button.BoundsRect.Left, Button.BoundsRect.Top, Buffer );
  finally
    Buffer.Free;
  end;
end;

class procedure TdcToolButtonDraw.DrawToolButton( Canvas: TCanvas; Button: TToolButton; Images, DisabledImages: TCustomImageList; State: TCustomDrawState; ShowCaptions, List: boolean );
const
  DropDownIndicatorSize = 24;
var
  ButtonRect, IconRect, CaptionRect, DropDownRect, ContentRect: TRect;
  ContentSize: TSize;
  IconLocation: TPoint;
  imageList: TCustomImageList;

  function GetContentSize: TSize;
  var
    Caption: string;
    CalcRect: TRect;
  begin
    Result := TSize.Create( 0, 0 );
    if Assigned( Images ) and Images.Exists( Button.ImageIndex ) then
    begin
      Result := TSize.Create( Images.Width, Images.Height );
    end;
    if ShowCaptions and ( Button.Caption <> EmptyStr ) then
    begin
      Caption := Button.Caption;
      Canvas.textRect( CalcRect, Caption, [tfLeft, tfTop, tfSingleLine, tfCalcRect] );
      if Result.cy <> 0 then
        Inc( Result.cy, 2 );
      Result.cy := Result.cy + CalcRect.Height;
      Result.cx := Max( Result.cx, CalcRect.Width );
    end;
  end;

begin
  ButtonRect := TRect.Create( 0, 0, Button.BoundsRect.Width, Button.BoundsRect.Height );

  if cdsDisabled in State then
  begin
    Canvas.Font.Color := clGrayText;
  end
  else
  begin
    if cdsHot in State then
      Canvas.Brush.Color := clToolButtonHot;
    if cdsSelected in State then
    begin
      Canvas.Brush.Color := clToolButtonPressed;
      Canvas.Font.Color := clWhite;
    end;
    if cdsChecked in State then
      Canvas.Brush.Color := clToolButtonChecked;
  end;
  Canvas.FillRect( ButtonRect );

  if Assigned( Button.DropdownMenu ) then
  begin
    DropDownRect := ButtonRect;
    DropDownRect.Left := DropDownRect.Right - DropDownIndicatorSize;
    DrawDropDownIndicator( Canvas, DropDownRect, TSize.Create( 8, 4 ), Button.Enabled );
  end;

  ContentSize := GetContentSize;
  ContentRect := CenteredRect( ButtonRect, Bounds( 0, 0, ContentSize.cx, ContentSize.cy ) );

  //if Assigned( Images ) then
  // NOTE: There is no point using assigned() since TComponents will never be
  // decoupled during a method call. Decoupling only happens between calls.
  // Assign() also checks the instance reference validity, which is considerably
  // slower and should be avoided in redraw operations.
  if Images <> nil then
  begin
    // Check if the toolbar is in LIST display mode
    // If so, the glyph is placed leftmost followed by the caption
    if List then
    begin
      if Images.Exists( Button.ImageIndex ) then
      begin
        // Setup our target rect for the glyph
        IconRect := ButtonRect;

        // Only adjust glyph sub-rect if there actually is
        // a caption to render at the end of this code block
        if ShowCaptions then
        begin
          // Make sure sub-rect for glyph is always dividable by 2.
          // Somewhat optimized this for speed, which is probably overkill
          // for anything UI related (e.g separate inc is faster than +1 etc)
          var wd := images.Width;
          inc(wd);
          if wd mod 2 <> 0 then
            IconRect.Width := Images.Width
          else
            IconRect.Width := wd;
        end;

        // Center by width / height of target rect, this will now always be
        // equally portioned on both sides due to division check (above)
        var dx := (iconRect.Width div 2 ) - (images.width div 2);
        var dy := (iconRect.Height div 2) - (images.Height div 2);

        // Draw glyph based on state
        if Button.Enabled then
          Images.Draw( Canvas, dx, dy, Button.ImageIndex )
        else
          Images.DrawBlended( Canvas, dx, dy, Button.ImageIndex, 92 );

        // Only draw caption if it's there, otherwise the glyph will
        // already be centered (see code above)
        if ShowCaptions then
        begin
          // offset by iconrect (since that is already adjusted for division)
          var TextRect := ButtonRect;
          inc(TextRect.Left, IconRect.Width);
          Canvas.RenderText( TextRect, Button.Caption, [tfSingleLine, tfCenter, tfVerticalCenter] );
        end;
      end else
      begin
        // No glyph? Just draw the text centered based on alotted button rect
        if ShowCaptions then
          Canvas.RenderText( ButtonRect, Button.Caption, [tfSingleLine, tfCenter, tfVerticalCenter] );
      end;

      exit;
    end;

    IconRect := ContentRect;
    IconRect.Height := Images.Height;
    IconLocation := ContentRect.Location;
    Inc( IconLocation.X, IconRect.Width div 2 - Images.Width div 2 );

    // if Button.Enabled then
    // imageList := Images
    // else
    // imageList := DisabledImages;
    imageList := Images;

    if Assigned( imageList ) then
      if imageList.Exists( Button.ImageIndex ) then
      begin
        if Button.Enabled then
          imageList.Draw( Canvas, IconLocation.X, IconLocation.Y, Button.ImageIndex )
        else
          imageList.DrawBlended( Canvas, IconLocation.X, IconLocation.Y, Button.ImageIndex, 92 );
      end;

    if ShowCaptions then
    begin
      CaptionRect := ContentRect;
      CaptionRect.Top := IconRect.Bottom + 2;
      Canvas.RenderText( CaptionRect, Button.Caption, [tfSingleLine, tfCenter] );
    end;
  end;
end;

procedure TdcToolButtonDraw.Set_DesignTime( const Value: boolean );
begin
  fDesignTime := Value;
end;

procedure TdcToolButtonDraw.Set_Toolbar( const Value: TToolbar );
begin
  if Value <> fToolbar then
  begin
    fToolbar := Value;
    if ( csDesigning in ComponentState ) and not fDesignTime then
      exit;

    if Assigned( fToolbar ) then
      Add( fToolbar );
  end;
end;

{ TdcSpinButtons }

constructor TdcRepeatButton.Create( AOwner: TComponent );
begin
  inherited;
  ControlStyle := ControlStyle - [csDoubleClicks];
  FDelayTimer := TTimer.Create( nil );
  FDelayTimer.Enabled := false;
  FDelayTimer.Interval := DELAY_INTERVAL;
  FDelayTimer.OnTimer := DoDelayTimer;
  FRepeatTimer := TTimer.Create( nil );
  FRepeatTimer.Enabled := false;
  FRepeatTimer.Interval := REPEAT_INTERVAL;
  FRepeatTimer.OnTimer := DoRepeatTimer;
end;

destructor TdcRepeatButton.Destroy;
begin
  FDelayTimer.Free;
  FRepeatTimer.Free;
  inherited;
end;

procedure TdcRepeatButton.DoDelayTimer( Sender: TObject );
begin
  FDelayTimer.Enabled := false;
  FRepeatTimer.Enabled := true;
end;

procedure TdcRepeatButton.DoRepeatTimer( Sender: TObject );
begin
  if Assigned( OnClick ) then
    OnClick( Self );
end;

procedure TdcRepeatButton.MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: Integer );
begin
  inherited;
  FDelayTimer.Enabled := true;
end;

procedure TdcRepeatButton.MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: Integer );
begin
  inherited;
  FDelayTimer.Enabled := false;
  FRepeatTimer.Enabled := false;
end;

procedure TdcRepeatButton.WMActivateApp( var Message: TWMActivateApp );
begin
  inherited;
  if not message.Active then
  begin
    FDelayTimer.Enabled := false;
    FRepeatTimer.Enabled := false;
  end;
end;

end.
