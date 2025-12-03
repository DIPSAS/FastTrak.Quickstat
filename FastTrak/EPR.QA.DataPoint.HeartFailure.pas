unit EPR.QA.DataPoint.HeartFailure;

interface

uses
  EPR.QA.Matrix.Interfaces,
  EPR.QA.DataPoint,
  {Standard}
  Graphics;

type
  TPulseQualityDatapoint = class( TDatapoint, IBrushColor )
  public
    function BrushColor: TColor;
    function CellText: string; override;
  end;

  THeartRhythmDatapoint = class( TDatapoint, IBrushColor )
  public
    function BrushColor: TColor;
    function CellText: string; override;
  end;

  TGeneralDirectionDatapoint = class( TDatapoint, IBrushColor )
  public
    function BrushColor: TColor;
    function CellText: string; override;
  end;

implementation

uses
  EPR.QA.DataPoint.Colors,
  System.SysUtils;

{ TPulseDatapoint }

function TPulseQualityDatapoint.BrushColor: TColor;
var
  intVal: integer;
begin
  intVal := Round( Value );
  case intVal of
    1: Result := clNoRisk;
    2, 3: Result := clMildRisk;
  else Result := clNoData;
  end;
end;

function TPulseQualityDatapoint.CellText: string;
var
  enumVal: integer;
begin
  enumVal := Round( Value );
  case enumVal of
    1: Result := 'Rgm';
    2: Result := 'AF';
    3: Result := 'ES';
  else Result := '?';
  end;
  Caption := Result;
end;

{ TPulseDatapoint }

function THeartRhythmDatapoint.BrushColor: TColor;
var
  intVal: integer;
begin
  intVal := Round( Value );
  case intVal of
    1: Result := clNoRisk;
    2: Result := clMildRisk;
    3: Result := clModerateRisk;
  else Result := clNoData;
  end;
end;

function THeartRhythmDatapoint.CellText: string;
var
  enumVal: integer;
begin
  enumVal := Round( Value );
  case enumVal of
    1: Result := 'Rgm';
    2: Result := 'AF';
    3: Result := 'ES';
    9: Result := 'n/a';
  else Result := EmptyStr;
  end;
  Caption := Result;
end;

{ TGeneralDirection }

function TGeneralDirectionDatapoint.BrushColor: TColor;
var
  intVal: integer;
begin
  intVal := Round( Value );
  case intVal of
    1: Result := clNoRisk;
    2: Result := clHighRisk;
    3: Result := clLowRisk;
  else Result := clNoData;
  end;
end;

function TGeneralDirectionDatapoint.CellText: string;
var
  enumVal: integer;
begin
  enumVal := Round( Value );
  case enumVal of
    1: Result := 'Stab';
    2: Result := '--';
    3: Result := '++';
  else Result := EmptyStr;
  end;
  Caption := Result;
end;

end.
