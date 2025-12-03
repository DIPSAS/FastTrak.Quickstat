unit CRF.Meta.FormAction.Interfaces;

interface

uses
  CRF.Meta.Item.Interfaces;

type
  TCRFFormItemActionType = ( actHideIfEqualTo, actShowIfEqualTo, actShowIfLessThan, actShowIfGreaterThan, actShowIfLessThanOrEqualTo,
    actShowIfGreaterThanOrEqualTo, actShowIfNotEqualToTo );

  ICRFBaseAction = interface
    ['{D9A31D92-6EA7-4F7F-A1E2-259107FC1F70}']
    { Property Accessors }
    function Get_ActionId: integer;
    function Get_DetailId: integer;
    function Get_FormId: integer;
    function Get_Value: integer;
    function Get_MasterId: integer;
    { Properties }
    property ActionId: integer read Get_ActionId;
    property FormId: integer read Get_FormId;
    property MasterId: integer read Get_MasterId;
    property DetailId: integer read Get_DetailId;
    property Value: integer read Get_Value;
  end;

  ICRFSingleAction = interface( ICRFBaseAction )
    ['{49B881F5-8538-44FD-84C6-263D7969FBA7}']
    { Property Accessors }
    function Get_MakesVisible: boolean;
    procedure Set_MakesVisible( const AValue: boolean );
    { Properties }
    property MakesVisible: boolean read Get_MakesVisible write Set_MakesVisible;
  end;

  ICRFMultiAction = interface( ICRFBaseAction )
    ['{4A04A876-8922-43E2-85CF-866967F7F392}']
    { Property Accessors }
    function Get_Activation: TCRFFormItemActionType;
    { Properties }
    property Activation: TCRFFormItemActionType read Get_Activation;
  end;

  ICRFFormActionList = interface
    ['{98E3DA92-18BA-4C14-9D0D-521BBC636C41}']
    { Property Accessors }
    function Get_Count: integer;
    function Get_Item( AIndex: integer ): ICRFSingleAction;
    { Other methods }
    function Add( const AMaster, ADetail: ICRFMetaFormItem; const AValue: integer; const AActivation: TCRFFormItemActionType ): ICRFMultiAction; overload;
    procedure Add( const AMaster, ADetail: ICRFMetaFormItem; const AEnumValue: integer; const AMakesVisible: boolean ); overload;
    function Contains( const AMaster, ADetail: ICRFMetaFormItem ): boolean;
    function IsMaster( const AItem: ICRFMetaFormItem ): boolean;
    function TryGetAction( const AMasterId, ADetailId: integer; const AValue: integer; out AFound: ICRFSingleAction ): boolean;
    procedure Clear;
    { Properties }
    property Items[AIndex: integer]: ICRFSingleAction read Get_Item; default;
    property Count: integer read Get_Count;
  end;

implementation

end.
