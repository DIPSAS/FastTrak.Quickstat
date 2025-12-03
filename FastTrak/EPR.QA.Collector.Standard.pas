unit EPR.QA.Collector.Standard;

interface

uses
  EPR.QA.Collector.Names,
  EPR.QA.DataPoint,
  EPR.QA.PointFactory,
  EPR.QA.Collector.Base,
  EPR.QA.SQL,
  {General interfaces}
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces,
  {Standard}
  Generics.Collections, Classes, Graphics;

type
  TFormDataCollector = class( TDataCollector )
  public
    constructor Create( const ACollectorName, ATitle, AFormName: string; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
  end;

  TFormInstanceCollector = class( TDataCollector )
  public
    procedure AfterConstruction; override;
  end;

  TFormDataNumericCollector = class( TDataCollector )
    constructor Create( const ACollectorName, ATitle, AFormName: string; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
  end;

implementation

uses
  {Standard}
  Data.Db, System.SysUtils;

const
  QRY_FORM_DATA      = 'EXEC Report.GetFormData :PersonId, %s';
  QRY_FORM_INSTANCES = 'EXEC Report.GetFormInstances :PersonId';

  { TFormDataCollector }

constructor TFormDataCollector.Create( const ACollectorName, ATitle, AFormName: string; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
begin
  inherited Create( PREFIX_FORM + ACollectorName, ATitle, AFactory, ADb, ALog );
  FVarPrefix := Format( '%s.', [AFormName] );
  FSQL := Format( QRY_FORM_DATA, [QuotedStr( AFormName )] );
end;

{ TFormInstanceCollector }

procedure TFormInstanceCollector.AfterConstruction;
begin
  inherited;
  FVarPrefix := PREFIX_FORM;
  FSQL := QRY_FORM_INSTANCES;
end;

{ TFormDataNumericCollector }

constructor TFormDataNumericCollector.Create( const ACollectorName, ATitle, AFormName: string; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
begin
  inherited Create( PREFIX_FORM + ACollectorName, ATitle, AFactory, ADb, ALog );
  FVarPrefix := Format( '%s.', [AFormName] );
  FSQL := SpSnapshotFormDataNumeric( AFormName );
  fMaxBatchSize := 100;
end;

end.
