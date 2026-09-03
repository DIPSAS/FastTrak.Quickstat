# QuickStat port — 01: connection, session, settings and logging substrate

Target: WPF, `net10.0-windows`, C#, `.slnx`, flat repo layout, CommunityToolkit.Mvvm,
`Microsoft.Data.SqlClient`, `Microsoft.Extensions.DependencyInjection` + `.Logging`, ClosedXML, xUnit.

Scope of this document: everything between the WPF views and SQL Server — connection strings,
the connect/login pipeline, the SQL execution surface, settings persistence and logging.
Collectors, the data matrix/grid and Excel/CSV export are **out of scope** (separate documents),
but this document defines the abstractions they will consume.

Everything below was read from the Delphi sources in `C:\work\FastTrak.Quickstat`
(root `*.pas`, `FastTrak\*.pas`), starting from `QuickStat.dpr` → `MainQuickStat.pas` +
`QuickStat.Collectors.pas` and following the `uses` graph. Unreferenced library code is ignored,
and where a unit is present but unreachable that is called out explicitly.

---

# Part 1 — Findings

## 1. Connection / login lifecycle

### 1.1 Object graph created at form-create time

`TfrmQuickStat.FormCreate` (`MainQuickStat.pas:254-316`) builds the whole graph before any
connection exists:

| Line | What |
|---|---|
| `MainQuickStat.pas:259-261` | `GlobalLog.Enabled := true; GlobalLog.LogCallStack := true; GlobalLog.EnterMethod(...)` |
| `MainQuickStat.pas:263` | `fSettings := TIniSettings.Create` |
| `MainQuickStat.pas:264` | `fGuiSettings := TGuiSettings.Create(Self, fSettings, GlobalLog)` |
| `MainQuickStat.pas:268` | `fQuickStatConnections := TConnectionList.Create([doOwnsValues])` |
| `MainQuickStat.pas:269` | `fCrfContext := TCRFSimpleContext.Create(GlobalLog)` |
| `MainQuickStat.pas:270` | `fCrfContext.Database.LogSql := true` |
| `MainQuickStat.pas:271` | `fCrfContext.Database.AddLoginObserver(Self)` |
| `MainQuickStat.pas:272` | `fPersonList := TPatientList.Create(fCrfContext, TParameterDictionary.Create(TPeriodDictionary.Create(fSettings, GlobalLog), fCrfContext, GlobalLog), fCrfContext.Database, GlobalLog)` |
| `MainQuickStat.pas:274` | `fQuickStat := TQuickStatCollectors.Create(fCrfContext.Database, Self, GlobalLog)` |
| `MainQuickStat.pas:276` | `fGrid := TStudyOverviewGrid.Create(nil, fPersonList, Self, fCrfContext.Database, GlobalLog)` |
| `MainQuickStat.pas:286` | `frmPopulations := TfrmPopulations.Create(nil, fCrfContext.DatabaseInfo, fSettings, fCrfContext.Database, GlobalLog)` |
| `MainQuickStat.pas:293` | `fCrfContext.Session.AddStudyObserver(frmPopulations)` |
| `MainQuickStat.pas:305` | `cbProject.OnChange := SelectConnection` |

`TCRFSimpleContext.AfterConstruction` (`CRF.Context.Facade.pas:148-178`) creates the sub-objects
and registers the login observers, **in this order**:

```
CRF.Context.Facade.pas:155  CreateSessionObject   -> fSession  : TCRFStudyContextFileSystem
CRF.Context.Facade.pas:156  CreateDBS             -> fDb : TSimpleDatabase, fDbInfo : TDatabaseInfo, fEventMap : TCRFEventMapper
CRF.Context.Facade.pas:157  CreateUserObject      -> fActiveUser : TActiveUser
CRF.Context.Facade.pas:169  fDb.AddLoginObserver( fActiveUser )     <- observer #1
CRF.Context.Facade.pas:170  fDb.AddLoginObserver( fDbInfo )         <- observer #2  { comment says "Sets date format" }
CRF.Context.Facade.pas:171  fDb.AddLoginObserver( fEventMap )       <- observer #3
CRF.Context.Facade.pas:172  fDb.AddLoginObserver( fSession )        <- observer #4
CRF.Context.Facade.pas:173  fSession.AddStudyObserver( fActiveUser ) <- study observer #1
```

`MainQuickStat.pas:271` appends the form as **observer #5** and
`MainQuickStat.pas:293` appends `frmPopulations` as **study observer #2**.

`TSimpleDatabase.AddLoginObserver` (`Emetra.Database.Simple.pas:309-313`) asserts
`Connected = false` — "Login observers should be added before the first login". Assertions are
compiled out in Release, so this is documentation, not enforcement.

### 1.2 `TfrmQuickStat.SelectConnection` — the user picks a project

`MainQuickStat.pas:495-519`:

```
497  Screen.Cursor := crSqlWait;              { blocks the UI thread for the whole operation }
499  Application.ProcessMessages;
500  SetInfo( TXT_PROJECT_SELECTED );
501-502  if fCrfContext.Connected then fCrfContext.Disconnect;
504-508  fConnection := cbProject.Items.Objects[ItemIndex] as TQuickStatConnection;   { may be nil }
511  SetInfo( Format( TXT_CONNECTING, [fConnection.Name] ) );
512  fCrfContext.Connect( fConnection.StudyName, fConnection.ConnectionString );
514  finally Done;                            { progress = 100, "Task completed" }
517  finally Screen.Cursor := crDefault;
```

There is **no auto-connect at startup**: `FormShow` (`MainQuickStat.pas:382-406`) only populates
`cbProject` from `QuickStat.config.xml`; `ItemIndex` stays −1 until the user selects an entry.

`TCRFSimpleContext.Disconnect` (`CRF.Context.Facade.pas:237-243`) →
`fSession.CloseSession` (`CRF.Context.Session.pas:237-247`, issues
`EXEC dbo.CloseSession :SessId,:Updates,:Inserts`) then `fDb.Disconnect`.
It does **not** clear `StudyId`/`StudyName` and does **not** notify observers.

### 1.3 `TCRFSimpleContext.Connect(studyName, connectionString)`

`CRF.Context.Facade.pas:216-230`:

```
220-221  if fDb.Connected then fDb.Disconnect;
222-223  if AConnString <> '' then fDb.ConnectionString := AConnString;
224      fSession.SetStudyName( AContext );
225      fDb.Connect;
226      Result := true;
```

**Step 223** → `TSimpleDatabase.Set_ConnectionString` (`Emetra.Database.Simple.pas:280-284`):
`fConnString.Value := AValue` (UDL expansion, see §3) **and**
`fConnection.ConnectionString := AValue` (the raw, un-expanded string — overwritten again at
`Emetra.Database.Simple.pas:380`).

**Step 224** → `TCRFStudyContext.SetStudyName` (`CRF.Context.Session.pas:312-322`) →
`SetContext('', '', AContext)` (`:324-338`) → if the study name changes and the previous one was
non-empty, it clears `fStudyName`/`fStudyId` and notifies study observers (`:329-334`), then
`SetStudyNameInDatabase` (`:259-272`) → `LoadStudyProperties` (`:175-212`).
At this point `Connected` is still false (`fSQL` is nil until `AfterLogin`, see
`CRF.Context.Session.pas:226-230`), so `LoadStudyProperties` only zeroes the counters and returns.
**Net effect of step 224: the study *name* is recorded; nothing is read from the database.**

**Step 225** → `TSimpleDatabase.Connect` (`Emetra.Database.Simple.pas:347-410`):

| Line | Action |
|---|---|
| `:360-361` | `if Connected then exit` — re-entrant no-op |
| `:364-379` | Credential branch, see §4 |
| `:380` | `fConnection.ConnectionString := fConnString.DelimitedText` (the **expanded** OLE DB string) |
| `:381` | `fConnection.Connected := true` — the actual server login |
| `:382` | `fCachedConnString := fConnString.DelimitedText` |
| `:383-389` | `FastQuery('SELECT @@SERVERNAME, DB_NAME()')` → `fServerName`, `fDatabaseName`. **The dataset is deliberately left open** ("Do not close, overridden methods may need to read info") |
| `:390` | `SilentSuccess('...: SERVER=%s, DATABASE=%s')` |
| `:391-406` | Iterate `fLoginObservers` in registration order and call `AfterLogin(Self)`. Any exception → `SilentError` then re-raise as `EDatabaseLoginObserverError` (`:400-403`) |

### 1.4 What each observer does, in firing order

**#1 `TActiveUser.AfterLogin`** (`CRF.Context.ActiveUser.pas:130-137`)
`if Sender.Connected and (StudyContext.StudyName <> '') then Populate else Clear`.
`Populate` (`:201-220`):
- `Load(GetData)` where `GetData` = `SQL.FastQuery('EXEC dbo.GetStudyAndUser :StudyName', [StudyName])` (`:166-169`)
- `TStudyUser.Load` (`CRF.User.StudyUser.pas:232-248`) reads `UserId`, `UserName`, `HPRNo`,
  `Signature`, `ProfId`, `ProfName`, `ProfType`, `CaseList`
- `TActiveUser.Load` (`CRF.Context.ActiveUser.pas:222-240`) adds `ShowMyGroup`, `BlockRules`,
  `StudyId`, `IsSuperuser`, `IsDbOwner`, `IsSingleGroupUser`, `RelationCount`, and — crucially —
  **`fStudyContext.StudyId := fStudyId` at `:236`**. This is the first place `StudyId` is set.
  Field names in `CRF.SQL.Fields.pas:54-63`.
- `FDataset.Close` (`:208`)
- **`:209-218`: if `ProfName = ''` → modal `ltMessage` dialog then `SelectProfession`; if
  `CenterName = ''` → modal dialog then `SelectCenter`.** Both go through `PickList` →
  `GlobalPickList`, which is `nil` in QuickStat (`Emetra.Database.Dialog.Interfaces.pas:60`,
  never assigned anywhere in this repo). `TStoredListItem.PickList`
  (`Emetra.Classes.Subject.Stored.pas:230-234`) asserts; in Release the assert is gone and the
  nil interface call is an AV. **QuickStat therefore requires the user to already have a
  profession and a work site registered in FastTrak.** Port must handle this gracefully.

**#2 `TDatabaseInfo.AfterLogin`** (`Emetra.Database.Info.pas:137-161`)
- `ExecuteCommand('SET XACT_ABORT ON')` (`:146`)
- `ExecuteCommand('SET DATEFORMAT ymd')` (`:147`) — **note this runs *after* observer #1's query**
- `Refresh` (`:99-124`): `SELECT SERVERPROPERTY('ProductVersion'), SERVERPROPERTY('Collation'),
  SERVERPROPERTY('ServerName'), HOST_NAME(), DB_NAME()` then `EXEC dbo.GetDatabaseInfo` →
  `ServerType`, `DbName`, `DbVersion`, `ServerVersion`, `EventScale`
- `VerifyDbVersion` (`:131-135`) raises if `0 < DbVersion < 510`
- The whole body is wrapped in `try..except` (`:154-159`): **any failure is swallowed** and
  `fDbVersion := -1`.

**#3 `TCRFEventMapper.AfterLogin`** (`CRF.Input.EventMap.pas:47-49`) — a *second*
`EXEC dbo.GetDatabaseInfo`, only to read `EventScale`. Redundant with #2.

**#4 `TCRFStudyContext.AfterLogin`** (`CRF.Context.Session.pas:154-167`)
- `Supports(Sender, ISQL, fSQL)`, else raise
- `LoadStudyProperties` (`:175-212`), now with `Connected = true`:
  - `SELECT StudyId FROM dbo.Study WHERE StudName=:StudyName` (`CRF.SQL.pas:21`) → `fStudyId`
    (**second** resolution of the same value)
  - if `fStudyId > 0`: `EXEC dbo.AddSession :StudyId,:CompName,:CompUser,:CompTime,:AppVer`
    (`CRF.SQL.pas:160`) with `[StudyId, GetWindowsComputerName, GetWindowsUserName, Now, fAppVersion]`
    → `fSessId`. **`fAppVersion` is always `''`** (`CRF.Context.Session.pas:218`; nothing in
    QuickStat ever sets `Session.AppVersion`).
  - `NotifyStudyObservers` (`:279-298`):
    - `TActiveUser.AfterStudyChange` (`CRF.Context.ActiveUser.pas:139-159`) → `Populate` **again**
      (a third `EXEC dbo.GetStudyAndUser`), then
      `SilentSuccess('Welcome %d %s, %s at %s')`
    - `TfrmPopulations.AfterStudyChange` → `ReadPopulationList`
      (`EPR.VclFrame.Populations.pas:238-253`) →
      `fPopulations.Load(StudyId, fDbInfo.DbVersion, cbShowCommon.Checked)`
      (`EPR.Population.List.pas:95-110`, three query variants keyed on `DbVersion`)

**#5 `TfrmQuickStat.AfterLogin`** (`MainQuickStat.pas:471-493`)
- `fGrid.Data.PrepareStudy(fCrfContext.StudyName)` (`EPR.QA.Matrix.pas:432-439`) →
  `SELECT StudyId FROM dbo.Study WHERE StudName=:StudName` (**third** resolution of `StudyId`)
- `fQuickStat.PrepareStudy(fCrfContext)` (`QuickStat.Collectors.pas:123-138`):
  - `fLabColorDictionary.AfterLogin(fSQL)` (`QuickStat.Percentile.pas:222-251`) →
    `EXEC Report.GetLabClassVarNames`, then **40** × `EXEC Report.GetPercentileRanksByClassId
    :LabClassId` (one per `RegisterLabPercentileColoring` call, `QuickStat.Collectors.pas:191-226`)
  - `AddCollectorsStudySpecific` → `EXEC Report.GetFormClasses :StudyId`
    (`QuickStat.Collectors.pas:394`)
- fills `cbDataCollector`
- `LoadPackagedSelections` (`MainQuickStat.pas:816-843`) →
  `SELECT r.* FROM Report.QuickStat r JOIN dbo.Study s ON s.StudyId=r.StudyId WHERE r.StudyId=:StudyId`
  (`EPR.QA.SQL.pas:44`)
- `ValidateCollectorSelection`

**Total: roughly 55 synchronous round trips on the UI thread for one combo-box change.**

### 1.5 State available after a successful connect

| Value | Source | Citation |
|---|---|---|
| `StudyName` | set before connecting, from `QuickStat.config.xml` | `CRF.Context.Facade.pas:224` |
| `StudyId` | `dbo.GetStudyAndUser` → then re-read from `dbo.Study` → then again for the grid | `CRF.Context.ActiveUser.pas:236`, `CRF.Context.Session.pas:194`, `EPR.QA.Matrix.pas:434` |
| `SessId` | `dbo.AddSession` | `CRF.Context.Session.pas:199-204` |
| `UserId`, `PersonId`, `UserName`, `FullName`, `Signature`, `HPRNo`, `CaseList` | `dbo.GetStudyAndUser` | `CRF.User.StudyUser.pas:237-244` |
| `ProfId`/`ProfName`/`ProfType`, `CenterId`/`CenterName`, `GroupId` | same | same + `CRF.SQL.Fields.pas:16,22,45-47` |
| Roles: `Superuser`, `DbOwner`, `SingleGroup`, `ShowMyGroup`, `BlockRules`, `RelationCount` | same | `CRF.Context.ActiveUser.pas:227-235` |
| `DatabaseInfo`: `ProductVersion`, `ProductYear`, `Collation`, `ServerName`, `WorkstationName`, `DbName`, `DbVersion`, `ServerVersion`, `EventScale`, `ServerType` | `SERVERPROPERTY` + `dbo.GetDatabaseInfo` | `Emetra.Database.Info.pas:99-124` |
| `TSimpleDatabase.ServerName` / `DatabaseName` | `SELECT @@SERVERNAME, DB_NAME()` | `Emetra.Database.Simple.pas:383-389` |

### 1.6 Failure behaviour

- An observer that throws aborts the whole `Connect`, but **the ADO connection stays open**
  (`fConnection.Connected` was set at `Emetra.Database.Simple.pas:381`, and nothing rolls it back).
  So `fCrfContext.Connected` returns `true` after a partially failed login.
- The exception propagates out of `SelectConnection` (an `OnChange` handler) to
  `Application.HandleException` → a raw Delphi error dialog.

---

## 2. The `ISQL` surface QuickStat actually uses

`ISQL` is declared at `Emetra.Database.Interfaces.pas:254-268` and inherits `IDatabaseConnection`
(`:104-114`). Implemented by `TSimpleDatabase` (`Emetra.Database.Simple.pas:20`), surfaced on the
facade via `property Database: TSimpleDatabase read fDb implements ISQL, IDatabaseLoginContext`
(`CRF.Context.Facade.pas:89`).

### 2.1 Methods actually called anywhere in the reachable graph

| Signature | Impl | Used |
|---|---|---|
| `function FastQuery( const ASQL: string ): TDataset` | `Emetra.Database.Simple.pas:567-584` | yes |
| `function FastQuery( const ASQL: string; const AParams: array of Variant ): TDataset` | `:586-604` | yes — the workhorse |
| `function ExecuteCommand( const ASQL: string ): Integer` | `:467-470` | yes (3 sites) |
| `function ExecuteCommand( const ASQL: string; const AParams: array of Variant ): Integer` | `:486-533` | yes |
| `function Get_Dataset: TDataset` / `property Dataset` | `:275-278` | yes — 4 sites |
| `function Get_Connected: boolean` / `property Connected` | `:286-289` | yes |
| `procedure Connect` / `procedure Disconnect` | `:347-410` / `:329-333` | yes (via the facade) |
| `procedure Set_ConnectionString` / `Get_ConnectionString` | `:280-284` / `:270-273` | yes |
| `procedure AddLoginObserver( AObserver: ILoginObserver )` | `:309-313` | yes |
| `property LogSql: boolean` | `:102` | yes (`MainQuickStat.pas:270`) |

### 2.2 Declared but **never called** (verified by repo-wide grep)

`ExecuteAsync` (both overloads, `Emetra.Database.Simple.pas:472-484`),
`DatabaseObjectExists` (`:689-709`), `IMSSQL`/`GetMultipleRecordsets`/`IMultipleRecordsets`,
`ITransactions` (`BeginTrans`/`CommitTrans`/`RollbackTrans`), `ISQLBatch`,
`IDatabaseAddUser.AddUser`, `IDatabaseChangePassword.TryChangePassword`,
`IDatabaseScript`, `ICheckPermissionProblem` (declared at
`Emetra.Database.Interfaces.pas:188-191` but **not implemented by `TSimpleDatabase`** — see §7.4).

### 2.3 `FastQuery` returns a **single shared dataset**

`FastQuery` always returns the *same* `TADOQuery` instance `fQuery`
(`Emetra.Database.Simple.pas:578`, `:598`), created once in the constructor
(`:167-170`) with `CursorType := ctOpenForwardOnly`, `CursorLocation := clUseClient`,
`LockType := ltReadOnly`.

Consequences that are load-bearing for the port:

1. **Only one result set can be alive at a time.** Every consumer follows a strict
   *open → drain → `Close`* discipline. Example: `TColorDictionary.AfterLogin`
   (`QuickStat.Percentile.pas:232-241`) fully drains and closes `QRY_LAB_VARNAME`
   *before* looping the colorings and issuing their queries.
2. `ISQL.Dataset` is used as "the result of the query I just ran" —
   `CRF.Person.StudyCase.pas:176`, `EPR.QA.CaptionDictionary.pas:91`, `:109`, `:124`.
3. `clUseClient` means ADO **already materialises the whole result set client-side**. So
   buffering results into a list in C# is behaviour-preserving, not a regression.

### 2.4 Parameter binding semantics

`PrepareQueryParameters` (`Emetra.Database.Simple.pas:415-433`):

```pascal
n := 0;
while n < fQuery.Parameters.Count do
begin
  fQuery.Parameters[n].Value := AParams[n];
  inc( n );
end;
```

`PrepareCommandParameters` (`:435-453`) is the same for `TADOCommand`.

- **Binding is positional, by order of first appearance of the `:Name` placeholders in the SQL
  text.** The names are only used by Delphi's `TParameters.ParseSQL`/`TParams.ParseSQL` to derive
  the parameter *list*; ADO converts them to `?` markers before sending.
- The loop is bounded by `Parameters.Count`, **not** by `Length(AParams)`. Passing too few values
  reads past the end of the open array (undefined behaviour, no error). The port must validate.
- `ExecuteCommand` sets `fCommand.ParamCheck := ( high(AParams) <> -1 )`
  (`Emetra.Database.Simple.pas:499`) — parameters are only parsed when values were supplied.
  `fQuery.ParamCheck` is left at its `True` default, and assigning `fQuery.SQL.Text` re-derives
  the parameter list every time.
- Positional binding makes argument-order bugs silent. A live example:
  `CMD_ADD_PERSON = 'EXEC dbo.AddPerson :DOB, :FstName, NULL, :MidName, :GenderId, :NationalId'`
  (`Emetra.Database.Simple.pas:140`) is called with
  `[ADOB, AFirstName, ALastName, AGenderId, ANationalId]` (`:459`) — the *last name* lands in
  `:MidName`. (Not reachable from QuickStat, but it demonstrates the hazard.)
- No SQL statement in the repo repeats a placeholder name (verified). DB-stored population SQL
  might; see Risks.

**Type mapping (Delphi Variant → ADO → SQL Server):**

| Delphi value | Variant type | ADO type | SQL |
|---|---|---|---|
| `UnicodeString` | `varUString` | `adVarWChar` | `NVARCHAR` |
| `Integer` | `varInteger` | `adInteger` | `INT` |
| `Boolean` | `varBoolean` | `adBoolean` | `BIT` |
| `Double` | `varDouble` | `adDouble` | `FLOAT` |
| `Currency` | `varCurrency` | `adCurrency` | `MONEY` |
| `TDateTime` / `TDate` | `varDate` | `adDBTimeStamp` | `DATETIME` |
| `Null` | `varNull` | — | `NULL` |
| `''` | `varUString` | `adVarWChar`, len 0 | `N''` — **empty string, not NULL** |

`ADOCommand`/`ADOQuery` parameters are derived by text parsing, not
`Parameters.Refresh`, so `DataType` is unknown and ADO infers it from the assigned Variant. This
is exactly what `SqlParameter` does from the CLR type — parity is good.

Dates get special treatment in two places:
- `SET DATEFORMAT ymd` at login (`Emetra.Database.Info.pas:147`) because ADO/T-SQL sometimes sees
  dates as strings.
- `CRF.Patient.List.pas:376` sidesteps the issue entirely:
  `FastQuery(QRY_STUDY_PERSON_BY_DOB, [StudyId, FormatDateTime('yyyy-mm-dd', searchDOB)])`.
  **This one is local-only and does not ship** (R11): upstream at `:411` passes the raw `TDateTime`,
  `[StudyId, searchDOB]`. `02-populations-patients.md` §5.2 records it as a local fix worth keeping.
  Nothing in the port depends on it — there is no date-of-birth search.

**Reading results.** Delphi `TField` NULL semantics are relied on throughout:
`AsInteger` → 0, `AsString` → `''`, `AsFloat` → 0, `AsDateTime` → 0.0 (= 1899-12-30),
`AsBoolean` → False. `FindField` returns nil when absent (`EPR.QA.Collector.Base.pas:149-150`),
`FieldByName` raises.

### 2.5 Not all SQL is parameterised

A large fraction of the collector SQL is *composed as text*:

- placeholder substitution: `{IdList}`, `{FormName}`, `{ItemList}`, `{LabList}`
  (`EPR.QA.SQL.pas:12-15`), e.g. `TDataCollector.SQL`
  (`EPR.QA.Collector.Base.pas:187-207`) splices up to 100 person IDs directly into the text.
- `Format('%d'/'%s', ...)` inlining, e.g. `QRY_LAB_QUARTERS = 'EXEC Report.ColLabQuarters %d'`
  (`EPR.QA.SQL.pas:53`), `CMD_GRANT_FASTTRAK_ROLE` (`CRF.SQL.pas:84`).
- **Whole SQL statements loaded from the database.** `TPopulation.Get_QueryText`
  (`CRF.Population.pas:115-118`) returns `dbo.DbProcList.ProcParams`-style SQL text fetched by
  `EXEC dbo.GetPopulations :StudyId`. `TPatientList.Query` (`CRF.Patient.List.pas:286-333`)
  executes it verbatim, with parameters resolved from the parameter dictionary.

So the port **must** keep an "arbitrary SQL text + ordered parameters" execution path; it cannot
move everything to `CommandType.StoredProcedure`.

### 2.6 The parameter dictionary

`TParameterDictionary.TryApplyParameters` (`Emetra.Database.ParameterDictionary.pas:79-133`):

1. `AParams.ParseSQL(AQuery, true)` (`:94`) discovers `:Name` placeholders.
2. If **both** `:StartDate` and `:StopDate` are present (`:96-98`, names at `:63-64`), ask
   `IPeriodDictionary.TryGetPeriod(AQuery, rsSelectPeriod, ...)` (`:101`) — this shows a **modal
   date-range dialog**. Cancel → `Result := false`, the whole query is abandoned
   (`CRF.Patient.List.pas:297`).
3. Remaining null parameters are resolved via
   `IVariantDictionary.TryGetValue(prm.Name, prmValue)` (`:121`). The dictionary is
   `fCrfContext` (`MainQuickStat.pas:272`), and `TBusiness.TryGetValue`
   (`Emetra.Classes.Business.pas:79-84`) resolves them by **RTTI published-property lookup**:
   `IsPublishedProp(Self, AVarName)` + `GetPropValue`.
   The published properties of `TCRFSimpleContext` (`CRF.Context.Facade.pas:97-104`) are the
   complete set of resolvable names: **`CenterId`, `StudyId`, `StudyName`, `UserId`, `SessId`,
   `CaseId`**.
4. Unresolved parameter → `SilentError` + `Result := false` (`:125-127`).

The caller then flattens the `TParams` into an ordered `array of variant`
(`CRF.Patient.List.pas:300-306`) and calls `FastQuery(AQuery, qryParams)`.

---

## 3. Connection strings

### 3.1 What the config actually contains

`QuickStat.config.xml` (repo root):

```xml
<?xml version="1.0"?>
<QuickStat>
  <Connections>
    <Connection>
      <Name>Testdatabase (NDV)</Name>
      <StudyName>NDV</StudyName>
      <ConnectionString>FILE NAME=.\FastTrak.UDL</ConnectionString>
    </Connection>
  </Connections>
</QuickStat>
```

`readme.md:9-22` documents the same shape with `FILE NAME=..\FastTrak.UDL` (the deployment
convention is `<install>\bin\QuickStat.exe` with the UDL one level up).

Parsing: `TConnectionList.Load` (`QuickStat.Connections.pas:56-76`) recursively collects every
node named `Connection` anywhere in the document (`Emetra.Xml.NodeList.pas:33-51`), keys them by
`<Name>` in a `TObjectDictionary` and **silently drops duplicates** (`:68-69`).
`TQuickStatConnection.Parse` (`:39-44`) reads the `Name`, `StudyName` and `ConnectionString` child
elements. File name is `ChangeFileExt(ParamStr(0), '.config.xml')` (`MainQuickStat.pas:391`);
if missing, `ERR_CONFIG_FILE_MISSING` is logged at `ltException` — a modal error dialog —
and the app **continues with an empty project list** (`MainQuickStat.pas:392-398`).

`FastTrak.UDL` (repo root) is **UTF-16 LE with BOM**, three lines:

```
[oledb]
; Everything after this line is an OLE DB initstring
Provider=SQLOLEDB.1;Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=EFT00028_BEHOVPOL_PRODSETTING;Data Source=localhost
```

### 3.2 How `FILE NAME=` is resolved

`TMSSQLConnString` is a `TStringList` with `Delimiter := ';'` and `StrictDelimiter := true`
(`Emetra.Database.ConnectionString.pas:147-148`), so keys containing spaces
(`Integrated Security`, `Initial Catalog`, `Data Source`, `Persist Security Info`) survive parsing
and quoting is disabled.

`Set_Value` (`:261-268`):

```pascal
DelimitedText := AValue;
fileName := Values[KEY_FILE_NAME];        // KEY_FILE_NAME = 'FILE NAME'  (:75)
if fileName <> EmptyStr then
  LoadFromUdl( fileName );
```

`LoadFromUdl` (`:184-198`) loads the file and takes **line index 2** (the third line), assigning
it to `DelimitedText` — i.e. it **replaces the entire key set**. Any keys that were next to
`FILE NAME=` in the original string are discarded.

**Path resolution is relative to the process current directory**, because
`TStringList.LoadFromFile` does no exe-relative resolution. Contrast with
`GetFastTrakParentConnection` (`:106-109`), which explicitly builds
`FILE NAME=<ExtractFilePath(ParamStr(0))>..\FastTrak.UDL`. This is a real deployment trap
(shortcut "Start in" folder must match) and the port should fix it — see §Design.

If the UDL has fewer than 3 lines, `LoadFromUdl` silently does nothing and the connection string
stays `FILE NAME=...`, which ADO itself also understands — so the failure mode is invisible.

### 3.3 OLE DB keywords in play

Recognised by `Emetra.Database.ConnectionString.pas:73-88`:

| Const | Value |
|---|---|
| `KEY_FILE_NAME` | `FILE NAME` |
| `KEY_PROVIDER` | `Provider` (`SQLOLEDB.1`, `SQLNCLI10.1`, `SQLNCLI11.1`) |
| `KEY_DATABASE` | `Initial Catalog` |
| `KEY_SERVER` | `Data Source` |
| `KEY_USER_ID` | `User ID` |
| `KEY_PASSWORD` | `Password` |
| `KEY_SECURITY` | `Integrated Security` (value `SSPI`) |

`Persist Security Info=False` appears in the UDL but is not referenced by any Delphi code — it is
passed through to OLE DB verbatim.

`IntegratedSecurity` is `SameText(Values['Integrated Security'], 'SSPI')` (`:229-232`);
`SetLogin` **removes** the `Integrated Security` key entirely rather than setting it to false
(`:239-250`).

### 3.4 Mapping to `Microsoft.Data.SqlClient`

| OLE DB keyword | Action | `SqlConnectionStringBuilder` |
|---|---|---|
| `Provider` | **DROP** — `SqlConnectionStringBuilder["Provider"]` throws `ArgumentException: Keyword not supported` | — |
| `FILE NAME` | **RESOLVE** before building | — |
| `Data Source` / `Server` / `Address` / `Addr` | keep | `DataSource` |
| `Initial Catalog` / `Database` | keep | `InitialCatalog` |
| `Integrated Security` = `SSPI`/`true`/`yes` | keep (SqlClient accepts the literal `SSPI`) | `IntegratedSecurity = true` |
| `Trusted_Connection` = `Yes` | translate | `IntegratedSecurity = true` |
| `Persist Security Info` | keep | `PersistSecurityInfo` |
| `User ID` / `UID` | keep | `UserID` |
| `Password` / `PWD` | keep | `Password` |
| `Application Name` / `App` | keep | `ApplicationName` |
| `Workstation ID` / `WSID` | keep | `WorkstationID` |
| `Connect Timeout` / `Connection Timeout` | keep | `ConnectTimeout` |
| `Current Language` / `Language` | keep | `CurrentLanguage` |
| `Packet Size` | keep | `PacketSize` |
| `Failover Partner` | keep | `FailoverPartner` |
| `MARS Connection` | translate | `MultipleActiveResultSets` |
| `Initial File Name` | translate | `AttachDBFilename` |
| `Use Encryption for Data` = `True` | translate | `Encrypt = true` |
| `Network Library` = `DBMSSOCN` | drop; prefix `DataSource` with `tcp:` | — |
| `Auto Translate`, `Tag with column collation when possible`, `Use Procedure for Prepare`, `OLE DB Services`, `General Timeout`, `Prompt`, `Window Handle`, `Mode`, `Asynchronous Processing`, `Extended Properties`, `Locale Identifier`, `Replication` | **DROP** (log at Debug) | — |
| anything else | try `builder[key] = value`; on `ArgumentException` → drop + log **Warning** | — |

**⚠ Unquote the value, and drop it when what is left is empty.** The Windows data link dialog writes
*every* property the provider knows about, and spells an unset one as two quote characters:
`User ID="";Initial File Name="";Server SPN="";Authentication="";Access Token=""`. The Delphi never
had to care — it handed the whole initialisation string to OLE DB, which knows its own quoting rules
— but a port that re-emits keyword by keyword does. Taken literally, `Initial File Name=""` becomes
an `AttachDBFilename` two characters long, and `Microsoft.Data.SqlClient` attaches a database file
for **any** non-empty value: the login then runs an implicit `CREATE DATABASE … FOR ATTACH` and the
server answers error 262, which §3.1's privilege list turns into "you are not a member of the
QuickStat database role". `OleDbKeywords.Unquote` strips one matching pair of `"` or `'` and
collapses a doubled inner quote; `MapKeyword` then drops the keyword rather than setting it to
nothing. See `PORT-PLAN.md` §8.11 (16).

### 3.5 ⚠ The `Encrypt` compatibility trap — read this before writing code

**The legacy strings contain no `Encrypt` keyword at all.** OLE DB `SQLOLEDB`/`SQLNCLI` defaulted
to *unencrypted* unless the server forced encryption.

`Microsoft.Data.SqlClient` **changed the `Encrypt` default from `false` to `true` in version 4.0**,
and 5.x/6.x keep `Encrypt=Mandatory` as the default (the keyword now takes
`Mandatory` / `Optional` / `Strict`). A verbatim translation therefore silently *turns TLS on*.

Against a typical on-prem SQL Server with a self-signed certificate this fails at login with:

```
A connection was successfully established with the server, but then an error occurred during the
login process. (provider: SSL Provider, error: 0 - The certificate chain was issued by an
authority that is not trusted.)
```

and, when the `Data Source` alias does not match the certificate subject (`localhost`, a bare IP,
a NetBIOS short name against an FQDN cert):

```
The target principal name is incorrect.
```

Because the shipped config uses `Data Source=localhost`, **this will fail on the very first run**
unless handled. Three remedies, in order of preference:

1. **Trusted certificate on the server** + `Encrypt=True` (+ `HostNameInCertificate=<cert CN>`,
   available from MDS 5.0, when the alias differs). Requires a server change.
2. **`Encrypt=True;TrustServerCertificate=True`** — encrypted, certificate not validated.
   No server change. Strictly better than the OLE DB status quo; documents an accepted
   MITM risk on a trusted LAN.
3. **`Encrypt=False`** — byte-for-byte the legacy behaviour, no encryption.

**Recommendation:** the translator injects `Encrypt=True;TrustServerCertificate=True` as a
*default* (only when the source string does not already specify them), and this is overridable
per connection. **Second trap:** SQL Server 2008 R2 / 2012 without the TLS 1.2 update cannot
negotiate with modern .NET (TLS 1.0/1.1 are disabled by OS policy on current Windows). For those
servers the site must fall back to option 3. `DatabaseInfo.ProductYear`
(`Emetra.Database.Info.pas:203-219`) tells you the server generation — but only *after* a
successful connect, so the fallback has to be configuration, not detection.

**Override mechanism that preserves "read `QuickStat.config.xml` verbatim":** add an *optional*
child element the Delphi app ignores (it only reads `Name`, `StudyName`, `ConnectionString`, so
unknown children are harmless and the file stays usable by both apps):

```xml
<Connection>
  <Name>Testdatabase (NDV)</Name>
  <StudyName>NDV</StudyName>
  <ConnectionString>FILE NAME=.\FastTrak.UDL</ConnectionString>
  <SqlOptions>Encrypt=False;TrustServerCertificate=True</SqlOptions>   <!-- optional, .NET only -->
</Connection>
```

`SqlOptions` is applied *after* the UDL expansion (so it survives, unlike extra keys next to
`FILE NAME=`). A process-wide escape hatch `QUICKSTAT_SQL_OPTIONS` environment variable is also
recommended for support scenarios.

**Other defaults the translator should inject when absent:**
`ApplicationName = "DIPS QuickStat"` (shows in `sys.dm_exec_sessions`; the OLE DB app showed the
provider name), `ConnectTimeout = 15`, `CommandTimeout = 300` (see §5),
`MultipleActiveResultSets = false`, `Pooling = true`.
Leave `WorkstationID` at its default — SqlClient sends `Environment.MachineName`, which matches
what `HOST_NAME()` returned before (`Emetra.Database.Info.pas:82`, `:106`).

---

## 4. Auth model

- **Integrated Security is the only path that works in QuickStat.** The credential branch in
  `TSimpleDatabase.Connect` (`Emetra.Database.Simple.pas:364-379`) is:
  1. `fConnString.IntegratedSecurity` → proceed (`:364-365`)
  2. `fConnString.HasUsernameAndPassword` → proceed (`:366-367`) — SQL logins *are* supported if
     `User ID=`/`Password=` are already in the UDL
  3. cached string matches → proceed (`:368-369`)
  4. `fLoginDialog` assigned → prompt (`:370-377`)
  5. otherwise → `raise EDatabaseCredentialsMissing('Påloggingsinformasjon mangler!')` (`:379`)
- **There is no login dialog in QuickStat.** `TSimpleDatabase.LoginDialog`
  (`Emetra.Database.Simple.pas:101`) is never assigned anywhere in this repo (verified by grep),
  so branch 4 is dead and branch 5 is the failure mode for a string without SSPI and without
  credentials.
- `GlobalPickList` (`Emetra.Database.Dialog.Interfaces.pas:60`) is likewise never assigned —
  see §1.4 observer #1 for the consequence.
- `TSimpleDatabase.Get_UserName` (`:250-256`) returns `GetWindowsUserName` under SSPI, otherwise
  the `User ID` from the string. `Get_Password` (`:237-243`) returns `''` under SSPI.
- `CanChangePassword` (`:335-338`) is `not IntegratedSecurity`; `TryChangePassword` (`:670-687`)
  emits `ALTER LOGIN ... WITH PASSWORD = ...` — unused by QuickStat.

### What the `QuickStat` database role gates

`readme.md:24-26`: *"Brukere som skal ha tilgang til QuickStat må ha en egen databaserolle i
aktuelle databaser, også kalt QuickStat."*

- The role name is `ROLE_QUICKSTAT = 'QuickStat'`
  (`FastTrak\Emetra.AccessControl.Constants.pas:51`).
- **QuickStat.exe never checks *this* right.** The check lives in *FastTrak.exe*:
  `GrantAccessToDatabaseRole( FUNC_START_QUICKSTAT_APP, ROLE_QUICKSTAT )`
  (`Emetra.AccessControl.AccessControlManager.pas:228`, `FUNC_START_QUICKSTAT_APP =
  'QUICKSTAT.START'` at `Emetra.AccessControl.Constants.pas:136`) and only enables/disables the
  *"Start QuickStat"* menu item (`EPR.Admin.GUI.Frame.MainMenu.pas:675`).
- **Corrected 2026-09-03 (R11 sweep).** An earlier revision said there was "no `AccessControl` usage
  in the QuickStat reachable graph" and gave the two line numbers above as `:137` and `:691`. All
  three were read from `C:\work\FastTrak`, which is on `master`. The line numbers are as shown here
  on the shipping lineage, and the absolute claim is **false**: `EPR.VclFrame.Populations.pas:168-177`
  registers `FUNC_POPULATION_SOURCE` as `asDenied` and gates the SQL source pane on it, identically
  on both lineages. What is true is narrower — QuickStat checks no right that governs *starting* it.
  The port did not inherit the error: `PopulationPickerViewModel.ShowSourceCode` cites the gate and
  records the owner's decision to replace it with a check box.
- Enforcement is therefore **entirely SQL Server object-level GRANTs**. The `QuickStat` role must
  hold `EXECUTE` on the `Report.*` and `QuickStat.*` programmability used by the app
  (`Report.AddQuickStat`, `Report.AddSelection`, `Report.AddSelectionMember`,
  `Report.GetFormClasses`, `Report.GetFormData`, `Report.GetFormInstances`,
  `Report.GetLabClassVarNames`, `Report.GetPercentileRanks*`, `Report.Col*`, `Report.NorGeP`,
  `QuickStat.DeletePackage`) plus `SELECT` on `Report.QuickStat`.
- A non-member simply gets a permission error at query time, which
  `ShouldRetryLastOperation` turns into `EDatabasePrivilegeError` for native errors
  229/230/262/300/1971/1972/1991 (`Emetra.Database.NativeErrors.pas:73-83`,
  raised at `Emetra.Database.Simple.pas:638-639`) with the Norwegian message
  `SDatabasePrivilegeError` (`Emetra.Database.Simple.pas:123-125`).

---

## 5. Retry / async / timeout in `TSimpleDatabase`

### 5.1 Retry

`ShouldRetryLastOperation` (`Emetra.Database.Simple.pas:606-668`) walks `fConnection.Errors`
(the ADO error/message collection) and:

| Condition | Behaviour | Line |
|---|---|---|
| SQLSTATE class `01`, `NativeError = 0` (a `PRINT`) | `fLog.Event(description)` | `:627-629` |
| SQLSTATE class `01` otherwise | `SilentWarning` | `:630-632` |
| anything else | `SilentError` | `:633-634` |
| `NativeError` ∈ {229,230,262,300,1971,1972,1991} | `raise EDatabasePrivilegeError` | `:638-639` |
| `NativeError >= 50000` | `raise EDatabaseUserDefinedError` | `:642-643` |
| SQLSTATE = `08S01` (communication link failure) | `Result := true` → retry | `:646-647` |
| any errors and not retryable | `raise EDatabaseCommandFailed` | `:652-656` |
| retryable | `SilentWarning`, `fConnection.Connected := false`, `Sleep(fRetryDelay)` | `:659-664` |
| always | `fConnection.Errors.Clear` | `:666` |

Retry loops: `ExecuteCommand` `:505-529` and `OpenDataset` `:544-562`, both bounded by
`fMaxRetries = 10` with `fRetryDelay = 500` ms (`:185-186`).

Three problems that must **not** be ported:

1. **`ExecuteCommand` calls `ShouldRetryLastOperation` on the *success* path**
   (`:508`). Because *any* entry in the ADO `Errors` collection (including informational
   `PRINT` output, already logged at `:629`) satisfies `errNo > 0`, a successful command whose
   stored procedure printed anything raises `EDatabaseCommandFailed`. That is a bug.
2. **The retry path disconnects (`:662`) and never explicitly reconnects.** Whether
   `TADOCommand.Execute`/`TADOQuery.Open` re-open implicitly is provider-dependent; the behaviour
   is not reliable.
3. **Non-idempotent commands are retried.** `Report.AddQuickStat`, `Report.AddSelectionMember`
   and `dbo.AddSession` would be re-executed on a transient failure, producing duplicates.

### 5.2 Async

`ExecuteAsync` (`:472-484`) spawns a `TSqlCommandThread` (`Emetra.Database.Async.pas`) with its
own `TADOConnection`, `FreeOnTerminate := true`, no error handling and no completion callback.
**No call site exists anywhere in the repo.** `fAllowAsync := true` (`:187`) is not exposed.
Drop entirely.

### 5.3 Timeouts

- `CommandTimeout` is set to 30 s only in the 4-argument constructor (`:157`). QuickStat uses the
  `Create(ALog)` overload (`:176-180` → `:160-174`), which leaves it at ADO's default of **30 s**.
- `Set_CommandTimeout` (`:263-268`) exists but nothing calls it.
- 30 s is too short for the collector queries over large populations; this is a known source of
  "the app hangs / errors on big protocols".

### 5.4 Wait cursor

`SetCursorToWaiting` / `SetCursorBack` (`:711-721`) push/pop `crSqlWait` on a global
`CursorStack` (`Emetra.Win.CursorStack.pas:23-24, 30-56`) around every `FastQuery` and
`ExecuteCommand`. `fUseCursorStack := true`, `fWaitCursor := crSqlWait` (`:188-189`).
Combined with `Screen.Cursor := crSqlWait` in the form handlers, this is the entire
"progress" story: the UI thread is blocked.

---

## 6. Settings

`fSettings := TIniSettings.Create` (`MainQuickStat.pas:263`) →
`TIniSettings` (`Emetra.Settings.IniFile.pas:48`), a *reference-counted*
`IScopedSettingsReadWrite`.

### 6.1 The real interface names

There is **no `ISettings` and no `IScopedSettings`**. The actual set
(`Emetra.Settings.Interfaces.pas`):

```pascal
TSettingScope = ( ssUndefined, ssGlobal, ssUser, ssMachineUser );          // :18-37

ISettingsRead                                                              // :45-65
  Exists/ReadBool/ReadDate/ReadInteger/ReadFloat/ReadString( AKey, ADefault )

IContextSettingsRead                                                       // :67-87   (no implementor — dead)

IScopedSettingsRead                                                        // :93-107
  Get_Scope/Set_Scope
  Exists    ( AScope; AContext, AKey )
  ReadBool  ( AScope; AContext, AKey; ADefault: boolean  = false )
  ReadDate  ( AScope; AContext, AKey; ADefault: TDateTime )                 { no default default }
  ReadFloat ( AScope; AContext, AKey; ADefault: double   = 0 )
  ReadInteger(AScope; AContext, AKey; ADefault: Integer  = 0 )
  ReadString( AScope; AContext, AKey; ADefault: string   = '' )

IScopedSettingsReadWrite = interface( IScopedSettingsRead )                // :109-117
  WriteBool / WriteDateTime / WriteFloat / WriteInteger / WriteString
```

`AScope` selects the **file**; `AContext` is the **INI section**; `AKey` is the key.
There is no delete, no section enumeration, and the reader is `ReadDate` while the writer is
`WriteDateTime`.

### 6.2 Where the files live

Root folder = `ExtractFilePath(ParamStr(0))` — the **exe directory**
(`Emetra.Settings.IniFile.pas:167-178`, specifically `:174`).

| Scope | Path | Resolver |
|---|---|---|
| `ssGlobal` | `<exedir>\Settings\emetra.ini` | `:251-254` |
| `ssMachineUser` | `%APPDATA%\Emetra\Shared\<COMPUTERNAME>.ini` (fallback `<exedir>\Settings\<COMPUTERNAME>-<USERNAME>.ini`) | `:256-264` |
| `ssUser` | `%APPDATA%\Emetra\Shared\<directoryIdentifier>.ini` | `:266-273` |

`%APPDATA%` is *roaming* (`CSIDL_APPDATA = $001A`, `Emetra.Win.ShellFolders.pas:14`).
`<directoryIdentifier>` is a GUID stored in `[Directory] Identifier` in the **global** ini,
generated on first run if the global ini is writable
(`Emetra.Settings.IniFile.pas:226-246`), then run through `NormalizeFileName` (`:138-153`).

Also written unconditionally into **every** ini that is opened, by `VerifyWriteAccess`
(`:311-322`): `[Directory] RootDir`, `[Test] LastOpened`, `[Test] WindowsUserName`.
And into the registry, `HKCU\Software\Emetra\QuickStat`: `AppDir`, `LastOpened`,
`WindowsUserName` (`:277-291`, called at `:210`).

No encryption, no obfuscation. `System.IniFiles.TIniFile` wraps
`WritePrivateProfileString`, so **every write is committed immediately** — there is no flush step.

If `%APPDATA%\Emetra\Shared\` cannot be created/opened, the constructor raises
`EAssertionFailed` (`:205`) — the only hard failure.

### 6.3 What QuickStat actually persists

Only **two** `IGuiSettings` calls exist in the whole application:
`MainQuickStat.pas:390` `fGuiSettings.RestoreFormState` (in `FormShow`) and
`MainQuickStat.pas:410` `fGuiSettings.SaveFormState` (in `FormClose`).

`TGuiSettings` (`Emetra.VclUtil.Settings.pas`) always uses `ssUser`.
Section = `FormKey` = `Format('%s.%dx%d', [FMainForm.Name, Screen.Width, Screen.Height])`
(`:70-77`) → **`frmQuickStat.1920x1080`** — the form's `Name`, not its class, and the section
changes with screen resolution by design.

| Section | Key | Type | Write | Read |
|---|---|---|---|---|
| `frmQuickStat.<W>x<H>` | `State` | `ord(TWindowState)` — always written | `:245` | `:193`, default 0 |
| `frmQuickStat.<W>x<H>` | `Left` | int — only when `WindowState = wsNormal` (`:246`) | `:248` | `:197` |
| `frmQuickStat.<W>x<H>` | `Top` | int | `:249` | `:198` |
| `frmQuickStat.<W>x<H>` | `Width` | int | `:250` | `:201` |
| `frmQuickStat.<W>x<H>` | `Height` | int | `:251` | `:202` |
| `PeriodStart` | *(the whole SQL text)* | datetime | `EPR.PeriodDictionary.pas:75` | `:65`, default `Now-1` |
| `PeriodEnd` | *(the whole SQL text)* | datetime | `:76` | `:66`, default `Now` |
| `Directory` | `RootDir` | string | `Emetra.Settings.IniFile.pas:315` | — |
| `Test` | `LastOpened` / `WindowsUserName` | | `:316` / `:317` | — |

`RestoreFormState` (`:185-211`) additionally: if the saved `State` is not `wsNormal` it
**returns immediately** without restoring bounds (`:195-196`), and it clamps the restored
rectangle to a visible monitor (`:204-205`, `RectIsVisibleOnMonitors` at `:168-183`).

**The period keys are inverted.** `TPeriodDictionary` (`EPR.PeriodDictionary.pas:65-66`, `:75-76`)
passes `KEY_PERIOD_START` where `AContext` (the section) belongs and `AContext` where the key
belongs — so the section is the literal string `PeriodStart` and the *key* is the entire SQL
query text. `WritePrivateProfileString` cannot store multi-line keys or keys containing `=`, so
for any realistic population SQL the round-trip silently fails and the defaults
(`Now-1` .. `Now`) are used every time. The intent was clearly
`(ssUser, AContext, KEY_PERIOD_START, ...)`.

`TPeriodDictionary.TryGetPeriod` (`:54-80`) shows a **modal** `TfrmPeriod` dialog
(`Emetra.VclForm.Period.pas:48-55`), enforces `start < end`, and the semantics are
**`[start, end)`** (`Emetra.VclForm.Period.pas:37-39`).

**Implemented but never called from QuickStat** (`Emetra.VclUtil.Settings.pas`): `Color`,
`FontName`, `FontSize` in section `Screen<W>x<H>`; `<Panel>.Height`, `<Panel>.Width`,
`<Splitter>.Position`. There is **no recursive control walk** — each control must be passed
explicitly.

`EPR.VclFrame.Populations.pas` receives `fSettings` (`:47`, `:64`, `:93`, `:99`) and
**never reads it** — a dead injected dependency. No `EPR.QA.*` or `CRF.*` unit uses settings.

**Deliberately not persisted today** (all reset on every launch): selected project, checked
collectors, anonymity mode, `cbShowDataHint`, `cbWideColumns`, `cbExportDates`, splitter position,
active tabs, package filter text, population filter/`cbShowCommon`/`cbSimpleView`, grid column
widths, last export folder.

Note: "packaged selections" *are* persisted — but to the **database**
(`Report.QuickStat` via `Report.AddQuickStat` / `QuickStat.DeletePackage`), not to settings.

### 6.4 Other configuration files

- `QuickStat.config.xml` — read-only, see §3.1.
- `<exedir>\LOGS\logging.ini` — log rotation state, see §7.

---

## 7. Logging — and the places where "logging" is really UI

### 7.1 `GlobalLog`

`GlobalLog` is a plain global interface variable:
`Emetra.Logging.Interfaces.pas:280-281` → `var GlobalLog: ILog = nil;`.
It is assigned in exactly one place — the **initialization section** of the concrete logger:

```pascal
initialization
  GlobalLog := TPlainTextLog.Create;      // Emetra.Logging.PlainText.pas:586-588
finalization
  GlobalLog := nil;                       // :590-592
```

`QuickStat.dpr:3-11` therefore selects the logger purely by which unit is first in the `uses`
clause (`Emetra.Logging.SmartInspect` in Debug, `Emetra.Logging.PlainText` otherwise).
**`Emetra.Logging.SmartInspect` does not exist in this checkout**, so the Debug configuration
cannot compile as-is; only the plain-text logger is portable.

### 7.2 `TLogLevel` and the dialog threshold

`Emetra.Logging.Interfaces.pas:56-85`, ordered ascending, compared with `>=`:

| Ord | Value | Logged? | **Modal dialog?** | Icon |
|---|---|---|---|---|
| 0 | `ltDebug` (`= ltTrivialInfo`) | no (below `Threshold`) | no | — |
| 1 | `ltInfo` | yes | no | — |
| 2 | `ltMessage` | yes | **YES** | `mtInformation` / `mtConfirmation` |
| 3 | `ltWarning` | yes | **YES** | `mtWarning` |
| 4 | `ltError` (`= ltException`, alias at `:267`) | yes | **YES** | `mtError` |
| 5 | `ltCritical` | yes | **YES** | `mtError` |

Defaults, set in `TLogAdapter.Create` (`Emetra.Logging.Base.pas:107-108`) and never changed by
QuickStat: `Threshold = ltInfo`, `ThresholdForDialog = ltMessage`.

`TPlainTextLog.Event` (`Emetra.Logging.PlainText.pas:393-425`) is where the dialog happens:

```pascal
if ( ALogType >= Threshold ) then
begin
  if Enabled then fItems.Add( ... );                              // :402-403  <- Enabled gates the LOG ONLY
  if not( ALogType >= ThresholdForDialog ) then SetDefaultResult  // :405-406
  else begin
    dialogText := PrepareForDialog( AMessage );                   // :409
    if mbNo in ButtonSet then begin
      ShowCrossPlatformDialog( ..., ALogType );                   // :412  -> VCL MessageDlg, MODAL
      if ModalResult = mrCancel then raise EAbort.Create( 'CanceledByUser' );   // :413-414
    end
    else if mbIgnore in ButtonSet then ShowCrossPlatformDialog( ..., [mbOk, mbIgnore], ... )  // :417
    else ShowCrossPlatformDialog( ..., [mbOk], ... );             // :419
  end;
end;
```

The whole block runs **inside `fCriticalSection`** (`:397`…`:423`) — the modal dialog is shown
while holding the log lock.

- `GlobalLog.Enabled := false` would suppress the *log line* but **not** the dialog.
- `Silent*` and `LogSql*` are hard-coded to `ltInfo` and bypass `Threshold`/`Enabled` entirely
  (`:308-348`) — they can never show UI.
- `EnterMethod`/`LeaveMethod` (`:269-303`) are gated on `MainThread and LogCallStack`, maintain a
  shared `fIndentLevel`, and emit `Enter` / `Leave ( n ms )` with a per-level stopwatch.
  QuickStat turns `LogCallStack` **on in release** (`MainQuickStat.pas:260`), so the log is
  dominated by call-stack noise.

### 7.3 `LogYesNo` — a modal dialog wearing a logger's clothes

`ILog.LogYesNo( const s: string; const ALevel: TLogLevel = ltMessage; const ACancel: boolean = false ): boolean`
(`Emetra.Logging.Interfaces.pas:101`), implemented at
`Emetra.Logging.PlainText.pas:447-455` → `Emetra.Logging.Base.pas:257-267`:
set the button set to `[mbYes, mbNo]` (+ `mbCancel`), call `Event`, return
`fModalResult = mrYes`, restore `[mbOk]`.

**Fail-open hazard:** when the level is below `ThresholdForDialog`,
`SetDefaultResult` / `MapButtonToResult(fDefaultButton)`
(`Emetra.Logging.Base.pas:143-146`, `:290`) sets the result from the default button `mbYes`, so
`LogYesNo` silently returns **true** without asking. Do not reproduce.

The only QuickStat-owned call site is the delete-package confirmation:

```pascal
MainQuickStat.pas:894
  if GlobalLog.LogYesNo( Format( CONFIRM_DELETE_PACKAGE, [packagedSelection.Title] ), ltWarning ) then
  begin
    packagedSelection.Delete( fCrfContext.Database );   // EXEC QuickStat.DeletePackage :RowId
    Items.Delete( ItemIndex );
  end;
```

(`CONFIRM_DELETE_PACKAGE` at `MainQuickStat.pas:226`; `Delete` at `QuickStat.Selection.pas:127-130`.)

The other eight `LogYesNo` sites live in patient-record code
(`CRF.Person.StudyCase.AccessControl.pas:187,189,198,200,218`,
`CRF.Context.ActiveCase.pas:350`, `CRF.User.StudyUser.pas:255`,
`Emetra.Person.Manager.pas:79`). **All are unreachable from QuickStat** — QuickStat never calls
`TCRFSimpleContext.Select(personId)` (verified: no call site), so `TActiveCase` /
`TStudyCaseAccessControl` never run.

### 7.4 Every place where logging is a UI interaction

All of these are **modal**, because `ThresholdForDialog` is never raised above its default.

**Reachable from QuickStat — `ltMessage` (OK, info icon):**

| Site | Message |
|---|---|
| `MainQuickStat.pas:561` | `'No population selected!'` |
| `MainQuickStat.pas:740` | `'Selection was successfully saved.'` |
| `CRF.Context.ActiveUser.pas:211` | `MSG_SET_PROFESSION` — then calls `SelectProfession` (needs `GlobalPickList`, nil) |
| `CRF.Context.ActiveUser.pas:216` | `MSG_SET_CENTER` — then calls `SelectCenter` (same) |

**Reachable — `ltWarning` (OK, warning icon):**

| Site | Message |
|---|---|
| `MainQuickStat.pas:743` | `'There was a problem:\n%s'` (save-selection failure) |
| `MainQuickStat.pas:790` | `MSG_UNKNOWN_POPULATION` |
| `MainQuickStat.pas:803` | `MSG_UNKNOWN_COLLECTOR` — **inside a `while` loop**, one dialog per unknown collector |
| `MainQuickStat.pas:890` | `'You need to select a package for this operation.'` |
| `CRF.Context.Session.pas:143` | `'...: Duplicate registration attempt for %s.'` — a **developer diagnostic** that pops a modal in release |
| `CRF.Context.Session.pas:368` | `'Kunne ikke velge ny protokoll: %s'` (only via `SelectStudy`, unreachable) |
| `EPR.QA.Collection.pas:91` | `'%s.AddCollector("%s"): Not added.'` — another developer diagnostic |

**Reachable — `ltError` / `ltException` (OK, error icon):**

| Site | Message |
|---|---|
| `MainQuickStat.pas:398` | `ERR_CONFIG_FILE_MISSING` — fires during `FormShow`, does **not** abort startup |
| `MainQuickStat.pas:545` | `ERR_POPULATION_NOT_SELECTED` |
| `CRF.Patient.List.pas:384` | raw exception text on **every failed patient search** |
| `EPR.VclFrame.Populations.pas:252` | population-list load failure; also sets `fUsable := false` |
| `Emetra.Business.BaseClass.pas:211`, `:224` | RTTI visibility self-checks |
| `Emetra.Classes.Subject.Stored.pas:150` | the generic `CheckPermissionProblem` fallback |

**`CheckPermissionProblem` — 11 more indirect modal error dialogs.**
`TStoredListItem.CheckPermissionProblem` (`Emetra.Classes.Subject.Stored.pas:143-151`) first tries
`Supports( SQL, ICheckPermissionProblem, intf )`. **`TSimpleDatabase` does not implement
`ICheckPermissionProblem`** (`Emetra.Database.Simple.pas:20` — not in the interface list), so
every call falls through to `Log.Event( AMsgTemplate, [E.Message], ltException )` → modal error.
Call sites: `CRF.Context.ActiveUser.pas:254, 267, 295, 313, 328, 356`,
`CRF.Person.StudyCase.pas:347`, `CRF.User.StudyUser.pas:137, 157, 228, 371`.

**`ltCritical`** — only `Emetra.Classes.Subject.pas:216, 258`, both inside `{$IFDEF Audit}`,
which is never defined. Dead.

**Adjacent:** `IDatabasePickList.SelectInteger/SelectString` take
`AMissingLevel: TLogLevel = ltMessage` (`Emetra.Database.Dialog.Interfaces.pas:22-26`) — the
"no rows" message severity. Same "log level drives dialog severity" convention.

### 7.5 Log file, format, rotation

`GetLogFileName` (`Emetra.Logging.PlainText.pas:127-146`):

```
fileName  = <ExeBaseName> + '-' + %USERNAME% + [ '-' + NNN ] + '.LOG'
directory = <exedir>\LOGS\   (falls back to <exedir>\..\LOGS\ only if the first does not exist)
```

e.g. `C:\FastTrak\bin\LOGS\QuickStat-jdoe-001.LOG`. No date, no PID.
The username comes from the `USERNAME` environment variable
(`Emetra.CrossPlatform.User.pas:40-43`).

Line format — `TLogItem.PlainText` (`Emetra.Logging.PlainText.LogItem.pas:118-121`):

```
FormatDateTime('hh:mm:ss.zzz') + #9 + LOG_LEVEL_NAMES[level] + #9 + DupeString(#9, indent) + text
```

Tab-separated, **no date**, no thread id, no class/method column (callers prepend those manually
via `LOG_STUB = '%s.%s: '`). `LOG_LEVEL_NAMES` = `('debug','info','message','warning','error','critical')`
(`Emetra.Logging.Interfaces.pas:278`).

Every entry is forced to one line (`StripNewlines`, `:384-388`), and every entry is passed through
`AnonymizeLogMessage` (`Emetra.Logging.PlainText.ItemList.pas:56`,
`Emetra.Logging.Utilities.pas:23-27`), which replaces `{{...}}` with `'(Anonymisert)'`.
The **dialog** text instead uses `PrepareForDialog` (`:29-37`), which keeps the content and
expands the literal `\n` escape. So `{{ }}` = "PII: show to the user, redact from the file".

File sink — `Emetra.Logging.Target.TextFile.pas`: UTF-8 **with BOM** (`:37`, `:100`), CRLF (`:58`),
**truncate on open** (`fmCreate`, `:90`), `FlushFileBuffers` after **every line** (`:61-63`),
`.01`…`.63` name fallback when the file is locked (`:88-98`), silent after 256 write errors
(`:36`, `:65-71`).

Rotation — `ReadLogSettings` (`:198-232`) reads `<exedir>\LOGS\logging.ini`:
`[Global] MaxFile` (default 10), `[<USERNAME>] FileNo`, `[<USERNAME>] MaxFile`; the number cycles
`1..MaxFile`, one slot per process start, written back at `:224`.

**The `LOGS` directory is never created.** If it is missing, all 64 file-creation attempts fail,
`fFileStream` stays nil and the application logs **to memory only, with no user-visible
indication** (`:209-211`, `:71`). Fix this in the port.

No error/warning counters exist; `ILog.Count` is just the number of in-memory items
(`:237-240`). There is no `Application.OnException` hook anywhere in the repo.

---

# Part 2 — What to port / what to drop

| Delphi construct | Where | Decision | Why |
|---|---|---|---|
| `ISQL.FastQuery(sql)` / `FastQuery(sql, params)` | `Emetra.Database.Simple.pas:567-604` | **Port**, reshaped to `QueryAsync` returning a materialised result set | Core surface; the shared-dataset return must go |
| `ISQL.ExecuteCommand(sql[, params])` | `:467-533` | **Port** as `ExecuteAsync` returning real rows-affected | Legacy always returns `1` (`:493`) |
| `ISQL.Dataset` property | `:275-278` | **Drop** | Only exists because `FastQuery` returns a shared cursor |
| Single shared `TADOQuery` | `:167-170` | **Drop** | Replaced by per-call materialised results |
| `array of Variant` positional binding | `:415-453` | **Port the call shape, add validation** | Keep call sites mechanical; reject count mismatches |
| `:Name` placeholders | everywhere | **Port** — rewrite to `@Name` at execution time | DB-stored population SQL uses `:Name`; cannot be changed |
| `{IdList}` / `{FormName}` / `{ItemList}` / `{LabList}` text substitution | `EPR.QA.SQL.pas:12-15` | **Port as-is** (collector doc) | Too invasive to parameterise now; keep, but escape/validate integers |
| `ExecuteAsync` + `TSqlCommandThread` | `:472-484`, `Emetra.Database.Async.pas` | **Drop** | Zero call sites; superseded by `async`/`await` |
| `DatabaseObjectExists` | `:689-709` | **Drop** | Zero call sites |
| `IMSSQL`, `IMultipleRecordsets`, `ITransactions`, `ISQLBatch` | `Emetra.Database.Interfaces.pas:242-289` | **Drop** | Zero call sites |
| `IDatabaseAddUser`, `IDatabaseChangePassword`, `IDatabaseScript` | `:129-138`, `:193-196` | **Drop** | Zero call sites in QuickStat |
| Retry on SQLSTATE `08S01`, 10 attempts, 500 ms | `:606-668`, `:185-186` | **Port, narrowed** | Keep transient retry for **reads only**, 3 attempts, exponential backoff |
| Privilege-error mapping (229/230/262/300/1971/1972/1991) | `Emetra.Database.NativeErrors.pas:73-83` | **Port** | Directly surfaces the missing `QuickStat` role |
| User-defined error mapping (`NativeError >= 50000`) | `:60`, `Emetra.Database.Simple.pas:642-643` | **Port** | Stored procs raise business errors this way |
| Treating `PRINT` output as a fatal error | `Emetra.Database.Simple.pas:652-656` | **Drop** | Bug; route `InfoMessage` to the log |
| Retrying non-idempotent commands | `:505-529` | **Drop** | Duplicate-row hazard |
| Disconnect-then-implicit-reconnect on retry | `:662` | **Drop** | Replace with an explicit reconnect step |
| `CommandTimeout` default 30 s | `:157` (unused path) | **Change** to 300 s, configurable | Collector queries exceed 30 s |
| `CursorStack` / `crSqlWait` | `Emetra.Win.CursorStack.pas` | **Drop** | Replaced by `IsBusy` + `async` |
| `Application.ProcessMessages` | `MainQuickStat.pas:499` | **Drop** | Re-entrancy hazard; not needed with `async` |
| `TSimpleDatabase.Connect` credential branch | `:364-379` | **Port** simplified | Integrated Security or embedded SQL login; no dialog |
| `LoginDialog` / `IDatabaseLoginDialog` | `:101`, `Emetra.Database.Dialog.Interfaces.pas:14-17` | **Drop** | Never assigned |
| `GlobalPickList` / `IDatabasePickList` | `Emetra.Database.Dialog.Interfaces.pas:19-60` | **Drop**, but **add a guard** | Nil in QuickStat → AV when profession/centre is unset. Port must detect and report cleanly |
| `ILoginObserver` observer list | `:309-324`, `CRF.Context.Facade.pas:169-172` | **Port as an explicit ordered pipeline** | Same effect, testable, reorderable |
| Order bug: `SET DATEFORMAT` runs after the first user query | `Emetra.Database.Info.pas:147` vs observer #1 | **Fix** — session options first | |
| Three separate `StudyId` resolutions | `CRF.Context.ActiveUser.pas:236`, `CRF.Context.Session.pas:194`, `EPR.QA.Matrix.pas:434` | **Collapse to one** | |
| Two `EXEC dbo.GetDatabaseInfo` calls | `Emetra.Database.Info.pas:113`, `CRF.Input.EventMap.pas:47` | **Collapse to one** | `EventScale` comes from the same row |
| Three `EXEC dbo.GetStudyAndUser` calls per connect | `CRF.Context.ActiveUser.pas:168` via `:134`, `:148`, `:210` | **Collapse to one** | |
| `dbo.AddSession` with empty `AppVer` | `CRF.Context.Session.pas:199`, `:218` | **Port, fixed** — send the real assembly version | |
| `dbo.CloseSession` on disconnect | `:242` | **Port** | Session accounting |
| `TCRFSimpleContext` as `IVariantDictionary` via RTTI | `Emetra.Classes.Business.pas:79-84` | **Drop the RTTI**, keep the *capability* | Explicit dictionary of `StudyId`/`StudyName`/`UserId`/`SessId`/`CenterId`/`CaseId`; AOT-safe |
| `TParameterDictionary` | `Emetra.Database.ParameterDictionary.pas` | **Port** as `IParameterResolver` | Needed for DB-stored population SQL |
| `TPeriodDictionary` + `TfrmPeriod` | `EPR.PeriodDictionary.pas`, `Emetra.VclForm.Period.pas` | **Port**, key bug fixed | `[start, end)` semantics preserved |
| Period keys stored with section/key swapped | `EPR.PeriodDictionary.pas:65-66, 75-76` | **Fix** — section = context hash, keys `PeriodStart`/`PeriodEnd` | Current form never round-trips |
| `TMSSQLConnString` (OLE DB string manipulation) | `Emetra.Database.ConnectionString.pas` | **Replace** with `SqlConnectionStringBuilder` + a translator | |
| `FILE NAME=` / UDL reading | `:184-198`, `:261-268` | **Port** with exe-relative-first resolution | Preserve the file format; fix path resolution |
| CWD-relative UDL path | `:190` | **Change** to exe-dir first, CWD fallback, log which | Deployment trap |
| `TIniSettings` 3-file scoped store + registry writes | `Emetra.Settings.IniFile.pas` | **Do not port as-is.** Port only the `ssUser` scope | `ssGlobal`/`ssMachineUser`/`HKCU` hold nothing QuickStat reads |
| `INumericDictionary` on `TIniSettings` | `:361-391` | **Drop** | Always returns false — broken and unused |
| `TGuiSettings` panel/splitter/font/colour persistence | `Emetra.VclUtil.Settings.pas:93-166, 213-239` | **Drop** (dead in QuickStat), **or** re-implement properly as part of a broader window-state feature | |
| `TGuiSettings` form geometry | `:185-257` | **Port** | Keep the resolution-keyed section and the monitor-visibility clamp |
| `ILog` as a single interface for logging + dialogs | `Emetra.Logging.Interfaces.pas:87-258` | **Split** into `ILogger<T>` + `IUserNotifier` | The single biggest structural fix |
| `Event(..., ltMessage/ltWarning/ltError)` implicit dialogs | `Emetra.Logging.PlainText.pas:405-420` | **Port each site explicitly**, triaged | ~35 dialogs would otherwise silently vanish |
| `LogYesNo` fail-open when below threshold | `Emetra.Logging.Base.pas:143-146` | **Drop** | Confirmations must actually ask |
| `LogYesNo` raising `EAbort` on Cancel | `Emetra.Logging.PlainText.pas:413-414` | **Drop** | No call site passes `ACancel := true` |
| `EnterMethod`/`LeaveMethod` at Information in release | `:269-303`, `MainQuickStat.pas:260` | **Port at `Debug`**, off by default | Release log is currently unreadable |
| `LogSqlQuery`/`LogSqlCommand` + `... ( n ms )` timing | `:308-316`, `:564` | **Port** at `Debug`; parameter *values* at `Trace` only | PII in parameters |
| `{{ }}` PII redaction in the log, kept in dialogs | `Emetra.Logging.Utilities.pas:23-37` | **Port** | Cheap, and the convention already exists |
| Truncate-on-open + 10-slot numeric rotation | `Emetra.Logging.Target.TextFile.pas:90`, `PlainText:198-232` | **Replace** with daily rolling + retention 10 | Truncation loses the previous run |
| `LOGS` folder never created | `PlainText:209-211` | **Fix** — create it; fall back to `%LOCALAPPDATA%` | Silent total log loss today |
| `FlushFileBuffers` per line | `TextFile:61-63` | **Drop** | Use buffered writes + flush on error/shutdown |
| Modal dialog shown while holding the log lock | `PlainText:397-423` | **Drop** | Deadlock/stall hazard |
| `Emetra.Win.Launcher.TMrLauncher` | `Emetra.Win.Launcher.pas` | **Drop** | Replaced by `Process.Start` (used only for the Excel hand-off) |
| `GetWindowsUserName` / `GetWindowsComputerName` / `GetTempDir` | `Emetra.Win.User.pas` | **Drop** | `Environment.UserName`, `Environment.MachineName`, `Path.GetTempPath()` |
| `Emetra.Xml.NodeList.TNodeList` | `Emetra.Xml.NodeList.pas` | **Drop** | `XDocument.Descendants("Connection")` |

---

# Part 3 — Proposed C# design

## 3.0 Assemblies and namespaces

Flat layout, four projects at the repo root, one `.slnx`:

```
Quickstat.slnx
Quickstat.Core/         net10.0            (no WPF reference)
Quickstat.Data/         net10.0            (Microsoft.Data.SqlClient)
Quickstat.App/          net10.0-windows    (WPF, WinExe, composition root)
Quickstat.Tests/        net10.0            (xUnit)
```

Namespaces — I am **keeping `Quickstat.Data` for the SQL surface** as instructed, but *not*
putting session, configuration and logging in it; a single namespace for all four concerns
becomes a dumping ground and makes the test seams unclear:

| Namespace | Assembly | Contents |
|---|---|---|
| `Quickstat.Data` | `Quickstat.Data` | `ISqlExecutor`, `SqlRequest`, `SqlResultSet`, `SqlRow`, `QuickstatDatabase`, retry, error types, `ISqlTextRewriter` |
| `Quickstat.Data.Abstractions` | `Quickstat.Core` | the interfaces only, so `Quickstat.Core` and the tests do not reference `Microsoft.Data.SqlClient` |
| `Quickstat.Configuration` | `Quickstat.Core` | `QuickStat.config.xml` catalogue, UDL reader, connection-string translator, settings store |
| `Quickstat.Session` | `Quickstat.Core` | login pipeline, `StudySession`, `StudyUser`, `DatabaseInfo`, parameter/period resolution |
| `Quickstat.Diagnostics` | `Quickstat.Core` | `IUserNotifier`, method-timing scope helper, file-logger provider |

If the team prefers a single `Quickstat` assembly with folders, nothing below changes except the
project boundaries.

## 3.1 `Quickstat.Data` — the execution surface

```csharp
namespace Quickstat.Data;

/// <summary>Delphi: ISQL (Emetra.Database.Interfaces.pas:254).</summary>
public interface ISqlExecutor
{
    Task<SqlResultSet> QueryAsync(SqlRequest request, CancellationToken ct = default);
    Task<int>          ExecuteAsync(SqlRequest request, CancellationToken ct = default);
    Task<T?>           ScalarAsync<T>(SqlRequest request, CancellationToken ct = default);
}

/// <summary>Delphi: TSimpleDatabase connection management.</summary>
public interface IDatabaseConnection
{
    bool    IsConnected  { get; }
    string? ServerName   { get; }   // @@SERVERNAME
    string? DatabaseName { get; }   // DB_NAME()
    Task ConnectAsync(SqlConnectionStringBuilder connectionString, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
}
```

### `SqlRequest`

```csharp
public sealed record SqlRequest
{
    public required string CommandText { get; init; }

    /// <summary>Positional values, in order of first appearance of the ':Name' placeholders.
    /// Mirrors Delphi's <c>array of Variant</c>.</summary>
    public IReadOnlyList<object?> Values { get; init; } = [];

    /// <summary>Preferred for new code; mutually exclusive with <see cref="Values"/>.</summary>
    public IReadOnlyDictionary<string, object?>? NamedValues { get; init; }

    public TimeSpan? CommandTimeout { get; init; }

    /// <summary>Only idempotent requests are retried after a transient failure.
    /// Queries default to true, commands to false.</summary>
    public bool IsIdempotent { get; init; }

    /// <summary>Short label used in logs and in the busy indicator.</summary>
    public string? Label { get; init; }

    public static SqlRequest Query(string sql, params object?[] values)
        => new() { CommandText = sql, Values = values, IsIdempotent = true };

    public static SqlRequest Command(string sql, params object?[] values)
        => new() { CommandText = sql, Values = values, IsIdempotent = false };
}
```

Call sites port almost verbatim:

```pascal
// Delphi: MainQuickStat.pas:826
thisDataset := fCrfContext.Database.FastQuery( QRY_GET_PACKAGES, [fGrid.Data.StudyId] );
```
```csharp
// C#
var rows = await _sql.QueryAsync(SqlRequest.Query(Sql.GetPackages, studyId), ct);
```

### `SqlResultSet` / `SqlRow`

```csharp
public sealed class SqlResultSet : IReadOnlyList<SqlRow>
{
    public IReadOnlyList<SqlColumn> Columns { get; }
    public int Count { get; }
    public SqlRow this[int index] { get; }
    public bool IsEmpty => Count == 0;

    /// <summary>Delphi TDataset.FindField — case-insensitive, -1 when absent.</summary>
    public int IndexOf(string columnName);
    /// <summary>Delphi TDataset.FieldByName — throws when absent.</summary>
    public int GetOrdinal(string columnName);
}

public readonly struct SqlRow
{
    /// <summary>Delphi's TField "zero date" (TDateTime 0.0).</summary>
    public static readonly DateTime ZeroDate = new(1899, 12, 30);

    public bool     IsNull(int ordinal);
    public object?  GetValue(int ordinal);

    // NULL-coalescing accessors that reproduce Delphi TField semantics exactly.
    public int      GetInt32   (int ordinal, int      @default = 0);
    public long     GetInt64   (int ordinal, long     @default = 0);
    public string   GetString  (int ordinal, string   @default = "");
    public double   GetDouble  (int ordinal, double   @default = 0);
    public decimal  GetDecimal (int ordinal, decimal  @default = 0);
    public bool     GetBoolean (int ordinal, bool     @default = false);
    public DateTime GetDateTime(int ordinal, DateTime? @default = null);   // null => ZeroDate

    // string overloads delegate to GetOrdinal
    public int GetInt32(string column, int @default = 0);
    // ...
}
```

The NULL defaults are **not** cosmetic — code such as
`fSuperuser := ( ReadInteger( FLD_SUPERUSER ) = 1 )`
(`CRF.Context.ActiveUser.pas:231`) and
`dataset.Fields[2].AsFloat` (`EPR.QA.Collector.Base.pas:159`) depends on them.
`GetDateTime` defaulting to `1899-12-30` rather than `DateTime.MinValue` preserves downstream
formatting of missing timestamps.

### Placeholder rewriting

```csharp
public interface ISqlTextRewriter
{
    RewrittenSql Rewrite(string commandText);
}

public sealed record RewrittenSql(
    string CommandText,                        // ':Name' replaced by '@Name'
    IReadOnlyList<string> ParameterNames);     // distinct, in first-appearance order
```

`ColonToAtSqlTextRewriter` is a hand-written scanner (not a regex) that skips:
single-quoted literals (`''` escape), bracketed identifiers `[...]` (`]]` escape),
double-quoted identifiers, `--` line comments and nested `/* */` block comments; it leaves `::`
and `@variables` alone. A placeholder is `:` + `[A-Za-z_][A-Za-z0-9_]*` not preceded by `:`.
Results are cached in a bounded `ConcurrentDictionary<string, RewrittenSql>` — collector SQL is
re-issued in a loop.

Binding rules:
- `NamedValues` given → bind by name; every discovered placeholder must be present.
- `Values` given → `Values.Count` must equal `ParameterNames.Count`, else throw
  `SqlParameterCountException` (the Delphi read past the end of the array instead).
- Repeated placeholders produce **one** `SqlParameter`; positional binding of a statement with
  repeats is rejected with a clear message.

### Value → `SqlParameter`

```csharp
internal static class SqlParameterFactory
{
    public static SqlParameter Create(string name, object? value);
}
```

| CLR | `SqlDbType` | Note |
|---|---|---|
| `null` / `DBNull` | inferred | `Value = DBNull.Value` |
| `string` | `NVarChar`, `Size = -1` when > 4000 else 4000 | matches ADO `adVarWChar`; keeps the existing (NVARCHAR) plan shapes |
| `int`/`short`/`byte` | `Int` | |
| `long` | `BigInt` | |
| `bool` | `Bit` | |
| `float`/`double` | `Float` | |
| `decimal` | `Decimal`, `Precision 19`, `Scale 4` | ADO `adCurrency` equivalent |
| `DateTime` | `DateTime` (**not** `DateTime2`) | matches legacy `adDBTimeStamp`; reject years < 1753 with a clear exception rather than letting SqlClient throw |
| `DateOnly` | `Date` | |
| `Guid` | `UniqueIdentifier` | |
| `byte[]` | `VarBinary` | |
| `Enum` | underlying integral | |

Do **not** use `AddWithValue` — it infers `DateTime2`/`nvarchar(n)` in ways that change plans.

## 3.2 `QuickstatDatabase` — connection lifetime

**Decision: one long-lived `SqlConnection`, guarded by a `SemaphoreSlim(1,1)`.** Rationale:

- The application model is explicitly session-scoped: connect → `dbo.AddSession` → work →
  `dbo.CloseSession`. `SessId` is a real database row.
- `SET XACT_ABORT ON` / `SET DATEFORMAT ymd` are **session** settings. With pooling,
  `sp_reset_connection` resets them on every logical re-open, so a pooled-per-operation model
  would need an extra round trip on every single call.
- QuickStat is a single-user desktop app that never needs two concurrent result sets. The Delphi
  literally cannot do it (one shared cursor).
- MARS is therefore unnecessary; keep `MultipleActiveResultSets=false`.

The semaphore turns accidental concurrency into serialisation instead of
`InvalidOperationException`. `ConnectionString` is exposed so a future parallel collector run can
open extra pooled connections deliberately.

```csharp
public sealed class QuickstatDatabase : ISqlExecutor, IDatabaseConnection, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ILogger<QuickstatDatabase> _log;
    private readonly ISqlTextRewriter _rewriter;
    private readonly SqlRetryPolicy _retry;
    private readonly SqlLoggingOptions _sqlLog;
    private SqlConnection? _connection;

    public string? ConnectionString { get; private set; }
    public bool IsConnected => _connection is { State: ConnectionState.Open };
    public string? ServerName { get; private set; }
    public string? DatabaseName { get; private set; }

    public async Task ConnectAsync(SqlConnectionStringBuilder csb, CancellationToken ct)
    {
        // Open, subscribe to InfoMessage, run the session-options batch,
        // read SELECT @@SERVERNAME, DB_NAME().  Does NOT run the login pipeline
        // (that is SessionService's job) -- Delphi conflated the two.
    }

    public Task<SqlResultSet> QueryAsync(SqlRequest r, CancellationToken ct = default);
    public Task<int>          ExecuteAsync(SqlRequest r, CancellationToken ct = default);
    public Task<T?>           ScalarAsync<T>(SqlRequest r, CancellationToken ct = default);
    public Task DisconnectAsync(CancellationToken ct = default);
}
```

Inside every operation:

1. `await _gate.WaitAsync(ct).ConfigureAwait(false)`
2. throw `DatabaseNotConnectedException` when `!IsConnected`
   (Delphi `CheckConnected` / `EDatabaseImplicitConnectError`, `Emetra.Database.Simple.pas:340-345`)
3. rewrite + bind + `Stopwatch.StartNew()`
4. `await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess is NOT used; we materialise)`
5. materialise into `SqlResultSet` (client-side buffering, matching `clUseClient`)
6. log at `Debug`: `"{Kind} {Elapsed:0.0} ms: {Sql}"` when `_sqlLog.Enabled`
   (Delphi `LogSqlQuery` + `' ... ( %.1f ms )'`, `Emetra.Database.Simple.pas:564`, `:581`)
7. on `SqlException` → classify (below); retry when `r.IsIdempotent` and transient
8. `finally _gate.Release()`

Session options, executed once per physical open:

```sql
SET XACT_ABORT ON;
SET DATEFORMAT ymd;
```

(`SET DATEFORMAT` still matters: DB-stored population SQL may contain literal date strings, even
though *parameters* are now strongly typed.)

`SqlConnection.InfoMessage` is subscribed and every `SqlError` is logged at
`Information` (SQLSTATE class `01` / severity ≤ 10) — **never** turned into an exception.
This deliberately fixes `Emetra.Database.Simple.pas:652-656`.

### Errors

```csharp
namespace Quickstat.Data;

public class QuickstatDataException : Exception
{
    public int? Number { get; }        // SqlException.Number  == ADO NativeError
    public string? Procedure { get; }
    public byte? Class { get; }
}
public sealed class SqlPrivilegeException    : QuickstatDataException; // 229,230,262,300,297,916,1971,1972,1991
public sealed class SqlUserDefinedException  : QuickstatDataException; // Number >= 50000
public sealed class SqlCommandFailedException: QuickstatDataException; // everything else
public sealed class DatabaseNotConnectedException : InvalidOperationException;
public sealed class SqlParameterCountException    : ArgumentException;
public sealed class ConnectionStringTranslationException : Exception;
public sealed class UdlFileException              : Exception;
```

`SqlPrivilegeException` carries the localised message from
`Emetra.Database.Simple.pas:123-125` / `Emetra.Database.NativeErrors.pas:76-82`, plus a hint that
the user is probably missing the **`QuickStat`** database role (see §4).

### Retry

```csharp
public sealed record SqlRetryPolicy
{
    public int MaxAttempts { get; init; } = 3;                              // Delphi: 10
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromMilliseconds(500);
    public bool IsTransient(SqlException ex);   // ex.IsTransient || known numbers
}
```

Known transient numbers (the SqlClient equivalents of SQLSTATE `08S01`):
`-2` (timeout), `20`, `64`, `233`, `10053`, `10054`, `10060`, `10061`, `40197`, `40501`, `40613`,
`49918`, `49919`, `49920`, `4060`, `4221`, `1205` (deadlock victim).
Backoff: `BaseDelay * 2^(attempt-1)` with jitter.
On a connection-level failure the policy closes and re-opens the connection **and re-runs the
session-options batch** before retrying — the missing half of `Emetra.Database.Simple.pas:662`.
`ExecuteAsync` (non-query) is retried only when `SqlRequest.IsIdempotent` is explicitly set.

### Timeouts

`SqlRequest.CommandTimeout` ?? `SqlOptions.DefaultCommandTimeout` (**300 s**, up from the legacy
30 s). Collector queries should pass a longer per-request value; short lookups can pass 30 s.

## 3.3 `Quickstat.Configuration` — connection catalogue and string translation

```csharp
namespace Quickstat.Configuration;

public sealed record ConnectionEntry(
    string Name,               // <Name>
    string StudyName,          // <StudyName>
    string ConnectionString,   // <ConnectionString> verbatim
    string? SqlOptions);       // <SqlOptions> -- optional, .NET-only extension

public interface IConnectionCatalog
{
    /// <summary>Reads &lt;exe&gt;.config.xml. Returns empty and logs when the file is absent
    /// (Delphi MainQuickStat.pas:391-398 behaves the same, minus the modal dialog).</summary>
    IReadOnlyList<ConnectionEntry> Load(string configFilePath);
}

public interface IUdlReader
{
    /// <summary>Reads a Data Link (.UDL) file and returns the OLE DB init string
    /// (the third line). Delphi: Emetra.Database.ConnectionString.pas:184-198.</summary>
    string ReadInitString(string path);
}

public interface IConnectionStringTranslator
{
    SqlConnectionStringBuilder Translate(ConnectionEntry entry, string baseDirectory);
}
```

`XmlConnectionCatalog` uses `XDocument.Load(...).Descendants("Connection")` (recursive, matching
`Emetra.Xml.NodeList.pas:33-51`), keeps the **first** entry per `Name` (matching
`QuickStat.Connections.pas:68-69`) and sorts by `Name` for the picker.

`UdlReader`:
- open with `new StreamReader(path, Encoding.Unicode, detectEncodingFromByteOrderMarks: true)` so
  both the UTF-16 LE file in the repo and any ANSI variant work;
- require at least 3 lines, else throw `UdlFileException` with the path and the line count
  (the Delphi silently did nothing — `Emetra.Database.ConnectionString.pas:193-194`);
- return line index 2, trimmed.

`OleDbConnectionStringTranslator.Translate`:

1. Parse the raw string: split on `;`, trim, split each token on the **first** `=`
   (no quote handling — the legacy used `StrictDelimiter := true`).
2. If a key matches `FILE NAME` / `FILENAME` / `FILE_NAME` (case- and space-insensitive):
   - resolve the path: `Path.GetFullPath(value, baseDirectory)` where `baseDirectory` =
     `AppContext.BaseDirectory`; if that file does not exist, retry against
     `Environment.CurrentDirectory` and log a **Warning** naming both paths.
     *(Deliberate behaviour change: the Delphi resolved relative to the CWD only —
     `Emetra.Database.ConnectionString.pas:190`.)*
   - read it with `IUdlReader` and **replace the entire key set** with the parsed init string
     (matching `Set_Value` → `LoadFromUdl`).
3. Apply the mapping table in §3.4 into a `SqlConnectionStringBuilder`. Unknown keys: try
   `builder[key] = value`; on `ArgumentException`, drop and log a **Warning** with the key name.
4. Apply `entry.SqlOptions`, then the `QUICKSTAT_SQL_OPTIONS` environment variable — both parsed
   the same way and applied **over** whatever came from the UDL.
5. Apply defaults only for keys still unset:
   `Encrypt = true`, `TrustServerCertificate = true`, `ApplicationName = "DIPS QuickStat"`,
   `ConnectTimeout = 15`, `CommandTimeout = 300`,
   `MultipleActiveResultSets = false`, `Pooling = true`.
6. Validate: `DataSource` must be non-empty; `InitialCatalog` must be non-empty;
   `IntegratedSecurity` or (`UserID` and `Password`) must be set — else throw
   `ConnectionStringTranslationException` carrying the same intent as
   `EDatabaseCredentialsMissing` (`Emetra.Database.Simple.pas:379`).
7. Log the final string at `Information` **with `Password` redacted**
   (`builder.Password = "***"` on a clone).

Unit tests (xUnit) that must exist:
- the repo's actual `FastTrak.UDL` translates to
  `Data Source=localhost;Initial Catalog=EFT00028_BEHOVPOL_PRODSETTING;Integrated Security=True;Persist Security Info=False;...`
  and **no** `Provider` key;
- `Provider=SQLNCLI11.1` is dropped without throwing;
- `Trusted_Connection=Yes` becomes `IntegratedSecurity = true`;
- a UDL with 2 lines throws `UdlFileException`;
- `<SqlOptions>Encrypt=False</SqlOptions>` wins over the injected default;
- a relative `FILE NAME=.\FastTrak.UDL` resolves against `AppContext.BaseDirectory`.

## 3.4 `Quickstat.Session` — the login pipeline

```csharp
namespace Quickstat.Session;

public interface ILoginStep
{
    string Name { get; }
    int Order { get; }
    Task ExecuteAsync(LoginContext context, CancellationToken ct);
}

public sealed class LoginContext
{
    public required string StudyName { get; init; }
    public required ISqlExecutor Sql { get; init; }
    public required IProgress<LoginProgress> Progress { get; init; }

    public int StudyId { get; set; }
    public int SessionId { get; set; }
    public StudyUser? User { get; set; }
    public DatabaseInfo? Database { get; set; }
}
```

Steps and their order (each is a separate testable class):

| Order | Step | SQL | Replaces |
|---|---|---|---|
| 0 | `SessionOptionsStep` | `SET XACT_ABORT ON; SET DATEFORMAT ymd;` + `SELECT @@SERVERNAME, DB_NAME()` | `Emetra.Database.Simple.pas:383-389` + `Emetra.Database.Info.pas:146-147` (**moved earlier** — the Delphi ran the first user query before `SET DATEFORMAT`) |
| 100 | `DatabaseInfoStep` | `SELECT SERVERPROPERTY(...)` + `EXEC dbo.GetDatabaseInfo` | `Emetra.Database.Info.pas:99-124` **and** `CRF.Input.EventMap.pas:47` (merged — `EventScale` comes from the same row) |
| 200 | `ActiveUserStep` | `EXEC dbo.GetStudyAndUser @StudyName` | `CRF.Context.ActiveUser.pas:168` — called **once**, not three times; also yields `StudyId` |
| 300 | `StudySessionStep` | `SELECT StudyId FROM dbo.Study WHERE StudName=@StudyName` **only if** `ActiveUserStep` produced `StudyId = 0`; then `EXEC dbo.AddSession @StudyId,@CompName,@CompUser,@CompTime,@AppVer` | `CRF.Context.Session.pas:191-205`; `@AppVer` now carries the real assembly informational version |

`DatabaseInfoStep` keeps the Delphi's deliberate swallow
(`Emetra.Database.Info.pas:154-159`): on failure log an error and set `DbVersion = -1` so the
population list falls back to the no-version query (`EPR.Population.List.pas:109`).
It also keeps the `DbVersion >= 510` check (`Emetra.Database.Info.pas:90`, `:131-135`) but raises
a typed `DatabaseVersionTooOldException`.

**Profession/centre guard (replaces the nil-`GlobalPickList` AV):** after `ActiveUserStep`,
if `User.ProfessionName` or `User.CenterName` is empty, do **not** attempt a picker. Set
`StudySession.HasIncompleteUserProfile = true` and surface a clear, actionable message
("Yrke og arbeidssted må registreres i FastTrak før QuickStat kan brukes") via `IUserNotifier`.
Continue if possible — QuickStat does not actually need `ProfId`/`CenterId` for anything it does.

```csharp
public interface ISessionService : IParameterSource
{
    StudySession? Current { get; }
    bool IsConnected { get; }
    event EventHandler<StudySession>? StudyChanged;   // Delphi: NotifyStudyObservers

    Task<StudySession> ConnectAsync(ConnectionEntry entry, IProgress<LoginProgress> progress,
                                    CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct = default);   // EXEC dbo.CloseSession
}

public sealed record StudySession(
    string StudyName, int StudyId, int SessionId,
    StudyUser User, DatabaseInfo Database,
    string ServerName, string DatabaseName)
{
    public bool HasIncompleteUserProfile { get; init; }
}

public sealed record StudyUser(
    int UserId, string UserName, int PersonId, string FullName,
    int ProfessionId, string ProfessionName, string ProfessionType, string Signature,
    int CenterId, string CenterName, int GroupId, string GroupName,
    bool IsSuperuser, bool IsDbOwner, bool IsSingleGroupUser, bool ShowMyGroup,
    int BlockRules, int RelationCount, int CaseList);

public sealed record DatabaseInfo(
    string ProductVersion, int ProductMajorVersion, int ProductYear,
    string Collation, string ServerName, string WorkstationName, string DbName,
    int DbVersion, string ServerVersion, int EventScale, ServerType ServerType)
{
    public bool Is2016OrHigher => ProductMajorVersion >= 13;   // Emetra.Database.Info.pas:246-249
}
```

`ProductYear` reproduces the switch at `Emetra.Database.Info.pas:203-219`
(6→1996 … 16→2022, else 9999) extended for 17→2025.

### Parameter resolution

```csharp
namespace Quickstat.Session;

/// <summary>Replaces IVariantDictionary + TBusiness.TryGetValue RTTI lookup
/// (Emetra.Classes.Business.pas:79-84).</summary>
public interface IParameterSource
{
    bool TryGetValue(string name, out object? value);   // case-insensitive
}
```

`SessionService.TryGetValue` serves exactly the six names the Delphi's published properties
exposed (`CRF.Context.Facade.pas:97-104`): `StudyId`, `StudyName`, `UserId`, `SessId`,
`CenterId`, `CaseId` (always 0 in QuickStat). No reflection.

```csharp
public readonly record struct DateRange(DateTime Start, DateTime End);   // [Start, End)

public interface IPeriodProvider
{
    /// <summary>Delphi: IPeriodDictionary.TryGetPeriod. Returns null when cancelled.
    /// MUST be called on the UI thread (it shows a dialog).</summary>
    Task<DateRange?> TryGetPeriodAsync(string context, string caption, CancellationToken ct);
}

public interface IParameterResolver
{
    /// <summary>Delphi: TParameterDictionary.TryApplyParameters
    /// (Emetra.Database.ParameterDictionary.pas:79-133).
    /// Returns null when the user cancelled the period dialog.</summary>
    Task<IReadOnlyDictionary<string, object?>?> TryResolveAsync(string sql, CancellationToken ct);
}
```

`ParameterResolver` uses `ISqlTextRewriter` to discover the placeholder names, then:
1. if both `StartDate` and `StopDate` are present → `IPeriodProvider`; null → return null;
2. everything else via `IParameterSource`; an unresolved name → log an error and return null
   (matching `:125-127`), with the name in the message.

`DialogPeriodProvider` persists the last-used range **fixed**: section =
`"Period:" + Sha256Hex(sql)[..16]`, keys `Start` / `End`, defaults `Today-1` / `Today`.
(The Delphi wrote section = `PeriodStart`, key = the whole SQL —
`EPR.PeriodDictionary.pas:65-66, 75-76` — which never round-trips.)

## 3.5 `Quickstat.Configuration` — settings

Only the `ssUser` scope carries anything QuickStat reads, so port that alone.

```csharp
namespace Quickstat.Configuration;

public interface ISettingsStore
{
    bool     TryGet(string section, string key, out string value);
    string   GetString  (string section, string key, string   @default = "");
    int      GetInt32   (string section, string key, int      @default = 0);
    bool     GetBoolean (string section, string key, bool     @default = false);
    double   GetDouble  (string section, string key, double   @default = 0);
    DateTime GetDateTime(string section, string key, DateTime @default);

    void Set(string section, string key, string value);
    void Set(string section, string key, int value);
    void Set(string section, string key, bool value);
    void Set(string section, string key, double value);
    void Set(string section, string key, DateTime value);

    void Flush();          // explicit; the store buffers, unlike TIniFile
}
```

Implementation `IniSettingsStore`:
- File: **`%APPDATA%\DIPS\QuickStat\QuickStat.ini`** (roaming, per user).
  *Deliberate change* from `%APPDATA%\Emetra\Shared\<guid>.ini`: the GUID-named file
  (`Emetra.Settings.IniFile.pas:226-273`) exists only to key an install to a directory, which is
  meaningless for the .NET app, and the `Emetra\Shared` folder is shared with FastTrak.
  A first-run migration that copies the `frmQuickStat.*` sections across is **optional** — the only
  thing lost is window geometry.
- No registry writes. Drop `HKCU\Software\Emetra\QuickStat` (`:277-291`).
- No `[Directory] RootDir` / `[Test] LastOpened` / `[Test] WindowsUserName` pollution (`:311-322`).
- Read into a `Dictionary<string, Dictionary<string,string>>` (ordinal-ignore-case) on
  construction; write on `Flush()` and on process exit. Numbers and dates in
  `CultureInfo.InvariantCulture` (`o` round-trip for dates); tolerate the legacy
  `TIniFile` datetime format on read.

Window state:

```csharp
public interface IWindowStateService
{
    void Restore(Window window);   // Delphi: TGuiSettings.RestoreFormState
    void Save(Window window);      // Delphi: TGuiSettings.SaveFormState
}
```

Section = `$"{window.Name}.{screenWidth}x{screenHeight}"` — keep the resolution-keyed convention
(`Emetra.VclUtil.Settings.pas:70-77`) and keep the monitor-visibility clamp (`:204-205`), now over
`System.Windows.Forms.Screen.AllScreens` or the Win32 monitor APIs.
Keys `State`, `Left`, `Top`, `Width`, `Height` with the same semantics, including
"do not write bounds unless `WindowState == Normal`" (`:246`).

**Recommended additions** (currently not persisted at all, and users notice):
`SelectedConnectionName`, `CheckedCollectors` (semicolon list), `AnonymityMode`,
`ShowDataHint`, `WideColumns`, `ExportDates`, `SplitterPosition`, `LastExportFolder`.
All in a `[QuickStat]` section. Flag these to the product owner as scope additions, not
silent behaviour changes.

## 3.6 `Quickstat.Diagnostics` — logging and the UI split

**The single most important structural change in this document: `ILog` is two things and must
become two interfaces.**

```csharp
// 1. Pure logging -> Microsoft.Extensions.Logging. No UI, ever.
ILogger<T> _log;

// 2. User interaction -> explicit, awaitable, UI-thread-affine.
namespace Quickstat.Diagnostics;

public enum NotificationSeverity { Information, Warning, Error }

public interface IUserNotifier
{
    Task InformAsync(string message, string? title = null);
    Task WarnAsync(string message, string? title = null);
    Task ErrorAsync(string message, string? title = null);

    /// <summary>Delphi: ILog.LogYesNo. Always asks; never fails open.</summary>
    Task<bool> ConfirmAsync(string message, NotificationSeverity severity = NotificationSeverity.Warning,
                            string? title = null);
}
```

`WpfUserNotifier` marshals to the dispatcher, owns the parent window, and **also** writes the
message to `ILogger` at the matching level — so the log still contains everything the Delphi log
contained.

Porting rule for every one of the ~35 sites in §7.4:

| Delphi | C# |
|---|---|
| `Log.Event(msg, ltMessage)` | `_log.LogInformation(msg); await _notifier.InformAsync(msg);` |
| `Log.Event(msg, ltWarning)` | `_log.LogWarning(msg); await _notifier.WarnAsync(msg);` |
| `Log.Event(msg, ltError)` / `ltException` | `_log.LogError(msg); await _notifier.ErrorAsync(msg);` |
| `Log.LogYesNo(msg, ltWarning)` | `await _notifier.ConfirmAsync(msg, NotificationSeverity.Warning)` |
| `Log.Event(msg)` / `Log.Event(msg, ltInfo)` | `_log.LogInformation(...)` — **no dialog** |
| `Log.SilentError/SilentWarning/SilentSuccess` | `_log.LogError/LogWarning/LogInformation` — **no dialog** |
| `Log.LogSqlQuery/LogSqlCommand` | `_log.LogDebug` inside `QuickstatDatabase` |
| `Log.EnterMethod/LeaveMethod` | `using var _ = _log.BeginTimedScope(nameof(X));` at `Debug` |

**Triage — these must become log-only, not dialogs** (they are developer diagnostics that today
interrupt the user):
`CRF.Context.Session.pas:143` (duplicate observer registration),
`EPR.QA.Collection.pas:91` (collector not added),
`Emetra.Business.BaseClass.pas:211`, `:224` (RTTI visibility checks),
`CRF.Patient.List.pas:384` (search failure — show inline in the search box instead).

**And this one must become a loop-aware summary**: `MainQuickStat.pas:803`
(`MSG_UNKNOWN_COLLECTOR`) currently pops one modal per unknown collector inside a `while`.
Collect the names and show **one** dialog listing them.

`{{ }}` PII markers: implement `PiiRedactor.ForLog(text)` → `(Anonymisert)` and
`PiiRedactor.ForDisplay(text)` → markers stripped, content kept
(`Emetra.Logging.Utilities.pas:23-37`). Apply `ForLog` in the logger provider and `ForDisplay`
in `WpfUserNotifier`.

File logging — a small custom `ILoggerProvider` (or Serilog, if the team prefers a dependency):

- Directory: `<AppContext.BaseDirectory>\LOGS\` when it exists **or can be created**; otherwise
  `%LOCALAPPDATA%\DIPS\QuickStat\logs\`. **Create it** — the Delphi's silent
  total log loss (`Emetra.Logging.PlainText.pas:209-211`) is a defect.
- File name: `QuickStat-<username>-yyyyMMdd.log`, daily rolling, retain 10 files
  (mirrors `MaxFile = 10`). Drop the numeric slot scheme and the truncate-on-open.
- Encoding UTF-8 **with BOM**, CRLF — unchanged, so existing ops tooling still reads it.
- Line format kept byte-compatible where it can be:
  `HH:mm:ss.fff\t<level>\t<indent>\t<message>` with level names
  `debug|info|message|warning|error|critical` mapped from
  `Trace/Debug → debug`, `Information → info`, `Warning → warning`, `Error → error`,
  `Critical → critical`. (`message` no longer occurs — it was the "show a dialog" level.)
- Buffered writes, flush every 2 s and on `Warning`+, and on shutdown. No per-line
  `FlushFileBuffers`.
- Default minimum level `Information`; `Debug` (SQL + method tracing) switchable via
  `QUICKSTAT_LOG_LEVEL` or an `[Logging] Level` setting.

Method tracing helper:

```csharp
public static class LoggerScopeExtensions
{
    public static IDisposable BeginTimedScope(this ILogger logger, string name,
                                              [CallerMemberName] string? caller = null);
    // logs "<Type>.<name>: Enter" then "<Type>.<name>: Leave ( n ms )" at Debug
}
```

Off by default (unlike `MainQuickStat.pas:260`, which enables it in release).

## 3.7 DI wiring

`Quickstat.App/App.xaml.cs`:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    var services = new ServiceCollection();

    services.AddLogging(b => b
        .AddQuickstatFile(o =>
        {
            o.BaseDirectory  = AppContext.BaseDirectory;
            o.FileNamePrefix = "QuickStat";
            o.RetainedFiles  = 10;
        })
        .AddDebug()
        .SetMinimumLevel(LogLevel.Information));

    // ---- configuration -------------------------------------------------
    services.AddSingleton<IUdlReader, UdlReader>();
    services.AddSingleton<IConnectionCatalog, XmlConnectionCatalog>();
    services.AddSingleton<IConnectionStringTranslator, OleDbConnectionStringTranslator>();
    services.AddSingleton<ISettingsStore>(_ => IniSettingsStore.OpenDefault());
    services.AddSingleton<IWindowStateService, WindowStateService>();
    services.Configure<SqlOptions>(o =>
    {
        o.DefaultCommandTimeout = TimeSpan.FromSeconds(300);
        o.LogSql = true;                       // Delphi MainQuickStat.pas:270
    });

    // ---- data ----------------------------------------------------------
    services.AddSingleton<ISqlTextRewriter, ColonToAtSqlTextRewriter>();
    services.AddSingleton(new SqlRetryPolicy());
    services.AddSingleton<QuickstatDatabase>();
    services.AddSingleton<ISqlExecutor>(sp => sp.GetRequiredService<QuickstatDatabase>());
    services.AddSingleton<IDatabaseConnection>(sp => sp.GetRequiredService<QuickstatDatabase>());

    // ---- session -------------------------------------------------------
    services.AddSingleton<ILoginStep, SessionOptionsStep>();   // Order 0
    services.AddSingleton<ILoginStep, DatabaseInfoStep>();     // Order 100
    services.AddSingleton<ILoginStep, ActiveUserStep>();       // Order 200
    services.AddSingleton<ILoginStep, StudySessionStep>();     // Order 300
    services.AddSingleton<SessionService>();
    services.AddSingleton<ISessionService>(sp => sp.GetRequiredService<SessionService>());
    services.AddSingleton<IParameterSource>(sp => sp.GetRequiredService<SessionService>());
    services.AddSingleton<IPeriodProvider, DialogPeriodProvider>();
    services.AddSingleton<IParameterResolver, ParameterResolver>();

    // ---- UI ------------------------------------------------------------
    services.AddSingleton<IUserNotifier, WpfUserNotifier>();
    services.AddSingleton<IDialogService, WpfDialogService>();
    services.AddSingleton<MainViewModel>();
    services.AddSingleton<MainWindow>();

    _host = services.BuildServiceProvider();
    _host.GetRequiredService<MainWindow>().Show();
}
```

`SessionService` resolves `IEnumerable<ILoginStep>` and runs them `OrderBy(s => s.Order)` —
adding a step is a one-line registration, and each step is unit-testable against a fake
`ISqlExecutor`.

## 3.8 The async story

The Delphi blocks the UI thread with a wait cursor for every database call
(`Emetra.Database.Simple.pas:711-721`, `MainQuickStat.pas:497`, `:528`, `:647`).
Every one of those becomes `async`/`await`. Precisely:

| User action | Delphi entry point | Round trips | Port |
|---|---|---|---|
| Select project in the combo | `MainQuickStat.pas:495` `SelectConnection` | **≈ 55** (open, 2× SET, `@@SERVERNAME`, 2 info queries, `GetStudyAndUser` ×3, `dbo.Study` ×2, `AddSession`, `GetLabClassVarNames`, **40×** `GetPercentileRanksByClassId`, `GetFormClasses`, `GetPopulations`, `GetPackages`) | `ConnectCommand` = `AsyncRelayCommand`; `await _session.ConnectAsync(entry, progress, ct)`; busy overlay + `IProgress<LoginProgress>` per step; **cancellable** |
| Population double-click | `MainQuickStat.pas:521` `AfterPopulationSelect` → `TPatientList.Load` | 1 + optional period dialog + `AddPopulationLog` | `await LoadPopulationAsync(ct)`. The period dialog must be awaited **on the UI thread before** the query is dispatched |
| "Collect data" | `MainQuickStat.pas:634` `actCollectDataExecute` | `ceil(N/100)` per checked collector — e.g. 20 collectors × 500 patients ≈ **100** | `await CollectAsync(progress, ct)`; report `(collectorIndex, collectorTitle, percent)`; **cancellable** |
| Save data package | `MainQuickStat.pas:845` | 1 (`Report.AddQuickStat`) | `await` |
| Save patient selection | `MainQuickStat.pas:734` → `EPR.QA.Matrix.pas:497-510` | 1 + **N** (`Report.AddSelectionMember` once per patient) | `await`; **batch** — see Risks |
| Delete package | `MainQuickStat.pas:883` | confirm + 1 | `await _notifier.ConfirmAsync(...)` then `await` |
| Reload packages | `MainQuickStat.pas:816` | 1 | `await` |
| Close the window | `MainQuickStat.pas:324` `fCrfContext.Disconnect` | 1 (`dbo.CloseSession`) | `await` in `Window.Closing` with a 5 s timeout; never block shutdown on it |
| Export to Excel / CSV | `MainQuickStat.pas:749`, `:758` | 0 | file I/O; `await` + `Task.Run` for the ClosedXML write |

Rules:

- `ConfigureAwait(false)` **everywhere inside `Quickstat.Data`, `Quickstat.Configuration` and
  `Quickstat.Session`**; **never** in view models (they need the dispatcher context).
- `IProgress<T>` instances are constructed on the UI thread, so `Report` marshals automatically.
  Replace `IStatus`/`IProgress` (`Emetra.Progress.Interfaces.pas:10-28`) with
  `IProgress<OperationProgress>` where
  `record OperationProgress(string Header, string Info, double? Percent)`.
- The wait cursor becomes `MainViewModel.IsBusy` bound to a busy overlay plus
  `Mouse.OverrideCursor = Cursors.Wait`. Never call `Application.ProcessMessages`'s equivalent
  (`DispatcherFrame` pumping).
- Commands are `[RelayCommand]`-generated `IAsyncRelayCommand`s with
  `IncludeCancelCommand = true` for Connect and Collect. `CanExecute` gates on `!IsBusy`, which
  removes the current re-entrancy hazards (the Delphi allows a second combo change mid-connect).
- `QuickstatDatabase`'s `SemaphoreSlim(1,1)` means two concurrent calls serialise rather than
  corrupting the connection.
- Cancellation: pass the token to `OpenAsync`, `ExecuteReaderAsync`, `ExecuteNonQueryAsync`.
  A cancelled SqlClient command sends an attention signal; on `OperationCanceledException`
  **verify the connection is still usable** (`SELECT 1`) and reconnect if not, before releasing
  the gate.
- Long collector runs: do **not** parallelise across collectors in v1. It changes SQL Server load
  characteristics and the shared-connection model forbids it. Revisit with pooled connections and
  a bounded `Parallel.ForEachAsync` once the port is behaviour-equivalent.

---

# Part 4 — Risks and unknowns

**Connection and TLS**

1. **`Encrypt` default (highest risk).** Covered in §3.5. Needs a decision from operations *before*
   first deployment, and a documented per-site override. Unknown: whether the production SQL
   Servers have TLS 1.2 and a usable certificate. Get `SELECT @@VERSION` and the
   `Force Encryption` setting from each site.
2. **UDL path resolution changes from CWD-relative to exe-relative.** Low risk (the two coincide
   when launched from a shortcut whose "Start in" is the exe folder) but it *is* a behaviour
   change; keep the CWD fallback and log which path was used.
3. **UDL files in the wild may not be UTF-16 or may not be 3 lines.** The reader must tolerate
   BOM-less ANSI and files with a trailing blank line. Only one UDL is available in this repo.
4. **`Data Source=localhost` in the shipped config** is almost certainly a developer placeholder;
   the real per-site UDLs are unknown.

**SQL execution**

5. **DB-stored population SQL is opaque.** `dbo.GetPopulations` returns arbitrary T-SQL
   (`CRF.Population.pas:89`, `:115-118`) that the app executes. Unknowns: does any of it repeat a
   `:Name` placeholder; does any of it contain a colon inside a string literal
   (`'23:59'`) or an old-style `::fn_...` call; does any of it rely on `SET DATEFORMAT ymd` for
   literal dates. **Mitigation:** build the rewriter as a proper scanner, add a "dry-run rewrite"
   diagnostic command that dumps every population's SQL before and after rewriting, and run it
   against a production database early.
6. **`Report.AddSelectionMember` is called once per patient** (`EPR.QA.Matrix.pas:507-509`).
   For a 5 000-patient population that is 5 000 round trips. Options: a table-valued parameter
   (needs a new type + procedure overload — a database change), `SqlBulkCopy` into a staging
   table, or batching 500 values per `INSERT ... VALUES`. **Requires a decision with the DBA**;
   the safe interim is to keep the loop but wrap it in a single transaction and await it.
7. **Retry semantics narrow from 10 attempts to 3, and non-idempotent commands stop being
   retried.** This is intentional, but it may surface flakiness that the old code papered over on
   unstable VPN links. Make `MaxAttempts` configurable.
8. **`CommandTimeout` rises from 30 s to 300 s.** A hung query now blocks the (cancellable) UI
   operation for five minutes instead of erroring. Make sure the cancel button actually works.
9. **NVARCHAR parameters against VARCHAR columns.** Preserved from the legacy (ADO also sent
   `adVarWChar`), so no regression — but if profiling shows implicit-conversion scans, the fix is
   per-parameter `SqlDbType.VarChar`, and that needs column-type knowledge we do not have.

**Session and permissions**

10. **The nil-`GlobalPickList` crash path.** Today a user with no profession or work site
    registered gets an assertion (Debug) or an access violation (Release) during login. We are
    replacing it with a message, which is strictly better — but we do not know how common the
    situation is, nor whether QuickStat is *supposed* to be usable in that state.
11. **The `QuickStat` database role's exact grant list is not in this repo.** §4 lists the
    procedures the app calls; the actual `GRANT` script lives in the FastTrak database project.
    Confirm before deployment, and make `SqlPrivilegeException` name the missing object.
12. **Three `StudyId` resolutions collapse to one.** If `dbo.GetStudyAndUser` and
    `dbo.Study` can ever disagree (e.g. a study renamed, or a user not enrolled), the collapsed
    version behaves differently. `StudySessionStep` keeps the `dbo.Study` lookup as a fallback
    when `GetStudyAndUser` yields 0; add a test.
13. **`dbo.AddSession` will now receive a real `@AppVer`.** Confirm the column width
    (it has always received `''`).

**Settings and logging**

14. **Settings file location changes** from `%APPDATA%\Emetra\Shared\<guid>.ini` to
    `%APPDATA%\DIPS\QuickStat\QuickStat.ini`. Only window geometry is lost. If that is
    unacceptable, add the one-off migration; the GUID is discoverable from
    `<exedir>\Settings\emetra.ini` → `[Directory] Identifier`.
15. **The period-dictionary key bug is being fixed**, which means the feature starts working for
    the first time. Users who never saw a remembered date range will now see one. Verify the
    `[start, end)` semantics against a real population query before shipping.
16. **~35 modal dialogs must be individually re-created.** The triage in §3.6 changes several from
    modal to log-only. Every one of those is a deliberate UX change that should be reviewed, not a
    mechanical port.
17. **Log format compatibility.** If any ops tooling parses `QuickStat-<user>-NNN.LOG`, the move
    to daily rolling breaks it. Unknown whether such tooling exists — ask.
18. **`Emetra.Logging.SmartInspect` does not exist in this checkout**, so there is no Debug-build
    reference implementation to compare against. Only the plain-text behaviour is documented here.

**Build / tooling**

19. **No existing `.slnx`, `.csproj` or C# code in the repo** (verified). This document is the
    first artifact of the port; project scaffolding is a prerequisite for everything above.
20. **`Docs/QuickStat.adoc` is a 7-line stub** and `Docs/QuickStat.dox`/`.doxdb` are Documentation
    Insight artifacts — there is no existing prose specification to reconcile against.
