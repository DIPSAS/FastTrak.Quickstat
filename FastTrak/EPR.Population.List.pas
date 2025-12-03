unit EPR.Population.List;

interface

uses
  CRF.Population,
  CRF.Population.Interfaces,
  {General classes}
  Emetra.Classes.Business,
  {General interfaces}
  Emetra.Logging.Interfaces,
  Emetra.Database.Interfaces,
  Emetra.Interfaces.List,
  {Standard}
  Data.Db, Generics.Collections, System.Classes;

type
  TPopulationList = class( TBusiness, IObjectList )
  strict private
    fSQL: ISQL;
    fPopulationList: TObjectList<TPopulation>;
  private
    function Get_Count: integer;
    function GetItem( AIndex: integer ): TObject;
  public
    constructor Create( ASQL: ISQL; ALog: ILog );
    { Initialization }
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { Other methods }
    function Add( const APopulationData: TPopulation ): TPopulation;
    function TryGetPopulation( const AProcId: integer; out APopulation: TPopulation ): boolean;
    procedure Load( const AStudyId, ADbVersion: integer; const AShowMostCommon: boolean );
    procedure Clear;
    { Properties }
    property Count: integer read Get_Count;
    property List: TObjectList<TPopulation> read fPopulationList;
  end;

implementation

uses
  CRF.SQL,
  System.SysUtils;

function TPopulationList.Add( const APopulationData: TPopulation ): TPopulation;
begin
  BeginUpdate;
  try
    fPopulationList.Add( APopulationData );
    Result := APopulationData;
  finally
    EndUpdate;
  end;
end;

procedure TPopulationList.AfterConstruction;
begin
  inherited;
  fPopulationList := TObjectList<TPopulation>.Create;
end;

procedure TPopulationList.BeforeDestruction;
begin
  fPopulationList.Free;
  inherited;
end;

procedure TPopulationList.Clear;
begin
  BeginUpdate;
  try
    fPopulationList.Clear;
  finally
    EndUpdate;
  end;
end;

constructor TPopulationList.Create( ASQL: ISQL; ALog: ILog );
begin
  inherited Create( ALog );
  fSQL := ASQL;
end;

function TPopulationList.GetItem( AIndex: integer ): TObject;
begin
  Result := fPopulationList[AIndex];
end;

function TPopulationList.Get_Count: integer;
begin
  Result := fPopulationList.Count;
end;

procedure TPopulationList.Load( const AStudyId, ADbVersion: integer; const AShowMostCommon: boolean );
var
  dsPopulations: TDataset;
begin
  BeginUpdate;
  try
    fPopulationList.Clear;
    if AStudyId > 0 then
    begin
      if AShowMostCommon then
        dsPopulations := fSQL.FastQuery( QRY_POPULAR_POPULATIONS, [AStudyId, ADbVersion] )
      else if ADbVersion >= 18200 then
        dsPopulations := fSQL.FastQuery( QRY_STUDY_POPULATIONS_WITH_VERSION, [AStudyId, ADbVersion] )
      else
        dsPopulations := fSQL.FastQuery( QRY_STUDY_POPULATIONS_NO_VERSION, [AStudyId] );
      with dsPopulations do
        try
          while not EOF do
            fPopulationList.Add( TPopulation.CreateAndNext( dsPopulations ) );
        finally
          Close;
        end;
    end;
  finally
    EndUpdate;
  end;
end;

function TPopulationList.TryGetPopulation( const AProcId: integer; out APopulation: TPopulation ): boolean;
var
  n: integer;
begin
  APopulation := nil;
  Result := false;
  n := 0;
  while n < fPopulationList.Count do
  begin
    APopulation := fPopulationList[n];
    Result := ( APopulation.ProcId = AProcId );
    if Result then
      break
    else
      inc( n );
  end;
end;

end.
