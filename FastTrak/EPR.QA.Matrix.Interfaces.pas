unit EPR.QA.Matrix.Interfaces;

interface

uses
  Graphics, Windows, Classes;

type
  TQaImageType = ( qatNone, qatHeartOrgan, qatPill, qatSyringe, qatBreakfastEgg, qatFemurBone, qatInfusionDrip, qatTablet, qatFormAge );

  ITitleDictionary = interface
    ['{3939CCC2-C18E-46BD-A21F-159D32637FFF}']
    function GetVarTitle( const AVarName: string ): string;
    function GetVarSubtitle( const AVarName: string ): string;
    function GetVarDescription( const AVarName: string ): string;
  end;

  IPersonGridData = interface
    ['{81E49A2C-9A99-48F9-8254-9258E6ABE53C}']
    function GetRowData( const ARow: Integer ): TObject;
    function VarName( const AFieldIndex: Integer ): string;
    function Title( const AFieldIndex: Integer ): string;
    function FieldCount: Integer;
    function FixedRows: Integer;
    function FixedCols: Integer;
    function DataRows: Integer;
  end;

  IPersonGridComponent = interface
    ['{ABD05C3A-095C-4287-A48A-42DF42256998}']
    { Property accessors }
    function Get_FixedCols: Integer;
    function Get_FixedRows: Integer;
    function Get_DataCols: longint;
    function Get_DataRows: longint;
    function Get_Col: longint;
    function Get_Row: longint;
    function Get_RowCount: longint;
    function Get_ColCount: longint;
    function Get_Top: Integer;
    function Get_Left: Integer;
    function Get_DefaultRowHeight: Integer;
    function Get_Canvas: TCanvas;
    procedure Set_DataCols( const AValue: longint );
    procedure Set_DataRows( const AValue: longint );
    { Other members }
    function CellRect( ACol, ARow: longint ): TRect;
    function TryGetObject( const ACol, ARow: longint; out AObject: TObject ): boolean;
    function GridToDataRow( const ARow: longint ): longint;
    function GridToDataCol( const ACol: longint ): longint;
    function IsDataRow( const ARow: longint ): boolean;
    function IsDataCol( const ACol: longint ): boolean;
    procedure Clear;
    procedure Home;
    procedure SetColWidth( const ACol: longint; const AWidth: Integer );
    procedure Adjust( const ACol: longint; const AWidth: Integer );
    procedure SetObject( ACol, ARow: longint; AObject: TObject );
    { Properties }
    property Canvas: TCanvas read Get_Canvas;
    property Col: longint read Get_Col;
    property DefaultRowHeight: Integer read Get_DefaultRowHeight;
    property Row: longint read Get_Row;
    property FixedCols: Integer read Get_FixedCols;
    property FixedRows: Integer read Get_FixedRows;
    property DataCols: longint read Get_DataCols write Set_DataCols;
    property DataRows: longint read Get_DataRows write Set_DataRows;
    property Top: Integer read Get_Top;
    property Left: Integer read Get_Left;
    property RowCount: longint read Get_RowCount;
    property ColCount: longint read Get_ColCount;
  end;

  IBrushColor = interface
    ['{54F7680A-F804-4F95-BBB3-8D21DABD0817}']
    function BrushColor: TColor;
  end;

  IFontColor = interface
    ['{383ACD20-42B7-41AB-8C89-4C71F17F68B5}']
    function FontColor: TColor;
  end;

  ICustomColor = interface
    ['{1DA7442B-90E1-4C8C-9578-EFC009AC9676}']
    procedure SetColor( const AColor: TColor );
  end;

  IEmptyColor = interface
    ['{92A976C0-7DFD-4519-8263-BB5CF7059A41}']
    function EmptyColor: TColor;
  end;

  ICellText = interface
    ['{13BEFE29-3F28-496D-8082-7B49EA740FBC}']
    function CellText: string;
    function AlignLeft: boolean;
    function CellHint: string;
  end;

  IVarName = interface
    ['{90BAC520-818C-4A7E-B99E-1E37FF3815BC}']
    function Get_VarName: string;
    property VarName: string read Get_VarName;
  end;

  IPersonGridRow = interface
    ['{57FC34DA-DE12-45D9-A216-42A4A774C461}']
    { Property Accessors }
    function Get_PersonId: Integer;
    function Get_FullName: string;
    function Get_NationalId: string;
    function Get_DOB: TDate;
    { Other members }
    function AddDatapoint( ADatapoint: TObject ): boolean;
    function AddData( const ARowId: Integer; const ATimestamp: TDateTime; const AVarName: string; const AValue: double ): boolean;
    function GetValue( const AVarName: string; out AValue: double ): boolean;
    { Properties }
    property PersonId: Integer read Get_PersonId;
    property DOB: TDate read Get_DOB;
    property FullName: string read Get_FullName;
    property NationalId: string read Get_NationalId;
  end;

  IPersonGridColumn = interface( IVarName )
    ['{9F0A59D2-BBAB-489C-815A-A945479F47AA}']
    function Get_Title: string;
    function Get_Subtitle: string;
    { Other members }
    property Title: string read Get_Title;
    property Subtitle: string read Get_Subtitle;
  end;

  IGridDataCollector = interface
    ['{DA543E8F-E925-4D2E-943C-2D01152F6616}']
    { Property accessors }
    function Get_Name: string;
    { other members }
    procedure AddToBatch( const AGridRow: IPersonGridRow );
    procedure RunBatch( const AStudyId: Integer );
    function Title: string;
    function VarNames: TStrings;
    function BatchSize: Integer;
    function BatchIsFull: boolean;
    { Properties }
    property Name: string read Get_Name;
  end;

  IDataTarget = interface
    ['{C577E17C-E8A6-48CB-93F2-56AD55408C60}']
    procedure AddData( const ACollector: IGridDataCollector );
  end;

resourcestring
  HDR_BORN = 'Født';
  HDR_NAME = 'Navn';
  HDR_NATIONAL_ID = 'Fødselsnummer';
  HDR_PID = 'PID';

const
  COL_PERSON_ID          = 0;
  COL_PERSON_DOB         = 1;
  COL_PERSON_NATIONAL_ID = 2;
  COL_PERSON_NAME        = 3;

implementation

end.
