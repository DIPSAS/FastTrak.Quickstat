{$HINTS OFF}
unit Emetra.Classes.RegEx;

interface

uses
  RegularExpressionsCore,
  {General}
  Emetra.Interfaces.RegEx,
  {Standard}
  Classes, SysUtils;

type
  TRegExEngine = class(TComponent, IRegExEngine)
  private
    FRegEx: TPerlRegEx;
    function GetMatchedExpression: string;
    function GetSubject: string;
    procedure SetSubject(const AValue: string);
    function GetReplacement: string;
    procedure SetReplacement(const AValue: string);
  protected
    function GetRegEx: string;
    procedure SetRegEx(const AValue: string);
  public
    constructor Create(AOwner: TComponent = nil); override;
    destructor Destroy; override;
    function StripRTF(ASource: string): string;
    procedure Compile;
    procedure Study;
    function Match: boolean;
    function MatchAgain: boolean;
    function MatchedExpressionOffset: integer;
    function MatchedExpressionLength: integer;
    function Replace: string;
    function ReplaceAll: boolean;
    property Subject: string read GetSubject write SetSubject;
    property RegEx: string read GetRegEx write SetRegEx;
    property MatchedExpression: string read GetMatchedExpression;
    property Replacement: string read GetReplacement write SetReplacement;
  end;

  TPatternMatcher = class(TObject)
  private
    FRegEx: TRegExEngine;
  public
    constructor Create;
    destructor Destroy; override;
    function PatternToRegEx(const AToken: string): string;
    function FindPattern(const ASubject: string; const APattern: string): boolean;
    function Found(const ARegEx, ASubject: string; const AReplaceCommas: boolean = true): boolean; overload;
    function Found(const ARegEx, ASubject: string; out AMatchingText: string; const AReplaceCommas: boolean = true): boolean; overload;
  end;

function ValidRegEx(const ARegEx: string): boolean;

function Match: TPatternMatcher;

const
  roSingleLine = preSingleLine;

implementation

resourcestring
  ASSERT_NO_WORDCHAR = 'The "\w" expression is not supported for Unicode, use \p{L} or similar constructs';

var
  DefaultInstance: TRegExEngine = nil;
  DefaultMatch: TPatternMatcher = nil;

function Match: TPatternMatcher;
begin
  Result := DefaultMatch;
end;

function TRegExEngine.GetSubject: string;
begin
  Result := string(FRegEx.Subject);
end;

function TRegExEngine.GetRegEx: string;
begin
  Result := string(FRegEx.RegEx);
end;

procedure TRegExEngine.Compile;
begin
  FRegEx.Compile;
end;

procedure TRegExEngine.Study;
begin
  FRegEx.Study;
end;

function TRegExEngine.Match: boolean;
begin
  Result := FRegEx.Match;
end;

function TRegExEngine.MatchAgain: boolean;
begin
  Result := FRegEx.MatchAgain;
end;

function TRegExEngine.ReplaceAll: boolean;
begin
  Result := FRegEx.ReplaceAll;
end;


function TRegExEngine.Replace: string;
begin
{$IF CompilerVersion < 25}
  Result := System.UTF8ToString(FRegEx.Replace)
{$ELSE}
  Result := FRegEx.Replace;
{$IFEND}
end;


function TRegExEngine.MatchedExpressionOffset: integer;
begin
  Result := FRegEx.MatchedOffset;
end;

function TRegExEngine.MatchedExpressionLength: integer;
begin
  Result := FRegEx.MatchedLength;
end;

function TPatternMatcher.Found(const ARegEx, ASubject: string; out AMatchingText: string;
  const AReplaceCommas: boolean): boolean;
begin
  if AReplaceCommas then
    Match.FRegEx.RegEx := StringReplace(ARegEx, ',', '|', [rfReplaceAll])
  else
    Match.FRegEx.RegEx := ARegEx;
  if Match.FRegEx.RegEx = EmptyStr then
    raise Exception.Create('PrepareRegEx failed (no expression)');
  Match.FRegEx.Compile;
  Match.FRegEx.Subject := ASubject;
  Result := Match.FRegEx.Match;
  if Result then
    AMatchingText := Match.FRegEx.MatchedExpression;
end;

function TPatternMatcher.Found(const ARegEx, ASubject: string; const AReplaceCommas: boolean = true): boolean;
var
  strTemp: string;
begin
  Result := Found(ARegEx, ASubject, strTemp, AReplaceCommas);
end;

function ValidRegEx(const ARegEx: string): boolean;
begin
  Result := false;
  if ARegEx <> EmptyStr then
    try
      DefaultInstance.RegEx := ARegEx;
      DefaultInstance.Compile;
      Result := true;
    except
      on E: Exception do
    end;
end;
{ TRegExEngine }

constructor TRegExEngine.Create(AOwner: TComponent = nil);
begin
  inherited Create(AOwner);
  FRegEx := TPerlRegEx.Create;
  FRegEx.Options := FRegEx.Options + [preCaseless];
end;

destructor TRegExEngine.Destroy;
begin
  FRegEx.Free;
  inherited;
end;

function TRegExEngine.GetMatchedExpression: string;
begin
{$IF CompilerVersion < 25}
  Result := System.UTF8ToString(FRegEx.MatchedText);
{$ELSE}
  Result := FRegEx.MatchedText;
{$IFEND}
end;

procedure TRegExEngine.SetRegEx(const AValue: string);
begin
  Assert(Pos('\w', AValue) = 0, ASSERT_NO_WORDCHAR);
  {$IF CompilerVersion < 25}
  FRegEx.RegEx := System.UTF8Encode(AValue);
  {$ELSE}
  FRegEx.RegEx := AValue;
{$IFEND}
end;

procedure TRegExEngine.SetSubject(const AValue: string);
begin
  {$IF CompilerVersion < 25}
  FRegEx.Subject := System.UTF8Encode(AValue);
  {$ELSE}
  FRegEx.Subject := AValue;
{$IFEND}
end;

function TRegExEngine.GetReplacement: string;
begin
  {$IF CompilerVersion < 25}
  Result := System.UTF8ToString(FRegEx.Replacement);
  {$ELSE}
  Result := FRegEx.Replacement;
{$IFEND}
end;

procedure TRegExEngine.SetReplacement(const AValue: string);
begin
  {$IF CompilerVersion < 25}
  FRegEx.Replacement := System.UTF8Encode(AValue);
  {$ELSE}
  FRegEx.Replacement := AValue;
  {$IFEND}
end;

function TRegExEngine.StripRTF(ASource: string): string;
var
  iChar: integer;
  i, l: integer;
  strHex: string;
begin
  Result := EmptyStr;
  if ASource = EmptyStr then
    exit;
  i := 1;
  ASource := StringReplace(ASource, '\par ', sLineBreak, [rfReplaceAll]);
  l := Length(ASource);
  while i <= l do
  begin
    if (ASource[i] = '\') and (ASource[i + 1] = '''') then
    begin
      strHex := Copy(ASource, i + 2, 2);
      iChar := StrToIntDef('$' + strHex, 32);
      Result := Result + chr(iChar);
      inc(i, 3);
    end
    else
      Result := Result + ASource[i];
    inc(i);
  end;
  Subject := Result;
  { Avoid assertion that \w is unsupported by accessing FRegEx.RegEx directly }
  FRegEx.RegEx := '{\\[\w\s\d]*}|{(\\\w+)+(\s\w+)*;}|\\\w+';
  Replacement := '';
  ReplaceAll;
  RegEx := '{}';
  ReplaceAll;
  Result := Subject;
  l := Length(Result);
  if l > 1 then
  begin
    if Result[l] = '}' then
      Delete(Result, l, 1);
    if Result[1] = '{' then
      Delete(Result, 1, 1);
    Result := Trim(Result);
  end;
end;

{ TMatchCompat }

constructor TPatternMatcher.Create;
begin
  FRegEx := TRegExEngine.Create(nil);
end;

destructor TPatternMatcher.Destroy;
begin
  FRegEx.Free;
  inherited;
end;

function TPatternMatcher.FindPattern(const ASubject, APattern: string): boolean;
begin
  Result := false;
  if APattern <> EmptyStr then
    try
      FRegEx.Subject := ASubject;
      FRegEx.RegEx := PatternToRegEx(APattern);
      Result := FRegEx.Match
    except
      on Exception do
    end;
end;

function TPatternMatcher.PatternToRegEx(const AToken: string): string;
var
  i, len: integer;
  strRegEx: string;
begin
  Result := EmptyStr;
  if AToken <> EmptyStr then
  begin
    { Fix match for beginning of string }
    strRegEx := AToken;
    if strRegEx[1] = '*' then
      strRegEx := Copy(strRegEx, 2, maxint)
    else
      strRegEx := '^' + strRegEx;
    { Fix match for end of string }
    len := Length(strRegEx);
    if strRegEx[len] = '*' then
      strRegEx := Copy(strRegEx, 1, len - 1)
    else
      strRegEx := strRegEx + '$';
    { Escape special characters }
    len := Length(strRegEx);
    for i := 1 to len do
      case strRegEx[i] of
        '-', '.', '(', ')', '\', '/':
          Result := Result + '\' + strRegEx[i];
        '?':
          Result := Result + '.';
        ' ':
          Result := Result + '\s';
        '*':
          Result := Result + '.*';
      else
        Result := Result + strRegEx[i];
      end;
  end;
end;

initialization

DefaultInstance := TRegExEngine.Create(nil);
DefaultMatch := TPatternMatcher.Create;

finalization

FreeAndNil(DefaultInstance);
FreeAndNil(DefaultMatch);

end.
