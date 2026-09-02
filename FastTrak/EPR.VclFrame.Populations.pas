unit EPR.VclFrame.Populations;

interface

uses
  Emetra.VclComp.ListView,
  Emetra.VclUtil.Style.Interfaces,
  Emetra.VclUtil.Module.Interfaces,
  EPR.Population.List,
  {CRF}
  CRF.Population.Interfaces,
  CRF.Study.Interfaces,
  {General}
  Emetra.Database.Interfaces,
  Emetra.AccessControl.Interfaces,
  Emetra.AccessControl.Constants,
  Emetra.Settings.Interfaces,
  Emetra.Logging.Interfaces,
  {Standard}
  Generics.Collections,
  Winapi.Windows, Winapi.Messages, System.SysUtils, System.Variants, System.Classes,
  Vcl.Graphics, Vcl.Controls, Vcl.Forms, Vcl.Dialogs, RzButton, RzTabs, Vcl.StdCtrls, Vcl.ExtCtrls, RzPanel, RzSplit;

type

  TfrmPopulations = class( TFrame, IGuiStyleObserver, IStudyObserver, IAccessControlObserver )
    edtPopFilter: TEdit;
    cbShowCommon: TCheckBox;
    cbSimpleView: TCheckBox;
    lblFilterHeader: TLabel;
    memSourceCode: TMemo;
    panCheckBoxes: TPanel;
    panFilter: TPanel;
    splitMain: TRzSplitter;
    Bevel1: TBevel;
  private
    { Private declarations }
    fDbInfo: IDatabaseInfo;
    fObservers: TList<IPopulationObserver>;
    fStudyId: IStudyId;
    fCurrentPopulation: IPopulation;
    fSQL: ISQL;
    fLog: ILog;
    fPopulations: TPopulationList;
    fPopView: TObjectListView;
    fUsable: boolean;
    fSettings: IScopedSettingsReadWrite;
    procedure PopulationRequested( Sender: TObject );
    procedure PopulationSelected( Sender: TObject );
    procedure RefreshPopulationListFromMemory( Sender: TObject );
    procedure ReadPopulationList( Sender: TObject );
    function TryGetHighlightedPopulation( out APopulation: IPopulation ): boolean;
    { Property accessors }
    function Get_Caption: string;
  protected
    { Respond to observers }
    procedure AfterStudyChange( const Sender: IStudyId );
    procedure UpdateStyle( Sender: IGuiStyle );
    { IAccessControlObserver }
    procedure AfterAccessControlChanged( const Sender: IAccessControl );
    procedure RegisterAsAccessControlObserver( const AManager: IAccessControlManager );
  public
    { Initialization }
    constructor Create( AOwner: TComponent; ADbInfo: IDatabaseInfo; ASettings: IScopedSettingsReadWrite; ASQL: ISQL; ALog: ILog ); reintroduce;
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    procedure Prepare( AParent: TWinControl; const ALayout: TAlign );
    { Other methods }
    function IsHighlighted( APopulation: IPopulation ): boolean;
    function TrySelect( const AProcId: integer; const ALoadIt: boolean; out APopulation: IPopulation ): boolean;
    { Add population observers that need to know when a population is activated }
    procedure AddPopulationObserver( AObserver: IPopulationObserver );
    { Properties }
    property Caption: string read Get_Caption;
    property ListView: TObjectListView read fPopView;
  end;

implementation

uses
  CRF.Population,
  CRF.SQL,
  System.Diagnostics;

resourcestring
  MSG_INVALID_REPORT = 'Det er ikke valgt en gyldig populasjon.';

{$R *.dfm}
  { TFrame1 }

{$REGION 'Initialization'}

constructor TfrmPopulations.Create( AOwner: TComponent; ADbInfo: IDatabaseInfo; ASettings: IScopedSettingsReadWrite; ASQL: ISQL; ALog: ILog );
begin
  inherited Create( AOwner );
  fSQL := ASQL;
  fLog := ALog;
  fDbInfo := ADbInfo;
  fSettings := ASettings;
end;

procedure TfrmPopulations.Prepare( AParent: TWinControl; const ALayout: TAlign );
begin
  Align := ALayout;
  Parent := AParent;
  ParentColor := true;
  ParentFont := true;
  ParentBackground := true;
  memSourceCode.ReadOnly := true;
  memSourceCode.WordWrap := false;
  memSourceCode.Font.Size := 8;
  memSourceCode.Font.Name := 'Consolas';
  cbShowCommon.Enabled := false;
end;

procedure TfrmPopulations.AfterConstruction;
begin
  inherited;
  fPopulations := TPopulationList.Create( fSQL, fLog );
  fPopView := TObjectListView.Create( Self );
  fPopView.Prepare( splitMain.UpperLeft.Pane, alClient );
  fPopView.List := fPopulations;
  fPopView.OnDblClick := PopulationRequested;
  fPopView.OnClick := PopulationSelected;
  fPopView.FilterCase := fcLower;
  edtPopFilter.OnChange := RefreshPopulationListFromMemory;
  cbSimpleView.OnClick := RefreshPopulationListFromMemory;
  cbShowCommon.OnClick := ReadPopulationList;
  fObservers := TList<IPopulationObserver>.Create;
end;

procedure TfrmPopulations.BeforeDestruction;
begin
  fObservers.Free;
  fPopView.List := nil;
  fPopulations.Free;
  inherited;
end;

{$ENDREGION}

{$REGION 'IStudyObserver'}

procedure TfrmPopulations.AfterStudyChange( const Sender: IStudyId );
begin
  cbShowCommon.Enabled := Sender.StudyId > 0;
  fStudyId := Sender;
  ReadPopulationList( Self );
end;

{$ENDREGION}
{$REGION 'IGuiStyleObserver'}

procedure TfrmPopulations.UpdateStyle( Sender: IGuiStyle );
begin
  Sender.StyleFrame( Self );
  Sender.StylePanel( panCheckBoxes );
  Sender.StyleCheckPanel( panCheckBoxes );
  Sender.StyleInfoLabel( lblFilterHeader );
  memSourceCode.Font.Color := Sender.DarkColor;
  splitMain.Color := Self.Color;
  splitMain.HotSpotColor := Sender.LightColor;
end;

{$ENDREGION}
{$REGION 'IAccessControlObserver'}

procedure TfrmPopulations.RegisterAsAccessControlObserver( const AManager: IAccessControlManager );
begin
  AManager.AddFunctionPoint( FUNC_POPULATION_SOURCE, asDenied );
end;

procedure TfrmPopulations.AfterAccessControlChanged( const Sender: IAccessControl );
begin
  memSourceCode.Text := EmptyStr;
  splitMain.LowerRight.Visible := Sender.TryGetAccess( FUNC_POPULATION_SOURCE );
end;

{$ENDREGION}

procedure TfrmPopulations.AddPopulationObserver( AObserver: IPopulationObserver );
begin
  fObservers.Add( AObserver );
end;

function TfrmPopulations.TrySelect( const AProcId: integer; const ALoadIt: boolean; out APopulation: IPopulation ): boolean;
var
  popObject: TPopulation;
begin
  APopulation := nil;
  if fPopulations.TryGetPopulation( AProcId, popObject ) then
  begin
    Assert( Supports( popObject, IPopulation, APopulation ) );
    Result := fPopView.TrySelectObject( popObject );
    if Result and ALoadIt then
      PopulationRequested( Self );
  end
  else
    Result := false;
end;

function TfrmPopulations.Get_Caption: string;
begin
  Result := fCurrentPopulation.Title;
end;

procedure TfrmPopulations.PopulationRequested( Sender: TObject );
const
  PROC_NAME = 'PopulationRequested';
var
  stopWatch: TStopWatch;
  obs: IPopulationObserver;
begin
  if TryGetHighlightedPopulation( fCurrentPopulation ) then
    try
      stopWatch := TStopWatch.StartNew;
      for obs in fObservers do
        obs.AfterPopulationSelect( fCurrentPopulation );
      fSQL.ExecuteCommand( CMD_LOG_POPULATION_CHANGE, [fStudyId.StudyId, fCurrentPopulation.ProcId, fCurrentPopulation.Title, stopWatch.ElapsedMilliseconds] );
    except
      on E: Exception do
        fLog.SilentWarning( '%s.%s: %s', [ClassName, PROC_NAME, E.Message] );
    end
  else
    fLog.Event( MSG_INVALID_REPORT );
end;

procedure TfrmPopulations.PopulationSelected( Sender: TObject );
var
  selPop: IPopulation;
begin
  if memSourceCode.Enabled then
    if TryGetHighlightedPopulation( selPop ) then
      memSourceCode.Text := StringReplace( StringReplace( selPop.SourceCode, #13#10, #10, [rfReplaceAll] ), #10, #13#10, [rfReplaceAll] )
    else
      memSourceCode.Text := EmptyStr;
end;

procedure TfrmPopulations.ReadPopulationList( Sender: TObject );
const
  PROC_NAME = 'ReadPopulationList';
begin
  fCurrentPopulation := nil;
  if fStudyId.StudyId >= 0 then
    try
      fPopulations.Load( fStudyId.StudyId, fDbInfo.DbVersion, cbShowCommon.Checked );
      fLog.SilentSuccess( 'Found %d populations', [fPopulations.Count] );
    except
      on E: Exception do
      begin
        fUsable := false;
        fLog.Event( E.Message, ltException );
      end;
    end;
end;

procedure TfrmPopulations.RefreshPopulationListFromMemory( Sender: TObject );
begin
  fPopView.RefreshView( cbSimpleView.Checked, edtPopFilter.Text, false );
end;

function TfrmPopulations.IsHighlighted( APopulation: IPopulation ): boolean;
var
  selPop: IPopulation;
begin
  Result := TryGetHighlightedPopulation( selPop ) and ( selPop.ProcId = APopulation.ProcId );
end;

function TfrmPopulations.TryGetHighlightedPopulation( out APopulation: IPopulation ): boolean;
begin
  APopulation := nil;
  Result := Supports( fPopView.SelectedObject, IPopulation, APopulation );
end;

end.
