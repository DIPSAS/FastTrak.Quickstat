{$D+}
unit Emetra.VclUtil.Listbox;

interface

uses
  Emetra.VclUtil.ColorSet.Interfaces,
  {General}
  Emetra.Classes.Tokenizer,
  {Standard}
  Dialogs, Generics.Collections,
  CheckLst, Contnrs, Controls, Classes, Math, Graphics, Windows, StdCtrls, SysUtils, Types;

type
  TItemPaintStage = ( ipsMeasureItem, ipsPaintItem );

  TOnFillEvent = procedure( Sender: TWinControl; ACanvas: TCanvas; const ASelected: boolean; var ARect: TRect; AIndex: integer ) of object;

  TListControlSettings = class( TObject )
  private
    FCanvas: TCanvas;
    FCodeWidth: integer;
    FControl: TCustomListControl;
    FItemHeights: array of integer;
    fItemSplitter: TTokenizer;
    FLastWidth: integer;
    FMaxHeight: integer;
    FShowCodes: boolean;
    FShowDividers: boolean;
    function GetFont: TFont;
  public
    { Initialization }
    constructor Create( AControl: TCustomListControl; const AShowCodes, AShowDividers: boolean );
    destructor Destroy; override;
    { Other members }
    function GetItemText( const AItemIndex: integer ): string;
    function GetItemObject( const AItemIndex: integer ): TObject;
    function TotalHeight: integer;
    function WidthChanged: boolean;
    procedure ClearCachedHeights;
    procedure SetCachedItemHeight( const AItemIndex, AHeight: integer );
    procedure ExtractDataElements( const AItemIndex: integer; out ACode, AHeader, AInfo, ARed, ABlue, AGreen: string );
    { Properties }
    property Canvas: TCanvas read FCanvas;
    property CodeWidth: integer read FCodeWidth write FCodeWidth;
    property Control: TCustomListControl read FControl;
    property Font: TFont read GetFont;
    property LastWidth: integer read FLastWidth write FLastWidth;
    property MaxHeight: integer read FMaxHeight;
    property ShowCodes: boolean read FShowCodes;
    property ShowDividers: boolean read FShowDividers;
  end;

  TControlDictionary = class( TObjectDictionary<TWinControl, TListControlSettings> );

  TCustomListControlPainter = class( TComponent )
  private
    FControls: TControlDictionary;
    FOnFill: TOnFillEvent;
    fDividerColor: TColor;
    procedure DrawItem( AControl: TWinControl; AIndex: integer; ARect: TRect; AState: TOwnerDrawState );
    procedure MeasureItem( AControl: TWinControl; Index: integer; var AHeight: integer );
    function PaintOrMeasure( AControl: TCustomListControl; const Index: integer; ARect: TRect; const AItemOperation: TItemPaintStage;
      const AState: TOwnerDrawState ): integer;
    procedure SetBackgroundColor( ASettings: TListControlSettings; AIndex: integer; AState: TOwnerDrawState );
  protected
    function UseColorSet: boolean;
  public
    { Initialization }
    constructor Create( AOwner: TComponent ); override;
    destructor Destroy; override;
    { Other members }
    class var ColorSet: IGuiListBoxColorSet;
    procedure ClearHeights( AControl: TCustomListControl );
    procedure Detach( AControl: TCustomListControl );
    procedure Attach( AControl: TCustomListControl; const ADrawCode: boolean = true; const ABorder: boolean = true ); overload;
    procedure RepopulateFromFilteredList( AListBox: TCustomListBox; AObjects: TList; const ASimple: boolean; const AFilterText: string;
      const AShowAll: boolean ); overload;
    procedure RepopulateFromList( AListBox: TCustomListBox; AObjects: TList );
    procedure SetFixedHeight( AControl: TCustomListControl );
    { Properties }
    property OnFill: TOnFillEvent read FOnFill write FOnFill;
    property DividerColor: TColor read fDividerColor;
  end;

resourcestring
  TXT_LISTBOX_COUNT = 'n = %d';
  TXT_LISTBOX_COUNT_OF_TOTAL = 'n = %d av %d';
  ERROR_ITEM_MISSING = '%s.%s(%s): Mangler';
  EXC_COUNT_MISMATCH = '%s.Count: Antall Indeks<>Liste, Liste=%d, Indeks=%d';

implementation

uses
  Emetra.Interfaces.Listbox,
  {Standard}
  System.UITypes;

resourcestring
  TALL_TEXT = 'Åge';

const
  TAB            = #9;
  DEFAULT_HEIGHT = 19;

{$REGION 'TControlInfo' }

constructor TListControlSettings.Create( AControl: TCustomListControl; const AShowCodes, AShowDividers: boolean );
begin
  inherited Create;
  fItemSplitter := TTokenizer.Create;
  FControl := AControl;
  FShowCodes := AShowCodes;
  if FShowCodes then
    FCodeWidth := 8
  else
    FCodeWidth := 0;
  FShowDividers := AShowDividers;
  if FControl is TListBox then
  begin
    FCanvas := TListBox( AControl ).Canvas;
    FCanvas.Font := TListBox( AControl ).Font;
  end
  else if FControl is TComboBox then
  begin
    FCanvas := TComboBox( AControl ).Canvas;
    FCanvas.Font := TComboBox( AControl ).Font;
  end
  else if FControl is TCheckListBox then
  begin
    FCanvas := TCheckListBox( AControl ).Canvas;
    FCanvas.Font := TCheckListBox( AControl ).Font;
  end
  else
    raise EArgumentException.CreateFmt( '%s.Create(%s): Class not supported.', [ClassName, FControl.ClassName] );
end;

destructor TListControlSettings.Destroy;
begin
  fItemSplitter.Free;
  inherited;
end;

function TListControlSettings.GetFont: TFont;
begin
  if FControl.InheritsFrom( TListBox ) then
    Result := TListBox( FControl ).Font
  else if FControl.InheritsFrom( TComboBox ) then
    Result := TComboBox( FControl ).Font
  else if FControl.InheritsFrom( TCheckListBox ) then
    Result := TCheckListBox( FControl ).Font
  else
    raise EArgumentException.CreateFmt( '%s.GetFont: Class %s not supported.', [ClassName, FControl.ClassName] );
end;

function TListControlSettings.GetItemText( const AItemIndex: integer ): string;
var
  controlItems: TStrings;
begin
  if FControl.InheritsFrom( TListBox ) then
    controlItems := TListBox( FControl ).Items
  else if FControl.InheritsFrom( TComboBox ) then
    controlItems := TComboBox( FControl ).Items
  else if FControl.InheritsFrom( TCheckListBox ) then
    controlItems := TCheckListBox( FControl ).Items
  else
    raise EArgumentException.CreateFmt( '%s.GetItemText(%d): Class %s not supported.', [ClassName, AItemIndex, FControl.ClassName] );
  Result := controlItems[AItemIndex];
end;

function TListControlSettings.GetItemObject( const AItemIndex: integer ): TObject;
var
  controlItems: TStrings;
begin
  if FControl.InheritsFrom( TListBox ) then
    controlItems := TListBox( FControl ).Items
  else if FControl.InheritsFrom( TComboBox ) then
    controlItems := TComboBox( FControl ).Items
  else if FControl.InheritsFrom( TCheckListBox ) then
    controlItems := TCheckListBox( FControl ).Items
  else
    raise EArgumentException.CreateFmt( '%s.GetItemObject(%d): Class %s not supported.', [ClassName, AItemIndex, FControl.ClassName] );
  Result := controlItems.Objects[AItemIndex];
end;

function TListControlSettings.TotalHeight: integer;
var
  n: integer;
begin
  Result := 0;
  n := 0;
  while n < Length( FItemHeights ) do
  begin
    Result := Result + FItemHeights[n];
    inc( n );
  end;
  if Result = 0 then
    Result := FControl.GetCount * DEFAULT_HEIGHT;
end;

function TListControlSettings.WidthChanged: boolean;
begin
  Result := ( FLastWidth <> FControl.ClientWidth );
  if not Result then
    exit;
  FLastWidth := FControl.ClientWidth;
  FControl.Invalidate;
  Result := true;
end;

procedure TListControlSettings.ClearCachedHeights;
begin
  FMaxHeight := DEFAULT_HEIGHT;
  SetLength( FItemHeights, 0 );
end;

procedure TListControlSettings.SetCachedItemHeight( const AItemIndex, AHeight: integer );
begin
  if AItemIndex < 0 then
    exit
  else if AItemIndex + 1 > Length( FItemHeights ) then
    SetLength( FItemHeights, AItemIndex + 1 );
  FItemHeights[AItemIndex] := AHeight;
  FMaxHeight := Max( AHeight, FMaxHeight );
end;

procedure TListControlSettings.ExtractDataElements( const AItemIndex: integer; out ACode, AHeader, AInfo, ARed, ABlue, AGreen: string );
var
  strLine: string;
begin
  strLine := GetItemText( AItemIndex );
  ACode := fItemSplitter.Extract( strLine, 0, TAB );
  AHeader := fItemSplitter.Extract( strLine, 1, TAB );
  AInfo := fItemSplitter.Extract( strLine, 2, TAB );
  ARed := fItemSplitter.Extract( strLine, 3, TAB );
  ABlue := fItemSplitter.Extract( strLine, 4, TAB );
  AGreen := fItemSplitter.Extract( strLine, 5, TAB );
end;

{$ENDREGION}
{ TListBoxPainter }

{$REGION 'Initialize'}

constructor TCustomListControlPainter.Create( AOwner: TComponent );
begin
  inherited Create( AOwner );
  FControls := TControlDictionary.Create( [doOwnsValues] );
  fDividerColor := clBtnFace;
end;

destructor TCustomListControlPainter.Destroy;
begin
  FControls.Free;
  inherited;
end;

procedure TCustomListControlPainter.Attach( AControl: TCustomListControl; const ADrawCode: boolean = true; const ABorder: boolean = true );
begin
  Assert( Assigned( AControl ) );
  FControls.Add( AControl, TListControlSettings.Create( AControl, ADrawCode, ABorder ) );
  { Attach event handlers and set to owner-draw styles }
  if AControl is TListBox then
    with TListBox( AControl ) do
    begin
      ParentFont := true;
      Style := lbOwnerDrawVariable;
      OnMeasureItem := MeasureItem;
      OnDrawItem := DrawItem;
      DoubleBuffered := true;
    end
  else if AControl is TComboBox then
    with TComboBox( AControl ) do
    begin
      ParentFont := true;
      Style := csOwnerDrawVariable;
      OnMeasureItem := MeasureItem;
      OnDrawItem := DrawItem;
      DoubleBuffered := true;
    end
  else if AControl is TCheckListBox then
    with TCheckListBox( AControl ) do
    begin
      ParentFont := true;
      Style := lbOwnerDrawVariable;
      OnMeasureItem := MeasureItem;
      OnDrawItem := DrawItem;
      DoubleBuffered := true;
    end
end;

procedure TCustomListControlPainter.Detach( AControl: TCustomListControl );
begin
  Assert( Assigned( AControl ) );
  { Detach event handlers, and set to normal default styles }
  if AControl is TListBox then
    with TListBox( AControl ) do
    begin
      Style := lbStandard;
      OnMeasureItem := nil;
      OnDrawItem := nil;
    end
  else if AControl is TComboBox then
    with TComboBox( AControl ) do
    begin
      Style := csDropDownList;
      OnMeasureItem := nil;
      OnDrawItem := nil;
    end
  else if AControl is TCheckListBox then
    with TCheckListBox( AControl ) do
    begin
      Style := lbStandard;
      OnMeasureItem := nil;
      OnDrawItem := nil;
    end;
  FControls.Remove( AControl );
end;

{$ENDREGION}
{$REGION 'Painting'}

function TCustomListControlPainter.PaintOrMeasure( AControl: TCustomListControl; const Index: integer; ARect: TRect; const AItemOperation: TItemPaintStage;
  const AState: TOwnerDrawState ): integer;
var
  itemHeader, itemDescripton, itemCode, itemStatus, itemInfoFirst, itemInfoSecond: string;
  iCalcRect: integer; { Used to turn on and off CalcRect }
  savedFontSize: integer;
  savedFontColor: TColor;
  rctOriginal: TRect;
  paintSettings: TListControlSettings;
  thisCanvas: TCanvas;
  maroonWidth: integer;
  procedure SetColor( AColor: TColor );
  begin
    if AControl.Enabled then
      thisCanvas.Font.Color := AColor
    else
      thisCanvas.Font.Color := clGrayText;
  end;

begin
  Result := 0;
  if not FControls.TryGetValue( AControl, paintSettings ) then
    exit;
  thisCanvas := paintSettings.Canvas;
  thisCanvas.Font := paintSettings.Font;
  savedFontSize := thisCanvas.Font.Size;
  savedFontColor := thisCanvas.Font.Color;
  with thisCanvas do
    try
      rctOriginal := ARect;
      iCalcRect := 1 - ORD( AItemOperation );
      paintSettings.ExtractDataElements( Index, itemCode, itemHeader, itemDescripton, itemStatus, itemInfoFirst, itemInfoSecond );
      Font.Style := [fsBold];
      if itemHeader <> EmptyStr then
        Result := TextHeight( itemHeader )
      else
        Result := 0;
      if paintSettings.ShowCodes then
      begin
        { Measure with of code }
        Font.Style := [];
        paintSettings.CodeWidth := Max( paintSettings.CodeWidth, TextWidth( itemCode ) );
      end;
      if AItemOperation = ipsPaintItem then
      begin
        { Draw code }
        if ( paintSettings.ShowCodes ) then
        begin
          if UseColorSet then
            SetColor( ColorSet.CodeColor )
          else
            SetColor( clCodeColor );
          Font.Style := [];
          DrawText( Handle, pChar( itemCode ), Length( itemCode ), ARect, 0 );
          ARect.Left := ARect.Left + paintSettings.CodeWidth + 4;
        end;
        { Draw StatusText }
        if UseColorSet then
          SetColor( ColorSet.StatusTextColor )
        else
          SetColor( clStatusTextColor );
        Font.Style := [];
        Font.Size := savedFontSize - 1;
        maroonWidth := TextWidth( itemStatus );
        if maroonWidth > 0 then
          DrawText( Handle, pChar( itemStatus ), Length( itemStatus ), ARect, DT_RIGHT );
        if UseColorSet then
          SetColor( ColorSet.TextColor )
        else
          SetColor( clTextColor );
        Font.Size := savedFontSize;
        { Draw ItemHeader }
        if itemHeader <> EmptyStr then
        begin
          ARect.Right := ARect.Right - maroonWidth;
          Font.Style := [fsBold];
          DrawText( Handle, pChar( itemHeader ), Length( itemHeader ), ARect, DT_END_ELLIPSIS );
          ARect.Right := ARect.Right + maroonWidth;
          { Move down a bit }
          ARect.Top := ARect.Top + Result;
        end;
      end;
      if UseColorSet then
        SetColor( ColorSet.TextColor )
      else
        SetColor( clTextColor );
      Font.Style := [];
      { Draw ItemDescription }
      if itemDescripton <> '' then
      begin
        Result := Result + DrawText( Handle, pChar( itemDescripton ), Length( itemDescripton ), ARect, DT_WORDBREAK + iCalcRect * DT_CALCRECT );
      end;
      if itemInfoSecond <> '' then
      begin
        { Reset rectangle bottom and right }
        ARect.Right := rctOriginal.Right;
        ARect.Top := rctOriginal.Top + Result;
        ARect.Bottom := rctOriginal.Bottom;
        if UseColorSet then
          SetColor( ColorSet.FirstInfoColor )
        else
          SetColor( clFirstInfoColor );
        Result := Result + DrawText( Handle, pChar( itemInfoSecond ), Length( itemInfoSecond ), ARect, DT_SINGLELINE + DT_END_ELLIPSIS + iCalcRect *
          DT_CALCRECT );
      end;
      if itemInfoFirst <> '' then
      begin
        { Reset rectangle bottom and right }
        ARect.Right := rctOriginal.Right;
        ARect.Top := rctOriginal.Top + Result;
        ARect.Bottom := rctOriginal.Bottom;
        if UseColorSet then
          SetColor( ColorSet.SecondInfoColor )
        else
          SetColor( clSecondInfoColor );
        Result := Result + DrawText( Handle, pChar( itemInfoFirst ), Length( itemInfoFirst ), ARect, DT_SINGLELINE + DT_END_ELLIPSIS + iCalcRect *
          DT_CALCRECT );
      end;
      Result := Max( Result, TextHeight( TALL_TEXT ) );
      if paintSettings.ShowDividers then
        Result := Result + 2
      else
        Result := Result + 1;
      paintSettings.WidthChanged;
      if AItemOperation = ipsMeasureItem then
        paintSettings.SetCachedItemHeight( Index, Result );
    finally
      Font.Size := savedFontSize;
      Font.Color := savedFontColor;
    end;
end;

procedure TCustomListControlPainter.SetBackgroundColor( ASettings: TListControlSettings; AIndex: integer; AState: TOwnerDrawState );
var
  backColor: IListBoxBackgroundColor;
begin
  ASettings.Canvas.Brush.Style := bsSolid;
  if odSelected in AState then
  begin
    if UseColorSet then
    begin
      if ASettings.Control.Focused then
        ASettings.Canvas.Brush.Color := ColorSet.FocusedSelectionColor
      else
        ASettings.Canvas.Brush.Color := ColorSet.UnfocusedSelectionColor;
    end
    else if ASettings.Control.Focused then
      ASettings.Canvas.Brush.Color := clFocusedSelectionColor
    else
      ASettings.Canvas.Brush.Color := clUnfocusedSelectionColor;
  end
  else
  begin
    { Check for custom color unselected object }
    if Supports( ASettings.GetItemObject( AIndex ), IListBoxBackgroundColor, backColor ) and ( backColor.Color <> clNone ) then
      ASettings.Canvas.Brush.Color := backColor.Color
    else
      ASettings.Canvas.Brush.Color := ASettings.Control.Brush.Color;
  end;
end;

procedure TCustomListControlPainter.DrawItem( AControl: TWinControl; AIndex: integer; ARect: TRect; AState: TOwnerDrawState );
var
  paintSettings: TListControlSettings;
  savedRect: TRect;
begin
  if FControls.TryGetValue( AControl, paintSettings ) then
    with paintSettings.Canvas do
    begin
      savedRect := ARect;
      SetBackgroundColor( paintSettings, AIndex, AState );
      FillRect( ARect );
      if Assigned( FOnFill ) then
        FOnFill( AControl, paintSettings.Canvas, odSelected in AState, ARect, AIndex );
      InflateRect( ARect, -2, 0 );
      Brush.Style := bsClear;
      PaintOrMeasure( AControl as TCustomListControl, AIndex, ARect, ipsPaintItem, AState );
      if paintSettings.ShowDividers then
      begin
        ARect := savedRect;
        Pen.Color := fDividerColor;
        Pen.Style := psSolid;
        MoveTo( ARect.Left, ARect.Bottom - 1 );
        LineTo( ARect.Right, ARect.Bottom - 1 );
      end;
    end;
end;

{$ENDREGION}
{$REGION 'ItemHeights'}

procedure TCustomListControlPainter.ClearHeights( AControl: TCustomListControl );
var
  paintSettings: TListControlSettings;
begin
  if FControls.TryGetValue( AControl, paintSettings ) then
    paintSettings.ClearCachedHeights;
end;

procedure TCustomListControlPainter.MeasureItem( AControl: TWinControl; Index: integer; var AHeight: integer );
var
  Rect: TRect;
begin
  Assert( AControl is TCustomListControl );
  Rect := AControl.BoundsRect;
  InflateRect( Rect, -2, 0 );
  if AControl is TCustomListBox then
    InflateRect( Rect, 0, -1 );
  AHeight := PaintOrMeasure( AControl as TCustomListControl, Index, Rect, ipsMeasureItem, [] );
end;

procedure TCustomListControlPainter.SetFixedHeight( AControl: TCustomListControl );
var
  paintSettings: TListControlSettings;
begin
  if FControls.TryGetValue( AControl, paintSettings ) then
  begin
    if ( AControl is TListBox ) then
      with AControl as TListBox do
      begin
        ItemHeight := paintSettings.MaxHeight;
        Style := lbOwnerDrawFixed;
      end
    else if ( AControl is TCheckListBox ) then
      with AControl as TCheckListBox do
      begin
        ItemHeight := paintSettings.MaxHeight;
        Style := lbOwnerDrawFixed;
      end;
  end;
end;

function TCustomListControlPainter.UseColorSet: boolean;
begin
  Result := Assigned( ColorSet );
end;

{$ENDREGION}
{$REGION 'Refresh'}

procedure TCustomListControlPainter.RepopulateFromList( AListBox: TCustomListBox; AObjects: TList );
begin
  RepopulateFromFilteredList( AListBox, AObjects, true, EmptyStr, true );
end;

procedure TCustomListControlPainter.RepopulateFromFilteredList( AListBox: TCustomListBox; AObjects: TList; const ASimple: boolean; const AFilterText: string;
  const AShowAll: boolean );
var
  n: integer;
  thisItem: IListBoxBase;
  savedItem: TObject;
  lookFor, itemText: string;
  topIndex: integer;
begin
  n := 0;
  AListBox.Items.BeginUpdate;
  try
    topIndex := AListBox.topIndex;
    if AListBox.ItemIndex <> -1 then
      savedItem := AListBox.Items.Objects[AListBox.ItemIndex]
    else
      savedItem := nil;
    if AShowAll then
      lookFor := AnsiUppercase( Trim( AFilterText ) )
    else
      lookFor := EmptyStr;
    AListBox.Clear;
    if Assigned( AObjects ) and ( AObjects.Count > 0 ) then
    begin
      Supports( AObjects[0], IListBoxBase, thisItem );
      if thisItem = nil then
        raise Exception.CreateFmt( 'Class %s does not implement IListBoxBase', [TObject( AObjects[0] ).ClassName] );
      while n < AObjects.Count do
      begin
        Supports( AObjects[n], IListBoxBase, thisItem );
        itemText := thisItem.AsListbox( ASimple );
        if ( lookFor = EmptyStr ) or ( Pos( lookFor, AnsiUppercase( itemText ) ) > 0 ) then
        begin
          if ( AShowAll ) or thisItem.IsCurrent then
            AListBox.Items.AddObject( itemText, thisItem as TObject );
        end;
        inc( n );
      end;
      if savedItem <> nil then
        AListBox.ItemIndex := AListBox.Items.IndexOfObject( savedItem );
      if topIndex < AListBox.Count then
        AListBox.topIndex := topIndex;
    end;
  finally
    AListBox.Items.EndUpdate;
  end;
end;

{$ENDREGION}

end.
