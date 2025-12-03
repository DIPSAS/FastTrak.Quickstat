unit CRF.Input.Interfaces;

interface

uses
  CRF.ClinForm.Interfaces,
  CRF.Meta.Enum.Interfaces,
  CRF.Meta.Form.Interfaces,
  CRF.Meta.Item.Interfaces,
  CRF.Meta.Page.Interfaces,
  CRF.Meta.FormAction.Interfaces,
  {Standard}
  Classes, Db, SysUtils;

type

  { Predeclarations }
  ICRFItem = interface;

  { Enumerations }
  TStrType = ( stNONE, stID, stQ, stNUM, stTXT ); { Columns on a visual form }
  TCRFItemLockStatus = ( lckUnlocked, lckYesButUnsaved, lckYes );
  TCRFItemReadStatus = ( rsUndefined, rsSameEventId, rsCarried, rsSameEventNum );
  TCRFItemValidationState = ( cvsUndefined, cvsValid, cvsInvalidOptional, cvsInvalidRequired, cvsLocked );
  TCRFItemEvalType = ( evValue, evMax, evMin );

  { Events }
  TCRFItemCalculateEvent = function( Sender: TObject; AType: TCRFItemEvalType; var Done: boolean ): double of object;
  TCRFItemEvent = procedure( Sender: ICRFItem ) of object;

  ICRFEventMap = interface
    ['{B377BF36-8595-424C-9F90-761BF2CF2EFE}']
    { Accessors }
    function Get_EventsPerDay: integer;
    { Convert event number to date }
    function EventNoToDate( const AEventNum: integer ): TDateTime;
    function EventDateToNo( const AEventDateTime: TDateTime ): integer;
    { Properties }
    property EventsPerDay: integer read Get_EventsPerDay;
  end;

  ICRFItem = interface
    ['{457CB994-D9BC-42BA-9CC2-78059B144FF0}']
    { Property Accessors }
    function Get_AffectedItems: TStrings;
    function Get_ValidationState: TCRFItemValidationState;

    { Data getters }
    function Get_AsDateTime: TDateTime;
    function Get_AsDouble: double;
    function Get_AsInteger: integer;
    function Get_AsScore: double;
    function Get_AsString: string;
    function Get_Datapoints: integer;
    function Get_OptionText: string;
    function Get_OriginalValue: string;
    function Get_Text: string;
    function Get_Visible: boolean;
    function Get_WasValid: boolean;

    { Data setters }
    procedure Set_AsString( const AValue: string );
    procedure Set_AsDateTime( const AValue: TDateTime );
    procedure Set_AsDouble( const AValue: double );
    procedure Set_AsInteger( const AValue: integer );
    procedure Set_AsScore( const AValue: double );
    procedure Set_EditorText( const AValue: string );
    procedure Set_Visible( const AValue: boolean );

    { DbLink getters }
    function Get_ReadStatus: TCRFItemReadStatus;
    function Get_RowId: integer;

    { Metadata getters }
    function Get_Header: string;
    function Get_Question: string;
    function Get_Expression: string;
    function Get_EditHint: string;
    function Get_EditorText: string;
    function Get_EventTime: TDateTime;
    function Get_MaxValue: double;
    function Get_MinValue: double;

    function Get_ItemId: integer;
    function Get_ItemType: TCRFItemType;
    function Get_Visibility: TCrfItemVisibility;
    function Get_VisibilityChanged: boolean;
    function Get_MetaFormItem: ICRFMetaFormItem;
    function Get_VarName: string;

    function TryGetValue( const AVarName: string; var AValue: variant ): boolean;

    { Other members }
    function AsEnum: integer;
    function AsVariant: variant;
    function CanEdit: boolean;
    function ChangeCount: integer;
    function ClearStrategy: TCRFItemClearStrategy;
    function DataChanged: boolean;
    function FormList: TStrings;
    function GetOptionText( const AIntValue: integer ): string;
    function IsLocked: boolean;
    function IsNan: boolean;
    function Lock: boolean;
    function Lockable: boolean;
    function Locked: TCRFItemLockStatus;
    function NeedsLocking: boolean;
    function NeedsSaving: boolean;
    function ParseDate( const AValue: string ): TDateTime;
    function Recalc: boolean;
    function SetByEventId: integer;
    function SetByEventNo: integer;
    function Str( const ADataCol: TStrType ): string;
    function ValidateItem: boolean;
    function ValidValue( const AValue: double ): boolean;
    procedure AddDependentItem( const AVarName: string );
    procedure Clear( const ADataOnly: boolean );
    procedure ClearDbLinks;
    procedure IncrementDatapointCounter;
    procedure Normalize;
    procedure Reset;
    procedure SetCalcEvent( AOnCalc: TCRFItemCalculateEvent );
    procedure SetChangeTrigger( AEnabled: boolean );
    procedure SetOriginalStatus( const AReadStatus: TCRFItemReadStatus; const ALockStatus: TCRFItemLockStatus; const ARowId, AChangeCount: integer );
    procedure SetOriginalValue( const AValue: variant; const AComment: string; const AEventNum, AEventId, ARowId: integer );
    property AffectedItems: TStrings read Get_AffectedItems;
    { Read write (data) properties }
    property AsDateTime: TDateTime read Get_AsDateTime write Set_AsDateTime;
    property AsDouble: double read Get_AsDouble write Set_AsDouble;
    property AsInteger: integer read Get_AsInteger write Set_AsInteger;
    property AsScore: double read Get_AsScore write Set_AsScore;
    property AsString: string read Get_AsString write Set_AsString;

    { Read only properties }
    property Datapoints: integer read Get_Datapoints;
    property EditHint: string read Get_EditHint;
    property EventTime: TDateTime read Get_EventTime;
    property Expression: string read Get_Expression;
    property Header: string read Get_Header;
    property ItemId: integer read Get_ItemId;
    property ItemType: TCRFItemType read Get_ItemType;
    property Meta: ICRFMetaFormItem read Get_MetaFormItem;
    property MaxValue: double read Get_MaxValue;
    property MinValue: double read Get_MinValue;
    property OptionText: string read Get_OptionText;
    property OriginalValue: string read Get_OriginalValue;
    property EditorText: string read Get_EditorText write Set_EditorText;
    property Question: string read Get_Question;
    property ReadStatus: TCRFItemReadStatus read Get_ReadStatus;
    property RowId: integer read Get_RowId;
    property Text: string read Get_Text;
    property VarName: string read Get_VarName;
    property ValidationState: TCRFItemValidationState read Get_ValidationState;
    property VisibilityChanged: boolean read Get_VisibilityChanged;
    property Visible: boolean read Get_Visible write Set_Visible;
    property WasValid: boolean read Get_WasValid;
  end;

  ICRFItemObserver = interface
    ['{08CB2EBA-EE12-43E8-BAA3-9740F75F8288}']
    procedure AfterItemChange( const Sender: ICRFItem );
  end;

  ICRFForm = interface( ICRFMetaForm )
    ['{659F772B-B798-45F8-83C2-8ACF3421BEAC}']
    { Property Accessors }
    function Get_Actions: ICRFFormActionList;
    function Get_CachedText: string;
    function Get_ClinForm: ICRFClinForm;
    function Get_ClinFormId: integer;
    function Get_Comment: string;
    function Get_CommentEdited: boolean;
    function Get_Completeness: integer;
    function Get_CompletenessRequired: integer;
    function Get_Count: integer;
    function Get_CreatedBy: integer;
    function Get_EventId: integer;
    function Get_EventNum: integer;
    function Get_EventTime: TDateTime;
    function Get_FormStatus: TCRFFormStatus;
    function Get_IncludeNewerData: boolean;
    function Get_Item( AIndex: integer ): ICRFItem;
    function Get_Loading: boolean;
    function Get_OnItemChange: TCRFItemEvent;
    function Get_OnItemVisibility: TCRFItemEvent;
    function Get_Page( Index: integer ): ICRFMetaPage;
    function Get_RatingScale: boolean;
    function Get_SignedBy: integer;
    function Get_SuspendRecalc: boolean;
    procedure Set_Comment( const AValue: string );
    procedure Set_IncludeNewerData( const Value: boolean );
    procedure Set_OnItemChange( const Value: TCRFItemEvent );
    procedure Set_OnItemVisibility( const Value: TCRFItemEvent );
    procedure Set_SuspendRecalc( const AValue: boolean );
    { Other members }
    function AsText( AStrings: TStrings ): string;
    function AddInputItem( const AItemId: Integer; const AVarName: string; const AItemType: TCRFItemType ): ICRFItem;
    function AddPage( const APageNumber: integer ): ICRFMetaPage;
    function CanHaveComments: boolean;
    function HasComment: boolean;
    function TryGetItem( const AVarName: string; out AItem: ICRFItem ): boolean; overload;
    function TryGetItem( const AItemId: integer; out AItem: ICRFItem ): boolean; overload;
    function TryGetValue( const AVarName: string; var AValue: variant ): boolean;
    function GetParentPage( Page: ICRFMetaPage ): ICRFMetaPage;
    function IndexOf( AItem: ICRFItem ): integer;
    function IsEmpty: boolean;
    function IsSigned: boolean;
    function ItemWantsData( AItem: ICRFItem; const AEventNum: integer; const AEventTime: TDateTime ): boolean;
    function Lockable: boolean;
    function LockItems: integer;
    function PageCount: integer;
    function ValidateForm: boolean;
    function VisibleCount: integer;
    procedure ClearData;
    procedure Identify( AClinForm: ICRFClinForm );
    procedure Recalc;
    procedure RefreshStatus;
    procedure Reset;
    procedure BeginLoad( const ARowType: TCRFRowType );
    procedure EndLoad;
    { Properties }
    property Actions: ICRFFormActionList read Get_Actions;
    property CachedText: string read Get_CachedText;
    property ClinForm: ICRFClinForm read Get_ClinForm;
    property ClinFormId: integer read Get_ClinFormId;
    property Count: integer read Get_Count;
    property Comment: string read Get_Comment write Set_Comment;
    property CommentEdited: boolean read Get_CommentEdited;
    property Completeness: integer read Get_Completeness;
    property CompletenessRequired: integer read Get_CompletenessRequired;
    property CreatedBy: integer read Get_CreatedBy;
    property EventId: integer read Get_EventId;
    property EventNum: integer read Get_EventNum;
    property EventTime: TDateTime read Get_EventTime;
    property FormStatus: TCRFFormStatus read Get_FormStatus;
    property IncludeNewerData: boolean read Get_IncludeNewerData write Set_IncludeNewerData;
    property Items[AIndex: integer]: ICRFItem read Get_Item;
    property Name: string read Get_Name;
    property Loading: boolean read Get_Loading;
    property Page[Index: integer]: ICRFMetaPage read Get_Page;
    property RatingScale: boolean read Get_RatingScale;
    property SignedBy: integer read Get_SignedBy;
    property SuspendRecalc: boolean read Get_SuspendRecalc write Set_SuspendRecalc;
    { Events }
    property OnItemChange: TCRFItemEvent read Get_OnItemChange write Set_OnItemChange;
    property OnItemVisibility: TCRFItemEvent read Get_OnItemVisibility write Set_OnItemVisibility;
  end;

  ICRFFormStatusObserver = interface
    ['{C263B04E-E6EC-49BB-B069-429C1F1B0579}']
    procedure AfterFormStatusChange( const AForm: ICRFForm );
  end;

  ICRFFormLoader = interface
    ['{6F6149E5-9627-4EFD-B655-372CFA3D76FA}']
    { Other members }
    function Load( const AFormId: integer; const ACRFMetaForm: ICRFMetaForm; const AFormLocation: TCRFFormLocation = flUndefined ): boolean;
  end;

const
  FORM_STATUS_CHARS: array [TCRFFormStatus] of char = ( 'U', 'E', 'I', 'C', 'L' );
  SCALE_NAMES: array [TCRFItemType] of string       = ( 'UNDEF', 'QN', 'ORD', 'NOM', 'NAR', 'DATE', 'H1', 'CHECKLIST', 'MSG' );

function StrToItemType( Value: string ): TCRFItemType;

implementation

function StrToItemType( Value: string ): TCRFItemType;
begin
  Result := itUNDEF;
  if Value = '' then
    Result := lsORD
  else if Value = SCALE_NAMES[lsORD] then
    Result := lsORD
  else if Value = SCALE_NAMES[lsQN] then
    Result := lsQN
  else if Value = SCALE_NAMES[lsNAR] then
    Result := lsNAR
  else if Value = SCALE_NAMES[lsNOM] then
    Result := lsNOM
  else if Value = SCALE_NAMES[itDATE] then
    Result := itDATE
  else if Value = SCALE_NAMES[itCHECKLIST] then
    Result := itCHECKLIST
  else if Value = SCALE_NAMES[itH1] then
    Result := itH1
  else if Value = SCALE_NAMES[itMSG] then
    Result := itMSG;
end;

end.
