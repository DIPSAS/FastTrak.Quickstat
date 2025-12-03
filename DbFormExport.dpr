program DbFormExport;

{$APPTYPE CONSOLE}
{$R *.res}

uses
  Emetra.Logging.PlainText,
  Emetra.Database.Simple,
  Emetra.Utils.Params,
  Emetra.Logging.Interfaces,
  Emetra.Database.ConnectionString,
  System.SysUtils, System.StrUtils,
  FormExport.SqlGenerator in 'FormExport.SqlGenerator.pas';

var
  db: TSimpleDatabase;
  formDumper: TFormDumper;
  prm: TParamList;
  formId: integer;
  ageInfo: char;

begin
  prm := TParamList.CreateFromCommandLine;
  WriteLn( '/* Form Export Utility v1.11, DIPS AS 2022 */' );
  formId := StrToIntDef( ParamStr( 1 ), 0 );
  ageInfo := AnsiUppercase( prm.ReadString( 'Age' ) + ' ' )[1];
  if formId = 0 then
  begin
    WriteLn( 'Use the FormId you want to export as the first parameter.' );
    WriteLn( 'Add /DoIt to save to a CSV file in current the directory.' );
    WriteLn( 'Use parameter Age=(Y|M|D) to include patient age at event.' );
  end
  else
  begin
    db := TSimpleDatabase.Create( GlobalLog );
    db.ConnectionString := GetFastTrakParentConnection;
    formDumper := TFormDumper.Create( db );
    case ageInfo of
      'Y': formDumper.DateInfo := diAgeYears;
      'M': formDumper.DateInfo := diAgeMonths;
      'D': formDumper.DateInfo := diAgeDays;
    else formDumper.DateInfo := diNone;
    end;
    try
      db.Connect;
      WriteLn( formDumper.GeneratePivotSQL( formId ) );
      if prm.Switch( 'DoIt' ) then
        formDumper.SaveToCSV( formId );
      WriteLn;
      WriteLn( 'The file ', formDumper.FileName, ' contains ', formDumper.LinesWritten, ' data rows. ' );
      db.Disconnect;
    except
      on E: Exception do
        WriteLn( E.ClassName, ': ', E.Message );
    end;
    formDumper.Free;
    db.Free;
  end;
  prm.Free;

end.
