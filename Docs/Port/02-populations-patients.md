# QuickStat port — 02: Populations & Patients ("who are the patients?")

Scope: population selection (left-hand "Select population" pane) and patient-list loading.
Source of truth: `C:\work\FastTrak.Quickstat` (branch `feature/dotnet`), Delphi units under `FastTrak\`
and the app root. Upstream reference: `C:\work\FastTrak`, branch `origin/tarmscreening/develop`.

Target: WPF `net10.0-windows`, C#, CommunityToolkit.Mvvm, `Microsoft.Data.SqlClient`,
Microsoft.Extensions.DI/Logging, ClosedXML, xUnit. Namespaces `Quickstat.Domain` / `Quickstat.Data`.

> **All SQL in this document is verbatim from the Delphi sources.** Implementation agents must
> not invent SQL. Where a Delphi constant is built by concatenation, the fully-resolved string
> is also given.

---

## 0. Object graph (what talks to what)

```
TfrmQuickStat (MainQuickStat.pas)
 ├─ TCRFSimpleContext          (CRF.Context.Facade.pas)   — ISQL + IStudyContext + IVariantDictionary
 ├─ TfrmPopulations            (EPR.VclFrame.Populations.pas)  — the "Select population" frame
 │    ├─ TPopulationList       (EPR.Population.List.pas)   — in-memory catalogue, IObjectList+IObservable
 │    │    └─ TPopulation      (CRF.Population.pas)        — one row of the catalogue
 │    └─ TObjectListView       (Emetra.VclComp.ListView.pas) — owner-drawn 3-column grid
 ├─ TPatientList               (CRF.Patient.List.pas)      — IPersonList, holds TStudyCase items
 │    └─ TParameterDictionary  (Emetra.Database.ParameterDictionary.pas)
 │         └─ TPeriodDictionary(EPR.PeriodDictionary.pas) → TfrmPeriod (Emetra.VclForm.Period.pas)
 └─ TStudyOverviewGrid / TPersonGridData (EPR.QA.Matrix.pas) — the result grid
```

Wiring is done in `MainQuickStat.pas:272` (patient list + parameter dictionary + period dictionary)
and `MainQuickStat.pas:286-293` (population frame, observer registration, English captions).

---

## 1. What IS a "population"?

A **population** is a *saved, named, parameterised query that returns a set of patients* for one
study. It is a row in a server-side catalogue; QuickStat never writes to it — it only enumerates
it and executes the `SqlText` it carries.

### 1.1 The `IPopulation` contract

`FastTrak\CRF.Population.Interfaces.pas:9-25`

| Property      | Type   | Dataset column (`FastTrak\CRF.Population.Interfaces.pas:44-51`) | Meaning |
|---------------|--------|------------------------------------------------|---------|
| `ProcId`      | int    | `ProcId`         (`FLD_PROC_ID`)      | Primary key of the population. Shown as the blue code column. |
| `Group`       | string | `ProcGroup`      (`FLD_PROC_GROUP`)   | Category, e.g. "Type 1", "Type 2", "Prosess", "Studier". Right-aligned magenta column. |
| `Title`       | string | `ProcTitle`      (`FLD_PROC_TITLE`)   | Bold main text, e.g. "HbA1c > 53 (7%)". |
| `QueryText`   | string | `SqlText`        (`FLD_SQL_TEXT`)     | **The statement QuickStat actually executes** to get the patients. Typically `EXEC dbo.GetCaseListXxx :StudyId`. |
| `InfoCaption` | string | `InfoCaption`    (`FLD_INFO_CAPTION`) | Loaded (`CRF.Population.pas:90`) but never rendered anywhere in QuickStat. |
| `SourceCode`  | string | `ProcSourceCode` (`FLD_SOURCE_CODE`)  | The `CREATE PROCEDURE …` text shown in the bottom pane (screenshot 1). |
| *(no property)* | string | `HelpText`     (`FLD_HELP_TEXT`)      | Stored in `fHelpText`; surfaced as `IListBoxItem.Description` — the grey wrapped description line shown when a row is expanded. |

`FLD_PROC_DESC = 'ProcDesc'` is declared (`CRF.Population.Interfaces.pas:47`) but **never read**
by `TPopulation.Load`. It *is* used as a parameter name in the population-change audit call
(see §2.6).

Loader: `FastTrak\CRF.Population.pas:81-95`. Every field is read with `FieldByName(...)`, i.e.
**all seven columns are mandatory** in the result set of the catalogue procedures — a missing
column raises and kills the whole load (caught at `EPR.VclFrame.Populations.pas:249-253`).

### 1.2 The backing table

Not directly queried by the population frame, but `FastTrak\EPR.QA.SQL.pas:37` pins it down:

```sql
SELECT ProcName, ProcDesc, ProcParams FROM dbo.DbProcList WHERE ProcId=:ProcId
```

So the catalogue lives in **`dbo.DbProcList`** (`ProcId`, `ProcName`, `ProcDesc`, `ProcParams`, …)
and the `Populations.*` procedures below project/filter it per study and per DB version.
`QRY_POPULATION_BY_ID` and `QRY_POPULATIONS` are **declared but never called** anywhere in this
repository (verified by grep) — do not port them.

### 1.3 The exact enumeration SQL

`FastTrak\CRF.Population.Interfaces.pas:36-41` — verbatim:

```pascal
const
  { Populations }
  QRY_POPULATIONS                    = 'EXEC dbo.GetPopulations :StudyId';
  QRY_STUDY_POPULATIONS_NO_VERSION   = 'EXEC Populations.GetStudyPopulations :StudyId';
  QRY_STUDY_POPULATIONS_WITH_VERSION = 'EXEC Populations.GetStudyPopulations :StudyId, :DbVer';
  QRY_POPULAR_POPULATIONS            = 'EXEC Populations.GetPopularPopulations :StudyId, :DbVer';
```

Selection logic — `FastTrak\EPR.Population.List.pas:95-121`:

```pascal
procedure TPopulationList.Load( const AStudyId, ADbVersion: integer; const AShowMostCommon: boolean );
var
  dsPopulations: TDataset;
begin
  BeginUpdate;
  try
    fPopulationList.Clear;
    if AStudyId > 0 then
    begin
      if AShowMostCommon then
        dsPopulations := fSQL.FastQuery( QRY_POPULAR_POPULATIONS, [AStudyId, ADbVersion] )
      else if ADbVersion >= 18200 then
        dsPopulations := fSQL.FastQuery( QRY_STUDY_POPULATIONS_WITH_VERSION, [AStudyId, ADbVersion] )
      else
        dsPopulations := fSQL.FastQuery( QRY_STUDY_POPULATIONS_NO_VERSION, [AStudyId] );
      with dsPopulations do
        try
          while not EOF do
            fPopulationList.Add( TPopulation.CreateAndNext( dsPopulations ) );
        finally
          Close;
        end;
    end;
  finally
    EndUpdate;
  end;
end;
```

Resolved to T-SQL for the port (three cases, in priority order):

```sql
-- (a) "Frequently used only" checked  →
EXEC Populations.GetPopularPopulations @StudyId, @DbVer
-- (b) not checked AND DatabaseVersion >= 18200 →
EXEC Populations.GetStudyPopulations   @StudyId, @DbVer
-- (c) not checked AND DatabaseVersion <  18200 →
EXEC Populations.GetStudyPopulations   @StudyId
```

`ADbVersion` is `IDatabaseInfo.DbVersion` (`EPR.VclFrame.Populations.pas:246`), which comes from
`EXEC dbo.GetDatabaseInfo` → column `DatabaseVersion` (`FastTrak\Emetra.Database.Info.pas:81,86,117`).
`fDbVersion` is set to `-1` if that call fails (`Emetra.Database.Info.pas:157`), so the `< 18200`
branch is also the failure branch. `AStudyId <= 0` ⇒ empty list, no query at all.

### 1.4 "Common" — clarification

There is **no** `Common` column. The `Common` concept in the UI is the checkbox
`cbShowCommon` ("Vis de mest brukte" in the DFM, re-captioned to **"Frequently used only"** at
`MainQuickStat.pas:291`). Checking it swaps the catalogue procedure to
`Populations.GetPopularPopulations` — i.e. the *server* decides what is popular
(`dbo.AddPopulationLog`, §2.6, is what feeds that popularity).

### 1.5 Row rendering (screenshot 1)

`TPopulation` implements `IListBoxBase`/`IListBoxItem` (`CRF.Population.pas:15,133-169`):

| Accessor | Returns | Column in `TObjectListView` |
|----------|---------|-----------------------------|
| `V`  | `IntToStr(ProcId)` | `CodeColumn = 0` — painted in `FCodeColor` (blue), left (`Emetra.VclComp.ListView.pas:168,629-645`) |
| `DN` | `Title`            | `TextColumn = 1` — bold, `DT_END_ELLIPSIS` (`…ListView.pas:679-682`) |
| `OT` | `Group`            | same cell, **right-aligned**, one point smaller, `FStatusTextColor` (magenta) (`…ListView.pas:666-676`) |
| `Description` | `HelpText`   | wrapped grey line below the title, only when the row is *expanded* (`…ListView.pas:688-696`, `ExpandRow` at `…ListView.pas:752-755`) |
| `StatusColumn = 2` | — | width 0, `ShowStatus` is never set (`…ListView.pas:322-326`) |

Row-expansion rule (`Emetra.VclComp.ListView.pas:752-755`):
`ExpandRow(i) := (not SimpleView) or (i = Row)` — the *selected* row always expands to show the
description; with "Simplified" unchecked **all** rows are expanded.

`AsListbox` (`CRF.Population.pas:133-139`) produces the TAB-joined string used for filtering:

```pascal
function TPopulation.AsListbox( const ASimple: boolean ): string;
begin
  if ASimple then
    Result := V + #9 + DN + #9 + #9 + OT
  else
    Result := fListBoxText;            // = V + #9 + DN + #9 + Description + #9 + OT   (line 94)
end;
```

---

## 2. The population-selection flow

### 2.1 Construction & wiring

`EPR.VclFrame.Populations.pas:116-130`:

```pascal
procedure TfrmPopulations.AfterConstruction;
begin
  inherited;
  fPopulations := TPopulationList.Create( fSQL, fLog );
  fPopView := TObjectListView.Create( Self );
  fPopView.Prepare( splitMain.UpperLeft.Pane, alClient );
  fPopView.List := fPopulations;
  fPopView.OnDblClick := PopulationRequested;
  fPopView.OnClick := PopulationSelected;
  fPopView.FilterCase := fcLower;
  edtPopFilter.OnChange := RefreshPopulationListFromMemory;
  cbSimpleView.OnClick := RefreshPopulationListFromMemory;
  cbShowCommon.OnClick := ReadPopulationList;
  fObservers := TList<IPopulationObserver>.Create;
end;
```

Note the asymmetry: **"Simplified" and the filter text only re-render from memory;
"Frequently used only" re-queries the database.**

`TPopulationList` is a `TBusiness` ⇒ `IObservable`; `TObjectListView.Set_List`
(`Emetra.VclComp.ListView.pas:447-477`) attaches itself, so `BeginUpdate/EndUpdate` in
`TPopulationList.Load` automatically triggers `TObjectListView.AfterUpdate` and re-filters.

### 2.2 Triggering a reload — study change

`EPR.VclFrame.Populations.pas:144-149` / `239-255`:

```pascal
procedure TfrmPopulations.AfterStudyChange( const Sender: IStudyId );
begin
  cbShowCommon.Enabled := Sender.StudyId > 0;
  fStudyId := Sender;
  ReadPopulationList( Self );
end;

procedure TfrmPopulations.ReadPopulationList( Sender: TObject );
begin
  fCurrentPopulation := nil;
  if fStudyId.StudyId >= 0 then
    try
      fPopulations.Load( fStudyId.StudyId, fDbInfo.DbVersion, cbShowCommon.Checked );
      fLog.SilentSuccess( 'Found %d populations', [fPopulations.Count] );
    except
      on E: Exception do
      begin
        fUsable := false;
        fLog.Event( E.Message, ltException );
      end;
    end;
end;
```

The frame is registered as an `IStudyObserver` at `MainQuickStat.pas:293`
(`fCrfContext.Session.AddStudyObserver( frmPopulations )`), so the catalogue reloads whenever the
user picks another database/protocol in the `cbProject` combo.

### 2.3 Filter / search text + "Simplified"

`EPR.VclFrame.Populations.pas:257-260`:

```pascal
procedure TfrmPopulations.RefreshPopulationListFromMemory( Sender: TObject );
begin
  fPopView.RefreshView( cbSimpleView.Checked, edtPopFilter.Text, false );
end;
```

`Emetra.VclComp.ListView.pas:350-360` lower-cases the filter because `FilterCase = fcLower`.
The actual matching (`Emetra.VclComp.ListView.pas:501-518`):

```pascal
else if Supports( thisObject, IListBoxBase, thisBase ) then
begin
  addThis := false;
  if thisBase.IsCurrent or FShowAll then
  begin
    if Supports( thisObject, IMatchable, thisMatchable ) then
      addThis := thisMatchable.Match( FFilter )
    else
    begin
      textToMatch := AnsiLowercase( thisBase.AsListBox( false ) );
      if ( FFilter = '' ) or ( Pos( FFilter, textToMatch ) > 0 ) then
        addThis := true;
    end;
    …
```

`TPopulation` does **not** declare `IMatchable` (`CRF.Population.pas:15`), so the *else* branch
runs: a **case-insensitive substring test against `AsListbox(false)`**, i.e. against
`ProcId TAB Title TAB HelpText TAB Group`. (`TPopulation.Match` at `CRF.Population.pas:156-159`
does the same thing on the same string, so behaviour is identical either way — but the port
should implement the `AsListbox(false)` version, since that is the code path actually taken.)

Consequences to preserve:
* Typing a number filters by `ProcId` **substring**, not equality (`"26"` matches 26, 126, 260, 267…).
* The group name and the help text are searchable.
* `ASimple` does **not** affect the filter (`AsListBox(false)` is always used for matching), only rendering.

`ReselectStrategy = rsNoChange` (`Emetra.VclComp.ListView.pas:269`) — after re-filtering the grid
keeps the same *row index*, which now points at a different population. Reproduce or fix
deliberately (see Risks).

### 2.4 Click vs. double-click

* **Single click** → `PopulationSelected` (`EPR.VclFrame.Populations.pas:228-237`): fills the bottom
  source pane only.

  ```pascal
  procedure TfrmPopulations.PopulationSelected( Sender: TObject );
  var
    selPop: IPopulation;
  begin
    if memSourceCode.Enabled then
      if TryGetHighlightedPopulation( selPop ) then
        memSourceCode.Text := StringReplace( StringReplace( selPop.SourceCode, #13#10, #10, [rfReplaceAll] ), #10, #13#10, [rfReplaceAll] )
      else
        memSourceCode.Text := EmptyStr;
  end;
  ```

  (The double `StringReplace` normalises mixed CR/LF to CRLF for the VCL memo. In WPF a
  `TextBox`/`AvalonEdit` accepts `\n` directly, so normalisation is optional.)

* **Double click** *or* **Enter** → `PopulationRequested`. Enter is routed to `DblClick` by
  `TObjectListView.DoKeyPress` (`Emetra.VclComp.ListView.pas:762-766`).

  ```pascal
  procedure TfrmPopulations.PopulationRequested( Sender: TObject );
  const
    PROC_NAME = 'PopulationRequested';
  var
    stopWatch: TStopWatch;
    obs: IPopulationObserver;
  begin
    if TryGetHighlightedPopulation( fCurrentPopulation ) then
      try
        stopWatch := TStopWatch.StartNew;
        for obs in fObservers do
          obs.AfterPopulationSelect( fCurrentPopulation );
        fSQL.ExecuteCommand( CMD_LOG_POPULATION_CHANGE, [fStudyId.StudyId, fCurrentPopulation.ProcId, fCurrentPopulation.Title, stopWatch.ElapsedMilliseconds] );
      except
        on E: Exception do
          fLog.SilentWarning( '%s.%s: %s', [ClassName, PROC_NAME, E.Message] );
      end
    else
      fLog.Event( MSG_INVALID_REPORT );        // 'Det er ikke valgt en gyldig populasjon.'
  end;
  ```

  (`EPR.VclFrame.Populations.pas:207-226`.) The hint label under the pane reads
  *"Tip: Double click to prepare population"* (`MainQuickStat.dfm:1026-1033`).

### 2.5 `IsHighlighted` and `TrySelect`

```pascal
function TfrmPopulations.IsHighlighted( APopulation: IPopulation ): boolean;
var
  selPop: IPopulation;
begin
  Result := TryGetHighlightedPopulation( selPop ) and ( selPop.ProcId = APopulation.ProcId );
end;

function TfrmPopulations.TryGetHighlightedPopulation( out APopulation: IPopulation ): boolean;
begin
  APopulation := nil;
  Result := Supports( fPopView.SelectedObject, IPopulation, APopulation );
end;
```
(`EPR.VclFrame.Populations.pas:262-273`)

```pascal
function TfrmPopulations.TrySelect( const AProcId: integer; const ALoadIt: boolean; out APopulation: IPopulation ): boolean;
var
  popObject: TPopulation;
begin
  APopulation := nil;
  if fPopulations.TryGetPopulation( AProcId, popObject ) then
  begin
    Assert( Supports( popObject, IPopulation, APopulation ) );
    Result := fPopView.TrySelectObject( popObject );
    if Result and ALoadIt then
      PopulationRequested( Self );
  end
  else
    Result := false;
end;
```
(`EPR.VclFrame.Populations.pas:186-200`; `TPopulationList.TryGetPopulation` is a linear scan,
`EPR.Population.List.pas:123-139`.)

`TrySelectObject` (`Emetra.VclComp.ListView.pas:245-261`) searches `FLocalList` — the **filtered**
list — so `TrySelect` fails when the target population is filtered out. Its only caller is
`MainQuickStat.pas:789`, when the user double-clicks a saved package:

```pascal
if not frmPopulations.TrySelect( packagedSelection.PopulationId, true, selectedPopulation ) then
  GlobalLog.Event( MSG_UNKNOWN_POPULATION, [packagedSelection.PopulationId], ltWarning )
```

`packagedSelection.PopulationId` comes from `Report.QuickStat.ProcId`
(`QuickStat.Selection.pas:105`, `EPR.QA.SQL.pas:44-45`).

### 2.6 The observer callback into the main form

`FastTrak\CRF.Population.Interfaces.pas:31-34`:

```pascal
IPopulationObserver = interface
  ['{086A34D1-C4FC-4728-A004-3EB268FF25D1}']
  procedure AfterPopulationSelect( APopulation: IPopulation );
end;
```

`MainQuickStat.pas:521-550`:

```pascal
procedure TfrmQuickStat.AfterPopulationSelect( APopulation: IPopulation );
…
    pgDataset.ActivePage := tbsOverview;
    fGrid.Clear;
    if frmPopulations.IsHighlighted( APopulation ) then
    begin
      fGridPopulation := nil;
      fPersonList.Load( APopulation );
      // TODO: Disse feiler, hvor er de??
//     if not fPersonList.IncludesNationalId then
//        fPersonList.AddNationalIds;
      LoadPopulationIntoGrid( APopulation );
      pgSelections.ActivePage := tbsDataElements;
      fGrid.StartPainting;
    end
    else
      GlobalLog.Event( ERR_POPULATION_NOT_SELECTED, ltError );
```

Audit trail, executed after the observers return
(`EPR.VclFrame.Populations.pas:219`, constant at `FastTrak\CRF.SQL.pas:174`):

```sql
EXEC dbo.AddPopulationLog :StudyId, :ProcId, :ProcDesc, :ElapsedMs
```
Arguments in order: `StudyId`, `ProcId`, **`Population.Title`** (passed into the `:ProcDesc`
slot), elapsed milliseconds of the whole observer fan-out. Failures here are swallowed
(`SilentWarning`) and must not break the UI in the port.

### 2.7 The source pane and access control (dead in QuickStat)

`EPR.VclFrame.Populations.pas:168-177` gates `splitMain.LowerRight.Visible` on
`FUNC_POPULATION_SOURCE = 'ADMIN.POPULATION.SOURCE'`
(`FastTrak\Emetra.AccessControl.Constants.pas:112`). **QuickStat never registers an
`IAccessControlManager`**, so `AfterAccessControlChanged` is never called and the pane keeps its
DFM state = visible (confirmed by screenshot 1, which shows `CREATE PROCEDURE
dbo.GetCaseListHbA1c9Plus( @StudyId …`). Port decision: show the source pane unconditionally, but
keep the constant so a permission check can be re-introduced.

---

## 3. Executing a population → the patient list

### 3.1 Entry point

`FastTrak\CRF.Patient.List.pas:281-284`:

```pascal
procedure TPatientList.Load( const APopulation: IPopulation );
begin
  Query( APopulation.QueryText );
end;
```

So the executed statement is **exactly the `SqlText` column of the catalogue row** — dynamic,
server-authored SQL. In practice it is `EXEC dbo.GetCaseListXxx :StudyId[, :StartDate, :StopDate]`,
but nothing in the client constrains it.

### 3.2 `Query` — verbatim

`FastTrak\CRF.Patient.List.pas:286-333`:

```pascal
procedure TPatientList.Query( const AQuery: string );
var
  n: integer;
  qryParams: array of variant;
  thisDataset: TDataset;
  thisStudyCase: TStudyCase;
  infoText: TField;
begin
  CheckAssigned( fParameterDictionary, 'ParameterDictionary' );
  BeginUpdate;
  try
    if ( fStudyContext.StudyId > 0 ) and fParameterDictionary.TryApplyParameters( AQuery, fParamValues ) then
    begin
      fList.Clear;
      SetLength( qryParams, fParamValues.Count );
      n := 0;
      while n < fParamValues.Count do
      begin
        qryParams[n] := fParamValues[n].Value;
        inc( n );
      end;
      thisDataset := fSQL.FastQuery( AQuery, qryParams );
      try
        infoText := thisDataset.FindField( 'InfoText' );
        while not thisDataset.EOF do
        begin
          thisStudyCase := TStudyCase.Create( fStudyContext, fSQL, Log );
          try
            thisStudyCase.Load( thisDataset );
            { Workarounds }
            thisStudyCase.FullName := thisDataset.FieldByName( FLD_FULL_NAME ).AsString;
            if Assigned( infoText ) then
              thisStudyCase.StatusText := infoText.AsString;
            fList.Add( thisStudyCase );
          except
            on E: Exception do
              thisStudyCase.Free;
          end;
          thisDataset.Next;
        end;
      finally
        thisDataset.Close;
      end;
    end;
  finally
    EndUpdate;
  end;
end;
```

**Three behaviours that must be reproduced or consciously fixed:**

1. `fList.Clear` is *inside* the `if`. If `StudyId <= 0` or the parameter dictionary returns
   `False` (user cancelled the period dialog, or an unresolvable `:Param`), the **previous
   population's patients stay in the list** and are re-shown under the new population's title.
   *This is a bug.* See Risks R2.
2. Parameter binding is **positional**: `fParamValues[n]` → `qryParams[n]` →
   `fQuery.Parameters[n].Value := AParams[n]`
   (`FastTrak\Emetra.Database.Simple.pas:415-433`). Both parsers (Delphi `TParams.ParseSQL` and
   ADO's `ParamCheck`) enumerate `:Name` placeholders left to right, which is why this works.
3. `FieldByName( FLD_FULL_NAME )` — `FLD_FULL_NAME = 'FullName'`
   (`FastTrak\Emetra.Person.SQL.pas:32`) — **raises** if the column is absent. The `except`
   silently `Free`s the row, so a population that omits `FullName` yields **zero patients with no
   error message**. The port must log this.

### 3.3 Result columns consumed

Row load chain: `TStudyCase.Load` → `TCustomStudyPerson.Load` → `TCRFPerson.Load` →
`TStoredListItem.Load`. All of these use `ReadInteger/ReadString/ReadDateTime/ReadBool`, which are
`FindField`-based and **tolerant of missing columns**
(`FastTrak\Emetra.Classes.Subject.Stored.pas:186-262`).

| Column | Default if missing | Read at | Target |
|--------|--------------------|---------|--------|
| `PersonId` | `-1` | `CRF.Person.pas:467` | `FPrimaryKey`, `fPerson.PersonId` |
| `FullName` | **raises → row dropped** | `CRF.Patient.List.pas:316` | `TPerson.Set_FullName` (parsed, see §3.4) |
| `DOB` | `0` | `CRF.Person.pas:471` | `fPerson.DOB` |
| `NationalId` | `''` | `CRF.Person.pas:470` | `fPerson.NationalId` — *usually absent*, see §5 |
| `GenderId` | `0` | `CRF.Person.pas:472` | `fPerson.Sex` (1=Male, 2=Female, else Unknown; `Emetra.Person.pas:363-377`) |
| `FstName` / `MidName` / `LstName` | `''` | `CRF.Person.pas:473-475` | overwritten later by `FullName` parsing |
| `Initials` | `''` | `CRF.Person.pas:468` | `fInitials` |
| `EmployeeNumber`, `HPRNo`, `EmailAddress`, `GSM` | `-1`/`''` | `CRF.Person.pas:476-479` | |
| `StreetAddress`, `PostalCode`, `City` | `''` | `CRF.Person.pas:481` | |
| `GroupId`, `GroupName` | `-1`/`''` | `CRF.Person.pas:618-619` | shown by some collectors |
| `CenterId/Name/Address/City/Phone/Postcode` | `-1`/`''` | `CRF.Person.pas:612-617` | unused by QuickStat |
| `RelId`, `RelName`, `StatusActive`, `StatusId`, `StatusText`, `ClinRelId`, `TestCase` | `-1`/`''`/`0`/`false` | `CRF.Person.StudyCase.pas:148-154` | |
| `InfoText` | optional (`FindField`) | `CRF.Patient.List.pas:309,317-318` | **overwrites `StatusText`** |
| `PK`, `LastUpdate` | `-1`/`0` | `Emetra.Classes.Subject.Stored.pas:213-214` | irrelevant here |

**Minimum contract for a population procedure: `PersonId` and `FullName`.**
Screenshot 1 confirms the typical shape:
`SELECT v.PersonId, v.DOB, v.FullName, v.GroupName, … FROM dbo.ViewActiveCaseListStub v`.

### 3.4 `FullName` parsing — subtle, must be reproduced

`TPerson.Set_FullName` (`FastTrak\Emetra.Person.pas:328-361`) splits on **comma**, `StrictDelimiter`:

* `"Nordmann, Ola"` → `LastName = "Nordmann"`, `FirstName = "Ola"`.
* `"Ola Nordmann"` (no comma) → falls into the `else` branch, which re-splits with the *same*
  comma delimiter, so `LastName = "Ola Nordmann"`, `FirstName = ""`.
* `""` → both empty.

The grid row is then built as `LastName + ', ' + FirstName`
(`FastTrak\EPR.QA.Matrix.Row.pas:90-97`), i.e. the grid always shows **"Last, First"**.
Note that `TPersonGridRow.Create(IPersonReadOnly)` reads `LastName`/`FirstName`, **not** `FullName`.
`IPersonReadOnly` on a `TStudyCase` resolves to the inner `TPerson` via
`property Person: IPersonReadOnly read Get_Person implements IPersonReadOnly;`
(`CRF.Person.pas:76`) — *not* to `TCRFPerson`, whose `Get_FullName` would have produced
`First Middle Last`.

### 3.5 The other patient-list entry points (also reached by QuickStat's stack)

Not used by the population flow but present in `TPatientList` and worth porting for completeness:

`FastTrak\CRF.SQL.pas:170` — the default list loaded on study change
(`CRF.Patient.List.pas:407`):

```sql
EXEC dbo.GetCaseList :StudyId
```

`FastTrak\CRF.Patient.List.pas:102-108` — free-text search (verbatim):

```pascal
const
  { Searches are limited to invividuals already enrolled in study, see #498565. }
  JOIN_STUDY = ' JOIN dbo.StudCase sc ON sc.StudyId=:StudyId AND sc.PersonId = p.PersonId ';

  { Queries with "fuzzy" criteria, i.e. that may get more than one hit. }
  QRY_STUDY_PERSON_BY_DOB       = SELECT_PERSON + JOIN_STUDY + 'WHERE p.DOB = :DOB' + TAIL_ORDER_BY;
  QRY_STUDY_PERSON_BY_DOB_NAME  = SELECT_PERSON + JOIN_STUDY + 'WHERE p.DOB = :DOB AND p.LstName LIKE :PartialLastName' + TAIL_ORDER_BY;
  QRY_STUDY_PERSON_BY_LAST_NAME = SELECT_PERSON + JOIN_STUDY + 'WHERE p.LstName LIKE :SearchFor' + TAIL_ORDER_BY;
```

with (`FastTrak\Emetra.Person.SQL.pas:9-16`):

```pascal
  SELECT_PERSON = 'SELECT p.* FROM dbo.Person p ';
  TAIL_ORDER_BY = ' ORDER BY p.LstName, p.FstName';

  QRY_PERSON_BY_ID        = SELECT_PERSON + 'WHERE p.PersonId = :PersonId' + TAIL_ORDER_BY;
  QRY_PERSON_BY_NATID     = SELECT_PERSON + 'WHERE p.NationalId = :NationalId' + TAIL_ORDER_BY;
```

Fully resolved:

```sql
-- QRY_STUDY_PERSON_BY_DOB
SELECT p.* FROM dbo.Person p  JOIN dbo.StudCase sc ON sc.StudyId=@StudyId AND sc.PersonId = p.PersonId WHERE p.DOB = @DOB ORDER BY p.LstName, p.FstName
-- QRY_STUDY_PERSON_BY_DOB_NAME
SELECT p.* FROM dbo.Person p  JOIN dbo.StudCase sc ON sc.StudyId=@StudyId AND sc.PersonId = p.PersonId WHERE p.DOB = @DOB AND p.LstName LIKE @PartialLastName ORDER BY p.LstName, p.FstName
-- QRY_STUDY_PERSON_BY_LAST_NAME
SELECT p.* FROM dbo.Person p  JOIN dbo.StudCase sc ON sc.StudyId=@StudyId AND sc.PersonId = p.PersonId WHERE p.LstName LIKE @SearchFor ORDER BY p.LstName, p.FstName
-- QRY_PERSON_BY_ID
SELECT p.* FROM dbo.Person p WHERE p.PersonId = @PersonId ORDER BY p.LstName, p.FstName
-- QRY_PERSON_BY_NATID
SELECT p.* FROM dbo.Person p WHERE p.NationalId = @NationalId ORDER BY p.LstName, p.FstName
```

Dispatch rules (`CRF.Patient.List.pas:350-386`) with regexes from
`FastTrak\Emetra.Person.Interfaces.pas:177-185`:

```pascal
  RGX_DATE              = '^[0123]\d\.?[01]\d\.?(18|19|20)?\d{2}';
  RGX_VALID_DOB         = RGX_DATE + '$';
  RGX_VALID_NATIONAL_ID = '^\d{11}$';
  RGX_SPLIT_NATIONAL_ID = '^\d{6}\s?\d{5}$';
  RGX_MOBILE_PHONE      = '^\d{8}$';
  RGX_NAME              = '(\p{L}+(\-\p{L}+)*)';
  RGX_TWO_NAMES         = RGX_NAME + '\s+' + RGX_NAME;
  RGX_DOB_AND_NAME      = RGX_DATE + '\s+' + RGX_NAME + '$';
  RGX_DOB_AND_TWO_NAMES = RGX_DATE + '\s+' + RGX_TWO_NAMES + '$';
```

Order: empty → nothing; 11-digit (optional space) → `QRY_PERSON_BY_NATID` (whitespace stripped);
date + name → `QRY_STUDY_PERSON_BY_DOB_NAME` (`name + '%'`); date → `QRY_STUDY_PERSON_BY_DOB`
(formatted `yyyy-mm-dd`); pure integer > 0 → `QRY_PERSON_BY_ID`; otherwise name →
`QRY_STUDY_PERSON_BY_LAST_NAME` (`text + '%'`).

> QuickStat's current UI does **not** expose patient search — `TPatientList.Search` has no caller
> in this repository. Port it behind an interface but do not build UI for it in phase 1.

---

## 4. Parameters: `TParameterDictionary`, `TPeriodDictionary`, the Period form

### 4.1 Composition

`MainQuickStat.pas:272`:

```pascal
fPersonList := TPatientList.Create( fCrfContext,
  TParameterDictionary.Create( TPeriodDictionary.Create( fSettings, GlobalLog ), fCrfContext, GlobalLog ),
  fCrfContext.Database, GlobalLog );
```

So: period source = `TPeriodDictionary` (ini-backed + modal dialog); variant source =
`TCRFSimpleContext` itself.

### 4.2 `TryApplyParameters` — verbatim

`FastTrak\Emetra.Database.ParameterDictionary.pas:79-133`:

```pascal
function TParameterDictionary.TryApplyParameters( const AQuery: string; AParams: TParams ): boolean;
const
  PROC_NAME = 'TryApplyParameters';
var
  startDate, stopDate: TDateTime;
  prmIndex: integer;
  prm: TParam;
  prmStartDate: TParam;
  prmStopDate: TParam;
  prmValue: variant;
begin
  Result := true;
  Guard.CheckNotNull( fVariantDictionary, 'VarSupplier' );
  Guard.CheckNotNull( AParams, 'Params' );
  { Use built-in parsing capability of TParams to create parameter list }
  AParams.ParseSQL( AQuery, true );
  { Get start and stop date from the period dictionary  if these parameters appear in query }
  prmStartDate := AParams.FindParam( PRM_START_DATE );
  prmStopDate := AParams.FindParam( PRM_STOP_DATE );
  if Assigned( prmStartDate ) and Assigned( prmStopDate ) then
  begin
    Guard.CheckNotNull( fPeriodDictionary, 'PeriodDictionary' );
    if fPeriodDictionary.TryGetPeriod( AQuery, rsSelectPeriod, startDate, stopDate ) then
    begin
      prmStartDate.Value := startDate;
      prmStopDate.Value := stopDate;
    end
    else
    begin
      Log.SilentWarning( LOG_USER_CANCELLED, [ClassName, PROC_NAME] );
      { This means that the user dialog was canceled or the period was invalid }
      Result := false;
      exit;
    end;
  end;
  { Set the rest of the parameters via the fVariantDictionary interface }
  prmIndex := 0;
  while prmIndex < AParams.Count do
  begin
    prm := AParams[prmIndex];
    if prm.IsNull then
    begin
      if fVariantDictionary.TryGetValue( prm.Name, prmValue ) then
        prm.Value := prmValue
      else
      begin
        Log.SilentError( LOG_UNRESOLVED_PARAMETER, [ClassName, PROC_NAME, prm.Name, prmIndex] );
        Result := false;
        break;
      end;
    end;
    Log.Event( LOG_PARAMETER_SET, [ClassName, PROC_NAME, prm.Name, VarToStr( prm.Value )] );
    inc( prmIndex );
  end;
end;
```

Constants (`…ParameterDictionary.pas:53-64`):

```pascal
resourcestring
  rsSelectPeriod = 'Denne spørringen krever at du angir et tidsintervall.';
const
  PRM_START_DATE = 'StartDate';
  PRM_STOP_DATE  = 'StopDate';
```

### 4.3 When is the user prompted?

**Only** when the population's `SqlText` contains **both** `:StartDate` **and** `:StopDate`
(`FindParam` is case-insensitive in Delphi). Then a modal two-calendar dialog appears. Any other
placeholder is resolved silently from the variant dictionary.

### 4.4 The resolvable parameter names (complete list)

`fVariantDictionary` is `TCRFSimpleContext`, a `TObjectContainer : TBusiness`, whose
`TryGetValue` (`FastTrak\Emetra.Classes.Business.pas:79-84`) is pure published-property RTTI:

```pascal
function TBusiness.TryGetValue( const AVarName: string; var AValue: variant ): boolean;
begin
  Result := IsPublishedProp( Self, AVarName );
  if Result then
    AValue := GetPropValue( Self, AVarName );
end;
```

`TCRFSimpleContext`'s published section (`FastTrak\CRF.Context.Facade.pas:97-104`) is therefore the
whole vocabulary:

| Placeholder | Value |
|---|---|
| `:StudyId`   | current study id |
| `:StudyName` | current study name |
| `:CenterId`  | active user's centre |
| `:UserId`    | active user id |
| `:SessId`    | current session id |
| `:CaseId`    | active patient id (0 in QuickStat) |
| `:StartDate` / `:StopDate` | from the period dialog (pair only) |

Anything else ⇒ `SilentError` + `Result := false` ⇒ **nothing is loaded and the previous list
survives** (§3.2 note 1).

### 4.5 `TPeriodDictionary` — verbatim

`FastTrak\EPR.PeriodDictionary.pas:35-37, 54-80`:

```pascal
const
  KEY_PERIOD_START = 'PeriodStart';
  KEY_PERIOD_END   = 'PeriodEnd';

function TPeriodDictionary.TryGetPeriod( const AContext, ACaption: string; out APeriodStart, APeriodEnd: TDateTime ): boolean;
var
  obs: IGuiStyleObserver;
begin
  Result := false;
  if fForm = nil then
    fForm := TfrmPeriod.Create( Application );
  if Assigned( fGuiStyle ) and Supports( fForm, IGuiStyleObserver, obs ) then
    obs.UpdateStyle( fGuiStyle );
  if Assigned( fSettings ) then
  begin
    APeriodStart := fSettings.ReadDate( ssUser, KEY_PERIOD_START, AContext, Now - 1 );
    APeriodEnd := fSettings.ReadDate( ssUser, KEY_PERIOD_END, AContext, Now );
  end;
  fForm.lblSubheader.Caption := ACaption;
  fForm.CalendarView1.Date := APeriodStart;
  fForm.CalendarView2.Date := APeriodEnd;
  if fForm.TryGetPeriod( APeriodStart, APeriodEnd ) then
  begin
    if Assigned( fSettings ) then
    begin
      fSettings.WriteDateTime( ssUser, KEY_PERIOD_START, AContext, APeriodStart );
      fSettings.WriteDateTime( ssUser, KEY_PERIOD_END, AContext, APeriodEnd );
    end;
    Result := true;
  end;
end;
```

Key points:
* **`AContext` is the full SQL text of the query** (passed from `TryApplyParameters`). It is used
  as the settings *section/context* key, so the last period is remembered **per query**.
  In an `.ini` that means a section name containing the whole `EXEC … :StartDate, :StopDate`
  string. For the port, hash it (`SHA256`/`ProcId`) instead — but keep the semantics
  "remember the period per population".
* Defaults: `Now - 1` (yesterday) and `Now`.
* Scope `ssUser` (`FastTrak\Emetra.Settings.Interfaces.pas:33`).
* The form is created once and reused.

### 4.6 The Period form

`FastTrak\Emetra.VclForm.Period.pas:48-79`:

```pascal
function TfrmPeriod.TryGetPeriod( out APeriodStart, APeriodEnd: TDateTime ): boolean;
begin
  CalendarView1.OnChange := VerifyInput;
  CalendarView2.OnChange := VerifyInput;
  Result := ( ShowModal = btnOk.ModalResult ) and ( CalendarView1.Date < CalendarView2.Date );
  APeriodStart := CalendarView1.Date;
  APeriodEnd := CalendarView2.Date;
end;

procedure TfrmPeriod.VerifyInput( Sender: TObject );
begin
  btnOk.Enabled := ( CalendarView1.Date < CalendarView2.Date );
  if btnOk.Enabled then
    lblBottomRightInfo.Caption := rsValidInput
  else
    lblBottomRightInfo.Caption := rsInvalidInput;
end;
```

UI text (`Emetra.VclForm.Period.pas:36-42`, `Emetra.VclForm.Period.dfm`):

* Window / header: **"Angi periode"**
* Sub-header: the caption passed in — `"Denne spørringen krever at du angir et tidsintervall."`
* Tip label: *"Tips: Klikk på månedens navn for å 'zoome ut' hvis datoen du vil ha er langt unna."*
* Valid: *"Angis som fra og med første dato (til venstre),\nog til men ikke inkludert siste dato (til høyre)."*
* Invalid: *"Siste dato må være etter første dato.\nMerk at siste dato ikke er med i perioden."*
* Buttons **OK** / **Avbryt**; two `TCalendarView`s side by side, `FirstDayOfWeek = dwMonday`,
  `FirstYear = 1900`.

**Semantics: `[StartDate, StopDate)` — end date exclusive.** Validation is strictly
`Start < Stop` (equal dates rejected).

WPF replacement: a modal `PeriodDialog` with two `Calendar` controls, OK disabled while
`Start >= Stop`, same Norwegian strings, same inclusive/exclusive wording.

---

## 5. Known gap: `AddNationalIds` / `IncludesNationalId`

### 5.1 The problem in the current branch

* `TPatientList` in `FastTrak\CRF.Patient.List.pas` has **no** `AddNationalIds` and no
  `IncludesNationalId` (see the class declaration at lines 35-91).
* `MainQuickStat.pas:537-539` therefore has them commented out:

  ```pascal
      fPersonList.Load( APopulation );
      // TODO: Disse feiler, hvor er de??
  //     if not fPersonList.IncludesNationalId then
  //        fPersonList.AddNationalIds;
  ```
* Consequence: `NationalId` is almost always empty, because population procedures generally do not
  return it. This is stated in the code itself
  (`FastTrak\EPR.QA.Matrix.Row.pas:174-177`):

  ```pascal
    fldNatId := ADataset.FindField( 'NationalId' );
    { NationalId missing from most Population datasets }
  ```
* Therefore the **"Fully identified patients"** radio button
  (`rbFullIdentification`, `MainQuickStat.dfm:1256`; `PersonGridIdentification → pgiFull`,
  `MainQuickStat.pas:905-915`) shows and exports an **empty `Fødselsnummer` column**, even though
  `TPersonGrid.Set_Anonymous` un-hides it (`FastTrak\EPR.QA.GUI.Grid.pas:321-331`) and
  `SaveToFile` writes it (`FastTrak\EPR.QA.Matrix.pas:469`).

### 5.2 Recovered implementation (verbatim, `origin/tarmscreening/develop`)

Recovered with `git -C C:/work/FastTrak show origin/tarmscreening/develop:CRF/CRF.Patient.List.pas`.
`origin/tarmscreening/develop` is the **only** branch in `C:\work\FastTrak` that has this code —
`develop_old` and `origin/develop` have neither `AddNationalIds` nor `QRY_PERSON_LIST_NATIONAL_IDS`.
`C:\work\FastTrak.Quickstat` has never contained it in any of its three commits.

**`fIncludesNationalId` field** — upstream `CRF/CRF.Patient.List.pas:46`, last member of the
`strict private` section (after `fParameterDictionary: IParameterDictionary;`):

```pascal
    fIncludesNationalId: boolean;
```

**`AddNationalIds` declaration** — upstream `CRF/CRF.Patient.List.pas:81`, in `public`, between
`function MovePrevious: boolean;` and `procedure Load( const APopulation: IPopulation );`:

```pascal
    procedure AddNationalIds;
```

**`IncludesNationalId`** is *not* a method — it is a read-only `published` property.
Upstream `CRF/CRF.Patient.List.pas:91`, between `property Count` and `property ItemIndex`:

```pascal
    property IncludesNationalId: boolean read fIncludesNationalId;
```

**`AddNationalIds` implementation** — upstream `CRF/CRF.Patient.List.pas:124-153`, sitting between
`TPatientList.Create` and `TPatientList.AfterConstruction` inside `{$REGION 'Initialization'}`:

```pascal
procedure TPatientList.AddNationalIds;
var
  sc: TStudyCase;
  scList: TDictionary<integer, TStudyCase>;
  sqlList: string;
begin
  scList := TDictionary<integer, TStudyCase>.Create;
  try
    sqlList := EmptyStr;
    for sc in fList do
    begin
      scList.Add( sc.PersonId, sc );
      sqlList := sqlList + Format( ', %d', [sc.PersonId] );
    end;
    with fSQL.FastQuery( Format( QRY_PERSON_LIST_NATIONAL_IDS, [Copy( sqlList, 3, maxint )] ) ) do
      try
        while not EOF do
        begin
          if scList.TryGetValue( Fields[0].AsInteger, sc ) then
            sc.NationalId := Fields[1].AsString;
          Next;
        end;
        fIncludesNationalId := true;
      finally
        Close;
      end;
  finally
    scList.Free;
  end;
end;
```

**Second write site** — upstream `CRF/CRF.Patient.List.pas:343`, inside `TPatientList.Query`, as
the first statement after `thisDataset := fSQL.FastQuery( AQuery, qryParams ); try`, i.e. it
precedes the existing `infoText := thisDataset.FindField( 'InfoText' );` line:

```pascal
        fIncludesNationalId := Assigned( thisDataset.FindField( FLD_NATIONAL_ID ) );
```

### 5.3 The recovered SQL and field constants (verbatim)

Upstream path `LIB/Service/Emetra.Person.SQL.pas`. `QRY_PERSON_LIST_NATIONAL_IDS` is at line 22,
`FLD_NATIONAL_ID` at line 36. The constant is a **standalone literal** — no concatenation with
`SELECT_PERSON`/`TAIL_ORDER_BY`:

```pascal
  QRY_PERSON_LIST_NATIONAL_IDS = 'SELECT PersonId, NationalId FROM dbo.Person WHERE PersonId IN ( %s ) AND NOT NationalId IS NULL';
```

```pascal
  FLD_NATIONAL_ID     = 'NationalId';
```

Fully resolved T-SQL (with `%s` replaced by a comma-separated list of `PersonId`s):

```sql
SELECT PersonId, NationalId FROM dbo.Person WHERE PersonId IN ( <ids> ) AND NOT NationalId IS NULL
```

Note `AND NOT NationalId IS NULL` — patients without a registered national id are simply absent
from the result set, so their `NationalId` stays empty. The port must keep that (do not write
`""`/`NULL` back over an existing value).

Diff against the local repo:
* `FastTrak\Emetra.Person.SQL.pas` is otherwise identical to upstream; **only
  `QRY_PERSON_LIST_NATIONAL_IDS` is missing** (`FLD_NATIONAL_ID` is already there at line 35).
* `FastTrak\CRF.Patient.List.pas` differs from upstream in exactly 5 hunks — the 4 listed in §5.2
  plus one **local-only fix that must be preserved**, not reverted
  (local `CRF.Patient.List.pas:376` vs upstream `:411`):

  ```diff
  -      ADataset := fSQL.FastQuery( QRY_STUDY_PERSON_BY_DOB, [fStudyContext.StudyId, searchDOB] )
  +      ADataset := fSQL.FastQuery( QRY_STUDY_PERSON_BY_DOB, [fStudyContext.StudyId, FormatDateTime( 'yyyy-mm-dd', searchDOB )] )
  ```

  Everything else (uses clauses, the `JOIN_STUDY` / `QRY_STUDY_PERSON_*` const block) is
  byte-identical, so re-enabling the feature in Delphi needs no new unit references.

**Two latent bugs in the recovered code — do NOT carry them over:**

* **B1 — empty list ⇒ SQL syntax error.** With `fList` empty, `sqlList` stays empty and
  `Copy( sqlList, 3, maxint )` returns `''`, producing `… WHERE PersonId IN (  ) AND NOT
  NationalId IS NULL`, which SQL Server rejects. There is no guard upstream.
* **B2 — `fIncludesNationalId := true` is unconditional.** It is set even when the query returned
  zero rows (i.e. nobody in the population has a national id), so a subsequent
  `if not IncludesNationalId then AddNationalIds` never retries. In the port, derive the flag from
  the data rather than latching it (see §5.4).

Additionally, `scList.Add( sc.PersonId, sc )` throws on a duplicate `PersonId`. `TPatientList`
does not de-duplicate (only the grid's dictionary does, §6.1), so a population procedure that
returns the same patient twice crashes `AddNationalIds`. Use `TryAdd`/`AddOrSetValue` semantics
in the port.

### 5.4 What the port must do instead

The recovered implementation builds an `IN` list by string concatenation of `PersonId`s. **Do not
port that literally.** Two hard limits:

* `Microsoft.Data.SqlClient` allows at most **2100 parameters** per command, so a naive
  "one parameter per PersonId" rewrite breaks at ~2 099 patients. Real QuickStat populations
  routinely exceed that (screenshot 2 shows a small demo set, but production protocols have
  tens of thousands of cases).
* A literal concatenated `IN (…)` list is an injection surface and also hits SQL Server's
  practical batch-text limits, and it defeats plan caching (a new plan per distinct list length).

**Recommendation — table-valued parameter (primary).** Add a TVP type once:

```sql
CREATE TYPE dbo.IntIdList AS TABLE ( Id INT NOT NULL PRIMARY KEY );
```

and query with:

```sql
SELECT p.PersonId, p.NationalId
FROM   dbo.Person p
JOIN   @Ids i ON i.Id = p.PersonId
WHERE  p.NationalId IS NOT NULL;   -- same filter as the upstream 'AND NOT NationalId IS NULL'
```

```csharp
using var cmd = new SqlCommand(Sql.PersonListNationalIds, conn);
var tvp = cmd.Parameters.AddWithValue("@Ids", ToIdTable(personIds));
tvp.SqlDbType = SqlDbType.Structured;
tvp.TypeName  = "dbo.IntIdList";
```

**Fallback — batched `IN` chunks (use only if creating the TVP type is not permitted).**
Chunk `personIds` into blocks of **1000** and issue one parameterised command per chunk with
named parameters `@p0…@p999` (1000 keeps a comfortable margin below the 2100 limit and matches
SQL Server's row-constructor sweet spot). Never interpolate the ids into the SQL text.

**Decision for this port: use the TVP.** It is one round-trip, one cached plan, and no length
limit. The fallback exists only for environments where DDL is unavailable; gate it behind a
configuration flag and log which path was used.

`IncludesNationalId` should become a cheap **derived** in-memory predicate rather than the latched
boolean field of §5.2. Its purpose is to avoid a second round-trip when the population procedure
already returned the column:

```csharp
/// Replaces fIncludesNationalId. Derived, so it cannot get stuck 'true' (bug B2).
public bool IncludesNationalId =>
    Patients.Count > 0 && Patients.All(p => !string.IsNullOrEmpty(p.NationalId));
```

If you want to keep the upstream "did the result set carry the column?" meaning as well, record it
separately as `bool PopulationReturnedNationalIdColumn` when mapping the reader (the analogue of
`fIncludesNationalId := Assigned( thisDataset.FindField( FLD_NATIONAL_ID ) )`), and use the derived
predicate to decide whether to fetch.

### 5.5 When to call it

Match the (commented-out) original at `MainQuickStat.pas:536-540`: immediately after
`Load(population)` and **before** `LoadPopulationIntoGrid`, because
`TPersonGridRow.Create(IPersonReadOnly)` snapshots `NationalId` into the grid row
(`FastTrak\EPR.QA.Matrix.Row.pas:96`) and never refreshes it.

Better for the port: only fetch national ids when they are actually needed, i.e. when
`rbFullIdentification` is selected — and re-fetch (or invalidate the grid) if the user switches
the radio button after loading. `ToggleGridAnonymity` (`MainQuickStat.pas:685-688`) currently only
flips column widths.

---

## 6. Sorting and identity

### 6.1 Sort order

`FastTrak\EPR.QA.Matrix.pas:28`:

```pascal
TPersonGridSortOrder = ( sbPersonId, sbReverseName );
```

Default is `sbReverseName` (`EPR.QA.Matrix.pas:121`), but QuickStat overrides it on every load
(`MainQuickStat.pas:563-566`):

```pascal
      fGrid.Data.ClearPopulation;
      fGrid.Data.SortBy := sbPersonId;
      fGrid.Data.PreparePopulation( fPersonList );
```

`Set_SortBy` throws once the grid is locked (`EPR.QA.Matrix.pas:512-520`), which is why the
assignment happens before `PreparePopulation`.

The sort itself (`FastTrak\EPR.QA.Matrix.pas:358-388`):

```pascal
    for gridRow in fPopulation.Values do
      sortedList.Add( gridRow );
    case fSortBy of
      sbReverseName: sortedList.SortByName;
      sbPersonId: sortedList.SortByPersonId;
    end;
```

Comparers (`FastTrak\EPR.QA.Matrix.Row.pas:232-242`):

```pascal
function TNameComparer.Compare( const Left, Right: TPersonGridRow ): integer;
begin
  Result := CompareStr( Left.FullName, Right.FullName );   // ordinal, case-sensitive
end;

function TPersonIdComparer.Compare( const Left, Right: TPersonGridRow ): integer;
begin
  Result := Left.PersonId - Right.PersonId;                // ascending, integer subtraction
end;
```

So: **the grid is always ordered by ascending `PersonId` in QuickStat** (matches screenshot 2:
8, 13, 17, 24, 27, 28, 38, 46, 47, 51, 52, 53, 54, 72, 73, 83, 2076). Any `ORDER BY` inside the
population procedure is discarded — the rows go through a
`TObjectDictionary<integer, TPersonGridRow>` (`EPR.QA.Matrix.pas:30-33, 409-430`) first, which also
**de-duplicates by `PersonId`** (`if not fPopulation.TryGetValue(...) then Add`).

Port note: `Left.PersonId - Right.PersonId` overflows for ids far apart; use
`Comparer<int>.Default` / `a.CompareTo(b)`.

### 6.2 Identity fields

`FastTrak\EPR.QA.Matrix.Interfaces.pas:153-163` — the four fixed grid columns:

```pascal
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
```

`TPersonGridRow` (`FastTrak\EPR.QA.Matrix.Row.pas:23-60`) is the identity carrier in the grid:
`PersonId` (int, key), `DOB` (TDate), `FullName` (string, "Last, First"), `NationalId` (string,
**writable** — this is the setter `AddNationalIds` targets), `GenderId` (int) / `Sex`
(`Row.pas:210-217`).

Identification modes (`FastTrak\EPR.QA.Matrix.pas:26`, `MainQuickStat.pas:905-915`):

| Radio button (`MainQuickStat.dfm`) | Enum | Effect |
|---|---|---|
| "Fully identified patients" (`rbFullIdentification`, l.1256) | `pgiFull` | all four columns exported |
| "Identified with PID only" (`rbKeepPids`, l.1244, **default `Checked`**) | `pgiPersonIdOnly` | DOB / NationalId / Name skipped |
| "Generate new random PIDs" (`rbRandomisePids`, l.1234) | `pgiRandomPersonId` | PID replaced via `TMatrixAnonymizer`, other three skipped |

Screen visibility is separate from export: `fGrid.Anonymous := not rbFullIdentification.Checked`
(`MainQuickStat.pas:685-688`) sets `ColWidths[NAME|DOB|NATIONAL_ID] := -1`
(`FastTrak\EPR.QA.GUI.Grid.pas:321-331`); `Get_Anonymous` is inferred from
`ColWidths[COL_PERSON_NAME] < 0` (`Grid.pas:207-210`). Export filtering happens independently in
`SaveToFile` (`EPR.QA.Matrix.pas:463-471`).

Other identity notes:
* `TStudyCase.Id` is `IntToStr(PersonId)` (`CRF.Person.StudyCase.pas:241-244`).
* `VisualId` = `"DDMMYY NNNNN - Full Name"` or `"dd.mm.yyyy - Full Name"` when the national id is
  missing (`Emetra.Person.pas:236-248`, `Emetra.Person.Interfaces.pas:222-234`).
* `BestId` = `NationalId` if present, else `DateToStr(DOB)` (`CRF.Person.pas:196-202`).
* `ShortId` = initials + `ddmmyy` (`CRF.Person.pas:537-544`).
* `TPersonGridRow.Load(TDataset)` (`EPR.QA.Matrix.Row.pas:167-182`) is **dead in QuickStat** — the
  grid row is always built from `IPersonReadOnly` in `PreparePopulation`. Do not port it.

---

## 7. Dead-code verdict: `QuickStat.Component.ReportTree.pas`

**Verdict: DEAD CODE. Do not port.**

Evidence:
1. `QuickStat.dpr:3-11` — the `uses` clause is
   `Emetra.Logging.SmartInspect|PlainText`, `Vcl.Forms`, `MainQuickStat`, `QuickStat.Collectors`.
   `QuickStat.Component.ReportTree` is absent.
2. `grep -rn "ReportTree" --include="*.pas" --include="*.dpr" --include="*.dproj" --include="*.dfm"`
   over the whole repository returns **only the 11 self-references inside
   `QuickStat.Component.ReportTree.pas` itself** — no other unit references `TReportTree` or
   `TPopulationTreeNode`.
3. `grep -c ReportTree QuickStat.dproj DbFormExport.dproj` → `0` and `0`. The unit is not in either
   project file, so it is not even compiled.
4. It is also functionally obsolete: it is a `TTreeView`-based grouped browser that predates the
   flat `TObjectListView` used by `TfrmPopulations`. `TReportTree.Filter`
   (`QuickStat.Component.ReportTree.pas:75-85`) is an empty loop with the comment
   `{ Not implemented }`, and `TReportTree.Load` builds tree nodes from a `TDataset` that nothing
   supplies.

The only idea worth keeping is the **grouping by `ProcGroup`** it implements
(`ReportTree.pas:150-168`). If the WPF port wants a grouped population list, use
`CollectionViewSource.GroupDescriptions` on `Population.Group` — no need to resurrect this unit.

---

## 8. Proposed C# design

### 8.1 `Quickstat.Domain` — models

```csharp
namespace Quickstat.Domain;

/// <summary>One row of the server-side population catalogue (dbo.DbProcList projected by
/// Populations.GetStudyPopulations / Populations.GetPopularPopulations).</summary>
public sealed record Population
{
    public required int    ProcId      { get; init; }   // ProcId
    public required string Title       { get; init; }   // ProcTitle
    public string Group       { get; init; } = "";      // ProcGroup
    public string QueryText   { get; init; } = "";      // SqlText  — executed verbatim
    public string HelpText    { get; init; } = "";      // HelpText — description line
    public string InfoCaption { get; init; } = "";      // InfoCaption (unused today)
    public string SourceCode  { get; init; } = "";      // ProcSourceCode — bottom pane

    /// <summary>Mirrors TPopulation.fListBoxText (CRF.Population.pas:94).</summary>
    public string SearchText => $"{ProcId}\t{Title}\t{HelpText}\t{Group}";

    public bool Matches(string? filter) =>
        string.IsNullOrEmpty(filter) ||
        SearchText.Contains(filter, StringComparison.CurrentCultureIgnoreCase);

    /// <summary>True when the query needs a period from the user (both placeholders present).</summary>
    public bool RequiresPeriod => SqlPlaceholders.RequiresPeriod(QueryText);
}

public enum Sex { Unknown = 0, Male = 1, Female = 2 }

/// <summary>A patient in a loaded population. Flattens TStudyCase down to what QuickStat reads.</summary>
public sealed class Patient
{
    public required int PersonId { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string FirstName { get; private set; } = "";
    public string LastName  { get; private set; } = "";
    public string? NationalId { get; set; }          // settable: filled by AddNationalIds
    public int GenderId { get; init; }
    public Sex Sex => GenderId switch { 1 => Sex.Male, 2 => Sex.Female, _ => Sex.Unknown };
    public int    GroupId   { get; init; }
    public string GroupName { get; init; } = "";
    public int    StatusId  { get; init; }
    public string StatusText { get; set; } = "";     // overwritten by the InfoText column
    public bool   IsTestCase { get; init; }

    /// <summary>"Last, First" — exactly what TPersonGridRow shows (EPR.QA.Matrix.Row.pas:94).</summary>
    public string DisplayName => $"{LastName}, {FirstName}";

    /// <summary>Ports TPerson.Set_FullName (Emetra.Person.pas:328-361). Comma-delimited,
    /// strict: "Last, First" splits; anything else becomes LastName with an empty FirstName.</summary>
    public void SetFullName(string? value)
    {
        var s = (value ?? "").Trim();
        if (s.Length == 0) { FirstName = ""; LastName = ""; return; }
        var parts = s.Split(',');
        if (parts.Length == 2) { LastName = parts[0].Trim(); FirstName = parts[1].Trim(); }
        else                   { LastName = s;               FirstName = ""; }
    }
}
```

### 8.2 `Quickstat.Domain` — abstractions

```csharp
namespace Quickstat.Domain;

public interface IPopulationRepository
{
    /// <summary>Populations.GetPopularPopulations / Populations.GetStudyPopulations.</summary>
    Task<IReadOnlyList<Population>> GetPopulationsAsync(
        int studyId, int dbVersion, bool frequentlyUsedOnly, CancellationToken ct = default);

    /// <summary>dbo.AddPopulationLog — fire-and-forget audit; must never throw to the caller.</summary>
    Task LogPopulationSelectedAsync(
        int studyId, int procId, string procTitle, long elapsedMs, CancellationToken ct = default);
}

public interface IPatientRepository
{
    /// <summary>Executes population.QueryText with the supplied parameter values.</summary>
    Task<IReadOnlyList<Patient>> LoadPopulationAsync(
        Population population, IReadOnlyDictionary<string, object?> parameters, CancellationToken ct = default);

    /// <summary>dbo.GetCaseList :StudyId</summary>
    Task<IReadOnlyList<Patient>> GetCaseListAsync(int studyId, CancellationToken ct = default);

    /// <summary>Recovers national ids for an arbitrary number of person ids (TVP-backed).</summary>
    Task<IReadOnlyDictionary<int, string>> GetNationalIdsAsync(
        IReadOnlyCollection<int> personIds, CancellationToken ct = default);

    /// <summary>Free-text search — QRY_PERSON_BY_NATID / _BY_ID / _BY_DOB[_NAME] / _BY_LAST_NAME.</summary>
    Task<IReadOnlyList<Patient>> SearchAsync(int studyId, string searchText, CancellationToken ct = default);
}

/// <summary>Ports IParameterDictionary (Emetra.Database.ParameterDictionary.Interfaces.pas:9-12).</summary>
public interface IQueryParameterResolver
{
    /// <summary>False when the user cancelled the period dialog or a placeholder is unknown.</summary>
    bool TryResolve(string sqlText, out IReadOnlyDictionary<string, object?> values, out string? failureReason);
}

/// <summary>Ports IPeriodDictionary (Emetra.Dictionary.Interfaces.pas:69-72).
/// <paramref name="context"/> is the query text; used as the persistence key.</summary>
public interface IPeriodPrompt
{
    bool TryGetPeriod(string context, string caption, out DateTime startDate, out DateTime stopDate);
}

/// <summary>Ports IVariantDictionary over TCRFSimpleContext's published properties.</summary>
public interface ISessionContext
{
    int    StudyId   { get; }
    string StudyName { get; }
    int    CenterId  { get; }
    int    UserId    { get; }
    int    SessId    { get; }
    int    CaseId    { get; }
    int    DbVersion { get; }
    bool TryGetValue(string name, out object? value);   // case-insensitive
}
```

### 8.3 `Quickstat.Data` — SQL constants and placeholder handling

Keep every statement in one static class so implementation agents never guess:

```csharp
namespace Quickstat.Data;

internal static class Sql
{
    // --- population catalogue (CRF.Population.Interfaces.pas:38-41) ---
    public const string PopularPopulations       = "Populations.GetPopularPopulations"; // @StudyId, @DbVer
    public const string StudyPopulations         = "Populations.GetStudyPopulations";   // @StudyId[, @DbVer]
    public const int    DbVersionWithVersionArg  = 18200;

    // --- audit (CRF.SQL.pas:174) ---
    public const string AddPopulationLog = "dbo.AddPopulationLog";  // @StudyId,@ProcId,@ProcDesc,@ElapsedMs

    // --- default case list (CRF.SQL.pas:170) ---
    public const string GetCaseList = "dbo.GetCaseList";            // @StudyId

    // --- person search (Emetra.Person.SQL.pas + CRF.Patient.List.pas:103-108) ---
    private const string SelectPerson = "SELECT p.* FROM dbo.Person p ";
    private const string JoinStudy    = " JOIN dbo.StudCase sc ON sc.StudyId=@StudyId AND sc.PersonId = p.PersonId ";
    private const string TailOrderBy  = " ORDER BY p.LstName, p.FstName";

    public const string PersonById       = SelectPerson + "WHERE p.PersonId = @PersonId"   + TailOrderBy;
    public const string PersonByNatId    = SelectPerson + "WHERE p.NationalId = @NationalId" + TailOrderBy;
    public const string StudyPersonByDob      = SelectPerson + JoinStudy + "WHERE p.DOB = @DOB" + TailOrderBy;
    public const string StudyPersonByDobName  = SelectPerson + JoinStudy + "WHERE p.DOB = @DOB AND p.LstName LIKE @PartialLastName" + TailOrderBy;
    public const string StudyPersonByLastName = SelectPerson + JoinStudy + "WHERE p.LstName LIKE @SearchFor" + TailOrderBy;

    // --- national-id recovery (see §5; TVP form, replaces the upstream concatenated IN list) ---
    public const string PersonListNationalIds =
        "SELECT p.PersonId, p.NationalId FROM dbo.Person p JOIN @Ids i ON i.Id = p.PersonId " +
        "WHERE p.NationalId IS NOT NULL";
    public const string IdListTypeName = "dbo.IntIdList";
    // Upstream (do not use): 'SELECT PersonId, NationalId FROM dbo.Person
    //   WHERE PersonId IN ( %s ) AND NOT NationalId IS NULL'  -- QRY_PERSON_LIST_NATIONAL_IDS

    // --- column names actually read ---
    public const string ColPersonId   = "PersonId";
    public const string ColFullName   = "FullName";
    public const string ColDob        = "DOB";
    public const string ColNationalId = "NationalId";
    public const string ColGenderId   = "GenderId";
    public const string ColGroupId    = "GroupId";
    public const string ColGroupName  = "GroupName";
    public const string ColStatusId   = "StatusId";
    public const string ColStatusText = "StatusText";
    public const string ColInfoText   = "InfoText";
    public const string ColTestCase   = "TestCase";
}
```

Placeholder translation (Delphi/ADO `:Name` → `Microsoft.Data.SqlClient` `@Name`):

```csharp
public static partial class SqlPlaceholders
{
    // Skips '::', qualified names and doubled colons; run AFTER stripping literals/comments.
    [GeneratedRegex(@"(?<![:\w]):(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    public static IReadOnlyList<string> Extract(string sql) => …;   // order of appearance, distinct
    public static string ToSqlClient(string sql) =>
        PlaceholderRegex().Replace(StripLiterals(sql), m => "@" + m.Groups["name"].Value);

    public static bool RequiresPeriod(string sql)
    {
        var names = Extract(sql);
        return names.Contains("StartDate", StringComparer.OrdinalIgnoreCase)
            && names.Contains("StopDate",  StringComparer.OrdinalIgnoreCase);
    }
}
```

Delphi bound parameters **positionally**; `Microsoft.Data.SqlClient` binds by name, which is
strictly safer and handles repeated placeholders correctly. Add each *distinct* name once.
`StripLiterals` must blank out `'…'`, `[…]`, `--` and `/* */` before scanning so that a colon
inside a literal is not mistaken for a placeholder.

### 8.4 `Quickstat.Data` — implementations (sketch)

```csharp
internal sealed class SqlPopulationRepository(ISqlConnectionFactory factory,
                                              ILogger<SqlPopulationRepository> log)
    : IPopulationRepository
{
    public async Task<IReadOnlyList<Population>> GetPopulationsAsync(
        int studyId, int dbVersion, bool frequentlyUsedOnly, CancellationToken ct)
    {
        if (studyId <= 0) return [];                              // EPR.Population.List.pas:102

        await using var conn = await factory.OpenAsync(ct);
        await using var cmd  = conn.CreateCommand();
        cmd.CommandType = CommandType.StoredProcedure;

        if (frequentlyUsedOnly)
        {
            cmd.CommandText = Sql.PopularPopulations;
            cmd.Parameters.AddWithValue("@StudyId", studyId);
            cmd.Parameters.AddWithValue("@DbVer",   dbVersion);
        }
        else if (dbVersion >= Sql.DbVersionWithVersionArg)
        {
            cmd.CommandText = Sql.StudyPopulations;
            cmd.Parameters.AddWithValue("@StudyId", studyId);
            cmd.Parameters.AddWithValue("@DbVer",   dbVersion);
        }
        else
        {
            cmd.CommandText = Sql.StudyPopulations;
            cmd.Parameters.AddWithValue("@StudyId", studyId);
        }
        …  // map ProcId/ProcGroup/ProcTitle/SqlText/InfoCaption/HelpText/ProcSourceCode
    }
}
```

`SqlPatientRepository.LoadPopulationAsync` must:
1. `CommandType.Text`, `CommandText = SqlPlaceholders.ToSqlClient(population.QueryText)`.
2. Add one `SqlParameter` per distinct placeholder from `parameters`.
3. Read with `SqlDataReader`; resolve column ordinals **once** via a `TryGetOrdinal` helper that
   returns `-1` when absent (mirrors `FindField`).
4. `PersonId` and `FullName` are mandatory — if `FullName` is missing, throw a typed
   `PopulationSchemaException` naming the population, instead of silently returning zero rows.
5. De-duplicate by `PersonId` and sort ascending (see §6.1) — or leave that to the grid layer,
   but do it in exactly one place.

### 8.5 View-model shape

```csharp
namespace Quickstat.App.ViewModels;

public sealed partial class PopulationsViewModel : ObservableObject
{
    private readonly IPopulationRepository _repo;
    private readonly ISessionContext _session;
    private readonly List<Population> _all = [];             // ~ TPopulationList

    public ObservableCollection<Population> Visible { get; } = [];   // ~ TObjectListView.FLocalList

    [ObservableProperty] private string _filterText = "";
    [ObservableProperty] private bool   _frequentlyUsedOnly;         // cbShowCommon  → re-query
    [ObservableProperty] private bool   _simplifiedView;             // cbSimpleView  → re-render only
    [ObservableProperty] private Population? _highlighted;           // single click
    [ObservableProperty] private string _sourceCode = "";            // bottom pane

    // filter/simplified change → ApplyFilter(); frequentlyUsedOnly change → ReloadAsync()
    partial void OnFilterTextChanged(string value)      => ApplyFilter();
    partial void OnSimplifiedViewChanged(bool value)    => /* rendering only */ ;
    partial void OnFrequentlyUsedOnlyChanged(bool v)    => _ = ReloadAsync();
    partial void OnHighlightedChanged(Population? p)    => SourceCode = p?.SourceCode ?? "";

    public bool CanShowSource { get; init; } = true;   // FUNC_POPULATION_SOURCE, unused in QuickStat

    [RelayCommand] public async Task ReloadAsync() { … }             // ~ ReadPopulationList
    [RelayCommand] public async Task PrepareAsync(Population? p)     // ~ PopulationRequested (dbl-click / Enter)
    {
        p ??= Highlighted;
        if (p is null) { /* 'Det er ikke valgt en gyldig populasjon.' */ return; }
        var sw = Stopwatch.StartNew();
        PopulationPrepared?.Invoke(this, p);                          // ~ IPopulationObserver
        _ = _repo.LogPopulationSelectedAsync(_session.StudyId, p.ProcId, p.Title, sw.ElapsedMilliseconds);
    }
    public bool TrySelect(int procId, out Population? population);    // ~ TrySelect, for saved packages
    public bool IsHighlighted(Population p) => Highlighted?.ProcId == p.ProcId;

    public event EventHandler<Population>? PopulationPrepared;
}
```

```csharp
public sealed partial class PatientListViewModel : ObservableObject
{
    private readonly IPatientRepository _patients;
    private readonly IQueryParameterResolver _parameters;

    public ObservableCollection<Patient> Patients { get; } = [];
    [ObservableProperty] private Population? _current;

    public bool IncludesNationalId =>
        Patients.Count > 0 && Patients.All(p => !string.IsNullOrEmpty(p.NationalId));

    public async Task<bool> LoadAsync(Population p, CancellationToken ct = default)
    {
        if (!_parameters.TryResolve(p.QueryText, out var values, out var why))
        {
            Patients.Clear();                     // deliberate FIX of the Delphi behaviour (R2)
            Current = null;
            return false;
        }
        var rows = await _patients.LoadPopulationAsync(p, values, ct);
        Patients.Clear(); foreach (var r in rows) Patients.Add(r);
        Current = p;
        return true;
    }

    public async Task EnsureNationalIdsAsync(CancellationToken ct = default)
    {
        if (IncludesNationalId || Patients.Count == 0) return;
        var map = await _patients.GetNationalIdsAsync(Patients.Select(p => p.PersonId).ToArray(), ct);
        foreach (var p in Patients)
            if (map.TryGetValue(p.PersonId, out var nid)) p.NationalId = nid;
    }
}
```

`PopulationsViewModel.PopulationPrepared` is the C# equivalent of
`IPopulationObserver.AfterPopulationSelect`. The shell view-model subscribes, calls
`PatientListViewModel.LoadAsync`, then `EnsureNationalIdsAsync` when the "Fully identified
patients" mode is active, then hands the list to the grid view-model.

Suggested DI registrations (`Microsoft.Extensions.DependencyInjection`):
`ISqlConnectionFactory` (scoped per connection string), `IPopulationRepository`,
`IPatientRepository` (singleton, stateless), `IPeriodPrompt` → `WpfPeriodPrompt` (owns the dialog),
`IQueryParameterResolver` → `SessionParameterResolver(ISessionContext, IPeriodPrompt, IUserSettings)`.

### 8.6 Testability (xUnit)

Pure-logic units that need no database and should be covered first:
* `Population.Matches` / `SearchText` — the tab-joined substring semantics of §2.3.
* `Patient.SetFullName` — all three branches of §3.4.
* `SqlPlaceholders.Extract` / `ToSqlClient` / `RequiresPeriod` — including literals with colons.
* `SessionParameterResolver` — the seven known names, the StartDate/StopDate pair rule, cancel ⇒
  `false`, unknown name ⇒ `false` + reason.
* Sort/de-duplication: `PersonId` ascending, duplicate `PersonId` collapses to one row.
* Chunking helper for the `IN`-list fallback (exact chunk sizes at 999/1000/1001/2100 ids).

---

## 9. Risks and gotchas

| # | Risk | Where | Mitigation |
|---|------|-------|------------|
| **R1** | Population `SqlText` is **server-authored SQL executed verbatim** by the client. Anyone who can write `dbo.DbProcList` gets code execution in the DB session. | `CRF.Patient.List.pas:283` | Keep it (it is the product), but run with a least-privilege login, log every executed statement, and never concatenate user input into it. |
| **R2** | Cancelling the period dialog (or an unresolvable `:Param`) leaves the **previous** population's patients in the list, which are then re-shown under the *new* population's title. | `CRF.Patient.List.pas:297-299` + `MainQuickStat.pas:532-540` | Fix in the port: clear the list and the header on failure (shown in §8.5). Call it out in the release notes as a deliberate behaviour change. |
| **R3** | A population that does not return `FullName` produces **zero patients with no error**. | `CRF.Patient.List.pas:316,320-322` | Throw/log a typed `PopulationSchemaException` naming the population and the missing column. |
| **R4** | 2100-parameter limit / concatenated `IN` lists. Applies to `AddNationalIds` **and** to every collector (`PID_LIST_PLACEHOLDER = '{IdList}'`, `EPR.QA.SQL.pas:13`, substituted by string concatenation at `EPR.QA.Collector.Base.pas:196-202` with `FMaxBatchSize := maxint` at `:221`). | §5.4 | Introduce `dbo.IntIdList` TVP once and reuse it for collectors too; fallback = 1000-id chunks with named parameters. |
| **R5** | `TrySelect` only finds populations that survive the current filter (`FLocalList.IndexOf`). Restoring a saved package can silently fail. | `Emetra.VclComp.ListView.pas:245-261`, `MainQuickStat.pas:789` | In the port, look the population up in the *full* collection and clear the filter before selecting. |
| **R6** | `ReselectStrategy = rsNoChange`: after filtering, the highlighted row index is preserved, so the *selected population changes identity* without a click. | `Emetra.VclComp.ListView.pas:269,527-530` | Bind `SelectedItem` to the `Population` object, not the index (WPF does this naturally). |
| **R7** | The period is persisted with **the whole SQL text as the settings context key** — huge, fragile ini section names. | `EPR.PeriodDictionary.pas:65-66,75-76` | Key on `ProcId` (or a hash of the SQL) and migrate silently; keep the "per-population memory" behaviour. |
| **R8** | Period semantics are **`[Start, Stop)`** with strict `Start < Stop`. Getting this wrong shifts every cohort by a day. | `Emetra.VclForm.Period.pas:52,74` | Preserve exactly; unit-test the boundary; keep the Norwegian explanatory text. |
| **R9** | `TPersonIdComparer` uses `Left.PersonId - Right.PersonId` (overflow-prone). | `EPR.QA.Matrix.Row.pas:239-242` | Use `int.CompareTo`. |
| **R10** | `TCRFSimpleContext.TryGetValue` is RTTI over *published properties*. Adding a property silently adds a resolvable `:Placeholder`; renaming one silently breaks populations. | `Emetra.Classes.Business.pas:79-84` | Replace with an explicit, tested name→value map (`ISessionContext.TryGetValue`); log the whole resolved parameter set at Debug level like `LOG_PARAMETER_SET` does. |
| **R11** | `TPopulation.Load` uses `FieldByName` for **all seven** catalogue columns, so a schema change in `Populations.GetStudyPopulations` breaks the entire list with one exception. | `CRF.Population.pas:81-95` | Read by ordinal-or-default and log missing columns once per load; only `ProcId` + `ProcTitle` + `SqlText` are truly required. |
| **R12** | `dbo.AddPopulationLog` runs **after** the observers, inside the same try; if the grid load throws, the audit row is never written. | `EPR.VclFrame.Populations.pas:214-223` | Write the audit row in a `finally`, fire-and-forget, never blocking the UI. |
| **R13** | Filtering by a numeric string is a **substring** match on `ProcId`. Users typing "26" get 126/260/267. | §2.3 | Preserve (it is expected behaviour) but consider exact-id match when the filter parses as an int and an exact id exists — behaviour change, needs sign-off. |
| **R14** | Case-insensitive comparison used `AnsiUppercase`/`AnsiLowercase` (Norwegian locale, æ/ø/å). | `CRF.Population.pas:158`, `Emetra.VclComp.ListView.pas:352-355` | Use `StringComparison.CurrentCultureIgnoreCase` with an explicitly-set `nb-NO` culture, or `InvariantCultureIgnoreCase`; decide once and test with "Ø"/"ø", "Å"/"å". |
| **R15** | `ADbVersion = -1` (database-info call failed, `Emetra.Database.Info.pas:154-158`) silently routes to the *no-version* catalogue procedure. | `EPR.Population.List.pas:106` | Surface the failure; do not treat "unknown version" as "old version" without a warning. |

---

## 10. Quick reference — every SQL statement in this area

| Purpose | Statement (Delphi form) | Constant / location |
|---|---|---|
| Populations, "frequently used" | `EXEC Populations.GetPopularPopulations :StudyId, :DbVer` | `QRY_POPULAR_POPULATIONS`, `CRF.Population.Interfaces.pas:41` |
| Populations, DB ≥ 18200 | `EXEC Populations.GetStudyPopulations :StudyId, :DbVer` | `QRY_STUDY_POPULATIONS_WITH_VERSION`, `:40` |
| Populations, DB < 18200 | `EXEC Populations.GetStudyPopulations :StudyId` | `QRY_STUDY_POPULATIONS_NO_VERSION`, `:39` |
| *(declared, never called)* | `EXEC dbo.GetPopulations :StudyId` | `QRY_POPULATIONS`, `:38` and `EPR.QA.SQL.pas:36` |
| *(declared, never called)* | `SELECT ProcName, ProcDesc, ProcParams FROM dbo.DbProcList WHERE ProcId=:ProcId` | `QRY_POPULATION_BY_ID`, `EPR.QA.SQL.pas:37` |
| Population audit | `EXEC dbo.AddPopulationLog :StudyId, :ProcId, :ProcDesc, :ElapsedMs` | `CMD_LOG_POPULATION_CHANGE`, `CRF.SQL.pas:174` |
| Run a population | *(the population's own `SqlText`)* | `CRF.Patient.List.pas:283` |
| Default case list | `EXEC dbo.GetCaseList :StudyId` | `QRY_GET_CASELIST`, `CRF.SQL.pas:170` |
| Anonymous case list | `EXEC dbo.GetCaseListAnonymous :StudyId` | `QRY_ACTIVE_ANONYMOUS`, `CRF.SQL.pas:139` |
| Single study case | `EXEC CRF.GetStudyCase :StudyId, :PersonId` | `QRY_LOAD_STUDYCASE`, `CRF.SQL.pas:142` |
| Study id by name | `SELECT StudyId FROM dbo.Study WHERE StudName=:StudName` | `QRY_STUDY_BY_NAME`, `EPR.QA.SQL.pas:35` |
| Saved packages | `SELECT r.* FROM Report.QuickStat r JOIN dbo.Study s ON s.StudyId=r.StudyId WHERE r.StudyId=:StudyId` | `QRY_GET_PACKAGES`, `EPR.QA.SQL.pas:44` |
| Save package | `EXEC Report.AddQuickStat :StudyId,:ProcId,:Title,:DataElements,:Comment` | `CMD_ADD_PACKAGE`, `EPR.QA.SQL.pas:45` |
| Delete package | `EXEC QuickStat.DeletePackage :RowId` | `QuickStat.Selection.pas:129` |
| Save selection | `EXEC Report.AddSelection :StudyId, :Title, :Description` | `QRY_ADD_SELECTION`, `EPR.QA.SQL.pas:40` |
| Selection member | `EXEC Report.AddSelectionMember :SelId, :PersonId` | `CMD_ADD_SELECTION_MEMBER`, `EPR.QA.SQL.pas:41` |
| Person by id | `SELECT p.* FROM dbo.Person p WHERE p.PersonId = :PersonId ORDER BY p.LstName, p.FstName` | `QRY_PERSON_BY_ID`, `Emetra.Person.SQL.pas:14` |
| Person by national id | `SELECT p.* FROM dbo.Person p WHERE p.NationalId = :NationalId ORDER BY p.LstName, p.FstName` | `QRY_PERSON_BY_NATID`, `Emetra.Person.SQL.pas:15` |
| Person national id | `SELECT NationalId FROM dbo.Person WHERE PersonId=:PersonId` | `QRY_PERSON_NATIONAL_ID`, `Emetra.Person.SQL.pas:20` |
| Study person by DOB | *(see §3.5)* | `CRF.Patient.List.pas:106` |
| Study person by DOB + name | *(see §3.5)* | `CRF.Patient.List.pas:107` |
| Study person by last name | *(see §3.5)* | `CRF.Patient.List.pas:108` |
| National ids for a list (upstream, string-concatenated) | `SELECT PersonId, NationalId FROM dbo.Person WHERE PersonId IN ( %s ) AND NOT NationalId IS NULL` | `QRY_PERSON_LIST_NATIONAL_IDS`, recovered from `origin/tarmscreening/develop:LIB/Service/Emetra.Person.SQL.pas:22` — **missing locally** |
| National ids for a list (port, TVP) | `SELECT p.PersonId, p.NationalId FROM dbo.Person p JOIN @Ids i ON i.Id = p.PersonId WHERE p.NationalId IS NOT NULL` | §5.4 |
| Database version | `EXEC dbo.GetDatabaseInfo` → `DatabaseVersion` | `Emetra.Database.Info.pas:81,86,117` |
