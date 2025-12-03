unit CRF.Context.Session.Interfaces;

interface

uses
  CRF.Study.Interfaces,
  Classes;

type
  /// <summary>
  ///   Interface that reflects the login session in a <b>FastTrak</b>
  ///   database. Every session (see table dbo.UserLog) is related to a
  ///   specific <b>Study</b> (table dbo.Study). The <b>SessId</b> is the
  ///   primary key in the table, and is an autoincremented integer.
  /// </summary>
  ISessId = interface
    ['{67D8A2BA-A686-4409-812C-C4AC2471F7C1}']
    { Accessors }
    function Get_SessId: integer;
    { Other members }
    property SessId: integer read Get_SessId;
  end;

  /// <summary>
  ///   Merges functionaly from the <b>ISessId</b> and <b>IStudyId</b>
  ///   interfaces.
  /// </summary>
  IStudySession = interface( IStudyId )
    ['{286FD480-B70D-4A05-B45A-2D6F07F71D68}']
    { Accessors }
    function Get_SessId: integer;
    { Other members }
    property SessId: integer read Get_SessId;
  end;

  /// <summary>
  ///   A decorated version of the IStudyId interface, that gives access to the
  ///   <b>StudyName</b>, and to change the same. Also allows a client to
  ///   increment the counters updates and inserts. This allows the <b>UserLog</b>
  ///    table to include information about the volume of edits in a given
  ///   session, finding very active/productive sessions.
  /// </summary>
  IStudyContext = interface( IStudyId )
    ['{63DBE42C-F8AD-43F2-983D-ED76C8A19455}']
    { Accessors }
    function Get_StudyName: string;
    function Get_StudyId: integer;
    function Get_SessId: integer;
    procedure Set_StudyId( const AValue: integer );
    { Other members }
    procedure AddStudyObserver( AStudyObserver: IStudyObserver );
    procedure IncrementUpdates;
    procedure IncrementInserts;
    procedure SetStudyName( const AProtocol: string ); overload;
    { Properties }
    property Protocol: string read Get_StudyName;
    property SessId: integer read Get_SessId;
    property StudyId: integer read Get_StudyId write Set_StudyId;
    property StudyName: string read Get_StudyName;
  end;

  /// <summary>
  ///   Allows passing the user/password in an object. Can be used where
  ///   credentials need to be saved for passing to subsystems when SQL Logins
  ///   are use, e.g. <b>FastReports</b> or external utilities.
  /// </summary>
  IStudyLoginContext = interface( IStudySession )
    ['{74E15710-8A6C-4DCC-8001-3500957E32AD}']
    { Other members }
    procedure SetContext( const AUser, APassword, AProtocol: string ); overload;
  end;

  /// <summary>
  /// A version of the study context that gives access to custom folders on
  /// the filesystem where <b>FastTrak.exe</b> resides, including folders
  /// that are shared between <b>StudyContexts</b> (protocols).
  /// </summary>
  IStudyFileSystemContext = interface( IStudyContext )
    ['{3AC97977-1955-4550-9E65-99E6D6F5B578}']
    { Accessors }
    function Get_Path: string;
    function Get_Root: string;
    function Get_UDL: string;
    procedure Set_Root( const ARootDir: string );
    { Other members }
    function WebRoot: string;
    function PatientDir( const AShared: Boolean = false; const AOnWeb: Boolean = false ): string;
    function PopulationDir( const AShared: Boolean = false; const AOnWeb: Boolean = false ): string;
    function ProtocolDir( const AOnWeb: Boolean = false ): string;
    function FileRoot: string;
    { Properties }
    property Path: string read Get_Path;
    property Root: string read Get_Root write Set_Root;
    property UDL: string read Get_UDL;
  end;

  IStudyCenterContext = interface
    ['{5E9D5FC7-04DB-453E-9254-76D4B0AA120A}']
    { Accessors }
    function Get_CenterId: integer;
    function Get_CenterName: string;
    { Other members }
    property CenterId: integer read Get_CenterId;
    property CenterName: string read Get_CenterName;
  end;

  IStudyCenterObserver = interface
    ['{7D541E82-8074-4CE9-85CB-FD23CABEB5D6}']
    procedure AfterCenterChange( Sender: IStudyCenterContext );
  end;

implementation

end.
