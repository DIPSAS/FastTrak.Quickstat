unit Emetra.VclUtil.ColorSet;
{$M+}
interface

uses
  Emetra.VclUtil.ColorSet.Interfaces,
  Emetra.VclUtil.ColorCalculator,
  {Standard}
  System.Classes, Vcl.Graphics;

type
  TBasicColorSet = class( TInterfacedObject, IGuiListBoxColorSet )
  strict private
    fCodeColor: TColor;
    fTextColor: TColor;
    fFocusedSelectionColor: TColor;
    fHyperlinkColor: TColor;
    fUnfocusedSelectionColor: TColor;
    fFirstInfoColor: TColor;
    fSecondInfoColor: TColor;
    fStatusTextColor: TColor;
  private
    { Property accessors }
    function Get_CodeColor: TColor;
    function Get_TextColor: TColor;
    function Get_FirstInfoColor: TColor;
    function Get_HyperlinkColor: TColor;
    function Get_SecondInfoColor: TColor;
    function Get_FocusedSelectionColor: TColor;
    function Get_StatusTextColor: TColor;
    function Get_UnfocusedSelectionColor: TColor;
  public
    { Initialization }
    constructor Create;
  published
    { Properties }
    property CodeColor: TColor read Get_CodeColor;
    property TextColor: TColor read Get_TextColor write fTextColor;
    property FirstInfoColor: TColor read Get_FirstInfoColor;
    property HyperlinkColor: TColor read Get_HyperlinkColor;
    property SecondInfoColor: TColor read Get_SecondInfoColor;
    property FocusedSelectionColor: TColor read Get_FocusedSelectionColor;
    property StatusTextColor: TColor read Get_StatusTextColor;
    property UnfocusedSelectionColor: TColor read Get_UnfocusedSelectionColor;
  end;

  TGuiColorSet = class( TBasicColorSet, IHtmlColorSet, IGuiColorSet )
  strict private
    FBase: TColor;
    FMedium: TColor;
    FLight: TColor;
    FVeryLight: TColor;
    FVeryDarkColor: TColor;
    FCurrCell: TColor;
    FCurrRow: TColor;
    FDarkColor: TColor;
  private
    { Property accessors }
    function Get_BaseColor: TColor;
    function Get_Code: string;
    function Get_CurrentCellColor: TColor;
    function Get_Dark: string;
    function Get_DarkColor: TColor;
    function Get_DialogColor: TColor;
    function Get_HeaderColor: TColor;
    function Get_Light: string;
    function Get_LightColor: TColor;
    function Get_Medium: string;
    function Get_MediumColor: TColor;
    function Get_PrettyDarkColor: TColor;
    function Get_TextColor: TColor;
    function Get_VeryDarkColor: TColor;
    function Get_VeryLight: string;
    function Get_VeryLightColor: TColor;
  protected
    procedure SetBaseColor( const ABaseColor: TColor = clNone ); dynamic;
  public
    property clCurrRow: TColor read FCurrRow default clWebAliceBlue;
    property clDark: TColor read FDarkColor;
    property clVeryLight: TColor read FVeryLight;
  published
    { Arena Colors }
    property DialogColor: TColor read Get_DialogColor;
    property HeaderColor: TColor read Get_HeaderColor;
    { GUI colors }
    property BaseColor: TColor read Get_BaseColor;
    property CurrentCellColor: TColor read Get_CurrentCellColor default clSkyBlue;
    property DarkColor: TColor read Get_DarkColor;
    property LightColor: TColor read Get_LightColor default clWebWhiteSmoke;
    property MediumColor: TColor read Get_MediumColor;
    property PrettyDarkColor: TColor read Get_PrettyDarkColor;
    property VeryDarkColor: TColor read Get_VeryDarkColor;
    property VeryLightColor: TColor read Get_VeryLightColor;
    { Html colors }
    property Code: string read Get_Code;
    property Dark: string read Get_Dark;
    property Light: string read Get_Light;
    property Medium: string read Get_Medium;
    property VeryLight: string read Get_VeryLight;
  end;

const
  { Some suitable base colors }
  clColdGray          = $00635C59;
  clGreenGray         = $00586359;
  clRedGray           = $005C5C8C;
  clLavenderGray      = $0062585E;
  clBlueGray          = $007C5C5C;
  clBrownGray         = $005C6266;
  clLightYellowOrange = $0066E0FF;

implementation

uses
  Emetra.Vcl.Consts;

{ TBasicColorSet }

constructor TBasicColorSet.Create;
begin
  inherited Create;
  fCodeColor := clCodeColor;
  fTextColor := clTextColor;
  fFocusedSelectionColor := clSelectedBk;// clFocusedSelectionColor;
  fHyperlinkColor := clHyperlinkForeground;
  fUnfocusedSelectionColor := clUnfocusedSelectionColor;
  fStatusTextColor := clStatusTextColor;
  fFirstInfoColor := clFirstInfoColor;
  fSecondInfoColor := clSecondInfoColor;
end;

function TBasicColorSet.Get_CodeColor: TColor;
begin
  Result := fCodeColor;
end;

function TBasicColorSet.Get_FirstInfoColor: TColor;
begin
  Result := fFirstInfoColor;
end;

function TBasicColorSet.Get_FocusedSelectionColor: TColor;
begin
  Result := fFocusedSelectionColor;
end;

function TBasicColorSet.Get_HyperlinkColor: TColor;
begin
  Result := fHyperlinkColor;
end;

function TBasicColorSet.Get_SecondInfoColor: TColor;
begin
  Result := fSecondInfoColor;
end;

function TBasicColorSet.Get_StatusTextColor: TColor;
begin
  Result := fStatusTextColor;
end;

function TBasicColorSet.Get_TextColor: TColor;
begin
  Result := fTextColor;
end;

function TBasicColorSet.Get_UnfocusedSelectionColor: TColor;
begin
  Result := fUnfocusedSelectionColor;
end;

{$REGION 'TGuiColorSet'}

procedure TGuiColorSet.SetBaseColor( const ABaseColor: TColor = clNone );
begin
  if ABaseColor = clNone then
{$IFDEF Debug}
    FBase := clRedGray
{$ELSE}
    FBase := clBlueGray
{$ENDIF}
  else
    FBase := ABaseColor;
  FMedium := TColorCalculator.BlendColors( FBase, clWhite, 75 );
  FLight := TColorCalculator.BlendColors( FMedium, clWhite, 50 );
  FDarkColor := TColorCalculator.BlendColors( FBase, clBlack, 25 );
  FVeryDarkColor := TColorCalculator.BlendColors( FDarkColor, clBlack, 50 );
  FCurrRow := TColorCalculator.BlendColors( FocusedSelectionColor, clWhite, 50 );
  FCurrCell := clSkyBlue;
  FVeryLight := TColorCalculator.BlendColors( FLight, clWhite, 75 );
  { Listbox color }
  TextColor := fVeryDarkColor;
end;

function TGuiColorSet.Get_Medium: string;
begin
  Result := TColorCalculator.HtmlColor( FMedium );
end;

function TGuiColorSet.Get_MediumColor: TColor;
begin
  Result := FMedium;
end;

function TGuiColorSet.Get_LightColor;
begin
  Result := FLight;
end;

function TGuiColorSet.Get_Light: string;
begin
  Result := TColorCalculator.HtmlColor( FLight );
end;

function TGuiColorSet.Get_VeryLight: string;
begin
  Result := TColorCalculator.HtmlColor( FVeryLight );
end;

function TGuiColorSet.Get_VeryLightColor: TColor;
begin
  Result := FVeryLight;
end;

function TGuiColorSet.Get_CurrentCellColor: TColor;
begin
  Result := FCurrCell;
end;

function TGuiColorSet.Get_BaseColor: TColor;
begin
  Result := FBase;
end;

function TGuiColorSet.Get_Code: string;
begin
  Result := TColorCalculator.HtmlColor( CodeColor );
end;

function TGuiColorSet.Get_Dark: string;
begin
  Result := TColorCalculator.HtmlColor( FDarkColor );
end;

function TGuiColorSet.Get_DarkColor;
begin
  Result := FDarkColor;
end;

function TGuiColorSet.Get_DialogColor: TColor;
begin
  Result := clDlgFace;
end;

function TGuiColorSet.Get_HeaderColor: TColor;
begin
  Result := clHeaderBk;
end;

function TGuiColorSet.Get_TextColor: TColor;
begin
  Result := FDarkColor;
end;

function TGuiColorSet.Get_PrettyDarkColor;
begin
  Result := TColorCalculator.BlendColors( FMedium, FDarkColor, 50 );
end;

function TGuiColorSet.Get_VeryDarkColor;
begin
  Result := FVeryDarkColor;
end;

{$ENDREGION}

end.
