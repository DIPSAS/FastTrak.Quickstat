unit CRF.Population.Interfaces;

interface

uses
  Emetra.Person.Interfaces;

type
  IPopulation = interface
    ['{256623AE-821E-47E5-B0C6-A1A9DD1FBCAA}']
    { Property accessors }
    function Get_Title: string;
    function Get_Group: string;
    function Get_InfoCaption: string;
    function Get_ProcId: integer;
    function Get_QueryText: string;
    function Get_SourceCode: string;
    { Other members }
    property Title: string read Get_Title;
    property Group: string read Get_Group;
    property InfoCaption: string read Get_InfoCaption;
    property ProcId: integer read Get_ProcId;
    property QueryText: string read Get_QueryText;
    property SourceCode: string read Get_SourceCode;
  end;

  IPopulationList = interface
    ['{A7F311E3-AA68-4A6D-A672-48DC905E1C84}']
  end;

  IPopulationObserver = interface
    ['{086A34D1-C4FC-4728-A004-3EB268FF25D1}']
    procedure AfterPopulationSelect( APopulation: IPopulation );
  end;

const
  { Populations }
  QRY_POPULATIONS                    = 'EXEC dbo.GetPopulations :StudyId';
  QRY_STUDY_POPULATIONS_NO_VERSION   = 'EXEC Populations.GetStudyPopulations :StudyId';
  QRY_STUDY_POPULATIONS_WITH_VERSION = 'EXEC Populations.GetStudyPopulations :StudyId, :DbVer';
  QRY_POPULAR_POPULATIONS            = 'EXEC Populations.GetPopularPopulations :StudyId, :DbVer';

const
  FLD_PROC_ID      = 'ProcId';
  FLD_PROC_GROUP   = 'ProcGroup';
  FLD_PROC_TITLE   = 'ProcTitle';
  FLD_PROC_DESC    = 'ProcDesc';
  FLD_HELP_TEXT    = 'HelpText';
  FLD_INFO_CAPTION = 'InfoCaption';
  FLD_SOURCE_CODE  = 'ProcSourceCode';
  FLD_SQL_TEXT     = 'SqlText';

implementation

end.
