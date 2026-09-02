program QuickStat;

uses
  {$IFDEF Debug }
  Emetra.Logging.SmartInspect,
  {$ELSE}
  Emetra.Logging.PlainText,
  {$ENDIF}
  Vcl.Forms,
  MainQuickStat in 'MainQuickStat.pas' {frmQuickStat};

{$R *.res}

begin
  ReportMemoryLeaksOnShutdown := true;
  Application.Initialize;
  Application.MainFormOnTaskbar := true;
  Application.Title := 'DIPS QuickStat';
  Application.CreateForm(TfrmQuickStat, frmQuickStat);
  Application.Run;

end.
