unit Emetra.Vcl.Helpers;

interface

uses
  System.Types, System.Classes,
  Winapi.Windows, Winapi.CommCtrl,
  Vcl.Graphics, Vcl.Controls, Vcl.ExtCtrls, Vcl.ImgList, Vcl.Imaging.pngimage,
  System.UITypes,
  {Emetra.Vcl}
  Emetra.Vcl.Consts,
  Emetra.Vcl.Types;

type
  { TCanvasHelper }

  TCanvasHelper = class helper for TCanvas
    procedure DrawBorder( const ARect: TRect; const AWidth: integer );
    procedure Rectangle3D( const ARect: TRect );
    procedure Erase( const AColor: TColor; const ARect: TRect );
    procedure MaskRect( const Value: TRect );
    procedure IsolateRect( const Value: TRect );
    procedure PaintButtonBackground( BtnRect: TRect; BtnState: TdcButtonState );
    procedure PaintFlatButtonBackground( BtnRect: TRect; BtnState: TdcButtonState );
    procedure PaintDropDownButton( BtnRect: TRect; BtnState: TdcButtonState );
    procedure PaintHint( Pt: TPoint; Text: string );
    procedure RenderText( TextRect: TRect; Text: string; TextFormat: TTextFormat );
    function GetTextSize( Text: string ): TSize;
    procedure SetGeometryColor( BtnState: TdcButtonState );
  end;

  { TRectHelper }

  TRectHelper = record helper for TRect
    function CenterSize( Size: TSize ): TPoint;
    function Intersect( ARect: TRect ): boolean;
    procedure Inflate( const DPadding: TPadding; Deflate: boolean = false ); overload;
  end;

  { TPaddingHelper }

  TPaddingHelper = class helper for TPadding
    function Empty: boolean;
    function GetHeight: integer;
    function GetWidth: integer;
    procedure Erase( Canvas: TCanvas );
    property Height: integer read GetHeight;
    property Width: integer read GetWidth;
  end;

  { TCustomImageListHelper }

  TCustomImageListHelper = class helper for TCustomImageList
    function ToSize: TSize;
    function Exists( Index: integer ): boolean;
    function TryDraw( Canvas: TCanvas; X, Y, Index: integer ): boolean;
    procedure DrawBlended( Canvas: TCanvas; X, Y, Index: integer; Blend: Byte );
  end;

  { TPngImageHelper }

  TPngImageHelper = class helper for TPngImage
    procedure LoadFromRHResourceName( Instance: HInst; const Name: string );
  end;

  { TTimerHelper }

  TTimerHelper = class helper for TTimer
    procedure Restart;
  end;

  { TPointHelper }

  TPointHelper = record helper for TPoint
    function IsInvalid: boolean;
  end;

implementation

uses
  System.Math;

{ TCanvasHelper }

procedure TCanvasHelper.Rectangle3D( const ARect: TRect );
var
  InnerRect: TRect;
begin
  InnerRect := ARect;
  Frame3D( Self, InnerRect, clBtnHighlight, clBtnShadow, 1 );
  FillRect( InnerRect );
end;

procedure TCanvasHelper.DrawBorder( const ARect: TRect; const AWidth: integer );
var
  r: TRect;
  i: integer;
begin
  r := ARect;
  for i := 1 to AWidth do
  begin
    FrameRect( r );
    r.Inflate( -1, -1 );
  end;
end;

procedure TCanvasHelper.Erase( const AColor: TColor; const ARect: TRect );
begin
  Brush.Color := AColor;
  FillRect( ARect );
end;

function TCanvasHelper.GetTextSize( Text: string ): TSize;
var
  r: TRect;
begin
  Self.TextRect( r, Text, [tfLeft, tfTop, tfSingleLine, tfCalcRect] );
  Result := TSize.Create( r.Width, r.Height );
end;

procedure TCanvasHelper.IsolateRect( const Value: TRect );
var
  CLPRGN: HRGN;
  P: TPoint;
begin
  with Value do
    CLPRGN := CreateRectRgn( Left, Top, Right, Bottom );
  try
    GetWindowOrgEx( Handle, P );
    OffsetRgn( CLPRGN, -P.X, -P.Y );
    SelectClipRgn( Handle, CLPRGN );
  finally
    DeleteObject( CLPRGN );
  end;
end;

procedure TCanvasHelper.MaskRect( const Value: TRect );
begin
  with Value do
    ExcludeClipRect( Handle, Left, Top, Left + ( Right - Left ), Top + ( Bottom - Top ) );
end;

procedure TCanvasHelper.PaintFlatButtonBackground( BtnRect: TRect; BtnState: TdcButtonState );
begin
  if btDefault in BtnState then
  begin
    Brush.Color := clBtnFaceDefault;
    if btHot in BtnState then
    begin
      Brush.Color := clBtnFaceDefaultHot;
      if btPushed in BtnState then
        Brush.Color := clBtnFaceDefaultPressed;
    end;
  end
  else
  begin
    if btHot in BtnState then
    begin
      Brush.Color := clBtnFaceNormalHot;
      if btPushed in BtnState then
        Brush.Color := clBtnFaceNormalPressed;
    end;
  end;
  if btDisabled in BtnState then
    Brush.Color := clBtnFaceDisabled;
  if Brush.Color <> clNone then
    FillRect( BtnRect );
  { Focus border }
  if btSelected in BtnState then
  begin
    Brush.Color := clSelectedBkDark;
    DrawBorder( BtnRect, 2 );
  end;
end;

procedure TCanvasHelper.PaintButtonBackground( BtnRect: TRect; BtnState: TdcButtonState );
begin

end;

procedure TCanvasHelper.PaintDropDownButton( BtnRect: TRect; BtnState: TdcButtonState );
var
  ButtonFace: TColor;
  ArrowRect: TRect;
begin
  ButtonFace := clEditControl;
  if btHot in BtnState then
  begin
    ButtonFace := clBtnFaceDropDown;
    if btPushed in BtnState then
      ButtonFace := clBtnFaceNormalPressed;
  end;
  Brush.Color := ButtonFace;
  FillRect( BtnRect );

  ArrowRect := CenteredRect( BtnRect, Bounds( 0, 0, 8, 5 ) );

  Pen.Color := clGlyph;
  Brush.Color := Pen.Color;

  with ArrowRect do
    Polygon( [Point( Left, Top ), Point( Right, Top ), Point( Left + Width div 2, Bottom - 1 )] );
end;

procedure TCanvasHelper.PaintHint( Pt: TPoint; Text: string );
var
  BorderRect, CalcRect, r: TRect;
  S: string;
begin
  Pen.Color := clGray;
  Brush.Color := clWhite;
  S := Text;
  TextRect( CalcRect, S, [tfLeft, tfTop, tfSingleLine, tfCalcRect] );
  r := Bounds( Pt.X, Pt.Y, CalcRect.Width, CalcRect.Height );
  BorderRect := r;
  BorderRect.Inflate( SpacingDouble, SpacingDouble );
  Rectangle( BorderRect );
  Font.Style := [];
  TextRect( r, S, [tfLeft, tfTop, tfSingleLine] );
end;

procedure TCanvasHelper.RenderText( TextRect: TRect; Text: string; TextFormat: TTextFormat );
var
  r: TRect;
  S: string;
begin
  S := Text;
  r := TextRect;
  Brush.Style := bsClear;
  Self.TextRect( r, S, TextFormat );
  Brush.Style := bsSolid;
end;

procedure TCanvasHelper.SetGeometryColor( BtnState: TdcButtonState );
begin
  if ( btPushed in BtnState ) and ( btHot in BtnState ) then
    Brush.Color := clWhite
  else
    Brush.Color := clGlyph;
end;

{ TRectHelper }

function TRectHelper.CenterSize( Size: TSize ): TPoint;
begin
  Result := CenterPoint;
  Result.Offset( -Size.cx div 2, -Size.cy div 2 );
end;

procedure TRectHelper.Inflate( const DPadding: TPadding; Deflate: boolean );
begin
  if Deflate then
    Inflate( -DPadding.Left, -DPadding.Top, -DPadding.Right, -DPadding.Bottom )
  else
    Inflate( DPadding.Left, DPadding.Top, DPadding.Right, DPadding.Bottom );
end;

function TRectHelper.Intersect( ARect: TRect ): boolean;
begin
  Result := not( ( Self.Right < ARect.Left ) or ( Self.Left > ARect.Right ) or ( Self.Bottom < ARect.Top ) or ( Self.Top > ARect.Bottom ) );
end;

{ TPaddingHelper }

function TPaddingHelper.Empty: boolean;
begin
  Result := Left + Top + Right + Bottom = 0;
end;

procedure TPaddingHelper.Erase( Canvas: TCanvas );
begin
  with Canvas do
  begin
    FillRect( Rect( 0, 0, Self.ControlWidth, Self.Top ) );
    FillRect( Rect( 0, Self.Top, Self.Left, Self.ControlHeight - Self.Height ) );
    FillRect( Rect( Self.ControlWidth - Self.Width, 0, Self.ControlWidth, Self.ControlHeight ) );
    FillRect( Rect( 0, Self.ControlHeight, Self.ControlWidth, Self.ControlHeight - Self.Height ) );
  end;
end;

function TPaddingHelper.GetHeight: integer;
begin
  Result := Top + Bottom;
end;

function TPaddingHelper.GetWidth: integer;
begin
  Result := Left + Right;
end;

{ TCustomImageListHelper }

procedure TCustomImageListHelper.DrawBlended( Canvas: TCanvas; X, Y, Index: integer; Blend: Byte );
  procedure BlendBitmap( ABitmap: TBitmap );
  var
    X: integer;
    Y: integer;
    Pixel: PRGBQuad;
  begin
    for Y := 0 to ABitmap.Height - 1 do
    begin
      Pixel := ABitmap.ScanLine[Y];
      for X := 0 to ABitmap.Width - 1 do
      begin
        Pixel.rgbReserved := Max( 0, Pixel.rgbReserved - Blend );
        Inc( Pixel );
      end;
    end;
  end;

var
  bitmap: TBitmap;
  P: Pointer;
begin
  bitmap := TBitmap.Create;
  bitmap.SetSize( Width, Height );
  bitmap.PixelFormat := pf32bit;
  bitmap.AlphaFormat := afIgnored;
  bitmap.HandleType := bmDIB;
  bitmap.IgnorePalette := true;
  bitmap.AlphaFormat := afPremultiplied;
  P := bitmap.ScanLine[bitmap.Height - 1];
  FillChar( P^, BytesPerScanLine( bitmap.Width, 32, 32 ) * bitmap.Height, 0 );
  ImageList_DrawEx( Handle, index, bitmap.Canvas.Handle, 0, 0, 0, 0, CLR_NONE, CLR_NONE, ILD_TRANSPARENT );
  BlendBitmap( bitmap );
  Canvas.Draw( X, Y, bitmap );
  bitmap.Free;
end;

function TCustomImageListHelper.Exists( Index: integer ): boolean;
begin
  Result := ( Count > 0 ) and InRange( index, 0, Count - 1 );
end;

function TCustomImageListHelper.ToSize: TSize;
begin
  Result := TSize.Create( Width, Height );
end;

function TCustomImageListHelper.TryDraw( Canvas: TCanvas; X, Y, Index: integer ): boolean;
begin
  Result := ( Count > 0 ) and InRange( index, 0, Count - 1 );
  if Result then
    Draw( Canvas, X, Y, index );
end;

{ TPngImageHelper }

procedure TPngImageHelper.LoadFromRHResourceName( Instance: HInst; const Name: string );
var
  rs: TResourceStream;
begin
  rs := TResourceStream.Create( Instance, PChar( name ), 'PNG' );
  try
    LoadFromStream( rs );
  finally
    rs.Free;
  end;
end;

{ TTimerHelper }

procedure TTimerHelper.Restart;
begin
  if Enabled then
    Enabled := false;
  Enabled := true;
end;

{ TPointHelper }

function TPointHelper.IsInvalid: boolean;
begin
  Result := ( Self.X = -1 ) and ( Self.Y = -1 );
end;

end.
