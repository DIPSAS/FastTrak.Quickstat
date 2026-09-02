unit EPR.QA.Collector.Drug;

interface

uses
  {Standard}
  EPR.QA.SQL,
  EPR.QA.PointFactory,
  EPR.QA.Collector.Base,
  EPR.QA.Collector.Names,
  {General}
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces;

type
  TTreatTypeFilter = ( ttAnyTreatType, ttLongTerm, ttAsNeeded );

  TDrugCollector = class( TDataCollector )
  private
    fAtcPattern: string;
    fUseNameChecksumForDatapoint: boolean;
    fTreatTypeFilter: TTreatTypeFilter;
  protected
  public
    { Initialization }
    constructor Create( const AName, ATitle, AMatchPatternAtc: string; const AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog ); reintroduce;
    constructor CreateBasic( const ATitle, AMatchPatternAtc: string; const AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
    constructor CreateChecksum( const ATitle, AMatchPatternAtc: string; const AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
    constructor CreateForTreatType( const ATitle, AMatchPatternAtc: string; const ATreatType: TTreatTypeFilter; const AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
    procedure AfterConstruction; override;
    class var GroupResults: boolean;
  end;

const
  { ATC code patterns used }
  ATC_A02A     = 'A02A%'; { Antacida }
  ATC_A02B     = 'A02B%'; { Midler mot Ulcus og GERD }
  ATC_A06A     = 'A06A%'; { Midler mot forstoppelse }
  ATC_A10      = 'A10%'; { Blodsukkersenkende midler }
  ATC_A10A     = 'A10A%'; { Insulin og analoger }
  ATC_A10B     = 'A10B%'; { Blodsukkersenkende utenom insulin }
  ATC_A10BA    = 'A10BA%'; { Biguanider }
  ATC_A10BA02  = 'A10BA02'; { Metformin except combinations }
  ATC_A11EA    = 'A11EA'; { Vitamin B-kompleks, usammensatte preparater }
  ATC_B01AA03  = 'B01AA03';
  ATC_B01AF    = 'B01AF%';
  ATC_B03BA    = 'B03BA%';
  ATC_B03BA01  = 'B03BA01';
  ATC_B03BA03  = 'B03BA03';
  ATC_C0x23789 = 'C0[23789]%';
  ATC_C01A     = 'C01A%';
  ATC_C02      = 'C02%';
  ATC_C03      = 'C03%';
  ATC_C07      = 'C07%';
  ATC_C08      = 'C08%';
  ATC_C08D     = 'C08D%';
  ATC_C09      = 'C09%';
  ATC_C10      = 'C10%';
  ATC_M01A     = 'M01A%';
  ATC_N02A     = 'N02A%';
  ATC_N02B     = 'N02B%';
  ATC_N04BA    = 'N04BA%';
  ATC_N05A     = 'N05A%';
  ATC_N05B     = 'N05B%';
  ATC_N05C     = 'N05C%';
  ATC_N06A     = 'N06A%';
  ATC_N06D     = 'N06D%';
  ATC_J01XX05  = 'J01XX05';

const

  { SQL Statements for the various collectors }

  QRY_NORGEP = 'EXEC Report.NorGeP';

  QRY_DRUGCOUNT_BY_TYPE =
  { } 'SELECT PersonId, TreatType, COUNT(*) AS DpValue, MAX(CreatedAt) AS LastDate, Max(TreatId) AS MaxTreatId ' +
  { } SQL_FROM_ONGOING_TREATMENT +
  { } 'WHERE DATALENGTH(ATC) > 4 ' +
  { } 'GROUP BY PersonId, TreatType';

const
  VAR_TEMPLATE       = '{VarTemplate}';
  VAR_TEMPLATE_GROUP = '%s';
  VAR_TEMPLATE_SPLIT = 'CONCAT(%s,''.'',ot.TreatType)';

  QRY_DRUGSET_BASIC =
  { } 'SELECT ot.PersonId, ' + VAR_TEMPLATE + ' AS VarName, 1 AS DpValue, ot.StartAt, ot.TreatId, ai.AtcName AS Caption ' +
  { } SQL_FROM_ONGOING_TREATMENT +
  { } SQL_JOIN_ATC_INDEX +
  { } SQL_WHERE_PERSON_LIST +
  { } 'AND ot.ATC' + SQL_COLLATION + 'LIKE %s' + SQL_COLLATION;

  QRY_DRUGSET_CHECKSUM =
  { } 'SELECT ot.PersonId, ' + VAR_TEMPLATE + ' AS VarName, ABS(CHECKSUM(ot.DrugName)) %% 100000 AS DpValue, ot.StartAt, ot.TreatId, ai.AtcName AS Caption ' +
  { } SQL_FROM_ONGOING_TREATMENT +
  { } SQL_JOIN_ATC_INDEX +
  { } SQL_WHERE_PERSON_LIST +
  { } 'AND ot.ATC' + SQL_COLLATION + 'LIKE %s' + SQL_COLLATION;

  QRY_DRUGSET_ANTICHOLIN_AB =
  { } 'SELECT PersonId, ''' + VAR_ANTICHOLIN_AB + ''' AS VarName, ABS(CHECKSUM(DrugName)) % 100000 AS DpValue, StartAt, TreatId, Caption ' +
  { } 'FROM ' +
  { } '( ' +
  { } '  SELECT ot.PersonId, ot.DrugName, ot.StartAt, ot.TreatId, ai.AtcName AS Caption, ' +
  { } '    RANK() OVER ( PARTITION BY ot.PersonId ORDER BY ac.AlertLevel, ot.StartAt DESC ) AS ReverseOrder ' +
  { } SQL_FROM_ONGOING_TREATMENT +
  { } '  JOIN dbo.KBAnticholinDrug ac ON ac.ATC = ot.ATC AND ac.AlertLevel IN ( ''A'',''B'') ' +
  { } SQL_JOIN_ATC_INDEX +
  { } ') agg ' +
  { } SQL_WHERE_PERSON_LIST +
  { } 'AND ( ReverseOrder = 1 )';

  QRY_DRUGSET_ANTICHOLIN_N05A =
  { } 'SELECT PersonId, ''' + VAR_ANTICHOLIN_N05 + ''' AS VarName, ABS(CHECKSUM(DrugName)) % 100000 AS DpValue, StartAt, TreatId, ai.AtcName AS Caption ' +
  { } SQL_FROM_ONGOING_TREATMENT +
  { } SQL_JOIN_ATC_INDEX +
  { } SQL_WHERE_PERSON_LIST +
  { } 'AND ( ot.ATC ' + SQL_COLLATION + ' LIKE ''N05A%'' ' + SQL_COLLATION + ' ) ' +
  { } 'AND NOT ( ( ot.ATC' + SQL_COLLATION + 'LIKE ''N05AH0[34]''' + SQL_COLLATION + ') OR ( ot.ATC' + SQL_COLLATION + 'LIKE ''N05AN%''' + SQL_COLLATION + ') )';

  QRY_DRUGSET_METFORMIN =
  { } 'SELECT PersonId, ''' + VAR_METFORMIN + ''' AS VarName, ABS(CHECKSUM(DrugName)) % 100000 AS DpValue, StartAt, TreatId, Caption ' +
  { } 'FROM ' +
  { } '( ' +
  { } '  SELECT ot.PersonId, ot.DrugName, ot.StartAt, ot.TreatId, ai.AtcName AS Caption, ' +
  { } '    RANK() OVER ( PARTITION BY ot.PersonId ORDER BY ot.StartAt DESC ) AS ReverseOrder ' +
  { } SQL_FROM_ONGOING_TREATMENT +
  { } '  JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC AND ai.AtcName LIKE ''%METFORMIN%'' ' +
  { } ') agg ' +
  { } SQL_WHERE_PERSON_LIST +
  { } 'AND ( ReverseOrder = 1 )';

  QRY_DRUGCOUNT_BY_ATCGROUP =
  { } 'SELECT PersonId, ATC, COUNT(*) AS n, MAX(StartAt) AS LastDate, MAX(TreatId) AS MaxTreatId ' +
  { } SQL_FROM_ONGOING_TREATMENT +
  { } 'WHERE ATC IN (' +
  { } '''J01XX04'',''M04AC01'',''N05CM02'' )' +
  { } 'GROUP BY PersonId, ATC ' +
  { } 'UNION ' +
  { } 'SELECT PersonId, SUBSTRING(ATC,1,5) AS ATCFragment, COUNT(*) AS n, MAX(StartAt) AS LastDate, MAX(TreatId) AS MaxTreatId ' +
  { } SQL_FROM_ONGOING_TREATMENT +
  { } 'WHERE SUBSTRING(ATC,1,5) IN (' +
  { } '''A10BA'',''B01AE'',''B01AF'',''B03BA'',''B03BB'',''G04BD'',''M04AA'',''M04AB'',''N02AA'',''N02AB'',' +
  { } '''N02AE'',''N02AG'',''N02AJ'',''N02AX'',''N02BA'',''N05BA'',''N05BB'',''N05CD'',''N05CF'',''N05CH'',' +
  { } '''N06DA'',''N06DX'',''R03AC'',''R03AK'',''R03BB'',''R03DA'',''R06AD'',''R06AE'',''R06AX'' )' +
  { } 'GROUP BY PersonId, SUBSTRING(ATC,1,5) ' +
  { } 'UNION ' +
  { } 'SELECT PersonId, SUBSTRING(ATC,1,4) AS ATCFragment, COUNT(*) AS n, MAX(StartAt) AS LastDate, MAX(TreatId) AS MaxTreatId ' +
  { } SQL_FROM_ONGOING_TREATMENT +
  { } 'WHERE SUBSTRING(ATC,1,4) IN (' +
  { } '''A02A'',''A02B'',''A06A'',''A10A'',''A10B'',''B01A'',''B01C'',''B03A'',''C01A'',''G04C'',' +
  { } '''H03A'',''M01A'',''N03A'',''N05A'',''N06A'',''N06D'',''S01E'' )' +
  { } 'GROUP BY PersonId, SUBSTRING(ATC,1,4) ' +
  { } 'UNION ' +
  { } 'SELECT PersonId, SUBSTRING(ATC,1,3) AS ATCFragment, COUNT(*) AS n, MAX(StartAt) AS LastDate, MAX(TreatId) AS MaxTreatId ' +
  { } SQL_FROM_ONGOING_TREATMENT +
  { } 'WHERE SUBSTRING(ATC,1,3) IN (' +
  { } '''A11'',''C02'',''C03'',''C07'',''C08'',''C09'',''H02'',''N04'' )' +
  { } 'GROUP BY PersonId, SUBSTRING(ATC,1,3) ' +
  { } 'ORDER BY PersonId';

implementation

uses
  System.SysUtils, System.StrUtils, System.RegularExpressions;

{ TDrugCollector }

constructor TDrugCollector.Create( const AName, ATitle, AMatchPatternAtc: string; const AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
begin
  inherited Create( AName, ATitle, AFactory, ADb, ALog );
  fAtcPattern := AMatchPatternAtc;
end;

constructor TDrugCollector.CreateBasic( const ATitle, AMatchPatternAtc: string; const AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
var
  collectorNamePrefix: string;
begin
  case fTreatTypeFilter of
    ttLongTerm: collectorNamePrefix := PREFIX_DRUGFAST_COLLECTOR;
    ttAsNeeded: collectorNamePrefix := PREFIX_DRUGNEED_COLLECTOR;
  else collectorNamePrefix := PREFIX_DRUG_COLLECTOR;
  end;
  inherited Create( collectorNamePrefix + ConvertAtcPatternToVariableName( AMatchPatternAtc ), ATitle, AFactory, ADb, ALog );
  fAtcPattern := AMatchPatternAtc;
end;

constructor TDrugCollector.CreateChecksum( const ATitle, AMatchPatternAtc: string; const AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
begin
  fUseNameChecksumForDatapoint := true;
  CreateBasic( ATitle, AMatchPatternAtc, AFactory, ADb, ALog );
end;

constructor TDrugCollector.CreateForTreatType( const ATitle, AMatchPatternAtc: string; const ATreatType: TTreatTypeFilter; const AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
begin
  fUseNameChecksumForDatapoint := true;
  fTreatTypeFilter := ATreatType;
  CreateBasic( ATitle, AMatchPatternAtc, AFactory, ADb, ALog );
end;

procedure TDrugCollector.AfterConstruction;
var
  sqlTemplate: string;
begin
  inherited;
  FVarPrefix := VAR_PREFIX_DRUG;
  if fUseNameChecksumForDatapoint then
    sqlTemplate := QRY_DRUGSET_CHECKSUM
  else
    sqlTemplate := QRY_DRUGSET_BASIC;
  if GroupResults then
    sqlTemplate := StringReplace( sqlTemplate, VAR_TEMPLATE, VAR_TEMPLATE_GROUP, [rfReplaceAll] )
  else
    sqlTemplate := StringReplace( sqlTemplate, VAR_TEMPLATE, VAR_TEMPLATE_SPLIT, [rfReplaceAll] );
  fSQL := Format( sqlTemplate, [QuotedStr( ConvertAtcPatternToVariableName( fAtcPattern ) ), QuotedStr( fAtcPattern )] );
  case fTreatTypeFilter of
    ttAnyTreatType:;
    ttLongTerm: fSQL := fSQL + SQL_AND_FAST;
    ttAsNeeded: fSQL := fSQL + SQL_AND_BEHOV;
  end;
  FMaxBatchSize := 100;
end;

end.
