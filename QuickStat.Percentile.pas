unit QuickStat.Percentile;

interface

uses
  EPR.Lab.Percentile,
  {General}
  Emetra.VclUtil.ColorCalculator,
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces,
  {Standard}
  System.Generics.Collections, System.Classes, Vcl.Graphics;

type
  TColorStrategy = ( csLowIsBadHighIsGood, csHighIsBadLowIsGood, csHighAndLowIsBad, csHighIsBadOnly, csLowIsBadOnly );

  TColoring = class( TInterfacedPersistent )
  protected
    fPercentileRank: TExactNumber;
  public
    function GetColor( const ANumResult: TExactNumber ): TColor; virtual;
    procedure Load; dynamic;
    { Properties }
    property PercentileRank: TExactNumber read fPercentileRank;
  end;

  TPercentileColoring = class( TColoring )
  strict private
    fSQL: ISQL;
    fLabClassId: integer;
    fLowColor: TColor;
    fHighColor: TColor;
    fRanker: TPercentileRanker;
    fHighBracket: double;
    fLowBracket: double;
  public
    { Initialization }
    constructor Create( const ALabClassId: integer; const AColorScheme: TColorStrategy; const ASQL: ISQL ); reintroduce;
    destructor Destroy; override;
    { Other members }
    procedure Load; override;
    function GetColor( const ANumResult: TExactNumber ): TColor; override;
    { Properties }
    property HighBracket: double read fHighBracket;
    property LabClassId: integer read fLabClassId;
    property LowBracket: double read fLowBracket;
  end;

  TColorDictionary = class( TInterfacedPersistent, ILoginObserver )
  strict private
    fNames: TDictionary<string, integer>;
    fDictionary: TObjectDictionary<integer, TColoring>;
    fLog: ILog;
    fSQL: ISQL;
  private
    function FriendlyName: string;
  public
    { Initialization }
    constructor Create( const ASQL: ISQL; const ALog: ILog ); reintroduce;
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { ILoginObserver }
    procedure AfterLogin( AConn: IDatabaseConnection );
    function TryGetValue( const ALabName: string; out AColoring: TColoring ): boolean;
    procedure Add( const ALabClassId: integer; const AStrategy: TColorStrategy );
  end;

implementation

uses
  {Standard}
  System.SysUtils, Data.Db;

resourcestring
  TXT_COLOR_CATALOG = 'Fargekatalog';

const
  QRY_LAB_VARNAME     = 'EXEC Report.GetLabClassVarNames';
  QRY_LAB_PERCENTILES = 'EXEC Report.GetPercentileRanksByClassId :LabClassId';
  FLD_NUM_RESULT      = 'NumResult';
  FLD_PERCENTILE_RANK = 'PercentileRank';

constructor TPercentileColoring.Create( const ALabClassId: integer; const AColorScheme: TColorStrategy; const ASQL: ISQL );
begin
  inherited Create;
  fLowBracket := 10;
  fHighBracket := 10;
  fSQL := ASQL;
  fRanker := TPercentileRanker.Create( fSQL );
  case AColorScheme of
    csLowIsBadHighIsGood:
      begin
        fLowColor := clRed;
        fHighColor := clLime;
      end;
    csHighIsBadLowIsGood:
      begin
        fLowColor := clLime;
        fHighColor := clRed;
      end;
    csHighAndLowIsBad:
      begin
        fLowColor := clRed;
        fHighColor := clRed;
      end;
    csHighIsBadOnly:
      begin
        fLowColor := clWhite;
        fHighColor := clRed;
      end;
    csLowIsBadOnly:
      begin
        fLowColor := clRed;
        fHighColor := clWhite;
      end;
  end;
end;

destructor TPercentileColoring.Destroy;
begin
  FreeAndNil( fRanker );
  inherited;
end;

function TPercentileColoring.GetColor( const ANumResult: TExactNumber ): TColor;
var
  percentColor: integer;
begin
  if fRanker.TryGetValue( ANumResult, fPercentileRank ) then
  begin
    if fPercentileRank > ( 100 - fHighBracket ) then
    begin
      percentColor := round( ( fPercentileRank - fHighBracket ) * fHighBracket );
      Result := TColorCalculator.BlendColors( clWhite, fHighColor, percentColor );
    end
    else if fPercentileRank > 90 then
    begin
      percentColor := round( ( fPercentileRank - 90 ) * 10.0 );
      Result := TColorCalculator.BlendColors( clYellow, clWebOrange, percentColor );
    end
    else if fPercentileRank > 80 then
    begin
      percentColor := round( ( fPercentileRank - 80 ) * 10.0 );
      Result := TColorCalculator.BlendColors( clWhite, clYellow, percentColor );
    end
    else if fPercentileRank < fLowBracket then
    begin
      percentColor := round( fPercentileRank * fLowBracket );
      Result := TColorCalculator.BlendColors( fLowColor, clWhite, percentColor );
    end
    else
      Result := clWhite;
    Result := TColorCalculator.BlendColors( Result, clWhite, 50 );
  end
  else
    Result := clFuchsia;
end;

procedure TPercentileColoring.Load;
var
  fldNumResult: TField;
  fldPercentileRank: TField;
begin
  fRanker.Clear;
  with fSQL.FastQuery( QRY_LAB_PERCENTILES, [fLabClassId] ) do
    try
      fldNumResult := FieldByName( FLD_NUM_RESULT );
      fldPercentileRank := FieldByName( FLD_PERCENTILE_RANK );
      while not EOF do
      begin
        fRanker.AddOrSetValue( fldNumResult.AsCurrency, fldPercentileRank.AsCurrency );
        Next;
      end;
    finally
      Close;
    end;
end;

{ TColoring }

function TColoring.GetColor( const ANumResult: TExactNumber ): TColor;
begin
  Result := clWhite;
end;

procedure TColoring.Load;
begin
  { Does nothing, overridden }
end;

{ TColorDictionary }

constructor TColorDictionary.Create( const ASQL: ISQL; const ALog: ILog );
begin
  inherited Create;
  fSQL := ASQL;
  fLog := ALog;
end;

function TColorDictionary.FriendlyName: string;
begin
  Result := TXT_COLOR_CATALOG;
end;

procedure TColorDictionary.AfterConstruction;
begin
  inherited;
  fDictionary := TObjectDictionary<integer, TColoring>.Create( [doOwnsValues] );
  fNames := TDictionary<string, integer>.Create;
end;

procedure TColorDictionary.BeforeDestruction;
begin
  fNames.Free;
  fDictionary.Free;
  inherited;
end;

procedure TColorDictionary.Add( const ALabClassId: integer; const AStrategy: TColorStrategy );
begin
  fDictionary.Add( ALabClassId, TPercentileColoring.Create( ALabClassId, AStrategy, fSQL ) );
end;

procedure TColorDictionary.AfterLogin( AConn: IDatabaseConnection );
const
  PROC_NAME = 'AfterLogin';
var
  sql: ISQL;
  item: TColoring;
begin
  fLog.EnterMethod( Self, PROC_NAME );
  try
    if Supports( AConn, ISQL, sql ) then
      with sql.FastQuery( QRY_LAB_VARNAME ) do
        try
          while not EOF do
          begin
            fNames.AddOrSetValue( Fields[1].AsString, Fields[0].AsInteger );
            Next;
          end;
        finally
          Close;
        end;
    for item in fDictionary.Values do
      try
        item.Load;
      except
        on E: Exception do
          fLog.SilentWarning( '%s.%s: %s', [ClassName, PROC_NAME, E.Message] );
      end;
  finally
    fLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

function TColorDictionary.TryGetValue( const ALabName: string; out AColoring: TColoring ): boolean;
var
  foundLabCodeId: integer;
begin
  AColoring := nil;
  if fNames.TryGetValue( ALabName, foundLabCodeId ) then
    Result := fDictionary.TryGetValue( foundLabCodeId, AColoring )
  else
    Result := false;
end;

end.
