unit Emetra.Vcl.ToolTip;

interface

uses
  System.Types, System.Classes, System.Math,
  Vcl.Controls, Vcl.Graphics,
  Winapi.Windows, Winapi.Messages,
  Emetra.Vcl.Consts, Emetra.Vcl.HTML;

const
  Indentation              = 6;
  clTipContainerForeground = $003F340D;

type
  { TdcToolTip }

  TdcToolTip = class( THintWindow )
  private
    fGlyph: TGraphic;
    fHeader: string;
    fHeaderFontColor: TColor;
    fHeaderHeight: integer;
    fHTMLView: THTMLView;
    fTarget: TWinControl;
  protected
    procedure CreateParams( var Params: TCreateParams ); override;
    function GetDefaultStyle: TCSSStyle;
    function HasGlyph: boolean;
    procedure Paint; override;
    procedure WndProc( var Message: TMessage ); override;
  public
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
    { Public Methods }
    function GetBestFitHeight( const AMaxWidth: integer ): integer;
    function GetBestFitWidth: integer;
  published
    { Properties }
    property Glyph: TGraphic read fGlyph write fGlyph;
    property Header: string read fHeader write fHeader;
    property HeaderFontColor: TColor read fHeaderFontColor write fHeaderFontColor default clTipContainerForeground;
    property HeaderHeight: integer read fHeaderHeight write fHeaderHeight default 20;
    property Target: TWinControl read fTarget write fTarget;
  end;

implementation

uses
  Emetra.Vcl.Helpers;

{ TdcToolTip }

constructor TdcToolTip.Create( AOwner: TComponent );
begin
  inherited;
  fGlyph := nil;
  fHeader := '';
  fHeaderFontColor := clTipContainerForeground;
  fHeaderHeight := 20;
  fHTMLView := THTMLView.Create;
  fTarget := nil;
  Color := clWindow;
end;

procedure TdcToolTip.CreateParams( var Params: TCreateParams );
begin
  inherited;
  with Params do
  begin
    WindowClass.Style := WindowClass.Style and not CS_DROPSHADOW;
    Style := Style and not WS_BORDER;
  end;
end;

destructor TdcToolTip.Destroy;
begin
  fHTMLView.Free;
  inherited;
end;

function TdcToolTip.GetBestFitHeight( const AMaxWidth: integer ): integer;
var
  ContentRect: TRect;
  PaddingTop: integer;
begin
  ContentRect := Bounds( 0, 0, AMaxWidth, 0 );
  ContentRect.Inflate( -Indentation, -Indentation );
  PaddingTop := 0;
  with Canvas do
  begin
    Font.Assign( Self.Font );
    { Build ContentRect }
    if HasGlyph then
      Inc( ContentRect.Left, 48 );
    if Header <> '' then
      PaddingTop := fHeaderHeight + Indentation;
    Inc( ContentRect.Top, PaddingTop );
    Inc( ContentRect.Left, Indentation );

    fHTMLView.HTML := Caption;
    fHTMLView.Operation := [hoCalc];
    fHTMLView.DefaultStyle := GetDefaultStyle;
    fHTMLView.PaintTo( Canvas, ContentRect );

    { Final Result }
    Result := fHTMLView.BestSize.cy + PaddingTop + Indentation * 2;
  end;
end;

function TdcToolTip.GetBestFitWidth: integer;
var
  ContentRect: TRect;
  PaddingLeft: integer;
  HeaderSize: TSize;
begin
  with Canvas do
  begin
    PaddingLeft := Indentation * 2;
    if HasGlyph then
      Inc( PaddingLeft, 48 );
    Inc( PaddingLeft, Indentation );

    { Header }
    Font.Assign( Self.Font );
    Font.Style := [fsBold];
    HeaderSize := Canvas.GetTextSize( Header );

    { Caption }
    ContentRect := Rect( 0, 0, MaxInt, 0 );
    fHTMLView.DefaultStyle := GetDefaultStyle;
    fHTMLView.HTML := Caption;
    fHTMLView.Operation := [hoCalc];
    fHTMLView.PaintTo( Canvas, ContentRect );
    Result := Max( fHTMLView.BestSize.cx, HeaderSize.cx ) + PaddingLeft;
  end;
end;

function TdcToolTip.GetDefaultStyle: TCSSStyle;
begin
  Result.Assign( Font );
end;

function TdcToolTip.HasGlyph: boolean;
begin
  Result := Assigned( fGlyph ) and not fGlyph.Empty;
end;

procedure TdcToolTip.Paint;
var
  DescriptionTop: integer;
  ContentRect, HeaderRect, DescriptionRect: TRect;
  HeaderText: string;
begin
  with Canvas do
  begin
    Brush.Color := $00ACACAC;
    FrameRect( ClientRect );
    Font.Assign( Self.Font );

    ContentRect := ClientRect;
    ContentRect.Inflate( -Indentation, -Indentation );

    if HasGlyph then
    begin
      Draw( ContentRect.Left + SpacingDefault, ContentRect.Top + SpacingDouble, fGlyph );
      ContentRect.Left := 48;
    end;

    DescriptionTop := ContentRect.Top;

    { Header }
    if Header <> '' then
    begin
      HeaderRect := ContentRect;
      HeaderRect.Height := fHeaderHeight;
      Inc( HeaderRect.Left, Indentation );
      HeaderText := Header;

      Font.Style := [fsBold];
      Font.Color := fHeaderFontColor;
      Brush.Style := bsClear;
      TextRect( HeaderRect, HeaderText, [tfLeft, tfSingleLine, tfExternalLeading, tfWordEllipsis, tfNoClip, tfVerticalCenter] );
      Brush.Style := bsSolid;
      Inc( DescriptionTop, HeaderRect.Height );

      { Header to Text Separator }
      Pen.Color := $00A9A9A9;
      Polyline( [Point( ContentRect.Left, HeaderRect.Bottom ), Point( ContentRect.Right, HeaderRect.Bottom )] );

      Inc( DescriptionTop, Indentation );
    end;

    DescriptionRect := Rect( ContentRect.Left, DescriptionTop, ContentRect.Right, ContentRect.Bottom );
    Inc( DescriptionRect.Left, Indentation );

    fHTMLView.DefaultStyle := GetDefaultStyle;
    fHTMLView.HTML := Caption;
    fHTMLView.Operation := [hoPaint];
    fHTMLView.PaintTo( Canvas, DescriptionRect );
  end;
end;

procedure TdcToolTip.WndProc( var Message: TMessage );
begin
  inherited;
  case Message.Msg of
    WM_ACTIVATEAPP:
      if TWMActivateApp( Message ).Active = false then
        ReleaseHandle;
  end;
end;

end.
