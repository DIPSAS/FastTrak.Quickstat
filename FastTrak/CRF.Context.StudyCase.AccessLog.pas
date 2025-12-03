unit CRF.Context.StudyCase.AccessLog;

interface

uses
  CRF.Person.StudyCase.Interfaces,
  CRF.User.Interfaces,
  CRF.SQL,
  {General}
  Emetra.Database.Interfaces,
  Emetra.Interfaces.Observer,
  Emetra.Logging.Interfaces,
  Emetra.Business.BaseClass,
  {Standard}
  System.Classes;

type
  TActiveCaseAccessLogger = class( TCustomBusinessReferenceCounted, IListener )
  strict private
    fUser: ICRFActiveUser;
    fSQL: ISQL;
    fLastPerson: integer;
    fEventGuid: string;
  private
    procedure AfterUpdate( Sender: TObject );
  public
    constructor Create( AUser: ICRFActiveUser; ASQL: ISQL; ALog: ILog ); reintroduce;
  end;

implementation

uses
  Emetra.StrUtils,
  Emetra.Person.Interfaces,
  System.SysUtils;

  { TActiveCaseAccessLogger }

constructor TActiveCaseAccessLogger.Create( AUser: ICRFActiveUser; ASQL: ISQL; ALog: ILog );
begin
  inherited Create( ALog );
  fUser := AUser;
  fSQL := ASQL;
end;

procedure TActiveCaseAccessLogger.AfterUpdate( Sender: TObject );
var
  currPerson: IActiveStudyCase;
begin
  if Supports( Sender, ICRFStudyCase, currPerson ) then
    try
      { Log close event if fLastPerson > 0 }
      if currPerson.PersonId <> fLastPerson then
        if fLastPerson > 0 then
          fSQL.ExecuteCommand( CMD_CLOSE_EVENT, [fEventGuid] );
      fLastPerson := currPerson.PersonId;
      { Log open event if new person >  0 }
      if currPerson.PersonId > 0 then
      begin
        fEventGuid := GetNewGuid;
        fSQL.ExecuteCommand( CMD_OPEN_EVENT, [fEventGuid, currPerson.PersonId, currPerson.ClinRelId] );
      end;
    except
      on E: Exception do
        Log.SilentError( E.Message );
    end;
end;

end.
