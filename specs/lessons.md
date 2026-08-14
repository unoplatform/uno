# Review-panel lessons

Corrections the `/review-panel` reviewers should apply on future passes. Every reviewer
reads this file before returning findings (the "lessons loop"); the orchestrator appends
to it — only on the user's go-ahead — when the panel produces a false positive, misses
something, mis-rates a severity, or didn't know a repo convention.

Keep each lesson **generalizable**: a rule that applies to future changes, not a note about
one diff. No specific file paths from the change that triggered it.

## Entry format

```markdown
## YYYY-MM-DD — <short title>
- **Lens:** <architect | security | skeptic | quality | operability | contract | performance | jerome | all>
- **Lesson:** <what the panel got wrong, or the repo nuance it didn't know>
- **Apply:** <one-line rule for future reviews>
```

---

## 2026-06-24 — Generated DependencyProperty fields are expected public surface
- **Lens:** contract
- **Lesson:** `[GeneratedDependencyProperty]` emits `public static DependencyProperty …Property` fields and public CLR accessors by design; they mirror WinUI and are required by the property system.
- **Apply:** Don't flag generated `…Property` fields or their accessors as gratuitous public surface or a contract-minimality issue. See `.claude/rules/dependency-properties.md`.

## 2026-06-24 — A guard-only change is not a root-cause fix
- **Lens:** skeptic
- **Lesson:** Adding null/bounds guards while the broken lifecycle/ownership invariant still produces stale state is symptom-patching, not a fix.
- **Apply:** Per `.claude/rules/debugging-discipline.md`, require the mutation point to be corrected and labelled `root-cause fix`; treat guard-only changes as incomplete.

## 2026-06-24 — Don't strip Uno divergence markers
- **Lens:** quality
- **Lesson:** `// TODO Uno:` comments and `#if HAS_UNO` / `#if !__SKIA__` blocks deliberately mark intentional divergences from WinUI; they are not dead code or stale TODOs.
- **Apply:** Don't flag these markers as comment noise or unreachable code; flag only genuine drift.

## 2026-08-06 — Check a generator claim against its golden before reporting it
- **Lens:** skeptic, contract, architect
- **Lesson:** Tracing argument flow through the XAML generator reliably shows what *could* diverge but not what does. A finding that one emission path keys differently from another was reported high-severity from call-graph reading; the golden showed both paths emit identically, because the deciding value was passed as a literal from a type-switch branch rather than derived from the argument that was traced. The generator has a `WRITE_EXPECTED` flag that regenerates goldens, so confirming an emission claim costs one test run.
- **Apply:** Before reporting that a generator emits differently across two paths, diff the emitted golden for both. Report unconfirmed call-graph reasoning as a question or a coverage gap, not as a defect with a severity.

## 2026-08-06 — Don't call a pre-existing bug newly introduced without checking the old path
- **Lens:** security, all
- **Lesson:** A change that alters an input's *shape* often routes to an unguarded sink that the previous shape also reached. A crash was reported as introduced by the change under review; the pre-change value hit the same unguarded call and failed the same way. What the change actually did was make an intermittent failure deterministic — still worth fixing, but a different claim, priority, and owner.
- **Apply:** Before attributing a failure to the diff, trace the pre-change value to the same sink. State whether the change *introduces*, *makes deterministic*, or merely *exposes* the failure.

## 2026-08-06 — Cross-branch CI comparisons need the runtime pinned
- **Lens:** all
- **Lesson:** "Green on the other branch, red here" is not evidence the diff caused it when the branches build on different SDKs or TFMs — long-lived integration branches routinely carry a runtime bump the trunk doesn't. A failure was attributed to a branch's own commits until the two pipelines turned out to run different .NET versions.
- **Apply:** When citing another branch's CI as a baseline, confirm both ran the same SDK/TFM first; otherwise say the comparison is confounded rather than drawing a causal conclusion.

## 2026-08-14 — Verify an issue's counted file references before planning around them
- **Lens:** all
- **Lesson:** A rollup item's issue text overstated a packaging-reference count by 6x, named a subsystem to prune that had zero matching entries, and listed per-platform project variants that had already been collapsed into one. All three claims came from a generated assessment captured against an older worktree, and all three were wrong in the same direction — describing a repo state that no longer existed.
- **Apply:** Treat counts and file lists in a generated issue or spec as a starting query, not an inventory. Re-grep before sizing the work, and record the corrections in the PR so the next reader doesn't re-derive them.
