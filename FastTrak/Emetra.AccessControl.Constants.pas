/// <summary>
/// This unit should contain all relevant database roles and professions that
/// are necessary to control access to function points. All function points should also be listed here.
/// </summary>
/// <seealso cref="Emetra.AccessControl.AccessControlManager" />
unit Emetra.AccessControl.Constants;

interface

const
  ROLE_DB_ACCESSADMIN   = 'db_accessadmin';
  ROLE_DB_DDLADMIN      = 'db_ddladmin';
  ROLE_DB_OWNER         = 'db_owner';
  ROLE_DB_SECURITYADMIN = 'db_securityadmin';
  ROLE_DB_DATAREADER    = 'db_datareader';
  ROLE_DB_DATAWRITER    = 'db_datawriter';

  { Tekniske roller }
  ROLE_ADMINISTRATOR = 'Administrator';
  ROLE_SUPERUSER     = 'superuser';
  ROLE_DATA_IMPORT   = 'DataImport';
  ROLE_SUPPORT       = 'Support';

  { Kameleon roller }
  ROLE_CHANGEPROFESSION = 'ChangeProfession';
  ROLE_CHANGEWORKSITE   = 'ChangeWorksite';

  { Roller etter plass i organisasjonen }
  ROLE_LEDER          = 'Leder';
  ROLE_AVDELINGSLEDER = 'Avdelingsleder';
  ROLE_GRUPPELEDER    = 'Gruppeleder';

  { Roller som svarer til yrker }
  ROLE_LEGE              = 'Lege';
  ROLE_SYKEPLEIER        = 'Sykepleier';
  ROLE_VERNEPLEIER       = 'Vernepleier';
  ROLE_HELSESEKRETÆR     = 'Helsesekretær';
  ROLE_FARMASØYT         = 'Farmasøyt';
  ROLE_RESEPTARFARMASØYT = 'Reseptarfarmasøyt';
  ROLE_PROVISORFARMASØYT = 'Provisorfarmasøyt';
  ROLE_FOTTERAPEUT       = 'Fotterapeut';

  { Roller som noen yrker typisk har }
  ROLE_DRUG_EDITOR        = 'DrugEditor';
  ROLE_JOURNALANSVARLIG   = 'Journalansvarlig';
  ROLE_PRINT_PRESCRIPTION = 'PrintPrescription';
  ROLE_INNSKRIVING        = 'Innskriving';
  ROLE_LABENTRY           = 'LabEntry';

  { Andre roller }
  ROLE_QUICKSTAT  = 'QuickStat';
  ROLE_RESEARCHER = 'Researcher';
  ROLE_FASTTRAK   = 'FastTrak';
  ROLE_SINGLEGROUP= 'SingleGroup';

  { Profesjoner }
  PROF_ASSISTENT              = 'ASS';
  PROF_BIOINGENIØR            = 'BI';
  PROF_ELEV                   = 'ELE';
  PROF_ERGOTERAPEUT           = 'ET';
  PROF_FOTTERAPEUT            = 'FO';
  PROF_FYSIOTERAPEUT          = 'FT';
  PROF_HELSEFAGARBEIDER       = 'HF';
  PROF_HELSESEKRETÆR          = 'HE';
  PROF_HJELPEPLEIER           = 'HP';
  PROF_LEGE                   = 'LE';
  PROF_LEVERANDØR             = 'SUP';
  PROF_MEDISINSK_IT_PERSONELL = 'MIT';
  PROF_MUSIKKTERAPEUT         = 'MUS';
  PROF_OMSORGSARBEIDER        = 'OA';
  PROF_PREST                  = 'PR';
  PROF_RESEPSJONIST           = 'REC';
  PROF_STYRER                 = 'STY';
  PROF_SYKEPLEIER             = 'SP';
  PROF_VERNEPLEIER            = 'VP';
  PROF_PSYKOLOG               = 'PS';
  PROF_PROVISORFARMASØYT      = 'FA1';
  PROF_RESEPTARFARMASØYT      = 'FA2';

  /// <summary>
  /// The user can take the role of Journalansvarlig for a patient.
  /// </summary>
  FUNC_PATIENT_JOURNALANSVARLIG_CHANGE = 'PATIENT.JOURNALANSVARLIG.CHANGE';
  /// <summary>
  /// The user may change a person's NationalId.
  /// </summary>
  FUNC_ADMIN_NATIONALID_CHANGE = 'ADMIN.NATIONALID.CHANGE';
  /// <summary>
  /// The user can add other users to the database.
  /// </summary>
  FUNC_ADMIN_USERS_ADD = 'ADMIN.USERS.ADD';
  /// <summary>
  /// The user can add new patients to the database.
  /// </summary>
  FUNC_ADMIN_PATIENTS_ADD = 'ADMIN.PATIENTS.ADD';
  /// <summary>
  /// The user has access to the Superuser menu.
  /// </summary>
  FUNC_ADMIN_SUPERUSER_CHANGE = 'ADMIN.SUPERUSER.CHANGE';
  /// <summary>
  /// The user can edit critical information about the patient (CAVE).
  /// </summary>
  FUNC_ADMIN_CAVE_EDIT = 'ADMIN.CAVE.EDIT';
  /// <summary>
  /// The user can designate a particular patient as a test-patient, or
  /// remove the <b>TestCase</b> attribute from test-patient
  /// </summary>
  FUNC_ADMIN_TESTPATIENTS_CHANGE = 'ADMIN.TESTPATIENTS.CHANGE';
  /// <summary>
  /// The user can view the source code for a population.
  /// </summary>
  FUNC_POPULATION_SOURCE = 'ADMIN.POPULATION.SOURCE';
  /// <summary>
  /// The user can start the <b>LabEntry</b> application.
  /// </summary>
  FUNC_START_LABENTRY_APP = 'LABDATA.APP.START';
  /// <summary>
  /// The user can sign lab results, usually a role for physicians.
  /// </summary>
  FUNC_LAB_CANSIGN = 'LAB.SIGN.ALLOW';
  /// <summary>
  /// The user can start AdminTool application.
  /// </summary>
  FUNC_ADMIN_TOOL = 'ADMIN.APP.START';
  /// <summary>
  /// The user is able to check/uncheck multiple group view. See dbo.UserList.ShowMyGroup in database.
  /// </summary>
  FUNC_CASELIST_MULTIGROUP = 'CASELIST.MULTIGROUP';
  /// <summary>
  /// The user can change their own profession from the titlebar.
  /// </summary>
  FUNC_USER_CHANGE_PROFESSION = 'USER.PROFESSION.CHANGE';

  FUNC_DRUGLIST_EDIT = 'DRUGLIST.EDIT';

  FUNC_START_QUICKSTAT_APP = 'QUICKSTAT.START';

implementation

end.
