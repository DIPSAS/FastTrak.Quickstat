unit Emetra.Database.ParameterDictionary;

interface

uses
  {General classes}
  Emetra.Classes.Transient,
  {General interfaces}
  Emetra.Database.ParameterDictionary.Interfaces,
  Emetra.Dictionary.Interfaces,
  Emetra.Logging.Interfaces,
  {Standard}
  Data.Db;

type
  /// <summary>
  ///   The parameter dictionary allows a database query to query the
  ///   application for the necessary parameters.
  /// </summary>
  /// <remarks>
  ///   Periods come with dates in pairs, so these are handled via a special
  ///   dictionary. The period is usually context-dependent, to it makes sense
  ///   to use a custom dictionary that allows saving and retrieving last
  ///   period based on the context.
  /// </remarks>
  /// <example>
  ///   A query may need to know the context (e.g. StudyId) in which it needs
  ///   to run. Using a name like :StudyId for the parameter allows the query
  ///   to look for this value in a general IVariantDictionary to find its
  ///   current value.
  /// </example>
  TParameterDictionary = class( TTransient, IParameterDictionary )
  strict private
    fVariantDictionary: IVariantDictionary;
    fPeriodDictionary: IPeriodDictionary;
  private
    { Other methods }
    function TryApplyParameters( const AQuery: string; AParams: TParams ): boolean;
  public
    constructor Create( APeriodDictionary: IPeriodDictionary; AVariantDictionary: IVariantDictionary; ALog: ILog ); reintroduce; overload;
    constructor Create( AVariantDictionary: IVariantDictionary; ALog: ILog ); overload;
  end;

implementation

uses
  Spring,
  System.Classes,
  System.SysUtils,
  System.Variants,
  System.RegularExpressions;

resourcestring
  rsSelectPeriod = 'Denne spørringen krever at du angir et tidsintervall.';

const
  { Silent log messages }
  LOG_USER_CANCELLED       = '%s.%s: The user cancelled the request for time period specification, aborting.';
  LOG_UNRESOLVED_PARAMETER = '%s.%s: Unknown parameter name "%s" found at position %d.';
  LOG_PARAMETER_SET        = '%s.%s: Parameter "%s" set to value "%s"';

  { Period parameters, names by convention }
  PRM_START_DATE = 'StartDate';
  PRM_STOP_DATE  = 'StopDate';

constructor TParameterDictionary.Create( AVariantDictionary: IVariantDictionary; ALog: ILog );
begin
  inherited Create( ALog );
  fVariantDictionary := AVariantDictionary;
end;

constructor TParameterDictionary.Create( APeriodDictionary: IPeriodDictionary; AVariantDictionary: IVariantDictionary; ALog: ILog );
begin
  inherited Create( ALog );
  fPeriodDictionary := APeriodDictionary;
  fVariantDictionary := AVariantDictionary;
end;

function TParameterDictionary.TryApplyParameters( const AQuery: string; AParams: TParams ): boolean;
const
  PROC_NAME = 'TryApplyParameters';
var
  startDate, stopDate: TDateTime;
  prmIndex: integer;
  prm: TParam;
  prmStartDate: TParam;
  prmStopDate: TParam;
  prmValue: variant;
begin
  Result := true;
  Guard.CheckNotNull( fVariantDictionary, 'VarSupplier' );
  Guard.CheckNotNull( AParams, 'Params' );
  { Use built-in parsing capability of TParams to create parameter list }
  AParams.ParseSQL( AQuery, true );
  { Get start and stop date from the period dictionary  if these parameters appear in query }
  prmStartDate := AParams.FindParam( PRM_START_DATE );
  prmStopDate := AParams.FindParam( PRM_STOP_DATE );
  if Assigned( prmStartDate ) and Assigned( prmStopDate ) then
  begin
    Guard.CheckNotNull( fPeriodDictionary, 'PeriodDictionary' );
    if fPeriodDictionary.TryGetPeriod( AQuery, rsSelectPeriod, startDate, stopDate ) then
    begin
      prmStartDate.Value := startDate;
      prmStopDate.Value := stopDate;
    end
    else
    begin
      Log.SilentWarning( LOG_USER_CANCELLED, [ClassName, PROC_NAME] );
      { This means that the user dialog was canceled or the period was invalid }
      Result := false;
      exit;
    end;
  end;
  { Set the rest of the parameters via the fVariantDictionary interface }
  prmIndex := 0;
  while prmIndex < AParams.Count do
  begin
    prm := AParams[prmIndex];
    if prm.IsNull then
    begin
      if fVariantDictionary.TryGetValue( prm.Name, prmValue ) then
        prm.Value := prmValue
      else
      begin
        Log.SilentError( LOG_UNRESOLVED_PARAMETER, [ClassName, PROC_NAME, prm.Name, prmIndex] );
        Result := false;
        break;
      end;
    end;
    Log.Event( LOG_PARAMETER_SET, [ClassName, PROC_NAME, prm.Name, VarToStr( prm.Value )] );
    inc( prmIndex );
  end;
end;

end.
