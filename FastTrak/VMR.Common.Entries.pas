unit VMR.Common.Entries;

interface

uses
  {Medical}
  VMR.Lab.Interfaces,
  VMR.Common.Interfaces,
  VMR.User.Interfaces,
  {Standard}
  Contnrs, ActiveX, Classes, Db, Math, SysUtils, XSBuiltIns;

const
  GET_LAST_LAB = 999999;

type
  { TVmrCustomFragment is the base class for all entries in the VMR.  This includes labdata, which are shown as groups and not indvidual rows }
  TVmrCustomFragment = class( TInterfacedPersistent )
  strict private
    fGUID: TGUID; { Unique identifier for this element }
  protected
    fFragmentType: TEpjFragmentType; { The type of information in this element, slightly different from class }
    fTimeStamp: TDateTime;
    fTag: integer;
    fShowDate: boolean; { Hiding the date for duplicated dates is useful, less clutter }
    fSortOrder: int64; { Sort order to override order of data with same TimeStamp and same entry type }
    fSource: TVmrDataSource;
    fMatchedExpression: string; { Substring that matched a successful Match this instance }
    function Get_TimeStamp: TDateTime;
    function AsSubject: string;
  public
    constructor Create( const ATimeStamp: TDateTime; const ASource: TVmrDataSource ); reintroduce;
    function AsString: string; dynamic;
    function LongAgo( const ATimeSpan: double ): boolean;
    function Recent( const ATimeSpan: double ): boolean;
    function Match( const ARegEx: string ): boolean;
    function MatchAgain: boolean;
    procedure UpdateTimeStamp( const ATimeStamp: TDateTime );
  published
    property FragmentType: TEpjFragmentType read fFragmentType;
    property TimeStamp: TDateTime read Get_TimeStamp;
    property GUID: TGUID read FGUID;
    property ShowDate: boolean read FShowDate write FShowDate;
    property SortOrder: int64 read FSortOrder;
    property Source: TVmrDataSource read FSource;
    property Tag: integer read FTag write FTag;
  end;

  { TVmrRowFragment is the base class for all entries that appear as separate lines in the VMR }
  TVmrRowFragment = class( TVmrCustomFragment, IVmrUserList )
  strict private
    fOnChange: TNotifyEvent;
    class var fSignatures: IVmrUserList;
  protected
    fDataset: TDataset;
    { Property accessors }
    function Get_OnChange: TNotifyEvent;
    function Get_ShowDate: boolean;
    procedure Set_OnChange( Value: TNotifyEvent );
    { Other members }
    function AsHtml: string; dynamic;
    function AsHtmlRow: string; dynamic;
    function Between( const ATime1, ATime2: TDateTime ): boolean;
    function RowStart: string;
    function GetSignatureHtml( const AUserId: integer ): string;
    { Read from dataset }
    function ReadDateTime( const AFieldName: string ): TDateTime;
    function ReadInteger( const AFieldName: string ): integer;
    function ReadString( const AFieldName: string ): string;
    function ReadFloat( const AFieldName: string ): double;
  public
    class procedure SetSignatures( const ASignatures: IVmrUserList );
    class var EventScale: integer;
    function TryGetUser( const AUserId: integer; out AUser: IVmrUser ): boolean;
    function GetUserName( const AUserId: integer ): string;
    function GetSignature( const AUserId: integer ): string;
    procedure TriggerChange;
    { Properties }
    property OnChange: TNotifyEvent read Get_OnChange write Set_OnChange;
    property ShowDate: boolean read Get_ShowDate write FShowDate;
  end;

  TVmrStoredFragment = class( TVmrRowFragment, IVmrFragment )
  strict private
    fEventId: integer; { Read only, used for Praksisprofil and FastTrak }
    fSignedBy: integer;
    fCreatedBy: integer;
  private
    fMarked: boolean; { Allows toggle marked/unmarked }
    fVisible: boolean; { Allows data to be hidden from views }
    { Property accessors }
    function Get_FragmentType: TEpjFragmentType;
    procedure Set_ShowDate( AValue: boolean );
  protected
    { Other members }
    function AsContent: string; dynamic;
    function GetSignedBySign: string;
    function GetCreatedBySign: string;
    function GetSignedByHtml: string;
    function GetCreatedByHtml: string;
  public
    constructor Create( const AFragmentType: TEpjFragmentType; const ATimeStamp: TDateTime; const ASource: TVmrDataSource ); reintroduce; dynamic;
    function SameAs( AEntry: TVmrStoredFragment ): boolean; dynamic;
    function SameData( AEntry: IVmrFragment ): boolean;
    function TimeAgo( const ADate: TDateTime = GET_LAST_LAB ): double;
    procedure Assign( ASource: TVmrStoredFragment ); reintroduce; dynamic;
    { Properties }
    property CreatedBy: integer read FCreatedBy write FCreatedBy;
    property FragmentType: TEpjFragmentType read Get_FragmentType;
    property MatchedBy: string read FMatchedExpression;
    property SignedBy: integer read FSignedBy write FSignedBy;
    property Visible: boolean read FVisible write FVisible;
  end;

  { TVmrCustomNumericFragment is base class for single point numeric data, like lab values }

  TVmrCustomNumericFragment = class( TVmrStoredFragment )
  private
    fLabClass: ILabClass;
    fArithmeticComparator: TArithmeticComp;
  protected
    fValue: double;
    fCaption: string;
    { Property accessors }
    function Get_ArithmeticComp: TArithmeticComp;
    function Get_Caption: string;
    function Get_FriendlyName: string;
    function Get_LabTest: TLabTest;
    function Get_LabClass: ILabClass;
    function Get_LabClassId: integer;
    function Get_FurstId: integer;
    function Get_Value: double;
    function Get_VarName: string;
    function Get_LoincCode: string;
  public
    function AsHtml: string; override;
    function AsInteger: integer;
    function AsPasteData: string;
    function SameAs( AEntry: TVmrStoredFragment ): boolean; override;
    procedure Assign( ASource: TVmrCustomNumericFragment ); reintroduce;
    procedure ClassifyAs( const ALabClass: ILabClass );
    { Properties }
    property ArithmeticComp: TArithmeticComp read Get_ArithmeticComp write FArithmeticComparator;
    property Caption: string read FCaption;
    property FriendlyName: string read Get_FriendlyName;
    property LabClass: ILabClass read Get_LabClass;
    property LabTest: TLabTest read Get_LabTest;
    property LoincCode: string read Get_LoincCode;
    property VarName: string read Get_VarName;
    property Value: double read Get_Value;
  end;

const
  NBSP   = '&nbsp;';
  TD_END = '</td>';

  HTML_MAINCELL = '<td class="%s">';

  { Wrappers }
  SPAN_TINY = '<font size="1" color="silver">%s</font>';

  { Sharable Html }
  HTML_BOLD_FIRST     = '<b>%s</b> %s ';
  HTML_BOLD_BOTH      = '<b>%s %s</b> ';
  HTML_DATE_SRC       = '<td class="TVmrDate">%s</td><td class="TVmrSource">%s</td>';
  HTML_DATE_TIME_SRC  = '<td class="TVmrDate">%s <span class="TVmrTime">Kl: %s</span></td><td class="TVmrSource">%s</td>';
  HTML_DSSN           = '<b>%s:</b> %s';
  HTML_REASON         = HTML_DSSN;

  { AsString templates }
  STR_NOTE = '%d %s'#9'%s';
  STR_DX   = '%s/%x'#9'%s';

resourcestring
  TXT_RXONGOING = 'Fast';
  TXT_RXCOUNT = 'No';
  TXT_RXREFILL = 'Reit';
  TXT_RXDOSE = 'Dssn';
  TXT_RXREASON = 'Indikasjon';
  TXT_RXREF = 'Ref';
  TXT_RXSTOP = 'Seponert';
  TXT_RXSTOP_PLAN = 'Planlagt seponering';
  TXT_STOPREASON = 'Årsak';
  TXT_RELATION = 'sammenheng';
  TXT_STATUS = 'Status';
  TXT_CHARS = 'tegn';
  TXT_GOAL = 'mål';
  TXT_ADR = 'bivirkning';
  TXT_DECIDED_BY = 'Bestemt av %s';

const
  { XML Tags for prescription }
  TAG_RX              = 'rx';
  TAG_STOPPEDBEFORE   = 'rxstoppedbefore';
  TAG_RXCODE          = 'rxcode';
  TAG_RXDOSE          = 'rxdose';
  TAG_RXDOSE24H       = 'rxdose24h';
  TAG_RXNAME          = 'rxname';
  TAG_RXREF           = 'rxref';
  TAG_RXREIT          = 'rxreit';
  TAG_RXSTOP          = 'rxstop';
  TAG_RXSTART         = 'rxstart';
  TAG_RXSTRENGTH      = 'rxstrength';
  TAG_RXSTOPPEDBEFORE = 'rxstoppedbefore';
  TAG_RXSIZE          = 'rxno';

{$IFDEF DotNet}
function CompEntriesByDate( p1, p2: TObject ): integer;
{$ELSE}
  { *************************************************************
    CompEntriesByDate is a helper function to sort the Virtual
    Medical Record in chronological order. It will also sort
    according to entry type, labtest order number, and finally on
    FSortOrder. The last allows blood pressure data to be sorted
    in descending order, making it easier to pick the lowest BP
    on a certain date.
    ************************************************************* }
function CompEntriesByDate( p1, p2: pointer ): integer;
{$ENDIF}

var
  MatchCodeOnly: boolean = false; {
    Only match DxCodes on regex search.  Sometimes codes are mentioned
    in other codes, like "Heart problems ex K77" }

implementation

uses
  Emetra.Classes.RegEx;

var
  GlobalVmrRegEx: TRegExEngine;
  AsStringDateFormat: string = 'YYYYMMDD';

{$REGION 'Compare'}
{$WARNINGS OFF}
{$IFDEF DotNet}

function CompEntriesByDate( p1, p2: TObject ): integer;
{$ELSE}

function CompEntriesByDate( p1, p2: pointer ): integer;
{$ENDIF}
var
  t1, t2: TVmrStoredFragment;
begin
  t1 := TVmrStoredFragment( p1 );
  t2 := TVmrStoredFragment( p2 );
  Result := sign( t2.TimeStamp - t1.TimeStamp );
  if Result = 0 then
    Result := VMR_SORT_ORDER[t1.fFragmentType] - VMR_SORT_ORDER[t2.fFragmentType];
  {
    if ( Result = 0 ) and ( t1.InheritsFrom( TVmrCustomNumericFragment ) and t2.InheritsFrom( TVmrCustomNumericFragment ) ) then begin
    Result := ord( TVmrCustomNumericFragment( t1 ).Source ) - ord( TVmrCustomNumericFragment( t2 ).Source );
    if Result = 0 then
    Result := ord( TVmrCustomNumericFragment( t1 ).Labtest ) - ord( TVmrCustomNumericFragment( t2 ).LabTest );
    if Result = 0 then
    Result := Sign( TVmrCustomNumericFragment( t1 ).Value - TVmrCustomNumericFragment( t2 ).Value );
    end;
  }
  if ( Result = 0 ) then
    Result := t2.FSortOrder - t1.FSortOrder;
end;
{$WARNINGS ON}
{$ENDREGION}
{ TVmrCustomFragment }

constructor TVmrCustomFragment.Create( const ATimeStamp: TDateTime; const ASource: TVmrDataSource );
begin
  CoCreateGUID( FGUID );
  FTimeStamp := ATimeStamp; // + EntryCount / 24 / 60 / 60 / 1000000;
  FSource := ASource;
end;

function TVmrCustomFragment.Get_TimeStamp: TDateTime;
begin
  Result := FTimeStamp;
end;

procedure TVmrCustomFragment.UpdateTimeStamp( const ATimeStamp: TDateTime );
begin
  FTimeStamp := ATimeStamp;
end;

function TVmrCustomFragment.LongAgo( const ATimeSpan: double ): boolean;
begin
  Result := ( Now - TimeStamp >= ATimeSpan );
end;

function TVmrCustomFragment.Recent( const ATimeSpan: double ): boolean;
begin
  Result := ( Now - TimeStamp <= ATimeSpan );
end;

function TVmrCustomFragment.Match( const ARegEx: string ): boolean;
begin
  GlobalVmrRegEx.RegEx := ARegEx;
  GlobalVmrRegEx.Subject := AsSubject;
  Result := GlobalVmrRegEx.Match;
  if Result then
    FMatchedExpression := GlobalVmrRegEx.MatchedExpression
  else
    FMatchedExpression := EmptyStr;
end;

function TVmrCustomFragment.MatchAgain: boolean;
begin
  Result := GlobalVmrRegEx.MatchAgain;
end;

function TVmrCustomFragment.AsString;
begin
  Result := FormatDateTime( AsStringDateFormat, FTimeStamp ) + #9 + VMR_SOURCE_IDS[FSource] + #9;
end;

function TVmrCustomFragment.AsSubject: string;
begin
  Result := Copy( AsString, Length( AsStringDateFormat ) + 1, maxint );
end;

function TVmrRowFragment.RowStart: string;
var
  strDate, strTime, strTimeFormat: string;
  addHour, addMin: boolean;
begin
  if Source <> vmrCrfForm then
  begin
    addHour := true;
    addMin := true;
  end else
  begin
    addHour := EventScale > 1;
    addMin := EventScale > 24;
  end;
  strDate := FormatDateTime( FormatSettings.ShortDateFormat, TimeStamp );
  if addHour then
  begin
    if addMin
      then strTimeFormat := FormatSettings.ShortTimeFormat
      else strTimeFormat := 'hh';
    strTime := FormatDateTime( strTimeFormat, TimeStamp );
    Result := Format( HTML_DATE_TIME_SRC, [strDate, strTime, VMR_SOURCE_IDS[FSource]] );
  end
  else Result := Format( HTML_DATE_SRC, [strDate, VMR_SOURCE_IDS[FSource]] )
end;

procedure TVmrRowFragment.Set_OnChange( Value: TNotifyEvent );
begin
  FOnChange := Value;
end;

function TVmrRowFragment.Get_OnChange;
begin
  Result := FOnChange;
end;

function TVmrRowFragment.AsHtml: string;
begin
  Result := RowStart;
end;

function TVmrRowFragment.AsHtmlRow: string;
begin
  Result := Format( '<tr id="%s" class="TVmrRowEntry">', [GuidToString( GUID )] ) + AsHtml + '</tr>';
end;

function TVmrRowFragment.Between( const ATime1, ATime2: TDateTime ): boolean;
begin
  Result := ( FTimeStamp >= ATime1 ) and ( FTimeStamp < ATime2 );
end;

procedure TVmrRowFragment.TriggerChange;
begin
  if Assigned( FOnChange ) then
    FOnChange( Self );
end;

class procedure TVmrRowFragment.SetSignatures( const ASignatures: IVmrUserList );
begin
  fSignatures := ASignatures;
end;

function TVmrRowFragment.TryGetUser(const AUserId: Integer; out AUser: IVmrUser): boolean;
var
  vmrUser: IVmrUser;
begin
  AUser := nil;
  Result := Assigned( fSignatures ) and fSignatures.TryGetUser( AUserId, vmrUser ) and Supports( vmrUser, IVmrUser, AUser );
end;

function TVmrRowFragment.GetUserName( const AUserId: integer ): string;
var
  thisUser: IVmrUser;
begin
  if AUserId = 0 then
    Result := 'public'
  else if TryGetUser( AUserId, thisUser ) then
    Result := thisUser.FullName
  else
    Result := Format( 'U%d', [AUserId] )
end;

function TVmrRowFragment.GetSignature(const AUserId: Integer): string;
var
  thisUser: IVmrUser;
begin
  if AUserId = 0 then
    Result := 'public'
  else if TryGetUser( AUserId, thisUser ) then
    Result := Format( '%s (%s)', [thisUser.UserName, thisUser.Signature] )
  else
    Result := Format( 'U%d', [AUserId] )
end;


function TVmrRowFragment.GetSignatureHtml(const AUserId: integer): string;
begin
  Result := Format( '<span class="Signature">%s</span>', [GetUserName( AUserId )] );
end;

function TVmrRowFragment.Get_ShowDate: boolean;
begin
  Result := FShowDate;
end;

{$REGION 'TVMREntry'}

constructor TVmrStoredFragment.Create( const AFragmentType: TEpjFragmentType; const ATimeStamp: TDateTime; const ASource: TVmrDataSource );
begin
  inherited Create( ATimeStamp, ASource );
  fFragmentType := AFragmentType;
end;

function TVmrStoredFragment.Get_FragmentType: TEpjFragmentType;
begin
  Result := fFragmentType;
end;

function TVmrStoredFragment.AsContent: string;
begin
  Result := EmptyStr;
end;

function TVmrStoredFragment.TimeAgo( const ADate: TDateTime = GET_LAST_LAB ): double;
begin
  if ADate = GET_LAST_LAB then
    Result := Now - TimeStamp
  else
    Result := ADate - TimeStamp;
end;

procedure TVmrStoredFragment.Assign( ASource: TVmrStoredFragment );
begin
  { GUID and Owner is not copied, inherited not called }
  FEventId := ASource.FEventId;
  FMarked := ASource.FMarked;
  FMatchedExpression := ASource.FMatchedExpression;
  FShowDate := ASource.FShowDate;
  FSignedBy := ASource.FSignedBy;
  FSortOrder := ASource.FSortOrder;
  FSource := ASource.FSource;
  FTag := ASource.FTag;
  FTimeStamp := ASource.FTimeStamp;
  FVisible := ASource.FVisible;
end;

function TVmrStoredFragment.SameAs( AEntry: TVmrStoredFragment ): boolean;
begin
  Result := Assigned( AEntry ) and ( AEntry.ClassType = Self.ClassType ) and SameValue( AEntry.FTimeStamp, FTimeStamp ) and ( AEntry.Source = FSource );
end;

function TVmrStoredFragment.SameData( AEntry: IVmrFragment ): boolean;
begin
  Result := false;
end;

procedure TVmrStoredFragment.Set_ShowDate( AValue: boolean );
begin
  FShowDate := AValue;
end;

function TVmrRowFragment.ReadFloat( const AFieldName: string ): double;
var
  fld: TField;
begin
  Result := -1;
  if Assigned( FDataset ) and not( FDataset.EOF ) then
  begin
    fld := FDataset.FindField( AFieldName );
    if Assigned( fld ) then
      Result := fld.AsFloat;
  end;
end;

function TVmrRowFragment.ReadInteger( const AFieldName: string ): integer;
var
  fld: TField;
begin
  Result := -1;
  if Assigned( FDataset ) and not( FDataset.EOF ) then
  begin
    fld := FDataset.FindField( AFieldName );
    if Assigned( fld ) then
      Result := fld.AsInteger;
  end;
end;

function TVmrRowFragment.ReadDateTime( const AFieldName: string ): TDateTime;
var
  fld: TField;
begin
  Result := -1;
  if Assigned( FDataset ) and not( FDataset.EOF ) then
  begin
    fld := FDataset.FindField( AFieldName );
    if Assigned( fld ) then
      Result := fld.AsDateTime;
  end;
end;

function TVmrRowFragment.ReadString( const AFieldName: string ): string;
var
  fld: TField;
begin
  Result := EmptyStr;
  if Assigned( FDataset ) and not( FDataset.EOF ) then
  begin
    fld := FDataset.FindField( AFieldName );
    if Assigned( fld ) then
      Result := fld.AsString;
  end;
end;

{$ENDREGION}
{$REGION 'TVmrNumericFragment'}

procedure TVmrCustomNumericFragment.Assign( ASource: TVmrCustomNumericFragment );
begin
  inherited Assign( ASource );
  FArithmeticComparator := ASource.FArithmeticComparator;
  FValue := ASource.Value;
  FCaption := ASource.Caption;
  FLabClass := ASource.LabClass;
end;

function TVmrCustomNumericFragment.AsPasteData: string;
begin
  Result := Format( '%s: %g ', [VarName, Value] );
end;

procedure TVmrCustomNumericFragment.ClassifyAs( const ALabClass: ILabClass );
begin
  FLabClass := ALabClass;
end;

function TVmrCustomNumericFragment.Get_Value: double;
begin
  Result := FValue;
end;

function TVmrCustomNumericFragment.Get_LabTest: TLabTest;
begin
  if Assigned( FLabClass ) then
    Result := FLabClass.LabTest
  else
    Result := ltUnclassified;
end;

function TVmrCustomNumericFragment.Get_LabClassId: integer;
begin
  if Assigned( FLabClass ) then
    Result := ord( FLabClass.LabClassId )
  else
    Result := 0;
end;

function TVmrCustomNumericFragment.Get_LabClass: ILabClass;
begin
  Result := FLabClass;
end;

function TVmrCustomNumericFragment.Get_LoincCode: string;
begin
  if Assigned( FLabClass ) then
    Result := FLabClass.LoincCode
  else
    Result := EmptyStr;
end;

function TVmrCustomNumericFragment.Get_VarName: string;
begin
  if Assigned( FLabClass ) then
    Result := FLabClass.VarName
  else
    Result := EmptyStr;
end;

function TVmrCustomNumericFragment.Get_ArithmeticComp: TArithmeticComp;
begin
  Result := FArithmeticComparator;
end;

function TVmrCustomNumericFragment.Get_Caption: string;
begin
  Result := FCaption;
end;

function TVmrCustomNumericFragment.Get_FriendlyName: string;
begin
  if Assigned( FLabClass ) then
    Result := FLabClass.FriendlyName
  else
    Result := FCaption;
end;

function TVmrCustomNumericFragment.Get_FurstId: integer;
begin
  if Assigned( FLabClass ) then
    Result := FLabClass.FurstId
  else
    Result := 0;
end;

function TVmrCustomNumericFragment.AsHtml: string;
begin
  Result := inherited AsHtml + '(not implemented)' + TD_END;
end;

function TVmrCustomNumericFragment.SameAs( AEntry: TVmrStoredFragment ): boolean;
begin
  Result := inherited SameAs( AEntry ) and ( TVmrCustomNumericFragment( AEntry ).LabTest = LabTest ) and
    SameValue( TVmrCustomNumericFragment( AEntry ).Value, Value ) and SameText( TVmrCustomNumericFragment( AEntry ).Caption, FCaption );
end;

function TVmrCustomNumericFragment.AsInteger: integer;
begin
  Result := Round( Value );
end;

{$ENDREGION}

function TVmrStoredFragment.GetSignedBySign: string;
begin
  Result := GetUserName( FSignedBy );
end;

function TVmrStoredFragment.GetSignedByHtml: string;
begin
  Result := Format( '<span class="Signature">%s</span>', [GetSignedBySign] );
end;

function TVmrStoredFragment.GetCreatedBySign: string;
begin
  Result := GetUserName( FCreatedBy );
end;

function TVmrStoredFragment.GetCreatedByHtml: string;
begin
  Result := Format( '<span class="Creator">%s</span>', [GetCreatedBySign] );
end;

initialization

GlobalVmrRegEx := TRegExEngine.Create( nil );

finalization

GlobalVmrRegEx.Free;

end.
