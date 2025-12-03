unit EPR.QA.Matrix.Row;

interface

uses
  EPR.QA.DataPoint,
  EPR.QA.Matrix.Interfaces,
  {General interfaces}
  Emetra.Logging.Interfaces,
  Emetra.Person.Interfaces,
  {Standard}
  Data.Db, Generics.Collections, Generics.Defaults, System.Classes;

type
  TPointCounter = record
    Added: integer;
    Updated: integer;
    Skipped: integer;
    Failed: integer;
    procedure Reset;
  end;

  TPersonGridRow = class( TInterfacedPersistent, IPersonGridRow, IPersonIdentity )
  strict private
    FCounter: TPointCounter;
    FRowData: TObjectDictionary<string, TDataPoint>;
    fPersonId: integer;
    fGenderId: integer;
    fFullName: string;
    fNationalId: string;
    FDOB: TDate;
  private
    class var fLog: ILog;
  protected
    { Property accessors }
    function Get_DOB: TDate;
    function Get_FullName: string;
    function Get_NationalId: string;
    function Get_PersonId: integer;
    function Get_Sex: TSex;
  public
    { Initialization }
    constructor Create( const APersonId: integer ); overload;
    constructor Create( const APerson: IPersonReadOnly ); overload;
    destructor Destroy; override;
    { Other members }
    function GetValue( const AVarName: string; out AValue: double ): boolean;
    function GetDatapoint( const AVarName: string; out ADatapoint: TDataPoint ): boolean;
    function AddDatapoint( ADatapoint: TObject ): boolean;
    function AddData( const ARowId: integer; const ATimestamp: TDateTime; const AVarName: string; const AValue: double ): boolean;
    procedure Load( ADataset: TDataset );
    { Properties }
    property DOB: TDate read FDOB;
    property FullName: string read Get_FullName;
    property GenderId: integer read fGenderId;
    property NationalId: string read Get_NationalId write fNationalId;
    property PersonId: integer read Get_PersonId;
    property Sex: TSex read Get_Sex;
    class property Log: ILog read fLog write fLog;
  end;

  TNameComparer = class( TInterfacedPersistent, IComparer<TPersonGridRow> )
    function Compare( const Left, Right: TPersonGridRow ): integer;
  end;

  TPersonIdComparer = class( TInterfacedPersistent, IComparer<TPersonGridRow> )
    function Compare( const Left, Right: TPersonGridRow ): integer;
  end;

  TGridRowList = class( TList<TPersonGridRow> )
  public
    procedure SortByName;
    procedure SortByPersonId;
  end;

implementation

uses
  System.SysUtils;

{$REGION 'TPersonGridRow'}

constructor TPersonGridRow.Create( const APersonId: integer );
begin
  fPersonId := APersonId;
  FRowData := TObjectDictionary<string, TDataPoint>.Create( [doOwnsValues] );
end;


constructor TPersonGridRow.Create(const APerson: IPersonReadOnly );
begin
  Create( APerson.PersonId );
  fDob := APerson.DOB;
  fFullName := APerson.LastName + ', ' + APerson.FirstName;
  fNationalId := APerson.NationalId;
  fGenderId := APerson.GenderId;
end;

destructor TPersonGridRow.Destroy;
begin
  FRowData.Free;
  inherited;
end;

function TPersonGridRow.AddData( const ARowId: integer; const ATimestamp: TDateTime; const AVarName: string; const AValue: double ): boolean;
const
  PROC_NAME = 'AddData';
var
  thisDataPoint: TDataPoint;
begin
  thisDatapoint := nil;
  Result := false;
  try
    if not FRowData.TryGetValue( AVarName, thisDataPoint ) then
    begin
      FRowData.Add( AVarName, TDataPoint.Create( AVarName, AValue, ATimestamp, ARowId ) );
      inc( FCounter.Added );
    end
    else if ( thisDataPoint.TimeStamp < ATimestamp ) then
    begin
      thisDataPoint.Update( AValue, ATimestamp, ARowId );
      inc( FCounter.Updated );
    end
    else
      inc( FCounter.Skipped );
  except
    on E: Exception do
    begin
      inc( FCounter.Failed );
      Log.SilentError( '%s.%s: %s', [ClassName, PROC_NAME, E.Message] );
    end;
  end;
end;

function TPersonGridRow.AddDatapoint( ADatapoint: TObject ): boolean;
var
  existingDatapoint: TDataPoint;
begin
  existingDatapoint := nil;
  Result := false;
  with ADatapoint as TDataPoint do
  begin
    if FRowData.TryGetValue( VarName, existingDatapoint ) then
      with ADatapoint as TDataPoint do
        existingDatapoint.Update( Value, TimeStamp, RowId )
    else
    begin
      FRowData.Add( VarName, ADatapoint as TDataPoint );
      Result := true;
    end;
  end;
end;


function TPersonGridRow.GetValue( const AVarName: string; out AValue: double ): boolean;
var
  thisData: TDataPoint;
begin
  thisData := nil;
  Result := FRowData.TryGetValue( AVarName, thisData );
  if Result then
    AValue := thisData.Value
  else
    AValue := -1;
end;

procedure TPersonGridRow.Load( ADataset: TDataset );
var
  fldNatId: TField;
  fldGenderId: TField;
begin
  fFullName := ADataset.FieldByName( 'FullName' ).AsString;
  FDOB := ADataset.FieldByName( 'DOB' ).AsDateTime;
  fldNatId := ADataset.FindField( 'NationalId' );
  { NationalId missing from most Population datasets }
  if Assigned( fldNatId ) then
    fNationalId := fldNatId.AsString;
  fldGenderId := ADataset.FindField( 'GenderId' );
  { GenderId missing from most Population datasets }
  if Assigned( fldGenderId ) then
    fGenderId := fldGenderId.AsInteger;
end;

function TPersonGridRow.GetDatapoint( const AVarName: string; out ADatapoint: TDataPoint ): boolean;
begin
  ADatapoint := nil; { CodeHealer }
  Result := FRowData.TryGetValue( AVarName, ADatapoint );
end;

function TPersonGridRow.Get_FullName: string;
begin
  Result := fFullName;
end;

function TPersonGridRow.Get_NationalId: string;
begin
  Result := fNationalId;
end;

function TPersonGridRow.Get_PersonId: integer;
begin
  Result := fPersonId;
end;

function TPersonGridRow.Get_DOB: TDate;
begin
  Result := FDOB;
end;

function TPersonGridRow.Get_Sex: TSex;
begin
  case fGenderId of
    1: Result := sexMale;
    2: Result := sexFemale;
  else Result := sexUnknown;
  end;
end;

{$ENDREGION}
{ TPointCounter }

procedure TPointCounter.Reset;
begin
  Added := 0;
  Failed := 0;
  Updated := 0;
  Skipped := 0;
end;

{ TNameComparer }

function TNameComparer.Compare( const Left, Right: TPersonGridRow ): integer;
begin
  Result := CompareStr( Left.FullName, Right.FullName );
end;

{ TPersonIdComparer }

function TPersonIdComparer.Compare( const Left, Right: TPersonGridRow ): integer;
begin
  Result := Left.PersonId - Right.PersonId;
end;

{ TGridRowList }

procedure TGridRowList.SortByName;
var
  comparer: TNameComparer;
begin
  comparer := TNameComparer.Create;
  try
    Sort( comparer );
  finally
    comparer.Free;
  end;
end;

procedure TGridRowList.SortByPersonId;
var
  comparer: TPersonIdComparer;
begin
  comparer := TPersonIdComparer.Create;
  try
    Sort( comparer );
  finally
    comparer.Free;
  end;
end;

end.
