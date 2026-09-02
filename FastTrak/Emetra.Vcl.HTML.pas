unit Emetra.Vcl.HTML;

interface

uses
  {Standard}
  System.Classes, System.Types, System.UITypes,
  {Vcl}
  Vcl.Graphics, Vcl.Controls, Vcl.StdCtrls, Vcl.ExtCtrls, Vcl.Forms, Vcl.ImgList,
  {Winapi}
  Winapi.Messages, Winapi.Windows,
  {Emetra.Vcl}
  Emetra.Vcl.Types,
  Emetra.Vcl.Classes,
  Emetra.Vcl.Consts,
  Emetra.Vcl.Graphics;

type
  TdcStylesSheet = class;

  IHTML = interface
    ['{DBB3FA83-A00E-4D38-865A-CD87947658CC}']
    procedure DoLinkClick( Href: string );
    function GetStylesSheet: TdcStylesSheet;
    procedure SetStylesSheet( const Value: TdcStylesSheet );
    property StylesSheet: TdcStylesSheet read GetStylesSheet write SetStylesSheet;
  end;

  PCSSStyle = ^TCSSStyle;

  TCSSStyle = record
    BackgroundColor: TColor;
    Color: TColor;
    Display: TCSSDisplay;
    FontFamily: string;
    FontSize: TCSSFontSize;
    FontStyle: TCSSFontStyle;
    FontWeight: TCSSFontWeight;
    PositionX: string;
    PositionY: string;
    Size: TSize;
    TextAlign: TCSSTextAlign;
    TextDecoration: TCSSTextDecoration;
    TextTransform: TCSSTextTransform;
    VerticalAlign: TCSSVerticalAlign;
    function Assign( const Font: TFont ): TCSSStyle;
  end;

  PTagParameter = ^TTagParameter;

  TTagParameter = record
    Name: string;
    Next: PTagParameter;
    Previous: PTagParameter;
    Value: string;
  end;

  THTMLTag = record
    Params: PTagParameter;
    Kind: THTMLTagKind;
  end;

  PNxStackItem = ^TNxStackItem;

  TNxStackItem = record
    Tag: THTMLTag;
    Content: string;
    Next: PNxStackItem;
    Position: TPoint;
    Size: TSize;
    Snapshot: TNxCanvasSnapshot;
    Url: string;
  end;

  { TdcPaintStyle }

  TdcPaintStyle = class( TPersistent )
  private
    FBackgroundColor: TColor;
    FColor: TColor;
    FFontSize: Integer;
    FOnChange: TNotifyEvent;
    FTextDecoration: TCSSTextDecoration;
    FTextTransform: TCSSTextTransform;
    { Property Accessors }
    procedure SetBackgroundColor( const Value: TColor );
    procedure SetColor( const Value: TColor );
    procedure SetFontSize( const Value: Integer );
    procedure SetTextDecoration( const Value: TCSSTextDecoration );
    procedure SetTextTransform( const Value: TCSSTextTransform );
  protected
    procedure DoChange;
  public
    constructor Create; virtual;
    destructor Destroy; override;
    class function TransformText( S: string; Value: TCSSTextTransform ): string;
    procedure Decorate( Font: TFont );
    procedure FillRect( Canvas: TCanvas; const Rect: TRect );
    function HasBackground: Boolean;
  published
    { Properties }
    property BackgroundColor: TColor read FBackgroundColor write SetBackgroundColor;
    property Color: TColor read FColor write SetColor;
    property FontSize: Integer read FFontSize write SetFontSize;
    property TextDecoration: TCSSTextDecoration read FTextDecoration write SetTextDecoration default tdNone;
    property TextTransform: TCSSTextTransform read FTextTransform write SetTextTransform default ttNone;
    { Events }
    property OnChange: TNotifyEvent read FOnChange write FOnChange;
  end;

  { TdcCSSRule }

  TCSSSelectorKind = ( skID, skTag, skClass );

  TdcCSSRule = class( TCollectionItem )
  private
    { Property Fields }
    FBackgroundColor: string;
    FColor: string;
    FFontFamily: string;
    FFontSize: TCSSFontSize;
    FFontStyle: TCSSFontStyle;
    FFontWeight: TCSSFontWeight;
    FName: string;
    FSelector: TCSSSelectorKind;
    FTextAlign: TCSSTextAlign;
    FTextDecoration: TCSSTextDecoration;
    { Property Accessors }
    procedure SetFontFamily( const Value: string );
  public
    constructor Create( Collection: TCollection ); override;
    { Methods }
    procedure AssignToStyle( var Dest: TCSSStyle );
  published
    { Properties }
    property BackgroundColor: string read FBackgroundColor write FBackgroundColor;
    property Color: string read FColor write FColor;
    property FontFamily: string read FFontFamily write SetFontFamily;
    property FontSize: TCSSFontSize read FFontSize write FFontSize;
    property FontStyle: TCSSFontStyle read FFontStyle write FFontStyle default fyInherit;
    property FontWeight: TCSSFontWeight read FFontWeight write FFontWeight default fwInherit;
    property name: string read FName write FName;
    property Selector: TCSSSelectorKind read FSelector write FSelector default skTag;
    property TextAlign: TCSSTextAlign read FTextAlign write FTextAlign default tlInherit;
    property TextDecoration: TCSSTextDecoration read FTextDecoration write FTextDecoration default tdInherit;
  end;

  { TdcCSSRules }

  TdcCSSRules = class( TCollection )
  private
    FOwner: TPersistent;
    function GetItem( Index: Integer ): TdcCSSRule;
    procedure SetItem( Index: Integer; const Value: TdcCSSRule );
  protected
    function GetOwner: TPersistent; override;
  public
    constructor Create( AOwner: TPersistent );
    { Methods }
    function Add: TdcCSSRule;
    property Items[index: Integer]: TdcCSSRule read GetItem write SetItem; default;
  end;

  { TdcStylesSheet }

  TdcStylesSheet = class( TComponent )
  private
    FRules: TdcCSSRules;
    { Property Accessors }
    function GetParam( Tag: THTMLTag; Kind: THTMLParameterKind ): string;
    procedure SetRules( const Value: TdcCSSRules );
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
    procedure AssignStyle( Tag: THTMLTag; var Style: TCSSStyle );
    { Properties }
    property Param[Tag: THTMLTag; Kind: THTMLParameterKind]: string read GetParam;
  published
    property Rules: TdcCSSRules read FRules write SetRules;
  end;

  TTagParsingState = ( psContent, psTagOpening, psTagClosing, psParameterReading );

  TParamParsingState = ( psNameReading, psValueReading );

  { THTMLView }

  TLinkHitEvent = procedure( Sender: TObject; Href: string ) of object;

  TProcHTMLOperation = set of ( hoPaint, hoCalc, hoHitTest );

  THTMLView = class
  private
    FBestSize: TSize;
    FCanvas: TCanvas;
    FDefaultStyle: TCSSStyle;
    FHitHref: string;
    FHitLocation: TPoint;
    FHTML: string;
    FImages: TImageList;
    FListBox: TListBox;
    FOnLinkHit: TLinkHitEvent;
    FOperation: TProcHTMLOperation;
    FStyleSheet: TdcStylesSheet;
    fLinkColor: TColor;
    { Property Accessors }
    function GetParam( Tag: THTMLTag; Kind: THTMLParameterKind ): string;
    function GetStyle( Tag: THTMLTag ): TCSSStyle;
    procedure SetStyleSheet( const Value: TdcStylesSheet );
    procedure SetHTML( const Value: string );
  protected
    function CanHaveChildren( TagKind: THTMLTagKind ): Boolean;
    { Event Handlers }
    procedure DoLinkHit( Href: string ); dynamic;
    { Paint }
    procedure PaintImage( Canvas: TCanvas; ImageRect: TRect; Src: string );
    procedure PaintText( Canvas: TCanvas; TextRect: TRect; Text: string );
  public
    constructor Create;
    destructor Destroy; override;
    procedure PaintTo( Canvas: TCanvas; Dest: TRect );
    procedure Reset;
    { Properties }
    property BestSize: TSize read FBestSize;
    property DefaultStyle: TCSSStyle read FDefaultStyle write FDefaultStyle;
    property HitHref: string read FHitHref;
    property HitLocation: TPoint read FHitLocation write FHitLocation;
    property HTML: string read FHTML write SetHTML;
    property Images: TImageList read FImages write FImages;
    property LinkColor: TColor read fLinkColor write fLinkColor;
    property ListBox: TListBox read FListBox write FListBox;
    property Operation: TProcHTMLOperation read FOperation write FOperation;
    property Param[Tag: THTMLTag; Kind: THTMLParameterKind]: string read GetParam;
    property Style[Tag: THTMLTag]: TCSSStyle read GetStyle;
    property StyleSheet: TdcStylesSheet read FStyleSheet write SetStyleSheet;
    { Events }
    property OnLinkHit: TLinkHitEvent read FOnLinkHit write FOnLinkHit;
  end;

function AlignmentToTextAlign( Value: TAlignment ): TCSSTextAlign;

function FontSizeToInt( Value: TCSSFontSize; Size: Integer ): Integer;
function SizeToPixels( Value: string; LineHeight: Integer ): Integer;
function ParamToStr( Value: THTMLParameterKind ): string;
function TagToStr( Value: THTMLTagKind ): string;

function StrToParam( const Value: string ): THTMLParameterKind;
function StrToTag( const Value: string ): THTMLTagKind;
function StrToTextAlign( const Value: string ): TCSSTextAlign;

implementation

uses
  {System}
  System.SysUtils, System.StrUtils, System.TypInfo, System.Math,
  {Vcl}
  Vcl.Dialogs,
  {Emetra.Vcl}
  Emetra.Vcl.Helpers;

function AlignmentToTextAlign( Value: TAlignment ): TCSSTextAlign;
begin
  case Value of
    taLeftJustify: Result := tlLeft;
    taRightJustify: Result := tlRight;
  else Result := tlCenter;
  end;
end;

function FontSizeToInt( Value: TCSSFontSize; Size: Integer ): Integer;
begin
  if Value = EmptyStr then
    Result := Size
  else
    Result := StrToIntDef( Value, 0 );
end;

function SizeToPixels( Value: string; LineHeight: Integer ): Integer;
var
  RealValue: string;
begin
  { Default }
  Result := 0;
  if Value <> '' then
  begin
    if Pos( '%', Value ) <> -1 then
    begin
      RealValue := LeftStr( Value, Pos( '%', Value ) - 1 );

      Result := Round( LineHeight * ( StrToIntDef( RealValue, 0 ) / 100 ) );
    end;
    if Pos( 'em', Value ) <> -1 then
    begin

    end;
    if Pos( 'px', Value ) <> -1 then
    begin

    end;
  end;
end;

function ParamToStr( Value: THTMLParameterKind ): string;
begin
  case Value of
    tpClass: Result := 'class';
    tpId: Result := 'id';
    tpAlign: Result := 'align';
    tpHref: Result := 'href';
  end;
end;

function TagToStr( Value: THTMLTagKind ): string;
begin
  case Value of
    tgA: Result := 'a';
    tgB: Result := 'b';
    tgBr: Result := 'br';
    tgDiv: Result := 'div';
    tgEm: Result := 'em';
    tgH1: Result := 'h1';
    tgH2: Result := 'h2';
    tgH3: Result := 'h3';
    tgHtml: Result := 'html';
    tgI: Result := 'i';
    tgImg: Result := 'img';
    tgP: Result := 'p';
    tgS: Result := 's';
    tgSpan: Result := 'span';
    tgStrong: Result := 'strong';
    tgSub: Result := 'sub';
    tgSup: Result := 'sup';
    tgU: Result := 'u';
  end;
end;

function StrToParam( const Value: string ): THTMLParameterKind;
var
  S: string;
begin
  S := LowerCase( Value );
  if S = 'class' then
    Result := tpClass
  else if S = 'id' then
    Result := tpId
  else if S = 'align' then
    Result := tpAlign
  else if S = 'href' then
    Result := tpHref
  else
    Result := tpUndefined;
end;

function StrToTag( const Value: string ): THTMLTagKind;
var
  S: string;
begin
  S := LowerCase( Value );
  if S = 'a' then
    Result := tgA
  else if S = 'br' then
    Result := tgBr
  else if S = 'div' then
    Result := tgDiv
  else if S = 'em' then
    Result := tgEm
  else if S = 'h1' then
    Result := tgH1
  else if S = 'h2' then
    Result := tgH2
  else if S = 'h3' then
    Result := tgH3
  else if S = 'img' then
    Result := tgImg
  else if S = 'p' then
    Result := tgP
  else if S = 's' then
    Result := tgS
  else if S = 'span' then
    Result := tgSpan
  else if S = 'strong' then
    Result := tgStrong
  else if S = 'sub' then
    Result := tgSub
  else if S = 'sup' then
    Result := tgSup
  else if S = 'b' then
    Result := tgB
  else if S = 'i' then
    Result := tgI
  else if S = 'u' then
    Result := tgU
  else
    Result := tgUndefined;
end;

function StrToTextAlign( const Value: string ): TCSSTextAlign;
var
  S: string;
begin
  S := LowerCase( Value );
  if S = 'left' then
    Result := tlLeft
  else if S = 'center' then
    Result := tlCenter
  else if S = 'right' then
    Result := tlRight
  else
    Result := tlInherit;
end;

procedure AddParam( Tag: THTMLTag; Param: PTagParameter );
begin
  if Tag.Params <> nil then
  begin
    Param^.Previous := Tag.Params;
  end;

  Tag.Params := Param;
end;

function CreateParam( Param: THTMLParameterKind ): PTagParameter;
begin
  New( Result );

  Result^.Name := EmptyStr;
  Result^.Value := EmptyStr;
  Result^.Next := nil;
  Result^.Previous := nil;
end;

procedure DisposeParams( Param: PTagParameter );
var
  T: PTagParameter;
begin
  while Param <> nil do
  begin
    T := Param;

    Param := Param.Previous;

    Dispose( T );
  end;
end;

{ TCSSStyle }

function TCSSStyle.Assign( const Font: TFont ): TCSSStyle;
begin
  Result.FontFamily := Font.Name;
  Result.FontSize := IntToStr( Font.Size );
end;

{ TdcPaintStyle }

constructor TdcPaintStyle.Create;
begin
  FBackgroundColor := clNone;
  FColor := clNone;
  FFontSize := 8;
  FTextDecoration := tdNone;
  FTextTransform := ttNone;
end;

procedure TdcPaintStyle.Decorate( Font: TFont );
begin
  Font.Style := [];

  if FTextDecoration = tdUnderline then
    Font.Style := Font.Style + [fsUnderline];
  if FTextDecoration = tdLineTrough then
    Font.Style := Font.Style + [fsStrikeOut];

  if FFontSize <> 0 then
    Font.Size := FFontSize;
  if FColor <> clNone then
    Font.Color := FColor;
end;

destructor TdcPaintStyle.Destroy;
begin
  inherited;
end;

procedure TdcPaintStyle.DoChange;
begin
  if Assigned( FOnChange ) then
    FOnChange( Self );
end;

procedure TdcPaintStyle.FillRect( Canvas: TCanvas; const Rect: TRect );
begin
  if BackgroundColor <> clNone then
  begin
    Canvas.Brush.Color := BackgroundColor;
    Canvas.FillRect( Rect );
  end;
end;

function TdcPaintStyle.HasBackground: Boolean;
begin
  Result := BackgroundColor <> clNone;
end;

procedure TdcPaintStyle.SetBackgroundColor( const Value: TColor );
begin
  if Value <> FBackgroundColor then
  begin
    FBackgroundColor := Value;
    DoChange;
  end;
end;

procedure TdcPaintStyle.SetColor( const Value: TColor );
begin
  if Value <> FColor then
  begin
    FColor := Value;
    DoChange;
  end;
end;

procedure TdcPaintStyle.SetFontSize( const Value: Integer );
begin
  if Value <> FFontSize then
  begin
    FFontSize := Value;
    DoChange;
  end;
end;

procedure TdcPaintStyle.SetTextDecoration( const Value: TCSSTextDecoration );
begin
  if Value <> FTextDecoration then
  begin
    FTextDecoration := Value;
    DoChange;
  end;
end;

procedure TdcPaintStyle.SetTextTransform( const Value: TCSSTextTransform );
begin
  if Value <> FTextTransform then
  begin
    FTextTransform := Value;
    DoChange;
  end;
end;

class function TdcPaintStyle.TransformText( S: string; Value: TCSSTextTransform ): string;
begin
  case Value of
    ttNone: Result := S;
    ttUpperCase: Result := UpperCase( S );
    ttLowerCase: Result := LowerCase( S );
    ttCapitalize:
      if Length( S ) > 0 then
        Result := UpperCase( S[1] );
  end;
end;

{ TdcCSSRule }

procedure TdcCSSRule.AssignToStyle( var Dest: TCSSStyle );
begin
  Dest.BackgroundColor := StrToColor( BackgroundColor );
  Dest.Color := StrToColor( Color );
  Dest.FontFamily := FontFamily;
  Dest.FontSize := FontSize;
  Dest.FontStyle := FontStyle;
  Dest.FontWeight := FontWeight;
  Dest.TextAlign := TextAlign;
  Dest.TextDecoration := TextDecoration;
end;

constructor TdcCSSRule.Create( Collection: TCollection );
begin
  inherited;
  FBackgroundColor := EmptyStr;
  FColor := '';
  FFontFamily := '';
  FFontSize := '';
  FFontStyle := fyInherit;
  FFontWeight := fwInherit;
  FSelector := skTag;
  FTextAlign := tlInherit;
  FTextDecoration := tdInherit;
end;

procedure TdcCSSRule.SetFontFamily( const Value: string );
begin
  if Value <> FFontFamily then
  begin
    FFontFamily := Value;
    { TODO: Notify }
  end;
end;

{ TdcCSSRules }

function TdcCSSRules.Add: TdcCSSRule;
begin
  Result := TdcCSSRule( inherited Add );
end;

constructor TdcCSSRules.Create( AOwner: TPersistent );
begin
  inherited Create( TdcCSSRule );
  FOwner := AOwner;
end;

function TdcCSSRules.GetItem( Index: Integer ): TdcCSSRule;
begin
  Result := TdcCSSRule( inherited GetItem( index ) );
end;

function TdcCSSRules.GetOwner: TPersistent;
begin
  Result := FOwner;
end;

procedure TdcCSSRules.SetItem( Index: Integer; const Value: TdcCSSRule );
begin
  inherited SetItem( index, Value );
end;

{ TdcStylesSheet }

procedure TdcStylesSheet.AssignStyle( Tag: THTMLTag; var Style: TCSSStyle );
var
  i: Integer;
begin
  for i := 0 to Pred( Rules.Count ) do
  begin

    if Rules[i].Name <> '' then

      case Rules[i].Selector of

        skTag:
          if StrToTag( Rules[i].Name ) = Tag.Kind then
          begin
            Rules[i].AssignToStyle( Style );
          end;

        skClass:
          if Rules[i].Name = Param[Tag, tpClass] then
          begin
            Rules[i].AssignToStyle( Style );
          end;

      end;

  end;

end;

constructor TdcStylesSheet.Create( AOwner: TComponent );
begin
  inherited;
  FRules := TdcCSSRules.Create( Self );
end;

destructor TdcStylesSheet.Destroy;
begin
  FRules.Free;
  inherited;
end;

function TdcStylesSheet.GetParam( Tag: THTMLTag; Kind: THTMLParameterKind ): string;
var
  P: PTagParameter;
begin
  P := Tag.Params;
  while P <> nil do
  begin
    if StrToParam( P^.Name ) = Kind then
    begin
      Result := P^.Value;
      Exit;
    end;
    P := P.Next;
  end;
end;

procedure TdcStylesSheet.SetRules( const Value: TdcCSSRules );
begin
  FRules.Assign( Value );
end;

{ THTMLView }

function THTMLView.CanHaveChildren( TagKind: THTMLTagKind ): Boolean;
begin
  case TagKind of
    tgBr, tgImg: Result := False;
  else Result := True;
  end;
end;

constructor THTMLView.Create;
begin
  FDefaultStyle.Color := clWindowText;
  FDefaultStyle.FontSize := '8';
  FDefaultStyle.TextAlign := tlLeft;
  FImages := nil;
  fLinkColor := clBlue;
  FOperation := [];
end;

destructor THTMLView.Destroy;
begin
  inherited;
end;

procedure THTMLView.DoLinkHit( Href: string );
begin
  if Assigned( FOnLinkHit ) then
    FOnLinkHit( Self, Href );
end;

function THTMLView.GetParam( Tag: THTMLTag; Kind: THTMLParameterKind ): string;
var
  P: PTagParameter;
begin
  P := Tag.Params;

  while P <> nil do
  begin
    if StrToParam( P^.Name ) = Kind then
    begin
      Result := P^.Value;
      Exit;
    end;

    P := P.Next;
  end;

end;

function THTMLView.GetStyle( Tag: THTMLTag ): TCSSStyle;
begin

  with Result do
  begin
    BackgroundColor := clNone;
    PositionX := '';
    PositionY := '';
    TextAlign := tlLeft;

    case Tag.Kind of
      tgHtml: FontSize := FDefaultStyle.FontSize;
      tgH1: FontSize := '16';
      tgH2: FontSize := '14';
      tgH3: FontSize := '12';
    else FontSize := '';
    end;

    { color }
    case Tag.Kind of
      tgA: Color := fLinkColor;
      tgHtml: Color := FDefaultStyle.Color;
    else Color := clNone;
    end;

    { display }
    case Tag.Kind of
      tgA, tgB, tgEm, tgSpan, tgHtml, tgI, tgS, tgStrong, tgSub, tgSup, tgU: Display := dpInline;
      tgDiv, tgH1, tgH2, tgH3, tgP: Display := dpBlock;
      tgImg: Display := dpInlineBlock;
    end;

    { text-align }
    case Tag.Kind of
      tgHtml: TextAlign := FDefaultStyle.TextAlign;
      tgP: TextAlign := StrToTextAlign( Param[Tag, tpAlign] );
    else TextAlign := tlInherit;
    end;

    { text-decoration }
    case Tag.Kind of
      tgA: TextDecoration := tdUnderline;
      tgHtml: TextDecoration := tdNone;
      tgS: TextDecoration := tdLineTrough;
      tgU: TextDecoration := tdUnderline;
    else TextDecoration := tdInherit;
    end;

    FontFamily := '';

    { font-weight }
    case Tag.Kind of
      tgB, tgStrong, tgH1, tgH2, tgH3: FontWeight := fwBold;
      tgHtml: FontWeight := fwNormal;
    else FontWeight := fwInherit;
    end;

    { font-style }
    case Tag.Kind of
      tgEm, tgI: FontStyle := fyItalic;
    else FontStyle := fyInherit;
    end;

    { vertical-align }
    case Tag.Kind of
      tgSub: VerticalAlign := vaSub;
      tgSup: VerticalAlign := vaSuper;
    else VerticalAlign := vaBaseline;
    end;

    { position }
    case Tag.Kind of
      tgSub: PositionY := '20%';
      tgSup: PositionY := '-35%';
    end;

  end;

  { Alter styles from Sheet }
  if Assigned( FStyleSheet ) then
    FStyleSheet.AssignStyle( Tag, Result );
end;

procedure THTMLView.PaintImage( Canvas: TCanvas; ImageRect: TRect; Src: string );
var
  Index: Integer;
begin
  if LeftStr( Src, 7 ) = 'list://' then
  begin
    index := StrToIntDef( Copy( Src, 8, Length( Src ) - 8 ), -1 );
    if Assigned( Images ) and InRange( index, 0, Pred( Images.Count ) ) then
    begin
      Images.Draw( Canvas, ImageRect.Left, ImageRect.Top, index );
    end;
  end;
end;

procedure THTMLView.PaintText( Canvas: TCanvas; TextRect: TRect; Text: string );
var
  Flags: Integer;
begin
  Flags := DT_NOPREFIX or DT_EXTERNALLEADING or DT_SINGLELINE or DT_LEFT or DT_BOTTOM or DT_NOCLIP;

  { Draw Text }
  with Canvas.Brush do
  begin
    Style := bsClear;
    DrawTextW( Canvas.Handle, PWideChar( Text ), Length( Text ), TextRect, Flags );
    Style := bsSolid;
  end;
end;

{$WARNINGS Off}

procedure THTMLView.PaintTo( Canvas: TCanvas; Dest: TRect );
var
  First, Last: PNxStackItem;
  LineSize, BufferSize: TSize;
  TextAlign, PrevAlign: TCSSTextAlign;
  LineColor: TColor;

  i: Integer;
  LastDisplay: TCSSDisplay;

  BreakItem: PNxStackItem;
  Pos: TPoint;

  procedure DisposeParams( Param: PTagParameter );
  var
    T: PTagParameter;
  begin
    while Param <> nil do
    begin
      T := Param;
      Param := Param.Next;

      Dispose( T );
    end;
  end;

  procedure AssignStyle( Style: TCSSStyle );
  begin

    with Canvas do
    begin

      { background-color }
      Brush.Color := Style.BackgroundColor;

      { color }
      if Style.Color <> clNone then
        Font.Color := Style.Color;

      { font-family }
      Font.Name := Style.FontFamily;

      { font-size }
      Font.Size := FontSizeToInt( Style.FontSize, Font.Size );

      { font-weight }
      case Style.FontWeight of
        fwInherit:;
        fwNormal: Font.Style := Font.Style - [fsBold];
        fwBold: Font.Style := Font.Style + [fsBold];
      end;

      { font-style }
      case Style.FontStyle of
        fyInherit:;
        fyNormal: Font.Style := Font.Style - [fsItalic];
        fyItalic: Font.Style := Font.Style + [fsItalic];
      end;

      { text-decoration }
      case Style.TextDecoration of
        tdInherit:;
        tdNone: Font.Style := Font.Style - [fsUnderline];
        tdUnderline: Font.Style := Font.Style + [fsUnderline];
        tdLineTrough: Font.Style := Font.Style + [fsStrikeOut];
      end;

      case Style.VerticalAlign of
        vaSuper, vaSub: Font.Size := Round( FontSizeToInt( Style.FontSize, Font.Size ) * 0.75 );
      end;

    end;

  end;

  procedure PaintStack( Location: TPoint; StackItem: PNxStackItem );
  var
    TextRect, BlockRect: TRect;
    BackgroundColor: TColor;
    OffsetY: Integer;
  begin
    with StackItem^ do

      case Tag.Kind of
        tgImg:
          begin
            BlockRect := Bounds( Location.X, Location.Y, Size.cx, LineSize.cy );
            if hoPaint in Operation then
            begin
              Canvas.Brush.Color := clRed;
              Canvas.FillRect( BlockRect );
            end;
          end;

      else
        begin
          TextRect := Bounds( Location.X, Location.Y, Size.cx, LineSize.cy );

          OffsetY := StackItem^.Position.Y;

          OffsetRect( TextRect, 0, OffsetY );

          if Tag.Kind = tgA then
            if hoHitTest in Operation then
            begin

              if PtInRect( TextRect, HitLocation ) then
              begin
                FHitHref := Url;
              end;

            end;

          BackgroundColor := StackItem^.Snapshot.Brush.Color;

          if BackgroundColor <> clNone then
          begin
            Canvas.Brush.Color := BackgroundColor;
            if hoPaint in Operation then
            begin
              Canvas.FillRect( TextRect );
            end;
          end;

          try
            { Set Snapshot back to Canvas }
            PutCanvasSnapshot( Canvas, Snapshot );

            if hoPaint in Operation then
            begin
              PaintText( Canvas, TextRect, Content );
            end;
          finally

          end;

        end;

      end;
  end;

  procedure BreakLine;
  begin
    Pos.X := Dest.Left;
    Pos.Y := Pos.Y + LineSize.cy;

    LineSize := TSize.Create( 0, 0 );

    BreakItem := nil;
  end;

  procedure OutputLine;
  var
    T: PNxStackItem;
    Done: Boolean;
    LinePos: TPoint;
    Snapshot: TNxCanvasSnapshot;
  begin
    if BufferSize.cy > LineSize.cy then
      LineSize.cy := BufferSize.cy;

    try
      Snapshot := GetCanvasSnapshot( Canvas );

      LinePos := Pos;

      FBestSize.cx := Max( FBestSize.cx, LineSize.cx );
      FBestSize.cy := Max( FBestSize.cy, LinePos.Y + LineSize.cy - Dest.Top );

      case TextAlign of
        tlInherit:;
        tlLeft: LinePos := Pos;
        tlCenter: LinePos.X := Dest.Left + ( ( Dest.Right - Dest.Left ) div 2 - LineSize.cx div 2 );
        tlRight: LinePos.X := Dest.Right - LineSize.cx;
      end;

      if LineColor <> clNone then
      begin
        Canvas.Brush.Color := LineColor;
        // Canvas.FillRect(Rect(LinePos.X, LinePos.Y, Dest.Right, LinePos.Y + LineSize.cy));
      end;

      Done := False;

      if Assigned( First ) and ( First^.Content = ' ' ) then
      begin
        T := First;
        First := First.Next;
        Dispose( T );
      end;

      { Output line & break
        to next line }
      while First <> nil do
      begin

        T := First;

        PaintStack( LinePos, First );

        Inc( LinePos.X, First^.Size.cx );

        if ( First = BreakItem ) then
        begin
          Done := True;
          BreakItem := nil;
        end;

        First := First.Next;

        Dispose( T );

        if Done then
        begin
          PutCanvasSnapshot( Canvas, Snapshot );
          Exit;
        end;

      end;

    finally
      PutCanvasSnapshot( Canvas, Snapshot );
    end;

    Last := nil;
  end;

  function AddToStack( Content: string; Tag: THTMLTag; Style: TCSSStyle; Snapshoot: TNxCanvasSnapshot; BreakAccept: Boolean ): PNxStackItem;
  var
    Size: TSize;
  begin
    if Content = '' then
    begin
      Exit;
    end;

    Size := Canvas.GetTextSize( Content );

    New( Result );

    Result^.Tag := Tag;
    Result^.Next := nil;
    Result^.Content := Content;
    Result^.Snapshot := Snapshoot;
    Result^.Size := Size;
    Result^.Position := Point( 0, 0 );

    if Style.PositionY <> '' then
    begin
      Result^.Position.Y := SizeToPixels( Style.PositionY, LineSize.cy );
    end;

    case Tag.Kind of
      tgA: Result^.Url := Param[Tag, tpHref];
    end;

    if First = nil then
    begin
      First := Result;
    end;

    if Last <> nil then
      Last^.Next := Result;

    Last := Result;

    { Required for horz Alignment }
    Inc( BufferSize.cx, Size.cx );

    { Can content fit? }
    if not( Content = ' ' ) and ( LineSize.cx + BufferSize.cx > Dest.Right - Dest.Left ) then
    begin

      { Text overlow }
      if BufferSize.cx > Dest.Right - Dest.Left then
      begin
        if LineSize.cy = 0 then
          LineSize.cy := Size.cy;

        BreakLine;

        Size.cy := 0;
      end;

      { Flush stack }
      OutputLine;

      { Go-to next line }
      BreakLine;

      LineSize := BufferSize;

      BufferSize := TSize.Create( 0, 0 );
    end;

    if Size.cy > BufferSize.cy then
      BufferSize.cy := Size.cy;

    { Breaking is possible }
    if BreakAccept then
    begin
      Inc( LineSize.cx, BufferSize.cx );

      if BufferSize.cy > LineSize.cy then
        LineSize.cy := BufferSize.cy;

      BufferSize := TSize.Create( 0, 0 );
    end;
  end;

  procedure NewLine;
  begin
    { Don't break any other word }
    BreakItem := nil;

    if BufferSize.cy > LineSize.cy then
      LineSize.cy := BufferSize.cy;

    Inc( LineSize.cx, BufferSize.cx );

    OutputLine;

    BreakLine;
    BufferSize := TSize.Create( 0, 0 );
  end;

  function ParseParams: PTagParameter;
  var
    Buffer: string;
    State: TParamParsingState;
    ParamInBuffer: THTMLParameterKind;
    T: PTagParameter;
  begin
    { Default }
    Result := nil;

    State := psNameReading;

    Buffer := EmptyStr;

    while i <= Length( HTML ) do
    begin

      if ( HTML[i] = '=' ) and ( State <> psValueReading ) then
      begin
        ParamInBuffer := StrToParam( Buffer );

        T := Result;

        Result := CreateParam( ParamInBuffer );

        Result.Previous := T;

        Result^.Name := Buffer;
        Buffer := EmptyStr;
      end

      else if HTML[i] = '"' then
      begin
        if State = psValueReading then
        begin
          Result^.Value := Buffer;
          Buffer := EmptyStr;
        end
        else
          State := psValueReading;
      end

      else if HTML[i] = ' ' then
      begin
        State := psNameReading;
        Buffer := EmptyStr;
      end

      else if HTML[i] = '>' then
      begin
        Exit;
      end

      else
        Buffer := Buffer + HTML[i];

      { Go-to next }
      Inc( i );
    end;

  end;

  function ParseTag( Kind: THTMLTagKind; Params: PTagParameter ): TCSSStyle;
  var
    Buffer: string;

    Tag: THTMLTag;

    StyleInBuffer: TCSSStyle;
    TagKind: THTMLTagKind;
    TagParams: PTagParameter;

    Snapshoot: TNxCanvasSnapshot;

    State: TTagParsingState;

    CanBreak: Boolean;
  begin

    { Tag start }

    try
      Tag.Kind := Kind;
      Tag.Params := Params;

      { Obtain style }
      Result := Self.Style[Tag];

      { Block? }

      case Result.Display of
        dpBlock: NewLine;
        dpInline:
          if LastDisplay = dpBlock then
          begin
            // ISSUE

            // NewLine;
          end;
      end;

      CanBreak := False;

      if Result.Display = dpBlock then
      begin
        // if Style.BackgroundColor <> clNone then
        LineColor := Result.BackgroundColor;

        PrevAlign := TextAlign;
        TextAlign := Result.TextAlign;
      end;

      { Apply style to Canvas }
      AssignStyle( Result );

      Snapshoot := GetCanvasSnapshot( Canvas );

      { Start }
      State := psContent;
      Buffer := EmptyStr;

      while i <= Length( HTML ) do
      begin

        { Closing }
        if ( HTML[i] = '/' ) and not ( State in [psParameterReading, psContent] ) then
        begin
          if State = psTagOpening then
          begin
            State := psTagClosing;
            Buffer := EmptyStr;
          end;
        end

        else if HTML[i] = '<' then
        begin
          if State = psContent then
            AddToStack( Buffer, Tag, Result, Snapshoot, Result.Display = dpBlock );

          { Start reading tag, or params }
          State := psTagOpening;

          Buffer := EmptyStr;
        end

        else if ( HTML[i] = '>' ) or ( ( HTML[i] = ' ' ) and ( State = psTagOpening ) ) then
        begin
          case State of
            psTagOpening:
              begin
                TagKind := StrToTag( Buffer );

                Buffer := EmptyStr;

                TagParams := nil;

                { Start reading params? }
                if HTML[i] = ' ' then
                  TagParams := ParseParams;

                if CanHaveChildren( TagKind ) then
                begin
                  try
                    { Parse child tag }
                    StyleInBuffer := ParseTag( TagKind, TagParams );
                  finally

                    if StyleInBuffer.Display = dpBlock then
                    begin
                      CanBreak := True;
                    end;

                    { Set style back }
                    PutCanvasSnapshot( Canvas, Snapshoot );

                    if Result.Display = dpBlock then
                    begin
                      // if Style.BackgroundColor <> clNone then
                      LineColor := Result.BackgroundColor;

                      // TextAlign := Result.TextAlign;
                    end;

                  end;
                end
                else
                begin
                  case TagKind of
                    tgBr: NewLine;
                    tgImg:
                      begin

                      end;
                  end;
                end;

                { As child tag is parsed,
                  we need set back style! }

                State := psContent;
              end;
            psTagClosing:
              begin
                // State := psContent;
                Buffer := EmptyStr;

                Break;
              end;
          end;
        end

        else if HTML[i] = ' ' then
        begin
          AddToStack( Buffer, Tag, Result, Snapshoot, False );

          BreakItem := Last;

          AddToStack( HTML[i], Tag, Result, Snapshoot, True );

          Buffer := EmptyStr;
        end

        else
        begin

          // if (State = psContent)
          // and not CharInSet(Char(HTML[i]), [#13, #10]) then
          // if CanBreak then
          // begin
          // NewLine;
          // CanBreak := False;
          // LastDisplay := dpInline;
          // end;

          if not CharInSet( Char( HTML[i] ), [#13, #10] ) then
          begin
            if ( State = psContent ) and CanBreak then
            begin
              NewLine;
              CanBreak := False;
              LastDisplay := dpInline;
            end;

            Buffer := Buffer + HTML[i];
          end;

        end;

        { Go-to next }
        Inc( i );
      end;

    finally
      { Tag end }
      AddToStack( Buffer, Tag, Result, Snapshoot, False );

      { Required! }
      if Result.Display = dpBlock then
      begin
        BreakItem := nil;

        NewLine;

        TextAlign := PrevAlign;
      end;

      LastDisplay := Result.Display;

      { Release params }
      DisposeParams( Tag.Params );

    end;

  end;

begin
  FCanvas := Canvas;
  FHitHref := '';

  { Clear junk value }
  LineSize := TSize.Create( 0, 0 );
  BufferSize := TSize.Create( 0, 0 );
  FBestSize := TSize.Create( 0, 0 );

  { Unset pointers }
  First := nil;
  Last := nil;
  BreakItem := nil;

  { Start }
  Pos := Dest.TopLeft;
  i := 1;

  LastDisplay := dpInline;

  LineColor := clNone;
  TextAlign := FDefaultStyle.TextAlign;

  { Parse & paint }
  if HTML <> EmptyStr then
  begin
    ParseTag( tgHtml, nil );

    BreakItem := nil;

    Inc( LineSize.cx, BufferSize.cx );

    if BufferSize.cy > LineSize.cy then
      LineSize.cy := BufferSize.cy;

    OutputLine;
  end;
end;

{$WARNINGS On}

procedure THTMLView.Reset;
begin
  FOperation := [];
  FBestSize.cx := 0;
  FBestSize.cy := 0;
  FHTML := '';
  FHitHref := '';
  FHitLocation := TPoint.Zero;
end;

procedure THTMLView.SetHTML( const Value: string );
begin
  FHTML := Trim( Value );
end;

procedure THTMLView.SetStyleSheet( const Value: TdcStylesSheet );
begin
  FStyleSheet := Value;
end;

end.
