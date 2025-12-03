unit QuickStat.Selection;

interface

uses
  EPR.QA.SQL,
  {General}
  Emetra.Interfaces.ListBox,
  Emetra.Database.Interfaces,
  {Standard}
  Classes, Db;

type
  TPackagedSelection = class( TInterfacedPersistent, IListBoxBase )
  private
    fCaption: string;
    fCollectorNames: TStringList;
    fComment: string;
    fPopulationId: integer;
    fRowId: integer;
    fStudyId: integer;
  protected
    { IListBoxBase }
    function V: string;
    function DN: string;
    function OT: string;
    function IsCurrent: boolean;
    function AsListBox( const ASimple: boolean = false ): string;
    { Property accessors }
    function Get_CollectorCount: integer;
    function Get_Collector( AIndex: integer ): string;
  public
    { Initialization }
    constructor Create( const AStudyId, AProcId: integer; const ATitle, AComment: string ); overload;
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { Other members }
    procedure AddCollector( const AName: string );
    procedure Load( ADataset: TDataset );
    procedure Save( const ASQL: ISQL );
    procedure Delete( const ASQL: ISQL );
    { Properties }
    property Collector[AIndex: integer]: string read Get_Collector;
    property CollectorCount: integer read Get_CollectorCount;
    property Comment: string read fComment;
    property PopulationId: integer read fPopulationId;
    property RowId: integer read fRowId;
    property StudyId: integer read fStudyId;
    property Title: string read fCaption;
  end;

implementation

uses
  SysUtils;

{ TDataElementSelection }

constructor TPackagedSelection.Create( const AStudyId, AProcId: integer; const ATitle, AComment: string );
begin
  inherited Create;
  fComment := AComment;
  fStudyId := AStudyId;
  fPopulationId := AProcId;
  fCaption := ATitle;
end;

procedure TPackagedSelection.AfterConstruction;
begin
  inherited;
  fCollectorNames := TStringList.Create;
  fCollectorNames.Delimiter := ';';
  fCollectorNames.StrictDelimiter := true;
  fCollectorNames.Sorted := true;
  fCollectorNames.Duplicates := dupIgnore;
end;

procedure TPackagedSelection.BeforeDestruction;
begin
  fCollectorNames.Free;
  inherited;
end;

procedure TPackagedSelection.AddCollector( const AName: string );
begin
  fCollectorNames.Add( AName );
end;

function TPackagedSelection.Get_Collector( AIndex: integer ): string;
begin
  Result := fCollectorNames[AIndex];
end;

function TPackagedSelection.Get_CollectorCount: integer;
begin
  Result := fCollectorNames.Count;
end;

procedure TPackagedSelection.Load( ADataset: TDataset );
begin
  with ADataset do
  begin
    fStudyId := FieldByName( FLD_STUDY_ID ).AsInteger;
    fRowId := FieldByName( FLD_ROW_ID ).AsInteger;
    fPopulationId := FieldByName( FLD_PROC_ID ).AsInteger;
    fCaption := FieldByName( FLD_TITLE ).AsString;
    fComment := FieldByName( FLD_COMMENT ).AsString;
    fCollectorNames.DelimitedText := FieldByName( FLD_DATA_ELEMENTS ).AsString;
  end;
end;

procedure TPackagedSelection.Save( const ASQL: ISQL );
begin
  with ASQL.FastQuery( CMD_ADD_PACKAGE, [fStudyId, fPopulationId, fCaption, fCollectorNames.DelimitedText, fComment] ) do
    try
      fRowId := FieldByName( FLD_ROW_ID ).AsInteger;
    finally
      CLose;
    end;
end;

function TPackagedSelection.V: string;
begin
  Result := IntToStr( fRowId );
end;

procedure TPackagedSelection.Delete( const ASQL: ISQL );
begin
  ASQL.ExecuteCommand( 'EXEC QuickStat.DeletePackage :RowId', [fRowId] );
end;

function TPackagedSelection.DN: string;
begin
  Result := fCaption;
end;

function TPackagedSelection.OT: string;
begin
  Result := Format( 'Pop#%d', [fPopulationId] );
end;

function TPackagedSelection.IsCurrent: boolean;
begin
  Result := true;
end;

function TPackagedSelection.AsListBox( const ASimple: boolean ): string;
begin
  Result := V + #9 + fCaption + #9;
  if not ASimple then
    Result := Result + fComment;
  Result := Result + #9 + OT;
end;

end.
