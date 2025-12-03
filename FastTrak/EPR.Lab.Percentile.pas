unit EPR.Lab.Percentile;

interface

uses
  Emetra.Database.Interfaces,
  System.Classes,
  System.Generics.Collections;

type
  TExactNumber = Currency;

  TPercentileRanker = class( TDictionary<TExactNumber, TExactNumber> )
  public
    fVarName: string;
    fLabCodeId: integer;
    fSQL: ISQL;
    FOnChange: TNotifyEvent;
    procedure LoadById;
    procedure LoadByName;
    procedure SetName( const AVarName: string );
  public
    constructor Create( ASQL: ISQL );
    procedure SetId( const ALabCodeId: integer );
    { Properties }
    property OnChange: TNotifyEvent read FOnChange write FOnChange;
    property VarName: string read fVarName;
  end;

implementation

uses
  Data.DB;

{ TPercentileRanker }

constructor TPercentileRanker.Create( ASQL: ISQL );
begin
  inherited Create;
  fSQL := ASQL;
end;

procedure TPercentileRanker.LoadByName;
var
  fldNumResult: TField;
  fldPercentileRank: TField;
begin
  Clear;
  with fSQL.FastQuery( 'EXEC Report.GetPercentileRanks :VarName', [fVarName] ) do
    try
      fldNumResult := FieldByName( 'NumResult' );
      fldPercentileRank := FieldByName( 'PercentileRank' );
      while not EOF do
      begin
        AddOrSetValue( fldNumResult.AsCurrency, fldPercentileRank.AsCurrency );
        Next;
      end;
    finally
      Close;
    end;
end;

procedure TPercentileRanker.LoadById;
var
  fldNumResult: TField;
  fldPercentileRank: TField;
begin
  Clear;
  with fSQL.FastQuery( 'EXEC Report.GetPercentileRanksById :LabCodeId', [fLabCodeId] ) do
    try
      fldNumResult := FieldByName( 'NumResult' );
      fldPercentileRank := FieldByName( 'PercentileRank' );
      while not EOF do
      begin
        AddOrSetValue( fldNumResult.AsCurrency, fldPercentileRank.AsCurrency );
        Next;
      end;
    finally
      Close;
    end;
end;

procedure TPercentileRanker.SetId( const ALabCodeId: integer );
begin
  if ALabCodeId <> fLabCodeId then
  begin
    fLabCodeId := ALabCodeId;
    LoadById;
    if Assigned( FOnChange ) then
      FOnChange( Self );
  end;
end;

procedure TPercentileRanker.SetName( const AVarName: string );
begin
  if AVarName <> fVarName then
  begin
    fVarName := AVarName;
    LoadByName;
    if Assigned( FOnChange ) then
      FOnChange( Self );
  end;
end;

end.
