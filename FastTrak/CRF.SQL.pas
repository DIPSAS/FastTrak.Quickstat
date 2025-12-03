unit CRF.SQL;

interface

const

  CMD_CDSS_RESPOND        = 'EXEC dbo.AddAlertResponse :AlertId, :ResponseChar';
  CMD_CHECK_SCRIPT_RIGHTS = 'EXEC dbo.CheckScriptPermission';
  CMD_UPDATE_DEFAULT_POP  = 'EXEC dbo.UpdateDefaultPopulation :StudyId, :ProcId';

  QRY_AMBIGUOUS_DATA     = 'EXEC dbo.GetAmbiguousData';
  QRY_FIELD_SIZE         = 'EXEC dbo.DbGetTextFieldSize :Table, :Field';
  QRY_GET_CLIN_TOUCH     = 'EXEC dbo.GetClinTouch :SessId, :PersonId, :EventNum, :MetaFormId';
  QRY_IMPORT_CONTEXT     = 'EXEC dbo.OpenImportContext :StudyName, :ContextName';
  QRY_IMPORT_CTX_UPDATE  = 'SELECT MAX(LastUpdate) AS LastUpdate FROM dbo.ImportContext WHERE ContextName=:ContextName';
  QRY_NOM_MATCH          = 'EXEC dbo.GetNomMatch :MatchStr, :ListId';
  QRY_MY_PROFESSIONS     = 'EXEC dbo.GetProfessions';
  QRY_ACTIVE_PROFESSIONS = 'EXEC dbo.GetActiveProfessions';

  { Studies }
  QRY_STUDY_ID   = 'SELECT StudyId FROM dbo.Study WHERE StudName=:StudyName';
  QRY_STUDY_NAME = 'SELECT StudName FROM dbo.Study WHERE StudyId=:StudyId';
  QRY_ALL_IDS    = 'SELECT PersonId FROM dbo.StudCase WHERE StudyId=:StudyId';
  QRY_MY_STUDIES = 'EXEC CRF.GetMyStudies :CurrentStudyId';

  { Batch import/export }
  CMD_CLOSE_IMPORT_CONTEXT = 'EXEC dbo.CloseImportContext :ContextId, :LastUpdate';
  CMD_UPDATE_BATCH_ERRORS  = 'EXEC dbo.UpdateBatchErrors :BatchId, :ErrCount, :ErrText';
  CMD_UPDATE_BATCH_DATA    = 'EXEC dbo.UpdateBatchData :BatchId, :BatchData';

  { Communications }
  CMD_SEND_MESSAGE = 'EXEC Comm.SendMessage :ClinFormId, :PartnerId, :MessageText, :MsgGuid';

  { Alerts - Clinical Decision Support }
  CMD_CDSS_UPDATE   = 'EXEC dbo.UpdateDSSAlerts :StudyId, :PersonId';
  QRY_CDSS_RETRIEVE = 'EXEC dbo.GetAlertsByPerson :StudyId, :PersonId';
  QRY_CDSS_RULE_LAG = 'EXEC dbo.GetRuleLag :StudyId, :PersonId';

  { Centers, groups, status and person responsible for the medical record }
  CMD_ADD_STUDY_CENTER    = 'EXEC dbo.AddStudyCenter :CenterName';
  CMD_ADD_STUDY_GROUP     = 'EXEC dbo.AddStudyGroup :GroupId, :GroupName, :CenterId';
  CMD_DISABLE_STUDY_GROUP = 'EXEC dbo.DisableStudyGroup :StudyGroupId';
  QRY_ALL_CENTERS         = 'EXEC dbo.GetAllStudyCenters';
  QRY_GROUPS              = 'EXEC dbo.GetStudyGroups :StudyId, :UserId';
  QRY_STUDYCASELOG        = 'EXEC dbo.GetStudyCaseLog :StudyId, :PersonId';

  { Event merging }
  QRY_MERGABLE_DATA   = 'EXEC dbo.GetDuplicatedData';
  CMD_MERGE_EVENT     = 'EXEC dbo.UtilMergeEvent :EventId';
  QRY_MERGABLE_EVENTS = 'EXEC dbo.UtilEventsToMerge';

  { Relations }
  QRY_ENABLED_RELATIONS  = 'EXEC dbo.GetEnabledRelations';
  QRY_DISABLED_RELATIONS = 'EXEC dbo.GetDisabledRelations';
  CMD_DISABLE_RELATION   = 'EXEC dbo.DisableRelation :RelId';
  CMD_ENABLE_RELATION    = 'EXEC dbo.EnableRelation :RelId';
  QRY_GET_RELATIONS      = 'EXEC dbo.GetUserRelations';
  CMD_ADD_RELATION       = 'EXEC dbo.AddRelation :PersonId, :RelationId';

  { Metadata for items and forms }
  QRY_STUDY_ITEMS     = 'EXEC CRF.GetStudyItems :StudyId';
  QRY_STUDY_ANSWERS   = 'EXEC CRF.GetStudyAnswers :StudyId';
  QRY_STUDY_FORMS     = 'EXEC CRF.GetMetaForms :StudyId';
  QRY_FORM_PRIVILEGES = 'EXEC CRF.GetMetaFormProfessionPrivileges';

  { User }
  CMD_ADD_SQL_USER         = 'EXEC dbo.AddUser :Username, :Password';
  CMD_ADD_DOMAIN_USER      = 'EXEC AdminTool.AddUser :UserName, :ProfId, :CenterId';
  QRY_MY_STUDYUSER         = 'EXEC dbo.GetStudyAndUser :StudyName';
  QRY_ANY_STUDY_USER       = 'EXEC dbo.GetStudyUser :StudyId, :UserId';
  CMD_DELETE_USER          = 'EXEC dbo.DeleteUser :UserName';
  CMD_UPD_PROFESSION       = 'EXEC dbo.UpdateUserProfession :UserId, :ProfId';
  CMD_UPD_USER_CENTER      = 'EXEC dbo.UpdateUserCenter :UserId, :CenterId';
  CMD_UPD_USER_GROUP       = 'EXEC dbo.UpdateUserGroup :StudyId, :UserId, :GroupId';
  CMD_UPD_USER_PERSON      = 'EXEC dbo.UpdateUserPerson :UserId, :PersonId';
  QRY_USER_DATA            = 'EXEC dbo.GetUserDetails :UserName, :StudyId';
  QRY_USER_ID              = 'SELECT USER_ID( :UserName )';
  QRY_USER_ROLES           = 'SELECT IS_MEMBER(''db_owner'') AS IsDbOwner,IS_MEMBER(''superuser'') AS IsSuperuser';
  QRY_USER_HAS_CASE_ACCESS = 'SELECT AccessCtrl.UserHasCaseAccess( :UserId, :PersonId )';

  { Access }
  CMD_REVOKE_DBACCESS     = 'EXEC sp_revokedbaccess :UserName';
  CMD_GRANT_DBACCESS      = 'EXEC sp_grantdbaccess :UserName';
  CMD_GRANT_FASTTRAK_ROLE = 'ALTER ROLE [FastTrak] ADD MEMBER [%s]';

  { Single Clinical form data }
  QRY_STUDYFORM_DATA   = 'EXEC CRF.GetStudyForm :StudyId,:FormId';
  QRY_PAGE_DATA        = 'EXEC CRF.GetFormPages :FormId';
  QRY_ACTION_DATA      = 'EXEC CRF.GetFormActions :FormId';
  QRY_ITEM_DATA        = 'EXEC CRF.GetFormItems :FormId';
  QRY_CARRY_FORWARD    = 'EXEC CRF.GetFormCarryExceptions';
  QRY_ITEM_LAB_MAPPING = 'EXEC CRF.GetItemToLabMapping';

  { Clinical form }
  CMD_ADD_CLINFORM                = 'EXEC dbo.AddClinForm :SessId, :PersonId, :FormId, :EventTime';
  CMD_DEL_CLINFORM                = 'EXEC dbo.DeleteClinForm :ClinFormId';
  QRY_CAN_SIGN_CLIN_FORM          = 'EXEC CRF.CanSignClinForm :ClinFormId';
  QRY_CAN_UNSIGN_CLIN_FORM        = 'EXEC CRF.CanUnsignClinForm :ClinFormId';
  QRY_CLINFORM_SINGLE             = 'EXEC CRF.GetClinForm :ClinFormId';
  QRY_CLINFORM_LIST               = 'EXEC CRF.GetClinFormList :StudyId, :PersonId, :IncludeArchived';
  QRY_DELETED_FORMS               = 'EXEC dbo.GetDeletedClinForms :StudyId, :PersonId';
  CMD_UPD_CLINFORM_ARCHIVE_STATUS = 'EXEC CRF.UpdateClinFormArchiveStatus :ClinFormId, :ArchiveStatus';
  CMD_UNDELETE_CLIN_FORM          = 'EXEC dbo.UndeleteClinForm :ClinFormId';
  CMD_UNSIGN_FORM                 = 'EXEC CRF.UpdateClinFormUnsign :ClinFormId';
  CMD_UPD_CLINFORM_CACHE          = 'EXEC dbo.UpdateFormText :EventId, :FormId, :HtmlText';
  CMD_UPD_CRF_CLINFORM_STATUS     = 'EXEC CRF.UpdateClinForm :ClinFormId, :FormComment, :FormStatus, :FormComplete';
  CMD_UPD_CRF_CLINFORM_SIGN       = 'EXEC CRF.UpdateClinFormSign :ClinFormId, :FormComment, :FormComplete, :SessionId';
  CMD_MOVE_CLINFORM               = 'EXEC dbo.UpdateClinFormSetTime :ClinFormId, :NewEventTime';
  CMD_ADD_CRF_CLINDATA            = 'EXEC CRF.AddClinDatapoint :TouchId, :EventId, :ItemId, :Quantity, :DTVal, :EnumVal, :TextVal, :Locked';
  CMD_ADD_CRF_DATEDATA            = 'EXEC CRF.AddClinDataDate :TouchId, :ItemId, :DTVal';
  CMD_ADD_CRF_ENUMDATA            = 'EXEC CRF.AddClinDataEnum :TouchId, :ItemId, :EnumVal';
  CMD_ADD_CRF_QUANTITY            = 'EXEC CRF.AddClinDataQuantity :TouchId, :ItemId, :Quantity';
  CMD_ADD_CRF_TEXTVAL             = 'EXEC CRF.AddClinDataTextVal :TouchId, :ItemId, :TextVal';
  QRY_GET_CRF_CLINDATA            = 'EXEC CRF.GetClinData :StudyId, :PersonId';
  QRY_CRF_SINGLE_FORM             = 'EXEC CRF.GetSingleFormData :ClinFormId, :LastUpdate';
  QRY_CRF_UPDATE_FORM_XML         = 'EXEC CRF.UpdateClinFormData :SessId, :ClinFormId, :FormData';

  { Threaded data }
  QRY_ADD_THREADED_DATA    = 'EXEC CRF.AddClinThreadData :TouchId,:ThreadId,:ItemId,:Quantity,:DTVal,:EnumVal,:TextVal';
  CMD_UPDATE_THREADED_DATA = 'EXEC CRF.UpdateClinThreadData :RowId,:TouchId,:Quantity,:DTVal,:EnumVal,:TextVal';
  CMD_LOCK_CLINTHREAD_ROW  = 'EXEC CRF.UpdateClinThreadLockRow :RowId';
  QRY_GET_THREADED_DATA    = 'EXEC CRF.GetClinThreadData :ThreadId';

  { Person }
  QRY_PERSON_DETAILS  = 'EXEC dbo.GetPersonDetails :SessId, :PersonId';
  CMD_UPDATE_GSM      = 'EXEC dbo.UpdatePersonGSM :PersonId, :GSM';
  CMD_UPDATE_HPR      = 'EXEC dbo.UpdatePersonHPRNo :PersonId, :HPRNo';
  CMD_UPDATE_TESTCASE = 'EXEC dbo.UpdatePersonTestCase :PersonId, :TestCase';

  CMD_UPDATE_NATIONAL_ID = 'EXEC dbo.UpdatePersonSetNationalId :PersonId, :NationalId';
  CMD_ADD_GROUP_RELATION = 'EXEC dbo.AddClinRelationToGroup :StudyId,:GroupId,:RelId';
  CMD_ADD_STUDY_CASE     = 'EXEC dbo.AddStudCase :StudyId,:PersonId';
  CMD_CLEAR_RELATIONS    = 'EXEC dbo.ExpireRelation :PersonId';
  CMD_UPD_CASE_ADOPT     = 'EXEC dbo.UpdateCaseAdopt :StudyId,:PersonId';
  CMD_UPD_JOURNALANSVAR  = 'EXEC dbo.UpdateCaseJournalansvar :StudyId,:PersonId';
  CMD_UPD_GROUP          = 'EXEC dbo.UpdateCaseGroup :StudyId,:PersonId,:GroupId';
  CMD_UPD_STATUS         = 'EXEC dbo.UpdateCaseStatus :StudyId,:PersonId,:StatusId';
  CMD_STUDYCASE_TRANSFER = 'EXEC dbo.UpdateCaseTransfer :StudyId, :PersonId, :GroupId, :StatusId';
  QRY_ACTIVE_ANONYMOUS   = 'EXEC dbo.GetCaseListAnonymous :StudyId';
  QRY_STUDY_STATUS       = 'EXEC dbo.GetStudyStatus :StudyId';
  QRY_MY_PATIENTS        = 'EXEC dbo.GetCaseListMyRelations :StudyId';
  QRY_LOAD_STUDYCASE     = 'EXEC CRF.GetStudyCase :StudyId, :PersonId';

  { Current user }
  CMD_ADD_MYSELF        = 'EXEC dbo.AddMyself :DOB, :GenderId, :FstName, NULL, :LstName, :NationalId';
  CMD_ADD_MY_CENTER     = 'EXEC dbo.AddMyCenter :CenterId';
  CMD_ADD_MY_PROFESSION = 'EXEC dbo.AddMyProfession :ProfId';
  CMD_CHG_MY_PASSWORD   = 'EXEC dbo.ChangePassword :OldPassword, :NewPassword';
  QRY_MY_STUDY_GROUPS   = 'EXEC dbo.GetStudyGroups :StudyId';
  QRY_MY_META_FORMS     = 'EXEC CRF.GetMyMetaForms :StudyId, :MyProfession, :PersonId';
  QRY_MY_ACTIVITY       = 'EXEC dbo.GetMyActivity :StudyId';
  QRY_MY_FAVORITE_FORMS = 'EXEC dbo.GetMyFavoriteForms :StudyId';
  QRY_MY_CENTERS        = 'EXEC dbo.GetStudyCenters';
  CMD_UPD_MY_CENTER     = 'EXEC dbo.UpdateMyCenter :CenterId';
  CMD_UPD_MY_GROUP      = 'EXEC dbo.UpdateMyGroup :StudyId, :GroupId';
  CMD_UPD_MY_PROFESSION = 'EXEC dbo.UpdateMyProfession :ProfId';
  CMD_UPD_SHOW_MY_GROUP = 'EXEC dbo.UpdateShowMyGroup :StudyId, :ShowMyGroup';

  { Session }
  QRY_ADD_SESSION   = 'EXEC dbo.AddSession :StudyId,:CompName,:CompUser,:CompTime,:AppVer';
  CMD_CLOSE_SESSION = 'EXEC dbo.CloseSession :SessId,:Updates,:Inserts';

  { Study }
  CMD_ADD_STUDY          = 'EXEC dbo.AddStudy :StudyName';
  QRY_STUDY_NAMES        = 'SELECT StudyId,StudyName FROM dbo.Study WHERE StudyId>0';
  QRY_STUDY_CENTER_COUNT = 'SELECT COUNT(*) FROM dbo.StudyCenter';

  { StudyCase }
  CMD_TOUCH_STUDY_CASE = 'EXEC dbo.TouchStudyCase :StudyId, :PersonId';
  QRY_GET_CASELIST     = 'EXEC dbo.GetCaseList :StudyId';
  QRY_TESTCASES        = 'EXEC dbo.GetCaseListTest :StudyId';

  { Logging }
  CMD_LOG_POPULATION_CHANGE = 'EXEC dbo.AddPopulationLog :StudyId, :ProcId, :ProcDesc, :ElapsedMs';
  CMD_LOG_SPECIAL_ACCESS    = 'EXEC dbo.AddSpecialAccessLog :PersonId, :Justification';
  CMD_LOG_ADD_REPORT_EVENT  = 'EXEC dbo.AddReportEvent :ReportName, :PersonId';

  { Logging for audit purposes }
  CMD_CLOSE_EVENT = 'EXEC AuditLog.AddCaseClosedEvent :ReadEventGuid';
  CMD_OPEN_EVENT  = 'EXEC AuditLog.AddCaseOpenedEvent :ReadEventGuid, :PersonId, :ClinRelId';

  { PROM-data }
  CMD_ADD_KIOSK_FORM = 'EXEC PROM.AddKioskForm :FormOrderId, :ClinFormId, :FormTag';

  { Fields }
  FLD_STUDY_NAME = 'StudyName';

implementation

end.
