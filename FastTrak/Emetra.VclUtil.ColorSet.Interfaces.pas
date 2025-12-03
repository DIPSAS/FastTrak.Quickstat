unit Emetra.VclUtil.ColorSet.Interfaces;

interface

uses
  Graphics;

type
  IHtmlColorSet = interface
    ['{41562062-95E5-480A-98FA-1FF9F0C5BA9D}']
    { Property accessors }
    function Get_Code: string;
    function Get_Dark: string;
    function Get_Light: string;
    function Get_Medium: string;
    function Get_VeryLight: string;
    { Properties }
    property Code: string read Get_Code;
    property Dark: string read Get_Dark;
    property Light: string read Get_Light;
    property Medium: string read Get_Medium;
    property VeryLight: string read Get_VeryLight;
  end;

  IGuiListBoxColorSet = interface
    ['{4F70A346-3F7C-43D7-A7B1-FD918E2562E2}']
    { Property accessors }
    function Get_CodeColor: TColor;
    function Get_FirstInfoColor: TColor;
    function Get_FocusedSelectionColor: TColor;
    function Get_SecondInfoColor: TColor;
    function Get_StatusTextColor: TColor;
    function Get_TextColor: TColor;
    function Get_UnfocusedSelectionColor: TColor;
    { Colors for listboxes }
    property CodeColor: TColor read Get_CodeColor;
    property FirstInfoColor: TColor read Get_FirstInfoColor;
    property FocusedSelectionColor: TColor read Get_FocusedSelectionColor;
    property SecondInfoColor: TColor read Get_SecondInfoColor;
    property StatusTextColor: TColor read Get_StatusTextColor;
    property TextColor: TColor read Get_TextColor;
    property UnfocusedSelectionColor: TColor read Get_UnfocusedSelectionColor;
  end;

  IGuiColorSet = interface( IGuiListBoxColorSet )
    ['{59484EAF-B22B-4734-AE6B-C019363096CF}']
    { Property accessors }
    function Get_BaseColor: TColor;
    function Get_CurrentCellColor: TColor;
    function Get_DarkColor: TColor;
    function Get_LightColor: TColor;
    function Get_MediumColor: TColor;
    function Get_PrettyDarkColor: TColor;
    function Get_TextColor: TColor;
    function Get_VeryDarkColor: TColor;
    function Get_VeryLightColor: TColor;
    { Properties }
    property BaseColor: TColor read Get_BaseColor;
    property DarkColor: TColor read Get_DarkColor;
    property LightColor: TColor read Get_LightColor;
    property MediumColor: TColor read Get_MediumColor;
    property PrettyDarkColor: TColor read Get_PrettyDarkColor;
    property VeryDarkColor: TColor read Get_VeryDarkColor;
    property VeryLightColor: TColor read Get_VeryLightColor;
  end;

const
  { Some default colors }
  clTextColor               = $00333333; // Dark Gray
  clUnfocusedSelectionColor = $00FCF2E7; // Very pale blue
  clFocusedSelectionColor   = $00D4FBFF; // Pale yellow
  clStatusTextColor         = $00822EB8; // Dark fuchsia
  clCodeColor               = $00A4294B; // Dark purple
  clFirstInfoColor          = $00AC6D2B; // Dark blue
  clSecondInfoColor         = $007BB02C; // Dark green
  { BergSoft colors }
  clSelectedFill          = $00FCEBDC;
  clModernBlue            = $00FBDF82;
  clModernSelectionBorder = $0065C3E5;
  clModernSelectionFill   = $00BBEFFF;

implementation

end.
