unit CRF.Person.Interfaces;

interface

uses
  Emetra.Person.Interfaces;

type
  ICRFPerson = interface( IPersonReadOnly )
    ['{281E1331-8005-448B-B84D-A49863E43D6F}']
    { Property accessors }
    function Get_Pronoun: string;
    function Get_PronounObjective: string;
    function Get_ReverseName: string;
    { Properties }
    property PronounObjective: string read Get_PronounObjective;
    property Pronoun: string read Get_Pronoun;
    property ReverseName: string read Get_ReverseName;
  end;

implementation

end.
