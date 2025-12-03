unit EPR.QA.KristianJansen;

interface

uses
  EPR.QA.Collector.Standard;

type
  TBergerKeyDataCollector = class( TDataCollector )
  private
    fKeyValue: string;
    fFormName: string;
  public
    procedure AfterConstruction; override;
    function SQL: string; override;
  end;

implementation

uses
  StrUtils, SysUtils;

{ TFormDataCollector }
const
  KEY_FORM_DATA = 'select ce.EventTime as #PreEventTime,ce.CreatedAt AS #PreEventCreated,cf.CreatedAt AS #PreFormCreated,cf.FormComplete as #Pre,cf.FormStatus as #PreFormStatus,co.Quantity AS %s ';

procedure TBergerKeyDataCollector.AfterConstruction;
begin
  inherited;
  FTitle := 'KJ: Skjemadata';
  FKeyValue := 'BERGER_SKALA';
  FFormName := 'BERGER';
end;

function TBergerKeyDataCollector.SQL;
begin
  Result := 'SELECT'
end;
{
function TBergerKeyDataCollector.SQL: string;
var
  fldNames: string;
  tableNames: string;
begin
  fldNames := StringReplace( KEY_FORM_DATA, '#Pre', 'Berger', [rfReplaceAll] );
  fldNames := Format( fldNames, [FKeyValue] );
  tableNames := Format(
  ' FROM ClinForm cf JOIN ClinEvent ce ON ce.EventId=cf.EventId ' +
  ' JOIN MetaForm mf ON mf.FormId=cf.FormId ' +
  ' JOIN ClinObservation co ON co.EventId=ce.EventId AND co.VarName=%s ' +
  ' WHERE mf.FormName=%s AND cf.DeletedAt IS NULL AND ce.PersonId=:PersonId ',
  [ QuotedStr(FKeyValue),QuotedStr(FFormName)] );
  Result := fldNames + tableNames;
end;
}

end.
