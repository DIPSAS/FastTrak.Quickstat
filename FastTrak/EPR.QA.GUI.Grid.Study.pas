unit EPR.QA.GUI.Grid.Study;

interface

uses
  EPR.QA.GUI.Grid,
  EPR.QA.Matrix,
  EPR.QA.Matrix.Interfaces,
  {General GUI}
  Emetra.VclUtil.Style.Interfaces,
  {General}
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces,
  Emetra.Progress.Interfaces,
  Emetra.Person.Interfaces,
  {Standard}
  VCL.Controls, VCL.StdCtrls,
  VCL.Graphics, VCL.Grids,
  Generics.Collections,
  System.Classes, System.Types;

type
  TStudyOverviewGrid = class( TPersonGrid, IGuiStyleObserver, IDataTarget )
  private
    fLog: ILog;
    fEmptyColorMap: TDictionary<string, TColor>;
    fData: TPersonGridData;
    FGapX: integer;
    FGapY: integer;
    FTallText: string;
    FWideText: string;
    FOnGetColor: TNotifyEvent;
    fFixedFontColor: TColor;
    function SelectEmptyColor( const AVarName: string ): TColor;
  protected
    function Get_StudyId: integer;
    procedure HandleCellDraw( Sender: TObject; ACol, ARow: integer; ARect: TRect; AState: TGridDrawState );
    procedure CMHintShow( var Message: TCMHintShow ); message CM_HINTSHOW;
  public
    { Initialization }
    constructor Create( AOwner: TComponent; APersonList: IPersonList; AProgress: IProgress; ASQL: ISQL; ALog: ILog ); reintroduce;
    procedure BeforeDestruction; override;
    { Other members }
    procedure AddData( const ACollector: IGridDataCollector );
    procedure AddEmptyColor( const AVarName: string; const AColor: TColor );
    procedure Lock;
    procedure SaveToFile( const AFileName: string; const AIdentification: TPersonIdentification; const AIncludeDates: boolean );
    procedure StartPainting;
    procedure UpdateStyle( Sender: IGuiStyle );
    { Properties }
    property FixedFontColor: TColor read fFixedFontColor write fFixedFontColor;
    property Data: TPersonGridData read fData;
    property StudyId: integer read Get_StudyId;
    property OnGetColor: TNotifyEvent read FOnGetColor write FOnGetColor;
    property WideText: string read FWideText write FWideText;
  end;

implementation

uses
  Emetra.VclUtil.ColorCalculator,
  VCL.Forms,
  EPR.QA.Collector.Names,
  {Standard}
  System.SysUtils, Winapi.Windows;

{$REGION 'Initialization' }

procedure TStudyOverviewGrid.CMHintShow( var Message: TCMHintShow );
var
  gridCoord: TGridCoord;
  cellObject: TObject;
  thisText: ICellText;
  thisColumn: IPersonGridColumn;
  hintText: string;
begin
  with message.HintInfo^ do
  begin
    gridCoord := MouseCoord( CursorPos.X, CursorPos.Y );
    if ( gridCoord.Y >= 0 ) and ( gridCoord.X >= 0 ) then
    begin
      // try to get an object behind the cell
      if TryGetObject( gridCoord.X, gridCoord.Y, cellObject ) then
      begin
        if ( gridCoord.Y < FixedRows ) then
        begin
          if Supports( cellObject, IPersonGridColumn, thisColumn ) then
          begin
            case gridCoord.Y of
              0: hintText := Data.Description( thisColumn.VarName );
              1: hintText := thisColumn.Subtitle;
            end;
          end;
        end
        else
        begin
          if Supports( cellObject, ICellText, thisText ) then
            hintText := thisText.CellHint;
        end;
        HintStr := hintText;
        CursorRect := Rect( CursorPos.X, CursorPos.Y, CursorPos.X, CursorPos.Y );
      end;
    end;
  end;
end;

constructor TStudyOverviewGrid.Create( AOwner: TComponent; APersonList: IPersonList; AProgress: IProgress; ASQL: ISQL; ALog: ILog );
begin
  inherited Create( AOwner );
  fLog := ALog;
  fFixedFontColor := clBlack;
  fEmptyColorMap := TDictionary<string, TColor>.Create;
  fData := TPersonGridData.Create( Self, APersonList, AProgress, ASQL, ALog );
  DataColWidth := 64;
  DefaultDrawing := true;
  ColCount := 5;
  FGapX := 3;
  FGapY := 1;
  FTallText := 'Åge';
  FWideText := '28.11.65';
  ShowHint := true;
end;

procedure TStudyOverviewGrid.BeforeDestruction;
begin
  fData.Clear;
  FreeAndNil( fEmptyColorMap );
  FreeAndNil( fData );
  inherited;
end;

{$ENDREGION}

procedure TStudyOverviewGrid.AddData( const ACollector: IGridDataCollector );
begin
  fData.AddData( ACollector );
end;

procedure TStudyOverviewGrid.AddEmptyColor( const AVarName: string; const AColor: TColor );
begin
  fEmptyColorMap.Add( AVarName, AColor );
end;

procedure TStudyOverviewGrid.HandleCellDraw( Sender: TObject; ACol, ARow: integer; ARect: TRect; AState: TGridDrawState );
var
  UI: Cardinal;
  cellObject: TObject;
  thisColor: IBrushColor;
  thisFontColor: IFontColor;
  thisText: ICellText;
  thisColumn: IPersonGridColumn;
  thisVarName: IVarName;
  thisRow: IPersonGridRow;
  cellText: string;
  fontColor: TColor;
  brushColor: TColor;
begin
  Canvas.Font.Assign( Self.Font );
  brushColor := clNone;
  cellText := EmptyStr;
  { Set alignment based on column type }
  if IsTextColumn( ACol ) then
    UI := DT_VCENTER + DT_SINGLELINE + DT_END_ELLIPSIS
  else
    UI := DT_VCENTER + DT_SINGLELINE + DT_RIGHT;

  { Find object for current row }
  if not TryGetObject( ACol, ARow, cellObject ) then
  begin
    brushColor := clWebSnow;
    cellObject := nil;
  end
  else if Supports( cellObject, IBrushColor, thisColor ) then
  begin
    { Custom coloring of object }
    brushColor := thisColor.brushColor;
    if ( ( brushColor = clNone ) or ( brushColor = 0 ) ) and Assigned( FOnGetColor ) then
    begin
      FOnGetColor( cellObject );
      brushColor := thisColor.brushColor;
    end;
  end;

  { Use default brush color if not set }
  if ( brushColor = clNone ) or ( brushColor = 0 ) then
    brushColor := clWhite;

  { Cet column header for fixed columns }
  if ( ARow = 0 ) and ( ACol < FixedCols ) then
    cellText := GetFixedHeader( ACol );

  { Fixed rows }
  if ( ARow < FixedRows ) then
  begin
    if Supports( cellObject, IPersonGridColumn, thisColumn ) then
      case ARow of
        0: cellText := thisColumn.Title;
        1: cellText := thisColumn.Subtitle;
      end;
    if ACol >= FixedCols then
      UI := DT_VCENTER + DT_SINGLELINE + DT_END_ELLIPSIS;
  end
  { Fixed columns }
  else if ( ACol < FixedCols ) then
  begin
    if Supports( cellObject, IPersonGridRow, thisRow ) then
      cellText := GetFixedFields( ACol, thisRow );
  end
  { Standard cells }
  else
  begin
    if Supports( cellObject, ICellText, thisText ) then
    begin
      cellText := thisText.cellText;
      if thisText.AlignLeft then
        UI := DT_VCENTER + DT_SINGLELINE + DT_END_ELLIPSIS;
    end
    else if Supports( cellObject, IVarName, thisVarName ) then
      brushColor := SelectEmptyColor( thisVarName.VarName );
  end;

  { Color mixing }
  if ( ACol = Col ) and ( ARow = CurrentRow ) then
    brushColor := CurrentCellColor
  else if ( gdSelected in AState ) or ( ARow = CurrentRow ) then
    brushColor := TColorCalculator.BlendColors( brushColor, CurrentRowColor, 50 )
  else if ( gdFixed in AState ) then
    brushColor := FixedColor;

  if ACol = 0 then
    fontColor := fFixedFontColor
  else if ( ACol = Col ) and ( ARow = CurrentRow ) then
    fontColor := Font.Color
  else if Supports( cellObject, IFontColor, thisFontColor ) then
    fontColor := thisFontColor.fontColor
  else
    fontColor := Font.Color;
  if ( ARow = 0 ) or ( ARow = CurrentRow ) then
    Canvas.Font.Style := [fsBold];
  Canvas.Font.Color := fontColor;
  Canvas.Brush.Color := brushColor;
  Canvas.FillRect( ARect );
  System.Types.InflateRect( ARect, -FGapX, -FGapY );
  DrawText( Canvas.Handle, pChar( cellText ), Length( cellText ), ARect, UI );
end;

procedure TStudyOverviewGrid.Lock;
const
  PROC_NAME = 'Lock';
begin
  fLog.EnterMethod( Self, PROC_NAME );
  try
    fData.Lock;
    StartPainting;
  finally
    fLog.LeaveMethod( Self, PROC_NAME );
  end;
end;

procedure TStudyOverviewGrid.SaveToFile( const AFileName: string; const AIdentification: TPersonIdentification; const AIncludeDates: boolean );
begin
  fData.SaveToFile( AFileName, AIdentification, AIncludeDates );
end;

function TStudyOverviewGrid.SelectEmptyColor( const AVarName: string ): TColor;
begin
  Result := clNone; { CodeHealer }
  if not fEmptyColorMap.TryGetValue( AVarName, Result ) then
    Result := clWebWhiteSmoke;
end;

procedure TStudyOverviewGrid.StartPainting;
begin
  DefaultDrawing := false;
  OnDrawCell := HandleCellDraw;
  Invalidate;
end;

procedure TStudyOverviewGrid.UpdateStyle( Sender: IGuiStyle );
begin
  CurrentCellColor := clWebOrange;
  CurrentRowColor := TColorCalculator.BlendColors( CurrentCellColor, clWhite, 50 );
  FixedColor := Sender.VeryLightColor;
  Font.Size := Sender.FontSize;
  Font.Name := Sender.FontName;
  Font.Style := [];
  Canvas.Font.Assign( Font );
  Canvas.Font.Style := [fsBold];
  { Recalculate row heights and column widths }
  DefaultRowHeight := Canvas.TextHeight( FTallText ) + FGapY * 2;
  DefaultDobColWidth := Canvas.TextWidth( DateToStr( EncodeDate( 2099, 12, 31 ) ) ) + FGapX * 2 + 2;
  DefaultIdColWidth := Canvas.TextWidth( '19999' ) + FGapX * 2 + 2;
  DefaultNameColWidth := DefaultDobColWidth * 2;
  DefaultNationalIdColWidth := Canvas.TextWidth( '30129933888' ) + FGapX * 2 + 2;
  DefaultColWidth := Canvas.TextWidth( FWideText ) + FGapX * 2;
  SetDefaultWidths( Self );
end;

function TStudyOverviewGrid.Get_StudyId;
begin
  Result := fData.StudyId;
end;

end.
