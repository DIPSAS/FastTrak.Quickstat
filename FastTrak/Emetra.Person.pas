unit Emetra.Person;
{$M+}

interface

uses
  {General classes}
  Emetra.Classes.Subject,
  {General interfaces}
  Emetra.Person.Interfaces,
  Emetra.Interfaces.Observer,
  Emetra.Interfaces.Geography,
  {Standard}
  System.Classes, System.SysUtils;

type
  TPerson = class( TObservable, IPersonId, IPersonIdentity, IPerson, IPersonReadOnly, IObservable, IPhysicalAddress )
  strict private
    FEventsEnabled: Boolean;
    FNationalId: string;
    FPersonId: integer;
  private
    FDOB: TDate;
    FFirstName: string;
    FMiddleName: string;
    FLastName: string;
    FSex: TSex;
    fStreetAddress: string;
    fCity: string;
    fPostalCode: string;
    fEmail: string;
    fPhone: string;
    fEmployeeNumber: integer;
    fHPRNo: integer;
  protected
    { Property accessors }
    function Get_Age: Double;
    function Get_City: string;
    function Get_DOB: TDate;
    function Get_FirstName: string;
    function Get_FullName: string;
    function Get_GenderId: integer;
    function Get_HPRNo: integer;
    function Get_LastName: string;
    function Get_MiddleName: string;
    function Get_NationalId: string;
    function Get_PersonId: integer;
    function Get_Phone: string;
    function Get_PostCode: string;
    function Get_Sex: TSex;
    function Get_SexStr: string;
    function Get_StreetAddress: string;
    function Get_VisualId: string;
    function Get_YOB: integer;
    procedure Set_DOB( const Value: TDate );
    procedure Set_Email( const Value: string );
    procedure Set_FirstName( const Value: string );
    procedure Set_FullName( const Value: string );
    procedure Set_GenderId( const AValue: integer );
    procedure Set_LastName( const Value: string );
    procedure Set_MiddleName( const Value: string );
    procedure Set_NationalId( const Value: string );
    procedure Set_PersonId( const Value: integer );
    procedure Set_Phone( const Value: string );
    procedure Set_PostCode( const Value: string );
    procedure Set_City( const Value: string );
    procedure Set_Sex( Value: TSex );
  public
    Tag: integer;
    function DOBName( const AISODate: Boolean = false; const ASpacer: string = ' ' ): string; dynamic;
    function Female: Boolean;
    function Male: Boolean;
    function ShortId: string;
    function Valid: Boolean;
    procedure Assign( ASource: TPerson ); reintroduce;
    procedure ChangeId( const ANewPersonId: integer );
    procedure Clear; override;
    procedure SetAddress( const AStreetAddress, APostalCode, ACity: string );
    property EventsEnabled: Boolean read FEventsEnabled write FEventsEnabled;
  published
    property Age: Double read Get_Age;
    property DOB: TDate read Get_DOB write Set_DOB;
    property EmployeeNumber: integer read fEmployeeNumber write fEmployeeNumber;
    property FirstName: string read Get_FirstName write Set_FirstName;
    property FullName: string read Get_FullName write Set_FullName;
    property GenderId: integer read Get_GenderId write Set_GenderId;
    property HPRNo: integer read Get_HPRNo write fHPRNo;
    property LastName: string read Get_LastName write Set_LastName;
    property MiddleName: string read Get_MiddleName write Set_MiddleName;
    property name: string read Get_FullName;
    property NationalId: string read Get_NationalId write Set_NationalId;
    property PersonId: integer read FPersonId write Set_PersonId;
    property Sex: TSex read Get_Sex write Set_Sex;
    property SexStr: string read Get_SexStr;
    property VisualId: string read Get_VisualId;
    property YOB: integer read Get_YOB;
    { Geographical data }
    property City: string read Get_City write Set_City;
    property PostCode: string read Get_PostCode write Set_PostCode;
    property StreetAddress: string read fStreetAddress;
    { Contact info }
    property Email: string read fEmail write Set_Email;
    property Phone: string read Get_Phone write Set_Phone;
  end;

implementation

uses
  System.DateUtils;

const
  DOB_ISO_FORMAT = 'yyyy-mm-dd';

function TPerson.Get_Age: Double;
begin
  Result := YearSpan( Now, FDOB );
end;

procedure TPerson.Assign( ASource: TPerson );
begin
  BeginUpdate;
  try
    FPersonId := ASource.PersonId;
    FDOB := ASource.DOB;
    FFirstName := ASource.FirstName;
    FMiddleName := ASource.MiddleName;
    FLastName := ASource.LastName;
    FSex := ASource.Sex;
    FNationalId := ASource.NationalId;
    FEventsEnabled := ASource.EventsEnabled;
    Tag := ASource.Tag;
    fStreetAddress := ASource.StreetAddress;
    fCity := ASource.City;
    fEmail := ASource.Email;
    fPhone := ASource.Phone;
    fPostalCode := ASource.PostCode;
    fEmployeeNumber := ASource.EmployeeNumber;
  finally
    EndUpdate;
  end;
end;

procedure TPerson.ChangeId( const ANewPersonId: integer );
begin
  PersonId := ANewPersonId;
end;

procedure TPerson.Clear;
begin
  BeginUpdate;
  try
    FPersonId := 0;
    FSex := sexUnknown;
    FFirstName := EmptyStr;
    FMiddleName := EmptyStr;
    FLastName := EmptyStr;
    FDOB := 0;
    FNationalId := EmptyStr;
    fCity := EmptyStr;
    fStreetAddress := EmptyStr;
    fPostalCode := EmptyStr;
    fEmail := EmptyStr;
    fPhone := EmptyStr;
    fEmployeeNumber := 0;
  finally
    EndUpdate;
  end;
end;

function TPerson.DOBName( const AISODate: Boolean = false; const ASpacer: string = ' ' ): string;
begin
  if AISODate then
    Result := FormatDateTime( DOB_ISO_FORMAT, FDOB ) + ASpacer + FullName
  else
    Result := DateToStr( DOB ) + ASpacer + FullName;
end;

function TPerson.Female: Boolean;
begin
  Result := ( FSex = sexFemale );
end;

function TPerson.Get_DOB: TDate;
begin
  Result := FDOB;
end;

function TPerson.Get_FirstName: string;
begin
  Result := FFirstName;
end;

function TPerson.Get_FullName: string;
begin
  if FMiddleName <> EmptyStr then
    Result := FFirstName + ' ' + Copy( FMiddleName, 1, 1 ) + '. ' + FLastName
  else
    Result := Trim( FFirstName + ' ' + FLastName );
end;

function TPerson.Get_GenderId: integer;
begin
  Result := ord( FSex );
end;

function TPerson.Get_HPRNo: integer;
begin
  Result := fHPRNo;
end;

function TPerson.Get_NationalId: string;
begin
  Result := FNationalId;
end;

function TPerson.Get_PersonId: integer;
begin
  Result := PersonId;
end;

function TPerson.Get_Phone: string;
begin
  Result := fPhone;
end;

function TPerson.Get_Sex: TSex;
begin
  Result := FSex;
end;

function TPerson.Get_SexStr: string;
begin
  Result := SexToStr( FSex );
end;

function TPerson.Get_VisualId: string;
begin
  if not Valid then
    Result := StrVisualIdNobody
  else
  begin
    if NationalId = EmptyStr then
      Result := DateToStr( FDOB )
    else
      Result := Copy( NationalId, 1, 6 ) + ' ' + Copy( NationalId, 7, maxint );
    Result := Trim( Result ) + ' - ' + FullName;
  end;
end;

function TPerson.Get_YOB;
begin
  Result := YearOf( FDOB );
end;

function TPerson.Get_LastName: string;
begin
  Result := FLastName;
end;

function TPerson.Get_MiddleName: string;
begin
  Result := FMiddleName;
end;

function TPerson.Male: Boolean;
begin
  Result := ( FSex = sexMale );
end;

procedure TPerson.Set_City( const Value: string );
begin
  if Value = fCity then
    exit;
  BeginUpdate;
  try
    fCity := Value;
  finally
    EndUpdate;
  end;
end;

procedure TPerson.Set_DOB( const Value: TDate );
begin
  if Value = FDOB then
    exit;
  BeginUpdate;
  try
    FDOB := Value;
  finally
    EndUpdate;
  end;
end;

procedure TPerson.Set_Email( const Value: string );
begin
  BeginUpdate;
  try
    fEmail := Value;
  finally
    EndUpdate;
  end;
end;

procedure TPerson.Set_FirstName( const Value: string );
begin
  if Value = FFirstName then
    exit;
  BeginUpdate;
  try
    FFirstName := Value;
  finally
    EndUpdate;
  end;
end;

procedure TPerson.Set_MiddleName( const Value: string );
begin
  if Value = FMiddleName then
    exit;
  BeginUpdate;
  try
    FMiddleName := Value;
  finally
    EndUpdate;
  end;
end;

procedure TPerson.Set_FullName( const Value: string );
var
  lstNames: TStringList;
  strFullName: string;
begin
  BeginUpdate;
  lstNames := TStringList.Create;
  try
    strFullName := Trim( Value );
    lstNames.Delimiter := ',';
    lstNames.StrictDelimiter := true;
    lstNames.DelimitedText := strFullName;
    if strFullName = EmptyStr then
    begin
      FLastName := EmptyStr;
      FFirstName := EmptyStr;
    end
    else if lstNames.Count = 2 then
    begin
      FLastName := Trim( lstNames[0] );
      FFirstName := Trim( lstNames[1] );
    end
    else
    begin
      lstNames.DelimitedText := Trim( Value );
      FLastName := lstNames[lstNames.Count - 1];
      lstNames.Delete( lstNames.Count - 1 );
      FFirstName := lstNames.DelimitedText;
    end;
  finally
    lstNames.Free;
    EndUpdate;
  end;
end;

procedure TPerson.Set_GenderId( const AValue: integer );
begin
  if AValue = ord( FSex ) then
    exit;
  BeginUpdate;
  try
    case AValue of
      1: FSex := sexMale;
      2: FSex := sexFemale;
    else FSex := sexUnknown;
    end;
  finally
    EndUpdate;
  end;
end;

procedure TPerson.Set_LastName( const Value: string );
begin
  BeginUpdate;
  try
    FLastName := Value;
  finally
    EndUpdate;
  end;
end;

procedure TPerson.Set_NationalId( const Value: string );
begin
  if Value = FNationalId then
    exit;
  BeginUpdate;
  try
    FNationalId := Value;
  finally
    EndUpdate;
  end;
end;

procedure TPerson.Set_PersonId( const Value: integer );
begin
  if Value = FPersonId then
    exit;
  BeginUpdate;
  try
    Clear;
    FPersonId := Value;
  finally
    EndUpdate;
  end;
end;

procedure TPerson.Set_Phone( const Value: string );
begin
  if Value = fPhone then
    exit;
  BeginUpdate;
  try
    fPhone := Value
  finally
    EndUpdate;
  end;
end;

procedure TPerson.Set_PostCode( const Value: string );
begin
  if Value = fPostalCode then
    exit;
  BeginUpdate;
  try
    fPostalCode := Value;
  finally
    EndUpdate;
  end;
end;

procedure TPerson.Set_Sex( Value: TSex );
begin
  if Value = FSex then
    exit;
  BeginUpdate;
  try
    FSex := Value;
  finally
    EndUpdate;
  end;
end;

function TPerson.ShortId: string;
begin
  Result := FormatDateTime( StrDobFormat, DOB );
  if ( Length( FirstName ) > 0 ) and ( Length( MiddleName ) > 0 ) and ( Length( LastName ) > 0 ) then
    Result := Result + FirstName[1] + MiddleName[1] + LastName[1]
  else if ( Length( FirstName ) > 0 ) and ( Length( LastName ) > 0 ) then
    Result := Result + FirstName[1] + LastName[1];
end;

function TPerson.Valid: Boolean;
begin
  Result := PersonId > 0;
end;

{$REGION 'IGeoLocation'}

procedure TPerson.SetAddress( const AStreetAddress, APostalCode, ACity: string );
begin
  BeginUpdate;
  try
    fStreetAddress := AStreetAddress;
    fPostalCode := APostalCode;
    fCity := ACity;
  finally
    EndUpdate;
  end;
end;

function TPerson.Get_City: string;
begin
  Result := fCity;
end;

function TPerson.Get_StreetAddress: string;
begin
  Result := fStreetAddress;
end;

function TPerson.Get_PostCode: string;
begin
  Result := fPostalCode;
end;

{$ENDREGION}

end.
