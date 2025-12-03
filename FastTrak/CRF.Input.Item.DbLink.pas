unit CRF.Input.Item.DbLink;

{$M+}

interface

uses
  CRF.Input.Interfaces;

type
  TDbLink = class( TObject )
  private
    fChangeCount: integer;
    fRowId: integer;
    fSetByEventNo: integer;
    fSetByEventId: integer;
    fLockStatus: TCRFItemLockStatus;
    fReadStatus: TCRFItemReadStatus;
    fOriginalValue: string;
    function Get_DateStr: string;
    function Get_EventTime: TDateTime;
  public
    class var EventMap: ICRFEventMap;
    function CanLock: boolean;
    function Lock: boolean;
    procedure Clear( const AOriginalValue: string = '' );
    procedure ClearDbLinks;
    procedure Identify( const AEventNum, AEventId, ARowId: integer );
    procedure SetOriginalStatus(const AReadStatus: TCRFItemReadStatus; const ALockStatus: TCRFItemLockStatus; const ARowId, AChangeCount: integer);
    { Properties }
    property LockStatus: TCRFItemLockStatus read FLockStatus;
    property ReadStatus: TCRFItemReadStatus read FReadStatus;
  published
    property ChangeCount: integer read FChangeCount;
    property DateStr: string read Get_DateStr;
    property EventTime: TDateTime read Get_EventTime;
    property OriginalValue: string read FOriginalValue;
    property RowId: integer read FRowId;
    property SetByEventNo: integer read FSetByEventNo;
    property SetByEventId: integer read FSetByEventId;
  end;

implementation

{ TEvent }

uses
  SysUtils;

function TDbLink.CanLock: boolean;
begin
  Result := FLockStatus = lckUnlocked;
end;

procedure TDbLink.Clear( const AOriginalValue: string = '' );
begin
  FOriginalValue := AOriginalValue;
  ClearDbLinks;
end;

procedure TDbLink.ClearDbLinks;
begin
  FChangeCount := 0;
  FRowId := 0;
  FSetByEventId := 0;
  FSetByEventNo := 0;
  FLockStatus := lckUnlocked;
  FReadStatus := rsUndefined;
end;

function TDbLink.Get_DateStr: string;
begin
  Assert( Assigned( EventMap ) );
  if FSetByEventNo = 0 then
    Result := ''
  else if Assigned( EventMap ) then
    Result := DateToStr( EventMap.EventNoToDate( FSetByEventNo ) )
  else
    Result := 'Unassigned(EventMap)';
end;

function TDbLink.Get_EventTime: TDateTime;
begin
  Assert( Assigned( EventMap ) );
  Result := EventMap.EventNoToDate( FSetByEventNo );
end;

procedure TDbLink.Identify(const AEventNum, AEventId, ARowId: integer);
begin
  FSetByEventNo := AEventNum;
  FSetByEventId := AEventId;
  FRowId := ARowId;
end;

function TDbLink.Lock: boolean;
begin
  Result := false;
  if CanLock then
  begin
    FLockStatus := lckYesButUnsaved;
    Result := True;
  end;
end;

procedure TDbLink.SetOriginalStatus;
begin
  FReadStatus := AReadStatus;
  FLockStatus := ALockStatus;
  FChangeCount := AChangeCount;
  FRowId := ARowId;
end;

end.
