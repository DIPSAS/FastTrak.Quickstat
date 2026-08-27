# 07 — Phase 3 UI contracts: what wave 2 inherits from step 3.1

Status: **complete for wave 1.** Produced by Phase 3, step 3.1 (PORT-PLAN.md §5).
Branch: `feature/dotnet`.

This is the document a **wave-2** agent — step 3.2, 3.3, 3.4 or 3.6 — consults before writing a line
of XAML. It is the UI half of `Docs/Port/06-contracts.md`, and it follows the same rule: everything
listed here exists in the tree, compiles with zero warnings, and has XML documentation.

Read `Docs/Port/05-ui-spec.md` for *what the window looks like*. Read this for *which files are
yours, which types you consume, and which traps have already been paid for*.

---

## 0. How to use this document

1. Find your step in §2. Those files, and only those files, are yours to edit.
2. Everything else under `QuickStat.App/` is a **read-only dependency**. Reference it freely; do not
   edit it. If you need a change, say so in your report — do not make it yourself.
3. §3 is the shared type surface: the six services and the state object every step touches.
4. §5 lists the decisions step 3.1 took that constrain you, each with what it would cost to reverse.
5. §6 is the build and test traps that were actually hit, not the ones that were imagined.

### The ownership rule

**Every file has exactly one owner, and ownership follows the folder** — except in `Views/` and
`ViewModels/`, which are flat by design (`05-ui-spec.md` §H.1) and are therefore owned **per file**.
Each wave-2 file already exists as a compiling stub whose header comment names its owner and lists
what is left to do. Start by reading that comment.

| Folder / file | Owner |
|---|---|
| `QuickStat.App/Theme/**` | **3.1** |
| `QuickStat.App/Converters/**` | **3.1** |
| `QuickStat.App/Services/**` *except the two rows below* | **3.1** |
| `QuickStat.App/Services/WpfNotificationPresenter.cs` | **3.6** |
| `QuickStat.App/Services/WpfPeriodPrompt.cs` | **3.6** |
| `QuickStat.App/Controls/SectionHeader.cs` | **3.1** |
| `QuickStat.App/Controls/Dataset/**` | **3.5** |
| `QuickStat.App/MainWindow.xaml{,.cs}`, `App.xaml{,.cs}` | **3.1** |
| `QuickStat.App/Logging/**` | Phase 0 — frozen |
| `Views/AppBannerView.xaml{,.cs}` | **3.1** |
| `Views/DatasetTabView.xaml{,.cs}` | **3.1** |
| `Views/PopulationTabView.xaml{,.cs}` | **3.2** |
| `Views/PopulationPickerView.xaml{,.cs}` | **3.2** |
| `Views/CollectionsTabView.xaml{,.cs}` | **3.3** |
| `Views/PackagesTabView.xaml{,.cs}` | **3.4** |
| `Views/Dialogs/SaveSpecDialog.xaml{,.cs}` | **3.6** |
| `Views/Dialogs/PeriodDialog.xaml{,.cs}` | **3.6** |
| `Views/Dialogs/BusyOverlayView.xaml{,.cs}` | **3.6** |
| `ViewModels/MainViewModel.cs`, `DatasetViewModel.cs`, `DataHint.cs` | **3.1** |
| `ViewModels/PopulationTabViewModel.cs`, `PopulationPickerViewModel.cs`, `PopulationViewModel.cs` | **3.2** |
| `ViewModels/CollectionsTabViewModel.cs`, `DataElementViewModel.cs` | **3.3** |
| `ViewModels/PackagesTabViewModel.cs`, `PackageViewModel.cs` | **3.4** |
| `ViewModels/SaveSpecViewModel.cs`, `PeriodViewModel.cs`, `BusyOverlayViewModel.cs` | **3.6** |
| `QuickStat.Tests/Ui/Shell/**`, `Ui/Dataset/**`, `Ui/Theme/**`, `Ui/Services/**`, `Ui/Converters/**` | **3.1** |
| `QuickStat.Tests/Ui/StaTestRunner.cs`, `StaTestRunnerTests.cs`, `DependencyPropertyRegistrationTests.cs` | **shared, read-only** — extend, do not copy |

Namespaces follow folders (`IDE0130` is enforced as an error), so
`QuickStat.App/Views/Dialogs/PeriodDialog.xaml.cs` is `QuickStat.Views.Dialogs.PeriodDialog`.

### Nothing outside `QuickStat.App/` and `QuickStat.Tests/Ui/` is yours

- **`QuickStat.Core/**` is finished** and verified by the Phase 2 suite. If something there is wrong,
  report it.
- **`.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.gitignore`,
  `global.json`, `QuickStat.slnx`, `app.manifest`** — report and work around; do not edit. Step 3.1
  needed none of them: `.xaml` under any folder is globbed as a `Page` automatically, and the
  application icon is already a resource at
  `pack://application:,,,/QuickStat;component/Assets/QuickStat_Icon.ico`.
- **`PORT-PLAN.md`, `Docs/Port/01`–`06`** — read-only. This file, `07`, is 3.1's; a wave-2 step that
  needs to record something adds it to its own report.

---

## 1. What already works, so you do not rebuild it

`QuickStat.exe` starts, renders the full shell with the real theme, and exits 0. Verified by
rendering the composed window off-screen and by launching the executable.

- The banner: icon, wordmark, `version <n>` in two colours, and the Progress block.
- The three left-hand tabs, with **Collections hidden and disabled until a population is loaded**.
- The splitter, at 293 with a minimum of 260, its position persisted.
- The right-hand pane: the dataset caption bar with *Wide columns*, the grid host, the floating hint
  and the grid context menu.
- Window geometry restore/save with the off-screen guard rail.
- The busy overlay, driven by `IShellProgress.IsBusy`.

What is *not* there: every wave-2 behaviour, plus the grid's own rendering, which is step 3.5's.

---

## 2. The file map

### 3.2 — Population tab + embedded picker

| File | State | What is left |
|---|---|---|
| `Views/PopulationTabView.xaml{,.cs}` | layout skeleton | fill the combo box; the selection triggers the connect |
| `Views/PopulationPickerView.xaml{,.cs}` | layout skeleton | row template + expansion, live filter, empty state, double click / `Enter`, SQL-preview gate |
| `ViewModels/PopulationTabViewModel.cs` | stub | `Projects` from `IConnectionCatalog`, sorted, **nothing preselected**; `SelectedProject` awaits `IConnectionCoordinator.ConnectAsync` |
| `ViewModels/PopulationPickerViewModel.cs` | stub | catalogue load, `ICollectionView` filter, `PreparePopulationCommand` |
| `ViewModels/PopulationViewModel.cs` | stub | `IsExpanded` |

### 3.3 — Collections tab and the collect run

| File | State | What is left |
|---|---|---|
| `Views/CollectionsTabView.xaml{,.cs}` | layout skeleton, radios and timestamp box **already wired to their shared homes** | the list's collecting-highlight and scroll restore |
| `ViewModels/CollectionsTabViewModel.cs` | stub; `Identification` and `ExportTimestamps` are real pass-throughs | fill `DataElements`, implement `CollectDataCommand` |
| `ViewModels/DataElementViewModel.cs` | stub | on `IsChecked`: `NotifyCanExecuteChanged` **and** `IShellWorkspace.SetCheckedCollectorNames` |

### 3.4 — Packages tab and the replay

| File | State | What is left |
|---|---|---|
| `Views/PackagesTabView.xaml{,.cs}` | layout skeleton with toolbar and context menu | two-line row template |
| `ViewModels/PackagesTabViewModel.cs` | stub | load/refresh, trimmed uppercase filter, confirmed delete, the replay, **and subscribing to `DatasetViewModel.SaveDataPackageRequested`** |
| `ViewModels/PackageViewModel.cs` | stub | nothing structural |

### 3.6 — Dialogs and the two WPF seams

| File | State | What is left |
|---|---|---|
| `Views/Dialogs/SaveSpecDialog.xaml{,.cs}` | layout skeleton, accept path works | §E metrics and banner |
| `Views/Dialogs/PeriodDialog.xaml{,.cs}` | layout skeleton | §D.5 calendars, valid/invalid line, settings round trip |
| `Views/Dialogs/BusyOverlayView.xaml{,.cs}` | works, placeholder visual | the real visual, and cancel |
| `ViewModels/SaveSpecViewModel.cs`, `PeriodViewModel.cs`, `BusyOverlayViewModel.cs` | stubs | see each header |
| `Services/WpfNotificationPresenter.cs` | **works** — `MessageBox`, correct threading and answers | replace the rendering with the themed dialog; keep every behavioural rule in the header |
| `Services/WpfPeriodPrompt.cs` | **stub — always cancels**, and says so to the user | show `PeriodDialog`; key remembered periods with `PeriodSettingsKey.For`, never the raw SQL |

---

## 3. The shared surface

Everything in this section is a singleton unless it says otherwise. Inject it; do not construct it.

### 3.1 `IShellWorkspace` — the cross-tab state

`QuickStat.App/Services/IShellWorkspace.cs`. The Delphi kept all of this in `TfrmQuickStat`'s
private fields; `05-ui-spec.md` §H.2's "State placement rules" says not to copy it into three
view-models, so it lives here.

```csharp
PersonMatrix          Matrix                  { get; }   // the one instance, same as the container's
Population?           Population              { get; }
bool                  HasPopulation           { get; }   // Population is not null AND Matrix.Rows.Count > 0
bool                  HasData                 { get; }   // Matrix.HasData — columns, not rows
int                   RowCount                { get; }
int                   ColumnCount             { get; }
IReadOnlyList<string> CheckedCollectorNames   { get; }
bool                  ExportTimestamps        { get; set; }

event EventHandler? PopulationChanged;
event EventHandler? DataChanged;
event EventHandler? CollectionsTabRequested;

void SetPopulation(Population? population);
void SetCheckedCollectorNames(IEnumerable<string> names);
void NotifyDataChanged();
void RequestCollectionsTab();
```

**§H.2 names two pieces of cross-tab state; there are three.** It lists `HasPopulation` and the
checked data elements, and misses `ExportTimestamps` — the *Export timestamp for every data element*
check box, which sits on the Collections tab (§B.2 item 9) and is read at export time by the Dataset
tab's two save commands (§D.1). It is here for the same reason as the other two.

The identification mode is deliberately **not** here: it already has one home,
`QuickStat.Domain.Anonymisation.IIdentificationPolicy` in Core.

**Ordering contract — get this wrong and `HasPopulation` reads the previous cohort.** `PersonMatrix`
is a plain mutable object with no change notification, so the workspace cannot observe it:

```csharp
matrix.Clear();                             // or ClearPopulation(); see below - it must come FIRST
matrix.SortBy = MatrixSortOrder.PersonId;   // throws while the matrix is locked
matrix.PreparePopulation(patients);
workspace.SetPopulation(population);        // now Rows.Count is right
workspace.RequestCollectionsTab();          // both entry points - see below
```

and a collect run ends with `matrix.Lock(); workspace.NotifyDataChanged();`.

**Two corrections, both found during wave 2 and both from agents that were handed the wrong version
of this block. Trust the code above, not any copy of it you have already read.**

**(1) The clear has to come first, and this snippet used to omit it.** `SortBy`'s setter throws
`InvalidOperationException` when `IsLocked`, and the check sits *before* the `_sortBy == value`
short-circuit, so even a no-op assignment throws. A collect run locks. `PreparePopulation` does
unlock — via `Clear()` — but only *after* you have assigned `SortBy`, so it does not save you. As
written before, the sequence worked once and threw the second time: load a population, collect, load
another. Both steps 3.2 and 3.4 hit it and added regression tests.

**(2) The package replay *does* switch to the Collections tab.** This section previously said it did
not, and both 3.2 and 3.4 independently traced the opposite. `PreparePackagedSelection` calls
`frmPopulations.TrySelect(procId, ALoadIt := true, …)` (`MainQuickStat.pas:789`); with `ALoadIt`
true, `TrySelect` calls `PopulationRequested` (`EPR.VclFrame.Populations.pas:195`); that walks
`fObservers` calling `AfterPopulationSelect` (`:217-218`); `TfrmQuickStat` registered itself as one
(`MainQuickStat.pas:288`); and `AfterPopulationSelect` ends with
`pgSelections.ActivePage := tbsDataElements` (`:541`). Control then returns to
`PreparePackagedSelection`, which calls `LoadPopulationIntoGrid` **a second time** — so the Delphi
also loads the cohort twice. The port switches the tab (parity) and loads once (the double load is a
plain waste, not a behaviour anyone can see).

### 3.2 `IShellProgress` — the Progress block and the busy flag

`QuickStat.App/Services/IShellProgress.cs`. Also registered as
`IProgress<OperationProgress>`, which is what Core's login pipeline and collector runner take; both
faces resolve to the same instance.

```csharp
string Header { get; }      // "Progress". Static in practice — §G.6
string Info   { get; }      // "Program is idle" → status text
double Percent { get; }
bool   IsError { get; }     // ProgressInfo shown in red
bool   IsBusy  { get; }

void SetInfo(string info);
void Fail(string message);
void Done();                       // 100 % + "Task completed"
void Reset();
IDisposable BeginOperation(string info);
```

`BeginOperation` **counts**, so nested scopes do not clear the busy flag early — the package replay
runs a collect inside its own wait cursor, which is exactly why the Delphi saves and restores
`Screen.Cursor` rather than assigning `crDefault`. Use it in a `using`; do not assign `IsBusy`.

`Report` is safe from any thread: it marshals through `IUiDispatcher` itself.

### 3.3 `IConnectionCoordinator` — the whole of `SelectConnection`

`QuickStat.App/Services/IConnectionCoordinator.cs`.

```csharp
Task<SessionContext> ConnectAsync(QuickStatConnection connection, CancellationToken ct = default);
Task DisconnectAsync(CancellationToken ct = default);
```

**Step 3.2 must call this and not `ISessionService` directly.** It does the status text, the busy
scope, the login *and* `ICaptionLoader.LoadAsync` — and that last one is not optional: nothing else
in the application calls it, and without it every lab column falls back to its raw variable name
with an empty header tooltip (PORT-PLAN.md §8.8, "nobody loaded the captions"). Captions are loaded
before the call returns, so a user who connects and immediately collects still gets titled columns.

### 3.4 `IUiDispatcher`, `IFileDialogService`, `IProcessLauncher`, `IApplicationInfo`, `IMonitorLayout`

Small seams, each with one job and each so a view-model can be unit-tested.

- **`IUiDispatcher`** — `IsOnUiThread`, `Invoke`, `Post`, `InvokeAsync`. Never touch `Dispatcher`
  directly, and never dereference `Application.Current` without a null check: it is `null` under
  test.
- **`IFileDialogService.ShowSaveFileDialog(SaveFileRequest)`** → path or `null` for cancel.
  `SaveFileRequest.DatasetCsv` is the Delphi's dialog verbatim.
- **`IProcessLauncher.OpenWithShell(path)`** — `ShellExecute` and return; no `Sleep(50)` pump.
- **`IApplicationInfo`** — `Title`, `ProductName`, `Version`.
- **`IMonitorLayout`** — work areas for the off-screen guard rail.

### 3.5 `IWindowStateService` — persisted geometry, and two additions

`QuickStat.App/Services/IWindowStateService.cs`. Step 3.1 uses it; step 3.2 may want
`GetLastDatabase` / `SetLastDatabase`, which are provisioned but **not** switched on — see §5.

### 3.6 `SectionHeader` — the teal bar

`QuickStat.App/Controls/SectionHeader.cs`, namespace `QuickStat.Controls`. A
`HeaderedContentControl`: `Header` is the heading string and `Content` is an optional right-aligned
slot, one point smaller and already white.

```xml
<q:SectionHeader Header="Select data elements" />

<q:SectionHeader Header="{Binding CaptionText}">
    <CheckBox FlowDirection="RightToLeft" IsChecked="{Binding WideColumns, Mode=TwoWay}">
        <TextBlock FlowDirection="LeftToRight" Text="Wide columns" />
    </CheckBox>
</q:SectionHeader>
```

The paired `FlowDirection` is how the caption goes to the **left** of the box (Delphi
`Alignment = taLeftJustify`) while staying a single, clickable `CheckBox` with a usable automation
name. `cbSimpleView` in the population frame needs the same trick.

No `Style` attribute is needed: there is an implicit style based on the keyed `QsSectionHeader`.

### 3.7 Converters

`QuickStat.App/Converters/`. All four are **markup extensions as well as converters**, so XAML uses
them without a resource key and there is deliberately no converter dictionary to keep in step:

```xml
Visibility="{Binding HasPopulation, Converter={conv:BoolToVisibilityConverter}}"
IsChecked="{Binding Identification, Converter={conv:EnumToBooleanConverter},
                    ConverterParameter={x:Static anon:PersonIdentification.Full}}"
```

| Converter | Notes |
|---|---|
| `BoolToVisibilityConverter` | `Invert`, and `Collapse` (default `true`). A non-boolean is false, so a late `DataContext` collapses rather than throws |
| `EnumToBooleanConverter` | `ConvertBack` returns `Binding.DoNothing` when *un*checking — without that the outgoing radio overwrites the incoming one and the group never changes. Parameter matching is case-**sensitive** |
| `NullToBooleanConverter` | one-way; empty string counts as absent |
| `RgbConverter` | `QuickStat.Domain.DataPoints.Rgb` → `Color` or a **cached, frozen** `Brush`. The only bridge between Core's WPF-free colours and the screen |

### 3.8 Icons

`QuickStat.Theme.SegoeIcons` — six `const string` code points, used as
`Text="{x:Static theme:SegoeIcons.Package}"` with `Style="{StaticResource QsIconGlyph}"`, which
supplies `Segoe MDL2 Assets`. All six were verified present in the font's
`CharacterToGlyphMap`.

---

## 4. The theme

`Theme/QuickStat.Brushes.xaml` (28 brushes) and `Theme/QuickStat.Styles.xaml` (18 named styles),
transcribed key for key from `05-ui-spec.md` §F.4, plus seven typography resources from §F.3
(`QsFontFamily`, `QsCodeFontFamily`, `QsIconFontFamily`, `QsFontSize`, `QsHeaderFontSize`,
`QsSmallFontSize`, `QsWordmarkFontSize`) and one extra style, `QsIconGlyph`.

**Do not add a brush without adding it to §F.4 as well.** `Ui/Theme/ThemeResourceTests.cs` asserts
the inventory in **both** directions: every §F.4 key resolves with the right type, colour and frozen
state, and every brush in the theme appears in §F.4. That test is the only thing between a typo and
a blank window, because `{StaticResource QsTelBrush}` compiles fine and throws at run time.

Two structural facts:

- **`QuickStat.Styles.xaml` merges `QuickStat.Brushes.xaml` itself**, and `App.xaml` merges only the
  styles. `StaticResource` is resolved when a dictionary is *parsed*, so a styles file that relied
  on the brushes having been merged first could not be loaded on its own — which is exactly what the
  theme test does. Merging both from `App.xaml` would instead give every brush key two instances.
- Every brush is `po:Freeze="True"`. They are shared application-wide; freezing turns a stray
  mutation into an exception instead of a rendering mystery.

The `QsDataGrid*` styles exist **for inventory completeness only**. The dataset grid is the custom
`MatrixGrid`, which takes its colours from its own brush dependency properties — the Dataset tab
binds those to the same `Qs*` brushes. Nothing in the port uses a `DataGrid`; do not start.

---

## 5. Decisions step 3.1 took that constrain you

Each of these was open in `05-ui-spec.md` §I or unstated. All are reversible; the cost is given.

| # | Decision | Why | Reversal cost |
|---|---|---|---|
| a | **Left pane 293, minimum 260** (§I.1) | The `.dfm` says `splMain.Position = 293`; the screenshots showing ≈336 are of the 2019 build whose layout differs elsewhere too (§0). The spec's own recommendation is 293/260 | `MainViewModel.DefaultSplitterPosition`, one number — and a user who prefers 330 now drags once, because the position is persisted |
| b | **Segoe MDL2 Assets glyphs, not the `.dfm` image blobs** (§I.10) | What the spec recommends. They scale with DPI, take their colour from the theme, need no `<Resource>` in a `.csproj` step 3.1 may not edit — and two of the eight blobs are not visible in any screenshot, so half an extraction would have been guesswork | six constants in `SegoeIcons` |
| c | **The right pane is a plain content host, not a `TabControl`** (§C.2) | *Time series* is an empty, permanently disabled `TRzTabSheet` with no code touching it. PORT-PLAN.md §7.1 drops it | re-add a `TabControl` in `MainWindow.xaml` |
| d | **Real newlines, not literal `\n`** (§I.8) | The Delphi resource strings contain the two-character sequence, which single-quoted Pascal does not process — so today users see a backslash and an n. Core's `PiiRedactor.ForDisplay` already converts them | none; it is what `IUserNotifier` does |
| e | **`actSavePatientSelection` is not ported, so §I.3 does not arise** | It is bound to no menu item, button or toolbar; PORT-PLAN.md §7.1 removes it. Confirmed: the port has no caller, and `SaveSpecViewModel` is only ever given the header `Save specification` | add a fourth `mnuGridPopup` item and a second header constant |
| f | **The splitter position and the last-used database are persisted** (§G.1's recommended additions) | Both are obvious wins and §G.1 asks for them to be flagged rather than slipped in | delete two keys from `WindowStateService` |
| g | **The last database is stored but not auto-connected** | Reconnecting on start-up is a behaviour change, not a convenience — it would hit the database before the user asked. Step 3.2 may *preselect*; it should not connect | 3.2's decision, not made here |
| h | **Nothing stored ⇒ the window stays centred** | The Delphi reads `Left`/`Top` with a default of **0**, so a first run opens hard against the top-left corner. `Restore` returns `null` instead | one branch in `WindowStateService.Restore` |
| i | **A window closed minimised reopens Normal** | The state is stored and restored faithfully; only *this run* is promoted, because starting minimised looks like a failure to launch | one line in `MainWindow.ApplyStoredPlacement` |
| j | **The off-screen guard rail uses `SystemParameters`, not per-monitor enumeration** | WPF exposes no monitor list. The real work areas need either a Windows Forms reference or `EnumDisplayMonitors` through P/Invoke, and both mean a `.csproj` change step 3.1 may not make. It catches the case §G.1 exists for — a monitor that has been unplugged — but not a window parked in the gap of an L-shaped arrangement | one class behind `IMonitorLayout` |
| k | **Export is refused when the matrix is empty or unlocked** | Both are reachable, because *Save dataset to CSV* is always enabled and *Open in Excel* latches on. The Delphi writes `(not ready)` into every cell, or a phantom `"nil"` row | delete `DatasetViewModel.EnsureExportable` |
| l | **`SaveDataPackageCommand` is an event, not an implementation** | Its caption, home and enable rule are Dataset-tab concerns; showing the dialog, building the `PackagedSelection` and refreshing the list are the Packages tab's. Step 3.4 subscribes to `DatasetViewModel.SaveDataPackageRequested` | — |
| m | **`App.Report` shows at most one crash dialog per process** | One startup fault reaches it three times over, and each used to stack another modal box somebody had to dismiss before the process could die. It stays a `MessageBox` rather than going through `IUserNotifier`, because it must work when the container is half-built | one flag |
| n | ~~**The version string is whatever the assembly reports**~~ | **Settled** at the start of Phase 4 (PORT-PLAN.md §8.9 b): `<Version>26.0.0.0</Version>` in `Directory.Build.props`. It was left unset on purpose while this row stood — inventing one would have put a false version in front of users and in every log line — because it is a packaging decision and it belonged to the product owner, who made it. One property covers the banner, the file properties, the `@AppVer` sent to `dbo.AddSession` and the start-up log line | — |

### Two things settled elsewhere that wave 2 inherits verbatim

**The two list filters are not the same filter** (PORT-PLAN.md §8.8 (i)). Both case-fold and then do
an *ordinal* substring search — Delphi `Pos`, not a collation — but they fold in opposite directions
and disagree about trimming:

| | Population list (3.2) | Packages list (3.4) |
|---|---|---|
| Case fold | `ToLower(CultureInfo.CurrentCulture)` **both sides** | `ToUpper(CultureInfo.CurrentCulture)` **both sides** |
| Filter trimmed | **no** | **yes** |
| Compare | `StringComparison.Ordinal` | `StringComparison.Ordinal` |
| Empty filter | matches everything | matches everything |
| Matched against | `Population.SearchText` | `RowId ⇥ Title ⇥ Comment ⇥ Pop#<n>` |

Never `StringComparison.CurrentCultureIgnoreCase` — that is a collation and folds more than `Pos`
does. And `TPopulation.Match` is **dead code**: it uppercases and would answer differently, but
`TObjectListView.AfterUpdate` reaches it only through `IMatchable`, which `TPopulation` does not
implement.

**"Collector order" means the check list's order, which is alphabetical by title** (PORT-PLAN.md
§6). `cbDataCollector.Sorted := true` is set in `FormShow` before `AfterLogin` fills the list, and
the collect loop walks `Items` from index 0 — so that walk *is* the column order of every export.
Sort with **`StringComparer.CurrentCultureIgnoreCase`**. Registry order decides which collectors
exist, not where their columns land.

> **Corrected after this document was written.** It originally said `StringComparer.Ordinal`, "which
> is what keeps the `^ `-prefixed demographic collectors first". Ordinal does the opposite: `'^'` is
> U+005E, above `'Z'` and below `'a'`, and every other title begins with a capital, so it sorts all
> eleven `^ ` elements **last**. `Sorted := true` is `LBS_SORT`, i.e.
> `CompareStringW(LOCALE_USER_DEFAULT, NORM_IGNORECASE)` — linguistic and case-insensitive. Step 3.3
> caught this and pinned it in `Ui/Collections/CollectorOrderTests.cs`. See PORT-PLAN.md §6.
>
> Note this is the opposite of the two **list filters** above, which really are ordinal: those model
> Delphi's `Pos`, a byte scan, while this models a Win32 list box's locale collation. Same file, two
> different mechanisms; do not unify them.

---

## 6. Build and test traps that were actually hit

- **`FrameworkPropertyMetadata` has no constructor taking only `FrameworkPropertyMetadataOptions`.**
  `new FrameworkPropertyMetadata(SomeFlags)` binds to the `(object defaultValue)` overload and boxes
  the flags *as the default value*. It compiles without a warning and then throws
  `ArgumentException: Default value type does not match type of property 'X'` out of the type
  initialiser the first moment XAML mentions the control — as a `XamlParseException` naming the
  window, not the control. Use `(object defaultValue, FrameworkPropertyMetadataOptions flags)`.
  `Ui/DependencyPropertyRegistrationTests.cs` now sweeps every `DependencyObject` in the assembly
  for this; extend it when you add a control.
- **An unresolvable `<see cref=…>` is a build error** (`CS1574`). A `cref` to a type in another
  namespace needs the full name — `QuickStat.ViewModels.MainViewModel`, not `MainViewModel` — and a
  `cref` to a member *inherited from* an interface you extend (`IProgress<T>.Report`) does not
  resolve at all: write `<c>Report</c>`.
- **An unused `using` is a build error** (`IDE0005`), and the WPF implicit-usings set is the reduced
  one — **no `System.IO`**. Add it per file.
- **`Application.LoadComponent(new Uri("/QuickStat;component/Theme/…xaml", UriKind.Relative))` works
  under test** with no `Application` instance, on an STA thread. That is how the theme test loads
  the dictionary. A `pack://` absolute URI is not needed.
- **`Application.Current` *may* be `null` under test**, and WPF allows one `Application` per
  `AppDomain`. Never dereference it without `?.`. Every shell service that needed it — the
  dispatcher, the file dialog's owner, the notification presenter's owner — has a null branch, which
  is also what makes the whole container resolvable headlessly. Since Phase 5 the suite does own
  **one**: `QuickStat.Tests/Ui/WpfApplicationFixture.cs` puts the shipped `App` on a dedicated
  background STA thread with `ShutdownMode.OnExplicitShutdown`, so that a view which resolves the
  theme through `Application.Current` can be constructed at all (PORT-PLAN.md §8.10 (a)). Two
  consequences. **Do not create a second one** — ask for `WpfApplicationFixture` through
  `[Collection(WpfApplicationCollection.Name)]`. And **do not write `Assert.Null(Application.Current)`**:
  nothing can un-set it once it exists, so that assertion passes or fails on collection order. To
  say "this window carries the theme itself", read the window's own
  `Resources.MergedDictionaries` — that indexer never falls back to the application.
- **The test assembly runs sequentially** (`[assembly: CollectionBehavior(DisableTestParallelization = true)]`,
  declared in the fixture file with its reason). `Application` registers and unregisters every
  `Window` on unsynchronised collections, from whichever apartment created it, so parallel window
  construction is an unsynchronised list mutation — and a corrupted list throws out of every later
  `Window` constructor, not just the racing one. Measured cost of serialising: 5 s → 8 s.
- **A collapsed `TabItem` that is still selected leaves the `TabControl` blank.** `MainViewModel`
  moves the selection back to Population when `HasPopulation` goes false. There is no Delphi
  equivalent, because `TRzPageControl` picks a new active page itself.
- **`ItemsSource` + `AlternationCount` alternation is inverted** relative to the usual WPF
  convention: §B.1.1 records `AlternationIndex 0` as the **tinted** row. `QsPopulationItem` already
  does this.
- **`Assert.Empty(x.Where(...))` is a build error** (`xUnit2029`). Materialise and compare.
- **Every test must be culture-independent.** This machine is `nb-NO`. Step 3.1's suite was swept
  under a forced `nb-NO` *and* `en-US` and passes under both; do the same before reporting.
- Tests are **xUnit v2 on VSTest**. Do not change the stack.
- Source files are UTF-8 **without** a BOM and contain Norwegian characters (`Født`,
  `Fødselsnummer`, `Angi periode`, `Avbryt`).

---

## 7. What step 3.1 could not verify, and what it needs

- **The floating data hint is wired end to end but invisible**, because
  `MatrixGrid.TryGetCellBounds` returns `false` until step 3.5 implements it. That is the declared
  contract — a scrolled-out cell returns `false` and the caller hides the hint — so nothing needs to
  change on either side. The text assembly and the anchor arithmetic are unit-tested against the
  view-model directly.
- ~~**No database.**~~ **Settled in Phase 5.** Against `EFT00028_TEST_020`: the connect path, the
  caption load and the population load all run, all 213 data elements build, and all 213 collectors
  execute. `PORT-PLAN.md` §8.11.
- ~~**Three §F.1 palette entries do not match the pinned parity baseline.**~~ **Two settled in
  Phase 5, by measurement; the third is still open.** `QsCodeBrush` is now `#888888` and
  `QsCategoryBrush` `#894605`, sampled off the running `22.12.21.547`. `QsCurrentCellBrush` stays
  `#FFFBD4` because nothing could be measured — see `PORT-PLAN.md` §8.9 (a). The original analysis,
  which the measurement confirmed, was: `05-ui-spec.md` §F.1 gives
  `clCodeColor = $00A4294B` (`#4B29A4` purple), `clStatusTextColor = $00822EB8` (`#B82E82` fuchsia)
  and `clFocusedSelectionColor = $00D4FBFF` (`#FFFBD4` pale yellow). Those are this repository's
  `FastTrak\` copy, i.e. `develop_old`. On `origin/tarmscreening/develop` — the ref PORT-PLAN.md
  §2.1 pins — commit `98f493bbc` (2022-09-29, *"Mindre retninger"*) changed them to `$00888888`
  (`#888888` grey), `clMandatoryGeometryFill` (`#894605` brown) and `clSelectedBk` (`#C8D9E9` pale
  blue). It is on **both** tarmscreening refs and predates the shipped `v22.12.21.547` by three
  months, so the 2022 binary showed grey, brown and pale blue. The screenshots that §F.1 pixel-checks
  against are of `19.8.14.477`, from 2019, which is why they agree with the older constants. Step
  3.1 transcribed §F.4 as instructed and as the screenshots show; this is exactly the class of error
  PORT-PLAN.md R11 warns about, and it is a decision for a human. Reversal cost: three hex values in
  `QuickStat.Brushes.xaml`, three in `ThemeResourceTests`, three in §F.4.
- **`05-ui-spec.md` §F.4 says `QsSplitter` is `Width=8`; §A.2 and `splMain.SplitterWidth` say 9.**
  The `.dfm` wins; the style uses 9.
