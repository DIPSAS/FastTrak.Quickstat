unit CRF.Meta.Enum.Interfaces;

interface

uses
  CRF.Meta.EnumItem.Interfaces;

type

  ICRFEnum = interface
    ['{93A33FFD-B4FD-4B43-90C2-7A1929E8A97B}']
    { Property Accessors }
    function Get_Id: integer;
    function Get_ShowNumbers: boolean;
    function Get_HasVerboseItems: boolean;
    function Get_Item( AIndex: integer ): ICRFEnumItem;
    function Get_OptionCount: integer;
    procedure Set_Id( const AId: integer );
    { Other members }
    function Add( const AOrder: integer; const AItemText: string; const AVerboseText: string ): ICRFEnumItem;
    function AsHelp( const AWithNums: boolean = false ): string;
    function Compare( const ACompareTo: TObject ): TCRFEnumCompareResult;
    function FindMatch( const AItemText: string ): integer;
    function FindText( const AOrder: integer ): string;
    function IndexOf( const AOrder: integer ): integer;
    function MinOrder: integer;
    function MinScore: double;
    function MaxOrder: integer;
    function MaxScore: double;
    function ScoreToValue( const AScore: double ): integer;
    function ShortCode( const AValue: integer ): string;
    function TryGetDefaultItem( out AItem: ICRFEnumItem ): boolean;
    function TryGetItem( const AValue: integer; out AItem: ICRFEnumItem ): boolean;
    function ValueToScore( const AValue: integer ): double;
    function ValidValue( const AValue: integer ): boolean;
    { properties }
    property Id: integer read Get_Id write Set_Id;
    property Items[AIndex: integer]: ICRFEnumItem read Get_Item; default;
    property HasVerboseItems: boolean read Get_HasVerboseItems;
    property OptionCount: integer read Get_OptionCount;
    property ShowNumbers: boolean read Get_ShowNumbers;
  end;

implementation

end.
