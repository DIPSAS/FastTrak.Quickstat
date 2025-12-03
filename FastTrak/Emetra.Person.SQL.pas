unit Emetra.Person.SQL;

interface

const
  CMD_ADD_PERSON    = 'EXEC dbo.AddPerson :DOB, :FstName, :MidName, :LstName, :GenderId, :NationalId';
  CMD_UPDATE_PERSON = 'EXEC dbo.UpdatePerson :PersonId, :DOB, :GenderId, :FstName, :LstName, :NationalId';

  SELECT_PERSON = 'SELECT p.* FROM dbo.Person p ';
  TAIL_ORDER_BY = ' ORDER BY p.LstName, p.FstName';

  QRY_PERSON_BY_DOB       = SELECT_PERSON + 'WHERE p.DOB = :DOB' + TAIL_ORDER_BY;
  QRY_PERSON_BY_DOB_NAME  = SELECT_PERSON + 'WHERE p.DOB = :DOB AND p.LstName LIKE :PartialLastName' + TAIL_ORDER_BY;
  QRY_PERSON_BY_ID        = SELECT_PERSON + 'WHERE p.PersonId = :PersonId' + TAIL_ORDER_BY;
  QRY_PERSON_BY_NATID     = SELECT_PERSON + 'WHERE p.NationalId = :NationalId' + TAIL_ORDER_BY;
  QRY_PERSON_BY_LAST_NAME = SELECT_PERSON + 'WHERE p.LstName LIKE :SearchFor' + TAIL_ORDER_BY;

  QRY_PERSON_ID             = 'SELECT PersonId FROM dbo.Person WHERE NationalId=:NationalId';
  QRY_PERSON_ID_BY_DOB_NAME = 'SELECT PersonId FROM dbo.Person WHERE DOB=:DOB AND FstName=:FstName AND LstName=:LstName ORDER BY PersonId DESC';
  QRY_PERSON_NATIONAL_ID    = 'SELECT NationalId FROM dbo.Person WHERE PersonId=:PersonId';
  QRY_PERSON_BY_USERNAME    = 'SELECT PersonId FROM dbo.Person WHERE UserName = :UserName';

  { Table dbo.Person fields }
  TBL_PERSON    = 'Person';
  FLD_PERSON_ID = 'PersonId';
  FLD_DOB       = 'DOB';
  { Name variants }
  FLD_FIRST        = 'FstName';
  FLD_MIDDLE       = 'MidName';
  FLD_LAST         = 'LstName';
  FLD_REVERSE_NAME = 'ReverseName';
  FLD_FULL_NAME    = 'FullName';
  { Other fields }
  FLD_GENDER_ID       = 'GenderId';
  FLD_NATIONAL_ID     = 'NationalId';
  FLD_HPR_NO          = 'HPRNo';
  FLD_GSM             = 'GSM';
  FLD_INITIALS        = 'Initials';
  FLD_EMAIL_ADDRESS   = 'EmailAddress';
  FLD_EMPLOYEE_NUMBER = 'EmployeeNumber';

  { Location }
  FLD_STREET_ADDRESS = 'StreetAddress';
  FLD_POSTAL_CODE    = 'PostalCode';
  FLD_CITY           = 'City';
  FLD_KOMMUNE_NAVN   = 'KommuneNavn';
  FLD_KOMMUNE_NR     = 'KommuneNr';

implementation

end.
