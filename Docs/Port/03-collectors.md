# 03 — The Collector Subsystem

Port reference for QuickStat (Delphi VCL) → WPF `net10.0-windows` / C#.

**Scope.** Everything that turns "user ticks a box in the list" into rows of
`(PersonId, VarName, Value, Timestamp, RowId [, ItemId] [, Caption])` that are folded into a
person × variable matrix.

**Sources analysed (all read-only):**

| File | Role |
| --- | --- |
| `C:\work\FastTrak.Quickstat\QuickStat.Collectors.pas` | The registry. `TQuickStatCollectors.PrepareStudy` is the entry point. |
| `C:\work\FastTrak.Quickstat\FastTrak\EPR.QA.Collector.Base.pas` | `TDataCollector`, `TCustomDataCollector`, batching, `{IdList}` substitution, row→datapoint mapping. |
| `C:\work\FastTrak.Quickstat\FastTrak\EPR.QA.Collector.Factory.pas` | `TCollectorFactory.CreateCollector` — 126 known collector names. |
| `C:\work\FastTrak.Quickstat\FastTrak\EPR.QA.Collector.Names.pas` | All collector-name constants, prefixes and Norwegian resource strings. |
| `C:\work\FastTrak.Quickstat\FastTrak\EPR.QA.Collector.Standard.pas` | `TFormDataCollector`, `TFormInstanceCollector`, `TFormDataNumericCollector`. |
| `C:\work\FastTrak.Quickstat\FastTrak\EPR.QA.Collector.Demographics.pas` | Demographics + study-scoped "global" collectors. |
| `C:\work\FastTrak.Quickstat\FastTrak\EPR.QA.Collector.Diagnose.pas` | `TDiagnoseCollector`, `TDementiaCollector`. |
| `C:\work\FastTrak.Quickstat\FastTrak\EPR.QA.Collector.Drug.pas` | `TDrugCollector` + the hand-written drug SQL. |
| `C:\work\FastTrak.Quickstat\FastTrak\EPR.QA.Collector.Labdata.pas` | `TLabSetCollector`, trust-level collectors. |
| `C:\work\FastTrak.Quickstat\FastTrak\EPR.QA.Collector.VarSet.pas` | `TVarSetCollector`, `TVarSetAgeCollector`, `TVarSetMaxCollector`, `TFormAgeCollector`. |
| `C:\work\FastTrak.Quickstat\FastTrak\EPR.QA.SQL.pas` | The SQL library (`Sp*` functions, placeholders). |
| `C:\work\FastTrak.Quickstat\FastTrak\EPR.QA.Definitions.pas` | `SET_*` item-id arrays, `LABCLASSES_*` lab-class arrays. |
| `C:\work\FastTrak.Quickstat\FastTrak\VMR.Lab.Interfaces.pas` | `TLabTest` enum, `LABSET_*` Delphi sets. |
| `C:\work\FastTrak.Quickstat\FastTrak\EPR.QA.Matrix.pas` | `TPersonGridData.AddData` — drives batching. |
| `C:\work\FastTrak.Quickstat\FastTrak\EPR.QA.Collection.pas`, `.Geriatri.pas` | Alternative (FastTrak-embedded) grouping of the same collectors. Not used by QuickStat. |

**Provenance finding (important).** The `FastTrak\` copies in the QuickStat repo are byte-identical
(modulo BOM and a `Generics.Collections` → `System.Generics.Collections` namespace fix applied by
`fix-namespaces.ps1`) to the **`develop_old`** branch of `C:\work\FastTrak`, which is the *mainline*
lineage. The four "lost" features were never merged from the `tarmscreening` lineage to mainline —
`git branch --contains` shows `4c96c3c3b`, `9f4a5ed4f`, `8a9954c13` and `fefc8a809` on
`origin/tarmscreening/develop` and friends, **never on `develop`**. So the code that ships today is
the local copy; the four features have to be re-applied by hand.

---

## 0. Runtime contract (read this before A and B)

### 0.1 The collector object

`TDataCollector` (`EPR.QA.Collector.Base.pas`) holds:

| Field | Meaning |
| --- | --- |
| `FName` | Stable identity (`QS_*` / `QST_*` constant). Used by saved "packages" and `TryFindCollector`. |
| `FTitle` | The Norwegian string shown in the checkbox list. **Some subclasses append a suffix** (§0.4). |
| `FVarPrefix` | Prepended to every `VarName` coming out of SQL to form the matrix column name. |
| `FSQL` | The SQL template, built in the constructor / `AfterConstruction`. |
| `FMaxBatchSize` | `1`, `100`, `200` or `maxint`. Controls how the person list is passed (§C). |
| `FVarList` | `TStringList`, `Sorted := True`, `Duplicates := dupIgnore` — the distinct column names produced. Column order in the grid is therefore **alphabetical**, not source order. |

### 0.2 Result-set shape

`TDataCollector.RunBatch` reads **by ordinal position**:

| Ordinal | Meaning | Delphi accessor |
| --- | --- | --- |
| `Fields[0]` | `PersonId` | `AsInteger` |
| `Fields[1]` | `VarName` (column suffix) | `AsString` |
| `Fields[2]` | Value | `AsFloat` |
| `Fields[3]` | Timestamp | `AsDateTime` |
| `Fields[4]` | RowId (source row identity) | `AsInteger` |

Two further columns are looked up **by name** and are optional:

* `ItemId` → `TDataPoint.ItemId`
* `Caption` → `TDataPoint.Caption`

Every collector SQL must therefore emit at least five columns in that exact order. Extra columns
(`OrderBy`, `rnk`, `ReverseOrder`, `ItemId`, `Caption`, `OrderNumber`) are tolerated because they sit
after position 4 — **except** where `Caption` is deliberately placed at position 5.

Rows whose `PersonId` is not in the current batch are counted and logged as
`'Unknown patients found, n =%d'` and silently discarded. This is load-bearing: several collectors
run **unfiltered over the whole database** and rely on this filter (§C.2).

Column name in the matrix = `FVarPrefix + Fields[1].AsString`.

### 0.3 Batching loop

`TPersonGridData.AddData` (`EPR.QA.Matrix.pas:143`):

```
for personId in fPopulation.Keys do
  collector.AddToBatch(row)
  if collector.BatchIsFull then collector.RunBatch(studyId)
if collector.BatchSize > 0 then collector.RunBatch(studyId)
then: for each name in collector.VarNames -> add a grid column
```

`TDataCollector.SQL` then decides how the ids reach the server:

```pascal
function TDataCollector.SQL: string;
begin
  if FMaxBatchSize <= 1 then
    Result := FSQL                                   // uses ":PersonId", bound positionally
  else
    Result := StringReplace( FSQL, PID_LIST_PLACEHOLDER,
                             '(' + pidList.DelimitedText + ')', [rfIgnoreCase, rfReplaceAll] );
end;
```

and `RunBatch` dispatches:

```pascal
if SinglePatient then dataset := FDB.FastQuery( SQL, [FLastId] )
else                  dataset := FDB.FastQuery( SQL );
```

### 0.4 Title suffixes applied by the class (do not lose these)

| Class | Suffix / transform |
| --- | --- |
| `TVarSetCollector` (all four `CreateFor*`) | `ATitle + ' (siste)'` (`TXT_LAST`) |
| `TVarSetAgeCollector` | `ATitle + ' (siste)'` |
| `TVarSetMaxCollector` | `ATitle + ' (høyeste)'` (`TXT_MAX`) |
| `TFormAgeCollector` | `ATitle + ' (siste)'` |
| `TLabSetCollector` | if `Pos(':', AGroupName) = 0` → `Format('Labdata: %s (siste)', [AGroupName])`, else the string verbatim |
| everything else | verbatim |

The Title is what the UI lists (`MainQuickStat.pas:485`,
`cbDataCollector.Items.AddObject( Collectors[n].Title, Collectors[n] )`) and it is also matched by
`TryFindCollector` (`SameText(name)` **or** `SameText(title)`), so titles must stay unique.

### 0.5 Prefix constants (`EPR.QA.Collector.Names.pas`)

```
GEN_PREFIX_DRUG_VS_DIAGNOSE = 'RXDX.'      GEN_PREFIX_DRUID          = 'DRUID.'
VAR_PREFIX_DRUG_COUNT  = 'ATCn_'           VAR_PREFIX_DRUG           = 'ATC_'
VAR_PREFIX_DRUG_FAST   = 'ATCF_'           VAR_PREFIX_DRUG_TREAT     = 'TREATn_'
VAR_PREFIX_DRUG_SPAN   = 'DRUG_'           VAR_PREFIX_DRUG_NorGeP    = 'NorGeP'
VAR_PREFIX_LAST2M      = 'LAST2M.'         VAR_PREFIX_LAST3M         = 'LAST3M.'
VAR_PREFIX_LAST6M      = 'LAST6M.'         VAR_PREFIX_LAST12M        = 'LAST12M.'
VAR_PREFIX_ITEMAGE     = 'ITEMAGE.'        VAR_PREFIX_OVERTREAT      = 'OVERTREAT.'
VAR_PREFIX_LAST_BELOW  = 'LASTUNDER.'
PREFIX_FORMAGE_COLLECTOR  = 'FORMAGE.'     PREFIX_PATIENT            = 'PATIENT.'
PREFIX_DRUG_COLLECTOR     = 'DRUG.'        PREFIX_DRUGFAST_COLLECTOR = 'DRUGFAST.'
PREFIX_DRUGNEED_COLLECTOR = 'DRUGNEED.'    PREFIX_LAB_COLLECTOR      = 'LAB.'
PREFIX_LAB_VARIABLE       = ''             PREFIX_DIAGNOSE_COLLECTOR = 'DX.'
PREFIX_DIAGNOSE_COUNT     = 'DXC.'         PREFIX_NDV_COLLECTOR      = 'NDV.'
PREFIX_GBD                = 'GBD.'         PREFIX_FORM               = 'FORM.'
PREFIX_FORMS_COLLECTOR    = 'FORMS.'       PREFIX_FORMS3M_COLLECTOR  = 'FORMS3M.'
PREFIX_FORMS6M_COLLECTOR  = 'FORMS6M.'     PREFIX_FORMS12M_COLLECTOR = 'FORMS12M.'
PREFIX_FORMS24M_COLLECTOR = 'FORMS24M.'    PREFIX_STUDY              = 'STUDY.'
PREFIX_FLACKER_KIELY      = 'FK.'
```
`EPR.QA.Collector.VarSet.pas` additionally defines `ITEM_PREFIX = 'ITEM.'`,
`ITEM_MAX_PREFIX = 'ITEMMAX.'`, `ITEM_AGE_PREFIX = 'ITEMAGE.'`, `FORM_AGE_PREFIX = 'FORMAGE.'`.

### 0.6 Two helper functions everything depends on

```pascal
function ConvertAtcPatternToVariableName( const AMatchPatternAtc: string ): string;
begin
  Result := TRegEx.Replace( AMatchPatternAtc, '\[', 'x' );      // '[' -> 'x'
  Result := TRegEx.Replace( Result, '[%\]]', EmptyStr );        // drop '%' and ']'
end;
```
`'C0[23789]%'` → `'C0x23789'`; `'E1[014]%'` → `'E1x014'`; `'A10%'` → `'A10'`.

```pascal
function ConvertArrayToList( const AIdentifiers: array of integer ): string;
// -> '3224, 3225, 3310'   (', ' separated, leading ', ' stripped)
```

---

## A. Complete collector inventory

**Counts.** The factory knows **126** distinct collector names. `PrepareStudy` can register at most
**126 static** collectors (87 via the factory + 39 constructed directly), plus **2 × N dynamic**
form collectors, where N = number of non-anonymous form classes in the study. Restoring the four
lost features raises the static maximum to **131**, which Phase 4 did — see the status box at the
head of §E.

Per study-name gate:

| Gate | Collectors added |
| --- | --- |
| always | 36 static (+ 2N dynamic) |
| `GBD\|LANGTID\|KORTTID` | +76 (24 GBD + 17 diagnose + 35 drug) |
| `NDV\|ENDO\|LANGTID\|GBD\|KORTTID` | +8 |
| `GWAS` | +3 |
| `ROAS` | +2 (+1 when `QS_ROAS_BASE` is restored) |
| `DOGFOOD` (case-insensitive) | +1 |

Legend for the *Gate* column: **A** = always, **G** = `GBD|LANGTID|KORTTID`,
**N** = `NDV|ENDO|LANGTID|GBD|KORTTID`, **W** = `GWAS`, **R** = `ROAS`, **D** = `DOGFOOD`,
**[recover]** = commented out in this repository's Delphi, see §E. All five have been restored in
the .NET port; the marker records why the Delphi row carries no number.

Rows are in registration order (= order in the checkbox list).

### A.1 `PrepareStudy` — direct

| # | Name constant | Name value | Display title (exact, as shown) | Class | Gate | SQL / ids |
| --: | --- | --- | --- | --- | --- | --- |
| 1 | `QS_FORM_FREQUENCY` | `FORMS.FREQUENCY` | `Skjema: Antall totalt per type` | `TFormInstanceCollector` | A | `EXEC Report.GetFormInstances :PersonId` (batch **1**) |

### A.2 `AddCollectorsBasic` — always

| # | Name constant | Name value | Display title | Class | Gate | SQL / ids |
| --: | --- | --- | --- | --- | --- | --- |
| 2 | `QS_PATIENT_AGE` | `PATIENT.AGE` | `^ Alder` | `TAgeCollector` | A | `QRY_DEMOGRAPHICS('AGE','DATEDIFF(YYYY,DOB,GETDATE())')` |
| 3 | `QS_PATIENT_SEX` | `PATIENT.SEX` | `^ Kjønn` | `TGenderCollector` | A | `QRY_DEMOGRAPHICS('SEX','GenderId')` |
| 4 | `QS_PATIENT_YOB` | `PATIENT.YOB` | `^ Fødselsår` | `TYOBCollector` | A | `QRY_DEMOGRAPHICS('YOB','DATEPART(YYYY,DOB)')` |
| 5 | `QS_PATIENT_YOD` | `PATIENT.YOD` | `^ Dødsår` | `TYODCollector` | A | `QRY_DEMOGRAPHICS('YOD','DATEPART(YYYY,DeceasedDate)')` |
| 6 | `QS_PATIENT_MOB` | `PATIENT.MOB` | `^ Fødselmåned` | `TMOBCollector` | A | `QRY_DEMOGRAPHICS('MOB','DATEPART(MM,DOB)')` |
| 7 | `QS_PATIENT_ZIP` | `PATIENT.ZIP` | `^ Postnummer` | `TPostCodeCollector` | A | `QRY_DEMOGRAPHICS('ZIP','CONVERT(INTEGER,PostalCode)')` |
| 8 | `QS_STUDY_STATUS` | `STUDY.STATUS` | `^ Statuskode` | `TStatusCollector` | A | `SpStudCaseFields('StatusId','FinState',StudyId)` |
| 9 | `QS_STUDY_CENTER` | `STUDY.CENTER` | `^ Institusjon / sted` | `TCenterCollector` | A | `SpStudyCenter(StudyId)` |
| 10 | `QS_STUDY_GROUP` | `STUDY.GROUP` | `^ Gruppe / avdeling nå` | `TGroupCollector` | A | `SpStudCaseFields('GroupId','GroupId',StudyId)` |
| 11 | `QS_STUDY_GROUP_DEATH` | `STUDY.GROUP_DEATH` | `^ Gruppe / avdeling ved død` | `TGroupAtDeathCollector` | A | `SpStudyGroupDeath(StudyId)` |
| 12 | `QS_STUDY_CENTER_DEATH` | `STUDY.CENTER_DEATH` | `^ Institusjon / sted ved død` | `TCenterAtDeathCollector` | A | `SpStudyCenterDeath(StudyId)` |
| 13 | `QS_FORM_COUNT24M` | `FORMS24M.FREQUENCY` | `Skjema: Antall siste 24 mnd per type` | `TCustomDataCollector` | A | `SpRecentFormCountAll(24)`, prefix `FORMS24M.` |
| 14 | `QS_FORM_COUNT12M` | `FORMS12M.FREQUENCY` | `Skjema: Antall siste 12 mnd per type` | `TCustomDataCollector` | A | `SpRecentFormCountAll(12)`, prefix `FORMS12M.` |
| 15 | `QS_FORM_COUNT6M` | `FORMS6M.FREQUENCY` | `Skjema: Antall siste 6 mnd per type` | `TCustomDataCollector` | A | `SpRecentFormCountAll(6)`, prefix `FORMS6M.` |
| 16 | `QS_FORM_COUNT3M` | `FORMS3M.FREQUENCY` | `Skjema: Antall siste 3 mnd per type` | `TCustomDataCollector` | A | `SpRecentFormCountAll(3)`, prefix `FORMS3M.` |

### A.3 `AddCollectorsLabData` — always

| # | Name constant | Name value | Display title | Class | Gate | SQL / ids |
| --: | --- | --- | --- | --- | --- | --- |
| 17 | `QST_LAB_KIDNEY` | `LAB.KIDNEY` | `Labdata: Nyrefunksjon (siste)` | `TLabSetCollector.CreateOldSchool` | A | `LABSET_KIDNEY` (`TLabSet` → ordinals, §A.8) |
| 18 | `QST_LAB_ANEMIA` | `LAB.ANEMIA` | `Labdata: Anemi (siste)` | `TLabSetCollector` | A | `LABCLASSES_ANEMIA` |
| 19 | `QST_LAB_LIPIDS` | `LAB.LIPIDS` | `Labdata: Lipider (siste)` | `TLabSetCollector` | A | `LABCLASSES_LIPIDS` |
| 20 | `QST_LAB_DIGITALIS` | `LAB.DIGITALIS` | `Labdata: Digitalis (siste)` | `TLabSetCollector` | A | `LABCLASSES_DIGITALIS` |
| 21 | `QST_LAB_LIVER` | `LAB.LIVER` | `Labdata: Leverstatus (siste)` | `TLabSetCollector` | A | `LABCLASSES_LIVER` |
| 22 | `QST_LAB_THYROID` | `LAB.THYROID` | `Labdata: Tyreoidea (siste)` | `TLabSetCollector` | A | `LABCLASSES_THYROID` |
| 23 | `QST_LAB_GLUCOSE` | `LAB.GLUCOSE` | `Labdata: Glukose (siste)` | `TLabSetCollector` | A | `LABCLASSES_GLUCOSE` |
| 24 | `QST_LAB_INR` | `LAB.INR` | `Labdata: INR fra labarket (siste)` | `TLabSetCollector` | A | `LABCLASSES_INR` |
| 25 | `QST_LAB_HYPERPARA` | `LAB.HYPERPARA` | `Labdata: Hyperparatyreoidisme (siste)` | `TLabSetCollector` | A | `LABCLASSES_HYPERPARA` |
| 26 | `QST_LAB_HEART_FAILURE` | `LAB.HEART_FAILURE` | `Labdata: Hjertesviktrelaterte labdata (siste)` | `TLabSetCollector` | A | `LABCLASSES_HEART_FAILURE` |
| — | `QST_LAB_INTERLEUKINS` | `LAB.INTERLEUKINS` | `Labdata: Interleukiner (siste)` | `TLabSetCollector` | A **[recover]** | `LABCLASSES_INTERLEUKINS` — §E.4 |
| 27 | `QST_LAB_CRP` | `LAB.CRP` | `Labdata: CRP (siste)` | `TLabSetCollector` | A | `LABCLASSES_CRP` |
| 28 | `QST_LAB_HIGH` | `LAB.TRUST3` | `Labdata: Alle med høy konfidens` | `TLabHighTrustCollector` | A | `SpSnapshotLabdataByTrustLevel(3)` |
| 29 | `QST_LAB_MEDIUM` | `LAB.TRUST2` | `Labdata: Alle med middels konfidens` | `TLabMediumTrustCollector` | A | `SpSnapshotLabdataByTrustLevel(2)` |
| 30 | `QST_LAB_LOW` | `LAB.TRUST2` ⚠ | `Labdata: Alle med lav konfidens` | `TLabLowTrustCollector` | A | `SpSnapshotLabdataByTrustLevel(1)` — **name bug**, see §A.9 |
| 31 | `QST_LAB_COUNT_3M` | `LAB.COUNT_3M` | `Labdata: Antall prøver siste 3 mnd` | `TCustomDataCollector` | A | `SpRecentLabdataPresent(3)`, prefix `''` |
| 32 | `QST_LAB_COUNT_6M` | `LAB.COUNT_6M` | `Labdata: Antall prøver siste 6 mnd` | `TCustomDataCollector` | A | `SpRecentLabdataPresent(6)` |
| 33 | `QST_LAB_COUNT_12M` | `LAB.COUNT_12M` | `Labdata: Antall prøver siste 12 mnd` | `TCustomDataCollector` | A | `SpRecentLabdataPresent(12)` |
| 34 | `QST_LAB_COUNT_24M` | `LAB.COUNT_24M` | `Labdata: Antall prøver siste 24 mnd (2 år)` | `TCustomDataCollector` | A | `SpRecentLabdataPresent(24)` |
| 35 | `QST_LAB_COUNT_60M` | `LAB.COUNT_60M` | `Labdata: Antall prøver siste 60 mnd (5 år)` | `TCustomDataCollector` | A | `SpRecentLabdataPresent(60)` |

### A.4 `AddCollectorsStudySpecific` — always, **dynamic**

Driven by `EXEC Report.GetFormClasses :StudyId` (fields `FormName`, `FormTitle`). Form names
matching `FORM\d+` are skipped ("anonymous forms"); duplicates by `FormName` are skipped.
For every remaining form class **two** collectors are added:

| Name | Display title | Class | SQL |
| --- | --- | --- | --- |
| `<FormName>` | `Skjema-alder: <FormTitle> (<FormName>) (siste)` | `TFormAgeCollector` | `SpFormAgeSingle` with `{FormName}` → `'<FormName>'`, prefix `FORMAGE.`, batch 100 |
| `FORM.<FormName>` | `Skjema-data: <FormTitle> (<FormName>)` | `TFormDataCollector` | `EXEC Report.GetFormData :PersonId, '<FormName>'`, prefix `<FormName>.`, batch **1** |

Templates: `StrTitleFormAgeTemplate = 'Skjema-alder: %s (%s)'`,
`StrTitleFormDataTemplate = 'Skjema-data: %s (%s)'`.

> The `TFormDataCollector` line is duplicated in the source — one call is commented out and an
> identical uncommented call follows it. Register it **once**.

### A.5 `AddCollectorsHardCoded` — always

| # | Name constant | Name value | Display title | Class | Gate | SQL / ids |
| --: | --- | --- | --- | --- | --- | --- |
| 36 | *(literal)* `'SIZE'` | `SIZE` | `Antropometri: Høyde og vekt (siste)` | `TVarSetCollector.CreateForNumeric` | A | `SET_HEIGHT_WEIGHT_BMI` = `3224, 3225, 3310` |

### A.6 `AddCollectorsHardCoded` — gate **G** = `GBD|LANGTID|KORTTID`

| # | Name constant | Name value | Display title | Class | SQL / ids |
| --: | --- | --- | --- | --- | --- |
| 37 | `QS_WEIGHT_DAYS` | `GBD.WEIGHT.DAYS` | `GBD: Tid siden siste veiing (siste)` | `TVarSetAgeCollector` | `SpSnapshotVarsetAge(SET_WEIGHT)` = `3224` |
| 38 | `QS_GBD_TVANGSVEDTAK` | `GBD.AKTIV_TVANG` | `GBD: Aktivt tvangsvedtak` | `TCustomDataCollector` | `QRY_TVANGSVEDTAK` = `EXEC Report.ColGbdTvangsvedtak`, prefix `''` |
| 39 | `QS_GBD_INNLEGGELSE_12M` | `FORMS12M.GBD_INNLEGGELSE` | `GBD: Innleggelser siste 12 mnd` | `TCustomDataCollector` | `SpRecentFormCountSingle('GBD_INNLEGGELSE',12)`, prefix `FORMS12M.` |
| 40 | `QS_GBD_FORM_LEGE3M` | `FORMS3M.LEGEALLE` | `GBD: Legenotater siste 3 mnd` | `TCustomDataCollector` | `SpRecentFormGroupLege3m`, prefix `FORMS.` |
| 41 | `QS_GBD_SCORES` | `GBD.SCORES` | `GBD: Viktigste scores (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_GBD_SCORES` |
| 42 | `QS_GBD_BP` | `GBD.BP` | `GBD: Blodtrykk fra kurve (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_GBD_BP` = `3555, 3556` |
| 43 | `QS_GBD_PRIMARY_CONTACT` | `GBD.VARAGE.8420` | `GBD: Primærkontakt registrert (siste)` | `TVarSetCollector.CreateForText` | `SET_GBD_PRIMARY_CONTACT` = `8420` |
| 44 | `QS_GBD_WEIGHT_2M` | `GBD.WEIGHT_2M` | `GBD: Vekt fra siste 2 mnd` | `TCustomDataCollector` | `SpRecentQuantityPresent(3224, 2)`, prefix `LAST2M.` |
| 45 | `QS_GBD_SBP_2M` | `GBD.WEIGHT_2M` ⚠ | `GBD: Blodtrykk fra siste 2 mnd` | `TCustomDataCollector` | `SpRecentQuantityPresent(3556, 2)`, prefix `LAST2M.` — **name bug**, §A.9 |
| 46 | `QS_GBD_FLACKER_12M` | `GBD.FLACKER_12M` | `GBD: Flacker-Kiely siste 12 mnd` | `TCustomDataCollector` | `SpRecentQuantityPresent(1128, 12)`, prefix `LAST12M.` |
| 47 | `QS_GBD_FLACKER_DEATH` | `GBD.FLACKER_DEATH` | `GBD: Flacker-Kiely og levedager` | `TCustomDataCollector` | `SpFlackerKileyDeath`, prefix `FK.` |
| 48 | `QS_GBD_HULTEN_3M` | `GBD.HULTEN_3M` | `GBD: Hulten siste 3 mnd` | `TCustomDataCollector` | `SpRecentQuantityPresent(4234, 3)`, prefix `LAST3M.` |
| 49 | `QS_GBD_QUALID_6M` | `GBD.QUALID_6M` | `GBD: Qualid siste 6 mnd` | `TCustomDataCollector` | `SpRecentQuantityPresent(5827, 6)`, prefix `LAST6M.` |
| 50 | `QS_GBD_KDV_6M` | `GBD.KDV_6M` | `GBD: KDV siste 6 mnd` | `TCustomDataCollector` | `SpRecentQuantityPresent(1685, 6)`, prefix `LAST6M.` |
| 51 | `QS_GBD_BARTHEL_6M` | `GBD.BARTHEL_6M` | `GBD: Barthel ADL-Indeks siste 6 mnd` | `TCustomDataCollector` | `SpRecentQuantityPresent(4342, 6)`, prefix `LAST6M.` |
| 52 | `QS_GBD_STRATIFY_6M` | `GBD.STRATIFY_6M` | `GBD: Stratify fallrisiko siste 6 mnd` | `TCustomDataCollector` | `SpRecentQuantityPresent(9257, 6)`, prefix `LAST6M.` |
| 53 | `QS_GBD_MNA_6M` | `GBD.MNA_6M` | `GBD: MNA ernæringsvurdering siste 6 mnd` | `TCustomDataCollector` | `SpRecentQuantityPresent(4771, 6)`, prefix `LAST6M.` |
| 54 | `QS_GBD_ANTIHYPERTENSIVES_LOW_BP` | `GBD.ANTIHT_LOW_BP` | `GBD: Blodtrykk < 120 og blodtrykksbehandling` | `TCustomDataCollector` | `SpDrugHypertensionWithLowBp(120)` = `EXEC Report.ColAntiHypertensivesLowBP 120`, prefix `OVERTREAT.` |
| 55 | `QS_GBD_LOW_BP` | `GBD.LOW_BP` | `GBD: Blodtrykk < 120 (siste)` | `TCustomDataCollector` | `SpSnapshotQuantityIfBelowThreshold(3556, 120.0)`, prefix `LASTUNDER.` |
| 56 | `QS_GBD_C09_GFR` | `GBD.C09_GFR` | `GBD: ACE/A2 og eGFR < 35` | `TCustomDataCollector` | `SpDrugAndRenalFunction('C09%', 35)` = `EXEC Report.ColDrugAndRenalFunction 'C09%', 35`, prefix `LASTUNDER.` |
| 57 | `QS_GBD_METFORMIN_GFR` | `GBD.METFORMIN_GFR` | `GBD: Metformin og eGFR < 50 ` *(trailing space)* | `TCustomDataCollector` | `SpDrugAndRenalFunction('A10BA%', 50)`, prefix `LASTUNDER.` |
| 58 | `QS_GBD_LMG_6M` | `GBD.LMG_6M` | `GBD: Skjema "Legemiddelgjennomgang" siste 6 mnd (kompletthet)` | `TCustomDataCollector` | `SpRecentFormCompleteness('LMG', 6)`, prefix `FORMS6M.` |
| 59 | `QS_GBD_BESLUTNINGER_6M` | `GBD.GBD_BESLUTNINGER_6M` | `GBD: Skjema "Beslutninger" siste 6 mnd (kompletthet)` | `TCustomDataCollector` | `SpRecentFormCompleteness('GBD_BESLUTNINGER', 6)`, prefix `FORMS6M.` |
| 60 | `QST_LAB_GERIATRIC` | `LAB.GERIATRIC` | `GBD: Sentrale labdata (siste)` | `TLabSetCollector` | `LABCLASSES_GERIATRIC` (title has `:` → not wrapped) |

### A.7 `AddCollectorsDiagnose` — gate **G** (called from the GBD block only)

| # | Name constant | Name value | Display title | Class | SQL / ids |
| --: | --- | --- | --- | --- | --- |
| 61 | `QS_DIAGNOSE_ALL1` | `DX.ALL1` | `Diagnoser: Spesifisert med 1 tegn` | `TCustomDataCollector` | `SpDiagnoseDetailsByLevel(1)`, prefix `DXC.` |
| 62 | `QS_DIAGNOSE_ALL2` | `DX.ALL2` | `Diagnoser: Spesifisert med 2 tegn` | `TCustomDataCollector` | `SpDiagnoseDetailsByLevel(2)`, prefix `DXC.` |
| 63 | `QS_DIAGNOSE_ALL3` | `DX.ALL3` | `Diagnoser: Spesifisert med 3 tegn` | `TCustomDataCollector` | `SpDiagnoseDetailsByLevel(3)`, prefix `DXC.` |
| 64 | `QS_DIAGNOSE_ALL4` | `DX.ALL4` | `Diagnoser: Spesifisert med 4 tegn` | `TCustomDataCollector` | `SpDiagnoseDetailsByLevel(4)`, prefix `DXC.` |
| 65 | `QS_DIAGNOSE_ALL5` | `DX.ALL5` | `Diagnoser: Spesifisert med 5 tegn` | `TCustomDataCollector` | `SpDiagnoseDetailsByLevel(5)`, prefix `DXC.` |
| 66 | `QS_DIAGNOSE_MISSING_E11` | `RXDX.E1xA10` | `Diagnose: Antidiabetika uten diabetesdiagnose` | `TCustomDataCollector` | `SpDrugWithoutDiagnose('A10_NOT_E1x01234','A10%','E1[01234]%')`, prefix `RXDX.` |
| 67 | *(pattern)* | `DX.C` | `Diagnoser: C - Kreft` | `TDiagnoseCollector` | `SpDiagnoseByPattern('C%')` |
| 68 | *(pattern)* | `DX.E0` | `Diagnoser: E0 - Tyreoidea-sykdommer` | `TDiagnoseCollector` | `SpDiagnoseByPattern('E0%')` |
| 69 | *(pattern)* | `DX.E1x014` | `Diagnoser: E1[014] - Diabetes Mellitus ` *(trailing space)* | `TDiagnoseCollector` | `SpDiagnoseByPattern('E1[014]%')` |
| 70 | *(pattern)* | `DX.Ex23` | `Diagnoser: E[23] - Andre endokrine lidelser )` *(stray `)`)* | `TDiagnoseCollector` | `SpDiagnoseByPattern('E[23]%')` |
| 71 | *(pattern)* | `DX.Ex789` | `Diagnoser: E[789] - Metabolske forstyrrelser` | `TDiagnoseCollector` | `SpDiagnoseByPattern('E[789]%')` |
| 72 | *(pattern)* | `DX.Fx123456789` | `Diagnoser: F[123456789]  - Psykisk lidelser` *(double space)* | `TDiagnoseCollector` | `SpDiagnoseByPattern('F[123456789]%')` |
| 73 | *(pattern)* | `DX.I1x012345` | `Diagnoser: I1[012345] - Hypertensjon` | `TDiagnoseCollector` | `SpDiagnoseByPattern('I1[012345]%')` |
| 74 | *(pattern)* | `DX.I2x012345` | `Diagnoser: I2[012345] - Iskemisk hjertesykdom` | `TDiagnoseCollector` | `SpDiagnoseByPattern('I2[012345]%')` |
| 75 | *(pattern)* | `DX.I48` | `Diagnoser: I48 - Atrieflimmer/flutter` | `TDiagnoseCollector` | `SpDiagnoseByPattern('I48%')` |
| 76 | *(pattern)* | `DX.I6x01234` | `Diagnoser: I6[01234 - Hjerneslag` *(unbalanced `[`)* | `TDiagnoseCollector` | `SpDiagnoseByPattern('I6[01234]%')` |
| 77 | *(fixed)* | `DX.DEMENTIA` | `Diagnoser: F0[123]+G03 - Demens + Alzheimer` | `TDementiaCollector` | `SpDiagnoseDementiaAndAlzheimers` |

> Registration order in the source is: cancer, thyroid, diabetes, endocrine, metabolic, psychiatry,
> hypertension, ischemia, **atrial fibrillation, stroke** (AF before stroke), then dementia.

### A.8 `AddCollectorsDrug` — gate **G**

All 18 `CreateChecksum` and 7 `CreateForTreatType` collectors use `TDrugCollector`, prefix
`ATC_`, batch 100. Name = `DRUG.` + `ConvertAtcPatternToVariableName(pattern)`.
Emitted `VarName` = `CONCAT('<pattern-name>','.',ot.TreatType)` (because
`TDrugCollector.GroupResults` is a `class var` defaulting to **False**), so the matrix columns are
e.g. `ATC_A10.F`, `ATC_A10.B`.

| # | Name value | Display title | Ctor | ATC pattern |
| --: | --- | --- | --- | --- |
| 78 | `DRUG.A10` | `Medisiner: A10 - Antidiabetika` | `CreateChecksum` | `A10%` |
| 79 | `DRUG.A10BA02` | `Medisiner: A10BA02 - Metformin alene` | `CreateChecksum` | `A10BA02` |
| 80 | `DRUG.A11EA` | `Medisiner: A11EA - Vitamin B-kompleks` | `CreateChecksum` | `A11EA` |
| 81 | `DRUG.B01AA03` | `Medisiner: B01AA03 - Warfarin` | `CreateChecksum` | `B01AA03` |
| 82 | `DRUG.B01AF` | `Medisiner: BO1AF - DOAK` *(letter O typo in title)* | `CreateChecksum` | `B01AF%` |
| 83 | `DRUG.B03BA` | `Medisiner: B03BA - Vitamin B12` | `CreateChecksum` | `B03BA%` |
| 84 | `DRUG.B03BA01` | `Medisiner: B03BA01 - Cyanokoblamin` | `CreateChecksum` | `B03BA01` |
| 85 | `DRUG.B03BA03` | `Medisiner: B03BA03 - Hydroksykobalamin` | `CreateChecksum` | `B03BA03` |
| 86 | `DRUG.C01A` | `Medisiner: C01A - Hjerteglykosider` | `CreateChecksum` | `C01A%` |
| 87 | `DRUG.C02` | `Medisiner: C02 - Antihypertensiva` | `CreateChecksum` | `C02%` |
| 88 | `DRUG.C03` | `Medisiner: C03 - Diuretika` | `CreateChecksum` | `C03%` |
| 89 | `DRUG.C07` | `Medisiner: C07 - Betablokkere` | `CreateChecksum` | `C07%` |
| 90 | `DRUG.C08` | `Medisiner: C08 - Kalsiumkanalblokkere/CCB` | `CreateChecksum` | `C08%` |
| 91 | `DRUG.C08D` | `Medisiner: C08D - CCB med kardiale effekter` | `CreateChecksum` | `C08D%` |
| 92 | `DRUG.C09` | `Medisiner: C09 - Renin/Angiotensin systemet` | `CreateChecksum` | `C09%` |
| 93 | `DRUG.C0x23789` | `Medisiner: C0[23789] - Antihypertensiva vidt definert` | `CreateChecksum` | `C0[23789]%` |
| 94 | `DRUG.M01A` | `Medisiner: M01A - NSAID` | `CreateChecksum` | `M01A%` |
| 95 | `DRUG.N04BA` | `Medisiner: N04BA - Antiparkinsonmidler` | `CreateChecksum` | `N04BA%` |
| 96 | `DRUG.N02A` | `Medisiner: N02A - Opioider` | `CreateForTreatType(ttAnyTreatType)` | `N02A%` |
| 97 | `DRUG.N02B` | `Medisiner: N02B - Analgetika/antipyretika` | `CreateForTreatType(ttAnyTreatType)` | `N02B%` |
| 98 | `DRUG.N05A` | `Medisiner: N05A - Antipsykotika` | `CreateForTreatType(ttAnyTreatType)` | `N05A%` |
| 99 | `DRUG.N05B` | `Medisiner: N05B - Anxiolytika` | `CreateForTreatType(ttAnyTreatType)` | `N05B%` |
| 100 | `DRUG.N05C` | `Medisiner: N05C - Hypnotika/sedativa` | `CreateForTreatType(ttAnyTreatType)` | `N05C%` |
| 101 | `DRUG.N06A` | `Medisiner: N06A - Antidepressiva` | `CreateForTreatType(ttAnyTreatType)` | `N06A%` |
| 102 | `DRUG.N06D` | `Medisiner: N06D - Antidemensmidler` | `CreateForTreatType(ttAnyTreatType)` | `N06D%` |

> `CreateForTreatType(..., ttAnyTreatType, ...)` is behaviourally identical to `CreateChecksum`
> here — `ttAnyTreatType` adds no `AND ot.TreatType ...` clause and yields prefix `DRUG.`.
> The `ttLongTerm` / `ttAsNeeded` variants (`DRUGFAST.` / `DRUGNEED.` prefixes, `SQL_AND_FAST` /
> `SQL_AND_BEHOV`) exist but are **never used by QuickStat**.

Factory-built drug collectors:

| # | Name constant | Name value | Display title | Class | SQL |
| --: | --- | --- | --- | --- | --- |
| 103 | `QS_DRUID_COUNT` | `DRUID.COUNT` | `Interaksjoner: Antall per nivå` | `TCustomDataCollector` | `SpDruidCountByLevel`, prefix `DRUID.` |
| 104 | `QS_DRUID_SPECIFIC` | `DRUID.SPECIFIED` | `Interaksjoner: Spesifisert i detalj` | `TCustomDataCollector` | `SpDruidIndividualInteractions(5)`, prefix `''` |
| 105 | `QS_DRUG_COUNT_GROUP` | `DRUG.GROUPCOUNT` | `Medisin: Antall på utvalgte ATC-grupper` | `TCustomDataCollector` | `QRY_DRUGCOUNT_BY_ATCGROUP`, prefix `ATCn_` |
| 106 | `QS_DRUG_COUNT_NOATC` | `DRUG.NOATC` | `Medisin: Antall uten ATC-kode` | `TCustomDataCollector` | `SpDrugCountNoAtc`, prefix `ATCn_` |
| 107 | `QS_DRUG_COUNT` | `DRUG.COUNT` | `Medisin: Antall per behandlingstype` | `TCustomDataCollector` | `QRY_DRUGCOUNT_BY_TYPE`, prefix `TREATn_` |
| 108 | `QS_DRUG_METFORMIN` | `DRUG.METFORMIN` | `Medisin: Metformin inkl. kombinasjoner` | `TCustomDataCollector` | `QRY_DRUGSET_METFORMIN`, prefix `DRUG_` |
| 109 | `QS_DRUG_ANTICHOLIN_N05` | `DRUG.ANTICHOLIN_N05` | `Medisin: N05A - Nevroleptika med sterk antikolinerg effekt (AB)` | `TCustomDataCollector` | `QRY_DRUGSET_ANTICHOLIN_N05A`, prefix `DRUG_` |
| 110 | `QS_DRUG_ANTICHOLIN_AB` | `DRUG.ANTICHOLIN_AB` | `Medisin: Sterke antikolinergika (AB)` | `TCustomDataCollector` | `QRY_DRUGSET_ANTICHOLIN_AB`, prefix `DRUG_` |
| 111 | `QS_DRUG_ANTIBIOTIC_RESISTANCE` | `DRUG.RESISTANCE_DRIVING` | `Medisin: Resistensdrivende antibiotika` | `TCustomDataCollector` | `SpDrugsetAntibiotic`, prefix `DRUG_` |
| — | `QS_DRUG_ANTIBIOTIC_INTERMEDIATE` | `DRUG.INTERMEDIATE` | `Antibiotika: Intermediære` | `TCustomDataCollector` | **[recover]** `SpDrugsetAntibioticIntermediate` — §E.1 |
| — | `QS_DRUG_ANTIBIOTIC_RECOMMENDED` | `DRUG.RECOMMENDED` | `Antibiotika: Anbefalte` | `TCustomDataCollector` | **[recover]** `SpDrugsetAntibioticRecommended` — §E.2 |
| — | `QS_DRUG_J01XX05` | `DRUG.J01XX05` | `Antibiotika: Metenamin / Hiprex` | `TDrugCollector.CreateBasic` | **[recover]** `ATC_J01XX05 = 'J01XX05'` — §E.2 |
| 112 | `QS_DRUG_NorGeP` | `DRUG.NorGEP` | `Medisin: NorGeP avvik` | `TCustomDataCollector` | `QRY_NORGEP` = `EXEC Report.NorGeP`, prefix `NorGeP` |

> Note `QS_DRUG_J01XX05` is built with `TDrugCollector.CreateBasic`, so its **name is derived from
> the ATC pattern** (`DRUG.` + `J01XX05`) and happens to equal the constant. `CreateBasic` (not
> `CreateChecksum`) means `QRY_DRUGSET_BASIC` → value is the literal `1`, not a name checksum.

`AddCollectorsDrug` also registers 22 datapoint classes with the datapoint factory before creating
collectors (`TDrugDatapoint` for `ATC_A10%`, `ATC_A10BA02`, `ATC_A11EA`, `ATC_B01AA03`, `ATC_B01AF%`,
`ATC_B03BA%`, `ATC_B03BA01`, `ATC_B03BA03`, `ATC_C0[23789]%`, `ATC_C09%`, `ATC_M01A%`, `ATC_N04BA%`,
`ATC_N05B%`, `ATC_N05C%`, `ATC_N06D%`, and `ATCF_`-prefixed `N02A%`, `N05A%`, `N05B%`, `N05C%`,
`N06D%`, plus `ATC_ANTICHOLIN_N05` and `ATC_ANTICHOLIN_AB`). These keys embed the raw `%`/`[]`
characters, so they only ever match if a column is literally named that — i.e. **effectively dead
registrations** given `VarName` is `A10.F` etc. Port them verbatim but do not expect them to fire.

### A.9 `AddCollectorsHardCoded` — gate **N** = `NDV|ENDO|LANGTID|GBD|KORTTID`

| # | Name constant | Name value | Display title | Class | ids |
| --: | --- | --- | --- | --- | --- |
| 113 | `QS_NDV_DIAGNOSE` | `NDV.DIAGNOSE` | `NDV: Basisdata (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_NDV_DIAGNOSE` = `3196, 3389, 3486` |
| 114 | `QS_NDV_TREATMENT` | `NDV.TREATMENT` | `Diabetes: Behandling (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_NDV_TREATMENT` = `3322, 4056` |
| 115 | `QS_NDV_COMPLICATIONS` | `NDV.COMPLICATIONS` | `Diabetes: Komplikasjoner (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_NDV_COMPLICATIONS` (21 ids) |
| 116 | `QS_NDV_INSULIN` | `NDV.INSULIN` | `Diabetes: Insulindosering (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_NDV_INSULIN` (8 ids) |
| 117 | `QS_NDV_HYPOGLYCEMIA` | `NDV.HYPOGLYCEMIA` | `Diabetes: Hypoglykemi (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_NDV_HYPOGLYCEMIA` = `3220, 3351, 4234, 3352` |
| 118 | `QS_NDV_EXERCISE` | `NDV.EXERCISE` | `Diabetes: Mosjon (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_NDV_EXERCISE` = `3340, 3197, 4638` |
| 119 | `QS_NDV_SOCIAL` | `NDV.SOCIAL` | `Diabetes: Sosialt (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_NDV_SOCIAL` = `3982, 4002` |
| 120 | `QST_LAB_DIABETES` | `LAB.DIABETES` | `Labdata: Diabetes (siste)` | `TLabSetCollector` | `LABCLASSES_DIABETES` (17 ids) |

### A.10 Remaining gates

| # | Name constant | Name value | Display title | Class | Gate | ids |
| --: | --- | --- | --- | --- | --- | --- |
| 121 | `QS_ROAS_GWAS_BG` | `ROAS.GWAS.BG` | `GWAS Bakgrunn (siste)` | `TVarSetCollector.CreateForNumeric` | W | `SET_GWAS_BG` (15) |
| 122 | `QS_ROAS_GWAS_AB` | `ROAS.GWAS.AB` | `GWAS Autoantistoffer (høyeste)` | `TVarSetMaxCollector` | W | `SET_GWAS_AUTOANTIBODY` (7) |
| 123 | `QS_ROAS_GWAS_AB_APS1` | `ROAS.GWAS.AB.APS1` | `GWAS APS-I spesfikk (høyeste)` *(typo "spesfikk")* | `TVarSetMaxCollector` | W | `SET_GWAS_APS1` (8) |
| 124 | `QS_ROAS_POI_ORD` | `ROAS.POI.ORD` | `POI Diagnoser (siste)` | `TVarSetCollector.CreateForNumeric` | R | `SET_POI_ORD` (20) |
| 125 | `QS_ROAS_POI_QN` | `ROAS.POI.QN` | `POI Diagnoseår (siste)` | `TVarSetCollector.CreateForNumeric` | R | `SET_POI_QN` (13) |
| — | `QS_ROAS_BASE` | `ROAS.BASE` | `Autommunitet (siste)` | `TVarSetCollector.CreateForNumeric` | R **[recover]** | `SET_ROAS_BASE` (68) — §E.3 |
| 126 | `QS_DOGFOOD_DATABASE_VERSION` | `DOGFOOD.DATABASE.VERSION` | `Dogfood: Databaseversjoner (siste)` | `TVarSetCollector.CreateForNumeric` | D | inline `[3812, 5117]` |

### A.11 Factory names the QuickStat app never registers (39)

These exist in `TCollectorFactory.CreateCollector` and are used by the FastTrak-embedded
`EPR.QA.Collection.Geriatri` frames and the (absent) Barnediabetes/NDV collections. **Port them into
the registry** — they cost nothing and keep the factory complete — but they are not reachable from
QuickStat's UI today.

| Name constant | Display title | Class | SQL / ids |
| --- | --- | --- | --- |
| `QS_GBD_FORM_MAREVAN` | `Marevanskjema` | `TFormDataCollector` | `EXEC Report.GetFormData :PersonId, ''` — **broken**, empty form name; name becomes `FORM.FORM.GBD_MAREVAN` |
| `QS_GBD_MEASURES` | `GBD: Høyde og vekt (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_HEIGHT_WEIGHT_BMI` |
| `QS_GBD_NUTRITION` | `GBD: Ernæringsdata (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_GBD_NUTRITION` = `4353, 4354, 4529, 4771` |
| `QS_ITEMAGE_MNA_PART1` | `GBD: Tid siden MNA del 1 utfylt (siste)` | `TVarSetAgeCollector` | `SET_MNA_PART1` = `4771` |
| `QS_GBD_DEMENTIA` | `GBD: Demens (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_GBD_DEMENTIA` = `4429, 1685` |
| `QS_GBD_HEART_FAILURE` | `Hjertesvikt (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_GBD_HEART_FAILURE` (7) |
| `QS_GBD_FALLS` | `GBD: Fallrisiko (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_GBD_FALLS` (9) |
| `QS_GBD_INR` | `INR og mål (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_GBD_INR` (4) |
| `QS_GBD_DIABETES_BASE` | `Basisdata (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_NDV_DIAGNOSE` |
| `QS_GBD_SMOKING` | `Røyking (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_SMOKING` = `3227` |
| `QS_NDV_BP` | `NDV: Blodtrykk (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_NDV_BP` = `3230, 3231` |
| `QS_NDV_SMOKING` | `NDV: Røyking (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_SMOKING` |
| `QS_NDV_ANTROPOMETRY` | `GBD: Høyde og vekt (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_HEIGHT_WEIGHT_BMI` |
| `QS_PUMPE_VARSET` | `Diabetes: CGM og Pumpetype (siste)` | `TVarSetCollector.CreateForEnum` | `SET_INSULINPUMPE` = `5166, 5162` |
| `QST_BDR_COMORBID` | `BDJ: Komorbiditet (siste)` | `TVarSetCollector.CreateForEnum` | `SET_BDR_COMORBID` (see §F.4) |
| `QS_BDR_DIAGNOSE` | `BDJ: Diagnose (siste)` | `TVarSetCollector.CreateForNumeric` | `SET_BDR_DIAGNOSE` = `3196, 3843` |
| `QS_BDR_DIAGNOSE_YEAR` | `Diagnoseår (siste)` *(literal, not a resourcestring)* | `TVarSetCollector.CreateForNumeric` | `SET_BDR_DIAGNOSE_YEAR` = `3486` |
| `QS_DRUG_C03` | `Medisiner: C03 - Diuretika` | `TDrugCollector.CreateBasic` | `C03%` |
| `QS_DRUG_C07` | `Medisiner: C07 - Betablokkere` | `TDrugCollector.CreateBasic` | `C07%` |
| `QS_DRUG_C08D` | `Medisiner: C08D - CCB med kardiale effekter` | `TDrugCollector.CreateBasic` | `C08D%` |
| `QS_DRUG_C09` | `Medisiner: C09 - Renin/Angiotensin systemet` | `TDrugCollector.CreateBasic` | `C09%` |
| `QS_DRUG_C10` | `Medisiner: C10 - Lipidsenkende` | `TDrugCollector.CreateBasic` | `C10%` |
| `QS_DRUG_M01A` | `Medisiner: M01A - NSAID` | `TDrugCollector.CreateBasic` | `M01A%` |
| `QST_LAB_NUTRITION` | `GBD: Ernæringsrelaterte labdata` | `TLabSetCollector` | `LABCLASSES_NUTRITION` = `22, 55, 83, 772, 1058` |
| `QST_LAB_NDV_CORE` | `NDV: Labdata` | `TLabSetCollector` | `LABCLASSES_DIABETES_NDV` (10) |
| `QST_LAB_BDR_CORE` | `BDJ: Labdata` | `TLabSetCollector` | `LABCLASSES_DIABETES_BDR` = `35, 497` |
| `QST_LAB_BDR_HBA1C` | `BDJ: HbA1c (siste)` | `TLabSetCollector` | inline `[1058]` |
| `QST_LAB_BDR_HBA1C_QUARTERS` | `BDJ: HbA1c siste 4 kvartaler` | `TCustomDataCollector` | `SpSnapshotLabQuarters(1058)` = `EXEC Report.ColLabQuarters 1058`, prefix `''` |
| `QS_FORMAGE_GBD_MAREVAN` | `Marevanskjema (siste)` | `TFormAgeCollector` | form `GBD_MAREVAN`; ⚠ registered under name `FORM.GBD_MAREVAN`, not `FORMAGE.GBD_MAREVAN` |
| `QS_FORMAGE_GBD_BARTHEL` | `Barthel (siste)` | `TFormAgeCollector` | form `BARTHEL` |
| `QS_FORMAGE_GBD_KDV` | `Demensvurdering (siste)` | `TFormAgeCollector` | form `KDV` |
| `QS_FORMAGE_GBD_FLACKERKIELY` | `Stratify (siste)` ⚠ *(shares `StrTitleFormStratify`)* | `TFormAgeCollector` | form `FLACKER_KIELY` |
| `QS_FORMAGE_GBD_BESLUTNINGER` | `Beslutninger (siste)` | `TFormAgeCollector` | form `GBD_BESLUTNINGER` |
| `QS_FORMAGE_GBD_MATKORT` | `Matkort (siste)` | `TFormAgeCollector` | form `GBD_MATKORTv2` |
| `QS_FORMAGE_GBD_HULTEN` | `Hultén (siste)` | `TFormAgeCollector` | form `HULTEN` |
| `QS_FORMAGE_GBD_LMG` | `Legemiddelgjennomgang (siste)` | `TFormAgeCollector` | form `LMG` |
| `QS_FORMAGE_GBD_NEWS2` | `NEWS2 (siste)` | `TFormAgeCollector` | form `NEWS2` |
| `QS_FORMAGE_GBD_QUALID` | `Livskvalitet (siste)` | `TFormAgeCollector` | form `QUALID` |
| `QS_FORMAGE_GBD_STRATIFY` | `Stratify (siste)` | `TFormAgeCollector` | form `STRATIFY` |

### A.12 Known name/title collisions in today's code

Port them **as bugs to fix**, and record the fix, because they affect saved packages
(`Report.QuickStat.DataElements` stores names) and `TryFindCollector`:

1. `QST_LAB_LOW` is constructed as `TLabLowTrustCollector.Create( QST_LAB_MEDIUM, StrTitleLabsetLow, ... )`
   → its `Name` is `LAB.TRUST2`, duplicating the medium-trust collector.
   **Fix:** use `QST_LAB_LOW` (`LAB.TRUST1`).
2. `QS_GBD_SBP_2M` is constructed as `TCustomDataCollector.Create( QS_GBD_WEIGHT_2M, StrTitleGbdSbp2m, ... )`
   → `Name` is `GBD.WEIGHT_2M`, duplicating the weight collector.
   **Fix:** use `QS_GBD_SBP_2M`.
3. `QS_FORMAGE_GBD_MAREVAN` is constructed with `QS_GBD_FORM_MAREVAN` as the name.
   **Fix:** use `QS_FORMAGE_GBD_MAREVAN`.
4. `QS_FORMAGE_GBD_FLACKERKIELY` uses `StrTitleFormStratify` → identical title to
   `QS_FORMAGE_GBD_STRATIFY`. **Fix:** add a `StrTitleFormFlackerKiely = 'Flacker-Kiely'`.
5. `QST_LAB_DIABETESw` (note the trailing `w`) in `EPR.QA.Collector.Names.pas` is a dead duplicate of
   `QST_LAB_DIABETES`. Drop it.
6. `StrTitleLabsetDiabetes = 'Labdata: Viktigste ved diabetes'` is declared in
   `QuickStat.Collectors.pas` and never used. Drop it.

---

## B. Collector class taxonomy

Every SQL string below is transcribed from the Delphi concatenations with the fragments resolved.
Delphi `Format` slots are shown as `%s` / `%d` / `%g`; `%%` in Delphi source has already been
reduced to a single `%`. Placeholders `{IdList}`, `{ItemList}`, `{LabList}`, `{FormName}` are
literal text in the built string and are substituted later (see §C).

Shared fragments (`EPR.QA.SQL.pas`) — **note the surrounding spaces, they matter**:

```pascal
SQL_COLLATION              = ' COLLATE Latin1_General_CI_AI ';
SQL_WHERE_PERSON_LIST      = 'WHERE ( PersonId IN {IdList} ) ';
SQL_AND_FAST               = ' AND ot.TreatType IN (''F'',''U'')';
SQL_AND_BEHOV              = ' AND ot.TreatType = ''B''';
SQL_JOIN_ATC_INDEX         = 'LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC ';
SQL_FROM_ONGOING_TREATMENT = 'FROM dbo.OngoingTreatment ot ';
```

### B.0 `TDataCollector` (abstract base) and `TCustomDataCollector`

```pascal
constructor TDataCollector.Create( const ACollectorName, ACaption: string;
                                   AFactory: TDataPointFactory; ASQL: ISQL; ALog: ILog );
// FMaxBatchSize := 1 (default), FVarPrefix := '' , FSQL := ''
```

```pascal
constructor TCustomDataCollector.Create( const AName, ATitle, AVarPrefix, ASQL: string;
                                         const AFactory: TDataPointFactory; ADb: ISQL; ALog: ILog );
begin
  inherited Create( AName, ATitle, AFactory, ADb, ALog );
  FSQL := ASQL;
  FVarPrefix := AVarPrefix;
  FMaxBatchSize := maxint;      // <- whole population in one query
end;
```

`TCustomDataCollector` is the workhorse: name, title, variable prefix and a finished SQL string are
all passed in. **34 of the 126 factory entries use it.** In C# it collapses to "a descriptor with a
literal/parameterised SQL string" — no subclass needed.

---

### B.1 Demographics collectors (`EPR.QA.Collector.Demographics.pas`)

`TDemographicsCollector` → `FVarPrefix := ''`, `FMaxBatchSize := 100`.

```sql
-- QRY_DEMOGRAPHICS, Format(..., [AVarName, AVarSpec])
SELECT PersonId,'%s' AS VarName, %s AS DpValue, GETDATE() AS VarDate, PersonId AS ResultId
FROM dbo.Person WHERE (PersonId IN {IdList})
```

| Class | `AVarName` | `AVarSpec` | Resulting column |
| --- | --- | --- | --- |
| `TAgeCollector` | `AGE` | `DATEDIFF(YYYY,DOB,GETDATE())` | `AGE` |
| `TYOBCollector` | `YOB` | `DATEPART(YYYY,DOB)` | `YOB` |
| `TYODCollector` | `YOD` | `DATEPART(YYYY,DeceasedDate)` | `YOD` |
| `TMOBCollector` | `MOB` | `DATEPART(MM,DOB)` | `MOB` |
| `TGenderCollector` | `SEX` | `GenderId` | `SEX` |
| `TPostCodeCollector` | `ZIP` (literal, not `VAR_ZIP`) | `CONVERT(INTEGER,PostalCode)` | `ZIP` |

`TGlobalCollector` → `FMaxBatchSize := maxint`; its five subclasses **override `function SQL`**
rather than setting `FSQL`, because the SQL depends on `fStudyId`, which is only known at
`RunBatch(AStudyId)` time. Port note: in C# the SQL builder must receive the study id.

```sql
-- SpStudCaseFields( AVarName, AFieldName, AStudyId )
SELECT sc.PersonId, %s AS VarName, sc.%s AS DpValue, GETDATE(), sc.StudCaseId AS RowId
FROM dbo.StudCase sc WHERE sc.StudyId = %d
```
* `TStatusCollector` → `SpStudCaseFields('StatusId','FinState',studyId)`
* `TGroupCollector`  → `SpStudCaseFields('GroupId','GroupId',studyId)`

```sql
-- SpStudyCenter( AStudyId )
SELECT sc.PersonId, 'CenterId' AS VarName, sg.CenterId AS DpValue, GETDATE(), sc.StudCaseId AS RowId
FROM dbo.StudCase sc
JOIN dbo.StudyGroup sg ON sg.StudyId = sc.StudyId AND sg.GroupId = sc.GroupId
WHERE sc.StudyId = %d
```

```sql
-- SpStudyGroupDeath( AStudyId )
SELECT PersonId, 'DEATH_GROUP' AS VarName, NewGroupId AS DpValue, DeceasedDate AS VarDate, StudCaseLogId AS RowId
FROM
(
  SELECT p.PersonId, p.DeceasedDate, scl.NewGroupId, scl.StudCaseLogId,
  ROW_NUMBER() OVER (PARTITION BY scl.StudCaseId ORDER BY scl.StudCaseLogId desc ) AS ReverseOrder
  FROM dbo.Person p
  JOIN dbo.StudCase sc ON sc.PersonId = p.PersonId AND sc.StudyId = %d
  JOIN dbo.StudCaseLog scl ON scl.StudCaseId = sc.StudCaseId AND scl.ChangedAt < p.DeceasedDate
  LEFT JOIN dbo.StudyGroup sg ON sg.StudyId = sc.StudyId AND sg.GroupId = scl.NewGroupId
  WHERE NOT DeceasedDate IS NULL
) agg
WHERE agg.ReverseOrder = 1
```

```sql
-- SpStudyCenterDeath( AStudyId )
SELECT PersonId, 'DEATH_CENTER' AS VarName, CenterId AS DpValue, DeceasedDate AS VarDate, StudCaseLogId AS RowId
FROM
(
  SELECT p.PersonId, p.DeceasedDate, sg.CenterId, scl.StudCaseLogId,
  ROW_NUMBER() OVER (PARTITION BY scl.StudCaseId ORDER BY scl.StudCaseLogId desc ) AS ReverseOrder
  FROM dbo.Person p
  JOIN dbo.StudCase sc ON sc.PersonId = p.PersonId AND sc.StudyId = %d
  JOIN dbo.StudCaseLog scl ON scl.StudCaseId = sc.StudCaseId AND scl.ChangedAt < p.DeceasedDate
  LEFT JOIN dbo.StudyGroup sg ON sg.StudyId = sc.StudyId AND sg.GroupId = scl.NewGroupId
  WHERE NOT DeceasedDate IS NULL
) agg
WHERE agg.ReverseOrder = 1
```

> Both "at death" queries `LEFT JOIN dbo.StudyGroup sg` but `SpStudyGroupDeath` never uses `sg`.
> Harmless; keep for fidelity or drop — no behavioural change.

---

### B.2 `TFormInstanceCollector` (`EPR.QA.Collector.Standard.pas`)

```pascal
procedure TFormInstanceCollector.AfterConstruction;
begin
  inherited;
  FVarPrefix := PREFIX_FORM;                  // 'FORM.'
  FSQL := QRY_FORM_INSTANCES;                 // 'EXEC Report.GetFormInstances :PersonId'
end;                                          // FMaxBatchSize stays 1
```

* **One round trip per patient.** `RunBatch` takes the `SinglePatient` path:
  `FDB.FastQuery( SQL, [FLastId] )`.
* Result set is whatever `Report.GetFormInstances` returns; it must satisfy the 5-column contract.
* Columns become `FORM.<FormName>`; the value is the instance count per form type.

**Port recommendation:** keep the stored-procedure call but batch it. Either add an overload of the
proc taking a TVP, or wrap: `SELECT ... FROM <TVP> CROSS APPLY ...`. As a minimum, run the N calls
concurrently on a bounded degree of parallelism — today this is the single slowest collector on a
large population.

### B.3 `TFormDataCollector`

```pascal
constructor TFormDataCollector.Create( const ACollectorName, ATitle, AFormName: string; ... );
begin
  inherited Create( PREFIX_FORM + ACollectorName, ATitle, AFactory, ADb, ALog );
  FVarPrefix := Format( '%s.', [AFormName] );
  FSQL := Format( QRY_FORM_DATA, [QuotedStr( AFormName )] );
end;
```
with
```
QRY_FORM_DATA = 'EXEC Report.GetFormData :PersonId, %s'
```
→ e.g. `EXEC Report.GetFormData :PersonId, 'BARTHEL'`. `FMaxBatchSize` stays **1**.

Columns become `<FormName>.<VarName>`.

> This is the collector the tarmscreening branch replaced with a batched, set-based query
> (`SpSnapshotFormDataAll`, batch 200) — see §F.1. On a study with 30 form classes and 2 000
> patients, today's code issues **60 000** stored-procedure calls.

### B.4 `TFormDataNumericCollector` — declared, never instantiated

```pascal
constructor TFormDataNumericCollector.Create( ... );
begin
  inherited Create( PREFIX_FORM + ACollectorName, ATitle, AFactory, ADb, ALog );
  FVarPrefix := Format( '%s.', [AFormName] );
  FSQL := SpSnapshotFormDataNumeric( AFormName );
  fMaxBatchSize := 100;
end;
```

```sql
-- SpSnapshotFormDataNumeric( AFormName ), Format(..., [QuotedStr(AFormName)])
SELECT agg.* FROM
(
  SELECT ce.PersonId, mi.VarName, ISNULL(dp.Quantity,DATEDIFF(DD,'1899-12-30',dp.DTVal)) AS DataValue, ce.EventTime, dp.RowId,
    RANK() OVER ( PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC ) AS OrderBy
  FROM dbo.ClinDatapoint dp
    JOIN dbo.ClinEvent ce ON ce.EventId = dp.EventId
    JOIN dbo.ClinForm cf ON cf.EventId = ce.EventId
    JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId
    JOIN dbo.MetaItem mi ON mi.ItemId = dp.ItemId AND mi.ItemType IN (1,2,5)
    JOIN dbo.MetaFormItem mfi ON mfi.FormId = cf.FormId AND mfi.ItemId = mi.ItemId
  WHERE ( mf.FormName = %s )
  AND ( ce.PersonId IN {IdList} )
) agg
WHERE agg.OrderBy = 1
```

Only `TFormDataNumericCollector` references it, and nothing references
`TFormDataNumericCollector` → dead in the shipping app. Port it anyway; it is the natural
replacement for `Report.GetFormData` if you want batching without recovering `SpSnapshotFormDataAll`.

---

### B.5 `TFormAgeCollector` (`EPR.QA.Collector.VarSet.pas`)

```pascal
constructor TFormAgeCollector.Create( const ACollectorName, ATitle: string; const AFormName: string; ... );
begin
  inherited Create( ACollectorName, ATitle + TXT_LAST, AFactory, ADb, ALog );   // ' (siste)'
  FVarPrefix := FORM_AGE_PREFIX;                                               // 'FORMAGE.'
  FMaxBatchSize := 100;
  fSQL := StringReplace( SpFormAgeSingle, FORM_NAME_PLACEHOLDER, QuotedStr( AFormName ), [] );
end;
```

```sql
-- SpFormAgeSingle;  {FormName} is replaced with a quoted literal, NOT a parameter
SELECT a.* FROM (
  SELECT ce.PersonId, mf.FormName AS VarName, DATEDIFF(dd,ce.EventTime,GETDATE()) AS DpValue, ce.EventTime AS VarDate, cf.ClinFormId,
  RANK() OVER (PARTITION BY ce.PersonId,mf.FormName ORDER BY ce.EventTime DESC ) AS OrderBy
  FROM dbo.ClinForm cf
  JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId
  JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId AND mf.FormName =  {FormName}
  WHERE ( ce.PersonId IN {IdList} ) AND ( cf.DeletedAt IS NULL )
 ) a
 WHERE a.OrderBy = 1
```

Value = **age in days** of the most recent instance of that form. Column = `FORMAGE.<FormName>`.

> `StringReplace(..., [])` — no `rfReplaceAll` — but `{FormName}` occurs once, so it is fine.
> `mf.FormName =  {FormName}` has a double space; preserve or normalise, no behavioural effect.

### B.6 `TVarSetCollector` and friends

```pascal
constructor TVarSetCollector.Create( ... );   // strict private; called by all four public ctors
begin
  inherited Create( ACollectorName, ATitle + TXT_LAST, AFactory, ADb, ALog );
  FVarPrefix := EmptyStr;
  FMaxBatchSize := 100;
end;
```

> **Order matters and is unusual:** the four public constructors assign `fSQL` *before* calling
> `Create`, e.g. `CreateForNumeric` does `fSQL := SpSnapshotVarset( itNumeric, AItemList ); Create(...)`.
> `Create` does not clear `fSQL`. In C# just build the SQL first and pass it in.

Variants:

| Constructor | SQL builder | `TCrfVarType` |
| --- | --- | --- |
| `CreateForNumeric` | `SpSnapshotVarset(itNumeric, ids)` | quantities & enums via `Quantity` |
| `CreateForDate` | `SpSnapshotVarset(itDate, ids)` | Excel serial date |
| `CreateForText` | `SpSnapshotVarset(itText, ids)` | `DATALENGTH(TextVal)` |
| `CreateForEnum` | `SpSnapshotEnum(ids)` | `EnumVal`, adds `Caption` from `MetaItemAnswer.ShortCode` |

```sql
-- SpSnapshotVarset( AVariableDataType, AItemIds )
-- Format(QRY_VARSET, [valueFragment, qualifyFragment]) then {ItemList} <- '3224, 3225, 3310'
SELECT a.* FROM (
  SELECT ce.PersonId, mi.VarName, %s AS DpValue, ce.EventTime AS VarDate, cdp.RowId, mi.ItemId,
  RANK() OVER (PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC ) AS OrderBy
  FROM dbo.ClinDataPoint cdp
  JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId
  JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId
  WHERE ( ce.PersonId IN {IdList} )
    AND ( %s )
    AND ( cdp.ItemId IN ( {ItemList} ) )
 ) a
 WHERE a.OrderBy = 1
ORDER BY PersonId
```

| `TCrfVarType` | `valueFragment` (`%s` #1) | `qualifyFragment` (`%s` #2) |
| --- | --- | --- |
| `itNumeric` | `cdp.Quantity` | `ISNULL(cdp.Quantity,-1) <> -1` |
| `itDate` | `DATEDIFF(DD,'1899-12-30',cdp.DTVal)` | `NOT cdp.DTVal IS NULL` |
| `itText` | `DATALENGTH(cdp.TextVal)` | `NOT cdp.TextVal IS NULL` |

> `itNumeric` deliberately discards the value `-1` **and** NULL — a real quantity of exactly `-1`
> is dropped. Reproduce it; do not "fix" it silently.

```sql
-- SpSnapshotEnum( AItemIds );  {ItemList} substituted the same way
SELECT a.* FROM (
  SELECT ce.PersonId, mi.VarName, cdp.EnumVal AS DpValue, ce.EventTime AS VarDate, cdp.RowId, mi.ItemId, mia.ShortCode AS Caption,
  RANK() OVER (PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC ) AS OrderBy
  FROM dbo.ClinDataPoint cdp
  JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId
  JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId
  LEFT JOIN dbo.MetaItemAnswer mia ON mia.ItemId = cdp.ItemId AND mia.OrderNumber = cdp.EnumVal
  WHERE ( ce.PersonId IN {IdList} )
    AND ( ISNULL(cdp.EnumVal,-1) >= 0  )
    AND ( cdp.ItemId IN ( {ItemList} ) )
 ) a
 WHERE ( a.OrderBy = 1 )
ORDER BY PersonId
```

`TVarSetAgeCollector` — prefix `ITEMAGE.`, title `+ ' (siste)'`, batch 100:

```sql
-- SpSnapshotVarsetAge( AItemIds )
SELECT a.* FROM
(
  SELECT ce.PersonId, mi.VarName, DATEDIFF(dd,ce.EventTime,GETDATE()) AS DpValue, ce.EventTime AS VarDate, cdp.RowId, mi.ItemId,
  RANK() OVER (PARTITION BY ce.PersonId,mi.ItemId ORDER BY ce.EventTime DESC ) AS OrderBy
  FROM dbo.ClinDataPoint cdp
  JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId
  JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId
  WHERE ( ce.PersonId IN {IdList} )
    AND NOT ( cdp.Quantity IS NULL AND cdp.DTVal IS NULL AND cdp.TextVal IS NULL )
    AND ( cdp.ItemId IN ( {ItemList} ) )
 ) a
 WHERE a.OrderBy = 1
```

`TVarSetMaxCollector` — prefix `ITEMMAX.`, title `+ ' (høyeste)'`, batch 100:

```sql
-- SpMaximumQuantityVarset( AItemIds )
 SELECT a.* FROM
(
  select ce.PersonId, mi.VarName, cdp.Quantity AS DpValue, ce.EventTime AS VarDate, cdp.RowId, cdp.ItemId,
  RANK() OVER ( PARTITION BY ce.PersonId, cdp.ItemId ORDER BY Quantity DESC, cdp.RowId DESC ) AS rnk
  FROM dbo.ClinDataPoint cdp
  JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId
  JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId
  WHERE ( ce.PersonId IN {IdList} )
    AND ( cdp.ItemId IN ( {ItemList} ) )
 ) a
where a.rnk = 1
```

### B.7 Lab collectors (`EPR.QA.Collector.Labdata.pas`)

```pascal
constructor TLabSetCollector.Create( const ACollectorName, AGroupName: string;
                                     const ALabClassSet: TLabClassSet; ... );
begin
  if pos( ':', AGroupName ) = 0 then
    labSetTitle := Format( StrTitleLabsetTemplate, [AGroupName] )   // 'Labdata: %s (siste)'
  else
    labSetTitle := AGroupName;
  inherited Create( ACollectorName, labSetTitle, AFactory, ADb, ALog );
  FVarPrefix := PREFIX_LAB_VARIABLE;      // '' — lab columns are unprefixed
  FSQL := SpSnapshotLabset( ALabClassSet );
  FMaxBatchSize := 100;
end;
```

```sql
-- SpSnapshotLabset( ALabClassIds );  {LabList} <- '22, 29, 30, ...'
SELECT agg.* FROM
(
  SELECT ld.PersonId, ISNULL(la.NLK, Report.LabClassName(lc.LabClassId)) AS VarName, ld.NumResult, ld.LabDate, ld.ResultId,
  RANK() OVER ( PARTITION BY ld.PersonId,lc.LabClassId ORDER BY ld.LabDate DESC ) AS OrderBy
  FROM dbo.LabData ld
  JOIN dbo.LabCode lc ON lc.LabCodeId = ld.LabCodeId
  JOIN dbo.LabClass la ON la.LabClassId = lc.LabClassId
  WHERE ( ld.PersonId IN {IdList} ) AND ( la.LabClassId IN ({LabList}) AND ( ld.NumResult >= 0 ) )
 ) agg
 WHERE agg.OrderBy = 1 ORDER BY agg.PersonId, agg.VarName
```

Column name is the **NLK code** (Norwegian lab code, e.g. `NPU01566`) or, when absent, the scalar
function `Report.LabClassName(id)`. This is why the datapoint-class registrations in
`RegisterCustomDatapoints` key on `NPU…` / `NOR…` codes.

`CreateOldSchool` converts a Delphi `TLabSet` (set of `TLabTest`) to an ordinal array:

```pascal
constructor TLabSetCollector.CreateOldSchool( const ACollectorName, ATitle: string; const ALabSet: TLabSet; ... );
begin
  SetLength( labSet, 1024 );
  setIndex := 0;
  for lt in ALabSet do begin labSet[setIndex] := ord( lt ); inc( setIndex ); end;
  SetLength( labSet, setIndex );
  Create( ACollectorName, ATitle, labSet, AFactory, ADb, ALog );
end;
```

`LABSET_KIDNEY = [ltUrate, ltUrea, ltEstGFR, ltCreatinine, ltNatrium, ltKalium] + LABSET_URINE`
where `LABSET_URINE = [ltDUAlbumin, ltUAlbumin, ltUMicroAlbumin, ltACRatio, ltDUProtein]`.
Resolved against the `TLabTest` declaration order in `VMR.Lab.Interfaces.pas`, the ordinals are
**ascending** (a Delphi `for..in` over a set yields ordinals in ascending order):

> **Corrected 2026-08-26 (step 2.4).** An earlier revision of this table had the kidney group one
> too high and therefore emitted the wrong id list. The cause is a **typo in the Delphi enum**:
> the coagulation member is `lFibrinogen`, not `ltFibrinogen`. Any extraction that filters members
> on an `lt` prefix silently drops it and shifts every later ordinal down by one. Recomputed from
> `C:\work\FastTrak-tarmscreening\VMR\VMR.Lab.Interfaces.pas` at `249ac2d16` (176 members, no
> explicit `=` assignments, so plain 0-based ordinals), and cross-checked twice: `LABCLASSES_KIDNEY`
> carries 49/50/53/54 for the same analytes, and `QuickStat.Collectors.pas:206-209` registers 51
> and 52 as "eGFR Cockgroft-Gault" and "eGFR MDRD".

| `TLabTest` | ordinal |
| --- | --- |
| `ltDUProtein` | 3 |
| `ltUAlbumin` | 4 |
| `ltUMicroAlbumin` | 5 |
| `ltACRatio` | 6 |
| `ltDUAlbumin` | 7 |
| `ltCreatinine` | 49 |
| `ltEstGFR` | 50 |
| `ltUrate` | 53 |
| `ltUrea` | 54 |
| `ltNatrium` | 90 |
| `ltKalium` | 91 |

→ `LAB.KIDNEY` emits `... la.LabClassId IN (3, 4, 5, 6, 7, 49, 50, 53, 54, 90, 91)`.

**Port recommendation:** do not port `TLabSet`/`TLabTest` as a C# enum-set. Replace the single
`CreateOldSchool` call with the literal id array above (and keep `LABCLASSES_KIDNEY` — note it is a
*different*, unused set: `[3,4,5,6,7,53,54,50,49,90,91,995,1075]`). Cross-check the ordinals against
the target database's `dbo.LabClass` before shipping; the `TLabTest` enum is a hard-coded mirror of
that table and drifts.

Trust-level collectors — `FVarPrefix := ''`, `FMaxBatchSize := 100`, title verbatim:

```sql
-- SpSnapshotLabdataByTrustLevel( ATrustLevel )   3=high, 2=medium, 1=low
SELECT a.* FROM
(
   SELECT ld.PersonId, ISNULL(la.NLK, Report.LabClassName( la.LabClassId)) AS VarName, ld.NumResult, ld.LabDate, ld.ResultId,
   RANK() OVER ( PARTITION BY PersonId ORDER BY LabDate DESC ) AS OrderBy
   FROM dbo.LabData ld
     JOIN dbo.LabCode lc ON lc.LabCodeId = ld.LabCodeId
     JOIN dbo.LabClass la ON la.LabClassId = lc.LabClassId
   WHERE ( la.TrustLevel = %d )  AND ( ld.PersonId IN {IdList} )
) a
WHERE a.OrderBy = 1
```

> ⚠ Behavioural quirk: the `RANK()` partitions by **`PersonId` only** (not by lab class), so this
> returns only the single most recent lab row per patient — plus ties on the same `LabDate`. It
> almost certainly *intends* `PARTITION BY ld.PersonId, la.LabClassId` like `SpSnapshotLabset` does.
> Port as-is (it is the shipping behaviour) and raise it as a question.

Lab counts:

```sql
-- SpRecentLabdataPresent( AMonthsAgo ); %0:d used twice
SELECT PersonId,'LABCOUNT%0:dM' AS VarName, COUNT(*) AS n, MAX(LabDate) AS MaxLabDate, MAX(ResultId) AS MaxResultId
FROM LabData
WHERE DATEDIFF(MM,LabDate,GETDATE()) < %0:d
GROUP BY PersonId
```
No `{IdList}` — scans all patients (see §C.2). Columns: `LABCOUNT3M`, `LABCOUNT6M`, …

```sql
-- SpSnapshotLabQuarters( ALabClassId ) -> QRY_LAB_QUARTERS, ProcId 9000
EXEC Report.ColLabQuarters %d
```

### B.8 `TDiagnoseCollector` / `TDementiaCollector`

```pascal
constructor TDiagnoseCollector.Create( const ATitle, ADxPattern: string; ... );
begin
  fDxPattern := ADxPattern;
  inherited Create( PREFIX_DIAGNOSE_COLLECTOR + ConvertAtcPatternToVariableName( ADxPattern ),
                    ATitle, AFactory, ADb, ALog );
  fSQL := SpDiagnoseByPattern( fDxPattern );
  FMaxBatchSize := maxint;
  FVarPrefix := PREFIX_DIAGNOSE_COLLECTOR;   // 'DX.'
end;
```

```sql
-- SpDiagnoseByPattern( APattern );  Format(..., [QuotedStr(varName), QuotedStr(APattern)])
SELECT PersonId, VarName, ListItem, CreatedAt, ProbId, Caption
FROM (
  SELECT cp.PersonId, %s AS VarName, cp.ListItem, cp.CreatedAt, cp.ProbId, mni.ItemCode AS Caption,
  RANK() OVER ( PARTITION BY cp.PersonId ORDER BY cp.CreatedAt ) AS OrderNo
  FROM dbo.ClinProblem cp
  JOIN dbo.MetaProblemType pt ON pt.ProbType = cp.ProbType AND pt.ProbActive = 1
  JOIN dbo.MetaNomListItem mnli ON mnli.ListItem = cp.ListItem
  JOIN dbo.MetaNomItem mni ON mni.ItemId = mnli.ItemId
  WHERE ( mni.ItemCode LIKE %s )
) agg WHERE OrderNo = 1 
```

The **value** (`Fields[2]`) is `cp.ListItem` — a nomenclature list-item id, not a count and not a
0/1 flag. `Caption` is the ICD-10 code. Earliest diagnosis wins (`ORDER BY cp.CreatedAt` ascending).
No `{IdList}`.

```sql
-- SpDiagnoseDementiaAndAlzheimers
SELECT PersonId, VarName, ListItem, CreatedAt, ProbId, Caption
FROM (
  SELECT cp.PersonId, 'DEMENTIA' AS VarName, cp.ListItem, cp.CreatedAt, cp.ProbId, mni.ItemCode AS Caption,
  RANK() OVER ( PARTITION BY cp.PersonId ORDER BY cp.CreatedAt ) AS OrderNo
  FROM dbo.ClinProblem cp
  JOIN dbo.MetaProblemType pt ON pt.ProbType = cp.ProbType AND pt.ProbActive = 1
  JOIN dbo.MetaNomListItem mnli ON mnli.ListItem = cp.ListItem
  JOIN dbo.MetaNomItem mni ON mni.ItemId = mnli.ItemId
  AND ( mni.ItemCode LIKE 'F0[0123]%' OR mni.ItemCode LIKE 'G30%' )
) agg WHERE OrderNo = 1 
```

> Two discrepancies worth recording: the ICD filter is an extra `AND` on the **join** rather than a
> `WHERE` (same result for an inner join), and the title says `F0[123]+G03` while the SQL matches
> `F0[0123]` and `G30`. The SQL is right (G30 = Alzheimer); the title is wrong.

```sql
-- SpDiagnoseDetailsByLevel( ALevel );  %0:d used twice.  prefix 'DXC.'
SELECT PersonId, SUBSTRING(ItemCode,1,%0:d) AS VarName, COUNT(*) AS DpValue, MIN(CreatedAt) AS MinCreatedAt, MIN(ProbId) AS MinProbId FROM 
(
  SELECT PersonId, mni.ItemCode, cp.ListItem, cp.CreatedAt, cp.ProbId
  FROM dbo.ClinProblem cp 
  JOIN dbo.MetaProblemType mp ON mp.ProbType = cp.ProbType AND mp.ProbActive = 1
  JOIN dbo.MetaNomListItem li ON li.ListItem = cp.ListItem
  JOIN dbo.MetaNomItem mni ON mni.ItemId = li.ItemId
) pro
GROUP BY PersonId, SUBSTRING(ItemCode,1,%0:d) 
```

Levels 1–5 produce columns `DXC.E`, `DXC.E1`, `DXC.E11`, `DXC.E110`, `DXC.E1100` — the value is the
**count** of problems with that code prefix. Level 1 alone can add ~22 columns; level 5 can add
hundreds. This is the widest collector in the app.

```sql
-- SpDrugWithoutDiagnose( AVarName, ADrugPattern, ADxPattern );  prefix 'RXDX.'
SELECT rx.PersonId, %s AS VarName, rxn AS DpValue, MaxCreatedAt, MaxTreatId FROM (
   SELECT PersonId, MAX(CreatedAt) AS MaxCreatedAt, MAX(TreatId) AS MaxTreatId, COUNT(*) AS rxn
   FROM dbo.OngoingTreatment
   WHERE ATC LIKE %s
   GROUP BY PersonId
) rx
LEFT JOIN
  (
    SELECT PersonId, COUNT(*) AS n FROM Diagnose.ICD10
    WHERE ItemCode LIKE %s AND ProbActive = 1
    GROUP BY PersonId
  ) agg ON agg.PersonId = rx.PersonId
WHERE ( agg.n IS NULL )ORDER BY PersonId
```
Called as `SpDrugWithoutDiagnose('A10_NOT_E1x01234', 'A10%', 'E1[01234]%')` → column
`RXDX.A10_NOT_E1x01234`. **External dependency:** the view/table `Diagnose.ICD10`.

### B.9 `TDrugCollector`

```pascal
constructor TDrugCollector.CreateBasic( const ATitle, AMatchPatternAtc: string; ... );
var collectorNamePrefix: string;
begin
  case fTreatTypeFilter of
    ttLongTerm: collectorNamePrefix := PREFIX_DRUGFAST_COLLECTOR;   // 'DRUGFAST.'
    ttAsNeeded: collectorNamePrefix := PREFIX_DRUGNEED_COLLECTOR;   // 'DRUGNEED.'
  else          collectorNamePrefix := PREFIX_DRUG_COLLECTOR;       // 'DRUG.'
  end;
  inherited Create( collectorNamePrefix + ConvertAtcPatternToVariableName( AMatchPatternAtc ),
                    ATitle, AFactory, ADb, ALog );
  fAtcPattern := AMatchPatternAtc;
end;

constructor TDrugCollector.CreateChecksum( const ATitle, AMatchPatternAtc: string; ... );
begin
  fUseNameChecksumForDatapoint := true;      // must be set BEFORE CreateBasic
  CreateBasic( ATitle, AMatchPatternAtc, AFactory, ADb, ALog );
end;

constructor TDrugCollector.CreateForTreatType( const ATitle, AMatchPatternAtc: string;
                                               const ATreatType: TTreatTypeFilter; ... );
begin
  fUseNameChecksumForDatapoint := true;
  fTreatTypeFilter := ATreatType;            // read by CreateBasic to pick the name prefix
  CreateBasic( ATitle, AMatchPatternAtc, AFactory, ADb, ALog );
end;

procedure TDrugCollector.AfterConstruction;
begin
  inherited;
  FVarPrefix := VAR_PREFIX_DRUG;                                 // 'ATC_'
  if fUseNameChecksumForDatapoint then sqlTemplate := QRY_DRUGSET_CHECKSUM
  else                                 sqlTemplate := QRY_DRUGSET_BASIC;
  if GroupResults then sqlTemplate := StringReplace( sqlTemplate, VAR_TEMPLATE, VAR_TEMPLATE_GROUP, [rfReplaceAll] )
  else                 sqlTemplate := StringReplace( sqlTemplate, VAR_TEMPLATE, VAR_TEMPLATE_SPLIT, [rfReplaceAll] );
  fSQL := Format( sqlTemplate, [QuotedStr( ConvertAtcPatternToVariableName( fAtcPattern ) ),
                                QuotedStr( fAtcPattern )] );
  case fTreatTypeFilter of
    ttAnyTreatType:;
    ttLongTerm: fSQL := fSQL + SQL_AND_FAST;     // " AND ot.TreatType IN ('F','U')"
    ttAsNeeded: fSQL := fSQL + SQL_AND_BEHOV;    // " AND ot.TreatType = 'B'"
  end;
  FMaxBatchSize := 100;
end;
```

with

```pascal
VAR_TEMPLATE       = '{VarTemplate}';
VAR_TEMPLATE_GROUP = '%s';                                  // when GroupResults = True
VAR_TEMPLATE_SPLIT = 'CONCAT(%s,''.'',ot.TreatType)';       // default (class var is False)
```

Resolved templates (`GroupResults = False`, the shipping default):

```sql
-- QRY_DRUGSET_BASIC, used by CreateBasic
SELECT ot.PersonId, CONCAT(%s,'.',ot.TreatType) AS VarName, 1 AS DpValue, ot.StartAt, ot.TreatId, ai.AtcName AS Caption FROM dbo.OngoingTreatment ot LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC WHERE ( PersonId IN {IdList} ) AND ot.ATC COLLATE Latin1_General_CI_AI LIKE %s COLLATE Latin1_General_CI_AI 
```

```sql
-- QRY_DRUGSET_CHECKSUM, used by CreateChecksum / CreateForTreatType
SELECT ot.PersonId, CONCAT(%s,'.',ot.TreatType) AS VarName, ABS(CHECKSUM(ot.DrugName)) % 100000 AS DpValue, ot.StartAt, ot.TreatId, ai.AtcName AS Caption FROM dbo.OngoingTreatment ot LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC WHERE ( PersonId IN {IdList} ) AND ot.ATC COLLATE Latin1_General_CI_AI LIKE %s COLLATE Latin1_General_CI_AI 
```

`%s` #1 = `QuotedStr(ConvertAtcPatternToVariableName(pattern))`, `%s` #2 = `QuotedStr(pattern)`.
Example for `A10%`: `CONCAT('A10','.',ot.TreatType)` and `LIKE 'A10%'` → columns
`ATC_A10.F`, `ATC_A10.B`, `ATC_A10.U`, …

**Why the checksum?** `ABS(CHECKSUM(DrugName)) % 100000` produces a pseudo-numeric "which drug"
value that is stable per drug name and fits a `double` cell. It is not a count and not meaningful
numerically — it exists so distinct drugs render as distinct values. The human-readable name is in
`Caption` (`ai.AtcName`). **Do not "improve" this** — downstream exports depend on it.

**Why `COLLATE Latin1_General_CI_AI` on both sides?** The comment in `EPR.QA.SQL.pas` says:
*"Collation issues can be dangerous when matching ATCs with LIKE because 'A%' will not match 'AA'."*
Keep it verbatim.

Hand-written drug SQL (`EPR.QA.Collector.Drug.pas`) — all consumed by `TCustomDataCollector`:

```sql
-- QRY_NORGEP
EXEC Report.NorGeP
```

```sql
-- QRY_DRUGCOUNT_BY_TYPE, prefix 'TREATn_'
SELECT PersonId, TreatType, COUNT(*) AS DpValue, MAX(CreatedAt) AS LastDate, Max(TreatId) AS MaxTreatId FROM dbo.OngoingTreatment ot WHERE DATALENGTH(ATC) > 4 GROUP BY PersonId, TreatType
```

```sql
-- QRY_DRUGSET_ANTICHOLIN_AB, prefix 'DRUG_'
SELECT PersonId, 'ANTICHOLIN_AB' AS VarName, ABS(CHECKSUM(DrugName)) % 100000 AS DpValue, StartAt, TreatId, Caption
FROM
(
  SELECT ot.PersonId, ot.DrugName, ot.StartAt, ot.TreatId, ai.AtcName AS Caption,
    RANK() OVER ( PARTITION BY ot.PersonId ORDER BY ac.AlertLevel, ot.StartAt DESC ) AS ReverseOrder
FROM dbo.OngoingTreatment ot   JOIN dbo.KBAnticholinDrug ac ON ac.ATC = ot.ATC AND ac.AlertLevel IN ( 'A','B') LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC ) agg WHERE ( PersonId IN {IdList} ) AND ( ReverseOrder = 1 )
```
**External dependency:** `dbo.KBAnticholinDrug`.

```sql
-- QRY_DRUGSET_ANTICHOLIN_N05A, prefix 'DRUG_'
SELECT PersonId, 'ANTICHOLIN_N05' AS VarName, ABS(CHECKSUM(DrugName)) % 100000 AS DpValue, StartAt, TreatId, ai.AtcName AS Caption FROM dbo.OngoingTreatment ot LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC WHERE ( PersonId IN {IdList} ) AND ( ot.ATC  COLLATE Latin1_General_CI_AI  LIKE 'N05A%'  COLLATE Latin1_General_CI_AI  ) AND NOT ( ( ot.ATC COLLATE Latin1_General_CI_AI LIKE 'N05AH0[34]' COLLATE Latin1_General_CI_AI ) OR ( ot.ATC COLLATE Latin1_General_CI_AI LIKE 'N05AN%' COLLATE Latin1_General_CI_AI ) )
```

```sql
-- QRY_DRUGSET_METFORMIN, prefix 'DRUG_'
SELECT PersonId, 'METFORMIN' AS VarName, ABS(CHECKSUM(DrugName)) % 100000 AS DpValue, StartAt, TreatId, Caption
FROM
(
  SELECT ot.PersonId, ot.DrugName, ot.StartAt, ot.TreatId, ai.AtcName AS Caption,
    RANK() OVER ( PARTITION BY ot.PersonId ORDER BY ot.StartAt DESC ) AS ReverseOrder
FROM dbo.OngoingTreatment ot   JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC AND ai.AtcName LIKE '%METFORMIN%' ) agg WHERE ( PersonId IN {IdList} ) AND ( ReverseOrder = 1 )
```

```sql
-- QRY_DRUGCOUNT_BY_ATCGROUP, prefix 'ATCn_'  (four UNIONed levels of ATC truncation)
SELECT PersonId, ATC, COUNT(*) AS n, MAX(StartAt) AS LastDate, MAX(TreatId) AS MaxTreatId FROM dbo.OngoingTreatment ot WHERE ATC IN ('J01XX04','M04AC01','N05CM02' )GROUP BY PersonId, ATC
UNION SELECT PersonId, SUBSTRING(ATC,1,5) AS ATCFragment, COUNT(*) AS n, MAX(StartAt) AS LastDate, MAX(TreatId) AS MaxTreatId FROM dbo.OngoingTreatment ot WHERE SUBSTRING(ATC,1,5) IN ('A10BA','B01AE','B01AF','B03BA','B03BB','G04BD','M04AA','M04AB','N02AA','N02AB','N02AE','N02AG','N02AJ','N02AX','N02BA','N05BA','N05BB','N05CD','N05CF','N05CH','N06DA','N06DX','R03AC','R03AK','R03BB','R03DA','R06AD','R06AE','R06AX' )GROUP BY PersonId, SUBSTRING(ATC,1,5)
UNION SELECT PersonId, SUBSTRING(ATC,1,4) AS ATCFragment, COUNT(*) AS n, MAX(StartAt) AS LastDate, MAX(TreatId) AS MaxTreatId FROM dbo.OngoingTreatment ot WHERE SUBSTRING(ATC,1,4) IN ('A02A','A02B','A06A','A10A','A10B','B01A','B01C','B03A','C01A','G04C','H03A','M01A','N03A','N05A','N06A','N06D','S01E' )GROUP BY PersonId, SUBSTRING(ATC,1,4)
UNION SELECT PersonId, SUBSTRING(ATC,1,3) AS ATCFragment, COUNT(*) AS n, MAX(StartAt) AS LastDate, MAX(TreatId) AS MaxTreatId FROM dbo.OngoingTreatment ot WHERE SUBSTRING(ATC,1,3) IN ('A11','C02','C03','C07','C08','C09','H02','N04' )GROUP BY PersonId, SUBSTRING(ATC,1,3)
ORDER BY PersonId
```

```sql
-- SpDrugCountNoAtc, prefix 'ATCn_'
SELECT PersonId, 'NOATC', COUNT(*) AS DpValue, MAX(StartAt) AS LastDate, MAX(TreatId) AS MaxTreatId FROM dbo.OngoingTreatment ot WHERE ISNULL(ATC,'') = ''GROUP BY PersonId
```
> ⚠ `Fields[1]` here is an **unaliased** literal, so ADO names the column `Expr1001` or similar.
> Position 1 is still the literal `'NOATC'`, so `RunBatch` works. In C# give it an alias
> (`AS VarName`) — that is safe and does not change behaviour.

> ⚠ The next block is the **`develop_old`** wording, kept for comparison. It is *not* what the port
> emits: `PORT-PLAN.md` §8.4 dropped `J01FF%` and took the `Antibiotika: Resistendrivende` caption,
> so `DrugSql.DrugSetAntibioticResistance()` has three groups, not four. See §E.2.1.

```sql
-- SpDrugsetAntibiotic (develop_old, pre-recovery), prefix 'DRUG_'
SELECT PersonId, 'RESISTANCE_DRIVING' AS VarName, ABS(CHECKSUM(DrugName)) % 100000 AS DpValue, StartAt, TreatId, ai.AtcName AS Caption FROM dbo.OngoingTreatment ot LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC WHERE ( PersonId IN {IdList} ) AND (   ( ot.ATC COLLATE Latin1_General_CI_AI LIKE 'J01CR%' COLLATE Latin1_General_CI_AI ) OR ( ot.ATC COLLATE Latin1_General_CI_AI LIKE 'J01D[CDH]%' COLLATE Latin1_General_CI_AI ) OR   ( ot.ATC COLLATE Latin1_General_CI_AI LIKE 'J01FF%' COLLATE Latin1_General_CI_AI ) OR ( ot.ATC COLLATE Latin1_General_CI_AI LIKE 'J01MA%' COLLATE Latin1_General_CI_AI ) )
```

```sql
-- SpDrugHypertensionWithLowBp( ALowBpThreshold ), prefix 'OVERTREAT.'
EXEC Report.ColAntiHypertensivesLowBP %d
```

```sql
-- SpDrugAndRenalFunction( ADrugPattern, ALowGfrValueThreshold ), prefix 'LASTUNDER.'
EXEC Report.ColDrugAndRenalFunction %s, %d       -- %s is QuotedStr(pattern)
```

### B.10 Drug–drug interaction (DRUID)

```sql
-- SpDruidIndividualInteractions( AMinCount ), prefix ''
SELECT a.PersonId, REPLACE(agg.AlertClass,'#','') AS VarName, AlertLevel AS DpValue,CreatedAt, a.AlertId, a.AlertHeader AS Caption
FROM
(
  SELECT AlertClass, COUNT(*) AS n FROM dbo.Alert
   WHERE AlertClass LIKE 'DRUID#%'
  GROUP BY AlertClass
) agg
JOIN dbo.Alert a ON a.AlertClass = agg.AlertClass
WHERE agg.n > %d
ORDER BY PersonId
```
Called with `AMinCount = 5`. Column = `DRUID<something>` (the `#` is stripped, so
`DRUID#C10AA_B01AA` → `DRUIDC10AA_B01AA`). Prefix is `''`.

```sql
-- SpDruidCountByLevel, prefix 'DRUID.'
SELECT PersonId,
  CASE AlertLevel WHEN 1 THEN 'GREEN' WHEN 2 THEN 'YELLOW' WHEN 3 THEN 'ORANGE' WHEN 4 THEN 'RED' END AS DruidLevel,
  n, MaxAlertDate, MaxAlertId
FROM
(
  SELECT PersonId, AlertLevel, MAX(CreatedAt) AS MaxAlertDate, COUNT(*) AS n, MAX(AlertId) AS MaxAlertId FROM dbo.Alert
  WHERE ( AlertClass LIKE 'DRUID%' ) AND ( AlertLevel > 0 )
  GROUP BY PersonId, AlertLevel
) agg
ORDER BY PersonId
```
Columns `DRUID.GREEN`, `DRUID.YELLOW`, `DRUID.ORANGE`, `DRUID.RED`.

### B.11 Form counting / completeness / recency

```sql
-- SpRecentFormCountAll( AMonthCount )   -- prefixes FORMS3M./6M./12M./24M.
SELECT ce.PersonId, UPPER(mf.FormName) AS VarName, COUNT(*) AS DpValue, MAX(ce.EventTime) AS VarDate, MAX(cf.ClinFormId) AS MaxClinFormId
FROM dbo.ClinForm cf
JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId
JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId
WHERE ( DATEDIFF( MM, ce.EventTime, GETDATE() ) < %d ) AND ( cf.DeletedAt IS NULL )
GROUP BY ce.PersonId, mf.FormName
```

```sql
-- SpRecentFormCountSingle( AFormName, AMonthCount )
SELECT ce.PersonId, UPPER(mf.FormName) AS VarName, COUNT(*) AS DpValue, MAX(ce.EventTime) AS VarDate, MAX(cf.ClinFormId) AS MaxClinFormId
FROM dbo.ClinForm cf
JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId
JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId AND mf.FormName=%s
WHERE ( DATEDIFF( MM, ce.EventTime, GETDATE() ) < %d ) AND ( cf.DeletedAt IS NULL )
GROUP BY ce.PersonId, mf.FormName
```

```sql
-- SpRecentFormGroupCount( AVarName, AFormNameList, AMonthCount )
SELECT ce.PersonId, UPPER(%s) AS VarName, COUNT(*) AS DpValue, MAX(ce.EventTime) AS MaxEventTime, MAX(cf.ClinFormId) AS MaxClinFormId
FROM dbo.ClinForm cf
JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId
JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId AND mf.FormName IN ( %s )
WHERE ( DATEDIFF( MM, ce.EventTime, GETDATE() ) < %d ) AND ( cf.DeletedAt IS NULL )
GROUP BY ce.PersonId
```
The form-name list is built by `'''' + TRegEx.Replace( AFormNameList, '\s*,\s*', ''',''' ) + ''''`,
i.e. `'A,B,C'` → `'A','B','C'`.

```pascal
function SpRecentFormGroupLege3m: string;
const
  GBD_LEGENOTATER     = 'GBD_NOTAT_LEGE,GBD_STATUS_PRESENS,GBD_INFECTION,GBD_BESLUTNINGER';
  VAR_GBD_LEGENOTATER = 'GBDLEGE';
begin
  Result := SpRecentFormGroupCount( QuotedStr( VAR_GBD_LEGENOTATER ), GBD_LEGENOTATER, 3 );
end;
```
→ `UPPER('GBDLEGE') AS VarName ... mf.FormName IN ( 'GBD_NOTAT_LEGE','GBD_STATUS_PRESENS','GBD_INFECTION','GBD_BESLUTNINGER' ) ... < 3`.
Column `FORMS.GBDLEGE`.

```sql
-- SpRecentFormCompleteness( AFormName, AMonths ), prefix 'FORMS6M.'
SELECT PersonId, VarName, DpValue, EventTime, ClinFormId
FROM
(
  SELECT ce.PersonId, mf.FormName AS VarName, cf.FormComplete AS DpValue, ce.EventTime, cf.ClinFormId,
  RANK() OVER (Partition by ce.PersonId ORDER BY cf.FormComplete, ce.EventNum DESC) AS rnk
  FROM dbo.ClinForm cf
  JOIN dbo.ClinEvent ce ON ce.EventId = cf.EventId
  JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId
  WHERE mf.FormName = %s AND cf.FormComplete > 0 AND cf.DeletedAt IS NULL
  AND DATEDIFF( MM, ce.EventTime, GETDATE() ) < %d
) agg
WHERE agg.rnk = 1
```
Value = `FormComplete` (percentage complete). Ranking picks the **lowest** completeness first
(`ORDER BY cf.FormComplete` ascending), then the newest event — i.e. the worst recent instance.

```sql
-- SpRecentQuantityPresent( AItemId, AMonthsAgo ), prefixes LAST2M./3M./6M./12M.
SELECT ce.PersonId, mi.VarName, cdp.Quantity, ce.EventTime, cdp.RowId
FROM dbo.ClinDataPoint cdp
JOIN dbo.ClinEvent ce ON ce.EventId = cdp.EventId
JOIN dbo.MetaItem mi ON mi.ItemId = cdp.ItemId
WHERE ( cdp.ItemId = %d )
AND ( NOT cdp.Quantity IS NULL )
AND DATEDIFF( MM, ce.EventTime, GETDATE()) < %d
ORDER BY ce.EventNum
```
> Returns **every** matching datapoint, not just the latest. `IPersonGridRow.AddDatapoint` returns
> False for a duplicate variable name and the collector then frees the extra datapoint — so the
> *first* row wins, and `ORDER BY ce.EventNum` ascending means the **oldest** value in the window is
> what lands in the cell. Reproduce this ordering exactly.

```sql
-- SpSnapshotQuantityIfBelowThreshold( AItemId, AValue ), prefix 'LASTUNDER.'
-- Format(..., [AItemId, AValue], TFormatSettings.Create('en-US'))  -> '%g' uses a dot decimal sep.
SELECT v.PersonId, mi.VarName, v.Quantity AS DpValue, v.EventTime, 0 AS RowId
FROM dbo.GetLastQuantityTable( %0:d, NULL ) v
JOIN dbo.MetaItem mi ON mi.ItemId = %0:d
WHERE v.Quantity < %g
```
Called as `(3556, 120.0)` → `... dbo.GetLastQuantityTable( 3556, NULL ) ... WHERE v.Quantity < 120`.
**External dependency:** table-valued function `dbo.GetLastQuantityTable`.
Note the explicit `en-US` format settings — the port must use `CultureInfo.InvariantCulture`.

```sql
-- SpFlackerKileyDeath, prefix 'FK.'  (item 1128 = Flacker-Kiely score)
SELECT PersonId, VarName, DataValue, EventTime, RowId, ReverseOrder 
FROM
(
  SELECT ce.PersonId, cdp.Quantity AS FK_SCORE,	 CONVERT(DECIMAL(18,4),DATEDIFF(DAY, ce.EventTime, p.DeceasedDate )) AS FK_DAYS_LIVED,    ce.EventTime, cdp.RowId,
  ROW_NUMBER() OVER (PARTITION BY ce.PersonId ORDER BY ce.EventTime DESC ) AS ReverseOrder
  FROM dbo.ClinEvent ce
  JOIN dbo.ClinDataPoint cdp ON cdp.EventId = ce.EventId
  JOIN dbo.Person p ON p.PersonId = ce.PersonId
  WHERE cdp.ItemId = 1128
) AS SourceTable
UNPIVOT
( DataValue FOR VarName IN ( FK_SCORE, FK_DAYS_LIVED ) ) AS DestTable
WHERE ReverseOrder = 1
```
Produces two columns per patient: `FK.FK_SCORE` and `FK.FK_DAYS_LIVED`. (The Delphi source contains a
literal tab character inside the string; harmless.)

### B.12 Caption / metadata queries (not collectors, but part of the pipeline)

```sql
QRY_FORM_CLASSES = EXEC Report.GetFormClasses :StudyId        -- FormName, FormTitle

QueryLabCaptions:
SELECT ISNULL(NLK, Report.LabClassName(LabClassId)) AS VarName, FriendlyName AS Caption, NULL AS VarDescription FROM dbo.LabClass ORDER BY LabClassId

QueryItemCaptions:
SELECT mi.VarName, ISNULL(mfi.ItemHeader,mfi.ItemText) AS Caption, mfi.ItemHelp AS VarDescription FROM dbo.MetaFormItem mfi JOIN dbo.MetaItem mi ON mi.ItemId = mfi.ItemId ORDER BY mfi.FormId

QueryCustomCaptions:
SELECT VarSpec AS VarName, Caption, VarDescription FROM Report.ColumnCaption
```

### B.13 Summary of database objects the collectors depend on

Tables/views: `dbo.Person`, `dbo.StudCase`, `dbo.StudCaseLog`, `dbo.StudyGroup`, `dbo.Study`,
`dbo.ClinEvent`, `dbo.ClinForm`, `dbo.ClinDataPoint` (also spelled `dbo.ClinDatapoint`),
`dbo.ClinProblem`, `dbo.MetaForm`, `dbo.MetaFormItem`, `dbo.MetaItem`, `dbo.MetaItemAnswer`,
`dbo.MetaProblemType`, `dbo.MetaNomListItem`, `dbo.MetaNomItem`, `dbo.LabData`, `dbo.LabCode`,
`dbo.LabClass`, `dbo.OngoingTreatment`, `dbo.Alert`, `dbo.KBAtcIndex`, `dbo.KBAnticholinDrug`,
`Diagnose.ICD10`, `Report.ColumnCaption`, `Report.QuickStat`, `dbo.DbProcList`.

Functions: `Report.LabClassName(int)`, `dbo.GetLastQuantityTable(int, ?)`.

Procedures: `Report.GetFormClasses`, `Report.GetFormData`, `Report.GetFormInstances`,
`Report.ColLabQuarters`, `Report.ColGbdTvangsvedtak`, `Report.ColAntiHypertensivesLowBP`,
`Report.ColDrugAndRenalFunction`, `Report.NorGeP`, `Report.AddSelection`,
`Report.AddSelectionMember`, `Report.AddQuickStat`, `QuickStat.DeletePackage`,
`dbo.GetPopulations`.

Plus, since §E.1 was recovered: `KB.AntibioticResistance2` — the one object the port probes for
before registering its collector, rather than depending on unconditionally.

---

## C. The `PID_LIST_PLACEHOLDER` mechanism

### C.1 What it is

```pascal
// EPR.QA.SQL.pas
FORM_NAME_PLACEHOLDER = '{FormName}';
PID_LIST_PLACEHOLDER  = '{IdList}';
ITEM_LIST_PLACEHOLDER = '{ItemList}';
LAB_LIST_PLACEHOLDER  = '{LabList}';
```

`{ItemList}`, `{LabList}` and `{FormName}` are substituted **once, at construction time**, from
compile-time constants — they are never user input and never change per batch. Only `{IdList}` is
substituted **per batch, at query time**:

```pascal
function TDataCollector.SQL: string;
var pidList: TStringList; key: integer;
begin
  if FMaxBatchSize <= 1 then
    Result := FSQL
  else
  begin
    pidList := TStringList.Create;
    try
      pidList.StrictDelimiter := true;
      pidList.Delimiter := ',';
      for key in FBatch.Keys do
        pidList.Add( IntToStr( key ) );
      Result := StringReplace( FSQL, PID_LIST_PLACEHOLDER,
                               '(' + pidList.DelimitedText + ')', [rfIgnoreCase, rfReplaceAll] );
    finally
      pidList.Free;
    end;
  end;
end;
```

The batch is a `TDictionary<integer, TObject>` keyed by `PersonId`, so the emitted order is **hash
order, not sorted**. The result is a literal in-list: `WHERE ( PersonId IN (4711,88,12903,…) )`.

### C.2 Which collectors use it, and how big the list gets

| `FMaxBatchSize` | Mechanism | Classes | Max ids per query |
| --- | --- | --- | --- |
| `1` | `:PersonId` parameter, `FastQuery(SQL, [FLastId])` | `TFormInstanceCollector`, `TFormDataCollector` | 1 (but **N round trips**) |
| `100` | `{IdList}` inlined | `TDemographicsCollector`, `TVarSetCollector`, `TVarSetAgeCollector`, `TVarSetMaxCollector`, `TFormAgeCollector`, `TLabSetCollector`, `TLab*TrustCollector`, `TDrugCollector` | 100 |
| `200` | `{IdList}` inlined | `TFormDataNumericCollector`, and `TFormDataCollector` **after** recovering `SpSnapshotFormDataAll` (§F.1) | 200 |
| `maxint` | `{IdList}` inlined — **the whole population in one statement** | `TCustomDataCollector`, `TDiagnoseCollector`, `TDementiaCollector`, `TGlobalCollector` (Status/Group/Center/…) | population size |

Crucially, **most `maxint` collectors do not even contain `{IdList}`.** `SpRecentFormCountAll`,
`SpRecentLabdataPresent`, `SpDiagnoseDetailsByLevel`, `SpDiagnoseByPattern`,
`SpDiagnoseDementiaAndAlzheimers`, `SpDruid*`, `SpDrugCountNoAtc`, `SpDrugWithoutDiagnose`,
`QRY_DRUGCOUNT_BY_TYPE`, `QRY_DRUGCOUNT_BY_ATCGROUP`, `QRY_NORGEP`, `SpRecentQuantityPresent`,
`SpRecentFormCompleteness`, `SpSnapshotQuantityIfBelowThreshold`, `SpFlackerKileyDeath` and all
`EXEC Report.Col*` procedures **scan the entire database** and rely on `RunBatch` discarding rows
whose `PersonId` is not in the batch (logged as `Unknown patients found, n = …`).

The ones that *do* inline the whole population (`maxint` **and** `{IdList}` present) are the drug-set
collectors built on `SQL_WHERE_PERSON_LIST`:
`QS_DRUG_METFORMIN`, `QS_DRUG_ANTICHOLIN_N05`, `QS_DRUG_ANTICHOLIN_AB`,
`QS_DRUG_ANTIBIOTIC_RESISTANCE` (+ the two recovered antibiotic collectors).

**How large can it get?** There is no cap anywhere. The population comes from a stored procedure
chosen in the UI (`dbo.GetPopulations`, default `GetCaseListMyRelations`) and is held in
`TPersonGridData.fPopulation`. For a whole-cohort selection on a GBD/LANGTID installation that is
routinely **thousands**, and nothing prevents tens of thousands. At 6 digits + comma ≈ 7 bytes per
id, 20 000 patients produce a ~140 KB `IN` list — which SQL Server *will* accept (batch limit is
64 MB, `IN` has no documented element cap) but which produces a unique, uncacheable plan every time
and is pathological for the optimiser.

### C.3 Injection risk in the current code

Effectively none: the ids come from `FBatch.Keys`, which is `Integer`, and are rendered with
`IntToStr`. `{FormName}`, `{ItemList}` and `{LabList}` come from compile-time constants and
`Report.GetFormClasses` (a database-controlled value, passed through `QuotedStr`). The port must
preserve that discipline — but must not *rely* on it, because form names now round-trip through
a UI-visible string.

### C.4 Recommendation for the .NET port — decision

**Use a table-valued parameter as the primary mechanism, with chunked literal inlining as the single
fallback. Do not build a third path.**

1. **Never** try to parameterise the list element-wise (`@p0, @p1, …`). SQL Server's hard limit is
   **2 100 parameters per statement** (2 098 usable), and `Microsoft.Data.SqlClient` throws
   `Too many parameters` at 2 101. A 3 000-patient population would break outright, and even a
   1 000-patient one would defeat plan reuse for no gain.

2. **Primary: a table-valued parameter.** Ship a migration that creates

   ```sql
   CREATE TYPE Report.PersonIdList AS TABLE ( PersonId INT NOT NULL PRIMARY KEY );
   GRANT EXECUTE ON TYPE::Report.PersonIdList TO QuickStat;
   ```

   Replace `{IdList}` with `(SELECT PersonId FROM @pids)` and bind:

   ```csharp
   var p = cmd.Parameters.Add("@pids", SqlDbType.Structured);
   p.TypeName = "Report.PersonIdList";
   p.Value = PersonIdTvp.From(personIds);   // IEnumerable<SqlDataRecord>, streamed
   ```

   One parameter, no element limit, one cached plan per collector, and the optimiser gets a real
   cardinality estimate from the TVP. Stream the `SqlDataRecord`s — do **not** materialise a
   `DataTable`.

3. **Fallback: chunked literal inlining, chunk size 1 000.** Detect the TVP once per connection at
   startup:

   ```csharp
   bool hasTvp = (await cmd("SELECT TYPE_ID('Report.PersonIdList')").ExecuteScalarAsync()) is int;
   ```

   If absent (older customer database, no DDL rights), fall back to today's behaviour but with a
   fixed chunk of 1 000 ids formatted with `int.ToString(CultureInfo.InvariantCulture)`. This is
   strictly better than the current `maxint` batches and preserves semantics exactly, because every
   `{IdList}` query is a per-person projection — chunking never changes the result. Keep the ids
   typed `int` end-to-end; never accept a `string` id.

   *Rejected as the fallback:* `STRING_SPLIT` / `OPENJSON`. Both need compatibility level 130+, and
   the whole point of the fallback is to work on the oldest database in the estate. If you later
   establish a 2016+ floor, `JOIN OPENJSON(@ids) j ON j.value = x.PersonId` is a fine one-parameter
   alternative and can replace the chunking path — but implement it only then, not in addition.

4. **Batch sizes become policy, not per-class magic numbers.** Today's 1 / 100 / 200 / `maxint` are
   accidents of history. With a TVP, use one value for everything — **2 000** is a good default —
   and expose it as a setting. Keep `maxint`-style "one shot" only for the collectors that have no
   `{IdList}` at all.

5. **Add `{IdList}` to the collectors that lack it.** The whole-table scans in §C.2 are the single
   biggest performance defect in the subsystem: `SpDiagnoseDetailsByLevel(5)` reads every problem
   row in the database to populate 300 patients. With a TVP in hand, adding
   `JOIN @pids p ON p.PersonId = <alias>.PersonId` is mechanical. Treat this as a **follow-up**,
   behind a flag, verified by comparing exported matrices — it changes nothing semantically (the
   rows were discarded client-side anyway) but it is a behaviour-affecting rewrite of 20 queries and
   should not ride along with the port.

6. **Kill the two `FMaxBatchSize = 1` collectors' N+1 pattern.** `TFormInstanceCollector` and
   `TFormDataCollector` issue one round trip *per patient*. Either recover `SpSnapshotFormDataAll`
   (§F.1) for form data, or add TVP-taking overloads `Report.GetFormDataBatch @pids, @formName` and
   `Report.GetFormInstancesBatch @pids`. Until then, budget for `patients × (1 + formClasses)`
   round trips.

7. **Interface shape.** Model this as one method on the SQL builder:

   ```csharp
   public interface IPidListStrategy
   {
       /// Returns the SQL fragment that replaces {IdList}, and binds whatever it needs.
       string Bind(SqlCommand cmd, IReadOnlyCollection<int> personIds);
       int MaxIdsPerBatch { get; }      // int.MaxValue for TVP, 1000 for the literal fallback
   }
   ```
   Two implementations: `TableValuedParameterPidList` and `InlineLiteralPidList`. The collector
   descriptors stay strategy-agnostic; only the executor knows which one is active. This also makes
   golden-file tests trivial: inject a `FixedTokenPidList` that always emits `(/*PIDS*/)`.

---

## D. Study-specific gating

### D.1 The exact predicates

All gating lives in `TQuickStatCollectors.AddCollectorsHardCoded`
(`QuickStat.Collectors.pas:417-490`) and is evaluated against `fStudyId.StudyName`
(`IStudyId.StudyName`, `CRF.Study.Interfaces.pas`), which is the study short name from
`dbo.Study.StudName`:

```pascal
if TRegEx.IsMatch( fStudyId.StudyName, 'GBD|LANGTID|KORTTID' ) then ...
if TRegEx.IsMatch( fStudyId.StudyName, 'NDV|ENDO|LANGTID|GBD|KORTTID' ) then ...
if TRegEx.IsMatch( fStudyId.StudyName, 'GWAS' ) then ...
if TRegEx.IsMatch( fStudyId.StudyName, 'ROAS' ) then ...
if TRegEx.IsMatch( fStudyId.StudyName, 'DOGFOOD', [roIgnoreCase] ) then ...
```

Semantics to preserve exactly:

* These are **unanchored substring** matches, not equality. A study named `MYGBDTEST` matches `GBD`.
* The first four are **case-sensitive** (`TRegEx.IsMatch` with no options). Only `DOGFOOD` passes
  `[roIgnoreCase]`.
* The blocks are **independent `if`s**, not `else if` — a study named `GBD_NDV_GWAS` gets all three
  collector sets.
* `LANGTID` and `KORTTID` and `GBD` appear in **both** the first and second predicate, so those
  studies get both the GBD set and the NDV/diabetes set.
* `AddCollectorsDiagnose` and `AddCollectorsDrug` are called **only** from inside the first block —
  the 17 diagnose collectors and 35 drug collectors are GBD/LANGTID/KORTTID-only.
* `TVarSetCollector.CreateForNumeric('SIZE', …)` is *outside* all blocks — always registered.
* `QST_LAB_DIABETES` (`LAB.DIABETES`) is registered by the **second** block only, even though the
  first block registers `QST_LAB_GERIATRIC`.

C# equivalent (note `RegexOptions.None` vs `IgnoreCase`):

```csharp
static readonly Regex GbdGate     = new(@"GBD|LANGTID|KORTTID",          RegexOptions.Compiled);
static readonly Regex NdvGate     = new(@"NDV|ENDO|LANGTID|GBD|KORTTID", RegexOptions.Compiled);
static readonly Regex GwasGate    = new(@"GWAS",                          RegexOptions.Compiled);
static readonly Regex RoasGate    = new(@"ROAS",                          RegexOptions.Compiled);
static readonly Regex DogfoodGate = new(@"DOGFOOD",  RegexOptions.Compiled | RegexOptions.IgnoreCase);
```

### D.2 Resulting collector sets

| Study name example | Gates hit | Static collectors |
| --- | --- | --- |
| `NDV` | always, N | 36 + 8 = **44** |
| `ENDO` | always, N | **44** |
| `GBD` | always, G, N | 36 + 76 + 8 = **120** |
| `LANGTID` | always, G, N | **120** |
| `KORTTID` | always, G, N | **120** ← changed by 5502b72 |

> **These totals count *this* repo, which is the reduced build.** The canonical FastTrakApps source
> registers five more (37 always / 79 G / 8 N = **124** for `GBD`, `LANGTID` and `KORTTID`; 131
> distinct names). The +4 on a gated study are the three antibiotic collectors — inside
> `AddCollectorsDrug`, which the **G** block calls — plus interleukins, which is always-on.
> `QS_ROAS_BASE` is `ROAS`-gated and does not move these numbers, but it does take a `ROAS` study
> from 38 to **40** (37 always + 3).
>
> **The .NET port reaches the canonical numbers**: Phase 4 restored all five, so the catalog holds
> **131** and a `KORTTID` study registers **124**. One caveat the Delphi has no equivalent for —
> `QS_DRUG_ANTIBIOTIC_INTERMEDIATE` is registered only when `KB.AntibioticResistance2` resolves
> (§E.1, `PORT-PLAN.md` R7), so on a database without the `KB` schema the same study registers
> **123**. See `PORT-PLAN.md` §10.4.
| `ROAS` | always, R | 36 + 2 = **38** |
| `ROAS_GWAS` | always, W, R | 36 + 3 + 2 = **41** |
| `DOGFOOD` / `dogfood` | always, D | 36 + 1 = **37** |
| anything else | always | **36** |

Plus `2 × N` dynamic form collectors in every case.

### D.3 The most recent change — QuickStat commit `5502b72`

```
commit 5502b72482203af4779efe8c0a41dbc5cb71d2d3
Author: Christoffer Hjeltnes Støle <chs@dips.no>
Date:   Tue Aug 25 13:27:48 2026 +0200

    #739506: Ta med GBD-utvalet i Korttid i QuickStat

 QuickStat.Collectors.pas | 4 ++--
```

```diff
-    if TRegEx.IsMatch( fStudyId.StudyName, 'GBD|LANGTID' ) then
+    if TRegEx.IsMatch( fStudyId.StudyName, 'GBD|LANGTID|KORTTID' ) then
...
-    if TRegEx.IsMatch( fStudyId.StudyName, 'NDV|ENDO|LANGTID|GBD' ) then
+    if TRegEx.IsMatch( fStudyId.StudyName, 'NDV|ENDO|LANGTID|GBD|KORTTID' ) then
```

**What it means:** studies whose name contains `KORTTID` ("short-term", the short-stay counterpart
to `LANGTID`) previously got only the 36 always-on collectors. After this change they get the full
GBD set (24), the diagnose set (17), the drug set (35) and the NDV/diabetes set (8) — going from
**36 to 120** static collectors.

**Port requirement:** `KORTTID` must appear in **both** regexes. This is the newest functional
change in the repository (HEAD), it is the reason the ticket exists, and it is the easiest thing to
lose when transcribing two similar-looking regex literals. Add an explicit unit test:

```csharp
[Theory]
[InlineData("KORTTID", true, true)]   // gbd, ndv
[InlineData("LANGTID", true, true)]
[InlineData("GBD",     true, true)]
[InlineData("NDV",     false, true)]
[InlineData("ENDO",    false, true)]
[InlineData("korttid", false, false)] // case-sensitive!
public void StudyGates(string studyName, bool gbd, bool ndv) { … }
```

---

## E. Recovering the four lost features

> **Status: done.** Phase 4 restored all five registrations. `DRUG.INTERMEDIATE`, `DRUG.RECOMMENDED`
> and `DRUG.J01XX05` are in `CollectorCatalog.Drug.cs`, `ROAS.BASE` in `.Protocol.cs`,
> `LAB.INTERLEUKINS` in `.LabData.cs`; the SQL is in `DrugSql`, the sets in `ItemSets.RoasBase` and
> `LabClassSets.Interleukins`. The registry holds **131** distinct names and a `KORTTID` study
> registers **124**, or **123** where `KB.AntibioticResistance2` does not resolve. Everything below
> is the derivation, kept because it is the evidence for each number; the two paragraphs marked
> *superseded* (§E.2.1's Recommendation) are the only places it disagrees with what shipped.

All four exist only on the **tarmscreening lineage** of `C:\work\FastTrak`
(`origin/tarmscreening/develop`, head `249ac2d16` 2023-09-01). `git branch --contains` confirms
none of the four commits is reachable from `develop`, which is why the extracted `FastTrak\` copies
(= `develop_old`) lack them and `QuickStat.Collectors.pas` has them commented out.

Currently commented out in `C:\work\FastTrak.Quickstat\QuickStat.Collectors.pas`:

```pascal
// line 297, AddCollectorsLabData
//    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_INTERLEUKINS ) );

// lines 381-383, AddCollectorsDrug
//  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_ANTIBIOTIC_INTERMEDIATE ) );
//  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_ANTIBIOTIC_RECOMMENDED ) );
//  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_J01XX05 ) );

// line 480, AddCollectorsHardCoded / ROAS block
//      AddCollector( fCollectorFactory.CreateCollector( QS_ROAS_BASE ) );
```

> ⚠ **Read the diffs, not just the commits.** `9f4a5ed4f` is titled *"Rebase on develop"* and its
> diff contains a large amount of **rebase noise**: the whole file was re-encoded from UTF-8 to
> Windows-1252 (hence `Kj�nn` etc. in the diff), and it appears to *delete* the BDR block,
> `FORM_NAME_NEWS2`, `StrTitleVarsetInsulinPump` and `StrTitleFormNEWS2`. **Those deletions are
> artefacts of the rebase base, not intentional removals** — the branch head still has all of them,
> and so does the local copy. Only apply the additions listed below.

### E.1 `QS_DRUG_ANTIBIOTIC_INTERMEDIATE` — commit `4c96c3c3b`

```
commit 4c96c3c3bfc1b2fb36001fac28ff8a69fa31846c
Author: Magne Rekdal <mrk@dips.no>
Date:   Mon Sep 21 13:22:43 2020 +0200
    Lagt til collector for intermediære antibiotika.
```

**`EPR.QA.Collector.Names.pas`** — one resource string and one collector name:

```pascal
  StrTitleDrugAntibioticIntermediate = 'Antibiotika: Intermediære';
```
```pascal
  QS_DRUG_ANTIBIOTIC_INTERMEDIATE = PREFIX_DRUG_COLLECTOR + 'INTERMEDIATE';   // 'DRUG.INTERMEDIATE'
```

**`EPR.QA.SQL.pas`** — interface declaration:

```pascal
function SpDrugsetAntibioticIntermediate: string;
```

**`EPR.QA.SQL.pas`** — implementation. The function body was actually introduced by `9f4a5ed4f`
with a **typo'd table and column name**, which `4c96c3c3b` then fixed. The corrected, final form
(exactly as it stands on `origin/tarmscreening/develop`) is:

```pascal
function SpDrugsetAntibioticIntermediate: string;
const
  VAR_RECOMMENDED_ANTIBIOTICS = 'INTERMEDIATE_AB';
  QRY_DRUGSET_ANTIBIOTICS            =
  { } 'SELECT PersonId, ''' + VAR_RECOMMENDED_ANTIBIOTICS +
    ''' AS VarName, ABS(CHECKSUM(DrugName)) % 100000 AS DpValue, StartAt, TreatId, ai.AtcName AS Caption ' +
  { } SQL_FROM_ONGOING_TREATMENT +
  { } SQL_JOIN_ATC_INDEX +
  {} 'JOIN KB.AntibioticResistance2 r2 ON r2.AtcCode = ot.ATC ' +
  { } SQL_WHERE_PERSON_LIST;
 begin
  Result := QRY_DRUGSET_ANTIBIOTICS;
end;
```

Resolved SQL:

```sql
SELECT PersonId, 'INTERMEDIATE_AB' AS VarName, ABS(CHECKSUM(DrugName)) % 100000 AS DpValue, StartAt, TreatId, ai.AtcName AS Caption FROM dbo.OngoingTreatment ot LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC JOIN KB.AntibioticResistance2 r2 ON r2.AtcCode = ot.ATC WHERE ( PersonId IN {IdList} ) 
```

The fix in `4c96c3c3b` was:

```diff
-  {} 'JOIN KB.AntibioticRestistance2 r2 ON r2.AtcKode = ot.ATC ' +
+  {} 'JOIN KB.AntibioticResistance2 r2 ON r2.AtcCode = ot.ATC ' +
```
(`Restistance` → `Resistance`, `AtcKode` → `AtcCode`). **Port the fixed spelling.**

**`EPR.QA.Collector.Factory.pas`**:

```pascal
  else if ACollectorName = QS_DRUG_ANTIBIOTIC_INTERMEDIATE then
    Result := TCustomDataCollector.Create( QS_DRUG_ANTIBIOTIC_INTERMEDIATE, StrTitleDrugAntibioticIntermediate, VAR_PREFIX_DRUG_SPAN, SpDrugsetAntibioticIntermediate, fDatapointFactory, fSQL, Log )
```

**`QuickStat.Collectors.pas`** — uncomment in `AddCollectorsDrug`, keeping the source order
(`INTERMEDIATE` immediately after `RESISTANCE`, before `RECOMMENDED`):

```pascal
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_ANTIBIOTIC_RESISTANCE ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_ANTIBIOTIC_INTERMEDIATE ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_ANTIBIOTIC_RECOMMENDED ) );
  AddCollector( fCollectorFactory.CreateCollector( QS_DRUG_J01XX05 ) );
```

> 🚩 **External dependency.** `KB.AntibioticResistance2` is in the `KB` ("knowledge base") schema,
> not `dbo`, and it is the **only** reference to that schema anywhere in the collector subsystem. It
> is not part of the core FastTrak schema and it will be **absent in many customer databases**. Note
> the join is an **inner** `JOIN`, so a missing object makes the whole query fail, not just return
> nothing.
>
> **The name is confirmed: `KB.AntibioticResistance2` exists, as a view.** Not the author's original
> `AntibioticRestistance2`, which `4c96c3c3b` corrected together with `AtcKode` → `AtcCode`. A view
> rather than a table changes nothing here — `OBJECT_ID` resolves both, and an inner join to a view
> fails the same way when it is missing — so the gate below stands unaltered.
>
> Required handling in the port:
> 1. Probe at registry-build time — `SELECT OBJECT_ID('KB.AntibioticResistance2')` — and only
>    register the collector when it resolves. There is no existing "optional collector" concept, so
>    add one. **Done**, as `CollectorDescriptor.Availability` / `CollectorAvailability`; the probe
>    is `CollectorRegistry.ProbeDatabaseObjectsAsync`, one round trip for the whole catalog.
> 2. Log at info level when it is skipped, so support can tell "missing" from "empty". **Done**, via
>    the `onUnavailable` callback `CollectorRegistryBuilder.Build` takes.
> 3. Confirm with the DB owner which spelling actually exists before enabling it anywhere.
>    **Resolved 2026-08-27: `KB.AntibioticResistance2`, a view.** The gate stays regardless — its
>    job is the databases that do not have the object at all, which is most of them.
>
> **This is the only collector that needs the gate.** `SpDrugsetAntibioticRecommended`
> (`EPR.QA.SQL.pas:431`) writes its nine ATC codes into the statement and joins no `KB` object, so it
> is registered unconditionally; gating it too would hide a working collector.

### E.2 `QS_DRUG_ANTIBIOTIC_RECOMMENDED` and `QS_DRUG_J01XX05` — commit `9f4a5ed4f`

```
commit 9f4a5ed4fed97282ae4e03136678966887bbfd3e
Author: Magne Rekdal <mrk@dips.no>
Date:   Mon Sep 21 12:26:30 2020 +0200
    Rebase on develop.
```

**`EPR.QA.Collector.Drug.pas`** — one new ATC constant:

```pascal
  ATC_J01XX05  = 'J01XX05';
```
(appended after `ATC_N06D = 'N06D%';`. Note: no `%` — an exact-match pattern.)

**`EPR.QA.Collector.Names.pas`** — resource strings and collector names:

```pascal
  StrTitleDrugAntibioticResistance = 'Antibiotika: Resistendrivende';
  StrTitleDrugAntibioticRecommended = 'Antibiotika: Anbefalte';
  StrTitleDrugAntibioticMetenamine = 'Antibiotika: Metenamin / Hiprex';
```
```pascal
  QS_DRUG_J01XX05 = PREFIX_DRUG_COLLECTOR + 'J01XX05';               // 'DRUG.J01XX05'
  QS_DRUG_ANTIBIOTIC_RECOMMENDED = PREFIX_DRUG_COLLECTOR + 'RECOMMENDED';  // 'DRUG.RECOMMENDED'
```

**`EPR.QA.SQL.pas`** — interface:

```pascal
function SpDrugsetAntibioticResistance: string;   // renamed from SpDrugsetAntibiotic
function SpDrugsetAntibioticRecommended: string;
```

**`EPR.QA.SQL.pas`** — implementation:

```pascal
function SpDrugsetAntibioticRecommended: string;
const
  VAR_RECOMMENDED_ANTIBIOTICS = 'RECOMMENDED_AB';
  QRY_DRUGSET_ANTIBIOTICS            =
  { } 'SELECT PersonId, ''' + VAR_RECOMMENDED_ANTIBIOTICS +
    ''' AS VarName, ABS(CHECKSUM(DrugName)) % 100000 AS DpValue, StartAt, TreatId, ai.AtcName AS Caption ' +
  { } SQL_FROM_ONGOING_TREATMENT +
  { } SQL_JOIN_ATC_INDEX +
  { } SQL_WHERE_PERSON_LIST +
  { } 'AND ( ot.ATC IN ( ''J01CE01'', ''J01CE02'', ''J01CF01'', ''J01CF02'', ''J01CA08'', ''J01CA11'', ''J01EA01'', ''J01EE01'', ''J01XE01'' ) )';
begin
  Result := QRY_DRUGSET_ANTIBIOTICS;
end;
```

Resolved SQL:

```sql
SELECT PersonId, 'RECOMMENDED_AB' AS VarName, ABS(CHECKSUM(DrugName)) % 100000 AS DpValue, StartAt, TreatId, ai.AtcName AS Caption FROM dbo.OngoingTreatment ot LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC WHERE ( PersonId IN {IdList} ) AND ( ot.ATC IN ( 'J01CE01', 'J01CE02', 'J01CF01', 'J01CF02', 'J01CA08', 'J01CA11', 'J01EA01', 'J01EE01', 'J01XE01' ) )
```

The nine "recommended" ATC codes: `J01CE01` benzylpenicillin, `J01CE02` phenoxymethylpenicillin,
`J01CF01` dicloxacillin, `J01CF02` cloxacillin, `J01CA08` pivmecillinam, `J01CA11` mecillinam,
`J01EA01` trimethoprim, `J01EE01` sulfamethoxazole+trimethoprim, `J01XE01` nitrofurantoin.
Note this list uses a plain `IN`, **no `COLLATE`** — unlike every other drug query.

**`EPR.QA.Collector.Factory.pas`**:

```pascal
  else if ACollectorName = QS_DRUG_ANTIBIOTIC_RESISTANCE then
    Result := TCustomDataCollector.Create( QS_DRUG_ANTIBIOTIC_RESISTANCE, StrTitleDrugAntibioticResistance, VAR_PREFIX_DRUG_SPAN, SpDrugsetAntibioticResistance, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_ANTIBIOTIC_RECOMMENDED then
    Result := TCustomDataCollector.Create( QS_DRUG_ANTIBIOTIC_RECOMMENDED, StrTitleDrugAntibioticRecommended, VAR_PREFIX_DRUG_SPAN, SpDrugsetAntibioticRecommended, fDatapointFactory, fSQL, Log )
  else if ACollectorName = QS_DRUG_J01XX05  then
    Result := TDrugCollector.CreateBasic( StrTitleDrugAntibioticMetenamine, ATC_J01XX05, fDatapointFactory, fSQL, Log )
```

#### E.2.1 Behavioural change to the **existing** `QS_DRUG_ANTIBIOTIC_RESISTANCE`

The same commit revises the shipping resistance query:

```diff
   { } '  ( ot.ATC' + SQL_COLLATION + 'LIKE ''J01CR%''' + SQL_COLLATION + ') OR ( ot.ATC' + SQL_COLLATION + 'LIKE ''J01D[CDH]%''' + SQL_COLLATION + ') OR ' +
-  { } '  ( ot.ATC' + SQL_COLLATION + 'LIKE ''J01FF%''' + SQL_COLLATION + ') OR ( ot.ATC' + SQL_COLLATION + 'LIKE ''J01MA%''' + SQL_COLLATION + ') ' +
+  { } '  ( ot.ATC' + SQL_COLLATION + 'LIKE ''J01MA%''' + SQL_COLLATION + ') ' +
```

and renames the function and the caption:

| | today (`develop_old`, shipping) | tarmscreening |
| --- | --- | --- |
| function | `SpDrugsetAntibiotic` | `SpDrugsetAntibioticResistance` |
| caption constant | `StrTitleDrugRestistanceAntibiotic` | `StrTitleDrugAntibioticResistance` |
| caption text | `Medisin: Resistensdrivende antibiotika` | `Antibiotika: Resistendrivende` |
| ATC groups matched | `J01CR%`, `J01D[CDH]%`, **`J01FF%`**, `J01MA%` | `J01CR%`, `J01D[CDH]%`, `J01MA%` |

**Behavioural change to document:** `J01FF` (lincosamides — clindamycin, lincomycin) is **removed**
from the resistance-driving set. Patients on clindamycin alone will no longer produce a
`DRUG_RESISTANCE_DRIVING` value; a cohort's resistance-driving count will fall. The remaining three
groups are `J01CR` (penicillin + beta-lactamase inhibitor), `J01D[CDH]` (2nd/3rd-gen cephalosporins
and carbapenems) and `J01MA` (fluoroquinolones).

The `VarName` emitted is unchanged (`RESISTANCE_DRIVING`), so the **column name stays
`DRUG_RESISTANCE_DRIVING`** and old exports remain comparable in shape but not in content. The
`Report.ColumnCaption` override registered in `MainQuickStat.pas`
(`TCaptionRecord.Create('DRUG.RESISTANCE_DRIVING', 'Resist', 'Resistance-driving antibiotics')`)
also keeps working.

**Recommendation — superseded; see `PORT-PLAN.md` §8.4.** This paragraph originally said to keep the
**existing** `J01FF%` clause and caption. That was written before the question was re-checked
against every ref capable of building the application: `J01FF` is absent from **all nine** of them
and survives only on mainline, which cannot build QuickStat at all. §8.4 therefore decided
*"drop `J01FF%`, take the new caption"*, and applied the whole tarmscreening version together —
function name, caption and clause — so the four antibiotic captions read as a consistent
`Antibiotika: …` family. Phase 2 implemented that and Phase 4 did not revisit it.

It is nonetheless still a *clinical definition change*, and §8.4 keeps it **release-blocking for
this collector only**: a protocol owner must confirm which antibiotics count as resistance-driving
before release. Reversing it is one line in `DrugSql.ResistanceDrivingAtcPatterns` plus a
regenerated golden file, which is why the list is a named array.

#### E.2.2 `QS_DRUG_J01XX05` details

Built with `TDrugCollector.CreateBasic`, **not** `CreateChecksum`:

* Collector name = `PREFIX_DRUG_COLLECTOR + ConvertAtcPatternToVariableName('J01XX05')` = `DRUG.J01XX05`.
* SQL template = `QRY_DRUGSET_BASIC` → value column is the literal `1`, not a name checksum.
* `fTreatTypeFilter` defaults to `ttAnyTreatType`, so no `TreatType` clause and prefix `DRUG.`.
* `AfterConstruction` still applies: `FVarPrefix = 'ATC_'`, `FMaxBatchSize = 100`,
  `GroupResults = False` → `VarName = CONCAT('J01XX05','.',ot.TreatType)`.
* Resulting columns: `ATC_J01XX05.F`, `ATC_J01XX05.B`, … with value `1`.

Resolved SQL:

```sql
SELECT ot.PersonId, CONCAT('J01XX05','.',ot.TreatType) AS VarName, 1 AS DpValue, ot.StartAt, ot.TreatId, ai.AtcName AS Caption FROM dbo.OngoingTreatment ot LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC WHERE ( PersonId IN {IdList} ) AND ot.ATC COLLATE Latin1_General_CI_AI LIKE 'J01XX05' COLLATE Latin1_General_CI_AI 
```

> `LIKE 'J01XX05'` with no wildcard is an exact match — deliberate, Hiprex/metenamin has no
> sub-codes. Keep `LIKE` rather than `=` so the collation behaviour is identical.

### E.3 `QS_ROAS_BASE` ("Autommunitet") — commit `8a9954c13`

```
commit 8a9954c135e6fe095d03ff09b100eaf79b4cfc8d
Author: Magne Rekdal <mrk@dips.no>
Date:   Fri Sep 3 11:22:03 2021 +0200
    Freshdesk #595: Ny collector til QuickStat.
```

**`EPR.QA.Collector.Names.pas`**:

```pascal
  QS_ROAS_BASE         = 'ROAS.BASE';
```

**`EPR.QA.Collector.Factory.pas`** — as committed in `8a9954c13`:

```pascal
  else if ACollectorName = QS_ROAS_BASE then
    Result := TVarSetCollector.CreateForNumeric( QS_ROAS_BASE, 'Autommunitet (siste)', SET_ROAS_BASE, fDatapointFactory, fSQL, Log )
```

**…but use the corrected version.** Commit `08e35bd8d` (2021-09-15,
*"Korrigert navn på Autoimmunitet. Teksten "(siste)" kommer to ganger, legges på i constructor for
klassen."*) fixed the double suffix. The branch head — and what the port must implement — is:

```pascal
  else if ACollectorName = QS_ROAS_BASE then
    Result := TVarSetCollector.CreateForNumeric( QS_ROAS_BASE, 'Autommunitet', SET_ROAS_BASE, fDatapointFactory, fSQL, Log )
```

`TVarSetCollector.Create` appends `TXT_LAST`, so the **displayed title is `Autommunitet (siste)`**.

> Keep the typo `Autommunitet` (correct Norwegian would be `Autoimmunitet`). It is what users see
> today on the tarmscreening build and what any saved package title would match. If it is to be
> fixed, do it as a separate, deliberate change.

**`EPR.QA.Definitions.pas`** — the 68-element array, verbatim:

```pascal
  SET_ROAS_BASE: array [0 .. 67] of integer  = ( 4255, 6314, 3486, 6312, 6323, 6313, 6324, 6299, 6089, 6090, 6321, 6332, 3410, 6328, 6317, 6327, 6316, 6326,8594,
  { } 8595, 6318, 6334, 6329, 3411, 6330, 6320, 6331, 6322, 6333, 8543, 8544, 6669, 6670, 6671, 6607, 5069, 3982, 6633, 6634, 6635, 6636, 6637, 6638, 6639, 6640,
  { } 6808, 6641, 5170, 9996, 3983, 7135, 4002, 6682, 3985, 8797, 6605, 2143, 9477, 10643, 3846, 3981, 6804, 6805, 6802, 6803, 7977, 7979, 6807 );
```

All 68 ids as a flat, verified list (count checked: 19 + 26 + 23 = 68):

```
4255, 6314, 3486, 6312, 6323, 6313, 6324, 6299, 6089, 6090,
6321, 6332, 3410, 6328, 6317, 6327, 6316, 6326, 8594, 8595,
6318, 6334, 6329, 3411, 6330, 6320, 6331, 6322, 6333, 8543,
8544, 6669, 6670, 6671, 6607, 5069, 3982, 6633, 6634, 6635,
6636, 6637, 6638, 6639, 6640, 6808, 6641, 5170, 9996, 3983,
7135, 4002, 6682, 3985, 8797, 6605, 2143, 9477, 10643, 3846,
3981, 6804, 6805, 6802, 6803, 7977, 7979, 6807
```

The order matters only for the generated `IN ( … )` list (via `ConvertArrayToList`), which is what
golden-file tests will compare — **preserve the order** so the generated SQL is byte-comparable
with the Delphi output. There are no duplicates.

The collector is `TVarSetCollector.CreateForNumeric` → SQL is `SpSnapshotVarset(itNumeric, ids)`
(§B.6), prefix `''`, batch 100. Columns are the raw `MetaItem.VarName` values for those 68 items.

**`QuickStat.Collectors.pas`** — uncomment inside the `ROAS` block:

```pascal
    if TRegEx.IsMatch( fStudyId.StudyName, 'ROAS' ) then
    begin
      { ROAS POI Collectors }
      AddCollector( fCollectorFactory.CreateCollector( QS_ROAS_POI_ORD ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_ROAS_POI_QN ) );
      AddCollector( fCollectorFactory.CreateCollector( QS_ROAS_BASE ) );
    end;
```

> Sanity check for implementers: with 68 item ids and 100-patient batches, this collector can add
> up to 68 columns to the matrix. That is fine, but combined with `DXC.*` it is the reason the grid
> has a column-count warning path (`CheckDimensions`).

### E.4 `QST_LAB_INTERLEUKINS` — commit `fefc8a809`

```
commit fefc8a809cc71e1312c99d67e2aa85f94b75c57b
Author: Christoffer Hjeltnes Støle <chs@dips.no>
Date:   Tue Dec 13 13:19:59 2022 +0100
    #531377: Støtte for interleukin
```

**`EPR.QA.Definitions.pas`**:

```pascal
  LABCLASSES_INTERLEUKINS: TLabClassSet    = [1094, 1095, 1096, 1097, 1098, 1099, 1100, 1101, 1102, 1103, 1104];
```

**Verified: this is exactly `[1094..1104]` — 11 consecutive lab-class ids, no gaps, no extras.**
Inserted between `LABCLASSES_HYPERPARA` and `LABCLASSES_INR` in the source.

**`EPR.QA.Collector.Names.pas`** — three additions:

```pascal
  StrTitleLabsetInterleukins = 'Interleukiner';      // after StrTitleLabsetInr
```
```pascal
  LAB_INTERLEUKINS  = 'INTERLEUKINS';                // after LAB_HEART_FAILURE
```
```pascal
  QST_LAB_INTERLEUKINS  = PREFIX_LAB_COLLECTOR + LAB_INTERLEUKINS;   // 'LAB.INTERLEUKINS'
```

**`EPR.QA.Collector.Factory.pas`** — inserted after the `QST_LAB_DIABETES` branch:

```pascal
  else if ACollectorName = QST_LAB_INTERLEUKINS then
    Result := TLabSetCollector.Create( QST_LAB_INTERLEUKINS, StrTitleLabsetInterleukins, LABCLASSES_INTERLEUKINS, fDatapointFactory, fSQL, Log )
```

`'Interleukiner'` contains no `:` → `TLabSetCollector` wraps it, so the **displayed title is
`Labdata: Interleukiner (siste)`**.

Resolved SQL (`SpSnapshotLabset`, prefix `''`, batch 100):

```sql
SELECT agg.* FROM
(
  SELECT ld.PersonId, ISNULL(la.NLK, Report.LabClassName(lc.LabClassId)) AS VarName, ld.NumResult, ld.LabDate, ld.ResultId,
  RANK() OVER ( PARTITION BY ld.PersonId,lc.LabClassId ORDER BY ld.LabDate DESC ) AS OrderBy
  FROM dbo.LabData ld
  JOIN dbo.LabCode lc ON lc.LabCodeId = ld.LabCodeId
  JOIN dbo.LabClass la ON la.LabClassId = lc.LabClassId
  WHERE ( ld.PersonId IN {IdList} ) AND ( la.LabClassId IN (1094, 1095, 1096, 1097, 1098, 1099, 1100, 1101, 1102, 1103, 1104) AND ( ld.NumResult >= 0 ) )
 ) agg
 WHERE agg.OrderBy = 1 ORDER BY agg.PersonId, agg.VarName
```

**`QuickStat.Collectors.pas`** — uncomment in `AddCollectorsLabData`, keeping the position
(between `QST_LAB_HEART_FAILURE` and `QST_LAB_CRP`):

```pascal
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_HEART_FAILURE ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_INTERLEUKINS ) );
    AddCollector( fCollectorFactory.CreateCollector( QST_LAB_CRP ) );
```

> Soft dependency: lab classes 1094–1104 must exist in `dbo.LabClass`. Unlike E.1 this is a *data*
> dependency, not a schema one — a database without them simply returns no rows, no error. No
> guarding needed.

### E.5 Complete list of symbols missing from the local `FastTrak\` copies

Everything the four features need that is **not** present today, with its definition:

| Symbol | File to add it to | Definition |
| --- | --- | --- |
| `ATC_J01XX05` | `EPR.QA.Collector.Drug.pas` (const block) | `ATC_J01XX05 = 'J01XX05';` |
| `StrTitleDrugAntibioticIntermediate` | `EPR.QA.Collector.Names.pas` | `'Antibiotika: Intermediære'` |
| `StrTitleDrugAntibioticRecommended` | `EPR.QA.Collector.Names.pas` | `'Antibiotika: Anbefalte'` |
| `StrTitleDrugAntibioticMetenamine` | `EPR.QA.Collector.Names.pas` | `'Antibiotika: Metenamin / Hiprex'` |
| `StrTitleDrugAntibioticResistance` | `EPR.QA.Collector.Names.pas` | `'Antibiotika: Resistendrivende'` — the §E.2.1 rename **was** accepted, by `PORT-PLAN.md` §8.4, and Phase 2 applied it |
| `StrTitleLabsetInterleukins` | `EPR.QA.Collector.Names.pas` | `'Interleukiner'` |
| `QS_DRUG_ANTIBIOTIC_INTERMEDIATE` | `EPR.QA.Collector.Names.pas` | `PREFIX_DRUG_COLLECTOR + 'INTERMEDIATE'` |
| `QS_DRUG_ANTIBIOTIC_RECOMMENDED` | `EPR.QA.Collector.Names.pas` | `PREFIX_DRUG_COLLECTOR + 'RECOMMENDED'` |
| `QS_DRUG_J01XX05` | `EPR.QA.Collector.Names.pas` | `PREFIX_DRUG_COLLECTOR + 'J01XX05'` |
| `QS_ROAS_BASE` | `EPR.QA.Collector.Names.pas` | `'ROAS.BASE'` |
| `LAB_INTERLEUKINS` | `EPR.QA.Collector.Names.pas` | `'INTERLEUKINS'` |
| `QST_LAB_INTERLEUKINS` | `EPR.QA.Collector.Names.pas` | `PREFIX_LAB_COLLECTOR + LAB_INTERLEUKINS` |
| `SET_ROAS_BASE` | `EPR.QA.Definitions.pas` | 68-element array, §E.3 |
| `LABCLASSES_INTERLEUKINS` | `EPR.QA.Definitions.pas` | `[1094 .. 1104]`, §E.4 |
| `SpDrugsetAntibioticIntermediate` | `EPR.QA.SQL.pas` | §E.1 |
| `SpDrugsetAntibioticRecommended` | `EPR.QA.SQL.pas` | §E.2 |
| `SpDrugsetAntibioticResistance` | `EPR.QA.SQL.pas` | rename of `SpDrugsetAntibiotic`, §E.2.1 — **conditional** |

**Already present, no action needed** (verified against the local copies):

* `TDrugCollector.CreateBasic` — exists in `EPR.QA.Collector.Drug.pas:175`.
* `TCustomDataCollector` — exists in `EPR.QA.Collector.Base.pas:54`.
* `TLabSetCollector.Create(…, TLabClassSet, …)` — exists in `EPR.QA.Collector.Labdata.pas:44`.
* `TVarSetCollector.CreateForNumeric` — exists in `EPR.QA.Collector.VarSet.pas:63`.
* `VAR_PREFIX_DRUG_SPAN` (`'DRUG_'`), `PREFIX_DRUG_COLLECTOR`, `PREFIX_LAB_COLLECTOR` — all present.
* `SQL_FROM_ONGOING_TREATMENT`, `SQL_JOIN_ATC_INDEX`, `SQL_WHERE_PERSON_LIST`, `SQL_COLLATION` — all present.
* `TLabClassSet` type — present in `EPR.QA.Definitions.pas:104`.

So the only genuinely *new* runtime machinery required is the "optional collector, gated on a
database object existing" concept for E.1.

---

## F. Unconfirmed drift — decide before implementing

These are **additional** differences between the local `FastTrak\` copies (= mainline `develop_old`,
what ships today) and `origin/tarmscreening/develop`. They are **not** part of the four confirmed
features and must be decided individually.

Method: every `EPR.QA.*` unit in `C:\work\FastTrak.Quickstat\FastTrak\` was extracted from
`origin/tarmscreening/develop` and diffed. `EPR.QA.Collector.Demographics.pas`,
`EPR.QA.Collector.Diagnose.pas`, `EPR.QA.Collector.Labdata.pas`, `EPR.QA.Collector.VarSet.pas`,
`EPR.QA.Collection.pas` and `EPR.QA.Collection.Geriatri.pas` are **identical**. The differences
below are everything else, minus the four confirmed features.

> **Encoding note:** the branch files are Windows-1252, the local copies are UTF-8 with BOM. Every
> `ø/å/æ` shows up as a diff. That is an artefact of the extraction, **not** drift. Ignore it.

> ## ⚠ F is superseded — read this first
>
> Everything below §F was written on the assumption that the local `FastTrak\` copies (=
> `develop_old`) are "what ships today". **They are not.** See `PORT-PLAN.md` §2.1: the canonical
> application at `C:\work\FastTrakApps\App.QuickStat\` references symbols that exist *only* on the
> tarmscreening lineage, so `develop_old` cannot build QuickStat at all. Every shipped QuickStat
> binary was built against **tarmscreening**.
>
> Verified at the branch tip of `origin/tarmscreening/develop` (not merely "the commit exists"):
>
> | | tarmscreening tip |
> |---|---|
> | `TDataCollector.VarNames` | `Result := FVarOrder` — **insertion order** |
> | `TFormDataCollector` | `SpSnapshotFormDataAll`, `fMaxBatchSize := 200` |
> | `J01FF%` in the resistance set | **absent** |
> | `StrTitleGbdAceLowGFR` | `'GBD: ACE/A2 og GFR < 35'` (**no** `e`) |
>
> All seven commits cited in this section (`8486b3d09`, `e59a06d3f`, `fefc8a809`, `4c96c3c3b`,
> `9f4a5ed4f`, `8a9954c13`, `109a31c7c`) are ancestors of `origin/tarmscreening/develop`, and none
> of them is an ancestor of `origin/develop`.
>
> **Correction (do not repeat the earlier claim).** An earlier revision of this block said they were
> ancestors of that ref "and of no other branch". That was false — only two refs had been tested.
> `4c96c3c3b` is contained by 27 refs, and 9 remote tips carry `QS_ROAS_BASE`. Only `fefc8a809`
> (interleukins) is narrow, at 3 remote tips.
>
> The verdicts below survive that error because they were re-checked across **all 9** refs that
> carry `QS_ROAS_BASE` — every candidate baseline that could build this application:
>
> | Check | Result across the 9 candidates |
> |---|---|
> | `VarNames` returns `FVarOrder` (insertion order) | **9 / 9** |
> | `J01FF` absent from the resistance set | **9 / 9** |
> | `GFR`, not `eGFR`, in the two GBD renal titles | **9 / 9** |
> | `TFormDataCollector` uses `SpSnapshotFormDataAll` | 5 / 9 — including **both** tarmscreening refs and every candidate newer than 2022-05 |
>
> So these four do not depend on identifying which ref shipped. See `PORT-PLAN.md` §2.1 for the one
> item that *does* — interleukins, which splits `origin/tarmscreening/develop` (131 collectors) from
> `origin/release/tarmscreening` (130).
>
> **Corrected verdicts — these override the per-item "Verdict / recommendation" lines below:**
>
> | Item | Superseded verdict | Corrected verdict |
> |---|---|---|
> | F.1 `SpSnapshotFormDataAll` + batch 200 | don't port; keep `EXEC Report.GetFormData` | **Port it.** Free-text export has shipped since 2022; keeping the proc would *remove* a live feature. The lost proc-side filtering (no `cf.DeletedAt IS NULL`, no item-type filter) is pre-existing production behaviour, not a port regression — record it, don't "fix" it here |
> | F.2 `FVarOrder` insertion order | keep alphabetical | **Port insertion (on-form) order.** Alphabetical would reorder every existing export. Still build it behind `ColumnOrder.FirstSeen \| Alphabetical`, now defaulting to `FirstSeen` |
> | F.3 `RANK` → `ROW_NUMBER` | take it | Unchanged — take it, plus the deterministic tie-breaker. Also what ships |
> | F.4 `SET_BDR_COMORBID` | port local `(3410, …)` | **Moot.** `QST_BDR_COMORBID` is one of the 39 names QuickStat never registers (§A.11) and is dropped from the port entirely (`PORT-PLAN.md` §7.1) |
> | F.5 antibiotic rename + `J01FF%` | keep local title and `J01FF%` | **Flip:** take `'Antibiotika: Resistendrivende'` and drop `J01FF%`. Still confirm with a clinical owner before release (`PORT-PLAN.md` §8.4) |
> | F.6 `GFR` vs `eGFR` | keep local `eGFR` | **Flip to `GFR`** (both titles) for parity. The prose below has the direction backwards: `eGFR` is the *mainline* wording and never shipped in QuickStat. `eGFR` is more correct clinically — raise it as an improvement, don't apply it silently |
> | F.7 mainline-only MNA→MST | out of scope | Unchanged — mainline-only, never shipped |
>
> The per-item analysis below (diffs, SQL, impact) remains accurate and is still the reference; only
> the *verdicts* were computed against the wrong baseline.

**Default recommendation for all of F: port the local (shipping) behaviour.** Exceptions are called
out per item. *(Superseded — see the block above. "Local" is not "shipping".)*

### F.1 `TFormDataCollector` → `SpSnapshotFormDataAll`, batch 200

*Origin:* `8486b3d09` (2022-05-06) *"#489525: QuickStat skal kunne vise og eksportere tekstdata fra
skjema."* Never merged to mainline.

`EPR.QA.Collector.Standard.pas`:

```diff
 const
-  QRY_FORM_DATA      = 'EXEC Report.GetFormData :PersonId, %s';
   QRY_FORM_INSTANCES = 'EXEC Report.GetFormInstances :PersonId';
 
 constructor TFormDataCollector.Create(...)
 begin
   inherited Create( PREFIX_FORM + ACollectorName, ATitle, AFactory, ADb, ALog );
   FVarPrefix := Format( '%s.', [AFormName] );
-  FSQL := Format( QRY_FORM_DATA, [QuotedStr( AFormName )] );
+  FSQL := SpSnapshotFormDataAll( AFormName );
+  fMaxBatchSize := 200;
 end;
```

New function in `EPR.QA.SQL.pas`:

```pascal
function SpSnapshotFormDataAll( const AFormName: string ): string;
const
  QRY_FORMDATA_ALL =
  { } 'SELECT agg.* FROM ' +
  { } '( ' +
  { } '  SELECT ce.PersonId, mi.VarName, ISNULL(dp.Quantity,DATEDIFF(DD,''1899-12-30'',dp.DTVal)) AS DataValue, ce.EventTime, dp.RowId, dp.TextVal AS Caption, mfi.OrderNumber, ' +
  { } '    ROW_NUMBER() OVER ( PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC ) AS OrderBy ' +
  { } '  FROM dbo.ClinDatapoint dp ' +
  { } '    JOIN dbo.ClinEvent ce ON ce.EventId = dp.EventId ' +
  { } '    JOIN dbo.ClinForm cf ON cf.EventId = ce.EventId ' +
  { } '    JOIN dbo.MetaForm mf ON mf.FormId = cf.FormId ' +
  { } '    JOIN dbo.MetaItem mi ON mi.ItemId = dp.ItemId ' +
  { } '    JOIN dbo.MetaFormItem mfi ON mfi.FormId = cf.FormId AND mfi.ItemId = mi.ItemId ' +
  { } '  WHERE ( mf.FormName = %s ) ' +
  { } '  AND ( ce.PersonId IN ' + PID_LIST_PLACEHOLDER + ' )' +
  { } ') agg ' +
  { } 'WHERE agg.OrderBy = 1 ORDER BY agg.OrderNumber';
begin
  Result := Format( QRY_FORMDATA_ALL, [QuotedStr( AFormName )] );
end;
```

**Behavioural difference:**
1. Replaces an opaque stored-procedure call (`Report.GetFormData`) with inline SQL. Whatever
   business logic lives in that proc — deleted-form filtering, permission checks, item-type
   filtering — is **lost**. Note the new query has no `cf.DeletedAt IS NULL` predicate and no
   `mi.ItemType IN (1,2,5)` filter (unlike `SpSnapshotFormDataNumeric`), so it returns **all** item
   types including free text.
2. Adds `dp.TextVal AS Caption`, which is the point of the change: free-text answers become visible
   and exportable (as the `Caption` of the datapoint; the numeric cell value stays
   `ISNULL(Quantity, excel-date)`).
3. Turns N round trips into ceil(N/200).
4. Adds `ORDER BY agg.OrderNumber` — see F.3.

**Verdict / recommendation:** *behaviour change plus performance fix, entangled.* Do **not** port it
as-is. Port the shipping `EXEC Report.GetFormData :PersonId, '<form>'` for behavioural fidelity, then
raise the batching separately (§C.4 item 6), ideally as a `Report.GetFormDataBatch` proc that keeps
the proc's logic. The free-text feature (#489525) is a genuine product request — schedule it, don't
smuggle it in.

### F.2 `TDataCollector.VarNames` returns insertion order (`FVarOrder`)

*Origin:* `e59a06d3f` (2021-05-11) *"#416959: Sortering endret til samme som i skjema ved innsamling
til nøkkeltallsbilde, batch size økt til 200."* Never merged to mainline.

`EPR.QA.Collector.Base.pas`:

```diff
   strict private
     FVarList: TStringList;
+    FVarOrder: TStringList;
...
   FVarList.Sorted := true;
   FVarList.Duplicates := dupIgnore;
+  FVarOrder := TStringList.Create;
...
           FVarList.Add( variableName );
+          if FVarList.Count > FVarOrder.Count then
+            FVarOrder.Add( variableName );
...
 function TDataCollector.VarNames;
 begin
-  Result := FVarList;
+  Result := FVarOrder;
 end;
```

**Behavioural difference:** today the grid columns produced by a collector are in **alphabetical**
order (`FVarList.Sorted := True`). With the change they are in **first-seen** order, which — because
`SpSnapshotFormDataAll` adds `ORDER BY mfi.OrderNumber` (F.3) — means "the order the items appear on
the form". `FVarList` is kept as the dedupe set; `FVarOrder` is the ordered projection.

This changes the **column order of every exported CSV/Excel sheet**, for every collector, not just
form data. Any downstream script that reads by column position breaks.

**Verdict / recommendation:** *deliberate UX improvement, but a breaking output change.* Port the
shipping alphabetical behaviour by default, and implement `VarNames` as an ordered, de-duplicating
collection (`OrderedSet<string>`) so switching to insertion order is a one-line policy change later.
In C#: keep a `List<string>` + `HashSet<string>` behind one type, and expose
`ColumnOrder.Alphabetical | ColumnOrder.FirstSeen` as a setting defaulting to `Alphabetical`.

### F.3 `QRY_FORMDATA_NUMERIC`: `RANK` → `ROW_NUMBER`, plus `OrderNumber`

*Origin:* same commit `8486b3d09`.

```diff
-  SELECT ce.PersonId, mi.VarName, ISNULL(dp.Quantity,DATEDIFF(DD,'1899-12-30',dp.DTVal)) AS DataValue, ce.EventTime, dp.RowId,
-    RANK() OVER ( PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC ) AS OrderBy
+  SELECT ce.PersonId, mi.VarName, ISNULL(dp.Quantity,DATEDIFF(DD,'1899-12-30',dp.DTVal)) AS DataValue, ce.EventTime, dp.RowId, mfi.OrderNumber,
+    ROW_NUMBER() OVER ( PARTITION BY ce.PersonId, mi.ItemId ORDER BY ce.EventNum DESC ) AS OrderBy
...
-) agg WHERE agg.OrderBy = 1
+) agg WHERE agg.OrderBy = 1 ORDER BY agg.OrderNumber
```

**Behavioural difference:**
* `RANK()` assigns the same rank to ties; with two datapoints for the same item in the *same*
  `ce.EventNum`, `WHERE OrderBy = 1` returns **both** rows. `ROW_NUMBER()` returns exactly one
  (arbitrarily chosen among ties, since there is no tie-breaker column). Given `AddDatapoint`
  rejects duplicates and the extra datapoint is freed, the *visible* result is usually the same —
  but with `RANK` the winner is the first row the server happens to return, and with `ROW_NUMBER`
  it is the first row the window function happens to pick. Neither is deterministic; `ROW_NUMBER` at
  least means one row per (person, item) leaves the server.
* `ORDER BY agg.OrderNumber` makes the row stream follow the on-form item order, which only matters
  in combination with F.2.
* `mfi.OrderNumber` is added as a 6th output column — harmless for the positional contract.

**Verdict / recommendation:** the `RANK` → `ROW_NUMBER` change **is an obvious bug fix** (it removes
duplicate rows that the client then has to throw away) and is safe: it cannot change which value
lands in a cell in any case where `RANK` produced a unique winner, and where it did not, the old
behaviour was already arbitrary. **Take this one.** Add a deterministic tie-breaker while you are
there — `ORDER BY ce.EventNum DESC, dp.RowId DESC` — so golden tests and reruns agree. The
`ORDER BY agg.OrderNumber` part should ride with F.2, not on its own.

Note this only affects `TFormDataNumericCollector`, which is **dead code** in the shipping app
(§B.4). Low risk either way.

### F.4 `SET_BDR_COMORBID` — one item swapped

*Origin:* `109a31c7c` (2021-09-09) *"#424746: 9478 er ny cøliaki-variabel; oppdaterer nøkkeltal"*.
Never merged to mainline.

```diff
-  SET_BDR_COMORBID: array [0 .. 6] of integer      = ( 3410, 6312, 6313, 3364, 3355, 3356, 3357 );
+  SET_BDR_COMORBID: array [0 .. 6] of integer      = ( 6312, 6313, 3364, 3355, 3356, 3357, 9478 );
```

**Behavioural difference:** item `3410` is dropped and item `9478` (a new coeliac-disease variable)
is added; the remaining six shift position. The array length is unchanged. Consumed by
`QST_BDR_COMORBID` → `TVarSetCollector.CreateForEnum` → `SpSnapshotEnum`, so the generated
`cdp.ItemId IN ( … )` list changes, and one grid column is replaced by another.

**Impact on QuickStat: none today.** `QST_BDR_COMORBID` is one of the 39 factory names QuickStat
never registers (§A.11) — it belongs to the Barnediabetes (BDJ) collection, which lives in
`EPR.QA.Collection.Barnediabetes.pas`, a unit that is not part of this repo.

**Verdict / recommendation:** *data-definition change, owned by the BDJ registry.* Port the local
(shipping) value `(3410, 6312, 6313, 3364, 3355, 3356, 3357)`. Flag to the BDJ owner that a coeliac
variable `9478` exists and was intended to replace `3410`; if they confirm, it is a one-line array
edit in the ported registry.

### F.5 Antibiotic-caption family rename

Covered in detail in §E.2.1 — repeated here because it is *drift on an existing collector*, not part
of the recovery:

* `SpDrugsetAntibiotic` → `SpDrugsetAntibioticResistance` (pure rename)
* `StrTitleDrugRestistanceAntibiotic` = `'Medisin: Resistensdrivende antibiotika'` →
  `StrTitleDrugAntibioticResistance` = `'Antibiotika: Resistendrivende'` (**user-visible**)
* `J01FF%` dropped from the resistance-driving ATC set (**clinically meaningful**)

**Verdict / recommendation:** port the shipping title and the shipping ATC set (including `J01FF%`);
treat the rename and the clause removal as one decision for the clinical owner. Note the tarmscreening
caption is also misspelled (`Resistendrivende`, missing an `s`).

### F.6 `StrTitleGbdAceLowGFR` / `StrTitleGbdMetforminLowGFR` — `GFR` vs `eGFR`

```diff
-  StrTitleGbdAceLowGFR = 'GBD: ACE/A2 og GFR < 35';
-  StrTitleGbdMetforminLowGFR = 'GBD: Metformin og GFR < 50 ';
+  StrTitleGbdAceLowGFR = 'GBD: ACE/A2 og eGFR < 35';
+  StrTitleGbdMetforminLowGFR = 'GBD: Metformin og eGFR < 50 ';
```

Here the **local copy is the newer, better** text (`eGFR` — estimated GFR, which is what
`Report.ColDrugAndRenalFunction` actually returns). The tarmscreening branch predates the wording
fix. **Verdict: keep the local strings** (including the trailing space in the metformin one, or
trim it deliberately — it is invisible in the list box).

### F.7 Mainline-only drift you will hit if you re-sync `EPR.QA.Definitions.pas`

Not a tarmscreening difference, but discovered while diffing: current mainline `develop` has moved
past `develop_old` (= the local copy) in two places.

```diff
-  SET_GBD_NUTRITION: array [0 .. 3] of integer       = ( 4353, 4354, 4529, 4771 );
+  SET_GBD_NUTRITION: array [0 .. 3] of integer       = ( 4353, 4354, 4529, 12584 );
-  SET_MNA_PART1: array [0 .. 0] of integer           = ( 4771 );
+  SET_MST_SCORE: array [0 .. 0] of integer           = ( 12584 );
```

MNA (item 4771) is being replaced by MST (item 12584) as the nutrition screening score. Both
`SET_GBD_NUTRITION` and `SET_MNA_PART1` feed collectors QuickStat never registers
(`QS_GBD_NUTRITION`, `QS_ITEMAGE_MNA_PART1`), so there is no QuickStat-visible effect — **but**
QuickStat *does* register `QS_GBD_MNA_6M`, which hard-codes item `4771` in
`SpRecentQuantityPresent( 4771, 6 )`, and `SET_GBD_SCORES` also contains `4771`. If MNA is being
retired, those two need the same treatment.

**Verdict / recommendation:** out of scope for the port; port the local values. Raise as a question:
"is item 4771 (MNA) still collected, or should GBD collectors move to 12584 (MST)?"

### F.8 Non-differences, recorded so nobody re-investigates

* `EPR.QA.Collector.Names.pas` on the branch has a duplicated `{ Collector names }` comment line and
  slightly different constant alignment. Cosmetic.
* `System.Generics.Collections` vs `Generics.Collections` in unit `uses` clauses — introduced by
  `fix-namespaces.ps1` in this repo. Irrelevant to C#.
* UTF-8 BOM present locally, absent on the branch. Irrelevant.
* `EPR.QA.Collector.Factory.pas`, `.Drug.pas`, `.Names.pas`, `.Labdata.pas`, `.VarSet.pas`,
  `.Diagnose.pas`, `.Demographics.pas` are byte-identical to `develop_old`. The local tree has **no
  local modifications** to the library — it is a clean extraction.

---

## G. Proposed C# design

Namespace `Quickstat.Collectors`, flat repo layout, `net10.0-windows`, CommunityToolkit.Mvvm for the
view models (the collector layer itself is **UI-free and DI-friendly** — it must be testable without
WPF and without a database).

### G.1 Shape of a collector

Replace the 13-class hierarchy with **one record + a SQL builder delegate**. Nothing in
`TDataCollector`'s subclasses varies except `(name, title, varPrefix, batchSize, sql)`; the
inheritance exists only because Delphi has no first-class functions in that codebase.

```csharp
namespace Quickstat.Collectors;

public enum CollectorKind      // provenance only: drives docs, grouping and golden-file naming
{
    Demographics, StudyCase, FormInstance, FormData, FormAge, FormCount, FormCompleteness,
    VarSet, VarSetAge, VarSetMax, LabSet, LabTrust, LabCount, Diagnose, DiagnoseCount,
    Drug, DrugSet, DrugInteraction, Custom
}

/// How the person-id list reaches the server for this collector.
public enum PidBinding
{
    None,          // query is global; rows for other persons are filtered client-side
    SinglePerson,  // legacy ":PersonId" one-round-trip-per-patient
    IdList         // query contains {IdList}
}

public sealed record CollectorDescriptor
{
    public required string Name          { get; init; }   // "LAB.ANEMIA" — stable, persisted in packages
    public required string Title         { get; init; }   // exact Norwegian string shown in the list
    public required CollectorKind Kind   { get; init; }
    public required string VarPrefix     { get; init; }   // "" | "ATC_" | "DX." | ...
    public required PidBinding Pid       { get; init; }
    public int  BatchSize                { get; init; } = 100;
    public StudyGate Gate                { get; init; } = StudyGate.Always;
    /// Builds the SQL. ctx supplies StudyId and the {IdList} fragment chosen by IPidListStrategy.
    public required Func<SqlBuildContext, string> Sql { get; init; }
    /// Optional: only register when this database object resolves (see E.1).
    public string? RequiresObject        { get; init; }
}

public readonly record struct SqlBuildContext(int StudyId, string IdListFragment);
```

`SqlBuildContext.IdListFragment` is what `{IdList}` expands to: `"(SELECT PersonId FROM @pids)"`
for the TVP strategy, `"(1,2,3)"` for the literal strategy, `"(/*PIDS*/)"` in tests.

**Why a `Func<>` and not an `ISqlSource` interface:** 34 collectors need a constant string, ~30 need
`string.Format` over one or two arguments, and ~10 need the study id. A delegate covers all three
with no ceremony, is trivially unit-testable, and keeps the registry declarative. Reach for an
interface only if a collector ever needs state beyond the context.

### G.2 The SQL library

Port `EPR.QA.SQL.pas` to a single static class of pure string functions, one per `Sp*`:

```csharp
internal static class QaSql
{
    public const string PidList  = "{IdList}";
    public const string ItemList = "{ItemList}";
    public const string LabList  = "{LabList}";
    public const string FormName = "{FormName}";

    public const string Collation             = " COLLATE Latin1_General_CI_AI ";
    public const string WherePersonList       = "WHERE ( PersonId IN {IdList} ) ";
    public const string FromOngoingTreatment  = "FROM dbo.OngoingTreatment ot ";
    public const string JoinAtcIndex          = "LEFT JOIN dbo.KBAtcIndex ai ON ai.AtcCode = ot.ATC ";

    public static string SnapshotLabSet(ReadOnlySpan<int> labClassIds) => …;
    public static string SnapshotVarSet(CrfVarType type, ReadOnlySpan<int> itemIds) => …;
    public static string DiagnoseByPattern(string pattern) => …;
    // …one per Sp* function in EPR.QA.SQL.pas
}
```

Rules:

* Use **raw string literals** (`"""…"""`) so the SQL is readable and diffable. Do not reproduce the
  Delphi `{ } '…' +` concatenation style.
* Preserve the SQL **verbatim**, whitespace included, on first port. Reformatting and golden-file
  regeneration are separate commits. A reviewer must be able to diff generated SQL against a Delphi
  trace.
* Replace every Delphi `Format` slot with an interpolated hole or an explicit
  `string.Format(CultureInfo.InvariantCulture, …)`. Note `SpSnapshotQuantityIfBelowThreshold` uses
  `%g` with explicit `en-US` settings — use `value.ToString("G", CultureInfo.InvariantCulture)`.
* `QuotedStr` → a single helper `SqlLiteral.Quote(string)` that doubles embedded apostrophes. Every
  current call site passes a constant or a DB-supplied form name, but centralising it is free.
* `ConvertArrayToList(int[])` → `string.Join(", ", ids)` — the Delphi output is `", "` separated,
  so the spacing matches.
* `ConvertAtcPatternToVariableName` → one small function with the two regex replacements; it must be
  byte-identical because it produces collector **names** that are persisted in saved packages.

### G.3 Keeping ~150 registrations readable

Split the registry by domain, one file per group, each exposing a `static IEnumerable<CollectorDescriptor>`.
Use small static factory helpers that mirror the Delphi constructors so each registration is one
line — this is the single most important readability decision.

```csharp
// Quickstat.Collectors/Registry/LabCollectors.cs
internal static class LabCollectors
{
    public static IEnumerable<CollectorDescriptor> All =>
    [
        Make.LabSet(Names.LabKidney,       "Nyrefunksjon",           Ids.LabKidney),
        Make.LabSet(Names.LabAnemia,       "Anemi",                  LabClasses.Anemia),
        Make.LabSet(Names.LabLipids,       "Lipider",                LabClasses.Lipids),
        Make.LabSet(Names.LabInterleukins, "Interleukiner",          LabClasses.Interleukins),  // E.4
        …
        Make.LabTrust(Names.LabHigh,   "Labdata: Alle med høy konfidens",     trustLevel: 3),
        Make.LabCount(Names.LabCount3M,"Labdata: Antall prøver siste 3 mnd",  months: 3),
    ];
}
```

`Make.LabSet` applies the title rule from §0.4 exactly once, in one place:

```csharp
internal static CollectorDescriptor LabSet(string name, string groupName, params int[] labClassIds) =>
    new()
    {
        Name      = name,
        Title     = groupName.Contains(':') ? groupName : $"Labdata: {groupName} (siste)",
        Kind      = CollectorKind.LabSet,
        VarPrefix = "",
        Pid       = PidBinding.IdList,
        BatchSize = 100,
        Sql       = ctx => QaSql.SnapshotLabSet(labClassIds).Replace(QaSql.PidList, ctx.IdListFragment)
    };
```

Suggested files (roughly matching the Delphi grouping, which is also how the checkbox list reads):

```
Quickstat.Collectors/
  CollectorDescriptor.cs        SqlBuildContext.cs      StudyGate.cs
  CollectorRegistry.cs          IPidListStrategy.cs     CollectorRunner.cs
  Sql/QaSql.cs                  Sql/DrugSql.cs          Sql/SqlLiteral.cs
  Registry/Make.cs              Registry/Names.cs       Registry/Titles.cs
  Registry/Ids.cs               Registry/LabClasses.cs
  Registry/BasicCollectors.cs   Registry/LabCollectors.cs
  Registry/FormCollectors.cs    Registry/DiagnoseCollectors.cs
  Registry/DrugCollectors.cs    Registry/GbdCollectors.cs
  Registry/NdvCollectors.cs     Registry/RoasCollectors.cs
  Registry/UnusedCollectors.cs  // the 39 from A.11, kept for completeness
```

`Names.cs`, `Titles.cs`, `Ids.cs`, `LabClasses.cs` are pure constant classes — direct transcriptions
of `EPR.QA.Collector.Names.pas` and `EPR.QA.Definitions.pas`. Keep the Delphi identifier names as
XML doc comments (`/// <remarks>QST_LAB_ANEMIA</remarks>`) so the two codebases stay greppable
against each other during the migration.

Norwegian titles go in **`Titles.cs` as plain constants**, not `.resx`, unless localisation is
actually planned. The Delphi `resourcestring` mechanism was never used for translation here, and
`.resx` would make the golden-file tests indirect for no benefit.

### G.4 Study gating

```csharp
[Flags]
public enum StudyGate
{
    Always  = 0,
    Gbd     = 1 << 0,   // GBD|LANGTID|KORTTID
    Ndv     = 1 << 1,   // NDV|ENDO|LANGTID|GBD|KORTTID
    Gwas    = 1 << 2,   // GWAS
    Roas    = 1 << 3,   // ROAS
    Dogfood = 1 << 4    // DOGFOOD, case-insensitive
}

public static class StudyGates
{
    static readonly (StudyGate Gate, Regex Rx)[] Patterns =
    [
        (StudyGate.Gbd,     new Regex("GBD|LANGTID|KORTTID",          RegexOptions.Compiled)),
        (StudyGate.Ndv,     new Regex("NDV|ENDO|LANGTID|GBD|KORTTID", RegexOptions.Compiled)),
        (StudyGate.Gwas,    new Regex("GWAS",                          RegexOptions.Compiled)),
        (StudyGate.Roas,    new Regex("ROAS",                          RegexOptions.Compiled)),
        (StudyGate.Dogfood, new Regex("DOGFOOD", RegexOptions.Compiled | RegexOptions.IgnoreCase)),
    ];

    public static StudyGate For(string studyName) =>
        Patterns.Where(p => p.Rx.IsMatch(studyName))
                .Aggregate(StudyGate.Always, (acc, p) => acc | p.Gate);
}
```

`CollectorRegistry.Build(studyName, formClasses, dbCapabilities)` then:

1. emits the always-on descriptors in source order,
2. appends `2 × N` form descriptors from `Report.GetFormClasses` (skipping `FORM\d+`, deduped),
3. appends each gated group whose flag is set, **in the Delphi order**
   (`Gbd` → its Diagnose and Drug sub-groups → `Ndv` → `Gwas` → `Roas` → `Dogfood`),
4. drops descriptors whose `RequiresObject` does not resolve.

**Order is part of the contract** — it is the order of the checkbox list the users know, and
`TryFindCollector` picks the first match on a duplicate name. Preserve it, and assert it in a test.

`StudyGate.Always = 0` makes `descriptor.Gate == StudyGate.Always` mean "ungated"; the filter is
`d.Gate == StudyGate.Always || (active & d.Gate) != 0`.

### G.5 Execution

```csharp
public interface ICollectorRunner
{
    Task RunAsync(CollectorDescriptor d, IReadOnlyList<int> personIds,
                  int studyId, IDataTarget target, CancellationToken ct);
}
```

`CollectorRunner` owns exactly what `TDataCollector.RunBatch` owns:

* chunk `personIds` by `min(d.BatchSize, pidStrategy.MaxIdsPerBatch)` (or one shot when
  `Pid == PidBinding.None`),
* build SQL via `d.Sql(ctx)`, bind via `IPidListStrategy`,
* read with `SqlDataReader` **by ordinal** 0–4, plus `GetOrdinal("ItemId")` / `GetOrdinal("Caption")`
  resolved once per reader (cache the ordinals; `TryGetOrdinal` returning `-1` when absent),
* drop rows whose `PersonId` is not in the chunk and count them (keep the
  `Unknown patients found, n = …` log — it is a real diagnostic),
* accumulate distinct column names into an `OrderedSet<string>` (§F.2).

Everything about the shape of the read is identical for all 126 collectors — 131 once §E's four
features are restored — which is the strongest argument that the class hierarchy should not survive
the port.

Use `Microsoft.Data.SqlClient` with `CommandBehavior.SequentialAccess` and async reads. Do **not**
reuse a single `SqlCommand` across collectors the way `TSimpleDatabase` reuses one `TADOQuery` —
that is the source of the fragile positional parameter binding in `PrepareQueryParameters`.

### G.6 Testing — golden files over generated SQL

**Strongly recommended, and cheap.** The entire subsystem is a pure function
`(studyName, formClasses, studyId) → (ordered list of descriptors, one SQL string each)`. Test that
function; the database adds nothing.

**Test 1 — SQL golden files.** One file per collector under
`tests/Quickstat.Collectors.Tests/Golden/Sql/{Name}.sql`, where `{Name}` is the collector name with
`.` → `_`:

```csharp
public sealed class SqlGoldenTests
{
    public static TheoryData<string> AllCollectors => …;   // every descriptor, all gates on

    [Theory, MemberData(nameof(AllCollectors))]
    public void Sql_matches_golden(string collectorName)
    {
        var d   = TestRegistry.All.Single(x => x.Name == collectorName);
        var ctx = new SqlBuildContext(StudyId: 42, IdListFragment: "(/*PIDS*/)");
        var sql = d.Sql(ctx);

        GoldenFile.Verify($"Sql/{collectorName.Replace('.', '_')}.sql", sql);
    }
}
```

`GoldenFile.Verify` writes the file and fails with a diff when
`Environment.GetEnvironmentVariable("UPDATE_GOLDEN") != "1"`, and simply overwrites when it is `1`.
Fixed `StudyId` and a fixed `IdListFragment` make the output deterministic — no `GETDATE()` problem,
because the dates are inside the SQL text, not evaluated.

*Why golden files rather than assertions:* nobody will hand-write an assertion for a 20-line
`UNPIVOT`. A golden file makes every SQL change visible in review, which is exactly the control you
want when the source of truth is a Delphi program nobody will run again. Seed the golden files from
the **Delphi** output once (instrument `TDataCollector.SQL` to dump to disk, or transcribe from this
document) so the first green run proves fidelity rather than self-consistency.

**Test 2 — registry golden files.** One file per representative study name:

```
Golden/Registry/GBD.txt          Golden/Registry/NDV.txt
Golden/Registry/KORTTID.txt      Golden/Registry/ROAS_GWAS.txt
Golden/Registry/DOGFOOD.txt      Golden/Registry/UNKNOWN.txt
```

each containing `Name<TAB>Title<TAB>Kind<TAB>VarPrefix<TAB>BatchSize`, one line per collector, **in
registration order**. This is the acceptance checklist from §A made executable: adding, removing,
renaming or re-ordering a collector shows up as a reviewable diff, and `KORTTID.txt` having 120 lines
is the regression test for commit `5502b72`.

**Test 3 — SQL parses.** Add `Microsoft.SqlServer.TransactSql.ScriptDom` and parse every generated
statement:

```csharp
[Theory, MemberData(nameof(AllCollectors))]
public void Sql_parses_and_projects_at_least_five_columns(string collectorName) { … }
```

Assert (a) zero parse errors, (b) for non-`EXEC` statements, the outermost `SELECT` has ≥ 5 output
elements. This catches exactly the class of bug the Delphi code is prone to — an unescaped `%` in a
`Format`, a missing space between concatenated fragments, an unbalanced quote — **without a
database**, and it would have caught the `KB.AntibioticRestistance2` typo class of error only if the
name were validated, which it cannot be, so keep the runtime probe from §E.1 too.

**Test 4 — pure-function invariants.** Cheap table tests for the pieces everything depends on:
`ConvertAtcPatternToVariableName` (`C0[23789]%` → `C0x23789` etc.), `ConvertArrayToList`,
`SqlLiteral.Quote`, the title-suffix rules of §0.4, and `StudyGates.For` (§D.3).

**Test 5 — integration, opt-in.** A single `[Trait("Category","Database")]` test that, given a
connection string in an environment variable, executes every generated SQL with an empty/one-element
pid list and asserts it does not throw and returns ≥ 5 columns. This is the only thing that can
validate the `Report.*` stored procedures and the `KB.` / `Diagnose.` / `dbo.GetLastQuantityTable`
dependencies. Excluded from CI by default; run it against each customer database shape before
release.

### G.7 Migration order (suggested)

1. `Names.cs` / `Titles.cs` / `Ids.cs` / `LabClasses.cs` — pure transcription, no logic. Verify
   against §A and §E by eye and by count.
2. `QaSql` + `DrugSql` — transcribe every `Sp*` / `QRY_*`. Golden-file each one as you go.
3. `CollectorDescriptor`, `Make`, the registry files, `StudyGates`. Registry golden files.
4. `IPidListStrategy` (TVP + literal fallback) and `CollectorRunner`.
5. Apply §E (the four recovered features) as **one commit per feature**, each adding its golden
   files, so they can be reverted independently if a customer database lacks `KB.AntibioticResistance2`.
6. Only then take the §F decisions, each as its own commit with a regenerated golden diff.

Do **not** collapse steps 1–3 into "write the registry"; the whole value of the golden-file approach
is that the SQL is frozen before the registry that consumes it starts moving.

