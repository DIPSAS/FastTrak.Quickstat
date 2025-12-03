unit EPR.QA.Collector.Diagnose;

interface

uses
  EPR.QA.Collector.Base,
  EPR.QA.PointFactory,
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces;

type
  TDiagnoseCollector = class( TDataCollector )
  strict private
    fDxPattern: string;
  public
    constructor Create( const ATitle, ADxPattern: string; const AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog ); reintroduce;
  end;

  TDementiaCollector = class( TDataCollector )
  public
    constructor Create( const ATitle: string; const AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog ); reintroduce;
  end;

implementation

uses
  EPR.QA.SQL,
  EPR.QA.Collector.Names,
  System.SysUtils;

{ TDiagnoseCollector }

constructor TDiagnoseCollector.Create( const ATitle, ADxPattern: string; const AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
begin
  fDxPattern := ADxPattern;
  inherited Create( PREFIX_DIAGNOSE_COLLECTOR + ConvertAtcPatternToVariableName( ADxPattern ), ATitle, AFactory, ADb, ALog );
  fSQL := SpDiagnoseByPattern( fDxPattern );
  FMaxBatchSize := maxint;
  FVarPrefix := PREFIX_DIAGNOSE_COLLECTOR;
end;

{ TDementiaCollector }

constructor TDementiaCollector.Create( const ATitle: string; const AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
begin
  inherited Create( PREFIX_DIAGNOSE_COLLECTOR + 'DEMENTIA', ATitle, AFactory, ADb, ALog );
  fSQL := SpDiagnoseDementiaAndAlzheimers;
  FMaxBatchSize := maxint;
  FVarPrefix := PREFIX_DIAGNOSE_COLLECTOR;
end;

end.
