unit Emetra.Vcl.Types;

interface

type
  TdcParentFontOptions = set of ( pfName, pfStyle, pfSize );
  TdcBindingType = ( btNone, btProperty, btMethod, btV, btDN, btOT, btS, btStaticText );
  TdcButtonState = set of ( btChecked, btDefault, btDisabled, btFocused, btHot, btInvalid, btPushed, btReadOnly, btSelected, btDesigning );

  TDateRange = ( drAll, drPastOnly, drFutureOnly );
  TPopupAnchor = ( paLeft, paRight, paRightTop );
  TPopupState = set of ( psOpening, psClosing );
  TSelectionStyle = ( ssLight, ssDark, ssCustom );
  TStartDayOfWeek = ( dwSystem, dwMonday, dwSunday );
  TStylingType = ( stStandard, stArenaUI );
  TTimeShowOptions = set of ( to24HourDay, toMinutes );
  TValidationResult = ( vrUnchecked, vrValid, vrInvalid );
  TValidationTriggers = set of ( vtLeave, vtTyping, vtTextChanged );

  TEditOptions = set of ( edPaste, edTyping );

  { Painting }
  TWordWrapType = ( wwNone, wwEllipsis );

  TNxWrapKind = ( wkNone, wkEllipsis, wkPathEllipsis, wkWordEllipsis, wkWordWrap );

  TNxNumericEditOptions = set of ( ednDecimals, ednSigns );
  TNxDrawingOptions = set of ( doBackground, doContent, doCustom );
  TNxDropDownStyle = ( dsDropDown, dsDropDownList );

  { HTML Types }
  THTMLTagKind = ( tgHtml, tgA, tgB, tgBr, tgDiv, tgEM, tgH1, tgH2, tgH3, tgI, tgImg, tgP, tgS, tgSpan, tgStrong, tgSub, tgSup, tgU, tgUndefined );
  THTMLParameterKind = ( tpClass, tpId, tpAlign, tpHref, tpUndefined );

  { CSS Styles Types }
  TCSSDisplay = ( dpBlock, dpInline, dpInlineBlock );
  TCSSFontSize = string;
  TCSSFontStyle = ( fyInherit, fyNormal, fyItalic );
  TCSSFontWeight = ( fwInherit, fwNormal, fwBold );
  TCSSTextAlign = ( tlInherit, tlLeft, tlCenter, tlRight );
  TCSSTextDecoration = ( tdInherit, tdNone, tdUnderline, tdLineTrough, tdOverline );
  TCSSTextTransform = ( ttNone, ttUppercase, ttCapitalize, ttLowercase );
  TCSSVerticalAlign = ( vaBaseline, vaSub, vaSuper );

implementation

end.
