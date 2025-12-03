unit Emetra.Classes.Subject.Stored;

interface

uses
  {General classes}
  Emetra.Classes.Subject,
  {General interfaces}
  Emetra.Database.Interfaces,
  Emetra.Database.Dialog.Interfaces,
  Emetra.Logging.Interfaces,
  Emetra.Dictionary.Interfaces,
  Emetra.Interfaces.ListBox,
  Emetra.Interfaces.Observer,
  Emetra.Interfaces.SaveLoad,
  Emetra.Interfaces.Lookup,
  {Standard}
  Data.Db, System.Math, System.Types, System.Classes, System.SysUtils, System.Variants, System.TypInfo, System.StrUtils;

type
  EDatasetEmpty = class( Exception );
  TOnDataset = function( const ASQL: string ): TDataset of object;

  TStoredObject = class( TObservable )
  strict private
    FDatabase: ISQL;
  protected
    function Get_SQL: ISQL;
  protected
    function Connected: boolean;
    property SQL: ISQL read Get_SQL;
  public
    constructor Create( ADatabase: ISQL; ALog: ILog ); reintroduce; virtual;
  end;

  TStoredListItem = class( TStoredObject, ICodedValue, IListBoxBase, IObservable, IVariantDictionary, ILoad, ICheckPermissionProblem )
  protected
    FDataset: TDataset;
    FPrimaryKey: integer;
    FLastUpdate: TDateTime;
    { Listbox text fragments }
    class function TableName: string; dynamic;
    class function IdentityField: string; dynamic;
    function PickList: IDatabasePickList;
    { ICodedText, ICodedValue, and IListBoxItem }
    function V: string; dynamic;
    function DN: string; dynamic;
    function OT: string; dynamic;
    function Description: string; dynamic;
    { Read properties }
    function ReadBool( ADataset: TDataset; const AFieldName: string; const ADefault: boolean = false ): boolean; overload;
    function ReadDateTime( ADataset: TDataset; const AFieldName: string; const ADefault: TDateTime = 0 ): TDateTime; overload;
    function ReadString( ADataset: TDataset; const AFieldName: string; const ADefault: string = '' ): string; overload;
    function ReadInteger( ADataset: TDataset; const AFieldName: string; const ADefault: integer = -1 ): integer; overload;
    function ReadFloat( ADataset: TDataset; const AFieldName: string; const ADefault: double = -1 ): double; overload;
    function ReadString( const AFieldName: string ): string; overload;
    function ReadBool( const AFieldName: string ): boolean; overload;
    function ReadDateTime( const AFieldName: string ): TDateTime; overload;
    function ReadInteger( const AFieldName: string ): integer; overload;
    function ReadFloat( const AFieldName: string ): double; overload;
    { Other members }
    function GetData: TDataset; dynamic;
    function Get_LastUpdate: TDateTime;
    procedure Set_LastUpdate( const AValue: TDateTime );
    procedure CheckPermissionProblem( const E: Exception; const AMsgTemplate: string );
    procedure CloseDataset;
    procedure Load( ADataset: TDataset ); dynamic;
    procedure SetDataset( ADataset: TDataset );
  public
    function AsListbox( const ASimple: boolean = true ): string; dynamic;
    function IsCurrent: boolean; dynamic;
    function Match( const AFilter: string ): boolean;
    function UniqueKey: string; dynamic;
    procedure Clear; override;
    procedure Populate; dynamic;
  published
    property PK: integer read FPrimaryKey;
    property LastUpdate: TDateTime read Get_LastUpdate;
  end;

var
  RaiseExceptionOnEmptyDataset: boolean = true;

implementation

{ TStoredListItem }

const
  TAB = #9;

constructor TStoredObject.Create( ADatabase: ISQL; ALog: ILog );
begin
  inherited Create( ALog );
  FDatabase := ADatabase;
end;

function TStoredObject.Connected: boolean;
begin
  Result := Assigned( FDatabase );
end;

function TStoredObject.Get_SQL: ISQL;
begin
  if not Assigned( FDatabase ) then
    raise EDatabaseError.CreateFmt( '%s.SQL: Not assigned', [ClassName] )
  else
    Result := FDatabase;
end;

{ TStoredListItem }

function TStoredListItem.AsListbox( const ASimple: boolean ): string;
begin
  Result := V + TAB + DN + TAB;
  if not ASimple then
    Result := Result + Description;
  Result := Result + TAB + OT;
end;

procedure TStoredListItem.Clear;
begin
  BeginUpdate;
  try
    inherited Clear;
    FPrimaryKey := 0;
    FLastUpdate := 0;
  finally
    EndUpdate;
  end;
end;

procedure TStoredListItem.CloseDataset;
begin
  if Assigned( FDataset ) then
    FDataset.Close;
end;

function TStoredListItem.V: string;
begin
  Result := IntToStr( FPrimaryKey );
end;

procedure TStoredListItem.CheckPermissionProblem( const E: Exception; const AMsgTemplate: string );
var
  intf: ICheckPermissionProblem;
begin
  if Supports( SQL, ICheckPermissionProblem, intf ) then
    intf.CheckPermissionProblem( E, AMsgTemplate )
  else
    Log.Event( AMsgTemplate, [E.Message], ltException );
end;

function TStoredListItem.GetData: TDataset;
var
  queryText: string;
begin
  queryText := Format( 'SELECT * FROM %s WHERE %s=:PrimaryKey', [TableName, IdentityField] );
  Result := SQL.FastQuery( queryText, [FPrimaryKey] );
end;

function TStoredListItem.Get_LastUpdate;
begin
  Result := FLastUpdate;
end;

procedure TStoredListItem.Set_LastUpdate( const AValue: TDateTime );
begin
  FLastUpdate := AValue;
end;

function TStoredListItem.DN: string;
begin
  Result := ClassName;
end;

class function TStoredListItem.IdentityField: string;
begin
  Result := 'PK';
end;

function TStoredListItem.IsCurrent: boolean;
begin
  Result := true;
end;

function TStoredListItem.ReadString( ADataset: TDataset; const AFieldName: string; const ADefault: string = '' ): string;
var
  fld: TField;
begin
  SetDataset( ADataset );
  fld := FDataset.FindField( AFieldName );
  if Assigned( fld ) then
    Result := fld.AsString
  else
    Result := ADefault;
end;

function TStoredListItem.ReadDateTime( ADataset: TDataset; const AFieldName: string; const ADefault: TDateTime = 0 ): TDateTime;
var
  fld: TField;
begin
  SetDataset( ADataset );
  fld := FDataset.FindField( AFieldName );
  if Assigned( fld ) then
    Result := fld.AsDateTime
  else
    Result := ADefault;
end;

procedure TStoredListItem.Load( ADataset: TDataset );
begin
  FDataset := ADataset;
  FPrimaryKey := ReadInteger( 'PK' );
  FLastUpdate := ReadDateTime( 'LastUpdate' );
end;

function TStoredListItem.Description: string;
begin
  Result := EmptyStr;
end;

function TStoredListItem.Match( const AFilter: string ): boolean;
begin
  if AFilter = EmptyStr then
    Result := true
  else
    Result := ContainsText( AsListbox( false ), AFilter );
end;

function TStoredListItem.PickList: IDatabasePickList;
begin
  Assert( Assigned( GlobalPickList ), Format( '%s: GlobalPickList is unassigned', [ClassName] ) );
  Result := GlobalPickList;
end;

procedure TStoredListItem.Populate;
begin
  BeginUpdate;
  try
    Load( GetData );
    FDataset.Close;
  finally
    EndUpdate;
  end;
end;

function TStoredListItem.ReadInteger( ADataset: TDataset; const AFieldName: string; const ADefault: integer = -1 ): integer;
var
  fld: TField;
begin
  SetDataset( ADataset );
  fld := FDataset.FindField( AFieldName );
  if Assigned( fld ) then
  begin
    if fld.IsNull then
      Result := ADefault
    else
      Result := fld.AsInteger
  end
  else
    Result := ADefault;
end;

function TStoredListItem.ReadString( const AFieldName: string ): string;
begin
  Result := ReadString( FDataset, AFieldName );
end;

function TStoredListItem.OT: string;
begin
  Result := IntToStr( SizeOf( Self ) );
end;

procedure TStoredListItem.SetDataset( ADataset: TDataset );
const
  EXC_EMPTY_CLASS_DATASET = 'TStoredListItem: The class instance (%s) can not be populated from an empty dataset.';
begin
  if RaiseExceptionOnEmptyDataset and ( ( ADataset = nil ) or ( ADataset.EOF ) ) then
  begin
    if Assigned( ADataset ) then
      ADataset.Close;
    raise EDatasetEmpty.CreateFmt( EXC_EMPTY_CLASS_DATASET, [ClassName] );
  end;
  FDataset := ADataset;
end;

class function TStoredListItem.TableName: string;
begin
  Result := 'ORM.' + Copy( ClassName, 2, maxint );
end;

function TStoredListItem.UniqueKey: string;
begin
  Result := IntToStr( FPrimaryKey );
end;

function TStoredListItem.ReadBool( ADataset: TDataset; const AFieldName: string; const ADefault: boolean ): boolean;
var
  fld: TField;
begin
  SetDataset( ADataset );
  fld := FDataset.FindField( AFieldName );
  if Assigned( fld ) then
    Result := fld.AsBoolean
  else
    Result := ADefault;
end;

function TStoredListItem.ReadBool( const AFieldName: string ): boolean;
begin
  Result := ReadBool( FDataset, AFieldName );
end;

function TStoredListItem.ReadDateTime( const AFieldName: string ): TDateTime;
begin
  Result := ReadDateTime( FDataset, AFieldName );
end;

function TStoredListItem.ReadInteger( const AFieldName: string ): integer;
begin
  Result := ReadInteger( FDataset, AFieldName );
end;

function TStoredListItem.ReadFloat( const AFieldName: string ): double;
begin
  Result := ReadFloat( FDataset, AFieldName );
end;

function TStoredListItem.ReadFloat( ADataset: TDataset; const AFieldName: string; const ADefault: double = -1 ): double;
var
  fld: TField;
begin
  SetDataset( ADataset );
  fld := FDataset.FindField( AFieldName );
  if Assigned( fld ) then
    Result := fld.AsFloat
  else
    Result := ADefault;
end;

end.
