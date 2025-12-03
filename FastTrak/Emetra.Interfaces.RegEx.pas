unit Emetra.Interfaces.RegEx;

interface

type

  IRegExEngine = interface(IInterface)
    ['{6DF2C75E-6998-421E-AF53-2F00C5B2190C}']
    { Accessors }
    function GetMatchedExpression: string;
    function GetSubject: string;
    function GetReplacement: string;
    procedure SetReplacement(const AReplacement: string);
    procedure SetRegEx(const ARegEx: string);
    procedure SetSubject(const ASubject: string);
    { Other methods }
    function Match: boolean;
    function MatchAgain: boolean;
    function ReplaceAll: boolean;
    procedure Study;
    property MatchedExpression: string read GetMatchedExpression;
    property RegEx: string write SetRegEx;
    property Replacement: string read GetReplacement write SetReplacement;
    property Subject: string read GetSubject write SetSubject;
  end;

implementation

end.
