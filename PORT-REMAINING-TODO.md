# What is left — a working TODO list

Last updated: **2026-09-01**. Companion to `PORT-PLAN.md`, not a replacement: the plan is the
record of *why* everything is the way it is, and it is long. This is the short list of what still
needs doing, ranked by what would stop a release. Every line points at the plan section or document
that carries the detail.

Build and tests are green: `dotnet build QuickStat.slnx` and `dotnet test QuickStat.slnx` pass with
**2 589 tests** and zero warnings, so acceptance criterion 1 is met.

---

## Blocking

**1. The human parity pass — acceptance criterion 8.**
`Docs/Port/08-parity-checklist.md`, **49 items** still marked `[ ]`:

| Section | Items |
|---|---|
| 4. The dataset grid | 13 |
| 2. Population tab | 11 |
| 3. Collections tab | 6 |
| 1. Launch and connect | 6 |
| 8. Chrome, theme and shutdown | 5 |
| 7. Dialogs | 4 |
| 5. Export | 4 |

Criteria 2, 3, 5, 6 and 7 close along the way, and everything already settled by test or
measurement is marked so it can be skipped. Largest single piece of work left; Phase 6 waits on it.

**2. R2 has never been discharged — the `:Name` → `@Name` rewriter has not been run over real
population SQL.** Population statements are author-written text stored *in the database*: arbitrary
SQL with literals, `[]`, `""`, `--`, `/* */` and `::` the scanner must skip. The scanner is built
and unit-tested; nobody has swept the production corpus and diffed before against after. Cheap, and
it reads SQL text rather than patient rows. `PORT-PLAN.md` §9 R2, `Docs/Port/01-data-access.md`
§7.5.

**3. Acceptance criterion 5 has never been shown end to end.** "Fully identified patients" recovers
280 of 281 national IDs through the port's own services (§8.11 (3)), but has not been driven through
the running application to a file. It is the criterion most entangled with R6 (privacy), so a
services-level proof is weaker than it looks.

**4. Acceptance criterion 6 needs a sign-off decision, not more work.** 0 differing cells in 12 462
stands. *Byte-identical* is unreachable while a dataset contains the form-instance collector: the
Delphi orders its ten `FORM.*` columns by a hash-dictionary walk, and it repeats two column names
the port de-duplicates. Accept the two exceptions, or ask for a literal comparison with that element
excluded. §8.14.

> ⚠ **The 31-patient cohort behind that comparison no longer exists.** Population ProcId 23 deletes
> from `StudCase` and was run on 2026-09-01; NDV went from 287 study cases to 26, the database is
> FULL recovery with no backups. The evidence is recorded, but a *repeat* run needs a different
> cohort or a restored database. Promise accordingly.

---

## Questions for a person, not a machine

**5. `J01FF%` — release-blocking, and the only one that is.** Does lincosamide (clindamycin,
lincomycin) count as *resistance-driving*? The port follows the shipping lineage and excludes it.
Two independent lines support that — every ref capable of building the application lacks it, and the
database's own `KB.AntibioticResistance*` tiers put it in *intermediate* — and the git chronology
settles the direction: one author created the collector **with** `J01FF%` in 2018, wrote the tier
views in 2019, and removed it in 2020 as a surgical edit. But the branch carrying the removal died
in 2023 while mainline, which never received it, is still developed. Neither archaeology nor a view
definition is clinical sign-off. §8.4.

**6. `ATC_A11EA` has no trailing `%`** (`EPR.QA.Collector.Drug.pas:44`), so `DRUG.A11EA` matches one
exact code while its title calls it a group. Same shape as (5), same owner, cheap to ask together.
§8.11 "Left open by Phase 5".

**7. §8.13, the 2023 SWEET field report** — date of birth and sex vanish from the extract because
they are `MetaFormItem.Expression` macros over two `NOT NULL` columns QuickStat already holds.
Root-caused; **parked by the product owner**. Listed only so it is not forgotten; the fix is three
lines plus one policy decision.

---

## Deployment-time, undischargeable here

**8. R10** — most `maxint`-batch collectors carry no `{IdList}` and scan whole tables, discarding
non-cohort rows client-side. Harmless on 25 patients, unknown on production volumes. Preserved
deliberately for parity; recorded as a performance follow-up.

**9. R11** — "what ships today" claims in `Docs/Port/01`, `02`, `04` and `05` are unverified except
where re-checked against the pinned ref. Confirm before relying on one.

**10. R13** — nobody has observed which branch Continua's `$Source.FastTrakDevelop` tracks. A
five-minute check for whoever has access; it would either confirm the row or overturn it.

---

## Phase 6 — cleanup

Signed off in principle, gated on the parity pass because the walk still needs `C:\work\qs-delphi`
and the reference worktree. One trap: deleting the `.dfm` files kills
`Ui/AppBannerIconTests.TheBannerIconIsTheOneTheDelphiFormCarries`, which must be **replaced** with a
recorded SHA-256 — it is the only thing tying the banner picture to the build being ported.
`PORT-PLAN.md` §5 Phase 6.

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
- **R1, the `Encrypt` / `TrustServerCertificate` default — closed for this port, on scope.**
  `Encrypt=True;TrustServerCertificate=True` is how every FastTrak application connects today, so
  the port changes nothing and decides nothing. Whether the estate should keep trusting arbitrary
  server certificates is a real question with a different owner and a wider blast radius than one
  application. §8 (2), §9 R1.
