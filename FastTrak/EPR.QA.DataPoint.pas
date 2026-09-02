unit EPR.QA.DataPoint;

interface

uses
  {EPR.QA}
  EPR.QA.Matrix.Interfaces,
  {Standard}
  Graphics, Classes;

type
  TDataPoint = class( TInterfacedPersistent, ICellText )
  strict private
    FItemId: integer;
    FRowId: integer;
    FTimeStamp: TDateTime;
    FUpdateCount: integer;
    FValue: double;
    FVarName: string;
    fCaption: string;
    procedure Set_VarName( const AVarName: string );
    procedure Set_Caption( const ACaption: string );
  public
    { Initialization }
    constructor Create( const AVarName: string; const AValue: double; const ATimestamp: TDateTime; const ARowId: integer );
    { Other members }
    function AsString: string;
    function CellText: string; dynamic;
    function CellHint: string; virtual;
    function AlignLeft: boolean;
    procedure Update( const AValue: double; const ATimestamp: TDateTime; const ARowId: integer );
    { Properties }
    property Caption: string read fCaption write Set_Caption;
    property ItemId: integer read FItemId write FItemId;
    property RowId: integer read FRowId;
    property TimeStamp: TDateTime read FTimeStamp;
    property Value: double read FValue;
    property VarName: string read FVarName write Set_VarName;
  end;

  { Colored datapoint base class }
  TColoredDataPoint = class( TDataPoint, IBrushColor, ICustomColor )
  protected
    FColor: TColor;
  public
    { Initialization }
    procedure AfterConstruction; override;
    { Other methods }
    function BrushColor: TColor;
    procedure SetColor( const AColor: TColor );
  end;

implementation

uses
  SysUtils;

{ TDatapoint }

constructor TDataPoint.Create( const AVarName: string; const AValue: double; const ATimestamp: TDateTime; const ARowId: integer );
begin
  inherited Create;
  FVarName := AVarName;
  Update( AValue, ATimestamp, ARowId );
end;

function TDataPoint.AlignLeft: boolean;
begin
  Result := ( fCaption <> EmptyStr );
end;

function TDataPoint.AsString: string;
begin
  Result := Format( '%s = %g'#10'TimeStamp = %s'#10'RowId = %d'#10'Updates = %d', [FVarName, FValue, DateToStr( FTimeStamp ), FRowId, FUpdateCount] );
  if FItemId > 0 then
    Result := Result + #10 + Format( 'ItemId = %d', [FItemId] );
  if fCaption <> EmptyStr then
    Result := Result + #10 + Format( 'Caption ="%s"', [fCaption] );
end;

function TDataPoint.CellHint: string;
begin
  Result := EmptyStr;
end;

function TDataPoint.CellText: string;
begin
  if fCaption <> EmptyStr then
    Result := Copy( fCaption, 1, 6 )
  else
    Result := Format( '%g', [FValue] );
end;

procedure TDataPoint.Set_VarName( const AVarName: string );
begin
  FVarName := AVarName;
end;

procedure TDataPoint.Set_Caption( const ACaption: string );
begin
  fCaption := ACaption;
end;

procedure TDataPoint.Update( const AValue: double; const ATimestamp: TDateTime; const ARowId: integer );
begin
  FValue := AValue;
  FTimeStamp := ATimestamp;
  FRowId := ARowId;
  inc( FUpdateCount );
end;

{ TColoredDatapoint }

procedure TColoredDataPoint.AfterConstruction;
begin
  inherited;
  FColor := clNone;
end;

function TColoredDataPoint.BrushColor: TColor;
begin
  Result := FColor;
end;

procedure TColoredDataPoint.SetColor( const AColor: TColor );
begin
  FColor := AColor;
end;

end.
