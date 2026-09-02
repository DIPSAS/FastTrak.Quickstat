unit Emetra.Vcl.Consts;

interface

uses
  Vcl.Graphics,
  {Winapi}
  Winapi.Windows;

const
  intTextDisabledAlpha    = 128; // 0 .. 255
  intSplitterFadeStep     = 12;
  intSplitterFadeInterval = 1;
  intSpinButtonWidth      = 18;
  intCheckBoxSize         = 14;

const
  clListBoxAlternationBackground = $00F6F6F6;

  clBtnFaceDisabled = $00E1E0E0;

  clControlBorderFocused = $008E8063; { Edit, CheckBox etc. }

  // NormalContainer_HeaderForeground
  clHeaderForeground = $0089732F;

  { FlatButtonIsDefaultBackground }
  clBtnFaceDefault        = $009D6A00;
  clBtnFaceDefaultHot     = $008C5F00;
  clBtnFaceDefaultPressed = $00654400;

  clBtnFaceDropDown      = $00E0E0E0;
  clBtnFaceNormal        = $00D5D5D5;
  clBtnFaceNormalHot     = $00BEBEBE;
  clBtnFaceNormalPressed = $00898989;

  { TODO }
  clTitleLine = $00D6D3CB;
  clDlgFace   = $00F3F3F3;

  { dc:DialogPresenter }
  clDlgBackground    = $00FAFAFA;
  clDlgContent       = $00F3F2F0;
  clDlgContentBorder = $00CCCCCC;

  clHeaderBk     = $00E9E6E2; // Reveal expander and slide in
  clSlideinBk    = clHeaderBk;
  clSlideinBkHot = $00D7D4D1;
  clFormBk       = $00F3F2F0; // $00FAFAFA;
  clFormBkLocked = $00FFFFFF;

  clToolButtonHot     = $00E0E0E0;
  clToolButtonPressed = $00A1A1A1;
  clToolButtonChecked = $00E9E7E2;

  clListSelectedBackgroundHot = $00EDEBE5;

  clContainerBackground = $00EEEEEE;
  // NormalContainer_Background_WhenFormStyleInfieldTopAlignedLabels
  clDatePickerDayHot      = $00F7F6F3;
  clDatePickerToday       = $00F1EFE8;
  clDatePickerTodayBorder = $00E7E5DD;
  clEditControl           = $00FEFEFE;
  clEditControlBorder     = $00A3A3A3;
  clEditControlBorderHot  = $00555451;
  clErrorGeometryFill     = $005E5EC9;
  clGlyph                 = $00444444;
  clGlyphDisabled         = $00C2C2C2;
  clGridHeaderBorder      = $00B2882D; // Grid header border
  clGridHeaderText        = $00444444;
  clGridText              = $00111111;
  clGridGridLines         = $00C7C7C0;
  clHyperlinkForeground   = $009E6F08;
  clInfoGeometryFill      = $00926E1E;
  clLabelForeground       = $00444444;
  clMandatoryBar          = $001685F6;
  clMandatoryGeometryFill = $00054689;
  clPopupBorder           = $00080808;
  clReadOnlyDashedLine    = $00BCBCBC;
  clReadOnlySolidLine     = $00C8C8C8;

  clSelectedBk         = $00E9D9C8;
  clSelectedBkDark     = $00897F04;
  clSelectedBkHot      = $00E8E6E0;
  clSelectedBkDisabled = $00F3EBE3;

  { CheckBox }
  clCheckBoxBorder        = $009C9A9A;
  clCheckBoxBorderHot     = $0071664F;
  clCheckBoxBorderChecked = clControlBorderFocused;
  clCheckBoxCheckMark     = $00333333;

  { PageControl }
  clTabsBackground       = $00DCD9D3;
  clSelectedTabFontColor = clHyperlinkForeground;
  clSelectedTabIndicator = clHyperlinkForeground;

  { Splitter }
  clSplitterBkHot = $00B7B7B7;

  { Standard BorderColor for TdcControl }
  clVeryDarkBorderColor = $00111111;

  clDotWideSpaceLine           = $00C3C3C3;
  clDotWideSpaceLineBackground = $00DEDEDE;

  { State Colors }
  ButtonColor: array [boolean] of integer        = ( clBtnFaceNormal, clBtnFaceDefault );
  ButtonColorHot: array [boolean] of integer     = ( clBtnFaceNormalHot, clBtnFaceDefaultHot );
  ButtonColorPressed: array [boolean] of integer = ( clBtnFaceNormalPressed, clBtnFaceDefaultPressed );
  ButtonColorText: array [boolean] of integer    = ( clBlack, clWhite );

const
  clStatusBarBk = $0080663E;
  { Misc }
  CONTROL_FRAME       = $00A5A2A2;
  SELECTED_TEXT_COLOR = $00FFFFFF;

  { Radio Button }
  RadioButtonRadius             = 14;
  RadioButtonsSpacing           = 10;
  RadioButtonBorder             = $00717171;
  RadioButtonBorderDisabled     = $00B6B6B6;
  RadioButtonCheckColorDisabled = $00C7AE75;
  clRadioButtonCheckMark        = clCheckBoxCheckMark;

{$REGION 'Button(s)'}
  DropDownButtonWidth = 16;
  ExecuteButtonWidth  = 21;
{$ENDREGION}
{$REGION 'Edit'}
  EditMultilineHeight = 48;
  EditWidth           = 112;
  PaddingEdit         = 4; // Border to content padding inside TEdit
  PreviewWidth        = 20;
{$ENDREGION}
{$REGION 'ComboBox'}
  DropDownCountDefault = 12;
{$ENDREGION}
  ValidColor: array [boolean] of integer = ( $005C5CCD, clNone );
  TagColorBlending                       = 200;
  TagColorSize                           = 8;
  MandatoryBarWidth                      = 3;

  ControlVisible: array [boolean] of integer = ( SW_HIDE, SW_SHOW );

  CheckBoxIndent           = 4;
  ComboBoxItemHeight       = 24;
  szItemHeight             = 13;
  clAlertErrorBackground   = $00F6F6F9;
  clAlertErrorBorder       = $009393D9;
  clAlertInfoBackground    = $00FBF8EC;
  clAlertInfoBorder        = $00B9B092;
  clAlertWarningBackground = $00F6FBFF;
  clAlertWarningBorder     = $00B0DEF2;

  clWarningGeometryFill = $000096D6;

resourcestring
  StrToday = 'i dag';
  StrReset = 'Nullstill';
  StrUnanswered = '(Ubesvart)';

var
  { Padding & Spacing }
  PaddingCell: integer = 6;
  PaddingCellText: integer = 8;

  SpacingThin: integer = 2;
  SpacingDouble: integer = 4;
  SpacingDefault: integer = 8;
  SpacingFar: integer = 12;

const
  DistanceDouple = 4;
  SplitterSize   = 5;

type
  TWebColors = ( cnColorName, cnColorValue );
  TWebColor = array [TWebColors] of string;

const
  DelphiColorNames: array [0 .. 51] of string = ( 'clBlack', 'clMaroon', 'clGreen', 'clOlive', 'clNavy', 'clPurple', 'clTeal', 'clGray', 'clSilver', 'clRed', 'clLime', 'clYellow', 'clBlue', 'clFuchsia', 'clAqua', 'clWhite', 'clMoneyGreen',
    'clSkyBlue', 'clCream', 'clMedGray', 'clActiveBorder', 'clActiveCaption', 'clAppWorkSpace', 'clBackground', 'clBtnFace', 'clBtnHighlight', 'clBtnShadow', 'clBtnText', 'clCaptionText', 'clDefault', 'clGradientActiveCaption',
    'clGradientInactiveCaption', 'clGrayText', 'clHighlight', 'clHighlightText', 'clHotLight', 'clInactiveBorder', 'clInactiveCaption', 'clInactiveCaptionText', 'clInfoBk', 'clInfoText', 'clMenu', 'clMenuBar', 'clMenuHighlight',
    'clMenuText', 'clNone', 'clScrollBar', 'cl3DDkShadow', 'cl3DLight', 'clWindow', 'clWindowFrame', 'clWindowText' );

  WebColors: array [0 .. 140] of TWebColor = ( ( 'transparent', '#FFFFFF' ), ( 'aliceblue', '#F0F8FF' ), ( 'antiquewhite', '#FAEBD7' ), ( 'aqua', '#00FFFF' ), ( 'aquamarine', '#7FFFD4' ), ( 'azure', '#F0FFFF' ), ( 'beige', '#F5F5DC' ),
    ( 'bisque', '#FFE4C4' ), ( 'black', '#000000' ), ( 'blanchedalmond', '#FFEBCD' ), ( 'blue', '#0000FF' ), ( 'blueviolet', '#8A2BE2' ), ( 'brown', '#A52A2A' ), ( 'burlywood', '#DEB887' ), ( 'cadetblue', '#5F9EA0' ),
    ( 'chartreuse', '#7FFF00' ), ( 'chocolate', '#D2691E' ), ( 'coral', '#FF7F50' ), ( 'cornflowerblue', '#6495ED' ), ( 'cornsilk', '#FFF8DC' ), ( 'crimson', '#DC143C' ), ( 'cyan', '#00FFFF' ), ( 'darkblue', '#00008B' ),
    ( 'darkcyan', '#008B8B' ), ( 'darkgoldenrod', '#B8860B' ), ( 'darkgray', '#A9A9A9' ), ( 'darkgreen', '#006400' ), ( 'darkkhaki', '#BDB76B' ), ( 'darkmagenta', '#8B008B' ), ( 'darkolivegreen', '#556B2F' ), ( 'darkorange', '#FF8C00' ),
    ( 'darkorchid', '#9932CC' ), ( 'darkred', '#8B0000' ), ( 'darksalmon', '#E9967A' ), ( 'darkseagreen', '#8FBC8B' ), ( 'darkslateblue', '#483D8B' ), ( 'darkslategray', '#2F4F4F' ), ( 'darkturquoise', '#00CED1' ),
    ( 'darkviolet', '#9400D3' ), ( 'deeppink', '#FF1493' ), ( 'deepskyblue', '#00BFFF' ), ( 'dimgray', '#696969' ), ( 'dodgerblue', '#1E90FF' ), ( 'firebrick', '#B22222' ), ( 'floralwhite', '#FFFAF0' ), ( 'forestgreen', '#228B22' ),
    ( 'fuchsia', '#FF00FF' ), ( 'gainsboro', '#DCDCDC' ), ( 'ghostwhite', '#F8F8FF' ), ( 'gold', '#FFD700' ), ( 'goldenrod', '#DAA520' ), ( 'gray', '#808080' ), ( 'green', '#008000' ), ( 'greenyellow', '#ADFF2F' ),
    ( 'honeydew', '#F0FFF0' ), ( 'hotpink', '#FF69B4' ), ( 'indianred', '#CD5C5C' ), ( 'indigo', '#4B0082' ), ( 'ivory', '#FFFFF0' ), ( 'khaki', '#F0E68C' ), ( 'lavender', '#E6E6FA' ), ( 'lavenderblush', '#FFF0F5' ),
    ( 'lawngreen', '#7CFC00' ), ( 'lemonchiffon', '#FFFACD' ), ( 'lightblue', '#ADD8E6' ), ( 'lightcoral', '#F08080' ), ( 'lightcyan', '#E0FFFF' ), ( 'lightgoldenrodyellow', '#FAFAD2' ), ( 'lightgreen', '#90EE90' ),
    ( 'lightgrey', '#D3D3D3' ), ( 'lightpink', '#FFB6C1' ), ( 'lightsalmon', '#FFA07A' ), ( 'lightseagreen', '#20B2AA' ), ( 'lightskyblue', '#87CEFA' ), ( 'lightslategray', '#778899' ), ( 'lightsteelblue', '#B0C4DE' ),
    ( 'lightyellow', '#FFFFE0' ), ( 'lime', '#00FF00' ), ( 'limegreen', '#32CD32' ), ( 'linen', '#FAF0E6' ), ( 'magenta', '#FF00FF' ), ( 'maroon', '#800000' ), ( 'mediumaquamarine', '#66CDAA' ), ( 'mediumblue', '#0000CD' ),
    ( 'mediumorchid', '#BA55D3' ), ( 'mediumpurple', '#9370DB' ), ( 'mediumseagreen', '#3CB371' ), ( 'mediumslateblue', '#7B68EE' ), ( 'mediumspringgreen', '#00FA9A' ), ( 'mediumturquoise', '#48D1CC' ), ( 'mediumvioletred', '#C71585' ),
    ( 'midnightblue', '#191970' ), ( 'mintcream', '#F5FFFA' ), ( 'mistyrose', '#FFE4E1' ), ( 'moccasin', '#FFE4B5' ), ( 'navajowhite', '#FFDEAD' ), ( 'navy', '#000080' ), ( 'oldlace', '#FDF5E6' ), ( 'olive', '#808000' ),
    ( 'olivedrab', '#6B8E23' ), ( 'orange', '#FFA500' ), ( 'orangered', '#FF4500' ), ( 'orchid', '#DA70D6' ), ( 'palegoldenrod', '#EEE8AA' ), ( 'palegreen', '#98FB98' ), ( 'paleturquoise', '#AFEEEE' ), ( 'palevioletred', '#DB7093' ),
    ( 'papayawhip', '#FFEFD5' ), ( 'peachpuff', '#FFDAB9' ), ( 'peru', '#CD853F' ), ( 'pink', '#FFC0CB' ), ( 'plum', '#DDA0DD' ), ( 'powderblue', '#B0E0E6' ), ( 'purple', '#800080' ), ( 'red', '#FF0000' ), ( 'rosybrown', '#BC8F8F' ),
    ( 'royalblue', '#4169E1' ), ( 'saddlebrown', '#8B4513' ), ( 'salmon', '#FA8072' ), ( 'sandybrown', '#F4A460' ), ( 'seagreen', '#2E8B57' ), ( 'seashell', '#FFF5EE' ), ( 'sienna', '#A0522D' ), ( 'silver', '#C0C0C0' ),
    ( 'skyblue', '#87CEEB' ), ( 'slateblue', '#6A5ACD' ), ( 'slategray', '#708090' ), ( 'snow', '#FFFAFA' ), ( 'springgreen', '#00FF7F' ), ( 'steelblue', '#4682B4' ), ( 'tan', '#D2B48C' ), ( 'teal', '#008080' ), ( 'thistle', '#D8BFD8' ),
    ( 'tomato', '#FF6347' ), ( 'turquoise', '#40E0D0' ), ( 'violet', '#EE82EE' ), ( 'wheat', '#F5DEB3' ), ( 'white', '#FFFFFF' ), ( 'whitesmoke', '#F5F5F5' ), ( 'yellow', '#FFFF00' ), ( 'yellowgreen', '#9ACD32' ) );

implementation

end.
