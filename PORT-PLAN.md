# QuickStat → WPF / .NET 10 port plan

Status: **in implementation — Phases 0–3 complete. Resume at Phase 4 (restore the lost functionality).**
Branch: `feature/dotnet`
Last updated: 2026-08-26

> **Resume here. Phase 3 is complete — the whole application is built.** All six UI steps are
> merged, verified independently rather than on the implementing agents' reports: from-scratch Debug
> and Release builds with zero warnings, **2205 tests passing** under `en-US`, `tr-TR`, `ar-SA`,
> `th-TH` and the machine's own `nn-NO`, and `QuickStat.exe` launching from a foreign working
> directory with the full DI graph, loading a real `QuickStat.config.xml`, and logging no errors.
>
> **Next: Phase 4.** Nothing blocks it.
>
> **Six defects were found while integrating wave 2, five of them in code or contracts that earlier
> phases had signed off.** Every one is fixed; they are listed here because they are the pattern to
> expect, not because they are still open.
>
> | # | Defect | Found by |
> |---|---|---|
> | 1 | **`05-ui-spec.md` §G.5, §6 and `07` §5 all specified the collector sort backwards.** They said `StringComparer.Ordinal` "keeps the `^ `-prefixed demographic collectors first". `'^'` is U+005E — above `'Z'`, below `'a'` — and every other title starts with a capital, so ordinal puts all eleven demographic elements **last**. Would have moved eleven columns from the left to the right edge of every CSV. §6 now specifies `CurrentCultureIgnoreCase`, which is what `LBS_SORT` does | 3.3 |
> | 2 | **`PersonMatrix` could not be re-collected.** `AddColumns` and `Add` refuse a locked matrix, `Lock()` ends every run, and only `ClearPopulation` unlocked — so the second click of *Collect data* threw. The Delphi's `fLocked` gates painting and export, never adding. `ClearVariables` now unlocks | 3.3 |
> | 3 | **The ordering contract in `07` §3.1 threw on the second population of a session.** `SortBy` throws while locked, and the check precedes the equality short-circuit. `Clear()` must come first | 3.2 and 3.4, independently |
> | 4 | **`07` §3.1 said the package replay stays on the Packages tab. It does not** — `TrySelect(procId, true, …)` reaches `AfterPopulationSelect`, which sets `pgSelections.ActivePage := tbsDataElements`. Restored as parity | 3.2 and 3.4, independently |
> | 5 | **The Progress header never went back.** Core reports `Connecting` and `Collecting data`; `ShellProgress` let them win, so the banner read *Collecting data* for the rest of the session. `TfrmQuickStat.SetHeader` exists at `MainQuickStat.pas:433` and nothing ever calls it (§G.6) | 3.3 |
> | 6 | **A mirrored checkmark.** `FlowDirection="RightToLeft"` puts the caption left of the box — and mirrors the whole subtree, including the tick. Fixed once in the theme as `QsCaptionLeftCheckBox` | reported from a running build |
>
> Two more came out of the culture sweep, which is now a permanent, opt-in file
> (`QuickStat.Tests/CultureSweep.cs`, `-e QUICKSTAT_TEST_CULTURE=xx-YY`) because three agents each
> built and discarded one: `SqlParameterFactory` threw out of its own error message under a
> non-Gregorian calendar, and a collector-SQL assertion used a collation where it meant a byte scan.
> A flaky settings-store test was also diagnosed and fixed — `Save()` gave up after one transient
> sharing violation, silently losing whatever the user had just changed.
>
> **Read `Docs/Port/07-ui-contracts.md` before touching the UI.** It is the ownership map: which
> files belong to which step, the shared surface (`IShellWorkspace`, `IShellProgress`,
> `IConnectionCoordinator`, `IUiDispatcher`, and the rest), the decisions that constrain callers, and
> the build and test traps that were actually hit. Its §3.1 and §5 carry the corrections above
> inline, marked as corrections.
>
> Shared and read-only, extend rather than copy: **`QuickStat.Tests/Ui/StaTestRunner.cs`**,
> **`DependencyPropertyRegistrationTests.cs`**, **`DatasetGridThemeTests.cs`**,
> **`Ui/Dialogs/RealisedWindow.cs`** (3.6's; a BAML binding does not attach until the element is
> realised, so asserting on one before that passes vacuously) and **`CultureSweep.cs`**.
> **`Controls/Dataset/**` is finished**; bind to `MatrixGrid`, never edit it.
>
> **`QuickStat.Core` is now functionally complete.** All seven Phase 2 steps are merged. The
> composition root in `QuickStat.App/App.xaml.cs` calls the seven `AddQuickStat*` extension methods;
> every one registers with `TryAdd`, so order does not matter and a later `Replace` wins.
>
> **Phase 3 must read `Docs/Port/06-contracts.md`** for the type surface, `Docs/Port/05-ui-spec.md`
> for the window layout, and — from wave 2 onward — `Docs/Port/07-ui-contracts.md`, which step 3.1
> writes as the UI ownership map. §5 explains why the six steps run in two waves and lists the WPF
> testing facts that were measured rather than assumed. Two things Phase 3 owns that Phase 2
> deliberately left as seams:
> - **`IUserNotificationPresenter`** — implement it and install with
>   `services.Replace(ServiceDescriptor.Singleton<IUserNotificationPresenter, WpfNotificationPresenter>())`.
>   Do **not** reimplement `IUserNotifier`: severity mapping, PII redaction and the never-fail-open
>   rule live in `QuickStat.Core` and are enforced by tests, including one asserting `UserNotifier`
>   is the only non-abstract `IUserNotifier` in the assembly.
> - **`IPeriodPrompt`** — shows a window, so step 2.3 declared it but did not register it.
>
> Things established during Phase 2 that later phases depend on:
> - **`ServiceProvider.Dispose()` throws for a singleton implementing only `IAsyncDisposable`.**
>   `OnExit` therefore disposes asynchronously, on the thread pool, with a 10 s ceiling. Registering
>   an async-only disposable is safe now; disposing the host synchronously is not.
> - **Every test must be culture-independent.** The whole suite was swept under a forced `en-US`
>   ambient culture and passes; a display-format assertion that depends on the machine's decimal
>   comma will pass here and fail on a build agent.
> - **`ICollectorResultSink.CreateVariableNameSet()`** is a default interface member. The sink, not
>   the runner, decides column order — `PersonMatrix.ColumnOrder` was otherwise a property nobody
>   read, and setting it to `Alphabetical` silently did nothing.
> - Library source for Phase 4 is still the pinned worktree. Note
>   **`EPR.QA.Collector.Names.pas` is the only Windows-1252 file** under `EPR\QA\`; every other unit
>   is UTF-8 with BOM, and converting the wrong one produces plausible-looking mojibake.
>
> Things established during Phase 1 that later steps depend on:
> - Study-gate regexes are frozen verbatim in `Collectors/StudyGatePatterns.cs`, including `KORTTID`
>   in **both** literals. Do not retype them.
> - `ColumnOrder.FirstSeen` is `0`, so `default(ColumnOrder)` is insertion order.
> - `DatasetExportOptions.Columns` is a computed property derived from `Identification` — display and
>   export anonymity cannot diverge (§7.2).
> - `.gitignore` needs `!/QuickStat.Core/Domain/Packages/`: the NuGet `packages/` rule matches that
>   folder on case-insensitive Windows git, and the negation only works *after* the rule it overrides.
>
> Things established during Phase 0 that later steps depend on:
> - Test stack is **xUnit v2 on VSTest**. Do not "upgrade" to xunit.v3 — it breaks `dotnet test` on
>   this SDK. Evidence is in `Directory.Packages.props`.
> - DI extension point: `ConfigureServices` in `QuickStat.App/App.xaml.cs`, with one labelled anchor
>   comment per phase so parallel agents never edit the same lines.
> - WPF projects get a **reduced** implicit-usings set — no `System.IO`. Add it per file.
> - `GenerateDocumentationFile=true` with `CS1591` in `NoWarn`: docs are produced but not mandated.
> - Warnings are errors and `EnforceCodeStyleInBuild` is live: an unused `using` fails the build.
> - `.gitignore` carries a `!/QuickStat.App/` negation that must not be removed — the pre-existing
>   Mac `*.app` pattern otherwise hides the entire application from git.

---

## 1. What this is

A rewrite of the Delphi VCL application **QuickStat** (`QuickStat.dpr` → `MainQuickStat.pas`) as a
WPF application on .NET 10, in this repository, using a flat layout.

QuickStat lets a researcher pick a *population* (a saved server-side query defining a patient
cohort), tick a set of *data elements* ("collectors"), pull the resulting person × variable matrix
out of a FastTrak SQL Server database, and export it to CSV or Excel with a chosen level of
patient identification.

### 1.1 Decisions already taken

| Question | Decision |
|---|---|
| Repo layout | Flat — project folders directly at repo root, no `src/` |
| Delphi sources | Kept on this branch as the porting reference; deleted in Phase 6 once parity is verified |
| `DbFormExport.dpr` | **Not** ported (out of scope) |
| Data access | `Microsoft.Data.SqlClient` |
| Configuration | Keep reading the deployed `QuickStat.config.xml` verbatim, including `FILE NAME=…\FastTrak.UDL`, translating OLE DB/UDL → ADO.NET at load |
| Dependencies | CommunityToolkit.Mvvm, Microsoft.Data.SqlClient, Microsoft.Extensions.{DependencyInjection,Logging}, ClosedXML, xUnit. No commercial control suites |
| UI | Same information architecture, tab names, control placement and workflow; modern flat rendering. No attempt to imitate a "Delphi look" |

### 1.2 Verified environment

Confirmed on this machine before planning (throwaway spike, built clean, then deleted):

| Component | Version |
|---|---|
| .NET SDK | 10.0.400 |
| `Microsoft.WindowsDesktop.App` | 10.0.11 |
| `.slnx` solution format | supported by this SDK |
| CommunityToolkit.Mvvm | 8.4.2 |
| Microsoft.Data.SqlClient | 7.0.2 |
| ClosedXML | 0.105.1 |
| Microsoft.Extensions.Hosting | 10.0.11 |

---

## 2. Reference documents

Deep analyses of the existing code live in `Docs/Port/`. **Implementation agents must read the
relevant document before writing code**; they contain verbatim SQL, exact captions, hex colours and
`file:line` citations, so that no one has to guess at behaviour.

| Document | Covers |
|---|---|
| `Docs/Port/01-data-access.md` | Connection/login lifecycle, the `ISQL` surface, UDL/OLE DB translation, retry, settings, logging |
| `Docs/Port/02-populations-patients.md` | What a population is, selection flow, patient loading, parameter prompting, national IDs |
| `Docs/Port/03-collectors.md` | Full collector inventory, collector class taxonomy, all SQL templates, the four recovered features |
| `Docs/Port/04-matrix-export.md` | Matrix model, datapoints, cell colouring, anonymisation, CSV/Excel export, grid rendering |
| `Docs/Port/05-ui-spec.md` | Window layout, every control and caption, commands, palette, WPF view/view-model breakdown |

### 2.1 The parity baseline is **not** this repository

`C:\work\FastTrakApps\App.QuickStat\` holds the canonical QuickStat source. **This repository is a
reduced copy of it.** The complete set of differences comes from the pairing chosen during
extraction (`9935ea9`, "Hentet ut fra FastTrakApps og develop_old"):

| | FastTrakApps (canonical) | This repo |
|---|---|---|
| The four collector registrations | active | commented out |
| `AddNationalIds` / `IncludesNationalId` | **active** | commented out, `// TODO: Disse feiler, hvor er de??` |
| `Generics.Collections` → `System.Generics.Collections`, `{$ENDIF }`, `.dpr` uses clause | — | cosmetic |

Nothing else differs. Whoever extracted this repo paired the canonical app with the `develop_old`
library, hit five symbols that do not exist there, and commented them out to make it compile.

**That pairing was a deliberate choice, not accidental damage.** `develop_old` is not a side branch:
it is one commit of its own and 252 behind `develop` — a stale snapshot of *mainline*. Tarmscreening
is the side branch, and it is absent from `develop`, `develop_old` and `master` alike. So `9935ea9`
reads as "canonical app + mainline library", and the five comment-outs are the price of that choice.
Read this section as recording which pairing the **port** should follow, not as an accusation.

#### Which library baseline, and how confident

The canonical app references `QST_LAB_INTERLEUKINS`, `QS_ROAS_BASE`, `AddNationalIds` and the
antibiotic collectors. Those symbols are absent from `develop`, `develop_old` and `master`, so
**`develop_old` cannot build this application at all** — that part is verified, not inferred.

**QuickStat almost certainly has no working build — checked in `C:\work\FastTrak.BuildServer` at
`HEAD`.** `QuickStat.fbp8` sets `QuickStatDir = %Source%\App.QuickStat` and compiles with
`searchpath = $(FastTrakDir)\Lib\Service;…;$(FastTrakDir)\EPR\QA;…`. An
`action.continua.iscontinua` guard rewrites `$(FastTrakDir)` → `%FastTrakDir%` in the `.dproj`, but
that guard has exactly **one** child — the find/replace. `action.delphi.build` is its *sibling*, so
only the `.dproj` rewrite is CI-conditional; **the compile runs either way.**

`FastTrakDir` resolves two ways, and both point away from the features:

- **Under Continua** it is bound to the `$Source.FastTrakDevelop` source. *What branch that source
  tracks has not been observed* — Continua's source definitions are in neither repo, and the
  inference is read off the name. This is the one load-bearing assumption under R13: if
  `FastTrakDevelop` happened to track a tarmscreening ref, the build would succeed and R13
  collapses. Treat it as probable, not established.
- **Outside Continua** the variable's own `defaultvalue` is `c:\work\FastTrak` — a local dev path.
  That working copy is on `master` today, which lacks every one of the symbols, so a local build
  fails now for reasons independent of Continua. This part *is* verified.

Either way it is **not** the FastTrakApps `FastTrak` submodule.

`develop` has never carried any of the four collector features, nor `TPatientList.AddNationalIds`
(0 hits for `QST_LAB_INTERLEUKINS`, `QS_ROAS_BASE`, `QS_DRUG_J01XX05` and
`QS_DRUG_ANTIBIOTIC_INTERMEDIATE`). Meanwhile the app has referenced tarmscreening-only symbols
since FastTrakApps' **initial commit** — `7b9409e` (2020-09-23) already registers
`QS_DRUG_ANTIBIOTIC_INTERMEDIATE`; `QS_ROAS_BASE` follows in `c15d5c9` (2021-09-03) and interleukins
in `abd4e44` (2022-12-13), each on the same day as its library half. So for as long as `FastTrakDir`
has resolved to a `develop`-tracking source, this build could not have succeeded — and by
2022-12-13 the app required the complete set. Nobody noticed because `App.QuickStat` then went
untouched for three and a half years.

The FastTrak **submodule** in FastTrakApps (introduced `1b31f22`, 2025-08-14; pinned `eb50824cf`,
2025-10-14, on mainline) is likewise symbol-free, but it is *not* what `QuickStat.fbp8` compiles
against and is not the cause. An earlier revision of this section said it was; that was wrong.

"The tarmscreening lineage" is also **not one ref.** Two candidates carry the features, and they are
siblings, not ancestor and descendant — they forked at `84d0c2b83` (2022-11-23) and diverged 5 / 64:

| Ref | Tip | Date | Interleukins | Target |
|---|---|---|---|---|
| `origin/tarmscreening/develop` | `249ac2d16` | 2023-09-01 | **yes** | 131 collectors |
| `origin/release/tarmscreening` | `54b91549e` | 2023-09-27 | **no** | 130 collectors |

`release/tarmscreening` is 26 days newer and its tip message is *"Versjon som er testet i HSØ"*, so
on a bare "what did a customer run" reading it looks like the stronger candidate. The diff between
the two over `EPR/QA/` is **exactly 6 insertions in 3 files** — the interleukins feature, nothing
else.

**Decision: `origin/tarmscreening/develop`, target 131.** This is settled by a dated commit chain,
not by preference:

| When | Where | What |
|---|---|---|
| 2022-11-23 | FastTrak | `84d0c2b83` — `release/tarmscreening` forks here, three weeks *before* interleukins exist |
| 2022-12-13 | FastTrak | `fefc8a809` adds `QST_LAB_INTERLEUKINS` (tarmscreening/develop only) |
| 2022-12-13 | FastTrakApps | `abd4e44` "#531377: Støtte for interleukin" adds the matching `AddCollector( … QST_LAB_INTERLEUKINS )` — **same day, coordinated** |
| 2022-12-21 | FastTrakApps | `313a15c` "Tar opp programversjon for QuickStat" |
| — | shipped exe | **v22.12.21.547** |

The exe's own version number is the date of the version-bump commit, eight days after the app and
library gained interleukins together. `release/tarmscreening` had already forked three weeks earlier
and never received the feature, so the shipped binary cannot have been built from it. The
`QST_LAB_INTERLEUKINS` registration is also still present in the canonical source today, which
independently rules that ref out as a compilation target.

> If a protocol owner nonetheless says interleukins were never used in the field, the change is
> small and localised: drop `QST_LAB_INTERLEUKINS`, 131 → 130, and Phase 4's fourth row disappears.
> Nothing else differs between the two refs.

Implementation agents must read app-level source from `C:\work\FastTrakApps\App.QuickStat\`, and
library source from the tarmscreening tip. Where the reference documents in `Docs/Port/` say "what
ships today" of this repo's `FastTrak\` copies, read that as "`develop_old`", which is an artefact of
the extraction — not a shipping baseline.

> **Read library source from `C:\work\FastTrak-tarmscreening\` — never from `C:\work\FastTrak\`.**
> The latter's working tree is checked out to `master`, which does **not** contain the tarmscreening
> lineage, so reading it returns the wrong content *silently*. `C:\work\FastTrak-tarmscreening\` is a
> detached `git worktree` pinned to `origin/tarmscreening/develop` (`249ac2d16`), created for this
> port. Treat it as read-only. Library units live under `EPR\QA\`, e.g.
> `C:\work\FastTrak-tarmscreening\EPR\QA\EPR.QA.Collector.Base.pas`.
>
> This repo's own `FastTrak\` folder is the `develop_old` extraction. Use it only to understand what
> the *degraded* build did — never as the porting source. Tear the worktree down in Phase 6 with
> `git -C C:\work\FastTrak worktree remove C:\work\FastTrak-tarmscreening`.

The Delphi source remains the final authority while it is still in the tree.

### 2.2 Repository state

- **Everything from Phase 1 onward is unpushed.** `origin`
  (`github.com/DIPSAS/FastTrak.Quickstat`) carries `main` and `feature/dotnet`, the latter at
  `7518e36` — the plan, the five reference analyses and the Phase 0 skeleton, including `5502b72`.
  Phases 1 and 2 are **33 commits and 262 files** ahead of that and exist only on this machine. The
  remote tip is an ancestor of the local branch, so publishing is a fast-forward with nothing to
  reconcile. Push before relying on any of it surviving this machine.
- **The KORTTID fix has a twin.** `FastTrakApps/App.QuickStat` carries the identical change on
  `feature/739506_GBD_utvalet_i_Korttid`, which is that repo's currently checked-out branch. The two
  agree; see §10.4 for why the *totals* still differ between the trees.
- **`App.QuickStat` had no code change between 2023-01-05 and 2026-08-24.** The only commit in that
  window is `c3f84e3` (2023-02-04, "Testdatabase lagt til i config.") which touches configuration,
  not code. The last substantive work is the interleukin support and version bump of December 2022,
  so anything describing "recent" QuickStat behaviour is describing a 2022 build.
- **Build definitions live in `C:\work\FastTrak.BuildServer`** (FinalBuilder, Continua CI).
  `QuickStat.fbp8` is the QuickStat build; see R13 for why it is unlikely to succeed.
- **Both of those repos have uncommitted changes right now** — `QuickStat.fbp8` in the build server,
  the `.dproj` in FastTrakApps. Quote them only via `git show HEAD:<path>`. See R14.

---

## 3. Ground rules

1. **Only port what is actually in use.** The `Spring/` tree is reachable from exactly two library
   units plus one `Guard.CheckNotNull` call; it disappears entirely. Anything not reachable from
   `QuickStat.dpr`'s `uses` graph is out of scope by default.
2. **Keep the shape of the code where it pays.** Class and concept names carry over
   (`Population`, `Collector`, `DataPoint`, `PackagedSelection`) so the two codebases can be read
   side by side. Diverge only where §7 says so.
3. **Behaviour is parity-first.** Anything a user can observe — collector titles, CSV byte format,
   grid column order — is reproduced exactly unless it appears in §7 as a deliberate change.
4. **No silent scope changes.** If an implementation step finds something that cannot be ported as
   specified, it records the finding and completes everything else in the step.

---

## 4. Target repository layout

```
QuickStat.slnx                  solution (slnx format)
global.json                     pins the 10.0.x SDK
Directory.Build.props           shared TFM, nullable, warnings-as-errors, deterministic builds
Directory.Packages.props        central package management (all versions pinned here)
.editorconfig                   formatting + analyzer severity
build.ps1                       untouched (Delphi build) until Phase 6; .NET uses `dotnet` directly

QuickStat.Core/                 domain, data access, collectors, export.  No WPF reference.
QuickStat.App/                  WPF application.  AssemblyName MUST be `QuickStat`.
QuickStat.Tests/                xUnit.

Docs/                           existing docs + Docs/Port/ analyses
QuickStat.config.xml            unchanged, still shipped next to the exe
```

Three projects, not four. Separation between data access and domain is expressed through
namespaces (`QuickStat.Configuration`, `QuickStat.Data`, `QuickStat.Domain`, `QuickStat.Collectors`,
`QuickStat.Export`, `QuickStat.Diagnostics`) rather than assembly boundaries, because nothing in this
application loads those layers independently.

### 4.1 Hard constraints

- **`QuickStat.App` must produce `QuickStat.exe`.** Config discovery is
  `ChangeFileExt(ParamStr(0), '.config.xml')`; if the assembly is named anything else,
  `QuickStat.config.xml` stops resolving for every existing installation. Set `<AssemblyName>QuickStat</AssemblyName>`.
- **Resolve paths from the executable directory, not the working directory.** The Delphi code
  resolves relative UDL paths against the CWD, which is a latent bug when launched from a shortcut.
  Use `AppContext.BaseDirectory`, with a CWD fallback for compatibility. Do not use
  `Assembly.Location` — it is empty under single-file publish.
- **Target x64.** The Delphi app is x86 only because of its toolchain; `Microsoft.Data.SqlClient`
  is managed TDS, so no 32-bit dependency survives.
- **Per-monitor DPI v2.** The old manifest declares no DPI awareness at all. WPF is DPI-aware by
  default; declare `PerMonitorV2` explicitly in `app.manifest` and keep `requestedExecutionLevel`
  at `asInvoker`.

---

## 5. Work breakdown

Each step below is sized for **one Opus 5 max-effort sub-agent**. Every step lists the files it
**owns**; two steps in the same phase never write the same file, so a phase can be executed fully in
parallel. A step may *read* anything.

### Phase 0 — Foundation  *(blocking; one agent; must land before anything else)*

**Step 0.1 — Solution skeleton**
Owns: `QuickStat.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`,
`.editorconfig`, `.gitignore`, all three `.csproj`, `QuickStat.App/app.manifest`,
`QuickStat.App/App.xaml{,.cs}`, `QuickStat.App/MainWindow.xaml{,.cs}`.

- Three projects, central package management, `net10.0-windows` for App/Tests, `net10.0` for Core.
- `TreatWarningsAsErrors`, `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=latest`.
- Generic Host bootstrap: DI container, `Microsoft.Extensions.Logging` with a file logger writing
  to `<exedir>/LOGS/` — **creating the directory if missing**, which the Delphi never did, silently
  losing all logs.
- Application icon from the existing `QuickStat_Icon.ico`.
- `.gitignore` additions: `.vs/`, `*.user`, `*.suo`. Note `*.ini` is already ignored — deliberate,
  settings files must not be committed.
- Exit criterion: `dotnet build QuickStat.slnx` and `dotnet test` both succeed, and
  `QuickStat.exe` launches to an empty window.

### Phase 1 — Contracts  *(blocking; one agent)*

**Step 1.1 — Types and interfaces only, no logic**
Owns: every file under `QuickStat.Core/` that declares a type consumed by more than one Phase 2 step.

Declare the full surface — records, enums, interfaces, exceptions — with XML doc comments and
`throw new NotImplementedException()` bodies, so Phase 2 agents compile against a fixed contract and
never edit each other's files. At minimum: `ISqlExecutor`, `SqlRequest`, `SqlResultSet`, `SqlRow`,
`IConnectionCatalog`, `QuickStatConnection`, `IPopulationRepository`, `Population`,
`IPatientRepository`, `Patient`, `ICollector`, `CollectorDescriptor`, `CollectorResultRow`,
`IDataPointFactory`, `DataPoint`, `DataPointRule`, `PersonMatrix`, `MatrixColumn`, `MatrixRow`,
`PersonIdentification`, `IAnonymiser`, `IDatasetExporter`, `PackagedSelection`,
`IPackageRepository`, `ISettingsStore`, `IUserNotifier`, `Rgb`.

Rule: `QuickStat.Core` must not reference `PresentationCore`/`WindowsBase`. Colours are a domain
`Rgb` struct; the App layer converts to `System.Windows.Media.Color`.

**Every file Phase 1 creates must have exactly one Phase 2 owner.** That is what makes Phase 2
safely parallel: after Phase 1, each contract file passes to the step that implements it, and no two
agents ever open the same file. One type per file, grouped so the folder maps to a single step:

| Folder | Owner |
|---|---|
| `QuickStat.Core/Configuration/*` (connection catalog, connection entry, SQL options) | 2.1 |
| `QuickStat.Core/Data/*` (`ISqlExecutor`, `SqlRequest`, `SqlResultSet`, `SqlRow`, `ILoginStep`, `SessionContext`, exceptions) | 2.2 |
| `QuickStat.Core/Domain/{Populations,Patients,Packages}/*` | 2.3 |
| `QuickStat.Core/Collectors/*` (`ICollector`, `CollectorDescriptor`, `CollectorResultRow`, registry, study gates) | 2.4 |
| `QuickStat.Core/Domain/{Matrix,DataPoints}/*` + `Rgb` | 2.5 |
| `QuickStat.Core/Domain/Anonymisation/*`, `QuickStat.Core/Export/*` | 2.6 |
| `QuickStat.Core/Configuration/Settings/*`, `QuickStat.Core/Diagnostics/*` | 2.7 |

Phase 1 records the final map in `Docs/Port/06-contracts.md`. A Phase 2 agent that needs a contract
change makes it **only** in a file it owns; anything else is reported, not edited.

### Phase 2 — Core implementation  *(parallel; seven agents)*

| Step | Scope | Owns | Reference |
|---|---|---|---|
| 2.1 | Config + connection strings: parse `QuickStat.config.xml`, read UTF-16LE UDL files, translate OLE DB → ADO.NET, apply `Encrypt` defaults | `QuickStat.Core/Configuration/**` | `01` §3 |
| 2.2 | SQL execution + login pipeline: single long-lived connection with a `SemaphoreSlim`, `:Name`→`@Name` rewriter, ordered `ILoginStep` pipeline, typed exceptions, transient retry on reads only | `QuickStat.Core/Data/**` | `01` §1,2,5 |
| 2.3 | Populations + patients (incl. national IDs via table-valued parameter) + packaged selections | `QuickStat.Core/Domain/Populations/**`, `.../Patients/**`, `.../Packages/**` | `02` |
| 2.4 | Collector framework + full registry | `QuickStat.Core/Collectors/**` | `03` |
| 2.5 | Matrix, datapoints, cell colouring | `QuickStat.Core/Domain/Matrix/**`, `.../DataPoints/**` | `04` §1-3 |
| 2.6 | Anonymisation + CSV/xlsx export | `QuickStat.Core/Domain/Anonymisation/**`, `QuickStat.Core/Export/**` | `04` §4,5 |
| 2.7 | Settings store + notification service | `QuickStat.Core/Configuration/Settings/**`, `QuickStat.Core/Diagnostics/**` | `01` §6,7 |

A packaged selection is stored **server-side** (`Report.QuickStat`, written by `Report.AddQuickStat`
and removed by `QuickStat.DeletePackage`), not in a local settings file — which is why it is a
repository under 2.3 and not part of the settings store in 2.7.

Step 2.4 is by far the largest: **131 distinct collector names** in the canonical registry, plus
`2 × N` dynamic per-form collectors. (126 of those are in *this* reduced repo — 87 built through the
factory, 39 constructed directly; the remaining five arrive with Phase 4. See §10.3.) Split it by
collector family —
demographics + forms (36 always-on) / labdata / drugs (35) / diagnoses (17) / GBD varsets (24) —
each family owning its own registry file behind a common partial registration entry point.

Three contract details that are easy to lose and change what the user sees:

- `RunBatch` reads results **by ordinal**: `Fields[0..4]` = PersonId, VarName, Value, Timestamp,
  RowId. `ItemId` and `Caption` are looked up **by name**.
- Column name is `VarPrefix + Fields[1]`. Column order is **insertion order** — the order the rows
  first arrive, which for form data is on-form item order. `FVarList` stays as the sorted dedupe
  set; `FVarOrder` is the ordered projection that `VarNames` returns. (Verified at the
  `origin/tarmscreening/develop` tip; the `develop_old` copies return the *alphabetical* `FVarList`,
  which is not what any shipped binary does — see `03-collectors.md` §F, corrected verdicts.)
- The collector *class* appends title suffixes: `' (siste)'` for varset and form-age collectors,
  `' (høyeste)'` for max collectors, and `TLabSetCollector` wraps as `'Labdata: %s (siste)'` **only
  when the title contains no colon**. Centralise these rules in one place.

### Phase 3 — User interface  *(six agents in two waves; needs Phase 1, and Phase 2 for live data)*

Still six steps, but **sequenced into two waves rather than run as one flat fan-out.** Phase 2 was
safely parallel because Phase 1 had already fixed every shared type; Phase 3 has no equivalent, and
six agents writing views, view-models, a theme and a shell would have had to invent the same seams
independently — brush keys, converters, the section-header control, the cross-tab state, and who
owns `App.xaml`. Step 3.1 is therefore promoted to "Phase 1 of the UI": it writes the theme, the
shell and the shared contracts, and it creates every other step's view and view-model as a
compiling stub, which that step then owns and fills in. That is the same handover that made Phase 2
work, and it means the application runs and shows real chrome at the end of wave 1.

| Wave | Step | Scope | Owns |
|---|---|---|---|
| 1 | 3.1 | Theme, shell, banner + progress, **Dataset tab**, shared contracts, and the stub views/view-models the other steps inherit | `QuickStat.App/Theme/**`, `Converters/**`, `Services/**`, `ViewModels/**`, `Views/**` (all created here), `MainWindow.xaml{,.cs}`, `App.xaml{,.cs}`, `Docs/Port/07-ui-contracts.md` |
| 1 | 3.5 | Dataset grid control only — the virtualised renderer, scrolling, hit-testing, tooltips, automation peer | `QuickStat.App/Controls/Dataset/**` |
| 2 | 3.2 | Population tab + embedded population picker | `Views/PopulationTabView.*`, `Views/PopulationPickerView.*`, `ViewModels/Population*ViewModel.cs` |
| 2 | 3.3 | Collections tab: data elements, **the collect run**, export options | `Views/CollectionsTabView.*`, `ViewModels/CollectionsTabViewModel.cs` |
| 2 | 3.4 | Packages tab, including the package replay | `Views/PackagesTabView.*`, `ViewModels/Packages*ViewModel.cs` |
| 2 | 3.6 | Save-specification dialog, period dialog, busy overlay, and the two WPF seams Phase 2 left | `Views/Dialogs/**`, `Services/Wpf*Prompt.cs`, `Services/WpfNotificationPresenter.cs` |

Three corrections to the earlier version of this table, each found while preparing the phase:

- **The Dataset tab had no owner.** 3.5 was scoped as "dataset grid control" and 3.1 as "shell", so
  §C.1 — the caption bar, *Wide columns*, the floating hint panel, the grid context menu and *Show
  data hint* — belonged to neither. It goes to **3.1**, which already owns the shared workspace the
  tab reads. 3.5 keeps only the control, which is the hard part (R5).
- **`Controls/MatrixGrid/` is renamed `Controls/Dataset/`.** `IDE0130` forces namespace to follow
  folder, so the old name would have produced `QuickStat.Controls.MatrixGrid.MatrixGrid` — a type
  whose name equals its own namespace's last segment, which is legal but resolves ambiguously from
  C# (`CS0118`) for every later consumer.
- **`App.xaml{,.cs}` is 3.1's**, because it is alone in wave 1 and the theme has to be merged into
  `Application.Resources` somewhere. Wave-2 steps each write their own `AddQuickStat*` extension
  method in a file they own, exactly as in Phase 2, and the composition root is wired at merge.

Two contract files are written **before** wave 1 starts, because they are the one seam that crosses
the two wave-1 agents: `QuickStat.App/Controls/Dataset/MatrixGrid.cs` (dependency properties,
`CellActivated`, `Refresh`, `TryGetCellBounds`) and `MatrixGridCellEventArgs.cs`. 3.1 binds to that
surface; 3.5 owns the files and implements them, and may add members but must not rename or remove
one.

**WPF testing facts, established by experiment on this machine rather than assumed:**

- The xUnit v2 / VSTest thread is **MTA**. `new Window()` and `FrameworkElement.Measure` both throw
  `InvalidOperationException: The calling thread must be STA`. Use
  `QuickStat.Tests/Ui/StaTestRunner.cs` — shared, read-only, and proved by its own twelve tests.
- **`FormattedText` works on the MTA test thread.** Text metrics need no ceremony; reach for the STA
  helper only for things that genuinely require an apartment.
- **`Application.Current` is `null` under test**, and WPF allows exactly one `Application` per
  `AppDomain` — so the helper deliberately creates none, and production code must never dereference
  `Application.Current` without a null check.
- Many STA threads per process are fine, so tests stay independent.
- This machine's culture is **nb-NO**; a display-format assertion written without an explicit culture
  passes here and fails on an English build agent. Same rule as Phase 2.

Grid rendering: build a **virtualised custom control** (`FrameworkElement` + `IScrollInfo`,
`OnRender` with cached brushes and `FormattedText`, frozen leading columns via transforms).
`DataGrid` is rejected: realistic datasets reach hundreds of columns, column virtualisation
interacts badly with `FrozenColumnCount`, and per-cell colouring would need a converter-driven
`MultiBinding` on roughly eight visuals per cell. Budget the automation peer explicitly — it is the
main accessibility cost of a custom control, and it must not be skipped.

### Phase 4 — Restore the lost functionality  *(one agent; after 2.4)*

This phase is **not archaeology** — see §2.1. The canonical application already registers all four
collectors and already calls `AddNationalIds`; only *this* repository has them commented out. The
work is to make sure the port does not reproduce the extraction damage, and to bring across the
*library-side* implementations, which were never merged into `develop`. Take each from the ref
pinned in §2.1 — `origin/tarmscreening/develop`, via the worktree at
`C:\work\FastTrak-tarmscreening`. Note the fourth row below (interleukins) is the **only** one that
depends on that choice; the other three are present on `release/tarmscreening` as well:

| Feature | Upstream commit | Date | Displayed title |
|---|---|---|---|
| `QS_DRUG_ANTIBIOTIC_INTERMEDIATE` | `4c96c3c3b` | 2020-09-21 | `Antibiotika: Intermediære` |
| `QS_DRUG_ANTIBIOTIC_RECOMMENDED`, `QS_DRUG_J01XX05` (Metenamin/Hiprex) | `9f4a5ed4f` | 2020-09-21 | `Antibiotika: Anbefalte`, `Antibiotika: Metenamin / Hiprex` |
| `QS_ROAS_BASE` | `8a9954c13` (+ `08e35bd8d`) | 2021-09-03 | `Autommunitet (siste)` |
| `QST_LAB_INTERLEUKINS` | `fefc8a809` | 2022-12-13 | `Labdata: Interleukiner (siste)` |

Restoring these takes the registry from **126** to **131** distinct collector names.

Notes carried from analysis — read `Docs/Port/03-collectors.md` §E before writing any of this:

- **`Autommunitet`, not `Autommunitet (siste)`.** Commit `8a9954c13` registered the literal
  `'Autommunitet (siste)'`; follow-up `08e35bd8d` corrected it to `'Autommunitet'` because
  `TVarSetCollector` appends `' (siste)'` itself. Register the short form or the suffix doubles.
  Keep the `Autommunitet` misspelling — matching production output beats fixing a typo.
- **`KB.AntibioticResistance2` is an INNER join**, and `KB` is the only non-`dbo` schema in the whole
  subsystem. A missing table is a **query failure, not an empty result**, and the table is absent
  from many customer databases. This requires a concept that does not exist today: an *optional
  collector*, gated at registration on `OBJECT_ID(...) IS NOT NULL`. Build that gate as part of this
  step — it is the only genuinely new machinery the four features need.
- **Do not take the `J01FF%` removal.** Commit `9f4a5ed4f` also drops `J01FF%` (lincosamides /
  clindamycin) from the *existing* resistance-driving set and renames its caption. That is a
  clinical-definition change to a collector already in production, not part of adding the new ones.
  Add the new collectors; leave `QS_DRUG_ANTIBIOTIC_RESISTANCE` exactly as it ships. See §8.
- `9f4a5ed4f` is a **rebase commit** — its apparent deletions (BDR block, NEWS2, encoding mangling)
  are artefacts of the rebase, not intent. Do not reproduce them.
- `SET_ROAS_BASE` is 68 IDs (count-verified) and `LABCLASSES_INTERLEUKINS` is exactly `[1094…1104]`,
  11 consecutive IDs with no gaps. Both quoted in full in the reference document.
- Commit one feature at a time, so the `KB.`-dependent one can be reverted independently.

Also in this phase: **national-ID export**. The canonical app calls
`if not fPersonList.IncludesNationalId then fPersonList.AddNationalIds;` — only this repo has it
commented out, which is why "Fully identified patients" produces no national IDs *here*. The
implementation lives on `origin/tarmscreening/develop`; port it with a table-valued parameter, not
the upstream string-concatenated `IN` list.

### Phase 5 — Verification  *(one agent + human)*

- Golden-file tests over generated SQL for every collector — the cheapest way to prove a 150-entry
  registry is faithful without a database.
- Byte-for-byte CSV comparison tests against fixtures captured from the Delphi build.
- Unit tests for connection-string translation, `:Name`→`@Name` rewriting, anonymisation,
  the colour ladders, and matrix assembly.
- Manual parity pass against a real database, using `Docs/Port/05-ui-spec.md` as the checklist.

### Phase 6 — Cleanup  *(one agent; only after sign-off)*

Remove the reference worktree:
`git -C C:\work\FastTrak worktree remove C:\work\FastTrak-tarmscreening`.

Delete the Delphi tree: `*.pas`, `*.dfm`, `*.dpr`, `*.dproj`, `*.groupproj`, `*.dsv`, `FastTrak/`,
`Spring/`, `Test/`, `build.bat`, `build.ps1`, `copy-missing-files.ps1`, `fix-namespaces.ps1`, `QuickStat.chp`,
`QuickStat.manifest`, `DbFormExport*`, `FormExport.SqlGenerator.pas`. Rewrite `readme.md` for the
.NET build. Keep `Docs/`, `QuickStat.config.xml`, `FastTrak.UDL`, `QuickStat_Icon.ico`.

---

## 6. Parity that must not drift

These are observable and must match the Delphi build exactly.

- **Collector titles**, in Norwegian, character for character. They are how users recognise rows.
- **CSV format**: `;` separator written after *every* field including the last (trailing separator
  before CRLF), `"`-quoting with doubling, **CP1252 without BOM**, `%g` numeric formatting using the
  **locale decimal separator** (comma on nb-NO), header row containing `VarName` — not the display
  title. The `Export timestamp…` option appends a `"<VarName>.DATE"` header cell and an ISO date
  cell after each data column.
- **Grid column order**: `PID`, then `Født` / `Fødselsnummer` / `Navn` (only when fully identified),
  then data columns in collector order.

  "Collector order" here is **the order of the check list, which is alphabetical by title** — not
  registry order, and the two differ. `cbDataCollector.Sorted := true` is set in `FormShow`
  (`MainQuickStat.pas:400`) before `AfterLogin` fills the list, and `actCollectDataExecute`
  (`:633-681`) walks `Items` from 0 upward calling `AddData` for each checked entry. Column order is
  insertion order, so that walk *is* the column order of every export. Registry order decides which
  collectors exist and how they are listed before sorting; it does not decide columns.

  **The sort is linguistic and case-insensitive — `StringComparer.CurrentCultureIgnoreCase` — not
  ordinal.** Earlier revisions of this section, of `05-ui-spec.md` §G.5 and of `07-ui-contracts.md`
  §5 all said ordinal, "which is what keeps the `^ `-prefixed demographic collectors first". That is
  exactly backwards, and step 3.3 caught it. `'^'` is U+005E, which sits **above** `'Z'` (U+005A) and
  below `'a'`, and every other title begins with a capital — so an ordinal sort puts all eleven
  demographic elements **last**. Sorted both ways against the real 120-collector KORTTID registry:
  ordinal leads with `Antibiotika: Resistendrivende` and trails with the `^ ` group; any linguistic
  ignore-case comparer reproduces `Docs/Screenshots/QuickStat bilde 2.png` exactly — `^ Alder … ^
  Statuskode`, then `Antropometri`, `Diabetes:`, `Labdata:`, `NDV:`.

  The mechanism is `LBS_SORT` on a Win32 list box, whose default comparison is `CompareStringW` with
  `LOCALE_USER_DEFAULT` and `NORM_IGNORECASE` — culture-sensitive by construction, exactly like
  `CurrentCultureIgnoreCase`. The port is therefore culture-dependent in the same way the Delphi is;
  nb-NO, nn-NO, en-US and tr-TR agree on the whole registry except one adjacent pair
  (`Medisin: Antall per behandlingstype` / `Medisin: Antall på utvalgte ATC-grupper`), and every
  Norwegian machine — the shipped configuration — agrees. Shipping ordinal would have moved eleven
  demographic columns from the left edge to the right edge of **every** CSV a customer has scripts
  against. Pinned by `Ui/Collections/CollectorOrderTests.cs`, including a test that deliberately
  records what the broken ordinal rule would produce so nobody "corrects" it back.
- **Identification modes**: `pgiPersonIdOnly` and `pgiRandomPersonId` **omit** the DOB, national ID
  and name columns entirely — no empty field, no separator.
- **Config file compatibility**: an existing `QuickStat.config.xml` must work untouched.

---

## 7. Deliberate changes

Each of these diverges from the Delphi build on purpose. Nothing else should.

### 7.1 Dead code removed

| Removed | Evidence |
|---|---|
| `QuickStat.Component.ReportTree.pas` | Not in the `.dpr` uses list, not compiled; its `Filter` is an empty `{ Not implemented }` loop |
| "Time series" tab | Empty `TRzTabSheet` with `TabEnabled = False`; referenced only by its own designer declaration. Renders as a permanently greyed tab today |
| "Save patient selection" action | `actSavePatientSelection` has an `OnExecute` handler but no menu item or button invokes it — unreachable |
| Percentile colour machinery | `ProvideColor` gates on `InheritsFrom(TColoredDatapoint)` and **nothing registered descends from it**, so 35 `RegisterLabPercentileColoring` calls and ~40 `Report.GetPercentileRanksByClassId` round-trips per login do nothing. Re-verified against the tarmscreening tip per R11: `TColoredDataPoint` is declared at `EPR.QA.DataPoint.pas:42` and has **no descendant anywhere in the library** on that lineage either, so this verdict is not a `develop_old` artefact |
| 39 factory collector names QuickStat never registers | BDJ/Barnediabetes, `FORMAGE.*` and several `QS_GBD_*` varsets belong to `EPR.QA.Collection.Geriatri` and sibling units that QuickStat does not use |
| `Spring/` | Reachable from two library units plus one `Guard.CheckNotNull` |

**Cell colouring is *not* removed.** The grid consults `IBrushColor` directly on the cell object and
only falls through to `OnGetColor` when that returns `clNone`. Fourteen registered analyte classes
implement `IBrushColor` with hardcoded threshold ladders (`Value > 8 → clGraveRisk`, and so on) —
that is the colouring users actually see, and it is ported as-is.

### 7.2 Bugs fixed

| Bug | Consequence today | Fix |
|---|---|---|
| Pseudonym RNG never seeded (`Randomize` appears nowhere) | Random PIDs are identical across every run and every machine, yet differ between two exports in one session — reproducible where it should not be, unstable where it should not be | Seed properly; make pseudonyms stable for the lifetime of a loaded dataset |
| `<export>.mapping.txt` re-identification key written next to every anonymised export, and never deleted for temp exports | Plaintext key files accumulate in `%TEMP%` | Make it opt-in, warn explicitly, and track it for deletion |
| Display anonymity and export anonymity are independent paths | The grid can show names while the export omits them, or the reverse | Single source of truth |
| Cancelling the period dialog | Previous population's patients stay on screen under the *new* population's title | Abort the load |
| Missing `FullName` column in a population's SQL | Zero patients, no error | Fail loudly |
| `SET DATEFORMAT ymd` issued *after* the first user query | Date-parsing ambiguity on the first query | Session options first |
| `LOGS\` directory never created | Total silent log loss | Create it |
| Retry logic runs on the success path | Any `PRINT` output raises `EDatabaseCommandFailed` | Retry only on failure, reads only |
| Period settings key is the entire SQL text, with arguments swapped | Never round-trips | Hash the SQL for the key |
| Six collector name/title collisions | `QST_LAB_LOW` registered under `QST_LAB_MEDIUM`; `QS_GBD_SBP_2M` under `QS_GBD_WEIGHT_2M`; `QS_FORMAGE_GBD_MAREVAN` under `QS_GBD_FORM_MAREVAN`; FlackerKiely shares Stratify's title; dead `QST_LAB_DIABETESw` and `StrTitleLabsetDiabetes` | Fix the registrations; the golden files will pin the corrected titles |
| A failed caption query calls `fTitles.Clear` (`EPR.QA.CaptionDictionary.pas:135-141`) | One database without `Report.LabClassName` discards the **twelve built-in captions as well**, and every column in the grid falls back to its raw variable name | Keep whatever is already loaded and log. Captions are cosmetic and must never fail a login |
| One `TVarCaptions` survives every project switch, merged first-wins | After switching from database A to B, A's captions remain and **beat** B's | Reset to the built-in captions before each load |

### 7.3 Improvements

- Database work is `async`/`await` with cancellation; the Delphi blocks the UI thread behind a wait
  cursor. "Open in Excel" currently pumps messages in a `Sleep(50)` loop until Excel exits.
- Login collapses ~55 synchronous round trips per project switch (three `GetStudyAndUser`, three
  StudyId resolutions, two `GetDatabaseInfo`, ~40 dead percentile queries) into one pass.
- Patient-ID lists move from string-concatenated `IN (…)` to a table-valued parameter — one
  round-trip, one cached plan, and no exposure to SQL Server's 2100-parameter limit.
- `ILog` splits into `ILogger<T>` and `IUserNotifier`. Today every `Event` at `ltMessage` or above
  is a synchronous modal dialog raised **while holding the log lock**, and `LogYesNo` *fails open* —
  returning "yes" when below the dialog threshold.
- Real `.xlsx` export via ClosedXML alongside CSV, avoiding the CSV locale round-trip.
- Per-monitor DPI awareness.

---

## 8. Open decisions

Not blocking; each has a working default so implementation can proceed.

1. **Config compatibility direction.** Default: read the legacy XML only. Optional `<SqlOptions>`
   child element for `Encrypt`/`TrustServerCertificate`, which the Delphi would ignore, keeping the
   file usable by both builds.
2. **`Encrypt` default.** SqlClient 7 defaults to `Encrypt=true`; the legacy OLE DB strings carry no
   encryption settings, so a literal translation fails to connect on day one against on-prem servers
   with self-signed certificates. Default: `Encrypt=True;TrustServerCertificate=True`, overridable.
   This preserves today's connectivity but is not a security improvement — worth revisiting.
3. **`Autommunitet` typo** — preserved for now.
4. **`J01FF%` in the resistance-driving antibiotic set — needs a protocol owner, blocking for this
   collector only.** Commit `9f4a5ed4f` bundles the two new antibiotic collectors together with
   *removing* `J01FF%` (lincosamides / clindamycin) from the existing resistance-driving set, and
   renaming its caption from `Medisin: Resistensdrivende antibiotika` to
   `Antibiotika: Resistendrivende`.

   The evidence does **not** depend on identifying which ref shipped. `J01FF` is absent from
   **all 9** refs that carry the symbols this application needs, and present only on mainline, which
   cannot build the app at all. So every baseline capable of producing a working QuickStat binary
   lacks `J01FF%`. Implementation default: **drop `J01FF%`, take the new caption.**

   **This remains release-blocking for this collector.** It is a clinical definition — which
   antibiotics count as resistance-driving — and "the code has been this way" is not clinical
   sign-off. A protocol owner must confirm before release. It does not block implementation: the ATC
   list is one array, changing it later is a one-line edit plus a golden-file update.
5. **Drift items in `Docs/Port/03-collectors.md` §F — re-decided; §F now carries a correction
   block that overrides its own per-item verdicts.** Those verdicts were computed against
   `develop_old`; the shipping binaries were built against tarmscreening (§2.1), and all seven
   commits §F discusses are on that lineage. Rather than rest on which single ref shipped, each
   verdict was re-checked across **all 9 refs** that carry `QS_ROAS_BASE` — i.e. every candidate
   baseline capable of building this application. Three of the four are **unanimous** across all 9:
   `VarNames` returns `FVarOrder` (9/9), `J01FF` absent (9/9), `GFR` not `eGFR` (9/9).
   `SpSnapshotFormDataAll` is present on 5 of 9, including **both** tarmscreening refs and every
   candidate newer than 2022-05. So these defaults hold regardless of how R12 is decided.
   Verified at the branch **tip**, the resolved defaults are: **take** `SpSnapshotFormDataAll` +
   batch 200 (free-text form export has
   shipped since 2022 — declining it would remove a live feature); **take** `FVarOrder`
   insertion-order columns (alphabetical would reorder every existing export), still behind a
   `ColumnOrder` policy flip; **take** `RANK` → `ROW_NUMBER` with a deterministic tie-breaker;
   **use `GFR`, not `eGFR`**, in the two GBD renal titles (`eGFR` is mainline-only wording that
   never shipped — better clinically, so raise it, do not apply it silently); `SET_BDR_COMORBID` is
   moot (unregistered collector, dropped per §7.1); MNA→MST stays out of scope.
6. **Norwegian vs English chrome.** Today the chrome is English and the data-element titles are
   Norwegian. Default: keep that split exactly.
7. **Deployment.** Default: framework-dependent x64, matching today's xcopy deployment into
   `.\bin`. Self-contained single-file is a one-line change if preferred.

### 8.8 Surfaced during Phase 2 — implemented as stated, each reversible

Every one of these is a place where a Phase 2 step had to choose and the plan did not say. All are
implemented, tested and cheap to reverse; none blocks Phase 3. The first three change behaviour a
customer could notice, so they are the ones worth a human decision.

| # | Decision | Chosen | Reversal cost |
|---|---|---|---|
| a | Connection string with no `Initial Catalog` | **Rejected at translation.** The Delphi would have connected and used the login's default database | One line in `OleDbConnectionStringTranslator` |
| b | `TrustServerCertificate` when `Encrypt=True` is set **explicitly** | **Still injected**, per §8.2's wording. This silently weakens a deliberately requested verified TLS — §8.2's rationale was about strings carrying *no* encryption settings | One condition |
| c | `DatabaseVersionTooOldException` | **Now actually fires.** In the Delphi the check raises inside the `try..except` that sets `DbVersion := -1`, so it has never once reached a user. A customer on a pre-510 schema now sees an error instead of a silent fallback | Move the check back inside the guard |
| d | Unknown date of birth in an identified export | **Empty field.** The Delphi wrote its `TDate` zero sentinel as `30.12.1899`, i.e. a false date of birth in a clinical export. 2.5 and 2.6 chose empty independently and now agree by construction | One line, byte-test pinned |
| e | Empty population | **Header row only.** The Delphi wrote a phantom `"nil";"nil";…` row | One line |
| f | xlsx shading for cells with no datapoint | **Not shaded.** Materialising `EmptyCell` for every hole would blow up a sparse matrix whose documented worst case is 1500 × 1000. The screen shades them; the workbook does not | — |
| g | `ROW_NUMBER` tie-breaker on the **live** `SpSnapshotFormDataAll` | **Applied.** §F.3's diff only covered the dead `SpSnapshotFormDataNumeric`, but `ROW_NUMBER` without a tie-breaker is non-deterministic across ties, which would destabilise exports *and* Phase 5's golden files. Five characters of divergence from upstream text | One line |
| h | Settings file location | `<exedir>\Settings\QuickStat.ini` **if it already exists**, else `%APPDATA%\DIPS\QuickStat\`. Portable mode cannot happen by accident | — |
| i | `Population.Matches` culture (`nb-NO` vs invariant for `Ø`/`ø`) | **Resolved** — read the Delphi rather than choosing. See below | One argument |

#### 8.8 (i), resolved: the two list filters are not the same filter

Both list boxes case-fold and then do an **ordinal** substring search — Delphi `Pos`, not a
linguistic comparison — but they fold in opposite directions and disagree about trimming. Neither is
a judgement call; both are read straight out of the library on the pinned ref.

| | Population list | Packages list |
|---|---|---|
| Source | `Emetra.VclComp.ListView.pas:353-362, 482-518` | `Emetra.VclUtil.Spotlight.pas:143-146` |
| Case fold | `AnsiLowercase` **both sides** | `AnsiUppercase` **both sides** |
| Filter trimmed | **no** | **yes** (`Trim`) |
| Empty filter | matches everything (explicit `FFilter = ''` branch) | matches everything (explicit `lookFor = EmptyStr` branch) |
| Matched against | `AsListBox(false)` = `ProcId ⇥ Title ⇥ HelpText ⇥ Group` | `AsListbox(showSimple)`, and `showSimple` is `false` here because QuickStat passes no *Simplified* box |

`AnsiUppercase`/`AnsiLowercase` are **locale-sensitive**, so `CultureInfo.CurrentCulture` is the
faithful port and `ToLowerInvariant` is not. For Norwegian the two agree — `Ø`↔`ø`, `Æ`↔`æ`, `Å`↔`å`
map identically — so the original worry was unfounded; the case where they diverge is a machine
whose locale is Turkish or Azeri (`I`↔`ı`). Match with
`ToLower(CultureInfo.CurrentCulture)` + `StringComparison.Ordinal`, **not**
`StringComparison.CurrentCultureIgnoreCase`, which is a collation and would fold more than `Pos`
does.

`Population.SearchText` is confirmed correct against `CRF.Population.pas:94`
(`fListBoxText := V + #9 + DN + #9 + Description + #9 + OT`), independently of the Phase 1 check.

One trap for whoever reads that unit: **`TPopulation.Match` is dead code.** It exists, it uppercases,
and it would give different answers — but `TObjectListView.AfterUpdate` reaches it only through
`Supports(thisObject, IMatchable, …)`, and `TPopulation` does not implement `IMatchable`
(`TClinForm` and `TDatabaseUser` do, and list it explicitly). Porting `Match` instead of the
`AsListBox` path would silently change the filter's case-folding direction and make an empty filter
match nothing, since Delphi's `Pos('', s)` returns 0 and only the caller's explicit branch saves it.

#### A Phase 2 gap closed while preparing Phase 3: nobody loaded the captions

`CaptionDictionary`'s own documentation records that QuickStat runs exactly one caption query —
`QueryLabCaptions` over `dbo.LabClass` — but **no production code ever called `AddRange`**; only
tests did. Every lab column would therefore have fallen back to its own variable name with an empty
description, so the grid's header tooltips would have been blank for precisely the columns that need
them. `ICaptionRepository`, `ICaptionLoader` and `CaptionSql` close it, with the two Delphi bugs
above fixed on the way. **Phase 3 must call `ICaptionLoader.LoadAsync` once a session is
established**; the Delphi re-ran the query at the start of every collect run, which a reference
table does not require.

Also recorded, not decisions: `03-collectors.md` §B.7's kidney ordinals were one too high and are
corrected (the Delphi enum member is `lFibrinogen`, not `ltFibrinogen`, so any `lt`-prefix filter
drops it and shifts every later ordinal); `01-data-access.md` §3.1 lists nine SQL privilege error
numbers where the Delphi has seven; `04-matrix-export.md` §5.2 describes `develop_old` — a non-empty
`DataPoint.Caption` exports as text, in full, on **both** tarmscreening refs (`8486b3d09`), so that
behaviour does not depend on R12.

### 8.10 Still open after Phase 3 — carried into Phase 5

Nothing here blocks Phase 4. Each is recorded so it is not rediscovered.

| # | Item | Note |
|---|---|---|
| a | **No view can be instantiated under test if it uses `{StaticResource}` and does not merge the theme itself.** `StaticResource` resolves during parse, against `Application.Current`, which is `null` under test — and creating an `Application` would break every later test, since WPF allows one per `AppDomain`. 3.6 merged the theme into each of its own views' `Resources`; 3.1's, 3.2's, 3.3's and 3.4's views do not, so their markup is pinned structurally (as XML) and proved to load only by launching the executable | The alternative — merging in every view — duplicates every brush. A test-only `Application` on a dedicated STA thread would solve it properly and is the right Phase 5 job |
| b | **Two population-loading code paths.** 3.2 owns `PopulationPickerViewModel.TryLoadPopulationAsync`; 3.4's replay has its own ~30-line equivalent because 3.2's command was synchronous when 3.4 was written. Both are tested and agree today | Exactly the "two halves each locally correct" shape that produced defects 3 and 4 above. Collapse onto 3.2's method; deferred because both halves are green and the merge was already large |
| c | **The busy overlay's Cancel button can never appear.** `IShellProgress.BeginOperation(string)` takes no `CancellationTokenSource`, so nothing can offer one to `BusyOverlayViewModel.OfferCancellation` | One overload. The overlay is correct and tested; the button is simply never shown |
| d | **`QsProgressBar` has no indeterminate state**, so `IsIndeterminate="True"` renders a bar that never moves. 3.6 worked around it in its own view | One storyboard in the style |
| e | **Two literal glyph colours** (`#C42B1C`, `#9D5D00`) are written inline in 3.6's dialogs because agents may not add a brush | Promote both into §F.4 and the theme |
| f | **The busy overlay blocks the mouse but not the keyboard** — you can still tab into the shell beneath it | Disable `MainWindow`'s content while `IsBusy` |
| g | **`ICollectorRegistry.BuildAsync` hangs off `ISessionService.SessionChanged`**, fire-and-forget, rather than being awaited inside `ConnectionCoordinator.ConnectAsync` alongside the login and the caption load | Consider moving it, so "connected" means the collector list is ready |
| h | **Nothing has ever run against a database.** No collector has executed, no population has loaded, no package has been read or written, no period prompt has fired for a real query | This is Phase 5, and it is the largest remaining unknown by a wide margin |

### 8.9 Surfaced during Phase 3 wave 1 — two of these need a human

| # | Question | Status |
|---|---|---|
| a | **Three palette colours in `05-ui-spec.md` §F.1 describe `develop_old`, not the parity baseline** | **Needs a decision.** See below |
| b | **No `<Version>` is set**, so the banner reads `1.0.0.0` | **Needs a decision.** The shipped Delphi build is `22.12.21.547`, a date-derived FinalBuilder number. Not for an agent to invent; it is a packaging choice |
| c | §H.2 lists two cross-tab items; there is a **third**, `ExportTimestamps` — owned by the Collections tab, read by the Dataset tab's export commands | Resolved: it lives on `IShellWorkspace`. Recorded in `07-ui-contracts.md` |
| d | §C.3 is wrong in three places — fixed-column header alignment, missing horizontal grid lines, and a two-header-row tooltip rule for a grid with `FixedRows = 1` | Resolved toward the `.pas` in each case, with evidence. Recorded in the step 3.5 report and `07-ui-contracts.md` |
| e | §F.4 says the splitter is 8 px; §A.2 and the `.dfm` say 9 | Resolved: 9 |

**(a) in full, because it is the R11 failure mode landing again.** Step 3.1 found it while transcribing
§F.4 and it was verified independently:

| Constant | §F.1 / this repo's `FastTrak\` (`develop_old`) | `origin/tarmscreening/develop` (the pinned baseline) |
|---|---|---|
| `clCodeColor` — population/package id column | `$00A4294B` → **`#4B29A4`** purple | `$00888888` → **`#888888`** grey |
| `clStatusTextColor` — `ProcGroup` / `Pop#n` | `$00822EB8` → **`#B82E82`** fuchsia | `clMandatoryGeometryFill` = `$00054689` → **`#894605`** brown |
| `clFocusedSelectionColor` — grid current cell | `$00D4FBFF` → **`#FFFBD4`** pale yellow | `clSelectedBk` = `$00E9D9C8` → **`#C8D9E9`** pale blue |

Commit **`98f493bbc`** (2022-09-29, "Mindre retninger") made the change. It is on **both** tarmscreening
refs and predates the shipped `v22.12.21.547` by nearly three months, so by the same dated-chain
argument that settled R12 in §2.1, the binary customers actually run shows the **right-hand** column.
§F.1's pixel checks are against screenshots of build **19.8.14.477** from 2019, which predate the
change — so the screenshots and `develop_old` agree with each other and both describe the old
palette.

The theme currently ships the **left-hand** column, because §F.4 was transcribed as written and step
3.1 was right not to change a spec unilaterally. Reversal cost is three hex values in
`Theme/QuickStat.Brushes.xaml` plus the inventory test. **R13 applies: settle it by looking at the
deployed exe**, not by reasoning further — this is precisely the kind of "what ships today" claim
R11 warns is unverified in `01`–`02` and `04`–`05`.

**The deployed exe is on this machine, and it cannot be inspected statically.** Four copies exist —
`C:\Users\chs\Downloads\QuickStat.exe`, `…\Downloads\FastTrakUpgrade.v22011\bin\`,
`C:\work\Test\TempArea\bin\` and `C:\work\Medikamentutdelingsapplikasjon\ELDOK-TEST\bin\` — all
**byte-identical at 1 951 936 bytes**, all reporting file version **22.12.21.547**, product `22.12`.
So there is exactly one shipped binary to compare against and it needs no Delphi build, which
removes R13's obstacle for Phase 5.

But it is **UPX-packed** (sections `UPX0` 5 029 888 virtual / `UPX1` 1 892 352 raw / `.rsrc`), so the
code and data are compressed and only the resource directory is readable — which is why the version
resource reads fine. Two searches were run and both are **inconclusive, not negative**:

| Attempt | Result |
|---|---|
| The six disputed `TColor` values as little-endian dwords | 0 hits — but so were 5 of the 7 *undisputed* control values from §F.1, so absence proves nothing |
| Collector names and titles as ANSI/UTF-16 (`QST_LAB_INTERLEUKINS`, `Autommunitet`, `J01XX05`, …) | 0 hits — and so were the controls `J01FF` and `LabClassName`, which are certainly in any build |

No UPX binary is installed, and unpacking to read a colour constant would be disproportionate.
**Therefore R13's "check the deployed exe" means run it and look**, which is a human step and belongs
to Phase 5's parity pass. Do it against the population list (id and category columns) and a populated
grid (current cell) — three colours, one screen each.

---

## 9. Risk register

| # | Risk | Mitigation |
|---|---|---|
| R1 | `Encrypt=true` breaks every existing connection | Explicit defaults + a connectivity smoke test before rollout |
| R2 | Population SQL is stored **in the database**, not in this repo — arbitrary text with `:Name` parameters | The `:Name`→`@Name` rewriter needs a real scanner (skipping literals, `[]`, `""`, `--`, `/* */`, `::`) plus a dry-run diagnostic over production `SqlText` before release |
| R3 | The 150-entry collector registry is transcribed by hand | Golden-file SQL tests; the inventory table in `03-collectors.md` is the acceptance checklist |
| R4 | CSV byte-format drift (encoding, decimal separator, trailing separator) breaks downstream consumers | Byte-comparison tests against fixtures from the Delphi build |
| R5 | Custom grid control is the largest single piece of UI work | Time-boxed; `DataGrid` fallback documented with a ~150-column ceiling |
| R6 | Privacy regressions around anonymisation | Dedicated tests; treated as release-blocking |
| R7 | `KB.AntibioticResistance2` is an **inner** join in a non-`dbo` schema; a missing table fails the query outright rather than returning nothing | Register that collector only when `OBJECT_ID(...) IS NOT NULL` |
| R10 | Most `maxint`-batch collectors carry **no `{IdList}` at all** and scan the whole database, discarding non-cohort rows client-side | Pre-existing behaviour, preserved for parity; recorded as a separate performance follow-up, not fixed during the port |
| R8 | Period semantics are `[Start, Stop)`, end-exclusive | Getting this wrong shifts every cohort by a day; explicit tests |
| R9 | No database available to the implementation agents | All DB-touching work must be unit-testable without a server; a human runs the parity pass |
| R11 | **Wrong parity baseline.** The five `Docs/Port/` analyses were written against *this* repo, which is a reduced copy (§2.1). Their "what ships today" statements describe `develop_old`, a combination that cannot build the application | **Resolved for §F** (2026-08-25) — see §8.5 for the corrected verdicts and the invariance evidence. **Correction:** an earlier revision of this row claimed the cited commits were ancestors of `origin/tarmscreening/develop` "and of no other branch". That was wrong — only two refs were tested. `4c96c3c3b` is contained by 27 refs; 9 remote tips carry `QS_ROAS_BASE`, including two release branches. Only `fefc8a809` (interleukins) is genuinely narrow, at 3 remote tips. The corrected verdicts survive this because they were re-checked across **all 9** candidate refs, not one. **Still open elsewhere:** any *other* "what ships today" claim in `01`–`02`, `04`–`05` is unverified — confirm against the pinned ref before relying on it |
| R12 | **Which of the two sibling tarmscreening refs is the baseline** — they disagree on interleukins, i.e. 131 vs 130 collectors | **Resolved** (2026-08-26) in favour of `origin/tarmscreening/develop`, target **131**. The app-side and library-side interleukin commits landed the same day (2022-12-13) and the shipped exe is v22.12.21.547, matching the version-bump commit eight days later; `release/tarmscreening` forked three weeks before interleukins existed. See the table in §2.1. Residual risk is clinical, not archaeological, and is covered by §8.4 |
| R13 | **QuickStat probably has no working build.** `QuickStat.fbp8` resolves the library through `$(FastTrakDir)`. Locally that defaults to `c:\work\FastTrak`, which is on `master` and lacks every symbol — **verified**. Under Continua it binds to the `$Source.FastTrakDevelop` source, whose tracked branch **has not been observed**; if it is `develop` (as the name implies) CI cannot succeed either, but that step is inference, not fact | Regardless of how the Continua half resolves, do not rely on a Delphi build as a check — nobody has demonstrated one succeeding. Phase 5's parity pass runs against the **existing deployed exe**, not a freshly built one. **That exe is already on this machine** — four byte-identical copies of `22.12.21.547`, listed in §8.9(a) — so this row no longer blocks Phase 5. It is UPX-packed, so it must be *run*, not read. To settle R13 properly, someone with Continua access should read the `FastTrakDevelop` source definition; it is a five-minute check and it would either confirm this row or overturn it |
| R14 | **Reading uncommitted working trees as if they were the shipped state.** This has now caused one wrong conclusion (see §2.1) and one near-miss (`C:\work\FastTrak` sits on `master`, which lacks the tarmscreening lineage) | For every repo outside this one, read through `git show HEAD:<path>` or a pinned worktree, and run `git status --porcelain` before quoting a file as evidence. `C:\work\FastTrak.BuildServer` currently has an uncommitted `QuickStat.fbp8`; `C:\work\FastTrakApps` has a dirty `.dproj`. The library worktree at `C:\work\FastTrak-tarmscreening` exists precisely to remove this failure mode — extend the same discipline to the other two repos |

---

## 10. Acceptance criteria

1. `dotnet build QuickStat.slnx` and `dotnet test` pass with warnings as errors.
2. `QuickStat.exe` starts, reads an unmodified `QuickStat.config.xml`, and connects.
3. Every collector in the `03-collectors.md` inventory appears in the list with its exact title,
   including class-applied suffixes, and the five restored registrations are present (126 → **131**
   distinct names, from four features). 131 assumes the ref pinned in §2.1; it is 130 if R12 is
   re-decided toward `release/tarmscreening`.
4. Study gating is exact: a `KORTTID` study registers the same static collectors as `GBD` and
   `LANGTID`. This is commit `5502b72`, and it lives in *two* near-identical regex literals — the
   single easiest thing to lose in transcription. Gate matching is case-**sensitive** except
   `DOGFOOD`.

   **The target is 124, not 120.** Counted per registration procedure in both trees:

   | | always | gate **G** | gate **N** | `KORTTID` total | distinct names |
   |---|---|---|---|---|---|
   | This repo (reduced) | 36 | 76 | 8 | **120** | 126 |
   | FastTrakApps (canonical) | 37 | 79 | 8 | **124** | 131 |

   The `120` quoted in earlier revisions of this plan and in `Docs/Port/03-collectors.md` §D.2 is
   *this repo's* number and therefore describes the reduced build. The +4 are the three antibiotic
   collectors (inside `AddCollectorsDrug`, which the **G** block calls) plus interleukins
   (always-on). `QS_ROAS_BASE` is `ROAS`-gated and so does not move the `KORTTID` count. If R12 is
   ever re-decided toward `release/tarmscreening`, this becomes 123 / 130.

   Note `5502b72` has a twin: the identical fix also sits on
   `feature/739506_GBD_utvalet_i_Korttid` in `FastTrakApps/App.QuickStat` (its current branch). The
   two agree, so the gating regexes are not in doubt — only the totals above depend on which tree
   you count.
5. "Fully identified patients" produces national IDs.
6. CSV output is byte-identical to the Delphi build for a fixture dataset.
7. The three identification modes behave exactly as specified in §6.
8. A human parity pass against `05-ui-spec.md` finds no unexplained differences.
