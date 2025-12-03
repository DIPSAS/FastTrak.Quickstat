unit EPR.QA.DataPoint.Biochemistry;

interface

uses
  EPR.QA.Matrix.Interfaces,
  EPR.QA.DataPoint,
  Vcl.Graphics;

type

  { Datapoint class for total cholesterol, with custom coloring. }
  TCholDatapoint = class( TDataPoint, IBrushColor )
  private
    function BrushColor: TColor;
  end;

  { Datapoint class for P-LDL-cholestero, with custom coloring. }
  TLdlDatapoint = class( TDataPoint, IBrushColor )
  private
    function BrushColor: TColor;
  end;

  { Datapoint class for glycosylated hemoglobin, with custom
    coloring suitable for diabetes patients. }
  THbA1cPercentDatapoint = class( TDataPoint, IBrushColor )
  private
    function BrushColor: TColor;
  end;

  THbA1cMmolDatapoint = class( TDataPoint, IBrushColor )
  private
    function BrushColor: TColor;
  end;

  THbA1cHistoryDatapoint = class( TDataPoint, IFontColor )
  private
    function FontColor: TColor;
  end;

  { Datapoint class for S-Sodium (Natrium), with warning colors for
    high and low values. }
  TSodiumDatapoint = class( TDataPoint, IBrushColor )
  private
    function BrushColor: TColor;
  end;

  { Datapoint class for S-Potassium (Kalium), with warning colors for
    high and low values. }
  TPotassiumDatapoint = class( TDataPoint, IBrushColor )
  private
    function BrushColor: TColor;
  end;

  THemoGlobinDatapoint = class( TDataPoint, IBrushColor )
  private
    function BrushColor: TColor;
  end;

implementation

uses
  EPR.QA.DataPoint.Colors;

{ TLDLDatapoint }

function TLdlDatapoint.BrushColor: TColor;
begin
  if Value > 5 then
    Result := clGraveRisk
  else if Value > 4 then
    Result := clHighRisk
  else if Value > 3 then
    Result := clModerateRisk
  else if Value > 2 then
    Result := clMildRisk
  else if Value > 1.8 then
    Result := clLowRisk
  else if Value > 0 then
    Result := clNoRisk
  else
    Result := clNoData;
end;

{ TCholDatapoint }

function TCholDatapoint.BrushColor: TColor;
begin
  if Value > 8 then
    Result := clGraveRisk
  else if Value > 7 then
    Result := clHighRisk
  else if Value > 6 then
    Result := clModerateRisk
  else if Value > 5 then
    Result := clMildRisk
  else if Value > 4.5 then
    Result := clLowRisk
  else if Value > 0 then
    Result := clNoRisk
  else
    Result := clNoData;
end;

{ THbA1cDatapoint }

function THbA1cPercentDatapoint.BrushColor: TColor;
begin
  if Value > 10 then
    Result := clGraveRisk
  else if Value > 9 then
    Result := clHighRisk
  else if Value > 8 then
    Result := clModerateRisk
  else if Value > 7 then
    Result := clMildRisk
  else if Value > 6.5 then
    Result := clLowRisk
  else if Value > 0 then
    Result := clNoRisk
  else
    Result := clNoData;
end;

function THbA1cMmolDatapoint.BrushColor: TColor;
begin
  if Value > 86 then
    Result := clGraveRisk
  else if Value > 75 then
    Result := clHighRisk
  else if Value > 65 then
    Result := clModerateRisk
  else if Value > 58 then
    Result := clMildRisk
  else if Value > 53 then
    Result := clLowRisk
  else if Value > 0 then
    Result := clWebAliceBlue
  else
    Result := clNoData;
end;

function THbA1cHistoryDatapoint.FontColor: TColor;
begin
  if Value >= 75 then
    Result := clWebRed
  else if Value >= 58 then
    Result := clWebDarkOrange
  else if Value >= 53 then
    Result := clGreen
  else if Value > 0 then
    Result := clBlue
  else
    Result := clNoData;
end;

{ TSodiumDatapoint }

function TSodiumDatapoint.BrushColor: TColor;
begin
  if ( Value < 132 ) or ( Value > 150 ) then
    Result := clGraveRisk
  else if ( Value < 134 ) or ( Value > 148 ) then
    Result := clHighRisk
  else if ( Value < 136 ) or ( Value > 146 ) then
    Result := clModerateRisk
  else if ( Value < 137 ) or ( Value > 145 ) then
    Result := clMildRisk
  else
    Result := clWhite;
end;

{ TPotassiumDatapoint }

function TPotassiumDatapoint.BrushColor: TColor;
begin
  if ( Value < 3 ) or ( Value > 5.5 ) then
    Result := clGraveRisk
  else if ( Value < 3.2 ) or ( Value > 5.3 ) then
    Result := clHighRisk
  else if ( Value < 3.3 ) or ( Value > 5.2 ) then
    Result := clModerateRisk
  else if ( Value < 3.4 ) or ( Value > 5.1 ) then
    Result := clMildRisk
  else
    Result := clWhite;
end;

{ THemoGlobinDatapoint }

function THemoGlobinDatapoint.BrushColor: TColor;
begin
  if ( Value < 9 ) or ( Value > 20 ) then
    Result := clGraveRisk
  else if ( Value < 10 ) or ( Value > 19 ) then
    Result := clHighRisk
  else if ( Value < 11 ) or ( Value > 18.5 ) then
    Result := clModerateRisk
  else if ( Value < 12 ) or ( Value > 18.0 ) then
    Result := clMildRisk
  else
    Result := clWhite;

end;

end.
