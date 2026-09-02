unit Emetra.VclUtil.Style.Interfaces;

interface

uses
  Emetra.VclUtil.ColorSet.Interfaces,
  Vcl.Graphics, Vcl.Controls, Vcl.StdCtrls, Vcl.Forms, Vcl.ExtCtrls, Vcl.Tabs, Vcl.ComCtrls,
  Emetra.Vcl.StdCtrls, Emetra.Vcl.ExtCtrls;

type
  IGuiStyleObserver = interface; // Needs to be pre-declared for IGuiStyle

  TGuiControlStyle = ( gsDefault, gsArena );

  TDialogControl = ( dcHeaderTitle, dsHeaderTitleAlone, dcHeaderSubTitle );

  IGuiStyle = interface( IGuiColorSet )
    ['{03AD6C21-F68E-4719-89E7-900334BFE50F}']
    { Property accessors }
    function Get_BaseColor: TColor;
    function Get_CurrentCellColor: TColor;
    function Get_Flat: boolean;
    function Get_FontName: string;
    function Get_FontSize: integer;
    function Get_MediumColor: TColor;
    function Get_LightColor: TColor;
    function Get_DarkColor: TColor;
    function Get_PrettyDarkColor: TColor;
    function Get_VeryDarkColor: TColor;
    function Get_VeryLightColor: TColor;
    procedure Set_Flat( const AValue: boolean );
    procedure Set_FontName( const AFontName: string );
    procedure Set_FontSize( const AFontSize: integer );
    procedure Set_BaseColor( const AColor: TColor );
    { Other members }
    function ToolBarHeight: integer;
    procedure StyleAccentPanel( APanel: TPanel );
    procedure StyleButton( AButton: TCustomButton );
    procedure StyleButtonPanel( APanel: TCustomPanel );
    procedure StyleCheckPanel( APanel: TCustomPanel );
    procedure StyleDialog( AForm: TForm );
    procedure StyleDialogControl( AControl: TControl; ADialogControl: TDialogControl );
    procedure StyleDialogHeader( APanel: TPanel );
    procedure StyleDialogFooter( APanel: TPanel );
    procedure StyleForm( AForm: TForm );
    procedure StyleFrame( AFrame: TFrame );
    procedure StyleBoldLabel( ALabel: TLabel );
    procedure StyleH1Label( ALabel: TLabel );
    procedure StyleHeaderLabel( ALabel: TLabel );
    procedure StyleHeaderPanel( APanel: TPanel ); overload;
    procedure StyleHeaderPanel( APanel: TdcHeaderPanel ); overload;
    procedure StyleInfoLabel( ALabel: TLabel ); overload;
    procedure StyleInfoLabel( ALabel: TdcLabel ); overload;
    procedure StyleLabel( ALabel: TLabel );
    procedure StylePanel( APanel: TCustomPanel );
    procedure StyleSimpleCheckbox( ACheck: TCheckbox );
    procedure StyleSmallHeaderLabel( ALabel: TLabel );
    procedure StyleSmallLabel( ALabel: TLabel );
    procedure StyleTabset( ATabSet: TTabSet );
    procedure StyleToolBar( AToolbar: TToolbar; AStyle: TGuiControlStyle = gsDefault );
    procedure StyleTopLabel( ALabel: TLabel );
    procedure StyleTopPanel( APanel: TPanel );
    { Registration of controls }
    procedure RegisterClient( AObject: IGuiStyleObserver );
    procedure UnregisterClient( AObject: IGuiStyleObserver );
    procedure UnregisterAll;
    { Font properties }
    property FontName: string read Get_FontName write Set_FontName;
    property FontSize: integer read Get_FontSize write Set_FontSize;
    property Flat: boolean read Get_Flat write Set_Flat;
    { Colors in order of darkness }
    property BaseColor: TColor read Get_BaseColor write Set_BaseColor;
    property CurrentCellColor: TColor read Get_CurrentCellColor;
    property VeryLightColor: TColor read Get_VeryLightColor;
    property LightColor: TColor read Get_LightColor;
    property MediumColor: TColor read Get_MediumColor;
    property DarkColor: TColor read Get_DarkColor;
    property PrettyDarkColor: TColor read Get_PrettyDarkColor;
    property VeryDarkColor: TColor read Get_VeryDarkColor;
  end;

  IGuiStyleObserver = interface
    ['{97F32F1B-4DEB-42BD-A8F3-0D03E40111F5}']
    procedure UpdateStyle( Sender: IGuiStyle );
  end;

var
  GlobalStyle: IGuiStyle;

implementation

end.
