unit Emetra.Vcl.Graphics;

interface

uses
  System.Classes, System.Types, System.UITypes,
  Windows, SysUtils, Math, Messages,
  {Vcl}
  Vcl.Graphics,
  Vcl.Controls,
  {Emetra.Vcl}
  Emetra.Vcl.Consts,
  Emetra.Vcl.Types,
  Emetra.Vcl.UITypes;

type
  TQuadColor = packed record
    case Boolean of
      True: ( Blue, Green, Red, Alpha: Byte );
      False: ( Quad: Cardinal );
  end;

  PQuadColor = ^TQuadColor;
  PPQuadColor = ^PQuadColor;

  TNxCanvasSnapshot = record
    Brush: record
      Color: TColor;
    end;

    Font: record
      Color: TColor;
      Name: string;
      Size: Integer;
      Style: TFontStyles;
    end;
  end;

  { Measuring }
function GetFontHeight( AFont: TFont ): Integer;
function GetEditHeight( AFont: TFont ): Integer;

{ Snapshots }
function GetCanvasSnapshot( Canvas: TCanvas ): TNxCanvasSnapshot;
procedure PutCanvasSnapshot( Canvas: TCanvas; Snapshoot: TNxCanvasSnapshot );

{ Modern Gui }
procedure DrawModernButton( Canvas: TCanvas; ButtonRect: TRect; State: TdcButtonState ); overload;
procedure DrawModernButton( Canvas: TCanvas; Text: WideString; ButtonRect: TRect; State: TdcButtonState ); overload;
procedure DrawModernButton( Canvas: TCanvas; Text, ResName: WideString; ButtonRect: TRect; State: TdcButtonState ); overload;
procedure DrawModernToolButton( Canvas: TCanvas; ButtonRect: TRect; State: TdcButtonState ); overload;

{ Blending }
procedure AlphaBlend( Canvas: TCanvas; Bitmap: TBitmap; const X, Y: Integer );

{ Drawing Glyphs }
procedure DrawBitmap( Canvas: TCanvas; X, Y: Integer; Bitmap: TBitmap );
procedure DrawBitmapFromRes( Canvas: TCanvas; Location: TPoint; Resource: string; Transparent: Boolean = False ); overload;
procedure DrawBitmapFromRes( Canvas: TCanvas; Rect: TRect; Alignment: TAlignment; VerticalAlignment: TVerticalAlignment; Resource: string; Transparent: Boolean = False ); overload;
procedure DrawGraphic( Canvas: TCanvas; X, Y: Integer; Graphic: TGraphic );
function HasGraphic( const APicture: TPicture ): Boolean;

{ Drawing }
procedure DrawFocusRect( Canvas: TCanvas; Rect: TRect );
procedure DrawGrayscaleBitmap( Canvas: TCanvas; X, Y: Integer; Bitmap: TBitmap );
procedure Draw32BitBitmap( Canvas: TCanvas; X, Y: Integer; Bitmap: TBitmap );
procedure DrawDotLine( Canvas: TCanvas; const X1, Y1, X2, Y2: Integer; Color: TColor );
procedure DrawGrid( Canvas: TCanvas; GridRect: TRect; Step: Integer );
procedure DrawImage( Canvas: TCanvas; Dest: TRect; Images: TImageList; Alignment: TAlignment; VerticalAlignment: TVerticalAlignment; Index: TImageIndex );
procedure DrawSizeGrip( Canvas: TCanvas; GripRect: TRect; Color: TColor );
procedure DrawPlus( Canvas: TCanvas; PlusRect: TRect; BrushColor: TColor );

{ PaintEffects }
procedure ColorOverlay( Canvas: TCanvas; Source: TRect; Color: TColor; const Alpha: Byte );

{ Bevel }
procedure DrawBevel( Canvas: TCanvas; Dest: TRect; GrayLevel: Byte; Intensity: Double; Orientation: TNxOrientation; Inverted: Boolean );
procedure DrawBevels( Canvas: TCanvas; Dest: TRect; BevelSize: Integer );

{ Inner Shadow }
procedure DrawInnerShadows( Canvas: TCanvas; ARect: TRect; ShadowSize: Integer );
procedure DrawInnerShadow( Canvas: TCanvas; Dest: TRect; GrayLevel: Byte; Intensity: Double; Orientation: TNxOrientation ); overload;

{ Light }
procedure DrawLight( Canvas: TCanvas; InnerRct: TRect; Size: Integer ); overload;
procedure DrawReflection( Canvas: TCanvas; InnerRct: TRect; Size: Integer );

{ Shadow }
procedure DrawShadow( Canvas: TCanvas; Inner: TRect; Orientation: TNxOrientation ); overload;
procedure DrawShadows( Canvas: TCanvas; InnerRct: TRect; Size: Integer ); overload;

{ Drawing Elements }
procedure DrawDropDownMark( Canvas: TCanvas; const X, Y: Integer; Color: TColor );
procedure DrawIndicatorArrow( Canvas: TCanvas; Rect: TRect; Color: TColor );
procedure DrawSortArrow( Canvas: TCanvas; const X, Y: Integer; SortKind: TNxSortKind );

{ Fading }
procedure FadeHorz( Canvas: TCanvas; SourceRect: TRect; InvertDirection: Boolean = False );
procedure FadeVert( Canvas: TCanvas; SourceRect: TRect );

{ Manipulation }
procedure FlipVert( Source: TBitmap );

{ Measuring }
procedure ConstrainedRect( Graphic: TGraphic; var Src: TRect );

{ Cursor }

function GetSpinEditCursor( const AControlRect: TRect; const X, Y: Integer ): TCursor;
function GetDropDownControlCursor( ACtrlRect: TRect; APt: TPoint ): TCursor;

{ Utilities }

function ColorToWebColor( Color: TColor ): string;

function BlendColor( Color1, Color2: TColor; Amount: Byte ): TColor;
procedure EraseBackground( Control: TCustomControl; Canvas: TCanvas );
function WebColorToColor( WebColor: string ): TColor;
procedure GetRGB( C: TColor; out R, G, B: Byte );
function GetSystemColor( Color: TColor ): TColor;
function HTMLColorToString( Color: TColor ): string;
function DarkerColor( Color: TColor; Percent: Byte ): TColor;
function LighterColor( Color: TColor; Percent: Byte ): TColor;
function ColorBrightness( const Color: TColor ): Byte;
function RandomColor: TColor;
function RectInRect( Container, Target: TRect ): Boolean;
function RectIntersect( Rect1, Rect2: TRect ): Boolean;
procedure SetFontQuality( Font: TFont; Quality: Byte );
function PosInRect( Width, Height: Integer; Container: TRect; Alignment: TAlignment; VerticalAlignment: TVerticalAlignment ): TPoint;
function IsColorLight( Color: TColor ): Boolean;

function StrToColor( S: string ): TColor;
function WebColorValue( Color: string ): string;

{ Clipping }
procedure SetClipRect( Canvas: TCanvas; ClipRect: TRect );
procedure ExcludeClipRect( Canvas: TCanvas; ClipRect: TRect );

{ Misc. }

procedure FillPadding( Canvas: TCanvas; Color: TColor; Dest: TRect; Padding: TPadding );

{ TRect }

function AnchorRect( Container: TRect; Size: TSize; Anchors: TAnchors; Offset: TPoint ): TRect;

{ TGraphic }

function FitToRect( Source, Destination: TRect ): TRect;
function PaintGraphic( Canvas: TCanvas; Graphic: TGraphic; Dest: TRect; FillMode: TNxFillMode ): TRect;

{ Dpi Scalling }
function DotToPixel( Value: TRect ): TRect; overload;
function DotToPixel( Value: TSize ): TSize; overload;
function DotToPixel( Value: Integer ): Integer; overload;

function DeviceIndipendedPoint( Value: Integer ): Integer; overload;
function DeviceIndipendedPoint( Value: TPadding ): TPadding; overload;

{ Color Utils }

function GetGeometryColor( State: TdcButtonState ): TColor;

function GetResourceGraphic( const ResourceId: string ): TGraphic;

implementation

uses
  {Standard}
  System.StrUtils,
  Vcl.Dialogs, Vcl.Forms, Vcl.Imaging.pngimage,
  Emetra.Vcl.Helpers;

const
  ThinDotPenStyle: array [1 .. 2] of DWORD = ( 0, 3 );

function GetFontHeight( AFont: TFont ): Integer;
var
  DC: HDC;
  SaveFont: HFont;
  SysMetrics, Metrics: TTextMetric;
begin
  DC := GetDC( 0 );
  try
    GetTextMetrics( DC, SysMetrics );
    SaveFont := SelectObject( DC, AFont.Handle );
    GetTextMetrics( DC, Metrics );
    SelectObject( DC, SaveFont );
  finally
    ReleaseDC( 0, DC );
  end;
  Result := Metrics.tmHeight;
end;

function GetEditHeight( AFont: TFont ): Integer;
begin
  Result := GetFontHeight( AFont ) + 4 + 4;
end;

function StrToColor( S: string ): TColor;
var
  C: string;
  LColor: LongInt;
begin
  { Default }
  Result := clNone;

  if S <> EmptyStr then
  begin
    if LeftStr( S, 1 ) = '#' then
      Result := WebColorToColor( S )
    else if LeftStr( S, 1 ) = '$' then
      Result := StringToColor( S )
    else
    begin
      { Web Color }
      C := WebColorValue( S );
      if C <> '' then
        Result := WebColorToColor( C );

      { Standard Color }
      if Result = clNone then
        if IdentToColor( S, LColor ) then
          Result := TColor( LColor )
        else
          Result := StrToIntDef( S, clNone );
    end
  end;
end;

function WebColorValue( Color: string ): string;
var
  i: Integer;
  ColorName: TWebColor;
begin
  Result := EmptyStr;
  for i := 0 to 140 do
  begin
    ColorName := WebColors[i];
    if LowerCase( ColorName[cnColorName] ) = LowerCase( Color ) then
    begin
      Result := ColorName[cnColorValue];
      Exit;
    end;
  end;
end;

function AnchorRect( Container: TRect; Size: TSize; Anchors: TAnchors; Offset: TPoint ): TRect;
var
  StartPoint, EndPoint: TPoint;
begin
  StartPoint := Point( 0, 0 );
  EndPoint := Point( 0, 0 );
  if akLeft in Anchors then
  begin
    StartPoint.X := Container.Left + Offset.X;
    EndPoint.X := Min( StartPoint.X + Size.cx, Container.Right );
  end;
  if akTop in Anchors then
  begin
    StartPoint.Y := Container.Top + Offset.Y;
    EndPoint.Y := Min( StartPoint.Y + Size.cy, Container.Bottom );
  end;
  if akRight in Anchors then
  begin
    EndPoint.X := Container.Right - Offset.X;
    if not( akLeft in Anchors ) then
      StartPoint.X := Max( EndPoint.X - Size.cx, Container.Left );
  end;
  if akBottom in Anchors then
  begin
    EndPoint.Y := Container.Bottom - Offset.Y;
    if not( akTop in Anchors ) then
      StartPoint.Y := Max( EndPoint.Y - Size.cy, Container.Top );
  end;
  Result := Rect( StartPoint.X, StartPoint.Y, EndPoint.X, EndPoint.Y );
end;

function GetCanvasSnapshot( Canvas: TCanvas ): TNxCanvasSnapshot;
begin
  Result.Brush.Color := Canvas.Brush.Color;
  Result.Font.Color := Canvas.Font.Color;
  Result.Font.Name := Canvas.Font.Name;
  Result.Font.Size := Canvas.Font.Size;
  Result.Font.Style := Canvas.Font.Style;
end;

procedure PutCanvasSnapshot( Canvas: TCanvas; Snapshoot: TNxCanvasSnapshot );
begin
  Canvas.Brush.Color := Snapshoot.Brush.Color;
  Canvas.Font.Color := Snapshoot.Font.Color;
  Canvas.Font.Size := Snapshoot.Font.Size;
  Canvas.Font.Style := Snapshoot.Font.Style;
  Canvas.Font.Name := Snapshoot.Font.Name;
end;

procedure DrawModernButton( Canvas: TCanvas; ButtonRect: TRect; State: TdcButtonState );
var
  FrameColor, FaceColor: TColor;
begin
  with Canvas do
  begin
    { Default style }
    FrameColor := clWindowFrame;
    FaceColor := clWindow;

    if btHot in State then
    begin
      FrameColor := clBtnShadow;
      FaceColor := clBtnFace;

      if btPushed in State then
      begin
        FrameColor := clWindowFrame;
        FaceColor := clBtnShadow;
      end;

    end;

    if ( btSelected in State ) or ( btChecked in State ) then
    begin
      FrameColor := clBtnShadow;
      FaceColor := clGrayText;
    end;

    Canvas.Pen.Color := FrameColor;
    Canvas.Brush.Color := FaceColor;
    Canvas.Rectangle( ButtonRect );

  end;
end;

procedure DrawModernButton( Canvas: TCanvas; Text: WideString; ButtonRect: TRect; State: TdcButtonState );
var
  btnRect: TRect;
  btnText: string;
begin
  DrawModernButton( Canvas, ButtonRect, State );
  btnRect := ButtonRect;
  btnText := btnText;
  Canvas.TextRect( btnRect, btnText, [tfCenter, tfVerticalCenter, tfEndEllipsis] );
end;

procedure DrawModernButton( Canvas: TCanvas; Text, ResName: WideString; ButtonRect: TRect; State: TdcButtonState );
var
  TextRect, IconRect: TRect;
  btnText: string;
begin
  DrawModernButton( Canvas, ButtonRect, State );

  IconRect := ButtonRect;
  Inc( IconRect.Left, 1 );

  DrawBitmapFromRes( Canvas, IconRect, taLeftJustify, taVerticalCenter, ResName, True );

  TextRect := ButtonRect;
  Inc( TextRect.Left, 1 + 16 + 2 );

  btnText := Text;
  Canvas.TextRect( ButtonRect, btnText, [tfLeft, tfVerticalCenter, tfEndEllipsis] );
end;

procedure DrawModernToolButton( Canvas: TCanvas; ButtonRect: TRect; State: TdcButtonState );
var
  FaceColor: TColor;
begin
  with Canvas do
  begin
    FaceColor := clNone;

    if ( btChecked in State ) or ( btPushed in State ) then
    begin
      FaceColor := clBtnShadow;
    end;

    if btHot in State then
    begin
      FaceColor := clBtnFace;
      if btPushed in State then
        FaceColor := clBtnShadow;
    end;

    if FaceColor <> clNone then
    begin
      Brush.Color := FaceColor;
      FillRect( ButtonRect );
    end;
  end;
end;

procedure AlphaBlend( Canvas: TCanvas; Bitmap: TBitmap; const X, Y: Integer );
var
  Func: TBlendFunction;
begin
  { Blending Params }
  Func.BlendOp := AC_SRC_OVER;
  Func.BlendFlags := 0;
  Func.SourceConstantAlpha := 255;
  Func.AlphaFormat := AC_SRC_ALPHA;

  { Do Blending }
  Windows.AlphaBlend( Canvas.Handle, X, Y, Bitmap.Width, Bitmap.Height, Bitmap.Canvas.Handle, 0, 0, Bitmap.Width, Bitmap.Height, Func );
end;

procedure ColorOverlay( Canvas: TCanvas; Source: TRect; Color: TColor; const Alpha: Byte );
var
  Row, Col: Integer;
  Pixel: PQuadColor;
  Mask: TBitmap;
begin
  if Alpha = 255 then
  begin
    Canvas.Brush.Color := Color;
    Canvas.FillRect( Source );

    { Skip complex computings }
    Exit;
  end;

  { Convert SystemColor }
  if Integer( Color ) < 0 then
    Color := GetSysColor( Color and $000000FF );

  { 10/26/11: We create an Alpha Mask TBitmap here which
    will be aplied into Canvas }
  Mask := TBitmap.Create;

  with Mask do
  begin
    { Important for AlphaBlend Func }
    PixelFormat := pf32bit;
    { Set Size of Mask }
    Width := Source.Right - Source.Left;
    Height := Source.Bottom - Source.Top;
  end;

  for Row := 0 to Pred( Mask.Height ) do
  begin
    { Read first Pixel in row }
    Pixel := Mask.Scanline[Row];

    for Col := 0 to Pred( Mask.Width ) do
    begin
      Pixel.Red := GetRValue( Color );
      Pixel.Green := GetGValue( Color );
      Pixel.Blue := GetBValue( Color );

      { Set Alpha }
      Pixel.Alpha := Alpha;

      { Premultiply R, G & B }
      Pixel.Red := MulDiv( Pixel.Red, Pixel.Alpha, $FF );
      Pixel.Green := MulDiv( Pixel.Green, Pixel.Alpha, $FF );
      Pixel.Blue := MulDiv( Pixel.Blue, Pixel.Alpha, $FF );

      { Move to next Pixed }
      Inc( Pixel );

    end; { for Col }

  end; { for Row }

  { Call Blending Func }
  AlphaBlend( Canvas, Mask, Source.Left, Source.Top );

  { Release }
  Mask.Free;
end;

procedure DrawBitmap( Canvas: TCanvas; X, Y: Integer; Bitmap: TBitmap );
begin
  case Bitmap.PixelFormat of
    pfDevice, pf1bit, pf4bit, pf8bit, pf15bit, pf16bit, pf24bit: Canvas.Draw( X, Y, Bitmap );
    pf32bit: Draw32BitBitmap( Canvas, X, Y, Bitmap );
    pfCustom:;
  end;
end;

procedure DrawBitmapFromRes( Canvas: TCanvas; Location: TPoint; Resource: string; Transparent: Boolean = False );
var
  Bitmap: TBitmap;
begin
  Bitmap := TBitmap.Create;
  try
    { Load }
    Bitmap.LoadFromResourceName( HInstance, Resource );

    Canvas.Draw( Location.X, Location.Y, Bitmap );
  finally
    Bitmap.Free;
  end;
end;

procedure DrawBitmapFromRes( Canvas: TCanvas; Rect: TRect; Alignment: TAlignment; VerticalAlignment: TVerticalAlignment; Resource: string; Transparent: Boolean = False );
var
  Bitmap: TBitmap;
  Location: TPoint;
begin
  Bitmap := TBitmap.Create;
  try

    { Load }
    Bitmap.LoadFromResourceName( HInstance, Resource );

    Location := PosInRect( Bitmap.Width, Bitmap.Height, Rect, Alignment, VerticalAlignment );

    if Transparent then
    begin
      Bitmap.Transparent := True;
      Bitmap.TransparentColor := Bitmap.Canvas.Pixels[0, Pred( Bitmap.Height )];
    end;

    Canvas.Draw( Location.X, Location.Y, Bitmap );
  finally
    Bitmap.Free;
  end;
end;

procedure DrawGraphic( Canvas: TCanvas; X, Y: Integer; Graphic: TGraphic );
begin
  if Graphic is TBitmap then
    DrawBitmap( Canvas, X, Y, Graphic as TBitmap )
  else
    Canvas.Draw( X, Y, Graphic );
end;

function HasGraphic( const APicture: TPicture ): Boolean;
begin
  Result := Assigned( APicture ) and Assigned( APicture.Graphic ) and not APicture.Graphic.Empty;
end;

procedure DrawFocusRect( Canvas: TCanvas; Rect: TRect );
var
  PrevBkColor, PrevTextColor: TColorRef;
  DC: HDC;
begin
  DC := Canvas.Handle;
  PrevBkColor := SetBkColor( DC, clBlack );
  PrevTextColor := SetTextColor( DC, clWhite );
  Windows.DrawFocusRect( DC, Rect );
  SetBkColor( DC, PrevBkColor );
  SetTextColor( DC, PrevTextColor );
end;

procedure DrawGrayscaleBitmap( Canvas: TCanvas; X, Y: Integer; Bitmap: TBitmap );
var
  Row, Col, Grey: Integer;
  Pixel: PQuadColor;
  Mask: TBitmap;

begin
  { 10/26/11: We create an Alpha Mask TBitmap here which
    will be aplied into Canvas }
  Mask := TBitmap.Create;

  with Mask do
  begin
    { Important for AlphaBlend Func }
    PixelFormat := pf32bit;

    { Set Size of Mask }
    Width := Bitmap.Width;
    Height := Bitmap.Height;

    Canvas.CopyRect( Bounds( 0, 0, Bitmap.Width, Bitmap.Height ), Bitmap.Canvas, Bounds( 0, 0, Bitmap.Width, Bitmap.Height ) );
  end;

  for Row := 0 to Pred( Mask.Height ) do
  begin
    { Read first Pixel in row }
    Pixel := Mask.Scanline[Row];

    for Col := 0 to Pred( Mask.Width ) do
    begin
      Grey := Round( 0.30 * Pixel.Red + 0.59 * Pixel.Green + 0.11 * Pixel.Blue );

      Pixel.Red := Grey;
      Pixel.Green := Grey;
      Pixel.Blue := Grey;

      Pixel.Alpha := 255;

      { Premultiply R, G & B }
      Pixel.Red := MulDiv( Pixel.Red, Pixel.Alpha, $FF );
      Pixel.Green := MulDiv( Pixel.Green, Pixel.Alpha, $FF );
      Pixel.Blue := MulDiv( Pixel.Blue, Pixel.Alpha, $FF );

      { Move to next Pixel }
      Inc( Pixel );

    end; { for Col }

  end; { for Row }

  { Call Blending Func }
  AlphaBlend( Canvas, Mask, X, Y );

  { Release }
  Mask.Free;
end;

procedure Draw32BitBitmap( Canvas: TCanvas; X, Y: Integer; Bitmap: TBitmap );
var
  Row, Col: Integer;
  Pixel: PQuadColor;
  Mask: TBitmap;

begin
  { 10/26/11: We create an Alpha Mask TBitmap here which
    will be aplied into Canvas }
  Mask := TBitmap.Create;

  with Mask do
  begin
    { Important for AlphaBlend Func }
    PixelFormat := pf32bit;

    { Set Size of Mask }
    Width := Bitmap.Width;
    Height := Bitmap.Height;

    Canvas.CopyRect( Bounds( 0, 0, Bitmap.Width, Bitmap.Height ), Bitmap.Canvas, Bounds( 0, 0, Bitmap.Width, Bitmap.Height ) );
  end;

  for Row := 0 to Pred( Mask.Height ) do
  begin
    { Read first Pixel in row }
    Pixel := Mask.Scanline[Row];

    for Col := 0 to Pred( Mask.Width ) do
    begin

      { Premultiply R, G & B }
      Pixel.Red := MulDiv( Pixel.Red, Pixel.Alpha, $FF );
      Pixel.Green := MulDiv( Pixel.Green, Pixel.Alpha, $FF );
      Pixel.Blue := MulDiv( Pixel.Blue, Pixel.Alpha, $FF );

      { Move to next Pixel }
      Inc( Pixel );

    end; { for Col }

  end; { for Row }

  { Call Blending Func }
  AlphaBlend( Canvas, Mask, X, Y );

  { Release }
  Mask.Free;
end;

procedure DrawDotLine( Canvas: TCanvas; const X1, Y1, X2, Y2: Integer; Color: TColor );
var
  LogBrush: TLogBrush;
begin
  with Canvas do
  begin
    Pen.Color := GetSystemColor( Color );
    LogBrush.lbStyle := BS_SOLID;
    LogBrush.lbColor := Pen.Color;
    Pen.Style := psUserStyle;
    Pen.Handle := ExtCreatePen( PS_GEOMETRIC or PS_USERSTYLE, Pen.Width, LogBrush, Length( ThinDotPenStyle ), @ThinDotPenStyle );
    MoveTo( X1, Y1 );
    LineTo( X2, Y2 );
    Pen.Style := psSolid;
  end;
end;

procedure DrawGrid( Canvas: TCanvas; GridRect: TRect; Step: Integer );
var
  i, j: Integer;
begin
  j := GridRect.Top;
  i := GridRect.Left;
  while j < GridRect.Bottom do
  begin
    while i < GridRect.Right do
    begin
      Canvas.Pixels[i, j] := clGrayText;
      Inc( i, Step );
    end;
    Inc( j, Step );
    i := GridRect.Left;
  end;
end;

procedure DrawImage( Canvas: TCanvas; Dest: TRect; Images: TImageList; Alignment: TAlignment; VerticalAlignment: TVerticalAlignment; Index: TImageIndex );
var
  Location: TPoint;
begin
  Location := PosInRect( Images.Width, Images.Height, Dest, Alignment, VerticalAlignment );
  Images.Draw( Canvas, Location.X, Location.Y, index );
end;

procedure DrawBevels( Canvas: TCanvas; Dest: TRect; BevelSize: Integer ); overload;
begin
  { Dark }
  DrawBevel( Canvas, Rect( Dest.Left, Dest.Bottom - BevelSize, Dest.Right, Dest.Bottom ), 0, 0.5, orHorizontal, False );
  DrawBevel( Canvas, Rect( Dest.Right - BevelSize, Dest.Top, Dest.Right, Dest.Bottom ), 0, 0.5, orVertical, False );

  { Light }
  DrawBevel( Canvas, Rect( Dest.Left, Dest.Top, Dest.Right, Dest.Top + BevelSize ), 255, 0.5, orHorizontal, True );
  DrawBevel( Canvas, Rect( Dest.Left, Dest.Top, Dest.Left + BevelSize, Dest.Bottom ), 255, 0.5, orVertical, True );
end;

procedure DrawBevel( Canvas: TCanvas; Dest: TRect; GrayLevel: Byte; Intensity: Double; Orientation: TNxOrientation; Inverted: Boolean ); overload;
var
  Row, Col: Integer;
  Alpha: Single;
  Pixel: PQuadColor;
  AlphaByte: Byte;
  Mask: TBitmap;
begin
  { 10/26/11: We create an Alpha Mask TBitmap here which
    will be aplied into Canvas }
  Mask := TBitmap.Create;

  with Mask do
  begin
    { Important for AlphaBlend Func }
    PixelFormat := pf32bit;
    { Set Size of Mask }
    Width := Dest.Right - Dest.Left;
    Height := Dest.Bottom - Dest.Top;
  end;

  for Row := 0 to Pred( Mask.Height ) do
  begin
    { Read first Pixel in row }
    Pixel := Mask.Scanline[Row];

    for Col := 0 to Pred( Mask.Width ) do
    begin
      { Calculate Alpha }
      case Orientation of
        orHorizontal:
          if Inverted then
          begin
            Alpha := 1 - ( ( Succ( Row ) / Mask.Height ) * ( 1 - Intensity ) );
          end
          else
          begin
            Alpha := ( Succ( Row ) / Mask.Height ) * Intensity;
          end;
      else
        if Inverted then
        begin
          Alpha := 1 - ( ( Succ( Col ) / Mask.Width ) * ( 1 - Intensity ) );
        end
        else
        begin
          Alpha := ( Succ( Col ) / Mask.Width ) * Intensity;
        end;
      end;

      { Convert 0-1 to 0-255 }
      AlphaByte := Round( Alpha * 255.0 );

      Pixel.Red := GrayLevel;
      Pixel.Green := GrayLevel;
      Pixel.Blue := GrayLevel;

      { Set Alpha }
      Pixel.Alpha := AlphaByte;

      { Premultiply R, G & B }
      Pixel.Red := Round( Pixel.Red * Alpha );
      Pixel.Green := Round( Pixel.Green * Alpha );
      Pixel.Blue := Round( Pixel.Blue * Alpha );

      { Move to next Pixed }
      Inc( Pixel );
    end; { for Col }

  end; { for Row }

  { Call Blending Func }
  AlphaBlend( Canvas, Mask, Dest.Left, Dest.Top );

  { Release }
  Mask.Free;
end;

procedure DrawInnerShadow( Canvas: TCanvas; Dest: TRect; GrayLevel: Byte; Intensity: Double; Orientation: TNxOrientation ); overload;
var
  Row, Col: Integer;
  Alpha: Single;
  Pixel: PQuadColor;
  PixelByte: Byte;
  Mask: TBitmap;
begin
  Mask := TBitmap.Create;

  with Mask do
  begin
    { Required by AlphaBlend WinApi Func }
    PixelFormat := pf32bit;

    { Set Size of Mask }
    Width := Dest.Right - Dest.Left;
    Height := Dest.Bottom - Dest.Top;
  end;

  for Row := 0 to Pred( Mask.Height ) do
  begin
    { Read first Pixel in row }
    Pixel := Mask.Scanline[Row];

    for Col := 0 to Pred( Mask.Width ) do
    begin

      { Calculate Alpha }
      case Orientation of
        orHorizontal: Alpha := 1 - ( ( Succ( Row ) / Mask.Height ) );
      else Alpha := 1 - ( ( Succ( Col ) / Mask.Width ) );
      end;

      Alpha := Alpha * Intensity;

      { Pixel Color is level of gray }
      PixelByte := Round( GrayLevel * Alpha );
      Pixel.Red := PixelByte;
      Pixel.Green := PixelByte;
      Pixel.Blue := PixelByte;

      { Convert 0-1 to 0-255 & set Alpha }
      Pixel.Alpha := Round( Alpha * 255.0 );

      { Move to next Pixed }
      Inc( Pixel );

    end; { for Col }

  end; { for Row }

  { Call Blending Func }
  AlphaBlend( Canvas, Mask, Dest.Left, Dest.Top );

  { Release }
  Mask.Free;
end;

procedure DrawInnerShadows( Canvas: TCanvas; ARect: TRect; ShadowSize: Integer );
begin
  DrawInnerShadow( Canvas, Rect( ARect.Left, ARect.Top, ARect.Right, ARect.Top + ShadowSize ), 0, 0.5, orHorizontal );
  DrawInnerShadow( Canvas, Rect( ARect.Left, ARect.Top, ARect.Left + ShadowSize, ARect.Bottom ), 0, 0.5, orVertical );
end;

procedure DrawLight( Canvas: TCanvas; InnerRct: TRect; Size: Integer ); overload;
var
  Row, Col, Middle: Integer;
  Alpha: Single;
  Pixel: PQuadColor;
  AlphaByte: Byte;
  Mask: TBitmap;
begin
  { 10/26/11: We create an Alpha Mask TBitmap here which
    will be aplied into Canvas }
  Mask := TBitmap.Create;

  Middle := ( ( InnerRct.Right - InnerRct.Left ) div 3 ) * 2;

  if Middle = 0 then
    Exit;

  with Mask do
  begin
    { Important for AlphaBlend Func }
    PixelFormat := pf32bit;

    { Set Size of Mask }
    Width := InnerRct.Right - InnerRct.Left;
    Height := InnerRct.Bottom - InnerRct.Top;
  end;

  for Row := 0 to Pred( Mask.Height ) do
  begin
    { Read first Pixel in row }
    Pixel := Mask.Scanline[Row];

    for Col := 0 to Pred( Mask.Width ) do
    begin
      Pixel.Red := 255;
      Pixel.Green := 255;
      Pixel.Blue := 255;

      { Determine Alpha Level }
      Alpha := ( Middle - Row ) / Middle;

      { Convert 0-1 to 0-255 }
      AlphaByte := Round( Alpha * 255.0 );

      if Col + Row > Middle then
        AlphaByte := 0;

      { Set Alpha }
      Pixel.Alpha := AlphaByte;

      { Premultiply R, G & B }
      Pixel.Red := Round( Pixel.Red * Alpha );
      Pixel.Green := Round( Pixel.Green * Alpha );
      Pixel.Blue := Round( Pixel.Blue * Alpha );

      { Move to next Pixed }
      Inc( Pixel );

    end; { for Col }

  end; { for Row }

  { Call Blending Func }
  AlphaBlend( Canvas, Mask, InnerRct.Left, InnerRct.Top );

  { Release }
  Mask.Free;
end;

procedure DrawReflection( Canvas: TCanvas; InnerRct: TRect; Size: Integer );
var
  Row, Col: Integer;
  Alpha: Single;
  Pixel: PQuadColor;
  AlphaByte: Byte;
  Mask: TBitmap;
begin
  { 10/26/11: We create an Alpha Mask TBitmap here which
    will be aplied into Canvas }
  Mask := TBitmap.Create;

  with Mask do
  begin
    { Important for AlphaBlend Func }
    PixelFormat := pf32bit;
    { Set Size of Mask }
    Width := InnerRct.Right - InnerRct.Left;
    Height := InnerRct.Bottom - InnerRct.Top;
  end;

  { Copy TRect from Canvas into Bitmap }
  Mask.Canvas.CopyRect( Rect( 0, 0, Mask.Width, Mask.Height ), Canvas, InnerRct );

  { Flip Bitmap }
  FlipVert( Mask );

  for Row := 0 to Pred( Mask.Height ) do
  begin
    { Read first Pixel in row }
    Pixel := Mask.Scanline[Row];

    for Col := 0 to Pred( Mask.Width ) do
    begin
      if Row < Size then
        Alpha := ( Size - Row ) / Size
      else
        Alpha := 0;

      { Convert 0-1 to 0-255 }
      AlphaByte := Round( Alpha * 255.0 );

      { Set Alpha }
      Pixel.Alpha := AlphaByte;

      { Premultiply R, G & B }
      Pixel.Red := Round( Pixel.Red * Alpha );
      Pixel.Green := Round( Pixel.Green * Alpha );
      Pixel.Blue := Round( Pixel.Blue * Alpha );

      { Move to next Pixed }
      Inc( Pixel );

    end; { for Col }

  end; { for Row }

  { Call Blending Func }
  AlphaBlend( Canvas, Mask, InnerRct.Left, InnerRct.Bottom + 1 );

  { Release }
  Mask.Free;
end;

procedure DrawShadows( Canvas: TCanvas; InnerRct: TRect; Size: Integer );
begin
  { Draw Horz Shadow }
  DrawShadow( Canvas, Rect( InnerRct.Left, InnerRct.Bottom, InnerRct.Right + Size, InnerRct.Bottom + Size ), orHorizontal );

  { Draw Vert Shadow }
  DrawShadow( Canvas, Rect( InnerRct.Right, InnerRct.Top, InnerRct.Right + Size, InnerRct.Bottom + Size - 1 ), orVertical );
end;

procedure DrawShadow( Canvas: TCanvas; Inner: TRect; Orientation: TNxOrientation );
var
  Row, Col: Integer;
  Alpha: Single;
  Pixel: PQuadColor;
  AlphaByte: Byte;
  Mask: TBitmap;
begin
  { 10/26/11: We create an Alpha Mask TBitmap here which
    will be aplied into Canvas }
  Mask := TBitmap.Create;

  with Mask do
  begin
    { Important for AlphaBlend Func }
    PixelFormat := pf32bit;
    { Set Size of Mask }
    Width := Inner.Right - Inner.Left;
    Height := Inner.Bottom - Inner.Top;
  end;

  for Row := 0 to Pred( Mask.Height ) do
  begin
    { Read first Pixel in row }
    Pixel := Mask.Scanline[Row];

    for Col := 0 to Pred( Mask.Width ) do
    begin
      Pixel.Red := 0;
      Pixel.Green := 0;
      Pixel.Blue := 0;

      { Determine Alpha Level }
      case Orientation of
        orHorizontal:
          begin
            Alpha := ( Mask.Height - Row ) / Mask.Height;
            if Col < Mask.Height then
              Alpha := Alpha * ( Col / Mask.Height )
            else if Col > Mask.Width - Mask.Height then
            begin
              Alpha := Alpha * ( ( Mask.Width - Col ) / Mask.Height );
            end;
          end;
      else
        begin
          Alpha := ( Mask.Width - Col ) / Mask.Width;
          if Row < Mask.Width then
            Alpha := Alpha * ( Row / Mask.Width )
          else if Row > Mask.Height - Mask.Width then
          begin
            Alpha := 0; // Alpha * ((Mask.Height - Row) / Mask.Width);
          end;
        end;
      end;

      Alpha := Alpha * 0.20;

      { Convert 0-1 to 0-255 }
      AlphaByte := Round( Alpha * 255.0 );

      { Set Alpha }
      Pixel.Alpha := AlphaByte;

      { Premultiply R, G & B }
      Pixel.Red := Round( Pixel.Red * Alpha );
      Pixel.Green := Round( Pixel.Green * Alpha );
      Pixel.Blue := Round( Pixel.Blue * Alpha );

      { Move to next Pixed }
      Inc( Pixel );

    end; { for Col }

  end; { for Row }

  { Call Blending Func }
  AlphaBlend( Canvas, Mask, Inner.Left, Inner.Top );

  { Release }
  Mask.Free;
end;

procedure DrawSizeGrip( Canvas: TCanvas; GripRect: TRect; Color: TColor );
var
  X, Y: Integer;
begin
  X := GripRect.Right + 1;
  Y := GripRect.Bottom - 0;
  while X > GripRect.Left do
    with Canvas do
    begin
      Pen.Color := Color;
      Polyline( [Point( X, GripRect.Bottom - 1 ), Point( GripRect.Right, Y )] );
      Dec( X, 3 );
      Dec( Y, 3 );
    end;
end;

procedure DrawDropDownMark( Canvas: TCanvas; const X, Y: Integer; Color: TColor );
begin
  with Canvas do
  begin
    Pen.Color := clBlack;
    Brush.Color := Pen.Color;
    Polygon( [Point( X, Y ), Point( X + 4, Y + 4 ), Point( X + 8, Y )] );
  end;
end;

procedure DrawPlus( Canvas: TCanvas; PlusRect: TRect; BrushColor: TColor );
var
  Pos: TPoint;
  BarRect: TRect;
begin
  { Calc. Center }
  Pos := PosInRect( 13, 13, PlusRect, System.Classes.taCenter, taVerticalCenter );

  { Draw Dots }
  with Canvas do
  begin
    Brush.Color := BrushColor;

    BarRect := Rect( Pos.X + 5, Pos.Y, Pos.X + 8, Pos.Y + 13 );
    FillRect( BarRect );

    BarRect := Rect( Pos.X, Pos.Y + 5, Pos.X + 13, Pos.Y + 8 );
    FillRect( BarRect );
  end;
end;

procedure DrawIndicatorArrow( Canvas: TCanvas; Rect: TRect; Color: TColor );
var
  MX, MY: Integer;
begin
  with Canvas do
  begin
    MX := Rect.Left + RectWidth( Rect ) div 2;
    MY := Rect.Top + RectHeight( Rect ) div 2;
    Pen.Color := Color;
    Brush.Color := Color;
    Polygon( [Point( Rect.Left, Rect.Top ), Point( MX, MY ), Point( Rect.Left, Rect.Bottom - 1 ), Point( MX, Rect.Bottom - 1 ), Point( Rect.Right - 1, MY ), Point( MX, Rect.Top ), Point( Rect.Left, Rect.Top )] );
  end;
end;

procedure DrawSortArrow( Canvas: TCanvas; const X, Y: Integer; SortKind: TNxSortKind );
begin

end;

procedure FadeHorz( Canvas: TCanvas; SourceRect: TRect; InvertDirection: Boolean );
var
  Row, Col: Integer;
  Alpha: Single;
  Pixel: PQuadColor;
  AlphaByte: Byte;
  Mask: TBitmap;
begin
  Mask := TBitmap.Create;

  { Used for AlphaBlend Func }
  with Mask do
  begin
    PixelFormat := pf32bit;
    Width := RectWidth( SourceRect );;
    Height := RectHeight( SourceRect );
  end;

  for Row := 0 to Pred( Mask.Height ) do
  begin
    { Read first Pixel in row }
    Pixel := Mask.Scanline[Row];

    for Col := 0 to Pred( Mask.Width ) do
    begin
      { Alpha Level }
      Alpha := Col / Mask.Width; // 0 - 1 of how far along the row this is
      if InvertDirection then
        Alpha := 1 - Alpha;

      AlphaByte := Round( Alpha * 255.0 ); { Convert 0 - 1 to 0 - 255 }

      { Alpha Channel }
      Pixel.Alpha := AlphaByte;

      { Premultiply R, G & B }
      Pixel.Red := Round( Pixel.Red * Alpha );
      Pixel.Green := Round( Pixel.Green * Alpha );
      Pixel.Blue := Round( Pixel.Blue * Alpha );

      { Move to next Pixed }
      Inc( Pixel );
    end;
  end;

  { Call Blending Func }
  AlphaBlend( Canvas, Mask, SourceRect.Left, SourceRect.Top );

  { Destroy TBitmap }
  Mask.Free;
end;

procedure FadeVert( Canvas: TCanvas; SourceRect: TRect );
var
  Row, Col: Integer;
  Alpha: Single;
  Pixel: PQuadColor;
  AlphaByte: Byte;
  Mask: TBitmap;
begin
  { 10/26/11: We create an Alpha Mask TBitmap here which
    will be aplied into Canvas }
  Mask := TBitmap.Create;

  with Mask do
  begin
    { Important for AlphaBlend Func }
    PixelFormat := pf32bit;
    Width := SourceRect.Right - SourceRect.Left;
    Height := SourceRect.Bottom - SourceRect.Top;
  end;

  for Row := 0 to Pred( Mask.Height ) do
  begin
    { Read first Pixel in row }
    Pixel := Mask.Scanline[Row];

    { Determine Alpha Level }
    Alpha := Row / Mask.Height; // 0-1 of how far along the row this is

    AlphaByte := Round( Alpha * 255.0 ); { Convert 0-1 to 0-255 }

    for Col := 0 to Pred( Mask.Width ) do
    begin
      { Alpha Channel }
      Pixel.Alpha := AlphaByte;

      { Premultiply R, G & B }
      Pixel.Red := Round( Pixel.Red * Alpha );
      Pixel.Green := Round( Pixel.Green * Alpha );
      Pixel.Blue := Round( Pixel.Blue * Alpha );

      { Move to next Pixed }
      Inc( Pixel );
    end;
  end;

  { Call Blending Func }
  AlphaBlend( Canvas, Mask, SourceRect.Left, SourceRect.Top );

  { Destroy TBitmap }
  Mask.Free;
end;

procedure FlipVert( Source: TBitmap );
var
  Col, Row: Integer;
  PixelSrc, PixelDest: PQuadColor;
begin
  for Row := 0 to Pred( Source.Height ) do
  begin
    PixelSrc := Source.Scanline[Row];;
    PixelDest := Source.Scanline[Source.Height - 1 - Row];
    for Col := 0 to Pred( Source.Width ) do
    begin
      PixelSrc.Red := PixelDest.Red;
      PixelSrc.Green := PixelDest.Green;
      PixelSrc.Blue := PixelDest.Blue;

      Inc( PixelSrc );
      Inc( PixelDest );
    end;
  end;
end;

function BlendColor( Color1, Color2: TColor; Amount: Byte ): TColor;
var
  Value: Cardinal;
begin
  Assert( Amount in [0 .. 255] );
  Value := Amount xor 255;
  if Integer( Color1 ) < 0 then
    Color1 := GetSysColor( Color1 and $000000FF );
  if Integer( Color2 ) < 0 then
    Color2 := GetSysColor( Color2 and $000000FF );
  Result := Integer( ( ( Cardinal( Color1 ) and $FF00FF ) * Cardinal( Amount ) + ( Cardinal( Color2 ) and $FF00FF ) * Value ) and $FF00FF00 + ( ( Cardinal( Color1 ) and $00FF00 ) * Cardinal( Amount ) + ( Cardinal( Color2 ) and $00FF00 ) *
    Value ) and $00FF0000 ) shr 8;
end;

function ColorToWebColor( Color: TColor ): string;
var
  RealColor: TColorRef;
begin
  RealColor := ColorToRGB( Color );
  Result := Format( '#%.2x%.2x%.2x', [GetRValue( RealColor ), GetGValue( RealColor ), GetBValue( RealColor )] );
end;

procedure EraseBackground( Control: TCustomControl; Canvas: TCanvas );
var
  Shift, Pt: TPoint;
  DC: HDC;
begin
  if Control.Parent = nil then
    Exit;
  if Control.Parent.HandleAllocated then
  begin
    DC := Canvas.Handle;
    Shift.X := 0;
    Shift.Y := 0;
    Shift := Control.Parent.ScreenToClient( Control.ClientToScreen( Shift ) );
    SaveDC( DC );
    try
      OffsetWindowOrgEx( DC, Shift.X, Shift.Y, nil );
      GetBrushOrgEx( DC, Pt );
      SetBrushOrgEx( DC, Pt.X + Shift.X, Pt.Y + Shift.Y, nil );
      Control.Parent.Perform( WM_ERASEBKGND, WParam( DC ), 0 );
      Control.Parent.Perform( WM_PAINT, WParam( DC ), 0 );
    finally
      RestoreDC( DC, -1 );
    end;
  end;
end;

function WebColorToColor( WebColor: string ): TColor;
begin
  try
    { Delete "#" Char }
    Delete( WebColor, 1, 1 );

    { Alpha Channel }
    WebColor := '$00' + WebColor;

    Result := StringToColor( WebColor );

    { Swap Red & Blue }
    Result := ( ( ( Result and $FF ) shl 16 ) + ( Result and $FF00 ) + ( Result and $FF0000 ) shr 16 );
  except
    Result := clNone;
  end;
end;

procedure GetRGB( C: TColor; out R, G, B: Byte );
begin
  if Integer( C ) < 0 then
    C := GetSysColor( C and $000000FF );
  R := C and $FF;
  G := C shr 8 and $FF;
  B := C shr 16 and $FF;
end;

function GetSystemColor( Color: TColor ): TColor;
begin
  if Integer( Color ) < 0 then
    Result := GetSysColor( Color and $000000FF )
  else
    Result := Color;
end;

function HTMLColorToString( Color: TColor ): string;
begin
  if Integer( Color ) < 0 then
    Color := GetSysColor( Color and $000000FF );
  Result := '#' + IntToHex( ( ( Color and $FF ) shl 16 ) + ( Color and $FF00 ) + ( Color and $FF0000 ) shr 16, 6 );
end;

function DarkerColor( Color: TColor; Percent: Byte ): TColor;
var
  R, G, B: Byte;
begin
  Color := ColorToRGB( Color );
  R := GetRValue( Color );
  G := GetGValue( Color );
  B := GetBValue( Color );
  R := R - MulDiv( R, Percent, 100 ); // Percent% closer to black
  G := G - MulDiv( G, Percent, 100 );
  B := B - MulDiv( B, Percent, 100 );
  Result := RGB( R, G, B );
end;

function IsColorLight( Color: TColor ): Boolean;
begin
  Color := ColorToRGB( Color );
  Result := ( ( Color and $FF ) + ( Color shr 8 and $FF ) + ( Color shr 16 and $FF ) ) >= $180;
end;

function LighterColor( Color: TColor; Percent: Byte ): TColor;
var
  R, G, B: Byte;
begin
  Color := ColorToRGB( Color );
  R := GetRValue( Color );
  G := GetGValue( Color );
  B := GetBValue( Color );
  R := R + MulDiv( 255 - R, Percent, 100 );
  G := G + MulDiv( 255 - G, Percent, 100 );
  B := B + MulDiv( 255 - B, Percent, 100 );
  Result := RGB( R, G, B );
end;

function ColorBrightness( const Color: TColor ): Byte;
var
  R, G, B: Byte;
begin
  R := Color and $FF;
  G := ( Color shr 8 ) and $FF;
  B := ( Color shr 16 ) and $FF;
  Result := ( Max( Max( R, G ), B ) + Min( Min( R, G ), B ) ) div 2;
end;

procedure ConstrainedRect( Graphic: TGraphic; var Src: TRect );
var
  ScaleHor, ScaleVer, Scale: Double;
  NewWidth, NewHeight, ATop, ALeft: Integer;
begin
  ScaleHor := ( Src.Right - Src.Left ) / Graphic.Width;
  ScaleVer := ( Src.Bottom - Src.Top ) / Graphic.Height;
  Scale := Math.Min( ScaleHor, ScaleVer );
  NewWidth := Round( Graphic.Width * Scale );
  NewHeight := Round( Graphic.Height * Scale );

  if ScaleHor < ScaleVer then
  begin
    NewWidth := Src.Right - Src.Left;
    ATop := Src.Top + ( ( Src.Bottom - Src.Top ) div 2 - ( NewHeight div 2 ) );
    Src := Rect( Src.Left, ATop, Src.Left + NewWidth, ATop + NewHeight );
  end
  else
  begin
    NewHeight := Src.Bottom - Src.Top;
    ALeft := Src.Left + ( ( Src.Right - Src.Left ) div 2 - ( NewWidth div 2 ) );
    Src := Rect( ALeft, Src.Top, ALeft + NewWidth, Src.Top + NewHeight );
  end;
end;

function RandomColor: TColor;
begin
  Randomize;
  Result := RGB( Random( 255 ), Random( 255 ), Random( 255 ) );
end;

function RectInRect( Container, Target: TRect ): Boolean;
begin
  Result := PtInRect( Container, Target.TopLeft ) and PtInRect( Container, Point( Pred( Target.Right ), Pred( Target.Bottom ) ) );
end;

function RectIntersect( Rect1, Rect2: TRect ): Boolean;
begin
  Result := not( ( Rect1.Right < Rect2.Left ) or ( Rect1.Left > Rect2.Right ) or ( Rect1.Bottom < Rect2.Top ) or ( Rect1.Top > Rect2.Bottom ) );
end;

procedure SetFontQuality( Font: TFont; Quality: Byte );
var
  LogFont: TLogFont;
begin
  if GetObject( Font.Handle, SizeOf( TLogFont ), @LogFont ) = 0 then
    RaiseLastOSError;
  LogFont.lfQuality := Quality;
  Font.Handle := CreateFontIndirect( LogFont );
end;

function PosInRect( Width, Height: Integer; Container: TRect; Alignment: TAlignment; VerticalAlignment: TVerticalAlignment ): TPoint;
begin
  case Alignment of
    taLeftJustify: Result.X := Container.Left;
    taRightJustify: Result.X := Container.Right - Width;
    System.Classes.taCenter: Result.X := CenterPoint( Container ).X - Floor( Width / 2 );
  end;
  case VerticalAlignment of
    taAlignTop: Result.Y := Container.Top;
    taAlignBottom: Result.Y := Container.Bottom - Height;
    taVerticalCenter: Result.Y := CenterPoint( Container ).Y - Floor( Height / 2 );
  end;
end;

procedure SetClipRect( Canvas: TCanvas; ClipRect: TRect );
var
  CLPRGN: HRGN;
  P: TPoint;
begin
  { TODO: use RectVisible WinApi }
  with ClipRect do
    CLPRGN := CreateRectRgn( Left, Top, Right, Bottom );
  try
    GetWindowOrgEx( Canvas.Handle, P );
    OffsetRgn( CLPRGN, -P.X, -P.Y );

    SelectClipRgn( Canvas.Handle, CLPRGN );
  finally
    DeleteObject( CLPRGN );
  end;
end;

procedure ExcludeClipRect( Canvas: TCanvas; ClipRect: TRect );
begin
  with ClipRect do
    Windows.ExcludeClipRect( Canvas.Handle, Left, Top, Left + ( Right - Left ), Top + ( Bottom - Top ) );
end;

procedure FillPadding( Canvas: TCanvas; Color: TColor; Dest: TRect; Padding: TPadding );
begin
  with Canvas, Padding do
  begin
    FillRect( Rect( Dest.Left + Left, Dest.Top, Dest.Right - Right, Dest.Top + Top ) );
    FillRect( Rect( Dest.Right - Right, Dest.Top, Dest.Right, Dest.Bottom ) );
    FillRect( Rect( Dest.Left + Left, Dest.Bottom - Bottom, Dest.Right - Right, Dest.Bottom ) );
    FillRect( Rect( Dest.Left, Dest.Top, Dest.Left + Left, Dest.Bottom ) );
  end;
end;

function FitToRect( Source, Destination: TRect ): TRect;
begin
  Result := Source;
  with Result do
  begin
    if Result.Left < Destination.Left then
      Result.Left := Destination.Left;
    if Result.Top < Destination.Top then
      Result.Top := Destination.Top;
    if Result.Right > Destination.Right then
      Result.Right := Destination.Right;
    if Result.Bottom > Destination.Bottom then
      Result.Bottom := Destination.Bottom;
  end;
end;

function PaintGraphic( Canvas: TCanvas; Graphic: TGraphic; Dest: TRect; FillMode: TNxFillMode ): TRect;
var
  ClipRect: TRect;

  procedure DrawAspectFillPicture;
  var
    GraphicRect: TRect;
  begin
    GraphicRect := Bounds( 0, 0, Graphic.Width, Graphic.Height );
    GraphicRect := CenteredRect( Dest, GraphicRect );

    if RectWidth( Result ) > RectWidth( GraphicRect ) then
    begin
      Result.Left := GraphicRect.Left;
      Result.Right := GraphicRect.Right;
    end;

    if RectHeight( Result ) > RectHeight( GraphicRect ) then
    begin
      Result.Top := GraphicRect.Top;
      Result.Bottom := GraphicRect.Bottom;
    end;

    SetClipRect( Canvas, Result );
    Canvas.Draw( GraphicRect.Left, GraphicRect.Top, Graphic );
  end;

  procedure DrawConstrainedPicture;
  var
    ScaleHor, ScaleVer, Scale: Double;
    NewWidth, NewHeight, ATop, ALeft: Integer;
  begin
    with Dest do
    begin
      ScaleHor := ( Right - Left ) / Graphic.Width;
      ScaleVer := ( Bottom - Top ) / Graphic.Height;
      Scale := Min( ScaleHor, ScaleVer );

      NewWidth := Round( Graphic.Width * Scale );
      NewHeight := Round( Graphic.Height * Scale );

      if ScaleHor < ScaleVer then
      begin
        NewWidth := Right - Left;
        ATop := Top + ( ( Bottom - Top ) div 2 - ( NewHeight div 2 ) );
        Result := Rect( Left, ATop, Left + NewWidth, ATop + NewHeight );
      end
      else
      begin
        NewHeight := Bottom - Top;
        ALeft := Left + ( ( Right - Left ) div 2 - ( NewWidth div 2 ) );
        Result := Rect( ALeft, Top, ALeft + NewWidth, Top + NewHeight );
      end;
      Canvas.StretchDraw( Result, Graphic );
    end;
  end;

  procedure DrawCenterPicture;
  begin
    Result := CenteredRect( Dest, Bounds( 0, 0, Graphic.Width, Graphic.Height ) );

    SetClipRect( Canvas, Dest );
    Canvas.Draw( Result.Left, Result.Top, Graphic );

    Result := FitToRect( Result, Dest );
  end;

begin
  ClipRect := Canvas.ClipRect;
  Result := Dest;
  try
    case FillMode of
      fmAspectFit: DrawConstrainedPicture;
      fmAspectFill: DrawAspectFillPicture;
      fmCenter: DrawCenterPicture;
      fmScaleToFill: Canvas.StretchDraw( Dest, Graphic );
    end;
  finally
    SetClipRect( Canvas, ClipRect );
  end;
end;

function DotToPixel( Value: TRect ): TRect; overload;
begin
  with Result do
  begin
    Left := Value.Left * ( Screen.PixelsPerInch div 72 );
    Top := Value.Top * ( Screen.PixelsPerInch div 72 );
    Right := Value.Right * ( Screen.PixelsPerInch div 72 );
    Bottom := Value.Bottom * ( Screen.PixelsPerInch div 72 );
  end;
end;

function DotToPixel( Value: TSize ): TSize; overload;
begin
  with Result do
  begin
    cx := Value.cx * ( Screen.PixelsPerInch div 72 );
    cy := Value.cy * ( Screen.PixelsPerInch div 72 );
  end;
end;

function DotToPixel( Value: Integer ): Integer; overload;
begin
  Result := Value * ( Screen.PixelsPerInch div 72 );
end;

function DeviceIndipendedPoint( Value: Integer ): Integer; overload;
begin
  Result := Value * ( Screen.PixelsPerInch div 72 );
end;

function DeviceIndipendedPoint( Value: TPadding ): TPadding; overload;
begin
  Result := Value;
  Result.Left := Value.Left * ( Screen.PixelsPerInch div 72 );
  Result.Top := Value.Top * ( Screen.PixelsPerInch div 72 );
  Result.Right := Value.Right * ( Screen.PixelsPerInch div 72 );
  Result.Bottom := Value.Bottom * ( Screen.PixelsPerInch div 72 );
end;

function GetSpinEditCursor( const AControlRect: TRect; const X, Y: Integer ): TCursor;
var
  EditRect, ButtonsRect: TRect;
begin
  EditRect := AControlRect;
  ButtonsRect := EditRect;
  ButtonsRect.Inflate( -2, -2 );

  ButtonsRect.Left := ButtonsRect.Right - intSpinButtonWidth * 2;

  if ButtonsRect.Contains( Point( X, Y ) ) then
    Result := crHandPoint
  else if EditRect.Contains( Point( X, Y ) ) then
    Result := crIBeam
  else
    Result := crDefault;
end;

function GetDropDownControlCursor( ACtrlRect: TRect; APt: TPoint ): TCursor;
var
  EditRect, btnRect: TRect;
begin
  EditRect := ACtrlRect;
  btnRect := EditRect;
  Dec( EditRect.Right, DropDownButtonWidth );
  btnRect.Left := EditRect.Right;
  if EditRect.Contains( APt ) then
    Result := crIBeam
  else if btnRect.Contains( APt ) then
    Result := crHandPoint
  else
    Result := crDefault;
end;

function GetGeometryColor( State: TdcButtonState ): TColor;
begin
  Result := clGlyph;
  if btPushed in State then
    Result := clWhite;
  if btDisabled in State then
    Result := clGray;
end;

function GetResourceGraphic( const ResourceId: string ): TGraphic;
var
  ResourceName: string;
  HResInfo: THandle;
begin
  Result := TPngImage.Create;
  ResourceName := ResourceId.ToUpper( );
  HResInfo := FindResource( HInstance, PChar( ResourceId ), 'PNG' );
  if HResInfo = 0 then
    Exit;
  TPngImage( Result ).LoadFromRHResourceName( HInstance, ResourceName );
end;

end.
