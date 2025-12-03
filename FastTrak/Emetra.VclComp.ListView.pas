unit Emetra.VclComp.ListView;

{$DEFINE NotDebugListView}

interface

uses
  {GUI}
  Emetra.VclUtil.Style.Interfaces,
  {Service}
  Emetra.Interfaces.Listbox,
  Emetra.Interfaces.Lookup,
  Emetra.Interfaces.List,
  Emetra.Interfaces.Observer,
  {Standard VCL}
  Vcl.Grids,
  Vcl.Controls,
  Vcl.StdCtrls,
  Vcl.Graphics,
  Vcl.Forms,
  Windows, Messages, System.Classes, System.Types, System.Contnrs;

type
  TFilterCase = ( fcNoChange, fcUpper, fcLower );
  TGetUserName = function( const AUserId: integer; out AUserName: string ): boolean of object;
  TOnFilterEvent = function( const AObject: TObject ): boolean of object;

  TSelectionListView = class( TCustomDrawGrid )
  strict private
    FLastChangeNotification: TObject;
    FLastSelection: TObject;
    FOnSelect: TNotifyEvent;
  protected
    FLocalList: TObjectList;
    function Get_SelectedObject: TObject;
    procedure TriggerChangeEvent( AObject: TObject );
  public
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    function TrySelectObject( AObject: TObject ): boolean;
    procedure ClearSelection;
    { Properties }
    property SelectedObject: TObject read Get_SelectedObject;
    property OnSelect: TNotifyEvent read FOnSelect write FOnSelect;
  end;

  TReselectStrategy = ( rsSelectFirst, rsReselect, rsNoChange );

  TObjectListView = class( TSelectionListView, IListener )
  strict private
    FFilter: string;
    function TextColor( const AColor: TColor; const ASelected: boolean ): TColor;
  private
    FBaseStyle: TFontStyles;
    FExternalList: IObjectList;
    FFilterCase: TFilterCase;
    FMaxCodeWidth: integer;
    FLastRow: integer;
    FGapX: integer;
    FGapY: integer;
    FDrawCount: integer;
    FShowAll: boolean;
    FMeasureCount: integer;
    FOnFilter: TOnFilterEvent;
    FSimpleView: boolean;
    FShowDescription: boolean;
    FStatusColumnWidth: integer;
    fBoldCode: boolean;
    FShowStatus: boolean;
    FHighText: string;
    FListSelectedBackground: TColor;
    FListSelectedBackgroundUnfocused: TColor;
    FCodeColor: TColor;
    FStatusTextColor: TColor;
    FTextColor: TColor;
    FFirstInfoColor: TColor;
    FSecondInfoColor: TColor;
    FReselectStrategy: TReselectStrategy;
    fAlternateColor: TColor;
    FListSelectedForeground: TColor;
    procedure SetDefaultItemHeight( const AHeight: integer );
  protected
    { Property accessors }
    function Get_Count: integer;
    procedure Set_ShowAll( const AValue: boolean );
    procedure Set_SimpleView( const AValue: boolean );
    procedure Set_List( AList: IObjectList );
    procedure Set_ShowStatus( const AValue: boolean );
    procedure Set_BoldCode( const Value: boolean );
    procedure Set_GapX( const Value: integer );
    procedure Set_GapY( const Value: integer );
    { Cell painters }
    procedure PaintBackground( AItem: IListboxItem; ARect: TRect; const ASelected, AOtherColor: boolean );
    procedure PaintCode( AItem: IListboxItem; ARect: TRect; const ASelected: boolean );
    procedure PaintColor( AItem: IListboxItem; ARect: TRect );
    procedure PaintContents( AItem: IListboxItem; ARect: TRect; AState: TGridDrawState; const AExpandRow, ASelected: boolean );
    { Default handlers }
    function DoMouseWheelDown( Shift: TShiftState; MousePos: TPoint ): boolean; override;
    function DoMouseWheelUp( Shift: TShiftState; MousePos: TPoint ): boolean; override;
    procedure DoSelectCell( Sender: TObject; ACol, ARow: integer; var ACanSelect: boolean );
    procedure DoDrawCell( Sender: TObject; ACol, ARow: integer; ARect: TRect; State: TGridDrawState );
    procedure DoKeyPress( Sender: TObject; var Key: Char );
    procedure DoResize( Sender: TObject );
    function HeightPadding: integer;
    function ExpandRow( const AIndex: integer ): boolean;
    function MeasureItem( const AIndex: integer; const AExpanded: boolean ): integer;
    procedure AfterUpdate( Sender: TObject );
  public
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    procedure Prepare( AParent: TWinControl; const ALayout: TAlign );
    procedure RefreshView( const ASimple: boolean; const AFilter: string; const AShowAll: boolean );
    procedure WMRButtonDown( var Message: TWMRButtonDown ); message WM_RBUTTONDOWN;
    procedure WMLButtonDown( var Message: TWMLButtonDown ); message WM_LBUTTONDOWN;
    procedure AdjustColumnWidths;
    procedure AdjustRowHeight( const ARow: integer );
    procedure AdjustRowHeights;
    function ObjectAt( const ARow: integer ): TObject;
    function AnythingSelected: boolean;
    property List: IObjectList read FExternalList write Set_List;
{$IFDEF Debug}
    { Counters for debug/performance test }
    property DrawCount: integer read FDrawCount;
    property MeasureCount: integer read FMeasureCount;
{$ENDIF}
    { Appearance }
    property ReselectStrategy: TReselectStrategy read FReselectStrategy write FReselectStrategy;
    property ShowAll: boolean read FShowAll write Set_ShowAll;
    property ShowDescription: boolean read FShowDescription write FShowDescription;
    property SimpleView: boolean read FSimpleView write Set_SimpleView;
    property StatusBoxWidth: integer read FStatusColumnWidth write FStatusColumnWidth;
    { Filter }
    property OnFilter: TOnFilterEvent read FOnFilter write FOnFilter;
    property Filter: string read FFilter;
    { IGuiStyleObserver }
    procedure UpdateStyle( Sender: IGuiStyle );
  published
    { Our methods }
    property GapX: integer read FGapX write Set_GapX;
    property GapY: integer read FGapY write Set_GapY;
    property Count: integer read Get_Count;
    property BoldCode: boolean read fBoldCode write Set_BoldCode;
    property FilterCase: TFilterCase read FFilterCase write FFilterCase;
    property ShowStatus: boolean read FShowStatus write Set_ShowStatus;
    { Arena colors }
    property AlternateColor: TColor read fAlternateColor write fAlternateColor;
    property ListSelectedBackground: TColor read FListSelectedBackground write FListSelectedBackground;
    property ListSelectedForeground: TColor read FListSelectedForeground write FListSelectedForeground;
    property ListSelectedBackgroundUnfocused: TColor read FListSelectedBackgroundUnfocused write FListSelectedBackgroundUnfocused;
    { from TCustomDrawGrid }
    property Font;
    property Color;
    property PopupMenu;
    property OnClick;
    property OnDblClick;
    property BorderStyle;
    property FixedCols;
    property Options;
  end;

implementation

uses
  Emetra.VclUtil.ColorSet.Interfaces,
  SysUtils, Math;

const
  CodeColumn       = 0;
  StatusColumn     = 2;
  TextColumn       = 1;
  StatusFrameColor = clGray;

{$REGION 'Workaround for TWebBrowser bug'}

procedure TObjectListView.WMRButtonDown( var Message: TWMRButtonDown );
begin
  if CanFocus then
  begin
    Windows.SetFocus( Self.Handle );
    DoEnter;
  end;
  inherited;
end;

procedure TObjectListView.WMLButtonDown( var Message: TWMLButtonDown );
begin
  if CanFocus then
  begin
    Windows.SetFocus( Self.Handle );
    DoEnter;
  end;
  inherited;
end;

{$ENDREGION}
{$REGION 'TSelectionListView'}

procedure TSelectionListView.AfterConstruction;
begin
  inherited;
  FLocalList := TObjectList.Create( false );
end;

procedure TSelectionListView.BeforeDestruction;
begin
  FLocalList.Free;
  inherited;
end;

procedure TSelectionListView.ClearSelection;
var
  oldRow: longint;
begin
  if Row >= 0 then
  begin
    oldRow := Row;
    Selection := TGridRect( Rect( Col, -1, Col, -1 ) );
    RowHeights[oldRow] := DefaultRowHeight;
    InvalidateRow( oldRow );
  end;
end;

function TSelectionListView.Get_SelectedObject: TObject;
begin
  if InRange( Row, 0, Pred( FLocalList.Count ) ) then
    Result := FLocalList[Row]
  else
    Result := nil;
  TriggerChangeEvent( Result );
end;

procedure TSelectionListView.TriggerChangeEvent( AObject: TObject );
begin
  if ( AObject <> FLastSelection ) or ( AObject <> FLastChangeNotification ) then
  begin
    FLastSelection := AObject;
    if Assigned( FOnSelect ) then
    begin
      FLastChangeNotification := AObject;
      FOnSelect( AObject );
    end;
  end;
end;

function TSelectionListView.TrySelectObject( AObject: TObject ): boolean;
var
  rowIndex: integer;
begin
  { Look for object }
  rowIndex := FLocalList.IndexOf( AObject );
  Result := ( rowIndex <> -1 );
  { Expand list if needed }
  if ( rowIndex >= RowCount ) then
    RowCount := rowIndex;
  { Move to row }
  if Result and ( rowIndex <> Row ) then
  begin
    Row := rowIndex;
    Assert( SelectedObject = AObject );
  end;
end;

{$ENDREGION}
{$REGION 'Initialization'}

procedure TObjectListView.AfterConstruction;
begin
  inherited;
  FReselectStrategy := rsNoChange;
  FHighText := 'Åge';
  FListSelectedBackground := clFocusedSelectionColor;
  FListSelectedBackgroundUnfocused := clUnfocusedSelectionColor;
  FListSelectedForeground := clNone;
  fAlternateColor := clNone;
  FCodeColor := clCodeColor;
  FStatusTextColor := clStatusTextColor;
  FFirstInfoColor := clFirstInfoColor;
  FSecondInfoColor := clSecondInfoColor;
  FTextColor := clTextColor;
  DoubleBuffered := true;
  DefaultDrawing := false;
  FShowDescription := true;
  FSimpleView := true;
  FMaxCodeWidth := 24;
  FStatusColumnWidth := -1;
  FShowAll := true;
  FixedRows := 0;
  FixedCols := 0;
  RowCount := 1;
  ColCount := 3;
  ScrollBars := ssVertical;
  SetDefaultItemHeight( 17 );
  FGapX := 4;
  FGapY := 3;
  Options := Options + [goRowSelect, goHorzLine, goRowMoving, goRangeSelect] - [goFixedVertLine, goFixedHorzLine, goVertLine];
  Self.OnResize := Self.DoResize;
  Self.OnKeyPress := Self.DoKeyPress;
end;

procedure TObjectListView.BeforeDestruction;
var
  thisObservable: IObservable;
begin
  if Assigned( FExternalList ) and Supports( FExternalList, IObservable, thisObservable ) then
    thisObservable.Detach( Self );
  inherited;
end;

procedure TObjectListView.Prepare( AParent: TWinControl; const ALayout: TAlign );
begin
  Parent := AParent;
  BorderStyle := bsNone;
  Align := ALayout;
end;

{$ENDREGION}

procedure TObjectListView.AdjustColumnWidths;
var
  restWidth: integer;
begin
  ColWidths[CodeColumn] := FMaxCodeWidth + 2 * FGapX;
  if FShowStatus then
    ColWidths[StatusColumn] := FStatusColumnWidth + FGapX * 2
  else
    ColWidths[StatusColumn] := 0;
  restWidth := ClientWidth - ColWidths[StatusColumn] - ColWidths[CodeColumn];
  if goVertLine in Options then
    restWidth := restWidth - ColCount;
  ColWidths[TextColumn] := restWidth;
end;

procedure TObjectListView.DoSelectCell( Sender: TObject; ACol, ARow: integer; var ACanSelect: boolean );
begin
  ACanSelect := ARow < FLocalList.Count;
  if not ACanSelect then
    exit;
  { Contract previous row }
  if ( FLastRow > -1 ) and ( FLastRow < RowCount ) then
  begin
    if FSimpleView then
      RowHeights[FLastRow] := DefaultRowHeight;
  end;
  FLastRow := ARow;

  { Current line should always be expanded }
  RowHeights[ARow] := MeasureItem( ARow, true );
end;

procedure TObjectListView.RefreshView( const ASimple: boolean; const AFilter: string; const AShowAll: boolean );
begin
  case FFilterCase of
    fcUpper: FFilter := AnsiUppercase( AFilter );
    fcLower: FFilter := AnsiLowercase( AFilter );
  else FFilter := AFilter;
  end;
  FShowAll := AShowAll;
  SimpleView := ASimple;
  AfterUpdate( Self );
end;

function TObjectListView.Get_Count: integer;
begin
  Result := FLocalList.Count;
end;

function TObjectListView.HeightPadding: integer;
begin
  Result := ( 2 * FGapY );
end;

procedure TObjectListView.Set_ShowStatus( const AValue: boolean );
begin
  if AValue = FShowStatus then
    exit;
  FShowStatus := AValue;
  AdjustColumnWidths;
end;

procedure TObjectListView.Set_ShowAll( const AValue: boolean );
begin
  if AValue = FShowAll then
    exit;
  FShowAll := AValue;
  RefreshView( FSimpleView, '', FShowAll );
end;

procedure TObjectListView.Set_SimpleView( const AValue: boolean );
var
  n: integer;
begin
  if FSimpleView = AValue then
    exit;
  FSimpleView := AValue;
  n := 0;
  while n < FLocalList.Count do
  begin
    if ExpandRow( n ) then
      RowHeights[n] := MeasureItem( n, true )
    else
      RowHeights[n] := DefaultRowHeight;
    inc( n );
  end;
end;

function TObjectListView.TextColor( const AColor: TColor; const ASelected: boolean ): TColor;
begin
  if ( FListSelectedForeground <> clNone ) and ( ASelected ) then
    Result := FListSelectedForeground
  else
    Result := AColor;
end;

procedure TObjectListView.SetDefaultItemHeight( const AHeight: integer );
begin
  if AHeight <> DefaultRowHeight then
    DefaultRowHeight := AHeight;
end;

procedure TObjectListView.Set_BoldCode( const Value: boolean );
begin
  if Value = fBoldCode then
    exit;
  fBoldCode := Value;
  Invalidate;
end;

procedure TObjectListView.Set_GapX( const Value: integer );
begin
  if Value <> FGapX then
  begin
    FGapX := Value;
    AdjustColumnWidths;
  end;
end;

procedure TObjectListView.Set_GapY( const Value: integer );
begin
  if Value <> FGapY then
  begin
    FGapY := Value;
    AdjustColumnWidths;
    AdjustRowHeights;
  end;
end;

procedure TObjectListView.Set_List( AList: IObjectList );
var
  thisObservable: IObservable;
begin
  if AList <> FExternalList then
  begin
    DrawingStyle := gdsClassic;
    if Assigned( AList ) then
    begin
      if Supports( AList, IObservable, thisObservable ) then
        thisObservable.Attach( Self )
      else
        raise Exception.CreateFmt( '%s: External list must support IObservable: %s', [Self.ClassName, TObject( AList ).ClassName] );
      FExternalList := AList;
      Self.OnSelectCell := DoSelectCell;
      Self.OnDrawCell := DoDrawCell;
      Self.DefaultDrawing := false;
    end
    else
    begin
      if Supports( FExternalList, IObservable, thisObservable ) then
        thisObservable.Detach( Self );
      FExternalList := AList;
      Self.OnSelectCell := nil;
      Self.OnDrawCell := nil;
      Self.DefaultDrawing := true;
    end;
    AdjustColumnWidths;
    AfterUpdate( Self );
  end;
end;

procedure TObjectListView.AfterUpdate( Sender: TObject );
var
  n: integer;
  thisBase: IListBoxBase;
  thisMatchable: IMatchable;
  textToMatch: string;
  savedObject: TObject;
  thisObject: TObject;
  localIndex: integer;
  addThis: boolean;
begin
  savedObject := SelectedObject;
  FLocalList.Clear;
  if Assigned( FExternalList ) then
  begin
    n := 0;
    while n < FExternalList.Count do
    begin
      thisObject := FExternalList[n];
      localIndex := FLocalList.IndexOf( thisObject );
      if localIndex <> -1 then
        raise Exception.CreateFmt( 'External %d already found at %d', [n, localIndex] )
      else if Supports( thisObject, IListBoxBase, thisBase ) then
      begin
        addThis := false;
        if thisBase.IsCurrent or FShowAll then
        begin
          if Supports( thisObject, IMatchable, thisMatchable ) then
            addThis := thisMatchable.Match( FFilter )
          else
          begin
            textToMatch := AnsiLowercase( thisBase.AsListBox( false ) );
            if ( FFilter = '' ) or ( Pos( FFilter, textToMatch ) > 0 ) then
              addThis := true;
          end;
          if addThis and Assigned( FOnFilter ) then
            addThis := FOnFilter( thisObject );
          if addThis then
            FLocalList.Add( TObject( thisBase ) );
        end;
      end;
      inc( n );
    end;
  end;
  RowCount := FLocalList.Count;
  Visible := FLocalList.Count > 0;
  AdjustRowHeights;

  if ReselectStrategy = rsSelectFirst then
    TrySelectObject( FLocalList.First )
  else if ReselectStrategy = rsReselect then
    TrySelectObject( savedObject );

  Invalidate;
end;

function TObjectListView.MeasureItem( const AIndex: integer; const AExpanded: boolean ): integer;
var
  singleLineHeight: integer;
  thisItem: IListboxItem;
  thisDetail: IListBoxDetails;
  descRect: TRect;
begin
  Result := DefaultRowHeight;
  if AExpanded and ( AIndex < FLocalList.Count ) and Supports( FLocalList[AIndex], IListboxItem, thisItem ) then
  begin
    Canvas.Font.Assign( Self.Font );
    inc( FMeasureCount );
    singleLineHeight := Canvas.TextHeight( FHighText );
    if singleLineHeight > DefaultRowHeight - HeightPadding then
      DefaultRowHeight := singleLineHeight + HeightPadding;
    Result := singleLineHeight;
    if FShowDescription then
    begin
      if thisItem.Description <> '' then
      begin
        descRect.Top := 0;
        descRect.Left := 0;
        descRect.Right := ColWidths[TextColumn] - 2 * FGapX;
        DrawText( Canvas.Handle, thisItem.Description, Length( thisItem.Description ), descRect, DT_CALCRECT + DT_WORDBREAK );
        Result := Result + ( descRect.Bottom - descRect.Top );
      end;
    end;
    if Supports( FLocalList[AIndex], IListBoxDetails, thisDetail ) then
    begin
      if thisDetail.GreenText <> EmptyStr then
        inc( Result, singleLineHeight );
      if thisDetail.BlueText <> EmptyStr then
        inc( Result, singleLineHeight );
    end;
    inc( Result, HeightPadding );
  end;
end;

function TObjectListView.ObjectAt( const ARow: integer ): TObject;
begin
  Result := FLocalList[ARow];
end;

function TObjectListView.AnythingSelected: boolean;
begin
  Result := ( SelectedObject <> nil );
end;

procedure TObjectListView.AdjustRowHeight( const ARow: integer );
begin
  RowHeights[ARow] := MeasureItem( ARow, ExpandRow( ARow ) );
end;

procedure TObjectListView.AdjustRowHeights;
var
  rowNo: integer;
begin
  Canvas.Font.Assign( Self.Font );
  SetDefaultItemHeight( Canvas.TextHeight( FHighText ) + HeightPadding );
  if not FSimpleView then
  begin
    rowNo := FLocalList.Count - 1;
    while rowNo >= 0 do
    begin
      AdjustRowHeight( rowNo );
      dec( rowNo );
    end;
  end;
end;

procedure TObjectListView.PaintBackground( AItem: IListboxItem; ARect: TRect; const ASelected, AOtherColor: boolean );
var
  thisBackground: IListBoxBackgroundColor;
begin
  if ASelected then
  begin
    if Focused then
      Canvas.Brush.Color := FListSelectedBackground
    else
      Canvas.Brush.Color := FListSelectedBackgroundUnfocused
  end
  else if Supports( AItem, IListBoxBackgroundColor, thisBackground ) and ( thisBackground.Color <> clNone ) then
    Canvas.Brush.Color := thisBackground.Color
  else if ( AOtherColor ) and ( fAlternateColor <> clNone ) then
    Canvas.Brush.Color := fAlternateColor
  else
    Canvas.Brush.Color := Color;
  if not Enabled then
    Canvas.Brush.Color := FListSelectedBackgroundUnfocused;
  Canvas.Brush.Style := bsSolid;
  Canvas.FillRect( ARect );
  Canvas.Brush.Style := bsClear;
end;

procedure TObjectListView.PaintCode( AItem: IListboxItem; ARect: TRect; const ASelected: boolean );
begin
  with Canvas do
  begin
    if fBoldCode then
      Font.Style := FBaseStyle + [fsBold]
    else
      Font.Style := FBaseStyle;
    Font.Color := TextColor( FCodeColor, ASelected );
    TextOut( ARect.Left, ARect.Top, AItem.V );
    FMaxCodeWidth := Max( TextWidth( AItem.V ), FMaxCodeWidth );
    AdjustColumnWidths;
    exit;
    MoveTo( ARect.Left - FGapX, ARect.Bottom + FGapY );
    LineTo( ARect.Right + FGapX + 1, ARect.Bottom + FGapY );
  end;
end;

procedure TObjectListView.PaintColor( AItem: IListboxItem; ARect: TRect );
var
  thisColor: IListBoxStatusColor;
begin
  Canvas.Pen.Color := StatusFrameColor;
  Canvas.Brush.Style := bsSolid;
  if Supports( AItem, IListBoxStatusColor, thisColor ) and ( thisColor.StatusColor <> clNone ) then
    Canvas.Brush.Color := thisColor.StatusColor;
  Canvas.Rectangle( ARect );
end;

procedure TObjectListView.PaintContents( AItem: IListboxItem; ARect: TRect; AState: TGridDrawState; const AExpandRow, ASelected: boolean );
var
  thisDetail: IListBoxDetails;
  statusText: string;
  offset: integer;
  statusWidth: integer;
begin
  { Draw Status text }
  Canvas.Font.Style := FBaseStyle;
  Canvas.Font.Color := TextColor( FStatusTextColor, ASelected );
  statusText := AItem.OT;
  if statusText = EmptyStr then
    statusWidth := 0
  else
  begin
    Canvas.Font.Size := Canvas.Font.Size - 1;
    DrawText( Canvas.Handle, statusText, Length( statusText ), ARect, DT_RIGHT );
    statusWidth := Canvas.TextWidth( statusText );
    Canvas.Font.Size := Canvas.Font.Size + 1;
  end;
  { Draw Main text }
  Canvas.Font.Color := TextColor( FTextColor, ASelected );
  Canvas.Font.Style := FBaseStyle + [fsBold];
  ARect.Right := ARect.Right - statusWidth - FGapX;
  offset := DrawText( Canvas.Handle, AItem.DN, Length( AItem.DN ), ARect, DT_SINGLELINE + DT_END_ELLIPSIS );
  ARect.Right := ARect.Right + statusWidth;
  if AExpandRow then
  begin
    Canvas.Font.Style := [];
    { Paint description }
    if FShowDescription then
    begin
      Canvas.Font.Color := TextColor( FTextColor, ASelected );
      ARect.Top := ARect.Top + offset;
      if ( AItem.Description <> '' ) then
        offset := DrawText( Canvas.Handle, AItem.Description, Length( AItem.Description ), ARect, DT_WORDBREAK )
      else
        offset := 0;
    end;
    { Paint creation and signature }
    if Supports( AItem, IListBoxDetails, thisDetail ) then
    begin
      ARect.Top := ARect.Top + offset;
      Canvas.Font.Color := TextColor( FFirstInfoColor, ASelected );
      offset := DrawText( Canvas.Handle, thisDetail.BlueText, Length( thisDetail.BlueText ), ARect, DT_LEFT );
      ARect.Top := ARect.Top + offset;
      Canvas.Font.Color := TextColor( FSecondInfoColor, ASelected );
      DrawText( Canvas.Handle, thisDetail.GreenText, Length( thisDetail.GreenText ), ARect, DT_LEFT );
    end;
  end;
end;

procedure TObjectListView.DoDrawCell( Sender: TObject; ACol, ARow: integer; ARect: TRect; State: TGridDrawState );
var
  thisObject: TObject;
  thisItem: IListboxItem;
  thisStrikeout: IListBoxStrikeout;
  bValidItem: boolean;
begin
  if ARow >= FLocalList.Count then
    RowCount := FLocalList.Count
  else
    try
      Canvas.Font.Assign( Font );
      thisObject := FLocalList[ARow];
      if ARow = Row then
        Assert( thisObject = SelectedObject );
      bValidItem := Supports( thisObject, IListboxItem, thisItem );
      PaintBackground( thisItem, ARect, ARow = Row, ARow mod 2 = 0 );
      inc( FDrawCount );
      if RowCount <> FLocalList.Count then
        RowCount := Max( FLocalList.Count, 1 );
      InflateRect( ARect, -FGapX, -FGapY );
      if bValidItem then
      begin
        if Supports( thisItem, IListBoxStrikeout, thisStrikeout ) and thisStrikeout.Strikeout then
          FBaseStyle := [fsStrikeout]
        else
          FBaseStyle := [];
        case ACol of
          CodeColumn: PaintCode( thisItem, ARect, ARow = Row );
          TextColumn: PaintContents( thisItem, ARect, State, ExpandRow( ARow ), ARow = Row );
          StatusColumn: PaintColor( thisItem, ARect );
        end;
      end;
    except
      on Exception do
      begin
        Canvas.Brush.Color := clWebMistyRose;
        Canvas.FillRect( ARect )
      end;
    end;
end;

function TObjectListView.ExpandRow( const AIndex: integer ): boolean;
begin
  Result := ( not FSimpleView ) or ( ( AIndex = Row ) );
end;

procedure TObjectListView.DoResize( Sender: TObject );
begin
  AdjustColumnWidths;
end;

procedure TObjectListView.DoKeyPress( Sender: TObject; var Key: Char );
begin
  if Key = #13 then
    DblClick
end;

procedure TObjectListView.UpdateStyle( Sender: IGuiStyle );
begin
  FixedColor := Sender.VeryLightColor;
  FListSelectedBackground := Sender.FocusedSelectionColor;
  FListSelectedBackgroundUnfocused := Sender.UnfocusedSelectionColor;
  FTextColor := Sender.TextColor;
  FCodeColor := Sender.CodeColor;
  FFirstInfoColor := Sender.FirstInfoColor;
  FSecondInfoColor := Sender.SecondInfoColor;
  FStatusTextColor := Sender.StatusTextColor;
  Font.Size := Sender.FontSize;
  Font.Name := Sender.FontName;
  AdjustRowHeights;
  Invalidate;
end;

function TObjectListView.DoMouseWheelDown( Shift: TShiftState; MousePos: TPoint ): boolean;
begin
  if PtInRect( ClientRect, ScreenToClient( MousePos ) ) then
  begin
    Result := inherited DoMouseWheelDown( Shift, MousePos );
  end
  else
    Result := false;
end;

function TObjectListView.DoMouseWheelUp( Shift: TShiftState; MousePos: TPoint ): boolean;
begin
  if PtInRect( ClientRect, ScreenToClient( MousePos ) ) then
  begin
    Result := inherited DoMouseWheelUp( Shift, MousePos );
  end
  else
    Result := false;
end;

end.
