unit VMR.Common.Interfaces;

interface

uses
  {Standard}
  Classes, Contnrs;

const
  VERY_DISTANT_FUTURE = 999999;
  VERY_DISTANT_PAST   = 0;

type

  { Empiric list of various types of data fragments from medical records. }

  TEpjFragmentType = ( epjFragmentLabdata, epjFragmentMedication, epjFragmentProblem, epjFragmentNote, qrtVitalSigns,
    epjFragmentProcedureCode, epjFragmentTreatmentGoal, epjFragmentLabDataSet, epjFragmentAdverseDrugReaction, epjFragmentMessage );

  TEpjFragmentSet = set of TEpjFragmentType;

  { Describes how sensitive a particular data fragment is, to support privacy }

  TEpjSensitivity = ( esNeverSensitive, esSensitive, esCanBeSensitive );

  { Sources for data in the Virtual medical record.  Elements starting with
    vmrExternal is from a traditional external EPR system. }

  TVmrDataSource = ( vmrNone,

    vmrExternalLab, { Labdata from external EPR system }
    vmrExternalProblem, { Problems/diagnoses from external EPR system }
    vmrExternalMedication, { Prescriptions for external EPR system }
    vmrExternalNotes, { Clinical notes from external EPR system }
    vmrExternalHistory, { Unstructured data about medical history }
    vmrExternalProcedures, { Procedure codes from external EPR system }
    vmrExternalVitals, { Vitals from external EPR system }

    vmrCrfItem, { Item data from CRF system }
    vmrCrfForm, { Form data from CRF system }
    vmrLog, { Log data from database }
    vmrMedication, { Drug treatment from CRF system }
    vmrProblem, { Problems/diagnoses from CRF system }
    vmrLab, { Lab data from CRF system }
    vmrAdverseDrugReactions, { Adverse drug reactions from CRF system }
    vmrCalculated, { Calculated data, like a risk score }

    vmrReminders, { Reminders or decision support }
    vmrNLP, { Structured data extracted from text with Natural Language Processing }
    vmrAbstraction, { Abstractions, like ex-smoker, based on other data elements }
    vmrMessages, { Data sent from other systems, e.g. discharge notes }
    vmrTemp ); { Used for temporary data, should not be stored or displayed }

  IVmrMatcher = interface
    ['{F1E452CD-A60D-4753-B161-7EDA81B60BD8}']
    function IsMatch( const AInput: string ): boolean;
  end;

  IVmrFragmentTime = interface
    ['{5A789AD9-219B-4785-83CB-52949D7C4891}']
    { Property Accessors }
    function Get_Timestamp: TDateTime;
    { Other members }
    property Timestamp: TDateTime read Get_Timestamp;
  end;

  { Fragment rows can trigger events to update a view }
  IVmrFragmentRow = interface( IVmrFragmentTime )
    ['{CE0FB541-6513-4903-89FA-AF55606ABA62}']
    { Property accessors }
    function Get_OnChange: TNotifyEvent;
    procedure Set_OnChange( Value: TNotifyEvent );
    { Other members }
    procedure TriggerChange;
    function AsHtmlRow: string;
    { Properties }
    property OnChange: TNotifyEvent read Get_OnChange write Set_OnChange;
  end;

  IVmrFragment = interface( IVmrFragmentTime )
    ['{9A7B026B-BC98-4650-A575-4D0A2EE47B68}']
    { Property Accessors }
    function Get_FragmentType: TEpjFragmentType;
    function Get_ShowDate: boolean;
    procedure Set_ShowDate( AValue: boolean );
    { Other members }
    function AsHtml: string;
    function AsString: string;
    function LongAgo( const ATimeSpan: double ): boolean;
    function Match( const ARegEx: string ): boolean;
    function MatchAgain: boolean;
    function Recent( const ATimeSpan: double ): boolean;
    function SameData( AOtherEntry: IVmrFragment ): boolean;
    { Properties }
    property FragmentType: TEpjFragmentType read Get_FragmentType;
    property ShowDate: boolean read Get_ShowDate write Set_ShowDate;
  end;

  IVmrExcludableFragment = interface['{A5E0E80F-F7D1-447B-955B-96629D087228}']
    { Property accessors }
    function  Get_IsExcluded: boolean;
    { Properties }
    property IsExcluded: boolean read Get_IsExcluded;
  end;

const
  { Sort order for TEpjFragmentType }
  VMR_SORT_ORDER: array [TEpjFragmentType] of integer = ( 6, 9, 8, 2, 3, 1, 4, 5, 7, 10 );

  { Names for enumerations }

  VMR_SOURCE_IDS: array [TVmrDataSource] of string = ( '-', 'ELab', 'EDx', 'ERx', 'ENote', 'EHist', 'EProc', 'EVit', 'Item',
    'Form', 'Log', 'CRx', 'CDx', 'CLab', 'ADR', 'Calc', 'NB', 'NLP', 'Abs', 'Msg', 'Temp' );

  { Sensitivity of various fragment types in the EPR }
  VMR_SENSITIVITY: array[TEpjFragmentType] of TEpjSensitivity = ( esCanBeSensitive, esCanBeSensitive, esNeverSensitive,
    esSensitive, esNeverSensitive, esNeverSensitive, esNeverSensitive, esNeverSensitive, esNeverSensitive, esSensitive );

implementation

end.
