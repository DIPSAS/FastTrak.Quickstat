{$HINTS OFF}
unit Emetra.VclUtil.Style;

interface

uses
  Emetra.VclUtil.ColorSet,
  Emetra.VclUtil.ColorSet.Interfaces,
  Emetra.VclUtil.ColorCalculator,
  Emetra.VclUtil.Style.Interfaces,
  {Emetra.Vcl}
  Emetra.Vcl.Consts,
  Emetra.Vcl.StdCtrls,
  Emetra.Vcl.ExtCtrls,
  {Standard}
  Windows, Controls, Classes, ExtCtrls, StdCtrls, SysUtils, Graphics, Tabs,
  Forms, TypInfo, Contnrs, ComCtrls, ToolWin;

type
  TGuiStyle = class( TGuiColorSet, IGuiColorSet, IGuiListBoxColorSet, IGuiStyle )
  strict private
    FClientList: TObjectList;
    FToolbarHeight: integer;
    FFontName: string;
    FFontSize: integer;
    FMinFontSize: integer;
    fFlat: boolean;
  private
    procedure StyleHeaderControl( AControl: TControl );
    { Property Accessors }
    function Get_Flat: boolean;
    function Get_FontSize: integer;
    function Get_FontName: string;
    procedure Set_BaseColor( const ABaseColor: TColor );
    procedure Set_FontName( const AFontName: string );
    procedure Set_FontSize( const AFontSize: integer );
    procedure Set_Flat( const AValue: boolean );
  protected
    procedure DoCustomDrawToolbar( Sender: TToolBar; const ARect: TRect; var DefaultDraw: boolean );
    procedure DoCustomDrawButton( Sender: TToolBar; Button: TToolButton; State: TCustomDrawState; var DefaultDraw: boolean );
    procedure NotifyClients;
    procedure SetColor( AControl: TControl; const APropName: string = 'Color'; AColor: TColor = clNone );
  public
    { Initialization }
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { Registration and deregistration of stylable elements }
    procedure RegisterClient( AClient: IGuiStyleObserver );
    procedure UnregisterClient( AClient: IGuiStyleObserver );
    procedure UnregisterAll;
    { Other members }
    function ToolbarHeight: integer;
    procedure StyleAccentPanel( APanel: TPanel );
    procedure StyleDialog( AForm: TForm );
    procedure StyleDialogControl( AControl: TControl; ADialogControl: TDialogControl );
    procedure StyleDialogFooter( APanel: TPanel );
    procedure StyleDialogHeader( APanel: TPanel );
    procedure StyleFrame( AFrame: TFrame );
    procedure StyleForm( AForm: TForm );
    procedure StyleH1Label( ALabel: TLabel );
    procedure StyleHeaderLabel( ALabel: TLabel );
    procedure StyleHeaderPanel( APanel: TPanel ); overload;
    procedure StyleHeaderPanel( APanel: TdcHeaderPanel ); overload;
    procedure StyleButton( AButton: TCustomButton );
    procedure StyleButtonPanel( APanel: TCustomPanel );
    procedure StyleCheckPanel( APanel: TCustomPanel );
    procedure StylePanel( APanel: TCustomPanel );
    procedure StyleLabel( ALabel: TLabel );
    procedure StyleBoldLabel( ALabel: TLabel );
    procedure StyleSmallLabel( ALabel: TLabel );
    procedure StyleInfoLabel( ALabel: TLabel ); overload;
    procedure StyleInfoLabel( ALabel: TdcLabel ); overload;
    procedure StyleTopPanel( APanel: TPanel );
    procedure StyleTopLabel( ALabel: TLabel );
    procedure StyleToolBar( AToolbar: TToolBar; AStyle: TGuiControlStyle = gsDefault );
    procedure StyleSimpleCheckbox( ACheck: TCheckBox );
    procedure StyleSmallHeaderLabel( ALabel: TLabel );
    procedure StyleTabset( ATabSet: TTabSet );
  published
    property Flat: boolean read Get_Flat write Set_Flat;
    property FontName: string read Get_FontName write Set_FontName;
    property FontSize: integer read Get_FontSize write Set_FontSize;
  end;

  { Special colors }

implementation

uses
  System.Math, Vcl.Buttons, Emetra.Vcl.Buttons;

type
  TExposedPanel = class( TCustomPanel )
  public
    property AutoSize;
    property Color;
    property Font;
  end;

{$REGION 'TGuiStyle'}

procedure TGuiStyle.AfterConstruction;
const
  FONT_SEGOE_UI = 'Segoe UI';
  FONT_ARIAL    = 'Arial';
begin
  inherited;
  fFlat := false;
  FClientList := TObjectList.Create( false );
  { Set font based on screen size and availability }
  if Screen.Fonts.IndexOf( FONT_SEGOE_UI ) <> -1 then
    FFontName := FONT_SEGOE_UI
  else
    FFontName := FONT_ARIAL;
  if Screen.Height >= 1024 then
    FFontSize := 10
  else
    FFontSize := 9;
  FMinFontSize := 8;
  SetBaseColor( clStatusBarBk );
end;

procedure TGuiStyle.BeforeDestruction;
begin
  FClientList.Free;
  inherited;
end;

procedure TGuiStyle.DoCustomDrawButton( Sender: TToolBar; Button: TToolButton; State: TCustomDrawState; var DefaultDraw: boolean );
var
  Buffer: TBitmap;
begin
  DefaultDraw := false;
  Buffer := TBitmap.Create;
  try
    Buffer.SetSize( Button.Width, Button.Height );
    Buffer.Canvas.Brush.Color := Sender.Color;
    Buffer.Canvas.Font.Name := FFontName;
    TdcToolButtonDraw.DrawToolButton( Buffer.Canvas, Button, Sender.Images, Sender.DisabledImages, State, Sender.ShowCaptions, Sender.List );
    Sender.Canvas.Draw( Button.BoundsRect.Left, Button.BoundsRect.Top, Buffer );
  finally
    Buffer.Free;
  end;
end;

procedure TGuiStyle.DoCustomDrawToolbar( Sender: TToolBar; const ARect: TRect; var DefaultDraw: boolean );
begin
  DefaultDraw := true;
  Sender.Canvas.Brush.Color := Sender.Color;
  Sender.Canvas.FillRect( ARect );
end;

function TGuiStyle.Get_Flat: boolean;
begin
  Result := fFlat;
end;

function TGuiStyle.Get_FontSize: integer;
begin
  Result := FFontSize;
end;

procedure TGuiStyle.StyleSimpleCheckbox( ACheck: TCheckBox );
begin
  ACheck.Font.Name := FFontName;
  ACheck.Font.Size := Max( FFontSize - 1, FMinFontSize );
  ACheck.Font.Color := VeryDarkColor;
  if ACheck.Alignment = taLeftJustify then
    ACheck.Width := 15 + 7 * ACheck.Font.Size;
  ACheck.Ctl3D := not fFlat;
end;

procedure TGuiStyle.StyleSmallHeaderLabel( ALabel: TLabel );
begin
  ALabel.Font.Name := FFontName;
  ALabel.Font.Size := Max( FFontSize - 1, FMinFontSize );
  ALabel.Layout := tlCenter;
  ALabel.Font.Color := clWhite;
  ALabel.Font.Style := [];
end;

procedure TGuiStyle.StyleHeaderPanel( APanel: TPanel );
begin
  StyleHeaderControl( APanel );
  APanel.Ctl3D := not fFlat;
end;

procedure TGuiStyle.StyleInfoLabel( ALabel: TdcLabel );
begin
  with ALabel do
  begin
    AlignWithMargins := true;
    Font.Name := FFontName;
    Font.Size := Max( Min( FFontSize - 1, 9 ), FMinFontSize );
    Font.Color := TColorCalculator.BlendColors( VeryDarkColor, PrettyDarkColor, 50 );
    Transparent := true;
    EllipsisPosition := epEndEllipsis;
    AutoSize := false;
    Height := abs( Font.Height ) + 4;
    Hint := Caption;
    ShowHint := true;
  end;
end;

procedure TGuiStyle.StyleHeaderLabel( ALabel: TLabel );
begin
  StyleHeaderControl( ALabel );
end;

procedure TGuiStyle.StyleHeaderPanel( APanel: TdcHeaderPanel );
begin
  APanel.HighlightActiveColor := BaseColor;
  APanel.HeaderPadding.SetBounds( SpacingDefault, SpacingDouble, SpacingDefault, SpacingDouble );
  APanel.HighlightStyle := hsThinBar;
  APanel.HeaderRelativeFontSize := 3;
  APanel.Font.Size := FFontSize;
  APanel.Font.Name := FFontName;
end;

procedure TGuiStyle.StyleToolBar( AToolbar: TToolBar; AStyle: TGuiControlStyle );
var
  i: integer;
begin
  case AStyle of
    gsArena:
      begin
        AToolbar.BorderWidth := 0;
        AToolbar.DrawingStyle := dsNormal;
        AToolbar.EdgeBorders := [];
        AToolbar.EdgeInner := esNone;
        AToolbar.EdgeOuter := esNone;
        AToolbar.Flat := true;
        AToolbar.Font.Name := FFontName;
        AToolbar.Font.Size := Max( FFontSize, FMinFontSize );
        AToolbar.GradientDrawingOptions := [];
        AToolbar.OnCustomDraw := DoCustomDrawToolbar;
        AToolbar.OnCustomDrawButton := DoCustomDrawButton;
        AToolbar.ParentColor := true;
        AToolbar.Transparent := false;
        for i := 0 to Pred( AToolbar.ButtonCount ) do
          AToolbar.Buttons[i].Cursor := crHandPoint;
      end;
  else
    begin
      AToolbar.Font.Name := FFontName;
      AToolbar.Font.Size := Max( FFontSize - 1, FMinFontSize );
      if Assigned( AToolbar.Images ) then
      begin
        AToolbar.AutoSize := false;
        FToolbarHeight := AToolbar.Images.Height + abs( AToolbar.Font.Height ) + 14;
      end
      else
        AToolbar.AutoSize := true;
      if AToolbar.ShowCaptions then
        AToolbar.Height := FToolbarHeight
      else
        AToolbar.Height := AToolbar.Images.Height + 11;
      AToolbar.BorderWidth := 0;
      AToolbar.EdgeOuter := esLowered;
      AToolbar.EdgeInner := esRaised;
      if fFlat then
      begin
        AToolbar.EdgeBorders := [ebBottom];
        AToolbar.DrawingStyle := dsNormal;
        AToolbar.GradientStartColor := clNone;
        AToolbar.GradientEndColor := clNone;
      end
      else
      begin
        AToolbar.EdgeBorders := [ebTop, ebBottom];
        AToolbar.GradientEndColor := MediumColor;
        AToolbar.GradientStartColor := VeryLightColor;
        AToolbar.DrawingStyle := dsGradient;
      end;
      AToolbar.Transparent := false;
    end;
  end;
end;

procedure TGuiStyle.StyleH1Label( ALabel: TLabel );
begin
  ALabel.Font.Name := FFontName;
  ALabel.Font.Size := 16;
  ALabel.Font.Color := $0089732F;
  ALabel.Font.Style := [];
  ALabel.AutoSize := true;
end;

procedure TGuiStyle.StyleHeaderControl( AControl: TControl );
var
  thisFont: TFont;
  thisPanel: TPanel;
begin
  Assert( Assigned( AControl ) );
  if AControl is TLabel then
    with AControl as TLabel do
    begin
      Transparent := true;
      Layout := tlCenter;
      Align := alClient;
      AlignWithMargins := true;
      Margins.Left := 8;
      Margins.Top := 0;
      Margins.Bottom := 0;
      Margins.Right := 2;
      thisFont := Font
    end
  else if AControl is TPanel then
    thisFont := TPanel( AControl ).Font
  else
    exit;
  thisFont.Name := FFontName;
  thisFont.Size := FFontSize;
  thisFont.Color := clBlack;
  thisFont.Style := [];
  if AControl is TPanel then
    thisPanel := AControl as TPanel
  else if AControl.Parent is TPanel then
    thisPanel := AControl.Parent as TPanel
  else
    exit;
  thisPanel.BorderWidth := 0;
  thisPanel.AutoSize := false;
  thisPanel.Color := HeaderColor;
  thisPanel.ClientHeight := abs( thisFont.Height ) + 16;
end;

procedure TGuiStyle.StylePanel( APanel: TCustomPanel );
begin
  with TExposedPanel( APanel ) do
  begin
    Color := DialogColor;
    Font.Color := VeryDarkColor;
    Font.Name := FFontName;
  end;
end;

procedure TGuiStyle.StyleTopPanel( APanel: TPanel );
begin
  APanel.BevelEdges := [beTop, beBottom];
  APanel.BevelKind := bkTile;
  APanel.Color := VeryLightColor;
  APanel.Font.Name := FFontName;
end;

procedure TGuiStyle.StyleTopLabel( ALabel: TLabel );
begin
  ALabel.Font.Name := FFontName;
  ALabel.Font.Size := FFontSize + 5;
  ALabel.Font.Style := [fsBold];
  ALabel.Font.Color := DarkColor;
end;

function TGuiStyle.ToolbarHeight: integer;
begin
  Result := FToolbarHeight;
end;

procedure TGuiStyle.StyleFrame( AFrame: TFrame );
begin
  AFrame.Color := DialogColor;
  AFrame.Font.Name := FFontName;
  AFrame.Font.Size := FFontSize;
end;

procedure TGuiStyle.StyleButton( AButton: TCustomButton );
begin
  if AButton is TButton then
    with AButton as TButton do
    begin
      Font.Name := FFontName;
      Font.Size := FFontSize - 1;
      Width := 60 + 3 * abs( Font.Height );
      Margins.Top := 8;
      Margins.Bottom := 8;
    end
  else if AButton is TBitBtn then
    with AButton as TBitBtn do
    begin
      Font.Name := FFontName;
      Font.Size := FFontSize - 1;
      Margins.Top := 8;
      Margins.Bottom := 8;
      Width := 64 + 4 * abs( Font.Height );
    end;
end;

procedure TGuiStyle.StyleButtonPanel( APanel: TCustomPanel );
begin
  with TExposedPanel( APanel ) do
  begin
    AutoSize := false;
    Color := LightColor;
    BorderWidth := 0;
    Font.Size := FFontSize;
    Font.Name := FFontName;
    Font.Color := DarkColor;
    ParentColor := false;
    Height := 44 + abs( Font.Height );
  end;
end;

procedure TGuiStyle.StyleCheckPanel( APanel: TCustomPanel );
var
  cbShowAll: TControl;
begin
  with TExposedPanel( APanel ) do
  begin
    Font.Name := FFontName;
    Font.Size := Max( FFontSize - 1, FMinFontSize );
    Height := abs( Font.Height ) + 12;
    AutoSize := false;
    Padding.Top := 3;
    Padding.Left := 3;
    Padding.Right := 3;
    Padding.Bottom := 3;
  end;
end;

procedure TGuiStyle.StyleDialog( AForm: TForm );
begin
  AForm.Color := clDlgBackground;
  AForm.Font.Name := FFontName;
  AForm.Font.Size := FFontSize;
end;

procedure TGuiStyle.StyleDialogControl( AControl: TControl; ADialogControl: TDialogControl );
begin
  case ADialogControl of
    dsHeaderTitleAlone:
      with AControl as TLabel do
      begin
        AlignWithMargins := true;
        Margins.SetBounds( 0, 15, 0, 0 );
        Font.Name := FFontName;
        Font.Color := $008B5F00;
        Font.Style := [];
        Font.Size := Round( FFontSize * 1.4 );
      end;
    dcHeaderTitle:
      with AControl as TLabel do
      begin
        AlignWithMargins := false;
        Font.Name := FFontName;
        Font.Color := $008B5F00;
        Font.Style := [];
        Font.Size := Round( FFontSize * 1.4 );
      end;
    dcHeaderSubTitle:
      with AControl as TLabel do
      begin
        AlignWithMargins := false;
        Font.Name := FFontName;
        Font.Size := FFontSize;
        Font.Color := clGrayText;
        Font.Style := [];
      end;
  end;
end;

procedure TGuiStyle.StyleDialogFooter( APanel: TPanel );
var
  i: integer;
begin
  APanel.Color := clWindow;
  APanel.ParentBackground := false;
  APanel.Padding.SetBounds( 20, 15, 20, 15 );
  for i := 0 to Pred( APanel.ControlCount ) do
  begin
    if APanel.Controls[i] is TdcButton then
    begin
      APanel.Controls[i].Align := alRight;
      APanel.Controls[i].AlignWithMargins := true;
      APanel.Controls[i].Margins.SetBounds( 10, 0, 0, 0 );
    end;
  end;
end;

procedure TGuiStyle.StyleDialogHeader( APanel: TPanel );
begin
  APanel.Color := clWindow;
  APanel.ParentBackground := false;
  APanel.Padding.SetBounds( 20, 0, 20, 0 );
end;

procedure TGuiStyle.StyleForm( AForm: TForm );
begin
  AForm.Color := DialogColor;
  AForm.Font.Name := FFontName;
  AForm.Font.Size := FFontSize;
end;

procedure TGuiStyle.StyleTabset( ATabSet: TTabSet );
begin
  with ATabSet do
  begin
    Font.Name := FFontName;
    Font.Size := Max( FFontSize - 1, FMinFontSize );
    Style := tsSoftTabs;
    ParentBackground := false;
    BackgroundColor := VeryLightColor;
    UnselectedColor := VeryLightColor;
    SelectedColor := LightColor;
    DitherBackground := true;
    Height := abs( Font.Height ) + 10;
  end;
end;

procedure TGuiStyle.Set_BaseColor( const ABaseColor: TColor );
begin
  SetBaseColor( ABaseColor );
  NotifyClients;
end;

procedure TGuiStyle.NotifyClients;
var
  n: integer;
  thisElement: IGuiStyleObserver;
begin
  n := 0;
  while n < FClientList.Count do
  begin
    if Supports( FClientList[n], IGuiStyleObserver, thisElement ) then
      thisElement.UpdateStyle( Self );
    inc( n );
  end;
end;

procedure TGuiStyle.RegisterClient( AClient: IGuiStyleObserver );
begin
  if Assigned( AClient ) then
  begin
    FClientList.Add( TObject( AClient ) );
    AClient.UpdateStyle( Self );
  end;
end;

procedure TGuiStyle.UnregisterClient( AClient: IGuiStyleObserver );
begin
  FClientList.Remove( TObject( AClient ) );
end;

procedure TGuiStyle.UnregisterAll;
begin
  FClientList.Clear;
end;

procedure TGuiStyle.StyleInfoLabel( ALabel: TLabel );
begin
  with ALabel do
  begin
    AlignWithMargins := true;
    Font.Name := FFontName;
    Font.Size := Max( Min( FFontSize - 1, 9 ), FMinFontSize );
    Font.Color := TColorCalculator.BlendColors( VeryDarkColor, PrettyDarkColor, 50 );
    Transparent := true;
    EllipsisPosition := epEndEllipsis;
    Layout := tlCenter;
    AutoSize := false;
    Height := abs( Font.Height ) + 4;
    Hint := Caption;
    ShowHint := true;
  end;
end;

procedure TGuiStyle.StyleLabel( ALabel: TLabel );
begin
  with ALabel do
  begin
    Font.Name := FFontName;
    Font.Size := Max( FFontSize - 1, FMinFontSize );
    Font.Color := VeryDarkColor;
    Transparent := true;
    AutoSize := true;
  end;
end;

procedure TGuiStyle.StyleAccentPanel( APanel: TPanel );
begin
  APanel.BorderStyle := bsNone;
  APanel.BevelOuter := bvNone;
  APanel.BevelInner := bvNone;
  APanel.Color := BaseColor;
  APanel.ParentFont := true;
  APanel.Font.Color := clWhite;
  APanel.StyleElements := [];
end;

procedure TGuiStyle.StyleBoldLabel( ALabel: TLabel );
begin
  with ALabel do
  begin
    Font.Name := FFontName;
    Font.Size := FFontSize + 1;
    Font.Style := [fsBold];
    Transparent := true;
    AutoSize := true;
  end;
end;

procedure TGuiStyle.StyleSmallLabel( ALabel: TLabel );
begin
  StyleLabel( ALabel );
  ALabel.Font.Size := Max( ALabel.Font.Size - 1, FMinFontSize );
end;

procedure TGuiStyle.SetColor( AControl: TControl; const APropName: string = 'Color'; AColor: TColor = clNone );
begin
  if AColor = clNone then
    AColor := LightColor;
  SetOrdProp( AControl, APropName, AColor );
end;

function TGuiStyle.Get_FontName: string;
begin
  Result := FFontName;
end;

procedure TGuiStyle.Set_FontName( const AFontName: string );
begin
  if AFontName <> FFontName then
  begin
    FFontName := AFontName;
    NotifyClients;
  end;
end;

procedure TGuiStyle.Set_FontSize( const AFontSize: integer );
begin
  if AFontSize <> FFontSize then
  begin
    FFontSize := AFontSize;
    NotifyClients;
  end;
end;

procedure TGuiStyle.Set_Flat( const AValue: boolean );
begin
  if AValue <> fFlat then
  begin
    fFlat := AValue;
    NotifyClients;
  end;
end;

{$ENDREGION}

initialization

GlobalStyle := TGuiStyle.Create;

finalization

GlobalStyle := nil;

end.
