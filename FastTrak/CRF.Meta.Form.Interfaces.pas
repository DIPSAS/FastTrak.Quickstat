unit CRF.Meta.Form.Interfaces;

interface

uses
  CRF.Meta.Item.Interfaces,
  CRF.Meta.Page.Interfaces,
  CRF.Meta.FormAction.Interfaces,
  Db;

type
  THideComment = ( hcAlwaysShown, hcStartHidden, hcAlwaysHidden );
  TCRFFormLocation = ( flUndefined, flAny, flLocal, flInternet, flDatabase ); { Where forms should be loaded from }

  ICRFFormId = interface
    ['{914FD381-1E05-49C3-828D-801A152A5396}']
    { Property accessors }
    function Get_FormId: integer;
    function Get_Name: string;
    { Properties }
    property Name: string read Get_Name;
    property FormId: integer read Get_FormId;
  end;

  ICRFFormIdObserver = interface
    ['{D439F1FC-52F6-4B69-AE81-C582C359AE07}']
    procedure AfterFormChange( const Sender: ICRFFormId );
  end;

  ICRFMetaForm = interface( ICRFFormId )
    ['{72B0536E-3995-4BAE-8093-296F039170EB}']
    { Property accessors }
    function Get_Actions: ICRFFormActionList;
    function Get_CalculateInvalid: boolean;
    function Get_Copyright: string;
    function Get_Count: integer;
    function Get_HideComment: THideComment;
    function Get_Instructions: string;
    function Get_Item( Index: integer ): ICRFMetaFormItem;
    function Get_Obsolete: boolean;
    function Get_ParentId: integer;
    function Get_RatingScale: boolean;
    function Get_Repeatable: boolean;
    function Get_Subtitle: string;
    function Get_SurveyStatus: string;
    function Get_Title: string;
    function Get_ThreadTypeId: integer;
    procedure Set_Repeatable( const Value: boolean );
    procedure Set_ThreadTypeId( const AValue: integer );
    procedure Set_Title( const AValue: string );
    { Other members }
    function AddMetaItem( const AMetaFormItem: ICRFMetaFormItem ): ICRFMetaFormItem;
    function AddPage( const APageNumber: integer ): ICRFMetaPage;
    function GetEnumerator: IEnumerator;
    function TryGetItem( const AItemId: integer; out ACRFItem: ICRFMetaFormItem ): boolean;
    procedure AddItemToPage( const APageNumber: integer; const AItem: ICRFMetaFormItem );
    procedure ChangeIdentity( const AFormId: integer; const AFormName: string );
    procedure Load( ADataset: TDataset );
    procedure Clone( const AMetaForm: ICRFMetaForm );
    procedure Normalize;
    { Properties }
    property Actions: ICRFFormActionList read Get_Actions;
    property CalculateInvalid: boolean read Get_CalculateInvalid;
    property Copyright: string read Get_Copyright;
    property Count: integer read Get_Count;
    property HideComment: THideComment read Get_HideComment;
    property Instructions: string read Get_Instructions;
    property Items[AIndex: integer]: ICRFMetaFormItem read Get_Item; default;
    property Obsolete: boolean read Get_Obsolete;
    property ParentId: integer read Get_ParentId;
    property RatingScale: boolean read Get_RatingScale;
    property Repeatable: boolean read Get_Repeatable write Set_Repeatable;
    property Subtitle: string read Get_Subtitle;
    property SurveyStatus: string read Get_SurveyStatus;
    property ThreadTypeId: integer read Get_ThreadTypeId write Set_ThreadTypeId;
    property Title: string read Get_Title write Set_Title;
  end;

  ICRFMetaFormList = interface
    ['{C88C5972-AD8A-4702-B814-130C1027414A}']
    procedure Clear;
    function Add( const AFormId: integer ): ICRFMetaForm;
    function FormId( const AFormName: string ): integer;
    function IsRepeatable( const AFormId: integer ): boolean;
    function IsObsolete( const AFormId: integer ): boolean;
    function TryGetForm( const AFormId: integer; out AMetaForm: ICRFMetaForm ): boolean;
  end;

  ICRFFormClearObserver = interface
    ['{158340C3-6F63-4D92-8972-5E398971DB29}']
    procedure BeforeFormClear( const Sender: ICRFMetaForm );
  end;

implementation

end.
