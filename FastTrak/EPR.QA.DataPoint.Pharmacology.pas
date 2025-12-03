unit EPR.QA.DataPoint.Pharmacology;

interface

uses
  EPR.QA.DataPoint,
  EPR.QA.Matrix.Interfaces,
  {Standard}
  Vcl.Graphics;

type
  TDigitoxinDatapoint = class( TDataPoint, IBrushColor )
  private
    function BrushColor: TColor;
  end;

  TDrugDatapoint = class( TDataPoint )
  public
    function CellText: string; override;
  end;

  TDrugGreenDatapoint = class( TDrugDatapoint, IBrushColor )
  private
    function BrushColor: TColor;
  end;

  TDrugRedDatapoint = class( TDrugDatapoint, IBrushColor )
  private
    function BrushColor: TColor;
  end;

implementation

uses
  System.SysUtils,
  EPR.QA.DataPoint.Colors;

resourcestring
  StrNo = 'Nei';
  StrYes = 'Ja';

function TDigitoxinDatapoint.BrushColor: TColor;
begin
  if ( Value < 5 ) or ( Value > 20 ) then
    Result := clGraveRisk
  else if ( Value < 6 ) or ( Value > 17 ) then
    Result := clDataPalePurple
  else if ( Value < 7 ) or ( Value > 16 ) then
    Result := clHighRisk
  else if ( Value < 8 ) or ( Value > 15 ) then
    Result := clModerateRisk
  else if ( Value < 9 ) or ( Value > 14 ) then
    Result := clMildRisk
  else if Value > 0 then
    Result := clNoRisk
  else
    Result := clNoData;
end;

{ TDrugDatapoint }

function TDrugDatapoint.CellText: string;
begin
  if Caption <> EmptyStr then
    Result := Copy( Caption, 1, 8 )
  else if Value > 0 then
    Result := StrYes
  else
    Result := StrNo;
end;

{ TDrugGreenDatapoint }

function TDrugGreenDatapoint.BrushColor: TColor;
begin
  if Value > 0 then
    Result := clLowRisk
  else
    Result := clNoData;
end;

{ TDrugRedDatapoint }

function TDrugRedDatapoint.BrushColor: TColor;
begin
  if Value > 0 then
    Result := clGraveRisk
  else
    Result := clNoData;
end;

end.
