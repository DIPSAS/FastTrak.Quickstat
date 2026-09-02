# 08 — Manual parity checklist

Acceptance criterion 8: *"A human parity pass against `05-ui-spec.md` finds no unexplained
differences."* This is that pass, written out so it can be walked rather than read.

It exists because `05-ui-spec.md` is a specification, not a checklist: it is 1 163 lines of prose
and tables, most of which describes things already pinned by a test or already measured off the
running binary. Re-reading all of it would spend the scarcest resource in the project — a person's
attention — on ground that is already covered. What follows separates the two.

## How to use it

Two windows, side by side:

- **The reference.** `C:\work\qs-delphi\QuickStat.exe`, the byte-identical shipped `22.12.21.547`.
- **The port.** `C:\work\qs-run\run.ps1` builds it, stages the configuration and launches it. That
  folder's `README.md` has the four traps and where the log lands.

Both against `EFT00028_TEST_020` on `localhost`, and no other database. **Pick
`Testdatabase (NDV)`** — the picker is sorted by display name and preselects nothing, so the
entry that sorts to the top is not necessarily the one you want, and everything measured during the
port is NDV. Suggested cohort: ProcId **282** *"Diagnoseår mangler"* — 31 patients, small enough to
collect all 213 elements in about a minute.

## Legend

| | Meaning |
|---|---|
| `[ ]` | **Look at it.** Nothing in the suite covers this, or what the suite covers is not the thing a person can see |
| ✅ | **Settled by machine.** Listed so you know it is covered and can skip it; the source of the assurance is named |
| ⚠ | **Deliberate difference.** Do *not* report it — it is a decision already taken, with its reason |
| ❓ | **A decision, not a comparison.** Nothing to look at; somebody has to choose |

**49 items need eyes** — it was 60 until §6, the whole Packages tab, was driven and
measured on 2026-09-01 rather than looked at. Four other acceptance criteria close along the way,
marked **[AC-n]** where they do.

**Where a ✅ says "measured", the number is in the item and the script that produced it is in
`C:\work\qs-run`.** That folder started as a launcher and is now a small instrument: `tree.ps1` and
`raw.ps1` read the automation tree (and the raw one is not optional — every `SectionHeader` in this
application is invisible to the control view), `grid-menu.ps1`, `dialog.ps1`, `pkg-*.ps1` and
`selection-colour.ps1` drive and sample, `zoom.ps1` blows a rectangle up for the questions only an
eye settles. Two traps are baked into them and worth knowing before writing another: an owned WPF
window is **not** a child of the desktop root in the automation tree, so a dialog that is plainly on
screen is missing from any search that looks there; and `Enable-QsDpiAwareness` has to be called
before anything reads a rectangle, or Windows hands this process a stretched copy of the screen and
every sampled colour is a resampled blend.

**A ✅ is worth what the test behind it is worth, and the first two defects this pass found were both
under one.** 2.10 was covered by a test that read the markup and confirmed the gesture was spelled
correctly — it was, and it did nothing. 4.12 was covered by a test that called the scrolling API
itself, which proves the arithmetic and not that anything reaches it; that test's *name* even
asserted a behaviour the product never had. Both now have a case that drives real input through the
real view. If a ✅ names a test whose subject is the *code* rather than the *behaviour*, treat it as
a `[ ]`.

**And when something still looks wrong after a fix, say so — 4.12 took two goes.** The first one
made the scrollbars work, which was a real fault, and left the reported one untouched, because the
wheel over a VCL grid moves the selection rather than the view. The reference build is the
authority; where it is cheaper to read `Vcl.Grids.pas` than to argue, that is what §2.1 means.

**A `[ ]` item can be wrong about what it is asking for.** 4.10 told you to check that the hint moves
*only* on click. It was quoting `05-ui-spec.md` §G.2, which had misread the Delphi, so following the
checklist faithfully would have confirmed the defect rather than found it. Both are corrected — but
if an item describes behaviour that seems unhelpful, that is worth a moment's suspicion of the item.

**Nothing below asks you to listen, and there is one class of difference you therefore cannot see.**
The Delphi's lists are real Win32 controls, so Windows names their rows for a screen reader without
anyone writing a line; the port's are WPF item containers, which name themselves from the item's
`ToString()` and so announced things like `QuickStat.ViewModels.DataElementViewModel` — 213 times —
while looking perfectly correct. Fixed and pinned by `Ui/AutomationNameTests.cs`; recorded here so
that "the parity pass found nothing" is not read as covering it. PORT-PLAN.md §8.11 (8).

---

## 1. Launch and connect — **[AC-2]**

- [ ] **1.1** The port starts from an *unmodified* `QuickStat.config.xml` and shows the three
  connections from it in `cbProject`, sorted, **with none preselected**. §B.1 item 2.
- [ ] **1.2** Picking one connects. Status text runs `New project selected` →
  `Connecting to <name> ...` → `Loading collectors` → `Task completed`, and the progress bar ends
  at 100 %. §G.6. *(The strings themselves are pinned — `ShellProgressTests`, `MainViewModelTests`,
  `ConnectionCoordinatorTests` — but that they appear **in that order, on screen, against a real
  server** is not.)*
- [ ] **1.3** The window is usable during the connect, or blocked by the busy overlay — and
  whichever it is, it matches what the Delphi does with its wait cursor. §G.3.
- [ ] **1.4** The `Collections` tab is **hidden** before a population is loaded. Tab strip should
  read `Population  Packages`. §B.0.
- [ ] **1.5** Banner: icon, wordmark `QuickStat`, `version 26.0.0.0` with `version` in blue
  `#0078D7` and the number in black. §A.2, §F.2. **The banner's icon is an *area* chart and the
  title bar's is a *line* chart — two different pictures.** The port showed the title bar's in both
  places until the pass caught it: `imgAppIcon` keeps its image inside `MainQuickStat.dfm` rather
  than in a file, so there was nothing in the repository to port. §A.3.
- [ ] **1.6** Window title is `FastTrak QuickStat`, taskbar/product title `DIPS QuickStat`. §A.1.
- ⚠ **`MinWidth="900" MinHeight="600"`** — an addition; the Delphi sets no minimum. §A.1.
- ⚠ **The window is 1320 × 840** where the Delphi design size is 1290 × 785. §A.1.
- ✅ Every view and `MainWindow` constructs, and no binding resolves to nothing —
  `Ui/ViewInstantiationTests.cs` sweeps for `System.Windows.Data Error: 40`.
- ✅ The composition root wires what it says it wires — `Ui/Shell/ShellCompositionTests.cs`.

## 2. Population tab — §B.1

- [ ] **2.1** Three section headers and labels read exactly: `Select database`, `Select population`,
  `Filter / search text`. §B.1.
- [ ] **2.1a** The hint is `Double-click on a population to select it`, and it sits **immediately
  above the list**, not at the foot of the tab. Hovering any row shows the tool tip `Double-click to
  select this population`; hovering the blank space under the last row shows nothing. §B.1 item 5,
  §B.1.1 items 4a and 5.
- ⚠ **All three are changes to `lblHintPopulation`, made on the owner's request.** The Delphi label
  is bottom-aligned on the tab, below the frame and the source pane, and reads `Tip: Double click to
  prepare population` — "prepare population" being `PreparePopulation`, an internal verb. The tool
  tip is new. And it is **bold**, decided on 2026-09-01 after seeing it both ways: muted grey and
  bold reads as an instruction rather than as a caption. It went bold by accident first, through the
  tab-inheritance defect in PORT-PLAN.md §8.11 (15); the accident is fixed and this is deliberate.
- [ ] **2.2** `Frequently used only` starts **disabled** and becomes enabled only once a study is
  connected. §G.6.
- [ ] **2.3** Toggling `Frequently used only` **re-queries the server** — the list content changes,
  it is not a client-side filter. §B.1.1 item 3. *(Watch `dbo.PopulationLog` or the port's log if
  you want proof rather than impression.)*
- [ ] **2.4** `Simplified` is client-side only: checked, only the selected row expands to show its
  `HelpText`; unchecked, every row is expanded. §B.1.1 item 4. **Compare it after a click, not on
  first load** — see the ⚠ below.
- [ ] **2.5** The filter matches on every keystroke, case-insensitively, over the whole
  `ProcId ⇥ Title ⇥ HelpText ⇥ ProcGroup` string, and is **not** trimmed — type a leading space and
  the result set should collapse. §B.1.1.
- [ ] **2.6** Population rows: id grey `#888888`, title bold `#333333` with ellipsis, group brown
  `#894605` right-aligned and one point smaller. Alternating rows tinted on **even** indices.
  §B.1.1.
- [ ] **2.7** Selected + focused is teal `#178891` with **all three runs white**. Click away to
  another control: selected + unfocused should be `#50AEB6`, text still white. §B.1.1.
- [ ] **2.8** `Enter` behaves like a double click. §B.1.1.
- [ ] **2.9** Tick **`Show source`** at the foot of the tab: the SQL pane opens below the inner splitter,
  monospaced and read-only, **already showing the selected population** rather than waiting for the
  next click; single click then follows the selection; the inner splitter drags; unticking closes
  it. §B.1 item 6, §B.1.1 items 6–7.
- ⚠ **`Show source` is an addition, and it replaces an access right.** The Delphi has no switch: the
  pane is visible exactly when `FUNC_POPULATION_SOURCE` is granted, which the frame registers as
  denied, and the port has no access control to ask. Added on the owner's request, off at start-up.
  §I.9.
- [ ] **2.10** Double click loads: the right pane switches to `Dataset`, the grid fills, and the
  `Collections` tab **appears and is activated**. §B.0, §B.1.1. *(This is where the pass found its
  first defect — the double click reached nothing at all, PORT-PLAN.md §8.11 (5). Fixed and pinned,
  but worth confirming with your own hands rather than on the strength of a test.)*
- ⚠ **A freshly loaded list is expanded in the port and collapsed in the Delphi**, under the same
  unticked `Simplified` box. The VCL grid initialises `FSimpleView := true`
  (`Emetra.VclComp.ListView.pas:283`) while `cbSimpleView` starts unticked, so the two disagree
  until the first click or keystroke re-synchronises them. The port follows the check box, which is
  what §B.1.1 describes. It is the first difference you will see on the first screen, and it is not
  a defect — `PopulationPickerViewModel.ApplyExpansion` says so at the code.
- ⚠ **Empty filter result shows an empty-state message** where the VCL hides the whole list.
  Deliberate improvement. §B.1.1.
- ⚠ **Populations are not sorted client-side** — stored-procedure order, ascending `ProcId`. If the
  two lists differ in order, that is the server, not the port. §G.5.
- ✅ Placeholder text, the two check-box captions and the filter header are the English runtime
  overrides, not the frame's Norwegian `.dfm` values — `Ui/Populations/PopulationViewMarkupTests.cs`.
- ✅ Filter semantics, `Simplified` expansion and selection behaviour —
  `Ui/Populations/PopulationPickerViewModelTests.cs`, `PopulationViewModelTests.cs`.
- ✅ The `#888888` / `#894605` pair is **measured off the running binary**, not transcribed: the
  screenshots and this repository's `develop_old` both carry the pre-`98f493bbc` purple/fuchsia.
  PORT-PLAN.md §8.9 (a).

## 3. Collections tab — §B.2

- [ ] **3.1** Header `Select data elements`, and the paragraph verbatim, **including the two
  spaces after `process.`**:
  `Select data elements from the list below, and click "Collect data" at the bottom to start the process.  Depending on what you select, this will take some time!`
  *(Not pinned by any test — it lives only in the XAML.)*
- [ ] **3.2** `Export options` header; the three radios read `Fully identified patients`,
  `Identified with PID only`, `Generate new random PIDs`; **`Identified with PID only` is the
  default**; the check box reads `Export timestamp for every data element`. §B.2.
- [ ] **3.3** `Collect data` is disabled until at least one element is checked, and re-disables
  when the last one is unchecked. §D.1.
- [ ] **3.4** During a run the element being collected is highlighted and the status text shows its
  title; when the run ends the scroll offset **and** the previous selection are back where they
  were. §G.4.
- [ ] **3.5** Progress advances per patient, not per collector — `100 × personIndex / count`. §G.6.
- [ ] **3.6** A second *Collect data* on the same population works (this threw once — wave-2
  defect 2).
- [ ] **3.7** Type in the filter box above the list: the list narrows on every keystroke,
  case-insensitively, on the title. `^` alone leaves the eleven demographic elements. A filter that
  matches nothing shows `No data elements match the filter.` inside the list's border. §B.2 item 2a.
- ⚠ **The filter box is an addition, asked for on 2026-09-02**, and the Delphi's `cbDataCollector`
  has nothing of the kind. It is the population tab's box — same label, same placeholder, same
  untrimmed lowercase-and-`Contains` rule — because the two sit two tabs apart in one window. **It
  hides rows and nothing else:** tick something, filter it out of sight, and *Collect data* stays
  enabled and still collects it, in the same column. Worth doing once by hand, because it is the
  one thing the addition could plausibly have broken —
  `TheFilterHidesRowsWithoutChangingWhatIsCollected` pins it. PORT-PLAN.md §7.3.
- ✅ **The whole row ticks.** A `Border` with a transparent background — which is hit-testable — used
  to own 6 of every 21 px of row height, so a click in the 6 px band between two rows selected and
  toggled nothing; the row was also 21.098 px, so that band fell on a different device pixel on every
  row down the list. Both fixed, and the row is still 21 px.
  `Ui/Collections/CheckListHitTargetTests.cs` hit-tests every pixel down a row through the shipped
  markup. PORT-PLAN.md §8.11 (17).
- ⚠ **A Cancel button appears on the busy overlay.** The Delphi has no cancel on the main form at
  all. Addition, PORT-PLAN.md §8.10 (c).
- ⚠ **The shell is greyed under the scrim while busy.** `Screen.Cursor := crSqlWait` did not do
  that; the trade is that the keyboard is locked out too. §8.10 (f).
- ⚠ **The trailing space in `Generate new random PIDs `** is dropped. §B.2 item 8.
- ✅ **The list contents and their order.** 213 elements, compared element by element with the
  running build; the comparer is `CompareStringEx`, not ICU, because the two disagree about
  punctuation and would move five columns. `Ui/Collections/CollectorOrderTests.cs` +
  `Ui/Collections/DelphiCheckList.NDV.txt`, PORT-PLAN.md §8.11 (2). **[AC-3]** — this is the "titles
  match a running build" half of criterion 3, already met.
- ✅ Check/uncheck, the enable rule and the identification enum mapping —
  `Ui/Collections/CollectionsTabViewModelTests.cs`, `DataElementViewModelTests.cs`.
- ✅ Keyboard lockout while busy, and focus restored afterwards —
  `Ui/Shell/MainWindowBusyLockoutTests.cs`.

## 4. The dataset grid — §C.1, §C.3

- [ ] **4.1** Caption bar reads `Your dataset` before a load and
  `Population: 282 "Diagnoseår mangler". Grid size: 31 x <n>` after — **rows × columns**, in that
  order. §C.1.
- [ ] **4.2** `Wide columns` sits **inside** the teal bar, **flush right**, **caption to the left of
  the box**, one point smaller. Toggling it moves data columns between 64 and 120 px. §C.1.
- [ ] **4.2a** **`Export ⌄`**, immediately left of `Wide columns`, drops down the same three items
  the grid's right-click offers, in the same order, under the button. **An addition** — the Delphi
  has no such button; those actions are reachable only by right-clicking the grid, which has to be
  guessed. The right-click still works and is unchanged; check both. §C.1 item 6, §D.2.
- [ ] **4.3** Frozen columns: `PID`, `Født`, `Fødselsnummer`, `Navn` — the three Norwegian headers
  verbatim. With anything but *Fully identified patients*, columns 1–3 are **hidden**, and the
  radio switches them live. §C.3. **[AC-7]**
- [ ] **4.4** With *Fully identified patients* the `Fødselsnummer` column **has values** — that is
  the Phase 4 feature, and it has never been seen through the running shell. Expect ~280 of 281 on
  ProcId 14. **[AC-5]**
- [ ] **4.5** `PID` text, header and data, is dark teal `#035F66`; everything else black. Header
  row and the current row are **bold**. §C.3. *(They were not: `MatrixGrid.EmphasisFontWeight`
  defaulted to `SemiBold`, as §F.3 asked, and this build draws `SemiBold` indistinguishably from
  `Normal`. **Fixed on the owner's instruction 2026-09-01** — the default is `Bold`, which is the
  Delphi's `[fsBold]`, and the header now reads bold in a fresh window. PORT-PLAN.md §8.11 (14).
  The colours are still yours to check.)*
- [ ] **4.6** Click a cell: it should go `#C8D9E9`, and the rest of that row a 50 % tint —
  `#F3F9FD` over white, `#EEF3F9` over a `#F5F5F5` empty cell. §C.3 rules 5–6.
- [ ] **4.7** Empty-but-known cells `#F5F5F5`, no-object cells `#FFFAFA`, ordinary `#FFFFFF`.
  §C.3 rules 1–4. *(With this database ~184 of 213 elements are legitimately empty, so there will
  be a lot of grey. That is the data, not the port — `qs-run/README.md`.)*
- [ ] **4.8** Right-align: everything except `Født`, `Fødselsnummer` and `Navn`, which are
  left-aligned with ellipsis. Header row left-aligned. §C.3.
- [ ] **4.9** Tooltips on header and data cells. §C.3.
- [ ] **4.10** The data-hint panel: pale yellow, appears **below** the current cell aligned to its
  left edge, line 1 `PersonId = <n>` (or the patient's name when fully identified), line 2 the
  value. It **follows the caret however the caret moves** — click, arrow keys, Page Up/Down,
  Home/End and the wheel — but not on hover. This checklist and §G.2 both said the opposite until
  the pass proved otherwise; see PORT-PLAN.md §8.11 (7). `Show data hint` is checked by default;
  **untick it with a cell selected and the panel goes, tick it again and the panel comes straight
  back on that same cell** — no second click. The port waited for one until the pass found it,
  §8.11 (9). §G.2.
- [ ] **4.11** Column resizing by drag works; clicking a fixed cell selects the row. §C.3.
- [ ] **4.12** **The mouse over the grid**, which is two different things. The **wheel moves the
  current row, one patient a notch** — it does not scroll; that is `TCustomGrid.DoMouseWheelDown`
  doing `Row := Row + 1`, and it is why the wheel visibly works in the reference even when every row
  already fits. Separately, **both scrollbars** should appear when the dataset outgrows the pane and
  go away when it does not. This is where the pass found its second defect, and the first fix for it
  was half wrong — PORT-PLAN.md §8.11 (6). Both halves are now measured against the running binary
  (`C:\work\qs-run\wheel-check.ps1`, `scrollbar-check.ps1`), so this one is closer to ✅ than to
  `[ ]`; spin the wheel anyway.
- ⚠ **No `Time series` tab.** The Delphi's is empty, permanently disabled and referenced by nothing;
  the port drops the `TabControl` entirely. §C.2 — and this is the one removal a user can see, so
  it belongs in the release notes.
- ⚠ **Grid lines are `#E2E6E6`, not `#C0C0C0`**; the grid host border `#9AA5A5`, not `#646464`;
  panel bevels `#D0D6D6`, not `#A0A0A0`. Deliberate modernisation. §F.4.
- ⚠ **Right-clicking selects the cell**, which the VCL did not do. Flagged as an improvement. §G.6.
- ✅ Cell painting priority, all seven rules, at pixel level — `Ui/Controls/MatrixGridCellPainterTests.cs`,
  `MatrixGridRenderTests.cs`, `MatrixGridPaletteTests.cs`, `Ui/DatasetGridThemeTests.cs`.
- ✅ `#C8D9E9` and the half-to-even blend are **measured off the running binary**, not transcribed —
  933 px on one click, and no `#FFFBD4` anywhere. PORT-PLAN.md §8.9 (a), §8.14.
- ✅ Layout, keyboard navigation, virtualisation and the automation peer —
  `Ui/Controls/MatrixGrid*Tests.cs`.
- ✅ Scrolling, in two halves that used to be one. What the grid *computes* —
  `Ui/Controls/MatrixGridScrollInfoTests.cs`, which called `IScrollInfo` itself and so said nothing
  about whether anything else did. What the tab actually *does* with a mouse —
  `Ui/Dataset/DatasetGridScrollHostTests.cs`, added after 4.12 turned up a defect the first file had
  covered for weeks.
- ✅ Caption format string and its argument order — `Ui/Dataset/DatasetViewModelTests.cs`.

## 5. Export — **[AC-6]** and **[AC-7]**

- [ ] **5.1** `Save dataset to CSV file` from the grid's context menu: default name `QuickStat.csv`,
  one file type *Comma separated values*, overwrite prompt on. §D.1. *(Reachable from the new
  `Export` button as well — 4.2a. Same menu, so proving one proves the other, but the button is
  where a first-time user will look.)*
- [ ] **5.2** All three identification modes produce what their captions promise, and
  `Export timestamp for every data element` adds a `.DATE` column after each value column. §B.2.
- [ ] **5.3** `Open this dataset in Excel` opens **Excel** on a temp file — not whatever owns
  `.csv` on the machine — and the temp file is gone after the app exits. §D.1, §G.6. *(A defect the
  pass found: the port used `ShellExecute`. It now resolves Excel's COM registration the way
  `TExcelAdapter` does. Worth checking on a machine whose default `.csv` handler is not Excel.)*
- [ ] **5.4** On a freshly started QuickStat **all three** menu items are greyed. They light up
  together at the end of a collect run, and go dark again the moment a new population empties the
  grid. §D.1.
- ⚠ **A deliberate divergence, on your own report.** The Delphi greys neither export: `actSaveDataset`
  is never assigned, and `actExportData` latches on and never off. Both are gated on one predicate
  here — `DatasetViewModel.CanExport` — rather than only the half that was reported, because they
  sit next to each other on one menu and fail for the same reason.
- ⚠ **Two structural differences from the Delphi's CSV are known, deliberate and attributed** — the
  port de-duplicates two repeated column names, and the ten `FORM.*` columns are permuted because
  the Delphi's order is `TObjectDictionary` hash order. **Read PORT-PLAN.md §8.14 before reporting
  either.**
- ✅ **0 differing cells in 12 462**, across three identification variants, same encoding,
  delimiters, quoting, line ends and trailing separator, including the eight CP1252 bytes.
  PORT-PLAN.md §8.14. Criterion 6 is met bar the two exceptions above.

## 6. Packages tab — §B.3

**This section is closed. Every item was driven through the running port and measured on
2026-09-01, and one of them was a defect.** Packages are stored *server-side* in
`Report.QuickStat`, so exercising them **writes to `EFT00028_TEST_020`** — which the product owner
authorised on 2026-09-01, closing PORT-PLAN.md §8.10 (h). Two rigs did it:
`C:\work\qs-packages` drives the real `IPackageRepository` against the real table (29 checks: save,
list, replay, delete, Norwegian text through the `varchar` columns, the sorted and de-duplicated
`DataElements`, a replay that collects into a 25 × 9 matrix), and `C:\work\qs-run` drives the
window itself — `grid-menu.ps1`, `dialog.ps1`, `package-row.ps1`, `selection-colour.ps1`,
`pkg-filter.ps1`, `pkg-open.ps1`, `pkg-delete.ps1`. Both restore what they found; `Report.QuickStat`
is back to the 0 rows it started with. **If you re-run any of this by hand, delete what you create.**

- ✅ **6.1** Header reads `Packaged datasets`, above the filter box; `Packages` is only the tab
  caption. §B.3. *(It is a `SectionHeader`, so like every other one it is raw-view only in the
  automation tree — `raw.ps1`, not `tree.ps1`.)*
- ✅ **6.2** `Package dataset specification for reuse` on the grid's right-click menu opens a
  modal headed `Save specification` with `Unique name` / `Comments`, `OK` disabled until a title is
  typed. **Cleared on every open**, checked the second time round, after a save had filled it.
  §D.1, §E.
- ✅ **6.3** Saving adds the row without a restart, and the row is: id `#888888`, `Pop#<n>`
  `#894605`, right-aligned two pixels off the row's edge and in the smaller size (13 px against
  17 px), comment `#333333` word-wrapped underneath (three lines, 51 px, against a 39 px row for a
  one-line comment), row `#FFFFFF`, and the bottom pixel of the row `#F0F0F0` — the 1 px divider.
  §B.3.
  ⚠ **The title was not bold, and now is.** With `FontWeight="SemiBold"` the title drew *pixel for
  pixel identically* to the comment beneath it. It says `Bold` now, like the population list next
  door. PORT-PLAN.md §8.11 (14). **There is no `SemiBold` left anywhere in the application**: the
  other ten were resolved on 2026-09-01, eight to `Bold` and two removed, and
  `Ui/Theme/SemiBoldTests` sweeps every XAML and `.cs` file so the weight cannot come back.
- ✅ **6.4** Selected + focused `#C8D9E9`, selected + unfocused `#E7F2FC`, unselected `#FFFFFF` —
  each the colour of 92 % of the row's pixels, so the raw pair and not the grid's 50 % blend.
  §8.14's defect stays fixed.
- ✅ **6.5** The filter is **trimmed** here and not on the population list, confirmed both ways in
  one sitting: `   ZZ-PARITY UI 2   ` matches its row here, `   Aktive personer   ` matches nothing
  there. §B.3 vs §B.1.1 — PORT-PLAN.md §8.8 (i).
- ✅ **6.6** **Double click replays the package in full**, and the *uncheck* half is what needed
  proving: replaying a three-element package gave `PID AGE YOB SEX`, replaying a two-element one
  straight afterwards gave `PID AGE SEX` — so the previous ticks went, rather than the two being
  added to them. The caption follows the package title each time. §B.3. *(Until PORT-PLAN.md
  §8.11 (5) this gesture was reachable by nothing at all — the same dead `MouseBinding` as the
  population list, and here with no `Enter` beside it, because the Delphi has none either.)*
- ✅ **6.7** …and the left pane ends on **Collections**, after every replay, including the one that
  warned first. Parity, restored after `07` §3.1 said otherwise (wave-2 defect 4).
- ✅ **6.8** Both warnings from §D.4, with **real line breaks**, from rows written for the purpose:
  `The selection is based on an unknown population (ProcId=999999).⏎The data collection can not be
  performed at this time.⏎Perhaps the population is from a different protocol?` — and that one
  stops, no population is loaded. `The selection contains an unknown data element.⏎Element name was
  "ZZ.NOT.A.COLLECTOR".⏎The data collection will be incomplete.⏎Perhaps the selection was created in
  a later version?` — and that one continues, collecting the elements it does know.
- ✅ **6.9** Delete asks `Do you really want to delete this package:⏎"<title>"?` with `Yes` / `No`,
  from **both** the toolbar button and the row's context menu. `No` leaves the row on the server;
  `Yes` takes it out of `Report.QuickStat` and out of the list without a restart.
- ⚠ **Delete is disabled when nothing is selected**, confirmed with four rows on screen and the
  selection cleared. The Delphi enables it always and warns at execute time. Improvement, §D.1.
- ⚠ **Saving under a title that already exists updates one row here and shows two in the Delphi.**
  `Report.AddQuickStat` is an upsert keyed on `(StudyId, Title)`; the Delphi appends its new object
  to an in-memory list and never re-reads, so it lists one server row twice until restart. The port
  reloads. Deliberate — PORT-PLAN.md §8.11 (13).
- ✅ **A title longer than 80 characters can no longer be typed.** `Report.QuickStat.Title` is
  `varchar(80)`, and past it the upsert above does not merely lose the tail — it overwrites the
  package sharing those 80, with nothing raising anywhere. The Delphi truncates silently; the name
  box now stops at `PackagedSelection.MaxTitleLength`. A divergence, taken on the owner's
  instruction 2026-09-01 — §8.11 (13), `TheNameBoxStopsAtTheWidthOfItsColumn`.
- ✅ Replay ordering, the by-name re-check and both warning paths —
  `Ui/Packages/PackagesTabViewModelTests.cs`, `PackageViewModelTests.cs`.
- ✅ Saving under an existing title leaves one list row, not two —
  `ReusingATitleUpdatesTheRowInsteadOfListingItTwice`, negative-controlled against the Delphi's
  append.
- ✅ Names inside a package are semicolon-delimited, sorted and deduplicated —
  `Domain/Packages/CollectorNameListTests.cs`, and confirmed against the live column.

## 7. Dialogs — §E, §D.4, §D.5

- [ ] **7.1** Save dialog: `Unique name` / `Comments`, `OK` / `Cancel`, centred on the main window,
  not resizable. §E. *(The four labels, the two buttons and the disabled `OK` are confirmed —
  §6.2. Two things a rig noticed. **The first-open offset is fixed**: the dialog used to come up
  27 px left and 90 px above the owner's centre the first time in a process, and every dialog now
  redoes the centring from `OnSourceInitialized` once its size is known. Verified in a fresh
  process on 2026-09-01 — first open, dialog centre 1437,870 against the owner's 1437,870.
  `Ui/Dialogs/DialogCentringTests`.)*
- ⚠ **The buttons read `OK` then `Cancel`, and the Delphi's read `Cancel` then `OK`.** A deliberate
  divergence, taken on the owner's instruction 2026-09-01: `btnSave.Left` 280 against
  `btnClose.Left` 184 puts OK on the right in both Emetra dialogs, which is the opposite of the
  Windows convention *and* of `NotificationDialog` next door, whose `Yes` has always come first —
  so the application disagreed with itself. Sizes and spacing are unchanged. §7.3;
  `TheButtonBarPutsOkFirst` in `SaveSpecDialogTests` and `PeriodDialogTests`.
- [ ] **7.2** The period picker appears **by itself** when a query declares `@StartDate` and
  `@StopDate` — it is not on any menu. Norwegian throughout, `OK` disabled while start ≥ end,
  and the dates are remembered per query. §D.5.
- [ ] **7.3** Message boxes show **real line breaks**, not literal `\n`. §D.4, §I.8.
- [ ] **7.4** `Det er ikke valgt en gyldig populasjon.` — the one Norwegian string in otherwise
  English chrome. Check what the port says and whether that is what you want. §D.4.
- ✅ Dialog layout, button roles, modality and the clearing rule — `Ui/Dialogs/SaveSpecDialogTests.cs`,
  `PeriodDialogTests.cs`, `PeriodViewModelTests.cs`, `NotificationDialogTests.cs`.

## 8. Chrome, theme and shutdown

- [ ] **8.1** Section headers: teal `#178891`, white text, 26 px. Tab strip: selected tab has a
  3 px teal bar along the **top** edge and **bold** text. §F.4. *(Both confirmed in a fresh window
  on 2026-09-01. The caption was `SemiBold`, which this build does not render; making it `Bold`
  then turned the **whole page** bold, because the setter was on the `TabItem` and `FontWeight`
  inherits into a tab's content. Both are fixed, and the same style's `FontSize` had been leaking
  13 px into every control on the selected tab since the theme was written — PORT-PLAN.md
  §8.11 (15). Worth a look at the page as well as the strip.)*
- [ ] **8.2** Move the window, maximise it, close, reopen — geometry comes back. Then change screen
  resolution: the ini is keyed **per resolution**, so a different geometry is expected, not a bug.
  §G.1.
- [ ] **8.3** Restore a window onto a monitor that is no longer there (unplug, or hand-edit the
  ini): it should fall back to the work area rather than open off-screen. §G.1.
- [ ] **8.4** Close the app and confirm `dbo.UserLog` has `ClosedAt` set and `DirtyClose = 0` for
  your session. This was a real defect and the fix is only observable in a real process exit —
  PORT-PLAN.md §8.11 (4).
- [ ] **8.5** Nothing personal in `LOGS\quickstat-*.log` after a full session with *Fully
  identified patients* — no national id, no patient name. R6 is release-blocking.
- ⚠ **Segoe UI throughout**, not Calibri/Tahoma/Consolas. §F.3.
- ⚠ **Tab strip is 28 px** against the Delphi's 19. §A.3.
- ⚠ **The splitter position and last-used database are persisted**; the Delphi persists neither.
  §G.1 — both were flagged as additions rather than slipped in.
- ✅ Every brush key, its colour and that it is frozen; every named style —
  `Ui/Theme/ThemeResourceTests.cs`, in both directions, so a brush that exists only in the theme
  fails too.
- ✅ The caption-left check boxes draw an unmirrored tick — `Ui/Theme/CaptionLeftCheckBoxTests.cs`,
  from rendered pixels, because this was a real defect found in a running build.
- ✅ Window geometry persistence and the off-screen guard — `Ui/Services/WindowStateServiceTests.cs`.
- ✅ Redaction on the way to the log file — `Logging/FileLoggerRedactionTests.cs`, which reads the
  actual bytes of the actual file. PORT-PLAN.md §8.11.

---

## 9. Decisions, not comparisons

Nothing to look at. Each needs somebody to choose, and four of them are §I of the specification
still waiting for an answer.

- ❓ **`J01FF%` in the resistance-driving antibiotic set.** Release-blocking, and clinical rather
  than technical. PORT-PLAN.md §8.4.
- ❓ **`ATC_A11EA = 'A11EA'` with no trailing `%`.** Same owner, same shape, cheap to ask at the
  same time. PORT-PLAN.md §8.11.
- ❓ **SWEET: date of birth and sex** — three candidate policies, and the choice changes values in a
  national-registry extract. Parked by the product owner on 2026-08-27. PORT-PLAN.md §8.13.
- ❓ **Left-pane width, 293 or ~330** (§I.1). The port ships 293, faithful to the `.dfm`.
- ❓ **`actSavePatientSelection` is unreachable** (§I.2) — drop it, or give it a home in the grid
  context menu.
- ❓ **The save dialog is not cleared for *Save selection*** (§I.3). Looks like a bug; the port
  reproduces it.
- ❓ **Icons** (§I.10) — extracted bitmaps or Segoe MDL2 glyphs. A designer's call.

## 10. If you would rather not do part of this by hand

Several `[ ]` items above are only unautomated because nobody has written the assertion yet, not
because they need judgement — **3.1**, **3.2**, **4.1** and **8.1** are literal strings and
literal brushes in XAML, and `Ui/ViewInstantiationTests.cs` already realises every view on an STA
thread, so asserting on their visual tree is a small addition rather than a new harness. Say so
and they can be moved from `[ ]` to ✅ before you start walking.

What genuinely cannot be moved: anything whose reference is *the other application's behaviour*
against a live server — the workflow items in §1, §2.3, §3.4, §6, and every colour that has to be
compared to what the Delphi actually paints rather than to what a document says it paints.
