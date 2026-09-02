unit MainQuickStat;

interface

uses
  QuickStat.Collectors,
  QuickStat.Connections,
  QuickStat.Selection,

  EPR.QA.GUI.Grid.Study,
  EPR.VclFrame.Populations,
  EPR.PeriodDictionary,

  {QA Mtarxi}
  EPR.QA.Matrix,
  EPR.QA.Matrix.Row,
  EPR.QA.Matrix.Interfaces,
  EPR.QA.Collector.Standard,
  EPR.QA.DataPoint,
  EPR.QA.SQL,

  {CRF}
  CRF.Patient.List,
  CRF.Population,
  CRF.Population.Interfaces,
  CRF.Context.Facade,

  {General GUI}
  Emetra.VclUtil.Spotlight,
  Emetra.VclUtil.Listbox,
  Emetra.VclUtil.Settings,
  Emetra.VclUtil.Settings.Interfaces,
  Emetra.VclUtil.Module.Interfaces,
  Emetra.VclForm.EditAndMemo,
  Emetra.VclForm.Period,

  {General classes}
  Emetra.Adapters.Office,
  Emetra.Database.Simple,
  Emetra.Database.ParameterDictionary,
  Emetra.Settings.Test,
  Emetra.Settings.IniFile,
  Emetra.StrUtils,
  Emetra.Win.Launcher,
  Emetra.Win.User,
  Emetra.Classes.Transient,
  Emetra.VclUtil.ArenaColors,

  {General interfaces}
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces,
  Emetra.Progress.Interfaces,
  Emetra.Settings.Interfaces,
  Emetra.Dictionary.Interfaces,
  {Third party}
  RzPanel, RzSplit, RzTabs, RzStatus,
  {Standard}
  Winapi.Windows, Winapi.Messages,
  System.Contnrs, System.Generics.Collections, System.SysUtils, System.Variants, System.Classes, System.Actions,
  Vcl.Graphics, Vcl.Controls, Vcl.Forms, Vcl.Dialogs, Vcl.StdCtrls, Vcl.Buttons, Vcl.Grids, Vcl.ComCtrls, Vcl.ExtCtrls,
  Vcl.ImgList, Vcl.CheckLst, Vcl.ActnList, Vcl.ToolWin, Vcl.ActnMan, Vcl.ActnCtrls, Vcl.PlatformDefaultStyleActnCtrls,
  Vcl.Menus, System.ImageList;

type
  TfrmQuickStat = class( TForm, IStatus, IProgress, ILoginObserver, IPopulationObserver )
    actCollectData: TAction;
    actDeletePackage: TAction;
    actExportData: TAction;
    ActionManager1: TActionManager;
    actSaveDataPackage: TAction;
    actSaveDataset: TAction;
    actSavePatientSelection: TAction;
    btnCollectData: TSpeedButton;
    cbDataCollector: TCheckListBox;
    cbExportDates: TCheckBox;
    cbProject: TComboBox;
    cbShowDataHint: TCheckBox;
    cbWideColumns: TCheckBox;
    Deletethispackage1: TMenuItem;
    edtPackageFilter: TEdit;
    FileSaveDialog1: TFileSaveDialog;
    hdrDatabase: TLabel;
    hdrElements: TLabel;
    hdrExportOptions: TLabel;
    hdrPackages: TLabel;
    hdrPopulation: TLabel;
    hdrPopulationName: TLabel;
    imgAppIcon: TImage;
    imgzFolderList: TImageList;
    lblAppName: TLabel;
    lblDataElementInfo: TLabel;
    lblDataHint: TLabel;
    lblHintPopulation: TLabel;
    lblInfo: TLabel;
    lblProgress: TLabel;
    lbPackagedGrids: TListBox;
    lstActiveImages: TImageList;
    lstDisabledImages: TImageList;
    mnuExportToExcel: TMenuItem;
    mnuGridPopup: TPopupMenu;
    mnuPackagePopup: TPopupMenu;
    mnuSaveDataPackage: TMenuItem;
    mnuSaveDataset: TMenuItem;
    N1: TMenuItem;
    Panel1: TPanel;
    panExportSettings: TPanel;
    panGrid: TPanel;
    panHdrDatabase: TPanel;
    panHdrElements: TPanel;
    panHdrExportOptions: TPanel;
    panHdrPackages: TPanel;
    panHdrPopulation: TPanel;
    panHdrYourDataset: TPanel;
    panHint: TPanel;
    panPopulation: TPanel;
    panProgress: TPanel;
    panSettings: TPanel;
    panWhiteTop: TPanel;
    pbProgress: TProgressBar;
    pgDataset: TRzPageControl;
    pgSelections: TRzPageControl;
    RzVersionInfo1: TRzVersionInfo;
    RzVersionInfoStatus1: TRzVersionInfoStatus;
    splMain: TRzSplitter;
    tbrPackages: TToolBar;
    tbsDataElements: TRzTabSheet;
    tbsOverview: TRzTabSheet;
    tbsPackages: TRzTabSheet;
    tbsPopulation: TRzTabSheet;
    tbsTimeSeries: TRzTabSheet;
    ToolButton1: TToolButton;
    rbRandomisePids: TRadioButton;
    rbKeepPids: TRadioButton;
    rbFullIdentification: TRadioButton;
    procedure FormCreate( Sender: TObject );
    procedure FormDestroy( Sender: TObject );
    procedure FormShow( Sender: TObject );
    procedure FormClose( Sender: TObject; var Action: TCloseAction );
    procedure actCollectDataExecute( Sender: TObject );
    procedure actExportToExcelExecute( Sender: TObject );
    procedure actSaveDataPackageExecute( Sender: TObject );
    procedure actSavePatientSelectionExecute( Sender: TObject );
    procedure actSaveDatasetToCsvExecute( Sender: TObject );
    procedure cbWideColumnsChecked( Sender: TObject );
    procedure actDeletePackageExecute( Sender: TObject );
  strict private
    { Private declarations }
    fConnection: TQuickStatConnection;
    fQuickStatConnections: TConnectionList;
    fCrfContext: TCRFSimpleContext;
    fPersonList: TPatientList;
    fGridPopulation: IPopulation;
    fPackagedQuickStatGrids: TObjectList;
    fQuickStat: TQuickStatCollectors;
    fSettings: IScopedSettingsReadWrite;
    fFilesThatMustBeDeleted: TStringList;
    { GUI }
    fSelContext: TSpotLightContext;
    frmPopulations: TfrmPopulations;
    fGrid: TStudyOverviewGrid;
    fListBoxPainter: TCustomListControlPainter;
    fGuiSettings: IGuiSettings;
    { Key event handlers }
    procedure AddCaptions;
    procedure PreparePackagedSelection( Sender: TObject );
    procedure SelectConnection( Sender: TObject );
    procedure ToggleGridAnonymity( Sender: TObject );
    procedure UpdateDataHintPanel( Sender: TObject );
    procedure ValidateCollectorSelection( Sender: TObject );
    procedure ApplyColors;
    procedure UpdateGridInfo;
  private
    { Other members }
    function GetTemporaryCsvFileName: string;
    function PersonGridIdentification: TPersonIdentification;
    function TryFindCollector( const ACollectorName: string; out AFoundAt: integer ): boolean;
    procedure LoadPopulationIntoGrid( APopulation: IPopulation );
    procedure LoadPackagedSelections( Sender: TObject );
  protected
    { ILoginObserver }
    function FriendlyName: string;
    procedure AfterLogin( Sender: IDatabaseConnection );
    { IPopulationObserver }
    procedure AfterPopulationSelect( APopulation: IPopulation );
    { IProgress }
    function GetInfo: string;
    procedure Done;
    procedure SetProgress( const AProgressPercent: double );
    procedure SetInfo( const s: string );
    procedure SetHeader( const s: string );
  public
    { Public declarations }
  end;

var
  frmQuickStat: TfrmQuickStat;

implementation

uses
  EPR.QA.Collector.Drug,
  EPR.QA.CaptionRecord,
  Spring,
  Math, Db;

resourcestring
  MSG_UNKNOWN_POPULATION =
  { } 'The selection is based on an unknown population (ProcId=%d).\n' +
  { } 'The data collection can not be performed at this time.\n' +
  { } 'Perhaps the population is from a different protocol?';

  MSG_UNKNOWN_COLLECTOR =
  { } 'The selection contains an unknown data element.\n' +
  { } 'Element name was "%s".\n' +
  { } 'The data collection will be incomplete.\n' +
  { } 'Perhaps the selection was created in a later version?';

  TXT_SAVE_SPEC = 'Save specification';
  TXT_SAVE_SELECTION = 'Save selection';

  TXT_TASK_COMPLETED = 'Task completed';

  MSG_SAVE_SUCCESSFUL = 'Selection was successfully saved.';
  WARN_SAVE_FAILED = 'There was a problem:\n%s';
  WARN_NO_PACKAGE_SELECTED = 'You need to select a package for this operation.';
  CONFIRM_DELETE_PACKAGE = 'Do you really want to delete this package:\n"%s"?';

  TXT_PROJECT_SELECTED = 'New project selected';
  TXT_CONNECTING = 'Connecting to %s ...';
  MSG_NO_POPULATION = 'No population selected!';

  ERR_POPULATION_NOT_SELECTED =
  { } 'The population was not selected as expected,\n' +
  { } 'You may need an updated version of QuickStat.';

  ERR_CONFIG_FILE_MISSING =
  { } 'The configuration file %s was not found.\n' +
  { } 'QuickStat can not be used without this file.';

resourcestring
  { Gui elements in population frame }
  rsFilterSearchText = 'Filter / search text';
  rsFrequentlyUsedOnly = 'Frequently used only';
  rsSimplifiedView = 'Simplified';
  rsFilterTextHint = 'Type filter text here';
  rsGridInfo = 'Population: %d "%s". Grid size: %d x %d';

const
  COL_WIDTH = 64;

{$R *.dfm}
{$REGION 'Initialize Form'}

procedure TfrmQuickStat.FormCreate( Sender: TObject );
const
  PROC_NAME = 'FormCreate';
begin
  TDrugCollector.GroupResults := false;
  GlobalLog.Enabled := true;
  GlobalLog.LogCallStack := true;
  GlobalLog.EnterMethod( Self, PROC_NAME );
  try
    fSettings := TIniSettings.Create;
    fGuiSettings := TGuiSettings.Create( Self, fSettings, GlobalLog );
    { Prepare key business objects }
    fFilesThatMustBeDeleted := TStringList.Create;
    fPackagedQuickStatGrids := TObjectList.Create;
    fQuickStatConnections := TConnectionList.Create( [doOwnsValues] );
    fCrfContext := TCRFSimpleContext.Create( GlobalLog );
    fCrfContext.Database.LogSql := true;
    fCrfContext.Database.AddLoginObserver( Self );
    fPersonList := TPatientList.Create( fCrfContext, TParameterDictionary.Create( TPeriodDictionary.Create( fSettings, GlobalLog ), fCrfContext, GlobalLog ), fCrfContext.Database, GlobalLog );
    { Prepare population view }
    fQuickStat := TQuickStatCollectors.Create( fCrfContext.Database, Self, GlobalLog );
    { Create grid view }
    fGrid := TStudyOverviewGrid.Create( nil, fPersonList, Self, fCrfContext.Database, GlobalLog );
    fGrid.OnGetColor := fQuickStat.ProvideColor;
    fGrid.Prepare( panGrid, alClient );
    fGrid.DataColWidth := COL_WIDTH;
    fGrid.PopupMenu := mnuGridPopup;
    fGrid.Anonymous := ( rbRandomisePids.Checked or rbKeepPids.Checked );
    { Move on top of the grid }
    panHint.Parent := panGrid;
    panHint.Visible := false;
    { Create population view }
    frmPopulations := TfrmPopulations.Create( nil, fCrfContext.DatabaseInfo, fSettings, fCrfContext.Database, GlobalLog );
    frmPopulations.Prepare( panPopulation, alClient );
    frmPopulations.AddPopulationObserver( Self );
    frmPopulations.edtPopFilter.TextHint := rsFilterTextHint;
    frmPopulations.lblFilterHeader.Caption := rsFilterSearchText;
    frmPopulations.cbShowCommon.Caption := rsFrequentlyUsedOnly;
    frmPopulations.cbSimpleView.Caption := rsSimplifiedView;
    fCrfContext.Session.AddStudyObserver( frmPopulations );
    { Custom painting / handling of saved selections }
    fSelContext := TSpotLightContext.Create( lbPackagedGrids, fPackagedQuickStatGrids, edtPackageFilter, nil, nil, GlobalLog );
    fListBoxPainter := TCustomListControlPainter.Create( Self );
    fListBoxPainter.Attach( lbPackagedGrids );
    { Final preparation of GUI }
    tbsDataElements.TabVisible := false;
    tbsDataElements.Enabled := false;
    pgSelections.ActivePageIndex := 0;
    pgDataset.ActivePageIndex := 0;
    pbProgress.Max := 100;
    { Wire up events to gui }
    cbProject.OnChange := SelectConnection;
    rbRandomisePids.OnClick := ToggleGridAnonymity;
    rbKeepPids.OnClick := ToggleGridAnonymity;
    rbFullIdentification.OnClick := ToggleGridAnonymity;
    cbDataCollector.OnClickCheck := ValidateCollectorSelection; { Check box click triggers enable/disable button }
    cbShowDataHint.OnClick := UpdateDataHintPanel;
    fGrid.OnClick := UpdateDataHintPanel; { Moving around in grid triggers update hint view }
    lbPackagedGrids.OnDblClick := PreparePackagedSelection; { Double click on packaged selection activates in }
  finally
    GlobalLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TfrmQuickStat.FormDestroy( Sender: TObject );
const
  PROC_NAME = 'FormDestroy';
begin
  GlobalLog.EnterMethod( Self, PROC_NAME );
  try
    fCrfContext.Disconnect;
    fListBoxPainter.Detach( lbPackagedGrids );
    while ( fFilesThatMustBeDeleted.Count > 0 ) do
    begin
      try
        System.SysUtils.DeleteFile( fFilesThatMustBeDeleted[0] );
      except
        on E: Exception do
          GlobalLog.Event( E.Message );
      end;
      fFilesThatMustBeDeleted.Delete( 0 );
    end;
    cbDataCollector.Clear;
    FreeAndNil( fFilesThatMustBeDeleted );
    FreeAndNil( fQuickStatConnections );
    FreeAndNil( fPackagedQuickStatGrids );
    FreeAndNil( fGrid );
    FreeAndNil( fQuickStat );
    FreeAndNil( frmPopulations );
    FreeAndNil( fPersonList );
    FreeAndNil( fCrfContext );
    FreeAndNil( fSelContext );
  finally
    GlobalLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TfrmQuickStat.ApplyColors;
const
  FONT_NAME = 'Calibri';
  FONT_SIZE = 9;
begin
  pgSelections.BackgroundColor := Self.Color;
  pgDataset.BackgroundColor := Self.Color;
  // exit;
  TArenaColors.StyleForm( Self );
  TArenaColors.StyleTabs( pgSelections );
  TArenaColors.StyleTabs( pgDataset );
  TArenaColors.StyleFrame( frmPopulations );
  lblProgress.Font.Name := FONT_NAME;
  lblProgress.Font.Size := FONT_SIZE + 1;
  { Set up panel }
  TArenaColors.StyleHeaderPanel( panHdrDatabase );
  TArenaColors.StyleHeaderPanel( panHdrPopulation );
  TArenaColors.StyleHeaderPanel( panHdrYourDataset );
  TArenaColors.StyleHeaderPanel( panHdrElements );
  TArenaColors.StyleHeaderPanel( panHdrPackages );
  TArenaColors.StyleHeaderPanel( panHdrExportOptions );
  TArenaColors.StyleListView( frmPopulations.ListView );
  TArenaColors.StyleSimpleCheckbox( frmPopulations.cbSimpleView );
  splMain.Color := clMyGreenColor;
  fGrid.FixedColor := clMyGreenColor;
  fGrid.FixedFontColor := clMenuBackgroundDarkBrush;
  cbWideColumns.Font.Size := FONT_SIZE;
  cbWideColumns.Top := hdrPopulationName.Top;
  cbWideColumns.Left := hdrPopulationName.Width - cbWideColumns.Width;
end;

procedure TfrmQuickStat.FormShow( Sender: TObject );
const
  PROC_NAME = 'FormShow';
var
  configFileName: string;
begin
  GlobalLog.EnterMethod( Self, PROC_NAME );
  try
    fGuiSettings.RestoreFormState;
    configFileName := ChangeFileExt( ParamStr( 0 ), '.config.xml' );
    if FileExists( configFileName ) then
    begin
      fQuickStatConnections.Load( configFileName );
      fQuickStatConnections.AddToStrings( cbProject.Items );
    end
    else
      GlobalLog.Event( ERR_CONFIG_FILE_MISSING, [ExtractFileName( configFileName )], ltException );
    cbProject.Sorted := true;
    cbDataCollector.Sorted := true;
    Application.CreateForm( TfrmSaveSpec, frmSaveSpec );
    ApplyColors;
  finally
    GlobalLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TfrmQuickStat.FormClose( Sender: TObject; var Action: TCloseAction );
begin
  fGuiSettings.SaveFormState;
end;

function TfrmQuickStat.FriendlyName: string;
begin
  Result := Caption;
end;

{$ENDREGION}
{$REGION 'IProgress'}

function TfrmQuickStat.GetInfo: string;
begin
  Result := lblInfo.Caption;
end;

procedure TfrmQuickStat.Done;
begin
  pbProgress.Position := 100;
  pbProgress.State := pbsNormal;
  SetInfo( TXT_TASK_COMPLETED );
end;

procedure TfrmQuickStat.SetHeader( const s: string );
begin
  lblProgress.Caption := s;
end;

procedure TfrmQuickStat.SetInfo( const s: string );
begin
  lblInfo.Caption := s;
  lblInfo.Update;
end;

procedure TfrmQuickStat.SetProgress( const AProgressPercent: double );
begin
  pbProgress.State := pbsNormal;
  pbProgress.Position := round( AProgressPercent );
end;

{$ENDREGION}
{$REGION 'Callback for observer methods'}

procedure TfrmQuickStat.AddCaptions;
begin
  { Add captions }
  fGrid.Data.AddCaption( TCaptionRecord.Create( 'DRUID.RED', 'DDI-R', 'Drug-Drug interactions, red level' ) );
  fGrid.Data.AddCaption( TCaptionRecord.Create( 'DRUID.YELLOW', 'DDI-Y', 'Drug-Drug interactions, yellow level' ) );
  fGrid.Data.AddCaption( TCaptionRecord.Create( 'DRUID.ORANGE', 'DDI-O', 'Drug-Drug interactions, orange level' ) );
  fGrid.Data.AddCaption( TCaptionRecord.Create( 'DRUID.GREEN', 'DDI-G', 'Drug-Drug interactions, green level' ) );
  fGrid.Data.AddCaption( TCaptionRecord.Create( 'DRUG.F', 'Regular' ) );
  fGrid.Data.AddCaption( TCaptionRecord.Create( 'DRUG.B', 'AsNeeded' ) );
  fGrid.Data.AddCaption( TCaptionRecord.Create( 'DRUG.U', 'Weekly' ) );
  fGrid.Data.AddCaption( TCaptionRecord.Create( 'DRUG.X', 'Unspec' ) );
  fGrid.Data.AddCaption( TCaptionRecord.Create( 'DRUG.K', 'Cure' ) );
  fGrid.Data.AddCaption( TCaptionRecord.Create( 'DRUG.NOATC', 'NoAtc' ) );
  fGrid.Data.AddCaption( TCaptionRecord.Create( 'DRUG.RESISTANCE_DRIVING', 'Resist', 'Resistance-driving antibiotics' ) );
  fGrid.Data.AddCaption( TCaptionRecord.Create( 'DRUG.METFORMIN', 'Metform', 'Metformin' ) );
  fGrid.Data.LoadCaptions( true, false );
end;

procedure TfrmQuickStat.AfterLogin( Sender: IDatabaseConnection );
const
  PROC_NAME = 'AfterLogin';
var
  n: integer;
begin
  GlobalLog.EnterMethod( Self, PROC_NAME );
  try
    fGrid.Data.PrepareStudy( fCrfContext.StudyName );
    fQuickStat.PrepareStudy( fCrfContext );
    cbDataCollector.Clear;
    n := 0;
    while n < fQuickStat.Collectors.Count do
    begin
      cbDataCollector.Items.AddObject( fQuickStat.Collectors[n].Title, fQuickStat.Collectors[n] );
      inc( n );
    end;
    LoadPackagedSelections( Self );
    ValidateCollectorSelection( Self );
  finally
    GlobalLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TfrmQuickStat.SelectConnection( Sender: TObject );
begin
  Screen.Cursor := crSqlWait;
  try
    Application.ProcessMessages;
    SetInfo( TXT_PROJECT_SELECTED );
    if fCrfContext.Connected then
      fCrfContext.Disconnect;
    { Retrieve the connection object from combobox }
    with cbProject do
      if ItemIndex = -1 then
        fConnection := nil
      else
        fConnection := Items.Objects[ItemIndex] as TQuickStatConnection;
    if Assigned( fConnection ) then
      try
        SetInfo( Format( TXT_CONNECTING, [fConnection.Name] ) );
        fCrfContext.Connect( fConnection.StudyName, fConnection.ConnectionString );
      finally
        Done;
      end;
  finally
    Screen.Cursor := crDefault;
  end;
end;

procedure TfrmQuickStat.AfterPopulationSelect( APopulation: IPopulation );
const
  PROC_NAME = 'AfterPopulationSelect';
var
  crSaved: TCursor;
begin
  crSaved := Screen.Cursor;
  Screen.Cursor := crSqlWait;
  GlobalLog.EnterMethod( Self, PROC_NAME );
  try
    pgDataset.ActivePage := tbsOverview;
    fGrid.Clear;
    if frmPopulations.IsHighlighted( APopulation ) then
    begin
      fGridPopulation := nil;
      fPersonList.Load( APopulation );
     if not fPersonList.IncludesNationalId then
        fPersonList.AddNationalIds;
      LoadPopulationIntoGrid( APopulation );
      pgSelections.ActivePage := tbsDataElements;
      fGrid.StartPainting;
    end
    else
      GlobalLog.Event( ERR_POPULATION_NOT_SELECTED, ltError );
  finally
    Screen.Cursor := crSaved;
    GlobalLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

{$ENDREGION}

procedure TfrmQuickStat.LoadPopulationIntoGrid( APopulation: IPopulation );
const
  PROC_NAME = 'LoadPopulationIntoGrid';
begin
  GlobalLog.EnterMethod( Self, PROC_NAME );
  try
    if not Assigned( APopulation ) then
      GlobalLog.Event( MSG_NO_POPULATION, ltMessage )
    else
    begin
      fGrid.Data.ClearPopulation;
      fGrid.Data.SortBy := sbPersonId;
      fGrid.Data.PreparePopulation( fPersonList );
      fGridPopulation := APopulation;
      tbsDataElements.TabVisible := fGrid.Data.DataRows > 0;
      tbsDataElements.Enabled := tbsDataElements.TabVisible;
      UpdateGridInfo;
    end;
  finally
    GlobalLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TfrmQuickStat.UpdateDataHintPanel( Sender: TObject );
var
  strHint: string;
  thisPatient: TPersonGridRow;
  thisDatapoint: TDataPoint;
  panRect: TRect;
begin
  panHint.Visible := false;
  if cbShowDataHint.Checked then
    try
      if fGrid.Data.TryGetPatientAtRow( fGrid.Row, thisPatient ) then
      begin
        if fGrid.Anonymous then
          strHint := Format( 'PersonId = %d', [thisPatient.PersonId] )
        else
          strHint := thisPatient.FullName;
        if fGrid.Data.TryGetDatapoint( fGrid.Col, fGrid.Row, thisDatapoint ) then
        begin
          strHint := strHint + sLineBreak + thisDatapoint.AsString;
          lblDataHint.Caption := strHint;
          panRect := fGrid.CellRect( fGrid.Col, fGrid.Row );
          OffsetRect( panRect, 3, 3 );
          panHint.Top := fGrid.Top + panRect.Top + fGrid.DefaultRowHeight + 1;
          panHint.Left := fGrid.Left + panRect.Left;
          panHint.ClientHeight := ( 8 * abs( lblDataHint.Font.Height ) + panHint.BorderWidth * 2 + 8 );
          panHint.Visible := true;
          panHint.BringToFront;
        end;
      end;
    except
      on E: Exception do
      begin
        lblInfo.Font.Color := clRed;
        lblInfo.Caption := E.Message;
      end;
    end;
end;

procedure TfrmQuickStat.UpdateGridInfo;
begin
  hdrPopulationName.Caption := Format( rsGridInfo, [fGridPopulation.ProcId, fGridPopulation.Title, fGrid.Data.DataRows, fGrid.Data.FieldCount] );
end;

function TfrmQuickStat.GetTemporaryCsvFileName: string;
begin
  Result := GetTempDir + GetNewStrippedGuid + '.csv';
  fFilesThatMustBeDeleted.Add( Result );
end;

procedure TfrmQuickStat.cbWideColumnsChecked( Sender: TObject );
begin
  if cbWideColumns.Checked then
    fGrid.DataColWidth := 120
  else
    fGrid.DataColWidth := COL_WIDTH;
end;

procedure TfrmQuickStat.actCollectDataExecute( Sender: TObject );
const
  PROC_NAME = 'actCollectDataExecute';
var
  n: integer;
  selectedCollector: IGridDataCollector;
  crSaved: TCursor;
  savedTopIndex: integer;
  savedItemIndex: integer;
begin
  GlobalLog.EnterMethod( Self, PROC_NAME );
  crSaved := Screen.Cursor;
  try
    Screen.Cursor := crSqlWait;
    fGrid.Data.ClearVariables;
    AddCaptions;
    with cbDataCollector do
    begin
      { Save Scroll info }
      savedTopIndex := TopIndex;
      savedItemIndex := ItemIndex;
      n := 0;
      { Loop through every collector and add data for those that are checked }
      while n < Items.Count do
      begin
        Update;
        if Checked[n] then
        begin
          ItemIndex := n;
          if Supports( Items.Objects[n], IGridDataCollector, selectedCollector ) then
          begin
            SetInfo( selectedCollector.Title );
            fGrid.Data.AddData( selectedCollector );
          end;
        end;
        inc( n );
      end;
      actExportData.Enabled := fGrid.Data.HasData;
      { Restore scroll status }
      TopIndex := savedTopIndex;
      ItemIndex := savedItemIndex;
    end;
    fGrid.Lock;
  finally
    UpdateGridInfo;
    Done;
    Screen.Cursor := crSaved;
    GlobalLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TfrmQuickStat.ToggleGridAnonymity( Sender: TObject );
begin
  fGrid.Anonymous := not( rbFullIdentification.Checked );
end;

procedure TfrmQuickStat.ValidateCollectorSelection( Sender: TObject );
const
  PROC_NAME = 'CheckValidSelection';
var
  collectorIndex: integer;
begin
  GlobalLog.EnterMethod( Self, PROC_NAME );
  try
    actCollectData.Enabled := false;
    actSaveDataPackage.Enabled := false;
    collectorIndex := 0;
    while collectorIndex < cbDataCollector.Count do
    begin
      if cbDataCollector.Checked[collectorIndex] then
      begin
        actCollectData.Enabled := true;
        actSaveDataPackage.Enabled := true;
        break;
      end;
      inc( collectorIndex );
    end;
  finally
    GlobalLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

function TfrmQuickStat.TryFindCollector( const ACollectorName: string; out AFoundAt: integer ): boolean;
var
  dataCollector: IGridDataCollector;
begin
  AFoundAt := 0;
  Result := false;
  while AFoundAt < cbDataCollector.Items.Count do
  begin
    if Supports( cbDataCollector.Items.Objects[AFoundAt], IGridDataCollector, dataCollector ) then
      if SameText( ACollectorName, dataCollector.Name ) or SameText( ACollectorName, dataCollector.Title ) then
      begin
        Result := true;
        break;
      end;
    inc( AFoundAt );
  end;
end;

procedure TfrmQuickStat.actSavePatientSelectionExecute( Sender: TObject );
begin
  frmSaveSpec.SetHeader( TXT_SAVE_SELECTION );
  if frmSaveSpec.ShowModal = mrOk then
    try
      fGrid.Data.SaveToSelection( frmSaveSpec.Title, frmSaveSpec.Comment );
      GlobalLog.Event( MSG_SAVE_SUCCESSFUL, ltMessage );
    except
      on E: Exception do
        GlobalLog.Event( WARN_SAVE_FAILED, [E.Message], ltWarning );
    end;
end;

{$REGION 'Save and export actions' }

procedure TfrmQuickStat.actExportToExcelExecute( Sender: TObject );
var
  fileName: string;
begin
  fileName := Self.GetTemporaryCsvFileName;
  fGrid.SaveToFile( fileName, PersonGridIdentification, cbExportDates.Checked );
  TExcelAdapter.LoadWithFile( fileName, GlobalLog );
end;

procedure TfrmQuickStat.actSaveDatasetToCsvExecute( Sender: TObject );
var
  fileName: string;
begin
  if FileSaveDialog1.Execute then
  begin
    fileName := FileSaveDialog1.fileName;
    fGrid.SaveToFile( fileName, PersonGridIdentification, cbExportDates.Checked );
  end;
end;

{$ENDREGION}
{$REGION 'Packaged selections'}

procedure TfrmQuickStat.PreparePackagedSelection( Sender: TObject );
const
  PROC_NAME = 'PrepareSavedSelection';
var
  n: integer;
  packagedSelection: TPackagedSelection;
  selectedPopulation: IPopulation;
  foundAt: integer;
begin
  GlobalLog.EnterMethod( Self, PROC_NAME );
  try
    with lbPackagedGrids do
    begin
      if ItemIndex <> -1 then
      begin
        { First find the correct population from selection and load it }
        packagedSelection := Items.Objects[ItemIndex] as TPackagedSelection;
        if not frmPopulations.TrySelect( packagedSelection.PopulationId, true, selectedPopulation ) then
          GlobalLog.Event( MSG_UNKNOWN_POPULATION, [packagedSelection.PopulationId], ltWarning )
        else
        begin
          { Load transfer population to grid }
          LoadPopulationIntoGrid( selectedPopulation );
          { Make the selection in the listbox, based on name }
          cbDataCollector.CheckAll( cbUnchecked );
          n := 0;
          while n < packagedSelection.CollectorCount do
          begin
            if TryFindCollector( packagedSelection.Collector[n], foundAt ) then
              cbDataCollector.Checked[foundAt] := true
            else
              GlobalLog.Event( MSG_UNKNOWN_COLLECTOR, [packagedSelection.Collector[n]], ltWarning );
            inc( n );
          end;
          actCollectDataExecute( Self );
          hdrPopulationName.Caption := packagedSelection.Title;
        end;
      end;
    end;
  finally
    GlobalLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TfrmQuickStat.LoadPackagedSelections( Sender: TObject );
const
  PROC_NAME = 'LoadPackagedSelections';
var
  packagedSelection: TPackagedSelection;
  thisDataset: TDataset;
begin
  GlobalLog.EnterMethod( Self, Format( '%s("%s")', [PROC_NAME, fCrfContext.StudyName] ) );
  try
    fPackagedQuickStatGrids.Clear;
    thisDataset := fCrfContext.Database.FastQuery( QRY_GET_PACKAGES, [fGrid.Data.StudyId] );
    with thisDataset do
      try
        while not EOF do
        begin
          packagedSelection := TPackagedSelection.Create;
          packagedSelection.Load( thisDataset );
          fPackagedQuickStatGrids.Add( packagedSelection );
          Next;
        end;
        fSelContext.RefreshList( Self );
      finally
        Close;
      end;
  finally
    GlobalLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TfrmQuickStat.actSaveDataPackageExecute( Sender: TObject );
const
  PROC_NAME = 'actSaveExecute';
var
  collectorIndex: integer;
  collectorName: string;
  collectorNameList: TStringList;
  dataCollector: IGridDataCollector;
  dataElementSelection: TPackagedSelection;
begin
  GlobalLog.EnterMethod( Self, PROC_NAME );
  collectorNameList := TStringList.Create;
  try
    Guard.CheckNotNull( fGridPopulation, 'GridPopulation' );
    collectorIndex := 0;
    while collectorIndex < cbDataCollector.Count do
    begin
      if cbDataCollector.Checked[collectorIndex] and Supports( cbDataCollector.Items.Objects[collectorIndex], IGridDataCollector, dataCollector ) then
        collectorNameList.Add( dataCollector.Name );
      inc( collectorIndex );
    end;
    frmSaveSpec.Clear;
    frmSaveSpec.SetHeader( TXT_SAVE_SPEC );
    if frmSaveSpec.ShowModal = mrOk then
    begin
      dataElementSelection := TPackagedSelection.Create( fGrid.Data.StudyId, fGridPopulation.ProcId, frmSaveSpec.Title, frmSaveSpec.Comment );
      for collectorName in collectorNameList do
        dataElementSelection.AddCollector( collectorName );
      fPackagedQuickStatGrids.Add( dataElementSelection );
      dataElementSelection.Save( fCrfContext.Database );
      fSelContext.RefreshList( Self );
    end;
  finally
    collectorNameList.Free;
  end;
  GlobalLog.LeaveMethod( Self, PROC_NAME );
end;

procedure TfrmQuickStat.actDeletePackageExecute( Sender: TObject );
var
  packagedSelection: TPackagedSelection;
begin
  with lbPackagedGrids do
  begin
    if ItemIndex = -1 then
      GlobalLog.Event( WARN_NO_PACKAGE_SELECTED, ltWarning )
    else
    begin
      packagedSelection := Items.Objects[ItemIndex] as TPackagedSelection;
      if GlobalLog.LogYesNo( Format( CONFIRM_DELETE_PACKAGE, [packagedSelection.Title] ), ltWarning ) then
      begin
        packagedSelection.Delete( fCrfContext.Database );
        Items.Delete( ItemIndex );
      end;
    end;
  end;
end;

{$ENDREGION}

function TfrmQuickStat.PersonGridIdentification: TPersonIdentification;
begin
  if rbFullIdentification.Checked then
    Result := pgiFull
  else if rbKeepPids.Checked then
    Result := pgiPersonIdOnly
  else if rbRandomisePids.Checked then
    Result := pgiRandomPersonId
  else
    raise EAbort.Create( 'Unhandled identification strategy.' );
end;

end.
