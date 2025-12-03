unit Emetra.Classes.Business;

interface

uses
  {General classes}
  Emetra.Classes.Subject,
  Emetra.Business.BaseClass,
  {General interfaces}
  Emetra.Logging.Interfaces,
  Emetra.Dictionary.Interfaces,
  Emetra.Interfaces.Observer,
  {Standard}
  System.Classes, System.Generics.Collections;

type

  /// <summary>
  ///   This is a business object that adds the IObservable interface, allowing
  ///   for a multicast notification of changes to the object.
  /// </summary>
  /// <remarks>
  ///   <para>
  ///     For the observer mechanism to work, descendants should make sure
  ///     that they call the protected methods <b>BeginUpdate</b> and <b>
  ///     EndUpdate</b> within a try..finally block like this:
  ///   </para>
  ///   <para>
  ///     <c>BeginUpdate; try // Your code here. finally EndUpdate; end;</c>
  ///   </para>
  /// </remarks>
  TBusiness = class( TCustomBusiness, IObservable, IVariantDictionary )
  strict private
    FContainedObservable: TContainedObservable;
  protected
    { Variant dictionary }
    function TryGetValue( const AVarName: string; var AValue: variant ): boolean; dynamic;
    { Expose for internal use }
    procedure BeginUpdate;
    procedure EndUpdate;
    procedure CancelUpdate;
    { Only used to support "implements" construct, never used for anything }
    property _Notifier: TContainedObservable read FContainedObservable implements IObservable;
  public
    { Initialization }
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { Other members }
    function Updating: boolean;
    procedure Attach( AObserver: IListener );
    procedure Detach( AObserver: IListener );
    procedure DetachAll;
  end;

  /// <summary>
  ///   This contains a dictionary of <b>TCustomBusinessObject</b> descendants.
  ///   In can be used to find objects by name, and it will also manage the
  ///   lifetime of the objects, i.e. when the catalog is freed, every object
  ///   in is also freed.
  /// </summary>
  TBusinessObjectCatalog = class( TCustomBusiness )
  strict private
    fLog: ILog;
    fCatalog: TObjectDictionary<string, TCustomBusiness>;
  protected
    property Log: ILog read fLog;
  public
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    procedure AddBusinessObject( const AName: string; ABusinessObject: TCustomBusiness );
  end;

implementation

uses
  {Standard}
  System.SysUtils, System.TypInfo;

function TBusiness.TryGetValue( const AVarName: string; var AValue: variant ): boolean;
begin
  Result := IsPublishedProp( Self, AVarName );
  if Result then
    AValue := GetPropValue( Self, AVarName );
end;

function TBusiness.Updating: boolean;
begin
  Result := FContainedObservable.Updating;
end;

procedure TBusiness.AfterConstruction;
begin
  inherited;
  VerifyConstructorParameters;
  FContainedObservable := TContainedObservable.Create( Self, Log );
end;

procedure TBusiness.BeforeDestruction;
begin
  FreeAndNil( FContainedObservable );
  inherited;
end;

procedure TBusiness.BeginUpdate;
begin
  FContainedObservable.BeginUpdate;
end;

procedure TBusiness.EndUpdate;
begin
  FContainedObservable.EndUpdate;
end;

procedure TBusiness.CancelUpdate;
begin
  FContainedObservable.CancelUpdate;
end;

procedure TBusiness.Attach( AObserver: IListener );
begin
  FContainedObservable.Attach( AObserver );
end;

procedure TBusiness.Detach( AObserver: IListener );
begin
  FContainedObservable.Detach( AObserver );
end;

procedure TBusiness.DetachAll;
begin
  FContainedObservable.DetachAll;
end;

{ TBusinessObjectCatalog }

procedure TBusinessObjectCatalog.AddBusinessObject( const AName: string; ABusinessObject: TCustomBusiness );
begin
  fCatalog.Add( AName, ABusinessObject );
end;

procedure TBusinessObjectCatalog.AfterConstruction;
begin
  inherited;
  fCatalog := TObjectDictionary<string, TCustomBusiness>.Create( [doOwnsValues] );
end;

procedure TBusinessObjectCatalog.BeforeDestruction;
begin
  fCatalog.Free;
  inherited;
end;

end.
