# 04 — Result matrix, datapoints, colouring, anonymisation and export

Analysis of the Delphi VCL implementation and the proposed WPF / .NET 10 design.
Scope: the *result data model* (matrix), *datapoints*, *cell colouring*, *anonymisation* and *export*.
Everything below is derived from the sources in `C:\work\FastTrak.Quickstat`. Citations are `file:line`.

Target stack for the port: WPF `net10.0-windows`, C#, `.slnx`, flat repo layout, CommunityToolkit.Mvvm,
`Microsoft.Data.SqlClient`, Microsoft.Extensions.DI/Logging, ClosedXML, xUnit.

---

## 0. Source map

| Concern | Delphi unit |
| --- | --- |
| Matrix (rows × columns, cell lookup, export) | `FastTrak\EPR.QA.Matrix.pas` |
| Row = one person + its datapoints | `FastTrak\EPR.QA.Matrix.Row.pas` |
| Column = one variable | `FastTrak\EPR.QA.Matrix.Column.pas` |
| Interfaces + column constants + Norwegian headers | `FastTrak\EPR.QA.Matrix.Interfaces.pas` |
| Pseudonymiser | `FastTrak\EPR.QA.Matrix.Anoymizer.pas` (filename typo is in the repo) |
| Datapoint base + coloured base | `FastTrak\EPR.QA.DataPoint.pas` |
| Datapoint subclasses | `FastTrak\EPR.QA.DataPoint.{Biochemistry,Pharmacology,VitalSigns,HeartFailure,Dogfood}.pas` |
| Risk palette constants | `FastTrak\EPR.QA.DataPoint.Colors.pas` |
| Datapoint class registry | `FastTrak\EPR.QA.PointFactory.pas` |
| Caption dictionary + record | `FastTrak\EPR.QA.CaptionDictionary.pas`, `FastTrak\EPR.QA.CaptionRecord.pas` |
| Caption SQL | `FastTrak\EPR.QA.SQL.pas:150-167` |
| Percentile colouring (**used**) | `QuickStat.Percentile.pas` (repo root) |
| Percentile ranker dictionary (**partly used**) | `FastTrak\EPR.Lab.Percentile.pas` |
| Custom-drawn grid (base) | `FastTrak\EPR.QA.GUI.Grid.pas` |
| Custom-drawn grid (study) | `FastTrak\EPR.QA.GUI.Grid.Study.pas` |
| Excel launcher | `FastTrak\Emetra.Adapters.Office.pas` |
| Theme palette | `FastTrak\Emetra.VclUtil.ArenaColors.pas`, `Emetra.VclUtil.ColorSet.pas`, `Emetra.VclUtil.ColorSet.Interfaces.pas` |
| Colour blending / HSL | `FastTrak\Emetra.VclUtil.ColorCalculator.pas` |
| Host form (wiring, captions, export actions) | `MainQuickStat.pas`, `MainQuickStat.dfm` |
| Collector + colour registrations | `QuickStat.Collectors.pas` |

---

## 1. The matrix data model

### 1.1 Shape

`TPersonGridData` (`EPR.QA.Matrix.pas:35`) is the matrix. It is **not** a standalone model: it
delegates all cell storage to the grid control through `IPersonGridComponent`
(`EPR.QA.Matrix.Interfaces.pas:29`). The grid stores `TObject` references in a sparse
`Dictionary<"col:row", TObject>` (`Emetra.Classes.SparseArray.pas:11`, key built with
`Format('%d:%d',[col,row])` at line 50/56 — a string key per cell; see Risks).

State it *does* own (`EPR.QA.Matrix.pas:36-48`):

| Field | Type | Meaning |
| --- | --- | --- |
| `fPopulation` | `TObjectDictionary<integer, TPersonGridRow>` (owns values) | rows, keyed by `PersonId` |
| `fColumnNames` | `TPersonGridColumnList` (`TObjectList<TPersonGridColumn>`) | ordered columns |
| `fCaptions` | `TVarCaptions` | VarName → (Title, Description) |
| `fLocked` | boolean | matrix has been materialised into the grid |
| `fSortBy` | `TPersonGridSortOrder` | `sbPersonId` \| `sbReverseName` |
| `fStudyId` | integer | from `PrepareStudy` |

A row (`TPersonGridRow`, `EPR.QA.Matrix.Row.pas:23`) holds `PersonId`, `DOB`, `FullName`
(`LastName + ', ' + FirstName`, line 94), `NationalId`, `GenderId`, and
`FRowData: TObjectDictionary<string, TDataPoint>` — the person's datapoints keyed by VarName
(line 26, owns values). Note `FRowData` is a case-**sensitive** dictionary (default comparer).

A column (`TPersonGridColumn`, `EPR.QA.Matrix.Column.pas:16`) holds only `VarName`, `Title`,
`Subtitle`. **`fSubtitle` is never assigned anywhere** — the constructor takes only
`(AVarName, ATitle)` (line 69-73). Subtitles are therefore always empty; see §6.1.

### 1.2 Lifecycle

```
PreparePopulation(IPersonList)        EPR.QA.Matrix.pas:409
  → Clear                             :198   (grid.Clear + ClearPopulation + ClearVariables)
  → for each IPersonReadOnly: fPopulation.Add(PersonId, TPersonGridRow.Create(person))   :423-424
       (duplicate PersonIds silently dropped)
  → finally PreparePatientMap                                                            :428

PreparePatientMap                     :358
  → grid.DataRows := fPopulation.Count        (grid RowCount := FixedRows + max(n,1))     :365 / GUI.Grid.pas:349
  → copy rows into TGridRowList, sort (SortByName | SortByPersonId)                       :371-374
  → for each row, for col in 0..FixedCols-1: grid.SetObject(col, rowIndex, gridRow)       :375-384

AddData(IGridDataCollector)           :143
  → for personId in fPopulation.Keys:
        collector.AddToBatch(row);  if collector.BatchIsFull then collector.RunBatch(fStudyId)
        progress.Percent := 100 * personIndex / fPopulation.Count                          :152-163
  → if collector.BatchSize > 0 then collector.RunBatch(fStudyId)   (flush tail)            :164-165
  → for n in 0..collector.VarNames.Count-1:
        fColumnNames.Add(TPersonGridColumn.Create(varName, fCaptions.GetVarTitle(varName))) :170-175
  → PrepareVariableMap                                                                     :176

PrepareVariableMap                    :390
  → grid.DataCols := fColumnNames.Count
  → for col in FixedCols..ColCount-1: grid.SetObject(col, 0, column); grid.SetObject(col, 1, column)  :402-403

Lock                                  :325   (called from TStudyOverviewGrid.Lock, GUI.Grid.Study.pas:247)
  → CheckDimensions (logs a SilentWarning on mismatch)                                     :186-196
  → fLocked := true
  → for row in FixedRows..RowCount-1, for col in FixedCols..ColCount-1:
        if TryGetDatapoint(col,row,dp) then grid.SetObject(col,row,dp)
        else grid.SetObject(col,row,fColumnNames[col-FixedCols])   ← "no value" placeholder :343-346
```

`TryGetDatapoint` (`:278`) resolves `col → fColumnNames[grid.GridToDataCol(col)].VarName`, then
`row → TPersonGridRow` via the object stored in column 0, then `row.GetDatapoint(varName, out dp)`.
This is an O(1) dictionary lookup per cell but it goes through the grid's string-keyed sparse array.

Accessors:

| Member | Line | Behaviour |
| --- | --- | --- |
| `ClearVariables` | `:205` | `grid.DataCols := 0` (→ `ColCount := FixedCols + max(0,1)`), `fColumnNames.Clear` |
| `ClearPopulation` | `:211` | `fPopulation.Clear; fLocked := false` |
| `Clear` | `:198` | grid.Clear + both of the above |
| `HasData` | `:320` | `fColumnNames.Count > 0` — i.e. *columns*, not rows |
| `DataRows` | `:522` | `fPopulation.Count` |
| `FieldCount` | `:217` | `fColumnNames.Count` |
| `FixedRows` / `FixedCols` | `:547` / `:552` | delegates to the grid → **1** and **4** |
| `SortBy` (setter) | `:512` | raises `EAssertionFailed('Can not change sort order after locking')` if `fLocked` |
| `FieldType` | `:542` | always `ftFloat` (values are always `double`) |
| `PrepareStudy(name)` | `:432` | `SELECT StudyId FROM dbo.Study WHERE StudName=:StudName` (`EPR.QA.SQL.pas:35`) |
| `SaveToSelection` | `:497` | `EXEC Report.AddSelection` then `EXEC Report.AddSelectionMember` per person (`EPR.QA.SQL.pas:40-41`) |

Host wiring (`MainQuickStat.pas`):
`LoadPopulationIntoGrid` → `ClearPopulation`, `SortBy := sbPersonId`, `PreparePopulation` (`:564-566`).
`actCollectDataExecute` → `ClearVariables`, `AddCaptions`, loop over checked collectors calling
`fGrid.Data.AddData(collector)`, then `fGrid.Lock` (`:648-676`).
Grid info label: `'Population: %d "%s". Grid size: %d x %d'` with
`[ProcId, Title, DataRows, FieldCount]` (`:246`, `:617`) — this is the `17 x 20` in the screenshots
(17 persons × 20 variables).

### 1.3 Column identity and titles

**Identity is the string `VarName`.** It is produced by the collector as
`FVarPrefix + dataset.Fields[1].AsString` (`EPR.QA.Collector.Base.pas:157`) and collected into
`TDataCollector.FVarList`, a `TStringList` with `Sorted := true; Duplicates := dupIgnore`
(`EPR.QA.Collector.Base.pas:82-83`). Consequences:

* Within one collector, columns are **alphabetically sorted** and de-duplicated.
* `FVarList` is **never cleared**. Re-running a collector against a different population keeps
  variables discovered in an earlier run, producing columns that are empty for everyone.
* `TPersonGridData.AddData` appends *all* the collector's varnames with **no cross-collector
  de-duplication**; `TPersonGridColumnList.ContainsVariable` / `TryGetColumn`
  (`EPR.QA.Matrix.Column.pas:83`/`:90`) exist but are **never called**. Two collectors that emit
  the same variable produce two identical columns.
* Comparisons are case-sensitive in the row dictionary but `TryGetColumn` uses `SameText`
  (case-insensitive) — inconsistent.

**Titles** come from `TVarCaptions.GetVarTitle(varName)` at column-creation time
(`EPR.QA.Matrix.pas:173`). `GetVarTitle` falls back to **the VarName itself** when there is no
caption (`EPR.QA.CaptionDictionary.pas:176-184`). That is why the screenshots show raw names such
as `NDV_INS…`, `INS_ALL…`, `NDV_TR…` next to friendly lab names such as `B-Hemo…` and `P-B12`.

`ITitleDictionary` (`EPR.QA.Matrix.Interfaces.pas:11`) is the abstraction:
`GetVarTitle`, `GetVarSubtitle` (**always returns `''`**, `CaptionDictionary.pas:171-174`),
`GetVarDescription`.

### 1.4 `AddCaption` (hard-coded) + `LoadCaptions(true, false)`

`MainQuickStat.AddCaptions` (`MainQuickStat.pas:453-469`) is called at the *start of every*
"Collect data" run (`:649`). It hard-codes 12 caption records and then calls
`fGrid.Data.LoadCaptions(true, false)`.

| VarName | Title | VarDescription | Line |
| --- | --- | --- | --- |
| `DRUID.RED` | `DDI-R` | Drug-Drug interactions, red level | 456 |
| `DRUID.YELLOW` | `DDI-Y` | Drug-Drug interactions, yellow level | 457 |
| `DRUID.ORANGE` | `DDI-O` | Drug-Drug interactions, orange level | 458 |
| `DRUID.GREEN` | `DDI-G` | Drug-Drug interactions, green level | 459 |
| `DRUG.F` | `Regular` | — | 460 |
| `DRUG.B` | `AsNeeded` | — | 461 |
| `DRUG.U` | `Weekly` | — | 462 |
| `DRUG.X` | `Unspec` | — | 463 |
| `DRUG.K` | `Cure` | — | 464 |
| `DRUG.NOATC` | `NoAtc` | — | 465 |
| `DRUG.RESISTANCE_DRIVING` | `Resist` | Resistance-driving antibiotics | 466 |
| `DRUG.METFORMIN` | `Metform` | Metformin | 467 |

`AddCaption` asserts `VarName <> ''` and `Title <> ''` and uses `AddOrSetValue`
(`EPR.QA.CaptionDictionary.pas:149-154`), so hard-coded captions **overwrite** anything present.

`LoadCaptions(AIncludeLab := true, AIncludeCustom := false)` (`EPR.QA.Matrix.pas:179-184`) sets
`fCaptions.LoadLabCaptions := true`, `fCaptions.LoadCustomCaptions := false` and calls
`TVarCaptions.AfterLogin(fDb)`. `fLoadItemCaptions` stays `false` (default,
`CaptionDictionary.pas:66`). Therefore **only one query runs**, `QueryLabCaptions`
(`EPR.QA.SQL.pas:150-154`):

```sql
SELECT ISNULL(NLK, Report.LabClassName(LabClassId)) AS VarName,
       FriendlyName                                 AS Caption,
       NULL                                         AS VarDescription
FROM dbo.LabClass
ORDER BY LabClassId
```

Rows are inserted with `if not fTitles.ContainsKey(...)` (`CaptionDictionary.pas:110-111`), so the
hard-coded captions win. The two disabled queries, for reference:

```sql
-- QueryCustomCaptions  (EPR.QA.SQL.pas:164-167) — DISABLED in QuickStat
SELECT VarSpec AS VarName, Caption, VarDescription FROM Report.ColumnCaption

-- QueryItemCaptions    (EPR.QA.SQL.pas:156-162) — DISABLED (fLoadItemCaptions = false)
SELECT mi.VarName, ISNULL(mfi.ItemHeader, mfi.ItemText) AS Caption, mfi.ItemHelp AS VarDescription
FROM dbo.MetaFormItem mfi
JOIN dbo.MetaItem mi ON mi.ItemId = mfi.ItemId
ORDER BY mfi.FormId
```
(item captions additionally strip parenthesised text with `TRegEx.Replace(Title,'\(.*\)','')`,
`CaptionDictionary.pas:125`).

**Net effect:** the only DB-sourced captions in QuickStat are lab-class friendly names; everything
else falls back to the raw variable name. `Report.ColumnCaption` is dead weight in this app.

> **Bug to carry over as a fix**: `DRUG.F`/`DRUG.B`/… no longer match anything. `TDrugCollector`
> emits `VarName = 'ATC_' + ConvertAtcPatternToVariableName(pattern) + '.' + TreatType` because
> `GroupResults` is set to `false` in `MainQuickStat.pas:258` and the split template
> `CONCAT(%s,'.',ot.TreatType)` is used (`EPR.QA.Collector.Drug.pas:206-215`,
> `VAR_PREFIX_DRUG = 'ATC_'` at `EPR.QA.Collector.Names.pas:206`). So the real varnames are
> `ATC_A10.F`, `ATC_C0x23789.B`, … — never `DRUG.F`.

---

## 2. Datapoints

### 2.1 Hierarchy

```
TInterfacedPersistent
└── TDataPoint  (ICellText)                                  EPR.QA.DataPoint.pas:12
    ├── TColoredDataPoint (IBrushColor, ICustomColor)         :42   ← never instantiated (§3.5)
    ├── TCholDatapoint            (IBrushColor)     Biochemistry:13
    ├── TLdlDatapoint             (IBrushColor)     Biochemistry:19
    ├── THbA1cPercentDatapoint    (IBrushColor)     Biochemistry:26
    ├── THbA1cMmolDatapoint       (IBrushColor)     Biochemistry:31
    ├── THbA1cHistoryDatapoint    (IFontColor)      Biochemistry:36   ← never registered
    ├── TSodiumDatapoint          (IBrushColor)     Biochemistry:43
    ├── TPotassiumDatapoint       (IBrushColor)     Biochemistry:50
    ├── THemoGlobinDatapoint      (IBrushColor)     Biochemistry:55
    ├── TDigitoxinDatapoint       (IBrushColor)     Pharmacology:12
    ├── TDrugDatapoint            (CellText only)   Pharmacology:17
    │   ├── TDrugGreenDatapoint   (IBrushColor)     Pharmacology:22   ← never registered
    │   └── TDrugRedDatapoint     (IBrushColor)     Pharmacology:27   ← never registered
    ├── TBMIDatapoint             (IBrushColor)     VitalSigns:12
    ├── TSBPDatapoint             (IBrushColor)     VitalSigns:20
    ├── TDBPDatapoint             (IBrushColor)     VitalSigns:27
    ├── TPulseQualityDatapoint    (IBrushColor)     HeartFailure:12
    ├── THeartRhythmDatapoint     (IBrushColor)     HeartFailure:18   ← never registered
    ├── TGeneralDirectionDatapoint(IBrushColor)     HeartFailure:24   ← never registered
    ├── TDbVersionDatapoint       (IBrushColor)     Dogfood:12
    └── TDbServerVersionDatapoint (IBrushColor)     Dogfood:17
```

`TDataPoint` state (`EPR.QA.DataPoint.pas:13-22`): `FVarName`, `FValue: double`,
`FTimeStamp: TDateTime`, `FRowId`, `FItemId`, `FUpdateCount`, `fCaption`.

Members:

* `Update(value, timestamp, rowId)` — assigns and increments `FUpdateCount` (`:104-110`).
* `CellText: string; dynamic` (`:86-92`) — **display** text.
  Base: `if fCaption <> '' then Copy(fCaption,1,6) else Format('%g',[FValue])`.
  Note the caption is truncated to **6** characters.
* `CellHint: string; virtual` (`:81-84`) — base returns `''`; **never overridden**, so the data-cell
  tooltip is always empty (`GUI.Grid.Study.pas:97-98`).
* `AlignLeft: boolean` (`:67-70`) — `fCaption <> ''`. Cells with a caption are drawn left-aligned.
* `AsString: string` (`:72-79`) — the *hint panel* text (`MainQuickStat.pas:595`):

```
%s = %g\nTimeStamp = %s\nRowId = %d\nUpdates = %d
  [ + "\nItemId = %d"    if FItemId > 0 ]
  [ + "\nCaption =\"%s\"" if fCaption <> '' ]
```
`%g` → shortest general form, 15 significant digits, **locale decimal separator**;
`DateToStr` → locale short date (`dd.mm.yyyy` on nb-NO).

`TColoredDataPoint` adds a settable `FColor` initialised to `clNone` in `AfterConstruction`,
`BrushColor` returning it and `ICustomColor.SetColor` (`:42-51`, `:114-128`).

### 2.2 Registrations — `QuickStat.Collectors.RegisterCustomDatapoints`

`QuickStat.Collectors.pas:154-176` — 16 registrations. Keys are matched **case-sensitively**
(`TDictionary<string,TClass>` default comparer, `EPR.QA.PointFactory.pas:36`) — note
`DB_VERSION` vs `DbVersion` on the last two rows.

| # | Line | VarName key | Class | Behaviour (thresholds ⇒ colour, see palette §7) |
| --- | --- | --- | --- | --- |
| 1 | 158 | `NPU01566` | `TCholDatapoint` | total cholesterol. `>8` grave, `>7` high, `>6` moderate, `>5` mild, `>4.5` low, `>0` noRisk(white), else noData (`Biochemistry:87-103`) |
| 2 | 159 | `NPU01568` | `TLdlDatapoint` | LDL. `>5` grave, `>4` high, `>3` moderate, `>2` mild, `>1.8` low, `>0` noRisk, else noData (`:67-83`) |
| 3 | 160 | `NPU03835` | `THbA1cPercentDatapoint` | HbA1c %. `>10` grave, `>9` high, `>8` moderate, `>7` mild, `>6.5` low, `>0` noRisk, else noData (`:107-123`) |
| 4 | 161 | `NPU27300` | `THbA1cMmolDatapoint` | HbA1c mmol/mol. `>86` grave, `>75` high, `>65` moderate, `>58` mild, `>53` low, `>0` **`clWebAliceBlue`**, else noData (`:125-141`) |
| 5 | 162 | `NPU04786` | `TDigitoxinDatapoint` | two-sided. `<5\|>20` grave, `<6\|>17` **`clDataPalePurple`**, `<7\|>16` high, `<8\|>15` moderate, `<9\|>14` mild, `>0` noRisk, else noData (`Pharmacology:42-58`) |
| 6 | 163 | `NPU03429` | `TSodiumDatapoint` | two-sided. `<132\|>150` grave, `<134\|>148` high, `<136\|>146` moderate, `<137\|>145` mild, else **white** (no `clNoData` branch — 0 renders as grave) (`Biochemistry:159-171`) |
| 7 | 164 | `NPU03230` | `TPotassiumDatapoint` | two-sided. `<3\|>5.5` grave, `<3.2\|>5.3` high, `<3.3\|>5.2` moderate, `<3.4\|>5.1` mild, else white (same 0-value trap) (`:175-187`) |
| 8 | 165 | `NOR05172` | `THemoGlobinDatapoint` | two-sided. `<9\|>20` grave, `<10\|>19` high, `<11\|>18.5` moderate, `<12\|>18` mild, else white (same 0-value trap) (`:191-204`) |
| 9 | 167 | `SBP_UNSPEC` | `TSBPDatapoint` | systolic BP. `>180` grave, `>160` high, `>150` moderate, `>140` **or** `<100` mild, `>0` noRisk, else noData (`VitalSigns:63-77`) |
| 10 | 168 | `DBP_UNSPEC` | `TDBPDatapoint` | diastolic BP. `>100` grave, `>95` high, `>90` moderate, `>85` mild, `>0` noRisk, else noData (`:81-95`) |
| 11 | 169 | `SYSBP` | `TSBPDatapoint` | as #9 |
| 12 | 170 | `DIABP` | `TDBPDatapoint` | as #10 |
| 13 | 171 | `BMI` | `TBMIDatapoint` | **overrides `CellText` → `Format('%.1f',[Value])`** (`VitalSigns:40-43`). Brush: `<=0` noData, `>40\|<15` grave, `>35\|<16` high, `>30\|<17` moderate, `>27\|<18.5` mild, else noRisk (`:45-59`) |
| 14 | 172 | `PULSE_QUALITY` | `TPulseQualityDatapoint` | **enum**. `CellText`: 1→`Rgm`, 2→`AF`, 3→`ES`, else `?`; *assigns `Caption := Result` as a side effect* → `AlignLeft` becomes true (`HeartFailure:50-62`). Brush: 1 noRisk, 2\|3 mild, else noData (`:38-48`) |
| 15 | 173 | `DB_VERSION` (`VAR_DB_VERSION`, `Dogfood:24`) | `TDbVersionDatapoint` | `>=19016` low, `>=19000` mild, `>=18000` moderate, `>0` grave, else noData (`Dogfood:29-41`) |
| 16 | 174 | `DbVersion` (`VAR_SERVER_VERSION`, `Dogfood:25`) | `TDbServerVersionDatapoint` | **overrides `CellText`**: 7→`2016`, 6→`2014`, 5→`2012`, 4→`2008R2`, `>0`→`Gammel`, else `?` (`:45-59`). Brush: `>=7` low, `>4` mild, `>0` grave, else noData (`:61-71`) |

A further **22** registrations happen in `AddCollectorsDrug` (`QuickStat.Collectors.pas:315-342`),
all mapping to `TDrugDatapoint`, and only when the study name matches `GBD|LANGTID|KORTTID`
(`:425`, `:453`). Keys are `'ATC_' + <raw ATC pattern>` (e.g. `ATC_A10%`, `ATC_C0[23789]%`) and
`'ATCF_' + …`. **None of them can ever match** — actual varnames are
`ATC_A10.F`, `ATC_C0x23789.B`, … (see the note at the end of §1.4). So `TDrugDatapoint.CellText`
(`Ja`/`Nei`, or `Copy(Caption,1,8)`, `Pharmacology:62-70`) never fires either. Port this as a fix,
not as a faithful reproduction.

Never registered anywhere: `THbA1cHistoryDatapoint`, `TDrugGreenDatapoint`, `TDrugRedDatapoint`,
`THeartRhythmDatapoint`, `TGeneralDirectionDatapoint`, `TColoredDataPoint`.

### 2.3 `TDataPointFactory` dispatch

`EPR.QA.PointFactory.pas`:

```pascal
constructor Create(const ADefaultDataPointClass: TClass);           // QuickStat passes TDataPoint (Collectors.pas:103)
procedure RegisterDataPointClass(const AVarName: string; AClass: TClass);  // AddOrSetValue :58
function  CreateDataPoint(const AVarName: string): TObject;                // :46-54
```

`CreateDataPoint` looks the varname up in `fClassDictionary`; on a miss it uses
`fDefaultDatapointClass`. Then it calls `datapointClass.Create` — because `datapointClass` is a bare
`TClass`, this resolves to the **non-virtual `TObject.Create`**: the correct runtime type is
allocated and zero-filled but `TDataPoint.Create(varName, value, ts, rowId)` is *not* executed.
Initialisation happens afterwards in the caller:

```pascal
// EPR.QA.Collector.Base.pas:93-98
Result := FFactory.CreateDatapoint(AVarName) as TDataPoint;
Result.VarName := AVarName;
Result.Update(AValue, ATimestamp, ARowId);
// :160-163  optional extras from the dataset
if Assigned(fldItemId)  then newDatapoint.ItemId  := fldItemId.AsInteger;
if Assigned(fldCaption) then newDatapoint.Caption := fldCaption.AsString;
```

No collector overrides the `dynamic CreateDatapoint` (verified across all
`EPR.QA.Collector.*.pas`). Duplicate handling is in the row:
`TPersonGridRow.AddDatapoint` updates the existing point and returns `false`, and the collector
frees the loser (`Matrix.Row.pas:135-152`, `Collector.Base.pas:164-165`).
`TPersonGridRow.AddData` (the non-factory path, `Matrix.Row.pas:105-133`) instead keeps the
**newest by timestamp** and counts Added/Updated/Skipped/Failed in `TPointCounter`.

**Port shape:** a `DataPointRegistry` mapping `VarName → DataPointRule`, where a rule is data
(format function + brush function + font function), not a subclass. Value semantics, trivially
unit-testable, no reflection, no `Activator.CreateInstance`.

---

## 3. Colouring

Three independent colouring mechanisms exist. Only the first is actually alive.

### 3.1 Hard-coded threshold colours (alive)

The subclasses in §2.2 implement `IBrushColor.BrushColor: TColor` returning one of the constants in
`EPR.QA.DataPoint.Colors.pas:8-16`. `THbA1cHistoryDatapoint` implements `IFontColor` instead.
Values in §7.

### 3.2 Empty-cell colours (alive but constant)

`TStudyOverviewGrid.SelectEmptyColor` (`GUI.Grid.Study.pas:265-270`) looks the varname up in
`fEmptyColorMap` and falls back to `clWebWhiteSmoke` (**#F5F5F5**). `AddEmptyColor` (`:139`) is
**never called** in QuickStat, so every "person has no value for this variable" cell is #F5F5F5.
`IEmptyColor` (`Matrix.Interfaces.pas:88`) is declared and never implemented.

Cells with no object at all get `clWebSnow` (**#FFFAFA**, `GUI.Grid.Study.pas:170`).

### 3.3 Percentile colouring — `TColorDictionary` / `TColoring` / `TColorStrategy`

`QuickStat.Percentile.pas` (repo root). **This is the unit that is used** —
`QuickStat.Collectors.pas:6` has `uses QuickStat.Percentile`.

```pascal
TColorStrategy = ( csLowIsBadHighIsGood, csHighIsBadLowIsGood, csHighAndLowIsBad,
                   csHighIsBadOnly, csLowIsBadOnly );          // :15
```

Strategy → endpoint colours (`:90-116`):

| Strategy | `fLowColor` | `fHighColor` |
| --- | --- | --- |
| `csLowIsBadHighIsGood` | `clRed` #FF0000 | `clLime` #00FF00 |
| `csHighIsBadLowIsGood` | `clLime` #00FF00 | `clRed` #FF0000 |
| `csHighAndLowIsBad` | `clRed` | `clRed` |
| `csHighIsBadOnly` | `clWhite` #FFFFFF | `clRed` |
| `csLowIsBadOnly` | `clRed` | `clWhite` |

`fLowBracket = fHighBracket = 10` (percentile points, `:86-87`).

Value → colour (`TPercentileColoring.GetColor`, `:125-157`):

```pascal
if fRanker.TryGetValue(ANumResult, fPercentileRank) then      // exact-match lookup!
begin
  if      rank > (100 - fHighBracket) then Result := Blend(clWhite,  fHighColor,  round((rank - 10) * 10))
  else if rank > 90                   then Result := Blend(clYellow, clWebOrange, round((rank - 90) * 10))
  else if rank > 80                   then Result := Blend(clWhite,  clYellow,    round((rank - 80) * 10))
  else if rank < fLowBracket          then Result := Blend(fLowColor, clWhite,    round(rank * 10))
  else                                     Result := clWhite;
  Result := Blend(Result, clWhite, 50);                        // final 50 % wash-out
end
else Result := clFuchsia;                                      // #FF00FF "no rank" marker
```

`Blend` is `TColorCalculator.BlendColors(A, B, pct) = A + (B - A) * pct / 100` per channel, with a
guard: if the high byte of `A` is neither `$00` nor `$02` (i.e. a system colour such as `clNone`),
it returns `$00808080` (#808080) (`Emetra.VclUtil.ColorCalculator.pas:203-231`).

Note the first two branches are unreachable in that order for `rank > 90`: the `rank > 90` branch
is dead because `100 - fHighBracket = 90` is checked first.

**Where the thresholds come from** — `TColorDictionary.AfterLogin` (`QuickStat.Percentile.pas:224-254`),
called from `TQuickStatCollectors.PrepareStudy` (`QuickStat.Collectors.pas:130`):

```sql
-- QRY_LAB_VARNAME (QuickStat.Percentile.pas:78) — VarName → LabClassId map
EXEC Report.GetLabClassVarNames
--   Fields[0] = LabClassId (int), Fields[1] = VarName (string)   :238
```

then for each registered `TColoring`, `item.Load` (`:244-250`) runs:

```sql
-- QRY_LAB_PERCENTILES (QuickStat.Percentile.pas:79)
EXEC Report.GetPercentileRanksByClassId :LabClassId
-- columns: NumResult (currency), PercentileRank (currency)       :80-81, :165-176
```

The rows go into `TPercentileRanker` = `TDictionary<Currency, Currency>`
(`EPR.Lab.Percentile.pas:13`) — an *exact-value* map, `NumResult → PercentileRank`, not an
interpolating lookup.

Registration: `TColorDictionary.Add(labClassId, strategy)` creates a `TPercentileColoring`
(`:219-222`). `TryGetValue(varName)` maps varname → labClassId via `fNames` and then to the
`TColoring` (`:256-265`).

35 registrations in `RegisterLabColors` (`QuickStat.Collectors.pas:188-226`).
`RegisterLabPercentileColoring(TLabTest)` uses **`ord(ALabTest)` as the LabClassId** (`:180`).
Resolved ordinals from `VMR.Lab.Interfaces.pas:22-…` (176 members; the port must hard-code these
integers, not re-derive them from a C# enum):

| LabClassId | Enum | Strategy |
| --- | --- | --- |
| 124 | `ltALAT` | `csHighIsBadOnly` |
| 123 | `ltAlcalicPhosphatase` | `csHighIsBadOnly` |
| 125 | `ltASAT` | `csHighIsBadOnly` |
| 27 | `ltEVF` | `csHighAndLowIsBad` |
| 28 | `ltTPC` | `csHighAndLowIsBad` |
| 22 | `ltHGB` | `csHighAndLowIsBad` |
| 31 | `ltMCH` | `csHighAndLowIsBad` |
| 30 | `ltMCHC` | `csHighAndLowIsBad` |
| 29 | `ltMCV` | `csHighAndLowIsBad` |
| 126 | `ltCK` | `csHighIsBadOnly` |
| 49 | `ltCreatinine` | `csHighIsBadOnly` |
| 26 | `ltCRP` | `csHighIsBadOnly` |
| 20 | `ltINR` | `csHighIsBadOnly` |
| 25 | `ltESR` | `csHighIsBadOnly` |
| 50 | `ltEstGfr` | `csLowIsBadOnly` |
| 51 | *(literal)* eGFR Cockcroft-Gault | `csLowIsBadOnly` |
| 52 | *(literal)* eGFR MDRD | `csLowIsBadOnly` |
| 995 | *(literal)* eGFR Cystatin C | `csLowIsBadOnly` |
| 1075 | *(literal)* eGFR CKD-EPI | `csLowIsBadOnly` |
| 128 | `ltGammaGT` | `csHighIsBadOnly` |
| 140 | `ltProBNP_pMol` | `csHighIsBadLowIsGood` |
| 55 | `ltAlbumine` | `csHighAndLowIsBad` |
| 92 | `ltCalcium` | `csHighAndLowIsBad` |
| 93 | `ltChloride` | `csHighAndLowIsBad` |
| 79 | `ltIron` | `csHighAndLowIsBad` |
| 78 | `ltFerritine` | `csHighAndLowIsBad` |
| 63 | `ltPFolate` | `csLowIsBadOnly` |
| 84 | `ltFT4` | `csHighAndLowIsBad` |
| 43 | `ltPlasmaGlucose` | `csHighIsBadOnly` |
| 44 | `ltBloodGlucose` | `csHighIsBadOnly` |
| 91 | `ltKalium` | `csHighAndLowIsBad` |
| 90 | `ltNatrium` | `csHighAndLowIsBad` |
| 83 | `ltTSH` | `csHighAndLowIsBad` |
| 54 | `ltUrea` | `csHighIsBadLowIsGood` |
| 53 | `ltUrate` | `csHighIsBadLowIsGood` |

(`ltCockcroftGault = 51` and `ltMDRD = 52` confirm the `ord()` correspondence.)

### 3.4 `OnGetColor` / `ProvideColor` / `ICustomColor` wiring

```
MainQuickStat.pas:277   fGrid.OnGetColor := fQuickStat.ProvideColor;      // TNotifyEvent(TObject)

GUI.Grid.Study.pas:173-182 (inside HandleCellDraw)
  if Supports(cellObject, IBrushColor, thisColor) then
  begin
    brushColor := thisColor.BrushColor;
    if ((brushColor = clNone) or (brushColor = 0)) and Assigned(FOnGetColor) then
    begin
      FOnGetColor(cellObject);          // → TQuickStatCollectors.ProvideColor
      brushColor := thisColor.BrushColor;   // re-read after the callback mutated it
    end;
  end;

QuickStat.Collectors.pas:148-152
  procedure ProvideColor(AObject: TObject);
  begin
    if AObject.InheritsFrom(TColoredDatapoint) then SetColor(AObject as TColoredDatapoint);
  end;

QuickStat.Collectors.pas:140-146
  procedure SetColor(AColoredDatapoint: TColoredDatapoint);
  begin
    if fLabColorDictionary.TryGetValue(AColoredDatapoint.VarName, thisColoring) then
      AColoredDatapoint.SetColor(thisColoring.GetColor(AColoredDatapoint.Value));  // ICustomColor
  end;
```

So `ICustomColor` is a *lazy memoisation* hook: the grid asks the callback to fill in a colour the
first time a cell is painted, and the datapoint caches it in `FColor`.

### 3.5 …and why the whole percentile path is dead as shipped

1. `TDataPointFactory` is created with the default class `TDataPoint`
   (`QuickStat.Collectors.pas:103`), which **does not implement `IBrushColor`**. So for an ordinary
   lab datapoint, `Supports(cellObject, IBrushColor)` is `false` and `FOnGetColor` is never invoked.
2. No class registered with `RegisterDataPointClass` descends from `TColoredDataPoint`
   (verified by grep: `TColoredDataPoint` appears only in `EPR.QA.DataPoint.pas` and
   `QuickStat.Collectors.pas`). The registered subclasses return a hard-coded, non-zero,
   non-`clNone` colour, so even they never reach the callback.
3. Even if (1)/(2) were fixed, `TPercentileColoring.Create` **never assigns `fLabClassId`**
   (`QuickStat.Percentile.pas:83-117` — the parameter `ALabClassId` is unused). `Load` therefore
   always executes `EXEC Report.GetPercentileRanksByClassId 0`, the ranker stays empty, and
   `GetColor` returns `clFuchsia` for every value.

**Recommendation for the port:** keep the *design* (strategy + percentile ranks + blend), implement
it correctly, but ship it **off by default** behind a feature flag until a product decision is made.
Do not silently turn 35 lab columns magenta or red-green.

### 3.6 `QuickStat.Percentile.pas` vs `EPR.Lab.Percentile.pas` — which is dead?

Neither is wholly dead, but the split is uneven:

* `QuickStat.Percentile.pas` **is used**: `QuickStat.Collectors.pas:6` imports it for
  `TColorDictionary`, `TColoring`, `TColorStrategy`.
* `EPR.Lab.Percentile.pas` **is used only for two things**: the type alias
  `TExactNumber = Currency` (`:11`) and the container `TPercentileRanker`
  (`:13`), instantiated at `QuickStat.Percentile.pas:89`.
* Everything else in `EPR.Lab.Percentile.pas` is dead in QuickStat:
  `LoadByName` (`EXEC Report.GetPercentileRanks :VarName`, `:49`),
  `LoadById` (`EXEC Report.GetPercentileRanksById :LabCodeId`, `:69`),
  `SetId`, `SetName`, `OnChange`, `fVarName`, `fLabCodeId`, `fSQL`.
  `TPercentileColoring.Load` fills the ranker directly with `AddOrSetValue`
  (`QuickStat.Percentile.pas:171`) using a *third* stored procedure,
  `Report.GetPercentileRanksByClassId`.

**Port decision:** collapse both units into one `Quickstat.Domain.Colouring` namespace. Keep only
`Report.GetPercentileRanksByClassId` and `Report.GetLabClassVarNames`. Drop
`GetPercentileRanks` / `GetPercentileRanksById` unless the DBA confirms they are still needed.

---

## 4. Anonymisation — privacy-critical

### 4.1 The three modes

`TPersonIdentification = ( pgiFull, pgiPersonIdOnly, pgiRandomPersonId )` (`EPR.QA.Matrix.pas:26`).

Selected by three radio buttons (`MainQuickStat.dfm:1234-1263`, mapped in
`MainQuickStat.PersonGridIdentification`, `MainQuickStat.pas:905-915`):

| Radio | Caption (DFM) | Default | → mode |
| --- | --- | --- | --- |
| `rbFullIdentification` | `Fully identified patients` | | `pgiFull` |
| `rbKeepPids` | `Identified with PID only` | **Checked** | `pgiPersonIdOnly` |
| `rbRandomisePids` | `Generate new random PIDs ` | | `pgiRandomPersonId` |

If none is checked, `raise EAbort.Create('Unhandled identification strategy.')` (`:914`).

Exactly what each emits in the CSV (`EPR.QA.Matrix.SaveToFile`, `:442-495`):

| Column (index) | `pgiFull` | `pgiPersonIdOnly` | `pgiRandomPersonId` |
| --- | --- | --- | --- |
| `COL_PERSON_ID` = 0 → `PID` | `"<PersonId>"` | `"<PersonId>"` | header `"PID"`; data rows **bare integer, unquoted** pseudonym |
| `COL_PERSON_DOB` = 1 → `Født` | `"<DateToStr(DOB)>"` | *column omitted entirely* | *omitted* |
| `COL_PERSON_NATIONAL_ID` = 2 → `Fødselsnummer` | `"<NationalId>"` | *omitted* | *omitted* |
| `COL_PERSON_NAME` = 3 → `Navn` | `"<Last, First>"` | *omitted* | *omitted* |
| data columns ≥ 4 | identical in all three modes | | |

"Omitted entirely" means **no field and no separator** is written (`:469-470` is an empty `then`
branch) — header and data rows stay aligned. Constants at `Matrix.Interfaces.pas:159-163`, headers
at `:153-157` (`HDR_PID='PID'`, `HDR_BORN='Født'`, `HDR_NATIONAL_ID='Fødselsnummer'`,
`HDR_NAME='Navn'`).

### 4.2 How the random PID is generated

`TMatrixAnonymizer` (`EPR.QA.Matrix.Anoymizer.pas`), constructed per `SaveToFile` call
(`EPR.QA.Matrix.pas:454`) with `(AFileName, fGridComponent.RowCount)`:

```pascal
constructor Create(const AFileName: string; const ARowCount: integer);   // :33-40
  fScaleFactor := 10;
  while fScaleFactor < ARowCount do fScaleFactor := fScaleFactor * 10;

function NewPersonId(const ACellText: string): integer;                  // :56-62
  repeat
    Result := fScaleFactor + Random(9 * fScaleFactor);
  until not fIdentifierMapping.ContainsKey(Result);
  fIdentifierMapping.Add(Result, ACellText);
```

`ARowCount = FixedRows + max(DataRows, 1)` = `1 + N`. So:

| Persons N | RowCount | scale | pseudonym range |
| --- | --- | --- | --- |
| 1–9 | 2–10 | 10 | 10 … 99 |
| 10–99 | 11–100 | 100 | 100 … 999 |
| 100–999 | 101–1000 | 1000 | 1 000 … 9 999 |
| 1 000–9 999 | | 10 000 | 10 000 … 99 999 |

Rejection sampling guarantees uniqueness **within one export**.

**Stability / reproducibility — the important part:**

* `Random` uses the global `System.RandSeed`, which the Delphi RTL initialises to **0**.
  `Randomize` is **never called** — grep over the whole repo (`*.pas`, `*.dpr`) returns no
  occurrence of `Randomize` or `RandSeed`.
* ⇒ **Reproducible across processes.** Launch QuickStat, export a population of 17 → you get the
  same pseudonym sequence every single time, on every machine. Two "anonymised" exports of two
  *different* populations of the same size share the same pseudonym list; joining them
  re-identifies by position.
* ⇒ **Not stable within a session.** A second `SaveToFile` in the same process continues the RNG
  stream (and builds a fresh `TMatrixAnonymizer`), so the *same patient* gets a *different*
  pseudonym. Longitudinal linkage across two exports is impossible.
* ⇒ Worst of both worlds. The port must fix this (§9, R-1).

**The re-identification key is written to disk.** `SaveToFile` on the anonymiser
(`Anoymizer.pas:64-82`) is invoked when `AIdentification = pgiRandomPersonId`
(`EPR.QA.Matrix.pas:489-490`) and writes `ChangeFileExt(csvPath, '.mapping.txt')`:

```
<pseudonym>=<original PersonId>
```
(`TStringList.Values[]`, then `Sort` — a **lexicographic** sort, so `10000=…` precedes `4711=…`;
ANSI encoding, CRLF, no BOM.)

**Leak:** for "Open this dataset in Excel", the CSV is
`%TEMP%\<32-hex-guid>.csv` (`MainQuickStat.pas:620-624`, `GetTempDir` from `%TEMP%`,
`Emetra.Win.User.pas:81`; `GetNewStrippedGuid` = lowercase GUID without braces/dashes-preserved,
`Emetra.StrUtils.pas:67-70`). Only that `.csv` path is added to `fFilesThatMustBeDeleted`.
The `.mapping.txt` sibling is **never registered and never deleted** — a plaintext
pseudonym→PersonId table accumulates in the user's TEMP directory indefinitely.

### 4.3 `Anonymous` — display only

`Anonymous` is declared on the base grid, not on `TStudyOverviewGrid`
(`EPR.QA.GUI.Grid.pas:69`, getter `:207-210`, setter `:321-331`):

```pascal
function  Get_Anonymous: boolean;  begin Result := ColWidths[COL_PERSON_NAME] < 0; end;
procedure Set_Anonymous(const Value: boolean);
begin
  if Value then
  begin
    ColWidths[COL_PERSON_NAME]        := -1;
    ColWidths[COL_PERSON_DOB]         := -1;
    ColWidths[COL_PERSON_NATIONAL_ID] := -1;
  end
  else SetDefaultWidths(Self);      // 128 / 64 / 84 / 44
end;
```

It **only hides three columns**; the state is *inferred from a column width*. It is preserved
across `DataColWidth` changes (`:338-347`).

| | Display (`Anonymous`) | Export (`TPersonIdentification`) |
| --- | --- | --- |
| Driven by | `fGrid.Anonymous := not rbFullIdentification.Checked` (`MainQuickStat.pas:687`), initial value `rbRandomisePids.Checked or rbKeepPids.Checked` (`:281`) | `PersonGridIdentification` read at export time (`:754`, `:765`) |
| Hides Name/DOB/NationalId | yes — column width `-1` | yes for both non-full modes |
| PID | always visible, real PersonId | real or pseudonymised |
| Hint panel | shows `PersonId = %d` instead of `FullName` (`MainQuickStat.pas:589-592`) | n/a |
| Data still in memory | **yes** — `TPersonGridRow` keeps FullName / NationalId / DOB | n/a |
| Reversible by the user | columns are hidden, and `goColSizing` is enabled (`GUI.Grid.pas:122`); `SetColWidth`/`Adjust` are public | no |

The two settings are *independent code paths* that happen to be driven by the same radio group.
Nothing enforces the invariant "grid is anonymised ⇒ export is anonymised".

---

## 5. Export

### 5.1 `SaveToFile(fileName, identification, includeDates)`

`TStudyOverviewGrid.SaveToFile` (`GUI.Grid.Study.pas:260-263`) simply forwards to
`TPersonGridData.SaveToFile` (`EPR.QA.Matrix.pas:442-495`).

Two entry points in the UI (popup menu, see screenshot 4):

| Action | Line | Behaviour |
| --- | --- | --- |
| *Open this dataset in Excel* (`mnuExportToExcel`) | `MainQuickStat.pas:749-756` | writes to `%TEMP%\<guid>.csv`, tracks it for deletion, calls `TExcelAdapter.LoadWithFile` |
| *Save this dataset to CSV file* | `:758-767` | `FileSaveDialog1.Execute` then writes to the chosen path |
| *Package dataset specification for reuse* | `:845-881` | unrelated (saves the collector selection to `Report.QuickStat`) |

Both pass `cbExportDates.Checked` as `AIncludeDates`
(caption `Export timestamp for every data element`, `MainQuickStat.dfm:1207`).

### 5.2 Exact CSV format

```
Field separator .......... ';'  (semicolon), written AFTER every field, INCLUDING the last one
                                → every line ends with ';' then CRLF
Line terminator .......... CRLF (Delphi WriteLn on a TextFile)
Quoting .................. AnsiQuotedStr(s, '"') → always double-quoted; embedded '"' doubled
                                EXCEPTION: the pgiRandomPersonId pseudonym is written by
                                Write(F, <integer>) → bare decimal, NO quotes
Encoding ................. ANSI / system code page (Windows-1252 on nb-NO). NO BOM.
                                (classic AssignFile/Rewrite/Write on a `TextFile`)
Decimal separator ........ locale (',' on nb-NO) — Format('%g', [Value]) uses global FormatSettings
Numeric format ........... '%g' → shortest general form, up to 15 significant digits
                                (3.5 → "3,5" ; 1.0 → "1" ; 1e20 → "1E20")
Header rows .............. exactly 1 (FixedRows = 1)
Date in the DOB column ... DateToStr → locale short date ("14.08.2019" on nb-NO)
Timestamp columns ........ FormatDateTime('yyyy-mm-dd', dp.TimeStamp) — ISO, locale-independent
File path of the key ..... <csv>.mapping.txt, only for pgiRandomPersonId
```

Cell text rules (`GetCellText`, `EPR.QA.Matrix.pas:222-276`, always called with `AExport = true`):

| Cell | Exported text |
| --- | --- |
| not `fLocked` | `(not ready)` for **every** cell |
| row 0, fixed cols | `PID` / `Født` / `Fødselsnummer` / `Navn` |
| row 0, data cols | **`VarName`** — *not* the Title (`:258-259`) |
| fixed cols, data rows | `IntToStr(PersonId)` / `DateToStr(DOB)` / `NationalId` / `FullName` |
| data cell with a datapoint | `Format('%g', [dp.Value])` — **the raw value**, `ICellText.CellText` overrides are ignored (so no `Ja`/`Nei`, no `Rgm`/`AF`, no `2016`, and BMI is *not* `%.1f`) |
| data cell without a datapoint | `''` (the placeholder `TPersonGridColumn` yields `Subtitle`, always empty) |
| no object and row ≠ 0 | `nil` |

`AIncludeDates` (`:475-482`): for **data columns only** (`colNo >= FixedCols`) an extra field is
appended after the value:

* header row → `"<VarName>.DATE"`
* data row with a datapoint → `"yyyy-mm-dd"`
* data row without a datapoint → *nothing* (empty, unquoted), followed by the `;`

#### Worked examples

Columns: 4 fixed + `AGE`, `YOB`. One person: PersonId 8, DOB 1922-03-12, fnr 12032212345,
"Hansen, Ola", AGE 97 @ 2019-08-14, YOB 1922 @ 2019-08-14.

`pgiFull`, `includeDates = false`:
```
"PID";"Født";"Fødselsnummer";"Navn";"AGE";"YOB";
"8";"12.03.1922";"12032212345";"Hansen, Ola";"97";"1922";
```

`pgiPersonIdOnly`, `includeDates = false`:
```
"PID";"AGE";"YOB";
"8";"97";"1922";
```

`pgiRandomPersonId`, `includeDates = false` (17 persons ⇒ scale 100):
```
"PID";"AGE";"YOB";
473;"97";"1922";
```
plus `…​.mapping.txt` containing `473=8`.

`pgiPersonIdOnly`, `includeDates = true`, second variable missing for this person:
```
"PID";"AGE";"AGE.DATE";"YOB";"YOB.DATE";
"8";"97";"2019-08-14";"";;
```

Empty population (`DataRows = 0` ⇒ `RowCount = 2`) produces a phantom row:
`"nil";"nil";"nil";"nil";"";…`.

### 5.3 `TExcelAdapter.LoadWithFile`

`Emetra.Adapters.Office.pas:22-56`. **No COM / Excel interop** — it resolves the Excel executable
from the registry and starts it as a process:

```
HKLM\Software\Classes\Excel.Application\CLSID           → default value = "{CLSID}"
HKLM\Software\Classes\CLSID\{CLSID}\LocalServer32       → default value = full command line
TTokenizer.Prepare(excelPath, ' ');  token[0] = the .exe path (strips /automation etc.)
TMrLauncher.Execute(token[0], AFileName)
```

`TMrLauncher.Execute` (`Emetra.Win.Launcher.pas:46-103`) builds `"<exe>" <file>` and calls
`CreateProcess` with `CREATE_NEW_CONSOLE or NORMAL_PRIORITY_CLASS`, then **blocks the UI thread**
in a `WaitForSingleObject` / `Application.ProcessMessages` / `Sleep(50)` pump until Excel exits,
then `WaitForSingleObject(..., INFINITE)`. It fails hard if Excel is not installed
(`regKey.ReadString` on a missing key raises / returns `''`).

**Temp-file lifetime** — `MainQuickStat.pas`:
* `GetTemporaryCsvFileName` (`:620-624`) → `%TEMP%\<guid>.csv`, appended to
  `fFilesThatMustBeDeleted: TStringList` (`:156`, created `:266`).
* `FormDestroy` (`:326-337`) drains the list, calling `System.SysUtils.DeleteFile` on each, logging
  and swallowing exceptions, then frees the list.
* So temp CSVs survive as long as the app runs (and Excel holds them open) and are removed on
  close — **except** the `.mapping.txt` sibling (§4.2), which is never tracked.

---

## 6. Grid rendering rules

### 6.1 Geometry

`TPersonGrid.Create` (`EPR.QA.GUI.Grid.pas:100-126`):

| Property | Value |
| --- | --- |
| `FixedRows` | **1** |
| `FixedCols` | `COL_PERSON_NAME + 1` = **4** |
| `ColCount` initial | 10 (then `TStudyOverviewGrid.Create` sets 5, `Grid.Study.pas:116`) |
| `DefaultRowHeight` | 17 |
| `RowHeights[0]` | 18 |
| `DefaultColWidth` | 64 |
| `FDefaultIdColWidth` (PID) | 44 |
| `FDefaultDobColWidth` | 64 |
| `FDefaultNationalIdColWidth` | 84 |
| `FDefaultNameColWidth` | 128 |
| `BorderStyle` | `bsNone` |
| `DrawingStyle` | `gdsClassic` |
| `DoubleBuffered` | `true` |
| `Options` | `+ [goColSizing, goFixedRowClick, goFixedColClick] - [goFixedVertLine, goRowSelect]` |
| `FGapX / FGapY` | 3 / 1 (`Grid.Study.pas:117-118`) |

`Set_DataCols(v)` → `ColCount := FixedCols + max(v,1)`; `Set_DataRows(v)` → `RowCount := FixedRows + max(v,1)`
(`:333-352`) — hence the "never zero" comment at `EPR.QA.Matrix.pas:188-191`.

**Column widths.** `DataColWidth` is `DefaultColWidth` (`:227-230`, `:338-347`).
`MainQuickStat` sets it to `COL_WIDTH = 64` (`MainQuickStat.pas:249`, `:279`) and the
`Wide columns` checkbox toggles **120 ↔ 64** (`:626-632`). Setting `DefaultColWidth` in VCL resets
*all* column widths, so the setter re-applies the PID width and re-applies the anonymous state.

`UpdateStyle` (`Grid.Study.pas:279-297`) would recompute all of these from font metrics
(`DefaultColWidth := TextWidth('28.11.65') + 6`, `DefaultRowHeight := TextHeight('Åge') + 2`, …)
— but it is **never called**: `IGuiStyleObserver.UpdateStyle` is only invoked from
`TGuiStyle.RegisterClient` / notify (`Emetra.VclUtil.Style.pas:361`, `:371`) and `MainQuickStat`
never registers the grid. `CurrentCellColor` / `CurrentRowColor` therefore keep their constructor
values (`GUI.Grid.pas:109-110`).

The subtitle row (row index 1 of `PrepareVariableMap`) is overwritten by `Lock` because
`FixedRows = 1` — **there is exactly one header row** and subtitles never render.

### 6.2 Leading columns

The four *fixed* columns are `PID`, `Født`, `Fødselsnummer`, `Navn`
(`Matrix.Interfaces.pas:159-163`). The `PID / AGE / YOB / SEX` sequence visible in the screenshots
is: fixed column 0 (`PID`) with the other three fixed columns **hidden by `Anonymous`**, followed by
the first three *data* columns produced by the demographics collectors
`PATIENT.AGE`, `PATIENT.YOB`, `PATIENT.SEX` (`EPR.QA.Collector.Names.pas:243-255`,
registered in `QuickStat.Collectors.pas:238-240`). They appear first because `cbDataCollector.Sorted
= true` (`MainQuickStat.pas:400`) orders the collector list by Norwegian title:
`^ Alder`, `^ Fødselsår`, `^ Kjønn`.

### 6.3 `HandleCellDraw` — the exact painting algorithm

`GUI.Grid.Study.pas:144-245`. Reproduce this order faithfully in WPF.

```
 1  Canvas.Font := Self.Font;  brushColor := clNone;  cellText := ''
 2  alignment: IsTextColumn(col)          → VCENTER|SINGLELINE|END_ELLIPSIS   (left, ellipsis)
             else                         → VCENTER|SINGLELINE|RIGHT
       IsTextColumn = col in {DOB, NAME, NATIONAL_ID}  (GUI.Grid.pas:293-296)  ← PID is right-aligned
 3  if not TryGetObject(col,row) → brushColor := clWebSnow (#FFFAFA), cellObject := nil
    else if Supports(IBrushColor) → brushColor := obj.BrushColor
         if brushColor in {clNone, 0} and OnGetColor assigned → OnGetColor(obj); re-read BrushColor
 4  if brushColor in {clNone, 0} → brushColor := clWhite
 5  if row = 0 and col < FixedCols → cellText := GetFixedHeader(col)     // PID/Født/Fødselsnummer/Navn
 6  if row < FixedRows:                                   // header
        if Supports(IPersonGridColumn) → row 0: Title, row 1: Subtitle
        if col >= FixedCols → alignment := VCENTER|SINGLELINE|END_ELLIPSIS   // left + ellipsis
    else if col < FixedCols:                              // fixed data column
        if Supports(IPersonGridRow) → cellText := GetFixedFields(col,row)
    else:                                                 // data cell
        if Supports(ICellText)  → cellText := obj.CellText;  if obj.AlignLeft then left+ellipsis
        else if Supports(IVarName) → brushColor := SelectEmptyColor(varName)   // #F5F5F5
 7  colour mixing (first match wins):
        (col = Col) and (row = CurrentRow)      → brushColor := CurrentCellColor        (#FFFBD4)
        gdSelected in State or row = CurrentRow → brushColor := Blend(brushColor, CurrentRowColor, 50)
        gdFixed in State                        → brushColor := FixedColor              (#F4FBFB)
 8  font colour:
        col = 0                                 → FixedFontColor                        (#035F66)
        (col = Col) and (row = CurrentRow)      → Font.Color
        Supports(IFontColor)                    → obj.FontColor
        else                                    → Font.Color
 9  if row = 0 or row = CurrentRow → Font.Style := [fsBold]
10  FillRect(rect);  InflateRect(rect, -3, -1);  DrawText(cellText, rect, alignment)
```

`CurrentRow` is tracked by `HandleSelect` (`GUI.Grid.pas:157-170`) which repaints the old and the
new row cell-by-cell (`RepaintRow`, `:134-141` — "workaround for bug in InvalidateRow").

`StartPainting` (`Grid.Study.pas:272-277`) flips `DefaultDrawing := false`, installs
`OnDrawCell := HandleCellDraw` and invalidates. Before that the grid is blank (screenshot 1).
It is called from `Lock` (`:254`) and from `AfterPopulationSelect` (`MainQuickStat.pas:542`).

### 6.4 Tooltips and the hint panel

Two distinct mechanisms:

* **Grid tooltip** `CMHintShow` (`Grid.Study.pas:69-105`): for header rows shows
  `Data.Description(varName)` (row 0) or `Subtitle` (row 1); for data cells shows
  `ICellText.CellHint` — always `''` today.
* **Hint panel** `UpdateDataHintPanel` (`MainQuickStat.pas:577-613`), wired to `fGrid.OnClick`
  (`:311`) and to the `Show data hint` checkbox (`:310`, DFM `:1492-1493` default **checked**):
  builds `FullName` (or `PersonId = %d` when `Anonymous`) + `sLineBreak` + `TDataPoint.AsString`,
  positions `panHint` just under the current cell (`CellRect`, offset by (3,3), `Top +
  DefaultRowHeight + 1`), sizes it to `8 * |Font.Height| + 2*BorderWidth + 8`, and shows it only if
  a datapoint exists at the current cell. Panel colour is `clInfoBk` (DFM `:1457`).

---

## 7. Colour palette (hex RGB)

Delphi `TColor` literals are `$00BBGGRR` — the byte order is reversed relative to HTML.

### 7.1 Risk palette — `EPR.QA.DataPoint.Colors.pas:8-16`

| Constant | Delphi | Hex RGB | Swatch meaning |
| --- | --- | --- | --- |
| `clNoRisk` (= `clWhite`) | `$00FFFFFF` | `#FFFFFF` | normal |
| `clLowRisk` | `$00B3EFD1` | `#D1EFB3` | pale green |
| `clMildRisk` | `$00BFFFFF` | `#FFFFBF` | pale yellow |
| `clModerateRisk` | `$00BFEDFF` | `#FFEDBF` | pale amber |
| `clHighRisk` | `$00BFDBFF` | `#FFDBBF` | pale orange |
| `clGraveRisk` | `$008080FF` | `#FF8080` | salmon red |
| `clNoData` (= `clWebGainsboro`) | `$00DCDCDC` | `#DCDCDC` | grey |
| `clDataPalePurple` | `$00E7B2EE` | `#EEB2E7` | pale purple (digitoxin only) |

### 7.2 Other cell colours

| Where | Constant | Delphi | Hex RGB |
| --- | --- | --- | --- |
| empty data cell | `clWebWhiteSmoke` | `$00F5F5F5` | `#F5F5F5` |
| cell with no object | `clWebSnow` | `$00FAFAFF` | `#FFFAFA` |
| HbA1c mmol, `0 < v <= 53` | `clWebAliceBlue` | `$00FFF8F0` | `#F0F8FF` |
| `THbA1cHistoryDatapoint` font ≥75 | `clWebRed` | `$000000FF` | `#FF0000` |
| … font ≥58 | `clWebDarkOrange` | `$00008CFF` | `#FF8C00` |
| … font ≥53 | `clGreen` | `$00008000` | `#008000` |
| … font >0 | `clBlue` | `$00FF0000` | `#0000FF` |
| percentile "no rank" | `clFuchsia` | `$00FF00FF` | `#FF00FF` |
| percentile mid band | `clYellow`, `clWebOrange` | `$0000FFFF`, `$0000A5FF` | `#FFFF00`, `#FFA500` |
| percentile endpoints | `clRed`, `clLime` | `$000000FF`, `$0000FF00` | `#FF0000`, `#00FF00` |
| `BlendColors` guard result | — | `$00808080` | `#808080` |

### 7.3 Grid chrome — actually applied values

| Role | Source | Delphi | Hex RGB |
| --- | --- | --- | --- |
| Fixed row/col background (`FixedColor`) | `clMyGreenColor` (`MainQuickStat.pas:375`) | `$00FBFBF4` | `#F4FBFB` |
| PID column font (`FixedFontColor`) | `clMenuBackgroundDarkBrush` (`:376`) | `$00665F03` | `#035F66` |
| Splitter (`splMain.Color`) | `clMyGreenColor` (`:374`) | `$00FBFBF4` | `#F4FBFB` |
| Current cell | `clFocusedSelectionColor` (`GUI.Grid.pas:109`) | `$00D4FBFF` | `#FFFBD4` |
| Current row (blend base, 50 %) | `clUnfocusedSelectionColor` (`:110`) | `$00FCF2E7` | `#E7F2FC` |
| Hint panel | `clInfoBk` (DFM) | system | `#FFFFE1` (default) |
| Tab sheet background | DFM `Color = 15987699` | `$00F3F3F3` | `#F3F3F3` |

`UpdateStyle` *would* set `CurrentCellColor := clWebOrange` (`#FFA500`) and
`CurrentRowColor := Blend(#FFA500, white, 50)` = `#FFD280`, but it is never invoked (§6.1).

### 7.4 Arena theme — `Emetra.VclUtil.ArenaColors.pas:11-42`

| Constant | Delphi | Hex RGB | Used for |
| --- | --- | --- | --- |
| `clArenaListSelectedBackground` | `$00918817` | **`#178891`** | teal header panels, selected list item, tab highlight bar |
| `clMenuItemSelectionFill` / `Stroke` / `clRightArrowFill` | = above | `#178891` | `StyleHeaderPanel` background (`:85`) |
| `clMenuItemSelectionForeground` / `clArenaListSelectedForeground` | `$00FFFFFF` | `#FFFFFF` | header panel font |
| `clSelectedItemBackground` | `$00897F04` | `#047F89` | Arena common selection |
| `clMenuBackgroundDarkBrush` | `$00665F03` | `#035F66` | dark teal text |
| `clListSelectedBackgroundUnfocused` | `$00B6AE50` | `#50AEB6` | unfocused list selection |
| `clMinimizedPatientTileBackground` | `$00908644` | `#448690` | — |
| `clFormFace` | `$00EEEEEE` | `#EEEEEE` | form background (`StyleForm`, `:67`) |
| `clMyGreenColor` | `$00FBFBF4` | **`#F4FBFB`** | splitter, grid fixed cells, tab page background |
| `clMyAlternateColor` | `$00F7F7F7` | `#F7F7F7` | list alternate row |
| `clMyListboxColor` | `$00FFFFFF` | `#FFFFFF` | list background |
| `clArenaLavender` | `$00F1E9E9` | `#E9E9F1` | — |
| `clMaximizedTileBackground` | `$00F8F8F8` | `#F8F8F8` | — |
| `clLightweightDataGridBackground` | `$00F3F3F3` | `#F3F3F3` | — |
| `clLightweightDataGridALternationBackground` | `$00ECECEC` | `#ECECEC` | — |
| `clSeparatorFill` | `$00E0E0E0` | `#E0E0E0` | — |
| `clSeparatorFill2` | `$00FFFFFF` | `#FFFFFF` | — |
| `clNormalContainerBackgroundWhenReadOnly` | `$00FFFFFF` | `#FFFFFF` | — |

Fonts: `Calibri` 10 pt for forms/tabs/lists, 11 pt for header panels, 9 pt for simple checkboxes
(`ArenaColors.pas:58-59`, `:89`, `:122`); `lblProgress` is Calibri 10 (`MainQuickStat.pas:353-364`).

### 7.5 Colour-set defaults — `Emetra.VclUtil.ColorSet.Interfaces.pas:68-80`, `ColorSet.pas:96-102`

| Constant | Delphi | Hex RGB |
| --- | --- | --- |
| `clTextColor` | `$00333333` | `#333333` |
| `clFocusedSelectionColor` | `$00D4FBFF` | `#FFFBD4` |
| `clUnfocusedSelectionColor` | `$00FCF2E7` | `#E7F2FC` |
| `clStatusTextColor` | `$00822EB8` | `#B82E82` |
| `clCodeColor` | `$00A4294B` | `#4B29A4` |
| `clFirstInfoColor` | `$00AC6D2B` | `#2B6DAC` |
| `clSecondInfoColor` | `$007BB02C` | `#2CB07B` |
| `clSelectedFill` | `$00FCEBDC` | `#DCEBFC` |
| `clModernBlue` | `$00FBDF82` | `#82DFFB` |
| `clModernSelectionBorder` | `$0065C3E5` | `#E5C365` |
| `clModernSelectionFill` | `$00BBEFFF` | `#FFEFBB` |
| `clColdGray` | `$00635C59` | `#595C63` |
| `clGreenGray` | `$00586359` | `#596358` |
| `clRedGray` | `$005C5C8C` | `#8C5C5C` |
| `clLavenderGray` | `$0062585E` | `#5E5862` |
| `clBlueGray` | `$007C5C5C` | `#5C5C7C` |
| `clBrownGray` | `$005C6266` | `#66625C` |
| `clLightYellowOrange` | `$0066E0FF` | `#FFE066` |

---

## 8. Proposed C# design

Two libraries. `Quickstat.Domain` has **no** WPF, no ADO.NET, no file I/O.
`Quickstat.Export` has no WPF.

### 8.1 `Quickstat.Domain`

```
Quickstat.Domain/
  Primitives/
    Rgb.cs                       readonly record struct Rgb(byte R, byte G, byte B)
                                 + static Rgb FromDelphi(int bbggrr)  + ToString() => "#RRGGBB"
    ColorBlend.cs                static Rgb Blend(Rgb a, Rgb b, int percent)   // port of BlendColors
  Captions/
    CaptionRecord.cs             readonly record struct (VarName, Title, VarDescription)
    ICaptionSource.cs            Task<IReadOnlyList<CaptionRecord>> LoadAsync(CancellationToken)
    CaptionDictionary.cs         ITitleDictionary; Add(record) overwrites; AddRange(...) first-wins
  DataPoints/
    DataPoint.cs                 sealed class: VarName, Value(double), Timestamp(DateTime),
                                 RowId, ItemId, Caption, UpdateCount; Update(...)
                                 Describe() → the AsString hint text
    DataPointRule.cs             sealed record: Format(Func<double,string>?),
                                 Brush(Func<double,Rgb?>?), Font(Func<double,Rgb?>?),
                                 SetsCaptionFromText(bool)
    DataPointRegistry.cs         IReadOnlyDictionary<string, DataPointRule>, ordinal comparer,
                                 TryGet(varName, out rule); Default rule = plain "%g"
    Rules/ThresholdRule.cs       static DataPointRule Descending((double gt, Rgb c)[] bands, Rgb below)
                                 static DataPointRule TwoSided((double lo,double hi,Rgb c)[] bands, Rgb inside)
                                 static DataPointRule Enum(IReadOnlyDictionary<int,(string,Rgb)>, ...)
    Rules/StandardRules.cs       the 16 registrations of §2.2, one static readonly field each
    NumericFormat.cs             string G(double) — the '%g' equivalent, culture-parameterised
  Colouring/
    RiskPalette.cs               static readonly Rgb NoRisk/Low/Mild/Moderate/High/Grave/NoData/PalePurple
    GridPalette.cs               FixedBackground, FixedForeground, CurrentCell, CurrentRow,
                                 EmptyCell, MissingObject, Teal, FormFace …  (§7)
    ColorStrategy.cs             enum { LowIsBadHighIsGood, HighIsBadLowIsGood, HighAndLowIsBad,
                                        HighIsBadOnly, LowIsBadOnly }
    PercentileRanker.cs          Dictionary<decimal, decimal> wrapper, TryGetRank(value, out rank)
    PercentileColoring.cs        Rgb GetColor(decimal value) — port of §3.3, LabClassId REQUIRED
    LabColorCatalog.cs           varName → LabClassId → PercentileColoring ; Load(IPercentileSource)
    ILabColorSource.cs           GetLabClassVarNamesAsync(), GetPercentileRanksAsync(labClassId)
  Anonymisation/
    PersonIdentification.cs      enum { Full, PersonIdOnly, RandomPersonId }
    IPseudonymGenerator.cs       int Next(int scaleFactor)
    DeterministicPseudonymGenerator.cs   HMAC-based, salt from a per-export secret (see R-1)
    SessionPseudonymMap.cs       maintains pseudonym ↔ personId for the lifetime of an export,
                                 exposes IReadOnlyDictionary for the key file
  Matrix/
    PersonRow.cs                 PersonId, Dob, FullName, NationalId, GenderId, Sex,
                                 IReadOnlyDictionary<string,DataPoint> Points,
                                 bool TryAdd(DataPoint) / AddOrUpdateNewest(DataPoint)
    MatrixColumn.cs              readonly record class (VarName, Title, Description)
    ResultMatrix.cs              THE model. See below.
    MatrixSortOrder.cs           enum { PersonId, ReverseName }
    IMatrixDataCollector.cs      the collector contract (owned by the collectors doc)
```

`ResultMatrix` — the important shape change: it owns its data instead of pushing objects into a
grid control.

```csharp
public sealed class ResultMatrix
{
    public IReadOnlyList<PersonRow>    Rows    { get; }     // sorted, materialised
    public IReadOnlyList<MatrixColumn> Columns { get; }
    public bool     IsLocked { get; }
    public int      RowCount    => Rows.Count;
    public int      FieldCount  => Columns.Count;
    public bool     HasData     => Columns.Count > 0;
    public MatrixSortOrder SortBy { get; set; }             // throws if IsLocked

    public void PreparePopulation(IEnumerable<PersonRow> people);
    public void ClearPopulation();
    public void ClearVariables();
    public void AddColumns(IEnumerable<string> varNames, ITitleDictionary titles);
    public void Lock();                                     // freezes + builds the column index

    public bool TryGetDataPoint(int rowIndex, int columnIndex, out DataPoint dp);
    public CellValue GetCell(int rowIndex, int columnIndex);   // struct: Text, Rgb? Brush, Rgb? Font, Align
}
```

`CellValue` is a `readonly record struct` produced by a pure `MatrixCellRenderer` that implements
§6.3 steps 3–6 (everything except selection/fixed overrides, which belong to the view). That makes
the *entire* cell-appearance logic unit-testable without WPF.

Column lookup: build a `Dictionary<string,int>` (ordinal) on `Lock()`. Enforce **de-duplication**
of varnames at `AddColumns` (behaviour change from Delphi — see R-4).

### 8.2 `Quickstat.Export`

```
Quickstat.Export/
  CsvExportOptions.cs      PersonIdentification Identification; bool IncludeTimestamps;
                           char Separator = ';'; CultureInfo Culture;
                           Encoding Encoding; bool TrailingSeparator = true;
                           string DateFormat = "yyyy-MM-dd"; bool WriteKeyFile;
                           CsvDialect Dialect { Legacy, Rfc4180 }
  CsvMatrixWriter.cs       void Write(ResultMatrix m, TextWriter w, CsvExportOptions o,
                                      SessionPseudonymMap? map)
  PseudonymKeyWriter.cs    void Write(SessionPseudonymMap map, TextWriter w)
  XlsxMatrixWriter.cs      void Write(ResultMatrix m, Stream s, XlsxExportOptions o)   // ClosedXML
  IExternalAppLauncher.cs  Task OpenAsync(string path, CancellationToken)
  ShellLauncher.cs         Process.Start(new ProcessStartInfo(path){ UseShellExecute = true })
  TempFileTracker.cs       IDisposable; Track(path); disposes → best-effort delete of ALL tracked
```

Key decisions:

* **`TextWriter`, not a file path.** Unit tests assert exact strings; the app passes a
  `StreamWriter(path, false, encoding)`.
* **`CsvDialect.Legacy`** reproduces §5.2 byte for byte (`;`, always-quoted, trailing separator,
  CP1252 via `CodePagesEncodingProvider`, culture decimal separator, unquoted pseudonym).
  **`CsvDialect.Rfc4180`** is the new default for fresh work: UTF-8 with BOM, quote-when-needed,
  no trailing separator, invariant `.` decimal separator, ISO dates everywhere. Ship both; make the
  dialect a user setting so downstream R/SPSS/Stata scripts keep working.
* **`TempFileTracker` must track the key file too.** Register the `.csv` *and* the
  `.mapping.txt`/key file.
* **ClosedXML writer**: header row frozen (`SheetView.FreezeRows(1)`), first N columns frozen
  (`FreezeColumns(1)` or 4), values written as **numbers** (`cell.Value = dp.Value`) not strings,
  per-cell `Style.Fill.BackgroundColor = XLColor.FromArgb(rgb.R, rgb.G, rgb.B)` taken from the same
  `MatrixCellRenderer` used by the screen, `Style.Font.FontColor` for `IFontColor`-style rules, and
  an `AutoFilter` on the header. Timestamps go in adjacent `<VarName>.DATE` columns as real dates
  with `Style.DateFormat.Format = "yyyy-mm-dd"`. Guard: Excel's hard limit is 16 384 columns —
  with timestamps enabled that is 8 191 variables; add an explicit check.

### 8.3 Purity / testability matrix

| Component | Pure? | How tested |
| --- | --- | --- |
| `Rgb`, `ColorBlend` | ✔ pure | table-driven: every Delphi constant → expected hex; blend fixtures |
| `NumericFormat.G` | ✔ pure | golden values incl. `3.5`→`3,5` (nb-NO) / `3.5` (invariant), `1.0`→`1`, `1e20` |
| `ThresholdRule` / `StandardRules` | ✔ pure | one theory per registered variable: value → expected `Rgb` and text; boundary values (`<=`/`<` and `>`/`>=` asymmetries in §2.2) |
| `DataPointRegistry` | ✔ pure | ordinal case sensitivity (`DB_VERSION` vs `DbVersion`), fallback to default |
| `CaptionDictionary` | ✔ pure | precedence: hard-coded overwrite vs DB first-wins |
| `PercentileColoring` | ✔ pure | ranker fixture → colour per band and per strategy; missing rank → configured fallback |
| `LabColorCatalog` | ✔ pure given a fake `ILabColorSource` | the 35 registrations resolve to the right strategy |
| `PersonRow` / `ResultMatrix` | ✔ pure | sort orders, de-dup, `Lock` invariants, `SortBy` after lock throws |
| `MatrixCellRenderer` | ✔ pure | the §6.3 decision table, incl. empty cell (#F5F5F5) and missing object (#FFFAFA) |
| `SessionPseudonymMap` + generator | ✔ pure (generator injected) | uniqueness, digit-count per scale factor, **stability within an export**, **unlinkability across salts** |
| `CsvMatrixWriter` | ✔ pure (`TextWriter`) | all three identification modes × timestamps on/off, golden files incl. the §5.2 worked examples; quote-doubling; empty population |
| `XlsxMatrixWriter` | ✔ (`MemoryStream`) | round-trip with ClosedXML: cell values, fills, frozen panes, column count guard |
| `ShellLauncher`, `TempFileTracker` | ✘ I/O | thin; integration-test `TempFileTracker` only |
| `MatrixView` (WPF) | ✘ | manual + a `RenderTargetBitmap` snapshot test if the team wants it |

---

## 9. WPF grid rendering — recommendation

**Build a virtualised custom control. Do not use `DataGrid` with dynamic columns.** This is a
decisive recommendation, not a preference.

### 9.1 Observed and expected sizes

* Screenshots: `Grid size: 17 x 20` — 17 persons × 20 variables (`MainQuickStat.pas:246`, `:617`).
* Populations are `dbo.GetPopulations` result sets — the sample list in screenshot 1 runs to
  hundreds of saved populations; real cohorts are routinely **hundreds to a few thousand persons**.
* Column count is the sum of *all* checked collectors' `VarNames`. The "everything" case is large:
  `QST_LAB_HIGH/MEDIUM/LOW` emit **one column per distinct lab class present in the data** (the
  `dbo.LabClass` catalogue has >170 entries, `VMR.Lab.Interfaces.pas`); `AddCollectorsStudySpecific`
  adds **two collectors per form** in the study (`QuickStat.Collectors.pas:404-406`), and each
  `TFormDataCollector` emits one column per numeric item on that form; the GBD branch adds ~25 more
  collectors and the drug branch ~30, each split by `TreatType` into up to five columns
  (`ATC_x.F/.B/.U/.X/.K`).
  **A realistic worst case is 500–1 500 columns × 1 000 rows ⇒ 0.5–1.5 million cells.**

### 9.2 Why not `DataGrid`

1. **Column creation cost.** `DataGrid.Columns` is an `ObservableCollection`; adding 800 columns
   triggers 800 collection-changed passes, a full measure invalidation each time, and rebuilds the
   header presenter. Even batched behind `Items.Defer*`, this is seconds, not milliseconds.
2. **Column virtualisation is unreliable.** `EnableColumnVirtualization` exists but interacts badly
   with frozen columns (`FrozenColumnCount`) — which we need for PID/DOB/fnr/Navn — and with
   `DataGridTemplateColumn`. Horizontal scrolling with hundreds of columns is visibly janky.
3. **No natural binding path.** A cell is `(personId, varName)`. With a `DataGrid` you either
   generate one `Binding` with an indexer path per column (`[VARNAME]`, requiring an
   `ICustomTypeDescriptor` or a dictionary-backed indexer plus 800 bindings per row) or reflect over
   a runtime-emitted type. Both are heavy; per-row binding count ≈ column count.
4. **Per-cell colouring is the expensive part.** The Delphi code decides brush + font colour + font
   weight + alignment **per cell** (§6.3). In `DataGrid` that means a `CellStyle` with a
   `MultiBinding`+`IMultiValueConverter` evaluated for every realised cell, allocating a
   `SolidColorBrush` unless carefully cached, on top of ~8 visuals per cell
   (`DataGridCell` → `ContentPresenter` → `TextBlock` …). At 60 visible rows × 25 visible columns
   that is ~12 000 visuals for one screen, re-created on every scroll tick with recycling churn.
5. It still cannot reproduce `RepaintRow`-style current-row highlighting cheaply, nor the exact
   `InflateRect(-3,-1)` + `DT_END_ELLIPSIS` metrics of the original.

### 9.3 The recommended control

A single `FrameworkElement` that renders the whole viewport in `OnRender`, hosted in a
`ScrollViewer` via `IScrollInfo`. This mirrors `TCustomDrawGrid` + `HandleCellDraw` one-to-one and
is the lowest-risk, highest-fidelity port.

```
Quickstat.App/Controls/
  MatrixView.cs          : FrameworkElement, IScrollInfo
  MatrixViewLayout.cs    // pure: column x-offsets, row y-offsets, hit testing, visible range
  CellVisualCache.cs     // Freeze()d SolidColorBrush + Pen per Rgb; FormattedText/GlyphRun cache
```

Design points:

* **Fixed row height** (17–18 px at 96 dpi, scaled by `DpiScale`) as in the original
  (`GUI.Grid.pas:114-116`). Row `i` is at `i * rowHeight` — O(1) scroll mapping, no measure pass.
* **Column x-offsets** in a prefix-sum `double[]`, rebuilt only when widths change
  (default 64, wide 120, fixed cols 44/64/84/128). Binary search for the first visible column.
* `OnRender` loops only the visible range: `dc.DrawRectangle(brush, null, cellRect)` then
  `dc.DrawText(formattedText, textOrigin)` with a `PushClip` per cell for the ellipsis behaviour
  (or pre-trim with `FormattedText.MaxTextWidth` + `TextTrimming.CharacterEllipsis`).
  **~1 500 draw calls per frame, zero visual-tree churn.**
* **Frozen panes**: render the 1 header row and the 4 fixed columns in the same `OnRender` after
  pushing the appropriate translate transforms — no separate control, no scroll synchronisation
  bugs.
* **Brush cache**: `Dictionary<Rgb, SolidColorBrush>` with `Freeze()`. The palette is tiny (§7), so
  this is ~20 brushes total. Never allocate a brush in `OnRender`.
* **Text cache**: cell text is derived from `DataPoint.Value` and does not change while the matrix
  is locked; cache `FormattedText` per `(text, bold, alignment, width)` in an LRU sized to a few
  screens. `GlyphRun` is faster still if profiling demands it.
* **Selection / current row**: keep `CurrentRow`/`CurrentColumn` on the control; on change call
  `InvalidateVisual()` — the whole viewport redraw is cheap enough that per-row invalidation is
  unnecessary (this removes the `RepaintRow` workaround entirely).
* **Tooltips**: `ToolTipService` on the control + `OnToolTipOpening` computing the cell from the
  mouse position via `MatrixViewLayout.HitTest`; content is `DataPoint.Describe()` (replaces both
  `CMHintShow` and the floating `panHint`).
* **Accessibility**: implement `AutomationPeer` returning an `ITableProvider`/`IGridProvider`.
  This is the one real cost of the custom control and must be scheduled explicitly.
* **Keyboard**: arrows / PageUp / PageDown / Ctrl+Home / Ctrl+End on the control; `IScrollInfo`
  members do the maths.

Estimated effort: 3–5 days for the control + layout + hit testing, plus 1–2 days for the automation
peer. That is comparable to fighting `DataGrid` into shape, with a far better result.

**Fallback**, if the team rejects a custom control: `DataGrid` with
`EnableRowVirtualization=true`, `EnableColumnVirtualization=true`,
`VirtualizingPanel.VirtualizationMode=Recycling`, `ScrollViewer.CanContentScroll=true`,
`FrozenColumnCount=4`, `RowHeight` set explicitly (never `Auto`), a **single shared** `CellStyle`
whose `Background` comes from one cached-converter `MultiBinding`, and a hard cap (e.g. 200) on the
number of columns rendered at once with a column picker for the rest. Accept visible lag beyond
~150 columns.

---

## 10. Risks and required behaviour changes

| # | Risk | Severity | Action |
| --- | --- | --- | --- |
| **R-1** | Pseudonyms are generated from an un-seeded `Random` (`RandSeed = 0`, no `Randomize` anywhere): **reproducible across runs and machines**, yet **unstable within a session**. | **Critical (privacy)** | Replace with a keyed generator: `pseudonym = 1 + HMACSHA256(exportSalt, personId) mod (9*scale)` with a per-export cryptographic salt, plus rejection on collision. Stable within an export, unlinkable across exports. Make the salt storable if a study genuinely needs longitudinal linkage — as an explicit, logged choice. |
| **R-2** | The `.mapping.txt` re-identification key is written next to the CSV and, for the Excel path, is left in `%TEMP%` forever (never added to `fFilesThatMustBeDeleted`). | **Critical (privacy)** | Track the key file in `TempFileTracker`; default to *not* writing it; when written, prompt for a separate destination, warn in the UI, and audit-log the event. |
| **R-3** | Grid `Anonymous` (display) and `TPersonIdentification` (export) are independent code paths that only happen to share a radio group. Hiding is column-width `-1`, not redaction; `goColSizing` is on. | **High (privacy)** | Model one `PrivacyMode` in the view-model; derive both the visible columns and the export options from it. The export path must read the mode, never a control's state. |
| **R-4** | No de-duplication of column varnames across collectors; `TDataCollector.FVarList` is never cleared, so stale variables from a previous population survive. | High | De-duplicate in `ResultMatrix.AddColumns` (ordinal), and give collectors an explicit `Reset()` before each run. Log dropped duplicates. |
| **R-5** | The entire percentile-colouring subsystem is dead: no `TColoredDataPoint` is ever created **and** `TPercentileColoring.Create` never assigns `fLabClassId` (`QuickStat.Percentile.pas:83-117`). | High | Port it correctly but **off by default** behind a flag. Verify `Report.GetPercentileRanksByClassId` returns data for the 35 IDs in §3.3 before enabling. Decide with the product owner whether red/green percentile shading is wanted at all. |
| **R-6** | All 22 `ATC_*` drug datapoint registrations and the `DRUG.F/.B/.U/.X/.K` captions are unreachable because the real varnames are `ATC_<code>.<TreatType>`. | Medium | Fix the keys during the port (`ATC_A10.F`, …) or normalise varnames. Coordinate with the collectors work item. |
| **R-7** | Export uses `Format('%g')` with the **ambient locale** → `,` decimal separator, and ANSI/CP1252 with no BOM. Downstream scripts depend on this. | Medium | Keep `CsvDialect.Legacy` bit-exact and default to it for existing users; offer `Rfc4180` (UTF-8 + `.`) as an opt-in. Register `CodePagesEncodingProvider` in startup — .NET Core does not ship CP1252 by default. |
| **R-8** | Export writes the *raw* `%g` value; `ICellText` overrides (`Ja/Nei`, `Rgm/AF`, `2016`, BMI `%.1f`) appear on screen only. Users may not know screen ≠ file. | Medium | Preserve the behaviour (it is correct for analysis) but add an option "export display text" for the xlsx writer, and document it. |
| **R-9** | `Sodium`/`Potassium`/`Haemoglobin` rules have no `NoData` branch — a value of `0` (or a genuinely missing 0) renders as `clGraveRisk`. | Medium (clinical) | Add an explicit `<= 0 → NoData` guard, matching the other two-sided rules. Flag as an intentional deviation in release notes. |
| **R-10** | Exporting before `Lock` writes `"(not ready)"` into every cell; an empty population writes a phantom `"nil"` row. | Low | Guard the export command with `matrix.IsLocked && matrix.RowCount > 0`; the command's `CanExecute` should already cover it (`actExportData.Enabled := fGrid.Data.HasData`, `MainQuickStat.pas:671`). |
| **R-11** | The Delphi model *is* the grid: `TPersonGridData` writes cell objects into `IPersonGridComponent` and reads them back. A naive port carries the view dependency into the domain. | High (architectural) | Invert it as in §8.1: the matrix owns rows/columns; the view is a pure projection. This is the single largest structural change and must be done first. |
| **R-12** | Cell storage keys are `Format('%d:%d',[col,row])` strings — one string allocation per cell per access (`Emetra.Classes.SparseArray.pas:50`). | Medium (perf) | Gone by construction in the new model (`Rows[i].Points[varName]`). |
| **R-13** | `TExcelAdapter` reads the Excel path from HKLM and blocks the UI thread until Excel exits (`Emetra.Win.Launcher.pas:92-98`). | Medium | Replace with `Process.Start(UseShellExecute = true)` on the file, fire-and-forget; no registry probing, works with any registered handler, and never blocks. |
| **R-14** | `UpdateStyle` (font-metric-driven sizing) is dead code, so the app is hard-wired to 17 px rows / 64 px columns regardless of DPI or font. | Medium (a11y) | In WPF, derive row height and default column width from the actual typeface metrics and `DpiScale`; keep 64/120 as *logical* defaults. |
| **R-15** | Excel column limit 16 384; with timestamps a 8 200-variable matrix silently overflows. | Low | Explicit precondition + user-facing error in `XlsxMatrixWriter`. |
| **R-16** | `EPR.Lab.Percentile.pas` contributes only `TExactNumber` + `TPercentileRanker`; the rest (`LoadById`, `LoadByName`, `SetId`, `SetName`, `OnChange`) is dead, as are two stored procedures. | Low | Do not port. Confirm with the DBA that `Report.GetPercentileRanks` / `…ById` have no other callers before removing them from the DB contract. |
| **R-17** | Dead datapoint classes: `THbA1cHistoryDatapoint`, `TDrugGreenDatapoint`, `TDrugRedDatapoint`, `THeartRhythmDatapoint`, `TGeneralDirectionDatapoint`, `TColoredDataPoint`. Dead interfaces: `IEmptyColor`, `ICustomColor` (only meaningful with R-5), `ITitleDictionary.GetVarSubtitle`, `MatrixColumn.Subtitle`, `TQaImageType`. | Low | Port the *rules* into `StandardRules.cs` (cheap, they are a few lines each and may be wanted) but do not port the plumbing. Drop `Subtitle` and `IEmptyColor` entirely. |
| **R-18** | Screenshots are from build **19.8.14.477** and may not match the current sources (e.g. the export radio group is a checkbox "Export PID only as identification" in the screenshots, three radio buttons in `MainQuickStat.dfm:1234-1263`). | Low | Treat the DFM + `.pas` as authoritative; use the screenshots for palette and layout only. |

---

## 11. Quick reference — constants the implementation needs

```
FixedRows                 = 1
FixedCols                 = 4
COL_PERSON_ID             = 0   HDR_PID           = "PID"
COL_PERSON_DOB            = 1   HDR_BORN          = "Født"
COL_PERSON_NATIONAL_ID    = 2   HDR_NATIONAL_ID   = "Fødselsnummer"
COL_PERSON_NAME           = 3   HDR_NAME          = "Navn"

DefaultRowHeight          = 17      RowHeights[0]   = 18
COL_WIDTH (normal)        = 64      wide            = 120
PID col width             = 44      DOB = 64   fnr = 84   Navn = 128
Cell padding              = 3 px horizontal, 1 px vertical

CSV separator             = ';'   (trailing separator on every line)
CSV quoting               = always '"', embedded '"' doubled; pseudonym unquoted
CSV encoding              = ANSI / CP1252, no BOM, CRLF
CSV timestamp format      = "yyyy-MM-dd"; extra header cell "<VarName>.DATE"
Key file                  = <csv path with extension replaced by> ".mapping.txt", lines "<pseudo>=<pid>"

Pseudonym scale           = smallest 10^k >= (1 + rowCount);  value in [scale, 10*scale - 1]
FullName                  = LastName + ", " + FirstName
Sex                       = 1 → Male, 2 → Female, else Unknown   (Matrix.Row.pas:210-217)
```
