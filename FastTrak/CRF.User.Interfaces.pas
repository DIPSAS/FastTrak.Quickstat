unit CRF.User.Interfaces;

interface

uses
  {CRF}
  CRF.Context.Session.Interfaces,
  {General}
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces,
  Emetra.Interfaces.Observer,
  Emetra.Person.Interfaces;

type
  ICRFUserEducation = interface['{C8182BCD-C35A-4C80-BA0B-61A70D4433EA}']
    function HealthCareProfessionalAtCollegeLevel: boolean;
  end;

  ICRFUser = interface( IDatabaseUser )
    ['{9E411BFE-E9E4-4281-BD8C-E5F527533675}']
    { Property accessors }
    function Get_CaseList: integer;
    function Get_CenterId: integer;
    function Get_CenterName: string;
    function Get_GroupId: integer;
    function Get_GroupName: string;
    function Get_HPRNo: integer;
    function Get_ProfessionName: string;
    function Get_ProfType: string;
    function Get_ProfId: integer;
    function Get_Signature: string;
    procedure Set_GroupId( const AValue: integer );
    { Other members }
    function SelectGroup: integer;
    procedure MapToPerson( const APersonId: integer );
    procedure SelectCenter;
    procedure SelectProfession;
    procedure Update( APerson: IPersonReadOnly; APersonId: integer = 0 );
    { Properties }
    property CaseList: integer read Get_CaseList;
    property GroupId: integer read Get_GroupId write Set_GroupId;
    property GroupName: string read Get_GroupName;
    property CenterName: string read Get_CenterName;
    property CenterId: integer read Get_CenterId;
    property HPRNo: integer read Get_HPRNo;
    property ProfId: integer read Get_ProfId;
    property Profession: string read Get_ProfessionName;
    property ProfessionId: integer read Get_ProfId;
    property ProfessionType : string read Get_ProfType;
    property Signature: string read Get_Signature;
  end;

  ICRFActiveUser = interface( ICRFUser )
    ['{8B66C62F-F110-4F8C-85E8-118C61225C4C}']
    { Property accessors }
    function Get_DbOwner: boolean;
    function Get_GSM: string;
    function Get_Log: ILog;
    function Get_Password: string;
    function Get_RelationCount: integer;
    function Get_SQL: ISQL;
    function Get_ShowMyGroup: boolean;
    function Get_StudyId: integer;
    function Get_StudyContext: IStudyContext;
    function Get_Superuser: boolean;
    procedure Set_ShowMyGroup( const Value: boolean );
    procedure Set_GSM( const Value: string );
    procedure Set_HPRNo( const Value: integer );
    { Other members }
    function AddMyself( const APerson: IPersonReadOnly ): integer;
    function ChangePassword( const ANewPassword: string ): boolean;
    function SetCenter( const ANewCenterId: integer ): boolean;
    procedure Attach( AObserver: IListener );
    procedure SetCredentials( const AUser, APassword: string );
    { Properties }
    property DbOwner: boolean read Get_DbOwner;
    property GSM: string read Get_GSM write Set_GSM;
    property HPRNo: integer read Get_HPRNo write Set_HPRNo;
    property Log: ILog read Get_Log;
    property Password: string read Get_Password;
    property RelationCount: integer read Get_RelationCount;
    property ShowMyGroup: boolean read Get_ShowMyGroup write Set_ShowMyGroup;
    property SQL: ISQL read Get_SQL;
    property StudyId: integer read Get_StudyId;
    property StudyContext: IStudyContext read Get_StudyContext;
    property Superuser: boolean read Get_Superuser;
  end;

implementation

end.
