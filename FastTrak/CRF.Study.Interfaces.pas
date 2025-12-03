unit CRF.Study.Interfaces;

interface

type
  IStudyId = interface['{994D576B-107A-4CBC-A0F8-B99C54C3B0F0}']
    { Accessors }
    function Get_StudyId: integer;
    function Get_StudyName: string;
    { Other members }
    property StudyId: integer read Get_StudyId;
    property StudyName: string read Get_StudyName;
  end;

  IStudyObserver = interface ['{BC2F61A0-B490-4B6F-A082-C9FDF284BF4B}']
    function GetNamePath: string;
    procedure AfterStudyChange( const Sender: IStudyId );
  end;

  IStudyFolder = interface['{C01CB49A-3F94-4468-930F-6F23366A0663}']
    { Accessors }
    function Get_Root: string;
    function Get_StudyName: string;
    { Other members }
    property Root: string read Get_Root;
    property StudyName: string read Get_StudyName;
  end;

  IStudySelector = interface( IStudyId ) ['{C41842A3-A273-464E-B560-C37B99A1592A}']
    procedure SelectStudy( Sender: TObject );
    procedure Refresh;
  end;

implementation

end.
