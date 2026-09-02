unit Emetra.Vcl.UITypes;

interface

uses
  { Standard }
  System.Classes, System.Types,
  Vcl.Graphics, Vcl.Forms;

type
  { Reusable event types }

  TNxIndexChangeEvent = procedure (Sender: TObject; NewIndex, OldIndex: Integer) of object;

  TNxCustomDrawEvent = procedure (Sender: TObject; DrawingCanvas: TCanvas; DrawingRect: TRect) of object;

type
  TNxScrollType = ( stFirst, stLast, stSmallDecrement, stSmallIncrement, stLargeDecrement, stLargeIncrement, stThumbPosition, stThumbTrack, stEndScroll );


  NxString = WideString;

  TBounds = record
    X, Y, Width, Height: Integer;
  end;

  TRange = record
    Min, Max: Integer;
  end;

  TNxSearchOptions = set of (soCaseSensitive, soRestartAfterEnd, soIncludeInvisible,
    soExactMatch);

  TNxSearchMatchingOptions = (moExactMatch, moBeginWith, moContain);

  TNxOperand = (opAnd, opOr, opNot, opUndefined);

  { Standard Shared }
  TNxDataKind = (dtLayout, dtContent);

  TNxColorKind = (ckDelphi, ckWeb);
  TNxFormatMaskKind = (mkText, mkNumber);
  TNxOrientation = (orHorizontal, orVertical);

  { Date }

//  TNxStartDayOfWeek = (dwSystem, dwMonday, dwSunday);
//  TNxDateRange = (drAll, drPastOnly, drFutureOnly);

  { Time }

  TNxTimeShowOptions = set of (to24HourDay, toMinutes);

  { Styles }

  TNxAppearanceOptions = set of (aoAlternatingRowColors, aoBoldSelectedText,
    aoHintIndicators, aoStyleColors);

  TNxAppearanceStyle = (stNative, stModern, stUserDefined);
  TNxBorderStyle = (btSolid, btLowered, btRaised);
  TNxToggleButtonStyle = (tsStyleNative, tsTriangle, tsSquare, tsGlyph, tsHidden);

  TNxFillMode = (fmAspectFit, fmAspectFill, fmCenter, fmScaleToFill);
  TNxUpdateOptions = set of (uoEvents, uoRecalculate, uoRecount, uoRepaint, uoScrollBar);
  TNxWantKeys = set of (wkArrows, wkTabs, wkReturns);

  TNxComparsion = (coLessThan, coLessEqualThan, coMoreThan, coMoreEqualThan, coEqual, coNotEqual, coNone);

  TNxDataType = (dtAutoInc, dtBoolean, dtDateTime, dtFloat, dtInteger, dtString);

  TNxEncoding = (ekAnsi, ekUnicode, ekUnicodeBigEndian);
  TNxUnicodeEncoding = set of (ueUnicode, ueBigEndian);

  TNxTabsLocation = (tbTop, tbLeft, tbRight, tbBottom);

  { Filter }
  TNxStringFilterOptions = set of (foExactMatch, foCaseSensitive);
  TNxDateFilterOptions = (foTomorrow, foToday, foYesterday, foThisWeek, foThisMonth,
    foThisYear, foUserDefined);

  TNxLocation = (loBottom, loLeft, loRight, loTop);

  TNxFormulaKind = (fkNone, fkAverage, fkCount, fkDistinct, fkMaximum,
    fkMinimum, fkSum, fkCustom);

  TNxSelectionStyle = (stSolid, stAlphaBlended, stFramed);
  TNxSlideSelectionStyle = (slNone, slSolid, slAlphaBlended, slFramed);

  { TStrings }
  TNxStringsDisplayMode = (dmDefault, dmNameList, dmValueList, dmIndentList);

  { Buttons }
  TNxButtonState = set of (bsHover, bsChecked, bsFocused, bsPressed, bsSelected, bsDisabled);

  { Text }
  TNxTextAngle = -90..90;
  TNxTextAlignment = (tlTopLeft, tlTopCenter, tlTopRight, tlCenterLeft, tlCenter,
    tlCenterRight, tlBottomLeft, tlBottomCenter, tlBottomRight);

  TNxCellPaintingState = set of (csSelected, csFocused, csEmpty, csPressed);

  TNxIPNumber = array[0..3] of Integer;

  { Effects }
  TNxPaintEffects = set of (efBevel, efInnerShadow, efLight, efReflection, efShadow, efShine);

  { Color Schemes }
  TNxColorScheme = (csDefault,
    csExcel, csOutlook, csPowerPoint, csWord,
    csVisualStudioBlue);

  TNxColorSchemes = csExcel..csVisualStudioBlue;

  { Misc. }
  TNxProgressBarStyle = (pbStyled, pbSolid, pbBoxes);
  TNxScrollBars = set of TScrollBarKind;
  TNxScrollButtonKind = (skNext, skPage, skPrior);

  { Sort }
  TNxSortKind = (skAscending, skDescending);
	TNxSortType = (stNone, stAlphabetic, stBoolean, stCaseInsensitive, stCustom,
    stNumeric, stDate, stIP, stUnicodeAlphabetic);

  { Specific }
  TNxColumnLocation = (clAlone, clLeftSide, clMiddle, clRightSide);

  { Design }
  TNxDesignOperation = (doSelect);

  { Wrap }
  TNxWrapKind = (wkNone, wkEllipsis, wkPathEllipsis, wkWordEllipsis, wkWordWrap);

  { Misc }
  TNxProcOperation = set of (poPaint, poCalc, poHitTest);

  { CSS Styles }

  TNxDisplay = (dpBlock, dpInline, dpInlineBlock);
  TNxTagKind = (tgHtml, tgA, tgB, tgBr, tgDiv, tgEM, tgH1, tgH2, tgH3, tgI, tgImg, tgP,
    tgS, tgSpan, tgStrong, tgSub, tgSup, tgU, tgUndefined);
  TNxParamKind = (tpClass, tpId, tpAlign, tpHref, tpUndefined);
  TNxFontSize = WideString;
  TNxFontStyle = (fyInherit, fyNormal, fyItalic);
  TNxFontWeight = (fwInherit, fwNormal, fwBold);
  TNxTextAlign = (taInherit, taLeft, taCenter, taRight);
  TNxTextDecoration = (tdInherit, tdNone, tdUnderline, tdLineTrough, tdOverline);
  TNxTextTransform = (ttNone, ttUppercase, ttCapitalize, ttLowercase);
  TNxVerticalAlign = (vaBaseline, vaSub, vaSuper);

  { Validation }

  TNxStatusIcons = set of (siRequired, siRecommended, siReused);

  function Max(const A, B: TSize): TSize; overload;
  function Rotate(const Size: TSize): TSize;

  function StrToBoolEx(const S: string): Boolean;
  function StrToComparsion(const S: string): TNxComparsion;
  function StrToOperand(const S: string): TNxOperand;

  { TRange }

  function EmptyRange: TRange;
  function IsEmpty(Range: TRange): Boolean;
  function Range(Min, Max: Integer): TRange;

  { TRect }

  function CenteredRect(const SourceRect: TRect; const CenteredRect: TRect): TRect;
{$IFNDEF EXTENDED_TYPES}
  function RectWidth(const Rect: TRect): Integer;
  function RectHeight(const Rect: TRect): Integer;
{$ENDIF}

implementation

uses
  { Standard }
  SysUtils, StrUtils, Math;

function Max(const A, B: TSize): TSize; overload;
begin
  Result.cx := Max(A.cx, B.cx);
  Result.cy := Max(A.cy, B.cy);
end;

function Rotate(const Size: TSize): TSize;
begin
  Result.cy := Size.cx;
  Result.cx := Size.cy;
end;

procedure AppendBoolStrArray;
begin
  if Length(TrueBoolStrs) = 0 then
  begin
    SetLength(TrueBoolStrs, 1);
    TrueBoolStrs[0] := DefaultTrueBoolStr;
  end;

  if Length(FalseBoolStrs) = 0 then
  begin
    SetLength(FalseBoolStrs, 1);
    FalseBoolStrs[0] := DefaultFalseBoolStr;
  end;
end;

function StrToBoolEx(const S: string): Boolean;
begin
  { Not a 1 or 0? }
  if not (TryStrToBool(S, Result)) then
  begin
    { Build BoolStrs, if needed }
    AppendBoolStrArray;

    { Check in arrays }
    Result := AnsiMatchText(S, TrueBoolStrs)
      and not AnsiMatchText(S, FalseBoolStrs);
  end;
end;

function StrToComparsion(const S: string): TNxComparsion;
begin
  if S = '>' then Result := coMoreThan
  else if S = '<=' then Result := coMoreEqualThan
  else if S = '<' then Result := coLessThan
  else if S = '>=' then Result := coLessEqualThan
  else if S = '=' then Result := coEqual
  else if S = '<>' then Result := coNotEqual
  else Result := coNone;
end;

function StrToOperand(const S: string): TNxOperand;
begin
  if LowerCase(S) = 'and' then Result := opAnd
  else if LowerCase(S) = 'or' then Result := opOr
  else if LowerCase(S) = 'not' then Result := opNot
  else Result := opUndefined;
end;

function EmptyRange: TRange;
begin
  Result.Min := 0;
  Result.Max := 0;
end;

function IsEmpty(Range: TRange): Boolean;
begin
  Result := (Range.Min = 0) and (Range.Max = 0);
end;

function Range(Min, Max: Integer): TRange;
begin
  Result.Min := Min;
  Result.Max := Max;
end;

function CenteredRect(const SourceRect: TRect; const CenteredRect: TRect): TRect;
var
  Width, Height: Integer;
  X, Y: Integer;
begin
  Width := RectWidth(CenteredRect);
  Height := RectHeight(CenteredRect);
  X := SourceRect.Left + RectWidth(SourceRect) div 2;
  Y := SourceRect.Top + RectHeight(SourceRect) div 2;
  Result := Bounds(X - (Width div 2), Y - (Height div 2), Width, Height);
end;

{$IFNDEF EXTENDED_TYPES}
function RectWidth(const Rect: TRect): Integer;
begin
  Result := Rect.Right - Rect.Left;
end;

function RectHeight(const Rect: TRect): Integer;
begin
  Result := Rect.Bottom - Rect.Top;
end;
{$ENDIF}

initialization

end.
