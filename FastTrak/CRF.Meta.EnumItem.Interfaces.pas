unit CRF.Meta.EnumItem.Interfaces;

interface

uses
  Db;

type
  TCRFEnumCompareResult = ( ecrNoDiff, ecrMinorDiff, ecrMajorDiff );
  ICRFEnumItem = interface
    ['{35821727-B0DA-4017-8A6A-369F8E789B3E}']
    { Property Accessors }
    function Get_AnswerId: integer;
    function Get_ExcludeFromText: boolean;
    function Get_ICD10: string;
    function Get_ItemText: string;
    function Get_HelpText: string;
    function Get_HtmlColor: string;
    function Get_IsDefaultAnswer: boolean;
    function Get_LastUpdate: TDateTime;
    function Get_ShortCode: string;
    function Get_Value: integer;
    function Get_VarName: string;
    function Get_VerboseText: string;
    function Get_Score: double;
    procedure Set_AnswerId( const AValue: integer );
    procedure Set_LastUpdate( const AValue: TDateTime );
    procedure Set_HelpText( const AValue: string );
    procedure Set_HtmlColor( const AValue: string );
    procedure Set_ICD10( const AValue: string );
    procedure Set_IsDefaultAnswer( const AValue: boolean );
    procedure Set_Score( const AValue: double );
    procedure Set_ShortCode( const AValue: string );
    procedure Set_Value( const AValue: integer );
    procedure Set_VarName( const AValue: string );
    procedure Set_VerboseText( const AValue: string );
    { Other members }
    function AsListbox( const ASimpleView: boolean = false ): string;
    function Compare( const ACompareTo: TObject ): TCRFEnumCompareResult;
    function IsFullSentence: boolean;
    procedure Load( ADataset: TDataset );
    { Properties }
    property AnswerId: integer read Get_AnswerId write Set_AnswerId;
    property ExcludeFromText: boolean read Get_ExcludeFromText;
    property ICD10: string read Get_ICD10 write Set_ICD10;
    property ItemText: string read Get_ItemText;
    property IsDefaultAnswer: boolean read Get_IsDefaultAnswer write Set_IsDefaultAnswer;
    property HelpText: string read Get_HelpText write Set_HelpText;
    property HtmlColor: string read Get_HtmlColor write Set_HtmlColor;
    property LastUpdate: TDateTime read Get_LastUpdate write Set_LastUpdate;
    property OptionText: string read Get_ItemText;
    property OrderNumber: integer read Get_Value;
    property ShortCode: string read Get_ShortCode write Set_ShortCode;
    property Score: double read Get_Score write Set_Score;
    property Value: integer read Get_Value write Set_Value;
    property VarName: string read Get_VarName write Set_VarName;
    property VerboseText: string read Get_VerboseText write Set_VerboseText;
  end;

implementation

end.
