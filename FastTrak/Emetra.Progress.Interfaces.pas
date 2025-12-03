unit Emetra.Progress.Interfaces;

interface

type

  TProgressState = ( pssNone, pssWaiting, pssRunning, pssComplete, pssMinorFailure, pssMajorFailure );
  TOnProgress = procedure( const APercentComplete: double ) of object;

  IStatus = interface
    ['{C3F25F6E-92B3-45B5-B26B-16F876EFEF2D}']
    { Accesssors }
    function GetInfo: string;
    procedure SetInfo(const s: string);
    { Other members }
    procedure Done;
    property Info: string read GetInfo write SetInfo;
  end;

  IProgress = interface( IStatus )
    ['{EF5C8790-D9A9-4374-9F24-9981A492A600}']
    { Accessors }
    procedure SetHeader(const s: string);
    procedure SetProgress(const APercentComplete: double);
    { Other members }
    property Header: string write SetHeader;
    property Percent: double write SetProgress;
  end;

  IProgressBar = interface
  ['{EF850771-E0ED-417F-A02D-7AD43F0E4B62}']
    { Accessors }
    function GetPosition: integer;
    procedure SetMax(Value: integer );
    procedure SetMin(Value: integer );
    procedure SetPosition(AValue: integer );
    { Other members }
    property Max: integer write SetMax;
    property Min: integer write SetMin;
    property Position: integer read GetPosition write SetPosition;
  end;

  IProgressGrid = interface ['{23FD701A-7A77-4126-BC19-E15C8EBFC8E9}']
    function AddStep( const ACaption: string ): integer;
    function StepCount: integer;
    procedure ClearSteps;
    procedure FinishStep( const ANewState: TProgressState ); overload;
    procedure FinishStep( const AStepNo: integer; const ANewState: TProgressState ); overload;
    procedure SetMaxValue( const AStepNo, AMaxValue: integer );
    procedure SetProgress( const AStepNo, APosition: integer );
    procedure SetToIdle( const AStepNo: integer );
    procedure SetToWaiting( const AStepNo: integer );
    procedure StartStep( const AStepNo: integer );
    procedure UpdateStepCaption( const AStepNo: integer; const ACaption: string );
  end;

implementation

end.
