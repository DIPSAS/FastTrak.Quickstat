unit Emetra.Vcl.Glyphs;

interface

uses
  System.Types, System.Classes, System.SysUtils,
  Vcl.Graphics,
  Vcl.Imaging.pngimage,
  Emetra.Vcl.Graphics,
  Emetra.Vcl.Helpers;

type
  TdcGlyphState = ( gtDark, gtLight, gtColor, gtDisabled );

  TdcGlyphResource = (
    grNone,
    grApprove,
    grArrowThinRight,
    grCancel,
    grChevronLeft,
    grChevronPageFirst,
    grChevronPageLast,
    grChevronRight,
    grCircleClose,
    grCircleHelp,
    grCircleOutlineHelp,
    grDataReuse,
    grDataReuseEnd,
    grDelete,
    grDragHorizontal,
    grListRemove,
    grMinus,
    grPatientsGroup,
    grPlus,
    grRefresh,
    grSearch,
    grSave,
    grToggleSwitchOff,
    grToggleSwitchOn
  );

  TdcGlyphDimensions = ( gd16x16, gd24x24, gdIntrinsic );

  { TGlyphLocator }

  TGlyphLocator = class( TPersistent )
  private
    { Property Fields }
    FGlyph: TdcGlyphResource;
    FOnChange: TNotifyEvent;
    FDimensions: TdcGlyphDimensions;
    { Property Accessors }
    procedure SetGlyph( const Value: TdcGlyphResource );
    procedure SetDimensions( const Value: TdcGlyphDimensions );
  protected
    procedure DoChange; dynamic;
  public
    constructor Create;
    procedure Assign( Source: TPersistent ); override;
    function GraphicSize( State: TdcGlyphState ): TSize;
    function HasGlyph: boolean;
  published
    { Properties }
    property Glyph: TdcGlyphResource read FGlyph write SetGlyph default grNone;
    property Dimensions: TdcGlyphDimensions read FDimensions write SetDimensions default gdIntrinsic;
    property OnChange: TNotifyEvent read FOnChange write FOnChange;
  end;

var
  Glyphs: array [TdcGlyphResource, TdcGlyphState, TdcGlyphDimensions] of TGraphic;

function FindGraphic( Glyph: TdcGlyphResource; State: TdcGlyphState; Dimensions: TdcGlyphDimensions = gdIntrinsic ): TGraphic;

function LocateGlyph( Locator: TGlyphLocator; State: TdcGlyphState ): TGraphic;
function GetGlyphSize( Locator: TGlyphLocator; State: TdcGlyphState ): TSize;
procedure Draw( Canvas: TCanvas; Locator: TGlyphLocator; X, Y: integer; State: TdcGlyphState );

implementation

{ TGlyphLookup }

procedure TGlyphLocator.Assign(Source: TPersistent);
begin
  if Source is TGlyphLocator then
  begin
    Dimensions := TGlyphLocator( Source ).Dimensions;
    Glyph := TGlyphLocator( Source ).Glyph;
  end;
end;

constructor TGlyphLocator.Create;
begin
  FGlyph := grNone;
  FDimensions := gdIntrinsic;
end;

procedure TGlyphLocator.DoChange;
begin
  if Assigned( FOnChange ) then
    FOnChange( Self );
end;

function TGlyphLocator.GraphicSize( State: TdcGlyphState ): TSize;
var
  Glyph: TGraphic;
begin
  Result := TSize.Create( 0, 0 );
  Glyph := LocateGlyph( Self, State );
  if Assigned( Glyph ) and not Glyph.Empty then
    Result := TSize.Create( Glyph.Width, Glyph.Height );
end;

function TGlyphLocator.HasGlyph: boolean;
begin
  Result := Glyph <> grNone;
end;

procedure TGlyphLocator.SetGlyph( const Value: TdcGlyphResource );
begin
  if Value <> FGlyph then
  begin
    FGlyph := Value;
    DoChange;
  end;
end;

procedure TGlyphLocator.SetDimensions( const Value: TdcGlyphDimensions );
begin
  if Value <> FDimensions then
  begin
    FDimensions := Value;
    DoChange;
  end;
end;

{ Standalone functions }

/// <summary>
/// Map glyph type to a resource ID inside RES file.
/// </summary>
function GetGlyphResourceID( Glyph: TdcGlyphResource ): string;
begin
  case Glyph of
    grApprove: Result := 'Approve';
    grArrowThinRight: Result := 'Arrow_Thin_Right';
    grCancel: Result := 'Cancel';
    grChevronLeft: Result := 'Chevron_Left';
    grChevronPageFirst: Result := 'Chevron_Page_First';
    grChevronPageLast: Result := 'Chevron_Page_Last';
    grChevronRight: Result := 'Chevron_Right';
    grCircleClose: Result := 'Circle_Close';
    grCircleHelp: Result := 'Circle_Help';
    grCircleOutlineHelp: Result := 'Circle_Outline_Help';
    grDataReuse: Result := 'DataReuse';
    grDataReuseEnd: Result := 'DataReuseEnd';
    grDelete: Result := 'Delete';
    grDragHorizontal: Result := 'Drag_Horizontal';
    grListRemove: Result := 'List_Remove';
    grMinus: Result := 'Minus';
    grPatientsGroup: Result := 'Patients_Group';
    grPlus: Result := 'Plus';
    grRefresh: Result := 'Refresh';
    grSave: Result := 'Save';
    grSearch: Result := 'Search';
    grToggleSwitchOff: Result := 'ToggleSwitch_Off';
    grToggleSwitchOn: Result := 'ToggleSwitch_On';
  else Result := '';
  end;
end;

procedure AddResource( Glyph: TdcGlyphResource );
var
  Resource: string;
begin
  Resource := GetGlyphResourceID( Glyph );

  Glyphs[Glyph, gtDark, gd16x16] := GetResourceGraphic( Resource + '.Dark.16x16' );
  Glyphs[Glyph, gtLight, gd16x16] := GetResourceGraphic( Resource + '.Light.16x16' );
  Glyphs[Glyph, gtDisabled, gd16x16] := GetResourceGraphic( Resource + '.#80444444.16x16' );

  Glyphs[Glyph, gtColor, gd24x24] := GetResourceGraphic( Resource + '.Color.24x24' );
  Glyphs[Glyph, gtColor, gdIntrinsic] := GetResourceGraphic( Resource + '.Color.AutoSize' );

  Glyphs[Glyph, gtDark, gdIntrinsic] := GetResourceGraphic( Resource + '.Dark.AutoSize' );
  Glyphs[Glyph, gtLight, gdIntrinsic] := GetResourceGraphic( Resource + '.Light.AutoSize' );
  Glyphs[Glyph, gtDisabled, gdIntrinsic] := GetResourceGraphic( Resource + '.#80444444.AutoSize' );
end;

procedure DestroyResource( AResource: TdcGlyphResource );
begin
  Glyphs[AResource, gtDark, gd16x16].Free;
  Glyphs[AResource, gtLight, gd16x16].Free;
  Glyphs[AResource, gtColor, gd16x16].Free;
  Glyphs[AResource, gtDisabled, gd16x16].Free;

  Glyphs[AResource, gtDark, gd24x24].Free;
  Glyphs[AResource, gtLight, gd24x24].Free;
  Glyphs[AResource, gtColor, gd24x24].Free;
  Glyphs[AResource, gtDisabled, gd24x24].Free;

  Glyphs[AResource, gtDark, gdIntrinsic].Free;
  Glyphs[AResource, gtLight, gdIntrinsic].Free;
  Glyphs[AResource, gtColor, gdIntrinsic].Free;
  Glyphs[AResource, gtDisabled, gdIntrinsic].Free;
end;

procedure Draw( Canvas: TCanvas; Locator: TGlyphLocator; X, Y: integer; State: TdcGlyphState );
var
  Graphic: TGraphic;
begin
  Graphic := LocateGlyph( Locator, State );
  if Assigned( Graphic ) and not Graphic.Empty then
    Canvas.Draw( X, Y, Graphic );
end;

function FindGraphic( Glyph: TdcGlyphResource; State: TdcGlyphState; Dimensions: TdcGlyphDimensions = gdIntrinsic ): TGraphic;
begin
  Result := Glyphs[Glyph, State, Dimensions];
end;

function GetGlyphSize( Locator: TGlyphLocator; State: TdcGlyphState ): TSize;
var
  Glyph: TGraphic;
begin
  Glyph := LocateGlyph( Locator, State );
  if Assigned( Glyph ) and not Glyph.Empty then
    Result := TSize.Create( Glyph.Width, Glyph.Height );
end;

/// <summary>
/// Finds TGraphic instance inside the Glyphs array.
/// </summary>
function LocateGlyph( Locator: TGlyphLocator; State: TdcGlyphState ): TGraphic;
begin
  Result := Glyphs[Locator.Glyph, State, Locator.Dimensions];
end;

initialization

AddResource( grApprove );
AddResource( grArrowThinRight );
AddResource( grCancel );
AddResource( grChevronLeft );
AddResource( grChevronRight );
AddResource( grChevronPageFirst );
AddResource( grChevronPageLast );
AddResource( grCircleClose );
AddResource( grCircleHelp );
AddResource( grCircleOutlineHelp );
AddResource( grDataReuse );
AddResource( grDataReuseEnd );
AddResource( grDragHorizontal );
AddResource( grDelete );
AddResource( grListRemove );
AddResource( grMinus );
AddResource( grPatientsGroup );
AddResource( grPlus );
AddResource( grRefresh );
AddResource( grSave );
AddResource( grSearch );
AddResource( grToggleSwitchOff );
AddResource( grToggleSwitchOn );

finalization

DestroyResource( grApprove );
DestroyResource( grArrowThinRight );
DestroyResource( grCancel );
DestroyResource( grChevronLeft );
DestroyResource( grChevronRight );
DestroyResource( grChevronPageFirst );
DestroyResource( grChevronPageLast );
DestroyResource( grCircleClose );
DestroyResource( grCircleHelp );
DestroyResource( grCircleOutlineHelp );
DestroyResource( grDataReuse );
DestroyResource( grDataReuseEnd );
DestroyResource( grDragHorizontal );
DestroyResource( grDelete );
DestroyResource( grListRemove );
DestroyResource( grMinus );
DestroyResource( grPatientsGroup );
DestroyResource( grPlus );
DestroyResource( grRefresh );
DestroyResource( grSave );
DestroyResource( grSearch );
DestroyResource( grToggleSwitchOff );
DestroyResource( grToggleSwitchOn );

end.
