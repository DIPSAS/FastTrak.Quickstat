# What is left — a working TODO list

Last updated: **2026-09-03**. Companion to `PORT-PLAN.md`, not a replacement: the plan is the
record of *why* everything is the way it is, and it is long. This is the short list of what still
needs doing, ranked by what would stop a release. Every line points at the plan section or document
that carries the detail.

**Nothing finished appears here.** A closed item is removed outright, not struck through or moved to
a tail section — the plan is where the record of a closed question lives, and a list of what is left
should be readable as exactly that.

Build and tests are green: `dotnet build QuickStat.slnx` and `dotnet test QuickStat.slnx` pass with
**2 633 tests** and zero warnings, so acceptance criterion 1 is met. Two more are *skipped* by
design — `Live/`, which needs a server and says so.

**The scope is porting.** Clear bugs get fixed; beyond that the port reproduces the Delphi. Anything
found to behave identically in both is not work — it goes under "Observed, handed on" below, so the
live list stays a list of things that actually stand between this and a release.

---

## Blocking

**1. The human parity pass — acceptance criterion 8.**
`Docs/Port/08-parity-checklist.md`, **47 items** still marked `[ ]`:

| Section | Items |
|---|---|
| 4. The dataset grid | 12 |
| 2. Population tab | 11 |
| 1. Launch and connect | 6 |
| 3. Collections tab | 5 |
| 5. Export | 5 |
| 7. Dialogs | 4 |
| 8. Chrome, theme and shutdown | 4 |

Criteria 2, 3, 5 and 7 close along the way, and everything already settled by test or measurement is
marked so it can be skipped. Largest single piece of work left; Phase 6 waits on it.

That is the whole blocking list: one pass of manual work.

> ⚠ **Pick the cohort deliberately.** ProcId 282 *"Diagnoseår mangler"* is the suggested one and now
> returns **25 patients**, not the 31 §8.14 was measured on. ProcId 23 deletes from `StudCase` and
> is what shrank it on 2026-09-01, on a database in FULL recovery with no backups — **do not pick
> it**.

---

## Deployment-time, undischargeable here

**2. R10** — most `maxint`-batch collectors carry no `{IdList}` and scan whole tables, discarding
non-cohort rows client-side. Harmless on 25 patients, unknown on production volumes. Preserved
deliberately for parity; recorded as a performance follow-up.

**3. R11** — "what ships today" claims in `Docs/Port/01`, `02`, `04` and `05` are unverified except
where re-checked against the pinned ref. Confirm before relying on one.

**4. R13** — nobody has observed which branch Continua's `$Source.FastTrakDevelop` tracks. A
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
