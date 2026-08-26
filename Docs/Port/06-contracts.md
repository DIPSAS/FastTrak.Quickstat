# 06 — Phase 1 contracts: the type surface of `QuickStat.Core`

Status: **complete**. Produced by Phase 1, step 1.1 (PORT-PLAN.md §5).
Branch: `feature/dotnet`.

This is the document a Phase 2 agent consults to find out **which files it owns**. Everything
listed here exists in the tree, compiles with zero warnings, and has XML documentation.

---

## 0. How to use this document

1. Find your step in §2. Those files, and only those files, are yours to edit.
2. Everything else in `QuickStat.Core/` is a **read-only dependency**. Reference it freely; do not
   edit it. If you need a contract changed, say so in your report — do not change it yourself.
3. Contract types are declared, not implemented. Interfaces, records and enums have no bodies at
   all; the three concrete classes (`SqlResultSet`, `SqlRow`, `PersonMatrix`, `VariableNameSet`,
   `DataPoint`) have members that `throw new NotImplementedException()`. Filling those in is the
   job of the step that owns the file.
4. Four members are **implemented** rather than stubbed, because the one-line body *is* the
   contract and a stub would let two steps disagree. They are called out in §4.

### The ownership rule

**Every file has exactly one Phase 2 owner, and ownership follows the folder.** That is what makes
Phase 2 safely parallel: no two agents ever open the same file, so there are no merge conflicts and
no lost edits.

| Folder | Owner |
|---|---|
| `QuickStat.Core/Configuration/*` (files directly in it) | **2.1** |
| `QuickStat.Core/Configuration/Settings/*` | **2.7** |
| `QuickStat.Core/Data/*` | **2.2** |
| `QuickStat.Core/Domain/Populations/*` | **2.3** |
| `QuickStat.Core/Domain/Patients/*` | **2.3** |
| `QuickStat.Core/Domain/Packages/*` | **2.3** |
| `QuickStat.Core/Collectors/*` | **2.4** |
| `QuickStat.Core/Domain/Matrix/*` | **2.5** |
| `QuickStat.Core/Domain/DataPoints/*` | **2.5** |
| `QuickStat.Core/Domain/Anonymisation/*` | **2.6** |
| `QuickStat.Core/Export/*` | **2.6** |
| `QuickStat.Core/Diagnostics/*` | **2.7** |

There is deliberately **no shared `Common/` folder**. A file two steps both want to edit is exactly
the failure this map exists to prevent; if a type does not fit, it goes in the most-coupled folder
and the reason is recorded in §3.

Namespaces follow folders (`RootNamespace` is `QuickStat`, and `IDE0130` is enforced as an error),
so `QuickStat.Core/Domain/Matrix/PersonMatrix.cs` is `QuickStat.Domain.Matrix.PersonMatrix`.

---

## 1. Hard constraints these contracts encode

| Constraint | How it is expressed |
|---|---|
| `QuickStat.Core` never references WPF | Colours are `QuickStat.Domain.DataPoints.Rgb`. The App layer converts to `System.Windows.Media.Color`. Nothing in `Core` touches `PresentationCore`/`WindowsBase`. |
| Contracts never expose `Microsoft.Data.SqlClient` | `IConnectionStringTranslator` returns `ResolvedConnectionString` (a string pair), not `SqlConnectionStringBuilder`. `SqlResultSet`/`SqlRow`/`SqlColumn` replace `SqlDataReader`. Table-valued parameters are `SqlTableParameter`, not `SqlDbType.Structured`. |
| Column order is insertion order | `VariableNameSet` + `ColumnOrder`, defaulting to `FirstSeen`. |
| Patient-id lists use a TVP | `SqlRequest.TableParameters` + `SqlTableParameter`; type name in `SqlOptions.PersonIdListTypeName`, chunk fallback in `SqlOptions.MaxIdsPerBatch`. |
| Optional collectors | `CollectorDescriptor.Availability` → `CollectorAvailability` (+ `CollectorAvailabilityContext`). |
| Study gating is exact | `StudyGatePatterns` holds the five regexes verbatim; `StudyGate` is `[Flags]` so gates compose. |
| Title suffixing is a rule | `CollectorTitle` — one home for `' (siste)'`, `' (høyeste)'` and the conditional `'Labdata: %s (siste)'` wrapper. |
| Display and export anonymity share one truth | `IIdentificationPolicy` (one instance) + `IdentificationColumns.For` (one derivation). `DatasetExportOptions.Columns` is derived, not settable. |
| Periods are `[Start, Stop)` | The type is called `HalfOpenPeriod`, with `Start` and `Stop`. |
| `ILog` splits in two | `Microsoft.Extensions.Logging.ILogger<T>` for logging; `IUserNotifier` for asking. `IUserNotifier.ConfirmAsync` documents that it must never fail open. |
| CSV parity is byte-level | `DatasetExportOptions` carries the specification as constants: `LegacySeparator`, `LegacyCodePage`, `TimestampColumnSuffix`, `TimestampFormat`, `KeyFileExtension`. Options are a record, not method parameters. |

---

## 2. The file map

### 2.1 — Configuration + connection strings → `QuickStat.Core/Configuration/`

Namespace `QuickStat.Configuration`. Reference: `Docs/Port/01-data-access.md` §3.

| File | Type | Promises |
|---|---|---|
| `Configuration/QuickStatConnection.cs` | `sealed record QuickStatConnection` | One `<Connection>` element: `Name`, `StudyName`, the raw OLE DB `ConnectionString`, and the optional .NET-only `SqlOptions` override. |
| `Configuration/IConnectionCatalog.cs` | `interface IConnectionCatalog` | Reads `<exe>.config.xml` into connections; returns **empty** (not an exception) when the file is missing; exposes `DefaultConfigFilePath` resolved from `AppContext.BaseDirectory`. |
| `Configuration/IConnectionStringTranslator.cs` | `interface IConnectionStringTranslator` | Resolves `FILE NAME=…\*.UDL`, maps OLE DB keywords to ADO.NET, applies defaults, and validates that credentials exist. |
| `Configuration/ResolvedConnectionString.cs` | `sealed record ResolvedConnectionString` | An ADO.NET connection string paired with a password-redacted rendering, so no caller has to decide what is safe to log. Records the UDL path that answered. |
| `Configuration/SqlOptions.cs` | `sealed record SqlOptions` | The process-wide knobs: command/connect timeouts, SQL logging, retry policy, injected `Application Name` and encryption defaults, and the person-id TVP type name and chunk size. |
| `Configuration/QuickStatConfigurationException.cs` | `class QuickStatConfigurationException` | One exception for "configuration could not be read or translated", carrying the offending `FilePath`. |

### 2.2 — SQL execution + login pipeline → `QuickStat.Core/Data/`

Namespace `QuickStat.Data`. Reference: `Docs/Port/01-data-access.md` §1, §2, §5.

| File | Type | Promises |
|---|---|---|
| `Data/ISqlExecutor.cs` | `interface ISqlExecutor` | `QueryAsync` / `ExecuteAsync` / `ScalarAsync<T>`. Materialised results, serialised access, real rows-affected. |
| `Data/SqlRequest.cs` | `sealed record SqlRequest` | A statement plus positional **or** named values, table-valued parameters, an optional timeout, an idempotency flag (retry gate) and a log label. |
| `Data/SqlTableParameter.cs` | `sealed record SqlTableParameter` | A single-column `int` table-valued argument: name, type name, column name, values. The only way to pass more than 2 100 ids. |
| `Data/SqlColumn.cs` | `readonly record struct SqlColumn` | Ordinal, name, CLR type. |
| `Data/SqlResultSet.cs` | `sealed class SqlResultSet : IReadOnlyList<SqlRow>` | A buffered result set with `IndexOf` (tolerant, `FindField`) and `GetOrdinal` (strict, `FieldByName`). |
| `Data/SqlRow.cs` | `readonly record struct SqlRow` | Typed accessors that reproduce Delphi `TField` null semantics exactly, including `ZeroDate` = 1899-12-30. |
| `Data/ISqlTextRewriter.cs` | `interface ISqlTextRewriter` | `:Name` → `@Name`, skipping literals, brackets, quoted identifiers and comments. Also used by 2.3 to detect the period pair. |
| `Data/RewrittenSql.cs` | `readonly record struct RewrittenSql` | Rewritten text, distinct placeholder names in first-appearance order, and whether any name repeats. |
| `Data/ILoginStep.cs` | `interface ILoginStep` | One ordered, testable stage of login. Replaces the five unordered `ILoginObserver`s. |
| `Data/LoginContext.cs` | `sealed class LoginContext` | The mutable builder threaded through the pipeline. |
| `Data/SessionContext.cs` | `sealed record SessionContext` | The immutable result: study, session, user, database info — plus `TryGetParameterValue`, the explicit replacement for RTTI placeholder resolution. |
| `Data/StudyUser.cs` | `sealed record StudyUser` | `dbo.GetStudyAndUser`, read once instead of three times. |
| `Data/DatabaseInfo.cs` | `sealed record DatabaseInfo` | Server and schema facts, plus `MinimumDbVersion` (510) and `PopulationsWithVersionDbVersion` (18200). |
| `Data/ISessionService.cs` | `interface ISessionService` | Connect / disconnect / `Current` / `SessionChanged`. Cancellable and progress-reporting. |
| `Data/QuickStatDataException.cs` | `class QuickStatDataException` | Base for the whole data layer; carries `Number`, `Procedure`, `Severity`, `CommandText`. |
| `Data/SqlPrivilegeException.cs` | `sealed class SqlPrivilegeException` | Missing `GRANT`; names `RequiredDatabaseRole = "QuickStat"`. |
| `Data/SqlUserDefinedException.cs` | `sealed class SqlUserDefinedException` | Error number ≥ 50000; the server's own message is the user's message. |
| `Data/SqlCommandFailedException.cs` | `sealed class SqlCommandFailedException` | Everything else. Never raised for `PRINT` output. |
| `Data/SqlParameterBindingException.cs` | `sealed class SqlParameterBindingException` | Count mismatch, unknown name, both binding styles, or positional binding of a repeated placeholder. |
| `Data/DatabaseNotConnectedException.cs` | `sealed class DatabaseNotConnectedException` | A statement before any project was selected. |
| `Data/DatabaseVersionTooOldException.cs` | `sealed class DatabaseVersionTooOldException` | `0 < DbVersion < 510`. |

### 2.3 — Populations, patients, packaged selections → `QuickStat.Core/Domain/{Populations,Patients,Packages}/`

Reference: `Docs/Port/02-populations-patients.md`.

| File | Type | Promises |
|---|---|---|
| `Domain/Populations/Population.cs` | `sealed record Population` | One catalogue row. `SearchText` reproduces the tab-joined filter string field for field. |
| `Domain/Populations/IPopulationRepository.cs` | `interface IPopulationRepository` | Catalogue load (three procedure variants keyed on `DbVersion` and the "frequently used" flag) and the fire-and-forget `dbo.AddPopulationLog` audit. |
| `Domain/Populations/IQueryParameterResolver.cs` | `interface IQueryParameterResolver` | Resolves a population's placeholders; period prompt first, session values second. Holds the two placeholder names that trigger the prompt. |
| `Domain/Populations/ParameterResolution.cs` | `sealed record ParameterResolution` | Success, values, and — the part the Delphi could not express — *why* it failed: user cancel versus unknown placeholder. |
| `Domain/Populations/IPeriodPrompt.cs` | `interface IPeriodPrompt` | Asks for a period and remembers the last one per population. Implemented by the UI; declared here because 2.3 needs the answer. |
| `Domain/Populations/HalfOpenPeriod.cs` | `readonly record struct HalfOpenPeriod` | `[Start, Stop)`. `IsValid` is strict `Start < Stop`. |
| `Domain/Populations/PopulationSchemaException.cs` | `sealed class PopulationSchemaException` | A population that omits `FullName` fails loudly instead of returning zero patients silently. |
| `Domain/Patients/Patient.cs` | `sealed class Patient` | One patient; split first/last name, settable `NationalId`, and `DisplayName` = `"Last, First"`. |
| `Domain/Patients/Sex.cs` | `enum Sex` | `Unknown`/`Male`/`Female` from `GenderId`. |
| `Domain/Patients/IPatientRepository.cs` | `interface IPatientRepository` | Population load, default case list, TVP-backed national-id recovery, free-text search. |
| `Domain/Packages/PackagedSelection.cs` | `sealed record PackagedSelection` | A saved dataset specification. `CollectorNames` is a persistence format, so collector names cannot change freely. |
| `Domain/Packages/IPackageRepository.cs` | `interface IPackageRepository` | `Report.QuickStat` read, `Report.AddQuickStat` write, `QuickStat.DeletePackage` delete. |

> **`Domain/Packages/` needs a `.gitignore` negation and already has one.** The repository's NuGet
> rule `packages/` matches this folder (git is case-insensitive on Windows), so the files compile —
> MSBuild globs the file system — but are invisible to git and would be lost on commit. The
> negation `!/QuickStat.Core/Domain/Packages/` sits immediately after the `packages/` rule, because
> a negation only works after the pattern it overrides. **Do not move or remove it**, and do not
> rename the folder.

### 2.4 — Collector framework + registry → `QuickStat.Core/Collectors/`

Namespace `QuickStat.Collectors`. Reference: `Docs/Port/03-collectors.md`.

| File | Type | Promises |
|---|---|---|
| `Collectors/ICollector.cs` | `interface ICollector` | Descriptor plus `BuildSql(CollectorSqlContext)`. Pure and deterministic, so golden files work. |
| `Collectors/CollectorDescriptor.cs` | `sealed record CollectorDescriptor` | Name, title, kind, var prefix, PID binding, batch size, gate, availability. Pure data; golden-file-able. |
| `Collectors/CollectorKind.cs` | `enum CollectorKind` | Family, for grouping and golden-file naming. Not polymorphism. |
| `Collectors/PidBinding.cs` | `enum PidBinding` | `None` (whole-database scan, rows filtered client-side), `SinglePerson` (N round trips), `IdList`. |
| `Collectors/CollectorSqlContext.cs` | `readonly record struct CollectorSqlContext` | Study id and the `{IdList}` replacement for this batch. |
| `Collectors/CollectorResultRow.cs` | `readonly record struct CollectorResultRow` | The five-column positional contract as named members, with the ordinals as constants; optional `ItemId`/`Caption` by name; `ColumnName(varPrefix)`. |
| `Collectors/StudyGate.cs` | `[Flags] enum StudyGate` | Which study families a collector is registered for. `Always = 0`. |
| `Collectors/StudyGatePatterns.cs` | `static class StudyGatePatterns` | The five regexes verbatim, with the case-sensitivity rule and the 124/131 target documented. |
| `Collectors/CollectorAvailability.cs` | `sealed record CollectorAvailability` | Required database objects (probed with `OBJECT_ID`) plus an optional predicate. `Always` is the default. |
| `Collectors/CollectorAvailabilityContext.cs` | `readonly record struct CollectorAvailabilityContext` | Study name, study id, and the set of database objects that resolved. |
| `Collectors/CollectorTitle.cs` | `static class CollectorTitle` | The three title-suffix rules and their exact strings, in one place. |
| `Collectors/ICollectorRegistry.cs` | `interface ICollectorRegistry` | Builds the ordered list for a session — registration order is part of the contract — and finds a collector by name **or** title. |
| `Collectors/ICollectorRunner.cs` | `interface ICollectorRunner` | Runs one collector over a cohort, batching as the descriptor requires. |
| `Collectors/ICollectorResultSink.cs` | `interface ICollectorResultSink` | Where accepted rows go. The 2.4 → 2.5 seam; see §3. |
| `Collectors/CollectorRunSummary.cs` | `sealed record CollectorRunSummary` | The column names (i.e. the column **order**) plus row and batch counts. |

### 2.5 — Matrix, datapoints, cell colouring → `QuickStat.Core/Domain/{Matrix,DataPoints}/`

Reference: `Docs/Port/04-matrix-export.md` §1–3.

| File | Type | Promises |
|---|---|---|
| `Domain/Matrix/PersonMatrix.cs` | `sealed class PersonMatrix : ICollectorResultSink` | The dataset. Owns its rows and columns instead of pushing objects into a grid control. Prepare / clear / add columns / lock / read cells. |
| `Domain/Matrix/MatrixRow.cs` | `sealed class MatrixRow` | One person plus their datapoints, keyed ordinally by column name. |
| `Domain/Matrix/MatrixColumn.cs` | `sealed record MatrixColumn` | `VarName` (which is what the CSV header carries), `Title`, `Description`. |
| `Domain/Matrix/MatrixCell.cs` | `readonly record struct MatrixCell` | Text, background, foreground, alignment, and whether there is a value at all — computed with no WPF dependency, so cell appearance is unit-testable. |
| `Domain/Matrix/ColumnOrder.cs` | `enum ColumnOrder` | `FirstSeen` (default, what ships) or `Alphabetical`. |
| `Domain/Matrix/VariableNameSet.cs` | `sealed class VariableNameSet : IReadOnlyList<string>` | The ordered, de-duplicating collection that decides column order. Must be cleared per run. |
| `Domain/Matrix/MatrixSortOrder.cs` | `enum MatrixSortOrder` | `PersonId` (what QuickStat always uses) or `ReverseName`. |
| `Domain/Matrix/FixedColumns.cs` | `static class FixedColumns` | The four leading identity columns: ordinals and the exact Norwegian headers. Shared with 2.6. |
| `Domain/DataPoints/DataPoint.cs` | `sealed class DataPoint` | One cell value: value, timestamp, row id, optional item id and caption, update count, and the hint text. |
| `Domain/DataPoints/DataPointRule.cs` | `sealed record DataPointRule` | Display format, brush colour and font colour as injected functions — the data that replaces eighteen subclasses. |
| `Domain/DataPoints/IDataPointFactory.cs` | `interface IDataPointFactory` | Creates datapoints and resolves rules. Lookup is **case-sensitive** (`DB_VERSION` ≠ `DbVersion`). |
| `Domain/DataPoints/Rgb.cs` | `readonly record struct Rgb` | The domain colour, with `FromDelphi` for the reversed `$00BBGGRR` literals. |

### 2.6 — Anonymisation + export → `QuickStat.Core/Domain/Anonymisation/`, `QuickStat.Core/Export/`

Reference: `Docs/Port/04-matrix-export.md` §4–5.

| File | Type | Promises |
|---|---|---|
| `Domain/Anonymisation/PersonIdentification.cs` | `enum PersonIdentification` | `Full` / `PersonIdOnly` (default) / `RandomPersonId`; the two non-full modes **omit** the DOB, national-id and name columns entirely. |
| `Domain/Anonymisation/IdentificationColumns.cs` | `readonly record struct IdentificationColumns` | The one derivation from mode to column set. Both the grid and the exporter call `For`. |
| `Domain/Anonymisation/IIdentificationPolicy.cs` | `interface IIdentificationPolicy` | The one shared instance of the current mode, with a change event. |
| `Domain/Anonymisation/IAnonymiser.cs` | `interface IAnonymiser` | Pseudonyms stable within a dataset and unlinkable across datasets, plus the map for the key file. |
| `Export/IDatasetExporter.cs` | `interface IDatasetExporter` | Writes a locked matrix; returns the paths written, including the key file. |
| `Export/DatasetExportOptions.cs` | `sealed record DatasetExportOptions` | Identification (required), derived `Columns`, timestamps, format, dialect, key-file opt-in, culture, encoding — plus the byte-parity constants. |
| `Export/DatasetExportResult.cs` | `sealed record DatasetExportResult` | File path, optional key-file path, row and column counts. |
| `Export/ExportFormat.cs` | `enum ExportFormat` | `Csv` or `Xlsx`. |
| `Export/CsvDialect.cs` | `enum CsvDialect` | `Legacy` (byte-for-byte the Delphi, the default) or `Rfc4180`. |

### 2.7 — Settings store + notification → `QuickStat.Core/Configuration/Settings/`, `QuickStat.Core/Diagnostics/`

Reference: `Docs/Port/01-data-access.md` §6–7.

| File | Type | Promises |
|---|---|---|
| `Configuration/Settings/ISettingsStore.cs` | `interface ISettingsStore` | Per-user section/key settings with typed get and set, a delete the Delphi never had, and an explicit `Flush`. |
| `Diagnostics/IUserNotifier.cs` | `interface IUserNotifier` | Inform / warn / error / confirm. Confirmation always asks — it must never fail open. |
| `Diagnostics/NotificationSeverity.cs` | `enum NotificationSeverity` | `Information` / `Warning` / `Error`. |
| `Diagnostics/OperationProgress.cs` | `readonly record struct OperationProgress` | Header, info line, optional percent. |

---

## 3. Types that do not belong cleanly to one owner

Three placements were judgement calls. Each is recorded here so the next reader does not have to
re-derive the reasoning, and so the owner knows the type has other consumers.

| Type | File / owner | Why it does not fit, and why this home was chosen |
|---|---|---|
| `OperationProgress` | `Diagnostics/` → **2.7** | Produced by 2.2 (login) and 2.4 (collect), and consumed by Phase 3. Neither producer can own it without the other having to edit its file. `Diagnostics` is the cross-cutting folder and 2.7 owns the other user-facing surface, so it lands there. 2.7 does not otherwise need it. |
| `VariableNameSet`, `ColumnOrder` | `Domain/Matrix/` → **2.5** | 2.4 writes into the set while reading a batch; 2.5 reads it to create columns. Column order is a matrix concept and the matrix is what makes it observable, so 2.5 owns it. **2.4 must not add members to it** — request them instead. |
| `ICollectorResultSink` | `Collectors/` → **2.4** | The seam between running a collector and storing the result. Placed with the runner because it is the runner's calling contract, but it is `PersonMatrix` (2.5) that implements it. If the shape needs to change, 2.5 reports it and 2.4 makes the edit. |

Two more are worth flagging even though their ownership is unambiguous:

- **`SqlOptions` (2.1) carries `PersonIdListTypeName`, `PersonIdListColumnName` and
  `MaxIdsPerBatch`,** which are read by 2.3 (national ids) and 2.4 (every `{IdList}` collector).
  They are configuration, so they live with configuration; both consumers read, neither writes.
- **`FixedColumns` (2.5)** is read by 2.6 for the CSV header row. Declared once so the four
  Norwegian headers are not transcribed twice.

---

## 4. Members that are implemented rather than stubbed

Phase 1 writes no logic, with four deliberate exceptions. In each case the body is one expression
that *is* the contract, and a `NotImplementedException` would let two steps disagree about it.

| Member | Body | Why not a stub |
|---|---|---|
| `Patient.DisplayName` | `$"{LastName}, {FirstName}"` | The grid (2.5) and the export (2.6) must render a name identically. `EPR.QA.Matrix.Row.pas:90-97`. |
| `Population.SearchText` | `$"{ProcId}\t{Title}\t{HelpText}\t{Group}"` | Defines what the population filter matches. Field order and the tab separator are observable. `CRF.Population.pas:94`. |
| `CollectorResultRow.ColumnName(varPrefix)` | `varPrefix + VarName` | Plain concatenation, no separator. Inserting a dot would rename every column in every export. `EPR.QA.Collector.Base.pas:157`. |
| `HalfOpenPeriod.IsValid`, `.Contains` | `Start < Stop`, `value >= Start && value < Stop` | The half-open semantics are the entire point of the type. PORT-PLAN.md R8. |

`DatasetExportOptions.Columns` is also an expression body, but it delegates to the stubbed
`IdentificationColumns.For`; the point is that it cannot be *set* independently.

---

## 5. What Phase 1 deliberately did **not** declare

These belong to a single step and are that step's to design:

- UDL reading, OLE DB keyword mapping tables, the connection-string translator implementation (2.1).
- The retry policy implementation, the `:Name` scanner, and every concrete `ILoginStep` (2.2).
- SQL constants for populations, patients and packages (2.3).
- The 131 collector names, titles, item-id and lab-class arrays, the `Sp*` SQL library, the
  `{IdList}` binding strategy, the study-gate evaluator, and the `Make.*` registration helpers (2.4).
- The caption dictionary, the risk palette, the sixteen threshold rules, the percentile machinery
  and the cell renderer (2.5).
- The CSV and xlsx writers, the pseudonym generator, and temp-file tracking (2.6).
- The INI store, the file logger, window-state persistence and PII redaction (2.7).

---

## 6. Build facts a Phase 2 agent will hit

- `TreatWarningsAsErrors` is on and `EnforceCodeStyleInBuild` is live. **An unused `using` fails the
  build** (`IDE0005`), and so does a namespace that does not match its folder (`IDE0130`).
- `GenerateDocumentationFile` is on. `CS1591` (missing doc) is suppressed, but `CS1574`
  (**unresolvable `cref`**) and `CS1570` (malformed doc XML) are *not* — a typo in a `<see cref=…>`
  is a build error.
- Interface members must not carry accessibility modifiers (`IDE0040`); write `const string X = …`,
  not `public const string X = …`, inside an interface.
- `Nullable` is enabled. A record property of a non-nullable reference type needs `required` or an
  initialiser.
- Implicit usings for `QuickStat.Core` (`net10.0`, plain SDK) include `System`,
  `System.Collections.Generic`, `System.IO`, `System.Linq`, `System.Threading` and
  `System.Threading.Tasks`. `System.Collections`, `System.Collections.ObjectModel`,
  `System.Diagnostics.CodeAnalysis`, `System.Globalization` and `System.Text` must be imported.
  Note `QuickStat.App` gets the *reduced* WPF set and has no `System.IO`.
- Source files are UTF-8 **without** a BOM and contain Norwegian characters. Verified end to end:
  `' (høyeste)'`, `'Født'` and `'Fødselsnummer'` round-trip into the compiled assembly's string heap.
- Test stack is xUnit v2 on VSTest. Do not change it.
