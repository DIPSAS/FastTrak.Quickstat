unit CRF.ClinForm.Interfaces;

interface

uses
  {Standard}
  Db;

type
  { Edit status of a clin form }
  TCRFFormStatus = ( fsUndefined, fsEmpty, fsIncomplete, fsComplete, fsLocked );

  ICRFClinForm = interface
    ['{403428F9-B003-4995-95A2-9EA25D944A61}']
    { Property Accessors }
    function Get_Archived: boolean;
    function Get_CachedText: string;
    function Get_ClinFormId: integer;
    function Get_Comment: string;
    function Get_Completeness: integer;
    function Get_CreatedAt: TDateTime;
    function Get_CreatedBy: integer;
    function Get_CreatedByProfId: integer;
    function Get_CreatedBySign: string;
    function Get_EventId: integer;
    function Get_EventNum: integer;
    function Get_EventTime: TDateTime;
    function Get_FormId: integer;
    function Get_FormStatus: TCRFFormStatus;
    function Get_FormTitle: string;
    function Get_Name: string;
    function Get_SignedAt: TDateTime;
    function Get_SignedBy: integer;
    function Get_SignedByProfId: integer;
    function Get_SignedBySign: string;
    function Get_UserHasEditPrivileges: boolean;
    procedure Set_Archived( const AValue: boolean );
    procedure Set_CachedText( const AValue: string );
    { Other members }
    function AsListbox( const AOneLiner: boolean = true ): string;
    function HasComment: boolean;
    function IsSigned: boolean;
    function IsRecent: boolean;
    procedure Load( ADataset: TDataset );
    procedure UpdateStatus( const AFormStatus: TCRFFormStatus; const ACompleteness, ACompletenessRequired: integer; const AStatusText: string );
    procedure UpdateComment( const AValue: string );
    { Properties }
    property Archived: boolean read Get_Archived write Set_Archived;
    property CachedText: string read Get_CachedText write Set_CachedText;
    property ClinFormId: integer read Get_ClinFormId;
    property Comment: string read Get_Comment;
    property CreatedAt: TDateTime read Get_CreatedAt;
    property CreatedBy: integer read Get_CreatedBy;
    property CreatedByProfId: integer read Get_CreatedByProfId;
    property CreatedBySign: string read Get_CreatedBySign;
    property EventId: integer read Get_EventId;
    property EventNum: integer read Get_EventNum;
    property EventTime: TDateTime read Get_EventTime;
    property FormComplete: integer read Get_Completeness;
    property FormId: integer read Get_FormId;
    property FormName: string read Get_Name;
    property FormStatus: TCRFFormStatus read Get_FormStatus;
    property FormTitle: string read Get_FormTitle;
    property Name: string read Get_Name;
    property SignedAt: TDateTime read Get_SignedAt;
    property SignedBy: integer read Get_SignedBy;
    property SignedByProfId: integer read Get_SignedByProfId;
    property SignedBySign: string read Get_SignedBySign;
    property UserHasEditPrivileges: boolean read Get_UserHasEditPrivileges;
  end;

  ICRFSelectedClinForm = interface
    ['{787650E0-4EF0-4E98-A624-C3076FC6C908}']
    { Property accessors }
    function Get_SelectedClinForm: ICRFClinForm;
    { Other methods }
    function AnythingSelected: boolean;
    { Properties }
    property SelectedClinForm: ICRFClinForm read Get_SelectedClinForm;
  end;

  ICRFAfterSaveEvent = interface
    ['{17548565-5935-4B22-84A6-EDB9AC3EF9A0}']
    function TryGetProcedure( const AFormId: integer; out AProcName: string ): boolean;
  end;


function StrToStatus( const s: string ): TCRFFormStatus;

implementation

function StrToStatus( const s: string ): TCRFFormStatus;
begin
  Result := fsUndefined;
  if Length( s ) > 0 then
    case s[1] of
      'U', 'u': Result := fsUndefined;
      'E', 'e': Result := fsEmpty;
      'I', 'i': Result := fsIncomplete;
      'C', 'c': Result := fsComplete;
      'L', 'l': Result := fsLocked;
    end;
end;

end.
