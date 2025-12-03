unit EPR.QA.Matrix;

interface

uses
  EPR.QA.CaptionRecord,
  EPR.QA.CaptionDictionary,
  EPR.QA.DataPoint,
  EPR.QA.Matrix.Row,
  EPR.QA.Matrix.Column,
  {Interfaces}
  EPR.QA.Matrix.Interfaces,
  {CRF}
  CRF.Population.Interfaces,
  {General classes}
  Emetra.Classes.Business,
  {General interfaces}
  Emetra.Progress.Interfaces,
  Emetra.Person.Interfaces,
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces,
  {Standard}
  System.Classes, Generics.Collections, Data.Db;

type
  TPersonIdentification = ( pgiFull, pgiPersonIdOnly, pgiRandomPersonId );

  TPersonGridSortOrder = ( sbPersonId, sbReverseName );

  TPersonGridPopulation = class( TObjectDictionary<integer, TPersonGridRow> )
  public
    constructor Create; reintroduce;
  end;

  TPersonGridData = class( TBusiness, IPersonGridData )
  strict private
    fCaptions: TVarCaptions;
    fColumnNames: TPersonGridColumnList;
    fDb: ISQL;
    fGridComponent: IPersonGridComponent;
    fLocked: boolean;
    fPopulation: TObjectDictionary<integer, TPersonGridRow>;
    fPopulationDescription: string;
    fPopulationProcedure: string;
    fProgress: IProgress;
    fSortBy: TPersonGridSortOrder;
    fStudyId: integer;
    fTokenizer: TStringList;
  private
    { Property accessors }
    procedure Set_SortBy( const AValue: TPersonGridSortOrder );
    { Other members }
    procedure CheckDimensions;
    procedure PreparePatientMap;
    procedure PrepareVariableMap;
  public
    { Initialization }
    constructor Create( ADataGrid: IPersonGridComponent; APersonList: IPersonList; AProgress: IProgress; ASQL: ISQL; ALog: ILog );
    destructor Destroy; override;
    { Other members }
    function DataRows: integer;
    function Description( const AVarName: string ): string;
    function FieldCount: integer;
    function FieldType( const AFieldNo: integer ): TFieldType;
    function FixedRows: integer;
    function FixedCols: integer;
    function GetCellText( const ACol, ARow: integer; out ADatapoint: TDataPoint; out UI: integer; const AExport: boolean = false ): string;
    function GetRowData( const ARow: integer ): TObject;
    function HasData: boolean;
    function Subtitle( const AFieldNo: integer ): string;
    function Title( const AFieldNo: integer ): string;
    function TryGetDatapoint( const ACol, ARow: integer; out ADatapoint: TDataPoint ): boolean;
    function TryGetPatientAtRow( const ARow: integer; out APersonGridRow: TPersonGridRow ): boolean;
    function VarName( const AFieldNo: integer ): string;
    procedure AddCaption( const ACaptionRecord: TCaptionRecord );
    procedure AddData( const ACollector: IGridDataCollector );
    procedure Clear;
    procedure ClearVariables;
    procedure ClearPopulation;
    procedure LoadCaptions( const AIncludeLab, AIncludeCustom: boolean );
    procedure Lock;
    procedure PreparePopulation( const APersonList: IPersonList );
    procedure PrepareStudy( const AStudyName: string );
    procedure SaveToFile( const AFileName: string; const AIdentification: TPersonIdentification; const AIncludeDates: boolean );
    procedure SaveToSelection( const ATitle, ADescription: string );
    { Properties }
    property DataGrid: IPersonGridComponent read fGridComponent;
    property PopulationDescription: string read fPopulationDescription;
    property PopulationProcedure: string read fPopulationProcedure;
    property StudyId: integer read fStudyId;
    property SortBy: TPersonGridSortOrder read fSortBy write Set_SortBy;
  end;

implementation

uses
  EPR.QA.SQL,
  EPR.QA.Matrix.Anoymizer,
  {Standard}
  System.SysUtils, System.Math, WinApi.Windows;

resourcestring
  TXT_ACTIVE_RELATIONS = 'Aktive relasjoner';

{$REGION 'TPersonGridData'}

constructor TPersonGridData.Create( ADataGrid: IPersonGridComponent; APersonList: IPersonList; AProgress: IProgress; ASQL: ISQL; ALog: ILog );
begin
  inherited Create( ALog );
  fTokenizer := TStringList.Create;
  fTokenizer.Delimiter := '.';
  fTokenizer.StrictDelimiter := true;
  fGridComponent := ADataGrid;
  fDb := ASQL;
  fCaptions := TVarCaptions.Create( ALog );
  fProgress := AProgress;
  fPopulation := TPersonGridPopulation.Create;
  fColumnNames := TPersonGridColumnList.Create( fCaptions );
  fPopulationProcedure := 'GetCaseListMyRelations';
  fPopulationDescription := TXT_ACTIVE_RELATIONS;
  fSortBy := sbReverseName;
end;

function TPersonGridData.Description( const AVarName: string ): string;
begin
  Result := fCaptions.GetVarDescription( AVarName );
end;

destructor TPersonGridData.Destroy;
begin
  FreeAndNil( fPopulation );
  FreeAndNil( fColumnNames );
  FreeAndNil( fCaptions );
  FreeAndNil( fTokenizer );
  inherited;
end;

procedure TPersonGridData.AddCaption( const ACaptionRecord: TCaptionRecord );
begin
  fCaptions.AddCaption( ACaptionRecord );
end;

procedure TPersonGridData.AddData( const ACollector: IGridDataCollector );
var
  n: integer;
  personIndex: integer;
  thisPatient: TPersonGridRow;
  collectorVarName: string;
  personId: integer;
begin
  personIndex := 0;
  for personId in fPopulation.Keys do
  begin
    if fPopulation.TryGetValue( personId, thisPatient ) then
      ACollector.AddToBatch( thisPatient );
    if ACollector.BatchIsFull then
    begin
      ACollector.RunBatch( fStudyId );
      if Assigned( fProgress ) then
        fProgress.Percent := 100 * ( personIndex / fPopulation.Count );
    end;
    inc( personIndex );
  end;
  if ACollector.BatchSize > 0 then
    ACollector.RunBatch( fStudyId );
  if Assigned( fProgress ) then
    fProgress.Percent := 100;
  { Add column names }
  n := 0;
  while n < ACollector.VarNames.Count do
  begin
    collectorVarName := ACollector.VarNames[n];
    fColumnNames.Add( TPersonGridColumn.Create( collectorVarName, fCaptions.GetVarTitle( collectorVarName ) ) );
    inc( n );
  end;
  PrepareVariableMap;
end;

procedure TPersonGridData.LoadCaptions( const AIncludeLab, AIncludeCustom: boolean );
begin
  fCaptions.LoadLabCaptions := AIncludeLab;
  fCaptions.LoadCustomCaptions := AIncludeCustom;
  fCaptions.AfterLogin( fDb );
end;

procedure TPersonGridData.CheckDimensions;
begin
  { FColumnNames.Count = 0 and FPopulation.Count = 0 are special cases, because TDrawGrid behaves strangely
    when FixedRows=Rows and FixedCols=Cols (no data rows/cols).
    See Set_DataCols and Set_DataRows in TPersonGrid, which doesn't allow for zero values.
    So, DataCols/DataRows in the grid is always 1 or more, even when there is no actual data to show. }
  if ( fColumnNames.Count > 0 ) and ( fGridComponent.DataCols <> fColumnNames.Count ) then
    Log.SilentWarning( 'DataCol count mismatch: %d <> %d', [fGridComponent.DataCols, fColumnNames.Count] );
  if ( fPopulation.Count > 0 ) and ( fGridComponent.DataRows <> fPopulation.Count ) then
    Log.SilentWarning( 'DataRow count mismatch: %d <> %d', [fGridComponent.DataRows, fPopulation.Count] );
end;

procedure TPersonGridData.Clear;
begin
  fGridComponent.Clear;
  ClearPopulation;
  ClearVariables;
end;

procedure TPersonGridData.ClearVariables;
begin
  fGridComponent.DataCols := 0;
  fColumnNames.Clear;
end;

procedure TPersonGridData.ClearPopulation;
begin
  fPopulation.Clear;
  fLocked := false;
end;

function TPersonGridData.FieldCount: integer;
begin
  Result := fColumnNames.Count;
end;

function TPersonGridData.GetCellText( const ACol, ARow: integer; out ADatapoint: TDataPoint; out UI: integer; const AExport: boolean ): string;
var
  thisPatient: TPersonGridRow;
  thisColumn: TPersonGridColumn;
  cellObject: TObject;
begin
  ADatapoint := nil;
  Result := '';
  { Find alignment }
  UI := DT_SINGLELINE + DT_VCENTER;
  if ( ACol = 1 ) or ( ARow = 0 ) then
    UI := UI + DT_LEFT
  else
    UI := UI + DT_RIGHT;
  if not fLocked then
    Result := '(not ready)'
  else if fGridComponent.TryGetObject( ACol, ARow, cellObject ) then
  begin
    if cellObject.InheritsFrom( TDataPoint ) then
    begin
      ADatapoint := cellObject as TDataPoint;
      Result := Format( '%g', [ADatapoint.Value] );
    end
    else if cellObject.InheritsFrom( TPersonGridRow ) then
    begin
      thisPatient := cellObject as TPersonGridRow;
      case ACol of
        COL_PERSON_ID: Result := IntToStr( thisPatient.personId );
        COL_PERSON_DOB: Result := DateToStr( thisPatient.DOB );
        COL_PERSON_NATIONAL_ID: Result := thisPatient.NationalId;
        COL_PERSON_NAME: Result := thisPatient.FullName;
      end;
    end
    else if cellObject.InheritsFrom( TPersonGridColumn ) then
    begin
      thisColumn := cellObject as TPersonGridColumn;
      if ( ARow = 0 ) and AExport then
        Result := thisColumn.VarName
      else
        case ARow of
          0: Result := thisColumn.Title;
          1: Result := thisColumn.Subtitle;
        end;
    end;
  end
  else if ARow = 0 then
    case ACol of
      COL_PERSON_ID: Result := HDR_PID;
      COL_PERSON_DOB: Result := HDR_BORN;
      COL_PERSON_NATIONAL_ID: Result := HDR_NATIONAL_ID;
      COL_PERSON_NAME: Result := HDR_NAME;
    end
  else
    Result := 'nil';
end;

function TPersonGridData.TryGetDatapoint( const ACol, ARow: integer; out ADatapoint: TDataPoint ): boolean;
var
  thisPatient: TPersonGridRow;
  columnVarName: string;
begin
  ADatapoint := nil;
  Result := false;
  try
    if fGridComponent.IsDataRow( ARow ) and fGridComponent.IsDataCol( ACol ) and ( fColumnNames.Count > 0 ) then
    begin
      columnVarName := fColumnNames[fGridComponent.GridToDataCol( ACol )].VarName;
      if TryGetPatientAtRow( ARow, thisPatient ) then
        Result := thisPatient.GetDatapoint( columnVarName, ADatapoint );
    end;
  except
    on E: Exception do
      Log.SilentError( '%s.TryGetDatapoint(%d,%d): %s', [ClassName, ACol, ARow, E.Message] );
  end;
end;

function TPersonGridData.TryGetPatientAtRow( const ARow: integer; out APersonGridRow: TPersonGridRow ): boolean;
var
  thisObject: TObject;
begin
  Result := false;
  if fGridComponent.TryGetObject( 0, ARow, thisObject ) and thisObject.InheritsFrom( TPersonGridRow ) then
  begin
    Result := true;
    APersonGridRow := thisObject as TPersonGridRow;
  end
end;

function TPersonGridData.GetRowData( const ARow: integer ): TObject;
var
  thisPatient: TPersonGridRow;
begin
  if TryGetPatientAtRow( ARow, thisPatient ) then
    Result := thisPatient
  else
    Result := nil;
end;

function TPersonGridData.HasData: boolean;
begin
  Result := fColumnNames.Count > 0;
end;

procedure TPersonGridData.Lock;
var
  rowIndex, colIndex: integer;
  nameIndex: integer;
  thisDatapoint: TDataPoint;
begin
  CheckDimensions;
  fLocked := true;
  if fPopulation.Count > 0 then
  begin
    rowIndex := FixedRows;
    while rowIndex < fGridComponent.RowCount do
    begin
      colIndex := FixedCols;
      while colIndex < fGridComponent.ColCount do
      begin
        try
          nameIndex := colIndex - FixedCols;
          if TryGetDatapoint( colIndex, rowIndex, thisDatapoint ) then
            fGridComponent.SetObject( colIndex, rowIndex, thisDatapoint )
          else
            fGridComponent.SetObject( colIndex, rowIndex, fColumnNames[nameIndex] );
        except
          on E: Exception do
            Log.SilentError( '%s.Lock(%d): %s', [ClassName, rowIndex, E.Message] );
        end;
        inc( colIndex );
      end;
      inc( rowIndex );
    end;
  end;
end;

procedure TPersonGridData.PreparePatientMap;
var
  colIndex: integer;
  rowIndex: integer;
  gridRow: TPersonGridRow;
  sortedList: TGridRowList;
begin
  fGridComponent.DataRows := fPopulation.Count;
  rowIndex := fGridComponent.FixedRows;
  sortedList := TGridRowList.Create;
  try
    for gridRow in fPopulation.Values do
      sortedList.Add( gridRow );
    case fSortBy of
      sbReverseName: sortedList.SortByName;
      sbPersonId: sortedList.SortByPersonId;
    end;
    for gridRow in sortedList do
    begin
      colIndex := 0;
      while colIndex < fGridComponent.FixedCols do
      begin
        fGridComponent.SetObject( colIndex, rowIndex, gridRow );
        inc( colIndex );
      end;
      inc( rowIndex );
    end;
  finally
    sortedList.Free;
  end;
end;

procedure TPersonGridData.PrepareVariableMap;
var
  nameIndex: integer;
  colIndex: integer;
begin
  fGridComponent.DataCols := fColumnNames.Count;
  colIndex := fGridComponent.FixedCols;
  while colIndex < fGridComponent.ColCount do
  begin
    nameIndex := colIndex - FixedCols;
    if nameIndex < fColumnNames.Count then
    begin
      fGridComponent.SetObject( colIndex, 0, fColumnNames[nameIndex] );
      fGridComponent.SetObject( colIndex, 1, fColumnNames[nameIndex] );
    end;
    inc( colIndex );
  end;
end;

procedure TPersonGridData.PreparePopulation( const APersonList: IPersonList );
var
  n: integer;
  patient: IPersonReadOnly;
  conflictingPatient: TPersonGridRow;
begin
  TPersonGridRow.Log := Log;
  Clear;
  try
    n := 0;
    while n < APersonList.Count do
    begin
      patient := APersonList.Person[n];
      { Add patient only if not already in the grid }
      if not fPopulation.TryGetValue( patient.personId, conflictingPatient ) then
        fPopulation.Add( patient.personId, TPersonGridRow.Create( patient ) );
      inc( n );
    end;
  finally
    PreparePatientMap;
  end;
end;

procedure TPersonGridData.PrepareStudy( const AStudyName: string );
begin
  with fDb.FastQuery( QRY_STUDY_BY_NAME, [AStudyName] ) do
    try
      fStudyId := Fields[0].AsInteger;
    finally
      Close;
    end;
end;

procedure TPersonGridData.SaveToFile( const AFileName: string; const AIdentification: TPersonIdentification; const AIncludeDates: boolean );
var
  colNo: integer;
  rowNo: integer;
  dummy: integer;
  strCellText: string;
  thisDatapoint: TDataPoint;
  F: TextFile;
  matrixAnonymizer: TMatrixAnonymizer;
begin
  AssignFile( F, AFileName );
  Rewrite( F );
  matrixAnonymizer := TMatrixAnonymizer.Create( AFileName, fGridComponent.RowCount );
  try
    rowNo := 0;
    while rowNo < fGridComponent.RowCount do
    begin
      colNo := 0;
      while colNo < fGridComponent.ColCount do
      begin
        strCellText := GetCellText( colNo, rowNo, thisDatapoint, dummy, true );
        if ( AIdentification = pgiRandomPersonId ) and ( colNo = COL_PERSON_ID ) and ( rowNo > FixedRows - 1 ) then
        begin
          begin
            write( F, matrixAnonymizer.NewPersonId( strCellText ), ';' )
          end;
        end
        else if ( AIdentification <> pgiFull ) and ( colNo in [COL_PERSON_NAME, COL_PERSON_NATIONAL_ID, COL_PERSON_DOB] ) then
          { Skip }
        else
        begin
          strCellText := GetCellText( colNo, rowNo, thisDatapoint, dummy, true );
          write( F, AnsiQuotedStr( strCellText, '"' ), ';' );
          if AIncludeDates and ( colNo >= Self.FixedCols ) then
          begin
            if rowNo < Self.FixedRows then
              write( F, AnsiQuotedStr( strCellText + '.DATE', '"' ) )
            else if Assigned( thisDatapoint ) then
              write( F, AnsiQuotedStr( FormatDateTime( 'yyyy-mm-dd', thisDatapoint.TimeStamp ), '"' ) );
            write( F, EmptyStr, ';' );
          end;
        end;
        inc( colNo );
      end;
      WriteLn( F );
      inc( rowNo );
    end;
    if AIdentification = pgiRandomPersonId then
      matrixAnonymizer.SaveToFile;
  finally
    matrixAnonymizer.Free;
    CloseFile( F );
  end;
end;

procedure TPersonGridData.SaveToSelection( const ATitle, ADescription: string );
var
  selId: integer;
  patient: TPersonGridRow;
begin
  with fDb.FastQuery( QRY_ADD_SELECTION, [fStudyId, ATitle, ADescription] ) do
    try
      selId := FieldByName( FLD_SELECTION_ID ).AsInteger;
    finally
      Close;
    end;
  for patient in fPopulation.Values do
    fDb.ExecuteCommand( CMD_ADD_SELECTION_MEMBER, [selId, patient.personId] );
end;

procedure TPersonGridData.Set_SortBy( const AValue: TPersonGridSortOrder );
begin
  if AValue <> fSortBy then
  begin
    if fLocked then
      raise EAssertionFailed.Create( 'Can not change sort order after locking' );
    fSortBy := AValue;
  end;
end;

function TPersonGridData.DataRows: integer;
begin
  Result := fPopulation.Count;
end;

function TPersonGridData.VarName( const AFieldNo: integer ): string;
begin
  Result := fColumnNames[AFieldNo].VarName;
end;

function TPersonGridData.Title( const AFieldNo: integer ): string;
begin
  Result := fColumnNames[AFieldNo].Title;
end;

function TPersonGridData.Subtitle( const AFieldNo: integer ): string;
begin
  Result := fColumnNames[AFieldNo].Subtitle;
end;

function TPersonGridData.FieldType( const AFieldNo: integer ): TFieldType;
begin
  Result := ftFloat
end;

function TPersonGridData.FixedRows: integer;
begin
  Result := fGridComponent.FixedRows;
end;

function TPersonGridData.FixedCols: integer;
begin
  Result := fGridComponent.FixedCols;
end;

{$ENDREGION}
{$REGION 'TPersonGridPopulation'}

constructor TPersonGridPopulation.Create;
begin
  inherited Create( [doOwnsValues] );
end;

{$ENDREGION}

end.
