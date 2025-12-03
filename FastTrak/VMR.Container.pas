unit VMR.Container;

interface

uses
  VMR.Common.Entries,
  Emetra.Logging.Interfaces,
  { Standard }
  System.RegularExpressions, System.Classes, System.Generics.Defaults,System.Generics.Collections;

type
  TVmrObjectList = class;

  TVmrStoredFragmentDateComparer = class( TComparer<TVmrCustomFragment> )
  public
    function Compare( const ALeft, ARight: TVmrCustomFragment ): Integer; override;
  end;

  IVmrBase = interface
    [ '{C9582A97-DF68-462D-B90F-071B67C9B17D}' ]
    { Property accessors }
    function Get_Count: Integer;
    function Get_Item( AIndex: Integer ): TVmrCustomFragment;
    { Other members }
    procedure AddEntry( AEntry: TVmrCustomFragment );
    procedure Remove( AEntry: TVmrCustomFragment );
    procedure RegisterList( AList: TVmrObjectList ); overload;
    procedure Sort;
    procedure UnregisterList( AList: TVmrObjectList );
    { Properties }
    property Count: Integer read Get_Count;
    property Items[ AIndex: Integer ]: TVmrCustomFragment read Get_Item; default;
  end;

  TVmrObjectList = class( TObjectList<TVmrCustomFragment> )
  private
    fComparer: TVmrStoredFragmentDateComparer;
    fName: string;
  public
    { Initialization }
    constructor Create( const AName: string; const AOwnsObjects: boolean = true ); reintroduce;
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { Other members }
    procedure SortByDateDescending;
    { Properties }
    property Name: string read FName;
  end;

  IVmrDataContainer = interface
    [ '{8F880752-7D66-47E5-8681-7FB8D70B2DF5}' ]
    procedure AddEntries( AVmr: IVmrBase );
  end;

  TVmr = class( TInterfacedPersistent, IVmrBase, IComparer<TVmrCustomFragment> )
  private
    fMainList: TVmrObjectList;
    fRegisteredLists: TList<TVmrObjectList>;
    fListNames: TStringList;
    fLog: ILog;
  protected
    function Get_Item( AIndex: Integer ): TVmrCustomFragment;
    function Get_Count: Integer;
    function Get_ListCount: integer;
    function Compare( const ALeft, ARight: TVmrCustomFragment ): Integer;
    procedure AddEntry( AEntry: TVmrCustomFragment );
    procedure Remove( AEntry: TVmrCustomFragment );
    procedure HandleNotify( Sender: TObject; const AItem: TVmrCustomFragment; Action: TCollectionNotification );
    procedure UnregisterList( AList: TVmrObjectList );
  public
    { Initialization }
    constructor Create( ALog: ILog );
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { Other members }
    procedure Clear;
    procedure RegisterList( AList: TVmrObjectList );
    procedure UnregisterAllObjectLists;
    procedure Sort;
    { Properties }
    property Items[ AIndex: Integer ]: TVmrCustomFragment read Get_Item; default;
  published
    property Count: Integer read Get_Count;
    property ListCount: integer read Get_ListCount;
  end;

implementation

uses
  {VMR}
  VMR.Common.Interfaces,
  {Standard}
  System.Math, System.SysUtils, WinApi.ActiveX;


  { TVmrMainList }

procedure TVmr.AfterConstruction;
begin
  inherited;
  FMainList := TVmrObjectList.Create( 'MainList', false );
  FRegisteredLists := TList<TVmrObjectList>.Create;
  FListNames := TStringList.Create;
  FListNames.Duplicates := dupError;
  FListNames.Sorted := true;
end;

procedure TVmr.BeforeDestruction;
begin
  if FRegisteredLists.Count > 0 then
  begin
    FLog.Event( 'Still registered: %s', [ FListNames.CommaText ] );
    FLog.SilentError( '%s.BeforeDestruction: RegisteredLists.Count = %d, should be empty!', [ ClassName, FRegisteredLists.Count ] );
  end;
  if FMainList.Count > 0 then
    FLog.SilentError( '%s.BeforeDestruction: MainList.Count = %d, should be empty!', [ ClassName, FMainList.Count ] );
  FreeAndNil( FRegisteredLists );
  FreeAndNil( FMainList );
  FreeAndNil( FListNames );
  inherited;
end;

procedure TVmr.HandleNotify( Sender: TObject; const AItem: TVmrCustomFragment; Action: TCollectionNotification );
begin
  if ( Action in [ cnRemoved, cnExtracted ] ) then
    FMainList.Remove( AItem )
  else if ( Action = cnAdded ) then
    FMainList.Add( AItem );
end;

procedure TVmr.RegisterList( AList: TVmrObjectList );
begin
  FLog.Event( '%s.RegisterList(%s)', [ ClassName, AList.Name ] );
  Assert( not Assigned( AList.OnNotify ), 'The list can only have single event OnNotify' );
  AList.OnNotify := Self.HandleNotify;
  FListNames.Add( AList.Name );
  FRegisteredLists.Add( AList );
end;

procedure TVmr.UnregisterAllObjectLists;
const
  PROC_NAME = 'UnregisterAllObjectLists';
begin
  FLog.EnterMethod( Self, Format( '%s: n=%d', [ PROC_NAME,FRegisteredLists.Count ] ) );
  try
    FMainList.Clear;
    while FRegisteredLists.Count > 0 do
      UnregisterList( FRegisteredLists[ 0 ] );
  finally
    FLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TVmr.UnregisterList( AList: TVmrObjectList );
begin
  FLog.Event( '%s.UnregisterList(%s)', [ ClassName, AList.Name ] );
  FRegisteredLists.Remove( AList );
  FListNames.Delete( FListNames.IndexOf( AList.Name ) );
  AList.OnNotify := nil;
end;

procedure TVmr.AddEntry( AEntry: TVmrCustomFragment );
begin
  FMainList.Add( AEntry );
end;

procedure TVmr.Clear;
begin
  FMainList.Clear;
end;

procedure TVmr.Sort;
begin
  FMainList.Sort( Self );
end;

function TVmr.Get_Count: Integer;
begin
  Result := FMainList.Count;
end;

function TVmr.Get_ListCount: integer;
begin
  Result := FRegisteredLists.Count;
end;

function TVmr.Get_Item( AIndex: Integer ): TVmrCustomFragment;
begin
  Result := FMainList[ AIndex ];
end;

procedure TVmr.Remove( AEntry: TVmrCustomFragment );
begin
  FMainList.Remove( AEntry );
end;

function TVmr.Compare( const ALeft, ARight: TVmrCustomFragment ): Integer;
begin
  Result := sign( ARight.Timestamp - ALeft.Timestamp );
  if Result = 0 then
    Result := VMR_SORT_ORDER[ ALeft.FragmentType ] - VMR_SORT_ORDER[ ARight.FragmentType ];
  if Result = 0 then
    Result := ARight.SortOrder - ALeft.SortOrder;
end;

constructor TVmr.Create( ALog: ILog );
begin
  FLog := ALog;
end;

{ TVmrObjectList }

procedure TVmrObjectList.AfterConstruction;
begin
  inherited;
  FComparer := TVmrStoredFragmentDateComparer.Create;
end;

procedure TVmrObjectList.BeforeDestruction;
begin
  FComparer.Free;
  inherited;
end;

constructor TVmrObjectList.Create( const AName: string; const AOwnsObjects: boolean = true );
begin
  inherited Create( AOwnsObjects );
  FName := AName;
end;

procedure TVmrObjectList.SortByDateDescending;
begin
  Self.Sort( FComparer );
end;

{ TVmrStoredFragmentDateComparer }

function TVmrStoredFragmentDateComparer.Compare( const ALeft, ARight: TVmrCustomFragment ): Integer;
begin
  { One second resolution }
  Result := round( 24 * 60 * 60 * ( ARight.Timestamp - ALeft.Timestamp ) );
end;

end.
