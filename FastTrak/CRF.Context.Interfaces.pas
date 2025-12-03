unit CRF.Context.Interfaces;

interface

uses
  CRF.Study.Interfaces;

type
  ICrfContext = interface( IStudyId ) ['{50732CEE-FFE4-4269-A8CF-F59C74983DB5}']
    { Property accessors }
    function Get_CaseId: integer;
    function Get_CenterId: integer;
    function Get_UserId: integer;
    { Properties }
    property CaseId: integer read Get_CaseId;
    property CenterId: integer read Get_CenterId;
    property UserId: integer read Get_UserId;
  end;

implementation

end.
