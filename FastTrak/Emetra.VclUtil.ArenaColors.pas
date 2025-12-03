unit Emetra.VclUtil.ArenaColors;

interface

uses
  Vcl.ExtCtrls, Vcl.StdCtrls, Vcl.Forms,
  RzTabs,
  Emetra.VclComp.ListView,
  Emetra.VclUtil.Style.Interfaces;

const
  clNormalContainerBackgroundWhenReadOnly = $00FFFFFF;
  clMenuBackgroundDarkBrush               = $00665F03;
  clMenuItemSelectionForeground           = $00FFFFFF;
  clMaximizedTileBackground               = $00F8F8F8;
  clMinimizedPatientTileBackground        = $00908644;

  { Arena Common Brushes }
  clSelectedItemBackground = $00897F04; { Official Arena Common Brushes }

  { Arena List Brushes }
  clArenaListSelectedBackground              = $00918817; { Offical Arena color }
  clArenaListSelectedForeground              = $00FFFFFF; { Official Arena color }
  clLightweightDataGridALternationBackground = $00ECECEC; { Official is transparent, so using dropper in Demo CLient }
  clLightweightDataGridBackground            = $00F3F3F3; { Official is transparent, so using dropper in Demo Client }

  { Arena Menu brushes }
  clMenuItemSelectionStroke = clArenaListSelectedBackground;
  clMenuItemSelectionFill   = clArenaListSelectedBackground;
  clRightArrowFill          = clArenaListSelectedBackground;
  clSeparatorFill           = $00E0E0E0;
  clSeparatorFill2          = $00FFFFFF;

  { Magne's derived colors }
  clListSelectedBackgroundUnfocused = $00B6AE50; { MR: I made this up with ColorImpact }

  { Dropper colors }
  clFormFace         = $00EEEEEE;
  clArenaLavender    = $00F1E9E9;
  clMyGreenColor     = $00FBFBF4;
  clMyAlternateColor = $00F7F7F7;
  clMyListboxColor   = $00FFFFFF;

type
  TArenaColors = class( TObject )
    class procedure StyleHeaderPanel( APanel: TPanel );
    class procedure StyleForm( AForm: TForm );
    class procedure StyleFrame( AFrame: TFrame );
    class procedure StyleLabel( ALabel: TLabel );
    class procedure StyleTabs( APage: TRzPageControl );
    class procedure StyleSimpleCheckbox( ACheck: TCheckbox );
    class procedure StyleListView( AView: TObjectListView );
  end;

implementation

const
  FONT_NAME = 'Calibri';
  FONT_SIZE = 10;

  { TArenaColors }

class procedure TArenaColors.StyleForm( AForm: TForm );
begin
  with AForm do
  begin
    Color := clFormFace;
    Font.Name := FONT_NAME;
    Font.Size := FONT_SIZE;
  end;

end;

class procedure TArenaColors.StyleFrame(AFrame: TFrame);
begin
  AFrame.ParentFont := true;
  AFrame.ParentBackground := true;
  AFrame.ParentColor := true;
end;

class procedure TArenaColors.StyleHeaderPanel( APanel: TPanel );
begin
  with APanel do
  begin
    Color := clMenuItemSelectionFill;
    Font.Color := clMenuItemSelectionForeground;
    Font.Style := [];
    Font.Name := FONT_NAME;
    Font.Size := FONT_SIZE + 1;
  end;
end;

class procedure TArenaColors.StyleLabel( ALabel: TLabel );
begin
  with ALabel do
  begin
    Font.Name := FONT_NAME;
    Font.Style := [];
    Font.Size := FONT_SIZE;
  end;
end;

class procedure TArenaColors.StyleListView( AView: TObjectListView );
begin
  with AView do
  begin
    GapY := 6;
    ListSelectedBackground := clArenaListSelectedBackground;
    ListSelectedForeground := clArenaListSelectedForeground;
    ListSelectedBackgroundUnfocused := clListSelectedBackgroundUnfocused;
    Color := clMyListboxColor;
    AlternateColor := clMyAlternateColor;
    Font.Size := FONT_SIZE;
    Font.Name := FONT_NAME;
  end;
end;

class procedure TArenaColors.StyleSimpleCheckbox( ACheck: TCheckbox );
begin
  with ACheck do
  begin
    Font.Name := FONT_NAME;
    Font.Size := 9;
  end;
end;

class procedure TArenaColors.StyleTabs( APage: TRzPageControl );
begin
  APage.Font.Name := FONT_NAME;
  APage.Font.Size := FONT_SIZE;
  APage.Color := clMyGreenColor;
  APage.ShowFocusRect := false;
  APage.ShowCardFrame := false;
  APage.ShowFullFrame := false;
  APage.ShowShadow := true;
  APage.TabStyle := tsSquareCorners;
  APage.TabColors.HighlightBar := clArenaListSelectedBackground;
  APage.TabColors.Unselected := clMyGreenColor;
  APage.LightenUnselectedColoredTabs := false;
  APage.BoldCurrentTab := true;
  APage.UseColoredTabs := true;
  APage.UseGradients := false;
  APage.HotTrackStyle := htsTabBar;
  APage.FlatColor := clMyGreenColor;
  APage.BackgroundColor := APage.FlatColor;
end;

end.
