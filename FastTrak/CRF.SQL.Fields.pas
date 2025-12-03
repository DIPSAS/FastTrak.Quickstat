unit CRF.SQL.Fields;

interface

const
  { Fields }
  FLD_CASELIST    = 'CaseList';
  FLD_BLOCK_RULES = 'BlockRules';

  { Study }
  FLD_STUDY_ID = 'StudyId';

  { StudyCenter }
  FLD_CENTER_ADDRESS  = 'CenterAddress';
  FLD_CENTER_CITY     = 'CenterCity';
  FLD_CENTER_ID       = 'CenterId';
  FLD_CENTER_NAME     = 'CenterName';
  FLD_CENTER_PHONE    = 'CenterPhone';
  FLD_CENTER_POSTCODE = 'CenterPostcode';

  { StudyGroup }
  FLD_GROUP_ID   = 'GroupId';
  FLD_GROUP_NAME = 'GroupName';
  FLD_STUDY_GROUP_ID = 'StudyGroupId';

  { StudyStatus }
  FLD_STATUS_ID     = 'StatusId';
  FLD_STATUS_ACTIVE = 'StatusActive';
  FLD_STATUS_TEXT   = 'StatusText';

  { ClinEvent }
  FLD_EVENT_ID  = 'EventId';
  FLD_EVENT_NUM = 'EventNum';

  { ClinDataPoint }
  FLD_ROW_ID = 'RowId';

  { ClinForm }
  FLD_CLINFORM_ID = 'ClinFormId';

  { ClinTouch }
  FLD_TOUCH_ID = 'TouchId';

  { MetaProfession }
  FLD_PROF_ID   = 'ProfId';
  FLD_PROF_NAME = 'ProfName';
  FLD_PROF_TYPE = 'ProfType';

  { MetaRelation }
  FLD_RELATION_ID   = 'RelId';
  FLD_RELATION_NAME = 'RelName';

  { UserList }
  FLD_SHOW_MY_GROUP = 'ShowMyGroup';
  FLD_SIGNATURE     = 'Signature';
  FLD_USER_ID       = 'UserId';
  FLD_USER_NAME     = 'UserName';

  { UserProperties }
  FLD_SUPERUSER      = 'IsSuperuser';
  FLD_DB_OWNER       = 'IsDbOwner';
  FLD_SINGLE_GROUP_USER   = 'IsSingleGroupUser';
  FLD_RELATION_COUNT = 'RelationCount';

  { StudyCase }
  FLD_JA_NAME          = 'JournalansvarligNavn';
  FLD_JA               = 'Journalansvarlig';
  FLD_CLIN_RELATION_ID = 'ClinRelId';
  FLD_TEST_CASE        = 'TestCase';

implementation

end.
