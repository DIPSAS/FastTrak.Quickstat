unit CRF.Person.MoveInterface;

interface

uses
  CRF.User.Interfaces,
  CRF.Person.StudyCase.Interfaces;

type
  ICRFStudyCaseTransfer = interface ['{8FA32376-20D1-46E5-8289-AC53334D74FA}']
    function TryTransfer( const AStudyCase: ICRFStudyCase; const AUser: ICRFActiveUser ): boolean;
  end;

implementation

end.
