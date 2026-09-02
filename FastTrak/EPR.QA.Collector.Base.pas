unit EPR.QA.Collector.Base;

interface

uses
  EPR.QA.PointFactory,
  EPR.QA.DataPoint,
  EPR.QA.SQL,
  {General interfaces}
  EPR.QA.Matrix.Interfaces,
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces,
  {Standard}
  System.Classes, Generics.Collections;

type
  TDataCollector = class( TInterfacedPersistent, IGridDataCollector )
  strict private
    FName: string;
    FVarCount: integer;
    FFactory: TDataPointFactory;
    FVarList: TStringList;
    FVarOrder: TStringList;
    FBatch: TDictionary<integer, TObject>;
    function CreateDatapoint( const AVarName: string; const AValue: double; const ATimestamp: TDateTime; const ARowId: integer ): TDataPoint; dynamic;
  protected
    fStudyId: integer;
    FDB: ISQL;
    FLog: ILog;
    FMaxBatchSize: integer;
    FVarPrefix: string;
    FSQL: string;
    FTitle: string;
    FLastId: integer;
    { Property accessors }
    function Get_Name: string;
    { Other members }
    function BatchIsFull: boolean;
    function SinglePatient: boolean;
    function SQL: string; virtual;
    procedure AddToBatch( const AGridRow: IPersonGridRow );
    procedure RunBatch( const AStudyId: integer );
  public
    { Initialization }
    constructor Create( const ACollectorName, ACaption: string; AFactory: TDataPointFactory; ASQL: ISQL; ALog: ILog );
    destructor Destroy; override;
    { Other members }
    function BatchSize: integer;
    function Title: string;
    function VarNames: TStrings;
    { Properties }
    property name: string read Get_Name;
  end;

  TCustomDataCollector = class( TDataCollector )
  public
    { Initialization }
    constructor Create( const AName, ATitle, AVarPrefix, ASQL: string; const AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog ); reintroduce;
  end;

  TDataCollectorList = class( TObjectList<TDataCollector> )
  end;

implementation

uses
  Data.Db,
  System.SysUtils, System.RegularExpressions;

{ TDataCollector }

constructor TDataCollector.Create( const ACollectorName, ACaption: string; AFactory: TDataPointFactory; ASQL: ISQL; ALog: ILog );
begin
  inherited Create;
  FMaxBatchSize := 1;
  FName := ACollectorName;
  FTitle := ACaption;
  FDB := ASQL;
  FLog := ALog;
  FFactory := AFactory;
  FBatch := TDictionary<integer, TObject>.Create;
  FVarList := TStringList.Create;
  FVarList.Sorted := true;
  FVarList.Duplicates := dupIgnore;
  FVarOrder := TStringList.Create;
end;

destructor TDataCollector.Destroy;
begin
  FreeAndNil( FVarOrder );
  FreeAndNil( FVarList );
  FreeAndNil( FBatch );
  inherited;
end;

function TDataCollector.CreateDatapoint( const AVarName: string; const AValue: double; const ATimestamp: TDateTime; const ARowId: integer ): TDataPoint;
begin
  Result := FFactory.CreateDatapoint( AVarName ) as TDataPoint;
  Result.VarName := AVarName;
  Result.Update( AValue, ATimestamp, ARowId );
end;

procedure TDataCollector.AddToBatch( const AGridRow: IPersonGridRow );
begin
  FLastId := AGridRow.PersonId;
  FBatch.Add( FLastId, TObject( AGridRow ) );
end;

function TDataCollector.BatchIsFull: boolean;
begin
  Result := ( FBatch.Count >= FMaxBatchSize );
end;

function TDataCollector.BatchSize: integer;
begin
  Result := FBatch.Count;
end;

function TDataCollector.Get_Name: string;
begin
  Result := FName;
end;

function TDataCollector.SinglePatient;
begin
  Result := ( FMaxBatchSize = 1 );
end;

procedure TDataCollector.RunBatch( const AStudyId: integer );
const
  PROC_NAME = 'RunBatch';
var
  fldItemId: TField;
  fldCaption: TField;
  variableName: string;
  thisRow: TObject;
  newDatapoint: TDataPoint;
  gridRow: IPersonGridRow;
  dataset: TDataset;
  outsidePopulation: integer;
begin
  fStudyId := AStudyId;
  outsidePopulation := 0;
  FLog.EnterMethod( Self, Format( '%s(%d)', [PROC_NAME, AStudyId] ) );
  try
    if SinglePatient then
      dataset := FDB.FastQuery( SQL, [FLastId] )
    else
      dataset := FDB.FastQuery( SQL );
    try
      thisRow := nil;
      fldItemId := dataset.FindField( 'ItemId' );
      fldCaption := dataset.FindField( 'Caption' );
      while not dataset.EOF do
      begin
        if not FBatch.TryGetValue( dataset.Fields[0].AsInteger, thisRow ) then
          inc( outsidePopulation )
        else if Supports( thisRow, IPersonGridRow, gridRow ) then
        begin
          variableName := FVarPrefix + dataset.Fields[1].AsString;
          FVarList.Add( variableName );
          if FVarList.Count > FVarOrder.Count then
            FVarOrder.Add( variableName );
          newDatapoint := CreateDatapoint( variableName, dataset.Fields[2].AsFloat, dataset.Fields[3].AsDateTime, dataset.Fields[4].AsInteger );
          if Assigned( fldItemId ) then
            newDatapoint.ItemId := fldItemId.AsInteger;
          if Assigned( fldCaption ) then
            newDatapoint.Caption := fldCaption.AsString;
          if not gridRow.AddDatapoint( newDatapoint ) then
            newDatapoint.Free; { Existed already, freed }
        end;
        inc( FVarCount );
        dataset.Next;
      end;
    finally
      dataset.Close;
    end;
    if outsidePopulation > 0 then
      FLog.SilentWarning( 'Unknown patients found, n =%d', [outsidePopulation] );
    FLog.SilentSuccess( 'Rows processed: %d', [FVarCount] );
    FBatch.Clear;
  finally
    FLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

function TDataCollector.Title: string;
begin
  Result := FTitle;
end;

function TDataCollector.SQL: string;
var
  pidList: TStringList;
  key: integer;
begin
  if FMaxBatchSize <= 1 then
    Result := FSQL
  else
  begin
    pidList := TStringList.Create;
    try
      pidList.StrictDelimiter := true;
      pidList.Delimiter := ',';
      for key in FBatch.Keys do
        pidList.Add( IntToStr( key ) );
      Result := StringReplace( FSQL, PID_LIST_PLACEHOLDER, '(' + pidList.DelimitedText + ')', [rfIgnoreCase, rfReplaceAll] );
    finally
      pidList.Free;
    end;
  end;
end;

function TDataCollector.VarNames;
begin
  Result := FVarOrder;
end;

{ TCustomDrugCollector }

constructor TCustomDataCollector.Create( const AName, ATitle, AVarPrefix, ASQL: string; const AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
begin
  inherited Create( AName, ATitle, AFactory, ADb, ALog );
  FSQL := ASQL;
  FVarPrefix := AVarPrefix;
  FMaxBatchSize := maxint;
end;

end.
