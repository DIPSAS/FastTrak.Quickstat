unit CRF.Meta.Item.Interfaces;

interface

uses
  CRF.Meta.Enum.Interfaces,
  CRF.Meta.Page.Interfaces,
  CRF.Meta.Interfaces;

type

  TCrfItemVisibility = ( civHideAlways = -1, civHideDynamic = 0, civShowDynamic = 1, civShowAlways = 2 );

  { Do NOT insert item types, only add them after itMSG, ORD(TCRFItemType) has meaning }

  TCRFItemType = ( itUNDEF, lsQN, lsORD, lsNOM, lsNAR, itDATE, itH1, itCHECKLIST, itMSG );

  { Do NOT insert item types, only add them }
  TCRFItemClearStrategy = ( clrNone, clrAfterHide, clrBeforeSave, clrNever );

  TCRFRowType = ( rtStandard, rtThreaded );

  ICRFMetaItem = interface( ICRFClickable )
    ['{F191B912-1867-4747-9333-CA5098394BDA}']
    { Property accessors }
    function Get_Comment: string;
    function Get_Description: string;
    function Get_Enum: ICRFEnum;
    function Get_ItemId: integer;
    function Get_ItemType: TCRFItemType;
    function Get_LastUpdate: TDateTime;
    function Get_LowerBound: double;
    function Get_MaxNormal: double;
    function Get_MaxOrder: integer;
    function Get_MinNormal: double;
    function Get_MinOrder: integer;
    function Get_OpenEHRProperty: integer;
    function Get_ThreadTypeId: integer;
    function Get_UnitStr: string;
    function Get_UpperBound: double;
    function Get_VarName: string;
    procedure Set_Enum( const AValue: ICRFEnum );
    procedure Set_ThreadTypeId( const AValue: integer );
    procedure Set_UnitStr( const AValue: string );
    procedure Set_MaxNormal( const AValue: double );
    procedure Set_MinNormal( const AValue: double );
    { other members }
    function CanBeMasterItem: boolean;
    function HasConstraints( out AMin, AMax: double ): boolean;
    function HasNormalRange( out AMin, AMax: double ): boolean;
    function Threaded: boolean;
    { Properties }
    property Comment: string read Get_Comment;
    property Description: string read Get_Description;
    property Enum: ICRFEnum read Get_Enum write Set_Enum;
    property ItemId: integer read Get_ItemId;
    property ItemType: TCRFItemType read Get_ItemType;
    property LastUpdate: TDateTime read Get_LastUpdate;
    property LowerBound: double read Get_LowerBound;
    property UpperBound: double read Get_UpperBound;
    property MinOrder: integer read Get_MinOrder;
    property MaxOrder: integer read Get_MaxOrder;
    property MaxNormal: double read Get_MaxNormal write Set_MaxNormal;
    property MinNormal: double read Get_MinNormal write Set_MinNormal;
    property OpenEHRProperty: integer read Get_OpenEHRProperty;
    property UnitStr: string read Get_UnitStr write Set_UnitStr;
    property VarName: string read Get_VarName;
    property ThreadTypeId: integer read Get_ThreadTypeId write Set_ThreadTypeId;
  end;

  ICRFMetaFormItem = interface( ICRFMetaItem )
    ['{A0418625-FE7B-4544-BBDC-DBB98353D62F}']
    { Property Accessors }
    function Get_AlwaysHidden: boolean;
    function Get_AlwaysInText: boolean;
    function Get_CarryForward: boolean;
    function Get_ClearStrategy: TCRFItemClearStrategy;
    function Get_Decimals: integer;
    function Get_DefaultValue: string;
    function Get_Enum: ICRFEnum;
    function Get_ExcludeCaption: boolean;
    function Get_ExcludeFromPrint: boolean;
    function Get_ExcludeFromText: boolean;
    function Get_Expression: string;
    function Get_FormatStr: string;
    function Get_FormId: integer;
    function Get_Header: string;
    function Get_Highlight: integer;
    function Get_IconIndex: integer;
    function Get_ItemHelp: string;
    function Get_MaxExpression: string;
    function Get_MaxCarryDays: double;
    function Get_MinExpression: string;
    function Get_Multiline: boolean;
    function Get_Optional: boolean;
    function Get_OrderNumber: integer;
    function Get_Page: ICRFMetaPage;
    function Get_PageNumber: integer;
    function Get_Question: string;
    function Get_ReadOnly: boolean;
    function Get_Visibility: TCrfItemVisibility;
    function Get_VisualId: string;
    procedure Set_CarryForward( const AValue: boolean );
    procedure Set_ClearStrategy( const AValue: TCRFItemClearStrategy );
    procedure Set_Decimals( const AVvalue: integer );
    procedure Set_ExcludeCaption( const AValue: boolean );
    procedure Set_ExcludeFromText( const AValue: boolean );
    procedure Set_Expression( const AValue: string );
    procedure Set_FormatStr( const AValue: string );
    procedure Set_Header( const AValue: string );
    procedure Set_Highlight( const AValue: integer );
    procedure Set_ItemHelp( const AValue: string );
    procedure Set_MaxExpression( const AExpression: string );
    procedure Set_MinExpression( const AExpression: string );
    procedure Set_Multiline( const AValue: boolean );
    procedure Set_Optional( const AValue: boolean );
    procedure Set_Page( const Value: ICRFMetaPage );
    procedure Set_PageNumber( const Value: integer );
    procedure Set_Question( const AValue: string );
    procedure Set_ReadOnly( const AValue: boolean );
    procedure Set_Visibility( const AValue: TCrfItemVisibility );
    { Other members }
    function CanHaveData: boolean;
    function CanSaveData: boolean;
    function HasCustomNumericFormat: boolean;
    function HeaderOrQuestion: string;
    function QuantityFormatStr: string;
    function IsCalculated: boolean;
    function Symbols: string;
    { Properties }
    property AlwaysHidden: boolean read Get_AlwaysHidden;
    property AlwaysInText: boolean read Get_AlwaysInText;
    property CarryForward: boolean read Get_CarryForward write Set_CarryForward;
    property ClearStrategy: TCRFItemClearStrategy read Get_ClearStrategy write Set_ClearStrategy;
    property Decimals: integer read Get_Decimals write Set_Decimals;
    property DefaultValue: string read Get_DefaultValue;
    property ExcludeCaption: boolean read Get_ExcludeCaption write Set_ExcludeCaption;
    property ExcludeFromPrint: boolean read Get_ExcludeFromPrint;
    property ExcludeFromText: boolean read Get_ExcludeFromText write Set_ExcludeFromText;
    property Expression: string read Get_Expression write Set_Expression;
    property FormId: integer read Get_FormId;
    property FormatStr: string read Get_FormatStr write Set_FormatStr;
    property Header: string read Get_Header write Set_Header;
    property Highlight: integer read Get_Highlight write Set_Highlight;
    property IconIndex: integer read Get_IconIndex;
    property ItemHelp: string read Get_ItemHelp write Set_ItemHelp;
    property MaxCarryDays: double read Get_MaxCarryDays;
    property MaxExpression: string read Get_MaxExpression write Set_MaxExpression;
    property MinExpression: string read Get_MinExpression write Set_MinExpression;
    property Multiline: boolean read Get_Multiline write Set_Multiline;
    property Optional: boolean read Get_Optional write Set_Optional;
    property OrderNumber: integer read Get_OrderNumber;
    property Page: ICRFMetaPage read Get_Page write Set_Page;
    property PageNumber: integer read Get_PageNumber;
    property Question: string read Get_Question write Set_Question;
    property ReadOnly: boolean read Get_ReadOnly write Set_ReadOnly;
    property VisualId: string read Get_VisualId;
    property Visibility: TCrfItemVisibility read Get_Visibility write Set_Visibility;
  end;

  ICRFMinMaxOrder = interface
    ['{02904936-FAFE-4AA4-BD25-CC094CA48550}']
    function Get_MinOrder: integer;
    function Get_MaxOrder: integer;
    { Properties }
    property MinOrder: integer read Get_MinOrder;
    property MaxOrder: integer read Get_MaxOrder;
  end;

const
  CRF_ITEMS_WITH_DATA    = [lsORD, lsQN, lsNOM, lsNAR, itDATE];
  CRF_ITEMS_NUMERIC      = [lsORD, lsQN, itDATE];
  CRF_ITEMS_WITHOUT_DATA = [itUNDEF, itH1, itMSG, itCHECKLIST];

const
  CRF_ITEM_VISIBLE_STATES = [civShowDynamic, civShowAlways];

implementation

end.
