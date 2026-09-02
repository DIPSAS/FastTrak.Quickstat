unit Emetra.VclUtil.ColorCalculator;

interface

uses
  Classes, Graphics, Windows;

type
  TColorCalculator = class( TInterfacedPersistent )
  public
    class function BlendColors( const AStartAt, AStopAt: TColor; const APercent: integer ): TColor;
    class function Complement( AColor: TColor ): TColor;
    class function HSLRangeToRGB( H, S, L: integer ): TColor;
    class function HSLtoRGB( H, S, L: double ): TColor;
    class function SelectFontColor( const AColor: TColor ): TColor;
    class function HtmlColor( const AColor: TColor ): string;
    class function IsLightColor( const AColor: TColor ): boolean;
    class procedure RGBtoHSL( RGB: TColor; var H, S, L: double );
    class procedure RGBtoHSLRange( RGB: TColor; var H, S, L: integer );
end;

implementation

uses
  SysUtils;

const
  HSLRange = 240;

function GetByte( Value: TColor; Shift: byte ): byte;
begin
  Result := ( Value and ( $FF shl Shift ) ) shr Shift;
end;

class function TColorCalculator.Complement( AColor: TColor ): TColor;
var
  H, S, L: double;
begin
  RGBtoHSL( AColor, H, S, L );
  H := ( H + 0.5 );
  if H > 1 then
    H := H - 1;
  Result := HSLtoRGB( H, S, L );
end;

class function TColorCalculator.HSLtoRGB( H, S, L: double ): TColor;
var
  M1, M2: double;

  function HueToColourValue( Hue: double ): byte;
  var
    V: double;
  begin
    if Hue < 0 then
      Hue := Hue + 1
    else if Hue > 1 then
      Hue := Hue - 1;

    if 6 * Hue < 1 then
      V := M1 + ( M2 - M1 ) * Hue * 6
    else if 2 * Hue < 1 then
      V := M2
    else if 3 * Hue < 2 then
      V := M1 + ( M2 - M1 ) * ( 2 / 3 - Hue ) * 6
    else
      V := M1;
    Result := round( 255 * V )
  end;

var
  R, G, B: byte;
begin
  if S = 0 then
  begin
    R := round( 255 * L );
    G := R;
    B := R
  end
  else
  begin
    if L <= 0.5 then
      M2 := L * ( 1 + S )
    else
      M2 := L + S - L * S;
    M1 := 2 * L - M2;
    R := HueToColourValue( H + 1 / 3 );
    G := HueToColourValue( H );
    B := HueToColourValue( H - 1 / 3 )
  end;

  Result := RGB( R, G, B )
end;

class function TColorCalculator.HtmlColor( const AColor: TColor ): string;
begin
  Result := Format( '%.6x', [ AColor ] );
  Result := '#' + Copy( Result, 5, 2 ) + Copy( Result, 3, 2 ) + Copy( Result, 1, 2 );
end;

class function TColorCalculator.IsLightColor( const AColor: TColor ): boolean;
var
  c: TColor;
begin
  c := ColorToRGB( AColor );
  Result := ( ( c and $FF ) + ( c shr 8 and $FF ) + ( c shr 16 and $FF ) ) >= $180;
end;

class procedure TColorCalculator.RGBtoHSL( RGB: TColor; var H, S, L: double );

  function Max( a, B: double ): double;
  begin
    if a > B then
      Result := a
    else
      Result := B
  end;

  function Min( a, B: double ): double;
  begin
    if a < B then
      Result := a
    else
      Result := B
  end;

var
  R, G, B, D, Cmax, Cmin: double;

begin
  R := GetRValue( RGB ) / 255;
  G := GetGValue( RGB ) / 255;
  B := GetBValue( RGB ) / 255;
  Cmax := Max( R, Max( G, B ) );
  Cmin := Min( R, Min( G, B ) );

  // calculate luminosity
  L := ( Cmax + Cmin ) / 2;

  if Cmax = Cmin then // it's grey
  begin
    H := 0; // it's actually undefined
    S := 0
  end
  else
  begin
    D := Cmax - Cmin;

    // calculate Saturation
    if L < 0.5 then
      S := D / ( Cmax + Cmin )
    else
      S := D / ( 2 - Cmax - Cmin );

    // calculate Hue
    if R = Cmax then
      H := ( G - B ) / D
    else if G = Cmax then
      H := 2 + ( B - R ) / D
    else
      H := 4 + ( R - G ) / D;

    H := H / 6;
    if H < 0 then
      H := H + 1
  end
end;

class function TColorCalculator.HSLRangeToRGB( H, S, L: integer ): TColor;
begin
  Result := HSLtoRGB( H / ( HSLRange - 1 ), S / HSLRange, L / HSLRange )
end;

class procedure TColorCalculator.RGBtoHSLRange( RGB: TColor; var H, S, L: integer );
var
  Hd, Sd, Ld: double;
begin
  RGBtoHSL( RGB, Hd, Sd, Ld );
  H := round( Hd * ( HSLRange - 1 ) );
  S := round( Sd * HSLRange );
  L := round( Ld * HSLRange );
end;

class function TColorCalculator.SelectFontColor( const AColor: TColor ): TColor;
var
  H, S, L: integer;
begin
  if ( AColor = clHighlight ) then
    Result := clHighlightText
  else if ( AColor = clWhite ) then
    Result := clBlack
  else if ( AColor = clBlack ) or ( AColor = clBlue ) or ( AColor = clRed ) or ( AColor = clTeal ) then
    Result := clWhite
  else if ( AColor = clWindow ) then
    Result := clWindowText
  else if ( AColor = clBtnFace ) then
    Result := clBtnText
  else if ( AColor = clInfoBk ) then
    Result := clInfoText
  else
  begin
    RGBtoHSLRange( AColor, H, S, L );
    case L of
      128 .. 255:
        L := 0;
      0 .. 127:
        L := 240;
    end;
    Result := HSLRangeToRGB( H, S, L );
  end
end;

class function TColorCalculator.BlendColors( const AStartAt, AStopAt: TColor; const APercent: integer ): TColor;
const
  steps = 100;
var
  IncRed, IncGreen, IncBlue: integer; // Must be able to hold negative values
  FromRed, ToRed: integer;
  FromGreen, ToGreen: integer;
  FromBlue, ToBlue: integer;
  highByte: byte;
begin
  Result := $00808080;
  highByte := GetByte( AStartAt, 24 );
  if not( highByte in [ $00, $02 ] ) then
  else
  begin
    FromRed := GetByte( AStartAt, 0 );
    ToRed := GetByte( AStopAt, 0 );
    IncRed := round( ( ToRed - FromRed ) * APercent / steps );

    FromBlue := GetByte( AStartAt, 16 );
    ToBlue := GetByte( AStopAt, 16 );
    IncBlue := round( ( ToBlue - FromBlue ) * APercent / steps );

    FromGreen := GetByte( AStartAt, 8 );
    ToGreen := GetByte( AStopAt, 8 );
    IncGreen := round( ( ToGreen - FromGreen ) * APercent / steps );
    Result := RGB( FromRed + IncRed, FromGreen + IncGreen, FromBlue + IncBlue );
  end;
end;

end.
