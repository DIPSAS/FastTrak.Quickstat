unit CRF.Meta.Page.Interfaces;

interface

uses
  CRF.Meta.Interfaces,
  Db;

type
  ICRFMetaPage = interface( ICRFClickable )
    ['{531EE30E-F2DE-41C2-AD15-AB40F53A02BA}']
    { Property Accessors }
    function Get_Expanded: boolean;
    function Get_FormId: integer;
    function Get_PageId: integer;
    function Get_PageTitle: string;
    function Get_PageIntroduction: string;
    function Get_PageNumber: integer;
    function Get_Shown: boolean;
    procedure Set_Expanded( const Value: boolean );
    procedure Set_PageTitle( const Value: string );
    procedure Set_PageIntroduction( const Value: string );
    procedure Set_PageNumber( const Value: integer );
    { Other members }
    procedure Load( ADataset: TDataset );
    { Properties }
    property Expanded: boolean read Get_Expanded write Set_Expanded;
    property FormId: integer read Get_FormId;
    property PageId: integer read Get_PageId;
    property PageIntroduction: string read Get_PageIntroduction write Set_PageIntroduction;
    property PageNumber: integer read Get_PageNumber write Set_PageNumber;
    property PageTitle: string read Get_PageTitle write Set_PageTitle;
    property Shown: boolean read Get_Shown;
  end;

  ICRFMetaPageList = interface
    ['{41860FA4-8899-47AD-A0DD-7B306263C0EB}']
    { Property Accessors }
    function Get_Count: integer;
    function Get_FormId: integer;
    function Get_Page( AIndex: integer ): ICRFMetaPage;
    procedure Set_FormId( const AFormId: integer );
    { Other members }
    function GetParentPage( const APage: ICRFMetaPage ): ICRFMetaPage; overload;
    function GetParentPage( const APageNumber: integer ): ICRFMetaPage; overload;
    function IndexOf( const APage: ICRFMetaPage ): integer;
    function TryGetOrCreatePage( const APageNumber: integer; out APage: ICRFMetaPage ): boolean;
    function TryGetPage( const APageNumber: integer; out APage: ICRFMetaPage ): boolean;
    procedure Clear;
    { Properties }
    property Page[AIndex: integer]: ICRFMetaPage read Get_Page; default;
    property Count: integer read Get_Count;
    property FormId: integer read Get_FormId write Set_FormId;
  end;

implementation

end.
