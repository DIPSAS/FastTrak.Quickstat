# QuickStat → WPF / .NET 10 port plan

Status: **in implementation — Phases 0–5 largely complete. Resume in Phase 5 (see below).**
Branch: `feature/dotnet`
Last updated: 2026-09-01

> **For "what is left", read `PORT-REMAINING-TODO.md` instead.** It is one page, ranked by what
> would stop a release, and every line points back into this document. This file is the record of
> *why*; that one is the list of *what next*. Keep them in step.

> **Resume here. Phases 0–4 are complete and Phase 5 is done bar two items that need a person, not a
> machine.** The application is built, the functionality lost in the extraction is restored, and —
> since 2026-08-27 — both it and the shipped Delphi build have been run against a real database and
> their exports compared. **2 588 tests** pass with zero warnings under the machine's own `nn-NO`
> plus `nb-NO` and `en-US`. The banner reads **`26.0.0.0`**.
>
> **Phase 5 — see §8.11 and §8.14 for the detail.** Golden SQL files for all 131 collectors,
> independently re-derived from the Pascal (131/131 match); all 131 bind against a live catalog and
> satisfy the five-column contract; **all 213 data elements then executed against that database, none
> threw**, and the port exported the result through its own CSV writer; **all of §8.10 done**;
> the shipped `22.12.21.547` build was set up, driven through a full collect, and **its export
> compared with the port's, cell by cell: 0 differences in 12 462 cells** across three identification
> variants (§8.14). All three of §8.9 (a)'s disputed colours are now sampled off that running binary.
> **Six real defects came out of the exercise, none reachable without a server:** a table type that
> never existed, which was silently blanking `Fødselsnummer`; an ICU-vs-NLS sort that put five export
> columns in the wrong place; a disposal order that meant the application **never closed its own
> session row**; a current-cell colour that was three years out of date; a colour blend that
> truncated where Delphi rounds; and a packages list painting its unfocused selection with the grid's
> blend result. All six are fixed, each with a regression test, and the last three were
> negative-controlled by reverting the production change and watching eight tests fail.
>
> **A seventh came out of the manual pass on its first screen, and it is the interesting one:
> double-clicking a population did nothing at all** — §8.11 (5). `<MouseBinding
> MouseAction="LeftDoubleClick">` inside `ListBox.InputBindings` never fires on a row, because
> `ListBoxItem` handles the mouse-down the binding needs. The package replay, which has no keyboard
> equivalent by design, was unreachable by **any** input. It is the only defect so far that a test
> had actively locked in: the case covering it asserted the markup, found the binding it expected,
> and passed. **A test that asserts what was typed cannot notice that what was typed does not
> work** — which is the argument for the whole live-verification exercise in one line. Fixed, and
> the negative control is now permanent rather than performed once.
>
> **An eighth, two screens later, is the same lesson one level up — and it took two goes** —
> §8.11 (6). The dataset grid had no scrollbars at all: `MatrixGrid` implements `IScrollInfo` in full
> and its own class documentation says to host it in a `ScrollViewer` with `CanContentScroll="True"`,
> and `DatasetTabView.xaml` did not, so every one of those methods was unreachable. The suite covered
> all of them — including a case then named `TheWheelMovesThreeRows` — **by calling them itself. A
> test that calls an API cannot notice that nothing else does.** Hosting the grid fixed the bars and
> was reported as still not fixing the wheel, which was correct: `TCustomGrid.DoMouseWheelDown` does
> `Row := Row + 1`, so the wheel moves the **caret**, one row a notch, and scrolls only to follow it.
> On the 31-patient cohort every row fits, so a correct scroll implementation and a dead one look
> identical. Both halves are now measured against the running binary through UI Automation: four
> notches move the caret 85 px, which is four rows to the pixel, and the vertical bar appears and
> disappears with the window.
>
> **A ninth and a tenth came off the same screen minutes later** — §8.11 (7). The floating data hint
> did not follow the caret, because `05-ui-spec.md` §G.2 asserted it should not and **§G.2 was
> wrong**: a VCL `Click` is raised by `FocusCell`, so the hint follows the arrow keys and the wheel
> in the shipped build. The spec is corrected in place. And an unhandled
> `ArgumentOutOfRangeException` **terminated the process** on a collect run: the grid's cell
> accessors indexed the matrix through cached counts, and a collect clears the columns and then
> awaits for minutes. Painting re-syncs every frame and so never noticed; the automation peer, which
> WPF drives on every layout pass once anything is listening, did. I met it by attaching UI
> Automation to the user's running instance to measure (6) — real, and a hard kill for any
> screen-reader user. The four new cases fail without the guard.
>
> **An eleventh was found while measuring the ninth and tenth, and is the same shape as the eighth**
> — §8.11 (8). Every one of the four item lists announced its rows to a screen reader as an object's
> `ToString()`: the database combo read out the whole connection string, the 213 data elements read
> out `QuickStat.ViewModels.DataElementViewModel`. `DisplayMemberPath` and an `ItemTemplate` both fix
> what is *drawn* and neither touches the name. It was reported and left for the user to call, and
> fixed on their word. **The estimate in the report — one `AutomationProperties.Name` binding per
> list — named the right binding and was half the fix**: the binding covers rows that have
> containers, and what was leaking is the fallback the peer uses when they do not, so `ToString()` is
> overridden on all four item types as well. A record's generated `ToString` prints every property,
> which is how the raw `<ConnectionString>` got one step from a screen reader; the deployed file
> names a UDL and leaked nothing, but the format allows credentials, so this is treated as privacy
> rather than polish. **2 528 tests**, nine of them new, negative-controlled in both directions.
>
> **A twelfth came from the parity pass, and the spec had it right all along** — §8.11 (9). Ticking
> `Show data hint` did nothing until the user clicked another cell. In the Delphi the check box's
> handler *is* `UpdateDataHintPanel`, which hides the panel and then rebuilds it from `fGrid.Col` /
> `fGrid.Row` — the *current* cell — and the port had only the hiding half, under a comment asserting
> the opposite. §G.2's first bullet says "triggered by `fGrid.OnClick` **and** by toggling
> `cbShowDataHint`"; that is the third defect here where the spec was read past rather than wrong.
> The rebuild has to cross the view-model/control seam, since only the grid knows where the caret is,
> so it goes the way `GridRefreshRequested` already goes. **2 534 tests**, four of them new,
> negative-controlled on each side of that seam separately.
>
> **The pass also asked for two things the Delphi does not have** — §7.3. `Show source` under the
> population tip, which opens the `CREATE PROCEDURE` pane and replaces an access right the port had
> no way to grant (§I.9 is struck through); and an `Export ⌄` button in the Dataset caption bar,
> which drops down the grid's own right-click menu from somewhere it can be found. The second is the
> *same* menu rather than a copy, and a test compares the two item by item. Both are marked in the
> parity checklist so they are not read as drift. **2 546 tests.**
>
> **A thirteenth and a fourteenth, both from the same pass, and both are the port being faithful to
> something that was never right** — §8.11 (10) and (11). `Save this dataset to CSV file` is black on
> a freshly started QuickStat, because `actSaveDataset` has `Enabled` unset in the `.dfm` and nothing
> ever assigns it; its neighbour latches on and never off. Both now hang on one predicate — columns
> *and* a lock — rather than a patch on the reported half, since they sit next to each other on one
> menu and fail for the same reason. And `Open this dataset in Excel` went through `ShellExecute`,
> which honours the `.csv` association and so opened whatever the machine had; the Delphi resolves
> Excel's COM registration and starts *that*, and now so does the port. Its parsing could not be
> transcribed: the Delphi splits the command line on a space, which only works in the registry view a
> 32-bit process reads. **2 565 tests.** The negative control for the first also caught a case that
> had been passing against the very defect it existed to catch.
>
> **The pass also moved the population tip and gave the rows a tool tip** — §7.3. `lblHintPopulation`
> sat at the foot of the tab, below the frame, the source pane and `Show source`, and read
> `Tip: Double click to prepare population`. It now sits directly above the list and reads
> `Double-click on a population to select it`; every row carries `Double-click to select this
> population`, on the row and not on the list, so the blank space under the last one stays silent.
>
> **Packages are done too, since 2026-09-01 (§8.11 (13)).** They were the last untouched area and
> the only one that cannot be tested read-only, because they live server-side in `Report.QuickStat`.
> The product owner authorised writes to `EFT00028_TEST_020`, so `C:\work\qs-packages` drives the
> real repository against the real table: 29 checks, 29 passed, table restored. It also found that
> `Report.AddQuickStat` is an upsert keyed on title — which the Delphi does not know, and shows a
> duplicate list row for.
>
> **§6 of the checklist — the whole Packages tab — is closed too, and it cost a fifteenth defect**
> (§8.11 (14)). Every one of its nine items was driven through the running window and measured
> rather than eyeballed: the two selection colours came back `#C8D9E9` and `#E7F2FC` exactly, the
> filter is trimmed here and not on the population list, the replay's *uncheck* half is proved by a
> three-element package followed by a two-element one, and both dangling-reference warnings read
> back with real line breaks. The one that failed was *"title bold"*: `FontWeight="SemiBold"` drew
> the title pixel for pixel identically to the comment below it. **`SemiBold` is a weight this
> application does not render.** On the owner's instruction of 2026-09-01 every remaining use was
> resolved — eight to `Bold`, two removed — and that exposed a defect the inert weight had been hiding
> since the theme was written: `QsTabItem` set `FontWeight` **and `FontSize`** on the `TabItem`, both
> of which inherit into the tab's whole page. The selected tab's content had been drawing at 13 px
> instead of 12 all along, and went bold the moment the weight became real. §8.11 (15).
>
> **What is left in Phase 5 needs a person.** The `05-ui-spec.md` walkthrough is still a human job,
> now written out as **`Docs/Port/08-parity-checklist.md`**:
> ~50 items that need eyes, everything else marked as already covered and by what. The port
> launches from **`C:\work\qs-run\run.ps1`**, which stages a working `QuickStat.config.xml` and a
> correctly encoded UDL beside the executable; the scripts beside it now drive the window as well as
> launch it. **§8.10 is fully closed.**
> Two structural differences between the two CSVs are known, attributed and deliberate — read §8.14
> before treating either as a bug.
>
> **A field report from 2023 was handed over on 2026-08-27 and is now root-caused (§8.13):** date of
> birth and sex go missing from a SWEET extract. They are the only two items on `SWEET_PATIENT` that
> are `MetaFormItem.Expression` macros over the person record, so QuickStat is reading a client-written
> copy of two `NOT NULL` columns it already holds in memory. Pre-existing product behaviour, faithfully
> ported; the fix is three lines of policy and one decision, and it is not made.
>
> **One release-blocking question still needs an owner, and it is not code:** the `J01FF%` clinical
> definition (§8.4). The other — the spelling of `KB.AntibioticResistance2` — was **settled on
> 2026-08-27: the object exists, as a view, under exactly that name.** The availability gate stays
> regardless; its job is the many customer databases that have no such object at all.
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
> **Phase 4 found one more of the same kind, and it was in this plan.** §5, R7,
> `CollectorAvailability`'s own XML docs and the placeholder comment in `CollectorCatalog.Drug.cs`
> all said that **two** collectors join `KB.AntibioticResistance2`. Only one does:
> `JOIN KB.AntibioticResistance2` occurs exactly once in the library, at `EPR.QA.SQL.pas:453`, and
> `SpDrugsetAntibioticRecommended` (`:431`) is a plain nine-code `ot.ATC IN ( … )`. Gating both would
> have deleted a working collector from every database without the knowledge-base schema. Caught by
> reading the Pascal before briefing the implementation, not by a test — no test could have failed,
> because the wrong behaviour was what every document specified. All six sites now name the one
> collector and say why the other is unconditional.
>
> Two more came out of the culture sweep, which is now a permanent, opt-in file
> (`QuickStat.Tests/CultureSweep.cs`, `-e QUICKSTAT_TEST_CULTURE=xx-YY`) because three agents each
> built and discarded one: `SqlParameterFactory` threw out of its own error message under a
> non-Gregorian calendar, and a collector-SQL assertion used a collation where it meant a byte scan.
> Both fixes are permanent and right on any locale. **The sweep itself is now `nb-NO` + `en-US`
> only** — Bokmål because it is the field locale and is not this machine's, English because build
> agents default to it. `tr-TR`, `ar-SA` and `th-TH` were dropped on 2026-08-27 at the product
> owner's direction: QuickStat ships to Norwegian hospitals, so each was a full extra run of the
> suite guarding a scenario no user can reach.
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
| Dependencies | CommunityToolkit.Mvvm, Microsoft.Data.SqlClient, Microsoft.Extensions.{DependencyInjection,Logging}, ClosedXML, **Serilog** (see below), xUnit. No commercial control suites |
| UI | Same information architecture, tab names, control placement and workflow; modern flat rendering. No attempt to imitate a "Delphi look" |

#### The one dependency added after planning: Serilog (2026-08-27)

`Docs/Port/01-data-access.md` §7.5 left the choice open — "file logging: a small custom
`ILoggerProvider` (**or Serilog, if the team prefers a dependency**)" — and Phase 0 took the custom
route. The deciding argument at the time was that the log file is a *compatibility surface*: the
format was to stay byte-compatible with the Delphi's plaintext log so existing ops tooling kept
reading it, and under Serilog that would still have meant a hand-written sink and formatter, so the
dependency bought nothing.

**The product owner withdrew that requirement on 2026-08-27: the format is not worth preserving,
only what is logged, when and how.** Write this down, because it is the whole reason the answer
changed — without it, the next reader re-derives the Phase 0 conclusion and reverts this. With the
format free, the custom provider was left hand-rolling four things `Serilog.Sinks.File` already
does: daily rolling, retention, shared-file access and encoding. Three files were deleted and
`Serilog`, `Serilog.Extensions.Logging` and `Serilog.Sinks.File` added (versions matched to the
sibling project `FastTrak.PersonInfoSync`, so the two DIPS C# applications do not drift).

Three constraints hold, and are enforced by structure rather than by intent:

1. **Serilog sits behind `Microsoft.Extensions.Logging`, not in front of it.** `QuickStat.Core`
   still references only `Microsoft.Extensions.Logging.Abstractions`; every call site logs through
   `ILogger<T>`. Serilog appears only under `QuickStat.App/Logging/` — not even `App.xaml.cs` names
   a Serilog type — so replacing it again is a change to one directory. `FastTrak.PersonInfoSync`
   takes the opposite route, a `global using Serilog` in every project; that is deliberately **not**
   copied, because it would put a third-party logging type in `QuickStat.Core`'s public surface.
2. **R6 survives the swap unchanged.** `QuickStatLogFormatter` is still the single choke point where
   `PiiRedactor` runs, and the five tests in `QuickStat.Tests/Logging/FileLoggerRedactionTests.cs`
   are unchanged assertion-for-assertion — only the four lines that build the pipeline differ. They
   were re-verified by negative control on the Serilog path.
3. **Two Serilog defaults had to be overridden**, both found by a failing test rather than by
   reading: its template parser eats the `{{ }}` PII convention (it reads `{{` as an escaped `{`,
   which MEL does not), and it renders string values in quotes (`'"HbA1c"'` for templates that
   already quote). See the class remarks.

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
| `Docs/Port/06-contracts.md` | Phase 1's output: the type surface of `QuickStat.Core`, and which files each Phase 2 step owns |
| `Docs/Port/07-ui-contracts.md` | Phase 3 step 3.1's output: what the wave-2 view-model agents inherit from the shell |
| `Docs/Port/08-parity-checklist.md` | Phase 5's manual pass, walkable: ~60 items needing eyes, everything already covered marked as such, and the deliberate differences collected so they are not reported as defects |

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

### 2.3 The database schema — `C:\work\FastTrak.Database`

A SQL Server database project holding the schema QuickStat queries. Known to the port from
2026-08-27; **nothing before that date was written with it available**, which is why §8.10 (h) reads
as bleakly as it does.

**Trust `Upgrades\` first.** The owner's caveat: the repository is *not necessarily* completely up
to date except for `Upgrades\`, and changes there do not necessarily affect QuickStat. So
`FastTrak.Schema\` is a strong hint and `Upgrades\Upgrade*.sql` is the authority. When the two agree
— as they do for the `KB` antibiotic views, checked — the answer is solid.

What it is good for, in rough order of value to the remaining work:

- **Static validation of every collector's object references, with no server.** 131 collectors name
  tables, views, synonyms, functions and procedures; all of them can be resolved against
  `FastTrak.Schema\<schema>\<kind>\<name>.sql`. That is the cheapest available attack on §8.10 (h)
  and it belongs in Phase 5 next to the golden files. Spot checks so far all resolve:
  `Report.GetFormClasses`, `Report.LabClassName`, `Report.ColDrugAndRenalFunction`, `Report.NorGeP`,
  `dbo.OngoingTreatment`, `KB.AntibioticResistance2`.
- **Column-level checks.** `KB.AntibioticResistance2` really does expose `AtcCode`, which is what
  `SpDrugsetAntibioticIntermediate` joins on.
- **Resolving indirection.** `dbo.KBAtcIndex` is a **synonym** for `FEST.AtcIndex`, so the
  collectors' `LEFT JOIN dbo.KBAtcIndex` and the `KB` views' `FROM FEST.AtcIndex` read the same
  table. Worth knowing before anyone "fixes" one to match the other.
- **Clinical definitions that the Delphi only half-encodes** — see §8.4, where the three
  `KB.AntibioticResistance*` views settle what the `J01FF%` argument was circling.
- `AGENTS.md` at the root documents house conventions: the population return shape
  (`PersonId, DOB, FullName, GenderId, GroupName, InfoText`), `dbo.ViewActiveCaseListStub`,
  `dbo.GetLastQuantityTable` / `dbo.GetLastEnumValuesTable`, and the FormName-vs-FormTitle trap.

It is **not** a substitute for a live database: it says an object exists, not that a query returns
the right rows, and it carries no data. The Phase 5 parity pass still needs a server.

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
  losing all logs. *(Phase 5: the provider behind it is now Serilog — see §1.1. The file is
  `quickstat-<user>@<machine>-yyyyMMdd.log`, ten kept, level overridable with
  `QUICKSTAT_LOG_LEVEL`, falling back to `%LOCALAPPDATA%\DIPS\QuickStat\logs` when `LOGS` cannot be
  created.)*
- Application icon from the existing `QuickStat_Icon.ico`. The banner's 32 × 32 icon is a *different*
  picture, carried inline in `MainQuickStat.dfm` as `imgAppIcon.Picture.Data`; extracted to
  `QuickStat_Banner_Icon.ico` — see `Docs/Port/05-ui-spec.md` §A.3.
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

> **Phase 4 is complete — both halves.** All five registrations are in the catalog, the registry is
> at **131** distinct names, and a `KORTTID` study registers **124** — **123** on a database where
> `KB.AntibioticResistance2` does not resolve, because `QS_DRUG_ANTIBIOTIC_INTERMEDIATE` is the one
> collector behind the availability gate. `QS_ROAS_BASE` is behind the `ROAS` gate and so does not
> move the `KORTTID` count. The **national-ID export** is restored as
> `QuickStat.Core/Domain/Patients/NationalIdRecovery.cs`, called from both load paths.
>
> Every number above was re-derived from the built assembly rather than read off a test: 131 in the
> catalog, `RequiredDatabaseObjects` exactly `[KB.AntibioticResistance2]`, and 124 / 123 for
> `KORTTID` under the two probe outcomes. The generated SQL for all five collectors was dumped and
> compared against the verbatim blocks in `Docs/Port/03-collectors.md` §E, and the 68-id
> `SET_ROAS_BASE` and 11-id `LABCLASSES_INTERLEUKINS` arrays were parsed straight out of
> `EPR.QA.Definitions.pas` and compared element by element, order included.

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
- **Exactly one collector needs that gate: `QS_DRUG_ANTIBIOTIC_INTERMEDIATE`.** `JOIN
  KB.AntibioticResistance2` occurs once in the whole library, at `EPR.QA.SQL.pas:453`, inside
  `SpDrugsetAntibioticIntermediate` — which has no ATC clause at all and delegates its entire
  selection to that table. `SpDrugsetAntibioticRecommended` (`EPR.QA.SQL.pas:431`) is a plain
  `ot.ATC IN ( … nine codes … )` and touches no `KB` object, so
  `QS_DRUG_ANTIBIOTIC_RECOMMENDED` is registered unconditionally. Gating it as well would make a
  perfectly working collector vanish on every database without the knowledge-base schema — a
  functional regression, not a safety measure.
- **The `J01FF%` question is settled in §8.4 — read that, not an earlier revision of this bullet.**
  Commit `9f4a5ed4f` also drops `J01FF%` (lincosamides / clindamycin) from the *existing*
  resistance-driving set and renames its caption. An earlier revision of this bullet said "do not
  take the removal"; that was written before the question was re-checked across all nine refs
  capable of building the application, every one of which lacks `J01FF%`. §8.4 supersedes it and
  Phase 2 implemented §8.4, so `DrugSql.ResistanceDrivingAtcPatterns` is `J01CR%`, `J01D[CDH]%`,
  `J01MA%` and the caption is `Antibiotika: Resistendrivende`. **It stays release-blocking for this
  one collector** until a protocol owner signs off on the clinical definition; Phase 4 did not
  touch it.
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

Three decisions that were settled while implementing it, recorded so they are not silently reversed:

- **The fetch is unconditional**, guarded only by "the population procedure did not already return
  the column". `02-populations-patients.md` §8.5 sketched it as conditional on the identification
  mode; that is wrong, because `IIdentificationPolicy` raises `ModeChanged` and the mode is
  switchable *after* a population is loaded — a load-time fetch gated on `Full` leaves
  `Fødselsnummer` silently blank for anyone who loads first and switches second. The port already
  loads name and date of birth regardless of mode and lets `IdentificationColumns.For` decide what
  is displayed and exported; the national id is no different. R6 is about what leaves the process.
- **It runs in both load paths** — the Populations tab and the package replay — and in both it must
  precede `PersonMatrix.PreparePopulation`, which copies the id onto the row it builds and never
  reads the patient again.
- **A failed recovery is logged and degraded, not fatal.** Because the fetch is unconditional, a
  fatal failure would destroy a load in `PersonIdOnly` or `RandomPersonId` whose result never needed
  the ids. `OperationCanceledException` is re-thrown: that means the whole load is being abandoned.

### Phase 5 — Verification  *(agents + human; largely done, see §8.11)*

- ~~Golden-file tests over generated SQL for every collector.~~ **Done.**
  `QuickStat.Tests/Collectors/Golden/` holds 131 files, each derived from the Delphi Pascal by a
  reader forbidden from seeing `QuickStat.Core/Collectors/`; 131 of 131 match. Changing one means
  changing it *from the Pascal* — a corpus regenerated from our own output would agree with its own
  bugs and prove nothing.
- ~~**Resolution of every database object the 131 collectors name.**~~ **Done, and better than
  planned.** A live catalog was available, so instead of parsing the schema project each generated
  statement went through `sp_describe_first_result_set`, which binds every object, column and join
  and executes nothing. All 131 bind; all 131 satisfy the five-column contract. The schema project
  (§2.3) remains the fallback where no server is available — and remember `dbo.KBAtcIndex` is a
  synonym, so a static resolver has to follow those.
- **Byte-for-byte CSV comparison tests against fixtures captured from the Delphi build.** *Still
  open.* `Export/CsvByteParityTests.cs` exists but its fixtures are derived from the specification,
  not captured from a run. Capturing real ones needs a collect run in the shipped build followed by
  an export — the app is set up for it (§8.9 a) and the study to use is `NDV`.
- ~~Unit tests for connection-string translation, `:Name`→`@Name` rewriting, anonymisation, the
  colour ladders, and matrix assembly.~~ **Already covered** by `ConnectionStringTranslatorTests`,
  `SqlTextRewriterTests`, `MatrixAnonymiserTests`, `RgbTests` and `PersonMatrixTests`.
- **Manual parity pass against a real database.** *Still open, and still a human job* — but it now
  has a written form: **`Docs/Port/08-parity-checklist.md`**, which walks `05-ui-spec.md` in
  workflow order and separates what a person has to look at (~60 items) from what a test or a
  measurement already settles. It also collects the deliberate differences in one place, so the
  walker does not spend the pass rediscovering decisions, and marks where four other acceptance
  criteria close along the way. Three things make it cheap: the shipped build is installed and
  working at `C:\work\qs-delphi`, its controls can be read programmatically (§8.9 a), so "what does
  the reference actually show" is a query rather than a squint, and the port launches from
  `C:\work\qs-run\run.ps1` with the configuration already staged.
- **Fix the two palette values now known to be wrong**, and measure the third (§8.9 a).

### Phase 6 — Cleanup  *(one agent; only after sign-off)*

**Signed off in principle on 2026-08-27, and deliberately not started.** The product owner's words
were "perfectly fine to do so … but don't launch the phase just yet", so the gate is now timing
rather than permission: it waits on the parity pass (§10.8), because the walk needs
`C:\work\qs-delphi` and the reference worktree still standing. Nothing below has been done.

Remove the reference worktree:
`git -C C:\work\FastTrak worktree remove C:\work\FastTrak-tarmscreening`.

Delete the Delphi tree: `*.pas`, `*.dfm`, `*.dpr`, `*.dproj`, `*.groupproj`, `*.dsv`, `FastTrak/`,
`Spring/`, `Test/`, `build.bat`, `build.ps1`, `copy-missing-files.ps1`, `fix-namespaces.ps1`, `QuickStat.chp`,
`QuickStat.manifest`, `DbFormExport*`, `FormExport.SqlGenerator.pas`. Rewrite `readme.md` for the
.NET build. Keep `Docs/`, `QuickStat.config.xml`, `FastTrak.UDL`, `QuickStat_Icon.ico`,
`QuickStat_Banner_Icon.ico`.

**One test dies with the `.dfm` and has to be replaced, not deleted.**
`Ui/AppBannerIconTests.TheBannerIconIsTheOneTheDelphiFormCarries` re-extracts
`imgAppIcon.Picture.Data` from `MainQuickStat.dfm` and compares it byte for byte with
`QuickStat_Banner_Icon.ico`; that is the only thing tying the banner's picture to the build being
ported. When the form goes, replace the comparison with the recorded SHA-256 of the extracted file
rather than dropping the case.

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
  demographic elements **last**. Sorted both ways against the real KORTTID registry (120 collectors
  when step 3.3 ran the experiment, 124 since Phase 4): ordinal leads with an `Antibiotika:` title
  and trails with the `^ ` group; any linguistic ignore-case comparer reproduces
  `Docs/Screenshots/QuickStat bilde 2.png` exactly — `^ Alder … ^ Statuskode`, then `Antropometri`,
  `Diabetes:`, `Labdata:`, `NDV:`. The conclusion does not depend on the count — the test re-runs it
  against whatever the registry currently holds.

  The mechanism is `LBS_SORT` on a Win32 list box, whose default comparison is `CompareStringW` with
  `LOCALE_USER_DEFAULT` and `NORM_IGNORECASE` — culture-sensitive by construction, exactly like
  `CurrentCultureIgnoreCase`. The port is therefore culture-dependent in the same way the Delphi is;
  nb-NO, nn-NO and en-US agree on the whole registry except one adjacent pair
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
| Pseudonym RNG never seeded (`Randomize` appears nowhere) | Random PIDs are identical across every run and every machine, yet differ between two exports in one session — reproducible where it should not be, unstable where it should not be | Keyed derivation from a per-dataset CSPRNG secret; stable for the lifetime of a loaded dataset, independent between datasets. The second half needs a *reset point*, and the port initially had none — §8.11 (12) |
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
- **Four changes to the Population and Dataset tabs the Delphi has not got, all asked for by the
  product owner during the parity pass (2026-08-28) and all flagged in
  `Docs/Port/08-parity-checklist.md` so the pass does not read them as drift.**
  - **`Show source`** at the foot of the population tab, opening the `CREATE PROCEDURE` pane. This one
    *replaces* something: in the Delphi the pane is not a setting at all, it is visible exactly when
    `FUNC_POPULATION_SOURCE` is granted, and the frame registers that right as `asDenied`. The port
    has no access control, took the registered default, and so could never show the pane — while the
    owner's own build shows it open, which settles the question the default was standing in for. One
    flag, off at start-up; access control, if it arrives, hides the check box. `05-ui-spec.md` §I.9
    is struck through accordingly.
  - **`Export ⌄`** in the Dataset tab's caption bar, dropping down `mnuGridPopup`. The Delphi's
    three dataset actions — the package, the Excel export and the CSV export — are reachable only by
    right-clicking the grid, which a user has to already know. **The same menu, not a second copy**:
    one `DatasetActionsMenu` resource with `x:Shared="False"`, an instance for the grid and one for
    the button, and a test that compares the two item by item rather than against a transcript. The
    right-click is unchanged.
  - **The population tip moves up to the list and is reworded.** `lblHintPopulation` is
    `Align = alBottom` on the tab, which in the port put it below the frame, below the source pane
    and below `Show source` — three controls away from the list it instructs. It now sits directly
    above the list, inside the frame, and reads `Double-click on a population to select it` rather
    than `Tip: Double click to prepare population`; "prepare population" was `PreparePopulation`, an
    internal verb no part of the screen uses. `PopulationTipTests` measures the two rectangles
    against each other, because a case asserting the text alone would pass with the label back at
    the foot.
  - **Every population row carries a tool tip**, `Double-click to select this population`. Set on the
    container and not on the `ListBox`: `ToolTipService` walks *up* from whatever is under the
    pointer, so a tip on the list would also answer for the empty space below the last row, where
    there is no population to double-click.
- **Three more from the same pass, on the product owner's instruction of 2026-09-01.**
  - **The `Unique name` box stops at 80 characters** — §8.11 (13). `Report.QuickStat.Title` is
    `varchar(80)` and `Report.AddQuickStat` upserts on `(StudyId, Title)`, so a longer title does
    not lose its tail, it overwrites the package sharing its first 80, silently. `edtTitle` has no
    `MaxLength`; the box is the only layer that can say anything, because nothing below it raises.
  - **`SemiBold` is gone from the application** — §8.11 (14) and (15). Eight uses became `Bold`,
    which is what §F.2 says the Delphi draws; two were removed, because the Delphi draws those
    plain. The population tip above the list is bold on the same instruction: muted grey and bold
    reads as an instruction rather than as a caption.
  - **The modals put OK first and Cancel second**, left to right — the platform order.
    `Emetra.VclForm.EditAndMemo.dfm` has it the other way (`btnSave.Left` 280 against
    `btnClose.Left` 184) and so does `Emetra.VclForm.Period.dfm`, but `NotificationDialog` has
    always read `Yes` then `No`, so the application disagreed with itself as well as with Windows.
    Sizes and the 4 + 4 + 16 spacing are still §E's; only the order moved. All three dialogs now
    pin it — `TheButtonBarPutsOkFirst` in both `SaveSpecDialogTests` and `PeriodDialogTests`, and
    the pre-existing `Yes`/`No` case in `NotificationDialogTests`.
- **A filter box on the Collections tab, on the product owner's request of 2026-09-02** —
  `05-ui-spec.md` §B.2 item 2a, `08-parity-checklist.md` 3.7. `cbDataCollector` has no filter, and
  the largest study in the field puts **530** data elements in it, so finding one means scrolling.
  It is deliberately the *population tab's* box rather than a new design: same label
  `Filter / search text`, same placeholder, same rule — lowercase both sides in the current culture,
  ordinal `Contains`, **not** trimmed (§8.8 (i)) — because the two sit two tabs apart in one window.
  It matches the title only, which is the one thing a row shows; the collector name is a persistence
  key and matching it would leave rows standing for a reason nothing on screen explains.
  - **It hides rows and nothing else**, and that is the whole design. `DataElements` is the check
    list, and the check list is the export column order (§6); the box drives
    `VisibleDataElements`, a projection holding the same instances in the same order. So a ticked
    element the filter is hiding is still collected, still in the same column, and `Collect data`
    stays enabled for it. The alternative — filtering the list itself — would make a keystroke
    silently drop a column from an export, with the evidence hidden by the very filter that caused
    it. `TheFilterHidesRowsWithoutChangingWhatIsCollected`.
  - **A projection and not an `ICollectionView`**, which is how the other two filters in the shell
    are built. A `CollectionView` captures `Dispatcher.CurrentDispatcher` at creation and throws on
    any change raised from another thread, and this is the one list filled from off the UI thread:
    `ICollectorRegistry.Rebuilt` is raised inside `BuildAsync` — §8.10 (g) — and marshalled with
    `IUiDispatcher`. Correct in the running application, but it would have made this the only shell
    view-model a test cannot drive without a real `Dispatcher`, which is what `InlineUiDispatcher`
    exists to avoid. It cost one `ObservableCollection`; `ConnectionCoordinatorTests` found it.

---

## 8. Open decisions

Not blocking; each has a working default so implementation can proceed.

1. **Config compatibility direction.** Default: read the legacy XML only. Optional `<SqlOptions>`
   child element for `Encrypt`/`TrustServerCertificate`, which the Delphi would ignore, keeping the
   file usable by both builds.
2. **`Encrypt` default.** SqlClient 7 defaults to `Encrypt=true`; the legacy OLE DB strings carry no
   encryption settings, so a literal translation fails to connect on day one against on-prem servers
   with self-signed certificates. Default: `Encrypt=True;TrustServerCertificate=True`, overridable.
   This preserves today's connectivity but is not a security improvement.

   **Settled 2026-09-01 — closed for this port, on scope.** The product owner's ruling, with the
   estate's actual picture: **`FastTrak.exe` just uses the UDL**, whose initialisation string
   carries no encryption keywords at all, and **at least one .NET service sets `Encrypt=false`**
   with no `TrustServerCertificate` beside it - checked, not reported. So there is no single house
   setting for this port to match or depart from —
   which is exactly why it is not QuickStat's decision to take. The connection-level posture across
   the estate belongs to whoever owns the platform. Recorded so that closing it is a decision rather
   than an omission, and so the earlier draft of this row — which claimed the port matched what
   every application already did — does not stand as a fact.
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

   **Second, independent line of evidence — the database's own classification agrees** (added
   2026-08-27 from `C:\work\FastTrak.Database`, §2.3). `KB` carries three sibling views that
   partition the antibiotics into resistance tiers, and `Upgrades/UpgradeTo19025.sql` names them in
   its own header comments:

   | View | Header comment | Definition |
   |---|---|---|
   | `KB.AntibioticResistance1` | *PREFERABLE antibiotics* | `J01CA08`, `J01CA11`, `J01XE01`, `J01C[EF]%`, `J01E%` |
   | `KB.AntibioticResistance3` | *HIGH RISK antibiotics* | `J01CR%`, `J01D[ABCDEHI]%`, `J01M%` |
   | `KB.AntibioticResistance2` | *INTERMEDIATE antibiotics* | everything `J01%` plus `A07AA09`, `P01AB01`, **`EXCEPT`** 1 and 3 |

   `J01FF` is in neither the preferable nor the high-risk view, so by construction it falls into
   `AntibioticResistance2` — **the database classifies clindamycin and lincomycin as intermediate,
   not resistance-driving.** Dropping `J01FF%` therefore does not merely follow the buildable refs;
   it brings the collector into line with the knowledge base the same product ships.

   *Correction, 2026-09-01:* an earlier revision said the string `J01FF` "appears nowhere in the
   database repository at all". True of the current tree, not of its history: `git log --all -S`
   finds it in exactly two commits, `7a5cafc5` (2016-07-01) adding and `5fade677` (2019-06-20)
   removing a `dbo.KBInteraction` seed script. Those rows are **drug–drug interaction** pairs for
   `J01FF01` — nephrotoxicity with `J01G`, prolonged muscle relaxation with `M03A…` — and say
   nothing about resistance tiers. The conclusion is unaffected; the claim needed the qualifier.

   **Third line of evidence — the chronology, and it answers "isn't the version without `J01FF`
   simply older?"** (asked 2026-09-01; checked in `C:\work\FastTrak`, which is the library history,
   not the flattened `FastTrak\` copy in this repository). **No — the removal is the later act, by
   the author of the original.**

   | When | Commit | What |
   |---|---|---|
   | 2018-04-08 | `222e6f54e`, Magne Rekdal | **Creates** `SpDrugsetAntibiotic`, all four patterns, `J01FF%` among them from birth |
   | 2019-12-28 | `af72c67a`, Magne Rekdal | Creates the three `KB.AntibioticResistance*` tier views. Never modified since |
   | 2020-09-21 | `9f4a5ed4f`, Magne Rekdal | **Removes** `J01FF%`; renames the collector and its caption; adds `SpDrugsetAntibioticRecommended` |

   A `-G 'J01FF'` sweep over **all** refs finds exactly those two commits ever touching a `J01FF`
   line in `EPR.QA.SQL.pas`. Mainline still carries the untouched 2018 text — `origin/master`
   (tip 2026-08-26), `origin/develop` (2026-05-07), `origin/develop_old` (2024-04-12) — and none of
   them descends from `9f4a5ed4f`. A tip sweep: **92 refs carry `J01FF`, 28 lack it, and 27 of the
   28 descend from `9f4a5ed4f`** (the 28th, a local `feature/eResept`, has no antibiotic collector
   at all).

   **It is not one of that commit's rebase artefacts, despite its "Rebase on develop." subject.**
   Its entire diff to `EPR.QA.SQL.pas` is **32 insertions and 3 deletions**, and the three deletions
   are the two `SpDrugsetAntibiotic` declarations being renamed to `SpDrugsetAntibioticResistance`
   plus the `J01FF%` line itself. Nothing else in the file was lost, so this is a surgical edit
   inside a coherent refactor — unlike the BDR and NEWS2 losses elsewhere in the same commit.

   What *is* true is that the lineage carrying the removal is dead — `origin/tarmscreening/develop`
   stops at 2023-09-01 — while mainline, which never received it, is still developed today. So this
   is an **unmerged divergence, not a stale-versus-current question**, and both readings have a
   defensible claim on "what the product means now". That is precisely why it needs a clinician and
   not an archaeologist.

   **This remains release-blocking for this collector.** It is a clinical definition — which
   antibiotics count as resistance-driving — and neither "the code has been this way" nor "the
   `KB` views say so" is clinical sign-off. A protocol owner must confirm before release. It does not
   block implementation: the ATC list is one array, changing it later is a one-line edit plus a
   golden-file update.

   **Give the owner this too, because it is the same question one level up.** The collectors and the
   `KB` views are two definitions of the same clinical concept, maintained separately, and they do
   not agree — the collectors hard-code ATC lists while the views are patterns over `FEST.AtcIndex`:

   | Concept | Collector | `KB` view | Divergence |
   |---|---|---|---|
   | Resistance-driving | `J01CR%`, `J01D[CDH]%`, `J01MA%` | `J01CR%`, `J01D[ABCDEHI]%`, `J01M%` | the view is **broader** — the collector misses `J01D[ABEI]%` and `J01M` outside `J01MA` |
   | Recommended | nine literal codes | `J01CA08`, `J01CA11`, `J01XE01`, `J01C[EF]%`, `J01E%` | the collector is a **snapshot enumeration**; any newer `J01CE`/`J01CF`/`J01E` code in `FEST.AtcIndex` is in the view and not in the collector |
   | Intermediate | *joins the view directly* | — | the only one of the three that cannot drift |

   Not a Phase 4 change — the collectors are what shipped, and rewriting two of them to join the
   views is a clinical decision, not a porting one. Recorded so the owner decides once, with the
   whole picture.
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

### 8.10 Still open after Phase 4 — carried into Phase 5

None of these blocked Phase 4, and none is fixed by it. Each is recorded so it is not rediscovered.
Item (b) grew slightly: both population-loading paths now also call `NationalIdRecovery`, so there
are two copies of one more step — which is exactly the argument for collapsing them.

**(a) through (g) are all closed in Phase 5**; the rows are kept, struck through, because their "why"
is still load-bearing — (a)'s is the reason the test suite now looks the way it does, (c)'s is why
cancelling is an addition rather than parity, and (d)'s is why the busy overlay does *not* spin.
(h) is a summary row rather than a task; see §8.11.

Three of the seven turned out to be **larger than the note said**, and in each case the note's own
suggested mechanism was half of the answer: (c) needed the register of offers to move onto the
service, not just an overload; (f) needed focus *moved* as well as the content disabled, or the
keyboard is stranded rather than blocked; (g) needed an event on the registry, because awaiting a
build is only useful if the subscribers have run too.

| # | Item | Note |
|---|---|---|
| a | ~~**No view can be instantiated under test if it uses `{StaticResource}` and does not merge the theme itself.**~~ | **Done** (Phase 5). `QuickStat.Tests/Ui/WpfApplicationFixture.cs` puts the shipped `App` — so the merge is `App.xaml`'s, not a second copy of it — on one dedicated background STA thread, behind a `static Lazy` for the single-instance guarantee and an `ICollectionFixture` for the handle. `Ui/ViewInstantiationTests.cs` constructs all seven views plus `MainWindow` through it and sweeps their bindings for `System.Windows.Data Error: 40`. Two consequences worth knowing: the assembly now runs sequentially, because `Application` registers every `Window` on unsynchronised collections from whichever apartment made it; and `Assert.Null(Application.Current)` is no longer a legitimate assertion anywhere, since nothing can un-set it — the four places that used it now read the window's own `Resources` instead, which is what they meant |
| b | ~~**Two population-loading code paths.**~~ | **Done** (Phase 5). `QuickStat.App/Services/PopulationLoader.cs` is now the one place the sequence exists — resolve placeholders, cohort query, `NationalIdRecovery`, prepare the matrix, tell the workspace. Both view models take the loader *instead of* `IPatientRepository` and `IQueryParameterResolver`, which they only ever held in order to write their own copy, so neither can assemble a second one. It lives in `QuickStat.App` and not `Core` because its last step is `IShellWorkspace.SetPopulation`; moving the first four to `Core` and leaving the fifth to each caller would split the ordering contract in half again. Differences that are real stayed parameters or stayed at the call sites (logger, progress scope, audit row, failure wording, `RequestCollectionsTab`); differences nobody chose — what an unresolvable placeholder reports — are marked in code and left alone |
| c | ~~**The busy overlay's Cancel button can never appear.**~~ | **Done** (Phase 5). `IShellProgress` gained `BeginOperation(string, CancellationTokenSource)` and, with it, `IsCancellable` and `RequestCancellation()`. **The plan's note said "one overload", and that is half of it**: an overload on its own hands a `CancellationTokenSource` to `ShellProgress` with nothing able to read it, because the overlay's register was private to `BusyOverlayViewModel`. The register therefore moved *onto the service* and `OfferCancellation` is gone — the operations are started by the tab view-models, none of which can reach the overlay, and a second register kept on the view-model would be a second thing able to disagree with the first. The overlay is now purely derived, which is what its own header always claimed. The one call site is the collect run, which links a source to `AsyncRelayCommand`'s own token so either end stops the same run; it is the only operation that both takes minutes and honours the token all the way down — between collectors, between batches in `CollectorRunner`, and inside the statement. Two candidates were examined and deliberately left alone: the package replay, because nothing re-reads its token after the inner collect returns, so a Cancel there would stop the run the user can see and then finish the replay anyway (its inner collect offers one, which covers the only unbounded stretch); and `ConnectionCoordinator.ConnectAsync`, whose token is the caller's, not its own. Worth recording because it is easy to assume otherwise: **cancelling is an addition, not parity.** The Delphi has no Cancel on the main form at all — the product's only two `bkCancel` buttons are on modal dialogs (`Emetra.VclForm.Period.dfm:169`, `Emetra.VclForm.EditAndMemo.dfm:994`) — and `actCollectDataExecute` could not be interrupted once started |
| d | ~~**`QsProgressBar` has no indeterminate state**, so `IsIndeterminate="True"` renders a bar that never moves. 3.6 worked around it in its own view.~~ | **Done** (Phase 5). One storyboard, in a `MultiTrigger` in the template. Two things decided rather than guessed. **The geometry is a fraction, not a pixel offset**: `ProgressBar.SetProgressBarIndicatorLength` sizes `PART_Indicator` from `PART_Track.ActualWidth` and gives it the full 100 % while the flag is set, so a `ScaleX` of 0→1 over it is a fraction of whatever the splitter has left the bar — the usual `TranslateTransform` sweep would be in device-independent pixels and wrong after a resize. The bar grows from the left edge and shrinks into the right; `RenderTransformOrigin` is what anchors it and is animated with two *discrete* key frames, since changing it invalidates the element's transforms, while `ScaleX` alone goes to the composition thread. **The clock is released, not parked**: `RepeatBehavior="Forever"` never completes on its own, so `ExitActions` *removes* the storyboard — both properties then fall back to their base values, which are the determinate identity transform — and `IsVisible` is a second condition alongside `IsIndeterminate`, because the busy overlay collapses its subtree rather than unloading it and would otherwise leave a live animation clock in the `MediaContext` for the rest of the process. `Ui/Theme/ProgressBarIndeterminateTests.cs` pins all of it from rendered geometry rather than from the markup, so a different future template still passes; against the unmodified style its first test reads *"the indicator took only 1 distinct positions in 2.3 s"*. **The busy overlay keeps determinate progress** and the workaround is gone rather than moved: §G.6 gives the collect run a real percentage per patient, so the reason not to spin is now the data, which is what `BusyOverlayView.xaml`, `BusyOverlayViewModel.Percent` and `BusyOverlayTests` say instead of what they used to say. §F.4's row records the state so the next reader does not rediscover the gap |
| e | ~~**Two literal glyph colours** (`#C42B1C`, `#9D5D00`) are written inline in 3.6's dialogs because agents may not add a brush~~ | **Done** (Phase 5). `QsErrorBrush` and `QsWarningBrush` are in `QuickStat.Brushes.xaml`, in §F.4, and in the inventory test. Three call sites, not two: `AppBannerView` had the same red inline for the failed status line (§G.2), which is the actual argument for the promotion — one of the two colours already existed twice and could drift. The hex is unchanged. `NotificationDialogTests` still asserts the literal rather than the key, on purpose: it is a rendering assertion and must keep failing if someone repoints the brush |
| f | ~~**The busy overlay blocks the mouse but not the keyboard.**~~ | **Done** (Phase 5). The note's mechanism — disable `MainWindow`'s content — is the right one and **is not sufficient by itself**, which `Ui/Shell/MainWindowBusyLockoutTests.cs` measures rather than assumes. `IsEnabled = false` on the content host does stop the keyboard *arriving*: `Focus()` on anything inside returns `false`, tab traversal skips the whole subtree, and unlike `KeyboardNavigation.TabNavigation="None"` it also covers access keys, `Ctrl+Tab` and typing. But it does **not** evict focus that is already inside. The focused control stays focused while disabled and then handles no input, so a user who was in the check list when the run started would have the keyboard stranded on a dead control with the Cancel button unreachable — worse than the bug. `MainWindow.OnBusyOverlayVisibilityChanged` therefore does both: disable the content host, move focus onto the overlay (which is `Focusable` for that one reason, and puts Cancel one `Tab` away), then re-enable and put focus back where the user left it. It hangs off the overlay's `IsVisibleChanged` and not off `IsBusy` because a collapsed element cannot take focus and the binding may not have run yet, and the order is load-bearing in both directions: disable before taking focus, re-enable before giving it back. One cost, accepted: the shell is drawn greyed under the scrim for the duration, which `Screen.Cursor := crSqlWait` did not do |
| g | ~~**`ICollectorRegistry.BuildAsync` hangs off `ISessionService.SessionChanged`**, fire-and-forget, rather than being awaited inside `ConnectionCoordinator.ConnectAsync` alongside the login and the caption load.~~ | **Done** (Phase 5). `ConnectAsync` is now login → captions → `BuildAsync` → `Done`, all awaited, and the Collections tab renders the result instead of fetching it: it empties the list on `SessionChanged` and fills it from a new `ICollectorRegistry.Rebuilt`, which `BuildAsync` raises *before it returns* — so the caller's `await` is also an await on the check list being on screen. That is what the Delphi got for free: `AfterLogin` (`MainQuickStat.pas:471-493`) is a login observer that `TSimpleDatabase.Connect` calls synchronously (`Emetra.Database.Simple.pas:391-406`), so `SelectConnection` cannot give the mouse back before `cbDataCollector` is populated. `TXT_LOADING_COLLECTORS` moved to `ConnectionCoordinator` with the query it describes. **Was it a race?** See §8.12 — the answer is "not the obvious one" |
| h | ~~**Closed, 2026-09-01.** Was: "nothing has ever run against a database"~~ | See §8.11. Against `EFT00028_TEST_020`: all **131 collectors bind** (`sp_describe_first_result_set`, which resolves every object, column and join without executing anything), all 131 satisfy the five-column positional contract, a population loads, and the port's 213-element data-element list was built from the same `Report.GetFormClasses` rows and compared to the shipped build's. That row then went further than it said: **all 213 collectors executed** (§8.11 (3)) and **both builds' exports were compared cell by cell** (§8.14). The last untouched area — packages, which cannot be tested without **writing** — was closed on 2026-09-01 once the product owner authorised writes to that one database: §8.11 (13) |

### 8.11 What Phase 5 found by running things

Seventeen entries, and the list grew as the phase went on: the first three came out of finally having
a server, the later ones out of the product owner's manual parity pass, one out of being
allowed to write to that server, and the last out of measuring what that pass could only squint at.
Each defect is fixed and each
has a regression test that fails if it comes back — every one of those tests was negative-controlled
by reverting the production change and watching it fail. (3) is not a defect but the thing this phase
existed to do.

**(1) `SqlOptions.PersonIdListTypeName` named a table type that has never existed.** It defaulted to
`"Report.PersonIdList"`, a name that comes from `Docs/Port/03-collectors.md` §C.4 item 2 — which
*proposes* the migration. It is in no Delphi source, in none of the 1 422 schema files or 375 upgrade
scripts of the schema project, and not in `EFT00028_TEST_020`; the only user-defined table type in
the whole product is `dbo.QuantityTableType`. `PatientSql.NationalIdRequests` branches on whether the
name is *set*, not on whether the type *exists*, so every run bound a TVP of an unknown type and the
server answered `Msg 2715: Cannot find data type Report.PersonIdList`. `NationalIdRecovery` caught it
and degraded — so the symptom was a blank `Fødselsnummer` column, i.e. **exactly the bug Phase 4
restored the feature to fix** (§10.5). The collectors escaped only because `AddCollectors`
hard-registers `InlineLiteralPersonIdListBinder`.

The default is now `null`, the shared chunked path, which recovers 342 national ids for the first 500
patients of the test database. §C.4 item 3 asks for a `TYPE_ID` probe at login, exactly as
`CollectorAvailability` already probes `OBJECT_ID`; that is the proper fix and is not built. **Every
test took the TVP path by default**, so the suite only ever exercised the branch production could not
reach — which is why nothing caught it, and why the tests that mean to use the table type now have to
ask for it by name.

**(2) The data-element list was sorted with ICU where the Delphi sorts with NLS.** Column order in
every export is check-list order, and the check list is a Win32 list box with `LBS_SORT`, i.e.
`CompareStringW(LOCALE_USER_DEFAULT, NORM_IGNORECASE)`. The port used
`StringComparer.CurrentCultureIgnoreCase`, documented as "the .NET equivalent, down to reading the
user's locale". The locale part is right; the collation part is not — since .NET 5 the framework
collates with ICU and the list box with NLS, and they disagree about punctuation. Measured against
the running build: of 213 data elements 208 agreed, and NLS puts the five `Skjema: Antall …` at
positions 41-45 where ICU puts them at 209-213. Five columns at the wrong edge of every file a
customer has scripts against — the drift §6 forbids. `QuickStat.App/ViewModels/LbsSortComparer.cs`
now calls `CompareStringEx` instead of approximating it.

**The 131-entry static catalog sorts identically under both comparers**, which is why no test could
see this: the collision only exists once the per-form elements from `Report.GetFormClasses` are in
the list, because they are what introduce `Skjema-alder:` and `Skjema-data:`. This is §8.10 (h) in
one sentence.

**What was confirmed rather than corrected**

- **All 131 collectors bind** against a real catalog — `sp_describe_first_result_set` resolves every
  object, column and join and executes nothing. Zero failures.
- **All 131 satisfy the five-column positional contract.** One collector projects a non-string at
  ordinal 1 (`DRUG.NorGEP`, `EXEC Report.NorGeP`, whose `VarName` is an `int`); `SqlRow.GetString`
  coerces it exactly as Delphi's `AsString` did, so it is parity-correct, but nothing pinned it.
- **`R13` is settled in practice: the shipped build runs.** `22.12.21.547` was copied to a scratch
  folder, pointed at the test database and driven to a loaded population. The legacy `SQLOLEDB`
  provider **cannot** reach SQL Server 2022 here (`[DBNETLIB][ConnectionOpen] SQL Server does not
  exist or access denied`); `MSOLEDBSQL` connects. That is worth knowing independently of the port.
- **Golden files for all 131 collectors**, re-derived from the Pascal by readers forbidden from
  seeing the C#. 131 of 131 matched, so the registry transcription is sound (R3). Two of the four
  derivations additionally re-extracted the Pascal string concatenations mechanically.
- **§6's linguistic-sort conclusion is now observed, not reasoned.** The `^ ` elements really do come
  first in the shipped build, so the "sort ordinally" instruction in three documents really is
  backwards.

**(3) Every collector has now executed, and the port exported the result.** The paragraph above this
one used to say the opposite; here is what replaced it.

Binding is not rows, so a second pass ran the port's own pipeline — `AddQuickStatConfiguration`
through `AddQuickStatExport`, the same services `App.xaml.cs` composes, in `PopulationLoader`'s and
`CollectDataAsync`'s order — against `EFT00028_TEST_020`, with no window. It is a scratch console
outside the repository (`C:\work\qs-harness`), deliberately not a test: it needs a live catalogue, and
driving the WPF shell to do the same thing would have needed the screen.

| | |
|---|---|
| Cohort | `Populations.GetStudyPopulations` → 55 populations; ProcId 282 *"Diagnoseår mangler"* (31) and ProcId 14 *"Alle testpersoner"* (281) |
| National ids | **280 of 281 recovered** — the Phase 4 feature, against a real server, through the chunked path §8.11 (1) switched it to |
| Registry | `BuildAsync` → **213 data elements**, the same 213 the shipped build shows |
| Collectors | **213 ran, 0 threw**, on both cohorts |
| Rows | 29 collectors returned data; 184 returned none |
| Export | `DatasetExporter` wrote 281 × 101 and 281 × 201 (timestamps) CSV files |

**The 184 empty results are the database, not a defect, and that was checked rather than assumed.**
`dbo.DrugPrescription` and `dbo.DrugTreatment` are **empty**, so every `DRUG.*` collector is correctly
silent. `dbo.LabData` holds 132 rows for 17 people, and **none of those 17 is in NDV** — zero overlap
with `dbo.StudCase` for study 2 — so no NDV cohort can have lab data here and every `LAB.*` collector
is correctly silent too. What did return rows covers every collector *shape*: `PATIENT.*` and
`STUDY.*` (static registry), `FORM.*` and the per-form data collectors (built from
`Report.GetFormClasses` at login), `FORMS/FORMS12M/FORMS24M.FREQUENCY` (batched), and `NDV.INSULIN` /
`NDV.TREATMENT` (study-gated).

**What the export bytes show.** No BOM; `CRLF` only, one per row plus the header, and the file ends
with one; `;` after **every** field including the last; every field quoted; `"PID";"AGE";"SEX";` as
the header, i.e. VarNames and not titles; and with timestamps on, `"AGE";"AGE.DATE";"SEX";"SEX.DATE";`
— §6's format, observed rather than specified.

One caveat, and it is about *this* evidence rather than about the code: the run produced **not one
byte above `0x7F`** and only six decimal commas in 93 kB, so the live files say nothing about CP1252
or about `%g` with a comma. Those two are not unpinned — `Export/CsvByteParityTests` asserts them at
byte level (`ø = 0xF8`, `æ = 0xE6`, `"3,5"`) — but those fixtures are specification-derived, which is
exactly what acceptance criterion 6 asks to replace. A cohort with Norwegian text in a data column is
what would let a live run confirm the part the specification currently vouches for on its own.

**(4) A third real defect, and the worst of the three: the application never closed its session
row.** Running the harness produced **"The connection did not close cleanly"** twice on every clean
exit, with an `ObjectDisposedException`. That is noise, and noise was the small half of it: the same
teardown order meant `dbo.CloseSession` never ran either, so **every `dbo.UserLog` row was left
open** — indistinguishable, to anyone reading that table, from a crash, which is precisely what its
`DirtyClose` column exists to tell apart.

The cause is a **factory alias to a disposable singleton**. `ServiceProvider` disposes in reverse
order of *capture* and captures once per descriptor that yields the instance, so an alias books a
second disposal slot at the moment it is first resolved. `SessionService` took `QuickStatDatabase`
concretely (captured early); a repository asked for `ISqlExecutor` later (same instance, captured
late); the late slot was disposed first, and the connection died before the session that owns it.
Nothing in the suite could see it: the two existing disposal tests resolve `ISqlExecutor` *first*,
which happens to produce the right order.

Fixed by making `ISqlExecutor` the primary registration and the concrete type the alias — then
`SessionService` cannot be built without resolving the alias, which resolves the primary, so the
database is always captured first whatever the resolution order — and by making both types
idempotent on disposal, since the instance is still captured twice. For `SessionService` that is not
tidiness: without it, `dbo.CloseSession` runs a second time for a session already closed.

Measured, not argued: sessions **814** and **815** were run down the application's own shutdown path
before the fix and still have `ClosedAt` NULL; session **816**, the same path after it, has
`ClosedAt` set and `DirtyClose = 0`.

**(5) The first defect found by the manual pass, and the only one a test had actively locked in.**
Double-clicking a population did nothing. Not slowly, not wrongly — nothing: no query, no log line,
no error. The port had issued no statement to the server for the whole minute after the click, which
is how it was caught rather than argued (`sys.dm_exec_sessions.last_request_end_time` against a
`PopulationPickerViewModel` that logs every load at `Information`).

Both lists said the obvious thing:

```xml
<ListBox.InputBindings>
  <MouseBinding MouseAction="LeftDoubleClick" Command="{Binding PreparePopulationCommand}" />
</ListBox.InputBindings>
```

**An `InputBinding` is matched while the input event bubbles through the element whose collection
holds it, and `ListBoxItem` marks the mouse-down it selects itself with as handled.** The event stops
one level below the `ListBox`, so the gesture never arrives. It is silent rather than broken —
no binding error, nothing in a log — and a double-click on the list's blank area *does* work, which
is the perfect camouflage: it looks right whenever the list is short.

The population list survived it because `Enter` is bound separately and works. **The packages list
did not**: its replay had no keyboard equivalent, by design — `lbPackagedGrids` is a plain `TListBox`
with `OnDblClick` and no `TObjectListView.DoKeyPress` — so the package replay was **unreachable by
any input at all**. That is §8.10 (h)'s untouched area, and it would have failed on contact.

Fixed with `QuickStat.App/Input/DoubleClick.cs`, an attached `Command` that hangs off
`Control.MouseDoubleClick`. The routing that makes it work is *not* the one that reads naturally, and
was got wrong once on the way: `MouseDoubleClick` is registered `RoutingStrategy.Direct` and does not
bubble. What happens instead is that `Control` class-handles the bubbling mouse-down **with
`handledEventsToo`**, so the list still sees it after the item has handled it, and raises its own
direct `MouseDoubleClick` on itself. `Ui/Input/DoubleClickTests.cs` drives that whole path with a
real `Mouse.MouseDownEvent` at `ClickCount` 2 rather than raising the double-click event directly —
a synthetic shortcut is what hid this in the first place.

**The lesson is about the test, not the markup.** `PopulationViewMarkupTests.EnterAndDoubleClickBothPreparePopulation`
covered this line and passed throughout: it read the `.xaml`, found a `MouseBinding` with the right
gesture bound to the right command, and asserted exactly that. *A test that asserts what was typed
cannot notice that what was typed does not work.* The negative control is now permanent rather than
performed once — `TheSpellingItReplacesNeverFires` builds the old markup, drives the same double
click, and asserts the command is never reached — and a repo-wide case forbids `MouseBinding`
reappearing in any view.

**(6) The dataset grid could not be scrolled with the mouse, and had no scrollbars.** Reported from
the manual pass two screens after (5): the wheel moves the patient list in the shipped build and did
nothing in the port.

`MatrixGrid` implements `IScrollInfo` in full — `LineUp`/`LineDown`, `PageUp`/`PageDown`,
`MouseWheelUp`/`MouseWheelDown`, `SetVerticalOffset`, `MakeVisible`, `ExtentHeight`, `ViewportHeight`
— and its own class documentation states the host requirement in as many words: *"Put it inside a
`ScrollViewer` with `CanContentScroll="True"`; the control implements `IScrollInfo` and drives both
bars itself."* `DatasetTabView.xaml` put it in a bare `Grid` inside a `Border`. **Those methods are
interface members, not input handlers: nothing calls them except a `ScrollViewer`.** The whole
implementation was unreachable, `ScrollOwner` was permanently `null`, and every
`ScrollOwner?.InvalidateScrollInfo()` in the control was a null-check that never fired.

So the wheel did nothing — and, the larger half of it, **there were no scrollbars on either axis**.
A dataset taller or wider than the pane could be reached only with the arrow keys, which the grid
handles itself in `OnKeyDown`. That is why it looked fine: `Focusable` is overridden to `true`, the
caret moves, and the view scrolls to follow it. The two halves of the control were written by
different steps — 3.1 wrote the view, 3.5 wrote the control — and the contract between them was a
comment.

The scrollbars were fixed by hosting the grid as documented. The hint `Canvas` deliberately stays a
*sibling* of the `ScrollViewer`: `TryGetCellBounds` answers in the grid's own coordinates and the
grid is arranged at the `ScrollViewer`'s origin, so the two spaces still coincide, but inside it the
hint would scroll away from the cell it describes.

**That fix was reported as still not working, and the report was right — the wheel is not a scroll
gesture in the first place.** `Vcl.Grids.pas`, `TCustomGrid.DoMouseWheelDown`:

```pascal
if (Row = -1) or (Col = -1) then
  begin if TopRow < RowCount - 1 then TopRow := TopRow + 1 end
else if Row < RowCount - 1 then
  Row := Row + 1;
```

A grid that has a current cell — which, in the VCL, is every grid with rows in it — moves its
**selection** one row a notch and scrolls only as much as keeping the caret visible requires. Only
the empty-grid branch touches `TopRow`. `TControl.DoMouseWheel` accumulates the delta and calls that
once per `WHEEL_DELTA`, applying no `WheelScrollLines`: **one notch is one row, not the three a
wheel usually means.** The user's words were exact — *"you can navigate up and down the patient
list"* — and I read them as scrolling.

So the first attempt fixed the half that was invisible on the cohort the parity pass uses and left
the half that was reported. On population 1, 31 patients in a maximised window, every row fits:
there is nothing to scroll, and a correct scroll implementation is indistinguishable from a dead
one. `MatrixGrid.OnMouseWheel` now moves the caret, one row a notch, with the VCL's accumulator so
a two-notch message moves two rows and a touchpad's fractions add up instead of vanishing. Marking
the event handled is load-bearing: the `ScrollViewer` would otherwise *also* scroll three rows.

**Verified against the running application, not only in the suite.** `C:\work\qs-run\uia.ps1` drives
the port through UI Automation — the Delphi rig next door walks child `HWND`s, which does not
transfer, because a WPF window is a single `HWND`. With population 1 loaded and the caret set by a
click, one column of pixels down the `PID` column locates the current-row tint `#F3F9FD`:

| | |
|---|---|
| caret after the click | top y = 252 |
| four notches down | y = 337, **moved 85 px** |
| four notches back up | y = 252, back exactly |
| one row at this display's 125 % | 21.25 px — so 85 px is **four rows**, to the pixel |

and `scrollbar-check.ps1` asks UIA rather than the screen: **0** scrollbars maximised where 31 rows
fit, **1** (21 × 550) once the window is shrunk so they do not, **0** again on re-maximising. That is
`Auto` behaving, and it is the thing that did not exist at all before. Only colours and offsets were
read; no cell value was.

**Same lesson as (5), one level up — and then again underneath it.**
`Ui/Controls/MatrixGridScrollInfoTests.cs` covered every one of the unreachable methods and passed
throughout, including a case then named `TheWheelMovesThreeRows` which calls `grid.MouseWheelDown()`
on a bare control. *A test that calls an API itself cannot notice that nothing else does* — and,
worse, its name asserted a behaviour the product never had. It is now
`TheIScrollInfoWheelMembersMoveThreeRows`, with a comment saying what it is not.
`Ui/Controls/MatrixGridWheelTests.cs` answers the gesture and
`Ui/Dataset/DatasetGridScrollHostTests.cs` answers the host, with
`TheHostDoesNotScrollWhenTheWheelMovesTheCaret` as the seam: it would read 51 px if the handled flag
were dropped.

**(7) Two more from the same screen, and one of them killed the process.** Reported minutes after
(6) was confirmed working.

**(7a) The floating data hint did not follow the caret.** The hint kept reading `PersonId = 260`
while the caret sat on 261. The port was faithful to `05-ui-spec.md` §G.2, which said the panel *"is
**not** repositioned on hover or on keyboard navigation — only on click"*. **§G.2 was wrong.** It
read `fGrid.OnClick := UpdateDataHintPanel` as a mouse click, and a VCL `Click` is not one:
`TCustomGrid.FocusCell` raises it (`Vcl.Grids.pas:3426`), and `SetRow`, `SetCol` and `KeyDown`'s
navigation all go through `FocusCell`, guarded by `if (NewCurrent.X <> Col) or (NewCurrent.Y <> Row)`
so that only a real move counts. The wheel arrives the same way, through `Row := Row + 1`. The
Delphi author left the answer in the source being ported — `MainQuickStat.pas:311`,
`{ Moving around in grid triggers update hint view }` — and the spec quoted the line above it
without the comment. So the arrow keys were wrong too, and had been since Phase 3; the wheel is only
what made it visible. `MoveCaret` now raises `CellActivated` on an actual move, and §G.2 is struck
through and corrected in place.

**(7b) An unhandled `ArgumentOutOfRangeException` on the UI thread, and the process terminated.**
`MatrixGrid.GetDisplayCellText` indexed `matrix.Columns[...]` against counts cached in
`MatrixGridLayout`. Those counts are a *copy*: `SyncCounts` refreshes them, and `OnRender` and
`MeasureOverride` both call it first, which is why **painting was never affected and this looked like
nothing was wrong**. The accessors are not on that path.
`CollectionsTabViewModel.CollectDataAsync` opens with `matrix.ClearVariables()` and then `await`s
once per ticked element, mutating the bound matrix in place and raising no notification — so the
cached column count outlives the columns for as long as the run takes, minutes with 213 elements
ticked. `MatrixGridAutomationPeer` reaches those accessors from `UpdateSubtree` during
`ContextLayoutManager.fireAutomationEvents`, i.e. **on every layout pass, whenever a UI Automation
client is attached**. Every such pass in that window indexed an emptied list.

Two fixes: the accessors are now total against the *live* matrix, per axis, because a column header
survives losing the rows and a fixed cell survives losing the columns; and `Refresh()` invalidates
the peer, which it never did — only the dependency properties did, and a collect run moves none of
them, so a screen reader was reading the previous dataset out of recycled peers. Four cases in
`Ui/Controls/MatrixGridStaleShapeTests.cs`, all four negative-controlled by reverting the guard.

**Worth being plain about how it was found: I caused it.** Driving the port through UI Automation to
measure (6) left a client attached to the running instance, and the next collect crashed. It is a
real defect — Narrator, any assistive technology, or any automation tool reproduces it, and it is a
hard kill rather than a degradation — but nobody had met it because nothing had ever listened to the
grid's peer in a live process. The suite exercises the peer directly, which cannot reproduce the
timing.

**(8) Every list announced an object's `ToString()` to a screen reader.** Found while measuring (6)
and (7), reported rather than fixed at the time, and fixed on the user's word immediately after.
Measured on the running binary: each connection in the database combo was announced as
`QuickStatConnection { Name = Testdatabase (NDV), StudyName = NDV, ConnectionString = FILE
NAME=.\FastTrak.UDL, … }`, and every row of the data-element list as
`QuickStat.ViewModels.DataElementViewModel`, 213 times.

**The mechanism, and why the one-line estimate was wrong.** `ItemAutomationPeer.GetNameCore` asks the
row's *container* for a name and, finding none, returns `Item.ToString()`. That fallback is invisible
while items are strings — a string's `ToString` is the string, which is why the defect has never been
seen in any of the WPF a list is normally built from — and wrong the moment they are objects, which
is every list here. `DisplayMemberPath` and an `ItemTemplate` both decide what is *drawn* and neither
touches the name. So the note above, "one `AutomationProperties.Name` binding on each
`ItemContainerStyle`", named the right binding and stopped one step short: **the binding fixes the
rows that have containers, and the thing that was actually leaking is the fallback.** Both halves are
now in: `AutomationProperties.Name` on the container, stated where the row is defined, and
`ToString()` overridden on all four item types as the backstop.

**And it was four lists, not two.** The same defect sat on the population catalogue and the packages
list; only the two I happened to enumerate were reported. Populations and packages announce the id
before the title, because both lists draw it first and neither guarantees the title is unique.

**The connection string is the part that matters.** A record's generated `ToString` prints every
property, so `QuickStatConnection` put the raw `<ConnectionString>` one fallback away from a screen
reader and from every UIA client's cache. The deployed file names a UDL and leaked nothing, which is
why the note above said "not R6" — but the format permits a connection string carrying credentials
and nothing rejects one, so the class is a privacy one and the fix is written that way:
`QuickStatConnection.ToString()` now returns `Name` and nothing else, the same rule
`ResolvedConnectionString` has always followed.

Nine cases in `Ui/AutomationNameTests.cs`, and the framework's behaviour in them is measured rather
than assumed — a virtualised list puts **no** peer in the tree for a row it has not realised, and a
combo whose drop-down has never been opened exposes no item peers at all, both of which change what
the two halves are each worth. Negative-controlled twice: reverting the markup fails the four
container cases while the `ToString` cases stay green, and reverting the four `ToString` overrides
fails the fallback cases. Not reproduced against the running binary, and it does not need to be —
attaching UI Automation is how it was found in the first place, and doing it again is what caused
(7b).

**(9) `Show data hint` only took effect on the next cell.** Reported from the parity pass: ticking the
box with a cell already selected showed nothing, and the hint appeared only once the user clicked
somewhere else. Unticking it worked.

The port had half of `UpdateDataHintPanel`. In the Delphi the check box's handler **is** that
procedure — `cbShowDataHint.OnClick := UpdateDataHintPanel` (`MainQuickStat.pas:310`) — whose first
statement hides the panel and whose remainder rebuilds it from `fGrid.Col` and `fGrid.Row`, the
*current* cell. `DatasetViewModel.OnShowDataHintChanged` cleared `Hint` when the box went off and did
nothing when it came on, under a comment asserting that the Delphi behaved the same way. It does not,
and `05-ui-spec.md` §G.2 already said so in its first bullet: *"Triggered by `fGrid.OnClick` **and**
by toggling `cbShowDataHint`"*. This is the third defect in this section where the spec was right and
the implementation read past it.

The rebuild needs the caret, and the view-model cannot see it: the two indices and the cell rectangle
are `MatrixGrid`'s, and fetching them is what `DatasetTabView` already does on `CellActivated`.
Toggling the box raises no such event, because nothing moved — so the view-model raises
`HintRefreshRequested` and the view runs the same path against `Grid.CurrentRowIndex` /
`Grid.CurrentColumnIndex`, exactly as `GridRefreshRequested` already works. `Hint` is still cleared
first, so a view-model with no view attached ends up with the hint off, which is the safe half.

**Why no existing test caught it, and where the new ones live.** Every hint case drove
`DatasetViewModel.UpdateHint` directly, and the defect is not in what that computes — it is that
nobody called it. The four new cases split across the seam deliberately:
`Ui/Dataset/DatasetViewModelTests.cs` pins that the view-model asks, in both directions and after
clearing; `Ui/Dataset/DatasetTabHintTests.cs` realises the whole tab, clicks a real cell through
`PressAt`, and toggles the **check box** rather than the property behind it. Negative-controlled
twice: dropping the `Invoke` fails four cases, and keeping it while dropping the view's subscription
fails exactly the two that go through the view. 2 534 tests.

One deliberate difference is documented rather than removed: with nothing ever clicked, both indices
are `NoIndex` and ticking the box shows nothing, where the Delphi would have hinted the first data
cell — a `TCustomGrid` always has a current cell and `MatrixGrid` grows a caret on the first click or
arrow key. That belongs to the caret, not to the hint.

**(10) Both export menu items lied about whether they could run.** Reported from the parity pass:
`Save this dataset to CSV file` was black on a freshly started QuickStat, with nothing to save. It was
faithful — `actSaveDataset` has `Enabled` unset in the `.dfm` and nothing ever assigns it — and the
faithfulness is the defect. Its neighbour is no better: `actExportData.Enabled := fGrid.Data.HasData`
is assigned inside `actCollectDataExecute` and **never reset**, so `Open this dataset in Excel` stays
lit over a matrix that a new population has since emptied (§D.1 records both as-implemented).

Both now hang on one predicate, `DatasetViewModel.CanExport`: the matrix has columns **and** is
locked. One predicate rather than a patch on the reported half, because the two sit next to each
other on one menu and fail for the same reason — greying one and not the other would read as a bug in
whichever still lit up. The execute-time guard stays: `PersonMatrix` raises no change notification, so
an enabled state is only as fresh as the last `NotifyCanExecuteChanged`, and a command can be executed
without `CanExecute` being consulted at all.

**The negative control found a second, quieter thing.** `ExportingAnUnlockedMatrixIsRefusedToo`
asserted only that no file was written — and `FakeFileDialogService.Answer` defaults to `null`, so the
command returned early at the dialog whether or not the guard had fired. The case passed against
exactly the defect it existed to catch. It now supplies an answer and asserts the dialog was never
shown.

**(11) `Open this dataset in Excel` opened whatever owned `.csv`.** Reported from the parity pass,
with the observation that the Delphi gets it right. It does, and by doing more work than the port did:
`TExcelAdapter.LoadWithFile` (`FastTrak/Emetra.Adapters.Office.pas`) reads
`HKLM\Software\Classes\Excel.Application\CLSID`, then that CLSID's `LocalServer32`, and starts the
executable it names. `IProcessLauncher.OpenWithShell` is `ShellExecute`, which honours the file
association — Notepad, an editor, anything — so a menu item naming Excel opened something else.

`ExcelLocator` does the same lookup, and **cannot** be a transcription of the Delphi's. `TExcelAdapter`
splits the `LocalServer32` command line on a space and takes token 0. That survives there only because
a 32-bit process reads the `WOW6432Node` view, where Office wrote the path *quoted*. Both views were
read off the development machine, same CLSID:

```
64-bit view:  C:\Program Files\Microsoft Office\Root\Office16\EXCEL.EXE /automation
32-bit view: "C:\Program Files\Microsoft Office\Root\Office16\EXCEL.EXE" /automation
```

A space-split of the first answers `C:\Program`. The port takes the quoted span when there is one and
otherwise the first token ending in `.exe` at a word boundary; it checks both registry views, falls
back to `App Paths\excel.exe`, and requires the file to exist, because an uninstalled Office leaves
its registration behind. No Excel found is not an error — it falls back to the shell and logs why,
which is the answer on a machine that has no Excel and the log line that would have shortened this
report.

Starting Excel is not something the suite may do, so the fifteen new cases pin the parsing as data —
including one that spells out the Delphi's own rule and shows it getting the 64-bit view wrong — plus
one machine-tolerant case: whatever `Find()` answers here, it is either `null` or a real `EXCEL.EXE`.
The end-to-end path was measured once by hand instead, with a throwaway probe on a synthetic
two-column CSV: `EXCEL.EXE` started from the resolved path and the window came up titled
`quickstat-excel-probe.csv - Excel`. Probe deleted, Excel closed, file removed. 2 565 tests.

**(12) Nothing in the running application ever reset the pseudonym map.** Asked from the parity pass:
*do we have tests for the new random PID feature? Ensuring that the PIDs are unique is essential.*
Uniqueness itself held, and holds structurally rather than statistically — `Derive` skips a candidate
already in the map and both directions are filled with `Dictionary.Add`, which throws rather than
overwriting, so the worst case is a loud failure. Two things behind the question did not hold.

**`IAnonymiser.Reset` had no caller outside the tests.** `QuickStat.App` contained no reference to
`IAnonymiser` at all; `PopulationLoader.LoadAsync` did not take one. The only production entry point
was `DatasetExporter`'s `EnsureSpaceFor`, which by design leaves a space that is already wide enough
alone — that is what makes two exports of one loaded dataset identical. With a singleton anonymiser
and nobody resetting, one map served the whole session:

- **Cross-dataset linkability, R6 and therefore release-blocking.** Load population A, export; load B,
  export. A patient in both is handed the *same* pseudonym in both files, because `GetPseudonym`
  returns the memoised value — so joining two anonymised exports reveals who is in both cohorts.
  `DatasetExporter`'s own remarks and §7.2 promise the opposite. `TheSamePatientIsUnlinkableAcrossDatasets`
  passed throughout, because the *test* calls `Reset`: it was guarding a path no user could take.
- **Capacity.** The space is `9 × ScaleFactor` and widens only for the current cohort, never for the
  accumulated map, so roughly ten same-scale exports in one session reach the wall and the eleventh
  throws. It fails closed, but with a message about attempts rather than about sessions.
- **Stale width.** A 12-patient cohort exported after a 5 000-patient one inherited five-digit ids.

`PopulationLoader` now calls `Reset(matrix.Rows.Count)` between `PreparePopulation` and
`SetPopulation` — the one place §8.10 (b) put the load sequence, so the double-click and the package
replay both get it. `Rows.Count` and not the cohort length, because that is the number
`ExportDataset.FromMatrix` later hands to `EnsureSpaceFor`; disagreeing would make the exporter widen
the space and throw away the map it had just been given. An abandoned load resets nothing, because a
cancelled period dialog leaves the previous cohort in the grid and its pseudonyms have to stay with
it.

**No test looked at the pseudonym column of a file with more than one row in it.** Every pseudonymised
export case used the one-patient worked example, or compared two whole files byte for byte. The new
`Export/PseudonymUniquenessTests.cs` parses the column back out of a finished 200-row CSV and a
finished 200-row workbook, checks each id against the patient it belongs to rather than only that the
ids differ, runs the densest cohort the scale factor allows (999 in 9 000, about 11% — the old case
ran at 5.6%), and walks a small space off the end to pin that the collision loop throws rather than
repeating. The gap was real: the negative control that hoists the writer's lookup out of its loop, so
that every row carries row 0's pseudonym, failed **five** cases and **all five were new** — nothing in
the previous 2 565 noticed a file in which two hundred patients shared one id. Three controls in all:
dropping the `Reset` fails four loader cases, hoisting the lookup fails five, and removing the
collision check fails fourteen. 2 576 tests.

**(13) Packages, the last untouched area, exercised end to end — and the port is right where the
Delphi is wrong.** §8.10 (h)'s final sentence was "no package has been read or written", and it
stayed that way because packages are the one feature that cannot be tested read-only: they live in
`Report.QuickStat`. The product owner authorised writes to `EFT00028_TEST_020` on 2026-09-01, so
`C:\work\qs-packages` — a scratch console beside `qs-harness`, outside the repository — now drives
`IPackageRepository` against the live table: **29 checks, 29 passed, table restored to the row count
it found.** Save, list, replay and delete all work; `Report.AddQuickStat`'s row id comes back out of
its result set as `TPackagedSelection.Save` assumed; a Norwegian title and a multi-line comment
survive the `varchar` columns byte for byte; `DataElements` is written sorted, de-duplicated and with
blanks dropped; an empty list parses back to no names rather than one blank one; and a replay of a
saved package loads its population and collects its four elements into a 25 × 9 matrix. Deleting a
row id that no longer exists is a no-op rather than an error, which is what a stale list needs.

Three things the round trip revealed that no reading of the Pascal would have:

- **`Report.AddQuickStat` is an upsert keyed on `(StudyId, Title)`,** not an insert. It looks the
  title up, `UPDATE`s when it finds one, and returns the *first* row id. Neither application says so.
- **The Delphi shows a duplicate row for it, and the port does not.**
  `actSaveDataPackageExecute` appends the new `TPackagedSelection` to `fPackagedQuickStatGrids` and
  refreshes the list view off that in-memory list (`MainQuickStat.pas:870-875`), so saving twice under
  one title leaves two entries pointing at one server row until the next start-up. The port's
  `SaveDataPackageAsync` ends in `ReloadAsync`, so it re-reads and shows one. **A deliberate
  divergence, now pinned**: `FakePackageRepository` models the upsert instead of appending, and
  `ReusingATitleUpdatesTheRowInsteadOfListingItTwice` fails with `[100 Same tittel, 100 Same tittel]`
  the moment the reload is replaced by a `Packages.Add`. The pre-existing `SavingRefreshesTheList`
  passes under that control, which is why the suite could not see it. 2 577 tests.
- **`Title` is `varchar(80)` and neither dialog capped its text box** — the Delphi's `edtTitle` has
  no `MaxLength` (`Emetra.VclForm.EditAndMemo.dfm:931-943`) and nor had the port's `TitleBox`. A
  110-character title is stored as 80 with no error from anywhere: assigning an over-long value to a
  `VARCHAR(80)` *parameter* truncates silently, where the same assignment in an `INSERT` would raise
  8152. That was parity, and it was not harmless: two long titles sharing their first 80 characters
  collide into one row through the upsert above, so the second silently overwrites the first.
  **Closed on the owner's instruction, 2026-09-01**: `PackagedSelection.MaxTitleLength = 80` names
  the column width once, `SaveSpecViewModel` forwards it so the markup can reach it, and the name box
  binds `MaxLength="{x:Static vm:SaveSpecViewModel.MaxTitleLength}"`. A deliberate divergence — it is
  the one layer that *can* say anything, since nothing below it raises. The comment box stays
  uncapped, because `Comment` is `varchar(MAX)`. Pinned by `TheNameBoxStopsAtTheWidthOfItsColumn`,
  which types 110 characters as a real `TextInput` composition rather than assigning `Text` (WPF caps
  what is entered, not what code sets) and gets 80 in both the box and the view-model;
  negative-controlled by removing the attribute and watching it fail. 2 580 tests.

**(14) `SemiBold` is not a weight this application renders, and the packages list was the proof.**
Closing §6 of the parity checklist meant measuring what a person is asked to judge by eye, and the
one item that failed was *"title bold"*. A package was saved whose comment is a copy of its own
title, so the two runs differ in nothing but their declared weight, and both were measured off the
screen at 100 % scale:

| | ink mass | drawn width |
|---|---|---|
| comment, no `FontWeight` | 52 570 | 92 px |
| title, `FontWeight="SemiBold"` | 52 570 | 92 px |
| title, `FontWeight="Bold"` | 80 839 | 93 px |

Identical to the unit is not "close": the title was being rasterised with the regular face. Zooming
both strings 6× says the same thing by eye, and the population list next door — which has always
said `Bold` (`PopulationPickerView.xaml:233`) — has always looked bold.

**It is not the font.** The same machine, outside the application, renders Segoe UI at 12 px with
`TextFormattingMode.Display` as 81 px / 83 px / 85 px wide and 43 568 / 63 952 / 80 539 ink for
Normal / SemiBold / Bold, and `Fonts.SystemFontFamilies` lists a real SemiBold face in the family —
so weight 600 is both available and plainly distinguishable out there. Inside the window it is not,
and the shape of the `Bold` result says why it is worth someone's curiosity rather than mine: 54 %
more ink for **one** pixel of width is the signature of synthetic emboldening, not of a heavier face
being picked up. **The root cause is not pinned, and this entry does not claim it is** — what is
established is the outcome, three ways.

*Fixed* where §6 needed it: `PackagesTabView.xaml`'s title says `Bold`, pinned by
`Ui/Packages/PackagesViewMarkupTests.TheRowHasItsFourRunsAndABoldTitle`, which also pins the other
three runs and the collapsing comment that §6.3 asks about.

*Settled on the owner's instruction, 2026-09-01* — "just make bold then, I guess". Every remaining
`SemiBold` was resolved one way or the other, on one rule: **a weight declaration must mean
something.** Where §F.2 says the Delphi draws the text bold it is now `Bold`; where the Delphi draws
it plain the declaration is **removed**, because a no-op that silently becomes a divergence the day
the rendering changes is worse than no declaration at all.

| Site | Delphi | Now |
|---|---|---|
| `MatrixGrid.EmphasisFontWeight` — grid header and current row | `[fsBold]`, §F.2 | `Bold` |
| `QsTabItem`, selected caption | selected tab bold, §F.2 | `Bold`, **on the caption presenter** — see (15) |
| `lblAppName`, the wordmark | `[fsBold]`, `MainQuickStat.dfm:891` | `Bold` |
| `lblProgress` | `[fsBold]`, `MainQuickStat.dfm:949` | `Bold` |
| `TfrmSaveSpec` banner | `[fsBold]`, `EditAndMemo.dfm:887` | `Bold` |
| `TfrmPeriod` banner | `[fsBold]`, `Period.dfm:58` | `Bold` |
| Busy overlay message | no counterpart — the port invented the overlay | `Bold` |
| `QsDataGridColumnHeader` | unused inventory style mirroring the grid header | `Bold` |
| Data hint, line 1 | `lblDataHint` is one plain `TLabel`, no `Font.Style` | **removed** |
| `lblInfo` when `ProgressIsError` | §G.2 turns it red, nothing more | **removed** |

`Ui/Theme/SemiBoldTests` now sweeps every XAML file as XML and every `.cs` file for the token, so
the weight cannot come back by habit; a comment may still discuss it, which is the point.

**(15) Making the tab caption bold exposed a second defect that had been there all along, and the
product owner saw it before the tests did.** `QsTabItem` set `FontWeight` **on the `TabItem`**.
`FontWeight` is an inherited property and a tab's `Content` is its logical child, so the setter
walked straight into the page behind the tab: every section header, label and check box on the
selected tab went bold. Reported from a running build within minutes of the change, with a
side-by-side screenshot against the Delphi.

It had always been wrong. `SemiBold` simply drew nothing, so nothing showed. And the same style set
**`FontSize`** on the `TabItem` too — that one was never inert: `TabCaptionWeightTests` measures the
page at **13 px against the base 12**, so every control on the selected tab had been a point too
large since the theme was written, in the port's most-looked-at pane. Both properties now sit on the
caption presenter inside the template, `Foreground` with them.

Two lessons, both cheap to state and neither obvious from a passing suite: an inert value hides the
mistakes it is attached to, and *the first screen of the application is not covered by 2 588 tests*
unless something looks at it.

**(16) A data link file written by the Windows dialog sent the port at `master`.** The product owner
staged a second test installation against another server and the login failed with *"Du mangler
rettigheter til å utføre denne operasjonen: CREATE DATABASE permission denied in database
'master'"*. Nothing in this repository issues a `CREATE DATABASE`, and the log said why:

```
Connection <name> translated to Data Source=…;AttachDbFilename='""';…;User ID='""';…;Server SPN='""'
```

The difference from every `FastTrak.UDL` used so far is that this one was **written by the Data Link
Properties dialog** rather than by hand. The dialog emits every property the provider knows about and
spells the unset ones as two quote characters — `User ID="";Initial File Name="";Server SPN="";
Authentication="";Access Token=""` — and `Initial File Name` maps to `AttachDBFilename`. The parser
did no unquoting, so the value was the two-character string `""`;
`Microsoft.Data.SqlClient` attaches a database file for any non-empty `AttachDBFilename`; the login
therefore ran an implicit `CREATE DATABASE … FOR ATTACH`; the server answered **error 262**, which is
one of the seven numbers in the Delphi's own `TPrivilegeErrors`, so `SqlErrorClassifier` reported it —
faithfully, and very confusingly — as a missing QuickStat database role.

The Delphi never met this: it handed the whole initialisation string to the OLE DB provider, which
knows its own quoting rules. A port that re-emits keyword by keyword into
`SqlConnectionStringBuilder` has to unquote for itself. `OleDbKeywords.Unquote` now strips one
matching pair of `"` or `'` and collapses a doubled inner quote, and `MapKeyword` drops a keyword
whose value is empty afterwards instead of setting it — an empty `AttachDBFilename` is still an
`AttachDBFilename`. Thirteen tests, including the dialog's exact output as a regression case.

**(17) A quarter of every check-list row did not tick, and it took a hit test to see it.** The owner
reported, right after the filter box landed in §7.3, that ticking felt *"slightly more sluggish /
more likely to miss"*. It was not the filter: the tick path is byte-identical across that commit —
`OnElementCheckedChanged` → `PublishCheckedCollectors` → two `NotifyCanExecuteChanged` calls — and
measured at **8–16 µs for the worst single tick** with 530 elements ticked, which nothing can
perceive. But the second half of the report was exact, and pre-existing.

The row template wrapped its `CheckBox` in a `Border` carrying `Padding="4,2"` and
`Background="Transparent"`. A transparent background is *hit-testable*, so those pixels answered to
the border rather than to the box. Hit-testing every pixel down a row through the shipped markup:

```
before   ---###############---      6 of 21 px dead        (~29 % of the row)
after    -###################-      2 of 21 px dead        (the ListBoxItem's own 1 px border)
```

Two adjacent rows therefore presented a **6 px band in which a click selected and toggled nothing** —
and with the tick being the entire purpose of the list, a miss looks exactly like a slow tick. The
fix moves the vertical padding off the border and gives the box a `MinHeight` that fills the row; the
horizontal inset stays, so the collecting highlight still spans the full width.

Two things fell out of measuring rather than reasoning. The row was **21.098 px**, not 21, because
its height came from the text's natural height — so under item scrolling every row below the first
sat at a fractional offset and the dead band landed on a different device pixel on each one, which is
why the misses would have felt random rather than positional. `MinHeight` pins it at exactly 21. And
the *horizontal* answer was the opposite of the guess: the whole width already toggles, because the
default `CheckBox` template's root is a stretched transparent `Border` — one `Padding` away from not
being true, so `TheWholeWidthOfARowTogglesTheBox` now holds it. `CheckListHitTargetTests` fails on
the old markup with three dead pixels at each end and a height of 21.098.

Two things worth keeping. **Any customer UDL is a candidate**: this is simply what the dialog
produces, and the hand-written files in this repository happen to have no empty properties, which is
the only reason it took until now to appear. And the same run is the first evidence that the injected
`Encrypt=True;TrustServerCertificate=True` default (§8 (2), R1) reaches a **non-local** server and
gets all the way past login — the 262 is a post-authentication permission check.

**Left open by Phase 5**

- ~~**The Delphi half of the CSV comparison.**~~ **Done** — §8.14. Same cohort, same 213 elements,
  three identification variants: 0 differing cells in 12 462, and the two structural differences are
  named. It also produced two more defects, one in the colour blend and one in the packages list.
- ~~**`clFocusedSelectionColor` is still unmeasured.**~~ **Measured** — §8.9 (a). It needed the grid
  to be *collected*, not merely loaded, which is why the first attempt saw nothing.
- ~~**`ATC_A11EA = 'A11EA'` has no trailing `%`.**~~ **Answered on 2026-09-02, and it is not a typo.
  It needed a check, not a clinician.** Asked whether this was a branch disagreement like `J01FF%`:
  it is not. A tip sweep of `EPR/QA/EPR.QA.Collector.Drug.pas` over every ref in `C:\work\FastTrak`
  finds **119 of 120 defining `ATC_A11EA = 'A11EA'` byte-identically** (the 120th has no such file),
  and `git log --all -S "A11EA%"` finds **nothing** — the `%` form has never existed anywhere in the
  history. Against `J01FF`'s 92-to-28 split, that is unanimity.

  The convention in that constant block is not "group codes get `%`" but **"`%` iff the code has
  level-5 children"**. Checked against `dbo.KBAtcIndex`: `A10BA` has 3 children, `B01AF` 5, `B03BA`
  7, `C08DA` 4 — all four carry `%`. `A11EA` has **zero**; `A11E`'s children are `A11EA`, `A11EB`,
  `A11EC`, `A11ED`, `A11EX` and none of them has a substance code below it. So `LIKE 'A11EA'` and
  `LIKE 'A11EA%'` select the same rows, and `dbo.OngoingTreatment.ATC` is `varchar(7)`, not `char`,
  so there is no trailing-space trap defeating the exact match. The title is right too: `A11EA` is a
  group, just a terminal one.

  Residual risk, and it is the whole of it: if FEST ever assigns an `A11EA01`, the collector drops
  it silently. One character to fix, in `AtcPatterns.A11Ea`. Not raised for sign-off.
- ~~**Unverified lead, worth one look:** `Docs/Port/01-data-access.md` §7.5 says `PiiRedactor.ForLog`
  is applied in the logger provider. A subagent reported it is not.~~ **Checked, and the lead was
  right — closed in commit `6e6b974`.** Nothing applied the redactor on the way to the log file, so
  anything written through `ILogger` directly landed on disk in the clear; only `UserNotifier` and
  `IniSettingsStore` redacted. `QuickStatLogFormatter` now runs `ForLog` over the rendered message
  and `Redact` over the exception block, and `Logging/FileLoggerRedactionTests.cs` reads the actual
  bytes of the actual file rather than asserting on a formatter in isolation — which is what kept it
  closed across the swap to Serilog (`f9cce45`), where Serilog's `{{` un-doubling would otherwise
  have silently disabled the handlebar convention. R6, and therefore not a lead anybody may leave
  standing.

### 8.12 §8.10 (g) in full — what the fire-and-forget collector build actually cost

Four questions were asked before the change, because the row could have been nothing but tidiness.

**(1) Was it a race? Not the one it looks like — but two others, and both are now pinned.**

The obvious fear is a package replayed before the check list exists:
`PackagesTabViewModel.ApplyCollectorSelectionAsync` walks `CollectionsTabViewModel.DataElements` by
name, so against an empty list every stored element is reported as
`The selection contains an unknown data element.` and the replay collects nothing. Both tabs load
from the same `SessionChanged`, the packages list is one query where the collector build is two, so
the packages list really does appear first. **It is nevertheless unreachable in the shipped
composition**, and the reason is an accident: `ReloadDataElementsAsync` opened its own
`IShellProgress.BeginOperation` scope *synchronously* inside the event handler, before the
coordinator's scope closed, so the busy depth never fell to zero while the build ran — and
`OpenPackageCommand` is bound to nothing but `LeftDoubleClick` (`PackagesTabView.xaml:115`), which
the overlay eats. Three unrelated facts had to hold at once for that to be safe. Saying so plainly:
**no test was written for a bug that was not there.**

What *was* reachable:

- **A failed build was announced and then un-announced.** The build fails on its first round trip;
  the caption load is a whole query slower; `ConnectAsync` then called `_progress.Done()`. The red
  line lost the race to `Task completed` and the user was told the project had opened, with an empty
  data-element list and no dialog. `ConnectionCoordinatorTests.AFailedCollectorBuildFailsTheConnect`.
- **A stale build could win.** The handler passed `CancellationToken.None`, so nothing could call a
  build off. Switch project while one is in flight — reachable, because the busy overlay blocks the
  mouse but not the keyboard (§8.10 (f)) — and the older answer could land last, in both the registry
  and the check list, and they could disagree with each other because they were written at different
  moments. `ASecondConnectCancelsTheFirstRatherThanRacingIt`, and
  `ABuildThatFinishesAfterADisconnectIsDiscarded` for the disconnect version.

**(2) The other `SessionChanged` subscribers.** Three in total: this tab, `PackagesTabViewModel`
(reloads `Report.QuickStat`) and `PopulationPickerViewModel` (reloads the catalogue). The event fixes
no order between them, and the Delphi's order is not free — `AfterLogin` fills `cbDataCollector`
*and then* calls `LoadPackagedSelections` (`MainQuickStat.pas:481-488`), so the package list cannot
exist before the elements it names. Moving one subscriber out does not give the other two an order;
what it gives is a *boundary*: everything on `SessionChanged` happens during the connect, and the
collector list is ready when the connect returns. The two remaining subscribers are unchanged and
still unordered with respect to each other, which is fine — neither reads the other's state.

**(3) Failure semantics: the connect fails, the session stays.** Both halves are argued in
`IConnectionCoordinator`. In short: `CollectorAvailability` already absorbs the degradation the port
was designed for — a database without `KB.AntibioticResistance2` loses one collector, logged, no
error (R7) — so what escapes `BuildAsync` is a round trip that failed, and a database that cannot
answer `EXEC Report.GetFormClasses` cannot be collected from at all. Delphi agrees: a throwing
`AfterLogin` becomes `EDatabaseLoginObserverError` and takes `Connect` down. But Delphi does **not**
roll the connection back (`Docs/Port/01-data-access.md` §1.6), and neither does this: the login
pipeline did finish, the session row is open, the population list works. So
`ISessionService.IsConnected` is not a promise that the collector list exists — a successful return
from `ConnectAsync` is.

**(4) Cancellation and re-entrancy.** The build now runs on the connect's token. `ConnectAsync` keeps
the in-flight `CancellationTokenSource`, and a second connect — or `DisconnectAsync` — cancels it;
the superseded call throws `OperationCanceledException` and is barred from writing the status line,
so it cannot stomp the connect that replaced it. `SessionService.Dispose` calls
`ISessionService.DisconnectAsync` straight past the coordinator at shutdown, which the coordinator
cannot see, so the Collections tab additionally drops a `Rebuilt` that arrives with no session.

**Negative control.** The new tests were run against the old shape — the awaited build taken out of
`ConnectAsync`, the fire-and-forget put back on `SessionChanged`, everything else including the
tests left alone — and **13 of 2 464 failed**: ten of the eleven `ConnectionCoordinatorTests`, two of
the new `CollectionsTabViewModelTests`, and `ASecondLoginReplacesTheListRatherThanAppendingToIt`,
which counts builds and sees two of them. Representative failures: `Assert.False(connect.IsCompleted)`
returning true while the registry was still gated, and the status line reading
`["New project selected", "Connecting to …", "Task completed"]` with no `Loading collectors` between.
The eleventh, `DisconnectingWithNothingInFlightStillDisconnects`, passes either way and is a guard,
not a control; so is `ALoginCopiesTheStudyIdOntoTheMatrix`, which pins where the study id is written
now that the build no longer writes it.

### 8.13 Reported from the field: date of birth and sex missing from a SWEET extract

A note handed over on 2026-08-27, written 2023-08-08 and never followed up:

> Når en ikke signerer FT Sweet startskjema, blir pasientens fødselsdato og kjønn ikke tatt med i
> quickstat excel fil. Når skjema signeres, kommer de med i uttrekket.
> (per 08.08.23 er det ikke testet /feilsøkt i vår database)

**The cause is identified and it is not in QuickStat's SQL. Those two fields are not form data.**

The form is `SWEET_PATIENT` (`FormId` 1265, *SWEET – pasientdata*), reached in QuickStat as the data
element **`Skjema-data: SWEET - pasientdata (SWEET_PATIENT)`** — one of the `2 × N` per-form
collectors `Report.GetFormClasses` generates, backed by `QaSql.SnapshotFormDataAll`. It carries ten
items. Two of them are special, and they are exactly the two the report names:

| Order | `VarName` | Type | `MetaFormItem.Expression` | `ItemText` |
|---|---|---|---|---|
| 1 | `SEX` (4255) | 2, enumeration | **`Patient.GenderId`** | Kjønn |
| 2 | `DateOfBirth` (11567) | 5, date | **`Patient.Dob`** | Fødselsdato |
| 3-10 | `BDR_DIAGNOSE`, `SWEET_TYPE`, `SWEET_SUBTYPE`, … | | *(none)* | |

The other eight are ordinary user-entered items with no expression. So the failing pair is precisely
the expression pair — a match too exact to be coincidence.

`Patient.GenderId` and `Patient.Dob` are **macros over the person record**, not answers:
`MainFastTrak.pas:1873-1878` registers `Sex → Patient.GenderId` and `DOB → Patient.DOB` as macro
synonyms, and the FastTrak client evaluates them and writes the result into `dbo.ClinDataPoint` as if
it were an answer. Whether that write happens is a client decision — `TCRFItem.NeedsSaving`
(`CRF.Input.Item.pas:437-447`) — and the XML the client posts to `CRF.UpdateClinFormData` contains
only the items that predicate accepts. **QuickStat reads the copy and nothing else.** No datapoint
row, no cell. There is no signature predicate anywhere in QuickStat's SQL, so signing is not
something the port can see; what it sees is whether the row exists.

**The values are never actually unknown.** `dbo.Person.DOB` and `dbo.Person.GenderId` are both
`NOT NULL` (`FastTrak.Schema/dbo/Tables/Person.sql:3,7`), with CHECK constraints tying each to the
national id, and QuickStat already loads the person for every patient in the cohort. It is reading a
second-hand copy of a column it is holding in memory.

**Partial mitigation that already exists, and its gap.** `^ Kjønn` (`PATIENT.SEX`) reads
`dbo.Person.GenderId` directly and emits `VarName = 'SEX'` — the same name as item 4255. Ticking it
alongside the form element therefore fills the column, because a missing form datapoint cannot
overwrite what is already there (`MatrixRow.TryAddDataPoint`), and the port collapses the two into one
column where the Delphi produced two identical ones (`PersonMatrix.cs:221-238`). **There is no
equivalent for the date of birth**: the catalogue has `^ Fødselsår` and `^ Fødselsmåned` but no
full-date collector, and the fixed `Født` column is dropped in both anonymised modes (§6). Nobody can
be expected to know any of this.

**What was verified, and what was not.** Verified against `EFT00028_TEST_020`, metadata only: the
form, its ten items, the two expressions, and the `NOT NULL` guarantee. One more number makes the
point — item 11567 `DateOfBirth` appears on **exactly one form in the entire metadata set**, this one,
and item 4255 `SEX` appears on 181 forms of which 179 hide it (`Visibility = -1`); `SWEET_PATIENT` is
one of the two that show it. **Not verified: that signing is the trigger.** The test database holds
zero `SWEET_PATIENT` instances, so there was nothing to reproduce; across all forms in it, expression
items have a datapoint 57 % of the time on unsigned forms and 61 % on signed, which is no signal, and
that base was script-loaded so it cannot speak for client behaviour anyway. Settling the trigger needs
the FastTrak client, not QuickStat — and it does not change the fix, because the port should not be
sourcing person data from a form copy whenever the client happens to have written one.

**Proposed fix, not implemented — needs a decision.** When a per-form collector's item carries a
`MetaFormItem.Expression` that QuickStat can evaluate itself over the loaded `Patient`
(`Patient.DOB`, `Patient.GenderId`, `Patient.Age`, `Patient.YOB`, `Patient.PersonId`), fill the cell
from the person record. Three variants, and the choice is the protocol owner's because it changes
values in a national-registry extract:

1. **Fill only when the datapoint is missing.** Smallest change; the form copy still wins when it
   exists, so a stale copy stays stale.
2. **Always compute, ignore the copy.** Consistent, and matches what the item *means*; diverges from
   the shipped build for every patient whose person record changed after the form was signed.
3. **Do nothing to the values; surface it.** Leave the export alone and make the two rescue collectors
   discoverable instead.

The port is faithful today: it reproduces the shipped behaviour exactly. This is a pre-existing
product defect, not a port regression, and it is recorded here so that it is a decision rather than an
oversight.

**Raised and deferred on 2026-08-27.** The three variants above were put to the product owner, who
chose to leave it. Nothing was implemented and no exported value changed. Do not re-open this as a
port task: what is missing is a ruling on which value belongs in the cell, not an analysis.

### 8.14 The two sides of the CSV comparison, side by side at last

The last two things Phase 5 owed — the Delphi half of the byte comparison (R4, §10.6) and the third
palette colour (§8.9 a) — needed the same setup, so they were done in one sitting on 2026-08-27:
`22.12.21.547` driven from `C:\work\qs-delphi` against `EFT00028_TEST_020`, and the port's headless
harness at `C:\work\qs-harness` run over the same cohort.

**Both sides were given the same job**, which is the part that took the care:

| | |
|---|---|
| Population | ProcId **282** *"Diagnoseår mangler"* — 31 patients. Confirmed from `dbo.PopulationLog`, not from the screen |
| Data elements | **all 213**, checked one by one through `LB_SETCURSEL` + `WM_CHAR ' '` |
| Order | the port ran them in the shipped build's own check-list order, read out with `LB_GETTEXT`. That list is **byte-identical** to `QuickStat.Tests/Ui/Collections/DelphiCheckList.NDV.txt`, so the §8.11 (2) sort fix reproduces exactly, and the comparison below is of the *export* rather than of the sort |
| Variants | PID-only; PID-only with timestamps; fully identified. `rbKeepPids` is the form's default and maps to `pgiPersonIdOnly` = `PersonIdentification.PersonIdOnly` |

**Result: every value matches, and the two structural differences are both understood.**

| | PID only | + timestamps | fully identified |
|---|---|---|---|
| Rows | 31 + header, both | same | same |
| Columns, Delphi / port | 102 / 100 | 203 / 199 | 105 / 103 |
| Column names present in one and not the other | **none** | none | none |
| **Data cells that differ** | **0** of 3 100 | **0** of 6 169 | **0** of 3 193 |
| BOM / line ends / trailing `;` / quoting | none, 32 CRLF and 0 bare LF, yes, every field — identical | identical | identical |
| Bytes above `0x7F` | 0 / 0 | 0 / 0 | **`0xD8` ×2, `0xE6` ×1, `0xF8` ×5** — identical |
| Decimal commas | 6 / 6 | 6 / 6 | 37 / 37 |

The cells were compared **by column name**, programmatically, and only positions were ever printed;
the six files were deleted as soon as they had been measured. Both sides are also deterministic: two
independent runs of each produced the same SHA-256.

**The fully-identified pair is what discharges the CP1252 half of R4.** §8.11 recorded that the live
run had produced *"not one byte above `0x7F` in 93 kB"*, so `ø` and `æ` were pinned only by
specification-derived fixtures. With `Født` and `Fødselsnummer` in the header and Norwegian names in
the data, both files now carry the same eight high bytes in the same places — the port's CP1252
encoder and the Delphi's ANSI writer agree on real data, not on a fixture.

**Difference 1 — the two duplicate columns — is the deliberate one.** The Delphi emits
`NDV_TREATMENT_TYPE` twice and `NDV_INSULIN_DEVICE` twice, because `TPersonGridData.AddData` appends
whatever each collector reports without checking what is already there
(`EPR.QA.Matrix.Column.pas:83`); `PersonMatrix.AddColumns` de-duplicates. Nothing is lost: the second
copy carries the same value, which is why removing it leaves 0 differing cells. Recorded in
`PersonMatrix.cs:215-242` since Phase 2; this is the first time it has been seen against a real
export.

**Difference 2 — `FORM.*` column order — was not known before and is not fixable.** Once the
duplicates are removed the two headers agree at 91 of 100 positions; the 9 that differ are a
permutation of the ten `FORM.<formname>` columns, and nothing else moves. The cause is
`TPersonGridData.AddData` iterating **`for personId in fPopulation.Keys`** over a
`TObjectDictionary<integer, TPersonGridRow>` (`EPR.QA.Matrix.pas:42`, `:152`) — Delphi's hash order,
not sorted order. Column order is first-seen order, so for a collector with `FMaxBatchSize = 1` the
patient iteration order *is* the column order, and `TFormInstanceCollector` is the only batch-size-1
collector in the library. Everything else sends one statement per batch and takes its column order
from the server, which is why `FORMS12M.*`, `FORMS24M.*` and `FORMAGE.*` all agree exactly. The port
iterates by ascending `PersonId`. Matching the Delphi would mean reimplementing Delphi's
`TDictionary` hashing and growth in C#, to reproduce a permutation nobody chose; the port's order is
stable, and consumers that key on the column *name* — which is what the header is for — see no
difference.

**Difference 3 — found by the same screen, and this one was the port's fault.** The current-row tint
is `Blend(cellColour, #E7F2FC, 50)`, and `TColorCalculator.BlendColors` does
`Round( (B - A) * pct / 100 )` in floating point
(`Emetra.VclUtil.ColorCalculator.pas:229-238`). Delphi's `Round` is the FPU's — **half to even**.
`MatrixGridPalette.Blend` used C# integer division, which truncates toward zero, and its own comment
asserted that the Pascal did too. At exactly 50 % every channel lands on a `.5`, so the tie-break
rule *is* the answer:

| Base | Delphi, measured | port before | port now |
|---|---|---|---|
| `#FFFFFF` an ordinary cell | **`#F3F9FD`** | `#F3F9FE` | `#F3F9FD` |
| `#F5F5F5` a known variable with no value | **`#EEF3F9`** | `#EEF4F8` | `#EEF3F9` |

Fixed in `MatrixGridPalette.BlendChannel`; `QsCurrentRowBrush` moved from `#F3F9FE` to `#F3F9FD` with
it. `MatrixGridPaletteTests.TheCurrentRowTintRoundsHalfToEvenLikeTheDelphi` pins both measured values
and `MatrixGridCellPainterTests.AnEmptyCellInTheCurrentRowIsBlendedToo` the second; reverting either
production change fails eight tests across the theme and the painter, which was checked.

**And one more, from reading rather than measuring.** `QsPackageItem` painted its *unfocused*
selection with `QsCurrentRowBrush`, i.e. with the grid's blend result. A list never blends:
`Emetra.VclUtil.ListBoxPainter.pas:490-492` and `Emetra.VclComp.ListView.pas:271-272` both use the
raw pair, `clFocusedSelectionColor` focused and `clUnfocusedSelectionColor` not — and
`05-ui-spec.md` §B.3 has said `#E7F2FC` since it was written. `clUnfocusedSelectionColor` is
`$00FCF2E7` in **both** copies of the library, so unlike the three colours above there is no
branch ambiguity to measure away. New brush `QsUnfocusedSelectionBrush` `#E7F2FC`, in the theme, in
§F.4 and in the inventory test. The focused half was wrong too and is now right for free, because it
binds `QsCurrentCellBrush`.

**What this leaves for §10.6.** The criterion says *byte-identical for a fixture dataset*. Taken
literally it cannot be met while the port de-duplicates columns, and it cannot be met at all for a
dataset containing the form-instance collector, because the Delphi's own column order there is an
artefact of its dictionary. What can be met — and now is — is: **same rows, same column names, same
values, same encoding, same delimiters, same line ends, and the same column order everywhere except
one collector.** The two exceptions are named, attributed and reproducible.

### 8.9 Surfaced during Phase 3 wave 1 — all five are now closed

| # | Question | Status |
|---|---|---|
| a | **Three palette colours in `05-ui-spec.md` §F.1 describe `develop_old`, not the parity baseline** | **Closed.** All three measured off the running binary in Phase 5. See below |
| b | **No `<Version>` is set**, so the banner reads `1.0.0.0` | **Resolved: `26.0.0.0`**, decided by the product owner at the start of Phase 4 and set once as `<Version>` in `Directory.Build.props`. MSBuild derives `AssemblyVersion`, `FileVersion` and `InformationalVersion` from it, and the banner reads `AssemblyFileVersion` — so the single property covers the banner, the file properties and the `@AppVer` the login sends to `dbo.AddSession` |
| c | §H.2 lists two cross-tab items; there is a **third**, `ExportTimestamps` — owned by the Collections tab, read by the Dataset tab's export commands | Resolved: it lives on `IShellWorkspace`. Recorded in `07-ui-contracts.md` |
| d | §C.3 is wrong in three places — fixed-column header alignment, missing horizontal grid lines, and a two-header-row tooltip rule for a grid with `FixedRows = 1` | Resolved toward the `.pas` in each case, with evidence. Recorded in the step 3.5 report and `07-ui-contracts.md` |
| e | §F.4 says the splitter is 8 px; §A.2 and the `.dfm` say 9 | Resolved: 9 |

**(a) in full, because it is the R11 failure mode landing again.** Step 3.1 found it while transcribing
§F.4 and it was verified independently:

| Constant | §F.1 / this repo's `FastTrak\` (`develop_old`) | `origin/tarmscreening/develop` (the pinned baseline) | Measured off the running `22.12.21.547` |
|---|---|---|---|
| `clCodeColor` — population/package id column | `$00A4294B` → **`#4B29A4`** purple | `$00888888` → **`#888888`** grey | **`#888888`** — exact, stroke core of the id column |
| `clStatusTextColor` — `ProcGroup` / `Pop#n` | `$00822EB8` → **`#B82E82`** fuchsia | `clMandatoryGeometryFill` = `$00054689` → **`#894605`** brown | **`#894605`** — exact, darkest warm pixel of the category column |
| `clFocusedSelectionColor` — grid current cell | `$00D4FBFF` → **`#FFFBD4`** pale yellow | `clSelectedBk` = `$00E9D9C8` → **`#C8D9E9`** pale blue | **`#C8D9E9`** — 933 px, one cell, after a collect run |

**All three are the right-hand column, exactly.** The shipped build was
run against `EFT00028_TEST_020`, the population list was screenshotted, and the stroke cores of the
two text columns were sampled: `#888888` and `#894605`, byte for byte, no interpretation needed. So
the dated-chain argument below was correct, and the theme's values for all three were wrong.
**The first two were corrected as soon as they were measured**, in `Theme/QuickStat.Brushes.xaml`,
`ThemeResourceTests`, §F.1, §F.4, the two §F.1 mock-ups that quoted them, `04-matrix-export.md` §7.5
and `07-ui-contracts.md` — nine places in all, which is itself the argument for having a single
inventory test. The third took the same nine, plus `MatrixGrid`'s own dependency-property default.

**The third took a second sitting, and the first sitting's guess about why was right.** Clicking into
the grid had produced no change at all, and the reason is that `TStudyOverviewGrid` only installs its
`OnDrawCell` handler in `StartPainting`, which `Lock` calls at the end of a collect
(`EPR.QA.GUI.Grid.Study.pas:272-277`) — before that the grid is on VCL's default drawing and the
custom colours are not in play at all. So the measurement needs a *collected* grid, not a loaded one.

With all 213 data elements collected over ProcId 282 and one click on a data cell, the grid's own
pixels were counted before and after — rendered with `PrintWindow` into a memory bitmap, so nothing
was written to disk and no cell was ever read:

| Colour | before the click | after |
|---|---|---|
| **`#C8D9E9`** `clFocusedSelectionColor` | 0 | **933** — one 64 × 17 cell, less its text |
| `#F3F9FD` = `Blend(#FFFFFF, #E7F2FC, 50)` | 1 024 | 13 285 — the current row over ordinary cells |
| `#EEF3F9` = `Blend(#F5F5F5, #E7F2FC, 50)` | 0 | 22 610 — the current row over empty-variable cells |
| `#FFFBD4` — what the theme shipped | 0 | **0** |
| `#FFA500` `clWebOrange` — what `UpdateStyle` would set | 0 | **0** |

The last row settles a live alternative rather than a straw man. `TStudyOverviewGrid.UpdateStyle`
*does* overwrite `CurrentCellColor := clWebOrange` (`:281`), and if anything called it the answer
would have been orange and neither column of the table above. Nothing calls it: `UpdateStyle` is only
reachable from `TGuiStyle.RegisterClient`/`NotifyClients`, `MainQuickStat` never registers the grid,
and `TArenaColors.StyleForm` sets a form's `Color` and `Font` and does not walk its children
(`Emetra.VclUtil.Style.pas:486-491`). The screen agrees: not one orange pixel.

The two blends are the second finding of the sitting and have their own section — see §8.14.

Commit **`98f493bbc`** (2022-09-29, "Mindre retninger") made the change. It is on **both** tarmscreening
refs and predates the shipped `v22.12.21.547` by nearly three months, so by the same dated-chain
argument that settled R12 in §2.1, the binary customers actually run shows the **right-hand** column.
§F.1's pixel checks are against screenshots of build **19.8.14.477** from 2019, which predate the
change — so the screenshots and `develop_old` agree with each other and both describe the old
palette.

The theme shipped the **left-hand** column until Phase 5, because §F.4 was transcribed as written and
step 3.1 was right not to change a spec unilaterally. **R13 applied: settle it by looking at the
deployed exe**, not by reasoning further — this is precisely the kind of "what ships today" claim
R11 warns is unverified in `01`–`02` and `04`–`05`. *All three were looked at in Phase 5, the
dated-chain argument held for all three, and all three are now changed; the paragraphs below describe
why that took running the thing.* The third was deliberately **not** changed by analogy with the
other two while it was unmeasured — that would have been the same mistake in the opposite direction —
and it is changed now because it was measured, not because the pattern held.

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
**Therefore R13's "check the deployed exe" means run it and look.**

**Phase 5 ran it.** `QuickStat.exe` was copied to `C:\work\qs-delphi` with a `QuickStat.config.xml`
and a `FastTrak.UDL` pointing at `EFT00028_TEST_020`, and driven — connection selected, population
loaded — far enough to read the population list and the data-element check list. Two of the three
colours came off that screen exactly (table above). Reproducing it takes three things worth writing
down:

- **The UDL must not say `Provider=SQLOLEDB.1`.** The legacy MDAC/DBNETLIB stack fails against SQL
  Server 2022 here with `[DBNETLIB][ConnectionOpen (Connect()).]SQL Server does not exist or access
  denied`, while `Microsoft.Data.SqlClient` and `sqlcmd` connect to the same instance without
  complaint. `Provider=MSOLEDBSQL` works. Both `MSOLEDBSQL` and `MSOLEDBSQL19` are installed.
- **The UDL is UTF-16 LE with a byte-order mark**, three lines, CRLF. Written any other way the app
  reads an empty connection string.
- The controls are ordinary Win32, so the window can be driven and *read* programmatically:
  `EnumChildWindows` finds them by class (`TComboBox`, `TCheckListBox`, `TStudyOverviewGrid`), and
  `LB_GETCOUNT`/`LB_GETTEXT` lift the whole data-element list straight out of the check list. That is
  where `QuickStat.Tests/Ui/Collections/DelphiCheckList.NDV.txt` came from, and it is a better
  artefact than a screenshot because it is exact and carries no patient data.

---

## 9. Risk register

| # | Risk | Mitigation |
|---|---|---|
| R1 | `Encrypt=true` breaks every existing connection | Explicit defaults + a connectivity smoke test before rollout. **Closed 2026-09-01, on scope** (§8 (2)). There is no house setting to match: `FastTrak.exe` connects through the UDL, whose string carries no encryption keywords, and at least one .NET service sets `Encrypt=false`, checked directly. The port's `Encrypt=True;TrustServerCertificate=True` is therefore one app's default among several, overridable per connection — and the estate-wide posture outlives this port and has a different owner |
| R2 | Population SQL is stored **in the database**, not in this repo — arbitrary text with `:Name` parameters | **Discharged (2026-09-02).** The catalogue was swept on two independent test databases — 518 and 520 rows of `dbo.DbProcList WHERE ListId = 'CASE'`, 319 and 322 distinct statements — and both reduce to the **same 44 argument lists**. The scanner rewrote 319/319 with zero invariant violations (length preserved, every difference exactly `:`→`@`, no placeholder left behind). `QuickStat.Tests/Data/PopulationCorpusRewriteTests.cs` pins the corpus. **The risk was overstated**: `SqlText` is `RTRIM(ProcName + ' ' + ISNULL(ProcParams,' '))`, an argument list capped at 70 characters, not arbitrary T-SQL — `[]`, `""`, `--`, `/* */`, `::` and newlines occur **zero** times in either catalogue, and only `'literals'` (9 rows) exercise a skip rule. Those rules stay: a catalogue can gain a row. The sweep's real finding is §9 R2a below |
| R2a | **Eleven parameter names no session can resolve**, found by the R2 sweep | `ATC`, `ATC1`, `ATC2`, `AlertLevel`, `ApsType`, `DaysBack`, `DiaType`, `FormName`, `GroupId`, `StatusId`, `Year`. **Not a port defect** — the Delphi resolves names with `IsPublishedProp` over `TCRFSimpleContext` (`Emetra.Classes.Business.pas:79-84`), the identical vocabulary, so these fail there too. On the second database **7 are reachable and live** (`dbo.GetCaseListDrug`, `…DrugCombo`, `…DruidLevel`, `…GlobalByStatusId`, `…GroupId`, `…NewForms`, `dbo.GetFormClassInPeriod`); on the first, none are. The port at least names the failing parameter where the Delphi logged a `SilentError`. Product decision, not a port task: supply the values, hide the populations, or leave them |
| R3 | The 150-entry collector registry is transcribed by hand | **Discharged** (Phase 5). `QuickStat.Tests/Collectors/Golden/` holds one Pascal-derived statement per collector and **131 of 131 match** what the port generates. The derivations were made blind to the C#, so agreement is evidence rather than tautology; the comparison was negative-controlled. The inventory table in `03-collectors.md` remains the acceptance checklist |
| R4 | CSV byte-format drift (encoding, decimal separator, trailing separator) breaks downstream consumers | **Discharged** (Phase 5, §8.14). The same cohort and the same 213 data elements were exported from `22.12.21.547` and from the port, in three identification variants, and compared programmatically: **0 differing cells in 12 462**, identical encoding, delimiters, quoting, line ends and trailing separator. The fully-identified pair carries `0xD8` ×2, `0xE6` ×1 and `0xF8` ×5 in both files, which is the CP1252 evidence this row used to lack — the earlier run had produced *no* byte above `0x7F`. Two structural differences remain and both are attributed: the port de-duplicates two repeated column names (deliberate, `PersonMatrix.cs:215-242`), and the ten `FORM.*` columns are ordered differently because the Delphi feeds a batch-size-1 collector from a hash dictionary. `Export/CsvByteParityTests` still pins the format from the specification; it is now corroborated rather than sole |
| R5 | Custom grid control is the largest single piece of UI work | Time-boxed; `DataGrid` fallback documented with a ~150-column ceiling |
| R6 | Privacy regressions around anonymisation | Dedicated tests; treated as release-blocking |
| R7 | `KB.AntibioticResistance2` is an **inner** join in a non-`dbo` schema; a missing table fails the query outright rather than returning nothing | Register that collector only when `OBJECT_ID(...) IS NOT NULL`. **One** collector is affected — `QS_DRUG_ANTIBIOTIC_INTERMEDIATE`, the sole `JOIN KB.AntibioticResistance2` in the library (`EPR.QA.SQL.pas:453`). `QS_DRUG_ANTIBIOTIC_RECOMMENDED` lists its nine ATC codes inline (`:431`) and is **not** gated |
| R10 | Most `maxint`-batch collectors carry **no `{IdList}` at all** and scan the whole database, discarding non-cohort rows client-side | Pre-existing behaviour, preserved for parity; recorded as a separate performance follow-up, not fixed during the port |
| R8 | Period semantics are `[Start, Stop)`, end-exclusive | Getting this wrong shifts every cohort by a day; explicit tests |
| R9 | No database available to the implementation agents | All DB-touching work must be unit-testable without a server; a human runs the parity pass. **Partly lifted on 2026-08-27**: `EFT00028_TEST_020` on `localhost` was made available for Phase 5 and is the only database that may be used. Everything learned from it is in §8.11. The rule still stands for the *suite* — no test may require a server, and none does |
| R11 | **Wrong parity baseline.** The five `Docs/Port/` analyses were written against *this* repo, which is a reduced copy (§2.1). Their "what ships today" statements describe `develop_old`, a combination that cannot build the application | **Resolved for §F** (2026-08-25) — see §8.5 for the corrected verdicts and the invariance evidence. **Correction:** an earlier revision of this row claimed the cited commits were ancestors of `origin/tarmscreening/develop` "and of no other branch". That was wrong — only two refs were tested. `4c96c3c3b` is contained by 27 refs; 9 remote tips carry `QS_ROAS_BASE`, including two release branches. Only `fefc8a809` (interleukins) is genuinely narrow, at 3 remote tips. The corrected verdicts survive this because they were re-checked across **all 9** candidate refs, not one. **Still open elsewhere:** any *other* "what ships today" claim in `01`–`02`, `04`–`05` is unverified — confirm against the pinned ref before relying on it |
| R12 | **Which of the two sibling tarmscreening refs is the baseline** — they disagree on interleukins, i.e. 131 vs 130 collectors | **Resolved** (2026-08-26) in favour of `origin/tarmscreening/develop`, target **131**. The app-side and library-side interleukin commits landed the same day (2022-12-13) and the shipped exe is v22.12.21.547, matching the version-bump commit eight days later; `release/tarmscreening` forked three weeks before interleukins existed. See the table in §2.1. Residual risk is clinical, not archaeological, and is covered by §8.4 |
| R13 | **QuickStat probably has no working build.** `QuickStat.fbp8` resolves the library through `$(FastTrakDir)`. Locally that defaults to `c:\work\FastTrak`, which is on `master` and lacks every symbol — **verified**. Under Continua it binds to the `$Source.FastTrakDevelop` source, whose tracked branch **has not been observed**; if it is `develop` (as the name implies) CI cannot succeed either, but that step is inference, not fact | Regardless of how the Continua half resolves, do not rely on a Delphi build as a check — nobody has demonstrated one succeeding. Phase 5's parity pass runs against the **existing deployed exe**, not a freshly built one. **That exe is already on this machine** — four byte-identical copies of `22.12.21.547`, listed in §8.9(a) — so this row no longer blocks Phase 5. It is UPX-packed, so it must be *run*, not read. To settle R13 properly, someone with Continua access should read the `FastTrakDevelop` source definition; it is a five-minute check and it would either confirm this row or overturn it. **Neutralised in practice on 2026-08-27:** the deployed exe was copied to `C:\work\qs-delphi`, configured against `EFT00028_TEST_020` and *run* — it connects, lists populations, loads one and shows its data elements. So a reference build exists to compare against whether or not anyone can compile one, and §8.9 (a) records the two setup traps (`MSOLEDBSQL`, not `SQLOLEDB`; UTF-16 LE UDL) |
| R14 | **Reading uncommitted working trees as if they were the shipped state.** This has now caused one wrong conclusion (see §2.1) and one near-miss (`C:\work\FastTrak` sits on `master`, which lacks the tarmscreening lineage) | For every repo outside this one, read through `git show HEAD:<path>` or a pinned worktree, and run `git status --porcelain` before quoting a file as evidence. `C:\work\FastTrak.BuildServer` currently has an uncommitted `QuickStat.fbp8`; `C:\work\FastTrakApps` has a dirty `.dproj`. The library worktree at `C:\work\FastTrak-tarmscreening` exists precisely to remove this failure mode — extend the same discipline to the other two repos |

---

## 10. Acceptance criteria

1. `dotnet build QuickStat.slnx` and `dotnet test` pass with warnings as errors.
2. `QuickStat.exe` starts, reads an unmodified `QuickStat.config.xml`, and connects.
3. Every collector in the `03-collectors.md` inventory appears in the list with its exact title,
   including class-applied suffixes, and the five restored registrations are present (126 → **131**
   distinct names, from four features). 131 assumes the ref pinned in §2.1; it is 130 if R12 is
   re-decided toward `release/tarmscreening`. **Met by Phase 4** — `CollectorCatalog.All` holds 131,
   pinned by `CollectorRegistryCountTests`. The one thing still to prove is that the *titles* match a
   running build, which is Phase 5's parity pass.
4. Study gating is exact: a `KORTTID` study registers the same static collectors as `GBD` and
   `LANGTID`. This is commit `5502b72`, and it lives in *two* near-identical regex literals — the
   single easiest thing to lose in transcription. Gate matching is case-**sensitive** except
   `DOGFOOD`.

   **The target is 124, not 120.** Counted per registration procedure in both trees:

   | | always | gate **G** | gate **N** | `KORTTID` total | distinct names |
   |---|---|---|---|---|---|
   | This repo (reduced) | 36 | 76 | 8 | **120** | 126 |
   | FastTrakApps (canonical) | 37 | 79 | 8 | **124** | 131 |
   | **The .NET port**, `KB` present | 37 | 79 | 8 | **124** | 131 |
   | **The .NET port**, `KB` absent | 37 | 78 | 8 | **123** | 131 |

   The `120` quoted in earlier revisions of this plan and in `Docs/Port/03-collectors.md` §D.2 is
   *this repo's* number and therefore describes the reduced build. The +4 are the three antibiotic
   collectors (inside `AddCollectorsDrug`, which the **G** block calls) plus interleukins
   (always-on). `QS_ROAS_BASE` is `ROAS`-gated and so does not move the `KORTTID` count. If R12 is
   ever re-decided toward `release/tarmscreening`, this becomes 123 / 130.

   **The port has two `KORTTID` totals, not one**, and that is the only place it departs from the
   canonical count. `QS_DRUG_ANTIBIOTIC_INTERMEDIATE` is registered only where
   `KB.AntibioticResistance2` resolves (R7), so a customer without the knowledge-base schema sees
   **123**. The Delphi has no equivalent, because it registers the collector unconditionally and
   then fails the query. The distinct-name count is 131 either way: `CollectorCatalog.All` is the
   catalog, and availability is applied when a session's registry is built. Both outcomes are
   asserted, and the difference between them is pinned to the set of gated collectors rather than to
   a literal, so a second optional collector cannot slip in unnoticed.

   Note `5502b72` has a twin: the identical fix also sits on
   `feature/739506_GBD_utvalet_i_Korttid` in `FastTrakApps/App.QuickStat` (its current branch). The
   two agree, so the gating regexes are not in doubt — only the totals above depend on which tree
   you count.
5. "Fully identified patients" produces national IDs.

   **Restored by Phase 4 and unblocked by Phase 5.** Phase 4 wrote the recovery; §8.11 (1) explains
   why it could not have worked on any real database until the table-type default was fixed, and why
   the failure was invisible — the recovery degrades rather than throwing, so the symptom was the
   blank column the feature exists to fill. The statement it now issues recovers 342 ids for the
   first 500 patients of `EFT00028_TEST_020`. **Not yet demonstrated end to end through the running
   application**, though Phase 5 got most of the way: through the port's own services against
   `EFT00028_TEST_020`, **280 of 281** patients came back with a national id (§8.11 (3)).
6. CSV output is byte-identical to the Delphi build for a fixture dataset.

   **Met, with two named exceptions — §8.14 has the evidence.** Both sides exported the same 31-patient
   cohort with the same 213 data elements in the same order, in three identification variants:
   **0 differing cells in 12 462**, same rows, same set of column names, same encoding (including the
   eight CP1252 bytes this criterion existed to check), same delimiters, quoting, line ends and
   trailing separator. Two structural differences survive and neither is a value:

   - the Delphi repeats two column names and the port does not — a deliberate port change
     (`PersonMatrix.cs:215-242`), and the repeat carries the same value, which is why no cell differs;
   - the ten `FORM.*` columns are in a different order, because `TPersonGridData.AddData` iterates
     `fPopulation.Keys` — a `TObjectDictionary` — and that order *is* the column order for the one
     batch-size-1 collector in the library. Reproducing it would mean reimplementing Delphi's hashing
     to copy a permutation nobody chose.

   So *byte-identical* is not achievable for a dataset containing the form-instance collector, and the
   remaining prose above is what was achieved instead. If a literal byte comparison is wanted for
   sign-off, exclude that one data element and remove the two duplicate columns from the Delphi file
   first; everything else already matches byte for byte.
7. The three identification modes behave exactly as specified in §6.
8. A human parity pass against `05-ui-spec.md` finds no unexplained differences.

   **Written out as `Docs/Port/08-parity-checklist.md`**, so the pass is a walk rather than a
   re-read of 1 163 lines of specification. **49 items** still need eyes; everything already pinned
   by a test or measured off the running binary is listed as covered and skippable, with the source
   of the assurance named, and every deliberate difference is collected so it is not reported as a
   defect. Criteria 2, 3, 5, 6 and 7 close along the way and are marked where they do.
