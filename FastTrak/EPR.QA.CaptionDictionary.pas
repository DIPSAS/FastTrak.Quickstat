unit EPR.QA.CaptionDictionary;

interface

uses
  EPR.QA.CaptionRecord,
  EPR.QA.Matrix.Interfaces,
  Emetra.Database.Interfaces,
  Emetra.Logging.Interfaces,
  System.Classes,
  System.Generics.Collections;

type
  TVarCaptions = class( TInterfacedPersistent, ITitleDictionary, ILoginObserver )
  strict private
    fTitles: TDictionary<string, TCaptionRecord>;
    fLog: ILog;
    fSQL: ISQL;
    fLoadCustomCaptions: boolean;
    fLoadItemCaptions: boolean;
    fLoadLabCaptions: boolean;
  public
    { Initialization }
    constructor Create( ALog: ILog ); reintroduce;
    procedure AfterConstruction; override;
    procedure BeforeDestruction; override;
    { ILoginObserver }
    procedure AfterLogin( Sender: IDatabaseConnection );
    function FriendlyName: string;
    { Other members }
    function GetVarDescription( const AVarName: string ): string;
    function GetVarTitle( const AVarName: string ): string;
    function GetVarSubtitle( const AVarName: string ): string;
    function TryGetCaptions( const AVarName: string; out ACaptionRecord: TCaptionRecord ): boolean;
    procedure AddCaption( const ACaptionRecord: TCaptionRecord );
    { Properties }
    property LoadCustomCaptions: boolean read fLoadCustomCaptions write fLoadCustomCaptions;
    property LoadLabCaptions: boolean read fLoadLabCaptions write fLoadLabCaptions;
    property LoadItemCaptions: boolean read fLoadItemCaptions write fLoadItemCaptions;
  end;

implementation

uses
  EPR.QA.SQL,
  System.RegularExpressions,
  System.SysUtils;

{ TVarCaptions }

{$REGION 'Initialization'}

constructor TVarCaptions.Create( ALog: ILog );
begin
  inherited Create;
  fLog := ALog;
  fSQL := nil;
end;

procedure TVarCaptions.AfterConstruction;
begin
  inherited;
  fTitles := TDictionary<string, TCaptionRecord>.Create;
  fLoadCustomCaptions := true;
  fLoadLabCaptions := true;
  fLoadItemCaptions := false;
end;

procedure TVarCaptions.BeforeDestruction;
begin
  fTitles.Free;
  inherited;
end;

{$ENDREGION}
{$REGION 'ILoginObserver'}

procedure TVarCaptions.AfterLogin( Sender: IDatabaseConnection );
var
  captionRec: TCaptionRecord;
begin
  if Supports( Sender, ISQL, fSQL ) then
    try
      if fLoadCustomCaptions then
        try
          { Custom captions have highest priority after programmatically added captions }
          with fSQL.FastQuery( QueryCustomCaptions ) do
            try
              while not EOF do
              begin
                captionRec.LoadAndNext( fSQL.Dataset );
                if not fTitles.ContainsKey( captionRec.VarName ) then
                  fTitles.AddOrSetValue( captionRec.VarName, captionRec );
              end;
            finally
              Close;
            end;
        except
          on E: Exception do
            fLog.SilentError( E.Message );
        end;
      { Lab captions are next in priority }
      if fLoadLabCaptions then
      begin
        with fSQL.FastQuery( QueryLabCaptions ) do
          try
            while not EOF do
            begin
              captionRec.LoadAndNext( fSQL.Dataset );
              if not fTitles.ContainsKey( captionRec.VarName ) then
                fTitles.AddOrSetValue( captionRec.VarName, captionRec );
            end;
          finally
            Close;
          end;
      end;
      if fLoadItemCaptions then
      begin
        { Captions based on MetaFormItem specification have lowest priority }
        with fSQL.FastQuery( QueryItemCaptions ) do
          try
            while not EOF do
            begin
              captionRec.LoadAndNext( fSQL.Dataset );
              captionRec.Title := Trim( TRegEx.Replace( captionRec.Title, '\(.*\)', EmptyStr ) );
              if not fTitles.ContainsKey( captionRec.VarName ) then
                fTitles.Add( captionRec.VarName, captionRec );
            end;
          finally
            Close;
          end;
      end;
    except
      on E: Exception do
      begin
        fLog.SilentWarning( E.Message );
        fTitles.Clear;
      end;
    end;
end;

function TVarCaptions.FriendlyName: string;
begin
  Result := 'Variable captions';
end;

{$ENDREGION}

procedure TVarCaptions.AddCaption( const ACaptionRecord: TCaptionRecord );
begin
  Assert( ACaptionRecord.VarName <> EmptyStr );
  Assert( ACaptionRecord.Title <> EmptyStr );
  fTitles.AddOrSetValue( ACaptionRecord.VarName, ACaptionRecord );
end;

function TVarCaptions.GetVarDescription( const AVarName: string ): string;
var
  captionRec: TCaptionRecord;
begin
  if fTitles.TryGetValue( AVarName, captionRec ) then
    Result := captionRec.VarDescription
  else
    Result := EmptyStr;
end;

function TVarCaptions.TryGetCaptions( const AVarName: string; out ACaptionRecord: TCaptionRecord ): boolean;
begin
  Result := fTitles.TryGetValue( AVarName, ACaptionRecord );
end;

function TVarCaptions.GetVarSubtitle( const AVarName: string ): string;
begin
  Result := EmptyStr;
end;

function TVarCaptions.GetVarTitle( const AVarName: string ): string;
var
  captionRec: TCaptionRecord;
begin
  if fTitles.TryGetValue( AVarName, captionRec ) then
    Result := captionRec.Title
  else
    Result := AVarName;
end;

end.
