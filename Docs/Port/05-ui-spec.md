# 05 — UI specification (QuickStat, Delphi VCL → WPF .NET 10)

Target: WPF `net10.0-windows`, C#, `.slnx`, flat repo layout, MVVM via `CommunityToolkit.Mvvm`,
no commercial control suites.

Design brief (verbatim from the product owner):
> *"keep the UI similar so that existing users do not have to re-learn it (but there is also no
> point in re-creating a 'Delphi-feel' or style or similar; make it look good and similar)"*

So: **same information architecture, same tab names, same control order, same workflow** —
flat/modern rendering, Segoe UI, no bevels, no gradients, 4/8 px spacing rhythm.
This document is written so the XAML can be produced without opening a single `.dfm`.

---

## 0. Sources, method, and confidence

| Source | Used for |
|---|---|
| `MainQuickStat.dfm` | Every control, its parent, alignment, size, and caption |
| `MainQuickStat.pas` | Runtime restyling (`ApplyColors`), enable/disable rules, event wiring, resource strings |
| `FastTrak\EPR.VclFrame.Populations.dfm` / `.pas` | The embedded population picker frame |
| `FastTrak\Emetra.VclForm.EditAndMemo.dfm` / `.pas` | `TfrmSaveSpec` modal |
| `FastTrak\Emetra.VclForm.Period.dfm` / `.pas` | Secondary "Angi periode" modal |
| `FastTrak\Emetra.VclUtil.ArenaColors.pas` | The teal/pale palette applied at runtime |
| `FastTrak\Emetra.VclUtil.ColorSet.Interfaces.pas` | List text colours (code purple, category fuchsia) |
| `FastTrak\EPR.QA.GUI.Grid[.Study].pas` | Grid cell painting, colours, column widths |
| `FastTrak\Emetra.VclComp.ListView.pas` | Population list custom drawing |
| `FastTrak\Emetra.VclUtil.Listbox.pas` + `Emetra.VclUtil.Spotlight.pas` | Packages list drawing + filtering |
| `FastTrak\Emetra.VclUtil.Settings.pas` | Window state persistence |
| `Docs\Screenshots\QuickStat bilde 1–4.png` | Pixel-sampled palette verification; menu strings |

**Important source-vs-screenshot mismatch.** The screenshots are of build **19.8.14.477**.
In that build the *Export options* group had two check boxes:
`Export PID only as identification` and `Export timestamp for every data element`.
The **current** `MainQuickStat.dfm` has replaced the first check box with **three radio buttons**
(`rbFullIdentification`, `rbKeepPids`, `rbRandomisePids`).
**The port must follow the current `.dfm`, not the screenshot.** Everything else in the
screenshots matches the current source.

All pixel values below are logical pixels at 96 dpi (= WPF device-independent units).
Colours quoted as `#RRGGBB` were cross-checked by sampling the PNGs; Delphi `$00BBGGRR`
literals are given where the constant matters.

---

## A. Window and layout

### A.1 Main window

| Property | Delphi value | WPF recommendation |
|---|---|---|
| `Title` | `FastTrak QuickStat` | `FastTrak QuickStat` (unchanged) |
| App title (taskbar) | `DIPS QuickStat` (`QuickStat.dpr`) | keep as assembly/product title |
| Client size (design) | 1290 × 785 | `Width="1320" Height="840"` (frame included) |
| Minimum size | *(none set)* | `MinWidth="900" MinHeight="600"` — **new, flag as an addition** |
| Startup position | `poDefault` + restored from ini | `WindowStartupLocation="Manual"`, restore from settings (§G.1) |
| Icon | `QuickStat_Icon.ico` (grid + green line chart + blue area) | reuse the `.ico` |
| Window state | restored/saved (`Normal`/`Maximized`/`Minimized`) | restore/save |
| Background | `#EEEEEE` (`clFormFace`) | `QsFormFaceBrush` |
| Base font | Calibri 10 pt (set at runtime by `TArenaColors.StyleForm`) | Segoe UI 12 px |

### A.2 Nesting

```
Window  (FastTrak QuickStat)
└─ DockPanel
   ├─ Border  panWhiteTop            Dock=Top, Height 55, Background #FFFFFF,
   │  │                              1 px bottom border #A0A0A0
   │  ├─ Image      imgAppIcon        Left, 32×32, Margin 15,3,0,3
   │  ├─ TextBlock  lblAppName        Left, "QuickStat", 20 px SemiBold
   │  ├─ VersionRun RzVersionInfoStatus1  Left, "version " + FileVersion
   │  └─ StackPanel panProgress       Right, Width 295
   │     ├─ TextBlock lblProgress     "Progress"  (bold)
   │     ├─ TextBlock lblInfo         "Program is idle" → status text
   │     └─ ProgressBar pbProgress    0..100, smooth
   └─ Grid  splMain                   Dock=Fill  (was TRzSplitter, SplitterWidth 9)
      ├─ ColumnDefinition  Width=293  (min 260)      → pgSelections   (left)
      ├─ GridSplitter      Width=9,  Background #F4FBFB
      └─ ColumnDefinition  Width=*                    → pgDataset      (right)
```

`pgSelections` is a `TabControl` with three tabs; `pgDataset` is a `TabControl` with two.

### A.3 Widths / heights / margins table

| Element | Delphi | Notes / WPF |
|---|---|---|
| `panWhiteTop` height | 55 (incl. 1 px bottom bevel) | `Height="55"`, `BorderThickness="0,0,0,1"` |
| `imgAppIcon` | 32×32, `Margins.Left = 15`, top/bottom 3 | `Margin="15,3,0,3"` |
| `lblAppName` | Tahoma 20 px Bold, vertically centred, `Margin 3` | Segoe UI 20 px SemiBold |
| `RzVersionInfoStatus1` | Left 154, Width 130, Height 53, Tahoma 13 px | inline run pair |
| `panProgress` | right-aligned, Width 295, Height 53 | `Width="295"` |
| `lblProgress` | top, bold, Height 13 | Segoe UI 12 px SemiBold |
| `lblInfo` | below, Height 13, `Margins.Top = 1`, `Margins.Right = 8` | Segoe UI 12 px |
| `pbProgress` | fills the rest, `Margins.Top = 1`, Max = 100, smooth | `Height="10"`, `Maximum="100"` |
| `splMain.Position` | **293** (`Percent = 23`, `SplitterWidth = 9`) | left column 293 (screenshots show ≈336 after the user dragged it; the brief says ≈330 — see §I.1) |
| Left pane (`pgSelections`) | Width 293 | `MinWidth="260"` |
| Right pane (`pgDataset`) | Width 988 at design size | `Width="*"` |
| Tab strip height | `FixedDimension = 19` | 28 px (modern touch-friendly) |
| Tab sheet padding | `Padding = 4` on all four sides | `Padding="4"` |
| Control margins inside a tab sheet | 3 px (`AlignWithMargins`) | `Margin="3"` → so content starts 7 px from the tab edge and is 279 px wide in a 293 px pane |
| Section header bar | Height 23–24 incl. 1 px border | `Height="26"`, `Padding="8,4"` |

---

## B. Left panel — `pgSelections`

`TabControl`, `SelectedIndex = 0` at startup. Three tabs, **in this order**:

| Index | Delphi name | Caption | Visible? |
|---|---|---|---|
| 0 | `tbsPopulation` | `Population` | always |
| 1 | `tbsDataElements` | `Collections` | **hidden + disabled until a population is loaded** |
| 2 | `tbsPackages` | `Packages` | always |

### B.0 The dynamic `Collections` tab (do not miss this)

```pascal
// FormCreate
tbsDataElements.TabVisible := false;
tbsDataElements.Enabled    := false;

// LoadPopulationIntoGrid
tbsDataElements.TabVisible := fGrid.Data.DataRows > 0;
tbsDataElements.Enabled    := tbsDataElements.TabVisible;

// AfterPopulationSelect (after the grid has been filled)
pgSelections.ActivePage := tbsDataElements;   // auto-switch to it
```

WPF: bind `TabItem.Visibility` to `HasPopulation` (`BooleanToVisibilityConverter`, `Collapsed`
when false) and `IsEnabled` to the same flag; after a successful population load, set
`SelectedIndex = 1` from the view-model. Screenshot 1 shows the tab strip reading
`Population  Packages`; screenshots 2–4 show `Population  Collections  Packages`.

### B.1 Tab **Population** (`tbsPopulation`)

Top-to-bottom (all stretched to the tab width, 279 px at design size):

| # | Control | Type | Exact caption / text | Behaviour |
|---|---|---|---|---|
| 1 | `panHdrDatabase` + `hdrDatabase` | teal section header | `Select database` | static |
| 2 | `cbProject` | `ComboBox`, `csDropDownList`, `DropDownCount = 24` | *(items from `QuickStat.config.xml` → `<Connection><Name>`, e.g. `Testdatabase (NDV)`)* | `Sorted := true` at `FormShow`; `OnChange = SelectConnection` → disconnect, set info to `New project selected`, then `Connecting to %s ...`, connect, `Done` |
| 3 | `panHdrPopulation` + `hdrPopulation` | teal section header | `Select population` | static |
| 4 | `panPopulation` | container (fills remaining height) | — | hosts the population frame (`alClient`) |
| 5 | `lblHintPopulation` | label, bottom | `Tip: Double click to prepare population` | static |

**No item is preselected in `cbProject`** — the user must pick one, which triggers the connection.

#### B.1.1 The embedded population frame (`TfrmPopulations`)

The frame's own `.dfm` captions are Norwegian, but `TfrmQuickStat.FormCreate` **overwrites all
four of them with English** at runtime. Use the English strings:

```pascal
frmPopulations.edtPopFilter.TextHint      := 'Type filter text here';
frmPopulations.lblFilterHeader.Caption    := 'Filter / search text';
frmPopulations.cbShowCommon.Caption       := 'Frequently used only';
frmPopulations.cbSimpleView.Caption       := 'Simplified';
```

Layout, top to bottom:

| # | Control | Type | Caption / placeholder | Behaviour |
|---|---|---|---|---|
| 1 | `lblFilterHeader` | label | `Filter / search text` | static |
| 2 | `edtPopFilter` | `TextBox`, Height 21 | placeholder `Type filter text here` | **live filter on every keystroke**; case-insensitive substring over the whole item text (see below) |
| 3 | `cbShowCommon` | `CheckBox`, left-aligned in a 28 px strip | `Frequently used only` | **disabled until a study/database is connected**; re-queries the server (different stored procedure), it is not a client-side filter |
| 4 | `cbSimpleView` | `CheckBox`, **right-aligned, caption to the LEFT of the box** (`Alignment = taLeftJustify`) | `Simplified` | client-side only: when checked, only the selected row expands to show its `HelpText`; when unchecked every row is expanded |
| 5 | `ListView` (`TObjectListView`) | 3-column virtual list, fills 77 % of the remaining height | — | see below |
| 6 | *(splitter)* | horizontal splitter, 9 px | — | resizes list vs. SQL preview |
| 7 | `memSourceCode` | read-only multi-line text, no word wrap, **Consolas 8 pt** | the population's `ProcSourceCode` | filled on **single** click; only visible if the user holds the `FUNC_POPULATION_SOURCE` right |

**Population list row layout** (reproduce with a `ListBox` + `DataTemplate`):

```
┌──────┬──────────────────────────────────────────────┬────────────────┐
│ 257  │ HbA1c > 53 (7%)                              │ Type 1 u/pumpe │
│ #4B29A4 (regular)  │ #333333 Bold, ellipsis on overflow │ #B82E82, 1 pt smaller, right │
└──────┴──────────────────────────────────────────────┴────────────────┘
  (when expanded, the wrapped HelpText follows underneath in #333333)
```

* Column 0 = `ProcId`, min width 32 px (`24 + 2×GapX`), grows to fit the widest id.
* Column 1 = `ProcTitle`, bold, single line, `TextTrimming="CharacterEllipsis"`.
* Right-hand text = `ProcGroup`, **drawn inside column 1, right-aligned**, one point smaller.
  The values (`Type 1`, `Type 2`, `Type 1 u/pumpe`, `Type 1 m/pumpe`, `Prosess`, `Behandling`,
  `Studier`, `Komplikasjoner`, `Inkretinbrukere`, …) are **database content, not literals** —
  they are Norwegian and must stay Norwegian.
* Row padding: 4 px horizontal, 6 px vertical → collapsed row height ≈ 29 px.
* Alternating background: index **0, 2, 4 …** = `#F7F7F7`, odd = `#FFFFFF`
  (note: inverted vs. the usual WPF convention — `AlternationIndex 0` is the tinted one).
* Selected + focused: background `#178891`, **all three text runs turn `#FFFFFF`**.
* Selected + unfocused: background `#50AEB6`, text still white.
* 1 px horizontal separator between rows.
* `Enter` behaves like a double click.
* Right-click focuses and selects the row first (VCL workaround) — WPF does this natively.

**Filtering semantics** (`TObjectListView.AfterUpdate`): lowercase the filter, lowercase the
item's full tab-joined string `ProcId ⇥ Title ⇥ HelpText ⇥ ProcGroup`, and do a plain
`Contains`. Empty filter = everything. The filter is **not** trimmed. The `Simplified`
checkbox does not change what is matched. In the VCL the whole grid hides itself when the
result set is empty — in WPF, prefer an empty-state message (flag as a deliberate improvement).

**Double click = prepare population** (`PopulationRequested` → `AfterPopulationSelect`):
switch `pgDataset` to `Dataset`, clear the grid, load the patient list, fill the grid, show
and activate the `Collections` tab. A `crSqlWait` wait cursor is shown for the whole operation.

### B.2 Tab **Collections** (`tbsDataElements`)

Top-to-bottom:

| # | Control | Type | Exact caption | Behaviour |
|---|---|---|---|---|
| 1 | `panHdrElements` + `hdrElements` | teal section header | `Select data elements` | static |
| 2 | `lblDataElementInfo` | wrapped paragraph, `Margins 6,·,6,6` | see verbatim below | static |
| 3 | `cbDataCollector` | `CheckedListBox` (fills remaining height) | *(Norwegian data-element titles)* | `Sorted := true` at `FormShow`; `OnClickCheck = ValidateCollectorSelection` |
| 4 | `btnCollectData` | wide flat button, Height 43, docked bottom, icon + caption centred | `Collect data` (from `actCollectData`) | disabled until ≥ 1 element is checked |
| 5 | `panHdrExportOptions` + `hdrExportOptions` | teal section header | `Export options` | static |
| 6 | `rbFullIdentification` | radio, Height 17 | `Fully identified patients` | `OnClick = ToggleGridAnonymity` |
| 7 | `rbKeepPids` | radio, Height 17, **`Checked = True` (default)** | `Identified with PID only` | `OnClick = ToggleGridAnonymity` |
| 8 | `rbRandomisePids` | radio, Height 17 | `Generate new random PIDs ` — **note the trailing space in the `.dfm`**; drop it in the port | `OnClick = ToggleGridAnonymity` |
| 9 | `cbExportDates` | check box, Height 18, `Margins.Top = 8`, `Margins.Bottom = 8` | `Export timestamp for every data element` | read at export time only |

Verbatim paragraph text (single string, note the **two spaces** after `process.`):

```
Select data elements from the list below, and click "Collect data" at the bottom to start the process.  Depending on what you select, this will take some time!
```

**`cbDataCollector` item text** is a Norwegian data-element title supplied by the server /
collector factory, e.g.:

```
^ Alder      ^ Dødsår      ^ Fødselmåned      ^ Fødselsår
^ Gruppe / avdeling nå     ^ Gruppe / avdeling ved død
^ Institusjon / sted       ^ Institusjon / sted ved død
^ Kjønn      ^ Postnummer  ^ Statuskode
Antropometri: Høyde og vekt (siste)
Diabetes: Behandling (siste)        Diabetes: Hypoglykemi (siste)
Diabetes: Insulindosering (siste)   Diabetes: Komplikasjoner (siste)
Diabetes: Mosjon (siste)            Diabetes: Sosialt (siste)
Labdata: Alle med høy konfidens     Labdata: Antall prøver siste 12 mnd
Labdata: Antall prøver siste 24 mnd (2 år)   …
NDV: Basisdata (siste)              …
```

The leading `^ ` on the eleven demographic collectors is a **sort hack** (`^` sorts before
letters) that pins them to the top of the alphabetically sorted list. Keep the titles verbatim,
including the `^ ` prefix, so users recognise them.

`ToggleGridAnonymity` sets `fGrid.Anonymous := not rbFullIdentification.Checked`, which in the
grid **hides the Born / National-ID / Name columns** (`ColWidth = -1`) and leaves only `PID`.
Default state (`rbKeepPids`) is therefore *anonymous* — which is what all screenshots show.

`PersonGridIdentification` maps the radio group to an export enum:

| Radio | Enum |
|---|---|
| `rbFullIdentification` | `pgiFull` |
| `rbKeepPids` | `pgiPersonIdOnly` |
| `rbRandomisePids` | `pgiRandomPersonId` |
| *(none)* | raises `EAbort('Unhandled identification strategy.')` |

### B.3 Tab **Packages** (`tbsPackages`)

Top-to-bottom (everything inside one container panel, 4 px margins):

| # | Control | Type | Exact caption | Behaviour |
|---|---|---|---|---|
| 1 | `panHdrPackages` + `hdrPackages` | teal section header | `Packaged datasets` (**not** "Packages" — that is only the tab caption) | static |
| 2 | `edtPackageFilter` | `TextBox`, Height 21 | *(no placeholder set)* | live filter on every keystroke; **trimmed**, case-insensitive substring |
| 3 | `tbrPackages` | small toolbar, Height 30, buttons 31 × 30, 24 × 24 icons | — | one button only |
| 3a | `ToolButton1` | tool button | icon-only, action `actDeletePackage` (`Delete this package`) | see §D |
| 4 | `lbPackagedGrids` | owner-drawn list, fills remaining height, `PopupMenu = mnuPackagePopup` | *(package rows)* | `OnDblClick = PreparePackagedSelection` |

`mnuPackagePopup` — one item: **`Delete this package`**.

**Package row layout** (each row is a tab-joined `RowId ⇥ Title ⇥ Comment ⇥ Pop#<n>`):

```
┌──────┬──────────────────────────────────────────────┬──────────┐
│ 41   │ Diabetes basissett 2024                      │ Pop#257  │
│ #4B29A4 │ #333333 Bold, ellipsis                    │ #B82E82, −1 pt, right │
├──────┴──────────────────────────────────────────────┴──────────┤
│ Comment text, word-wrapped, #333333                            │
└────────────────────────────────────────────────────────────────┘   1 px #F0F0F0 divider
```

Selected + focused: `#FFFBD4`; selected + unfocused: `#E7F2FC`; otherwise white. Variable row
height. Horizontal padding 2 px, no vertical padding.

**Double click** (`PreparePackagedSelection`) does a full replay:
find the package → `frmPopulations.TrySelect(PopulationId, load=true)` → load into the grid →
uncheck everything in `cbDataCollector` → re-check each stored collector **by name** →
run the collect action → set the dataset caption bar to the package `Title`.
Missing population → warning `The selection is based on an unknown population (ProcId=%d)…`;
missing collector → warning `The selection contains an unknown data element…`.

---

## C. Right panel — `pgDataset`

`TabControl`, `SelectedIndex = 0` at startup. Two tabs.

### C.1 Tab **Dataset** (`tbsOverview`)

| # | Control | Type | Exact caption | Behaviour |
|---|---|---|---|---|
| 1 | `panHdrYourDataset` | teal section header, Height 24 | — | contains 2 and 3 |
| 2 | `hdrPopulationName` | label filling the header bar, white text | initially `Your dataset`; then `Population: %d "%s". Grid size: %d x %d` | `OnDblClick = cbWideColumnsChecked` (re-applies the current column width — a quirk, it does not toggle) |
| 3 | `cbWideColumns` | check box **inside the teal bar, right-aligned, caption to the LEFT of the box** | `Wide columns` | `OnClick`: `DataColWidth := 120` when checked, `64` when not. Positioned at runtime: `Top = hdrPopulationName.Top; Left = hdrPopulationName.Width − Width`, font one point smaller |
| 4 | `panGrid` | 1 px `#646464` bordered container, fills the remaining height | — | hosts the grid (`alClient`) and `panHint` |
| 4a | *(grid)* | `TStudyOverviewGrid` (owner-drawn) | — | see C.3 |
| 4b | `panHint` | floating tooltip panel, 240 px wide, `bsSingle` border, `BorderWidth = 2`, `Color = clInfoBk` (`#FFFFE1`), initially hidden | `lblDataHint` initial caption `Data hint is shown here` | see §G.2 |
| 5 | `panSettings` → `cbShowDataHint` | check box docked at the very bottom, **`Checked = True`** | `Show data hint` | `OnClick = UpdateDataHintPanel` |

Live caption format string:

```pascal
rsGridInfo = 'Population: %d "%s". Grid size: %d x %d';
// → Population: 1 "Aktive pasienter". Grid size: 17 x 20
//   args: ProcId, Title, DataRows, FieldCount
```

Note the order: `%d x %d` is **rows × columns** (`DataRows`, `FieldCount`). Screenshot 3 shows
`17 x 20` for 17 patients across 20 fields.

### C.2 Tab **Time series** (`tbsTimeSeries`) — **VERDICT: DROP IT**

```
object tbsTimeSeries: TRzTabSheet
  Color = 15987699
  TabEnabled = False
  Caption = 'Time series'
  Enabled = False
end
```

The tab sheet is **completely empty** — no child controls at all — and it is created disabled
(`TabEnabled = False`, `Enabled = False`). A repo-wide search finds exactly **two** references
to `tbsTimeSeries`:

* `MainQuickStat.pas:130` — the published field declaration generated by the form designer.
* `MainQuickStat.dfm:1498` — the object declaration above.

There is **no code that touches it**: no `ActivePage := tbsTimeSeries`, no population, no
enabling, no data binding, nothing. It is a permanently greyed-out placeholder that a user can
see but never click (screenshots 1–4 all show `Time series` greyed out next to `Dataset`).

**Recommendation: do not port it.** Drop the tab entirely and make `pgDataset` a plain content
host (no `TabControl`) — or, if the product owner wants the visual promise preserved, keep a
single disabled `TabItem` with `IsEnabled="False"` and a "Not implemented" placeholder. My
recommendation is to remove it and note the removal in the release notes; it removes a
`TabControl` from the layout for zero functional loss.

### C.3 The dataset grid

Not a `DataGrid` in the source — a hand-drawn `TCustomDrawGrid` subclass. A WPF `DataGrid` with
a custom cell style reproduces it acceptably; the important behaviours are listed here.

**Columns.** Four *fixed* (frozen) columns followed by N data columns:

| Index | Constant | Header | Default width | Content |
|---|---|---|---|---|
| 0 | `COL_PERSON_ID` | `PID` | 44 | `PersonId` |
| 1 | `COL_PERSON_DOB` | `Født` | 64 | date of birth (`dd.MM.yyyy`) |
| 2 | `COL_PERSON_NATIONAL_ID` | `Fødselsnummer` | 84 | national id |
| 3 | `COL_PERSON_NAME` | `Navn` | 128 | full name |
| 4… | — | collector variable name (e.g. `AGE`, `YOB`, `SEX`, `NDV_INS…`, `B-Hemo…`, `P-B12`) | 64, or **120** with *Wide columns* | data points |

Headers 1–3 are **Norwegian** (`Født`, `Fødselsnummer`, `Navn`); `PID` and the data-column
headers are variable names. Keep them as-is.

**Anonymity.** When `Anonymous` (the default, i.e. anything except *Fully identified patients*),
columns 1–3 are hidden (`ColWidth = -1`). Screenshots therefore show only `PID` frozen.

**Rows.** `DefaultRowHeight = 17`, header row 18. One header row (`FixedRows = 1`).
Column resizing by drag is allowed (`goColSizing`); clicking a fixed cell selects the row.

**Cell painting rules** (`TStudyOverviewGrid.HandleCellDraw`), in priority order:

1. No object behind the cell → background `clWebSnow` `#FFFAFA`.
2. Object exposes a brush colour (lab-value percentile colouring) → use it. Colours are computed
   by `TPercentileColoring.GetColor`: white → yellow (`#FFFF00`) → orange (`#FFA500`) blends as
   the percentile rises, and red (`#FF0000`) → white blends at the low end. Screenshot 3 shows
   one such cell (`10` under `B-Hemo…`) at `#FFEDBF`.
3. Object exists but is empty for a known variable → `clWebWhiteSmoke` **`#F5F5F5`**
   (these are the light-grey blocks in screenshot 3).
4. Otherwise → `#FFFFFF`.
5. **Current cell** (`Col == Col && Row == CurrentRow`) → `#FFFBD4` (pale yellow) — overrides all
   of the above.
6. **Current row** (any other cell in the row) → 50 % blend of the cell colour with `#E7F2FC`.
7. **Fixed cells** → `FixedColor` = `#F4FBFB`.

**Text.**
* Column 0 (`PID`), header *and* data rows: `#035F66` (dark teal, `clMenuBackgroundDarkBrush`).
* Everything else: black `#000000`.
* Header row and the current row are **bold**.
* Text columns (`Født`, `Fødselsnummer`, `Navn`) are left-aligned with ellipsis; all other
  columns are **right-aligned**; the header row is left-aligned with ellipsis.
* Cell padding: 3 px horizontal, 1 px vertical.

**Grid lines.** Vertical lines between data columns `#C0C0C0`; the fixed (`PID`) column and the
header row are separated by a darker line. No vertical line inside the fixed block.

**Tooltips.** Native cell tooltips (`CM_HINTSHOW`): header row 0 → the variable's description,
header row 1 → the column subtitle, data cells → `ICellText.CellHint`. Reproduce with
`DataGridCell.ToolTip`.

**Context menu** = `mnuGridPopup`, see §D.2.

---

## D. Commands

### D.1 `ActionManager1` actions

No action in the entire form has a `Hint` or a `ShortCut`. Icons come from `lstActiveImages`
(24 × 24) with disabled variants in `lstDisabledImages`.

| Action | Caption (`.dfm`) | Hint | Shortcut | Image idx | Initial `Enabled` | Enable rule (as implemented) | Surfaced on |
|---|---|---|---|---|---|---|---|
| `actCollectData` | `Collect data` | — | — | 4 (gold magic wand + sparkles) | `False` | `ValidateCollectorSelection`: `true` iff **at least one item is checked** in `cbDataCollector`. Recomputed on every check-box click and after login. | `btnCollectData` (Collections tab, full-width, 43 px tall) |
| `actExportData` | `Open this dataset in Excel` | — | — | 3 (green Excel “X”) | `False` | Set inside `actCollectDataExecute`: `actExportData.Enabled := fGrid.Data.HasData`. **Never reset to false** after that. | `mnuGridPopup` |
| `actSaveDataPackage` | `Package this dataset for reuse` | — | — | 1 (tan parcel) | `False` | Same predicate as `actCollectData` (both are set together in `ValidateCollectorSelection`). | `mnuGridPopup` (caption **overridden** to `Package dataset specification for reuse`) |
| `actSaveDataset` | `Save dataset to CSV file` | — | — | 6 (floppy/save) | `True` | Never changed — always enabled. | `mnuGridPopup` (caption **overridden** to `Save this dataset to CSV file`) |
| `actSavePatientSelection` | `Save patient selection` | — | — | 5 | `False` | **Never changed.** | ⚠️ **Not attached to any menu item, button or toolbar.** Dead UI — see §I.2 |
| `actDeletePackage` | `Delete this package` | — | — | 7 | `True` | Never changed; validated at execute time (warns `You need to select a package for this operation.` when nothing is selected). | `tbrPackages/ToolButton1` **and** `mnuPackagePopup` |

**`RelayCommand` mapping**

```csharp
// CollectionsViewModel
[RelayCommand(CanExecute = nameof(CanCollectData))]
private async Task CollectDataAsync() { … }
private bool CanCollectData() => DataElements.Any(e => e.IsChecked);

// DatasetViewModel
[RelayCommand(CanExecute = nameof(CanExportData))]
private void OpenInExcel() { … }
private bool CanExportData() => HasData;          // set true once a collect run produced data

[RelayCommand(CanExecute = nameof(CanSavePackage))]
private void SaveDataPackage() { … }
private bool CanSavePackage() => _collections.DataElements.Any(e => e.IsChecked)
                                 && GridPopulation is not null;   // Guard.CheckNotNull in Delphi

[RelayCommand]                                     // always enabled, matches the Delphi action
private void SaveDatasetToCsv() { … }

// PackagesViewModel
[RelayCommand(CanExecute = nameof(CanDeletePackage))]
private void DeletePackage() { … }
private bool CanDeletePackage() => SelectedPackage is not null;   // improvement over Delphi,
                                   // which enables it always and warns at execute time
```

Call `NotifyCanExecuteChanged()` from the `IsChecked` setter of every data element (or observe
the collection) — the Delphi code re-evaluates on every `OnClickCheck`.

**Execute behaviour, condensed**

* `actCollectData` — wait cursor; clear variables; add the DRUID/DRUG caption records; loop the
  check-list in order, and for every checked item set the status text to the collector title and
  pull its data; then `actExportData.Enabled := HasData`; restore the list's scroll position and
  selection (§G.4); lock the grid; update the caption bar; `Done` → progress 100 % and status
  `Task completed`.
* `actExportData` — write the grid to a **temporary** CSV (`%TEMP%\<guid>.csv`, registered for
  deletion on exit) and hand it to Excel.
* `actSaveDataset` — `TFileSaveDialog`: default file name `QuickStat.csv`, default extension
  `*.csv`, one file type `Comma separated values` / `*.csv`, OK button labelled `Save`,
  overwrite prompt on, strict file types on.
* `actSaveDataPackage` — collect the checked collector *names*, `frmSaveSpec.Clear()`,
  `SetHeader('Save specification')`, show modal; on OK build a `TPackagedSelection` and save it,
  then refresh the packages list.
* `actSavePatientSelection` — `SetHeader('Save selection')` (**without** `Clear()` — see §I.3),
  show modal; on OK save the selection and log `Selection was successfully saved.` or
  `There was a problem:\n%s`.
* `actDeletePackage` — if nothing selected, warn `You need to select a package for this
  operation.`; otherwise confirm with a Yes/No dialog
  `Do you really want to delete this package:\n"%s"?` and delete on Yes.

### D.2 Grid context menu (`mnuGridPopup`)

Verified character-for-character against screenshot 4:

| # | Item | Icon | Command |
|---|---|---|---|
| 1 | `Package dataset specification for reuse` | tan parcel box | `SaveDataPackageCommand` |
| 2 | *(separator)* | — | — |
| 3 | `Open this dataset in Excel` | green Excel “X” | `OpenInExcelCommand` |
| 4 | `Save this dataset to CSV file` | floppy/save | `SaveDatasetToCsvCommand` |

Menu chrome in the screenshot is stock Win10: face `#F2F2F2`, 1 px `#A0A0A0` border,
separator `#D7D7D7`, hovered item `#CCE8FF` fill with a `#91C9F7` border. In the port just use
the default WPF `ContextMenu` styling with 16 px icons.

### D.3 Package context menu (`mnuPackagePopup`)

One item: `Delete this package` (same command as the toolbar button).

### D.4 Message boxes raised by the workflow

These come through `GlobalLog.Event(..., ltMessage|ltWarning|ltError)` and
`GlobalLog.LogYesNo(...)`, which show a modal dialog for `ltMessage` and above.
**Note: the multi-line strings in `MainQuickStat.pas` contain literal `\n` two-character
sequences, not real line breaks** (Delphi single-quoted strings do not process escapes) — this
is a latent bug; use real newlines in the port.

| Trigger | Level | Text |
|---|---|---|
| Package references an unknown population | warning | `The selection is based on an unknown population (ProcId=%d).` / `The data collection can not be performed at this time.` / `Perhaps the population is from a different protocol?` |
| Package references an unknown collector | warning | `The selection contains an unknown data element.` / `Element name was "%s".` / `The data collection will be incomplete.` / `Perhaps the selection was created in a later version?` |
| Selection saved | message | `Selection was successfully saved.` |
| Save failed | warning | `There was a problem:` + message |
| Delete with nothing selected | warning | `You need to select a package for this operation.` |
| Delete confirmation | Yes/No | `Do you really want to delete this package:` / `"%s"?` |
| Population not selected as expected | error | `The population was not selected as expected,` / `You may need an updated version of QuickStat.` |
| Config file missing at startup | exception | `The configuration file %s was not found.` / `QuickStat can not be used without this file.` |
| No population | message | `No population selected!` |
| Invalid population (frame) | message | `Det er ikke valgt en gyldig populasjon.` *(Norwegian — the only Norwegian string in the chrome; consider translating to `No valid population is selected.` and flag the change)* |

### D.5 Secondary modal — period picker (`TfrmPeriod`)

Raised automatically, **not** from a menu: when a population's or collector's SQL declares both
`@StartDate` and `@StopDate` parameters, `TParameterDictionary` prompts for a period before
running the query. Cancelling aborts the query.

Fully Norwegian, 527 × 374, centred on the main window, white top banner:

| Element | Text |
|---|---|
| Window / main header | `Angi periode` |
| Sub-header | set at runtime to `Denne spørringen krever at du angir et tidsintervall.` |
| Tip label | `Tips: Klikk på månedens navn for å "zoome ut" hvis datoen du vil ha er langt unna.` |
| Two calendars | Monday-first, `FirstYear = 1900`, Segoe UI |
| Bottom info (valid) | `Angis som fra og med første dato (til venstre), og til men ikke inkludert siste dato (til høyre).` |
| Bottom info (invalid) | `Siste dato må være etter første dato.` / `Merk at siste dato ikke er med i perioden.` |
| Buttons | `OK` (84 × 36) / `Avbryt` (84 × 36), right-aligned |

`OK` is disabled while `start >= end`. Start/end are remembered per query in the ini file
(`PeriodStart` / `PeriodEnd` keyed by the SQL text). Port with two WPF `Calendar` controls.

---

## E. `TfrmSaveSpec` — the save modal (`Emetra.VclForm.EditAndMemo`)

One dialog serves two purposes; only the header/title text differs.

| | Value |
|---|---|
| Size | 388 × 288 client, **not resizable** |
| Position | centred on the main window (`poMainFormCenter`) |
| Header text | `SetHeader(s)` sets **both** `Caption` (title bar) and the big banner label |
| — from `actSaveDataPackage` | `Save specification` |
| — from `actSavePatientSelection` | `Save selection` |
| Banner | white strip, 41 px, 32 × 33 icon at `Margin 16,3`, then the header label in Tahoma 19 px Bold, vertically centred |
| Body | 16 px border all round |
| Field 1 label | `Unique name` |
| Field 1 | `edtTitle` — single-line `TextBox`, 21 px, full width → `Title` property |
| Field 2 label | `Comments` (4 px extra top margin) |
| Field 2 | `memComment` — multi-line `TextBox` filling the rest → `Comment` property |
| Button bar | 48 px, docked bottom, 1 px top border |
| OK | `TBitBtn Kind = bkOK` → caption **`OK`**, 92 × 30, `ModalResult = mrOk`, `Margin R 16` |
| Cancel | `TBitBtn Kind = bkCancel` → caption **`Cancel`**, 88 × 30, `ModalResult = mrCancel`, `Margin R 4` |
| `Clear()` | empties both fields |

Semantics:

* The form is created **once** at `FormShow` and reused (`Application.CreateForm(TfrmSaveSpec, …)`),
  so field contents survive between invocations unless `Clear()` is called.
* `actSaveDataPackage` calls `Clear()` before `SetHeader`/`ShowModal`.
* `actSavePatientSelection` **does not** — the previous title/comment is still there. See §I.3.
* There is **no validation**: OK is always enabled and an empty `Title` is accepted.
  Recommended (and low-risk) improvement: disable OK while `Title` is blank — flag it.
* Cancel does nothing at all (no side effects).

WPF: a `Window` with `WindowStartupLocation="CenterOwner"`, `ResizeMode="NoResize"`,
`ShowInTaskbar="False"`, `SizeToContent="Manual"`, `IsDefault` on OK and `IsCancel` on Cancel.

---

## F. Visual design

### F.1 Extracted palette

All values verified twice: from the Delphi constants **and** by sampling the screenshots.

| Role | Delphi constant | `$00BBGGRR` | Hex | Sampled |
|---|---|---|---|---|
| **Section header fill (teal)** | `clArenaListSelectedBackground` / `clMenuItemSelectionFill` | `$00918817` | **`#178891`** | ✔ `#178891` |
| Section header text | `clMenuItemSelectionForeground` | `$00FFFFFF` | `#FFFFFF` | ✔ |
| Page / tab-sheet background | `clMyGreenColor` | `$00FBFBF4` | **`#F4FBFB`** | ✔ |
| Splitter fill | `clMyGreenColor` | `$00FBFBF4` | `#F4FBFB` | ✔ (grip `#A0A0A0`) |
| Form face | `clFormFace` | `$00EEEEEE` | `#EEEEEE` | — |
| Banner | `clWhite` | — | `#FFFFFF` | ✔ |
| List background | `clMyListboxColor` | `$00FFFFFF` | `#FFFFFF` | ✔ |
| List alternate row | `clMyAlternateColor` | `$00F7F7F7` | `#F7F7F7` | ✔ |
| List selected (focused) | `clArenaListSelectedBackground` | `$00918817` | `#178891` | ✔ |
| List selected (unfocused) | `clListSelectedBackgroundUnfocused` | `$00B6AE50` | `#50AEB6` | — |
| List selected text | `clArenaListSelectedForeground` | `$00FFFFFF` | `#FFFFFF` | ✔ |
| **List code / id column** | `clCodeColor` | `$00A4294B` | **`#4B29A4`** (purple) | ✔ |
| **List category column** | `clStatusTextColor` | `$00822EB8` | **`#B82E82`** (fuchsia) | ✔ |
| List title text | `clTextColor` | `$00333333` | `#333333` | ✔ |
| Grid fixed (header) fill | `clMyGreenColor` | `$00FBFBF4` | `#F4FBFB` | ✔ |
| **Grid fixed / PID text** | `clMenuBackgroundDarkBrush` | `$00665F03` | **`#035F66`** (dark teal) | ✔ |
| Grid normal cell | `clWhite` | — | `#FFFFFF` | ✔ |
| Grid "known empty" cell | `clWebWhiteSmoke` | — | **`#F5F5F5`** | ✔ |
| Grid "no object" cell | `clWebSnow` | — | `#FFFAFA` | — |
| Grid current cell | `clFocusedSelectionColor` | `$00D4FBFF` | **`#FFFBD4`** (pale yellow) | ✔ |
| Grid current row tint | `clUnfocusedSelectionColor` | `$00FCF2E7` | `#E7F2FC` → 50 % ≈ `#F3F9FE` | — |
| Grid line | `clSilver` | — | `#C0C0C0` | ✔ |
| Panel bevel (light) | — | — | `#A0A0A0` + 1 px `#FFFFFF` inner | ✔ |
| Panel border (grid host) | — | — | `#646464` | ✔ |
| Data hint panel | `clInfoBk` | — | `#FFFFE1` | — |
| "version" field label | Raize default | — | `#0078D7` | ✔ |
| Progress bar fill | OS theme | — | `#06B025` | ✔ |
| Lab value warn/alarm | percentile blend | — | white → `#FFFF00` → `#FFA500`; low end `#FF0000` → white; observed `#FFEDBF` | ✔ |
| List divider | `clBtnFace` | — | `#F0F0F0` | ✔ |
| Window border | OS accent (**not** app-controlled) | — | `#96C254` in the screenshots | ✔ |

### F.2 Typography (as shipped)

| Element | Delphi font | Size | Style |
|---|---|---|---|
| Form / everything with `ParentFont` | Calibri | 10 pt (13.3 px) | regular |
| Section header labels | Calibri | 11 pt (14.7 px) | regular, white |
| Tab captions | Calibri | 10 pt | selected tab **bold** |
| Population list rows | Calibri | 10 pt | id regular, title **bold**, category 9 pt |
| `cbSimpleView` ("Simplified") | Calibri | 9 pt | regular |
| `cbWideColumns` | Calibri | 9 pt | regular |
| `lblProgress` ("Progress") | Calibri | 10 pt | **bold** |
| `lblAppName` ("QuickStat") | **Tahoma** | 20 px (15 pt) | **bold** |
| `RzVersionInfoStatus1` | **Tahoma** | 13 px (≈10 pt) | regular; the word `version` in `#0078D7`, the number in black |
| Grid cells | Calibri | 10 pt | header row and current row **bold** |
| SQL preview `memSourceCode` | **Consolas** | 8 pt (10.7 px) | regular |
| `TfrmSaveSpec` banner | Tahoma | 19 px | bold |

### F.3 Typography (port)

Single system font stack, no Calibri/Tahoma:

| Role | WPF |
|---|---|
| Base / body | `Segoe UI`, `FontSize="12"` |
| Section header | `Segoe UI`, `FontSize="13"`, `Foreground="White"` |
| Tab caption | `Segoe UI`, `FontSize="13"`; selected `FontWeight="SemiBold"` |
| Wordmark | `Segoe UI`, `FontSize="20"`, `FontWeight="SemiBold"` |
| Version | `Segoe UI`, `FontSize="12"`; `version` in `QsAccentBrush`, number in `QsTextBrush` |
| List title | `FontSize="12"`, `FontWeight="SemiBold"` |
| List id / category | `FontSize="11"` |
| Grid | `FontSize="12"`; header `SemiBold` |
| Code / SQL | `Cascadia Mono, Consolas, Courier New`, `FontSize="11"` |
| Paragraph (`lblDataElementInfo`) | `FontSize="12"`, `TextWrapping="Wrap"`, `LineHeight="17"` |

Spacing rhythm: **4 / 8 / 16**. Section header bar `Padding="8,4"`, `Height="26"`. Controls
inside a tab: `Margin="4"` horizontally, `8` between logical groups. No `BorderThickness > 1`.

### F.4 Resource dictionary

`Themes/QuickStat.Brushes.xaml`:

```xml
<!-- Brand -->
<SolidColorBrush x:Key="QsTealBrush"              Color="#178891"/>  <!-- section headers, selection -->
<SolidColorBrush x:Key="QsTealDarkBrush"          Color="#035F66"/>  <!-- PID column text -->
<SolidColorBrush x:Key="QsTealHoverBrush"         Color="#1A9BA6"/>  <!-- new: header hover -->
<SolidColorBrush x:Key="QsTealUnfocusedBrush"     Color="#50AEB6"/>

<!-- Surfaces -->
<SolidColorBrush x:Key="QsBannerBrush"            Color="#FFFFFF"/>
<SolidColorBrush x:Key="QsPageBrush"              Color="#F4FBFB"/>  <!-- tab pages, splitter -->
<SolidColorBrush x:Key="QsSurfaceBrush"           Color="#FFFFFF"/>  <!-- lists, grid, editors -->
<SolidColorBrush x:Key="QsAltRowBrush"            Color="#F7F7F7"/>
<SolidColorBrush x:Key="QsFormFaceBrush"          Color="#EEEEEE"/>

<!-- Lines -->
<SolidColorBrush x:Key="QsBorderBrush"            Color="#D0D6D6"/>  <!-- modernised from #A0A0A0 -->
<SolidColorBrush x:Key="QsBorderStrongBrush"      Color="#9AA5A5"/>  <!-- modernised from #646464 -->
<SolidColorBrush x:Key="QsGridLineBrush"          Color="#E2E6E6"/>  <!-- modernised from #C0C0C0 -->
<SolidColorBrush x:Key="QsDividerBrush"           Color="#EDF1F1"/>

<!-- Text -->
<SolidColorBrush x:Key="QsTextBrush"              Color="#202020"/>
<SolidColorBrush x:Key="QsTitleBrush"             Color="#333333"/>
<SolidColorBrush x:Key="QsMutedTextBrush"         Color="#5E6A6A"/>
<SolidColorBrush x:Key="QsOnAccentBrush"          Color="#FFFFFF"/>
<SolidColorBrush x:Key="QsCodeBrush"              Color="#4B29A4"/>  <!-- population/package id -->
<SolidColorBrush x:Key="QsCategoryBrush"          Color="#B82E82"/>  <!-- ProcGroup / Pop#n -->
<SolidColorBrush x:Key="QsAccentBrush"            Color="#0078D7"/>  <!-- "version" label -->

<!-- Grid semantics -->
<SolidColorBrush x:Key="QsCellEmptyBrush"         Color="#F5F5F5"/>
<SolidColorBrush x:Key="QsCellNoDataBrush"        Color="#FFFAFA"/>
<SolidColorBrush x:Key="QsCurrentCellBrush"       Color="#FFFBD4"/>
<SolidColorBrush x:Key="QsCurrentRowBrush"        Color="#F3F9FE"/>
<SolidColorBrush x:Key="QsHintBackgroundBrush"    Color="#FFFFE1"/>
<SolidColorBrush x:Key="QsHintBorderBrush"        Color="#D8D2A8"/>

<!-- Progress -->
<SolidColorBrush x:Key="QsProgressBrush"          Color="#06B025"/>
<SolidColorBrush x:Key="QsProgressTrackBrush"     Color="#E3E9E9"/>
```

`Themes/QuickStat.Styles.xaml` — the named styles the XAML author needs:

| Key | Target | Description |
|---|---|---|
| `QsSectionHeader` | `Border` (or a tiny `HeaderBar` `UserControl`) | `Background={QsTealBrush}`, `Height=26`, `Padding=8,4`, `CornerRadius=0`, content `TextBlock` white 13 px. Supports an optional right-aligned `ContentPresenter` for the *Wide columns* check box. |
| `QsHeaderText` | `TextBlock` | white, 13 px |
| `QsTabControl` / `QsTabItem` | `TabControl` / `TabItem` | flat, square, `Background={QsPageBrush}`; selected item gets a **3 px `QsTealBrush` bar along the TOP edge** and `FontWeight=SemiBold`; no border, no card frame (this is exactly what `RzPageControl` with `TabColors.HighlightBar` renders — confirmed in screenshot 1) |
| `QsFlatTextBox` | `TextBox` | 1 px `QsBorderBrush`, `Padding=6,3`, focus border `QsTealBrush` |
| `QsFlatComboBox` | `ComboBox` | same chrome, `MaxDropDownHeight` ≈ 24 items |
| `QsPrimaryButton` | `Button` | full width, `Height=40`, icon 20 px + label, `Background=QsSurfaceBrush`, 1 px `QsBorderBrush`; hover `#EAF6F7`, pressed `#DCF0F1`, disabled 40 % opacity — replaces the Win32 `E5F1FB`/`0078D7` hot state |
| `QsToolButton` | `Button` | 30 × 30, icon only, transparent until hover |
| `QsPopulationItem` | `ListBoxItem` | see B.1.1: alternation, teal selection, white selected text, 1 px bottom divider |
| `QsPackageItem` | `ListBoxItem` | see B.3 |
| `QsCheckListItem` | `ListBoxItem` | `CheckBox` + text, `Padding=4,2`, native selection colours |
| `QsCaptionLeftCheckBox` | `CheckBox` | Caption to the **left** of the box, i.e. Delphi `Alignment = taLeftJustify`. Used by *Wide columns* (§C.1) and *Simplified* (§B.1). `FlowDirection=RightToLeft` is the only way to get that from a stock `CheckBox`, but it mirrors the whole subtree and the default template draws the tick as a `Path`, so the check comes out backwards; the style carries an implicit `Path` style in `Style.Resources` setting it back to `LeftToRight`, which reaches inside the control's own template and flips the glyph alone. Call sites still set `FlowDirection=LeftToRight` on the caption `TextBlock`. Added during Phase 3 wave 2, from a defect seen in a running build; pinned by `Ui/Theme/CaptionLeftCheckBoxTests.cs`, which compares rendered pixels |
| `QsDataGrid`, `QsDataGridColumnHeader`, `QsDataGridCell`, `QsDataGridRow` | `DataGrid` family | see C.3 |
| `QsProgressBar` | `ProgressBar` | `Height=10`, `Foreground={QsProgressBrush}`, `Background={QsProgressTrackBrush}`, no bevel |
| `QsSplitter` | `GridSplitter` | `Width=8`, `Background={QsPageBrush}`, 2 × 20 px `QsBorderBrush` grip dots centred |
| `QsHintPanel` | `Popup`/`Border` | `QsHintBackgroundBrush`, 1 px `QsHintBorderBrush`, `Padding=6,4`, drop shadow off |

### F.5 ASCII wireframe (main window, Collections tab active, populated grid)

```
┌────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ [▨] FastTrak QuickStat                                                            ─    □    ×      │  OS chrome
├────────────────────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                                    │
│  ┌──┐                                                                    Progress                  │  panWhiteTop
│  │▨ │  QuickStat  version 19.8.14.477                                    Task completed            │  H=55  #FFFFFF
│  └──┘                                                                    ████████████████████░░░░  │  bar #06B025
├────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ ▔▔▔▔▔▔▔▔▔▔                              ║ ▔▔▔▔▔▔▔                                                   │  3 px teal bar
│ Population  Collections  Packages       ║ Dataset   Time series (disabled)                         │  tabs, H=28
│─────────────────────────────────────────║────────────────────────────────────────────────────────  │
│ ┌─────────────────────────────────────┐ ║ ┌────────────────────────────────────────────────────┐   │
│ │ Select data elements                │ ║ │ Population: 1 "Aktive pasienter". Grid size: 17x20 │   │  teal H=26
│ └─────────────────────────────────────┘ ║ │                                    Wide columns ☐  │   │
│ Select data elements from the list      ║ └────────────────────────────────────────────────────┘   │
│ below, and click "Collect data" at the  ║ ┌────────────────────────────────────────────────────┐   │
│ bottom to start the process.  Depending ║ │ PID │ AGE │ YOB  │ SEX │ NDV_… │ INS_… │ B-Hem… │ … │   │  header #F4FBFB
│ on what you select, this will take some ║ ├─────┼─────┼──────┼─────┼───────┼───────┼────────┼───┤   │  PID text #035F66
│ time!                                   ║ │   8 │  97▓│ 1922 │   1 │     1 │    36 │        │   │   │  ▓ = current cell
│ ┌─────────────────────────────────────┐ ║ │  13 │  95 │ 1924 │   1 │     1 │       │        │   │   │
│ │ ☑ ^ Alder                         ▲ │ ║ │  17 │  94 │ 1925 │   1 │     2 │▒▒▒▒▒▒▒│▒▒▒▒▒▒▒▒│▒▒▒│   │  ▒ = #F5F5F5 empty
│ │ ☐ ^ Dødsår                          │ ║ │  24 │  90 │ 1929 │   1 │     1 │▒▒▒▒▒▒▒│▒▒▒▒▒▒▒▒│▒▒▒│   │
│ │ ☐ ^ Fødselmåned                     │ ║ │  27 │  88 │ 1931 │   1 │     1 │     0 │        │   │   │
│ │ ☑ ^ Fødselsår                       │ ║ │  52 │  55 │ 1964 │   1 │       │       │   ▓10▓ │   │   │  ▓ = lab colouring
│ │ ☐ ^ Gruppe / avdeling nå            │ ║ │ ... │     │      │     │       │       │        │   │   │
│ │ ▓ ^ Kjønn                (selected) │ ║ │                                                    │   │
│ │ ☑ Diabetes: Behandling (siste)      │ ║ │        ┌──────────────────────────┐                │   │
│ │ ☑ Labdata: Alle med høy konfidens   │ ║ │        │ PersonId = 52            │  ← panHint     │   │
│ │ ☐ Labdata: Anemi (siste)          ▼ │ ║ │        │ 10 g/dL  (2019-04-02)    │    #FFFFE1     │   │
│ └─────────────────────────────────────┘ ║ │        └──────────────────────────┘                │   │
│ ┌─────────────────────────────────────┐ ║ │                                                    │   │
│ │        ✨  Collect data              │ ║ │                                                    │   │
│ └─────────────────────────────────────┘ ║ └────────────────────────────────────────────────────┘   │
│ ┌─────────────────────────────────────┐ ║ ☑ Show data hint                                         │
│ │ Export options                      │ ║                                                          │
│ └─────────────────────────────────────┘ ║                                                          │
│ ○ Fully identified patients             ║                                                          │
│ ◉ Identified with PID only              ║                                                          │
│ ○ Generate new random PIDs              ║                                                          │
│ ☐ Export timestamp for every data elem. ║                                                          │
└─────────────────────────────────────────╨──────────────────────────────────────────────────────────┘
  ◄─────────── 293 px (min 260) ─────────►│8│◄──────────────── * ──────────────────────────────────►
```

Population tab, left panel only:

```
│ ┌─────────────────────────────────────┐ │
│ │ Select database                     │ │  teal
│ └─────────────────────────────────────┘ │
│ [ Testdatabase (NDV)                ▾ ] │  cbProject (sorted)
│ ┌─────────────────────────────────────┐ │
│ │ Select population                   │ │  teal
│ └─────────────────────────────────────┘ │
│  Filter / search text                   │
│ [ Type filter text here               ] │
│  ☐ Frequently used only    Simplified ☐ │  ← right one has caption on the LEFT
│ ┌─────────────────────────────────────┐ │
│ │ 257  HbA1c > 53 (7%)  Type 1 u/pumpe│ │  id #4B29A4 · title bold #333 · group #B82E82
│ │ 258  Siste hos øyelege      Prosess │ │
│ │ 270  HbA1c > 75 (9%)     Behandling │ │  ← selected: #178891 bg, all text white
│ │ ...                                 │ │
│ ├──────────────── ══ ─────────────────┤ │  ← vertical splitter (77 %)
│ │ CREATE PROCEDURE dbo.GetCaseList…   │ │  memSourceCode, Consolas, read-only
│ │ BEGIN                               │ │
│ └─────────────────────────────────────┘ │
│ Tip: Double click to prepare population │
```

---

## G. Behavioural details that are easy to miss

### G.1 Form-state persistence (`IGuiSettings`)

`fGuiSettings := TGuiSettings.Create(Self, TIniSettings.Create, GlobalLog)`.
`RestoreFormState` on `FormShow`; `SaveFormState` on `FormClose`.

Ini section key: `frmQuickStat.<ScreenWidth>x<ScreenHeight>` — **per screen resolution**, so a
laptop docked to a 4K monitor keeps a separate geometry. Values:

| Key | Type | Saved | Restored |
|---|---|---|---|
| `State` | int (`TWindowState`: 0 = Normal, 1 = Minimized, 2 = Maximized) | always | always; if ≠ Normal, the bounds below are ignored |
| `Left` | int | only when Normal | only when Normal |
| `Top` | int | only when Normal | only when Normal |
| `Width` | int | only when Normal | only when Normal |
| `Height` | int | only when Normal | only when Normal |

Guard rail: if the restored rectangle does not intersect any monitor's work area, the window
falls back to `Screen.WorkAreaRect` (full work area). Reimplement this — it matters for users who
unplug a second monitor.

**Not persisted** (although the helper class has methods for it, they are never called from
QuickStat): the `splMain` splitter position, the population frame's inner splitter, the selected
tabs, the check-list selection, the `Frequently used only` / `Simplified` / `Wide columns` /
`Show data hint` / identification settings, the chosen database, and the window font/colour
overrides (`TryGetFont`/`TryGetColor`/`SaveFont`/`SaveColor` exist but are unused).

Also persisted separately by `TPeriodDictionary`: `PeriodStart` / `PeriodEnd`, keyed by the SQL
text of the query that asked for them.

**Recommendation for the port:** keep the same set as a minimum, and additionally persist the
splitter position and the last-used database (both are obvious wins) — but flag them as
additions rather than slipping them in silently.

### G.2 The floating data-hint panel

Not a tooltip — a real panel parented to the grid host and moved on every click.

```pascal
panHint.Visible := false;                                // always hidden first
if cbShowDataHint.Checked then
  if TryGetPatientAtRow(fGrid.Row, thisPatient) then
  begin
    strHint := fGrid.Anonymous ? Format('PersonId = %d', [PersonId]) : thisPatient.FullName;
    if TryGetDatapoint(fGrid.Col, fGrid.Row, thisDatapoint) then
    begin
      lblDataHint.Caption := strHint + sLineBreak + thisDatapoint.AsString;
      panRect := fGrid.CellRect(fGrid.Col, fGrid.Row);
      OffsetRect(panRect, 3, 3);
      panHint.Top  := fGrid.Top  + panRect.Top + fGrid.DefaultRowHeight + 1;
      panHint.Left := fGrid.Left + panRect.Left;
      panHint.ClientHeight := 8 * abs(lblDataHint.Font.Height) + panHint.BorderWidth*2 + 8;
      panHint.Visible := true;
      panHint.BringToFront;
    end;
  end;
```

* Triggered by `fGrid.OnClick` **and** by toggling `cbShowDataHint`.
* Anchored just **below** the clicked cell (cell top + one row height + 1, offset 3,3), left edge
  aligned with the cell's left edge. It is **not** repositioned on hover or on keyboard
  navigation — only on click.
* Fixed width 240 px; the height formula yields ≈ 116 px for a two-line hint, which is far too
  tall — **size to content in the port** and cap the width at ~320 px.
* Content: line 1 = `PersonId = <n>` when anonymous, else the patient's full name;
  line 2 = the data point's `AsString`.
* Hidden whenever the cell has no data point, or `Show data hint` is off.
* Any exception while building the hint turns `lblInfo` **red** and shows the exception message
  in the status area — reproduce with an error state on the status text.
* WPF: a non-focusable `Popup` (`Placement=Relative` to the `DataGridCell`, `AllowsTransparency=False`,
  `StaysOpen=True`) or a `Canvas`-hosted `Border` over the grid. Keep it click-driven.

### G.3 Wait cursors

`Screen.Cursor := crSqlWait` (an hourglass/database cursor) is set for the duration of:

| Operation | Restores to |
|---|---|
| `SelectConnection` (database change + connect) | `crDefault` |
| `AfterPopulationSelect` (loading a population into the grid) | the **previously saved** cursor |
| `actCollectDataExecute` (the whole collect run) | the **previously saved** cursor |

Port as `Mouse.OverrideCursor = Cursors.Wait` in a `try/finally`, or — better, since these are
now `async` — an `IsBusy` flag that drives both the cursor and command `CanExecute`. Note that
the Delphi code also calls `Application.ProcessMessages` inside `SelectConnection` to force a
repaint; in WPF do the work off the UI thread instead.

### G.4 Check-list scroll position across a collect run

`actCollectDataExecute` walks the check-list and **sets `ItemIndex := n` for every checked item**
so the user can see progress. Before the loop it saves, and after the loop it restores, both the
scroll offset and the selection:

```pascal
savedTopIndex  := cbDataCollector.TopIndex;
savedItemIndex := cbDataCollector.ItemIndex;
…loop, moving ItemIndex and calling Update…
cbDataCollector.TopIndex  := savedTopIndex;
cbDataCollector.ItemIndex := savedItemIndex;
```

In WPF this "highlight the element being collected" feedback is worth keeping — bind a
`CurrentlyCollecting` element and let the `ListBox` style highlight it — but **do not** move
`SelectedItem`, and restore the scroll offset (`ScrollViewer.VerticalOffset`) when done. In
screenshot 2/3 you can see the residual selection on `^ Kjønn`.

### G.5 Sorting

* `cbDataCollector.Sorted := true` (set in `FormShow`) — the data-element list is sorted
  **alphabetically by title**. This is why the `^ `-prefixed demographic collectors float to the top.

  **Corrected during Phase 3 wave 2 (step 3.3): use `StringComparer.CurrentCultureIgnoreCase`, not
  `StringComparer.Ordinal`.** This bullet previously said ordinal, "to keep the `^` group first". It
  does the opposite: `'^'` is U+005E, above `'Z'` and below `'a'`, and every other title starts with
  a capital, so ordinal sorts all eleven `^ ` elements **last**. Sorted against the real
  120-collector KORTTID registry, only a linguistic ignore-case comparer reproduces screenshot 2
  (`^ Alder … ^ Statuskode`, then `Antropometri…`, `Diabetes…`, `Labdata…`, `NDV…`). `Sorted := true`
  is `LBS_SORT`, whose default comparison is `CompareStringW(LOCALE_USER_DEFAULT, NORM_IGNORECASE)` —
  culture-sensitive, which is what `CurrentCultureIgnoreCase` means. See PORT-PLAN.md §6; this is a
  parity item, and getting it wrong reorders every exported CSV.
* `cbProject.Sorted := true` (set in `FormShow`) — the database drop-down is sorted by display
  name, and no item is preselected.
* Populations are **not** sorted by the client — they arrive in stored-procedure order
  (ascending `ProcId` in the screenshots).
* Collector names inside a saved package are stored semicolon-delimited, sorted, deduplicated.

### G.6 Other

* **`Done()`** sets the progress bar to 100 % and the status text to `Task completed`. It is
  called at the end of a connect and at the end of a collect run. The initial status text is
  `Program is idle`; the `Progress` header label is **never changed at runtime** (the
  `IProgress.SetHeader` implementation exists but nothing calls it) — treat it as a static label.
* Status texts you will see: `Program is idle`, `New project selected`, `Connecting to %s ...`,
  `Loading collectors`, `<collector title>` (during a run), `Task completed`.
* Progress percentage is driven per patient during a collect run
  (`Percent := 100 * personIndex / population.Count`).
* **Temporary CSV files** created by *Open this dataset in Excel* are collected in
  `fFilesThatMustBeDeleted` and deleted on shutdown (best-effort, exceptions logged). Port this
  or use `FileOptions.DeleteOnClose` semantics.
* The `Frequently used only` check box starts **disabled** and only becomes enabled once a study
  is connected (`StudyId > 0`).
* Toggling `Frequently used only` **re-queries the database** (a different stored procedure);
  toggling `Simplified` or typing in the filter does not.
* `ShowAll` toggling in the population list **clears the filter text** as a side effect — that
  path is unreachable in QuickStat (`AShowAll` is always `false`), so do not reproduce it.
* Right-clicking the grid does not change the current cell in the VCL version; WPF `DataGrid`
  will select the cell on right-click. That is a behaviour change but an improvement — flag it.
* `ReportMemoryLeaksOnShutdown := true` in the `.dpr` is a debug aid; no UI equivalent.

---

## H. Proposed WPF view / view-model breakdown

Flat repo layout, one folder level for views and view-models.

### H.1 Views

| File | Type | Contents |
|---|---|---|
| `MainWindow.xaml` | `Window` | banner + `GridSplitter` + the two tab controls; hosts the child views |
| `Views/AppBannerView.xaml` | `UserControl` | icon, wordmark, version, `Progress` block (header / info / bar) |
| `Views/PopulationTabView.xaml` | `UserControl` | `Select database` header + combo, `Select population` header, hosts `PopulationPickerView`, tip label |
| `Views/PopulationPickerView.xaml` | `UserControl` | filter header + box, the two check boxes, the population `ListBox`, splitter, SQL preview |
| `Views/CollectionsTabView.xaml` | `UserControl` | header, paragraph, checked `ListBox`, `Collect data` button, `Export options` header, radio group, timestamp check box |
| `Views/PackagesTabView.xaml` | `UserControl` | `Packaged datasets` header, filter box, toolbar, package `ListBox` + context menu |
| `Views/DatasetTabView.xaml` | `UserControl` | caption bar + `Wide columns`, the `DataGrid` (+ hint popup + context menu), `Show data hint` |
| `Views/SaveSpecDialog.xaml` | `Window` | §E |
| `Views/PeriodDialog.xaml` | `Window` | §D.5 |
| `Controls/SectionHeader.cs` + style | `HeaderedContentControl` | the teal bar; `Header` string + optional right-aligned `Content` |
| `Themes/QuickStat.Brushes.xaml`, `Themes/QuickStat.Styles.xaml` | `ResourceDictionary` | §F.4 |
| `Converters/…` | | `BoolToVisibility`, `EnumToBool` (radio group), `NullToBool` |

*(No `TimeSeriesTabView` — see §C.2.)*

### H.2 View-models and where state lives

```
MainViewModel                                   ← singleton, the composition root
├─ string   WindowTitle            = "FastTrak QuickStat"
├─ string   VersionText            (from AssemblyFileVersion)
├─ string   ProgressHeader         = "Progress"          (static, never changes)
├─ string   ProgressInfo           = "Program is idle"   (IProgress.SetInfo)
├─ double   ProgressPercent        0..100                (IProgress.SetProgress)
├─ bool     ProgressIsError                              (turns ProgressInfo red)
├─ bool     IsBusy                                       (wait cursor + command gating)
├─ int      SelectedSelectionTab   0..2
├─ int      SelectedDatasetTab     0        (drops to a single view if Time series is removed)
├─ double   SplitterPosition       293
├─ PopulationTabViewModel   Population
├─ CollectionsTabViewModel  Collections
├─ PackagesTabViewModel     Packages
└─ DatasetViewModel         Dataset

PopulationTabViewModel
├─ ObservableCollection<ProjectConnection> Projects     (from QuickStat.config.xml, sorted)
├─ ProjectConnection?  SelectedProject                  → triggers connect
└─ PopulationPickerViewModel Picker

PopulationPickerViewModel
├─ string   FilterText                       (live, untrimmed, case-insensitive)
├─ bool     FrequentlyUsedOnly               (disabled until connected; re-queries)
├─ bool     Simplified                       (client-side row expansion only)
├─ bool     CanFilterFrequentlyUsed          (StudyId > 0)
├─ ObservableCollection<PopulationViewModel> Populations
├─ ICollectionView                            PopulationsView   (the filter)
├─ PopulationViewModel? SelectedPopulation   → updates SqlPreview
├─ string   SqlPreview
├─ bool     IsSqlPreviewVisible              (FUNC_POPULATION_SOURCE access right)
└─ IRelayCommand PreparePopulationCommand    (double click / Enter)

PopulationViewModel : { int ProcId; string Title; string Group; string HelpText; string SourceCode }

CollectionsTabViewModel
├─ ObservableCollection<DataElementViewModel> DataElements   (sorted ordinal by Title)
├─ DataElementViewModel? CurrentlyCollecting                 (visual feedback during a run)
├─ PersonIdentification Identification = PersonIdOnly        (enum, bound via 3 radios)
├─ bool     ExportTimestamps                                 = false
├─ bool     HasPopulation                                    (drives tab visibility + enabled)
└─ IAsyncRelayCommand CollectDataCommand                     (CanExecute: any IsChecked)

DataElementViewModel : { string Name; string Title; bool IsChecked }

PackagesTabViewModel
├─ string   FilterText                       (live, trimmed, case-insensitive)
├─ ObservableCollection<PackageViewModel> Packages
├─ ICollectionView PackagesView
├─ PackageViewModel? SelectedPackage
├─ IRelayCommand DeletePackageCommand        (CanExecute: SelectedPackage is not null)
└─ IAsyncRelayCommand OpenPackageCommand     (double click → the replay in §B.3)

PackageViewModel : { int RowId; string Title; string Comment; int PopulationId; IReadOnlyList<string> CollectorNames }

DatasetViewModel
├─ string   CaptionText              = "Your dataset" → "Population: {0} \"{1}\". Grid size: {2} x {3}"
├─ bool     WideColumns              (64 ↔ 120 px data columns)
├─ bool     ShowDataHint             = true
├─ DataHint? Hint                    { string Line1; string Line2; Point Anchor; bool IsOpen }
├─ bool     HasData                  (gates OpenInExcel)
├─ DatasetGridViewModel Grid         (rows, columns, cell states, anonymity)
├─ IRelayCommand SaveDataPackageCommand
├─ IRelayCommand OpenInExcelCommand
└─ IRelayCommand SaveDatasetToCsvCommand

SaveSpecViewModel  { string Header; string Title; string Comment; bool CanSave => !IsBlank(Title) }
PeriodViewModel    { DateTime Start; DateTime End; string Subheader; bool CanAccept => Start < End }
```

**State placement rules used above**

* Everything that the *window chrome* shows (title, version, progress, busy) lives on
  `MainViewModel`, because `IProgress`/`IStatus` was implemented by the main form.
* Everything that only one tab reads lives on that tab's view-model.
* The two pieces of **cross-tab** state are:
  1. `HasPopulation` — owned by `MainViewModel` (or a shared `SessionState`), read by
     `CollectionsTabViewModel` (tab visibility) and `DatasetViewModel` (caption).
  2. The checked data elements — owned by `CollectionsTabViewModel`, read by
     `DatasetViewModel.SaveDataPackageCommand.CanExecute`. Inject `CollectionsTabViewModel` into
     `DatasetViewModel`, or route both through a shared `SessionState` object. **Do not**
     duplicate the list.
* Persisted settings (§G.1) belong in an `IAppSettings` service injected into `MainViewModel`.
* Database access, the collector catalogue, Excel launching and CSV writing are services, not
  view-model state.

---

## I. Ambiguities and things I did not invent

1. **Left-pane width.** The `.dfm` says `splMain.Position = 293`. The screenshots show ≈ 336 px.
   The design brief says "~330 px". The splitter position is **not** persisted by the current
   code, so I cannot explain the 336 from the source — most likely the screenshots are from the
   older 19.8.14.477 layout. **Decision needed:** 293 (faithful to source) or ~320–330
   (faithful to the screenshots and the brief). I have specified 293 with `MinWidth 260`; change
   one number if the product owner prefers the wider default.
2. **`actSavePatientSelection` is unreachable.** The action, its handler
   (`fGrid.Data.SaveToSelection(Title, Comment)`), its image (index 5) and the `Save selection`
   header text all exist, but the action is not bound to any menu item, button, or toolbar in
   `MainQuickStat.dfm`. Either it was removed from the UI at some point, or a menu item was lost.
   **Decision needed:** drop it, or add it back (the obvious home is a fourth item in
   `mnuGridPopup`). I have not invented a home for it.
3. **`TfrmSaveSpec` is not cleared for "Save selection".** `actSaveDataPackage` calls `Clear()`
   first, `actSavePatientSelection` does not, so the dialog opens pre-filled with whatever was
   typed last time. Looks like a bug. Recommend always clearing; flag if the behaviour is
   intentional.
4. **Export options: check box vs. radio buttons.** See §0. I have specified the current `.dfm`
   (three radios). Confirm this is the intended shipping behaviour before the screenshots are
   used as acceptance criteria.
5. **`Time series` tab.** Recommended for removal (§C.2). Needs a product decision, not a
   technical one.
6. **Grid technology.** I have specified a WPF `DataGrid` with styles. The original is a
   hand-painted grid with a *sparse* object matrix, frozen columns, per-cell brushes and
   variable column counts decided at run time. If the datasets are large (thousands of patients
   × dozens of variables), `DataGrid` with `EnableRowVirtualization` + `EnableColumnVirtualization`
   should be benchmarked before committing; a custom `VirtualizingPanel` may be needed.
   **Not decided here.**
7. **`Do you really want to delete this package` and friends** are shown through the logging
   framework's dialog threshold, not through explicit `MessageDlg` calls. The port needs an
   explicit dialog service; the mapping of log level → dialog kind (info / warning / error /
   yes-no) is in §D.4, but the exact button sets and icons are the framework's, not the app's.
8. **The `\n` literals** in `MainQuickStat.pas` resource strings are not escape sequences in
   Delphi and are almost certainly rendered as literal backslash-n today. I have written them as
   line breaks in §D.4 on the assumption that that was the intent. Confirm.
9. **Access control for the SQL preview.** `memSourceCode` is only visible when the user holds
   `FUNC_POPULATION_SOURCE`; the default is `asDenied`. Screenshot 1 shows it visible, so at
   least some users have it. The port needs the same gate — but the access-control plumbing is
   outside this document's scope.
10. **Icons.** `lstActiveImages` / `lstDisabledImages` are 24 × 24 image lists embedded in the
    `.dfm` as binary blobs. The four I could identify from screenshot 4 and the Collections tab
    are: index 1 = tan parcel box, index 3 = green Excel “X”, index 4 = gold magic wand with
    sparkles, index 6 = floppy/save. Indices 5 (`Save patient selection`) and 7
    (`Delete this package`) are not visible in any screenshot. Either extract the bitmaps from
    the `.dfm` or substitute Fluent / Segoe MDL2 Assets glyphs — I recommend the latter for a
    modern look. Suggested code points: Package `&#xE7B8;`, Excel `&#xE8A5;` tinted green,
    Collect `&#xE9D9;` or `&#xE768;`, Save CSV `&#xE74E;`, Delete `&#xE74D;`. Confirm with the
    designer.
