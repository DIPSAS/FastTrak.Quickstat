unit CRF.Patient.List;

interface

uses
  {Project}
  CRF.SQL,
  CRF.Context.Session.Interfaces,
  CRF.Person,
  CRF.Person.StudyCase,
  CRF.Study.Interfaces,
  CRF.Population.Interfaces,
  {General}
  Emetra.Classes.Auditing,
  Emetra.Classes.Business,
  Emetra.Classes.Tokenizer,
  Emetra.Dates.Utils,
  {General interfaces}
  Emetra.Logging.Interfaces,
  Emetra.Database.Interfaces,
  Emetra.Database.ParameterDictionary.Interfaces,
  Emetra.Person.Interfaces,
  Emetra.Interfaces.Observer,
  Emetra.Dictionary.Interfaces,
  {Standard}
  Generics.Collections,
  Classes, Contnrs, DB, SysUtils;

resourcestring
  StrListHeader = 'Pasienter';
  StrActivePatients = 'Aktive pasienter';
  StrFriendlyName = 'Pasientliste';

type
  TPatientList = class( TBusiness, IPersonList, IListener, IStudyObserver, IEnumerator )
  strict private
    fCaption: string;
    fLastStudyId: integer;
    fItemIndex: integer;
    fList: TObjectList<TStudyCase>;
    fSQL: ISQL;
    fStudyContext: IStudyContext;
    fTokens: TTokenizer;
    fParamValues: TParams;
    fParameterDictionary: IParameterDictionary;
  private
    function ParseDateOfBirth( ATrimmedText: string ): TDate;
    function TryFindPeople( const ASearchText: string; out ADataset: TDataset ): boolean;
  protected
    { IPersonList }
    function Get_Count: integer;
    function Get_Name: string;
    function Get_Person( AIndex: integer ): IPersonReadOnly;
    function Get_Usable: boolean;
    { Other property accessors }
    function Get_Header: string;
    function Get_Item( AIndex: integer ): TStudyCase;
    function Get_ItemIndex: integer;
    { Other members }
    function FriendlyName: string;
    function GetCurrent: TObject;
    procedure VerifyConstructorParameters; override;
    procedure AfterUpdate( Sender: TObject );
    procedure SelectCurrent;
  public
    { Initialization }
    constructor Create( AStudyContext: IStudyContext; AParameterDictionary: IParameterDictionary; ADatabase: ISQL; ALog: ILog ); reintroduce;
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { IPersonList }
    function Search( const ASearchText: string ): integer;
    { IStudyObserver }
    procedure AfterStudyChange( const Sender: IStudyId );
    { Other members }
    function TryFind( const APersonId: integer; out AFoundAt: integer ): boolean;
    function CanMoveNext: boolean;
    function CanMovePrevious: boolean;
    function MoveNext: boolean;
    function MovePrevious: boolean;
    procedure Load( const APopulation: IPopulation );
    procedure Query( const AQuery: string );
    procedure Reset;
    { Properties }
    property Items[AIndex: integer]: TStudyCase read Get_Item; default;
    property Person[AIndex: integer]: IPersonReadOnly read Get_Person;
  published
    property Caption: string read fCaption write fCaption;
    property Count: integer read Get_Count;
    property ItemIndex: integer read Get_ItemIndex;
    property StudyName: string read Get_Name;
  end;

implementation

uses
  {General}
  Emetra.Person.SQL,
  {Standard}
  System.RegularExpressions, System.Variants;

const
  { Searches are limited to invividuals already enrolled in study, see #498565. }
  JOIN_STUDY = ' JOIN dbo.StudCase sc ON sc.StudyId=:StudyId AND sc.PersonId = p.PersonId ';

  { Queries with "fuzzy" criteria, i.e. that may get more than one hit. }
  QRY_STUDY_PERSON_BY_DOB       = SELECT_PERSON + JOIN_STUDY + 'WHERE p.DOB = :DOB' + TAIL_ORDER_BY;
  QRY_STUDY_PERSON_BY_DOB_NAME  = SELECT_PERSON + JOIN_STUDY + 'WHERE p.DOB = :DOB AND p.LstName LIKE :PartialLastName' + TAIL_ORDER_BY;
  QRY_STUDY_PERSON_BY_LAST_NAME = SELECT_PERSON + JOIN_STUDY + 'WHERE p.LstName LIKE :SearchFor' + TAIL_ORDER_BY;

{$REGION 'Initialization'}

constructor TPatientList.Create( AStudyContext: IStudyContext; AParameterDictionary: IParameterDictionary; ADatabase: ISQL; ALog: ILog );
begin
  inherited Create( ALog );
  fStudyContext := AStudyContext;
  fSQL := ADatabase;
  fParameterDictionary := AParameterDictionary;
  Reset;
end;

procedure TPatientList.AfterConstruction;
begin
  inherited;
  fTokens := TTokenizer.Create;
  fList := TObjectList<TStudyCase>.Create( true );
  fParamValues := TParams.Create( nil );
end;

procedure TPatientList.BeforeDestruction;
begin
  fList.Clear;
  fParamValues.Free;
  fList.Free;
  fTokens.Free;
  inherited;
end;

procedure TPatientList.VerifyConstructorParameters;
begin
  inherited;
  CheckAssigned( fSQL, 'Database' );
  CheckAssigned( fStudyContext, 'StudyContext' );
end;

{$ENDREGION}
{$REGION 'Simple Accessors}

function TPatientList.Get_Count: integer;
begin
  Result := fList.Count;
end;

function TPatientList.Get_Header: string;
begin
  Result := StrListHeader;
end;

function TPatientList.Get_Person( AIndex: integer ): IPersonReadOnly;
begin
  Supports( fList[AIndex], IPersonReadOnly, Result );
end;

function TPatientList.Get_Item( AIndex: integer ): TStudyCase;
begin
  Result := fList[AIndex] as TStudyCase;
end;

function TPatientList.Get_ItemIndex: integer;
begin
  Result := fItemIndex;
end;

function TPatientList.Get_Name: string;
begin
  Result := fStudyContext.StudyName;
end;

function TPatientList.Get_Usable: boolean;
begin
  Result := true;
end;

{$ENDREGION}
{$REGION 'IEnumerate'}

function TPatientList.GetCurrent: TObject;
begin
  if fList.Count = 0 then
    Result := nil
  else if fItemIndex < fList.Count then
    Result := fList[fItemIndex]
  else
    Result := nil;
end;

function TPatientList.CanMoveNext;
begin
  Result := ( fItemIndex < fList.Count - 1 );
end;

function TPatientList.MoveNext: boolean;
begin
  Result := CanMoveNext;
  if Result then
    inc( fItemIndex );
end;

procedure TPatientList.Reset;
begin
  fItemIndex := -1;
end;

function TPatientList.CanMovePrevious: boolean;
begin
  Result := ( fItemIndex > 0 );
end;

function TPatientList.MovePrevious: boolean;
begin
  Result := CanMovePrevious;
  if Result then
    dec( fItemIndex );
end;

function TPatientList.ParseDateOfBirth( ATrimmedText: string ): TDate;
begin
  Result := 0;
  try
    { Correction for search done on Danish CPR-number }
    if ( Length( ATrimmedText ) > 7 ) and CharInSet( ATrimmedText[1], ['0' .. '3'] ) and ( Pos( '-', ATrimmedText ) = 7 ) then
      ATrimmedText := fTokens.Extract( ATrimmedText, 0, '-' );
    { See if SearchText is a birthday }
    if Length( ATrimmedText ) >= 6 then
      Result := GetDate( ATrimmedText );
  except
    on EConvertError do
      { Ignore exception in conversion }
    else
  end;
end;

{$ENDREGION}

function TPatientList.Search( const ASearchText: string ): integer;
var
  personDataset: TDataset;
  StudyCase: TStudyCase;
begin
  BeginUpdate;
  try
    fList.Clear;
    Reset;
    if TryFindPeople( ASearchText, personDataset ) then
      try
        while not personDataset.EOF do
        begin
          StudyCase := TStudyCase.Create( fStudyContext, fSQL, Log );
          try
            StudyCase.Load( personDataset );
            fList.Add( StudyCase );
          except
            on E: Exception do
              StudyCase.Free;
          end;
          personDataset.Next;
        end;
      finally
        personDataset.Close;
      end;
  finally
    Result := fList.Count;
    EndUpdate;
  end;
end;

procedure TPatientList.SelectCurrent;
begin
  fList[fItemIndex].SelectStatus( )
end;

procedure TPatientList.Load( const APopulation: IPopulation );
begin
  Query( APopulation.QueryText );
end;

procedure TPatientList.Query( const AQuery: string );
var
  n: integer;
  qryParams: array of variant;
  thisDataset: TDataset;
  thisStudyCase: TStudyCase;
  infoText: TField;
begin
  CheckAssigned( fParameterDictionary, 'ParameterDictionary' );
  BeginUpdate;
  try
    if ( fStudyContext.StudyId > 0 ) and fParameterDictionary.TryApplyParameters( AQuery, fParamValues ) then
    begin
      fList.Clear;
      SetLength( qryParams, fParamValues.Count );
      n := 0;
      while n < fParamValues.Count do
      begin
        qryParams[n] := fParamValues[n].Value;
        inc( n );
      end;
      thisDataset := fSQL.FastQuery( AQuery, qryParams );
      try
        infoText := thisDataset.FindField( 'InfoText' );
        while not thisDataset.EOF do
        begin
          thisStudyCase := TStudyCase.Create( fStudyContext, fSQL, Log );
          try
            thisStudyCase.Load( thisDataset );
            { Workarounds }
            thisStudyCase.FullName := thisDataset.FieldByName( FLD_FULL_NAME ).AsString;
            if Assigned( infoText ) then
              thisStudyCase.StatusText := infoText.AsString;
            fList.Add( thisStudyCase );
          except
            on E: Exception do
              thisStudyCase.Free;
          end;
          thisDataset.Next;
        end;
      finally
        thisDataset.Close;
      end;
    end;
  finally
    EndUpdate;
  end;
end;

function TPatientList.TryFind( const APersonId: integer; out AFoundAt: integer ): boolean;
begin
  AFoundAt := 0;
  while AFoundAt < fList.Count do
  begin
    if Items[AFoundAt].PersonId = APersonId then
    begin
      Result := true;
      exit;
    end;
    inc( AFoundAt );
  end;
  Result := false;
end;

function TPatientList.TryFindPeople( const ASearchText: string; out ADataset: TDataset ): boolean;
var
  searchDOB: TDate;
  searchPersonId: integer;
  searchText: string;
  dateMatch: TMatch;
  textMatch: TMatch;
begin
  ADataset := nil;
  Result := false;
  searchText := Trim( ASearchText );
  searchPersonId := StrToIntDef( searchText, 0 );
  dateMatch := TRegEx.Match( searchText, RGX_DATE );
  textMatch := TRegEx.Match( searchText, RGX_NAME );
  if dateMatch.Success then
    searchDOB := ParseDateOfBirth( dateMatch.Value )
  else
    searchDOB := 0;
  try
    if searchText = EmptyStr then
      exit
    else if TRegEx.IsMatch( searchText, RGX_SPLIT_NATIONAL_ID ) then
      ADataset := fSQL.FastQuery( QRY_PERSON_BY_NATID, [TRegEx.Replace( searchText, '\s', EmptyStr )] )
    else if TRegEx.IsMatch( searchText, RGX_DOB_AND_NAME ) and textMatch.Success and dateMatch.Success then
      ADataset := fSQL.FastQuery( QRY_STUDY_PERSON_BY_DOB_NAME, [fStudyContext.StudyId, searchDOB, textMatch.Value + '%'] )
    else if searchDOB <> 0 then
      ADataset := fSQL.FastQuery( QRY_STUDY_PERSON_BY_DOB, [fStudyContext.StudyId, FormatDateTime( 'yyyy-mm-dd', searchDOB )] )
    else if ( searchPersonId > 0 ) then
      ADataset := fSQL.FastQuery( QRY_PERSON_BY_ID, [searchPersonId] )
    else if TRegEx.IsMatch( searchText, RGX_NAME ) then
      ADataset := fSQL.FastQuery( QRY_STUDY_PERSON_BY_LAST_NAME, [ fStudyContext.StudyId, searchText + '%'] );
    Result := Assigned( ADataset );
  except
    on E: Exception do
      Log.Event( '%s.TryFindPeople("%s"): %s', [ClassName, ASearchText, E.Message], ltError );
  end;
end;

function TPatientList.FriendlyName: string;
begin
  Result := StrFriendlyName;
end;

{$REGION 'IListener and IStudyObserver'}

procedure TPatientList.AfterStudyChange( const Sender: IStudyId );
const
  PROC_NAME = 'AfterStudyChange';
begin
  Assert( Sender.StudyId = fStudyContext.StudyId, 'StudyId mismatch' );
  Log.EnterMethod( Self, Format( '%s(StudyId=%d)', [PROC_NAME, fStudyContext.StudyId] ) );
  BeginUpdate;
  try
    fCaption := StrActivePatients;
    if fStudyContext.StudyId = 0 then
      fList.Clear
    else
      Query( QRY_GET_CASELIST );
    fLastStudyId := fStudyContext.StudyId;
  finally
    EndUpdate;
    Log.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TPatientList.AfterUpdate( Sender: TObject );
const
  PROC_NAME = 'AfterUpdate';
begin
  Log.EnterMethod( Self, PROC_NAME );
  try
    AfterStudyChange( fStudyContext );
  finally
    Log.LeaveMethod( Self, PROC_NAME );
  end;
end;

{$ENDREGION}

end.
