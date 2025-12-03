unit Emetra.Utils.Params;

interface

uses
  Emetra.Settings.Interfaces,
  {Standard}
  System.Classes, System.SysUtils;

type
  /// <summary>
  /// Provides a simple way to access parameters passed on the command line,
  /// or as part of an HTML og XML tag.
  /// </summary>
  /// <example>
  /// Pass a command line or a complete Html tag to the Parse method, and
  /// read back information with ReadXXX methods, or access the indiviual
  /// parameters with the Items property.
  /// </example>
  TParamList = class( TInterfacedPersistent, IParametersRead )
  private
    function Get_Item( AIndex: integer ): string;
  protected
    FData: TStringList;
  public
    { Initialzation }
    constructor Create; reintroduce; overload;
    constructor Create( const AParamStr: string ); overload;
    constructor CreateFromCommandLine;
    procedure BeforeDestruction; override;
    { Other members }
    function Count: integer;

    /// <summary>
    /// A Switch is an option on a command line preceded by a forward slash.
    /// </summary>
    function Switch( const ASwitch: string ): boolean;

    /// <summary>
    /// An option is similar to a Switch, only preceded by a dash.  Returns
    /// true if options is found in AParamString passed to Parse method.
    /// </summary>
    function Option( const AOption: string ): boolean;
    /// <summary>
    ///   <para>
    ///     Returns true if a flag was found on the parameter list. A flag is
    ///     just text, e.g. on the command line:
    ///   </para>
    ///   <para>
    ///     <c>c:\&gt;SomeProgram LoadFromFile.</c> There is no dash or slash
    ///     in front of it. Flags are case insensitive.
    ///   </para>
    /// </summary>
    function Flag( const AFlag: string ): boolean; { Flags are just text }

    { Read data from the input string in a similar fashion to TIniFile }
    function ReadBool( const AKey: string; const ADefault: boolean = false ): boolean;
    function ReadInteger( const AKey: string; const ADefault: integer = 0 ): integer;
    function ReadString( const AKey: string; const ADefault: string = '' ): string;
    function ReadDate( const AKey: string; const ADefault: TDateTime ): TDateTime;
    function ReadFloat( const AKey: string; const ADefault: double ): double;

    { Writable }
    procedure WriteFloat( const AKey: string; const AValue: double );
    procedure WriteBool( const AKey: string; const AValue: boolean );
    procedure WriteString( const AKey: string; const AValue: string );
    procedure WriteInteger( const AKey: string; const AValue: integer );

    { Checks to see if a key exists }
    function Exists( const AKey: string ): boolean;
    function Text: string;
    procedure AddFlag( const AFlag: string );
    procedure Clear;
    procedure Parse( const AParamStr: string );
    procedure ParseCommandLine;
    { Properties }
    property Items[AIndex: integer]: string read Get_Item; default;
  end;

implementation

{ TParamList }

{$REGION 'Initialization'}

constructor TParamList.Create;
begin
  inherited;
  FData := TStringList.Create;
  FData.CaseSensitive := false;
  FData.Duplicates := dupIgnore;
end;

constructor TParamList.Create( const AParamStr: string );
begin
  Create;
  Parse( AParamStr );
end;

constructor TParamList.CreateFromCommandLine;
begin
  Create;
  ParseCommandLine;
end;

procedure TParamList.BeforeDestruction;
begin
  FData.Free;
  inherited;
end;

{$ENDREGION}

procedure TParamList.ParseCommandLine;
var
  n: integer;
begin
  FData.Clear;
  for n := 0 to ParamCount do
    FData.Add( ParamStr( n ) );
  Parse( FData.Text );
end;

function TParamList.Count: integer;
begin
  Result := FData.Count;
end;

function TParamList.Exists( const AKey: string ): boolean;
begin
  Result := FData.IndexOfName( AKey ) <> -1;
end;

function TParamList.Flag( const AFlag: string ): boolean;
begin
  Result := FData.IndexOf( AFlag ) <> -1;
end;

function TParamList.Get_Item( AIndex: integer ): string;
begin
  Result := FData[AIndex];
end;

function TParamList.Option( const AOption: string ): boolean;
begin
  Result := FData.IndexOf( '-' + AOption ) <> -1;
end;

procedure TParamList.Clear;
begin
  FData.Clear;
end;

procedure TParamList.Parse( const AParamStr: string );
var
  i, len: integer;
  bBreakOnSpace: boolean;
  strVal: string;
begin
  Clear;
  len := Length( AParamStr );
  if len = 0 then
    exit;
  bBreakOnSpace := true;
  strVal := EmptyStr;
  for i := 1 to len do
    case AParamStr[i] of
      '"': bBreakOnSpace := not bBreakOnSpace;
      ' ', #9, #10, #13:
        if not bBreakOnSpace then
          strVal := strVal + AParamStr[i]
        else if Trim( strVal ) <> EmptyStr then
        begin
          FData.Add( Trim( strVal ) );
          strVal := EmptyStr;
        end;
    else strVal := strVal + AParamStr[i];
    end;
  if Trim( strVal ) <> EmptyStr then
    FData.Add( strVal );
end;

function TParamList.ReadBool( const AKey: string; const ADefault: boolean = false ): boolean;
begin
  Result := StrToBoolDef( FData.Values[AKey], ADefault );
end;

function TParamList.ReadInteger( const AKey: string; const ADefault: integer = 0 ): integer;
begin
  Result := StrToIntDef( FData.Values[AKey], ADefault );
end;

function TParamList.ReadString( const AKey: string; const ADefault: string = '' ): string;
begin
  Result := FData.Values[AKey];
  if Result = EmptyStr then
    Result := ADefault;
end;

function TParamList.ReadDate( const AKey: string; const ADefault: TDateTime ): TDateTime;
begin
  Result := StrToDateDef( FData.Values[AKey], ADefault );
end;

function TParamList.ReadFloat( const AKey: string; const ADefault: double ): double;
begin
  Result := StrToFloatDef( FData.Values[AKey], ADefault );
end;

function TParamList.Switch( const ASwitch: string ): boolean;
begin
  Result := FData.IndexOf( '/' + ASwitch ) <> -1;
end;

function TParamList.Text: string;
begin
  Result := FData.Text;
end;

{$REGION 'Writing to the settings'}

procedure TParamList.AddFlag( const AFlag: string );
begin
  FData.Add( AFlag );
end;

procedure TParamList.WriteFloat( const AKey: string; const AValue: double );
begin
  FData.Values[AKey] := AValue.ToString;
end;

procedure TParamList.WriteBool( const AKey: string; const AValue: boolean );
begin
  FData.Values[AKey] := BoolToStr( AValue, true );
end;

procedure TParamList.WriteInteger( const AKey: string; const AValue: integer );
begin
  FData.Values[AKey] := AValue.ToString;
end;

procedure TParamList.WriteString( const AKey, AValue: string );
begin
  FData.Values[AKey] := AValue;
end;

{$ENDREGION}

end.
