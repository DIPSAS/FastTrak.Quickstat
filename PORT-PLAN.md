# QuickStat → WPF / .NET 10 port plan

Status: **in implementation — Phase 0 complete (`3b1f8c0`). Resume at Phase 1.**
Branch: `feature/dotnet`
Last updated: 2026-08-25

> **Resume here.** Phase 0 landed and its exit criteria were verified independently: clean Debug and
> Release builds with zero warnings, `dotnet test` 4/4 in both, `QuickStat.exe` at x64, launches with
> a foreign working directory and exits 0, `LOGS\` created beside the exe. Next action is **Phase 1**
> (one blocking agent, contracts only) using the file-ownership map in §5.
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

- **Nothing on this branch is pushed.** `origin` (`github.com/DIPSAS/FastTrak.Quickstat`) has exactly
  one branch, `main` @ `ad80437`. `feature/dotnet` is local-only, and that includes `5502b72` — the
  KORTTID fix is *not* published from here. Push before relying on any of it surviving this machine.
- **The KORTTID fix has a twin.** `FastTrakApps/App.QuickStat` carries the identical change on
  `feature/739506_GBD_utvalet_i_Korttid`, which is that repo's currently checked-out branch. The two
  agree; see §10.4 for why the *totals* still differ between the trees.
- **`App.QuickStat` had no code change between 2023-01-05 and 2026-08-24.** The only commit in that
  window is `c3f84e3` (2023-02-04, "Testdatabase lagt til i config.") which touches configuration,
  not code. The last substantive work is the interleukin support and version bump of December 2022,
  so anything describing "recent" QuickStat behaviour is describing a 2022 build.
- **Build definitions live in `C:\work\FastTrak.BuildServer`** (FinalBuilder, Continua CI).
  `QuickStat.fbp8` is the QuickStat build; see R13 for why it cannot succeed.
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

Step 2.4 is by far the largest: **126 distinct collector names** (87 built through the factory, 39
constructed directly) plus `2 × N` dynamic per-form collectors. Split it by collector family —
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

### Phase 3 — User interface  *(parallel; six agents; needs Phase 1, and Phase 2 for live data)*

| Step | Scope | Owns |
|---|---|---|
| 3.1 | Theme: resource dictionary, brushes, typography, control styles, shell window with banner + progress + splitter | `QuickStat.App/Theme/**`, `QuickStat.App/Views/ShellView.xaml{,.cs}` |
| 3.2 | Population tab | `QuickStat.App/Views/PopulationView.*`, `QuickStat.App/ViewModels/PopulationViewModel.cs` |
| 3.3 | Collections tab (data elements, Collect data, export options) | `QuickStat.App/Views/CollectionsView.*`, `.../CollectionsViewModel.cs` |
| 3.4 | Packages tab | `QuickStat.App/Views/PackagesView.*`, `.../PackagesViewModel.cs` |
| 3.5 | Dataset grid control | `QuickStat.App/Controls/MatrixGrid/**` |
| 3.6 | Save-specification dialog, period dialog, busy/progress overlay | `QuickStat.App/Views/Dialogs/**` |

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
| R13 | **QuickStat probably has no working build.** `QuickStat.fbp8` resolves the library through `$(FastTrakDir)`. Locally that defaults to `c:\work\FastTrak`, which is on `master` and lacks every symbol — **verified**. Under Continua it binds to the `$Source.FastTrakDevelop` source, whose tracked branch **has not been observed**; if it is `develop` (as the name implies) CI cannot succeed either, but that step is inference, not fact | Regardless of how the Continua half resolves, do not rely on a Delphi build as a check — nobody has demonstrated one succeeding. Phase 5's parity pass runs against the **existing deployed exe**, not a freshly built one. To settle R13 properly, someone with Continua access should read the `FastTrakDevelop` source definition; it is a five-minute check and it would either confirm this row or overturn it |
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
