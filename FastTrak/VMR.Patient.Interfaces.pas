unit VMR.Patient.Interfaces;

interface

uses
  Emetra.Person.Interfaces;

type
  IPatient = interface( IPersonReadOnly )
    ['{4668C6B3-9211-427C-9D5F-0E08389688AF}']
    { Property Accessors }
    function Get_Person: IPersonReadOnly;
    function Get_YearsOld: integer;
    { Other members }
    procedure ClearValues;
    function TryGetValue( const AVarName: string; var AValue: Variant ): boolean;
    property Person: IPersonReadOnly read Get_Person;
    property YearsOld: integer read Get_YearsOld;
  end;

  IPatientEditObserver = interface
    ['{72AFED92-76C2-45B0-B1D3-75FA9EF1EA8F}']
    procedure AfterEdit( Sender: IPersonId );
  end;

implementation

end.
