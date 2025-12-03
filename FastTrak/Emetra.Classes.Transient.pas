unit Emetra.Classes.Transient;

interface

uses
  Emetra.Interfaces.Observer,
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces,
  Generics.Collections;

type
  TTransient = class( TInterfacedObject )
  strict private
    fLog: ILog;
  protected
    property Log: ILog read fLog;
  public
    constructor Create( ALog: ILog );
  end;

  TTransientObservable = class( TTransient, IObservable )
  strict private
    FUpdateLevel: integer;
  private
  protected
    FObservers: TList<IListener>;
    procedure Clear; dynamic;
    { Observer interface }
    procedure NotifyObservers; dynamic;
    procedure Notify( AObserver: IListener ); dynamic;
    function Controller: TObject; dynamic;
    procedure Attach( AObserver: IListener ); dynamic;
    procedure Detach( AObserver: IListener ); dynamic;
    procedure BeginUpdate;
    procedure CancelUpdate;
    procedure EndUpdate;
    procedure DetachAll;
  public
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
  end;

  TTransientConnected = class( TTransient )
  strict private
    fSQL: ISQL;
  protected
    property SQL: ISQL read fSQL;
  public
    constructor Create( ASQL: ISQL; ALog: ILog );
  end;

implementation

uses
  SysUtils;

resourcestring
  SExtraEndUpdate = '%s.EndUpdate called without matching BeginUpdate';
  SClearWithoutBeginUpdate = '%s.Clear called without BeginUpdate';
  SObserversStillAttached = '%s.Destroy: Object still has %d registered observers!';
  SNotifyObservers = 'NotifyObserver';

type
  ENestingError = class( Exception );

  { TTransient }

constructor TTransient.Create( ALog: ILog );
begin
  inherited Create;
  fLog := ALog;
end;

{ TTransientConnected }

constructor TTransientConnected.Create( ASQL: ISQL; ALog: ILog );
begin
  inherited Create( ALog );
  fSQL := ASQL;
end;

procedure TTransientObservable.AfterConstruction;
begin
  inherited;
  FObservers := TList<IListener>.Create;
end;

procedure TTransientObservable.BeforeDestruction;
begin
  FObservers.Free;
  inherited;
end;

procedure TTransientObservable.Notify( AObserver: IListener );
begin
  AObserver.AfterUpdate( Controller );
end;

procedure TTransientObservable.Attach( AObserver: IListener );
const
  LOG_ATTACHMENT = '%s.Attach(%s): %s';
var
  newObject: TObject;
begin
  newObject := TObject( AObserver );
  if FObservers.IndexOf( AObserver ) = -1 then
  begin
    Log.Event( LOG_ATTACHMENT, [Controller.ClassName, newObject.QualifiedClassName, 'Added'] );
    FObservers.Add( AObserver );
    AObserver.AfterUpdate( Self );
  end
  else
    Log.SilentWarning( LOG_ATTACHMENT, [Controller.ClassName, newObject.QualifiedClassName, 'Added before, will not add again.'] );
end;

procedure TTransientObservable.Detach( AObserver: IListener );
begin
  Log.Event( '%s.Detach(%s)', [Controller.ClassName, TObject( AObserver ).QualifiedClassName] );
  FObservers.Remove( AObserver );
end;

procedure TTransientObservable.DetachAll;
begin
  Log.Event( '%s.DetachAll(n=%d)', [Controller.ClassName, FObservers.Count] );
  FObservers.Clear;
end;

procedure TTransientObservable.Clear;
begin
  Log.Event( '%s.Clear(n=%d)', [Controller.ClassName, FObservers.Count] );
  { Override in descendants }
end;

function TTransientObservable.Controller: TObject;
begin
  Result := Self;
end;

procedure TTransientObservable.BeginUpdate;
begin
  inc( FUpdateLevel );
end;

procedure TTransientObservable.CancelUpdate;
begin
  dec( FUpdateLevel );
end;

procedure TTransientObservable.EndUpdate;
begin
  if FUpdateLevel < 1 then
    raise ENestingError.CreateFmt( SExtraEndUpdate, [ClassName] );
  if FUpdateLevel > 0 then
    dec( FUpdateLevel );
  if ( FUpdateLevel = 0 ) then
    NotifyObservers;
end;

procedure TTransientObservable.NotifyObservers;
const
  LOG_CALL = '%s.NotifyObservers(%d): %s';
var
  observerIndex: integer;
  thisObserver: IListener;
begin
  if FObservers.Count = 0 then
    exit;
  Log.EnterMethod( Self, SNotifyObservers );
  try
    observerIndex := 0;
    while observerIndex < FObservers.Count do
    begin
      if Supports( FObservers[observerIndex], IListener, thisObserver ) then
        try
          Log.Event( LOG_CALL, [Controller.ClassName, observerIndex, TObject( thisObserver ).QualifiedClassName] );
          Notify( thisObserver );
        except
          on E: Exception do
            Log.SilentError( LOG_CALL, [Controller.ClassName, observerIndex, E.Message] );
        end;
      inc( observerIndex );
    end;
  finally
    Log.LeaveMethod( Self, SNotifyObservers );
  end;
end;

end.
