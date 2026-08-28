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

Roughly **60 items need eyes**. Four other acceptance criteria close along the way, marked
**[AC-n]** where they do.

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
  `#0078D7` and the number in black. §A.2, §F.2.
- [ ] **1.6** Window title is `FastTrak QuickStat`, taskbar/product title `DIPS QuickStat`. §A.1.
- ⚠ **`MinWidth="900" MinHeight="600"`** — an addition; the Delphi sets no minimum. §A.1.
- ⚠ **The window is 1320 × 840** where the Delphi design size is 1290 × 785. §A.1.
- ✅ Every view and `MainWindow` constructs, and no binding resolves to nothing —
  `Ui/ViewInstantiationTests.cs` sweeps for `System.Windows.Data Error: 40`.
- ✅ The composition root wires what it says it wires — `Ui/Shell/ShellCompositionTests.cs`.

## 2. Population tab — §B.1

- [ ] **2.1** Four section headers and labels read exactly: `Select database`, `Select population`,
  `Filter / search text`, `Tip: Double click to prepare population`. §B.1.
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
- [ ] **2.9** Single click fills the SQL preview below the inner splitter, monospaced and
  read-only; the inner splitter drags. §B.1.1 items 6–7.
- [ ] **2.10** Double click loads: the right pane switches to `Dataset`, the grid fills, and the
  `Collections` tab **appears and is activated**. §B.0, §B.1.1.
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
- [ ] **4.2** `Wide columns` sits **inside** the teal bar, right-aligned, **caption to the left of
  the box**, one point smaller. Toggling it moves data columns between 64 and 120 px. §C.1.
- [ ] **4.3** Frozen columns: `PID`, `Født`, `Fødselsnummer`, `Navn` — the three Norwegian headers
  verbatim. With anything but *Fully identified patients*, columns 1–3 are **hidden**, and the
  radio switches them live. §C.3. **[AC-7]**
- [ ] **4.4** With *Fully identified patients* the `Fødselsnummer` column **has values** — that is
  the Phase 4 feature, and it has never been seen through the running shell. Expect ~280 of 281 on
  ProcId 14. **[AC-5]**
- [ ] **4.5** `PID` text, header and data, is dark teal `#035F66`; everything else black. Header
  row and the current row are **bold**. §C.3.
- [ ] **4.6** Click a cell: it should go `#C8D9E9`, and the rest of that row a 50 % tint —
  `#F3F9FD` over white, `#EEF3F9` over a `#F5F5F5` empty cell. §C.3 rules 5–6.
- [ ] **4.7** Empty-but-known cells `#F5F5F5`, no-object cells `#FFFAFA`, ordinary `#FFFFFF`.
  §C.3 rules 1–4. *(With this database ~184 of 213 elements are legitimately empty, so there will
  be a lot of grey. That is the data, not the port — `qs-run/README.md`.)*
- [ ] **4.8** Right-align: everything except `Født`, `Fødselsnummer` and `Navn`, which are
  left-aligned with ellipsis. Header row left-aligned. §C.3.
- [ ] **4.9** Tooltips on header and data cells. §C.3.
- [ ] **4.10** The data-hint panel: pale yellow, appears **below** the clicked cell aligned to its
  left edge, line 1 `PersonId = <n>` (or the patient's name when fully identified), line 2 the
  value. It moves **only on click** — not on hover, not on arrow keys. `Show data hint` is checked
  by default and hides it. §G.2.
- [ ] **4.11** Column resizing by drag works; clicking a fixed cell selects the row. §C.3.
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
- ✅ Layout, keyboard navigation, scrolling, virtualisation and the automation peer —
  `Ui/Controls/MatrixGrid*Tests.cs`.
- ✅ Caption format string and its argument order — `Ui/Dataset/DatasetViewModelTests.cs`.

## 5. Export — **[AC-6]** and **[AC-7]**

- [ ] **5.1** `Save dataset to CSV file` from the grid's context menu: default name `QuickStat.csv`,
  one file type *Comma separated values*, overwrite prompt on. §D.1.
- [ ] **5.2** All three identification modes produce what their captions promise, and
  `Export timestamp for every data element` adds a `.DATE` column after each value column. §B.2.
- [ ] **5.3** `Open this dataset in Excel` opens Excel on a temp file, and the temp file is gone
  after the app exits. §D.1, §G.6.
- ⚠ **Two structural differences from the Delphi's CSV are known, deliberate and attributed** — the
  port de-duplicates two repeated column names, and the ten `FORM.*` columns are permuted because
  the Delphi's order is `TObjectDictionary` hash order. **Read PORT-PLAN.md §8.14 before reporting
  either.**
- ✅ **0 differing cells in 12 462**, across three identification variants, same encoding,
  delimiters, quoting, line ends and trailing separator, including the eight CP1252 bytes.
  PORT-PLAN.md §8.14. Criterion 6 is met bar the two exceptions above.

## 6. Packages tab — §B.3

**This is the one functional area nothing has exercised against a real database** (PORT-PLAN.md
§8.10 h). Packages are stored *server-side* in `Report.QuickStat`, so testing save and delete
**writes to `EFT00028_TEST_020`** — everything to date has been read-only. Do it knowingly, and
delete what you create.

- [ ] **6.1** Header reads `Packaged datasets` — not `Packages`, which is only the tab caption.
  §B.3.
- [ ] **6.2** `Package dataset specification for reuse` from the grid context menu opens the save
  dialog **cleared**, headed `Save specification`. §D.1, §E.
- [ ] **6.3** Saving adds a row to the list without a restart: id grey, title bold, `Pop#<n>` brown
  and right-aligned, comment word-wrapped underneath, 1 px divider. §B.3.
- [ ] **6.4** Selected + focused is `#C8D9E9` and selected + unfocused `#E7F2FC` — the raw pair. A
  list never paints the grid's 50 % blend; getting this wrong is exactly the defect §8.14 found.
- [ ] **6.5** The filter is **trimmed** here, unlike the population filter, which is not. §B.3
  vs §B.1.1 — PORT-PLAN.md §8.8 (i) says the difference is real and deliberate.
- [ ] **6.6** **Double click replays the package in full:** selects and loads its population,
  unchecks everything, re-checks each stored collector by name, runs the collect, and sets the
  dataset caption to the package title. §B.3.
- [ ] **6.7** …and the left pane ends on the **Collections** tab, not the Packages tab. That is
  parity, restored after `07` §3.1 said otherwise (wave-2 defect 4).
- [ ] **6.8** A package naming an unknown population, and one naming an unknown collector, produce
  the two warnings in §D.4. *(Hand-editable in `Report.QuickStat` if you want to force it.)*
- [ ] **6.9** Delete asks `Do you really want to delete this package: "<title>"?` and deletes on
  Yes. §D.1.
- ⚠ **Delete is disabled when nothing is selected.** The Delphi enables it always and warns at
  execute time. Improvement, §D.1.
- ✅ Replay ordering, the by-name re-check and both warning paths —
  `Ui/Packages/PackagesTabViewModelTests.cs`, `PackageViewModelTests.cs`.
- ✅ Names inside a package are semicolon-delimited, sorted and deduplicated —
  `Domain/Packages/CollectorNameListTests.cs`.

## 7. Dialogs — §E, §D.4, §D.5

- [ ] **7.1** Save dialog: `Unique name` / `Comments`, `OK` / `Cancel`, centred on the main window,
  not resizable. §E.
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
  3 px teal bar along the **top** edge and semibold text. §F.4.
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
