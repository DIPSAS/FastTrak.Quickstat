unit VMR.User.Interfaces;

interface

type
  IVmrUser = interface['{B87C503B-E2C7-4E34-B21C-BA16B92CA157}']
    { Property accessors }
    function Get_FullName: string;
    function Get_ProfName: string;
    function Get_ProfType: string;
    function Get_Signature: string;
    function Get_UserId: integer;
    function Get_UserName: string;
    { Properties }
    property FullName: string read Get_FullName;
    property ProfName: string read Get_ProfName;
    property ProfType: string read Get_ProfType;
    property Signature: string read Get_Signature;
    property UserId: integer read Get_UserId;
    property UserName: string read Get_UserName;
  end;

  IVmrUserList = interface['{2F9C163F-4115-442E-96D7-FDAC34C5429E}']
    function TryGetUser( const AUserId: integer; out ASystemUser: IVmrUser ): boolean;
  end;

implementation

end.
