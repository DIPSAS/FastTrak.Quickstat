unit EPR.QA.Collector.VarSet;

interface

uses
  EPR.QA.Collector.Base,
  EPR.QA.PointFactory,
  {General}
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces;

type
  TVarSetCollector = class( TDataCollector )
  private
    constructor Create( const ACollectorName, ATitle: string; const AItemList: array of integer; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog ); reintroduce;
  public
    constructor CreateForText( const ACollectorName, ATitle: string; const AItemList: array of integer; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
    constructor CreateForEnum( const ACollectorName, ATitle: string; const AItemList: array of integer; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
    constructor CreateForNumeric( const ACollectorName, ATitle: string; const AItemList: array of integer; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
    constructor CreateForDate( const ACollectorName, ATitle: string; const AItemList: array of integer; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
  end;

  TVarSetAgeCollector = class( TDataCollector )
  public
    constructor Create( const ACollectorName, ATitle: string; const AItemList: array of integer; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog ); reintroduce;
  end;

  TVarSetMaxCollector = class( TDataCollector )
  public
    constructor Create( const ACollectorName, ATitle: string; const AItemList: array of integer; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog ); reintroduce;
  end;

  TFormAgeCollector = class( TDataCollector )
  public
    constructor Create( const ACollectorName, ATitle: string; const AFormName: string; AFactory: TDataPointFactory; const ADb: ISQL; const ALog: ILog ); reintroduce;
  end;

const
  ITEM_PREFIX     = 'ITEM.';
  ITEM_MAX_PREFIX = 'ITEMMAX.';
  ITEM_AGE_PREFIX = 'ITEMAGE.';
  FORM_AGE_PREFIX = 'FORMAGE.';

implementation

uses
  EPR.QA.SQL,
  {Standard}
  System.Classes, System.SysUtils;

resourcestring
  TXT_LAST = ' (siste)';
  TXT_MAX  = ' (høyeste)';

  { TVarSetCollector }

constructor TVarSetCollector.CreateForText( const ACollectorName, ATitle: string; const AItemList: array of integer; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
begin
  fSQL := SpSnapshotVarset( itText, AItemList );
  Create( ACollectorName, ATitle, AItemList, AFactory, ADb, ALog );
end;

constructor TVarSetCollector.CreateForNumeric( const ACollectorName, ATitle: string; const AItemList: array of integer; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
begin
  fSQL := SpSnapshotVarset( itNumeric, AItemList );
  Create( ACollectorName, ATitle, AItemList, AFactory, ADb, ALog );
end;

constructor TVarSetCollector.CreateForEnum( const ACollectorName, ATitle: string; const AItemList: array of integer; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
begin
  fSQL := SpSnapshotEnum( AItemList );
  Create( ACollectorName, ATitle, AItemList, AFactory, ADb, ALog );
end;

constructor TVarSetCollector.CreateForDate( const ACollectorName, ATitle: string; const AItemList: array of integer; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
begin
  fSQL := SpSnapshotVarset( itDate, AItemList );
  Create( ACollectorName, ATitle, AItemList, AFactory, ADb, ALog );
end;

constructor TVarSetCollector.Create( const ACollectorName, ATitle: string; const AItemList: array of integer; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
begin
  inherited Create( ACollectorName, ATitle + TXT_LAST, AFactory, ADb, ALog );
  FVarPrefix := EmptyStr;
  FMaxBatchSize := 100;
end;

constructor TVarSetAgeCollector.Create( const ACollectorName, ATitle: string; const AItemList: array of integer; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
begin
  inherited Create( ACollectorName, ATitle + TXT_LAST, AFactory, ADb, ALog );
  FVarPrefix := ITEM_AGE_PREFIX;
  FMaxBatchSize := 100;
  fSQL := SpSnapshotVarsetAge( AItemList );
end;

constructor TVarSetMaxCollector.Create( const ACollectorName, ATitle: string; const AItemList: array of integer; AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
begin
  inherited Create( ACollectorName, ATitle + TXT_MAX, AFactory, ADb, ALog );
  FVarPrefix := ITEM_MAX_PREFIX;
  FMaxBatchSize := 100;
  fSQL := SpMaximumQuantityVarset( AItemList );
end;

constructor TFormAgeCollector.Create( const ACollectorName, ATitle: string; const AFormName: string; AFactory: TDataPointFactory; const ADb: ISQL; const ALog: ILog );
begin
  inherited Create( ACollectorName, ATitle + TXT_LAST, AFactory, ADb, ALog );
  FVarPrefix := FORM_AGE_PREFIX;
  FMaxBatchSize := 100;
  fSQL := StringReplace( SpFormAgeSingle, FORM_NAME_PLACEHOLDER, QuotedStr( AFormName ), [] );
end;

end.
