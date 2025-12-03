unit CRF.Person.StudyCase.AccessControl;

interface

uses
  CRF.Person.StudyCase.Interfaces,
  CRF.Person.MoveInterface,
  CRF.User.Interfaces,
  {General}
  Emetra.Classes.Business,
  Emetra.Logging.Interfaces,
  Emetra.Database.Interfaces,
  {Standard}
  System.Classes;

type
  TStudyCaseAccessControl = class( TBusiness, IStudyCaseAccessControl )
  strict private
    fSQL: ISQL;
    fActiveCase: IActiveStudyCase;
    fActiveUser: ICRFActiveUser;
    fIgnoreRelation: boolean;
    fIgnoreLocation: boolean;
    fIgnoreBlocking: boolean;
    fStudyCaseTransfer: ICRFStudyCaseTransfer;
  private
    { Property accessors }
    function Get_IgnoreLocation: boolean;
    function Get_IgnoreRelation: boolean;
    function Get_IgnoreBlocking: boolean;
    procedure Set_IgnoreLocation( const AValue: boolean );
    procedure Set_IgnoreRelation( const AValue: boolean );
    procedure Set_IgnoreBlocking( const AValue: boolean );
    { Access checks }
    function CheckCanAccessCase: boolean;
    function CheckSameLocation: boolean;
    function CheckActiveRelation: boolean;
  public
    { Initialization }
    constructor Create( const AMyUser: ICRFActiveUser; const ASQL: ISQL; const ALog: ILog ); reintroduce;
    { IStudyCaseAccessControl }
    /// <summary>
    /// Determine whether user (FActiveUser) have access to patient (FActiveCase)
    /// </summary>
    /// <returns>
    /// True if user is allowed to access case.
    /// </returns>
    function TryGetAccess( const AStudyCase: IActiveStudyCase ): boolean;
    { Properties }
    property IgnoreLocation: boolean read Get_IgnoreLocation write Set_IgnoreLocation;
    property IgnoreRelation: boolean read Get_IgnoreRelation write Set_IgnoreRelation;
    property IgnoreBlocking: boolean read Get_IgnoreBlocking write Set_IgnoreBlocking;
    property StudyCaseTransfer: ICRFStudyCaseTransfer read fStudyCaseTransfer write fStudyCaseTransfer;
  end;

implementation

uses
  System.SysUtils, CRF.SQL;

resourcestring
  { Move to current location }

  ASK_TRANSFER_PATIENT =
  { } '{{%s}} er tilknyttet %s.\n' +
  { } 'Vil du overflytte %s til %s?';

  ASK_CHANGE_WORKSITE =
  { } 'Vil du i stedet bytte eget arbeidssted til %s?';

  LOG_MSG_ACCESS_BLOCKED =
  { } 'Du har ikke tilgang til denne journalen.';

  MSG_RELOCATE_TO_CENTER =
  { } '{{%s}} er ikke tilknyttet noen lokalisasjon.\n' +
  { } 'Du må velge en gruppe på ditt arbeidssted.\n\n' +
  { } 'Vil du hente %s inn til ditt arbeidssted?';

  WARN_WRONG_CENTER =
  { } 'Du er ikke på samme sted som {{%s}}:\n' +
  { } 'Personen er tilknyttet %s.\n' +
  { } 'Personen må eventuelt utskrives/avsluttes derfra.';

  { Select relation }

  WARN_NO_RELATION =
  { } 'Du har ikke en aktiv relasjon til {{%s}}.\n' +
  { } 'Du må definere din profesjonelle relasjon til\n' +
  { } 'pasienten for å kunne åpne denne journalen.\n\n' +
  { } 'Vil du definere en profesjonell relasjon nå?';
  MSG_RELOCATE_TO_GROUP =
  { } '{{%s}} er ikke tilknyttet noen lokalisasjon.\n' +
  { } 'Vil du hente %s inn til din gruppe (%s)?';

constructor TStudyCaseAccessControl.Create( const AMyUser: ICRFActiveUser; const ASQL: ISQL; const ALog: ILog );
begin
  inherited Create( ALog );
  fSQL := ASQL;
  fActiveUser := AMyUser;
end;

{$REGION 'Property accessors'}

function TStudyCaseAccessControl.Get_IgnoreLocation: boolean;
begin
  Result := fIgnoreLocation;
end;

function TStudyCaseAccessControl.Get_IgnoreRelation: boolean;
begin
  Result := fIgnoreRelation;
end;

function TStudyCaseAccessControl.Get_IgnoreBlocking: boolean;
begin
  Result := fIgnoreBlocking;
end;

procedure TStudyCaseAccessControl.Set_IgnoreLocation( const AValue: boolean );
begin
  fIgnoreLocation := AValue;
end;

procedure TStudyCaseAccessControl.Set_IgnoreRelation( const AValue: boolean );
begin
  fIgnoreRelation := AValue;
end;

procedure TStudyCaseAccessControl.Set_IgnoreBlocking( const AValue: boolean );
begin
  fIgnoreBlocking := AValue;
end;

{$ENDREGION}

function TStudyCaseAccessControl.TryGetAccess( const AStudyCase: IActiveStudyCase ): boolean;
begin
  fActiveCase := AStudyCase;
  Result := CheckCanAccessCase and CheckSameLocation and CheckActiveRelation;
  if not Result then
    fActiveCase.Select( 0 );
end;

function TStudyCaseAccessControl.CheckCanAccessCase: boolean;
const
  PROC_NAME      = '%s.CheckCanAccessCase: ';
  LOG_FUNC_ERROR = PROC_NAME + ' PersonId=%d, Exception=%s';
begin
  if fIgnoreBlocking then
    Result := true
  else
    try
      with fSQL.FastQuery( QRY_USER_HAS_CASE_ACCESS, [fActiveUser.UserId, fActiveCase.PersonId] ) do
        try
          Result := Fields[0].AsBoolean;
        finally
          Close;
        end;
      { Message dialog shown to user. }
      if not Result then
        Log.Event( LOG_MSG_ACCESS_BLOCKED, ltWarning );
    except
      on E: Exception do
      begin
        { SQL query fail e.g. stored procedure
          doesn't exist etc. }
        Result := true;
        Log.SilentError( LOG_FUNC_ERROR, [ClassName, fActiveCase.PersonId, E.ClassName] );
      end;
    end;
end;

function TStudyCaseAccessControl.CheckSameLocation: boolean;
const
  PROC_NAME = '%s.CheckSameLocation: ';
  LOG_GROUP = PROC_NAME + 'CenterId=%d, GroupId=%d, GroupName=%s, Result=%s';
begin
  if fIgnoreLocation then
    Result := true
  else
  begin
    if not fActiveCase.ValidGroup then
    with fActiveCase do
    begin
      { Allow patient to be connected to a group at user's location }
      if ( fActiveUser.GroupId > 0 ) and ( fActiveUser.ShowMyGroup )
      { } and Log.LogYesNo( Format( MSG_RELOCATE_TO_GROUP, [FirstName, PronounObjective, fActiveUser.GroupName] ) ) then
        GroupId := fActiveUser.GroupId
      else if Log.LogYesNo( Format( MSG_RELOCATE_TO_CENTER, [FirstName, PronounObjective] ) ) then
        UpdateGroup;
    end
    else if ( fActiveCase.CenterId <> fActiveUser.CenterId ) then
      with fActiveCase do
      begin
        { Move if possible }
        if not Assigned( fStudyCaseTransfer ) then
          Log.Event( Format( WARN_WRONG_CENTER, [FirstName, CenterName] ), ltWarning )
        else if Log.LogYesNo( Format( ASK_TRANSFER_PATIENT, [FirstName, CenterName, PronounObjective, fActiveUser.CenterName] ), ltMessage ) then
          fStudyCaseTransfer.TryTransfer( fActiveCase, fActiveUser )
        else if Log.LogYesNo( Format( ASK_CHANGE_WORKSITE, [CenterName] ), ltMessage ) then
          fActiveUser.SetCenter( CenterId )
      end;
    Result := ( fActiveCase.CenterId = fActiveUser.CenterId );
  end;
  Log.Event( LOG_GROUP, [ClassName, fActiveCase.CenterId, fActiveCase.GroupId, fActiveCase.GroupName, BoolToStr( Result, true )] );
end;

function TStudyCaseAccessControl.CheckActiveRelation: boolean;
const
  LOG_RESULT = '%s.CheckActiveRelation() = %s ';
begin
  if fIgnoreRelation then
    Result := true
  else if fActiveCase.ValidRelation then
    Result := true
  else
  begin
    if ( fActiveUser.RelationCount = 1 ) or Log.LogYesNo( Format( WARN_NO_RELATION, [fActiveCase.FullName] ), ltWarning ) then
      fActiveCase.UpdateRelation;
    Result := fActiveCase.ValidRelation;
  end;
  Log.Event( LOG_RESULT, [ClassName, BoolToStr( Result, true )] );
end;

end.
