unit Emetra.Person.Manager;

interface

uses
  Emetra.Person.Interfaces,
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces,
  {Standard}
  System.Classes;

type
  TPersonManager = class( TInterfacedPersistent, IPersonIdentityMapper, IPersonDuplicateManager, IPersonListManager )
  strict private
    FSQL: ISQL;
    fLog: ILog;
  private
    function VerifyMatch( const APersonId, ARowCount: integer ): boolean;
  public
    { Initialization }
    constructor Create( ASQL: ISQL; ALog: ILog );
    { Other members }
    function AddPerson( const APerson: IPersonReadOnly ): integer; overload;
    function AddPerson( const ADOB: TDateTime; const AGenderId: integer; const AFirstName, ALastName, ANationalId: string ): integer; overload;
    procedure EditPerson( const APersonId: integer; ADOB: TDateTime; AGenderId: integer; AFirstName, ALastName, ANationalId: string );
    procedure UpdatePerson( const APersonId: integer; APerson: IPersonReadOnly );
    { Exact mapping }
    function TryMapNationalId( const ANationalId: string; out APersonId: integer ): boolean;
    function TryMapDOBName( const ADOB: TDateTime; const AFirstName, ALastName: string; out APersonId: integer ): boolean;
    function TryMapUsername( const AUserName: string; out APersonId: integer ): boolean;
    { Fuzzy matching }
    function MustBeSamePerson( const APerson1, APerson2: IPersonReadOnly ): boolean;
    function ProbablySamePerson( const APerson1, APerson2: IPersonReadOnly ): boolean;
    function UserConfirmedSamePerson( const APerson1, APerson2: IPersonReadOnly ): boolean;
  end;

implementation

uses
  Emetra.Person.SQL,
  {Standard}
  SysUtils;

resourcestring

  ASK_SAME_PERSON = 
  { } 'Er disse to den samme personen?\n\n{{%s}}\n{{%s}}\n\n' + 
  { } 'Hvis du svarer Nei opprettes det en ny person.';
  
  EXC_DUPLICATE_PERSON = 'Denne personen finnes allerede i databasen.';

constructor TPersonManager.Create( ASQL: ISQL; ALog: ILog );
begin
  inherited Create;
  FSQL := ASQL;
  fLog := ALog;
end;

function TPersonManager.ProbablySamePerson( const APerson1, APerson2: IPersonReadOnly ): boolean;
begin
  { Matching DOB and one name, or matching both names }
  Result := ( ( APerson1.DOB = APerson2.DOB ) and SameText( APerson1.FirstName, APerson2.FirstName ) ) or
    ( ( APerson1.DOB = APerson2.DOB ) and SameText( APerson1.LastName, APerson2.LastName ) ) or
    ( SameText( APerson1.FirstName, APerson2.FirstName ) and SameText( APerson1.LastName, APerson2.LastName ) );
end;

function TPersonManager.MustBeSamePerson( const APerson1, APerson2: IPersonReadOnly ): boolean;
begin
  { Matching NationalId }
  Result := ( ( Length( APerson1.NationalId ) > 9 ) and ( APerson1.NationalId = APerson2.NationalId ) );
  { One or more empty NationalId, everytning else matches }
  if not Result then
    Result := ( ( APerson1.DOB = APerson2.DOB ) and SameText( APerson1.LastName, APerson2.LastName ) and ( APerson1.GenderId = APerson2.GenderId ) and
      SameText( APerson1.FirstName, APerson2.FirstName ) and ( ( APerson1.NationalId = EmptyStr ) or ( APerson2.NationalId = EmptyStr ) ) );
end;

function TPersonManager.UserConfirmedSamePerson( const APerson1, APerson2: IPersonReadOnly ): boolean;
begin
  Result := ProbablySamePerson( APerson1, APerson2 ) and fLog.LogYesNo( Format( ASK_SAME_PERSON, [APerson1.VisualId, APerson2.VisualId] ), ltWarning )
end;

function TPersonManager.VerifyMatch( const APersonId, ARowCount: integer ): boolean;
begin
  Result := ( APersonId > 0 ) and ( ARowCount = 1 );
end;

function TPersonManager.TryMapNationalId( const ANationalId: string; out APersonId: integer ): boolean;
var
  rowsFound: integer;
begin
  APersonId := 0;
  if Trim( ANationalId ) = EmptyStr then
    Result := false
  else
  begin
    rowsFound := 0;
    with FSQL.FastQuery( QRY_PERSON_ID, [ANationalId] ) do
      try
        while not EOF do
        begin
          inc( rowsFound );
          APersonId := Fields[0].AsInteger;
          Next;
        end
      finally
        Close;
      end;
    Result := VerifyMatch( APersonId, rowsFound );
  end;
end;

function TPersonManager.TryMapUsername(const AUserName: string; out APersonId: integer): boolean;
begin
  APersonId := 0;
  with FSQL.FastQuery( QRY_PERSON_BY_USERNAME, [Trim( AUserName )] ) do
    try
      while not EOF do
      begin
        APersonId := Fields[0].AsInteger;
        Next;
      end;
    finally
      Close;
    end;
  Result := APersonId > 0;
end;

function TPersonManager.TryMapDOBName( const ADOB: TDateTime; const AFirstName, ALastName: string; out APersonId: integer ): boolean;
var
  rowsFound: integer;
begin
  APersonId := 0;
  rowsFound := 0;
  with FSQL.FastQuery( QRY_PERSON_ID_BY_DOB_NAME, [ADOB, Trim( AFirstName ), Trim( ALastName )] ) do
    try
      while not EOF do
      begin
        inc( rowsFound );
        APersonId := Fields[0].AsInteger;
        Next;
      end;
    finally
      Close;
    end;
  Result := VerifyMatch( APersonId, rowsFound );
end;

function TPersonManager.AddPerson( const APerson: IPersonReadOnly ): integer;
begin
  Result := AddPerson( APerson.DOB, APerson.GenderId, Trim( APerson.FirstName ), Trim( APerson.LastName ), APerson.NationalId );
end;

function TPersonManager.AddPerson( const ADOB: TDateTime; const AGenderId: integer; const AFirstName, ALastName, ANationalId: string ): integer;
var
  newPersonId: integer;
  existingPersonId: integer;

begin
  newPersonId := 0;
  existingPersonId := 0;
  { First check that there is no person like this already }
  if TryMapNationalId( ANationalId, existingPersonId ) or TryMapDOBName( ADOB, AFirstName, ALastName, existingPersonId ) then
    fLog.Event( EXC_DUPLICATE_PERSON, ltWarning )
  else
    try
      with FSQL.FastQuery( CMD_ADD_PERSON, [ADOB, Trim( AFirstName ), EmptyStr, Trim( ALastName ), AGenderId, ANationalId] ) do
      begin
        { Dataset will return the new PersonId }
        if not EOF then
          newPersonId := Fields[0].AsInteger
        else
          newPersonId := 0;
        Close;
      end;
    except
      on E: Exception do
        fLog.Event( E.Message, ltException );
    end;
  Result := newPersonId;
end;

procedure TPersonManager.EditPerson( const APersonId: integer; ADOB: TDateTime; AGenderId: integer; AFirstName: string; ALastName: string;
  ANationalId: string );
begin
  FSQL.ExecuteCommand( CMD_UPDATE_PERSON, [APersonId, ADOB, AGenderId, Trim( AFirstName ), Trim( ALastName ), ANationalId] );
end;

procedure TPersonManager.UpdatePerson( const APersonId: integer; APerson: IPersonReadOnly );
begin
  EditPerson( APersonId, APerson.DOB, APerson.GenderId, APerson.FirstName, APerson.LastName, APerson.NationalId );
end;

end.
