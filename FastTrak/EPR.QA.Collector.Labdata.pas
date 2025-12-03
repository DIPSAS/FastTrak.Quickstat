unit EPR.QA.Collector.Labdata;

interface

uses
  EPR.QA.Definitions,
  EPR.QA.PointFactory,
  EPR.QA.Collector.Base,
  VMR.Lab.Interfaces,
  {General}
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces;

type

  TLabSetCollector = class( TDataCollector )
  public
    constructor Create( const ACollectorName, AGroupName: string; const ALabClassSet: TLabClassSet; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog ); reintroduce; overload;
    constructor CreateOldSchool( const ACollectorName, ATitle: string; const ALabSet: TLabSet; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog ); overload;
  end;

  TLabHighTrustCollector = class( TDataCollector )
    procedure AfterConstruction; override;
  end;

  TLabMediumTrustCollector = class( TDataCollector )
    procedure AfterConstruction; override;
  end;

  TLabLowTrustCollector = class( TDataCollector )
    procedure AfterConstruction; override;
  end;

implementation

uses
  EPR.QA.Collector.Names,
  EPR.QA.SQL,
  {Standard}
  System.Classes, System.SysUtils;

{ TLabSetCollector }

constructor TLabSetCollector.Create( const ACollectorName, AGroupName: string; const ALabClassSet: TLabClassSet; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
var
  labSetTitle: string;
begin
  { Override title }
  if pos( ':', AGroupName ) = 0 then
    labSetTitle := Format( StrTitleLabsetTemplate, [AGroupName] )
  else
    labSetTitle := AGroupName;
  inherited Create( ACollectorName, labSetTitle, AFactory, ADb, ALog );
  FVarPrefix := PREFIX_LAB_VARIABLE;
  FSQL := SpSnapshotLabset( ALabClassSet );
  FMaxBatchSize := 100;
end;

constructor TLabSetCollector.CreateOldSchool( const ACollectorName, ATitle: string; const ALabSet: TLabSet; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
var
  lt: TLabTest;
  labSet: TLabClassSet;
  setIndex: integer;
begin
  { Create a TLabClassSet representation of TLabSet }
  SetLength( labSet, 1024 );
  setIndex := 0;
  for lt in ALabSet do
  begin
    labSet[setIndex] := ord( lt );
    inc( setIndex );
  end;
  SetLength( labSet, setIndex );
  { Use the other constructor }
  Create( ACollectorName, ATitle, labSet, AFactory, ADb, ALog );
end;

procedure TLabHighTrustCollector.AfterConstruction;
begin
  inherited;
  FVarPrefix := PREFIX_LAB_VARIABLE;
  FSQL := SpSnapshotLabdataByTrustLevel( 3 );
  FMaxBatchSize := 100;
end;

procedure TLabMediumTrustCollector.AfterConstruction;
begin
  inherited;
  FVarPrefix := PREFIX_LAB_VARIABLE;
  FSQL := SpSnapshotLabdataByTrustLevel( 2 );
  FMaxBatchSize := 100;
end;

procedure TLabLowTrustCollector.AfterConstruction;
begin
  inherited;
  FVarPrefix := PREFIX_LAB_VARIABLE;
  FSQL := SpSnapshotLabdataByTrustLevel( 1 );
  FMaxBatchSize := 100;
end;

end.
