unit EPR.QA.DataPoint.VitalSigns;

interface

uses
  EPR.QA.Matrix.Interfaces,
  EPR.QA.DataPoint,
  Vcl.Graphics;

type
  { Datapoint class for Body Mass Index. }
  TBMIDatapoint = class( TDataPoint, IBrushColor )
  public
    function BrushColor: TColor;
    function CellText: string; override;
  end;

  { Datapoint for systolic blood pressure, with coloring
    to indicate higher than normal values. }
  TSBPDatapoint = class( TDataPoint, IBrushColor )
  public
    function BrushColor: TColor;
  end;

  { Datapoint class for diastolic blood pressure, with coloring
    to indicate higher than normal values. }
  TDBPDatapoint = class( TDataPoint, IBrushColor )
  public
    function BrushColor: TColor;
  end;

implementation

uses
  EPR.QA.DataPoint.Colors,
  System.SysUtils;

{ TBMIDatapoint }

function TBMIDatapoint.CellText;
begin
  Result := Format( '%.1f', [Value] );
end;

function TBMIDatapoint.BrushColor: TColor;
begin
  if Value <= 0 then
    Result := clNoData
  else if ( Value > 40 ) or ( Value < 15 ) then
    Result := clGraveRisk
  else if ( Value > 35 ) or ( Value < 16 ) then
    Result := clHighRisk
  else if ( Value > 30 ) or ( Value < 17 ) then
    Result := clModerateRisk
  else if ( Value > 27 ) or ( Value < 18.5 ) then
    Result := clMildRisk
  else
    Result := clNoRisk
end;

{ TSBPDatapoint }

function TSBPDatapoint.BrushColor: TColor;
begin
  if Value > 180 then
    Result := clGraveRisk
  else if Value > 160 then
    Result := clHighRisk
  else if Value > 150 then
    Result := clModerateRisk
  else if ( Value > 140 ) or ( Value < 100 ) then
    Result := clMildRisk
  else if Value > 0 then
    Result := clNoRisk
  else
    Result := clNoData;
end;

{ TDBPDatapoint }

function TDBPDatapoint.BrushColor: TColor;
begin
  if Value > 100 then
    Result := clGraveRisk
  else if Value > 95 then
    Result := clHighRisk
  else if Value > 90 then
    Result := clModerateRisk
  else if Value > 85 then
    Result := clMildRisk
  else if Value > 0 then
    Result := clNoRisk
  else
    Result := clNoData;
end;

end.
