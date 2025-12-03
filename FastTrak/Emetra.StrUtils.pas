unit Emetra.StrUtils;

interface

function Coalesce( const s1, s2: string ): string;
function RemoveTrailingPunctuation( const s: string ): string;
function PascalCase( const s: string ): string;
function EscapeRichTextFormat( const s: string ): string;
function GetNewGuid: string;
function GetNewStrippedGuid: string;

implementation

uses
  System.SysUtils;

function PascalCase( const s: string ): string;
begin
  Result := AnsiUppercase( Copy( s, 1, 1 ) ) + AnsiLowercase( Copy( s, 2, maxint ) );
end;

function RemoveTrailingPunctuation( const s: string ): string;
var
  idx: integer;
begin
  Result := Trim( s );
  idx := Length( Result );
  while idx > 1 do
  begin
    case Result[idx] of
      '.', ',', '!', '?': Result[idx] := ' ';
    else break;
    end;
    dec( idx );
  end;
  Result := Trim( Result );
end;

function EscapeRichTextFormat( const s: string ): string;
var
  idx, inputLength: integer;
begin
  if s = EmptyStr then
    Result := s
  else
  begin
    inputLength := Length( s );
    SetLength( Result, inputLength * 2 );
    Result := EmptyStr;
    for idx := 1 to inputLength do
    begin
      if CharInSet( s[idx], ['{', '}', '\'] ) then
        Result := Result + '\';
      Result := Result + s[idx];
    end;
  end;
end;

function GetNewGuid: string;
var
  newGuid: TGuid;
begin
  CreateGuid( newGuid );
  Result := GuidToString( newGuid );
end;

function GetNewStrippedGuid: string;
begin
  Result := AnsiLowercase( Copy( GetNewGuid, 2, 36 ) );
end;


function Coalesce( const s1, s2: string ): string;
begin
  if s1 = EmptyStr then
    Result := s2
  else
    Result := s1;
end;

end.
