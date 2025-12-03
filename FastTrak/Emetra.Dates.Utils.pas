{$D-}
unit Emetra.Dates.Utils;

interface

uses DateUtils, SysUtils;

function ApproxTime( const ATime: TDateTime ): string;
function GetDateText( const ADate: TDateTime; const AIncludeDayName: boolean ): string; { Formats dates into reader-friendly }
function SameWeek( const ADate1, ADate2: TDateTime ): boolean;
function SameMonth( const ADate1, ADate2: TDateTime ): boolean;
function SameHour( const ADate1, ADate2: TDateTime ): boolean;
function SameMinute( const ADate1, ADate2: TDateTime ): boolean;
function GetDate( DateStr: string ): TDateTime;
function ISOTime( value: TDateTime; fullsize: boolean = true ): string; overload;
function ISOTime( s: string ): TDateTime; overload;

implementation

resourcestring
  IN_D_YEARS = 'om %d år';
  IN_D_MONTHS = 'om %d mnd';
  IN_D_WEEKS = 'om %d uker';
  IN_D_DAYS = 'om %d dager';
  D_YEARS_AGO = 'for %d år siden';
  D_MONTHS_AGO = 'for %d mnd siden';
  D_WEEKS_AGO = 'for %d uker siden';
  D_DAYS_AGO = 'for %d dager siden';
  DAY_TODAY = 'i dag';
  DAY_TOMORROW = 'i morgen';
  DAY_YESTERDAY = 'i går';

function ApproxTime( const ATime: TDateTime ): string;
begin
  if ( Now - ATime ) < 365 then
    Result := DateToStr( ATime )
  else if ( Now - ATime < 3 * 365 ) then
    Result := FormatDateTime( 'mmmm yyyy', ATime )
  else
    Result := FormatDateTime( 'yyyy', ATime );
end;

function GetDateText( const ADate: TDateTime; const AIncludeDayName: boolean ): string;
var
  iDays: integer;
begin
  iDays := trunc( ADate ) - trunc( Now );
  case iDays of
    0:
      Result := DAY_TODAY;
    1:
      Result := DAY_TOMORROW;
    -1:
      Result := DAY_YESTERDAY;
  else
    if iDays > 730 then
      Result := Format( IN_D_YEARS, [ round( iDays / 365 ) ] )
    else if iDays > 90 then
      Result := Format( IN_D_MONTHS, [ round( iDays / 30 ) ] )
    else if iDays > 30 then
      Result := Format( IN_D_WEEKS, [ round( iDays / 7 ) ] )
    else if iDays > 0 then
      Result := Format( IN_D_DAYS, [ iDays ] )
    else if iDays > -30 then
      Result := Format( D_DAYS_AGO, [ -iDays ] )
    else if iDays > -90 then
      Result := Format( D_WEEKS_AGO, [ round( -iDays / 7 ) ] )
    else if iDays > -730 then
      Result := Format( D_MONTHS_AGO, [ round( -iDays / 30 ) ] )
    else
      Result := Format( D_YEARS_AGO, [ round( -iDays / 365 ) ] );
  end;
  if AIncludeDayName then
    Result := Result + ' (' + FormatDateTime( 'dddd', ADate ) + ')';
end;

function SameWeek( const ADate1, ADate2: TDateTime ): boolean;
begin
  { Trunc doesn't work correctly }
  Result := ( YearOf( ADate1 ) = YearOf( ADate2 ) ) and ( WeekOf( ADate1 ) = WeekOf( ADate2 ) );
end;

function SameMonth( const ADate1, ADate2: TDateTime ): boolean;
begin
  { Trunc doesn't work correctly }
  Result := ( YearOf( ADate1 ) = YearOf( ADate2 ) ) and ( MonthOf( ADate1 ) = MonthOf( ADate2 ) );
end;

function SameHour( const ADate1, ADate2: TDateTime ): boolean;
begin
  { Trunc doesn't work correctly }
  Result := ( YearOf( ADate1 ) = YearOf( ADate2 ) ) and ( MonthOf( ADate1 ) = MonthOf( ADate2 ) ) and
    ( DayOf( ADate1 ) = DayOf( ADate2 ) ) and ( HourOf( ADate1 ) = HourOf( ADate2 ) );
end;

function SameMinute( const ADate1, ADate2: TDateTime ): boolean;
begin
  { Trunc doesn't work correctly }
  Result := ( YearOf( ADate1 ) = YearOf( ADate2 ) ) and ( MonthOf( ADate1 ) = MonthOf( ADate2 ) ) and
    ( DayOf( ADate1 ) = DayOf( ADate2 ) ) and ( HourOf( ADate1 ) = HourOf( ADate2 ) ) and
    ( MinuteOf( ADate1 ) = MinuteOf( ADate2 ) );
end;

function GetDate( DateStr: string ): TDateTime;
var
  iYear: integer;
begin
  if Length( DateStr ) = 0 then
  begin
    Result := 0;
    exit;
  end;
  try
    Result := StrToDate( DateStr );
  except
    on Exception do
      with FormatSettings do
      begin
        Result := 0;
        case Length( Trim( DateStr ) ) of
          4:
            DateStr := DateStr + IntToStr( YearOf( Now ) );
          6, 7:
            if Pos( 'y', Lowercase( ShortDateFormat ) ) > Pos( 'd', Lowercase( ShortDateFormat ) ) then
            begin
              iYear := StrToIntDef( Copy( DateStr, 5, 2 ), 0 );
              if iYear < ( YearOf( Now ) mod 100 ) then
{$IFNDEF DotNet}System.{$ENDIF}Insert( '/20', DateStr, 5 )
              else
{$IFNDEF DotNet}System.{$ENDIF}Insert( '/19', DateStr, 5 );
{$IFNDEF DotNet}System.{$ENDIF}Insert( '/', DateStr, 3 );
            end;
        end;
        DateStr := StringReplace( DateStr, '-', DateSeparator, [ rfReplaceAll ] );
        DateStr := StringReplace( DateStr, '/', DateSeparator, [ rfReplaceAll ] );
        if DateStr = EmptyStr then
          exit;
        try
          Result := StrToDate( DateStr )
        except
          on E: Exception do
            try
              DateStr := StringReplace( DateStr, '/', DateSeparator, [ rfReplaceAll ] );
              Result := StrToDate( DateStr );
            except
              on Exception do
            end;
        end;
        if ( Result = 0 ) and ( Pos( DateSeparator, DateStr ) = 0 ) then
          try
{$IFNDEF DotNet}System.{$ENDIF}Insert( DateSeparator, DateStr, 5 );
{$IFNDEF DotNet}System.{$ENDIF}Insert( DateSeparator, DateStr, 3 );
            Result := StrToDate( DateStr );
          except
            on Exception do
          end;
      end;
  end;
  if Result > Now + 365 then
    Result := EncodeDate( YearOf( Result ) - 100, MonthOf( Result ), DayOf( Result ) );
end;

function ISOTime( value: TDateTime; fullsize: boolean = true ): string;
var
  Year, Month, Day, Hour, Min, Sec, MSec: word;
begin
  Result := '2000-00-00 00:00:00';
  DecodeDate( value, Year, Month, Day );
  DecodeTime( value, Hour, Min, Sec, MSec );
  if ( Hour + Min + Sec <> 0 ) and fullsize then
    Result := Format( '%.4d-%.2d-%.2d %.2d:%.2d:%.2d', [ Year, Month, Day, Hour, Min, Sec ] )
  else
    Result := Format( '%.4d-%.2d-%.2d', [ Year, Month, Day ] );
end;

function ISOTime( s: string ): TDateTime;
const
  NOT_VALID = '"%s" (length=%d) is not a valid ISO time string';
var
  yr: string;
  mo, da, ho, mi, se: string;
begin
  s := Trim( s );
  if s = EmptyStr then
    raise EConvertError.CreateFmt( NOT_VALID, [ s, Length( s ) ] );
  yr := Copy( s, 1, 4 );
  Delete( s, 1, 5 );
  mo := Copy( s, 1, 2 );
  Delete( s, 1, 3 );
  da := Copy( s, 1, 2 );
  Delete( s, 1, 3 );
  case Length( s ) of
    8:
      begin
        ho := Copy( s, 1, 2 );
        Delete( s, 1, 3 );
        mi := Copy( s, 1, 2 );
        Delete( s, 1, 3 );
        se := Copy( s, 1, 2 );
        Delete( s, 1, 3 );
      end;
    0:
      begin
        ho := '00';
        mi := '00';
        se := '00';
      end;
  else
    raise EConvertError.CreateFmt( NOT_VALID, [ s, Length( s ) ] );
  end;
  Result := EncodeDate( StrToInt( yr ), StrToInt( mo ), StrToInt( da ) );
  Result := Result + EncodeTime( StrToInt( ho ), StrToInt( mi ), StrToInt( se ), 0 );
end;

end.
