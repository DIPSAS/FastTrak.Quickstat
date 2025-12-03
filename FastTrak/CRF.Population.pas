unit CRF.Population;

interface

uses
  CRF.Population.Interfaces,
  {General}
  Emetra.Interfaces.SaveLoad,
  Emetra.Interfaces.Lookup,
  Emetra.Interfaces.ListBox,
  {Standard}
  System.Classes, Data.Db;

type
  TPopulation = class( TInterfacedPersistent, ICodedValue, ILoad, IListBoxBase, IListBoxItem, IPopulation )
  strict private
    fTitle: string;
    fGroup: string;
    fHelpText: string;
    fInfoCaption: string;
    fListBoxText: string;
    fProcId: integer;
    fSourceCode: string;
    fSqlText: string;
  private
    { Property accessors }
    function Get_Group: string;
    function Get_InfoCaption: string;
    function Get_ProcId: integer;
    function Get_QueryText: string;
    function Get_SourceCode: string;
    function Get_Title: string;
  protected
    function V: string;
    function DN: string;
    function OT: string;
    function Description: string;
    function IsCurrent: boolean;
    function Match( const AFilter: string ): boolean;
    function AsListbox( const ASimple: boolean ): string;
  public
    { Initialization }
    constructor CreateAndNext( ADataset: TDataset );
    constructor Create( const AGroup, ACaption: string );
    { Other members }
    procedure Load( ADataset: TDataset );
    { Properties }
    property Group: string read Get_Group;
    property InfoCaption: string read Get_InfoCaption;
    property ProcId: integer read Get_ProcId;
    property QueryText: string read Get_QueryText;
    property SourceCode: string read Get_SourceCode;
    property Title: string read Get_Title;
  end;

implementation

uses
  System.StrUtils,
  System.SysUtils;

{$REGION 'Initialization'}

constructor TPopulation.Create( const AGroup, ACaption: string );
begin
  inherited Create;
  fGroup := AGroup;
  fTitle := ACaption;
end;

constructor TPopulation.CreateAndNext( ADataset: TDataset );
begin
  inherited Create;
  Load( ADataset );
  ADataset.Next;
end;

{$ENDREGION}
{$REGION 'ILoad'}

procedure TPopulation.Load( ADataset: TDataset );
begin
  Assert( Assigned( ADataset ), Format( '%s.Load: Dataset is unassigned', [ClassName] ) );
  with ADataset do
  begin
    fProcId := FieldByName( FLD_PROC_ID ).AsInteger;
    fGroup := FieldByName( FLD_PROC_GROUP ).AsString;
    fTitle := FieldByName( FLD_PROC_TITLE ).AsString;
    fSqlText := FieldByName( FLD_SQL_TEXT ).AsString;
    fInfoCaption := FieldByName( FLD_INFO_CAPTION ).AsString;
    fHelpText := FieldByName( FLD_HELP_TEXT ).AsString;
    fSourceCode := FieldByName( FLD_SOURCE_CODE ).AsString;
  end;
  fListBoxText := V + #9 + DN + #9 + Description + #9 + OT;
end;

{$ENDREGION}
{$REGION 'IPopulation interface'}

function TPopulation.Get_Group: string;
begin
  Result := fGroup;
end;

function TPopulation.Get_InfoCaption: string;
begin
  Result := fInfoCaption;
end;

function TPopulation.Get_ProcId: integer;
begin
  Result := fProcId;
end;

function TPopulation.Get_QueryText: string;
begin
  Result := fSqlText;
end;

function TPopulation.Get_SourceCode: string;
begin
  Result := fSourceCode;
end;

function TPopulation.Get_Title: string;
begin
  Result := fTitle;
end;

{$ENDREGION}
{$REGION 'IListboxInterface'}

function TPopulation.AsListbox( const ASimple: boolean ): string;
begin
  if ASimple then
    Result := V + #9 + DN + #9 + #9 + OT
  else
    Result := fListBoxText;
end;

function TPopulation.Description: string;
begin
  Result := fHelpText;
end;

function TPopulation.DN: string;
begin
  Result := fTitle;
end;

function TPopulation.IsCurrent: boolean;
begin
  Result := true;
end;

function TPopulation.Match( const AFilter: string ): boolean;
begin
  Result := Pos( AnsiUppercase( AFilter ), AnsiUppercase( fListBoxText ) ) > 0;
end;

function TPopulation.OT: string;
begin
  Result := fGroup;
end;

function TPopulation.V: string;
begin
  Result := IntToStr( fProcId );
end;

{$ENDREGION}

end.
