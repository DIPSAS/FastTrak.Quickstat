unit Emetra.AccessControl.Interfaces;

{$M+}

interface

uses
  System.SysUtils,
  Spring.Collections;

type
  /// <summary>
  /// Id to a function-point. Can be any human readable string.
  /// </summary>
  TFunctionPointId = string;

  TAccessState = ( asUnknown, asGranted, asDenied );

  /// <summary>
  /// A function-point.
  /// </summary>
  IFunctionPoint = interface
    ['{43A0F330-11C9-4CAA-A3C0-99E90A4333B5}']

    /// <summary>
    /// Getter for property Id
    /// </summary>
    function Get_Id: TFunctionPointId;

    /// <summary>
    /// Getter for property DefaultState
    /// </summary>
    function Get_DefaultState: TAccessState;

    /// <summary>
    /// Setter for DefaultState
    /// </summary>
    procedure Set_DefaultState( const ADefaultState: TAccessState );

    /// <summary>
    /// The id (and also the name) of the function-point.
    /// </summary>
    property Id: TFunctionPointId read Get_Id;

    /// <summary>
    /// The default-state - this is returned for a role/profession which are
    /// not defined for the function.
    /// </summary>
    property DefaultState: TAccessState read Get_DefaultState write Set_DefaultState;

    /// <summary>
    /// Given a profession and a list of roles return the access-state. If
    /// there is no state defined for the given profession/roles DefaultState
    /// is returned.
    /// </summary>
    function GetAccessState( const AProfession: string; const ARoles: IList<string> ): TAccessState;

    /// <summary>
    /// Set state for the given profession.
    /// </summary>
    procedure SetProfessionAccess( const AProfession: string; const AState: TAccessState );

    /// <summary>
    /// Set state for the given role.
    /// </summary>
    procedure SetRoleAccess( const ARole: string; const AState: TAccessState );

    function Roles: IDictionary<string, TAccessState>;

    function Professions: IDictionary<string, TAccessState>;
    function AsString: string;
  end;

  /// <summary>
  /// Contains all database-methods used by access-control functions.
  /// </summary>
  IAccessControlDb = interface
    ['{B072CD27-AC62-4759-B4AC-5D859E230A53}']
    /// <summary>
    /// Get the database roles I have.
    /// </summary>
    function GetMyRoles: IList<string>;
    function GetFunctionPoints: IList<IFunctionPoint>;
  end;

  /// <summary>
  /// Service used to grant/deny access given a valid functionpoint.
  /// </summary>
  IAccessControl = interface
    ['{972871B1-90C1-49A5-A284-DF819C2605C8}']
    /// <summary>
    /// Return true if I have access (i.e. have state asGranted) to the given
    /// function.
    /// </summary>
    function TryGetAccess( const AFunctionPoint: TFunctionPointId ): boolean;
  end;

  IAccessControlManager = interface;

  /// <summary>
  /// IAccessControlObserver can be implemented by a software object to
  /// adjust itself if access rights change during program execution
  /// </summary>
  IAccessControlObserver = interface
    ['{87050969-6181-425B-9AE8-3D236F14E924}']
    procedure AfterAccessControlChanged( const Sender: IAccessControl );
    procedure RegisterAsAccessControlObserver( const AManager: IAccessControlManager );
  end;

  /// <summary>
  /// IAccessControlManager allows administration of access control to
  /// various function points <br />
  /// </summary>
  IAccessControlManager = interface
    ['{612DC222-BD07-4373-BD56-5BF6D92F0E03}']
    procedure AddFunctionPoints( const AFunctionPointList: IList<IFunctionPoint> );
    /// <summary>
    /// Used for adding a function-point.
    /// </summary>
    /// <param name="AFunctionPoint">
    /// Id (i.e. a human readable string indicating the function)
    /// </param>
    /// <param name="ADefaultState">
    /// If a role/profession is check, and not added, this state will be
    /// returned.
    /// </param>
    procedure AddFunctionPoint( const AFunctionPoint: TFunctionPointId; const ADefaultState: TAccessState );
    /// <summary>
    /// Deny access to a given function-point for a given profession
    /// </summary>
    /// <param name="AFunctionPoint">
    /// Id (i.e. a human readable string indicating the function)
    /// </param>
    /// <param name="AProfType">
    /// Profession-type. This should be one of the professions defined in
    /// table dbo.MetaProfession
    /// </param>
    procedure DenyAccessToProfession( const AFunctionPoint: TFunctionPointId; const AProfType: string );
    /// <summary>
    /// Deny access to a given functionpoint for a given role
    /// </summary>
    /// <param name="AFunctionPoint">
    /// Id (i.e. a human readable string indicating the function)
    /// </param>
    /// <param name="ARoleName">
    /// A database-role, defined in sqlserver
    /// </param>
    procedure DenyAccessToDatabaseRole( const AFunctionPoint: TFunctionPointId; const ARoleName: string );
    /// <summary>
    /// Grant access to a given function-point for a given profession
    /// </summary>
    /// <param name="AFunctionPoint">
    /// Id (i.e. a human readable string indicating the function)
    /// </param>
    /// <param name="AProfType">
    /// A profession (shortcode, i.e. 'LE' or lege etc)
    /// </param>
    procedure GrantAccessToProfession( const AFunctionPoint: TFunctionPointId; const AProfType: string );
    /// <summary>
    /// Grant access to a given function-point for a given role
    /// </summary>
    /// <param name="AFunctionPoint">
    /// Id (i.e. a human readable string indicating the function)
    /// </param>
    /// <param name="ARoleName">
    /// A database role
    /// </param>
    procedure GrantAccessToDatabaseRole( const AFunctionPoint: TFunctionPointId; const ARoleName: string );
    /// <summary>
    /// This method is used to register clients. A client can register itself
    /// to a given functionpoint, with a default state. If the functionpoint
    /// already exists the given state must be the same as the current
    /// default state.
    /// </summary>
    /// <param name="AFunctionPoint">
    /// Id (i.e. a human readable string indicating the function)
    /// </param>
    /// <exception cref="EInvalidDefaultAccessState">
    /// If a function-point already exists, but the default state differs
    /// </exception>
    procedure RegisterClient( const AFunctionPoint: TFunctionPointId; const AAccessState: TAccessState; const AAccessControlObserver: IAccessControlObserver );
    /// <summary>
    /// The unregistration of client should start when the program shuts down, before the clients are destroyed.
    /// </summary>
    procedure UnregisterAllClients;
  end;

const
  ACCESS_STATE_NAMES: array [TAccessState] of string = ( 'UNKNOWN', 'GRANT', 'DENY' );

implementation

end.
