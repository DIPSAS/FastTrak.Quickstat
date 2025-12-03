unit Emetra.Classes.SparseArray;

interface

uses
  System.Classes, 
  System.SysUtils, 
  System.Generics.Collections;

type
  TSparseObjectGrid = class( TObject )
  strict private
    fCells: TObjectDictionary<string, TObject>;
  private
    function GetCell( ACol, ARow: integer ): TObject;
    procedure SetCell( ACol, ARow: integer; const AValue: TObject );
  public
    { Initialization }
    constructor Create;
    destructor Destroy; override;
    procedure Clear;
    { Properties }
    property Cells[Col, Row: integer]: TObject read GetCell write SetCell; default;
  end;

implementation

const
  cFmtDims = '%d:%d';

constructor TSparseObjectGrid.Create;
begin
  inherited Create;
  FCells := TObjectDictionary<string, TObject>.Create;
end;

destructor TSparseObjectGrid.Destroy;
begin
  FCells.Free;
  inherited;
end;

procedure TSparseObjectGrid.Clear;
begin
  FCells.Clear;
end;

function TSparseObjectGrid.GetCell( ACol, ARow: integer ): TObject;
begin
  if not fCells.TryGetValue( Format( cFmtDims, [ACol, ARow] ), Result ) then
    Result := nil;
end;

procedure TSparseObjectGrid.SetCell( ACol, ARow: integer; const AValue: TObject );
begin
  fCells.AddOrSetValue( Format( cFmtDims, [ACol, ARow] ), AValue );
end;

end.
