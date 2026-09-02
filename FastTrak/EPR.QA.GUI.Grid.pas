unit EPR.QA.GUI.Grid;

interface

uses
  EPR.QA.Matrix.Interfaces,
  {General}
  Emetra.VclUtil.ColorSet.Interfaces,
  Emetra.Classes.SparseArray,
  {Standard}
  Graphics, Grids, Controls, Classes, Windows, Generics.Collections;

type
  TPersonGrid = class( TCustomDrawGrid, IPersonGridComponent )
  strict private
    fCurrentRow: integer;
    FObjects: TSparseObjectGrid;
    FDefaultNameColWidth: integer;
    FDefaultNationalIdColWidth: integer;
    FDefaultIdColWidth: integer;
    FDefaultDobColWidth: integer;
    fCurrentCellColor: TColor;
    fCurrentRowColor: TColor;
  protected
    { Property accessors }
    function Get_Anonymous: Boolean;
    function Get_DataColWidth: integer;
    function Get_Canvas: TCanvas;
    function Get_Col: longint;
    function Get_ColCount: longint;
    function Get_Row: longint;
    function Get_RowCount: longint;
    function Get_DataCols: longint;
    function Get_DataRows: longint;
    function Get_FixedCols: integer;
    function Get_FixedRows: integer;
    function Get_DefaultRowHeight: integer;
    function Get_Left: integer;
    function Get_Top: integer;
    procedure Set_Anonymous( const Value: Boolean );
    procedure Set_DataColWidth( const Value: integer );
    procedure Set_DataCols( const AValue: longint );
    procedure Set_DataRows( const AValue: longint );
    { Other members }
    function IsTextColumn( const ACol: integer ): Boolean;
    function GridToDataRow( const ARow: longint ): longint;
    function GridToDataCol( const ACol: longint ): longint;
    function TryGetObject( const ACol, ARow: longint; out AObject: TObject ): Boolean;
    procedure HandleSelect( Sender: TObject; ACol, ARow: longint; var CanSelect: Boolean );
    procedure HandleFixedClick( Sender: TObject; ACol, ARow: longint );
    procedure Home;
    procedure RepaintRow( const ARow: longint );
    procedure SetObject( ACol, ARow: longint; AObject: TObject );
    procedure SetColWidth( const ACol: longint; const AWidth: integer );
    procedure SetDefaultWidths( Sender: TObject );
  public
    { Initialization }
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
    procedure Clear;
    procedure Prepare( AParent: TWinControl; const ALayout: TAlign );
    { Other members }
    function GetFixedHeader( const ACol: longint ): string;
    function GetFixedFields( const ACol: longint; ARow: IPersonGridRow ): string;
    function IsDataRow( const ARow: longint ): Boolean;
    function IsDataCol( const ACol: longint ): Boolean;
    procedure Adjust( const ACol: longint; const AWidth: integer );
  published
    property Anonymous: Boolean read Get_Anonymous write Set_Anonymous;
    property CurrentCellColor: TColor read fCurrentCellColor write fCurrentCellColor;
    property CurrentRowColor: TColor read fCurrentRowColor write fCurrentRowColor;
    property CurrentRow: integer read fCurrentRow;
    property DataColWidth: integer read Get_DataColWidth write Set_DataColWidth;
    property DefaultIdColWidth: integer read FDefaultIdColWidth write FDefaultIdColWidth;
    property DefaultNameColWidth: integer read FDefaultNameColWidth write FDefaultNameColWidth;
    property DefaultNationalIdColWidth: integer read FDefaultNationalIdColWidth write FDefaultNationalIdColWidth;
    property DefaultDobColWidth: integer read FDefaultDobColWidth write FDefaultDobColWidth;
    { Exposed properties }
    property DefaultRowHeight;
    property FixedColor;
    property PopupMenu;
    property RowCount;
    property ColCount;
    { Events }
    property OnDrawCell;
    property OnDblClick;
    property OnClick;
    property OnFixedCellClick;
  end;

implementation

uses
  Forms, Math, SysUtils;

{ TPersonGrid }

{$REGION 'Initialization'}

constructor TPersonGrid.Create( AOwner: TComponent );
begin
  inherited;
  fCurrentRow := -1;
  ColCount := 10;
  FixedRows := 1;
  FixedCols := COL_PERSON_NAME + 1;
  FObjects := TSparseObjectGrid.Create;
  BorderStyle := bsNone;
  fCurrentCellColor := clFocusedSelectionColor;
  fCurrentRowColor := clUnfocusedSelectionColor;
  DoubleBuffered := true;
  DrawingStyle := gdsClassic;
  DefaultDrawing := false;
  DefaultRowHeight := 17;
  DefaultColWidth := 64;
  RowHeights[0] := 18;
  Clear;
  FDefaultIdColWidth := 44;
  FDefaultNameColWidth := 128;
  FDefaultDobColWidth := 64;
  FDefaultNationalIdColWidth := 84;
  Options := Options + [goColSizing, goFixedRowClick, goFixedColClick] - [goFixedVertLine, goRowSelect];
  OnSelectCell := HandleSelect;
  OnFixedCellClick := HandleFixedClick;
  SetDefaultWidths( Self );
end;

destructor TPersonGrid.Destroy;
begin
  FreeAndNil( FObjects );
  inherited;
end;

procedure TPersonGrid.RepaintRow( const ARow: integer );
var
  colIndex: integer;
begin
  { Workaround for bug in InvalidateRow }
  for colIndex := 0 to ColCount - 1 do
    InvalidateCell( colIndex, ARow );
end;

procedure TPersonGrid.Prepare( AParent: TWinControl; const ALayout: TAlign );
begin
  Align := ALayout;
  Parent := AParent;
end;

{$ENDREGION}

procedure TPersonGrid.HandleFixedClick( Sender: TObject; ACol, ARow: longint );
begin
  if ARow >= FixedRows then
    Row := ARow;
end;

procedure TPersonGrid.HandleSelect( Sender: TObject; ACol, ARow: integer; var CanSelect: Boolean );
var
  oldCurrentRow: integer;
begin
  if ARow <> fCurrentRow then
  begin
    oldCurrentRow := fCurrentRow;
    fCurrentRow := ARow;
    if ( oldCurrentRow > 0 ) and ( oldCurrentRow < RowCount ) then
      RepaintRow( oldCurrentRow );
    RepaintRow( fCurrentRow );
  end;
  CanSelect := true;
end;

procedure TPersonGrid.Adjust( const ACol, AWidth: integer );
begin
  if ( AWidth > ColWidths[ACol] ) then
    ColWidths[ACol] := AWidth;
end;

procedure TPersonGrid.Clear;
begin
  ColCount := 1 + FixedCols;
  RowCount := 1 + FixedRows;
  FObjects.Clear;
end;

function TPersonGrid.GetFixedFields( const ACol: integer; ARow: IPersonGridRow ): string;
begin
  case ACol of
    COL_PERSON_ID: Result := IntToStr( ARow.PersonId );
    COL_PERSON_DOB: Result := DateToStr( ARow.DOB );
    COL_PERSON_NATIONAL_ID: Result := ARow.NationalId;
    COL_PERSON_NAME: Result := ARow.FullName;
  else Result := EmptyStr;
  end;
end;

function TPersonGrid.GetFixedHeader( const ACol: integer ): string;
begin
  case ACol of
    COL_PERSON_ID: Result := HDR_PID;
    COL_PERSON_DOB: Result := HDR_BORN;
    COL_PERSON_NATIONAL_ID: Result := HDR_NATIONAL_ID;
    COL_PERSON_NAME: Result := HDR_NAME;
  else Result := EmptyStr;
  end;
end;

function TPersonGrid.Get_Anonymous: Boolean;
begin
  Result := ColWidths[COL_PERSON_NAME] < 0;
end;

function TPersonGrid.Get_Canvas: TCanvas;
begin
  Result := Canvas;
end;

function TPersonGrid.Get_Col: longint;
begin
  Result := Col;
end;

function TPersonGrid.Get_ColCount: longint;
begin
  Result := ColCount;
end;

function TPersonGrid.Get_DataColWidth: integer;
begin
  Result := DefaultColWidth;
end;

function TPersonGrid.Get_DefaultRowHeight: integer;
begin
  Result := DefaultRowHeight;
end;

function TPersonGrid.Get_FixedCols: integer;
begin
  Result := inherited FixedCols;
end;

function TPersonGrid.Get_DataCols;
begin
  Result := ColCount - FixedCols;
end;

function TPersonGrid.Get_DataRows;
begin
  Result := RowCount - FixedRows;
end;

function TPersonGrid.Get_FixedRows: integer;
begin
  Result := inherited FixedRows;
end;

function TPersonGrid.Get_Left: integer;
begin
  Result := Left;
end;

function TPersonGrid.Get_Row: longint;
begin
  Result := Row;
end;

function TPersonGrid.Get_RowCount: longint;
begin
  Result := RowCount;
end;

function TPersonGrid.Get_Top: integer;
begin
  Result := Top;
end;

function TPersonGrid.GridToDataCol( const ACol: integer ): longint;
begin
  Result := ACol - FixedCols;
end;

function TPersonGrid.GridToDataRow( const ARow: integer ): longint;
begin
  Result := ARow - FixedRows;
end;

procedure TPersonGrid.Home;
begin
  Self.HideEditor;
  Self.Options := Self.Options + [goDrawFocusSelected];
end;

function TPersonGrid.IsTextColumn( const ACol: integer ): Boolean;
begin
  Result := ( ACol = COL_PERSON_DOB ) or ( ACol = COL_PERSON_NAME ) or ( ACol = COL_PERSON_NATIONAL_ID );
end;

function TPersonGrid.IsDataCol( const ACol: integer ): Boolean;
begin
  Result := ( ACol >= FixedCols ) and ( ACol < ColCount );
end;

procedure TPersonGrid.SetDefaultWidths;
begin
  ColWidths[COL_PERSON_ID] := FDefaultIdColWidth;
  ColWidths[COL_PERSON_DOB] := FDefaultDobColWidth;
  ColWidths[COL_PERSON_NATIONAL_ID] := FDefaultNationalIdColWidth;
  ColWidths[COL_PERSON_NAME] := FDefaultNameColWidth;
end;

function TPersonGrid.IsDataRow( const ARow: integer ): Boolean;
begin
  Result := ( ARow >= FixedRows ) and ( ARow < RowCount );
end;

procedure TPersonGrid.SetColWidth( const ACol, AWidth: integer );
begin
  ColWidths[ACol] := AWidth;
end;

procedure TPersonGrid.Set_Anonymous( const Value: Boolean );
begin
  if Value then
  begin
    ColWidths[COL_PERSON_NAME] := -1;
    ColWidths[COL_PERSON_DOB] := -1;
    ColWidths[COL_PERSON_NATIONAL_ID] := -1;
  end
  else
    SetDefaultWidths( Self );
end;

procedure TPersonGrid.Set_DataCols( const AValue: integer );
begin
  ColCount := FixedCols + max( AValue, 1 );
end;

procedure TPersonGrid.Set_DataColWidth( const Value: integer );
var
  savedState: Boolean;
begin
  savedState := Anonymous;
  DefaultColWidth := Value;
  ColWidths[COL_PERSON_ID] := FDefaultIdColWidth;
  { Refresh name column width }
  Set_Anonymous( savedState );
end;

procedure TPersonGrid.Set_DataRows( const AValue: integer );
begin
  RowCount := FixedRows + max( AValue, 1 );
end;

function TPersonGrid.TryGetObject( const ACol, ARow: integer; out AObject: TObject ): Boolean;
begin
  AObject := FObjects[ACol, ARow];
  Result := Assigned( AObject );
end;

procedure TPersonGrid.SetObject( ACol: integer; ARow: integer; AObject: TObject );
begin
  FObjects[ACol, ARow] := AObject;
end;

end.
