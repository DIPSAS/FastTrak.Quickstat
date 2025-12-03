{$REGION 'Documentation'}
/// <summary>
/// Emetra.Classes.Subject contains base classes to inherit from that support the Observer and the Singleton pattern.
/// TExposed replaces TInterfacedObject and TInterfacedPersistent.
/// </summary>
{$ENDREGION}
unit Emetra.Classes.Subject;

{$DEFINE Debug}

interface

uses
  Emetra.Interfaces.Observer,
  Emetra.Dictionary.Interfaces,
  Emetra.Logging.Interfaces,
  {VCL}
  System.Classes, System.Contnrs, System.Types,
  Generics.Defaults, Generics.Collections;

type

{$REGION 'Documentation'}
  /// <summary>
  /// Intented to replace TObject and TInterfacedObject as a generic base class that also adds the ability to respond
  /// to a macro handler.
  /// </summary>
{$ENDREGION}
  TExposed = class( TInterfacedPersistent, IVariantDictionary )
  strict private
    FAfterConstructionCalled: boolean;
    FBeforeDestructionCalled: boolean;
{$IFDEF Audit}
  private
    procedure CheckForMultipleSingletons;
{$ENDIF}
  protected
    FSingleton: boolean;
    function Get_Persistent: boolean;
    function RestName: string; dynamic;
    procedure CheckAssigned( AInterface: IInterface; const AName: string );
    procedure VerifyConstructorParameters; dynamic;
    function TryGetValue( const AVarName: string; var AValue: variant ): boolean; dynamic;
  public
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    property Singleton: boolean read FSingleton;
    property Persistent: boolean read Get_Persistent;
  end;

  TExposedLogged = class( TExposed, ILog )
  strict private
    FLog: ILog;
  protected
    procedure VerifyConstructorParameters; override;
    function Get_Log: ILog;
  protected
    procedure EnterMethod( const AProcName: string );
    procedure LeaveMethod( const AProcName: string );
  public
    constructor Create( ALog: ILog ); reintroduce; dynamic;
    property Log: ILog read Get_Log implements ILog;
  end;

{$REGION 'Documentation'}
  /// <summary>
  /// Simple base class for implementation of the Observer pattern.  Use this class to inherit the ability to notify
  /// a list of observers of changes to the instance.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Use TContainedObservable for compositions, where TContainedObservable is inside a controller.
  /// </para>
  /// <para>
  /// Please note that TObservable is a persistent class, similar to TObject and unlike TInterfacedObject.
  /// </para>
  /// </remarks>
{$ENDREGION}

  TObservable = class( TExposedLogged, IObservable )
  strict private
    FUpdateLevel: integer;
  private
  protected
    FObservers: TObjectList;
    procedure Clear; dynamic;
    procedure NotifyObservers; dynamic;
    procedure Notify( AObserver: IListener ); dynamic;
    function Controller: TObject; dynamic;
  public
    { Initialization }
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { Other members }
    function Updating: boolean;
    procedure Attach( AObserver: IListener ); dynamic;
    procedure Detach( AObserver: IListener ); dynamic;
    procedure BeginUpdate;
    procedure CancelUpdate;
    procedure EndUpdate;
    procedure DetachAll;
  end;

  TExposedContainer = class( TExposedLogged )
  private
    function GetObject( AIndex: integer ): TObject;
  protected
    FOwnedObjects: TObjectList;
    { Property Accessors }
    function Get_Count: integer;
    { Other members }
    procedure Sort( ACompareProc: TListSortCompare );
    function AddOwnedObject( AObject: TObject ): TObject;
    procedure Remove( AObject: TObject ); overload;
    procedure Remove( AIndex: integer ); overload;
  public
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    function GetEnumerator: TListEnumerator;
    procedure Clear; dynamic;
    property Items[AIndex: integer]: TObject read GetObject; default;
    property Count: integer read Get_Count;
  end;

{$REGION 'Documentation'}
  /// <summary>
  /// Identical to TObservable, except that it is always reference counted, and uses the Owner when it broadcasts
  /// updates to the observer list.
  /// </summary>
  /// <remarks>
  /// Should only be used through its interface, and only as part of a composition.  Instead of notifying observers
  /// about itself, it notifies with the FController as the Sender parameter.
  /// </remarks>
{$ENDREGION}

  TContainedObservable = class( TObservable, IObservable )
  strict private
    FController: TObject;
  protected
    function Controller: TObject; override;
  public
    constructor Create( AController: TObject; ALog: ILog ); reintroduce; dynamic;
  end;

implementation

uses
{$IFDEF Audit}
  Emetra.Classes.Auditing,
{$ENDIF}
  System.SysUtils, System.TypInfo, System.Math;

resourcestring
  SMultipleSingletons = 'Class %s should be Singleton';
  SInstanceNotFound = 'Class %s not found in GlobalClassCounter. Please remember to put "inherited;" at the start of all AfterConstruction overrides. ';
  SExtraEndUpdate = '%s.EndUpdate called without matching BeginUpdate';
  SClearWithoutBeginUpdate = '%s.Clear called without BeginUpdate';
  SObserversStillAttached = '%s.Destroy: Object still has %d registered observers!';
  SNotifyObservers = 'NotifyObserver';

type
{$REGION 'Documentation'}
  /// <summary>
  /// The exception is raised when there is an attempt to create more than one instance of an object that should be a
  /// singleton.
  /// </summary>
{$ENDREGION}
  ESingletonException = class( Exception );

{$REGION 'Documentation'}
  /// <summary>
  /// This exception is raised when the global class counter thinks that there are no more live instances of this
  /// class.
  /// </summary>
{$ENDREGION}
  EInstanceMissing = class( Exception );

{$REGION 'Documentation'}
  /// <summary>
  /// This exception is raised when EndUpdate or Clear is called with no matching BeginUpdate.
  /// </summary>
{$ENDREGION}
  EMissingBeginUpdate = class( Exception );

{$REGION 'TBaseObject'}

procedure TExposed.AfterConstruction;
begin
  inherited;
  Assert( FAfterConstructionCalled = false );
  FAfterConstructionCalled := true;
{$IFDEF Audit}
  CheckForMultipleSingletons;
{$ENDIF}
  VerifyConstructorParameters;
{$IFDEF Audit}
  GlobalClassCounter.AddInstance( Self.QualifiedClassName );
{$ENDIF}
end;

procedure TExposed.BeforeDestruction;
{$IFDEF Audit}
var
  preExistingInstances: integer;
{$ENDIF}
begin
  Assert( FAfterConstructionCalled = true, Format( 'AfterConstruction not called for %s', [ClassName] ) );
  Assert( FBeforeDestructionCalled = false, Format( 'BeforeDestruction called already for %s', [ClassName] ) );
  FBeforeDestructionCalled := true;
{$IFDEF Audit}
  preExistingInstances := GlobalClassCounter[Self.QualifiedClassName];
  GlobalClassCounter.RemoveInstance( Self.QualifiedClassName );
  if preExistingInstances = 0 then
  begin
    if Assigned( GlobalLog ) then
      GlobalLog.Event( Format( SInstanceNotFound, [Self.QualifiedClassName] ), ltCritical )
    else
      raise EInstanceMissing.CreateFmt( SInstanceNotFound, [Self.QualifiedClassName] );
  end;
{$ENDIF}
  inherited;
end;

function TExposed.Get_Persistent: boolean;
begin
  Result := true;
end;

procedure TExposed.VerifyConstructorParameters;
begin
  { No parameters }
end;

procedure TExposed.CheckAssigned( AInterface: IInterface; const AName: string );
const
  ERR_MSG = '%s.CheckAssigned: Interface %s not assigned';
begin
  if not Assigned( AInterface ) then
  begin
    if Assigned( GlobalLog ) then
      GlobalLog.SilentError( ERR_MSG, [ClassName, AName] );
    raise EArgumentNilException.CreateFmt( ERR_MSG, [ClassName, AName] );
  end;
end;

{$IFDEF Audit}

procedure TExposed.CheckForMultipleSingletons;
var
  preExistingInstances: integer;
begin
  if FSingleton then
  begin
    preExistingInstances := GlobalClassCounter[Self.QualifiedClassName];
    if ( preExistingInstances > 0 ) then
    begin
      if Assigned( GlobalLog ) then
        GlobalLog.Event( SMultipleSingletons, [Self.QualifiedClassName], ltCritical )
      else
        raise ESingletonException.CreateFmt( SMultipleSingletons, [Self.QualifiedClassName] );
    end;
  end;
end;
{$ENDIF}

function TExposed.TryGetValue( const AVarName: string; var AValue: variant ): boolean;
begin
  Result := IsPublishedProp( Self, AVarName );
  if Result then
    AValue := GetPropValue( Self, AVarName );
end;

function TExposed.RestName: string;
begin
  Result := Copy( ClassName, 2, maxint );
end;

{$ENDREGION}
{$REGION 'TSubject'}

procedure TObservable.AfterConstruction;
begin
  inherited;
  FObservers := TObjectList.Create( false );
end;

procedure TObservable.BeforeDestruction;
begin
  if Assigned( FObservers ) then
  begin
{$IFDEF Debug}
    if ( FObservers.Count > 0 ) and Assigned( GlobalLog ) then
      GlobalLog.SilentWarning( SObserversStillAttached, [Controller.QualifiedClassName, FObservers.Count] );
{$ENDIF}
{$IFDEF Audit}
    SafeFree( FObservers );
{$ELSE}
    FObservers.Free;
{$ENDIF}
  end;
  inherited;
end;

procedure TObservable.Notify( AObserver: IListener );
begin
  AObserver.AfterUpdate( Controller );
end;

procedure TObservable.Attach( AObserver: IListener );
const
  LOG_ATTACHMENT = '%s.Attach(%s): %s';
var
  newObject: TObject;
begin
  newObject := TObject( AObserver );
  if FObservers.IndexOf( newObject ) = -1 then
  begin
{$IFDEF Debug}
    Log.Event( LOG_ATTACHMENT, [Controller.ClassName, newObject.QualifiedClassName, 'Added'] );
{$ENDIF}
    FObservers.Add( AObserver as TObject );
  end
  else
    Log.SilentWarning( LOG_ATTACHMENT, [Controller.ClassName, newObject.QualifiedClassName, 'Added before, will not add again.'] );
end;

procedure TObservable.Detach( AObserver: IListener );
begin
{$IFDEF Debug}
  Log.Event( '%s.Detach(%s)', [Controller.ClassName, TObject( AObserver ).QualifiedClassName] );
{$ENDIF}
  FObservers.Remove( AObserver as TObject );
end;

procedure TObservable.DetachAll;
begin
{$IFDEF Debug}
  Log.Event( '%s.DetachAll(n=%d)', [Controller.ClassName, FObservers.Count] );
{$ENDIF}
  FObservers.Clear;
end;

procedure TObservable.Clear;
begin
{$IFDEF Debug}
  if FUpdateLevel < 1 then
    raise EMissingBeginUpdate.CreateFmt( SClearWithoutBeginUpdate, [ClassName] );
{$ENDIF}
end;

function TObservable.Controller: TObject;
begin
  Result := Self;
end;

procedure TObservable.BeginUpdate;
begin
  inc( FUpdateLevel );
end;

procedure TObservable.CancelUpdate;
begin
  dec( FUpdateLevel );
end;

procedure TObservable.EndUpdate;
begin
{$IFDEF Debug}
  if FUpdateLevel < 1 then
    raise EMissingBeginUpdate.CreateFmt( SExtraEndUpdate, [ClassName] );
{$ENDIF}
  if FUpdateLevel > 0 then
    dec( FUpdateLevel );
  if ( FUpdateLevel = 0 ) then
    NotifyObservers;
end;

procedure TObservable.NotifyObservers;
const
  LOG_CALL = '%s.NotifyObservers(%d): %s';
var
  observerIndex: integer;
  thisObserver: IListener;
begin
  if FObservers.Count = 0 then
    exit;
  EnterMethod( SNotifyObservers );
  try
    observerIndex := 0;
    while observerIndex < FObservers.Count do
    begin
      if Supports( FObservers[observerIndex], IListener, thisObserver ) then
        try
{$IFDEF Debug}
          Log.Event( LOG_CALL, [Controller.ClassName, observerIndex, TObject( thisObserver ).QualifiedClassName] );
{$ENDIF}
          Notify( thisObserver );
        except
          on E: Exception do
            Log.SilentError( LOG_CALL, [Controller.ClassName, observerIndex, E.Message] );
        end;
      inc( observerIndex );
    end;
  finally
    LeaveMethod( SNotifyObservers );
  end;
end;

function TObservable.Updating: boolean;
begin
  Result := FUpdateLevel > 0;
end;

{$ENDREGION}
{$REGION 'TContainedSubject' }

function TContainedObservable.Controller: TObject;
begin
  Result := FController;
end;

constructor TContainedObservable.Create( AController: TObject; ALog: ILog );
begin
  inherited Create( ALog );
  FController := AController;
end;

{$ENDREGION}
{$REGION 'TLoggedObject'}

procedure TExposedLogged.VerifyConstructorParameters;
begin
  inherited;
  CheckAssigned( FLog, 'Logger' );
end;

function TExposedLogged.Get_Log: ILog;
begin
  Result := FLog;
end;

procedure TExposedLogged.EnterMethod( const AProcName: string );
begin
  FLog.EnterMethod( Self, AProcName );
end;

procedure TExposedLogged.LeaveMethod( const AProcName: string );
begin
  FLog.LeaveMethod( Self, AProcName );
end;

constructor TExposedLogged.Create( ALog: ILog );
begin
  FLog := ALog;
  inherited Create;
end;
{$ENDREGION}
{$REGION 'TBaseContaier' }

function TExposedContainer.AddOwnedObject( AObject: TObject ): TObject;
begin
  Result := AObject;
  FOwnedObjects.Add( AObject );
end;

procedure TExposedContainer.AfterConstruction;
begin
  inherited;
  FOwnedObjects := TObjectList.Create( true );
end;

procedure TExposedContainer.BeforeDestruction;
begin
  inherited;
  FOwnedObjects.Free;
end;

procedure TExposedContainer.Clear;
begin
  FOwnedObjects.Clear;
end;

function TExposedContainer.GetEnumerator: TListEnumerator;
begin
  Result := FOwnedObjects.GetEnumerator;
end;

function TExposedContainer.GetObject( AIndex: integer ): TObject;
begin
  Result := FOwnedObjects[AIndex];
end;

function TExposedContainer.Get_Count: integer;
begin
  Result := FOwnedObjects.Count;
end;

procedure TExposedContainer.Remove( AObject: TObject );
begin
  FOwnedObjects.Remove( AObject );
end;

procedure TExposedContainer.Remove( AIndex: integer );
begin
  FOwnedObjects.Delete( AIndex );
end;

procedure TExposedContainer.Sort( ACompareProc: TListSortCompare );
begin
  FOwnedObjects.Sort( ACompareProc );
end;

{$ENDREGION}

end.
