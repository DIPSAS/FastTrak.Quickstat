unit CRF.Context.ActiveCase;

interface

uses
  CRF.CommandNames,
  CRF.Person.StudyCase,
  CRF.Person.StudyCase.Interfaces,
  CRF.Person.MoveInterface,
  CRF.User.Interfaces,
  {Medical}
  VMR.Container,
  VMR.Patient.Interfaces,
  {General}
  Emetra.Command.Interfaces,
  Emetra.ObjectContainer.Interfaces,
  Emetra.Person.Interfaces,
  {Standard}
  Vcl.Clipbrd,
  Data.Db,
  System.Classes, System.SysConst, System.Contnrs, System.Types, System.SysUtils;

type
  TActiveCase = class( TStudyCase, ICRFStudyCase, IActiveStudyCase, IPersonId, IObjectContainer, IPatient, IPersonVisualId, ICommandReceiver, ISelfRegisterCommandReceiver )
  strict private
    fObservers: TObjectList;
    fEditUpdateLevel: integer;
    fVmrContainer: TVMR;
    fActiveUser: ICRFActiveUser;
    fAccessControl: IStudyCaseAccessControl;
    fJournalansvarlig: integer;
    fJournalansvarligNavn: string;
    fRevertToActiveOnSelect: boolean;
  private
    { Property accessors }
    function Get_IgnoreRelation: boolean;
    function Get_IgnoreLocation: boolean;
    function Get_Journalansvarlig: integer;
    function Get_JournalansvarligNavn: string;
    procedure Set_IgnoreRelation( const AIgnoreRelation: boolean );
    procedure Set_IgnoreLocation( const AIgnoreLocation: boolean );
    { Other members }
    function TryGetObject( const AName: string; out AObject: TObject ): boolean;
    procedure RegisterCommands( const AMediator: ICommandMediator );
    procedure GetObjectNames( ANames: TStrings );
    procedure Touch;
    procedure SetRelation( const ARelationId: integer );
    procedure NotifyBeforeClose;
    procedure NotifyAfterSelect;
    procedure UpdateRelation;
    procedure UpdateGroup;
    procedure ClearIdentityAndData;
  protected
    function ExecuteCmd( const ACommand: ICommand ): boolean;
    function SelectRelation: integer; {
      Brings up a dialog box with valid patient relations, returns -1 for no selection }
  public
    { Initialization }
    constructor Create( AControlAccess: IStudyCaseAccessControl; AMyUser: ICRFActiveUser ); reintroduce;
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { Other members }
    procedure BeginEdit;
    procedure EndEdit;
    procedure Clear; override;
    procedure AssumeJournalansvar; { Journalansvarlig is a legally defined role }
    procedure Load( ADataset: TDataset ); override;
    procedure AddActiveCaseObserver( AObject: TObject );
    procedure RemoveActiveCaseObserver( AObject: TObject );
    procedure RemoveAllActiveCaseObservers;
    procedure PrepareToDestroy;
    function Select( const APatId: integer ): boolean;
    { Event hooks before and after person changes }
  public
    property RevertToActiveOnSelect: boolean read fRevertToActiveOnSelect write fRevertToActiveOnSelect;
    property VMR: TVMR read fVmrContainer;
    property IgnoreRelation: boolean read Get_IgnoreRelation write Set_IgnoreRelation;
    property IgnoreLocation: boolean read Get_IgnoreLocation write Set_IgnoreLocation;
    property Journalansvarlig: integer read Get_Journalansvarlig;
    property JournalansvarligNavn: string read Get_JournalansvarligNavn;
  published
    procedure UpdateStatus( const AStatusId: integer );
  end;

implementation

uses
  CRF.SQL,
  CRF.SQL.Fields,
  {General}
  Emetra.Logging.Interfaces,
  Emetra.Interfaces.Observer,
  Emetra.Classes.Subject;

resourcestring

  EXC_INVALID_PERSON = 'Personen %d har ikke gyldige data i databasen.';

  HDR_ROLES = 'Profesjonell relasjon';

  TXT_SELECT_RELATION = 'Velg en av relasjonene fra listen nedenfor.';

  MSG_NO_RELATIONS_AVAIL =
  { } 'Yrket ditt har ingen definerte roller som gir journaltilgang.\n' +
  { } 'Kontakt brukerstøtte hvis du mener at dette skal endres.';

  WARN_JOURNALANSVAR_FAILED =
  { } 'Du kan ikke bli journalansvarlig for denne personen.\n' +
  { } 'Feilmelding: %s';

  TXT_MISSING = '(ikke registrert)';

  ASK_REVERT_TO_ACTIVE =
  { } 'Denne personens status er satt til "%s".\n' +
  { } 'Vil du sette status for {{%s}} tilbake til "Aktiv"?';

var
  InstanceCount: integer = 0;

  { TActiveCase }

{$REGION 'Initialization'}

constructor TActiveCase.Create( AControlAccess: IStudyCaseAccessControl; AMyUser: ICRFActiveUser );
begin
  inherited Create( AMyUser.StudyContext, AMyUser.SQL, AMyUser.Log );
  fAccessControl := AControlAccess;
  fActiveUser := AMyUser;
end;

procedure TActiveCase.AfterConstruction;
begin
  inherited;
  fVmrContainer := TVMR.Create( Log );
  fObservers := TObjectList.Create( false );
  inc( InstanceCount );
end;

procedure TActiveCase.PrepareToDestroy;
const
  PROC_NAME = 'PrepareToDestroy';
begin
  Log.EnterMethod( Self, PROC_NAME );
  try
    RemoveAllActiveCaseObservers;
    fAccessControl := nil;
    fActiveUser := nil;
  finally
    Log.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TActiveCase.BeforeDestruction;
begin
  RemoveAllActiveCaseObservers;
  FreeAndNil( fObservers );
  FreeAndNil( fVmrContainer );
  inherited;
end;

{$ENDREGION}
{$REGION 'IObjectTree'}

function TActiveCase.TryGetObject( const AName: string; out AObject: TObject ): boolean;
var
  thisPersonId: integer;
begin
  if TryStrToInt( AName, thisPersonId ) then
  begin
    Select( thisPersonId );
    AObject := Self;
  end
  else if SameText( AName, 'VMR' ) then
    AObject := fVmrContainer
  else if SameText( AName, 'Person' ) then
    AObject := fPerson
  else
    AObject := nil;
  Result := Assigned( AObject );
end;

procedure TActiveCase.GetObjectNames( ANames: TStrings );
begin
  ANames.Add( 'Person' );
  ANames.Add( 'VMR' );
end;

procedure TActiveCase.EndEdit;
const
  PROC_NAME = 'EndEdit';
var
  n: integer;
  thisObserver: IPatientEditObserver;
begin
  Assert( fEditUpdateLevel > 0 );
  dec( fEditUpdateLevel );
  if ( fEditUpdateLevel = 0 ) then
  begin
    Log.EnterMethod( Self, PROC_NAME );
    try
      n := 0;
      while n < fObservers.Count do
      begin
        if Supports( fObservers[n], IPatientEditObserver, thisObserver ) then
        begin
          Log.Event( '%s.%s: Notifying %s', [ClassName, PROC_NAME, fObservers[n].ClassName] );
          thisObserver.AfterEdit( Self );
        end;
        inc( n );
      end;
    finally
      Log.LeaveMethod( Self, PROC_NAME );
    end;
  end;
end;

{$ENDREGION}
{$REGION 'ICommandReceiver'}

function TActiveCase.ExecuteCmd( const ACommand: ICommand ): boolean;
begin
  Result := false;
  if ACommand.Matches( ACTIVE_CASE_SET_STATUS ) then
  begin
    SetStatus( ACommand.ReadInteger( PRM_STATUS_ID ), PersonId );
    Result := true;
  end;
end;

{$ENDREGION}
{$REGION 'ISelfRegisterCommandReceiver'}

procedure TActiveCase.RegisterCommands( const AMediator: ICommandMediator );
begin
  AMediator.RegisterReceiver( ACTIVE_CASE_SET_STATUS, Self );
end;

{$ENDREGION}

procedure TActiveCase.BeginEdit;
begin
  inc( fEditUpdateLevel );
end;

procedure TActiveCase.Clear;
begin
  BeginUpdate;
  try
    inherited;
    fVmrContainer.Clear;
    fJournalansvarlig := 0;
    fJournalansvarligNavn := EmptyStr;
    try
      Clipboard.Clear;
    except
      on E: Exception do
        Log.SilentError( E.Message );
    end;
  finally
    EndUpdate;
  end;
end;

procedure TActiveCase.RemoveActiveCaseObserver( AObject: TObject );
var
  thisListener: IListener;
begin
  if Assigned( AObject ) then
  begin
    fObservers.Remove( AObject );
    if Supports( AObject, IListener, thisListener ) then
      Detach( thisListener );
  end;
end;

procedure TActiveCase.RemoveAllActiveCaseObservers;
const
  PROC_NAME = 'RemoveAllObservers';
begin
  Log.EnterMethod( Self, PROC_NAME );
  try
    fObservers.Clear;
    DetachAll;
  finally
    Log.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TActiveCase.AddActiveCaseObserver( AObject: TObject );
const
  PROC_NAME = 'AddActiveCaseObserver';
var
  thisListener: IListener;
begin
  if Assigned( AObject ) then
  begin
    {
      Because an interfaced object is added as an object, there could be a problem with reference-counted objects.
      The reference count is not incremented, and this means that the object may be destroyed while this class still
      holds a reference to it, causing an AV at a later stage. The PrepareToDestroy method has been added to help.
    }
    Log.Event( '%s.%s: Adding observer of class %s at index #%d.', [ClassName, PROC_NAME, AObject.QualifiedClassName, fObservers.Count] );
    fObservers.Add( AObject );
    if Supports( AObject, IListener, thisListener ) then
      Attach( thisListener )
{$IFDEF Debug}
    else if Supports( AObject, IActiveCaseObserver ) then
      Log.Event( '%s.%s: Adding %s as an IActiveCaseObserver', [ClassName, PROC_NAME, AObject.ClassName] )
    else if Supports( AObject, IActiveCaseCloseObserver ) then
      Log.Event( '%s.%s: Adding %s as an IActiveCaseCloseObserver', [ClassName, PROC_NAME, AObject.ClassName] )
    else if Supports( AObject, IPatientEditObserver ) then
      Log.Event( '%s.%s: Adding %s as an IPatientEditObserver', [ClassName, PROC_NAME, AObject.ClassName] )
    else
      Log.SilentError( '%s.%s: An observer %s was added that supports neither the IListener, IActiveCase/IActiveCaseClose or the IPatientEditObserver interfaces.', [ClassName, PROC_NAME, AObject.ClassName] );
{$ENDIF}
  end;
end;

procedure TActiveCase.Touch;
begin
  try
    SQL.ExecuteCommand( CMD_TOUCH_STUDY_CASE, [StudyId, PersonId] );
  except
    on Exception do
      { Silent fail, not present until DB version 4.85 }
  end;
end;

function TActiveCase.Select( const APatId: integer ): boolean;
const
  PROC_NAME             = 'Select';
  LOG_SUCCESSFUL_SELECT = '%s.%s(%d): Successful';
begin
  Log.EnterMethod( Self, PROC_NAME );
  try
    BeginUpdate;
    if APatId <> PersonId then
      try
        Log.Event( '%s.%s( %d => %d) requested', [ClassName, PROC_NAME, PersonId, APatId] );
        NotifyBeforeClose;
        { Cleare person, and set to new PersonId }
        Clear;
        fPerson.PersonId := APatId;
        FPrimaryKey := APatId;
        Populate;
        { Make sure that we are allowed to select this person }
        if Valid and fAccessControl.TryGetAccess( Self ) then
        begin
          if fRevertToActiveOnSelect and ( StatusActive <> 1 ) and
          { } Log.LogYesNo( Format( ASK_REVERT_TO_ACTIVE, [StatusText, FullName] ), ltMessage ) then
            UpdateStatus( 1 );
          Log.SilentSuccess( LOG_SUCCESSFUL_SELECT, [ClassName, PROC_NAME, APatId] );
        end;
        NotifyAfterSelect;
      except
        on E: Exception do
        begin
          ClearIdentityAndData;
          Log.SilentWarning( E.Message );
        end;
      end;
    Result := ( PersonId = APatId );
  finally
    EndUpdate;
    Log.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TActiveCase.ClearIdentityAndData;
begin
  fPerson.Clear;
  FPrimaryKey := fPerson.PersonId;
  Clear;
end;

function TActiveCase.SelectRelation: integer;
begin
  Result := 0;
  try
    Result := PickList.SelectInteger( QRY_GET_RELATIONS, [], HDR_ROLES, TXT_SELECT_RELATION, MSG_NO_RELATIONS_AVAIL, true )
  except
    on E: Exception do
      Log.SilentError( E.Message );
  end;
end;

procedure TActiveCase.SetRelation( const ARelationId: integer );
begin
  SQL.ExecuteCommand( CMD_ADD_RELATION, [PersonId, ARelationId] );
  Populate;
end;

procedure TActiveCase.UpdateStatus( const AStatusId: integer );
begin
  BeginEdit;
  try
    SetStatus( AStatusId, PersonId );
  finally
    EndEdit;
  end;
end;

procedure TActiveCase.UpdateRelation;
var
  selectedRelationId: integer;
begin
  Assert( PersonId > 0 );
  selectedRelationId := SelectRelation;
  if selectedRelationId > 0 then
    SetRelation( selectedRelationId );
end;

procedure TActiveCase.UpdateGroup;
var
  selectedGroupId: integer;
begin
  Assert( PersonId > 0 );
  selectedGroupId := SelectGroup;
  if selectedGroupId > 0 then
    SetGroup( selectedGroupId, PersonId );
end;

procedure TActiveCase.NotifyBeforeClose;
const
  PROC_NAME = 'NotifyBeforeClose';
var
  thisObserver: IActiveCaseCloseObserver;
  n: integer;
begin
  Log.EnterMethod( Self, PROC_NAME );
  try
    n := 0;
    while n < fObservers.Count do
    begin
      if Supports( fObservers[n], IActiveCaseCloseObserver, thisObserver ) then
        try
          Log.Event( '%s.%s(%d): %s', [ClassName, PROC_NAME, n, TObject( thisObserver ).ClassName] );
          thisObserver.SaveActiveCaseData( Self );
        except
          on E: Exception do
            Log.SilentError( '%s.%s(%d): %s', [ClassName, PROC_NAME, PersonId, E.Message] );
        end;
      inc( n );
    end;
  finally
    Log.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TActiveCase.NotifyAfterSelect;
const
  PROC_NAME = 'NotifyAfterSelect';
var
  thisObserver: IActiveCaseObserver;
  n: integer;
begin
  Log.EnterMethod( Self, PROC_NAME );
  try
    n := 0;
    while n < fObservers.Count do
    begin
      if Supports( fObservers[n], IActiveCaseObserver, thisObserver ) then
        try
          Log.Event( '%s.%s(%d): %s', [ClassName, PROC_NAME, n, TObject( thisObserver ).ClassName] );
          thisObserver.LoadActiveCaseData( Self );
        except
          on E: Exception do
            Log.SilentError( '%s.%s(%d): %s', [ClassName, PROC_NAME, PersonId, E.Message] );
        end;
      inc( n );
    end;
  finally
    Log.LeaveMethod( Self, PROC_NAME );
  end;
end;

function TActiveCase.Get_IgnoreRelation: boolean;
begin
  Result := fAccessControl.IgnoreRelation;
end;

procedure TActiveCase.Set_IgnoreRelation( const AIgnoreRelation: boolean );
begin
  fAccessControl.IgnoreRelation := AIgnoreRelation;
end;

function TActiveCase.Get_IgnoreLocation: boolean;
begin
  Result := fAccessControl.IgnoreLocation;
end;

procedure TActiveCase.Set_IgnoreLocation( const AIgnoreLocation: boolean );
begin
  fAccessControl.IgnoreLocation := AIgnoreLocation;
end;

function TActiveCase.Get_JournalansvarligNavn: string;
begin
  if fJournalansvarligNavn = EmptyStr then
    Result := TXT_MISSING
  else
    Result := fJournalansvarligNavn;
end;

procedure TActiveCase.Load( ADataset: TDataset );
begin
  inherited Load( ADataset );
  fJournalansvarligNavn := ReadString( FLD_JA_NAME );
  fJournalansvarlig := ReadInteger( FLD_JA );
end;

function TActiveCase.Get_Journalansvarlig: integer;
begin
  Result := fJournalansvarlig;
end;

procedure TActiveCase.AssumeJournalansvar;
begin
  BeginUpdate;
  try
    SQL.ExecuteCommand( CMD_UPD_JOURNALANSVAR, [FStudyContext.StudyId, PersonId] );
    fJournalansvarligNavn := fActiveUser.FullName;
    fJournalansvarlig := fActiveUser.UserId;
  except
    on E: Exception do
      Log.Event( WARN_JOURNALANSVAR_FAILED, [E.Message], ltWarning );
  end;
  EndUpdate;
end;

end.
