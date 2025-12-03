unit EPR.QA.Collector.Demographics;

interface

uses
  EPR.QA.Collector.Base;

type
  TDemographicsCollector = class( TDataCollector )
  protected
    function BuildSqlStatement( const AVarName, AVarSpec: string ): string;
  public
    procedure AfterConstruction; override;
  end;

  TAgeCollector = class( TDemographicsCollector )
  public
    procedure AfterConstruction; override;
  end;

  TGenderCollector = class( TDemographicsCollector )
  public
    procedure AfterConstruction; override;
  end;

  TYOBCollector = class( TDemographicsCollector )
  public
    procedure AfterConstruction; override;
  end;

  TYODCollector = class( TDemographicsCollector )
  public
    procedure AfterConstruction; override;
  end;

  TMOBCollector = class( TDemographicsCollector )
  public
    procedure AfterConstruction; override;
  end;

  TPostCodeCollector = class( TDemographicsCollector )
  public
    procedure AfterConstruction; override;
  end;

{ Collectors that collect across all patients at the same time }

  TGlobalCollector = class( TDataCollector )
  public
    procedure AfterConstruction; override;
  end;

  TStatusCollector = class( TGlobalCollector )
  public
    function SQL: string; override;
  end;

  TGroupCollector = class( TGlobalCollector )
  public
    function SQL: string; override;
  end;

  TCenterCollector = class( TGlobalCollector )
  public
    function SQL: string; override;
  end;

  TGroupAtDeathCollector = class( TGlobalCollector )
  public
    function SQL: string; override;
  end;

  TCenterAtDeathCollector = class( TGlobalCollector )
  public
    function SQL: string; override;
  end;

implementation

uses
  EPR.QA.Collector.Names,
  EPR.QA.SQL,
  {Standard}
  System.SysUtils;

procedure TDemographicsCollector.AfterConstruction;
begin
  inherited;
  FVarPrefix := EmptyStr;
  FMaxBatchSize := 100;
end;

function TDemographicsCollector.BuildSqlStatement( const AVarName, AVarSpec: string ): string;
begin
  Result := Format( QRY_DEMOGRAPHICS, [AVarName, AVarSpec] );
end;

procedure TAgeCollector.AfterConstruction;
begin
  inherited;
  FSQL := BuildSqlStatement( VAR_AGE, 'DATEDIFF(YYYY,DOB,GETDATE())' );
end;

procedure TYOBCollector.AfterConstruction;
begin
  inherited;
  FSQL := BuildSqlStatement( VAR_YOB, 'DATEPART(YYYY,DOB)' );
end;

procedure TYODCollector.AfterConstruction;
begin
  inherited;
  FSQL := BuildSqlStatement( VAR_YOD, 'DATEPART(YYYY,DeceasedDate)' );
end;

procedure TMOBCollector.AfterConstruction;
begin
  inherited;
  FSQL := BuildSqlStatement( VAR_MOB, 'DATEPART(MM,DOB)' );
end;

procedure TGenderCollector.AfterConstruction;
begin
  inherited;
  FSQL := BuildSqlStatement( VAR_SEX, 'GenderId' );
end;

procedure TPostCodeCollector.AfterConstruction;
begin
  inherited;
  FSQL := BuildSqlStatement( 'ZIP', 'CONVERT(INTEGER,PostalCode)' );
end;

function TStatusCollector.SQL: string;
begin
  Result := SpStudCaseFields( 'StatusId', 'FinState', fStudyId );
end;

function TGroupCollector.SQL: string;
begin
  Result := SpStudCaseFields( 'GroupId', 'GroupId', fStudyId );
end;

function TCenterCollector.SQL: string;
begin
  Result := SpStudyCenter( fStudyId );
end;

{ TGroupAtDeath }

function TGroupAtDeathCollector.SQL: string;
begin
  Result := SpStudyGroupDeath( fStudyId );
end;

function TCenterAtDeathCollector.SQL: string;
begin
  Result := SpStudyCenterDeath( fStudyId );
end;


{ TGlobalCollector }

procedure TGlobalCollector.AfterConstruction;
begin
  inherited;
  fMaxBatchSize := maxint;
end;

end.
