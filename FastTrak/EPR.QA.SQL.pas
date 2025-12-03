unit EPR.QA.SQL;

interface

type
  TCrfVarType = ( itNumeric, itDate, itText );

const

  { Placeholders in SQL statements }

  FORM_NAME_PLACEHOLDER = '{FormName}';
  PID_LIST_PLACEHOLDER  = '{IdList}';
  ITEM_LIST_PLACEHOLDER = '{ItemList}';
  LAB_LIST_PLACEHOLDER  = '{LabList}';

  { Fields }
  FLD_FORM_NAME       = 'FormName';
  FLD_FORM_TITLE      = 'FormTitle';
  FLD_VAR_SPEC        = 'VarSpec';
  FLD_VAR_NAME        = 'VarName';
  FLD_CAPTION         = 'Caption';
  FLD_VAR_DESCRIPTION = 'VarDescription';
  FLD_ROW_ID          = 'RowId';
  FLD_DATA_ELEMENTS   = 'DataElements';
  FLD_COMMENT         = 'Comment';
  FLD_TITLE           = 'Title';
  FLD_PROC_ID         = 'ProcId';
  FLD_SELECTION_ID    = 'SelId';
  FLD_STUDY_ID        = 'StudyId';

  { Queries }

const
  QRY_STUDY_BY_NAME    = 'SELECT StudyId FROM dbo.Study WHERE StudName=:StudName';
  QRY_POPULATIONS      = 'EXEC dbo.GetPopulations :StudyId';
  QRY_POPULATION_BY_ID = 'SELECT ProcName, ProcDesc, ProcParams FROM dbo.DbProcList WHERE ProcId=:ProcId';

  { Selection }
  QRY_ADD_SELECTION        = 'EXEC Report.AddSelection :StudyId, :Title, :Description';
  CMD_ADD_SELECTION_MEMBER = 'EXEC Report.AddSelectionMember :SelId, :PersonId';

  { Packaged datasets and captions }
  QRY_GET_PACKAGES = 'SELECT r.* FROM Report.QuickStat r JOIN dbo.Study s ON s.StudyId=r.StudyId WHERE r.StudyId=:StudyId';
  CMD_ADD_PACKAGE  = 'EXEC Report.AddQuickStat :StudyId,:ProcId,:Title,:DataElements,:Comment';

  { Collectors }
  QRY_FORM_CLASSES = 'EXEC Report.GetFormClasses :StudyId';

  QRY_DEMOGRAPHICS =
  { } 'SELECT PersonId,''%s'' AS ' + FLD_VAR_NAME + ', %s AS DpValue, GETDATE() AS VarDate, PersonId AS ResultId ' +
  { } 'FROM dbo.Person WHERE (PersonId IN ' + PID_LIST_PLACEHOLDER + ')';

  { Column providers }
  QRY_LAB_QUARTERS = 'EXEC Report.ColLabQuarters %d'; // ProcId = 9000
  QRY_TVANGSVEDTAK = 'EXEC Report.ColGbdTvangsvedtak'; // ProcId = 9001

  { Grid Caption overrides }
function QueryItemCaptions: string;
function QueryLabCaptions: string;
function QueryCustomCaptions: string;

{ StudyCase, StudyGroup and StudyCenter }
function SpStudCaseFields( const AVarName, AFieldName: string; const AStudyId: integer ): string;
function SpStudyCenter( const AStudyId: integer ): string;
function SpStudyGroupDeath( const AStudyId: integer ): string;
function SpStudyCenterDeath( const AStudyId: integer ): string;

{ Form count, presence and completeness }
function SpRecentFormCountSingle( const AFormName: string; const AMonthCount: integer ): string;
function SpRecentFormGroupCount( const AVarName: string; const AFormNameList: string; const AMonthCount: integer ): string;
function SpRecentFormGroupLege3m: string;

{ Drug-Drug interaction }
function SpDruidIndividualInteractions( const AMinCount: integer ): string;
function SpDruidCountByLevel: string;

{ Diagnose }
function SpDiagnoseDetailsByLevel( const ALevel: integer ): string;
function SpDiagnoseByPattern( const APattern: string ): string;
function SpDiagnoseDementiaAndAlzheimers: string;

{ Drug related }
function SpDrugCountNoAtc: string;
function SpDrugWithoutDiagnose( const AVarName, ADrugPattern, ADxPattern: string ): string;
function SpDrugAndRenalFunction( const ADrugPattern: string; const ALowGfrValueThreshold: integer ): string;
function SpDrugHypertensionWithLowBp( const ALowBpThreshold: integer ): string;
function SpDrugsetAntibiotic: string;

{ Snapshot no matter the age }
function SpSnapshotEnum( const AItemIds: array of integer ): string;
function SpSnapshotVarset( const AVariableDataType: TCrfVarType; const AItemIds: array of integer ): string;
function SpSnapshotVarsetAge( const AItemIds: array of integer ): string;
function SpSnapshotLabdataByTrustLevel( const ATrustLevel: integer ): string;
function SpSnapshotLabset( const ALabClassIds: array of integer ): string;
function SpSnapshotQuantityIfBelowThreshold( const AItemId: integer; const AValue: double ): string;
function SpSnapshotFormDataNumeric( const AFormName: string ): string;
function SpSnapshotLabQuarters( const ALabClassId: integer ): string;

{ Maximum values }

function SpMaximumQuantityVarset( const AItemIds: array of integer ): string;

{ Recent data found }
function SpRecentFormCountAll( const AMonthCount: integer ): string;
function SpRecentFormCompleteness( const AFormName: string; const AMonths: integer ): string;
function SpRecentQuantityPresent( const AItemId, AMonthsAgo: integer ): string;
function SpRecentLabdataPresent( const AMonthsAgo: integer ): string;

{ Age of datapoints, forms etc }
function SpFormAgeSingle: string;
function SpFlackerKileyDeath: string;

function ConvertAtcPatternToVariableName( const AMatchPatternAtc: string ): string;
function ConvertArrayToList( const AIdentifiers: array of integer ): string;

const
  { Collation issues can be dangerous when matching ATCs with LIKE because "A%" will not match "AA" }
  SQL_COLLATION              = ' COLLATE Latin1_General_CI_AI ';
  SQL_WHERE_PERSON_LIST      = 'WHERE ( PersonId IN ' + PID_LIST_PLACEHOLDER + ' ) ';
  SQL_AND_FAST               = ' AND ot.TreatType IN (''F'',''U'')';
  SQL_AND_BEHOV              = ' AND ot.TreatType = ''B''';
  SQL_JOIN_ATC_INDEX         = 'LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC ';
  SQL_FROM_ONGOING_TREATMENT = 'FROM dbo.OngoingTreatment ot ';

implementation

uses
  System.SysUtils, System.RegularExpressions;

function ConvertArrayToList( const AIdentifiers: array of integer ): string;
var
  itemId: integer;
begin
  Result := EmptyStr;
  for itemId in AIdentifiers do
    Result := Result + ', ' + IntToStr( itemId );
  Result := Copy( Result, 3, maxint );
end;

function ConvertAtcPatternToVariableName( const AMatchPatternAtc: string ): string;
begin
  { Generate name based on ATC, remove invalid variable characters }
  Result := TRegEx.Replace( AMatchPatternAtc, '\[', 'x' );
  Result := TRegEx.Replace( Result, '[%\]]', EmptyStr );
end;

{$REGION 'Grid Caption overrides'}

function QueryLabCaptions: string;
begin
  Result := 'SELECT ISNULL(NLK, Report.LabClassName(LabClassId)) AS ' + FLD_VAR_NAME + ', FriendlyName AS ' + FLD_CAPTION + ', NULL AS ' + FLD_VAR_DESCRIPTION +
    ' FROM dbo.LabClass ORDER BY LabClassId';
end;

function QueryItemCaptions: string;
begin
  Result :=
  { } 'SELECT mi.' + FLD_VAR_NAME + ', ISNULL(mfi.ItemHeader,mfi.ItemText) AS ' + FLD_CAPTION + ', mfi.ItemHelp AS ' + FLD_VAR_DESCRIPTION + ' ' +
  { } 'FROM dbo.MetaFormItem mfi ' +
  { } 'JOIN dbo.MetaItem mi ON mi.ItemId = mfi.ItemId ORDER BY mfi.FormId';
end;

function QueryCustomCaptions: string;
begin
  Result := 'SELECT VarSpec AS ' + FLD_VAR_NAME + ', ' + FLD_CAPTION + ', ' + FLD_VAR_DESCRIPTION + ' FROM Report.ColumnCaption';
end;

{$ENDREGION}
{$REGION 'Form count, presence and completeness'}

function SpSnapshotFormDataNumeric( const AFormName: string ): string;
const
  QRY_FORMDATA_NUMERIC =
  { } 'SELECT agg.* FROM ' +
  { } '( ' +
  { } '  SELECT ce.PersonId, mi.VarName, ISNULL(dp.Quantity,DATEDIFF(DD,''1899-12-30'',dp.DTVal)) AS DataValue, ce.EventTime, dp.RowId, ' +
  { } '    RANK() OVER ( PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC ) AS OrderBy ' +
  { } '  FROM dbo.ClinDatapoint dp ' +
  { } '    JOIN dbo.ClinEvent ce ON ce.EventId = dp.EventId ' +
  { } '    JOIN dbo.ClinForm cf ON cf.EventId = ce.EventId ' +
  { } '    JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId ' +
  { } '    JOIN dbo.MetaItem mi ON mi.ItemId = dp.ItemId AND mi.ItemType IN (1,2,5) ' +
  { } '    JOIN dbo.MetaFormItem mfi ON mfi.FormId = cf.FormId AND mfi.ItemId = mi.ItemId ' +
  { } '  WHERE ( mf.FormName = %s ) ' +
  { } '  AND ( ce.PersonId IN ' + PID_LIST_PLACEHOLDER + ' )' +
  { } ') agg ' +
  { } 'WHERE agg.OrderBy = 1';
begin
  Result := Format( QRY_FORMDATA_NUMERIC, [QuotedStr( AFormName )] );
end;

function SpRecentFormCountAll( const AMonthCount: integer ): string;
const
  QRY_ALL_FORMS_MONTHS =
  { } 'SELECT ce.PersonId, UPPER(mf.FormName) AS ' + FLD_VAR_NAME + ', COUNT(*) AS DpValue, MAX(ce.EventTime) AS VarDate, MAX(cf.ClinFormId) AS MaxClinFormId ' +
  { } 'FROM dbo.ClinForm cf ' +
  { } 'JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId ' +
  { } 'JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId ' +
  { } 'WHERE ( DATEDIFF( MM, ce.EventTime, GETDATE() ) < %d ) AND ( cf.DeletedAt IS NULL ) ' +
  { } 'GROUP BY ce.PersonId, mf.FormName';
begin
  Result := Format( QRY_ALL_FORMS_MONTHS, [AMonthCount] );
end;

function SpRecentFormCountSingle( const AFormName: string; const AMonthCount: integer ): string;
const
  QRY_SINGLE_FORM_MONTHS =
  { } 'SELECT ce.PersonId, UPPER(mf.FormName) AS ' + FLD_VAR_NAME + ', COUNT(*) AS DpValue, MAX(ce.EventTime) AS VarDate, MAX(cf.ClinFormId) AS MaxClinFormId ' +
  { } 'FROM dbo.ClinForm cf ' +
  { } 'JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId ' +
  { } 'JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId AND mf.FormName=%s ' +
  { } 'WHERE ( DATEDIFF( MM, ce.EventTime, GETDATE() ) < %d ) AND ( cf.DeletedAt IS NULL ) ' +
  { } 'GROUP BY ce.PersonId, mf.FormName';
begin
  Result := Format( QRY_SINGLE_FORM_MONTHS, [QuotedStr( AFormName ), AMonthCount] );
end;

function SpRecentFormGroupCount( const AVarName: string; const AFormNameList: string; const AMonthCount: integer ): string;
const
  QRY_FORM_GROUP_MONTHS =
  { } 'SELECT ce.PersonId, UPPER(%s) AS ' + FLD_VAR_NAME + ', COUNT(*) AS DpValue, MAX(ce.EventTime) AS MaxEventTime, MAX(cf.ClinFormId) AS MaxClinFormId ' +
  { } 'FROM dbo.ClinForm cf ' +
  { } 'JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId ' +
  { } 'JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId AND mf.FormName IN ( %s ) ' +
  { } 'WHERE ( DATEDIFF( MM, ce.EventTime, GETDATE() ) < %d ) AND ( cf.DeletedAt IS NULL ) ' +
  { } 'GROUP BY ce.PersonId';
begin
  Result := Format( QRY_FORM_GROUP_MONTHS, [AVarName, '''' + TRegEx.Replace( AFormNameList, '\s*,\s*', ''',''' ) + '''', AMonthCount] );
end;

function SpRecentFormGroupLege3m: string;
const
  GBD_LEGENOTATER     = 'GBD_NOTAT_LEGE,GBD_STATUS_PRESENS,GBD_INFECTION,GBD_BESLUTNINGER';
  VAR_GBD_LEGENOTATER = 'GBDLEGE';
begin
  Result := SpRecentFormGroupCount( QuotedStr( VAR_GBD_LEGENOTATER ), GBD_LEGENOTATER, 3 );
end;

function SpRecentFormCompleteness( const AFormName: string; const AMonths: integer ): string;
const
  QRY_FORM_ROUTINE =
  { } 'SELECT PersonId, VarName, DpValue, EventTime, ClinFormId ' +
  { } 'FROM ' +
  { } '( ' +
  { } '  SELECT ce.PersonId, mf.FormName AS VarName, cf.FormComplete AS DpValue, ce.EventTime, cf.ClinFormId, ' +
  { } '  RANK() OVER (Partition by ce.PersonId ORDER BY cf.FormComplete, ce.EventNum DESC) AS rnk ' +
  { } '  FROM dbo.ClinForm cf ' +
  { } '  JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId ' +
  { } '  JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId ' +
  { } '  WHERE mf.FormName = %s AND cf.FormComplete > 0 AND cf.DeletedAt IS NULL ' +
  { } '  AND DATEDIFF( MM, ce.EventTime, GETDATE() ) < %d ' +
  { } ') agg ' +
  { } 'WHERE agg.rnk = 1';
begin
  Result := Format( QRY_FORM_ROUTINE, [QuotedStr( AFormName ), AMonths] );
end;

{$ENDREGION}
{$REGION 'Drug-Drug Interactions'}

function SpDruidIndividualInteractions( const AMinCount: integer ): string;
const
  QRY_DRUID_DETAILS =
  { } 'SELECT a.PersonId, REPLACE(agg.AlertClass,''#'','''') AS VarName, AlertLevel AS DpValue,CreatedAt, a.AlertId, a.AlertHeader AS Caption ' +
  { } 'FROM ' +
  { } '( ' +
  { } '  SELECT AlertClass, COUNT(*) AS n FROM dbo.Alert ' +
  { } '   WHERE AlertClass LIKE ''DRUID#%%''' +
  { } '  GROUP BY AlertClass ' +
  { } ') agg ' +
  { } 'JOIN dbo.Alert a ON a.AlertClass = agg.AlertClass ' +
  { } 'WHERE agg.n > %d ' +
  { } 'ORDER BY PersonId';
begin
  Result := Format( QRY_DRUID_DETAILS, [AMinCount] );
end;

function SpDruidCountByLevel: string;
const
  QRY_DRUID_GLOBAL =
  { } 'SELECT PersonId, ' +
  { } '  CASE AlertLevel WHEN 1 THEN ''GREEN'' WHEN 2 THEN ''YELLOW'' WHEN 3 THEN ''ORANGE'' WHEN 4 THEN ''RED'' END AS DruidLevel, ' +
  { } '  n, MaxAlertDate, MaxAlertId ' +
  { } 'FROM ' +
  { } '( ' +
  { } '  SELECT PersonId, AlertLevel, MAX(CreatedAt) AS MaxAlertDate, COUNT(*) AS n, MAX(AlertId) AS MaxAlertId FROM dbo.Alert ' +
  { } '  WHERE ( AlertClass LIKE ''DRUID%'' ) AND ( AlertLevel > 0 ) ' +
  { } '  GROUP BY PersonId, AlertLevel ' +
  { } ') agg ' +
  { } 'ORDER BY PersonId';
begin
  Result := QRY_DRUID_GLOBAL;
end;

{$ENDREGION}
{$REGION 'Diagnose' }

function SpDiagnoseDetailsByLevel( const ALevel: integer ): string;
const
  QRY_DIAGNOSE_DETAILS =
  { } 'SELECT PersonId, SUBSTRING(ItemCode,1,%0:d) AS VarName, COUNT(*) AS DpValue, MIN(CreatedAt) AS MinCreatedAt, MIN(ProbId) AS MinProbId FROM  ' +
  { } '( ' +
  { } '  SELECT PersonId, mni.ItemCode, cp.ListItem, cp.CreatedAt, cp.ProbId ' +
  { } '  FROM dbo.ClinProblem cp  ' +
  { } '  JOIN dbo.MetaProblemType mp ON mp.ProbType = cp.ProbType AND mp.ProbActive = 1 ' +
  { } '  JOIN dbo.MetaNomListItem li ON li.ListItem = cp.ListItem ' +
  { } '  JOIN dbo.MetaNomItem mni ON mni.ItemId = li.ItemId ' +
  { } ') pro ' +
  { } 'GROUP BY PersonId, SUBSTRING(ItemCode,1,%0:d) ';
begin
  Result := Format( QRY_DIAGNOSE_DETAILS, [ALevel] );
end;

function SpDiagnoseByPattern( const APattern: string ): string;
const
  QRY_DIAGNOSIS_GLOBAL =
  { } 'SELECT PersonId, VarName, ListItem, CreatedAt, ProbId, Caption ' +
  { } 'FROM ( ' +
  { } '  SELECT cp.PersonId, %s AS VarName, cp.ListItem, cp.CreatedAt, cp.ProbId, mni.ItemCode AS Caption, ' +
  { } '  RANK() OVER ( PARTITION BY cp.PersonId ORDER BY cp.CreatedAt ) AS OrderNo ' +
  { } '  FROM dbo.ClinProblem cp ' +
  { } '  JOIN dbo.MetaProblemType pt ON pt.ProbType = cp.ProbType AND pt.ProbActive = 1 ' +
  { } '  JOIN dbo.MetaNomListItem mnli ON mnli.ListItem = cp.ListItem ' +
  { } '  JOIN dbo.MetaNomItem mni ON mni.ItemId = mnli.ItemId ' +
  { } '  WHERE ( mni.ItemCode LIKE %s ) ' +
  { } ') agg WHERE OrderNo = 1 ';
var
  varName: string;
begin
  varName := ConvertAtcPatternToVariableName( APattern );
  Result := Format( QRY_DIAGNOSIS_GLOBAL, [QuotedStr( varName ), QuotedStr( APattern )] );
end;

function SpDiagnoseDementiaAndAlzheimers: string;
const
  QRY_DEMENTIA_ALZHEIMER_GLOBAL =
  { } 'SELECT PersonId, VarName, ListItem, CreatedAt, ProbId, Caption ' +
  { } 'FROM ( ' +
  { } '  SELECT cp.PersonId, ''DEMENTIA'' AS VarName, cp.ListItem, cp.CreatedAt, cp.ProbId, mni.ItemCode AS Caption, ' +
  { } '  RANK() OVER ( PARTITION BY cp.PersonId ORDER BY cp.CreatedAt ) AS OrderNo ' +
  { } '  FROM dbo.ClinProblem cp ' +
  { } '  JOIN dbo.MetaProblemType pt ON pt.ProbType = cp.ProbType AND pt.ProbActive = 1 ' +
  { } '  JOIN dbo.MetaNomListItem mnli ON mnli.ListItem = cp.ListItem ' +
  { } '  JOIN dbo.MetaNomItem mni ON mni.ItemId = mnli.ItemId ' +
  { } '  AND ( mni.ItemCode LIKE ''F0[0123]%'' OR mni.ItemCode LIKE ''G30%'' ) ' +
  { } ') agg WHERE OrderNo = 1 ';
begin
  Result := QRY_DEMENTIA_ALZHEIMER_GLOBAL;
end;

{$ENDREGION}
{$REGION 'Drug related'}

function SpDrugWithoutDiagnose( const AVarName, ADrugPattern, ADxPattern: string ): string;
const
  QRY_DRUG_VS_DIAGNOSE =
  { } 'SELECT rx.PersonId, %s AS VarName, rxn AS DpValue, MaxCreatedAt, MaxTreatId FROM ( ' +
  { } '   SELECT PersonId, MAX(CreatedAt) AS MaxCreatedAt, MAX(TreatId) AS MaxTreatId, COUNT(*) AS rxn ' +
  { } '   FROM dbo.OngoingTreatment ' +
  { } '   WHERE ATC LIKE %s ' +
  { } '   GROUP BY PersonId ' +
  { } ') rx ' +
  { } 'LEFT JOIN ' +
  { } '  ( ' +
  { } '    SELECT PersonId, COUNT(*) AS n FROM Diagnose.ICD10 ' +
  { } '    WHERE ItemCode LIKE %s AND ProbActive = 1 ' +
  { } '    GROUP BY PersonId ' +
  { } '  ) agg ON agg.PersonId = rx.PersonId ' +
  { } 'WHERE ( agg.n IS NULL )' +
  { } 'ORDER BY PersonId';
begin
  Result := Format( QRY_DRUG_VS_DIAGNOSE, [QuotedStr( AVarName ), QuotedStr( ADrugPattern ), QuotedStr( ADxPattern )] );
end;

function SpDrugHypertensionWithLowBp( const ALowBpThreshold: integer ): string;
const
  QRY_ANTIHYPERTENSIVES = 'EXEC Report.ColAntiHypertensivesLowBP %d';
begin
  Result := Format( QRY_ANTIHYPERTENSIVES, [ALowBpThreshold] );
end;

function SpDrugAndRenalFunction( const ADrugPattern: string; const ALowGfrValueThreshold: integer ): string;
const
  QRY_ATC_GFR = 'EXEC Report.ColDrugAndRenalFunction %s, %d';
begin
  Result := Format( QRY_ATC_GFR, [QuotedStr( ADrugPattern ), ALowGfrValueThreshold] );
end;

function SpDrugsetAntibiotic: string;
const
  VAR_RESISTANCE_DRIVING_ANTIBIOTICS = 'RESISTANCE_DRIVING';
  QRY_DRUGSET_ANTIBIOTICS            =
  { } 'SELECT PersonId, ''' + VAR_RESISTANCE_DRIVING_ANTIBIOTICS + ''' AS VarName, ABS(CHECKSUM(DrugName)) % 100000 AS DpValue, StartAt, TreatId, ai.AtcName AS Caption ' +
  { } SQL_FROM_ONGOING_TREATMENT +
  { } SQL_JOIN_ATC_INDEX +
  { } SQL_WHERE_PERSON_LIST +
  { } 'AND ' +
  { } '( ' +
  { } '  ( ot.ATC' + SQL_COLLATION + 'LIKE ''J01CR%''' + SQL_COLLATION + ') OR ( ot.ATC' + SQL_COLLATION + 'LIKE ''J01D[CDH]%''' + SQL_COLLATION + ') OR ' +
  { } '  ( ot.ATC' + SQL_COLLATION + 'LIKE ''J01FF%''' + SQL_COLLATION + ') OR ( ot.ATC' + SQL_COLLATION + 'LIKE ''J01MA%''' + SQL_COLLATION + ') ' +
  { } ')';
begin
  Result := QRY_DRUGSET_ANTIBIOTICS;
end;

function SpDrugCountNoAtc: string;
const
  QRY_DRUGCOUNT_NOATC =
  { } 'SELECT PersonId, ''NOATC'', COUNT(*) AS DpValue, MAX(StartAt) AS LastDate, MAX(TreatId) AS MaxTreatId ' +
  { } SQL_FROM_ONGOING_TREATMENT +
  { } 'WHERE ISNULL(ATC,'''') = ''''' +
  { } 'GROUP BY PersonId';
begin
  Result := QRY_DRUGCOUNT_NOATC;
end;

{$ENDREGION}

function SpSnapshotVarset( const AVariableDataType: TCrfVarType; const AItemIds: array of integer ): string;
const
  QRY_VARSET =
  { } 'SELECT a.* FROM ( ' +
  { } '  SELECT ce.PersonId, mi.' + FLD_VAR_NAME + ', %s AS DpValue, ce.EventTime AS VarDate, cdp.RowId, mi.ItemId, ' +
  { } '  RANK() OVER (PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC ) AS OrderBy ' +
  { } '  FROM dbo.ClinDataPoint cdp ' +
  { } '  JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId ' +
  { } '  JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId ' +
  { } '  WHERE ( ce.PersonId IN ' + PID_LIST_PLACEHOLDER + ' ) ' +
  { } '    AND ( %s ) ' +
  { } '    AND ( cdp.ItemId IN ( ' + ITEM_LIST_PLACEHOLDER + ' ) )' +
  { } ' ) a ' +
  { } ' WHERE a.OrderBy = 1 ' +
  { } 'ORDER BY PersonId';
var
  valueFragment: string;
  qualifyFragment: string;
begin
  case AVariableDataType of
    itNumeric:
      begin
        { Quantities and Enums are represented by Quantity }
        valueFragment := 'cdp.Quantity';
        qualifyFragment := 'ISNULL(cdp.Quantity,-1) <> -1';
      end;
    itDate:
      begin
        { Dates are represented as Excel dates }
        valueFragment := 'DATEDIFF(DD,''1899-12-30'',cdp.DTVal)';
        qualifyFragment := 'NOT cdp.DTVal IS NULL';
      end;
    itText:
      begin
        { Text is represented as length of text }
        valueFragment := 'DATALENGTH(cdp.TextVal)';
        qualifyFragment := 'NOT cdp.TextVal IS NULL';
      end;
  end;
  Result := Format( QRY_VARSET, [valueFragment, qualifyFragment] );
  Result := StringReplace( Result, ITEM_LIST_PLACEHOLDER, ConvertArrayToList( AItemIds ), [] );
end;

function SpSnapshotEnum( const AItemIds: array of integer ): string;
const
  QRY_VARSET =
  { } 'SELECT a.* FROM ( ' +
  { } '  SELECT ce.PersonId, mi.' + FLD_VAR_NAME + ', cdp.EnumVal AS DpValue, ce.EventTime AS VarDate, cdp.RowId, mi.ItemId, mia.ShortCode AS Caption, ' +
  { } '  RANK() OVER (PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC ) AS OrderBy ' +
  { } '  FROM dbo.ClinDataPoint cdp ' +
  { } '  JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId ' +
  { } '  JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId ' +
  { } '  LEFT JOIN dbo.MetaItemAnswer mia ON mia.ItemId = cdp.ItemId AND mia.OrderNumber = cdp.EnumVal ' +
  { } '  WHERE ( ce.PersonId IN ' + PID_LIST_PLACEHOLDER + ' ) ' +
  { } '    AND ( ISNULL(cdp.EnumVal,-1) >= 0  ) ' +
  { } '    AND ( cdp.ItemId IN ( ' + ITEM_LIST_PLACEHOLDER + ' ) )' +
  { } ' ) a ' +
  { } ' WHERE ( a.OrderBy = 1 )' +
  { } 'ORDER BY PersonId';
begin
  Result := StringReplace( QRY_VARSET, ITEM_LIST_PLACEHOLDER, ConvertArrayToList( AItemIds ), [] );
end;

function SpSnapshotLabset( const ALabClassIds: array of integer ): string;
const
  QRY_LABSET =
  { } 'SELECT agg.* FROM ' +
  { } '( ' +
  { } '  SELECT ld.PersonId, ISNULL(la.NLK, Report.LabClassName(lc.LabClassId)) AS VarName, ld.NumResult, ld.LabDate, ld.ResultId, ' +
  { } '  RANK() OVER ( PARTITION BY ld.PersonId,lc.LabClassId ORDER BY ld.LabDate DESC ) AS OrderBy ' +
  { } '  FROM dbo.LabData ld ' +
  { } '  JOIN dbo.LabCode lc ON lc.LabCodeId = ld.LabCodeId ' +
  { } '  JOIN dbo.LabClass la ON la.LabClassId = lc.LabClassId ' +
  { } '  WHERE ( ld.PersonId IN ' + PID_LIST_PLACEHOLDER + ' ) AND ( la.LabClassId IN (' + LAB_LIST_PLACEHOLDER + ') AND ( ld.NumResult >= 0 ) ) ' +
  { } ' ) agg ' +
  { } ' WHERE agg.OrderBy = 1 ORDER BY agg.PersonId, agg.VarName';
begin
  Result := StringReplace( QRY_LABSET, LAB_LIST_PLACEHOLDER, ConvertArrayToList( ALabClassIds ), [] );
end;

function SpSnapshotLabQuarters( const ALabClassId: integer ): string;
begin
  Result := Format( QRY_LAB_QUARTERS, [ALabClassId] );
end;

function SpSnapshotLabdataByTrustLevel( const ATrustLevel: integer ): string;
const
  QRY_LAB_DATA_TRUST =
  { } 'SELECT a.* FROM ' +
  { } '( ' +
  { } '   SELECT ld.PersonId, ISNULL(la.NLK, Report.LabClassName( la.LabClassId)) AS ' + FLD_VAR_NAME + ', ld.NumResult, ld.LabDate, ld.ResultId, ' +
  { } '   RANK() OVER ( PARTITION BY PersonId ORDER BY LabDate DESC ) AS OrderBy ' +
  { } '   FROM dbo.LabData ld ' +
  { } '     JOIN dbo.LabCode lc ON lc.LabCodeId = ld.LabCodeId ' +
  { } '     JOIN dbo.LabClass la ON la.LabClassId = lc.LabClassId ' +
  { } '   WHERE ( la.TrustLevel = %d )  AND ( ld.PersonId IN ' + PID_LIST_PLACEHOLDER + ' ) ' +
  { } ') a ' +
  { } 'WHERE a.OrderBy = 1';

begin
  Result := Format( QRY_LAB_DATA_TRUST, [ATrustLevel] );
end;

function SpRecentQuantityPresent( const AItemId, AMonthsAgo: integer ): string;
const
  QRY_RECENT_QUANTITY_MONTHS =
  { } 'SELECT ce.PersonId, mi.VarName, cdp.Quantity, ce.EventTime, cdp.RowId ' +
  { } 'FROM dbo.ClinDataPoint cdp ' +
  { } 'JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId ' +
  { } 'JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId ' +
  { } 'WHERE ( cdp.ItemId = %d ) ' +
  { } 'AND ( NOT cdp.Quantity IS NULL ) ' +
  { } 'AND DATEDIFF( MM, ce.EventTime, GETDATE()) < %d ' +
  { } 'ORDER BY ce.EventNum';
begin
  Result := Format( QRY_RECENT_QUANTITY_MONTHS, [AItemId, AMonthsAgo] );
end;

function SpRecentLabdataPresent( const AMonthsAgo: integer ): string;
const
  QRY_RECENT_LABCOUNT =
  { } 'SELECT PersonId,''LABCOUNT%0:dM'' AS VarName, COUNT(*) AS n, MAX(LabDate) AS MaxLabDate, MAX(ResultId) AS MaxResultId ' +
  { } 'FROM LabData ' +
  { } 'WHERE DATEDIFF(MM,LabDate,GETDATE()) < %0:d ' +
  { } 'GROUP BY PersonId';
begin
  Result := Format( QRY_RECENT_LABCOUNT, [AMonthsAgo] );
end;

function SpSnapshotQuantityIfBelowThreshold( const AItemId: integer; const AValue: double ): string;
const
  QRY_LAST_QUANTITY_IF_BELOW_THRESHOLD =
  { } 'SELECT v.PersonId, mi.VarName, v.Quantity AS DpValue, v.EventTime, 0 AS RowId ' +
  { } 'FROM dbo.GetLastQuantityTable( %0:d, NULL ) v ' +
  { } 'JOIN dbo.MetaItem mi ON mi.ItemId = %0:d ' +
  { } 'WHERE v.Quantity < %g';

begin
  Result := Format( QRY_LAST_QUANTITY_IF_BELOW_THRESHOLD, [AItemId, AValue], TFormatSettings.Create( 'en-US' ) );
end;

function SpRecentQuantity( const AItemId: integer; const AMonths: integer ): string;
const
  QRY_RECENT_QUANTITY =
  { } 'SELECT ce.PersonId, %s AS VarName, cdp.Quantity, ce.EventTime, cdp.RowId ' +
  { } 'FROM dbo.ClinDataPoint cdp ' +
  { } 'JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId ' +
  { } 'WHERE ( cdp.ItemId = %d ) ' +
  { } 'AND ( NOT cdp.Quantity IS NULL ) ' +
  { } 'AND DATEDIFF( DD, ce.EventTime, GETDATE()) < %d ' +
  { } 'ORDER BY ce.EventNum';
begin
  Result := Format( QRY_RECENT_QUANTITY, [AItemId, AMonths] );
end;

function SpFormAgeSingle: string;
begin
  Result :=
  { } 'SELECT a.* FROM ( ' +
  { } '  SELECT ce.PersonId, mf.FormName AS ' + FLD_VAR_NAME + ', DATEDIFF(dd,ce.EventTime,GETDATE()) AS DpValue, ce.EventTime AS VarDate, cf.ClinFormId, ' +
  { } '  RANK() OVER (PARTITION BY ce.PersonId,mf.FormName ORDER BY ce.EventTime DESC ) AS OrderBy ' +
  { } '  FROM dbo.ClinForm cf ' +
  { } '  JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId ' +
  { } '  JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId AND mf.FormName =  ' + FORM_NAME_PLACEHOLDER +
  { } '  WHERE ( ce.PersonId IN ' + PID_LIST_PLACEHOLDER + ' ) AND ( cf.DeletedAt IS NULL )' +
  { } ' ) a ' +
  { } ' WHERE a.OrderBy = 1';
end;

function SpFlackerKileyDeath: string;
begin
  Result :=
  { } 'SELECT PersonId, VarName, DataValue, EventTime, RowId, ReverseOrder  ' +
  { } 'FROM ' +
  { } '( ' +
  { } '  SELECT ce.PersonId, cdp.Quantity AS FK_SCORE,' +
  { } '	 CONVERT(DECIMAL(18,4),DATEDIFF(DAY, ce.EventTime, p.DeceasedDate )) AS FK_DAYS_LIVED,' +
  { } '    ce.EventTime, cdp.RowId,' +
  { } '  ROW_NUMBER() OVER (PARTITION BY ce.PersonId ORDER BY ce.EventTime DESC ) AS ReverseOrder ' +
  { } '  FROM dbo.ClinEvent ce ' +
  { } '  JOIN dbo.ClinDataPoint cdp ON cdp.EventId = ce.EventId ' +
  { } '  JOIN dbo.Person p ON p.PersonId = ce.PersonId ' +
  { } '  WHERE cdp.ItemId = 1128 ' +
  { } ') AS SourceTable ' +
  { } 'UNPIVOT ' +
  { } '( DataValue FOR VarName IN ( FK_SCORE, FK_DAYS_LIVED ) ) AS DestTable ' +
  { } 'WHERE ReverseOrder = 1';
end;

function SpSnapshotVarsetAge( const AItemIds: array of integer ): string;
const
  QRY_VARSET_AGE =
  { } 'SELECT a.* FROM ' +
  { } '(' +
  { } '  SELECT ce.PersonId, mi.' + FLD_VAR_NAME + ', DATEDIFF(dd,ce.EventTime,GETDATE()) AS DpValue, ce.EventTime AS VarDate, cdp.RowId, mi.ItemId, ' +
  { } '  RANK() OVER (PARTITION BY ce.PersonId,mi.ItemId ORDER BY ce.EventTime DESC ) AS OrderBy ' +
  { } '  FROM dbo.ClinDataPoint cdp ' +
  { } '  JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId ' +
  { } '  JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId ' +
  { } '  WHERE ( ce.PersonId IN ' + PID_LIST_PLACEHOLDER + ' ) ' +
  { } '    AND NOT ( cdp.Quantity IS NULL AND cdp.DTVal IS NULL AND cdp.TextVal IS NULL ) ' +
  { } '    AND ( cdp.ItemId IN ( ' + ITEM_LIST_PLACEHOLDER + ' ) )' +
  { } ' ) a ' +
  { } ' WHERE a.OrderBy = 1';
begin
  Result := StringReplace( QRY_VARSET_AGE, ITEM_LIST_PLACEHOLDER, ConvertArrayToList( AItemIds ), [] );
end;

function SpMaximumQuantityVarset( const AItemIds: array of integer ): string;
const
  QRY_VARSET_MAX_QUANTITY =
  { } ' SELECT a.* FROM  ' +
  { } '(' +
  { } '  select ce.PersonId, mi.' + FLD_VAR_NAME + ', cdp.Quantity AS DpValue, ce.EventTime AS VarDate, cdp.RowId, cdp.ItemId, ' +
  { } '  RANK() OVER ( PARTITION BY ce.PersonId, cdp.ItemId ORDER BY Quantity DESC, cdp.RowId DESC ) AS rnk ' +
  { } '  FROM dbo.ClinDataPoint cdp ' +
  { } '  JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId ' +
  { } '  JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId ' +
  { } '  WHERE ( ce.PersonId IN ' + PID_LIST_PLACEHOLDER + ' ) ' +
  { } '    AND ( cdp.ItemId IN ( ' + ITEM_LIST_PLACEHOLDER + ' ) )' +
  { } ' ) a ' +
  { } 'where a.rnk = 1';
begin
  Result := StringReplace( QRY_VARSET_MAX_QUANTITY, ITEM_LIST_PLACEHOLDER, ConvertArrayToList( AItemIds ), [] );
end;

{ StudyCase, StudyGroup and StudyCennter }

function SpStudCaseFields( const AVarName, AFieldName: string; const AStudyId: integer ): string;
begin
  Result := Format(
    { } 'SELECT sc.PersonId, %s AS VarName, sc.%s AS DpValue, GETDATE(), sc.StudCaseId AS RowId ' +
    { } 'FROM dbo.StudCase sc ' +
    { } 'WHERE sc.StudyId = %d', [QuotedStr( AVarName ), AFieldName, AStudyId] );
end;

function SpStudyCenter( const AStudyId: integer ): string;
begin
  Result := Format(
    { } 'SELECT sc.PersonId, ''CenterId'' AS VarName, sg.CenterId AS DpValue, GETDATE(), sc.StudCaseId AS RowId ' +
    { } 'FROM dbo.StudCase sc ' +
    { } 'JOIN dbo.StudyGroup sg ON sg.StudyId = sc.StudyId AND sg.GroupId = sc.GroupId ' +
    { } 'WHERE sc.StudyId = %d', [AStudyId] );
end;

function SpStudyGroupDeath( const AStudyId: integer ): string;
begin
  Result := Format(
    { } 'SELECT PersonId, ''DEATH_GROUP'' AS VarName, NewGroupId AS DpValue, DeceasedDate AS VarDate, StudCaseLogId AS RowId  ' +
    { } 'FROM ' +
    { } '( ' +
    { } '  SELECT p.PersonId, p.DeceasedDate, scl.NewGroupId, scl.StudCaseLogId, ' +
    { } '  ROW_NUMBER() OVER (PARTITION BY scl.StudCaseId ORDER BY scl.StudCaseLogId desc ) AS ReverseOrder ' +
    { } '  FROM dbo.Person p ' +
    { } '  JOIN dbo.StudCase sc ON sc.PersonId = p.PersonId AND sc.StudyId = %d ' +
    { } '  JOIN dbo.StudCaseLog scl ON scl.StudCaseId = sc.StudCaseId AND scl.ChangedAt < p.DeceasedDate ' +
    { } '  LEFT JOIN dbo.StudyGroup sg ON sg.StudyId = sc.StudyId AND sg.GroupId = scl.NewGroupId ' +
    { } '  WHERE NOT DeceasedDate IS NULL ' +
    { } ') agg ' +
    { } 'WHERE agg.ReverseOrder = 1', [AStudyId] );
end;

function SpStudyCenterDeath( const AStudyId: integer ): string;
begin
  Result := Format(
    { } 'SELECT PersonId, ''DEATH_CENTER'' AS VarName, CenterId AS DpValue, DeceasedDate AS VarDate, StudCaseLogId AS RowId  ' +
    { } 'FROM ' +
    { } '( ' +
    { } '  SELECT p.PersonId, p.DeceasedDate, sg.CenterId, scl.StudCaseLogId, ' +
    { } '  ROW_NUMBER() OVER (PARTITION BY scl.StudCaseId ORDER BY scl.StudCaseLogId desc ) AS ReverseOrder ' +
    { } '  FROM dbo.Person p ' +
    { } '  JOIN dbo.StudCase sc ON sc.PersonId = p.PersonId AND sc.StudyId = %d ' +
    { } '  JOIN dbo.StudCaseLog scl ON scl.StudCaseId = sc.StudCaseId AND scl.ChangedAt < p.DeceasedDate ' +
    { } '  LEFT JOIN dbo.StudyGroup sg ON sg.StudyId = sc.StudyId AND sg.GroupId = scl.NewGroupId ' +
    { } '  WHERE NOT DeceasedDate IS NULL ' +
    { } ') agg ' +
    { } 'WHERE agg.ReverseOrder = 1', [AStudyId] );
end;

end.
