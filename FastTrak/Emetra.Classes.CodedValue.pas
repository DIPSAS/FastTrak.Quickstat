unit Emetra.Classes.CodedValue;

interface

uses
  CRF.Meta.NomItem,
  System.JSON, System.SysUtils;

type
  { TCodedValue }

  TCodedValue = record
  public
    DN: string;
    OT: string;
    S: string;
    V: string;
    { Methods }
    procedure Clear;
    function Empty: boolean;
    class function Parse( Str: string ): TCodedValue; static;
    function ToJSON: string;
    function ToString: string;
    constructor Create( ANomItem: TNomItem );
  end;

  { ICodedValue }

  ICodedValue = interface
    ['{05FD8C97-2708-4BAE-A60A-15239E787728}']
    { Property Accessors }
    function Get_CodedValue: TCodedValue;
    procedure Set_CodedValue( const Value: TCodedValue );
    { Properties }
    property CodedValue: TCodedValue read Get_CodedValue write Set_CodedValue;
  end;

implementation

{ TCodedValue }

procedure TCodedValue.Clear;
begin
  DN := '';
  OT := '';
  S := '';
  V := '';
end;

constructor TCodedValue.Create( ANomItem: TNomItem );
begin
  DN := ANomItem.DN;
  V := ANomItem.V;
  OT := ANomItem.OT;
end;

function TCodedValue.Empty: boolean;
begin
  Result := ( DN + V + OT + S ).Trim = EmptyStr;
end;

class function TCodedValue.Parse( Str: string ): TCodedValue;
var
  JSON: TJSONObject;
begin
  Result.Clear;
  JSON := TJSONObject.Create;
  try
    if JSON.Parse( BytesOf( Str ), 0 ) <> -1 then
    begin
      JSON.TryGetValue( 'DN', Result.DN );
      JSON.TryGetValue( 'V', Result.V );
      JSON.TryGetValue( 'OT', Result.OT );
      JSON.TryGetValue( 'S', Result.S );
    end;
  finally
    JSON.Free;
  end;
end;

function TCodedValue.ToJSON: string;
var
  JSON: TJSONObject;
begin
  JSON := TJSONObject.Create;
  try
    JSON.AddPair( 'DN', DN );
    JSON.AddPair( 'OT', OT );
    JSON.AddPair( 'S', S );
    JSON.AddPair( 'V', V );
    Result := JSON.ToJSON;
  finally
    JSON.Free;
  end;
end;

function TCodedValue.ToString: string;
begin
  if Empty then Result := EmptyStr else Result := ToJSON;
end;

end.
