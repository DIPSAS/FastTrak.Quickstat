unit CRF.Person.StudyCase.Interfaces;

interface

uses
  VMR.Patient.Interfaces,
  {General}
  Emetra.Person.Interfaces,
  {RTL}
  Classes;

type
  ICRFSelectCase = interface
    ['{2C5B0189-BAEA-484B-9587-5C1968174CF9}']
    function Select( const APersonId: integer ): Boolean;
  end;

  ICRFStudyCase = interface( IPatient )
    ['{4F87B329-11F9-4565-B530-E580AB701168}']
    { Property Accessors }
    function Get_CenterId: integer;
    function Get_CenterName: string;
    function Get_ClinRelId: integer;
    function Get_EmployeeNumber: integer;
    function Get_IsTestCase: Boolean;
    function Get_GroupId: integer;
    function Get_GroupName: string;
    function Get_HPRNo: integer;
    function Get_RelName: string;
    function Get_PhoneNumber: string;
    function Get_Pronoun: string;
    function Get_PronounObjective: string;
    function Get_RelId: integer;
    function Get_StatusId: integer;
    function Get_StatusText: string;
    function Get_StudyId: integer;
    procedure Set_GroupId( const AValue: integer );
    procedure Set_StatusId( const AValue: integer );
    procedure Set_IsTestCase( const AValue: Boolean );
    { Other members }
    function AddToStudy( const AStudyId: integer; APersonId: integer ): Boolean;
    function SelectGroup: integer;
    function SelectStatus: integer;
    function TryTransfer( const AGroupId, AStatusId: integer ): boolean;
    procedure Adopt( APersonId: integer = 0 );
    procedure UpdateNationalId( const ANationalId: string; APersonId: integer = 0 );
    { Properties }
    property CenterId: integer read Get_CenterId;
    property CenterName: string read Get_CenterName;
    property ClinRelId: integer read Get_ClinRelId;
    property EmployeeNumber: integer read Get_EmployeeNumber;
    property GroupId: integer read Get_GroupId write Set_GroupId;
    property GroupName: string read Get_GroupName;
    property HPRNo: integer read Get_HPRNo;
    property IsTestCase: Boolean read Get_IsTestCase write Set_IsTestCase;
    property Pronoun: string read Get_Pronoun;
    property PronounObjective: string read Get_PronounObjective;
    property RelId: integer read Get_RelId;
    property RelName: string read Get_RelName;
    property PhoneNumber: string read Get_PhoneNumber;
    property StatusId: integer read Get_StatusId write Set_StatusId;
    property StatusText: string read Get_StatusText;
    property StudyId: integer read Get_StudyId;
  end;

  IActiveStudyCase = interface( ICRFStudyCase )
    ['{1AAA9085-B435-4886-B702-2D10895893C7}']
    { Property accessors }
    function Get_JournalansvarligNavn: string;
    function Get_Journalansvarlig: integer;
    { Other members }
    function Select( const APersonId: integer ): Boolean;
    function SelectRelation: integer;
    function ValidGroup: Boolean;
    function ValidRelation: Boolean;
    procedure AddActiveCaseObserver( AObject: TObject );
    procedure RemoveActiveCaseObserver( AObject: TObject );
    procedure BeginEdit;
    procedure EndEdit;
    procedure AssumeJournalansvar;
    procedure Touch;
    procedure Update( APerson: IPersonReadOnly; APersonId: integer = 0 );
    procedure UpdateGroup;
    procedure UpdateRelation;
    { Properties }
    property Journalansvarlig: integer read Get_Journalansvarlig;
    property JournalansvarligNavn: string read Get_JournalansvarligNavn;
  end;

  IStudyCaseAccessControl = interface
    ['{D6A2A9F1-BBAC-4051-AE74-8079304A9190}']
    { Property accessors }
    function Get_IgnoreLocation: Boolean;
    procedure Set_IgnoreLocation( const AValue: Boolean );
    function Get_IgnoreRelation: Boolean;
    procedure Set_IgnoreRelation( const AValue: Boolean );
    function Get_IgnoreBlocking: Boolean;
    procedure Set_IgnoreBlocking( const AValue: Boolean );
    { Other members }
    function TryGetAccess( const AStudyCase: IActiveStudyCase ): Boolean;
    { Properties }
    property IgnoreBlocking: Boolean read Get_IgnoreBlocking write Set_IgnoreBlocking;
    property IgnoreLocation: Boolean read Get_IgnoreLocation write Set_IgnoreLocation;
    property IgnoreRelation: Boolean read Get_IgnoreRelation write Set_IgnoreRelation;
  end;

implementation

end.
