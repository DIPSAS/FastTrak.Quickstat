unit Emetra.Vcl.StdCtrls;

interface

uses
  System.Classes, System.Types, System.SysUtils,
  Vcl.Controls, Vcl.StdCtrls, Vcl.ImgList, Vcl.Graphics, Vcl.Imaging.pngimage,
  System.UITypes,
  Emetra.Vcl.Consts, Emetra.Vcl.Types, Emetra.Vcl.Intf, Emetra.Vcl.Controls,
  Winapi.Windows, Winapi.Messages;

type
  { TdcLabel }

  TdcLabelKind = ( lkCustom, lkClose, lkInfo, lkMandatory, lkWarning );

  TdcLabel = class( TGraphicControl )
  private
    fEllipsisPosition: TEllipsisPosition;
    fImageAlignment: TVerticalAlignment;
    fImageIndex: TImageIndex;
    fImages: TCustomImageList;
    fKind: TdcLabelKind;
    fPadding: TPadding;
    fSpacing: integer;
    fTransparentSet: boolean;
    fWordWrap: boolean;
    function Get_CaptionSize: TSize;
    function Get_ImageSize: TSize;
    function Get_Transparent: boolean;
    procedure Set_EllipsisPosition( const Value: TEllipsisPosition );
    procedure Set_ImageAlignment( const Value: TVerticalAlignment );
    procedure Set_ImageIndex( const Value: TImageIndex );
    procedure Set_Images( const Value: TCustomImageList );
    procedure Set_Kind( const Value: TdcLabelKind );
    procedure Set_Padding( const Value: TPadding );
    procedure Set_Spacing( const Value: integer );
    procedure Set_Transparent( const Value: boolean );
    procedure Set_WordWrap( const Value: boolean );
  protected
    function CanAutoSize( var NewWidth, NewHeight: integer ): boolean; override;
    procedure DoImagesChange( Sender: TObject );
    procedure DoPaddingChange( Sender: TObject );
    function HasImage: boolean;
    procedure Notification( AComponent: TComponent; Operation: TOperation ); override;
    procedure Paint; override;
    function GetKindGlyph: TGraphic;
    { Delphi Messages }
    procedure CMFontChanged( var Message: TMessage ); message CM_FONTCHANGED;
    procedure CMTextChanged( var Message: TMessage ); message CM_TEXTCHANGED;
    property CaptionSize: TSize read Get_CaptionSize;
    property ImageSize: TSize read Get_ImageSize;
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
  published
    property Action;
    property Align;
    property Anchors;
    property AutoSize;
    property Caption;
    property Color;
    property Constraints;
    property Cursor;
    property DragCursor;
    property DragKind;
    property DragMode;
    property EllipsisPosition: TEllipsisPosition read fEllipsisPosition write Set_EllipsisPosition default epNone;
    property Font;
    property ImageAlignment: TVerticalAlignment read fImageAlignment write Set_ImageAlignment default taAlignTop;
    property ImageIndex: TImageIndex read fImageIndex write Set_ImageIndex;
    property Images: TCustomImageList read fImages write Set_Images;
    property Kind: TdcLabelKind read fKind write Set_Kind default lkCustom;
    property Padding: TPadding read fPadding write Set_Padding;
    property ParentColor;
    property ParentFont;
    property ParentShowHint;
    property ShowHint;
    property Spacing: integer read fSpacing write Set_Spacing default 4;
    property Transparent: boolean read Get_Transparent write Set_Transparent stored fTransparentSet;
    property Visible;
    property WordWrap: boolean read fWordWrap write Set_WordWrap default false;
    property OnCanResize;
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

  { TdcBorderedControl }

  TdcBorderedControl = class( TCustomControl )
  private
    fBorderColor: TColor;
    fShowBorder: boolean;
    procedure Set_BorderColor( const Value: TColor );
    procedure Set_ShowBorder( const Value: boolean );
  protected
    procedure AdjustClientRect( var Rect: TRect ); override;
    procedure DoPaddingChange( Sender: TObject );
    function GetBackgroundColor: TColor; virtual;
    function GetBorderColor: TColor; virtual;
    function GetBorderSize: integer; virtual;
    procedure UpdateNCRect; virtual;
    procedure InvalidateNC;
    { Winapi Messages }
    procedure WMEraseBkgnd( var Message: TWmEraseBkgnd ); message WM_ERASEBKGND;
    procedure WMNCCalcSize( var Message: TWMNCCalcSize ); message WM_NCCALCSIZE;
    procedure WMNCPaint( var Message: TWMNCPaint ); message WM_NCPAINT;
    procedure WMSize( var Message: TWMSize ); message WM_SIZE;
  public
    constructor Create( AOwner: TComponent ); override;
  published
    property Align;
    property Anchors;
    property AutoSize;
    property BorderColor: TColor read fBorderColor write Set_BorderColor default clGray;
    property Caption;
    property Color;
    property Constraints;
    property Font;
    property Padding;
    property ParentColor;
    property ParentFont;
    property ShowBorder: boolean read fShowBorder write Set_ShowBorder default true;
    property ShowHint;
    property TabOrder;
    property TabStop;
    property Visible;
  end;

  { TdcBorderedPanel }

  TdcBorderedPanel = class( TdcBorderedControl )
  private
    fBorderColor: TColor;
    fShowBorder: boolean;
    procedure Set_BorderColor( const Value: TColor );
    procedure Set_ShowBorder( const Value: boolean );
  published
    property BorderColor: TColor read fBorderColor write Set_BorderColor default clGray;
    property ShowBorder: boolean read fShowBorder write Set_ShowBorder default true;
  end;

  { TdcPanel }

  TdcPanelKind = ( pkInfo, pkWarning, pkError, pkOther );

  TdcPanel = class( TdcBorderedPanel )
  private
    fAlignment: TAlignment;
    fPanelKind: TdcPanelKind;
    fShowBackground: boolean;
    fShowCaption: boolean;
    function Get_CaptionRect: TRect;
    procedure Set_Alignment( const Value: TAlignment );
    procedure Set_PanelKind( const Value: TdcPanelKind );
    procedure Set_ShowBackground( const Value: boolean );
    procedure Set_ShowCaption( const Value: boolean );
  protected
    function GetBackgroundColor: TColor; override;
    function GetBorderColor: TColor; override;
    procedure Paint; override;
    { Delphi Messages }
    procedure CMTextChanged( var Message: TMessage ); message CM_TEXTCHANGED;
  public
    constructor Create( AOwner: TComponent ); override;
    { Public Properties }
    property CaptionRect: TRect read Get_CaptionRect;
  published
    { Properties }
    property Alignment: TAlignment read fAlignment write Set_Alignment default taCenter;
    property PanelKind: TdcPanelKind read fPanelKind write Set_PanelKind default pkOther;
    property ShowBackground: boolean read fShowBackground write Set_ShowBackground default true;
    property ShowCaption: boolean read fShowCaption write Set_ShowCaption default true;
  end;

  { TdcDialogContentPanel }

  TdcDialogContentPanel = class( TdcBorderedControl )
  protected
    function GetBorderSize: integer; override;
    procedure Paint; override;
    { Windows Messages }
    procedure WMNCPaint( var Message: TWMNCPaint ); message WM_NCPAINT;
  public
    constructor Create( AOwner: TComponent ); override;
  published
    property Color default clDlgContent;
  end;

  { TdcCheckBox }

  TdcCheckArea = ( caCheckMarkAndText, caCheckMark, caClientRect );

  TdcCheckBoxStyle = ( ccCheckBox, ccToggle );

  TdcCheckBox = class( TdcControl, IValueAccessors )
  private
    fAlignment: TAlignment;
    fAllowGrayed: boolean;
    fButtonState: TdcButtonState;
    fCheckArea: TdcCheckArea;
    fIndent: integer;
    fOnChange: TNotifyEvent;
    fState: TCheckBoxState;
    fStyle: TdcCheckBoxStyle;
    fTagString: string;
    fVerticalAlignment: TVerticalAlignment;
    { Property Accessors }
    function GetCheckBoxRect: TRect;
    function GetChecked: boolean;
    function GetTagString: string;
    function GetTextRect: TRect;
    procedure Set_Alignment( const Value: TAlignment );
    procedure Set_ButtonState( const Value: TdcButtonState );
    procedure Set_Checked( const Value: boolean );
    procedure Set_Indent( const Value: integer );
    procedure Set_State( const Value: TCheckBoxState );
    procedure Set_Style( const Value: TdcCheckBoxStyle );
    procedure Set_TagString( const Value: string );
    procedure Set_VerticalAlignment( const Value: TVerticalAlignment );
  protected
    { IValueAccessors }
    function GetAsBoolean: boolean; virtual;
    function GetAsDate: TDate; virtual;
    function GetAsDateTime: TDateTime; virtual;
    function GetAsFloat: Double; virtual;
    function GetAsInteger: integer; virtual;
    function GetAsString: string; virtual;
    procedure SetAsBoolean( const Value: boolean ); virtual;
    procedure SetAsDate( const Value: TDate ); virtual;
    procedure SetAsDateTime( const Value: TDateTime ); virtual;
    procedure SetAsFloat( const Value: Double ); virtual;
    procedure SetAsInteger( const Value: integer ); virtual;
    procedure SetAsString( const Value: string ); virtual;
    { Event Handlers }
    procedure DoChange; dynamic;
    procedure DoImagesChange( Sender: TObject );
    { Methods }
    function InActiveArea( X, Y: integer ): boolean;
    procedure KeyDown( var Key: Word; Shift: TShiftState ); override;
    procedure MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: integer ); override;
    procedure MouseMove( Shift: TShiftState; X, Y: integer ); override;
    procedure MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: integer ); override;
    { TControl }
    function CanAutoSize( var NewWidth, NewHeight: integer ): boolean; override;
    { TCustomControl }
    procedure Paint; override;
    { Delphi Messages }
    procedure CMFontChanged( var Message: TMessage ); message CM_FONTCHANGED;
    procedure CMMouseEnter( var Message: TMessage ); message CM_MOUSEENTER;
    procedure CMMouseLeave( var Message: TMessage ); message CM_MOUSELEAVE;
    procedure CMTextChanged( var Message: TMessage ); message CM_TEXTCHANGED;
    { Windows Messages }
    procedure WMKillFocus( var Msg: TWMKillFocus ); message WM_KILLFOCUS;
    procedure WMLButtonUp( var Message: TWMLButtonUp ); message WM_LBUTTONUP;
    procedure WMSetFocus( var Message: TWMSetFocus ); message WM_SETFOCUS;
    { Properties }
    property CheckBoxRect: TRect read GetCheckBoxRect;
    property TextRect: TRect read GetTextRect;
  public
    constructor Create( AOwner: TComponent ); override;
    class procedure DrawCheckBox( Canvas: TCanvas; Style: TdcCheckBoxStyle; Dest: TRect; State: TdcButtonState ); virtual;

    procedure Toggle;
    { Properties }
    property ButtonState: TdcButtonState read fButtonState write Set_ButtonState;
  published
    property Align;
    property Alignment: TAlignment read fAlignment write Set_Alignment default taLeftJustify;
    property AllowGrayed: boolean read fAllowGrayed write fAllowGrayed default false;
    property AutoSize;
    property Caption;
    property CheckArea: TdcCheckArea read fCheckArea write fCheckArea default caCheckMarkAndText;
    property Checked: boolean read GetChecked write Set_Checked default false;
    property Color;
    property Cursor default crHandPoint;
    property Enabled;
    property Font;
    property Indent: integer read fIndent write Set_Indent default CheckBoxIndent;
    property Padding;
    property ParentColor;
    property ParentFont;
    property ParentShowHint;
    property State: TCheckBoxState read fState write Set_State default cbUnchecked;
    property Style: TdcCheckBoxStyle read fStyle write Set_Style default ccCheckBox;
    property TabOrder;
    property TabStop;
    property TagString: string read GetTagString write Set_TagString;
    property VerticalAlignment: TVerticalAlignment read fVerticalAlignment write Set_VerticalAlignment default taVerticalCenter;
    property Visible;

    property OnChange: TNotifyEvent read fOnChange write fOnChange;
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

  { TdcIconListControl }

  TdcIconListControl = class( TdcBorderedControl )
  private
    fHotIndex: integer;
    fImages: TCustomImageList;
    fItemIndex: integer;
    fItems: TStrings;
    fItemWidth: integer;
    fOnSelect: TdcIndexNotifyEvent;
    fPushedIndex: integer;
    fTextHeight: integer;
    procedure Set_HotIndex( const Value: integer );
    procedure Set_Images( const Value: TCustomImageList );
    procedure Set_ItemIndex( const Value: integer );
    procedure Set_Items( const Value: TStrings );
    procedure Set_ItemWidth( const Value: integer );
    procedure Set_PushedIndex( const Value: integer );
    procedure Set_TextHeight( const Value: integer );
  protected
    { Keyboard & Mouse }
    procedure MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: integer ); override;
    procedure MouseMove( Shift: TShiftState; X, Y: integer ); override;
    procedure MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: integer ); override;
    { Paint }
    procedure Paint; override;
    procedure PaintIcon( const AIconRect: TRect; const AImageIndex: TImageIndex; const ButtonState: TdcButtonState ); virtual;
    procedure RefreshItem( const Index: integer );
    { Delphi Messages }
    procedure CMMouseLeave( var Message: TMessage ); message CM_MOUSELEAVE;
    { Properties }
    property HotIndex: integer read fHotIndex write Set_HotIndex;
    property PushedIndex: integer read fPushedIndex write Set_PushedIndex;
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
    { Public Methods }
    function GetItemAtPos( X, Y: integer ): integer;
    function GetItemRect( const Index: integer ): TRect;
    function ItemExists( const Index: integer ): boolean;
  published
    property Color default clWindow;
    property Images: TCustomImageList read fImages write Set_Images;
    property ItemIndex: integer read fItemIndex write Set_ItemIndex default -1;
    property Items: TStrings read fItems write Set_Items;
    property ItemWidth: integer read fItemWidth write Set_ItemWidth default 52;
    property ParentColor default false;
    property TextHeight: integer read fTextHeight write Set_TextHeight default 40;
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
    property OnSelect: TdcIndexNotifyEvent read fOnSelect write fOnSelect;
    property OnStartDock;
    property OnStartDrag;
  end;

implementation

uses
  System.Math,
  Emetra.Vcl.Graphics,
  Emetra.Vcl.Glyphs,
  Emetra.Vcl.Helpers;

var
  CircleCloseGlyph, CircleInfoGlyph, MandatoryGlyph, WarningGlyph: TPngImage;

  { TdcLabel }

function TdcLabel.CanAutoSize( var NewWidth, NewHeight: integer ): boolean;
begin
  Result := inherited CanAutoSize( NewWidth, NewHeight );
  NewWidth := CaptionSize.cx;
  if HasImage then
    Inc( NewWidth, ImageSize.cx + Spacing );
  Inc( NewWidth, Padding.Width );
  NewHeight := Max( ImageSize.cy, CaptionSize.cy ) + Padding.Height;
end;

procedure TdcLabel.CMFontChanged( var Message: TMessage );
begin
  inherited;
  Invalidate;
end;

procedure TdcLabel.CMTextChanged( var Message: TMessage );
begin
  inherited;
  Invalidate;
end;

constructor TdcLabel.Create( AOwner: TComponent );
begin
  inherited;
  ControlStyle := [csDoubleClicks, csReplicatable, csSetCaption];
  fEllipsisPosition := epNone;
  fImageAlignment := taAlignTop;
  fImageIndex := -1;
  fImages := nil;
  fKind := lkCustom;
  fPadding := TPadding.Create( nil );
  fPadding.OnChange := DoPaddingChange;
  fSpacing := 4;
  fWordWrap := false;
end;

destructor TdcLabel.Destroy;
begin
  fPadding.Free;
  inherited;
end;

procedure TdcLabel.DoImagesChange( Sender: TObject );
begin
  Invalidate;
end;

procedure TdcLabel.DoPaddingChange( Sender: TObject );
begin
  Invalidate;
end;

function TdcLabel.GetKindGlyph: TGraphic;
begin
  case Kind of
    lkClose: Result := CircleCloseGlyph;
    lkInfo: Result := CircleInfoGlyph;
    lkMandatory: Result := MandatoryGlyph;
    lkWarning: Result := WarningGlyph;
  else Result := nil;
  end;
end;

function TdcLabel.Get_CaptionSize: TSize;
var
  TextFormat: TTextFormat;
  TextRect: TRect;
  s: string;
begin
  TextFormat := [tfLeft, tfTop, tfCalcRect];
  if WordWrap then
    Include( TextFormat, tfWordBreak )
  else
    Include( TextFormat, tfSingleLine );
  case EllipsisPosition of
    epEndEllipsis: Include( TextFormat, tfEndEllipsis );
    epPathEllipsis: Include( TextFormat, tfPathEllipsis );
    epWordEllipsis: Include( TextFormat, tfWordEllipsis );
  end;
  s := Caption;
  TextRect := ClientRect;
  TextRect.Inflate( Padding.Left, Padding.Top, Padding.Right, Padding.Bottom );
  if HasImage then
    Inc( TextRect.Left, ImageSize.cx + Spacing );
  Canvas.Font.Assign( Font );
  Canvas.TextRect( TextRect, s, TextFormat );
  Result := TSize.Create( TextRect.Width, TextRect.Height );
end;

function TdcLabel.Get_ImageSize: TSize;
begin
  if HasImage then
  begin
    case Kind of
      lkCustom: Result := TSize.Create( fImages.Width, fImages.Height );
    else Result := TSize.Create( GetKindGlyph.Width, GetKindGlyph.Height );
    end;
  end
  else
    Result := TSize.Create( 0, 0 );
end;

function TdcLabel.Get_Transparent: boolean;
begin
  Result := not( csOpaque in ControlStyle );
end;

function TdcLabel.HasImage: boolean;
begin
  Result := ( Assigned( fImages ) and InRange( fImageIndex, 0, Pred( fImages.Count ) ) ) or ( Kind <> lkCustom );
end;

procedure TdcLabel.Notification( AComponent: TComponent; Operation: TOperation );
begin
  inherited;
  if ( AComponent = fImages ) and ( Operation = opRemove ) then
    Images := nil;
end;

procedure TdcLabel.Paint;
var
  X, Y, ImageY: integer;
  CaptionRect: TRect;
  TextFormat: TTextFormat;
begin
  inherited;
  with Canvas do
  begin
    if not Transparent then
    begin
      Brush.Color := Self.Color;
      Brush.Style := bsSolid;
      FillRect( ClientRect );
    end;
  end;
  X := Padding.Left;
  Y := Padding.Top;
  ImageY := Y;
  if HasImage then
  begin
    case ImageAlignment of
      taAlignTop: ImageY := Y;
      taAlignBottom: ImageY := ClientHeight - Padding.Bottom - ImageSize.cy;
      taVerticalCenter: ImageY := ClientHeight div 2 - ImageSize.cy div 2;
    end;
    if Kind = lkCustom then
      Images.Draw( Canvas, X, ImageY, ImageIndex )
    else
      Canvas.Draw( X, ImageY, GetKindGlyph );
    Inc( X, ImageSize.cx + Spacing );
  end;
  TextFormat := [tfLeft];
  if WordWrap then
    TextFormat := TextFormat + [tfWordBreak, tfTop]
  else
    TextFormat := TextFormat + [tfSingleLine, tfVerticalCenter];

  case EllipsisPosition of
    epEndEllipsis: Include( TextFormat, tfEndEllipsis );
    epPathEllipsis: Include( TextFormat, tfPathEllipsis );
    epWordEllipsis: Include( TextFormat, tfWordEllipsis );
  end;

  CaptionRect := Rect( X, Y, ClientRect.Right - Padding.Right, ClientRect.Bottom - Padding.Bottom );
  Canvas.Font.Assign( Font );
  if not Enabled then
    Canvas.Font.Color := BlendColor( Canvas.Font.Color, Color, intTextDisabledAlpha );

  Canvas.RenderText( CaptionRect, Caption, TextFormat );
end;

procedure TdcLabel.Set_EllipsisPosition( const Value: TEllipsisPosition );
begin
  if Value <> fEllipsisPosition then
  begin
    fEllipsisPosition := Value;
    Invalidate;
  end;
end;

procedure TdcLabel.Set_ImageAlignment( const Value: TVerticalAlignment );
begin
  if Value <> fImageAlignment then
  begin
    fImageAlignment := Value;
    Invalidate;
  end;
end;

procedure TdcLabel.Set_ImageIndex( const Value: TImageIndex );
begin
  if Value <> fImageIndex then
  begin
    fImageIndex := Value;
    Invalidate;
  end;
end;

procedure TdcLabel.Set_Images( const Value: TCustomImageList );
begin
  if Value <> fImages then
  begin
    if Assigned( fImages ) then
      fImages.RemoveFreeNotification( Self );
    fImages := Value;
    if Assigned( fImages ) then
    begin
      fImages.OnChange := DoImagesChange;
      fImages.FreeNotification( Self );
    end;
  end;
end;

procedure TdcLabel.Set_Kind( const Value: TdcLabelKind );
begin
  if Value <> fKind then
  begin
    fKind := Value;
    Invalidate;
  end;
end;

procedure TdcLabel.Set_Padding( const Value: TPadding );
begin
  fPadding.Assign( Value );
  fPadding.OnChange := DoPaddingChange;
end;

procedure TdcLabel.Set_Spacing( const Value: integer );
begin
  if Value <> fSpacing then
  begin
    fSpacing := Value;
    Invalidate;
  end;
end;

procedure TdcLabel.Set_Transparent( const Value: boolean );
begin
  if Transparent <> Value then
  begin
    if Value then
      ControlStyle := ControlStyle - [csOpaque]
    else
      ControlStyle := ControlStyle + [csOpaque];
    Invalidate;
  end;
  fTransparentSet := true;
end;

procedure TdcLabel.Set_WordWrap( const Value: boolean );
begin
  if Value <> fWordWrap then
  begin
    fWordWrap := Value;
    Invalidate;
  end;
end;

{ TdcBorderedControl }

procedure TdcBorderedControl.AdjustClientRect( var Rect: TRect );
begin
  inherited;

end;

constructor TdcBorderedControl.Create( AOwner: TComponent );
begin
  inherited;
  ControlStyle := [csAcceptsControls, csDoubleClicks, csOpaque, csSetCaption];
  fBorderColor := clGray;
  fShowBorder := true;
end;

procedure TdcBorderedControl.DoPaddingChange( Sender: TObject );
begin
  Invalidate;
  Realign;
end;

function TdcBorderedControl.GetBackgroundColor: TColor;
begin
  Result := Color;
end;

function TdcBorderedControl.GetBorderColor: TColor;
begin
  Result := BorderColor;
end;

function TdcBorderedControl.GetBorderSize: integer;
begin
  Result := 1;
end;

procedure TdcBorderedControl.InvalidateNC;
var
  R: TRect;
begin
  inherited;
  R := ClientRect;
  RedrawWindow( Handle, @R, 0, RDW_FRAME or RDW_INVALIDATE );
end;

procedure TdcBorderedControl.Set_BorderColor( const Value: TColor );
begin
  if Value <> fBorderColor then
  begin
    fBorderColor := Value;
    UpdateNCRect;
  end;
end;

procedure TdcBorderedControl.Set_ShowBorder( const Value: boolean );
begin
  if Value <> fShowBorder then
  begin
    fShowBorder := Value;
    UpdateNCRect;
  end;
end;

procedure TdcBorderedControl.UpdateNCRect;
begin
  if HandleAllocated then
    SetWindowPos( Handle, 0, Left, Top, Width, Height, SWP_FRAMECHANGED or SWP_NOREPOSITION );
end;

procedure TdcBorderedControl.WMEraseBkgnd( var Message: TWmEraseBkgnd );
begin
  message.Result := 1;
end;

procedure TdcBorderedControl.WMNCCalcSize( var Message: TWMNCCalcSize );
var
  Params: PNCCalcSizeParams;
  borderSize: integer;
begin
  Params := message.CalcSize_Params;
  with Params^ do
  begin
    if ShowBorder then
      borderSize := GetBorderSize
    else
      borderSize := 0;
    rgrc[0].Inflate( -borderSize, -borderSize );
  end;
end;

procedure TdcBorderedControl.WMNCPaint( var Message: TWMNCPaint );
var
  DC: HDC;
  Pen: HPEN;
  borderRect: TRect;
begin
  if ShowBorder then
  begin
    DC := GetWindowDC( Handle );
    try
      Pen := CreatePen( PS_SOLID, 1, ColorToRGB( GetBorderColor ) );
      try
        borderRect := Rect( 0, 0, Width, Height );
        SelectObject( DC, Pen );
        SelectObject( DC, GetStockObject( NULL_BRUSH ) );
        Rectangle( DC, borderRect.Left, borderRect.Top, borderRect.Right, borderRect.Bottom );
      finally
        DeleteObject( Pen );
      end;
    finally
      ReleaseDC( Handle, DC );
    end;
  end;
end;

procedure TdcBorderedControl.WMSize( var Message: TWMSize );
begin
  inherited;
  InvalidateNC;
end;

{ TdcBorderedPanel }

procedure TdcBorderedPanel.Set_BorderColor( const Value: TColor );
begin
  if Value <> fBorderColor then
  begin
    fBorderColor := Value;
    UpdateNCRect;
  end;
end;

procedure TdcBorderedPanel.Set_ShowBorder( const Value: boolean );
begin
  if Value <> fShowBorder then
  begin
    fShowBorder := Value;
    UpdateNCRect;
  end;
end;

{ TdcPanel }

procedure TdcPanel.CMTextChanged( var Message: TMessage );
begin
  inherited;
  Invalidate;
end;

constructor TdcPanel.Create( AOwner: TComponent );
begin
  inherited;
  fAlignment := taCenter;
  fBorderColor := clGray;
  fPanelKind := pkOther;
  fShowBackground := true;
  fShowCaption := true;
end;

function TdcPanel.GetBackgroundColor: TColor;
begin
  case PanelKind of
    pkInfo: Result := clAlertInfoBackground;
    pkWarning: Result := clAlertWarningBackground;
    pkError: Result := clAlertErrorBackground;
  else Result := Color;
  end;
end;

function TdcPanel.GetBorderColor: TColor;
begin
  case fPanelKind of
    pkInfo: Result := clAlertInfoBorder;
    pkWarning: Result := clAlertWarningBorder;
    pkError: Result := clAlertErrorBorder;
  else Result := fBorderColor;
  end;
end;

function TdcPanel.Get_CaptionRect: TRect;
begin
  Result := ClientRect;
  Result.Inflate( Padding, true );
end;

procedure TdcPanel.Paint;
var
  TextFormat: TTextFormat;
begin
  inherited;
  if ShowBackground then
  begin
    with Canvas do
    begin
      case PanelKind of
        pkInfo: Brush.Color := clAlertInfoBackground;
        pkWarning: Brush.Color := clAlertWarningBackground;
        pkError: Brush.Color := clAlertErrorBackground;
      else Brush.Color := Color;
      end;
      FillRect( ClientRect );
    end;
  end;
  if ShowCaption then
  begin
    TextFormat := [tfSingleLine, tfVerticalCenter, tfEndEllipsis];
    case Alignment of
      taLeftJustify: TextFormat := TextFormat + [tfLeft];
      taRightJustify: TextFormat := TextFormat + [tfRight];
      taCenter: TextFormat := TextFormat + [tfCenter];
    end;
    Canvas.Font.Assign( Font );
    Canvas.RenderText( CaptionRect, Caption, TextFormat );
  end;
end;

procedure TdcPanel.Set_Alignment( const Value: TAlignment );
begin
  if Value <> fAlignment then
  begin
    fAlignment := Value;
    Invalidate;
  end;
end;

procedure TdcPanel.Set_PanelKind( const Value: TdcPanelKind );
begin
  if Value <> fPanelKind then
  begin
    fPanelKind := Value;
    Color := GetBackgroundColor;
    InvalidateNC;
  end;
end;

procedure TdcPanel.Set_ShowBackground( const Value: boolean );
begin
  if Value <> fShowBackground then
  begin
    fShowBackground := Value;
    Invalidate;
  end;
end;

procedure TdcPanel.Set_ShowCaption( const Value: boolean );
begin
  if Value <> fShowCaption then
  begin
    fShowCaption := Value;
    Invalidate;
  end;
end;

{ TdcDialogContentPanel }

constructor TdcDialogContentPanel.Create( AOwner: TComponent );
begin
  inherited;
  Color := clDlgContent;
end;

function TdcDialogContentPanel.GetBorderSize: integer;
begin
  Result := 4;
end;

procedure TdcDialogContentPanel.Paint;
begin
  inherited;
  with Canvas do
  begin
    Brush.Color := Color;
    FillRect( ClientRect );
  end;
end;

procedure TdcDialogContentPanel.WMNCPaint( var Message: TWMNCPaint );
var
  DC: HDC;
  BorderPen: HPEN;
  borderRect: TRect;
  i: integer;
begin
  if ShowBorder then
  begin
    DC := GetWindowDC( Handle );
    try
      borderRect := Rect( 0, 0, Width, Height );
      BorderPen := CreatePen( PS_SOLID, 1, ColorToRGB( clWhite ) );
      try
        SelectObject( DC, BorderPen );
        SelectObject( DC, GetStockObject( NULL_BRUSH ) );
        for i := 1 to 3 do
        begin
          Rectangle( DC, borderRect.Left, borderRect.Top, borderRect.Right, borderRect.Bottom );
          InflateRect( borderRect, -1, -1 );
        end;

        { Second Color }
        BorderPen := CreatePen( PS_SOLID, 1, ColorToRGB( clDlgContentBorder ) );
        SelectObject( DC, BorderPen );
        SelectObject( DC, GetStockObject( NULL_BRUSH ) );
        Rectangle( DC, borderRect.Left, borderRect.Top, borderRect.Right, borderRect.Bottom );
      finally
        DeleteObject( BorderPen );
      end;
    finally
      ReleaseDC( Handle, DC );
    end;
  end;
end;

{ TdcCheckBox }

function TdcCheckBox.CanAutoSize( var NewWidth, NewHeight: integer ): boolean;
begin
  Result := true;
  NewWidth := Padding.Height + CheckBoxRect.Width;
  if Caption <> '' then
  begin
    Inc( NewWidth, TextRect.Width + fIndent );
  end;
  NewHeight := Max( CheckBoxRect.Height, TextRect.Height );
end;

procedure TdcCheckBox.CMFontChanged( var Message: TMessage );
begin
  inherited;
  Realign;
end;

procedure TdcCheckBox.CMMouseEnter( var Message: TMessage );
begin
  ButtonState := ButtonState + [btHot];
end;

procedure TdcCheckBox.CMMouseLeave( var Message: TMessage );
begin
  ButtonState := ButtonState - [btHot];
end;

procedure TdcCheckBox.CMTextChanged( var Message: TMessage );
begin
  inherited;
  Invalidate;
end;

constructor TdcCheckBox.Create( AOwner: TComponent );
begin
  inherited;
  ControlStyle := ControlStyle - [csDoubleClicks];
  fAlignment := taLeftJustify;
  fAllowGrayed := false;
  fCheckArea := caCheckMarkAndText;
  fIndent := SpacingDouble;
  fState := cbUnchecked;
  fStyle := ccCheckBox;
  fTagString := '';
  fVerticalAlignment := taVerticalCenter;
  Cursor := crHandPoint;
  Height := 17;
  Width := 97;
end;

procedure TdcCheckBox.DoChange;
begin
  if Assigned( fOnChange ) then
    fOnChange( Self );
end;

procedure TdcCheckBox.DoImagesChange( Sender: TObject );
begin
  Invalidate;
end;

class procedure TdcCheckBox.DrawCheckBox( Canvas: TCanvas; Style: TdcCheckBoxStyle; Dest: TRect; State: TdcButtonState );
var
  MarkRect: TRect;
  ToggleResource: TdcGlyphResource;
begin
  case Style of
    ccCheckBox:
      begin
        MarkRect := Dest;
        MarkRect.Inflate( -2, -2 );
        with Canvas do
        begin
          Brush.Color := clWindow;
          Pen.Color := clCheckBoxBorder;
          if btHot in State then
            Pen.Color := clCheckBoxBorderHot;

          if btFocused in State then
          begin
            Pen.Color := clCheckBoxBorderChecked;
          end;
          Rectangle( Dest );
          if btFocused in State then
          begin
            Dest.Inflate( -1, -1 );
            Rectangle( Dest );
          end;
          if btChecked in State then
            with MarkRect.Location do
            begin
              Pen.Color := clCheckBoxCheckMark;
              Polyline( [Point( X + 1, Y + 4 ), Point( X + 3, Y + 6 ), Point( X + 9, Y + 0 )] );
              Polyline( [Point( X + 1, Y + 5 ), Point( X + 3, Y + 7 ), Point( X + 9, Y + 1 )] );
              Polyline( [Point( X + 1, Y + 6 ), Point( X + 3, Y + 8 ), Point( X + 9, Y + 2 )] );
            end;
        end;
      end;
    ccToggle:
      begin
        if btChecked in State then
          ToggleResource := grToggleSwitchOn
        else
          ToggleResource := grToggleSwitchOff;
        Canvas.Draw( Dest.Left, Dest.Top, FindGraphic( ToggleResource, gtDark ) );
      end;
  end;
end;

function TdcCheckBox.GetAsBoolean: boolean;
begin
  Result := Checked;
end;

function TdcCheckBox.GetAsDate: TDate;
begin
  Result := 0;
end;

function TdcCheckBox.GetAsDateTime: TDateTime;
begin
  Result := 0;
end;

function TdcCheckBox.GetAsFloat: Double;
begin
  Result := IfThen( Checked, 1, 0 );
end;

function TdcCheckBox.GetAsInteger: integer;
begin
  Result := integer( Checked );
end;

function TdcCheckBox.GetAsString: string;
begin
  Result := BoolToStr( Checked, true );
end;

function TdcCheckBox.GetCheckBoxRect: TRect;
var
  Pos: TPoint;
  PaddingRect: TRect;
  CheckBoxSize: TSize;
  GlyphGraphic: TGraphic;
begin
  PaddingRect := ClientRect;
  PaddingRect.Inflate( Padding );

  case Style of
    ccCheckBox: CheckBoxSize := TSize.Create( intCheckBoxSize, intCheckBoxSize );
    ccToggle:
      begin
        if Checked then
          GlyphGraphic := FindGraphic( grToggleSwitchOn, gtDark )
        else
          GlyphGraphic := FindGraphic( grToggleSwitchOff, gtDark );
        CheckBoxSize := TSize.Create( GlyphGraphic.Width, GlyphGraphic.Height );
      end;
  end;

  Pos := PosInRect( CheckBoxSize.cx, CheckBoxSize.cy, PaddingRect, Alignment, VerticalAlignment );

  Result := Bounds( Pos.X, Pos.Y, CheckBoxSize.cx, CheckBoxSize.cy );
end;

function TdcCheckBox.GetChecked: boolean;
begin
  Result := State = cbChecked;
end;

function TdcCheckBox.GetTagString: string;
begin
  Result := fTagString;
end;

function TdcCheckBox.GetTextRect: TRect;
var
  calcRect: TRect;
  captionText: string;
  TextSize: TSize;
begin
  Canvas.Font.Assign( Font );
  if Checked then
    Canvas.Font.Style := [fsBold];

  captionText := Caption;
  Canvas.TextRect( calcRect, captionText, [tfCalcRect, tfSingleLine] );

  TextSize := calcRect.Size;

  Result := Bounds( 0, 0, TextSize.cx, TextSize.cy );

  { Horizontal }
  case Alignment of
    taLeftJustify, TAlignment.taCenter: OffsetRect( Result, CheckBoxRect.Right + fIndent, 0 );
    taRightJustify: OffsetRect( Result, CheckBoxRect.Left - TextSize.cx - fIndent, 0 );
  end;

  { Vertical }
  OffsetRect( Result, 0, Round( ClientHeight / 2 - TextSize.cy / 2 ) );
end;

function TdcCheckBox.InActiveArea( X, Y: integer ): boolean;
begin
  Result := false;
  case fCheckArea of
    caCheckMarkAndText: Result := PtInRect( CheckBoxRect, Point( X, Y ) ) or PtInRect( TextRect, Point( X, Y ) );
    caCheckMark: Result := PtInRect( CheckBoxRect, Point( X, Y ) );
    caClientRect: PtInRect( ClientRect, Point( X, Y ) );
  end;
end;

procedure TdcCheckBox.KeyDown( var Key: Word; Shift: TShiftState );
begin
  inherited;
  if Key = VK_SPACE then
    Toggle;
end;

procedure TdcCheckBox.MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: integer );
begin
  inherited;
  if not Focused then
    SetFocus;

  if ( Button = mbLeft ) and InActiveArea( X, Y ) then
  begin
    ButtonState := ButtonState + [btPushed];
  end;
end;

procedure TdcCheckBox.MouseMove( Shift: TShiftState; X, Y: integer );
begin
  inherited;
  if Style = ccCheckBox then
    if InActiveArea( X, Y ) then
    begin
      ButtonState := ButtonState + [btHot];
    end
    else
      ButtonState := ButtonState - [btHot];
end;

procedure TdcCheckBox.MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: integer );
begin
  inherited;
end;

procedure TdcCheckBox.Paint;
var
  CaptionRect: TRect;
  checkBtnState: TdcButtonState;
begin
  inherited;

  { Caption }
  with Canvas do
  begin
    Font.Assign( Self.Font );

    EraseBkGnd( ClientRect );

    { Rendering CheckBox and CheckMark }
    checkBtnState := ButtonState;
    if Focused then
      Include( checkBtnState, btFocused );
    if Checked then
      Include( checkBtnState, btChecked );

    DrawCheckBox( Canvas, Style, CheckBoxRect, checkBtnState );

    CaptionRect := Self.TextRect;

    Font.Assign( Self.Font );
    if not Enabled then
      Font.Color := clGrayText;

    Font.Style := [];
    if State = cbChecked then
      Font.Style := [fsBold];

    if CaptionRect.Right > ClientWidth then
      CaptionRect.Right := ClientWidth;

    Canvas.RenderText( CaptionRect, Caption, [tfLeft, tfEndEllipsis, tfTop] );
  end;
end;

procedure TdcCheckBox.Set_Alignment( const Value: TAlignment );
begin
  if Value <> fAlignment then
  begin
    fAlignment := Value;
    Invalidate;
  end;
end;

procedure TdcCheckBox.SetAsBoolean( const Value: boolean );
begin
  Checked := Value;
end;

procedure TdcCheckBox.SetAsDate( const Value: TDate );
begin

end;

procedure TdcCheckBox.SetAsDateTime( const Value: TDateTime );
begin

end;

procedure TdcCheckBox.SetAsFloat( const Value: Double );
begin

end;

procedure TdcCheckBox.SetAsInteger( const Value: integer );
begin
  Checked := boolean( Value );
end;

procedure TdcCheckBox.SetAsString( const Value: string );
begin
  Checked := StrToBoolDef( Value, false );
end;

procedure TdcCheckBox.Set_ButtonState( const Value: TdcButtonState );
begin
  if Value <> fButtonState then
  begin
    fButtonState := Value;
    Winapi.Windows.InvalidateRect( Handle, CheckBoxRect, false );
    Winapi.Windows.InvalidateRect( Handle, TextRect, false );
  end;
end;

procedure TdcCheckBox.Set_Checked( const Value: boolean );
begin
  if Value then
    State := cbChecked
  else
    State := cbUnchecked;
end;

procedure TdcCheckBox.Set_Indent( const Value: integer );
begin
  if Value <> fIndent then
  begin
    fIndent := Value;
    Invalidate;
  end;
end;

procedure TdcCheckBox.Set_State( const Value: TCheckBoxState );
var
  invalidationRect: TRect;
begin
  if fState <> Value then
  begin
    invalidationRect := TextRect;
    fState := Value;

    { Trigger event }
    DoChange;

    if AutoSize then
      AdjustSize;

    { Refresh }
    if HandleAllocated then
    begin
      InvalidateRect( CheckBoxRect );
      InvalidateRect( invalidationRect );
    end;
  end;
end;

procedure TdcCheckBox.Set_Style( const Value: TdcCheckBoxStyle );
begin
  if Value <> fStyle then
  begin
    fStyle := Value;
    Invalidate;
  end;
end;

procedure TdcCheckBox.Set_TagString( const Value: string );
begin
  fTagString := Value;
end;

procedure TdcCheckBox.Set_VerticalAlignment( const Value: TVerticalAlignment );
begin
  if Value <> fVerticalAlignment then
  begin
    fVerticalAlignment := Value;
    Invalidate;
  end;
end;

procedure TdcCheckBox.Toggle;
begin
  case State of
    cbUnchecked:
      if AllowGrayed then
        State := cbGrayed
      else
        State := cbChecked;
    cbChecked: State := cbUnchecked;
    cbGrayed: State := cbChecked;
  end;
end;

procedure TdcCheckBox.WMKillFocus( var Msg: TWMKillFocus );
begin
  inherited;
  Invalidate;
end;

procedure TdcCheckBox.WMLButtonUp( var Message: TWMLButtonUp );
begin
  if btPushed in ButtonState then
  begin
    if InActiveArea( message.XPos, message.YPos ) then
      Toggle;

    ButtonState := ButtonState - [btPushed];

    inherited; { Call OnClick }
  end;
end;

procedure TdcCheckBox.WMSetFocus( var Message: TWMSetFocus );
begin
  inherited;
  Invalidate;
end;

{ TdcIconListControl }

procedure TdcIconListControl.CMMouseLeave( var Message: TMessage );
begin
  inherited;
  HotIndex := -1;
end;

constructor TdcIconListControl.Create( AOwner: TComponent );
begin
  inherited;
  fHotIndex := -1;
  fImages := nil;
  fItems := TStringList.Create;
  fItemWidth := 52;
  fPushedIndex := -1;
  fTextHeight := 40;
  ParentColor := false;
  Color := clWindow;
end;

destructor TdcIconListControl.Destroy;
begin
  fItems.Free;
  inherited;
end;

function TdcIconListControl.GetItemAtPos( X, Y: integer ): integer;
begin
  Result := X div ItemWidth;
end;

function TdcIconListControl.GetItemRect( const Index: integer ): TRect;
begin
  Result := Bounds( Padding.Left, Padding.Top, ItemWidth, ClientHeight );
  Result.Offset( index * ItemWidth, 0 );
end;

function TdcIconListControl.ItemExists( const Index: integer ): boolean;
begin
  Result := InRange( index, 0, Pred( Items.Count ) );
end;

procedure TdcIconListControl.MouseDown( Button: TMouseButton; Shift: TShiftState; X, Y: integer );
begin
  inherited;
  if Button = mbLeft then
  begin
    PushedIndex := GetItemAtPos( X, Y );
    if CanFocus then
      SetFocus;
  end;
end;

procedure TdcIconListControl.MouseMove( Shift: TShiftState; X, Y: integer );
begin
  inherited;
  HotIndex := GetItemAtPos( X, Y );
end;

procedure TdcIconListControl.MouseUp( Button: TMouseButton; Shift: TShiftState; X, Y: integer );
begin
  inherited;
  if ItemExists( PushedIndex ) then
    ItemIndex := PushedIndex;
  PushedIndex := -1;
end;

procedure TdcIconListControl.Paint;
var
  buttonRect, iconRect, TextRect: TRect;
  itemText: string;
  ButtonState: TdcButtonState;
begin
  inherited;

  Canvas.Font.Assign( Font );
  Canvas.Brush.Color := Color;
  Padding.Erase( Canvas );
  Canvas.Brush.Color := Color;

  buttonRect := Bounds( Padding.Left, Padding.Top, ItemWidth, ClientHeight - Padding.Height );
  if Assigned( fItems ) and Assigned( fImages ) then
  begin
    for var i := 0 to Pred( fItems.Count ) do
    begin

      ButtonState := [];
      if i = fHotIndex then
      begin
        Include( ButtonState, btHot );
        if i = fPushedIndex then
          Include( ButtonState, btPushed );
      end;
      if i = fItemIndex then
        Include( ButtonState, btSelected );

      if ButtonState = [] then
      begin

        Canvas.Brush.Color := Self.Color;
        Canvas.FillRect( buttonRect );
      end;

      Canvas.PaintFlatButtonBackground( buttonRect, ButtonState );

      iconRect := buttonRect;
      Dec( iconRect.Bottom, fTextHeight );

      PaintIcon( iconRect, i, ButtonState );
      itemText := fItems[i];

      { Text }
      TextRect := buttonRect;
      TextRect.Top := TextRect.Bottom - fTextHeight;
      Canvas.Brush.Style := bsClear;
      Canvas.TextRect( TextRect, itemText, [tfCenter, tfWordBreak, tfVerticalCenter] );
      Canvas.Brush.Style := bsSolid;

      { Shift to the right }
      buttonRect.Offset( iconRect.Width, 0 );
    end;
  end;
  Canvas.Brush.Color := Color;
  Canvas.FillRect( Rect( iconRect.Right, Padding.Top, ClientWidth - Padding.Right, ClientHeight - Padding.Height ) );
end;

procedure TdcIconListControl.PaintIcon( const AIconRect: TRect; const AImageIndex: TImageIndex; const ButtonState: TdcButtonState );
begin
  with Canvas do
  begin
    with centeredRect( AIconRect, Bounds( 0, 0, Images.Width, Images.Height ) ) do
      Images.Draw( Canvas, Left, Top, AImageIndex );
  end;
end;

procedure TdcIconListControl.RefreshItem( const Index: integer );
begin
  if ItemExists( index ) then
    InvalidateRect( Handle, GetItemRect( index ), false );
end;

procedure TdcIconListControl.Set_HotIndex( const Value: integer );
begin
  if Value <> fHotIndex then
  begin
    RefreshItem( fHotIndex );
    fHotIndex := Value;
    if ItemExists( fHotIndex ) then
      Cursor := crHandPoint
    else
      Cursor := crDefault;
    RefreshItem( fHotIndex );
  end;
end;

procedure TdcIconListControl.Set_Images( const Value: TCustomImageList );
begin
  if Value <> fImages then
  begin
    fImages := Value;
    Invalidate;
  end;
end;

procedure TdcIconListControl.Set_ItemIndex( const Value: integer );
begin
  if Value <> fItemIndex then
  begin
    RefreshItem( fItemIndex );
    fItemIndex := Value;
    if Assigned( fOnSelect ) then
      fOnSelect( Self, fItemIndex );
    RefreshItem( fItemIndex );
  end;
end;

procedure TdcIconListControl.Set_Items( const Value: TStrings );
begin
  fItems.Assign( Value );
end;

procedure TdcIconListControl.Set_ItemWidth( const Value: integer );
begin
  if Value <> fItemWidth then
  begin
    fItemWidth := Value;
    Invalidate;
  end;
end;

procedure TdcIconListControl.Set_PushedIndex( const Value: integer );
begin
  if Value <> fPushedIndex then
  begin
    RefreshItem( fPushedIndex );
    fPushedIndex := Value;
    RefreshItem( fPushedIndex );
  end;
end;

procedure TdcIconListControl.Set_TextHeight( const Value: integer );
begin
  if Value <> fTextHeight then
  begin
    fTextHeight := Value;
    Invalidate;
  end;
end;

initialization

CircleCloseGlyph := TPngImage.Create;
CircleCloseGlyph.LoadFromRHResourceName( HInstance, 'CIRCLECLOSE' );

CircleInfoGlyph := TPngImage.Create;
CircleInfoGlyph.LoadFromRHResourceName( HInstance, 'CIRCLEINFO' );

WarningGlyph := TPngImage.Create;
WarningGlyph.LoadFromRHResourceName( HInstance, 'WARNING' );

MandatoryGlyph := TPngImage.Create;
MandatoryGlyph.LoadFromRHResourceName( HInstance, 'MANDATORY' );

finalization

FreeAndNil( CircleCloseGlyph );
FreeAndNil( CircleInfoGlyph );
FreeAndNil( MandatoryGlyph );
FreeAndNil( WarningGlyph );

end.
