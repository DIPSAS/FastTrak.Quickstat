unit Emetra.Reporting.Interfaces;

interface

uses
  Classes, Vcl.ComCtrls;

type
  TReportTextFormat = ( nfPlain, nfHtml, nfSimpleHtml, nfRTF );

  IHtmlReportEngine = interface
    ['{D77F2A9A-6429-4061-BACF-1F3CB0F99BD2}']
    procedure ShowReport( const ATemplateFile: string; AStrings: TStrings );
  end;

  IHtmlTagReplacer = interface
    ['{F22F8A82-2868-4A42-868A-ECC12862275B}']
    function GetReplacement( const ATagName: string; AParams: TStrings ): string;
  end;

  IPrintReportEngine = interface
    ['{EB77ACC5-159A-4DD8-99A0-CC3B1059E26D}']
    { Property accessors }
    function Get_AfterPrint: TNotifyEvent;
    procedure Set_AfterPrint( const Value: TNotifyEvent );
    { Other memberse }
    function ReportExists( const AReportName: string ): boolean;
    function ShowReport( const AReportName: string ): boolean;
    procedure Print;
    { Properties }
    property AfterPrint: TNotifyEvent read Get_AfterPrint write Set_AfterPrint;
  end;

  IPrintReportDesigner = interface
    ['{C737FE42-2266-4A61-840E-FE581F9C988C}']
    procedure EditReport( const AFileName: string );
  end;

  IPrintReportEngineReset = interface
    ['{32A3E7A9-09CB-4693-A858-3A9C481D16D8}']
    procedure ReportEngineReset( const AFormat: TReportTextFormat );
  end;

  IPrintHandler = interface
    ['{B7A3DA03-AC67-4315-A422-D4322C048BF8}']
    function PrintFile( const AFileName: string ): boolean;
  end;

  ITextProcessor = interface
    ['{93361168-0506-4497-BFA9-EF93E80B2847}']
    function ProcessText( const s: string ): string;
  end;

  INoteGenerator = interface
    ['{BD851BD4-A5DE-4231-8B2D-696700394017}']
    function TryPrepareNote( Sender: TObject ): boolean;
  end;

implementation

end.
