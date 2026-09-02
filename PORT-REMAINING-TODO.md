# What is left — a working TODO list

Last updated: **2026-09-02**. Companion to `PORT-PLAN.md`, not a replacement: the plan is the
record of *why* everything is the way it is, and it is long. This is the short list of what still
needs doing, ranked by what would stop a release. Every line points at the plan section or document
that carries the detail.

Build and tests are green: `dotnet build QuickStat.slnx` and `dotnet test QuickStat.slnx` pass with
**2 622 tests** and zero warnings, so acceptance criterion 1 is met.

**The scope is porting.** Clear bugs get fixed; beyond that the port reproduces the Delphi. Anything
found to behave identically in both is not work — it goes under "Observed, handed on" below, so the
live list stays a list of things that actually stand between this and a release.

---

## Blocking

**1. The human parity pass — acceptance criterion 8.**
`Docs/Port/08-parity-checklist.md`, **50 items** still marked `[ ]`:

| Section | Items |
|---|---|
| 4. The dataset grid | 13 |
| 2. Population tab | 11 |
| 3. Collections tab | 7 |
| 1. Launch and connect | 6 |
| 8. Chrome, theme and shutdown | 5 |
| 7. Dialogs | 4 |
| 5. Export | 4 |

Criteria 2, 3, 5, 6 and 7 close along the way, and everything already settled by test or
measurement is marked so it can be skipped. Largest single piece of work left; Phase 6 waits on it.

**2. Acceptance criterion 5 has never been shown end to end.** Two halves are proved and they do not
meet: the recovery ran on a real cohort — 280 of 281, the 281st having none on file (§8.11 (3)) —
and a *fully identified* file was written and matched the shipped build cell for cell, 0 differing of
3 193 (§8.14). But the first run exported only PID-only variants, and **the port's side of both was
the headless harness**, not the window. Nobody has selected *Fully identified patients* in the
running application and saved a file. The untested span is radio → `IIdentificationPolicy` → grid
columns → export options → writer: unit-tested on fabricated data, never carrying a real national id.
A parity-pass item rather than development, but the criterion most entangled with R6, so it must be
checked by counting non-empty cells programmatically and deleting the file.

**3. Acceptance criterion 6 needs a sign-off decision, not more work.** 0 differing cells in 12 462
stands. *Byte-identical* is unreachable while a dataset contains the form-instance collector: the
Delphi orders its ten `FORM.*` columns by a hash-dictionary walk, and it repeats two column names
the port de-duplicates. Accept the two exceptions, or ask for a literal comparison with that element
excluded. §8.14.

> ⚠ **The 31-patient cohort behind that comparison no longer exists.** Population ProcId 23 deletes
> from `StudCase` and was run on 2026-09-01; NDV went from 287 study cases to 26, the database is
> FULL recovery with no backups. The evidence is recorded, but a *repeat* run needs a different
> cohort or a restored database. Promise accordingly.

That is the whole blocking list: one pass of manual work, one demonstration, one decision.

---

## Deployment-time, undischargeable here

**4. R10** — most `maxint`-batch collectors carry no `{IdList}` and scan whole tables, discarding
non-cohort rows client-side. Harmless on 25 patients, unknown on production volumes. Preserved
deliberately for parity; recorded as a performance follow-up.

**5. R11** — "what ships today" claims in `Docs/Port/01`, `02`, `04` and `05` are unverified except
where re-checked against the pinned ref. Confirm before relying on one.

**6. R13** — nobody has observed which branch Continua's `$Source.FastTrakDevelop` tracks. A
five-minute check for whoever has access; it would either confirm the row or overturn it.

---

## Observed, handed on — **not port work**

Three findings that are real, measured, and reproduce the Delphi exactly. Each would change
behaviour to fix, so each belongs to the product, not to this port. Listed so they are not lost, and
kept off the list above so it does not read as unfinished porting.

**The antibiotic collectors miss 87 of the 333 antibiotic codes.** The three cover 246 and overlap
on 0; the 87 fall in two halves — `QS_DRUG_ANTIBIOTIC_RESISTANCE` reaches 84 of view 3's 119,
`QS_DRUG_ANTIBIOTIC_RECOMMENDED` reaches 9 of view 1's 61 — and neither remainder falls through to
the intermediate collector, because view 2's `EXCEPT` removes them for being in view 1 or 3.
**Checked, not assumed:** both lists are character-identical to the shipping Delphi at `9f4a5ed4f`
(`EPR/QA/EPR.QA.SQL.pas:401-402`, `:417`), and the golden files pin them. The gap comes from the
Delphi writing the lists by hand instead of pointing at the `KB` views. §8.4 keeps the measurement
and the proposed fix.

**Seven live populations pass a parameter name no session can resolve**, so they fail to load — in
the Delphi too, for the same reason: `TBusiness.TryGetValue` is `IsPublishedProp` over a fixed
vocabulary, and these names are not in it. Somebody has to decide whether to supply the values, hide
the populations, or leave them. §9 R2a.

**§8.13, the 2023 SWEET field report** — date of birth and sex vanish from the extract because they
are `MetaFormItem.Expression` macros over two `NOT NULL` columns QuickStat already holds.
Root-caused, **parked by the product owner**; the fix is three lines plus one policy decision.

---

## Phase 6 — cleanup

Signed off in principle, gated on the parity pass because the walk still needs `C:\work\qs-delphi`
and the reference worktree. One trap: deleting the `.dfm` files kills
`Ui/AppBannerIconTests.TheBannerIconIsTheOneTheDelphiFormCarries`, which must be **replaced** with a
recorded SHA-256 — it is the only thing tying the banner picture to the build being ported.
`PORT-PLAN.md` §5 Phase 6.

---

## Closed on 2026-09-02

- **`J01FF%` — answered by the product owner: lincosamides are *intermediate*.** The pattern stays
  out, the set is `J01CR%`, `J01D[CDH]%`, `J01MA%`, and the caption stays
  `Antibiotika: Resistendrivende`. The port and the database now agree, so the two antibiotic
  collectors cannot both fire for one treatment — measured across all 333 codes, 0 overlaps. §8.4.
- **`ATC_A11EA` — answered, and it never needed a person.** Not a branch disagreement: 119 of 120
  refs define it identically and `'A11EA%'` has never existed in the history. The rule is `%` iff the
  code has level-5 children, and `A11EA` has none, so the exact match is correct. §8.11.
- **R2 — the `:Name` → `@Name` rewriter has now met real data.** The population catalogue was swept
  on two independent test databases: 518 and 520 rows, 319 and 322 distinct statements, and both
  reduce to the **same 44 argument lists**. 319 of 319 rewrote with zero invariant violations.
  The risk was overstated — `SqlText` is a procedure name plus an argument list, 70 characters at
  most, and `[]`, `""`, `--`, `/* */`, `::` and newlines appear **nowhere** in either catalogue.
  `QuickStat.Tests/Data/PopulationCorpusRewriteTests.cs` keeps it that way. §9 R2.
- **A quarter of every check-list row did not tick.** The row template's transparent `Border`
  carried `Padding="4,2"`, and a transparent background is hit-testable, so 6 of every 21 px
  answered to the border instead of the box — 12 px of dead band between two adjacent rows.
  `CheckListHitTargetTests` measures it pixel by pixel. §8.11 (17).
- **The Collections tab has a filter**, at the product owner's request: the population tab's box,
  immediately above the list. It hides rows only, so a ticked element the filter is hiding is still
  collected in the same export column. It adds checklist item 3.7, which is why the count above went
  up rather than down. §7.3, 05-ui-spec §B.2 item 2a.

---

## Closed on 2026-09-01, so they are not asked again

- **The `Unique name` box caps at 80** — the column width, above which the upsert silently
  overwrote another package. §8.11 (13).
- **`SemiBold` is gone** — eight uses became `Bold`, two were removed. It was a weight this
  application never rendered. §8.11 (14).
- **`QsTabItem` leaked `FontWeight` and `FontSize` into the tab's whole page** — the selected tab's
  content had been drawing at 13 px instead of 12 since the theme was written. §8.11 (15).
- **Dialogs re-centre on their owner**, so the first one of a session is no longer 27 px off, and
  **the buttons read `OK` then `Cancel`** — the platform order. §7.3, checklist 7.1.
- **R1, the `Encrypt` / `TrustServerCertificate` default — closed for this port, on scope.** There
  is no house setting to match: `FastTrak.exe` connects through the UDL, which carries no encryption
  keywords, and at least one .NET service sets `Encrypt=false`. The connection-level posture across
  the estate is a platform question with a different owner, not one this port settles. §8 (2),
  §9 R1.
