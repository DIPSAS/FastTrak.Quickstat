unit Emetra.Profession.Interfaces;

interface

type
  IProfession = interface['{F9026BF3-C949-43F6-8DE8-111057362302}']
    { Property accessors }
    function Get_ProfessionName: string;
    function Get_ProfType: string;
    { Properties }
    property ProfessionName: string read Get_ProfessionName;
    property ProfType: string read Get_ProfType;
  end;

implementation

end.
