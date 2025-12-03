unit Emetra.Interfaces.List;

interface

uses
  Contnrs;

type
  IObjectList = interface ['{48F48709-149A-4FD6-AD47-B2D1FEAACCAA}']
    { Property accessors }
    function Get_Count: integer;
    function GetItem( AIndex: integer ): TObject;
    { Other members }
    property Count: integer read Get_Count;
    property Items[AIndex: integer]: TObject read GetItem; default;
  end;

  TObjectListWithInterface = class( TObjectList, IInterface, IObjectList )
  protected
    function _AddRef: Integer; stdcall;
    function _Release: integer; stdcall;
    function QueryInterface(const IID: TGUID; out Obj): HResult; virtual; stdcall;
    function Get_Count: integer;
  end;

implementation

function TObjectListWithInterface._AddRef: Integer;
begin
  Result := -1;
end;

function TObjectListWithInterface._Release: Integer;
begin
  Result := -1;
end;

function TObjectListWithInterface.Get_Count: integer;
begin
  Result := inherited Count;
end;

function TObjectListWithInterface.QueryInterface(const IID: TGUID; out Obj): HResult;
const
  E_NOINTERFACE = HResult($80004002);
begin
  if GetInterface(IID, Obj) then Result := 0 else Result := E_NOINTERFACE;
end;

end.
