unit EPR.QA.Matrix.Anoymizer;

interface

uses
  System.Generics.Collections;

type
  TMatrixAnonymizer = class
  strict private
    fIdentifierMapping: TDictionary<integer, string>;
    fFileName: string;
    fScaleFactor: integer;
  public
    { Initialization }
    constructor Create( const AFileName: string; const ARowCount: integer ); reintroduce;
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { Other members }
    procedure SaveToFile;
    function NewPersonId( const ACellText: string ): integer;
  end;

implementation

uses
  System.Classes, System.SysUtils;

{ TMatrixAnonymizer }

{$REGION 'Initialization'}

constructor TMatrixAnonymizer.Create( const AFileName: string; const ARowCount: integer );
begin
  inherited Create;
  fFileName := AFileName;
  fScaleFactor := 10;
  while fScaleFactor < ARowCount do
    fScaleFactor := fScaleFactor * 10;
end;

procedure TMatrixAnonymizer.AfterConstruction;
begin
  inherited;
  fIdentifierMapping := TDictionary<integer, string>.Create;
end;

procedure TMatrixAnonymizer.BeforeDestruction;
begin
  fIdentifierMapping.Free;
  inherited;
end;

{$ENDREGION}

function TMatrixAnonymizer.NewPersonId( const ACellText: string ): integer;
begin
  repeat
    Result := fScaleFactor + Random( 9 * fScaleFactor );
  until not fIdentifierMapping.ContainsKey( Result );
  fIdentifierMapping.Add( Result, ACellText );
end;

procedure TMatrixAnonymizer.SaveToFile;
var
  lstFile: TStringList;
  personId: integer;
  originalIdentifier: string;
begin
  lstFile := TStringList.Create;
  try
    for personId in fIdentifierMapping.Keys do
      if fIdentifierMapping.TryGetValue( personId, originalIdentifier ) then
      begin
        lstFile.Values[IntToStr( personId )] := originalIdentifier;
      end;
    lstFile.Sort;
    lstFile.SaveToFile( ChangeFileExt( fFileName, '.mapping.txt' ) );
  finally
    lstFile.Free;
  end;
end;

end.
