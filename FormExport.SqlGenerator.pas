unit FormExport.SqlGenerator;

interface

uses
  System.SysUtils,
  Data.Db,
  Emetra.Database.Interfaces;

type
  TDateInfo = ( diNone, diAgeYears, diAgeMonths, diAgeDays );

  TFormDumper = class( TObject )
  strict private
    fSQL: ISQL;
    fDateInfo: TDateInfo;
    fLinesWritten: integer;
    fFileName: string;
  private
    procedure ExportDataSetToCSV( ADataset: TDataSet; const AFileName: string );
  public
    { Initialization }
    constructor Create( ASQL: ISQL );
    { Other methods }
    function GeneratePivotSQL( const AFormId: integer ): string;
    procedure SaveToCSV( const AFormId: integer );
    { Properties }
    property FileName: string read fFileName;
    property DateInfo: TDateInfo read fDateInfo write fDateInfo;
    property LinesWritten: integer read fLinesWritten;
  end;

implementation

uses
  System.Classes, System.RegularExpressions;

const
  { Placeholders used in the query, to be replaced with generated content }
  PLACEHOLDER_AGE      = '{AgeOnEvent}';
  PLACEHOLDER_VARLIST  = '{VarList}';
  PLACEHOLDER_ITEMLIST = '{ItemList}';

const
  TEMP_TABLE = '#SourceTable';

  CMD_CREATE_TEMP_SOURCE_TABLE =
  { } 'SELECT p.PersonId, p.DOB, p.GenderId, ce.StudyId, ' +
  { } '  ce.StatusId, sg.CenterId, sg.GroupId, sg.GroupName, sge.CenterId AS EventCenterId, ce.EventId, ' +
  { } '  cf.ClinFormId, cf.FormComplete, cf.FormStatus, cf.Comment, ce.EventTime, cdp.ItemId, ' + sLinebreak +
  { } 'COALESCE( FORMAT( cdp.EnumVal, ''D'', ''en-US'' ), FORMAT( cdp.Quantity, ''G6'', ''en-US'' ), FORMAT( cdp.DTVal, ''yyyy-MM-dd'' ), cdp.TextVal ) AS DataValue ' + sLinebreak +
  { } 'INTO  ' + TEMP_TABLE + ' ' +
  { } 'FROM dbo.ClinEvent ce ' + sLinebreak +
  { } '  JOIN dbo.StudCase sc ON sc.StudyId = ce.StudyId AND sc.PersonId = ce.PersonId ' + sLinebreak +
  { } '  JOIN dbo.Person p ON p.PersonId = ce.PersonId ' + sLinebreak +
  { } '  JOIN dbo.ClinForm cf ON cf.EventId = ce.EventId AND cf.FormId = %d AND cf.DeletedBy IS NULL ' + sLinebreak +
  { } '  JOIN dbo.ClinDataPoint cdp ON cdp.EventId = ce.EventId ' + sLinebreak +
  { } '  JOIN dbo.StudyGroup sge ON sge.StudyId = ce.StudyId AND sge.GroupId = ce.GroupId ' + sLinebreak +
  { } '  LEFT JOIN dbo.StudyGroup sg ON sg.StudyId = sc.StudyId AND sg.GroupId = sc.GroupId;';

  SQL_PIVOT_QUERY =

  { } 'SELECT PersonId, GenderId, DATEPART(YY,DOB) AS YOB, ' + PLACEHOLDER_AGE + 'StudyId, StatusId, CenterId, GroupId, GroupName, EventCenterId, EventId, ClinFormId, FormComplete, FormStatus, Comment,  ' + sLinebreak +
  { } ' ROW_NUMBER() OVER ( PARTITION BY PersonId ORDER BY EventTime ) AS EventNo, ' + sLinebreak +
  { } ' CONVERT( DATE, EventTime ) AS EventDate ' + sLinebreak + PLACEHOLDER_VARLIST + sLinebreak +
  { } 'FROM ' + sLinebreak +
  { } '  (  SELECT * FROM ' + TEMP_TABLE + ' ) AS SourceTable ' + sLinebreak +
  { } 'PIVOT ' + sLinebreak +
  { } '  ( ' + sLinebreak +
  { } '      MIN( DataValue ) ' + sLinebreak +
  { } '      FOR ItemId IN ( ' + PLACEHOLDER_ITEMLIST + ' ) ' + sLinebreak +
  { } '  ) AS PivotTable;';

  QRY_ITEMS =
  { } 'SELECT mi.ItemId, mi.VarName, mi.ItemType, ISNULL(mfi.Decimals,0) AS Decimals ' +
  { } 'FROM dbo.MetaFormItem mfi ' +
  { } '  JOIN dbo.MetaItem mi ON mi.ItemId = mfi.ItemId ' +
  { } 'WHERE mfi.FormId = :FormId AND ISNULL(mfi.ReadOnly,0) = 0 ' +
  { } 'ORDER BY mfi.PageNumber, mfi.OrderNumber';

const
  { Various types of age info }
  AGE_YEARS_DECIMAL = 'CONVERT( INT, DATEDIFF( DD, DOB, EventTime ) / 365.25 ) AS AgeOnEventY, ';
  AGE_MONTHS        = 'CONVERT( INT, DATEDIFF( DD, DOB, EventTime ) / ( 365.25 / 12 ) ) AS AgeOnEventM, ';
  AGE_DAYS          = 'DATEDIFF( DD, DOB, EventTime ) AS AgeOnEventD, ';

  { TFormDumper }

constructor TFormDumper.Create( ASQL: ISQL );
begin
  inherited Create;
  fSQL := ASQL;
  fDateInfo := diNone;
end;

function TFormDumper.GeneratePivotSQL( const AFormId: integer ): string;
var
  dateSpec: string;
  varNameList: string;
  itemList: string;
begin
  itemList := EmptyStr;
  varNameList := EmptyStr;
  with fSQL.FastQuery( QRY_ITEMS, [AFormId] ) do
    try
      while not EOF do
      begin
        varNameList := varNameList + '  , ' + Format( '[%d] AS %s', [Fields[0].AsInteger, Fields[1].AsString] ) + sLinebreak;
        itemList := itemList + Format( ', [%d]', [Fields[0].AsInteger] );
        Next;
      end;
    finally
      Close;
    end;
  Result := Format( CMD_CREATE_TEMP_SOURCE_TABLE, [AFormId] ) + SQL_PIVOT_QUERY;
  case fDateInfo of
    diAgeYears: dateSpec := AGE_YEARS_DECIMAL;
    diAgeMonths: dateSpec := AGE_MONTHS;
    diAgeDays: dateSpec := AGE_DAYS;
  else dateSpec := EmptyStr;
  end;
  Result := StringReplace( Result, PLACEHOLDER_AGE, dateSpec, [] );
  Result := StringReplace( Result, PLACEHOLDER_VARLIST, varNameList, [] );
  Result := StringReplace( Result, PLACEHOLDER_ITEMLIST, Copy( itemList, 3, maxint ), [] );
end;

procedure TFormDumper.ExportDataSetToCSV( ADataset: TDataSet; const AFileName: string );
var
  fld: TField;
  lst: TStringList;
  wasActive: Boolean;
  writer: TTextWriter;
begin
  writer := TStreamWriter.Create( AFileName );
  try
    lst := TStringList.Create;
    lst.Delimiter := #9;
    try
      wasActive := ADataset.Active;
      try
        ADataset.Active := true;
        ADataset.GetFieldNames( lst );
        writer.WriteLine( lst.DelimitedText );
        ADataset.First;
        while not ADataset.EOF do
        begin
          lst.Clear;
          for fld in ADataset.Fields do
            lst.Add( TRegEx.Replace( fld.AsString, '\s+', ' ' ) );
          writer.WriteLine( lst.DelimitedText );
          ADataset.Next;
          inc( fLinesWritten );
        end;
      finally
        ADataset.Active := wasActive;
      end;
    finally
      lst.Free;
    end;
  finally
    writer.Free;
  end;
end;

procedure TFormDumper.SaveToCSV( const AFormId: integer );
var
  sql: string;
  ds: TDataSet;
begin
  fLinesWritten := 0;
  fFileName := Format( 'Form-%d.csv', [AFormId] );
  FormatSettings := TFormatSettings.Create( 'en-US' );
  sql := GeneratePivotSQL( AFormId );
  ds := fSQL.FastQuery( sql );
  try
    ExportDataSetToCSV( ds, fFileName );
  finally
    ds.Close;
  end;
end;

end.
