unit QuickStat.Component.ReportTree;

interface

uses
  CRF.Population,
  EPR.Population.List,
  { CRF }
  CRF.Population.Interfaces,
  {General}
  Emetra.Interfaces.SaveLoad,
  Emetra.Logging.Interfaces,
  {Standard}
  Generics.Collections,
  Classes, ComCtrls, Contnrs, Controls, DB, Graphics, SysUtils;

type
  TPopulationTreeNode = class( TPopulation )
  private
    FNode: TTreeNode;
  public
    constructor Create( const AGroup, ATitle: string; ANode: TTreeNode ); reintroduce;
    property Node: TTreeNode read FNode;
  end;

  TReportTree = class( TTreeView, ILoad )
  private
    FLog: ILog;
    FReports: TPopulationList;
  public
    { Initialization }
    constructor Create( AOwner: TComponent; APopulationList: TPopulationList; ALog: ILog ); reintroduce;
    procedure BeforeDestruction; override;
    { Other members }
    function FindNode( const ANodeText: string; var ANode: TTreeNode ): boolean;
    function FindReport( const AProcId: integer; var ANode: TPopulation ): boolean;
    function ReportSelected( var AReportData: TPopulation ): boolean;
    function Select( const AProcId: integer ): boolean;
    procedure Load( ADataset: TDataset );
    procedure Filter( const ASearchText: string );
    procedure Prepare( AParent: TWinControl; ALayout: TAlign );
  end;

implementation

{ TPopulationNode }

constructor TPopulationTreeNode.Create( const AGroup, ATitle: string; ANode: TTreeNode );
begin
  inherited Create( AGroup, ATitle );
  FNode := ANode;
end;

{ TTreeBuilder }

constructor TReportTree.Create( AOwner: TComponent; APopulationList: TPopulationList; ALog: ILog );
begin
  inherited Create( AOwner );
  FLog := ALog;
  FReports := APopulationList;
  Self.ReadOnly := true;
  Self.AutoExpand := true;
  Self.Width := 200;
  Self.ShowRoot := false;
end;

procedure TReportTree.BeforeDestruction;
begin
  Self.OnCustomDraw := nil;
  Self.OnChange := nil;
  Self.OnDblClick := nil;
  inherited;
end;

procedure TReportTree.Filter( const ASearchText: string );
var
  n: integer;
begin
  n := 0;
  while n < Items.Count do
  begin
    { Not implemented }
    inc( n );
  end;
end;

function TReportTree.FindNode( const ANodeText: string; var ANode: TTreeNode ): boolean;
var
  n: integer;
begin
  Result := false;
  n := 0;
  while n < Items.Count do
  begin
    ANode := Items[n];
    if SameText( ANodeText, ANode.Text ) then
    begin
      Result := true;
      exit;
    end;
    inc( n );
  end;
end;

function TReportTree.FindReport( const AProcId: integer; var ANode: TPopulation ): boolean;
begin
  Result := FReports.TryGetPopulation( AProcId, ANode );
end;

function TReportTree.Select( const AProcId: integer ): boolean;
var
  thisPopulation: TPopulation;
begin
  Result := FindReport( AProcId, thisPopulation );
  if Result and thisPopulation.InheritsFrom( TPopulationTreeNode ) then
    TPopulationTreeNode( thisPopulation ).Node.Selected := true;
end;

procedure TReportTree.Prepare( AParent: TWinControl; ALayout: TAlign );
begin
  Parent := AParent;
  ALign := ALayout;
end;

function TReportTree.ReportSelected( var AReportData: TPopulation ): boolean;
begin
  if Assigned( Selected ) and Assigned( Selected.Data ) then
    AReportData := Selected.Data
  else
    AReportData := nil;
  Result := Assigned( AReportData );
end;

procedure TReportTree.Load( ADataset: TDataset );
const
  PROC_NAME = 'Load';
var
  fldGroup: TField;
  fldTitle: TField;
  groupNodeText: string;
  parentTreeNode: TTreeNode;
  reportTreeNode: TTreeNode;
  reportCaption: string;
begin
  FLog.EnterMethod( Self, Format( '%s.%s()', [ClassName, PROC_NAME] ) );
  try
    Items.Clear;
    with ADataset do
      try
        fldGroup := FindField( FLD_PROC_GROUP );
        fldTitle := FindField( FLD_PROC_TITLE );
        Assert( Assigned( fldGroup ) and Assigned( fldTitle ) );
        while not EOF do
        begin
          groupNodeText := FindField( FLD_PROC_GROUP ).AsString;
          reportCaption := FindField( FLD_PROC_TITLE ).AsString;
          if groupNodeText = EmptyStr then
            parentTreeNode := nil
          else if not FindNode( groupNodeText, parentTreeNode ) then
            parentTreeNode := Items.Add( nil, groupNodeText );
          if Assigned( parentTreeNode ) then
          begin
            parentTreeNode.ImageIndex := 1;
            parentTreeNode.SelectedIndex := 2;
          end;
          reportTreeNode := Items.AddChild( parentTreeNode, reportCaption );
          reportTreeNode.ImageIndex := 0;
          reportTreeNode.Data := FReports.Add( TPopulationTreeNode.CreateAndNext( ADataset ) );
        end;
      finally
        Self.AlphaSort( true );
      end;
  finally
    FLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

end.
