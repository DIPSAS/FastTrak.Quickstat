unit EPR.QA.Matrix.Column;

interface

uses
  EPR.QA.CaptionRecord,
  EPR.QA.Matrix.Interfaces,
  EPR.QA.CaptionDictionary,
  {General interfaces}
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces,
  {Standard}
  Generics.Collections, System.Classes, Data.Db;

type
  TPersonGridColumn = class( TInterfacedPersistent, IPersonGridColumn, IVarName )
  strict private
    fVarName: string;
    fTitle: string;
    fSubtitle: string;
  protected
    function Get_Title: string;
    function Get_Subtitle: string;
    function Get_VarName: string;
  public
    { Initialization }
    constructor Create( const AVarName, ATitle: string ); reintroduce;
    { Properties }
    property VarName: string read Get_VarName;
    property Title: string read Get_Title;
    property Subtitle: string read Get_Subtitle;
  end;

  TPersonGridColumnList = class( TObjectList<TPersonGridColumn> )
  private
    fCaptions: TVarCaptions;
  public
    { Initialization }
    constructor Create( ACaptions: TVarCaptions );
    { Other members }
    function TryGetColumn( const AVarName: string; out AGridColumn: TPersonGridColumn ): boolean;
    function ContainsVariable( const AVarName: string ): boolean;
  end;

implementation

uses
  EPR.QA.SQL,
  {Standard}
  System.SysUtils, System.RegularExpressions;

{ TGridColumn }

function TPersonGridColumn.Get_Title: string;
begin
  Result := fTitle;
end;

function TPersonGridColumn.Get_VarName: string;
begin
  Result := fVarName;
end;

function TPersonGridColumn.Get_Subtitle;
begin
  Result := fSubtitle;
end;

constructor TPersonGridColumn.Create( const AVarName, ATitle: string );
begin
  fVarName := AVarName;
  fTitle := ATitle;
end;

{ TColumnList }

constructor TPersonGridColumnList.Create( ACaptions: TVarCaptions );
begin
  inherited Create;
  fCaptions := ACaptions;
end;

function TPersonGridColumnList.ContainsVariable( const AVarName: string ): boolean;
var
  dummyCol: TPersonGridColumn;
begin
  Result := TryGetColumn( AVarName, dummyCol );
end;

function TPersonGridColumnList.TryGetColumn( const AVarName: string; out AGridColumn: TPersonGridColumn ): boolean;
var
  gridColumn: TPersonGridColumn;
begin
  Result := false;
  for gridColumn in Self do
  begin
    if SameText( gridColumn.VarName, AVarName ) then
    begin
      AGridColumn := gridColumn;
      Result := true;
    end;
  end;
end;

end.
