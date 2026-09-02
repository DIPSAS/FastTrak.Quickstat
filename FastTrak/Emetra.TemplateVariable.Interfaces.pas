unit Emetra.TemplateVariable.Interfaces;

interface

uses
  Emetra.Settings.Interfaces,
  Classes;

type
  TOutputFormat = ( outPlain, outRtf, outSimpleHtml, outHtml );
  TMacroFindStatus = (
    mfsUndefined,
    mfsUnknownGlobal,
    mfsUnknownObject, mfsUnknownProperty,
    mfsFoundGlobal, mfsFoundObject,
    mfsFoundProperty, mfsDefaultProperty,
    mfsViaMacroInterface, mfsViaVariantInterface, mfsViaStringInterface );

  ITemplateVariableSynonymList = interface['{FDC36521-6A2D-4645-BBD5-92C16A8BA461}']
    procedure AddSynonym( const ASynonym, AMapsTo: string );
    function GetSynonym( const ASynonum: string ): string;
  end;

  IMacroRequestFulfiller = interface['{85FA5B4B-E64E-4425-A93A-0FA12B52DFA1}']
    function TryGetString( const AVarName: string; const AParams: IParametersRead; out AValue: string ): boolean;
  end;

  IMacroMediator = interface['{D586E098-49C4-4A43-8BF0-F0D452E083E6}']
    { Property accessors }
    function Get_EmptyText: string;
    function Get_FormatText: string;
    function Get_FindStatus: TMacroFindStatus;
    function Get_OverrideNewLine: string;
    { Other members }
    function TryGetValue(const AVarName: string; var AValue: Variant): boolean;
    function TryGetNumber( const AVarName : string; var Value : Extended ): boolean;
    function TryGetString( const AVarName: string; var AValue: string ): boolean;
    { Properties }
    property EmptyText: string read Get_EmptyText; { Text to show if the macro returns an empty string }
    property FormatText: string read Get_FormatText; { Format string to use for the result of the macro }
    property FindStatus: TMacroFindStatus read Get_FindStatus;
    property OverrideNewLine: string read Get_OverrideNewLine;
  end;

implementation

end.

