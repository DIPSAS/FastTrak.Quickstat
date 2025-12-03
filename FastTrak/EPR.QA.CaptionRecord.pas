unit EPR.QA.CaptionRecord;

interface

uses
  Data.Db, System.SysUtils;

type
  TCaptionRecord = record
    Title: string;
    VarDescription: string;
    VarName: string;
    procedure Clear;
    procedure LoadAndNext( ADataset: TDataset );
    constructor Create( const AVarName, ATitle: string; const AVarDescription: string = '' );
  end;

implementation

uses
  EPR.QA.SQL;

{ TCaptionRecord }

constructor TCaptionRecord.Create(const AVarName, ATitle, AVarDescription: string);
begin
  VarName := AVarName;
  Title := ATitle;
  VarDescription := AVarDescription;
end;

procedure TCaptionRecord.Clear;
begin
  Title := EmptyStr;
  VarDescription := EmptyStr;
  VarName := EmptyStr;
end;

procedure TCaptionRecord.LoadAndNext( ADataset: TDataset );
begin
  Title := ADataset.FieldByName( FLD_CAPTION ).AsString;
  VarDescription := ADataset.FieldByName( FLD_VAR_DESCRIPTION ).AsString;
  VarName := ADataset.FieldByName( FLD_VAR_NAME ).AsString;
  ADataset.Next;
end;

end.
