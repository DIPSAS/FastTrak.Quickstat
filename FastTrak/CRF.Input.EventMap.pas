unit CRF.Input.EventMap;

interface

uses
  { Project interfaces }
  CRF.Input.Interfaces,
  { General interfaces }
  Emetra.Database.Interfaces,
  { Standard }
  Classes;

type
  TCRFEventMapper = class( TInterfacedPersistent, ILoginObserver, ICRFEventMap )
  strict private
    fSQL: ISQL;
    fEventScale: integer;
    fDateOffset: integer;
  protected
    function Get_EventsPerDay: integer;
    function Get_EventScale: integer;
    function EventNoToDate(const AEventNum: integer): TDateTime;
    function EventDateToNo( const ADateTime: TDateTime ): integer;
    function FriendlyName: string;
  public
    procedure AfterLogin( Sender: IDatabaseConnection );
  published
    property EventScale: integer read Get_EventScale;
    property EventsPerDay: integer read Get_EventScale;
    property DateOffset: Integer read FDateOffset;
  end;

implementation

uses
  SysUtils;

const
  QRY_GET_DATA = 'EXEC dbo.GetDatabaseInfo';
  FLD_EVENT_SCALE = 'EventScale';

{ TCRFEventMapper }

procedure TCRFEventMapper.AfterLogin(Sender: IDatabaseConnection);
begin
  if Supports( Sender, ISQL, FSQL ) then
  with FSQL.FastQuery( QRY_GET_DATA ) do
  try
    FEventScale := FieldByName(FLD_EVENT_SCALE).AsInteger;
    FDateOffset := 1;
  finally
    Close;
  end;
end;

function TCRFEventMapper.Get_EventScale: integer;
begin
  Result := FEventScale;
end;

function TCRFEventMapper.Get_EventsPerDay: integer;
begin
  Result := FEventScale;
end;

function TCRFEventMapper.EventNoToDate(const AEventNum: integer): TDateTime;
begin
  Result := AEventNum / FEventScale + FDateOffset;
end;

function TCRFEventMapper.EventDateToNo(const ADateTime: TDateTime): integer;
begin
  Result := trunc( ( ADateTime - FDateOffset + 1/24/60/60 ) * FEventScale );
end;

function TCRFEventMapper.FriendlyName: string;
begin
  Result := 'EventMapper';
end;

end.
