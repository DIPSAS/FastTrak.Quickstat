unit EPR.QA.DataPoint.Dogfood;

interface

uses
  EPR.QA.DataPoint,
  EPR.QA.Matrix.Interfaces,
  EPR.QA.Datapoint.Colors,
  Vcl.Graphics;

type
  TDbVersionDatapoint = class( TDatapoint, IBrushColor )
  public
    function BrushColor: TColor;
  end;

  TDbServerVersionDatapoint = class( TDatapoint, IBrushColor )
    function BrushColor: TColor;
    function CellText: string; override;
  end;
  { TDbVersionDatapoint }

const
  VAR_DB_VERSION       = 'DB_VERSION'; { Variable name for latest installed database version, ItemId = 38 }
  VAR_SERVER_VERSION   = 'DbVersion'; { ItemId = 5917 }

implementation

function TDbVersionDatapoint.BrushColor: TColor;
begin
  if Value >= 19016 then
    Result := clLowRisk
  else if Value >= 19000 then
    Result := clMildRisk
  else if Value >= 18000 then
    Result := clModerateRisk
  else if Value > 0 then
    Result := clGraveRisk
  else
    Result := clNoData;
end;

{ TServVersionDatapoint }

function TDbServerVersionDatapoint.CellText: string;
begin
  if Value = 7 then
    Result := '2016'
  else if Value = 6 then
    Result := '2014'
  else if Value = 5 then
    Result := '2012'
  else if Value = 4 then
    Result := '2008R2'
  else if Value > 0 then
    Result := 'Gammel'
  else
    Result := '?';
end;

function TDbServerVersionDatapoint.BrushColor: TColor;
begin
  if Value >= 7 then
    Result := clLowRisk
  else if Value > 4 then
    Result := clMildRisk
  else if Value > 0 then
    Result := clGraveRisk
  else
    Result := clNoData;
end;


end.
