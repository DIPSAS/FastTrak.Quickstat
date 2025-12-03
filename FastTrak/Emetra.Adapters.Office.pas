unit Emetra.Adapters.Office;

interface

uses
  Emetra.Logging.Interfaces;

type
  TExcelAdapter = class( TObject )
    class procedure LoadWithFile( const AFileName: string; ALog: ILog );
end;

implementation

uses
  Emetra.Classes.Tokenizer,
  Emetra.Win.Launcher,
  System.Win.Registry,
  System.SysUtils,
  WinApi.Windows;

class procedure TExcelAdapter.LoadWithFile( const AFileName: string; ALog: ILog );
const
  PROC_NAME = '%s.LoadWithFile: ';
var
  regKey: TRegistry;
  excelApplication: string;
  excelPath: string;
  thisLauncher: TMrLauncher;
  tokenizer: TTokenizer;
begin
  regKey := TRegistry.Create;
  try
    regKey.RootKey := HKEY_LOCAL_MACHINE;
    regKey.OpenKeyReadOnly('Software\Classes\Excel.Application\CLSID');
    excelApplication := regKey.ReadString('');
    ALog.Event( PROC_NAME + 'Excel.Application.CLSID=%s', [ClassName,excelApplication]);
    regKey.CloseKey;
    regKey.OpenKeyReadOnly(Format('Software\Classes\CLSID\%s\LocalServer32', [excelApplication]));
    excelPath := regKey.ReadString('');
    ALog.Event( PROC_NAME + 'Excel.Path=%s', [ClassName, excelPath]);
    begin
      thisLauncher := TMrLauncher.Create;
      tokenizer := TTokenizer.Create;
      try
        tokenizer.Prepare(excelPath, ' ');
        thisLauncher.Execute(tokenizer[0], AFileName);
      finally
        thisLauncher.Free;
        tokenizer.Free;
      end;
    end;
  finally
    regKey.Free;
  end;
end;

end.
