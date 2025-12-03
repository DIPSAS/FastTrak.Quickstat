unit Emetra.Business.BaseClass;

interface

uses
  {General interfaces}
  Emetra.Logging.Interfaces,
  {Standard}
  System.Classes, System.Generics.Collections;

type
  /// <summary>
  /// Business objects are lightweight objects that can be used as base classes
  /// for more complex objects that contain business logic. Typically they
  /// are not used for smaller data objects with limited scope. Data objects may not
  /// need a logger, and they usually don't need to implement interfaces. More
  /// complex data objects like <b>TPatient</b> in an EHR application may
  /// need both of these. Because TCustomBusiness inherits from <b>TInterfacedPersistent</b> (a
  /// standard RTL class) it is not reference counted and must be freed when
  /// it is no longer used. If it it is added to a <b>TBusinessObjectCatalog</b>,
  /// the lifetime and destruction is handled by the catalog.
  /// </summary>
  /// <remarks>
  /// All TCustomBusiness descendants need to have a logger injected upon
  /// construction.
  /// </remarks>
  /// <seealso cref="TBusinessObjectCatalog" />
  TCustomBusiness = class( TInterfacedPersistent )
  strict private
    fLog: ILog;
  protected
    procedure CheckAssigned( const AInterface: IInterface; const ANameOfInterface: string );
    procedure EnterMethod( const AProcName: string );
    procedure LeaveMethod( const AProcName: string );
    procedure VerifyConstructorParameters; dynamic;
    { Properties }
    property Log: ILog read fLog;
  public
    { Initialization }
    constructor Create( ALog: ILog ); reintroduce;
    procedure BeforeDestruction; override;
  end;

  /// <summary>
  /// This a reference counted version of TCustomBusiness.  It allows a descending class
  /// to check that it doesn't introduce visible members, which is an antipottern for reference
  /// counted objects in Delphi.
  /// </summary>
  /// <remarks>
  /// All TCustomBusinessReferenceCounted descendants need to have a logger injected upon
  /// construction.
  /// </remarks>
  /// <seealso cref="TCustomBusiness" />
  TCustomBusinessReferenceCounted = class( TInterfacedObject )
  strict private
    fLog: ILog;
  protected
    function MethodAllowed( const AMethodName: string ): boolean;
    procedure CheckAssigned( const AInterface: IInterface; const ANameOfInterface: string );
    procedure EnterMethod( const AProcName: string );
    procedure LeaveMethod( const AProcName: string );
    procedure VerifyConstructorParameters; dynamic;
    procedure VerifyVisibility( const ATypeInfo: Pointer );
    { Properties }
    property Log: ILog read fLog;
  public
    { Initialization }
    constructor Create( ALog: ILog ); reintroduce;
    procedure BeforeDestruction; override;
  end;

  /// <summary>
  /// This a component that has a log, and that gets automatically named (which means a single one should
  /// have the same owner.
  /// counted objects in Delphi.
  /// </summary>
  /// <remarks>
  /// All TCustomBusinessComponent descendants need to have a logger injected upon
  /// construction.
  /// </remarks>
  /// <seealso cref="TCustomBusiness" />
  TCustomBusinessComponent = class( TComponent )
  strict private
    fLog: ILog;
  protected
    procedure EnterMethod( const AProcName: string );
    procedure LeaveMethod( const AProcName: string );
    { Properties }
    property Log: ILog read fLog;
  public
    constructor Create( AOwner: TComponent; ALog: ILog ); reintroduce;
    procedure BeforeDestruction; override;
  end;

implementation

uses
  System.SysUtils, System.Rtti, System.TypInfo;

resourcestring
  LOG_DESTRUCTION = '%s.BeforeDestruction(): Called.';

var
  methodsFromBaseClass: TStringList; // A list of methods that already exists in TInterfacedObject, almost all from TObject.
  rttiContext: TRttiContext;

{$REGION 'TCustomBusiness'}

constructor TCustomBusiness.Create( ALog: ILog );
begin
  inherited Create;
  fLog := ALog;
end;

procedure TCustomBusiness.BeforeDestruction;
begin
  Log.Event( LOG_DESTRUCTION, [ClassName] );
  inherited;
end;

procedure TCustomBusiness.EnterMethod( const AProcName: string );
begin
  fLog.EnterMethod( Self, AProcName );
end;

procedure TCustomBusiness.LeaveMethod( const AProcName: string );
begin
  fLog.LeaveMethod( Self, AProcName );
end;

procedure TCustomBusiness.VerifyConstructorParameters;
begin
  CheckAssigned( fLog, 'Log' );
end;

procedure TCustomBusiness.CheckAssigned( const AInterface: IInterface; const ANameOfInterface: string );
const
  PROC_NAME = 'CheckAssigned';
  ERR_MSG   = LOG_STUB_STRING + 'The interface is not assigned.';
var
  errorMessage: string;
begin
  if not Assigned( AInterface ) then
  begin
    errorMessage := Format( ERR_MSG, [ClassName, PROC_NAME, ANameOfInterface] );
    if Assigned( fLog ) then
      fLog.SilentError( errorMessage )
    else if Assigned( GlobalLog ) then
      GlobalLog.SilentError( errorMessage );
    raise EArgumentNilException.Create( errorMessage );
  end;
end;

{$ENDREGION}
{$REGION 'TCustomBusinessReferenceCounted'}

constructor TCustomBusinessReferenceCounted.Create( ALog: ILog );
begin
  inherited Create;
  fLog := ALog;
end;

procedure TCustomBusinessReferenceCounted.BeforeDestruction;
begin
  Log.Event( LOG_DESTRUCTION + ' RefCount = %d.', [ClassName, Self.RefCount] );
  inherited;
end;

procedure TCustomBusinessReferenceCounted.EnterMethod( const AProcName: string );
begin
  fLog.EnterMethod( Self, AProcName );
end;

procedure TCustomBusinessReferenceCounted.LeaveMethod( const AProcName: string );
begin
  fLog.LeaveMethod( Self, AProcName );
end;

function TCustomBusinessReferenceCounted.MethodAllowed( const AMethodName: string ): boolean;
var
  foundAt: Integer;
begin
  Result := methodsFromBaseClass.Find( AMethodName, foundAt );
end;

procedure TCustomBusinessReferenceCounted.VerifyConstructorParameters;
begin
  CheckAssigned( fLog, 'Log' );
end;

procedure TCustomBusinessReferenceCounted.VerifyVisibility( const ATypeInfo: Pointer );
var
  baseTypeInfo: Pointer;
  ctx: TRttiContext;
  ourType: TRttiType;
  baseType: TRttiType;
  ourProperty: TRttiProperty;
  ourMethod: TRttiMethod;
begin
  { Get basic info }
  baseTypeInfo := TypeInfo( TCustomBusinessReferenceCounted );
  baseType := ctx.GetType( baseTypeInfo );
  { Get info for this type }
  ourType := ctx.GetType( ATypeInfo );
  { Check that now new methods are added }
  for ourMethod in ourType.GetMethods do
  begin
    if ( baseType.MethodAddress( ourMethod.Name ) = nil ) and ( not MethodAllowed( ourMethod.Name ) ) then
    begin
      if ourMethod.Visibility > mvProtected then
        Log.Event( '%s.%s: Visibility is too high for this method.', [ClassName, ourMethod.Name], ltError )
      else
        Log.SilentSuccess( '%s.%s: Visibility OK for this method.', [ClassName, ourMethod.Name] );
    end
    else
      Log.Event( '%s.%s: Method found in base class or in list of allowed methods.', [ClassName, ourMethod.Name], ltDebug );
  end;

  for ourProperty in ourType.GetProperties do
  begin
    if baseType.GetProperty( ourProperty.Name ) = nil then
    begin
      if ourProperty.Visibility > mvProtected then
        Log.Event( '%s.%s: Visibility too high', [ClassName, ourProperty.Name], ltError )
      else
        Log.SilentSuccess( '%s.%s: Visibility OK for this property.', [ClassName, ourProperty.Name] );
    end
    else
      Log.Event( '%s.%s: Property found in base class or in list of allowed properties.', [ClassName, ourProperty.Name], ltDebug );
  end;
end;

procedure TCustomBusinessReferenceCounted.CheckAssigned( const AInterface: IInterface; const ANameOfInterface: string );
const
  PROC_NAME = 'CheckAssigned';
  ERR_MSG   = LOG_STUB_STRING + 'The interface is not assigned.';
var
  errorMessage: string;
begin
  if not Assigned( AInterface ) then
  begin
    errorMessage := Format( ERR_MSG, [ClassName, PROC_NAME, ANameOfInterface] );
    if Assigned( fLog ) then
      fLog.SilentError( errorMessage )
    else if Assigned( GlobalLog ) then
      GlobalLog.SilentError( errorMessage );
    raise EArgumentNilException.Create( errorMessage );
  end;
end;

{$ENDREGION}
{$REGION 'TCustomBusinessReferenceCounted'}

constructor TCustomBusinessComponent.Create( AOwner: TComponent; ALog: ILog );
begin
  inherited Create( AOwner );
  fLog := ALog;
  name := ClassName.Substring( 1 );
end;

procedure TCustomBusinessComponent.EnterMethod( const AProcName: string );
begin
  fLog.EnterMethod( Self, AProcName );
end;

procedure TCustomBusinessComponent.LeaveMethod( const AProcName: string );
begin
  fLog.LeaveMethod( Self, AProcName );
end;

procedure TCustomBusinessComponent.BeforeDestruction;
begin
  fLog.Event( LOG_DESTRUCTION, [ClassName] );
  inherited;
end;

{$ENDREGION}

initialization

methodsFromBaseClass := TStringList.Create( TDuplicates.dupError, true, false );
methodsFromBaseClass.CommaText :=
{ } 'AfterConstruction,BeforeDestruction,ClassInfo,ClassName,ClassNameIs,ClassParent,ClassType,CleanupInstance,Create,DefaultHandler,Destroy,' +
{ } 'Dispatch,DisposeOf,Equals,FieldAddress,Free,FreeInstance,GetHashCode,GetInterface,GetInterfaceEntry,GetInterfaceTable,InheritsFrom,InitInstance,InstanceSize,' +
{ } 'MethodAddress,MethodName,NewInstance,QualifiedClassName,SafeCallException,ToString,UnitName,UnitScope';

rttiContext := TRttiContext.Create;

finalization

rttiContext.Free;
methodsFromBaseClass.Free;

end.
