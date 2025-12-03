unit EPR.QA.PointFactory;

interface

uses
  System.Generics.Collections;

type
  // <summary>
  // The datapoint factory will create a datapoint based on the variable name returned in the dataset.
  // For this to work, a class has to be registered for a variable name.
  // </summary>

  TDataPointFactory = class( TObject )
  strict private
    fClassDictionary: TDictionary<string, TClass>;
    fDefaultDatapointClass: TClass;
  private
    function TryGetClass( const AVarName: string; out AClass: TClass ): boolean;
  public
    { Initialization }
    constructor Create( const ADefaultDataPointClass: TClass );
    destructor Destroy; override;
    { Other members }
    function CreateDataPoint( const AVarName: string ): TObject;
    procedure RegisterDataPointClass( const AVarName: string; AClass: TClass );
  end;

implementation

{ TDataPointFactory }

constructor TDataPointFactory.Create( const ADefaultDataPointClass: TClass );
begin
  inherited Create;
  fClassDictionary := TDictionary<string, TClass>.Create;
  fDefaultDatapointClass := ADefaultDataPointClass;
end;

destructor TDataPointFactory.Destroy;
begin
  fClassDictionary.Free;
  inherited;
end;

function TDataPointFactory.CreateDataPoint( const AVarName: string ): TObject;
var
  datapointClass: TClass;
begin
  if TryGetClass( AVarName, datapointClass ) then
    Result := datapointClass.Create
  else
    Result := fDefaultDatapointClass.Create;
end;

procedure TDataPointFactory.RegisterDataPointClass( const AVarName: string; AClass: TClass );
begin
  fClassDictionary.AddOrSetValue( AVarName, AClass )
end;

function TDataPointFactory.TryGetClass( const AVarName: string; out AClass: TClass ): boolean;
begin
  AClass := fDefaultDatapointClass;
  Result := fClassDictionary.TryGetValue( AVarName, AClass );
end;

end.
