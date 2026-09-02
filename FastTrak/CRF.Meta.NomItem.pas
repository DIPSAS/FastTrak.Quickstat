unit CRF.Meta.NomItem;

interface

uses
  Emetra.Interfaces.Listbox,
  Emetra.Logging.Interfaces,
  Emetra.Database.Interfaces,
  Emetra.Classes.Business,
  CRF.Study.Interfaces,
  {Standard}
  System.Classes, System.Generics.Collections, Data.DB;

type
  TNomItem = class( TInterfacedPersistent, IListBoxBase )
  strict private
    fV: string;
    fDN: string;
    fOT: string;
    fEnumVal: integer;
    fQuantity: double;
    fTextVal: string;
  public
    { Initialization }
    constructor Create( ADataset: TDataset ); reintroduce; overload;
    constructor Create( V, DN, OT: string ); reintroduce; overload;
    { Other members }
    function AsListbox( const ASimple: boolean = true ): string;
    function IsCurrent: boolean;
    function V: string;
    function DN: string;
    function OT: string;
    { Properties }
    property EnumVal: integer read fEnumVal;
    property Quantity: double read fQuantity;
    property TextVal: string read fTextVal;
  end;

  TNomItemList = class( TObjectList<TNomItem> )
  public
    procedure AddAndNext( const ADataset: TDataset );
  end;

  TNomSearchProcedure = class( TInterfacedPersistent, IListBoxBase )
  strict private
    fItemId: integer;
    fProcId: integer;
    fProcName: string;
    fDescription: string;
  public
    { Initialization }
    constructor Create( const AItemId, AProcId: integer; const AProcName, ADescription: string ); reintroduce;
    { IListBoxInterface }
    function AsListbox( const ASimple: boolean = true ): string;
    function IsCurrent: boolean;
    function V: string;
    function DN: string;
    function OT: string;
    { Properties }
    property ItemId: integer read fItemId;
    property ProcId: integer read fProcId;
    property ProcName: string read fProcName;
    property Description: string read fDescription;
  end;

  TNomSearchProcedureDictionary = class( TObjectDictionary<integer, TNomSearchProcedure> )
  public
    procedure AddAndNext( const ADataset: TDataset );
  end;

  TNomSearch = class( TBusiness, ILoginObserver, IStudyObserver )
  strict private
    fSQL: ISQL;
    fNomItems: TNomItemList;
    fProcedures: TNomSearchProcedureDictionary;
  public
    { Initialization }
    constructor Create( const ASQL: ISQL; const ALog: ILog ); reintroduce;
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { ILoginObserver }
    procedure AfterLogin( AConnection: IDatabaseConnection );
    function FriendlyName: string;
    { IStudyObserver }
    function GetNamePath: string; override;
    procedure AfterStudyChange( const Sender: IStudyId );
    { Other methods }
    function TryFind( const AStudyId, AItemId: integer; const ASearchText: string; out AMatches: integer ): boolean;
    { Properties }
    property Matches: TNomItemList read fNomItems;
    property Procedures: TNomSearchProcedureDictionary read fProcedures;
  end;

implementation

uses
  CRF.SQL,
  System.SysUtils;

{ TNomItem }

function TNomItem.AsListbox( const ASimple: boolean ): string;
begin
  Result := Format( '%s'#9'%s'#9#9'%s', [fV, fDN, fOT] );
end;

constructor TNomItem.Create( ADataset: TDataset );
begin
  inherited Create;
  fV := ADataset.FieldByName( 'V' ).AsString;
  fDN := ADataset.FieldByName( 'DN' ).AsString;
  fOT := ADataset.FieldByName( 'OT' ).AsString;
  fTextVal := ADataset.FieldByName( 'TextVal' ).AsString;
  fEnumVal := ADataset.FieldByName( 'EnumVal' ).AsInteger;
  fQuantity := ADataset.FieldByName( 'Quantity' ).AsFloat;
end;

constructor TNomItem.Create( V, DN, OT: string );
begin
  inherited Create;
  fV := V;
  fDN := DN;
  fOT := OT;
  fTextVal := fDN;
end;

function TNomItem.DN: string;
begin
  Result := fDN;
end;

function TNomItem.IsCurrent: boolean;
begin
  Result := true;
end;

function TNomItem.OT: string;
begin
  Result := fOT;
end;

function TNomItem.V: string;
begin
  Result := fV;
end;

{ TNomItemList }

procedure TNomItemList.AddAndNext( const ADataset: TDataset );
var
  newItem: TNomItem;
begin
  newItem := TNomItem.Create( ADataset );
  Add( newItem );
  ADataset.Next;
end;

{ TNomSearchProcedure }

constructor TNomSearchProcedure.Create( const AItemId, AProcId: integer; const AProcName, ADescription: string );
begin
  inherited Create;
  fItemId := AItemId;
  fProcId := AProcId;
  fProcName := AProcName;
  fDescription := ADescription;
end;

function TNomSearchProcedure.AsListbox( const ASimple: boolean ): string;
begin
  Result := V + #9 + DN + #9#9 + OT;
end;

function TNomSearchProcedure.DN: string;
begin
  Result := fDescription;
end;

function TNomSearchProcedure.IsCurrent: boolean;
begin
  Result := true;
end;

function TNomSearchProcedure.OT: string;
begin
  Result := IntToStr( fProcId );
end;

function TNomSearchProcedure.V: string;
begin
  Result := IntToStr( fItemId );
end;

{ TNomSearchProcedureDictionary }

procedure TNomSearchProcedureDictionary.AddAndNext( const ADataset: TDataset );
var
  newProc: TNomSearchProcedure;
begin
  with ADataset do
  begin
    newProc := TNomSearchProcedure.Create( FieldByName( 'ItemId' ).AsInteger, FieldByName( 'ProcId' ).AsInteger, FieldByName( 'ProcName' ).AsString, FieldByName( 'ProcDesc' ).AsString );
    Add( newProc.ItemId, newProc );
    Next;
  end;
end;

{ TNomSearch }

constructor TNomSearch.Create( const ASQL: ISQL; const ALog: ILog );
begin
  inherited Create( ALog );
  fSQL := ASQL;
end;

function TNomSearch.FriendlyName: string;
begin
  Result := 'Nomenklaturer';
end;

function TNomSearch.GetNamePath: string;
begin
  Result := '';
end;

procedure TNomSearch.AfterConstruction;
begin
  inherited;
  fNomItems := TNomItemList.Create( true );
  fProcedures := TNomSearchProcedureDictionary.Create;
end;

procedure TNomSearch.BeforeDestruction;
begin
  fProcedures.Free;
  fNomItems.Free;
  inherited;
end;

procedure TNomSearch.AfterLogin(AConnection: IDatabaseConnection);
begin

end;

procedure TNomSearch.AfterStudyChange(const Sender: IStudyId);
begin
  fProcedures.Clear;
  with fSQL.FastQuery( QRY_NOM_PROCEDURES, [Sender.StudyId] ) do
    try
      while not EOF do
        fProcedures.AddAndNext( fSQL.Dataset );
    finally
      Close;
    end;
end;

function TNomSearch.TryFind( const AStudyId, AItemId: integer; const ASearchText: string; out AMatches: integer ): boolean;
var
  searchProc: TNomSearchProcedure;
begin
  if not fProcedures.TryGetValue( AItemId, searchProc ) then
    raise EAssertionFailed.CreateFmt( 'TNomSearch.TryFind(): Unknown ItemId: %d', [AItemId] );
  fNomItems.Clear;
  with fSQL.FastQuery( Format( 'EXEC %s :StudyId, :SearchText', [searchProc.ProcName] ), [AStudyId, ASearchText] ) do
    try
      while not EOF do
        fNomItems.AddAndNext( fSQL.Dataset );
    finally
      Close;
    end;
  AMatches := fNomItems.Count;
  Result := AMatches > 0;
end;

end.

