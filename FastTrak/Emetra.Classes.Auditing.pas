unit Emetra.Classes.Auditing;

interface

uses
  Classes, Contnrs,
  Generics.Collections;

type
  TClassCounter = class( TObject )
  private
    FIgnoreClass: TStringList;
    FClassCatalog: TDictionary<string, integer>;
    FLogEverything: boolean;
    function GetClassInstanceCount( AClassName: string ): integer;
  public
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    procedure AddInstance( const AFullyQualifiedClassName: string );
    procedure RemoveInstance( const AFullyQualifiedClassName: string );
    procedure DoNotLog( const AFullyQualifiedClassName: string );
    function IgnoredClass( const AFullyQualifiedClassName: string ): boolean;
    property Count[AName: string]: integer read GetClassInstanceCount; default;
  end;

var
  GlobalClassCounter: TClassCounter;

procedure SafeFree( AObject: TObject );
function SafeToFree( AObject: TObject ): boolean;

implementation

uses
{$IFNDEF FMX}
{$IFNDEF Console}
  Vcl.Forms,
{$ENDIF}
{$ENDIF}
  Emetra.Logging.Interfaces,
  System.SysUtils;

const
  { SafeFree messages }
  INFO_FREE_SAFE             = 'SafeFree: Nobody owns "%s" of class %s, should be safe to free.';
  ERR_FREE_REFERENCE_COUNTED = 'SafeFree: The class %s is reference counted and should not be freed.  Set interface to nil instead.';
  EXC_FREE_FAILED            = 'SafeFree: Class %s failed with error "%s".';
  EXC_LOG_UNASSIGNED         = 'SafeFree: The global log variable must be set to use this method.';
  WARN_FREE_UNSAFE           = 'SafeFree: Somebody else (%s) owns %s of class %s, free is unsafe. Not freed, young Zaphod plays it safe.';

const
  { Class counter messages }
  LOG_SUCCESS_EMPTY_CATALOG = '%s.%s: All instances destroyed successfully.';
  WARN_SOME_CLASSES_REMAIN  = '%s.%s: Instances of %d classes remain!';
  LOG_REMOVE_INSTANCE       = '%s.RemoveInstance: %s ( n = %d )';
  LOG_ADD_INSTANCE          = '%s.AddInstance: %s Create( n = %d )';

function SafeToFree( AObject: TObject ): boolean;
begin
  Result := Assigned( AObject );
  if Result and AObject.InheritsFrom( TComponent ) then
    Result := TComponent( AObject ).Owner = nil
end;

procedure SafeFree( AObject: TObject );
var
  className: string;
begin
  Assert( Assigned( GlobalLog ), EXC_LOG_UNASSIGNED );
  className := 'Unknown';
  if Assigned( AObject ) then
    try
      className := AObject.className;
{$IFDEF Debug}
      if not GlobalClassCounter.IgnoredClass( AObject.QualifiedClassName ) then
        GlobalLog.Event( 'SafeFree( %s )', [AObject.QualifiedClassName] );
{$ENDIF}
      if AObject.InheritsFrom( TInterfacedObject ) then
        GlobalLog.SilentError( ERR_FREE_REFERENCE_COUNTED, [AObject.className] )
      else if AObject.InheritsFrom( TComponent ) then
        with TComponent( AObject ) do
        begin
          if Assigned( Owner ) then
          begin
            GlobalLog.SilentWarning( WARN_FREE_UNSAFE, [Owner.className, name, className] );
            exit;
          end;
          GlobalLog.Event( INFO_FREE_SAFE, [name, className] )
        end;
      FreeAndNil( AObject );
    except
      on E: Exception do
        GlobalLog.SilentError( EXC_FREE_FAILED, [className, E.Message] );
    end;
end;

{$REGION 'TClassCounter'}

procedure TClassCounter.AfterConstruction;
begin
  inherited;
  FIgnoreClass := TStringList.Create;
  FIgnoreClass.Sorted := true;
  FIgnoreClass.Duplicates := dupIgnore;
  FClassCatalog := TDictionary<string, integer>.Create;
end;

procedure TClassCounter.BeforeDestruction;
var
  instances: integer;
  iWarnings: integer;
  checkClass: string;
begin
  if Assigned( GlobalLog ) then
  begin
    GlobalLog.EnterMethod( Self, PROC_BEFORE_DESTRUCTION );
    try
      iWarnings := 0;
      for checkClass in FClassCatalog.Keys do
        if FClassCatalog.TryGetValue( checkClass, instances ) and ( instances <> 0 ) then
        begin
          GlobalLog.SilentWarning( '%s: n=%d', [checkClass, instances] );
          inc( iWarnings );
        end;
      if iWarnings = 0 then
        GlobalLog.SilentSuccess( LOG_SUCCESS_EMPTY_CATALOG, [className, PROC_BEFORE_DESTRUCTION] )
      else
        GlobalLog.SilentError( WARN_SOME_CLASSES_REMAIN, [className, PROC_BEFORE_DESTRUCTION, iWarnings] )
    finally
      GlobalLog.LeaveMethod( Self, PROC_BEFORE_DESTRUCTION );
    end;
  end;
  FreeAndNil( FClassCatalog );
  FreeAndNil( FIgnoreClass );
  inherited;
end;

procedure TClassCounter.AddInstance( const AFullyQualifiedClassName: string );
var
  foundAt: integer;
  instances: integer;
begin
  instances := GetClassInstanceCount( AFullyQualifiedClassName ) + 1;
  FClassCatalog.AddOrSetValue( AFullyQualifiedClassName, instances );
  if not FLogEverything then
    exit;
  if not FIgnoreClass.Find( AFullyQualifiedClassName, foundAt ) and Assigned( GlobalLog ) then
    GlobalLog.Event( LOG_ADD_INSTANCE, [className, AFullyQualifiedClassName, instances] );
end;

procedure TClassCounter.DoNotLog( const AFullyQualifiedClassName: string );
begin
  FIgnoreClass.Add( AFullyQualifiedClassName )
end;

function TClassCounter.GetClassInstanceCount( AClassName: string ): integer;
begin
  if not FClassCatalog.TryGetValue( AClassName, Result ) then
    Result := 0;
end;

function TClassCounter.IgnoredClass( const AFullyQualifiedClassName: string ): boolean;
begin
  Result := FIgnoreClass.IndexOf( AFullyQualifiedClassName ) <> -1;
end;

procedure TClassCounter.RemoveInstance( const AFullyQualifiedClassName: string );
var
  foundAt: integer;
  instances: integer;
begin
  instances := GetClassInstanceCount( AFullyQualifiedClassName ) - 1;
  FClassCatalog.AddOrSetValue( AFullyQualifiedClassName, instances );
  if not FLogEverything then
    exit;
  if not FIgnoreClass.Find( AFullyQualifiedClassName, foundAt ) and Assigned( GlobalLog ) then
    GlobalLog.Event( LOG_REMOVE_INSTANCE, [className, AFullyQualifiedClassName, instances] );
end;

{$ENDREGION}

initialization

GlobalClassCounter := TClassCounter.Create;
GlobalClassCounter.DoNotLog( 'CRF.Input.Item.TCRFItem' );
GlobalClassCounter.DoNotLog( 'CRF.Input.Form.TCRFForm' );
GlobalClassCounter.DoNotLog( 'CRF.Input.Lists.TCRFItemList' );
GlobalClassCounter.DoNotLog( 'CRF.Input.Lists.TCRFPageList' );
GlobalClassCounter.DoNotLog( 'CRF.Meta.FormAction.TCRFAction' );
GlobalClassCounter.DoNotLog( 'CRF.Meta.FormAction.TCRFActionList' );
GlobalClassCounter.DoNotLog( 'CRF.Meta.Form.TMetaForm' );
GlobalClassCounter.DoNotLog( 'CRF.Meta.Page.TCRFMetaPage' );
GlobalClassCounter.DoNotLog( 'CRF.ClinForm.TClinForm' );
GlobalClassCounter.DoNotLog( 'EPR.CDSS.Alert.TAlert' );
GlobalClassCounter.DoNotLog( 'EPR.Drug.Treatment.TDrugTreatment' );
// Problems on problem list
GlobalClassCounter.DoNotLog( 'EPR.Problem.ClinProblem.TClinProblem' );
GlobalClassCounter.DoNotLog( 'EPR.Problem.MetaProblemType.TMetaProblemType' );
GlobalClassCounter.DoNotLog( 'EPR.Problem.MetaProblem.TMetaProblem' );
GlobalClassCounter.DoNotLog( 'EPR.Problem.ProblemFinder.TProblemGroup' );
GlobalClassCounter.DoNotLog( 'EPR.Problem.ProblemFinder.TFrequentProblem' );
GlobalClassCounter.DoNotLog( 'WinMed.Reader.Problem.TWinMedProblem' );
// Datapoints
GlobalClassCounter.DoNotLog( 'CRF.ClinData.DataRow.TCRFStandardRow' );
// People
GlobalClassCounter.DoNotLog( 'CRF.Person.StudyCase.TStudyCase' );
GlobalClassCounter.DoNotLog( 'Emetra.Person.TPerson' );
// VMR Entries
GlobalClassCounter.DoNotLog( 'VMR.Entries.LabData.TLabEntry' );
GlobalClassCounter.DoNotLog( 'VMR.Entries.Text.TClinFormEntry' );
GlobalClassCounter.DoNotLog( 'VMR.Entries.Base.TTextEntry' );
// Other objects
GlobalClassCounter.DoNotLog( 'Kith.Inbox.Message.TInboxMessage' );
GlobalClassCounter.DoNotLog( 'Emetra.Database.TimeLogger.TTimeData' );
GlobalClassCounter.DoNotLog( 'Emetra.Database.User.TDatabaseUser' );
GlobalClassCounter.DoNotLog( 'Emetra.Classes.Subject.TContainedObservable' );
GlobalClassCounter.DoNotLog( 'System.Contnrs.TObjectList' );

finalization

{$IFNDEF FMX}
{$IFNDEF Console}
if Assigned( GlobalLog ) then
  GlobalLog.EnterMethod( Application, 'Finalizing Emetra.Classes.Auditing' );
{$ENDIF}
{$ENDIF}
GlobalClassCounter.Free;
{$IFNDEF FMX}
{$IFNDEF Console}
if Assigned( GlobalLog ) then
  GlobalLog.LeaveMethod( Application, 'Finalized Emetra.Classes.Auditing' );
{$ENDIF}
{$ENDIF}

end.
