unit EPR.QA.Collection;

interface

uses
  EPR.QA.Collector.Factory,
  EPR.QA.PointFactory,
  EPR.QA.Collector.Base,
  EPR.QA.Matrix.Interfaces,
  {General}
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces,
  {Standard}
  Generics.Collections;

type
  TQACollection = class( TObject )
  private
    fCollectors: TDataCollectorList;
    fPopulationId: integer;
    fStudyName: string;
    fFriendlyName: string;
    fCaption: string;
    fImageIndex: TQaImageType;
  protected
    fCollectorFactory: TCollectorFactory;
    fSQL: ISQL;
    FLog: ILog;
    function Get_Caption: string;
    function TryAdd( const ACollector: TDataCollector ): boolean;
  public
    { Initialization }
    constructor Create( const AFactory: TDataPointFactory; const AStudyName: string; const APopulationId: integer; const ASQL: ISQL; const ALog: ILog ); reintroduce;
    destructor Destroy; override;
    { Other members }
    procedure AddData( const ADataTarget: IDataTarget );
    procedure AddCollector( const ACollectorName: string );
    { Properties }
    property Caption: string read Get_Caption write fCaption;
    property ImageIndex: TQaImageType read fImageIndex write fImageIndex;
    property PopulationId: integer read fPopulationId write fPopulationId;
    property StudyName: string read fStudyName;
    property FriendlyName: string read fFriendlyName write fFriendlyName;
  end;

  TQACollectionList = class( TObjectList<TQACollection> )
  end;

implementation

{ TQACollection }

{$REGION 'Initialization'}

constructor TQACollection.Create( const AFactory: TDataPointFactory; const AStudyName: string; const APopulationId: integer; const ASQL: ISQL; const ALog: ILog );
begin
  inherited Create;
  fCaption := ClassName;
  fStudyName := AStudyName;
  fPopulationId := APopulationId;
  fSQL := ASQL;
  FLog := ALog;
  fCollectors := TDataCollectorList.Create( true );
  fCollectorFactory := TCollectorFactory.Create( AFactory, ASQL, ALog );
end;

destructor TQACollection.Destroy;
begin
  fCollectorFactory.Free;
  fCollectors.Free;
  inherited;
end;

function TQACollection.Get_Caption: string;
begin
  Result := fCaption;
end;

{$ENDREGION}

function TQACollection.TryAdd( const ACollector: TDataCollector ): boolean;
begin
  Result := Assigned( ACollector );
  if Result then
    fCollectors.Add( ACollector );
end;

procedure TQACollection.AddCollector( const ACollectorName: string );
begin
  if not TryAdd( fCollectorFactory.CreateCollector( ACollectorName ) ) then
    FLog.Event( '%s.AddCollector("%s"): Not added.', [ClassName, ACollectorName], ltWarning );
end;

procedure TQACollection.AddData( const ADataTarget: IDataTarget );
var
  n: integer;
begin
  n := 0;
  while n < fCollectors.Count do
  begin
    ADataTarget.AddData( fCollectors[n] );
    inc( n );
  end;
end;

end.
